using UnityEditor;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;

/// <summary>
/// Android の OpenXR 設定で Eye Gaze Interaction Profile を ON / OFF する。
/// テンプレートでは OFF（ON にすると、どのシーンでも起動時にアイトラッキングの許可を求められる）。
///
/// Toggles the Eye Gaze Interaction Profile in the Android OpenXR settings.
/// It is OFF in the templates. When ON, the app asks for eye tracking permission at startup in every scene.
/// </summary>
public static class AndroidXREyeTracking
{
    const string EnableMenu = "Tools/Android XR/Enable Eye Tracking";
    const string DisableMenu = "Tools/Android XR/Disable Eye Tracking";

    [MenuItem(EnableMenu)]
    static void Enable() => Set(true);

    [MenuItem(DisableMenu)]
    static void Disable() => Set(false);

    [MenuItem(EnableMenu, true)]
    static bool CanEnable() => Feature() is { enabled: false };

    [MenuItem(DisableMenu, true)]
    static bool CanDisable() => Feature() is { enabled: true };

    static EyeGazeInteraction Feature()
    {
        var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        return settings != null ? settings.GetFeature<EyeGazeInteraction>() : null;
    }

    static void Set(bool on)
    {
        var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        var feature = settings != null ? settings.GetFeature<EyeGazeInteraction>() : null;
        if (feature == null)
        {
            Debug.LogWarning("[AndroidXREyeTracking] Eye Gaze Interaction Profile was not found in the Android OpenXR settings.");
            return;
        }
        feature.enabled = on;
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        Debug.Log($"[AndroidXREyeTracking] Eye Gaze Interaction Profile (Android): {(on ? "ON" : "OFF")}. Rebuild the app to apply.");
    }
}
