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
| A request value the add-in's binder cannot leave out, left out | Every run would be NOT RUN. Which ones is `heron_fragment.need_may_be_absent`'s answer, asked by the server |
| A rule on an output the card does not declare | It would never come back; the refusal lists what the fragment does leave |
| A rule on text, a point, one element, a `float`, a `HashSet` or an `ISet` | `RevitFragment.Describe` sends none of them as a number - a float and a HashSet arrive as their type names - so the check could never run |
| An `ids` output that holds elements, not ids | The add-in sends an element list's **names**, never its ids |
| More than `MAX_CHECKS` checks | A run is one read per check (`brain/heron_checklists.py`) |

**Three labels, each optional and the modeller's own** ([D-159](decisions/D-159.md)): `severity` - high,
medium or low; `clause` - the BEP or standard clause the check comes from, one line up to `CLAUSE_MAX`
characters; `advice` - how to fix a failure, one line up to `ADVICE_MAX` (both in
`brain/heron_checklists.py`). Heron never sets a severity and never checks a clause:
it quotes them on every run, in the chat, on the page and in the CSV. A checklist saved before them reads
as before. **The same limits hold when a checklist is read** - a file made by hand or shared from another
PC never went through the save, so one whose sentence or labels break them is listed as one that could not
be read ([row 5b-1618](fragment-issues/row-5b-1618.md)).

**Not checked when saved, and asked on every run:** whether the capability is PROVEN, which model, whether
Revit is there. A proof can go stale between saving and running ([D-30](DECISIONS.md)).

**How a fragment declares its outputs.** `contract.provides` on its card - each a `name`, a `type` and a
`role` (`result` or `accounting`). A rule may use what `RevitFragment.Describe` sends as a number: `int`,
`long` and `double` as their text, `bool` as true/false (read as 1/0 - `workshared == true` works), and a
list, an array or a dictionary as its count (`3 item(s) [...]`, `3 entry(ies)`). A rule's number may be sent
as text, `"0"`; the rule is given once - as `failures == 0`, or as output, op and target, never both. Element
ids come from a list of `ElementId` or a dictionary keyed by one.

**A saved file is read whole.** Every field each check needs is checked when a file is read; a file of the
wrong shape - edited by hand, say - is listed as one that could not be read, and the list, the chat and the
Companion keep working around it.

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
from no request value - `FIND_DUPLICATE_ELEMENTS`, `FIND_DUPLICATE_VALUES`, `FIND_CLASHES`, each while its
card is PROVEN, which `--fragments` above lists and this sentence does not - runs on what is
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
  read, and a chat can be answered between two checks. A run from the chat holds it for the whole tool
  call, as every tool does.
- **One run at a time, the chat's and the page's together.** Both claim the Companion panel's busy flag: a
  second **Run**, a page Run while the chat runs one, and a chat run while the page runs one are each
  refused in words, naming who is running. Each finished row reaches the page as it is done.
- **A run that stops part way is still finished.** A fault in a read keeps the rows already read, makes every
  check after it NOT RUN with the reason, and is listed as failed - never left at *Running*.
- **The model changing between two checks is said.** After each read the add-in's change count (D-147) is
  read; when it moved during the run, the result says so and asks for Run again.
- **What arrives.** A number arrives as its text, a list as its count and first three items, a dictionary
  with every entry (`RevitFragment.Describe`, `providesEntries`). **A check's own ids output arrives whole**
  ([D-158](decisions/D-158.md)): its read names it in `sendIds`, and the add-in sends every id of that list
  in `providesIds`, up to the add-in's `IdsSentPerList`, with its total. A list longer than that - or any
  list from an add-in from before D-158, or ids sent with no total beside them - arrives incomplete: the
  row says how many arrived and **Select in Revit is not offered** for it, rather than selecting part of
  what failed. A dictionary keyed by element id arrives whole.
- **Select in Revit** is the Model Dashboard's own route (`_dashboard_show`: `FILTER_ELEMENTS_BY_ID`, then
  `SET_SELECTION`, both PROVEN, declared `companion_dashboard_select` at EXECUTE), with the ids the run found
  - the page sends the row's number and the run's token, never ids. A row from a run that has since been
  replaced - by a chat run, say - is refused in words; so is the model having moved (*the checklist was run on
  another model - press Run again*). Past the route's limit (`DASHBOARD_SELECT_MOST` in the server) the button and the answer say *the first
  N of M* - a partial Select, recorded as [row 5b-1621](fragment-issues/row-5b-1621.md) and
  [Q-90](open-questions/tier-2.md).
- **What changed since the last run** ([D-159](decisions/D-159.md), repaired 2026-11-12 - rows
  [5b-1613](fragment-issues/row-5b-1613.md) to [5b-1620](fragment-issues/row-5b-1620.md)). A finished run
  is kept in `<data home>/checklists/runs/<checklist>/<model>/` - the newest `RUNS_KEPT` **of each model** -
  and compared with the newest kept run of the same checklist **on the same model**. **A model is its file
  path**: the same file opened in a restarted Revit is the same model, a Save As is a new one, and a model
  with no path (never saved, or detached and not saved) is never compared or kept - the reply says to save
  it. Each check is **NEW** (failing now, not last time), **STILL OPEN** (with how many elements are failing
  that were not, and how many are no longer failing, when every id arrived), **FIXED**, or **NOT COMPARED**,
  with why: not run now or last time; not in the last run (added, or edited - a check is matched by its
  sentence, capability, rule **and values**); two checks the same in all four; a check that reads the
  selection; a newer kept run that cannot be read; or nothing to compare with - a first run, whose rows read
  NOT COMPARED, never *met both times*. A run that stopped part way is neither compared nor kept, and a
  history that cannot be read or written never stops a run.
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

1. ~~**All the failing ids of a long list.**~~ **Built 2026-11-12, [D-158](decisions/D-158.md)**: a check's
   read names its ids output and the add-in sends that list whole, up to `IdsSentPerList`. Not yet run in Revit
   ([Group NV](needs-checking/group-nv.md)).
2. **Inputs** - a value asked at each run, as a saved command has. Every value is fixed when saved.
3. **Team checklists** - a shared folder, and who may publish to it.
4. **A checklist in Revit's Heron panel**, beside the saved commands.
5. **A rule across two outputs** (`failures == 0 and notChecked == 0`) - one rule per check today; two checks
   say the same.
6. ~~**Saving a run**~~ **Built 2026-11-12, [D-159](decisions/D-159.md)**: each finished run is kept and
   compared with the last on the same model. Not yet run in Revit ([Group NV](needs-checking/group-nv.md)).
