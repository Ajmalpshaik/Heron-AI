# Needs checking — Group EQ

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group EQ - "2020 → latest" backed by evidence: the add-in loaded on every release, and a core set proved on each (2026-10-17)

Heron claims every release from 2020 to 2027. The evidence is much narrower, and it is derived, not typed:

```bash
# which release each PROVEN fragment's proof names (the release is prose inside proof.model - heron_matrix.py says why)
for f in $(grep -l '^heron-status: PROVEN' brain/fragments/*/fragment.yaml); do
  awk '/^proof:/{p=1;next} p&&/^[^ #]/{p=0} p' "$f" | grep -o 'Revit 20[0-9][0-9]' | sort -u | tr '\n' ' '; echo
done | sort | uniq -c
# PROVEN fragments with no second route
grep -l '^heron-status: PROVEN' brain/fragments/*/fragment.yaml | xargs grep -l 'second_route: NOT ESTABLISHED' | wc -l
```

Run on 2026-10-17 this named **2024 and 2020 only, one proof on 2022, and none on 2021, 2023, 2025, 2026 or
2027**. The add-in has been **loaded** in a real Revit on 2020, 2024 and 2027 only
([row A12](../needs-checking-archive/group-a.md), build `48ae1dc`, 2026-09-19).

**Prepared in a cloud container on 2026-10-17, with no Revit. Nothing in this group has been run.**

**Run on the owner's laptop the same afternoon**, by a local session, with the add-in built from `76006767`
(main at the time) for every release installed there - 2020, 2024, 2026 and 2027, read from
`C:\Program Files\Autodesk\Revit *` with a `RevitAPI.dll` in each. **EQ6 passed on all three releases. EQ5 loaded
and then failed at the first tool - [row 5b-575](../fragment-issues/section-5b-rows-401-425.md).** EQ1 to EQ4
were not run: those releases are not installed on any PC this session could reach. EQ7 was still waiting on Q-66 when this ran; [D-131](../decisions/D-131.md) answered it later the same day - see its row. EQ8's
results are in its row. The two commands above, run again at `76006767`: still no PROVEN fragment's proof names
2021, 2023, 2025, 2026 or 2027 - a load record is not a proof, and this afternoon added none. **Whoever runs a row next: read `%LOCALAPPDATA%\Heron\logs\addin-<date>.log` before
pressing the Heron button** - it is a toggle, and on 2024 and 2027 a press at the PC and this session's press
landed together and cancelled each other; the log names every press.

### Two things stopped the proving half as first written - both are answered

1. **Heron had no place to put a proof on a second release.** `batch-prove` refused a PROVEN fragment as
   `ALREADY`, `accept` refused one whose proof still matched its code, and a fragment's `proof:` holds one
   record. [Q-66](../open-questions/tier-2.md) was answered on 2026-10-17 and built the same day as
   [D-131](../decisions/D-131.md): each further release is one entry of `proof.releases`, run by
   `batch-prove --release` and signed by `accept --release`, and a second route on a PROVEN fragment is
   written by `accept --second-route`. **Built in a cloud container and tested there
   (`tests/test_release_proofs.py`); no run of it has been made in Revit.**
2. **`plan` printed the first LETTER of some second routes** - "A" for `select-by-parameter-value`, "R" for
   `set-sheet-title-block` - for every case file that writes the route as one sentence. Fixed the same day,
   [row 5b-511](../fragment-issues/section-5b-rows-401-425.md). EQ8 reads that output.

### The core set - 30 PROVEN fragments, for EQ7

Chosen by how often a fragment is used (how often its name appears in `tools/jobs/*.yaml`, as a fragment
or as a setup step) and by which release-boundary API it touches. The risk column comes from greps over
each fragment's `impl/`: units `UnitUtils|ForgeTypeId|UnitTypeId|DisplayUnitType` (2021), specs
`SpecTypeId|ParameterType|GetDataType|GroupTypeId|BuiltInParameterGroup` (2022), ElementId
`IntegerValue|new ElementId(` (2024), dimension subclasses `LinearDimension|AngularDimension|SpotDimension|NewDimension` (2025),
release branches `#if REVIT20xx`, and reflection `GetMethod(|GetProperty(` (a member looked up by name, so
a rename shows only at run time). **No PUBLISH or ADMIN fragment is in it** - `batch-prove` never sends one.
`set-selection` and `describe-elements` are left out because their proofs are by tracking
([D-53](../DECISIONS.md)); `set-selection` is still run on every release, as the setup step of most jobs.

