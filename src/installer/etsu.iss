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
LicenseFile=..\..\LICENSE
PrivilegesRequired=lowest
OutputDir=..\..\dist
OutputBaseFilename=v{#AppVersion}_installer_{#AppArchitecture}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern dark includetitlebar
ArchiveExtraction=full
ChangesEnvironment=yes
#ifdef ArchX64
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
#elif defined(ArchX86)
ArchitecturesAllowed=x86compatible
#else
#error Pass /DArchX64 or /DArchX86.
#endif
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\bin\{#AppExeName}
UninstallFilesDir={app}\bin
VersionInfoVersion={#AppVersion}.0
VersionInfoCompany={#AppPublisher}
VersionInfoDescription={#AppName} Setup
SetupIconFile=..\Assets\icon.ico

[Files]
Source: "..\..\bin\{#AppExeName}"; DestDir: "{app}\bin"; Flags: ignoreversion
Source: "default.ini"; DestDir: "{app}\bin"; DestName: "config.ini"; Flags: onlyifdoesntexist
Source: "ETSU Documentation.url"; DestDir: "{app}"; Flags: ignoreversion
Source: "ETSU Documentation.url"; DestDir: "{userprograms}\ETSU"; Flags: ignoreversion
Source: "{tmp}\{#ExifToolBaseName}\exiftool.exe"; DestDir: "{app}\exiftool"; Flags: external skipifsourcedoesntexist ignoreversion uninsneveruninstall; Check: ShouldInstallExifTool
Source: "{tmp}\{#ExifToolBaseName}\exiftool_files\*"; DestDir: "{app}\exiftool\exiftool_files"; Flags: external skipifsourcedoesntexist recursesubdirs ignoreversion uninsneveruninstall; Check: ShouldInstallExifTool

[Dirs]
Name: "{app}\bin"
Name: "{app}\exiftool"
Name: "{app}\logs"
Name: "{app}\tmp"

[Registry]
Root: HKCU; Subkey: "Software\Classes\*\shell\etsu"; ValueType: string; ValueName: "MUIVerb"; ValueData: "ETSU"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\*\shell\etsu"; ValueType: string; ValueName: "subcommands"; ValueData: ""
Root: HKCU; Subkey: "Software\Classes\*\shell\etsu"; ValueType: string; ValueName: "MultiSelectModel"; ValueData: "Player"

Root: HKCU; Subkey: "Software\Classes\*\shell\etsu\shell\a_read"; ValueType: string; ValueName: "MUIVerb"; ValueData: "&Read metadata..."
Root: HKCU; Subkey: "Software\Classes\*\shell\etsu\shell\a_read"; ValueType: string; ValueName: "MultiSelectModel"; ValueData: "Single"
Root: HKCU; Subkey: "Software\Classes\*\shell\etsu\shell\a_read\command"; ValueType: string; ValueName: ""; ValueData: """{app}\bin\{#AppExeName}"" read ""%L"""

Root: HKCU; Subkey: "Software\Classes\*\shell\etsu\shell\b_clean"; ValueType: string; ValueName: "MUIVerb"; ValueData: "&Clean metadata..."
Root: HKCU; Subkey: "Software\Classes\*\shell\etsu\shell\b_clean"; ValueType: string; ValueName: "MultiSelectModel"; ValueData: "Player"
Root: HKCU; Subkey: "Software\Classes\*\shell\etsu\shell\b_clean\command"; ValueType: string; ValueName: ""; ValueData: """{app}\bin\{#AppExeName}"" clean ""%L"""

Root: HKCU; Subkey: "Software\Classes\*\shell\etsu\shell\c_date"; ValueType: string; ValueName: "MUIVerb"; ValueData: "&Set date..."
Root: HKCU; Subkey: "Software\Classes\*\shell\etsu\shell\c_date"; ValueType: string; ValueName: "MultiSelectModel"; ValueData: "Player"
Root: HKCU; Subkey: "Software\Classes\*\shell\etsu\shell\c_date\command"; ValueType: string; ValueName: ""; ValueData: """{app}\bin\{#AppExeName}"" date ""%L"""

[Icons]
Name: "{userprograms}\ETSU\Uninstall"; Filename: "{app}\bin\uninstall.exe"; Comment: "Uninstall ETSU"

[UninstallDelete]
Type: files; Name: "{app}\bin\config.ini"
Type: files; Name: "{app}\Uninstall.lnk"
Type: filesandordirs; Name: "{app}\logs"
Type: filesandordirs; Name: "{app}\tmp"
Type: dirifempty; Name: "{app}\bin"
Type: dirifempty; Name: "{app}"
Type: dirifempty; Name: "{userprograms}\ETSU"

[Code]
const
  ExifToolUrl = '{#ExifToolUrl}';
  ExifToolZipName = '{#ExifToolZipName}';
  ExifToolBaseName = '{#ExifToolBaseName}';
  ExifToolSHA256 = '{#ExifToolHash}';
  EnvironmentKey = 'Environment';
  AppRegistryKey = 'Software\TinyScripts\ETSU';
  UninstallKey = 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{81ADEFE9-F5E3-4A42-8DF5-5D7E9AE39925}_is1';
  ExifToolPathMarker = 'ExifToolPathAdded';
  BinPathMarker = 'BinPathAdded';
  ExifToolFilesMarker = 'ExifToolFilesInstalled';

var
  CheckProgressPage: TOutputProgressWizardPage;
  DownloadPage: TDownloadWizardPage;
  ExifToolPage: TWizardPage;
  ExifToolDownloadRadio, ExifToolManualRadio: TNewRadioButton;
  ExifToolInstallSelected: Boolean;
  ExifToolInstalled: Boolean;
  UninstallExifTool: Boolean;

procedure ExifToolChoiceClick(Sender: TObject);
begin
  ExifToolInstallSelected := ExifToolDownloadRadio.Checked;
  WizardForm.NextButton.Enabled := ExifToolInstallSelected;
end;

procedure LinkLabelOnLinkClick(Sender: TObject; const Link: string; LinkType: TSysLinkType);
var
  ErrorCode: Integer;
begin
  if LinkType = sltURL then
    ShellExecAsOriginalUser('open', Link, '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
end;

procedure CreateExifToolPage;
var
  InfoText, PromptText, DownloadText: TNewStaticText;
  ManualText: TNewLinkLabel;
begin
  ExifToolPage := CreateCustomPage(wpSelectDir, 'ExifTool Required', '');

  InfoText := TNewStaticText.Create(ExifToolPage);
  InfoText.Parent := ExifToolPage.Surface;
  InfoText.AutoSize := False;
  InfoText.WordWrap := True;
  InfoText.Width := ExifToolPage.SurfaceWidth;
  InfoText.Caption := 'ExifTool was not found on your system. ETSU requires ExifTool to function.';
  InfoText.AdjustHeight;

  PromptText := TNewStaticText.Create(ExifToolPage);
  PromptText.Parent := ExifToolPage.Surface;
  PromptText.Top := InfoText.Top + InfoText.Height + ScaleY(18);
  PromptText.Width := ExifToolPage.SurfaceWidth;
  PromptText.Caption := 'How would you like to proceed?';
  PromptText.Font.Style := [fsBold];

  ExifToolDownloadRadio := TNewRadioButton.Create(ExifToolPage);
  ExifToolDownloadRadio.Parent := ExifToolPage.Surface;
  ExifToolDownloadRadio.Top := PromptText.Top + PromptText.Height + ScaleY(12);
  ExifToolDownloadRadio.Width := ExifToolPage.SurfaceWidth;
  ExifToolDownloadRadio.Height := ScaleY(17);
  ExifToolDownloadRadio.Caption := 'Download and install ExifTool automatically (recommended)';
  ExifToolDownloadRadio.Font.Style := [fsBold];
  ExifToolDownloadRadio.Checked := True;
  ExifToolDownloadRadio.OnClick := @ExifToolChoiceClick;

  DownloadText := TNewStaticText.Create(ExifToolPage);
  DownloadText.Parent := ExifToolPage.Surface;
  DownloadText.Left := ScaleX(17);
  DownloadText.Top := ExifToolDownloadRadio.Top + ExifToolDownloadRadio.Height + ScaleY(3);
  DownloadText.AutoSize := False;
  DownloadText.WordWrap := True;
  DownloadText.Width := ExifToolPage.SurfaceWidth - DownloadText.Left;
  DownloadText.Caption := 'Setup will download ExifTool 13.60 (~11 MB) from SourceForge and place it in the exiftool folder.';
  DownloadText.AdjustHeight;

  ExifToolManualRadio := TNewRadioButton.Create(ExifToolPage);
  ExifToolManualRadio.Parent := ExifToolPage.Surface;
  ExifToolManualRadio.Top := DownloadText.Top + DownloadText.Height + ScaleY(14);
  ExifToolManualRadio.Width := ExifToolPage.SurfaceWidth;
  ExifToolManualRadio.Height := ScaleY(17);
  ExifToolManualRadio.Caption := 'I''ll install ExifTool manually';
  ExifToolManualRadio.Font.Style := [fsBold];
  ExifToolManualRadio.OnClick := @ExifToolChoiceClick;

  ManualText := TNewLinkLabel.Create(ExifToolPage);
  ManualText.Parent := ExifToolPage.Surface;
  ManualText.Left := ScaleX(17);
  ManualText.Top := ExifToolManualRadio.Top + ExifToolManualRadio.Height + ScaleY(3);
  ManualText.AutoSize := False;
  ManualText.Width := ExifToolPage.SurfaceWidth - ManualText.Left;
  ManualText.Caption := 'Download ExifTool from <a href="https://exiftool.org/">exiftool.org</a>, install it, then run this setup again.';
  ManualText.OnLinkClick := @LinkLabelOnLinkClick;
  ManualText.AdjustHeight;

  ExifToolInstallSelected := True;
end;

procedure InitializeWizard;
begin
  CreateExifToolPage;

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
  Configured := GetIniString('General', 'exiftoolPATH', 'null', ExpandConstant('{app}\bin\config.ini'));
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

  Result := ExpandConstant('{app}\exiftool\exiftool.exe');
  if FileExists(Result) then
    Exit;

  Result := '';
end;

function ExifToolMissing(): Boolean;
begin
  Result := ExifToolLocation() = '';
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  if PageID = ExifToolPage.ID then
    Result := not ExifToolMissing()
  else
    Result := False;
end;

function ShouldInstallExifTool(): Boolean;
begin
  Result := ExifToolInstalled;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = ExifToolPage.ID then
    WizardForm.NextButton.Enabled := ExifToolInstallSelected;
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
    else if ExifToolInstallSelected then
      CheckProgressPage.SetText('Checking ExifTool...', 'Not found; will download and install.')
    else
      CheckProgressPage.SetText('Checking ExifTool...', 'Not found (required).');
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

  if (ExifPath = '') and (not ExifToolInstallSelected) then
  begin
    Result := 'ETSU requires ExifTool. Install it from exiftool.org, then run Setup again.';
    Exit;
  end;

  if CscPath = '' then
    MsgBox('csc.exe was not found. This is only needed to build ETSU from source; the installed app will still run.' + #13#10 + #13#10 +
           'If you plan to build from source, install .NET Framework 4.5 or later.', mbInformation, MB_OK);
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

function AddPathEntry(const Entry, MarkerName: string): Boolean;
var
  Added: Boolean;
  Marker: Cardinal;
begin
  Result := AddToUserPath(Entry, Added);
  if not Result then
    Exit;

  if Added then
  begin
    if not RegWriteDWordValue(HKCU, AppRegistryKey, MarkerName, 1) then
      Log('Could not record that Setup added ' + Entry + ' to PATH.');
  end
  else if not RegQueryDWordValue(HKCU, AppRegistryKey, MarkerName, Marker) then
    Log(Entry + ' was already on PATH; Setup will leave it in place on uninstall.');
end;

procedure FinishExifToolInstall;
var
  ExifDir, ConfigPath, Configured: string;
begin
  if not ExifToolInstalled then
    Exit;

  ExifDir := ExpandConstant('{app}\exiftool');
  if not FileExists(AddBackslash(ExifDir) + 'exiftool.exe') then
  begin
    Log('ExifTool was downloaded but exiftool.exe is missing after the file copy.');
    MsgBox('ExifTool was downloaded, but exiftool.exe was not copied into:' + #13#10 + ExifDir + #13#10 + #13#10 +
           'Install ExifTool manually or run Setup again.', mbError, MB_OK);
    Exit;
  end;

  if not RegWriteDWordValue(HKCU, AppRegistryKey, ExifToolFilesMarker, 1) then
    Log('Could not record that Setup installed ExifTool.');

  ConfigPath := ExpandConstant('{app}\bin\config.ini');
  Configured := GetIniString('General', 'exiftoolPATH', 'null', ConfigPath);
  if (CompareText(Configured, 'null') <> 0) and (not FileExists(Configured)) then
  begin
    if SetIniString('General', 'exiftoolPATH', 'null', ConfigPath) then
      Log('Reset invalid exiftoolPATH in config.ini to null.')
    else
      Log('Could not reset invalid exiftoolPATH in config.ini.');
  end;

  if not AddPathEntry(ExifDir, ExifToolPathMarker) then
  begin
    Log('Could not add the ExifTool folder to PATH.');
    MsgBox('ExifTool was installed in:' + #13#10 + ExifDir + #13#10 + #13#10 +
           'Setup could not add that folder to PATH. ETSU will still find ExifTool in its install folder.' + #13#10 + #13#10 +
           'To use exiftool from a terminal, add that folder to PATH yourself.', mbError, MB_OK);
  end;
end;

procedure RenameUninstaller;
var
  OldExe, NewExe, OldDat, NewDat, Value: string;
  Renamed: Boolean;
begin
  OldExe := ExpandConstant('{app}\bin\unins000.exe');
  NewExe := ExpandConstant('{app}\bin\uninstall.exe');
  OldDat := ExpandConstant('{app}\bin\unins000.dat');
  NewDat := ExpandConstant('{app}\bin\uninstall.dat');

  Renamed := False;
  if FileExists(OldExe) then
  begin
    if FileExists(NewExe) and (not DeleteFile(NewExe)) then
    begin
      Log('Could not replace the existing uninstall.exe.');
      Exit;
    end;
    Renamed := RenameFile(OldExe, NewExe);
    if not Renamed then
      Log('Could not rename unins000.exe to uninstall.exe.');
  end
  else if FileExists(NewExe) then
    Renamed := True;

  if not Renamed then
    Exit;

  if FileExists(OldDat) then
  begin
    if FileExists(NewDat) and (not DeleteFile(NewDat)) then
      Log('Could not replace the existing uninstall.dat.')
    else if not RenameFile(OldDat, NewDat) then
      Log('Could not rename unins000.dat to uninstall.dat.');
  end;

  if RegQueryStringValue(HKCU, UninstallKey, 'UninstallString', Value) then
  begin
    StringChangeEx(Value, 'unins000.exe', 'uninstall.exe', True);
    if not RegWriteStringValue(HKCU, UninstallKey, 'UninstallString', Value) then
      Log('Could not update UninstallString.');
  end;

  if RegQueryStringValue(HKCU, UninstallKey, 'QuietUninstallString', Value) then
  begin
    StringChangeEx(Value, 'unins000.exe', 'uninstall.exe', True);
    if not RegWriteStringValue(HKCU, UninstallKey, 'QuietUninstallString', Value) then
      Log('Could not update QuietUninstallString.');
  end;
end;

procedure RemoveLegacyBinPath;
var
  Marker: Cardinal;
begin
  if not RegQueryDWordValue(HKCU, AppRegistryKey, BinPathMarker, Marker) or (Marker = 0) then
    Exit;

  if RemoveFromUserPath(ExpandConstant('{app}\bin')) then
  begin
    if not RegDeleteValue(HKCU, AppRegistryKey, BinPathMarker) then
      Log('Could not remove the legacy bin PATH marker.');
  end
  else
    Log('Could not remove the legacy bin folder from PATH.');
end;

procedure CreateUninstallShortcut;
var
  LinkPath, TargetPath: string;
begin
  LinkPath := ExpandConstant('{app}\Uninstall.lnk');
  TargetPath := ExpandConstant('{app}\bin\uninstall.exe');
  if not FileExists(TargetPath) then
  begin
    Log('Skipping the Uninstall shortcut: uninstall.exe was not found.');
    Exit;
  end;

  try
    CreateShellLink(LinkPath, 'Uninstall ETSU', TargetPath, '', ExpandConstant('{app}\bin'), '', 0, 1);
  except
    Log('Could not create Uninstall.lnk: ' + GetExceptionMessage);
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  Error: string;
  Aborted: Boolean;
begin
  if CurStep = ssInstall then
  begin
    if ExifToolInstallSelected and ExifToolMissing() then
    begin
      while not InstallExifTool(Error, Aborted) do
      begin
        if Aborted then
          Abort;
        case MsgBox('ExifTool could not be installed. ETSU requires ExifTool.' + #13#10 + #13#10 +
                    Error + #13#10 + #13#10 +
                    'Retry?',
                    mbError, MB_RETRYCANCEL) of
          IDRETRY:
            begin
            end;
          IDCANCEL:
            Abort;
        end;
      end;
    end;
  end
  else if CurStep = ssPostInstall then
  begin
    FinishExifToolInstall;
    RenameUninstaller;
    RemoveLegacyBinPath;
    CreateUninstallShortcut;
  end;
end;

function InitializeUninstall: Boolean;
begin
  UninstallExifTool := True;
  Result := True;
end;

procedure RemoveExifToolFiles;
var
  ExifDir, ExePath, FilesDir: string;
begin
  ExifDir := ExpandConstant('{app}\exiftool');
  ExePath := AddBackslash(ExifDir) + 'exiftool.exe';
  FilesDir := AddBackslash(ExifDir) + 'exiftool_files';
  if FileExists(ExePath) and (not DeleteFile(ExePath)) then
    Log('Could not delete exiftool.exe.');
  if DirExists(FilesDir) and (not DelTree(FilesDir, True, True, True)) then
    Log('Could not delete exiftool_files.');
  RemoveDir(ExifDir);
end;

procedure InitializeUninstallProgressForm;
var
  UninstallPage: TNewNotebookPage;
  UninstallButton: TNewButton;
  ExifToolCheckbox: TNewCheckBox;
  InfoText: TNewStaticText;
  OriginalPageNameLabel, OriginalPageDescriptionLabel: string;
  OriginalCancelButtonEnabled: Boolean;
  OriginalCancelButtonModalResult: Integer;
  ctrl: TWinControl;
begin
  UninstallExifTool := True;
  if UninstallSilent then
    Exit;
  if not FileExists(ExpandConstant('{app}\exiftool\exiftool.exe')) then
    Exit;

  ctrl := UninstallProgressForm.StatusLabel;

  UninstallButton := TNewButton.Create(UninstallProgressForm);
  UninstallButton.Parent := UninstallProgressForm;
  UninstallButton.Left := UninstallProgressForm.CancelButton.Left - UninstallProgressForm.CancelButton.Width - ScaleX(10);
  UninstallButton.Top := UninstallProgressForm.CancelButton.Top;
  UninstallButton.Width := UninstallProgressForm.CancelButton.Width;
  UninstallButton.Height := UninstallProgressForm.CancelButton.Height;
  UninstallButton.TabOrder := UninstallProgressForm.CancelButton.TabOrder;
  UninstallButton.Caption := 'Uninstall';
  UninstallButton.ModalResult := mrOK;
  UninstallButton.Default := True;
  UninstallProgressForm.CancelButton.TabOrder := UninstallButton.TabOrder + 1;

  UninstallPage := TNewNotebookPage.Create(UninstallProgressForm);
  UninstallPage.Notebook := UninstallProgressForm.InnerNotebook;
  UninstallPage.Parent := UninstallProgressForm.InnerNotebook;
  UninstallPage.Align := alClient;
  UninstallProgressForm.InnerNotebook.ActivePage := UninstallPage;

  InfoText := TNewStaticText.Create(UninstallProgressForm);
  InfoText.Parent := UninstallPage;
  InfoText.Left := ctrl.Left;
  InfoText.Top := ctrl.Top;
  InfoText.Width := ctrl.Width;
  InfoText.AutoSize := False;
  InfoText.WordWrap := True;
  InfoText.Caption := 'Setup installed ExifTool in:' + #13#10 +
    ExpandConstant('{app}\exiftool') + #13#10#13#10 +
    'ETSU will be uninstalled. Leave the option below checked to remove ExifTool and its PATH entry, or clear it to keep them.';
  InfoText.AdjustHeight;

  ExifToolCheckbox := TNewCheckBox.Create(UninstallProgressForm);
  ExifToolCheckbox.Parent := UninstallPage;
  ExifToolCheckbox.Left := ctrl.Left;
  ExifToolCheckbox.Top := InfoText.Top + InfoText.Height + ScaleY(16);
  ExifToolCheckbox.Width := ctrl.Width;
  ExifToolCheckbox.Height := ScaleY(17);
  ExifToolCheckbox.Caption := 'Uninstall ExifTool (recommended)';
  ExifToolCheckbox.Checked := True;

  OriginalPageNameLabel := UninstallProgressForm.PageNameLabel.Caption;
  OriginalPageDescriptionLabel := UninstallProgressForm.PageDescriptionLabel.Caption;
  OriginalCancelButtonEnabled := UninstallProgressForm.CancelButton.Enabled;
  OriginalCancelButtonModalResult := UninstallProgressForm.CancelButton.ModalResult;

  UninstallProgressForm.PageNameLabel.Caption := 'Uninstall ETSU';
  UninstallProgressForm.PageDescriptionLabel.Caption := 'Choose whether to remove ExifTool.';
  UninstallProgressForm.CancelButton.Enabled := True;
  UninstallProgressForm.CancelButton.ModalResult := mrCancel;

  if UninstallProgressForm.ShowModal <> mrOK then
    Abort;

  UninstallExifTool := ExifToolCheckbox.Checked;
  UninstallButton.Visible := False;
  UninstallProgressForm.PageNameLabel.Caption := OriginalPageNameLabel;
  UninstallProgressForm.PageDescriptionLabel.Caption := OriginalPageDescriptionLabel;
  UninstallProgressForm.CancelButton.Enabled := OriginalCancelButtonEnabled;
  UninstallProgressForm.CancelButton.ModalResult := OriginalCancelButtonModalResult;
  UninstallProgressForm.InnerNotebook.ActivePage := UninstallProgressForm.InstallingPage;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Marker: Cardinal;
begin
  if CurUninstallStep <> usUninstall then
    Exit;

  if RegQueryDWordValue(HKCU, AppRegistryKey, BinPathMarker, Marker) and (Marker <> 0) then
  begin
    if RemoveFromUserPath(ExpandConstant('{app}\bin')) then
    begin
      if not RegDeleteValue(HKCU, AppRegistryKey, BinPathMarker) then
        Log('Could not remove the ETSU bin PATH marker.');
    end
    else
      Log('Could not remove the ETSU bin folder from PATH.');
  end;

  if not UninstallExifTool then
  begin
    RegDeleteKeyIfEmpty(HKCU, AppRegistryKey);
    Exit;
  end;

  if FileExists(ExpandConstant('{app}\exiftool\exiftool.exe')) or
     DirExists(ExpandConstant('{app}\exiftool')) then
    RemoveExifToolFiles;

  if RegQueryDWordValue(HKCU, AppRegistryKey, ExifToolFilesMarker, Marker) and (Marker <> 0) then
  begin
    if not RegDeleteValue(HKCU, AppRegistryKey, ExifToolFilesMarker) then
      Log('Could not remove the ExifTool files marker.');
  end;

  if RemoveFromUserPath(ExpandConstant('{app}\exiftool')) then
  begin
    if RegQueryDWordValue(HKCU, AppRegistryKey, ExifToolPathMarker, Marker) and (Marker <> 0) then
    begin
      if not RegDeleteValue(HKCU, AppRegistryKey, ExifToolPathMarker) then
        Log('Could not remove the ExifTool PATH marker.');
    end;
    RegDeleteKeyIfEmpty(HKCU, AppRegistryKey);
  end
  else
    Log('Could not remove the ExifTool folder from PATH.');
end;
