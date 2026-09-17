# What actually blocks the 62 drafts

**2026-09-17.** The model session, against a real Revit. One fragment passed. The rest of this
page is why the other 61 did not, which turned out to be four *structural* reasons rather than
sixty-one separate arrangement mistakes.

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
elements, Revit 2024.3, session 14912. Named on every row below, because a proof that does not name
its model is not a proof.

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

**`create-hvac-zone` — PASS.** Positive: 5 Spaces in `FloorPlan: M1`, level `M1`, phase
`New Construction` → `added 5`. Negative: the same call over the 22 Ducts → empty. A draft is
waiting in `brain/proof-drafts/` for a person's signature; **a pass is not a proof.**

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

## Blocker 3 — an element-shaped need wants a one-element SELECTION, and a job file cannot make one

This is the biggest group, and the refusals are correct and well worded:

> `'target' asks for ONE PARTICULAR ELEMENT in the model, and a typed name cannot say which one.
> [...] SELECT IT IN REVIT and pass "selected"`

> `'elementIds' is an id, and Heron resolves one by NAMING the thing it belongs to - a level, a
> sheet, a view, a type. There is no rule for this name yet, so it refuses rather than searching
> every element in Snowdon-scratch`

`RevitFragment.OneElement` is explicit that **`selected` means exactly one** — *"Not 'the first of
them' [...] Zero and many are both refused."*

So the chain has to leave Revit holding **exactly one** element. On this model it cannot:

- `filter-elements-by-id` refuses a bare id, so ids observed in earlier run records cannot be fed
  back in;
- no category resolves to exactly one element in either measured view;
- every one of the 1,053 ducts has `Mark` unset, so no parameter filter narrows to one — and
  `Mark` must never be bulk-written to create one, being an identifier Revit warns on.

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

**What would unblock it** is a way to hand the chain one element without a person clicking — a
`take-first` / `filter-to-one` fragment, or a resolution rule for `elementIds`. That is a design
decision, so it is written here rather than taken.

**There may already be a route, and it is worth ten minutes before anyone builds one.**
`select-by-numeric-parameter` is PROVEN and takes **scalars only** — `parameterName`, `comparison`,
`compareValue`, `compareValueMax`, `tolerance`, `includeTypeParameters`, `categories` — so a
narrow enough band should isolate a single element without any id:

```
select-by-numeric-parameter   parameterName=Length comparison=between
                              compareValue=9.00 compareValueMax=9.01 categories=Ducts
  -> set-selection            (Revit now holds exactly one)
  -> <fragment>               target=selected
```

Its value is handed in **already in Revit's internal unit** and that is deliberate — a length is
decimal feet, so 3000 mm is `3000/304.8`. This was set up and not completed: the lease was held by
the other client id at the time (see below), so no band was ever measured. **The idea is untested
and is recorded as a lead, not a result.**

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
| passed, draft awaiting a signature | **1** (`create-hvac-zone`) |
| blocked by the Publish/Admin ceiling | **6** |
| blocked by an orphan chain need | **6** |
| blocked by "one element, selected in Revit" | **13** — 3 observed refusing, 10 read from their contracts |
| unjudgeable because `findings` is the only provide | **2** |
| defect found and reported | **1** (`set-mep-justification`) |
| positive empty for want of model content | **4** |

**Left at DRAFT: 61.** The largest single thing standing between this list and a proving run is
Blocker 3 — a supported way to put one element in the chain.
