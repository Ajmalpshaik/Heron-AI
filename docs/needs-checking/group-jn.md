# Needs checking — Group JN

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-28 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group JN - a duct or pipe fitting's own rules checked before it is loaded: `CHECK_FAMILY_STANDARDS` version 4 (2026-10-28)

From [row 5b-1346](../fragment-issues/section-5b-rows-401-425.md). The fitting method
([`mep-fitting-family-creation`](../../brain/skills/mep-fitting-family-creation.yaml), deepened the same day on the
owner's request) checks a fitting before it is flexed. `CHECK_FAMILY_STANDARDS` already read most of what a
fitting needs - Work Plane-Based, a Part Type left Normal, connectors pointing in, sizes driven by type
parameters - and four fitting rules were left to be read by hand off `REPORT_FAMILY_CONNECTORS`.

**What changed.** [`impl/any/fragment.cs`](../../brain/fragments/check-family-standards/impl/any/fragment.cs), in a
Pipe Fittings or Duct Fittings family open in the Family Editor: its run connectors counted against its Part
Type (an elbow two, a tee three, a cross four, a cap one - a FAIL; a part whose count is not recorded NOT
CHECKED); each one's System Classification Fitting (a WARN); one primary on the X axis (a WARN); an elbow's
connector Angle driven by an instance parameter (a TYPE one a FAIL, none a WARN). Nothing else in the tool
changed.

**What is done and what is not.** It compiles for all eight releases (`tools/check-fragments-compile.py`,
2026-10-28). **Nothing here has run in Revit.** The card went from PROVEN to DRAFT with this change, as any change
under a proven card's `impl/` does; version 3's proof is kept on it as a record and is stale.

**The models - scratch, never a working project.** Revit 2020 first. The two families the job file
[`tools/jobs/check-family-standards-fittings-2026-10-28.yaml`](../../tools/jobs/check-family-standards-fittings-2026-10-28.yaml)
describes, saved under `%TEMP%\heron-work\group-jn\`. **Revit's own pipe elbow** for JN5 is whichever generic
elbow the installed library holds - its file name differs by release and is not guessed here; write the one used.

| # | Run | Look for |
|---|---|---|
| **JN0** | *Open.* *JN Elbow Good* and *JN Elbow Bad* open in Revit 2020, nothing of the owner's | Both titles in the window list |
| **JN1** | *Arrange, by hand.* *JN Elbow Good* built as the job file says - by the fitting method's steps 3 to 8 if the owner likes, which is Group JH's JH3 too | `REPORT_FAMILY_CONNECTORS`: 2 pipe connectors, Fitting, one PRIMARY pointing right (+X), each angle *(<- Angle)* |
| **JN2** | *Check the arrangement.* `CHECK_FAMILY_STANDARDS` on *JN Elbow Good* | `offStandard` **0**. Anything else is put right in the family before JN3, never judged |
| **JN3** | *The positive - the D-30 positive.* The job file's run, on *JN Elbow Bad* | FAIL *"Part Type is Elbow but the family has 3 pipe connector(s) - that part has 2"*, FIX naming REMOVE_FAMILY_CONNECTOR; WARN the Global connector *"is Global, not Fitting"* |
| **JN4** | *The negative - the D-30 negative.* The job file's negative leg, on *JN Elbow Good* | `offStandard` 0; PASS rows for the count, each connector Fitting, the primary on the X axis and each Angle driven by an instance parameter |
| **JN5** | *Revit's own elbow, read only.* Open it; `CHECK_FAMILY_STANDARDS`; close without saving | **No FAIL from the version 4 rows.** A WARN that Revit's own elbow leaves a connector Angle driven by no parameter means the WARN is wrong and is struck from the code; a FAIL there is a defect in version 4 |
| **JN6** | *An accessory is not a fitting.* A Pipe Accessories family with two Global connectors (Group CB's valve, or any) | None of the version 4 rows appear - they are a fitting's |

**Then sign**, on the owner's word only: `python brain/heron_validate.py accept check-family-standards --by "Ajmal
PS"` once JN3 and JN4 pass, and `heron-status` set to PROVEN in the same change. A row that fails reopens
5b-1346; nothing in a row changes to make it pass.
