<!--
Heron-Agent:  none
Heron-Step:   18
Heron-Status: DRAFT
Heron-Since:  0.1.0
Heron-Layer:  brain
See docs/29-metadata-standard.md
-->

# 45 — Five more downloaded skill files, studied

**Asked 2026-10-04.** The owner shared the folder [39](39-three-downloaded-skill-sets.md) studied again,
now thirteen files, and asked for each and everything in it to be checked and, where it is useful, made
part of Heron as a skill or a tool.

**Eight of the thirteen are byte-identical, by MD5, to what 39 already read** - so they were not read a
second time. **Five are new**, and every line of them was read. This page is those five.

**The short answer: almost everything in them that is Revit, Heron already does - usually more carefully.**
What the five gave is not new tools so much as **six things Heron was getting wrong or leaving out, found
by checking their lessons against Heron's code**. Three are defects that were sitting in Heron's own
fragments. Nothing was copied ([D-25](DECISIONS.md), [31 §2](31-studying-the-existing-libraries.md)): no
tool name, no sentence, no file name, no code.

| What it produced | Where it lives now |
|---|---|
| **Three defects in Heron, found by reading Heron's code against a lesson in the files** - a Yes/No column Heron exports that Heron cannot import back; a blank spreadsheet cell that silently wiped a text value; and two fragments that left Revit's own shared parameter file setting pointed at Heron's path, where Undo never reaches | Rows [5b-315, 5b-316, 5b-317](fragment-issues/section-5b-rows-176-200.md) - each fixed in a DRAFT fragment, §5 |
| **Eight fragments widened, one added**, all DRAFT, compiled for every release Heron supports | §5, and [Group CD](needs-checking/group-cd.md) to prove them |
| **A method, written down:** what to read in the model before it goes out as IFC | [`ifc-export-readiness`](../brain/skills/ifc-export-readiness.yaml), DRAFT |
| **What is worth building but touches a PROVEN fragment**, so waits for the owner's word | §6 |
| **Why most of it is out of scope** | §7 |

---

## 1. What was handed over

| File | What it is | Size | Worth to Heron |
|---|---|---|---|
| **A** | A Revit scripting toolkit: twenty-five prompts, each of which **writes a script for the modeller to paste and run** - visual-programming graphs, add-in boilerplate, and calls to a cloud document service | 204 KB | The Revit jobs are nearly all Heron's already. The value was in its scars, one of which is a Heron defect (§4) |
| **B** | A data-exchange toolkit: model to spreadsheet, quantities, clashes, validation - **working on exported files** through offline converters and a data-frame library | 225 KB | The jobs are Heron's on the live model. Its round-trip code exposed two Heron defects |
| **C** | An IFC toolkit built around **an external Python IFC library**: creating, reading, changing and checking `.ifc` files outside Revit | 231 KB | The library is out. What carries over is what goes wrong in Revit's own IFC export, and what can be read in the model before it |
| **D** | A computational-design toolkit, eighteen skills: parametric and generative design, geometry, simulation, fabrication, optimisation and machine learning, **almost all in other tools** | 815 KB | About an eighth of it translates into a Revit job. Its geometry lessons are mostly held |
| **E** | **A larger version of the connector notes 39 studied**: its six workflow sections, then thirty-eight more - aircraft, ships, games, animation, vehicles, and a few real building jobs | 255 KB | Its section on the connector's tools is three-quarters identical to what 39 read. The building sections gave three gaps |

Three of the five state an open licence. That changes nothing: Heron takes mechanisms and lessons and
re-authors them, never text or code, whatever the licence. **The names are left out on purpose**, as in 39.

## 2. The method

The same as [39 §2](39-three-downloaded-skill-sets.md): Rule 0 first - *name the Heron capability that does
the job, or the claim fails*. Six readers took a file each (two for D), read every line, and for every
section named the fragment that covers it **after reading that fragment's card**, not its name. Every
lesson about Revit was checked against the fragment's **code**.

**Then every claim that anything here was built on was checked a second time**, by reading the code again,
before a line was written. All of them held. One reader's suggestion did not survive a second reason, and
it is recorded rather than built (§6, the IFC schema).

---

## 3. Every section, and what Heron has

### A - the scripting toolkit

