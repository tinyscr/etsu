# How it works

ETSU is a C# WinForms app compiled against .NET Framework. The installer copies the executable, seeds `config.ini`, creates the log and temp directories, and writes registry entries. ExifTool performs the metadata work.

## Task window

Read starts one task window for one file. Clean and Set date start one batch window for the selected files. The operation runs on a background task and reports log lines to the UI thread, so the window stays responsive while ExifTool runs.

The window has no progress bar. Clean and Set date show one five-stage sequence per batch. The header lists every selected file; stage 2 lists each file with its removed tags or date result; stages 3 to 5 show batch summaries. Read shows the formatted metadata output.

The log is read-only and selectable. The window uses the short batch lines. The log file keeps those lines plus the detailed per-file pipeline, the ExifTool command, and the ExifTool output. The Open log button opens the file after ETSU writes it. See [Configuration](configuration.md) for log settings.

The window stays open after the operation finishes. Closing it during a run cancels the operation. ETSU waits for the operation to stop before the window closes.

## Multi-file batches

Explorer invokes a registry verb once per selected file. It does not pass the whole selection to one process, and `%*` is not substituted in this path. ETSU's Clean and Set date commands therefore use a named mutex and a named pipe:

1. The first process becomes the collector and opens the batch window.
2. Each other process sends its file path to the collector and exits.
3. The collector waits briefly for more paths, removes duplicates, then runs the batch.

Read is registered as `Single` and accepts exactly one file. Clean and Set date are registered as `Player`, which makes them available for multi-selection. The parent ETSU submenu is also `Player`; the legacy `Player` limit is 100 selected items.

The batch runs all selected files through stage 1, then all files through stage 2, and so on. A file that fails a stage is skipped by later stages; the other files continue. Cancellation stops the batch before the next file or stage. The final summary reports the number of files processed and failed, and the process exit code is 1 when any file failed.

## Read

Read runs `exiftool FILE` and formats the output into padded tag/value lines. It passes no write flags, so the file is not modified. Logging is off by default for this action.

## Clean

Clean removes metadata from each selected file with this ExifTool command on a temporary copy:

```bat
exiftool -all= -overwrite_original -P -v <temp>
```

The `-v` output is parsed for `Deleting ...` lines. The window lists those tags under stage 2; the log file keeps the full ExifTool output.

## Date

Set date opens one dialog with day, month, year, hour, minute, second, and AM/PM lists. The selected values are applied in the local time zone to each file, then ETSU runs:

```bat
exiftool -AllDates=<date> -FileModifyDate=<date> -FileCreateDate=<date> -overwrite_original -P <temp>
```

`-AllDates` targets `DateTimeOriginal`, `CreateDate`, and `ModifyDate`. `-FileModifyDate` sets the filesystem modification time. `-FileCreateDate` sets the filesystem creation time.

After each replacement, ETSU sets the creation time on the final file again with `File.SetCreationTime`. If that final update fails, ETSU logs a warning and leaves the other dates in place. The window shows `Date set` under stage 2 for each file; the log file keeps the ExifTool command and output.

## Copy, verify, replace

Clean and Set date use the same five stages for the whole batch:

1. Copy every selected file to its own temporary directory beside the original (`.etsu_<random>\file<ext>`). ETSU compares each source and copy size and SHA-256 hash.
2. Run the ExifTool operation on each temp copy.
3. Verify that each temp file exists, is not empty, can be opened for reading, and still matches its SHA-256 hash.
4. Refuse to overwrite an existing `<file>.bak`. Replace each original with its temp file using `File.Replace`, which creates the `.bak` file. Verify that the final file exists, is not empty, and matches the processed hash.
5. Verify that each `.bak` file matches the original hash, delete the `.bak` file, and finish. The temp directories are removed when the operation returns.

All selected files have temp copies alive at the same time, so a batch needs temporary disk space for the whole selection.

ETSU does not automatically restore the original if a step fails. If a failure happens before the replacement, the original file is unchanged. If a failure happens after the replacement, the original path contains the processed file and `<file>.bak` may remain. Inspect both files before you remove or rename either one.

The `.bak` file is a replacement artifact, not a backup. A successful run deletes it. If ETSU is killed or the machine loses power during replacement, a `.bak` file can remain. The next run stops when it sees an existing `.bak`; see [Troubleshooting](troubleshooting.md).

## Cancellation

The task window passes a cancellation token to the operation. Closing the window during a run cancels that token.

- The file copy checks the token between 1 MB chunks.
- ExifTool runs in a child process, which ETSU kills on cancel.
- The pipeline checks the token before the replacement step.
- After replacement starts, ETSU does not roll back. A cancellation at that point does not undo the new file, and the operation can finish normally.
- A batch checks the token before each file and each stage. Files already processed keep their changes; the remaining files and stages are not started.

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
- Read is single-file. Clean and Set date accept multiple selected files and coordinate the per-file Explorer launches into one batch.
- The window shows one batch summary; the log file keeps the per-file detail and ExifTool output.
- Separate actions or separate batches can still run in separate windows. ETSU does not prevent two windows from working on the same file.
- ETSU does not keep an automatic rollback copy after a successful run.

[Documentation](README.md)
