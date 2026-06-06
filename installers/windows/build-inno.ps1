#requires -Version 7.0
<#
.SYNOPSIS
    Builds an Inno Setup installer (pandoc-gui-setup.exe) for Pandoc GUI.
.DESCRIPTION
    Publishes the app self-contained, then compiles installers/windows/installer.iss
    with the Inno Setup command-line compiler (ISCC). Output lands under dist/ (gitignored).
    Classic wizard: license, choose destination folder, optional desktop icon.
.EXAMPLE
    pwsh installers/windows/build-inno.ps1 -Version 0.1.0
#>
[CmdletBinding()]
param(
    # Empty = derive from the published PandocGui.exe (single source: <Version> in the csproj).
    [string]$Version = '',
    [string]$Runtime = 'win-x64',
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
# Make native command non-zero exits throw (PowerShell 7.3+).
$PSNativeCommandUseErrorActionPreference = $true

$repoRoot   = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$project    = Join-Path $repoRoot 'src/PandocGui/PandocGui.csproj'
$issScript  = Join-Path $PSScriptRoot 'installer.iss'
$publishDir = Join-Path $repoRoot "dist/publish/$Runtime"
$releaseDir = Join-Path $repoRoot "dist/releases/$Runtime"
$repoUrl    = 'https://github.com/Ombrelin/pandoc-gui'

# Resolve ISCC.exe (the Inno Setup installer does NOT add it to PATH).
$iscc = (Get-Command iscc -ErrorAction SilentlyContinue)?.Source
if (-not $iscc) {
    $iscc = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
        "${env:LocalAppData}\Programs\Inno Setup 6\ISCC.exe"
    ) | Where-Object { Test-Path $_ } | Select-Object -First 1
}
if (-not $iscc) {
    throw "Inno Setup compiler (ISCC.exe) not found. Install it: winget install -e --id JRSoftware.InnoSetup"
}

Write-Host "==> Publishing $Runtime ($Configuration), self-contained" -ForegroundColor Cyan
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
dotnet publish $project -c $Configuration -r $Runtime --self-contained -o $publishDir

if ([string]::IsNullOrWhiteSpace($Version)) {
    $exe = Join-Path $publishDir 'PandocGui.exe'
    $Version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($exe).FileVersion
    Write-Host "==> Version from assembly: $Version" -ForegroundColor Cyan
}

Write-Host "==> Compiling installer v$Version with $iscc" -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null
& $iscc `
    "/DMyAppVersion=$Version" `
    "/DMyAppURL=$repoUrl" `
    "/DSourceDir=$publishDir" `
    "/DOutputDir=$releaseDir" `
    $issScript

Write-Host "==> Done. Installer in $releaseDir" -ForegroundColor Green
Get-ChildItem $releaseDir -Filter *.exe | Select-Object Name, Length
