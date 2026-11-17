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

### Row 5b-1576

*Moved from the register on 2026-11-17.*

**Defect.** **A 2D ARC'S OR CIRCLE'S CENTRE COULD NOT BE LOCKED TO REFERENCE PLANES IN A FAMILY - `LOCK_FAMILY_LINES_TO_PLANES` LOCKED ONLY ALONG A LINE AND BY ITS ENDS.** The gap table's row 3e ([2026-11-11-family-2d-capabilities](../work-notes/investigations/2026-11-11-family-2d-capabilities.md)): a symbol's circle could be drawn and its radius labelled (`LABEL_FAMILY_RADIUS` v6), but nothing kept its centre on the planes a size moves; `LOCK_FORM_SKETCH_TO_PLANES` does it for a 3D form's sketch only. Built 2026-11-12 on the owner's word ("make the arc centre lock tool"): **version 3 (DRAFT)** locks an arc's centre - or a whole circle's, a closed curve now taken for its centre alone - to each plane named that it lies on, by `CurveElement.CenterPointReference`; a hidden Center Mark gives no centre reference, so it is switched on first and said; an ellipse or a spline is still refused. Compiled 2020, 2024 and 2027. **Run in Revit 2024** (session 13164, the SAVED scratch family `%TEMP%\heron-work\rev2024\R24 Arc Lock.rfa`, rolled back - Group NQ, NQ1 and NQ2): an arc centred (200, 100) radius 100, planes "Arc X, Arc Y" - 4 locks, its centre to both planes and its two ends to "Arc Y", ends read back unchanged; planes "Spare" refused, the centre named 300 mm off, nothing locked. **Kept** (NQ3, `try-once`, the owner pressed Keep): the arc and the two halves of a circle "circle 200,100 50" - 12 locks, each centre to both planes. **It follows** (NQ4): "Arc X" set to 300, every centre read back at (300, 100), radii unchanged, an unlocked shaft unmoved; the negative (`validate --write --kept`, "Spare") refused on that state. A closed circle is NOT RUN: Heron draws a circle as two half-arcs, never one closed curve. Proof draft written.

**State.** **FIXED - PROVEN on Revit 2024, signed by Ajmal PS 2026-11-12; Revit 2020 added the same day (`proof.releases`, signed); a closed circle drawn by hand held on 2024 (NQ5).**

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

