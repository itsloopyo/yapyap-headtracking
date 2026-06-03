#!/usr/bin/env pwsh
#Requires -Version 5.1
# Populates src/YapyapHeadTracking/libs/ with the reference DLLs needed for a
# local build: Unity engine modules from the game install and BepInEx core DLLs
# from the vendored loader zip. libs/ contents are gitignored (except UnityStubs.cs).

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ProgressPreference    = 'SilentlyContinue'

$scriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir
$libsDir    = Join-Path $projectDir 'src/YapyapHeadTracking/libs'

Import-Module (Join-Path $projectDir 'cameraunlock-core/powershell/GamePathDetection.psm1') -Force

$gamePath = Find-GamePath -GameId 'yapyap'
if (-not $gamePath) {
    throw "YAPYAP install not found. Set YAPYAP_PATH or pass the install dir."
}

$managed = Join-Path $gamePath 'yapyap_Data/Managed'

$unityDlls = @(
    'UnityEngine.dll',
    'UnityEngine.CoreModule.dll',
    'UnityEngine.IMGUIModule.dll',
    'UnityEngine.InputLegacyModule.dll',
    'UnityEngine.TextRenderingModule.dll',
    'UnityEngine.PhysicsModule.dll',
    'UnityEngine.UIModule.dll',
    'UnityEngine.UI.dll'
)

New-Item -ItemType Directory -Path $libsDir -Force | Out-Null

foreach ($dll in $unityDlls) {
    Copy-Item (Join-Path $managed $dll) $libsDir -Force
    Write-Host "  Copied $dll" -ForegroundColor Gray
}

# BepInEx core DLLs: from the game install if BepInEx is already deployed there,
# otherwise from the vendored zip.
$bepinexCore = Join-Path $gamePath 'BepInEx/core'
if (Test-Path (Join-Path $bepinexCore 'BepInEx.dll')) {
    Copy-Item (Join-Path $bepinexCore 'BepInEx.dll') $libsDir -Force
    Copy-Item (Join-Path $bepinexCore '0Harmony.dll') $libsDir -Force
    Write-Host "  Copied BepInEx.dll + 0Harmony.dll from game install" -ForegroundColor Gray
} else {
    $vendorZip = Join-Path $projectDir 'vendor/bepinex/BepInEx_win_x64.zip'
    if (-not (Test-Path $vendorZip)) {
        throw "No BepInEx in game install and no vendored zip at $vendorZip. Run 'pixi run update-deps' first."
    }
    $temp = Join-Path $env:TEMP "yapyap-bepinex-extract-$([guid]::NewGuid().ToString('N'))"
    Expand-Archive -Path $vendorZip -DestinationPath $temp
    Copy-Item (Join-Path $temp 'BepInEx/core/BepInEx.dll') $libsDir -Force
    Copy-Item (Join-Path $temp 'BepInEx/core/0Harmony.dll') $libsDir -Force
    Remove-Item -Recurse -Force $temp
    Write-Host "  Copied BepInEx.dll + 0Harmony.dll from vendored zip" -ForegroundColor Gray
}

Write-Host ""
Write-Host "libs/ populated. 'pixi run build' will now use these references." -ForegroundColor Green
