# Needs checking — Group LW

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group LW - Modify > Cut Geometry and Uncut Geometry in a project - one element cut by a named cutting element, a void family or another solid, each pair read back - and the read of what cuts what: `CUT_GEOMETRY`, `REPORT_GEOMETRY_CUTS` (2026-11-02)

Asked for in the modeller's words: *"cut this beam out of the slab"*, *"cut the column with the void family"*,
*"uncut these two"*, *"make the opening family cut the wall"* and *"which elements is this cutting"*. No card
did it: of every fragment's C# none called `SolidSolidCutUtils` or `InstanceVoidCutUtils`, and the ribbon
coverage note of 2026-11-01 lists both buttons MISSING. heron-duplicate-checker's method, run before writing:
`brain/heron_merge.py` found no shared signature with a shared capability; the duplicate check (`0 already
here`) and the Matcher (`0 match(es), 0 partial`) found nothing equal for either. **Look-alikes, kept
separate:** `JOIN_GEOMETRY` (DRAFT) is Join Geometry - two elements share their overlap - a different Revit
command and utility, so it was not widened; `REPORT_JOINED_ELEMENTS` (PROVEN) reads joins, where a cut does
not show, and widening its code would put its proof out of date; `COMBINE_FAMILY_FORMS` (PROVEN) is the
Family Editor's cut and join, by a combination; `COPE_FRAMING` (PROVEN) is the steel coping of beam ends;
`CREATE_OPENING` (DRAFT) makes a new opening element.

| What | Where | What it does |
|---|---|---|
| `CUT_GEOMETRY` (MODIFY, Revit 2020-2027) | [`brain/fragments/cut-geometry`](../../brain/fragments/cut-geometry/fragment.yaml) | Cuts each element handed in with `cutter`, or takes the cut away again - `cut` true or false, no default. **The cutter is never guessed:** it is the one element selected and passed as *"selected"*, as `JOIN_GEOMETRY` names its target; with two or more selected the add-in refuses, because a selection has no order. The elements to cut come from the chain. A family instance with a void that cuts when loaded cuts by Revit's void cut; anything else by a cut between solids, asked first so a pair Revit will not cut is refused with Revit's reason in words. A pair already cut, or already not cut, is left alone and counted; a cut that runs the other way is refused, never turned round. Every pair is read back into `pairs` |
| `REPORT_GEOMETRY_CUTS` (READ, Revit 2020-2027) | [`brain/fragments/report-geometry-cuts`](../../brain/fragments/report-geometry-cuts/fragment.yaml) | For each element handed in, what it cuts and what cuts it, by both kinds of cut, by name and id. Joins are not read; an element with no cuts is counted in `withoutCuts` |

**What is done and what is not.** Both compile on every release 2020 to 2027 (`tools/check-fragments-compile.py`,
2026-11-02, a Linux container). Every Revit member they call was read off the reference assemblies of all eight
releases with `tools/api-surface --members` - the lists are in each card's `compatibility-note`; there is no
`HasVoidInstanceCut` in any release. **Nothing here has run in Revit.** Both cards are DRAFT. **Only Revit can
settle:** whether a beam and a floor are a pair Revit cuts between solids in a project, or answer
*CutNotAppropriateForElements* (LW2); that an opening family takes the void route (LW3); that a read-back after
the call sees the cut without a regeneration (LW2, LW3). **Not done:** turning a cut round - uncut, then cut
naming the other element - and the option to split the faces of the cutting solid.

**Routing.** `heron_retrieve.find` on fresh scratch stores of the tree at `cd323cb` and of this change, over the
five sentences and every live card's utterance and routing row with *cut*, *void*, *join*, *opening* or *cope*
in it (133), on Revit 2024, 2027 and none named, with and without the host's question flag. The four writes
reach `CUT_GEOMETRY` without the flag (before: `COPE_FRAMING`, `COMBINE_FAMILY_FORMS`, `CREATE_OPENING`);
*"which elements is this cutting"* reaches `REPORT_GEOMETRY_CUTS` with and without it (before:
`REPORT_DIVIDED_SURFACES`). `tools/check-routing.py` on scratch stores: no question answered by a write before or
after; claimed and not reached 68 before, 69 with the new cards - *"draw a curved wall through these points"*
moved off `CREATE_WALL` - and 68 after `CREATE_WALL` declared it, the same lines as before
([row 5b-1416](../fragment-issues/section-5b-rows-401-425.md)). With the question flag only, six write
sentences moved between reads to the new read - recorded there, not tuned. *"Which one is cutting"*, a row of
`JOIN_GEOMETRY`'s table, reached no card it names before or after
([row 5b-1417](../fragment-issues/section-5b-rows-401-425.md), OPEN).

**The model** - a scratch project from Revit's default structural template, saved under `%TEMP%\heron-work`,
never a working model. Every write here is ROLLED BACK unless the modeller keeps it. The job file is
[`tools/jobs/cut-geometry-lw-2026-11-02.yaml`](../../tools/jobs/cut-geometry-lw-2026-11-02.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`, `--session
<process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **LW1** | *Arrange.* A new project from the default structural template. On Level 1: a floor; ONE beam passing through the floor's thickness; a wall standing on the floor, clear of the beam, JOINED to the floor with Modify > Join Geometry. A floor plan *LW Cutter* showing Structural Framing only. Write down the floor's and the wall's ids (Manage > IDs of Selection) into the job file. Save under `%TEMP%\heron-work` | *LW Cutter* shows the one beam and nothing else |
| **LW2** | *D-30 positive and negative, THE NEGATIVE FIRST.* The job file's `cut-geometry` job, `--write`, rolled back | Negative, the wall: changed 0, the wall in `wouldNot` with Revit's reason in words, the binding note *elements from filter-elements-by-id (1)*. Positive, the floor: changed 1, `pairs` *"... cut by ... (solid cut) - read back: cut"*, QUANTITIES HAVE MOVED. **If Revit answers *CutNotAppropriateForElements* for the beam and floor, that is Revit's rule, not a pass - arrange LW2 again with two generic model families, and record which** |
| **LW3** | *The void route, and uncut.* A non-hosted generic model family with an unattached void and *Cut with Voids When Loaded* on, placed so its void overlaps a second wall that it does not yet cut, and selected alone; that wall's id typed: `cut=true`, then the same pair `cut=true` again, then `cut=false`, each rolled back | changed 1 with *"(void cut)"*; then changed 0 and the wall in `alreadyRight`; then *"uncut from"*. Kept once through `try-once` on the modeller's yes: ONE entry in Revit's undo list |
| **LW4** | *The read.* After keeping a cut by hand (Modify > Cut Geometry, the floor then the beam): the job file's `report-geometry-cuts` job | Positive, the floor: *"... is CUT BY ... - solid from solid"*. Negative, the joined wall: no rows, `withoutCuts` 1 - a row would mean it reads joins |
| **LW5** | *The refusals.* The cutter among the elements; two elements selected with `cutter=selected`; `cut=sideways`; a floor already cutting the beam (made by hand) with the beam the cutter, `cut=true` | Refused by id; the add-in refuses before the run; refused before anything changes; *"the cut already runs THE OTHER WAY ... NOT turned round"* - nothing changed in any |
| **LW6** | *The second route.* Modify > Cut Geometry and Uncut Geometry by hand on the same pairs | The same section after each; Uncut Geometry offers exactly the pairs LW4 lists |
