# Needs checking — Group PS

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group PS - a kept delete names what went: every deleted id's category and name, read before it went, and a form's own sketches counted together: `DELETE_ELEMENTS` version 3 - BUILT, NOT RUN IN REVIT (2026-11-23)

**Tracked as GitHub issue [#1283](https://github.com/Ajmalpshaik/Heron-AI/issues/1283) since 2026-11-23** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** [row 5b-1996](../fragment-issues/row-5b-1996.md). On 2026-11-23 (Revit 2020, add-in 0.3.0.0, family documents)
the owner pressed Delete in Revit's box several times while the reply could not say what the extra elements were:
`alsoWent 1`, `alsoWent 220`, and a run record holding only `{"change": "deleted", "id": "9021"}`.
[`DELETE_ELEMENTS`](../../brain/fragments/delete-elements/fragment.yaml) version 3 tries each delete first in a
`SubTransaction` it rolls back, reads every id the trial returns while it still exists, then deletes for real.
`deletedWhat` names every deleted id (`category | name | why it went`); `deletedByCategory` counts it. It compiles for
Revit 2020 to 2027 (`tools/check-fragments-compile.py`) and every member it calls exists on every release
(`tools/check-api-surface.py`). **None of it has met a Revit.** This is LOCAL work: a cloud session cannot do it.
Rebuild and restart Revit first (`python tools/heron-ready.py`) - that needs Revit closed, so it is the owner's step.

| Change | Files | Checks |
|---|---|---|
| The trial delete, rolled back, and every id it returns read before the real delete | `brain/fragments/delete-elements/impl/any/fragment.cs` | PS1, PS3, PS4 |
| `deletedWhat` and `deletedByCategory`, a form's own sketch counted as "their sketches" | the same | PS1, PS2, PS3 |

**The models: scratch copies, never a working model.** Saved for it in `%TEMP%\heron-work\ps`: a family (a new
Generic Model family with 3 extrusions drawn by hand, and one nested family loaded with two types, one unused), and a
scratch project with 4 ducts drawn on their own and one floor. Ask to Delete ON, so Revit's Delete box comes up -
its own count and category list is the second source every check compares against.

| ID | Do this | Pass looks like |
|---|---|---|
| **PS1** | *Forms and their sketches.* In the scratch family select the 3 extrusions and ask the chat to delete them through `revit_change` (`DELETE_ELEMENTS`). Press Delete in Revit's box. Then read the run record with `heron_run_record`. | The box's count equals `deleted`. `deletedByCategory` begins *"N deleted = 3 form(s) + their sketches"*, and its sketch(es), sketch plane(s) and sketch line(s) add up to `alsoWent`. `deletedWhat` has one entry per deleted id, every extra one ending *"of form <id>"* with one of the three form ids. The run record holds every `deletedWhat` entry. |
| **PS2** | *A type and what Revit took with it.* In the scratch family select the unused nested type and delete it. | `deletedWhat` names the type *"asked for"* with its category and name, and any extra id *"went with <the type's id>"* with a category that matches what Revit's own box listed for it. `deletedByCategory` says *"Asked for 1: ..."* and, when something else went, *"Also went N: <category> N"* - never *"form(s)"*. |
| **PS3** | *THE NEGATIVE CASE, by tracking (D-53) - a different delete, a different answer.* In the scratch project select the 4 ducts and delete them; then undo, select the floor and delete it. | Ducts: *"4 deleted. Asked for 4: Ducts 4. Nothing else went."*, 4 entries each *"Ducts \| <type name> \| asked for"*. Floor: the floor *"asked for"* and its own sketch's parts by category under *"Also went"* - **not** counted as *"their sketches"*, which is for a form only. Nothing in either says NOT READ. |
| **PS4** | *The trial leaves nothing behind.* Repeat PS3's duct delete and press Don't delete; then repeat it and press Delete, and Ctrl+Z once. | Don't delete: all 4 ducts are still there and the undo list has no Heron entry. Delete: the box listed exactly the count `deleted` reports - not double - no warning appears that the same delete without Heron does not raise, and one Ctrl+Z brings back exactly what `deleted` listed. |
