<!-- Heron-Agent:  none -->
<!-- Heron-Step:   17 -->
<!-- Heron-Status: DRAFT -->
<!-- Heron-Since:  0.1.0 -->
<!-- Heron-Layer:  brain -->
<!-- See docs/29-metadata-standard.md -->

# Fragments with something wrong — the sit-down list

> | | |
> |---|---|
> | **Type** | **Permanent register.** Append-only, and **never deleted** — this is where work notes empty into |
> | **For** | Anyone proving fragments — **this file is the queue** |
> | **Authority** | [DECISIONS](DECISIONS.md) and the [Golden Rules](14-golden-rules.md) win. A row here records what was seen, on the day it was seen |
> | **Waiting on you?** | `python tools/owner-queue.py` — **never a list typed on this page** |
> | **Adding to it** | One row per fragment or defect, with **the model it was seen on named**. A defect found while tidying is **recorded, not fixed** |
> | **Its numbers** | Counts inside a row describe **the day it was written** and are deliberately not updated. Derive today's: `grep -rh '^heron-status:' brain/fragments/*/fragment.yaml | sort | uniq -c` |

> **Read [FOR-THE-OWNER.md](FOR-THE-OWNER.md) first if you are the owner.** It is the one page that
> says what is waiting on you, across every register, without holding a list of its own.


**What this is.** Every fragment that was PUT IN FRONT OF A REAL MODEL and did not come away proved,
with the reason. Opened 2026-09-08 at the owner's request: *"we will sit for this specially, that
issued one we can do together."*

**What this is NOT.** It is not the list of unproven fragments — that is 301 and most of them have
simply not been tried yet. Everything here has been RUN. A fragment earns a row by failing, refusing,
or passing in a way that proves nothing.

**How to use it.** Read `Status` first. `NEEDS THE OWNER` means the model has to be arranged by hand
and nothing else will do. `SUSPECT` means it did something to Revit that has not been explained and it
should not be run again casually.

Derive the counts rather than trusting any typed here:

```bash
grep -h '^heron-status:' brain/fragments/*/fragment.yaml | sort | uniq -c
```

---

## 1. SUSPECT — these upset Revit, and why is not known

Both failed **cleanly** — `transaction.Commit()` did not return `Committed`, the write path rolled back,
and the element count afterwards was unchanged. Revit then raised its own dialog:

> *"A serious error has occurred. It is strongly recommended that you use Save As to save your work in
> a new file before continuing."*

The model was not damaged and nothing was saved. **Neither has been run since.** They are first on the
list to look at together.

| Fragment | Values used | What came back |
|---|---|---|
| `create-mep-system-type` | `copyFromName=Supply Air`, `newName=HERON TEST SYS`, `abbreviation=HTS` | `operation_failed` — "ran but Revit did not accept the change" |
| `assign-scope-box-to-view` | `views=Model Linking`, `scopeBoxName=Grids` | `operation_failed` — same |

**How to look at them safely:** one at a time, on a freshly opened model, with nothing else run in that
session — so that if Revit complains again it is unambiguous which one did it. Run READ-only first
(`fragment <name>` with no `--write`) to see how far the C# gets before the transaction matters.

---

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

## 1b. NEEDS A HUMAN AT THE KEYBOARD — Revit opens a dialog Heron cannot answer

| Fragment | What happened |
|---|---|
| `transfer-materials-between-documents` | Copying materials from `Snowdon Towers Sample Architectural` into the scratch model, Revit raised **"Duplicate Types — Material Assets : Steel"** and waited. The run returned `still_running` after 60 s, and the next request was refused with `revit_busy`. The owner pressed OK and Revit carried on |
| `transfer-views-between-documents` | The same, `viewKind=drafting`: **"Duplicate Types — Callout Tag, Drafting View, Section Tag, Viewport"**. Also `still_running` then `revit_busy`. **Found after the row below was written**, which had said only one fragment did this — so the split is 2 against 3, not 1 against 3 |

**THE BRIDGE DID EXACTLY THE RIGHT THING and that half is a pass.** Both messages were the ones
[docs/03](03-heron-revit.md) asks for — *"Revit started the request but has not finished within 60
seconds. It is still working; do not repeat the request"*, then *"A dialog may be open... Finish what is
open in Revit and ask again"*. No hang, no wrong answer, and it named the cure.

**What cannot be fixed by trying harder:** a transfer that collides with an existing type stops and
waits for a person, every time. Heron cannot dismiss a Revit dialog and must not learn to — the dialog
is Revit asking a question only the modeller can answer, and the two choices produce different models.

**THREE OF THE FIVE ALREADY DO THE RIGHT THING, which settles the argument.** Proved the same
afternoon, against the same two models, with no dialog at all:

| Fragment | Clashes it met | What it did |
|---|---|---|
| `transfer-view-filters-between-documents` | 1 — *"Interior (already here)"* | Copied 54, **reported** the clash |
| `transfer-line-styles-between-documents` | 21 | Created 8, **reported** the 21 |
| `transfer-object-styles-between-documents` | — | Created 73, changed 124, weakened 15, skipped 1 |

So the fix for `transfer-materials-between-documents` **and `transfer-views-between-documents`** is not
a new idea to invent — it is the shape the other three already use: **look for the collision first,
decide it in the fragment, and report it**, rather than starting a paste Revit has to interrupt with a
question. Five fragments, one family, two of them written the other way.

---

## 1d. A ROLLED-BACK WRITE CLEARS THE SELECTION — and that blocks 100 proofs

Established by running it, 2026-09-08:

| Step | Selection afterwards |
|---|---|
| `select-by-category-name` chained into `set-selection` | **307** |
| a READ fragment | 307 |
| the same READ fragment again | 307 |
| **a WRITE fragment with no `apply`** — so nothing was kept | **gone** — `needs_unbound` |

Reads leave it alone. The rollback takes it with them.

### Why this is not a small thing

**100 fragments need a selection AND a caller value** — that combination is the whole remaining
population of provable work, and the negative case for every one of them is a second run with a
different value. `validate` runs both phases in one go, so the second phase always arrives at an empty
selection and refuses. `create-selection-filter` proved its positive this afternoon and could not reach
its negative for exactly this reason.

**It is arguably correct behaviour.** The selected elements were deleted and recreated by the rollback,
so the ids Revit was holding no longer exist; clearing is safer than pointing at ghosts. But correct or
not, it means no selection-based write fragment can be proved by the current tooling.

### …and `--setup` cannot run a step that WRITES — found 2026-09-09

Setup steps go through `run_fragment_read`, which opens no transaction, so a `MODIFY` fragment used as
setup throws *"Attempt to modify the model outside of transaction"*.

`show-elements` is the fragment that needs it: to prove it, something must be hidden **and stay
hidden**, so its arrangement is `hide-elements` — a write. And a write in the setup would have to be
APPLIED to survive into the phase, which is the one thing the whole rollback design exists to avoid.

**A genuine chicken-and-egg, worth deciding rather than patching:** either the setup gets its own
applied-then-undone bracket around both phases, or fragments that need a written arrangement are proved
by TRACKING instead. It is the same shape as `update-saved-set`, which needs a saved set that another
fragment creates and rolls back.

### FIXED, 2026-09-08 — `validate --setup`

`--setup <fragment>` names a fragment to run before **each** phase, repeatable and in order. It runs
with the POSITIVE values always, because it is the arrangement rather than the question: *"these
elements, selected"* is what both phases are asked about, and only the question changes.

```
validate create-selection-filter --write   --setup select-by-category-name --setup set-selection   --set categoryName=Ducts --set "inViewOnly=FloorPlan: L3"   --set filterName="HERON SEL Z9"   --negative-set filterName=Domestic ...
```

**`create-selection-filter` was the first selection-based WRITE ever proved** — 307 elements saved to a
new filter, 0 when the name was already taken. `assign-location-data` followed.

One thing had to be got right and was got wrong first: **only the first setup step resets the chain.**
The whole point is that step two consumes what step one left — `select-by-category-name` leaves
`elements`, `set-selection` needs them — and resetting between them threw that away. The symptom read
as a missing selection rather than a discarded one.

### The reasoning that led there, kept because it generalises

`validate` needs to re-establish the arrangement before EACH phase, not once at the start — the same
way it already sends different caller values per phase. A `--setup` chain naming fragments to run first
(`select-by-category-name`, `set-selection`) would do it, and the machinery already exists: `prove` runs
a chain in one lease, and D-29's chaining carried `elements` between those two fragments correctly on
the first try.

Until then, a selection-based write fragment can be RUN once and its positive recorded, but not proved.

---

## 2. CANNOT PROVE — Revit itself declines

None of these looks like a fragment defect. Each asked Revit to do something and Revit said no, for a
reason that reads as correct. **They stay `DRAFT` because a fragment that will not act cannot be shown
to act** — the positive case and the negative case come back identical, and D-30 exists to catch
exactly that.

| Fragment | What Revit said | Worth trying |
|---|---|---|
| `delete-revision` | *"Revit would not delete revision 1 — a revision cloud still uses it"* | A revision with **no** cloud on it. There is only one revision in Snowdon |
| `edit-revision` | *"Nothing changed on revision 1"* | Why. The values passed differed from what was there |
| `remove-view-template` | *"'L2' — Revit refused to detach 'Mechanical Plan': A managed …"* (message truncated in the record) | Read the full message. It may be the workshared model, or a template that is in use |

---

## 3. NEEDS THE OWNER — the model has to be arranged by hand

These ran correctly and gave one honest half of a proof. The other half does not exist in
`Snowdon Towers Sample HVAC` and cannot be conjured by any input.

| Fragment | Has | Missing | The arrangement |
|---|---|---|---|
| `read-graphic-overrides` | The empty answer — 0 of 625 elements | A positive | Select ducts → Override Graphics in View → By Element → red |
| `diagnose-visibility` | 625 visible, 0 reasons | Something invisible | Select one element → HH |
| `find-unused-materials` | 56 found | A model with none | A second, clean model — or accept tracking |
| `find-unused-families` | 64 found | A model with none | Same |

---

## 3b. NO POSITIVE CASE IN THIS MODEL — found 2026-09-08, second round

Ran correctly and returned the honest empty answer. The model simply has none of the thing.

| Fragment | What was tried | What came back |
|---|---|---|
| `select-openings` | `inViewOnly=FloorPlan: L3` | `elements 0` — there are no openings in that view. Needs a view with a wall or floor opening in it, or one drawn on purpose |
| `select-from-saved-set` | `filterName=Domestic`, `view=FloorPlan: L2` | `elements 0` — *"'Domestic' is a RULE. It matches 0 element(s) in view"*. Consistent with `audit-view-filters`, which found all four filters on the Mechanical Plan template are switched off AND carry an empty override. **The filters in this model do nothing**, so nothing here can prove a fragment that reads them |
| `report-areas` | `schemeNameContains=` (everything) | `areas 0` — the model has no Area scheme with placed areas. The negative returned 0 too, so the two cases are identical and nothing separates working from doing nothing |
| `check-ceiling-coordination` | 307 ducts in L3, `tolerance=99999` | `outOfPlane 0`. A tolerance nothing can satisfy still found nothing, because the ceilings are in the architectural LINK and the executor skips linked documents by design. Needs a ceiling drawn in the host |
| `check-fixture-connectivity` | air terminals in M1, three services required | `missingService 0`. Demanding Supply, Return AND Exhaust of every terminal still found none incomplete. Needs a terminal with a service genuinely absent |
| `find-overlapping-lines` | 307 ducts, `toleranceMm=99999` | `overlapping 0`. **Not a tolerance defect — it works on model LINES, and a duct is not one.** Needs detail or model lines, which this selection never contained |
| `find-dead-ends` | 307 ducts, `stubLength=99999` | `deadEnds 0`, confirming the earlier run rather than resting on it. `select-by-connection-status` proved every duct end in this model is connected, so there is nothing to find at any stub length. **Confirmed a THIRD time 2026-09-10 with `stubLength=10`, and this run names the mechanism rather than the outcome: `openEndsFound 0` across all 307.** The fragment filters open ends that are meant to be there - a run reaching a terminal, a fixture or a cap - but here there were no open ends AT ALL to filter, so the empty answer is settled at the source. **STOP RE-RUNNING THIS ONE.** Three runs at three stub lengths have now agreed, and the row said so before the third. **This still stands FOR THIS MODEL — but the fragment is PROVEN, on `Project1` which has four open duct ends. The row named what it needed and a different model had it: §3b-ii** |
| `select-subcomponents` | 10 Mechanical Equipment (Heat Recovery Units) in L3, `recursive=true` | `elements 0`, and the accounting proves it LOOKED rather than skipped: `withoutSubComponents 10` and *"0 nested element(s) found under 10 parent(s), followed 0 levels"*. A nested shared family lives inside a loadable family instance, and none of this model's equipment has one. **Its NEGATIVE is already perfect and is worth keeping** - 22 Ducts return `withoutSubComponents 0` with *"22 of what was given"* excluded, because ducts are SYSTEM families and structurally cannot nest anything in any model, which is a stronger negative than a count that a richer model could overturn. Needs one piece of equipment with a nested shared family, or a multi-component fixture |

---

## 3b-i. THE `POSITIVE EMPTY` POOL IS EXHAUSTED FOR THIS MODEL — triaged 2026-09-10

**`batch-prove.judge` reports `POSITIVE EMPTY` for 41 DRAFT fragments, and that verdict reads like a
worklist: it means the ARRANGEMENT was wrong, not the fragment.** It was treated as one, and it is
not. Triaged rather than retried:

| | |
|---|---|
| POSITIVE EMPTY, still DRAFT | **41** |
| already explained in §1, §1b, §1c, §1d, §2, §3, §3b or §3c | **34** |
| left over | **7** |

**AND EVERY ONE OF THE SEVEN IS BLOCKED BY SOMETHING ALREADY KNOWN, not by a bad arrangement:**

| Fragment | What actually blocks it |
|---|---|
| `select-in-region` | **§5 row 14** — it converts millimetres to feet a second time, so every volume is 304.8x too small. A units defect, and it must not be re-run until that is fixed |
| `set-element-workset` | **The `LIST_*` gap.** It takes `worksetId` as an `int`, which IS receivable — but `list-worksets` provides only `findings`, `worksetCount`, `closedCount` and `workshared`. **The model is workshared with 2 worksets and their ids cannot be learned**, and typing a guess is how a job runs against the wrong thing and reports success |
| `place-views-on-sheet` | The same gap from the other end. 17 sheets exist, so `sheetNumber` is easy; `elements` must be the VIEWS to place, and nothing in the library can put views into a selection |
| `dimension-rooms`, `report-door-room-links` | Both need Rooms. Snowdon is an HVAC model and carries **Spaces**, not Rooms — `FloorPlan: M1` holds 5 Spaces and 0 Rooms |
| `reset-graphic-overrides` | Needs elements that already carry an override. `read-graphic-overrides` sits in §3 for the same reason: nothing in this model has one |
| `place-flow-arrows` | Needs `arrowFamilyName` and `arrowTypeName` — a specific annotation family that has to be loaded first |

**WHAT THIS MEANS FOR THE NEXT SESSION, and it is the useful part.** Further proving against
Snowdon-scratch is not blocked on effort or on arrangements. It is blocked on three things, and each
is a decision rather than a batch: **build the `LIST_*` fragments** (§3i names this the binding
constraint), **fix §5 row 14**, or **build the content on purpose in a scratch model** the way the
first eight write proofs were taken on 2026-09-10.

**Read this table before picking a fragment out of a failure list.** `find-dead-ends` was re-run on
2026-09-10 against a row in §3b that had already recorded, on 2026-09-08, why it can never pass here.

---

## 3b-ii. A SECOND MODEL CLEARS A BLOCK THAT WAS NEVER ABOUT THE FRAGMENT — 2026-09-10

Every row above says *"in this model"*, and it was easy to read them as *"this fragment cannot be
proved"*. One of them was tested against a different model and fell immediately.

**`find-dead-ends` is PROVEN.** It was set aside three times on Snowdon, and §3b ends with **STOP
RE-RUNNING THIS ONE** — which was the correct instruction and is still correct *for Snowdon*. The row
also said exactly what would lift it: *"Needs a duct run with a loose end."* `Project1 work_ajmal.al`
was drawn by hand with five ducts, four of them left open:

| Measured 2026-09-10, `select-by-connection-status` `wantOpenEnds=true` | |
|---|---|
| Ducts | **4 of 5** have at least one OPEN end |
| Duct Fittings | 0 of 2, and `withoutConnectors 0` |

Positive `deadEnds 4`, negative `deadEnds 0` and `stubs 0`. Judged **PASS**.

> A blocked row names a MODEL, not a fragment. Before writing one off, read what the row says it
> needs and ask whether some other model has it.

### The view was not called what everyone assumed, and it cost a session

`Project1` is an MEP template. Its **levels** are `Level 1` and `Level 2`; its **plan views** are
`1 - Mech`, `2 - Mech`, `1 - Plumbing` and `2 - Plumbing`. Earlier probes asked for `Level 1` and
`FloorPlan: Level 1`, got nothing back, and concluded the model *"has no floor plan view at all"* —
which is written into [`creators-round-two.yaml`](../tools/jobs/creators-round-two.yaml). It has four.

The level name and the view name are different things, and in every delivered template they differ.
`find-views` with `viewType=FloorPlan` answers this in one call and no probe needs to guess again.

### What this model blocks, and it is a different list from Snowdon's

| Fragment | What was tried | What came back |
|---|---|---|
| `report-door-room-links` | 1 door, phase `New Construction` against phase `Existing` | **POSITIVE EMPTY.** The pair is honest and shows the purpose's own phase-dependence claim — the same door reads `(outside) -> Room 1` on New Construction and `(outside) -> (outside)` on Existing. But the only `role: result` field is `disagreements`, and it is 0 in both legs. A disagreement needs Revit's `FromRoom`/`ToRoom` to contradict where the rooms physically are — a real modelling fault, which a clean four-wall model does not have and which cannot honestly be manufactured. Needs a model with a genuinely mis-facing door |
| `check-flow-direction` | 5 ducts in `1 - Mech` | `jointsChecked 0`, `bidirectionalSkipped 4`. The ducts are drawn but carry no flow — no system, no equipment, nothing to set a direction. Needs ducts on a real system |
| `find-overlapping-lines` | 5 ducts, then 4 walls, `toleranceMm=99999` | `overlapping 0`, `notStraight 0`, starts and ends read for every element. It looked and there is genuinely nothing stacked. Needs two lines drawn on top of each other — **and note §3b blamed the category for this on Snowdon; here the category was varied and the answer did not change** |
| `find-nearest-elements` | 5 ducts, `metric=distance` | Refused before running: *"this fragment needs 2 separate sets of elements and one selection cannot say which is which… Running anyway would report 0 results, which reads as 'there was nothing to find' rather than 'nobody was asked'."* The two-set gap of §5, refusing honestly. Nothing about this model can fix it |

---

## 3b-iii. THE SNOWDON ROUND THAT WAS WRITTEN AND NEVER RUN — 2026-09-10

`rerun-stale-passes.yaml` and `read-recoveries.yaml` were both written earlier the same night and
neither was ever run — housekeeping interrupted, and all eight fragments involved were still `DRAFT`
hours later. Running them cost one command each.

**The four stale re-runs all passed.** Their earlier records were same-day and therefore stale by this
repository's own rule ([`heron_validate.implementation_changed_after`](../brain/heron_validate.py)), so
signing them on that evidence would have been signing a record the rule already distrusted:

| Fragment | Positive |
|---|---|
| `read-space-loads` | `noLoad 5` |
| `check-ceiling-coordination` | `noCeilingAbove 307` |
| `check-fixture-connectivity` | `noConnectors 10` |
| `audit-mep-openings` | `combined 9` |

> The staleness rule did its job. Four fragments that *looked* signable at 19:00 needed a fresh run,
> got one, and passed on evidence that is now current.

### `check-flow-direction` — the negative is the same category, elsewhere in the same building

The only DRAFT read fragment this model could still feed, and the pair is the strongest shape available
to a fragment with no value able to switch its own answer off:

| View | Ducts | `jointsChecked` | `bothOut` | `bothIn` |
|---|---|---|---|---|
| `FloorPlan: L3` | 307 | 165 | **7** | 0 |
| `FloorPlan: M1` | 22 | 19 | 0 | 0 |

`jointsChecked` is `role: accounting` so it cannot make the negative read non-empty — but **19 is the
evidence the fragment looked** in that leg rather than being handed something it could not read. That
is the whole difference between an honest empty answer and a missing arrangement (§1d), and it is why
Duct Tags were **not** used: a tag has no connectors, so its empty answer would have been about the
arrangement, not the model.

The positive is a real finding — seven joints with both connectors flowing OUT, reported as pairs
(`1431100 | 1510229`) so each can be selected in Revit and looked at.

**Project1 could not have proved it.** Five hand-drawn ducts, `jointsChecked 0`,
`bidirectionalSkipped 4`: drawn but carrying no system, so there is no flow to check. The two models
block opposite things, which is the argument for keeping both.

### What Snowdon still cannot feed, and it is a different list from Project1's

**Snowdon is an HVAC model and its architecture is in a LINK.** Probed 2026-09-10 in both
`FloorPlan: M1` and `FloorPlan: L3` — **Doors 0, Rooms 0, Lines 0, Detail Items 0.** The executor skips
linked documents by design, the same fact that blocked `check-ceiling-coordination` in §3b.

| Fragment | Blocked on Project1 because | Blocked on Snowdon because |
|---|---|---|
| `report-door-room-links` | one door, and it is not mis-facing — the only `role: result` field needs a genuine fault | no host doors at all; they are in the architectural link |
| `find-overlapping-lines` | nothing is drawn on top of anything (`overlapping 0` at 99999 mm) | no model or detail lines in the host |

`select-subcomponents` came back **POSITIVE EMPTY a second time**, exactly as
[`read-recoveries.yaml`](../tools/jobs/read-recoveries.yaml) predicted in its own comment. Its negative
is still the best in the library — ducts are SYSTEM families and structurally cannot nest anything in
any model — but the positive needs one piece of equipment with a nested shared family, and neither
model has one. **Two runs have now agreed. Do not run it a third time on either model.**

### The WRITE pool on this model, triaged — 2026-09-10

`write.enabled` was switched on and five MODIFY fragments were run for real, inside the rolled-back
TransactionGroup. **The model was 9,628 elements before and after**, so §1c's rollback held across all
of them — including a `group-elements` run that really did group 22 ducts.

Four are blocked, and each one says exactly what it needs:

| Fragment | What came back | What it needs |
|---|---|---|
| `flip-elements` | `cannotFlip 144` on air terminals, `cannotFlip 10` on mechanical equipment, `notFamilyInstance 0` both times — so it read every one and none has a flip control | An instance that can actually flip. Snowdon's host has no doors or windows; they are in the architectural link |
| `set-mep-slope` | `sloped 0`, and the accounting explains it completely: `bothEndsConnected 20` + `risers 2` = the whole selection of 22 | A duct with a FREE end. **`Project1` has four** — the same open ends that proved `find-dead-ends`. This is the clearest "wrong model, right fragment" case since that one. **Done — PASS on `Project1`, `sloped 4`, below** |
| `disallow-join` | not run | Host walls, and there are none in `FloorPlan: M1` or `L3` |
| `group-elements` | `grouped 44`, `groupId 1`, `refused false` | A negative. It groups whatever it is handed — the shape [`value-driven-negatives.yaml`](../tools/jobs/value-driven-negatives.yaml) already names, and no value it takes can switch that off |

**`set-mep-slope` is worth reading rather than filing.** It refused all 22 and gave a per-element reason
for every one, splitting them into two named causes that add up exactly. That is the behaviour §3h.1
("make silence illegal") is asking every fragment for, already built.

### …and `set-mep-slope` then PASSED on Project1, exactly as the row predicted

The prediction above was written before the run and held: Snowdon refused all 22, `Project1`'s four
free ends took the fall.

| `slopeRatio` | `sloped` | `findings` | `refused` |
|---|---|---|---|
| 100 — *1 in 100* | **4** | 4 | 0 |
| 1 — *1 in 1* | 0 | 0 | 4 |

**The positive is arithmetic anyone can check**, which is what a proof is for:

```
925641  - run 13650 mm, end moved 136 mm          13650 / 100 = 136.5
```

The negative moves only the ratio. A 45° fall wants to move the same run's end 13650 mm, and
`maxEndMoveMm` refuses it — the fragment's own stated safety behaviour, and it turns **both** declared
results off at once. Contrast `find-dead-ends`, where the value only re-labelled findings between two
result fields and emptied neither. `bothEndsConnected 1` is identical in both legs, which is the
evidence it examined the same five ducts each time.

3,471 elements before and after, on a fragment whose implementation opens with *"THIS MOVES REAL
GEOMETRY. IT IS NOT A COSMETIC CHANGE."*

#### `slopeRatio` is the X in "1 in X", and reading it as a gradient nearly filed a false defect

A bigger number is a **shallower** fall — the opposite of how a ratio usually reads — and the
implementation divides by it (`var drop = run / slopeRatio`).

Asking for a 2% fall as `slopeRatio=0.02` means *"1 in 0.02"*, a fall of fifty to one, and the
fragment answered `would move an end 682500 mm`. `slopeRatio=0` then produced **`would move an end ∞
mm`** — which is *"1 in 0"*, and equally correct.

Both readings looked exactly like an inverted-arithmetic bug, and the division was about to be filed
as one. The purpose settles it in its first sentence: *"until the run sits at **1 in X**."*

> A number that looks wrong is a reason to read the purpose, not to file a defect. The caller was
> wrong twice and the fragment was right twice.

### Two more that Snowdon blocked and Project1 proved — 2026-09-10

The third and fourth *"wrong model, right fragment"* cases of the night, after `find-dead-ends` and
`set-mep-slope`. Snowdon's architecture is in a **link**, and the executor skips linked documents by
design — so Doors 0, Walls 0, Rooms 0 in both working views. Project1 has four walls drawn by hand
with one door in them.

| Fragment | Positive | Negative |
|---|---|---|
| `flip-elements` | `flipped 1`, `cannotFlip 0` | `flipped 0`, `cannotFlip 1` |
| `dimension-wall-openings` | `created 8`, `notAWall 0` | `created 0`, `segmentsDisagree 0`, `notAWall 5` |

`flip-elements` is a true value-driven pair — the same door in both legs, and asking it to flip in *no*
direction turns the only declared result off. On Snowdon it returned `cannotFlip` for all 154 air
terminals and mechanical equipment with `notFamilyInstance 0`: it read every one, and none has a flip
control.

`dimension-wall-openings` has no value that can empty it, so the negative is a second selection — and
it is the shape that already proved `dimension-rooms`, which used Walls against a declared `notARoom`.
Here it is Ducts against a declared `notAWall`. **Being handed the wrong kind of thing is inside the
contract when the fragment declares what it does with it**, and `notAWall 5` is the evidence it
examined all five.

> A triage row that names what a fragment NEEDS is a shopping list. Four fragments have now been
> unblocked by reading one and going to the other model.

### Two more write fragments triaged, both blocked by CONTENT — 2026-09-10

| Fragment | What came back | What it needs |
|---|---|---|
| `rename-elements` | `find=Generic` on 4 walls: `planned 4`, **`collisions 3`**, `renamed 0`. `find=Room` on the room: `planned 1`, **`refused 1`**, `renamed 0` | A selection whose members have **distinct, renameable names**. Project1's four walls share ONE type, so renaming each instance collides with the first — correct behaviour, wrong selection. Revit then refused the room outright |
| `transfer-materials-between-documents` | `Steel` → `clashed 2`, `copied 0`. `Gypsum` → `clashed 1`. `Brick` → `clashed 1`. `Carpet`, `Aluminum` → nothing in either | A material present in the source and **absent** from the target. Both models derive from the standard Autodesk library, so every shared name clashes and nothing else exists to copy |

**`rename-elements` is not failing — it is refusing correctly, twice over.** Four instances of one wall
type all want the same new name, and it reports `collisions 3` rather than renaming one and silently
dropping three. That is the opposite of the §3h.1 problem: a fragment that declines rather than
inventing. Proving it needs a model with distinctly-named renameable elements.

**A second project being open is not enough for the transfer family.** `openTitles` came back with
**eight** documents — the two projects plus six loaded links — so the source resolves fine. The
obstacle is that the two projects are too ALIKE. A transfer proof needs models that differ in the
thing being transferred, which is a sharper requirement than "two are open".

## 3g-iii. A THIRTY-FRAGMENT SWEEP, AND WHAT IT SEGREGATED — 2026-09-10

Every other job file here was written after probing one fragment at a time and knowing the answer
before the run. [`sweep-project1.yaml`](../tools/jobs/sweep-project1.yaml) is the opposite on purpose:
it asked **thirty at once** so the pool could be split in a single pass. 36 write jobs ran for real
against a live model; **3,471 elements before and after.**

| Verdict | Count | What it means |
|---|---|---|
| **PASS** | **5** | `array-elements`, `mirror-elements`, `remove-view-filter`, `edit-parameter-text`, `create-levels` |
| POSITIVE EMPTY | 13 | the positive found nothing |
| **NEG NOT EMPTY** | **9** | **the negative acted anyway — see below** |
| POSITIVE UNREADABLE | 2 | the result is a name or a type, not a quantity |
| DID NOT RUN | 1 | `isolate-elements` threw *"Attempt to modify the model outside of transaction"* — it is `risk: EXECUTE` and still needs the write path |

**A second pass fixed the arrangements and changed nothing.**
[`sweep-project1-pass2.yaml`](../tools/jobs/sweep-project1-pass2.yaml) re-ran six of the POSITIVE
EMPTY with better values — the real working 3D view `{3D - ajmal.al}` instead of `{3D}`, walls instead
of ducts for `snap-to-grid`, `System Type` instead of the empty `Comments` for `copy-parameter-value`,
an underlay looking DOWN a level instead of up. **All six came back POSITIVE EMPTY again.** They are
blocked by this model, not by the job file, and that is now settled rather than assumed.

### Sweep 2 — twenty-one more, 2026-09-11

[`sweep-project1-2.yaml`](../tools/jobs/sweep-project1-2.yaml). **3 PASS**, 10 POSITIVE EMPTY,
4 NEG NOT EMPTY, 2 NO NEGATIVE, 2 DID NOT RUN. Model 3,471 elements before and after.

| Passed | Positive | Negative |
|---|---|---|
| `sum-by-group` | `totals 1 entry`, `counts 1 entry` | empty |
| `rename-family` | `renamed 1 [HERON FAM A]` on `M_Single-Flush` | an empty name is refused |
| `create-room-elevations` | `created 4` elevations round the room | `slotCount 0` → none |

**`sum-by-group` completes the row 11 payoff.** It is the second of the two fragments the `keep-chain`
fix was built for, and it needs a FOUR-step setup — `select-by-category-name`, `set-selection`,
`read-element-parameters`, `measure-run-quantities` — because it takes **two** values from the chain:

```
bound: values from measure-run-quantities (5); quantities from measure-run-quantities (5)
```

Worth a glance when this draft is read: the `bound` line attributes **both** to
`measure-run-quantities`, though `values` is what `read-element-parameters` was put in the chain to
leave. Only the first setup step resets the chain, so later steps accumulate and the line names the
last producer to touch a name. The proof holds either way — both legs bound, positive non-empty,
negative empty — but the attribution is not evidence of which fragment supplied what.

**IT ALSO OVERWROTE A PROOF, AND THE TRADE IS WORTH STATING.** `sum-by-group` already carried a
`proof:` block on `main` — signed by Ajmal PS, still `DRAFT`, and **mis-stamped**: `model: Project1
work_ajmal.al (3,445 elements)` while its positive case ends *"on Snowdon-scratch_ajmal.al"*. The
sweep re-proved it and `accept` replaced that block:

| | old | new |
|---|---|---|
| stamp | `Project1` | `Project1` |
| actually ran on | **`Snowdon`** | `Project1` |
| evidence | `totals 3 entry(ies)`, 22 ducts | `totals 1 entry(ies)`, 5 ducts |

**The richer evidence was the mis-stamped one.** Three groups across 22 ducts says more than one group
across five — but it claimed a model it was not taken against, which is the OPEN defect in §5. A thin
proof that names its own model correctly is worth more than a rich one that does not, and that is why
the replacement was kept rather than reverted.

> `batch-prove` reports `ALREADY` only for `heron-status: PROVEN`. A fragment sitting at `DRAFT` with
> a signed proof already on it is re-run and overwritten in silence. Nothing warns you.

**What sweep 2 removed from the pool, with the reason measured rather than assumed:**

- **NO SHEETS AT ALL** in Project1 (`find-views viewType=DrawingSheet` → 0). That blocks
  `place-views-on-sheet`, `align-viewports-across-sheets`, `create-sheet-list`,
  `set-sheet-title-block` and `export-sheets-to-pdf` at the source. None was run.
- `set-view-crop-to-shape` and `show-analysis-heatmap` **DID NOT RUN** — both need a value no
  selection can supply (`boundary: IList<Curve>`, `values: IDictionary<ElementId,double>`) and the
  executor refused rather than reporting a false zero. They belong with the D-54 group, not here.
- Four more joined the *acted-when-told-not-to* family: `check-family-standards` (`offStandard 191`
  with every requirement removed), `check-surface-fit`, `set-view-template-control` and
  `export-parameters-to-csv` (`rows 5` for a parameter that does not exist).

**Two refusals were better than the arrangement that produced them**, and both are worth quoting
because they are what §3h.1 asks every fragment for:

> *"No wall, floor, ceiling or roof type called "ZZZNOTHINGHERE"… the Properties palette writes it
> "Basic Wall: Generic - 200mm", and either that or just "Generic - 200mm" works when the short name
> is unique."*

> *"No points were given. Separate them with semicolons and their three millimetre ordinates with
> commas — "0,0,0; 5000,0,0; 5000,3000,0"."*

Both were judged NO NEGATIVE / POSITIVE EMPTY, because a refusal is not an empty answer — but a
fragment that refuses like that is not the problem.

### Sweep 3 — MAKE the missing input instead of waiting for it, 2026-09-11

[`sweep-project1-3.yaml`](../tools/jobs/sweep-project1-3.yaml). **2 PASS**, 6 POSITIVE EMPTY.

| Passed | Positive | Negative |
|---|---|---|
| `import-parameter-values` | `valuesWritten 5`, `rowsMatched 5` | `rowsUnmatched 5`, nothing written |
| `export-views-to-fbx` | `exported 1 [{3D - ajmal.al}]` | `notThreeD 1`, *"None of the views given is a 3D view"* |

**`import-parameter-values` is the one worth copying as a method.** It reads a CSV, so a CSV was
written — two of them, the same shape, the same size, the same columns, differing **only in the first
column**: one holds the five real duct ids in this model, the other holds `1,2,3,4,5`. The fragment
reads both, matches five rows in one and none in the other, and reports `rowsUnmatched` either way.

> Where a fragment's input is a FILE, the missing content can be manufactured. That is a whole class
> of fragment that does not need the model to have anything.

**`export-views-to-fbx` has a STRUCTURAL negative**, which is the strongest kind available. FBX is a
3D format and a plan view has no solid geometry to give it — so `notThreeD 1` is true of every model
ever, not a count this project happens to produce. Compare `select-subcomponents`, whose negative is
strong for the same reason: ducts are system families and cannot nest anything, anywhere.

**The six that came back empty needed content this model has not got**, and the sweep is how that got
settled rather than assumed: no revisions (`delete-revision`), no design options (`set-design-option`),
no scope boxes (`assign-scope-box-to-view`), no groups (`ungroup-elements`), no named DWG export setup
(`export-views-to-dwg`, the same block Snowdon has in §3b-i), and no schedules (`export-schedule-to-csv`).

### Sweep 4 — Snowdon, 0 passes and four findings, 2026-09-11

[`sweep-snowdon.yaml`](../tools/jobs/sweep-snowdon.yaml). **Nothing passed**, and it was still the most
useful sweep of the four, because two of its jobs existed to settle a question rather than to prove a
fragment. 9,628 elements before and after.

**THE REASON FOR GOING THERE WAS WRONG, AND THAT IS THE FIRST FINDING.** Sweep 3 ended by saying five
fragments were blocked only by Project1 having no sheets, and that Snowdon has a drawing set —
`find-unplaced-views` reported *"42 that are placed"*. **It does not.** That proof was taken against
`Snowdon Towers Sample HVAC.rvt`, the delivered sample; the open model is `Snowdon-scratch_ajmal.al`,
a working copy, and `find-views viewType=DrawingSheet` returns **0** there too.

> A proof names its model. Reading "Snowdon" in one and assuming it means the Snowdon you have open is
> the same mistake as reading a count instead of deriving it. `align-viewports-across-sheets`,
> `create-sheet-list`, `export-sheets-to-pdf`, `place-views-on-sheet` and `set-sheet-title-block` are
> blocked on **both** open models.

### `set-mep-justification` and `select-in-region` are the FRAGMENT, not the model — SETTLED

Both were recorded NEEDS_REVIEW after odd behaviour on Project1's five ducts. Re-run here against 307:

| Fragment | Project1 (5 ducts) | Snowdon (307 ducts) |
|---|---|---|
| `set-mep-justification` | every non-zero offset refused, only 0 accepted | **`set 0` at 50 mm — same** |
| `select-in-region` | `elements 0` in a ±100 m box | **`elements 0` in a ±200 m box — same** |

**Neither is model-specific.** One model can never tell you that; two can, which is the only reason
both were run again. `set-mep-justification` writes, reads back, and honestly reports the value did not
stick — on every duct in a delivered sample as well as a hand-drawn scratch. `select-in-region` still
reports the volume it searched in **feet labelled as millimetres**.

> Both move from NEEDS_REVIEW to **OPEN**. What was a coincidence on one model is a defect on two.

### `create-hvac-zone` IGNORES THE LEVEL IT WAS GIVEN — found 2026-09-11, OPEN

Seventeen spaces were selected in `FloorPlan: L3`. Grouped by level with `group-and-count`, ten are on
`L3`, five on `Parking`, one on `L1 - Block 37`. The negative asked for a zone on **`L1 - Block 35`**,
a real level in the same model that **none of the seventeen is on**:

```
added 17
movedFromAnotherZone 17 [Residential Lobby 106 (id 1410865) LEFT zone 'Default', ...]
```

It put **all seventeen** into a zone on a level none of them belongs to, and pulled each one out of its
existing `Default` zone to do it. This is worse than the counting family below: those report work
nobody did, and this one **does real work in the wrong place**. Its reporting is good — every move is
named — but nothing checks that the space is on the level the zone is for.

`stack-tags` joins the counting family: `gapMm 0` still `stacked 73`.

### What is left, and why it needs a different model

After three sweeps, **27 arrangeable fragments remain and most are blocked on content Project1 will
never have**:

| Need | Fragments |
|---|---|
| **Sheets** | `align-viewports-across-sheets`, `create-sheet-list`, `export-sheets-to-pdf`, `place-views-on-sheet`, `set-sheet-title-block` |
| CAD import | `convert-cad-to-directshape`, `extract-cad-curves` |
| Worksets | `create-workset`, `set-element-workset` |
| A shared parameter file | `add-project-parameter`, `transfer-project-parameters-between-documents` |
| One each | lines, pipes, tags, spaces, design options, scope boxes, revisions, family files on disk |

`Project1` has **no sheets at all**. `Snowdon-scratch` is a delivered sample model and has a drawing
set — `find-unplaced-views` reported *"42 that are placed"* on it. **Five fragments turn on that one
fact**, and the only thing standing between them and a run is which document is in front, because the
`model:` stamp follows the active one.

### The nine that acted when told not to — one family, and it is the biggest yet

Each was handed the value that should switch its own answer off, and each reported work anyway:

| Fragment | The "off" value | What it still reported |
|---|---|---|
| `move-elements` | `offset 0,0,0` | `moved 5` |
| `copy-elements` | `offset 0,0,0` | `copies 5` — five copies stacked on the originals |
| `rotate-elements` | `angleRadians 0` | `rotated 5` |
| `hide-elements` | `permanent false` | `hidden 5` |
| `zoom-to-elements` | `alsoSelect false` | `shown 5` |
| `set-category-visibility` | `visible true` on something already visible | `changed 1` |
| `set-crop-box-settings` | every switch `off` | `changed 1` |
| `place-room-at-point` | a point 900 m from anything | `created 1`, `unenclosed 1` |
| `create-workset-3d-views` | `namePrefix ""` | `created 2` |

With `disallow-join` (recorded below) that is **ten fragments** whose counter reports the CALL rather
than the CHANGE.

> A counter that cannot come back zero cannot be wrong, and a fragment that cannot be wrong cannot be
> proved. This is §3h.1 — *make silence illegal* — and the sweep has just multiplied its membership by
> five in one pass.

**`copy-elements` is the one to fix first.** A zero offset produces five real copies sitting exactly on
top of the originals — invisible in every view, counted as success, and a genuine modelling fault the
next person inherits. `move-elements` and `rotate-elements` merely do nothing and say they did
something; this one leaves debris.

**`place-room-at-point` is the interesting exception.** It created a room 900 m away and reported
`unenclosed 1` in the same breath — so it already knows the room is nonsense. It has the information
needed to refuse and reports it as accounting instead.

### `disallow-join` COUNTS THE CALL, NOT THE CHANGE — found 2026-09-10, OPEN

Run on the same four walls, both ways, everything else held identical:

| `allowJoin` | `changed` | `refused` | `wasPinned` | `unsupported` |
|---|---|---|---|---|
| `false` — disallow | **4** | 0 | 0 | 0 |
| `true` — allow | **4** | 0 | 0 | 0 |

**Identical.** Walls that already allow joining are told to allow joining, and all four are counted as
changed. `changed` is `role: result`, and it reports the number of CALLS MADE rather than the number
of ends whose state actually moved.

This is the family §3h.1 is about — `create-levels` reporting `created 2` beside `nameRefused 2`, and
`duplicate-type` producing a second type called `Tees`. A counter that cannot come back zero is a
counter that cannot be wrong, and a fragment that cannot be wrong cannot be proved.

**Left out of the batch deliberately.** A pair like that demonstrates nothing, and proving it would
freeze a counter that reports work nobody did. The fix is to read the end's join state first and count
only what moved — a fragment change, and the owner's.

### `group-and-count` PROVED THROUGH THE CHAIN — the payoff for row 11, 2026-09-10

The fix that added `--keep-chain` was made earlier the same day and never collected on. This is what
it was for.

`group-and-count` takes `values` from the CHAIN, not from the selection, and by default the fragment
under test resets the chain — so before the fix it came back `needs_unbound: 'values' was never
supplied` while the setup reported success. The setup is three steps and only the first resets:
`select-by-category-name` leaves `elements`, `set-selection` writes the real selection,
`read-element-parameters` leaves `values`.

| `parameterName` | `groups` |
|---|---|
| `System Type` | **1 item(s) `[[Supply Air, 5]]`** |
| `ZZZNOTHINGHERE` | 0 item(s) |

**The `bound` line is the whole argument, and it is recorded in the draft:**

```
positive  values from read-element-parameters (5)
negative  values from read-element-parameters (0)
```

Both legs are **bound**. A parameter name matching nothing leaves `read-element-parameters` returning
an EMPTY dictionary — not a failure — so `values` is bound to something real and the fragment ran and
found nothing. That is the §1d distinction exactly: a cleared selection leaves the need **unbound**
and the executor refuses before the fragment starts, which proves nothing.

Snowdon gives richer evidence for the same fragment — three groups, `9 + 8 + 5 = 22`, measured
2026-09-10 — and is worth taking if this is ever re-proved. Arithmetic across three groups is stronger
than a count of one.

### `select-in-region` REPORTS FEET AS MILLIMETRES, AND FINDS NOTHING — NEEDS_REVIEW, 2026-09-10

Asked for a 20,000 mm cube over content that sits within 6 m of the origin:

```
minMm=0,0,0   maxMm=20000,20000,20000   categories=Ducts
  -> "0 element(s) in a 66 x 66 x 66 mm volume, by the fast BOUNDING BOX test"
```

**20000 ÷ 304.8 = 65.6.** The value is converted to internal feet correctly and then reported with a
`mm` label — so the sentence a reader checks their input against is off by a factor of 305. A second
run at `-5000 … 20000` (25,000 mm) reported *"82 x 82 x 82 mm"*; 25000 ÷ 304.8 = 82.

**And it found nothing** — `elements 0`, `withoutGeometry 0` — in a box that on those numbers is
65.6 ft ≈ 20 m and should contain four walls and five ducts. Tried with `Ducts` and `Walls`, with
`exact` false and true, and with a ±100 m box. Always zero.

**No mechanism is claimed.** The label defect is certain — the arithmetic is on the page. Whether the
empty result is the same units problem one layer deeper, a different origin, or something else is not
settled by anything measured here.

> A fragment that reports the volume it searched in the wrong unit is worse than one that reports
> nothing, because the number looks like a confirmation of what you asked for.

### `set-mep-justification` ACCEPTS ONLY AN OFFSET OF ZERO — NEEDS_REVIEW

Run on the same 22 ducts, three times:

| `horizontalOffsetMm` / `verticalOffsetMm` | `set` | `withoutOffsets` |
|---|---|---|
| 500 / 500 | **0** | 0 |
| 50 / 50 | **0** | 0 |
| 0 / 0 | **22** | 0 |

Every non-zero offset is refused, per element: *"Refused: id 1447716: 2 of 2 offset(s) did not take the
value"*. `withoutOffsets 0` says all 22 **have** the parameters, so it is not a missing-parameter case.

**The fragment is behaving well** — it writes, reads back, and reports that the value did not stick
rather than claiming success. The question is why nothing but zero sticks. Two readings, and nothing
here settles which:

- the ducts' justification is constrained or locked in this model, and zero is the only legal value; or
- the write is going somewhere the read-back does not see, in which case `set 22` for zero is **also**
  wrong — it would be reporting success for a change that never had to happen.

> The second reading is the reason not to prove it. A pair of `set 22` against `set 0` would pass D-30
> on the strength of a zero-offset positive, and freeze whichever of the two is true.

Same family as `check-equipment-connectors` in §3b-i, where a larger tolerance found MORE mismatches:
**worth reading before it is proved, because a proof would freeze whichever behaviour is there.**

---

## 3c. THE NEGATIVE CASE HAS NOT BEEN FOUND YET — 2026-09-08, third round

These have a **working positive**. What is missing is an arrangement in which the answer MUST be empty,
and the obvious one turned out not to be.

| Fragment | Positive | Why the obvious negative fails |
|---|---|---|
| `create-levels` | Created 2 levels at 45000 and 46000 | A **name clash does not stop it** — asked for `L2,L3`, which both exist, it created 2 levels anyway. Revit renames rather than refusing. Try an ELEVATION that already carries a level, so `alreadyThere` fires |
| `create-view-template-from-view` | Created template "HERON TPL Z9" from `Model Linking` | Same shape: *"the template was created and could NOT be called 'Mechanical Plan'"* — it made one under another name. The clash is reported, not obeyed |
| `create-sheet-list` | Created a sheet list with 2 fields | Same as the two above — asked for `Sheet Index`, a name already taken, it added the fields and made the schedule anyway. `refused` names the clash; `fieldsAdded 2` says it proceeded |
| `check-family-standards` | 97 family types reported off standard | **No negative exists in this model.** `namePattern=*`, which everything matches, still reports 97 — because "off standard" also counts families with `(0 placed)`. Needs a model whose families all pass, or a narrower question |
| `select-unenclosed-rooms` | **None.** Examined 67 rooms, `elements 0` | Every room in Snowdon is properly enclosed. Needs a room with a wall deleted |
| `create-key-schedule` | Created a key schedule | A name already taken does not stop it — **the fourth creator today to rename instead of refusing**. See the decision in §3d |
| `select-by-material` | **None.** `0 of 1053 scanned` | No duct in Snowdon carries a named material, so no material name gives a positive. Needs a category that does |
| `set-view-underlay` | Set an underlay between L2 and L3 | An EMPTY `baseLevel` is refused by Heron before the fragment sees it, so "clear the underlay" cannot be expressed. That is arguably a gap in the value passing, not the fragment |
| `reset-view-graphics` | Cleared 109 element overrides in `Model Linking` | The drafting view chosen as the negative had **8 overrides of its own** and cleared them. Not a failure — a real answer. Needs a view with none, and no view in Snowdon has none |
| `switch-active-project` | Reported `switched true` | Only ONE project is open, so it switched to the model it was already in. The handover's real question — *"check the tab bar afterwards, and never chain a write onto it assuming it landed"* — needs a second project open |
| `export-views-to-dwg` | **None.** `created 0` | *"name the DWG export setup"* — it will not use Revit's default, and this model has no named setup to give it. Needs one created in Export Setups first |
| `find-dead-ends` | **None.** `deadEnds 0`, `openEndsFound 0`, `stubs 0` | Consistent with `select-by-connection-status`, which proved every duct end in this model is connected. There are no dead ends to find. Needs a duct run with a loose end — **and one was built: PROVEN on `Project1`, §3b-ii** |
| `test-view-filter-match` | **None.** `matched 0` at both ends | The filter *is* found — `outOfScope` is 307 against 0 — but `Domestic` matches nothing, which `audit-view-filters` and `select-from-saved-set` both showed earlier. Third fragment blocked by the same fact about this model |
| `report-bounding-box` | 307 measured, combined 53957 × 22545 × 7137 mm | `maxRows` only limits how many rows are LISTED; `measured` and `combinedSizeMm` are the same whatever it is set to. There is no input that makes this fragment measure nothing, so the negative has to come from the selection — which no read fragment can change |
| `find-untagged-elements` | 235 untagged of 307 in `L3`, 72 already tagged | Its answer is the UNTAGGED ones, so it empties only when everything is tagged — and in a view with no tags at all it correctly returns all 307, which is a full answer rather than an empty one. `alreadyTagged` does go 72 → 0, so TRACKING ([D-53](DECISIONS.md)) would prove it |
| `report-geometry-complexity` | 28,244 triangles across 1 type | `maxTypes` and `listTop` only limit what is LISTED — every counter is identical at 10/10 and 0/0. Same shape as `report-bounding-box`: no input makes it measure nothing |
| `measure-run-quantities` | 307 quantities | **`measure=ZZZNOTHINGHERE` produced an identical answer to `measure=area`.** An unrecognised mode is silently ignored rather than refused, so the value has no effect at all and there is nothing to vary. That is a defect in the fragment, not only a proving obstacle — a caller asking for the wrong measure gets a confident wrong answer |
| `check-equipment-connectors` | 22 mismatched connectors on 10 units | **A LARGER tolerance found MORE mismatches, not fewer** — 22 at `sizeTolerance=99999` against 14 at `0`. That is backwards: a generous tolerance should accept more sizes as matching. Either the value is not in the units the name implies, or it is applied the wrong way round. Worth reading before it is proved, because a proof would freeze whichever behaviour is there |
| `select-subcomponents` | **None.** `elements 0`, `withoutSubComponents 10` | The 10 mechanical equipment units in `L3` are not nested families, so there are no subcomponents to select. Needs a family that has some |
| `report-parameter-inventory` | 71 parameters across 10 elements | `sampleOnly=true` reads ONE element and still returns 32 parameters. It narrows the sample, it does not empty the answer — the same shape as `report-bounding-box` and `report-geometry-complexity`, and the third fragment today whose only input controls how much is shown rather than what is found |
| `read-space-loads` | **None.** All 17 spaces return `noLoad` — *"Revit refused the f…"* | Exactly what HANDOVER.md predicted: *"every space in the model returns noLoad; a positive needs Areas and Volumes on with the analysis run, or a Design Heating Load typed on one space."* Confirmed against the model today rather than carried forward on trust |
| `find-overlapping-lines` | **None.** `overlapping 0` on 9 real detail lines, all straight | Nothing overlaps in the drafting view even at a 99,999 mm tolerance — which is itself worth a look, since at that tolerance almost any two lines in one view should qualify. Either the tolerance governs collinearity rather than distance, or it is not in millimetres. Read it before arranging a case |
| `copy-parameter-value` | **None.** `copied 0`, `typeMismatch true` | Copying `System Type` into `Comments` is refused because one is a type parameter and the other an instance one — correct behaviour, and not a positive. Needs two instance parameters of the same kind where the source actually holds something. `Comments` is blank on every duct in this model, which is what made it look like an easy choice |
| `group-elements` | Grouped 614 elements from 307 ducts | Every category tried can be grouped — spaces gave 17, not 0. Revit groups almost anything, so an empty case needs a selection it REFUSES rather than a different category. `refused` is the field to make fire |
| `ungroup-elements` | **None.** `ungrouped 0`, `notGroups 307` | Nothing in this model is grouped, and `group-elements` cannot leave one behind because its own run rolls back. Proving it needs a group that already exists in the model |
| `center-room-tags` | **None.** `centred 0`, `notRoomTags 17` | It wants ROOM tags. This is an MEP model — it has Spaces and Space Tags, not Rooms — so every tag in it is `notRoomTags`. Needs the architectural model, or a room placed in this one |
| `maximize-datum-extents` | **None.** The selector found no Levels in `FloorPlan: L3` | A level does not appear in its own plan view. It needs a section or elevation, where datums are visible — `Elevation: North - Mech` is the obvious candidate |
| `isolate-elements` | 307 isolated in a plan | 307 isolated in a drafting view too — `viewRefused false` both times. It isolates whatever it is handed, so it CANNOT come back empty; the drafting view was expected to refuse and did not. Same case as `count-elements`: prove it by TRACKING ([D-53](DECISIONS.md)) across selections of different sizes |
| `trim-extend-elements` | **None.** `moved 0` against ducts AND against tags | Nothing in the selection needed squaring up — the ducts here already meet cleanly. Needs two elements deliberately left short of each other |
| `create-assembly-views` | **None.** `created 0`, `notAnAssembly 307` | Ducts are not assemblies. Nothing in this model is — needs an Assembly created first, which is itself a Revit command with no fragment behind it |
| `zoom-to-elements` | Zoomed to 307 ducts | The negative selection — Duct Tags in a drafting view — does not exist, so the setup found nothing. Any category present in BOTH a plan and a drafting view would do; there may not be one |
| `flip-elements` | **None.** `flipped 0`, `cannotFlip 144` | Air terminals in this model cannot be flipped — Revit refuses all 144. Needs a family that is flippable, which is a property of the family rather than of the fragment |
| `hide-elements` | Hid 307 ducts in `L3` | The negative selection — duct tags on the schedules sheet — does not exist there, so the setup found nothing. A category present in two different views is needed, and tags are view-specific by nature |
| `edit-parameter-text` | **None.** `edited 0`, `refused 307` | It refused every duct. `Comments` is writable, so the refusal is about something else — worth reading, because a fragment that refuses everything silently is the same shape as the eight schedule ones |
| `connect-open-ends` | **None.** `openEnds 0`, `connected 0` | Nothing here has an open end. `select-by-connection-status` proved that this morning — third fragment blocked by it, after `find-dead-ends` |
| `fillet-lines` | **None.** `created 0` on both legs | The nine detail lines in that drafting view do not meet at an angle a fillet can round. Needs two lines drawn to cross |
| `disallow-join` | **None.** `changed 0`, `unsupported 307` | Disallow-join is a WALL and beam idea; ducts do not support it. Needs walls, and Snowdon's are in the architectural link |
| `add-revision-cloud` | Not run | It needs `revisionId` as an **ElementId**, which Heron deliberately refuses to accept — see defect 8's neighbour in §6. The first fragment blocked by that decision rather than by the model, and a fair cost to weigh against it |
| `set-mep-justification` | **None.** `set 0` on both legs | *"0 run(s) set"*. Retried on 22 ducts as well as 307 — the same. Justification applies to a duct RUN and a flat selection of individual ducts is not one, whatever its size. The fragment needs whatever it counts as a run |
| `set-view-crop` | Cropped around 307 ducts | And around 73 tags in the negative — `enclosed 73`, `applied true`. It crops around whatever it is handed, so it CANNOT come back empty. Same family as `count-elements` and `isolate-elements`: prove it by TRACKING ([D-53](DECISIONS.md)) across selections of different sizes |
| `update-saved-set` | **None.** *"No saved set called…"* on both legs | The set it was pointed at was created by `create-selection-filter` in an earlier run — which rolled back, taking the set with it. **A fragment that edits what another fragment creates cannot be proved while both roll back.** Needs a saved set that already exists in the model |
| `edit-text-values` | **None.** `changed 0`, `absent 7` on the positive | Handed 7 text notes with `field=text` it reported all seven as `absent` — so `text` is not the field name it wants. The valid values are not written anywhere findable; `mode` accepts prefix/replace/suffix, but `field` was not discoverable from the source. **Another instance of the LIST_ gap in §3d** — a fragment taking a name with no way to learn the names |
| `compare-elements` | 29 differing parameters across 8 compared ducts | Handed tags instead it still found **1** differing — an empty negative needs elements that are genuinely identical, or fewer than two so `tooFewToCompare` fires. `select-by-category-name` cannot narrow to one, so this needs a way to select exactly one element |
| `check-flow-direction` | **None.** `bothIn 0`, `bothOut 0` — 15 joints checked, 25 skipped as bidirectional | Exactly what HANDOVER.md predicted on 2026-09-07: *"it works, there is simply no fault in Snowdon Towers to find. Its positive needs two connectors both set to Out, built in the Family Editor."* Confirmed against the model rather than carried forward |
| `read-graphic-overrides` | **None.** `withOverride 0` on both legs | No element in this model carries a per-element override. Same blocker as `report-category-overrides` had, and the same cure — the Architectural model has views that do |
| `audit-mep-openings` · `check-ceiling-coordination` · `check-fixture-connectivity` · `place-mep-fitting` · `auto-size-pipe` | **None.** Every declared result came back zero | Each needs model content this one lacks: openings through walls, host ceilings, plumbing fixtures, a gap wanting a fitting, pipes. Snowdon HVAC has ducts and its architecture is a LINK |
| `color-by-parameter` | Coloured 22 ducts by System Type | A parameter name that does not exist still coloured 22 — it falls back rather than refusing, so the input cannot empty the answer. Worth reading: silently ignoring an unknown parameter is the same shape as `measure-run-quantities` |
| `remove-parameter-value` · `set-mep-slope` | **None.** `cleared 0` and `sloped 0` on both legs | `remove-parameter-value` found Comments already empty on all 22 — an earlier write had rolled back. `set-mep-slope` found 20 of 22 with both ends connected, which cannot be sloped without moving what they join. Both need arranging, not fixing |
| `rename-elements` · `rename-family` · `unload-links` · `reload-links` · `set-design-option` · `set-schedule-filters` · `duplicate-sheets` · `align-viewports-across-sheets` | **None.** Every declared result zero, or the positive never ran | Each wants content or a selectable category this model does not offer in the view chosen — sheets and viewports are not in a floor plan, there are no design options, and the link and family categories did not resolve. Re-run once the batch tool exists, with the arranging rules applied |
| `check-vertical-clearance` | 18 services too close at a 99999 mm required gap | `clashing` stayed at **5 in both runs** — it counts services actually touching, which does not vary with the gap asked for. So the fragment has two results and only one answers the question put to it. Either `clashing` is a separate finding that belongs in its own fragment, or the negative has to be arranged some other way |
| `set-category-visibility` | Hid Ducts in `Model Linking` — `changed 1` | Showing them again is also `changed 1`. The same two-position needle as `set-crop-box-settings`, and the same fix would serve both |
| `set-view-template-control` | 19 parameters held by the template, 9 freed | `nowHeld`/`nowFree` are the template's WHOLE state, so they are never empty whatever is asked. The result worth judging is *how many changed*, which the fragment does not report |
| `create-workset-3d-views` | Created one 3D view per workset (2) | An empty `namePrefix` still creates 2. There is no input that makes it create nothing |
| `select-by-electrical-circuit` | **None.** `elements 0`, `panels 0` | An HVAC model has no electrical circuits. Needs `Snowdon Towers Sample Electrical` |
| `set-crop-box-settings` | Turned the crop on in `Model Linking` — `changed 1` | Turning it OFF is also a change: `changed 1` again. The needle has only two positions and both move it. Needs a run that asks for the state the view is ALREADY in |
| `set-section-mark-visibility` | **None.** `hidden 0`, `shown 0` | There are no section marks in `L3` to make visible or hide. Needs a view containing one |
| `remove-view-filter` | **None.** `removed 0`, `deleted 0` | *"'L2' is governed by template 'Mechanical Plan', which owns its filters"* — a filter cannot be removed from a template-driven view at all. Needs a view whose filters are its own |

---

## 3d. GAPS — what proving showed is MISSING, not broken

Opened 2026-09-08 on the owner's instruction: *"if you find a gap also mention that, and new fragments
we need, we need to split this fragment, add new, edit — like that also mention in that list."*

Everything here was met by needing it, not by imagining it.

### New fragments the library wants

`list-levels`, `list-worksets`, `list-grids`, `list-sheets`, `list-revisions` and `list-linked-models`
all exist. The gaps below are the same shape and were each hit by having to write a throwaway probe
instead — several times in one session.

| Wanted | Why it was missed | How often today |
|---|---|---|
| **`LIST_VIEWS`** | `find-views` needs a `viewType` AND a `nameContains` before it answers. There is no way to ask *"what views are there"* — which is the first question of every proof that takes a view, and **53 fragments take one** | Every single view-based proof. The most-needed missing fragment of the day |
| `LIST_VIEW_TEMPLATES` | `select-view-templates` selects; nothing lists. Needed the names of all 18 before anything could be done with a template | 3 times |
| `LIST_LINE_STYLES` | `remap-line-styles` takes two style names and there is no way to discover one | 1 |
| `LIST_MEP_SYSTEM_TYPES` | `create-mep-system-type` takes `copyFromName` and nothing lists the 14 that exist | 1 |
| `LIST_MATERIALS` | `find-unused-materials` reports only the unused ones. There is no list of the materials that ARE used | 1 |
| `LIST_DWG_EXPORT_SETUPS` | `export-views-to-dwg` refuses without a named setup — *"Revit's default is not used"* — and nothing lists the setups a project has. It could not be proved at all for want of one name | 1 |

**The pattern is one sentence: every fragment that takes a NAME needs a way to discover the names.**
A caller who cannot discover a value cannot supply one, and [D-54](DECISIONS.md) made supplying them
possible without making them findable.

### …and two fragments already show the cheaper fix

**A new `LIST_*` fragment is not the only answer, and may not be the best one.** Two fragments proved
today already solve it for themselves, by handing back the alternatives **in the refusal**:

| Fragment | What it returns when it cannot match the name |
|---|---|
| `set-print-settings` | `availableSizes` — all 79 the print driver offers |
| `open-view` | `matches` — what the name DID match, and why it was refused: *"[view template, cannot be opened]"* |

One round trip instead of two, and the list arrives exactly when it is wanted — at the moment somebody
got the name wrong. **Deciding between the two shapes is part of the sit-down**, because doing both
means the same list is maintained in two places. The five `LIST_*` rows above are written as new
fragments only because that is how the library already answers this question elsewhere
(`list-levels`, `list-worksets`, `list-grids`); the convention below may be the better trade.

### One fragment to SPLIT

| Fragment | Why |
|---|---|
| `check-family-standards` | It answers two unrelated questions under one word. `namePattern=*`, which everything matches, still reported **97 off standard** — because "off standard" also counts families with `(0 placed)`. *"This family is named wrongly"* and *"this family is loaded and never used"* are different findings with different fixes, and `find-unused-families` already owns the second one. Merged, neither can be proved: there is no arrangement that empties both at once |

### A decision to make, then edits to follow

**A name clash refuses in some creators and renames in others**, and nothing says which is right:

| Refuses | Renames anyway |
|---|---|
| `create-level` | `create-levels` |
| `create-drafting-view` | `create-view-template-from-view` |
| | `create-sheet-list` |
| | `create-key-schedule` |
| | `duplicate-type` — *"'Tees' duplicated as 'Tees'"* |

**Five against two now**, so the majority behaviour is renaming. If the decision goes that way it is two
fragments to change, not five — but it also means a caller cannot rely on a clash being refused
anywhere, which is the more important half.

Both behaviours are defensible. What is not defensible is that a caller cannot predict which they will
get, and it cost three failed negative cases today before the pattern was visible. **Decide once, then
edit the minority to match** — and note that `create-level` and `create-levels`, the singular and plural
of the same idea, are on opposite sides.

### One edit, small and specific

| Fragment | Edit |
|---|---|
| `set-crop-box-settings` | Its three settings accept `on`/`off`/`true`/`false`/`yes`/`no` — every value is a change. There is no way to say **leave this one alone**, so a caller wanting to turn the crop on without touching the annotation crop cannot. It also means the fragment has no empty case: both `on` and `off` report `changed 1`. Add a `leave` value, and make it the default |

---

## 3e. THE SCHEDULE WALL HAS A DOOR — found 2026-09-09

**Ten fragments read or edit a schedule by looking for one in the selection**, and a schedule is a
VIEW — it cannot be selected as an element. HANDOVER.md hit this on 2026-09-07: *"both schedule
fragments refused the only object a person can select… a view cannot be selected as an element."*

**The way through was already in the library and nobody had written it down.**
`report-schedule-definition` was proved on 2026-09-08 against *"the 'Heat Recovery Unit Summary'
schedule, placed on a sheet and clicked"* — a `ScheduleSheetInstance`, which IS an element.

**And it can be done without clicking.** The category is called **`Schedule Graphics`**:

```
prove select-by-category-name set-selection   --set categoryName="Schedule Graphics"   --set inViewOnly="Notes, Symbols & Schedules"
```

Three instances on that sheet. `read-schedule-contents` — listed as *"fixed and UNPROVEN"* since
2026-09-07 — was proved through it immediately: 3 schedules, 144 body rows, against 307 non-schedules
skipped.

### …and behind the door, EIGHT fragments never got the fix

Tried through it, four of them, and every one did **nothing** — `added 0`, `changed 0`, `sorted 0`, and
tellingly `availableFields 0` and `presentFields 0`, meaning they never saw the schedule at all.

The reason is exact. **Only four fragments in the library understand `ScheduleSheetInstance`:**

| Understands it | Status |
|---|---|
| `report-schedule-definition` | PROVEN |
| `read-schedule-contents` | PROVEN 2026-09-09 |
| `place-schedule-on-sheet` | DRAFT |
| `find-unplaced-views` | — |

**Eight cast to `ViewSchedule` and nothing else**, so a schedule on a sheet slides straight past them:

`add-schedule-fields` · `add-schedule-combined-field` · `remove-schedule-fields` ·
`set-schedule-appearance` · `set-schedule-filters` · `set-schedule-sort-group` ·
`add-revision-cloud` · `export-schedule-to-csv`

The 2026-09-07 fix HANDOVER.md describes — *"they demanded a `ViewSchedule`; clicking a schedule on a
sheet gives a `ScheduleSheetInstance`"* — was applied to the two READ fragments and not to the eight
that edit.

**AND THEY FAIL SILENTLY.** Handed a schedule on a sheet they report `0 changed` with no refusal, which
reads as *"there was nothing to do"* rather than *"I could not see what you gave me"*. That is the
failure D-30's negative case exists to catch, and it is why none of them can be proved: both legs come
back identical because both legs did nothing.

**The fix is one line each and already written twice.** Do it once for all eight rather than per
fragment — a shared helper that resolves a selection to the `ViewSchedule` behind it, whichever form
arrived, is the shape the library wants.

---

## 3f. PROVED, BUT QUERIED — worth a second look at the sit-down

Marked `PROVEN` and standing when this section was opened, but the owner raised a doubt on the day and
it is recorded rather than argued away. A proof nobody questions is not the same as a proof that
survived being questioned.

| Fragment | Proved on | The doubt |
|---|---|---|
| `set-view-section-box` | 22 elements enclosed in `3D HVAC Layout`; the same call on `FloorPlan: M1` returned `viewRefused true`, `applied false`, 0 enclosed | **The negative may be testing Revit rather than the fragment.** A plan view *cannot* have a section box, so the empty answer is guaranteed by the view type and not by anything the fragment decided. A stronger negative would be a 3D view where the selection has no geometry to enclose — then the fragment has to reach the same conclusion by its own work |

**If the doubt is upheld, the remedy is to re-run it, not to un-prove it by argument** — and D-30's
fingerprint means the record says exactly what was run, so a better arrangement can replace it cleanly.

### SETTLED, and not by argument — 2026-09-09

The doubt was upheld, and the fix in §4 is what settled it: `viewRefused` now means only that the VIEW
refused, so the stronger negative the owner asked for — a 3D view whose selection has nothing to
enclose — is finally distinguishable from the weak one a plan view guarantees.

**`set-view-section-box` is back at `DRAFT`, and the reason matters more than the fact.** It was not
un-proved because the argument won. It was un-proved because the CODE MOVED: fixing the doubt changed
the implementation, the fingerprint stopped matching, and a proof is evidence about the bytes it was
taken against. Set back by the owner on 2026-09-09; the `proof:` block is kept as the record of what
was run.

> Un-proving by argument is what this section refuses. Un-proving because the implementation changed
> under the proof is not an argument at all — it is the fingerprint doing its job, and the only
> correct response to it is to run the fragment again.

---

## 3g. RUNNING FRAGMENTS IN BULK IS A WORKING NEED, NOT A TESTING ONE

Raised by the owner, 2026-09-09: *"maybe sometimes while we are working we need to run fragments in
bulk — is there an option?"*

**There is, and today depended on it.** `prove` runs several fragments in ONE process, ONE lease, with
D-29's chain carrying values between them:

```
prove select-by-category-name set-selection --set categoryName=Ducts --set "inViewOnly=FloorPlan: M1"
```

Two fragments, one job: the first finds 22 ducts, the second selects them. Every selection-based proof
today went through that, and the binding note on the answer says where the elements came from —
*"elements from select-by-category-name (22)"*.

### The name is the problem

**`prove` is a working command wearing a testing name.** Nobody doing real work would think to reach for
it, and that alone hides the capability the library already has. Renaming or aliasing it to `run` is
ten minutes and costs nothing.

### The real version is designed and unbuilt

| | |
|---|---|
| `HERON-KRN-WFL-007` **Workflow Engine** | *"Ordering, retries, timeouts, rollback, checkpoints, resume. Never decides…"* |
| `HERON-ORC-MAIN-001` **Orchestrator** | Understands the request, selects capability, builds and runs the work |

[docs/11](11-orchestration-and-workflows.md) is the whole design, and its own reference workflow for
*"select all ducts"* is eleven steps long — so multi-fragment work is not an edge case in this
architecture, it is the ordinary case.

### The ORDER is already computed — asked 2026-09-09, and the answer is better than expected

*"For arranging the correct order, is there something?"*

**Yes, and it works today.** `brain/heron_graph.py` (`HERON-KRN-DEP-013`) derives the edges from the
contracts — *"fragment → fragment, from the contracts: A provides what B needs"*:

```
$ python brain/heron_graph.py FRG-SEL-001          # set-selection
  could run after it   nothing
  could run before it  FRG-SEL-002, FRG-ELE-001, FRG-MEP-030 … 50 fragments
```

It says so itself: *"Every line above was computed just now from the fragments themselves. Nothing here
is stored, so nothing here is stale."* That is [D-40](DECISIONS.md) — an edge is derived, never written
down twice.

**So ordering is not a missing capability. It is a missing COMMAND.** The graph answers *"what can run
before this one"* for a single fragment. What a bulk runner needs is the other shape: *"here are five
fragments — what order?"*, which is a topological sort over edges that already exist.

`HERON-DEV-PLN-002` **Planning Agent — "Sequences the work"** is the registry slot for the judgement
part, and is unbuilt. But the hard half — knowing which fragment can feed which, across all 360 — is
done.

### Three different things, worth not confusing

| Thing | For | State |
|---|---|---|
| `prove` | Run several fragments now, in a line | **Works today**, misnamed |
| `tools/batch-prove.py` | Run many PROOFS and judge them | Being built by a separate session |
| Workflow Engine | Real work: retries, rollback, resume | **Designed, not built** |

### What today showed is missing, concretely

**When a chain half-succeeded there was no resume.** `select-by-category-name` would run, `set-selection`
would refuse because the category was not in that view, and the only option was to re-run both. On a
two-step chain that is trivial. On the eleven-step workflow docs/11 describes, it is not — and that is
exactly the gap `HERON-KRN-WFL-007` exists to fill.

---

## 3g-ii. WHAT BLOCKS THE REMAINING 172, DERIVED RATHER THAN ASSUMED — 2026-09-10

After a night of proving fragment by fragment, the obvious question is what is actually left. This is
computed from the contracts and from `FromRequest` in
[`RevitFragment.cs`](../revit/Heron.Revit.Addin/RevitFragment.cs), not estimated.

| Count | What blocks it |
|---|---|
| **97** | **Arrangeable — blocked only by CONTENT or by a hard negative** |
| **67** | A request value that cannot be supplied ([D-54](DECISIONS.md)) |
| 3 | Needs TWO element sets, and one selection cannot say which is which |
| 3 | Its only declared result is a bare `ElementId`, skipped as a helper object |
| 1 | Its only declared result is a `bool` — neither leg can be non-empty |
| 1 | No declared result at all |
| **172** | |

### The refused values are ONE problem wearing several hats

82 refused request values across 27 types, and two of them are half of it:

| Count | Type | Example |
|---|---|---|
| 28 | `ElementId` | `add-revision-cloud.revisionId` |
| 14 | `Element` | `align-elements.reference` |
| 4 | `OverrideGraphicSettings` | `apply-view-filter.overrides` |
| 3 each | `View3D`, `IList<Element>`, `IList<ElementId>`, `ICollection<ElementId>`, `Color` | `create-view-filter.categoryIds` |

**`ElementId` and `Element` together are 42 of the 82.** That is D-54's own claim, now counted: naming
one particular element in the model is the single most-wanted thing a caller cannot say.

### A first pass at this said 111, and it was wrong

The first attempt assumed only `string`, `double`, `int` and `bool` could be supplied, and reported
**111** blocked on values. `FromRequest` resolves far more than that by name — `View`, `Level`,
`Category`, `BuiltInCategory`, `FamilySymbol`, `WallType`, `FloorType`, `CeilingType`,
`MEPCurveType`, `FilledRegionType`, `Phase`, `IList<XYZ>`, `IList<string>` and the `ICollection` /
`IEnumerable` / `List` forms of each. Every one of those was used in a job file tonight:
`view=1 - Mech`, `categories=Ducts`, `points=0,0,0; 6000,0,0`.

> The count was wrong because the resolver was assumed instead of read. **Read `FromRequest` before
> claiming a type cannot be supplied.**

`Element` and `ElementId` are the conditional case, and the condition is §5 row 13's fix: they resolve
only when the need NAME ends in `Type`, because a type can be found by name and one particular
instance cannot.

### What this means for the next proving session

**The binding constraint is no longer the models.** 97 of 172 are arrangeable as the harness stands —
more than half — and tonight's four unblocked fragments all came out of that group by reading a triage
row and going to the model that had what it named.

The 67 are a different kind of work: they need value passing, not a better model, and no job file can
route around them.

---

## 3g-iv. SIX SWEEPS LATER: THE ARRANGEABLE POOL IS EXHAUSTED — 2026-09-11

Six bulk passes asked **77 fragments**; 13 passed, 12 were promoted, one was put back. What remains,
recomputed from the contracts and from `FromRequest` after the last sweep:

| Count | What blocks it |
|---|---|
| 76 | Arrangeable — blocked by CONTENT or a hard negative |
| 65 | A request value that cannot be supplied ([D-54](DECISIONS.md)) |
| **12** | **Gated by the permission tier — `ADMIN` or `PUBLISH`** |
| 6 | Its only declared result is a `bool`, a bare `ElementId` or a `string` |
| 3 | Needs TWO element sets from one selection |
| 1 | No declared result at all |
| **163** | |

**The 12 are new to this table and they are not blocked by anything technical.** `validate` runs them
today — that is the OPEN defect in §5 — and the owner's decision on 2026-09-11 is that it should not.
Until the gate is on the proving path, **every job file has to apply it by hand**, which is what
[`sweep-last-three.yaml`](../tools/jobs/sweep-last-three.yaml) does and says.

### The eight that were left, and why five could never have run

| Fragment | Blocked on |
|---|---|
| `align-viewports-across-sheets`, `create-sheet-list`, `place-views-on-sheet`, `set-sheet-title-block` | **SHEETS.** Neither open model has one |
| `fillet-lines` | **LINES.** Neither open model has one |
| `set-element-workset` | `moved 0` at `worksetId 0` — either there is no workset 0 or the ducts are already on it |
| `place-accessory-on-run` | *"No family type called "Damper" in Snowdon-scratch"* — the model has no duct accessory |
| `report-findings` | **Cannot carry a proof at all.** Its only result is `report`, a `string`, and its whole purpose is to write a sentence *"when nothing was found at all"* — so it is never empty. A describer, and [D-53](DECISIONS.md) tracking is the route |

### What a seventh sweep would need, and it is not another job file

Nothing is left that a cleverer arrangement reaches. The three routes out are all changes to the
library or the harness:

1. **Value passing** — 65 fragments, and `ElementId` + `Element` are 42 of the 82 refused values.
2. **A model with a drawing set, CAD imports, drafted lines, pipes and design options.** Four sheet
   fragments and a dozen others turn on content neither open model has. `Snowdon Towers Sample
   HVAC.rvt` — the delivered sample rather than the scratch copy — is the obvious candidate and was
   never opened.
3. **The gate on `validate`**, which releases 12 immediately.

> Six sweeps found thirteen proofs and eleven defects. **The defects are the better return**, and
> that is not a consolation: a sweep that only proves things tells you what already works, and this
> one kept finding fragments that report work nobody did.

---

## 3h. FOUR THINGS THAT WOULD IMPROVE THIS, RANKED — 2026-09-09

Asked for by the owner at the end of two days of proving. Ranked by what they would have saved
**today**, not by how interesting they are.

### 1. MAKE SILENCE ILLEGAL — do this one first

A fragment handed something it cannot use reports **`0`** instead of refusing. `0 changed` and *"I could
not see what you gave me"* are then the same sentence.

| Fragment | Given | Answered |
|---|---|---|
| the eight schedule fragments in §3e | a schedule on a sheet | `added 0`, `changed 0`, `sorted 0` — no refusal |
| `measure-run-quantities` | `measure=ZZZNOTHINGHERE` | identical to a valid mode |
| `color-by-parameter` | a parameter that does not exist | coloured 22 elements anyway |
| `edit-text-values` | `field=text` | all seven notes reported `absent` |

**This cost more time today than anything else**, because every one of them looks exactly like a
fragment correctly finding nothing — which is also why none of them can be proved: both legs come back
identical.

**And on a real project it is worse than slow. It is a confident wrong answer.** A modeller who asks for
the wrong measure gets a number, not a question.

> **A fragment that cannot use its input must REFUSE and say why. It must never report zero.**

Every fragment already has a `refused` output, and 117 of them now declare it as accounting. The
machinery is there; the discipline is not.

### 2. Finish declaring the roles — **DONE 2026-09-09, and it was bigger than this**

This asked for 40. **The library held 989**, and the section could not see the other 949 because it was
counting by name shape.

| Tranche | What it was | Count |
|---|---|---|
| bookkeeping-shaped, the patterns MISS them → guessed `result` | what this item counted | 40 |
| the patterns CATCH them → guessed `accounting` | invisible to this item | 150 |
| match no pattern at all → defaulted to `result` | invisible to this item | 799 |

**All 1,201 provides now declare a role**, read off what each fragment is FOR rather than what its
output is called. The reading disagreed with the naming patterns **166 times, in both directions** —
`select-unenclosed-rooms` declares `unplaced` and `unenclosed`, the two faults it exists to find, and
`REJECT_NAMES` swallowed both; `set-mep-slope.inGroup` and `flip-elements.cannotFlip` are bookkeeping no
pattern could see, and a non-zero one of those banks a proof for a run that changed nothing.

So `REJECT_PREFIX`, `REJECT_NAMES` and `WORK_COUNTER` were **deleted** rather than kept as a fallback
that is wrong one time in seven, and `check_contract` now REFUSES a provide with no `role` — the
omission is a validation error somebody fixes instead of a silent default nobody sees. `findings` is the
one exemption (D-51). **Nothing reads a name any more.**

The original two victims are declared: `remove-parameter-value.alreadyEmpty` and
`set-mep-slope.bothEndsConnected` are `accounting`, and `bothIn`/`bothOut` on `check-flow-direction` are
`result` — which is the example this item was written around.

### 3. Ask the model once, not six times

Six throwaway probe scripts were written today asking the same kinds of question: what views exist, how
many ducts per view, what connector sizes, which categories hold anything. Every one a round trip.

One command answering *"describe this model"* — views by type, categories by count, the sizes actually
present — removes all six, and it is the same gap as the missing `LIST_*` fragments in §3d.

### 4. Generate the job file

`tools/batch-prove.py` takes a hand-written job list. Most of that list is derivable: which fragments are
still DRAFT and untried, whether `--write` is needed, the setup chain, and **the exact input names** —
six were mistyped today, `widthMm` for `width` and `sortByFields` for `sortFieldNames` among them.

It must leave the category and the view **blank rather than guessing**. A wrong category produces a
confident meaningless result, which happened eleven times in one batch.

### Why the order matters

Two, three and four make the PROVING faster. **One makes HERON honest**, and a tool that quietly gives
wrong answers is worse than a slow one.

---

## 3i. THE ELEMENT-SHAPED WALL — found 2026-09-09, second sitting

Eighteen MODIFY fragments were run in two rounds against `Snowdon-scratch_ajmal.al`, chosen because
they had **no run record at all** — they had never been in front of a model. Three proved.
`tools/jobs/modify-never-run.yaml` and `tools/jobs/modify-round-2.yaml` are the arrangements, kept so
the next round starts from what was learned rather than from the fragment list.

| Proved | Positive | Negative |
|---|---|---|
| `set-element-level` | `moved 1`, `alreadyThere 21` — 21 of the 22 ducts were already on L2 | `moved 0`, `refused 9` |
| `array-elements-radial` | `count=2` → `created 22`, `copiesEach 1` | `count=1` → `created 0`, and it says why: *"a count of 1 asks for no copies at all"* |
| `renumber-sequential` | `renumbered 22`, planned HZ1–HZ22, `collisions 0` | `renumbered 0`, `refused 9` |

`array-elements-radial` is the best-shaped proof of the three: **both legs are the same selection** and
differ only in the value, so it tests the input rather than the arrangement. Where a fragment allows
that, prefer it.

`set-element-level` is the first fragment that **needed [D-54](DECISIONS.md) to exist** — it was run
earlier the same day, with no value, and refused by name rather than guessing.

### The wall itself

[D-54](DECISIONS.md) resolves a **view, a level, a category, a name, a number, or true/false — and
lists of those.** It refuses everything else by name, with a reason. `set-global-parameter` states the
boundary better than this file can:

> *"Heron can be handed a view, a level, a category, a name, a number, or true/false - and lists of
> those. `ParameterValue` is not one of them yet, so this fragment still has no way to receive it."*

**That refusal is correct and must not be softened.** Guessing which `FamilySymbol` was meant is how a
job runs against the wrong thing and reports success. But it is now the binding constraint: of the
**24** MODIFY fragments with only simple caller values and no run record, **more than half cannot be
arranged at all** because they want one of these:

| Shape wanted | Fragments blocked on it |
|---|---|
| `FamilySymbol` | `set-sheet-title-block`, `distribute-along-run`, `place-accessory-on-run` |
| `OverrideGraphicSettings` | `override-graphics-in-view`, `set-link-graphics`, `apply-view-filter` — and `read-graphic-overrides` already returns this type *unreadably*, so it is one finding about one type, not four |
| `Element` / `Material` / `Curve` | `align-elements`, `join-geometry`, `match-element-type`, `replace-material`, `set-view-crop-to-shape`, `create-from-room-boundaries` |
| `ParameterValue` | `set-global-parameter` |

**This is the next unlock after D-54, and it is the same shape of unlock.** D-54 took the caller's half
across as text and resolved it inside Revit where the document is. The same argument applies here: a
type name, a material name, a title-block name are all things Revit can look up — what is missing is
the resolver, not the possibility. The three that genuinely cannot work this way are
`OverrideGraphicSettings`, `Curve` and bare `Element`, because there is no name to look up.

### Two traps that cost a round each

**A HAND-RUN COMMAND'S LEASE FAILS THE WHOLE BATCH, AND LIES ABOUT WHY.** Round one failed **all
eleven jobs** immediately after a `count` check. `batch-prove` pins its own client id, the hand-run
command had pinned another, and the lease is per chat and lasts five minutes. The failures did not say
`session_in_use` — they said `needs_request_values` on the first two and *"Could not identify the
active model"* on the other nine, which reads exactly like an arrangement fault. The same job file ran
clean after `heron_bridge_client.py release`, with no other change.

> Release before batching. And distrust a batch where *every* job fails the same way — that is the
> environment, not the arrangement.

**SHEETS AND VIEWS CANNOT BE SELECTED BY CATEGORY — BUT THEY CAN BE SELECTED.** `manage-sheet-sets`
and `duplicate-views` both answered `setup_failed: the arrangement could not be re-made`, with and
without `inViewOnly`. Rule 3 says a category has to be visible where you select, and a sheet is not
*in* a view, it **is** one — so `select-by-category-name` cannot reach it.

**The conclusion first written here — that this blocks every sheet and view fragment — was wrong, and
it was wrong for an hour.** [`list-sheets`](../brain/fragments/list-sheets) is already `PROVEN`, needs
nothing but the document, and provides `elements`, which is exactly what `set-selection` consumes:

```yaml
    setup:
      - list-sheets
      - set-selection
```

That arranges all 17 sheets in one step. `list-levels`, `list-grids`, `list-revisions` and
`list-linked-models` do the same for their own kinds, and **every one of them is already PROVEN and was
sitting unused.** The §3d gap was never *"nothing lists these"* — it was that nothing named them where
somebody writing a job file would look. `tools/jobs/list-as-setup.yaml` is the worked example, and **five fragments proved through that route on the same day** — `edit-revision`, `duplicate-sheets`, `select-view-templates`, `manage-sheet-sets` and `remove-view-template`.

Three of the five were held up by an arrangement fault rather than by the fragment, and **each one named its own fault**: *"Mode 'add' is not one of create, rename, delete"*, *"No revision with sequence number 99. LIST_REVISIONS is where that number comes from"*, *"No sheet set called 'ZZZNOTHINGHERE'"*. A fragment that refuses in those words costs one run to correct. One that answers `0` costs a morning — which is the whole argument of §3h.1, seen from the other side.

### The rest of the eighteen, by what they need

| Fragment | What came back | What it needs |
|---|---|---|
| `dimension-rooms` | `created 0` with `view` correctly supplied | CONFIRM the L3 Spaces are bounded and that `measureTo=finish` is a mode it knows |
| `place-flow-arrows` | `placed 0` in both legs | The arrow family loaded. Both legs empty is the family missing, not the fragment failing |
| `create-legend-view` | *"No view called `Legend: Mechanical Legend`"* — a good refusal | The real legend name. Nothing lists them (§3d again) |
| `set-element-workset` | `moved 0` with `worksetId=0` | A workset **id**, and the gap is narrower than first written here. `list-worksets` is PROVEN and reports the names and the count — this model has two, *Shared Levels and Grids* and *Workset1*, both open — but **not the integer ids**, and the fragment reads `ELEM_PARTITION_PARAM` as an integer, so a name cannot stand in. One field added to `list-worksets` closes it |
| `place-views-on-sheet` | `placed 0`, and the negative was not empty either | Views selected, which is the sheet/view wall above |
| `align-viewports-across-sheets` | `aligned 0`, but `scaleMismatch 10` and `ambiguous 6` of 17 sheets | Sheets at one scale carrying one viewport each. Blocked on model content, not code — and the fragment said exactly why, which is the behaviour §3h.1 wants |
| `set-section-mark-visibility` | `setup_failed` twice — `categoryName: Sections`, model-wide and scoped to a plan | **Nothing in the library reaches section marks.** `select-by-category-name` cannot, and there is no `list-sections`. A gap row, not a defect |

### Reading the 196 run records was worth more than running anything — 2026-09-09

Every run record was re-read against its fragment's own contract. **80 DRAFT fragments have both
phases recorded**, and the shape of what stops them is now known rather than guessed:

| | |
|---|---|
| **25** | positive moved, negative **not** empty |
| **51** | positive never moved |
| **4** | already satisfied D-30 and nobody had noticed |

Two of the four were proved the same hour. **A run record is evidence that keeps**, and re-reading the
pile found more than the next batch did.

### How the judge decides "empty", exactly — because two rows of this file guessed at it

`looks_empty` in [`heron_validate.py`](../brain/heron_validate.py) filters to the contract's declared
names, drops anything declared `role: accounting`, and then, per value:

1. `_is_helper_object(value)` → **skipped**. A bare type name like `"ElementId"` or `"Func\`2"` is a
   rendering artefact, not an answer.
2. `_as_count(value)` returns `None` → **`return False` immediately.** Unreadable is not empty, and
   saying so is the whole point.
3. A non-zero count → not empty.
4. Nothing countable at all → not empty. There was no number here to have been zero.

**Step 1 is why `dimension-mep-runs` and `dimension-family-instances` proved** with `dimensionId`
reading as `"ElementId"` in all four phases: it is skipped, never reaching step 2. §5 row 8 assumed it
reached step 2 and blocked them. It did not.

**Step 2 is real, though, and it bites a NAME.** `report-geometry-complexity` returns
`heaviestTypeName: "Duct Size Tag: Duct Size Tag"` in its negative — a genuine string, not a helper
object, so it stops at step 2 and the negative can never be judged empty. Declaring the two work
counters (`typesMeasured`, `typesUnmeasured`) as accounting was right and is kept, but it does not
unblock it, and no role change should: **the fragment describes whatever it is handed**, so it has no
empty case at all. It belongs with the D-53 tracking group in §3c, not here.

> A declared result that is a NAME cannot carry the negative leg. Either the fragment has a countable
> result beside it, or it is a describer and D-53 is the route.

### A VALUE THAT RE-LABELS IS NOT A VALUE THAT TURNS THE ANSWER OFF — found 2026-09-10

`looks_empty` reads **every** declared result, so a negative is empty only when they are **all** zero
at once. A value-driven negative therefore has to switch the whole answer off, and one that merely
moves findings from one declared result into another does not — it looks like the strongest possible
pair and proves nothing.

`find-dead-ends` was arranged that way and judged **NEG NOT EMPTY**, correctly:

| `stubLength` | `deadEnds` | `stubs` | `openEndsFound` |
|---|---|---|---|
| 0.1 ft | 4 | 0 | 4 |
| 99999 ft | 0 | **4** | 4 |

`deadEnds` and `stubs` are both `role: result`. The four findings were re-labelled from one to the
other and the negative still returned content. The conserved total — which is what made the pair look
convincing — is precisely the proof that nothing was turned off.

> Before choosing a value for the negative leg, list the fragment's `role: result` fields. If the
> value moves findings **between** them rather than emptying them, it cannot carry the negative.

What worked instead was a second selection that was **not** a wrong category: two duct fittings, with
`withoutConnectors 0` — elements fully capable of showing an open end that genuinely had none. Same
domain, same view, populated on both sides. See §3b-ii.

### Four more for "make silence illegal" (§3h.1)

All four create something **in both legs**, which is why none can be proved — and all four are worse
on a real project than in a proof:

| Fragment | Positive | Negative |
|---|---|---|
| `create-view-template-from-view` | created `HERON TPL Z9` | asked for a name it could not use, **created `Model Linking Copy 1` anyway** and reported the refusal beside it |
| `duplicate-type` | `'Tees' duplicated as 'HERON TYPE Z9'` | given no new name, **`'Tees' duplicated as 'Tees'`** |
| `create-key-schedule` | created `HERON KEY Z9` | created `Duct Style Schedule` |
| `create-levels` | `created 2`, `nameRefused 0` | **`created 2` AND `nameRefused 2`** — it rejected both names and made both levels anyway |

A fragment that cannot use the name it was given should refuse it, not invent one. `duplicate-type`
producing a second type called `Tees` is the clearest case: nothing downstream can tell the two apart.

**`create-levels` is the most costly of the four**, because a level is not a type. It reports
`nameRefused 2` and `created 2` in the same breath — it rejected both names and built both levels
regardless, leaving two default-named levels in the model. Levels are what the 5,636-element rollback
failure of 2026-09-09 was about (§1c), so a fragment that creates them when it has already decided it
cannot do the job is the one to fix first of the four.

**And the counter-example is in this same file.** `duplicate-sheets`, given no prefix and no suffix,
duplicated NOTHING and named all 17 numbers that would have collided. Same situation, same kind of
fragment, opposite behaviour. It is worth reading before touching any of the four, because it shows the
refusal already has a shape in this library — it is not being invented.

**The first three are also obscured by the `Describe` defect** — each returns its new element as
`"ElementId"` — but fixing that would not prove any of them, because the negative would still have
created something. The naming defect is the one that matters.

`create-levels` is the exception, and it is worth knowing why: it returns `created` as a **list**, so
`Describe` renders a count rather than the bare word. Nothing is hiding its behaviour. It reports
`created 2` and `nameRefused 2` in plain sight, and it is still wrong.

### Value-driven negatives: four tried, none proved, three findings — 2026-09-09

Thirty DRAFT fragments have both legs recorded with a positive that moved and a negative that did not
come back empty. Re-reading them showed the contrast was usually in the wrong place: the negative was a
different *selection*, and a fragment that acts on whatever it is handed acts on that too.
`isolate-elements` isolated 307 either way; `group-elements` grouped whatever arrived.

So four were re-run with the negative driven by the **value** instead —
[`tools/jobs/value-driven-negatives.yaml`](../tools/jobs/value-driven-negatives.yaml). None proved, and
three of the four produced something better.

| Fragment | Positive | Negative | What it means |
|---|---|---|---|
| `check-equipment-connectors` | `sizeTolerance=0` → `mismatched 22` | `sizeTolerance=99999` → `mismatched 21` | **The tolerance barely does anything.** A tolerance of 99999 should make every connector size acceptable; it removed one. This is the `measure-run-quantities` shape — a number the caller supplies that does not change the answer — and a modeller setting a tolerance would get a confident wrong list |
| `isolate-elements` | `view=FloorPlan: M1` → `isolated 22` | `view=Cover Sheet` → `isolated 22`, `viewRefused false` | **Identical answers from different views.** Either `view` is not being honoured or a sheet really can take a temporary isolate. Worth settling: the same shape as `color-by-parameter`, which coloured 22 elements by a parameter that does not exist |
| `set-crop-box-settings` | `changed 1` | `views="Cover Sheet"` → `changed 1` | Changed a crop setting on a sheet. Same count both legs |
| `set-category-visibility` | `changed 0` | `changed 0` | **Not a defect — and the explanation is already in this file.** M1 follows the `Mechanical Plan` view template (`remove-view-template` reported it in the same session), so the view cannot change category visibility at all: the template owns it. This is the trap that `report-category-visibility` was fixed for in §4 — *a view driven by a template answers false to everything*. Any V/G fragment must be proved on a view carrying NO template |

### `--write` serves EXECUTE, not only MODIFY

`isolate-elements` first answered `fragment_threw: Attempt to modify the model outside of transaction`,
and the job file was the reason: it carried `write: false` because the fragment is `risk: EXECUTE`
rather than `MODIFY`. That was a guess and it was wrong.

Temporary Hide/Isolate changes no model data — the fragment says so itself, *"nothing here is saved
into the view"* — but Revit still requires a transaction to call it. Re-run with `--write` both legs ran.

> **`write:` follows whether the fragment needs a TRANSACTION, not whether its risk is `MODIFY`.**
> Only `READ` is refused the write path. An `EXECUTE` fragment that touches the document needs
> `write: true` exactly like a `MODIFY` one.

Four of the six `EXECUTE` fragments are still DRAFT — `isolate-elements`, `set-selection`,
`switch-active-project`, `zoom-to-elements` — and `set-selection` runs in every setup chain without a
transaction, so the need is per-fragment rather than per-risk.

### Tracking (D-53) proved five, and the harness nearly proved a sixth wrongly — 2026-09-09

`set-selection` was still DRAFT while running in **every setup chain in every job file** — everything
proved this week leaned on it. It provides one thing, `selectedCount`, and it is the count of whatever
it was handed, so no arrangement makes it empty and D-53 is the route. It now tracks 22, 9, 10, 10, 307
across five selections.

| Proved by tracking | Field | Answers across five inputs |
|---|---|---|
| `set-selection` | `selectedCount` | 22, 9, 10, 10, 307 |
| `report-geometry-complexity` | `totalTriangles` | 2024, 0, 1320, 3920, 28244 |
| `report-parameter-inventory` | `parameterNames` | 93, 22, 86, 71, 93 |
| `find-untagged-elements` | `elements` | 14, 9, 10, 10, 307 |

`compare-elements` was proved the ordinary way instead, and the reason matters: tracking showed duct
tags giving `differing 0`, so it **can** come back empty. D-53 is for fragments that cannot. Given a
real negative it answered `differing 29` against `differing 0`.

### A harness bug that reads like a clean proof

The tracking runner drives a CHAIN — `select-by-category-name` → `set-selection` → the fragment — and
`prove` prints one provides block per fragment. The first version searched the whole output for the
field name. `elements` is declared by the selector as well as by most fragments under test, so it read
the **selector's** copy and produced five rows that tracked the input perfectly, because they *were*
the input.

**`find-untagged-elements` was promoted on that evidence and had to be reverted.** Its real answer for
the L3 ducts is 235 untagged, not the 307 it was handed. Two guards now, because the first alone is not
enough:

1. Find the fragment's own block by slug, then the field inside it.
2. **Refuse to write any rows unless at least one agrees with what `validate` recorded for that
   fragment running alone.** This is the guard that would have caught it: not one of 22, 9, 10, 10, 307
   matched the recorded 235 or 307.

> Rows that match the input exactly are the thing to distrust. A real describer's answer *differs* from
> what it was given — 22 ducts, 14 untagged.

### Two counters that were not defects, checked before filing

- `compare-elements` answered `comparedCount 8` for every input including 307 ducts. The code is
  `elements.Take(8)` and the comment says *"comparedCount says how many were actually looked at"*.
  Deliberate and documented. Declared `accounting`, along with `identicalCount`.
- `select-subcomponents` answered `elements 0` for all five, agreeing with its own recorded run.
  Nothing in this model has nested components. Content, not code.

**The naming patterns catch a shape, not a meaning.** `comparedCount` ends in `Count`, not `Compared`,
so the work-counter scan missed it entirely. That is the argument for §3h.2 doing the declarations
explicitly rather than leaving the patterns to guess.

### ONE SELECTION CANNOT ARRANGE A TWO-SET FRAGMENT — found 2026-09-09

`find-nearest-elements` refused, and the refusal is the clearest statement of a structural gap that
nothing else in this file names:

> *"Cannot run: `elements (IList<Element>)`, `targets (IList<Element>)` were never supplied. There is a
> selection, but this fragment needs 2 separate sets of elements and one selection cannot say which is
> which. Run the fragments that produce them first. Running anyway would report 0 results, which reads
> as 'there was nothing to find' rather than 'nobody was asked'."*

The setup chain leaves exactly one `elements` behind, and `set-selection` consumes exactly one. A
fragment comparing **two** sets — nearest-to, clashes-against, this-versus-that — has no arrangement at
all in a job file today, however its inputs are typed.

**This is a job-file and chaining gap, not a D-54 one.** D-54 carries the caller's half across as text;
a second set of elements is not text. Closing it means either a chain that can bind two named sets, or
a `--set-b` alongside `--set`. Neither exists.

Named so far: `find-nearest-elements`, `find-clashes` (`against`), `measure-distance` (`first`,
`second`), `compare-elements` would have been one had it not taken its pair from a single selection.

### Two more shapes D-54 refuses, and one that does NOT bind from the chain

§3i lists `FamilySymbol`, `Material`, `OverrideGraphicSettings`, `Curve`, `ParameterValue` and bare
`Element`. Two more, both met on 2026-09-09:

| Shape | Fragment | What it said |
|---|---|---|
| `IDictionary<string, double>` | `check-minimum-clearance` | `rules` is a value the caller supplies. A dictionary has no name to look up, so this belongs with `OverrideGraphicSettings` rather than with the resolvable names |
| `IList<Element>` **as a named need** | `check-room-mep-completeness` | `devices` was never supplied, even with a selection present |

**That second row corrects an assumption made earlier the same day.** `remove-view-template` showed
`views (IList<View>)` resolving from a single name, and it was reasonable to expect `IList<Element>` to
bind from the chain the same way. It does not. A `View` has a name Revit can look up; an arbitrary
element list has none, so a *named* element need has to come from a previous fragment's `provides` and
not from the selection — which is D-29's job, and is exactly what the two-set gap above blocks.

### Two more with no positive case in this model

- `check-flow-direction` — `bothOut 0`, `bothIn 0` on mechanical equipment. No contradictory joints
  exist here. Its two results are also the clearest example in §3h.2 of names that LOOK like
  bookkeeping and are the answer.
- `audit-mep-openings` — `stale 0`, `combined 0`, `unhosted 0` on 307 ducts in L3, the richer
  selection. Consistent with the earlier run on 22, so it is content rather than scale.

### THE MISSING `LIST_*` FRAGMENTS ARE NOW THE BINDING CONSTRAINT — 2026-09-09

§3d has listed the missing `LIST_*` capabilities as a gap since 2026-09-08. After a full day of proving
they are no longer a gap among others: **they are what stops the next batch**, and the count is
concrete.

| Blocked fragment | The value it wants | Why guessing failed |
|---|---|---|
| `set-element-workset` | a workset **id** | `list-worksets` gives the names — *Shared Levels and Grids*, *Workset1* — and not the integers. The fragment reads `ELEM_PARTITION_PARAM` as an int, so a name cannot stand in |
| `set-category-visibility`, `set-view-crop`, `reset-view-graphics`, `set-crop-box-settings` | a view carrying **no template** | Every V/G fragment must be proved on one — a templated view answers "changed 0" because the template owns visibility (§4). `3D View: {3D}` was a guess and the model has no such view. **Nothing lists views**, so there is no way to find a template-free one except by asking a person |
| `create-legend-view` | a legend's exact name | *"No view called Legend: Mechanical Legend"* — a good refusal against a name nothing could have supplied |
| `set-section-mark-visibility` | any way to reach section marks | `select-by-category-name` answers `setup_failed` for `Sections` model-wide and in a plan. There is no `list-sections` |
| `select-by-material` | a material name | Ducts and mechanical equipment both answer `noMaterial` — **this model assigns no materials to MEP elements at all**, which is also why `find-unused-materials` finds 56 unused. Content, but nothing would have told a job-file author that without running a fragment to find out |

**The pattern is the same every time: the fragment is fine, the arrangement needs a name, and the name
can only be got by asking a person or by guessing.** Guessing is what D-54 refuses to do on the
caller's behalf, and it is right to refuse — so the missing half is a way to ASK the model.

`list-sheets`, `list-levels`, `list-grids`, `list-revisions`, `list-worksets` and `list-linked-models`
already exist and are PROVEN, and using `list-sheets` as a setup step proved five fragments in an hour
(§3i). **The route works. There are simply not enough of them**, and two of the ones that exist are one
field short:

1. **`list-views`** — name, type, and whether it carries a template. Unblocks four V/G fragments at
   once and would have saved three wrong guesses today.
2. **`list-worksets` needs the id**, not only the name.
3. **`list-materials`** — the names, and whether anything uses them.
4. **`list-sections`**, or any route to a section mark.

Ranked against §3h, this now sits above everything except making silence illegal. §3h.3 asked for one
command answering *"describe this model"*; this is the same argument arriving from the proving side,
and it is the cheaper half of it.

### `batch-prove` REPORTS A VERDICT ON A RUN THAT NEVER HAPPENED — found 2026-09-09, OPEN

A batch of six MODIFY fragments came back **6 for 6 `POSITIVE EMPTY`**. Six independent content
problems in one batch is not a pattern that happens, so the records were read rather than the summary,
and none of the six had run at all.

Every one had been refused for the lease:

> *"This Revit is in use by another chat, so Heron has refused rather than taking it over mid-job.
> Nothing was sent to Revit. **Refusing to record evidence about a model that will not name itself.**"*

**Heron behaved perfectly.** It refused, it said nothing was sent, and it declined to write a record —
which is exactly right. The defect is that `batch-prove` then judged **the record already on disk from
an earlier run**, and reported verdicts about it as though they were this batch's.

The evidence it judged was not merely old, it was about a **different arrangement**. `flip-elements`
reported `POSITIVE EMPTY` from a record whose negative reads *"run with categoryName=Ducts,
inViewOnly=FloorPlan: L3"* — while the job file asked for duct tags in M1.

**A date check would not catch this.** Both records say `2026-09-09`. What gives it away is the session
id inside the `model` line: `session 17356` against the live `session 32940`.

> A verdict about a run that did not happen is worse than a crash, because it is filed as a finding.
> This is the same family as the `ALREADY` hole closed earlier — reporting on something that is not
> this run — and it wants the same kind of fix.

**The fix, and it is small:** `validate` already stamps `model` with the session id. Read the record's
mtime or its session before judging, and refuse to report on one this invocation did not write.
`NO RECORD` and `REFUSED` verdicts already exist for exactly this shape of answer.

Confirmed: **fourteen of the eighteen fragments proved on 2026-09-09 came from the live session.** The
other four were proved by TRACKING, where the D-53 evidence *is* the tracking rows and those were run
fresh — but their positive phase came from the earlier session, so they are being re-run rather than
argued for.

### A RUN BOUND THE PREVIOUS SELECTION AND CALLED IT A PASS — seen ONCE, 2026-09-10, NOT REPRODUCED

Recorded because it is the same family as the section above — a confident answer about something that
was not what was asked — and because it nearly went into a job file as a proved negative.

`dimension-rooms` was run twice back to back on the write path, first with `categoryName=Rooms` and
then with `categoryName=Walls`, each with `--setup select-by-category-name --setup set-selection`. The
second run returned **`created 2` and `notARoom 0`** — identical to the first — and its record says:

> `elements from the selection (1)`

One element, when its own setup had just asked for four walls. Had it been read as the negative leg it
would have said "the negative created two dimensions as well", which is a finding about nothing.

**It has not reproduced.** The same Walls run in isolation binds `(4)` and returns `created 0`,
`notARoom 4` — twice — and three further deliberate Rooms-then-Walls trials, run back to back to
provoke it, were all correct. Five clean runs against one bad one.

**No mechanism is claimed.** §1d records that a rolled-back write clears the selection, and a race
between that restore and the next run's setup would fit — but that is a guess, and a guess written down
here would be read as a finding later. What is certain is only what the record says.

> `bound` is the field that catches this, and it is already printed. **Read `elements from the
> selection (N)` and check N against the category you asked for**, before reading the result. A wrong
> selection does not announce itself in the answer.

Marked **NEEDS_REVIEW**, not OPEN: one unreproduced observation is not yet a defect.

### `validate` DOES NOT APPLY THE RISK GATE — found 2026-09-11, OPEN

**The proving path is the one place `risk_refusal` is not called, and proving is where fragments run
against a real model with `write.enabled` on.**

Found by accident. `create-workset` came back PASS in a sweep, and the same fragment run through
`fragment` refused:

> *"create-workset is declared risk: ADMIN, and Heron does not run those yet — HeronPermissions puts
> Publish and Admin out of reach for Phase 0 and Phase 1. **Nothing was sent to Revit.**"*

But the run record says it ran, on the live model:

```
positive ok=True  doc=Snowdon-scratch_ajmal.al
  createdNames 2 item(s) [HERON WS A, HERON WS B]
  findings     "2 workset(s) created: HERON WS A, ..."
```

**Read in the client**, `risk_refusal` is called in exactly two places —
[`cmd_fragment`](../mcp/client/heron_bridge_client.py) and `cmd_prove`. `cmd_validate` does not call
it at all.

| Path | Gated? |
|---|---|
| `fragment` — one run, no proof | **yes** |
| `prove` — the old chain runner | **yes** |
| `validate` — **the proving path, with `--write`** | **NO** |

### Eight gated fragments were run through it in one session

| Fragment | risk | status |
|---|---|---|
| `create-workset` | ADMIN | DRAFT |
| `add-project-parameter` | ADMIN | DRAFT |
| `transfer-project-parameters-between-documents` | ADMIN | DRAFT |
| `upgrade-family-files` | PUBLISH | DRAFT |
| `export-view-image` | PUBLISH | DRAFT |
| `export-parameters-to-csv` | PUBLISH | DRAFT |
| `export-families` | PUBLISH | DRAFT |
| **`export-views-to-fbx`** | **PUBLISH** | **PROVEN** |

**One of them is already promoted.** `export-views-to-fbx` was proved and signed on 2026-09-11 through
a path `HeronPermissions` says must not run in Phase 0 or 1. Its evidence is real — a file was written,
and the negative is structural — but it was obtained through a gate that was supposed to stop it.

**DECIDED BY THE OWNER, 2026-09-11: the permission tier wins.** `export-views-to-fbx` is back to
`heron-status: DRAFT`.

**Its proof block was KEPT, deliberately.** The evidence is real and it carries Ajmal PS's signature —
throwing that away would lose a true record of a run that happened. What was withdrawn is the CLAIM
the status makes, not the evidence behind it. When `PUBLISH` is allowed by the phase, or when
`validate` is given the gate and the run is re-taken, the fragment is one promotion away rather than
one proof away.

> A proof and a status are different assertions. The proof says *this ran and this came back*; the
> status says *this library stands behind it*. Only the second was wrong here.

That leaves the second question open and it is worth a `D-` number if the answer is the other one:

**Whether the gate belongs on `validate` at all.** Two readings, and nothing here settles which:

- the permission tier is the authority, and a `PUBLISH` fragment has no business running yet, so the
  status goes back to `DRAFT` until the phase allows it; **or**
- the gate is about what Heron does *for a user*, proving is a different act, and `validate` is
  correctly exempt — in which case the exemption should be **written down and deliberate**, not the
  absence of a line.

> The exports all wrote into this session's scratchpad, and the ADMIN runs were inside the rolled-back
> TransactionGroup. Nothing landed anywhere it should not have. **That is luck about where the paths
> pointed, not the gate doing its job.**

### `can_promote` WILL PROMOTE AN UNPROVEN FRAGMENT TO `PROVEN` — found 2026-09-10, OPEN

This is in the machinery the whole *"the machine never signs"* discipline stands on, so it is written
out in full rather than summarised.

**Reproduced on a fragment with no proof at all:**

```
>>> f = heron_fragment.load('brain/fragments/connect-open-ends')
>>> f.status
'DRAFT'
>>> f.proof
None
>>> heron_fragment.proof_problems(f)
[]
>>> heron_fragment.can_promote(f, 'PROVEN')
(True, 'well-formed, and PROVEN needs no proof')
```

`PROVEN` **is** in `NEEDS_PROOF`, and `can_promote` **does** call `proof_problems` for it. The check
inside is the problem — it reads the fragment's **CURRENT** status, not the **TARGET** it is being
asked about:

```python
if proof is None:
    if frag.status in NEEDS_PROOF:      # <- where it IS, not where it is GOING
        problems.append("... status is %s but there is no proof ...")
```

A `DRAFT` has status `DRAFT`, which is not in `NEEDS_PROOF`, so the "there is no proof" branch cannot
fire during the one call that decides the promotion. By the time `status` reads `PROVEN` the promotion
has already happened.

**`proof_problems` is not wrong — it is being asked the wrong question.** Its own docstring says
*"Whether this fragment's proof, **if it has one**, is a proof at all"*: it is an AUDIT of a fragment
already sitting at `PROVEN`, and [`validate`](../brain/heron_fragment.py) uses it that way correctly.
`can_promote` reuses it as a transition gate, and as a transition gate it can never refuse.

> The same shape as the comment sitting ten lines above it — *"The gate existed and nothing stood on
> it."* One layer up, and still true.

**What it does NOT mean.** Nothing has reached `PROVEN` this way. Every promotion so far has been a
person naming signed fragments after reading each draft, and `accept` is unaffected — it still records
the signature and still refuses to set a status. The hole is that nothing would have STOPPED a wrong
promotion, not that one happened. It was found by checking eligibility before offering to promote, and
seeing three **unsigned** fragments come back READY beside two signed ones.

**Not fixed here.** The obvious change — pass `target` to `proof_problems`, or test
`target in NEEDS_PROOF` rather than `frag.status` — touches the promotion gate itself and belongs to
the owner, not to a proving session. Recorded and left, per the standing rule.

### A PROOF TAKEN WITH `--in` NAMES THE WRONG MODEL — found 2026-09-10, OPEN

D-30 asks for *"a recorded run against a **named real model**"*. `proof.model` is that name, and when
`--in` is used it is **the model that happened to be in front, not the one the run went to.**

**Demonstrated in one run.** `validate --in "Snowdon-scratch_ajmal.al" list-levels`, with `Project1`
active:

```
record model line : Project1 work_ajmal.al (3,471 elements), Revit 2024, session 24688
phase document    : Snowdon-scratch_ajmal.al
levels            : 11 item(s) [Parking, L1 - Block 35, L1 - Block 37, ...]
```

Eleven levels with Snowdon's names — so the run went where it was told. Only the headline is wrong,
and the element count is wrong with it: 3,471 is Project1's, not Snowdon's.

**The cause, read rather than guessed** ([`heron_bridge_client.py`](../mcp/client/heron_bridge_client.py)):

```python
model = "%s (%s elements), Revit %s, session %s" % (
    opening.get("document"), ...)
```

`opening` is the probe that identifies the **ACTIVE** model — its own failure text says *"Could not
identify the active model"*. `in_document` is passed separately to each phase, which is why every
phase's `document` field is right and only the stamp is wrong. The refusal sitting immediately above
that line reads *"Refusing to record evidence about a model that will not name itself"* — and then it
mis-names one that did.

**Three PROMOTED fragments carry it.** All three came from a job file pinning `in:
"Snowdon-scratch_ajmal.al"` while `Project1` was the active document:

| Fragment | `proof.model` says | but both phases ran on |
|---|---|---|
| `create-callout` | `Project1 work_ajmal.al (3,445 elements)` | `Snowdon-scratch_ajmal.al` |
| `create-key-schedule` | `Project1 work_ajmal.al (3,445 elements)` | `Snowdon-scratch_ajmal.al` |
| `duplicate-type` | `Project1 work_ajmal.al (3,445 elements)` | `Snowdon-scratch_ajmal.al` |

**The evidence is not invalid, and that distinction matters.** The runs happened, both phases are
recorded, and each names the document it used — the proof contradicts itself *inside one block*, which
is how it was caught. What is wrong is the field a reader trusts first.

**Three more are FLAGGED, not asserted** — `transfer-line-styles-`, `transfer-object-styles-` and
`transfer-view-filters-between-documents`, whose model line says `Snowdon Towers Sample Architectural`
while the phases ran on `Snowdon-scratch_ajmal.al`. These fragments take a `sourceDocumentTitle`, so
the named document is a genuine participant and the mismatch may read differently. Needs a look by
someone who took them.

**`restamp` is NOT the remedy** — it re-records fingerprints only, and refuses anything whose code
moved after the proof. Nothing in the repository currently rewrites `proof.model`.

> The five fragments proved on `Project1` tonight are unaffected: the pin and the active document were
> the same, so the stamp is true. That is luck, not a safeguard.

**Not fixed here.** The change is one line in the proving client — stamp the resolved document rather
than the active one — plus a decision about the three already promoted. Both belong to the owner.

### `switch-active-project` ASKS THE WRONG `UIDocument`, AND HAS NEVER SWITCHED — found 2026-09-10, OPEN

§3b-i said this one *"needs a second project open"* to be tested properly. Two were open on 2026-09-10,
and it does not work.

```
target=Project1 work_ajmal.al   viewName=1 - Mech
  activeBefore  Snowdon-scratch_ajmal.al
  switched      false
  findings      Revit refused to change to the view "1 - Mech" in "Project1 ..."
```

Verified afterwards the way the purpose demands — `REPORT_OPEN_DOCUMENTS` still reports
`active is "Snowdon-scratch_ajmal.al"`. **The tab bar did not move.**

**The cause, read in two files.** The target view is collected correctly from the target document:

```csharp
var openable = new FilteredElementCollector(wantedDoc)      // the OTHER project
...
uidoc.RequestViewChange(chosen);                            // line 150
```

but `uidoc` is bound by the executor to the **active** document
([`RevitFragment.cs:145`](../revit/Heron.Revit.Addin/RevitFragment.cs)):

```csharp
uidoc = app.ActiveUIDocument;
```

So it hands Snowdon's `UIDocument` a view belonging to Project1, and Revit throws. The fragment's own
comment states the rule it then breaks — *"a view belongs to one document"*. Switching needs a
`UIDocument` for the **target** document, not the active one.

**This means the fragment has never done the thing it exists for.** The earlier record that reported
`switched true` took the *"already the active project"* branch — asking to switch to the project you
are already in, which returns success having done nothing and is documented as correct. Every path
that would actually move between projects throws.

**It was NOT the transaction.** Run first with `--write` and then without, the refusal is identical —
consistent with its own line *"Opens no transaction and needs none… That is why it is EXECUTE rather
than MODIFY."*

### …and it could not be PROVED even once it is fixed

Its only `role: result` is `switched`, a **bool** — and the judge reads every boolean as zero, however
it arrives:

```python
if isinstance(value, bool):     return 0
if text.lower() in ("true", "false"):   ...
```

That is deliberate and right — `looks_empty`'s own docstring gives the reason: a flag that reads the
same in both cases *"would make an empty answer impossible for it to demonstrate."* But the
consequence here is that `switched: true` counts as zero as well, so **the POSITIVE reads empty too**.

> A declared result that is a BOOL cannot carry EITHER leg. This is the sibling of the NAME case
> above, and worse: a name at least makes the negative impossible, while a bool makes both impossible.
> Either the fragment gains a countable result beside the flag, or D-53 tracking is the route.

**Not fixed here.** The `UIDocument` fix is one line in a fragment implementation, the proving question
is a contract change, and both are the owner's.

### ONE PERSON, ONE `HERON_CLIENT_ID`

The lease identifies a **chat**, not a person. Four ids were in use for one afternoon's work —
`proving-2026-09-09b` by hand, `tracking-run` and `tracking-set-selection` by two scripts, and
`heron-batch-prove` chosen by the runner itself — and to Heron that is four chats competing for one
Revit. Each refusal reads exactly like a fragment failing.

> Pick one id for a working session and put it in every script and every command. A second id is a
> second chat, and the lease is doing its job when it refuses the second one.

This also explains the round-one wipe-out earlier the same day, when eleven jobs failed immediately
after a hand-run `count` — the same collision, differently dressed, and it was misread then as an
arrangement fault.

### Six MODIFY fragments, six honest refusals — 2026-09-09

Run on the tags-negative pattern. None proved, none is defective, and **every one said in words why
not** — which is the behaviour §3h.1 is asking the twelve silent ones for.

| Fragment | What it answered | What it needs |
|---|---|---|
| `rename-elements` | `planned 22`, **`collisions 21`**, `renamed 0` | Every duct here is a `Tees`, so a global find/replace makes 21 duplicate names — and it refused rather than creating them. **This fragment may not be provable by find/replace at all** on same-typed elements: the arrangement has to produce unique names, which `find`/`replaceWith` cannot |
| `trim-extend-elements` | *"This squares off exactly TWO elements — 22 were given. Nothing…"* | Exactly two elements. `select-by-category-name` cannot narrow to two, so this is the two-set problem above wearing a different coat |
| `remove-parameter-value` | `alreadyEmpty 22`, and *"0 cleared, 22 already empty. **An empty field and a zero are different.**"* | A populated WRITABLE parameter. `System Type` and `Comments` are both blank on every duct in this model |
| `flip-elements` | `cannotFlip 10` on air terminals | A family with a hand to flip. The flippable ones here — doors, windows — are in the architectural link |
| `set-design-option` | `available 0`, *"Nothing was copied into a design option"* | Design options. This model has none |
| `set-mep-justification` | `0 run(s) set to 100 mm horizontal and 100 mm vertical` | Ran and moved nothing. Worth a second look at the sit-down — justification should apply to a duct run |

**Read the `remove-parameter-value` refusal twice.** *"An empty field and a zero are different"* is the
whole of §3h.1 in seven words, written by a fragment that already gets it right.

### FIFTEEN NARROWER SELECTORS WERE ALREADY PROVED, AND EVERY JOB FILE USED ONE — 2026-09-09

Four fragments refused within an hour of each other, each perfectly clearly, and each for the same
reason:

| Fragment | What it said |
|---|---|
| `rename-family` | *"The selection covers 4 different families, and one name cannot…"* |
| `place-mep-fitting` | *"a fitting joins two, three or four runs — 22 were given. Two makes an elbow, a union or a transition, three a tee, four a cross"* |
| `rename-elements` | `planned 22`, `collisions 21` — every duct in this model is a `Tees` |
| `trim-extend-elements` | *"This squares off exactly TWO elements — 22 were given"* |

**The conclusion first drawn from this was that the library cannot select more narrowly than a
category. That is wrong, and it was one edit away from being written into this file as a gap.**

`select-by-category-name` hands over every element of a category in a view, and it is the only selector
any job file had used. The library also has, all `PROVEN` and all usable as a setup step:

`select-by-family`, `select-by-connection-status`, `select-by-parameter-value`,
`select-by-numeric-parameter`, `select-types`, `select-by-workset`, `select-by-mep-system`,
`select-by-pin-state`, `select-by-phase`, `select-by-design-option`, `select-by-insulation`,
`select-from-link`, `select-visible-in-view`, `select-with-warnings`, `select-scope-boxes`,
`select-by-categories`.

> **This is the third time in one day the same shape has appeared:** a capability existed, was proved,
> and was invisible because nothing named it where somebody writing a job file would look. First the
> `list-*` fragments as a setup chain (§3i), then `views (IList<View>)` resolving from a single name,
> now the narrow selectors. **The library is further ahead than the job files are.**

That is an argument for §3h.4 — generating the job file from the fragment library rather than typing
it — considerably stronger than the six mistyped input names it was first written about. A generator
reading `contract.needs` would have offered `select-by-family` for a fragment that renames a family,
because the contract says what it wants.

### The contradiction this exposes, still open

§3 records *"every duct end in this model is connected"*, and `find-dead-ends` was set aside twice on
that basis. But `place-mep-fitting`, run on 2026-09-09, reported **`openEnds 22 item(s)`** against the
same 22 ducts in M1.

Both cannot be true. `select-by-connection-status` — itself `PROVEN`, taking `wantOpenEnds` — asks the
model the question directly, and until it answers, **`find-dead-ends` is recorded as blocked on a claim
that has not been re-checked since it was made.**

### What this says about where the proving goes next

**142 to 160 on 2026-09-09.** The write engine is not the constraint — fragments proved through it all
day and rolled back cleanly. What is left is blocked on three things, in this order:

0. **Naming what already exists.** Three separate capabilities were proved and invisible today — the
   `list-*` fragments as a setup chain, `views` resolving from one name, and fifteen selectors narrower
   than a category. Nothing needs building for these; they need to reach the person writing the job
   file. This is the cheapest item on the list and it outranks the rest.
1. **`LIST_*` fragments**, above. This stopped more batches today than anything else, and the route is
   already proved: `list-sheets` as a setup step proved five fragments in an hour.
2. **A resolver for named Revit objects** — `FamilySymbol`, `Material`, and the view-like ones. Half of
   the never-run MODIFY fragments wait on it.
3. **A way to bind TWO sets of elements**, which no job file can express today.

All three are offline work and none needs Revit to build. **Making silence illegal (§3h.1) still comes
first**, because everything above makes proving faster while that one makes Heron honest.

---

## 3j. THE REFUSALS OF §3h.1, IN FRONT OF A MODEL — 2026-09-09

**Eight of the eleven fragments PR #50 changed have now been run with both legs** against
`Snowdon-scratch_ajmal.al` (9,628 elements, Revit 2024). Drafts are in `brain/proof-drafts/`,
**unsigned**, and `heron-status` is untouched on all eight — promotion follows a signature, not a run.

The arrangement is `tools/jobs/reprove-refusals.yaml`, kept so the next round starts from what was
learned rather than from the fragment list.

| Fragment | Positive | Negative — the refusal |
|---|---|---|
| `measure-run-quantities` | `areas 22`, `lengths 22` | `measure=ZZZNOTHINGHERE` → `areas 0`, `lengths 0` |
| `color-by-parameter` | `carrying 22`, `coloured 22` | `carrying 0`, `coloured 0` |
| `add-schedule-fields` | `added 2`, `schedulesSeen 3` | `added 0`, `availableFields 0` |
| `add-schedule-combined-field` | `added 1` | `added 0`, `notASchedule 1` |
| `remove-schedule-fields` | `hidden 1` | `hidden 0` |
| `set-schedule-appearance` | `changed 1` | `changed 0` |
| `set-schedule-filters` | `filtered 1` | `filtered 0` |
| `set-schedule-sort-group` | `appliedOrder 1` | `appliedOrder 0` |

**The element count was 9,628 before and 9,628 after**, and the document's *unsaved changes* marker
cleared — every write rolled back. §1c's check, run either side of the batch as that section asks.

### Two of them show the FIX rather than merely passing

**`add-schedule-fields` is §3e end to end.** The positive reports `handed 3, schedulesSeen 3` — three
`ScheduleSheetInstance`s resolved through to the `ViewSchedule` behind them, which is exactly what it
could not do before. Two took the column; the third answered `unknownField [Comments]` and handed back
**256** real names in `availableFields`. Three outcomes, correctly separated, on one call.

**`color-by-parameter` carries the defect and the repair in one counter.** `carrying 22` on the
positive, `carrying 0` on the negative. Before PR #50 there was no such counter: `valueOf` answered
`""` both for a parameter that is absent and one that is empty, so `carrying 0` still coloured 22.

### One still blocked, and it is the arrangement

`edit-text-values` — `setup_failed` on **both** legs. `Text Notes` is not selectable on the sheet
`Notes, Symbols & Schedules`, so the seven notes §3c found live in some other view and **nothing in
the library lists which**. That is §3d's `LIST_*` gap again, and it is now the only thing between this
fragment and a proof: its refusal path is written and compiles, and has never run.

Its draft is in `brain/proof-drafts/` and says `NOT ESTABLISHED` for both legs. **It must not be
accepted.**

### Two were skipped by decision, not by failure

| Fragment | Why |
|---|---|
| `export-schedule-to-csv` | `risk: PUBLISH`, and §5 defect 9 means the client refuses to *send* anything above Modify |
| `add-revision-cloud` | needs `revisionId` as an **ElementId**, which D-54 does not resolve — the same wall as `set-global-parameter` in §3i |

### Three things about ARRANGING these, each learned by getting it wrong

**1. `open-view` CANNOT BE A SETUP STEP.** Setup runs every step down `run_fragment_read`
(`heron_bridge_client.py`), and `open-view` is `risk: EXECUTE` — so step 0 is refused and **every leg
of every job fails**, which reads as a broken arrangement. Putting it in the chain made a batch of
seven *worse* than leaving it out, where at least the negatives had run. Run it as its own `validate`
before the batch instead; give the negative a view name that does not exist and the positive's view
stays on screen.

**2. BOTH LEGS MUST SELECT IN THE SAME VIEW, or the arrangement depends on what is open.** Selection
is view-scoped (rule 3). A positive selecting on a sheet and a negative selecting in `FloorPlan: M1`
works only until some earlier job leaves the wrong view on screen — and then six jobs fail at setup
with no hint that the *view* is what moved. The fix is to find the contrast **inside one view**:
`Schedule Graphics` against `Title Blocks` on the same sheet is a real contrast and needs no view
change at all.

**3. ONE REVIT, ONE SESSION.** Two chats proving at once cannot share a Revit, and the failure does not
say so — §3i already records that a stale lease reports `Could not identify the active model`. Two more
shapes of the same collision were met today: `ping` succeeding while every real request times out (the
bridge answers on its own thread; the API thread is busy), and a countdown that goes **up** rather than
down, because each refused attempt counts as activity and renews the very lease it is waiting on.
Stop touching it and wait, or stop the other session.

**And the MCP tools and the command line hold DIFFERENT leases.** One `revit_use_this_model` from a
chat takes the lease under that chat's id; the CLI pins its own and is refused, `release` cannot hand
back a lease it does not own, and the MCP server's id is a fresh uuid per process so it cannot be
matched. Pick one and stay on it for the whole run.

---

## 4. FIXED during proving — kept because the shape returns

| Fragment | What was wrong |
|---|---|
| `report-category-visibility` | Used `CanCategoryBeHidden` as a **gate**. That call means *"may this view change it"*, not *"is it hidden"* — and a view driven by a template answers false to everything. It skipped all 68 categories and reported *"nothing is switched off"* while 18 genuinely were, **Levels and HVAC Zones among them**. Most views in a real project carry a template, so it was wrong in the ordinary case. `GetCategoryHidden` needed no help: asked of the view it returns exactly what the template returns when asked directly. Fixed 2026-09-08 |
| `color-by-parameter` | **A parameter that does not exist coloured 22 elements anyway.** `valueOf` answers `""` both for a parameter that is not there and for one that is there and empty, so every element landed in the single `(no value)` group and the view was painted one colour — a drawing that *looks* grouped and is grouped by nothing. The parameter is now counted across the set, and where **not one** element carries it nothing is coloured and `refused` says so, handing back the names the first element does carry. A MIXED selection is unchanged: an element without the parameter still joins `(no value)` and is still coloured, which is the existing decision and a deliberate one. Fixed 2026-09-09 |
| `measure-run-quantities` | **`measure=ZZZNOTHINGHERE` gave an answer identical to `measure=area`.** Anything not containing "area" fell through to length, so the input had no effect and there was nothing to vary between the two legs of a proof. Both measures are now matched against a stated list of words, and a word in neither is refused with nothing measured. An **absent** `measure` still means length — an input nobody supplied is a different thing from one somebody got wrong, and that path was working. Fixed 2026-09-09 |
| `edit-text-values` | **`field=text` reported all seven notes `absent` and changed nothing**, which reads as "there was nothing to do". Three unworkable inputs are now refused before any element is read — an empty `field`, a `mode` that is not prefix/suffix/replace (`transform` handed every value straight back, so everything landed in `untouched`), and an empty `text`. Two more are refused after looking, safe because neither path writes: the field is on **not one** element handed in, and the field is not **text** on any of them. Where a refusal is decided after looking, the list the unusable input filled is cleared — `absent: 7` on a run that did nothing reads as a finding and is not one. Fixed 2026-09-09 |
| `add-schedule-fields` | Cast to `ViewSchedule` and nothing else, so a schedule **placed on a sheet** — the only form a person can select — slid past it and it answered `added 0`, `availableFields 0` with no refusal. Now resolves a `ScheduleSheetInstance` to the schedule behind it, the way `report-schedule-definition` and `read-schedule-contents` already did, and refuses when not one element handed in was a schedule, or when no column names were given. Fixed 2026-09-09 |
| `add-schedule-combined-field` | Same `ViewSchedule` cast. It already named what it could not use in `notASchedule`, so it was never silent — what it could not do was **see** a schedule on a sheet. Resolution added; nothing else changed. Fixed 2026-09-09 |
| `remove-schedule-fields` | Same cast, and silent with it: `removed 0`, `presentFields 0`, no refusal. Resolution added, plus a refusal when nothing handed in was a schedule and when no field names were given — the second also removes a null dereference of `fieldNames`. `refusedInUse` is left as it was: a field a rule still points at is a finding **about the schedule**, which is a different sentence from "I could not use what you gave me". Fixed 2026-09-09 |
| `set-schedule-appearance` | Same cast, answering `changed 0` — which reads as "the column was already like that". Resolution added, and its existing `refused` now carries both "nothing handed in was a schedule" and "no column was named". Fixed 2026-09-09 |
| `set-schedule-filters` | Same cast. **And its unknown-match-type refusal was in `cannotFilterBy`**, which is a finding about the schedule — so an unusable request looked like something learnt, and a negative case could never come back empty. Resolution added, a `refused` output declared, and the match-type refusal moved into it along with "no column was named" and "nothing handed in was a schedule". `cannotFilterBy` keeps what it should: the fields Revit itself will not filter on. Fixed 2026-09-09 |
| `set-schedule-sort-group` | Same cast, answering `sorted 0` — which reads as "already in that order". Resolution added, plus a refusal when nothing handed in was a schedule and when no sort field was named. As with the filter fragment, `cannotSortBy` stays a finding about the schedule and `refused` is the fragment declining. Fixed 2026-09-09 |
| `export-schedule-to-csv` | Same cast. A missing export folder and a selection holding no schedule both came back as "nothing came out"; only the first said why. Resolution added, and the second now refuses too. Fixed 2026-09-09 |
| `add-revision-cloud` | **The `ViewSchedule` §3e lists it for is a view-type guard** (`view is ViewSchedule`), not a cast of a selected element — it never consumes a schedule as input, so the placement fix does not apply and was not made. **What was wrong is next to it: `viewRefused` was one bool covering two causes**, *"the view will not take a cloud"* **and** *"that revision does not exist"*, and the file's own comment admitted the conflation. Those have opposite fixes — change the view, or make the revision — and a caller told only `viewRefused true` goes looking at the drawing when the revision is what is missing. `viewRefused` now means exactly its name; a missing revision is its own sentence; and a new `refusalReasons` carries the words for both, naming **which** of the five view types refused and why. The three ways a single element misses out (not visible in this view, too small to cloud, Revit declined the rectangle) each get a sentence carrying a **count** rather than one line per element. An empty `elements` with a good view and a real revision — four zeroes that read as "nothing to cloud" — is refused too. Fixed 2026-09-09 |
| `set-view-section-box` | **`viewRefused` meant two opposite things.** It was set when the VIEW could not carry a section box — a plan, a section, a template — and again when the view was a perfectly good 3D view and **nothing handed in had any geometry**. Opposite fixes: open a 3D view, or hand over something measurable. **This is also §3f's recorded doubt, and it could not be settled while the two shared a word:** the stronger negative the owner asked for — a 3D view whose selection has nothing to enclose — reported `viewRefused true` exactly like the weak one. It now leaves `viewRefused` **false** and says in words that the view was fine and the selection was not. `viewRefused` means only its name; a new `refusalReasons` carries the words and names **which** view type refused. Two further silences closed while in there: an **empty** `elements` in a good 3D view answered `applied false`, `enclosed 0`, `noGeometry 0`, `viewRefused true` — blaming the view for an empty selection; and **a margin negative enough to turn the box inside out** was applied and reported `applied true`, cutting the view to an empty screen, which reads as though the model had been deleted. **The proof of 2026-09-09 went stale the moment this was fixed** — the fragment leaves outputs it did not have, so the fingerprint no longer matches — and `heron-status` was set back to `DRAFT` on the owner's instruction the same day. The `proof:` block is KEPT: nothing it recorded is contradicted, it is the record of what was actually run, and `can_promote` refuses to carry a stale proof back to PROVEN, so keeping it costs nothing and re-proving it starts from a written arrangement rather than a blank page. Fixed 2026-09-09 |

---

## 5. HERON'S OWN DEFECTS found by proving

> **This heading said *"six still open"* from the day it was written until 2026-09-16, and by then it was TWENTY-NINE.** Ninety-four rows were appended under a heading nobody re-read - the prose-total drift [`NEEDS-CHECKING.md`](NEEDS-CHECKING.md) records against itself, happening here too. **No number is typed here now. Derive it:**
>
> ```bash
> python tools/open-defects.py
> ```

| # | Defect | State |
|---|---|---|
| 1 | No way to pass a view, category or name. **288 of 308 fragments blocked**, and the refusal said so in its own words | Fixed — [D-54](DECISIONS.md) |
| 2 | The chain silently dropped what a filter narrowed. `find-untagged-elements` cut 625 to 560 and the next fragment counted 625 | Fixed — identity, not name |
| 3 | `refused` read as a finding. **117 fragments**, every write among them, could never pass a negative case | Fixed — `role: accounting`, [D-52](DECISIONS.md) |
| 4 | `"(null)"` unreadable, so **every creator's** negative case was flagged | Fixed |
| 5 | `deploy-addin.ps1` installed a build for the wrong Revit release. Symptom was *"Revit cannot run the external application"* and nothing else | Fixed — the script refuses it now |
| 6 | `validate` sent writes down the READ path for one commit. Revit refused politely, the fragment reported `refused` like any decline, and it read as intermittent worksharing behaviour | Fixed — the line carries why |
| 7 | **The naming heuristics were dead code.** `provide_role()` answers `"result"` for an entry with no `role:` key, and that default went into the map the judge consults - so every declared name looked explicitly declared, and D-51/D-52's patterns never ran. **102 names across 134 fragments** (`scanned`, `unplaced`, `noConnectors`, `notASheet`) were judged as findings. Found proving `select-scope-boxes`, whose negative had every result at zero and `scanned: 5` | Fixed - the judge-set and the role-map are separate arguments now |
| 9 | **A fragment's OWN risk level was never enforced.** The gate reads the OPERATION's risk from the tool registry — Golden Rule 19, and right — and `run_fragment_write` is declared Modify. So a fragment declaring `risk: ADMIN` or `PUBLISH` ran under a Modify gate and nobody was consulted, though `HeronPermissions` says those *"are not reachable in Phase 0 or Phase 1 at all"*. **12 fragments are above Modify**, and `create-workset` (ADMIN) created one on the first try. The hole existed before `run_fragment_write` and was harmless — no transaction, so nothing could happen. Building the write path made it real | Fixed — the client refuses to SEND one. Be honest about what that is: a guard against a mistake, not a boundary against malice. A caller skipping this client is unaffected, and Golden Rule 19 forbids closing that by sending the risk over the wire, because then the caller decides how dangerous its own request is |
| **8** | **`Describe` renders a valid `ElementId` and `ElementId.InvalidElementId` as the same word, `"ElementId"`.** Real, and still worth fixing — an answer that cannot say whether a thing was created is a poor answer. **But it has NO named victims, and this row claimed two it did not have.** `dimension-mep-runs` and `dimension-family-instances` were listed here as unprovable because of it. Both were **proved on 2026-09-09** with `dimensionId` still reading as `ElementId` in all four phases. An unreadable value is SKIPPED by the judge, not counted as content — the opposite of what this row asserted. What actually blocked them was `problem`, an explanation field, undeclared and therefore judged as a finding; `role: accounting` on `problem` and `noReference` proved both in one run. **The lesson is the one in §3h.2, not this one:** an undeclared role is more likely to be the blocker than a defect in the renderer | **OPEN, AND NOW THE BIGGEST SINGLE UNBLOCK LEFT — revised 2026-09-10.** **This row's claim that it has "NO named victims" is no longer true.** Proving the creators against Project1 returned `POSITIVE UNREADABLE  created ElementId cannot be read as a quantity` for **create-grid, create-floor and create-ceiling** in one batch. All three declare `created` as a bare `ElementId`, not a list, so `Describe` renders the word `"ElementId"` (`RevitFragment.cs:1930`) and `_as_count` returns None. **29 fragments declare a bare `ElementId` result and 15 DRAFT ones have no other countable result at all**, so this single line is the whole of what blocks them: `create-3d-view`, `create-callout`, `create-ceiling`, `create-filled-region`, `create-floor`, `create-grid`, `create-key-schedule`, `create-legend-view`, `create-plan-view`, `create-schedule`, `create-sheet`, `create-text-note`, `create-view-filter`, `duplicate-type`, `place-mep-fitting`. The fix stays one small block and is version-safe if it uses `ElementId.InvalidElementId` and `ToString()` — never `IntegerValue`, which is deprecated at 2024, and never the constructor, which changed from int to long there. `_as_count` already reads `"(null)"` as zero, so the invalid case needs no new rule. **AND IT IS THE WHOLE FIX, WHICH WAS NOT OBVIOUS.** The batch also said *"the negative did not come back empty either"* for `create-floor` and `create-ceiling`, which looked like a second, separate defect — an undeclared `findings` role, §3h.2. **It is not.** `findings` is already in `heron_validate.NOTE_KEYS` and is skipped as prose. The run records show the real cause: `create-ceiling`'s NEGATIVE reported `created "ElementId"` while its own findings said *"A ceiling needs at least three boundary points and 2 were given"*. It created nothing — `created` held `InvalidElementId` — and the renderer gave it the same word as a real id. So ONE line repairs BOTH legs of all three. `create-grid` already proves the shape works: its negative renders `created "(null)"` and reads as empty today, because that path assigns null rather than InvalidElementId. **Patched 2026-09-10** — `Describe` now returns `"(null)"` for `InvalidElementId` and `"1 item(s) [id N]"` otherwise; compiles clean on 2020, 2024 and 2027. Needs the deploy and a Revit restart to take effect |
| **10** | **A proof draft claims the model was left unchanged, and nothing ever checked.** Every write phase's record ended *“run inside a transaction and ROLLED BACK, so the model was left exactly as it was”*, appended from the `writing` FLAG. On 2026-09-09 it was false: `edit-text-values` renamed 17 sheets, the rollback did not hold (§1c), and both the run record and the draft asserted the model was untouched. **The client was only repeating the add-in.** `RevitFragment.WithVerdict` said *“NOTHING WAS KEPT”* on the strength of `apply` alone — the request, not the result — and `SafeRollBack` was `void`, so the two ways a rollback silently does nothing (**a group whose status is not `Started`, so the call is skipped; a `RollBack()` that throws, so it is swallowed**) left no trace for anyone to report. See §1c: this is the first concrete mechanism for why three rollback failures have no explanation | **OPEN - DEPLOYED 2026-09-16 18:06, NOT YET PROVED.** The word OPEN leads deliberately. A row that needs a RUN is open, and `tools/open-defects.py` reads the first word: writing `DEPLOYED` here dropped it out of the count for the few minutes between this edit and the next, which is the same two-marks-one-fact failure this file recorded against `E11`-`E15` the same day. All three releases were rebuilt and redeployed on the owner's PC and the deployed binaries were read back to confirm each got its own framework. **That clears this row's precondition and nothing more** - the fix can now be RUN, and D-30 wants it run. Any proof taken before 18:06 still carries the old unchecked sentence. Fixed in code 2026-09-09: `SafeRollBack` returns whether Revit reports the group `RolledBack` afterwards, `WithVerdict` says **THE ROLLBACK DID NOT REPORT SUCCESS** when it does not, a `rolledBack` flag rides on the reply, and the client writes only what the reply says — naming an add-in build that cannot answer rather than assuming. Compiles 2020–2027. **It needs a rebuild and a Revit restart to take effect, so proofs taken before that still carry the old unchecked sentence.** `RevitWrite.SafeRollBack` carries the same defect on its error paths (*“rolled back completely, so the model is as it was”*) and was deliberately NOT changed on the day the model was already damaged |
| **11** | **The setup chain's values are wiped before the fragment under test runs.** `validate` arranges the case by running a setup chain — `select-by-category-name`, `set-selection`, and anything after it — and then calls `run_fragment(..., reset=True)` (`mcp/client/heron_bridge_client.py:1377`), which sends `chain: "reset"`; the add-in answers that with `Forget(client)` (`revit/Heron.Revit.Addin/RevitFragment.cs:232`). **The chain the setup just filled is discarded a moment later.** So a setup chain can hand over only what lives in Revit's OWN state — the selection — and never a value. `select-by-category-name → set-selection` survives because `set-selection` writes the real selection; `read-element-parameters → group-and-count` cannot. Both `group-and-count` and `sum-by-group` came back `needs_unbound: 'values (IDictionary<ElementId, string>)' was never supplied` on 2026-09-10, with the setup chain reporting success — a failed arrangement reports `setup_failed`, so the producer demonstrably ran and demonstrably left nothing. **This is part of why every job file ever written uses `select-by-category-name` and nothing else**; §3h.4 read that as job files lagging the library. **The obvious fix is WRONG.** `reset = not setup` would leave `elements` sitting in the chain, and the chain OUTRANKS the selection (`RevitFragment.cs:917` — "1. THE CHAIN", then "2. THE SELECTION"), so every existing arrangement would start binding from the chain, `set-selection` would become decorative in the middle of it, and the proofs would quietly begin testing something other than what they say. It has to be opt-in per job | **FIXED AND PROVEN AGAINST A MODEL, 2026-09-10.** Opt-in per job, as this row asked. `keep-chain: true` in a job file, `--keep-chain` on the client, and `reset = not keep_chain` for the fragment under test. **THE DEFAULT IS UNCHANGED**, so every arrangement written before today still resets and `set-selection` keeps outranking nothing - which is the whole reason it is not a global switch. `keep-chain` with no `setup:` is REFUSED in both places: there would be nothing to keep, and what survived would be whatever an earlier run left behind, which is the hazard the reset exists for. Proven by dry-run without Revit - a job carrying it emits `--keep-chain` after `--setup`, a job without it is byte-identical to today's command line, and one with no setup is refused before Revit is touched. **The producer -> consumer pair it unblocks (`read-element-parameters` -> `group-and-count`) has NOT been run against a model. **PROVEN THE SAME NIGHT, AS AN A/B ON ONE ARRANGEMENT.** `select-by-category-name -> set-selection -> read-element-parameters -> group-and-count` against Snowdon-scratch, 22 Ducts in `FloorPlan: M1`. WITHOUT `--keep-chain`: both phases `needs_unbound`, which is this row reproduced exactly. WITH it: both `ok`, the positive bound *"values from read-element-parameters (22)"* and returned `groups 3 item(s) [[Supply Air, 9], [Exhaust Air, 8], [Return Air, 5]]` - and 9 + 8 + 5 = 22, so the arithmetic is checkable rather than merely non-zero. The negative used `parameterName: Comments`, blank on all 22 ducts, and bound *"values from read-element-parameters (0)"* for `groups 0 item(s)` - an honest empty arranged from the model's own state rather than from a name it has not got. A draft is waiting for a signature. **AND IT IMMEDIATELY EXPOSED A SECOND, SMALLER DEFECT - THE CHAIN NAMES THE WRONG PRODUCER.** `sum-by-group` was proved the same night on a TWO-producer chain (`read-element-parameters` for `values`, `measure-run-quantities` for `quantities`), which row 11 is what made possible at all. Its record reads *"values from measure-run-quantities (22); quantities from measure-run-quantities (22)"* - but `measure-run-quantities` does not provide `values`; only `read-element-parameters` does. **The BINDING is right and only the LABEL is wrong**: `values` tracked `parameterName` exactly across the two phases, 22 for `System Type` and 0 for `Comments`, while `quantities` stayed 22 in both. The cause is that the origin string is built from `chain.By`, a single name holding the LAST fragment that wrote to the chain, rather than from whichever provide actually carried that name. It could not show up before, because a chain could only ever have one producer. **Filed, not fixed** |
| **12** | **The add-in Revit 2024 is running predates D-67, so a point cannot be typed in — and THAT is what gates the arrangeable library, not the resolver.** `select-in-region` and `check-surface-fit` both refused on 2026-09-10 with *"A point cannot be typed in yet. The Revit API works in feet and this library talks millimetres, so which unit the number is in has to be settled before one can be accepted."* **That sentence exists nowhere in the source.** `FromRequest` has handled `XYZ` since D-67 (`RevitFragment.cs:1582`), and the comment at 1515 records that a point *was* refused "until the unit was settled". The deployed binary is dated **2026-09-08 23:06**; `RevitFragment.cs` was last written **2026-09-09 23:14** — a full day newer. The HANDOVER row claiming the add-in was *"rebuilt and redeployed to Revit 2024 on 2026-09-09"* is wrong. **This is the same pending rebuild as rows 8 and 10, and it is no longer true that nothing is waiting on it:** D-67 is what "took the arrangeable library from 6 to 40", and none of that widening is in the binary Revit has loaded | **OPEN.** Found 2026-09-10. `dotnet 10.0.303` is on the machine and `deploy-addin.ps1` refuses while Revit is open — *"a loaded assembly cannot be replaced"* — so it needs Revit closed |
| **13** | **An `Element` caller value binds a TYPE where all twelve fragments mean an INSTANCE, and answers emptily instead of refusing.** `FromRequest` sends `Element` to `OneElement`, which is one line — `OneOfClass(doc, typeof(ElementType), ...)` (`RevitFragment.cs:1389`) — and `OneOfClass` returns `found[0]` **with `problem` left null** when the name matches once. So a typed name resolves *successfully* to a type object and the fragment runs. The resolver's own doc comment (`RevitFragment.cs:1362`) already names the split: a TYPE to build with (`wallType`, `floorType`, `ceilingType`, `regionType`, `runType`, `hostType`) versus a specific INSTANCE (`reference`, `target`, `source`, `run`, `start`, `host`) — and says instances *are refused*. **They are not refused. They are mis-bound.** Every one of the twelve DRAFT fragments declaring a bare `Element` means an instance: `align-elements`(reference), `join-geometry`(target), `select-touching`(target), `match-element-type`(source), `distribute-along-run`(run), `trace-connectivity`(start), `select-by-host`(host), `measure-distance`(first,second), `read-ceiling-grid`(ceiling), `select-group-members`(group), `measure-available-fall`(upstream,downstream), `filter-elements-by-type`(exemplar). Read against the code: `select-by-host` takes `host.Id` — a TYPE's id — and asks whether any instance's `Host.Id` equals it, which nothing can; `filter-elements-by-type` calls `exemplar.GetTypeId()`, and a type has no type; `trace-connectivity` enqueues `start` and walks connectors a type does not have. **All three return 0 with no error and a findings line naming the type**, which is the confident meaningless result this file exists to refuse. **`generate-jobs.py` lists all twelve as arrangeable** — it checks that the name RESOLVES, never that the resolution fits — so a batch built from it spends twelve slots and returns twelve POSITIVE EMPTY verdicts that read as fragment defects. Of the 39 it offers, **27 are genuinely testable.** The fix is a refusal, not a resolution: `OneElement` should decline when the need-name is one of the instance-meaning set, because "which duct" is a question a text value cannot answer | **FIXED IN CODE 2026-09-10. THE GENERATOR HALF WAS ALREADY LIVE; THE REFUSAL WAS DEPLOYED 2026-09-16 18:06** - six days after this cell was written, and it read *"needs a deploy"* throughout. **Deployed is not proved:** the refusal can now be triggered, and has not been. The need's NAME decides, because the type cannot: a bare `Element` resolves to a type only when the name ends in `Type`, and every other one is refused saying to select it in Revit instead. **An allowlist, not a list of the thirteen instance names** - a blocklist would silently mis-bind the next contract that says `Element duct`, which is this defect exactly. Measured rather than reasoned: all **14** bare `Element` needs across 12 fragments mean an instance and **not one ends in `Type`**, because the six type-meaning names were narrowed to `WallType`, `FloorType`, `CeilingType`, `FilledRegionType`, `MEPCurveType` and `HostObjAttributes` on 2026-09-09. `generate-jobs.py` carries the SAME rule - a disagreement there would emit a job that always declines - and drops exactly those twelve: **32 emitted becomes 20**, checked against the twelve by name. Compiles clean on 2020, 2024 and 2027, verified in a scratch output folder so the shared one was untouched. **The C# half is NOT deployed and NOT proven against a model** |
| **14** | **`select-in-region` converts millimetres to feet a SECOND time, so every volume it is given is 304.8x too small.** D-67 put the conversion in the resolver: `OnePoint` returns `MillimetresToFeet(millimetres)` (`RevitFragment.cs:1215`), so an `XYZ` need arrives **already in feet**. `select-in-region` then divides again — `lowMm.X / MillimetresPerFoot` (`fragment.cs:47-48`) — because its header still says *"MILLIMETRES TO FEET BY 304.8, PLAIN ARITHMETIC"*, written when fragments owned the conversion. **Measured, not reasoned:** a box of `-500000,-500000,-500000` to `500000,500000,500000` (1,000,000 mm across, half a kilometre either side of origin) returned `elements 0` against Mechanical Equipment, and its own findings line called the volume *"3281 x 3281 x 3281 mm"* — which is 1,000,000/304.8, the value after ONE conversion, printed in feet and labelled mm. The box actually searched was about **10.7 feet** across. **Both halves of the fragment's own stated safeguard fail together:** it promises *"a value handed in already converted shows up as an absurd volume, not as a quiet zero"* — the value IS handed in already converted, and it produced exactly the quiet zero. **It is the only one.** All 29 XYZ-taking fragments were checked line by line: the other 28 either convert a SCALAR (`spacingMm` in `array-elements`, `gapMm` in `stack-tags`) or use `1.0/304.8` as a one-millimetre tolerance (`move-elements`, `snap-to-grid`, `mirror-elements`), both correct. `create-grid` carries the rule explicitly: *"adding one 'to match the other fragments' would place every grid 304.8 times too far out."* **No signed proof is affected — all 29 are DRAFT** | **OPEN.** Found 2026-09-10. Fix is to delete the division and the report's mm label, not to add conversions elsewhere |
| **15** | **A PROOF'S `model:` LINE NAMES THE ACTIVE DOCUMENT, NOT THE ONE THE FRAGMENT RAN AGAINST.** `cmd_validate` records `model` from the opening `count_elements` call, which answers for the document IN FRONT - and then every phase runs against `--in` when a job pins one. The two disagree inside the same proof: `create-callout`, `create-key-schedule` and `duplicate-type` were signed on 2026-09-10 with `model: Project1 work_ajmal.al (3,445 elements)` while each case's own text ends *"on Snowdon-scratch_ajmal.al"*, which is where they actually ran. **The element count is the worst part** - 3,445 belongs to a model those three fragments never touched, so a reader checking the evidence against the model would be checking the wrong one. The evidence itself is sound; only the header lies. It went unnoticed until a job used `in:` for the first time, which is why no earlier proof shows it - every one of them ran against whatever was in front. **The proofs already signed do not need re-running**: the run document is recorded in the case text, so the correction is readable from the file rather than from a repeat. | **OPEN.** Found 2026-09-10 |
| **16** | **AN EMPTY LIST CANNOT CROSS THE CHAIN, SO A LIST-CONSUMING FRAGMENT CAN NEVER BE GIVEN ITS NEGATIVE CASE.** `Shape` returns null for an empty `IList<ElementId>` (`RevitFragment.cs`, *`return ids.Count == 0 ? null : value;`*), the binder reports *'nothing usable survived'*, and the run stops with `needs_unbound`. **THERE IS NO SUCH RULE FOR A DICTIONARY**, which is how the same night's `group-and-count` bound *"values from read-element-parameters (0)"* and returned `groups 0` perfectly happily. Found proving `describe-blank-parameters`, which consumes `blank` and `absent` from `read-element-parameters`, and the correlation is exact rather than inferred: with `parameterName: Comments` the producer left blank=22 and absent=0, and ONLY `absent` came back unmet; with `System Type` it left blank=0 and absent=0, and BOTH did. **THE RULE IS RIGHT FOR THE SELECTION AND WRONG FOR THE CHAIN, and it is one code path serving both.** An empty SELECTION means nobody picked anything, and refusing is correct. An empty list from a PRODUCER is an answer - *nothing was blank* - and refusing it makes D-30's second leg unreachable for every list-consuming chain fragment, because the negative case is precisely the one that empties the producer. **Could not appear before 2026-09-10**: until row 11 was fixed no value crossed the chain at all. | **OPEN.** Found 2026-09-10 || **17** | **`remove-parameter-value` calls `ClearValue()`, which Revit allows only on SHARED and PROJECT parameters - so it cannot empty a BUILT-IN one, which is most of what a modeller would want to clear.** `fragment.cs:67` is `parameter.ClearValue();` inside a try/catch that records the exception, and against `Comments` on ten ducts in `Project1 work_ajmal.al` every one came back refused: *"Radius Elbows / Tees (id 925641): Cannot call ClearValue on ..."*, with `cleared 0`, `alreadyEmpty 0`, `missing 0`, `readOnly 0`. **Retried on a single duct Ajmal had filled by hand, to rule out the arrangement: same refusal.** So it is not an empty-parameter problem and not a selection problem - `ClearValue` is simply the wrong call for a built-in parameter, where emptying means setting `""`. Comments, Description and Mark are all built-in. **THE FRAGMENT IS HONEST ABOUT IT**, which is why this is a defect and not a trap: it reported `refused 10` and named every element and Revit's own words, rather than reporting `cleared 0` and reading as a fragment that found nothing to do. Its NEGATIVE leg is sound and was proved in the same run - asked for `HERON_NO_SUCH_PARAMETER` it returned `missing 8` and said *"8 element(s) have no parameter called ..."*. Found 2026-09-12 on the first arrangement the new MODIFY-setup path made possible: `write-element-parameters` fills `Comments` inside the same transaction group, so there was genuinely a value to clear | **OPEN.** Found 2026-09-12. The fix is to set `""` when the parameter is built-in and keep `ClearValue` for shared and project parameters - not to report the refusal more loudly, which it already does well |
| **18** | **`set-mep-justification` writes `RBS_CURVE_HOR_OFFSET_PARAM` and `RBS_CURVE_VERT_OFFSET_PARAM` with `Parameter.Set()`, and on a real duct run the value does not take.** Against **191 ducts** in `FloorPlan: L2` of `Snowdon-scratch_ajmal.al`, asked for 50 mm horizontal and 25 mm vertical, every single one came back *"id 1365662: 2 of 2 offset(s) did not take the value asked for"* — `set 0`, `withoutOffsets 0`, `notCurves 0`. **The fragment is not guessing and not lying**: it checks `IsReadOnly` first, calls `Set()`, then reads the parameter back with `AsDouble()` and only counts the ones that changed. Both parameters exist and both report writable; the read-back simply returns the old value. **THE UNITS ARE NOT THE PROBLEM.** `horizontalOffsetMm` is a plain `double`, which the add-in passes through untouched — only an `XYZ` is converted at the boundary — so `/ MillimetresPerFoot` in the fragment is correct, and this is NOT the defect found the same day in `select-in-region`. **ONE HYPOTHESIS WAS TESTED AND DISPROVED RATHER THAN ASSUMED**: a `doc.Regenerate()` between the write and the read-back changes nothing — still `set 0` — so it is not a staleness problem. What remains is that Revit derives these offsets from the justification and the run's own size and recomputes them, in which case they cannot be driven by parameter write at all and the fragment needs a different API. Found 2026-09-13 proving Group A | **OPEN.** Found 2026-09-13. The next step is to establish whether ANY API can set these — Revit's own justification dropdown moved by hand, then read the two parameters back, which says whether the value is settable at all before any more code is written. Its NEGATIVE leg is also unusable as arranged: `Duct Fittings` returned `withoutOffsets 0`, so fittings DO carry these parameters and are not the "cannot take this" case the arrangement assumed |
| **19** | **A ROLLBACK REPORTED AS DONE DID NOT UNDO A RENAME. FOURTH OCCURRENCE, AND THE THIRD OF THIS EXACT SHAPE.** `rename-elements` was run through `validate` — **no `--apply`**, so the transaction group must roll back — against 11 FloorPlan views in `Snowdon-scratch_ajmal.al`. It reported `renamed 8` and Heron printed *"run inside a transaction, and Revit reported the transaction group **ROLLED BACK** afterwards"*. **The eight views are still renamed**: `L2` reads `HERONL2`, `L1 - Block 35` reads `HERONL1 - Block 35`, and six more. Read back with `find-views` minutes later, in a separate process. | **OPEN.** Found 2026-09-13. |
| | **WHY IT SURVIVED THREE HUNTS, AND IT IS NOT ABOUT SIZE.** The element count was **9,638 before and 9,638 after** — a rename changes no count, so every count-based check reports clean. That is the same signature as the third failure (seventeen renamed sheets) recorded against **A16**, and it is why A16's own proof does not cover this: A16 was witnessed on `align-mep-elevation` moving **9 ducts**, and verified by reading **duct elevations off the Properties palette** precisely because counting cannot see an edit. **A MOVE ROLLS BACK AND A RENAME DOES NOT, AND A16 ONLY EVER WATCHED A MOVE.** | The next step is to establish the boundary rather than assume it: run one rename and one move through the SAME path, reading the value back both times, and find what separates them — `Element.Name` is a property setter rather than a parameter write, which is the first thing to rule in or out |
| | **WHAT IT MEANS FOR EVERY MODIFY PROOF ALREADY SIGNED.** Every one of them rests on *"nothing was kept"*, and that sentence is now known to be false for at least one kind of edit. It does **not** invalidate a positive case — the fragment still did what it reported — but it does mean **the model was left changed** by proofs that said it was not. Two MODIFY drafts from 2026-09-13 (`rename-elements`, `set-view-underlay`) are being held unsigned for this reason, not because their evidence was weak |  |
| | **THE BOUNDARY, NARROWED THE SAME DAY AND BY MEASUREMENT.** `duplicate-views` was run through the identical path minutes later — same model, same `validate`, no `--apply` — and **made 11 new views**. Read back with `find-views` afterwards: **still 11 floor plans, not 22.** So the created views WERE rolled back. Together with the renames that were not, that puts the line between **creating an element (rolls back)** and **setting `Element.Name` on an existing one (does not)** — which is a much smaller question than "rollback is unreliable", and it is the one to take to the next sitting |  |
| | **THAT NARROWING IS WRONG, AND THE SAME FRAGMENT DISPROVED IT.** `rename-elements` was run through this path **twice** on 2026-09-13, minutes apart, same model, same job file, no `--apply` either time. **Run 1 renamed `L1 - Block 35` to `HERONL1 - Block 35` and it STUCK. Run 2 then planned `HERONL1 - Block 35` to `HERONHERONL1 - Block 35` — the double prefix is recorded verbatim in the signed proof's `planned` list — and that one ROLLED BACK.** The views read `HERONL1 - Block 35` today: one rename survived, the next did not. So it is **not** "creating rolls back, renaming does not". Same fragment, same kind of edit, two different outcomes | **OPEN, and now WORSE than when it was written.** A rollback that fails *sometimes* is harder to design around than one that fails predictably, and it means no MODIFY proof's "nothing was kept" can be trusted on the strength of having held once |
| | **WHAT DIFFERED BETWEEN THE TWO RUNS, AS FAR AS IT IS KNOWN.** In run 1 the NEXT job in the same batch file (`set-view-underlay`) was refused with `bad_request_value` before it reached Revit; in run 2 it succeeded. Each job is its own `validate` process, so that should be irrelevant — which is exactly why it is written down rather than dismissed. **THE EXPERIMENT THAT WOULD SETTLE IT**: run one rename ten times in a row, reading the name back after each, and see whether the failures cluster or scatter. Until that is done, "renames do not roll back" and "rollback is intermittent" are both consistent with the evidence, and only the second is safe to assume |  |
| **20** | **A SHEET ACCEPTS A HIDE, SO `hide-elements` HAS NO REFUSAL TO PROVE ITS NEGATIVE WITH — AND IT REPORTS THE COUNT IT ASKED FOR RATHER THAN ONE IT READ BACK.** Handed 191 ducts and `view = "Cover Sheet"` (a ViewSheet) it returned **`hidden 191`, `viewRefused false`, `cannotHide 0`** — with `permanent: false`, which is the ONE path the fragment gates, on `CanUseTemporaryVisibilityModes()`. So a sheet reports true for that, and `Element.CanBeHidden(sheet)` reports true for 191 model elements that are not on it. **THIS IS RECORDED AS AN OBSERVATION, NOT A PROVEN FAULT.** Revit accepted the call, so `hidden = ids.Count` (`fragment.cs:56`) is arguably an honest count of what Revit took. What it is NOT is a count of what visibly changed, and it was never read back. | **OPEN as a question, 2026-09-13.** |
| | **WHY IT IS WORTH THE ROW ANYWAY: ITS OWN SIBLING REFUSES TO DO THIS.** `show-elements` gates the same situation and says so in its header — *"Reporting `shown = 4` there would be a lie of exactly the shape this project keeps meeting: the number ASKED FOR reported as the number that HAPPENED."* It returns `temporaryModeCleared` instead. Two fragments written to the same contract, one declining to report an unverified count and the other reporting it. **Whichever is right, they should not disagree** | The next step is cheap and decides it: hide model elements on a sheet, then read `IsHidden` back on one of them. If it is false, `hidden 191` is wrong and the fix is a read-back. If it is true, the fragment is right and the NEGATIVE needs to come from somewhere else — `cannotHide` on an element Revit genuinely refuses, which is a different arrangement entirely |
| **21** | **`export-families` HAS NO PATH THAT CAN RUN IT. Both doors are shut, for opposite reasons.** Its header is explicit: *"OPENS NO TRANSACTION, AND MUST NOT. Each family is a SEPARATE document. A transaction in the project would not cover them."* But `validate --write` — the only route a job file has — runs every fragment inside a `TransactionGroup` plus a `Transaction`, and `Document.EditFamily` cannot be called with a transaction open in the project. So `fragment.cs:103` throws every time and `fragment.cs:104` reports it honestly: against 2 families matching "Duct" and again against 2 matching "Air Terminal", every one came back ***"Revit refused to open it for editing"*** with `saved 0`, `nameCollisions 0`. **THE OTHER DOOR REFUSES IT BEFORE REVIT IS TOUCHED**: `heron_bridge_client.py fragment export-families` answers *"export-families is declared risk: PUBLISH, and Heron does not run those yet - HeronPermissions puts Publish and Admin out of reach for Phase 0 and Phase 1."* | **OPEN.** Found 2026-09-13 working the export cluster |
| | **THE INCONSISTENCY IS THE PART TO FIX FIRST.** One command refuses PUBLISH outright and the other runs it inside a transaction it must not have. Whichever is right, they disagree — and the disagreement is what made this look like a fragment fault for two arrangements running. **It is not a fragment fault**: the matching worked both times and named exactly which families it found, and the refusal is Revit's, reported verbatim. **This blocks the whole PUBLISH class**, 8 DRAFT fragments, not just this one | The decision is whether a PUBLISH fragment gets a route that opens NO transaction. Until then no exporter that edits a family can be proved, and a job file asking for one is asking for something the runner cannot give |
| **22** | **`snap-to-grid` TREATS ITS SPACING AS FEET, SO A 600 mm CEILING GRID IS A 183 METRE ONE — AND IT REPORTS SUCCESS.** `fragment.cs:43-47` computes `Math.Floor((at.X - anchor.X) / spacingX)` and `anchor.X + (cellX + 0.5) * spacingX` where `at.X` and `anchor` are Revit internal FEET — `anchor` because the add-in's point parser converts every XYZ, `at` because that is what a Location returns. **`spacingX` and `spacingY` are plain `double`s, which the binder passes through UNTOUCHED**, so the 600 a modeller types stays 600 and is used as 600 feet. The file knows the unit it is in: `fragment.cs:24` is `var tolerance = 1.0 / 304.8;   // one millimetre, in feet`. **The guard at line 28 passes anyway** — `600 > 0.00328` is true — so nothing refuses. | **OPEN.** Found 2026-09-13 by Codex review of PR #136, then confirmed by reading the implementation |
| | **THIS INVALIDATED A PROOF THAT HAD ALREADY BEEN SIGNED AND PROMOTED.** On 2026-09-13 it was run against 70 air terminals in `FloorPlan: L2` with `spacingX: 600`, `spacingY: 600`, returned `snapped 70`, and was signed and promoted on the strength of it. **It did move 70 diffusers — onto a 183 metre grid.** The positive case proved the fragment DOES something; it did not prove it does the documented thing, and nothing in the run record could have shown the difference. Promotion reversed the same day; the proof block is KEPT, because the run is a true record of what happened. | **THE SAME CLASS AS ROW 19's SIBLING, `select-in-region`** — a unit mismatch between a binder that converts XYZ and a scalar that arrives raw. The house rule is visible in `array-elements`, which does it correctly: `var step = spacingMm / MillimetresPerFoot;`. The fix is those two divisions, and then a proof that reads a diffuser's position back rather than counting how many moved |
| **23** | **`set-section-mark-visibility` READS AN UNPLACED VIEW'S SHEET NUMBER AS "ON A SHEET", SO IT HIDES NOTHING AND SAYS NOTHING.** `fragment.cs:48-54` reads `BuiltInParameter.VIEWER_SHEET_NUMBER` and sets `onSheet[name] = !string.IsNullOrEmpty(sheetNumber)`. **Revit fills that parameter with its placeholder `---` for a view that is on no sheet**, which is not empty, so every view reads as placed. Line 103 then does `if (onSheet[name]) continue;` and every mark is skipped. Run on 2026-09-13 against `1 - Mech` of `Project1 work_ajmal.al` with `onSheetsOnly: true`: **`markers 5`, `unmatched 0`, `hidden 0`, `shown 0`** — it found all five marks, matched all five to their views, and acted on none. **Those five are four elevations and the owner's section**, confirmed by category count in the same view: `Elevations` returns 4 and the section is the fifth `OST_Viewers` element. So the section drawn specifically to test this was seen, matched, and left visible. **`find-unplaced-views` on the same model the same minute reports `placedCount 0` across 18 views, and `list-sheets` reports 0 sheets.** So the model has no sheets at all and the fragment believed every view was on one. | **OPEN.** Found 2026-09-13 proving it on the owner's own model, after he drew a section specifically so this could be tested |
| | **THE FAILURE IS SILENT, WHICH IS WHAT MAKES IT WORTH A ROW.** The whole purpose line is *"Shows only the section and elevation marks whose view is actually on a sheet, and hides the rest - the pre-issue tidy-up"*. In a project where nothing is placed yet it should hide EVERY mark; it hides none and returns `hidden 0` with no refusal and nothing in `unmatched`, which reads as "there was nothing to do" rather than "I could not tell". A pre-issue tidy-up that quietly does nothing is worse than one that fails. | The fix is to treat `---` as unplaced — the placeholder is not a sheet number. Safer still is to stop reading the parameter and ask the view whether a `Viewport` references it, which cannot be spoofed by a placeholder. **The proof must then read a mark's `IsHidden` back**, not count how many the fragment says it hid |
| **24** | **A `ViewSheet` ACCEPTS VIEW-GRAPHICS WORK THAT IS MEANINGLESS ON IT, AND THREE FRAGMENTS REPORT SUCCESS FOR IT.** Run on 2026-09-13 against sheet **A101** of `Project1 work_ajmal.al`, each handed the same 9 ducts that live in `1 - Mech` and are not on that sheet at all: `set-view-crop` returned **`applied true, enclosed 9, viewRefused false`**; `isolate-elements` returned **`isolated 9, viewRefused false`**; and `hide-elements` had already returned **`hidden 191, viewRefused false`** on a sheet in Snowdon ([row 20](#)). **Each of the three declares `viewRefused` as exactly this guard and a sheet trips none of them.** `isolate-elements` asks `view.CanUseTemporaryVisibilityModes()` — its own comment calls that *"Revit answering for itself"* — and a sheet answers yes. `set-view-crop` sets `CropBoxActive` and reads it back; on a sheet that read comes back true. | **OPEN.** Found 2026-09-13 after Ajmal made a sheet specifically so these two could be given a view that refuses |
| | **TWO SEPARATE CONSEQUENCES, AND THE SECOND IS THE ONE THAT MATTERS IN THE FIELD.** First, **none of the three can build a negative case from a sheet** — which is what this run set out to do and is the smaller loss. Second, and worse: **each reports a confident count for work with no observable effect.** A modeller told *"isolated 9"* on a sheet has been told something true about the API call and false about their drawing. | The question to settle before any of them is proved: **should a sheet be refused explicitly?** Revit says the operation is legal, so the fragment would be overruling it — which is a decision, not a fix, and belongs in `DECISIONS.md`. Until then these three need their negative from somewhere other than a view type. **A LEGEND WAS THEN MADE AND TRIED, AND IT ACCEPTS TOO**: `isolate-elements` on `Legend 1` with its 15 text notes returned **`isolated 15, viewRefused false`**. That is six view types now — FloorPlan, CeilingPlan, ThreeD, Section, ViewSheet and Legend — and `CanUseTemporaryVisibilityModes()` answers YES to every one of them in this model. **There may be no view type at all that trips this guard**, which would mean `viewRefused` is unreachable rather than merely unreached, and the fragment can never satisfy D-30 by view type |
| **25** | **A SINGULAR `Element` NEED AT `source: request` CANNOT BE BOUND BY ANY ROUTE, AND THE REFUSAL SENDS YOU DOWN ONE THAT DOES NOT EXIST.** Twelve DRAFT fragments declare one - **14 needs across 12 fragments**, every one meaning *this particular element*: `filter-elements-by-type.exemplar`, `align-elements.reference`, `match-element-type.source`, `join-geometry.target`, `trace-connectivity.start`, `measure-distance.first`/`.second`, `measure-available-fall.upstream`/`.downstream`, `select-group-members.group`, `select-by-host.host`, `select-touching.target`, `read-ceiling-grid.ceiling`, `distribute-along-run.run`. **All three doors are shut.** Typed: `OneElement` refuses any need whose name is not type-shaped, deliberately and correctly. Selection: `BindNeeds` offers it only when `IsElementList(type)` is true, and `Element` is not a list - and separately `src == "request"` is excluded from `selectable` before that. Chain: nothing in the library provides a value under any of those names. | **OPEN.** Found 2026-09-13 trying to prove `filter-elements-by-type` |
| | **THE MISLEADING PART IS THE MESSAGE, AND IT COST AN ARRANGEMENT BEFORE IT WAS SPOTTED.** Handed a typed id the refusal reads *"...**SELECT IT IN REVIT** and run this again."* Selecting it and running again gives the OPPOSITE refusal - *"'exemplar (Element)' is a value the CALLER supplies, not something the model holds"* - because a request-sourced need never consults the selection. **Two refusals, each pointing at the other.** Both were seen in the same minute on `Project1 work_ajmal.al`, with exactly one duct selected by `select-in-region` and `set-selection`. | The 2026-09-10 fix that produced this was right to refuse rather than mis-bind - the comment at `RevitFragment.cs:1570` explains why an allowlist beats a list of instance names. What is missing is a ROUTE. Either a request-sourced singular `Element` should accept the selection when exactly one thing is selected, or these 12 contracts should declare the need without `source: request` so the selection path can reach it. **Until one of those, none of the 12 can be run at all**, and they should not be counted as "needs a value" in the sweep |
| **26** | **`create-mep-system-type` DECLARES TWO RESULTS AND NEITHER CAN EVER BE READ AS A QUANTITY, so it cannot pass the runner however well it works.** Run on 2026-09-13 against `Project1 work_ajmal.al` it did exactly the right thing in both legs — positive: `created` = **`MechanicalSystemType`**, `classification` = **`SupplyAir`**, with `refused` carrying *"'HERON_TEST_SYSTEM' was copied from 'Supply Air' and is clas..."*; negative, asked to copy from `ZZZNOSUCHSYSTEM`: `created` **null**, `classification` **empty**, and *"no system type called 'ZZZNOSUCHSYSTEM'"*. **A person reading that has a complete proof.** The runner cannot: `created` returns the OBJECT, which reports as the literal text `MechanicalSystemType`, and naming `classification` with `expect:` fails the same way — *"classification SupplyAir cannot be read as a quantity"*. | **OPEN.** Found 2026-09-13 |
| | **THIS IS THE CONTRACT, NOT THE RUNNER.** Refusing an object is right — the skill says so in as many words: *"An `OverrideGraphicSettings` object is not a count that could have been zero."* A result has to be something that could have been zero, and neither of these is. **The fix belongs in the fragment**: `created` should leave an id or a count the way every other creating fragment does — `create-legend-view` leaves `created 1 item(s) [id 926117]` and was proved on it an hour earlier. `classification` is genuinely a word and is fine as a result for a reader; it just cannot be the one the machine judges. | Also noted the same run: **`export-views-to-dwg` requires a NAMED DWG export setup** and refuses an empty one on purpose — *"name the DWG export setup. Revit's defaults produce a file that opens perfectly and fails the recipient"*. `Project1 work_ajmal.al` has none saved, so that fragment is blocked by the model rather than by itself. Its NEGATIVE leg is already sound: pointed at `Q:/heron-no-such-drive` it answered *"there is no folder at ... It is not created here on purpose - a mistyped path"* |
| **27** | **`create-wall` BUILDS AT 3000 FEET WHEN ASKED FOR 3000, AND IT IS `PROVEN`. A LENGTH SCALAR HAS NO UNIT RULE IN THIS LIBRARY AND THE TWO HALVES DISAGREE.** Built on 2026-09-13 in a fresh model (`test projject`, Revit 2024) with `height=3000`, meaning 3 m. It returned `created 4 item(s)` and `findings [4 wall(s) built on 'Level 1' at **914400 mm** high, unconnected]`. **Read back from a different fragment minutes later**, `report-bounding-box` on the same four walls answered `combinedSizeMm` **`8200 x 6200 x 914400 mm`**, `maxZ 3000`, `minZ 0`, bound `elements from the selection (4)`. The plan footprint is exactly right — 8000 + 200 of wall thickness — so the `XYZ` run converted correctly and **only the scalar did not**. 3000 × 304.8 = 914400: the height was used as FEET. `impl/any/fragment.cs:61` passes `height` straight to `Wall.Create` and line 75 multiplies it by 304.8 *for the message*, so the fragment reports the wall it actually built and the sentence is true. **THE CONTRACT SAYS NOTHING.** `height: double, source: request` carries no unit and no comment; the impl header says *"Lengths are internal FEET"*; the caller-facing side says the opposite — `OnePoint` is documented in MILLIMETRES and the add-in's own refusal for a bad `double` reads *"Type digits only - 250 or 250.5, not 250mm"*. | **FIXED 2026-09-13 — [D-71](DECISIONS.md).** Ajmal was asked and answered: **a number a caller types is millimetres, always.** 32 values across 23 implementations now convert once, at the top, before first use; `check-fragments-compile` is clean on all eight releases. The one refusal is `select-by-numeric-parameter.tolerance`, which compares against whatever parameter was named and cannot know its unit — its own header already said so. **Seventeen `PROVEN` fragments changed code and are now `RE_PROVE`**: their proofs measured a version that no longer runs. Found on the first build of the owner's "let Heron make what it needs" method, and found **only because the thing built was read back** — nothing in the proof that promoted `create-wall` ever measured a wall |
| | **THE SPLIT IS THE FINDING, NOT THIS ONE FRAGMENT.** Measured across all 360 contracts the same hour: **81 length-shaped scalars are taken at `source: request`**, and they follow two opposite conventions with nothing declaring either. **~60 are named `...Mm`** — `spacingMm`, `offsetMm`, `marginMm`, `elevationMm` — and their implementations divide by 304.8. **21 are bare** — `height`, `distance`, `width`, `tolerance` — and their implementations use the number as internal feet. **The clearest proof that it is undecided rather than designed: `create-level` takes `elevationMm` and does `wantedMm / 304.8`; `create-levels`, the plural of the same job, takes `elevations` and uses each one raw.** Both are `PROVEN`. The bare ones, all `PROVEN` unless marked: `create-wall.height`; `check-equipment-clearance` (`frontClearance`, `sideClearance`, `backClearance`, `topClearance`); `create-grids` (`spacingsAcross`, `spacingsUp`); `create-levels.elevations`; `find-dead-ends.stubLength`; `offset-elements.distance` (its header says so outright — *"`distance` is internal FEET"*); `report-coverage.coverageRadius`; `set-mep-size` (`width`, `height`, `diameter`); and DRAFT: `check-minimum-clearance.defaultClearance`, `check-obstructions.maxDistance`, `create-from-room-boundaries.heightAboveLevel`, `move-to-ray-hit` (`maxDistance`, `offsetAlongRay`), `probe-around-elements.maxDistance`, `trace-connectivity.tolerance`, and [`snap-to-grid`](#) (`spacingX`, `spacingY`) which is already [row 22](#) — **the same defect, found twice from opposite directions, three weeks apart.** | **[D-67](DECISIONS.md) settled this for `XYZ` and said so explicitly — a point is millimetres. It did not reach the scalars, and the note in `create-grid` that mentions "a factor of 304.8" is about points too.** The decision owed is one sentence: either every length scalar is millimetres and the ~21 bare ones convert, or the `...Mm` suffix becomes a rule the contract checker enforces so a bare name is a build failure. Until then a modeller cannot tell by looking, and the wrong guess is 304.8× wrong with no error |
| **28** | **68 CALLER VALUES ACROSS 55 FRAGMENTS DECLARE A TYPE THE ADD-IN CANNOT RECEIVE, SO THOSE FRAGMENTS HAVE NEVER EXECUTED A LINE.** Measured 2026-09-13 by extracting every `wanted == "..."` from [`RevitFragment.cs`](../revit/Heron.Revit.Addin/RevitFragment.cs) `FromRequest` — **44 type names are receivable** — and diffing it against every `source: request` need in the library. **All 55 are DRAFT**, which is 36% of the 153 outstanding. The refusal is exact and it names the type, e.g. for `create-duct`: *"Heron can be handed a view, a level, a category, an element type - including a wall, floor, ceiling, filled region, MEP curve or family type - a phase, a view filter, a point in millimetres, a name, a number, or true/false, and lists of most of those. **"MechanicalSystemType" is not one of them yet**, so this fragment still has no way to receive it."* | **MOSTLY FIXED 2026-09-13 — 68 needs across 55 fragments is now 15 across 15.** `FromRequest` receives **64 type names where it received 44**: the six MEP type classes, `View3D`, `Material`, `RevitLinkInstance`, `SpatialElement`, `ViewDuplicateOption`, `Color` and `IList<Color>`, and `ElementId` with its list forms through a new `OneIdNamed`. Built and deployed for **2020, 2024 and 2027**, the whole span the add-in claims. Found when the owner's "build what you need to test with" method hit `create-duct` on its first call |
| | **WHAT AN `ElementId` NOW MEANS, AND WHAT IS STILL REFUSED.** `OneIdNamed` resolves the thing by NAME and takes its `.Id` — `levelId` among levels, `sheetId` among sheets, `titleblockTypeId` among family types, `categoryId` through the category lookup that already handles both spellings. **The need's name decides the class, exactly as `OneElement` does and for the same reason**: `levelId` and `sheetId` are both written `ElementId` and mean different searches. **A name the table does not hold is REFUSED rather than resolved against every element in the model** — `elementIds` and `linkedElementIds` mean *those ones there*, and searching for them by text would turn a missing rule into a confident wrong answer. It also keeps the file clear of `new ElementId(int)`, the constructor that became `long` at 2024 ([D-05](DECISIONS.md)): an id that is never CONSTRUCTED cannot break on the release where constructing one changed. | **What is left is 15 needs that are genuinely structured values rather than names**: `OverrideGraphicSettings` 4, a bare `IList<Element>` at `source: request` 3, two `IDictionary` shapes, `ForgeTypeId`, `ParameterValue`, `IList<Reference>`, `IList<IList<XYZ>>`, and `FamilyInstance` — refused deliberately, because `Element.Name` on an instance returns its TYPE's name and would match every one of them. `IFCVersion` is refused on purpose too: its members differ per release, and a name that resolves on 2024 and refuses on 2021 is worse than a refusal on both |
| | **RANKED BY WHAT ONE ROW OF DISPATCH BUYS.** `ElementId` — **28 needs, 26 fragments**, far the largest: `add-revision-cloud`, `apply-view-template`, `cap-open-pipe-ends`, `change-element-type`, `create-3d-view`, `create-plan-view`, `create-schedule`, `create-sheet`, `create-text-note`, `create-view-filter`, `export-model-to-ifc`, `export-model-to-nwc`, `filter-elements-by-category`, `place-rooms` (×3), `place-schedule-on-sheet`, `place-view-on-sheet`, `repoint-view-reference`, `select-by-level`, `set-element-phase`, `set-mep-insulation`, `set-view-phase` (×2), `set-wall-constraints` (×2), `tag-elements`, `tag-elements-in-view`. **Every one of those means "the thing called X"** — `levelId`, `phaseId`, `sheetId`, `titleblockTypeId` — and `OneOfClass` already resolves exactly that by name for nine other classes; what is missing is the `.Id` at the end. Then `OverrideGraphicSettings` 4, `View3D` 3, `IList<Element>` 3, `IList<ElementId>` 3, `ICollection<ElementId>` 3, `Color` 3, `SpatialElement` 2, `Material` 2, and 14 more at one each — including the five MEP type classes (`MechanicalSystemType`, `DuctType`, `MEPSystemType`, `FlexDuctType`, `PipingSystemType`, `PipeType`) that block `create-duct`, `create-pipe` and `create-flex-duct`. **Those six are the cheapest of the lot**: `DuctType`, `PipeType` and `FlexDuctType` all derive from `MEPCurveType`, which the dispatch already accepts — the string match on the declared name is the only thing in the way. | **§6 below is superseded by this row.** It was measured on a different basis and says `ElementId` 21, `Element` 20, `everything else` 26. Read this one |
| **29** | **`set-wall-constraints` COULD NEVER DO THE ONE THING ITS FLAG EXISTS FOR, AND REPORTED IT AS A REFUSAL RATHER THAN A FAULT.** Run 2026-09-13 on `test projject` against four plain walls - base `Level 1`, unconnected top, 3000 mm - asked for top `Level 2` with `allowUnconnectedToBecomeBound` **true**: `rehosted 0`, `refused` holding all four, `notAWall 0`, `unconnectedTop 0`, and both levels resolved (`newBase Level`, `newTop Level`), so the blanket refusal at `fragment.cs:44` had not fired. **One run isolated it.** The same four walls with the flag **false** - which skips the top block entirely - came back **`rehosted 4`, `refused 0`, `unconnectedTop 4`, `elevationChanged 0`**. The base path was never the problem. **The cause is the order of two lines**: the top block read `WALL_TOP_OFFSET` and tested `IsReadOnly` *before* setting `WALL_HEIGHT_TYPE`, and on a wall with an unconnected top Revit greys that offset out because there is no top level to measure from. So every unconnected wall was refused - and an unconnected wall is exactly what `allowUnconnectedToBecomeBound` is there to convert. | **FIXED 2026-09-13.** The constraint is set first and the offset read after. Same command, same model: `rehosted 4`, `refused 0`, `elevationChanged 0`. Found by proving it - the POSITIVE came back empty, which is the half D-30 is not written about |
| **30** | **`create-schedule` CRASHED AT THE EXACT POINT IT WAS WRITTEN TO REFUSE POLITELY.** `fragment.cs:39` called `ViewSchedule.CreateSchedule(doc, categoryId)` and `fragment.cs:41` tested `schedule == null` to produce *"Revit declined to create a schedule for that category - not every category can be scheduled"*. **Revit THROWS instead of returning null**, so that sentence was unreachable. Run 2026-09-13 on `test projject`: `categoryId=Ducts` created one with both fields and `missingFields 0`; `categoryId=Views` came back **`fragment_threw`** - *"'create-schedule' threw while running: categoryId is not a valid category for a regular schedule. Parameter name: categoryId"*. A fragment that throws where it means to refuse has no negative leg at all. | **FIXED 2026-09-13.** The call is wrapped and Revit's own sentence is passed on, because "not every category can be scheduled" does not say WHICH rule was broken and Revit's does. The negative now reads `created (null)`, `refused "...Revit said: categoryId is not a valid category for a regular schedule"`. Found the same way as row 29 |
| | **BOTH WERE FOUND BY THE SAME THING, AND IT IS WORTH NAMING.** Neither is a units fault, neither shows on a count, and neither would ever appear in a run that only did the positive case. `set-wall-constraints` was caught because its POSITIVE came back empty - the half [D-30](DECISIONS.md) is *not* written about, and the half the proving skill's rule 2 exists for. `create-schedule` was caught because its NEGATIVE threw instead of answering. **A fragment whose refusal path is unreachable looks perfect until somebody asks it to refuse** - and eight of the eleven fragments proved on 2026-09-13 refused correctly on the first attempt, which is what makes these two stand out rather than blend in. | Both fixes are fragment `.cs` only. **No rebuild, because the client reads `impl/any/fragment.cs` off disk and sends it in the request** (`heron_bridge_client.py:932`) - a fact worth its own line, since half a session was planned around closing Revit for a change that never needed it |
| **31** | **`place-rooms` REPORTED EVERY ROOM IT MADE AS "NOT ENCLOSED", INCLUDING THE ONES SITTING CORRECTLY INSIDE FOUR WALLS.** Run 2026-09-13 on `test projject` against a **7200 x 5200 x 3000 mm box built on Level 2 and read back from a separate process at Z 4000 to 7000** - a genuinely closed region with no room in it - it answered `created 1`, **`unbounded 1`**. The same call on Level 1, whose only enclosed region already holds a room, answered `created 1, unbounded 1` as well. **Two opposite arrangements, one answer.** The cause is a read-back with nothing to read: `fragment.cs` took `BuiltInParameter.ROOM_AREA` in the same breath as `NewRooms2`, and **a room has no area until Revit regenerates** - so the zero-area test, which the file correctly calls *"exactly the 'Not Enclosed' condition"*, was measuring Revit's not-yet rather than the room's geometry. | **FIXED 2026-09-13** with one `doc.Regenerate()` before the loop. Same command, same model: Level 2 now `created 1, **unbounded 0**`, Level 1 still `created 1, unbounded 1`. **This is the opposite result to [row 18](#)**, where a Regenerate between a write and its read-back changed nothing and the hypothesis was correctly discarded - so "add a Regenerate" is not a rule, it is a thing to test each time |
| **32** | **NEITHER ROOM-PLACING FRAGMENT CAN EVER COME BACK EMPTY, so D-30's second leg is unreachable for both as contracted.** `place-room-at-point` says it in its own purpose text - *"A POINT OUTSIDE AN ENCLOSED REGION STILL MAKES A ROOM"* - and the only route to `refused` is a null point, which the caller's parser rejects before the fragment sees one. Measured 2026-09-13: inside the walls `created 1, unenclosed 0`; on open ground `created 1, unenclosed 1`. **`place-rooms` turns out to behave the same way and nothing said so.** Its refusal *"nothing was placed - there is no enclosed area on this level"* fires only when `NewRooms2` returns an empty collection, and it does not: asked on a level whose one enclosed region was already occupied it placed an **unbounded** room and returned it. `created 1` in both legs, measured. | **OPEN, and it is a contract question rather than a fault.** Both fragments have a real result that DOES move between arrangements - `unenclosed` / `unbounded` - and each holds in both directions: 2 points on open ground gave `unenclosed 2` where 1 point inside the box gave `unenclosed 0`. **The decision owed is whether `created` is the right first result for a fragment that always creates something.** Same shape as [row 26](#), where two declared results could be read by a person and neither by the runner. Found 2026-09-13 |
| **33** | **`create-sheet` CRASHED WHERE IT MEANT TO REFUSE - THE SECOND FRAGMENT OF THIS EXACT SHAPE IN ONE AFTERNOON.** `fragment.cs:68` called `ViewSheet.Create(doc, titleblock)` and tested `sheet == null` to say *"Revit declined to create the sheet"*. **Revit throws instead.** Run 2026-09-13 on `test projject`: `titleblockTypeId = "A1 metric: A1 metric"` made the sheet (`created 1`); a **Detail Item** family type came back `fragment_threw` - *"The ElementId titleBlockTypeId does not correspond to a TitleBlock type. Parameter name: titleBlockTypeId"*. Identical to [row 30](#), found the same day on a different fragment, which is what turned it from an incident into a pattern worth searching for. | **FIXED 2026-09-13**, the same way: the call is wrapped and Revit's own sentence is passed on. The negative now reads `created (null)`, `refused "Revit declined to create the sheet. Revit said: The ElementId titleBlockTypeId does not correspond to a TitleBlock type"`. **Both fixes were only possible because the id could be typed in at all** - `titleblockTypeId` and `categoryId` were both unreachable until [row 28](#) was fixed that morning |
| | **THE SHAPE, SEARCHED FOR RATHER THAN WAITED FOR - 11 fragments carry it.** A Revit factory call assigned to a local, tested for `null`, with no `try` around it: `add-revision-cloud` (`RevisionCloud.Create`), `create-callout` (`ViewSection.CreateCallout`), `create-drafting-view`, `create-filled-region`, `create-grid`, `create-level`, `create-plan-view`, `create-revision`, `create-text-note`, `create-view-filter` (`ParameterFilterElement.Create`) and `place-family-instances` (`doc.Create.NewFamilyInstance`). **This is NOT a list of eleven defects and must not be read as one.** Some Revit factories do return null. Three of the eleven were proved on 2026-09-13 with a negative that refused cleanly - `create-filled-region`, `create-plan-view` and `create-text-note` - and in all three an EARLIER guard caught the bad input, so the `== null` test was never reached and says nothing either way. | **OPEN as a question, not a fault list.** Five of the eleven are `PROVEN` - `create-callout`, `create-drafting-view`, `create-grid`, `create-level`, `create-revision` - and each therefore has a refusal path nobody has made fire. **The cheap experiment is one bad argument each**, of the kind Revit is known to throw on, and the answer per fragment is either "returns null, guard is right" or "throws, wrap it". Neither can be guessed from the source |
| **34** | **REVIT APPLIES A 3D VIEW TEMPLATE TO A FLOOR PLAN AND MEANS IT, so a mismatched template is not a usable negative.** Run 2026-09-13 on `test projject`: `HERON 3D TEMPLATE`, made from `{3D}` by `create-view-template-from-view`, applied to `1 - Mech` and `2 - Mech` returned **`applied 2`, `refused 0`**. **The count is not a guess** - `fragment.cs:52` is `if (view.ViewTemplateId == templateId) applied++; else refused.Add(...)`, so it set the value and read it back. Revit really does accept it. | **NOT A FAULT - it is [row 24](#)'s rule holding in a new place.** *A view type is a usable negative only where Revit itself refuses*, and here it does not. The negative that DOES work is the one `tests/cases.yaml` already named: views that **already carry** the template - `applied 0`, `alreadyOnIt 2`. Recorded so the next person does not spend the run I spent |
| **35** | **`create-flex-duct` REPORTED THE LENGTH OF THE ROUTE IT REFUSED TO BUILD, beside a `created` of null.** Its contract says `lengthMm` is *"how long the flex actually came out, which is the number the length limit is about - the answer, not a count of it"*, and it is declared `role: result`. Run 2026-09-13 on `test projject` with a 9000 mm route against a 3000 mm limit: `created (null)`, `refused` correctly saying *"The route measures 9000 mm, over the 3000 mm limit given, so NOTHING WAS CREATED"* - and **`lengthMm 9000`**, which reads as a 9000 mm flex duct existing. The gate caught it as *"the negative case returned content instead of nothing"*. **The author had already got this right one branch earlier**: the empty-point case at `fragment.cs:55` does `lengthMm = 0.0;`, and the over-length branch three lines below simply did not. | **FIXED 2026-09-13**, one line in each of the two branches that refuse after measuring. The route's real length is not lost - it is in the refusal sentence, which is where a measurement of what was ASKED FOR belongs rather than in the field that says what was MADE. Now `created (null)`, `lengthMm 0`. Found by proving the fragment |
| **36** | **`create-sheet-list`'s DUPLICATE-NAME REFUSAL CANNOT FIRE, because Revit lets a schedule take a floor plan's name.** `fragment.cs:31` sets `schedule.Name` inside a `try` and the `catch` reports *"'{0}' is already the name of another view - the sheet list was created as '{1}' instead"*. Run 2026-09-13 on `test projject` with `scheduleName = "1 - Mech"`, the name of an existing floor plan: **`refused 0`**, `sheetList ViewSchedule`, `fieldsAdded 2`. No exception was raised, so the sentence never ran. | **OPEN as an observation, and the fragment is not obviously wrong.** A `ViewSchedule` and a `ViewPlan` may genuinely be allowed to share a name in Revit, in which case the `catch` is dead code guarding against something that cannot happen, and the honest fix is to delete it rather than leave a refusal nobody can trigger. **The experiment that settles it is one applied run and a look in the Project Browser** - whether two views called `1 - Mech` now appear. Not run here, because it cannot be undone by a rollback. This is [row 24](#)'s rule again: *Revit allows more than it looks like it should*, and a refusal path built on an assumed refusal is untestable. The proof that stands uses the OTHER declared refusal - a column a sheet list cannot carry: `fieldsAdded 0` with all four bad names reported |
| **37** | **A REVISION CANNOT BE NAMED, so `revisionId` is receivable in type and unresolvable in practice.** [Row 28](#)'s `OneIdNamed` maps `revisionId` to `typeof(Revision)` and resolves it through `ElementsNamed`, which matches on `Element.Name`. **A Revision has no name a modeller would recognise.** Tried 2026-09-13 on `test projject`, which holds two revisions: `"HERON TEST REVISION"` (the description), `"Seq. 2"` and `"2"` all came back *"No revision called ... in test projject"*. `list-revisions` on the same model the same minute reports them as **`Seq. 1 - Revision 1`** and **`Seq. 2 - HERON TEST REVISION`** - but that string is the fragment's own formatting, not `Element.Name`. So `add-revision-cloud` is still unreachable, one layer further in than it was this morning. | **WITHDRAWN 2026-09-13 by [row 46](#), and this cell still said `OPEN` on 2026-09-16.** Row 46 measured it: a Revision's `Element.Name` **is** `"Seq. N - Description"`, that string is Revit's own, `OneIdNamed` resolves it as it stands, and `set-sheet-revisions` was proved through this very route the same hour. **No rebuild was ever needed.** The reasoning below is left standing because it is a good example of a sound-looking inference from three refusals that a single measurement overturned. What identifies a revision to the person holding the drawing is its **SEQUENCE NUMBER** - it is the first column of Revit's own Sheet Issues/Revisions dialog, and `delete-revision` already takes `sequenceNumber: int` rather than a name, which is the same conclusion reached independently. `OneIdNamed` should resolve `revisionId` by sequence, falling back to description. **The general lesson is worth more than the row**: adding a CLASS to the dispatch is not the same as making its instances reachable, and the only way to tell is to ask for one. Found proving `add-revision-cloud` |
| **38** | **`set-view-phase` THREW AWAY A REFUSED PHASE FILTER WHENEVER THE PHASE ITSELF WENT THROUGH, and reported the view as a clean success.** `fragment.cs` ended each view with `else if (refused && !touched) templateControlled.Add(view.Id);` - and `touched` goes true the moment the PHASE is written. So a view whose phase set and whose phase FILTER was refused in the same call satisfied `!touched == false`, was added to nothing, and came back with `phaseFilterSet` silently one short. **Measured 2026-09-13 on `test projject`**, view `1 - Mech`, with `HERON PLAN TEMPLATE` holding the Phase Filter and the phase free to change: **`phaseSet 1`, `phaseFilterSet 0`, `alreadySet 0`, `templateControlled 0`, `unsupported 0`, `findings 0`.** Nothing anywhere said the filter had not taken. **`alreadySet` being empty is what rules out the innocent reading** - "it was already that filter" has its own branch at `fragment.cs:129` and adds to `alreadySet`. | **FIXED 2026-09-13** by deleting `&& !touched`. A refusal is a refusal whether or not the OTHER property went through; the read-back already caught it and only the reporting was discarding it. The same call now answers `phaseSet 1`, `templateControlled 1 [312]` and a finding. **The finding's wording was corrected too**: it claimed a view template was holding the property, which is much the commonest reason and not the only one, so it now says a view listed here had SOMETHING refuse and names the template as where to look first. Found proving the fragment against the case `tests/cases.yaml` calls its first |
| | **AND ONE FALSE ALARM, RECORDED BECAUSE THE DISCIPLINE IS THE POINT.** The same run first looked like a second defect: asked for a filter of `Show All`, the fragment answered `phaseFilterSet 0` with `alreadySet 0`, which reads as a silent failure. It was not. The view's filter genuinely was already `Show All` in one run and template-held in another, and the difference was invisible until the template was released and re-applied deliberately. **Asking for a DIFFERENT filter is what separated them** - `Show Complete` came back `phaseFilterSet 1` at once. | **No change, and no row of its own.** It is here because a defect list is only worth the sit-down if every row was observed rather than expected, and that means recording the one that dissolved as well as the one that held. The rule it cost: **before writing down a silent no-op, ask for a value you KNOW is different.** If it takes, the first value was already there |
| **39** | **`set-view-template-control` DECLARES STATE AS RESULT, so D-30's second leg is unreachable - and the state is read back on purpose, which is the awkward part.** `nowHeld` and `nowFree` are `role: result`, and `fragment.cs:27` says why they exist: *"READ FIRST, WRITE, READ BACK. nowHeld and nowFree are a second read off the"* template. They are therefore the template's WHOLE state afterwards, not what this call changed. Measured 2026-09-13 on `test projject` with `parameterNames = "ZZZ NOT A PARAMETER"` - a name the template cannot carry: **`notOnTemplate 1`, `nowFree 0`, `refused 0` - and `nowHeld` still 26, `viewsFollowing` still 2.** Nothing was changed and two declared results are non-empty, so any negative returns content. | **OPEN as a contract question, and the implementation is NOT at fault.** Reading back rather than assuming is the behaviour this repository asks for everywhere else, and `viewsFollowing`'s own contract comment calls it *"THE WHOLE HAZARD... it must be read before the call, not after"* - a warning, not a finding. **What the fragment does not report is what CHANGED**, which is the only thing that could be empty. Same family as [row 26](#), [row 32](#) and [row 35](#): a declared result that is a measurement of state or of input rather than of work done. The decision owed is one rule for all four |
| **40** | **`place-schedule-on-sheet` HAS A POSITIVE AND NO REACHABLE NEGATIVE, because every route that can supply its `elements` supplies only schedules.** Its own refusals are real and there are five, at `fragment.cs:36, 47, 57, 69, 76`. **None can be triggered from a caller.** *Not a sheet* (:36) is unreachable because `OneIdNamed` resolves `sheetId` among `ViewSheet` only, so a wrong name is a `bad_request_value` from the binder before the fragment runs - measured 2026-09-13 with `sheetId = "ZZZ NO SUCH SHEET"`. *Not a schedule* (:47) needs a floor plan in the selection, and the only fragment that provides schedules is `find-schedules`, which returns nothing else; `find-views` excludes them the other way - `skippedNonDrawing 8`. *A schedule TEMPLATE* (:57) is excluded by `find-schedules` too. And **the obvious fallback is not a refusal either: Revit placed the SAME schedule on the SAME sheet twice without complaint** - `placed 1 [926190]` then `placed 1 [926191]`, `refused 0` both times. | **OPEN. The fragment is not at fault and its positive is sound**: `HERON DUCT SCHEDULE` on `HERON TEST SHEET` gave `placed 1` with `placedAt` naming the schedule, the sheet and the position. What is missing is a way to hand it something that is NOT a schedule, and that is a gap in the ARRANGEMENT MECHANISM rather than in the code: `validate` runs one setup chain for both phases, so a negative needing a different KIND of element cannot be built. **This is the fifth time today Revit allowed something expected to refuse** - see rows 24, 34, 36 and the `create-sheet-list` name clash. Leaving it DRAFT is the honest outcome |
| **41** | **ONE SHAPE, EIGHT FRAGMENTS: A `role: result` THAT MEASURES REACH, STATE OR INPUT RATHER THAN WORK DONE.** Rows 26, 32, 35, 39 and 40 are all this, found separately over one day, and three more turned up after them. **The test that separates them is a single question: COULD THIS NUMBER HAVE BEEN ZERO IF THE FRAGMENT HAD DONE NOTHING?** Where the answer is no, D-30's second leg is unreachable and the gate correctly reports *"the negative case returned content instead of nothing"*. **Four were corrected and are now `accounting`**: `create-flex-duct.lengthMm` (row 35 - it reported the length of a route it refused to build), `test-view-filter-match.outOfScope` (elements ruled out of scope, never found), `replace-material.typesTouched` (types the swap REACHED - 1 even when nothing was replaced), and `set-view-phase` was a different fault entirely (row 38). **Four are still open**: `create-mep-system-type.created` returns the object (row 26); `place-room-at-point.created` and `place-rooms.created` can never be empty because a point always makes a room (row 32); `set-view-template-control.nowHeld` is a deliberate read-back of the template's whole state (row 39); `describe-blank-parameters` declares NO `role: result` at all, only `findings`, and separately needs `blank` AND `absent` both non-empty in one run, which no arrangement in this model produces. | **OPEN - and it wants ONE rule, not eight patches.** The rule that fits every case already exists in [D-52](DECISIONS.md)'s words for accounting: *"Never a thing it FOUND"*. Applied to results it reads: **a `role: result` is a measurement of what this call DID, and anything else - reach, state after, an echo of the input, a count of what was excluded - is `accounting` however useful it is to a reader.** Nothing is lost by the demotion: every one of the four corrected fields is still reported and still in the run record. What changes is only whether the machine reads it as evidence that something was found |
| | **AND A SECOND, DIFFERENT BLOCKER THAT LOOKS THE SAME FROM THE OUTSIDE.** `place-schedule-on-sheet` (row 40) and `copy-elements` have honest contracts and no reachable negative, because **`validate` runs ONE setup chain for both phases**. A negative needing a different KIND of element - a floor plan where the positive had a schedule, something Revit refuses to duplicate where the positive had walls - cannot be arranged at all. Measured while trying: `copy-elements` copied a **Room** without complaint, which was the last candidate for "an element Revit refuses to duplicate" that this model could offer. | **The harness change is small and would unblock both**: let the negative phase name its own setup chain, the way it already names its own `--negative-set` values. Until then the honest outcome is DRAFT, which is what both have. Worth separating from row 41 proper: that one is a contract question for the owner, this one is a tool that cannot express the case |
| **42** | **`set-category-visibility` COUNTED THE CALL IT MADE, NOT THE CHANGE IT CAUSED - so hiding something already hidden reported success.** `fragment.cs:46-47` was `view.SetCategoryHidden(category.Id, !visible); changed++;` with no read-back and no already-that-way branch. **Measured 2026-09-13 on `test projject`**: Walls hidden in `{3D}` and kept, then asked to hide Walls in `{3D}` again - **`changed 1`**, `notControllable 0`, `refused 0`. A caller checking whether their instruction did anything was told yes while nothing moved. Same shape as [row 20](#)'s `hidden = ids.Count`, in a second fragment. | **FIXED 2026-09-13.** `GetCategoryHidden` is read before and after and only a real change is counted; a category that was already the way it was asked for goes to a new `alreadyThatWay`, declared `role: accounting` because it is never a thing this call did. **The distinction is the point, not the number**: `changed 0` on its own cannot separate "already done" from "something is wrong", and the sibling fragments all name it - `set-element-phase.alreadySet`, `apply-view-template.alreadyOnIt`, `change-element-type.alreadyThatType`. Same call now: `changed 0, alreadyThatWay 1` |
| | **A SCHEDULE IS THE VIEW TYPE THAT REFUSES, AND IT UNBLOCKED FOUR FRAGMENTS.** [Rows 20 and 24](#) recorded that a `ViewSheet` accepts a hide, an isolate and a crop - Revit permits all three meaninglessly, so `viewRefused` could never fire and none of those fragments had a negative case. **A `ViewSchedule` refuses.** Measured the same day: `isolate-elements` `isolated 0, viewRefused true`; `hide-elements` on the temporary path the same; `set-view-crop` `applied false, enclosed 0, viewRefused true`. All four are now proved. | **TWO LIMITS FOUND WITH IT, both recorded rather than tidied away.** `set-category-visibility` on a schedule answers `changed 1` - Revit accepts a category hide there, so a schedule is NOT a universal refuser. And `hide-elements` only refuses on the TEMPORARY path: with `permanent: true` the same schedule answered `viewRefused false, hidden 3`, because the guard is `CanUseTemporaryVisibilityModes()` and nothing checks the permanent one. **The rule from row 24 holds and is now narrower: a view type is a usable negative only where Revit itself refuses - and which view refuses depends on WHICH CALL you make, not only on the view** |
| **43** | **`remove-parameter-value` COULD NOT CLEAR A PLAIN TEXT PARAMETER - its main path failing on its commonest case.** The file's own header says *"WRITE_ELEMENT_PARAMETERS CANNOT DO THIS... Clearing is its own call and **works on all four**"*. It does not. `fragment.cs:67` calls `parameter.ClearValue()` and **Revit refuses it for a built-in text parameter**. Measured 2026-09-13 on `test projject`: `write-element-parameters` put `Comments = "HERON TEST"` on four walls (`written 4`), and clearing the same field answered **`cleared 0`, `alreadyEmpty 0`, `readOnly 0`, `refused 5`** - every wall reporting *"Cannot call Clear..."*. **The fragment was not lying** - it reported the failure as a refusal rather than counting it - but the premise in its header was wrong, and Comments is the field anybody actually wants emptied. | **FIXED 2026-09-13**, and the fix was already written five lines above the bug: *"an empty string clears a text parameter"*. A `StorageType.String` parameter that refuses `ClearValue` now falls back to `Set(string.Empty)`, **read back before it counts**. `cleared 0` became `cleared 4` on the same call. **ONLY for String**: a length, an airflow or one pointing at another element still refuses rather than being set to zero, because *"an empty parameter and a zero are not the same thing"* is the reason the fragment exists and a fallback must not quietly break it |
| **44** | **THIRTEEN SIGNED FRAGMENTS WERE STILL `DRAFT`, SO THE NEXT ROUND OFFERED THEM TO BE PROVED AGAIN - AND THE OWNER FOUND IT, NOT A TOOL.** `heron_validate.py accept` writes the proof and deliberately never writes `heron-status`; its own header says so, and the separation is right - the evidence and the decision to trust it are different acts. Promotion is therefore a HAND EDIT, and a hand edit gets forgotten. A forgotten one is invisible from the inside: the file still reads `DRAFT`, so `grep -h '^heron-status:'` counts it as unproved and it returns to the queue. Ajmal saw it from the only place it shows - *"becose i signed item i have to do again i sow lkike that for exambple set view crop i singe multiple time"* (2026-09-13). Measured the same hour: 13 fragments carried `by: Ajmal PS` at `DRAFT`, the oldest signed 2026-09-08, none with a caveat saying it was held back on purpose. **The published count was wrong too** - README said 247 PROVEN / 113 DRAFT when six of that 113 were already signed. **THE THIRTEEN SPLIT IN TWO, AND MERGING THEM WOULD HAVE HIDDEN BOTH.** `can_promote` was asked about each rather than the answer being re-derived: **six** were pure waste - signed, fingerprint fresh, nothing blocking, promoted on the spot. **Seven** were STALE: the implementation changed under them after signing (the D-71 units work), so their proofs no longer describe the code that would run and re-proving them is CORRECT. D-30 wants a stale proof to be loud. That is [D-52](DECISIONS.md)'s rule in another costume - a count of things correctly refused is never evidence of something found - and the fault was never telling him which kind he was looking at. | Fixed - `tools/check-signatures.py`. **This cell said *"wired into `gates.yml`"* until 2026-09-16 and it was never true**: the commit that adds the step sits on the local branch `ci/run-check-signatures` and **cannot be pushed** - the `gh` token has no `workflow` scope. It runs in CI only because it **rides inside `tools/check-docs.py` as section 9**, which `gates.yml` does call. The distinction matters the day somebody edits that workflow by hand: the section's own comment says to give it a real step and delete the section. Exit 1 on an UNUSED signature, stale ones reported and not failed. Proved both ways before it was trusted: with `set-view-crop` put back to `DRAFT` it names it and exits 1; with the six promoted it exits 0 |
| **45** | **`set-mep-justification` CANNOT SET A NON-ZERO OFFSET, AND THE ONLY VALUE THAT WORKS IS THE ONE THAT CHANGES NOTHING.** Measured 2026-09-13 on `test projject`, 6 ducts in `{3D}` created by Heron itself. `RBS_CURVE_HOR_OFFSET_PARAM` and `RBS_CURVE_VERT_OFFSET_PARAM` both report `IsReadOnly == false`, `.Set()` throws nothing, and the read-back comes away unchanged: **10 mm, 25 mm, 50 mm and 100 mm each gave `set 0` with all six reported refused - `2 of 2 offset(s) did not take the value asked for`.** **`0 mm` gave `set 6`.** So the write path works and Revit is discarding the magnitude, silently, while declaring the parameter writable. **THE FRAGMENT IS THE HONEST PART HERE.** It writes, reads back and compares within 1e-6 before it counts anything, which is the only reason this was visible at all - a fragment trusting `.Set()` to have worked would have reported `set 6` on every one of those runs and been believed. **AND THE 0 mm RUN IS A TRAP, NOT A PROOF.** Positive `set 6` on 0 mm against a negative of 19 walls `set 0` is a clean-looking D-30 pass that measures nothing: the ducts were already at 0, so it wrote a value that was already there and counted it as work. That is exactly the *succeeds while doing nothing* case D-30 exists to catch, and it passes the runner. **It was not drafted, and it must not be** - the fragment stays DRAFT. **THE LIKELY CAUSE IS NOT YET MEASURED, and is written here as a question rather than an answer.** The fragment touches those two parameters and nothing else - `grep JUSTIFICATION` on its implementation returns 0 - so it never sets the Horizontal/Vertical Justification the offsets hang off. Under a Center/Middle justification Revit recomputes the offset, which would explain an accepted write that does not stick; row 29's `set-wall-constraints` was the same shape, where `WALL_HEIGHT_TYPE` had to be set before `WALL_TOP_OFFSET` would take. **What would settle it:** set `RBS_CURVE_HOR_JUSTIFICATION_PARAM` away from centre first, then write the offset, and see whether it holds. Until somebody runs that, the cause is a hypothesis and the measurement above is the fact. | Open - the defect is real and reproduced at four values; the fix is unverified |
| **46** | **ROW 37 IS WRONG, AND NO REBUILD IS NEEDED: A REVISION IS NAMED `"Seq. N - Description"`, AND THAT STRING IS REVIT'S OWN.** Row 37 tried `"HERON TEST REVISION"`, `"Seq. 2"` and `"2"`, got three refusals, saw `list-revisions` print `Seq. 2 - HERON TEST REVISION`, and concluded **that string is the fragment's own formatting, not `Element.Name`** - so `OneIdNamed` would need rebuilding to resolve by sequence number. **The one string it never tried was the whole one.** Measured 2026-09-13 on `test projject`: `revisionIds="Seq. 2 - HERON TEST REVISION"` resolved and `set-sheet-revisions` reported `sheetsChanged 2`; `revisionIds="Seq. 1 - Revision 1"` resolved the same way, `sheetsChanged 2`, so it is a rule and not one lucky string. `Element.Name` on a `Revision` IS the sequence and the description joined, built by Revit. **WHAT WENT WRONG WAS THE INFERENCE, NOT THE TESTING.** Row 37 tried the description, the prefix and the bare number - three guesses at what a name might be - and never tried COPYING WHAT THE MODEL HAD JUST PRINTED. It then explained the failure with a theory ("that is the fragment's formatting") and promoted the theory to a conclusion strong enough to schedule an add-in rebuild against. An untested explanation of a failure is not a finding; it is the next thing to test. **WHAT THIS REOPENS:** `add-revision-cloud` was recorded unreachable in three places (§3i, §6 and row 37) on this reasoning alone. It is worth trying with the full name before any of that is believed. `delete-revision` taking `sequenceNumber: int` is still a good contract and nothing here argues against it. | Fixed by measurement - row 37's conclusion is withdrawn, the rebuild it asked for is not needed, and `set-sheet-revisions` was proved through this route the same hour |
| **47** | **`accept` COULD NOT SIGN A D-53 TRACKING PROOF AT ALL, SO THE ROUTE FOR FRAGMENTS WITH NO EMPTY CASE WAS QUIETLY SHUT.** `draft` has understood tracking since D-53 - it reads `record["tracking"]` and writes the negative case from it - but `_evidence_refusal`, the guard `accept` added 2026-09-12, never looks at that key. It demands a negative PHASE that `looks_empty`, and a fragment that cannot come back empty has neither. Measured 2026-09-13 on `report-filterable-parameters`, which is one of them: any category it is handed is either filterable or is reported as unfilterable, so one result is always non-zero. Both shapes were refused and **both refusals blamed the fragment** - drop the negative phase and it says *"the run record has no negative phase in it"*; keep it and it says *"the negative case came back with content"*. Neither mentions tracking. `count-elements` and `report-category-visibility` are PROVEN by this route and were signed before the guard existed, so nothing was visibly broken until the next one came along. **THE BAR ADDED IS THE VARIATION, NOT THE ROW COUNT** - rows that all carry the same value are exactly what a fragment ignoring its input produces, so identical rows are refused and fewer than three are refused. Proved both ways before trusting it: the real 6-row set (69, 74, 60, 48, 29, 0) is accepted; the same set with every value forced to 69 is refused; two rows are refused. **AND A SECOND GUARD, FROM ROW 44'S COMPLAINT RATHER THAN FROM A CRASH.** `accept` writes the draft's fingerprint through unchanged, so signing a draft taken against code that has since changed lands a proof that is stale on arrival: `can_promote` refuses it for ever, the fragment stays DRAFT, and the next round offers it up again. That is the exact loop Ajmal reported. `check-ceiling-coordination` was sitting in that state - draft `c905e58bb9224d02`, code now `4db71c18b96ed9bb` - and passed every other check. A wasted signature is now refused BEFORE the name is typed. | Fixed - `_evidence_refusal` in `brain/heron_validate.py`. `test_validate_agent`, `test_batch_prove` and `test_fragment_store` all pass |
| **48** | **`read-space-loads` WAS ONE SIGNATURE AWAY FROM BEING PROVEN ON A RUN THAT READ NOTHING - AND SIXTEEN DRAFTS ARE WAITING BEHIND IT.** Its draft's positive case read 5 spaces on `Snowdon-scratch` and returned **`heatingW 0`, `coolingW 0`, `airflowLs 0`** - every figure the fragment exists to report, empty. It passed every check because `noLoad`, a count of the spaces Revit REFUSED to give figures for, is declared `role: result`, and 5 of those carried the positive leg past the judge. That is [D-52](DECISIONS.md) word for word: a refusal count is never evidence something was found. `noLoad` is now `accounting` - which does not make it unimportant, `notSpaces` beside it is equally worth acting on and has always been accounting - and the draft correctly reads `POSITIVE EMPTY` at once. **Proving this fragment needs a model whose spaces have been ANALYSED; Snowdon's have not**, so no arrangement on that model would have worked. This is [row 41](#)'s family, ninth member. **THE REASON IT SURFACED IS WORTH MORE THAN THE ROW.** Asking `_evidence_refusal` about every draft on disk - read-only, signing nothing - turned up **16 of 72 that would be accepted**, only 4 of them from that day's proving. Twelve valid drafts had been sitting unsigned across earlier rounds while the library counted their fragments as unproved. Two of the sixteen were wrong on inspection: this one, and `select-by-insulation`, which looked identical in both legs until the run record showed `elements 1053` bare against `elements 0` insulated - a textbook pass whose giveaway numbers were locals the summary had truncated. **Read the run record, not the summary line.** | Fixed - role corrected, and the draft-audit is worth re-running before every signing round |
| **49** | **ROW 48 COUNTED NINE ALREADY-PROVEN FRAGMENTS AS WORK WAITING FOR A SIGNATURE, AND HANDED THEM TO THE OWNER TO SIGN AGAIN.** The draft-audit it describes asked `_evidence_refusal` about every draft on disk and reported 16 ready. **It never asked what STATUS the fragment was at.** `accept` does not delete the draft it consumed, so a proved fragment keeps its draft file for ever, and 9 of the 16 - `audit-mep-openings`, `check-fixture-connectivity`, `disallow-join`, `place-view-on-sheet`, `report-connector-loads`, `report-connectors`, `report-space-airflow`, `select-by-insulation`, `update-saved-set` - were `PROVEN` already. The real number was **6**. **THIS IS ROW 44 HAPPENING AGAIN, IN THE SAME SESSION THAT FIXED IT, COMMITTED BY THE TOOL THAT WAS SUPPOSED TO HAVE FIXED IT.** Row 44's gate catches a signature that was never USED; nothing caught a signature about to be spent on work already DONE. Ajmal met it from the other end the same hour: signing `remove-parameter-value`, already `PROVEN`, printed *"Status is still DRAFT - promoting it is a separate, deliberate act"* - boilerplate that `accept` prints unconditionally, and flatly untrue of that file. Nothing on screen could have told him. **AND THE FIX EXPOSED THAT `read-space-loads` IS PROVEN ON EVIDENCE THAT PROVES NOTHING.** Row 48 called it *one signature away*; it was signed on **2026-09-10** and its recorded proof carries the very numbers that row objected to - `airflowLs 0`, `coolingW 0`, `heatingW 0`, carried by `noLoad 5`. Correcting the role did not touch it, because the fingerprint covers the implementation and not the contract's roles - so **a proof that demonstrates nothing is sitting at PROVEN and no gate can see it.** That is a decision for the owner, not an edit: demoting it costs a number he is tracking, and leaving it costs more. | Guard fixed - `accept` now refuses a fragment already `PROVEN` whose proof is not stale, and says why. **`read-space-loads` is OPEN and needs the owner's call.** |
| **50** | **A FRAGMENT THAT MAKES ONE THING RETURNS THE THING, NOT A COUNT, AND THE SIGNING GATE COULD ONLY COUNT.** `positive_worked` skips any declared result that `_is_helper_object` recognises - the rule exists for real working values like `OverrideGraphicSettings`, and it is right to. But that test is a heuristic on the RENDERED string, and it cannot tell a working object from a created one: both arrive as a bare CamelCase word. Measured 2026-09-13 proving `create-mep-system-type`, which duplicates a duct system type. Positive: **`created MechanicalSystemType`, `classification SupplyAir`**. Negative, given a name the model has not got: **`created (null)`, `classification ""`**. Both real results were discarded as helper objects and the leg read `POSITIVE UNREADABLE - nothing it returned is a declared result that can be read as a quantity`. **THE SAME HEURISTIC READ THE NEGATIVE CORRECTLY** - `(null)` and `""` count as zero - so only the positive was ever mis-read, and the evidence was sitting in plain sight on both lines. **THE FIX ASKS THE QUESTION D-30 ACTUALLY ASKS, WHICH IS A COMPARISON RATHER THAN A COUNT.** When the positive reads UNREADABLE, `_result_appeared` now looks for a declared result that is PRESENT in the positive and GONE in the negative. That is stronger than counting one leg, not weaker: a fragment succeeding while doing nothing returns the same value in both phases and cannot pass it, and the `OverrideGraphicSettings` the original rule guards against is present in both legs and cannot pass it either. Proved three ways before it was trusted - the real record accepted; the negative faked to carry the same object refused; the positive faked to create nothing refused. Re-auditing all 72 drafts afterwards unlocked exactly one, the fragment that found it, so it is not quietly waving others through. | Fixed - `_result_appeared` in `brain/heron_validate.py`. `test_validate_agent` and `test_batch_prove` pass |
| **51** | **THE D-71 UNITS PASS CONVERTED ONE LENGTH PER FRAGMENT AND WALKED PAST THE SECOND.** Both fragments below carry the MILLIMETRES IN, FEET INSIDE block, both convert the length named first, and both leave the other one raw. Found 2026-09-13 while arranging `check-surface-fit`, by reading the file rather than by a run - the model has nothing beneath its ducts, so no arrangement on it would ever have reached the line. **`check-surface-fit.evenness`** is compared against `spread`, the difference between two `ReferenceIntersector` distances - Revit's units, feet. An `evenness` of 5 meant **five FEET**, a flatness tolerance 304.8x looser than the 5 mm typed, and a surface a metre and a half out of true reporting as flush. The giveaway was in the same file all along: the message it prints does `spread * 304.8` to turn that very number into millimetres. **`check-valve-accessibility.envelope`** is added to and subtracted from `centre`, an XYZ in feet, to build the operating zone - so an unconverted 500 put a **152-metre box** around every valve and would have condemned every one of them as crowded. Its neighbour `maxReachHeight` was converted on the line above. **THE SWEEP MATTERED MORE THAN EITHER FIX, AND SO DID GETTING IT WRONG FIRST.** Every fragment carrying the block was checked for a `double` request input with no conversion. A first pass matched only self-assignment (`x = x / MillimetresPerFoot`) and accused **11 fragments, 7 of them PROVEN** - `set-mep-justification` among them, which converts into a new variable two lines later and is perfectly correct. Checking whether the NAME appears on any conversion line at all left 3. Of those, `distribute-along-run` is also a false alarm: it converts Revit's feet up to millimetres, does all its arithmetic there, and converts back once at the point of use - deliberate and right. **Two real, nine accused wrongly by the first pattern.** A units sweep that reports a PROVEN fragment as broken is not a cheap mistake, and the fix was to read the code rather than trust the regex. | Fixed - both conversions added, `check-fragments-compile` exits 0, and no PROVEN proof went stale. **Neither fix is verified against a model that exercises it** - `check-surface-fit` needs something beneath the services and `check-valve-accessibility` needs valves; this model has neither |
| **52** | **A FRAGMENT THAT WRITES A FILE HAS A SECOND ROUTE SITTING ON DISK, AND EVERY PROOF SO FAR HAS RECORDED `NOT ESTABLISHED` INSTEAD.** D-30 asks for a second route *where one exists*, and the draft writes `NOT ESTABLISHED - whether one exists here has not been settled` when none is supplied - honest, and it had become the default answer. For the eight `PUBLISH` fragments it is simply wrong: **the file IS the work**, it is readable without Revit, and counting it does not take the fragment's word for anything. Measured 2026-09-13 proving `export-parameters-to-csv` - the fragment reported `rows` 6, 6, 19 and 2 across four runs, and the four CSVs on disk held **6, 6, 19 and 2 data lines after the header**, every one matching. The header carried the exact column asked for, including `ZZZ NO SUCH PARAMETER` with every value blank, which is the right answer rather than a silent substitution. **The draft came back with `gaps: []` - the first in this session with all three legs, and the second route is the leg that has been missing from every proof in the library.** **IT ALSO HAS NO EMPTY CASE, WHICH IS WHY THE SECOND ROUTE MATTERS HERE.** A parameter name the model has not got still writes one row per ELEMENT - correctly, the row is the element - so the negative leg is D-53 tracking: 6 ducts, 19 walls, 2 pipes. **The remaining seven PUBLISH fragments can all be proved this way**, four of them still DRAFT (`export-families`, `export-model-to-nwc`, `export-schedule-to-csv`, and this one) and three already PROVEN on a `NOT ESTABLISHED` second route that need not have been. | Method established, not a defect. Worth applying to the other seven |
| **53** | **`set-view-template-control` REPORTS THE STATE AFTER THE CALL, NOT THE CHANGE, SO NEITHER LEG IS EVER EMPTY - AND THE ARITHMETIC PROVED IT ANYWAY.** `nowHeld` and `nowFree` are the FULL lists a template holds and leaves free afterwards, both `role: result`, both non-zero whatever the call did. [Row 41](#)'s family again. But unlike `revisionsRemaining` ([row 43's neighbour](#)) these lists ARE the fragment's answer - *which settings does this template control now* is the question it exists to answer - so declaring them accounting would be describing the fragment wrongly to fix a judging problem. **D-53 TRACKING SETTLED IT IN FOUR RUNS, ON `Architectural Plan` IN `test projject`:** naming a setting the template has not got left **22 held / 4 free**; freeing `View Scale` gave **21 / 5**; `View Scale, Detail Level` gave **20 / 6**; adding `Discipline` gave **19 / 7**. One fewer held and one more free for each name, every time. A fragment ignoring its input, answering about a different template, or reporting a cached list cannot produce that arithmetic four times running. **Where a result is a STATE rather than a count of work, the state moving exactly as the input says is the proof** - and it is a better one than an empty leg, because an empty leg is also what doing nothing produces. | Proved by tracking, contract left alone deliberately |
| **54** | **THE RISK GATE GUARDED EVERY PATH EXCEPT THE ONE THAT RUNS FRAGMENTS ALL DAY, AND ITS OWN COMMENT NAMED THE ACCIDENT IT THEN FAILED TO STOP.** `RUNNABLE_RISKS` excludes `PUBLISH` and `ADMIN` because HeronPermissions puts them out of reach in Phase 0 and Phase 1. `risk_refusal` has enforced that in `cmd_fragment` and `cmd_prove` since 2026-09-08. **`cmd_validate` never called it** - and validate is the proving command, the one path that runs fragments continuously. The comment beside `RUNNABLE_RISKS` says the risk it guards against is *"somebody proving fragments alphabetically and reaching `export-*`"*. **FOUND BY WALKING INTO IT, NOT BY READING.** `export-parameters-to-csv` (PUBLISH) was proved through `validate` earlier the same day and wrote four real CSV files to disk without a word from the gate; the hole only surfaced an hour later when `export-families` was tried through `fragment` instead and was refused on the spot. Two commands, the same fragment risk, opposite answers. `export-sheets-to-pdf`, `export-view-image`, `export-views-to-fbx`, `upgrade-family-files` (all PUBLISH) and `create-workset` (ADMIN) are already PROVEN, so this had been open long enough to matter. **THE EVIDENCE FROM THOSE RUNS STANDS AND THE PROOFS ARE NOT WITHDRAWN.** The fragments did what they said and the files on disk agree ([row 52](#)). What was missing was not correctness - it was anybody DECIDING to run a Publish fragment. **REFUSED BY DEFAULT, EXCEPTION MADE VISIBLE.** `--allow-publish` is typed per run and lands in the shell history. Blocking these outright was the wrong fix: proving them is real work that has to happen, and a gate that makes the necessary thing impossible gets worked around rather than obeyed. Proved three ways - a PUBLISH fragment without the flag is refused and names it; a READ fragment without the flag still runs; the same PUBLISH fragment with the flag runs. | Fixed - `cmd_validate` in `mcp/client/heron_bridge_client.py`. `test_bridge_roundtrip`, `test_caller_values`, `test_validate_agent` and `test_batch_prove` pass |
| **55** | **`delete-elements` PROVED, AND THE EVIDENCE IS THE ONE PAIR OF ROWS WHERE THE INPUT WAS IDENTICAL AND THE ANSWER WAS NOT.** The library's most destructive fragment has no empty case - it deletes whatever it is handed - so D-53 tracking, on `test projject`, Revit 2024: **2 pipes -> `deleted 4` (`alsoWent 2`)**, **2 levels -> `deleted 62` (`alsoWent 60`, `refused 1`)**, 6 ducts -> `deleted 6`, 19 walls -> `deleted 19`. The first two rows both carry `askedFor 2`. **A fragment echoing its input would have said 2 twice**; this one said 4 and 62, which is the cascade its own purpose warns about - *deleting one element can delete others* - measured rather than described. The ducts and walls rows show the other half: where nothing hangs off the selection, `deleted` equals `askedFor` exactly. **AND THE ROLLBACK WAS CHECKED RATHER THAN ASSUMED, WHICH IT HAS NOT BEEN BEFORE.** [Rollback is not reliable](#) - three recorded failures, the third only 17 renamed sheets - and every write proof in this library ends *"Revit reported the transaction group ROLLED BACK"* on the add-in's word alone. Here the model was counted **before and after**: **3,509 elements, then 3,509 again**, across a run that deleted 62 elements including two levels and a run that deleted 19 walls. A delete is the one write where counting elements CAN see the damage, which is exactly why the earlier failures went unnoticed - they were renames. **Counting either side of a destructive proof costs one call and is worth making routine.** | Proved by tracking, awaiting signature. The count check is evidence the model was left alone, NOT independent confirmation of the delete counts - the transaction is gone before anything outside Revit can look |
| **56** | **THE MODEL WAS THE BLOCKER, SO THE MODEL WAS BUILT - AND ONE FLOOR UNBLOCKED `check-surface-fit` AND CONFIRMED [ROW 51](#)'S UNITS FIX IN THE SAME RUN.** `check-surface-fit` fires rays from an element's whole footprint and reports whether it can sit flat against what it hits. On `test projject` there was nothing beneath the ducts, so **both legs came back identical** - `unsafeToMove 6, clean 0`, every finding *NOTHING within reach* - and `evenness` was never reached, which is exactly why row 51's bug had to be found by READING the file rather than by running it. **BUILT WHAT IT NEEDED, WHICH IS THE OWNER'S OWN METHOD:** `report-bounding-box` measured the ducts at X 0-47000, Y 1848-30152, Z 1348-2652 mm; `create-floor` (PROVEN) then laid a `Generic 300mm` slab on Level 1 across 0-48000 x 0-31000, applied for real - one undo step. The model went 3,509 -> 3,520 elements for **one** floor, the other ten being its sketch, which `select-by-category-name` confirms: `Floors` 1, `Lines` 0. **THEN BOTH LEGS SEPARATED CLEANLY:** rays DOWN onto the new slab gave `clean 6, unsafeToMove 0` - *6 sit flat*; rays UP into empty air gave `unsafeToMove 6, clean 0`. Same selection, same tolerance, opposite answers, and the only thing varied was the direction. **`evenness 5` arrived as `0.0164 ft`** - 5/304.8 exactly - so row 51's fix is now measured and not merely reasoned. | Proved, awaiting signature. The floor is a deliberate, permanent addition to `test projject` |
| **57** | **`check-vertical-clearance` CANNOT BE PROVED ON THIS MODEL, AND THE REASON IS A RESULT THAT NO INPUT CAN ZERO.** It reports `tooClose` (pairs tighter than the rule) and `clashing` (pairs that overlap outright), both `role: result`. On `test projject` the 6 ducts hold **exactly one vertical relationship: `clashing 2`** - two of them genuinely overlap - and `tooClose 0`. Varying `requiredGap` from **5000 mm to 1 mm changed neither number**, correctly: a clash is a clash whatever the rule says, and there are no pairs merely *near* each other for the rule to catch. So the gap value cannot drive a negative, and the only selection that would - one without the clashing pair - cannot be made: `select-by-category-name` takes ONE category (`"Ducts,Pipes"` returns 0), and `select-visible-in-view`, which would take everything at once, needs `IList<BuiltInCategory>` - a shape Heron cannot receive (D-54). **What it needs is two services at a real, non-touching vertical separation.** Building that is possible - `create-duct` is PROVEN and takes explicit points - but it also means removing the existing clash first, which is a deletion from the owner's model rather than an addition, and that is his call. **Separately, two of the six test ducts overlapping is worth knowing about the CONTENT** rather than the fragment. | Open - needs content, and the content needs a decision. One of the seven stale proofs, so it is owed a re-run regardless |
| **58** | **A CEILING WAS BUILT, AND THE FIRST OF THE SEVEN STALE PROOFS IS CLOSED ON THE MODEL IN FRONT OF US.** `check-ceiling-coordination` was signed 2026-09-10 against `Project1 work` and went stale in the D-71 units pass; re-proving it needed a ceiling, and `test projject` had none. `create-ceiling` (PROVEN) laid a `600 x 600mm Grid` on Level 1 at 3000 mm - above the ducts, whose top is 2652 mm - applied for real; `heightAboveLevel` came back **9.84 ft**, which is 3000/304.8 exactly. Model 3,520 -> 3,530, one ceiling plus its sketch, and `Ceilings` reports 1. **BOTH LEGS THEN CAME FROM THE TOLERANCE ALONE:** `tolerance 1 mm` -> **`outOfPlane 6`**, every duct out of the ceiling plane; `tolerance 100000 mm` -> **`outOfPlane 0`**. `noCeilingAbove 0` in both, correctly - there IS a ceiling above all six now, where before this run there was nothing above anything. The tolerance arrived as `0.00328 ft` and `328.08 ft` - 1/304.8 and 100000/304.8. **Three of the four fragments proved since the floor went in were blocked by the MODEL rather than by themselves**, which is worth more than any one of the proofs. | Proved. Six stale proofs left |
| **59** | **`place-family-instances` THROWS RATHER THAN COUNTING A FAILURE, AND THAT IS CORRECT - THE FILE ARGUED IT BEFORE I TESTED IT.** Handed `A1 metric`, a TITLEBLOCK, at three free points on Level 1, it answered `operation_failed: An internal error has occurred` - Revit's own words, straight through. That looks exactly like a missing try/catch, and the fragment declares `failed: int`, which makes it look more so. **It is deliberate, and the reasoning is in the file above the loop:** Golden Rule 16 says a failed operation leaves the model UNTOUCHED, the operation owns the TransactionGroup, so an exception rolls the whole layout back and the caller gets a clean model and a reason. Swallowing it would turn that into *37 sprinklers placed, 3 missing, nothing rolled back and no transaction aware anything went wrong* - and whether a layout should be partial or all-or-nothing is the OWNER'S call, not the fragment's. **The model was counted afterwards and was intact** - 3,530 with the ceiling still there. **SO THE NEGATIVE IS TRACKING, NOT A CRASH:** 1 point -> `placed 1`, 2 -> 2, 3 -> 3, 5 -> 5. One instance per point, four different counts. **The lesson is about reading before reporting**: a throw where a count was expected is the shape of a defect, and this one had its defence written twenty lines up. | Not a defect. Proved by tracking, awaiting signature |
| **60** | **THREE UNITS OF EQUIPMENT WERE PLACED, AND TWO MORE OF THE SIX STALE PROOFS CLOSED.** `place-family-instances` was promoted an hour earlier and immediately paid for itself: type `03` at three points on Level 1 landed as **3 Mechanical Equipment**, applied for real. Both checks then came from a VALUE, with the selection identical in each leg. `check-equipment-clearance`: **10000 mm all round -> `blocked 3`**, **1 mm all round -> `blocked 0`**, against 3,533 host-model elements. `report-coverage`: **radius 2000 mm -> `gapSuspects 3`**, **radius 50000 mm -> `gapSuspects 0`**. **THE BUILD-WHAT-IT-NEEDS CHAIN IS NOW THREE DEEP:** the floor unblocked `check-surface-fit`, the ceiling unblocked `check-ceiling-coordination`, and the equipment - placed by a fragment proved the same day - unblocked these two. **Four stale proofs left**, and the ones remaining need valves, sleeves and connected services rather than anything already in the model. | Both proved, awaiting signature |
| **61** | **TWICE IN ONE ROUND A DELIBERATE DECISION LOOKED LIKE A DEFECT, AND BOTH TIMES THE ANSWER WAS ALREADY WRITTEN DOWN.** Worth a row of its own because the failure mode is mine, not the library's, and it is cheap to repeat. **FIRST:** `place-family-instances` handed a titleblock answered `An internal error has occurred` instead of counting a failure - the shape of a missing try/catch, made more convincing by its `failed: int` output. The defence was twenty lines above the loop ([row 59](#)). **SECOND:** `report-coverage` returned `areaEach 135.26` for a 2000 mm radius. π x 2 m² is 12.6, so the number is plainly not square metres - it is **square feet** (2000 mm = 6.5617 ft, π x 6.5617² = 135.26), and the library's own convention puts the unit in the NAME (`report-areas` has `schemeTotalsM2`). A clean finding, except [D-71](DECISIONS.md) already says it: *"`areaEach` and `areaTotal` are PI \* coverageRadius², and with the radius now in feet they are square feet where they used to be square millimetres. That is not a regression... **the result's own unit is a separate question and is not settled here.**"* Deliberate, recorded, and deferred on purpose. **WHAT THE MEASUREMENT DID ADD** is the open question made concrete: a metric modeller typing `2000` gets `135.26` back with nothing on the name saying feet, and now there is a number to argue about instead of a principle. **The rule for next time: before writing a defect row, grep DECISIONS.md and read the fragment's own header.** Both of these cost a detour; a third would have cost a wrong row in a permanent register. | No defect either time. The area-unit question stays OPEN and is D-71's to settle |
| **62** | **`IList<BuiltInCategory>` IS RECEIVABLE, AND I ALMOST WROTE DOWN THAT IT IS NOT.** Three PROVEN selection fragments declare it - `select-in-region`, `select-by-level`, `select-by-parameter-value` - and after `select-visible-in-view` failed with `setup_failed` I read the type, matched it against the shapes D-54 refuses, and was one paragraph from recording *three selectors blocked by an unreceivable type*. **Testing it took one call and it worked:** `select-in-region --set categories="Mechanical Equipment"` returned 3 elements. `RevitFragment.cs:1781` has had `OneBuiltInCategory` all along. `select-visible-in-view` fails for its own reason, not this one. **THIRD FALSE ALARM IN TWO ROUNDS** ([rows 59](#) and [61](#)), and the first two were caught by reading. This one was caught by RUNNING, which is the stronger habit: reading told me the type looked unreceivable and reading was wrong. **Run the one-call test before writing the row.** | No defect. The finding is the near-miss |
| **63** | **FIVE OF THE SEVEN STALE PROOFS ARE CLOSED, AND THE LAST ONE NEEDED A SELECTION NOTHING COULD MAKE BY CATEGORY.** `check-valve-accessibility` reports `obstructed`, `needAccessPanel` and `outOfReach`, all `role: result`. On equipment placed under the new ceiling, `needAccessPanel` was **3 in both legs whatever the envelope did** - there is a ceiling above, and that is a fact about position, not about the rule being tested. Varying the envelope alone gave 5000mm -> 3, 1500mm -> 3, 700mm -> 3, 1mm -> 0: two values, which is the positive/negative pair repeated rather than tracking. **THE ARRANGEMENT WAS THE ANSWER, NOT THE VALUE.** Three more units were placed on **Level 2 at 4000 mm - above the 3000 mm ceiling** - and `select-in-region` cut the model by a Z BOX rather than by category: `0,0,-1000` to `48000,31000,2500` takes the three below, `0,0,3500` to `48000,31000,6000` the three above. Positive: `obstructed 3`, `needAccessPanel 3`. Negative: **all three results zero**. Same fragment, same category, same model - only what is above them differs. **IT ALSO CONFIRMED [ROW 51](#)'S SECOND FIX BY MEASUREMENT:** `envelope 5000` arrived as **16.404 ft** and `1` as **0.00328 ft**. Unconverted, 5000 would have been 5000 FEET and every unit in a building would have read as obstructed. Both halves of row 51 are now measured rather than reasoned. **Two stale proofs left** - `check-sleeve-size` needs openings and `check-vertical-clearance` needs two services at a real separation ([row 57](#)). | Proved, awaiting signature |
| **64** | **[ROW 57](#) IS SOLVED, AND THE ANSWER WAS A BOX RATHER THAN A CATEGORY.** `check-vertical-clearance` was recorded blocked because `test projject`'s only vertical service relationship was `clashing 2` - two ducts overlapping outright - which no `requiredGap` can zero, and the selection that would avoid them could not be made by category. **[Row 62](#) removed that second half**: `select-in-region` cuts by a Z/X/Y BOX and its `categories` binds fine. **SO THE PAIR WAS BUILT.** A box sweep found `30000,18000,0 - 42000,26000,4000` empty of ducts and pipes; `create-duct` (PROVEN) then laid two runs along Y 22000 at **Z 2500 and Z 1500**, a clean 1000 mm centre-to-centre with nothing else nearby. Selecting that box alone: **`requiredGap 2000 mm` -> `tooClose 1, clashing 0`**; **`requiredGap 1 mm` -> `tooClose 0, clashing 0`**. Both results zero in the negative, driven by the value, on a pair with no clash to muddy it. **`check-sleeve-size` CLOSED THE SEVENTH, AND ITS NEGATIVE NEEDS READING CAREFULLY.** Nothing in the model had a service passing through it, so `orphaned` was non-zero for every selection - 3 for equipment, 19 for walls. `create-wall` put a `Generic - 200mm` wall across the two new ducts at X 36000. Positive, 3 equipment with nothing through them: **`orphaned 3`**. Negative, that wall: **`orphaned 0`** - it correctly saw the ducts passing through - and `undersized 0`. **But `undersized` is zero because the wall's opening could not be READ (`unreadable 1`), not because a size was checked and passed.** The discrimination that is genuinely proved is orphaned-vs-not; the sizing arithmetic still needs a real sleeve family with a real hole. | Both proved, awaiting signature. **All seven stale proofs closed.** The sizing half of `check-sleeve-size` is not proved and the row says so |
| **65** | **FIRST BULK RUN AGAINST THE GROUPED LIST: NINE JOBS, ZERO PASSES, AND EVERY FAILURE NAMED ITS OWN CAUSE - WHICH IS WHAT THE BATCH IS FOR.** `--dry-run` said all nine were runnable and all nine ran. Results: **5 POSITIVE EMPTY, 2 NEG NOT EMPTY, 2 DID NOT RUN**. The value is the breakdown, because the nine split three ways and only one of the three is about the fragments. **ONE WAS MY ARRANGEMENT, NOT THE FRAGMENT.** `move-to-ray-hit` came back NEG NOT EMPTY, `snapped 3`. The negative fired rays UP from equipment on Level 1 - straight into the ceiling built an hour earlier. Re-arranged on the Level 2 units, where there genuinely is nothing above: **rays DOWN to the ceiling -> `snapped 3`; rays UP into open air -> `snapped 0, noHit 3`.** Proved. **Building content changes what counts as an empty negative, and the arrangement that worked yesterday may not today.** **ONE WAS A CONTRACT ERROR THE BATCH EXPOSED.** `select-openings` came back NEG NOT EMPTY on `byKind 5 entry(ies)`. Its implementation SEEDS every opening category at zero on purpose - *"no shafts at all is a finding on a building with risers in it"* - so that dictionary holds exactly 5 entries whatever the model contains. The judge reads an entry COUNT, so a deliberate constant was being read as a finding. `byKind` is now `accounting`; the values inside it are still reported. **The verdict changed from a wrong answer to a true one**: `POSITIVE EMPTY - every declared result came back zero (elements, found)`, which is correct - this model has no openings. **ONE WAS MINE TOO, IN THE GROUPING.** `find-clashes` was listed as ready-to-try and refused on `against: ElementId` - it belongs with the thirteen that need one PARTICULAR element, not with the ready ones. **The remaining six are the model**, not the library: no flow in the pipes, no design options, no electrical circuits, no assemblies, no air terminals, no nested families. | 1 proved, 1 contract fixed, 1 regrouped, 6 waiting on content. **The "21 ready now" estimate was optimistic and this measured it** |
| **66** | **THE ROOMS BATCH: FOUR WALLS, TWO ROOMS, AND AN AREA THAT MATCHED THE ARITHMETIC TO NINE DECIMAL PLACES.** `test projject` had no rooms, which blocked eight fragments. Building them took four attempts and each failure was informative. **`place-rooms` needs a PLAN VIEW WHOSE PHASE MATCHES**, and every plan view in this model is on `Existing` - a leftover from proving `set-view-phase` days ago - while all 24 walls are `New Construction` (`select-by-phase --createdPhaseName` confirms: 24 and 0). Revit's own words came straight through: *"Phase associated with view does not match input phase"*. A view created fresh with `create-plan-view` inherited `Existing` too, and `set-view-phase` takes its view from a CHAIN, which `validate` can run but never `--apply`. So that route is shut from the command line. **`place-room-at-point` needs no view at all** and placed one immediately. **THE MISLEADING REFUSAL WAS HONEST AND STILL SENT ME THE WRONG WAY:** `place-rooms` said *"there is no enclosed area on this level... walls that do not meet enclose nothing"*, which reads as a geometry problem. The geometry was fine - the same walls gave `unenclosed 0` the moment a fragment asked without a view. **A refusal can be true about what it saw and still not name the cause.** **THE SECOND ROUTE IS THE BEST EVIDENCE IN THE LIBRARY SO FAR.** The rooms were built to known wall lines, so their areas are computable without asking anything: 9000x7000 mm of 200 mm wall encloses 8800x6800 = **644.112399 ft²**; 8000x5000 encloses 7800x4800 = **403.000806 ft²**. `read-room-geometry` returned 644.112399 and 403.000806. **Largest difference: 0.000000000 ft².** With tracking across the two enclosed rooms and one placed in open ground (`area 0`), that draft came back `gaps: []`. | `read-room-geometry` and `place-room-at-point` proved, awaiting signature. **Rooms now exist, so the other six room fragments are unblocked** |
| **67** | **SECOND BULK RUN - EIGHT JOBS, EIGHT DIFFERENT DIAGNOSES, AND NOT ONE OF THEM THE SAME COMPLAINT.** Run against the rooms and sheets groups on `test projject` after three rooms, two spaces and eight walls were built for it. Verdicts: 2 POSITIVE EMPTY, 1 POSITIVE UNREADABLE, 2 NEG NOT EMPTY, 3 DID NOT RUN. Written out one by one because the value of a batch is the SPREAD - eight separate causes found in one run, where one-at-a-time would have been eight sittings. **MINE, AND THE SECOND TIME:** `check-room-mep-completeness` wants `devices: IList<Element>` - unreceivable - so it belongs with the code-blocked group and not the rooms group. [Row 65](#) caught `find-clashes` the same way. **Grouping a fragment by what it is ABOUT rather than by what it can RECEIVE is what put both in the wrong batch.** **`create-hvac-zone`: setup_failed.** The two MEP Spaces placed for it came back `unenclosed 2` - a space with no bounding volume has no bounding BOX either, so `select-in-region` cannot see it. Spaces need more than the walls a Room needs. **`set-sheet-title-block`: NEG NOT EMPTY, `swappedInPlace 2` - AND THE MODEL HOLDS ONLY ONE TITLE BLOCK TYPE.** `report-sheet-title-blocks` says both sheets already carry `A1 metric`, so setting them to `A1 metric` changed nothing and was still counted. The fragment is honest about the OUTCOME - it reads back and compares `nowThere.Symbol.Id != symbol.Id` before counting - but `swappedInPlace` counts the call, not the change. **[Row 42](#)'s family, and [row 41](#)'s point stands: this wants one rule from the owner, not a ninth patch.** With one type in the model no negative is arrangeable either. **`export-views-to-dwg`: `created 0` and NO FILES ON DISK**, twice, with the folder created first. It is also declared `risk: MODIFY` while writing files to disk, where the other seven file-writers are `PUBLISH` - worth a second look. **`describe-blank-parameters`: `absent` never arrived** - *"nothing usable survived"* - even with `--keep-chain` behind `read-element-parameters`. **`report-findings`: POSITIVE UNREADABLE by construction.** Its only result is `report`, a STRING, and its whole job is to produce prose - including prose that says nothing was found. Neither leg can ever be a number or be empty. **`select-subcomponents`: equipment has no nested families.** **`find-unused-materials`: 60 unused materials in the negative** - it describes the whole document and takes NO input, so tracking cannot vary anything and D-53 has nothing to work with. It needs a second model, exactly as `plan` says. | 0 proved. 8 causes recorded, 2 of them mine |
| **68** | **"SELECT IT IN REVIT AND RUN THIS AGAIN" WAS ADVICE WITH NO WAY TO TAKE IT, AND IT BLOCKED THIRTEEN FRAGMENTS.** `OneElement` refuses a typed name for one particular element, and it is right to - `Element.Name` on an instance returns its TYPE's name, so a name would match every element of that type. The refusal has ended with *SELECT IT IN REVIT and run this again* since 2026-09-10. **There was no word a caller could then type to mean the thing they had selected.** So the sentence read like a workaround and was a dead end, and thirteen fragments sat behind it: `align-elements`, `distribute-along-run`, `filter-elements-by-type`, `join-geometry`, `match-element-type`, `measure-available-fall`, `measure-distance`, `read-ceiling-grid`, `select-by-host`, `select-group-members`, `select-touching`, `trace-connectivity` and `filter-elements-by-id`. **THE WORD IS `selected`, AND IT MEANS EXACTLY ONE.** The selection was already read once in `BindNeeds` for the `IList<Element>` needs; it is now threaded into `FromRequest` and on to `OneElement`. **Zero selected and several selected are BOTH refused, each saying how many were found** - a Revit selection has no order this code may rely on, so taking one of three would be a confident wrong answer wearing a right one's clothes, which is the exact defect the whole function exists to prevent. `selection` and `the selected one` are accepted too; nothing longer, because *selection set* is a different thing in Revit and must not quietly resolve to this. **BUILT AND DEPLOYED FOR ALL THREE INSTALLED RELEASES** - 2020, 2024 and 2027, one at a time, 0 errors each. `test_generate_jobs`, `test_caller_values`, `test_bridge_roundtrip`, `check-api-surface`, `check-metadata` and `check-structure` all pass. `generate-jobs.py` no longer lists these as unreachable; it now says to pass `selected`. **Unverified against a running Revit** - it was closed to do the rebuild, and nothing here has met a model yet. | Built, deployed, untested. The thirteen are arrangeable on paper and that is not the same as proved |
| **69** | **CODEX REVIEWED PR #138 AND FOUND FIVE REAL DEFECTS, INCLUDING ONE IN A `PROVEN` FRAGMENT AND ONE IN MY OWN FIX FROM THE SAME DAY.** Sixteen comments; every one was checked against the code rather than taken on trust, and the five below reproduced. **`align-mep-elevation.targetZ` WENT TO REVIT UNCONVERTED WHILE THE FRAGMENT WAS `PROVEN`.** Its header still read *"Heights are internal FEET"* after D-71, so a modeller asking for 2700 moved the services toward **2700 feet - 823 metres**. That is D-71's own opening example, the 914-metre wall, wearing another fragment's name, and its proof never measured the resulting height. Converted, header rewritten, and **DEMOTED to DRAFT** - the proof describes code that no longer runs. **`set-wall-constraints` LEFT A HALF-SET WALL, AND THE COMMENT EXPLAINING WHY WAS MINE.** [Row 29](#)'s fix sets `WALL_HEIGHT_TYPE` before reading `WALL_TOP_OFFSET`; if the offset is still read-only the wall is counted `refused` - but the constraint has ALREADY been written, and on a normal return with `apply=true` the transaction group is assimilated. My comment said *"the transaction decides what happens to it"*, which is not true of a group that commits. The fragment's own `cases.yaml` forbids it in as many words: *"the id in `refused` and no half-set wall"*. The old constraint is now read before the write and put back before refusing. **DEMOTED to DRAFT.** **THREE IN THE DISPATCH, ALL SHAPED THE SAME WAY - A VALID ANSWER WITH NO WAY TO SAY IT.** `sheetId` resolved through `Element.Name`, which on a ViewSheet is the TITLE; the identifier anybody actually uses is the **SheetNumber** (*"put the door schedule on sheet A101"*), so A101 found nothing. Now number first, title as fallback, ambiguity still refused. An **empty `ElementId` list** was refused outright, blocking a case `set-sheet-revisions/tests/cases.yaml` declares - *"an empty revision list -> nothing changed on any sheet"*. **`ElementId.InvalidElementId`** had no spelling at all, so four declared cases were unreachable: a cap type Revit chooses, a sheet with no title block, a whole-model NWC export, and setting only one of Phase or Phase Filter. `none`, `invalid` and blank now mean it. **TWO I DID NOT ACT ON, AND WHY.** The `28 needs, 26 fragments` row is HISTORICAL - this file's own header says counts inside a row describe the day they were written and are deliberately not updated - and Codex's replacement figure of 24 does not match my own count of 27 either, so I am not editing a frozen number to one I cannot reproduce. Seven further comments argue that proofs on other branches are too weak and the fragments should stay DRAFT; **that is the owner's call, not an edit**, and it is listed for him rather than acted on. | 5 fixed, 2 fragments demoted, 3 releases rebuilt and redeployed. One Codex comment - the `check-valve-accessibility` envelope - was already fixed by [row 51](#) hours earlier, independently |
| **70** | **`selected` MET A REAL REVIT AND BOTH HALVES HELD - THE THIRTEEN ARE REACHABLE.** [Row 68](#) shipped the word untested because Revit had to be closed to build it. On `Snowdon-scratch` (9,638 elements, Revit 2024): with **22 ducts selected** it refused in its own words - *"'target' means ONE particular element and \"selected\" means the one selected in Revit - but 22 are selected. A Revit selection has no order this can rely on"*; with **exactly one** selected it bound it, and the fragment's finding named the thing it got - *"0 element(s) physically overlap 'Tees' (id 1447768)"*. The single-element selection was found by halving a box: 22 ducts in `FloorPlan: M1` -> 12 -> 4 -> 2 -> 1. **BUT SIX OF THE THIRTEEN STILL CANNOT USE IT, AND THE REASON IS STRUCTURAL.** `align-elements`, `join-geometry` and `match-element-type` need ONE element AND a working set of others; `measure-distance` and `measure-available-fall` need TWO particular elements. The Revit selection is one thing and cannot be both a single element and a set, nor two distinguishable ones. **`selected` unblocks the seven that need only a single element** - `select-touching`, `select-by-host`, `select-group-members`, `trace-connectivity`, `read-ceiling-grid`, `filter-elements-by-type`, `distribute-along-run` - and the other six need a second way to say *that one* before they are arrangeable. | Verified. 7 of 13 unblocked, 6 need more |
| **71** | **A CALLER VALUE FOR A NEED THAT DOES NOT EXIST IS SILENTLY DROPPED, AND IT COST A WRONG NEGATIVE BEFORE IT WAS NOTICED.** Proving `trace-connectivity` I passed `--set maxSteps=200` against `--negative-set maxSteps=0` and both legs came back `reached 25`. The fragment looked broken. **It declares no `maxSteps` at all** - only `start`, `elements` and `tolerance` - so the value was accepted, ignored, and never mentioned. Confirmed directly afterwards: `list-levels --set totallyMadeUpValue=42` runs clean. **THIS IS THE MISTYPED-INPUT FAMILY THE LIBRARY ALREADY KNOWS ABOUT** - `generate-jobs.py` exists because *six input names were mistyped on 2026-09-09*, `widthMm` for `width` among them - and that tool only protects a GENERATED job file. A hand-run `validate` or `fragment` takes anything. A mistyped name and an invented one are the same event here, and both read as a fragment that ignores its input. **TOLERANCE WAS NOT THE DRIVER EITHER, AND THAT PART IS THE FRAGMENT BEING RIGHT:** at 25 mm and at 0.0001 mm it still reached 25, because it walks CONNECTORS and `joinedByGeometry` was 0 - `tolerance` only gates the geometry route, which found nothing here. So `trace-connectivity` has no empty case from a connected start, and its negative needs an isolated element or D-53 tracking across different starts. | Open - the silent drop is the finding. Refusing an undeclared caller value, or naming it, would have turned a confusing run into a one-line answer |
| **72** | **EIGHT MORE JOBS, ZERO PASSES, AND EVERY ONE SAID "THIS MODEL DOES NOT HAVE IT" - SO THE REMAINING 81 WERE CLASSIFIED INSTEAD OF GUESSED AT.** Round 2 on `Snowdon-scratch`: `report-areas` -> *"This model has no Area elements at all"*; `place-flow-arrows` -> *"No symbol 'M_Air Flow Arrow : Standard' is loaded - this fragment places an arrow family that is already in the project and loads nothing"*; `find-overlapping-lines` -> *"0 overlapping pair(s) across 22 line(s)"*; and no openings, no design options, no assemblies, no pipes. **Snowdon is four times the size of `test projject` and lacks the same things.** Two batches, 15 jobs, 2 passes - and the passes were both MEP work (`tag-elements-in-view` tagged 14, `filter-elements-by-type` found 22 against 0). **SO THE WHOLE REMAINDER WAS DERIVED RATHER THAN ESTIMATED**, from the contracts and from what a PROVEN fragment can leave in the chain. All 81 accounted for, none twice: **A - 11 need an input Heron cannot receive** (4 of them the same `OverrideGraphicSettings`, so one fix buys four). **B - 6 need a way to say *that one* that `selected` cannot give**: 4 want ONE element AND a working set, 2 want TWO particular elements, and a Revit selection is one thing. **C - 5 need a chain value no PROVEN fragment provides** - `targets`, `parents`, `first`/`second`, `imports`. **D - 59 are arrangeable in principle and blocked by CONTENT or simply untried.** **WHAT THE CLASSIFIER CANNOT SEE, SAID PLAINLY:** it reads declared TYPES, so `find-clashes` lands in D although its `against` is an `ElementId` whose NAME `OneIdNamed` refuses - a name-level refusal looks like a receivable type from here. D is the bucket to distrust; A, B and C are structural and firm. | Not a defect row. The measured answer to *what is actually left*, replacing three earlier estimates that were all too optimistic |
| **73** | **A `MODIFY` FRAGMENT RUN *WITHOUT* `--write` CHANGED THE MODEL FOR REAL AND NOTHING COULD UNDO IT.** `--write` wraps a run in a `TransactionGroup` that is rolled back unless `apply` is given - that is the whole safety story, and it only covers changes Revit makes INSIDE a transaction. **Unloading a link is not one.** Run without `--write` on `Snowdon-scratch`, `unload-links` reported `unloaded 6` and **all six links in the owner's open model were genuinely unloaded**: `list-linked-models` then said `notLoaded 6`. There was no transaction, so there was nothing to roll back and no undo step. They were reloaded immediately - `notLoaded 0`, `RVT Links` back to 6, model still 9,638 elements - but nothing in the tooling made that happen or would have noticed if it had not. **THE SAFETY MODEL READS AS "NO `--write` MEANS NO CHANGE" AND THAT IS NOT WHAT IT GUARANTEES.** It guarantees that TRANSACTIONAL work is rolled back. Document-level operations - link loading, and anything else Revit does outside a transaction - pass straight through. `unload-links` and `reload-links` are `risk: MODIFY`, so the risk gate was satisfied and the transaction gate simply did not apply. **AND IT ANSWERED A QUESTION THE FILE ASKS ABOUT ITSELF.** `unload-links`' header says *"whether Revit permits it inside an open transaction is UNPROVEN here... guessing either way would harden into a fact nobody checked"*. Now measured, both ways: **inside** a transaction (`--write`) -> `unloaded 0, refused 6`, Revit refuses every one; **outside** -> `unloaded 6, refused 0`. So the fragment can only work on the path that cannot be rolled back, which is worth knowing before anybody runs it on a job model. | **OPEN and it is the owner's call.** The proofs are real and both are ready to sign; the hazard is that proving them required changing his model with no undo |
| **74** | **THERE ARE SIX LINKED MODELS AND I TOLD THE OWNER THERE WERE NONE.** My census of `Snowdon-scratch` used `select-by-category-name --inViewOnly`, which reads the HOST document, so Rooms, Doors and everything else living in a link came back 0 and I reported the model as lacking them. He corrected it: *"all the door and rooms are linked file... but door and everything is there"*. `list-linked-models` - PROVEN, and one call - says **6 loaded links**: Architectural, Structural, Facades and three more. **`"RVT Links"` IS A SELECTABLE CATEGORY and returns the 6 instances**, which is what made [row 73](#)'s pair arrangeable. **THE LESSON IS ABOUT THE CENSUS, NOT THE MODEL.** Counting categories in the host and calling the answer *what this model has* is wrong in any federated job, which is most real jobs. **Ask `list-linked-models` first.** It also reopens `select-from-link` and `copy-from-link` for the doors and rooms I had written off, and it is the reason two batches of "this model has no content" were partly measuring the wrong document. | Corrected. `unload-links` and `reload-links` proved as a direct result |
| **75** | **THE CHAIN CANNOT CARRY A LINKED ELEMENT, AND WHEN AN ID COLLIDES IT BINDS THE WRONG ONE SILENTLY.** `select-from-link` is PROVEN and reaches into a link happily - on `Snowdon-scratch` it returns **142 Doors, 54 Rooms, 1128 Walls, 106 Windows** from `Snowdon Towers Sample Architectural.rvt`. **Nothing downstream can consume any of it.** `RevitFragment.Shape()` revives a carried value by walking its ElementIds through **`doc.GetElement(id)` on the HOST document**, and a linked element's id belongs to the LINK's document. **MOSTLY THAT FAILS SAFE AND SAYS SO:** 142 linked doors carried over produced *"'elements (IList<Element>) - nothing usable survived'"*, and so did 54 rooms. The message is honest and the run stops. **BUT 1128 LINKED WALLS PRODUCED `elements from select-from-link (2)`.** Two ids out of 1128 happen to name real elements in the HOST document, so two unrelated host elements were bound in place of linked ones, with a binding note that reads exactly like a successful narrowing. `report-door-room-links` then reported `doorsChecked 0` - it rejected them as non-doors and saved itself - **but a fragment that accepts any element would have acted on two things nobody chose.** **WHAT WAS AND WAS NOT ESTABLISHED:** the counts are measured (142->0, 54->0, 1128->2) and the mechanism is read from `Shape()`. The two surviving elements were NOT identified, so "they are unrelated host elements" follows from the ids belonging to another document rather than from inspecting them. **The safe behaviour and the dangerous one differ only by whether an id number happens to be in use twice.** | OPEN. `select-from-link` is sound alone; the hazard is anything that consumes it. The doors and rooms in the link stay unreachable to every other fragment until a carried value can say which document it came from |
| **76** | **`align-mep-elevation` RE-PROVED, AND THE UNITS FIX IS NOW MEASURED RATHER THAN REASONED.** [Row 69](#) found `targetZ` going to Revit unconverted while the fragment was `PROVEN` - 2700 meaning 2700 FEET - fixed it and demoted it, because the old proof described code that no longer ran. That demotion is now paid back on `Snowdon-scratch`: **22 ducts in `FloorPlan: M1`, `targetZ 3000` arriving as `9.8425 ft`** - 3000/304.8 exactly - and `aligned 22`. The negative is a contrasting selection rather than a nudged value: **10 air terminals, `aligned 0`, `notCurveBased 10`**, because a terminal is not an MEP curve. **THE REST OF ROUND 3 WAS BLOCKED AND EACH SAID SO.** `move-to-ray-hit` was `ALREADY` - proved on `test projject` hours earlier, which is the batch runner refusing to re-prove finished work. `set-sheet-title-block` found no `A1 metric`: Snowdon's 17 sheets all carry `E1 30 x 42 Horizontal_Snowdon` and it is **the only title block type loaded**, so there is nothing to swap TO and [row 67](#)'s finding stands unresolved on a second model. `describe-blank-parameters` again lost `absent` out of the chain. `stack-tags` found no tags to stack and `select-touching` found nothing overlapping the one duct it was given. | 1 proved. The pattern of round 3 is that the HOST document holds ducts and sheets and little else - everything a drawing set is made of lives in the links |
| **77** | **SNOWDON IS WORKSHARED AND HAS A REVISION, WHICH `test projject` NEVER HAD - TWO MORE PROVED, AND ONE OF THEM WAS RECORDED UNREACHABLE IN THREE PLACES.** `list-worksets` says `workshared true`, 2 worksets; `list-revisions` says one, `Seq. 1 - Revision 1`. **`add-revision-cloud` is the one that was written off.** §3i, §6 and [row 37](#) all recorded it unreachable because a revision could not be named, and [row 46](#) withdrew that on the grounds the whole string `"Seq. N - Description"` IS `Element.Name`. **That prediction is now paid**: `revisionId="Seq. 1 - Revision 1"` resolved, and the legs came from the VIEW - `FloorPlan: M1` -> **`created 22, refused 0`**; `3D HVAC Layout` -> **`created 0, refused 22`**, because a 3D view will not take a revision cloud. **`set-element-workset`** moved 22 ducts to workset 1 and refused all 22 for a workset id that does not exist - `moved 22 / refused 0` against `moved 0 / refused 22`. The id had to be guessed: `list-worksets` reports names and a count and **exposes no workset id at all**, which is a gap worth closing, since `worksetId` is declared `int` and nothing in the library can tell a caller what to type. **THREE OF ROUND 4 FAILED BECAUSE MY NEGATIVE WAS MY POSITIVE.** `create-workset-3d-views` made 2 views in both legs, `add-revision-cloud` clouded in both, `report-open-documents` described the same single session twice. Setting `negative-set` to the same values is not a negative, and the runner said so three times in one batch before I read it. | 2 proved. `create-workset-3d-views` and `report-open-documents` have no empty case in one session and need D-53 tracking or a second document |
| **78** | **`distribute-along-run` PROVED WITH ALL THREE LEGS, AND THE THIRD `selected` BRANCH WAS VERIFIED ON THE WAY.** The last of the seven fragments `selected` unblocked. Run against one duct in `Snowdon-scratch` - an **8839.2 mm** run by the fragment's own report - the count follows the spacing exactly: **1000 mm -> 9 placed, 1500 -> 6, 3000 -> 3, 999999 -> 1**. It never reaches zero, because a run always takes the first instance, so D-53 tracking rather than an empty leg. **THE SECOND ROUTE IS ARITHMETIC AND NEEDS NO REVIT.** `floor(length / spacing) + 1` and `length - floor(length/spacing) * spacing`, computed against all four: **9/839.2, 6/1339.2, 3/2839.2, 1/8839.2 - four matches, no differences.** `gaps: []`, the second proof in the library with nothing missing after [row 66](#). **AND THE RESOLVER EARNED ITS KEEP TWICE ALONG THE WAY.** `symbol="Standard"` was refused - *39 family types in this model are called "Standard"* - and so was `"16x4 Connection 8 Diameter Duct"`, which two types share. Both refusals NAMED the alternatives, and the second one handed over the exact spelling that worked: `Supply Grille - Double Deflection - Curve Face Rectangular Neck: 16x4 Connection 8 Diameter Duct`. A resolver that guessed would have placed the wrong family into a run and reported success. **`selected`'S ZERO-SELECTED BRANCH WAS ALSO EXERCISED HERE** - running it outside a setup chain gave *"NOTHING is selected. Click the element in the model and run this again"*. With [row 70](#)'s two, all three branches of [row 68](#)'s change are now measured against a real Revit. | Proved. `selected` is fully verified |
| **79** | **AN EMPTY LIST CANNOT CROSS THE CHAIN, SO `describe-blank-parameters` CAN NEVER RUN - AND IT IS THE SAME ROOT CAUSE AS ROW 69'S DISPATCH FIX, ONE LAYER OVER.** `RevitFragment.Shape()` ends both of its list branches with `Count == 0 ? null : value`, and a null carried value is reported as **`'<name>' - nothing usable survived`**. So a legitimately EMPTY list and *nothing resolved* are the same event to the chain. **MEASURED, AND THE TWO INPUTS ARE MUTUALLY EXCLUSIVE BY DESIGN.** `describe-blank-parameters` needs `blank` AND `absent`, both from `read-element-parameters`, which on 22 ducts in `FloorPlan: M1` returns: `parameterName="Comments"` -> **`blank 22, absent 0`**; a name nothing carries -> **`absent 22, blank 0`**. One of the pair is always empty, the empty one is nulled, and the run stops on it. Four separate attempts today failed this way before the cause was read out of `Shape()`. **THE FIX IS TO SEPARATE TWO THINGS THAT ARE NOT THE SAME**, and it is the same distinction Codex asked for in the dispatch (row 69): a source list that was EMPTY should carry an empty list, while a list whose ids resolved to nothing - [row 75](#)'s linked elements - is genuinely unusable and should stay null. Today both return null, which is why row 75's diagnosis and this one arrive wearing the same message. **It needs an add-in rebuild, so Revit must be closed.** | OPEN. `describe-blank-parameters` is unreachable until then, and any fragment whose input may legitimately be empty is in the same position |
| **80** | **THE CHAIN NOW TELLS AN EMPTY LIST FROM A LIST THAT RESOLVED TO NOTHING, WHICH [ROW 79](#) ASKED FOR AND [ROW 75](#) NEEDED.** `Shape()` ended both list branches with `Count == 0 ? null : value`, and the caller reports a null as *nothing usable survived* - so the two were one event. They are not. **EMPTY IN NOW MEANS EMPTY OUT.** A carried list that was empty crosses as an empty list, because that is a real answer: `describe-blank-parameters` needs `blank` AND `absent` from `read-element-parameters`, and those are mutually exclusive by construction - 22 ducts asked for `Comments` give `blank 22, absent 0`, and a name nothing carries gives `absent 22, blank 0`. One of the pair is ALWAYS empty, so the fragment could never run on any model with any values. **IDS THAT RESOLVE TO NOTHING STILL FAIL, AND DELIBERATELY.** A list that HELD ids and resolved none of them is row 75's linked-element hazard - a linked id belongs to the link's document - and carrying an empty list there would say *there were none* about elements nobody could look at. That branch keeps returning null. The two messages will no longer be the same message. **BUILT AND DEPLOYED FOR 2020, 2024 AND 2027**, 0 errors each. `test_generate_jobs`, `test_caller_values`, `test_bridge_roundtrip`, `test_validate_agent`, `check-api-surface`, `check-metadata`, `check-structure`, `check-reachable` and `check-docs` all pass. **Not yet met a model** - Revit was closed to build it, exactly as [row 68](#) was. | Fixed, untested against Revit. `list-worksets` exposing no workset id ([row 77](#)) was left alone on purpose: adding a provide would stale its proof, and that is a usability gap for the owner to decide, not a correctness fix to slip in |
| **81** | **[ROW 80](#) VERIFIED AGAINST A REAL REVIT, BOTH BRANCHES, AND `describe-blank-parameters` RAN FOR THE FIRST TIME.** On `Snowdon-scratch`, 22 ducts in `FloorPlan: M1`: **the empty list crossed** - the binding note reads `blank from read-element-parameters (22); absent from read-element-parameters (0)`, and the reverse on the other leg, `blank (0); absent (22)`. A `(0)` in a binding note was impossible before this morning. The two findings are correctly different: *"22 element(s) have Comments but it is empty"* against *"22 element(s) do NOT have ZZZ NO SUCH PARAMETER at all"*. **AND THE BRANCH THAT MUST KEEP FAILING STILL DOES.** 142 linked Doors and 54 linked Rooms carried into `count-elements` still answer *nothing usable survived* - they hold ids that resolve to nothing in the host document, which is [row 75](#)'s hazard and not an empty list. The two cases no longer arrive wearing the same message. **IT STILL CANNOT BE SIGNED, AND THAT IS A SECOND FRAGMENT WITH THE SAME SHAPE.** `describe-blank-parameters` declares ONE provide, `findings`, with no role - it exists to turn two lists into a sentence - so the judge reads `POSITIVE UNREADABLE - nothing it returned is a declared result that can be read as a quantity`. `report-findings` ([row 67](#)) is identical: its only result is a string, and its job is prose including prose saying nothing was found. **Two fragments whose whole output is language cannot be proved by a rule that looks for a number**, and that wants one decision from the owner rather than two patched contracts - the same argument as [row 41](#). | Fix verified. Both prose-only fragments remain unprovable and are now a named pair rather than two separate puzzles |
| **82** | **`tag-elements` PROVED THROUGH A THREE-STEP CHAIN, AND IT NEEDS A REAL TAG TYPE WHERE ITS NEIGHBOUR TAKES A HINT.** It wants `elements` AND `alreadyTagged`, and `find-untagged-elements` (PROVEN) leaves **both** - so the arrangement is `select-by-category-name` -> `set-selection` -> `find-untagged-elements` with `--keep-chain`, and the binding note reads `elements from find-untagged-elements (14)`. The legs come from the VIEW, the same shape that worked for [row 77](#)'s revision clouds: **`FloorPlan: M1` -> `tagged 14, refused 0`; `3D HVAC Layout` -> `tagged 0, refused 22`.** **THE FIRST ATTEMPT FAILED ON `tagTypeId=none` AND THAT IS NOT A BUG IN EITHER FRAGMENT.** `tag-elements-in-view` takes `tagTypeHintId` - a HINT - and picks a type itself, so `none` is fine there and it tagged 14 earlier the same day. `tag-elements` passes `tagTypeId` straight into `IndependentTag.Create`, and Revit refuses `InvalidElementId` - all 14 came back `refused` with no finding to explain it. **Two fragments one word apart in name, and the difference between a hint and an id is the whole of it.** `"Duct Size Tag"` - found by listing `Duct Tags` in the view, where 9 already existed - made it work first time. | Proved. The hint-versus-id difference is worth a line in whatever names these two, because the refusal said nothing and the names suggest they are interchangeable |
| **83** | **TWO MORE PROVED BY LOOKING AT WHAT THE MODEL ALREADY HAD RATHER THAN BY BUILDING ANYTHING.** `stack-tags` needed tags and `FloorPlan: M1` already held **9 Duct Size Tags**; `assign-scope-box-to-view` needed a scope box and the model holds **5** (`Grids`, `Overall`, `Block 35`...). Neither was in any earlier estimate of what Snowdon could feed, because the census only ever asked about MEP categories. **`stack-tags`**: 9 Duct Tags -> **`stacked 9`**; 22 Ducts, which are not tags -> **`stacked 0`**. A contrasting selection, same view and same point. **`assign-scope-box-to-view`**: `"Overall"` -> **`assigned 1`**; `"ZZZ NO SUCH BOX"` -> **`assigned 0`**, refused with *no scope box called...* - the value drives it and the refusal names the reason. **THE LESSON IS THE CENSUS AGAIN, THIRD TIME TODAY.** [Row 74](#) was counting the host and missing six links; this is counting MEP categories and missing tags and scope boxes. **Ask what a model HAS before deciding what it lacks** - both of these were arrangeable for hours while being written off as content this model does not have. | 2 proved |
| **84** | **THE CENSUS FINISHED, AND WITH IT THE ANSWER TO *WHAT IS ACTUALLY LEFT*: 72 fragments, 45 of them blocked for a reason MEASURED against a running Revit rather than guessed.** Snowdon's host document was probed category by category instead of by assumption - the mistake made three times today. **It holds** ducts (1053 in `{3D}`), air terminals, mechanical equipment, duct fittings, **duct tags (9 in M1)**, **grids (15)**, levels (11), **scope boxes (5)**, sheets (17), 6 RVT links, 2 worksets and 1 revision. **It holds none of** walls, rooms, doors, windows, ceilings, floors, lines, openings, sections, schedules, areas, spaces, design options, assemblies, parts, CAD imports, electrical, plumbing, sprinklers, furniture, casework, topography, stairs, railings or roofs. **THE BREAKDOWN, ALL 72, NONE TWICE:** **11** need an input the add-in cannot receive - 4 of them the same `OverrideGraphicSettings`. **45** have a measured blocker: 27 are content this model does not have, 5 are ids whose NAME `OneIdNamed` refuses (`against`, `elementIds`, `linkedElementIds`, and two `IList<Element>` requests), 5 need a second open document, 4 have no empty case in one session or one model, 2 output only prose, 1 is broken in Revit itself ([row 45](#)) and 1 needs a phase-matched plan view. **6** need a way to say *that one* that `selected` cannot give - 4 want one element AND a set, 2 want two. **10 remain untried**, and 4 of those are `PUBLISH`/`ADMIN` and wait on `--allow-publish`, which is the owner's word and not mine. **SO THE PROVING JOB IS FINISHED FOR WHAT THIS MACHINE CAN REACH.** What is left is three decisions and two code changes, not more batches. | State of the work, measured. Replaces every earlier estimate |
| **85** | **THE OWNER SAID TO BUILD THE SCHEDULE RATHER THAN WAIT FOR ONE, AND IT PROVED A FRAGMENT PLUS TWO OF THE MORNING'S FIXES IN ONE RUN.** [Row 84](#) listed `place-schedule-on-sheet` as blocked - *no schedule reaches the chain* - which was true and was also a thing that could be fixed in one call: `create-schedule` is PROVEN, so `HERON DUCT SCHEDULE` was made (Type, Size, Length, `missingFields 0`) and `find-schedules` - also PROVEN - carries it. **THE PROOF:** `placed 1` onto sheet **M002 'Notes, Symbols & Schedules'**, which `list-sheets` showed as EMPTY; a name nothing matches gave `placed 0`. **AND THE BINDING NOTES CARRY TWO SEPARATE VERIFICATIONS.** `sheetId="M002"` resolved - **the Codex fix from PR #138**, which changed sheet lookup from `Element.Name` (the title) to SheetNumber, meeting a model for the first time. And the negative reads **`elements from find-schedules (0)`** - an empty list crossing the chain, which is [row 80](#)'s fix doing exactly what it was built for, in a second fragment and by accident rather than by arrangement. **THE LINKS ARE A DIFFERENT ANSWER AND IT IS NOT AS GOOD.** He is right that the architectural link holds the doors, walls and rooms - `select-from-link` reads 142, 1128 and 54 of them. But **only ONE unproven fragment takes a link at all** (`copy-from-link`, and its `linkedElementIds` is refused by name), and everything else would need them through the chain, which [row 75](#) shows cannot carry them. **Building content works; reading it from a link does not, yet.** | 1 proved, 2 earlier fixes verified on a real model |
| **86** | **TWO PROJECTS OPEN AT ONCE UNBLOCKED THE TRANSFER GROUP - TWO PROVED IMMEDIATELY, AND `switch-active-project` TURNED OUT TO BE REFUSED BY REVIT ITSELF.** `report-open-documents` confirms `count 2`: `test projject` (active) and `Snowdon-scratch_ajmal.al`, plus the note that **6 linked models are loaded and deliberately NOT listed** - a link is not an open document. Five fragments were blocked purely by there being one document, and nothing else was wrong with them. **`transfer-materials-between-documents`**: `copied 4` (Concrete Cast-in-Place gray, Vapor Retarder, Asphalt Shingle...) against `copied 0` for a name nothing matches. **`transfer-views-between-documents`**: `copied 6` schedules against `copied 0`. **The first attempt failed on `viewKind: "Schedule"`** - the fragment matches `templates`/`legends`/`drafting`/**`schedules`**, lower-case and plural, and said so precisely: *Nothing of kind 'Schedule' matched*. **`switch-active-project` IS REFUSED BY REVIT, NOT BY THE ARRANGEMENT.** Asked to switch to a view in the OTHER document it answered *Revit refused to change to the view*; asked to switch to a view in the document that was ALREADY ACTIVE it answered the same. **`RequestViewChange` fails from this context regardless of target**, which the fragment's own header half-predicts - *THE SWITCH IS REQUESTED, NOT CONFIRMED... nothing here can read back the new active document*. **`transfer-project-parameters-between-documents` was the honest near-miss**: `bound 0` but `clashed 1 [Sub-Discipline (already bound here, left alone)]`. It found the parameter and correctly declined to rebind it. It needs a shared parameter the target does NOT already carry. | 2 proved. `switch-active-project` is a Revit-side refusal and belongs beside [row 45](#) |
| **87** | **`batch-prove` TURNED A REFUSAL INTO A RESULT, AND I REPORTED THE RESULT AS A MEASUREMENT.** The runner asked only `if not os.path.isfile(record_path)` before judging - *the file is there* is not *this run wrote it*. Any record left by any earlier run, session or model was read and judged as if fresh. **MEASURED 2026-09-14 AND IT HAD ALREADY MISLED ME.** `transfer-project-parameters-between-documents` is `risk: ADMIN`, so `validate` refused it and wrote nothing ([row 54](#)'s gate working exactly as designed). The runner then read a record written at **01:45 the previous day, Revit session 47740**, and reported `POSITIVE EMPTY (bound, regrouped)` with a finding - `clashed 1 [Sub-Discipline (already bound here, left alone)]` - that I passed on to the owner as that afternoon's result. It was the day before's, on a different session. **THE FIX IS TO DELETE THE RECORD BEFORE THE RUN**, which makes the existing check honest rather than adding a second one: no file means nothing ran, which is what `DID NOT RUN` is for. Verified by re-running the same job file - the ADMIN fragment now reports **`DID NOT RUN`** naming the risk gate, where minutes earlier it had reported numbers. **THE SHAPE IS THE ONE THIS REPOSITORY KEEPS MEETING:** an absence and a stale answer look identical unless something insists on freshness - the same root as [row 79](#)'s empty-versus-unresolved and [row 75](#)'s ids that resolve to nothing. **A runner that cannot tell "nothing happened" from "something happened once" will eventually launder one into the other.** | Fixed. `test_batch_prove` passes. The stale finding I quoted is withdrawn |
| **88** | **TWO CORRECTIONS TO MY OWN BLOCKER LIST, AND A HONEST STOP.** [Row 84](#) recorded `export-views-to-dwg` as *wrote no files*, which reads as a defect. It is not: the fragment **refuses an empty `setupName` on purpose**, and says why - *"Revit's defaults produce a file that opens perfectly and fails the recipient's CAD standard, which is the worst kind of wrong because nothing looks broken"*. It needs a **named DWG export setup saved in the model**, and neither model has one. Content, not a fault, and the refusal is the fragment being right. **`find-unused-materials` AND `find-unused-families` CANNOT BE TRACKED ACROSS THESE TWO MODELS EITHER.** Both report **`unusedMaterials 60`** - `test projject` and `Snowdon-scratch` alike, because both descend from the same Revit template. `scannedInstances 3565` confirms it read the right document. Two models, one number: D-53 needs the answer to MOVE with the input, and it does not move here. They need a model from a different template, not merely a second model. **WHERE THE WORK STANDS: 291 proved, 69 left, and the batches have stopped paying.** The last four rounds returned 1, 2, 1 and 2 passes for 23 jobs, and every failure was one of the blockers already written down. What remains is not more arranging - it is two code changes (the links fix, the graphics input), one permission, and two decisions about how a fragment that answers in words or reports state rather than work should be judged. | Not a defect row. The list corrected, and the reason for stopping said out loud |
| **89** | **THE OWNER ARRANGED THIS ONE HIMSELF AND IT BROKE [ROW 67](#) OPEN.** He said: *"set-sheet-title-block FOR THIS I CREATE A SHEET WITHOUT TITLE THAT IS GOOD AM I RGHT"*. He was right, and for a reason worth writing down. [Row 67](#) and [row 76](#) both failed this fragment on the SAME thing - every sheet in both models already carried the only title block type loaded, so setting it changed nothing and `swappedInPlace` counted the call anyway. **A sheet with NO title block is the one arrangement that escapes that**, because the fragment takes its other branch: it PLACES an instance, reads it back and fills `replaced`, and `replaced` cannot be filled by a call that did nothing. `report-sheet-title-blocks` on `test projject` confirmed the rig before anything was run - `HERON-01` and `HERON-04` on `A1 metric`, **`HERON-05 - NO TITLE BLOCK**. **THE FIRST RUN STILL FAILED, AND THE FAILURE WAS THE DEFECT.** Positive `replaced 1`, and the negative - `HERON-01`, already on `A1 metric` - came back `swappedInPlace 1`. `expect:` narrows the POSITIVE only; the negative is judged across every declared result, so the counter that counts the call was judged and the whole thing read `NEG NOT EMPTY`. **THE FIX IS THE FRAGMENT'S OWN CASES FILE ARGUING AGAINST IT.** Its first positive case says *"the same family, A DIFFERENT TYPE"* - so `swappedInPlace` was never meant to count a sheet that was already there. The type id is now read BEFORE the assignment (after it, `onThisSheet` IS the new type - the same instance re-pointed, with nothing left to compare) and an unchanged one goes to a new `alreadyCarried`, `role: accounting`. **AND THE NO-TITLE-BLOCK LINE STOPPED LYING**: it said *'(no title block)' was DELETED and replaced. Anything typed into that title block instance is gone* about a sheet that never had one. **Re-run: PASS** - `replaced 1 [sheet HERON-05 - had NO title block, and 'A1 metric : A1 metric' was placed on it. Nothing was lost; there was nothing there]` against a negative that now returns nothing at all. **THIS IS ONE OF [ROW 41](#)'s NINE, SETTLED BY EVIDENCE RATHER THAN BY A BLANKET RULE** - the fragment's own declared case decided it, not a judgement call about counting. | 1 PASS, waiting on a signature. Row 67's complaint is closed |
| **90** | **`switch-active-project` HAD TWO BUGS AND A THIRD THING THAT IS NOT A BUG AT ALL, AND NONE OF IT WAS VISIBLE BECAUSE THE REPLY TRUNCATES.** [Row 86](#) recorded it as *refused by Revit regardless of target* and left it there; the actual sentence Revit says had been cut off the end of the finding since 2026-09-10. **`Describe` renders a list item SHORT**, so a refusal that opens with sixty characters of our own context arrives with the only words that matter missing. Reversing the order - Revit's message first, our context after - is now permanent in the fragment, and it produced this in one run: **"Changing the active view is not applicable to inactive documents."** **THAT RETIRES THE FRAGMENT'S PREMISE.** Its header says *IT SWITCHES BY CHANGING THE VIEW, BECAUSE THAT IS WHAT REVIT OFFERS*. `RequestViewChange` changes the view WITHIN the document that is already active; it cannot bring another one forward, and there is no other call that can. This is not an arrangement problem, a context problem or a UIDocument problem - **it is the API declining the operation the capability is named after**, and it belongs beside [row 45](#) rather than in any batch. **THE TWO REAL BUGS WERE FOUND ON THE WAY AND ARE FIXED.** (1) `ReferenceEquals(wantedDoc, current)` - `app.Documents` and `uidoc.Document` hand back DIFFERENT managed wrappers around one open file, so the already-active case answered "no" for the model you are standing in and fell through to a view change Revit then refused. **That is why row 86 saw the same refusal in both directions.** Compared by `PathName` now, and `target="test projject"` answers correctly: *"test projject" is already the active project. Nothing was changed, and nothing needed to be.* (2) `uidoc.RequestViewChange` was asked with a view belonging to the OTHER document; a `UIDocument` constructed for the target is the one that can be asked. Both stand on their own even though the capability cannot be reached. **ALL THREE FOUND WITHOUT CLOSING REVIT** - `heron_bridge_client` reads `impl/any/fragment.cs` off disk on every call and sends it, so fragment source is live and only the add-in needs a rebuild. | 2 bugs fixed, 1 capability retired as not offered by the API |
| **91** | **THE OWNER CLOSED REVIT AND THE FOUR REFUSED VALUE TYPES WERE BUILT - FIVE FRAGMENTS ARRANGEABLE, AND ONE THAT NEVER WILL BE.** He named twelve tools; six of them were blocked not by any model but by `FromRequest` having no way to turn text into the object the contract asked for. Four are now built and one is refused ON PURPOSE, and the difference is the row. **BUILT:** `IList<IList<XYZ>>` - a PIPE between pairs, `"0,0,0; 5000,0,0 | 0,0,0; 0,5000,0"` - which [D-67](#) had deferred with *"a decision to make when a second fragment wants one"*; no second fragment came, the OWNER asked for `create-line` by name, and that is the same signal. `OverrideGraphicSettings` - nine settings, `"halftone=true; transparency=50"`, **an unknown key refused by NAMING the nine** so a wrong spelling is one line from a right one. `ForgeTypeId` - `"Length"`, `"Text"`, `"YesNo"`. `ParameterValue` - **the KIND first**, `"length 2700"`, because 2700 alone is a length, a count or a price depending on where it lands and a built ParameterValue has that choice already inside it. **AND A LIST OF IDS NOW TAKES `selected`**, which [row 68](#) claimed was already true and was **wrong about**: the word reached `OneElement` and never reached the id-list branch, so `filter-elements-by-id` sat unreachable for four days while the register said it was not. **REFUSED, AND NOT 'YET':** `IList<Reference>` is a FACE - a particular solid, on a particular element, seen in a particular view, produced by a mouse coming to rest on geometry. **No text names one.** It is not a rule waiting to be written, so it is now a NAMED refusal in both halves rather than falling to a catch-all that ends *"this is not one of them yet"*. `place-family-on-face` needs Revit's own picking. **TWO THINGS WERE MEASURED RATHER THAN READ.** `ForgeTypeId` is fetched by REFLECTION because the type arrived at Revit 2021 and this add-in compiles 2020-2027 from one source - naming it would break the 2020 build outright - and **every one of the ten names was then checked against the real `RevitAPI.dll` of Revit 2024**: `Length`, `Number`, `Angle`, `Area`, `Volume`, `Currency`, `Mass`, `String.Text`, `Boolean.YesNo`, `Int.Integer`, all present. A reflection name is the one kind of mistake a green compile cannot catch. And **all eight releases compile**, 2020 through 2027, which is what proves `SetSurfaceForegroundPatternColor`, `SetDetailLevel` and the three `ParameterValue` classes exist as far back as 2020. **BUILT AND DEPLOYED FOR 2020, 2024 AND 2027**, one at a time, hashes checked distinct because the output folder is shared and a build that silently did not run deploys the previous release's assembly. **NOT ONE OF IT HAS MET A MODEL** - Revit was closed to do the work, which is [row 68](#)'s position exactly, and [row 70](#) is what paying it back looks like. | 4 types built, 1 refused by name, 5 fragments arrangeable on paper |
| **92** | **FIVE FOR FIVE - THE NEW INPUTS MET A MODEL AND EVERY ONE OF THEM HELD.** [Row 91](#) shipped four value types built and deployed with Revit closed and said plainly that none had met a model. This is paying it back, on `Snowdon-scratch_ajmal.al` (9,638 elements, 6 links, Revit 2024, session 28236). **`create-line`: `created 2 [DetailLine, DetailLine]`** from `"0,0,0; 5000,0,0 | 0,0,0; 0,5000,0"` - the pipe separator works, and it is the first time anything has reached this fragment by ANY route. Its negative is its own declared case: two points typed the same gives `created 0, tooShort 1` and the fragment names the reason. **`filter-elements-by-id`: `elements 22`** with `elementIds: selected`, against `elements 0, missing 0` for a deliberately blank list - both halves of [row 80](#)'s empty-list fix arriving by the front door. **`override-graphics-in-view`: `overridden 22`** against **`overridden 0, skipped 22`** on a schedule: the view is asked ONCE whether it allows overrides, and every element is accounted for rather than half the view being changed. **`set-link-graphics`: `appliedTo 6`**, each link named, against a refusal that says *'Space Schedule' (Schedule) does not accept graphic overrides at all*. **AND THE ONE THAT FAILED FIRST TAUGHT THE MOST.** `set-category-graphics` came back `overridden 0, refused [Ducts]` on `FloorPlan: M1` - **because M1 carries a view template, and a template holding V/G makes `SetCategoryOverrides` throw.** Element overrides still work on that same view in that same run, which is the distinction: `AreGraphicsOverridesAllowed()` is true and the CATEGORY half is still locked. `find-views-without-template` named six working views; `Model Linking` gives `overridden 1`. **So M1 became the NEGATIVE and it is a better one than the schedule would have been** - it is the case the fragment's own header calls the one that actually happens in real projects, *"the user asks for the grayout, the view has a template, and nothing appears to happen"*, and the fragment answers it by NAMING the category instead of reporting a success that changed nothing. **THE MODEL WAS CHECKED AFTERWARDS AND IS UNTOUCHED** - `0 lines` in M1, `0 of 15` categories overridden in `Model Linking`, `0 of 22` ducts and `0 of 6` links carrying a per-element override. Every leg ran under `--write` with no `--apply`, and this time the rollback held on all five. | 5 PASS from 5 jobs. The best return of any batch this project has run |
| **93** | **A FRAGMENT WHOSE ONLY RESULT IS A YES/NO COULD NOT BE PROVED BY ANY ARRANGEMENT, AND NOTHING SAID SO.** `apply-view-filter` was arranged as well as it can be on `Snowdon-scratch`: **`applied true` on `Model Linking`**, which took the filter, against **`applied false` on `Project View`**, which refused it in Revit's own words. That is D-30's comparison exactly, and the runner said **POSITIVE EMPTY**. **THE CAUSE IS A RULE THAT IS RIGHT AND WAS TOO BROAD.** `_as_count` reads a boolean as ZERO on purpose: `report-global-parameters` answers `allowed: true` in BOTH legs because the document permits globals either way, and counting that as content would make an empty answer impossible for it to demonstrate. True - and it also meant that a fragment declaring **one result, of type `bool`**, was unprovable however it was run. No view, no filter and no override could have answered it, because the judge was not reading the value at all. **THE FIX IS A COMPARISON, NOT A LOOSENING.** `_flag_flipped` asks whether a DECLARED RESULT is true in the positive and false in the negative, and it is strictly stronger than counting one leg: a flag true in both phases - the case the zero rule exists for - cannot pass it; a fragment that succeeds while doing nothing reports the same flag twice and cannot pass it; an `accounting` flag cannot pass it whatever it does (D-52). Same move as [row 79](#)'s `_result_appeared`, which rescues a result that is a THING rather than a number, and it is checked BEFORE that one because a boolean arrives as EMPTY rather than as unreadable. **MEASURED, NOT ESTIMATED: 5 fragments in the library declare nothing but booleans**, and only 2 were DRAFT - this one and `switch-active-project`, which [row 90](#) retired. So the unlock is ONE fragment, and it was a total block rather than a hard case. The other three passed on side-results that happened to be countable, which is luck, not a route. **BOTH JUDGES ASK THE SAME FUNCTION**, because a job reading PASS in `batch-prove` and then being refused at `accept` is worse than either behaviour alone. `test_batch_prove` covers the pass AND both refusals. | 1 blocked fragment freed; the rule it was blocked by kept intact |
| **94** | **THE OWNER OPENED A SECOND PROJECT AND ASKED FOR `switch-active-project` TO BE TRIED PROPERLY. IT WAS, FOUR WAYS, AND REVIT REFUSES ALL FOUR.** [Row 90](#) retired this capability on one measurement and he was right to push back on that - one refusal can be a wrong call rather than a wall. So it was tried again with two projects **genuinely open**, `test projject` in front and `Snowdon-scratch_ajmal.al` behind, and every attempt answered with the same sentence, which is Revit's own: ***"Changing the active view is not applicable to inactive documents."*** (1) `RequestViewChange` on the host's `UIDocument` - refused. (2) `RequestViewChange` on a `UIDocument` **constructed for the target** - refused; this was row 90's fix and it is still the right code, it just cannot help. (3) the **`ActiveView` SETTER** on that same target `UIDocument`, a different call with a different rule - refused, word for word. (4) naming a view **already open** in the target, in case the restriction was about opening rather than about the document - refused. **THE FALLBACK CODE WAS REMOVED AGAIN RATHER THAN KEPT.** A try/catch whose second branch never succeeds is dead code that reads like a safety net; what is kept is the MEASUREMENT, written into the fragment's own header where the next person meets it before they spend an afternoon. **`ShowElements` WAS DELIBERATELY NOT TRIED.** It can raise a modal message box when it cannot find a view, and a modal dialog inside the bridge's API context hangs the session until somebody clicks it - a risk to his open model for a route that would still be Revit choosing the view. **WHAT IS AND IS NOT BROKEN.** Everything this fragment is built out of works and was checked: path-before-title matching, two matches refused rather than guessed, already-there reported as success, and its own honesty that a request is not a confirmation. **The operation itself is not offered by the API.** It cannot be proved by any arrangement, and it is sitting in the DRAFT count as though it were work outstanding. **Whether it stays in that count is the owner's call, not a tool's** - it is one of the 63. | Not a defect. 4 routes measured, 1 capability confirmed unreachable |
| **95** | **`prove` AND `validate` CHOSE A REVIT SESSION BY TAKING THE LOWEST PID, AND NEVER SAID WHICH.** Both read `live[0]` from the session list with no way to name one, while `cmd_fragment` went the other way and looped over **every** live session. With two Revits open — `PIPE` and a second model — `validate` ran the whole arrangement against the wrong one and returned **`positive ok`** on a phase whose own accounting read **`scanned 0`**. **That is the failure this project exists to refuse:** a green proof taken against a model that never held the arrangement. Not a Revit defect and not a fragment defect — the client picked for you and stayed quiet about it | **FIXED 2026-09-16.** `--session <pid>` on `fragment`, `prove` and `validate`. `only_session()` **refuses** when more than one session is live and none is named, rather than guessing; `pull_session()` pins the chosen one. The refusal is the point — the old behaviour's whole problem was that it always had an answer |
| **96** | **`binds:` IS HONOURED BY THE PYTHON HALF AND NEVER READ BY THE C# EXECUTOR.** `brain/heron_fragment.py:244` (`need_binds`) reads a need's `binds:` key and treats the contract as satisfiable. `revit/Heron.Revit.Addin/RevitFragment.cs` binds by the need's NAME and TYPE and **never opens `binds` at all**, so a fragment relying on it arrives at Revit with the need unbound and stops with `needs_unbound`. **5 fragments cannot be run by any route**, and the two halves disagreeing is what makes it invisible: the shape gate passes, the store indexes it, and only a live run says otherwise | **FIXED 2026-09-16 - and this cell read `OPEN` for a day after it was closed.** Found 2026-09-16 building the MEP set, **closed 2026-09-15 by the session it names** - the row was filed against a defect another session had already fixed, and the closing sentence was appended without changing the word the reader sees first. `Binds()` in `RevitFragment.cs` now resolves the chain lookup through the alias ([row 100](#)). - see [row 100](#), which measured what it actually did: not `needs_unbound` but a silent bind of the Revit selection. The executor now reads `binds`. Two possible fixes were open and they were **not** equivalent — teach the executor to read `binds`, or make the Python half refuse a key the executor cannot honour. The second is safer and smaller; the first is what the contracts were written expecting |
| **97** | **`revit_change` REFUSED ITS OWN CHANGE, because one document can be keyed two ways.** `mcp/server/heron_write.py:233` (`key_of`) keys the pinned document by whichever id field the reply happened to carry. An **unsaved** model has no path, so one call keys it by title and the next by session id — two keys for one document — and the pin written by the preview does not match the one the apply looks up. The refusal reads like a permission decline, which sends a reader to the wrong place entirely | **FIXED - landed as #147 `c7162df`, *"Every write refused against the model it was already pinned to"*.** Confirmed 2026-09-16 by reading the code: `DocumentPin.identity_of` keeps **every** identity field the add-in sent instead of collapsing them, and `_common` compares the most specific field **both** sides carry - *"This is the whole fix. Comparing collapsed keys compares whatever each side happened to win with."* `RevitFragment.Report` now sends `documentPath` and `projectKey` too, so a fragment reply can no longer be the one operation that answers with a title alone. Found 2026-09-16 moving a pipe; the separate session named below (`task_1441f96f`) is the one that finished it. Worked around on the day with `run_fragment_write`, which pins nothing |
| **98** | **A READING FRAGMENT CLOSED SIX FAMILY WINDOWS THE OWNER HAD OPEN, AND UNSAVED WORK WENT WITH THEM.** `report-family-tables-in-project` opens every family in the project, reads its lookup table and closes it again — including the ones **already open in Revit's own window**. Six families the owner was part-way through fixing and had not yet loaded back. His words: *"whille your cheking you close all the family i did not lode to the project"*. **A `risk: READ` fragment destroyed work**, and no gate here could have caught it: it opened no transaction and changed no element, so every check reports it clean | **FIXED 2026-09-16.** The fragment records `app.Documents` **before** it opens anything (`wasOpenBefore`, by title) and never closes a document it did not open — one it found already open is reported in `leftOpen` instead. **The durable rule is not about families:** anything that opens documents closes only what it opened. `risk:` describes what happens to the MODEL, not to the session, and a window somebody is working in is neither |
| **99** | **GOLDEN RULE 16 IS ENFORCED AS "NO FRAGMENT OPENS A `Transaction`", AND NOTHING HAD EVER NEEDED A SECOND DOCUMENT.** `tests/test_revit_gate.py` Q4 greps for `new Transaction(` across the library and had always counted zero. `write-family-table-in-project` (FRG-ELE-060) is the first to trip it — and its transaction is on a FAMILY document it opened itself, not on the one the executor handed it. The executor never sees that document, holds no `TransactionGroup` over it, and cannot commit it; leaving it uncommitted discards the import. So the rule's PURPOSE — one job, one `TransactionGroup`, one undo on the model — is arguably untouched, while its TEXT is plainly broken. **Revit requires a transaction to modify any document, so as written the rule makes editing a second document impossible for every fragment, forever.** That is a capability question, not a safety property | **OPEN as a question, 2026-09-16, and the fragment is WITHHELD rather than the gate widened.** FRG-ELE-060 was built, proved and then left out of the change that introduced it — partly for this, and partly because its one write leg does not work anyway (`Document.LoadFamily` needs an `IFamilyLoadOptions` and **a fragment cannot declare a class**, which is `load-family`'s ruling arriving a second time). **Widening a gate in the same change that trips it is the wrong order**, and it would have been done to admit a fragment that cannot finish its job. The question to settle first: does Rule 16 mean *the executor's document* or *any document*? Today it means any, because nothing had ever asked. The route that WORKS is `open-family-for-editing` → `write-family-size-table` → the owner clicks Load into Project |
| **100** | **[Row 96](#) MEASURED: THE EXECUTOR NEVER READ `binds:`, AND WHAT IT DID INSTEAD WAS QUIETLY BIND THE REVIT SELECTION.** Row 96 recorded this as `needs_unbound` and open; that is the symptom when nothing is selected, and it is not the dangerous one. Five fragments declare a need that is filled by a provide under ANOTHER name — `targets` with `binds: elements` (`find-nearest-elements`, `check-minimum-clearance`, `extract-cad-curves`) and `first`/`second` both bound to `elements` (`unjoin-geometry`, `switch-join-order`). `need_binds` at `brain/heron_fragment.py:244` has honoured the key since it existed — composition, the graph and `generate-jobs.py` all read it — and `RevitFragment.cs` looked every need up under its own name. **The reported symptom was `needs_unbound`, and the measured one was worse.** Run on Project1 (3,174 elements, Revit 2020, session 23804) 2026-09-15, `validate find-nearest-elements --setup select-by-categories --keep-chain --set categories=OST_PipeCurves,OST_Walls --set "inViewOnly=FloorPlan: 1 - Mech"`, the reply's own binding note read **`elements from select-by-categories (2); targets from the selection (1)`** and both phases reported `ok`. `targets` was not found in the chain, fell through to the selection branch as the only unbound list of elements, and bound **whatever happened to be left selected in Revit** — one element, with no bounding box, so `targetBoxes 0` and `unmeasurable 2`. A refusal is visible on the page; this is a clean-looking run measuring two pipes against one unrelated leftover. It is exactly the stale-selection failure the proving skill's rule 5 exists for, arriving from the host rather than from the arrangement. | Fixed — `Binds()` in `RevitFragment.cs` resolves the chain lookup through the alias, the read-back now names it (`targets from select-by-categories as 'elements' (2)`) and the refusal names the name actually looked for. `tests/test_fragment_needs.py` now fails on ANY need key the library uses and the executor never reads, which is the general form of this bug; checked against the pre-fix file, it reports `binds`. Proof: row 97 |
| **101** | **`optional: true` IS DECLARED ON THREE NEEDS AND READ BY NOTHING AT ALL.** Found 2026-09-15 by the same sweep as row 95 — the library puts five keys on a need (`name`, `type`, `source`, `binds`, `optional`) and the executor read three of them. `copy-view-filters`, `create-line` and `create-sheet-list` each mark one need `optional: true`; all three are `source: request`, so `BindNeeds` reports them as values the caller must supply and the fragment cannot run without one. Neither `RevitFragment.cs` nor `heron_fragment.py` mentions the key. **Not fixed, deliberately:** "a need that may be absent" is a decision about refusals, and this library refuses rather than quietly supplying an empty value on purpose — `BindNeeds` says so in its own header. Recorded rather than invented. | Open — listed in `UNREAD_KEYS` in `tests/test_fragment_needs.py` so it is accounted for rather than forgotten, and the test fails if a SIXTH key appears |
| **102** | **`check-minimum-clearance` USED ITS PER-CATEGORY CLEARANCES RAW, IN FEET — AND ONLY THE PAIRS A RULE JUDGED.** The D-71 units pass recorded in [row 51](#) converted one length per fragment and walked past the second; this is the third instance and the first in a DICTIONARY, which is why that sweep could not have found it — it looked for a `double` request input with no conversion, and `rules` is an `IDictionary<string, double>`. `defaultClearance` was converted on line 29 and every value inside `rules` was used exactly as typed against a gap measured in feet, so `rules="Walls=150"` asked for 150 mm and demanded **150 FEET**. The shape of the wrong answer is the dangerous part: the default-judged pairs stayed correct while the rule-judged ones were 304.8× out, so the report reads as a model full of clashes with the rule column beside them looking like the explanation rather than the fault. **Found by reading, not by a run**, and it could not have been found by a run — see row 98. | Fixed — `rules` is converted into a local `clearances` dictionary once, before use, copied rather than written back so a re-run on the same values cannot divide twice. **Unverified against a model**: `rules` cannot be typed at all (row 98), so no arrangement on any model reaches the line |
| **103** | **`check-minimum-clearance` CANNOT BE PROVED AT ALL, BY ANY ARRANGEMENT, AND IT IS `rules` THAT STOPS IT.** Asked for 2026-09-15 alongside `find-nearest-elements`; run both ways on Project1, Revit 2020, session 23804, and refused both ways. Without the value: `[needs_request_values]` — *"'rules (IDictionary<string, double>)' is a value the CALLER supplies"*. With one: `--set "rules=Walls=150"` gives `[bad_request_value]` — *"\"IDictionary<string, double>\" is not one of them yet, so this fragment still has no way to receive it"*. `FromRequest` refuses `IDictionary` by name, and the need is `source: request` with no way to omit it (row 96), so the two refusals close the loop on each other. `tools/jobs/defect-finders-2.yaml` predicted exactly this in its own note — *"`rules` is a dictionary and is left unset — if the fragment demands it, that is a D-54 gap worth recording rather than working around"* — and it demands it. **This is the machinery, not the model:** no second model, view, category or threshold changes it. | Open — a syntax decision of the same class as [D-72](DECISIONS.md)'s four, and the owner's to make. Section 6 below now counts it |
| **104** | **A FRAGMENT THAT RECURSES WITHOUT A FLOOR DOES NOT RETURN AN ERROR - IT ENDS REVIT.** `RunScript` catches `Exception` and its own comment reads *"a fragment that throws is a finding, not a crash"*. True of every exception but one: **`StackOverflowException` cannot be caught.** Since .NET 2.0 the runtime fails fast - no handler, no `finally`, nothing written, nothing asked - and the process it ends is **Revit, holding the user's unsaved model**. Nothing in the add-in guarded against it. Found 2026-09-16 reading an unrelated open-source WPF app that had solved the same problem for its own scripting host. **Zero of the 372 fragments recurse today**, so this was a door standing open rather than a fire - but a fragment's `.cs` is LIVE, read off disk and sent on every call, so the door is one edit wide | **FIXED IN CODE 2026-09-16, NOT YET PROVED IN FRONT OF REVIT - [J9](NEEDS-CHECKING.md).** `HeronStackGuard` injects `RuntimeHelpers.EnsureSufficientExecutionStack()`, which throws a **catchable** exception just before the stack runs out, so `RunScript`'s existing catch handles it. **THE IDEA WAS TAKEN AND THE CODE WAS NOT, and that distinction is the whole row.** The usual form of this rewriter guards named members and deliberately skips lambdas, on the stated grounds that recursion through anonymous functions alone is not a real shape. **Measured here: 0 of 372 fragments declare a method or a local function** - a fragment is a script of top-level statements and `Func<>` lambdas - so the received rule, applied unexamined, would have shipped a guard that compiles, reviews well and protects **nothing**. Guarding block-bodied lambdas instead covers **91**. An EXPRESSION-bodied lambda is still unguarded and that is deliberate: rewriting one can change `Func` to `Expression` and pick a different overload. Proof: `tests/Heron.StackGuard.TestHost`, 836 checks over the whole library - line counts unchanged, no new diagnostic, and every one of the 91 guarded |
| **105** | **TWO WRITES THAT LOOKED ATOMIC AND WERE WORSE THAN A PLAIN OVERWRITE.** `HeronConfig.Save` and `BridgeIdentity` both wrote a sibling `.tmp` and then did `if (File.Exists(target)) File.Delete(target); File.Move(tmp, target);`. A plain write leaves a **truncated** file if interrupted; delete-then-move leaves **no file at all**, through a window made wider by the extra syscall inside it. **In `HeronConfig` it does not corrupt the settings, it erases them, silently:** `Load` answers a missing file with `Defaults` and swallows `IOException` without a word, and the next `Save` writes those defaults back - so the file whose own header says *"Owned by you. Never overwritten by an update"* is gone with no message. In `BridgeIdentity` the comment read *"so a client never reads a half-written file"*, which is true and does not cover a client reading **no** file and concluding no bridge is running. Found 2026-09-16 | **FIXED 2026-09-16.** `HeronAtomicWrite` - `File.Replace` when the target exists (atomic on NTFS, and available since .NET Framework 2.0, unlike `File.Move(overwrite:)` which arrived in .NET Core 3.0 and is no use to net472/net48), a plain rename when it does not. Both call sites now go through it. **Neither the fix nor the bug changes any fragment, so no fingerprint moved and no proof went stale.** It needs the add-in deployed to take effect |
| **106** | **FOUR CLEARANCE FRAGMENTS COMPARE EVERY ELEMENT AGAINST EVERY ELEMENT WHILE FIVE OTHERS ASK REVIT.** `find-clashes`, `check-equipment-clearance`, `check-valve-accessibility`, `select-in-region` and `select-touching` all narrow through Revit's own spatial index (`BoundingBoxIntersectsFilter` and its siblings). **`check-minimum-clearance`, `check-vertical-clearance`, `check-insulation-clearance` and `find-nearest-elements` do not** - they nest two `foreach` loops over bounding boxes with no spatial pruning. The good pattern is already in the library; four fragments do not use it. Found 2026-09-16 | **OPEN, AND DELIBERATELY NOT MEASURED YET.** No claim is made here that it is slow, because nothing has timed it - and `check-minimum-clearance` **already reports `pairsChecked`**, so one run on a real model answers it without new tooling. **The fix is not a swap.** A spatial filter finds what intersects; clearance is about what does NOT touch yet, so the outline has to be grown by the threshold first. And it edits fragment files, so **those four fingerprints change and those four proofs go stale and need re-signing** - a real bill, and the reason this is recorded rather than done |

---

## 6. WHAT CANNOT BE RUN AT ALL — 100 fragments, by what they need

Not failures. Heron has no way to receive these inputs yet, so they have never executed a line.

| Waiting on | Fragments | Why not done |
|---|---|---|
| `ElementId` | 21 | Its constructor changed from `int` to `long` at Revit 2024, and nothing in the add-in carries a version `#if` |
| `Element` (one, not a list) | 20 | **Half done, 2026-09-09.** A TYPE resolves by name — `Basic Wall: Generic - 200mm` — and **eight contracts were narrowed off `Element` the same day** so the search is confined to the kind actually wanted. An INSTANCE still cannot be typed in and never will be: `Element.Name` on one returns its type's name, so searching instances matches every element of that type. The other 14 want *that one there* and need the selection, not text |
| `XYZ` | 14 | **Done, 2026-09-09 — [D-67](DECISIONS.md).** A point is three numbers in MILLIMETRES, `"5000, 3000, 2800"`; several are separated by semicolons. The unit was not chosen so much as read off what the library already said — `HeronUnits` converts nothing else, 59 caller values are named `...Mm`, and two fragments were already splitting points into `centreXMm`/`centreYMm` because they could not send one. Each ordinate is bounded at 100 km, and a direction needs no separate rule: scaling all three components alike does not move a vector, checked against all six that take one |
| `FamilySymbol` | 4 | **Done, 2026-09-09.** Resolved by name among family types, on the same mechanism as `Element` — no rule of its own was needed. `set-sheet-title-block` and `distribute-along-run` were the two waiting on it |
| `View3D` | 3 | A narrower view lookup |
| element/id collections | 9 | Same as the two above |
| `OverrideGraphicSettings` | 3 | A structured value, not a name |
| `IDictionary<string, double>` | 1 | **Found 2026-09-15 — row 98.** `check-minimum-clearance.rules` carries a different clearance per category, which is the normal MEP case rather than the advanced one. `FromRequest` refuses `IDictionary` by name and the need cannot be omitted, so the fragment has never run a line. A syntax decision of the same class as [D-72](DECISIONS.md)'s four |
| everything else | 26 | One rule each |

---

## How to arrange a case, learned by getting it wrong all day

Not a list of problems — the working method, written down because most of today's misses were the
arrangement rather than the fragment.

**This section is the evidence; the operative version is
[`.claude/skills/fragment-proving/SKILL.md`](../.claude/skills/fragment-proving/SKILL.md).** The two say
the same five things and are deliberately not the same document: here is what was observed, with the
values that were passed and what came back; there is what to do about it, next to the job file that
does it. Fix the skill when a rule turns out to be wrong, and add the observation here.

**PROVE ON A SMALL SELECTION.** The owner's instruction, 2026-09-09, after `set-mep-size` timed out on
307 ducts: *"a lot of items change, it will affect slow process… you can try with a small number of
ducts like 2 or 3."* Retried on the 22 ducts in `FloorPlan: M1` it sized all 22 immediately, and
`split-mep-run` passed in the same batch. **A heavy write on a big selection is not a stronger test, it
is a slower one** — and a timeout tells you nothing at all about the fragment.

`select-by-category-name --set inViewOnly="FloorPlan: M1"` gives 22 ducts. `FloorPlan: L3` gives 307.

**ASK FOR WHAT THE MODEL HAS.** Five times today the POSITIVE case was the empty one, because D-30 is
written about the negative and the positive quietly goes unarranged. `select-by-connection-status` was
asked for open ends in a model with none; `measure-mep-slope` for a minimum nothing falls below;
`report-coverage` for gaps at a radius that leaves none; `check-family-standards` for a pattern nothing
matches, which makes MORE findings not fewer.

**CHECK THE CATEGORY IS VISIBLE WHERE YOU SELECT.** Sheets do not appear in a floor plan; levels do not
appear in their own plan. Twice the setup found nothing and the answer read as a missing selection.

**MATCH THE INPUT TO THE MODEL'S OWN UNITS AND SHAPES.** `set-mep-size` was handed a width and height
for ducts that are round. Every duct here is dia 102, 152 or 203 — an imperial model.

**READ THE BINDING NOTE ON THE ANSWER.** `find-overlapping-lines` ran on a stale selection of ten
equipment items and answered anyway; only `elements from the selection (10)` on the reply gave it away.

---

## Add to this file, do not start another

A fragment that fails in front of a model belongs here the same day, with what was passed to it and
what came back **verbatim**. The value of the list is that every row was observed rather than expected —
the moment it fills with things somebody thought might be wrong, it stops being worth the sit-down.
