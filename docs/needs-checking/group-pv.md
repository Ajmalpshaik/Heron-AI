# Needs checking — Group PV

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group PV - a family just saved reads as unsaved: what in Revit marks it changed, and the refusal that now names the file's time - BUILT, NOT RUN IN REVIT (2026-11-23)

**Tracked as GitHub issue [#1291](https://github.com/Ajmalpshaik/Heron-AI/issues/1291) since 2026-11-23** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** [row 5b-1999](../fragment-issues/row-5b-1999.md). On 2026-11-23 a family saved with Ctrl+S was refused
`ask_needs_saved_model` by `try-once`, and a second save let it run. The code was traced in a cloud container:
the refusal reads Revit's own `Document.IsModified`, and no Heron code in that sequence opens a transaction or sets
the flag. What in Revit sets it is not known. The refusal now names the file's last write and says a second save is
the fix. The add-in compiles for Revit 2020 to 2027 (`tools/check-compile.py`); the words are checked without Revit
(`tests/test_keep_question.py` section 17). **None of it has met a Revit.** Rebuild the add-in and restart Revit
first (`python tools/heron-ready.py`). That needs Revit closed, so it is the owner's step. **LOCAL work.**

| Change | Files | Checks |
|---|---|---|
| `ask_needs_saved_model` names the file's last write and a second save | `RevitFragment.cs` (`AskRefusal`), `HeronKeepQuestion.cs` (`UnsavedRefusal`, `When`) | PV2, PV3 |
| Nothing changed - what in Revit marks a saved family changed | none | PV1 |

**The models: scratch copies, never a working model.** A new family from Revit 2020's *Generic Model* template,
saved in `%TEMP%\heron-work\pv`. Keep Revit's journal for the session (`%LOCALAPPDATA%\Autodesk\Revit\Autodesk
Revit 2020\Journals`) and Heron's add-in log.

| ID | Do this | Pass looks like |
|---|---|---|
| **PV1** | *What sets the flag.* In the scratch family add, by hand in Family Types, a Length instance parameter `Test_Width` = 600 and a Length instance parameter `Blade_Start` with formula `-Test_Width / 2`. Ctrl+S. Touch nothing, then run `report-open-documents` through `revit_read`. **Negative:** a second scratch family with `Test_Width` only and no formula, the same steps. | Record, for each family, whether it reads `[unsaved changes]` straight after Ctrl+S, and the journal lines between the save and the read. **Either answer is a result:** formulas re-evaluated after a save mark the family changed if the formula family reads changed and the negative does not; if neither does, repeat with the same parameters added by a kept `revit_change` instead of by hand, and record that. The read must answer, not refuse `read_changed_model`. |
| **PV2** | *The new words, on a family Revit marked changed.* Rebuilt add-in. On a family that reads `[unsaved changes]` straight after a save (PV1's if it does; otherwise change one value in Family Types and do not save), `python mcp/client/heron_bridge_client.py try-once <any MODIFY DRAFT>` with that family in front. Then Ctrl+S once more and run it again. | Refused `ask_needs_saved_model`: *"Revit marked this document changed after its last save (file written HH:MM, N min ago)"* - HH:MM the same as the file's *Date modified* in Explorer - then *"Nothing was run. Save the model, then ask again."* and *"save it once more and ask again."* Undo list unchanged. After the second save the run goes ahead to Keep or Discard (press Discard). |
| **PV3** | *Negative: no file, no time.* A new family never saved, in front, and the same `try-once`. | Refused `ask_needs_saved_model`: *"has never been saved"*, with no *"file written"* and no *"once more"*. Nothing was run. |
