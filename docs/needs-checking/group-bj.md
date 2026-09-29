# Needs checking — Group BJ

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-29 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group BJ - D-59's fifth batch: four ray and reach checks see linked ceilings, walls and structure when asked (2026-09-29)

**The same shape as [Group BI](group-bi.md)** - `includeLinks` (absent means this model only), `linksSearched`
and `linkedMatches`. A tool's pass/fail lists stay this model's answer and what the links show is TEXT
([row 75](../FRAGMENT-ISSUES.md)). Only model elements are read from a link, never its views or sheets. Why
these four, and what was decided: [row 5b-259](../FRAGMENT-ISSUES.md).

**All four compile on all eight releases and none has been run.** All four were `PROVEN` and went back to
`DRAFT` as version 2 ([D-30](../DECISIONS.md)).

**BE1 must pass first.** The model is `Snowdon-scratch_ajmal.al`; the negative case for every row is a model
with no links, checked in Manage Links first. **Three of the four cast rays in a 3D view, and a ray sees a
link only where that view shows it** - check the link is visible there before reading a zero.

| ID | Do this | Pass looks like |
|---|---|---|
| **BJ1** | `check-surface-fit` on air terminals under a ceiling that is only in the link, direction up, `includeLinks=true` | `unsafeToMove` and `clean` unchanged; each terminal's line reads `sits flat` or names its verdict against the linked ceiling. **Negative:** a terminal half off the ceiling's edge reads OVERHANGING |
| **BJ2** | `check-valve-accessibility` on valves above a linked ceiling, `envelope=300`, `includeLinks=true` | `needAccessPanel` unchanged; each valve's line names the linked ceiling and reads `NEEDS AN ACCESS PANEL`. **Negative:** a valve whose envelope overlaps only a linked room gives no ROOM line |
| **BJ3** | `probe-around-elements` on an AHU 400 mm from a linked wall, the four horizontal directions, `maxDistance=1000`, `includeLinks=true` | `hits` unchanged; one linked line for the direction facing the wall, with the distance in mm matching a plan dimension. **Negative:** hide the link in that 3D view - no linked line |
| **BJ4** | `check-minimum-clearance` with a duct 30 mm from a linked wall, `defaultClearance=50`, `rules="Walls=100"`, `includeLinks=true` | `tooClose` unchanged; the duct's line reads about 30 mm, needs 100 mm (Walls). **Negative:** a duct 500 mm clear reads `0 pair(s) too close` with a checked count |

**What cannot be answered here.** Every row needs a Revit. What was checked without one: the four compile on
2020 to 2027, and every suite passes.
