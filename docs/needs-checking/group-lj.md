# Needs checking — Group LJ

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group LJ - the path of travel lines in a floor plan and their lengths: `REPORT_PATHS_OF_TRAVEL` (2026-11-02)

Asked for in the modeller's words: *"what is the travel distance from room 204 to the exit"* and *"list the path
of travel lines in this plan and their lengths"* - Revit's Analyze > Route Analysis > Path of Travel - beside
*"draw the path of travel from this room to the nearest exit"* and *"show the egress route from each room on
Level 2 to the stair door"* ([Group LK](group-lk.md)). No card read or drew one: a grep of every fragment's C#
that day for `PathOfTravel` found none. `FIND_NEAREST_ELEMENTS` ranks by straight line or box gap and says on
its own card that the nearest exit may be through a wall; `MEASURE_DISTANCE` measures between two elements'
boxes; `PROBE_AROUND_ELEMENTS` casts rays; `DIMENSION_ROOMS` and `MEASURE_ROOM_DIMENSIONS` are room sizes -
look-alikes, not duplicates.

| What | Where | What it does |
|---|---|---|
| `REPORT_PATHS_OF_TRAVEL` (READ) | [`brain/fragments/report-paths-of-travel`](../../brain/fragments/report-paths-of-travel/fragment.yaml) | Lists the **path of travel lines in one floor plan** - the plan named, or **the view in front** - each with its id, its **length in metres measured along its own lines** (never a straight line between its ends), its start and end in mm, the **room at each end** in the plan's phase, the **door within 1500 mm of its end** by Mark, and its waypoints. `room` narrows to the paths starting or ending in one room, by number or name. **Any view that is not a floor plan, and any template, is refused by name.** A plan with none is said in words and names `CREATE_PATHS_OF_TRAVEL`. **A measured length, never a compliance verdict** (`pathCount`, `pathIds`, `report`, `refused`). Opens no transaction |

**One of a pair.** `CREATE_PATHS_OF_TRAVEL` ([Group LK](group-lk.md)) draws them; every question about which
paths a plan has and how long they are is answered here, so none reaches the write.

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-02, a Linux container). Read off the shipped assemblies with
`tools/api-surface --members`: `PathOfTravel` with `GetCurves`, `GetWaypoints`, `PathStart` and `PathEnd` is in
the 2020 assembly and unchanged to 2027. **Nothing here has run in Revit.** The card is DRAFT. **Only Revit can
settle** whether a path's start and end sit on the level, so the room lookup a foot above them finds the room,
and whether the length summed from `GetCurves` matches the Length Revit shows - LJ2 and LJ3 check them.

**Routing.** See [Group LK](group-lk.md), which measured both cards together.

**The model - `travel-ip`, a scratch project** new from the metric architectural template, saved under
`%TEMP%\heron-work`, never a working model. The job file is
[`tools/jobs/report-paths-of-travel-lj-2026-11-02.yaml`](../../tools/jobs/report-paths-of-travel-lj-2026-11-02.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **LJ1** | *Arrange.* On "Level 1" a 20000 x 10000 mm box of Generic - 200mm walls, an interior wall across it at x = 10000 with a 1000 mm gap at its north end, rooms "201 Office" (west) and "202 Store" (east), a door Mark "D-EXIT" in the east wall; with Revit's own Analyze > Path of Travel, one path from each room's middle to the door. "Level 2" with none. Save | Two paths in "Level 1", none in "Level 2" |
| **LJ2** | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file | Negative, "Level 2": `pathCount` 0, *"Level 2" has no path of travel lines*, no row of Level 1. Positive, "Level 1": `pathCount` 2, each row's metres, start and end in mm, room 201 or 202 at its start, door "D-EXIT" at its end; the 201 path longer than 10 m |
| **LJ3** | *Narrowing, refusals, the second route.* `room` "201", then "Office", then "999"; then `{3D}`, a ceiling plan and a sheet; then each path's Length in the Properties palette | One path for 201 and for Office; none for 999, said in words; each other view refused by name; every row's metres equal to the palette's Length |
