[CmdletBinding()]
param(
    [ValidateSet("x64", "x86")]
    [string]$Architecture = "x64",
    [switch]$NoInstaller
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$versionPath = Join-Path $root "version"
$src = Join-Path $root "src"
$bin = Join-Path $root "bin"
$obj = Join-Path $root "obj"
$dist = Join-Path $root "dist"
$icon = Join-Path $src "Assets\icon.ico"
$manifest = Join-Path $src "Assets\etsu.manifest"
$iss = Join-Path $src "Installer\etsu.iss"
$exe = Join-Path $bin "etsu.exe"

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

function Find-Iscc {
    $candidates = @()
    if ($env:LOCALAPPDATA) {
        $candidates += Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"
    }
    $programFilesX86 = ${env:ProgramFiles(x86)}
    if ($programFilesX86) {
        $candidates += Join-Path $programFilesX86 "Inno Setup 6\ISCC.exe"
    }
    if ($env:ProgramFiles) {
        $candidates += Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe"
    }
    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }
    Fail "Inno Setup 6 not found. Install it with: winget install JRSoftware.InnoSetup --scope user"
}

if (-not (Test-Path -LiteralPath $versionPath)) {
    Fail "version file not found: $versionPath"
}
$Version = [string](Get-Content -LiteralPath $versionPath -Raw)
$Version = $Version.Trim()
if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    Fail "version must contain three numeric parts, for example 0.2.0."
}
$Architecture = $Architecture.ToLowerInvariant()

$csc = Find-Csc
$iscc = $null
if (-not $NoInstaller) {
    $iscc = Find-Iscc
}

foreach ($path in @($bin, $obj, $dist)) {
    New-Item -ItemType Directory -Force -Path $path | Out-Null
}
foreach ($path in @($icon, $manifest)) {
    if (-not (Test-Path -LiteralPath $path)) {
        Fail "Required asset not found: $path"
    }
}

$versionFile = Join-Path $obj "Version.g.cs"
$versionText = @"
using System.Reflection;

[assembly: AssemblyVersion("$Version.0")]
[assembly: AssemblyFileVersion("$Version.0")]
[assembly: AssemblyInformationalVersion("$Version")]
"@
[System.IO.File]::WriteAllText($versionFile, $versionText, (New-Object System.Text.UTF8Encoding($false)))

$sources = @(
    Get-ChildItem -Path $src -Recurse -File -Filter "*.cs" |
        Sort-Object FullName |
        ForEach-Object { $_.FullName }
)
if ($sources.Count -eq 0) {
    Fail "No C# sources found under $src."
}
$sources += $versionFile

Remove-Item -LiteralPath $exe -Force -ErrorAction SilentlyContinue

$compilerArgs = @(
    "/nologo",
    "/target:winexe",
    "/platform:$Architecture",
    "/optimize+",
    "/codepage:65001",
    "/win32icon:$icon",
    "/win32manifest:$manifest",
    "/out:$exe",
    "/reference:System.dll",
    "/reference:System.Core.dll",
    "/reference:System.Drawing.dll",
    "/reference:System.Windows.Forms.dll"
)
$compilerArgs += $sources

Write-Host "Building $exe..."
& $csc @compilerArgs
if ($LASTEXITCODE -ne 0) {
    Fail "csc failed with exit code $LASTEXITCODE."
}
if (-not (Test-Path -LiteralPath $exe)) {
    Fail "Build did not produce $exe."
}
$expectedArchitecture = if ($Architecture -eq "x64") { "Amd64" } else { "X86" }
$actualArchitecture = [System.Reflection.AssemblyName]::GetAssemblyName($exe).ProcessorArchitecture.ToString()
if ($actualArchitecture -ne $expectedArchitecture) {
    Fail "Build produced $actualArchitecture instead of $expectedArchitecture."
}

if (-not $NoInstaller) {
    Write-Host "Building installer..."
    $archDefine = if ($Architecture -eq "x64") { "/DArchX64" } else { "/DArchX86" }
    & $iscc "/DAppVersion=$Version" "/DAppArchitecture=$Architecture" $archDefine $iss
    if ($LASTEXITCODE -ne 0) {
        Fail "Inno Setup failed with exit code $LASTEXITCODE."
    }
    $installer = Join-Path $dist "v${Version}_installer_${Architecture}.exe"
    if (-not (Test-Path -LiteralPath $installer)) {
        Fail "Installer build did not produce $installer."
    }

    $checksumFile = Join-Path $dist "SHA256SUMS_$Architecture.txt"
    $checksum = (Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash.ToLowerInvariant()
    "$checksum  $([System.IO.Path]::GetFileName($installer))" |
        Set-Content -LiteralPath $checksumFile -Encoding ascii
    Write-Host "Installer: $installer"
    Write-Host "Checksums: $checksumFile"
}

Write-Host "Build complete: $exe"
