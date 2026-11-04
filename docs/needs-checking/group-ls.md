# Needs checking — Group LS

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group LS - an L-shaped, U-shaped or spiral stair by component placed from a point, and a landing added between two runs of a stair already made: `CREATE_SHAPED_STAIRS`, `ADD_STAIRS_LANDING` (2026-11-02)

Asked for in the modeller's words: *"make an L-shaped stair from Level 1 to Level 2 turning left at the landing"*,
*"add a U-shaped stair with a 1200 landing here"*, *"place a spiral stair of 1500 radius from Level 0 to Level 1"*
and *"add a landing between these two runs"* - Revit's Stair by Component, its L, U and spiral runs and its
landings. heron-duplicate-checker's method, run before writing: `brain/heron_merge.py` found no shared signature
with a shared capability, and the duplicate check and the Matcher found nothing equal. `CREATE_STAIRS` (PROVEN)
makes a straight or U stair **fitted to a stair hall's walls** - an **overlap on the U only**: this card places a
stair from a point and makes the L, the spiral and a U with a landing of a set depth, which that card cannot.
Widening it would make its proof stale, so it was not widened; combining the two is the owner's choice once this
is proved. `CREATE_RAILING` and `EDIT_RAILING` (PROVEN) put and change railings - look-alikes. Nothing in the
library added a landing to a stair already made.

| What | Where | What it does |
|---|---|---|
| `CREATE_SHAPED_STAIRS` (MODIFY) | [`brain/fragments/create-shaped-stairs`](../../brain/fragments/create-shaped-stairs/fragment.yaml) | Makes a stair by component between two **named** levels, placed from a point: an **L** with a landing at the turn, a **U** with a landing across its end and the second run alongside, or a **spiral** round a centre at a **given** radius. The first run climbs north, south, east or west and turns left or right. Risers are the fewest at or under the stair type's Maximum Riser Height, the tread its Minimum Tread Depth; the type is the one named or **Revit's default for new stairs, said in the answer**. A landing depth in mm makes a sketched landing that deep; left out, Revit's own. **Read back inside the edit scope** - risers, tread, runs, landings, the landing's depth - and any miss cancels it; one riser more is tried once when Revit will not make the count; the railings Revit adds are named; a refusal quotes Revit and leaves nothing |
| `ADD_STAIRS_LANDING` (MODIFY) | [`brain/fragments/add-stairs-landing`](../../brain/fragments/add-stairs-landing/fragment.yaml) | Adds Revit's own landing between **two selected runs of one stair** already in the model, of the landing type the stair type names. Two runs of different stairs, anything but two runs, and a pair Revit says it cannot join are refused before the stair is opened. The new landing's depth, thickness and height above the stair's base are read back in mm, with the stair's risers before and after |

