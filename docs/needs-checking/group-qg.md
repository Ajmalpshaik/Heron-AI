# Needs checking — Group QG

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group QG - Automatically Embed: REPORT_CURTAIN_WALL_TYPE version 2 reads it, SET_CURTAIN_WALL_EMBED ticks it - DRAFT until proved (2026-11-24)

**Tracked as GitHub issue - number written here once it is opened.** The checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**LOCAL work - it needs Revit on the owner's PC.** A cloud session cannot run any of it.

**Why:** [row 5b-2008](../fragment-issues/row-5b-2008.md), issue [#1012](https://github.com/Ajmalpshaik/Heron-AI/issues/1012).
A plain question - *"is automatically embed on for this curtain wall"* - ranked the write `SET_CURTAIN_WALL_EMBED`
first on every routing backend, because no read card held the words. The owner chose option (a) on 2026-11-24: widen
the read. Two cards, written in a cloud session:

| Card | What it does in Revit words |
|---|---|
| [`report-curtain-wall-type`](../../brain/fragments/report-curtain-wall-type/fragment.yaml) (FRG-ELE-074, READ, **version 2**) | a curtain wall type's grid and mullions as before, **and** its *Automatically Embed* box and *Join Condition* (Type Properties > Construction) - read, never changed. Version 1 was PROVEN on 2026-10-18; its C# changed, so it is DRAFT again (D-30) |
| [`set-curtain-wall-embed`](../../brain/fragments/set-curtain-wall-embed/fragment.yaml) (FRG-ELE-176, MODIFY, version 1) | *Automatically Embed* ticked or not on the picked curtain wall's type, and which walls it cuts read back. Was rows QB10 and QB11 of [Group QB](group-qb.md) until it was held back on row 5b-2008 |

**What is done and what is not.** `ALLOW_AUTO_EMBED`, `AUTO_JOIN_CONDITION_WALL` and `AUTO_JOIN_CONDITION` were read
off all eight releases' reference assemblies with `tools/api-surface --members BuiltInParameter` on 2026-11-24 - all
three on 2020 to 2027; `RevitAPI.xml` (2024) labels the first *"Automatically Embed"* and **both** others *"Join
Condition"*. Both cards compile for 2020 to 2027 (`tools/check-fragments-compile.py`, cloud container, 2026-11-24).
Routing measured main against the branch on all four backends (the pull request has the numbers). **Not tested in
Revit.** `revit_change` refuses the write until its rows below are signed.

**What the run must show (it was assumed, not measured):**
- which of the two *Join Condition* ids a curtain wall TYPE carries - the read asks `AUTO_JOIN_CONDITION_WALL` first and names the id that answered;
- whether ticking *Automatically Embed* embeds a curtain wall **already drawn**, or only walls drawn after.

**The model: a scratch copy, never the working model** - "Heron QG Embed", a new project from the default
architectural template (metric), saved under `%TEMP%\heron-work\qg`. Arranged by hand: a basic wall *Generic -
200mm*, 9000 mm long, 3000 mm high, on Level 1; `DUPLICATE_TYPE` of *Curtain Wall: Curtain Wall* named *HERON QG
Embed*, its *Automatically Embed* UNTICKED; one 3000 x 2000 curtain wall of it drawn by hand on the basic wall's
line. Write down the type's *Automatically Embed* and *Join Condition* from Type Properties before the run. Revit
2020 first, then 2024. Each card's `tests/cases.yaml` has every case in full.

| ID | Do this | Pass looks like |
|---|---|---|
| **QG1** | `REPORT_CURTAIN_WALL_TYPE` on *HERON QG Embed* (box unticked) | `automaticallyEmbed` *"no"*; `joinCondition` the words Type Properties shows, Revit's value and the id that answered - RECORD the id; grid and mullion settings as Type Properties shows them |
| **QG2** | *Negative.* `REPORT_CURTAIN_WALL_TYPE` on *Basic Wall: Generic - 200mm* | Refused as version 1 refused it; `automaticallyEmbed` and `joinCondition` empty |
| **QG3** | Version 1's own proof cases again - *Curtain Wall: Storefront* | Every grid and mullion setting as version 1 read it on 2026-10-18; `settings` ends with a *"construction:"* part |
| **QG4** | *Routing, on the PC (model+rerank):* `heron_lookup` *"is automatically embed on for this curtain wall"*, then `revit_read` it with the type named | `REPORT_CURTAIN_WALL_TYPE` first; the box still unticked in Type Properties afterwards. *"tick automatically embed on this curtain wall type"* still `SET_CURTAIN_WALL_EMBED` |
| **QG5** | `SET_CURTAIN_WALL_EMBED` yes, the *HERON QG Embed* wall selected | `typesChanged 1`, the box reads ticked (QG1's read again says *"yes"*); RECORD whether it now cuts the basic wall or only a wall drawn after |
| **QG6** | *Negative.* `SET_CURTAIN_WALL_EMBED` with the basic wall picked, and yes twice | Refused *"Nothing given is a curtain wall"*; the second `typesChanged 0`, *"already has Automatically Embed ticked"* |

**Closing:** each card goes to PROVEN only with its positive, its negative and a person's name (D-30) -
`report-curtain-wall-type` on QG1 to QG3, `set-curtain-wall-embed` on QG5 and QG6; its rows here are struck with the
release, model and date.
