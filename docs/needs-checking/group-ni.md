# Needs checking — Group NI

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group NI - three repairs to the proving tools that only a PC with Revit can watch: a card refused on a release its own list leaves out, the client's output on Windows, and two suites that reach the live Revit (2026-11-11)

Three [FRAGMENT-ISSUES](../FRAGMENT-ISSUES.md) rows were repaired or recorded on 2026-11-11 in a cloud
container, where there is no Windows and no Revit. Each is held by a suite on a stand-in; **none of the
rows below has run**, and each says what the suite cannot see.

| Row | What changed | Held without Revit by | What that cannot see |
|---|---|---|---|
| [5b-1403](../fragment-issues/section-5b-rows-401-425.md) | `validate` and [`tools/batch-prove.py`](../../tools/batch-prove.py) refuse a card whose `revit:` list leaves out the connected release, before a fragment is sent. A dry run now **pings** each Revit in the address book to learn which is connected, and says so | [`tests/test_batch_prove.py`](../../tests/test_batch_prove.py), on a stand-in Revit | the real address book, a real ping, and that Revit is left alone by it |
| [5b-1487](../fragment-issues/section-5b-rows-401-425.md) | The bridge client prints as UTF-8, so a label with a character cp1252 has not got no longer ends a command in a traceback; `try-once` reads `--in` wherever it stands | [`tests/test_try_once.py`](../../tests/test_try_once.py) section 8, on a strict cp1252 stream | a real Windows console, pipe or file |
| [5b-1510](../fragment-issues/section-5b-rows-401-425.md) | **Not repaired - recorded.** Two suites run `batch-prove`'s `main` for real and leave `free_models` as it is, so with a Revit connected they ping it and send it `release_models` | nothing - it was read, not run | whether it happens at all on the PC |

**The PC, any Revit Heron supports, a SAVED scratch copy of a sample model - never the owner's own file.**
NI1 to NI4 use Revit 2020, 2021 or 2022, because the card they send, `report-analytical-model` (READ,
DRAFT on 2026-11-11), lists 2023 to 2027: on those three releases its list leaves the connected one out.
Read its `revit:` line first - if it has changed, take any DRAFT at READ whose list leaves the connected
release out. Nothing in NI1 to NI5 keeps a change: every run is a read, a refusal, or rolled back.

**The job file for NI1 and NI2** - two jobs, written to `%TEMP%\heron-work\ni\release.yaml` (rough work
goes there, never beside Heron):

```yaml
defaults:
  negative-set:
    categoryName: Sheets
jobs:
  - fragment: report-analytical-model      # its revit: list starts at 2023
  - fragment: report-level-elevations      # lists every release; DRAFT on 2026-11-11
```

If `report-level-elevations` is no longer DRAFT it reads `ALREADY` and proves nothing here: put any DRAFT
at READ that lists the connected release in its place, and dry-run until it says `WOULD RUN`.

