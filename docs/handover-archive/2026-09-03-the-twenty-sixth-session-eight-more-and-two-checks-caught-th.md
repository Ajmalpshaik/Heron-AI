# The twenty-sixth session, 2026-09-03 — eight more, and two checks caught the session's own mistakes

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**What it did:** took the library from **186 to 194**. Same container, same limitation. `A9` now names
**fifty-six**. Four of the eight close a mis-route where a QUESTION was being answered by a fragment that
WRITES — the shape this library treats as the worst.

| | |
|---|---|
| `CHECK_FAMILY_STANDARDS` | *"Check the families are named to our standard"* was answering `RENAME_FAMILY` — a write, to a question. **It audits TYPES, not instances**: one badly built type placed two hundred times is ONE thing to fix, and an instance report buries everything else in it. **Connectors are read through a placed instance**, because a type does not expose them — so a type placed nowhere is NOT CHECKED rather than passed, and that distinction is what separates an audit from a guess |
| `CHECK_CEILING_COORDINATION` | *"Are the light fittings sitting properly in the ceiling tiles"* was answering `SNAP_TO_GRID` — which MOVES things. **The fault it catches is invisible in plan**: a diffuser 40 mm above the ceiling and one exactly in it look identical from above. NO CEILING ABOVE is a finding, not an error — an open soffit is fine |
| `CHECK_EQUIPMENT_CONNECTORS` | *"Are the equipment connectors the right size for the duct"* was answering `CHECK_FLOW_DIRECTION`. **The unconnected spigots are usually the bigger finding** — no clash test and no connectivity walk flags them, because the services that ARE connected trace perfectly. **Size comparison is shape-aware**: round reports a radius and rectangular reports width and height, and comparing them as one number is how this kind of check produces confident nonsense |
| `CHECK_FIXTURE_CONNECTIVITY` | *"Are all the sinks and toilets connected to the drainage"* was answering `MEASURE_MEP_SLOPE`. **It reports PER SERVICE**, which is the only actionable form — *"nine WCs have no vent"*, not *"twelve fixtures have an unconnected connector"*. And it is not `FIND_DEAD_ENDS` from the other end: **a fixture nobody piped produces no open pipe end at all**, so that fragment structurally cannot see it |
| `REPORT_CATEGORY_OVERRIDES` | *"What category overrides are on this view"* was answering `READ_GRAPHIC_OVERRIDES` — **whose own purpose says in writing that it reads the per-element override only**. A question answered by a fragment that structurally cannot answer it, coming back empty and reading as *"nothing is overridden"*. **The visibility flag is not the signal**: a pattern's own "is visible" reads TRUE with nothing set, so a check built on it reports the whole model as overridden |
| `REPORT_DESIGN_OPTIONS` | *"Which design option am I working in"* was answering `REPORT_SPACE_AIRFLOW`. A design option changes what *"all the ducts"* means — a count that disagrees with the screen, a short schedule and an element that will not delete all have this one cause. **The active option cannot be changed from code on any release**, checked at both ends, and `Element.DesignOption` is read-only too |
| `RENAME_WORKSET` | *"Rename this workset"* was answering `SET_VIEW_WORKSET_VISIBILITY` — which changes a drawing. **A workset is not an element**; it lives in the workset table, which `RENAME_ELEMENTS` cannot reach. Deleting one is absent from 2020 and present from 2024 — read at both ends, not inherited |
| `CREATE_GRIDS` | *"Make a grid series across the building"* was answering `SNAP_TO_GRID`. Bay dimensions are the input, which is how a drawing states a grid. **They are GAPS, not positions** — four of them make FIVE grids, and both readings look sensible to somebody checking the code |

**The API check earned its keep a sixth time, and this time on code this session had already written.**
`RENAME_WORKSET` reaches `DeleteWorkset` **by name** because that call is absent from Revit 2020 — and
the lookup then named `DeleteWorksetSettings` in its argument list, **a type that does not exist on 2020
either**. The reflection was protecting the build from one missing member while breaking it on another,
on the very line meant to protect it. Nothing in the fragment's own logic would have shown this; the
metadata read printed `TYPE NOT FOUND` and it was fixed before it was ever compiled. **Reaching a call by
name is not enough — every TYPE named in the lookup has to exist on the oldest release too.**

**And `test_embed.py` caught the other one, in a way worth copying.** `CREATE_GRIDS` took
`READ_CEILING_GRID`'s own declared words: *"what is the ceiling grid spacing"* ranked the new fragment
first. The test's failure message **names the thief** rather than just failing, so the diagnosis was one
line long. The fix is the interesting part: the repository's rule is that weakening an utterance somebody
actually says, to buy back a rank, is never the answer — so nothing on `READ_CEILING_GRID` was touched.
What changed was **my own prose**: an invented utterance in developer-speak, and a `purpose` that said
*"spacings"* five times because the gaps-not-positions point was repeated. The keyword index reads the
purpose as well as the utterances, which is what made repetition a ranking act rather than a stylistic
one. Both were tightened; every point the fragment made survives.

**Three sentences were already answered and are recorded rather than rebuilt.** *"Turn the whole category
to halftone"* → `SET_CATEGORY_GRAPHICS`, which takes a prepared settings object and therefore already does
halftone and transparency. *"Put the elements back that I hid"* → `SHOW_ELEMENTS`. *"Which filters are set
up but never used"* → `REPORT_VIEW_FILTERS`, whose purpose already names the unused row as the one worth
finding. And *"is there room for the insulation on these pipes"* → `CHECK_MINIMUM_CLEARANCE`, which folded
insulation in as a flag on purpose rather than keeping it as a second fragment.

**A gap this batch measured and deliberately left:** *"What changed between this model and the old one"*
still answers `SET_ELEMENT_WORKSET`. `COMPARE_ELEMENTS` compares elements inside one document and cannot
be it. A real model-to-model compare needs a second document opened or linked, and designing that
honestly needs the open-document API checked properly rather than guessed at — it belongs to a batch that
can give it the room.

---
