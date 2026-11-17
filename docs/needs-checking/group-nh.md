# Needs checking — Group NH

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group NH - the modeller clicks in Revit to answer the chat: `PICK_ON_SCREEN` (2026-11-11)

**Tracked as GitHub issue [#992](https://github.com/Ajmalpshaik/Heron-AI/issues/992) since 2026-11-16** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

Found missing on 2026-11-11: the owner wants to work by chat with very few clicks in Revit, and nothing in Heron
let the modeller click to answer the chat - a point had to be typed as three millimetre numbers
([D-67](../decisions/D-67.md)). `PICK_ON_SCREEN` is new, DRAFT, and asks for one point for each call. Why it
stops at a point and at one click, and what the add-in does with a click that comes late, is
[FRAGMENT-ISSUES row 5b-1507](../fragment-issues/section-5b-rows-1501-1525.md) and
[Q-86](../open-questions/tier-2.md).

| What | Where | What it does |
|---|---|---|
| `PICK_ON_SCREEN` version 1 (EXECUTE) | [`brain/fragments/pick-on-screen`](../../brain/fragments/pick-on-screen/fragment.yaml) | Puts Revit into its own pick mode, waits for ONE click in the view in front, and hands the point back in millimetres as [D-67](../decisions/D-67.md) writes one. Esc cancels. Changes nothing in the model |

**What is done and what is not.** The card compiles on all eight releases, 2020 to 2027, with the whole library
(`tools/check-fragments-compile.py`, 2026-11-11, a cloud container with no Revit). **Nothing here has run in
Revit - every row below is NEEDS REAL REVIT.** The batch half is
[`tools/jobs/pick-on-screen-nh-2026-11-11.yaml`](../../tools/jobs/pick-on-screen-nh-2026-11-11.yaml), read back by
`--dry-run` as 1 WOULD RUN.

**And the wait it needs, since 2026-11-16 ([Q-86](../open-questions/tier-2.md), decided 2026-11-15).** The card
carries the label `waitsForModeller`, and for a card that carries it the add-in holds its timers as it does for
the Keep or Discard box: the run clock is paused for the card's code, the dispatcher waits
`revit.askTimeoutSeconds` more (300 as installed) and says `awaiting_click` in words of its own if even that runs
out, and the chat's side waits as long. Built and compiled for all eight releases in a cloud container, held by
`tests/test_person_wait.py` - **not run in Revit**. Every row below needs the add-in rebuilt and redeployed from
2026-11-16 or later: one built before does not know the label and refuses the card before anything is asked.

**Somebody is at Revit for every row.** The card waits for a click, so none of this runs overnight or
`--unattended`. Click as soon as the status bar asks, except in NH9 and NH12 to NH14, which are slow on purpose.

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
| ~~**NH8**~~ | ~~*SLOW* ([Q-86](../open-questions/tier-2.md)) - the same command; let the prompt sit past `revit.operationTimeoutSeconds` (60 as installed), then click~~ **SUPERSEDED by NH12** - on 2026-11-16 the card took the label `waitsForModeller` and the add-in its hold, so a click after a minute is expected to reach the card; an add-in built before refuses the card outright, so this row can no longer be run as written | ~~**Read from the code, not yet seen:** before the click the chat is told Revit is still working on the request and not to repeat it; Revit stays in pick mode; the click's point reaches nobody and no answer follows. Write down the words the chat was given and what Revit did. A limit of the add-in, not a pass or a fail of the card~~ |
| **NH9** | *SLOW, BUT INSIDE THE WAIT* (Q-86) - the same command; click about 55 seconds after the request was sent, past the run clock's budget as installed and inside the add-in's wait | The point comes back as in NH1, waitedSeconds about 55 - the card has no loop, so nothing asks the run clock after the click. **If the chat is told the job ran out of time instead, record its words** - the card's reading of the add-in is then wrong |
| **NH10** | *From the chat* - in a Heron chat on the scratch project, with "Level 1" in front: "I will click the point in Revit" | `heron_lookup` names `PICK_ON_SCREEN` and its answer says Revit waits several minutes for the click; `revit_show` runs it with Changes off; the chat says the point it was given and that the card is DRAFT. Esc: the chat says the pick was cancelled and does not ask Revit again by itself. Then NH12 from the chat: where the host shows progress, it reads "Waiting for you to click in Revit - N s so far" rather than "Waiting for Revit" |
| **NH11** | *Two more releases* - NH1 and NH2 again on Revit 2020 and on the newest release installed | The same answers. A pick is the modeller's own Revit doing the work, and the releases run on different .NET |
| **NH12** | *A CLICK AFTER MORE THAN A MINUTE REACHES ITS TOOL* ([Q-86](../open-questions/tier-2.md), the add-in rebuilt and redeployed from 2026-11-16) - the same command; let the prompt sit about 90 seconds, past `revit.operationTimeoutSeconds` (60 as installed), then click one end of the line | **Read from the code, not yet seen:** the point comes back as in NH1, `waitedSeconds` about 90; the command is never told Revit is "still working" and never that the job "ran out of time"; Heron's banner is not over the view while Revit waits, and comes back with the outcome. The trail (`audit-YYYYMM.jsonl`) has one successful `run_fragment_read` of `pick-on-screen`. **If the command is told `still_running` at about 60 seconds, the hold did not reach the dispatcher - record the words** |
| **NH13** | *A SLOW SERIES OF CLICKS IS NOT A RUNAWAY JOB* (Q-86) - three calls one after another, each clicked 70 to 90 seconds after its prompt: one end of the line, the other end, the first end again | Three points, the first and third the same and 5000 mm from the second; none of the three answers says "ran out of time", "a loop that never ends" or "still working"; three successful runs in the trail. Then NH9 again on this add-in: about 55 seconds, past the run clock's budget, answers the same |
| **NH14** | *PAST EVEN THE LONGER WAIT* (Q-86) - the same command; leave the prompt past `revit.operationTimeoutSeconds` plus `revit.askTimeoutSeconds` (six minutes as installed), then press Esc. And once, the change path: Changes on in Heron's Settings, `--write` on the same command (no `--apply`), then Changes off again | **Read from the code, not yet seen:** at about six minutes the command is told `awaiting_click` - Revit is still waiting for the click, Heron has stopped waiting, press Esc and ask again - never Keep or Discard's words; Revit stays in pick mode until the Esc, and the banner comes back when it ends. With `--write`: refused `waits_on_change_path` before anything is asked (with Changes off it is `write_disabled` first, which says nothing about this) - no prompt in Revit, nothing run. Record the words of both |

**What would make the card PROVEN** ([D-30](../decisions/D-30.md)): NH1 with both legs holding, drafted and
signed by a person. NH2 to NH7 are the cases a batch cannot judge and are read by eye. NH9 and NH12 to NH14
measure the wait Q-86 is about and do not hold the proof up.