| Job | Heron |
|---|---|
| Write a visual-programming script, an add-in, a macro | **Out.** Heron never hands the modeller code to run; a fragment is reviewed, versioned and proved first |
| Query, list, count, filter by value or level | **Held** - `FILTER_ELEMENTS_BY_CATEGORY`, `COUNT_ELEMENTS`, `SELECT_BY_PARAMETER_VALUE`, `SELECT_BY_LEVEL` |
| Model health: warnings, unused families, unplaced views, worksets | **Held** - `READ_MODEL_WARNINGS`, `FIND_UNUSED_FAMILIES`, `FIND_UNPLACED_VIEWS`, `LIST_WORKSETS`, and the model auditor. **Its fixed thresholds** ("over so many warnings, investigate") **are rejected**: Heron gives the number, never the verdict |
| Required parameters blank, absent, or outside an allowed list | **Partly, now widened** - `CHECK_MODEL_STANDARDS` version 2 (§5) |
| Links: loaded, where the file is, imports against links | **Held** - `LIST_LINKED_MODELS`, `REPORT_EXTERNAL_REFERENCES`, `RELOAD_LINKS`. **Where each link sits** is §6 |
| Elements on the wrong workset | **Partly** - `SELECT_BY_WORKSET` narrows *to* a workset; *not on* is §6 |
| Clashes | **Held, more soundly** - `FIND_CLASHES` tests every solid, counts an element with no solid as untested, reads links |
| Rooms, areas, totals by department | **Held** - `REPORT_ROOM_SPACE_DATA`, `SELECT_UNENCLOSED_ROOMS`, `REPORT_AREAS`, `SUM_BY_GROUP`. Areas against a brief is an architect's job, not built |
| Schedules: make, field, filter, place | **Held** - `CREATE_SCHEDULE` and its five siblings. A **numeric** schedule filter is §6 |
| Parameters to and from a spreadsheet | **Held** - `EXPORT_PARAMETERS_TO_CSV`, `IMPORT_PARAMETER_VALUES` - and two defects, §4 |
| IFC export | **Held** - `EXPORT_MODEL_TO_IFC` |
| Shared parameters: bind to categories | **Held** - `ADD_PROJECT_PARAMETER` - and a defect, §4. A read-only report of the project's bindings is §6 |
| Rename, renumber, move, rotate, mirror, change type, view filters, project setup, sheets | **Held**, each by a fragment of that name |
| Five skills for a cloud document and coordination service | **Out.** Heron has no cloud account and reaches nothing outside the open model |

### B - the data-exchange toolkit

| Job | Heron |
|---|---|
| Convert a model file to IFC, a spreadsheet or a mesh **without Revit** | **Out** - an external converter, reading files rather than the model |
| Push spreadsheet values back onto elements | **Held** - `IMPORT_PARAMETER_VALUES`, `SET_PARAMETER_VALUES_BY_ID` - two defects, §4 |
| Quantities by type, level, material | **Held** - `MEASURE_RUN_QUANTITIES`, `MEASURE_ELEMENT_VOLUME`, `REPORT_MATERIAL_TAKEOFF`, `SUM_BY_GROUP`. **A table by two keys at once** (level and size) is §6. Its rule-of-thumb ratios are rejected: an assumed design value (D-33) |
| Clashes by bounding box, by vertex distance, with preset clearances | **Held, more soundly** - `FIND_CLASHES`, `CHECK_MINIMUM_CLEARANCE`. Preset clearances are assumed values and are rejected as defaults |
| Prioritise and assign clashes by discipline | **Out** - Heron does not decide who moves |
| Validation: required, naming, level, classification, value patterns, placeholders | **Partly, now widened** - `CHECK_MODEL_STANDARDS` version 2 (§5). Fixed level-of-development tiers are rejected: a standard Heron would be assuming |
| A report that says *compliant* | **Rejected.** A check passes against the values it checked, and Heron says only that |

### C - the IFC toolkit

| Job | Heron |
|---|---|
| The library's mechanics: its object model, query syntax, files, geometry, performance | **Out** - a third-party library working on a file. Revit writes the IFC; Heron reads the model |
| Authoring IFC from nothing, relationships, materials | **Out** as code. The lessons in it are §4 |
| Checking a written file against the schema | **Out** - it needs the library and the file |
| Checking information requirements: class, property, classification, storey | **Partly** - the property half is `CHECK_MODEL_STANDARDS`; the rest is the new recipe |
| Schema differences between IFC versions | **Held by design** in `EXPORT_MODEL_TO_IFC` - the schema is required and never defaulted. **It cannot be typed from a chat**, on purpose: §6 |

### D - the computational-design toolkit