**State.** **MOVED TO ISSUE [#960](https://github.com/Ajmalpshaik/Heron-AI/issues/960) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not changed.** Two parts. The line: write it without the colon and space - *"named by the modeller as Family and Type, 'Family: Type'"* inside quotes, or the whole item in double quotes. The hole: `heron_skill.validate()` should refuse a precondition or an utterance that is not text, so the next one is named by `python brain/heron_skill.py` and by `tests/test_skills.py`. Until then `tests/test_brain_reachable.py` holds it for three methods only - `annotate-a-view`, `system-colours` and `hvac-spaces-and-zones`.

---

### Row 5b-1587

*Moved from the register on 2026-11-17.*

**Defect.** **`open-defects.py` EXITS 0 WHEN IT COULD NOT READ THE REGISTER AT ALL, SO ANYTHING THAT RUNS IT FOR ITS EXIT CODE IS TOLD NOTHING IS WRONG.** Found 2026-11-12 by the review of [row 5b-1585](../fragment-issues/row-5b-1585.md), in passing, so recorded, not changed (AGENTS.md). When `tools/register-text.py` refuses the register - `RegisterBroken`: a file the page names that is missing, or since row 5b-1585 a row's own file that is not what its name says - `tools/open-defects.py` prints `Could not read the register: ...` once for each section, prints no row, no id and none of its three TOTAL lines, and returns 0 (`main`: *"if not seen: return 0"*; its docstring: *"Always exits 0. This reports; it does not gate."*). Seen on a scratch copy with `row-5b-1564-copy.md` planted beside the row's own file: two lines of *Could not read*, exit 0. A report that found nothing to report and a report that could not look exit alike. **What does turn red on the same tree, since row 5b-1585:** `python tools/check-docs.py` section 10 names the file, and `tests/test_open_defects.py` fails one check in a sentence that says to run it - until that row's review it died there with AttributeError. `tools/balance-of-work.py` reads this tool's printed totals with `grab()`, which gives None when a line is missing, and its docstring says a None is printed as not derived rather than as a zero - READ, not run on a broken register. Not checked: whether anything outside this repository runs `open-defects.py` and reads its exit code.

**State.** **MOVED TO ISSUE [#938](https://github.com/Ajmalpshaik/Heron-AI/issues/938) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not changed.** The choice is the tool's contract, and it is a person's: keep *always exits 0* and say so louder - the one line it prints is easy to lose above nothing - or give *could not read the register* an exit code of its own (2, as `register-text.py`'s own command line and `next-id.py` use for *could not look*), which changes what *"Always exits 0"* promises its callers. Either way with a case in `tests/test_open_defects.py` that plants a row's file the reader refuses and fails on the tool as it stands.

---

### Row 5b-1589

*Moved from the register on 2026-11-17.*

**Defect.** **[`SYNC_SPACES_WITH_LINKED_ROOMS`](../../brain/fragments/sync-spaces-with-linked-rooms/fragment.yaml) TAKES A PHASE FOR THE SPACES IT MAKES AND NEVER USES IT WHEN IT LOOKS FOR THE SPACES AND ROOMS ALREADY THERE - SO WITH ANY PHASE BUT THE MODEL'S LAST, A SECOND RUN CANNOT SEE WHAT THE FIRST ONE MADE.** Found 2026-11-12 while the [`hvac-spaces-and-zones`](../../brain/skills/hvac-spaces-and-zones.yaml) method was written from the card, so recorded, not changed (AGENTS.md). **READ off the code and Revit's own API reference; NOT RUN - no Revit in a cloud session.** The card makes each new space with `doc.Create.NewSpace(onLevel, onPhase, ...)`, `onPhase` being `phaseName` or, left empty, the model's last phase. Three lookups in the same file name no phase: (a) `doc.GetSpaceAtPoint(point)`, which decides whether a room already has a space - `RevitAPI.xml` of the Revit 2020 reference package says of that overload *"Surveys only the spaces from the final phase of the project"*, and an overload taking the phase exists beside it; (b) `linked.GetRoomAtPoint(point)`, which decides whether a space is an orphan - the same sentence, for the LINK's final phase; (c) the rooms are collected from the link with no phase test at all. **What follows, by reading:** with `phaseName` a phase that is not the model's last, a space made on the first run is not found on the second, so it is neither counted in `alreadyMatching` nor renamed with `updateNames`, and `NewSpace` is called at the same point again - whether Revit then makes a second space in the same region or refuses is not known; a room of an earlier phase in the architect's model is given a space; and a space standing over such a room is listed in `orphans`, since the link's final phase holds no room there. With `phaseName` empty and one phase in both models - the proof's case, 2026-10-18 - none of this shows. The card's text says nothing of it. The method tells the modeller at its step 4, and says to run that step once when a phase other than the last is named.

**State.** **MOVED TO ISSUE [#951](https://github.com/Ajmalpshaik/Heron-AI/issues/951) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not changed.** The card is PROVEN; changing its `impl/` makes the proof stale, and the fix is small: pass `onPhase` to `GetSpaceAtPoint`, read the link's rooms and `GetRoomAtPoint` in the link's phase that matches by name (or say which phase was read), and say in `findings` which phase each lookup used. NEEDS REAL REVIT first, to see what a second run does: [NEEDS-CHECKING Group NR](../needs-checking/group-nr.md), NR9.

---

### Row 5b-1594

*Moved from the register on 2026-11-17.*

**Defect.** **THE ADD-IN RUNS A PUBLISH, ADMIN OR EXECUTE FRAGMENT SENT WITHOUT `apply` AND ROLLS BACK WHAT IT CAN - [row 5b-1593](../fragment-issues/row-5b-1593.md)'S REFUSAL IS THE SERVER'S ALONE.** Read 2026-11-12 off [`RevitFragment.cs`](../../revit/Heron.Revit.Addin/RevitFragment.cs) (`Run`: `apply` is true only for `"apply": "true"`, and nothing there refuses its absence), **not run in Revit**. The add-in never sees a card's risk (row 9, Golden Rule 19), so it cannot tell an EXECUTE card from a MODIFY one; it does know the operation - `run_fragment_publish`, `run_fragment_admin` - and whether the card declares `noTransactionOpen`, and it refuses neither when `apply` is absent. A caller that skips Heron's server gets a "rolled back" verdict for an export that left a file. **Deliberately not changed with the server's repair:** a run without `apply` is how every proof is taken (D-55), and `validate --write --allow-publish` proves a PUBLISH or ADMIN card exactly that way - refusing it in the add-in would stop those proofs, and needs a rebuild and a Revit restart on every release.

**State.** **MOVED TO ISSUE [#911](https://github.com/Ajmalpshaik/Heron-AI/issues/911) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired.** For the owner: whether the add-in's verdict for a `run_fragment_publish` or `run_fragment_admin` run without `apply`, and for a `noTransactionOpen` one, should stop saying *"NOTHING WAS KEPT"* of work a rollback cannot return - a sentence change in `WithVerdict`, which the proving tools read (`heron_validate`), so not a small one.

---

### Row 5b-1597

*Moved from the register on 2026-11-17.*

**Defect.** **A `readBack` HERON COULD NOT READ COST A KEPT CHANGE ITS WHOLE REPLY.** Found 2026-11-12 in an independent review of #861, against a stand-in for Revit - **not seen in Revit**. [D-156](../decisions/D-156.md) item 6 says the read-back *"changes nothing and can break nothing"*. On the chat's side `read_back_lines` iterated and converted what it was given: `elements: 5` and `values: 7` raised TypeError, `valuesMore: "many"` and `elementsMore: "x"` raised ValueError - after the request had left, so the door answered *"revit_change broke in Heron's own code ... OUTCOME UNKNOWN"* and the KEPT verdict, the counts and the undo rule the add-in had sent were gone. A count sent as text was printed as it came. No add-in sends such a field today; one of another age, or a reply cut short, could.

**State.** **CLOSED 2026-11-12 in the server.** The field is checked whole before a line is written - every count a whole number, `elements` a list of elements, each one's `values` a list - and one that fails prints a single line, *before -> after: not shown - the read-back Revit's Heron sent could not be read (...)*, with the rest of the reply untouched. Caught narrowly (`tools/check-narrow-errors.py`): the reader's own exception, TypeError, ValueError and AttributeError. `tests/test_read_back.py` section 9, and `tests/test_mcp_serves.py` through the door, on a KEPT reply.

---

### Row 5b-1598

*Moved from the register on 2026-11-17.*

**Defect.** **THE READ-BACK'S WORDS CLAIMED WHAT NOBODY CHECKED, SAID IT ON EVERY WRITE, AND SENT A KEPT CHANGE TO BE RUN TWICE.** Found 2026-11-12 in an independent review of #861, against a stand-in for Revit - **not seen in Revit**. Three sentences of `read_back_lines` ([D-156](../decisions/D-156.md)): (a) an element with no differing value was called *"regenerated, or hosted or joined to what moved"* - a cause Heron never looks for; (b) what a kept change did not read ended *"a dry run shows them all"* - untrue past the add-in's limit of `HeronReadBack.MostRead`, and printed on every kept change with nothing selected: a standing nudge toward the two-run habit [D-126](../decisions/D-126.md) refused as the way a change is approved; (c) every write, an add-only one included, printed *"before -> after: 0 of 0 changed element(s) read on both sides"*.

**State.** **CLOSED 2026-11-12 in the server.** (a) reads *"show no difference in any value Heron reads - why Revit counted them, Heron does not know"*. (b) says why and the limit - *"a kept change reads only what was selected before it ran (up to N elements)"*, the number read from `READ_BACK_MOST_READ`, which `tests/test_read_back.py` holds equal to the add-in's - and names no dry run. (c) a write Revit counted no modified element in prints nothing of a read-back, and one with none of it read prints one line. **One existing assertion changed, because it pinned (b):** `tests/test_read_back.py`'s *"...; a dry run shows them all"*. D-156's own words are unchanged; its record carries a dated note.

---

### Row 5b-1600

*Moved from the register on 2026-11-17.*

**Defect.** **`revit_change`'S DESCRIPTION SAID A PUBLISH OR ADMIN CAPABILITY *"NEEDS THAT SWITCH ON TOO"* - WHICH READS AS THE CHANGES SWITCH.** Found 2026-11-12 in an independent review of #861 - **not seen in Revit**. The sentence was cut on 2026-11-12 to make room for `dry_run` inside ALWAYS_LOADED's budget ([D-118](../decisions/D-118.md)): *"It runs only while the owner has Changes ON, and a Publish or Admin capability needs that switch on too."* The only switch named before it is Changes; D-106 gives each level a switch of its own.

**State.** **CLOSED 2026-11-12.** *"needs its own switch too"* - the same length, so ALWAYS_LOADED's size is what it was (measured 2026-11-12 with `python tests/test_tool_sizes.py`: 4,498 characters before and after). `tests/test_tool_sizes.py` holds the phrase among what a trim may never take.

---
