using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

/// <summary>
/// 開いているシーンを「頭の動きに追従する最小構成」にする。
/// Main Camera を XR Origin の下に移し、Tracked Pose Driver で HMD の姿勢を反映させる。
/// 目印として正面にキューブを置く。すでに XR Origin があるシーンでは何もしない。
///
/// メニュー「Tools/Android XR/Scene Setup」、または batchmode:
/// unity run &lt;project&gt; -- -buildTarget Android -executeMethod AndroidXRSceneSetup.Run
/// </summary>
public static class AndroidXRSceneSetup
{
    const string ScenePath = "Assets/Scenes/VR_SampleScene.unity";

    [MenuItem("Tools/Android XR/Scene Setup")]
    public static void SetupOpenScene() => Setup(EditorSceneManager.GetActiveScene());

    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Setup(scene);
    }

    static void Setup(UnityEngine.SceneManagement.Scene scene)
    {
        if (Object.FindFirstObjectByType<XROrigin>() != null)
        {
            Debug.Log("[AndroidXRSceneSetup] XR Origin は既にあるので何もしません");
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
        // Tracking Origin Mode は既定（Not Specified）のまま。起動時の頭の位置・向きが原点になり、+Z が正面になる

        // 見回したときの目印（床から 1.5 m、正面 1.5 m）
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Reference Cube";
        cube.transform.position = new Vector3(0f, 1.5f, 1.5f);
        cube.transform.localScale = Vector3.one * 0.2f;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[AndroidXRSceneSetup] XR Origin を追加しました: {scene.path}");
    }

    static InputAction Action(string name, string controlType, string binding)
    {
        var action = new InputAction(name, expectedControlType: controlType);
        action.AddBinding(binding);
        return action;
    }
}
