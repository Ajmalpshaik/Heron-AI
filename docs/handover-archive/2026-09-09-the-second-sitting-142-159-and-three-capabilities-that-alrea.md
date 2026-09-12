# HANDOVER — 2026-09-09, the second sitting: 142 → 159, and three capabilities that already existed

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**READ THIS FIRST IF THE SCRATCH MODEL IS STILL OPEN.** `Snowdon-scratch_ajmal.al` has been written to
all day and **at least one rollback did not hold** (see below). **Close it without saving.** Nothing in
this entry is worth a damaged model, and every proof recorded here was taken on a rolled-back write, so
none of them needs the file kept.

**Revit was left with a dialog open or a command running** — every request answered *"Revit is busy"*
for the last half hour of the session, and before that a fifth chat held the lease. Both are worth
clearing before the next session starts.

### What moved

| | |
|---|---|
| Proved | **142 → 159.** Eighteen signed, one (`set-view-section-box`) correctly demoted by a parallel session when its code changed under its proof |
| Job files | **14** in `tools/jobs/`, each carrying the reasoning for its arrangement in comments |
| Branches | **`main` only.** Everything merged: PRs #48, #52, #55, #56, #58, #59, #61. Nothing parked |
| Gates | check-docs, check-metadata, check-structure, bridge-roundtrip — all green on `main` |

### The thing worth carrying forward, said once

**Three separate capabilities existed, were already `PROVEN`, and were invisible — all in one day.**

1. **The `list-*` fragments as a setup chain.** `list-sheets` → `set-selection` arranges all 17 sheets
   in one step. Five fragments proved on that route within an hour of finding it. `list-levels`,
   `list-grids`, `list-revisions`, `list-linked-models` do the same for their kinds.
2. **`views (IList<View>)` resolves from a single name.** D-54 fills a list from one value.
3. **Fifteen selectors narrower than a category** — `select-by-family`, `select-by-connection-status`,
   `select-types`, `select-by-parameter-value` and twelve more. **Every job file written that day used
   `select-by-category-name` and nothing else**, and four fragments refused because they were handed a
   whole category when they needed one family or exactly two runs.

Each time the fragment was fine, the library was ahead of the job files, and the cost was hours. **This
is the strongest argument for §3h.4 — generating the job file from the contracts — and much stronger
than the mistyped field names it was first written about.** A generator reading `contract.needs` would
have offered `select-by-family` to a fragment that renames a family.

### Two defects found in the tooling, both about evidence

**`batch-prove` reports verdicts on runs that never happened.** Six jobs came back `POSITIVE EMPTY`
against records from an *earlier Revit session* describing a *different arrangement*. Heron behaved
correctly throughout — it refused the lease and declined to write a record about *"a model that will
not name itself"* — and the runner judged the file already on disk. A date check does not catch it;
both records say `2026-09-09`. The session id in the `model` line does, and `validate` already stamps
it. **OPEN.**

**A tracking harness read the wrong fragment's block.** `prove` prints one provides block per chained
fragment, and `elements` is declared by the selector as well as by the fragment under test. Reading the
selector's copy produced five rows that tracked the input perfectly — because they *were* the input.
`find-untagged-elements` was promoted on that evidence and had to be reverted and re-proved. Fixed with
two guards, and the second is the one that matters: **refuse to write rows unless one agrees with what
`validate` recorded for that fragment alone.**

> Rows that match the input exactly are the thing to distrust. A real describer's answer *differs* from
> what it was given — 22 ducts, 14 untagged.

### ONE PERSON, ONE `HERON_CLIENT_ID`

The lease identifies a **chat**, not a person. Four ids were in use for one afternoon — one by hand,
two by scripts, one the runner picks itself — and to Heron that is four chats competing for one Revit.
**Every refusal reads exactly like a fragment failing.** It cost two batches before it was understood,
and it explains an eleven-job wipe-out earlier the same day that was written up as an arrangement
fault.

### The rollback is not reliable, and size is not the boundary

§1c said small writes roll back correctly. **That is now known to be false.** A parallel session renamed
17 sheets inside a transaction with no `apply`; the rollback did not hold and `list-sheets` afterwards
still read `M000 HERON Cover Sheet`. Seventeen sheets, one rename, no cascade, no dialog.

**Every write proof signed today carries the sentence** *"run inside a transaction and ROLLED BACK, so
the model was left exactly as it was"* — and that sentence comes from the `writing` flag in the bridge
client, **never from anything Revit returns**. It is not evidence. §5 row 10 is the standing record.

### Where the proving goes next

1. **Name what already exists.** Nothing to build — the three capabilities above need to reach whoever
   writes the next job file. Cheapest item on the list and it outranks the rest.
2. **`LIST_*` fragments**, §3d. Five fragments could not be arranged for want of a name nothing can
   supply: a workset **id** (`list-worksets` gives names only — one field closes it), a view carrying
   no template, a legend name, a route to a section mark, a material name.
3. **A resolver for named Revit objects** — `FamilySymbol`, `Material`. Half the never-run MODIFY
   fragments wait on it.
4. **A way to bind two sets of elements**, which no job file can express today.

**Making silence illegal (§3h.1) still comes first.** Everything above makes proving faster; that one
makes Heron honest — and twelve fragments still answer `0` where `duplicate-sheets`, `edit-revision`
and `remove-parameter-value` say plainly what went wrong. The best line written by any fragment all
day was a refusal:

> *"0 cleared, 22 already empty. An empty field and a zero are different."*

---
