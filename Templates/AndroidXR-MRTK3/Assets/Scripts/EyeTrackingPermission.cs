using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
#endif

/// <summary>
/// Eye Gaze Interaction Profile が ON のときだけ、Android XR のアイトラッキング権限（EYE_TRACKING_FINE）を求める。
/// この権限が無いと視線が取れず、頭の向き（Head Gaze）で代用される。OFF のときは何もしない（確認を出さない）。
///
/// Requests the Android XR eye tracking permission (EYE_TRACKING_FINE) only when the Eye Gaze Interaction Profile is enabled.
/// Without this permission, eye gaze is not available and head gaze is used instead. Does nothing when the profile is off.
/// </summary>
public class EyeTrackingPermission : MonoBehaviour
{
    const string EyeTrackingFine = "android.permission.EYE_TRACKING_FINE";

    void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        var eyeGaze = OpenXRSettings.Instance != null ? OpenXRSettings.Instance.GetFeature<EyeGazeInteraction>() : null;
        if (eyeGaze == null || !eyeGaze.enabled) return;
        if (!Permission.HasUserAuthorizedPermission(EyeTrackingFine))
            Permission.RequestUserPermission(EyeTrackingFine);
#endif
    }
}
