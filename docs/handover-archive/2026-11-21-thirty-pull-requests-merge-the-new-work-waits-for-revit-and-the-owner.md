# Session note — THIRTY PULL REQUESTS MERGE IN A DAY: THE NEW WORK WAITS FOR REVIT, AND FIVE QUESTIONS WAIT FOR THE OWNER

> **Session note** from 2026-11-21, written straight into this folder as its README asks. Nothing here is
> specification: where it disagrees with [DECISIONS.md](../DECISIONS.md), the
> [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those win**. A
> note records what was true on its own day.

---

### 2026-11-21 evening — THIRTY PULL REQUESTS MERGE IN A DAY: THE NEW WORK WAITS FOR REVIT, AND FIVE QUESTIONS WAIT FOR THE OWNER

Written to the rule in [section 11](../HANDOVER.md#11-handover--2026-09-12): what is NEW, a MISTAKE worth not
repeating, what is still TO DO. A cloud session with no Revit, bringing the status pages up after the day's
merges, with `origin/main` at `0429fc1c`. **Every number below is a record of this day, not a count to carry
forward** - derive today's with the command beside it.

**NEW - what merged on 2026-11-21.** The list is GitHub's, not typed from memory:
`gh api -X GET search/issues -f q='repo:Ajmalpshaik/Heron-AI is:pr is:merged merged:2026-11-21'` (dates in
UTC; the first-parent log of `main` for the day starts at `d1f9cc20`). Each pull request's own page says what it
changed and how it was checked.

| Pull request | What it did | What it leaves owed |
|---|---|---|
| [#1135](https://github.com/Ajmalpshaik/Heron-AI/pull/1135) | The usage counter: which clauses helped, on the modeller's word only ([D-191](../decisions/D-191.md), Accepted) | - |
| [#1150](https://github.com/Ajmalpshaik/Heron-AI/pull/1150) | A new family and an export of families open no transaction group in the model ([D-192](../decisions/D-192.md), Accepted) | [Group OX](../needs-checking/group-ox.md), issue [#1142](https://github.com/Ajmalpshaik/Heron-AI/issues/1142) |
| [#1152](https://github.com/Ajmalpshaik/Heron-AI/pull/1152) | Row 5b-1882 tracked as issue #1127 | - |
| [#1154](https://github.com/Ajmalpshaik/Heron-AI/pull/1154) | Checking-tool fixes: `row_links`, batch-prove's legs and retired cards, `next-id.py` reads issues | - |
| [#1155](https://github.com/Ajmalpshaik/Heron-AI/pull/1155) | The owner's delegated choices: NO6 is a Revit run, the ribbon note became [docs/52](../52-revit-ribbon-coverage.md), two agents are T1 | NO6 |
| [#1157](https://github.com/Ajmalpshaik/Heron-AI/pull/1157) | *Make sheets* and *Make sheets from a spreadsheet* became one tool, `CREATE_SHEETS` version 4 ([D-193](../decisions/D-193.md), Accepted) | [Group OZ](../needs-checking/group-oz.md), [#988](https://github.com/Ajmalpshaik/Heron-AI/issues/988) |
| [#1168](https://github.com/Ajmalpshaik/Heron-AI/pull/1168) | `EXTEND_ROOF_TO_ROOF` version 2 - a roof by footprint, from Revit 2022 | [Group OY](../needs-checking/group-oy.md) OY2-OY4 again and OY6-OY8, [#1144](https://github.com/Ajmalpshaik/Heron-AI/issues/1144) |
| [#1171](https://github.com/Ajmalpshaik/Heron-AI/pull/1171) | batch-prove gives the refusal as DID NOT RUN's reason; `ACTIVATE_DOCUMENT` offered for opening a file by path | - |
| [#1173](https://github.com/Ajmalpshaik/Heron-AI/pull/1173) | `REPORT_SPRINKLER_NETWORK` version 2: a ceiling sprinkler's level from its Schedule Level; a timed run for row 106 | [Group PA](../needs-checking/group-pa.md) [#1160](https://github.com/Ajmalpshaik/Heron-AI/issues/1160), [Group PD](../needs-checking/group-pd.md) [#1172](https://github.com/Ajmalpshaik/Heron-AI/issues/1172) |
| [#1178](https://github.com/Ajmalpshaik/Heron-AI/pull/1178) | **Revit 2024 on the owner's PC:** OI31 and Group OY passed, OU21 and OU22 run on 2024, four faults recorded | the four rows it names |
| [#1179](https://github.com/Ajmalpshaik/Heron-AI/pull/1179) | Row 5b-1952 recorded: questions about a Revit file by its path miss `READ_MODEL_FILE_INFO` | [#1177](https://github.com/Ajmalpshaik/Heron-AI/issues/1177) |
| [#1180](https://github.com/Ajmalpshaik/Heron-AI/pull/1180) | No-Revit fixes for #950, #1005, #1112, #1114: five cards DRAFT; the open-documents read answers whatever the pin | [Group PE](../needs-checking/group-pe.md), [#1166](https://github.com/Ajmalpshaik/Heron-AI/issues/1166) |
| [#1181](https://github.com/Ajmalpshaik/Heron-AI/pull/1181) | Every tool that labels its ending says an `outcome:` word; the doors and `heron_resolve` choose a provider in one order | - |
| [#1182](https://github.com/Ajmalpshaik/Heron-AI/pull/1182) | Add-in: a stopped Admin or Publish job says its outside work stands; a read that commits is refused; memory and pipe-client log lines | [Group PC](../needs-checking/group-pc.md), [#1167](https://github.com/Ajmalpshaik/Heron-AI/issues/1167) - **the add-in rebuilt** |
| [#1183](https://github.com/Ajmalpshaik/Heron-AI/pull/1183) | Routing: a level's elevation, a roof by footprint, filter order, joins, spreadsheet return and datum copy reach their tools | - |
| [#1184](https://github.com/Ajmalpshaik/Heron-AI/pull/1184) | Skills, job methods and two tool messages (#959, #960, #1125) | [Group PF](../needs-checking/group-pf.md), [#1170](https://github.com/Ajmalpshaik/Heron-AI/issues/1170) |
| [#1185](https://github.com/Ajmalpshaik/Heron-AI/pull/1185) | Family tools for #1111: divided-surface counts, a loop crossing itself, lists printed twice - all DRAFT | [Group PB](../needs-checking/group-pb.md), [#1163](https://github.com/Ajmalpshaik/Heron-AI/issues/1163) |
| [#1186](https://github.com/Ajmalpshaik/Heron-AI/pull/1186) | **Revit 2024 on the owner's PC:** Group OX's OX1 and OX2 passed | OX3 to OX5 |
| [#1189](https://github.com/Ajmalpshaik/Heron-AI/pull/1189) | Search names a card the model cuts before its job; vectors compared in plain Python, sqlite-vec removed ([D-194](../decisions/D-194.md), Accepted) | - |
| [#1190](https://github.com/Ajmalpshaik/Heron-AI/pull/1190), [#1191](https://github.com/Ajmalpshaik/Heron-AI/pull/1191), [#1193](https://github.com/Ajmalpshaik/Heron-AI/pull/1193), [#1199](https://github.com/Ajmalpshaik/Heron-AI/pull/1199) | Routing words: questions that reached a write now reach the read; a level's elevation to the PROVEN write; object-style materials; keynotes, text notes, viewports, walls not joining | [#1188](https://github.com/Ajmalpshaik/Heron-AI/issues/1188) is half repaired; the rest each pull request names |
| [#1192](https://github.com/Ajmalpshaik/Heron-AI/pull/1192) | `REPORT_ROOF_SLOPES` (DRAFT), a read for a roof's pitch | [Group PG](../needs-checking/group-pg.md), now on [#1202](https://github.com/Ajmalpshaik/Heron-AI/issues/1202) |
| [#1195](https://github.com/Ajmalpshaik/Heron-AI/pull/1195) | The doors skip a card that does not claim the Revit in front (row 5b-1131) | - |
| [#1206](https://github.com/Ajmalpshaik/Heron-AI/pull/1206) | The Revit checks rows 5b-1351 and 5b-1702 still owe; OT29 and OT30 brought up to date (#944, #945) | OT29, OT30 |
| [#1208](https://github.com/Ajmalpshaik/Heron-AI/pull/1208) | An owed Revit reading (OR29, on #1115); the owner's questions recorded (Q-95, Q-96); Q-94 answered; `open-issue.py` finds colour-marked questions | OR29, and three questions below |
| [#1210](https://github.com/Ajmalpshaik/Heron-AI/pull/1210) | Job methods say each card's real status; the builders-work method's step 5 keeps the face (#960) | - |
| [#1211](https://github.com/Ajmalpshaik/Heron-AI/pull/1211) | A lost read answer says it reached Revit; a carried id list mixing two models is refused; [D-195](../decisions/D-195.md) **Proposed** for Finalize | [Group PL](../needs-checking/group-pl.md), [#1204](https://github.com/Ajmalpshaik/Heron-AI/issues/1204) - **the add-in rebuilt** |
| [#1212](https://github.com/Ajmalpshaik/Heron-AI/pull/1212) | WN-7 (#1005): eight reading tools count what they pass over (D-64) - all DRAFT | [Group PK](../needs-checking/group-pk.md), [#1202](https://github.com/Ajmalpshaik/Heron-AI/issues/1202) |

**What is DRAFT now** is derived, never typed: `grep -h '^heron-status:' brain/fragments/*/fragment.yaml | sort | uniq -c`.
The cards today's pull requests changed, with the state each card says:

```bash
git diff --name-only d1f9cc20 0429fc1c -- 'brain/fragments/*/fragment.yaml' |
  while read f; do [ -f "$f" ] && echo "$(grep -h '^heron-status:' "$f") $f"; done | sort
```

Run on this day it listed DRAFT cards for every group above that says *DRAFT until proved*, PROVEN cards whose
routing words alone moved, and two cards DEPRECATED by `CREATE_SHEETS` version 4 (`create-sheet-list`,
`create-sheets-from-table`).

**MISTAKES worth not repeating.**

- **Two branches took the same group letter on the same day.** Group PK was taken by #1212 while #1211 held it,
  and #1211 had to become Group PL before it merged. `python tools/next-id.py` again *just before* the merge, not
  only when the id is chosen - heron-ship §6a says so, and on a day of thirty merges it is the step that matters.
- **A group merged with no issue of its own.** Group PG (#1192) reached `main` naming only its source issue
  #1161, where D-187 asks for an issue per new group in the same change. Its checks were added to #1202 - the
  same card's version 2 runs there, "after Group PG" - and its file now names #1202.
- **`tools/open-issue.py` would not do that.** It read #1202's passing words *"after Group PG"* as Group PG
  already being tracked there, and refused both `--create` and `--into`. The checks were added by hand and the
  number written back with `--record 1202`; the fault is [row 5b-1962](../fragment-issues/row-5b-1962.md), issue
  [#1219](https://github.com/Ajmalpshaik/Heron-AI/issues/1219).
- **A generated page was left stale by a change that knew it.** #1208 left `BALANCE-OF-WORK.md` listing Q-94 as
  open, correctly, for its own tool; it was regenerated in this change. A page stamped with another day is
  history - run its tool, never edit it.

**TO DO - in this order.**

1. **The owner's word, no Revit** - five things only he can answer, each where to answer it:
   - [D-195](../decisions/D-195.md), **Proposed**: should the Loads panel's Finalize go to Revit as ONE change
     (one undo entry), every step gated as if sent alone? Accept, change or reject. Nothing is built.
   - [#1103](https://github.com/Ajmalpshaik/Heron-AI/issues/1103): five add-in questions - 5b-1060 (compare the
     path too?), row 129 (`prove` or `validate` to change?), 5b-1903 (read-back says *"asked, did not change"*?
     - question 1 of #1208), 5b-1904 (refuse deleting a Revit link until OS13 runs?), D-112 (read Accepted?).
   - [#1009](https://github.com/Ajmalpshaik/Heron-AI/issues/1009): **Q-95** - which service may index document
     text in the cloud, and does the switch stay off until he turns it on? **Q-96** - add a document from the chat
     in one step? (questions 2 and 3 of #1208).
   - [#1070](https://github.com/Ajmalpshaik/Heron-AI/issues/1070): `turn it` or `new view is enough` - should
     Heron turn an isometric 3D view such as a family's View 1 (row 5b-1860)?
   - [#1118](https://github.com/Ajmalpshaik/Heron-AI/issues/1118): rename docs 35 and 48, rename them and take
     the names out of their text, or leave them (D-177)?
2. **On the PC, the reads first** - DRAFT, changing nothing, on scratch copies: Group PG then PK on the same
   roof copy (#1202), Group PE (#1166), PB (#1163), PA (#1160), and PD's one timed run (#1172).
3. **Then the DRAFT writes**, behind Keep or Discard: Group OZ (`CREATE_SHEETS` v4, #988) and Group OY's
   version 2 rows (#1144).
4. **Then rebuild and deploy the add-in** - #1182 and #1211 changed it - and run what only the new add-in can
   show: Groups PL (#1204), PC (#1167), OV (#1123), OW (#1136), OX3 to OX5 (#1142), OR29 (#1115), and the
   methods of Group PF (#1170). Each group's file has its order and its negative case.

The full list waiting on Revit is `python tools/owner-queue.py`; what is unfinished is `python tools/check-gaps.py`.
