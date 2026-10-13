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
| **BX6** | A stair whose base level is NOT at 0 ([5b-445](../fragment-issues/section-5b-rows-401-425.md)), version 2: base Level 2, top Level 3, Monolithic Stair, u, run width 1100, entry south, first run west, start offset 0, cut opening, in a hall whose base is above 0 | Made, not refused: two runs and one landing, every riser at or under the type's maximum, the plan extent inside the hall, an opening in the Level 3 slab over it; then the levels swapped is refused with nothing made. **Run once 2026-10-13 on Revit 2020.2, on the owner's UNSAVED working model and KEPT** (stair 348438, 20 risers of 180 mm, opening 348538) - read back four minutes before Revit was closed WITHOUT SAVING, so the stair is gone; no run record and no saved copy, so not this check |
| **BX7** | The railings that come with the stair ([5b-455](../fragment-issues/section-5b-rows-401-425.md)): after BX2 (before its Ctrl+Z) and after BX5, `SELECT_BY_CATEGORY_NAME` categoryName=Railings in a 3D view, and each railing's type and host in Properties | Railings made by Revit with the stair, of the model's default stair railing type, hosted by the stair - **record how many and of what type**; on Revit 2020.2 (2026-10-13, Project1, rolled back) every stair had two: on a U one between the runs and one round the outside, on a straight one each side. One Ctrl+Z removes them with the stair |
| **BX8** | **Version 3 names the railings in its answer** ([5b-455](../fragment-issues/section-5b-rows-401-425.md)): BX2 and BX5 again on `CREATE_STAIRS` version 3, each first as a PREVIEW - which runs it and rolls all of it back (D-112) - and only then kept on the saved test copy; then BX3. Read the answer's `railings` and its findings, then count as BX7 does. Compiled for 2020 to 2027 on 2026-10-13; **never yet run in a Revit** | BX2 and BX5: `railings` holds as many ids as BX7 counts, and they are the same railings, each hosted by the stair - 2 on Revit 2020.2; one finding reads *"Revit added 2 railings of type '<the model's default stair railing type>' with the stair"*. BX3: refused as before, `railings` empty and no finding beginning "Revit added". If 2024 is not at hand, Project1 on Revit 2020.2 with `Cast-In-Place Stair`, where 5b-455 measured two railings of type `1100mm`, is the same check |
