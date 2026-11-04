## What's new

### Features

- Clean metadata and Set date now process multiple selected files in one batch.
- The installer checks prerequisites, uses a dark wizard, and can download ExifTool 13.60 during Setup.
- The ExifTool page appears only when ExifTool is missing and says so directly. Choosing manual installation disables Next and explains what to do. Setup will not install ETSU unless ExifTool is found or Setup installs it successfully.
- The uninstaller offers `Uninstall ExifTool`, checked by default.
- App dialogs use the dark theme. The usage popup links to the CLI docs and opens the default browser.

### Fixes

- Setup no longer exits when you choose the manual ExifTool option.
- The date dialog shows the app icon.
- Long URLs are no longer clipped in information dialogs.

### Security

- Setup verifies the ExifTool download with a pinned SHA-256 checksum.

### Improvements

- Batch Clean and Set date continue after a per-file failure and report a five-stage summary with detailed logs.
- ExifTool installed by Setup sits beside `etsu.exe`, and the install folder is added to the user PATH.
- Installer prerequisite checks show progress before installation.

### Docs

- Document multi-file batches, ExifTool requirements, installer behavior, and dark dialogs.

**Commit**:
```
<commit>
```

**Full Changelog**: https://github.com/tinyscr/etsu/commits/<tag>
