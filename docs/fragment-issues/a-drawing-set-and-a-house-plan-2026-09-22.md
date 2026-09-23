# Fragment issues — A drawing set and a house plan, 2026-09-22

> One section of [the register](../FRAGMENT-ISSUES.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## A drawing set and a house plan, 2026-09-22 — seven things measured against Project1 and Project4

Ajmal asked for a drawing set in `Project1` — twelve levels, twelve sheets, thirty
plan views, the Mech plans placed, the titleblock names filled in, a sheet list —
and then for the ground floor of a small house, read off an image, drawn into a new
`Project4`. Both Revit 2024, session 37184. Everything below was observed in front
of the model.

### 1. A DOOR PLACED THROUGH `place-family-instances` CAME OUT UNHOSTED, AND THE ANSWER SAID SUCCESS

`symbol=0864 x 2134mm` (M_Single-Flush), `points=3550,3000,0`, `level=Level 1` — a
point on the centreline of a 4000 mm long, 3000 mm high `Generic - 140mm Masonry`
wall. It answered `failed 0`, `placed 1 item(s) [352528]`.

**The door cut nothing.** The evidence is the wall's own area, read in internal
square feet with `SELECT_BY_NUMERIC_PARAMETER` on Walls: **0 of 14** had `Area`
between 104 and 120, where a 12 m² wall less a door opening would sit, and **2 of
14** — both 4 m × 3 m walls — had `Area` between 122 and 136, which is full. It was
deleted: `DELETE_ELEMENTS` answered `deleted 1 item(s) [352528]`, `alsoWent 0`.

Two things follow, neither fixed here. `place-family-instances` should refuse a
wall-hosted family by name rather than make an orphan that looks right in plan. And
the route meant for doors, `place-hosted-family` (DRAFT), takes `host` as a
caller-typed `Element`, which `brain/heron_fragment.py` resolves to an element TYPE
by name, never to one particular wall — so it was not tried. **On this date Heron has
no working route that puts a door or a window into a wall.**

`SELECT_BY_PARAMETER_VALUE` with `parameterName=Host Id` answered `1 of 1 scanned
element(s) match Host Id equals ''` for the orphan. Whether a hosted door reads any
differently was not established, so that filter proves nothing about hosting either
way. The host wall's area does.

### 2. `find-views` MATCHES A SUBSTRING, SO `1 - Mech` ALSO FINDS `Level 11 - Mech`

`viewType=FloorPlan`, `nameContains=2 - Mech` answered `2 item(s) [2 - Mech, Level
12 - Mech]`, and `nameContains=1 - Mech` answered `[1 - Mech, Level 11 - Mech]`.
There is no exact mode. What kept the sheet set right was ORDER: `Level 11 - Mech`
and `Level 12 - Mech` were already on A111 and A112, so `place-views-on-sheet`
skipped them (`notPlaced 1 item(s) [926315]`, then `[926286]`) and A101 and A102
each received only the view intended. Run the other way round, A101 would have been
given two plans and A111 refused its own. **A name that is the tail of another name
cannot be selected on its own.**

### 3. `read-element-parameters` ANSWERS WITH A COUNT, NOT THE VALUES

`parameterName=Discipline` on three floor plans answered `values 3 entry(ies)` and
``readValue Func`2``. The values never reach the caller, so the read half of
`write-element-parameters` cannot be seen through `revit_change` at all. The
read-back that worked was a filter, `SELECT_BY_PARAMETER_VALUE` with
`matchMode=equals`: `12 of 12 scanned element(s) match Drawn By equals 'Ajmal PS'`,
and the same for Checked By, Designed By and Approved By. The same shape as a list
cut to three items — the count is honest and the contents are not there.

### 4. THE ANSWER TO A SIX-WALL `create-wall` WAS LOST, AND THE WALLS WERE MADE

`points=0,5000,0; 0,12000,0; 7000,12000,0; 7000,3000,0; 3000,3000,0; 3000,5000,0;
0,5000,0`, `height=3000`, `wallType=Generic - 140mm Masonry` answered *"The request
reached Revit but the answer was lost, so Heron cannot tell whether it ran."* Heron
did not retry, and that was right: `revit_families` then read `Basic Wall ... 6
placed` and `revit_health` read 3,209 elements where it had read 3,203. Repeating it
would have doubled the outer wall of the house. The two- and three-point runs that
followed answered normally. Not explained; the length of the run is the only thing
that set this call apart.

### 5. A NEW PLAN TAKES ITS VIEW TYPE'S DEFAULT TEMPLATE, AND A CEILING PLAN HERE TOOK NONE

In `Project1`, `create-plan-view` with `planKind=floor` made a plan that
`SELECT_VIEW_TEMPLATES` then reported under `Mechanical Plan` — *"1 view(s) follow
it; it is the template ne..."* — and the Project Browser put it under Mechanical >
HVAC. `planKind=ceiling` made a plan with no template, which the browser put under
**Coordination > ???**, a branch that had not existed before. Neither answer says
which template, if any, the new view received. `APPLY_VIEW_TEMPLATE` with
`Mechanical Ceiling` and `Plumbing Plan` settled all twenty (`applied 9`,
`refused 0`, twice), after which each template read `10 view(s) follow it`.

### 6. NO CAPABILITY IS NAMED FOR RENAMING A SHEET

`heron_lookup` for *"change the name of sheet A101 to Level 01"* answered
`SET_SHEET_TITLE_BLOCK`, and *"rename a sheet"* answered `RENAME_FAMILY`, a coin toss
with `RENAME_ELEMENTS`. The route that worked was `FIND_SHEETS` with
`numberOrNameContains=A101`, then `RENAME_ELEMENTS` with `find=Unnamed` and
`replaceWith=Level 01` — `renamed 1`, read back by `revit_sheets` as `A101 Level 01`.

### 7. NOT A DEFECT: A WRITE CONSUMES WHAT IT WAS HANDED

Four `WRITE_ELEMENT_PARAMETERS` calls were sent together after one `FIND_SHEETS`,
each with `expect_from=find-sheets where numberOrNameContains=A1`. The first wrote
12. The other three answered *"This request expects values left by 'find-sheets', but
what is carried was left by 'write-element-parameters' ... NOTHING WAS BOUND and no
fragment ran."* Correct and safe. **One find per write.**

---
