# Development

## Requirements

- 64-bit Windows.
- .NET Framework 4.5 or later. The build uses the `csc.exe` that ships with .NET Framework, so it needs no SDK or NuGet packages.
- [Inno Setup](https://jrsoftware.org/isinfo.php) 6.

The app is a C# WinForms program. It uses `System.Windows.Forms`, `System.Drawing`, and the .NET Framework base libraries. It does not use a bundled runtime or WebView2.

## Version

The root `version` file is the only version source. It contains a plain `X.Y.Z` value with no leading `v`. `build.ps1` and `test.ps1` read it. `build.ps1` generates the assembly version metadata and passes the value to Inno Setup.

To release, bump `version`, commit, and push to `main`. The release workflow creates the tag and a draft release.

## Build

```powershell
.\build.ps1
```

`build.ps1` compiles every `.cs` file under `src\`, embeds `src\Assets\icon.ico` and `src\Assets\etsu.manifest`, and writes `bin\etsu.exe`. It then runs the Inno Setup compiler and writes:

```text
dist\v<version>_installer.exe
dist\SHA256SUMS.txt
```

`build.bat` and `test.bat` are local convenience triggers. They are ignored by Git; the real scripts are `build.ps1` and `test.ps1`.

Install Inno Setup 6 with:

```powershell
winget install JRSoftware.InnoSetup --scope user
```

The build script looks for `ISCC.exe` in `%LOCALAPPDATA%\Programs\Inno Setup 6`, `%ProgramFiles(x86)%\Inno Setup 6`, and `%ProgramFiles%\Inno Setup 6`.

## Tests

```powershell
.\test.ps1
```

`test.ps1` compiles the app sources and the regression tests into `bin\tests\etsu-tests.exe`, then runs them. The suite covers path handling, command-line parsing, config parsing, date input, log formatting, read formatting, and the copy, verify, and replace pipeline.

## Source layout

```text
etsu/
  version
  build.ps1
  test.ps1
  src/
    App/
    Assets/
      etsu.manifest
      icon.ico
    Installer/
      default.ini
      etsu.iss
    Ops/
    Properties/
      AssemblyInfo.cs
    Ui/
  tests/
    TestRunner.cs
    TestSupport.cs
  docs/
  README.md
  LICENSE
  .github/workflows/release.yml
```

- `src\App`: entry point, paths, config access, ExifTool lookup, process execution, file copy, hashing, and log helpers.
- `src\Ops`: read, clean, date, and the shared copy, verify, and replace pipeline.
- `src\Ui`: task window, date dialog, dark controls, and theme helpers.
- `src\Assets`: app icon and manifest. The manifest declares Common Controls 6.0, PerMonitorV2 DPI awareness, and `asInvoker`.
- `src\Installer`: Inno Setup script and the default config seeded on install.
- `src\Properties\AssemblyInfo.cs`: assembly metadata.
- `tests`: regression tests.
- `docs`: these guides.

Build outputs are `bin\etsu.exe`, `obj\Version.g.cs`, `bin\tests\etsu-tests.exe`, and the files under `dist\`. These directories are ignored by Git.

## Installer

`src\Installer\etsu.iss` builds the per-user installer from `bin\etsu.exe`. `build.ps1` passes the version from the root `version` file. A direct Inno compile without `AppVersion` fails. The installer seeds `config.ini` only when the file is missing, creates `logs\etsu` and `tmp`, and registers the Explorer menu under HKCU. The uninstaller removes `config.ini`, `logs`, and `tmp`.

## Icon and manifest resources

`build.ps1` passes the icon and manifest directly to `csc.exe`. The app does not use a separate resource compiler. Change `src\Assets\icon.ico` or `src\Assets\etsu.manifest` and rebuild.

The manifest requests Common Controls 6.0 for themed controls, PerMonitorV2 DPI awareness, and the current user's execution level.

## Release workflow

`.github/workflows/release.yml` runs when `version` changes on `main`. It validates the version, fails if the tag already exists, runs the regression suite, installs Inno Setup when needed, calls `build.ps1`, verifies the installer checksum, and creates the tag and a draft GitHub release containing:

```text
v<version>_installer.exe
SHA256SUMS.txt
```

A manual run reads the same `version` file. The workflow runs on GitHub Actions. A local clone can build the same app and installer with `build.ps1`.

## Local check

Build the app and run it against a copy of a file:

```powershell
bin\etsu.exe read "C:\Photos\copy.jpg"
```

The window is the only output. Use copies when you test Clean or Set date. For normal use, start the action from the Explorer menu.

Run `.\test.ps1` after code changes.

[Documentation](README.md)
