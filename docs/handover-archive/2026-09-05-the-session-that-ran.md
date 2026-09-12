# HANDOVER — the session that ran 2026-09-05

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**What happened, in one line: the library went from 210 fragments to 218 — the REPORTING block — and
four of the eight replaced an answer that was a WRITE.**

**The selection rule found the worst shape yet.** Asking `heron_brain.lookup` the owner's own sentences,
four questions were being answered by fragments that change the model:

| The sentence | Where it went | What that fragment does |
|---|---|---|
| *"what is the wall build up"* | `CREATE_WALL` | builds a wall |
| *"what panels and mullions are in this curtain wall"* | `CREATE_WALL` | builds a wall |
| *"what rooms are on either side of this door"* | `PLACE_ROOMS` | creates rooms |
| *"what is the pressure drop in this duct run"* | `SET_MEP_SLOPE` | changes the slope |

**Reading the neighbours stopped a duplicate outright.** `REPORT_NESTED_FAMILIES` was in the batch until
`GROUP_BY_ASSEMBLY` was read — which is that fragment already, near word for word (*"an AHU with a nested
fan, coil and filter is FOUR family instances… there is one unit on site"*). The routing was still wrong,
so the fix was **three utterances on the existing fragment, not a ninth file.** A lookup shows what is
missing; only reading the candidates shows what already exists under a different name.

**The metadata gate caught a ninth shape, and it is the one that matches itself end to end.**
`Definition.ParameterGroup` exists on 2020 and is gone by 2027; its replacement `GetGroupTypeId` has
never existed on 2020. **Neither spelling works at both ends**, so `REPORT_PARAMETER_INVENTORY` reports
no parameter group at all and says why in its own summary. *A member having a replacement does not make
it portable — check the replacement against the OLD release too.* The same pass corrected two names
before they were written: `MEPSection` is in `Autodesk.Revit.DB.Mechanical` **for pipe systems as well as
duct ones**, and `RoutingPreferenceRule` has `GetCriterion(int)`, not a `GetCriteria()` collection.
Neither guess exists on any release.

**Three routing defects were found and fixed, two of them older than this batch:**

- **A fragment contradicting itself.** `READ_ROOM_GEOMETRY`'s routing comment said *"how big is this
  room → here"* while the table in its own purpose, ten lines above, said `MEASURE_ROOM_DIMENSIONS`.
  Two comments in one file disagreeing is invisible until something reads both.
- **A question answered by a write.** *"What is the Mark on these"* resolved to
  `WRITE_ELEMENT_PARAMETERS`. Declared on `READ_ELEMENT_PARAMETERS` so identity wins.
- **A new fragment stealing a room sentence.** `REPORT_AREAS` was winning *"what is the room area"* on
  rank while `READ_ROOM_GEOMETRY` merely claimed it in a comment. An area and a room are different
  elements — that fragment's own routing table says so — so the sentence was declared where it belongs.

`check-routing`'s claimed-but-not-reached list went from **8 to 4**, and the four left are all older.

### What the next session should do, in order

1. **`python tools/check-gaps.py` first.** Computed from disk; it wins over every sentence here.
2. **On any machine with the .NET SDK: `python tools/check-fragments-compile.py`.** **Eighty** fragments
   now owe a real compile — `A9`, still the largest thing needing no Revit.
3. **With Revit open: start proving.** All 218 are `DRAFT`.
4. **To carry the library build on:** [§9a](../HANDOVER.md#9a-continuing-the-library-build--the-recipe-so-another-session-can-just-start).

**How much of the owner's earlier library is left, measured rather than guessed (2026-09-05).** Of its
398 scripts, **68 are not fragments at all** — 44 `recipes/` become skills, 12 `context/` are the host's
job ([D-46](../DECISIONS.md)), 8 `commands/` are native Revit commands, 4 are examples and the prelude.
That leaves **330 fragment-eligible**, of which roughly **60 are still worth adding**. It is not a
subtraction: the library is re-authored, several sources fold into one fragment, and some Heron fragments
have no source at all. The biggest remaining blocks are **tag and dimension placement** (~10: auto-arrange
tags, centre room tags, stack tags, L-shape leader, spot elevations), **filters** (~8: by parameter value,
by host, by sub-component), **CAD and model admin** (~8), **revisions** (~5: edit, delete, remove from
sheet — only create/list/cloud exist) and **exports** (~3: view image, FBX, families).

**Measured gaps left open on purpose, with the sentences that found them:**

| The sentence | Where it goes today |
|---|---|
| *"save the view as an image"* | `CREATE_SELECTION_FILTER` |
| *"which title block is on each sheet"* / *"change the title block"* | `CREATE_SHEETS` — a **write** for a question |
| *"why is this element not matching my view filter"* | `DIAGNOSE_VISIBILITY` — about view visibility, not filter rules |
| *"count the fittings per space"* | `CHECK_EQUIPMENT_CLEARANCE` |
| *"where is this element located"* | `FIND_VIEWS_SHOWING_ELEMENT` |

---
