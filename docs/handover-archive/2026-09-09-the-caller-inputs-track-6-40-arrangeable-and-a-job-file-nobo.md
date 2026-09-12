# HANDOVER — 2026-09-09 (the CALLER-INPUTS track): 6 → 40 arrangeable, and a job file nobody types

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**The through-line is one question: what can a person hand a fragment?** Four changes answer it, and the
fifth is a rebase that had to be untangled before any of them could land.

### The measure that matters, taken the same way twice

`tools/generate-jobs.py` reports how many DRAFT fragments with no run record can be arranged as a job.
Run before and after each change against the **identical** library, it is the only honest number here:

| | Arrangeable |
|---|---|
| start of the track | **6** |
| after `Element` resolves by name | 6 (+2 `FamilySymbol`, which came free) |
| after eight contracts narrowed | 8 |
| after a point can be typed in | **40** |

**The units decision did four fifths of it**, and everything before it was groundwork that looked bigger
than it was. Say that plainly to whoever reads this: narrowing eight contracts unblocked almost nothing
on its own, because six of the eight were also waiting on `IList<XYZ>`.

### 1. The job file is generated now — `tools/generate-jobs.py`

`batch-prove.py` takes a hand-written job list, and **six input names were mistyped on 2026-09-09
alone** — `widthMm` for `width`, `sortByFields` for `sortFieldNames`. A mistyped name does not look like
a typo coming back: the fragment refuses, or binds nothing and reports zero, and both read exactly like
a broken fragment. This is [FRAGMENT-ISSUES 3h.4](../FRAGMENT-ISSUES.md), fourth of the four.

Everything it emits is read out of a file named beside it: DRAFT-and-no-run-record from the library and
`brain/proof-drafts/runs/`, `write: true` from the fragment's `risk:` against what `run_fragment_write`
carries in the registry, the setup chain from whether anything a *fragment* provides is needed, and
**the input names spelled from `contract.needs`** — which is the point of the tool.

**What it refuses to derive is the more important half.** The category and the view are left blank and
marked `FILL IN`. A wrong category produces a confident meaningless result — eleven times in one batch.
So does `expect:`; the candidate names are offered as a comment and never chosen. **It removes the
errors a person makes while typing, not the judgement a person has to make.**

**Nothing is dropped silently.** Every candidate is either a job or a marked one with its reason: a risk
Heron does not reach (`PUBLISH`/`ADMIN`, refused by the client on the doorstep), a shape D-54 cannot
receive, a value the setup chain does not leave behind, two needs bound to one chain value, or **nothing
to vary between the legs** — which makes it [D-53](../DECISIONS.md) tracking work rather than batch work.

**There is no generated job file committed, on purpose.** `brain/proof-drafts/runs/` is gitignored, so
*"never been in front of a model"* is a **local** fact: on a fresh clone every fragment looks untried,
and on the proving machine the list shrinks after every batch. A committed snapshot is stale by the
afternoon. **Generate it on the machine that will run it, and generate it fresh.**

### 2, 3, 4. Three type rules, and one of them was a decision nobody had made

| Landed | Rule |
|---|---|
| `Element` | resolves to an element **TYPE** by name, `Basic Wall: Generic - 200mm`. An **instance** is refused and always will be: `Element.Name` on one returns its TYPE's name, so searching instances would match every element of that type — a missing rule turned into a wrong answer |
| nine narrower classes | `WallType`, `FloorType`, `CeilingType`, `FilledRegionType`, `HostObjAttributes`, `MEPCurveType`, `FamilySymbol`, `Phase`, `FilterElement` |
| `XYZ`, `IList<XYZ>` | **[D-67](../DECISIONS.md)** — three numbers in **MILLIMETRES**; several points separated by semicolons |