| Area | Fragment | Risk | Why it is in the set |
|---|---|---|---|
| read | `filter-elements-by-category` | READ | the most-used read |
| read | `select-by-parameter-value` | READ | much used; proved on 2020 only |
| read | `read-element-parameters` | READ | much used; the setup step of many jobs |
| read | `group-and-count` | READ | much used; ElementId |
| modify | `rename-elements` | MODIFY | much used; ElementId |
| modify | `change-element-type` | MODIFY | much used |
| modify | `set-parameter-values-by-id` | MODIFY | specs (2022), reflection |
| modify | `create-wall` | MODIFY | much used |
| modify | `create-floor` | MODIFY | specs (2022), reflection |
| views | `find-views` | READ | much used |
| views | `hide-elements` | MODIFY | much used |
| views | `isolate-elements` | EXECUTE | much used |
| views | `set-view-crop` | MODIFY | much used |
| views | `create-view-filter` | MODIFY | specs (2022), reflection |
| views | `apply-view-filter` | MODIFY | `#if REVIT2020`, ElementId, reflection |
| views | `create-section-view` | MODIFY | units (2021) |
| sheets | `list-sheets` | READ | much used |
| sheets | `place-views-on-sheet` | MODIFY | much used |
| sheets | `set-sheet-title-block` | MODIFY | much used |
| schedules | `create-schedule` | MODIFY | the schedule write |
| schedules | `read-schedule-contents` | READ | the schedule read |
| schedules | `report-schedule-definition` | READ | ElementId |
| MEP | `measure-run-quantities` | READ | much used |
| MEP | `measure-mep-velocity` | READ | units (2021) |
| MEP | `check-fixture-connectivity` | READ | much used |
| annotation | `create-dimension` | MODIFY | dimension subclasses (2025) |
| annotation | `tag-elements-in-view` | MODIFY | `#if REVIT2020` and `REVIT2021`, ElementId |
| families | `add-family-parameters` | MODIFY | units, specs, reflection |
| families | `set-family-type-values` | MODIFY | units, specs, ElementId, reflection |
| families | `label-family-dimension` | MODIFY | units, specs, dimension subclasses |

### The rows

**A SCRATCH COPY, never a working model.** One named test model per release - the same project saved down or
upgraded into each, so every release answers the same question - with a family open in the Family Editor for
the three family rows. Write the model's name, its element count and the session number into every result.