| Job | Heron |
|---|---|
| Design methods, node libraries, NURBS and mesh processing, generative and evolutionary search, structural, energy, airflow and daylight engines, fabrication, robots, optimisation, machine learning | **Out** - other tools, heavy dependencies, and a search loop of thousands of changes breaks one request, one undo |
| Divide a surface and put a panel on every cell | **Held** - `DIVIDE_FAMILY_SURFACE`, `PLACE_ADAPTIVE_POINTS`; on a curtain wall type, `SET_CURTAIN_WALL_GRID` |
| How many different panel sizes a facade has | **Partly** - `REPORT_CURTAIN_ELEMENTS` counts panels by type, not by size; §6 |
| Numbering every piece by building, level, zone | **Held** - `ASSIGN_LOCATION_DATA`, `RENUMBER_SEQUENTIAL` |
| Colour the model by a status or a number | **Held** - `COLOR_BY_PARAMETER`, `SHOW_ANALYSIS_HEATMAP` |
| Views per level, sheets, tags, exports, crop to a room | **Held**, each by its fragment |
| Room proportion and minimum size against values the modeller gives | **Partly** - `MEASURE_ROOM_DIMENSIONS` measures; the check is §6 |
| Glazing per wall and per room, and which way each facade faces | **Not built** - §6 |
| Coordinates: internal origin, project base point, survey point | **Partly, now widened** - `REPORT_LOCATION` version 3 (§5) |
| Mirrored instances | **Not built, now added** (§5) |
| Floors from rooms, occupant load, door clear width | **Held**, or a recipe on `REPORT_ROOM_SPACE_DATA` with the modeller's factor |
| Any report that ends in PASS for a design | **Rejected** for the same reason as B's |

### E - the larger connector notes

| Sections | Heron |
|---|---|
| The library index, the roof workflow, connection diagnostics, the connector reference | **Held** - [39](39-three-downloaded-skill-sets.md); the reference's new lines are headings, a compatibility caveat and the roof view rule 39 lesson 3 already answered |
| A floor plan from scratch, a row of townhouses | **Held as [`building-shell`](../brain/skills/building-shell.yaml)**, with two gaps it showed: a window's sill height (§5) and which face of a wall is its outside (§6) |
| A structural grid with a column at every intersection | **Partly, now widened** - columns had a base and no top; `PLACE_STRUCTURAL_FAMILY` version 2 (§5) |
| Data-centre MEP: what is unconnected, height above level against absolute | **Held** - the `check-connectivity` skill, `SELECT_BY_CONNECTION_STATUS` |
| A facade bay pushed back and the slab under it re-made | **Partly** - move, re-make, disallow join exist; editing a floor's own boundary does not, and its API splits at 2022 |
| Continue a model and check it; resume an interrupted build | **Held**, except that **re-running an interrupted build lays a second set of walls** - §6 |
| Working drawings: views before sheets, exact types, overlaps, export | **Held**, except viewports overlapping on a sheet |
| Aircraft, ships, rockets, bridges, robots, vehicles, games, characters, cameras, animation, physics, rides, sculpture, organic shells, street scenes, a game engine's materials | **Out** - not a BIM model, and not Heron's |

**39 §5 was out of date on one line,** and still said stairs were unbuilt. `CREATE_STAIRS` exists (DRAFT);
§5 there now says so.

---

## 4. The scars, and the three that were Heron's

**A scar is a failure somebody hit and wrote down.** 39 learned that it is the most valuable thing in anybody
else's notes, and that its diagnosis is a hypothesis until something isolates it. Of the forty-odd here,
most were **already held** - checked in Heron's code, not its documents:

- **Units:** Revit stores feet and radians; exact factors, never rounded ones. Held - every fragment converts
  at the edge, and the volume fragment uses the exact foot.
- **Element ids became 64-bit in 2024** and the old integer property is gone by 2026. Held - ids are
  formatted, never read as numbers.
- **The floor, unit and filter-rule APIs changed between releases.** Held - each fragment picks its call by
  release, or finds the overload at run time.
- **A clash test on the first solid only, or by box.** Held - every solid, and an untested element counted.
- **A name two parameters share.** Held for writes since [5b-203](fragment-issues/section-5b-rows-176-200.md).
- **A link read without its transform; a link's *loaded* flag taken as stored.** Held.
- **Overwriting a view filter of the same name strips it from every view.** Held - a name in use is skipped.
- **A zero-area room treated as one fault.** Held - unplaced and unenclosed are kept apart.
- **A short curve, a coordinate compared with `==`.** Held - tolerances everywhere geometry is built.
- **Stairs refused inside an open transaction.** Held, and isolated independently in `CREATE_STAIRS` - their
  diagnosis and Heron's agree.

