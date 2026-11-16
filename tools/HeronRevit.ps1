# Heron-Agent:  HERON-INS-ENV-002
# Heron-Step:   1
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

<#
.SYNOPSIS
    Environment detection - which Revit is installed, which is open, and
    where its add-ins go.

.DESCRIPTION
    Dot-source this rather than repeating the checks. Two scripts asking the
    same question in two slightly different ways is how they end up
    disagreeing about whether it is safe to install.

    The rule this exists to enforce: Revit holds a lock on every assembly it
    has loaded, so an add-in cannot be replaced underneath a running Revit.
    Copying anyway half-updates and looks like it worked, which is the worst
    of the possible outcomes - the user gets a mix of old and new code and no
    error to explain it (docs/07).

    The check is PER VERSION. Revit 2024 being open says nothing about
    whether it is safe to install for 2020, and refusing everything because
    one Revit is open is a needless obstacle on a machine with three
    installed.

    NOTHING HERE EVER CLOSES REVIT, and nothing that uses it may either.

    An open Revit almost certainly has a model in it, and that model almost
    certainly has unsaved work. Closing it to make an install proceed would
    destroy hours of somebody's modelling to save them one click. There is no
    flag for it, no -Force, and no "it looked idle" - the only correct
    behaviour is to detect it, name the release, say what to close, and stop.

    Detect, report, refuse. The decision to close a model belongs to the
    person who has it open, always.
#>

function Get-InstalledRevit {
    <#
    .SYNOPSIS
        Every Revit release installed on this machine, oldest first.
    .DESCRIPTION
        Discovered from disk, never from a fixed list of years - a hardcoded
        upper bound makes the next Revit release invisible to Heron's own
        installer, and D-05 promises 2020 to latest, permanently.
    #>
    $autodesk = Join-Path ${env:ProgramFiles} "Autodesk"
    if (-not (Test-Path $autodesk)) { return @() }

    return @(Get-ChildItem -Path $autodesk -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -match '^Revit (\d{4})$' -and (Test-Path (Join-Path $_.FullName "Revit.exe")) } |
        ForEach-Object { $_.Name.Substring(6) } |
        Sort-Object)
}

function Get-HeronFolder {
    <#
    .SYNOPSIS
        One of the folders the install scripts need, read from the list the
        add-in's own path code wrote: platform\heron-folders.json.
    .DESCRIPTION
        NOTHING HERE BUILDS A HERON PATH, and that is Q-58, decided on
        2026-11-15. platform/README.md rule 3 says HeronPaths is the only thing
        that may, and a .ps1 cannot call a C# class - it runs before any Heron
        assembly is on the PC. So HeronPaths writes the folders down
        (tests/Heron.Folders.TestHost, through tools/heron-folders.py --write)
        and this reads them. tests/test_heron_folders.py fails when the list
        and the code disagree, and when a script spells a folder itself.

        Each entry names a Windows special folder and the parts under it. The
        special folder is resolved with [Environment]::GetFolderPath, the call
        HeronPaths makes, so the script and the add-in name one folder even
        where an environment variable says otherwise.

        STOPS, BY NAME, when the list is missing or does not have the folder
        asked for. Guessing a path instead is the second owner Q-58 removes.
        Nothing has been changed by then: every caller asks before it copies.
    .PARAMETER Name
        The folder's name in the list - RevitAddins, InstallBackup.
    .PARAMETER Fill
        The blanks the entry has, by name - @{ release = "2024" }. A value that
        could step out of its folder is refused.
    #>
    param(
        [Parameter(Mandatory = $true)][string] $Name,
        [hashtable] $Fill = @{}
    )

    # Beside the tools folder this file sits in - a checkout and an unzipped
    # heron-project.zip have the same shape (tools/build-release-assets.py).
    $listPath = Join-Path (Split-Path -Parent $PSScriptRoot) "platform\heron-folders.json"
    if (-not (Test-Path $listPath)) {
        throw "Heron's folder list is not at $listPath, so this cannot tell where '$Name' is, and nothing was changed. The Heron files are incomplete - fetch them again."
    }

    $list = Get-Content $listPath -Raw | ConvertFrom-Json
    $entry = $null
    if ($list.folders) { $entry = $list.folders.PSObject.Properties[$Name] }
    if (-not $entry) {
        throw "Heron's folder list at $listPath has no folder called '$Name', so nothing was changed. The Heron files do not match each other - fetch them again."
    }
    $entry = $entry.Value

    try {
        $special = [Environment+SpecialFolder]([string]$entry.under)
    } catch {
        throw "Heron's folder list names '$($entry.under)' for '$Name', which is not a Windows folder this PowerShell knows, so nothing was changed. Fetch the Heron files again."
    }
    $path = [Environment]::GetFolderPath($special)
    if (-not $path) {
        throw "Windows has no $($entry.under) folder for this account, so '$Name' has nowhere to be and nothing was changed. Run this as the person who uses Revit on this PC."
    }

    foreach ($part in @($entry.parts)) {
        $filled = [string]$part
        foreach ($key in $Fill.Keys) {
            $value = [string]$Fill[$key]
            if ($value -match '[\\/]' -or $value -eq '..' -or $value -eq '.' -or $value -eq '') {
                throw "'$value' is not a $key this can put in a folder name, so nothing was changed."
            }
            $filled = $filled.Replace("{$key}", $value)
        }
        if ($filled -match '[{}]') {
            throw "Heron's folder list wants '$filled' filled in for '$Name' and nothing was given for it, so nothing was changed."
        }
        $path = Join-Path $path $filled
    }
    return $path
}

