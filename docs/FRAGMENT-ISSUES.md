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

### …and `--setup` cannot run a step that WRITES — found 2026-09-09

Setup steps go through `run_fragment_read`, which opens no transaction, so a `MODIFY` fragment used as
setup throws *"Attempt to modify the model outside of transaction"*.

`show-elements` is the fragment that needs it: to prove it, something must be hidden **and stay
hidden**, so its arrangement is `hide-elements` — a write. And a write in the setup would have to be
APPLIED to survive into the phase, which is the one thing the whole rollback design exists to avoid.

**A genuine chicken-and-egg, worth deciding rather than patching:** either the setup gets its own
applied-then-undone bracket around both phases, or fragments that need a written arrangement are proved
by TRACKING instead. It is the same shape as `update-saved-set`, which needs a saved set that another
fragment creates and rolls back.

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
| `check-ceiling-coordination` | 307 ducts in L3, `tolerance=99999` | `outOfPlane 0`. A tolerance nothing can satisfy still found nothing, because the ceilings are in the architectural LINK and the executor skips linked documents by design. Needs a ceiling drawn in the host |
| `check-fixture-connectivity` | air terminals in M1, three services required | `missingService 0`. Demanding Supply, Return AND Exhaust of every terminal still found none incomplete. Needs a terminal with a service genuinely absent |
| `find-overlapping-lines` | 307 ducts, `toleranceMm=99999` | `overlapping 0`. **Not a tolerance defect — it works on model LINES, and a duct is not one.** Needs detail or model lines, which this selection never contained |
| `find-dead-ends` | 307 ducts, `stubLength=99999` | `deadEnds 0`, confirming the earlier run rather than resting on it. `select-by-connection-status` proved every duct end in this model is connected, so there is nothing to find at any stub length |

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
| `flip-elements` | **None.** `flipped 0`, `cannotFlip 144` | Air terminals in this model cannot be flipped — Revit refuses all 144. Needs a family that is flippable, which is a property of the family rather than of the fragment |
| `hide-elements` | Hid 307 ducts in `L3` | The negative selection — duct tags on the schedules sheet — does not exist there, so the setup found nothing. A category present in two different views is needed, and tags are view-specific by nature |
| `edit-parameter-text` | **None.** `edited 0`, `refused 307` | It refused every duct. `Comments` is writable, so the refusal is about something else — worth reading, because a fragment that refuses everything silently is the same shape as the eight schedule ones |
| `connect-open-ends` | **None.** `openEnds 0`, `connected 0` | Nothing here has an open end. `select-by-connection-status` proved that this morning — third fragment blocked by it, after `find-dead-ends` |
| `fillet-lines` | **None.** `created 0` on both legs | The nine detail lines in that drafting view do not meet at an angle a fillet can round. Needs two lines drawn to cross |
| `disallow-join` | **None.** `changed 0`, `unsupported 307` | Disallow-join is a WALL and beam idea; ducts do not support it. Needs walls, and Snowdon's are in the architectural link |
| `add-revision-cloud` | Not run | It needs `revisionId` as an **ElementId**, which Heron deliberately refuses to accept — see defect 8's neighbour in §6. The first fragment blocked by that decision rather than by the model, and a fair cost to weigh against it |
| `set-mep-justification` | **None.** `set 0` on both legs | *"0 run(s) set"*. Retried on 22 ducts as well as 307 — the same. Justification applies to a duct RUN and a flat selection of individual ducts is not one, whatever its size. The fragment needs whatever it counts as a run |
| `set-view-crop` | Cropped around 307 ducts | And around 73 tags in the negative — `enclosed 73`, `applied true`. It crops around whatever it is handed, so it CANNOT come back empty. Same family as `count-elements` and `isolate-elements`: prove it by TRACKING ([D-53](DECISIONS.md)) across selections of different sizes |
| `update-saved-set` | **None.** *"No saved set called…"* on both legs | The set it was pointed at was created by `create-selection-filter` in an earlier run — which rolled back, taking the set with it. **A fragment that edits what another fragment creates cannot be proved while both roll back.** Needs a saved set that already exists in the model |
| `edit-text-values` | **None.** `changed 0`, `absent 7` on the positive | Handed 7 text notes with `field=text` it reported all seven as `absent` — so `text` is not the field name it wants. The valid values are not written anywhere findable; `mode` accepts prefix/replace/suffix, but `field` was not discoverable from the source. **Another instance of the LIST_ gap in §3d** — a fragment taking a name with no way to learn the names |
| `compare-elements` | 29 differing parameters across 8 compared ducts | Handed tags instead it still found **1** differing — an empty negative needs elements that are genuinely identical, or fewer than two so `tooFewToCompare` fires. `select-by-category-name` cannot narrow to one, so this needs a way to select exactly one element |
| `check-flow-direction` | **None.** `bothIn 0`, `bothOut 0` — 15 joints checked, 25 skipped as bidirectional | Exactly what HANDOVER.md predicted on 2026-09-07: *"it works, there is simply no fault in Snowdon Towers to find. Its positive needs two connectors both set to Out, built in the Family Editor."* Confirmed against the model rather than carried forward |
| `read-graphic-overrides` | **None.** `withOverride 0` on both legs | No element in this model carries a per-element override. Same blocker as `report-category-overrides` had, and the same cure — the Architectural model has views that do |
| `audit-mep-openings` · `check-ceiling-coordination` · `check-fixture-connectivity` · `place-mep-fitting` · `auto-size-pipe` | **None.** Every declared result came back zero | Each needs model content this one lacks: openings through walls, host ceilings, plumbing fixtures, a gap wanting a fitting, pipes. Snowdon HVAC has ducts and its architecture is a LINK |
| `color-by-parameter` | Coloured 22 ducts by System Type | A parameter name that does not exist still coloured 22 — it falls back rather than refusing, so the input cannot empty the answer. Worth reading: silently ignoring an unknown parameter is the same shape as `measure-run-quantities` |
| `remove-parameter-value` · `set-mep-slope` | **None.** `cleared 0` and `sloped 0` on both legs | `remove-parameter-value` found Comments already empty on all 22 — an earlier write had rolled back. `set-mep-slope` found 20 of 22 with both ends connected, which cannot be sloped without moving what they join. Both need arranging, not fixing |
| `rename-elements` · `rename-family` · `unload-links` · `reload-links` · `set-design-option` · `set-schedule-filters` · `duplicate-sheets` · `align-viewports-across-sheets` | **None.** Every declared result zero, or the positive never ran | Each wants content or a selectable category this model does not offer in the view chosen — sheets and viewports are not in a floor plan, there are no design options, and the link and family categories did not resolve. Re-run once the batch tool exists, with the arranging rules applied |
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
| | `create-key-schedule` |
| | `duplicate-type` — *"'Tees' duplicated as 'Tees'"* |

