# HANDOVER — 2026-09-09 (the REVIEW track): five defects in the branch, three proofs, and the model is still dirty

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**Read "WHAT IS NOT FINISHED" below before starting anything.** Something is left in your Revit that a
fresh session cannot see and cannot fix.

### What this session was asked to do, and did

Review `fix/make-silence-illegal` before it went further. Five defects found, all fixed, all on main:

| | |
|---|---|
| **The stale-proof guard let the same day through** | `implementation_changed_after` compared `last > proof_date` — both DATES. `set-view-section-box` was proved and edited on the same day, so the guard answered *"the code did not move"* and `restamp` offered to re-bless a genuinely stale proof, refusing nothing. Now `>=`: a proof records a date, not a time, so same-day ordering is not in the data, and this file's own rule is that unknown counts as YES |
| **A refusal that still reported a count** | `set-view-section-box`'s inside-out-margin guard refused without clearing `enclosed`, so a run that changed nothing came back `enclosed 22, applied false` — the exact defect the branch existed to remove |
| **`measure-run-quantities` matched asymmetrically** | area by substring, length against a closed word list, so `surface area` read and `total length` was REFUSED. Both sides now match BY WORD — and by word matters: `diameter` contains "meter" and `volume` contains "m", so the obvious symmetric fix silently measures the wrong thing |
| **A hint narrower than its own search** | `edit-text-values` looks up the instance parameter then falls back to the TYPE, but its refusal listed instance parameters only — so it could print a list omitting the very name that would have worked |
| **Three vs four in the docs** | FRAGMENT-ISSUES §3i's *"Three more"* heading over a four-row table |

Then `set-view-section-box` went **PROVEN → DRAFT** on the owner's instruction, because fixing the §3f
doubt changed its implementation and a proof is evidence about the bytes it was taken against. The
`proof:` block was KEPT — `can_promote` refuses to carry a stale proof forward, so keeping it costs
nothing and re-proving starts from a written arrangement rather than a blank page.

### Three fragments proved, and the arrangement is the transferable part

`tools/jobs/refusal-paths.yaml`, against `Snowdon-scratch_ajmal.al` (9,628 elements, Revit 2024,
session 32940). All three PASS. Signed `by: Ajmal PS`; **all three are still `DRAFT`**, because
`accept` records a proof and refuses to promote — *"promoting it is a separate, deliberate act, and it
is yours"*.

**Every negative is a VALUE on the SAME selection.** A negative made by pointing at a different
category proves the old thing — that an empty set gives an empty answer — and never once enters the
code that was written:

```
measure-run-quantities   "total length" -> quantities 22    "diameter"       -> quantities 0, REFUSED
set-view-section-box     marginMm 500   -> enclosed 22      marginMm -999999 -> enclosed 0, REFUSED
edit-text-values         mode=prefix    -> changed 17       mode=transform   -> handed 0, REFUSED
```

**`set-view-section-box` finally has §3f's stronger negative.** Both legs report `view3D View3D` and
`viewRefused false` — a good 3D view either way — so the fragment reached the empty answer by its own
work rather than because Revit forbids a section box on a plan. That doubt is settled by evidence
rather than argument.

**Rule 2 nearly cost a leg.** `edit-text-values` needed a field already holding text, and duct
`Comments` probed `blank 22, values 0` — that positive would have come back `changed 0` and read as a
defect. Moved to the 17 sheets via `list-sheets` + `set-selection`, where `Sheet Name` probed
`values 17, blank 0`. **Probe before trusting a positive.**

### What it cost, and the finding that came out of it

Proving `edit-text-values` renamed all 17 sheets and **the rollback did not hold**. Third incident
(§1c) — and this one breaks the theory: **seventeen sheets and a rename.** Not large, no cascade, no
dialog, nothing created.

> **The element count could not see it.** 9,628 before and after. Both earlier incidents were caught by
> counting; a rename creates nothing. After a write proof, re-read the thing that was written.

Reading `RevitFragment.cs` then gave the first concrete mechanism for why three incidents have no
explanation. `SafeRollBack` was `void`:

```csharp
try { if (group.GetStatus() == TransactionStatus.Started) group.RollBack(); }
catch { }
```

The inner transaction is COMMITTED first; the undo depends entirely on this group rollback landing
afterwards. Two silent exits — a status that is not `Started` **skips** the call, a throw is
**swallowed** — and `void` meant neither reached a caller. `WithVerdict` then said *"NOTHING WAS KEPT"*
because `apply` was false, which is the request and not the result. **A failed rollback and a clean one
came back byte-identical.**

Fixed: `SafeRollBack` returns Revit's status after the attempt, `WithVerdict` says **THE ROLLBACK DID
NOT REPORT SUCCESS** when it does not, the reply carries `rolledBack`, and the client writes only what
the reply says. §5 row 10 rewritten. `RevitWrite.SafeRollBack` carries the same defect on its error
paths and was deliberately NOT changed on the day the model got damaged — named in row 10 so it reads
as a decision.

### WHAT IS NOT FINISHED — read this before starting anything

**1. THE MODEL IS STILL DIRTY. DO NOT SAVE IT.** `Snowdon-scratch_ajmal.al` in session 32940 has all
17 sheets renamed `HERON Cover Sheet`, `HERON Learn about this project`, and so on. Sheet NUMBERS are
intact. The disk file was not written. **Close without saving** and the names come back.

**2. Revit has had a dialog open for hours.** Every request since has answered *"Revit is busy… a
dialog may be open"*. Nothing model-side can run until somebody answers it at the keyboard. If it is
the save prompt, the answer is **Don't Save**.

**3. The re-prove NEVER RAN.** `set-view-section-box` and `edit-text-values` carry proofs whose text
says *"run inside a transaction and ROLLED BACK, so the model was left exactly as it was"*. For
`edit-text-values` that is FALSE and was proved false the same hour. The client no longer writes that
sentence, but **these two proof blocks were signed before the fix** and still hold it.
`measure-run-quantities` is clean — it is READ-only and never had one.

**4. The add-in fix is NOT DEPLOYED.** It compiles on 2020–2027. It needs a rebuild and a Revit
restart, which is the same restart that clears the dialog. **Do that first, then re-prove the two** —
their new proofs will carry a checked sentence instead of a disclaimer.

**5. `write.enabled` is still TRUE.** Correct for the proving run, wrong to leave. `revit_health` says
so on every call.

**6. [PR #62](https://github.com/Ajmalpshaik/Heron-AI/pull/62) is OPEN and unmerged.** Mergeable,
clean, no CI configured. This session was blocked from merging it by a permission rule, so it is a
click that is owed.

### Two things that cost this session time, so they do not cost the next one

**One `HERON_CLIENT_ID` per person.** The lease names a CHAT, not a person. Calling an MCP `revit_*`
tool claims it for the MCP process; a command line with its own id is then refused as *"in use by
another chat"* — by yourself. It reads exactly like a fragment failing. Pick one id and drive
everything from it.

**A deleted branch nearly took a commit with it.** `fix/make-silence-illegal` was merged as PR #61 and
deleted at both ends while this session still had an unpushed commit on it. The commit survived only
as an unreferenced object — nothing pointed at it, and a `git gc` would have taken it. It was rescued
onto `fix/rollback-reports-its-outcome`. **Check for unpushed work before deleting a branch in a shared
tree.**

### To carry this on

```bash
python tools/batch-prove.py tools/jobs/refusal-paths.yaml --client-id heron-review-proof
python brain/heron_validate.py review set-view-section-box
```

The job file is written and dry-runs clean. Drop the `measure-run-quantities` job — its proof is
already signed and honest.
