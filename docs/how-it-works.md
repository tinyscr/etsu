# How it works

ETSU is a Go command-line program. It has no GUI; it runs in a console window. The installer copies the binary, seeds the config, and writes registry entries. ExifTool performs the metadata work.

## Read

`etsu read FILE` runs `exiftool FILE` and prints the output. It does not pass write flags, so the file is not modified. It takes exactly one path. Logging is off by default for this command.

## Clean

`etsu clean FILE...` removes metadata with this ExifTool command on a temporary copy:

```bat
exiftool -all= -overwrite_original -P -v <temp>
```

The `-v` output is parsed for `Deleting ...` lines. Those lines go into the log.

## Date

`etsu date FILE...` asks for a date in `DD-MM-YYYY HH:MM:SS`, parses it in the local time zone, and runs:

```bat
exiftool -AllDates=<date> -FileModifyDate=<date> -FileCreateDate=<date> -overwrite_original -P <temp>
```

`-AllDates` targets `DateTimeOriginal`, `CreateDate`, and `ModifyDate`. `-FileModifyDate` sets the filesystem modification time. `-FileCreateDate` sets the filesystem creation time. Empty input cancels the command.

After the swap, ETSU sets the creation time on the final file again with `SetFileTime`. Windows file-system tunneling can restore the old creation time when a replacement reuses the original file name.

## Copy, verify, swap

`clean` and `date` use the same pipeline:

1. Copy each file to `%LOCALAPPDATA%\etsu\tmp\etsu_<random>\<n><ext>`. ETSU compares the source and copy sizes.
2. Run the ExifTool operation on the temp copy.
3. Verify that each temp file exists, is not empty, and can be opened for reading.
4. Refuse to overwrite an existing `<file>.bak`. Rename the original to `<file>.bak`, move the temp file into the original path, and check that the result is not empty.
5. Delete the `.bak` files and remove the temp directory. Write the log.

If a step fails, ETSU restores every `.bak` it created, removes the files it swapped in, and removes the temp directory. The original content is returned.

The pipeline is a rollback mechanism for the current run. It is not a backup: a successful run deletes the `.bak` files. If ETSU is killed or the machine loses power during the swap, a `.bak` file can remain. The next run stops when it sees an existing `.bak`; see [Troubleshooting](troubleshooting.md).

## Timestamps

`clean` replaces the original with a temp copy. The final file has the timestamps of that copy, not the original. `-P` only stops ExifTool from changing the temp copy's modification time. Use `etsu date` after cleaning if you need a specific timestamp.

`date` sets the EXIF dates and the filesystem modification and creation times on the temp copy, then sets the creation time again after the swap. If the final creation-time update fails, ETSU prints a warning and leaves the other dates in place.

## Drive boundaries

The temp workspace is on the `%LOCALAPPDATA%` drive. Windows cannot rename a file across drives, so `clean` and `date` fail at the swap step when the target file is on another drive. ETSU restores the original. [Troubleshooting](troubleshooting.md) describes the workaround.

## Design choices

- The original is not modified until the temp copy passes verification.
- `read` prints the raw ExifTool output so tags that ETSU does not categorize are still visible.
- The installer is per-user and writes only to AppData and HKCU.
- The Explorer menu is single-file. Batch work uses the CLI.

[Documentation index](index.md)
