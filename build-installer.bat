@echo off
setlocal

call build.bat
if errorlevel 1 exit /b 1

set "ISCC=%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"

if not exist "%ISCC%" (
    echo Inno Setup 6 not found. Install it with:
    echo   winget install JRSoftware.InnoSetup --scope user
    exit /b 1
)

if not exist dist\ md dist

echo Building installer...
"%ISCC%" installer\etsu.iss
if errorlevel 1 exit /b 1

echo Installer written to dist.
