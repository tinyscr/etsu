#define AppName "ETSU"
#ifndef AppVersion
#error AppVersion is required. Build with build.ps1.
#endif
#define AppPublisher "TinyScripts"
#define AppURL "https://github.com/tinyscr/etsu"
#define AppExeName "etsu.exe"
#define AppId "{{81ADEFE9-F5E3-4A42-8DF5-5D7E9AE39925}"

[Setup]
AppId={#AppId}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
AppCopyright=Copyright (C) 2026 TinyScripts
DefaultDirName={localappdata}\Programs\ETSU
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
DisableDirPage=no
LicenseFile=..\..\LICENSE
PrivilegesRequired=lowest
OutputDir=..\..\dist
OutputBaseFilename=v{#AppVersion}_installer
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExeName}
VersionInfoVersion={#AppVersion}.0
VersionInfoCompany={#AppPublisher}
VersionInfoDescription={#AppName} Setup
SetupIconFile=..\Assets\icon.ico

[Files]
Source: "..\..\bin\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "default.ini"; DestDir: "{app}"; DestName: "config.ini"; Flags: onlyifdoesntexist

[Dirs]
Name: "{app}\logs\etsu"
Name: "{app}\tmp"

[Registry]
Root: HKCU; Subkey: "Software\Classes\*\shell\etsu"; ValueType: string; ValueName: "MUIVerb"; ValueData: "ETSU"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\*\shell\etsu"; ValueType: string; ValueName: "subcommands"; ValueData: ""
Root: HKCU; Subkey: "Software\Classes\*\shell\etsu"; ValueType: string; ValueName: "MultiSelectModel"; ValueData: "Single"

Root: HKCU; Subkey: "Software\Classes\*\shell\etsu\shell\a_read"; ValueType: string; ValueName: "MUIVerb"; ValueData: "&Read metadata..."
Root: HKCU; Subkey: "Software\Classes\*\shell\etsu\shell\a_read"; ValueType: string; ValueName: "MultiSelectModel"; ValueData: "Single"
Root: HKCU; Subkey: "Software\Classes\*\shell\etsu\shell\a_read\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExeName}"" read ""%L"""

Root: HKCU; Subkey: "Software\Classes\*\shell\etsu\shell\b_clean"; ValueType: string; ValueName: "MUIVerb"; ValueData: "&Clean metadata..."
Root: HKCU; Subkey: "Software\Classes\*\shell\etsu\shell\b_clean"; ValueType: string; ValueName: "MultiSelectModel"; ValueData: "Single"
Root: HKCU; Subkey: "Software\Classes\*\shell\etsu\shell\b_clean\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExeName}"" clean ""%L"""

Root: HKCU; Subkey: "Software\Classes\*\shell\etsu\shell\c_date"; ValueType: string; ValueName: "MUIVerb"; ValueData: "&Set date..."
Root: HKCU; Subkey: "Software\Classes\*\shell\etsu\shell\c_date"; ValueType: string; ValueName: "MultiSelectModel"; ValueData: "Single"
Root: HKCU; Subkey: "Software\Classes\*\shell\etsu\shell\c_date\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExeName}"" date ""%L"""

[UninstallDelete]
Type: files; Name: "{app}\config.ini"
Type: filesandordirs; Name: "{app}\logs"
Type: filesandordirs; Name: "{app}\tmp"

[Code]
function ExifToolOnPath(): Boolean;
var
  Paths, Dir: string;
  P: Integer;
begin
  Paths := GetEnv('PATH');
  while Paths <> '' do
  begin
    P := Pos(';', Paths);
    if P = 0 then
    begin
      Dir := Paths;
      Paths := '';
    end
    else
    begin
      Dir := Copy(Paths, 1, P - 1);
      Delete(Paths, 1, P);
    end;
    if (Dir <> '') and FileExists(AddBackslash(Dir) + 'exiftool.exe') then
    begin
      Result := True;
      Exit;
    end;
  end;
  Result := False;
end;

function ExifToolAvailable(): Boolean;
var
  Configured: string;
begin
  Configured := GetIniString('General', 'exiftoolPATH', 'null', ExpandConstant('{app}\config.ini'));
  if CompareText(Configured, 'null') <> 0 then
  begin
    Result := FileExists(Configured);
    Exit;
  end;
  Result := ExifToolOnPath() or FileExists(ExpandConstant('{app}\exiftool.exe'));
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    if not ExifToolAvailable() then
      MsgBox('ExifTool was not found on PATH.' + #13#10 +
             'Install ExifTool or set exiftoolPATH in ' + ExpandConstant('{app}\config.ini') +
             ' before using etsu.', mbInformation, MB_OK);
end;
