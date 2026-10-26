# ETSU documentation

ETSU is a Windows command-line tool that reads metadata from one file, removes metadata from one or more files, and sets EXIF dates and filesystem timestamps. It calls ExifTool for the metadata work and does not bundle it. The installer adds an Explorer submenu for the same actions.

## Guides

| Guide | Contents |
| --- | --- |
| [Installation](installation.md) | Install, upgrade, silent install, and uninstall. |
| [Usage](usage.md) | Commands, examples, date input, Explorer menu, and exit codes. |
| [Configuration](configuration.md) | Config keys, logs, temp files, and ExifTool lookup. |
| [How it works](how-it-works.md) | Read, clean, date, copy, verify, swap, and rollback. |
| [Troubleshooting](troubleshooting.md) | Common errors, limitations, and recovery notes. |
| [Development](development.md) | Build, source layout, installer, icon, and release workflow. |

## Requirements

- 64-bit Windows.
- [ExifTool](https://exiftool.org/), either on PATH, next to `etsu.exe`, or at a path set in `%APPDATA%\etsu\.etsu`.
- Go 1.27 or newer and Inno Setup 6 only for building from source.

ETSU installs for the current user. It does not need administrator rights and is not a portable app.
