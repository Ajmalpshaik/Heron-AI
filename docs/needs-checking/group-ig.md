# Needs checking — Group IG

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-21 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group IG - which shared parameter file Revit is set to, what is in it, and its GUIDs against the family in front: `REPORT_SHARED_PARAMETER_FILE` (2026-10-21)

From [row 5b-1289](../fragment-issues/section-5b-rows-401-425.md). On 2026-10-21 the owner, adding shared
parameters from his office file to the family *TRG_Duct Accessory_Electric Duct Heater* so he can tag and
schedule its size, asked whether Heron can read which shared parameter file Revit is set to. Nothing could:
three tools touch that setting and all three write. Built the same day, in the cloud, with no Revit:

| What | Where | What it does |
|---|---|---|
| `REPORT_SHARED_PARAMETER_FILE` | [`report-shared-parameter-file`](../../brain/fragments/report-shared-parameter-file/fragment.yaml) | READ - the path Revit is set to now, or none; whether the file is there, marked read-only and opens in Revit; its groups; every parameter with its GUID, data type, group, visible and description, narrowed by `nameContains` and `groupContains`; each name asked for answered by itself; with `compareWithDocument`, the family or project in front by GUID - carried, carried under the file's name with another GUID, or not shared |
| `family-creation` step 2, and the eight family methods that name `ADD_FAMILY_SHARED_PARAMETERS` | [`brain/skills/family-creation.yaml`](../../brain/skills/family-creation.yaml) | the read first, before any shared parameter is added |

**What is done and what is not.** The card validates (`python brain/heron_fragment.py`) and compiles for
2020 to 2027 (`tools/check-fragments-compile.py`); every Revit member it calls was read with
`tools/api-surface --members` on all eight releases. The data type is the one member that moved -
`ParameterType` 2020 to 2022, `GetDataType()` from 2022 - and one `#if REVIT2020 || REVIT2021` picks, so
**2020 and 2021 run one branch and 2022 to 2027 the other: IG10 is the row that runs the second.** The
methods validate (`python brain/heron_skill.py`). **Nothing here has run in Revit.** The card is DRAFT until
these rows pass and the owner signs.

**Two things the card does not claim, and these rows settle.** Whether a family loaded into a project
leaves its shared parameters in the project as elements the comparison reads (IG6), and whether
`OfClass(ParameterElement)` hands back the shared ones too - the code asks both and joins them, so either
answer is right, but which one Revit gives is not known.

**The arrangement.** On **Revit 2020**, all of it in `%TEMP%\heron-work\spf-ig`, never the office's file and
never a working model:

- `heron-ig-shared.txt` - a scratch shared parameter file made in Manage > Shared Parameters > Create, with
  two groups: **Mechanical** holding `Duct_Accessory_Width` and `Duct_Accessory_Height` (both Length), and
  **Identity** holding `Heron_Equipment_Tag` (Text, with a description) and one parameter with **Visible
  off**.
- `heron-ig-other.txt` - a second scratch file holding its own `Duct_Accessory_Height` (Length) - the same
  name, another GUID.
- `Heron SPF Test.rfa` - a new Generic Model family carrying `Duct_Accessory_Width` as a shared parameter
  from the first file, `Duct_Accessory_Height` as a shared parameter from the SECOND file, and
  `Heron_Equipment_Tag` as a plain family parameter, not shared. Then set Revit back to the first file.

Write down the path Manage > Shared Parameters shows, and the first file's modified time, before IG1: IG9
checks neither moved. [`tools/jobs/report-shared-parameter-file-ig-2026-10-21.yaml`](../../tools/jobs/report-shared-parameter-file-ig-2026-10-21.yaml)
runs IG1 and IG2's pair as a batch; the rows below are run by name. A read changes nothing, so there is no
kept run and no try-once.

