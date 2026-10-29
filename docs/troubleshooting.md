# Troubleshooting

## ExifTool is not found

Open a terminal and run `exiftool -ver`. If it fails, install ExifTool and add it to PATH. You can also put `exiftool.exe` next to `etsu.exe`. On 32-bit Windows, use a 32-bit ExifTool build.

If `exiftoolPATH` is set, ETSU uses only that path. A wrong path does not fall back to PATH. Set `exiftoolPATH=null` to search PATH again, or correct the path in `<install>\config.ini`. The default location is `%LOCALAPPDATA%\Programs\ETSU\config.ini`.

If you just changed PATH, restart Explorer or sign out and back in so new actions see the new value.

The installer warns at the end when it cannot find ExifTool.

## The Explorer menu is missing

On Windows 11, open Show more options. The menu is under ETSU.

The menu is registered for files, not folders, and for one selected file at a time. If the menu is still missing, reinstall ETSU. The registry key is under the current user:

```text
HKCU\Software\Classes\*\shell\etsu
```

## Clean or Set date stops with a backup already exists error

ETSU refuses to overwrite an existing `<file>.bak`. It does not automatically restore the original. Inspect both files before you remove anything. If the original is intact, rename or delete the `.bak`. If the `.bak` is the only good copy, restore it by renaming it back to the original name.

A crash during replacement can leave a `.bak` file. Do not delete it until you know which file is good.

## Clean changed the file dates

ETSU cleans a temp copy and replaces the original with it. The final file has the copy's timestamps, not the original. `-P` only stops ExifTool from changing the temp copy's modification time.

Run Set date after cleaning if you need a specific timestamp. Record the original timestamps first if you need to restore them.

## The date dialog does not accept a typed time

The date dialog uses lists for the day, month, year, hour, minute, second, and AM/PM value. Pick the values instead of typing a time string. The dialog clamps the day to the last valid day when you change the month or year. Click OK to apply the value, or click Cancel or press Esc to close without changing the file.

## The window stays open

The task window does not close automatically. Click OK after the operation finishes. Closing it during a run cancels the operation.

If you need the log later, click Open log. Read logging is off by default, so enable it in `config.ini` if you want a log file for Read. See [Configuration](configuration.md) for the log directory and settings.

## An action failed

The window shows `Failed: ...` and the failing step includes an `[ABORT]` line in the log. The window stays open.

ETSU does not automatically restore the original. If the failure happened before the replacement step, the original file is unchanged. If it happened after the replacement, `<file>.bak` may remain. Inspect both files before you remove or rename either one.

## I cancelled an action

Closing the window during a run cancels the operation. The window shows `Cancelled.`

If cancellation happens before the replacement step, the original file is unchanged. Once replacement starts, ETSU does not roll back. A failed replacement can leave a `.bak` file.

## The installer is unsigned

The current build is not code-signed. Windows SmartScreen may warn. GitHub release builds include `SHA256SUMS.txt`; compare the hash before you run the installer.

## Uninstall leaves files

The uninstaller removes the Explorer menu, installed files, `config.ini`, `logs`, and `tmp` from the install directory. Files you added to the install directory are left in place. Remove them manually if you do not need them.

Uninstall removes `config.ini` from the install directory. Back it up first if you want to keep the configuration.

## Known limitations

- ETSU has no general command-line interface. It runs from the Explorer menu.
- The Explorer menu is single-file.
- Read shows a tag/value text view, not a categorized summary.
- ExifTool is not bundled.
- Clean does not keep the original timestamps.
- The Explorer menu is for files, not folders.
- Each action opens its own window. ETSU does not prevent two windows from working on the same file.
- ETSU does not keep an automatic rollback copy after a successful run.

[Documentation](README.md)
