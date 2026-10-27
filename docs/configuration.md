# Configuration

ETSU reads `%APPDATA%\etsu\.etsu`. The installer creates the file from a bundled default when it is missing. The format is INI-like: section and key names are case-insensitive, and lines starting with `#` or `;` are comments. Each action loads the file when it starts, so restart the action after editing it.

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
| clean, date, read | `logCount` | `10` | Number of logs to keep per action. Older logs are deleted after a new log is written. Invalid or negative values use `10`. |

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

ETSU writes log files as `<action>_<YYYYMMDD>_<HHMMSS>.log` in the log directory.

- `read` writes a log when logging is enabled.
- `clean` and `date` write a log after success, failure, or cancellation.
- Clean logs list the tags reported as deleted. Read logs contain the raw ExifTool output.
- Clean and date logs include the detected ExifTool version and path.
- `logCount` cleanup runs after each new log is written.

The log panel in the task window shows the last 5,000 lines. The log file keeps the full run.

## Temp files

Clean and Set date copy files to `%LOCALAPPDATA%\etsu\tmp\etsu_<random>` and remove that directory when the run ends. The `.bak` files used during the swap are created next to the original file, not in the temp directory.

## ExifTool

ETSU does not bundle ExifTool. Install it from [exiftool.org](https://exiftool.org/) and use one of these options:

1. Set `exiftoolPATH` to the full path of `exiftool.exe`.
2. Put `exiftool.exe` on PATH.
3. Put `exiftool.exe` next to `etsu.exe`.

ETSU uses `exiftoolPATH` when it is not `null`. If that path does not exist, ETSU shows an error and does not fall back to PATH. Clean and date logs show the detected ExifTool version and path.

[Documentation index](index.md)
