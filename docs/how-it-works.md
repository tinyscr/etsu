# How it works

ETSU is a C# WinForms app compiled against .NET Framework. The installer copies the executable, seeds `config.ini`, creates the log and temp directories, and writes registry entries. ExifTool performs the metadata work.

## Task window

Each Explorer action starts one task window. The operation runs on a background task and reports log lines to the UI thread, so the window stays responsive while ExifTool runs.

The window has no progress bar. The log reports the five pipeline steps for Clean and Set date, and the formatted metadata output for Read.

The log is read-only and selectable. The reporter lines stay in memory for the current window and go to the log file when logging is enabled. The Open log button opens the file after ETSU writes it. See [Configuration](configuration.md) for log settings.

The window stays open after the operation finishes. Closing it during a run cancels the operation. ETSU waits for the operation to stop before the window closes.

## Read

Read runs `exiftool FILE` and formats the output into padded tag/value lines. It passes no write flags, so the file is not modified. Logging is off by default for this action.

## Clean

Clean removes metadata with this ExifTool command on a temporary copy:

```bat
exiftool -all= -overwrite_original -P -v <temp>
```

The `-v` output is parsed for `Deleting ...` lines. Those lines go into the log.

## Date

Set date opens a dialog with day, month, year, hour, minute, second, and AM/PM lists. The selected values are applied in the local time zone, then ETSU runs:

```bat
exiftool -AllDates=<date> -FileModifyDate=<date> -FileCreateDate=<date> -overwrite_original -P <temp>
```

`-AllDates` targets `DateTimeOriginal`, `CreateDate`, and `ModifyDate`. `-FileModifyDate` sets the filesystem modification time. `-FileCreateDate` sets the filesystem creation time.

After the replacement, ETSU sets the creation time on the final file again with `File.SetCreationTime`. If that final update fails, ETSU logs a warning and leaves the other dates in place.

## Copy, verify, replace

Clean and Set date use the same pipeline:

1. Copy the file to a temporary directory beside the original (`.etsu_<random>\file<ext>`). ETSU compares the source and copy sizes and SHA-256 hashes.
2. Run the ExifTool operation on the temp copy.
3. Verify that the temp file exists, is not empty, can be opened for reading, and still matches its SHA-256 hash.
4. Refuse to overwrite an existing `<file>.bak`. Replace the original with the temp file using `File.Replace`, which creates the `.bak` file. Verify that the final file exists, is not empty, and matches the processed hash.
5. Verify that the `.bak` file matches the original hash, delete the `.bak` file, and finish. The temp directory is removed when the operation returns.

ETSU does not automatically restore the original if a step fails. If a failure happens before the replacement, the original file is unchanged. If a failure happens after the replacement, the original path contains the processed file and `<file>.bak` may remain. Inspect both files before you remove or rename either one.

The `.bak` file is a replacement artifact, not a backup. A successful run deletes it. If ETSU is killed or the machine loses power during replacement, a `.bak` file can remain. The next run stops when it sees an existing `.bak`; see [Troubleshooting](troubleshooting.md).

## Cancellation

The task window passes a cancellation token to the operation. Closing the window during a run cancels that token.

- The file copy checks the token between 1 MB chunks.
- ExifTool runs in a child process, which ETSU kills on cancel.
- The pipeline checks the token before the replacement step.
- After replacement starts, ETSU does not roll back. A cancellation at that point does not undo the new file, and the operation can finish normally.

The window shows `Cancelled.` when the operation is cancelled. Closing the window waits for the operation to stop before the window closes.

## Timestamps

Clean replaces the original with a temp copy. The final file has the timestamps of that copy, not the original. `-P` only stops ExifTool from changing the temp copy's modification time. Use Set date after cleaning if you need a specific timestamp.

Set date writes the EXIF dates and the filesystem modification and creation times on the temp copy, then sets the creation time again after the replacement. If the final creation-time update fails, ETSU logs a warning and leaves the other dates in place.

## Drive boundaries

The operation workspace is created in the original file's directory, so the replacement stays on one volume. ETSU needs write permission in that directory; the replacement already needs it for the `.bak` file.

## Design choices

- The original is not modified until the temp copy passes verification.
- Read shows every tag in a text view so tags that ETSU does not categorize are still visible.
- The installer is per-user and writes only to the install directory and HKCU.
- The Explorer menu is single-file. ETSU has no general command-line interface.
- Each action opens its own window. ETSU does not coordinate between windows.
- ETSU does not keep an automatic rollback copy after a successful run.

[Documentation index](index.md)
