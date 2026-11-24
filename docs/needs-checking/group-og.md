# Needs checking — Group OG

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group OG - a wall's top shaped to the roof above it by editing its profile, on Revit 2022 to 2024, doors and windows kept, a sketch that does not follow the roof: `SHAPE_WALL_TOPS_TO_ROOF` (2026-11-16) - SUPERSEDED 2026-11-17 by Group OE

**SUPERSEDED 2026-11-17 by [Group OE](group-oe.md) on main, where the card was proved and signed 2026-11-16.** Written when `main` was merged into pull request #871. This group was written for #871's own DRAFT of `SHAPE_WALL_TOPS_TO_ROOF` (FRG-ELE-170), declared 2022 to 2024, with a `redraw` input and a `keptInserts` read-back. Main built the same card under the same id for the same field report, and its version replaced this one in the merge: declared 2022 to 2027, no `redraw`, and **PROVEN** - Revit 2024, `Heron OE Roofs`, session 18772, signed by Ajmal PS on 2026-11-16 (Group OE's OE4 and OE5, and the card's `proof:` block). So no row here can be run as written against the card that is in the library now, and every one is struck as CLOSED, not passed: nothing below ran. What OG asked that OE did not run - a second call on a wall already shaped (OG4) and a release-2021 refusal in words (OG5) - is owed by OE's own rows where it applies (OE5's repeat and curved wall are NOT RUN there), not by this group. The file is kept as the record, and so is its job file, [`tools/jobs/wall-tops-to-roof-of-2026-11-16.yaml`](../../tools/jobs/wall-tops-to-roof-of-2026-11-16.yaml).

