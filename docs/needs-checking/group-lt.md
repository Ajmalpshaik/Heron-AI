# Needs checking — Group LT

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group LT - a roof by extrusion, its profile sketched on a named reference plane or along a grid and extruded between a start and an end: `CREATE_EXTRUSION_ROOF` (2026-11-02)

Asked for in the modeller's words: *"make an extruded roof along grid A with this profile"*, *"add a curved
extrusion roof from 0 to 12000 mm, rise 2000"*, *"make a barrel roof over the entrance"* and *"add a gable
extrusion roof on reference plane North Face"* - Revit's Architecture > Roof > Roof by Extrusion.
heron-duplicate-checker's method, run before writing: `brain/heron_merge.py` found no shared signature with a
shared capability, and the duplicate check and the Matcher found nothing equal. No fragment's C# called
`NewExtrusionRoof`. `CREATE_FLOOR` (DRAFT, version 3) makes a **footprint** roof from a closed outline on plan,
left flat - a **look-alike**: a roof by extrusion is an open profile in a vertical plane with a start and an end,
and widening that card would make one input mean two shapes and add three inputs its floors, ceilings and
footprint roofs must refuse. `SLOPE_ROOF_EDGES` (PROVEN) slopes a footprint roof and refuses an extrusion roof by
name. `CREATE_FAMILY_FORMS` and `CREATE_CONCEPTUAL_FORM` extrude forms in a family; this one refuses a family.

| What | Where | What it does |
|---|---|---|
| `CREATE_EXTRUSION_ROOF` (MODIFY) | [`brain/fragments/create-extrusion-roof`](../../brain/fragments/create-extrusion-roof/fragment.yaml) | Makes a roof by extrusion on a **named** level, of a **named** roof type, its profile sketched on a **named** vertical reference plane - or along a **named** straight grid, where a reference plane is drawn in a floor plan of the level and handed back. The profile is an **open** line of points and arcs in millimetres, along the plane and up from the level; the extents are millimetres from the plane, positive east (north for a plane facing north-south). With a level, type or plane left out or unknown, nothing is made and what the project has is listed. A closed profile, two points in one place and an arc in a line are refused by name; a profile Revit will not make is refused with Revit's own words and nothing is kept. The roof's type, level, Extrusion Start and End, edge count and top height are **read back** off Revit |

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-02, a Linux container). Every Revit member it calls was read off the
shipped assemblies with `tools/api-surface --members`, the same signature 2020 to 2027 - the list is in the card's
`compatibility-note`; `ExtrusionRoof` has no Extrusion Start or End property on any release, so both are read off
their parameters. **Nothing here has run in Revit.** The card is DRAFT. **Only Revit can settle:** which way Revit
measures Extrusion Start and End along the plane's normal, and whether it keeps them in the order given - the
read-back accepts either order and throws on anything else, so a wrong guess shows as a refusal, never as a roof on
the wrong side (LT3); which way a reference plane drawn along a grid faces (LT3); and what Revit says for a profile
that crosses itself (LT4). **Not done:** a profile drawn on a level's or a section's own work plane, a roof by
extrusion cut back to walls or a footprint roof (Revit's Join/Unjoin Roof), a sloped glazing type's curtain grid,
and editing the profile of a roof already made.

**Routing.** `tools/check-routing.py` on its own scratch store, on a copy of the tree without the card and on this
change, on both backends (`model`, and lexical with the embedding libraries hidden): no question answered by a
write; claimed in a routing table and not reached 67 before and 67 after. Through the host's lookup on Revit 2024
and 2020 the four sentences moved to this card by identity - before, they reached `CREATE_CONCEPTUAL_FORM`,
`SLOPE_ROOF_EDGES`, `LOCK_FORM_TO_PLANES`, `CREATE_FLOOR` and `CREATE_PROJECT_REFERENCE_PLANES` by ranking - and
every other sentence checked - each one `CREATE_FLOOR`, `SLOPE_ROOF_EDGES`, `CREATE_EDGE_SWEEPS` and
`CREATE_PROJECT_REFERENCE_PLANES` declare or claim for a roof, a slope or a plane - answered the same card before
and after. On the keyword route alone this card now heads *"create a roof"*, *"make the roof on this level"*
(`CREATE_FLOOR`'s), *"extrude this shape on the reference plane"*, *"make an extrusion with this profile"*
(`CREATE_FAMILY_FORMS'`) and *"it is missing from the plan"* (`DIAGNOSE_VISIBILITY`'s), each already lost on that
route and each answered by identity at its own card.

**The model** - "extroof-ip", a scratch project from Revit's metric Architectural template, saved under
`%TEMP%\heron-work`, never a working model. Every write here is ROLLED BACK unless the modeller keeps it. The job
file is [`tools/jobs/create-extrusion-roof-lt-2026-11-02.yaml`](../../tools/jobs/create-extrusion-roof-lt-2026-11-02.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`, `--session
<process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **LT1** | *Arrange.* "extroof-ip" from the metric Architectural template; Level 1 at 0, Level 2 at 4000 with its floor plan; grids A from 0,0 to 0,20000 and B from 6000,0 to 6000,20000; in "Level 2" a reference plane from 0,20000 to 12000,20000 named *North Face*; a Basic Roof type named *Generic - 400mm* (duplicate and rename one if the template has none). Save | The plane's Name in Properties; the roof type in the Project Browser |
| **LT2** | *D-30 positive and negative, THE NEGATIVE FIRST.* The job file's job, `--write`, rolled back | Negative, the closed profile: refused *"The profile ends where it starts ... OPEN line ... NOTHING WAS MADE."*, `roof` and `planeDrawn` invalid. Positive: `roof` 1 id, `planeDrawn` 1 id, the report naming *Basic Roof: Generic - 400mm*, *Level 2* and *extruded from 0 to 6000 mm east of the plane* |
| **LT3** | *What Revit made.* LT2's positive, kept through `try-once` on the modeller's yes | A barrel running north along grid A for 12000, its profile line 2000 above Level 2 at its middle, reaching from grid A to 6000 east of it - **measured** with dimensions; Properties' Extrusion Start and End as the report says. **A roof west of grid A, or a refusal saying the extents did not read back, is a finding for the author, never a pass** |
| **LT4** | *Revit's refusal.* `validate create-extrusion-roof --write` with the job's values and profile *0,0; 6000,2000; 6000,0; 0,2000*, rolled back | The call throws *"Revit refused the roof profile on ... NOTHING from this call was kept."* with Revit's words; no roof and no reference plane after |
| **LT5** | *The named plane and the lists.* `validate create-extrusion-roof --write` with plane *North Face*, profile *0,0; 6000,2000; 12000,0*, extents -3000 and 0; then with roof type left out; then with plane *the entrance*; each rolled back | A gable whose ridge runs north-south at x 6000, from 3000 south of North Face up to it, `planeDrawn` invalid; the roof types listed; the planes and grids listed, nothing made |
| **LT6** | *The second route.* Revit's own Roof by Extrusion by hand on North Face, Level 2, the same gable profile, Extrusion Start -3000 and End 0; compared with LT5's roof kept through `try-once` | The same ridge, eaves and length, measured; Undo takes the Heron roof back as one entry |