**Three were broken in Heron, and are fixed at DRAFT** - rows [5b-315 to 5b-317](fragment-issues/section-5b-rows-176-200.md):

1. **A Yes/No round trip.** The export writes a tick box as *Yes* or *No*; the import took only a whole
   number, so every tick box came back refused. Safe, and useless.
2. **A blank cell.** A text column's empty cell cleared the element's value, while a number column's empty
   cell was refused - one empty cell, two meanings, and the modeller asked for neither. Heron now asks.
3. **The shared parameter file.** Two fragments pointed Revit at a file and left it pointing there. That
   setting is Revit's, not the model's, so the one Undo that takes the parameter back cannot take it back.

**Two are recorded as NOT HELD and not built here** - §6: the true north angle for anything that says which
way a facade faces, and nothing yet reports how far the model sits from its internal origin outside
`REPORT_LOCATION`.

**Several claims in the files are wrong about Revit, and none was taken:** that project parameters do not
reach schedules (they do; tags need shared ones); that a link with any rotation or offset has been moved by
mistake (shared coordinates put most of them there on purpose); that one distance from the origin means
the coordinates are wrong (a threshold, not a fact); and a dating of the toposolid one release early.

---

## 5. Built, all DRAFT

**Compiled for every release Heron supports; not one has run in Revit.** The proofs are [Group CD](needs-checking/group-cd.md).
Every one below was DRAFT before it was touched, so **no proven fragment lost its proof** for this.

| Fragment | Version | What it does now, in Revit words |
|---|---|---|
| `IMPORT_PARAMETER_VALUES` | 3 | Takes a Yes/No column back - Yes/No, True/False, On/Off, 1/0 - and reads it back. **Asks every time what an empty cell means**, leave or clear, and counts what it left (5b-315, 5b-316) |
| `ADD_PROJECT_PARAMETER` | 2 | Puts Revit's shared parameter file setting back as it found it, whatever happens; names a category that cannot take the parameter, and one Revit dropped (5b-317) |
| `TRANSFER_PROJECT_PARAMETERS_BETWEEN_DOCUMENTS` | 2 | Puts the setting back the same way (5b-317) |
| `PLACE_HOSTED_FAMILY` | 3 | Takes a **sill height** for a window, sets it, reads it back. Without one it places as before and says what sill Revit gave |
| `PLACE_STRUCTURAL_FAMILY` | 2 | Takes a **top level** and top offset for a column, refuses a top at or below the base before placing anything, and reads the top back |
| `REPORT_LOCATION` | 3 | Gives positions in **shared (survey) coordinates** beside the internal ones, the angle to true north, and where the project base point and survey point are |
| `REPORT_LEVEL_ELEVATIONS` | 3 | Reports each level's **Building Story** tick, and how many elements sit on a level without one - what an IFC export files somewhere else |
| `CHECK_MODEL_STANDARDS` | 2 | Checks a parameter's **value** against a pattern, counts placeholder values the modeller names (TBD, XXX) as blank, and checks **type names** and **room names** against patterns - *every TRG type*, for instance |
| **New:** a report of **mirrored and flipped** instances | 1 | Lists the doors, fixtures and equipment that are mirrored, kept apart from the ones only flipped, by category and type - a mirrored door schedules the wrong hand |

And the recipe [`ifc-export-readiness`](../brain/skills/ifc-export-readiness.yaml): storeys, elements on no
level, duplicates, duplicate IFC identities, elements with no IFC class or set not to export, required
values, MEP pieces off their system - each a capability Heron already has, in order, and **a parameter the
model does not have is NOT CHECKED, never zero**.

### Four things the building taught

1. **A value cannot be left out of a Heron call; it can only be left blank.** The add-in refuses any value
   that arrives with nothing in it, except a yes/no switch marked optional (rows 5b-227 and 5b-252). So the
   sill height, the column's top level and top offset are text values where **blank means what the fragment
   did before**, and a caller that sends nothing is asked rather than run with a guess. The standards check's
   new rules ride as new keys inside the name patterns it already took, so no existing caller breaks.
   Letting an empty text value bind as absent is a change to the add-in, recorded here, not made.
