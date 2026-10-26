# Development

## Requirements

- Go 1.27 or newer.
- [Inno Setup](https://jrsoftware.org/isinfo.php) 6.
- `rsrc` only when you change the app icon.

## Build

```bat
build.bat
build.bat 0.2.0
```

`build.bat` runs `go build` for `src\`, then the Inno Setup compiler, and writes `dist\etsu_v<version>_installer.exe`. The version defaults to `0.1.0`. A leading `v` is removed, so `build.bat v0.2.0` and `build.bat 0.2.0` produce the same file name.

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
    cmd/etsu/
      main.go
      rsrc_windows_amd64.syso
    internal/
      app/
      config/
      ops/
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

- `src\cmd\etsu`: entry point and command dispatch.
- `src\internal\app`: paths, config access, ExifTool lookup, console and log helpers, and the creation-time helper.
- `src\internal\config`: INI parser.
- `src\internal\ops`: read, clean, date, and the shared copy, verify, swap pipeline.
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

## Icon resource

The app icon is embedded from `src\cmd\etsu\rsrc_windows_amd64.syso`. The Go build picks up that file automatically. The installer uses `src\icon.ico` directly through `SetupIconFile`.

After changing `src\icon.ico`, regenerate the resource:

```bat
go install github.com/akavel/rsrc@latest
rsrc -arch amd64 -ico src\icon.ico -o src\cmd\etsu\rsrc_windows_amd64.syso
```

`build.bat` does not regenerate the `.syso` file.

## Release workflow

`.github/workflows/release.yml` is a manual workflow. It accepts a tag in the form `vMAJOR.MINOR.PATCH`, builds the app and installer, writes `dist\SHA256SUMS.txt`, and creates a draft GitHub release with both files.

The workflow runs on GitHub Actions. A local clone can build the same app and installer with `build.bat`.

## Local check

Build the app and run it against a copy of a file:

```bat
bin\etsu.exe read "C:\Photos\copy.jpg"
```

Successful commands wait for Enter before they exit. Use copies when you test `clean` or `date`.

[Documentation index](index.md)
