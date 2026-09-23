# Fragment issues — section 1c

> One section of [the register](../FRAGMENT-ISSUES.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## 1c. A WRITE INTERRUPTED BY A DIALOG DOES NOT FULLY ROLL BACK — 2026-09-08

**The most important thing found today, and the least expected.**

`transfer-views-between-documents` was run with `--write` and **no** `apply`, so the whole run should
have been undone. It raised a stream of Revit "Duplicate Types" dialogs, the owner answered OK to each,
and the run timed out at 60 s (`still_running`, then `revit_busy`).

**Afterwards the model held 60 more placed elements than it started with — 9,628 → 9,688.**

What was checked, and is clean: levels 11 (unchanged), worksets 2 with `Workset1` still named that,
drafting views 2 (unchanged), legends 0, **nothing named HERON anywhere**. Nothing any fragment
*created by name* survived. The 60 came in with the paste itself.

### Why this matters more than the count

The rollback is the whole basis of proving writes safely — every write proof recorded today says *"run
inside a transaction and ROLLED BACK, so the model was left exactly as it was."* **That sentence is
true for the 20-odd fragments that completed, and was not true here.** A guarantee with an unstated
exception is worse than a guarantee that is qualified.

### What is NOT known, and must not be guessed

- whether the `TransactionGroup` rolled back at all, or rolled back and the paste sat outside it
  (`Document.Paste`-style operations can commit their own transaction)
- whether the timeout is involved, or only the dialog
- whether the same happens with `transfer-materials-between-documents`, which failed identically
- **whether any completed proof from today is affected.** The element count was checked after the
  earlier batch and matched, so probably not — but "probably" is not the standard this file uses

### IT HAPPENED AGAIN, MUCH LARGER — 2026-09-09

`delete-elements` was run with `--write` and **no `apply`**. Its negative case selected `Levels`, and
this is what the record holds:

```
askedFor  11        the eleven levels
deleted   5636
alsoWent  5625      everything hosted on them, and their views
```

Afterwards: **9,628 → 3,966 placed elements, and every floor plan gone.** The rollback did not undo it.

**THE FRAGMENT DID NOTHING WRONG.** `alsoWent` exists to report exactly this and reported it. The
choice of Levels as a "harmless" contrast was mine, made without thinking about what a level carries in
Revit — and the fragment's own output is what said so.

**Two failures at this point, and the shape looked the same both times:** a large operation (60 elements,
then 5,636) run with no `apply`, and the model left changed. It was believed then that small writes roll
back correctly — checked after every batch that day, with the `create-level` proof verified
element-by-element. **A third incident on 2026-09-09 disproved that**, below: seventeen sheets, a rename,
nothing created, and the rollback still did not hold. Whether the boundary is size, cascade depth, or
Revit committing something of its own is **not known** — but it is now known that it is NOT size.

Until it is: **run write proofs on a model you are willing to throw away, and check the element count
after every batch.** Both incidents were caught by that check and nothing else.

### AND A THIRD TIME, ON A WRITE THAT WAS NOT LARGE AT ALL — 2026-09-09

**Seventeen sheets. A rename. Nothing created, nothing deleted, no cascade, no dialog.** The rollback
still did not hold, and this is the incident that says the boundary is not size.

`edit-text-values` was proved through `tools/jobs/refusal-paths.yaml` with `--write` and **no `apply`**.
Its positive put a `HERON ` prefix on `Sheet Name` across all 17 sheets and reported `changed 17`,
`blank 0`, `untouched 0` — a clean, correct, small write. The transaction was rolled back.

Afterwards `list-sheets` still read:

```
M000  HERON Cover Sheet
M001  HERON Learn about this project
M002  HERON Notes, Symbols & Schedules
```

**The element count did not move — 9,628 before and after — and that is the trap.** Both earlier
incidents were caught by counting elements, and this one is invisible to that check: a rename creates
nothing. The count was read, came back correct, and the model was still wrong. It was found only by
re-reading the values that had been written.

> **Checking the element count is not enough.** It catches a write that CREATES or DELETES. A write that
> EDITS — a rename, a parameter, a type change — passes that check while still standing in the model.
> After a write proof, re-read the thing that was written.

Sheet NUMBERS were untouched, so nothing became ambiguous. **The disk file was NOT verified the way the
2026-09-08 incident was** — that one was closed without saving and reopened to a confirmed 9,628. Here
the session reported *unsaved changes* and nothing was saved, so the damage should be confined to the
live session on the same reasoning as before — but it is an inference, and it is written down as one
rather than as a check that was made. Recovery is the known one: **close without saving**.

**What this rules out.** Not size (17), not cascade (a name has no dependents), not a dialog (§1b —
none was raised), not deletion. What the three incidents still share is only `--write` with no `apply`
— which is to say, the rollback path itself, and nothing about what was asked of it.

#### And the rollback path could not have told anyone — found by reading it, 2026-09-09

The mechanism was called *not known* three times in this section. Reading `RevitFragment.cs` after the
third incident gives a concrete candidate, and it is not exotic:

```csharp
private static void SafeRollBack(TransactionGroup group)
{
    try
    {
        if (group.GetStatus() == TransactionStatus.Started) group.RollBack();
    }
    catch { }
}
```

**The inner transaction is COMMITTED first**, and the undo depends entirely on the outer group being
rolled back afterwards. That call has two silent exits: a group whose status is not `Started` **skips
the rollback**, and a `RollBack()` that throws is **swallowed**. The method returned `void`, so neither
reached a caller, and `WithVerdict` then reported *“NOTHING WAS KEPT”* because `apply` was false.

> **A failed rollback and a clean one produced byte-identical output.** That is why three incidents have
> no explanation: nothing was ever in a position to notice one, let alone report it.

This is not proof that it is what happened on any of the three days — that needs the instrumented
build in front of a model. It is the reason the failures were INVISIBLE, which is a different claim and
a checkable one. `SafeRollBack` now returns Revit's status after the attempt and the answer carries
`rolledBack` (§5 row 10), so the next occurrence says so on the reply instead of being reconstructed
from renamed sheets a day later.

### `delete-elements` IS BLOCKED, not merely untested — 2026-09-09

Three attempts, three different failures, and **the fragment was correct every time**:

| Negative case chosen | What happened |
|---|---|
| `Levels` (11) | `alsoWent 5625` — 5,636 gone, rollback failed, 9,628 → 3,966 |
| `Duct Systems` (148) | Revit stopped answering. Forced close |
| — | (no third choice attempted) |

**In an MEP model almost everything is hosted on, or belongs to, something else.** A level carries its
views; a duct system carries its ducts. There is no category here that can be deleted in isolation, so
there is no safe negative case to find — the problem is not that the right one has not been picked yet.

It stays `DRAFT` and should not be attempted again **until the rollback is understood**. Running it is
how both of today's model wipes happened, and the second one hung Revit hard enough to need a forced
close.

`alsoWent` is worth keeping in mind for its own sake: the fragment reports the cascade in the same
answer, and both times it was right and was read too late.

### It never reached disk, and that is checked rather than hoped

The model was closed **without saving** and reopened: **9,628 placed elements**, exactly what it held
this morning. So the failure is confined to the live session — a rollback that does not undo everything,
not a file that ends up wrong. That is the difference between a bug and a data-loss bug, and it is worth
stating plainly next to the finding rather than leaving the reader to fear the worse one.

It also means the recovery is known and cheap: **close without saving**.

### Until it is understood

Run the two dialog-raising transfers **only on a model you are willing to throw away**, and check the
element count afterwards. The right fix is the one in §1b — collide-detect first, so no paste starts
that Revit has to interrupt — and it removes this failure as a side effect.

---
