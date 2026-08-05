<#
.SYNOPSIS
    Turns the [Unreleased] changelog section into a dated version heading.

.DESCRIPTION
    Writing what changed needs a person. Deciding that those changes are called
    2.2.0 and stamping today's date on them does not, so the release workflow does
    it here rather than asking for two hand edits that have to agree.

    Accumulate notes under [Unreleased] as you work. At release time this promotes
    that section to '## [X.Y.Z] - yyyy-MM-dd' and leaves a fresh empty [Unreleased]
    above it. The version comes either from -Version or from bumping whatever
    CrowFX/package.json currently holds.
#>
param(
    # patch, minor, or major. Ignored when -Version is given.
    [ValidateSet("patch", "minor", "major")]
    [string]$Bump = "minor",

    # Exact version, e.g. 2.2.0. Overrides -Bump.
    [string]$Version,

    [string]$ChangelogPath = "CHANGELOG.md",
    [string]$PackageJsonPath = "CrowFX/package.json",

    [switch]$EmitGitHubOutput
)

$ErrorActionPreference = "Stop"

function Resolve-RepositoryRoot {
    $scriptDir = Split-Path -Parent $PSCommandPath
    return (Resolve-Path (Join-Path $scriptDir "../..")).Path
}

$repoRoot = Resolve-RepositoryRoot
$changelogFull = Join-Path $repoRoot $ChangelogPath
$packageFull = Join-Path $repoRoot $PackageJsonPath

foreach ($path in @($changelogFull, $packageFull)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Could not find '$path'."
    }
}

# ---------------------------------------------------------------------------
# Work out the version being released.
# ---------------------------------------------------------------------------
if ([string]::IsNullOrWhiteSpace($Version)) {
    $currentRaw = (Get-Content -LiteralPath $packageFull -Raw | ConvertFrom-Json).version

    if ($currentRaw -notmatch '^(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)') {
        throw "package.json version '$currentRaw' is not a plain x.y.z version, so it cannot be bumped automatically. Pass -Version instead."
    }

    $major = [int]$Matches['major']
    $minor = [int]$Matches['minor']
    $patch = [int]$Matches['patch']

    switch ($Bump) {
        "major" { $major++; $minor = 0; $patch = 0 }
        "minor" { $minor++; $patch = 0 }
        "patch" { $patch++ }
    }

    $newVersion = "$major.$minor.$patch"
    Write-Host "Bumping $currentRaw -> $newVersion ($Bump)"
}
else {
    $newVersion = $Version.Trim().TrimStart('v', 'V')

    if ($newVersion -notmatch '^\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.\-]+)?$') {
        throw "Version '$Version' is not a semantic version."
    }

    Write-Host "Releasing explicitly requested version $newVersion"
}

# ---------------------------------------------------------------------------
# Promote the [Unreleased] section.
# ---------------------------------------------------------------------------
$text = [System.IO.File]::ReadAllText($changelogFull)
$newline = if ($text.Contains("`r`n")) { "`r`n" } else { "`n" }

$unreleasedPattern = '(?m)^##[ \t]+\[Unreleased\][^\r\n]*'
$match = [regex]::Match($text, $unreleasedPattern)

if (-not $match.Success) {
    throw "No '## [Unreleased]' heading found in $ChangelogPath."
}

if ([regex]::IsMatch($text, "(?m)^##[ \t]+\[$([regex]::Escape($newVersion))\]")) {
    throw "$ChangelogPath already has a [$newVersion] section. Nothing to promote."
}

# Everything between [Unreleased] and the next '## ' heading is what ships. An
# empty section means there is nothing to release, which is worth failing on
# rather than publishing a version with no notes.
$afterHeading = $text.Substring($match.Index + $match.Length)
$nextHeading = [regex]::Match($afterHeading, '(?m)^##[ \t]')
$body = if ($nextHeading.Success) { $afterHeading.Substring(0, $nextHeading.Index) } else { $afterHeading }

if ([string]::IsNullOrWhiteSpace($body)) {
    throw "The [Unreleased] section in $ChangelogPath is empty. Add notes before releasing."
}

$date = (Get-Date).ToString("yyyy-MM-dd")
$replacement = "## [Unreleased]" + $newline + $newline + "## [$newVersion] - $date"

$updated = $text.Remove($match.Index, $match.Length).Insert($match.Index, $replacement)

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($changelogFull, $updated, $utf8NoBom)

Write-Host "Promoted [Unreleased] to [$newVersion] - $date"

if ($EmitGitHubOutput -and $env:GITHUB_OUTPUT) {
    Add-Content -Path $env:GITHUB_OUTPUT -Value "version=$newVersion"
}

return $newVersion
