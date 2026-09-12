# HANDOVER — the session that ran 2026-09-02 into 2026-09-03

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**What happened, in one line: the library went from 138 fragments to 202, in eight batches of eight, and
was merged to `main`.**

**The method, which is the part worth keeping.** Every batch started the same way: ask
`heron_brain.lookup` the owner's own sentences against the library as it then stood, and write down which
ones came back wrong. That is the whole selection rule — **nothing was built because it seemed useful; it
was built because a real sentence was measurably going somewhere else.** Then read the top three to five
candidates' `purpose` before writing anything, verify every API member against the Revit reference
assemblies for 2020, 2024 and 2027, write the three files, measure routing again, check every new
collision through `lookup`, add the cross-reference tables BOTH ways, run every checker, commit.

**Reading the neighbours before building is what earned the most.** It stopped seven fragments from being
written that already existed under another name — cable tray and conduit are one fragment, spaces are
`PLACE_ROOMS`, clearing a category override is `SET_CATEGORY_GRAPHICS` with an empty settings object, and
so on. The batch that skipped that step built a duplicate and had to withdraw it the same hour. **A
lookup can show what is missing; only reading the candidates shows what already exists under a different
name.**

**The metadata substitute for the compile gate paid for itself seven times**, and the shapes it caught
are worth knowing because a compiler on the PC will not teach them again:

| # | Shape | Example |
|---|---|---|
| 1–3 | A member the OLD release never had | `Ceiling.Create` is absent on 2020 |
| 4 | A member the NEW release removed | `GlobalParameter.IsValidDataType` is gone by 2024 |
| 5 | A whole CAPABILITY removed | Revit 2027 has no HVAC zone creation at all |
| 6 | Reflection that still names a missing TYPE | the delete-workset lookup named a 2020-absent type |
| 7 | An OVERLOAD whose ARITY changed | the filter rule takes 3 arguments on 2020, 2 by 2027 |

**Five impossibilities are now recorded rather than re-derived** — a scope box cannot be created, a design
option cannot be made active, the first legend cannot be created, a wall cannot be split, and a phase
cannot be created. Each is a routing row that says so and names the Revit route instead.

### What the next session should do, in order

1. **`python tools/check-gaps.py` first.** It is computed from disk and wins over every sentence here.
2. **On any machine with the .NET SDK: `python tools/check-fragments-compile.py`.** Sixty-four fragments
   owe a real compile. This is `A9`, and it is the largest thing that needs no Revit.
3. **With Revit open: start proving.** Every one of the 202 is `DRAFT`. [D-30](../DECISIONS.md) means a
   proof needs a case that comes back EMPTY, not just one that works.
4. **To carry the library build on instead:** [§9a](../HANDOVER.md#9a-continuing-the-library-build--the-recipe-so-another-session-can-just-start),
   and run the lookup sweep first — the un-mined source areas left are `actions/sheets-views/`,
   `actions/reporting/` and the rest of `filters/`.

**One gap measured and deliberately left:** *"What changed between this model and the old one"* still
routes to `SET_ELEMENT_WORKSET`. `COMPARE_ELEMENTS` compares elements inside one document and cannot be
it. A real model-to-model compare needs a second document opened or linked, and that API deserves
checking properly rather than guessing.

---
