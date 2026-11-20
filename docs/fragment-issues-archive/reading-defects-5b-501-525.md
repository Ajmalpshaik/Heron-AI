# FRAGMENT-ISSUES archive — section 5b, rows 501 to 525

> **Finished rows, moved out of the live register.** These are rows from section 5b of
> [`FRAGMENT-ISSUES.md`](../FRAGMENT-ISSUES.md) — Heron's own defects found by reading the repository — whose state said they were
> finished when they were moved. Each still has its line in the register: the same number, a
> one-line title, the opening of its state, and a link to its full text here.
>
> **Nothing below was rewritten** except its links, which were re-pointed one folder deeper and
> each checked to reach the same file it reached before. Where a row disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win** — a row records what was true on
> the day it was written.
>
> **Nothing new is written here.** A new defect goes in the register, and a row here that turns
> out not to be finished is reopened **there**, on its own line.
>
> Rows 501 to 525 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-501

*Moved from the register on 2026-11-17.*

**Defect.** **FOUR SENTENCES OF `tests/test_routing_phrases.py` REACHED THE WRONG TOOL ON `main`, AND NOTHING IN CI COULD SEE IT.** The suite runs only on the trained model (`model2vec`), which CI does not install, so it exits 3 there - and on 2026-10-17, with `minishlab/potion-base-8M` installed in a cloud container, it failed four checks on `main` at 721ec402: *"change the area scheme name from rentable to lettable"* reached `CREATE_AREA_PLAN`, *"line the viewports up"* reached `REPORT_VIEWPORT_TYPES`, and *"is project north rotated from true north"* - a question - reached `SET_PROJECT_LOCATION`, an ADMIN write (twice, in two tables). Two were coin tosses the PROVEN status nudge settled for the wrong card, after `REPORT_LOCATION` went to DRAFT at version 4 and `CREATE_AREA_PLAN` arrived; the third was a new read whose purpose said *"line"* three times.

**State.** **FIXED 2026-10-17, measured on the trained model with no re-ranker - NOT on the owner's PC, which has one.** Real phrasings added to the cards that answer, none of them a sentence of the suite word for word (which the suite checks): `REPORT_LOCATION` *"is project north the same as true north"* and *"has project north been rotated"*; `SET_AREA_VOLUME_COMPUTATIONS` *"change the area scheme name"*; `ALIGN_VIEWPORTS_ACROSS_SHEETS` *"line the viewports up on every sheet"*, and its purpose now opens with what it does - *"Lines the viewports up across sheets"*. `REPORT_VIEWPORT_TYPES`' purpose names no type and not the word *"line"* (a comment above it says why), and its own phrasings still reach it. No utterance was taken away from any card. With these, the suite passes; `tools/check-routing.py` and `tools/check-intrusion.py` exit 0 on the spelling backend. **Not measured:** the same sentences with the owner's re-ranker installed - `python tests/test_routing_phrases.py` on his PC is the check.

---

### Row 5b-511

*Moved from the register on 2026-11-17.*

**Defect.** **`heron_validate.py plan` PRINTED THE FIRST LETTER OF A SECOND ROUTE AS THE ROUTE - *"A"* FOR `select-by-parameter-value`, *"R"* FOR `set-sheet-title-block` - FOR EVERY CASE FILE THAT WRITES `second_route:` AS ONE SENTENCE RATHER THAN A LIST.** Found 2026-10-17 in a cloud container, preparing [NEEDS-CHECKING Group EQ](../needs-checking/group-eq.md), whose second-route row tells the person at the PC to read these routes from `plan`. `declared_second_route` walked `cases.get("second_route")` row by row; a folded `>` scalar is a string, so each row was one character and the first was kept. Measured that day: 74 of the 569 `brain/fragments/*/tests/cases.yaml` write it as a sentence. Nothing was proved wrong by it - the route is a person's to run, and a draft never prints it - but a person handed *"A"* has no route at all.

**State.** **FIXED 2026-10-17**: a string is one route, whole. [`tests/test_validate_agent.py`](../../tests/test_validate_agent.py) `test_a_second_route_written_as_one_sentence` fails without the fix and passes with it. `declared_negative` reads a string `negative:` as no case rather than a wrong one, and was left alone

