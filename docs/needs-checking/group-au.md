# Needs checking — Group AU

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-27 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group AU - `center-viewports-on-sheets`: each sheet's viewport moved to the centre of its title block (2026-09-27)

**One fragment from one sitting on 2026-09-27, in Project1 on Revit 2024, and it is DRAFT.**
[`center-viewports-on-sheets`](../../brain/fragments/center-viewports-on-sheets/fragment.yaml) -
CENTER_VIEWPORTS_ON_SHEETS, FRG-SHT-020 - moves the one viewport on each sheet handed in so that its box's centre
sits on the centre of the sheet's title block, reporting where it was, where it went and the centre read back, in
millimetres on the paper. It compiles on every release from 2020 to 2027. It has run in a chat on the owner's
working model, and **a chat run is not a proof**: no fingerprint was taken and nothing was rolled back
([D-30](../DECISIONS.md)), so no row below is closed by it.

**Why it exists.** Four floor plans sat to one side of their A1 sheets, M-101 to M-104, after
`PLACE_VIEWS_ON_SHEET` placed them and their scale went from 1:50 to 1:75. Nothing in the library centred a
viewport: *"centre the view on the sheet"* resolved to `ALIGN_VIEWPORTS_ACROSS_SHEETS`, which copies a master
sheet's centre, and *"the plan is off to the side, centre it"* to `PLACE_HOSTED_FAMILY`, which places doors.

**The working runs, 2026-09-27 - chat runs, not a proof.** "heron ai bulding", Revit 2024, Heron session 46596,
Changes on. Each write was a `revit_read` FIND_SHEETS followed at once by `revit_change`
CENTER_VIEWPORTS_ON_SHEETS with `expect_from` naming that find. The title block centre is (422, 296) mm on every
sheet, and every viewport box is 531 x 443 mm in an 840 x 594 mm title block.

1. `numberOrNameContains=M-10`, four sheets - `centred` 4. M-101 (397, 299) to (422, 296); M-102 (397, 309);
   M-103 (395, 293); M-104 (387, 296); each read back at (422, 296). **Kept.**
2. The same again - M-101 to M-103 in `alreadyCentred`, nothing moved; **M-104 moved again**, from (392, 296).
3. The same again - `alreadyCentred` 4, `centred` 0.
4. After another chat added 90 air terminal tags to M-104's view: M-104 from (379, 296) to (422, 296), the other
   three already centred. Then once more - `alreadyCentred` 4, `centred` 0. **This is the read-back.**
5. FIND_VIEWS for the floor plan `Level 1 - Room Plan Layout`, handed in instead of a sheet - `refused` 1, named
   as not a sheet, `centred` 0.
6. `numberOrNameContains=Schedule -`, the three schedule sheets M-105 to M-107 - `noViewport` 3, `centred` 0.

**WHY M-104 MOVED BETWEEN RUNS.** Another chat added 15 room tags to that view between runs 1 and 2, and 90 air
terminal tags between 3 and 4, and changed no crop, scale or placement. Each time the viewport's box centre on the
sheet had shifted left - 30 mm, then 43 mm - with the box still 531 x 443 mm. So annotating a placed view can move
it on its sheet; run this fragment last. Measured twice, on one view; why Revit does it was not looked into.

Every row runs on a TEST COPY of "heron ai bulding" - never the working model.

| # | Check | Expected |
|---|---|---|
| **AU1** | The proof ([D-30](../DECISIONS.md)). **A copy saved after 2026-09-27 is already centred**, so first, on the test copy only: open M-101, select its viewport and drag it about 50 mm left. Then, with `HERON_CLIENT_ID=ajmal-pc` set and the test copy in front, `python tools/batch-prove.py tools/jobs/center-viewports-on-sheets-project1-2026-09-27.yaml --dry-run`, then the same without `--dry-run`. Then read the draft, sign it with `python brain/heron_validate.py accept center-viewports-on-sheets --by "Ajmal PS"`, and set `heron-status: PROVEN` | `PASS`. The positive - the four `Level 1 -` sheets: `centred` 1, `alreadyCentred` 3, and `report` saying M-101 was about 50 mm left of (422, 296) and reads back at it. The negative - the three `Schedule -` sheets: `centred` 0 and `noViewport` 3. `POSITIVE EMPTY` with `alreadyCentred` 4 means the drag was skipped. Afterwards M-101 sits where the drag left it - both legs are rolled back |
| **AU2** | The second route - the paper. On the WORKING model after the chat runs, open M-101 to M-104 and measure, or print them | The viewport's box sits with equal margins left and right, and top and bottom, inside the title block's outer edge - **measured on the sheet, not inferred from `centred`**. Read it against the box, not the drawing: a box taking in an elevation marker puts the plan off-centre by half the marker's distance, and the fragment reports that rather than hiding it |
| **AU3** | The ambiguous case - no sheet in this model has two viewports. On the test copy, drag a legend or a drafting view onto M-102 by hand, then in a chat: FIND_SHEETS `numberOrNameContains=M-102`, then CENTER_VIEWPORTS_ON_SHEETS with `expect_from` naming it | `ambiguous` 1 naming M-102, `centred` 0, and **neither viewport moved** - which one is the plan is not answerable, and moving the wrong one is worse than moving none |
