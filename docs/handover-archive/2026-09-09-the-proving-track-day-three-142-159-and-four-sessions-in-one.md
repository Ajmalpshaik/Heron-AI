# HANDOVER — 2026-09-09 (the PROVING track, day three): 142 → 159, and four sessions in one tree

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**Eighteen fragments proved against `Snowdon-scratch_ajmal.al` in Revit 2024, and one un-proved.**
`set-view-section-box` went back to `DRAFT` when its implementation changed after its proof was signed
— which is the staleness guard working, not a regression.

Three other sessions ran alongside this one, on the prompts in the previous entry. **Sixteen PRs merged
on 2026-09-09**, #44 to #61. Everything is on `main` and no branch survives.

### What was proved, and the arrangement that did it

| | Positive | Negative |
|---|---|---|
| `set-element-level` | `moved 1` | `moved 0`, `refused 9` |
| `array-elements-radial` | `count=2` → `created 22` | `count=1` → `created 0` |
| `renumber-sequential` | `renumbered 22` | `renumbered 0` |
| `dimension-mep-runs` | `dimensionedCount 22` | `0` |
| `dimension-family-instances` | `dimensionedCount 10` | `0` |
| `edit-revision` | `changed 4` | `changed 0` |
| `duplicate-sheets` | `newSheetIds 17` | `0`, all 17 collisions named |
| `select-view-templates` | 18 templates | `0` |
| `manage-sheet-sets` | `memberCount 17` | `0` |
| `remove-view-template` | `detached 1` | `0` |
| `compare-elements` | `differing 29` | `0` |
| `check-vertical-clearance` | `tooClose 18` | `0` |
| `check-insulation-clearance` | `violations 46971` | `0` |
| `check-equipment-connectors` | `mismatched 22` | `0` |
| `set-selection` | tracking (D-53) | 22, 9, 10, 10, 307 across five inputs |
| `report-geometry-complexity` | tracking | 2024, 0, 1320, 3920, 28244 |
| `report-parameter-inventory` | tracking | 93, 22, 86, 71, 93 |
| `find-untagged-elements` | tracking | 14, 9, 10, 10, 307 |

**`set-selection` is the one that mattered.** It runs in every setup chain in every job file, so every
proof taken this week was standing on a fragment that was itself `DRAFT`.

**The arrangement that works for defect-finders**, learned by getting `check-vertical-clearance` wrong
four times: **a rich selection AND a demanding threshold for the positive; DUCT TAGS for the negative.**
A value-driven negative is stronger where it works, but it only empties the results that *depend* on
the value — `clashing 5` survived every gap tried, because a physical overlap is an overlap whatever
clearance you ask for. Only a selection with no geometry empties them all at once.

### THE THING WORTH READING: three capabilities were proved and invisible

Three separate times in one day, a batch stalled on something the library already had:

| What was thought missing | What actually existed |
|---|---|
| *"Sheets and views cannot be selected"* | **`list-sheets` is PROVEN**, needs only the document, and provides `elements` — which `set-selection` consumes. `list-sheets` → `set-selection` proved **five fragments in an hour**. Same for levels, grids, revisions, links |
| *"`views` needs an element list"* | `views (IList<View>)` **resolves from a single name** |
| *"The library cannot select more narrowly than a category"* | **Fifteen PROVEN selectors** are narrower — `select-by-family`, `select-by-connection-status`, `select-types`, `select-by-parameter-value`, `select-by-workset`, and more. **Every job file written that day used `select-by-category-name` and nothing else** |

Each was one edit away from being filed as a gap in `FRAGMENT-ISSUES.md`. **The library is further
ahead than the job files are**, and that is a far stronger argument for §3h.4 — generating job files
from contracts — than the six mistyped input names it was first written about.

### Two defects found in the proving harness itself

**`batch-prove` reports verdicts about runs that never happened — OPEN.** Six lease refusals came back
as six `POSITIVE EMPTY` verdicts, judged against an earlier session's record with a *different
arrangement*. Heron behaved perfectly — it refused, said nothing was sent, and declined to write a
record *"about a model that will not name itself"*. The runner judged the leftover file. **A date check
cannot catch this**; both records say `2026-09-09`. The session id in the `model` line is what separates
them, and `validate` already stamps it.

**A tracking runner that read the wrong fragment's output.** `prove` runs a chain and prints one
provides block per fragment; a search for `elements` found the *selector's* copy, producing five rows
that tracked the input perfectly because they **were** the input. `find-untagged-elements` was promoted
on that evidence and had to be **reverted and re-proved**. Its real answer is 14 untagged of 22, not 22.
Two guards now: find the block by slug first, and refuse to write rows unless one agrees with what
`validate` recorded for that fragment alone.

> **Rows that match the input exactly are the thing to distrust.** A real describer's answer *differs*
> from what it was given.

### One person, one `HERON_CLIENT_ID`

Four ids were in use for one afternoon — one by hand, two in scripts, one the runner picks itself. The
lease identifies a **chat**, so that is four chats competing for one Revit, and **every refusal reads
exactly like a fragment failing.** It caused an eleven-job wipe-out that was misread as an arrangement
fault, and the six stale verdicts above.

### Still open, and what tomorrow starts with

1. **`batch-prove`'s stale-record hole**, above. Small fix, and it protects every future batch.
2. **The rollback failed on seventeen renamed sheets** and counting could not see it — §1c, third
   incident. Two proof blocks carry the sentence *"rolled back, so the model was left exactly as it
   was"*, which is **false for `edit-text-values`** and **unverified for `set-view-section-box`**. The
   sentence comes from a flag in the client, never from anything Revit returns (§5 row 10).
3. **The `LIST_*` gap** — five fragments blocked purely for want of a name nothing can supply: a workset
   id, a template-free view, a legend name, a section mark, a material name. `list-worksets` reports
   names but not ids.
4. **Then keep proving.** 193 `DRAFT` remain, as of 2026-09-10 — derive it, do not read it here. Read §3i first — it now holds the arrangements that work.

**Revit was left busy with a dialog open at the end of the session.** If `heron_bridge_client.py count`
answers *"Revit is busy"*, look at the Revit window before anything else.

---
