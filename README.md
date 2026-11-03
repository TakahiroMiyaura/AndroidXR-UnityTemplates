# Android XR Unity Templates

English | [日本語](README.ja.md)

Unity project templates preconfigured for Android XR (Galaxy XR and other devices).
Use them from the Unity Hub "New project" window or with `unity projects create --template` in the Unity CLI.

| Template | Name (`--template`) | Contents |
|---|---|---|
| Android XR | `com.reseul.template.androidxr` | URP with the Android XR packages and settings. XR Origin, a cube in front of you, and hand visualization |
| Android XR with MRTK3 | `com.reseul.template.androidxr-mrtk3` | The above plus MRTK3. MRTK XR Rig and a cube you can grab with your hands |
| Android XR with MRTK3 (Samples) | `com.reseul.template.androidxr-mrtk3-samples` | The above plus the official MRTK3 sample scenes |

### Scenes

Scene names starting with `VR_` use the Skybox (VR view). Names starting with `Passthrough_` show your surroundings (passthrough). The first scene is the VR one.

| Template | Scenes |
|---|---|
| Android XR | `VR_SampleScene` (first), `Passthrough_SampleScene` |
| Android XR with MRTK3 | `MRTK3_VR_SampleScene` (first), `MRTK3_Passthrough_SampleScene`, `MRTK3_EyeAndHand_SampleScene` (select with your eyes, grab with your hands; an explanation is shown in the scene) |
| Android XR with MRTK3 (Samples) | `HandInteractionExamples` (first), `HandInteractionExamples_Passthrough` (modified to use passthrough and spatial awareness), and the other official samples. Only the samples that work on Android XR are in the build; switch scenes with Next / Previous on the hand menu. The three MRTK3 scenes above are not included because they are not MRTK3 samples |

The following official samples stay in the Samples project but are not in the build, because they do not work on Android XR or assume a different setup.

| Scenes | Reason |
|---|---|
| SpeechInputExamples, DictationExample, TextToSpeechExamples, SeeItSayItExample | Require Windows speech recognition and text-to-speech |
| SpatialMappingExample | Requires HoloLens spatial mapping (try spatial awareness in the passthrough scene instead) |
| EyeGazeExample, the 5 scenes in `EyeTracking/` | Require eye tracking (off in these templates; try eye gaze in `MRTK3_EyeAndHand_SampleScene` of the MRTK3 template) |
| The 5 scenes in `Experimental/` | Experimental features |
| SampleEmptyMRTKScene | An empty scene to start from. It has no hand menu, so you could not move on to the next scenes |

- Unity **6000.3.27f1** (with Android Build Support)
- `com.unity.xr.androidxr-openxr` 1.5.1 / `com.unity.xr.openxr` 1.19.0 / `com.google.xr.extensions` v1.5.0 / XR Interaction Toolkit 3.7.0
- MRTK3: from the Unity registry (core 3.2.2 and others). Only the Samples template includes the latest stable packages from GitHub Releases (core 3.3.0 and others)

## What is configured

- XR Plug-in Management: OpenXR loader for Android, the "Android XR" feature group, and no Project Validation issues
- Player Settings: Vulkan / IL2CPP / ARM64 / Min API 25 / GameActivity
- URP: HDR and post-processing disabled
- XR Origin: Tracking Origin Mode is Device. The origin is your head at startup (+Z is the direction you face), and it follows Recenter in the Android XR settings. Not Specified / Floor would use the STAGE space (the system boundary), whose origin is kept from earlier sessions
- The Unbounded Reference Space feature is off. It is only used when you request the Unbounded tracking origin mode (world-locked, does not follow Recenter)
- Eye Gaze Interaction Profile and Foveated Rendering are off. When they are on, every scene asks for eye tracking permission at startup. Turn on Eye Gaze with `Tools > Android XR > Enable Eye Tracking`
- Passthrough scenes: AR Camera Manager on the camera, Solid Color background with alpha 0, Post Processing off, and an AR Session
- Spatial awareness (passthrough scenes): real floors, walls, and furniture become meshes with Android XR Extensions Scene Meshing (`Assets/AndroidXR/Scripts/SpatialMeshController.cs`)
  - The meshes are drawn with a depth-only shader (`Android XR/Depth Only Occluder`), so virtual objects behind real ones are hidden
  - The meshes have MeshColliders, so virtual objects hit the real floor and walls (release a cube or ball and it falls and rolls on the floor)
  - Meshing starts after the `SCENE_UNDERSTANDING_FINE` permission is granted (`EnableAfterPermission`). Objects stay in the air until a floor is found below them (`GravityWhenGroundFound`)
  - Turn on **Show Mesh** on `SpatialMeshController` to draw the meshes semi-transparent (for checking)
- MRTK3: an Android XR profile that takes hand joints from XR Hands (`UnityHandsSubsystem`) is assigned to Android
- `Assets/Editor/ManifestDedupe.cs`: merges the duplicated `uses-feature android.software.xr.api.spatial` entries (without it, the APK build fails)
- `Assets/Editor/AndroidXRProjectInit.cs`: sets the package name and product name

## Installing the templates

### Install-Templates.ps1 (with the Unity CLI)

```powershell
./Install-Templates.ps1                          # all three
./Install-Templates.ps1 -Template androidxr      # the basic template only
```

