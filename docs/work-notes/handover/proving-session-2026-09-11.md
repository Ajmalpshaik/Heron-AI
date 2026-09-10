# Proving session handover — 2026-09-10 into 2026-09-11

> **Type:** Operational work note — the handover for one proving session. **Not specification.**
> Where a sentence here disagrees with the [Constitution](../../../HERON_CONSTITUTION.md), the
> [Golden Rules](../../14-golden-rules.md) or [DECISIONS.md](../../DECISIONS.md), **those win.**
> **Status:** **Session closed clean.** `main` at `14d025a`, working tree clean, no open PRs, no
> stray branches. **197 PROVEN / 163 DRAFT**, up from 175 at the start.
> **This note is scaffolding.** Everything durable is already in
> [`FRAGMENT-ISSUES.md`](../../FRAGMENT-ISSUES.md) and in the job files under
> [`tools/jobs/`](../../../tools/jobs/). **Delete this once the next session has read it.**
> **Asked for by:** Ajmal PS, 2026-09-11 — *"is that for this session handover note you prepared?"*

---

## 1. Where the library stands

**Derive it, do not read it here.** This number moves hourly:

```bash
grep -rh "^heron-status:" brain/fragments --include="*.yaml" | sort | uniq -c
```

At close: **197 `PROVEN`, 163 `DRAFT`, 360 total.** Twenty-two promoted in this session, each one
signed by Ajmal PS after reading the draft. `export-views-to-fbx` was promoted and then **put back to
DRAFT** — see §5.

## 2. What was proved, and the one move that did most of the work

| Fragment | Model | Why it had been blocked |
|---|---|---|
| `find-dead-ends` | Project1 | §3b said **STOP RE-RUNNING THIS ONE** — correctly, for Snowdon |
| `set-mep-slope` | Project1 | Snowdon refused all 22: `bothEndsConnected 20` + `risers 2` |
| `flip-elements` | Project1 | Snowdon has no flippable instance — its doors are in a link |
| `dimension-wall-openings` | Project1 | same — no host walls or doors in Snowdon |
| `check-flow-direction` | Snowdon | Project1's ducts carry no system, so `jointsChecked 0` |
| `group-and-count`, `sum-by-group` | Project1 | the `keep-chain` pair — see §6 |
| plus 15 more | both | |

> **A blocked row names a MODEL, not a fragment.** Four were unblocked by reading the triage row that
> says what a fragment *needs*, then opening the model that has it. That is the single most productive
> habit this session found.

The second was **making the input instead of waiting for it**: `import-parameter-values` reads a CSV,
so two CSVs were written — same shape, same columns, differing only in the first column. No model has
to hold anything. That works for any fragment whose input is a file.

## 3. The defects are the better return — eleven of them

Every one is written up in [`FRAGMENT-ISSUES.md`](../../FRAGMENT-ISSUES.md) with its evidence. Ranked
by what they cost if left:

| # | Defect | Why it matters |
|---|---|---|
| 1 | **`validate` does not apply the risk gate** | `cmd_fragment` and `cmd_prove` call `risk_refusal`; the **proving path does not**. Eight ADMIN/PUBLISH fragments ran against live models in one session |
| 2 | **A proof taken with `--in` names the wrong model** | `model:` is stamped from the **active** document, not the pin. **Seven promoted proofs name a model they were not taken against** |
| 3 | **`can_promote` will promote an unproven fragment** | It reads the fragment's *current* status, not the target, so it can never refuse for lack of a proof |
| 4 | **Ten fragments count the CALL, not the CHANGE** | `move`, `copy`, `rotate`, `hide`, `zoom`, `set-category-visibility`, `set-crop-box-settings`, `place-room-at-point`, `create-workset-3d-views`, `disallow-join` — plus four more from sweep 2 and `stack-tags`. **`copy-elements` is worst: a zero offset leaves five real copies stacked on the originals** |
| 5 | **`create-hvac-zone` ignores the level** | Zoned 17 spaces onto a level none of them is on, and pulled each out of its existing zone |
| 6 | **`switch-active-project` asks the wrong `UIDocument`** | Has never actually switched; the only path that "worked" was *already-there* |
| 7 | **`set-mep-justification` accepts only a zero offset** | Confirmed on both models — the fragment, not the content |
| 8 | **`select-in-region` reports feet as millimetres** | A 20,000 mm box is reported as *"66 mm"*, and it finds nothing at any size, on both models |
| 9 | **`batch-prove` overwrites a DRAFT proof silently** | `ALREADY` only fires for `PROVEN`. A DRAFT carrying a signed proof is re-run and replaced with no warning |
| 10 | A run bound the **previous** selection and called it a pass | Seen once, **not reproduced** in five attempts. NEEDS_REVIEW, no mechanism claimed |
| 11 | A value that **re-labels** is not a value that turns the answer off | Both `deadEnds` and `stubs` are results; moving findings between them empties neither |

