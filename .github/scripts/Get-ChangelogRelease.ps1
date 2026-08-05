<#
.SYNOPSIS
    Reads the version and release notes for a release out of CHANGELOG.md.

.DESCRIPTION
    CHANGELOG.md is the single place a version number is typed by hand. This script
    reads the topmost versioned heading (skipping [Unreleased]) and returns both the
    version and that section's body, so the release workflow never needs the version
    passed to it separately and cannot disagree with the changelog.

    Headings look like:  ## [2.1.0] - 2026-08-04
#>
param(
    [string]$ChangelogPath = "CHANGELOG.md",

    # Optional. Pass to release a version other than the topmost one; it must still
    # exist in the changelog, so a typo fails here rather than producing a release
    # with empty notes.
    [string]$Version,

    # When set, writes 'version' and 'notes' to the GitHub Actions output file.
    [switch]$EmitGitHubOutput
)

$ErrorActionPreference = "Stop"

function Resolve-RepositoryRoot {
    $scriptDir = Split-Path -Parent $PSCommandPath
    return (Resolve-Path (Join-Path $scriptDir "../..")).Path
}

$repoRoot = Resolve-RepositoryRoot
$fullPath = Join-Path $repoRoot $ChangelogPath

if (-not (Test-Path -LiteralPath $fullPath)) {
    throw "Could not find a changelog at '$fullPath'."
}

$lines = [System.IO.File]::ReadAllLines($fullPath)

# Collect every versioned section: its version, the line it starts on, and where it ends.
$headingPattern = '^##\s+\[(?<version>\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.\-]+)?)\]'
$sections = @()

for ($i = 0; $i -lt $lines.Length; $i++) {
    $match = [regex]::Match($lines[$i], $headingPattern)
    if (-not $match.Success) { continue }

    $sections += [pscustomobject]@{
        Version   = $match.Groups['version'].Value
        BodyStart = $i + 1
        BodyEnd   = $lines.Length - 1
    }
}

if ($sections.Count -eq 0) {
    throw "No versioned '## [x.y.z]' heading was found in $ChangelogPath."
}

# Each section body runs until the next '## ' heading of any kind, so an [Unreleased]
# block sitting above a release does not get swallowed into it.
for ($s = 0; $s -lt $sections.Count; $s++) {
    for ($i = $sections[$s].BodyStart; $i -lt $lines.Length; $i++) {
        if ($lines[$i] -match '^##\s') {
            $sections[$s].BodyEnd = $i - 1
            break
        }
    }
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    $selected = $sections[0]
}
else {
    $wanted = $Version.Trim().TrimStart('v', 'V')
    $selected = $sections | Where-Object { $_.Version -eq $wanted } | Select-Object -First 1

    if ($null -eq $selected) {
        throw "Version '$Version' has no section in $ChangelogPath. Add its heading before releasing."
    }
}

$body = @()
for ($i = $selected.BodyStart; $i -le $selected.BodyEnd; $i++) {
    $body += $lines[$i]
}

$notes = ($body -join "`n").Trim()

if ([string]::IsNullOrWhiteSpace($notes)) {
    throw "The [$($selected.Version)] section in $ChangelogPath is empty. A release needs notes."
}

Write-Host "Releasing version $($selected.Version) from $ChangelogPath"

if ($EmitGitHubOutput -and $env:GITHUB_OUTPUT) {
    # Multi-line values need a heredoc-style delimiter that cannot occur in the body.
    $delimiter = "CROWFX_NOTES_$([guid]::NewGuid().ToString('N'))"

    Add-Content -Path $env:GITHUB_OUTPUT -Value "version=$($selected.Version)"
    Add-Content -Path $env:GITHUB_OUTPUT -Value "notes<<$delimiter"
    Add-Content -Path $env:GITHUB_OUTPUT -Value $notes
    Add-Content -Path $env:GITHUB_OUTPUT -Value $delimiter
}

return [pscustomobject]@{
    Version = $selected.Version
    Notes   = $notes
}
