#!/usr/bin/env pwsh
#Requires -Version 5.1
param(
    [Parameter(Position=0)]
    [string]$Version = "",
    [switch]$AllowDirty
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir
$csprojPath = Join-Path $projectDir "src\YapyapHeadTracking\YapyapHeadTracking.csproj"
$pluginPath = Join-Path $projectDir "src\YapyapHeadTracking\Core\HeadTrackingPlugin.cs"
$changelogPath = Join-Path $projectDir "CHANGELOG.md"

Import-Module (Join-Path $projectDir "cameraunlock-core\powershell\ReleaseWorkflow.psm1") -Force

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

    Set-CsprojVersion $csprojPath $Version

    $pluginContent = Get-Content $pluginPath -Raw
    if ($pluginContent -notmatch 'PluginVersion = "[^"]+"') {
        Exit-WithError "PluginVersion constant not found in $pluginPath."
    }
    $pluginContent = $pluginContent -replace 'PluginVersion = "[^"]+"', "PluginVersion = `"$Version`""
    $pluginContent | Set-Content $pluginPath -NoNewline

    & pixi run build
    if ($LASTEXITCODE -ne 0) {
        Exit-WithError "pixi run build failed."
    }

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

    & git add $csprojPath $pluginPath $changelogPath
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
