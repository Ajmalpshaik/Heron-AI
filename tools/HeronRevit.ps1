# Heron-Agent:  HERON-INS-ENV-002
# Heron-Step:   1
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

<#
.SYNOPSIS
    Environment detection - which Revit is installed, and which is open.

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
