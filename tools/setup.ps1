# Heron-Agent:  HERON-INS-ORC-001
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

. (Join-Path $PSScriptRoot "HeronRevit.ps1")

function Write-Step($n, $text) { Write-Host ""; Write-Host "[$n] $text" -ForegroundColor Cyan }
function Write-Ok($text)       { Write-Host "    OK   $text" -ForegroundColor Green }
function Write-Warn($text)     { Write-Host "    !    $text" -ForegroundColor Yellow }

Write-Host ""
Write-Host "  Heron AI - setup" -ForegroundColor White
Write-Host "  ================"

# --- 1. environment ---------------------------------------------------------
Write-Step 1 "Checking the environment"

# Python is a prerequisite, not an extra - the MCP server is Python (D-06).
# Checked and named here rather than discovered later by a user wondering why
# nothing happens when they ask Claude a question.
$python = Get-PythonStatus
if ($python.Found -and $python.HasMcp) {
    $scope = if ($python.PerUser) { ", per-user" } else { "" }
    Write-Ok "$($python.Version)$scope, with the MCP package"
} else {
    Write-Host ""
    [void](Write-PythonAdvice $python)
    Write-Host ""
    Write-Warn "The Revit add-in below will still install and work."
    Write-Warn "Only asking Claude questions needs Python."
    Write-Host ""
}

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    throw "The .NET SDK is not installed. Get it from https://dotnet.microsoft.com/download"
}
Write-Ok ".NET SDK $(& dotnet --version)"

# Revit locks every assembly it has loaded, so an add-in cannot be replaced
# underneath a running one. That is true PER VERSION: Revit 2024 being open
# says nothing about whether 2020 can be installed for.
$running = Get-RunningRevit
if ($running.Count -eq 0) {
    Write-Ok "No Revit is open"
} else {
    Write-Warn "Open right now: $(Format-RunningRevit $running)"
    Write-Warn "Those releases will be skipped - installing under an open Revit"
    Write-Warn "half-updates it and looks like it worked. Every other release is"
    Write-Warn "installed as normal."
}

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
$skipped = @()

foreach ($version in $targets) {
    $blocked = Get-RevitBlockReason -RevitVersion $version -Running $running
    if ($blocked) {
        Write-Step 3 "Revit $version - skipped"
        Write-Warn "$blocked."
        Write-Warn "Close it and run this again to install for Revit $version."
        $skipped += $version
        continue
    }

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
    Write-Host "  First - and this needs no Revit at all:"
    Write-Host "         dotnet build tests\Heron.Bridge.TestHost -p:RevitVersion=2024" -ForegroundColor White
    Write-Host "         python tests\test_bridge_roundtrip.py" -ForegroundColor White
    Write-Host "    It starts a Revit-free bridge host and checks the pipe and the session"
    Write-Host "    lease. If something is wrong there, it is much easier to find here than"
    Write-Host "    three steps later with Revit open."
    Write-Host ""
    Write-Host "  Then:"
    Write-Host "    1. Start Revit"
    Write-Host "    2. Ribbon -> Heron -> AI Bridge -> Heron   (click to connect, again to disconnect)"
    Write-Host "    3. Back here, run:"
    Write-Host "         python mcp\client\heron_bridge_client.py ping" -ForegroundColor White
    Write-Host ""
    Write-Host "  If anything goes wrong:"
    Write-Host "         python mcp\client\heron_bridge_client.py doctor" -ForegroundColor White
}

if ($skipped.Count -gt 0) {
    Write-Host ""
    Write-Host "  Skipped, still open in Revit: $($skipped -join ', ')" -ForegroundColor Yellow
    Write-Host "  Close them and run this again. Nothing was changed for those."
}

if ($failed.Count -gt 0) {
    Write-Host ""
    Write-Host "  Failed for Revit: $($failed -join ', ')" -ForegroundColor Red
    exit 1
}

if ($succeeded.Count -eq 0 -and $skipped.Count -gt 0) {
    Write-Host ""
    Write-Host "  Nothing was installed - every release asked for is open." -ForegroundColor Red
    exit 1
}

Write-Host ""
