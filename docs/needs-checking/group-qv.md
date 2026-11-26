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

**Version 4 was not run before 2026-11-26** (that day's run on rooms is under Results). Compiled for Revit 2020 to 2027 (`tools/check-fragments-compile.py`, the owner's
laptop, 2026-11-26). **Not known, and what QV2 settles:** whether Heron's own Delete box and its Keep or Discard
box stay away when the call stops - nothing is deleted, so neither should appear.

**Version 5 (2026-11-26) - the stop is a refusal now, and every row below is owed on it.** On version 4 the stop
was THROWN, so a negative came back `fragment_threw` and could not sign the card
([row 5b-2060](../fragment-issues/row-5b-2060.md), issue [#1462](https://github.com/Ajmalpshaik/Heron-AI/issues/1462)).
[`DELETE_ELEMENTS`](../../brain/fragments/delete-elements/fragment.yaml) version 5 (DRAFT) gives the same words in a
new output **`refusedWhy`**, every id asked for in `refused`, and `deleted` empty - nothing deleted. Read QV2, QV3 and
QV3b's *"The call answers STOPPED BEFORE THE DELETE"* as **`refusedWhy` says it, never `fragment_threw`**, and write
down the reply's `outcome:` line - it should read refused. The same holds when the trial cannot start. Compiled for
Revit 2020 to 2027 (`tools/check-fragments-compile.py`, 2026-11-26); never run in Revit. **The re-run:** the proof job
`tools/jobs/qv-delete-elements-v4-on-mep-revit2020-2026-11-26.yaml` (on PR #1450's branch until it merges), on the
same scratch copy's 78 rooms, with `expectedCount` **81** for the positive (Revit's own count) and **80** for the
negative - **change the job's `set` from 78 to 81 and its `negative-set` from 77 to 80 first**: as written it
names 78, which version 5 now refuses, so its positive would have nothing to show. Then 82 and 0 by hand. **The
Keep or Discard box:** a DRAFT's one run in a chat is asked whatever it counted (D-119), so it may still appear
after a refusal - write down whether it did, and what its count said. Why Revit's count is 81 and the dependency report's 78 is row 5b-2061, a separate
fix.

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

**Version 5 run and signed, 2026-11-26 - Revit 2020, session 27788, add-in file `df0d401c52ca05f4`:** this pull request's
head `021fd164d`, on *on-mep* (a scratch copy of Revit 2020's `rme_advanced_sample_project.rvt`, `%TEMP%\heron-work\on`),
its 78 Rooms (Revit's delete takes 81 - the rooms and 3 no-category elements), rolled back. Proof job
[`qv-delete-elements-v5-on-mep-...`](../../tools/jobs/qv-delete-elements-v5-on-mep-revit2020-2026-11-26.yaml): **PASS** -
`expectedCount=81`: `deleted` 81, nothing refused; `expectedCount=80`: `deleted` 0, the stop returned through `refused`,
**not thrown**. **Signed by Ajmal PS** (his typed word in chat, "Yes, sign it"; signature put on by the session),
card PROVEN. Still owed: the A-tag-dimension rig of QV1/QV2 itself, QV3 to QV7.

