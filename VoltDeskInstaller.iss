[Setup]
AppName=VoltDesk
AppVersion=1.0
DefaultDirName={autopf}\VoltDesk
DefaultGroupName=VoltDesk
UninstallDisplayIcon={app}\VoltDesk.exe
Compression=lzma2
SolidCompression=yes
OutputDir=Output
OutputBaseFilename=VoltDeskSetup

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startup"; Description: "Run VoltDesk automatically when Windows starts"; GroupDescription: "Startup Options";

[Files]
Source: "publish\VoltDesk.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "appicon.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\VoltDesk"; Filename: "{app}\VoltDesk.exe"; IconFilename: "{app}\appicon.ico"
Name: "{autodesktop}\VoltDesk"; Filename: "{app}\VoltDesk.exe"; IconFilename: "{app}\appicon.ico"; Tasks: desktopicon
Name: "{userstartup}\VoltDesk"; Filename: "{app}\VoltDesk.exe"; IconFilename: "{app}\appicon.ico"; Tasks: startup

[Run]
Filename: "{app}\VoltDesk.exe"; Description: "Launch VoltDesk"; Flags: nowait postinstall skipifsilent
