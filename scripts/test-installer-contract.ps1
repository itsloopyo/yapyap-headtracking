#!/usr/bin/env pwsh
#Requires -Version 5.1
# Contract tests for install.cmd / uninstall.cmd (the Unified Launcher Contract).
#
# Covers:
#   1. Arg-parser exit codes: unknown flags and unrecognised arguments exit 2
#      without touching the system.
#   2. Fresh install -> uninstall round-trip against a sandbox "game" directory:
#      BepInEx extraction, plugin deploy, state-file attribution, and vanilla
#      restore on uninstall.
#   3. Loader attribution safety: installing over a user's pre-existing BepInEx
#      records installed_by_us=false, uninstall leaves the loader intact, and
#      /force removes it.
#
# Everything runs against staged copies in %TEMP%; the dev tree and the real
# game install are never modified. Requires a prior Release build
# (pixi run build) for the plugin DLLs.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir

$buildOutput = Join-Path $projectDir 'src\YapyapHeadTracking\bin\Release\net48'
$modDlls = @('YapyapHeadTracking.dll', 'CameraUnlock.Core.dll', 'CameraUnlock.Core.Unity.dll')
foreach ($dll in $modDlls) {
    if (-not (Test-Path (Join-Path $buildOutput $dll))) {
        throw "Missing build output $dll in $buildOutput. Run 'pixi run build' first."
    }
}

$script:failures = @()
$script:testCount = 0

function Assert-True {
    param([bool]$Condition, [string]$Message)
    $script:testCount++
    if ($Condition) {
        Write-Host "  PASS: $Message" -ForegroundColor Green
    } else {
        Write-Host "  FAIL: $Message" -ForegroundColor Red
        $script:failures += $Message
    }
}

# Runs a .cmd with stdin redirected from NUL so a stray `pause` can never hang
# the test run. Returns the process exit code.
function Invoke-CmdScript {
    param([string]$ScriptPath, [string[]]$Arguments)
    $quoted = @()
    foreach ($a in $Arguments) { $quoted += ('"' + $a + '"') }
    $line = '"' + $ScriptPath + '" ' + ($quoted -join ' ') + ' < nul'
    cmd.exe /d /s /c " $line " | Out-Null
    return $LASTEXITCODE
}

$root = Join-Path $env:TEMP "yapyap-installer-test-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $root -Force | Out-Null

