# Needs checking — Group HG

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-18 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group HG - the Ask to Keep switch, and one Keep per job: D-138 (2026-10-18)

[D-138](../decisions/D-138.md) (accepted by the owner 2026-10-18, by asking for it) supersedes
[D-126](../decisions/D-126.md) item 5 in part, from [row 5b-1101](../fragment-issues/section-5b-rows-401-425.md).
**What changed.** An *Ask to Keep* button beside Changes, Admin and Publish turns Revit's Keep or Discard
box on and off - it writes `write.askBeforeKeeping`, asks once on the way off and never on the way on,
and starts ON. While it is on, the box has a third link, *"Keep, and keep the rest of this job without
asking"*: it keeps the change, and the same chat's next changes to the same model are kept without the
box until ten minutes pass with none, another chat or model asks, the switch is turned off, the model
closes or Revit closes. A publish and a DRAFT's one run are always asked. Each change is its own undo
step, `Heron: <name>`. **The switch is OFF by default** - the owner's word the same day, *"make the box
off by default"* - and a value nobody can read is off on both sides.

**What is tested and not run.** It compiles for 2020 to 2027. The grant's rule - who holds it, on which
model, for how long, what ends it - is run without Revit by `tests/Heron.KeepQuestion.TestHost`
sections 7 to 10; the wiring is read as text by `tests/test_keep_question.py` sections 8 to 12. **Nothing
here has run in Revit.** Needs the add-in rebuilt from this change and deployed with Revit closed, Changes
ON, a saved test model, and one chat running a family-sized job of PROVEN `revit_change` calls.

| # | Run | Look for |
|---|---|---|
| **HG1** | *Off by default.* On a `heron.config` that does not name `write.askBeforeKeeping` - a new PC, or the line deleted - start Revit and ask the chat for one change - `CREATE_LEVEL`, say | The button reads `Ask to Keep off` on the red open padlock. The change: **no box**, kept, ONE undo entry `Heron: CREATE_LEVEL`. `python tools/heron-ready.py --check` lists Ask to Keep as off |
| **HG2** | *Switch on.* Press **Ask to Keep**, then ask for one change | No window on the way on. The button reads `Ask to Keep ON` on the blue closed padlock; `heron.config` holds `write.askBeforeKeeping = true`; the add-in log says *"Ask to Keep switch set to True from the ribbon."* The change puts **the box up**, with three links - Keep it, Discard it, and *Keep, and keep the rest of this job without asking* |
| **HG2a** | *Switch off again.* Press **Ask to Keep** once more and, in the window, press **Stop asking**; ask for one change | The window says what stops and that Ctrl+Z still takes each change back, with **Keep asking** beside **Stop asking**. The button reads `Ask to Keep off`; the file says `false`; the log says *"Ask to Keep switch set to False from the ribbon."* The change: **no box**, kept, one undo entry. **The D-30 negative:** press **Keep asking** instead the first time - the button and the file do not change, and the log says *"offered, declined. Still on."* |
| **HG3** | *The third link.* With the box up from HG2, press the third link; then ask the same chat for three more changes to the same model, within a few minutes of each other | The first change is kept and its reply says the modeller pressed KEEP, AND KEEP THE REST OF THIS JOB WITHOUT ASKING. The button reads `Keep: rest of job` on the red padlock. The next three: **no box**, each kept, and each reply's verdict begins *"Kept WITHOUT ASKING the modeller, under the grant they gave in Revit at HH:MM"* with the time of the click; the activity banner ends each with *"Kept without asking - job allowed at HH:MM"*. **The D-30 negative:** a Discard of the HG2 box gives no grant - the next change puts the box up |
| **HG4** | *Ten idle minutes.* After HG3, send nothing for more than ten minutes, then ask the same chat for one more change | Within about 15 seconds of the ten minutes the button goes back to `Ask to Keep ON`, and the add-in log says the grant *"ended - 10 minutes passed with no change from that chat"*. The change puts **the box up**, and the box says the grant ended because ten minutes passed |
| **HG5** | *Another chat.* Give a grant (HG3) in chat A; close chat A, wait for its lease to lapse, and ask a change from chat B on the same model | Chat B's change puts **the box up** and says the grant ended because the change comes from another chat; the log says so. Chat A, back again, is asked too |
| **HG6** | *A publish.* With a grant held (HG3) and Publish ON, ask the same chat for an export (a schedule to CSV, say) | **The box is up**, with only Keep it and Discard it, and it says the grant still stands but an export, a save or a sync is always asked. The change after it, from the same chat, is kept with no box |
| **HG7** | *A DRAFT's try-once.* With a grant held (HG3), on the saved model in front, run a DRAFT fragment once - `python mcp/client/heron_bridge_client.py try-once <draft-fragment>` (D-119), with `HERON_CLIENT_ID` set to that chat's id so it is the same chat | **The box is up**, headed *"... has never been proved. Keep what it did?"*, with no third link, saying a tool that has never been proved is always asked |
| **HG8** | *Ctrl+Z.* After HG3's four kept changes, press Ctrl+Z four times, looking at the undo list before each | Four separate entries, each `Heron: <name>`; each Ctrl+Z takes back exactly one change, newest first - never two at once, never the whole job |
| **HG9** | *The model closes.* With a grant held, close that model and open it again, then ask one change | The log says the grant ended because its model closed; the button reads `Ask to Keep ON`; the change puts **the box up** |
| **HG10** | *The window fails to draw.* Not arranged on purpose - noted if it happens on any press | The plain Revit question appears instead, Enter landing on **Keep asking**; the add-in log names why the window did not open |
