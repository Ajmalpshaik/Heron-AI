# FRAGMENT-ISSUES archive — section 5b, rows 351 to 375

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
> Rows 351 to 375 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-356

*Moved from the register on 2026-11-17.*

**Defect.** **`tests/test_routing_phrases.py` FAILS ON MAIN ON THE OWNER'S PC: SIGNING THE TWO SITE SETTERS PROVEN TIPPED TWO QUESTIONS ONTO THEM.** Found 2026-10-07 measuring 5b-355, and reported the same day by the session fixing 5b-345, on main and on its own branch alike. On the trained model the suite exits 1 on three checks: *which units does this model show cooling load in* reaches `SET_PROJECT_UNITS` and *is project north rotated from true north* reaches `SET_PROJECT_LOCATION` - both ADMIN - the second failing in two sections. CI ranks by spelling and the suite exits 3 there, so only a machine with the trained model sees it. Narrowed on private stores: it passes at 0b6d4290 and fails at 60da770c, the commit in #427 that signed both setters PROVEN, and at every later commit tried - #427's head, its merge c4f2c5f2, and main at 6d02459b and 46ba7356. At 60da770c with nothing changed but those two cards' `heron-status` set back to DRAFT it passes, with no card refused: the PROVEN nudge settles two close contests the setters' way - the hazard 5b-329 recorded, a guard that a status change flips.

