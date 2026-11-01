# このプロジェクトを Unity で直接開く前に一度実行する。MRTK3 の tgz を GitHub Releases から Packages/MRTK に取得する。
# （Releases のテンプレート .tgz には同梱済みなので、テンプレートから作る場合は不要）
$ErrorActionPreference = 'Stop'
$dir = Join-Path $PSScriptRoot 'Packages\MRTK'
New-Item -ItemType Directory -Force $dir | Out-Null
foreach ($url in @(
    'https://github.com/microsoft/MixedReality-GraphicsTools-Unity/releases/download/v0.8.1/com.microsoft.mrtk.graphicstools.unity-0.8.1.tgz',
    'https://github.com/MixedRealityToolkit/MixedRealityToolkit-Unity/releases/download/core-v3.3.0/org.mixedrealitytoolkit.core-3.3.0.tgz',
    'https://github.com/MixedRealityToolkit/MixedRealityToolkit-Unity/releases/download/core-v3.3.0/org.mixedrealitytoolkit.input-3.3.0.tgz',
    'https://github.com/MixedRealityToolkit/MixedRealityToolkit-Unity/releases/download/core-v3.3.0/org.mixedrealitytoolkit.spatialmanipulation-3.4.0.tgz',
    'https://github.com/MixedRealityToolkit/MixedRealityToolkit-Unity/releases/download/core-v3.3.0/org.mixedrealitytoolkit.uxcomponents-3.4.0.tgz',
    'https://github.com/MixedRealityToolkit/MixedRealityToolkit-Unity/releases/download/core-v3.3.0/org.mixedrealitytoolkit.uxcomponents.noncanvas-3.1.5.tgz',
    'https://github.com/MixedRealityToolkit/MixedRealityToolkit-Unity/releases/download/core-v3.3.0/org.mixedrealitytoolkit.standardassets-3.2.1.tgz',
    'https://github.com/MixedRealityToolkit/MixedRealityToolkit-Unity/releases/download/core-v3.3.0/org.mixedrealitytoolkit.extendedassets-3.0.4.tgz',
    'https://github.com/MixedRealityToolkit/MixedRealityToolkit-Unity/releases/download/core-v3.3.0/org.mixedrealitytoolkit.audio-3.0.5.tgz',
    'https://github.com/MixedRealityToolkit/MixedRealityToolkit-Unity/releases/download/core-v3.3.0/org.mixedrealitytoolkit.tools-3.0.5.tgz',
    'https://github.com/MixedRealityToolkit/MixedRealityToolkit-Unity/releases/download/core-v3.3.0/org.mixedrealitytoolkit.diagnostics-3.0.3.tgz',
    'https://github.com/MixedRealityToolkit/MixedRealityToolkit-Unity/releases/download/core-v3.3.0/org.mixedrealitytoolkit.windowsspeech-3.0.4.tgz',
    'https://github.com/MixedRealityToolkit/MixedRealityToolkit-Unity/releases/download/uxcore-v3.2.1/org.mixedrealitytoolkit.data-1.0.3-pre.20.tgz',
    'https://github.com/MixedRealityToolkit/MixedRealityToolkit-Unity/releases/download/uxcore-v3.2.1/org.mixedrealitytoolkit.accessibility-1.0.3-pre.20.tgz'
)) {
    $out = Join-Path $dir (Split-Path $url -Leaf)
    if (-not (Test-Path $out)) { Write-Host "download $(Split-Path $url -Leaf)"; Invoke-WebRequest $url -OutFile $out }
}
