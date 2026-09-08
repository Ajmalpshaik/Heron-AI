<!-- Heron-Agent:  none -->
<!-- Heron-Step:   17 -->
<!-- Heron-Status: DRAFT -->
<!-- Heron-Since:  0.1.0 -->
<!-- Heron-Layer:  brain -->
<!-- See docs/29-metadata-standard.md -->

# Fragments with something wrong — the sit-down list

**What this is.** Every fragment that was PUT IN FRONT OF A REAL MODEL and did not come away proved,
with the reason. Opened 2026-09-08 at the owner's request: *"we will sit for this specially, that
issued one we can do together."*

**What this is NOT.** It is not the list of unproven fragments — that is 301 and most of them have
simply not been tried yet. Everything here has been RUN. A fragment earns a row by failing, refusing,
or passing in a way that proves nothing.

**How to use it.** Read `Status` first. `NEEDS THE OWNER` means the model has to be arranged by hand
and nothing else will do. `SUSPECT` means it did something to Revit that has not been explained and it
should not be run again casually.

Derive the counts rather than trusting any typed here:

```bash
grep -h '^heron-status:' brain/fragments/*/fragment.yaml | sort | uniq -c
```

---

## 1. SUSPECT — these upset Revit, and why is not known

Both failed **cleanly** — `transaction.Commit()` did not return `Committed`, the write path rolled back,
and the element count afterwards was unchanged. Revit then raised its own dialog:

> *"A serious error has occurred. It is strongly recommended that you use Save As to save your work in
> a new file before continuing."*

The model was not damaged and nothing was saved. **Neither has been run since.** They are first on the
list to look at together.

| Fragment | Values used | What came back |
|---|---|---|
| `create-mep-system-type` | `copyFromName=Supply Air`, `newName=HERON TEST SYS`, `abbreviation=HTS` | `operation_failed` — "ran but Revit did not accept the change" |
| `assign-scope-box-to-view` | `views=Model Linking`, `scopeBoxName=Grids` | `operation_failed` — same |

**How to look at them safely:** one at a time, on a freshly opened model, with nothing else run in that
session — so that if Revit complains again it is unambiguous which one did it. Run READ-only first
(`fragment <name>` with no `--write`) to see how far the C# gets before the transaction matters.

---

## 1c. A WRITE INTERRUPTED BY A DIALOG DOES NOT FULLY ROLL BACK — 2026-09-08

**The most important thing found today, and the least expected.**

`transfer-views-between-documents` was run with `--write` and **no** `apply`, so the whole run should
have been undone. It raised a stream of Revit "Duplicate Types" dialogs, the owner answered OK to each,
and the run timed out at 60 s (`still_running`, then `revit_busy`).

**Afterwards the model held 60 more placed elements than it started with — 9,628 → 9,688.**

What was checked, and is clean: levels 11 (unchanged), worksets 2 with `Workset1` still named that,
drafting views 2 (unchanged), legends 0, **nothing named HERON anywhere**. Nothing any fragment
*created by name* survived. The 60 came in with the paste itself.

### Why this matters more than the count

The rollback is the whole basis of proving writes safely — every write proof recorded today says *"run
inside a transaction and ROLLED BACK, so the model was left exactly as it was."* **That sentence is
true for the 20-odd fragments that completed, and was not true here.** A guarantee with an unstated
exception is worse than a guarantee that is qualified.

### What is NOT known, and must not be guessed

- whether the `TransactionGroup` rolled back at all, or rolled back and the paste sat outside it
  (`Document.Paste`-style operations can commit their own transaction)
- whether the timeout is involved, or only the dialog
- whether the same happens with `transfer-materials-between-documents`, which failed identically
- **whether any completed proof from today is affected.** The element count was checked after the
  earlier batch and matched, so probably not — but "probably" is not the standard this file uses

### IT HAPPENED AGAIN, MUCH LARGER — 2026-09-09

`delete-elements` was run with `--write` and **no `apply`**. Its negative case selected `Levels`, and
this is what the record holds:

```
askedFor  11        the eleven levels
deleted   5636
alsoWent  5625      everything hosted on them, and their views
```