2. **A question was answered by a write.** Before this change *"is this door mirrored"* ranked
   `MIRROR_ELEMENTS` first - PROVEN, and it changes the model - because nothing read the flag. Measured on a
   scratch store: the new report now takes *is this door / fixture / equipment mirrored* and *are the toilets
   mirrored*, and *mirror these to the other room* and *copy it mirrored* still reach the mirror. Routing
   rows were added to `MIRROR_ELEMENTS`, `FLIP_ELEMENTS` and `REPORT_DOOR_ROOM_LINKS` - card wording,
   outside the proof seal. [CD12](needs-checking/group-cd.md) checks it again after the merge.
3. **The Building Story count has no sentence of its own yet.** *Which levels are building stories* still
   ranks `LIST_LEVELS` first. No words were invented for it; it is reached through the recipe, and its own
   utterance waits for the modeller's real words.
4. **The two shared-parameter fragments are ADMIN**, so Heron refuses them until the owner's Admin switch
   is on. Their fix is in; their proof waits for that switch. And `ADD_PROJECT_PARAMETER`, given a name the
   shared parameter FILE does not hold, writes the definition into that file on disk, where no Undo
   reaches either. Its card does not say so yet; recorded here, unchanged.

---

## 6. Worth building, and not built here

**Each touches a PROVEN fragment, whose proof any change to its code makes stale**, or needs a decision, or
needs measuring first. Each waits for the owner, one at a time ([31 §5](31-studying-the-existing-libraries.md)).

| The job | Why it is not in this change |
|---|---|
| **Where each link sits** - offset and rotation from the host, in millimetres and degrees | Widens `LIST_LINKED_MODELS`, PROVEN |
| **Elements NOT on a workset** - *ducts not on M-HVAC* | Widens `SELECT_BY_WORKSET`, PROVEN |
| **A numeric schedule filter** - width 600 mm or more | Widens `SET_SCHEDULE_FILTERS`, PROVEN, which builds every filter from text |
| **Room proportion and minimum size**, against the modeller's values | Widens `MEASURE_ROOM_DIMENSIONS`, PROVEN |
| **Panel sizes on a curtain wall**, grouped within the modeller's tolerance | Widens `REPORT_CURTAIN_ELEMENTS`, PROVEN |
| **A build that can be run twice** - a wall already there is not drawn again | Widens `CREATE_WALL`, PROVEN, then the floor, ceiling and roof creators |
| **Copy to another level** that lands *on* that level, not on the old one with a large offset | `COPY_ELEMENTS` is PROVEN; until then the route is `COPY_ELEMENTS` then `SET_ELEMENT_LEVEL` |
| **Which face of a wall is its outside**, and a point that says so | Widens `CREATE_WALL`, PROVEN |
| **Glazing per wall and per room, and each facade's bearing from true north** | A new fragment; it needs `REPORT_LOCATION` version 3 proved first |
| **A report of the project's parameter bindings** | A new read; nothing asks for it yet |
| **A takeoff by two keys at once** - level and size | Widens `SUM_BY_GROUP`; nothing asks for it yet |
| **The IFC schema typed in a chat** | **A recorded decision, not a gap.** [Section 6](fragment-issues/section-6.md) of the register keeps the schema untypeable on purpose: its names differ per release, and a name that works on one release and not another is worse than refusing on both. A reader proposed resolving it on the running release and listing the names that release has. That answers a different worry, and the decision is the owner's |
| **Elements with no IFC class, before export, through the category mapping** | Whether the API can read the export mapping on 2020 to 2027 is not known; CD9 measures it before anything is written |
| **A caveat on volume takeoffs** - two overlapping elements that are not joined are counted twice | One line in two PROVEN cards; wording in `fragment.yaml` is outside the proof seal, and is left for a session touching them |

---

## 7. Out of scope, and why

- **Code for the modeller to run** - scripts, graphs, add-ins. A Heron fragment is reviewed and proved; a
  generated script is neither.
- **Anything outside the open model** - cloud services, offline converters, files read instead of the model.
- **Heavy dependencies** - an IFC library, data-frame and geometry libraries, solvers, machine learning.
- **Design and engineering decisions** - generative layout, structural and environmental analysis,
  clash responsibility, rule-of-thumb quantities, fixed development tiers, preset clearances. Heron asks for
  every value and decides none ([D-33](DECISIONS.md)).
- **What is not a building model** - games, animation, vehicles, ships, aircraft, robots, physics.
- **Any verdict of compliance**, in every file that offered one.

## 8. What this is not

**Not an import** - no file from the folder is in this repository, and none of its sentences. **Not a
proof** - nine fragments and one recipe owe a run against a real model, and Group CD says which.
