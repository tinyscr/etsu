## Read, clean, and set file dates

- **Source:** `cmd/etsu/main.go` (entry + dispatch), `internal/app` (paths, config, ExifTool lookup, output), `internal/ops` (read/clean/date), `internal/install` (self-installer + registry)
- **Dependencies:** external `exiftool`; Go 1.27+ to build
- **Description:** CLI frontend for ExifTool. Reads all metadata from one file, strips EXIF/IPTC/XMP from several (rollback-safe), or sets EXIF dates and filesystem timestamps on several.

---

### Install

`etsu install` is the installer:

1. Copies the running `etsu.exe` to `%LOCALAPPDATA%\Programs\etsu\etsu.exe`.
2. Seeds `%APPDATA%\etsu\.etsu` if missing.
3. Verifies ExifTool is available (config path, then PATH, then the exe directory).
4. Registers the HKCU context menu pointing at the installed copy.

```
HKCU\Software\Classes\*\shell\etsu
  MUIVerb=etsu, subcommands="", MultiSelectModel=Single
  shell\a_read  -> "…\etsu.exe" read "%L"
  shell\b_clean -> "…\etsu.exe" clean "%L"
  shell\c_date  -> "…\etsu.exe" date "%L"
```

`etsu uninstall` removes the registry tree and the install directory (self-delete is deferred until exit). `--purge` also removes `%APPDATA%\etsu` and `%LOCALAPPDATA%\etsu`.

No admin rights are used anywhere. On Windows 11 the menu appears under **Show more options**.

---

### Usage

```
etsu read FILE         Read metadata from one file
etsu clean FILE...     Strip EXIF/IPTC/XMP from files (rollback-safe)
etsu date FILE...      Set EXIF dates + filesystem timestamps on files
etsu install           Install to AppData and register the context menu
etsu uninstall [--purge]  Remove the menu and installed files
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

`%APPDATA%\etsu\.etsu`, seeded on install. One section per subtool, keys case-insensitive, `#` and `;` lines are comments:

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
- **Self-installing binary:** no MSI/NSIS dependency. The same exe handles install, upgrade (rerun `install`), and uninstall.
- **HKCU-only registration:** per-user install and menu, no elevation.
- **Raw ExifTool dump for read:** `read` delegates wholesale instead of re-deriving a categorized summary, so all tags (including unknown ones) are shown.
- **Per-file `-v` parsing in clean:** the `Deleting ...` lines are captured into logs so you can see exactly which tags were stripped from each file.

---

### References

- [ExifTool](https://exiftool.org/) - the metadata engine etsu wraps

---

### Source tree

```
etsu/
├── cmd/
│   └── etsu/
│       └── main.go         ← entry + dispatch
├── internal/
│   ├── app/                ← paths, config access, ExifTool lookup, console/log helpers
│   ├── config/             ← INI parser
│   ├── install/            ← self-installer, HKCU registry, default config
│   └── ops/                ← read, clean, date, shared copy/verify/swap pipeline
├── bin/
│   └── etsu.exe            ← compiled binary
├── docs/
│   └── etsu.md
├── build.bat
└── go.mod
```

---

### Known limitations

- `read` shows the raw ExifTool dump, not a categorized summary.
- The Explorer context menu is single-file only; multi-select needs a COM shell extension.
- ExifTool is not bundled. The installer fails with guidance if it cannot find `exiftool.exe`.
- Context-menu actions open a console window (unchanged from the C# version).
