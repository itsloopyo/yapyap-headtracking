#!/usr/bin/env pwsh
#Requires -Version 5.1
param(
    [Parameter(Position=0)]
    [string]$Version = "",
    # Ship a release even when there are no user-facing commits since the
    # last tag (writes a maintenance changelog entry instead of aborting).
    [switch]$Force,
    [switch]$AllowDirty
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# THIRD-PARTY-NOTICES.md names the cameraunlock-core commit compiled into the
# release ZIPs, and bumping the submodule does not touch it. Packaging refuses
# to ship that mismatch, so a bump with no notices edit stopped the release
# here, or in CI once the tag had already been pushed. Re-sync it and let this
# release carry the correction.
$noticesRoot = Split-Path -Parent $PSScriptRoot
& git -C $noticesRoot diff --quiet -- THIRD-PARTY-NOTICES.md
if ($LASTEXITCODE -ne 0) { throw "THIRD-PARTY-NOTICES.md has uncommitted edits. Commit or discard them, then re-run." }
& (Join-Path $noticesRoot 'cameraunlock-core\scripts\sync-core-notices.ps1') -Repo $noticesRoot
if ($LASTEXITCODE -ne 0) { throw "sync-core-notices.ps1 exited $LASTEXITCODE - fix THIRD-PARTY-NOTICES.md before releasing." }
& git -C $noticesRoot diff --quiet -- THIRD-PARTY-NOTICES.md
if ($LASTEXITCODE -ne 0) {
    & git -C $noticesRoot commit -q -m 'chore: record the cameraunlock-core commit this build compiles' -- THIRD-PARTY-NOTICES.md
    if ($LASTEXITCODE -ne 0) { throw "Could not commit the re-synced THIRD-PARTY-NOTICES.md." }
    Write-Host 'THIRD-PARTY-NOTICES.md re-synced to the pinned cameraunlock-core commit.' -ForegroundColor Yellow
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir
$csprojPath = Join-Path $projectDir "src\YapyapHeadTracking\YapyapHeadTracking.csproj"
$pluginPath = Join-Path $projectDir "src\YapyapHeadTracking\Core\HeadTrackingPlugin.cs"
$changelogPath = Join-Path $projectDir "CHANGELOG.md"
$installCmdPath = Join-Path $projectDir "scripts\install.cmd"

Import-Module (Join-Path $projectDir "cameraunlock-core\powershell\ReleaseWorkflow.psm1") -Force

# Mirrors New-ChangelogFromCommits' insertion so a -Force maintenance entry
# lands in the same place with the same shape.
function Add-MaintenanceChangelogEntry {
    param([string]$Path, [string]$NewVersion)
    $date = Get-Date -Format 'yyyy-MM-dd'
    $entry = "## [$NewVersion] - $date`n`n### Changed`n`n- Maintenance release (no user-facing changes).`n`n"
    $changelog = Get-Content $Path -Raw
    if ($changelog -match '(?s)(# Changelog.*?)(## \[)') {
        $changelog = $changelog -replace '(?s)(# Changelog.*?\n\n)', "`$1$entry"
    } else {
        $changelog = $changelog -replace '(?s)(# Changelog.*?\n)', "`$1$entry"
    }
    $changelog = $changelog.TrimEnd() + "`n"
    Set-Content $Path $changelog -NoNewline
}

function Exit-WithError {
    param([Parameter(Mandatory=$true)][string]$Message)
    Write-Host "Error: $Message" -ForegroundColor Red
    exit 1
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    Write-Host "Current version: $(Get-CsprojVersion $csprojPath)"
    Exit-WithError "Usage: pixi run release <major|minor|patch|nightly|X.Y.Z>"
}

if ($Version -eq 'nightly') {
    & (Join-Path $PSScriptRoot 'release-nightly.ps1') -AllowDirty:$AllowDirty
    exit $LASTEXITCODE
}

Push-Location $projectDir
try {
    $currentVersion = Get-CsprojVersion $csprojPath
    try {
        $Version = Resolve-ReleaseVersion -Argument $Version -CurrentVersion $currentVersion
    } catch {
        Exit-WithError $_.Exception.Message
    }

    if ($Version -notmatch '^\d+\.\d+\.\d+$') {
        Exit-WithError "Resolved version '$Version' is not a release semver X.Y.Z."
    }

    $currentBranch = (& git rev-parse --abbrev-ref HEAD).Trim()
    if ($LASTEXITCODE -ne 0) {
        Exit-WithError "Could not determine current git branch."
    }
    if ($currentBranch -ne "main") {
        Exit-WithError "Must be on 'main' branch to release (currently on '$currentBranch')."
    }

    if (-not (Test-CleanGitStatus)) {
        Exit-WithError "Working directory has uncommitted changes."
    }

    $tagName = "v$Version"
    if (Test-GitTagExists $tagName) {
        Exit-WithError "Tag '$tagName' already exists."
    }

    # Generate CHANGELOG from commits since last tag. This is the gate that
    # aborts when there are no user-facing commits, so run it BEFORE mutating
    # any version files - a failure here then leaves a clean tree instead of
    # stranding a half-applied version bump with no tag.
    Write-Host "Generating CHANGELOG..." -ForegroundColor Cyan
    $hasExistingTags = git tag -l 2>$null
    if (-not $hasExistingTags) {
        # First release - ensure a baseline CHANGELOG exists
        if (-not (Test-Path $changelogPath)) {
            $date = Get-Date -Format 'yyyy-MM-dd'
            "# Changelog`n`n## [$Version] - $date`n`nFirst release.`n" | Set-Content $changelogPath
            Write-Host "  Wrote initial CHANGELOG.md" -ForegroundColor Gray
        }
    } else {
        try {
            $changelogArgs = @{
                ChangelogPath = $changelogPath
                Version = $Version
                ArtifactPaths = @(
                    "src/YapyapHeadTracking/",
                    "cameraunlock-core",
                    "scripts/install.cmd",
                    "scripts/uninstall.cmd"
                )
            }
            New-ChangelogFromCommits @changelogArgs | Out-Null
        } catch {
            if (-not $Force) {
                Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
                Write-Host "No user-facing changes to release. Re-run with -Force for a maintenance release." -ForegroundColor Yellow
                exit 1
            }
            Write-Host "No user-facing commits since last tag - writing maintenance entry (-Force)." -ForegroundColor Yellow
            Add-MaintenanceChangelogEntry -Path $changelogPath -NewVersion $Version
        }
    }

    Set-CsprojVersion $csprojPath $Version

    $pluginContent = Get-Content $pluginPath -Raw
    if ($pluginContent -notmatch 'PluginVersion = "[^"]+"') {
        Exit-WithError "PluginVersion constant not found in $pluginPath."
    }
    $pluginContent = $pluginContent -replace 'PluginVersion = "[^"]+"', "PluginVersion = `"$Version`""
    $pluginContent | Set-Content $pluginPath -NoNewline

    # install.cmd's MOD_VERSION is what the install writes into the launcher's
    # state file, which is where the launcher looks to spot a stale install.
    $installCmdContent = Get-Content $installCmdPath -Raw
    if ($installCmdContent -notmatch 'set "MOD_VERSION=[^"]+"') {
        Exit-WithError "MOD_VERSION line not found in $installCmdPath."
    }
    $installCmdContent = $installCmdContent -replace 'set "MOD_VERSION=[^"]+"', "set `"MOD_VERSION=$Version`""
    $installCmdContent | Set-Content $installCmdPath -NoNewline

    & pixi run build
    if ($LASTEXITCODE -ne 0) {
        Exit-WithError "pixi run build failed."
    }

    & git add $csprojPath $pluginPath $installCmdPath $changelogPath
    if ($LASTEXITCODE -ne 0) {
        Exit-WithError "git add failed."
    }

    # Commit message must stay 'Release v<version>': build.yml skips its job for
    # commits with this prefix and release notes filter them as noise.
    & git commit -m "Release v$Version"
    if ($LASTEXITCODE -ne 0) {
        Exit-WithError "git commit failed."
    }

    New-ReleaseTag -Version $Version -Message "Release $tagName"

    Write-Host "Release $tagName pushed." -ForegroundColor Green
} finally {
    Pop-Location
}
