# FRAGMENT-ISSUES archive — section 5b, rows 1176 to 1200

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
> Rows 1176 to 1200 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-1176

*Moved from the register on 2026-11-17.*

**Defect.** **[D-138](../decisions/D-138.md) SAYS TWO THINGS ABOUT A PUBLISH WITH *ASK TO KEEP* OFF.** Found 2026-10-18 by the second docs pass. Its item 5: a publish is *"Still asked every time, grant or not"*. Its *What it accepts*: with Ask to Keep off *"A publish is then done without asking too"*. The code follows the second - `UnderTheGrant` in `RevitFragment.cs` shows no box when neither asking nor Ask to Keep is on - and [02](../02-architecture-overview.md) and [03](../03-heron-revit.md) were corrected to it on 2026-10-18. Decisions are append-only, so item 5's words stay.

**State.** **MOVED TO ISSUE [#915](https://github.com/Ajmalpshaik/Heron-AI/issues/915) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - the owner's.** A short decision that says item 5 reads *"while Ask to Keep is on"*, or a change to the code if item 5 is what was meant.

---

### Row 5b-1177

*Moved from the register on 2026-11-17.*

**Defect.** **THREE PLACES STILL WORD HERON AS IT WAS BEFORE [D-27](../decisions/D-27.md) AND THE STANDARDS TOOL.** Found 2026-10-18 by the second docs pass. `brain/heron_onboarding.py` (about lines 55 and 157) and `tools/check-metadata.py` (line 78) say the host chooses *"the wording and the level"*; D-27 has no level - nothing detects who is talking, only the shape of the request - and `tests/test_onboarding.py` line 186 asserts the sentence. `brain/heron_context.py` line 147 says the standards source *"does not exist yet"*; `heron_standards` serves it. [28](../28-agent-registry.md) marks `HERON-RPT-CMP-001` and `HERON-KRN-TRU-019` T2 though neither file makes a model call, by their own headers - the tier is the owner's.

**State.** **MOVED TO ISSUE [#937](https://github.com/Ajmalpshaik/Heron-AI/issues/937) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired.** Wording and one test line; the tiers are the owner's.

---

### Row 5b-1180

*Moved from the register on 2026-11-17.*

**Defect.** **THE OWNER'S ASK FOR ONE REAL NUMBERED SPECIFICATION DOCUMENT LIVES IN A WORK NOTE ONLY, SO `owner-queue.py` NEVER SHOWS IT.** Found 2026-10-18 by the second docs pass. It is A3 in `work-notes/plans/next-steps-2026-09-12.md`; no register row carries it, and [FOR-THE-OWNER](../FOR-THE-OWNER.md) §4 now says so. A work note is deleted at the end of its life.

**State.** **MOVED TO ISSUE [#937](https://github.com/Ajmalpshaik/Heron-AI/issues/937) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded.** A register row, so the ask outlives the note.

---

### Row 5b-1181

*Moved from the register on 2026-11-17.*

**Defect.** **THIS FILE HOLDS ROWS FAR OUTSIDE ITS BAND OF 25.** Found 2026-10-18 by the second docs pass. `section-5b-rows-401-425.md` holds rows up to this one and rows 393 to 395, against FRAGMENT-ISSUES' rule of files of 25 by number. The split that fixes it is `split-register.py fragment-issues`, and [row 5b-1169](../fragment-issues/section-5b-rows-1151-1175.md) records why it refuses today: it names a rows file for 1000 and above with four digits and its read-back expects three. It would also re-point links in `docs/decisions/`.

**State.** **MOVED TO ISSUE [#937](https://github.com/Ajmalpshaik/Heron-AI/issues/937) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - the file has stopped growing, and the re-band is still owed.** 2026-11-12, [row 5b-1585](../fragment-issues/row-5b-1585.md): from row 5b-1585 on a new row is a file of its own, so nothing is appended here any more and the reason the split could never be run - every open branch was adding rows to this file - ends when the branches cut before that day have merged. **Not done:** the rows this file holds outside its band are where they were; no row was moved in that change. 5b-1169 and 5b-1509 are both repaired, so what is left is the run itself: `python tools/split-register.py fragment-issues --write`, a change of its own, when no open branch edits this file - `python tools/next-id.py` lists the branches and the rows each holds. It re-points the links in `docs/decisions/` and every other document, and leaves the rows' own files as they are.

---

### Row 5b-1182

*Moved from the register on 2026-11-17.*

**Defect.** **THREE COMPANION SETTINGS CARDS WOULD BE REFUSED IF THE CHANGES PANEL CAME BACK.** Found 2026-10-18 by the second docs pass. Two of the eight `companion: settings` cards, `set-global-parameter` and `set-view-filter-categories`, and `REPORT_CATEGORY_OVERRIDES` are DRAFT; their Apply goes through `revit_change`, which keeps a change only from a PROVEN fragment ([D-117](../decisions/D-117.md)). Unreachable today because `SHOW_CHANGES` is off; the same cause as [row 5b-1129](../fragment-issues/section-5b-rows-1126-1150.md).

**State.** **CLOSED 2026-11-16** - found finished while the open rows were moved to GitHub issues: all three cards are PROVEN now - set-global-parameter and report-category-overrides (proofs 2026-11-02), set-view-filter-categories (2026-10-18) - and none is on check-signatures' stale list, so revit_change keeps their changes. Was: **OPEN - latent.** Proved, or the panel told, before `SHOW_CHANGES` is turned on.

---
