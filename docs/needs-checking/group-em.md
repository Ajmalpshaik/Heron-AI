# Needs checking — Group EM

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group EM - a DRAFT run once on the live model, behind Keep or Discard: `"apply": "ask"` and `try-once` (2026-10-17)

Built for [D-119 to D-122](../DECISIONS.md#d-119--a-draft-fragment-may-run-once-on-the-model-in-front-behind-a-keep-or-discard-prompt-in-revit-and-article-11-says-so),
all four **accepted by the owner on 2026-10-17** - this group is run once the add-in carrying them is
deployed.

**What changed.** `run_fragment_write` takes `"apply": "ask"`
([`RevitFragment.cs`](../../revit/Heron.Revit.Addin/RevitFragment.cs)): refused unless the model is in front,
saved and with nothing unsaved, and only on the change path; otherwise the fragment runs inside Heron's
one `TransactionGroup`, the view is redrawn, and a box asks **Keep it** or **Discard it** - Discard the
default, Escape and the close button discarding too. Keep assimilates the group into one undo entry named
`Heron: <fragment>`; anything else rolls it back. The dispatcher waits `revit.askTimeoutSeconds` (300)
more for a job that put the box up ([`RevitDispatcher.cs`](../../revit/Heron.Revit.Addin/RevitDispatcher.cs)).
`heron_bridge_client.py try-once <fragment>` sends it, only for a DRAFT at risk MODIFY, and records the run
at `brain/proof-drafts/runs/<fragment>.once.json`; `validate <fragment> --write --kept` runs only the
negative and puts the kept run in the positive's place, and `heron_validate` refuses a kept phase that was
discarded or whose code moved.

**What is compiled and what is not run.** The add-in builds for all eight releases 2020 to 2027 and every
Revit member it calls exists in all eight (`tools/check-compile.py`, `tools/check-api-surface.py`,
2026-10-17). `tests/test_try_once.py` runs the client and the brain against a Revit that is not there, and
reads the add-in as text. **None of it has run in Revit**: whether Revit redraws the change inside an open
group before the box, and whether Keep leaves exactly one undo entry and Discard none, are what this group
is for.

**A SCRATCH PROJECT, never a working model.** In Revit 2020 first: a project from the default metric
template with a few walls or ducts in a plan view, saved as *Heron Try Once.rvt*, with Changes on. The
fragment is a DRAFT at risk MODIFY with no proof block whose change shows in a plan - `python
tools/check-signatures.py` and the card say which; `color-by-parameter` or `override-graphics-in-view`
are the shape. Note the Undo list (the arrow beside Undo) before each row.

| # | Run | Look for |
|---|---|---|
| **EM1** | The plan in front, saved. `try-once <fragment>` with the values it needs; when the box opens, look at the view, then press **Keep it** | The view shows the change BEFORE the box is answered; the box reads *"'<fragment>' has never been proved. Keep what it did?"*; after Keep the change stays, the Undo list has exactly ONE new entry `Heron: <fragment>`, and the command line prints KEPT and writes `<fragment>.once.json` with `kept: true`. The box waits for you - Heron's own dialog watch (Group EL) never answers it. One Ctrl+Z takes the whole change back |
| **EM2** | Save again. The same fragment, corrected or with other values so it is a run the modeller would not keep; press **Discard it**. Then once more, closing the box with Escape | Both times the model is exactly as before - **the Undo list is unchanged**, no `Heron:` entry - the reply says `rolledBack` true and DISCARDED, and the record says `kept: false`. Ctrl+Z undoes whatever was done BEFORE the run, never this |
| **EM3** | After EM1's Keep: `validate <fragment> --write --kept` with a negative arrangement that must find nothing, then `heron_validate.py draft`, `review` and `accept --by "Ajmal PS"` | Only the negative runs (one rolled-back write); the draft's positive case is the kept run, *"the modeller saw the result in Revit and pressed Keep"*, the negative comes back empty, and `accept` writes the proof with the status still DRAFT. Edit one character of the fragment's `impl/` and re-run `validate --kept`: refused, *"taken against different code"* |
| **EM4** | Refusals, each before anything runs: (a) make one unsaved change, then `try-once`; (b) open a second model in front, then `try-once --in "Heron Try Once"`; (c) `try-once` on a PROVEN or READ fragment | (a) `ask_needs_saved_model` - *"Save the model, then ask again"*; (b) `ask_needs_model_in_front`; (c) refused by the command line, nothing sent. In every case the Undo list is unchanged |
| **EM5** | `try-once`, and leave the box open for two minutes before pressing Keep | The answer arrives - no *"still running"* at 60 s. Then set `revit.askTimeoutSeconds = 60`, restart Revit, and leave the box open three minutes: the command line says the box is still open and the run is not recorded; pressing Discard afterwards still puts the model back |
| **EM6** | EM1 and EM2 once on Revit 2024 and once on Revit 2027 | The same - one undo entry on Keep, none on Discard |
| **EM7** | *Not a proof - a check on the old add-in.* Before deploying, `try-once` against the add-in that is installed now | *"older than the Keep or Discard box"* - it ran as a preview, rolled back, nothing recorded, and no box appeared |

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

The DRAFT fragments tried were `create-text-note` (EM1, EM3), `create-fill-pattern` (EM2, EM5) and
`create-line-pattern` (EM6, on 2027). The model was saved before each try through `save-document` run from
*Heron Guard B 2024* - run from the model it would save, it refuses: *"Revit will not save a document while
Heron's Undo entry is open in it"*.

| # | Release | What was run and what came back | State |
|---|---|---|---|
| EM1 | 2024 | `try-once create-text-note` *HERON EM1* in `Level 1 HVAC Plan`; he clicked **Keep it**. `create-text-note.once.json` `answer keep`, `kept true`, `view_refreshed true`; 21,568 → 21,569; Undo list one new entry `Heron: create-text-note`. The note sat far from the building, so it was small in the view - see ET's note on zooming | **PASS** |
| EM2 | 2024 | `try-once create-fill-pattern` *HERON EM2 Hatch*: Discard clicked → `discard`, `kept false`, rolled back; again with Esc → `closed`, the same. The model stayed saved (no *unsaved changes*), the Undo list unchanged | **PASS** |
| EM3 | 2024 | `validate create-text-note --write --kept` with the negative `noteText FIRST\rSECOND`, `listType Numbered` - every value restated, because negative values replace the positive's rather than add to them: only the negative ran, *"NO NOTE WAS MADE"*; `heron_validate draft` wrote the positive as the kept run (*"the modeller saw the result in Revit and pressed Keep"*), status still DRAFT. One comment line added to the impl: `validate --kept` refused *"taken against different code ... Nothing was sent to Revit"*; the impl restored. `accept` is the owner's | **PASS** (signature owed) |
| EM4 | 2024 | (a) unsaved model: `ask_needs_saved_model` *"Nothing was run"*. (b) B in front: `ask_needs_model_in_front`. (c) `create-line` (PROVEN) and `read-selection` (READ): refused by the command line, nothing sent. And `try-once` on the version already kept: *"already ran once and was KEPT ... Nothing was sent to Revit"* | **PASS** |
| EM5 | 2024 | He left the box about two minutes: the answer came after 122 s, KEPT, recorded. The 60 s timeout half NOT RUN | **PASS** (first half) |
| EM6 | 2027 | `try-once create-line-pattern` *Heron EM6 Centre*: Discard → `discard`, the model still saved at 9,813; again, Keep → `keep`, 9,814. One `Heron: create-line-pattern` undo entry | **PASS** on 2027 (2024 is EM1/EM2) |
| EM7 | - | The add-in before the box was not installed anywhere | **NOT RUN** |
