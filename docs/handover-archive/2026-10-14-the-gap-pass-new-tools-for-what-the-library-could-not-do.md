# Session note — THE GAP PASS: NEW TOOLS FOR WHAT THE LIBRARY COULD NOT DO

> **Session note** from 2026-10-14, written straight into this folder, one file per sitting, as
> [`README.md`](README.md) asks. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**. A note records what was true on its own day.

---

### 2026-10-14 — THE GAP PASS: NEW TOOLS FOR WHAT THE LIBRARY COULD NOT DO

The owner asked for every fragment to be checked - *"is there anything we are missing ... or we can
merge something, we can add something"* - and then, going to sleep, for the work to carry on without
him until Revit was free. A cloud session, no Revit; every tool below is **DRAFT** and **not run**.

**How the pass was done.** Every card on `main` `bed17298` was read by area. The library's own merge
detector compares signatures and found only plumbing matches, so look-alike pairs were checked by hand
for a routing row naming the partner - more than sixty had one. Gaps were tested by searching every
fragment's code for the Revit call a job needs, then by asking the search what a modeller's sentence
reaches.

**What was missing, and is now a DRAFT tool** - [NEEDS-CHECKING Group EC](../needs-checking/group-ec.md)
has the test project, its parts and one dry-run-clean job file per row:

| Tool | The gap |
|---|---|
| `CREATE_FLEX_PIPE` | Flex sprinkler drops - only flex duct existed |
| `CREATE_PANEL_SCHEDULES` | Panel schedule views - the circuits could only be read |
| `CREATE_FIXTURE_BRANCHES` | A sprinkler or fixture joined to its main - Revit has no pipe twin of the duct tap |
| `CREATE_AREA_PLAN` | The area plan `PLACE_AREAS` needs |
| `PLACE_DETAIL_COMPONENTS` | Detail items and annotation symbols into a view |
| `OFFSET_MEP_SECTION` | A stretch of duct or pipe over or under a clash |
| `PLACE_MODEL_GROUPS` | Another instance of a model group type |
| `ACQUIRE_COORDINATES` | Shared coordinates from a Revit link - ADMIN |
| `SET_LINK_ROOM_BOUNDING` | Why spaces will not place in an MEP model |
| `CREATE_WALL_ELEVATION` | An elevation turned square to one wall |
| `SET_WORKSHARING_OWNERSHIP` | Make a workset editable, or relinquish, without a sync - PUBLISH |
| `SELECT_GROUP_MEMBERS` v2 | An assembly opened, not built |

Two methods string them together: `sprinkler-piping` and `clash-reroute`.

**What overlaps, recorded for the owner** - rows 5b-469 to 5b-474: two PROVEN fragments doing one job
(469), *"what is in this assembly"* reaching a write (470, fixed), three PROVEN pairs that never named
each other (471, comments added), two QA domains (472), four sentences declared twice (473), and no
read for a link's Room Bounding or path type (474).

**What a new card does to the old ones.** Every new card took sentences from older ones at first -
*"what is the gross floor area"* reached the new area-plan WRITE, *"what are the shared coordinates of
this"* the new ADMIN one. `tools/check-routing.py` run on fresh stores from `main` and from the branch,
and its claimed-and-not-reached lists compared, found each; every one was given back by declaring the
sentence on the read that answers it or by changing the new card's words. **Compare against `main`, not
against nothing** - the lexical route is noisy on paraphrase everywhere (*"open the level 2 plan"*
reaches `CREATE_STAIRS` on `main`), and only the difference says what a change did.

**Two failures only the full suite found**, both this branch's: a pinned roll-call in
`test_generate_jobs.py`, and `test_owner_queue.py` reading the test project's part labels - written
`**P1**` - as register rows, colliding with Group P. A table of anything but register rows must not
start a row with a bold capital-and-digit cell.

**Not built, on purpose:** ramps, new phases, Copy/Monitor and legend components have no API to call;
a rotating part on a reference line is stopped at the API in docs/43 §9 (row 280); exterior elevations
already come with the templates.

**Waiting on the owner:** the EC rows on a test copy; the merge in 5b-469; 5b-472's domains;
5b-473's duplicate sentences; widening the PROVEN `LIST_LINKED_MODELS` for 5b-474.
