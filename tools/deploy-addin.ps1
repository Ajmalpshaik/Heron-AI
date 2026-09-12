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

# THE BUILD OUTPUT IS SHARED BETWEEN ALL EIGHT RELEASES, and the newest one
# wins the search above. tools/check-compile.py builds 2020 through 2027 into
# this same folder, so running it leaves 2027's assemblies sitting there - and
# deploying those into Revit 2024 produced exactly one symptom: "Revit cannot
# run the external application Heron AI", with nothing to say why. Found by
# doing it, 2026-09-08.
#
# 2020-2024 are .NET Framework and have no deps.json. 2025+ are .NET and
# always do, naming the runtime they need. That one file separates them.
$depsFile = Join-Path $buildOut "Heron.Revit.Addin.deps.json"
$isDotNet = Test-Path $depsFile
$wantsDotNet = [int]$RevitVersion -ge 2025

if ($isDotNet -ne $wantsDotNet) {
    $found = if ($isDotNet) { ".NET (Revit 2025 and later)" } else { ".NET Framework (Revit 2024 and earlier)" }
    $need  = if ($wantsDotNet) { ".NET (Revit 2025 and later)" } else { ".NET Framework (Revit 2024 and earlier)" }
    throw "The build in $buildOut is $found, but Revit $RevitVersion needs $need. Revit would refuse to load it and would not say why. Rebuild first:`n  dotnet build revit\Heron.Revit.Addin\Heron.Revit.Addin.csproj -c $Configuration -p:RevitVersion=$RevitVersion"
}

# 2025 and 2026 are .NET 8; 2027 moved to .NET 10. Both have a deps.json, so
# the check above passes either way and this is what separates them.
if ($isDotNet) {
    $wantedRuntime = if ([int]$RevitVersion -ge 2027) { "v10.0" } else { "v8.0" }
    $runtimeName = (Get-Content $depsFile -Raw | ConvertFrom-Json).runtimeTarget.name
    if ($runtimeName -notlike "*$wantedRuntime*") {
        throw "The build in $buildOut targets $runtimeName, but Revit $RevitVersion needs $wantedRuntime. Rebuild first:`n  dotnet build revit\Heron.Revit.Addin\Heron.Revit.Addin.csproj -c $Configuration -p:RevitVersion=$RevitVersion"
    }
}

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

# The runtime metadata. A .NET build (Revit 2025+) emits deps.json - and for
# some project shapes runtimeconfig.json - naming the runtime and every
# dependency the host must resolve. THE ASSEMBLIES ALONE ARE NOT A DEPLOYMENT.
#
# This was missed until 2026-09-12: the check above reads deps.json to decide
# whether the build is the right flavour, and then the copy took *.dll only,
# so the file the check had just relied on was left behind. A guard that
# passes while the thing it guards is broken is worse than no guard - it was
# the reason nobody looked here.
#
# .NET Framework (2020-2024) emits neither, so there is nothing to copy and
# nothing to check. That is why this went unnoticed: 2024 is the release
# everything has been proved on.
if ($isDotNet) {
    $runtimeFiles = Get-ChildItem -Path $buildOut -Filter "*.json" |
                    Where-Object { $_.Name -like "*.deps.json" -or $_.Name -like "*.runtimeconfig.json" }

    if (-not $runtimeFiles) {
        throw "Revit $RevitVersion needs .NET runtime metadata and the build in $buildOut has none. Revit would load the assembly and fail to resolve its dependencies, reporting only that it cannot run the external application. Rebuild first:`n  dotnet build revit\Heron.Revit.Addin\Heron.Revit.Addin.csproj -c $Configuration -p:RevitVersion=$RevitVersion"
    }

    $runtimeFiles | ForEach-Object {
        Copy-Item $_.FullName -Destination $addinDir -Force
        Write-Host "  $($_.Name)"
    }
}

$pdb = Get-ChildItem -Path $buildOut -Filter *.pdb -ErrorAction SilentlyContinue
if ($Configuration -eq "Debug" -and $pdb) {
    $pdb | ForEach-Object { Copy-Item $_.FullName -Destination $addinDir -Force }
}

# Verify what was WRITTEN, not what was intended. The deployed folder is what
# Revit reads, and every failure this script has caused looked like success at
# this point.
$deployedDll  = Join-Path $addinDir "Heron.Revit.Addin.dll"
$deployedDeps = Join-Path $addinDir "Heron.Revit.Addin.deps.json"

if (-not (Test-Path $deployedDll)) {
    throw "Deployment finished but $deployedDll is not there. Nothing was installed."
}
if ($isDotNet -and -not (Test-Path $deployedDeps)) {
    throw "Deployment finished but $deployedDeps is not there, and Revit $RevitVersion needs it. Do not start Revit against this folder."
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
Write-Host "  2. Ribbon > Heron AI > Heron   (click to connect, click again to disconnect)"
Write-Host "  3. python mcp\client\heron_bridge_client.py ping"
