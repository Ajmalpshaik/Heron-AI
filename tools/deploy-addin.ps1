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

.PARAMETER Rollback
    Put back the install this script last replaced, and stop.

.EXAMPLE
    .\tools\deploy-addin.ps1 -RevitVersion 2024

.EXAMPLE
    .\tools\deploy-addin.ps1 -RevitVersion 2024 -Remove

.EXAMPLE
    .\tools\deploy-addin.ps1 -RevitVersion 2024 -Rollback

.NOTES
    Revit must be closed. Assemblies loaded into Revit cannot be unloaded,
    so an update always needs a restart (docs/07 section 7).

    ROLLBACK, AND WHY IT IS HERE RATHER THAN IN heron-backup.py
    ------------------------------------------------------------
    docs/07 section 7 rule 5 says rollback must be TESTED, not merely
    implemented, and brain/heron_update.py enforces that: a release without a
    recorded `rollback_tested` is REFUSED with ROLLBACK_NOT_TESTED. Until
    2026-09-19 nothing in this repository could perform the test it demands.

    tools/heron-backup.py covers the DATA class - `%APPDATA%\Heron`, the
    audit log, the settings and the knowledge. It has never touched the
    add-in, and the add-in is the PRODUCT class. So the one thing an update
    replaces was the one thing with no way back: this script overwrote the
    previous install with Copy-Item -Force and kept nothing.

    That is what the block below fixes. Before anything is overwritten, the
    install being replaced is copied aside, with a manifest recording what it
    was and when. -Rollback puts it back.

    WHERE THE COPY LIVES, and the honest limit of it. It goes in

        %LOCALAPPDATA%\Heron\install-backup\<version>\

    which is Heron's own folder rather than Autodesk's, so nothing here ever
    writes a stray file into the folder Revit scans for manifests. It is
    machine-local and NOT roamed: losing the profile or the disk loses it,
    and the way back from that is to build and deploy again from source.
    Calling it a backup of the product would overstate it - the product's
    real home is git. It is the previous install, kept so that an update
    which turns out badly has somewhere to go at the moment it is noticed.

    ONE deep only, deliberately. Two would need a policy for which to restore
    and a way to say so, and an update that has gone wrong twice running is
    not a case for a longer history - it is a case for rebuilding from source.
