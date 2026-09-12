# The twentieth session, 2026-09-02 — eight fragments, and the first batch no compiler has read

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**What it did:** took the library from **138 to 146**, and it is the first batch in this repository's
history that has **never been compiled**. That is written at the top of this file and in the register as
`A9`, because a session that quietly skipped the gate and said nothing would be indistinguishable from
one that ran it.

**Why it could not run.** There is no .NET SDK in this container and none can be installed: the SDK
download host answers **403 to the CONNECT itself** — the network's policy, not a transient failure.
Checked, not assumed: `curl "$HTTPS_PROXY/__agentproxy/status"` names the host and the refusal.

**What was done instead, and exactly what it is worth.** nuget.org IS reachable from here, so the Revit
API reference assemblies for **2020, 2024 and 2027** were downloaded and read directly — every type and
every member each new fragment calls was looked up in the assemblies' own metadata, with the argument
COUNT where there were overloads.

That answers *"does this member exist in this release"*, which is the failure this repository has
actually had — `Document.CreationGUID` compiled on 2024 and does not exist on 2020, and it had survived
several readings. **It answers nothing about argument TYPES and nothing about syntax.** A metadata read
is not a compile and must not be recorded as one.

> It did earn its keep twice. `ExternalDefinitionCreationOptions.Type` is **present on 2020 and gone by
> 2024**, which is what makes `ADD_PROJECT_PARAMETER` a reflection job rather than a written-down call;
> and `Space` carries neither `Number` nor `Area` of its own — both come from `SpatialElement`, so a
> member check that does not walk the base type reports a false absence. Worth knowing before trusting
> any tool of that shape.

### The eight

| | |
|---|---|
| `CREATE_PIPE` | The plumbing twin of `CREATE_DUCT`, and deliberately the SAME SHAPE - a run of points, one segment per pair. The earlier library drew one pipe between two points and drew duct as a run; one job in two shapes is how a composition that works for duct fails for pipe. **It sets no size**: Revit snaps a diameter to the nearest its type allows and returns TRUE while doing it - 77 mm asked for, 80 mm delivered - so that check stays in `SET_MEP_SIZE`, in one place |
| `SET_MEP_SLOPE` | Puts a fall on a drainage run by moving one end. **A riser is never sloped, and the test is geometric rather than a millimetre threshold**: the earlier library used a 300 mm minimum run, which a 2 m drop with 10 mm of horizontal run walks straight past - it would be re-drawn 10 mm long and the pipe destroyed. A run that rises more than it runs is a riser. It also **reads** Revit's Slope parameter rather than writing it: a parameter written to disagree with the geometry is a lie every schedule then repeats |
| `CONNECT_OPEN_ENDS` | Joins open connectors already at one point - the "touching but Revit says not connected" cleanup. Four tests and all four must pass, and the one that matters is **facing**: two pipes crossing at the same height pass distance, domain and size, and are not a joint. **Nothing moves**, so a wide gap tolerance buys a model that REPORTS connected while the hole is still there |
| `FIND_DUPLICATE_VALUES` | Two doors marked D-101. Duplicate DATA, where `FIND_DUPLICATE_ELEMENTS` is duplicate GEOMETRY - the two share the word and nothing else. **A blank is not a duplicate**, and "180 of these have no mark" is reported as its own finding, as is "these do not carry that parameter at all" - folding those together lets a question nobody asked come back as a clean bill of health |
| `FIND_OVERLAPPING_TAGS` | Annotation printing on top of annotation, measured in **paper millimetres** - the same two tags are clear at 1:50 and merged at 1:200. **It projects onto the view's own right and up directions**, where the earlier library compared model X against model Y: right in a plan, and in a section it reports nearly every pair, because a view looking along X has all its annotation at one X |
| `COLOR_BY_PARAMETER` | A colour per value - colour by system, by level, by type. **No palette**: a list of six colours gives the seventh system the first system's colour, which is the defect the hue stepping was written to fix. **And the start hue is fixed, not random** - the earlier library re-rolled it each run "for variety", which makes today's drawing incomparable with the one issued last week |
| `ADD_PROJECT_PARAMETER` | Creates a shared parameter and binds it. **ADMIN, not MODIFY**: a binding changes the project's data structure for everyone, and un-binding discards every value anybody typed. **The shared parameter file path is asked for, never invented** - the earlier library fell back to a temp folder, which produces a shared parameter nobody can find again. **And an existing binding is extended, not replaced**: ReInsert with today's categories alone strips the parameter off every category it had |
| `DUPLICATE_TYPE` | *"Make a new duct type at 300 wide"* - which resolved to `CREATE_3D_VIEW` before this. **More than one source type is a refusal, not a loop**: Revit needs type names unique, so one literal name over five types succeeds on the first and quietly does not on the rest - a batch that reports success and half happened |

