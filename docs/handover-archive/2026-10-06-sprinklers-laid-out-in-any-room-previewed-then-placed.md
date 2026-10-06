# Session note — SPRINKLERS LAID OUT IN ANY ROOM, PREVIEWED, THEN PLACED

> **Session note** from 2026-10-06, written straight into this folder, one file per sitting, as
> [`README.md`](README.md) asks. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**. A note records what was true on its own day.

---

### 2026-10-06 — SPRINKLERS LAID OUT IN ANY ROOM, PREVIEWED, THEN PLACED

The owner asked whether Heron could place sprinklers in a room by itself - *"in the companion app I can
select and it will place any shape"*. Built the same way as the Sprinkler panel: the design
([47](../47-sprinkler-layout-from-the-model.md)), the [plan](../work-notes/plans/sprinkler-layout-2026-10-06.md),
an independent review of the plan against the code (19 findings, the engine's measured in Python), the
fixes, then the build.

**What it does.** Select Spaces or Rooms, say *"place sprinklers in these rooms"*. A new read gives each
room's outline, holes, level, ceilings and the heads already there; the fire engine's new
`sprinkler_layout_room` lays each out on the fewest heads of a regular grid that pass the very measure
`sprinkler_spacing` uses; the Companion's new Sprinkler Layout panel shows every room's plan, OK or
FAIL; Apply places one level through the PROVEN `PLACE_FAMILY_INSTANCES` and reads the heads back.

**The review's best catches.**

- Revit's room tests check height, so a head at the ceiling reads as outside its room - the layout's
  read now finds heads in plan.
- A second press of Apply would have placed a second grid - a level is now used once.
- `Family : Type` would never have matched; the add-in wants `Family: Type`.
- A farthest-point gate would have rejected the rectangle layout's own answer.
- A head on a re-entrant wall passes the existing check.

**Not run in Revit.** Group CG has the runs. The question that matters most: does a head sent at a z
land at that height, or one level's elevation off (5b-340)? The read-back says CHECK if it does.

**Recorded, not fixed:** 5b-338 (the network read's Spaces by volume), 5b-339 (a head on a wall passes),
5b-340 (two fragments describe a level's elevation differently).

**Also this sitting:** GitHub Actions stopped starting jobs at about 16:09 - every job failed in three
seconds with no runner. The likely cause is the private repository's free minutes, used up this month;
the owner was told how to check, and that going public is the free fix. Every gate and suite was run
locally instead.
