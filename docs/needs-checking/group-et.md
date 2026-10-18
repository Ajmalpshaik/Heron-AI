# Needs checking — Group ET

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group ET - Revit asks Keep or Discard before a change from a chat is kept: D-126 (2026-10-17)

[D-126](../DECISIONS.md#d-126--revit-asks-the-modeller-keep-or-discard-before-a-change-from-a-chat-is-kept)
(accepted by the owner 2026-10-17) supersedes D-99. `revit_change` still runs its change inside one
`TransactionGroup`; then, with the group open, Revit selects and isolates what it touched and a dialog asks
**Keep it** or **Discard it** - the same box as D-119's DRAFT try-once (Group EM), one box for both. Keep is one undo entry, `Heron: <name>`; Discard, Esc or the close button rolls the
group back and puts the modeller's own selection back. The owner's switch is `write.askBeforeKeeping` in
`heron.config`, on by default.

**What is compiled and what is not run.** The add-in builds for all eight releases 2020 to 2027
(`tools/check-compile.py`, 2026-10-17), and every Revit member it calls exists on all eight
(`tools/check-api-surface.py`). `tests/test_keep_question.py` runs the Revit-free half - the words, the rule
that only the Keep link keeps, and D-60's cap - in its test host, and reads the wiring as text. Nothing here is PROVEN.

**SINCE 2026-10-18 THE BOX ALSO ZOOMS THE VIEW TO THE CHANGE** - the owner's ask while proving this group
on Revit 2024.3 in *Heron Safety 2024*, chosen over zooming less. With the target model in front and something
selected to show, the active view is zoomed to the box round it, with a quarter of its larger side - never less
than 1000 mm - of room. Keep leaves the view there; Discard, Esc, the X and every failure put the camera back
where it was. A sheet, a schedule, a legend and a perspective 3D view are left as they were, and so is a view
where nothing shown has a box; the box says *zoomed* only when it was. Compiled for all eight releases and every
member found on all eight (2026-10-18), the words and the margin run in the test host - **NOT YET RUN IN
REVIT**: **ET14**.

**THE FIRST REVIT RUN CRASHED BEFORE THE BOX, and the fix has not run in Revit.** 2026-10-17, Revit 2024.3 on
the owner's laptop, add-in `main` at `76006767`: a PROVEN `CREATE_DUCT` kept through `revit_change` came back
to the chat as *"Corresponding button not found. Parameter name: defaultButton"*, the log named
`TaskDialog.set_DefaultButton` inside `RevitFragment.AskTheModeller`, and no box appeared. The model was
left unchanged - it failed closed - but no row below could have passed on that add-in.
[Row 5b-621](../fragment-issues/section-5b-rows-401-425.md) has the reply, the log and the cause: Discard
was made the default before the box had its two links. Fixed in the add-in the same day and compiled for
all eight releases, **NOT YET RUN IN REVIT** - **ET13 first**, then the rest.

**Before any row: rebuild the add-in, deploy it for each Revit, restart Revit, and restart the Claude app**
- the question lives in the add-in, and the client's longer wait for a change that keeps lives in the
server. Changes must be ON.

**A NEW SCRATCH PROJECT, never a working model.** In Revit 2020 first: a project from the default metric
template, saved as *Heron Keep Question.rvt*, with Level 1 and Level 2 and two walls on Level 1. Open the
South elevation for ET1 to ET4.

| # | Run | Look for |
|---|---|---|
| **ET1** | Select one of the two walls. Then in the chat: *"create a level called ET Test at 6000 mm"* (`revit_change` capability `CREATE_LEVEL`). When the dialog appears, read it, then choose **Keep it** | The dialog says `'CREATE_LEVEL' ran in Heron Keep Question`, **Revit counts 1 element added** (or more if Revit adds its plan views - write down the numbers it gives), and that the added elements are selected and isolated; the new level is the only thing visible in the elevation while it is up. After Keep: the elevation is back to normal (isolation ended), the new level is selected, the Undo list's top entry is **one** `Heron: CREATE_LEVEL`, and the reply says *"You were asked in Revit and pressed KEEP after seeing the result."* Then press **Ctrl+Z once**: the level is gone and nothing else moved. **The D-30 positive.** If **no dialog appears** and the verdict says *"Revit reported nothing added, changed or deleted"*, the change collector did not hear Revit inside Heron's group - D-126's accepted failure, and this row fails |
| **ET2** | Same model and view. Select the two walls. Note the element count of the Levels (e.g. a level schedule or Project Browser). Ask the same level again, `ET Test 2` at 7000 mm, and choose **Discard it** | The level count is **unchanged**, there is **no** new `Heron:` entry at the top of the Undo list, the elevation is not isolated, and the **two walls are selected again** - the modeller's own selection, put back. The chat says *"The modeller chose Discard in Revit, so NOTHING WAS KEPT"* and does **not** try again or try another capability. **The D-30 negative** |
| **ET3** | Ask the same level twice more. The first time press **Esc**; the second time close the dialog with its **X** | Both times exactly ET2's result: nothing kept, no undo entry, selection back. Only the Keep link keeps |
| **ET4** | Ask the same level again and **wait 90 seconds** before choosing **Keep** | The chat gets the final answer - the KEPT verdict and *"You were asked in Revit and pressed KEEP"* - and never *"still running"*. One undo entry, as ET1. *Then, separately and only if there is time:* set `revit.askTimeoutSeconds = 60` in `heron.config`, restart Revit and the Claude app, ask again and wait 3 minutes: the chat is told Revit is **still waiting for you to choose Keep or Discard** and not to repeat it; then choose Discard and see the model unchanged. Put the setting back to 300 |
| **ET5** | Open a second scratch project (*Heron Keep Back.rvt*), leave it in front, and ask the chat to create the level in *Heron Keep Question* by name | The dialog says the model **is not the one in front** and nothing is selected or isolated in *either* window. Keep works as ET1 in the named model; nothing appears in the model in front |
| **ET6** | **Waits on `CREATE_STAIRS` being PROVEN.** It is the only fragment that changes the model inside one of Revit's edit modes (D-112), and it is DRAFT - so D-117 (Article 11) refuses to keep it, through the chat and through `--write --apply` alike, and no question is ever reached. Once it is PROVEN: in *Heron Keep Question*, ask for a stair between Level 1 and Level 2. Choose **Keep**; then ask again and choose **Discard** | Keep: the stair is there and **one** `Heron:` undo entry. Discard: no stair, no new undo entry. The run must not be refused with "left a transaction of its own open" |
| **ET7** | *An observation, not a proof.* Does `View.IsolateElementsTemporary` need a transaction? Run, with `revit_read` (no transaction open), a scratch fragment outside the repository that calls `uidoc.ActiveView.IsolateElementsTemporary` on one wall's id | Write down which: it isolates (no transaction needed), or Revit refuses with a modification-outside-a-transaction error (needed). The add-in gives it one either way; this says whether that was necessary. Then turn the isolation off by hand |
| **ET8** | *Watching, not a proof.* During ET1 | The amber activity banner is **not** over the dialog while it is up, and comes back for a moment after the answer |
| **ET9** | Set `write.askBeforeKeeping = false` in `heron.config`, restart Revit, and ask for a level `ET Off` | **No dialog**: kept at once, as D-99 kept it - one undo entry, and the reply carries no `asked` - nobody was asked. Put the setting back to `true` and restart |
| **ET10** | With the switch on, ask the chat to open the South elevation (`OPEN_VIEW`) while a plan is in front | **No dialog** - Revit reports no change to the model - and the verdict says *"the modeller was not asked"*. The view opens |
| **ET11** | ET1 and ET2 once on Revit 2024 and once on 2027 | The same answers - the question is in the add-in every release loads |
| **ET12** | *Watching, not a proof.* On every row above that shows the question | It waits for a click. If it ever closes by itself, or the chat reports `dialog_refused` or a Discard nobody chose, the job's dialog watch (Group EL) is answering Heron's own question and `HeronDialogWatch.HeronAsks()` is not standing it down - write down the row and what the chat said |
| **ET13** | **Run first, after the 5b-621 fix.** Close every Revit, deploy the add-in LOCALLY for each release, restart Revit 2024 and the Claude app. In a SCRATCH copy of `rme_basic_sample_project.rvt`, ask the chat for the same duct the crash met: `CREATE_DUCT`, `systemType=Supply Air`, `ductType=Rectangular Duct: Mitered Elbows / Tees`, `level=Level 1`, `points=0,-28000,3000; 8000,-28000,3000`. When the box appears, press **Enter** without clicking. Ask again and choose **Keep it**. Then ET1 on 2020 and 2027 | **The box appears** - no *"Corresponding button not found"* in the chat, and no `set_DefaultButton` in the day's add-in log. **Discard it** is the highlighted link, so Enter discards: the element count is as before, no new `Heron:` undo entry, the view is not left isolated, and the selection is the modeller's own. Keep: one duct, one `Heron: CREATE_DUCT` undo entry. If the box still does not appear, the chat must say nothing was kept, the view must not be left isolated and the selection must be put back - write down the log line that starts *"The Keep or Discard box could not be built or shown"* |
| **ET14** | **The view zoomed to the change - the owner's ask, 2026-10-18.** Rebuild and deploy the add-in for each release, restart Revit and the Claude app. In *Heron Keep Question*, on Revit 2020 first. **(a)** Open the **Level 1** plan and zoom in tight on the two walls, so the empty area well away from them is off screen; take a screenshot. Ask the chat for a wall on Level 1 from `20000,20000` to `26000,20000` mm (`CREATE_WALL`). Read the box, then choose **Discard it**. **(b)** Ask for the same wall again and press **Esc**; once more, and close the box with its **X**. **(c)** Ask for it again and choose **Keep it**. **(d)** Open the **South** elevation, zoomed in on Level 1's head only, and ask for a level `ET Zoom` at 9000 mm; **Discard it**. **(e)** With a **sheet** in front, ask for a level `ET Sheet` at 9500 mm; **Discard it**. Then (a), (c) and (e) on **Revit 2024** and on **2027** | **(a)** While the box is up, the plan is zoomed onto the new wall with clear room round it - the wall touches no edge of the window - and the box says *"The view is zoomed to them, with some room around. Keep leaves it there; Discard puts the view back where it was."* After Discard the plan is back **exactly** as in the screenshot: the same centre and the same zoom. **(b)** Esc and the X each do exactly what Discard did. **(c)** After Keep the plan **stays** zoomed onto the kept wall, the wall is selected, and the top of the Undo list is **one** `Heron: CREATE_WALL` - the zoom adds no undo entry. **(d)** The elevation is zoomed onto the new level line, and goes back exactly on Discard. **(e)** The sheet does **not** move, and the box does **not** say zoomed. On every run the day's add-in log has no line *"Could not zoom the view to the change"* or *"Could not put the view back where it was"*. **If one appears** - above all on 2020, where the zoom runs inside Heron's open transaction group - write the line down: the box must still have appeared, must not have said zoomed, and Discard must still have left the model unchanged. Nothing about the zoom is PROVEN until this row passes |

### The 2026-10-18 run

**On the owner's laptop, 2026-10-18.** Revit 2024.3 session 63712, and for the rows marked 2027 Revit 2027.3
session 22024, both on add-in `0.1.0+48230c8c` - `main`, Debug, deployed 08:45 local by this run with
`tools/deploy-addin.ps1` for 2024, 2026 and 2027. Revit 2020 session 55496 belonged to another chat and ran
`ab77d5fb`; **nothing here ran on Revit 2020.** Models, all scratch copies in `%TEMP%\heron-work\safety-1018\models`:
*Heron Safety 2024* (Autodesk's `rme_basic_sample_project.rvt`, via the move-rails chat's *Rails-X2*; 21,562
elements at open), *Heron Guard B 2024* (a copy of *Rails-S*, the Dynamo sample) and *Heron Safety 2027* (Revit
2027's `Snowdon Towers Sample HVAC.rvt`, links unresolved; 9,813 elements). Changes, Admin and Publish on except
where a row says. **The owner pressed every Keep, Discard, Esc, X and Undo himself;** nothing answered a box for
him. Undo lists were read with UI Automation, read-only. Throwaway probe fragments were made in `brain/fragments/`
for the session and deleted by name at the end - none is committed.

| # | Release | What was run and what came back | State |
|---|---|---|---|
| **ET13** | 2024 | Run first. `revit_change` `CREATE_DUCT` with the row's values: **the box appeared** - no *"Corresponding button not found"*, no `set_DefaultButton` in the log. He clicked **Keep it**: 21,562 → 21,565 elements, the Undo list exactly one entry `Heron: create-duct`. The Enter half: at 05:59 he pressed Enter only and reported **Keep it** as the highlighted link - the run was KEPT (`modellerAnswer keep`). Re-checked at 06:47 with the model in front: his screenshot shows **Discard it** with the focus frame, he pressed Enter only, and the run was DISCARDED, nothing kept | **PASS on the re-check**; the 05:59 observation is kept beside it - if Keep is ever seen highlighted again, picture the box before answering |
| **ET1** | 2024 | With `CREATE_DUCT`, not `CREATE_LEVEL`. The box read *"The added elements are selected and isolated in the active view"* (his screenshot); after Keep the duct and its system were selected (`READ_SELECTION` 891701, 891702); one undo entry. **The chat was never told it was asked:** the run record carries `asking` *"You were asked in Revit and pressed KEEP after seeing the result."*, the printed reply does not - [5b-1093](../fragment-issues/section-5b-rows-401-425.md). Ctrl+Z on it not run | **FAIL** (the chat's words); the box half held |
| **ET2** | 2024 | Two of his ducts selected by Keep; `CREATE_DUCT` again, he clicked **Discard it**: *"The modeller chose Discard in Revit, so NOTHING WAS KEPT ... Their own selection was put back"*; 21,568 unchanged; `READ_SELECTION` the same two ids; no new undo entry | **PASS** |
| **ET3** | 2024 | Esc, then the X: both recorded `modellerAnswer closed`, nothing kept, selection back, count and Undo list unchanged | **PASS** |
| **ET4** | 2024 | He waited about 57 s (audit `ms` 56,626) before Keep: the KEPT verdict arrived, never *"still running"* | **part** - 90 s not reached; the `askTimeoutSeconds = 60` half NOT RUN |
| **ET5** | 2024 | *Heron Guard B 2024* in front, `CREATE_DUCT` in *Heron Safety 2024* by name: the box read *"This model is not the one in front, so nothing is selected or isolated on screen"* (his screenshot); Keep kept it in A (`wasActiveDocument` false); B 4,948 elements before and after; nothing selected in A | **PASS** |
| **ET6** | - | `CREATE_STAIRS` is still DRAFT | **NOT RUN** (waits on its proof) |
| **ET7, ET8** | - | Not arranged; the banner was not watched | **NOT RUN** |
| **ET9** | - | Needs `write.askBeforeKeeping` edited in the owner's own `heron.config` | **NOT RUN** |
| **ET10** | 2024 | `OPEN_VIEW` `Level 1 HVAC Plan` through `revit_change`: *"Revit reported nothing added, changed or deleted in the model, so the modeller was not asked"*; the plan opened; Undo list unchanged | **PASS** |
| **ET11** | 2027 | `CREATE_LEVEL` *ET Test* at 60000 mm, Keep: kept, Revit counted one level added. *ET Test 2* at 70000 mm, Discard: nothing kept. Undo list exactly `Heron: create-level` and EM6's `Heron: create-line-pattern` | **PASS** on 2027 |
| **ET12** | 2024, 2027 | Every box waited for a click - none closed by itself, no `dialog_refused` on a Heron box. [EL3](group-el.md) shows the watch DOES answer an add-in's own TaskDialog, so `HeronAsks()` is what keeps it off this one | **PASS** (watching) |

**Asked for by the owner while running ET:** when the box shows a change, zoom the view to it with a little
margin. On the add-in run here it did not zoom - it only selected and isolated at the view's current zoom. Built
the same day in another session and merged as **ET14** (#648); not run in Revit by this run.
