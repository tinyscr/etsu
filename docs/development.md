# Development

## Requirements

- Go 1.27 or newer.
- [Inno Setup](https://jrsoftware.org/isinfo.php) 6.
- `rsrc` only when you change the app icon or manifest.

## Build

```bat
build.bat
build.bat 0.2.0
```

`build.bat` runs `go build` for `src\`, then the Inno Setup compiler, and writes `dist\etsu_v<version>_installer.exe`. The version defaults to `0.1.0`. A leading `v` is removed, so `build.bat v0.2.0` and `build.bat 0.2.0` produce the same file name.

The app is built with `-H windowsgui`, so it does not open a console window. The GUI uses [walk](https://github.com/lxn/walk) with native Win32 controls. No cgo and no WebView2.

Install Inno Setup 6 with:

```bat
winget install JRSoftware.InnoSetup --scope user
```

The build script looks for `ISCC.exe` in `%LOCALAPPDATA%\Programs\Inno Setup 6`, `%ProgramFiles(x86)%\Inno Setup 6`, and `%ProgramFiles%\Inno Setup 6`.

## Source layout

```text
etsu/
  build.bat
  src/
    go.mod
    go.sum
    etsu.manifest
    cmd/etsu/
      main.go
      rsrc_windows_amd64.syso
    internal/
      app/
      config/
      ops/
      ui/
    installer/
      etsu.iss
      default.etsu
    icon.ico
    icon.png
  docs/
  README.md
  LICENSE
  .github/workflows/release.yml
```

- `src\cmd\etsu`: entry point, action parsing, ExifTool check, and window startup.
- `src\internal\app`: paths, config access, ExifTool lookup, file copy, log helpers, and the creation-time helper.
- `src\internal\config`: INI parser.
- `src\internal\ops`: read, clean, date, the reporter interface, and the shared copy, verify, swap pipeline.
- `src\internal\ui`: task window and date dialog.
- `src\etsu.manifest`: Common Controls 6.0 and PerMonitorV2 declarations. walk needs the Common Controls entry for its tooltips and themed controls.
- `src\installer`: Inno Setup script and the default config seeded on install.
- `src\icon.ico`: app and installer icon.
- `docs`: these guides.

Build outputs are `bin\etsu.exe` and `dist\etsu_v<version>_installer.exe`. Both directories are ignored by Git.

## Installer

`src\installer\etsu.iss` builds a per-user installer. It uses:

- a stable AppId, `{81ADEFE9-F5E3-4A42-8DF5-5D7E9AE39925}`
- `PrivilegesRequired=lowest`
- `DefaultDirName={localappdata}\Programs\ETSU`
- `LicenseFile=..\..\LICENSE`
- `SetupIconFile=..\icon.ico`
- HKCU registry entries for the Explorer submenu and the uninstall entry

`src\installer\default.etsu` is copied to `%APPDATA%\etsu\.etsu` only when the file is missing.

## Icon and manifest resources

The app icon and manifest are embedded from `src\cmd\etsu\rsrc_windows_amd64.syso`. The Go build picks up that file automatically. The installer uses `src\icon.ico` directly through `SetupIconFile`.

After changing `src\icon.ico` or `src\etsu.manifest`, regenerate the resource:

```bat
go install github.com/akavel/rsrc@latest
rsrc -arch amd64 -ico src\icon.ico -manifest src\etsu.manifest -o src\cmd\etsu\rsrc_windows_amd64.syso
```

`build.bat` does not regenerate the `.syso` file.

`rsrc` puts the manifest at resource ID 1 and the icon group at ID 2. The window code loads the icon from ID 2.

## Release workflow

`.github/workflows/release.yml` is a manual workflow. It accepts a tag in the form `vMAJOR.MINOR.PATCH`, builds the app and installer, writes `dist\SHA256SUMS.txt`, and creates a draft GitHub release with both files.

The workflow runs on GitHub Actions. A local clone can build the same app and installer with `build.bat`.

## Local check

Build the app and run it against a copy of a file:

```bat
bin\etsu.exe read "C:\Photos\copy.jpg"
```

The window is the only output. Read stays open so you can inspect the log. Use copies when you test Clean or Set date. For normal use, start the action from the Explorer menu.

Run `go vet ./...` from `src\` after code changes.

[Documentation index](index.md)
