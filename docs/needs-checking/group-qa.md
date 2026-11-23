# Needs checking — Group QA

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group QA - what an arrangement step made inside a write that stopped part way is never bound by the next request - BUILT, NOT RUN IN REVIT (2026-11-23)

**Tracked as GitHub issue [#1316](https://github.com/Ajmalpshaik/Heron-AI/issues/1316) since 2026-11-23** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**LOCAL work - it needs Revit on the owner's PC.** A cloud session cannot run any of it.

**Why:** [row 5b-1743](../fragment-issues/row-5b-1743.md). A setup (arrangement) step that writes commits inside the
fragment's own `TransactionGroup`, and what it made is carried so the fragment after it can bind it. Q-89's mark -
how a run was taken back - was written only by the run's own `Remember`, at its end, so a run that stopped before
that end (the fragment threw, the run clock tripped, a dialog or a later step stopped it) left the steps' values
carried with no mark, its group rolled back. **Now** (`revit/Heron.Revit.Addin/RevitFragment.cs`): a step's values
are marked as left inside an open group (`Chain.OpenGroup`); the run's own `Remember` clears the mark at its end;
and a request that finds it still set marks the chain taken back (`HeronBindingNote.StoppedPartWay`) before anything
binds, so Q-89's `chain_taken_back` refusal and its by-name gate hand none of it on.

**Not yet run anywhere.** The add-in compiles for Revit 2020 to 2027 (`tools/check-compile.py`, in a cloud container,
2026-11-23). `tests/test_binding_note.py` (the words, through `Heron.BindingNote.TestHost`) and
`tests/test_chain_expectation.py` (where the add-in asks them) hold it, each seen to fail first. **Reachable only
from the proving client:** `cmd_validate` is the one sender of `setup`, and no chat door sends it.

**The model: a scratch copy, never the working model** - a sample project saved as `%TEMP%\heron-work\qa\qa.rvt`.
The add-in rebuilt and redeployed (`python tools/heron-ready.py`) and Revit restarted. Revit 2020 first.

| ID | Do this | Pass looks like |
|---|---|---|
| **QA1** | *Positive - the arrangement still reaches the fragment.* `heron_bridge_client.py validate` a write phase whose setup step draws two model lines (`create-line`) and whose fragment is `find-overlapping-lines`, sent without apply | The fragment binds the two lines the step drew, as on 2026-09-17 (`Snowdon-scratch.rvt`); the run rolls back as any proof's trial. Nothing about the order changed |
| **QA2** | *Negative - stopped part way.* The same, with a second setup step that cannot be given what it needs (a need named that nothing supplies), so the run returns `setup_failed` after the first step committed | `setup_failed`, the model as it was. **Then** a request with `expectChain` naming `create-line` and no reset: refused `chain_taken_back`, naming *"a run that stopped before its end"*, NOTHING WAS BOUND. And one with no expectation whose need is `elements` from the chain: `elements` never supplied - not bound to the two lines Revit no longer holds |
| **QA3** | *Regression - a kept run's arrangement stands.* QA1 sent with apply on a scratch copy, the modeller presses Keep, then a request expecting `create-line` | Bound: the lines are in the model, and what was carried is handed on as before |

**Closing:** once QA1 to QA3 pass, row 5b-1743 is closed in its file with the release, model and date, and each id
here struck at both ends.
