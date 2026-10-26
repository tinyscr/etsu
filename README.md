# ETSU

ETSU reads, cleans, and sets file dates with [ExifTool](https://exiftool.org/). It is a Windows command-line tool with a per-user installer and an Explorer submenu. Published by TinyScripts.

## Install

Run the installer in `dist\`, for example `dist\etsu_v0.1.0_installer.exe`. The wizard asks for the install location and shows the MIT license. It installs under `%LOCALAPPDATA%\Programs\ETSU` by default and does not request administrator rights.

The installer copies `etsu.exe` to the install directory, creates `%APPDATA%\etsu\.etsu` when missing, registers the ETSU submenu for files, adds an entry to Settings > Apps as ETSU, and warns when it cannot find ExifTool.

On Windows 11, the Explorer menu is under Show more options. See [Installation](docs/installation.md) for upgrade, silent install, and uninstall details.

## Use

The installer does not add ETSU to PATH. Add `%LOCALAPPDATA%\Programs\ETSU` to PATH, or call `etsu.exe` by full path. The examples below assume it is on PATH.

```bat
etsu read "C:\Photos\image.jpg"
etsu clean "C:\Photos\image.jpg"
etsu date "C:\Photos\image.jpg"
```

`read` takes one file. `clean` and `date` take one or more. The Explorer menu runs the same commands on the selected file.

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

MIT. See [LICENSE](LICENSE).
