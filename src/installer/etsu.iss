#define AppName "ETSU"
#ifndef AppVersion
  #error AppVersion is required. Build with build.ps1.
#endif
#ifndef AppArchitecture
  #error AppArchitecture is required. Build with build.ps1.
#endif
#ifndef ExifToolVersion
  #define ExifToolVersion "13.60"
#endif
#ifndef ExifToolHash
  #error ExifToolHash is required. Build with build.ps1.
#endif
#define AppPublisher "TinyScripts"
#define AppURL "https://github.com/tinyscr/etsu"
#define AppExeName "etsu.exe"
#define AppId "{{81ADEFE9-F5E3-4A42-8DF5-5D7E9AE39925}"
#ifdef ArchX64
  #define ExifToolArch "64"
#elif defined(ArchX86)
  #define ExifToolArch "32"
#else
  #error Pass /DArchX64 or /DArchX86.
#endif
#define ExifToolBaseName "exiftool-" + ExifToolVersion + "_" + ExifToolArch
#define ExifToolZipName ExifToolBaseName + ".zip"
#define ExifToolUrl "https://downloads.sourceforge.net/project/exiftool/" + ExifToolZipName

#if VER < EncodeVer(7,0,0)
  #error Inno Setup 7 or later is required.
#endif

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
ArchiveExtraction=full
ChangesEnvironment=yes
UsePreviousTasks=no
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

[Tasks]
Name: "exiftool"; Description: "Download and install ExifTool (about 11 MB)"; GroupDescription: "ExifTool:"; Check: ExifToolMissing

[Files]
Source: "..\..\bin\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "default.ini"; DestDir: "{app}"; DestName: "config.ini"; Flags: onlyifdoesntexist
Source: "{tmp}\{#ExifToolBaseName}\*"; DestDir: "{app}"; Flags: external skipifsourcedoesntexist recursesubdirs ignoreversion; Check: ShouldInstallExifTool

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
const
  ExifToolUrl = '{#ExifToolUrl}';
  ExifToolZipName = '{#ExifToolZipName}';
  ExifToolBaseName = '{#ExifToolBaseName}';
  ExifToolSHA256 = '{#ExifToolHash}';
  EnvironmentKey = 'Environment';
  AppRegistryKey = 'Software\TinyScripts\ETSU';
  ExifToolPathMarker = 'ExifToolPathAdded';

var
  CheckProgressPage: TOutputProgressWizardPage;
  DownloadPage: TDownloadWizardPage;
  ExifToolInstalled: Boolean;

procedure InitializeWizard;
begin
  CheckProgressPage := CreateOutputProgressPage(
    'Checking prerequisites',
    'Setup is checking the tools ETSU needs before installing.');
  DownloadPage := CreateDownloadPage(
    'Installing ExifTool',
    'Setup is downloading ExifTool.',
    nil);
  DownloadPage.ShowBaseNameInsteadOfUrl := True;
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

function ExifToolMissing(): Boolean;
begin
  Result := ExifToolLocation() = '';
end;

function ShouldInstallExifTool(): Boolean;
begin
  Result := ExifToolInstalled;
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
    else if WizardIsTaskSelected('exiftool') then
      CheckProgressPage.SetText('Checking ExifTool...', 'Not found; will download and install.')
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

  if (ExifPath = '') and (not WizardIsTaskSelected('exiftool')) then
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

function NormalizePathEntry(const Value: string): string;
begin
  Result := Trim(Value);
  if (Length(Result) >= 2) and (Result[1] = '"') and (Result[Length(Result)] = '"') then
    Result := Copy(Result, 2, Length(Result) - 2);
  if Result <> '' then
    Result := RemoveBackslashUnlessRoot(Result);
end;

function PathEntryExists(const Paths, Entry: string): Boolean;
var
  Rest, Dir: string;
  P: Integer;
