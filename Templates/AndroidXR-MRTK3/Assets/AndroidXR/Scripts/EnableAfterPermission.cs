using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

/// <summary>
/// Android の権限が許可されてから、指定したコンポーネントを有効にする。
/// Android XR の空間認識（AR Mesh Manager）やオクルージョンは SCENE_UNDERSTANDING_FINE が必要で、
/// 許可の前に有効にするとエラーになるため、シーンでは無効にしておき、許可されたらこれで有効にする。
/// Editor では許可を求めずにすぐ有効にする。
///
/// Enables the target components after an Android permission is granted.
/// Android XR meshing (AR Mesh Manager) and occlusion need SCENE_UNDERSTANDING_FINE; keep them disabled in the scene
/// and let this component enable them once the permission is granted. In the Editor they are enabled immediately.
/// </summary>
public class EnableAfterPermission : MonoBehaviour
{
    [SerializeField] string permission = "android.permission.SCENE_UNDERSTANDING_FINE";
    [SerializeField] Behaviour[] targets = new Behaviour[0];

    void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (Permission.HasUserAuthorizedPermission(permission))
        {
            EnableTargets();
            return;
        }
        var callbacks = new PermissionCallbacks();
        callbacks.PermissionGranted += granted => { if (granted == permission) EnableTargets(); };
        callbacks.PermissionDenied += denied => Debug.LogWarning($"[EnableAfterPermission] {denied} was denied. {targets.Length} component(s) stay disabled.");
        Permission.RequestUserPermission(permission, callbacks);
#else
        EnableTargets();
#endif
    }

    void EnableTargets()
    {
        foreach (var target in targets)
        {
            if (target == null) continue;
            target.enabled = true;
            Debug.Log($"[EnableAfterPermission] {permission} granted. Enabled {target.GetType().Name} on {target.name}");
        }
    }
}
