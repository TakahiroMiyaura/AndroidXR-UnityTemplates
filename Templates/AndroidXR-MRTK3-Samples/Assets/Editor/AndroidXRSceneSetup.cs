using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

/// <summary>
/// 開いているシーンを「頭の動きに追従し、手でつかめる最小構成」にする。
/// Main Camera を XR Origin の下に移し、Tracked Pose Driver で HMD の姿勢を反映させる。
/// 左右の手に XR Interaction Toolkit の Near-Far Interactor（つかむ・レイ）と Poke Interactor（押す）を付ける。
/// 目印として正面につかめるキューブ（XRGrabInteractable）を置く。すでに XR Origin があるシーンでは、手が無ければ手だけ足す。
///
/// 手の入力は OpenXR の Hand Interaction Profile（&lt;HandInteraction&gt;）。XRI のサンプル（Starter Assets）は使わない。
///
/// メニュー「Tools/Android XR/Scene Setup」「Tools/Android XR/Add Hand Interaction」、または batchmode:
/// unity run &lt;project&gt; -- -buildTarget Android -executeMethod AndroidXRSceneSetup.Run
/// </summary>
public static class AndroidXRSceneSetup
{
    const string ScenePath = "Assets/Scenes/VR_SampleScene.unity";

    [MenuItem("Tools/Android XR/Scene Setup")]
    public static void SetupOpenScene() => Setup(EditorSceneManager.GetActiveScene());

