# FRAGMENT-ISSUES archive — section 5, rows 51 to 75

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
> Rows 51 to 75 of section 5 belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 51

*Moved from the register on 2026-09-23.*

**Defect.** **THE D-71 UNITS PASS CONVERTED ONE LENGTH PER FRAGMENT AND WALKED PAST THE SECOND.** Both fragments below carry the MILLIMETRES IN, FEET INSIDE block, both convert the length named first, and both leave the other one raw. Found 2026-09-13 while arranging `check-surface-fit`, by reading the file rather than by a run - the model has nothing beneath its ducts, so no arrangement on it would ever have reached the line. **`check-surface-fit.evenness`** is compared against `spread`, the difference between two `ReferenceIntersector` distances - Revit's units, feet. An `evenness` of 5 meant **five FEET**, a flatness tolerance 304.8x looser than the 5 mm typed, and a surface a metre and a half out of true reporting as flush. The giveaway was in the same file all along: the message it prints does `spread * 304.8` to turn that very number into millimetres. **`check-valve-accessibility.envelope`** is added to and subtracted from `centre`, an XYZ in feet, to build the operating zone - so an unconverted 500 put a **152-metre box** around every valve and would have condemned every one of them as crowded. Its neighbour `maxReachHeight` was converted on the line above. **THE SWEEP MATTERED MORE THAN EITHER FIX, AND SO DID GETTING IT WRONG FIRST.** Every fragment carrying the block was checked for a `double` request input with no conversion. A first pass matched only self-assignment (`x = x / MillimetresPerFoot`) and accused **11 fragments, 7 of them PROVEN** - `set-mep-justification` among them, which converts into a new variable two lines later and is perfectly correct. Checking whether the NAME appears on any conversion line at all left 3. Of those, `distribute-along-run` is also a false alarm: it converts Revit's feet up to millimetres, does all its arithmetic there, and converts back once at the point of use - deliberate and right. **Two real, nine accused wrongly by the first pattern.** A units sweep that reports a PROVEN fragment as broken is not a cheap mistake, and the fix was to read the code rather than trust the regex.

**State.** Fixed - both conversions added, `check-fragments-compile` exits 0, and no PROVEN proof went stale. **Neither fix is verified against a model that exercises it** - `check-surface-fit` needs something beneath the services and `check-valve-accessibility` needs valves; this model has neither

---

### Row 54

*Moved from the register on 2026-09-23.*