| # | Run | Look for |
|---|---|---|
| **EQ1** | **NOT RUN 2026-10-17 - Revit 2021 is not installed on the owner's laptop**, the one PC with Revit the session could reach. Needs a PC that has it. **Revit 2021.** Install Heron for it with `HeronInstaller.exe` ([D-96](../DECISIONS.md)), or `tools/deploy-addin.ps1 -RevitVersion 2021` for a development build. Start Revit, open the test copy, connect the bridge, and in a chat ask `revit_health` | **Loaded:** the journal has `API_SUCCESS { Starting External Application: Heron AI` and no *"cannot run the external application"*. **Ribbon:** the **Heron** tab - write down every panel and button you see. **Bridge:** it connects, and the add-in log says `Heron loaded. Revit 2021`. **`revit_health`** answers and names 2021. A FAIL at any step is the finding: it proves the 2021 claim wrong today |
| **EQ2** | **NOT RUN 2026-10-17 - Revit 2022 is not installed on the owner's laptop.** EQ1 on **Revit 2022** | The same, naming 2022 |
| **EQ3** | **NOT RUN 2026-10-17 - Revit 2023 is not installed on the owner's laptop.** EQ1 on **Revit 2023** | The same, naming 2023 |
| **EQ4** | **NOT RUN 2026-10-17 - Revit 2025 is not installed on the owner's laptop.** When it is run, also run one PROVEN fragment (`list-sheets` needs no input): 2025 is a .NET release, so [row 5b-575](../fragment-issues/section-5b-rows-401-425.md) can stop every fragment there while the load itself passes. EQ1 on **Revit 2025** - the first .NET 8 release | The same, naming 2025. The deployed assembly reads `.NETCoreApp,Version=v8.0` |
| **EQ5** | **RUN 2026-10-17 - THE ADD-IN LOADS, THEN NO FRAGMENT CAN RUN: FAIL, [row 5b-575](../fragment-issues/section-5b-rows-401-425.md).** Revit 2026.5, PID 30812, the first time Revit 2026 was started on the laptop; scratch copy "EQ-2026 Snowdon Towers Sample HVAC" (9,800 elements, copied from `Revit 2026\Samples`). **Loaded:** journal.0001 line 1467 `API_SUCCESS { Starting External Application: Heron AI, ... Assembly Version: 0.1.0.0 }`, and no *"cannot run the external application"*; Revit asked its unsigned add-in question twice at start-up (`TaskDialog_Security_Unsigned_File_Loading`, lines 252 and 262) and somebody at the PC answered it - the journal records neither the add-in nor the button. **Ribbon:** the Heron tab, one panel **AI Bridge**: **Heron** (a split button - **Bridge Status** in its list), **Changes ON**, **Admin ON**, **Publish ON** (the owner's three switches were all on), **Companion** (a split button - **Turn Companion off** in its list); read through UI Automation, and the journal's seven `Added pushbutton` lines 1460-1466 name the same seven. **Bridge:** `Heron loaded. Revit 2026, add-in 0.1.0.0, pid 30812.`, then `Bridge listening on heron.2026.30812` and `Connected from the ribbon` at 13:54:10. **`revit_health`:** *"Revit 2026, session 30812 - (free)"*; `ping`: add-in 0.1.0.0, protocol 2. **Then:** the start-up warm-up failed (*"Method 'CommonGetSemanticModel' ... does not have an implementation"*), and `list-sheets` (PROVEN) and `list-levels` came back `operation_failed` with the same sentence - another add-in's newer C# compiler had loaded first. **Run it again once 5b-575 is repaired, with AJ Tools still installed.** Seen as well: Revit 2026.5 runs on .NET 10 on this PC (every `*.runtimeconfig.json` in its folder says `net10.0`) - Heron's `net8.0-windows` build loaded on it. EQ1 on **Revit 2026** - manifest isolation | The same, naming 2026 |
| ~~**EQ6**~~ | **RUN, PASS 2026-10-17 ON ALL THREE, with today's build** - `76006767`, Debug, deployed at 13:39 by `tools/deploy-addin.ps1`, the three Heron DLLs' hashes equal to the build's, and each DLL's version text `0.1.0+76006767...`. On every release the ribbon was the one EQ5 lists - the Heron tab, panel **AI Bridge**: **Heron** (with **Bridge Status**), **Changes ON**, **Admin ON**, **Publish ON**, **Companion** (with **Turn Companion off**) - read through UI Automation and matched by seven `Added pushbutton` lines in each journal. **2024:** Revit 2024.3, PID 8168, "EQ-2024 Snowdon Towers Sample HVAC" (9,694 elements, from `Revit 2024\Samples`); journal.0008 line 1298 `API_SUCCESS { Starting External Application: Heron AI`, no *"cannot run"*, no unsigned question; `Heron loaded. Revit 2024, add-in 0.1.0.0, pid 8168.`, warm-up compiled in 5368 ms; bridge `heron.2024.8168` connected 13:49:05; `revit_health` *"Revit 2024, session 8168 - (free)"*; `ping` add-in 0.1.0.0, protocol 2. Left running for another chat's proofs. **2027:** Revit 2027.3, PID 6096, "EQ-2027 Snowdon Towers Sample HVAC" (9,813 elements, from `Revit 2027\Samples`); journal.0002 line 2393 `API_SUCCESS`, no *"cannot run"*, the unsigned question twice (lines 263 and 276), answered at the PC; `Heron loaded. Revit 2027, add-in 0.1.0.0, pid 6096.`, warm-up 2708 ms; bridge `heron.2027.6096` connected 13:58:26; `revit_health` *"Revit 2027, session 6096 - (free)"*; `list-sheets` (PROVEN) answered the model's 17 sheets. **Caveat:** that compile used AJ Tools' C# compiler, not Heron's - [row 5b-575](../fragment-issues/section-5b-rows-401-425.md). Closed by asking Revit to close, 13:58:53. **2020:** Revit 2020.2, PID 51568, "EQ-2020 rme_advanced_sample_project" (from `Revit 2020\Samples`; took six minutes to open, with 1.2 GB of 15.4 GB free); journal.0036 line 899 `API_SUCCESS { Starting External Application: Heron AI` (2020's journal leaves out `Assembly Version:`, [docs/07 §10.9](../07-installation-and-update.md) item 4), no *"cannot run"*, the unsigned question once (line 171), answered at the PC; `Heron loaded. Revit 2020, add-in 0.1.0.0, pid 51568.`, warm-up 4941 ms; bridge `heron.2020.51568` connected from the ribbon at 14:07:47, pressed at the PC; `revit_health` *"Revit 2020, session 51568 - (free)"*; `ping` add-in 0.1.0.0, protocol 2. **Element count not taken on 2020:** the owner started working in that same Revit (a new "Project1", then a family) before the bridge was up, so the session ran nothing there and left it open. EQ1 again on **2020, 2024 and 2027** with today's build | The same on all three. A12 was build `48ae1dc` and the add-in has changed since; a pass then is not a pass now |
| **EQ7** | **NOT RUN - left on the owner's word for a separate fragment-testing session.** When this session checked main at `76006767` on 2026-10-17 there was no `--release`; [D-131](../decisions/D-131.md) built it later the same day (#582), so nothing blocks it now. Two things moved under the core set that day: `read-element-parameters` went to DRAFT at version 3 ([row 5b-520](../fragment-issues/section-5b-rows-401-425.md)) - read its status before putting it in a job - and on Revit 2026 no fragment runs until [row 5b-575](../fragment-issues/section-5b-rows-401-425.md) is repaired, so 2026 is not a release to run it on yet. The core set above on every release that passed EQ1 to EQ6 ([D-131](../decisions/D-131.md)). One job file for the set, in `tools/jobs/`, each job with its `negative-set:` or `negative-in:` arranged on THAT release's model; then, per release, in that Revit: `python tools/batch-prove.py tools/jobs/<file>.yaml --release <year> --dry-run`, then the same without `--dry-run` and with `--session <pid>`, then `python brain/heron_validate.py review <fragment>@<year>` and `accept <fragment> --release <year> --by "Your Name"` for each PASS. `python brain/heron_matrix.py --fragments` then shows `P` on that release | **Per fragment, per release:** PASS / FAIL / NOT RUN (say why) / NEEDS REAL REVIT, never merged. The dry run names the fragments already proved on that release as `ALREADY` - they need nothing. **`WRONG RELEASE` means the batch ran on another Revit**: nothing it found counts, pin `--session` and run it again. **A FAIL gets a 5b row the same day**, with what was passed and what came back - the proof is never edited to pass |
| **EQ8** | **NOT RUN 2026-10-17 - prepared, and left on the owner's word for a separate fragment-testing session**; the arrangement for each of the twelve is in the table below this one. Why none was run that afternoon: (1) every route is a person's - a schedule made in Revit, the Project Browser, the Properties palette, a section - and the only free Revit that can run fragments (2027; 2026 cannot, [row 5b-575](../fragment-issues/section-5b-rows-401-425.md)) would have taken over the screen the owner was working at in Revit 2020 all afternoon; (2) six of the twelve are write-path routes whose change must be KEPT to be looked at, and since D-126 a kept change waits on the modeller's own Keep or Discard box, which a session does not press; (3) `read-element-parameters` is DRAFT again ([row 5b-520](../fragment-issues/section-5b-rows-401-425.md)), so its route belongs in its new proof, not here. `plan` printed every route whole (row 5b-511's fix holds). **Second routes**, for the most-used PROVEN fragments that have none: `filter-elements-by-category`, `select-by-parameter-value`, `read-element-parameters`, `group-and-count`, `rename-elements`, `create-wall`, `find-views`, `hide-elements`, `set-view-crop`, `set-sheet-title-block`, `place-views-on-sheet`, `measure-run-quantities`. `python brain/heron_validate.py plan` prints each one's declared route - mostly a person's, in Revit: a schedule count, the Project Browser, the Properties palette | The route's answer **equals** the fragment's on the same model and selection, written into `proof.second_route` with the date, the model and both numbers - **by `accept --second-route`, never by hand** ([D-131](../decisions/D-131.md)): `validate --cross count_elements --out brain/proof-drafts/runs/<fragment>@second-route.json ...`, then `heron_validate.py draft <fragment> --second-route --from` that record, then `accept <fragment> --second-route --by "Your Name"`. **A person's route has no recorded run**, so it cannot be written there and `second_route` stays NOT ESTABLISHED: record what Revit showed, both numbers, on this row instead. A route whose answer differs is a finding, not a pass |

### EQ8 - the twelve routes, arranged for one sitting (2026-10-17)

**One scratch model for all twelve:** a copy of the release's own `Samples\Snowdon Towers Sample HVAC.rvt` -
the model several of these proofs were run on (`Snowdon-scratch`). The session left one at
`%TEMP%\heron-work\eq-loads\r2024\` (9,694 elements) and one at `...\r2027\` (9,813). **Not on Revit 2026**
until [row 5b-575](../fragment-issues/section-5b-rows-401-425.md) is repaired. For each: run the fragment, then the
route, on the same model and the same selection, and write both numbers here with the date. **A route whose
number differs is a finding, not a pass.** A write runs through `revit_change` and is kept only when the person
at Revit presses **Keep** (D-126); the route is looked at after that, and one Ctrl+Z takes the change back.

| Fragment | Risk | The fragment's run | The route, by hand in Revit |
|---|---|---|---|
| `filter-elements-by-category` | READ | `category=Ducts`, no level - the whole model | View > Schedules > Schedule/Quantities > Ducts, the **Count** field, Grand totals on: Revit's count equals `elements`. For a level case, a plan of that level filtered by eye |
| `select-by-parameter-value` | READ | `categories=Ducts`, `parameterName=System Type`, `matchMode=contains`, `matchText=` a word the model's system types hold, such as `Supply` | A Ducts schedule with System Type as a field and a **Contains** filter on the same text, counted |
| `read-element-parameters` | READ | - | **DRAFT since [row 5b-520](../fragment-issues/section-5b-rows-401-425.md)**: its route goes with its new proof, not here |
| `group-and-count` | READ | `select-by-category-name` (Ducts in one small plan - the proofs used 22 from `FloorPlan: M1`) -> `set-selection` -> `read-element-parameters parameterName=System Type`, then `group-and-count` with `--keep-chain` | The same ducts in a schedule **grouped by System Type**, itemize off, with Count: each group's count equals its `groups` entry |
| `measure-run-quantities` | READ | the same small set of ducts selected, `measure=length`, then `measure=area` | A Ducts schedule filtered to the same ducts with **Length** and **Area**, totals on: both totals equal the fragment's |
| `find-views` | READ | `viewType=FloorPlan`, `nameContains=L` | The Project Browser, by eye: floor plans whose name holds "L", templates not counted, equal `elements` |
| `rename-elements` | MODIFY | `find-views` for the set, then `find` / `replaceWith` on a few view names | A View List schedule showing Name, before and after the kept rename |
| `create-wall` | MODIFY | two points on a level, a wall type the model has | Architecture > Wall between the same two points by hand, and the two compared in a section |
| `hide-elements` | MODIFY | a view and a selection, `permanent=true` and `false` | Close and reopen the model - the temporary hide is gone, the permanent one is not - and the view's Visibility/Graphics read by hand |
| `set-view-crop` | MODIFY | a view on a sheet, `marginMm` | Look at the sheet, not the view: the crop is what the drawing shows |
| `set-sheet-title-block` | MODIFY | `symbol` = another title block type the model has | Select the title block on one sheet and change its Type in Properties (the same-family swap); on another, delete it and drag one in (the replace). Both counts compared |
| `place-views-on-sheet` | MODIFY | `sheetNumber` of an empty sheet, after `find-unplaced-views` | Open the sheet and count the viewports by eye - equal to `placed`, each in a different place |