## 4. What is left, and why a seventh sweep will not help

Six sweeps asked **77 fragments**. Recomputed from the contracts at close:

| Count | Blocked by |
|---|---|
| 76 | Content, or a negative that cannot be arranged |
| 65 | A request value that cannot be supplied — `ElementId` + `Element` are **42 of the 82** |
| 12 | The permission tier — **nothing technical** |
| 6 | Result is only a `bool`, a bare `ElementId` or a `string` |
| 4 | Two element sets, or no declared result |

**Three routes out, none of which is another job file:**

1. **Value passing.** 65 fragments. Naming one particular element is the most-wanted thing a caller
   still cannot say.
2. **A richer model.** Four sheet fragments and a dozen others need a drawing set, CAD imports,
   drafted lines, pipes or design options. **Neither open model has any of it** —
   `Snowdon-scratch_ajmal.al` has **zero sheets**, and so does Project1. The delivered sample
   `Snowdon Towers Sample HVAC.rvt` is the obvious candidate and **was never opened**.
3. **The gate on `validate`** — releases 12 immediately.

## 5. What is owed, and the one decision already taken

**Taken 2026-09-11:** `export-views-to-fbx` is back to `DRAFT`. It is `risk: PUBLISH` and was proved
through the ungated path. **Its proof block was kept** — the evidence is real and signed; only the
status claim was withdrawn.

**Still open, and worth a `D-` number:** whether the risk gate belongs on `validate` at all, or
whether proving is a different act from doing and the exemption should be **written down** rather than
being the absence of a line.

**Still owed:** seven promoted proofs name the wrong model (defect 2). Whether they are re-taken,
corrected, or accepted as-is has not been decided.

## 6. Traps that cost time here — do not pay for them twice

- **The view is not called what the level is called.** `Project1` is an MEP template: levels
  `Level 1` / `Level 2`, plan views `1 - Mech`, `2 - Mech`, `1 - Plumbing`, `2 - Plumbing`. An earlier
  session probed for `Level 1`, got nothing, and concluded the model had no floor plan. It has four.
- **A proof names its model, and two models can share a name.** `Snowdon Towers Sample HVAC.rvt` is
  not `Snowdon-scratch_ajmal.al`. Reading "Snowdon" in a proof and assuming it is the one you have
  open is how §3b-iii got written and then corrected.
- **Read `FromRequest` before claiming a type cannot be supplied.** A first pass at the blocked count
  said 111 by assuming only text and numbers resolve. It is 65 — `View`, `Level`, `Category`,
  `FamilySymbol`, `WallType`, `Phase`, `IList<XYZ>` and more all resolve **by name**.
- **`slopeRatio` is the X in "1 in X".** A bigger number is a *shallower* fall. Asking for 0.02
  meaning 2% produced *"would move an end 682500 mm"*, and 0 produced **infinity** — both correct, and
  both nearly filed as an arithmetic defect.
- **One `HERON_CLIENT_ID` per person**, and **switch Revit to the model you are pinning** before
  running, or the stamp lies.
- **`--keep-chain` is opt-in per job.** The chain outranks the selection, so keeping it everywhere
  would make `set-selection` decorative in every arrangement already written.

## 7. What this note does not replace

[`FRAGMENT-ISSUES.md`](../../FRAGMENT-ISSUES.md) is the durable record and it has all the evidence.
[`docs/HANDOVER.md`](../../HANDOVER.md) is the entry point and its counts are current. The seventeen
job files under [`tools/jobs/`](../../../tools/jobs/) each carry the reasoning for their own
arrangement — **read the one next to a fragment before re-arranging it**, because most of them say in
their comments why an obvious-looking value is wrong.

**This note is a work note. It ends in deletion.** Once the next session has read it, the useful part
is already elsewhere and this file should go.
