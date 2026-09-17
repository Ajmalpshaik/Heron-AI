# The last ten Revit agents, and why every one of them reads

**2026-09-17.** The REVIT ENGINEERING session, on `revit/views-sheets-levels`. **No Revit was
touched**: another session had the only one open for the whole sitting, and the owner's instruction
was *"create but dont go to revit i will tell you if you can go to revit."*

**Revit Engineering goes from 26 of 36 to 36 of 36.** The register goes from 227 built to 237, and the
four rows still outstanding are the ones [D-75](../DECISIONS.md) and [D-77](../DECISIONS.md) already
deferred — none of them is a Revit row.

---

## 1. Ten agents, and a decision that governs all of them

| agent | operation | what it is for |
|---|---|---|
| `HERON-REVIT-LVL-027` | `list_levels` | two levels at one elevation · a level with nothing on it · names that sort wrong |
| `HERON-REVIT-WRK-014` | `list_worksets` | who owns what · worksets closed in this session, hiding elements from every count |
| `HERON-REVIT-VIE-013` | `list_views` | a view with no template · a view on no sheet · a template nothing uses |
| `HERON-REVIT-SHT-029` | `list_sheets` | a numbered sheet with nothing on it · no titleblock · two titleblock families |
| `HERON-REVIT-RM-028` | `list_rooms` | rooms against MEP spaces · unplaced vs not-enclosed |
| `HERON-REVIT-SCH-026` | `list_schedules` | what each schedule is *leaving out* |
| `HERON-REVIT-FAM-012` | `list_families` | types placed nowhere · in-place families · many types, none used |
| `HERON-REVIT-EXP-018` | `check_export` | would an export be worth sending |
| `HERON-REVIT-IMP-019` | `list_imports` | linked against imported |
| `HERON-REVIT-DIM-031` | `list_annotation` | **an overridden dimension** · a tag that lost its host |

### Every one of these rows says MODIFY or PUBLISH. Every one is registered at Read.

That is not timidity and it is not a new idea — it is the column's own definition, *"the **highest**
permission level it can require"*, and the precedent `PAR-011` and `GRP-033` set, whose entries in
[`HeronOperationRegistry.cs`](../../platform/Heron.Core/HeronOperationRegistry.cs) say it in as many
words: **a write will be a separate entry at its own risk, because the risk is looked up by name and
one name must mean one thing.**

It is also the only honest position for code that has never run in Revit. Moving a level's elevation
drags every element hosted on it. Applying a view template changes what everybody on the job sees.
Renumbering a sheet breaks every drawing reference pointing at it. Relinquishing a borrowed element
loses somebody's unsynchronised work. None of those should be attempted first by a machine with no
Revit on it.

---

## 2. The three the code refuses to do, and why each refusal is the point

### Loading a family — `FAM-012`

The row lists *"families, types, **loading**, placement"*. Loading is exactly what the file will not
do, and the reason is a real incident rather than a hypothetical:

> **A family carries its own materials, and loading it overwrites the project's.** Six families loaded
> on one job silently reset the pipe colour for the whole model. Nothing warned. No count moved. **No
> check in this repository could have seen it**, because the element count was identical either side.

A family also brings its own types, nested families, parameters and sometimes line patterns. *"Load
this family"* is a request to merge one document into another, offered in the user interface as though
it were opening a file. So `LOAD` is not an operation here, and if it is ever built it belongs at a
much higher risk than anything in this batch.

### Exporting — `EXP-018`

PUBLISH is the level for sending something **out**. Once a file has left there is no undo, the
recipient has it, and somebody may already be building from it. A bad read wastes a minute; a bad
export is a drawing on a site hut wall.

So what exists is `check_export`, which answers **"would an export of this be worth sending?"** — the
question people actually have, and the one nothing in Revit answers until the file is already written.
It finds sheets that would print blank, **links that are not loaded** (an unloaded link exports as
nothing at all, so the drawing goes out with that model missing and the geometry it was coordinated
against absent), and rooms with no area feeding area schedules.

It reports and does not judge: a coordination model with links deliberately unloaded is a legitimate
thing to export, and this has never seen anybody's issue process.

**The doing half is deliberately absent.** It needs a destination, an overwrite decision, a format and
a person who meant it. [27](../27-build-order.md)'s rule is that a write path built before its safety
path is a write path that ships without one — so the safety path is what was built, first.

### Importing — `IMP-019`

A linked CAD file stays outside the model and comes out cleanly. An imported one is copied **into** it
and never leaves: its layers, line patterns, text styles and fonts are in the project permanently,
appear in every dialog from then on, and **deleting the import does not remove them.**

