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

**ONLY WHAT IS MISSING IS WRITTEN DOWN.** The owner's instruction, 2026-09-17: a
job that is already covered needs no row. So the absence of a row here is not
evidence of anything - it means either covered, or never asked. The counts table
below is the only record that a list was walked at all.

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
| 4 | The twenty-group sweep - copy, move, rotate, modify, dimension, tag, colour, hide, show, search, list, analyze, export, get, set, filter, calculate, verify, align, rename, update, count, assign, detect, apply | ~230 | ~205 | 14 new, 2 already logged |
| 5 | Datum 2D/3D toggle | 2 | 1 | 1 |

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

---

# List 4 - the twenty-group sweep

Everything below was asked for in one list of roughly 230 jobs, grouped the way a
modeller works rather than the way the library is filed. Tag, Hide, Show, Filter,
Count, Calculate, Align, Rename, Apply and nearly all of Get, Set, Search, List,
Analyze and Verify came back fully covered and get no rows, per the rule at the top.

**Two of its gaps are already above**: elements without level (G-07, asked here three
more times as *search*, *detect* and *verify*) and point-to-point dimension (G-08).

## NOTHING THERE

### G-11 Rotate about anything but the vertical axis

`ROTATE_ELEMENTS` is *"turn these elements about a VERTICAL axis"* and that is the
whole of it. Rotate on X and rotate on Y have no route.

Not every "rotate" needs it: a beam's cross-section rotation is an instance
parameter and `WRITE_ELEMENT_PARAMETERS` already sets it. What is missing is
rotation about an arbitrary horizontal axis - tilting a panel, a sloped support.

### G-12 Move to an absolute coordinate

`MOVE_ELEMENTS` is *"shift these elements by one OFFSET"*. There is no "put it at
X,Y,Z", so **set coordinates** and **set location** have no route either.

A host could subtract current from target and pass the difference - but current
position comes from `REPORT_LOCATION`, so it is two calls and arithmetic between
them, which D-01 puts on the host rather than in the library. Worth deciding
deliberately rather than by default.

### G-13 Annotation cannot be put where somebody wants it

**Move tag, move dimension, rotate tag, set tag location** - none of them.

Every tag fragment here is RULE-based: `ARRANGE_TAGS` spreads them, `STACK_TAGS`
columns them, `CENTER_ROOM_TAGS` centres them, `ARRANGE_TAGS_TO_VIEW_EDGES` parks
them. All of them decide the position. Not one takes a position.

That is a shape, not four fragments: **nothing in the library moves an annotation
element to a point.**

### G-14 Modify thickness

A wall's or floor's thickness lives in its compound structure.
`REPORT_COMPOUND_STRUCTURE` reads it, layer by layer. **Nothing writes it** -
`WRITE_ELEMENT_PARAMETERS` cannot reach a layer.

### G-15 Radial, diameter, angular and height dimensions

Extends **G-02**, which recorded radial only. The sweep asked for four:
**radius, diameter, angle, height**. None exists, and the same 2025 subclass split
applies to the first three.

### G-16 Colour fill scheme

**Create color scheme** - Revit's room and area colour fill legend. Nothing touches
`ColorFillScheme`.

Everything else under Colour is covered and should not be confused with it:
`OVERRIDE_GRAPHICS_IN_VIEW` colours chosen elements (and carries transparency),
`SET_CATEGORY_GRAPHICS` colours categories, `COLOR_BY_PARAMETER` colours by any
parameter - which is how "by level" and "by phase" are answered.

### G-17 List categories

Nothing lists the categories in the model. `SELECT_BY_CATEGORY_NAME` returns NEAR
MISSES when a name does not match, which is the closest thing and is not a list.

### G-18 Get centroid

`REPORT_BOUNDING_BOX` gives the box each element occupies. The centre of mass is a
different number and nothing reports it. `CENTER_ROOM_TAGS` computes a room centroid
internally, for tag placement, and does not hand it back.