| # | Run | Look for |
|---|---|---|
| **NI1** | *The dry run refuses, and says what it asked of Revit.* Revit 2020 open and connected, nothing else using it. `python mcp/client/heron_bridge_client.py list` for the session's pid, then `python tools/batch-prove.py "%TEMP%\heron-work\ni\release.yaml" --dry-run --session <pid>` | `report-analytical-model` is **`REFUSED`**, its line naming Revit 2020 and the card's own releases, and saying *"No fragment was sent to Revit"*; `report-level-elevations` is `WOULD RUN`; the batch exits **1**. The `mode:` line says no fragment is run and that a connected Revit is asked only whether it is there, and the closing lines say no fragment was run and that each Revit the address book lists *"was asked one thing, once: whether it is there"*. **In Revit nothing happened:** no box, no undo entry, the model not marked changed, and a chat that asks Heron for something straight afterwards is not told the Revit is in use. `WOULD RUN` for the first job fails the row: the address book was not read as the suite's stand-in was |
| **NI2** | *The real run sends nothing for the refused card.* `python tools/batch-prove.py "%TEMP%\heron-work\ni\release.yaml" --only report-analytical-model --session <pid>` - no `--dry-run`, and `--only` so that the second job is not sent: a real run of it would leave a run record and a draft for `report-level-elevations` in `brain/proof-drafts/`, arranged for nothing | `REFUSED` again, at once, with no wait for a compile and **no `compile_failed`** anywhere in the output; exit 1; `git status` shows nothing new under `brain/proof-drafts/` |
| **NI3** | *`validate` by hand.* `python mcp/client/heron_bridge_client.py validate report-analytical-model --session <pid>`, then `echo %ERRORLEVEL%` | The refusal naming Revit 2020 and the card's releases, **before any `active:` line** - the model was never asked for its element count - and exit **2**. An `active:` line, or `compile_failed`, fails the row |
| **NI4** | *Negative - a card that lists the release is not refused.* On the same Revit: `python mcp/client/heron_bridge_client.py validate --out "%TEMP%\heron-work\ni\levels.json" --negative-set categoryName=Sheets --session <pid> report-level-elevations` - `--out` in front of the name, where the client reads it, so the record lands in the scratch folder and not in `brain/proof-drafts/`. Where the PC also has a Revit 2023 or newer, NI3's own command on that one, with `--out "%TEMP%\heron-work\ni\analytical.json"` in front of the name | It prints `active: <the scratch model> ... Revit 2020` and runs its phases; nothing about releases is said. On the newer Revit `report-analytical-model` is not refused either. Neither record is evidence for a proof - they are arranged for nothing; delete the folder afterwards |
| **NI5** | *A label cp1252 has not got, printed to a file* - row 5b-1487's own character, the Greek delta of a pressure-drop label, on the model that met it: Revit 2024, the saved copy "Snowdon HVAC - schematic proof" of [Group MW](group-mw.md) in front. `python mcp/client/heron_bridge_client.py fragment draw-mep-schematic --write --set "systemName=Mechanical Supply Air 1" --set "newViewName=NI5 schematic" --session <pid> > "%TEMP%\heron-work\ni\schematic.txt" 2>&1` - its own view name, because MW8's kept run left *"Schematic - Mechanical Supply Air 1"* in that copy -, then `echo %ERRORLEVEL%`. No `--apply`, so the schematic is drawn and **rolled back** (D-55). Then the same command with no `>`, straight to the console | Exit **0** both times and **no `UnicodeEncodeError`** in either; the file ends on the fragment's results, not on a traceback, and opened as UTF-8 its label lines carry the delta whole. On the console the delta is shown, or a substitute character - either passes; a traceback fails. If no line of the answer carries a delta the row has **not run** - say so, and name the label it did print |
| **NI6** | *`--in` after the fragment's name is read, with nothing run.* The same model in front. `python mcp/client/heron_bridge_client.py try-once draw-mep-schematic --in "Snowdon HVAC - schematic proof"`, then `try-once draw-mep-schematic --in` with nothing after it | The first prints that `draw-mep-schematic` is PROVEN, not DRAFT, and has nothing to try - **not** the usage text that begins *"Which DRAFT fragment?"*, which is what row 5b-1487 met; nothing is sent and no box opens. The second prints *"--in needs the title of an open document after it"*. Both exit 2. If the card is DRAFT again when this is run, the first WILL run it once behind the Keep or Discard box: press **Discard** |
| **NI7** | *Waits on row 5b-1510's repair - do not run it before.* Once both suites replace `free_models`, or install [`tests/support/live_revit_guard.py`](../../tests/support/live_revit_guard.py): a Revit open and connected, then `python tests/test_batch_prove.py` and `python tests/test_release_proofs.py` | Both exit 0, and the guard's own findings - `guard.touched()`, which the repair has each suite print - say **no named pipe was opened** and nothing in the real address book was listed, opened or removed. A repair that only replaces `free_models` has nothing to count with, so this row asks for the guard. Before the repair nothing on the PC counts it at all, which is why the row was read and not measured |

**Then** the group closes on NI1 to NI6, with NI7 closing when row 5b-1510 does - there is no card to sign:
these are tools, not fragments. A row that fails reopens its FRAGMENT-ISSUES row; nothing in a row changes
to make it pass.

### Run record - 2026-11-13, NI1 to NI4 on the laptop, Revit 2020

Branch `claude/pc-proofs-2026-11-12` (main at 10334996 and this sitting's records). Revit 2020, session
41776, the add-in built from 10334996; the model **P1102 MEP**, the saved scratch copy of Revit 2020's
`rme_advanced_sample_project.rvt`. `report-analytical-model` read `revit: ["2023" ... "2027"]` and DRAFT;
`report-level-elevations` DRAFT, all eight releases - both as the rows assume. Every line with
`HERON_CLIENT_ID=ajmal-pc`, stdin closed. The job file and every output are in `%TEMP%\heron-work\ni`.

| # | Result | What was seen |
|---|---|---|
| NI1 | **PASS** | `report-analytical-model  REFUSED  ... does not claim Revit 2020, the release connected - its revit: is 2023, 2024, 2025, 2026, 2027. No fragment was sent to Revit ...`; `report-level-elevations` WOULD RUN; exit 1. The `mode:` line: *no fragment is run ... a connected Revit is asked only whether it is there (a ping)*; the closing lines: *each Revit it lists ... was asked one thing, once: whether it is there*. The add-in log gained no line during the run; a chat that asked `READ_SELECTION` straight afterwards was answered, not told the Revit was in use |
| NI2 | **PASS** | `--only report-analytical-model`, no `--dry-run`: REFUSED in 3 seconds, no `compile_failed` in the output, exit 1, `git status` nothing new under `brain/proof-drafts/`, the add-in log no new line |
| NI3 | **PASS** | `validate report-analytical-model --session 41776`: the same refusal and nothing before it - no `active:` line, no `compile_failed`; `ERRORLEVEL` 2 |
| NI4 | **PASS** - Revit 2020 half | `validate --out ...\ni\levels.json --negative-set categoryName=Sheets ... report-level-elevations`: `active: P1102 MEP (21,453 elements), Revit 2020, session 41776`, positive ok, negative ok, exit 0, nothing said about releases, the record in the scratch folder. Also printed: *IGNORED 'categoryName' - not a value this fragment declares* - the row's negative value is not one this card takes, so its negative leg ran with no input of its own; the record is evidence for nothing, as the row says |
| NI4, newer half | **NOT RUN** | needs a Revit 2023 or newer connected at the same time; one Revit at a time on this PC |

The scratch folder is left as it is: deleting it waits for the owner's word.