Asked for in the field: Sruthina's house shell on Revit 2024, model `villa 2`, 2026-11-11 - *"Attach all walls to the
roof."* - after the main roof was rebuilt; 25 walls stopped at the 2440 plate and left open gable triangles, and
`ATTACH_WALLS` is declared for 2025 on, so the modeller ran Attach Top/Base by hand once per roof
([Ajmalpshaik/Heron-AI#852](https://github.com/Ajmalpshaik/Heron-AI/issues/852), [#853](https://github.com/Ajmalpshaik/Heron-AI/issues/853)
item A1, [row 5b-1672](../fragment-issues/row-5b-1672.md)). **The owner's answer** to
[Q-91](../open-questions/tier-2.md), 2026-11-16, Ajmal PS - *"go with you suggetions"*: shape 2, a new tool on 2022, 2023
and 2024 that edits each wall's profile so its top follows the roof above it, says plainly on 2020 and 2021 that it
cannot, and says plainly that the top is a sketch that does NOT follow the roof when the roof changes.
**Searched first:** no card's C# named `CreateProfileSketch`, `CanHaveProfileSketch` or a `SketchEditScope` on a wall;
the duplicate check and the Matcher found nothing to reuse. **Look-alikes, kept separate:** `ATTACH_WALLS` (PROVEN, 2025
on) is Revit's real attach, a top that follows the roof when it moves; `CREATE_WALL` (PROVEN) makes walls and never
reshapes one; `SET_ELEMENT_LEVEL` gives a flat top at another height.

| What | Where | What it does |
|---|---|---|
| `SHAPE_WALL_TOPS_TO_ROOF` (MODIFY, Revit 2022-2024 only) | [`brain/fragments/shape-wall-tops-to-roof`](../../brain/fragments/shape-wall-tops-to-roof/fragment.yaml) | For each wall handed in: the roofs whose plan extent overlaps it are cut by a vertical line every 100 mm just inside each face, and the lowest underside of a roof that reaches above the wall's own top less 300 mm is its roof there; the lower face wins. Then `Wall.CreateProfileSketch` in one transaction (a `redraw` first takes the old one away with `RemoveProfileSketch`), and in a `SketchEditScope` on that sketch the rectangle's lines are deleted and the new outline drawn on the sketch's own plane - bottom and ends where they were, the top following the roof, a ridge put where the two slopes' lines meet. Read back: the profile's highest point, and every door, window and opening still in the wall. Left as they were, and named, before anything changes: not a wall, in a group, owned by somebody else, its top already attached, a profile Revit will not edit, a profile already edited and no `redraw`, a profile with openings drawn in it, no roof over it (`noRoof`), a floor between its top and the roof, a roof under 300 mm above its base, a top that already meets the roof. A Revit refusal once a wall is changed throws, and the add-in rolls the whole call back |

**Why it opens its own transactions.** A profile is edited only inside a `SketchEditScope`, and Revit's reference says
on `SketchEditScope.Start`, 2022 to 2024: *"SketchEditScope can only be started when there is no transaction active"*.
So the card declares `editScopeFailures`, [D-112](../DECISIONS.md)'s switch, as the stair cards and `CREATE_ASSEMBLY` do:
the add-in runs it inside its ONE named transaction group with none of its own open. **Read off RevitAPI.xml, not
measured.**

**Why 2022 to 2024 only.** `Wall.CanHaveProfileSketch`, `CreateProfileSketch`, `RemoveProfileSketch`, `SketchId`,
`Sketch.GetAllElements` and `SketchEditScope` are in 2022 to 2027 and not in 2020 or 2021 (`tools/api-surface --members`,
2026-11-16); compiled for 2021 on purpose, the snippet fails on exactly those names. From 2025 `ATTACH_WALLS` attaches
the wall for real, so this card is not offered there.

**What is done and what is not.** Compiles on 2022, 2023 and 2024 (`tools/check-fragments-compile.py`, 2026-11-16, a
Linux container). **Nothing has run in Revit.** DRAFT. **Only Revit can settle:** that a `SketchEditScope` starts and
commits inside the add-in's open group, as a `StairsEditScope` does (OG3); that the plane `CreateProfileSketch` gives
is upright along the wall, which the card checks and refuses otherwise; that a door keeps its host through the new
profile; whether Revit snaps or rejects an outline with many short lines along a curved roof. **Not done:** a roof that
slopes across the wall (an eave wall) gives a flat top at the lower face, leaving a small wedge Revit's Attach would
fill; a wall standing on both sides of a roof's edge keeps its height where no roof is over it, with one sloped line
of 100 mm at the edge.

**Routing.** Measured 2026-11-16 on a scratch store of this change (`heron_brain.lookup`, model backend, no re-ranker
installed): *"attach all walls to the roof"* and *"attach the tops of these walls to the roof"* reach
`SHAPE_WALL_TOPS_TO_ROOF` by identity on Revit 2022 and 2024, and `ATTACH_WALLS` on 2025 and 2026 with no other-release
line; on 2021 the answer opens *"NOT ON REVIT 2021. ATTACH_WALLS matches these words best ... tell them plainly that
Heron cannot do this on Revit 2021 and that it is done by hand in Revit."* `tools/check-routing.py` exits 0. On 2025
and 2026 *"edit the wall profile to follow the gable"* was answered *"NOT ON REVIT 2025 ... done by hand"* though
`ATTACH_WALLS` does that job there - [row 5b-1823](../fragment-issues/row-5b-1823.md). Repaired in code the same day,
in the lookup and not in either card: `ATTACH_WALLS` is offered there in its place, under *"ON REVIT 2025 THIS IS
ATTACH_WALLS"*; on 2020 and 2021, where neither card is declared, the line stands.

**The model** - `of-gable`, a scratch project from Revit 2024's default architectural template (metric), saved under
`%TEMP%\heron-work`, never a working model. Every write here is ROLLED BACK unless the modeller keeps it. The job file
is [`tools/jobs/wall-tops-to-roof-of-2026-11-16.yaml`](../../tools/jobs/wall-tops-to-roof-of-2026-11-16.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`, `--session <process
id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| ~~**OG1**~~ | Arrange, Revit 2024. `of-gable`: on Level 1, four `Generic - 200mm` walls on their centrelines from… — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | A section across x = 0 shows the open triangle over the west wall; the fifth wall has… |
| ~~**OG2**~~ | D-30 positive and negative, THE NEGATIVE FIRST. The job file, `--write`, rolled back Struck… — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | Negative, the fifth wall: shaped 0, `noRoof` naming it, nothing changed. |
| ~~**OG3**~~ | ~~*The edit mode inside Heron's group.* The positive's answer, and Revit's undo list after it rolled back~~ **Struck 2026-11-17:** CLOSED, not passed - superseded 2026-11-17 by [Group OE](group-oe.md) on main, where the card was proved and signed 2026-11-16. | ~~No *"Revit will not open the profile ... for editing"*, no *"transaction is open around this fragment (D-112)"*; after the rollback, Edit Profile on the west wall shows the plain rectangle again. **Either answer is Revit's: record it** - a refusal here means the card's D-112 route does not reach a sketch, and it is changed before it is proved~~ |
| ~~**OG4**~~ | The refusals. On `of-gable`: the positive kept once through `try-once` and the same call again… — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | The second call shaped 0, "its profile is already edited ... |
| ~~**OG5**~~ | Release 2021, refused in words. A chat connected to Revit 2021 (or 2020) asks "attach all walls to… — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | The lookup's answer opens "NOT ON REVIT 2021" and says Heron cannot do this on Revit 2021… |
| ~~**OG6**~~ | Kept once, and the second route. Through `try-once` on the modeller's yes, the west wall Struck… — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | ONE entry in Revit's undo list; a section through the west wall: the top runs up under… |
