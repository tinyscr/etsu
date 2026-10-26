# Troubleshooting

## ExifTool is not found

Run `exiftool -ver` in the same terminal. If it fails, install ExifTool and add it to PATH. You can also put `exiftool.exe` next to `etsu.exe`.

If `exiftoolPATH` is set, ETSU uses only that path. A wrong path does not fall back to PATH. Set `exiftoolPATH=null` to search PATH again, or correct the path in `%APPDATA%\etsu\.etsu`.

The installer warns at the end when it cannot find ExifTool.

## The Explorer menu is missing

On Windows 11, open Show more options. The menu is under ETSU.

The menu is registered for files, not folders, and for one selected file at a time. If the menu is still missing, reinstall ETSU. The registry key is under the current user:

```text
HKCU\Software\Classes\*\shell\etsu
```

## Clean or date stops with a different disk drive error

The error ends with:

```text
The system cannot move the file to a different disk drive.
```

ETSU copies the file to `%LOCALAPPDATA%\etsu\tmp` and then renames the copy into place. Windows cannot rename across drives. The original is restored.

Process a copy on the system drive, or set `LOCALAPPDATA` to a folder on the target drive for that terminal session:

```bat
set LOCALAPPDATA=D:\LocalAppData
etsu clean "D:\Photos\image.jpg"
```

ETSU creates `etsu\tmp` and `etsu\logs` under that directory. The config path stays under `%APPDATA%`.

## Clean or date stops with a backup already exists error

ETSU refuses to overwrite an existing `<file>.bak`. Inspect both files before you remove anything. If the original is intact, rename or delete the `.bak`. If the `.bak` is the only good copy, restore it by renaming it back to the original name.

A crash during a swap can leave a `.bak` file. Do not delete it until you know which file is good.

## Clean changed the file dates

ETSU cleans a temp copy and replaces the original with it. The final file has the copy's timestamps, not the original. `-P` only stops ExifTool from changing the temp copy's modification time.

Run `etsu date` after cleaning if you need a specific timestamp. Record the original timestamps first if you need to restore them.

## The date prompt rejects my input

Use `DD-MM-YYYY HH:MM:SS`, for example `26-10-2026 22:47:52`. ETSU parses the value in the local time zone. Empty input cancels the command. Invalid input exits with code 1.

## The console window opens and closes

Successful runs wait for `Press enter to exit.` Press Enter to close the window. Menu actions open a console window by design.

If a command fails before the final wait, it prints the error and exits.

## The installer is unsigned

The current build is not code-signed. Windows SmartScreen may warn. GitHub release builds include `SHA256SUMS.txt`; compare the hash before you run the installer.

## Uninstall leaves files

The uninstaller removes the Explorer menu, installed files, `%APPDATA%\etsu`, `%LOCALAPPDATA%\etsu\logs`, and `%LOCALAPPDATA%\etsu\tmp`. Files you added to the install directory are left in place. Remove them manually if you do not need them.

Uninstall removes `%APPDATA%\etsu` and its contents, including `.etsu`. Back it up first if you want to keep the configuration.

## Known limitations

- `read` prints the raw ExifTool dump, not a categorized summary.
- The Explorer menu is single-file. Batch work uses the CLI.
- The installer always registers the Explorer menu. An option to install only the CLI is planned.
- ExifTool is not bundled.
- Context-menu actions open a console window.
- `clean` does not keep the original timestamps.
- `clean` and `date` fail on files that are on a different drive from `%LOCALAPPDATA%`.
- The Explorer menu is for files. `clean` and `date` expect file paths, not folders.

[Documentation index](index.md)
