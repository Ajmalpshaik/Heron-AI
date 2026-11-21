# Needs checking — Group PL

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group PL - a carried id list naming elements here and not here is refused whole, and a read whose answer was lost says it reached Revit - BUILT, NOT RUN IN REVIT (2026-11-21)

**Tracked as GitHub issue [#1204](https://github.com/Ajmalpshaik/Heron-AI/issues/1204) since 2026-11-21** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** issues [#933](https://github.com/Ajmalpshaik/Heron-AI/issues/933) (row 75, part (2)) and
[#932](https://github.com/Ajmalpshaik/Heron-AI/issues/932) (row 5b-1167, part (b)). Built in a cloud container with
no Revit: the add-in compiles for every release (`tools/check-compile.py`, the .NET 10 SDK), the binder's new rule
runs in `tests/Heron.BindingNote.TestHost` (`tests/test_binding_note.py`, which also reads `Remember` as text), and
the client's new answer runs in `tests/test_failure_analysis.py` with only the pipe replaced - **none of it has met a
Revit or a real pipe**. Rebuild the add-in and restart Revit first (`python tools/heron-ready.py`).

| Change | Files | Row | Checks |
|---|---|---|---|
| A list a step leaves as ElementIds is looked up in the model it was left in; some ids naming an element there and some not is recorded, and the next step that would bind it is refused `needs_unbound`, *"ids from more than one model: N of M ..."*, NOTHING WAS BOUND | `RevitFragment.cs` (`Remember`, `IsElementNumber`), `HeronBindingNote.cs` (`IdsFromElsewhere`, `CarriedFromElsewhere`) | [75](../fragment-issues/section-5-rows-051-075.md) | PL1 |
| A read that was sent and whose every answer was lost comes back `answer_lost`, not "did not answer"; a Revit never reached is still "did not answer" | `heron_bridge_client.py` (`request`, `lost_reply`), `heron_failure.py`, `batch-prove.py` | [5b-1167](../fragment-issues/section-5b-rows-1151-1175.md) | PL2 |

**The models: scratch copies, never a working model.** For PL1, a scratch copy of a project with at least one Revit
link loaded - Snowdon-scratch with *Snowdon Towers Sample Architectural.rvt* is the one row 75 was measured on - in
`%TEMP%\heron-work\pl`, pinned in the chat. The scratch cards below are written in `%TEMP%\heron-work\pl\cards`,
never in Heron's folder, and sent one after the other with `python mcp/client/heron_bridge_client.py fragment
<card> ...` from one terminal, so they share one chain. Read the document name on every reply.

| ID | Do this | Pass looks like |
|---|---|---|
| **PL1** | *A carried id list mixing a link's ids with this model's, positive* - scratch card A at `risk: READ` that provides `elements` as `IList<ElementId>`: every wall id of the loaded link (`GetLinkDocument()`, a collector of walls, each `.Id`) plus the id of one wall of this model; then scratch card B at `risk: READ` that needs `elements` (`IList<ElementId>`) and provides its count; send A, then B | B is refused `needs_unbound`: *"'elements' (IList<ElementId>) would be filled from A ..., and that list holds ids from more than one model: N of M named no element in the model they were left in, '<this model>' ..."* with NOTHING WAS BOUND, where M is the link's wall count plus one and N is M less the ids that happen to name something here; B never ran. Write down N and M. **Negative:** card A changed to leave this model's own wall ids only - B binds them and answers their count, as before; and card A leaving a list of built-in category ids only (`new ElementId(BuiltInCategory.OST_Walls)` and two more) - B binds them as before, never refused; and card A leaving the link's wall ids alone, with no host wall added - write down whether B was refused (it is, when one of the link's ids names an element here too - the case this exists for) or bound and answered, and what count it answered |
| **PL2** | *A read whose answer is lost, on a real pipe* - with a chat connected to the scratch model, run [OW1](group-ow.md)'s burst of twenty `count` calls at once, and read every reply; then close that Revit and run one `count` against its session | Every lost reply reads `answer_lost`: *"The request reached Revit - it was sent 3 times - and no answer came back, so whether it ran is not known from here"*, never *"did not answer"*; in a chat, `revit_read`'s line is OUTCOME UNKNOWN. Write down how many of the twenty were lost. **Negative:** against the closed Revit no attempt opens anything, so the reply is still *"did not answer"* / `no_reply` - never `answer_lost` - and a `batch-prove` job whose Revit stopped answering is still TIMEOUT |