**Five against two now**, so the majority behaviour is renaming. If the decision goes that way it is two
fragments to change, not five — but it also means a caller cannot rely on a clash being refused
anywhere, which is the more important half.

Both behaviours are defensible. What is not defensible is that a caller cannot predict which they will
get, and it cost three failed negative cases today before the pattern was visible. **Decide once, then
edit the minority to match** — and note that `create-level` and `create-levels`, the singular and plural
of the same idea, are on opposite sides.

### One edit, small and specific

| Fragment | Edit |
|---|---|
| `set-crop-box-settings` | Its three settings accept `on`/`off`/`true`/`false`/`yes`/`no` — every value is a change. There is no way to say **leave this one alone**, so a caller wanting to turn the crop on without touching the annotation crop cannot. It also means the fragment has no empty case: both `on` and `off` report `changed 1`. Add a `leave` value, and make it the default |

---

## 3e. THE SCHEDULE WALL HAS A DOOR — found 2026-09-09

**Ten fragments read or edit a schedule by looking for one in the selection**, and a schedule is a
VIEW — it cannot be selected as an element. HANDOVER.md hit this on 2026-09-07: *"both schedule
fragments refused the only object a person can select… a view cannot be selected as an element."*

**The way through was already in the library and nobody had written it down.**
`report-schedule-definition` was proved on 2026-09-08 against *"the 'Heat Recovery Unit Summary'
schedule, placed on a sheet and clicked"* — a `ScheduleSheetInstance`, which IS an element.

**And it can be done without clicking.** The category is called **`Schedule Graphics`**:

```
prove select-by-category-name set-selection   --set categoryName="Schedule Graphics"   --set inViewOnly="Notes, Symbols & Schedules"
```

Three instances on that sheet. `read-schedule-contents` — listed as *"fixed and UNPROVEN"* since
2026-09-07 — was proved through it immediately: 3 schedules, 144 body rows, against 307 non-schedules
skipped.

### …and behind the door, EIGHT fragments never got the fix

Tried through it, four of them, and every one did **nothing** — `added 0`, `changed 0`, `sorted 0`, and
tellingly `availableFields 0` and `presentFields 0`, meaning they never saw the schedule at all.

The reason is exact. **Only four fragments in the library understand `ScheduleSheetInstance`:**

| Understands it | Status |
|---|---|
| `report-schedule-definition` | PROVEN |
| `read-schedule-contents` | PROVEN 2026-09-09 |
| `place-schedule-on-sheet` | DRAFT |
| `find-unplaced-views` | — |

**Eight cast to `ViewSchedule` and nothing else**, so a schedule on a sheet slides straight past them:

`add-schedule-fields` · `add-schedule-combined-field` · `remove-schedule-fields` ·
`set-schedule-appearance` · `set-schedule-filters` · `set-schedule-sort-group` ·
`add-revision-cloud` · `export-schedule-to-csv`

The 2026-09-07 fix HANDOVER.md describes — *"they demanded a `ViewSchedule`; clicking a schedule on a
sheet gives a `ScheduleSheetInstance`"* — was applied to the two READ fragments and not to the eight
that edit.

**AND THEY FAIL SILENTLY.** Handed a schedule on a sheet they report `0 changed` with no refusal, which
reads as *"there was nothing to do"* rather than *"I could not see what you gave me"*. That is the
failure D-30's negative case exists to catch, and it is why none of them can be proved: both legs come
back identical because both legs did nothing.

**The fix is one line each and already written twice.** Do it once for all eight rather than per
fragment — a shared helper that resolves a selection to the `ViewSchedule` behind it, whichever form
arrived, is the shape the library wants.

---

## 3f. PROVED, BUT QUERIED — worth a second look at the sit-down

Marked `PROVEN` and standing, but the owner raised a doubt on the day and it is recorded rather than
argued away. A proof nobody questions is not the same as a proof that survived being questioned.

| Fragment | Proved on | The doubt |
|---|---|---|
| `set-view-section-box` | 22 elements enclosed in `3D HVAC Layout`; the same call on `FloorPlan: M1` returned `viewRefused true`, `applied false`, 0 enclosed | **The negative may be testing Revit rather than the fragment.** A plan view *cannot* have a section box, so the empty answer is guaranteed by the view type and not by anything the fragment decided. A stronger negative would be a 3D view where the selection has no geometry to enclose — then the fragment has to reach the same conclusion by its own work |

