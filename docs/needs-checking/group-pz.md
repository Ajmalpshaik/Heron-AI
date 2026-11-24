# Needs checking — Group PZ

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group PZ - CONNECT_AIR_TERMINALS version 3: the duct chosen from the ones that fit, and joined read off the partners - DRAFT until proved (2026-11-23)

**Tracked as GitHub issue [#1312](https://github.com/Ajmalpshaik/Heron-AI/issues/1312) since 2026-11-23** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**LOCAL work - it needs Revit on the owner's PC.** A cloud session cannot run any of it.

**Why:** work-note item WN-79 of issue [#1005](https://github.com/Ajmalpshaik/Heron-AI/issues/1005) - C05 of
[the fragment review plan](../work-notes/plans/FRAGMENT-REVIEW-PLAN-CHATGPT-2026-09-07.md). Version 2 of
[`CONNECT_AIR_TERMINALS`](../../brain/fragments/connect-air-terminals/fragment.yaml) took a terminal's first End
connector whatever its domain, let the `IsConnected` flag decide both the skip and the result, and chose the
nearest duct before asking whether it could take the terminal. **Version 3** taps Air Terminals only (by
category), uses the terminal's duct connector, reads *joined* off the physical partners in `AllRefs` (the rule
`REPORT_CONNECTORS` uses - not the terminal itself, not an `MEPSystem`), filters the ducts first - a straight
duct, not on the opposite air system - then takes the nearest within `maxDistanceMm`, and after the tap counts a
terminal only when it is joined to THAT duct, directly or through the one fitting between them. A sprinkler or
fixture head's skip and join read the partners too. Its C# changed, so it is **DRAFT** until this group runs and
Ajmal PS signs (D-30). The version 2 proof stays on the card as the record.

**Not yet run anywhere.** Compiled for Revit 2020 to 2027 (`tools/check-fragments-compile.py`, in a cloud
container, 2026-11-23). **Not known, and what PZ1 settles first:** what Revit puts in the terminal connector's
`AllRefs` after `ConnectAirTerminalOnDuct` - a takeoff fitting whose other side reaches the duct, or the duct
itself. If it is neither, version 3 refuses every good tap as *"does not reach duct N"*: write down what the
terminal is joined to, and that is a fix to the card, not a pass.

**The model: a scratch copy, never the working model** - a sample project with ductwork saved as
`%TEMP%\heron-work\pz\pz-project.rvt`. Revit 2020 first, then 2024. Every run is Undone after.

| ID | Do this | Pass looks like |
|---|---|---|
| **PZ1** | *Positive - the ordinary tap.* Two supply diffusers under one supply duct, `ducts=Ducts`, `maxDistanceMm` 1500, Keep. `REPORT_CONNECTORS` on the diffusers after | `connected 2`, each `tapped into <the duct>`; `REPORT_CONNECTORS` shows each diffuser's duct connector joined to a takeoff (or the duct) and none in `flagDisagrees`. Write down what each diffuser is joined to. Undo |
| **PZ2** | *Negative - a nearer duct of the wrong system.* One supply diffuser with a RETURN duct about 300 mm from its connector and a SUPPLY duct about 900 mm from it, `maxDistanceMm` 1500, Keep | Tapped into the SUPPLY duct; its finding says the return duct was passed over, *"it is return air and the terminal is supply air"*. Version 2 tapped the return duct. Undo |
| **PZ3** | *Negative - only the wrong system in reach.* The same diffuser with the supply duct out of reach (`maxDistanceMm` 500) | No tap made: the diffuser in `noDuctNear`, the finding naming the return duct as passed over; nothing in `moved`, and nothing in the model changed |
| **PZ4** | *Negative - not an air terminal.* A fan coil unit or AHU (Mechanical Equipment with a duct spigot) handed in with one diffuser | The equipment in `refused` as *"not an air terminal"*, untouched; the diffuser tapped. Undo |
| **PZ5** | *Regression - a head still branches.* One pendent sprinkler 600 mm beside a straight wet main 500 mm above it, `ducts=Pipes`, `maxDistanceMm` 1500, Keep | `branched 1`, riser and arm drawn and joined at the head - the head's join now read off its partners. Run again: the head in `alreadyConnected`. Undo |

**Signing, once PZ1 to PZ5 pass:** the card's proof is written by `tools/batch-prove.py` with the owner, and
`heron-status` set to PROVEN in the same change - never by a session, and never on a working model.
