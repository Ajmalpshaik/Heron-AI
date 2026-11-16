# Needs checking — Group EN

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group EN - a big build is saved first: step 0 of `building-shell` and `drawing-set`, the family method's closing line, and the one measurement `Q-69` asks for (2026-10-17)

On 2026-10-13 and 2026-10-14 a house with 9 sheets and a 32-floor tower were built in models that had
never been saved, and Revit was closed: both were lost. Heron cannot save the model it works in -
`SAVE_DOCUMENT` refuses it, because Heron's one Undo entry is open in it for the whole run.
[Row 5b-513](../fragment-issues/section-5b-rows-401-425.md) has the whole of it, and
[OPEN-QUESTIONS Q-69](../open-questions/tier-2.md) the proposal for the owner.

**What changed.** No fragment and nothing in the add-in. `building-shell` and `drawing-set` have a step 0:
read `REPORT_OPEN_DOCUMENTS`, and when it says the model has NEVER BEEN SAVED, ask the modeller once to
save it before step 1. `family-creation`'s closing paragraph says a big build in a family never saved is
saved first, as its step 0 already asks ([Group EI](group-ei.md) EI8 watches that one).

**What is run and what is not.** Nothing here has run in Revit. Rows EN1 to EN3 are *watching, not a
proof*: a method's step is DRAFT until a real chat is seen to take it. EN4 and EN5 are measurements
for Q-69 and are run **only on the owner's word**.

**2026-11-16 - Q-69 answered, and built.** Ajmal PS chose Shape 2 that day (*"go with you suggetions"*):
the add-in saves the model it works in after the transaction group closes, only when the change was kept and
the modeller asked, and a Save As goes only to a path the modeller typed. `SAVE_DOCUMENT` version 3 asks
for it through the ambient need `saveWhenKept`; [`RevitFragmentSave.cs`](../../revit/Heron.Revit.Addin/RevitFragmentSave.cs)
does it; the chat's pin follows a Save As this run did ([`heron_write.py`](../../mcp/server/heron_write.py)
`after_save`). Compiled for Revit 2020 to 2027 and tested in a cloud container (`tests/test_save_after_group.py`,
`tests/test_document_pin.py`, `tests/test_fragment_ambient.py`) - **NOT RUN IN REVIT.** The card is DRAFT again,
so `revit_change` refuses a kept run of it (D-117) and `try-once` takes only MODIFY: **EN6, EN8 and EN9's kept
halves need the owner's word on a route first** (Q-69's Work note). EN7 and EN10 run today, rolled back, with
the Publish switch on. A rolled-back run of EN4 and EN5 is no longer needed to decide Q-69; they stay as the
measurement of a save from the callback.

**SCRATCH ONLY, never a working model.** A new project from the default metric template, left unsaved.

