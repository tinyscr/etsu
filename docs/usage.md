# Usage

ETSU has three commands. The Explorer menu runs the same commands on the selected file.

| Command | Files | Action |
| --- | --- | --- |
| `etsu read FILE` | One | Prints the raw ExifTool output. The file is not modified. |
| `etsu clean FILE...` | One or more | Removes metadata with `exiftool -all=`. |
| `etsu date FILE...` | One or more | Sets EXIF dates and filesystem timestamps. |

Command names are case-insensitive. No command or an unknown command prints `Usage: etsu read|clean|date [FILE...]` and exits with code 1.

`clean` removes metadata permanently. It does not keep a backup after a successful run. Work on a copy if you need the original metadata.

The installer does not add the install directory to PATH. Add `%LOCALAPPDATA%\Programs\ETSU` to PATH, or call `etsu.exe` by full path. Quote paths that contain spaces.

ETSU has no file picker. Pass paths on the command line or use the Explorer menu.

## Examples

```bat
etsu read "C:\Photos\2026 trip\IMG_0001.jpg"
etsu clean "C:\Photos\IMG_0001.jpg" "C:\Photos\IMG_0002.jpg"
etsu date "C:\Photos\IMG_0001.jpg"
```

`read` takes exactly one path. `clean` and `date` take one or more. They check every path before they start; a missing file stops the command with exit code 2.

## Date input

`etsu date` prompts for a date:

```text
Enter date (DD-MM-YYYY HH:MM:SS):
```

ETSU parses the date in the local time zone and applies the same date to every selected file. Empty input cancels without changes and exits with code 0. An invalid date exits with code 1.

## Explorer menu

The ETSU submenu contains Read metadata, Clean metadata, and Set date. It is registered for one selected file. On Windows 11, open Show more options to reach it.

- Read metadata prints the ExifTool dump and waits for Enter.
- Clean metadata starts immediately. It does not ask for confirmation.
- Set date prompts for a date.

Use the CLI to process several files in one run. Successful runs end with `Press enter to exit.` The console window stays open until you press Enter.

## Output and exit codes

`read` prints the raw ExifTool output. `clean` and `date` print the selected files, the step progress, and a final count.

| Code | Meaning |
| --- | --- |
| 0 | Success. Also returned when you leave the date prompt empty. |
| 1 | Usage error, ExifTool not found, invalid date, or an operation failure. |
| 2 | Wrong number of file paths, or a path does not exist. |

Logs and configuration are covered in [Configuration](configuration.md). The copy, verify, and swap steps are covered in [How it works](how-it-works.md).

[Documentation index](index.md)
