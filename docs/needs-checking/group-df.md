# Needs checking — Group DF

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-12 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group DF - a shared parameter definition created in the shared parameter file first, then added or replaced (2026-10-12)

On Revit 2020 the owner asked for an outdoor unit family's refrigerant connector sizes - the family
parameters **Gas Pipe Diameter** (12.7 mm) and **Liquid Pipe Diameter** (6.35 mm) - to become shared
parameters. His shared parameter file (`D:\04_BIM\06_Shared_Parameters\Shared_Parameters.txt`, UTF-16,
marked "Do not edit manually") has no refrigerant names, and `ADD_FAMILY_SHARED_PARAMETERS` only binds
what that file already holds, so he made them by hand in Manage > Shared Parameters
([row 5b-438](../fragment-issues/section-5b-rows-401-425.md)).

**What was built, by widening - no new capability.** `ADD_FAMILY_SHARED_PARAMETERS` version 3
(FRG-PAR-031) takes an optional `createMissing`: true, and a name the file does not have may be written
`Group: Name [Data type]` - in `parameterNames` when adding, or as the SharedName of a pair when
replacing ([Group DE](group-de.md)'s `replaceExisting`), so the owner's own job is one call. The colon
and the brackets cannot be misread, because Revit refuses both in a parameter's name. **The group and
the data type are never defaulted** - either missing is refused. A name the file already holds is used
as it is when its data type is the one written, and refused when it is another; an existing definition
is never touched. **Every refusal comes before the file is written**, the family's own checks and a
read-only file included. Then each new definition is read back twice - by Revit opening the file again,
and in the file's own text on disk (its `PARAM` line by GUID, its `GROUP` line by id) - before the family
changes. Version 2's proof is kept as a record and the card reads STALE until this group proves it.

**The card is now ADMIN ([D-106](../decisions/D-106.md)).** Creating writes the office's file, which is
outside the model: D-106 names exactly that consequence - *a parameter written into the shared parameter
file stays in that file* - and `ADD_PROJECT_PARAMETER`, which also writes it, is ADMIN. A card carries
one risk, so **adding and replacing without `createMissing` need the Admin switch too** from version 3.
That is stricter, never looser, and **the owner kept it so on 2026-10-12: "keep it ADMIN"** (DF9). One consequence:
`batch-prove` sends no ADMIN fragment (D-106 point 6), so no batch runs a job for this card - the two
earlier job files for it ([`family-settings-shared-2026-10-01.yaml`](../../tools/jobs/family-settings-shared-2026-10-01.yaml)
and [`replace-family-shared-parameters-2026-10-12.yaml`](../../tools/jobs/replace-family-shared-parameters-2026-10-12.yaml))
included; this group's runs use `validate --allow-publish --write` by name instead. Its dry run said
WOULD RUN for such a job when this group was written; since row 5b-440 (#492) it says WILL NOT RUN IN A
BATCH and prints that command.

**Measured without Revit, 2026-10-12.** `ExternalDefinitionCreationOptions(name, ParameterType)` is 2020
to 2022 and `(name, ForgeTypeId)` 2022 to 2027; `DefinitionGroups.Create`, `Definitions.Create` and
`NamingUtils.IsValidName` 2020 to 2027; `SpecUtils.GetAllSpecs` 2022 to 2027 (`tools/api-surface
--members`, all eight releases). It compiles on all eight. The parser, the near-miss matcher and the
on-disk read-back passed 13 checks in a scratch console against a UTF-16 file written in Revit's layout -
**a file this code wrote, not one Revit wrote**: DF2 is the first time it reads Revit's own. Routing, measured through `heron_brain.lookup` on Revit 2020 and 2024: the two new
utterances reach it by identity; "our shared parameter file has no Gas Pipe Diameter - create it and
add it to this family" ranks it first; "make Gas Pipe Diameter and Liquid Pipe Diameter shared
parameters, our file does not have them yet" goes to `REPORT_PIPE_SEGMENTS` on the pipe words - a read,
so nothing is written by it - recorded, not chased. **Nothing
below has run in Revit.** The arrangement is in
[`create-family-shared-parameters-2026-10-12.yaml`](../../tools/jobs/create-family-shared-parameters-2026-10-12.yaml),
read back by `batch-prove --dry-run` as 1 WILL NOT RUN IN A BATCH - risk ADMIN, with the command to prove
it by name (measured 2026-10-12, after #492).

**Reviewed 2026-10-12** (Codex was out of credit, so a separate review read the code): no
refusal after the write, version 2 unchanged with `createMissing` off. Two points were acted on:
every definition is now bound from the file as Revit opened it again, found by GUID, in case a second
opening leaves the first one's objects stale; and replacing reads its "before" values before the file is
written, so after the write only Revit's own add or replace and the read-back can fail - each naming
what the file keeps.

**The one thing to watch: whether Revit writes a new definition to disk at once.** The on-disk
read-back assumes `Definitions.Create` saves the file when it returns. If it does not, DF2 fails safe -
it throws, the family rolls back, and the message says what the file keeps - and that is a finding about
Revit, recorded as it is, not something to work around.

**Never the office's file, never his families.** Manage > Shared Parameters is set to a scratch copy for
the whole group; write down the file set before and put it back by hand afterwards - the setting is
outside every Undo. With `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are
connected, `< /dev/null` on every line, and **Changes and Admin both on** in Revit's ribbon.

| # | Run | Look for |
|---|---|---|
| **DF1** | Arrange on **Revit 2020**: copy any valid shared parameter file to `%TEMP%\heron-work\df\heron-df-shared.txt`, with a group "Heron Test" holding **Heron Test Tag** (Text) and no group "Refrigerant"; set Manage > Shared Parameters to it, noting the file set before; copy it aside as `heron-df-before.txt`. File, New, Family, Metric Generic Model.rft, saved as **Heron Create Shared Test** | the scratch file set; the family open |
| **DF2** | The `validate --allow-publish --write` line in the job file's header | positive: `created` two entries - Heron Gas Pipe Diameter and Heron Liquid Pipe Diameter, Pipe Size, group Refrigerant, a GUID each and the file's word for the data type; `added` two with the same GUIDs; findings naming the group made and that Undo does not reach the file - record them whole. negative ("Identity Data: Heron Test Tag [Length]"): refused - already in the file as Text - `created` EMPTY. Then: Family Types shows neither (rolled back - BD1), and the scratch file in Notepad has a Refrigerant GROUP line and both PARAM lines with the GUIDs reported |
| **DF3** | The same positive again, by hand through `revit_change`, kept | `created` EMPTY, "already in the shared parameter file" for both and nothing written; the two added - Family Types shows both with the shared marker under Mechanical. The second route: Manage > Shared Parameters lists both under Refrigerant as Pipe Size |
| **DF4** | **The owner's case, on the scratch family**: `ADD_FAMILY_PARAMETERS` "Gas Pipe Diameter, Liquid Pipe Diameter", Length, type; `SET_FAMILY_TYPE_VALUES` 12.7 and 6.35 mm. First `revit_change` replaceExisting true, createMissing true, "Gas Pipe Diameter=Refrigerant: Gas Pipe Diameter [Pipe Size]; Liquid Pipe Diameter=Refrigerant: Liquid Pipe Diameter [Pipe Size]"; then the same with `[Length]` | the Pipe Size call **refused** - Length against Pipe Size, Group DE's kind rule - with the scratch file's bytes unchanged. The Length call: **either** created 2 and replaced 2 under the SAME names, values 12.7 and 6.35 mm kept; **or** Revit refuses a shared parameter of the same name in place - a throw naming what the scratch file now keeps, the family rolled back. Which of the two answers Group DE's DE6; record which. So the owner's own family takes `[Length]`, matching what his family parameters hold |
| **DF5** | The refusals, each with the scratch file's bytes compared to `heron-df-before.txt` after (`fc /b`): "Heron New Thing [Length]" (no group); "Refrigerant: Heron New Thing []" (no type); "Refrigerant: Heron Gas Pipe Diameter [Pipe Sise]"; createMissing absent with "Refrigerant: X [Length]"; "Refrigerant: Heron Test Tag2 [Text]" with a family parameter Heron Test Tag2 in the family; the scratch file marked read-only | each refused with its reason - the typo naming Pipe Size as close, the clash pointing to replaceExisting - and **the file's bytes unchanged every time**: the family's checks come before the file |
| **DF6** | createMissing true, "Refrigerant: Heron Speed [Velocity]" | refused naming each data type "Velocity" fits (HVAC and piping), **or** created once - record which; a probe of the matcher, not a negative |
| **DF7** | DF2 with Admin OFF (Changes on) | refused before anything is sent, naming the Admin switch - and the scratch file unchanged |
| **DF8** | DF2 and DF4 on **Revit 2024** and **2027** - the ForgeTypeId side: `SpecUtils.GetAllSpecs`, `GetDataType` | the same answers; the file's word for Pipe Size the same as 2020's |
| ~~**DF9**~~ | **DECIDED 2026-10-12 by Ajmal PS: "keep it ADMIN".** The card stays ADMIN for adding and replacing as well as creating; no mode is split out for the add-in to gate on its own | nothing to run - the decision is recorded here and in [row 5b-438](../fragment-issues/section-5b-rows-401-425.md) |
| **DF10** | The signature, on the owner's word only, after DF2, DF3, DF4 and DF5 | `python brain/heron_validate.py accept add-family-shared-parameters --by "Ajmal PS"` |

## The run of 2026-10-20 (Revit 2020, the owner's laptop) - version 4 signed, PROVEN

**Arranged as DF1.** A scratch shared parameter file, `%TEMP%\heron-work\df\heron-df-shared.txt`, was written
fresh: UTF-16 as Revit writes one, group `Heron Test` holding `Heron Test Tag` (Text), no `Refrigerant`. Revit's
Manage > Shared Parameters was set to it by a rolled-back probe. Before, it was the office's
`D:\04_BIM\06_Shared_Parameters\Shared_Parameters.txt`. It was set back to that file at the end and read back.
The office file was never written: its time stayed 00:39. The family was made by `CREATE_FAMILY_DOCUMENT`
(PROVEN) from `Metric Generic Model.rft`: `%TEMP%\heron-work\df\Heron Create Shared Test.rfa`, opened in a
window. Changes and Admin were on. Every run was rolled back (`--write`, no `--apply`) except the arrangement
steps, which used PROVEN tools on the scratch family.

| # | Result |
|---|---|
| DF2 | **PASS** on version 3, and again on version 4. Version 3: `created` 2 - Heron Gas Pipe Diameter and Heron Liquid Pipe Diameter, Pipe Size, group Refrigerant - and `added` 2 with the same GUIDs (`f66b1ded...`, `8ed2844c...`). The file then held a `GROUP 2 Refrigerant` line and both `PARAM` lines with those GUIDs, `PIPE_SIZE`. Revit rewrote the header with a `HIDEWHENNOVALUE` column. Negative `Identity Data: Heron Test Tag [Length]`: refused, `created` 0, `Heron Test Tag` still TEXT. **The signed record is version 4's**, with fresh names (Heron Suction / Heron Discharge Pipe Diameter), because version 3's two were already in the file |
| DF3 | **NOT RUN.** A kept run "through `revit_change`" cannot happen while a card is DRAFT ([D-117](../DECISIONS.md), later than this group), and `try-once` takes MODIFY only - this card is ADMIN. Now that the card is PROVEN, it can be run as written |
| DF4 | **FAIL on version 3, fixed in version 4 ([row 5b-1276](../fragment-issues/section-5b-rows-401-425.md)).** The arrangement was Gas Pipe Diameter and Liquid Pipe Diameter as Length type parameters, with 12.7 and 6.35 mm in type `DF Test`. `[Pipe Size]` was **refused** - *"holds Length and the shared ... would hold Pipe Size"* - and the file's bytes were unchanged. `[Length]` on version 3: **Revit refused, "Parameter replacement failed."**, though no formula read either parameter. The family was rolled back, and the two definitions createMissing had written stayed in the scratch file, as the message said. Isolated: the same call with the definitions already there failed the same way. Replacing Gas Pipe Diameter with a shared `DF Gas Diameter` (another name) replaced it. A probe that renamed the family parameter first was then given the same-name shared one. **Version 4 renames it out of the way first:** `replaced` 2, Gas Pipe Diameter -> Gas Pipe Diameter and Liquid -> Liquid, Length, type, the values 12.7 and 6.35 mm kept (read back by type) |
| DF5 | **PASS**, every leg rolled back with the file's bytes compared before and after - **unchanged every time**. No group: *"names no group ... The group is never chosen for you"*. No type: *"names no data type in its brackets"*. `Pipe Sise`: *"Close: Pipe Size (PipeSize)"*. createMissing left out: *"nothing is written to the file unless that is asked for"*. A family parameter `Heron Test Tag4` of that name: *"would be two under one name ... replace it instead: replaceExisting true"*. The file read-only: *"is read-only, so nothing can be created in it"*. Two earlier tries at the clash leg made nothing to clash with - the arrangement step failed both times (a wrong input name, then a heading split by the shell) - so `Heron Test Tag2` and `Heron Test Tag3` were created and added as asked, and are in the scratch file |
| DF6 | **Refused, recorded:** *"'Velocity' is 3 data types in this Revit - Velocity (HVACVelocity), Velocity (PipingVelocity), Velocity (StructuralVelocity). Name one by the word in brackets."* The file was unchanged |
| DF7 | **NOT RUN** - it needs the owner to turn Admin off |
| DF8 | **NOT RUN** - Revit 2024 and 2027 |
| DF10 | **Signed by Ajmal PS, 2026-10-20**, on his word ("finish it all"), on version 4's DF2 record. Fingerprint `d61152201bfbd446` |

**Group DE's DE6, answered on the way:** Revit 2020 refuses a same-name replace with no formula reading it.
DE6 expected it to be allowed. Version 4 makes it work by renaming first.
