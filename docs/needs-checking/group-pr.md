# Needs checking — Group PR

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group PR - Ask to Delete: the switch off stops Revit's Delete box and a DRAFT's Keep or Discard - BUILT, NOT RUN IN REVIT (2026-11-23)

**Tracked as GitHub issue [#1276](https://github.com/Ajmalpshaik/Heron-AI/issues/1276) since 2026-11-23** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** [D-197](../decisions/D-197.md), asked for and accepted by the owner on 2026-11-23. A sixth switch, *Ask to
Delete*, in Heron's Settings. It is held in Revit's memory and on at every start. Off, Revit shows neither D-142's
Delete box nor D-119's Keep or Discard for a DRAFT's one run. The add-in compiles for Revit 2020 to 2027
(`tools/check-compile.py`), and `tests/test_ask_to_delete.py` holds the wiring as text. **None of it has met a
Revit.** Rebuild the add-in and restart Revit first (`python tools/heron-ready.py`). That needs Revit closed, so it
is the owner's step.

| Change | Files | Checks |
|---|---|---|
| The Ask to Delete row in Settings, its question on the way off, held in memory and on at Revit start | `Commands.cs` (`AskToDeleteToggleCommand`), `HeronPermissions.cs` | PR1, PR2 |
| Off: a kept change that deletes is kept with no Delete box and says so first | `RevitFragmentDelete.cs` (`DeleteGate`, `WithDeleteUnasked`), `RevitFragment.cs` | PR3, PR4 |
| Off: a DRAFT's one run is kept with no box, and its record says nobody was asked | `RevitFragment.cs` (`WithUnasked`), `heron_bridge_client.py` (`try-once`) | PR5 |
| `info` says `askToDelete`; `revit_health` and `heron_diagnose` print a line when it is off | `BridgeServer.cs`, `heron_health.py`, `heron_mcp_server.py`, `heron_diagnose.py` | PR6 |

**The models: scratch copies, never a working model.** A scratch project saved for it in `%TEMP%\heron-work\pr`,
for example a new project from the default template with four walls drawn by hand and saved. The DRAFT for PR5 is
any MODIFY DRAFT whose version has not been kept once.

| ID | Do this | Pass looks like |
|---|---|---|
| **PR1** | *The switch and its start.* Start Revit and open Heron's Settings. | Six rows. *Ask to Delete* reads ON with the line *"Revit asks before a delete, and before an unproven tool's first try is kept."* `revit_health` prints no Ask to Delete line. |
| **PR2** | *Off asks once; on asks nothing; a restart turns it on.* Press Ask to Delete and choose *Keep asking*, then press it again and choose *Stop asking*, then press it once more. Close Revit and start it again. | *Keep asking* leaves it ON and the log says `Ask to Delete toggle: offered, declined. Still on.` *Stop asking* sets it off (`Ask to Delete switch set to False`), and `revit_health` prints the OFF line. Pressing it again turns it on with no question. After the restart it is ON. |
| **PR3** | *On: the Delete box as before.* Changes on, Ask to Delete ON, Ask to Keep off. Select one wall and ask the chat to delete it through `revit_change` (`DELETE_ELEMENTS`). Press *Don't delete*. | The *"Delete 1 element?"* box comes up. *Don't delete* deletes nothing (`delete_declined`), as D-142 / Group HL. |
| **PR4** | *Off: no Delete box.* The same, with Ask to Delete OFF. | No box. The wall is gone and Ctrl+Z brings it back as one step. The reply prints *"Revit did not ask before deleting: Ask to Delete is off ..."* first, with `deleteAsked false`, `deletedUnasked 1`. The log says `Delete box: off (Ask to Delete) - 'delete-elements' deletes 1 element(s)`. **Negative:** with Ask to Keep ON as well, the Keep or Discard box still comes up. |
| **PR5** | *Off: a DRAFT's one run with no box.* Ask to Delete OFF. `python mcp/client/heron_bridge_client.py try-once <draft>` on the saved scratch project in front. | No box. The client prints *"NOBODY WAS ASKED"*. The reply has `asked false`, `askSkipped askToDeleteOff`, `kept true`. The banner reads *"Kept without asking - Ask to Delete off"*. The once record has `"asked": false`, and its positive phase says nobody looked. Ctrl+Z takes it back. **Negative:** Ask to Delete ON, a second DRAFT: the Keep or Discard box comes up as before. |
| **PR6** | *The OFF line, and only then.* With Ask to Delete OFF, run `revit_health`, then `heron_diagnose`. Turn it ON and run both again. | Off: both print *"Ask to Delete is OFF in this Revit right now ..."* under that Revit. On: neither prints an Ask to Delete line. |
