# The ninth session, 2026-08-31 — seven fragments, and three of them were found by tools

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**What it did:** took the library from **32 to 39**, chosen by evidence rather than by picking from the
owner's earlier library in order. All 39 compile on all eight releases; all 39 are `DRAFT`
([D-44](../DECISIONS.md)).

**How the seven were chosen.** The brain was asked the sentences the owner actually says, and the
answers were read. Some were not near misses but opposites: *"how many metres of pipe"* returned
`CREATE_DUCT`, *"renumber the doors"* returned `SNAP_TO_GRID`, and **nothing could undo a hide** — Heron
could take things off a view with `HIDE_ELEMENTS` and `ISOLATE_ELEMENTS` and had no way to put them back,
so every hide was a one-way door. Those holes picked the batch.

| | |
|---|---|
| `SHOW_ELEMENTS` | Closes the one-way door. **Undoing a hide is not the mirror of doing one**: a permanent hide is undone element by element, a temporary one cannot be — Revit's only exit is leaving the mode, which brings back everything. So the temporary path ignores its own element list and says `temporaryModeCleared` rather than a count that would imply a precision it has not got |
| `SET_PIN_STATE` | The other half of the move defect this repository already found: Revit's move returns normally and moves nothing for a pinned element. Reports what **changed**, counting already-pinned separately |
| `MEASURE_ELEMENT_LENGTHS` | The takeoff question. Anything without a length is **named, never counted as zero** — a fitting scored 0 mm gives a total that looks complete and is short |
| `RENUMBER_SEQUENTIAL` | Does **not** decide the order — that is the caller's. Checks collisions across the whole batch **before writing anything**, because a half-renumbered corridor has two schemes in it and no record of where it stopped |
| `MEASURE_MEP_SLOPE` | Computed from the endpoints, not read from the slope parameter — a pipe whose parameter says 1% and whose ends are level is flat. Being a ratio, it is the one measurement in the library that **cannot carry a unit error** |
| `TAG_ELEMENTS` | One tag per element, skipping the already-tagged: a second pass otherwise stacks a duplicate exactly on top of the first, invisible until somebody drags one |
| `FIND_UNTAGGED_ELEMENTS` | **Written because the dependency graph said it had to exist** — see below |

### Three defects the tools found, and only one of them was in the C#

**1. The compile gate caught a version break that no documentation would have.** `find-untagged-elements`
was written against `IndependentTag.TaggedElementId`, which compiled clean on 2020 and 2022 and **failed
on 2027 — the property is removed there.** Its replacement, `GetTaggedLocalElementIds()`, **arrived in
2022**. The overlap is 2022–2026 and **no single accessor spans the supported releases**, so the
implementation splits on a compile symbol. This is the owner's own named problem — *"it errors on a newer
Revit"* — caught before the machine instead of mid-job.

> **It puts a requirement on unbuilt work.** This is the first fragment in the library to need a
> `REVIT20xx` compile symbol, so [D-28](../DECISIONS.md)'s in-process Roslyn executor **must define the
> same symbols MSBuild does**. A host defining none takes the `#else` branch and breaks on 2020 and 2021
> only — which is the release the owner actually runs.

**2. The dependency graph refused a fragment that compiled perfectly.** `TAG_ELEMENTS` took an
`alreadyTagged` list and **nothing on disk could produce one**, so `test_graph.py` reported an action
nothing can feed: a composition that cannot be assembled however well each half compiles. The easy fix
was to mark that input as coming from the request, which would have made the graph quiet and the problem
permanent — nobody can type a list of element ids. So the missing piece was written instead, and it
answers a real question of its own: *"which ducts have not been tagged"*.

**3. `check-routing` caught the new fragment stealing another's sentence — and the first fix made it
worse.** `RENUMBER_SEQUENTIAL` was answering *"set the room number"*, which is
`WRITE_ELEMENT_PARAMETERS`' own declared utterance and a plain single write. The fix written first added
a disclaimer to `purpose` quoting that sentence — **and `purpose` is indexed**
([`heron_search.py`](../../brain/heron_search.py) puts it in the searchable text), so the fragment was handed
one more copy of the words it was losing on and did not move.

> **The general rule, and it applies to every routing table in the library: a disclaimer written into an
> indexed field makes the fragment claim the sentence it is disclaiming.** Routing tables belong in
> comments, where a human reads them and the index does not. Contested sentences went 16 → 15 of 205 once
> the quote was removed from `purpose`.

**And a test fixture that had expired twice was made to derive itself.** `test_graph.py` hardcoded the
providers of `elements` — one until 2026-08-29, two until this session, and each time a correct library
failed the test and the fix was to type one more path. It now finds them from disk. Its first version
matched the same two lines under `needs:` as well, which broke the composition from the consumer's side
and passed for the wrong reason; it is scoped to the `provides:` block.

**What none of this is.** Not one of the seven has met a model. They are `DRAFT`, `check-gaps` counts
**39** fragments below `PROVEN`, and compiling proves the API surface agrees and nothing whatever about
whether a duct is measured in millimetres or feet.

---
