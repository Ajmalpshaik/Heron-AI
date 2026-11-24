# FRAGMENT-ISSUES archive — section 5b, rows 1876 to 1900

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
> Rows 1876 to 1900 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-1878

*Moved from the register on 2026-11-24.*

**Defect.** **THREE PAIRS OF QA AND DATUM CARDS ARE ONE JOB MADE AS TWO (OVERLAP) - A MERGE FOR A LATER SESSION, NOT DONE IN ROW 5b-472.** Found 2026-11-20 by the duplicate check row 5b-472's answer asked for (the `heron-duplicate-checker` method: `brain/heron_merge.py` first - 610 live cards, four signature groups, none of them QA or datum cards - then each near pair's card and C# read). No pair read was SAME JOB. **(A) `CHECK_VERTICAL_CLEARANCE` into `CHECK_MINIMUM_CLEARANCE`:** vertical measures only pairs whose plan boxes overlap (`check-vertical-clearance/impl/any/fragment.cs`, the two `continue` lines on X and Y), and for exactly those pairs minimum's box gap is already the Z gap; one `stackedOnly` option on the keeper would cover vertical's which-is-on-top and overlap-depth answers, and vertical would gain insulation, links and selectable ids. Opposite rule to settle first: where two boxes overlap, minimum reports 0 mm and vertical reports CLASH with the depth. **(B) `CHECK_SLEEVE_SIZE` into `AUDIT_MEP_OPENINGS`:** both pair a hole with the pipe, duct, tray or conduit through it by box on the same four categories, and both report a hole with nothing through it; sleeve size stops at the FIRST service its box test finds (`check-sleeve-size/impl/any/fragment.cs`, `through = service; break;`), so a sleeve with two pipes is sized against one of them without a word, where audit already reports that case as COMBINED. Both cards say place and size are separate questions - if the owner holds to that, this is a LOOK-ALIKE and the `break` is a defect of its own (row 5b-1879 item 1). **(C) the linked-level lines of `REPORT_LEVEL_ELEVATIONS` and `CHECK_DATUMS_AGAINST_LINK`:** both carry a link's levels into this model and match them by name; elevations measures a link placed more than once through its first placement, datums refuses until one placement is named. Both are DRAFT, so a C# change there costs no proof. **(A) and (B) change the keeper's C#, which sends a PROVEN card back to DRAFT** - not small and certain, so nothing was merged. Verdicts are the checker's reading of the cards and C#; nothing was run in Revit.

