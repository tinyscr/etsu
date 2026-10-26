# Installation

ETSU installs for the current Windows user. The wizard asks for the install location and shows the MIT license. It does not request administrator rights. ETSU is not portable: it writes to AppData and registers an Explorer menu.

## Before you install

- 64-bit Windows.
- ExifTool. ETSU does not bundle it. The installer warns if it cannot find `exiftool.exe`.

## Install

Run the installer from `dist\`, for example:

```bat
dist\etsu_v0.1.0_installer.exe
```

The default install directory is `%LOCALAPPDATA%\Programs\ETSU`. You can choose another directory on the wizard's location page.

The installer:

1. Copies `etsu.exe` to the install directory.
2. Creates `%APPDATA%\etsu\.etsu` when the file is missing.
3. Registers the ETSU submenu for files under `HKCU\Software\Classes\*\shell\etsu`.
4. Adds an uninstall entry under `HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall`.
5. Checks the configured ExifTool path when one is set; otherwise it looks on PATH and next to the installed `etsu.exe`. It warns when it finds no ExifTool.

The installer does not add the install directory to PATH. Add `%LOCALAPPDATA%\Programs\ETSU` to PATH, or run `etsu.exe` by full path.

## Explorer menu

The ETSU submenu contains Read metadata, Clean metadata, and Set date. It is registered for one selected file. On Windows 11, open Show more options to reach it.

The command values point to the installed `etsu.exe` and pass the selected file as `%L`:

```text
HKCU\Software\Classes\*\shell\etsu
  MUIVerb = ETSU
  subcommands = ""
  MultiSelectModel = Single

  shell\a_read  -> etsu.exe read "%L"
  shell\b_clean -> etsu.exe clean "%L"
  shell\c_date  -> etsu.exe date "%L"
```

Clean metadata starts without a confirmation prompt. Set date asks for a date. Read metadata only prints the ExifTool output.

## Upgrade

Run a newer installer. The installer uses a stable AppId, so the new build updates the existing entry. It creates `%APPDATA%\etsu\.etsu` only when the file is missing, so an existing config stays in place.

## Silent install

The installer accepts standard Inno Setup switches. For example:

```bat
etsu_v0.1.0_installer.exe /VERYSILENT /DIR="D:\Tools\ETSU"
```

## Uninstall

Uninstall from Settings > Apps, Revo Uninstaller, or `unins000.exe` in the install directory.

The uninstaller removes the ETSU Explorer menu, the files it installed, `%APPDATA%\etsu` and its contents, `%LOCALAPPDATA%\etsu\logs`, and `%LOCALAPPDATA%\etsu\tmp`. Back up `%APPDATA%\etsu\.etsu` first if you want to keep the configuration. Files you added to the install directory are left in place.

[Documentation index](index.md)
