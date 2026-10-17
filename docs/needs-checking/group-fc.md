# Needs checking — Group FC

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group FC - put one view where another is, on every sheet it is on: `PLACE_VIEW_ON_SHEET` version 2 (2026-10-17)

Found on a real project in Revit 2020: a shop-drawing legend NOTES sits on four sheets, and at the as-built
issue its copy AS-BUILT NOTES has to go exactly where NOTES is, on every one. Heron had no tool for it.
[FRAGMENT-ISSUES row 5b-540](../fragment-issues/section-5b-rows-401-425.md).

**What was built - version 2 of the same capability, not a new one.** `PLACE_VIEW_ON_SHEET` takes a new
optional `oldViewId`: the one view in `views` goes where the old view sits - on every sheet the old view
is on, or only on `sheetId` when one is named - and the old view comes off those sheets. The old view
stays in the project. For a viewport: a new viewport at the old box centre, the old viewport type, its
rotation and, from Revit 2022, its title position; then the old one is deleted. For a schedule: the new
schedule at the old one's top-left corner. `at` is an offset in mm from where the old view sat - `0,0,0`
is the job. Each sheet is all or nothing, and the whole run is one undo. `sheetId` may now be left out.

**What is compiled and what is not run.** Fragment compiled on all eight releases 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-10-17); the add-in, which now resolves `oldViewId` by view name,
built on all eight (`tools/check-compile.py`). **None of it has run in Revit.** Version 1's proof is kept
on the card as a record and is stale, so the card is DRAFT. The binding of `oldViewId` and `sheetId` when
left out is the add-in's `absent:` rule, which needs the add-in from this change deployed.

**A SCRATCH PROJECT, IN A LOCAL SESSION ON THE OWNER'S PC - NEVER THE REAL 4355 MODEL.** Make *Heron Swap
Scratch* from any template, Revit 2020 first:

1. Sheets **HSW-01**, **HSW-02**, **HSW-03**, **HSW-04**, any title block.
2. A legend **NOTES** with a text note in it. Place it on HSW-01 and on HSW-02. Give the two viewports
   **different viewport types** - one with a title, one *No Title* - and on HSW-01 drag the title away
   from its default place and **pin** the viewport.
3. Duplicate NOTES (Duplicate with Detailing), rename it **AS-BUILT NOTES**, change its text, place it on
   **no** sheet.
4. A schedule **HSW SCHEDULE A** placed on HSW-03; a second schedule **HSW SCHEDULE B** on no sheet.
5. A drafting view **HSW DRAFT A** placed on HSW-04 with its **detail number set to 7**, **HSW DRAFT B**
   placed on HSW-01, and **HSW DRAFT C** on no sheet.
6. A floor plan **Level 1** on no sheet, for FC9 - the template's own is enough. Save.
7. Before each run, note where each old viewport sits (a detail line snapped to its centre, or its
   position read off the sheet).

| # | Run | Look for |
|---|---|---|
| **FC1** | **The D-30 positive - the legend.** `oldViewId` NOTES, `views` AS-BUILT NOTES, `at` 0,0,0, `sheetId` left out: the second job of `tools/jobs/place-view-on-sheet-swap-2026-10-17.yaml` | the binding note *sheetId not given - "none"*; two lines in `swapped`, HSW-01 and HSW-02, each *'NOTES' gone, 'AS-BUILT NOTES' in its place - centre 0.0 mm from the old one*, each with **its own sheet's** viewport type, HSW-01's *pinned as before*; on 2022 and later *title position copied*, on 2020 and 2021 *title where Revit draws a new one*; `placed` two ids; `notDone` and `refused` empty |
| **FC2** | **The schedule.** `oldViewId` HSW SCHEDULE A, `views` HSW SCHEDULE B, `at` 0,0,0: the first job | one line, HSW-03, *corner 0.0 mm from the old one* and *a schedule has no viewport type* |
| **FC3** | **The D-30 negative - the new view already on a sheet.** `oldViewId` HSW DRAFT A, `views` HSW DRAFT B: both jobs' `negative-set` | `swapped` and `placed` empty; `notDone` names HSW-04 - *Revit will not put 'HSW DRAFT B' here - it is on sheet HSW-01* - and *Nothing on this sheet was changed*; `refused` holds HSW-04's id. After the run DRAFT A is still on HSW-04 and DRAFT B still on HSW-01 |
| **FC4** | **The refusals.** `views` NOTES with `oldViewId` NOTES; then `views` HSW SCHEDULE B with `oldViewId` NOTES; then `oldViewId` AS-BUILT NOTES (on no sheet); then FC1 with `sheetId` HSW-03; then version 1 with `sheetId` left out and no `oldViewId` | each says why in `notDone` and changes nothing: *both 'NOTES'*; *not the same kind*; *not on any sheet*; *not on sheet HSW-03*; *No sheet was named* with the views in `refused` |
| **FC5** | **Kept, and looked at - the second route.** FC1 once with the change KEPT (`try-once`, or `revit_change` after it is PROVEN), then open HSW-01 and HSW-02 | AS-BUILT NOTES exactly where NOTES was - on the detail line noted in step 7; the title shown on one and hidden on the other, as before; the title on HSW-01 where it was dragged (2022 and later); NOTES still in the Project Browser under Legends; **one Ctrl+Z** puts NOTES back on both sheets |
| **FC6** | **The new view already on one of the sheets.** Place AS-BUILT NOTES on HSW-02 by hand, then run FC1 | HSW-01 swapped; HSW-02 in `notDone` - *already on this sheet ... would show it twice* - and left exactly as it was |
| **FC7** | **A split schedule (2022 and later).** Split HSW SCHEDULE A on HSW-03 into two pieces, then run FC2 | *split into 2 pieces ... Merge it back into one*; nothing changed |
| **FC8** | FC1 to FC3 once on **Revit 2020** and once on **2027** | the same answers, except the title line. The members were read off the reference assemblies for every release and compiled, not run |
| **FC9** | **Version 1 still works.** The placing job as version 1 was proved: one free view, a sheet, a point | `placed` one viewport id, `swapped` and `notDone` empty - version 1's proof, re-run so the card can be signed again |
| **FC10** | **A drafting view keeps its detail number.** `oldViewId` HSW DRAFT A, `views` HSW DRAFT C, `at` 0,0,0 | one line, HSW-04, *detail number 7 kept*; DRAFT C's viewport reads 7 in Properties |
