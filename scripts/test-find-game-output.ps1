#!/usr/bin/env pwsh
#Requires -Version 5.1

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir
$findGame = Join-Path $projectDir 'cameraunlock-core/scripts/find-game.ps1'

$root = Join-Path $env:TEMP "cul-find-game-test-$([guid]::NewGuid().ToString('N'))"
$outFile = Join-Path $root 'resolved.cmd'
$echoFile = Join-Path $root 'echo.txt'

New-Item -ItemType Directory -Path $root -Force | Out-Null

# Reads find-game.ps1's output as the shared install bodies do: `call` with delayed expansion
# off, then `!GAME_PATH!` with it on, which hands the value back without cmd.exe parsing it.
$verify = Join-Path $root 'verify.cmd'
$cmd = @"
@echo off
setlocal disabledelayedexpansion
call "$outFile"
setlocal enabledelayedexpansion
>"$echoFile" echo(!GAME_PATH!
exit /b 0
"@
[System.IO.File]::WriteAllText($verify, $cmd, [System.Text.Encoding]::ASCII)

try {
    foreach ($leaf in @('YAPYAP & Percent %Literal%', 'Bang !Path! ^caret')) {
        $gamePath = Join-Path $root $leaf
        New-Item -ItemType Directory -Path $gamePath -Force | Out-Null

        & $findGame -GameId 'yapyap' -GivenPath $gamePath -OutFile $outFile
        if ($LASTEXITCODE -ne 0) {
            throw "find-game.ps1 returned exit code $LASTEXITCODE for $gamePath"
        }

        & cmd /d /c $verify
        if ($LASTEXITCODE -ne 0) {
            throw "Reading the generated batch output failed for $gamePath. cmd exit code: $LASTEXITCODE"
        }
        $read = [System.IO.File]::ReadAllText($echoFile, [System.Text.Encoding]::ASCII).TrimEnd("`r", "`n")
        if ($read -ne $gamePath) {
            throw "Generated batch output did not round-trip: wrote $gamePath, read back $read"
        }
    }
} finally {
    if (Test-Path $root) {
        Remove-Item -Recurse -Force $root
    }
}

Write-Host "find-game batch escaping tests passed"
