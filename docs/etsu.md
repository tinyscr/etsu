## Read, clean, and set file dates

- **Source:** `src/cmd/etsu/main.go` (entry + dispatch), `src/internal/app` (paths, config, ExifTool lookup, output), `src/internal/ops` (read/clean/date), `src/installer/etsu.iss` (Inno Setup installer)
- **Dependencies:** external `exiftool`; Go 1.27+ to build the app; Inno Setup 6 to build the installer
- **Description:** CLI frontend for ExifTool. Reads all metadata from one file, strips EXIF/IPTC/XMP from several (rollback-safe), or sets EXIF dates and filesystem timestamps on several.

---

### Install

The release artifact is `etsu_v<version>_installer.exe`, built with Inno Setup. It installs per-user and requires no admin rights:

1. Copies `etsu.exe` to `%LOCALAPPDATA%\Programs\etsu`.
2. Seeds `%APPDATA%\etsu\.etsu` if missing.
3. Warns if ExifTool cannot be found (PATH, configured path, or next to the exe).
4. Registers the HKCU context menu pointing at the installed copy.
5. Creates an uninstall entry in `HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall`, so it appears in **Settings > Apps**, Revo Uninstaller, and similar tools.

```
HKCU\Software\Classes\*\shell\etsu
  MUIVerb=etsu, subcommands="", MultiSelectModel=Single
  shell\a_read  -> "…\etsu.exe" read "%L"
  shell\b_clean -> "…\etsu.exe" clean "%L"
  shell\c_date  -> "…\etsu.exe" date "%L"
```

Uninstall from Settings > Apps, Revo, or the generated `unins000.exe`. The uninstaller removes the menu, install directory, config, logs, and temp files.

On Windows 11 the menu appears under **Show more options**.

### Build

```bat
build.bat
build.bat 0.2.0
```

`build.bat` runs `go build` for `src/`, then the Inno Setup compiler, and writes `dist\etsu_v<version>_installer.exe`. Version defaults to `0.1.0`. Install Inno Setup 6 with:

```bat
winget install JRSoftware.InnoSetup --scope user
```

The release workflow overrides the version with `/DAppVersion=<tag without v>`.

---

### Usage

```
etsu read FILE         Read metadata from one file
etsu clean FILE...     Strip EXIF/IPTC/XMP from files (rollback-safe)
etsu date FILE...      Set EXIF dates + filesystem timestamps on files
```

No subcommand prints usage and exits. File paths are required; the WinForms pickers from the C# version were dropped because the Explorer menu always supplies `%L`.

Every subcommand shares the same flow: header with the detected ExifTool version (`-ver`), `[n/5]` step progress, and `Press enter to exit.` so the report stays readable before the console closes.

---

### How it works

#### read

Runs `exiftool "<file>"` and prints every tag with no modification. The file is opened read-only - nothing is written. Logging is off by default for this subtool (see Configuration).

#### clean

The rollback-safe strip pipeline:

1. Copy - each file is copied into `%LOCALAPPDATA%\etsu\tmp\etsu_<random>`, preserving extensions. Each copy is verified by byte size.
2. Clean - runs `exiftool -all= -overwrite_original -P -v` on the temp copies, recording `Deleting ...` lines per file.
3. Verify - each temp file must exist, be non-empty, and open for reading.
4. Swap - refuses to overwrite an existing `<file>.bak`, renames each original to `<file>.bak`, moves the cleaned copy into the original path, checks the result is non-empty.
5. Done - deletes `.bak` files, removes the temp workspace, writes a log.

The original is never touched until the cleaned copy passes verification. Any failure restores every `.bak` (removing swapped-in copies) and removes the temp workspace. `-P` preserves the filesystem modification time.

#### date

Prompts for a date (`DD-MM-YYYY HH:MM:SS`), then uses the same copy/verify/swap pipeline:

```
exiftool -AllDates=<date> -FileModifyDate=<date> -FileCreateDate=<date> \
         -overwrite_original -P
```

`-AllDates=` sets DateTimeOriginal, CreateDate, ModifyDate plus the filesystem mtime; `-FileCreateDate=` sets the filesystem creation timestamp. Empty date input cancels.

After the swap, etsu sets the creation time once more via `SetFileTime`. Windows file-system tunneling otherwise restores the old creation time when the replacement reuses the original file name.

---

### Configuration

`%APPDATA%\etsu\.etsu`, seeded by the installer. One section per subtool, keys case-insensitive, `#` and `;` lines are comments:

```ini
[General]
exiftoolPATH=null

[clean]
log=true
logCount=10

[date]
log=true
logCount=10

[read]
log=false
logCount=10
```

| Key | Default | Description |
|---|---|---|
| `exiftoolPATH` | `null` | `null` finds `exiftool.exe` on PATH, then next to `etsu.exe`. Otherwise a full path. |
| `log` | `true` (`false` for `read`) | Write `%LOCALAPPDATA%\etsu\logs\etsu\<tool>_<timestamp>.log`. |
| `logCount` | `10` | Keep the N latest logs per subtool; older ones are deleted after each write. |

Missing file or section falls back to these defaults.

---

### Design decisions

- **Copy-verify-swap pipeline:** the original is never touched until a verified cleaned copy exists. Any failure restores `.bak` files, so a bad wipe can't destroy metadata permanently.
- **Inno Setup installer:** a standard wizard plus an Apps & Features / Revo uninstall entry. The app itself is only the CLI.
- **Per-user install:** everything lives in AppData and HKCU, so no elevation is needed.
- **Raw ExifTool dump for read:** `read` delegates wholesale instead of re-deriving a categorized summary, so all tags (including unknown ones) are shown.
- **Per-file `-v` parsing in clean:** the `Deleting ...` lines are captured into logs so you can see exactly which tags were stripped from each file.

---

### References

- [ExifTool](https://exiftool.org/) - the metadata engine etsu wraps
- [Inno Setup](https://jrsoftware.org/isinfo.php) - installer used for the setup exe

---

### Source tree

```
etsu/
├── build.bat
├── src/
│   ├── go.mod
│   ├── go.sum
│   ├── cmd/
│   │   └── etsu/
│   │       └── main.go         ← entry + dispatch
│   ├── internal/
│   │   ├── app/                ← paths, config access, ExifTool lookup, console/log helpers
│   │   ├── config/             ← INI parser
│   │   └── ops/                ← read, clean, date, shared copy/verify/swap pipeline
│   └── installer/
│       ├── etsu.iss            ← Inno Setup script
│       └── default.etsu        ← config seeded on install
├── bin/
│   └── etsu.exe                ← intermediate app build
├── dist/
│   └── etsu_v*_installer.exe   ← final installer
├── docs/
│   └── etsu.md
├── LICENSE
└── README.md
```

---

### Known limitations

- `read` shows the raw ExifTool dump, not a categorized summary.
- The Explorer context menu is single-file only; multi-select needs a COM shell extension.
- ExifTool is not bundled yet. The installer warns if it cannot find `exiftool.exe`.
- Context-menu actions open a console window (unchanged from the C# version).
- The setup and app use the default Inno/Go icon until an icon is added.
