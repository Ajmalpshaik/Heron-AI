# FRAGMENT-ISSUES archive — section 5b, rows 1901 to 1925

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
> Rows 1901 to 1925 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-1904

*Moved from the register on 2026-11-24.*

**Defect.** **A LINK DELETED OR UNLOADED INSIDE HERON'S GROUP MAY NOT COME BACK WHEN THE GROUP ROLLS BACK, AND `DELETE_ELEMENTS` CAN BE HANDED A LINK TYPE.** From research note 51 section 4 ([doc 51](../31-research.md)), checked against Heron 2026-11-20 for issue [#1057](https://github.com/Ajmalpshaik/Heron-AI/issues/1057), read and not run. The other platform records that deleting a link type unloads its document and a rollback does not load it again. Heron rolls back to preview (a dry run, D-155), to measure a delete before it asks (D-142), and on Discard (D-126); each says the model is as it was. No live card's C# deletes a link, and only `RELOAD_LINKS` (DRAFT, its first finding *"No undo"*) unloads one - `tests/test_read_door.py` section 14 now fails on any new card that does either. But [`DELETE_ELEMENTS`](../../brain/fragments/delete-elements/fragment.yaml) (PROVEN) deletes whatever it is handed, and `UNLOAD_LINKS`' own routing sends *"remove the link completely"* to it on the link type - so doc 51's *"no Heron card removes links today"* is wrong in practice. Whether Revit's rollback reloads the link is **not known**.