begin
  Result := False;
  Rest := Paths;
  while Rest <> '' do
  begin
    P := Pos(';', Rest);
    if P = 0 then
    begin
      Dir := Rest;
      Rest := '';
    end
    else
    begin
      Dir := Copy(Rest, 1, P - 1);
      Delete(Rest, 1, P);
    end;
    if (NormalizePathEntry(Dir) <> '') and
       (CompareText(NormalizePathEntry(Dir), NormalizePathEntry(Entry)) = 0) then
    begin
      Result := True;
      Exit;
    end;
  end;
end;

function AddToUserPath(const Entry: string; var Added: Boolean): Boolean;
var
  Paths, NewPaths: string;
begin
  Added := False;
  Result := False;
  if not RegQueryStringValue(HKCU, EnvironmentKey, 'Path', Paths) then
  begin
    if RegValueExists(HKCU, EnvironmentKey, 'Path') then
      Exit;
    Paths := '';
  end;

  if PathEntryExists(Paths, Entry) then
  begin
    Result := True;
    Exit;
  end;

  if Paths = '' then
    NewPaths := Entry
  else if Paths[Length(Paths)] = ';' then
    NewPaths := Paths + Entry
  else
    NewPaths := Paths + ';' + Entry;

  Result := RegWriteExpandStringValue(HKCU, EnvironmentKey, 'Path', NewPaths);
  Added := Result;
end;

function RemoveFromUserPath(const Entry: string): Boolean;
var
  Paths, Rest, Dir, NewPaths: string;
  P: Integer;
begin
  Result := False;
  if not RegQueryStringValue(HKCU, EnvironmentKey, 'Path', Paths) then
  begin
    Result := not RegValueExists(HKCU, EnvironmentKey, 'Path');
    Exit;
  end;

  if not PathEntryExists(Paths, Entry) then
  begin
    Result := True;
    Exit;
  end;

  Rest := Paths;
  NewPaths := '';
  while Rest <> '' do
  begin
    P := Pos(';', Rest);
    if P = 0 then
    begin
      Dir := Rest;
      Rest := '';
    end
    else
    begin
      Dir := Copy(Rest, 1, P - 1);
      Delete(Rest, 1, P);
    end;
    if (NormalizePathEntry(Dir) = '') or
       (CompareText(NormalizePathEntry(Dir), NormalizePathEntry(Entry)) <> 0) then
    begin
      if NewPaths <> '' then
        NewPaths := NewPaths + ';';
      NewPaths := NewPaths + Dir;
    end;
  end;

  Result := RegWriteExpandStringValue(HKCU, EnvironmentKey, 'Path', NewPaths);
end;

function InstallExifTool(out Error: string; out Aborted: Boolean): Boolean;
var
  ZipPath, ExtractDir, SourceExe, TargetExe: string;
