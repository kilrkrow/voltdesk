$ErrorActionPreference = 'Stop'
$packageName = 'voltdesk'

# VoltDeskSetup.exe is a per-user Inno Setup installer (PrivilegesRequired=lowest),
# so it lands in %LOCALAPPDATA%\Programs\VoltDesk and registers its uninstaller
# under HKCU. Older machine-wide installs registered under HKLM. Look in all of them.
$uninstallKeys = @(
  'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\VoltDesk_is1',
  'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\VoltDesk_is1',
  'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\VoltDesk_is1'
)

$found = $false
foreach ($key in $uninstallKeys) {
  $entry = Get-ItemProperty -Path $key -ErrorAction SilentlyContinue
  if (-not $entry -or -not $entry.UninstallString) { continue }
  $found = $true

  $uninstallerPath = $entry.UninstallString.Trim('"')
  $installDir = $entry.InstallLocation
  if (-not $installDir) { $installDir = Split-Path -Parent $uninstallerPath }
  $installDir = $installDir.TrimEnd('\')

  # Stop only VoltDesk processes running from this install, so files are not locked.
  Get-Process -Name 'VoltDesk' -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -and $_.Path.StartsWith($installDir + '\', [System.StringComparison]::OrdinalIgnoreCase) } |
    ForEach-Object { Stop-Process -Id $_.Id -Force; $_.WaitForExit(10000) | Out-Null }

  if (-not (Test-Path $uninstallerPath)) {
    Write-Warning "VoltDesk uninstaller not found at $uninstallerPath"
    continue
  }

  $process = Start-Process -FilePath $uninstallerPath -ArgumentList '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART' -Wait -NoNewWindow -PassThru
  $validExitCodes = @(0, 3010)
  if ($validExitCodes -notcontains $process.ExitCode) {
    throw "VoltDesk uninstaller failed with exit code $($process.ExitCode)"
  }
  # Remove only one install: the first match, per-user (HKCU) first.
  break
}

if (-not $found) {
  Write-Warning "VoltDesk does not appear to be installed (no VoltDesk_is1 uninstall entry); nothing to uninstall."
}