Afterwards: **9,628 → 3,966 placed elements, and every floor plan gone.** The rollback did not undo it.

**THE FRAGMENT DID NOTHING WRONG.** `alsoWent` exists to report exactly this and reported it. The
choice of Levels as a "harmless" contrast was mine, made without thinking about what a level carries in
Revit — and the fragment's own output is what said so.

**Two failures now, and the shape is the same both times:** a large operation (60 elements, then 5,636)
run with no `apply`, and the model left changed. Small writes roll back correctly — that has been
checked after every batch all day, and the `create-level` proof was verified element-by-element. Whether
the boundary is size, cascade depth, or Revit committing something of its own is **not known**.

Until it is: **run write proofs on a model you are willing to throw away, and check the element count
after every batch.** Both incidents were caught by that check and nothing else.

### `delete-elements` IS BLOCKED, not merely untested — 2026-09-09

Three attempts, three different failures, and **the fragment was correct every time**:

| Negative case chosen | What happened |
|---|---|
| `Levels` (11) | `alsoWent 5625` — 5,636 gone, rollback failed, 9,628 → 3,966 |
| `Duct Systems` (148) | Revit stopped answering. Forced close |
| — | (no third choice attempted) |

**In an MEP model almost everything is hosted on, or belongs to, something else.** A level carries its
views; a duct system carries its ducts. There is no category here that can be deleted in isolation, so
there is no safe negative case to find — the problem is not that the right one has not been picked yet.

It stays `DRAFT` and should not be attempted again **until the rollback is understood**. Running it is
how both of today's model wipes happened, and the second one hung Revit hard enough to need a forced
close.

`alsoWent` is worth keeping in mind for its own sake: the fragment reports the cascade in the same
answer, and both times it was right and was read too late.

### It never reached disk, and that is checked rather than hoped

### It never reached disk, and that is checked rather than hoped

The model was closed **without saving** and reopened: **9,628 placed elements**, exactly what it held
this morning. So the failure is confined to the live session — a rollback that does not undo everything,
not a file that ends up wrong. That is the difference between a bug and a data-loss bug, and it is worth
stating plainly next to the finding rather than leaving the reader to fear the worse one.

It also means the recovery is known and cheap: **close without saving**.

### Until it is understood

Run the two dialog-raising transfers **only on a model you are willing to throw away**, and check the
element count afterwards. The right fix is the one in §1b — collide-detect first, so no paste starts
that Revit has to interrupt — and it removes this failure as a side effect.

---

## 1b. NEEDS A HUMAN AT THE KEYBOARD — Revit opens a dialog Heron cannot answer

| Fragment | What happened |
|---|---|
| `transfer-materials-between-documents` | Copying materials from `Snowdon Towers Sample Architectural` into the scratch model, Revit raised **"Duplicate Types — Material Assets : Steel"** and waited. The run returned `still_running` after 60 s, and the next request was refused with `revit_busy`. The owner pressed OK and Revit carried on |
| `transfer-views-between-documents` | The same, `viewKind=drafting`: **"Duplicate Types — Callout Tag, Drafting View, Section Tag, Viewport"**. Also `still_running` then `revit_busy`. **Found after the row below was written**, which had said only one fragment did this — so the split is 2 against 3, not 1 against 3 |

**THE BRIDGE DID EXACTLY THE RIGHT THING and that half is a pass.** Both messages were the ones
[docs/03](03-heron-revit.md) asks for — *"Revit started the request but has not finished within 60
seconds. It is still working; do not repeat the request"*, then *"A dialog may be open... Finish what is
open in Revit and ask again"*. No hang, no wrong answer, and it named the cure.

**What cannot be fixed by trying harder:** a transfer that collides with an existing type stops and
waits for a person, every time. Heron cannot dismiss a Revit dialog and must not learn to — the dialog
is Revit asking a question only the modeller can answer, and the two choices produce different models.

**THREE OF THE FIVE ALREADY DO THE RIGHT THING, which settles the argument.** Proved the same
afternoon, against the same two models, with no dialog at all:

