# Android XR Unity Templates

[English](README.md) | 日本語

Android XR（Galaxy XR など）向けの設定を済ませた Unity プロジェクトテンプレートです。
Unity Hub の新規作成画面、または Unity CLI の `unity projects create --template` から使えます。

| テンプレート | 名前（`--template`） | 中身 |
|---|---|---|
| Android XR | `com.reseul.template.androidxr` | URP + Android XR のパッケージと設定。XR Origin・正面のキューブ・手のゴースト |
| Android XR with MRTK3 | `com.reseul.template.androidxr-mrtk3` | 上記 + MRTK3。MRTK XR Rig と、手で掴めるキューブ |
| Android XR with MRTK3 (Samples) | `com.reseul.template.androidxr-mrtk3-samples` | 上記 + MRTK3 の公式サンプルシーン一式 |

### シーン

シーン名の `VR_` は Skybox の VR 表示、`Passthrough_` はパススルー（周りが見える）表示です。最初のシーンは VR です。

| テンプレート | シーン |
|---|---|
| Android XR | `VR_SampleScene`（最初）、`Passthrough_SampleScene` |
| Android XR with MRTK3 | `MRTK3_VR_SampleScene`（最初）、`MRTK3_Passthrough_SampleScene`、`MRTK3_EyeAndHand_SampleScene`（視線で選び、手で掴む。シーン内に英語の説明あり） |
| Android XR with MRTK3 (Samples) | `HandInteractionExamples`（最初）、`HandInteractionExamples_Passthrough`（パススルーと空間認識を使うように改造したもの）、ほかの公式サンプル（Android XR で使えるものだけビルドに入れる。手のメニューの「次へ／前へ」で切り替え）。上記 MRTK3 の 3 シーンは MRTK3 のサンプルではないので入れていません |

Samples の公式サンプルのうち、次のシーンはプロジェクトに残していますが、ビルドには入れていません（Android XR では動かない、または前提が違うため）。

| シーン | 理由 |
|---|---|
| SpeechInputExamples、DictationExample、TextToSpeechExamples、SeeItSayItExample | Windows の音声認識・音声合成が前提 |
| SpatialMappingExample | HoloLens の空間マッピングが前提（空間認識はパススルー版のシーンで確認できます） |
| EyeGazeExample、`EyeTracking/` の 5 シーン | アイトラッキングが前提（テンプレートでは OFF。視線は MRTK3 版の `MRTK3_EyeAndHand_SampleScene` で確認できます） |
| `Experimental/` の 5 シーン | 実験的な機能 |
| SampleEmptyMRTKScene | 空のシーン（新しいシーンの土台）。手のメニューが無く、ここから先のシーンへ進めなくなるため |

- Unity **6000.3.27f1**（Android Build Support 込み）
- `com.unity.xr.androidxr-openxr` 1.5.1 / `com.unity.xr.openxr` 1.19.0 / `com.google.xr.extensions` v1.5.0 / XR Interaction Toolkit 3.7.0
- MRTK3: Unity レジストリ版（core 3.2.2 ほか）。Samples だけ GitHub Releases の最新安定版（core 3.3.0 ほか）を同梱

## 設定済みの内容

- XR Plug-in Management: Android の OpenXR ローダー、Feature Group「Android XR」、Project Validation の指摘 0 件
- Player Settings: Vulkan / IL2CPP / ARM64 / Min API 25 / GameActivity
- URP: HDR と post-processing を無効
- Unbounded Reference Space は OFF（起動時の正面が +Z になるように）
- Eye Gaze Interaction Profile と Foveated Rendering は OFF（ON にすると、どのシーンでも起動時にアイトラッキングの許可を求められる）。`Tools > Android XR > Enable Eye Tracking` で Eye Gaze を ON にできる
- パススルーのシーン: カメラに AR Camera Manager、背景は Solid Color・アルファ 0、Post Processing は OFF、AR Session を配置
- 空間認識（パススルーのシーン）: Android XR Extensions の Scene Meshing で現実の床・壁・家具をメッシュにする（`Assets/AndroidXR/Scripts/SpatialMeshController.cs`）
  - メッシュは奥行きだけ書くシェーダー（`Android XR/Depth Only Occluder`）で描くので、現実の物の後ろの仮想物が隠れる
  - MeshCollider 付きなので、仮想物が現実の床や壁に当たる（キューブやボールを離すと床に落ちて転がる）
  - `SCENE_UNDERSTANDING_FINE` の許可を得てから動き出す（`EnableAfterPermission`）。物体は床が見つかるまで空中で止まる（`GravityWhenGroundFound`）
  - `SpatialMeshController` の **Show Mesh** を ON にすると、メッシュを半透明で表示できる（確認用）
- MRTK3: Android XR 用のプロファイル（手の関節を XR Hands から取る `UnityHandsSubsystem`）を Android に割り当て
- `Assets/Editor/ManifestDedupe.cs`: `uses-feature android.software.xr.api.spatial` の二重注入を畳む（無いと APK のビルドが失敗する）
- `Assets/Editor/AndroidXRProjectInit.cs`: パッケージ名と製品名を設定する

## 取り込み方

### Install-Templates.ps1（Unity CLI がある場合）

```powershell
./Install-Templates.ps1                          # 3 つすべて
./Install-Templates.ps1 -Template androidxr      # 基本版だけ
```

