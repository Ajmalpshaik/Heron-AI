<!-- Heron-Agent:  none -->
<!-- Heron-Step:   17 -->
<!-- Heron-Status: DRAFT -->
<!-- Heron-Since:  0.1.0 -->
<!-- Heron-Layer:  docs -->
<!-- See docs/29-metadata-standard.md -->

# What the owner asked for, and what is not there - 2026-09-17

**This is a GAP LIST, not a plan.** The owner read out lists of jobs he wants Heron
to do. Each was checked against `brain/fragments/` on the day. What follows is only
the part that came back missing, or came back with something wrong with it.

**It is a work note and it is meant to be deleted.** Every row here ends either as a
built fragment or as a row in [PROPOSALS.md](../PROPOSALS.md). Nothing here is a
decision and nothing here overrides the specification.

Derive the total rather than trusting one typed here:

```bash
ls brain/fragments | wc -l
```

---

## Lists checked so far

| # | Subject | Asked | Covered | Missing |
|---|---|---|---|---|
| 1 | Grids and levels | 3 | 3 | 0 |
| 2 | Structural, annotation, views | 17 | 13 | 4 |
| 3 | Selection | 16 | 14 | 2 |

---

## NOTHING THERE - a fragment has to be written

### G-01 Create beam

Every placement fragment in the library is POINT based. `PLACE_FAMILY_INSTANCES`
takes `IList<XYZ>` and a level. A beam is placed from a CURVE with
`StructuralType.Beam`, and nothing here does that.

This is not one missing fragment, it is a missing SHAPE: braces, and any line-based
loadable family, are placed the same way and are equally unreachable.

### G-02 Create radial dimension

Nothing in the repository mentions `RadialDimension`, `DiameterDimension` or
`ArcLengthDimension`. `CREATE_DIMENSION` is linear only, and narrowly so - see G-07.

Worth noting against [`revit-version-support`](../16-version-support-strategy.md):
these became their own subclasses at Revit 2025, so whatever is written has to
carry the version split.

### G-03 Create family

Nothing creates a new family DOCUMENT. `LOAD_FAMILY`, `OPEN_FAMILY_FOR_EDITING`,
`RENAME_FAMILY` and `UPGRADE_FAMILY_FILES` all act on a family that already exists.

### G-04 Create phase - and it may not be writable at all

Nothing creates a phase. `REPORT_PHASES`, `SET_ELEMENT_PHASE`, `SET_VIEW_PHASE` and
`SELECT_BY_PHASE` all read or assign phases that are already in the document.

**UNVERIFIED AGAINST THE API REFERENCE:** the belief is that `Document.Phases` is
read-only and the Revit API exposes no phase creation at all, which would make this
a permanent gap rather than an unbuilt fragment. That has NOT been checked against
the reference assemblies here and must be before anyone plans work on it -
[NEEDS-CHECKING](../NEEDS-CHECKING.md).

### G-05 Select hidden elements

Nothing answers "what is hidden in this view". `DIAGNOSE_VISIBILITY` takes ONE
element and one view and names which of the eight mechanisms hid it - a different
question.

The set could be composed - everything in category, minus `SELECT_VISIBLE_IN_VIEW` -
but no fragment does that subtraction, so it falls to the host.

---

## THERE, BUT WRONG - an existing fragment needs a change

### G-06 Structural placement goes in as non-structural

`brain/fragments/place-family-instances/impl/any/fragment.cs:58` calls

```csharp
doc.Create.NewFamilyInstance(point, symbol, level, StructuralType.NonStructural);
```

with the structural type HARDCODED. So a structural column or an isolated footing
placed through it is created, and created wrong - it is not a structural member, and
nothing says so.

The fragment's own `purpose` names air terminals, sprinkler heads and detectors, so
the hardcoding was right for what it was written for. The defect is that there is no
other route, so structural work silently uses it.

**Proposed:** `structuralType` becomes a request input. That changes a PROVEN
fragment's contract, so it needs re-proving under D-30.

### G-07 Select elements without level - counted, never returned

`SELECT_BY_LEVEL` resolves a level for every element and reports

```yaml
    - name: unresolvedLevel
      role: accounting
      type: int
```