**If the doubt is upheld, the remedy is to re-run it, not to un-prove it by argument** — and D-30's
fingerprint means the record says exactly what was run, so a better arrangement can replace it cleanly.

---

## 3g. RUNNING FRAGMENTS IN BULK IS A WORKING NEED, NOT A TESTING ONE

Raised by the owner, 2026-09-09: *"maybe sometimes while we are working we need to run fragments in
bulk — is there an option?"*

**There is, and today depended on it.** `prove` runs several fragments in ONE process, ONE lease, with
D-29's chain carrying values between them:

```
prove select-by-category-name set-selection --set categoryName=Ducts --set "inViewOnly=FloorPlan: M1"
```

Two fragments, one job: the first finds 22 ducts, the second selects them. Every selection-based proof
today went through that, and the binding note on the answer says where the elements came from —
*"elements from select-by-category-name (22)"*.

### The name is the problem

**`prove` is a working command wearing a testing name.** Nobody doing real work would think to reach for
it, and that alone hides the capability the library already has. Renaming or aliasing it to `run` is
ten minutes and costs nothing.

### The real version is designed and unbuilt

| | |
|---|---|
| `HERON-KRN-WFL-007` **Workflow Engine** | *"Ordering, retries, timeouts, rollback, checkpoints, resume. Never decides…"* |
| `HERON-ORC-MAIN-001` **Orchestrator** | Understands the request, selects capability, builds and runs the work |

[docs/11](11-orchestration-and-workflows.md) is the whole design, and its own reference workflow for
*"select all ducts"* is eleven steps long — so multi-fragment work is not an edge case in this
architecture, it is the ordinary case.

### The ORDER is already computed — asked 2026-09-09, and the answer is better than expected

*"For arranging the correct order, is there something?"*

**Yes, and it works today.** `brain/heron_graph.py` (`HERON-KRN-DEP-013`) derives the edges from the
contracts — *"fragment → fragment, from the contracts: A provides what B needs"*:

```
$ python brain/heron_graph.py FRG-SEL-001          # set-selection
  could run after it   nothing
  could run before it  FRG-SEL-002, FRG-ELE-001, FRG-MEP-030 … 50 fragments
```

It says so itself: *"Every line above was computed just now from the fragments themselves. Nothing here
is stored, so nothing here is stale."* That is [D-40](DECISIONS.md) — an edge is derived, never written
down twice.

**So ordering is not a missing capability. It is a missing COMMAND.** The graph answers *"what can run
before this one"* for a single fragment. What a bulk runner needs is the other shape: *"here are five
fragments — what order?"*, which is a topological sort over edges that already exist.

`HERON-DEV-PLN-002` **Planning Agent — "Sequences the work"** is the registry slot for the judgement
part, and is unbuilt. But the hard half — knowing which fragment can feed which, across all 360 — is
done.

### Three different things, worth not confusing

| Thing | For | State |
|---|---|---|
| `prove` | Run several fragments now, in a line | **Works today**, misnamed |
| `tools/batch-prove.py` | Run many PROOFS and judge them | Being built by a separate session |
| Workflow Engine | Real work: retries, rollback, resume | **Designed, not built** |

### What today showed is missing, concretely

**When a chain half-succeeded there was no resume.** `select-by-category-name` would run, `set-selection`
would refuse because the category was not in that view, and the only option was to re-run both. On a
two-step chain that is trivial. On the eleven-step workflow docs/11 describes, it is not — and that is
exactly the gap `HERON-KRN-WFL-007` exists to fill.

---

## 3h. FOUR THINGS THAT WOULD IMPROVE THIS, RANKED — 2026-09-09

Asked for by the owner at the end of two days of proving. Ranked by what they would have saved
**today**, not by how interesting they are.

### 1. MAKE SILENCE ILLEGAL — do this one first

A fragment handed something it cannot use reports **`0`** instead of refusing. `0 changed` and *"I could
not see what you gave me"* are then the same sentence.

| Fragment | Given | Answered |
|---|---|---|
| the eight schedule fragments in §3e | a schedule on a sheet | `added 0`, `changed 0`, `sorted 0` — no refusal |
| `measure-run-quantities` | `measure=ZZZNOTHINGHERE` | identical to a valid mode |
| `color-by-parameter` | a parameter that does not exist | coloured 22 elements anyway |
| `edit-text-values` | `field=text` | all seven notes reported `absent` |

**This cost more time today than anything else**, because every one of them looks exactly like a
fragment correctly finding nothing — which is also why none of them can be proved: both legs come back
identical.

**And on a real project it is worse than slow. It is a confident wrong answer.** A modeller who asks for
the wrong measure gets a number, not a question.

> **A fragment that cannot use its input must REFUSE and say why. It must never report zero.**

Every fragment already has a `refused` output, and 117 of them now declare it as accounting. The
machinery is there; the discipline is not.

### 2. Finish declaring the roles

**40 provides across 39 fragments** still do not say whether they are an ANSWER or BOOKKEEPING, so both
the judge and the batch runner have to guess — and on 2026-09-09 the runner guessed wrong twice and came
within one step of banking a proof for a fragment that had done nothing (`remove-parameter-value`,
`set-mep-slope`).

`already*` was done today across 21 fragments. `both*`, `without*`, `outside*` and the rest remain, and
each needs a decision rather than a pattern — `bothIn`/`bothOut` on `check-flow-direction` match the
shape and ARE the answer.

### 3. Ask the model once, not six times

Six throwaway probe scripts were written today asking the same kinds of question: what views exist, how
many ducts per view, what connector sizes, which categories hold anything. Every one a round trip.

