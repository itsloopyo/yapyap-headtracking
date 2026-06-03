#!/usr/bin/env pwsh
#Requires -Version 5.1
# Thin wrapper: calls shared packaging script with YAPYAP values.

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir

& "$projectDir/cameraunlock-core/scripts/package-bepinex-mod.ps1" `
    -ModName "YapyapHeadTracking" `
    -CsprojPath "src/YapyapHeadTracking/YapyapHeadTracking.csproj" `
    -BuildOutputDir "src/YapyapHeadTracking/bin/Release/net48" `
    -ModDlls @("YapyapHeadTracking.dll","CameraUnlock.Core.dll","CameraUnlock.Core.Unity.dll") `
    -ProjectRoot $projectDir `
    -CreateNexusZip
