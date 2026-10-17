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

**SCRATCH ONLY, never a working model.** A new project from the default metric template, left unsaved.

| # | Run | Look for |
|---|---|---|
| **EN1** | *Watching, not a proof.* The unsaved scratch project in front; ask a chat to build a small shell - two levels and four walls - so that it takes `heron_method("building-shell")` | Before step 1 it reads `REPORT_OPEN_DOCUMENTS`, sees NEVER BEEN SAVED, and asks ONCE for Ctrl+S under a name of the modeller's own; it builds only after the file exists, and does not ask again |
| **EN2** | EN1's negative: the same request in the same project once it HAS been saved | No ask at all - straight to step 1. A step 0 that asks every time is the failure |
| **EN3** | *Watching, not a proof.* A second never-saved scratch project with a few walls on two levels drawn by hand; ask a chat for a two-sheet drawing set, so that it takes `heron_method("drawing-set")`. Then the same request once that project is saved | The same ask before step 1 while it has no file; none once it has |
| **EN4** | *A measurement for Q-69, on the owner's word only.* A chat reads the never-saved scratch project once (`revit_health`), so its pin holds that model. Then a scratch fragment, run by the proving client on the READ path - no transaction and no group anywhere - calls `SaveAs` on that project to a new path under `%TEMP%\heron-work`, in Revit 2020. Then the chat is asked to add one level | Whether Revit lets an ExternalEvent callback save at all: the file on disk and its size, the title bar's new name, any dialog Revit raises. And **write down the chat's answer to the level**: on 2020 its pin holds only the old title, so Q-69 predicts a refusal - the title it pinned is no longer open - and no level |
| **EN5** | EN4 on Revit 2024, where the model's own id exists | The same save; and whether the level goes in. Q-69 predicts the pin follows the rename on 2024, matched on `creationGuid`, once a reply under the new title has been read - write down whether the first call after the save is refused anyway |