One command answering *"describe this model"* — views by type, categories by count, the sizes actually
present — removes all six, and it is the same gap as the missing `LIST_*` fragments in §3d.

### 4. Generate the job file

`tools/batch-prove.py` takes a hand-written job list. Most of that list is derivable: which fragments are
still DRAFT and untried, whether `--write` is needed, the setup chain, and **the exact input names** —
six were mistyped today, `widthMm` for `width` and `sortByFields` for `sortFieldNames` among them.

It must leave the category and the view **blank rather than guessing**. A wrong category produces a
confident meaningless result, which happened eleven times in one batch.

### Why the order matters

Two, three and four make the PROVING faster. **One makes HERON honest**, and a tool that quietly gives
wrong answers is worse than a slow one.

---

## 3i. THE ELEMENT-SHAPED WALL — found 2026-09-09, second sitting

Eighteen MODIFY fragments were run in two rounds against `Snowdon-scratch_ajmal.al`, chosen because
they had **no run record at all** — they had never been in front of a model. Three proved.
`tools/jobs/modify-never-run.yaml` and `tools/jobs/modify-round-2.yaml` are the arrangements, kept so
the next round starts from what was learned rather than from the fragment list.

| Proved | Positive | Negative |
|---|---|---|
| `set-element-level` | `moved 1`, `alreadyThere 21` — 21 of the 22 ducts were already on L2 | `moved 0`, `refused 9` |
| `array-elements-radial` | `count=2` → `created 22`, `copiesEach 1` | `count=1` → `created 0`, and it says why: *"a count of 1 asks for no copies at all"* |
| `renumber-sequential` | `renumbered 22`, planned HZ1–HZ22, `collisions 0` | `renumbered 0`, `refused 9` |

`array-elements-radial` is the best-shaped proof of the three: **both legs are the same selection** and
differ only in the value, so it tests the input rather than the arrangement. Where a fragment allows
that, prefer it.

`set-element-level` is the first fragment that **needed [D-54](DECISIONS.md) to exist** — it was run
earlier the same day, with no value, and refused by name rather than guessing.

### The wall itself

[D-54](DECISIONS.md) resolves a **view, a level, a category, a name, a number, or true/false — and
lists of those.** It refuses everything else by name, with a reason. `set-global-parameter` states the
boundary better than this file can:

> *"Heron can be handed a view, a level, a category, a name, a number, or true/false - and lists of
> those. `ParameterValue` is not one of them yet, so this fragment still has no way to receive it."*

**That refusal is correct and must not be softened.** Guessing which `FamilySymbol` was meant is how a
job runs against the wrong thing and reports success. But it is now the binding constraint: of the
**24** MODIFY fragments with only simple caller values and no run record, **more than half cannot be
arranged at all** because they want one of these:

| Shape wanted | Fragments blocked on it |
|---|---|
| `FamilySymbol` | `set-sheet-title-block`, `distribute-along-run`, `place-accessory-on-run` |
| `OverrideGraphicSettings` | `override-graphics-in-view`, `set-link-graphics`, `apply-view-filter` — and `read-graphic-overrides` already returns this type *unreadably*, so it is one finding about one type, not four |
| `Element` / `Material` / `Curve` | `align-elements`, `join-geometry`, `match-element-type`, `replace-material`, `set-view-crop-to-shape`, `create-from-room-boundaries` |
| `ParameterValue` | `set-global-parameter` |

**This is the next unlock after D-54, and it is the same shape of unlock.** D-54 took the caller's half
across as text and resolved it inside Revit where the document is. The same argument applies here: a
type name, a material name, a title-block name are all things Revit can look up — what is missing is
the resolver, not the possibility. The three that genuinely cannot work this way are
`OverrideGraphicSettings`, `Curve` and bare `Element`, because there is no name to look up.

### Two traps that cost a round each

**A HAND-RUN COMMAND'S LEASE FAILS THE WHOLE BATCH, AND LIES ABOUT WHY.** Round one failed **all
eleven jobs** immediately after a `count` check. `batch-prove` pins its own client id, the hand-run
command had pinned another, and the lease is per chat and lasts five minutes. The failures did not say
`session_in_use` — they said `needs_request_values` on the first two and *"Could not identify the
active model"* on the other nine, which reads exactly like an arrangement fault. The same job file ran
clean after `heron_bridge_client.py release`, with no other change.

> Release before batching. And distrust a batch where *every* job fails the same way — that is the
> environment, not the arrangement.

**SHEETS AND VIEWS CANNOT BE SELECTED BY CATEGORY — BUT THEY CAN BE SELECTED.** `manage-sheet-sets`
and `duplicate-views` both answered `setup_failed: the arrangement could not be re-made`, with and
without `inViewOnly`. Rule 3 says a category has to be visible where you select, and a sheet is not
*in* a view, it **is** one — so `select-by-category-name` cannot reach it.

**The conclusion first written here — that this blocks every sheet and view fragment — was wrong, and
it was wrong for an hour.** [`list-sheets`](../brain/fragments/list-sheets) is already `PROVEN`, needs
nothing but the document, and provides `elements`, which is exactly what `set-selection` consumes:

```yaml
    setup:
      - list-sheets
      - set-selection
```

That arranges all 17 sheets in one step. `list-levels`, `list-grids`, `list-revisions` and
`list-linked-models` do the same for their own kinds, and **every one of them is already PROVEN and was
sitting unused.** The §3d gap was never *"nothing lists these"* — it was that nothing named them where
somebody writing a job file would look. `tools/jobs/list-as-setup.yaml` is the worked example, and **five fragments proved through that route on the same day** — `edit-revision`, `duplicate-sheets`, `select-view-templates`, `manage-sheet-sets` and `remove-view-template`.

