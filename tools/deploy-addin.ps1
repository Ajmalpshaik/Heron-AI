# Heron-Agent:  HERON-REVIT-DEP-024
# Heron-Step:   1
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

<#
.SYNOPSIS
    Deploys the Heron add-in into Revit for the current user.

.DESCRIPTION
    Copies the built assemblies and the .addin manifest into

        %APPDATA%\Autodesk\Revit\Addins\<version>\

    Per-user, so it needs NO administrator rights - which matters when the
    target is a BIM modeller on a locked-down corporate machine
    (docs/07 section 5).

.PARAMETER RevitVersion
    Revit release to deploy for. Default 2024.

.PARAMETER Configuration
    Build configuration. Default Debug.

.PARAMETER Remove
    Uninstall instead of installing.

.EXAMPLE
    .\tools\deploy-addin.ps1 -RevitVersion 2024

.EXAMPLE
    .\tools\deploy-addin.ps1 -RevitVersion 2024 -Remove

.NOTES
    Revit must be closed. Assemblies loaded into Revit cannot be unloaded,
    so an update always needs a restart (docs/07 section 7).
#>
[CmdletBinding()]
param(
    [string] $RevitVersion = "2024",
    [string] $Configuration = "Debug",
    [switch] $Remove
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot

. (Join-Path $PSScriptRoot "HeronRevit.ps1")

$target   = Join-Path $env:APPDATA "Autodesk\Revit\Addins\$RevitVersion"
$addinDir = Join-Path $target "Heron"
$manifest = Join-Path $target "Heron.addin"

if ($Remove) {
    if (Test-Path $manifest) { Remove-Item $manifest -Force;               Write-Host "Removed $manifest" }
    if (Test-Path $addinDir) { Remove-Item $addinDir -Recurse -Force;      Write-Host "Removed $addinDir" }
    Write-Host ""
    Write-Host "Heron uninstalled for Revit $RevitVersion. Restart Revit to unload it."
    return
}

# Revit holds a lock on loaded assemblies; deploying under it silently fails.
# Checked fresh here rather than trusting a caller: a build takes long enough
# that Revit can be opened in between, and this is the last gate before files
# are replaced.
$blocked = Get-RevitBlockReason -RevitVersion $RevitVersion -Running (Get-RunningRevit)
if ($blocked) {
    throw "Cannot install for Revit ${RevitVersion}: $blocked. Close it and run this again - a loaded assembly cannot be replaced, so installing now would half-update and look like it worked."
}

# Find the build output rather than assuming its shape. Directory.Build.props
# sets Platform=x64, so the path is bind\$Configuration - but that is a
# build detail this script should not have to know.
$projDir  = Join-Path $repoRoot "revit\Heron.Revit.Addin"
$buildOut = Get-ChildItem -Path (Join-Path $projDir "bin") -Recurse -Filter "Heron.Revit.Addin.dll" -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -like "*$Configuration*" } |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 1 -ExpandProperty DirectoryName

if (-not $buildOut) {
    throw "No $Configuration build found under $projDir\bin. Run:`n  dotnet build -c $Configuration -p:RevitVersion=$RevitVersion"
}
Write-Host "  from $buildOut"

New-Item -ItemType Directory -Force -Path $addinDir | Out-Null

# Assemblies only. Never the Revit API DLLs - they are Autodesk's, are not
# redistributable, and Revit supplies them at runtime (docs/07 section 4).
Get-ChildItem -Path $buildOut -Filter *.dll |
    Where-Object { $_.Name -notlike "RevitAPI*" -and $_.Name -notlike "AdWindows*" } |
    ForEach-Object {
        Copy-Item $_.FullName -Destination $addinDir -Force
        Write-Host "  $($_.Name)"
    }

# Ribbon icons. IconLoader looks for them in Resources beside the assembly,
# so the folder has to travel with it - without them the buttons deploy blank.
$resourceSource = Join-Path $buildOut "Resources"
if (Test-Path $resourceSource) {
    $resourceTarget = Join-Path $addinDir "Resources"
    New-Item -ItemType Directory -Force -Path $resourceTarget | Out-Null
    Get-ChildItem -Path $resourceSource -File | ForEach-Object {
        Copy-Item $_.FullName -Destination $resourceTarget -Force
        Write-Host "  Resources\$($_.Name)"
    }
}

$pdb = Get-ChildItem -Path $buildOut -Filter *.pdb -ErrorAction SilentlyContinue
if ($Configuration -eq "Debug" -and $pdb) {
    $pdb | ForEach-Object { Copy-Item $_.FullName -Destination $addinDir -Force }
}

# Point the manifest at the deployed assembly.
$manifestSource = Join-Path $repoRoot "revit\Heron.Revit.Addin\Heron.addin"
(Get-Content $manifestSource -Raw).Replace(
    "<Assembly>Heron.Revit.Addin.dll</Assembly>",
    "<Assembly>Heron\Heron.Revit.Addin.dll</Assembly>"
) | Set-Content -Path $manifest -Encoding UTF8

Write-Host ""
Write-Host "Deployed to $addinDir"
Write-Host "Manifest    $manifest"
Write-Host ""
Write-Host "Next:"
Write-Host "  1. Start Revit $RevitVersion"
Write-Host "  2. Ribbon > Heron AI > Connect Heron"
Write-Host "  3. python mcp\client\heron_bridge_client.py ping"
