# Needs checking — Group MM

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group MM - Manage > Mechanical Settings, Analyze > Energy Settings and Create Energy Model, and Insert > Import gbXML - read and change the duct and pipe settings and the energy settings for the whole model, build the energy analytical model, and import a gbXML file: `REPORT_MECHANICAL_SETTINGS`, `SET_MECHANICAL_SETTINGS`, `REPORT_ENERGY_SETTINGS`, `SET_ENERGY_SETTINGS`, `CREATE_ENERGY_MODEL`, `IMPORT_GBXML` (2026-11-02)

Asked for as three gaps on Revit's ribbon: *"Mechanical Settings: duct (and pipe) settings - read and set
DuctSettings values: connector separator, flat-on-top/bottom annotations, duct fitting annotation size, air
density/viscosity, rise/drop symbols"*, *"Energy settings (Analyze > Energy Settings) - read and set
EnergyDataSettings (building type, ground plane, project phase, analytical mode, export category) and, if the
API allows from a fragment, create the energy analytical model"* and *"gbXML import (Insert > Import gbXML)"*.
Nothing in Heron read or changed either dialog, built an energy model or imported gbXML: `brain/heron_retrieve.py`
on a scratch store of this tree (Revit 2024) sent the questions to `REPORT_STRUCTURAL_SETTINGS`,
`REPORT_ELECTRICAL_SETTINGS`, `REPORT_CONNECTORS` and `REPORT_DUCT_CONDUIT_SIZES` (reads of other things) and the
changes to `ADD_FAMILY_CONNECTOR`, `EDIT_MEP_SIZES`, `SET_SUN_SETTINGS`, `CREATE_ANALYTICAL_ELEMENTS`,
`IMPORT_IMAGE` and `LOAD_FAMILY`. `RUN_HEATING_COOLING_LOADS` (Revit 2027) reads four Energy Settings into its
report and sets none; it was not edited.

| What | Where | What it does |
|---|---|---|
| `REPORT_MECHANICAL_SETTINGS` (READ) | [`brain/fragments/report-mechanical-settings`](../../brain/fragments/report-mechanical-settings/fragment.yaml) | Reads Duct Settings and Pipe Settings in the names the write takes: annotation scale for single line fittings, fitting annotation size, air density and air dynamic viscosity, the rectangular / round / oval duct size prefix, separator and suffix, pipe size prefix and suffix, connector separator, flat on top / bottom, set up / down from center and bottom, centerline, rise/drop annotation size. `show` duct, pipe or all |
| `SET_MECHANICAL_SETTINGS` (ADMIN) | [`brain/fragments/set-mechanical-settings`](../../brain/fragments/set-mechanical-settings/fragment.yaml) | A table of `name=value` - text as written (`""` for empty), sizes in millimetres, density kg/m3, viscosity Pa-s. A name both pages have, given without "duct" or "pipe", is taken for `system` (duct, pipe or both) and refused naming both when there is none. The **rise/drop symbols** are refused by name - no Duct or Pipe Settings member on any release, Revit holds them on each system type |
| `REPORT_ENERGY_SETTINGS` (READ) | [`brain/fragments/report-energy-settings`](../../brain/fragments/report-energy-settings/fragment.yaml) | Reads Energy Settings in the names the write takes - building type, ground plane, project phase, analysis mode, export category and complexity, thermal properties, building envelope, construction class, service type, report type, use current view only (2024+), sliver space tolerance, grid cell size, core offset, perimeter zones, target glazing, energy model, heating credits - and whether there is an energy analytical model, with its counts |
| `SET_ENERGY_SETTINGS` (ADMIN) | [`brain/fragments/set-energy-settings`](../../brain/fragments/set-energy-settings/fragment.yaml) | A table of `name=value`. A choice is one of Revit's own on the release that runs (read with `Enum.GetNames`) and is put to Revit's own `Check...` method before anything is written; a level or phase by name; lengths in millimetres. The location is refused and sent to `SET_PROJECT_LOCATION` |
| `CREATE_ENERGY_MODEL` (MODIFY) | [`brain/fragments/create-energy-model`](../../brain/fragments/create-energy-model/fragment.yaml) | `EnergyAnalysisDetailModel.Create` from the Energy Settings as they are - `Create(Document)` on 2027, `EnergyModelType.AnalysisMode` on 2022-2026, building elements only on 2020-2021 (any other mode refused by name). Counts the analytical spaces, surfaces and openings; names a model already there and what became of it |
| `IMPORT_GBXML` (MODIFY) | [`brain/fragments/import-gbxml`](../../brain/fragments/import-gbxml/fragment.yaml) | `Document.Import(path, new GBXMLImportOptions())` inside the caller's transaction; every element new afterwards handed back and counted by category. A family, no file, a missing file or not .xml / .gbxml refused first |

