# What actually blocks the 62 drafts

**2026-09-17.** The model session, against a real Revit. **Three fragments passed.** The rest of
this page is why the other 59 did not, which turned out to be FIVE *structural* reasons rather than
fifty-nine separate arrangement mistakes.

**One of the five is now solved.** Blocker 3 — "an element-shaped need wants a one-element
selection and a job file cannot make one" — was the largest, and it turned out to need no new code
at all. [The filter-to-one recipe](#solved--the-filter-to-one-recipe) is below, with the two
fragments it proved.

**Blocker 5 was found last and is the one worth reading first.** Building the test case on purpose
— create an element, then prove the fragment against it — is the right answer to a clean model,
and it is currently impossible: **none of the 51 proven creation fragments provides `elements`**,
which is the name every consumer asks for.

> **Register rule.** This is a new file on purpose. HANDOVER, DECISIONS, PROPOSALS,
> NEEDS-CHECKING and FRAGMENT-ISSUES all have other sessions writing to them today. The rows
> below are written to be folded into those registers later, one at a time.

## The build this was proved against

**Add-in built from `7d3d7c0`** (`origin/main`, which contains
[#165](https://github.com/Ajmalpshaik/Heron-AI/pull/165), merged 01:51 that morning),
**deployed to Revit 2020, 2024 and 2027 at 19:11–19:12 on 2026-09-17.**

The deployed binaries were **one hour stale** when the sitting began — 00:50, against a #165 that
landed at 01:51 — and #165 is the chain-consumption change that fragment proving leans on directly.
Everything below was run after the rebuild, never before it.

```
heron_version -> Heron 0.1.0, bridge protocol 2
                 Revit 2024 (session 14912) - add-in 0.1.0.0
                 Everything agrees.
```

**Model: `D:\Ajmal\Heron-Proving\Snowdon-scratch.rvt`** — Snowdon Towers Sample HVAC, 9,638 placed
elements, Revit 2024.3, sessions 14912 then 51080 after the mid-sitting rebuild. Named on every row
below, because a proof that does not name its model is not a proof.

**A second model, `Project1.rvt`** (3,420 elements, session 51080), was opened by the owner late in
the sitting and carries **10 walls** he drew — which is what `set-wall-constraints` needs and
Snowdon does not have natively. Every row below names which of the two it ran against.

**Snowdon was byte-identical on disk afterwards** — 22,847,488 bytes, timestamp `14-09-2026
12:42:21`, unchanged from before the sitting, with no lock file left behind. Every write here was
rolled back and nothing was saved. Worth stating explicitly: a matching *element count* would not
have proved that, because a count cannot see an edit — the file itself not moving can.

## The model, measured rather than assumed

Measured with `select-by-category-name` + `set-selection` + `count-elements`, one category at a time:

| view | what is in it |
|---|---|
| `FloorPlan: M1` | Ducts **22**, Duct Fittings **16**, Air Terminals **10**, Spaces **5**, Mechanical Equipment **2** |
| `FloorPlan: L3` | Ducts **307** |

**Pipes, Walls, Rooms, Lines, Room Tags and Duct Accessories returned nothing in either view.**
`list-linked-models` says why: **the architecture, structure and facades are six loaded LINKS**
(`Snowdon Towers Sample Architectural.rvt`, `... Structural.rvt`, `... Facades.rvt`, and three
more). Walls, rooms, doors and ceilings are not native to this model and cannot be selected in it.

Also measured: **workshared, 2 worksets** (`Shared Levels and Grids`, `Workset1`); **11 levels**
(`Parking`, `L1 - Block 35`, `L1 - Block 37`, `L1 - Block 43`, `M1`, `L2`, `L3`, `L4`, `L5`, `R1`,
`R2`); **1,053 ducts, every one with `Mark` unset.**

**The example job file's negative case does not work on this model.**
`tools/jobs/example.yaml` uses `categoryName: Structural Framing, inViewOnly: FloorPlan: M1` as its
empty half. Structural framing is not in that view here, so `set-selection` gets nothing and the
negative comes back `needs_unbound` — which proves nothing, exactly as rule 3 warns. Every negative
below is a **different selection that exists**.

## What passed

**`create-hvac-zone` — PASS, and SIGNED.** Positive: 5 Spaces in `FloorPlan: M1`, level `M1`, phase
`New Construction` → `added 5`, naming Stair S1, Stair S2 and Elevator E1 as leaving zone
`Default`. Negative: the same call over the 22 Ducts → `added 0`, with 23 refusals reading *"is not
a Space"*. Both phases reported the transaction group **ROLLED BACK** and the model's element count
was unchanged at 9,638 afterwards.

Ajmal PS signed it on 2026-09-17. `check-signatures` then reported it **UNUSED** — signed but still
`DRAFT` — because `accept` writes the proof and deliberately never writes `heron-status`. Promoted
to `PROVEN` here, which is what cleared `check-docs`.

**`match-element-type` — PASS** and **`align-elements` — PASS**, both via the filter-to-one recipe
below, both previously unreachable. Their drafts are in `brain/proof-drafts/` and are **unsigned**:
a pass means the evidence held, and the signature is still owed.

## Blocker 1 — Publish and Admin cannot run at all, by design

`HeronPermissions.Allows`:

```csharp
// Publish and Admin are not reachable in Phase 0 or Phase 1 at all.
if (risk > HeronRisk.Modify) return false;
```

That is unconditional and deliberate — `write.enabled = true` does not reach it. **Six of the 62 are
therefore unprovable on this build**, and no arrangement can change it:

| risk | fragments |
|---|---|
| ADMIN | `add-project-parameter`, `create-global-parameter`, `transfer-project-parameters-between-documents` |
| PUBLISH | `export-families`, `export-model-to-nwc`, `export-schedule-to-csv` |

Observed, verbatim, from `batch-prove`:

> `export-families is declared risk: PUBLISH, and Heron does not run those yet - HeronPermissions
> puts Publish and Admin out of reach for Phase 0 and Phase 1. Nothing was sent to Revit.`

**This is a question for the owner, not a defect.** Raising the ceiling is "a deliberate edit to
this method", which is not a call a proving session should make on its own.

## Blocker 2 — five chain names that nothing in the library produces

`check-minimum-clearance`, `find-nearest-elements`, `select-subcomponents`, `switch-join-order`,
`unjoin-geometry` and `extract-cad-curves` consume values from the chain rather than from the
caller. Searched across **all 372 fragments at every status**:

| chain name | produced by |
|---|---|
| `targets` | **nothing in the library** |
| `parents` | **nothing in the library** |
| `first` | **nothing in the library** |
| `second` | **nothing in the library** |
| `imports` | **nothing in the library** |

These are orphan consumers. They cannot be arranged, because there is no producer to put in front
of them — the same shape of gap that `describe-blank-parameters` records as its own reason for
existing (*"the orphan check named it, which is what a dependency graph is for"*).

## Blocker 3 — an element-shaped need wants a one-element SELECTION — **SOLVED**

This was the biggest group. The refusals are correct and well worded:

> `'target' asks for ONE PARTICULAR ELEMENT in the model, and a typed name cannot say which one.
> [...] SELECT IT IN REVIT and pass "selected"`

> `'elementIds' is an id, and Heron resolves one by NAMING the thing it belongs to - a level, a
> sheet, a view, a type. There is no rule for this name yet, so it refuses rather than searching
> every element in Snowdon-scratch`

`RevitFragment.OneElement` is explicit that **`selected` means exactly one** — *"Not 'the first of
them' [...] Zero and many are both refused."*

So the chain has to leave Revit holding **exactly one** element. Three obvious routes do not work:

- `filter-elements-by-id` refuses a bare id too, so ids observed in earlier run records cannot be
  fed back in;
- no category resolves to exactly one element in either measured view;
- every one of the 1,053 ducts has `Mark` unset, so no *text* filter narrows to one — and `Mark`
  must never be bulk-written to create one, being an identifier Revit warns on.

A fourth route does. It is below.

**Three were observed refusing**, verbatim, this sitting: `select-touching` and
`trace-connectivity` on `target` / `start`, and `find-clashes` on `against`.

**Ten more are unarrangeable for the same reason, read from their contracts rather than run:**
`measure-distance` (`first`, `second`), `measure-available-fall` (`upstream`, `downstream`),
`join-geometry` (`target`), `match-element-type` (`source`), `align-elements` (`reference`),
`select-by-host` (`host`), `select-group-members` (`group`), `read-ceiling-grid` (`ceiling`),
`propose-mep-openings` (`hosts`), `copy-from-link` (`linkInstance`, `linkedElementIds`).

Thirteen in total, which is the same number `RevitFragment.OneElement`'s own comment records as
having been unreachable when `selected` was introduced on 2026-09-13.

**And three that look like this group are NOT in it.** `set-wall-constraints`
(`baseLevelId`, `topLevelId`), `place-rooms` (`levelId`, `phaseId`, `planViewId`) and
`repoint-view-reference` (`targetViewId`) ask for levels, phases and views — and the refusal
message says those are exactly what Heron *can* resolve, "by NAMING the thing it belongs to — a
level, a sheet, a view, a type". This model has 11 named levels. Those three are blocked on model
content or on arrangement, not on this, and should be tried first by whoever picks this up.

### Solved — the filter-to-one recipe

**No new fragment was needed.** `select-by-numeric-parameter` is already PROVEN and takes
**scalars only**, so a narrow enough band isolates one element with no id and nobody clicking:

```
select-by-numeric-parameter   parameterName=Length comparison=between
                              compareValue=9.00 compareValueMax=9.05 categories=Ducts
  -> set-selection            Revit now holds EXACTLY ONE
  -> select-by-category-name  leaves `elements`, the set to work across
     keep-chain: true         or `elements` is thrown away before the run
  -> <fragment>               source / target / reference / start = selected
```

Only the **first** setup step resets the chain, so the three compose. `set-selection` writes
Revit's *own* selection, which a chain reset cannot touch — that is why the single element survives
to the fragment while `elements` needs `keep-chain`.

**The band is in Revit's internal unit** — decimal feet for a length — which is
`select-by-numeric-parameter` refusing to guess at a unit it cannot know. Measured on
`Snowdon-scratch.rvt`: `Length 9.00–9.05 ft` catches **exactly one** duct, a `Tees` (id 1519117).
`4.00–4.05`, `6.00–6.05`, `7.00–7.05` and `8.00–8.05` catch none; `5.00–5.05` catches two.
**The band is the arrangement**, and it is model-specific — measure it before writing the job.

**Two fragments proved with it**, both unreachable that morning:

| fragment | positive | negative |
|---|---|---|
| `match-element-type` | `changed 19` — 19 of L2's 191 ducts retyped, 172 already matched | `changed 0`, 10 Air Terminals refused by name: *"is a Air Terminals and the source is a Duct"* |
| `align-elements` | `aligned 175` of 191, 16 blocked | `aligned 0` — all 5 Spaces reported `blocked` |

**The `align-elements` negative was wrong first time and the run said so.** Air terminals were
assumed unmovable; they align in Z perfectly well, so `aligned 10` came back and the verdict was
`NEG NOT EMPTY`. Spaces genuinely cannot. A negative is a claim about the model, and it needs
measuring like any other.

### The recipe needs VARIATION, and a uniform model defeats it

Tried on `Project1.rvt`'s 10 walls, to reach `join-geometry` and `select-touching` — walls being
what `JoinGeometry` is actually for, and the crossing ones genuinely overlap.

It could not isolate one. All ten carry `Length`; **six of them sit inside 81.5–82.0 ft and four
inside 100–110 ft**, bisected down to a half-foot. They are drawn to matching lengths, so no band
narrows to a single element and `selected` refuses at "many" exactly as designed.

**So the recipe's precondition is a numeric parameter whose values DIFFER.** Snowdon's ducts vary
in length and it works there; a rectangle of equal walls has nothing to sort on. Worth knowing
before reaching for it: check the spread first with one wide band, then bisect, and if the count
never drops below two the model is telling you to pick a different parameter — or a different
element.

### What the recipe does NOT unlock

Stated so nobody re-runs it hopefully:

- **`measure-distance` and `measure-available-fall` need TWO elements** (`first`/`second`,
  `upstream`/`downstream`), and `selected` means exactly one. **Still blocked** — they need a
  second mechanism, not this one.
- **`select-touching` is blocked by a NAME COLLISION.** `select-by-numeric-parameter` needs
  `categories` to find the one element, and `select-touching` reads `categories` too — one `--set`
  feeds both, so the fragment can never be pointed at a different category from the filter. It
  also found nothing to overlap: cleanly joined ducts do not overlap, by design.
- **`find-clashes`** asks for `against` as an id collection rather than an element, so `selected`
  does not apply. Its own refusal already says the contract should ask for the element.
- **`trace-connectivity` runs fine** — `reached 58` from the one duct — but `reached` includes the
  start, so it can never be 0. That makes it a **D-53 tracking** proof, not a batch one.
- **`join-geometry`, `select-by-host`, `select-group-members`, `read-ceiling-grid` and
  `propose-mep-openings`** are blocked on *model content*, not on this. `Duct Insulations`,
  `Model Groups` and `Duct Accessories` all return nothing in `FloorPlan: L2`; ducts host nothing;
  ceilings and walls are in the links.

**`elements` not bounding `trace-connectivity` is NOT a defect** — read in the source before saying
so. It is the candidate pool for the *geometry fallback* only, where a connector is found sitting at
the same point despite the API flag reporting nothing joined. That is what `joinedByGeometry`
counts, and it was 0 in both runs, which is exactly why the pool made no difference.

## Blocker 4 — a fragment whose only output is `findings` can never be judged

`brain/heron_validate.py`:

```python
NOTE_KEYS = frozenset(("findings",))
# `findings` is the single exemption and NOTE_KEYS above is where it lives.
```

`describe-blank-parameters` declares exactly one provide, `findings`, and nothing else. So the
runner reports `POSITIVE UNREADABLE` however well it is arranged.

**And its evidence was a textbook D-30 pass, which is the galling part.** Chained
`select-by-category-name → set-selection → read-element-parameters` with `--keep-chain`
(the pair #165 exists for), on the 22 ducts in `FloorPlan: M1`:

| phase | bound | findings |
|---|---|---|
| positive, `parameterName=Comments` | `blank from read-element-parameters (22); absent (0)` | **1** — *"22 element(s) have Comments but it is empty"* |
| negative, `parameterName=Diameter` | `blank from read-element-parameters (0); absent (0)` | **0** |

Both halves held. The runner could not see it. `report-findings` is in the same position — it
produced a correct eleven-level report and was judged unreadable for the same reason.

**The fix belongs in the contract, not the job file**: a fragment of this shape needs a countable
declared result beside its sentences. Left as a proposal rather than done here, because changing
what a fragment provides changes its C# too, and that wants proving in its own right.

## The defect — `set-mep-justification` writes nothing, and says so honestly

Run twice against the **22 ducts in `FloorPlan: M1`**, `Snowdon-scratch.rvt`:

| passed in | `set` | what came back |
|---|---|---|
| `horizontalOffsetMm=0, verticalOffsetMm=100` | **0** | 22 × *"Refused: id 1447716: **1 of 2** offset(s) did not take the value asked for"* |
| `horizontalOffsetMm=50, verticalOffsetMm=100` | **0** | 22 × *"Refused: id 1447716: **2 of 2** offset(s) did not take the value asked for"* |

`withoutOffsets` was **0** both times, so the fragment found the parameters and believed them
writable. The negative (10 Air Terminals) correctly returned `set 0`.

**The one-of-two / two-of-two split is the whole diagnosis.** In the first run the horizontal
offset asked for was `0`; in the second it was `50`. The only value that ever "took" was the one
that already equalled what was there. `brain/fragments/set-mep-justification/impl/any/fragment.cs`
does this:

```csharp
horizontal.Set(wantedHorizontal);
if (Math.Abs(horizontal.AsDouble() - wantedHorizontal) < 1e-6) wrote++;
```

`Parameter.Set` on `RBS_CURVE_HOR_OFFSET_PARAM` / `RBS_CURVE_VERT_OFFSET_PARAM` returns without
complaint and **does not land** on these ducts in Revit 2024, even though `IsReadOnly` is false.

**The fragment is behaving correctly and the read-back is why we know.** Its own header says
*"EACH VALUE IS READ BACK. A parameter that refuses returns without complaint on some element
types."* — written as a precaution, and this is it firing on a real model. Without it this would
have reported 22 runs set and changed nothing.

**Not fixed here on purpose.** The likely repair is to drive the justification parameters
(`RBS_CURVE_HOR_JUSTIFICATION_PARAM` / `..._VERT_...`) rather than the offsets, or to set the
offsets through a different route — but that is a guess at a Revit API behaviour, and a guessed fix
to a write fragment is worth less than a precise report of what was observed. It wants its own
sitting with a section open on screen, which is what the fragment's own last finding already asks
for.

## What the model cannot supply, so the positive came back empty

Not defects. Rule 2 — *ask for what the model has* — and this model is a clean, coordinated
Autodesk sample with its architecture linked:

| fragment | arranged as | came back |
|---|---|---|
| `find-overlapping-lines` | Ducts 22 @ 25 mm, then Duct Fittings 16 @ 500 mm | `overlapping 0`, and `notStraight` listed **all 16** — there are no straight lines here to overlap |
| `select-openings` | `FloorPlan: L3` | `found 0` — no shafts or openings in the measured views |
| `report-areas` | every scheme | `schemeTotalsM2 0, unbounded 0, unplaced 0` — no placed areas |
| `select-by-electrical-circuit` | `Power` | `elements 0, panels 0` — an HVAC model carries no power circuits |

`create-assembly-views` and `set-design-option` were read and **not run**: the first needs elements
that are already assemblies, the second needs design options, and this model has neither.

## One to reproduce: two client ids gave WRONG COUNTS, not a refusal

Recorded because it nearly went into this page as a defect, and was not one.

Midway through the sitting, probing eight categories in `FloorPlan: M1` returned **307 for every
single one** — 307 being the Ducts count in `FloorPlan: L3`, a different category in a different
view. Read as a fragment answering on a stale selection, which is what rule 5 exists for.

It was not. **The cause was mine:** I had called the `revit_parameters` MCP tool, which takes the
lease under the *chat's* client id, while the probes ran under `HERON_CLIENT_ID=model-58759a`. Two
ids from one person. Re-run after the lease expired, with one id and nothing else changed:

| category in `FloorPlan: M1` | contended run | clean re-run |
|---|---|---|
| Ducts | 307 | **22** |
| Air Terminals | 307 | **10** |
| Flex Ducts | 307 | **needs_unbound** |
| Sprinklers | 307 | **needs_unbound** |

The clean numbers are right, and no fragment is at fault.

**What is still worth a look is the shape of the failure.** The contended calls did not refuse —
they came back with confident, wrong counts. Elsewhere the same conflict refuses cleanly
(*"This Revit is in use by another chat, so Heron has refused rather than taking it over
mid-job"*), which is the behaviour that makes the conflict visible. Here it was silent, and only
disagreed with a number measured ten minutes earlier.

**Not asserted as a defect** — it was observed once, as a side effect of a mistake, and has not
been reproduced deliberately. But a second id producing a wrong answer instead of a refusal is
worth one session's attention, and it is the reason the one-id rule is worth more than a
convention.

## A second defect, found and FIXED: four view needs could not reach half the views

`place-rooms` refused on every level it was pointed at. Two refusals, in order:

> `No view called "FloorPlan: L2" in Snowdon-scratch.`

> `2 views in Snowdon-scratch are called "L2", so the name does not say which one is meant.
> Rename one.`

**Both are dead ends, and the second is bad advice** — telling a modeller to rename views in their
own model to satisfy a tool. It is also unavoidable: every Revit level normally carries a floor
plan *and* a ceiling plan under one name, and **all eleven of Snowdon's plan names are duplicated.**

`RevitFragment.cs` has **two** view resolvers. `OneView` already reads the `FloorPlan: L2`
spelling, and when it still cannot decide it *lists the choices*. `OneOfClass` is the generic
by-name lookup and knows nothing about view types. The dispatch sent a need **typed `View`** to
`OneView` — but `planViewId`, `scopeViewId`, `targetViewId` and `templateId` are typed
**`ElementId`**, so they took the other path and were stuck with the weaker resolver.

Fixed by sending `View` to `OneView` from the ElementId path too. Rebuilt and redeployed to 2020,
2024 and 2027.

**Proved after the fix, on `Snowdon-scratch.rvt`:** `place-rooms` with
`planViewId=FloorPlan: L2` now resolves and **`created 30` spaces**, `unbounded 7`. The same call
refused outright before it.

**`place-rooms` still has no D-30 negative on this model**: every level places something —
`R2` created 13, `Parking` created 12. It needs a level with no enclosed region, or a tracking
proof.

## `set-wall-constraints` — a work counter declared as a result

Attempted on **`Project1.rvt`** (Revit 2024, session 51080, 3,420 elements), on **10 walls** in
`FloorPlan: 1 - Mech`. It is the library's one **STALE** signature, so re-proving it is correct.

| phase | asked for | `rehosted` | `elevationChanged` |
|---|---|---|---|
| positive | base `Level 2`, top `Level 2` | **10** | 0 |
| negative | base `Level 1`, top `Level 2` | **10** | 0 |

The negative could not come back empty, and the source says why —
`brain/fragments/set-wall-constraints/impl/any/fragment.cs` line 193:

```csharp
rehosted++;          // unconditional, once per wall that gets through
```

**`rehosted` counts walls PROCESSED, not walls changed**, and it is declared `role: result`. So it
can never reach zero for any non-empty wall selection, and D-30's empty leg is unreachable through
it. That is the work-counter shape `fragment-proving` warns about.

**Not fixed, and the obvious fix is wrong.** Moving `rehosted` to `role: accounting` would leave
`elevationChanged` as the only result — but this fragment exists to move walls between levels
*without the walls moving*, so `elevationChanged 0` is its **success**, not its emptiness. The
contract needs a count of walls whose base or top level actually differed, which is a new value
rather than a re-labelled one. Recorded for a session that can add it and prove it.

## Blocker 5 — "build the case on purpose" is the right answer and it cannot be wired up

The owner's instruction, and it is the correct one: *"if you want to test something, the creation
fragment is there — create an element and test on that."* It is also what `fragment-proving`
rule 2 says to do on a clean model: *"build it on purpose."*

**It does not work today, for two separate reasons, both measured.**

### 1. No creation fragment provides `elements`

```
proven creation fragments:           51
of those that provide `elements`:    0   -- NONE --
```

They provide `created`, `createdId`, `placed`, `groupId`, `viewId`, `marker`. Every
selection-consuming fragment needs **`elements`**. Binding is by name, so a creator can never feed
a consumer — the same orphan-name shape as [Blocker 2](#blocker-2--five-chain-names-that-nothing-in-the-library-produces),
but far wider, because it blocks the whole build-the-case strategy rather than six fragments.

**The fix is a design decision, so it is written here rather than taken.** Either creation
fragments also declare `elements` (they already hold the list — `created` *is* it), or the
executor aliases `created` to `elements` when a consumer asks for one and only a creator ran.
The first is honest and per-fragment; the second is one change and reaches all 51.

### 1b. The alias was written, deployed — and is INERT on the write path

`RevitFragment.BindNeeds` now falls back to `created` when a consumer asks for `elements`, nothing
else filled it, and the contract set no `binds` of its own. It compiles, it is deployed to all
three releases, and the string is in the shipped DLL. **It does not fire, and the reason is an
ordering one that no alias can reach.**

`RevitFragment.cs`, in the order the code runs:

| line | what happens |
|---|---|
| **424** | the fragment under test **binds its needs** |
| **435** | the fragment is **compiled**, prologue and all |
| **492** | `RunSetupSteps` runs the **deferred write setup steps** |

A MODIFY setup step on a write phase is **deferred** — the client sends it inside the request and
the add-in runs it in the same `TransactionGroup`, so the fragment can see what it made. That part
works. But the fragment has already bound and compiled **sixty-eight lines earlier**, so a value a
deferred step leaves can only ever reach the *next setup step*, never the fragment itself.
`RunSetupSteps` calls `Remember` faithfully; there is simply nobody left to read it.

**This also explains reason 2 below exactly**, which had looked like two unrelated oddities:

- `setup create-line` → **fragment** `select-by-category-name` found **2** — the fragment queries
  the model, and by then the lines exist inside the group.
- `setup create-line` → `setup select-by-category-name` → fragment found **0** —
  `select-by-category-name` is READ, so it is **not** deferred: it runs as its own call *before the
  write group is even opened*, and at that moment nothing has been created.

So the real repair is **not** a name: it is moving the fragment's `BindNeeds` and `Compile` to
after `RunSetupSteps` on the write path. That reorders the hot path of every write run and would
put a compile failure after the setup has already run rather than before — which the current order
is plainly written to avoid. **Not taken here.** It is a bigger change than the alias it replaces,
and it needs its own sitting and its own re-proving of the write fragments.

The alias stays because it is correct and costs nothing: it is the right behaviour the moment the
ordering allows a creator's output to be seen, and it already works on any path where the producer
runs as its own call.

### 2. A creation fragment's write does not survive to the NEXT setup step

Measured on `Project1.rvt`, same run shape both times:

| arrangement | what `select-by-category-name` saw |
|---|---|
| setup `create-line` → **fragment** `select-by-category-name` | `found 2`, `resolvedTo Lines`, `elements 2 item(s) [ModelLine, ModelLine]` |
| setup `create-line` → setup `select-by-category-name` → fragment `find-overlapping-lines` | `elements from select-by-category-name (0)` |

`create-line` itself is fine and does the work — run on its own it reports
`created 2 item(s) [ModelLine, ModelLine]`, `refused 0`. But the lines are visible to the
**fragment** that follows it and invisible to the next **setup step**, so a creator can only ever
be the last thing before the fragment under test — which is exactly the position that reason 1
already makes useless.

**So the case for `find-overlapping-lines` was built successfully and still could not be handed
to it.** Two model lines were drawn overlapping on purpose —
`0,0,0; 10000,0,0 | 2000,0,0; 8000,0,0`, the second lying inside the first — with a genuine empty
half arranged alongside it, two parallel lines 5 m apart. The fragment never saw either.

**One smaller thing found on the way:** `create-line` declares `view` **optional** in its
contract, and the executor refuses without it — *"'view (View)' is a value the CALLER supplies"*.
One of the two is wrong.

## A cross-drive crash in `heron_buildmatrix`, found by the suites

`tests/test_buildmatrix.py` fails on this machine, and it is **not on heron-ship §2's known list**.
It is also **not caused by anything in this branch** — the branch is one new documentation file, and
the worktree was at `origin/main` exactly when the failure first appeared.

```
2. a missing baseline is refused, not quietly written
  File "brain/heron_buildmatrix.py", line 259, in regression
    "should be." % os.path.relpath(where, ROOT)}
ValueError: path is on mount 'C:', start on mount 'D:'
```

**The repository is on `D:` and `%TEMP%` is on `C:`.** The test writes its fixture to a temp
directory, `regression()` builds its refusal message with `os.path.relpath(where, ROOT)`, and
`os.path.relpath` **raises** rather than returning something when the two are on different Windows
drives. The message being built is itself a refusal — so the code is crashing on the path where it
was trying to explain a problem.

This is the same family as [`A14`](../NEEDS-CHECKING.md): a path assumption that cannot appear on
Linux, where there is only one mount. It will hit any Windows user whose checkout is not on the
same drive as their temp directory, which on this PC is the default arrangement.

**Not fixed here.** `brain/heron_buildmatrix.py` is outside the paths this session was given to
edit (`brain/fragments/**`, `revit/**`, `mcp/**`), and other sessions are writing to `brain/`
today. The repair is small and local — guard the `relpath` and fall back to the absolute path —
but it should be someone's deliberate change, not a proving session's aside.

`tests/test_bridge_roundtrip.py` also fails here, and that one **is** documented in heron-ship §2:
it wants a built .NET test host and prints the exact line (`dotnet build
tests\Heron.Bridge.TestHost -p:RevitVersion=2024`). Left alone because building writes into
`tests/`. Worth noting that §2 says all four of its waiting suites exit **3**; this one exits **1**
on this machine, which is the difference between "waiting" and "failing" to `check-gaps`.

**The four gates pass**: `check-docs`, `check-metadata`, `check-structure`, `check-package`, all
exit 0.

**The suites: 199 run, 197 exit 0, the two above are the only failures** — count derived with
`ls tests/test_*.py | wc -l`, not read off a page. They do not share a reason, which is the thing
heron-ship §2 says to check for: one wants a build, the other hits a Windows drive boundary.

Two notes for whoever runs them next, both costing time here. The loop needs
`< /dev/null` on each suite or a prompt-driven one waits at the keyboard. And this
machine's full sweep takes long enough that a foreground run is killed and reports no total at
all — run it in the background and read the total, or you will conclude from a truncated file
that it stalled.

## Numbers

**62 DRAFT at the start** (derived, `grep -h '^heron-status:' brain/fragments/*/fragment.yaml`),
and `check-signatures.py` reported **no UNUSED**, so all 62 were genuinely unproved. It reported
one **STALE** — `set-wall-constraints`, signed by Ajmal PS on 2026-09-13, code changed under it.

| | |
|---|---|
| **passed** | **3** — `create-hvac-zone` (signed, promoted to PROVEN), `match-element-type`, `align-elements` |
| blocked by the Publish/Admin ceiling | **6** |
| blocked by an orphan chain need | **6** |
| ~~blocked by "one element, selected in Revit"~~ | ~~13~~ → **SOLVED**; 2 of the 13 proved, 2 still need TWO elements, the rest want model content this file lacks |
| unjudgeable because `findings` is the only provide | **2** |
| defects found | **3** — `set-mep-justification` (reported), the view resolver (**fixed**), `set-wall-constraints`' work counter (reported) |
| positive empty for want of model content | **6** |

**Left at DRAFT: 61**, and **PROVEN is 311** — derived, not counted by hand:
`grep -h '^heron-status:' brain/fragments/*/fragment.yaml | sort | uniq -c`.

**Three passed but only ONE moved the DRAFT count**, and the gap is the point: `create-hvac-zone`
was signed by a person and promoted, so it left DRAFT. `match-element-type` and `align-elements`
passed and are **unsigned**, so they stay at DRAFT — correctly. A pass is evidence; only a
signature is a proof.

**This paragraph said 59 until `check-docs` refused it.** The number was reasoned from "three
passed" instead of derived, and the gate caught it in the same run — which is exactly the failure
[its own §7 exists for](../../tools/check-docs.py): *"A stated count is a claim; a derived count is
a fact."* Promoting one fragment also moved `310 PROVEN` → `311` and `158 MODIFY PROVEN` → `159`
in three READMEs, and those sentences were fixed in the same change.

**What now stands between this list and a proving run is model content, not machinery.**
Snowdon Towers is a clean, coordinated Autodesk sample with its architecture linked: nothing
overlaps, nothing clashes, no areas are placed, there are no groups, no design options, no CAD
imports, no electrical circuits, and no native walls, rooms, doors or ceilings. Defect-finders
find nothing here **because there is nothing wrong** — which is rule 2 biting, not a fault in
the fragments. A second, messier model would prove more of this list than any amount of further
arrangement against this one.
