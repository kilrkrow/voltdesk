[Setup]
#ifndef AppVersion
#define AppVersion "1.0.0"
#endif
AppName=VoltDesk
AppVersion={#AppVersion}
; Per-user install, no elevation. Keeps {autopf} -> {localappdata}\Programs,
; puts {group}/{autodesktop} in the installing user's own profile, and makes
; {userstartup} below resolve to that user rather than whichever admin
; happened to satisfy UAC (which silently broke "start with Windows").
PrivilegesRequired=lowest
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

[Code]
// VoltDesk itself can write HKCU\...\Run\VoltDesk ("Start with Windows") and
// {userprograms}\VoltDesk.lnk ("Start Menu shortcut"). Remove them on
// uninstall, but only when they point into this install, so a separate copy
// of VoltDesk (portable zip, dev build) keeps its own entries.
function PointsIntoApp(const Target: String): Boolean;
var
  AppDir: String;
begin
  AppDir := Lowercase(AddBackslash(ExpandConstant('{app}')));
  Result := (Target <> '') and (Pos(AppDir, Lowercase(Target)) > 0);
end;

function ShortcutTarget(const LinkPath: String): String;
var
  Shell, Link: Variant;
begin
  Result := '';
  try
    Shell := CreateOleObject('WScript.Shell');
    Link := Shell.CreateShortcut(LinkPath);
    Result := Link.TargetPath;
  except
    Result := '';
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  RunValue, LinkPath: String;
begin
  if CurUninstallStep = usUninstall then
  begin
    if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'VoltDesk', RunValue) then
      if PointsIntoApp(RunValue) then
        RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'VoltDesk');
    LinkPath := ExpandConstant('{userprograms}\VoltDesk.lnk');
    if FileExists(LinkPath) then
      if PointsIntoApp(ShortcutTarget(LinkPath)) then
        DeleteFile(LinkPath);
  end;
end;