The two look identical in the drawing area. Only Manage Links and the Import category tell them apart,
and nobody opens those until the file is already slow. So they are counted separately and named, every
time.

---

## 3. Two defects found in this session's own code, before either shipped

Both were caught by re-reading rather than by any gate. Both would have passed every check available
today, because this repository has no Revit to run them against.

### A counter that could only ever report zero

`RevitRooms.cs` was written with a `roomsRedundant` count — two rooms in one enclosure, which is a real
and expensive fault. Its detector returned `false` unconditionally.

That is **a gate reporting zero for ever and being read as evidence that the check works**, which is
the exact mistake `RevitSheets.cs` declines to make about sheet numbers two files away. It was removed
rather than shipped.

Redundancy genuinely cannot be seen from here: Revit reports it through the **Warnings list** rather
than on the room, and matching a warning by its description text would work in English and silently
report zero in every other language. So the agent now *says* it does not check that state, in the
answer as well as in the source, and a silence is not read as a clean bill.

### A sweep of the whole model, once per schedule

`RevitSchedules.cs` asked *"how many elements are in this category"* inside its per-schedule loop. On a
job with fifty schedules that is fifty full sweeps of the document — millions of iterations, **on
Revit's own thread**, which the conventions forbid outright because a slow handler freezes the model
for whoever is working in it.

One sweep, then a lookup. The comment explaining why is in the file, because the next person to add a
per-schedule number will reach for the same shape.

**Neither defect was findable by a test on this machine.** The sample models here hold no rooms at all,
so the room counter would have read zero correctly for the wrong reason, and no model here is large
enough for the sweep to be noticeable.

---

## 4. The argument that had to be made three times

`list_phases` already says it. `list_views` says it again. `list_schedules` says it a third time:

> **A number read off a model is a number through a filter.** A count in a view has been through that
> view's template, discipline, detail level, filters and crop. A count off a schedule has been through
> its category, its phase and its filters. *"412 ducts"* is a fact about a view, and *"48 doors"* is a
> fact about a schedule, at least as much as either is a fact about the model.

Having to write it in three separate agents is itself the finding. `list_schedules` acts on it rather
than only stating it: every schedule is reported with **what the model holds in that category** beside
it, so the difference between the two numbers is visible instead of invisible.

---

## 5. Two decisions taken rather than assumed

**Sheets belong to the Sheet Agent.** `VIE-013`'s row reads *"Views, **sheets**, view templates,
visibility"* and `SHT-029`'s reads *"Sheets … **Distinct from views**"*. Both claimed them. The owner
settled it on 2026-09-17. The overlapping words were left in **both** rows rather than edited out of
one, because `VIE-013`'s row is quoted from the specification and deleting quoted text to record a
decision loses the fact that a decision was ever needed.

**A view is placed if either of two things places it.** A drawing arrives on a sheet as a `Viewport`; a
schedule arrives as a `ScheduleSheetInstance` and carries no `Viewport` at all. Sweeping only
`Viewport`s reports every schedule in the job as unplaced — a confident wrong answer, and the kind this
repository keeps writing about.

---

## 6. What is proven, and what is not

| | |
|---|---|
| `check-docs`, `check-metadata`, `check-structure`, `check-package` | all exit 0 |
| `agent-count.py` | 250 agents · 237 built · 9 host-provided · 4 left · **Revit Engineering 36/36** |
| `check-compile.py` | all five projects, all eight releases, **2020 through 2027**, 0 warnings |
| `check-api-surface.py` | every referenced type and member exists on every release |

**None of it has been run in Revit, and every file says so in its own header banner.** *"It compiles"*
is not *"it works"*. These are **built, not proven**, and [D-30](../DECISIONS.md) is what would change
that: a real named model, a positive case, a negative case and a staleness fingerprint.

**Nothing was deployed.** `check-compile.py` builds every release into one shared folder and the newest
wins, which would leave .NET 10 binaries that Revit 2024 refuses — so the deploy was not run, and the
folder is the worktree's own `bin/`, never the installed add-in.

---

## 7. What the next session needs

1. **Revit, and the owner's word that it is free.** Ten agents are waiting on one thing.
2. **A model with rooms in it.** Neither sample model on this machine has any, so `list_rooms` has
   never had anything to count. Same for worksets: nothing here is workshared, so the *"not
   workshared"* branch is the only one that could ever have run.
3. **The write halves, if they are wanted.** Every one of these ten has a write its row implies and
   this session declined to build. Each needs its own registry entry at its own risk, and the
   safety rails — preview, re-count, document pinning, permission gate — that Step 6 already
   defines for moves.
