# Configuration

ETSU reads `%APPDATA%\etsu\.etsu`. The installer creates the file from a bundled default when it is missing. The format is INI-like: section and key names are case-insensitive, and lines starting with `#` or `;` are comments. ETSU loads the file at startup, so restart the app after editing it.

## Default file

```ini
[General]
exiftoolPATH=null

[clean]
log=true
logCount=10

[date]
log=true
logCount=10

[read]
log=false
logCount=10
```

## Keys

| Section | Key | Default | Effect |
| --- | --- | --- | --- |
| General | `exiftoolPATH` | `null` | `null` searches PATH, then the directory containing `etsu.exe`. Any other value must be a full path to an existing `exiftool.exe`. |
| clean, date, read | `log` | `true` for clean and date, `false` for read | `true` enables logging. Any other value disables it. |
| clean, date, read | `logCount` | `10` | Number of logs to keep per tool. Older logs are deleted after a new log is written. Invalid or negative values use `10`. |

If the file or a section is missing, ETSU uses the defaults.

`exiftoolPATH` must not contain double quotes. ETSU rejects a value that contains a quote. Do not wrap the path in quotes, even when it contains spaces.

## Paths

| Purpose | Path |
| --- | --- |
| Config | `%APPDATA%\etsu\.etsu` |
| Logs | `%LOCALAPPDATA%\etsu\logs\etsu\` |
| Temp workspace | `%LOCALAPPDATA%\etsu\tmp\` |
| Default install directory | `%LOCALAPPDATA%\Programs\ETSU` |

The uninstaller removes `%APPDATA%\etsu`, `%LOCALAPPDATA%\etsu\logs`, and `%LOCALAPPDATA%\etsu\tmp`. Back up `.etsu` before uninstalling if you want to keep it.

## Logs

ETSU writes log files as `<tool>_<YYYYMMDD>_<HHMMSS>.log` in the log directory.

- `read` writes a log when logging is enabled.
- `clean` and `date` write a log after a successful run. A failed clean or date prints the error to the console and does not write a log.
- Clean logs list the tags reported as deleted. Read logs contain the raw ExifTool output.
- `logCount` cleanup runs after each new log is written.

## Temp files

`clean` and `date` copy files to `%LOCALAPPDATA%\etsu\tmp\etsu_<random>` and remove that directory when the run ends. The `.bak` files used during the swap are created next to the original file, not in the temp directory.

## ExifTool

ETSU does not bundle ExifTool. Install it from [exiftool.org](https://exiftool.org/) and use one of these options:

1. Set `exiftoolPATH` to the full path of `exiftool.exe`.
2. Put `exiftool.exe` on PATH.
3. Put `exiftool.exe` next to `etsu.exe`.

ETSU uses `exiftoolPATH` when it is not `null`. If that path does not exist, ETSU stops with an error and does not fall back to PATH. The header shows the detected ExifTool version.

[Documentation index](index.md)
