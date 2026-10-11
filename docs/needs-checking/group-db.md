# Needs checking — Group DB

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-11 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group DB - a family's OmniClass Number read, set, cleared and checked against Revit's own list (2026-10-11)

On 2026-10-10 a DN50 PN16 gate valve family (Pipe Accessories, Revit 2024) was built through Heron, and
the modeller set its OmniClass Number by hand: `SET_FAMILY_SETTINGS` refused it and no lookup found
anything ([row 5b-422](../fragment-issues/section-5b-rows-401-425.md)). The owner's rules for the repair:
reuse or widen before adding, build it general - every category, every classification field - and
read, set, change, clear and check, not only set.

**What was built, by widening - no new capability.** `SET_FAMILY_SETTINGS` v2 (FRG-DOC-036) takes
"OmniClass Number" - "Classification Number" from Revit 2026 - and writes only a code Revit's own
taxonomy file files under the family's category; anything else is refused with that category's codes
listed, and blank, `none` or `clear` empties it. It reads the code back, all or nothing, and reports
whether Revit filled the title, the title was written from the file, or Revit holds it read-only.
`REPORT_FAMILY_TEMPLATE` v2 (FRG-DOC-037) shows the number and title among the family's settings and
lists the file's codes for the category in `classificationCodes`. `CHECK_FAMILY_STANDARDS` v3
(FRG-QA-021) flags a blank code (a WARN in a family, counted in a project unless `requiredParameters`
names it), a code not in the file, and one filed under another category or none; it checks each
type's Assembly Code against the document's Assembly Code table and its Keynote against the keynote
table, and a missing table is NOT CHECKED. Assembly Code and Keynote keep their setters,
`SET_FAMILY_TYPE_VALUES` in a family and `SET_PARAMETER_VALUES_BY_ID` in a project. The eight family
methods classify right after `SET_FAMILY_CATEGORY`. The two PROVEN cards went back to DRAFT, their
old proofs kept as records.

**Measured without Revit, 2026-10-11.** The built-in parameters were renamed at 2026 with their
numbers kept (`OMNICLASS_CODE` -1002502 is `CLASSIFICATION_CODE` there), so the code finds the name
the running Revit has. The taxonomy is a TAB-separated file no API reads, `OmniClassTaxonomy.txt` to
2025 and `Classification_Taxonomy.txt` from 2026, read from `Application.CurrentUsersDataFolderPath`
only; on Revit 2026 and 2027 it is a 14-line sample ([row 5b-423](../fragment-issues/section-5b-rows-401-425.md)).
All three compile on Revit 2020 to 2027 (`tools/check-fragments-compile.py`), and the eight new
sentences reach their own card on both retrieval backends. **Nothing below has run in Revit.** The
arrangements and commands are in the three job files:
[`set-family-settings-omniclass-2026-10-11.yaml`](../../tools/jobs/set-family-settings-omniclass-2026-10-11.yaml),
[`report-family-template-omniclass-2026-10-11.yaml`](../../tools/jobs/report-family-template-omniclass-2026-10-11.yaml) and
[`check-family-standards-classification-2026-10-11.yaml`](../../tools/jobs/check-family-standards-classification-2026-10-11.yaml),
each read by `batch-prove --dry-run` as 1 WOULD RUN.