| Fragment | Clashes it met | What it did |
|---|---|---|
| `transfer-view-filters-between-documents` | 1 — *"Interior (already here)"* | Copied 54, **reported** the clash |
| `transfer-line-styles-between-documents` | 21 | Created 8, **reported** the 21 |
| `transfer-object-styles-between-documents` | — | Created 73, changed 124, weakened 15, skipped 1 |

So the fix for `transfer-materials-between-documents` **and `transfer-views-between-documents`** is not
a new idea to invent — it is the shape the other three already use: **look for the collision first,
decide it in the fragment, and report it**, rather than starting a paste Revit has to interrupt with a
question. Five fragments, one family, two of them written the other way.

---

## 1d. A ROLLED-BACK WRITE CLEARS THE SELECTION — and that blocks 100 proofs

Established by running it, 2026-09-08:

| Step | Selection afterwards |
|---|---|
| `select-by-category-name` chained into `set-selection` | **307** |
| a READ fragment | 307 |
| the same READ fragment again | 307 |
| **a WRITE fragment with no `apply`** — so nothing was kept | **gone** — `needs_unbound` |

Reads leave it alone. The rollback takes it with them.

### Why this is not a small thing

**100 fragments need a selection AND a caller value** — that combination is the whole remaining
population of provable work, and the negative case for every one of them is a second run with a
different value. `validate` runs both phases in one go, so the second phase always arrives at an empty
selection and refuses. `create-selection-filter` proved its positive this afternoon and could not reach
its negative for exactly this reason.

**It is arguably correct behaviour.** The selected elements were deleted and recreated by the rollback,
so the ids Revit was holding no longer exist; clearing is safer than pointing at ghosts. But correct or
not, it means no selection-based write fragment can be proved by the current tooling.

### FIXED, 2026-09-08 — `validate --setup`

`--setup <fragment>` names a fragment to run before **each** phase, repeatable and in order. It runs
with the POSITIVE values always, because it is the arrangement rather than the question: *"these
elements, selected"* is what both phases are asked about, and only the question changes.

```
validate create-selection-filter --write   --setup select-by-category-name --setup set-selection   --set categoryName=Ducts --set "inViewOnly=FloorPlan: L3"   --set filterName="HERON SEL Z9"   --negative-set filterName=Domestic ...
```

**`create-selection-filter` was the first selection-based WRITE ever proved** — 307 elements saved to a
new filter, 0 when the name was already taken. `assign-location-data` followed.

One thing had to be got right and was got wrong first: **only the first setup step resets the chain.**
The whole point is that step two consumes what step one left — `select-by-category-name` leaves
`elements`, `set-selection` needs them — and resetting between them threw that away. The symptom read
as a missing selection rather than a discarded one.

### The reasoning that led there, kept because it generalises

`validate` needs to re-establish the arrangement before EACH phase, not once at the start — the same
way it already sends different caller values per phase. A `--setup` chain naming fragments to run first
(`select-by-category-name`, `set-selection`) would do it, and the machinery already exists: `prove` runs
a chain in one lease, and D-29's chaining carried `elements` between those two fragments correctly on
the first try.

Until then, a selection-based write fragment can be RUN once and its positive recorded, but not proved.

---

## 2. CANNOT PROVE — Revit itself declines

None of these looks like a fragment defect. Each asked Revit to do something and Revit said no, for a
reason that reads as correct. **They stay `DRAFT` because a fragment that will not act cannot be shown
to act** — the positive case and the negative case come back identical, and D-30 exists to catch
exactly that.

| Fragment | What Revit said | Worth trying |
|---|---|---|
| `delete-revision` | *"Revit would not delete revision 1 — a revision cloud still uses it"* | A revision with **no** cloud on it. There is only one revision in Snowdon |
| `edit-revision` | *"Nothing changed on revision 1"* | Why. The values passed differed from what was there |
| `remove-view-template` | *"'L2' — Revit refused to detach 'Mechanical Plan': A managed …"* (message truncated in the record) | Read the full message. It may be the workshared model, or a template that is in use |

---

## 3. NEEDS THE OWNER — the model has to be arranged by hand

