$ErrorActionPreference = 'Stop'
$packageName = 'voltdesk'
$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$installerPath = Get-ChildItem $toolsDir -Filter "*VoltDesk*.exe" | Select-Object -First 1 -ExpandProperty FullName

if (-not $installerPath) {
  throw "VoltDesk installer not found in tools directory"
}

$silentArgs = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART'

$process = Start-Process -FilePath $installerPath -ArgumentList $silentArgs -Wait -NoNewWindow -PassThru
$validExitCodes = @(0, 3010)
if ($validExitCodes -notcontains $process.ExitCode) {
  throw "VoltDesk installer failed with exit code $($process.ExitCode)"
}
