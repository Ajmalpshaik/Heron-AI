# Needs checking — Group LY

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group LY - Modify > Wall Joins - butt, miter or square off where the walls handed in meet, each corner read back - the read of the join type at each wall end, and structural framing's start / end extension and join cutback by the end: `SET_WALL_JOIN_TYPE`, `REPORT_WALL_JOIN_TYPE`, `SET_FRAMING_END_EXTENSION` (2026-11-02)

Asked for in the modeller's words: *"make this wall corner a miter join"*, *"change these wall joins to butt"*,
*"square off the wall join here"*, *"what join type is at this wall corner"*, *"set the beam end extension to 150
mm"* and *"cut back these beams 50 mm at the column"*. No card did it: of every fragment's C# none called
`LocationCurve.get_JoinType` or `set_JoinType`, or named `START_EXTENSION`, `END_EXTENSION`,
`START_JOIN_CUTBACK` or `END_JOIN_CUTBACK`. heron-duplicate-checker's method, run before writing:
`brain/heron_merge.py` found no shared signature with a shared capability; the duplicate check (`0 already
here`) and the Matcher (`0 match(es), 0 partial`) found nothing equal for any of the three. **Look-alikes, kept
separate:** `DISALLOW_JOIN` (PROVEN) turns joining at a wall or beam end off or on - these change the shape of a
join that is there and the length at a beam end, and never turn joining on or off; `JOIN_GEOMETRY` (DRAFT) is
Join Geometry between elements; `REPORT_JOINED_ELEMENTS` (PROVEN) reads Join Geometry joins, where a wall
corner does not show (its own proof says so), and widening its code would put its proof out of date;
`COPE_FRAMING` (PROVEN) is the coping cut round a column, a cut in the beam's shape rather than its length;
`WRITE_ELEMENT_PARAMETERS` (PROVEN) writes one parameter named exactly and cannot choose the end from what the
beam meets; `TRIM_EXTEND_ELEMENTS` (PROVEN) moves two ends to meet.

| What | Where | What it does |
|---|---|---|
| `SET_WALL_JOIN_TYPE` (MODIFY, Revit 2020-2027) | [`brain/fragments/set-wall-join-type`](../../brain/fragments/set-wall-join-type/fragment.yaml) | Sets the join where the walls handed in meet to butt, miter or square off - `joinType`, no default. **The end is never guessed:** an end is changed only where the walls at its join include another wall handed in, so the modeller selects the walls on both sides of the corner; a wall meeting none of the others is refused by name, one wall alone before anything changes. An end with joining disallowed is refused in words and left for `DISALLOW_JOIN`. Each corner is changed once, Revit refusing a type at a join is reported with Revit's message, and every corner is read back into `corners` as before -> after |
| `REPORT_WALL_JOIN_TYPE` (READ, Revit 2020-2027) | [`brain/fragments/report-wall-join-type`](../../brain/fragments/report-wall-join-type/fragment.yaml) | For each wall handed in, what meets it at each end and the join type there, one row per corner; free ends counted in `freeEnds`, an end with joining disallowed said so |
| `SET_FRAMING_END_EXTENSION` (MODIFY, Revit 2020-2027) | [`brain/fragments/set-framing-end-extension`](../../brain/fragments/set-framing-end-extension/fragment.yaml) | Sets Start / End Extension (`setting` extension) or Start / End Join Cutback (cutback) of the beams and braces handed in, `lengthMm` turned into feet by / 304.8, at `whichEnd` start, end, both, or joined - each end Revit has joined to another member, read off the beam's location line, never guessed. A parameter Revit has greyed out at that end is refused in words; anything not structural framing by name; every end read back into `ends` as before -> after in millimetres |

**What is done and what is not.** All three compile on every release 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-02, a Linux container). Every Revit member they call was read off the
reference assemblies of all eight releases with `tools/api-surface --members` - the lists are in each card's
`compatibility-note`; Revit's *Butt* is `JoinType.Abut`. **Nothing here has run in Revit.** All three cards are
DRAFT. **Only Revit can settle:** that `ElementsAtJoin` at an L names the other wall and not the wall itself
(LY2, LY3); that a miter at a T, or a square off at a right angle, is refused by Revit rather than accepted
(LY5); that `ElementsAtJoin` answers for a beam end joined to a column (LY4); whether Join Cutback is read-only
at an end that is not joined (LY5). **Not done:** a join type for one wall end named by the modeller with no
second wall; a beam end set to a picked face (`StructuralFramingUtils.SetEndReference`, present on every
release, nobody asked for it).

