# Needs checking — Group QV

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in this
> file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back into the
> register for every tool that reads the register.

## Group QV - a delete whose count is not the one the yes named stops before anything goes: `DELETE_ELEMENTS` version 4 - DRAFT until proved (2026-11-26)

**Tracked as GitHub issue [#1432](https://github.com/Ajmalpshaik/Heron-AI/issues/1432) since 2026-11-26** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**LOCAL work - it needs Revit on the owner's PC.** A cloud session cannot run any of it.

**Why:** [row 5b-1990](../fragment-issues/row-5b-1990.md), issue
[#1278](https://github.com/Ajmalpshaik/Heron-AI/issues/1278), owed part (2). On 2026-11-23, Revit 2020, a real job,
the method [`replace-in-place-family`](../../brain/skills/replace-in-place-family.yaml) asked the owner's yes for
36 elements and Revit's delete took 62; the method compared `deleted` only AFTER the delete.
**[`DELETE_ELEMENTS`](../../brain/fragments/delete-elements/fragment.yaml) version 4** takes the count the yes
named as `expectedCount` - [`REPORT_ELEMENT_DEPENDENCIES`](../../brain/fragments/report-element-dependencies/fragment.yaml)
version 2 gives it as `goesTotal` - deletes the whole request inside one SubTransaction, rolls it back, and
deletes NOTHING when Revit's own count differs, answering with Revit's list by category. Left out, the delete runs
as version 3. Its C# changed, so it is **DRAFT** until this group runs and Ajmal PS signs (D-30). The version 3
proof stays on the card as the record. **The dimensions themselves** - which a delete takes and which it cuts -
are [Group QT](group-qt.md)'s, for `REPORT_ELEMENT_DEPENDENCIES` version 2; run QT first, on the same model.

**Not yet run anywhere.** Compiled for Revit 2020 to 2027 (`tools/check-fragments-compile.py`, the owner's
laptop, 2026-11-26). **Not known, and what QV2 settles:** whether Heron's own Delete box and its Keep or Discard
box stay away when the call stops - nothing is deleted, so neither should appear.

**The model: a scratch copy, never the working model** - a sample project saved as
`%TEMP%\heron-work\qv\qv.rvt`, Revit 2020 first. On Level 1's floor plan, two loadable boxes A and B (any
Generic Model or Mechanical Equipment family), one tag on A, and one dimension A to B. **Ask to Delete** on in
Settings. Every run that keeps a change is Undone after.

| ID | Do this | Pass looks like |
|---|---|---|
| **QV1** | *Positive - the count met.* `REPORT_ELEMENT_DEPENDENCIES` on A, then `DELETE_ELEMENTS` on A with `expectedCount=` its `goesTotal`, Keep | `countChecked` says Revit's trial took that count before anything was deleted; `deleted` and `deletedWhat` hold A, its tag and the dimension, and nothing else; Revit's Delete box showed the same count. Undo puts all of it back in one step |
| **QV2** | *Negative - a count that differs stops it.* The same with `expectedCount=` one LESS | The call answers `STOPPED BEFORE THE DELETE - NOTHING HAS BEEN DELETED`, both numbers, Revit's list by category and every id beyond A. **No Delete box and no Keep or Discard box.** A, its tag and the dimension are all still there - the model's element count unchanged. Then the same call with `expectedCount=` the count it named deletes, as QV1. Undo |
| **QV3** | *Negative - more than Revit takes.* `expectedCount=` one MORE than QV1's | Stopped the same way, nothing deleted - a count that is too high is as wrong as one too low |
| **QV3b** | *Negative - a 0 is a count.* `expectedCount=0` on A | Stopped the same way, nothing deleted: a yes that named nothing never lets a delete through. Only a count left out skips the check |
| **QV4** | *Regression - no count given.* `DELETE_ELEMENTS` on A with `expectedCount` left out, Keep | `countChecked` empty; `deleted`, `deletedWhat` and `deletedByCategory` the same as QV1's - version 3's delete, unchanged. Undo |
| **QV5** | *Two asked, one taking the other.* A wall with a door hosted in it; both handed in, `expectedCount=` the wall + the door + the door's tag | The trial counts the door once (it went with the wall, so its own delete takes nothing more), the count is met and the delete runs. **Write down** what `refused` says about the door, and whether the call failed on it: the real delete reads each element asked for after the one before it went, as version 3 did, and this is the first check that hands it an element already gone. Undo |
| **QV6** | *The method.* [`replace-in-place-family`](../../brain/skills/replace-in-place-family.yaml) step 10 walked on an in-place unit with a tag and two dimensions to it (Group QT's model will do) | The yes question names `goesTotal`, every dimension deleted and every one cut with its view; `DELETE_ELEMENTS` is sent with that `expectedCount`. Given a wrong count on purpose, the chat names Revit's list and asks again - it never leaves `expectedCount` out to get past the stop |
| **QV7** | QV1, QV2 and QV4 on **Revit 2024** and **2027** | The same answers. 2024 is the first release of 64-bit element ids; 2027 runs on .NET 10 |

**Signing, once QV1 to QV6 pass:** the card's proof is written by `tools/batch-prove.py` with the owner, and
`heron-status` set to PROVEN in the same change - never by a session, and never on a working model.
