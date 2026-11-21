# Needs checking — Group OZ

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group OZ - CREATE_SHEETS version 4, the sheets of a table and of typed numbers made by one card: CREATE_SHEETS_FROM_TABLE merged in, with D-193's three rules - DRAFT until proved (2026-11-21)

**Tracked as GitHub issue [#988](https://github.com/Ajmalpshaik/Heron-AI/issues/988) since 2026-11-21** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** the owner's *"merge: yes"* on issue [#1080](https://github.com/Ajmalpshaik/Heron-AI/issues/1080),
2026-11-21, the merge rows [5b-1486](../fragment-issues/section-5b-rows-1476-1500.md) and
[5b-1875](../fragment-issues/row-5b-1875.md) planned: `CREATE_SHEETS_FROM_TABLE` (PROVEN v1, signed 2026-11-13) was
merged into [`CREATE_SHEETS`](../../brain/fragments/create-sheets/fragment.yaml) (PROVEN v3, signed 2026-11-02) as its
version 4 and is DEPRECATED. Version 4 takes a `sheets` table beside `numbers` and `names`, with the table card's code
moved whole, and holds three rules for both routes - [D-193](../decisions/D-193.md): a sheet whose number or name
Revit refuses is TAKEN BACK and listed with Revit's words (this also mends [row 5b-1893](../fragment-issues/row-5b-1893.md));
a number asked for twice makes NEITHER; at most 1,000 sheets in one call, typed numbers too. [D-183](../decisions/D-183.md)'s
four choices hold for the table as they were. Its C# changed, so the card is **DRAFT** until this group is run and
Ajmal PS signs (D-30); version 3's proof is kept on the card as the record. Until then *make a new sheet*,
*duplicate these sheets* and *make the sheets from this Excel* leave `revit_change` (D-117) - the command line
previews them, rolled back, and `try-once` runs them once. Written in a cloud container with no Revit; it compiles on
2020 to 2027 (`tools/check-fragments-compile.py`, 2026-11-21). **Nothing here has run in Revit.**

**Group MY's checks are carried here.** [Group MY](group-my.md) asked them of `CREATE_SHEETS_FROM_TABLE`; the same
code now answers as `CREATE_SHEETS`, so OZ1, OZ3, OZ7 and OZ9 to OZ15 ask them again of version 4 - MY1 and MY2 in
OZ1 and OZ9, MY3 in OZ3, MY4 in OZ7, MY5 to MY7 in OZ10, MY8 and MY12 in OZ13, MY9 in OZ11, MY10 in OZ14, MY11 in
OZ15, MY13 in OZ12. Nothing more is run from Group MY.

**The model is a scratch copy, never a working model** - a SAVED copy of Project1, Revit 2020 (the copy
MY1 arranges, if it was made). *Corrected 2026-11-21 by the review of this change:* this said Project1 was the
project `CREATE_SHEETS` was first proved on - git history gives Snowdon-scratch (Revit 2024) for its first proof
and P1102 MEP for version 3's, title block "A0 metric"; and "A1 metric" below is the name Group MY expected, not one
a proof recorded. OZ1 records the real type before anything else runs. The job file
[`tools/jobs/create-sheets-oz-2026-11-21.yaml`](../../tools/jobs/create-sheets-oz-2026-11-21.yaml) holds the table's
D-30 pair (`--dry-run` reads it back and sends nothing). Every run below is ROLLED BACK except OZ1 and OZ11's one
run. Every command line with `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are connected,
and `< /dev/null`, in PowerShell in the Heron folder; the long ones are written out under the table, by row.

| ID | Do this | Pass looks like |
|---|---|---|
| **OZ0** | *Open.* Revit 2020, the scratch copy, Changes on at the ribbon | The title bar names the scratch copy |
| **OZ1** | *Arrange, and KEEP it* (MY1). Record the title block type as the Project Browser shows it (Family: Type) and write it into the job file and the lines below if it is not "A1 metric"; record one sheet number already in the register; add a TEXT project parameter "Discipline" bound to Sheets if the copy has none, and a YES/NO one "Heron Stamp" bound to Sheets. Save | The names recorded; both parameters in a sheet's Properties |
| **OZ2** | *Typed numbers - the D-30 pair, THE NEGATIVE FIRST.* The OZ2 line below | Negative: titleBlockNotFound true, titleBlockProblem *"No title block type called "NoSuchTitleBlockAnywhere" is loaded in ... Nothing was created."*, created empty - **the sheet count unchanged**. Positive: created three ids, numberedAs OZ-101 to OZ-103, namedAs the three names, notCreated empty |
| **OZ3** | *The table - the card's D-30 pair, THE NEGATIVE FIRST* (MY3). The job file | Negative: titleBlockNotFound true, titleBlockProblem naming NoSuchTitleBlockAnywhere and OZ-501 to OZ-503, **nothing made** - the sheet count unchanged. Positive: three ids, numberedAs OZ-501 to OZ-503, valuesSet 8, columnsMatched Drawn By, Checked By and Discipline *"the sheet's own parameter (text) on 3 sheet(s)"*. Then sign: this is the pair the proof records - **but only after OZ15's routing reading holds** (no question reaching `CREATE_SHEETS`, or the owner's word on [row 5b-1937](../fragment-issues/row-5b-1937.md)), because the card being DRAFT is one of that row's guards and signing removes it (review of 2026-11-21) |
| **OZ4** | *D-193 rule 1 - a name Revit refuses takes that sheet back, both routes.* The OZ4 lines below: typed, then the table | Each: created two ids, OZ-201 and OZ-203 (OZ-211 and OZ-213 for the table); a line in notCreated for OZ-202 (OZ-212) with **Revit's own words** - typed: *"Revit refused the name ... so the sheet was taken back. Revit said: ..."*; the table: *"Revit refused it, so this row was taken back - ..."*; and **NO sheet for it** in the Project Browser, not under a number or a name Revit picked. **Record Revit's sentence.** If ":" is accepted, run it again with "{" and record which characters Revit refuses. **The number half (added 2026-11-21, review of this change):** run it again with the refused character in the NUMBER of the middle sheet (OZ-2:02, OZ-21:2 for the table) - **record** whether Revit refuses a number for it; if it does, the same: that row in notCreated with Revit's words and NO sheet; if no number can be made to be refused, say so here, and the card's number-refused path stays unexercised in Revit. **This fails on version 3**, which kept the sheet under Revit's own name and said nothing (row 5b-1893) |
| **OZ5** | *D-193 rule 2 - one number twice makes neither, both routes.* The OZ5 lines below | Each: ONE sheet, OZ-302 (OZ-312); two lines in notCreated, one per entry, each naming entries 1 and 2 and *"neither was made"*; numberTaken empty. **Fails on version 3**, which made OZ-301 and refused oz-301 as already used |
| **OZ6** | *D-193 rule 3 - 1,001 is refused whole, both routes.* The OZ6 lines below | Typed: notCreated one line *"1001 sheet numbers were given; one call makes at most 1000 sheets. Split the list. Nothing was created."*; the table: *"The table has 1001 rows; one call makes at most 1000 sheets ..."* in notCreated and findings; **created empty, the sheet count unchanged**. **Fails on version 3**, which made 1,001 typed sheets (rolled back). **And the limit itself is allowed** (added 2026-11-21): exactly 1,000 typed numbers with previewOnly true - preview lines for all 1,000, findings *"... 1000 of 1000 number(s) would be made"*, nothing kept |
| **OZ7** | *Previews, both routes* (MY4). OZ2's and OZ3's positives with previewOnly true - the OZ7 lines below | **Nothing new in the Project Browser.** Typed: preview two lines *"... on <the title block>: would be made"*, findings *"PREVIEW - nothing was kept. 2 of 2 number(s) would be made"*. Table: three lines with each value, findings *"PREVIEW - nothing was kept. 3 of 3 row(s) would be made as sheets, 8 value(s) set"* |
| **OZ8** | *What is refused before anything, and the copy job.* (a) the table and numbers both given, then the table and names "Typed Name" given without numbers; (b) one sheet selected, numbers left out, numberSuffix "-COPY", previewOnly true; (c) the same without previewOnly; (d) **the copy job's negative** (added 2026-11-21: version 3's proof ran typed numbers only) - make a sheet BY HAND numbered as the source's number + "-COPY" (Revit's own New Sheet), then (c) again on the same source; Ctrl+Z the hand-made sheet afterwards - the OZ8 lines below | (a) notCreated *"Both a table of sheets (sheets) and typed sheet numbers (numbers) were given ... Nothing was created."*, then the same naming *"typed names (names)"* - the names never silently ignored; created empty both times. (b) created empty, the source in notDuplicated, notCreated saying a copy run is not previewed. (c) one copy, numbered as its source + "-COPY", on its source's title block, no views on it - version 3's copy job unchanged. (d) the "-COPY" number in numberTaken, the source in notDuplicated, created empty and **the sheet count unchanged** - no orphan sheet under a number Revit picked |
| **OZ9** | *The register read in a chat* (MY2). In Excel, a sheet list: Sheet Number, Sheet Name, Drawn By, Checked By, Discipline, Issue Date (a real Excel date), Heron Stamp; rows OZ-501 to OZ-503 as the job file's table, one row OZ-504 with Heron Stamp "Yes"; a blank row; a row with a name and no number; OZ-505 twice. Save as .xlsx, and again as CSV UTF-8 and as plain CSV. `heron_sheet_list` on each, in a chat | Each read with the same sheets; the blank row skipped; the row with no number and OZ-505 listed, neither OZ-505 passed; Issue Date as 2026-..-..; the answer names **CREATE_SHEETS** and `create-sheets`, never the retired card, and - the card DRAFT - the command-line preview and try-once, not revit_change. An .xls of the same refused with how to save it |
| **OZ10** | *Columns, kinds of value, rows Revit refuses* (MY5, MY6, MY7). OZ9's .xlsx table through the tool (its exact value), titleBlockName as OZ1 recorded, rolled back; then the same with a column naming a title block INSTANCE parameter (record which; if none, add a text label parameter to a copy of the family), one naming a TYPE parameter, "Current Revision", a misspelt "Discipln", a "maybe" in Heron Stamp, a row with MY1's existing number, and a hand-made value with an unencoded "\|" in a name | The instance column in columnsMatched *"the title block instance's parameter"*; the type column in columnsNotWritten *"the title block TYPE"*, the type's value in Edit Type unchanged; Heron Stamp ticked on OZ-504; "maybe" in valuesNotSet with its sheet made (D-183); "Discipln" in columnsUnmatched; "Current Revision" in columnsNotWritten as read-only; the existing number in numberTaken naming the sheet holding it; the unencoded "\|" refused before anything is made. **Record** whether findings say a title block was *"found only after a regeneration"* - never: the scoped collector needs none and the fallback can go; always: its cost is OZ13's; and whether the title block shows Issue Date as the office expects |
| **OZ11** | *Kept, through `try-once`, on the modeller's yes* (MY9) - version 4's ONE run (D-119). The OZ11 line below: the table OZ-801 to OZ-803 and one row OZ-804 with a name Revit refused in OZ4, on the saved copy, the Project Browser in view | Keep or Discard. Look: three sheets, the title block drawn, Drawn By and Checked By in the title block's labels; OZ-804 in notCreated with Revit's words and NO sheet for it; one Ctrl+Z (before Keep) takes all three away |
| **OZ12** | *What Revit posts at the commit* (MY13) - read off OZ11's run | **Record** whether any warning or error appears as the call is kept. None: "taken back alone" holds in practice. One that undoes the call: the WHOLE call rolled back, one undo, nothing kept, and Revit's words in the answer - the card says that is what happens; record which row causes it |
| **OZ13** | *Time against the run clock* (MY8, MY12). The office's own sheet register (100 rows or more) and two made registers through `heron_sheet_list`, 300 rows and 1,000 rows, each with Drawn By, Checked By, Discipline and one title block INSTANCE column: previewOnly true, then made, rolled back. On the command line a 1,000-row value is refused as too long for one command - run 300 there, and 1,000 once the card is PROVEN or through the bridge client's `validate` with the value from a job file | **Record the seconds** for each, and the findings line *"found only after a regeneration"* (how many). Against `HeronRunClock`'s 60 seconds (`revit.operationTimeoutSeconds`): above about 45 s for 1,000 rows, the ceiling of 1,000 is for the owner to lower. Every row accounted for: made, or in notCreated with its reason |
| **OZ14** | *Revit 2025 or later* (MY10). The copy upgraded in Revit 2025, 2026 or 2027: OZ2's and OZ3's pairs and OZ10's first run, rolled back | As on 2020 - the Yes/No column recognised by its spec on 2025+, the same values set |
| **OZ15** | *The whole method in a chat* (MY11). *"make the sheets from this Excel: <OZ9's .xlsx>"*, then *"make a new sheet OZ-901 called HERON CHAT on <the title block>"*, in a chat with Heron | The host calls `heron_method` for `sheets-from-excel`, reads the file with `heron_sheet_list`, puts the file's problems and the missing title block to the modeller, runs `FIND_SHEETS` with the numbers, and - while the card is DRAFT - says no chat tool runs it: the preview on the command line (`fragment create-sheets ... --write`, rolled back), the creation through `try-once` on the modeller's ask; never through another tool, never `create-sheets-from-table`. The second sentence reaches `CREATE_SHEETS` and is refused by `revit_change` as DRAFT, in words. Then, on the owner's PC (model with the re-ranker), `heron_lookup` of *"which sheets are in this excel file"*, *"what is in this sheet list spreadsheet"* and *"which columns of the sheet list match sheet parameters"*: **record** where each lands - none may be `CREATE_SHEETS`. On lexical and model the second reaches it since the merge ([row 5b-1937](../fragment-issues/row-5b-1937.md)); say whether the re-ranker does too |

### The command lines, by row

Each is ONE line in PowerShell; it is broken here only to be read. Change "A1 metric" to what OZ1 recorded.

**OZ2** - the typed D-30 pair, its own record (the job file's is the table's):

```text
python mcp/client/heron_bridge_client.py validate --out brain/proof-drafts/runs/create-sheets-oz2.json --write
  --set "numbers=OZ-101,OZ-102,OZ-103" --set "names=HERON TEST SHEET 1,HERON TEST SHEET 2,HERON TEST SHEET 3"
  --set "titleBlockName=A1 metric"
  --negative-set "numbers=OZ-101,OZ-102,OZ-103" --negative-set "names=HERON TEST SHEET 1,HERON TEST SHEET 2,HERON TEST SHEET 3"
  --negative-set "titleBlockName=NoSuchTitleBlockAnywhere" create-sheets
```

**OZ4** - typed, then the table:

```text
python mcp/client/heron_bridge_client.py fragment create-sheets --write --set "numbers=OZ-201,OZ-202,OZ-203"
  --set "names=HERON GOOD 1,HERON BAD:NAME,HERON GOOD 3" --set "titleBlockName=A1 metric"
python mcp/client/heron_bridge_client.py fragment create-sheets --write
  --set "sheets=Sheet Number|Sheet Name;OZ-211|HERON GOOD 1;OZ-212|HERON BAD:NAME;OZ-213|HERON GOOD 3"
  --set "titleBlockName=A1 metric"
```

**OZ5** - typed, then the table:

```text
python mcp/client/heron_bridge_client.py fragment create-sheets --write --set "numbers=OZ-301,oz-301,OZ-302"
  --set "titleBlockName=A1 metric"
python mcp/client/heron_bridge_client.py fragment create-sheets --write
  --set "sheets=Sheet Number|Sheet Name;OZ-311|HERON TWICE A;oz-311|HERON TWICE B;OZ-312|HERON ONCE"
  --set "titleBlockName=A1 metric"
```

**OZ6** - 1,001 typed numbers, then a 1,001-row table (each value is under a Windows command line's 32,767 characters):

```text
$n = (1..1001 | ForEach-Object { 'OZ-{0:D4}' -f $_ }) -join ','
python mcp/client/heron_bridge_client.py fragment create-sheets --write --set "numbers=$n" --set "titleBlockName=A1 metric"
$t = 'Sheet Number|Sheet Name;' + ((1..1001 | ForEach-Object { 'OZ-T{0:D4}|HERON CAP {0}' -f $_ }) -join ';')
python mcp/client/heron_bridge_client.py fragment create-sheets --write --set "sheets=$t" --set "titleBlockName=A1 metric"
```

**OZ7** - the previews:

```text
python mcp/client/heron_bridge_client.py fragment create-sheets --write --set "numbers=OZ-401,OZ-402"
  --set "titleBlockName=A1 metric" --set previewOnly=true
python mcp/client/heron_bridge_client.py fragment create-sheets --write --set previewOnly=true
  --set "sheets=Sheet Number|Sheet Name|Drawn By|Checked By|Discipline;OZ-501|HERON TEST PLAN 1|AP|JS|Mechanical;OZ-502|HERON TEST PLAN 2|AP|JS|Mechanical;OZ-503|HERON TEST PLAN 3|AP||Mechanical"
  --set "titleBlockName=A1 metric"
```

**OZ8** - (a), both lines; then (b), (c) and - after the hand-made "-COPY" sheet - (d), with one sheet selected in the Project Browser:

```text
python mcp/client/heron_bridge_client.py fragment create-sheets --write --set "numbers=OZ-701"
  --set "sheets=Sheet Number|Sheet Name;OZ-702|HERON BOTH" --set "titleBlockName=A1 metric"
python mcp/client/heron_bridge_client.py fragment create-sheets --write --set "names=Typed Name"
  --set "sheets=Sheet Number|Sheet Name;OZ-703|HERON BOTH" --set "titleBlockName=A1 metric"
python mcp/client/heron_bridge_client.py fragment create-sheets --write --set "numberSuffix=-COPY" --set previewOnly=true
python mcp/client/heron_bridge_client.py fragment create-sheets --write --set "numberSuffix=-COPY"
python mcp/client/heron_bridge_client.py fragment create-sheets --write --set "numberSuffix=-COPY"
```

**OZ11** - the one kept run, on the modeller's yes in the chat (D-119); use the name OZ4 found Revit refuses:

```text
python mcp/client/heron_bridge_client.py try-once create-sheets
  --set "sheets=Sheet Number|Sheet Name|Drawn By|Checked By;OZ-801|HERON KEPT 1|AP|JS;OZ-802|HERON KEPT 2|AP|JS;OZ-803|HERON KEPT 3|AP|JS;OZ-804|HERON BAD:NAME|AP|JS"
  --set "titleBlockName=A1 metric"
```