These ran correctly and gave one honest half of a proof. The other half does not exist in
`Snowdon Towers Sample HVAC` and cannot be conjured by any input.

| Fragment | Has | Missing | The arrangement |
|---|---|---|---|
| `read-graphic-overrides` | The empty answer — 0 of 625 elements | A positive | Select ducts → Override Graphics in View → By Element → red |
| `diagnose-visibility` | 625 visible, 0 reasons | Something invisible | Select one element → HH |
| `find-unused-materials` | 56 found | A model with none | A second, clean model — or accept tracking |
| `find-unused-families` | 64 found | A model with none | Same |

---

## 3b. NO POSITIVE CASE IN THIS MODEL — found 2026-09-08, second round

Ran correctly and returned the honest empty answer. The model simply has none of the thing.

| Fragment | What was tried | What came back |
|---|---|---|
| `select-openings` | `inViewOnly=FloorPlan: L3` | `elements 0` — there are no openings in that view. Needs a view with a wall or floor opening in it, or one drawn on purpose |
| `select-from-saved-set` | `filterName=Domestic`, `view=FloorPlan: L2` | `elements 0` — *"'Domestic' is a RULE. It matches 0 element(s) in view"*. Consistent with `audit-view-filters`, which found all four filters on the Mechanical Plan template are switched off AND carry an empty override. **The filters in this model do nothing**, so nothing here can prove a fragment that reads them |
| `report-areas` | `schemeNameContains=` (everything) | `areas 0` — the model has no Area scheme with placed areas. The negative returned 0 too, so the two cases are identical and nothing separates working from doing nothing |

---

## 3c. THE NEGATIVE CASE HAS NOT BEEN FOUND YET — 2026-09-08, third round

These have a **working positive**. What is missing is an arrangement in which the answer MUST be empty,
and the obvious one turned out not to be.