try {
    # ---- Stage a release-ZIP-shaped layout (mirrors Copy-SharedBundle output) ----
    $staging = Join-Path $root 'installer'
    New-Item -ItemType Directory -Path (Join-Path $staging 'plugins') -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $staging 'shared') -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $staging 'vendor\bepinex') -Force | Out-Null

    Copy-Item (Join-Path $scriptDir 'install.cmd') $staging
    Copy-Item (Join-Path $scriptDir 'uninstall.cmd') $staging
    foreach ($dll in $modDlls) {
        Copy-Item (Join-Path $buildOutput $dll) (Join-Path $staging 'plugins')
    }
    Copy-Item (Join-Path $projectDir 'vendor\bepinex\BepInEx_win_x64.zip') (Join-Path $staging 'vendor\bepinex')
    Copy-Item (Join-Path $projectDir 'cameraunlock-core\scripts\find-game.ps1') (Join-Path $staging 'shared')
    Copy-Item (Join-Path $projectDir 'cameraunlock-core\powershell\GamePathDetection.psm1') (Join-Path $staging 'shared')
    Copy-Item (Join-Path $projectDir 'cameraunlock-core\data\games.json') (Join-Path $staging 'shared')

    $installCmd = Join-Path $staging 'install.cmd'
    $uninstallCmd = Join-Path $staging 'uninstall.cmd'
    $stateFileName = '.headtracking-state.json'

    # ============================================================
    Write-Host ""
    Write-Host "=== 1. Arg-parser contract (exit 2, no side effects) ===" -ForegroundColor Cyan

    foreach ($case in @(
        @{ Script = $installCmd;   Args = @('/y', '/badflag');  Name = 'install.cmd /y /badflag' }
        @{ Script = $installCmd;   Args = @('/y', '--badflag'); Name = 'install.cmd /y --badflag' }
        @{ Script = $installCmd;   Args = @('/y', '-badflag');  Name = 'install.cmd /y -badflag' }
        @{ Script = $installCmd;   Args = @('/y', 'C:\nonexistent\path\xyz123'); Name = 'install.cmd /y <nonexistent path>' }
        @{ Script = $uninstallCmd; Args = @('/y', '/badflag');  Name = 'uninstall.cmd /y /badflag' }
        @{ Script = $uninstallCmd; Args = @('/y', '--badflag'); Name = 'uninstall.cmd /y --badflag' }
    )) {
        $ec = Invoke-CmdScript -ScriptPath $case.Script -Arguments $case.Args
        Assert-True ($ec -eq 2) "$($case.Name) exits 2 (got $ec)"
    }

    # ============================================================
    Write-Host ""
    Write-Host "=== 2. Fresh install -> uninstall round-trip ===" -ForegroundColor Cyan

    $game = Join-Path $root 'game-fresh'
    New-Item -ItemType Directory -Path $game -Force | Out-Null

    $ec = Invoke-CmdScript -ScriptPath $installCmd -Arguments @($game, '/y')
    Assert-True ($ec -eq 0) "fresh install exits 0 (got $ec)"
    Assert-True (Test-Path (Join-Path $game 'BepInEx\core\BepInEx.dll')) "BepInEx loader extracted"
    Assert-True (Test-Path (Join-Path $game 'winhttp.dll')) "doorstop winhttp.dll extracted"
    foreach ($dll in $modDlls) {
        Assert-True (Test-Path (Join-Path $game "BepInEx\plugins\$dll")) "plugin deployed: $dll"
    }

    $statePath = Join-Path $game $stateFileName
    Assert-True (Test-Path $statePath) "state file written"
    $state = Get-Content $statePath -Raw | ConvertFrom-Json
    Assert-True ($state.framework.installed_by_us -eq $true) "state records installed_by_us=true (we installed the loader)"
    Assert-True ($state.mod.name -eq 'YapyapHeadTracking') "state records mod name"

    # Re-install over our own install: must stay attributed to us, not demote to false.
    $ec = Invoke-CmdScript -ScriptPath $installCmd -Arguments @($game, '/y')
    Assert-True ($ec -eq 0) "re-install exits 0 (got $ec)"
    $state = Get-Content $statePath -Raw | ConvertFrom-Json
    Assert-True ($state.framework.installed_by_us -eq $true) "re-install preserves installed_by_us=true"

    $ec = Invoke-CmdScript -ScriptPath $uninstallCmd -Arguments @($game, '/y')
    Assert-True ($ec -eq 0) "uninstall exits 0 (got $ec)"
    $leftovers = @(Get-ChildItem -Path $game -Force -Recurse | Select-Object -ExpandProperty FullName)
    Assert-True ($leftovers.Count -eq 0) "game directory restored to vanilla (leftovers: $($leftovers -join ', '))"

    # Uninstalling twice is a no-op, not an error.
    $ec = Invoke-CmdScript -ScriptPath $uninstallCmd -Arguments @($game, '/y')
    Assert-True ($ec -eq 0) "second uninstall is a clean no-op (got $ec)"

    # ============================================================
    Write-Host ""
    Write-Host "=== 3. Pre-existing loader attribution safety ===" -ForegroundColor Cyan

    $game2 = Join-Path $root 'game-existing-loader'
    New-Item -ItemType Directory -Path $game2 -Force | Out-Null
    # Simulate the user's own BepInEx install before our mod arrives.
    & "$env:SystemRoot\System32\tar.exe" -xf (Join-Path $projectDir 'vendor\bepinex\BepInEx_win_x64.zip') -C $game2
    if ($LASTEXITCODE -ne 0) { throw "test setup: failed to pre-extract BepInEx into $game2" }
    New-Item -ItemType Directory -Path (Join-Path $game2 'BepInEx\plugins') -Force | Out-Null
    $userPluginPath = Join-Path $game2 'BepInEx\plugins\SomeOtherUsersMod.dll'
    Set-Content -Path $userPluginPath -Value 'not really a dll' -Encoding ASCII

    $ec = Invoke-CmdScript -ScriptPath $installCmd -Arguments @($game2, '/y')
    Assert-True ($ec -eq 0) "install over existing loader exits 0 (got $ec)"
    $state2 = Get-Content (Join-Path $game2 $stateFileName) -Raw | ConvertFrom-Json
    Assert-True ($state2.framework.installed_by_us -eq $false) "state records installed_by_us=false (loader was the user's)"

    $ec = Invoke-CmdScript -ScriptPath $uninstallCmd -Arguments @($game2, '/y')
    Assert-True ($ec -eq 0) "uninstall exits 0 (got $ec)"
    Assert-True (Test-Path (Join-Path $game2 'BepInEx\core\BepInEx.dll')) "user's BepInEx left intact without /force"
    Assert-True (Test-Path $userPluginPath) "user's other plugin left intact"
    foreach ($dll in $modDlls) {
        Assert-True (-not (Test-Path (Join-Path $game2 "BepInEx\plugins\$dll"))) "our plugin removed: $dll"
    }
    Assert-True (-not (Test-Path (Join-Path $game2 $stateFileName))) "state file removed"

    # /force removes the loader even when it wasn't ours.
    $ec = Invoke-CmdScript -ScriptPath $uninstallCmd -Arguments @($game2, '/y', '/force')
    Assert-True ($ec -eq 0) "forced uninstall exits 0 (got $ec)"
    Assert-True (-not (Test-Path (Join-Path $game2 'BepInEx'))) "/force removes the loader"
} finally {
    if (Test-Path $root) {
        Remove-Item -Recurse -Force $root
    }
}

Write-Host ""
if ($script:failures.Count -gt 0) {
    Write-Host "$($script:failures.Count) of $script:testCount assertions FAILED:" -ForegroundColor Red
    $script:failures | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}
Write-Host "All $script:testCount installer contract assertions passed" -ForegroundColor Green
exit 0
