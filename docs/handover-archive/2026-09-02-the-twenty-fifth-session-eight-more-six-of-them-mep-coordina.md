# The twenty-fifth session, 2026-09-02 — eight more, six of them MEP coordination

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**What it did:** took the library from **178 to 186**. Same container, same limitation. `A9` now names
**forty-eight**. This batch went deliberately at the owner's own trade: six of the eight are the checks a
BIM modeller runs before a coordination issue.

| | |
|---|---|
| `READ_GRAPHIC_OVERRIDES` | *"Make this pipe look the same as that one"* was answering `CREATE_PIPE`. **This is the gap the twenty-fourth session measured and left on purpose**, and closing it took one small read. `OVERRIDE_GRAPHICS_IN_VIEW` takes settings ALREADY BUILT — nothing could produce them from an element that already looked right, so matching had no answer at all. An element with NO override still returns the empty settings object, because applying that CLEARS another element's override, which is a real use |
| `CHECK_SLEEVE_SIZE` | *"Are the sleeves big enough"* was answering `MEASURE_MEP_SLOPE`. The service is found by GEOMETRY — a sleeve family records nothing about what goes through it. **A sleeve with nothing through it is the finding**, not a blank row: either an orphan, or the run moved and the hole did not. Required size is arithmetic with every term explicit, and the insulation is read from the real element because a 50 mm jacket turns a comfortable sleeve into a tight one |
| `CHECK_EQUIPMENT_CLEARANCE` | *"Is there enough space in front of the panel"* was answering `MEASURE_MEP_SLOPE` too. **The zone is DIRECTIONAL and that is the whole point** — an AHU needs 1500 mm in front and 200 mm behind, and a sphere either passes real obstructions or fails on the wall the unit is meant to stand against. Built on the family's own facing direction, which is REPORTED per unit, because a family authored facing the wrong way makes this confidently wrong and nothing else would say so |
| `CHECK_VALVE_ACCESSIBILITY` | *"Can we reach the valve to operate it"* was answering `LIST_GRIDS`. **Three questions, answered separately, because each has a different fix**: room, an access panel where it sits above a ceiling, and reach height. Above a ceiling is NOT a fault — most valves are — so it is a list for the architect rather than a failure. A check that cries wolf about every valve gets switched off by lunchtime |
| `CHECK_VERTICAL_CLEARANCE` | *"How much room between the duct and the pipe above it"* was answering `MEASURE_ELEMENT_LENGTHS`. **Not `CHECK_MINIMUM_CLEARANCE` with a smaller number**: two services 200 mm apart diagonally have 200 mm of straight-line clearance and may have 40 mm of vertical room, which is what a hanger and a flange need. It says which one is ON TOP, so drainage above the duct it must cross under reads as a fact |
| `AUDIT_MEP_OPENINGS` | *"Which of my openings are wrong now the ducts moved"* was answering `READ_ELEMENT_OWNERSHIP`. **The trap it is built around**: a cut void is a Revit `Opening` with NO SOLID, a placed sleeve is a family instance that has one, and the obvious way to gather openings returns mostly the first — so an audit written for the second reports "no geometry" for all of them, with no crash and no error, and audits nothing |
| `CREATE_ROOM_ELEVATIONS` | *"Make elevations inside each room"* was answering `FILTER_ELEMENTS_IN_ROOM`. A marker with four slots, not a section — nothing about a section makes the four-arrow symbol a drawing set expects. **The slots are NOT rotated to face the walls**, so in a rotated room the views look at corners, and that is said in the report because it is invisible until somebody lays out the sheet |
| `CREATE_HVAC_ZONE` | *"Create an HVAC zone"* was answering `PLACE_ROOMS`. See below — this one is a first for the library |

**The first fragment here whose capability a Revit release REMOVES.** Read off 2027's own reference
assembly rather than taken from a note: the creation factory has **no zone method left**, and
`Zone.AddSpaces` and `RemoveSpaces` are **gone from the `Zone` class** — while `Zone` itself, `Zone.Spaces`
and `Zone.Area` remain, so the type still existing proves nothing. Both vanished calls are therefore
reached BY NAME, which keeps one source compiling on all eight releases, working on 2020 through 2026, and
**reporting a plain reason on 2027** instead of failing. Naming either directly would break the 2027
build, and a try/catch does not help — it never gets to run.

**Three sentences turned out to be already answered, and reading the neighbours is what showed it.**
*"Draw a cable tray here"* and *"draw the conduit"* → `CREATE_ELECTRICAL_RUN`, which already covers both
and says in its own purpose why they are one fragment. *"Place spaces in all the rooms"* → `PLACE_ROOMS`,
which already places rooms **or** spaces. *"Save these as a named selection set"* → `CREATE_SELECTION_FILTER`.
All three were routing correctly at #1 already; the batch just confirmed it rather than building beside them.
**Two more were dropped for being too close to something that exists**: a filter-per-value fragment sits
too near `COLOR_BY_PARAMETER` to be worth the crowding, and it waits for evidence rather than a guess.

**The orphan check earned its keep again, and the fix was better than last time's.** `CREATE_HVAC_ZONE`
named its input `spaces`, which nothing provides. Last batch the same report was answered by marking the
input `source: request`; here the honest answer was different — `FILTER_ELEMENTS_BY_CATEGORY` **does**
provide it, under the library's own name `elements`. Renaming the need made the composition real instead
of declaring it unfeedable. **Worth remembering: an orphan report is a question about the NAME first and
about the source second.**

**And a mis-route the batch found in passing, in a fragment it did not write.** *"What is too close to
what"* was answering `FIND_NEAREST_ELEMENTS` — while `CHECK_MINIMUM_CLEARANCE`'s own purpose says, in
capitals, that it is NOT that fragment with a threshold. The routing table said one thing and the index
had never seen the sentence. Declared, and it now routes by identity.

---
