<#
.SYNOPSIS
  GitHub Releases から Android XR 向け Unity テンプレートを取得し、Unity Hub / Unity CLI のテンプレートフォルダに置く。
.DESCRIPTION
  置き場所は `unity templates location` の結果（Unity CLI が無い場合は -Destination で指定）。
  置いたあとは Unity Hub の新規作成画面、または `unity templates list --editor <version>` に表示される。
.EXAMPLE
  ./Install-Templates.ps1                         # 最新リリースの 3 テンプレートをすべて
  ./Install-Templates.ps1 -Template androidxr     # 基本版だけ
  ./Install-Templates.ps1 -Tag v2.3.0 -Destination "$HOME/Unity user templates"
#>
param(
    [ValidateSet('androidxr', 'androidxr-mrtk3', 'androidxr-mrtk3-samples')]
    [string[]] $Template = @('androidxr', 'androidxr-mrtk3', 'androidxr-mrtk3-samples'),
    [string] $Tag = 'latest',
    [string] $Destination,
    [string] $Repository = 'TakahiroMiyaura/AndroidXR-UnityTemplates'
)
$ErrorActionPreference = 'Stop'

if (-not $Destination) {
    if (-not (Get-Command unity -ErrorAction SilentlyContinue)) {
        throw 'Unity CLI (unity) が見つかりません。-Destination でテンプレートフォルダを指定してください（Unity Hub の Preferences > Projects で確認できます）。'
    }
    $Destination = (unity templates location --no-banner | Select-Object -Last 1).Trim()
}
New-Item -ItemType Directory -Force $Destination | Out-Null

$api = if ($Tag -eq 'latest') { "https://api.github.com/repos/$Repository/releases/latest" } else { "https://api.github.com/repos/$Repository/releases/tags/$Tag" }
$release = Invoke-RestMethod $api -Headers @{ 'User-Agent' = 'Install-Templates' }
Write-Host "release: $($release.tag_name)"

foreach ($t in $Template) {
    $file = 'comreseultemplate' + $t.Replace('-', '') + '.tgz'
    $asset = $release.assets | Where-Object name -eq $file
    if (-not $asset) { throw "$file is not in release $($release.tag_name)" }
    $dir = Join-Path $Destination ([IO.Path]::GetFileNameWithoutExtension($file))
    New-Item -ItemType Directory -Force $dir | Out-Null
    $out = Join-Path $dir $file
    Write-Host "download $file ($([math]::Round($asset.size / 1MB, 1)) MB) -> $dir"
    Invoke-WebRequest $asset.browser_download_url -OutFile $out
}

if (Get-Command unity -ErrorAction SilentlyContinue) {
    Write-Host ''
    unity templates list --editor (Get-Content (Join-Path $PSScriptRoot 'EDITOR_VERSION') -ErrorAction SilentlyContinue | Select-Object -First 1) --no-banner 2>$null |
        Select-String 'reseul' | ForEach-Object Line
}
