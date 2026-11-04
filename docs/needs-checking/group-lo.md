# Needs checking — Group LO

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group LO - MEP fabrication: the configuration and loaded services read, design ducts and pipes converted to fabrication parts of a named service, and Optimize Lengths: `REPORT_FABRICATION_SETUP`, `CONVERT_TO_FABRICATION`, `OPTIMIZE_FABRICATION_LENGTHS` (2026-11-02)

Asked for in the modeller's words: *"which fabrication configuration and services are loaded"*, *"list the
fabrication services and their buttons"*, *"convert these ducts to fabrication parts using the Low Pressure
service"*, *"convert this pipe run to fabrication using service Copper Plumbing"* and *"optimise the lengths of
these fabrication parts"* - Revit's Systems > Fabrication > MEP Fabrication Settings, Design to Fabrication and
Modify | Fabrication Parts > Optimize Lengths. No card touched fabrication: a grep of every fragment that day for
`FabricationPart`, `FabricationConfiguration` and `DesignToFabricationConverter` found nothing, and
`brain/heron_retrieve.py` sent the five sentences to `REPORT_FAMILY_CONNECTORS`, `EDIT_ELECTRICAL_CIRCUIT`,
`CONVERT_MEP_PLACEHOLDERS`, `CHECK_FIXTURE_CONNECTIVITY` and `DIVIDE_PARTS`. heron-duplicate-checker's method found
look-alikes only - `CONVERT_MEP_PLACEHOLDERS` turns a placeholder into a design duct, `DIVIDE_PARTS` cuts Revit's
Parts, `SPLIT_MEP_RUN` cuts a design duct at a distance - so three new cards: one read, two writes.

| What | Where | What it does |
|---|---|---|
| `REPORT_FABRICATION_SETUP` (READ) | [`brain/fragments/report-fabrication-setup`](../../brain/fragments/report-fabrication-setup/fragment.yaml) | The **fabrication configuration** - name, version, where it is kept, units, profile - and the services **loaded** out of all it has, each with its abbreviation, whether parts use it, its palettes (groups to 2021) and button counts; every **button** by name when `show` asks for buttons or `service` names one. The **fabrication parts selected** - or every one in the model with `show parts` - with service, item number, size and straight length, totalled per service; anything selected that is not a fabrication part named in `notFabrication`. **No configuration** said in Revit words (`configurationName`, `loadedServices`, `buttons`, `fabricationParts`, `report`, `names`, `notFabrication`) |
| `CONVERT_TO_FABRICATION` (MODIFY) | [`brain/fragments/convert-to-fabrication`](../../brain/fragments/convert-to-fabrication/fragment.yaml) | Revit's **Design to Fabrication** on the design ducts, pipes, fittings and accessories handed in, to the loaded service **named by its whole name or abbreviation - never guessed**: not named, not matched or matched twice converts nothing and **lists the loaded services**; one in the configuration but not loaded is said so. **No configuration** refused in Revit words. A placeholder, flex duct or pipe, a part already fabrication or anything else is named in `notDesign` and left. Ductwork and pipework go to Revit **as two calls**, so a ductwork service refusing pipes leaves the duct side converted. Revit's result in words - success, or each of its five partial failures - every new part with its service, size and length, and **how many design elements Revit replaced and which are still there** (`fabricationParts`, `notDesign`, `notConverted`) |
| `OPTIMIZE_FABRICATION_LENGTHS` (MODIFY) | [`brain/fragments/optimize-fabrication-lengths`](../../brain/fragments/optimize-fabrication-lengths/fragment.yaml) | Revit's **Optimize Lengths** on the fabrication parts handed in. Not a fabrication part - a design duct is pointed at Design to Fabrication - named and left; a selection with **no straight refused** before Revit is asked. Each new or changed part with its length, how many straights Revit replaced, and the run's **total straight length before and after** - a difference over 1 mm a finding (`optimised`, `notFabrication`) |

**What is done and what is not.** All three compile for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-02, a Linux container). Read off the shipped assemblies with
`tools/api-surface --members`: `FabricationConfiguration` `GetFabricationConfiguration` (its parameter renamed in
2024, called by position), `GetFabricationConfigurationInfo`, `GetAllLoadedServices`, `GetAllServices`,
`GetAllUsedServices`; `FabricationService` and `FabricationServiceButton`, whose button sets are **groups to 2021
and palettes from 2022** (`#if REVIT2020 || REVIT2021`); `DesignToFabricationConverter` and its result and
partial-failure enums; `FabricationPart.OptimizeLengths` (parameter renamed in 2024) - the same 2020 to 2027 otherwise.
The converter lives in a namespace the executor does not import (`HeronFragmentImports`), so `CONVERT_TO_FABRICATION`
finds it beside `FabricationPart` and calls its four members **by reflection**; `check-api-surface` cannot see a
member named in a string, so those four were checked by hand. **Nothing here has run in Revit.** All three cards are
DRAFT. **Only Revit can settle** what Design to Fabrication does to the design elements it converts - RevitAPI.xml
does not say whether they are deleted or kept - so the report reads it back and LO3 records it; whether the sample
configuration's services take the arrangement's sizes; and the lengths Optimize Lengths chooses.

