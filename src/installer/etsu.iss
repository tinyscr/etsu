#define AppName "ETSU"
#ifndef AppVersion
#error AppVersion is required. Build with build.ps1.
#endif
#ifndef AppArchitecture
#error AppArchitecture is required. Build with build.ps1.
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
LicenseFile=..\..\LICENSE.rtf
PrivilegesRequired=lowest
OutputDir=..\..\dist
OutputBaseFilename=v{#AppVersion}_installer_{#AppArchitecture}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern dark includetitlebar
#ifdef ArchX64
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
#elif defined(ArchX86)
ArchitecturesAllowed=x86compatible
#else
#error Pass /DArchX64 or /DArchX86.
#endif
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
Root: HKCU; Subkey: "Software\Classes\*\shell\etsu"; ValueType: string; ValueName: "MultiSelectModel"; ValueData: "Player"

Root: HKCU; Subkey: "Software\Classes\*\shell\etsu\shell\a_read"; ValueType: string; ValueName: "MUIVerb"; ValueData: "&Read metadata..."
Root: HKCU; Subkey: "Software\Classes\*\shell\etsu\shell\a_read"; ValueType: string; ValueName: "MultiSelectModel"; ValueData: "Single"
Root: HKCU; Subkey: "Software\Classes\*\shell\etsu\shell\a_read\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExeName}"" read ""%L"""

Root: HKCU; Subkey: "Software\Classes\*\shell\etsu\shell\b_clean"; ValueType: string; ValueName: "MUIVerb"; ValueData: "&Clean metadata..."
Root: HKCU; Subkey: "Software\Classes\*\shell\etsu\shell\b_clean"; ValueType: string; ValueName: "MultiSelectModel"; ValueData: "Player"
Root: HKCU; Subkey: "Software\Classes\*\shell\etsu\shell\b_clean\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExeName}"" clean ""%L"""

Root: HKCU; Subkey: "Software\Classes\*\shell\etsu\shell\c_date"; ValueType: string; ValueName: "MUIVerb"; ValueData: "&Set date..."
Root: HKCU; Subkey: "Software\Classes\*\shell\etsu\shell\c_date"; ValueType: string; ValueName: "MultiSelectModel"; ValueData: "Player"
Root: HKCU; Subkey: "Software\Classes\*\shell\etsu\shell\c_date\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExeName}"" date ""%L"""

[UninstallDelete]
Type: files; Name: "{app}\config.ini"
Type: filesandordirs; Name: "{app}\logs"
Type: filesandordirs; Name: "{app}\tmp"

[Code]
var
  CheckProgressPage: TOutputProgressWizardPage;

procedure InitializeWizard;
begin
  CheckProgressPage := CreateOutputProgressPage(
    'Checking prerequisites',
    'Setup is checking the tools ETSU needs before installing.');
end;

function ExifToolOnPath(): string;
var
  Paths, Dir, Candidate: string;
  P: Integer;
begin
  Result := '';
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
    if Dir <> '' then
    begin
      Candidate := AddBackslash(Dir) + 'exiftool.exe';
      if FileExists(Candidate) then
      begin
        Result := Candidate;
        Exit;
      end;
    end;
  end;
end;

function ExifToolLocation(): string;
var
  Configured: string;
begin
  Configured := GetIniString('General', 'exiftoolPATH', 'null', ExpandConstant('{app}\config.ini'));
  if CompareText(Configured, 'null') <> 0 then
  begin
    if FileExists(Configured) then
      Result := Configured
    else
      Result := '';
    Exit;
  end;

  Result := ExifToolOnPath();
  if Result <> '' then
    Exit;

  Result := ExpandConstant('{app}\exiftool.exe');
  if FileExists(Result) then
    Exit;

  Result := '';
end;

function CscLocation(): string;
var
  Candidate: string;
begin
  Candidate := ExpandConstant('{win}\Microsoft.NET\Framework64\v4.0.30319\csc.exe');
  if FileExists(Candidate) then
  begin
    Result := Candidate;
    Exit;
  end;

  Candidate := ExpandConstant('{win}\Microsoft.NET\Framework\v4.0.30319\csc.exe');
  if FileExists(Candidate) then
  begin
    Result := Candidate;
    Exit;
  end;

  Result := '';
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  DotNetOK: Boolean;
  CscPath, ExifPath: string;
begin
  Result := '';

  CheckProgressPage.SetText('Checking .NET Framework 4.5 or later...', '');
  CheckProgressPage.SetProgress(0, 3);
  CheckProgressPage.Show;
  try
    DotNetOK := IsDotNetInstalled(net45, 0);
    if DotNetOK then
      CheckProgressPage.SetText('Checking .NET Framework 4.5 or later...', 'Found.')
    else
      CheckProgressPage.SetText('Checking .NET Framework 4.5 or later...', 'Not found (required).');
    Sleep(200);

    CheckProgressPage.SetText('Checking the C# compiler (csc.exe)...', '');
    CheckProgressPage.SetProgress(1, 3);
    CscPath := CscLocation();
    if CscPath <> '' then
      CheckProgressPage.SetText('Checking the C# compiler (csc.exe)...', CscPath)
    else
      CheckProgressPage.SetText('Checking the C# compiler (csc.exe)...', 'Not found (optional).');
    Sleep(200);

    CheckProgressPage.SetText('Checking ExifTool...', '');
    CheckProgressPage.SetProgress(2, 3);
    ExifPath := ExifToolLocation();
    if ExifPath <> '' then
      CheckProgressPage.SetText('Checking ExifTool...', ExifPath)
    else
      CheckProgressPage.SetText('Checking ExifTool...', 'Not found (optional).');
    Sleep(200);

    CheckProgressPage.SetText('Prerequisite checks complete.', '');
    CheckProgressPage.SetProgress(3, 3);
    Sleep(200);
  finally
    CheckProgressPage.Hide;
  end;

  if not DotNetOK then
  begin
    Result := 'ETSU requires .NET Framework 4.5 or later. Install it, then run Setup again.';
    Exit;
  end;

  if CscPath = '' then
    MsgBox('csc.exe was not found. This is only needed to build ETSU from source; the installed app will still run.' + #13#10 + #13#10 +
           'If you plan to build from source, install .NET Framework 4.5 or later.', mbInformation, MB_OK);

  if ExifPath = '' then
  begin
    if MsgBox('ExifTool was not found on PATH or in the ETSU folder.' + #13#10 + #13#10 +
              'ETSU can still be installed, but metadata operations need ExifTool. Install it later or set exiftoolPATH in config.ini.' + #13#10 + #13#10 +
              'Continue?', mbConfirmation, MB_YESNO) = IDNO then
    begin
      Result := 'Setup stopped because ExifTool was not found.';
      Exit;
    end;
  end;
end;