**What is done and what is not.** Both compile for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-02, a Linux container). Every Revit member both cards call was read
off the shipped assemblies with `tools/api-surface --members`, the same signature 2020 to 2027 - the list is in
each card's `compatibility-note`. Both follow `CREATE_STAIRS`'s edit-scope pattern exactly: the ambient
`editScopeFailures` need, so the add-in runs them inside its transaction group with no transaction open (D-112).
**Nothing here has run in Revit.** Both cards are DRAFT. **Only Revit can settle:** whether a **sketched landing**
drawn at the base level's elevation, with its base elevation relative to the stair, joins the two runs as an
automatic one does (LS4); whether a spiral laid out as *(risers - 1) treads* along its centre line arrives at the
top level with the riser count asked (LS5) - the rule `CREATE_STAIRS` measured for a straight run; which way Revit
turns a spiral it is told is clockwise; and whether an automatic landing between two runs that already meet at
one is refused by `CanCreateAutomaticLanding` or made a second time (LS7). **Not done:** a straight stair and a U
fitted to a stair hall (`CREATE_STAIRS`'s), a gap between the runs of a U, a winder at the turn, a spiral of more
than one turn as anything but what Revit makes, a landing of a set depth added to a stair already made, and the
opening in the slab above (`CREATE_STAIRS` with `cutOpening`, or `CREATE_SHAFT_OPENING`).

**Routing.** `tools/check-routing.py` on its own scratch store, on a copy of the tree without the two cards and on
this change, on both backends (`model`, and lexical with the embedding libraries hidden): no question answered by
a write; claimed in a routing table and not reached 67 before and 67 after. The four sentences reach their card by
identity through the host's lookup; *"make a spiral staircase"*, *"add an L shaped stair here"*, *"put a landing
between the runs"* and *"make the stair turn right at the landing"*, nobody's words, reach them too, and every
sentence `CREATE_STAIRS` and `CREATE_RAILING` declare still reaches its own card. On `model`, *"put it back"* - a
sentence already reaching a write its claimant does not name ([row 5b-1375](../fragment-issues/section-5b-rows-401-425.md))
- moved from `RESET_GRAPHIC_OVERRIDES` to `PLACE_FAMILY_INSTANCES`, wrong to wrong; recorded as [row
5b-1405](../fragment-issues/section-5b-rows-401-425.md), not declared, because which card it means is the owner's
choice. On the keyword route alone this card now heads *"make an L shaped extrusion"* and *"are these on the right
level"*, both already lost on that route and both answered by identity at their own cards.

**The model** - "Heron Shaped Stair Test", a scratch project from Revit's metric Architectural template, saved under
`%TEMP%\heron-work`, never a working model. Every write here is ROLLED BACK unless the modeller keeps it. The job
file is [`tools/jobs/shaped-stairs-ls-2026-11-02.yaml`](../../tools/jobs/shaped-stairs-ls-2026-11-02.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`, `--session
<process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **LS1** | *Arrange.* "Heron Shaped Stair Test" from the metric Architectural template; Level 1 at 0, Level 2 at 3000; one stair type set as the default for new stairs. Away from 0,0,0 and 10000,0,0, draw two stairs by hand with Revit's Stair tool, Level 1 to Level 2, each two straight runs in an L with Revit's landing; in Edit Stairs delete the landing of the first. Save. Write the four run ids into the job file | The default stair type's name, Maximum Riser Height and Minimum Tread Depth in Type Properties; the first stair's two runs with no landing, the second whole |
| **LS2** | *The L - D-30 positive and negative, THE NEGATIVE FIRST.* The job file's first job, `--write`, rolled back | Negative, stair type *Spiral Feature Stair*: refused naming the loaded stair types, NOTHING WAS MADE, `created` invalid. Positive: `runCount` 2, `landingCount` 1, the finding naming the type *(Revit's default for new stairs)*, *turning left*, the risers per run |
| **LS3** | *What Revit made.* LS2's positive, kept through `try-once` on the modeller's yes, or read in the job's report | Risers the fewest at or under the type's Maximum Riser Height; run 1 climbing north from 0,0; run 2 leaving the landing to the **west**; the railings Revit added named |
| **LS4** | *The U with a 1200 landing.* `validate create-shaped-stairs --write` with *shape u, startPoint 0,0,0, direction east, turn left, runWidth 1100, landingDepth 1200*, rolled back | `landingDepthMm` 1200; run 2 to the north of run 1 coming back west; a sketched landing joined to both runs. **Refused by Revit with its reason** is a finding for the author, never a pass |
| **LS5** | *The spiral.* `validate create-shaped-stairs --write` with *shape spiral, startPoint 10000,0,0, direction east, turn right, runWidth 1000, radius 1500*, then the same with radius left out and with radius 400, rolled back | One run, no landing, risers reaching Level 2, the first riser east of the centre turning **clockwise** seen from above; the second refused (radius never guessed), the third refused (the inside edge crosses the centre) |
| **LS6** | *The landing - positive.* The job file's second job, `--write`, rolled back | `landings` 1 id between the first stair's two runs; its depth, thickness and height above the stair's base in mm; risers before and after |
| **LS7** | *The landing - negative, RUN BEFORE LS6.* The same job's negative: the second stair's two runs, which already share a landing | Refused - Revit answers it cannot join them - NOTHING WAS CHANGED. **A second landing made on that stair is the defect this case is here to catch** |
| **LS8** | *The second route.* Each stair after a Keep through `try-once`, selected in Revit; and Revit's own Stair by Component drawn by hand along the same path, with the same levels, type and width | Properties shows the type, Desired Number of Risers and Actual Riser Height the answer gave; the hand-drawn stair's riser count matches; Undo takes each job back as one entry |
