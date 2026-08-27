# Heron-Agent:  HERON-INS-ORC-001, HERON-INS-ENV-002
# Heron-Step:   1
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

<#
.SYNOPSIS
    Builds and installs Heron for every Revit on this machine. One command.

.DESCRIPTION
    Detects installed Revit versions, builds the add-in for each, and deploys
    it per-user - no administrator rights needed.

    The working prototype of the Installation Orchestrator
    (HERON-INS-ORC-001) and Environment Detection (HERON-INS-ENV-002).

.PARAMETER RevitVersion
    Build for one version only. Omit to do every version found.

.PARAMETER Configuration
    Debug (default) or Release.

.EXAMPLE
    .\tools\setup.ps1

.EXAMPLE
    .\tools\setup.ps1 -RevitVersion 2024

.NOTES
    Revit must be closed. A loaded assembly cannot be replaced, so installing
    over a running Revit silently half-updates (docs/07).
#>
[CmdletBinding()]
param(
    [string] $RevitVersion,
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

function Write-Step($n, $text) { Write-Host ""; Write-Host "[$n] $text" -ForegroundColor Cyan }
function Write-Ok($text)       { Write-Host "    OK   $text" -ForegroundColor Green }
function Write-Warn($text)     { Write-Host "    !    $text" -ForegroundColor Yellow }

Write-Host ""
Write-Host "  Heron AI - setup" -ForegroundColor White
Write-Host "  ================"

# --- 1. environment ---------------------------------------------------------
Write-Step 1 "Checking the environment"

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    throw "The .NET SDK is not installed. Get it from https://dotnet.microsoft.com/download"
}
Write-Ok ".NET SDK $(& dotnet --version)"

if (Get-Process -Name "Revit" -ErrorAction SilentlyContinue) {
    Write-Host ""
    Write-Host "    Revit is running." -ForegroundColor Red
    Write-Host "    Close it and run this again - a loaded assembly cannot be replaced," -ForegroundColor Red
    Write-Host "    so installing now would half-update and look like it worked." -ForegroundColor Red
    exit 1
}
Write-Ok "Revit is not running"

# --- 2. which Revit versions are installed ----------------------------------
Write-Step 2 "Looking for Revit"

# Discover what is installed rather than checking a fixed list of years. A
# hardcoded upper bound means the next Revit is invisible to Heron's own
# installer - and D-05 promises 2020 to latest, permanently.
$autodesk = Join-Path ${env:ProgramFiles} "Autodesk"
$found = @()
if (Test-Path $autodesk) {
    $found = @(Get-ChildItem -Path $autodesk -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -match '^Revit (\d{4})$' -and (Test-Path (Join-Path $_.FullName "Revit.exe")) } |
        ForEach-Object { $_.Name.Substring(6) } |
        Sort-Object)
}

if ($found.Count -eq 0) {
    Write-Warn "No Revit installation found under $env:ProgramFiles\Autodesk."
    Write-Warn "Heron can still be built, but there is nothing to install it into."
    if (-not $RevitVersion) {
        Write-Host ""
        Write-Host "    Pass -RevitVersion to build anyway, e.g. .\tools\setup.ps1 -RevitVersion 2024"
        exit 1
    }
} else {
    Write-Ok "Found Revit: $($found -join ', ')"
}

$targets = if ($RevitVersion) { @($RevitVersion) } else { $found }
if ($RevitVersion -and $found.Count -gt 0 -and $found -notcontains $RevitVersion) {
    Write-Warn "Revit $RevitVersion was not detected. Building for it anyway."
}

# --- 3. build and deploy, per version ---------------------------------------
$succeeded = @()
$failed = @()

foreach ($version in $targets) {
    Write-Step 3 "Revit $version - building"

    $proj = Join-Path $repoRoot "revit\Heron.Revit.Addin\Heron.Revit.Addin.csproj"
    & dotnet build $proj -c $Configuration -p:RevitVersion=$version --nologo -v minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Warn "Build failed for Revit $version"
        $failed += $version
        continue
    }
    Write-Ok "Built"

    Write-Host ""
    Write-Host "[4] Revit $version - deploying" -ForegroundColor Cyan
    # try/catch, not $LASTEXITCODE: a PowerShell script does not set it, so the
    # old check silently read the exit code of the dotnet build above - always 0 -
    # and a failed deploy was recorded as a success.
    try {
        & (Join-Path $PSScriptRoot "deploy-addin.ps1") -RevitVersion $version -Configuration $Configuration
    }
    catch {
        Write-Warn "Deploy failed for Revit $version - $($_.Exception.Message)"
        $failed += $version
        continue
    }
    $succeeded += $version
}

# --- 5. what next -----------------------------------------------------------
Write-Host ""
Write-Host "  ================" -ForegroundColor White

if ($succeeded.Count -gt 0) {
    Write-Host "  Installed for Revit: $($succeeded -join ', ')" -ForegroundColor Green
    Write-Host ""
    Write-Host "  Next:"
    Write-Host "    1. Start Revit"
    Write-Host "    2. Ribbon -> Heron AI -> Connect Heron"
    Write-Host "    3. Back here, run:"
    Write-Host "         python mcp\client\heron_bridge_client.py ping" -ForegroundColor White
    Write-Host ""
    Write-Host "  If anything goes wrong:"
    Write-Host "         python mcp\client\heron_bridge_client.py doctor" -ForegroundColor White
}

if ($failed.Count -gt 0) {
    Write-Host "  Failed for Revit: $($failed -join ', ')" -ForegroundColor Red
    exit 1
}

Write-Host ""
