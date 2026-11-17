# NEEDS-CHECKING archive — Group AZ

> **Checks that were done, moved out of the live register.** These are rows of Group AZ of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *`create-sheet-list` version 2: a view list as well as a sheet list, and only the placed views (2026-09-27)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row AZ2

*Moved from the register on 2026-11-17.*

**Check.** The old job, unchanged. One chat run on a test copy: CREATE_SHEET_LIST `listKind` blank (sent as an empty value), `fieldNames=Sheet Number, Sheet Name`, `sortByField=Sheet Number`, then open the schedule

**Expected.** A SHEET list, exactly as version 1 made it: every sheet a row, in number order, `refused` empty. A view list here is a failure - blank must mean sheets **CLOSED 2026-11-13, not passed** - `CREATE_SHEET_LIST` was retired into `CREATE_SCHEDULE` on 2026-11-01 (its `deprecated:` block); `CREATE_SCHEDULE` is PROVEN at version 3, `proof:` dated 2026-11-02. Nothing here runs on the retired card.

---

### Row AZ3

*Moved from the register on 2026-11-17.*

**Check.** The View List the owner asked for, on the WORKING model, as chat runs with no selection-fed step: CREATE_SHEET_LIST `listKind=views`, `scheduleName=View List`, fields such as View Name, Sheet Number, Sheet Name, Title on Sheet, View Scale, Detail Number, `sortByField=Sheet Number` (a name refused is replaced by the real name the refusal lists, and which was used is written here); then FIND_SCHEDULES `nameContains=View List` and SET_SCHEDULE_FILTERS `filterFieldName=Sheet Number`, `matchType=has value`, `filterValue` blank; then REPORT_SCHEDULE_DEFINITION on it

**Expected.** The definition reads back the columns asked for, the filter as Sheet Number HasValue, and the sort by Sheet Number. **Heron reads the schedule's setup, not its rows**, so the owner opens it: one row for each of the four plan views on M-101 to M-104 - the Level 1 Room, HVAC Zone and Space plan layouts and "Level 1 - Air Terminal Layout" - each with its own sheet's number. The schedules on M-105 to M-107 are not rows: a view list is a schedule of views, and a schedule is not one **CLOSED 2026-11-13, not passed** - `CREATE_SHEET_LIST` was retired into `CREATE_SCHEDULE` on 2026-11-01 (its `deprecated:` block); `CREATE_SCHEDULE` is PROVEN at version 3, `proof:` dated 2026-11-02. Nothing here runs on the retired card.

---

### Row AZ4

*Moved from the register on 2026-11-17.*

**Check.** The second route, THEN the signature. View tab, Schedules, View List by hand on the test copy, filtered on Sheet Number has a value, and its rows compared with the views under each sheet in the Sheets branch of the Project Browser. Write what was seen into the `second_route` field of the draft `brain/proof-drafts/` holds for `create-sheet-list`. Only then `python brain/heron_validate.py accept create-sheet-list --by "Ajmal PS"` and set `heron-status: PROVEN`

**Expected.** The hand-made list and the one AZ3 made show the same rows, and every placed view in the Project Browser is one of them **CLOSED 2026-11-13, not passed** - `CREATE_SHEET_LIST` was retired into `CREATE_SCHEDULE` on 2026-11-01 (its `deprecated:` block); `CREATE_SCHEDULE` is PROVEN at version 3, `proof:` dated 2026-11-02. Nothing here runs on the retired card.

---

### Row AZ5

*Moved from the register on 2026-11-17.*

**Check.** The routing, on the owner's PC with the shared store rebuilt from main after this merges: `heron_lookup` on *"create a view list"*, *"make a schedule of the views on sheets"* and *"create a sheet list"*

**Expected.** All three name CREATE_SHEET_LIST. The first two are its own declared sentences since version 2; the third is the job it always had **CLOSED 2026-11-13, not passed** - `CREATE_SHEET_LIST` was retired into `CREATE_SCHEDULE` on 2026-11-01 (its `deprecated:` block); `CREATE_SCHEDULE` is PROVEN at version 3, `proof:` dated 2026-11-02. Nothing here runs on the retired card.

---
