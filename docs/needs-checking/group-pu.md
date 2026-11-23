# Needs checking — Group PU

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group PU - LOAD_FAMILY version 4: each material compared with itself by id, never with another of its name - DRAFT until proved (2026-11-23)

**Tracked as GitHub issue [#945](https://github.com/Ajmalpshaik/Heron-AI/issues/945) since 2026-11-23** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**LOCAL work - it needs Revit on the owner's PC.** A cloud session cannot run any of it.

**Why:** [row 5b-1995](../fragment-issues/row-5b-1995.md). On 2026-11-23, Revit 2020, a NEW family load reported
8 project materials "changed" that had not moved: the project held 3 materials named *Default*, 3 named *Poche* and
2 each of *Phase-Demo*, *Phase-Exist* and *Phase-Temp*, and versions 1 to 3 paired before and after by NAME.
[`LOAD_FAMILY`](../../brain/fragments/load-family/fragment.yaml) **version 4** reads each material by its element id
before the load and the same id after, labels a repeated name `Name (id N)`, and names every repeated name in one
`findings` line. Its C# changed, so it is **DRAFT** until this group runs and Ajmal PS signs (D-30). The version 3
proof stays on the card as the record.

**Not yet run anywhere.** Compiled for Revit 2020 to 2027 (`tools/check-fragments-compile.py`, in a cloud container,
2026-11-23); `tests/test_load_family_material_ids.py` holds the rule as text.

**The model: a scratch copy, never the working model.** Revit's Material Browser is expected to refuse a name
already in use (not checked here), so how to arrange two materials of one name from nothing is **not known**. Use a scratch copy of the project row 5b-1995 was seen on, which already holds them: *File > Save As* to
`%TEMP%\heron-work\pu\pu-project.rvt` (*Detach from Central* if it is workshared, discarding worksets). The family:
a scratch copy of the same inertial air filter `.rfa`, in `%TEMP%\heron-work\pu\`, which carries *Poche*, *Default*
and *Glass* of its own. Revit 2020 first. Every load is Undone after.

| ID | Do this | Pass looks like |
|---|---|---|
| **PU1** | *Negative first - two same-named materials, a family carrying that name.* `LIST_MATERIALS` on pu-project and write down every *Poche*, *Default*, *Glass* and *Phase-* material with its id and colour. `LOAD_FAMILY` the scratch family (not yet in the project), Keep. `LIST_MATERIALS` again | `reloaded` says it was loaded new. **`materialsChanged 0 item(s)`** while every id written down reads the same colour after as before - version 3 listed 8. `findings` names *Default (3)*, *Poche (3)*, *Phase-Demo (2)*, *Phase-Exist (2)*, *Phase-Temp (2)* (and any other repeated name). Any material in `materialsAdded` is one whose id was not in the first list. Undo the load |
| **PU2** | *Positive - a real change to ONE of a repeated name is still seen, on the right id.* On pu-project, make a scratch family whose own material is named *Poche* at colour 10,200,10, saved as `%TEMP%\heron-work\pu\pu-poche.rfa`; `LOAD_FAMILY` it, Keep; `LIST_MATERIALS` | If Revit moved a project *Poche*: exactly that one is in `materialsChanged` as *Poche (id N): ... -> ...*, N the id whose colour `LIST_MATERIALS` shows moved, and no other *Poche*. If none moved: `materialsChanged` 0 and every *Poche* id reads as before - write down which happened, it is what Revit does to a repeated name on a load and nobody has measured it. Undo the load |
| **PU3** | *Regression - a name held once, as version 3.* A new scratch project from the default template, which holds each material name once. `LOAD_FAMILY` the same scratch family, Keep | Lines read `Name: before -> after` with no id, as version 3; `findings` empty; anything new in `materialsAdded` by name. Undo the load |

**Signing, once PU1 to PU3 pass:** the card's proof is written by `tools/batch-prove.py` with the owner, and
`heron-status` set to PROVEN in the same change - never by a session, and never on a working model.
