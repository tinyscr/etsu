# Usage

ETSU runs from the Explorer context menu. It has no command-line interface yet and no file picker. Each action works on the file you right-clicked.

## Open the menu

1. Right-click a file in File Explorer.
2. Choose ETSU, then Read metadata, Clean metadata, or Set date.
3. On Windows 11, open Show more options first.

The menu is registered for files, not folders, and for one selected file at a time.

## Actions

| Action | Effect |
| --- | --- |
| Read metadata | Runs ExifTool and shows the raw output. The file is not modified. |
| Clean metadata | Removes metadata with `exiftool -all=`. |
| Set date | Sets the EXIF dates and the filesystem modification and creation times. |

Clean removes metadata permanently. It does not keep a backup after a successful run. Work on a copy if you need the original metadata.

## Task window

Each action opens a small window with a status line, a progress bar, a log toggle, and two buttons.

The progress bar follows the operation phases: copy, metadata work, verify, replace, finish. Read has a shorter run and jumps from reading to done.

The log panel shows the raw operation log. It is read-only, selectable, and scrolls to the newest line. The panel keeps the last 5,000 lines. The log file keeps the full run when logging is enabled.

- **Read metadata** opens with the log expanded and stays open after it finishes.
- **Clean metadata** and **Set date** open with the log collapsed. They close 1.5 seconds after success if the log is still collapsed.
- Expanding the log or clicking Copy log keeps the window open.
- A failed or cancelled action keeps the window open and expands the log.

The main button says Cancel while the task runs and Close after it finishes. Closing the window during a run cancels the task and restores the original file. Cancel does the same without closing the window.

Copy log copies the full log, including lines that are no longer visible in the panel, to the clipboard.

## Set date

Set date opens a small dialog before the task starts. Pick a date, enter the time as `HH:MM:SS`, and click OK. ETSU applies the value in the local time zone. Cancel closes the dialog without changing the file.

ETSU sets the EXIF dates (`DateTimeOriginal`, `CreateDate`, `ModifyDate`) and the filesystem modification and creation times.

## Errors

When an action fails, the status line reads Failed. See log. The log shows the failing step and an `[ABORT]` line. If the file was already replaced, the log also shows a `[ROLLBACK]` line and the original is restored.

Click Copy log to paste the details into a report. Logs are covered in [Configuration](configuration.md). The copy, verify, and swap steps are covered in [How it works](how-it-works.md).

[Documentation index](index.md)