| Fragment | Positive | Why the obvious negative fails |
|---|---|---|
| `create-levels` | Created 2 levels at 45000 and 46000 | A **name clash does not stop it** — asked for `L2,L3`, which both exist, it created 2 levels anyway. Revit renames rather than refusing. Try an ELEVATION that already carries a level, so `alreadyThere` fires |
| `create-view-template-from-view` | Created template "HERON TPL Z9" from `Model Linking` | Same shape: *"the template was created and could NOT be called 'Mechanical Plan'"* — it made one under another name. The clash is reported, not obeyed |
| `create-sheet-list` | Created a sheet list with 2 fields | Same as the two above — asked for `Sheet Index`, a name already taken, it added the fields and made the schedule anyway. `refused` names the clash; `fieldsAdded 2` says it proceeded |
| `check-family-standards` | 97 family types reported off standard | **No negative exists in this model.** `namePattern=*`, which everything matches, still reports 97 — because "off standard" also counts families with `(0 placed)`. Needs a model whose families all pass, or a narrower question |
| `select-unenclosed-rooms` | **None.** Examined 67 rooms, `elements 0` | Every room in Snowdon is properly enclosed. Needs a room with a wall deleted |
| `create-key-schedule` | Created a key schedule | A name already taken does not stop it — **the fourth creator today to rename instead of refusing**. See the decision in §3d |
| `select-by-material` | **None.** `0 of 1053 scanned` | No duct in Snowdon carries a named material, so no material name gives a positive. Needs a category that does |
| `set-view-underlay` | Set an underlay between L2 and L3 | An EMPTY `baseLevel` is refused by Heron before the fragment sees it, so "clear the underlay" cannot be expressed. That is arguably a gap in the value passing, not the fragment |
| `reset-view-graphics` | Cleared 109 element overrides in `Model Linking` | The drafting view chosen as the negative had **8 overrides of its own** and cleared them. Not a failure — a real answer. Needs a view with none, and no view in Snowdon has none |
| `switch-active-project` | Reported `switched true` | Only ONE project is open, so it switched to the model it was already in. The handover's real question — *"check the tab bar afterwards, and never chain a write onto it assuming it landed"* — needs a second project open |
| `export-views-to-dwg` | **None.** `created 0` | *"name the DWG export setup"* — it will not use Revit's default, and this model has no named setup to give it. Needs one created in Export Setups first |
| `find-dead-ends` | **None.** `deadEnds 0`, `openEndsFound 0`, `stubs 0` | Consistent with `select-by-connection-status`, which proved every duct end in this model is connected. There are no dead ends to find. Needs a duct run with a loose end |
| `test-view-filter-match` | **None.** `matched 0` at both ends | The filter *is* found — `outOfScope` is 307 against 0 — but `Domestic` matches nothing, which `audit-view-filters` and `select-from-saved-set` both showed earlier. Third fragment blocked by the same fact about this model |
| `report-bounding-box` | 307 measured, combined 53957 × 22545 × 7137 mm | `maxRows` only limits how many rows are LISTED; `measured` and `combinedSizeMm` are the same whatever it is set to. There is no input that makes this fragment measure nothing, so the negative has to come from the selection — which no read fragment can change |
| `find-untagged-elements` | 235 untagged of 307 in `L3`, 72 already tagged | Its answer is the UNTAGGED ones, so it empties only when everything is tagged — and in a view with no tags at all it correctly returns all 307, which is a full answer rather than an empty one. `alreadyTagged` does go 72 → 0, so TRACKING ([D-53](DECISIONS.md)) would prove it |
| `report-geometry-complexity` | 28,244 triangles across 1 type | `maxTypes` and `listTop` only limit what is LISTED — every counter is identical at 10/10 and 0/0. Same shape as `report-bounding-box`: no input makes it measure nothing |
| `measure-run-quantities` | 307 quantities | **`measure=ZZZNOTHINGHERE` produced an identical answer to `measure=area`.** An unrecognised mode is silently ignored rather than refused, so the value has no effect at all and there is nothing to vary. That is a defect in the fragment, not only a proving obstacle — a caller asking for the wrong measure gets a confident wrong answer |
| `check-equipment-connectors` | 22 mismatched connectors on 10 units | **A LARGER tolerance found MORE mismatches, not fewer** — 22 at `sizeTolerance=99999` against 14 at `0`. That is backwards: a generous tolerance should accept more sizes as matching. Either the value is not in the units the name implies, or it is applied the wrong way round. Worth reading before it is proved, because a proof would freeze whichever behaviour is there |
| `select-subcomponents` | **None.** `elements 0`, `withoutSubComponents 10` | The 10 mechanical equipment units in `L3` are not nested families, so there are no subcomponents to select. Needs a family that has some |
| `report-parameter-inventory` | 71 parameters across 10 elements | `sampleOnly=true` reads ONE element and still returns 32 parameters. It narrows the sample, it does not empty the answer — the same shape as `report-bounding-box` and `report-geometry-complexity`, and the third fragment today whose only input controls how much is shown rather than what is found |
| `read-space-loads` | **None.** All 17 spaces return `noLoad` — *"Revit refused the f…"* | Exactly what HANDOVER.md predicted: *"every space in the model returns noLoad; a positive needs Areas and Volumes on with the analysis run, or a Design Heating Load typed on one space."* Confirmed against the model today rather than carried forward on trust |
| `find-overlapping-lines` | **None.** `overlapping 0` on 9 real detail lines, all straight | Nothing overlaps in the drafting view even at a 99,999 mm tolerance — which is itself worth a look, since at that tolerance almost any two lines in one view should qualify. Either the tolerance governs collinearity rather than distance, or it is not in millimetres. Read it before arranging a case |
| `copy-parameter-value` | **None.** `copied 0`, `typeMismatch true` | Copying `System Type` into `Comments` is refused because one is a type parameter and the other an instance one — correct behaviour, and not a positive. Needs two instance parameters of the same kind where the source actually holds something. `Comments` is blank on every duct in this model, which is what made it look like an easy choice |
| `group-elements` | Grouped 614 elements from 307 ducts | Every category tried can be grouped — spaces gave 17, not 0. Revit groups almost anything, so an empty case needs a selection it REFUSES rather than a different category. `refused` is the field to make fire |
| `ungroup-elements` | **None.** `ungrouped 0`, `notGroups 307` | Nothing in this model is grouped, and `group-elements` cannot leave one behind because its own run rolls back. Proving it needs a group that already exists in the model |
| `center-room-tags` | **None.** `centred 0`, `notRoomTags 17` | It wants ROOM tags. This is an MEP model — it has Spaces and Space Tags, not Rooms — so every tag in it is `notRoomTags`. Needs the architectural model, or a room placed in this one |
| `maximize-datum-extents` | **None.** The selector found no Levels in `FloorPlan: L3` | A level does not appear in its own plan view. It needs a section or elevation, where datums are visible — `Elevation: North - Mech` is the obvious candidate |
| `isolate-elements` | 307 isolated in a plan | 307 isolated in a drafting view too — `viewRefused false` both times. It isolates whatever it is handed, so it CANNOT come back empty; the drafting view was expected to refuse and did not. Same case as `count-elements`: prove it by TRACKING ([D-53](DECISIONS.md)) across selections of different sizes |
| `trim-extend-elements` | **None.** `moved 0` against ducts AND against tags | Nothing in the selection needed squaring up — the ducts here already meet cleanly. Needs two elements deliberately left short of each other |
| `create-assembly-views` | **None.** `created 0`, `notAnAssembly 307` | Ducts are not assemblies. Nothing in this model is — needs an Assembly created first, which is itself a Revit command with no fragment behind it |
| `zoom-to-elements` | Zoomed to 307 ducts | The negative selection — Duct Tags in a drafting view — does not exist, so the setup found nothing. Any category present in BOTH a plan and a drafting view would do; there may not be one |
| `add-schedule-fields` | **None.** `added 0`, `availableFields 0` | It needs a SCHEDULE in the selection, and a schedule is a view — it cannot be selected as an element in another view. This is the same wall HANDOVER.md hit on 2026-09-07: *"both schedule fragments refused the only object a person can select… a view cannot be selected as an element."* Selecting by category does not get round it either |
| `check-vertical-clearance` | 18 services too close at a 99999 mm required gap | `clashing` stayed at **5 in both runs** — it counts services actually touching, which does not vary with the gap asked for. So the fragment has two results and only one answers the question put to it. Either `clashing` is a separate finding that belongs in its own fragment, or the negative has to be arranged some other way |
| `set-category-visibility` | Hid Ducts in `Model Linking` — `changed 1` | Showing them again is also `changed 1`. The same two-position needle as `set-crop-box-settings`, and the same fix would serve both |
| `set-view-template-control` | 19 parameters held by the template, 9 freed | `nowHeld`/`nowFree` are the template's WHOLE state, so they are never empty whatever is asked. The result worth judging is *how many changed*, which the fragment does not report |
| `create-workset-3d-views` | Created one 3D view per workset (2) | An empty `namePrefix` still creates 2. There is no input that makes it create nothing |
| `select-by-electrical-circuit` | **None.** `elements 0`, `panels 0` | An HVAC model has no electrical circuits. Needs `Snowdon Towers Sample Electrical` |
| `set-crop-box-settings` | Turned the crop on in `Model Linking` — `changed 1` | Turning it OFF is also a change: `changed 1` again. The needle has only two positions and both move it. Needs a run that asks for the state the view is ALREADY in |
| `set-section-mark-visibility` | **None.** `hidden 0`, `shown 0` | There are no section marks in `L3` to make visible or hide. Needs a view containing one |
| `remove-view-filter` | **None.** `removed 0`, `deleted 0` | *"'L2' is governed by template 'Mechanical Plan', which owns its filters"* — a filter cannot be removed from a template-driven view at all. Needs a view whose filters are its own |

