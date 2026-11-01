# Third Party Notices

このリポジトリのテンプレートには、次のサードパーティ製コンポーネントが含まれます。
This repository's templates include the following third-party components.

| Component | Where | License |
|---|---|---|
| Mixed Reality Toolkit 3 (`org.mixedrealitytoolkit.*`) | `Templates/AndroidXR-MRTK3/Packages/org.mixedrealitytoolkit.uxcore` (patched), `Templates/AndroidXR-MRTK3-Samples/Packages/`, `Templates/AndroidXR-MRTK3-Samples/Assets/` (samples from MRTKDevTemplate) | BSD 3-Clause, Copyright (c) Mixed Reality Toolkit Contributors — https://github.com/MixedRealityToolkit/MixedRealityToolkit-Unity |
| Mixed Reality Graphics Tools (`com.microsoft.mrtk.graphicstools.unity`) | `Templates/AndroidXR-MRTK3-Samples/Packages/MRTK/` | MIT, Copyright (c) Microsoft Corporation — https://github.com/microsoft/MixedReality-GraphicsTools-Unity |

Unity のパッケージ（`com.unity.*`）と Android XR Extensions（`com.google.xr.extensions`）は、テンプレートからプロジェクトを作るときに Unity Package Manager が取得します。
Unity packages (`com.unity.*`) and Android XR Extensions (`com.google.xr.extensions`) are resolved by the Unity Package Manager when a project is created from a template.

Android XR テンプレートの手のゴーストに使う XR Hands の HandVisualizer サンプルも同梱していません。作成後に Package Manager から取り込みます（README の「使い方」を参照）。
The XR Hands "HandVisualizer" sample used for the hand visuals in the Android XR template is not included either. It is imported from the Package Manager after the project is created (see "使い方" in the README).
