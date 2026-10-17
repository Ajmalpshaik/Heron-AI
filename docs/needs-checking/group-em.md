# Needs checking — Group EM

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group EM - a DRAFT run once on the live model, behind Keep or Discard: `"apply": "ask"` and `try-once` (2026-10-17)

Built for [D-118 to D-121](../DECISIONS.md#d-118--a-draft-fragment-may-run-once-on-the-model-in-front-behind-a-keep-or-discard-prompt-in-revit-and-article-11-says-so),
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
