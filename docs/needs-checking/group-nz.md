# Needs checking — Group NZ

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group NZ - a symbol legend: one legend component per door, window or fixture type, copied from the one the legend holds, each read back with its name beside it: `PLACE_LEGEND_COMPONENTS` (2026-11-15)

**Tracked as GitHub issue [#988](https://github.com/Ajmalpshaik/Heron-AI/issues/988) since 2026-11-16** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

Asked for in the field: Sruthina's drawing set on Revit 2024, model `villa 2`, wanted a symbol legend - a legend view
of legend components for the doors, windows and fixtures - and *"no capability exists"*; a keynote legend and
schedules stood in ([Ajmalpshaik/Heron-AI#854](https://github.com/Ajmalpshaik/Heron-AI/issues/854) item f,
[#853](https://github.com/Ajmalpshaik/Heron-AI/issues/853) D2, [row 5b-1735](../fragment-issues/row-5b-1735.md)).
**Searched first:** no fragment's C# named `LEGEND_COMPONENT` or the Legend Components category; `CREATE_LEGEND_VIEW`
(DEPRECATED, superseded by `DUPLICATE_VIEWS`) said a new legend component could not be placed. **Look-alikes, kept
separate:** `DUPLICATE_VIEWS` (PROVEN) copies whole views and makes the legend this fills; `CREATE_SCHEDULE`'s keynote
legend is a schedule of text, never a symbol; `PLACE_FAMILY_INSTANCES` (PROVEN) places model and detail families, and a
legend component is neither.

| What | Where | What it does |
|---|---|---|
| `PLACE_LEGEND_COMPONENTS` (MODIFY, Revit 2020-2027) | [`brain/fragments/place-legend-components`](../../brain/fragments/place-legend-components/fragment.yaml) | In the legend named, copies the one legend component placed by hand once for each type of the categories named - only types placed in the model unless `onlyUsed` is false - sets each copy's Component (`BuiltInParameter.LEGEND_COMPONENT`) and reads it back, deletes and names any copy Revit would not take, stacks the rest in a column under what the legend held, `spacingMm` paper millimetres apart, and writes `Family: Type` beside each in the default text type. A type a component already shows is left alone (`alreadyShown`). Not a legend, no category, more than 200 types, or no component placed by hand to copy is refused before anything changes |

**Why one must be placed by hand.** Revit's API has no method that makes a legend component, on any release 2020 to
2027; it can only copy one (`ElementTransformUtils.CopyElement`) and change what it shows. It cannot make the first
legend view either - `DUPLICATE_VIEWS` copies one. The refusal says how: *Annotate > Component > Legend Component*, any
family.

**What is done and what is not.** Compiles on every release 2020 to 2027 (`tools/check-fragments-compile.py`,
2026-11-15, a Linux container). **Nothing has run in Revit.** DRAFT. **Only Revit can settle:** whether a copy's
Component takes a type of ANOTHER category than the one placed by hand - a window type on a copy of a door component
(NZ3); whether a component of a type that cannot show the seed's view (a window type seen in Floor Plan) is drawn or
refused; whether `get_BoundingBox(legend)` answers right after `Regenerate`, which the column needs. **Not done:** a
component's View (Floor Plan, Elevation: Front) is the seed's for every copy - changed in Properties; detail level and
host length are left as Revit sets them.

**Routing.** Measured 2026-11-15 on a scratch store of this change, Revit 2024 (`brain/heron_retrieve.py`, no
re-ranker installed): the two sentences declared on the card - *"make a symbol legend of the doors and windows"*, *"add
the plumbing fixtures to the legend"* - reach `PLACE_LEGEND_COMPONENTS` by identity, offered and not run because it is
DRAFT; *"make a legend"* and *"duplicate the legend"* still reach `DUPLICATE_VIEWS`. `tools/check-routing.py` exits 0;
its keyword-route-alone report now lists *"make a legend"* (`DUPLICATE_VIEWS`' utterance) as answered by this card on
that route, where `origin/main` did not - identity still serves it to `DUPLICATE_VIEWS`. `tools/check-intrusion.py`
names neither card.

**The model** - a scratch project from Revit 2024's default architectural template, saved under `%TEMP%\heron-work`,
never a working model. Every write here is ROLLED BACK unless the modeller keeps it. The job file is
[`tools/jobs/legend-components-nz-2026-11-15.yaml`](../../tools/jobs/legend-components-nz-2026-11-15.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`, `--session
<process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **NZ1** | *Arrange, Revit 2024.* `legend-ny`: a new project from the default architectural template (metric), four walls on Level 1, three doors of two types and two windows of one type in them, nothing of Casework. View > Legends > Legend, `Symbol Legend` at 1:50; in it, by hand, ONE legend component of the first door type, Floor Plan | The legend shows one door symbol; the Project Browser names `Symbol Legend` under Legends |
| **NZ2** | *D-30 positive and negative, THE NEGATIVE FIRST.* The job file, `--write`, rolled back | Negative, Casework: placed 0, *"No placed type of Casework in this model"*. Positive, Doors and Windows: placed 2, alreadyShown 1, placedIds two ids, each new component's Properties > Component the type it names |
| **NZ3** | *Another category than the seed's.* The positive's window type, on a copy of the door component | Placed and read back as the window type - or skipped with Revit's reason. **Either is Revit's answer: record it.** Skipped means a seed per category, and the card's purpose changes before it is proved |
| **NZ4** | *The refusals.* A legend with no component in it (duplicate `Symbol Legend` empty with `DUPLICATE_VIEWS`, or make a second by hand); `view` a floor plan; then the positive run kept once through `try-once` and the same call again | *"holds no legend component, and Revit's API cannot place the first one"*; *"... not a legend"*; the second call placed 0, alreadyShown 3 |
| **NZ5** | *Kept once, and the second route.* Through `try-once` on the modeller's yes, Doors and Windows | ONE entry in Revit's undo list; the legend looked at: a column under the first symbol, `Family: Type` beside each, nothing overlapping |
