# Needs checking — Group QF

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group QF - a sheet named by its number, one of two same-named parameters selected, a refused value never thrown - BUILT, NOT RUN IN REVIT (2026-11-24)

**Tracked as GitHub issue [#1339](https://github.com/Ajmalpshaik/Heron-AI/issues/1339) since 2026-11-24** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** three gaps met on 2026-11-24 on Revit 2020 (session 29500, add-in 0.3.0), filling a site sheet's
title block from a reference sheet on 4355-BHVD-3D-60P00-BL003A (15 sheets, three named *Unnamed*):
[row 5b-2015](../fragment-issues/row-5b-2015.md) (which sheet is in front could not be told, and no sheet
could be named by its number), [row 5b-2016](../fragment-issues/row-5b-2016.md)
(`SELECT_BY_PARAMETER_VALUE` could not pick one of two *Sheet Number* parameters) and
[row 5b-2017](../fragment-issues/row-5b-2017.md) (`SET_PARAMETER_VALUES_BY_ID` threw on a Yes/No given
`-1`). All three were fixed in a cloud session with no Revit.

**What is done and what is not.** The add-in and both cards compile for 2020 to 2027
(`tools/check-compile.py`, `tools/check-fragments-compile.py`, 2026-11-24, in a cloud container), and
`tests/test_sheet_by_number.py` and `tests/test_companion.py` hold the code's shape - each seen red on the
code before the change. **Not tested in Revit.** `SELECT_BY_PARAMETER_VALUE` (version 5) and
`SET_PARAMETER_VALUES_BY_ID` (version 11) are `DRAFT`, so `revit_change` refuses the second until it is
signed. The add-in must be rebuilt and redeployed before QF1.

**The model: a saved copy of a model with sheets, never the working model** - three sheets given one name
(*Unnamed*) and different numbers, one shared Yes/No sheet parameter bound to Sheets. Revit 2020 first.

| ID | Do this | Pass looks like |
|---|---|---|
| **QF1** | Open one of the three *Unnamed* sheets. `revit_view_image` with `view` empty; then `view=Unnamed`; then `view=<that sheet's number>`; then `view=Sheet <number> - Unnamed`; then `view=<the id the first answer gave>`. **Negative:** `view=NO-SUCH-NUMBER` | The first answer reads *"sheet <number> - 'Unnamed' (DrawingSheet, view id N)"*; `Unnamed` refuses listing three DIFFERENT *"Sheet <number> - Unnamed"*; the next three each picture that sheet; the negative refuses *"no sheet numbered that"*, nothing pictured. `revit_views` gives the view in front's id and number. Then a card whose `view` input goes through the binder - `REPORT_TAGS_AND_TARGETS`, `view=<that sheet's number>` - reads that sheet, not another |
| **QF2** | `SELECT_BY_PARAMETER_VALUE`, categories Sheets, matchMode equals, matchText one sheet's number: (i) `parameterName=Sheet Number {SHEET_NUMBER}`; (ii) `Sheet Number`; (iii) **negative** `Sheet Number {NO_SUCH_PARAMETER}` | (i) exactly that sheet, ambiguous 0; (ii) 0 selected, ambiguous = the sheet count, the finding naming *'Sheet Number {SHEET_NUMBER}' or 'Sheet Number {VIEWER_SHEET_NUMBER}'*; (iii) 0, *"NOTHING carries"* |
| **QF3** | The same with `parameterName=Sheet Number {VIEWER_SHEET_NUMBER}`, and with the shared Yes/No parameter named `<its name> {<its GUID>}` | Each reads that one parameter alone, ambiguous 0 - what `VIEWER_SHEET_NUMBER` holds on a sheet recorded as found |
| **QF4** | `SET_PARAMETER_VALUES_BY_ID` with a CSV: id column, then the shared Yes/No column - (i) every cell 1 or 0; (ii) **negative** one cell `-1`; (iii) by key, `key=Sheet Number\|in=Sheets`, one row giving the box `-1` | (i) written = rows, each box as asked; (ii) NOTHING written, `stale` *"is a Yes/No, which takes 1 or 0 (yes or no) - not '-1'"*, the outcome line REFUSED or NOTHING CHANGED - never `fragment_threw` - and no other box moved; (iii) that key refused with the same words, the other rows written |
| **QF5** | `SET_PARAMETER_VALUES_BY_ID`, id rows, two rows: a sheet's text parameter, and a number parameter given `abc` | NOTHING written - the text row is not kept either - `stale` names the number row *"Revit would not take 'abc'"*; never `fragment_threw` |
