# The fifteenth session, 2026-09-01 — eight fragments, and an agent nobody owned

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**What it did:** took the library from **70 to 78**. All 78 compile on all eight releases; all 78 are
`DRAFT`. The batch is weighted to drawing production, because that is where the holes were: five of the
eight were sentences answered by a fragment that does something else entirely.

| | |
|---|---|
| `READ_SELECTION` | **The other half of the library.** Every other filter starts from a set that can be DESCRIBED — a category, a type, a room, an id. This one starts from what a human already pointed at, so *"move these up 200"* finally has a *"these"*. Before it, *"what did I select"* resolved to `SET_SELECTION` — a question answered by the fragment that **overwrites the thing being asked about** |
| `CREATE_SECTION_VIEW` | *"Create a section through this"* was answering `SET_VIEW_SECTION_BOX`. Same word, opposite jobs: one makes a drawing that can go on a sheet, the other hides part of a view that already exists. Cuts **across** a run, not along it — the cross-section, which is what that sentence means |
| `LIST_SHEETS` | First fragment in the `SHT` area. A sheet **view template** is dropped (it comes back from the same collector carrying a number and a name, and listing it puts a row in the register that can never be issued); a **placeholder** sheet is listed *and* flagged, because hiding it loses a row the register deliberately contains |
| `CREATE_DIMENSION` | The registry calls *"create dimensions"* a founding example. Dimensions **centre to centre between parallel MEP runs** and refuses everything else. A reference only exists if it was asked for: without `ComputeReferences` **and** `IncludeNonVisibleObjects`, every element yields a null reference, nothing is drawn, and **no error is raised** |
| `ADD_REVISION_CLOUD` | **One cloud per element, not one around the set.** A cloud spanning scattered elements states that everything between them changed, on a drawing somebody signs. Three tight clouds where one loose one would have done is untidy and true; between untidy and false this takes untidy |
| `READ_MODEL_WARNINGS` | *"How many warnings"* was answering `COUNT_ELEMENTS` — a number about something else. Grouped by message and **worst first**, so an Error cannot hide under 600 identical warnings. It provides `elements`, so *"show me what is causing them"* composes into `ISOLATE_ELEMENTS` |
| `LIST_LINKED_MODELS` | **One row per FILE, not per placement** — counting instances reports four links as nine. Loaded is established by asking for the document, not by reading the stored status: the same discipline that produced `TRACE_CONNECTIVITY`, where the flag described intent and the geometry described reality |
| `SPLIT_MEP_RUN` | Named for what it **can** do. Revit exposes a break for ducts and for pipes and nothing else, so calling it `SPLIT_ELEMENT` would promise a wall split no release can perform. Cable tray and conduit are `MEPCurve`s too and go to `notMepCurve`, not to `refused`, which would suggest it was attempted |

### Six fragments named an agent that does not exist, and both checkers said clean

`create-duct`, `measure-mep-slope`, `measure-mep-velocity`, `read-mep-system`, `set-mep-insulation` and
`set-mep-size` all claimed **`HERON-REVIT-MEP-012`**. That id is not in the registry and never was — the
MEP owner is `HERON-REVIT-SYS-030`, and `-012` is the Family Agent. All six now point at the real one.

**The reason it survived is the interesting half, and it is a gap between two checks that each look
complete.** [`tools/check-metadata.py`](../../tools/check-metadata.py) *does* audit agent ids against the
registry, and *deliberately* skips `brain/fragments` — its own comment gives the reason, and the reason
is good: a fragment's metadata standard is its `fragment.yaml`, and adding a `Heron-` header beside it
would give two places to update and one that goes stale. So fragments sat outside the only check that
looks, and [`brain/heron_fragment.py`](../../brain/heron_fragment.py) — the one that *does* read them — was
not looking. Neither was wrong on its own. The gap was between them.

`heron_fragment.py` now parses the registry (it does not copy it — adding an agent there is enough) and
refuses a fragment naming an id that is not in it. **Proven by putting the bad id back and watching the
validator fail on that fragment**, not by trusting that it would.

> **A checker's exclusion is a claim that something ELSE covers that ground.** When you write one, say
> which tool takes the part you are skipping — and when you skip a thing nothing else reads, that is not
> an exclusion, it is a hole with a comment over it.

### The routing was measured against a baseline, not just run

Adding eight fragments to a 70-fragment corpus changes what every OTHER fragment ranks against, so
`check-routing.py` was run on `HEAD` first and the two collision lists diffed. Four collisions were
genuinely introduced and all four are fixed:

- *"cut the pipe here"* went to `CREATE_SECTION_VIEW` — because its prose said *"a duct, a pipe, a
  wall"*, and *"cut"* is in both jobs. The enumeration went.
- *"draw the duct run"* went to `SPLIT_MEP_RUN` — its purpose used **three forms of the word "draw"** to
  explain which end the distance is measured from. Reworded.
- *"is this on any drawing"* went to `CREATE_SECTION_VIEW` — introduced by the *fix* to the first one,
  which is why it was re-measured after each edit rather than once at the end.
- *"the ones I have selected"* moved to `READ_SELECTION`, and that one is a **correction, not a
  regression**: `FILTER_ELEMENTS_BY_ID` resolves ids somebody already has and cannot ask Revit what is
  highlighted. It was never that fragment's sentence to answer.

**What was NOT fixed, and is recorded rather than smoothed over.** Five pre-existing collisions got
worse — the loser slipped a rank or two — and one flipped its winner (*"number the sprinklers along the
branch"*, now `ARRAY_ELEMENTS` over `RENUMBER_SEQUENTIAL`) between two fragments this session did not
touch. That is corpus growth changing the word weighting, and chasing it means editing other fragments'
honest prose on a guess. It is the price of a bigger library and it should be watched, not papered over.

> **Run `check-routing.py` on `HEAD` before your batch and diff the two lists.** Run only after, and
> every pre-existing collision looks like yours — 8 lines that were really 4, and half the "new" ones
> were old ones a rank lower.

### One composition claim was wrong, and `composable()` agreed with it

`CREATE_SECTION_VIEW`'s purpose said `PLACE_VIEW_ON_SHEET` could take the new view straight from it, and
`composable()` returned **true**. It was true for the wrong reason: `PLACE_VIEW_ON_SHEET` declares
`views` as `source: request`, so it has **no fragment-sourced needs at all** and every producer composes
with it vacuously. Nothing flows across; the host carries the view into the next call. The purpose now
says so.

**What none of this is.** Not one fragment has met a model. `check-gaps` reports **0 unfinished, 55
waiting**, and counts **78** below `PROVEN`.

---