### One was not written, on purpose

`PLACE_SPACES` was in the plan and was dropped after reading `PLACE_ROOMS`, which already places spaces —
it takes a `placeKind` and calls `NewSpaces2`. A second fragment for it would have been a near-duplicate
in the most crowded kind of area, and the cost of that is not tidiness: it is retrieval order, which is
not ours to choose.

### The gaps were found by asking the brain, not by working down a list

Before writing anything, twelve of the owner's own sentences were put through `heron_brain.lookup` and
the answers read. That is what chose the batch, and some of the answers were worth the exercise on their
own:

| The sentence | What came back | |
|---|---|---|
| *"make a new duct type at 300 wide"* | `CREATE_3D_VIEW` | |
| *"the tags are on top of each other"* | `ALIGN_MEP_ELEVATION` | a WRITE, on MEP |
| *"add a project parameter"* | `COPY_PARAMETER_VALUE` | a different write |
| *"create a ceiling in this room"* | `MEASURE_CEILING_HEIGHT` | a read answering a create |
| *"add a new workset"* | `LIST_WORKSETS` | |
| *"change the material on these"* | `READ_ELEMENT_MATERIAL` | |
| *"draw a cable tray"* | `DIMENSION_MEP_RUNS` | |
| *"reload the links"* | `LIST_LINKED_MODELS` | |
| *"put a scope box round this area"* | `SET_VIEW_SECTION_BOX` | |

**The last six are still true and are the obvious next batch** — `CREATE_CEILING`, `CREATE_WORKSET`,
`REPLACE_MATERIAL`, `CREATE_CABLE_TRAY`, `RELOAD_LINKS`, `CREATE_SCOPE_BOX`. `JOIN_GEOMETRY` belongs on
that list too: *"join these walls together"* now lands on `CONNECT_OPEN_ENDS`, which joins MEP
connectors and cannot do it, so `PLACE_MEP_FITTING`'s table says so out loud until something can.

> **All of that was done by the twenty-first session, below, except one**: `CREATE_SCOPE_BOX` cannot be
> built at all — Revit exposes no way to create one on any supported release. Read that section before
> reaching for it again.

### The routing was measured against a baseline, not just run

The new fragments were moved aside and `check-routing.py` run on the 138 that were there before, so
every number below is a difference rather than an impression.

| | before | after |
|---|---|---|
| Utterances | 802 | 860 |
| Claimed in a routing table and NOT reached | 5 | **3** |
| Shortlist collisions | 107 (13.3%) | 122 (14.2%) |

**Every one of the new fragments' own sentences resolves correctly through `heron_brain.lookup`** —
checked one at a time, because the collision list measures the keyword route alone and the eighteenth
session recorded two consecutive sessions being misled by exactly that.

**The unreached-claim count went DOWN while eight fragments were added**, and that is where the real
work was:

- Three of the new fragments claimed a sentence in a table and declared it nowhere. `"add a drain line"`
  was resolving to **`CREATE_GRID`**. A routing table is a comment; comments are not indexed.
- Two of them wrapped a quoted sentence across two lines, and the checker read the fragment. **Keep each
  sentence on one line** — nothing says so anywhere else, so it is said here.
- The reciprocal tables went into **eighteen** counterpart fragments, per the rule that a cross-reference
  written one way only routes whoever lands on the newer file. Seven sentences those tables claimed were
  then declared as utterances, which is the same defect the seventeenth session named and it reappears
  every time a table is written.
- **Two of the five pre-existing claims were not harmless and were fixed**: `"is the model healthy"` was
  being served by `EXPORT_MODEL_TO_NWC` — a PUBLISH answering a read — and `"move these to level 3"` by
  `CREATE_LEVELS`, which makes levels. The other three are near-synonym pairs where both sides do the
  same kind of work, and they are left alone rather than decided blind.

### Two checkers earned their place again

- **`check-structure.py` refused `ADD_PROJECT_PARAMETER`**: its reflection named
  `Autodesk.Revit.DB.SpecTypeId` in a string, and the adapter boundary says that name belongs inside
  `revit/`. The fix is better code, not an exemption — the namespace is taken from `typeof(Document)`,
  so it follows the API instead of being written down beside it.
- **`tools/README.md` was claiming "all 32 fragments compile on all 8 releases"** while the library was
  past four times that. The line is gone rather than corrected: a count typed into prose goes stale the
  day after it is true, and a stale green is believed. The tool's own output is the count.

---
