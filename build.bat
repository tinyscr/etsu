@echo off
setlocal

set "VERSION=%~1"
if "%VERSION%"=="" set "VERSION=0.1.0"
if /i "%VERSION:~0,1%"=="v" set "VERSION=%VERSION:~1%"

set "ISCC=%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"

if not exist "%ISCC%" (
    echo Inno Setup 6 not found. Install it with:
    echo   winget install JRSoftware.InnoSetup --scope user
    exit /b 1
)

if not exist bin\ md bin
if not exist dist\ md dist

set "CGO_ENABLED=0"

rem App icon and manifest are embedded from src\cmd\etsu\rsrc_windows_amd64.syso.
rem Regenerate after changing src\icon.ico or src\etsu.manifest:
rem   rsrc -arch amd64 -ico src\icon.ico -manifest src\etsu.manifest -o src\cmd\etsu\rsrc_windows_amd64.syso
echo Building etsu...
go build -C src -trimpath -buildvcs=false -ldflags "-s -w -X github.com/tinyscr/etsu/internal/app.Version=%VERSION%" -o ..\bin\etsu.exe .\cmd\etsu
if errorlevel 1 exit /b 1

echo Building installer...
"%ISCC%" /DAppVersion=%VERSION% src\installer\etsu.iss
if errorlevel 1 exit /b 1

echo.
echo Installer: dist\etsu_v%VERSION%_installer.exe