---

## 3d. GAPS — what proving showed is MISSING, not broken

Opened 2026-09-08 on the owner's instruction: *"if you find a gap also mention that, and new fragments
we need, we need to split this fragment, add new, edit — like that also mention in that list."*

Everything here was met by needing it, not by imagining it.

### New fragments the library wants

`list-levels`, `list-worksets`, `list-grids`, `list-sheets`, `list-revisions` and `list-linked-models`
all exist. The gaps below are the same shape and were each hit by having to write a throwaway probe
instead — several times in one session.

| Wanted | Why it was missed | How often today |
|---|---|---|
| **`LIST_VIEWS`** | `find-views` needs a `viewType` AND a `nameContains` before it answers. There is no way to ask *"what views are there"* — which is the first question of every proof that takes a view, and **53 fragments take one** | Every single view-based proof. The most-needed missing fragment of the day |
| `LIST_VIEW_TEMPLATES` | `select-view-templates` selects; nothing lists. Needed the names of all 18 before anything could be done with a template | 3 times |
| `LIST_LINE_STYLES` | `remap-line-styles` takes two style names and there is no way to discover one | 1 |
| `LIST_MEP_SYSTEM_TYPES` | `create-mep-system-type` takes `copyFromName` and nothing lists the 14 that exist | 1 |
| `LIST_MATERIALS` | `find-unused-materials` reports only the unused ones. There is no list of the materials that ARE used | 1 |
| `LIST_DWG_EXPORT_SETUPS` | `export-views-to-dwg` refuses without a named setup — *"Revit's default is not used"* — and nothing lists the setups a project has. It could not be proved at all for want of one name | 1 |

