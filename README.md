## etsu

Read, clean, and set file dates through ExifTool. Portable, no install step. Builds with Windows' built-in C# compiler.

### Build

```bat
build.bat
```

Creates `bin\etsu.exe`. Requires .NET Framework 4.0+ and `exiftool.exe` on PATH or next to the binary.

### Explorer context menu (single file)

```bat
bin\etsu.exe install
```

Adds an `etsu` submenu with Read metadata, Clean metadata, and Set date for one selected file. Writes only HKCU registry keys. On Windows 11 the menu appears under **Show more options**.

Remove it with:

```bat
bin\etsu.exe uninstall
```

### Config and logs

`conf\.etsu` controls per-subtool logging. Logs go to `logs\etsu\`.
