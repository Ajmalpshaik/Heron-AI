<!--
Heron-Agent:  HERON-QA-BIM-011
Heron-Step:   18
Heron-Status: DRAFT
Heron-Since:  0.1.0
Heron-Layer:  brain
See docs/29-metadata-standard.md
-->

# 50 — QA checklists

> **Status: built, tested without Revit. Never run in a real Revit.** Decision: [D-152](decisions/D-152.md),
> **Proposed**. Asked by the owner on 2026-11-08 after [docs/48](48-studying-bimlogiq.md) §5 item 4:
> *"QAQC Checklist. We need it ... We can add also in the companion app also."*

---

## 1. What the modeller sees

1. In the chat: *"make me a checklist called Before issue: every linked model is loaded, every room is
   enclosed, nothing references a missing file"*. The chat turns each sentence into a check, shows it, and
   saves it when they agree.
2. Later, in the chat - *"run Before issue"* - or on the Heron Companion's **QA Checklist** page: pick the
   checklist, press **Run**.
3. Each check reads the model once. The answer is one row a check:

   | # | Check | Rule | Value | Result | Why |
   |---|---|---|---|---|---|
   | 1 | every linked model is loaded | `notLoaded == 0` | 0 | **PASS** | |
   | 2 | no room is left unenclosed | `unenclosed == 0` | 2 | **FAIL** | unenclosed read 2; the rule is unenclosed == 0. **Select in Revit** |
   | 3 | every duct is on a system | ... | - | **NOT RUN** | DUCT_CHECK is DRAFT - no recorded proof stands behind it |

   *(An invented example.)* Then the counts: how many meet the value checked, how many do not, how many
   were not run.

**The words are fixed.** PASS means *the model meets the value checked* - the modeller's own rule, nothing
more. Heron never calls a model compliant: whether it is right is the engineer's call (the host
instructions, and `heron_modelqa`'s own rule). **A NOT RUN is never counted as a pass** and always says
why. **A run changes nothing**: every check is a read with no transaction open, so there is no undo entry,
and nothing reaches Keep or Discard.

Heron also has a **starter** - housekeeping checks (links loaded, referenced files found, rooms placed,
rooms enclosed, Spaces placed and enclosed). It lives in the code as an example and is **never saved by
itself**: the chat saves a copy under the modeller's own name only when they ask (*"save the starter
checklist as Before issue"*).

## 2. What a checklist is

One file, `%USERPROFILE%\.heron\checklists\personal\<name>.json` - the same data home as saved commands
(`HeronPaths.PythonData`, [D-135](decisions/D-135.md)):

```json
{"heron-checklist": 1, "name": "Before issue", "description": "",
 "checks": [{"sentence": "no room is left unenclosed",
             "capability": "SELECT_UNENCLOSED_ROOMS",
             "values": [{"name": "includeRooms", "value": "true"},
                        {"name": "includeSpaces", "value": "false"}],
             "output": "unenclosed", "op": "==", "target": 0,
             "ids": "unenclosed", "idsKind": "list", "selection": []}]}
```

A **check** is the modeller's sentence, **one** capability, its values, and a **pass rule on one output** the
fragment declares - the output, one of `== != < <= > >=`, and a number. `ids`, when given, is the output that
holds the failing elements' ids, for **Select in Revit**.

**The chat turns a sentence into a check** - Heron never classifies intent ([D-01](DECISIONS.md)). Heron
checks only what is saved, against each fragment's own card, and refuses the whole checklist naming every
problem:

| Refused when saved | Why |
|---|---|
| A capability declared above `ANALYZE` | A check only reads - the read door's own ceiling (`heron_tools.door_refusal`) |
| A capability Heron does not have | Nothing would run |
| A value the fragment does not take, one given twice, one over two lines, an `{input}` | `heron_commands.problems`, called - the same check as a saved command ([docs/49](49-saved-commands-and-the-heron-panel.md)) |
| A request value the card does not mark optional, left out | The add-in refuses an absent request need, so every run would be NOT RUN |
| A rule on an output the card does not declare | It would never come back; the refusal lists what the fragment does leave |
| A rule on text, a point or one element | Nothing to compare a number with |
| An `ids` output that holds elements, not ids | The add-in sends an element list's **names**, never its ids |
| More than `MAX_CHECKS` checks | A run is one read per check (`brain/heron_checklists.py`) |

**Not checked when saved, and asked on every run:** whether the capability is PROVEN, which model, whether
Revit is there. A proof can go stale between saving and running ([D-30](DECISIONS.md)).

**How a fragment declares its outputs.** `contract.provides` on its card - each a `name`, a `type` and a
`role` (`result` or `accounting`). A number reads from `int`, `double` and their kind; a count from a list or
dictionary, as the add-in describes it (`3 item(s) [...]`, `3 entry(ies)`); `bool` as 1 or 0. Element ids
come from a list of `ElementId` or a dictionary keyed by one.

## 3. Which capabilities a check can use today

**Never typed here - derived:**

```bash
python brain/heron_checklists.py --fragments
```

It lists every PROVEN capability with a proof that still describes its code, declared READ or ANALYZE, with
at least one output that reads as a number - each with the outputs a rule can use, the outputs that hold
element ids, and what it takes from the selection. Its last line counts them.

The starter uses three of them, chosen because each **collects its own elements** - none reads the
selection - and has both a count and an ids output:

| Capability | Rule on | Failing ids in |
|---|---|---|
| `LIST_LINKED_MODELS` | `notLoaded` | `notLoaded` |
| `REPORT_EXTERNAL_REFERENCES` | `unresolved`, `unloaded` | `unresolved`, `unloaded` |
| `SELECT_UNENCLOSED_ROOMS` | `unplaced`, `unenclosed`, `elements` | `unplaced`, `unenclosed` |

