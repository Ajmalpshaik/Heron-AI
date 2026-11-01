# Needs checking — Group JU

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-11-01 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group JU - the model's 3D geometry as triangles for the Companion's 3D model view: `REPORT_MODEL_GEOMETRY` (2026-11-01)

Asked for by the owner on 2026-11-01: a 3D view of the model in the Heron Companion's Model Dashboard
([docs/40](../40-heron-companion.md) section 21.10 to 21.12; the plan's phase 6), drawn by Heron's own
WebGL renderer. Nothing in Heron handed back geometry a renderer can draw - `heron_retrieve` for *"model
geometry for the dashboard"*, *"show the model in 3D"* and *"3D view of the model in the companion"*
offered `REPORT_ELEMENT_GEOMETRY` (one element's shape in numbers), `CREATE_3D_VIEW` (a new Revit view, a
write) and the dashboard's counts, and the Matcher found nothing for the capability. The geometry walk is
`REPORT_ELEMENT_GEOMETRY`'s and the triangulation `REPORT_GEOMETRY_COMPLEXITY`'s, re-used.

| What | Where | What it does |
|---|---|---|
| `REPORT_MODEL_GEOMETRY` (READ) | [`brain/fragments/report-model-geometry`](../../brain/fragments/report-model-geometry/fragment.yaml) | one JSON string, `geometryJson` format 1: per placed model element, its category and its triangles in metres, the vertices made small by subtracting one `offset` (the middle of what is drawn). Scope: the **active 3D view** - its section box, hidden categories and hidden elements honoured - or, with no 3D view active or `wholeModel` true, the whole model at Coarse. Optional `categories` (BuiltInCategory names, an unknown one refused) and `maxTriangles` (150000 left out, above 500000 refused). Largest elements first; the budget stops the reading and `leftOut` counts what it left. Links, CAD, rooms and spaces are not drawn and `notRead` says how many. A pipe or duct that is only a line at the first detail level is read again at Fine. **Version 2 (2026-11-01):** it watches its own time - an optional `timeLimitSeconds` (40 left out; below 5 or above 55 refused) stops the reading as the budget does, the element being read and every one after it left out, and the JSON gains `stopped`: `"triangles"`, `"time"` or `null`, with a `capped` line *"time: stopped at N s; ..."* for the clock. `format` stays 1 |

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-01), and every Revit member it calls was read off the shipped
assemblies of all eight (`tools/api-surface --members`), each with one signature 2020 to 2027. Version 2,
the same day, compiles on all eight too and adds no Revit member: its clock is `Environment.TickCount`,
because a fragment script references mscorlib and not `System.dll`, where `Stopwatch` lives on the .NET
Framework releases (2020 to 2024). **Nothing here has run in Revit.** The card is DRAFT. The brain module that reads the JSON, the Companion panel and
the renderer are the owner's, built beside it on the same branch, and are not part of this group.

