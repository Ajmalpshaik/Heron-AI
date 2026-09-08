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
| `report-category-overrides` | The empty answer — **0 overrides in all 68 views** | A positive | Visibility/Graphics → set one category to a colour |
| `read-graphic-overrides` | The empty answer — 0 of 625 elements | A positive | Select ducts → Override Graphics in View → By Element → red |
| `diagnose-visibility` | 625 visible, 0 reasons | Something invisible | Select one element → HH |
| `report-category-visibility` | Real answers — 18 hidden in `L2`, 7 in `Model Linking` | A view with **zero** hidden | No view in the model has none. Either make one, or prove it by TRACKING (D-53) |
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
| `remove-view-filter` | **None.** `removed 0`, `deleted 0` | *"'L2' is governed by template 'Mechanical Plan', which owns its filters"* — a filter cannot be removed from a template-driven view at all. Needs a view whose filters are its own |

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
