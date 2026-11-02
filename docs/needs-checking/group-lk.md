# Needs checking — Group LK

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group LK - path of travel lines drawn from rooms or points to a named exit: `CREATE_PATHS_OF_TRAVEL` (2026-11-02)

Asked for in the modeller's words: *"draw the path of travel from this room to the nearest exit"* and *"show the
egress route from each room on Level 2 to the stair door"* - Revit's Analyze > Route Analysis > Path of Travel.
No card drew one: a grep of every fragment's C# that day for `PathOfTravel` found none. `FIND_NEAREST_ELEMENTS`
ranks by straight line and draws nothing; `CREATE_ROOM_SEPARATION_LINES` and `DRAW_FAMILY_DETAIL_LINES` draw
along curves they are given, where here Revit finds the route - look-alikes, not duplicates. *"what is the travel
distance from room 204 to the exit"* is `REPORT_PATHS_OF_TRAVEL`'s ([Group LJ](group-lj.md)).

| What | Where | What it does |
|---|---|---|
| `CREATE_PATHS_OF_TRAVEL` (MODIFY) | [`brain/fragments/create-paths-of-travel`](../../brain/fragments/create-paths-of-travel/fragment.yaml) | Draws **one path of travel per start to ONE exit** in a floor plan - the plan named, or **the view in front**. Starts: `rooms` by number or name separated by ";", or "all" for every placed room on the plan's level in its phase, each from its room location point; and/or `fromPoints` "x,y; x,y" in **mm**. Exit: `toDoor`, a door on the level by **Mark**, ended at its location point, or `toPoint` "x,y" in **mm** - **never guessed**: neither, both, a Mark no door has or one two doors share is refused with the level's doors listed. **Any view that is not a floor plan, and any template, is refused by name** before anything is drawn. A start Revit finds no path from is named in `notFound` with **Revit's own status**, the rest still drawn. A path already there from the same start to the same end (10 mm) is named and **not drawn twice**. **Never deletes.** Read back after a regenerate: each path's id, **length in metres** along its lines, start and end in mm (`created`, `notFound`, `report`, `refused`). A measured length, never a compliance verdict |

**One of a pair.** `REPORT_PATHS_OF_TRAVEL` ([Group LJ](group-lj.md)) answers every question about the paths a
plan has, so none reaches this write.

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-02, a Linux container). Read off the shipped assemblies with
`tools/api-surface --members`: `PathOfTravel.Create(View, XYZ, XYZ, out PathOfTravelCalculationStatus)`,
`GetCurves`, `PathStart` and `PathEnd` are in the 2020 assembly and unchanged to 2027;
`PathOfTravelCalculationStatus` gained two values in 2021, and the status is reported by name. **Nothing here
has run in Revit.** The card is DRAFT. **Only Revit can settle** whether a path can end at a door's location
point - on the wall's line inside the opening - or Revit wants the end clear of the wall; whether a room
location point is always a start Revit accepts; and what `Create` returns or throws for a start it cannot reach
- LK2 to LK4 check them. One exit per run in version 1: choosing the nearest of several named exits is not built.

**Routing.** Measured through `heron_retrieve.find` on a fresh scratch store (`model` backend, no re-ranker; Revit 2020 and 2024). The four sentences reach their cards by identity - before, `FIND_NEAREST_ELEMENTS`, `DRAW_FAMILY_DETAIL_LINES` and `CREATE_STAIRS`. Probes nobody declared - *how long is the path of travel*, *what is the egress distance from this room*, *how far is it to the exit* - reach `REPORT_PATHS_OF_TRAVEL`; *draw a path of travel*, *draw the egress route* reach this card; *add an egress path* reaches the read, which draws nothing and names this card; *find the nearest exit* and *which exit is closest to this room* still reach `FIND_NEAREST_ELEMENTS`. `tools/check-routing.py` (scratch store, before and after): no question answered by a write, no routing row unreached, three new contests on the keyword route alone, each answered by identity through `find` and written in both cards - *find the nearest exit* (`FIND_NEAREST_ELEMENTS`), *create the rooms on this floor* (`PLACE_ROOMS`) and *what files travel with this model* (`REPORT_EXTERNAL_REFERENCES`). No sentence `find` answered changed card.

**The model - `travel-ip`**, as [Group LJ](group-lj.md) arranges it but with **no path drawn**, and a second
door Mark "D-2" in the interior wall's gap and a "{3D}" view. Every write here is ROLLED BACK unless the
modeller keeps it. The job file is
[`tools/jobs/create-paths-of-travel-lk-2026-11-02.yaml`](../../tools/jobs/create-paths-of-travel-lk-2026-11-02.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **LK1** | *Arrange.* `travel-ip` as LJ1 with no path of travel, door "D-2" in the gap, a "{3D}" view. Save | No path in any view |
| **LK2** | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file, `--write`, rolled back | Negative, `{3D}`: nothing drawn, refused *"{3D}" is a 3D view. Revit draws paths of travel in floor plans only*. Positive, rooms "201; 202" to "D-EXIT" in "Level 1": `created` 2, `notFound` empty, each row's metres; the 201 path goes round the interior wall's end |
| **LK3** | *The exit never guessed.* No exit; `toDoor` "D-99"; both `toDoor` and `toPoint`; `rooms` "999" | Each refused, nothing drawn; the first two list "D-EXIT" and "D-2" with their points; the last lists rooms 201 and 202 |
| **LK4** | *Points, twins, Revit's status, the second route.* `fromPoints` "2000,2000; 18000,8000" to "D-EXIT"; LK2's positive KEPT once with `try-once` in a scratch copy, then run again; `fromPoints` "5000,5000" to `toPoint` "5005,5000"; Revit's own Path of Travel drawn by hand beside one, and one Ctrl+Z | Two paths from the points; the second run draws none and names both as already there; the too-close pair named in `notFound` with Revit's status; the hand-drawn path's Length the same; one undo takes every path the run drew |
