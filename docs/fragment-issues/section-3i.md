# Fragment issues — section 3i

> One section of [the register](../FRAGMENT-ISSUES.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

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

`set-element-level` is the first fragment that **needed [D-54](../DECISIONS.md) to exist** — it was run
earlier the same day, with no value, and refused by name rather than guessing.

### The wall itself

[D-54](../DECISIONS.md) resolves a **view, a level, a category, a name, a number, or true/false — and
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
it was wrong for an hour.** [`list-sheets`](../../brain/fragments/list-sheets) is already `PROVEN`, needs
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

`looks_empty` in [`heron_validate.py`](../../brain/heron_validate.py) filters to the contract's declared
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

### A VALUE THAT RE-LABELS IS NOT A VALUE THAT TURNS THE ANSWER OFF — found 2026-09-10

`looks_empty` reads **every** declared result, so a negative is empty only when they are **all** zero
at once. A value-driven negative therefore has to switch the whole answer off, and one that merely
moves findings from one declared result into another does not — it looks like the strongest possible
pair and proves nothing.

`find-dead-ends` was arranged that way and judged **NEG NOT EMPTY**, correctly:

| `stubLength` | `deadEnds` | `stubs` | `openEndsFound` |
|---|---|---|---|
| 0.1 ft | 4 | 0 | 4 |
| 99999 ft | 0 | **4** | 4 |

`deadEnds` and `stubs` are both `role: result`. The four findings were re-labelled from one to the
other and the negative still returned content. The conserved total — which is what made the pair look
convincing — is precisely the proof that nothing was turned off.

> Before choosing a value for the negative leg, list the fragment's `role: result` fields. If the
> value moves findings **between** them rather than emptying them, it cannot carry the negative.

What worked instead was a second selection that was **not** a wrong category: two duct fittings, with
`withoutConnectors 0` — elements fully capable of showing an open end that genuinely had none. Same
domain, same view, populated on both sides. See §3b-ii.

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
[`tools/jobs/value-driven-negatives.yaml`](../../tools/jobs/value-driven-negatives.yaml). None proved, and
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

### ONE SELECTION CANNOT ARRANGE A TWO-SET FRAGMENT — found 2026-09-09

`find-nearest-elements` refused, and the refusal is the clearest statement of a structural gap that
nothing else in this file names:

> *"Cannot run: `elements (IList<Element>)`, `targets (IList<Element>)` were never supplied. There is a
> selection, but this fragment needs 2 separate sets of elements and one selection cannot say which is
> which. Run the fragments that produce them first. Running anyway would report 0 results, which reads
> as 'there was nothing to find' rather than 'nobody was asked'."*

The setup chain leaves exactly one `elements` behind, and `set-selection` consumes exactly one. A
fragment comparing **two** sets — nearest-to, clashes-against, this-versus-that — has no arrangement at
all in a job file today, however its inputs are typed.

**This is a job-file and chaining gap, not a D-54 one.** D-54 carries the caller's half across as text;
a second set of elements is not text. Closing it means either a chain that can bind two named sets, or
a `--set-b` alongside `--set`. Neither exists.

Named so far: `find-nearest-elements`, `find-clashes` (`against`), `measure-distance` (`first`,
`second`), `compare-elements` would have been one had it not taken its pair from a single selection.

### Two more shapes D-54 refuses, and one that does NOT bind from the chain

§3i lists `FamilySymbol`, `Material`, `OverrideGraphicSettings`, `Curve`, `ParameterValue` and bare
`Element`. Two more, both met on 2026-09-09:

| Shape | Fragment | What it said |
|---|---|---|
| `IDictionary<string, double>` | `check-minimum-clearance` | `rules` is a value the caller supplies. A dictionary has no name to look up, so this belongs with `OverrideGraphicSettings` rather than with the resolvable names |
| `IList<Element>` **as a named need** | `check-room-mep-completeness` | `devices` was never supplied, even with a selection present |

**That second row corrects an assumption made earlier the same day.** `remove-view-template` showed
`views (IList<View>)` resolving from a single name, and it was reasonable to expect `IList<Element>` to
bind from the chain the same way. It does not. A `View` has a name Revit can look up; an arbitrary
element list has none, so a *named* element need has to come from a previous fragment's `provides` and
not from the selection — which is D-29's job, and is exactly what the two-set gap above blocks.

### Two more with no positive case in this model

- `check-flow-direction` — `bothOut 0`, `bothIn 0` on mechanical equipment. No contradictory joints
  exist here. Its two results are also the clearest example in §3h.2 of names that LOOK like
  bookkeeping and are the answer.
- `audit-mep-openings` — `stale 0`, `combined 0`, `unhosted 0` on 307 ducts in L3, the richer
  selection. Consistent with the earlier run on 22, so it is content rather than scale.

### THE MISSING `LIST_*` FRAGMENTS ARE NOW THE BINDING CONSTRAINT — 2026-09-09

§3d has listed the missing `LIST_*` capabilities as a gap since 2026-09-08. After a full day of proving
they are no longer a gap among others: **they are what stops the next batch**, and the count is
concrete.

| Blocked fragment | The value it wants | Why guessing failed |
|---|---|---|
| `set-element-workset` | a workset **id** | `list-worksets` gives the names — *Shared Levels and Grids*, *Workset1* — and not the integers. The fragment reads `ELEM_PARTITION_PARAM` as an int, so a name cannot stand in |
| `set-category-visibility`, `set-view-crop`, `reset-view-graphics`, `set-crop-box-settings` | a view carrying **no template** | Every V/G fragment must be proved on one — a templated view answers "changed 0" because the template owns visibility (§4). `3D View: {3D}` was a guess and the model has no such view. **Nothing lists views**, so there is no way to find a template-free one except by asking a person |
| `create-legend-view` | a legend's exact name | *"No view called Legend: Mechanical Legend"* — a good refusal against a name nothing could have supplied |
| `set-section-mark-visibility` | any way to reach section marks | `select-by-category-name` answers `setup_failed` for `Sections` model-wide and in a plan. There is no `list-sections` |
| `select-by-material` | a material name | Ducts and mechanical equipment both answer `noMaterial` — **this model assigns no materials to MEP elements at all**, which is also why `find-unused-materials` finds 56 unused. Content, but nothing would have told a job-file author that without running a fragment to find out |

**The pattern is the same every time: the fragment is fine, the arrangement needs a name, and the name
can only be got by asking a person or by guessing.** Guessing is what D-54 refuses to do on the
caller's behalf, and it is right to refuse — so the missing half is a way to ASK the model.

`list-sheets`, `list-levels`, `list-grids`, `list-revisions`, `list-worksets` and `list-linked-models`
already exist and are PROVEN, and using `list-sheets` as a setup step proved five fragments in an hour
(§3i). **The route works. There are simply not enough of them**, and two of the ones that exist are one
field short:

1. **`list-views`** — name, type, and whether it carries a template. Unblocks four V/G fragments at
   once and would have saved three wrong guesses today.
2. **`list-worksets` needs the id**, not only the name.
3. **`list-materials`** — the names, and whether anything uses them.
4. **`list-sections`**, or any route to a section mark.

Ranked against §3h, this now sits above everything except making silence illegal. §3h.3 asked for one
command answering *"describe this model"*; this is the same argument arriving from the proving side,
and it is the cheaper half of it.

### `batch-prove` REPORTS A VERDICT ON A RUN THAT NEVER HAPPENED — found 2026-09-09, FIXED - corrected 2026-09-19

**FIXED, and this heading said OPEN until 2026-09-19.** [Row 87](../FRAGMENT-ISSUES.md#) records the fix and names the
same defect - *"the runner asked only `if not os.path.isfile(record_path)`"*. Confirmed 2026-09-19 by
reading `tools/batch-prove.py`, which now calls `os.remove(record_path)` **before** the run, so a
missing file means nothing ran and `DID NOT RUN` is what comes back. The account below is KEPT: the
session-id tell it names - `session 17356` against the live `session 32940` - is the only thing that
gave it away, and it is how the next stale-record bug will be caught too.

A batch of six MODIFY fragments came back **6 for 6 `POSITIVE EMPTY`**. Six independent content
problems in one batch is not a pattern that happens, so the records were read rather than the summary,
and none of the six had run at all.

Every one had been refused for the lease:

> *"This Revit is in use by another chat, so Heron has refused rather than taking it over mid-job.
> Nothing was sent to Revit. **Refusing to record evidence about a model that will not name itself.**"*

**Heron behaved perfectly.** It refused, it said nothing was sent, and it declined to write a record —
which is exactly right. The defect is that `batch-prove` then judged **the record already on disk from
an earlier run**, and reported verdicts about it as though they were this batch's.

The evidence it judged was not merely old, it was about a **different arrangement**. `flip-elements`
reported `POSITIVE EMPTY` from a record whose negative reads *"run with categoryName=Ducts,
inViewOnly=FloorPlan: L3"* — while the job file asked for duct tags in M1.

**A date check would not catch this.** Both records say `2026-09-09`. What gives it away is the session
id inside the `model` line: `session 17356` against the live `session 32940`.

> A verdict about a run that did not happen is worse than a crash, because it is filed as a finding.
> This is the same family as the `ALREADY` hole closed earlier — reporting on something that is not
> this run — and it wants the same kind of fix.

**The fix, and it is small:** `validate` already stamps `model` with the session id. Read the record's
mtime or its session before judging, and refuse to report on one this invocation did not write.
`NO RECORD` and `REFUSED` verdicts already exist for exactly this shape of answer.

Confirmed: **fourteen of the eighteen fragments proved on 2026-09-09 came from the live session.** The
other four were proved by TRACKING, where the D-53 evidence *is* the tracking rows and those were run
fresh — but their positive phase came from the earlier session, so they are being re-run rather than
argued for.

### A RUN BOUND THE PREVIOUS SELECTION AND CALLED IT A PASS — seen ONCE, 2026-09-10, NOT REPRODUCED

Recorded because it is the same family as the section above — a confident answer about something that
was not what was asked — and because it nearly went into a job file as a proved negative.

`dimension-rooms` was run twice back to back on the write path, first with `categoryName=Rooms` and
then with `categoryName=Walls`, each with `--setup select-by-category-name --setup set-selection`. The
second run returned **`created 2` and `notARoom 0`** — identical to the first — and its record says:

> `elements from the selection (1)`

One element, when its own setup had just asked for four walls. Had it been read as the negative leg it
would have said "the negative created two dimensions as well", which is a finding about nothing.

**It has not reproduced.** The same Walls run in isolation binds `(4)` and returns `created 0`,
`notARoom 4` — twice — and three further deliberate Rooms-then-Walls trials, run back to back to
provoke it, were all correct. Five clean runs against one bad one.

**No mechanism is claimed.** §1d records that a rolled-back write clears the selection, and a race
between that restore and the next run's setup would fit — but that is a guess, and a guess written down
here would be read as a finding later. What is certain is only what the record says.

> `bound` is the field that catches this, and it is already printed. **Read `elements from the
> selection (N)` and check N against the category you asked for**, before reading the result. A wrong
> selection does not announce itself in the answer.

Marked **NEEDS_REVIEW**, not OPEN: one unreproduced observation is not yet a defect.

### `validate` DOES NOT APPLY THE RISK GATE — found 2026-09-11, FIXED - corrected 2026-09-19

**FIXED, and this heading said OPEN until 2026-09-19.** [Row 54](../FRAGMENT-ISSUES.md#) records the fix. Confirmed
2026-09-19 by reading `mcp/client/heron_bridge_client.py`: `cmd_validate` calls `risk_refusal`, and
the comment beside it names this passage. A PUBLISH fragment is refused unless `--allow-publish` is
typed for that run. The account below is KEPT for the shape: two commands gave opposite answers about
the same fragment risk, and that is what a gate wired into some paths and not others looks like.

**The proving path is the one place `risk_refusal` is not called, and proving is where fragments run
against a real model with `write.enabled` on.**

Found by accident. `create-workset` came back PASS in a sweep, and the same fragment run through
`fragment` refused:

> *"create-workset is declared risk: ADMIN, and Heron does not run those yet — HeronPermissions puts
> Publish and Admin out of reach for Phase 0 and Phase 1. **Nothing was sent to Revit.**"*

But the run record says it ran, on the live model:

```
positive ok=True  doc=Snowdon-scratch_ajmal.al
  createdNames 2 item(s) [HERON WS A, HERON WS B]
  findings     "2 workset(s) created: HERON WS A, ..."
```

**Read in the client**, `risk_refusal` is called in exactly two places —
[`cmd_fragment`](../../mcp/client/heron_bridge_client.py) and `cmd_prove`. `cmd_validate` does not call
it at all.

| Path | Gated? |
|---|---|
| `fragment` — one run, no proof | **yes** |
| `prove` — the old chain runner | **yes** |
| `validate` — **the proving path, with `--write`** | **NO** |

### Eight gated fragments were run through it in one session

| Fragment | risk | status |
|---|---|---|
| `create-workset` | ADMIN | DRAFT |
| `add-project-parameter` | ADMIN | DRAFT |
| `transfer-project-parameters-between-documents` | ADMIN | DRAFT |
| `upgrade-family-files` | PUBLISH | DRAFT |
| `export-view-image` | PUBLISH | DRAFT |
| `export-parameters-to-csv` | PUBLISH | DRAFT |
| `export-families` | PUBLISH | DRAFT |
| **`export-views-to-fbx`** | **PUBLISH** | **PROVEN** |

**One of them is already promoted.** `export-views-to-fbx` was proved and signed on 2026-09-11 through
a path `HeronPermissions` says must not run in Phase 0 or 1. Its evidence is real — a file was written,
and the negative is structural — but it was obtained through a gate that was supposed to stop it.

**DECIDED BY THE OWNER, 2026-09-11: the permission tier wins.** `export-views-to-fbx` is back to
`heron-status: DRAFT`.

**Its proof block was KEPT, deliberately.** The evidence is real and it carries Ajmal PS's signature —
throwing that away would lose a true record of a run that happened. What was withdrawn is the CLAIM
the status makes, not the evidence behind it. When `PUBLISH` is allowed by the phase, or when
`validate` is given the gate and the run is re-taken, the fragment is one promotion away rather than
one proof away.

> A proof and a status are different assertions. The proof says *this ran and this came back*; the
> status says *this library stands behind it*. Only the second was wrong here.

That leaves the second question open and it is worth a `D-` number if the answer is the other one:

**Whether the gate belongs on `validate` at all.** Two readings, and nothing here settles which:

- the permission tier is the authority, and a `PUBLISH` fragment has no business running yet, so the
  status goes back to `DRAFT` until the phase allows it; **or**
- the gate is about what Heron does *for a user*, proving is a different act, and `validate` is
  correctly exempt — in which case the exemption should be **written down and deliberate**, not the
  absence of a line.

> The exports all wrote into this session's scratchpad, and the ADMIN runs were inside the rolled-back
> TransactionGroup. Nothing landed anywhere it should not have. **That is luck about where the paths
> pointed, not the gate doing its job.**

### `can_promote` WILL PROMOTE AN UNPROVEN FRAGMENT TO `PROVEN` — found 2026-09-10, OPEN - RE-CONFIRMED 2026-09-19

**STILL OPEN, and reproduced again 2026-09-19** on `add-project-parameter` (DRAFT, no proof):
`proof_problems` returns `[]` and `can_promote(f, 'PROVEN')` returns
`(True, 'well-formed, and PROVEN needs no proof')`. `brain/heron_fragment.py` still asks
`frag.status in NEEDS_PROOF` - where it IS, not where it is GOING - and `NEEDS_PROOF` is
`('PROVEN', 'PRODUCTION')`, so DRAFT never trips it. Nothing has been promoted this way, because
promotion is a hand edit; the gate simply would not stop one.

This is in the machinery the whole *"the machine never signs"* discipline stands on, so it is written
out in full rather than summarised.

**Reproduced on a fragment with no proof at all:**

```
>>> f = heron_fragment.load('brain/fragments/connect-open-ends')
>>> f.status
'DRAFT'
>>> f.proof
None
>>> heron_fragment.proof_problems(f)
[]
>>> heron_fragment.can_promote(f, 'PROVEN')
(True, 'well-formed, and PROVEN needs no proof')
```

`PROVEN` **is** in `NEEDS_PROOF`, and `can_promote` **does** call `proof_problems` for it. The check
inside is the problem — it reads the fragment's **CURRENT** status, not the **TARGET** it is being
asked about:

```python
if proof is None:
    if frag.status in NEEDS_PROOF:      # <- where it IS, not where it is GOING
        problems.append("... status is %s but there is no proof ...")
```

A `DRAFT` has status `DRAFT`, which is not in `NEEDS_PROOF`, so the "there is no proof" branch cannot
fire during the one call that decides the promotion. By the time `status` reads `PROVEN` the promotion
has already happened.

**`proof_problems` is not wrong — it is being asked the wrong question.** Its own docstring says
*"Whether this fragment's proof, **if it has one**, is a proof at all"*: it is an AUDIT of a fragment
already sitting at `PROVEN`, and [`validate`](../../brain/heron_fragment.py) uses it that way correctly.
`can_promote` reuses it as a transition gate, and as a transition gate it can never refuse.

> The same shape as the comment sitting ten lines above it — *"The gate existed and nothing stood on
> it."* One layer up, and still true.

**What it does NOT mean.** Nothing has reached `PROVEN` this way. Every promotion so far has been a
person naming signed fragments after reading each draft, and `accept` is unaffected — it still records
the signature and still refuses to set a status. The hole is that nothing would have STOPPED a wrong
promotion, not that one happened. It was found by checking eligibility before offering to promote, and
seeing three **unsigned** fragments come back READY beside two signed ones.

**Not fixed here.** The obvious change — pass `target` to `proof_problems`, or test
`target in NEEDS_PROOF` rather than `frag.status` — touches the promotion gate itself and belongs to
the owner, not to a proving session. Recorded and left, per the standing rule.

### A PROOF TAKEN WITH `--in` NAMES THE WRONG MODEL — found 2026-09-10, OPEN

D-30 asks for *"a recorded run against a **named real model**"*. `proof.model` is that name, and when
`--in` is used it is **the model that happened to be in front, not the one the run went to.**

**Demonstrated in one run.** `validate --in "Snowdon-scratch_ajmal.al" list-levels`, with `Project1`
active:

```
record model line : Project1 work_ajmal.al (3,471 elements), Revit 2024, session 24688
phase document    : Snowdon-scratch_ajmal.al
levels            : 11 item(s) [Parking, L1 - Block 35, L1 - Block 37, ...]
```

Eleven levels with Snowdon's names — so the run went where it was told. Only the headline is wrong,
and the element count is wrong with it: 3,471 is Project1's, not Snowdon's.

**The cause, read rather than guessed** ([`heron_bridge_client.py`](../../mcp/client/heron_bridge_client.py)):

```python
model = "%s (%s elements), Revit %s, session %s" % (
    opening.get("document"), ...)
```

`opening` is the probe that identifies the **ACTIVE** model — its own failure text says *"Could not
identify the active model"*. `in_document` is passed separately to each phase, which is why every
phase's `document` field is right and only the stamp is wrong. The refusal sitting immediately above
that line reads *"Refusing to record evidence about a model that will not name itself"* — and then it
mis-names one that did.

**Three PROMOTED fragments carry it.** All three came from a job file pinning `in:
"Snowdon-scratch_ajmal.al"` while `Project1` was the active document:

| Fragment | `proof.model` says | but both phases ran on |
|---|---|---|
| `create-callout` | `Project1 work_ajmal.al (3,445 elements)` | `Snowdon-scratch_ajmal.al` |
| `create-key-schedule` | `Project1 work_ajmal.al (3,445 elements)` | `Snowdon-scratch_ajmal.al` |
| `duplicate-type` | `Project1 work_ajmal.al (3,445 elements)` | `Snowdon-scratch_ajmal.al` |

**The evidence is not invalid, and that distinction matters.** The runs happened, both phases are
recorded, and each names the document it used — the proof contradicts itself *inside one block*, which
is how it was caught. What is wrong is the field a reader trusts first.

**Three more are FLAGGED, not asserted** — `transfer-line-styles-`, `transfer-object-styles-` and
`transfer-view-filters-between-documents`, whose model line says `Snowdon Towers Sample Architectural`
while the phases ran on `Snowdon-scratch_ajmal.al`. These fragments take a `sourceDocumentTitle`, so
the named document is a genuine participant and the mismatch may read differently. Needs a look by
someone who took them.

**`restamp` is NOT the remedy** — it re-records fingerprints only, and refuses anything whose code
moved after the proof. Nothing in the repository currently rewrites `proof.model`.

> The five fragments proved on `Project1` tonight are unaffected: the pin and the active document were
> the same, so the stamp is true. That is luck, not a safeguard.

**Not fixed here.** The change is one line in the proving client — stamp the resolved document rather
than the active one — plus a decision about the three already promoted. Both belong to the owner.

### `switch-active-project` ASKS THE WRONG `UIDocument`, AND HAS NEVER SWITCHED — found 2026-09-10, FIXED, AND THE CAPABILITY RETIRED - corrected 2026-09-19

**FIXED, and this heading said OPEN until 2026-09-19.** [Row 90](../FRAGMENT-ISSUES.md#) records both bugs fixed and the
capability retired - Revit answers *"Changing the active view is not applicable to inactive
documents"*, so no call switches projects and the fragment's premise is gone. Confirmed 2026-09-19 by
reading `impl/any/fragment.cs`: the already-active check compares `PathName` instead of
`ReferenceEquals`, and the header now lists all three routes tried and refused. The account below is
KEPT: it is the measurement that retired the capability.

§3b-i said this one *"needs a second project open"* to be tested properly. Two were open on 2026-09-10,
and it does not work.

```
target=Project1 work_ajmal.al   viewName=1 - Mech
  activeBefore  Snowdon-scratch_ajmal.al
  switched      false
  findings      Revit refused to change to the view "1 - Mech" in "Project1 ..."
```

Verified afterwards the way the purpose demands — `REPORT_OPEN_DOCUMENTS` still reports
`active is "Snowdon-scratch_ajmal.al"`. **The tab bar did not move.**

**The cause, read in two files.** The target view is collected correctly from the target document:

```csharp
var openable = new FilteredElementCollector(wantedDoc)      // the OTHER project
...
uidoc.RequestViewChange(chosen);                            // line 150
```

but `uidoc` is bound by the executor to the **active** document
([`RevitFragment.cs:145`](../../revit/Heron.Revit.Addin/RevitFragment.cs)):

```csharp
uidoc = app.ActiveUIDocument;
```

So it hands Snowdon's `UIDocument` a view belonging to Project1, and Revit throws. The fragment's own
comment states the rule it then breaks — *"a view belongs to one document"*. Switching needs a
`UIDocument` for the **target** document, not the active one.

**This means the fragment has never done the thing it exists for.** The earlier record that reported
`switched true` took the *"already the active project"* branch — asking to switch to the project you
are already in, which returns success having done nothing and is documented as correct. Every path
that would actually move between projects throws.

**It was NOT the transaction.** Run first with `--write` and then without, the refusal is identical —
consistent with its own line *"Opens no transaction and needs none… That is why it is EXECUTE rather
than MODIFY."*

### …and it could not be PROVED even once it is fixed

Its only `role: result` is `switched`, a **bool** — and the judge reads every boolean as zero, however
it arrives:

```python
if isinstance(value, bool):     return 0
if text.lower() in ("true", "false"):   ...
```

That is deliberate and right — `looks_empty`'s own docstring gives the reason: a flag that reads the
same in both cases *"would make an empty answer impossible for it to demonstrate."* But the
consequence here is that `switched: true` counts as zero as well, so **the POSITIVE reads empty too**.

> A declared result that is a BOOL cannot carry EITHER leg. This is the sibling of the NAME case
> above, and worse: a name at least makes the negative impossible, while a bool makes both impossible.
> Either the fragment gains a countable result beside the flag, or D-53 tracking is the route.

**Not fixed here.** The `UIDocument` fix is one line in a fragment implementation, the proving question
is a contract change, and both are the owner's.

### ONE PERSON, ONE `HERON_CLIENT_ID`

The lease identifies a **chat**, not a person. Four ids were in use for one afternoon's work —
`proving-2026-09-09b` by hand, `tracking-run` and `tracking-set-selection` by two scripts, and
`heron-batch-prove` chosen by the runner itself — and to Heron that is four chats competing for one
Revit. Each refusal reads exactly like a fragment failing.

> Pick one id for a working session and put it in every script and every command. A second id is a
> second chat, and the lease is doing its job when it refuses the second one.

This also explains the round-one wipe-out earlier the same day, when eleven jobs failed immediately
after a hand-run `count` — the same collision, differently dressed, and it was misread then as an
arrangement fault.

### Six MODIFY fragments, six honest refusals — 2026-09-09

Run on the tags-negative pattern. None proved, none is defective, and **every one said in words why
not** — which is the behaviour §3h.1 is asking the twelve silent ones for.

| Fragment | What it answered | What it needs |
|---|---|---|
| `rename-elements` | `planned 22`, **`collisions 21`**, `renamed 0` | Every duct here is a `Tees`, so a global find/replace makes 21 duplicate names — and it refused rather than creating them. **This fragment may not be provable by find/replace at all** on same-typed elements: the arrangement has to produce unique names, which `find`/`replaceWith` cannot |
| `trim-extend-elements` | *"This squares off exactly TWO elements — 22 were given. Nothing…"* | Exactly two elements. `select-by-category-name` cannot narrow to two, so this is the two-set problem above wearing a different coat |
| `remove-parameter-value` | `alreadyEmpty 22`, and *"0 cleared, 22 already empty. **An empty field and a zero are different.**"* | A populated WRITABLE parameter. `System Type` and `Comments` are both blank on every duct in this model |
| `flip-elements` | `cannotFlip 10` on air terminals | A family with a hand to flip. The flippable ones here — doors, windows — are in the architectural link |
| `set-design-option` | `available 0`, *"Nothing was copied into a design option"* | Design options. This model has none |
| `set-mep-justification` | `0 run(s) set to 100 mm horizontal and 100 mm vertical` | Ran and moved nothing. Worth a second look at the sit-down — justification should apply to a duct run |

**Read the `remove-parameter-value` refusal twice.** *"An empty field and a zero are different"* is the
whole of §3h.1 in seven words, written by a fragment that already gets it right.

### FIFTEEN NARROWER SELECTORS WERE ALREADY PROVED, AND EVERY JOB FILE USED ONE — 2026-09-09

Four fragments refused within an hour of each other, each perfectly clearly, and each for the same
reason:

| Fragment | What it said |
|---|---|
| `rename-family` | *"The selection covers 4 different families, and one name cannot…"* |
| `place-mep-fitting` | *"a fitting joins two, three or four runs — 22 were given. Two makes an elbow, a union or a transition, three a tee, four a cross"* |
| `rename-elements` | `planned 22`, `collisions 21` — every duct in this model is a `Tees` |
| `trim-extend-elements` | *"This squares off exactly TWO elements — 22 were given"* |

**The conclusion first drawn from this was that the library cannot select more narrowly than a
category. That is wrong, and it was one edit away from being written into this file as a gap.**

`select-by-category-name` hands over every element of a category in a view, and it is the only selector
any job file had used. The library also has, all `PROVEN` and all usable as a setup step:

`select-by-family`, `select-by-connection-status`, `select-by-parameter-value`,
`select-by-numeric-parameter`, `select-types`, `select-by-workset`, `select-by-mep-system`,
`select-by-pin-state`, `select-by-phase`, `select-by-design-option`, `select-by-insulation`,
`select-from-link`, `select-visible-in-view`, `select-with-warnings`, `select-scope-boxes`,
`select-by-categories`.

> **This is the third time in one day the same shape has appeared:** a capability existed, was proved,
> and was invisible because nothing named it where somebody writing a job file would look. First the
> `list-*` fragments as a setup chain (§3i), then `views (IList<View>)` resolving from a single name,
> now the narrow selectors. **The library is further ahead than the job files are.**

That is an argument for §3h.4 — generating the job file from the fragment library rather than typing
it — considerably stronger than the six mistyped input names it was first written about. A generator
reading `contract.needs` would have offered `select-by-family` for a fragment that renames a family,
because the contract says what it wants.

### The contradiction this exposes, still open

§3 records *"every duct end in this model is connected"*, and `find-dead-ends` was set aside twice on
that basis. But `place-mep-fitting`, run on 2026-09-09, reported **`openEnds 22 item(s)`** against the
same 22 ducts in M1.

Both cannot be true. `select-by-connection-status` — itself `PROVEN`, taking `wantOpenEnds` — asks the
model the question directly, and until it answers, **`find-dead-ends` is recorded as blocked on a claim
that has not been re-checked since it was made.**

### What this says about where the proving goes next

**142 to 160 on 2026-09-09.** The write engine is not the constraint — fragments proved through it all
day and rolled back cleanly. What is left is blocked on three things, in this order:

0. **Naming what already exists.** Three separate capabilities were proved and invisible today — the
   `list-*` fragments as a setup chain, `views` resolving from one name, and fifteen selectors narrower
   than a category. Nothing needs building for these; they need to reach the person writing the job
   file. This is the cheapest item on the list and it outranks the rest.
1. **`LIST_*` fragments**, above. This stopped more batches today than anything else, and the route is
   already proved: `list-sheets` as a setup step proved five fragments in an hour.
2. **A resolver for named Revit objects** — `FamilySymbol`, `Material`, and the view-like ones. Half of
   the never-run MODIFY fragments wait on it.
3. **A way to bind TWO sets of elements**, which no job file can express today.

All three are offline work and none needs Revit to build. **Making silence illegal (§3h.1) still comes
first**, because everything above makes proving faster while that one makes Heron honest.

---
