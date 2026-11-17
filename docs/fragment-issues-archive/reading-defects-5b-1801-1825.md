# FRAGMENT-ISSUES archive — section 5b, rows 1801 to 1825

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
> Rows 1801 to 1825 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-1801

*Moved from the register on 2026-11-17.*

**Defect.** **TWO PROVEN CARDS READ A SECTION'S CUT PLANE IN TWO PLACES: `CREATE_SECTION_VIEW` BUILDS ITS CROP BOX WITH THE CUT AT THE BOX'S NEAR FACE (Max.Z), AND `MOVE_VIEW_CUT_PLANE` READS AND MOVES THE CUT AT `View.Origin`. WHERE THE TWO DIFFER, ONE OF THEM IS WRONG ABOUT WHERE REVIT DRAWS THE SECTION LINE.** Found 2026-11-16 by review of the [row 5b-1736](../fragment-issues/row-5b-1736.md) repair, in code, not in Revit. `brain/fragments/create-section-view/impl/any/fragment.cs` (PROVEN) says of its box *"Max.Z the near side, at the cut plane"* and sets Max.Z to half the elements' depth plus the margin - so a section made on a 5000 mm wall with `marginMm=5000` would cut 7500 mm past the wall's middle, past the building, if that comment is right. `brain/fragments/move-view-cut-plane/impl/any/fragment.cs` (PROVEN) measures the cut from `View.Origin` (*"now cuts through"*). For a section drawn by hand the two are thought to coincide (Max.Z 0 at the origin); **not read here**. `FILTER_ELEMENTS_BY_CATEGORY` v7's `cutByView` takes Max.Z, and its finding prints the plane it used.

**State.** **MOVED TO ISSUE [#908](https://github.com/Ajmalpshaik/Heron-AI/issues/908) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired.** [Group OC](../needs-checking/group-oc.md) OC0 (10) and OC7 settle it on Revit 2024: a section drawn by hand at a known line, read with `REPORT_VIEW_SETTINGS` (its *"cuts through"* point, `View.Origin`), and v7's printed plane (the crop's Max.Z) compared with both and with the drawn line. Then whichever card reads it wrong is repaired, and `CREATE_SECTION_VIEW` is checked on W5 for where its line lands. Both cards are PROVEN and other sessions may hold them; not changed here.

---

### Row 5b-1821

*Moved from the register on 2026-11-17.*

**Defect.** **THE RUN RECORD A CHAT READS BACK (`heron_run_record`, D-161) STILL SAYS *KEPT* FOR A CHANGE WHOSE OUTCOME LINE NOW SAYS *NOTHING CHANGED* OR *REFUSED* (D-168): `heron_run_records._ended` WORDS IT FROM THE ADD-IN'S `verdictKind` ALONE.** Found 2026-11-16 by the review of PR #888, read in the code: `mcp/server/heron_run_records.py`, `_ended(reply)` - *"if kind == \"kept\": return \"KEPT\""* - which the record's opening line uses (`record.get("at") ... _ended(reply)`). D-168 reads the same reply with the card's declared results (`heron_server_core.kept_nothing`) at the door, so the reply a chat was given and the record it can read later name the same call two ways. The model and the undo list are not touched; only the record's words differ. Not run.

**State.** **FIXED IN CODE - repaired 2026-11-16, the door's own reading.** `heron_run_record` reads a kept record with `kept_nothing` and the card the record names (`fragment`, its folder - read on a real record on the owner's PC), and hands the word to `heron_run_records`, which stays free of the card: the opening line and the list of newest records say NOTHING CHANGED or REFUSED where the door did. Fails closed: a card renamed or retired since the run, a name that is not a folder name, or any other word reads KEPT, so a record of a card read differently now than at the call can still disagree - narrowed, not closed. `tests/test_run_records.py` (CI) and `tests/test_outcome_line.py` (the real server, on the owner's PC) check it. NOT TOUCHED: the record's own top-level `kind`, which the add-in writes and Revit's Activity pane reads (C#), still says kept. Needs the Claude app restarted to reach a chat

---

### Row 5b-1822

*Moved from the register on 2026-11-17.*

**Defect.** **`tools/change-evidence.py capture --gaps` CANNOT RECORD `check-gaps` ON A MACHINE WHERE THE SUITES TAKE OVER HALF AN HOUR - EVERY GATE RUNS UNDER A FIXED 1800-SECOND CAP (`run(cmd, timeout=1800)`), AND `check-gaps` RUNS EVERY SUITE.** Found 2026-11-16 in a cloud container recording the evidence for the `built_on:` change ([NEEDS-CHECKING Group OF](../needs-checking/group-of.md)): the record says `check-gaps NOT RUN - timed out after 1800 seconds`, after `--tests all` alone had taken about 40 minutes in the same capture. `tools/check-change.py` then rules REVISE, *"owed and not run: check-gaps"*, for every change whose parts owe it, and `--stated` is the wrong way round it - the tool's own comment says a measurement filed as a claim is what `--stated` exists not to be.

**State.** **MOVED TO ISSUE [#939](https://github.com/Ajmalpshaik/Heron-AI/issues/939) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN.** Not fixed here: found while doing something else. A shape for whoever takes it: a longer cap for `check-gaps` alone, or `--gaps` reading the suite results `--tests all` already took.

---
