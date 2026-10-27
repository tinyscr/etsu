# Troubleshooting

## ExifTool is not found

Open a terminal and run `exiftool -ver`. If it fails, install ExifTool and add it to PATH. You can also put `exiftool.exe` next to `etsu.exe`.

If `exiftoolPATH` is set, ETSU uses only that path. A wrong path does not fall back to PATH. Set `exiftoolPATH=null` to search PATH again, or correct the path in `%APPDATA%\etsu\.etsu`.

If you just changed PATH, restart Explorer or sign out and back in so new actions see the new value.

The installer warns at the end when it cannot find ExifTool.

## The Explorer menu is missing

On Windows 11, open Show more options. The menu is under ETSU.

The menu is registered for files, not folders, and for one selected file at a time. If the menu is still missing, reinstall ETSU. The registry key is under the current user:

```text
HKCU\Software\Classes\*\shell\etsu
```

## Clean or Set date stops with a backup already exists error

ETSU refuses to overwrite an existing `<file>.bak`. Inspect both files before you remove anything. If the original is intact, rename or delete the `.bak`. If the `.bak` is the only good copy, restore it by renaming it back to the original name.

A crash during a swap can leave a `.bak` file. Do not delete it until you know which file is good.

## Clean changed the file dates

ETSU cleans a temp copy and replaces the original with it. The final file has the copy's timestamps, not the original. `-P` only stops ExifTool from changing the temp copy's modification time.

Run Set date after cleaning if you need a specific timestamp. Record the original timestamps first if you need to restore them.

## The date dialog rejects my time

Enter the time as `HH:MM:SS`, for example `22:47:52`. The date comes from the date picker. ETSU parses both in the local time zone. An invalid time shows a warning and keeps the dialog open. Cancel closes the dialog without changing the file.

## The window closes too quickly

Clean and Set date close 1.5 seconds after success when the log is collapsed. Click Show log while the task runs to keep the window open. Read always stays open.

If the window has already closed, open the log file. See [Configuration](configuration.md) for the log directory and settings.

## An action failed

The status line reads Failed. See log. The window stays open and expands the log. The failing step is followed by an `[ABORT]` line. If the original was already replaced, a `[ROLLBACK]` line follows and the original is restored.

Click Copy log to copy the details. The log file has the same content when logging is enabled.

## I cancelled an action

Click Cancel while the task runs to stop it and keep the window open. The log shows `[CANCELLED]` and, if needed, `[ROLLBACK]`. Closing the window with the X during a run does the same, then closes the window after the rollback.

## The installer is unsigned

The current build is not code-signed. Windows SmartScreen may warn. GitHub release builds include `SHA256SUMS.txt`; compare the hash before you run the installer.

## Uninstall leaves files

The uninstaller removes the Explorer menu, installed files, `%APPDATA%\etsu`, `%LOCALAPPDATA%\etsu\logs`, and any legacy `%LOCALAPPDATA%\etsu\tmp`. Files you added to the install directory are left in place. Remove them manually if you do not need them.

Uninstall removes `%APPDATA%\etsu` and its contents, including `.etsu`. Back it up first if you want to keep the configuration.

## Known limitations

- ETSU has no command-line interface yet. It runs from the Explorer menu only.
- The Explorer menu is single-file.
- Read shows a tag/value table, not a categorized summary.
- ExifTool is not bundled.
- Clean does not keep the original timestamps.
- The Explorer menu is for files, not folders.
- Each action opens its own window. ETSU does not prevent two windows from working on the same file.
- Clean and Set date close after success when the log is collapsed.

[Documentation index](index.md)
