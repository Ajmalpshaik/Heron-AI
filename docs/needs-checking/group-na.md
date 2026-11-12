# Needs checking — Group NA

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group NA - a form's SKETCH locked to named planes - a sweep's path lines, a circle's centre: `LOCK_FORM_SKETCH_TO_PLANES` (2026-11-10)

Two jobs on 2026-11-10 (Revit 2020, session 9912) needed a lock Heron did not have: a square diffuser's cone
rings as sweeps, whose FACE locks made every size change fail ("Can't create sweep"), and a plenum box's spigot
circle that kept its labelled size and drifted 75 mm off centre. The owner chose a Heron tool over locking by
hand. Made NEW, at DRAFT - `LOCK_FORM_TO_PLANES` is PROVEN and stays untouched below `impl/`; why, and what is not
known yet, is [FRAGMENT-ISSUES row 5b-1491](../fragment-issues/section-5b-rows-401-425.md).

| What | Where | What it does |
|---|---|---|
| `LOCK_FORM_SKETCH_TO_PLANES` (MODIFY) | [`brain/fragments/lock-form-sketch-to-planes`](../../brain/fragments/lock-form-sketch-to-planes/fragment.yaml) | For each named plane: every straight sketch line lying in it, and every circle or arc centre on it (once per centre), is aligned and locked - an extrusion's or revolve's sketch, a sweep's or swept blend's path, a blend's base and top. In the plan or elevation looking straight at the sketch. A plane with nothing on it refuses the whole call, naming the nearest line or centre and the gap. Every lock read back locked; the form still a solid, its extent unmoved |