Others that suit a checklist, read off their cards: `CHECK_MODEL_STANDARDS` (`failures`, ids in
`offenders` - the owner's *"every duct has a System Name"* is a `requiredOn` and `requiredParameters` here,
and every one of its request values must be given; **not yet saved or run as a check**), `LIST_REVISIONS`
(`onNoSheets`, `lastDiffers`), `REPORT_MIRRORED_INSTANCES` (`mirrored`), `FIND_OVERLAPPING_TAGS` (`pairs`,
ids in `overlapping`, per view), `REPORT_TAGS_AND_TARGETS` (`orphanTags`, per view).

**A check that reads the selection.** A fragment whose card takes `elements` (or `devices`, `fixtures`...)
from no request value - `FIND_DUPLICATE_ELEMENTS`, `FIND_DUPLICATE_VALUES`, `FIND_CLASHES` - runs on what is
selected in Revit at that moment: the read starts with the chain reset. It can be saved; the chat says so,
and the page marks it *reads the selection in Revit*.

## 4. How a run works

```
heron_checklist run  /  Companion QA Checklist: Run
        |
  binding.resolve()  -- not connected: every check NOT RUN, nothing sent
        |
  for each check, in order (heron_checklists.run):
        PROVEN and fresh?  (bridge.proof_of)  -- no: NOT RUN, nothing sent
        under _revit_lock, for THIS read only:
            still the model the first check read?  (_moved_since)
            revit_read's own body, via _through   -- no transaction, the pin, the count
            the pin's refusal stands               (pinned.check)
        the output read off the reply  ->  PASS / FAIL / NOT RUN
```

- **One read per check, one check at a time.** The lock every tool call holds is taken for each check's read
  and let go before the next, so a Companion run never holds the pipe longer than the Model Dashboard's one
  read, and a chat can be answered between two checks. A run from the chat holds it for the tool call, as
  every tool does.
- **One run at a time on the page.** A second **Run** while one runs is refused in words. Each finished row
  reaches the page as it is done.
- **What arrives.** A number arrives as its text, a list as its count and first three items, a dictionary
  with every entry (`RevitFragment.Describe`, `providesEntries`). So **a list of more than three ids arrives
  incomplete**: the row says how many arrived and **Select in Revit is not offered** for it, rather than
  selecting part of what failed. A dictionary keyed by element id arrives whole.
- **Select in Revit** is the Model Dashboard's own route (`_dashboard_show`: `FILTER_ELEMENTS_BY_ID`, then
  `SET_SELECTION`, both PROVEN, declared `companion_dashboard_select` at EXECUTE), with the ids the run found
  - the page sends the row's number, never ids - and only on the model the run read.
- **CSV and Print / PDF** are made on the page from the rows Heron sent; nothing is read again.

## 5. What was built

| Part | File |
|---|---|
| The store, the checks against a card, the pass rule, the run, the starter, `--fragments` | [`brain/heron_checklists.py`](../brain/heron_checklists.py) |
| The shared folder and whole-file write, moved out of the command store for both | [`brain/heron_commands.py`](../brain/heron_commands.py) (`store_folder`, `write_whole`) |
| A fragment's contract as a checklist reads it | [`heron_brain.py`](../mcp/server/heron_brain.py) (`fragment_contract`, `checklists`) |
| The chat's tool `heron_checklist` (list, show, save, starter, run, delete), the run, the page's hooks | [`heron_mcp_server.py`](../mcp/server/heron_mcp_server.py), [`heron_tools.py`](../mcp/server/heron_tools.py) |
| The QA Checklist workspace | [`heron_companion.py`](../mcp/companion/heron_companion.py) (`ChecklistPanel`), [`static/`](../mcp/companion/static/) |

**`heron_checklist` is declared `ANALYZE` with `run_fragment_read`**, not `READ`: its run reaches the model
through `revit_read`'s body, so it carries that door's level and operation, as `revit_model_dashboard` does.
List, show, save and delete touch only the modeller's own files.

## 6. What is proven, and what is not

| | |
|---|---|
| **PASS** | `tests/test_checklists.py` - the store, every save refusal, the rule on the add-in's own shapes, a run with NOT RUN never a pass, the wording, the server's run against a stand-in server, and the starter against today's cards; runs everywhere (that last part needs PyYAML, as CI has) |
| **PASS** | `tests/test_checklist_tool.py` - the MCP tool through the real cards and a stand-in Revit: one `run_fragment_read` per check and no write, PASS and FAIL off the reply, nothing sent with no Revit; needs the MCP SDK, so CI lists it as not runnable |
| **PASS** | `tests/test_companion.py`'s QA Checklist group - data and hooks only, one run at a time, Select with the panel's ids by the dashboard's route, every route behind the pairing on a real server |
| **NEEDS REAL REVIT** | [group MV](needs-checking/group-mv.md) - a starter run on a real model, a FAIL with Select, a DRAFT check NOT RUN, a run with Revit closed, the page's Run beside a chat |

## 7. Not built - each waits on the owner

1. **All the failing ids of a long list.** The add-in sends a list of ids as its count and first three (D-26).
   Selecting all of them needs the add-in to send the ids of a list a check names - a C# change, not made.
2. **Inputs** - a value asked at each run, as a saved command has. Every value is fixed when saved.
3. **Team checklists** - a shared folder, and who may publish to it.
4. **A checklist in Revit's Heron panel**, beside the saved commands.
5. **A rule across two outputs** (`failures == 0 and notChecked == 0`) - one rule per check today; two checks
   say the same.
6. **Saving a run** as a dated report, to compare with the next.