function Get-RevitAddinsFolder {
    <#
    .SYNOPSIS
        Where Revit scans for add-in manifests, for one release.
    .DESCRIPTION
        PER USER, never all-user. docs/07 section 5: the target is a BIM
        modeller on a locked-down corporate machine, so Heron promises an
        install that needs no administrator. ProgramData and Program Files
        are the paths that break that promise, and Revit 2027 moved the
        all-user location from one to the other - which is why naming them is
        not enough on its own and neither ever appears here.

        ONE OWNER, AND SINCE 2026-11-16 IT IS HeronPaths.RevitAddins. This
        function spelled the path until then - deploy-addin.ps1 had built it
        itself and the installer was about to build a third copy in C# on
        2026-09-21, which tools/check-structure.py refused. Both still ask
        here; this asks the list HeronPaths wrote (Get-HeronFolder, Q-58). The
        folder belongs to Autodesk rather than to Heron - other vendors'
        add-ins sit in it - so nothing may ever treat it as Heron's to clear.

        NOT CREATED. A release nobody has installed has no Addins folder, and
        making one would leave a folder behind and answer no question.
    .PARAMETER RevitVersion
        The four-digit release, as a string.
    #>
    param([Parameter(Mandatory = $true)][string] $RevitVersion)

    return (Get-HeronFolder -Name RevitAddins -Fill @{ release = $RevitVersion })
}

function Get-RunningRevit {
    <#
    .SYNOPSIS
        Every Revit currently open, with the release each one is.
    .DESCRIPTION
        Version comes from the executable path, which is the only thing that
        reliably distinguishes one running Revit from another. Where it
        cannot be read - a process this account cannot query - Version is
        $null, and callers must treat that as "unproven", never as "safe".

        Matches the process named exactly Revit, so the worksharing monitor
        and the accelerator are correctly ignored: they hold no lock on the
        add-in.
    #>
    $found = @()
    foreach ($proc in @(Get-Process -Name "Revit" -ErrorAction SilentlyContinue)) {
        $path = $null
        try { $path = $proc.Path } catch { $path = $null }

        $version = $null
        if ($path -and $path -match 'Revit[ _]?(\d{4})') { $version = $matches[1] }

        $found += [PSCustomObject]@{
            Version   = $version
            ProcessId = $proc.Id
            Path      = $path
        }
    }
    return @($found)
}

function Get-RevitBlockReason {
    <#
    .SYNOPSIS
        Why installing for this release is unsafe right now, or $null if it
        is safe.
    .PARAMETER RevitVersion
        The release about to be installed for.
    .PARAMETER Running
        The result of Get-RunningRevit. Pass it in rather than re-reading, so
        one report describes one moment.
    #>
    param(
        [Parameter(Mandatory = $true)] [string] $RevitVersion,
        $Running
    )

    # An empty array handed to a parameter arrives as $null, and @($null) is a
    # one-element array holding nothing. Without this filter, "no Revit open"
    # read as "one Revit open, release unknown" and blocked every install.
    $open = @(@($Running) | Where-Object { $null -ne $_ })

    # An unidentifiable Revit blocks everything. It might be this one, and a
    # guess that lands wrong produces a silently half-updated install.
    foreach ($r in $open) {
        if ($null -eq $r.Version) {
            return "a Revit is open (process $($r.ProcessId)) and its release could not be determined, so no version can be replaced safely"
        }
    }

    foreach ($r in $open) {
        if ($r.Version -eq $RevitVersion) {
            return "Revit $RevitVersion is open (process $($r.ProcessId))"
        }
    }

    return $null
}

