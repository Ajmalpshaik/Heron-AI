# Needs checking — Group EN

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group EN - a big build is saved first: step 0 of `building-shell` and `drawing-set`, the family method's closing line, and the one measurement `Q-69` asks for (2026-10-17)

**Tracked as GitHub issue [#970](https://github.com/Ajmalpshaik/Heron-AI/issues/970) since 2026-11-16** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

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

**2026-11-16 - Q-69 answered, and built.** Ajmal PS chose Shape 2 that day (*"go with you suggetions"*),
recorded as [D-171](../decisions/D-171.md): the add-in saves the model it works in after the transaction
group closes, only when the modeller asked, and a Save As goes only to a path the modeller typed.
`SAVE_DOCUMENT` version 3 asks for it through the ambient need `saveWhenKept`;
[`RevitFragmentSave.cs`](../../revit/Heron.Revit.Addin/RevitFragmentSave.cs) does it; the chat's pin follows a
Save As this run did ([`heron_write.py`](../../mcp/server/heron_write.py) `after_save`). **A PREVIEW SAVES TOO**
(D-171 point 6): asked the same day whether a preview of *"save my model"* should really save, he answered
*"okkey"* - only when the save was asked for, and the reply says a save cannot be undone. So a preview's change
is rolled back and its save is not, and the reply says both; a **Discard** saves nothing (D-171 point 5).
Compiled for Revit 2020 to 2027 and tested in a cloud container (`tests/test_save_after_group.py`,
`tests/test_document_pin.py`, `tests/test_fragment_ambient.py`) - **NOT RUN IN REVIT.** The card is DRAFT again,
so `revit_change` refuses it (D-117) - but **EN6, EN8, EN9 and EN10 run TODAY as previews, on a scratch model**,
through the proving client with the Publish switch on and Changes on:
`heron_bridge_client.py fragment save-document --write --set ...` with no `--apply` (row 5b-1063's command),
or `validate save-document --write --allow-publish` for the proof itself; both print the `save` block under
the verdict. EN7's Discard waits for the card to be proved. EN6 and EN8's chat halves - a kept change through
`revit_change`, the next call after a Save As - wait for that too. A rolled-back run of EN4 and EN5 is no longer
needed to decide Q-69; they stay as the measurement of a save from the callback.

**2026-11-16 - row 5b-1824, fixed in code.** `SAVE_DOCUMENT`'s other route - a save of ANOTHER open document,
a nested family's own window, and the close of that window - ran inside the run, before Revit's Keep or Discard
box, so a Discard still wrote that file. It goes the same way as the model Heron works in now: the card asks
through a second ambient need, `saveOtherWhenKept`, and writes and closes nothing itself;
[`RevitFragmentSave.cs`](../../revit/Heron.Revit.Addin/RevitFragmentSave.cs) `SaveOtherNow` saves it after the
group by the same rule - a preview that asked saves, a Discard saves nothing, a rollback that did not hold saves
nothing - and then closes its window when asked, never the window in front. And the box tells a publish whose
save waits for Keep *"NOTHING IS SAVED YET"*, not that its save *"is done already"*
([`HeronKeepQuestion.cs`](../../revit/Heron.Revit.Addin/HeronKeepQuestion.cs) `WaitsForKeep`). Compiled for Revit
2020 to 2027 and tested in a cloud container (`tests/test_save_after_group.py`, `tests/test_keep_question.py` and
its host, `tests/test_fragment_ambient.py`) - **NOT RUN IN REVIT.** EN11 and EN12 run TODAY as previews; EN13
waits.

**SCRATCH ONLY, never a working model.** A new project from the default metric template, left unsaved.

| # | Run | Look for |
|---|---|---|
| **EN1** | *Watching, not a proof.* The unsaved scratch project in front; ask a chat to build a small shell - two levels and four walls - so that it takes `heron_method("building-shell")` | Before step 1 it reads `REPORT_OPEN_DOCUMENTS`, sees NEVER BEEN SAVED, and asks ONCE for Ctrl+S under a name of the modeller's own; it builds only after the file exists, and does not ask again |
| **EN2** | EN1's negative: the same request in the same project once it HAS been saved | No ask at all - straight to step 1. A step 0 that asks every time is the failure |
| **EN3** | *Watching, not a proof.* A second never-saved scratch project with a few walls on two levels drawn by hand; ask a chat for a two-sheet drawing set, so that it takes `heron_method("drawing-set")`. Then the same request once that project is saved | The same ask before step 1 while it has no file; none once it has |
| **EN4** | *A measurement for Q-69, on the owner's word only.* A chat reads the never-saved scratch project once (`revit_health`), so its pin holds that model. Then a scratch fragment, run by the proving client on the READ path - no transaction and no group anywhere - calls `SaveAs` on that project to a new path under `%TEMP%\heron-work`, in Revit 2020. Then the chat is asked to add one level | Whether Revit lets an ExternalEvent callback save at all: the file on disk and its size, the title bar's new name, any dialog Revit raises. And **write down the chat's answer to the level**: on 2020 its pin holds only the old title, so Q-69 predicts a refusal - the title it pinned is no longer open - and no level |
| **EN5** | EN4 on Revit 2024, where the model's own id exists | The same save; and whether the level goes in. Q-69 predicts the pin follows the rename on 2024, matched on `creationGuid`, once a reply under the new title has been read - write down whether the first call after the save is refused anyway |
| **EN6** | *`SAVE_DOCUMENT` v3, a save on a PREVIEW - runs today, on a scratch model (D-171).* Changes and Publish on. A SAVED scratch project with one wall drawn by hand since its last save, in front. (a) `heron_bridge_client.py fragment save-document --write --set documentPath=<its file>`, no `--apply`. (b) The same on a workshared LOCAL copy of a scratch central. (c) Once the card is proved: the same through a chat's `revit_change`, kept, Ask to Keep off | (a): the verdict ROLLED BACK and, under it, **SAVED** - the `save` block `done true`, `preview true`, its words saying the change was rolled back AND the save was done, and that a save cannot be undone; the file's time on disk moves to now and the title bar's unsaved mark goes; reopened, the file holds the hand-drawn wall. **Write down any dialog Revit raised** and how long the save took. (b): *"NOTHING WENT TO CENTRAL"*, and a second user's Reload Latest does NOT show the wall. (c): `preview false`, the group's one Undo entry there - Ctrl+Z takes the change back while the file keeps it; and the reply's `undoState` (D-157), read before the save |
| **EN7** | *Nothing is saved without a save asked for, or after a Discard.* EN6's project, a second wall unsaved. (a) Runs today: a preview of a request that asks for no save - `documentPath` of the project after it has just been saved, so it has nothing unsaved. (b) Once the card is proved: with Ask to Keep on, the same as EN6 (c) kept from a chat, and **Discard** pressed | (a): `saveAsked false`, NO `save` block and no SAVED line; the file's time on disk UNCHANGED. (b): the `save` block `done false`, saying Discard was pressed and a save follows only a Keep (D-171 point 5); **the file's time on disk UNCHANGED**, the unsaved mark still there. (b) also: write down the box's words - since [row 5b-1824](../fragment-issues/row-5b-1824.md) it says *"NOTHING IS SAVED YET. Keep keeps the change, then saves"* the model's file, never that a publish's save *"is done already"* |
| **EN8** | *A Save As on a PREVIEW - runs today, on a scratch model; the chat half once the card is proved.* A NEVER-saved scratch project in front, in **Revit 2020** (no `creationGuid`). (a) `fragment save-document --write --set saveAsPath=%TEMP%\heron-work\EN8.rvt`, no `--apply`. Then the same on Revit 2024. (b) Once proved: a chat pinned to a never-saved project asks for the same Save As, kept, then to add one level | (a): the file is at that path; Revit's title is `EN8`; the `save` block names `EN8` and that file, `preview true`, and says the change was rolled back and the save done. Write down any dialog Revit raised during the Save As. (b): the reply says the chat now works on `EN8` at that path, *"the same model under its new name"*, and **the level goes in with no "use this model"** on both releases - the refusal EN4 predicted for 2020 is what this removes |
| **EN9** | *A Save As goes only where the modeller said - runs today, as previews.* The never-saved project: `saveAsPath` = (a) the path of a file already there, no `replaceFile`; (b) a relative path; (c) a folder that does not exist; (d) a `.txt`. Then (e) a project that HAS a file, with `saveAsPath` set. Then (f) (a) again with `replaceFile=true` | (a) to (e) each refused in its own words, `saveAsked false`, no `save` block, no file written or touched - (a)'s file time unchanged. (f): the file replaced - a preview saves - and the save block saying it REPLACED it |
| **EN10** | *Nothing here syncs - runs today, rolled back.* A workshared scratch CENTRAL model opened directly (not a local copy), `SAVE_DOCUMENT documentPath=<its file>`; and a never-saved project with worksharing enabled, `saveAsPath` given | Both refused before anything is asked - the central one naming *"the CENTRAL model itself"*; no Synchronize with Central in Revit's own journal, and the central file's time unchanged |
| **EN11** | *`SAVE_DOCUMENT` v3, ANOTHER open document saved AFTER the group, on a PREVIEW - runs today, on scratch families ([row 5b-1824](../fragment-issues/row-5b-1824.md)).* Changes and Publish on. A scratch host family in front, Heron working in it, and a scratch part family open BEHIND it with one form added since it was saved - [Group CS](group-cs.md)'s `HeronNest_Car` and `HeronNest_Hub` will do. (a) `heron_bridge_client.py fragment save-document --write --set documentPath=<the part's file>`, no `--apply`. (b) Another form added to the part, then the same with `--set closeWindow=true`. (c) (b) again at once, with nothing unsaved in the part | (a): the verdict ROLLED BACK and, under it, **SAVED** - the `save` block `kind saveOther`, `done true`, `preview true`, its words saying the change was rolled back AND the save was done, and that a save cannot be undone; `saveAsked true`, `closeAsked false`; the part's file time on disk moves to now and the host's does not. (b): the block `closed true`, *"Its window is closed"*; `REPORT_OPEN_DOCUMENTS` no longer lists the part; then `LOAD_FAMILY` into the host brings in both forms - `family-creation` step 11 f and g. **Write down any dialog Revit raised during the save or the close, and whether the reply came back at all** - the close is the first Heron makes after its group; CS2's was inside the run. (c): `saveAsked false`, `closeAsked true`, the block `nothingToSave true` and `closed true`; the file's time unchanged |
| **EN12** | *The rules read again after the group - runs today, as previews (row 5b-1824).* EN11's two families. (a) The part IN FRONT, Heron working in the host behind it, one form unsaved, `closeWindow=true`. (b) A workshared LOCAL copy of a scratch central open behind the host, one wall unsaved, `documentPath` its file | (a): `closeAsked false` and the finding saying Revit lets no tool close the window in front; the `save` block `done true`, `closed false`; the part's file time moves, its window is still open and Revit still running. (b): `done true` and *"NOTHING WENT TO CENTRAL"* in the block's words; a second user's Reload Latest does NOT show the wall |
| **EN13** | *A Discard saves and closes NEITHER document, and Revit's box says what waits for Keep - waits for the card to be proved, Ask to Keep on, AND for a run that changes the model and asks a save in one group:* Revit shows its box only for a change it counts, and `SAVE_DOCUMENT` alone changes nothing ([row 5b-1834](../fragment-issues/row-5b-1834.md)). EN11's part behind the host, one form unsaved, `closeWindow=true`; Discard pressed. Then the same with Keep | The box says *"NOTHING IS SAVED YET. Keep keeps the change, then saves"* the part's file *"and closes its window"* and *"saves nothing and closes nothing"* for Discard - never *"is done already"*. Discard: the `save` block `kind saveOther`, `done false`, `closed false`, saying Discard was pressed and *"Its window was left open"*; the part's file time UNCHANGED, the part still open with its unsaved form. Keep: `done true`, `closed true`, `preview false` |
