using System.Collections.Generic;
using Google.XR.Extensions;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Android XR Extensions の Scene Meshing（XR_ANDROID_scene_meshing）で、現実の床・壁・家具のメッシュを作る。
/// メッシュはプレハブ（奥行きだけ書くマテリアル + MeshCollider）で置くので、
/// 現実の物の後ろの仮想物が隠れ、仮想物は現実の床や壁に当たる。
///
/// XR Origin の Camera Offset の子に置き、SCENE_UNDERSTANDING_FINE が許可されてから有効にする（EnableAfterPermission）。
/// Extensions のメッシュは位置・向き・大きさを生成結果で返すので、それを各メッシュの Transform に反映する
/// （AR Mesh Manager はこれを反映しないため使わない）。
/// 作り直しは少しずつ・間を空けて行う。一度に全部作り直すとコライダーの作成で CPU を使い切り、端末全体が重くなる。
/// Android XR の「中央に再配置」で原点が変わると、置いてあるメッシュは古い原点のまま一緒に回ってしまうので、
/// すべて作り直して、新しい原点での位置に置き直す。
///
/// Builds real-world meshes (floor, walls, furniture) with Android XR Extensions scene meshing.
/// Each mesh uses the prefab (depth-only material + MeshCollider): virtual objects behind real surfaces are hidden
/// and collide with them. Put this under the XR Origin and enable it after SCENE_UNDERSTANDING_FINE is granted.
/// Meshes are regenerated a few at a time with a pause in between, to keep the device responsive.
/// </summary>
public class SpatialMeshController : MonoBehaviour
{
    [SerializeField] MeshFilter meshPrefab;
    [Tooltip("一度に作るメッシュの最大数 / Max meshes generated per batch")]
    [SerializeField] int maxMeshesPerBatch = 16;
    [Tooltip("ひとまとまり作り終えてから次を作るまでの秒数 / Seconds to wait between batches")]
    [SerializeField] float batchInterval = 0.25f;
    [Tooltip("メッシュを見えるようにする（空間認識の確認用）。OFF なら見えないまま奥行きだけ書く / Show the mesh for debugging")]
    [SerializeField] bool showMesh;
    [SerializeField] Material visibleMaterial;

    readonly Dictionary<MeshId, MeshFilter> meshes = new Dictionary<MeshId, MeshFilter>();
    readonly List<MeshInfo> meshInfos = new List<MeshInfo>();
    // 作る（作り直す）必要があるメッシュ。差分は一度しか返らないので、ここに貯めて少しずつ処理する
    readonly List<MeshId> pending = new List<MeshId>();
    readonly HashSet<MeshId> pendingSet = new HashSet<MeshId>();
    XRMeshSubsystem subsystem;
    readonly List<XRInputSubsystem> inputSubsystems = new List<XRInputSubsystem>();
    int generating;
    float nextBatchTime;

    public int MeshCount => meshes.Count;
    public int PendingCount => pending.Count;

    void OnEnable()
    {
        var list = new List<XRMeshSubsystem>();
        SubsystemManager.GetSubsystems(list);
        subsystem = list.Count > 0 ? list[0] : null;
        if (subsystem == null)
            Debug.LogWarning("[SpatialMeshController] No XRMeshSubsystem. Enable 'Android XR (Extensions): Scene Meshing' in the OpenXR settings.");

        // 原点が変わったら（中央に再配置など）、置いてあるメッシュを作り直す
        SubsystemManager.GetSubsystems(inputSubsystems);
        foreach (var input in inputSubsystems) input.trackingOriginUpdated += OnTrackingOriginUpdated;
    }

    void OnDisable()
    {
        foreach (var input in inputSubsystems) input.trackingOriginUpdated -= OnTrackingOriginUpdated;
        inputSubsystems.Clear();
    }

    void OnTrackingOriginUpdated(XRInputSubsystem input)
    {
        foreach (var id in meshes.Keys)
            if (pendingSet.Add(id)) pending.Add(id);
        Debug.Log($"[SpatialMeshController] Tracking origin updated. Regenerating {pending.Count} meshes");
    }

    void Update()
    {
        if (subsystem == null || !subsystem.running || meshPrefab == null) return;

        CollectChanges();

        if (generating > 0 || Time.time < nextBatchTime || pending.Count == 0) return;
        var count = Mathf.Min(maxMeshesPerBatch, pending.Count);
        for (var i = 0; i < count; i++)
        {
            var id = pending[0];
            pending.RemoveAt(0);
            pendingSet.Remove(id);
            Generate(id);
        }
    }

    void CollectChanges()
    {
        if (!subsystem.TryGetMeshInfos(meshInfos)) return;
        foreach (var info in meshInfos)
        {
            // 手のメッシュなど、空間以外のメッシュは除く
            if (!subsystem.IsSceneMeshId(info.MeshId)) continue;

            switch (info.ChangeState)
            {
                case MeshChangeState.Added:
                case MeshChangeState.Updated:
                    if (pendingSet.Add(info.MeshId)) pending.Add(info.MeshId);
                    break;
                case MeshChangeState.Removed:
                    if (pendingSet.Remove(info.MeshId)) pending.Remove(info.MeshId);
                    if (meshes.TryGetValue(info.MeshId, out var removed))
                    {
                        Destroy(removed.gameObject);
                        meshes.Remove(info.MeshId);
                    }
                    break;
            }
        }
    }

    void Generate(MeshId id)
    {
        if (!meshes.TryGetValue(id, out var filter))
        {
            filter = Instantiate(meshPrefab, transform);
            filter.name = "SpatialMesh " + id;
            filter.sharedMesh = new Mesh();
            if (showMesh && visibleMaterial != null) filter.GetComponent<MeshRenderer>().sharedMaterial = visibleMaterial;
            meshes.Add(id, filter);
        }
        generating++;
        // ConsumeTransform: 頂点はメッシュのローカル座標で返り、位置・向き・大きさは結果に入る。
        // コライダーはここでは渡さない: 渡すと変換を分ける前の座標で作られ、Transform が二重にかかって 90° 回転した位置になる
        // （床がカメラを通る縦の面になる）。生成後に、表示用のメッシュから作り直す
        subsystem.GenerateMeshAsync(id, filter.sharedMesh, null, MeshVertexAttributes.Normals,
            OnMeshGenerated, MeshGenerationOptions.ConsumeTransform);
    }

    void OnMeshGenerated(MeshGenerationResult result)
    {
        generating--;
        if (generating == 0) nextBatchTime = Time.time + batchInterval;
        if (result.Status != MeshGenerationStatus.Success) return;
        if (!meshes.TryGetValue(result.MeshId, out var filter)) return;
        filter.transform.localPosition = result.Position;
        filter.transform.localRotation = result.Rotation;
        filter.transform.localScale = result.Scale;
        var collider = filter.GetComponent<MeshCollider>();
        if (collider != null)
        {
            // 同じメッシュを入れ直しても作り直されないので、いったん外してから入れる
            collider.sharedMesh = null;
            collider.sharedMesh = filter.sharedMesh;
        }
    }
}
