# Needs checking — Group EO

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group EO - Revit asks Keep or Discard before a change from a chat is kept: D-124 (2026-10-17)

[D-124](../DECISIONS.md#d-124--revit-asks-the-modeller-keep-or-discard-before-a-change-from-a-chat-is-kept)
(accepted by the owner 2026-10-17) supersedes D-99. `revit_change` still runs its change inside one
`TransactionGroup`; then, with the group open, Revit selects and isolates what it touched and a dialog asks
**Keep it** or **Discard it** - the same box as D-119's DRAFT try-once (Group EM), one box for both. Keep is one undo entry, `Heron: <name>`; Discard, Esc or the close button rolls the
group back and puts the modeller's own selection back. The owner's switch is `write.askBeforeKeeping` in
`heron.config`, on by default.

**What is compiled and what is not run.** The add-in builds for all eight releases 2020 to 2027
(`tools/check-compile.py`, 2026-10-17), and every Revit member it calls exists on all eight
(`tools/check-api-surface.py`). `tests/test_keep_question.py` runs the Revit-free half - the words, the rule
that only the Keep link keeps, and D-60's cap - in its test host, and reads the wiring as text. **None of it
has run in Revit.** Nothing here is PROVEN.

**Before any row: rebuild the add-in, deploy it for each Revit, restart Revit, and restart the Claude app**
- the question lives in the add-in, and the client's longer wait for a change that keeps lives in the
server. Changes must be ON.

**A NEW SCRATCH PROJECT, never a working model.** In Revit 2020 first: a project from the default metric
template, saved as *Heron Keep Question.rvt*, with Level 1 and Level 2 and two walls on Level 1. Open the
South elevation for EO1 to EO4.

| # | Run | Look for |
|---|---|---|
| **EO1** | Select one of the two walls. Then in the chat: *"create a level called EO Test at 6000 mm"* (`revit_change` capability `CREATE_LEVEL`). When the dialog appears, read it, then choose **Keep it** | The dialog says `'CREATE_LEVEL' ran in Heron Keep Question`, **Revit counts 1 element added** (or more if Revit adds its plan views - write down the numbers it gives), and that the added elements are selected and isolated; the new level is the only thing visible in the elevation while it is up. After Keep: the elevation is back to normal (isolation ended), the new level is selected, the Undo list's top entry is **one** `Heron: CREATE_LEVEL`, and the reply says *"You were asked in Revit and pressed KEEP after seeing the result."* Then press **Ctrl+Z once**: the level is gone and nothing else moved. **The D-30 positive.** If **no dialog appears** and the verdict says *"Revit reported nothing added, changed or deleted"*, the change collector did not hear Revit inside Heron's group - D-124's accepted failure, and this row fails |
| **EO2** | Same model and view. Select the two walls. Note the element count of the Levels (e.g. a level schedule or Project Browser). Ask the same level again, `EO Test 2` at 7000 mm, and choose **Discard it** | The level count is **unchanged**, there is **no** new `Heron:` entry at the top of the Undo list, the elevation is not isolated, and the **two walls are selected again** - the modeller's own selection, put back. The chat says *"The modeller chose Discard in Revit, so NOTHING WAS KEPT"* and does **not** try again or try another capability. **The D-30 negative** |
| **EO3** | Ask the same level twice more. The first time press **Esc**; the second time close the dialog with its **X** | Both times exactly EO2's result: nothing kept, no undo entry, selection back. Only the Keep link keeps |
| **EO4** | Ask the same level again and **wait 90 seconds** before choosing **Keep** | The chat gets the final answer - the KEPT verdict and *"You were asked in Revit and pressed KEEP"* - and never *"still running"*. One undo entry, as EO1. *Then, separately and only if there is time:* set `revit.askTimeoutSeconds = 60` in `heron.config`, restart Revit and the Claude app, ask again and wait 3 minutes: the chat is told Revit is **still waiting for you to choose Keep or Discard** and not to repeat it; then choose Discard and see the model unchanged. Put the setting back to 300 |
| **EO5** | Open a second scratch project (*Heron Keep Back.rvt*), leave it in front, and ask the chat to create the level in *Heron Keep Question* by name | The dialog says the model **is not the one in front** and nothing is selected or isolated in *either* window. Keep works as EO1 in the named model; nothing appears in the model in front |
| **EO6** | **Waits on `CREATE_STAIRS` being PROVEN.** It is the only fragment that changes the model inside one of Revit's edit modes (D-112), and it is DRAFT - so D-117 (Article 11) refuses to keep it, through the chat and through `--write --apply` alike, and no question is ever reached. Once it is PROVEN: in *Heron Keep Question*, ask for a stair between Level 1 and Level 2. Choose **Keep**; then ask again and choose **Discard** | Keep: the stair is there and **one** `Heron:` undo entry. Discard: no stair, no new undo entry. The run must not be refused with "left a transaction of its own open" |
| **EO7** | *An observation, not a proof.* Does `View.IsolateElementsTemporary` need a transaction? Run, with `revit_read` (no transaction open), a scratch fragment outside the repository that calls `uidoc.ActiveView.IsolateElementsTemporary` on one wall's id | Write down which: it isolates (no transaction needed), or Revit refuses with a modification-outside-a-transaction error (needed). The add-in gives it one either way; this says whether that was necessary. Then turn the isolation off by hand |
| **EO8** | *Watching, not a proof.* During EO1 | The amber activity banner is **not** over the dialog while it is up, and comes back for a moment after the answer |
| **EO9** | Set `write.askBeforeKeeping = false` in `heron.config`, restart Revit, and ask for a level `EO Off` | **No dialog**: kept at once, as D-99 kept it - one undo entry, and the reply carries no `asked` - nobody was asked. Put the setting back to `true` and restart |
| **EO10** | With the switch on, ask the chat to open the South elevation (`OPEN_VIEW`) while a plan is in front | **No dialog** - Revit reports no change to the model - and the verdict says *"the modeller was not asked"*. The view opens |
| **EO11** | EO1 and EO2 once on Revit 2024 and once on 2027 | The same answers - the question is in the add-in every release loads |
| **EO12** | *Watching, not a proof.* On every row above that shows the question | It waits for a click. If it ever closes by itself, or the chat reports `dialog_refused` or a Discard nobody chose, the job's dialog watch (Group EL) is answering Heron's own question and `HeronDialogWatch.HeronAsks()` is not standing it down - write down the row and what the chat said |