**State.** **MOVED TO ISSUE [#909](https://github.com/Ajmalpshaik/Heron-AI/issues/909) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN for the True North question - an exact tie the PROVEN nudge settles (5b-329). The units question was repaired on 2026-10-14 in PR #535.** Measured with this suite on private stores, trained model, against `95db79b9`. *"which units does this model show cooling load in"* now reaches `REPORT_PROJECT_UNITS`: two question utterances on that DRAFT read, and *"what symbol does air flow show"*, claimed in its routing table, declared on it once the longer card let `SET_AIR_TERMINAL_FLOW`, a write, take it. `REPORT_LOCATION`'s purpose was 809 words and its version 4 paragraph on the site began at word-piece 512, past what the trained model reads; those paragraphs moved to comments word for word, and two True North questions were added. *"is project north rotated from true north"* is still `SET_PROJECT_LOCATION` at words #2 + nearness #1 against `REPORT_LOCATION` at words #1 + nearness #2 - equal fused scores, settled by the PROVEN nudge. Of five True North paraphrases written before any wording was chosen, two now reach the read, two still reach the setter and one `ROTATE_ELEMENTS`. `tools/check-routing.py`: claimed and not reached 52 to 51 on the trained model and 39 to 36 on spelling; *"which way is true north"* (trained) and *"what latitude is the site set to"* (spelling) no longer reach `SET_PROJECT_LOCATION`. **The cost:** *"set cooling load to Btu/h"* - claimed in both units cards' routing tables - now reaches the read by one rank on the trained model, the safe direction. Declaring it on `SET_PROJECT_UNITS` would answer it by identity; that card is PROVEN, so it is the owner's call. *"move the site to these coordinates"*, claimed by `SET_PROJECT_LOCATION`, reached it on the trained model before and reaches `REPORT_LOCATION` now - the same direction, the same repair on a PROVEN card. The setters were not demoted. Was: **OPEN - recorded, not fixed.** Not 5b-355's: the same three failures with and without that change. The sentences belong to 5b-322 and 5b-346, which is OPEN; their reads are `REPORT_PROJECT_UNITS` and `REPORT_LOCATION`. Not to be fixed by demoting the setters - that is buying a rank. Until it is fixed, `tools/change-evidence.py` counts the suite red on both sides of every change made on this PC.

---

### Row 5b-358

*Moved from the register on 2026-11-17.*

**Defect.** **AN `ARRAY_FAMILY_FORMS` MEMBER IS A MODEL GROUP EVEN FOR ONE FORM, AND THE ORIGINAL IS PUT IN ONE TOO - THE CARD SAYS ONLY SEVERAL FORMS DO THAT.** Seen 2026-10-07 measuring 5b-357 on `ArrayAxisScratch.rfa` (Revit 2024 session 9240, rolled back): one extrusion arrayed three times gave `GetOriginalMemberIds` a Group holding the sketch plane, the sketch, the extrusion and its four model lines, and the extrusion itself now reads that group as its `GroupId`; each copy is a Group of the same kind. The fragment's header and card say *"Arraying several elements at once makes each member a group"* as the reason for one form per array, and `members` is described as *"the copies' unique ids"* - they are the groups' ids. Its finding that void copies *"cut nothing until COMBINE_FAMILY_FORMS combines them"* points at those ids; whether that tool accepts a group, or a form inside one, was not tried.

**State.** **MOVED TO ISSUE [#944](https://github.com/Ajmalpshaik/Heron-AI/issues/944) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not fixed.** Found while fixing 5b-357 and outside it. The questions for whoever takes it: does `COMBINE_FAMILY_FORMS` take a void copy by the id `members` gives, and should `members` name the forms inside the groups. **2026-10-07, row 366:** `members` stay the groups' ids, now in order along the row, and `LOCK_NESTED_FAMILY_TO_PLANES` version 2 takes such a group id and locks the nested family inside it (measured on an array's last copy). `COMBINE_FAMILY_FORMS` with a void copy's group id is still untried. **2026-10-08, project review: the card's words are corrected** - its purpose and its `members` description now say every member is a model group, the original included, even for one form, and that `members` names the groups; the proof stands (a card's words are outside its seal). The PROVEN impl header still gives the old reason and is left as it is, since editing it stales the proof. `COMBINE_FAMILY_FORMS` with a void copy's group id is still untried, so the row stays open.

---

### Row 5b-361

*Moved from the register on 2026-11-17.*

**Defect.** **A REPLY IS LOST WHEN A REVIT WINDOW OPENS MID-CALL: SOMETHING CONNECTS TO THE BRIDGE AND TAKES THE PIPE FROM THE CALL WAITING ON IT.** Seen twice on 2026-10-07, Revit 2024 session 9240. `CREATE_FAMILY_DOCUMENT` aimed at a family not in front opened `HeronNest_Car` (journal: opened 21:21:58) and came back `unknown_outcome`; the add-in log shows *"A newer connection took the session"* at 18:22:04Z. `ACTIVATE_DOCUMENT` opened `HeronNest_Tyre` (journal 21:23:16.784); the log shows a newer connection at 18:23:17Z, and the client's read path - which retries a lost answer as safe - sent it again: the second run truthfully said `wasAlready true`, so the reply read as "nothing was changed" after a switch that had happened. A third open the same evening came back first time. The waiting call's own client never reconnects while it waits (`request`, `heron_bridge_client.py`), so the connection came from somewhere else; the add-in writes `HeronLiveState` on `ViewActivated` for the Companion page (D-108), which is a candidate and is NOT established.

**State.** **MOVED TO ISSUE [#932](https://github.com/Ajmalpshaik/Heron-AI/issues/932) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not fixed.** Not established: which process connects, and whether it is every window open or only some. Until it is, a call that opens a window is checked with `REPORT_OPEN_DOCUMENTS` and the file on disk, never repeated - written into the `family-creation` skill's step 11b. Settling it is [NEEDS-CHECKING CS](../needs-checking/group-cs.md) CS6.

---

### Row 5b-364

*Moved from the register on 2026-11-17.*

**Defect.** **`LABEL_FAMILY_RADIUS` STILL REFUSES A SECOND EXTRUSION OR REVOLVE SHARING ONE PARAMETER, THOUGH TWO ROUND LABELS ON ONE PARAMETER WERE MEASURED TO WORK.** Version 4 lets a parameter label the same kind of size on several swept blends, because a coil's half-turns share their radii - measured 2026-10-07: two radial dimensions labelled "Wire Radius", one per profile, both drove at 8 and 3. Extrusions and revolves keep the version-3 rule, *"A second label on it would fight the first"*, so two cylinders of one Diameter still need a parameter each.

**State.** **MOVED TO ISSUE [#920](https://github.com/Ajmalpshaik/Heron-AI/issues/920) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not fixed.** The rule was left alone on the forms it was proved for. Whether to relax it for them is a decision; the swept blend measurement is the evidence for it.

---

### Row 5b-367

*Moved from the register on 2026-11-17.*

**Defect.** **`FLEX_FAMILY` DOES NOT READ NESTED FAMILIES - A NESTED PART THAT FAILED TO FOLLOW WOULD PASS THE FLEX.** Seen 2026-10-07 flexing TRG_Spring_Compression Spring (Revit 2024): every trial reported *"across 4 solid(s)"* - the four end turns, the family's own forms - while the four to eight arrayed one-turn families were never measured; only a scratch probe reading every `FamilyInstance` showed them following. The card says it reads *"EVERY solid's own extent"* so that a part moving inside one that does not is caught - a nested family is exactly such a part.

**State.** **MOVED TO ISSUE [#944](https://github.com/Ajmalpshaik/Heron-AI/issues/944) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not fixed.** Found while building the spring and outside it. For whoever takes it: read each nested `FamilyInstance`'s extent per trial, beside the forms.

---