[Releases](https://github.com/TakahiroMiyaura/AndroidXR-UnityTemplates/releases) から `.tgz` を取得し、`unity templates location` のフォルダに置きます。

### 手動

1. [Releases](https://github.com/TakahiroMiyaura/AndroidXR-UnityTemplates/releases) から `comreseultemplate*.tgz` をダウンロードする
2. テンプレートのフォルダに置く（`unity templates location` で確認できます。Unity Hub の既定は `%USERPROFILE%\Unity user templates`）
3. Unity Hub の新規作成画面に表示されます

## 使い方

```powershell
unity projects create MyXrApp --path D:/WorkSpaces --editor-version 6000.3.27f1 --template com.reseul.template.androidxr
unity run D:/WorkSpaces/MyXrApp -- -buildTarget Android -executeMethod AndroidXRProjectInit.Run -appId com.example.myxrapp -productName MyXrApp
```

2 行目で、ビルドターゲットを Android に切り替え、パッケージ名と製品名を設定します（パッケージ名は Android・Standalone・iOS にそろえて設定）。`-companyName MyCompany` を付けると会社名も設定します。
**テンプレートのままだと全プロジェクトが同じパッケージ名になり、実機で上書きインストールされます。** Unity Hub から作った場合は、Player Settings で変更してください。

Android XR（基本版）では、2 行目で手のゴースト表示に使う XR Hands の HandVisualizer サンプルも取り込みます（ライセンスの都合でテンプレートには入れていません）。
Unity Hub から作った場合は、`Tools > Android XR > Import Hand Visualizer Sample` を一度実行してください。実行するまでは、シーンの手のプレハブが Missing と表示されます。

## 既知の問題と対応

使っているパッケージ側の問題で、そのままではコンパイルできない箇所があります。上流では修正済みでまだリリースされていないため、テンプレートでは同じ内容の修正を先に入れています。修正を含む版に上げたら、テンプレート側の対応は不要になり、外します。

| 問題 | テンプレートでの対応 | 上流の状況 | 対応が不要になる条件 |
|---|---|---|---|
| MRTK3 uxcore の `DialogButton.cs` が、Unity 6.3 の新しいパッチで CS0592 になる（クラスに `[SerializeField]` が付いている）。Android XR ではなく Unity の版による問題で、どのプラットフォームでも起きる | この 1 行を消した uxcore を `Packages/org.mixedrealitytoolkit.uxcore` に埋め込み（変更内容は同じフォルダの `PATCHED.md`） | [MRTK#1101](https://github.com/MixedRealityToolkit/MixedRealityToolkit-Unity/issues/1101) は Close 済みで、main では修正済み。修正を含むパッケージはまだリリースされていない | 修正を含む uxcore がリリースされ、それに上げたとき |

## 既知の制限

- XR Session の Target API Version は 3 に固定しています（manifest の `android.software.xr.api.spatial` の版）。既定の Automatic だと、Android XR Extensions v1.5.0 では 4 になり、Galaxy XR（3）が配布の対象から外れるためです。この値は最低要件なので、3 のままでも 4 以上の端末で動きます。Geospatial もレベル 3 で使えます。レベル 4 が必要な機能を使うときだけ、**Project Settings > XR Plug-in Management > OpenXR** の Android タブで XR Session の設定を開き、Target API Version を変えてください。端末の対応版は `adb shell pm list features` で確認できます。
- Samples の一部の音声ミキサーは Microsoft Spatializer（Windows 用のネイティブプラグイン）を参照しているため、ビルド時に「Effect Microsoft Spatializer Mixer could not be found」が出ます。Android ではこのプラグインの HRTF（上下・前後の方向感）が使えないだけで、音源の位置による聞こえ方の違いや、壁で音が小さくなる処理（AudioOcclusionExample）は動きます。ビルドと実行には影響しません。
- MRTK3 の「見てピンチ（Gaze Pinch）」は、Android XR デバイス（Galaxy XR）では動きません。視線で選ぶ（ハイライト）ことはでき、掴むのは手のレイか直接つかみで行えます。
- Samples の SolverExamples と TapToPlaceExample は、ARMeshManager（部屋のメッシュの表示）を止め、仮想の床と壁（TestDummyWalls）のマテリアルを半透明のものに差し替えています。元のワイヤーフレームのマテリアル（MRTK_Wireframe）はジオメトリシェーダーを使っていて、Android XR（Vulkan のマルチビュー）では左目の映像が崩れるためです。吸着や配置は仮想の床と壁で試せます。
- パススルーのオクルージョンは、空間認識のメッシュで行います（現実の床・壁・家具の後ろの仮想物が隠れる）。手のように動く物では隠れません。AR Foundation の深度によるオクルージョン（AR Occlusion Manager）は、深度を読む専用のシェーダーが必要なので入れていません。

## リポジトリの構成

```text
Templates/AndroidXR/                  テンプレートの元プロジェクト（Assets / Packages / ProjectSettings）
Templates/AndroidXR-MRTK3/
Templates/AndroidXR-MRTK3-Samples/    Packages/MRTK の tgz は git に入れていない。直接開く前に Get-MrtkPackages.ps1 を実行
Install-Templates.ps1                 Releases から取り込む
```

元プロジェクトを Unity で開いて変更し、`unity templates create <path> --name <名前> --keep-project-settings --keep-embedded-packages` で自分用のテンプレートを作ることもできます。

## ライセンス

このリポジトリのスクリプトと設定は [Boost Software License 1.0](LICENSE)。
同梱しているサードパーティのコンポーネント（MRTK3、Mixed Reality Graphics Tools）は、それぞれのライセンスに従います。[THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) を参照してください。
