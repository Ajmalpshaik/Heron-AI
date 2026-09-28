# Needs checking — Group BF

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-28 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group BF - the Admin and Publish switches: refused by name while off, a project parameter bound with Admin on, refused again once it is off (2026-09-28)

**Built 2026-09-28 in a cloud session with no Revit, and none of it has run in Revit.**
[D-106](../DECISIONS.md) put two switches beside Changes on the ribbon, **Admin** and **Publish**, each off
until the owner turns it on, each asking before it goes on and never on the way off, and each working
only while Changes is on as well. The add-in declares one operation per level - `run_fragment_admin`
and `run_fragment_publish`, the same executor as `run_fragment_write` - and `revit_change` sends an
`ADMIN` or `PUBLISH` fragment as the one its own card names. Why: [row 5b-254](../FRAGMENT-ISSUES.md).
**What is measured already, off Revit:** every project compiles on 2020 to 2027 with no warnings;
`tests/Heron.Kernel.TestHost` runs all eight on/off combinations of the three switches against all seven
levels; `tests/test_admin_publish.py` drives the client through a fake Revit and watches an `ADMIN`
fragment leave as `run_fragment_admin`. **Nothing here says what Revit does with any of it.**

**Before any row: with EVERY Revit closed**, build and deploy the add-in for all three releases on this PC,
then **restart the Claude app** so the new Python - the client and the MCP server - is the one running:

```
dotnet build revit\Heron.Revit.Addin\Heron.Revit.Addin.csproj -p:RevitVersion=2020
.\tools\deploy-addin.ps1 -RevitVersion 2020
dotnet build revit\Heron.Revit.Addin\Heron.Revit.Addin.csproj -p:RevitVersion=2024
.\tools\deploy-addin.ps1 -RevitVersion 2024
dotnet build revit\Heron.Revit.Addin\Heron.Revit.Addin.csproj -p:RevitVersion=2027
.\tools\deploy-addin.ps1 -RevitVersion 2027
```

**The shared parameter file for BF3 is the owner's own**, `D:\Ajmal\BIM Resources\NEW\Modeling\06_Shared_Parameters\Shared_Parameters.txt`.
`ADD_PROJECT_PARAMETER` reuses a definition it finds by name in the group it is given, and **creates one
in that file if it does not find it** - so before BF3, open the file in Revit's *Manage > Shared
Parameters* and confirm `ID_Room_Name` and `ID_Room_Number` are both in group `Location_Data`. The
fragment also points this Revit session at that file; *Manage > Shared Parameters* shows it afterwards.

| # | Check | Expected |
|---|---|---|
| **BF1** | **Admin OFF: refused by name.** Revit 2024, *heron ai bulding* open, Changes ON, Admin off. In a chat: *"add the project parameters ID_Room_Name and ID_Room_Number to Mechanical Equipment and Air Terminals"* | The reply says Heron's **Admin** switch is off, **nothing was sent to Revit**, and to turn on Admin in Revit's Heron ribbon (Heron > AI Bridge > Admin). The banner over Revit reads *"Admin is off - nothing was sent"*. *Manage > Project Parameters* is unchanged. The audit line for the run carries `error` `admin_disabled`. A reply that runs anything, or refuses in the old words *"Heron does not run those yet"*, is a FAIL - the second means the Claude app is still running the old Python |
| **BF2** | **The question, on the way on.** Press **Admin** on the ribbon. Press Esc. Press it again and pick *Leave it off*. Press it a third time and pick *Turn Admin on* | A window, not a TaskDialog: amber strip, *"Let Heron change how this project is set up?"*, three green ticks, the amber line about Ctrl+Z and the shared parameter file, the *Turn Admin on* button - and NO default button, so Enter does nothing. Esc and *Leave it off* leave the closed blue padlock and *Admin off*; the add-in log says *"Admin toggle: offered, declined"*. The third press turns it to the open red padlock and *Admin ON*, and `heron.config` now reads `admin.enabled = true` |
| **BF3** | **Admin ON: bind the two parameters, and read them back.** Changes and Admin both ON, *heron ai bulding* in front. In a chat, ADD_PROJECT_PARAMETER once for each: `sharedParameterFile` = the file above, `groupName=Location_Data`, `parameterName=ID_Room_Name` (then `ID_Room_Number`), `categories=Mechanical Equipment, Air Terminals`, `instanceBinding=true`. Then read back in Revit | Each reply ran in *heron ai bulding* with `bound` true and `boundCategories` naming Mechanical Equipment and Air Terminals, and the finding *"The definition already existed in group 'Location_Data' and was reused"*. The banner said *"Changing the project setup: add-project-parameter"*. **Read back:** *Manage > Project Parameters* lists both as **Instance**, with Mechanical Equipment and Air Terminals ticked; selecting one mechanical equipment and one air terminal shows both parameters, blank, in Properties. Revit's undo list has one entry per call. **No `reused` finding means the file gained a new definition - record that as a FAIL** |
| **BF4** | **Admin OFF again: refused.** Press **Admin** (it asks nothing on the way off). Ask the same as BF1 | The padlock is closed and blue, *Admin off*, with no question asked. The request is refused in BF1's words, and nothing changes: both parameters from BF3 are still bound, and no new one appears |
| **BF5** | **Admin ON with Changes OFF: still refused.** Turn **Changes** off and **Admin** on. Ask again | Refused: the reply says *"Admin is on, but Changes is off"* and to turn on Changes; nothing sent. The Admin window, when it asked, carried the extra amber line *"Changes is off right now..."* |
| **BF6** | **Publish, the same three steps.** Changes ON. Publish off: ask for EXPORT_VIEW_IMAGE of one view into a scratch folder. Then turn **Publish** on (its window asks) and ask again. Then turn it off and ask a third time | Off: refused by name - *Publish* - nothing written to the folder. On: the image is written, and the banner said *"Publishing from the model: export-view-image"*. Off again: refused, and no second file appears |
| **BF7** | **The state survives a restart, and draws on every release.** With Admin ON and Publish off, close and reopen Revit 2024; then open Revit 2020 and Revit 2027 | Revit 2024 starts with Admin's open red padlock reading *Admin ON* and Publish's closed blue one reading *Publish off* - read from `heron.config`, not assumed. All three releases show Changes, Admin and Publish in that order under Heron > AI Bridge, each with its picture, and the Admin window draws on 2020 and 2027. Turn Admin off afterwards |
