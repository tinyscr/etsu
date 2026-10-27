# ETSU

ETSU reads, cleans, and sets file dates with [ExifTool](https://exiftool.org/). It runs from the Windows Explorer context menu. Published by TinyScripts.

## Install

Run the installer in `dist\`, for example `dist\etsu_v0.1.0_installer.exe`. The wizard asks for the install location and shows the MIT license. It installs under `%LOCALAPPDATA%\Programs\ETSU` by default and does not request administrator rights.

The installer copies `etsu.exe`, creates `%APPDATA%\etsu\.etsu` when missing, registers the ETSU submenu for files, adds an entry to Settings > Apps as ETSU, and warns when it cannot find ExifTool.

## Use

Right-click a file, open the ETSU submenu, and choose Read metadata, Clean metadata, or Set date. On Windows 11, the submenu is under Show more options.

Read opens a metadata table and stays open. Clean and Set date open a smaller window with a progress bar and a collapsible log; they close 1.5 seconds after success when the log is collapsed. Errors keep the window open. The window follows the Windows light or dark theme. See [Usage](docs/usage.md).

## Documentation

- [Documentation index](docs/index.md)
- [Installation](docs/installation.md)
- [Usage](docs/usage.md)
- [Configuration](docs/configuration.md)
- [How it works](docs/how-it-works.md)
- [Troubleshooting](docs/troubleshooting.md)
- [Development](docs/development.md)

## Build

```bat
build.bat
```

Builds `dist\etsu_v0.1.0_installer.exe`. Pass a version to override the default:

```bat
build.bat 0.2.0
```

Go 1.27+ and Inno Setup 6 are required. See [Development](docs/development.md) for the source layout, icon resource, and release workflow.

## License

MIT. See [LICENSE](LICENSE). Source: [github.com/tinyscr/etsu](https://github.com/tinyscr/etsu).