Both settings writes: everything parsed before anything is written, so a bad entry changes nothing; a setting
**Revit** refuses is answered with Revit's own words, every setting already written in that call put back,
`refused` says NOTHING WAS CHANGED; a put-back that fails throws and the caller's transaction rolls back. `before`
and `after` read every setting back; one that reads back different throws.

**Why ADMIN.** Graded as `SET_STRUCTURAL_SETTINGS` and `SET_PROJECT_UNITS` are ([D-106](../DECISIONS.md)): both
dialogs are how the project is set up for everybody - every duct and pipe tag's separator and suffix, every duct
pressure loss Revit works out, and every energy model, gbXML export and loads run. So neither write can be sent
by `batch-prove` or `try-once`; each is proved by name with the owner's Admin switch on.
`CREATE_ENERGY_MODEL` and `IMPORT_GBXML` make elements and are MODIFY.

**What is done and what is not.** All six compile for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-02, a Linux container). Every Revit member was read off the shipped
assemblies of all eight with `tools/api-surface --members`; the per-release members are named in each card's
`compatibility-note`. **Nothing here has run in Revit.** All six are DRAFT. **Only Revit can settle**, and
MM3-MM7 record: whether the fitting and rise/drop annotation sizes and the energy lengths are held in feet, as
converted ([row 5b-1449](../fragment-issues/section-5b-rows-401-425.md)); whether Revit holds the glazing
percentage as 40 or 0.4, and what turning the energy model switch does by itself
([row 5b-1450](../fragment-issues/section-5b-rows-401-425.md)); what `EnergyAnalysisDetailModel.Create` does with
a model already there ([row 5b-1451](../fragment-issues/section-5b-rows-401-425.md)); and **whether the gbXML
import runs inside the host's transaction at all** - the assembly carries no "close the transaction" refusal for
it, but that is not a run ([row 5b-1452](../fragment-issues/section-5b-rows-401-425.md)).

**Routing.** Measured through `heron_retrieve.find` on fresh scratch stores of the tree at `0634fb7` and of this change, Revit 2024, with and without the host's question flag (the `READ` ceiling), over every declared utterance and routing-table sentence of the library and 26 probes. Every sentence the six cards declare reaches its card by identity; *"what are the mechanical settings"*, *"what are the energy settings"* and *"which phase is the energy analysis using"* reach the two reads with and without the flag. Two made-up questions reach a settings write without the flag (a read with it), two made-up requests still miss `SET_MECHANICAL_SETTINGS`, and *"put it back"* flipped again between two cards that do not claim it - [row 5b-1453](../fragment-issues/section-5b-rows-401-425.md), OPEN. *"make it mechanical equipment and work plane based"*, claimed by `SET_FAMILY_SETTINGS`' own table, moved on one intermediate store and is now declared there (card text only). A first draft pushed `tests/test_routing_phrases.py`'s *"put the duct tags above the ducts with a leader"* off `OFFSET_TAGS_FROM_HOST`; the two mechanical cards were reworded until it came back, and that suite fails exactly its known seven. `tools/check-routing.py`: no question answered by a write.

