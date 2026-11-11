# Needs checking — Group NH

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group NH - the modeller clicks in Revit to answer the chat: `PICK_ON_SCREEN` (2026-11-11)

Found missing on 2026-11-11: the owner wants to work by chat with very few clicks in Revit, and nothing in Heron
let the modeller click to answer the chat - a point had to be typed as three millimetre numbers
([D-67](../decisions/D-67.md)). `PICK_ON_SCREEN` is new, DRAFT, and asks for points only. Why it stops at points,
and what the add-in does with a click that comes late, is
[FRAGMENT-ISSUES row 5b-1507](../fragment-issues/section-5b-rows-401-425.md) and
[Q-86](../open-questions/tier-2.md).

| What | Where | What it does |
|---|---|---|
| `PICK_ON_SCREEN` version 1 (EXECUTE) | [`brain/fragments/pick-on-screen`](../../brain/fragments/pick-on-screen/fragment.yaml) | Puts Revit into its own pick mode, waits for `count` clicks (left out, one) in the view in front, and hands the points back in millimetres as [D-67](../decisions/D-67.md) writes them. Esc stops it and hands back what was clicked so far. Changes nothing in the model |

**What is done and what is not.** The card compiles on all eight releases, 2020 to 2027, with the whole library
(`tools/check-fragments-compile.py`, 2026-11-11, a cloud container with no Revit). **Nothing here has run in
Revit - every row below is NEEDS REAL REVIT.** The batch half is
[`tools/jobs/pick-on-screen-nh-2026-11-11.yaml`](../../tools/jobs/pick-on-screen-nh-2026-11-11.yaml), read back by
`--dry-run` as 1 WOULD RUN.

**Somebody is at Revit for every row.** The card waits for a click, so none of this runs overnight or
`--unattended`. Click as soon as the status bar asks, except in NH8 and NH9, which are slow on purpose.

**The model - a SAVED scratch project, never a working model.** File, New, Project, the metric architectural
template, saved as "Heron Pick Test" under `%TEMP%\heron-work\pick`, **Revit 2024**. Every call with
`HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are connected, and `< /dev/null`. The card
opens no transaction, so nothing is rolled back and nothing is kept: the model must read the same after every row.

**The arrangement (NH0), by hand:** in the floor plan "Level 1", Architecture, Model Line, one straight line
drawn 5000 mm long (type 5000 while drawing) - a model line, not a wall, so its two ends are the only things an
endpoint snap can land on; View, Schedules, Schedule/Quantities, Walls, OK, named "Wall Schedule"; the default 3D
view "{3D}" opened once and left with no work plane set; Manage, Snaps, Endpoints ticked. Save.

| # | Run | Look for |
|---|---|---|
| **NH0** | *Open and arrange, save,* as above | The title bar names the scratch project; the line measures 5000 with Revit's own Measure Between Two References |
| **NH1** | *The batch job* - `python tools/batch-prove.py tools/jobs/pick-on-screen-nh-2026-11-11.yaml`. Positive: "Level 1" in front, count 2 - when the status bar reads "Heron: click point 1 of 2 - Esc to stop", click one end of the line, then the other. Negative: "Wall Schedule" in front - click nothing | **Does Revit enter pick mode at all from Heron's request, and does it come to the front by itself** - the first thing this group learns. Positive: clicked 2; pointsMm two points 5000 mm apart, both z 0; cancelled false; refused empty; viewName "Level 1". Negative: no prompt ever appears; clicked 0; pointsMm empty; refused names "Wall Schedule" as a Schedule |
| **NH2** | *Esc at once* - `python mcp/client/heron_bridge_client.py fragment pick-on-screen --set count=2` with "Level 1" in front; press Esc when the prompt appears | An answer, not a failure: clicked 0, pointsMm empty, cancelled true, refused empty, findings "The modeller stopped the pick after 0 of 2 ..." |
| **NH3** | *Esc part way* - the same with `count=3`; click one end, then Esc | clicked 1, that one point handed back, cancelled true |
| **NH4** | *The count* - the same with no `--set` at all, one click; then `--set count=0` | Left out: the prompt reads "point 1 of 1", clicked 1. Zero: refused "count is how many clicks to ask for ...", no prompt in Revit |
| **NH5** | *A view with no work plane* - "{3D}" in front, `--set count=1` | Revit's own refusal in `refused`, with the advice to set a work plane; clicked 0. **If Revit takes the click instead, record which plane `findings` names** - the card's words about 3D views are then wrong and are corrected |
| **NH5b** | *The model behind* - a second project open and in front, the call aimed at the scratch project with `fragment --in "Heron Pick Test" pick-on-screen --set count=1` | refused "This model is not the one in front in Revit ..."; no prompt |
| **NH6** | *The handover* - NH1's pointsMm, copied as it came back, into `PLACE_FAMILY_INSTANCES` as `points`, a loaded furniture type, level "Level 1", `--write` and no `--apply` | Taken as typed: two instances, one on each end of the line, then rolled back. Nothing re-keyed between the two |
| **NH7** | *The second route* - select the line; `REPORT_LOCATION`; and Revit's Measure Between Two References between the two ends | The two ends `REPORT_LOCATION` gives are NH1's two points, to the snap; Measure reads 5000, the distance between them |
| **NH8** | *SLOW, one click* ([Q-86](../open-questions/tier-2.md)) - `--set count=1`; let the prompt sit past `revit.operationTimeoutSeconds` (60 by default), then click | **Read from the code, not yet seen:** the chat is told Revit is still running the request, before the click; Revit stays in pick mode; the click's point reaches nobody. Write down the words the chat was given and what Revit did. A limit of the add-in, not a pass or a fail of the card |
| **NH9** | *SLOW, several clicks* (Q-86) - `--set count=3`; click the first at once, then the second 55 seconds after the request was sent - past the run clock's budget at the default settings, inside the add-in's wait | **Read from the code, not yet seen:** the job is stopped as out of time at the next pass and the first point is NOT handed back. Write down the chat's words |
| **NH10** | *From the chat* - in a Heron chat on the scratch project, with "Level 1" in front: "put it here" | `heron_lookup` names `PICK_ON_SCREEN`; `revit_show` runs it with Changes off; the chat says the points it was given and that the card is DRAFT. Esc: the chat says the pick was cancelled and does not ask Revit again by itself |
| **NH11** | *Two more releases* - NH1 and NH2 again on Revit 2020 and on the newest release installed | The same answers. A pick is the modeller's own Revit doing the work, and the releases run on different .NET |

**What would make the card PROVEN** ([D-30](../decisions/D-30.md)): NH1 with both legs holding, drafted and
signed by a person. NH2 to NH7 are the cases a batch cannot judge and are read by eye. NH8 and NH9 measure the
limit Q-86 is about and do not hold the proof up.
