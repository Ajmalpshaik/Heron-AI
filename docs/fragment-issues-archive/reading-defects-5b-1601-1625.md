# FRAGMENT-ISSUES archive — section 5b, rows 1601 to 1625

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
> Rows 1601 to 1625 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-1601

*Moved from the register on 2026-11-17.*

**Defect.** **THREE PLACES SAID HOW A CHANGE IS KEPT AND NAMED NO DRY RUN.** Found 2026-11-12 in an independent review of #861 - **not seen in Revit**. (a) `revit_health`'s Ask to Keep lines (`heron_health.ASK_TO_KEEP_ON`, `_OFF`) list what is *"Different for:"* and were written the day before [D-155](../decisions/D-155.md): *"a change made through revit_change is kept at once"* was said of a call that keeps nothing. (b) [`hvac-spaces-and-zones`](../../brain/skills/hvac-spaces-and-zones.yaml) said *"STEP 4 HAS NO PREVIEW OF ITS OWN"*; its card, SYNC_SPACES_WITH_LINKED_ROOMS, is PROVEN and MODIFY and declares no `noTransactionOpen`, so `dry_run` runs it and rolls it back. (c) [`parameters-csv-round-trip`](../../brain/skills/parameters-csv-round-trip.yaml) said *"Nothing in Heron shows which cells differ from the model first"*; a dry run's read-back does, for a card `revit_change` runs - and that step's card, SET_PARAMETER_VALUES_BY_ID, was DRAFT on 2026-11-12, so a dry run of it is refused as the write is.

**State.** **CLOSED 2026-11-12 in words; nothing of it run in Revit.** (a) both lines name a dry run: rolled back, so never kept and never asked about (`tests/test_ask_to_keep_health.py`). (b) and (c) reworded in the method's `purpose` alone - parsed before and after, no other field differs - saying what `dry_run` gives, that it is a count for the chat and never the modeller's yes, and for (c) that it is there only for a card `revit_change` runs at all. `tools/check-routing.py` and `tools/check-intrusion.py` printed the same before and after.

---

### Row 5b-1602

*Moved from the register on 2026-11-17.*

**Defect.** **A KEPT CHANGE SENT STRAIGHT AFTER A DRY RUN PRINTED *"AND N VALUES UNCHANGED FROM THE LAST REPLY"* - COUNTED AGAINST THE RUN THAT WAS ROLLED BACK.** Found 2026-11-12 in an independent review of #861, against a stand-in for Revit - **not seen in Revit**. The batch rule of [row 5b-407](../fragment-issues/section-5b-rows-401-425.md) leaves out a value the last reply of the same capability showed word for word. A dry run was remembered as that last reply, so the kept run after it - the one whose values describe what is in the model - printed its count of unchanged values in place of them, with nothing saying the reply it was measured against was of a run Revit no longer holds. The dry run's own closing sentence says to *"read its own counts"*.

**State.** **CLOSED 2026-11-12 in the server.** A dry run is outside the batch both ways: its values are printed whole, it is never remembered as the last reply (`Batch.forget`), and its verdict takes no part in the once-per-batch KEPT paragraph. `tests/test_mcp_serves.py` section 13, seen to fail by assertion before. **Not built, and proposed instead:** one line on a kept run comparing its counts with the dry run's before it - [proposal F44](../proposals/f44.md) says why the records Heron keeps cannot yet tell whose dry run it was.

---

### Row 5b-1611

*Moved from the register on 2026-11-17.*