**Routing.** Measured through `heron_retrieve.find` on fresh scratch stores of the tree at `c584a6e` and of this change (`model` backend, no re-ranker; Revit 2024 and no Revit filter; with and without the host's question flag), over every declared sentence and routing-table claim of every live card. The five sentences reach these cards by identity - the two questions `REPORT_FABRICATION_SETUP` with and without the flag, the convert and optimise sentences their writes without it, and with it a read - `REPORT_FABRICATION_SETUP`, or for the pipe sentence `CHECK_FIXTURE_CONNECTIVITY`, as before. Three sentences moved off the card their routing tables give them and are declared on that card, outside `impl/`, fingerprints unchanged - *"change the project units to imperial"* and *"set cooling load to Btu/h"* on `SET_PROJECT_UNITS`, *"why is this setting greyed out"* (pulled from a read to a write) on `REPORT_VIEW_TEMPLATE_CONTROL` - [row 5b-1394](../fragment-issues/section-5b-rows-401-425.md), FIXED. With the flag, twenty-four sentences claimed by writes moved from one read to another, eight of them to `REPORT_FABRICATION_SETUP` on the word *parts* - recorded, not tuned. *"select all the ducts"* reaches a write without the flag before and after - [row 5b-1395](../fragment-issues/section-5b-rows-401-425.md), OPEN. `CREATE_DUCT_RUNS`, `CONVERT_MEP_PLACEHOLDERS` and `CREATE_ASSEMBLY` keep every sentence they claim. `tools/check-routing.py` (its own scratch store, before and after): no question answered by a write; claimed and not reached 67 and 67.

**The model - `fabrication-lo`**, a scratch project from Revit's metric Mechanical template, saved under
`%TEMP%\heron-work`, never a working model. Every write here is ROLLED BACK unless the modeller keeps it. The job
file is
[`tools/jobs/fabrication-setup-and-convert-lo-2026-11-02.yaml`](../../tools/jobs/fabrication-setup-and-convert-lo-2026-11-02.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **LO1** | *Arrange.* `fabrication-lo`: Systems > Fabrication > MEP Fabrication Settings, one of Autodesk's sample configurations, **one ductwork and one pipework service** loaded - write down the configuration's name and the two services' exact names. In "Level 1": a rectangular duct run of two straights and an elbow, a copper pipe run of two straights and an elbow, one duct placeholder, one wall. Save. A second scratch project with **no configuration**, holding one duct | The configuration and the two services in MEP Fabrication Settings |
| **LO2** | *The read - D-30 positive and negative, THE NEGATIVE FIRST.* The job file's first job | Negative, service "ZZNOTASERVICE": `loadedServices` 0, findings name both loaded services. Positive: `configurationName` as MEP Fabrication Settings shows it, `loadedServices` 2, `names` the two services - **write the ductwork service's name into the job file's second job**. Then `show buttons` with the ductwork service: the palettes and buttons the MEP Fabrication Parts palette shows. On the second project: *"This model has no fabrication configuration ..."* |
| **LO3** | *The convert - D-30 positive and negative, THE NEGATIVE FIRST.* The job file's second job, `--write`, rolled back | Negative, service "Low": nothing converted, the loaded services listed. Positive: `fabricationParts` each a fabrication part on the ductwork service; **RECORD how many design ducts the report says are gone and which are still there** - what Revit does to the originals |
| **LO4** | *Never guessed, refused by name.* The duct run with its elbow, the pipe run with its elbow, the placeholder and the wall selected together, service the ductwork service; the same with service blank; the second project's duct with any service | The duct run and elbow converted; the pipe side in `notConverted` with Revit's words; the placeholder *Convert Placeholder makes it a real duct first* and the wall *not a design duct ...*, both unchanged; the blank service lists the services; the second project refused in Revit words - nothing changed there |
| **LO5** | *Optimize Lengths - D-30 positive and negative, THE NEGATIVE FIRST.* Arrange: on a copy of `fabrication-lo` saved as `fabrication-lo-run`, three ductwork straights of about 2000 mm end to end from the MEP Fabrication Parts palette. The job file's third job, `--write`, rolled back | Negative, the design ducts: nothing re-cut, each named. Positive: the straights' total the same before and after, no finding that it differs; RECORD the straights Revit made and their lengths. One fabrication elbow alone: *None of the 1 fabrication part(s) given is a straight* |
| **LO6** | *The second route.* Revit's own Design to Fabrication and Optimize Lengths by hand on copies of the same saved files, the same elements and service | The same number of parts, the same design elements gone or left, the same straight lengths as the reports gave; Undo takes each back as one entry |