**What is done and what is not.** The card compiles on all eight releases, 2020 to 2027, with the whole library
(`tools/check-fragments-compile.py`, 2026-11-10, the owner's PC). Every Revit member it uses is in the card's
compatibility note, looked up with `tools/api-surface --members`. **Nothing here has run in Revit.**
*Out of date since 2026-11-10: NA1 to NA5 ran and passed and the card was signed PROVEN - the runs recorded below the
table. Written 2026-11-12, from those records.*

**The model - a SAVED scratch family, never the owner's open families.** File, New, Family, the metric Generic
Model template, saved as "Heron Sketch Lock Test" under `%TEMP%\heron-work\sketch-lock`, Revit 2020. The job
file is [`tools/jobs/lock-form-sketch-na-2026-11-10.yaml`](../../tools/jobs/lock-form-sketch-na-2026-11-10.yaml)
(`--dry-run` reads it back and sends nothing); its header has the arrangement. Every line with
`HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **NA0** | *Open and arrange, KEEP it.* The scratch family; its planes, the sweep and the circle extrusion as the job file's header says, made with Heron's own tools; record both forms' ids into the job file. Save | The title bar names the scratch family; the sweep's faces carry NO locks |
| ~~**NA1**~~ | *The D-30 negative FIRST, then the positive - the sweep.* The job file's first job, rolled back | Negative ("Ring Left, Spigot Y" - all or nothing): refused, the nearest path lines named 200 mm off "Spigot Y", and NO lock on "Ring Left" either. Positive: four locks, one per path line, in Ref. Level - **if Revit refuses a straight sketch line's reference, that is the first thing this group learns** and the card is changed, not proved **PASSED 2026-11-10, Revit 2020 (session 9912), rolled back - the run recorded below; the card signed PROVEN that day.** |
| ~~**NA2**~~ | *The circle - negative first, then positive.* The job file's second job, rolled back | Negative: the centre named 450 mm off "Ring Left". Positive: two locks on ONE half-arc's centre; a line saying "Center Mark Visible" was switched on, or not **PASSED 2026-11-10, Revit 2020 (session 9912), rolled back - the run recorded below; the card signed PROVEN that day.** |
| ~~**NA3**~~ | *A plane that cannot be locked.* The job file's third job: the sweep with planes "Ref. Level" | Refused as not standing square to the path, saying a sweep's ends follow its path - and NOT naming `LOCK_FORM_TO_PLANES` **PASSED 2026-11-10, Revit 2020 (session 9912), rolled back - the run recorded below; the card signed PROVEN that day.** |
| **NA3b** | *A second run.* NA4's kept sweep, the first job's positive once more | Nothing new, nothing refused: each of the four said "already locked, left as it was" |
| ~~**NA4**~~ | *FOLLOW - the sweep, kept.* Run NA1's positive through `try-once` (or `revit_change` once PROVEN) and KEEP; label "Ring Left"-"Ring Right" and "Ring Front"-"Ring Back" with a length "Ring Size" (600, EQ about the centre planes); `FLEX_FAMILY` "Ring Size" at 450 and 900 | The sweep rebuilt at each size, mitred corners, read back 450 and 900 across; **no "Can't create sweep"** - the failure of 2026-11-10 gone. LOOK at Edit Sweep > Edit Path: four closed padlocks **PASSED 2026-11-10, Revit 2020 (session 9912) - the second run recorded below; the card signed PROVEN that day.** |
| ~~**NA5**~~ | *FOLLOW - the circle, kept.* NA2's positive kept; label "Spigot X" from "Center (Left/Right)" and "Spigot Y" from "Center (Front/Back)" (150 / 100); `FLEX_FAMILY` them to 250 / 50 | The circle's centre read back at (250, 50), its 150 mm diameter unchanged - both halves moved together **PASSED 2026-11-10, Revit 2020 (session 9912) - the second run recorded below; the card signed PROVEN that day.** |
| **NA6** | *Second route.* By hand on a copy: Edit Sweep > Edit Path, Align, the plane, the line, the padlock; Center Mark Visible, Align, the centre, the padlock; flexed side by side with NA4 and NA5 | The two copies read back the same at every size |
| **NA7** | *The kept NA4's own negative - Revit, the scratch family with NA4's sweep kept.* `validate lock-form-sketch-to-planes --write --kept` with NA1's negative, planes "Ring Left, Spigot Y" | Refused as NA1's negative was - the nearest path lines named off "Spigot Y" - and nothing new locked, the four kept locks as they were. **NOT RUN:** both run records below name it as not run, and the SIGNED line as still open, in prose only; written as a row 2026-11-12 so that it is counted |

**Run 2026-11-10, ~15:10 - NA1, NA2 and NA3 PASS, rolled back** (Revit 2020, session 9912, the scratch family "Heron Sketch Lock Test", 1,529 elements; `tools/batch-prove.py` with a filled copy of the job file; log `%TEMP%\heron-work\sketch-lock\na-run2.txt`). NA1: four path lines locked; "Ring Left, Spigot Y" refused with nothing locked - **Revit takes a straight sketch line's reference outside sketch mode.** NA2: the circle's centre locked to "Spigot X" and "Spigot Y"; "Ring Left" refused. NA3: "Ring Front, Ring Back" locked; "Ref. Level" refused without naming `LOCK_FORM_TO_PLANES`. The first attempt answered "Could not identify the active model" for all three because a chat held the model; `revit_release` cleared it. Proof drafts are local (gitignored), unsigned. **Still owed:** NA4 and NA5 (kept, then flexed - the FOLLOW half), NA3b, NA6 (second route), and the owner's signature. The card stays DRAFT.

**Run 2026-11-10, ~15:15-15:20 - NA4 and NA5 PASS** (same Revit, session and scratch family). NA4: `try-once`,
**KEPT by the owner** - the four path lines locked to "Ring Left / Right / Front / Back" (record
`brain/proof-drafts/runs/lock-form-sketch-to-planes.once.json`, local); then "Ring Width" / "Ring Depth" labelled
EQ about the centre planes and the type set with `SET_FAMILY_TYPE_VALUES` (PROVEN, kept), read back with
`REPORT_FAMILY_FORMS`: 450 x 450 read X -225..225, Y -225..225; 900 x 450 read X -450..450, Y -225..225; back to 600 -
**no "Can't create sweep"**, the failure of 2026-11-10 gone. NA5: rolled back (`validate --write --keep-chain`, the lock
and `SET_FAMILY_TYPE_VALUES` as setups, `REPORT_FAMILY_FORMS` under test), "Spigot X / Y" labelled from the centre
planes. **FINDING:** with NO size label the circle's centre reached (250, 50) but the circle was STRETCHED to 350 across
(X 75..425, Y -125..225) - one edge stayed put; with its diameter labelled "Spigot Dia" (150) by `LABEL_FAMILY_RADIUS`
first, it read X 175..325, Y -25..125 - centre (250, 50), 150 across. So the size is labelled before the centre is
locked; the card's purpose and `family-creation` step 9 now say so (no code change, so these runs still describe it).
**Not run:** the rolled-back negative for the kept NA4 (`validate --kept`), NA3b and NA6 (the hand route). Unsigned.

**SIGNED 2026-11-10 by Ajmal PS - `LOCK_FORM_SKETCH_TO_PLANES` is PROVEN** on NA3's run (the proof block in the card),
with NA1, NA2, NA4 and NA5 above beside it. Still open, not blocking: NA3b, NA6 and the kept NA4's own negative.

**Before the owner's own families.** The first run on `KDF Square Diffuser - Supply_DRAFT.rfa` or
`Plenum Box_DETAILED_DRAFT.rfa` is his choice, through `try-once`, behind Revit's Keep or Discard. The diffuser's
sweeps still carry the FACE locks made on 2026-11-10: the card counts them and says so, and does not remove them -
release them by hand first, or the flex will fail for that reason, not this card's.