| # | Run | Look for |
|---|---|---|
| **IG0** | *Set up.* The three files above, `Heron SPF Test` open in the Family Editor, Revit set to `heron-ig-shared.txt` | Manage > Shared Parameters shows the first file. Note its path and the file's modified time |
| **IG1** | *The negative FIRST.* `nameContains` `ZZZ_Not_In_The_File`, nothing else | parameterCount **0**, parameterList empty, inFile **4** - the file was read - and a finding: *"ZZZ_Not_In_The_File" is not in the shared parameter file ... - no name holds it*. matchedByGuid 0, comparison empty |
| **IG2** | *The positive.* Nothing narrowed | sharedFile the path IG0 noted; fileExists, opensInRevit true; fileReadOnly false; groups `Identity (2)  \|\|  Mechanical (2)` or the file's own order; parameterCount = inFile = **4**, the count the dialog shows. **Open the file in Notepad:** each line's GUID matches its PARAM line; each data type reads as the dialog names it (Length, Text) with Revit's own name in brackets; the Visible-off one says HIDDEN; the description shows |
| **IG3** | `nameContains` `Duct_Accessory_Width`; then `duct_accessory_width`; then `Duct_Accessory` | **First:** parameterCount 1, a finding *is in the file, under Mechanical, GUID ...*. **Second:** *not in the file with those capitals; "Duct_Accessory_Width" is ...*. **Third:** parameterCount 2, a finding that no parameter is named exactly that and 2 names hold it. `groupContains` `Identity` alone: parameterCount 2 |
| **IG4** | *Compared, the positive.* `nameContains` `Duct_Accessory_Width`, `compareWithDocument` true | comparedWith `Heron SPF Test (family)`; matchedByGuid **1**; comparison *carried by its GUID ... - a shared type parameter*; guidMismatches 0, notShared 0. Check the GUID against Family Types > the parameter > Modify |
| **IG5** | *Compared, the classic mistake.* Nothing narrowed, `compareWithDocument` true | matchedByGuid **1** (Width only); guidMismatches **1** - `Duct_Accessory_Height` with BOTH GUIDs named, the family's being the second file's; notShared **1** - `Heron_Equipment_Tag`, *not shared*. Neither counted as carried; the finding also says 1 shared parameter in the family comes from another file. Its four numbers - 1, 1, 1 and 1 not in it - add up to parameterCount |
| **IG6** | *A project.* A new scratch project, `Heron SPF Test` loaded into it; a project parameter added from `heron-ig-shared.txt` (`Heron_Equipment_Tag`, by hand, Manage > Project Parameters). `compareWithDocument` true | comparedWith *(project)*; `Heron_Equipment_Tag` carried by its GUID. **Record whether** the loaded family's `Duct_Accessory_Width` counts as carried - whether Revit keeps a loaded family's shared parameter in the project as an element. The card claims neither; write what Revit said into the card's purpose |
| **IG7** | *No file, and a file that is not there.* Empty Manage > Shared Parameters (Browse, Cancel leaves it - clear the box); run it. Then set it to `heron-ig-missing.txt`, a name with no file, and run it | **First:** fileState `none set`, sharedFile empty, inFile 0, parameterCount 0. **Second:** fileState `set, but the file is not there`, fileExists false, opensInRevit false - and **no file of that name in the folder afterwards**: nothing created it. Set Revit back to the first file |
| **IG8** | *Read-only.* Mark `heron-ig-shared.txt` read-only in Windows (Properties); run it with nothing narrowed | fileReadOnly true; fileState *marked read-only*; the list as IG2. Unmark it after |
| **IG9** | *Nothing changed.* After IG8 | Manage > Shared Parameters shows the path IG0 noted; the file's modified time is IG0's; Revit's undo list holds nothing from these rows |
| **IG10** | *The 2022-and-later branch.* IG1, IG2 and IG5 again on **Revit 2024** (or any of 2022 to 2027), the same files, the family upgraded on a copy | The same counts; each data type read through `GetDataType()` - *Length [autodesk.spec.aec:length-2.0.0]*, *Text [autodesk.spec:spec.string-2.0.0]* or what that Revit says - never an empty bracket. Add a **Family Type** parameter to the file first (Mechanical, any category) and look for *Family Type: <category>* |
| **IG11** | *The method.* In a chat with `Heron SPF Test` open: *"add the shared parameter Duct_Accessory_Height from our file to this family"* | `heron_method` family-creation step 2 runs `REPORT_SHARED_PARAMETER_FILE` FIRST and the chat says the family already carries that name with another GUID - before any write is offered |
| **IG12** | *Routing on the owner's PC* - `model+rerank`, the backend there. The 16 sentences in row 5b-1289, main against the branch, with `tools/score-routing.py` | The four task phrasings reach this card; no question reaches a write that did not before; *"what data is hidden in this file"* - REPORT_ADDIN_DATA's - say which card answers it |