**The pattern is one sentence: every fragment that takes a NAME needs a way to discover the names.**
A caller who cannot discover a value cannot supply one, and [D-54](DECISIONS.md) made supplying them
possible without making them findable.

### …and two fragments already show the cheaper fix

**A new `LIST_*` fragment is not the only answer, and may not be the best one.** Two fragments proved
today already solve it for themselves, by handing back the alternatives **in the refusal**:

| Fragment | What it returns when it cannot match the name |
|---|---|
| `set-print-settings` | `availableSizes` — all 79 the print driver offers |
| `open-view` | `matches` — what the name DID match, and why it was refused: *"[view template, cannot be opened]"* |

One round trip instead of two, and the list arrives exactly when it is wanted — at the moment somebody
got the name wrong. **Deciding between the two shapes is part of the sit-down**, because doing both
means the same list is maintained in two places. The five `LIST_*` rows above are written as new
fragments only because that is how the library already answers this question elsewhere
(`list-levels`, `list-worksets`, `list-grids`); the convention below may be the better trade.

### One fragment to SPLIT

| Fragment | Why |
|---|---|
| `check-family-standards` | It answers two unrelated questions under one word. `namePattern=*`, which everything matches, still reported **97 off standard** — because "off standard" also counts families with `(0 placed)`. *"This family is named wrongly"* and *"this family is loaded and never used"* are different findings with different fixes, and `find-unused-families` already owns the second one. Merged, neither can be proved: there is no arrangement that empties both at once |

### A decision to make, then edits to follow

**A name clash refuses in some creators and renames in others**, and nothing says which is right:

| Refuses | Renames anyway |
|---|---|
| `create-level` | `create-levels` |
| `create-drafting-view` | `create-view-template-from-view` |
| | `create-sheet-list` |

Both behaviours are defensible. What is not defensible is that a caller cannot predict which they will
get, and it cost three failed negative cases today before the pattern was visible. **Decide once, then
edit the minority to match** — and note that `create-level` and `create-levels`, the singular and plural
of the same idea, are on opposite sides.

### One edit, small and specific

| Fragment | Edit |
|---|---|
| `set-crop-box-settings` | Its three settings accept `on`/`off`/`true`/`false`/`yes`/`no` — every value is a change. There is no way to say **leave this one alone**, so a caller wanting to turn the crop on without touching the annotation crop cannot. It also means the fragment has no empty case: both `on` and `off` report `changed 1`. Add a `leave` value, and make it the default |

---

## 4. FIXED during proving — kept because the shape returns

| Fragment | What was wrong |
|---|---|
| `report-category-visibility` | Used `CanCategoryBeHidden` as a **gate**. That call means *"may this view change it"*, not *"is it hidden"* — and a view driven by a template answers false to everything. It skipped all 68 categories and reported *"nothing is switched off"* while 18 genuinely were, **Levels and HVAC Zones among them**. Most views in a real project carry a template, so it was wrong in the ordinary case. `GetCategoryHidden` needed no help: asked of the view it returns exactly what the template returns when asked directly. Fixed 2026-09-08 |

