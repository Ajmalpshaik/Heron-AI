# FRAGMENT-ISSUES archive — section 5b, rows 1701 to 1725

> **Finished rows, moved out of the live register.** These are rows from section 5b of
> [`FRAGMENT-ISSUES.md`](../FRAGMENT-ISSUES.md) — Heron's own defects found by reading the repository — whose state said they were
> finished when they were moved. Each still has its line in the register: the same number, a
> one-line title, the opening of its state, and a link to its full text here.
>
> **Nothing below was rewritten** except its links, which were re-pointed one folder deeper and
> each checked to reach the same file it reached before. Where a row disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win** — a row records what was true on
> the day it was written.
>
> **Nothing new is written here.** A new defect goes in the register, and a row here that turns
> out not to be finished is reopened **there**, on its own line.
>
> Rows 1701 to 1725 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-1702

*Moved from the register on 2026-11-17.*

**Defect.** **`LOAD_FAMILY` REPORTED ON EVERY LOAD OF A NEW FAMILY THAT THE PROJECT'S `Glass` WENT FROM 0,0,255 AT 75% TRANSPARENCY TO 245,245,243 AT 100% - AND HERON CANNOT TELL WHETHER THE GLASS REALLY CHANGED OR THE READING IS WRONG.** Reported 2026-11-11 by Sruthina in Ajmalpshaik/Heron-AI#853, one house build on Revit 2024, model `villa 2` (item C4): every `LOAD_FAMILY` of a new family listed `materialsChanged` - *Glass* 0,0,255 / 75% to 245,245,243 / 100%, *Default Light Source* 75% to 100%, and *Poche*'s appearance asset swapped. **Confirmed in code** (`brain/fragments/load-family/impl/any/fragment.cs`, PROVEN v3): every project material is described by name before the load and again straight after `LoadFamily` returns, inside the job's still-open transaction, with no `Regenerate` between, and a description that differs is listed. So the list is Revit's own reading of the project's `Material` elements at that moment, not a guess - and a load CAN overwrite a same-named material that stays overwritten: the 2026-09-16 Copper case was still 128,0,0 when `SET_MATERIAL_COLOUR` read it later. **Only reported:** whether the Glass stayed at 100% once the load was kept. *If* every load listed the same "0,0,255 / 75% ->" before-value, the first load's change did not last, which points at the reading; the report does not say. Nothing in Heron reads a material's transparency on its own to check (`LIST_MATERIALS` names materials and appearance assets).

**State.** **MOVED TO ISSUE [#945](https://github.com/Ajmalpshaik/Heron-AI/issues/945) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired; the check is in [Group NY](../needs-checking/group-ny.md) NY5.** Which of two fixes depends on NY5: (a) the Glass really changes - the load should put same-named project materials back, or refuse and name them, before the job is kept; (b) the reading is early - read the after-values once the load has regenerated, or after the group closes. Either changes `LOAD_FAMILY`'s C#, so it goes back to DRAFT at v4: **the owner's choice.** Until then, after a family load check *Manage > Materials > Glass > Graphics > Transparency* by hand.

---

### Row 5b-1709

*Moved from the register on 2026-11-17.*

**Defect.** **A FAMILY BUILT FROM THE METRIC CASEWORK TEMPLATE, DRAWN IN PROJECT COORDINATES AND PLACED AT 0,0,0, CAME OUT 45 MM SHORT IN X, AND ITS BOX REACHED BACK TO (-405, -600) WHERE NOTHING IS DRAWN.** Reported 2026-11-11 by Sruthina in Ajmalpshaik/Heron-AI#853, one house build on Revit 2024, model `villa 2` (item C5). **Only reported** - nothing here can open the template. The method (`brain/skills/family-creation.yaml`) says to run `REPORT_FAMILY_TEMPLATE` first, set *Defines Origin*, keep the shape about its insertion point and draw where the planes are NOW; `REPORT_FAMILY_TEMPLATE` (PROVEN v4) lists each reference plane's position and which defines the origin, and **does not list solids, forms or other content already in the template** - which a box reaching (-405, -600) suggests is there. Nothing in the method says a family is drawn from its own origin planes and not in project coordinates.

**State.** **MOVED TO ISSUE [#960](https://github.com/Ajmalpshaik/Heron-AI/issues/960) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired.** Two fixes, neither moves C#: (1) the method says to draw from the origin planes `REPORT_FAMILY_TEMPLATE` names, and to compare the drawn extent with them before saving; (2) whether the casework template carries content of its own is measured once on the PC - open it, list its elements - and the method names it. **(2) needs a real Revit.**

---

### Row 5b-1711

*Moved from the register on 2026-11-17.*

**Defect.** **EVERY LOOKUP ON THE PC WARNED *"The knowledge store does not match this checkout: 3 card(s) ..."* - `dimension-views-by-rules`, `draw-mep-schematic`, `tag-views-by-rules` - FOR A WHOLE SESSION.** Reported 2026-11-11 by Sruthina in Ajmalpshaik/Heron-AI#853, one house build on Revit 2024, model `villa 2` (item C8). **Confirmed in code:** `_store_drift` (`mcp/server/heron_brain.py`) says this when cards on disk have no row in the store. The shared store adds new cards by itself only from the main checkout on `main` (`brain/heron_scope.py`, rows 5b-233 and 5b-406); elsewhere the warning names no command, only *"rebuild it only from there - or set HERON_KNOWLEDGE to an empty folder"*. All three cards validate here. **Only reported:** which checkout the PC ran from; the quote is cut, so which of the two sentences it printed is not known.

**State.** **REPAIRED 2026-11-16 in code, NEEDS THE PC** - NEEDS-CHECKING [Group OD](../needs-checking/group-od.md) OD8 and OD9. Two halves. **(1) `tools/heron-ready.py` rebuilds a drifted store it may build** - a card with no row, or a row with no card - by running `heron_scope.py --rebuild` in its own process, compares again, and says *"rebuilt, because ..."*, or FIX when the store still does not match (a card that does not validate is given no row). Opening a store adds no card, so it said "ready" over a store every lookup then warned about. **(2) The warning names what reaches it, by where it is read** (`heron_brain._drift_remedy`): a private store - the rebuild, with its folder; the shared store from the main Heron folder on main - `python tools/heron-ready.py`; the main folder on a branch - `git checkout main`, after which the next lookup adds what main has (row 5b-406); a worktree - the main Heron folder BY PATH, `git pull` there for cards it lacks or `git merge origin/main` here for rows with no card; a worktree whose main folder cannot be found, or a question about the store that fails - no rebuild offered. Everywhere a rebuild cannot help, it offers HERON_KNOWLEDGE in that folder's `.mcp.json`. `--rebuild` is named only where it can help: from a worktree it rebuilds the shared store from MAIN's cards (row 5b-404) and adds none of the worktree's. Held by `tests/test_store_drift_remedy.py` (six checks red on the code as it stood, no crash) and `tests/test_heron_ready.py` section 3a (two red). Python only; no card moved.

---
