# The sixteenth session, 2026-09-01 — eight fragments, and the search backend hit its ceiling

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**What it did:** took the library from **78 to 86**. All 86 compile on all eight releases; all 86 are
`DRAFT`. Two of the eight close a hole where a request to CHANGE the model was being answered by a
read-only report.

| | |
|---|---|
| `CREATE_SHEET` | The missing link in producing a set: `CREATE_SECTION_VIEW` makes the drawing, this makes the sheet, `PLACE_VIEW_ON_SHEET` joins them, `LIST_SHEETS` reads it back. **The number is checked for a clash BEFORE anything is created** — Revit refuses a duplicate by throwing, and a throw part-way through leaves an unnumbered sheet in the register that nobody finds until an issue |
| `CREATE_TEXT_NOTE` | View-only annotation, and the refusals name the fix rather than saying "it did not work". Guarded on 3D views, schedules and templates — three different reasons, three different remedies |
| `LIST_LEVELS` | **Two elevations, and the obvious one is not the one on the drawing.** `Elevation` is measured from the internal origin; `ProjectElevation` is what the level head reads. They agree until somebody moves the base point — so most projects, which is exactly why the difference goes unnoticed until the one where it matters. Both reported when they differ. Sorted by HEIGHT, because "Level 10" sorts before "Level 2" as text |
| `CREATE_LEVEL` | Takes the height in the drawing's terms and converts, by **measuring the base-point shift off an existing level** rather than assuming it is zero. And it says out loud that **a level created through the API has no plan view** — Revit's *tool* makes those, the level does not, and the modeller who looks in the Project Browser and sees nothing concludes it failed when it did not |
| `LIST_WORKSETS` | **A closed workset is why a count comes back low, and nothing else says so.** Elements on one are not loaded: no collector returns them, no schedule counts them. Provides no `elements` on purpose — a `Workset` is not an `Element` and an empty list to look composable is a lie the graph would act on |
| `FIND_DUPLICATE_ELEMENTS` | *"Find duplicate elements"* was answering `RENAME_ELEMENTS`. **One of each cluster is kept and is NOT returned** — that is what makes the result safe to hand to `DELETE_ELEMENTS`. Same type AND same place both required: two different types at one point is a design clash, and filing it here puts a coordination problem in a list people bulk-delete from |
| `PLACE_MEP_FITTING` | *"Join these two"* was answering `CREATE_DUCT` and *"connect these two ducts"* was answering `TRACE_CONNECTIVITY` — **a read-only report answering an instruction to build something.** The fitting is chosen from the geometry (in line + same size → union, in line + different → transition, at an angle → elbow, three → tee, four → cross), which is what Revit does itself. **Nothing is moved to close a gap**: ends that are apart are refused with the distance |
| `CREATE_SCHEDULE` | A schedule VIEW that goes on a sheet, as distinct from `GROUP_AND_COUNT`, which puts numbers in the reply. A field the category does not have is **reported**, not skipped — a silently short schedule prints with a column missing and nobody reading it can tell one was wanted |

### Quoting another fragment's sentence in `purpose` hands it that sentence

The first draft of `LIST_WORKSETS` explained itself by quoting what other fragments answer — *"how many
ducts are there"*, *"is the model clean"*, *"a clash check comes back clean"*. **`purpose` is indexed.**
Those quotes took **four** sentences off the fragments that own them, including `READ_MODEL_WARNINGS`'s
own declared *"is the model clean"* and `SET_ELEMENT_WORKSET`'s *"they are on the wrong workset"* — a
request to change the model, answered by a read. Moving the examples into a `#` comment fixed all four
at once.

> **Illustrate a fragment with the sentences it OWNS.** Another fragment's example goes in a comment,
> where a reader sees it and retrieval does not. The same edit fixed `CREATE_SHEET`, whose purpose named
> `PLACE_VIEW_ON_SHEET` and thereby took *"add the view to the drawing sheet"* off it.

### Two utterances were retired, and the reason is not a rank

`CREATE_TEXT_NOTE` declared *"write this on the drawing"* and *"type this on the plan"*. Both lost — the
first to `WRITE_ELEMENT_PARAMETERS`, which is a **MODIFY**, so an annotation request was reaching a bulk
parameter write. The cause is that **write** and **type** are both Revit nouns, and a sentence whose
strongest word means something else in this domain will keep losing to the fragment that owns that
meaning. No rewording of the losing file changes it. The phrasings now avoid both words and the
ambiguity is recorded in the fragment; if somebody genuinely says one of them it belongs in a glossary
as a site-word mapping, not as an utterance that quietly loses.

### The built-in search backend has saturated, and a test was rewritten to say so

`tests/test_retrieve.py` failed. Its section 1 asserted that *"show me every duct in the model"* returns
at least two of four named claimants; at 86 fragments exactly **one** does, and the shortlist is
warnings, selection, sheets, findings and levels — **nothing about ducts.**

Measured rather than guessed: drop two words and the answer is right.

| query | top 3 |
|---|---|
| `show me every duct in the model` | QA-004, SEL-001, SHT-001 — none about ducts |
| `every duct in the model` | MEP-010, MEP-006, MEP-003 |

So the shortlist is decided by **"show me"**, not by **"duct"**. That phrase now opens an utterance in
half the library (*"show me the levels"*, *"show me the sheets"*, *"show me the warnings"* — each of them
exactly what a modeller says), while the one discriminating word carries almost no weight. The top five
span **0.0017**, well under one rank of fusion: nothing is being ranked at all. **n-gram similarity
separated 28 fragments and does not separate 86.**

**No utterance was weakened to make it green**, and the rewritten check says so in the file. What it
asserts now is what is true and stable — that the discriminating word still works when the saturating
phrase is out of the way — which is precisely the gap **`A7`** would close. A7 is the trained embedding
backend that has never run because the weights host is unreachable from here. It has been the least
urgent blocker on the register for weeks. **It is now the most urgent one**, and this is the first
measurement that says why.

> A test assertion was rewritten this session. That is worth flagging rather than burying: it is exactly
> the move that turns a real regression into a green tick. The defence is that the file had done it once
> before at 28 fragments, for the same reason, and recorded it; that the new assertion measures
> something *harder* rather than something looser; and that the old claim is left in place above the new
> one with the numbers that falsified it.

**What none of this is.** Not one fragment has met a model. `check-gaps` reports **0 unfinished, 55
waiting**, and counts **86** below `PROVEN`.

---