**State.** **MOVED TO ISSUE [#1103](https://github.com/Ajmalpshaik/Heron-AI/issues/1103) 2026-11-20** Was: **OPEN 2026-11-20 - recorded; the runtime guard is the owner's.** Group [OS](../needs-checking/group-os.md)'s OS13 finds out what a rollback does to a link. Whether the add-in should refuse to delete a `RevitLinkType` or `RevitLinkInstance` until then is asked on the pull request.

---

### Row 5b-1906

*Moved from the register on 2026-11-24.*

**Defect.** **ON THE OWNER'S PC A FILTERING PROGRAM REWRITES THE SECURITY HEADER THE COMPANION SENDS, AND CONNECTIONS TO 127.0.0.1 RESET WHEN THE SERVER CLOSES THEM - AND THE COMPANION'S SERVER ANSWERS A REFUSAL BEFORE READING THE REQUEST'S BODY, WHICH LOSES MOST REFUSALS THERE.** Found 2026-11-20 on the owner's PC (Windows 11, Python 3.12.10, a filtering program running) working GitHub issue [#1055](https://github.com/Ajmalpshaik/Heron-AI/issues/1055), where `tests/test_companion.py` died on `main` with `ConnectionResetError [WinError 10054]` at its first refused POST. Measured from Python in `%TEMP%\heron-work\i1055`, nothing in the repository: (1) **what arrives is not what was sent:** a bare server on 127.0.0.1, with no Heron code in it, sent `Content-Security-Policy: default-src 'none'` and the client received a policy naming the filter's own hosts for scripts, styles and sockets - every one of the 177 replies of 200 that arrived was changed. Whether the owner's browser gets the same rewrite on the Companion page was not checked; if it does, the page runs under the filter's policy rather than Heron's ([docs/40](../40-heron-companion.md) section 11). (2) **Resets on close, on the same machine:** that bare server, reading each whole request, answering and closing, lost 24 of 200 to a reset; with nothing closing, 200 of 200 came back. That the filter causes the resets as well as the rewrite was not separated. (3) **The real Companion there**, 100 requests each: a POST refused for its Origin before its body is read, 33 got their 403 back and 67 reset; a POST to no route, 24 got their 404 and 76 reset; the page itself by GET, 88 came back and 12 reset. A trial patch in `mcp/companion/heron_companion.py` - `_Server.shutdown_request` stopping writing, then reading and dropping what the caller still sends, at most 1 MiB and 2 s, before closing (a web server's "lingering close") - lifted those to 97, 98 and 98 of 100. **Not kept:** every number above was measured on that machine, no suite fails without the patch, and Linux CI cannot see either side, so it is not shown to be a defect of the Companion's own. `tests/test_companion.py` now runs that bare control first and, where it does not come back unchanged every time, waits (exit 3) on its four server sections rather than failing them. **Owed:** (a) the owner's choice whether the filter leaves `127.0.0.1` and `python.exe` alone - a setting of his, never changed by Heron; (b) the patch measured on a Windows machine with no filter, where a refused POST losing its reply would be the Companion's own defect.

**State.** **MOVED TO ISSUE [#972](https://github.com/Ajmalpshaik/Heron-AI/issues/972) 2026-11-21** - still open, tracked there. Was: **OPEN 2026-11-20** - the numbers recorded; the suite waits on such a machine; the server patch not kept. Found working GitHub issue #1055 on the owner's PC.

---

### Row 5b-1909

*Moved from the register on 2026-11-24.*

**Defect.** **THE READING FRAGMENTS THAT GO LOOKING AND CAN DROP A CANDIDATE WITHOUT DECLARING HOW MANY THEY DROPPED - [D-64](../decisions/D-64.md)'S WORKLIST - HAD NO ROW, PROPOSAL OR ISSUE HOLDING THEM, ONLY A WORK-NOTE LINE.** Written 2026-11-20 for work-note item WN-7 of GitHub issue [#1005](https://github.com/Ajmalpshaik/Heron-AI/issues/1005) (item 2 of the archived link-contracts queue in [`HANDOVER.md`](../HANDOVER.md), 59 fragments when D-64 was accepted on 2026-09-09). **The list is a command, not a number typed here:** `python tools/check-revit-gate.py --list reporting` names every fragment its question 14 raises, with the reason - measured 2026-11-20 on `main` at `d5640f27`, 98 of 744. Each is a reader whose answer can come back short without saying so: D-64 asks it to declare the dropped count as a provide (`role: accounting`) and to carry the marker only on an empty answer. **Not repaired here:** each fragment is its own change, most are PROVEN, and the gate is a report - a hit is a question for a person, not a defect proved. A reader that is changed for any other reason should take the count in the same change.

**State.** **MOVED TO ISSUE [#1005](https://github.com/Ajmalpshaik/Heron-AI/issues/1005) 2026-11-20** - still open, tracked there. **2026-11-21: eight DRAFT reading cards done, the row stays open.** `REPORT_LINKED_VIEW_USAGE` v2 (`skippedPairs`, and an instance with no override of its own read through its link type, which version 1 dropped), `LIST_LINKED_MODELS` v3 (`skippedPlacements`), `REPORT_STRUCTURAL_CONNECTIONS` v2 (`unresolvedConnections`), `REPORT_ROOF_SLOPES` v2 (`skippedRoofs`), `REPORT_DIVIDED_SURFACES` v4 (`missingNodes`, and the 50-node cap said), `REPORT_FABRICATION_SETUP` v2 (`skippedButtons`), `REPORT_CATEGORY_OVERRIDES` v5 (`skippedCategories`) and `REPORT_VIEW_FILTERS` v6 (`skippedViews`, asked for every view) each count what their loop passed over and say it on the empty answer only - NEEDS-CHECKING [Group PK](../needs-checking/group-pk.md), issue [#1202](https://github.com/Ajmalpshaik/Heron-AI/issues/1202); `check-revit-gate.py --list reporting` no longer names them. **Looked at and left, each a question for a person, not a fix:** `REPORT_STRUCTURAL_LOADS` already declares what it drops as `noAnalytical`, a name the gate's word list does not know ([row 5b-1958](../fragment-issues/row-5b-1958.md) - fixed 2026-11-21 in the gate: a contract's own `role: accounting` with a negative name counts, so it and every other card already declaring its drop that way leave the list); `REPORT_FAMILY_TEMPLATE`'s loops skip by scope (comment lines in the classification file, templates, views of no kind), not on a failed read; `REPORT_SPRINKLER_NETWORK` passes over a connector whose domain will not read (`catch { continue; }`), but its answer is a network in JSON with no empty answer for the marker to ride on, so where its count is said is a question for a person; `CHECK_LINK_COORDINATES` and `REPORT_LEVEL_ELEVATIONS` are other sessions' cards today; the five MODIFY cards on the list are writes, and D-64 was written about readers. An independent review of the change found `REPORT_CATEGORY_OVERRIDES`' whole-dialog block showing a failed read as *"no override"* ([row 5b-1959](../fragment-issues/row-5b-1959.md)), left for that card's next change. Still owed: every PROVEN reader on the list - each one a card back to DRAFT, so each waits for a change to its C# for another reason, as this row says. Was: **OPEN 2026-11-20** - the worklist's home; WN-7 in issue #1005 stays open until it is worked.

---

### Row 5b-1914

*Moved from the register on 2026-11-24.*

**Defect.** **`revit_view_image` WITH `around` ANSWERED ONLY .NET'S *"Object reference not set to an instance of an object"* ON A REVIT 2020 FLOOR PLAN, TWICE - AND THE REPLY SAID NOTHING OF WHAT WAS MISSING.** Field report [#1058](https://github.com/Ajmalpshaik/Heron-AI/issues/1058) A1, 2026-11-19, Revit 2020, a real MEP project (not named; the repository is public): `around` given two element ids, then one (duct accessories drawn in the plan), `width=1200` - both *"Revit could not picture that region of '<the plan>': Object reference not set to an instance of an object."* A picture of the whole plan worked. Group OD's runs of `around` were on Revit 2024; nothing records it on 2020. **Cause not found** - no Revit in the session that recorded this. Read in [`RevitCapture.cs`](../../revit/Heron.Revit.Addin/RevitCapture.cs) `OfRegion`: the region path copies the plan (`View.Duplicate`), crops the copy (`CropTo`) and exports it; the whole-plan path does neither, so the null is in the copy, the crop or the export of the copy.

**State.** **MOVED TO ISSUE [#1115](https://github.com/Ajmalpshaik/Heron-AI/issues/1115) 2026-11-20** - still open, tracked there as OR21 (was #1058 A1). Was: **OPEN 2026-11-20 - the reply now names the step; the cause is owed to a Revit 2020 run.** Done here: each step of `OfRegion` names itself, a copy Revit does not make and a copy with no crop box are named in words, and a null still thrown answers *"something Revit should have handed back was missing while <step> (in <the call>)"* - so the run on the owner's PC says where. [NEEDS-CHECKING Group OR](../needs-checking/group-or.md) OR21 reproduces it on a Revit 2020 scratch plan; the fix of the null itself follows what that run says. Tracked on [#1058](https://github.com/Ajmalpshaik/Heron-AI/issues/1058).

---

### Row 5b-1919

*Moved from the register on 2026-11-24.*

**Defect.** **[`REPORT_ROOM_SPACE_DATA`](../../brain/fragments/report-room-space-data/fragment.yaml) MAY PRINT A ROOM'S NUMBER TWICE IN ITS IDENTITY** - row 5b-1858's label defect, in another card. Read 2026-11-20 for issue [#1060](https://github.com/Ajmalpshaik/Heron-AI/issues/1060) part (3): `report-room-space-data/impl/any/fragment.cs` builds each identity as the number, a space, and `spatial.Name` (around line 221), and Revit's `Name` of a room or space already carries its number - `COUNT_BY_SPATIAL_CONTAINER`'s own signed proof reads *"Room 1 1"* for the same read. So a room numbered 101 called Office would read *"101 Office 101"*. **Not confirmed on a sample model** - no Revit in the session that recorded this.

**State.** **MOVED TO ISSUE [#1115](https://github.com/Ajmalpshaik/Heron-AI/issues/1115) 2026-11-20** - still open, tracked there as OR20 (was #1060 part 3). Was: **OPEN 2026-11-20 - needs a real Revit to confirm**, on a scratch copy of a sample model: [Group OR](../needs-checking/group-or.md) OR20 reads one room's identity. If it carries the number twice, the fix is the `ROOM_NAME` parameter, as `COUNT_BY_SPATIAL_CONTAINER` version 3 reads it, in a PROVEN card's C#.

---