    [MenuItem("Tools/Android XR/Add Hand Interaction")]
    public static void AddHandInteractionToOpenScene()
    {
        var origin = Object.FindFirstObjectByType<XROrigin>();
        if (origin == null)
        {
            Debug.LogWarning("[AndroidXRSceneSetup] XR Origin がありません。先に Tools/Android XR/Scene Setup を実行してください");
            return;
        }
        if (AddHandInteraction(origin)) Save(origin.gameObject.scene);
    }

    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Setup(scene);
    }

    static void Setup(UnityEngine.SceneManagement.Scene scene)
    {
        var existing = Object.FindFirstObjectByType<XROrigin>();
        if (existing != null)
        {
            Debug.Log("[AndroidXRSceneSetup] XR Origin は既にあります（手の Interactor が無ければ足します）");
            if (AddHandInteraction(existing)) Save(scene);
            return;
        }

        // XR Origin / Camera Offset
        var originGo = new GameObject("XR Origin");
        var offsetGo = new GameObject("Camera Offset");
        offsetGo.transform.SetParent(originGo.transform, false);

        // 既存の Main Camera を流用する（URP のカメラ設定を引き継ぐため）
        var camera = Camera.main;
        if (camera == null)
        {
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camera = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
        }
        camera.transform.SetParent(offsetGo.transform, false);
        camera.transform.localPosition = Vector3.zero;
        camera.transform.localRotation = Quaternion.identity;
        camera.nearClipPlane = 0.01f;

        // HMD の姿勢をカメラに反映する
        // Unity の Object は ?? で null 判定できない（Editor では偽の null が返る）ので明示的に比べる
        var tpd = camera.GetComponent<TrackedPoseDriver>();
        if (tpd == null) tpd = camera.gameObject.AddComponent<TrackedPoseDriver>();
        tpd.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
        tpd.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
        tpd.positionInput = new InputActionProperty(Action("Position", "Vector3", "<XRHMD>/centerEyePosition"));
        tpd.rotationInput = new InputActionProperty(Action("Rotation", "Quaternion", "<XRHMD>/centerEyeRotation"));
        tpd.trackingStateInput = new InputActionProperty(Action("Tracking State", "Integer", "<XRHMD>/trackingState"));

        var origin = originGo.AddComponent<XROrigin>();
        origin.Origin = originGo;
        origin.CameraFloorOffsetObject = offsetGo;
        origin.Camera = camera;
        // Device: 起動時の頭の位置・向きが原点になり、+Z が正面。Android XR の「中央に再配置」にも追従する
        // （Not Specified / Floor は床基準の空間になり、再配置に追従しない）。頭の高さは Camera Y Offset で足す
        origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;
        origin.CameraYOffset = 1.5f;

        AddHandInteraction(origin);

        // 見回したときの目印（高さ 1.5 m = 目の高さ、正面 1.5 m）。手のレイで指してピンチするとつかめる
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Reference Cube";
        cube.transform.position = new Vector3(0f, 1.5f, 1.5f);
        cube.transform.localScale = Vector3.one * 0.2f;
        MakeGrabbable(cube);

        Save(scene);
        Debug.Log($"[AndroidXRSceneSetup] XR Origin を追加しました: {scene.path}");
    }

    static void Save(UnityEngine.SceneManagement.Scene scene)
    {
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    /// <summary>
    /// 手でつかめるようにする（XRGrabInteractable + Rigidbody）。このままでは離した場所に止まる（重力で落とすのは別のスクリプト）。
    /// つかんだ点を保ったまま動かす（Use Dynamic Attach）。
    /// </summary>
    public static XRGrabInteractable MakeGrabbable(GameObject go)
    {
        var grab = go.GetComponent<XRGrabInteractable>();
        if (grab == null) grab = go.AddComponent<XRGrabInteractable>();
        grab.useDynamicAttach = true;
        var body = go.GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        // 投げたときに薄い面（空間メッシュ）を抜けないように
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        return grab;
    }

    // ---------------- hands ----------------

    /// <summary>
    /// XR Origin の Camera Offset の下に左右の手（Near-Far Interactor・Poke Interactor）を置く。すでにあれば何もしない。
    /// 手が追跡されているあいだだけ有効にする（XR Input Modality Manager）。
    /// </summary>
    public static bool AddHandInteraction(XROrigin origin)
    {
        var parent = origin.CameraFloorOffsetObject != null ? origin.CameraFloorOffsetObject.transform : origin.transform;
        if (parent.Find("Left Hand Interaction") != null) return false;

        if (Object.FindFirstObjectByType<XRInteractionManager>() == null)
            new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();

        var left = CreateHand(parent, "Left");
        var right = CreateHand(parent, "Right");

        var modality = origin.GetComponent<XRInputModalityManager>();
        if (modality == null) modality = origin.gameObject.AddComponent<XRInputModalityManager>();
        modality.leftHand = left;
        modality.rightHand = right;
        Debug.Log("[AndroidXRSceneSetup] 手の Interactor を追加しました（Near-Far / Poke）");
        return true;
    }

    // Left Hand Interaction
    //   Pinch Point          … 親指と人差し指の間（pinch）。近くでつかむときの判定の中心
    //   Near-Far Interactor  … 手のレイ（aim）。近くは Pinch Point の球、遠くはレイでつかむ。ピンチ・握りで選択
    //     Line Visual
    //   Poke Interactor      … 人差し指の先（poke）。押せる物（XRPokeFilter 付き）だけを押す
    static GameObject CreateHand(Transform parent, string side)
    {
        var hand = new GameObject($"{side} Hand Interaction");
        hand.transform.SetParent(parent, false);

        var pinchPoint = new GameObject("Pinch Point");
        pinchPoint.transform.SetParent(hand.transform, false);
        AddPoseDriver(pinchPoint, side, "pinch");

        // Near-Far
        var nearFarGo = new GameObject("Near-Far Interactor");
        nearFarGo.transform.SetParent(hand.transform, false);
        AddPoseDriver(nearFarGo, side, "aim");
        var near = nearFarGo.AddComponent<SphereInteractionCaster>();
        near.castOrigin = pinchPoint.transform;
        near.castRadius = 0.04f;
        var far = nearFarGo.AddComponent<CurveInteractionCaster>();
        // 手のレイは揺れるので、根元の位置と向きを安定させる
        far.enableStabilization = true;
        var nearFar = nearFarGo.AddComponent<NearFarInteractor>();
        nearFar.handedness = side == "Left" ? InteractorHandedness.Left : InteractorHandedness.Right;
        nearFar.nearInteractionCaster = near;
        nearFar.farInteractionCaster = far;
        // テンプレートには UI が無い（UI を使うなら ON にし、EventSystem に XR UI Input Module を置く）
        nearFar.enableUIInteraction = false;
        // ピンチ（親指と人差し指）か握りで選択する。XRHandDevice（XR Hands）と HandInteraction（OpenXR）の両方を見る
        nearFar.selectInput = Button($"{side} Select", side,
            new[] { "<XRHandDevice>{H}/pinchTouched", "<XRHandDevice>{H}/graspFirm", "<HandInteraction>{H}/pinchTouched", "<HandInteraction>{H}/graspFirm" },
            new[] { "<XRHandDevice>{H}/pinchValue", "<XRHandDevice>{H}/graspValue", "<HandInteraction>{H}/pinchValue", "<HandInteraction>{H}/graspValue" });
        nearFar.activateInput = new XRInputButtonReader($"{side} Activate", $"{side} Activate Value", false, XRInputButtonReader.InputSourceMode.Unused);

        var lineGo = new GameObject("Line Visual");
        lineGo.transform.SetParent(nearFarGo.transform, false);
        var line = lineGo.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        // LineRenderer の既定の太さは 1 m。CurveVisualController が太さを変えない状態でも細く見えるよう、ここで 5 mm → 2 mm にする
        // （AURA で、手から出たレイが太い白い帯に見えた）
        line.widthMultiplier = 1f;
        line.widthCurve = AnimationCurve.Linear(0f, 0.005f, 1f, 0.002f);
        line.numCapVertices = 4;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.sharedMaterial = LineMaterial();
        var visual = lineGo.AddComponent<CurveVisualController>();
        visual.lineRenderer = line;
        visual.curveInteractionDataProvider = nearFar;
        // 既定の OFF だと状態ごとの太さ（5 mm）と色が使われず、LineRenderer の太さのままになる
        visual.customizeLinePropertiesForState = true;

        // Poke
        var pokeGo = new GameObject("Poke Interactor");
        pokeGo.transform.SetParent(hand.transform, false);
        AddPoseDriver(pokeGo, side, "poke");
        var poke = pokeGo.AddComponent<XRPokeInteractor>();
        poke.handedness = nearFar.handedness;
        poke.enableUIInteraction = false;

        return hand;
    }

    // OpenXR の Hand Interaction Profile（XR_EXT_hand_interaction）の aim / pinch / poke の姿勢を Tracked Pose Driver で反映する。
    // XRI の Starter Assets は XR Hands の <XRHandDevice> を使うが、AURA ではそちらの aim / pinch / poke が 0 のまま
    // （関節だけ取れる）で、レイが目の位置から出た。<HandInteraction> は Galaxy XR・AURA の両方で取れる。
    // 姿勢は 1 つの入力元だけにする（2 つバインドすると、値が少し違うときに入力元が行き来して揺れる）
    static void AddPoseDriver(GameObject go, string side, string pose)
    {
        var tpd = go.AddComponent<TrackedPoseDriver>();
        tpd.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
        tpd.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
        var h = "{" + side + "Hand}";
        var control = pose == "aim" ? "pointer" : pose;  // HandInteraction では aim の姿勢を pointer と呼ぶ
        tpd.positionInput = new InputActionProperty(Action($"{side} {pose} Position", "Vector3", $"<HandInteraction>{h}/{control}Position"));
        tpd.rotationInput = new InputActionProperty(Action($"{side} {pose} Rotation", "Quaternion", $"<HandInteraction>{h}/{control}Rotation"));
        tpd.trackingStateInput = new InputActionProperty(Action($"{side} {pose} Tracking State", "Integer", $"<HandInteraction>{h}/trackingState"));
    }

    // ボタン（押した・離した）と値（どこまで押したか）の 2 つのアクションで読む。{H} は {LeftHand} / {RightHand}
    static XRInputButtonReader Button(string name, string side, string[] buttonBindings, string[] valueBindings)
    {
        var h = "{" + side + "Hand}";
        var reader = new XRInputButtonReader(name, name + " Value", false, XRInputButtonReader.InputSourceMode.InputAction);
        var performed = new InputAction(name, InputActionType.Button);
        foreach (var b in buttonBindings) performed.AddBinding(b.Replace("{H}", h));
        var value = new InputAction(name + " Value", InputActionType.Value, expectedControlType: "Axis");
        foreach (var b in valueBindings) value.AddBinding(b.Replace("{H}", h));
        reader.inputActionPerformed = performed;
        reader.inputActionValue = value;
        return reader;
    }

    static InputAction Action(string name, string controlType, string binding)
    {
        var action = new InputAction(name, expectedControlType: controlType);
        action.AddBinding(binding);
        return action;
    }

    // 手のレイの線。URP の Particles/Unlit は頂点カラー（CurveVisualController のグラデーション）を使う
    const string LineMaterialPath = "Assets/AndroidXR/Materials/HandRay.mat";

    static Material LineMaterial()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(LineMaterialPath);
        if (material != null) return material;
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        material = new Material(shader) { name = "HandRay" };
        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1);
            material.SetFloat("_Blend", 0);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
        }
        if (!AssetDatabase.IsValidFolder("Assets/AndroidXR")) AssetDatabase.CreateFolder("Assets", "AndroidXR");
        if (!AssetDatabase.IsValidFolder("Assets/AndroidXR/Materials")) AssetDatabase.CreateFolder("Assets/AndroidXR", "Materials");
        AssetDatabase.CreateAsset(material, LineMaterialPath);
        return material;
    }
}