**D-67 was not a preference, it was already decided and unwritten.** `HeronUnits` converts nothing but
millimetres to feet; 59 caller values are named `...Mm`; D3 is written *"200 mm. Not 200 feet"*. And
`array-elements-radial` takes `centreXMm`/`centreYMm` while `place-detail-item` takes `atXMm`/`atYMm` —
**those are points, split into millimetre scalars because there was no way to send one.** The workaround
named the unit years before anybody wrote the decision.

Two things about it were checked rather than assumed, and both could have gone the other way:

- **A direction has no unit**, so dividing it by 304.8 looked wrong. It is harmless — scaling all three
  components alike does not move a vector — but **all six consumers were read** before relying on that.
  Four call `Normalize()`; `check-obstructions` hands it to `ReferenceIntersector.FindNearest`;
  `place-family-on-face` uses it as a facing vector. **None uses the magnitude.**
- **Two separators, not one.** `"0,0,0,1000,0,0"` is two points only if you already know they come in
  threes, and a list one number short silently becomes a different, valid-looking list.

Each ordinate is bounded by `HeronUnits.MaxMillimetres` (100 km) and a number past it is **refused
rather than converted** — that is a value that arrived in the wrong unit.

### The eight contracts, and the two that would have been narrowed wrongly

Each type was read out of the fragment's **own body**, never guessed from the need's name:

| Fragment | Was `Element` | Is | Because |
|---|---|---|---|
| `create-hvac-zone` | `phase` | `Phase` | the body opened with `var asPhase = phase as Phase` — declaring the base class, then casting back |
| `create-floor` | `floorType` | `FloorType` | the 2020 overload is reached by reflection as `NewFloor(CurveArray, FloorType, Level, bool)` |
| `apply-view-filter` | `filter` | **`FilterElement`** | its own comment: a rule filter and a selection filter **share a base class** |
| `create-from-room-boundaries` | `hostType` | **`HostObjAttributes`** | branches on `hostType is CeilingType` / `is FloorType` |
| `create-electrical-run` | `runType` | **`MEPCurveType`** | builds a cable tray **or** a conduit from the same value |

`ParameterFilterElement` was the obvious reading of `filter` and it is the **wrong** one.
**Narrower than `Element` is the point; narrower than the fragment can use is a regression dressed as
precision.**

The classes are named with `typeof(...)`, not looked up by string, so one missing on a release is a
build failure rather than a refusal in front of a model. `ElementsNamed` tests the class with
`IsInstanceOfType` rather than `OfClass`, because four of them are **abstract** and which abstract
classes `ElementClassFilter` accepts is a runtime question that cannot be settled at compile time on
eight releases.

### 5. The rebase that had three stale commits in it

`fix/make-silence-illegal` was eleven commits ahead of main. #50 had merged three of them **in rewritten
form** — three `role: accounting` hunks and the inside-out-margin guard removed before merge — so the
branch's copies were stale. **Because the content differed the patch-ids differed, and
`git log --cherry-pick` recognised none of the eleven as upstream**: a plain rebase would have replayed
all three and put the margin guard back. Eight cherry-picked, three dropped, merged as #55.

**Two resolutions in it were judgement, and a reviewer should know which:**

- The `FRAGMENT-ISSUES` conflict was **not two rows about different things.** Main's paragraph concluded
  sheets and views are unreachable; the branch's restated every one of its facts and then said that
  conclusion *"was wrong, and it was wrong for an hour"*. Keeping both would leave the document
  asserting something the next paragraph calls wrong, so **the branch side was taken.**
- `731cda4` carried a real `enclosed = 0;` guard for `set-view-section-box`. **Main's side was taken**,
  and that was right for a reason found later: the guard lived **inside the margin guard**, which the
  owner had had removed. See below — it must not be re-added.

### WHAT IS NOT FINISHED

**`IList<XYZ>` was the biggest blocker and is gone; the next one is `ElementId`.** Of the fragments
still marked unarrangeable, the shapes they wait on, most-wanted first: `ElementId` (24 in the MODIFY
set alone — the 2024 32-to-64-bit change), `Element` as a specific instance (14 — needs the selection,
not text), `OverrideGraphicSettings`, `Color`, `Material`, `View3D`. **Generate the list rather than
trusting this sentence:** `python tools/generate-jobs.py`.

