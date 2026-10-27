# ETSU documentation

ETSU is a Windows app that reads metadata, removes metadata, and sets EXIF dates and filesystem timestamps for one file. It runs from the Explorer context menu, calls ExifTool for the metadata work, and does not bundle it.

## Guides

| Guide | Contents |
| --- | --- |
| [Installation](installation.md) | Install, upgrade, silent install, and uninstall. |
| [Usage](usage.md) | Explorer menu, progress window, log panel, date dialog, and auto-close. |
| [Configuration](configuration.md) | Config keys, logs, temp files, and ExifTool lookup. |
| [How it works](how-it-works.md) | GUI reporter, read, clean, date, copy, verify, swap, and rollback. |
| [Troubleshooting](troubleshooting.md) | Common errors, limitations, and recovery notes. |
| [Development](development.md) | Build, source layout, installer, icon, and release workflow. |

## Requirements

- 64-bit Windows.
- [ExifTool](https://exiftool.org/), either on PATH, next to `etsu.exe`, or at a path set in `%APPDATA%\etsu\.etsu`.
- Go 1.27 or newer and Inno Setup 6 only for building from source.

ETSU installs for the current user. It does not need administrator rights and is not a portable app.

Source: [github.com/tinyscr/etsu](https://github.com/tinyscr/etsu).