**The model - a copy, never a working project.** Revit 2024: a **fresh** copy of
`Snowdon Towers Sample HVAC.rvt` - its building is in its six Revit links, so the host holds the ducts, air
terminals and mechanical equipment. The job file is
[`tools/jobs/report-model-geometry-2026-11-01.yaml`](../../tools/jobs/report-model-geometry-2026-11-01.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`. **The fragment reads the active
view of the model in FRONT** - arrange each row by bringing the view named to the front first; `--in` reads
a model without bringing it forward.

| # | Run | Look for |
|---|---|---|
| **JU0** | *Arrange, by hand, saved in the copy.* The Snowdon copy in front. Duplicate `{3D}`, name it `Heron empty box`, turn on its Section Box and drag the box wholly clear of the building - nothing inside it; leave that view active and save | The view in the Project Browser, its box empty on screen |
| **JU1** | *The D-30 positive.* The job file's leg - `wholeModel=true`, `maxTriangles=20000` - with `Heron empty box` active | `triangles` above 0 and at most 20000; `scope` `"whole model"`, no `active view:` line; `leftOut` above 0 and one `capped` line, *"triangles: the budget of 20000 was reached; N element(s) left out - the largest were drawn first"*; `len(elements) + noGeometry + leftOut = elementsRead`; every `c` indexes `categories`, every `t` index is below its `v` length / 3; `stopped` `"triangles"` (should the 40-second clock come first, `"time"` with its own `capped` line - record which) |
| **JU2** | *Its negative - the first case to run.* The job file's leg - `wholeModel=false` - the same view active | `triangles` 0; `scope` `"active 3D view 'Heron empty box'"`; `elements` `[]` and `categories` `[]` - **never null** - `offset` `[0,0,0]`, `elementsRead` 0, `noGeometry` 0, `leftOut` 0, `capped` `[]`, `stopped` `null`. Not one of JU1's ids. **FAIL:** any element - the scope fell back to the whole model, or the section box was not honoured |
| **JU3** | *The positive the owner asked for.* `{3D}` active and untouched: `python mcp/client/heron_bridge_client.py fragment report-model-geometry` - nothing set | `triangles` above 0; `scope` `"active 3D view '{3D}'"`; `categories` holds Ducts, Duct Fittings, Air Terminals and Mechanical Equipment, sorted by name; a `links:` line in `notRead` for the links the view shows, and **no** link's element among the ids; an air terminal's `v` plus `offset` lands where its Properties place it, to the millimetre; `stopped` `null` unless a limit was met. Record `triangles`, `elementsRead`, `noGeometry`, `leftOut`, `stopped` and the `findings` line *"Read in N s of the 40 s limit"* |
| **JU4** | *How long it takes.* JU3's time on the reply, then `--set wholeModel=true` with the default budget, timed | Both recorded with their `triangles`, `elementsRead`, `stopped` and `findings`' *"Read in"* seconds - the reply's time less that is the extent pass and the JSON's writing, which the clock cannot cut short. The add-in stops a fragment at `revit.operationTimeoutSeconds` (60 s by default) and a stopped read hands back nothing - the whole model is the case to watch, and the active 3D view with a section box is the way to keep it small |
| **JU5** | *The budget.* `{3D}` active, `--set maxTriangles=2000` | `triangles` at most 2000 and above 0; `leftOut` above 0; `stopped` `"triangles"`; the one `capped` line names 2000 and `leftOut`; the elements drawn are among the longest in the view - equipment and main ducts, not grilles |
| **JU6** | *Pipes and ducts at Coarse.* `{3D}` set to Coarse in its View Control Bar, then JU3 again | Ducts drawn as tubes - with triangles, not in `noGeometry` - and `findings` has the line counting elements that were *only lines at the view's detail level* and were read again at Fine. **If ducts come back in `noGeometry` with no such line, Revit gave a solid at Coarse after all, or gave nothing** - record which, and correct the card's note |
| **JU7** | *No 3D view active.* A floor plan in front (`M1`, or any plan), nothing set | `scope` `"whole model"`; `notRead`'s first line *"active view: '<the plan>' is not a 3D view, so the whole model was read - open a 3D view first to draw only what it shows"*; `triangles` above 0 |
| **JU8** | *One category, capitals wrong on purpose.* `{3D}` active, `--set categories=ost_ductcurves` | `categories` `["Ducts"]` and nothing else; every id a duct; `triangles` below JU3's |
| **JU9** | *The refusals.* `--set categories=OST_Wals`, then `--set maxTriangles=600000` | Both **refused** before anything is read: the first names `'OST_Wals'` and how a category is spelt, the second names the limit of 500000; no `geometryJson` either time |
| **JU10** | *A category the view hides.* `{3D}` with Ducts unticked in its Visibility/Graphics, `--set categories=OST_DuctCurves` | `triangles` 0, `elements` `[]`; `findings` says Ducts (OST_DuctCurves) has nothing in the active 3D view '{3D}'. Tick Ducts back on after |
| **JU11** | *An annotation category, and a category only the link holds.* `{3D}` active, `--set categories=OST_DuctTags`; then `--set wholeModel=true --set categories=OST_Walls` (first check a Walls schedule in the host is empty) | Duct Tags: `triangles` 0, `notRead` *"categories: Duct Tags (OST_DuctTags) is not a model category, so nothing of it is drawn"* - answered, not refused. Walls: `triangles` 0, `findings` says Walls has nothing in the whole model - **the link's walls are never drawn** |
| **JU12** | *What Revit does at the section box, and with temporary hide.* In `{3D}`, a section box cutting through a duct run; then one air terminal hidden with Temporary Hide/Isolate | Record both: whether a duct crossing the box comes back **whole or cut** at the box (the card says whole, unmeasured), and whether the temporarily hidden terminal is **left out** (its id absent). The card's notes are corrected to what is seen |
| **JU13** | *The second route.* On JU3's run: `FILTER_ELEMENTS_BY_CATEGORY` (which `SELECT_BY_CATEGORY_NAME` was merged into on 2026-11-01) with `categoryName=Ducts`, `inViewOnly` `{3D}`, then `REPORT_BOUNDING_BOX` on the elements JU3 drew | The ducts selected = the ducts drawn plus their share of `noGeometry` and `leftOut`; the combined box's middle is `offset`, within the few millimetres an extent is wider than its solid. **A disagreement is a FRAGMENT-ISSUES row**, never a rounding |
| **JU14** | *The brain reads it.* JU3's `geometryJson` through `brain/heron_model_view.py` | It reads whole, and each `notRead` and `capped` line is shown as the page says |
| **JU15** | *Version 2: the clock stops it - the case that matters for it.* A floor plan or `{3D}` in front: `--set wholeModel=true --set maxTriangles=500000 --set timeLimitSeconds=5` - the budget too high to stop it first | `stopped` `"time"`; `leftOut` above 0; one `capped` line *"time: stopped at 5 s; N element(s) left out - the largest were drawn first"*, N being `leftOut`, and no `triangles:` line; `len(elements) + noGeometry + leftOut = elementsRead`; the reply carries `geometryJson` - **never "stopped, rolled back"**. Record the reply's time beside `findings`' *"Read in"* seconds. If the extent pass alone took 5 s, `elements` is `[]` and `leftOut` = `elementsRead`, still `stopped` `"time"` - record it. **If it comes back `stopped` `null`**, the whole model read in under 5 s: record the time, and repeat on a larger model |
| **JU16** | *Version 2's refusals.* `--set timeLimitSeconds=4`, then `--set timeLimitSeconds=56` | Both **refused** before anything is read, each naming the limits 5 and 55; no `geometryJson` either time |
| **JU17** | *Above what the default timeout allows - recorded, not a pass or fail.* With `revit.operationTimeoutSeconds` at its default 60: `--set wholeModel=true --set maxTriangles=500000 --set timeLimitSeconds=55` on the largest model to hand | Which stop came first: the fragment's own (`stopped` `"time"`, an answer) or the add-in's at 50 s - a sixth of 60 kept for the rollback (`HeronRunClock.BudgetFor`) - which hands back nothing. The card says the add-in's comes first above about 45 s; this row is what shows it, and why 40 is the default |

**Then sign**, on the owner's word only: `python brain/heron_validate.py accept report-model-geometry --by
"Ajmal PS"` once JU1, JU2, JU3 and JU15 pass, and `heron-status` set to PROVEN in the same change. A row that
fails is recorded here and the card corrected; nothing in a row changes to make it pass.

**Routing, measured 2026-11-01 in a cloud container on its `model` backend, re-ranker absent** - the owner's
PC ranks with `model+rerank`, which is **NOT RUN** here. `heron_retrieve.find` answers *"show the model in
3D"*, *"3D view of the model in the companion"* and *"model geometry for the dashboard"* with this card by
identity, and the paraphrases *"show me the model in 3d in the companion"* and *"draw the model in the
dashboard"* on the hybrid route; *"what is this element's geometry"* still reaches `REPORT_ELEMENT_GEOMETRY`,
*"how complex is this family"* and *"how much geometry does this family carry"* `REPORT_GEOMETRY_COMPLEXITY`,
*"make a new 3d view"*, *"create a 3d view"* and *"open the 3d view"* `CREATE_3D_VIEW`, and *"model
dashboard"* `REPORT_MODEL_DASHBOARD`. *"Show the model in 3D"* reached `CREATE_3D_VIEW` (a write) before
this card; both cards carry the row. Three paraphrases moved here, each from a card that answered them
worse and none onto a write: *"view the model in 3d"* (was `CREATE_3D_VIEW`), *"show the building in 3d"*
(was `REPORT_SPACE_ENVELOPE`) and *"show the 3d view"* (was `OPEN_RENDER_DIALOG` - in Revit words it may
mean *open the {3D} view*, `OPEN_VIEW`'s job, which reached it neither before nor after: a question for the
owner). `check-routing` against a run taken before the card: no question reaches a write, the card takes
no other card's declared sentence, and one new contest - *"model geometry for the dashboard"* ranks
`REPORT_MODEL_DASHBOARD` first **on the keyword route alone**; both cards carry the row.

**What the reply carries, estimated off the code on 2026-11-01, not measured.** About 30 to 40 bytes a
triangle - roughly 5 MB at the default budget and 17 MB at 500000. A provided string crosses whole, with no
cap on the way back (`REPORT_MODEL_DASHBOARD`'s notes, JT). **Asked through `revit_read`, all of it is
printed into the chat**: the Companion's model view is the reader this is for.
