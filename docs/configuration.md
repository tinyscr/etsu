# Configuration

ETSU reads `config.ini` from the install directory. The installer creates the file from a bundled default when it is missing. The format is INI-like: section and key names are case-insensitive, and lines starting with `#` or `;` are comments. Each action loads the file when it starts, so restart the action after editing it.

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
| Config | `<install>\config.ini` |
| Logs | `<install>\logs\etsu\` |
| App temp directory | `<install>\tmp\` |
| Operation workspace | Beside the original file (`<original folder>\.etsu_<random>\`) |
| Default install directory | `%LOCALAPPDATA%\Programs\ETSU` |

ETSU creates the log and temp directories at startup and checks that the temp directory is writable. The operation workspace is separate from the app temp directory because the replacement step needs the workspace and the original file on the same volume.

The uninstaller removes `config.ini`, `logs`, and `tmp` from the install directory. Back up `config.ini` before uninstalling if you want to keep it.

## Logs

ETSU writes log files as `<action>_<YYYYMMDD>_<HHMMSS>.log` in the log directory.

- `read` writes a log when logging is enabled.
- `clean` and `date` write a log after success, failure, or cancellation.
- Clean logs list the tags reported as deleted. Read logs contain the formatted ExifTool output.
- Clean and date batch logs include every file in the batch, the per-file five-stage pipeline, the ExifTool command and output, and a final summary.
- Clean and date logs include the detected ExifTool version.
- `logCount` cleanup runs after each new log is written.

The task window shows the short batch summary for Clean and Set date and adds a final `Failed: ...` or `Cancelled.` line when needed. The log file contains the window lines plus the raw per-file detail when logging is enabled.

## Temp files

Clean and Set date create a temporary directory beside each original file, copy into it, and remove it when the run ends. A batch keeps every selected file's temp copy alive until stage 4, so it needs temporary disk space for the whole selection. Keeping the copies on the same volume keeps each replacement on one volume. The `.bak` file used during replacement is also created next to the original file.

A successful run deletes the `.bak` file. ETSU does not automatically restore the original if a later step fails, so a failed replacement can leave a `.bak` file behind. See [Troubleshooting](troubleshooting.md).

## ExifTool

ETSU does not bundle ExifTool. Install it from [exiftool.org](https://exiftool.org/) and use one of these options:

1. Set `exiftoolPATH` to the full path of `exiftool.exe`.
2. Put `exiftool.exe` on PATH.
3. Put `exiftool.exe` next to `etsu.exe`.

ETSU uses `exiftoolPATH` when it is not `null`. If that path does not exist, ETSU shows an error and does not fall back to PATH. Clean and date logs show the detected ExifTool version.

[Documentation](README.md)