Three of the five were held up by an arrangement fault rather than by the fragment, and **each one named its own fault**: *"Mode 'add' is not one of create, rename, delete"*, *"No revision with sequence number 99. LIST_REVISIONS is where that number comes from"*, *"No sheet set called 'ZZZNOTHINGHERE'"*. A fragment that refuses in those words costs one run to correct. One that answers `0` costs a morning — which is the whole argument of §3h.1, seen from the other side.

### The rest of the eighteen, by what they need

| Fragment | What came back | What it needs |
|---|---|---|
| `dimension-rooms` | `created 0` with `view` correctly supplied | CONFIRM the L3 Spaces are bounded and that `measureTo=finish` is a mode it knows |
| `place-flow-arrows` | `placed 0` in both legs | The arrow family loaded. Both legs empty is the family missing, not the fragment failing |
| `create-legend-view` | *"No view called `Legend: Mechanical Legend`"* — a good refusal | The real legend name. Nothing lists them (§3d again) |
| `set-element-workset` | `moved 0` with `worksetId=0` | A workset **id**, and the gap is narrower than first written here. `list-worksets` is PROVEN and reports the names and the count — this model has two, *Shared Levels and Grids* and *Workset1*, both open — but **not the integer ids**, and the fragment reads `ELEM_PARTITION_PARAM` as an integer, so a name cannot stand in. One field added to `list-worksets` closes it |
| `place-views-on-sheet` | `placed 0`, and the negative was not empty either | Views selected, which is the sheet/view wall above |
| `align-viewports-across-sheets` | `aligned 0`, but `scaleMismatch 10` and `ambiguous 6` of 17 sheets | Sheets at one scale carrying one viewport each. Blocked on model content, not code — and the fragment said exactly why, which is the behaviour §3h.1 wants |
| `set-section-mark-visibility` | `setup_failed` twice — `categoryName: Sections`, model-wide and scoped to a plan | **Nothing in the library reaches section marks.** `select-by-category-name` cannot, and there is no `list-sections`. A gap row, not a defect |

### Reading the 196 run records was worth more than running anything — 2026-09-09

Every run record was re-read against its fragment's own contract. **80 DRAFT fragments have both
phases recorded**, and the shape of what stops them is now known rather than guessed:

| | |
|---|---|
| **25** | positive moved, negative **not** empty |
| **51** | positive never moved |
| **4** | already satisfied D-30 and nobody had noticed |

Two of the four were proved the same hour. **A run record is evidence that keeps**, and re-reading the
pile found more than the next batch did.

### How the judge decides "empty", exactly — because two rows of this file guessed at it

`looks_empty` in [`heron_validate.py`](../brain/heron_validate.py) filters to the contract's declared
names, drops anything declared `role: accounting`, and then, per value:

1. `_is_helper_object(value)` → **skipped**. A bare type name like `"ElementId"` or `"Func\`2"` is a
   rendering artefact, not an answer.
2. `_as_count(value)` returns `None` → **`return False` immediately.** Unreadable is not empty, and
   saying so is the whole point.
3. A non-zero count → not empty.
4. Nothing countable at all → not empty. There was no number here to have been zero.

**Step 1 is why `dimension-mep-runs` and `dimension-family-instances` proved** with `dimensionId`
reading as `"ElementId"` in all four phases: it is skipped, never reaching step 2. §5 row 8 assumed it
reached step 2 and blocked them. It did not.

**Step 2 is real, though, and it bites a NAME.** `report-geometry-complexity` returns
`heaviestTypeName: "Duct Size Tag: Duct Size Tag"` in its negative — a genuine string, not a helper
object, so it stops at step 2 and the negative can never be judged empty. Declaring the two work
counters (`typesMeasured`, `typesUnmeasured`) as accounting was right and is kept, but it does not
unblock it, and no role change should: **the fragment describes whatever it is handed**, so it has no
empty case at all. It belongs with the D-53 tracking group in §3c, not here.

> A declared result that is a NAME cannot carry the negative leg. Either the fragment has a countable
> result beside it, or it is a describer and D-53 is the route.

### Four more for "make silence illegal" (§3h.1)

All four create something **in both legs**, which is why none can be proved — and all four are worse
on a real project than in a proof:

| Fragment | Positive | Negative |
|---|---|---|
| `create-view-template-from-view` | created `HERON TPL Z9` | asked for a name it could not use, **created `Model Linking Copy 1` anyway** and reported the refusal beside it |
| `duplicate-type` | `'Tees' duplicated as 'HERON TYPE Z9'` | given no new name, **`'Tees' duplicated as 'Tees'`** |
| `create-key-schedule` | created `HERON KEY Z9` | created `Duct Style Schedule` |
| `create-levels` | `created 2`, `nameRefused 0` | **`created 2` AND `nameRefused 2`** — it rejected both names and made both levels anyway |

A fragment that cannot use the name it was given should refuse it, not invent one. `duplicate-type`
producing a second type called `Tees` is the clearest case: nothing downstream can tell the two apart.

**`create-levels` is the most costly of the four**, because a level is not a type. It reports
`nameRefused 2` and `created 2` in the same breath — it rejected both names and built both levels
regardless, leaving two default-named levels in the model. Levels are what the 5,636-element rollback
failure of 2026-09-09 was about (§1c), so a fragment that creates them when it has already decided it
cannot do the job is the one to fix first of the four.

