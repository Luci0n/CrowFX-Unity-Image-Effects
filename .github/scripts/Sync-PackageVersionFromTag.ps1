param(
    [string]$Tag,
    [string]$PackageJsonPath = "CrowFX/package.json"
)

$ErrorActionPreference = "Stop"

function Normalize-VersionTag {
    param([string]$RawTag)

    if ([string]::IsNullOrWhiteSpace($RawTag)) {
        throw "A tag value is required. Pass -Tag v1.1.0 or run from an exact tagged commit."
    }

    $value = $RawTag.Trim()

    if ($value.StartsWith("refs/tags/", [System.StringComparison]::OrdinalIgnoreCase)) {
        $value = $value.Substring("refs/tags/".Length)
    }

    if ($value.StartsWith("v", [System.StringComparison]::OrdinalIgnoreCase)) {
        $value = $value.Substring(1)
    }

    if ($value -notmatch '^\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.\-]+)?$') {
        throw "Tag '$RawTag' does not look like a semantic version. Expected something like v1.1.0."
    }

    return $value
}

function Resolve-RepositoryRoot {
    $scriptDir = Split-Path -Parent $PSCommandPath
    return (Resolve-Path (Join-Path $scriptDir "..\\..")).Path
}

function Get-ExactTagFromGit {
    param([string]$RepositoryRoot)

    $resolved = git -C $RepositoryRoot describe --tags --exact-match 2>$null
    if ($LASTEXITCODE -ne 0) {
        return $null
    }

    return ($resolved | Select-Object -First 1)
}

$repoRoot = Resolve-RepositoryRoot
if ([string]::IsNullOrWhiteSpace($Tag)) {
    $Tag = Get-ExactTagFromGit -RepositoryRoot $repoRoot
}

$version = Normalize-VersionTag -RawTag $Tag
$packageJsonFullPath = Join-Path $repoRoot $PackageJsonPath

if (-not (Test-Path -LiteralPath $packageJsonFullPath)) {
    throw "Could not find package.json at '$packageJsonFullPath'."
}

$original = [System.IO.File]::ReadAllText($packageJsonFullPath)
$pattern = '("version"\s*:\s*")([^"]+)(")'
$updated = [System.Text.RegularExpressions.Regex]::Replace($original, $pattern, "`${1}$version`${3}", 1)

if ($updated -eq $original) {
    Write-Host "package.json already uses version $version"
    exit 0
}

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($packageJsonFullPath, $updated, $utf8NoBom)
Write-Host "Updated $PackageJsonPath to version $version"
