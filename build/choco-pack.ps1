<#
.SYNOPSIS
  Stages and packs the VoltDesk Chocolatey package (never pushes).

.DESCRIPTION
  Copies the template in choco\ into a staging folder, adds the built
  installer, LICENSE.txt and VERIFICATION.txt, stamps the nuspec <version>
  and <releaseNotes> from the release tag, and runs `choco pack`.
  Pushing is done only by the release workflow, and only for real releases.

.PARAMETER Version
  Release tag, for example v1.1.1 or v1.1.1-rc.1.

.PARAMETER Installer
  Path to the built VoltDeskSetup.exe.

.PARAMETER OutDir
  Folder for the staging folder and the .nupkg. Defaults to <repo>\release\choco.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Version,
    [Parameter(Mandatory = $true)][string]$Installer,
    [string]$OutDir
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $OutDir) { $OutDir = Join-Path $repoRoot 'release\choco' }

if ($Version -notmatch '^v(\d+\.\d+\.\d+(?:\.\d+)?)(-[0-9A-Za-z][0-9A-Za-z.-]*)?$') {
    throw "Version '$Version' is not a release tag like v1.2.3 or v1.2.3-rc.1"
}
$pkgVersion = $Version.Substring(1)
if (-not (Test-Path -LiteralPath $Installer)) { throw "Installer not found: $Installer" }

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$outFull = (Resolve-Path -LiteralPath $OutDir).ProviderPath
$stage = Join-Path $outFull 'pkg'
if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
Copy-Item -LiteralPath (Join-Path $repoRoot 'choco') -Destination $stage -Recurse

$tools = Join-Path $stage 'tools'
Copy-Item -LiteralPath $Installer -Destination (Join-Path $tools 'VoltDeskSetup.exe')
Copy-Item -LiteralPath (Join-Path $repoRoot 'LICENSE') -Destination (Join-Path $tools 'LICENSE.txt')

$installerHash = (Get-FileHash -LiteralPath (Join-Path $tools 'VoltDeskSetup.exe') -Algorithm SHA256).Hash.ToLowerInvariant()
$verification = @"
VERIFICATION
Verification is intended to assist the Chocolatey moderators and community
in verifying that this package's embedded binaries are trustworthy.

The embedded installer is built from source by the GitHub Actions workflow
for the release tag and then packaged without downloading third-party
application binaries.

Package: voltdesk
Version: $pkgVersion
Embedded file: tools\VoltDeskSetup.exe
SHA256: $installerHash

Source: https://github.com/kilrkrow/voltdesk/tree/$Version
Release: https://github.com/kilrkrow/voltdesk/releases/tag/$Version
Workflow: https://github.com/kilrkrow/voltdesk/actions/workflows/release.yml
"@
[System.IO.File]::WriteAllText((Join-Path $tools 'VERIFICATION.txt'), $verification, (New-Object System.Text.ASCIIEncoding))

$nuspecPath = Join-Path $stage 'voltdesk.nuspec'
[xml]$nuspec = Get-Content -LiteralPath $nuspecPath -Raw
$ns = New-Object System.Xml.XmlNamespaceManager($nuspec.NameTable)
$ns.AddNamespace('n', $nuspec.DocumentElement.NamespaceURI)
$nuspec.SelectSingleNode('/n:package/n:metadata/n:version', $ns).InnerText = $pkgVersion
$nuspec.SelectSingleNode('/n:package/n:metadata/n:releaseNotes', $ns).InnerText = "https://github.com/kilrkrow/voltdesk/releases/tag/$Version"
$settings = New-Object System.Xml.XmlWriterSettings
$settings.Encoding = New-Object System.Text.UTF8Encoding($false)
$settings.Indent = $true
$writer = [System.Xml.XmlWriter]::Create($nuspecPath, $settings)
try { $nuspec.Save($writer) } finally { $writer.Dispose() }

$nupkg = Join-Path $outFull "voltdesk.$pkgVersion.nupkg"
if (Test-Path -LiteralPath $nupkg) { Remove-Item -LiteralPath $nupkg -Force }
Push-Location $stage
try {
    & choco pack voltdesk.nuspec --out-dir $outFull --limit-output
    if ($LASTEXITCODE -ne 0) { throw "choco pack failed with exit code $LASTEXITCODE" }
} finally { Pop-Location }
if (-not (Test-Path -LiteralPath $nupkg)) { throw "Expected package not found: $nupkg" }

Write-Host "nupkg: $nupkg"
Write-Host "embedded installer sha256: $installerHash"
if ($env:GITHUB_OUTPUT) {
    Add-Content -LiteralPath $env:GITHUB_OUTPUT -Encoding ascii -Value @("nupkg=$nupkg", "package_version=$pkgVersion", "package_dir=$outFull")
}