It downloads the `.tgz` files from [Releases](https://github.com/TakahiroMiyaura/AndroidXR-UnityTemplates/releases) and puts them in the folder shown by `unity templates location`.

### Manually

1. Download `comreseultemplate*.tgz` from [Releases](https://github.com/TakahiroMiyaura/AndroidXR-UnityTemplates/releases)
2. Put them in your templates folder (check it with `unity templates location`; the Unity Hub default is `%USERPROFILE%\Unity user templates`)
3. They appear in the Unity Hub "New project" window

## Usage

```powershell
unity projects create MyXrApp --path D:/WorkSpaces --editor-version 6000.3.27f1 --template com.reseul.template.androidxr
unity run D:/WorkSpaces/MyXrApp -- -buildTarget Android -executeMethod AndroidXRProjectInit.Run -appId com.example.myxrapp -productName MyXrApp
```

The second line switches the build target to Android and sets the package name and product name (the package name is set for Android, Standalone, and iOS). Add `-companyName MyCompany` to set the company name as well.
**If you keep the template's package name, every project gets the same one and installs over the others on the device.** If you created the project from Unity Hub, change it in Player Settings.

For the basic Android XR template, the second line also imports the XR Hands HandVisualizer sample used for hand visualization. It is not included in the template because of its license.
If you created the project from Unity Hub, run `Tools > Android XR > Import Hand Visualizer Sample` once. Until then, the hand prefabs in the scenes show as Missing.

## Known issues and workarounds

Some code in the packages used here does not compile as is. It has been fixed upstream but not released yet, so the template applies the same fix in advance. Once a release with the fix is used, the workaround is no longer needed and will be removed.

| Issue | Workaround in the template | Upstream status | No longer needed when |
|---|---|---|---|
| `DialogButton.cs` in MRTK3 uxcore fails with CS0592 on newer Unity 6.3 patches (`[SerializeField]` is put on the class). This comes from the Unity version, not from Android XR, and happens on every platform | A copy of uxcore with that one line removed is embedded in `Packages/org.mixedrealitytoolkit.uxcore` (the change is described in `PATCHED.md` in the same folder) | [MRTK#1101](https://github.com/MixedRealityToolkit/MixedRealityToolkit-Unity/issues/1101) is closed and fixed on main. No package with the fix has been released yet | A uxcore release with the fix is out and the template uses it |

## Known limitations

- The XR Session Target API Version is fixed to 3 (the version of `android.software.xr.api.spatial` in the manifest). With the default Automatic, Android XR Extensions v1.5.0 writes 4, and Galaxy XR (3) would be excluded from distribution. The value is a minimum requirement, so 3 also works on devices with 4 or later. Geospatial needs level 4 or later and GPS (`android.hardware.location.gps`), so it is not available on Galaxy XR. Only when you use a feature that needs level 4, open the XR Session settings in the Android tab of **Project Settings > XR Plug-in Management > OpenXR** and change Target API Version. Check a device's level with `adb shell pm list features`.
- Some audio mixers in the Samples refer to Microsoft Spatializer (a native plug-in for Windows), so the build shows "Effect Microsoft Spatializer Mixer could not be found". On Android, only the plug-in's HRTF (the sense of up/down and front/back) is unavailable. Changes in sound by source position and the muffling behind walls (AudioOcclusionExample) still work. The build and the app are not affected.
- MRTK3 gaze pinch (look and pinch) does not work on Android XR devices (Galaxy XR). Selecting with your eyes (highlight) works, and you can grab with the hand ray or by grabbing directly.
- In SolverExamples and TapToPlaceExample of the Samples, ARMeshManager (room mesh display) is disabled and the material of the virtual floor and walls (TestDummyWalls) is replaced with a semi-transparent one. The original wireframe material (MRTK_Wireframe) uses a geometry shader, which breaks the left-eye image on Android XR (Vulkan multiview). You can still try snapping and placement on the virtual floor and walls.
- With Tracking Origin Mode Device, the floor height is not known. The camera height is assumed with XR Origin Camera Y Offset (1.5 m in the basic template, 1.6 m in the MRTK XR Rig), so content can look higher than expected when you sit.
- After Recenter, the spatial meshes are regenerated so that they line up with the real world again (it takes a few seconds).
- Passthrough occlusion uses the spatial awareness meshes (virtual objects behind real floors, walls, and furniture are hidden). Moving things such as your hands do not hide virtual objects. Depth-based occlusion with AR Foundation (AR Occlusion Manager) is not included, because it needs special shaders that read the depth.

## Repository layout

```text
Templates/AndroidXR/                  Source projects of the templates (Assets / Packages / ProjectSettings)
Templates/AndroidXR-MRTK3/
Templates/AndroidXR-MRTK3-Samples/    The tgz files in Packages/MRTK are not in git. Run Get-MrtkPackages.ps1 before opening it directly
Install-Templates.ps1                 Installs the templates from Releases
```

You can also open a source project in Unity, change it, and make your own template with `unity templates create <path> --name <name> --keep-project-settings --keep-embedded-packages`.

## License

The scripts and settings in this repository are under the [Boost Software License 1.0](LICENSE).
Bundled third-party components (MRTK3, Mixed Reality Graphics Tools) are under their own licenses. See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