Never on the owner's own families - the fire damper and the gate valve open on 2026-10-11 are his and
unsaved. With `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are connected, and
`< /dev/null` on every line.

| # | Run | Look for |
|---|---|---|
| **DB1** | Arrange on Revit 2024, from the metric templates: **Heron OmniClass Valve Test** (Pipe Accessory, OmniClass Number blank), **Heron OmniClass Wrong Valve** (Pipe Accessory, OmniClass Number 23.30.10.00 set by hand - a Doors code), **Heron OmniClass Project** (Architectural, the wrong valve loaded) and **Heron OmniClass Clean Project** (Architectural, only the valve test loaded, once its number is set). Save each | the arrangement each job file names. Read the clean project's `offStandard` first with `CHECK_FAMILY_STANDARDS` and empty values: it must be 0 before the run, or re-arrange |
| **DB2** | `python tools/batch-prove.py tools/jobs/set-family-settings-omniclass-2026-10-11.yaml` | positive: `changed` 1, settingReport *OmniClass Number (blank) -> 23.65.55.14.14 Isolation Valves for Liquid Services* and **which title outcome it names** - the first evidence of whether an API write fills OmniClass Title. negative (23.30.10.00): refused naming **Doors**, the 21 Pipe Accessories codes listed, `changed` 0. Look at Family Category and Parameters afterwards anyway - rollback has failed on this PC before (BD1) |
| **DB3** | By hand through `revit_change` on the valve test: the positive twice; *OmniClass Number=23.99.99.99*; *OmniClass Number=clear*; with the code set again, *OmniClass Number=* (nothing after the equals) | `alreadySet` 1 the second time; 23.99.99.99 refused as not in the file, nothing changed; both clears leave it blank, read back. The last settles whether an empty value reaches the fragment as "" (the add-in's NamedValues keeps it, by reading) and whether `Parameter.Set("")` clears |
| **DB4** | The same call with **Heron OmniClass Project** in front | `notAFamily` true, the refusal naming `OPEN_FAMILY_FOR_EDITING` and `LOAD_FAMILY`, nothing changed |
| **DB5** | The second route: Family Category and Parameters in the valve test after DB2's positive is kept | the OmniClass Number and Title Revit shows are the ones the answer reported |
| **DB6** | `python tools/batch-prove.py tools/jobs/report-family-template-omniclass-2026-10-11.yaml`, with the valve test's number set to 23.65.55.14.14 | positive: `classificationCodes` 21 codes, 23.65.55.14.14 among them, and the per-user file's path; templateReport's settings showing the number and title. negative (the project): `notAFamily` true, `classificationCodes` empty. Then a new Detail Item family: it must SAY the file gives Detail Items no code, not come back empty |
| **DB7** | `python tools/batch-prove.py tools/jobs/check-family-standards-classification-2026-10-11.yaml` | positive: `offStandard` 1 or more, the wrong valve's row *filed under Doors in the classification file, not Pipe Accessories* with its fix, `classificationReport` counting it and naming the file. negative (clean project): `offStandard` 0. **If the row says NOT CHECKED instead**, a loaded family's type does not carry the OmniClass Number in its own parameters - an answer to this group, not a failure of the card: read which of the type or its family carries it |
| **DB8** | `CHECK_FAMILY_STANDARDS` through `revit_read` in the Family Editor on each family of DB1, and on a Detail Item family with the number blank | the wrong valve FAIL *filed under Doors*; the valve test PASS; the Detail Item NOT CHECKED. And whether a family document holds an Assembly Code table and a keynote table: a type's Assembly Code reads NOT CHECKED where it does not |
| **DB9** | In the clean project, give one loaded type an Assembly Code that is not in the project's Assembly Code table, through `SET_PARAMETER_VALUES_BY_ID`, then DB7's command against that project | that type off standard, *not in the Assembly Code table*. And whether `ClassificationEntry.CategoryId` is ever filled from the Uniformat file - the *entry for another category* check fires only if it is |
| **DB10** | DB2 and DB6 on **Revit 2026** or **2027**, the arrangement made again from that release's templates | the labels read *Classification Number* and *Classification Title*; a code refused as the SAMPLE file Revit ships, with the path; `classificationCodes` saying the same. And whether Revit 2026 reads its classification file from anywhere else - an Options setting, Revit.ini - [row 5b-423](../fragment-issues/section-5b-rows-401-425.md) |
| **DB11** | DB2, DB6 and DB7 on **Revit 2020** | the same answers as on 2024 for 23.65.55.14.14 and 23.30.10.00, which the 2020 file files the same way - but it differs from 2024's in 45 rows and gives Pipe Accessories **22** codes, not 21 (read 2026-10-11), so the lists are 2020's own. Compiled on all eight releases; what Revit DOES was measured on none |
| **DB12** | The signatures, on the owner's word only, each after its own run and its second route | `python brain/heron_validate.py accept set-family-settings --by "Ajmal PS"`, the same for `report-family-template` and `check-family-standards` |
| **DB13** | First use, not proof: on the owner's word, after DB2 has passed, his DN50 PN16 gate valve gets *OmniClass Number=23.65.55.14.14* through `revit_change` | the number and title read back in Family Category and Parameters. The family is his to save |