| # | Run | Look for |
|---|---|---|
| **EN1** | *Watching, not a proof.* The unsaved scratch project in front; ask a chat to build a small shell - two levels and four walls - so that it takes `heron_method("building-shell")` | Before step 1 it reads `REPORT_OPEN_DOCUMENTS`, sees NEVER BEEN SAVED, and asks ONCE for Ctrl+S under a name of the modeller's own; it builds only after the file exists, and does not ask again |
| **EN2** | EN1's negative: the same request in the same project once it HAS been saved | No ask at all - straight to step 1. A step 0 that asks every time is the failure |
| **EN3** | *Watching, not a proof.* A second never-saved scratch project with a few walls on two levels drawn by hand; ask a chat for a two-sheet drawing set, so that it takes `heron_method("drawing-set")`. Then the same request once that project is saved | The same ask before step 1 while it has no file; none once it has |
| **EN4** | *A measurement for Q-69, on the owner's word only.* A chat reads the never-saved scratch project once (`revit_health`), so its pin holds that model. Then a scratch fragment, run by the proving client on the READ path - no transaction and no group anywhere - calls `SaveAs` on that project to a new path under `%TEMP%\heron-work`, in Revit 2020. Then the chat is asked to add one level | Whether Revit lets an ExternalEvent callback save at all: the file on disk and its size, the title bar's new name, any dialog Revit raises. And **write down the chat's answer to the level**: on 2020 its pin holds only the old title, so Q-69 predicts a refusal - the title it pinned is no longer open - and no level |
| **EN5** | EN4 on Revit 2024, where the model's own id exists | The same save; and whether the level goes in. Q-69 predicts the pin follows the rename on 2024, matched on `creationGuid`, once a reply under the new title has been read - write down whether the first call after the save is refused anyway |
| **EN6** | *`SAVE_DOCUMENT` v3, a save after a KEPT change - on the owner's word, by the route he names (the card is DRAFT).* Changes and Publish on, Ask to Keep off. A SAVED scratch project with one wall drawn since its last save, in front, the chat pinned to it. `SAVE_DOCUMENT documentPath=<its file>`, kept. Then the same on a workshared LOCAL copy of a scratch central | The reply's `save` block `done true`, its words saying a save cannot be undone; the file's time on disk moves to now and the title bar's unsaved mark goes; **Revit raised no dialog and the group's one Undo entry is there** - press Ctrl+Z and the wall goes while the file keeps it. On the local copy: *"NOTHING WENT TO CENTRAL"*, and a second user's Reload Latest does NOT show the wall. Write down how long the save took and the reply's `undoState` (D-157), which is read before the save |
| **EN7** | *Nothing is saved after a Discard or a preview - runs today, rolled back.* EN6's project, a second wall unsaved. (a) `heron_bridge_client.py fragment save-document --write --set documentPath=<its file>`, no `--apply` (row 5b-1063's command). (b) With Ask to Keep on, the same kept from a chat once the card is proved, and **Discard** pressed | (a) and (b): `saveAsked true` and the `save` block `done false`, saying the run was rolled back - or that Discard was pressed - and nothing was saved; **the file's time on disk UNCHANGED**, the unsaved mark still there. (b) also: write down the box's words - it says a publish's save *"is done already"*, which is untrue here ([row 5b-1824](../fragment-issues/row-5b-1824.md)) |
| **EN8** | *A Save As keeps the chat on the model - the kept half on the owner's word, as EN6.* A NEVER-saved scratch project, Heron's chat pinned to it, in **Revit 2020** (no `creationGuid`). `SAVE_DOCUMENT saveAsPath=%TEMP%\heron-work\EN8.rvt`, kept. Then ask the chat to add one level. Then the same on Revit 2024 | The file is at that path; Revit's title is `EN8`; the reply says the chat now works on `EN8` at that path, *"the same model under its new name"*. **The level goes in with no "use this model"** on both releases - the refusal EN4 predicted for 2020 is what this removes. Write down any dialog Revit raised during the Save As |
| **EN9** | *A Save As goes only where the modeller said - the refusals run today, rolled back.* The never-saved project: `saveAsPath` = (a) the path of a file already there, no `replaceFile`; (b) a relative path; (c) a folder that does not exist; (d) a `.txt`. Then (e) a project that HAS a file, with `saveAsPath` set. Kept half, on the owner's word: (a) again with `replaceFile=true` | (a) to (e) each refused in its own words, `saveAsked false`, no file written or touched - (a)'s file time unchanged. The kept (a) with `replaceFile=true`: the file replaced, and the save block saying it REPLACED it |
| **EN10** | *Nothing here syncs - runs today, rolled back.* A workshared scratch CENTRAL model opened directly (not a local copy), `SAVE_DOCUMENT documentPath=<its file>`; and a never-saved project with worksharing enabled, `saveAsPath` given | Both refused before anything is asked - the central one naming *"the CENTRAL model itself"*; no Synchronize with Central in Revit's own journal, and the central file's time unchanged |