an INT. So the model can say there are 412 elements with no level and cannot hand
them over. The elements are in scope during the run and are discarded.

**Proposed:** return the set beside the count. Note the fragment's own warning first
- a great many elements legitimately have no level (views, sheets, materials, most
annotation), so an unbounded run returns a large and correct set that reads as an
error. Whatever is returned needs the same sentence attached.

---

## THERE, BUT NARROWER THAN THE WORDS SUGGEST

### G-08 Linear dimension between two points or faces

`CREATE_DIMENSION` is the DUCT SPACING dimension and says so: centreline to
centreline, across parallel linear elements. The specific jobs are covered -
`DIMENSION_GRIDS_AND_LEVELS`, `DIMENSION_ROOMS`, `DIMENSION_WALL_OPENINGS`,
`DIMENSION_FAMILY_INSTANCES`, `DIMENSION_MEP_RUNS`.

What is not covered is the plain one: dimension between two references the user
names. Each reference kind needs different geometry handling, which is why the
library split it - so this is a real gap, not an oversight.

### G-09 Curtain wall

`CREATE_WALL` takes a `WallType`, and a curtain wall type IS a `WallType`, so it
should build one. **It is proven on basic walls only** and its own routing comment
points curtain questions at `REPORT_CURTAIN_ELEMENTS`, which READS panels and
mullions and creates nothing.

So: probably works for the wall itself, unproven, and there is no route to curtain
grids, mullions or panel swaps.

### G-10 No preset category groups

"Structural elements" and "architectural elements" both work through
`SELECT_BY_CATEGORIES`, which takes the category list. It works, and the list has to
be typed out every time, correctly, from memory.

`SELECT_BY_CATEGORIES`'s own purpose makes the argument for fixing this - it exists
because *"what somebody means by the ductwork is five Revit categories"*. The same
reasoning applies to "the structure" and "the architecture", and the MEP groups are
not preset either.

---

## Checked and NOT a gap

Recorded so the same ground is not walked twice.

| Asked for | Answer |
|---|---|
| Create vertical grid / horizontal grid | `CREATE_GRID` - one fragment, both directions, by two points. Also `CREATE_GRIDS` from bay spacings |
| Create level | `CREATE_LEVEL`, `CREATE_LEVELS`. Makes the datum only - no plan view, which is `CREATE_PLAN_VIEW` |
| Create slab | `CREATE_FLOOR` |
| Create basic wall | `CREATE_WALL` |
| Create tag | `TAG_ELEMENTS`, `TAG_ELEMENTS_IN_VIEW` |
| Create annotation text | `CREATE_TEXT_NOTE` |
| Create schedule | `CREATE_SCHEDULE`, `CREATE_KEY_SCHEDULE` |
| Create 3D view | `CREATE_3D_VIEW` |
| Create section box | `SET_VIEW_SECTION_BOX` (the section VIEW is `CREATE_SECTION_VIEW`) |
| Create view filter | `CREATE_VIEW_FILTER`, `CREATE_VIEW_FILTERS_BY_VALUE` |
| Create shared parameter | `ADD_PROJECT_PARAMETER` - **DRAFT, not proven** |
| Select by category | `SELECT_BY_CATEGORY_NAME`, `FILTER_ELEMENTS_BY_CATEGORY`, `SELECT_BY_CATEGORIES` |
| Select by level | `SELECT_BY_LEVEL` |
| Select by name | `SELECT_BY_FAMILY`, `SELECT_BY_PARAMETER_VALUE`, `SELECT_TYPES` |
| Select by ID | `FILTER_ELEMENTS_BY_ID` |
| Select by parameter / by parameter value | `SELECT_BY_PARAMETER_VALUE` (text), `SELECT_BY_NUMERIC_PARAMETER` (units) |
| Select inside bounding box | `SELECT_IN_REGION` |
| Select in active view / visible elements | `SELECT_VISIBLE_IN_VIEW` |
| Select by family type | `FILTER_ELEMENTS_BY_TYPE`, `SELECT_TYPES` |
| Select duplicates | `FIND_DUPLICATE_ELEMENTS`, `FIND_DUPLICATE_VALUES` |
| Select by phase | `SELECT_BY_PHASE` |
