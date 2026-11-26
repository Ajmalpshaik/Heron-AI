# Needs checking — Group QT

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in this
> file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back into the
> register for every tool that reads the register.

## Group QT - REPORT_ELEMENT_DEPENDENCIES version 2: every dimension in every view that a delete takes or cuts - DRAFT until proved (2026-11-26)

**Why:** swapping three in-place Mechanical Equipment units on Revit 2020 (project 4355-BHVD-3D-60P00-BL003A,
2026-11-23), `REPORT_ELEMENT_DEPENDENCIES` version 1 named 36 dependents; the delete took 62 elements and cut 26
more dimensions that measured to the units and to something else
([row 5b-1990](../fragment-issues/row-5b-1990.md), issue [#1278](https://github.com/Ajmalpshaik/Heron-AI/issues/1278)).
Version 1 read only `GetDependentElements(null)`. Version 2 also reads every dimension in the model - no view -
and names each one whose references touch the elements or their dependents: DELETED when too few references
would be left (`dimensionsDeleted`, only those Revit did not list itself), CUT when a linear string keeps two or
more (`dimensionsCut`), one line each with its view in `dimensionLines`, and `goesTotal` - the count Revit's
Delete box should show. The card's [`tests/cases.yaml`](../../brain/fragments/report-element-dependencies/tests/cases.yaml)
holds every case; these rows are the run.

**What is done and what is not.** Compiles for 2020 to 2027 (`tools/check-fragments-compile.py`, 2026-11-26), and
every member it reads is on every release (`tools/api-surface --members Dimension`, `Reference`, `DimensionShape`).
**Never run in Revit.** The DELETED / CUT rule - fewer than two references left on a linear string, or any
reference on an angular, radial, diameter, arc-length or spot dimension - is read from what Revit did on
2026-11-23, not from any API promise: QT3 and QT4 settle it. What a delete changes in a view itself (seven 3D
views were modified on 2026-11-23) is not reported, and is not checked here.

**The model: scratch, never a working one** - Revit 2020 first (the release the defect was seen on), then 2024.
`dims-scratch`: new from the metric template. Two grids A and B 6000 mm apart, a third grid C 3000 mm beyond B.
One wall `W-1` well away from the grids, which nothing dimensions. One loadable family instance `UNIT-1` (any Mechanical Equipment or Generic Model) between A and B, one tag on it in
Level 1's plan. In `Section 1` through `UNIT-1`: one linear dimension from grid A to `UNIT-1`'s face (2 references).
In `South` elevation: one linear string grid A - `UNIT-1` - grid B (3 references). In `Section 1`: one dimension
grid B - grid C (touches nothing that goes). Write each dimension's id here before the run. Keep Level 1's plan the
active view throughout - none of the three dimensions is in it.

| ID | Do this | Pass looks like |
|---|---|---|
| **QT1** | **Negative, first.** Select a wall nothing measures to; `REPORT_ELEMENT_DEPENDENCIES` | `dimensionsDeleted`, `dimensionsCut` and `dimensionLines` empty; `dimensionsRead` 3 - all three were looked at; `goesTotal` 1 plus its dependents. A version that names every dimension in a view, or near the element, fails here |
| **QT2** | **Negative.** Select grid C alone | ONE line: the `Section 1` grid B - grid C dimension, DELETED, *"1 of its 2"*. Nothing names the A - `UNIT-1` dimension or the `South` string - neither measures to grid C |
| **QT3** | Select `UNIT-1`; `REPORT_ELEMENT_DEPENDENCIES` with Level 1's plan active | The tag among `dependents`; the `Section 1` dimension DELETED (in `dimensionsDeleted` unless Revit listed it as a dependent - say which), *"1 of its 2"*; the `South` string CUT, *"1 of its 3 ... 2 stay"*; each line naming its own view; `dimensionsUnread` empty. Write down `goesTotal` |
| **QT4** | `DELETE_ELEMENTS` on `UNIT-1` with `expect_from` naming the select that found it; read the count in Heron's *Delete N elements?* box and press **Cancel** (nothing deleted). Then press Delete on `UNIT-1` by hand, look at both views, and Undo | The box's N equals QT3's `goesTotal`. After the hand delete: the `Section 1` dimension gone, the `South` string still there with two references (A - B). Either different is a defect in the rule - open a row; the card is not proved |
| **QT5** | The same run as QT3 on Revit 2024 (a 64-bit id release), then QT4 there | The same answers; ids printed in full |
