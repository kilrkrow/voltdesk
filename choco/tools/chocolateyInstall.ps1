$ErrorActionPreference = 'Stop'
$packageName = 'voltdesk'
$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$installerPath = Get-ChildItem $toolsDir -Filter "*VoltDesk*.exe" | Select-Object -First 1 -ExpandProperty FullName

if (-not $installerPath) {
  throw "VoltDesk installer not found in tools directory"
}

# VoltDesk 1.1.0 and older installed machine-wide (Program Files, HKLM uninstall
# entry). The installer is per-user now, so remove an old machine-wide copy first;
# otherwise `choco upgrade` leaves two side-by-side installs.
$machineKeys = @(
  'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\VoltDesk_is1',
  'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\VoltDesk_is1'
)
foreach ($key in $machineKeys) {
  $old = Get-ItemProperty -Path $key -ErrorAction SilentlyContinue
  if (-not $old -or -not $old.UninstallString) { continue }
  $oldUninstaller = $old.UninstallString.Trim('"')
  if (-not (Test-Path $oldUninstaller)) { continue }
  $oldDir = (Split-Path -Parent $oldUninstaller).TrimEnd('\')
  Write-Host "Removing previous machine-wide VoltDesk install from $oldDir"
  Get-Process -Name 'VoltDesk' -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -and $_.Path.StartsWith($oldDir + '\', [System.StringComparison]::OrdinalIgnoreCase) } |
    ForEach-Object { Stop-Process -Id $_.Id -Force; $_.WaitForExit(10000) | Out-Null }
  try {
    $p = Start-Process -FilePath $oldUninstaller -ArgumentList '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART' -Wait -NoNewWindow -PassThru
    if (@(0, 3010) -notcontains $p.ExitCode) { Write-Warning "Old VoltDesk uninstaller exited with code $($p.ExitCode)" }
  } catch {
    Write-Warning "Could not remove the old machine-wide VoltDesk install: $($_.Exception.Message)"
  }
}

$silentArgs = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART'

$process = Start-Process -FilePath $installerPath -ArgumentList $silentArgs -Wait -NoNewWindow -PassThru
$validExitCodes = @(0, 3010)
if ($validExitCodes -notcontains $process.ExitCode) {
  throw "VoltDesk installer failed with exit code $($process.ExitCode)"
}