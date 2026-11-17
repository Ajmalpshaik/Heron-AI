# NEEDS-CHECKING archive — Group FB

> **Checks that were done, moved out of the live register.** These are rows of Group FB of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *the sheets of one revision, latest on the sheet or not, and a typed list of sheet numbers: `FIND_SHEETS_BY_REVISION_OR_NUMBER` (2026-10-17)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row FB1

*Moved from the register on 2026-11-17.*

**Run.** `revit_read` capability `FIND_SHEETS_BY_REVISION_OR_NUMBER` with `revisionSequence=2` and nothing else

**Look for.** **Eight** sheets, 179 to 186 - 180 and 181 included though their latest revision is Seq 4, 186 included though it carries Seq 2 only by a cloud in a view. `revisionsMatched` *Seq 2, number 2, date 14.10.2026, FB ISSUED AS-BUILT - on 8 sheet(s)*. The binding note names the three inputs left out as their card defaults. **The D-30 positive** **CLOSED 2026-11-13, not passed** - `FIND_SHEETS_BY_REVISION_OR_NUMBER` was retired into `FIND_SHEETS` on 2026-11-01 (its `deprecated:` block); `FIND_SHEETS` is PROVEN at version 3, `proof:` dated 2026-11-02. Nothing here runs on the retired card.

---

### Row FB2

*Moved from the register on 2026-11-17.*

**Run.** The same with `sheetNumbers=17` and nothing else

**Look for.** `elements` empty and `notFound` *17* - neither 179 nor 1790 picked. **The D-30 negative**: a typed number is whole **CLOSED 2026-11-13, not passed** - `FIND_SHEETS_BY_REVISION_OR_NUMBER` was retired into `FIND_SHEETS` on 2026-11-01 (its `deprecated:` block); `FIND_SHEETS` is PROVEN at version 3, `proof:` dated 2026-11-02. Nothing here runs on the retired card.

---

### Row FB3

*Moved from the register on 2026-11-17.*

**Run.** `revisionSequence=2`, `sheetNumbers=179, 180, 1790, 999`

**Look for.** 179 and 180 picked; `listedWithoutRevision` *1790*; `notFound` *999*; `revisionsMatched` ends *of those listed*. Then `revisionSequence=3` with `179, 180, 181`: nothing picked, all three in `listedWithoutRevision` **CLOSED 2026-11-13, not passed** - `FIND_SHEETS_BY_REVISION_OR_NUMBER` was retired into `FIND_SHEETS` on 2026-11-01 (its `deprecated:` block); `FIND_SHEETS` is PROVEN at version 3, `proof:` dated 2026-11-02. Nothing here runs on the retired card.

---

### Row FB4

*Moved from the register on 2026-11-17.*

**Run.** `revisionDescription=FB ISSUED AS-BUILT`; then `revisionDescription=as-built`

**Look for.** The first picks FB1's eight and does not choose Seq 3. The second is **refused**, naming Seq 2 and Seq 3 with *Give the sequence of the one meant*, `elements` empty **CLOSED 2026-11-13, not passed** - `FIND_SHEETS_BY_REVISION_OR_NUMBER` was retired into `FIND_SHEETS` on 2026-11-01 (its `deprecated:` block); `FIND_SHEETS` is PROVEN at version 3, `proof:` dated 2026-11-02. Nothing here runs on the retired card.

---

### Row FB5

*Moved from the register on 2026-11-17.*

**Run.** `revisionDate=14.10.2026`; then `revisionSequence=9`; then no values at all

**Look for.** The date picks 179 to 186 and 1790, both revisions named with their counts. Seq 9 is refused, listing every revision in the project. No values is refused - *Nothing to pick the sheets by* - never every sheet **CLOSED 2026-11-13, not passed** - `FIND_SHEETS_BY_REVISION_OR_NUMBER` was retired into `FIND_SHEETS` on 2026-11-01 (its `deprecated:` block); `FIND_SHEETS` is PROVEN at version 3, `proof:` dated 2026-11-02. Nothing here runs on the retired card.

---

### Row FB6

*Moved from the register on 2026-11-17.*

**Run.** Second route, by hand: each sheet's Revisions on Sheet, and a sheet list schedule with *Current Revision*

**Look for.** Seq 2 ticked or clouded on exactly FB1's eight; the schedule shows Seq 4 as current on 180 and 181 - the two a *Current Revision Description* filter misses and this does not **CLOSED 2026-11-13, not passed** - `FIND_SHEETS_BY_REVISION_OR_NUMBER` was retired into `FIND_SHEETS` on 2026-11-01 (its `deprecated:` block); `FIND_SHEETS` is PROVEN at version 3, `proof:` dated 2026-11-02. Nothing here runs on the retired card.

---

### Row FB7

*Moved from the register on 2026-11-17.*

**Run.** *An observation.* Set Seq 2 to *Show: None* in Sheet Issues/Revisions, then FB1 again; set it back

**Look for.** Whether the eight still come back. Write down which: if none do, a revision hidden from the title block is invisible to this card, and the purpose must say so **CLOSED 2026-11-13, not passed** - `FIND_SHEETS_BY_REVISION_OR_NUMBER` was retired into `FIND_SHEETS` on 2026-11-01 (its `deprecated:` block); `FIND_SHEETS` is PROVEN at version 3, `proof:` dated 2026-11-02. Nothing here runs on the retired card.

---

### Row FB8

*Moved from the register on 2026-11-17.*

**Run.** *An observation.* Try to tick Seq 2 on placeholder P-01; if Revit allows it, FB1 again

**Look for.** Whether P-01 is picked and its id is in `placeholders`. If Revit refuses the tick, write that down - the placeholder half of the purpose then never fires **CLOSED 2026-11-13, not passed** - `FIND_SHEETS_BY_REVISION_OR_NUMBER` was retired into `FIND_SHEETS` on 2026-11-01 (its `deprecated:` block); `FIND_SHEETS` is PROVEN at version 3, `proof:` dated 2026-11-02. Nothing here runs on the retired card.

---

### Row FB9

*Moved from the register on 2026-11-17.*

**Run.** Then the chain the 2026-10-14 job needed: FB1's sheets to `revit_change` `MANAGE_SHEET_SETS` `mode=create`, `setName=FB Seq 2` (Changes ON, rolled back or kept by the modeller)

**Look for.** `memberCount` 8 and `notASheet` empty. View/Sheet Set in the Print dialog lists the eight **CLOSED 2026-11-13, not passed** - `FIND_SHEETS_BY_REVISION_OR_NUMBER` was retired into `FIND_SHEETS` on 2026-11-01 (its `deprecated:` block); `FIND_SHEETS` is PROVEN at version 3, `proof:` dated 2026-11-02. Nothing here runs on the retired card.

---

### Row FB10

*Moved from the register on 2026-11-17.*

**Run.** FB1 and FB2 once on Revit 2024 and once on 2027

**Look for.** The same answers - one implementation, every release **CLOSED 2026-11-13, not passed** - `FIND_SHEETS_BY_REVISION_OR_NUMBER` was retired into `FIND_SHEETS` on 2026-11-01 (its `deprecated:` block); `FIND_SHEETS` is PROVEN at version 3, `proof:` dated 2026-11-02. Nothing here runs on the retired card.

---