**Defect.** **`heron-guard` REFUSES EVERY EDIT THAT NAMES THE REVIT API IN A FILE UNDER `revit/` WHEN THE SESSION WORKS IN A WORKTREE INSIDE THE CHECKOUT.** Found 2026-11-12 repairing `revit_undo_last` from `.claude/worktrees/<name>/`, in passing, so recorded, not repaired (AGENTS.md). Writing `revit/Heron.Revit.Addin/RevitUndo.cs` there was denied: *"This edit would put the Revit vendor namespace in .claude"*. The hook's command runs the copy of `bin/heron_guard.py` of the [`heron-guard`](../../.claude/skills/heron-guard/SKILL.md) skill under `$CLAUDE_PROJECT_DIR`, the main checkout, and `main()` takes the repository root from that file's own place - so a path inside a worktree is measured from the main checkout, and its first folder is `.claude`, never `revit`. **Reproduced by the review of that work:** the MAIN checkout's copy of the hook denies an edit to a path under a worktree's `revit/` (*in .claude*), and the worktree's OWN copy of the same script, given the same edit, allows it. The edit was right and the rule it guards was kept: the file is in `revit/` of its own checkout, and `tools/check-structure.py`, the gate the hook stands in front of, passes there. The same shape as [row 5b-1539](../fragment-issues/section-5b-rows-1526-1550.md), in the one walk that row did not reach. **What it cost:** every edit to an add-in file that carries a `using` of the API had to be written outside the editing tools and copied in, which is also how an edit gets past the hook's other check unseen.

