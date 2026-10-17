# Needs checking — Group FY

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-16 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group FY - every column of a schedule in one call, and a heading turned back to horizontal: `SET_SCHEDULE_APPEARANCE` version 3 (2026-10-17)

Found on 2026-10-15 on a real project, Revit 2020 (never written to for proving). Ajmal PS asked *"make
all the fields formatting horizontal and centre"* on a schedule: it took one call per field - one undo
entry per column - and no call could turn a heading back to horizontal, because `headingSideways` false
meant "leave it" and only Vertical was ever written.
[Row 5b-620](../fragment-issues/section-5b-rows-401-425.md) has the finding.

**What changed.** [`SET_SCHEDULE_APPEARANCE`](../../brain/fragments/set-schedule-appearance/fragment.yaml)
(FRG-VIEW-061, was PROVEN, now DRAFT, version 3). `fieldName` "all" (also "every", "*", "all fields",
"all columns") is every column of each schedule, hidden ones included; names with commas between - "Mark,
Size" - are each one named, by field name or printed heading. One call, so one transaction and one undo
entry, and one read-back line per column. A text matching one column exactly is that column, as in
version 2. A new heading on more than one column is refused and the rest still applied. The new
`headingOrientation` - "horizontal" or "vertical" - is `optional: true` with `absent: leave`, so a caller
that never sends it gets exactly version 2; `headingSideways` true with "horizontal" is refused before
anything is written.

**What is compiled, tested and not run.** Every fragment compiles on all eight releases 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-10-17). The fragment's own code was run in a cloud container
against stand-in types with the same member names - not Revit: on 2,304 version 2 style inputs version 3
gave the same results and left the same schedule state as version 2, and fourteen version 3 cases passed.
That proves the logic, not Revit's answer. **Nothing has run in Revit.** The add-in that binds
`absent: leave` is main's from #545 on; an add-in deployed before #545 refuses the call for the missing
`headingOrientation`.

**A NEW SCRATCH PROJECT WITH A SCHEDULE, never a working model and never any 4355-* model.** Revit 2020
first: a project from the default metric template, saved as *Heron Schedule Appearance.rvt*; draw four
walls; make a Wall Schedule with the fields Type, Length, Area, Mark and Comments; in its Properties >
Formatting set Mark's heading orientation to Vertical and Length's alignment to Left; place it on a new
sheet. Note each field's alignment and heading orientation from the Formatting tab before FY1.

| # | Run | Look for |
|---|---|---|
| **FY1** | The schedule clicked on the sheet. `revit_change` capability `SET_SCHEDULE_APPEARANCE`: fieldName=all, alignment=centre, headingOrientation=horizontal, newHeading="", columnWidthMm=0, headingSideways=false, hideOrShow="" | `changed` 5 and five `appliedAppearance` lines, each `Center, heading Horizontal`; on the sheet every column centred and Mark's heading horizontal again; **Edit > Undo lists ONE entry**, and one Ctrl+Z puts all five back as noted. **The D-30 positive** |
| **FY2** | FY1's call with the sheet's TITLE BLOCK selected instead of the schedule | `changed` 0, `refused` *"not one of the 1 element(s) handed in is a schedule"*, nothing on the sheet moved. **The D-30 negative** |
| **FY3** | The schedule again, fieldName=all and every other value blank (alignment="", headingOrientation left out, headingSideways=false) | `changed` 0, `appliedAppearance` empty - nothing changed and nothing claimed. Version 2's first negative, on the new path |
| **FY4** | fieldName="Mark, Length, Nope", alignment=right, the rest blank | Mark and Length right-aligned and read back, Type, Area and Comments untouched, `notPresent` names *'Nope'* |
| **FY5** | fieldName=Mark, headingOrientation=vertical; then the same with horizontal | Mark's heading sideways, then horizontal - the change version 2 could not make. Each read back |
| **FY6** | A version 2 caller: `python tools/batch-prove.py --only set-schedule-appearance tools/jobs/reprove-refusals.yaml` - it does not send `headingOrientation` | Runs, never refused for a missing value; the binding note reads `headingOrientation not given - "leave", the card's own default (optional)`; Mark's heading text written and its orientation untouched, as version 2's 2026-09-09 proof |
| **FY7** | fieldName=all, newHeading=X, alignment=centre; then fieldName=Mark, headingSideways=true, headingOrientation=horizontal | First: no heading changed, `refused` says *heading NOT written* over 5 columns, every column centred. Second: `refused` says the two ask for opposite things and NOTHING changed |
| **FY8** | FY1 and FY2 once on Revit 2024 and once on 2027 | The same answers |
| **FY9** | Second route: Revit's schedule Properties > Formatting, each field in turn | Alignment and heading orientation as `appliedAppearance` said, field for field |
| **FY10** | *Routing, on the owner's PC.* `heron_lookup` *"make all the schedule columns horizontal and centred"* and *"set every heading horizontal"* | `SET_SCHEDULE_APPEARANCE` first for both. Measured in the cloud on both embedding backends without the re-ranker (row 5b-620); the re-ranked backends were not measured there |
| **FY11** | *Not the proof - an observation.* fieldName=all, hideOrShow=hide, then show | What Revit does with every column hidden - write it down. If Revit refuses, the refusal is named per column; if it allows it, `show` must bring all five back |

Signing: Ajmal PS signs the FY1/FY2 draft; then he says whether the card goes back to PROVEN.
