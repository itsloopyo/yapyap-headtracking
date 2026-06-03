#!/usr/bin/env pwsh
#Requires -Version 5.1

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir
$findGame = Join-Path $projectDir 'cameraunlock-core/scripts/find-game.ps1'

$root = Join-Path $env:TEMP "cul-find-game-test-$([guid]::NewGuid().ToString('N'))"
$gamePath = Join-Path $root 'YAPYAP & Percent %Literal%'
$outFile = Join-Path $root 'resolved.cmd'

New-Item -ItemType Directory -Path $gamePath -Force | Out-Null

try {
    & $findGame -GameId 'yapyap' -GivenPath $gamePath -OutFile $outFile
    if ($LASTEXITCODE -ne 0) {
        throw "find-game.ps1 returned exit code $LASTEXITCODE"
    }

    $gamePathForBatch = $gamePath -replace '%', '%%'
    $cmd = @"
@echo off
setlocal enabledelayedexpansion
call "$outFile"
if not "!GAME_PATH!"=="$gamePathForBatch" exit /b 10
echo Game found: "!GAME_PATH!" > "$root\echo.txt"
exit /b 0
"@
    $verify = Join-Path $root 'verify.cmd'
    [System.IO.File]::WriteAllText($verify, $cmd, [System.Text.Encoding]::ASCII)
    & cmd /c $verify
    if ($LASTEXITCODE -ne 0) {
        throw "Generated batch output did not round-trip safely. cmd exit code: $LASTEXITCODE"
    }

    $unsafePath = Join-Path $root 'Bang !Path!'
    New-Item -ItemType Directory -Path $unsafePath -Force | Out-Null
    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    & powershell -ExecutionPolicy Bypass -File $findGame -GameId 'yapyap' -GivenPath $unsafePath -OutFile $outFile 2>$null
    $unsafeExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference
    if ($unsafeExitCode -eq 0) {
        throw "find-game.ps1 accepted a path that cannot survive delayed expansion"
    }
} finally {
    if (Test-Path $root) {
        Remove-Item -Recurse -Force $root
    }
}

Write-Host "find-game batch escaping tests passed"
