# The seventeenth session, 2026-09-01 — eight fragments, and a comment nobody could hear

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**What it did:** took the library from **86 to 94**. All 94 compile on all eight releases; all 94 are
`DRAFT`. The batch closed a hole the previous one opened, and then found a defect class running through
half the library.

| | |
|---|---|
| `CREATE_PLAN_VIEW` | **Closes the hole `CREATE_LEVEL` opened.** A level made through the API has no views — Revit's *tool* makes them, the level does not — so *"add 4 more floors"* means levels **and** plans, and only the first half existed |
| `LIST_GRIDS` | **Grid names cannot be sorted as text.** 1 to 12 sorts to 1, 10, 11, 12, 2, 3 — somebody looking for grid 3 finds it sixth. Split into letters and trailing digits, compared as a number. Every row says which way the grid runs, because the name never does and both conventions exist in one building |
| `CREATE_GRID` | A grid is a **vertical plane**, so its defining line must be level — checked here, where the message can say why, rather than surfacing as an exception naming a curve |
| `PLACE_ROOMS` | Rooms **or** spaces, in one pass per level. MEP loads run on Spaces and the two are indistinguishable to look at. An unbounded result is the useful half: the gap in the walls is also why the area next door came out enormous |
| `LOAD_FAMILY` | **Will not overwrite, and that is a limit of the fragment format itself.** Revit needs an `IFamilyLoadOptions` object to decide about differing parameter values; that needs a **class**, and a fragment is a body of statements. The behaviour lands on the safe side by accident and is kept on purpose |
| `CREATE_VIEW_FILTER` | A filter **matches**; an override **remembers**. Work modelled next week comes out the right colour with nobody touching the view — on a reissued drawing that is the whole job |
| `EXPORT_VIEWS_TO_DWG` | The first fragment that writes **outside the model**, so it cannot be undone. Files that would be overwritten are reported, and the run still exports — refusing everything over one collision means somebody empties the folder and re-runs |
| `EXPORT_MODEL_TO_IFC` | The schema version is required, never defaulted: hand a recipient expecting 2x3 an IFC 4 and it **opens**, shows geometry, and reads as a bad model rather than a wrong format |

### The compile gate caught two API guesses, and reflection caught a third thing it could not

Two fragments failed on **all eight releases** — not a version break, just wrong signatures written from
memory. Both were settled by reflecting over the reference assemblies rather than guessing again:

- `ExportDWGSettings.GetByName` does not exist. It is **`FindByName`**.
- `NewSpaces2(Level)` does not exist. It is **`NewSpaces2(Level, Phase, View)`**, while `NewRooms2(Level)`
  needs neither. Identical on 2020 and 2026, so **not a version difference — a real asymmetry between
  the two elements.** A space belongs to a phase and is placed from a view; a room does not.

**And then the thing a green build cannot catch.** `CREATE_VIEW_FILTER` needs a compile symbol, and its
note said the old `CreateEqualsRule` overload was *"gone after 2022"*. Reflection over all eight
assemblies says it survived to **2025 and was removed in 2026**, with the replacement arriving in 2023 —
a three-release overlap. **The code was right, so all eight compiled green and nothing complained.**

> **A compile gate checks the call. It does not read the sentence beside it.** Every version claim in
> this repository is a prose claim, and prose is exactly what the gate cannot see. Read the assemblies.

### A routing table is a comment, and comments are not indexed

`OVERRIDE_GRAPHICS_IN_VIEW` claimed *"make these red"* in its routing table and never declared it as an
utterance — so the sentence resolved to `GROUP_ELEMENTS`. `heron_search` indexes **semantic-identity,
the utterances, the capability, the domain and the purpose**, and nothing else. A table saying `-> here`
records a decision retrieval cannot act on.

Audited across all 94: **69 such claims, 23 of them reaching the wrong fragment.** Among them
*"zoom to these"* building an **MEP fitting**, *"write that up"* reaching a bulk parameter **write**, and
*"place a diffuser"* reaching the duplicate finder. All 23 fixed; two of the 69 turned out to be **one
sentence claimed by two tables**, which is two comments disagreeing and invisible to every other check
here. Both resolved by deciding an owner.

`check-routing.py` now audits this every run — measured through `find`, and **proven by planting an
unreachable claim and watching it fire.** 159 claims, 0 wrong.

### `heron_scope.py --rebuild` did not do what its own help said

It promised *"re-index every scope from disk"* and rebuilt **only the fragments table** — leaving the
identity table and the embeddings holding whatever the last run put there. Measured: after adding an
utterance and running only that command, the new sentence resolved to a **different fragment**,
confidently, by the hybrid route, while the identity route that should have matched it exactly had never
heard of it.

It went unseen because every tool that reads the index calls `SEARCH.index` itself first — `check-routing`
does, the brain does. **The only person who could hit it was somebody rebuilding by hand and then asking
a question, which is exactly what the command is for and exactly what its help told them to do.** Fixed
in the command, not in `rebuild()`, which stays cheap for callers that only want the metadata.

### One fragment was not written, on purpose

*"What is the area of this"* was on the list of gaps. It is not one: `MEASURE_ELEMENT_VOLUME` already
provides `areaM2` from the same solids. A second fragment would compute the same number from the same
input and could one day disagree, with nothing to say which was right. **The gap was in the words** —
two utterances were added instead. Same finding as *"what is the elevation of this"* two sessions ago.

**What none of this is.** Not one fragment has met a model. `check-gaps` reports **0 unfinished, 55
waiting**, and counts **94** below `PROVEN`.

---
