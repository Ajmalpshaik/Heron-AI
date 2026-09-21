# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

<#
.SYNOPSIS
    Puts Stage 2's two shape proofs into Revit, and takes them out again.

.DESCRIPTION
    STAGE 2 ONLY. This is not an installer and it never becomes one.

    docs/work-notes/plans/plugin-extension/02-implementation.md Stage 2 asks
    three questions that only a real Revit can answer:

        1  can TWO Heron tabs live in one Revit?
        2  can the Heron tab be built by the tools alone, the AI Bridge
           alone, and BOTH - as one tab, never two with the same name?
        3  does deleting the second product's two files take its tab away and
           leave the first tab untouched?

    Answering them needs the two throwaway products on disk where Revit looks.
    tools\deploy-addin.ps1 cannot do it: every path in it names
    Heron.Revit.Addin - the project folder, the DLL, the manifest, the
    rollback. That script is PROVEN and is left alone rather than widened for
    a throwaway.

    WHY THIS IS NOT A SECOND INSTALLER, which R-31 would refuse. It installs
    nothing a user gets. It has no product list, no version detection, no
    download, no replace and no rollback. Stage 3 builds the one real engine
    and drives deploy-addin.ps1 from it. This copies two proof files so a
    person can look at a ribbon, and it is deleted with the rest of Stage 2.

    WHAT IT WILL NOT TOUCH, EVER
    -----------------------------
    Heron.addin and Heron.Revit.Addin.dll - the add-in that works today - and
    anything under %APPDATA%\Heron, which is the user's own data. It writes
    and deletes exactly four paths, all of them named after the two proofs,
    and it checks each one before removing it.

.PARAMETER RevitVersion
    Revit release to deploy for. Default 2024.

