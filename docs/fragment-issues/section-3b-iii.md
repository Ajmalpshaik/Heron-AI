# Fragment issues — section 3b-iii

> One section of [the register](../FRAGMENT-ISSUES.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## 3b-iii. THE SNOWDON ROUND THAT WAS WRITTEN AND NEVER RUN — 2026-09-10

`rerun-stale-passes.yaml` and `read-recoveries.yaml` were both written earlier the same night and
neither was ever run — housekeeping interrupted, and all eight fragments involved were still `DRAFT`
hours later. Running them cost one command each.

**The four stale re-runs all passed.** Their earlier records were same-day and therefore stale by this
repository's own rule ([`heron_validate.implementation_changed_after`](../../brain/heron_validate.py)), so
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
[`read-recoveries.yaml`](../../tools/jobs/read-recoveries.yaml) predicted in its own comment. Its negative
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
| `group-elements` | `grouped 44`, `groupId 1`, `refused false` | A negative. It groups whatever it is handed — the shape [`value-driven-negatives.yaml`](../../tools/jobs/value-driven-negatives.yaml) already names, and no value it takes can switch that off |

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
