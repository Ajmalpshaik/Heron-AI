# Needs checking — Group BX

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone.
> **The register's rules, and every group's place in it, are on that page.** A new row for this group
> goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads
> it back into the register for every tool that reads the register.

## Group BX - a stair between two levels, and the opening over it, through Revit's stair edit mode (2026-10-03)

**The owner's PC, Revit 2024, on a SAVED COPY of Project2.** Asked for in the school chat on 2026-10-03 -
*"make the stair also"* - when no tool in the library could make a stair:

- [`create-stairs`](../../brain/fragments/create-stairs/fragment.yaml) - `CREATE_STAIRS`, FRG-ELE-076
- the add-in change that lets it run at all: [D-112](../decisions/D-112.md)

**What is already known, and it is NOT a proof.** Measured on Project2 (Revit 2024, session 41260),
rolled back, nothing kept: inside the add-in's own transaction a `StairsEditScope` reads `IsPermitted`
false and refuses to start; a shaft from Level 1 to Level 2 over Stair 1 removed nothing from either
slab, and a floor opening in the Level 2 slab removed 8 m3. The fragment compiles on all eight releases
and the add-in on 2020, 2024 and 2027 (2026-10-03), and every stair member it calls was read in the
reference assemblies of all eight. **The new add-in has not been in a Revit.** The proof plan is
[`tools/jobs/create-stairs-project2-2026-10-03.yaml`](../../tools/jobs/create-stairs-project2-2026-10-03.yaml).

| # | Check | Expected |
|---|---|---|
| **BX1** | The add-in from this change deployed for 2024 and Revit restarted (Project2 SAVED first - it was unsaved on 2026-10-03), then the job file's positive case | A stair edit mode starts inside Heron's TransactionGroup - **record whether it does**; that is D-112's open question |
| **BX2** | The positive case: Monolithic Stair, u, 1500 mm runs, entry north, first run east, start offset 1500, cut opening | 23 risers of 173.9 mm (12 + 11), two runs, one landing at the south end, an opening in the Level 2 slab over it; checked in 3D and a section, then Ctrl+Z ONCE removes all of it |
| **BX3** | The negative case: stair type `Spiral Feature Stair` | Refused naming the six loaded types; nothing made |
| **BX4** | The same values sent as a READ | Refused by the add-in (D-112), nothing run |
| **BX5** | Straight, entry south, first run centre, 1200 mm, start offset 300, no opening | One run of 23 risers along the hall, no landing |
