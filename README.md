# ETSU

ETSU reads, cleans, and sets file dates with [ExifTool](https://exiftool.org/). It runs from the Windows Explorer context menu.

## Install

Run `dist\v0.2.0_installer_x64.exe` on 64-bit Windows or `dist\v0.2.0_installer_x86.exe` on 32-bit Windows. Both install the same app; install one. The installer uses a dark wizard, shows the MIT license, and checks .NET Framework 4.5 (required), `csc.exe` (optional build tool), and ExifTool before installing. It installs under `%LOCALAPPDATA%\Programs\ETSU` by default and does not request administrator rights.

The installer copies `etsu.exe`, creates `config.ini` in the install directory when missing, registers the ETSU submenu for files, adds an entry to Settings > Apps as ETSU, and lets you continue if ExifTool is missing. Uninstall from Settings > Apps.

## Use

Select one or more files, right-click, open the ETSU submenu, and choose Read metadata, Clean metadata, or Set date. On Windows 11, the submenu is under Show more options. Read works on one file; Clean and Set date work on the whole selection.

Read shows the ExifTool output as a tag/value text view. Clean and Set date show one five-stage batch summary; the log file keeps the detailed per-file pipeline and ExifTool output. The window stays open until you click OK. Closing it during a run cancels the operation. See [Usage](docs/usage.md).

## Documentation

See [docs](docs/README.md) for usage, configuration, internals, troubleshooting, and development.

## Build

`build.ps1` reads the root `version` file and builds one installer. The default architecture is x64.

```powershell
.\build.ps1 -Architecture x64
.\build.ps1 -Architecture x86
```

It writes `dist\v<version>_installer_x64.exe` or `dist\v<version>_installer_x86.exe` and the matching `dist\SHA256SUMS_x64.txt` or `dist\SHA256SUMS_x86.txt`.

To release, bump `version`, commit, and push to `main`. The release workflow builds both installers and creates the tag and a draft release.

Run the regression suite with:

```powershell
.\test.ps1
```

To build, install .NET Framework 4.5 or later and Inno Setup 6 or later. See [Development](docs/development.md) for the source layout, build steps, and release workflow.

## License

MIT. See [LICENSE](LICENSE).
