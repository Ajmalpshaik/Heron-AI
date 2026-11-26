# Needs checking — Group QK

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group QK - CREATE_CURTAIN_SYSTEM version 2: a curtain system on chosen mass faces - sloped, by number, all but some - never over a face already glazed - DRAFT until proved (2026-11-24)

**Tracked as GitHub issue [#1366](https://github.com/Ajmalpshaik/Heron-AI/issues/1366) since 2026-11-24** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**LOCAL work - it needs Revit on the owner's PC.** A cloud session cannot run any of it.

**Why:** [row 5b-2020](../fragment-issues/row-5b-2020.md), issue [#1344](https://github.com/Ajmalpshaik/Heron-AI/issues/1344).
On Revit 2024, on a folded mass of 14 faces, `faces=vertical` left its 3 tilted faces bare, and `faces=all` would
have laid a second curtain system over the 7 faces already glazed and glazed the roof and the cantilever soffit,
which had to stay solid. [`create-curtain-system`](../../brain/fragments/create-curtain-system/fragment.yaml)
(FRG-ELE-127) **version 2**, written in a cloud session:

- `faces` takes **vertical** (the default), **top**, **underside**, **sloped** (tilted or curved), **all**, face
  **numbers** (*"3, 5 and 9"*, *"4 to 7"*), joined (*"vertical and sloped"*) or taken away (*"all but top and
  underside"*). Numbers need one host.
- Every reply - dry run, refusal, run - **lists the host's faces numbered**: kind, the way it looks (compass point in
  the model's own directions, and its tilt), area, any curtain system it already carries, and which are asked.
- **A face asked that already carries a curtain system refuses the whole call** (`alreadyGlazed`), naming the glazed
  faces and the bare ones. The owner chose refusing over skipping the glazed faces, 2026-11-24, Ajmal: *"A"*. Revit gives a curtain system no host face to read back, so it is found from the standing
  system: each curtain grid is read cell by cell, and the host face holding the middle of every edge of a cell, to
  within 5 mm, is the face that grid sits on.

Version 1 was PROVEN on 2026-11-01 (Group JM); its C# changed, so it is DRAFT again (D-30).

**What is done and what is not.** It compiles for all eight releases 2020 to 2027
(`tools/check-fragments-compile.py`, cloud container, 2026-11-24) - so `CurtainGrid.GetCurtainCells`,
`CurtainCell.CurveLoops`, `Face.Project` and `IntersectionResult.Distance` are on every release. Routing measured main
against the branch on all four backends, 16 frozen sentences: lexical unchanged; both re-ranked backends gain
*"glaze the tilted faces of the mass"* and *"glaze the mass but leave the roof and soffit solid"*; **the model
backend loses the first** to `CREATE_WALLS_BY_FACE` - a dead heat on main (words #1, nearness #2 against words
#2, nearness #1) that only the PROVEN status nudge decided, gone with the card's return to DRAFT. QK10's
signature gives it back. **Not tested in Revit.** `revit_change`
refuses the write until QK10 is signed.

**What the run must show (assumed, not measured):**
- that a curtain system's cells lie on the host face it was laid on, in the model's coordinates, to within 5 mm -
  so a glazed face is found, and its neighbour is not;
- that an unchanged mass is numbered the same on every read;
- that a folded face reads as *sloped*, and which compass point Revit's +Y gives (project north).

**The model: Group JM's rig, a scratch copy, never a working model** - *Heron JM Curtain.rvt* in
`%TEMP%\heron-work\group-jm` (see [Group JM](group-jm.md) JM0), with one mass added:

| What | How |
|---|---|
| *Heron QK Fold* | Massing & Site > In-Place Mass, named *Heron QK Fold*. On a vertical work plane through y = 0, the closed profile (60000, 0) (72000, 0) (72000, 6000) (66000, 12000) (60000, 12000) in x and z, mm; Create Form, extruded 12000 mm along +Y. Seven faces, derived: 4 upright (x = 60000, x = 72000, y = 0, y = 12000), a top at z = 12000, the underside, and one face tilted 45 degrees from (72000, 6000) to (66000, 12000), looking east and up - 8485 x 12000 mm, 101.8 m2 |

Revit 2020 first, then 2024. [`tools/jobs/create-curtain-system.yaml`](../../tools/jobs/create-curtain-system.yaml)'s
second job is QK6 and QK5 together, once QK4 is done. The card's `tests/cases.yaml` has every case in full.

| ID | Do this | Pass looks like |
|---|---|---|
| **QK1** | `host=Heron QK Fold`, `curtainSystemType=Heron JM 1500`, `faces=sloped`, rolled back | `created` 1, `facesAsked` 1, `facesTaken` 1, `alreadyGlazed` 0; the findings list 7 faces numbered - 4 vertical, 1 top, 1 underside, one *"sloped or curved, looks east, 45 deg up from level, 101.8 m2 - ASKED"*. RECORD each face's number |
| **QK2** | QK1 with `faces=all but top and underside` | `facesAsked` 5, `facesTaken` 5 |
| **QK3** | QK1 with `faces=<the sloped face's number>, <the x = 72000 face's number>` from QK1 | `facesAsked` 2; every face numbered as in QK1 |
| **QK4** | *By hand, kept:* Model by Face > Curtain System, *Heron JM 1500*, the Fold's four upright faces, Create System; save | One curtain system on the Fold - RECORD its id |
| **QK5** | *The negative - the D-30 negative.* The job file's second negative: `faces=all but top and underside` | `created` empty, `alreadyGlazed` 4; `refused` names the four upright faces by number as carrying QK4's curtain system and the sloped face as bare, ending *NOTHING WAS MADE*; still ONE curtain system on the Fold |
| **QK6** | The job file's second positive: `faces=sloped` | `created` 1, `alreadyNear` 1, `alreadyGlazed` 0; the list shows the four upright faces *"carries curtain system <QK4's id>"* |
| **QK7** | *The other refusals, rolled back:* `faces=sideways`; `faces=20`; `faces=1, 2` with the tower and the Fold both selected and `host` left out | Each refused before anything is made: naming *"sideways"* and the words it knows; *"has 7 face(s), numbered 1 to 7 - face 20 is not one of them"*; *"Face numbers name the faces of ONE mass"* |
| **QK8** | *Version 1 unchanged:* the job file's first job (JM2 and JM9) | Version 1's answers, with the tower's 6 faces listed numbered in the findings; `alreadyGlazed` 0 |
| **QK9** | *Routing, on the PC (`model`, re-pointed 2026-11-26: this row asked for `model+rerank`, and [D-201](../decisions/D-201.md) removed the re-ranker):* `heron_lookup` *"put a curtain system on the sloped faces of this mass"* and *"which faces of this mass already have a curtain system"* *(Re-pointed again 2026-11-26: [D-203](../decisions/D-203.md) removed `model` as well, so the PC ranks on `lexical`, the one search every PC and CI share - the run owed is on `lexical`, and its search half is the same answer in any checkout of the same commit.)* | The first `CREATE_CURTAIN_SYSTEM`. RECORD where the second lands: in the cloud it reached `CREATE_CURTAIN_SYSTEM`, a write, on main too - [row 5b-2028](../fragment-issues/row-5b-2028.md) |
| **QK10** | The signature, on the owner's word only, after QK1, QK5, QK6 and QK8 | `python brain/heron_validate.py accept create-curtain-system --by "Ajmal PS"` |

**Closing:** the card goes back to PROVEN only with its positive, its negative and a person's name (D-30); its rows
here are struck with the release, model and date.
