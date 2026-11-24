# FRAGMENT-ISSUES archive — section 5, rows 1 to 25

> **Finished rows, moved out of the live register.** These are rows from section 5 of
> [`FRAGMENT-ISSUES.md`](../FRAGMENT-ISSUES.md) — Heron's own defects found by proving — whose state said they were
> finished when they were moved. Each still has its line in the register: the same number, a
> one-line title, the opening of its state, and a link to its full text here.
>
> **Nothing below was rewritten** except its links, which were re-pointed one folder deeper and
> each checked to reach the same file it reached before. Where a row disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win** — a row records what was true on
> the day it was written.
>
> **Nothing new is written here.** A new defect goes in the register, and a row here that turns
> out not to be finished is reopened **there**, on its own line.
>
> Rows 1 to 25 of section 5 belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 10

*Moved from the register on 2026-11-17.*

**Defect.** **A proof draft claims the model was left unchanged, and nothing ever checked.** Every write phase's record ended *“run inside a transaction and ROLLED BACK, so the model was left exactly as it was”*, appended from the `writing` FLAG. On 2026-09-09 it was false: `edit-text-values` renamed 17 sheets, the rollback did not hold (§1c), and both the run record and the draft asserted the model was untouched. **The client was only repeating the add-in.** `RevitFragment.WithVerdict` said *“NOTHING WAS KEPT”* on the strength of `apply` alone — the request, not the result — and `SafeRollBack` was `void`, so the two ways a rollback silently does nothing (**a group whose status is not `Started`, so the call is skipped; a `RollBack()` that throws, so it is swallowed**) left no trace for anyone to report. See §1c: this is the first concrete mechanism for why three rollback failures have no explanation

**State.** **MOVED TO ISSUE [#898](https://github.com/Ajmalpshaik/Heron-AI/issues/898) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - DEPLOYED, AND THE HONEST HALF IS NOW SEEN WORKING; THE HALF THAT MATTERS STILL IS NOT.** On 2026-09-19 every `--write` run against session 20472 - eleven of them, across six fragments - came back *"Revit reported the transaction group ROLLED BACK afterwards, so the counts above are what it actually did rather than a guess"*. That is this row's fix live: the sentence is built from what the reply SAYS, not from `apply` being requested, and the words *rather than a guess* are the change. **IT IS NOT THE PROOF THIS ROW WANTS.** What was never seen is the branch that says **THE ROLLBACK DID NOT REPORT SUCCESS**, and that is the one the row exists for - a rollback that silently does nothing. Arranging one means making Revit fail to roll back on purpose, which nothing here knows how to do. **A run where the rollback WORKS cannot distinguish a fragment that reports honestly from one that always says the same thing.** The word OPEN still leads, deliberately. The word OPEN leads deliberately. A row that needs a RUN is open, and `tools/open-defects.py` reads the first word: writing `DEPLOYED` here dropped it out of the count for the few minutes between this edit and the next, which is the same two-marks-one-fact failure this file recorded against `E11`-`E15` the same day. All three releases were rebuilt and redeployed on the owner's PC and the deployed binaries were read back to confirm each got its own framework. **That clears this row's precondition and nothing more** - the fix can now be RUN, and D-30 wants it run. Any proof taken before 18:06 still carries the old unchecked sentence. Fixed in code 2026-09-09: `SafeRollBack` returns whether Revit reports the group `RolledBack` afterwards, `WithVerdict` says **THE ROLLBACK DID NOT REPORT SUCCESS** when it does not, a `rolledBack` flag rides on the reply, and the client writes only what the reply says — naming an add-in build that cannot answer rather than assuming. Compiles 2020–2027. **It needs a rebuild and a Revit restart to take effect, so proofs taken before that still carry the old unchecked sentence.** `RevitWrite.SafeRollBack` carries the same defect on its error paths (*“rolled back completely, so the model is as it was”*) and was deliberately NOT changed on the day the model was already damaged **2026-10-08, project review: the move path is honest too.** `RevitWrite.SafeRollBack` was still `void`, and a failed move still said *"rolled back completely"* unconditionally. It now returns whether Revit reports the group ROLLED BACK, and a failed move says it was rolled back only on that answer; otherwise it says Revit did not confirm it and to look before going on. Compiled on 2020 to 2027; `tests/test_central_skip.py` section 4a holds it, seen to FAIL first. The proof this row wants - a failure branch seen in Revit - is still owed, so the row stays open.

---

### Row 19

*Moved from the register on 2026-11-17.*

**Defect.** **A ROLLBACK REPORTED AS DONE DID NOT UNDO A RENAME. FOURTH OCCURRENCE, AND THE THIRD OF THIS EXACT SHAPE.** `rename-elements` was run through `validate` — **no `--apply`**, so the transaction group must roll back — against 11 FloorPlan views in `Snowdon-scratch_ajmal.al`. It reported `renamed 8` and Heron printed *"run inside a transaction, and Revit reported the transaction group **ROLLED BACK** afterwards"*. **The eight views are still renamed**: `L2` reads `HERONL2`, `L1 - Block 35` reads `HERONL1 - Block 35`, and six more. Read back with `find-views` minutes later, in a separate process.

**State.** **MOVED TO ISSUE [#898](https://github.com/Ajmalpshaik/Heron-AI/issues/898) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN.** Found 2026-09-13.

---

### Row 21

*Moved from the register on 2026-11-17.*

**Defect.** **`export-families` HAS NO PATH THAT CAN RUN IT. Both doors are shut, for opposite reasons.** Its header is explicit: *"OPENS NO TRANSACTION, AND MUST NOT. Each family is a SEPARATE document. A transaction in the project would not cover them."* But `validate --write` — the only route a job file has — runs every fragment inside a `TransactionGroup` plus a `Transaction`, and `Document.EditFamily` cannot be called with a transaction open in the project. So `fragment.cs:103` throws every time and `fragment.cs:104` reports it honestly: against 2 families matching "Duct" and again against 2 matching "Air Terminal", every one came back ***"Revit refused to open it for editing"*** with `saved 0`, `nameCollisions 0`. **THE OTHER DOOR REFUSES IT BEFORE REVIT IS TOUCHED**: `heron_bridge_client.py fragment export-families` answers *"export-families is declared risk: PUBLISH, and Heron does not run those yet - HeronPermissions puts Publish and Admin out of reach for Phase 0 and Phase 1."*

**State.** **MOVED TO ISSUE [#941](https://github.com/Ajmalpshaik/Heron-AI/issues/941) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN.** Found 2026-09-13 working the export cluster

---
