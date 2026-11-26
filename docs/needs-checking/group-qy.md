# Needs checking — Group QY

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in this
> file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back into the
> register for every tool that reads the register.

## Group QY - CHECK_MINIMUM_CLEARANCE version 5 (vertical clearance merged in), CHECK_SLEEVE_SIZE version 3 and PROPOSE_MEP_OPENINGS version 3 - DRAFT until proved (2026-11-26)

**Tracked as GitHub issue [#1453](https://github.com/Ajmalpshaik/Heron-AI/issues/1453) since 2026-11-26** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** the owner's answers on issue [#1088](https://github.com/Ajmalpshaik/Heron-AI/issues/1088), 2026-11-26,
to [row 5b-1878](../fragment-issues/row-5b-1878.md) (A) and (B) and [row 5b-1879](../fragment-issues/row-5b-1879.md)
items 1 and 2. Three PROVEN cards changed their C# and went back to DRAFT, and one was retired:

- `CHECK_MINIMUM_CLEARANCE` version 5 - `CHECK_VERTICAL_CLEARANCE` merged in (*"Yes, one tool"*), now DEPRECATED.
  `stackedOnly` measures only pairs one above the other in plan, the vertical gap, each row naming the upper one;
  `slopedOrVertical` counts runs whose box is the whole rise. `overlapAs` ([D-208](../decisions/D-208.md)) says how
  an overlap reads: `clash` (the default) - CLASH with its depth in `overlapsMm`, too close whatever clearance was
  asked - or `zero`, version 4's 0 mm. `clashing` counts the overlapping pairs either way.
- `CHECK_SLEEVE_SIZE` version 3 - every service through a sleeve is judged, not the first; a sleeve with two or more
  is in `combined`, and oversize is not judged for it.
- `PROPOSE_MEP_OPENINGS` version 3 - each pipe and duct is asked whether it carries insulation, and twice its
  thickest wrap is added to the hole only when it does (D-208).

Each card's `tests/cases.yaml` holds every case, each marked *Group QY*; these rows are the run.

**What is done and what is not.** Compiles for 2020 to 2027 (`tools/check-fragments-compile.py`, 2026-11-26), on
Linux with no Revit. **Never run in Revit.** Not known until a run: whether a crossing at 45 degrees overlaps in
plan over its box corner the way the vertical card assumed; how deep a box overlap reads for two pipes that only
cross (the smallest of three axis overlaps); whether `GetInsulationIds` on a duct returns its insulation and never
its lining. Row [5b-2055](../fragment-issues/row-5b-2055.md) (nominal against outside diameter) is a separate
question, not settled here.

**The model: scratch, never a working one** - Revit 2024 first (version 4's proof ran there), then 2020.
`qy-scratch`: new from the mechanical template. **Stack** - a 600 x 300 duct `D-1` at 3000 mm to its bottom, a 100 mm
pipe `P-1` directly under it 90 mm below the duct's bottom, a second 100 mm pipe `P-2` beside `D-1` - 60 mm clear
of its side in plan, at the same height as `P-1` - and a duct `D-2` that interpenetrates `D-1` by 30 mm in height over a short length.
A vertical pipe riser `R-1` beside them. **Sleeves** - a 200 mm sleeve family instance `S-1` in a wall with two pipes
through it, 100 mm and 160 mm; `S-2`, a 200 mm sleeve with one 150 mm pipe. **Openings** - a 150 mm pipe with
25 mm pipe insulation and a 400 x 250 duct with 50 mm duct insulation, each crossing a wall, and a bare 400 x 250 duct
with a lining. Write each element's id here before the run.

| ID | Do this | Pass looks like |
|---|---|---|
| **QY1** | **Negative, first.** `CHECK_MINIMUM_CLEARANCE` on `P-2` and a duct far off in plan but at the same height, `stackedOnly` true, `defaultClearance` 5000 | `violations` 0, `clashing` 0, *"1 pair(s) passed over: not one above the other in plan"*. With `stackedOnly` false the same pair is reported |
| **QY2** | `CHECK_MINIMUM_CLEARANCE` on `D-1`, `P-1`, `P-2`, `stackedOnly` true, `defaultClearance` 100 | `D-1` *"is ABOVE"* `P-1`, about 90 mm, *"10 mm short"* - check against a section; the `D-1` - `P-2` pair passed over and counted. With `stackedOnly` false, `D-1` - `P-2` is reported too, at about 60 mm, and `D-1` - `P-1` reads the same 90 mm |
| **QY3** | `CHECK_MINIMUM_CLEARANCE` on `D-1` and `D-2`, `defaultClearance` 0, `overlapAs` left out | One row *"CLASH ... overlap by"* about 30 mm (with `stackedOnly` true, *"in height"*); `clashing` 1; `overlapsMm` holds the depth; `gapsMm` 0, never negative. Version 4 reported nothing |
| **QY4** | The same with `defaultClearance` 100 and `overlapAs` zero | The version 4 row: *"REAL GAP 0 mm against 100 required"*; `overlapsMm` empty; `clashing` 1 |
| **QY5** | `overlapAs` deep | *"NOTHING WAS MEASURED"*, naming the word; every result empty |
| **QY6** | `stackedOnly` true with `R-1` added | `slopedOrVertical` 1 and its warning |
| **QY7** | **Negative.** The keeper on sheets only (as version 4's proof), `stackedOnly` true | `tooClose` 0, `clashing` 0, `overlapsMm` empty; the sheets said as having no model geometry |
| **QY8** | `CHECK_SLEEVE_SIZE` on `S-1`, `annularClearance` 25 | `S-1` in `undersized` and `combined`; a COMBINED line naming both pipes; the 100 mm row ok, the 160 mm row *"UNDERSIZED by 10 mm"* |
| **QY9** | **Negative.** `CHECK_SLEEVE_SIZE` on `S-2`, `annularClearance` 25 | Exactly version 2's row; `combined` empty; *"0 with more than one service through them"* |
| **QY10** | `PROPOSE_MEP_OPENINGS` on the insulated pipe and the insulated duct, `allowanceMm` 25 and then 50 | Pipe at 25: 250 x 250, *"+ 25 mm insulation"*. Duct at 50: 600 x 450, *"+ 50 mm insulation"*. Findings count 2 services with insulation |
| **QY11** | **Negative.** `PROPOSE_MEP_OPENINGS` on the bare lined duct, `allowanceMm` 50 | 500 x 350, *"(no insulation)"*, 0 services with insulation - the lining not counted |
| **QY12** | Run `CHECK_SLEEVE_SIZE` on a sleeve sized as QY10 proposed for the insulated pipe, `annularClearance` 25 | The jacket no longer makes it UNDERSIZED. Any gap that remains is nominal against outside diameter - write the figure into row 5b-2055 |
