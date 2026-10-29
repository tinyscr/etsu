# Usage

ETSU runs from the Explorer context menu. It has no file picker. Read works on one selected file. Clean and Set date work on one or more selected files, and the executable accepts the three action names that the menu passes to it.

## Open the menu

1. Select one or more files in File Explorer.
2. Right-click the selection.
3. Choose ETSU, then Read metadata, Clean metadata, or Set date.
4. On Windows 11, open Show more options first.

The menu is registered for files, not folders. Read is available for one selected file; Clean and Set date are available for multiple selected files.

## Actions

| Action | Effect |
| --- | --- |
| Read metadata | Runs ExifTool on one file and shows the output as a tag/value text view. The file is not modified. |
| Clean metadata | Removes metadata from each selected file with `exiftool -all=`. |
| Set date | Sets the EXIF dates and the filesystem modification and creation times on each selected file. |

Clean removes metadata permanently. It does not keep a backup after a successful run. Work on a copy if you need the original metadata. A batch copies every selected file to its own temp workspace before processing, so it needs temporary disk space for the whole batch.

## Task window

Read, Clean, and Set date open the task window. It shows the operation log, an Open log button, and an OK button. The operation runs in the background, so the window stays responsive while ExifTool runs.

- The log is read-only and selectable. It scrolls to the newest line. Use Ctrl+C to copy selected text.
- Clean and Set date show one five-stage sequence for the batch. The header lists every selected file, and stage 2 lists each file with the tags removed or the date result. Read shows the formatted metadata output.
- The log file keeps the detailed per-file pipeline, the ExifTool command, and the ExifTool output. The window shows the shorter batch summary.
- A failed file does not stop the remaining files. The window marks the failure under the current stage, and the final summary reports how many succeeded.
- The window has no progress bar. The log reports the current phase.
- The window stays open after the operation finishes. OK is enabled when the operation is done. Open log is enabled when ETSU wrote a log file.
- Closing the window during a run cancels the operation. ETSU waits for the operation to stop before closing. A cancelled batch does not start the remaining files or stages.
- Use Ctrl+plus, Ctrl+minus, or Ctrl+wheel to change the log text size. Ctrl+0 resets it.
- The window and its dialogs use a dark theme.

## Set date

Set date opens one fixed-size dialog before the batch starts. Choose the day, month, year, hour, minute, second, and AM/PM value. The dialog starts at the current local date and time, and the same value is applied to every selected file.

The day list is clamped to the last valid day when you change the month or year. Click OK to apply the value, or click Cancel or press Esc to close the dialog without changing any file.

ETSU sets the EXIF dates (`DateTimeOriginal`, `CreateDate`, `ModifyDate`) and the filesystem modification and creation times. It applies the value in the local time zone.

## Errors and cancellation

When a file fails, the window shows a `[FAILED]` line under the current stage and the log file includes the `[ABORT]` detail and ExifTool output. A batch skips that file in later stages and continues with the others. The final summary reports how many files succeeded and how many failed.

ETSU does not automatically restore the original after a failure. If the failure happens after the replacement step, `<file>.bak` remains next to the original. Inspect both files before you remove or rename either one. See [Troubleshooting](troubleshooting.md).

Closing the window during a run cancels the operation. If cancellation happens before the replacement step, the original file is unchanged. Once replacement starts, ETSU does not roll back. If the replacement step fails, a `.bak` file can remain. A cancelled batch does not start the remaining files or stages.

Logs are covered in [Configuration](configuration.md). The copy, verify, and replace steps are covered in [How it works](how-it-works.md).

[Documentation](README.md)
