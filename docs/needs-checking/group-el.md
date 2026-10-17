# Needs checking — Group EL

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group EL - "2020 → latest" backed by evidence: the add-in loaded on every release, and a core set proved on each (2026-10-17)

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

### Two things stop the proving half as written - read before the session

1. **Heron has no place to put a proof on a second release.** `batch-prove` refuses a PROVEN fragment as
   `ALREADY`, with no flag on purpose; `accept` refuses one whose proof still matches its code; and a
   fragment's `proof:` holds one record. So EL7 cannot be run until
   [Q-64](../open-questions/tier-2.md) is answered and what it chooses is built. EL1 to EL6 and EL8 do not wait for it.
2. **`plan` printed the first LETTER of some second routes** - "A" for `select-by-parameter-value`, "R" for
   `set-sheet-title-block` - for every case file that writes the route as one sentence. Fixed the same day,
   [row 5b-491](../fragment-issues/section-5b-rows-401-425.md). EL8 reads that output.

### The core set - 30 PROVEN fragments, for EL7

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
| **EL1** | **Revit 2021.** Install Heron for it with `HeronInstaller.exe` ([D-96](../DECISIONS.md)), or `tools/deploy-addin.ps1 -RevitVersion 2021` for a development build. Start Revit, open the test copy, connect the bridge, and in a chat ask `revit_health` | **Loaded:** the journal has `API_SUCCESS { Starting External Application: Heron AI` and no *"cannot run the external application"*. **Ribbon:** the **Heron** tab - write down every panel and button you see. **Bridge:** it connects, and the add-in log says `Heron loaded. Revit 2021`. **`revit_health`** answers and names 2021. A FAIL at any step is the finding: it proves the 2021 claim wrong today |
| **EL2** | EL1 on **Revit 2022** | The same, naming 2022 |
| **EL3** | EL1 on **Revit 2023** | The same, naming 2023 |
| **EL4** | EL1 on **Revit 2025** - the first .NET 8 release | The same, naming 2025. The deployed assembly reads `.NETCoreApp,Version=v8.0` |
| **EL5** | EL1 on **Revit 2026** - manifest isolation | The same, naming 2026 |
| **EL6** | EL1 again on **2020, 2024 and 2027** with today's build | The same on all three. A12 was build `48ae1dc` and the add-in has changed since; a pass then is not a pass now |
| **EL7** | **Waits on [Q-64](../open-questions/tier-2.md).** Once answered and built: the core set above on every release that passed EL1 to EL6, one job file per release in `tools/jobs/`, `--dry-run` first, each job with its `negative-set:` or `negative-in:` | **Per fragment, per release:** PASS / FAIL / NOT RUN (say why) / NEEDS REAL REVIT, never merged. **A FAIL gets a 5b row the same day**, with what was passed and what came back - the proof is never edited to pass |
| **EL8** | **Second routes**, for the most-used PROVEN fragments that have none: `filter-elements-by-category`, `select-by-parameter-value`, `read-element-parameters`, `group-and-count`, `rename-elements`, `create-wall`, `find-views`, `hide-elements`, `set-view-crop`, `set-sheet-title-block`, `place-views-on-sheet`, `measure-run-quantities`. `python brain/heron_validate.py plan` prints each one's declared route - mostly a person's, in Revit: a schedule count, the Project Browser, the Properties palette | The route's answer **equals** the fragment's on the same model and selection, written into `proof.second_route` with the date, the model and both numbers. **Q-64 also asks who writes that line** on a fragment already PROVEN, because `accept` refuses one. A route whose answer differs is a finding, not a pass |
