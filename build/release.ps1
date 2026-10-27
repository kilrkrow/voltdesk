<#
.SYNOPSIS
  Builds VoltDesk (Release, win-x64) and packages it as the release zip plus
  a .sha256 file.

.DESCRIPTION
  This is the single packaging path for VoltDesk. The GitHub Actions release
  workflow (.github/workflows/release.yml) calls this same script, so a local
  run produces the same file name and the same zip layout as the asset that
  is attached to the GitHub release.

  Output (in <repo>\release unless -OutDir is given):
    VoltDesk-win-x64-<tag>.zip         publish output, files at the zip root
    VoltDesk-win-x64-<tag>.zip.sha256  "<sha256>  <zip name>" (sha256sum format)

  Works in Windows PowerShell 5.1 and PowerShell 7.

.PARAMETER Version
  Tag-style version, for example v1.1.1 or v1.1.1-rc.1. Defaults to "v" plus
  the <Version> in PowerDesktopApp.csproj.

.PARAMETER OutDir
  Folder for the zip and .sha256. Defaults to <repo>\release (git-ignored).

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File build\release.ps1
.EXAMPLE
  powershell -ExecutionPolicy Bypass -File build\release.ps1 -Version v1.1.1
#>
[CmdletBinding()]
param(
    [string]$Version,
    [string]$OutDir
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'PowerDesktopApp.csproj'
$publishDir = Join-Path $repoRoot 'publish'
if (-not $OutDir) { $OutDir = Join-Path $repoRoot 'release' }

if (-not $Version) {
    [xml]$csproj = Get-Content -LiteralPath $project -Raw
    $node = $csproj.SelectSingleNode('/Project/PropertyGroup/Version')
    if ($null -eq $node -or -not $node.InnerText) { throw "No <Version> found in $project" }
    $Version = 'v' + $node.InnerText.Trim()
}

# vMAJOR.MINOR.PATCH[.REVISION][-prerelease]
if ($Version -notmatch '^v(\d+\.\d+\.\d+(?:\.\d+)?)(-[0-9A-Za-z][0-9A-Za-z.-]*)?$') {
    throw "Version '$Version' is not a release tag like v1.2.3 or v1.2.3-rc.1"
}
$coreVersion = $Matches[1]
$semVersion = $Version.Substring(1)
$zipName = "VoltDesk-win-x64-$Version.zip"

Write-Host "VoltDesk release build"
Write-Host "  tag:          $Version"
Write-Host "  file version: $coreVersion"
Write-Host "  publish dir:  $publishDir"
Write-Host "  output dir:   $OutDir"

if (Test-Path -LiteralPath $publishDir) { Remove-Item -LiteralPath $publishDir -Recurse -Force }

& dotnet publish $project -c Release -o $publishDir --nologo `
    "-p:Version=$semVersion" "-p:AssemblyVersion=$coreVersion" "-p:FileVersion=$coreVersion"
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

# Sanity check: VoltDesk.exe is only an apphost stub; the app needs the full
# self-contained Windows App SDK output next to it, plus the tray icon file.
foreach ($required in @('VoltDesk.exe', 'VoltDesk.dll', 'VoltDesk.runtimeconfig.json', 'appicon.ico')) {
    if (-not (Test-Path -LiteralPath (Join-Path $publishDir $required))) { throw "publish\$required is missing" }
}
$publishFull = (Resolve-Path -LiteralPath $publishDir).ProviderPath.TrimEnd('\')
$files = @(Get-ChildItem -LiteralPath $publishFull -Recurse -File)
Write-Host "publish output contains $($files.Count) files"
if ($files.Count -lt 100) { throw "publish output has only $($files.Count) files; expected the full self-contained set" }

# Build the zip with forward-slash entry names in ordinal order so the layout
# is identical no matter which PowerShell or machine runs this script.
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$outFull = (Resolve-Path -LiteralPath $OutDir).ProviderPath
$zipPath = Join-Path $outFull $zipName
$shaPath = "$zipPath.sha256"
foreach ($p in @($zipPath, $shaPath)) { if (Test-Path -LiteralPath $p) { Remove-Item -LiteralPath $p -Force } }

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$map = @{}
foreach ($f in $files) { $map[$f.FullName.Substring($publishFull.Length + 1).Replace('\', '/')] = $f.FullName }
[string[]]$entries = @($map.Keys)
[Array]::Sort($entries, [StringComparer]::Ordinal)

$stream = [System.IO.File]::Open($zipPath, [System.IO.FileMode]::CreateNew)
try {
    $zip = New-Object System.IO.Compression.ZipArchive($stream, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($entry in $entries) {
            [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $zip, $map[$entry], $entry, [System.IO.Compression.CompressionLevel]::Optimal)
        }
    } finally { $zip.Dispose() }
} finally { $stream.Dispose() }

$hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
[System.IO.File]::WriteAllText($shaPath, "$hash  $zipName`n", (New-Object System.Text.ASCIIEncoding))

Write-Host "zip:    $zipPath ($($entries.Count) entries)"
Write-Host "sha256: $hash"

if ($env:GITHUB_OUTPUT) {
    Add-Content -LiteralPath $env:GITHUB_OUTPUT -Encoding ascii -Value @(
        "zip=$zipPath", "zip_name=$zipName", "sha256_file=$shaPath", "sha256=$hash", "publish_dir=$publishFull")
}