begin
  Result := False;
  Error := '';
  Aborted := False;
  ZipPath := ExpandConstant('{tmp}\' + ExifToolZipName);
  ExtractDir := ExpandConstant('{tmp}\' + ExifToolBaseName);

  DownloadPage.Clear;
  DownloadPage.Add(ExifToolUrl, ExifToolZipName, ExifToolSHA256);
  DownloadPage.Show;
  try
    try
      DownloadPage.Download;
      DelTree(ExtractDir, True, True, True);
      ForceDirectories(ExpandConstant('{tmp}'));
      ExtractArchive(ZipPath, ExpandConstant('{tmp}'), '', True, nil);

      SourceExe := AddBackslash(ExtractDir) + 'exiftool(-k).exe';
      TargetExe := AddBackslash(ExtractDir) + 'exiftool.exe';
      if not FileExists(SourceExe) then
        RaiseException('The ExifTool package did not contain exiftool(-k).exe.');
      if not FileExists(AddBackslash(ExtractDir) + 'exiftool_files\exiftool.pl') then
        RaiseException('The ExifTool package did not contain exiftool_files.');
      if FileExists(TargetExe) and (not DeleteFile(TargetExe)) then
        RaiseException('Could not replace the existing exiftool.exe in the temporary folder.');
      if not RenameFile(SourceExe, TargetExe) then
        RaiseException('Could not rename exiftool(-k).exe to exiftool.exe.');

      ExifToolInstalled := True;
      Result := True;
    except
      if DownloadPage.AbortedByUser then
      begin
        Aborted := True;
        Error := 'The ExifTool download was cancelled.';
      end
      else
        Error := Format('%s: %s', [DownloadPage.LastBaseNameOrUrl, GetExceptionMessage]);
      DelTree(ExtractDir, True, True, True);
    end;
  finally
    DownloadPage.Hide;
  end;
end;

procedure FinishExifToolInstall;
var
  AppDir, ConfigPath, Configured: string;
  Added: Boolean;
  Marker: Cardinal;
begin
  if not ExifToolInstalled then
    Exit;

  AppDir := ExpandConstant('{app}');
  if not FileExists(AddBackslash(AppDir) + 'exiftool.exe') then
  begin
    Log('ExifTool was downloaded but exiftool.exe is missing after the file copy.');
    MsgBox('ExifTool was downloaded, but exiftool.exe was not copied into:' + #13#10 + AppDir + #13#10 + #13#10 +
           'Install ExifTool manually or run Setup again.', mbError, MB_OK);
    Exit;
  end;

  ConfigPath := AddBackslash(AppDir) + 'config.ini';
  Configured := GetIniString('General', 'exiftoolPATH', 'null', ConfigPath);
  if (CompareText(Configured, 'null') <> 0) and (not FileExists(Configured)) then
  begin
    if SetIniString('General', 'exiftoolPATH', 'null', ConfigPath) then
      Log('Reset invalid exiftoolPATH in config.ini to null.')
    else
      Log('Could not reset invalid exiftoolPATH in config.ini.');
  end;

  if AddToUserPath(AppDir, Added) then
  begin
    if Added then
    begin
      if not RegWriteDWordValue(HKCU, AppRegistryKey, ExifToolPathMarker, 1) then
        Log('Could not record that Setup added the ETSU folder to PATH.');
    end
    else if not RegQueryDWordValue(HKCU, AppRegistryKey, ExifToolPathMarker, Marker) then
      Log('The ETSU folder was already on PATH; Setup will leave it in place on uninstall.');
  end
  else
  begin
    Log('Could not add the ETSU folder to PATH.');
    MsgBox('ExifTool was installed in:' + #13#10 + AppDir + #13#10 + #13#10 +
           'Setup could not add that folder to PATH. ETSU will still find ExifTool in its install folder.' + #13#10 + #13#10 +
           'To use exiftool from a terminal, add that folder to PATH yourself.', mbError, MB_OK);
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  Error: string;
  Aborted: Boolean;
begin
  if CurStep = ssInstall then
  begin
    if WizardIsTaskSelected('exiftool') then
    begin
      while not InstallExifTool(Error, Aborted) do
      begin
        if Aborted then
          Abort;
        case MsgBox('ExifTool could not be installed.' + #13#10 + #13#10 +
                    Error + #13#10 + #13#10 +
                    'Retry? Yes = retry, No = continue without ExifTool, Cancel = stop Setup.',
                    mbError, MB_YESNOCANCEL) of
          IDYES:
            begin
            end;
          IDNO:
            Break;
          IDCANCEL:
            Abort;
        end;
      end;
    end;
  end
  else if CurStep = ssPostInstall then
    FinishExifToolInstall;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Marker: Cardinal;
begin
  if CurUninstallStep <> usUninstall then
    Exit;

  if RegQueryDWordValue(HKCU, AppRegistryKey, ExifToolPathMarker, Marker) and (Marker <> 0) then
  begin
    if RemoveFromUserPath(ExpandConstant('{app}')) then
    begin
      if not RegDeleteValue(HKCU, AppRegistryKey, ExifToolPathMarker) then
        Log('Could not remove the PATH marker.');
      RegDeleteKeyIfEmpty(HKCU, AppRegistryKey);
    end
    else
      Log('Could not remove the ETSU folder from PATH.');
  end;
end;