**Defect.** **THE RISK GATE GUARDED EVERY PATH EXCEPT THE ONE THAT RUNS FRAGMENTS ALL DAY, AND ITS OWN COMMENT NAMED THE ACCIDENT IT THEN FAILED TO STOP.** `RUNNABLE_RISKS` excludes `PUBLISH` and `ADMIN` because HeronPermissions puts them out of reach in Phase 0 and Phase 1. `risk_refusal` has enforced that in `cmd_fragment` and `cmd_prove` since 2026-09-08. **`cmd_validate` never called it** - and validate is the proving command, the one path that runs fragments continuously. The comment beside `RUNNABLE_RISKS` says the risk it guards against is *"somebody proving fragments alphabetically and reaching `export-*`"*. **FOUND BY WALKING INTO IT, NOT BY READING.** `export-parameters-to-csv` (PUBLISH) was proved through `validate` earlier the same day and wrote four real CSV files to disk without a word from the gate; the hole only surfaced an hour later when `export-families` was tried through `fragment` instead and was refused on the spot. Two commands, the same fragment risk, opposite answers. `export-sheets-to-pdf`, `export-view-image`, `export-views-to-fbx`, `upgrade-family-files` (all PUBLISH) and `create-workset` (ADMIN) are already PROVEN, so this had been open long enough to matter. **THE EVIDENCE FROM THOSE RUNS STANDS AND THE PROOFS ARE NOT WITHDRAWN.** The fragments did what they said and the files on disk agree ([row 52](../FRAGMENT-ISSUES.md#)). What was missing was not correctness - it was anybody DECIDING to run a Publish fragment. **REFUSED BY DEFAULT, EXCEPTION MADE VISIBLE.** `--allow-publish` is typed per run and lands in the shell history. Blocking these outright was the wrong fix: proving them is real work that has to happen, and a gate that makes the necessary thing impossible gets worked around rather than obeyed. Proved three ways - a PUBLISH fragment without the flag is refused and names it; a READ fragment without the flag still runs; the same PUBLISH fragment with the flag runs.

**State.** Fixed - `cmd_validate` in `mcp/client/heron_bridge_client.py`. `test_bridge_roundtrip`, `test_caller_values`, `test_validate_agent` and `test_batch_prove` pass

---

### Row 71

*Moved from the register on 2026-09-23.*

**Defect.** **A CALLER VALUE FOR A NEED THAT DOES NOT EXIST IS SILENTLY DROPPED, AND IT COST A WRONG NEGATIVE BEFORE IT WAS NOTICED.** Proving `trace-connectivity` I passed `--set maxSteps=200` against `--negative-set maxSteps=0` and both legs came back `reached 25`. The fragment looked broken. **It declares no `maxSteps` at all** - only `start`, `elements` and `tolerance` - so the value was accepted, ignored, and never mentioned. Confirmed directly afterwards: `list-levels --set totallyMadeUpValue=42` runs clean. **THIS IS THE MISTYPED-INPUT FAMILY THE LIBRARY ALREADY KNOWS ABOUT** - `generate-jobs.py` exists because *six input names were mistyped on 2026-09-09*, `widthMm` for `width` among them - and that tool only protects a GENERATED job file. A hand-run `validate` or `fragment` takes anything. A mistyped name and an invented one are the same event here, and both read as a fragment that ignores its input. **TOLERANCE WAS NOT THE DRIVER EITHER, AND THAT PART IS THE FRAGMENT BEING RIGHT:** at 25 mm and at 0.0001 mm it still reached 25, because it walks CONNECTORS and `joinedByGeometry` was 0 - `tolerance` only gates the geometry route, which found nothing here. So `trace-connectivity` has no empty case from a connected start, and its negative needs an isolated element or D-53 tracking across different starts.

**State.** **FIXED 2026-09-19, in the client, and it needed no Revit.** This cell asked for *"refusing an undeclared caller value, **or naming it**"*, and **naming** is what was built - the half that cannot break a caller relying on today's behaviour, and the half that was actually missing: the confusing part was never the drop, it was the silence. `undeclared_values(values, needs)` and `report_undeclared()` in `mcp/client/heron_bridge_client.py` compare what was typed against `contract.needs`, and `cmd_fragment`, `cmd_prove` and `cmd_validate` call them **before `discover()`** - so the answer arrives while Revit is still untouched. `cmd_validate` checks BOTH legs, because this row's own case was a value typed into the negative. **THE CHECK BELONGS HERE AND NOT IN THE ADD-IN**: `needs_for` has already read the contract off disk before anything is sent, where the executor would have to answer after a round trip. On this row's two verbatim cases it now prints *"trace-connectivity IGNORED 'maxSteps' - not a value this fragment declares, so it will be dropped / it takes: start (Element), tolerance (double)"*, and for `list-levels --set totallyMadeUpValue=42`, *"it takes: no caller values at all"*. Only `source: request` needs are offered, because a need filled by an earlier fragment is not an answer to *what should I have written*. **Eight checks in `tests/test_caller_values.py`**, whose docstring already said these functions have *"no excuse to be unproved"* - including the `width` / `widthMm` pair, the shape that cost six names on 2026-09-09. **WHAT IT DOES NOT DO:** it never refuses, so a run still proceeds and the value is still dropped by the executor exactly as before - the behaviour is unchanged and only the silence is gone. And it cannot see a value typed for a SETUP step, which reaches the chain by a different path

---

### Row 72

*Moved from the register on 2026-09-23.*

**Defect.** **EIGHT MORE JOBS, ZERO PASSES, AND EVERY ONE SAID "THIS MODEL DOES NOT HAVE IT" - SO THE REMAINING 81 WERE CLASSIFIED INSTEAD OF GUESSED AT.** Round 2 on `Snowdon-scratch`: `report-areas` -> *"This model has no Area elements at all"*; `place-flow-arrows` -> *"No symbol 'M_Air Flow Arrow : Standard' is loaded - this fragment places an arrow family that is already in the project and loads nothing"*; `find-overlapping-lines` -> *"0 overlapping pair(s) across 22 line(s)"*; and no openings, no design options, no assemblies, no pipes. **Snowdon is four times the size of `test projject` and lacks the same things.** Two batches, 15 jobs, 2 passes - and the passes were both MEP work (`tag-elements-in-view` tagged 14, `filter-elements-by-type` found 22 against 0). **SO THE WHOLE REMAINDER WAS DERIVED RATHER THAN ESTIMATED**, from the contracts and from what a PROVEN fragment can leave in the chain. All 81 accounted for, none twice: **A - 11 need an input Heron cannot receive** (4 of them the same `OverrideGraphicSettings`, so one fix buys four). **B - 6 need a way to say *that one* that `selected` cannot give**: 4 want ONE element AND a working set, 2 want TWO particular elements, and a Revit selection is one thing. **C - 5 need a chain value no PROVEN fragment provides** - `targets`, `parents`, `first`/`second`, `imports`. **D - 59 are arrangeable in principle and blocked by CONTENT or simply untried.** **WHAT THE CLASSIFIER CANNOT SEE, SAID PLAINLY:** it reads declared TYPES, so `find-clashes` lands in D although its `against` is an `ElementId` whose NAME `OneIdNamed` refuses - a name-level refusal looks like a receivable type from here. D is the bucket to distrust; A, B and C are structural and firm.

**State.** Not a defect row. The measured answer to *what is actually left*, replacing three earlier estimates that were all too optimistic

---
