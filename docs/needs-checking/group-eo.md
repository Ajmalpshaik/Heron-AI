# Needs checking — Group EO

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group EO - a rollback that takes the last kept job's work is said, and a rollback whose additions still stand is not called complete (2026-10-17)

A fragment's preview is the run itself, rolled back ([D-55](../DECISIONS.md)), so Heron rolls back far more
than it keeps. [Section 1c](../fragment-issues/section-1c.md) holds three rollbacks that did not hold while
the reply said they had, and nothing checked whether a rollback had also taken the elements the **last kept**
job made - the job on screen truly kept nothing, said so, and the stage before it could be gone without a word.

**What changed.** Every transaction group in Heron - the fragment write path in
[`RevitFragment.cs`](../../revit/Heron.Revit.Addin/RevitFragment.cs) and the move in
[`RevitWrite.cs`](../../revit/Heron.Revit.Addin/RevitWrite.cs) - is now rolled back through one helper,
`RollBackAndCheck`, and assimilated through one, `Keep` - the rollbacks a declined Revit dialog causes
([Group EL](group-el.md)) included, whose refusal now says what the check found beside Revit's status. A permanent `DocumentChanged` listener, subscribed in
`OnStartup`, counts the elements each write job adds while it runs and samples up to 200 by UniqueId. When a job
is **kept**, that sample is kept under the model's identity strings (CreationGUID from 2024, path, title - never
the Document). When the next write job **starts**, the sample is narrowed to what still resolves, so what the
modeller deleted or undid in between is never blamed on Heron. After that job's **rollback**, anything gone from
the narrowed sample is said once - *"Rolling back this run removed X of N elements the last kept job made
('NAME') - rebuild that stage"* - and the sample is forgotten. And the job's own additions are counted back as a
second witness beside Revit's status: any still standing and the reply says **THE ROLLBACK DID NOT HOLD**, and
`rolledBack` on the reply is false. A Ctrl+Z of the kept job's own undo entry, seen by the listener as
`TransactionUndone` with the name `Heron: NAME`, is reported at the next job's start as an undo, not a loss.

The bookkeeping and every sentence are in
[`HeronRollbackWitness.cs`](../../revit/Heron.Revit.Addin/HeronRollbackWitness.cs), which names no Autodesk type;
[`tests/test_rollback_witness.py`](../../tests/test_rollback_witness.py) runs it in
`tests/Heron.RollbackWitness.TestHost` and reads every rollback site as text. **What nobody has seen is Revit
reporting what the listener relies on**, and every row below is about that.

**What is compiled and what is not run.** The add-in and the test host build for all eight releases 2020 to
2027 (`tools/check-compile.py`, 2026-10-17); the members used - `UndoOperation`'s five values,
`DocumentChangedEventArgs.Operation`, `GetAddedElementIds`, `GetDeletedElementIds`, `GetTransactionNames`,
`TransactionGroup.Assimilate` returning a status, `Document.GetElement(string)` - were read out of all eight
releases' RevitAPI.dll with `tools/api-surface --members`. **None of it has run in Revit.** It works only once the
add-in is rebuilt, deployed for each Revit and Revit restarted.

**A NEW SCRATCH PROJECT, never a working model.** Make it before EO1, in Revit 2024 first: a project from the
default metric template saved as *Heron Rollback Check.rvt*, a floor plan of Level 1 open. Each write below goes
through the chat's `revit_change`, or the same add-in path from the bridge client:
`python mcp/client/heron_bridge_client.py fragment --in "Heron Rollback Check" create-wall --write [--apply]
--set level=Level 1 --set "wallType=Generic - 200mm" --set height=3000 --set structural=false --set points=...`,
three plan points in the units the card states. Read `verdict`, `rolledBack`, `rollbackReported` and
`rollbackCheck` off each reply.

| # | Run | Look for |
|---|---|---|
| **EO1** | **Keep A, then preview B and roll it back.** A: `CREATE_WALL` with **apply**, three points - two walls, KEPT. B: `CREATE_WALL` again, no apply, three other points | B's reply: `rolledBack` true, `rollbackReported` true, `rollbackCheck` `added` 2, `checked` 2, `standing` 0, `keptJob` "create-wall", `keptChecked` 2, `keptLost` 0, and the verdict says NOTHING WAS KEPT with *"none of the 2 elements this run added is still in the model"* and **no** "Rolling back this run removed". A's two walls still there, B's gone. **The check stays silent** - the D-30 positive |
| **EO2** | **Ctrl+Z on A, then B.** Keep A as in EO1, press Ctrl+Z once in Revit (the undo entry reads *Heron: create-wall*), then preview B | B's verdict ends with *"The last kept job ('create-wall') was undone in Revit before this run started, so this run did not remove what it made."* and **never** *"Rolling back this run removed"*; `keptLost` 0. **Reported as undone by the user.** If it says *"What the last kept job ('create-wall') made was already gone when this run started"* instead, the listener did not match the undo - the name `GetTransactionNames()` reports for an assimilated group is not *Heron: create-wall*; write down what it is |
| **EO3** | **Whether `DocumentChanged` reports the group's commits and its rollback with the right ids.** EO1's B, read off the reply | `rollbackCheck.added` equal to the walls B built (2) - **0 means the listener never saw the inner commits inside the open group**, and every other row here is then silent for the wrong reason. `eventNamed` 2: Revit's `TransactionGroupRolledBack` event named both walls as removed. **-1 means that event never reached the job** (raised after the request ended, or not at all); 0 means it came with other ids. Neither decides anything today - write down which it is |
| **EO4** | **The negative: B deletes A's walls, and is rolled back.** Keep A, select its two walls, `DELETE_ELEMENTS` without apply | `keptLost` 0, no "Rolling back this run removed", both walls back in the model - the rollback restored them, and the check says so by saying nothing. **The D-30 negative**: a loss that a rollback DOES undo is not reported |
| **EO5** | **The modeller's own delete is not Heron's.** Keep A, delete ONE of its walls by hand, then preview B | Silent, `keptChecked` 1, `keptLost` 0. Then delete the other by hand and preview B again: the verdict ends with *"What the last kept job ('create-wall') made was already gone when this run started - removed in Revit, not by this run."* once, and the next preview says nothing |
| **EO6** | **The move.** Keep A, then `preview_move` / `move_elements` on Walls with Revit refusing (a pinned wall in a workshared copy, or whatever makes `transaction.Commit()` fail) | The `move_failed` message is the one it always was, with no loss sentence; A's walls still there. A move that SUCCEEDS leaves A watched - a following EO1 B still reports `keptJob` "create-wall" |
| **EO7** | EO1 and EO2 once on Revit 2020 and once on 2027 | The same answers. 2020 has no CreationGUID, so the model is keyed by path and title alone - a Save As between A and B must leave B silent (a new key, so nothing is watched), never a false loss |
| **EO8** | *Not a proof - an observation.* Ctrl+Y after EO2's Ctrl+Z, then preview B | Silent, `keptChecked` 2 - the redo was seen and A is watched again. If B says A was undone, the listener did not see `TransactionRedone` with the same name |

**What no row here can arrange: a real loss.** The sentence *"Rolling back this run removed X of N elements the
last kept job made"* exists for section 1c's kind of failure, and nobody knows how to cause one on purpose - the
three on record came from a dialog-raising transfer, a cascading delete and a plain rename. The host proves what
Heron says when one happens; only the next one, met in a real model, proves Revit can produce one for it to see.