**State.** **MOVED TO ISSUE [#1088](https://github.com/Ajmalpshaik/Heron-AI/issues/1088) 2026-11-20** - still open, tracked there. Was: **OPEN 2026-11-20** - the owner's yes for each merge, then `heron-fragment-merger`; (A) and (C) also need his choice of the opposite rule named.

---

### Row 5b-1879

*Moved from the register on 2026-11-24.*

**Defect.** **FIVE DISAGREEMENTS BETWEEN QA, OPENING AND DATUM CARDS, FOUND BY ROW 5b-472's DUPLICATE CHECK - RECORDED, NOT FIXED.** Found 2026-11-20 reading the cards' C# (`heron-duplicate-checker` method; lines named are on `main` at `de6dcbad`). **(1)** `CHECK_SLEEVE_SIZE` checks only the first service through a sleeve: `through = service; break;` in its `impl/any/fragment.cs` - a sleeve two pipes pass through is judged against one. **(2)** `PROPOSE_MEP_OPENINGS` and `CHECK_SLEEVE_SIZE` size a hole differently: propose is service + 2 x allowance with no insulation term (no `insulat` in its card or C#), sleeve size requires + 2 x insulation too - a hole proposed for a lagged pipe reads UNDERSIZED when the sleeve check runs with the same gap. **(3)** Two pattern languages for a name: `CHECK_FAMILY_STANDARDS` reads `namePattern` as *contains* on the family's name (`IndexOf(namePattern, ...) < 0` fails it), `CHECK_MODEL_STANDARDS` reads `type=` as a `*` wildcard on the whole name - `TRG` passes any name holding TRG in the first and only a name that is exactly TRG in the second. **(4)** A name already taken: `CREATE_PROJECT_REFERENCE_PLANES` skips it and makes the rest (its header comment, *SKIPPED AND NAMED, NEVER MADE TWICE*), `CREATE_GRIDS` and `CREATE_LEVELS` refuse the whole call. **(5)** A link placed more than once: `REPORT_LEVEL_ELEVATIONS` measures through the first placement and says so, `CHECK_DATUMS_AGAINST_LINK` refuses until one is named. Items 1, 2, 3 and the reference-plane half of 4 were read in the C# by this session as well; the rest is the checker's reading. None was run in Revit.

**State.** **MOVED TO ISSUE [#1088](https://github.com/Ajmalpshaik/Heron-AI/issues/1088) 2026-11-20** - still open, tracked there. Was: **OPEN 2026-11-20** - each a fix of its own, or the owner's choice of one rule where two cards disagree (3, 4, 5). Item 1 changes PROVEN C#.

---

### Row 5b-1889

*Moved from the register on 2026-11-24.*

**Defect.** **A WARNING REVIT RAISES IN A CALL IS DISMISSED AND COUNTED, BUT NOT TIED TO THE ROW THAT CAUSED IT (issue [#1051](https://github.com/Ajmalpshaik/Heron-AI/issues/1051) item A4).** Seen 2026-11-19 on Revit 2020: *"Line in Sketch is slightly off axis"* (4 elements) on three `CREATE_FAMILY_FORMS` calls; Heron dismissed it and reported the count, not which rows - so the caller cannot tell which form to fix. The cause there was the caller's own 0.1 mm rounding on 45-degree braces, not Heron. **Not a fragment fix:** the warning is caught and worded by the add-in's failure handling ([`HeronFailureNote.cs`](../../revit/Heron.Revit.Addin/HeronFailureNote.cs)), which another area owns; naming the elements of each dismissed warning there would let any card's reply say which of its own results they are. Not guessed here: Revit's threshold for *slightly off axis*.

**State.** **MOVED TO ISSUE [#1112](https://github.com/Ajmalpshaik/Heron-AI/issues/1112) 2026-11-20** - still open, tracked there. **Fixed in code 2026-11-21, NOT RUN IN REVIT** (Ajmal PS allowed `revit/` for this row that day): every failure the fragment path reads now carries the ids Revit named, as `ElementId.ToString()` prints them; `HeronFailureNote` keeps each kind's ids once each, in the order first named, and the verdict names the first 5 - *"(2 times, 4 elements, element ids 352621, 352622, 352630)"* - while `warningsDismissed[].elementIds` lists up to 50 with `elementIdsMore` beside a list cut short, written by `HeronFailureNote.Listed`, the one writer; a refusal names its elements the same way, and the run record's `warning` line lists them. A warning whose ids could not be read reads exactly as before. `tests/Heron.FailureNote.TestHost` section 12 and `tests/test_failure_note.py` section 3e - red on the code before; `tests/test_run_records.py`. Compiles for 2020 to 2027. Owed: NEEDS-CHECKING [Group PM](../needs-checking/group-pm.md) PM1-PM2, issue [#1214](https://github.com/Ajmalpshaik/Heron-AI/issues/1214); the row closes after them. Still not guessed: Revit's threshold for *slightly off axis*, and whether a sketch line Revit names outlives the form it made (PM1 writes it down). Was: **OPEN 2026-11-20** - recorded from the issue, for whoever owns the add-in's failure handling.

---

### Row 5b-1890

*Moved from the register on 2026-11-24.*

**Defect.** **`revit_view_image` OF A TALL, SLIM FAMILY COMES BACK AS A SLIVER, AND WITH `elements=` IT FRAMES ANY LARGE SOLID CROSSING THE SECTION BOX (issue [#1051](https://github.com/Ajmalpshaik/Heron-AI/issues/1051) item A5).** Seen 2026-11-19 on Revit 2020: View 1 of a rod hanger about 760 x 140 mm came back 1000 x 5159 px; with `elements=` the frame zoomed out to a context slab crossing the section box, so the small parts could not be shown close up. The Left elevation and an element picture were taken instead. Wanted: the height or aspect capped, and `elements=` framed on the elements asked for. **Not a family-editor fix:** `revit_view_image` is the view-picture tool, which another area owns.

**State.** **MOVED TO ISSUE [#1112](https://github.com/Ajmalpshaik/Heron-AI/issues/1112) 2026-11-20** - still open, tracked there. **Fixed in code 2026-11-21, NOT RUN IN REVIT** (Ajmal PS allowed `revit/` for this row that day): (a) THE LONGEST SIDE IS THE WIDTH ASKED - `RevitCapture` reads the exported PNG's size from its header (`HeronPicture.Size`) and, when it came back taller than the width asked (`HeronPicture.TooTall`), exports it again fitted on its height (`FitDirectionType.Vertical`, in the same folder, group and run-guard door); the reply sends `pictureWidth`, `pictureHeight`, `fittedOn`, and `refitFailed` when Revit refused the second export, which then sends the first picture; the chat says *"1000 px tall"* and why. (b) IN A FAMILY, `elements=` hides every model element not asked for whose extent reaches out past a face of the section box (`HeronPicture.ReachesOut`, `HideReachingOut`) and names it, `hiddenCrossing` - the move row 5b-1327 made for connectors; what lies wholly inside the box stays, and a project is unchanged. `tests/Heron.Picture.TestHost` and `tests/test_picture_fit.py` (new), `tests/test_mcp_serves.py` - red on the code before. Compiles for 2020 to 2027. Owed: NEEDS-CHECKING [Group PM](../needs-checking/group-pm.md) PM3-PM6, issue [#1214](https://github.com/Ajmalpshaik/Heron-AI/issues/1214); the row closes after them. Not measured: whether a project's picture zooms out to a wall through the box (PM6 writes it down). Was: **OPEN 2026-11-20** - recorded from the issue, for whoever owns the view tools.

---

### Row 5b-1896

*Moved from the register on 2026-11-24.*

**Defect.** **THE CONSTITUTION'S *ENFORCEMENT* TABLE NAMES A CREDENTIAL STORE AND AN EGRESS FILTER AS WHERE ARTICLES 13 TO 17 ARE ENFORCED IN CODE, AND NEITHER EXISTS.** Found 2026-11-20 reading the four parts of the specification line by line against the tree ([row 5b-1867](../fragment-issues/row-5b-1867.md), issue [#1072](https://github.com/Ajmalpshaik/Heron-AI/issues/1072)), in a cloud container; each place read back the same day. [`HERON_CONSTITUTION.md`](../../HERON_CONSTITUTION.md) line 224, under the column *Enforced in code at*: *"Permission layer, credential store, egress filter"*. [`brain/heron_secrets.py`](../../brain/heron_secrets.py) lines 24-28 say it is not the store, [12 §5a](../12-security-and-permissions.md)'s note says the same, a search for a Windows credential store in the code finds none, and *egress* appears only in the docstrings of `brain/heron_release.py`, which no served tool reaches. A reader of the table would take two guards for built that are not.

**State.** **MOVED TO ISSUE [#1100](https://github.com/Ajmalpshaik/Heron-AI/issues/1100) 2026-11-20** - the owner answers there; this row is kept whole. Was: **OPEN - recorded, not fixed**, in the change that found it (AGENTS.md: a defect found while doing something else is recorded). The Constitution changes only as it says it does, so the words are the owner's: the table could name what is built and say the rest is not. Nothing here is a code change.

---

### Row 5b-1898

*Moved from the register on 2026-11-24.*

**Defect.** **TWO SOURCES DISAGREE ON WHO RETIRES AN AGENT: [28](../28-agent-registry.md) RETIRES `HERON-ORC-PER-003` ON THE OWNER'S DELEGATION, NOT READ BACK TO HIM, WHILE [D-154](../decisions/D-154.md) POINT 2 SAYS THE OWNER SIGNS A RETIREMENT BY NAME AND AN AI SESSION NEVER SIGNS.** Found 2026-11-20 reading the four parts of the specification line by line against the tree ([row 5b-1867](../fragment-issues/row-5b-1867.md), issue [#1072](https://github.com/Ajmalpshaik/Heron-AI/issues/1072)), in a cloud container; each place read back the same day. Line 57 of 28: *"RETIRED 2026-11-15 by [D-27] ... as [PROPOSALS F19] decided on the owner's delegation, not yet read back to him"*. D-154 (Accepted 2026-11-15) point 2: *"The owner signs, by name, per agent or per named group ... A Heron agent or an AI session never signs"*. It may be outside D-154's reach: the Communication / Persona Agent was always the host's, with no file of Heron's to carry a `Heron-Status`, and D-27 (Accepted) already set one voice with no level per person. Recorded both ways, as AGENTS.md asks; not resolved here.

**State.** **MOVED TO ISSUE [#1100](https://github.com/Ajmalpshaik/Heron-AI/issues/1100) 2026-11-20** - the owner answers there; this row is kept whole. Was: **OPEN - the owner's to resolve.** *Your choice:* does a retirement of a host-provided agent, with no Heron file, need his signature under D-154, or is D-27 enough? Either answer is one line in 28; nothing in code changes.

---

### Row 5b-1899

*Moved from the register on 2026-11-24.*

**Defect.** **THE TWO INSTALL GUIDES STILL SEND A MODELLER TO THE ZIP, THOUGH [D-167](../decisions/D-167.md) MAKES THE ONE SETUP FILE THE MODELLER'S DOOR AND v0.2.0'S RELEASE PAGE ALREADY DOES.** Found 2026-11-20 reading the four parts of the specification line by line against the tree ([row 5b-1867](../fragment-issues/row-5b-1867.md), issue [#1072](https://github.com/Ajmalpshaik/Heron-AI/issues/1072)), in a cloud container; each place read back the same day. [07](../07-installation-and-update.md) and [WHAT-TO-INSTALL](../WHAT-TO-INSTALL.md) name no `HeronSetup` file. D-167's Consequences hold them back on purpose - *"07 and WHAT-TO-INSTALL say so once it has run on the owner's PC; until then they are unchanged"*. [Group OA](../needs-checking/group-oa.md): OA12, the Setup double-clicked, ran 2026-11-16 on the owner's PC, PASS; OA13, the install from its window, ran the same day with its install half PASS, while the *Add Heron to Claude Code* tick has not run on a PC without a Heron checkout. The specification's 00 §8 says the Setup since 2026-11-20, under D-167.

**State.** **MOVED TO ISSUE [#1100](https://github.com/Ajmalpshaik/Heron-AI/issues/1100) 2026-11-20** - the owner answers there; this row is kept whole. Was: **OPEN - the owner's to decide.** *Your choice:* send people to the Setup in both guides now, or wait until OA13's *Add Heron to Claude Code* tick has passed on a PC that is not a developer's. Words only either way.

---