**And the counter-example is in this same file.** `duplicate-sheets`, given no prefix and no suffix,
duplicated NOTHING and named all 17 numbers that would have collided. Same situation, same kind of
fragment, opposite behaviour. It is worth reading before touching any of the four, because it shows the
refusal already has a shape in this library — it is not being invented.

**The first three are also obscured by the `Describe` defect** — each returns its new element as
`"ElementId"` — but fixing that would not prove any of them, because the negative would still have
created something. The naming defect is the one that matters.

`create-levels` is the exception, and it is worth knowing why: it returns `created` as a **list**, so
`Describe` renders a count rather than the bare word. Nothing is hiding its behaviour. It reports
`created 2` and `nameRefused 2` in plain sight, and it is still wrong.

### Value-driven negatives: four tried, none proved, three findings — 2026-09-09

Thirty DRAFT fragments have both legs recorded with a positive that moved and a negative that did not
come back empty. Re-reading them showed the contrast was usually in the wrong place: the negative was a
different *selection*, and a fragment that acts on whatever it is handed acts on that too.
`isolate-elements` isolated 307 either way; `group-elements` grouped whatever arrived.

So four were re-run with the negative driven by the **value** instead —
[`tools/jobs/value-driven-negatives.yaml`](../tools/jobs/value-driven-negatives.yaml). None proved, and
three of the four produced something better.

| Fragment | Positive | Negative | What it means |
|---|---|---|---|
| `check-equipment-connectors` | `sizeTolerance=0` → `mismatched 22` | `sizeTolerance=99999` → `mismatched 21` | **The tolerance barely does anything.** A tolerance of 99999 should make every connector size acceptable; it removed one. This is the `measure-run-quantities` shape — a number the caller supplies that does not change the answer — and a modeller setting a tolerance would get a confident wrong list |
| `isolate-elements` | `view=FloorPlan: M1` → `isolated 22` | `view=Cover Sheet` → `isolated 22`, `viewRefused false` | **Identical answers from different views.** Either `view` is not being honoured or a sheet really can take a temporary isolate. Worth settling: the same shape as `color-by-parameter`, which coloured 22 elements by a parameter that does not exist |
| `set-crop-box-settings` | `changed 1` | `views="Cover Sheet"` → `changed 1` | Changed a crop setting on a sheet. Same count both legs |
| `set-category-visibility` | `changed 0` | `changed 0` | **Not a defect — and the explanation is already in this file.** M1 follows the `Mechanical Plan` view template (`remove-view-template` reported it in the same session), so the view cannot change category visibility at all: the template owns it. This is the trap that `report-category-visibility` was fixed for in §4 — *a view driven by a template answers false to everything*. Any V/G fragment must be proved on a view carrying NO template |

### `--write` serves EXECUTE, not only MODIFY

`isolate-elements` first answered `fragment_threw: Attempt to modify the model outside of transaction`,
and the job file was the reason: it carried `write: false` because the fragment is `risk: EXECUTE`
rather than `MODIFY`. That was a guess and it was wrong.

Temporary Hide/Isolate changes no model data — the fragment says so itself, *"nothing here is saved
into the view"* — but Revit still requires a transaction to call it. Re-run with `--write` both legs ran.

> **`write:` follows whether the fragment needs a TRANSACTION, not whether its risk is `MODIFY`.**
> Only `READ` is refused the write path. An `EXECUTE` fragment that touches the document needs
> `write: true` exactly like a `MODIFY` one.

Four of the six `EXECUTE` fragments are still DRAFT — `isolate-elements`, `set-selection`,
`switch-active-project`, `zoom-to-elements` — and `set-selection` runs in every setup chain without a
transaction, so the need is per-fragment rather than per-risk.

### Tracking (D-53) proved five, and the harness nearly proved a sixth wrongly — 2026-09-09

`set-selection` was still DRAFT while running in **every setup chain in every job file** — everything
proved this week leaned on it. It provides one thing, `selectedCount`, and it is the count of whatever
it was handed, so no arrangement makes it empty and D-53 is the route. It now tracks 22, 9, 10, 10, 307
across five selections.

| Proved by tracking | Field | Answers across five inputs |
|---|---|---|
| `set-selection` | `selectedCount` | 22, 9, 10, 10, 307 |
| `report-geometry-complexity` | `totalTriangles` | 2024, 0, 1320, 3920, 28244 |
| `report-parameter-inventory` | `parameterNames` | 93, 22, 86, 71, 93 |
| `find-untagged-elements` | `elements` | 14, 9, 10, 10, 307 |

`compare-elements` was proved the ordinary way instead, and the reason matters: tracking showed duct
tags giving `differing 0`, so it **can** come back empty. D-53 is for fragments that cannot. Given a
real negative it answered `differing 29` against `differing 0`.

### A harness bug that reads like a clean proof

The tracking runner drives a CHAIN — `select-by-category-name` → `set-selection` → the fragment — and
`prove` prints one provides block per fragment. The first version searched the whole output for the
field name. `elements` is declared by the selector as well as by most fragments under test, so it read
the **selector's** copy and produced five rows that tracked the input perfectly, because they *were*
the input.

**`find-untagged-elements` was promoted on that evidence and had to be reverted.** Its real answer for
the L3 ducts is 235 untagged, not the 307 it was handed. Two guards now, because the first alone is not
enough:

1. Find the fragment's own block by slug, then the field inside it.
2. **Refuse to write any rows unless at least one agrees with what `validate` recorded for that
   fragment running alone.** This is the guard that would have caught it: not one of 22, 9, 10, 10, 307
   matched the recorded 235 or 307.

> Rows that match the input exactly are the thing to distrust. A real describer's answer *differs* from
> what it was given — 22 ducts, 14 untagged.

