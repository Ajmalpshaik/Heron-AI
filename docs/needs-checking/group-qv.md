# Needs checking — Group QV

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in this
> file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back into the
> register for every tool that reads the register.

## Group QV - the Delete box announced: a preview that deletes says the box is coming and what goes by category, and a box that closed by itself says TIMED OUT with the time it had - NOT RUN (2026-11-26)

**Why:** seen 2026-11-22 on Revit 2024.3, model `Tower 171.rvt`: making a curtain system on 14 faces of a mass
would also delete 49 elements, so Heron's Delete box ([`HeronDeleteQuestion`](../../revit/Heron.Revit.Addin/HeronDeleteQuestion.cs),
D-142) came up. Nobody had been told to expect it, nobody answered in time, it closed by itself, and nothing was
made or deleted; the 49 ids the dry run had listed no longer existed when looked up ([row 5b-1992](../fragment-issues/row-5b-1992.md),
issue [#1280](https://github.com/Ajmalpshaik/Heron-AI/issues/1280)). Fixed in code 2026-11-26:

- A **preview** (dry run) that Revit counts deletions in now carries `deleteAhead`, `deleteAheadByCategory`,
  `deleteAheadMadeAndGone` and `deleteAheadSays`, read AFTER the preview's rollback
  ([`RevitFragmentDelete.cs`](../../revit/Heron.Revit.Addin/RevitFragmentDelete.cs) `WithDeleteAhead`): the
  elements back in the model are counted by category, and an id that names nothing - made by the change itself
  and removed by it again - is counted apart, never listed. The chat prints `deleteAheadSays` first
  ([`heron_run_records.verdict_line`](../../mcp/server/heron_run_records.py)): kept, Revit will show the Delete
  box for them - or, with Ask to Delete off, it will not and the chat's word is the yes (D-197).
- A box that **closed by itself** now says *"TIMED OUT, NOT ANSWERED - nothing was made and nothing was
  deleted"* and how long it had to be answered (`revit.askTimeoutSeconds`, 300 s by default).

**What is done and what is not.** The add-in compiles; `tests/test_delete_box.py` section 8,
`test_keep_question.py`, `test_ask_to_delete.py` and `test_rollback_witness.py` pass. **Never run in Revit.**
That a preview's deleted ids are back in the model after its rollback, and that the curtain-system case's ids are
the change's own, are expected, not seen. **Owed to the owner, not decided here:** whether 300 seconds is long
enough for a change this size.

**The model: scratch** - Revit 2024 (the release it was seen on), a copy of a sample model or a new project with
one mass. Every run is a dry run or answered as the row says.

| ID | Do this | Pass looks like |
|---|---|---|
| **QV1** | **Negative, first.** A dry run of a change that deletes nothing - rename one view | No `deleteAhead` on the reply, and the chat's line reads exactly as before |
| **QV2** | A dry run of a change that deletes elements already in the model - change one wall's type to one that loses its hosted door, or delete two walls with doors | `deleteAhead` equals what Revit counted, `deleteAheadByCategory` names each category with its count, `deleteAheadMadeAndGone` 0, and the chat's first line is `deleteAheadSays` |
| **QV3** | A dry run of `CREATE_CURTAIN_SYSTEM` on mass faces where it removes elements it made (the row's case) | Write down `deleteAhead` and `deleteAheadMadeAndGone`: the ones not in the model after the rollback are counted apart, never listed by id |
| **QV4** | With Ask to Delete off (Heron's Settings), QV2 again | `deleteAheadSays` says Revit will not ask and the chat's word is the yes |
| **QV5** | Keep QV2's change (Ask to Delete on) and do not answer the Delete box; set `revit.askTimeoutSeconds` to 60 in heron.config first | The box closes by itself after 60 s; nothing deleted; the reply's `deleteAsking` says *"TIMED OUT, NOT ANSWERED"* and *"the box had 60 seconds to be answered"*. Put the setting back |
