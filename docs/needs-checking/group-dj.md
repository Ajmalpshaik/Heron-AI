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
note) a table no type names. It does **not** check whether each type's key value has a row - that is
reported as NOT CHECKED.

**Measured without Revit, 2026-10-12.** Every API member it calls is in all eight releases
(`tools/api-surface --members`); it compiles on all eight (`tools/check-fragments-compile.py`); its
four sentences reach it by identity through the host lookup, and the table readers keep theirs.
**Nothing below has run in Revit.** The arrangement is in
[`check-family-size-tables-2026-10-12.yaml`](../../tools/jobs/check-family-size-tables-2026-10-12.yaml),
read by `batch-prove --dry-run` as 1 WOULD RUN.

**What only Revit can say:** whether `FamilyType.AsString` gives a formula-driven Text parameter's
value; whether Revit matches a table or column name regardless of letter case (reported, not judged); and
whether a family whose `size_lookup` names its table directly, not through a parameter, can exist at
all on any release (docs/43 section 5 saw Revit 2024 refuse it).

Never on a working family. Test copies only.

| # | Run | Look for |
|---|---|---|
| **DJ1** | Arrange on **Revit 2024** the two test families the job file names: "Heron Lookup Clean" (every lookup correct, both types flexed with no dimension at its default) and "Heron Lookup Broken" (one type's table name "NoSuchTable", one column "NoSuchColumn") | both open in the Family Editor |
| **DJ2** | `python tools/batch-prove.py tools/jobs/check-family-size-tables-2026-10-12.yaml` | positive: `problemCount` 2 or more - MISSING TABLE naming the changed type only, MISSING COLUMN listing the real columns. negative: `problemCount` 0 with `lookupsChecked` non-zero |
| **DJ3** | By hand through `revit_read`, a project in front | `notAFamily` true, `problemCount` 0 |
| **DJ4** | The second route: Manage Lookup Tables and Family Types by hand on the broken copy | each reported name matches what is there; the broken type's dimension sits at the formula's default |
| **DJ5** | DJ2 on **Revit 2020** and **2027** | the same answers. Compiled on all eight releases; what Revit DOES was measured on none |
| **DJ6** | The signature, on the owner's word only, after DJ2 and DJ4 | `python brain/heron_validate.py accept check-family-size-tables --by "Ajmal PS"` |
