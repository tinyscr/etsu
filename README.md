## ETSU

Read, clean, and set file dates through ExifTool. Windows-only, per-user install with a standard setup wizard. Published by TinyScripts.

### Build

```bat
build.bat
```

Builds the app and the installer, producing `dist\etsu_v0.1.0_installer.exe`. Pass a version to override:

```bat
build.bat 0.2.0
```

Requires Go 1.27+ and Inno Setup 6:

```bat
winget install JRSoftware.InnoSetup --scope user
```

### Install

Run `dist\etsu_v0.1.0_installer.exe`. The wizard asks where to install, shows the MIT license, then:

- Installs to `%LOCALAPPDATA%\Programs\ETSU` by default
- Seeds `%APPDATA%\etsu\.etsu`
- Registers the **ETSU** submenu (Read metadata, Clean metadata, Set date) for one selected file
- Adds an entry to **Settings > Apps** (and Revo Uninstaller) as `ETSU`, publisher `TinyScripts`

No admin rights are needed; everything is per-user. On Windows 11 the context menu appears under **Show more options**.

Uninstall from Settings > Apps, Revo, or `%LOCALAPPDATA%\Programs\ETSU\unins000.exe`. It removes the menu, config, logs, and temp files.

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

ExifTool is an external dependency. Install it and put `exiftool.exe` on PATH, or set a full path in the config.
