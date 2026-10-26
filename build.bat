@echo off
if not exist bin\ md bin

echo Building etsu...
go build -trimpath -ldflags "-s -w" -o bin\etsu.exe .\cmd\etsu
if %ERRORLEVEL% NEQ 0 exit /b 1

echo Build succeeded.
