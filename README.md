## etsu

Read, clean, and set file dates through ExifTool. Windows-only, installed per-user under AppData. The binary is its own installer.

### Build

```bat
build.bat
```

Creates `bin\etsu.exe`. Requires Go 1.27+ and `exiftool.exe` on PATH (or a full path in the config).

### Install

```bat
bin\etsu.exe install
```

Copies `etsu.exe` to `%LOCALAPPDATA%\Programs\etsu\etsu.exe`, seeds `%APPDATA%\etsu\.etsu`, and registers an `etsu` submenu (Read metadata, Clean metadata, Set date) for one selected file. Writes only HKCU registry keys, so no admin rights are needed. On Windows 11 the menu appears under **Show more options**.

Uninstall from the installed copy:

```bat
etsu uninstall
etsu uninstall --purge
```

`--purge` also removes config, logs, and temp files. The install directory removes itself after exit.

### Usage

```bat
etsu read FILE
etsu clean FILE...
etsu date FILE...
```

File paths are required; there are no GUI pickers. The Explorer menu passes the selected file automatically.

### Config and logs

- Config: `%APPDATA%\etsu\.etsu`
- Logs: `%LOCALAPPDATA%\etsu\logs\etsu\`
- Temp workspace: `%LOCALAPPDATA%\etsu\tmp\`

`exiftoolPATH=null` means "find exiftool on PATH". Logging is controlled per subtool (`log`, `logCount`).