### G-19 Calculate perimeter

Area, volume and length are all covered - `REPORT_AREAS`, `MEASURE_ELEMENT_VOLUME`,
`MEASURE_ELEMENT_LENGTHS`. Perimeter is not. It appears inside
`REPORT_DUCT_WEIGHT` and `MEASURE_RUN_QUANTITIES` as sheet-metal arithmetic, never
as a room or floor perimeter anybody can ask for.

### G-20 Refresh view

Nothing regenerates the document or refreshes the active view as a job of its own.
`Regenerate` is called INSIDE several write fragments where their own work needs it,
which is correct and is not the same thing.

### G-21 Detect degenerate lines

`FIND_OVERLAPPING_LINES` finds lines drawn on top of each other. A DEGENERATE line -
zero length, or so short it is an accident - is a different defect and nothing looks
for it.

## THERE, BUT IN TWO STEPS

### G-22 Copy or move to another level

**Copy to upper level, copy to lower level, move to another level.** Both halves
exist and neither does the whole job: `COPY_ELEMENTS` and `MOVE_ELEMENTS` shift by an
offset without touching the level, `SET_ELEMENT_LEVEL` reassigns the level
*"without moving them"*, `SET_WALL_CONSTRAINTS` does the same for walls.

So it is offset-then-reassign, and getting the Z offset right means reading both
levels' project elevations first. That is exactly the arithmetic `CREATE_LEVEL` warns
about, and it is being left to whoever composes the two.

### G-23 Export coordinates

`ASSIGN_LOCATION_DATA` writes each element's position onto its own parameters, then
`EXPORT_PARAMETERS_TO_CSV` gets them out. It works, and it **changes the model to
answer a read-only question** - every element gets written to, on a job that was
only ever asking where things are.

`REPORT_LOCATION` already has the numbers and no fragment exports them.

## NOT A GAP - REVIT WILL NOT ALLOW IT

### G-24 Assign category

**An element's category cannot be changed in Revit**, by the API or by hand. A door
is a door. Nothing can be written here and the row exists so nobody plans it.

`CHECK_CATEGORY_MISMATCH` is the honest answer to the underlying complaint - it finds
things modelled in the wrong category so they can be rebuilt in the right one.

---

# List 5 - the datum 2D/3D toggle

Asked as *"grid change 3d to 2d or 2d to 3d"* - the toggle on the end of a grid
or level line that decides whether an extent change belongs to this view or to
every view.

**One direction is covered and it is the harder one.** `RESET_DATUM_EXTENTS`
writes `DatumExtentType.Model`, which IS 2D to 3D: it throws away the per-view
override somebody made by dragging an end and puts the datum back on its shared
extent, both ends handled separately, grids and levels alike.

## G-25 3D to 2D, and reading which one a datum is on

**Nothing writes `DatumExtentType.ViewSpecific`.** Measured - `SetDatumExtentType`
appears exactly once in the whole library, at
`reset-datum-extents/impl/any/fragment.cs:60`, and the value is always `Model`.

So a modeller can be given back the shared extent and can never be given a
view-specific one. That is the normal drafting job: make this grid 2D in this
view so it can be shortened here without touching the other twenty.

**And the state cannot be read on its own.** `MAXIMIZE_DATUM_EXTENTS` calls
`GetDatumExtentTypeInView` and reports `hiddenByViewExtent` - which datums have an
end on a view-specific extent - but only as accounting while it stretches them.
There is no read that answers *"is this grid 2D or 3D in this view"* without
modifying the model to find out.

The two belong in one fragment: the toggle needs to report what it found before
it changes it, the same way `SET_VIEW_RANGE` and `SET_VIEW_SCALE` both read and
set.

**One warning to carry into it**, and `SET_DATUM_BUBBLES` already states it:
bubbles are per view and extents are NOT - the model extent is shared. Confusing
the two is how somebody fixes one plan and changes twenty. A fragment that sets
extent type has to be explicit about which of those it is doing.