**Routing.** `heron_retrieve.find` on fresh scratch stores of the tree at `c65fd64` and of this change, over the
six sentences and every live card's utterance and routing row with *join*, *miter*, *butt*, *square off*,
*corner*, *extension*, *cutback*, *cope* or *beam end* in it (79), on Revit 2024, 2027 and none named, with and
without the host's question flag. The five writes reach their card without the flag (before: `CREATE_WALL`,
`REPORT_JOINED_ELEMENTS`, `DISALLOW_JOIN`, `CREATE_BEAM_SYSTEM`, `COPE_FRAMING`); *"what join type is at this
wall corner"* reaches `REPORT_WALL_JOIN_TYPE` with and without it (before: `CREATE_WALL`, and
`REPORT_JOINED_ELEMENTS` with the flag). Four sentences the new words moved off the card a routing table names,
and one that already missed it, are declared on that card, card text only - [row 5b-1419](../fragment-issues/section-5b-rows-401-425.md).
`tools/check-routing.py` on scratch stores: no question answered by a write; claimed and not reached 68 before,
67 after - the one already missed, now declared. With the question flag only, write sentences on joins moved between reads to
`REPORT_WALL_JOIN_TYPE` - recorded in row 5b-1419, not tuned.

**The model** - a scratch project from Revit's default structural template, saved under `%TEMP%\heron-work`,
never a working model. Every write here is ROLLED BACK unless the modeller keeps it. The job file is
[`tools/jobs/wall-joins-framing-ends-ly-2026-11-02.yaml`](../../tools/jobs/wall-joins-framing-ends-ly-2026-11-02.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`, `--session
<process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **LY1** | *Arrange.* A new project from the default structural template. On Level 1: walls A and B meeting in an L (Revit's default butt join); walls C and D parallel and a metre apart, touching nothing; two structural columns with a steel beam E framing between them, both ends joined; a beam F touching nothing. Write every id down (Manage > IDs of Selection) into the job file. Save under `%TEMP%\heron-work` | Modify > Wall Joins on the L shows Butt; beam E's Properties show Start and End Join Cutback editable |
| **LY2** | *D-30 positive and negative, THE NEGATIVE FIRST.* The job file's `set-wall-join-type` job, `--write`, rolled back | Negative, C and D: changed 0, each refused *"meets none of the other walls handed in"*. Positive, A and B: changed 1, *"Butt -> Miter, read back"* |
| **LY3** | *The read.* The job file's `report-wall-join-type` job | Positive: ONE row for the L corner, freeEnds 2. Negative, wall C alone: no rows, freeEnds 2 |
| **LY4** | *The framing ends, THE NEGATIVE FIRST.* The job file's `set-framing-end-extension` job, `--write`, rolled back | Negative, beam F: changed 0, *"neither end is joined to another member"*. Positive, beam E: changed 2, Start and End Join Cutback *"0 mm -> 50 mm, read back"*. **If `ElementsAtJoin` names nothing at E's ends, that is Revit's rule, not a pass - record it, and run with `whichEnd=both`** |
| **LY5** | *The refusals.* Wall A alone; a third wall making a T at the L corner, all three with `joinType=miter`; the L with joining disallowed at the corner; `joinType=sideways`; beam F with `setting=cutback`, `whichEnd=start`; a wall with beam E under `set-framing-end-extension` | Refused before anything changes; the T refused with Revit's message or read back unchanged and refused, never counted; *"joining is disallowed"* or *"meets none"* - joining never turned on; refused naming butt, miter and square off; *"greyed out"* or changed 1 - record which; the wall *"not structural framing"* |
| **LY6** | *Kept once, and the second route.* Through `try-once` on the modeller's yes: the L made a miter, and beam E cut back 50 at its joined ends; then Modify > Wall Joins and Properties by hand | ONE entry in Revit's undo list each; the Options Bar shows Miter at the corner and Properties shows 50 mm at both ends |
