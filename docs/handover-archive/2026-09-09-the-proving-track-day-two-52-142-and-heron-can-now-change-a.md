# HANDOVER — 2026-09-09 (the PROVING track, day two): 52 → 142, and Heron can now change a model

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**Ninety fragments proved in one day, against `Snowdon Towers Sample HVAC` in Revit 2024 on the owner's
PC.** Two things were built to make that possible and both are bigger than the day's count: fragments
can now be handed **the caller's half of their inputs**, and fragments that CHANGE a model can now
**run at all**. Before this morning, **287 of the 360** could not be given so much as a category name,
and the **184** carrying `risk: MODIFY` could not execute a single line.

**If you are here to prove more, read [the method](#the-method-that-made-it-fast--this-is-the-part-that-transfers)
and [`docs/FRAGMENT-ISSUES.md`](../FRAGMENT-ISSUES.md) first.** The issues file is the sit-down list — 107
rows of what failed, what is missing, what should be split and what should be edited. It is not a bug
list; it is the work queue.

### What was built

| | What it does | Why it mattered today |
|---|---|---|
| **Caller values** (D-54) | `--set categoryName=Ducts` crosses the bridge **as text** and is resolved into a `View`, `Level`, `Category` or `BuiltInCategory` **inside Revit**, where the document is | **287 fragments** declare a need the selection cannot fill — 675 such needs in all. It refuses `XYZ` and `ElementId` **by name**, with a reason, rather than half-resolving them |
| **The write engine** (D-55) | `run_fragment_write` — a second operation, `MODIFY` in the registry, wrapping the run in a `TransactionGroup` that is **assimilated only on `apply=true`** and rolled back otherwise | **184 fragments carry `risk: MODIFY`** and not one had a path to execute. A preview is now the run itself, undone — nothing is simulated, so nothing can lie |
| **Risk by name** (Golden Rule 19) | The caller says which fragment; **the registry says what it costs.** `--write` on a `READ` fragment is refused | A caller that could declare its own risk could declare `MODIFY` work to be `READ` |
| **`tools/batch-prove.py`** | Runs 10–20 fragments per pass and hands back only the failures | One-at-a-time was the real bottleneck, and the failures only make sense **read together** |
| **`tests/test_caller_values.py`** | 21 cases over the resolver, **no Revit needed** | The resolver is the piece most likely to rot silently |

### The method that made it fast — this is the part that transfers

Four things, in the order they were learned. All four are written up properly in
[`docs/FRAGMENT-ISSUES.md`](../FRAGMENT-ISSUES.md), under *How to arrange a case*.

1. **Never select by hand. Chain the fragments.** The owner asked *"why do I need to select — you have
   fragments for selecting, use that"*, and he was right. `select-by-category-name` → `set-selection`
   as a `--setup` chain arranges every case, repeatably, and it is faster than clicking.
2. **Small selections.** Also the owner's: *"try with a small number of ducts, like 2 or 3."*
   `FloorPlan: M1` has **22 ducts**; `FloorPlan: L3` has **307**. Two proofs that had been failing on
   time alone passed immediately on the small view. A slow case does not prove more than a fast one.
3. **Get the positive and the negative the right way round** — this was got **WRONG FIVE TIMES** in one
   day, which is why it is now rule #2 in the arranging guide. The POSITIVE is the case where the
   fragment has something to find. The NEGATIVE is where it must come back empty. Writing them the
   other way produces a run that passes and means nothing.
4. **Read the positive, not just the negative.** `heron_validate` judges whether the negative came back
   empty. A fragment that does **nothing at all** satisfies that trivially. Every silent failure found
   today was caught by reading the positive by hand, so `batch-prove.py` now checks **both halves**.

### The two model wipes — read this before you run a write

**The model was destroyed twice today, and both times it was the choice of test data, not the code.**
`transfer-views` over 60 elements, and `delete-elements` over the 5,636 elements under `Levels`. In
both cases **the rollback did not fully undo the write**, and the model was recovered only by closing
it **without saving**.

> **The boundary is not known.** Small writes roll back cleanly — dozens were proved today. Something
> between 22 elements and 5,636 does not. Nobody has found where, and **guessing at it is worse than
> saying it is unknown**, which is why §1c of the issues file says so in those words and
> `delete-elements` is marked BLOCKED rather than untested.

Until it is understood: **write against small selections, on a scrap model, and never save.**

### Ten defects, and where they came from

| Found in | What it was |
|---|---|
| The chain | A fragment's own output was overwriting the value handed to it, **matched by NAME**. Fixed by identity — `ReferenceEquals` against what was handed in. It hit 4 filter fragments, not "every filter fragment" as first claimed |
| `validate` | Was sending **writes down the read path**. A lost patch. It first read as *"intermittent worksharing behaviour"* and was written up as such, and only `prove` — which has no write path — failing identically exposed it |
| The batch runner | Passed a fragment whose only real result was **0 in both legs**, because an unreadable value counted as work. *"I cannot read this"* and *"this is a result"* must not collapse |
| The batch runner | Counted **accounting fields** as results — `check-flow-direction`, `remove-parameter-value`, `set-mep-slope`. `role: result` vs `role: accounting` exists for exactly this, and 40 fragments still do not declare it |
| The batch runner | Re-proved **15 of 16 already-`PROVEN`** fragments in its first run, because the names came off a capability list and nothing filtered by status |
| `Describe` | Renders a **valid and an invalid `ElementId` identically**. Two proofs are blocked on this and cannot be arranged around it |
| Schedules | The `Schedule Graphics` door was found — a schedule can be selected without clicking it — but **eight schedule fragments never got the fix** and fail silently |
| `--setup` | **Cannot run a step that writes.** So any case needing a written setup cannot be arranged at all |
| The deploy script | Deployed **.NET 10 binaries into Revit 2024**, which refuses them with *"Revit cannot run the external application"*. `check-compile.py` builds 2020–2027 into one folder and the newest wins. Now guarded in `deploy-addin.ps1` rather than left to memory |
| The CLI | Every invocation was a **new chat** taking a five-minute lease, so the second call was always refused. `HERON_CLIENT_ID` fixes it, set once |

**The activity banner was NOT one of them.** A whole morning was lost to `switch-active-project`
appearing to hang; a fix was guessed at, did not help, and **was reverted**. Another session then
proved the cause was the banner's own threading. *The fragment is innocent* — and reverting the guess
is the reason that could be established at all.

### What tomorrow starts with

1. **The sit-down list.** [`docs/FRAGMENT-ISSUES.md`](../FRAGMENT-ISSUES.md), and specifically §3h — the
   four improvements, already ranked. Do **§3h.1 first**: make silence illegal. A fragment that returns
   nothing and reports success is the failure mode that has cost the most time, twice over.
2. **The eight schedule fragments** that never got the `ScheduleSheetInstance` fix.
3. **The rollback boundary.** Find where it breaks, on a scrap model, deliberately — it is the only
   thing standing between the write engine and a real project.
4. **Then keep proving.** 219 `DRAFT` remain, and roughly 100 of them cannot run for reasons §6 of the
   issues file lists by cause.

---