.PARAMETER Product
    Doc, Tools, or Both. Default Both.

    THE THREE COMBINATIONS STAGE 2 ASKS FOR are made with -Product and
    -Remove. Tools-only is the case that breaks first, and it is the one
    R-34 promises:

        AI Bridge only    -Product Both -Remove          (leaves today's add-in)
        Tools only        -Product Tools                 (with the bridge removed
                                                          by deploy-addin.ps1 -Remove)
        Both              -Product Tools                 (with the bridge installed)

.PARAMETER Configuration
    Build configuration. Default Debug.

.PARAMETER Remove
    Take the proofs out again. This is question 3 above.

.EXAMPLE
    .\tools\deploy-stage2-proof.ps1 -RevitVersion 2024

.EXAMPLE
    .\tools\deploy-stage2-proof.ps1 -RevitVersion 2024 -Product Tools

.EXAMPLE
    .\tools\deploy-stage2-proof.ps1 -RevitVersion 2024 -Remove

.NOTES
    REVIT MUST BE CLOSED. Assemblies loaded into Revit cannot be unloaded, so
    a copy over a loaded DLL fails and a delete of one fails too. This
    refuses and names the release that is open, using the same detection
    deploy-addin.ps1 uses - tools\HeronRevit.ps1 - rather than a second copy
    of that rule.

    THE DOWNLOAD MARK. Windows stamps anything that arrived from a browser
    with a zone marker, and a marked DLL makes Revit fail to load the add-in
    with an error naming nothing useful. AJ Tools' installer learned that the
    hard way - L4 in 04-lessons-from-aj-tools.md, and R-37. Cleared here
    before the copy and again on the copies, because somebody proving Stage 2
    may well have downloaded this repository as a zip.

    ASCII ONLY. Windows PowerShell 5.1 reads a file with no BOM as ANSI, and
    tools\check-structure.py fails a non-ASCII .ps1 without one.
#>

param(
    [string] $RevitVersion = "2024",
    [ValidateSet("Doc", "Tools", "Both")] [string] $Product = "Both",
    [string] $Configuration = "Debug",
    [switch] $Remove
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot

. (Join-Path $PSScriptRoot "HeronRevit.ps1")

$target = Join-Path $env:APPDATA "Autodesk\Revit\Addins\$RevitVersion"

# The two proofs, and everything this script is allowed to know about them.
# One row per product; nothing below names a product any other way.
$proofs = @(
    [PSCustomObject]@{
        Name     = "Doc"
        Project  = "Heron.Doc"
        Assembly = "Heron.Doc.dll"
        Manifest = "Heron.Doc.addin"
        Tab      = "Heron Doc"
    },
    [PSCustomObject]@{
        Name     = "Tools"
        Project  = "Heron.Tools"
        Assembly = "Heron.Tools.dll"
        Manifest = "Heron.Tools.addin"
        Tab      = "Heron"
    }
)

$wanted = @($proofs | Where-Object { $Product -eq "Both" -or $_.Name -eq $Product })

# ---------------------------------------------------------------- guard rails

# THE ADD-IN THAT WORKS TODAY. Named here so the removal below can refuse to
# go anywhere near it, rather than relying on nobody ever passing a bad name.
$protected = @("Heron.addin", "Heron.Revit.Addin.dll", "Heron")

function Assert-NotTheRealAddin {
    param([string] $Leaf)
    if ($protected -contains $Leaf) {
        throw "Refusing to touch $Leaf. That is the Heron add-in that works today, and this script only ever handles the two Stage 2 proofs. Use tools\deploy-addin.ps1 for the real add-in."
    }
}

# ------------------------------------------------------------- Revit is open?

$running = Get-RunningRevit
$blocked = Get-RevitBlockReason -RevitVersion $RevitVersion -Running $running

if ($blocked) {
    Write-Host ""
    Write-Host "Close Revit first." -ForegroundColor Yellow
    Write-Host "  $blocked."
    Write-Host "  Open now: $(Format-RunningRevit $running)"
    Write-Host ""
    Write-Host "  Revit holds its add-in DLLs open, so nothing can be copied over"
    Write-Host "  them or deleted while it is running. Nothing has been changed."
    Write-Host ""
    exit 1
}

# --------------------------------------------------------------------- remove

if ($Remove) {
    Write-Host ""
    Write-Host "Removing the Stage 2 proofs for Revit $RevitVersion"
    Write-Host ""

    $gone = 0
    foreach ($p in $wanted) {
        Assert-NotTheRealAddin $p.Manifest
        Assert-NotTheRealAddin $p.Project

        $manifestPath = Join-Path $target $p.Manifest
        $folderPath = Join-Path $target $p.Project

        foreach ($path in @($manifestPath, $folderPath)) {
            if (Test-Path $path) {
                Remove-Item -Path $path -Recurse -Force
                Write-Host "  removed  $path"
                $gone++
            }
            else {
                Write-Host "  not there  $path"
            }
        }

        # VERIFY IT IS ACTUALLY GONE. R-38b says a delete that half succeeded
        # is the one outcome worse than waiting, and this is the same rule in
        # miniature: say so rather than reporting a removal that did not
        # happen.
        foreach ($path in @($manifestPath, $folderPath)) {
            if (Test-Path $path) {
                throw "$path is still there after being deleted. Something is holding it open - close Revit and every file browser looking at that folder, then run this again. Nothing else has been changed."
            }
        }
    }

    Write-Host ""
    Write-Host "  $gone path(s) removed. The Heron add-in that was already"
    Write-Host "  installed has not been touched."
    Write-Host ""
    Write-Host "  Start Revit and look at the ribbon. What should be true:"
    foreach ($p in $wanted) {
        Write-Host "    the $($p.Tab) tab no longer carries the $($p.Name) proof panel"
    }
    Write-Host "    the Heron tab and its three buttons still work"
    Write-Host ""
    exit 0
}

# --------------------------------------------------------------------- deploy

Write-Host ""
Write-Host "Deploying the Stage 2 proofs for Revit $RevitVersion"
Write-Host ""

if (-not (Test-Path $target)) {
    New-Item -ItemType Directory -Path $target -Force | Out-Null
}

foreach ($p in $wanted) {
    $projDir = Join-Path $repoRoot "revit\$($p.Project)"

    $built = Get-ChildItem -Path (Join-Path $projDir "bin") -Recurse -Filter $p.Assembly -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    $rebuild = "dotnet build revit\$($p.Project)\$($p.Project).csproj -c $Configuration -p:RevitVersion=$RevitVersion"

    if (-not $built) {
        throw "No $($p.Assembly) under $projDir\bin, so there is nothing to deploy. Build it for this release first:`n  $rebuild"
    }

    $buildOut = $built.DirectoryName

    # .NET 8 AND .NET 10 NEED MORE THAN THE DLL. Revit 2025 and later load
    # the assembly and then fail to resolve its dependencies, reporting only
    # that it cannot run the external application. L4 / R-40: the unit of
    # installation is a FOLDER of files, not one file.
    $payload = @(Join-Path $buildOut $p.Assembly)
    foreach ($extra in @("$($p.Project).deps.json", "$($p.Project).runtimeconfig.json")) {
        $extraPath = Join-Path $buildOut $extra
        if (Test-Path $extraPath) { $payload += $extraPath }
    }

    if ([int] $RevitVersion -ge 2025) {
        $deps = Join-Path $buildOut "$($p.Project).deps.json"
        if (-not (Test-Path $deps)) {
            throw "Revit $RevitVersion runs on .NET 8 or later and the build in $buildOut carries no $($p.Project).deps.json. Revit would load the assembly and fail to resolve its dependencies, saying only that it cannot run the external application. Rebuild first:`n  $rebuild"
        }
    }

    # THE DOWNLOAD MARK, cleared before the copy - R-37.
    foreach ($file in $payload) { Unblock-File -Path $file -ErrorAction SilentlyContinue }

    # A FOLDER PER PRODUCT, never a shared one - R-41. Two products carrying
    # different versions of the same helper assembly must not overwrite each
    # other, and uninstall becomes one folder and one manifest.
    $folderPath = Join-Path $target $p.Project
    Assert-NotTheRealAddin $p.Project

    if (-not (Test-Path $folderPath)) {
        New-Item -ItemType Directory -Path $folderPath -Force | Out-Null
    }

    foreach ($file in $payload) {
        Copy-Item -Path $file -Destination $folderPath -Force
        Write-Host "  copied   $(Split-Path -Leaf $file)  ->  $folderPath"
    }

    # AND AGAIN ON THE COPIES. AJ Tools' installer clears it twice, and its
    # own comment ties the marker to the Revit error it produces.
    Get-ChildItem -Path $folderPath -File | ForEach-Object {
        Unblock-File -Path $_.FullName -ErrorAction SilentlyContinue
    }

    # The manifest sits BESIDE the folder, which is where Revit scans.
    $manifestSource = Join-Path $projDir $p.Manifest
    if (-not (Test-Path $manifestSource)) {
        throw "$manifestSource is missing, so Revit would have a DLL it never looks at."
    }

    Assert-NotTheRealAddin $p.Manifest
    $manifestPath = Join-Path $target $p.Manifest

    # Point <Assembly> at where the DLL actually landed. Same rewrite
    # deploy-addin.ps1 makes, for the same reason: the manifest in the
    # repository names the file, the deployed one names the path.
    $xml = Get-Content -Path $manifestSource -Raw
    $xml = $xml.Replace(
        "<Assembly>$($p.Assembly)</Assembly>",
        "<Assembly>$($p.Project)\$($p.Assembly)</Assembly>")

    Set-Content -Path $manifestPath -Value $xml -Encoding UTF8
    Unblock-File -Path $manifestPath -ErrorAction SilentlyContinue
    Write-Host "  wrote    $manifestPath"

    # VERIFY IT LANDED. L7: check the file is actually there, and stop if it
    # is not, rather than reporting a deploy that did not happen.
    $deployedDll = Join-Path $folderPath $p.Assembly
    if (-not (Test-Path $deployedDll)) {
        throw "$deployedDll is not there after the copy. Nothing further has been changed."
    }
    if (-not (Test-Path $manifestPath)) {
        throw "$manifestPath is not there after being written. Nothing further has been changed."
    }

    Write-Host ""
}

Write-Host "  Done. Start Revit $RevitVersion and LOOK AT THE RIBBON."
Write-Host ""
Write-Host "  What should be true, and what to record:"
foreach ($p in $wanted) {
    if ($p.Name -eq "Doc") {
        Write-Host "    a SECOND tab called 'Heron Doc', with one panel and one button"
        Write-Host "    the Heron tab and its three buttons still work"
    }
    else {
        Write-Host "    ONE tab called 'Heron' carrying a 'Tools' panel"
        Write-Host "    and the 'AI Bridge' panel too, if that add-in is installed"
        Write-Host "    NEVER two tabs both called 'Heron' - that is the failure"
    }
}
Write-Host ""
Write-Host "  TAKE A SCREENSHOT OF EACH COMBINATION. Stage 2 is not done until"
Write-Host "  a real Revit has been seen showing all three, and a compile is"
Write-Host "  not a proof."
Write-Host ""
