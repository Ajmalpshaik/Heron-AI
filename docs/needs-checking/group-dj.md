# Needs checking — Group DJ

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-12 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group DJ - every size_lookup in a family checked against the tables it names (2026-10-12)

A `size_lookup` that cannot find its table, or its column, answers with its default and Revit says
nothing - the family builds at the default size. Heron could read a table
([`REPORT_FAMILY_SIZE_TABLE`](../../brain/fragments/report-family-size-table/fragment.yaml)) but not
check the formulas that read it.

**What was built - a new capability.** `CHECK_FAMILY_SIZE_TABLES` (FRG-ELE-078), read-only, in the
family open in front: for every `size_lookup` formula it reads the Text parameter holding the table
name in every type, and names a blank or missing table, a first argument that is not a Text parameter,
a missing column, a table or column name differing only in letter case, an empty table, an unnamed column, and (as a
note) a table no type names.

**Version 2, 2026-10-13 - a row for every type's size.** Each key that is a TYPE parameter holding a
number is read in every type, converted into the unit its column's header declares, and looked for in
the rows to 0.0001 of that unit; a miss is a NO ROW problem naming the type, the table, the key values
and every formula left at its default. Keys are matched from the table's second column on (FRAGMENT-ISSUES
row 5b-281 is still open). A key that is an instance parameter, an expression or text, or a measured key
against a column with no unit, is a ROWS NOT CHECKED note, never a pass.

**Measured without Revit, 2026-10-12.** Every API member it calls is in all eight releases
(`tools/api-surface --members`); it compiles on all eight (`tools/check-fragments-compile.py`); its
four sentences reach it by identity through the host lookup, and the table readers keep theirs.
**Nothing below has run in Revit.** The arrangement is in
[`check-family-size-tables-2026-10-12.yaml`](../../tools/jobs/check-family-size-tables-2026-10-12.yaml),
read by `batch-prove --dry-run` as 1 WOULD RUN.

**What only Revit can say:** whether `FamilyType.AsString` gives a formula-driven Text parameter's
value; whether Revit matches a table or column name regardless of letter case (reported, not judged);
whether a family whose `size_lookup` names its table directly, not through a parameter, can exist at
all on any release (docs/43 section 5 saw Revit 2024 refuse it); and, for version 2, whether
`FamilySizeTable.AsValueString` writes every cell in its column's own unit on every release (seen on
Revit 2020 only), and whether Revit's own match uses a tolerance like 0.0001 of that unit.

Never on a working family. Test copies only.

| # | Run | Look for |
|---|---|---|
| **DJ1** | Arrange on **Revit 2024** the two Generic Model test families the job file names: "Heron Lookup Clean" (table HeronSizes keyed on the TYPE length parameter Size, types A, B, C at 20, 25, 32, every type flexed with no Width or Depth at its 10 mm default) and "Heron Lookup Broken" (type B's Size 33, type C's table name "NoSuchTable", Depth asking for "NoSuchColumn") | both open in the Family Editor |
| **DJ2** | `python tools/batch-prove.py tools/jobs/check-family-size-tables-2026-10-12.yaml` | positive: `problemCount` 3 or more - NO ROW naming type B and "Size = 33", MISSING TABLE naming type C only, MISSING COLUMN listing the real columns; type A in none. negative: `problemCount` 0 with `lookupsChecked` and `rowSearches` non-zero |
| **DJ3** | By hand through `revit_read`: a project in front; then a pipe fitting keyed on its instance Nominal Diameter | the project: `notAFamily` true, `problemCount` 0. The fitting: a ROWS NOT CHECKED note naming Nominal Diameter as an instance parameter, no NO ROW line |
| **DJ4** | The second route: Manage Lookup Tables and Family Types by hand on the broken copy | each reported name matches what is there; type B's Width and type C's Width sit at the 10 mm default |
| **DJ5** | DJ2 on **Revit 2020** and **2027** | the same answers. Compiled on all eight releases; what Revit DOES was measured on none |
| **DJ6** | The signature, on the owner's word only, after DJ2 and DJ4 | `python brain/heron_validate.py accept check-family-size-tables --by "Ajmal PS"` |