---

## 5. HERON'S OWN DEFECTS found by proving — one still open

| # | Defect | State |
|---|---|---|
| 1 | No way to pass a view, category or name. **288 of 308 fragments blocked**, and the refusal said so in its own words | Fixed — [D-54](DECISIONS.md) |
| 2 | The chain silently dropped what a filter narrowed. `find-untagged-elements` cut 625 to 560 and the next fragment counted 625 | Fixed — identity, not name |
| 3 | `refused` read as a finding. **117 fragments**, every write among them, could never pass a negative case | Fixed — `role: accounting`, [D-52](DECISIONS.md) |
| 4 | `"(null)"` unreadable, so **every creator's** negative case was flagged | Fixed |
| 5 | `deploy-addin.ps1` installed a build for the wrong Revit release. Symptom was *"Revit cannot run the external application"* and nothing else | Fixed — the script refuses it now |
| 6 | `validate` sent writes down the READ path for one commit. Revit refused politely, the fragment reported `refused` like any decline, and it read as intermittent worksharing behaviour | Fixed — the line carries why |
| 7 | **The naming heuristics were dead code.** `provide_role()` answers `"result"` for an entry with no `role:` key, and that default went into the map the judge consults - so every declared name looked explicitly declared, and D-51/D-52's patterns never ran. **102 names across 134 fragments** (`scanned`, `unplaced`, `noConnectors`, `notASheet`) were judged as findings. Found proving `select-scope-boxes`, whose negative had every result at zero and `scanned: 5` | Fixed - the judge-set and the role-map are separate arguments now |
| 9 | **A fragment's OWN risk level was never enforced.** The gate reads the OPERATION's risk from the tool registry — Golden Rule 19, and right — and `run_fragment_write` is declared Modify. So a fragment declaring `risk: ADMIN` or `PUBLISH` ran under a Modify gate and nobody was consulted, though `HeronPermissions` says those *"are not reachable in Phase 0 or Phase 1 at all"*. **12 fragments are above Modify**, and `create-workset` (ADMIN) created one on the first try. The hole existed before `run_fragment_write` and was harmless — no transaction, so nothing could happen. Building the write path made it real | Fixed — the client refuses to SEND one. Be honest about what that is: a guard against a mistake, not a boundary against malice. A caller skipping this client is unaffected, and Golden Rule 19 forbids closing that by sending the risk over the wire, because then the caller decides how dangerous its own request is |
| **8** | **`Describe` renders a valid `ElementId` and `ElementId.InvalidElementId` as the same word, `"ElementId"`.** A negative case that correctly created nothing reads as though it created something, and only the fragment's source settles it — which is how `duplicate-view-template` was judged | **OPEN.** One line, but it needs the add-in rebuilt and redeployed, which costs a Revit restart |

---

## 6. WHAT CANNOT BE RUN AT ALL — 100 fragments, by what they need

Not failures. Heron has no way to receive these inputs yet, so they have never executed a line.

| Waiting on | Fragments | Why not done |
|---|---|---|
| `ElementId` | 21 | Its constructor changed from `int` to `long` at Revit 2024, and nothing in the add-in carries a version `#if` |
| `Element` (one, not a list) | 20 | Only element **lists** bind from the selection. **The cheapest of these to fix** |
| `XYZ` | 14 | The API works in feet, the library talks millimetres. Which unit a typed number is in has to be decided, not guessed — a units error is what `D3` exists to catch |
| `FamilySymbol` | 4 | Needs a family-and-type lookup rule |
| `View3D` | 3 | A narrower view lookup |
| element/id collections | 9 | Same as the two above |
| `OverrideGraphicSettings` | 3 | A structured value, not a name |
| everything else | 26 | One rule each |

---

## Add to this file, do not start another

A fragment that fails in front of a model belongs here the same day, with what was passed to it and
what came back **verbatim**. The value of the list is that every row was observed rather than expected —
the moment it fills with things somebody thought might be wrong, it stops being worth the sit-down.
