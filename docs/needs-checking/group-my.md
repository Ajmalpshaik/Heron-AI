# Needs checking — Group MY

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group MY - the sheets of a spreadsheet sheet register made in one call, on their title block, with Drawn By, Checked By and the other columns filled in: `CREATE_SHEETS_FROM_TABLE`, the `heron_sheet_list` tool and the `sheets-from-excel` method (2026-11-09)

The owner's ask: [docs/48](../48-studying-bimlogiq.md) section 5 item 7, *"Sheets from Excel - read the sheet
list from a spreadsheet, preview, create"*, started 2026-11-09 (*"start item 7"*). What the modeller gets: *"make
the sheets from this Excel: C:\\...\\sheet list.xlsx"* (or .csv) - a preview, then the sheets in one call and one
undo. Made NEW, at DRAFT. It overlaps `CREATE_SHEETS` and `SET_PARAMETER_VALUES_BY_ID` (both PROVEN) - neither
was edited; widening a PROVEN card sends it back to DRAFT and out of the chat's reach
([D-117](../DECISIONS.md)). The overlap and the merge planned once this is proved are
[FRAGMENT-ISSUES row 5b-1485](../fragment-issues/section-5b-rows-401-425.md).

| What | Where | What it does |
|---|---|---|
| `heron_sheet_list` (chat tool, READ) | [`brain/heron_sheet_list.py`](../../brain/heron_sheet_list.py), served by `heron_mcp_server.py` | Reads an .xlsx (the standard library's zip and XML - no openpyxl) or a .csv (BOM, `,` `;` or tab, Windows-1252 when not UTF-8). The first row (or `header_row`) is the headers: Sheet Number required (Number, Sheet No, Drawing Number ...), Sheet Name and Title Block optional, every other column a parameter. Lists a blank row (skipped), a row with no number, a number on two rows (neither passed), a multi-line cell (one line), an Excel error cell, a header missing or used twice. An Excel date arrives as `2026-11-09`. An .xls, .xlsb or .ods is refused with how to save the file. Answers with the sheets, the problems, the numbers for `FIND_SHEETS` and the exact `sheets=` value, and the route the card's status allows. Touches no model |
| `CREATE_SHEETS_FROM_TABLE` (MODIFY) | [`brain/fragments/create-sheets-from-table`](../../brain/fragments/create-sheets-from-table/fragment.yaml) | `sheets` is ONE value: rows `;`, cells `\|`, a `;` `\|` `%` or `"` in a cell percent-encoded. Checked before any sheet: the table's shape; every title block named - `titleBlockName` or a Title Block cell, by type name or Family: Type, a name two families share refused, `none` only said outright - one not found refuses the call and nothing is made. Per row: no number, a number twice in the table (neither made), a number in the register (placeholders count, capitals ignored) - not made. Each sheet in its own SubTransaction: the number and the name must both take or the row is TAKEN BACK and listed with Revit's words. Each other column matched by name on that sheet: the sheet's own parameter, else its title block instance's; a title block TYPE parameter, a read-only one, two sharing the name, or one holding an element - not written, listed. Text as typed, a tick box from Yes/No (by spec on 2022+, ParameterType on 2020-2021, both by reflection), a whole number, any other number through `SetValueString`. An empty cell writes nothing. Read back after one regeneration. `previewOnly` does the whole job and rolls each row back. One undo |
| `sheets-from-excel` (method) | [`brain/skills/sheets-from-excel.yaml`](../../brain/skills/sheets-from-excel.yaml) | `heron_method`: read the file, put its problems to the modeller, `FIND_SHEETS` for the numbers already used, preview, ask about every unmatched column (never guessed), create, Keep or Discard |

**What is done and what is not.** The card compiles on all eight releases, 2020 to 2027, with the whole library
(`tools/check-fragments-compile.py`, 2026-11-09, a Linux container). Every Revit member it uses is in the card's
compatibility note, looked up with `tools/api-surface --members`; there is no version branch. The reader and the
chat tool are tested without Revit (`tests/test_sheet_list.py`: a real .xlsx written with zipfile - shared and
inline strings, built-in and custom date formats, the 1904 date system, booleans, an error cell, a blank row, a
missing and a repeated number, a multi-line cell, a second sheet; a CSV with a BOM, `;`, a quoted line break and a
title above the table; .xls, .xlsb and .ods refused; the value encoded and decoded back. `tests/test_sheet_list_tool.py`
through the real server). **Nothing here has run in Revit.** The card is DRAFT.

**Not known from the assemblies, and recorded here rather than guessed** (the rows below measure them): whether
the title block instance on a sheet made inside the call is found by its `OwnerViewId` without a regeneration
(MY5); whether a per-row `SubTransaction` rolled back leaves no trace of its sheet in the register or the undo
list, and whether `doc.Regenerate()` inside each row's SubTransaction costs much on a large register (MY8);
which characters Revit refuses in a sheet number or name and in what words (MY7); and how a project's own date
format in a text parameter compares with the ISO date the reader writes (MY6).

**Routing.** Measured 2026-11-09 with `tools/score-routing.py --sentences` (45 sentences frozen before the card was
written: 8 for it, 25 of the neighbouring cards' own words, 12 questions about sheets and the file; lexical and
model - the re-ranked backends NOT ASKED) against main with no card, and `tools/check-routing.py` on a scratch
store. Row 5b-1485 has the result. **A CONDITION FOR PROMOTION TO PROVEN:** *"which sheets are in this excel file"*
- a question - reaches this write on the model backend (before: `EXPORT_SCHEDULE_TO_CSV`, also a write), and is
settled before the card is signed: the owner's recorded decision, or a change that takes it off.

**The model - a SAVED scratch copy of Project1, Revit 2020** (the project `CREATE_SHEETS` was first proved on).
Never a working model. Every run below is rolled back except the arrangement (MY1) and MY9's one run. The job
file is [`tools/jobs/create-sheets-from-table-my-2026-11-09.yaml`](../../tools/jobs/create-sheets-from-table-my-2026-11-09.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`, `--session
<process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **MY0** | *Open.* Revit 2020, the scratch copy, Changes on at the ribbon | The title bar names the scratch copy |
| **MY1** | *Arrange, and KEEP it.* Record the title block type as the Project Browser shows it (Family: Type) and write it into the job file if it is not "A1 metric"; record one sheet number already in the register; add a TEXT project parameter "Discipline" bound to Sheets if the copy has none, and a YES/NO one "Heron Stamp" bound to Sheets. Save | The names recorded; both parameters in a sheet's Properties |
| **MY2** | *The register.* In Excel, a sheet list: Sheet Number, Sheet Name, Drawn By, Checked By, Discipline, Issue Date (a real Excel date), Heron Stamp; rows MY-101 to MY-103 as the job file's table, one row MY-104 with Heron Stamp "Yes"; a blank row; a row with a name and no number; MY-105 twice. Save as .xlsx, and again as CSV UTF-8 and as plain CSV. `heron_sheet_list` on each, in a chat | Each read with the same sheets; the blank row skipped; the row with no number and MY-105 listed, neither MY-105 passed; Issue Date as 2026-..-..; the plain CSV's note naming Windows-1252 when it has accents. An .xls of the same refused with how to save it |
| **MY3** | *The D-30 negative - THE NEGATIVE FIRST - then the positive.* The job file | Negative: titleBlockNotFound true, refused naming NoSuchTitleBlockAnywhere and MY-101 to MY-103, **nothing made** - the sheet count unchanged. Positive: three ids, numberedAs MY-101 to MY-103, valuesSet 8, columnsMatched Drawn By, Checked By, Discipline "the sheet's own parameter (text) on 3 sheet(s)" |
| **MY4** | *The preview.* The positive with previewOnly true | **Nothing new in the Project Browser.** `preview`: three lines "would be made" on the title block with each value; findings "PREVIEW - nothing was kept. 3 of 3 row(s) would be made as sheets, 8 value(s) set" |
| **MY5** | *Title block instance and type.* A column naming a parameter the title block family has as an INSTANCE parameter (record which; if none, add one to a copy of the family - a text label is enough) and one naming a TYPE parameter, rolled back | The instance column in columnsMatched "the title block instance's parameter"; the type column in columnsNotWritten "the title block TYPE" and the type's value in Edit Type unchanged |
| **MY6** | *Kinds of value.* MY2's .xlsx table through the tool (its exact value), titleBlockName as MY1 recorded, rolled back | Heron Stamp ticked on MY-104 (Properties); Issue Date as written by the reader - **record** whether the title block shows it as the office expects; "maybe" in a Yes/No column in valuesNotSet with the sheet made; a misspelt header ("Discipln") in columnsUnmatched with nothing written; "Current Revision" in columnsNotWritten as read-only |
| **MY7** | *Rows Revit refuses.* A row whose number is MY1's existing number; a name with ":" and one with "{"; a number with "\|" typed unencoded in a hand-made value - rolled back | The taken number in numberTaken naming the sheet holding it; the two names in notCreated with **Revit's own words** and "taken back" - and NO sheet for them in the Project Browser, not even under a number Revit picked; the unencoded "\|" refused before anything is made. Record Revit's words |
| **MY8** | *A real register and its time.* The office's own sheet register (100 rows or more), previewOnly true, then made, rolled back | **Record the time** for each. Every row accounted for: made, or in notCreated with its reason |
| **MY9** | *Kept, through `try-once`, on the modeller's yes.* MY3's positive on the saved copy, the Project Browser in view | Keep or Discard. Look: three sheets, the title block drawn, Drawn By and Checked By in the title block's labels; one Ctrl+Z (before Keep) takes all three away |
| **MY10** | *Revit 2025 or later.* The copy upgraded in Revit 2025, 2026 or 2027: MY3's two legs and MY6, rolled back | As on 2020 - the Yes/No column recognised by its spec on 2025+, the same values set |
| **MY11** | *The whole method in a chat.* "make the sheets from this Excel: <MY2's .xlsx>" in a chat with Heron | The host calls `heron_method` for `sheets-from-excel`, reads the file with `heron_sheet_list`, puts the file's problems and the missing title block to the modeller, runs `FIND_SHEETS` with the numbers, and - while the card is DRAFT - says that no chat tool runs it: the preview on the command line (`fragment ... --write`, rolled back), the creation through `try-once` on the modeller's ask; never sent through another tool. It asks about each unmatched column; it never edits the value by hand |
