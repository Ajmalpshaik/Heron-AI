# Needs checking — Group FG

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group FG - which sheets a view is on, each box on the paper, and boxes off the title block or over each other: `LIST_VIEWS_ON_SHEET` version 2 (2026-10-17)

[FRAGMENT-ISSUES row 5b-560](../fragment-issues/section-5b-rows-401-425.md). `LIST_VIEWS_ON_SHEET` (FRG-SHT-027)
was widened, not replaced: every sheet (`sheetNumber` left out or `all`), one view by name across sheets
(`viewName`), each viewport's and schedule's box corner to corner in millimetres with its viewport type, and two
checks - a box past its sheet's title block and two boxes overlapping, each with how many millimetres.

**What is compiled and what is not run.** It builds on all eight releases 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-10-17), every Revit member it calls exists with the same signature on
all eight (`tools/api-surface --members`), and `tests/test_list_views_on_sheet.py` holds its rules as text.
**None of it has run in Revit.** It is DRAFT.

**A NEW SCRATCH PROJECT, in a local session on your own PC - never the 4355 model and never any job model.**
The arrangement is in
[`tools/jobs/list-views-on-sheet-v2-scratch-2026-10-17.yaml`](../../tools/jobs/list-views-on-sheet-v2-scratch-2026-10-17.yaml):
a legend `HERON TEST LEGEND` on S-01 (its box dragged past the title block's right edge by about 50 mm) and on
S-02 (its box over a corner of the Level 2 plan), S-03 a clean sheet with the Site plan alone, S-04 nothing.
Revit 2020 first. The tool is a read, so nothing it does needs Keep or Discard.

| # | Run | Look for |
|---|---|---|
| **FG1** | `python tools/batch-prove.py tools/jobs/list-views-on-sheet-v2-scratch-2026-10-17.yaml` with the scratch project in front | Job 3 PASS - the legend on S-01 and S-02 and nowhere else, the empty S-04 giving nothing. **The D-30 pair.** Read the job's own notes for jobs 1 and 2: job 1 is expected to read NEG NOT EMPTY, and what decides it is `offTitleBlockCount` 0 and `overlapCount` 0 on the clean sheet S-03 |
| **FG2** | On S-01, select the legend's viewport and measure its box's right edge against the title block's with the Measure tool | Within a millimetre or two of the "mm on the right" `offTitleBlock` gives. A box said off on the LEFT, or an amount near 0.05 or 15000, means a side or a unit is wrong |
| **FG3** | On S-02, compare the overlap's "W x H mm" with the two boxes in the same answer | W and H are where the two boxes share width and height. Then move the legend clear by hand and ask again: `overlapCount` 0 |
| **FG4** | Give the legend's viewport a title (viewport type with title) and drag the title out past the title block, the box itself inside | `offTitleBlock` stays EMPTY - `GetBoxOutline` is the view without its title, which is RevitAPI.xml's word on 2024 and has not been seen on a sheet. If the title counts, write down the release |
| **FG5** | A viewport whose view has a visible crop region and an elevation marker off to one side | The box takes in the marker - write down whether Revit's own selection box on the sheet agrees with the box given |
| **FG6** | Ask with `viewName` set to part of the legend's name only, e.g. `HERON TEST` | No row; `viewProblem` says no view by that name, and lists `HERON TEST LEGEND` among the names that hold it - never the legend's rows from a part of its name |
| **FG7** | Delete S-03's title block, keep its Site plan, and ask for every sheet | S-03 named in `notChecked`, never counted clean. Put the title block back |
| **FG8** | A schedule on S-03, split into two pieces (Revit 2022 or later) and one piece dragged over the Site plan | One schedule row with two boxes, and one overlap naming the schedule and the plan |
| **FG9** | FG1 once on Revit 2024 and once on 2027 | The same answers - one implementation for every release |
| **FG10** | In the chat, *"which sheets is this legend on"*, *"does any view run off the sheet"*, *"check the sheets for overlapping viewports"* | Each reaches `LIST_VIEWS_ON_SHEET` through `revit_read`, and the answer says the boxes and amounts in millimetres |
