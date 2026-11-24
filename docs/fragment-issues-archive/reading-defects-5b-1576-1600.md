# FRAGMENT-ISSUES archive — section 5b, rows 1576 to 1600

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
> Rows 1576 to 1600 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-1579

*Moved from the register on 2026-11-17.*

**Defect.** **AN UNLOCKED DETAIL LINE IN A FAMILY FOLLOWS ITS PLANES ANYWAY - REVIT'S AUTOMATIC SKETCH DIMENSIONS - SO GROUP NC'S "CONTROL" LINE PROVES NOTHING; AND A LOCK THAT REPLACES THOSE DIMENSIONS RAISES HERON'S DELETE BOX.** Found 2026-11-12 in Group NC, Run 2 (Revit 2024, session 44572, the SAVED scratch family `%TEMP%\heron-work\rev2024\R24 Line Lock.rfa`). (1) The control "path 0,-200; 300,-200", never locked, read (0, -200) to (450, -200) at Length 900 and to (150, -200) at 300 - `REPORT_FAMILY_FORMS` lists it "dimensioned (not locked) to Head Base": Revit's own Automatic Sketch Dimensions carry it. The plan's expectation that it holds still was wrong; `tests/cases.yaml` now says so, and a lock is evidenced by its read-back ("locked along", "locked at an end"), not by an unlocked line standing still. (2) `LOCK_FAMILY_LINES_TO_PLANES` KEPT through `revit_change` deletes the Automatic Sketch Dimensions its locks replace (8 for the arrow, 4 for the arc in Group NQ's kept run), and Heron's delete box asks first, naming "Automatic Sketch Dimensions 8" (the owner's screenshot). Correct behaviour, but it reads as alarming: whether the box should pass Revit's own automatic dimensions without asking is the owner's decision, not changed here.

**State.** **MOVED TO ISSUE [#910](https://github.com/Ajmalpshaik/Heron-AI/issues/910) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded; the test wording fixed; the delete-box question is the owner's.**

---

### Row 5b-1582

*Moved from the register on 2026-11-17.*

**Defect.** **`owner-queue.py` READS NO DECISION, SO A DECISION STILL *Proposed* - WRITTEN DOWN FOR THE OWNER TO ACCEPT OR OVERTURN - IS ON NO LINE OF THE QUEUE AS A THING TO DECIDE, AND ITS FOOT DOES NOT SAY IT LEAVES THEM OUT.** Found 2026-11-12 writing the handover of that day, in passing, so recorded, not repaired (AGENTS.md). [`docs/FOR-THE-OWNER.md`](../FOR-THE-OWNER.md) opens *Everything waiting on you, in one place*, and its section 1 says the tool reads OPEN-QUESTIONS, NEEDS-CHECKING and PROPOSALS. [`tools/owner-queue.py`](../../tools/owner-queue.py) reads those three and nothing else, and its foot - *NOT COVERED HERE, because no register owns them as rows* - names open defects, the plans and the fragment library. A decision whose Status line says Proposed is waiting on the owner by its own words: [D-153](../decisions/D-153.md) says *the owner has not read this back*, and [D-154](../decisions/D-154.md) *nothing is retired under it until he accepts it*. **Measured 2026-11-12:** `grep -l 'Status:\*\* Proposed' docs/decisions/*.md` named fourteen files; the tool's output named three of those decisions, each only inside the words of a NEEDS-CHECKING row (D-14 in row R1b, D-112, D-146), and none as a thing to decide. D-153 and D-154 were not in it at all. So neither place an owner is sent for what he owes holds these, and [`docs/HANDOVER.md`](../HANDOVER.md) section 8 names them by hand - a typed list, which is what the tool exists to replace. Not checked: which of the fourteen the owner still means to read back; some are old enough that the Status line may be the stale part.

**State.** **MOVED TO ISSUE [#914](https://github.com/Ajmalpshaik/Heron-AI/issues/914) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired; part of it is the owner's.** The tool could read `docs/decisions/` for a Status line that begins *Proposed* and print each under A DECISION, or name them at its foot with the `grep` above; which, and whether an old Proposed decision is still a question, is his to say. Until then HANDOVER section 8 names the ones each sitting adds.

---

### Row 5b-1584

*Moved from the register on 2026-11-17.*

**Defect.** **A PRECONDITION WITH A COLON AND A SPACE IN IT IS NOT A SENTENCE TO YAML, IT IS A MAPPING - AND `heron_method` SERVES IT AS ONE: [`sheets-from-excel`](../../brain/skills/sheets-from-excel.yaml) HANDS A CHAT A PYTHON DICT WHERE ITS TITLE BLOCK PRECONDITION SHOULD BE.** Found 2026-11-12 while a precondition of [`hvac-spaces-and-zones`](../../brain/skills/hvac-spaces-and-zones.yaml) was reworded and made the same mistake; that one was caught by a new check and reworded in the same change, and every skill was then read for it. One other has it: `sheets-from-excel`'s line *"the title block is in the file or named by the modeller as Family: Type - never assumed (D-33)"* parses as `{'the title block is in the file or named by the modeller as Family': 'Type - never assumed (D-33)'}`, and `heron_brain.method("sheets-from-excel")["preconditions"]` returns that dict - READ from the seam on this date, not seen in a chat. `heron_method` prints each precondition with `"  - %s"`, so the modeller's chat is shown the braces. Nothing fails: `heron_skill.validate()` asks only that `preconditions` is not empty, `tests/test_skills.py` the same, and no suite reads an item's type. Recorded, not changed (AGENTS.md): the fix is one line outside the change it was found in.

**State.** **MOVED TO ISSUE [#960](https://github.com/Ajmalpshaik/Heron-AI/issues/960) 2026-11-16** - still open, tracked there; this row is kept whole. Part done 2026-11-19: the precondition is quoted, and tests/test_skills.py fails on any skill whose preconditions or utterances are not text. Still owed: `heron_skill.validate()` refusing the same, in brain/heron_skill.py. Was: **OPEN - recorded, not changed.** Two parts. The line: write it without the colon and space - *"named by the modeller as Family and Type, 'Family: Type'"* inside quotes, or the whole item in double quotes. The hole: `heron_skill.validate()` should refuse a precondition or an utterance that is not text, so the next one is named by `python brain/heron_skill.py` and by `tests/test_skills.py`. Until then `tests/test_brain_reachable.py` holds it for three methods only - `annotate-a-view`, `system-colours` and `hvac-spaces-and-zones`.
