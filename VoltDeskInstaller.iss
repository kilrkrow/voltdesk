[Setup]
#ifndef AppVersion
#define AppVersion "1.0.0"
#endif
AppName=VoltDesk
AppVersion={#AppVersion}
DefaultDirName={autopf}\VoltDesk
DefaultGroupName=VoltDesk
UninstallDisplayIcon={app}\VoltDesk.exe
ArchitecturesInstallIn64BitMode=x64compatible
Compression=lzma2
SolidCompression=yes
OutputDir=Output
OutputBaseFilename=VoltDeskSetup

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startup"; Description: "Run VoltDesk automatically when Windows starts"; GroupDescription: "Startup Options";

[Files]
; Ship the entire publish output. VoltDesk.exe is only a ~356 KB apphost stub:
; the real code lives in VoltDesk.dll, and the self-contained Windows App SDK
; needs its ~240 sibling files (plus VoltDesk.runtimeconfig.json) to start.
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "appicon.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\VoltDesk"; Filename: "{app}\VoltDesk.exe"; IconFilename: "{app}\appicon.ico"
Name: "{autodesktop}\VoltDesk"; Filename: "{app}\VoltDesk.exe"; IconFilename: "{app}\appicon.ico"; Tasks: desktopicon
Name: "{userstartup}\VoltDesk"; Filename: "{app}\VoltDesk.exe"; IconFilename: "{app}\appicon.ico"; Tasks: startup

[Run]
Filename: "{app}\VoltDesk.exe"; Description: "Launch VoltDesk"; Flags: nowait postinstall skipifsilent