**State.** **MOVED TO ISSUE [#940](https://github.com/Ajmalpshaik/Heron-AI/issues/940) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not changed.** The repair is the hook's: find the checkout the edited file belongs to (the nearest folder above it that holds `.git`, file or folder) and measure the path from there, with a case in `tests/test_heron_guard.py` that plants a worktree inside a checkout and fails on the hook as it stands - an edit under its `revit/` allowed, the same text under its `brain/` refused. Until then a session in a worktree writes such a file elsewhere and copies it in, and runs `python tools/check-structure.py` itself.

---

### Row 5b-1618

*Moved from the register on 2026-11-17.*

**Defect.** **A CHECK'S LABELS WERE HELD TO THEIR LIMITS ONLY WHEN SAVED THROUGH THE TOOL.** Found 2026-11-12 in an independent review of #866 ([D-159](../decisions/D-159.md)), which ran it against a stand-in for Revit - **not seen in Revit**. A checklist file made by hand or shared from another PC, with a 5,000-character, two-line `clause`, loaded (`_check_shape` checked only that it was text) and was printed into the chat's reply word for word; a carriage return in a clause passed the save's own check, which looked only for a line feed.

**State.** **CLOSED 2026-11-12 in the brain.** `_check_shape` holds the sentence and the labels to the save's limits, so such a file is refused as the loader refuses any other bad field - listed as one that could not be read, and `find` says why; a carriage return is refused at save. `tests/test_checklists.py` section 9, seen to fail by assertion on the code before the repair, then pass.

---

### Row 5b-1620

*Moved from the register on 2026-11-17.*

**Defect.** **IDS SENT WITH NO TOTAL, OR A TOTAL THAT IS NOT A NUMBER, WERE TAKEN AS ALL OF THEM - AND SELECT WAS OFFERED.** Found 2026-11-12 in an independent review of #866 ([D-159](../decisions/D-159.md)), which ran it against a stand-in for Revit - **not seen in Revit**. A reply whose `providesIds` list carried 2 of 9 ids and no numeric `total` was read as complete (`ids_from` fell back to the count of ids it was given), so Select in Revit would have taken part of what failed - the half-Select [D-158](../decisions/D-158.md) forbids.

**State.** **CLOSED 2026-11-12 in the brain.** A missing, non-numeric or true/false total is incomplete: no Select, and the row says Revit did not say how many the list holds. `tests/test_checklists.py` section 9 and `tests/test_checklist_tool.py`, seen to fail by assertion on the code before the repair, then pass.

---

### Row 5b-1622

*Moved from the register on 2026-11-17.*

**Defect.** **KEPT QA RUNS ARE ORDERED BY THE PC'S CLOCK, SO A CLOCK THAT GOES BACK ORDERS THEM WRONGLY.** Found 2026-11-12 in an independent review of #866 ([D-159](../decisions/D-159.md)), which ran it against a stand-in for Revit - **not seen in Revit**. A run's file name begins with the local time it was kept (`keep_run`), and the newest is found by sorting names. The end of summer time, or a clock corrected backwards, makes a later run sort before an earlier one: the review ran pass at 01:10, fail at 01:50, then a pass stamped 01:05 after the clock went back - and a pass at 01:20 was compared with the 01:50 fail and called **FIXED**, though the run made just before it had passed.

**State.** **CLOSED 2026-11-13 in the brain.** A kept run's file name begins with a number one higher than the highest kept in that model's folder (`n000012-...`), and runs are ordered by it (`_order`), never by the time; the time stays in the name to be read. A run kept by an earlier Heron has no number and sorts before every numbered one. `tests/test_checklists.py` section 10, seen to fail by assertion on the code before the repair, then pass. The review's own case - 01:10 pass, 01:50 fail, a pass stamped 01:05, a pass at 01:20 - now compares with the 01:05 pass, nothing FIXED.

---

### Row 5b-1623

*Moved from the register on 2026-11-17.*

**Defect.** **DELETING A QA CHECKLIST LEAVES ITS KEPT RUNS BEHIND - WITH THE MODEL'S FULL PATH, THE CHECK SENTENCES AND THE FAILING ELEMENT IDS.** Found 2026-11-12 in an independent review of #866 ([D-159](../decisions/D-159.md)), which ran it against a stand-in for Revit - **not seen in Revit**. `delete` removes the checklist's file and nothing under `checklists/runs/<checklist>/`. The files stay on the PC only - nothing leaves it - but a modeller who deleted a list would expect its history gone, and a list saved again under the same name is compared with the old one's runs.

**State.** **CLOSED 2026-11-13 in the brain.** `delete` removes the checklist's runs folder with it, and a folder that cannot be removed is said, never hidden. The chat's reply says the runs went too. `tests/test_checklists.py` section 10, seen to fail by assertion on the code before the repair, then pass.

---

### Row 5b-1624

*Moved from the register on 2026-11-17.*

**Defect.** **THE COMPANION'S POLL RETURNS EVERY ROW'S FAILING IDS ON EVERY POLL.** Found 2026-11-12 in an independent review of #866 ([D-159](../decisions/D-159.md)), which ran it against a stand-in for Revit - **not seen in Revit**. `/api/checklist` sends the held result whole, each row's `ids` with it - up to `IdsSentPerList` a row and `MAX_CHECKS` rows - each time the page polls, though the page uses them only for *Copy IDs* and the count. On this PC only, through the paired page; the cost is size, not reach.

**State.** **CLOSED 2026-11-13 in the Companion.** The page names the run whose ids it holds (`/api/checklist?have=<run>`), only once that run has finished; the poll then leaves every row's ids out and says so (`idsHeld`), and the page puts its own back. A running run, another run or a fresh page gets them whole. `tests/test_checklists.py` section 10, seen to fail by assertion on the code before the repair, then pass.

---

### Row 5b-1625

*Moved from the register on 2026-11-17.*

**Defect.** **A SUMMARY COMMENT IN `RevitFragment.cs` SITS ON THE WRONG MEMBER.** Found 2026-11-12 in an independent review of #866 ([D-159](../decisions/D-159.md)), which ran it against a stand-in for Revit - **not seen in Revit**. Around `IdsSentPerList` (lines near 1224 to 1230 of `revit/Heron.Revit.Addin/RevitFragment.cs`), a `/// <summary>` written for one member is stranded above another, so the IDE's tooltip for the constant describes something else. No behaviour changes.

**State.** **CLOSED 2026-11-13.** The summary moved onto `WithGrant`; a comment only, no behaviour changed. `tests/test_checklists.py` section 10, seen to fail by assertion on the code before the repair, then pass. `python tools/check-compile.py 2020 2024 2027` compiled every project on 2026-11-13.

---
