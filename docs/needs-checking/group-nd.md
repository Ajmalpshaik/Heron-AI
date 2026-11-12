# Needs checking — Group ND

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group ND - a family's 2D side read back - lines, regions, nested families, and what is locked, labelled and linked to each: `REPORT_FAMILY_FORMS` version 5 (2026-11-11)

Asked 2026-11-11 by the audit of every capability a family's 2D side needs
([work note](../work-notes/investigations/2026-11-11-family-2d-capabilities.md)): after a flow arrow is placed,
locked and stretched in a diffuser family, nothing in Heron read back what was locked to what. **Widened, not
made new**, on the owner's word - no second tool for the same kind of job: `REPORT_FAMILY_FORMS` (READ) goes from
version 4 to 5 and back to DRAFT. Why, and what is not known, is
[FRAGMENT-ISSUES row 5b-1496](../fragment-issues/section-5b-rows-401-425.md).

| What | Where | What it does |
|---|---|---|
| `REPORT_FAMILY_FORMS` version 5 (READ) | [`brain/fragments/report-family-forms`](../../brain/fragments/report-family-forms/fragment.yaml) | Version 4's forms, unchanged; and in `twoDList` every detail line, symbolic line, filled and masking region and nested family: view or plane, ends, style or type, visibility, Visible and its link, nested parameter links, and every lock, label and EQ on it in words. `which` takes 2D kinds and ids. "all" hands on forms alone |

**What is done and what is not.** The card compiles on all eight releases, 2020 to 2027, with the whole library
(`tools/check-fragments-compile.py`, 2026-11-11, the owner's PC). **Nothing here has run in Revit.** It is READ:
every run below changes nothing, so no Keep or Discard is asked.

**The model - SAVED scratch families, never the owner's own families.** File, New, Family, the metric Detail
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
| **ND1** | *The D-30 negative FIRST* - `validate report-family-forms --set "which=<the shaft's id with its FIRST character changed>"` | Refused "is not an id in this family"; nothing read, elements 0. Then `which=<the shaft's id>, <Tip's UniqueId>` - refused naming Tip, the shaft not read either |
| **ND2** | *The positive* - `which` "2d" | twoDCount 3, formCount 0, elements 3. The shaft "from 0,0,0 to 300,0,0 mm, 300 long", "locked along Center (Front/Back)"; the CONTROL "nothing holds it"; the region with its type, "1 loop(s), X 300 to 600, Y -80 to 80 mm", "3 sketch edge(s)" and "locked at an end to Tip". **The region's three edges are NOT listed as lines** - if they are, `GetDependentElements` did not return them and the card is changed, not proved |
| **ND3** | *"all" hands on forms alone* - `which` "all"; then `which` = the id of one EDGE of the region (Select by ID in Edit Boundary) | The same three rows in twoDList - and elements 0. The edge: refused naming the region, nothing handed on |
| **ND4** | *Version 4 unchanged, and "all" safe* - a scratch metric Generic Model "Heron 2D Read 3D" with one solid extrusion, a symbolic line "path 0,0; 500,0" on Ref. Level for coarse only (`DRAW_FAMILY_SYMBOLIC_LINES`); `which` "solid", then "symbolic line", then "all" | "solid": formCount 1, twoDCount 0, twoDList empty, findings as version 4's. "symbolic line": one row, "on plane Level : Ref. Level" (or Revit's own name for it), "shows in coarse \| plan", formCount 0. "all": formCount 1, the symbolic line in twoDList, and elements 1 - the extrusion ONLY |
| **ND5** | *A nested detail item and a link* - in ND4's family, a detail item loaded and placed in the view Ref. Level by hand, its Visible associated with a new Yes/No "Show Arrow" by hand; `which` "nested" | The nested row names its family and type, "in view Ref. Level", its point and "turned 0 deg", and "Visible yes (linked to Show Arrow)" |
| **ND6** | *Second route.* In the Family Editor, select each element and read Properties, the padlocks and Visibility Settings by hand | The same views, ends, locks and links as ND2 to ND5 |

**What this group also measures for others.** ND2 says whether a plain detail line reads as a loose `DetailCurve`
with nothing in a sketch - the question Group NC's first run raised for `LOCK_FAMILY_LINES_TO_PLANES` - and ND5
whether a detail item placed in a family's view has an owner view, Group NB's question.

**SIGNED 2026-11-12 by Ajmal PS - `REPORT_FAMILY_FORMS` version 5 is PROVEN, and NOT on this group's rows.** The
proof block in the card is a run of 2026-11-12 on another scratch family - "Heron Link Detail Test" (1,503
elements), Revit 2020, session 33000, [Group NE](group-ne.md)'s Detail Item family. `which` "2d" read 2 elements: a
detail line "from 0,0,0 to 300,0,0 mm, 300 long" and a filled region "1 loop(s), X 300 to 600, Y -80 to 80",
"3 sketch edge(s)" - the edges counted under the region and not listed as lines, which is what ND2 asked of
`GetDependentElements` - both "nothing holds it". The negative, `which` "nested", read none. A `proof.releases`
entry of the same day adds Revit 2024 ("R24 Arrow", session 13164): two detail lines, one an arc, and a filled
region, again "nothing holds it". [Group NF](group-nf.md)'s NF12 used the card to read a detail arc back at radius
200 mm.

**Of this group's rows: ND0 to ND6 were not run as written** - "Heron 2D Read Test" is the family in neither
proof. Nothing on record reads a lock, a label or a link back (ND2's "locked along" and "locked at an end to",
ND5's "linked to Show Arrow"), refuses a changed id or a region's edge (ND1, ND3), reads a form beside a symbolic
line (ND4) or a nested detail item (ND5), or compares by hand (ND6). The 2024 run's own
[work note](../work-notes/investigations/2026-11-11-family-2d-capabilities.md) lists "Group ND's lock rows" as
still owed. "Nothing here has run in Revit" and "back to DRAFT", above, were true on 2026-11-11.
*Written back 2026-11-12 from the card, in a cloud container (no Revit); nothing was run here.*
