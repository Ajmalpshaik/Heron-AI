# Needs checking — Group ND

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group ND - a family's 2D side read back - lines, regions, nested families, and what is locked, labelled and linked to each: `REPORT_FAMILY_2D_ELEMENTS` (2026-11-11)

Asked 2026-11-11 by the audit of every capability a family's 2D side needs
([work note](../work-notes/investigations/2026-11-11-family-2d-capabilities.md)): after a flow arrow is placed,
locked and stretched in a diffuser family, nothing in Heron read back what was locked to what. Made NEW, READ, at
DRAFT; why, and what is not known, is [FRAGMENT-ISSUES row 5b-1496](../fragment-issues/section-5b-rows-401-425.md).

| What | Where | What it does |
|---|---|---|
| `REPORT_FAMILY_2D_ELEMENTS` (READ) | [`brain/fragments/report-family-2d-elements`](../../brain/fragments/report-family-2d-elements/fragment.yaml) | Every detail line, symbolic line, filled and masking region and nested family in the family open: view or plane, ends, style or type, visibility, Visible and its link, nested parameter links, and every lock, label and EQ on it in words. `which` narrows by kind or by id |

**What is done and what is not.** The card compiles on all eight releases, 2020 to 2027, with the whole library
(`tools/check-fragments-compile.py`, 2026-11-11, the owner's PC). **Nothing here has run in Revit.** It is READ:
every run below changes nothing, so no Keep or Discard is asked.

**The model - a SAVED scratch family, never the owner's own families.** File, New, Family, the metric Detail
Item template, saved as "Heron 2D Read Test" under `%TEMP%\heron-work\2d-read`, Revit 2020. Every call with
`HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are connected, and `< /dev/null`.

**The arrangement (ND0), made with Heron's own PROVEN tools and KEPT:** reference planes "Tip" at X 600 and "Head
Top" at Y 80 (`CREATE_REFERENCE_PLANES`); a detail line "path 0,0; 300,0" in Ref. Level, `lockToPlanes` true
(`DRAW_FAMILY_DETAIL_LINES` locks it along Center (Front/Back)); a filled region "polygon 300,80 600,0 300,-80",
Solid Black (`DRAW_FAMILY_FILLED_REGION`); a second detail line "path 0,-200; 300,-200", lockToPlanes false - the
CONTROL. By hand: Align and lock the region's tip corner to "Tip". Record the three ids. Save.

| # | Run | Look for |
|---|---|---|
| **ND0** | *Arrange, KEEP it,* as above | The title bar names the scratch family; Edit Boundary on the region shows one closed padlock at the tip |
| **ND1** | *The D-30 negative FIRST* - `validate report-family-2d-elements --set "which=<the shaft's id with its FIRST character changed>"` | Refused "is not an id in this family"; scanned 0, nothing read. Then `which=<the shaft's id>, <Tip's UniqueId>` - refused naming Tip, the shaft not read either |
| **ND2** | *The positive* - `which` left out | elementCount 3, scanned 3. The shaft "from 0,0,0 to 300,0,0 mm, 300 long", "locked along Center (Front/Back)"; the CONTROL "nothing holds it"; the region with its type, "1 loop(s), X 300 to 600, Y -80 to 80 mm", "3 sketch edge(s)" and "locked at an end to Tip". **The region's three edges are NOT listed as lines** - if they are, `GetDependentElements` did not return them and the card is changed, not proved |
| **ND3** | *A 3D family's symbol* - a scratch metric Generic Model "Heron 2D Read 3D", a symbolic line "path 0,0; 500,0" on Ref. Level for coarse only (`DRAW_FAMILY_SYMBOLIC_LINES`); `which` "symbolic line" | One row: "on plane Level : Ref. Level" (or Revit's own name for it), "shows in coarse \| plan" |
| **ND4** | *A nested detail item and a link* - in ND3's family, a detail item loaded and placed in the view Ref. Level by hand, its Visible associated with a new Yes/No "Show Arrow" by hand; `which` "nested" | The nested row names its family and type, "in view Ref. Level", its point and "turned 0 deg", and "Visible yes (linked to Show Arrow)" |
| **ND5** | *Second route.* In the Family Editor, select each element and read Properties, the padlocks and Visibility Settings by hand | The same views, ends, locks and links as ND2 to ND4 |

**What this group also measures for others.** ND2 says whether a plain detail line reads as a loose `DetailCurve`
with nothing in a sketch - the question Group NC's first run raised for `LOCK_FAMILY_LINES_TO_PLANES` - and ND4
whether a detail item placed by hand in a family has an owner view, Group NB's question.