function Format-RunningRevit {
    <#
    .SYNOPSIS
        The open releases as one readable phrase, for a message to a person.
    #>
    param($Running)

    $list = @(@($Running) | Where-Object { $null -ne $_ })
    if ($list.Count -eq 0) { return "none" }

    return (($list | ForEach-Object {
        if ($_.Version) { "Revit $($_.Version)" } else { "Revit (release unknown)" }
    } | Sort-Object -Unique) -join ", ")
}

function Get-PythonStatus {
    <#
    .SYNOPSIS
        Is Python present, and does it have the MCP package and PyYAML?
    .DESCRIPTION
        Heron's MCP server is Python (D-06, Q-39), so Python is a prerequisite
        and not an optional extra. It was never written down as one, which is
        how a prerequisite becomes a surprise on somebody else's machine.

        Two packages are asked for, not one. The MCP package is what the
        server is served by; PyYAML is the one package requirements.txt calls
        REQUIRED, and installing mcp does not bring it (row 5b-201). Asking
        for mcp alone passed a PC on which every question to Heron's
        knowledge then answered "needs PyYAML".

        NONE OF THIS NEEDS ADMINISTRATOR RIGHTS, which is the part worth
        knowing on a locked-down company laptop: the Microsoft Store build and
        winget both install Python per-user, into AppData, and pip installs
        into a per-user site-packages folder.

        Returns an object rather than printing, so the caller decides how loud
        to be about it.
    #>
    $result = [PSCustomObject]@{
        Found     = $false
        Command   = $null
        Version   = $null
        HasMcp    = $false
        HasYaml   = $false
        PerUser   = $false
    }

    foreach ($candidate in @("python", "py")) {
        $exe = (Get-Command $candidate -ErrorAction SilentlyContinue)
        if (-not $exe) { continue }
        try {
            $version = & $candidate --version 2>&1 | Select-Object -First 1
        } catch { continue }
        if ($LASTEXITCODE -ne 0 -or -not $version) { continue }

        $result.Found = $true
        $result.Command = $candidate
        $result.Version = "$version".Trim()

        # Where it lives says whether it needed admin to get there.
        try {
            $where = & $candidate -c "import sys; print(sys.executable)" 2>$null
            $result.PerUser = ("$where" -like "*\AppData\*")
        } catch { }

        try {
            & $candidate -c "import mcp" 2>$null | Out-Null
            $result.HasMcp = ($LASTEXITCODE -eq 0)
        } catch { }

        try {
            & $candidate -c "import yaml" 2>$null | Out-Null
            $result.HasYaml = ($LASTEXITCODE -eq 0)
        } catch { }
        break
    }

    return $result
}

function Write-PythonAdvice {
    <#
    .SYNOPSIS
        Says exactly what to install, in commands that need no admin rights.
    #>
    param($Python)

    if (-not $Python.Found) {
        Write-Host "    Python is not installed. Heron's MCP server needs it." -ForegroundColor Yellow
        Write-Host "    Install it for yourself only - no administrator rights required:"
        Write-Host "        winget install Python.Python.3.12 --scope user" -ForegroundColor White
        Write-Host "    or get it from the Microsoft Store, which is also per-user."
        Write-Host "    Then run this again."
        return $false
    }

    # Both are named in one go: a PC missing both would otherwise be sent round
    # twice (row 5b-201). ONE line installs both - requirements-lock.txt's own,
    # every version pinned and hashed, the file HeronInstaller.exe installs
    # from. Unpinned, `pip install mcp` pulled a cryptography that broke on
    # import on 2026-09-16 (row 5b-944). The lock is for Python 3.11 to 3.14,
    # as its header says, so a Python outside that is told so first.
    $missing = @()
    if (-not $Python.HasYaml) { $missing += "PyYAML" }
    if (-not $Python.HasMcp)  { $missing += "the MCP package" }
    if ($missing.Count -gt 0) {
        $verb = if ($missing.Count -gt 1) { "are" } else { "is" }
        Write-Host "    Python is here ($($Python.Version)) but $($missing -join ' and ') $verb not." -ForegroundColor Yellow
        if ("$($Python.Version)" -notmatch '^Python 3\.(1[1-4])(\.|$)') {
            Write-Host "    Heron's pinned packages are for Python 3.11 to 3.14 - install one for yourself first:"
            Write-Host "        winget install Python.Python.3.12 --scope user" -ForegroundColor White
            Write-Host "    then, in a NEW terminal, in the Heron folder:"
        } else {
            Write-Host "    Install into your own profile, in the Heron folder - no administrator rights required:"
        }
        Write-Host "        $($Python.Command) -m pip install --user --require-hashes -r requirements-lock.txt" -ForegroundColor White
        Write-Host "    Then run this again."
        return $false
    }

    return $true
}