### Two counters that were not defects, checked before filing

- `compare-elements` answered `comparedCount 8` for every input including 307 ducts. The code is
  `elements.Take(8)` and the comment says *"comparedCount says how many were actually looked at"*.
  Deliberate and documented. Declared `accounting`, along with `identicalCount`.
- `select-subcomponents` answered `elements 0` for all five, agreeing with its own recorded run.
  Nothing in this model has nested components. Content, not code.

**The naming patterns catch a shape, not a meaning.** `comparedCount` ends in `Count`, not `Compared`,
so the work-counter scan missed it entirely. That is the argument for §3h.2 doing the declarations
explicitly rather than leaving the patterns to guess.

### What this says about where the proving goes next

The MODIFY pool is not blocked on the write engine any more — three fragments proved through it today
and rolled back cleanly. It is blocked on **two resolvers and one list**:

1. A resolver for named Revit objects — `FamilySymbol`, `Material`, and the view-like ones.
2. `LIST_*` fragments for sheets, views, legends, worksets and global parameters, so a job file can be
   written against what the model actually holds instead of a guess.

Both are offline work. Neither needs Revit to build.

---

## 4. FIXED during proving — kept because the shape returns

| Fragment | What was wrong |
|---|---|
| `report-category-visibility` | Used `CanCategoryBeHidden` as a **gate**. That call means *"may this view change it"*, not *"is it hidden"* — and a view driven by a template answers false to everything. It skipped all 68 categories and reported *"nothing is switched off"* while 18 genuinely were, **Levels and HVAC Zones among them**. Most views in a real project carry a template, so it was wrong in the ordinary case. `GetCategoryHidden` needed no help: asked of the view it returns exactly what the template returns when asked directly. Fixed 2026-09-08 |
| `color-by-parameter` | **A parameter that does not exist coloured 22 elements anyway.** `valueOf` answers `""` both for a parameter that is not there and for one that is there and empty, so every element landed in the single `(no value)` group and the view was painted one colour — a drawing that *looks* grouped and is grouped by nothing. The parameter is now counted across the set, and where **not one** element carries it nothing is coloured and `refused` says so, handing back the names the first element does carry. A MIXED selection is unchanged: an element without the parameter still joins `(no value)` and is still coloured, which is the existing decision and a deliberate one. Fixed 2026-09-09 |
| `measure-run-quantities` | **`measure=ZZZNOTHINGHERE` gave an answer identical to `measure=area`.** Anything not containing "area" fell through to length, so the input had no effect and there was nothing to vary between the two legs of a proof. Both measures are now matched against a stated list of words, and a word in neither is refused with nothing measured. An **absent** `measure` still means length — an input nobody supplied is a different thing from one somebody got wrong, and that path was working. Fixed 2026-09-09 |
| `edit-text-values` | **`field=text` reported all seven notes `absent` and changed nothing**, which reads as "there was nothing to do". Three unworkable inputs are now refused before any element is read — an empty `field`, a `mode` that is not prefix/suffix/replace (`transform` handed every value straight back, so everything landed in `untouched`), and an empty `text`. Two more are refused after looking, safe because neither path writes: the field is on **not one** element handed in, and the field is not **text** on any of them. Where a refusal is decided after looking, the list the unusable input filled is cleared — `absent: 7` on a run that did nothing reads as a finding and is not one. Fixed 2026-09-09 |
| `add-schedule-fields` | Cast to `ViewSchedule` and nothing else, so a schedule **placed on a sheet** — the only form a person can select — slid past it and it answered `added 0`, `availableFields 0` with no refusal. Now resolves a `ScheduleSheetInstance` to the schedule behind it, the way `report-schedule-definition` and `read-schedule-contents` already did, and refuses when not one element handed in was a schedule, or when no column names were given. Fixed 2026-09-09 |
| `add-schedule-combined-field` | Same `ViewSchedule` cast. It already named what it could not use in `notASchedule`, so it was never silent — what it could not do was **see** a schedule on a sheet. Resolution added; nothing else changed. Fixed 2026-09-09 |
| `remove-schedule-fields` | Same cast, and silent with it: `removed 0`, `presentFields 0`, no refusal. Resolution added, plus a refusal when nothing handed in was a schedule and when no field names were given — the second also removes a null dereference of `fieldNames`. `refusedInUse` is left as it was: a field a rule still points at is a finding **about the schedule**, which is a different sentence from "I could not use what you gave me". Fixed 2026-09-09 |
| `set-schedule-appearance` | Same cast, answering `changed 0` — which reads as "the column was already like that". Resolution added, and its existing `refused` now carries both "nothing handed in was a schedule" and "no column was named". Fixed 2026-09-09 |
| `set-schedule-filters` | Same cast. **And its unknown-match-type refusal was in `cannotFilterBy`**, which is a finding about the schedule — so an unusable request looked like something learnt, and a negative case could never come back empty. Resolution added, a `refused` output declared, and the match-type refusal moved into it along with "no column was named" and "nothing handed in was a schedule". `cannotFilterBy` keeps what it should: the fields Revit itself will not filter on. Fixed 2026-09-09 |
| `set-schedule-sort-group` | Same cast, answering `sorted 0` — which reads as "already in that order". Resolution added, plus a refusal when nothing handed in was a schedule and when no sort field was named. As with the filter fragment, `cannotSortBy` stays a finding about the schedule and `refused` is the fragment declining. Fixed 2026-09-09 |
| `export-schedule-to-csv` | Same cast. A missing export folder and a selection holding no schedule both came back as "nothing came out"; only the first said why. Resolution added, and the second now refuses too. Fixed 2026-09-09 |
| `add-revision-cloud` | **The `ViewSchedule` §3e lists it for is a view-type guard** (`view is ViewSchedule`), not a cast of a selected element — it never consumes a schedule as input, so the placement fix does not apply and was not made. **What was wrong is next to it: `viewRefused` was one bool covering two causes**, *"the view will not take a cloud"* **and** *"that revision does not exist"*, and the file's own comment admitted the conflation. Those have opposite fixes — change the view, or make the revision — and a caller told only `viewRefused true` goes looking at the drawing when the revision is what is missing. `viewRefused` now means exactly its name; a missing revision is its own sentence; and a new `refusalReasons` carries the words for both, naming **which** of the five view types refused and why. The three ways a single element misses out (not visible in this view, too small to cloud, Revit declined the rectangle) each get a sentence carrying a **count** rather than one line per element. An empty `elements` with a good view and a real revision — four zeroes that read as "nothing to cloud" — is refused too. Fixed 2026-09-09 |
| `set-view-section-box` | **`viewRefused` meant two opposite things.** It was set when the VIEW could not carry a section box — a plan, a section, a template — and again when the view was a perfectly good 3D view and **nothing handed in had any geometry**. Opposite fixes: open a 3D view, or hand over something measurable. **This is also §3f's recorded doubt, and it could not be settled while the two shared a word:** the stronger negative the owner asked for — a 3D view whose selection has nothing to enclose — reported `viewRefused true` exactly like the weak one. It now leaves `viewRefused` **false** and says in words that the view was fine and the selection was not. `viewRefused` means only its name; a new `refusalReasons` carries the words and names **which** view type refused. Two further silences closed while in there: an **empty** `elements` in a good 3D view answered `applied false`, `enclosed 0`, `noGeometry 0`, `viewRefused true` — blaming the view for an empty selection; and **a margin negative enough to turn the box inside out** was applied and reported `applied true`, cutting the view to an empty screen, which reads as though the model had been deleted. `heron-status` and the `proof:` block are untouched — **but the proof of 2026-09-09 is now stale**: the fragment leaves an output it did not have, so the fingerprint no longer matches. Nothing it recorded is contradicted. Fixed 2026-09-09 |

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
| **8** | **`Describe` renders a valid `ElementId` and `ElementId.InvalidElementId` as the same word, `"ElementId"`.** Real, and still worth fixing — an answer that cannot say whether a thing was created is a poor answer. **But it has NO named victims, and this row claimed two it did not have.** `dimension-mep-runs` and `dimension-family-instances` were listed here as unprovable because of it. Both were **proved on 2026-09-09** with `dimensionId` still reading as `ElementId` in all four phases. An unreadable value is SKIPPED by the judge, not counted as content — the opposite of what this row asserted. What actually blocked them was `problem`, an explanation field, undeclared and therefore judged as a finding; `role: accounting` on `problem` and `noReference` proved both in one run. **The lesson is the one in §3h.2, not this one:** an undeclared role is more likely to be the blocker than a defect in the renderer | **OPEN, and no longer urgent.** One line, still needing a rebuild and a Revit restart — but nothing is waiting on it |

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

