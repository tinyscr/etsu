# Usage

ETSU runs from the Explorer context menu. It has no file picker. Each action works on the file you right-clicked, and the executable accepts the three action names that the menu passes to it.

## Open the menu

1. Right-click a file in File Explorer.
2. Choose ETSU, then Read metadata, Clean metadata, or Set date.
3. On Windows 11, open Show more options first.

The menu is registered for files, not folders, and for one selected file at a time.

## Actions

| Action | Effect |
| --- | --- |
| Read metadata | Runs ExifTool and shows the output as a tag/value text view. The file is not modified. |
| Clean metadata | Removes metadata with `exiftool -all=`. |
| Set date | Sets the EXIF dates and the filesystem modification and creation times. |

Clean removes metadata permanently. It does not keep a backup after a successful run. Work on a copy if you need the original metadata.

## Task window

Read, Clean, and Set date open the task window. It shows the operation log, an Open log button, and an OK button. The operation runs in the background, so the window stays responsive while ExifTool runs.

- The log is read-only and selectable. It scrolls to the newest line. Use Ctrl+C to copy selected text.
- The log shows each pipeline step for Clean and Set date. Read shows the formatted metadata output.
- The window has no progress bar. The log reports the current phase.
- The window stays open after the operation finishes. OK is enabled when the operation is done. Open log is enabled when ETSU wrote a log file.
- Closing the window during a run cancels the operation. ETSU waits for the operation to stop before closing.
- Use Ctrl+plus, Ctrl+minus, or Ctrl+wheel to change the log text size. Ctrl+0 resets it.
- The window uses a dark theme.

## Set date

Set date opens a fixed-size dialog before the task starts. Choose the day, month, year, hour, minute, second, and AM/PM value. The dialog starts at the current local date and time.

The day list is clamped to the last valid day when you change the month or year. Click OK to apply the value, or click Cancel or press Esc to close the dialog without changing the file.

ETSU sets the EXIF dates (`DateTimeOriginal`, `CreateDate`, `ModifyDate`) and the filesystem modification and creation times. It applies the value in the local time zone.

## Errors and cancellation

When an action fails, the window shows `Failed: ...` and the failing step includes an `[ABORT]` line in the log. The window stays open.

ETSU does not automatically restore the original after a failure. If the failure happens after the replacement step, `<file>.bak` remains next to the original. Inspect both files before you remove or rename either one. See [Troubleshooting](troubleshooting.md).

Closing the window during a run cancels the operation. If cancellation happens before the replacement step, the original file is unchanged. Once replacement starts, ETSU does not roll back. If the replacement step fails, a `.bak` file can remain.

Logs are covered in [Configuration](configuration.md). The copy, verify, and replace steps are covered in [How it works](how-it-works.md).

[Documentation](README.md)
