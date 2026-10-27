# How it works

ETSU is a Go program with a native Win32 GUI. The installer copies the binary, seeds the config, and writes registry entries. ExifTool performs the metadata work.

## Task window

Each Explorer action starts one short-lived window. The window runs the operation on a background goroutine and reports progress to the UI thread. It stays responsive while ExifTool runs.

The reporter updates the status line and, for Clean and Set date, the progress bar. Progress is phase-based, not parsed from ExifTool output. The bar moves to 0 when copying starts, 20 before the metadata step, 40 before verification, 60 before the swap, 80 before finishing, and 100 when done. Read uses 20 for the ExifTool call and 100 for done, without a progress bar.

The log panel keeps the last 5,000 lines. Older lines are dropped from the panel, but the full run stays in memory for Copy log and in the log file when logging is enabled. See [Configuration](configuration.md) for log settings.

Read opens with the log expanded. Clean and Set date open with it collapsed and close 1.5 seconds after success if it stays collapsed. Expanding the log or clicking Copy log cancels that timer. Failures and cancellations expand the log and keep the window open.

## Read

Read runs `exiftool FILE` and writes the output to the log. It passes no write flags, so the file is not modified. Logging is off by default for this action.

## Clean

Clean removes metadata with this ExifTool command on a temporary copy:

```bat
exiftool -all= -overwrite_original -P -v <temp>
```

The `-v` output is parsed for `Deleting ...` lines. Those lines go into the log.

## Date

Set date asks for a date and a time in `HH:MM:SS`, parses them in the local time zone, and runs:

```bat
exiftool -AllDates=<date> -FileModifyDate=<date> -FileCreateDate=<date> -overwrite_original -P <temp>
```

`-AllDates` targets `DateTimeOriginal`, `CreateDate`, and `ModifyDate`. `-FileModifyDate` sets the filesystem modification time. `-FileCreateDate` sets the filesystem creation time.

After the swap, ETSU sets the creation time on the final file again with `SetFileTime`. Windows file-system tunneling can restore the old creation time when a replacement reuses the original file name.

## Copy, verify, swap

Clean and Set date use the same pipeline:

1. Copy the file to a temporary directory beside the original (`.etsu_<random>\file<ext>`). ETSU compares the source and copy sizes.
2. Run the ExifTool operation on the temp copy.
3. Verify that the temp file exists, is not empty, and can be opened for reading.
4. Refuse to overwrite an existing `<file>.bak`. Rename the original to `<file>.bak`, move the temp file into the original path, and check that the result is not empty.
5. Check for cancellation, delete the `.bak` file, and finish. The temp directory is removed when the operation returns, and the window writes the log.

If a step fails after the original is renamed, ETSU removes the file it swapped in, renames `<file>.bak` back, and removes the temp directory. The original content is returned.

The pipeline is a rollback mechanism for the current run. It is not a backup: a successful run deletes the `.bak` file. If ETSU is killed or the machine loses power during the swap, a `.bak` file can remain. The next run stops when it sees an existing `.bak`; see [Troubleshooting](troubleshooting.md).

## Cancellation

The task window passes a context to the operation. Cancel, or closing the window during a run, cancels that context.

- The file copy checks the context between 1 MB chunks.
- ExifTool runs with `exec.CommandContext`, so the process is killed on cancel.
- A cancellation after the swap but before the `.bak` is removed triggers the same rollback as a failure.
- Closing the window during a run cancels the task, waits for the rollback, and then closes.

The log records a cancelled run with a `[CANCELLED]` line and writes a log file with outcome `CANCELLED` when logging is enabled.

## Timestamps

Clean replaces the original with a temp copy. The final file has the timestamps of that copy, not the original. `-P` only stops ExifTool from changing the temp copy's modification time. Use Set date after cleaning if you need a specific timestamp.

Set date writes the EXIF dates and the filesystem modification and creation times on the temp copy, then sets the creation time again after the swap. If the final creation-time update fails, ETSU logs a warning and leaves the other dates in place.

## Drive boundaries

The temp workspace is created in the original file's directory, so the swap stays on one volume. ETSU needs write permission in that directory; the swap already needs it for the `.bak` file.

## Design choices

- The original is not modified until the temp copy passes verification.
- Read shows every tag in the table so tags that ETSU does not categorize are still visible.
- The installer is per-user and writes only to AppData and HKCU.
- The Explorer menu is single-file. ETSU has no command-line interface yet.
- Each action opens its own window. ETSU does not coordinate between windows.

[Documentation index](index.md)
