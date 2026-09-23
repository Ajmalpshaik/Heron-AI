# Fragment issues — section 3g-iii

> One section of [the register](../FRAGMENT-ISSUES.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## 3g-iii. A THIRTY-FRAGMENT SWEEP, AND WHAT IT SEGREGATED — 2026-09-10

Every other job file here was written after probing one fragment at a time and knowing the answer
before the run. [`sweep-project1.yaml`](../../tools/jobs/sweep-project1.yaml) is the opposite on purpose:
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
[`sweep-project1-pass2.yaml`](../../tools/jobs/sweep-project1-pass2.yaml) re-ran six of the POSITIVE
EMPTY with better values — the real working 3D view `{3D - ajmal.al}` instead of `{3D}`, walls instead
of ducts for `snap-to-grid`, `System Type` instead of the empty `Comments` for `copy-parameter-value`,
an underlay looking DOWN a level instead of up. **All six came back POSITIVE EMPTY again.** They are
blocked by this model, not by the job file, and that is now settled rather than assumed.

### Sweep 2 — twenty-one more, 2026-09-11

[`sweep-project1-2.yaml`](../../tools/jobs/sweep-project1-2.yaml). **3 PASS**, 10 POSITIVE EMPTY,
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

[`sweep-project1-3.yaml`](../../tools/jobs/sweep-project1-3.yaml). **2 PASS**, 6 POSITIVE EMPTY.

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

[`sweep-snowdon.yaml`](../../tools/jobs/sweep-snowdon.yaml). **Nothing passed**, and it was still the most
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