---

### Row 5b-513

*Moved from the register on 2026-11-17.*

**Defect.** **HERON CANNOT SAVE THE MODEL IT IS WORKING IN, AND TWICE IN TWO DAYS A WHOLE BUILD WAS LOST BECAUSE THAT MODEL HAD NEVER BEEN SAVED.** On 2026-10-13 and 2026-10-14 a house with 9 sheets and a 32-floor tower were built in models that had never been saved, and Revit was closed: both were lost. `building-shell` and `drawing-set` each listed *"a new model has been saved under a name before the first step"* as a precondition - a state to be true, served before the steps, and not an act any step performed - so nothing asked the modeller. Nor could Heron have saved either: `SAVE_DOCUMENT` version 2 refuses the document it works in by name ([row 365](../fragment-issues/section-5b-rows-351-375.md)), because the add-in's write path keeps ONE `TransactionGroup` open in that document for the whole run (`RevitFragment.cs` line 706) and Revit saves nothing while one is open (RevitAPI.xml, `Document.Save` and `SaveAs`). **Read from the add-in and the server, 2026-10-17 - NOT RUN.** In the same request, once the group is closed (line 900) and before the reply is built (line 916), nothing of Heron's is open, so the add-in could save there on a fragment's behalf. Three things stop it being built as a widening alone: it is a new kind of act for the add-in - work of its own after Heron's Undo entry closes, where each earlier ambient need came with a decision ([D-112](../DECISIONS.md), [D-114](../DECISIONS.md)); a save cannot be rolled back, so [D-55](../DECISIONS.md)'s preview needs a word on it - version 2 already writes its other document on a preview; and a Save As renames the model, which the chat's pin reads as a DIFFERENT model on Revit 2020 to 2023 and for every family, where there is no `CreationGUID` (`RevitOperations.cs` line 664) and an unsaved model has no path - so `revit_change` would answer *"Nothing has been sent to Revit"* about a model it had just saved, and aim the next call at the old title (`heron_mcp_server.py` lines 1921 and 1455; `heron_write.py` line 461).

**State.** **ANSWERED 2026-11-16 - [Q-69](../open-questions/tier-2.md) was answered by the owner that day as [D-171](../DECISIONS.md): Heron may save the model it works in once its Undo entry has closed, only when the modeller asks, and never picks a name itself. Built as `SAVE_DOCUMENT` version 3 (DRAFT, the add-in saving after the group closes); its Revit run is owed in [Group EN](../needs-checking/group-en.md). The owner confirmed it settled on [#911](https://github.com/Ajmalpshaik/Heron-AI/issues/911), 2026-11-19; written here 2026-11-20.** Tracked as issue [#911](https://github.com/Ajmalpshaik/Heron-AI/issues/911) until then. Was: **OPEN - A QUESTION FOR THE OWNER, NOT BUILT.** [OPEN-QUESTIONS Q-69](../open-questions/tier-2.md) holds the proposal - `SAVE_DOCUMENT` version 3 with the add-in saving once the group has closed, a preview that saves too (the only way to prove it: [D-117](../DECISIONS.md) refuses a DRAFT kept for real, and [D-119](../DECISIONS.md)'s `try-once` runs only on a model with nothing unsaved), and a pin that accepts a new title only when the reply says this run did the Save As - the evidence above, and three shapes for him to choose between. Nothing Heron does at runtime changed. **The methods ask first, 2026-10-17, DRAFT like every step of them.** `building-shell` and `drawing-set` have a step 0: `REPORT_OPEN_DOCUMENTS` (version 2 says NEVER BEEN SAVED of a project too), and when the model has no file, ask the modeller ONCE to save it, Ctrl+S under a name of their own, before step 1 - Heron cannot. Each adds that capability to its list. `family-creation`'s closing paragraph says a big build in a family never saved is saved FIRST, as step 0 asks (row 487's line, untouched). Served whole by `heron_method`; a skill's text is indexed by no search, so the lines move no routing. Watching them, and the one measurement Q-69 asks for, are NEEDS-CHECKING [Group EN](../needs-checking/group-en.md).

---