**The models - scratch, any one release 2020 to 2027.** Never a working model. Every run below is rolled back
except MM1's arrangement. The job file is
[`tools/jobs/mechanical-energy-gbxml-mm-2026-11-02.yaml`](../../tools/jobs/mechanical-energy-gbxml-mm-2026-11-02.yaml)
(the two reads, `CREATE_ENERGY_MODEL` and `IMPORT_GBXML`; `--dry-run` reads it back and sends nothing). Every line
below with `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **MM0** | *Open.* Any release 2020 to 2027. New projects saved under `%TEMP%\heron-work`: "Heron MM Mechanical" and "Heron MM gbXML" from the metric mechanical template, "Heron MM Energy" from the metric architectural template; any family saved as "Heron MM Family"; Changes and Admin on at the ribbon | The title bar names the scratch project |
| **MM1** | *Arrange, and KEEP it.* In "Heron MM Mechanical" one tagged rectangular duct and one pipe on Level 1. In "Heron MM Energy" four walls, a floor and a roof enclosing one room on Level 1, a Level 2, Energy Settings' analysis mode "building elements". Save each. Export "Heron MM Energy" with File > Export > gbXML to `%TEMP%\heron-work\heron-mm.xml`; make `not-gbxml.xml` from any text file. Write down Mechanical Settings > Duct Settings and Pipe Settings, and Analyze > Energy Settings, by eye | The values recorded |
| **MM2** | *REPORT_MECHANICAL_SETTINGS - D-30 pair.* The job file's first job | Positive (`show duct`): ductSettings as MM1 recorded. Negative (`show pipe`): ductSettings empty, pipeSettings filled |
| **MM3** | *SET_MECHANICAL_SETTINGS, NEGATIVE FIRST.* `validate set-mechanical-settings --in "Heron MM Mechanical" --allow-publish --write --set "settings=duct connector separator=#" --negative-set "settings=connector separator=#"`; then by hand, rolled back, `duct fitting annotation size=5; air density=1.25 kg/m3`, `connector separator=/` with `system=both`, `duct rise drop symbol=Dot`, `duct fitting annotation size=3 in` | Negative: changed 0, *on both Duct Settings and Pipe Settings*, settings as MM1. Positive: changed 1, `after` *duct connector separator "#"*. **RECORD** that 5 mm reads back as 5 mm in the dialog (row 5b-1449) and that 1.25 kg/m3 shows as 1.25 in the project's units; the rise/drop symbol refused by name; inches refused |
| **MM4** | *REPORT_ENERGY_SETTINGS - D-30 pair.* The job file's second job | Positive: energySettings as MM1 recorded, energyModelExists false. **RECORD** the glazing percentage beside the dialog's (row 5b-1450). Negative (the family): settingsRead 0 |
| **MM5** | *SET_ENERGY_SETTINGS, NEGATIVE FIRST.* `validate set-energy-settings --in "Heron MM Energy" --allow-publish --write --set "settings=building type=office" --negative-set "settings=building type=spaceship"` (pick another type if the template already says office); then by hand, rolled back, `ground plane=Level 1; export category=spaces`, `target percentage glazing=40`, `location=London`, `sliver space tolerance=1 ft`, `energy model=<the opposite of MM1's>` | Negative: changed 0, *not one of Revit's choices* with Revit's list. Positive: changed 1, `after` *building type office*, the dialog shows Office. The glazing: 40% in the dialog. **RECORD** whether the energy model switch built or removed an energy model by itself (REPORT_ENERGY_SETTINGS afterwards) |
| **MM6** | *CREATE_ENERGY_MODEL - D-30 pair, NEGATIVE FIRST.* The job file's third job; then by hand in a copy, the positive KEPT once and run again | Negative (the family): refused, nothing made. Positive: an energy model id, analyticalSpaces 1 or more, surfaces 6 or more. The second run: **RECORD** rebuilt in place, replaced, or a second model beside the first (row 5b-1451) |
| **MM7** | *IMPORT_GBXML - D-30 pair, NEGATIVE FIRST.* The job file's fourth job, the two paths written out in full | Negative (`not-gbxml.xml`): Revit refuses, nothing kept. Positive (`heron-mm.xml`): imported lists the new elements, counted by category. **RECORD first whether Revit ran the import inside the host's transaction at all** (row 5b-1452) |
| **MM8** | *The second route.* Mechanical Settings > Duct Settings, Analyze > Energy Settings, Analyze > Create Energy Model and Insert > Import gbXML by hand on copies of the same saved files | The same values, the same counts, the same categories; one Ctrl+Z takes each Heron run back as one entry |
