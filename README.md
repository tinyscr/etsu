# ETSU

ETSU reads, cleans, and sets file dates with [ExifTool](https://exiftool.org/). It runs from the Windows Explorer context menu.

## Install

Run the installer in `dist\`, for example `dist\v0.1.0_installer.exe`. The wizard asks for the install location and shows the MIT license. It installs under `%LOCALAPPDATA%\Programs\ETSU` by default and does not request administrator rights.

The installer copies `etsu.exe`, creates `config.ini` in the install directory when missing, registers the ETSU submenu for files, adds an entry to Settings > Apps as ETSU, and warns when it cannot find ExifTool. Uninstall from Settings > Apps.

## Use

Right-click a file, open the ETSU submenu, and choose Read metadata, Clean metadata, or Set date. On Windows 11, the submenu is under Show more options.

Read shows the ExifTool output as a tag/value text view. Clean and Set date show the operation log in the same window. The window stays open until you click OK. Closing it during a run cancels the operation. See [Usage](docs/usage.md).

## Documentation

See [docs](docs/README.md) for usage, configuration, internals, troubleshooting, and development.

## Build

```powershell
.\build.ps1
```

Builds `dist\v0.1.0_installer.exe` and `dist\SHA256SUMS.txt`. Pass a version to override the default:

```powershell
.\build.ps1 0.2.0
```

Run the regression suite with:

```powershell
.\test.ps1
```

To build, install .NET Framework 4.5 or later and Inno Setup 6. See [Development](docs/development.md) for the source layout, build steps, and release workflow.

## License

MIT. See [LICENSE](LICENSE).
