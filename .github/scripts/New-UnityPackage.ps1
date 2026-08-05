<#
.SYNOPSIS
    Builds a .unitypackage from the CrowFX source tree without needing Unity.

.DESCRIPTION
    A .unitypackage is a gzipped tar with one directory per asset, named by that
    asset's GUID. Each directory holds:

        asset       the file itself, omitted for folders
        asset.meta  the Unity .meta file the GUID came from
        pathname    where the asset lands on import, with no trailing newline

    That is the whole format, so a package identical in structure to one Unity
    exports can be produced from the repository alone. Verified against the shipped
    v2.0.1 package, which contains 146 entries laid out exactly this way.

    Preview thumbnails are the one thing not reproduced. Unity embeds a preview.png
    for a few textures; without it the import dialog shows a generic icon instead of
    a thumbnail for those three files. Nothing about the import itself changes.
#>
param(
    # Folder to package, relative to the repository root.
    [string]$SourceRoot = "CrowFX",

    # Where the packaged files should land in a consuming project. Must match what
    # previous releases used or existing installs will be duplicated rather than
    # updated, because Unity matches on this path.
    [string]$AssetRoot = "Assets/CrowFX-Unity-Image-Effects",

    [Parameter(Mandatory = $true)]
    [string]$Version,

    [string]$OutputDirectory = "."
)

$ErrorActionPreference = "Stop"

function Resolve-RepositoryRoot {
    $scriptDir = Split-Path -Parent $PSCommandPath
    return (Resolve-Path (Join-Path $scriptDir "../..")).Path
}

$repoRoot = Resolve-RepositoryRoot
$sourceFullPath = Join-Path $repoRoot $SourceRoot

if (-not (Test-Path -LiteralPath $sourceFullPath)) {
    throw "Could not find a source folder at '$sourceFullPath'."
}

$version = $Version.Trim().TrimStart('v', 'V')
$packageName = "CrowFX_V$version.unitypackage"

# Join-Path would splice an absolute output directory onto the repo root and produce
# a path no provider accepts, so only relative directories are resolved against it.
$outputFullPath = if ([System.IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory
}
else {
    Join-Path $repoRoot $OutputDirectory
}

if (-not (Test-Path -LiteralPath $outputFullPath)) {
    New-Item -ItemType Directory -Path $outputFullPath -Force | Out-Null
}
$outputFullPath = Join-Path ((Resolve-Path $outputFullPath).Path) $packageName

# Staging directory: one folder per GUID, tarred at the end.
$staging = Join-Path ([System.IO.Path]::GetTempPath()) "crowfx-unitypackage-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $staging -Force | Out-Null

function Get-MetaGuid {
    param([string]$MetaPath)

    foreach ($line in [System.IO.File]::ReadAllLines($MetaPath)) {
        $match = [regex]::Match($line, '^guid:\s*(?<guid>[0-9a-fA-F]{32})\s*$')
        if ($match.Success) {
            return $match.Groups['guid'].Value
        }
    }

    return $null
}

try {
    # Drive the walk from the .meta files: an asset without one cannot be imported,
    # and a .meta without its asset is a folder or a stale leftover.
    $metaFiles = Get-ChildItem -Path $sourceFullPath -Filter "*.meta" -Recurse -File
    $written = 0
    $folders = 0
    $seenGuids = @{}

    # The packaged root folder needs its own entry, and its .meta sits outside the
    # folder being walked.
    $rootMeta = Join-Path $repoRoot "$SourceRoot.meta"
    if (Test-Path -LiteralPath $rootMeta) {
        $metaFiles = @(Get-Item -LiteralPath $rootMeta) + $metaFiles
    }
    else {
        Write-Warning "No $SourceRoot.meta found. The packaged root folder will have no entry."
    }

    foreach ($meta in $metaFiles) {
        $guid = Get-MetaGuid -MetaPath $meta.FullName
        if ([string]::IsNullOrEmpty($guid)) {
            Write-Warning "Skipping '$($meta.FullName)': no guid line."
            continue
        }

        if ($seenGuids.ContainsKey($guid)) {
            throw "Duplicate GUID $guid in '$($meta.FullName)' and '$($seenGuids[$guid])'. Unity would import one over the other."
        }
        $seenGuids[$guid] = $meta.FullName

        # Strip the trailing '.meta' to find what it describes.
        $assetPath = $meta.FullName.Substring(0, $meta.FullName.Length - ".meta".Length)
        $isFolder = (Test-Path -LiteralPath $assetPath -PathType Container)
        $assetExists = (Test-Path -LiteralPath $assetPath)

        if (-not $assetExists) {
            Write-Warning "Skipping orphaned meta '$($meta.FullName)': nothing at '$assetPath'."
            continue
        }

        # Path relative to the repository root, converted to the install location.
        $relative = $assetPath.Substring($repoRoot.Length).TrimStart('\', '/').Replace('\', '/')
        $pathname = "$AssetRoot/$relative"

        $entry = Join-Path $staging $guid
        New-Item -ItemType Directory -Path $entry -Force | Out-Null

        Copy-Item -LiteralPath $meta.FullName -Destination (Join-Path $entry "asset.meta") -Force

        if ($isFolder) {
            $folders++
        }
        else {
            Copy-Item -LiteralPath $assetPath -Destination (Join-Path $entry "asset") -Force
            $written++
        }

        # No trailing newline: Unity writes the path raw, and a stray newline becomes
        # part of the destination path.
        $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
        [System.IO.File]::WriteAllText((Join-Path $entry "pathname"), $pathname, $utf8NoBom)
    }

    if ($written -eq 0) {
        throw "No assets were packaged from '$sourceFullPath'. Refusing to publish an empty package."
    }

    if (Test-Path -LiteralPath $outputFullPath) {
        Remove-Item -LiteralPath $outputFullPath -Force
    }

    # -C so the archive holds GUID directories at its root rather than the temp path.
    tar -czf $outputFullPath -C $staging .
    if ($LASTEXITCODE -ne 0) {
        throw "tar failed with exit code $LASTEXITCODE."
    }

    $sizeKb = [math]::Round((Get-Item -LiteralPath $outputFullPath).Length / 1KB, 1)
    # ${} around the name: a bare '$packageName:' parses as a drive-qualified variable.
    Write-Host "Built ${packageName}: $written assets, $folders folders, $sizeKb KB"

    if ($env:GITHUB_OUTPUT) {
        Add-Content -Path $env:GITHUB_OUTPUT -Value "package-path=$outputFullPath"
        Add-Content -Path $env:GITHUB_OUTPUT -Value "package-name=$packageName"
    }

    return $outputFullPath
}
finally {
    if (Test-Path -LiteralPath $staging) {
        Remove-Item -LiteralPath $staging -Recurse -Force -ErrorAction SilentlyContinue
    }
}