#>
[CmdletBinding()]
param(
    [string] $RevitVersion = "2024",
    [string] $Configuration = "Debug",
    [switch] $Remove,
    [switch] $Rollback
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot

. (Join-Path $PSScriptRoot "HeronRevit.ps1")

$target   = Join-Path $env:APPDATA "Autodesk\Revit\Addins\$RevitVersion"
$addinDir = Join-Path $target "Heron"
$manifest = Join-Path $target "Heron.addin"

# Heron's own folder, never Autodesk's - see the -Rollback note in the header.
$backupDir      = Join-Path $env:LOCALAPPDATA "Heron\install-backup\$RevitVersion"
$backupAddinDir = Join-Path $backupDir "Heron"
$backupManifest = Join-Path $backupDir "Heron.addin"
$backupRecord   = Join-Path $backupDir "replaced.json"

# Revit holds a lock on loaded assemblies; deploying under it silently fails.
# Checked fresh here rather than trusting a caller: a build takes long enough
# that Revit can be opened in between, and this is the last gate before files
# are replaced.
#
# This now guards REMOVE AND ROLLBACK TOO, and did not before 2026-09-19.
# Remove-Item on an assembly Revit has loaded fails with a file-in-use error
# halfway through the folder, which leaves a partial install behind and reads
# as "the uninstall went wrong" rather than "close Revit first". Every path
# below replaces or deletes the same locked files, so they all want the same
# gate and the same sentence.
$blocked = Get-RevitBlockReason -RevitVersion $RevitVersion -Running (Get-RunningRevit)
if ($blocked) {
    $verb = if ($Remove) { "uninstall" } elseif ($Rollback) { "roll back" } else { "install" }
    throw "Cannot $verb for Revit ${RevitVersion}: $blocked. Close it and run this again - a loaded assembly cannot be replaced, so doing this now would half-finish and look like it worked."
}

function Save-PreviousInstall {
    <#
    .SYNOPSIS
        Copy the install about to be replaced, so -Rollback has somewhere to
        go. Silent and cheap when there is nothing installed yet.
    #>
    if (-not (Test-Path $addinDir)) { return }

    if (Test-Path $backupDir) { Remove-Item $backupDir -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $backupDir | Out-Null

    Copy-Item $addinDir -Destination $backupAddinDir -Recurse -Force
    if (Test-Path $manifest) { Copy-Item $manifest -Destination $backupManifest -Force }

    # What it was, so a rollback can say what it is putting back rather than
    # just doing it. A restore nobody can read back is the same evidence as
    # no restore - the shape brain/heron_update.py already refuses.
    $replacedDll = Join-Path $backupAddinDir "Heron.Revit.Addin.dll"
    $record = [ordered]@{
        revitVersion = $RevitVersion
        replacedAt   = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
        assembly     = if (Test-Path $replacedDll) {
                           (Get-Item $replacedDll).VersionInfo.FileVersion
                       } else { $null }
        sha256       = if (Test-Path $replacedDll) {
                           (Get-FileHash $replacedDll -Algorithm SHA256).Hash
                       } else { $null }
        fileCount    = @(Get-ChildItem $backupAddinDir -Recurse -File).Count
    }
    $record | ConvertTo-Json | Set-Content -Path $backupRecord -Encoding UTF8

    Write-Host "  kept the install being replaced in $backupDir"
}

if ($Rollback) {
    if (-not (Test-Path $backupAddinDir)) {
        throw "Nothing to roll back to for Revit $RevitVersion. $backupDir holds no previous install - this script keeps one only from the moment it has replaced something, and it is machine-local, so a new profile or a cleared cache starts empty. Build and deploy from source instead:`n  dotnet build revit\Heron.Revit.Addin\Heron.Revit.Addin.csproj -c $Configuration -p:RevitVersion=$RevitVersion`n  .\tools\deploy-addin.ps1 -RevitVersion $RevitVersion"
    }

    if (Test-Path $backupRecord) {
        $was = Get-Content $backupRecord -Raw | ConvertFrom-Json
        Write-Host "Rolling back Revit $RevitVersion to the install replaced at $($was.replacedAt)"
        Write-Host "  assembly $($was.assembly), $($was.fileCount) file(s)"
    }

    if (Test-Path $addinDir) { Remove-Item $addinDir -Recurse -Force }
    Copy-Item $backupAddinDir -Destination $addinDir -Recurse -Force
    if (Test-Path $backupManifest) { Copy-Item $backupManifest -Destination $manifest -Force }

    # Verify what was WRITTEN, for the same reason the deploy path does.
    $rolledDll = Join-Path $addinDir "Heron.Revit.Addin.dll"
    if (-not (Test-Path $rolledDll)) {
        throw "Rollback finished but $rolledDll is not there. Do not start Revit against this folder."
    }
    if (-not (Test-Path $manifest)) {
        throw "Rollback finished but $manifest is not there, so Revit would not find Heron at all."
    }

    Write-Host ""
    Write-Host "Rolled back to $addinDir"
    Write-Host "Restart Revit $RevitVersion - the version in memory is still the one you just left."
    return
}

if ($Remove) {
    # Backed up first, so an uninstall is recoverable too. An uninstall is
    # the one action a user takes when something is already going wrong, and
    # it is the worst moment to discover there is no way back.
    Save-PreviousInstall

    if (Test-Path $manifest) { Remove-Item $manifest -Force;               Write-Host "Removed $manifest" }
    if (Test-Path $addinDir) { Remove-Item $addinDir -Recurse -Force;      Write-Host "Removed $addinDir" }
    Write-Host ""
    Write-Host "Heron uninstalled for Revit $RevitVersion. Restart Revit to unload it."
    Write-Host "Put it back with:  .\tools\deploy-addin.ps1 -RevitVersion $RevitVersion -Rollback"
    return
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
# WHAT THIS USED TO DO, AND THE CASE IT COULD NOT SEE. Until 2026-09-20 this
# guard read the presence of a deps.json (".NET or .NET Framework?") and then
# that file's runtimeTarget (".NET 8 or .NET 10?"). Both are PROXIES for the
# runtime rather than the runtime itself, and between them they could not tell
# net472 from net48, because neither emits a deps.json at all. So a Revit 2024
# build deployed into the 2020 folder passed every check and reported success.
#
# That was written down as a known gap on 2026-09-19 (NEEDS-CHECKING A12) and
# it happened for real the next day: .NETFramework,Version=v4.8 sitting in
# Addins\2020, found by reading the deployed assembly rather than by anything
# here.
#
# SO THIS ASKS THE ASSEMBLY WHAT IT WAS BUILT FOR. Every Heron build stamps a
# TargetFrameworkAttribute - GenerateAssemblyInfo is on in
# Directory.Build.props - and that string IS the fact the two proxies were
# standing in for. One check now, covering all eight releases and the two
# pairs neither proxy could separate.
#
# READ AS BYTES, NEVER LOADED. Reflection would lock the file this script is
# about to replace, and Windows PowerShell runs on .NET Framework, which
# cannot load a .NET 10 assembly at all - so the release most worth checking
# is the one reflection could not check. The attribute is stored as plain
# UTF-8 in the metadata, so finding it costs a read and locks nothing.

function Get-AssemblyTargetFramework {
    <#
        Every distinct TargetFrameworkAttribute value in an assembly's bytes.

        Returns an array so the caller can tell "none" from "one" from "more
        than one" and refuse on anything but exactly one. Guessing which of
        two is the real one is how a guard becomes a coin toss.
    #>
    param([string] $Path)

    $bytes = [System.IO.File]::ReadAllBytes($Path)
    $text  = [System.Text.Encoding]::UTF8.GetString($bytes)

    return @([regex]::Matches($text, '\.NET(?:Framework|CoreApp),Version=v\d+(?:\.\d+)+') |
             ForEach-Object { $_.Value } |
             Sort-Object -Unique)
}

function Get-ExpectedTargetFramework {
    <#
        The release-to-runtime table, and it is Directory.Build.props's table.

        A release not listed there is an ERROR there and an error here, never
        a guess - Autodesk has moved the runtime twice already, at 2025 and at
        2027. The old guard treated anything past 2027 as .NET 10 and would
        have happily deployed a 2027 build for a release nobody has seen.
    #>
    param([int] $Release)

    if ($Release -eq 2020) { return ".NETFramework,Version=v4.7.2" }
    if ($Release -ge 2021 -and $Release -le 2024) { return ".NETFramework,Version=v4.8" }
    if ($Release -ge 2025 -and $Release -le 2026) { return ".NETCoreApp,Version=v8.0" }
    if ($Release -eq 2027) { return ".NETCoreApp,Version=v10.0" }
    return $null
}

function Get-RuntimeName {
    <# The same fact in words a person reads, for the message. #>
    param([string] $Tfm)

    if ($Tfm -match '^\.NETFramework,Version=v(.+)$') { return ".NET Framework $($Matches[1])" }
    if ($Tfm -match '^\.NETCoreApp,Version=v(.+)$')   { return ".NET $($Matches[1] -replace '\.0$', '')" }
    return $Tfm
}

$rebuildLine = "dotnet build revit\Heron.Revit.Addin\Heron.Revit.Addin.csproj -c $Configuration -p:RevitVersion=$RevitVersion"

$wantedTfm = Get-ExpectedTargetFramework -Release ([int]$RevitVersion)
if (-not $wantedTfm) {
    throw "Heron does not know which .NET runtime Revit $RevitVersion uses, so it will not guess which build to deploy. Supported today: 2020 to 2027. Confirm the runtime for that release against the Autodesk SDK and add it to Directory.Build.props and to Get-ExpectedTargetFramework in this script."
}

$mainAssembly = Join-Path $buildOut "Heron.Revit.Addin.dll"
if (-not (Test-Path $mainAssembly)) {
    throw "No Heron.Revit.Addin.dll in $buildOut, so there is nothing to check and nothing to deploy. Build for this release first:`n  $rebuildLine"
}

# @() AROUND THE CALL, and it is load-bearing. PowerShell UNROLLS a
# one-element array on its way out of a function, so the array built
# inside comes back as a bare string - whose .Count is also 1, so the
# check below still passes, and whose [0] is then the first CHARACTER.
# The first run of this guard refused correctly and said the build was
# made for ".", which is how that surfaced.
$foundTfm = @(Get-AssemblyTargetFramework -Path $mainAssembly)

if ($foundTfm.Count -eq 0) {
    throw "Could not tell which runtime the build in $buildOut was made for - it carries no target framework. Rather than deploy something Revit may refuse to load without saying why, build for this release and run this again:`n  $rebuildLine"
}

if ($foundTfm.Count -gt 1) {
    throw "The build in $buildOut names more than one runtime ($($foundTfm -join ', ')), so this cannot say which it really is. Delete $buildOut, then build for this release alone:`n  $rebuildLine"
}

if ($foundTfm[0] -ne $wantedTfm) {
    throw "The build in $buildOut was made for $(Get-RuntimeName $foundTfm[0]), but Revit $RevitVersion needs $(Get-RuntimeName $wantedTfm). Revit would refuse to load it and would not say why. The build folder is shared between every release, so whichever was built last is what is sitting there - build for this one and run this again:`n  $rebuildLine"
}

Write-Host "  built for $(Get-RuntimeName $wantedTfm), which is what Revit $RevitVersion needs"

# DERIVED FROM THE RELEASE, and no longer from whether a deps.json happens
# to be lying in the build folder. The copy step below and the check after
# it both need to know whether this release carries runtime metadata, and
# reading that off the file they are about to copy was circular - the
# comment further down still records the day that bit.
$isDotNet = $wantedTfm.StartsWith(".NETCoreApp")

# The last moment at which the install being replaced still exists. Every
# check above has passed by now, so this copies only when a deploy is really
# about to happen - a run that threw on the wrong build flavour leaves the
# previous rollback point intact rather than spending it on a no-op.
Save-PreviousInstall

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
# This was missed until 2026-09-12: the check above USED TO read deps.json to
# decide whether the build was the right flavour, and then the copy took
# *.dll only, so the file the check had just relied on was left behind. A
# guard that passes while the thing it guards is broken is worse than no
# guard - it was the reason nobody looked here.
#
# That circularity is gone since 2026-09-20: the flavour comes from the
# assembly's own target framework, and deps.json is now only ever a file to
# copy. The paragraph stays because the lesson did not go with it.
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
Write-Host "  2. Ribbon > Heron > AI Bridge > Heron   (click to connect, click again to disconnect)"
Write-Host "  3. python mcp\client\heron_bridge_client.py ping"
