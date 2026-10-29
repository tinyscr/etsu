# ETSU documentation

ETSU is a Windows app that reads metadata, removes metadata, and sets EXIF dates and filesystem timestamps for one file. It runs from the Explorer context menu, calls ExifTool for the metadata work, and does not bundle it.

## Guides

| Guide | Contents |
| --- | --- |
| [Usage](usage.md) | Explorer menu, task window, log, and date dialog. |
| [Configuration](configuration.md) | Config keys, logs, temp files, and ExifTool lookup. |
| [How it works](how-it-works.md) | Read, clean, date, copy, verify, and replace. |
| [Troubleshooting](troubleshooting.md) | Common errors, limitations, and recovery notes. |
| [Development](development.md) | Versioning, build, tests, source layout, installer, and release workflow. |

## Requirements

- 32-bit or 64-bit Windows.
- [ExifTool](https://exiftool.org/), either on PATH, next to `etsu.exe`, or at a path set in `config.ini`. Use a 32-bit ExifTool build on 32-bit Windows.
- .NET Framework 4.5 or later.
- Inno Setup 6 only when building from source.

ETSU installs for the current user. It does not need administrator rights and is not a portable app.

Install and build commands are in the top-level [README](../README.md).