## How to arrange a case, learned by getting it wrong all day

Not a list of problems — the working method, written down because most of today's misses were the
arrangement rather than the fragment.

**This section is the evidence; the operative version is
[`.claude/skills/fragment-proving/SKILL.md`](../.claude/skills/fragment-proving/SKILL.md).** The two say
the same five things and are deliberately not the same document: here is what was observed, with the
values that were passed and what came back; there is what to do about it, next to the job file that
does it. Fix the skill when a rule turns out to be wrong, and add the observation here.

**PROVE ON A SMALL SELECTION.** The owner's instruction, 2026-09-09, after `set-mep-size` timed out on
307 ducts: *"a lot of items change, it will affect slow process… you can try with a small number of
ducts like 2 or 3."* Retried on the 22 ducts in `FloorPlan: M1` it sized all 22 immediately, and
`split-mep-run` passed in the same batch. **A heavy write on a big selection is not a stronger test, it
is a slower one** — and a timeout tells you nothing at all about the fragment.

`select-by-category-name --set inViewOnly="FloorPlan: M1"` gives 22 ducts. `FloorPlan: L3` gives 307.

**ASK FOR WHAT THE MODEL HAS.** Five times today the POSITIVE case was the empty one, because D-30 is
written about the negative and the positive quietly goes unarranged. `select-by-connection-status` was
asked for open ends in a model with none; `measure-mep-slope` for a minimum nothing falls below;
`report-coverage` for gaps at a radius that leaves none; `check-family-standards` for a pattern nothing
matches, which makes MORE findings not fewer.

**CHECK THE CATEGORY IS VISIBLE WHERE YOU SELECT.** Sheets do not appear in a floor plan; levels do not
appear in their own plan. Twice the setup found nothing and the answer read as a missing selection.

**MATCH THE INPUT TO THE MODEL'S OWN UNITS AND SHAPES.** `set-mep-size` was handed a width and height
for ducts that are round. Every duct here is dia 102, 152 or 203 — an imperial model.

**READ THE BINDING NOTE ON THE ANSWER.** `find-overlapping-lines` ran on a stale selection of ten
equipment items and answered anyway; only `elements from the selection (10)` on the reply gave it away.

---

## Add to this file, do not start another

A fragment that fails in front of a model belongs here the same day, with what was passed to it and
what came back **verbatim**. The value of the list is that every row was observed rather than expected —
the moment it fills with things somebody thought might be wrong, it stops being worth the sit-down.