**The `enclosed = 0;` guard — and the question it turned into. Read this before touching
`set-view-section-box`.**

This entry first said the guard must NOT be re-added, and the reasoning was right when written: it lives
inside the **inside-out-margin guard**, that guard was removed before #50 merged, and without it the
line is dead — `enclosed++` is reached only after a non-null bounding box, and reaching it is what sets
`any = true`, so `!any` already implies `enclosed == 0`.

**Both came back the same evening in `40915d6`, and both were removed again on 2026-09-09 when the
owner confirmed the removal still stands.** `40915d6` restored them without saying so: its message
carefully documents its choices for every `fragment.yaml`, for `heron_validate.py` and for
`FRAGMENT-ISSUES.md`, and **says nothing about this `.cs` file** — a conflict resolved per-side rather
than a decision taken. `impl/any/fragment.cs` is now byte-identical to `531f47e` again, and the
`fragment.yaml` beside it keeps the role declarations that merge brought, which were wanted.

**THE SAME MERGE ALSO BROKE THE FRAGMENT COMPILE GATE, and it was red on `main` for hours.**
`set-schedule-filters` came out of it declaring `refused` **twice** — two byte-identical `provides`
entries — and the generated code then failed on all eight releases with *"a local variable named
`__provides_refused` is already defined"*. Removed with the guard.

**The shape of that is worth more than the fix.** A merge resolved per-side duplicated an entry, and
nothing above the compiler could see it — the same failure that let a duplicate `## D-56` through
`check-docs.py` the same day. **After a merge that touched contracts, run
`python tools/check-fragments-compile.py` before trusting the tree**, and read a merge's message for
what it does NOT mention as carefully as for what it does.

**That last path is silent, and that IS a real finding.** If `IsSectionBoxActive` comes back false the
fragment reports `applied false`, `enclosed 22` and **adds nothing to `refusalReasons`** — a count on a
run that changed nothing, with no reason given. That is §3h.1's exact shape in the fragment §3h.1 was
written around. **It has been read from the code and never seen happen**, so it is not written into
FRAGMENT-ISSUES, whose value is that every row was observed. Someone with Revit open should try to make
it happen before it is recorded as fact.

**Three fragments declare `IList<Element>` as a caller value** — `check-room-mep-completeness`,
`connect-air-terminals`, `propose-mep-openings`. They want instances, and a comma-separated list of type
names is not what they are asking for, so the list form was deliberately **not** added alongside the
singular.

**`IList<IList<XYZ>>` stays refused** — one need in the library, `pointPairs`. It wants a third
separator, and that is a decision for when a second fragment wants one.

### Two things about the tooling that cost time here

**`check-docs.py` could not see a duplicate decision number — FIXED the same day, and it now fails the
run.** D-67 was first written as **D-56, which already existed**, and the checker passed on a file with
two `## D-56` headings. The cause was one character of intent: every registry was read with
`set(re.findall(...))`, and a set is precisely the thing that makes a duplicate invisible. The same line
was written three times, so **rules, decisions and questions are all checked now**, not just the case
that was caught.

**It fails the run where a broken link only prints, and that asymmetry is deliberate.** A dead link
announces itself the moment somebody clicks it; a duplicate id is silent and makes every reference to
that number ambiguous — both entries look correct in isolation. Verified by breaking it on purpose once
per registry, and the unmodified repository still exits 0.

**What is still owed there: `check-docs.py` gates the build and has no test at all**, unlike every other
tool in `tools/` that concludes. That gap is older than this session and closing it moves the derived
test count, so it wants its own commit.

**A stale local `main` makes a verification gate lie.** The rebase brief's gate said
`git diff main...HEAD`, and the local `main` ref was 175 commits behind `origin/main`. The gate printed
a large diff that meant nothing. **Use `origin/main` in any gate written for somebody else to run.**

---
