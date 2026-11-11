# Needs checking — Group NH

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group NH - the modeller clicks in Revit to answer the chat: `PICK_ON_SCREEN` (2026-11-11)

Found missing on 2026-11-11: the owner wants to work by chat with very few clicks in Revit, and nothing in Heron
let the modeller click to answer the chat - a point had to be typed as three millimetre numbers
([D-67](../decisions/D-67.md)). `PICK_ON_SCREEN` is new, DRAFT, and asks for one point for each call. Why it
stops at a point and at one click, and what the add-in does with a click that comes late, is
[FRAGMENT-ISSUES row 5b-1507](../fragment-issues/section-5b-rows-401-425.md) and
[Q-86](../open-questions/tier-2.md).

| What | Where | What it does |
|---|---|---|
| `PICK_ON_SCREEN` version 1 (EXECUTE) | [`brain/fragments/pick-on-screen`](../../brain/fragments/pick-on-screen/fragment.yaml) | Puts Revit into its own pick mode, waits for ONE click in the view in front, and hands the point back in millimetres as [D-67](../decisions/D-67.md) writes one. Esc cancels. Changes nothing in the model |

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
| **NH1** | *The batch job* - `python tools/batch-prove.py tools/jobs/pick-on-screen-nh-2026-11-11.yaml`. Positive: "Level 1" in front - when the status bar reads "Heron: click the point - Esc to cancel", click one end of the line. Negative: "Wall Schedule" in front - click nothing | **Does Revit enter pick mode at all from Heron's request, and does it come to the front by itself** - the first thing this group learns. Positive: clicked 1; pointMm one point with z 0; cancelled false; refused empty; viewName "Level 1". Negative: no prompt ever appears; clicked 0; pointMm empty; refused names "Wall Schedule" as a Schedule |
| **NH2** | *Esc* - `python mcp/client/heron_bridge_client.py fragment pick-on-screen` with "Level 1" in front; press Esc when the prompt appears | An answer, not a failure: clicked 0, pointMm empty, cancelled true, refused empty, findings "The modeller cancelled the pick ..." |
| **NH3** | *The answer follows the click* - the same command twice, one end of the line and then the other | Two different points, 5000 mm apart along the line - never the first call's point handed back again |
| **NH5** | *A view with no work plane* - "{3D}" in front, the same command | Revit's own refusal in `refused`, with the advice to set a work plane; clicked 0. **If Revit takes the click instead, record which plane `findings` names** - the card's words about such a view are then wrong and are corrected |
| **NH5b** | *The model behind* - a second project open and in front, the call aimed at the scratch project with `fragment --in "Heron Pick Test" pick-on-screen` | refused "This model is not the one in front in Revit ..."; no prompt |
| **NH6** | *The handover* - NH3's two points joined with "; ", into `PLACE_FAMILY_INSTANCES` as `points`, a loaded furniture type, level "Level 1", `--write` and no `--apply` | Taken as typed: two instances, one on each end of the line, then rolled back. No number re-keyed |
| **NH7** | *The second route* - select the line; `REPORT_LOCATION`; and Revit's Measure Between Two References between the two ends | The two ends `REPORT_LOCATION` gives are NH3's two points, to the snap; Measure reads 5000, the distance between them |
| **NH8** | *SLOW* ([Q-86](../open-questions/tier-2.md)) - the same command; let the prompt sit past `revit.operationTimeoutSeconds` (60 as installed), then click | **Read from the code, not yet seen:** before the click the chat is told Revit is still working on the request and not to repeat it; Revit stays in pick mode; the click's point reaches nobody and no answer follows. Write down the words the chat was given and what Revit did. A limit of the add-in, not a pass or a fail of the card |
| **NH9** | *SLOW, BUT INSIDE THE WAIT* (Q-86) - the same command; click about 55 seconds after the request was sent, past the run clock's budget as installed and inside the add-in's wait | The point comes back as in NH1, waitedSeconds about 55 - the card has no loop, so nothing asks the run clock after the click. **If the chat is told the job ran out of time instead, record its words** - the card's reading of the add-in is then wrong |
| **NH10** | *From the chat* - in a Heron chat on the scratch project, with "Level 1" in front: "I will click the point in Revit" | `heron_lookup` names `PICK_ON_SCREEN` and its answer says the click has to come within about a minute; `revit_show` runs it with Changes off; the chat says the point it was given and that the card is DRAFT. Esc: the chat says the pick was cancelled and does not ask Revit again by itself. Then NH8 from the chat: it tells the modeller the click was not received and asks again |
| **NH11** | *Two more releases* - NH1 and NH2 again on Revit 2020 and on the newest release installed | The same answers. A pick is the modeller's own Revit doing the work, and the releases run on different .NET |

**What would make the card PROVEN** ([D-30](../decisions/D-30.md)): NH1 with both legs holding, drafted and
signed by a person. NH2 to NH7 are the cases a batch cannot judge and are read by eye. NH8 and NH9 measure the
limit Q-86 is about and do not hold the proof up.
