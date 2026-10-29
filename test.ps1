[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$versionPath = Join-Path $root "version"
$src = Join-Path $root "src"
$tests = Join-Path $root "tests"
$bin = Join-Path $root "bin"
$obj = Join-Path $root "obj"
$testDir = Join-Path $bin "tests"
$testExe = Join-Path $testDir "etsu-tests.exe"

function Fail([string]$Message) {
    throw $Message
}

function Find-Csc {
    $candidates = @(
        (Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"),
        (Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe")
    )
    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }
    Fail "csc.exe was not found. Install .NET Framework 4.5 or later."
}

if (-not (Test-Path -LiteralPath $versionPath)) {
    Fail "version file not found: $versionPath"
}
$Version = [string](Get-Content -LiteralPath $versionPath -Raw)
$Version = $Version.Trim()
if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    Fail "version must contain three numeric parts, for example 0.2.0."
}

$csc = Find-Csc
foreach ($path in @($bin, $obj, $testDir)) {
    New-Item -ItemType Directory -Force -Path $path | Out-Null
}

$sources = @(
    Get-ChildItem -Path $src -Recurse -File -Filter "*.cs" |
        Sort-Object FullName |
        ForEach-Object { $_.FullName }
)
$sources += @(
    Get-ChildItem -Path $tests -Recurse -File -Filter "*.cs" |
        Sort-Object FullName |
        ForEach-Object { $_.FullName }
)
if ($sources.Count -eq 0) {
    Fail "No C# sources found under src or tests."
}

$versionFile = Join-Path $obj "Version.g.cs"
$versionText = @"
using System.Reflection;

[assembly: AssemblyVersion("$Version.0")]
[assembly: AssemblyFileVersion("$Version.0")]
[assembly: AssemblyInformationalVersion("$Version")]
"@
[System.IO.File]::WriteAllText($versionFile, $versionText, (New-Object System.Text.UTF8Encoding($false)))
$sources += $versionFile

Remove-Item -LiteralPath $testExe -Force -ErrorAction SilentlyContinue

$compilerArgs = @(
    "/nologo",
    "/target:exe",
    "/platform:anycpu",
    "/optimize+",
    "/codepage:65001",
    "/main:Etsu.Tests.TestRunner",
    "/out:$testExe",
    "/reference:System.dll",
    "/reference:System.Core.dll",
    "/reference:System.Drawing.dll",
    "/reference:System.Windows.Forms.dll"
)
$compilerArgs += $sources

Write-Host "Building tests..."
& $csc @compilerArgs
if ($LASTEXITCODE -ne 0) {
    Fail "Test build failed with exit code $LASTEXITCODE."
}
if (-not (Test-Path -LiteralPath $testExe)) {
    Fail "Test build did not produce $testExe."
}

Write-Host "Running tests..."
& $testExe
if ($LASTEXITCODE -ne 0) {
    Fail "Tests failed with exit code $LASTEXITCODE."
}

Write-Host "Tests complete."
