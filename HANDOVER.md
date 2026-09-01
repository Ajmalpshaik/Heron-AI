# Heron AI — Session Handover

**Updated 2026-08-30, during the seventh working session — the one re-authoring the owner's earlier
fragment library into this one.** The sixth wired the brain to the host, which was the last thing here
that could be *built* without a machine; the seventh is doing the thing that can still be done without
one — **writing fragments**, from 7 to **32** so far, each studied and rewritten rather than copied
([D-44](docs/DECISIONS.md): none of them inherits the earlier library's proven status). For whoever picks
this up next: a fresh Claude session, a person, or the owner on his phone.

---

## The first five minutes of the next session

**Run this before reading anything else. It is computed from disk, so it wins over every sentence
below:**

```bash
python tools/check-gaps.py
```

It sweeps the build order against what is actually on disk, runs every test and every checker, checks
every agent id against the registry, walks the fragment library, the capability registry and the
dependency graph, and reads the register. Then it sorts everything into **UNFINISHED** and **WAITING**,
and its exit code follows only the first.

**As of 2026-08-31 it reports 0 unfinished and 55 waiting.** Everything waiting needs a machine, a
dependency or a conversation: **48 a real Revit**, plus the 86 unproven fragments as one further item,
**3 Windows** (`A4`, `A6` and `A8`), **1 a network that can reach the weights host** (`A7`), **1 the
owner** (`R1b`), and **1 an optional dependency this machine does not have** — that last one is new and
it is `test_mcp_serves.py` reporting honestly that it was skipped, rather than being counted as a pass.

> Those figures were **read off the tool, not carried forward**, and the sentence they replace shows why
> that matters: it said *55 waiting* and then listed parts summing to 54, and it still counted **3 owner
> items** after `R1` and `R2` had been struck off in the same session. A total and its own breakdown
> disagreeing is the cheapest possible drift to catch and it survived anyway. Re-derive rather than
> edit the digits.

**`A8` is mostly done, and doing it found the worst defect in this repository's history — read this
one first.** It was written down as *needing Windows*. It did not: the blocker was a missing **pip
package**, and the MCP SDK is pure Python. Installing it took ten minutes and produced two things.

**A real SDK now serves all ten tools** — names, descriptions and argument schemas — and the three brain
tools answer through its own dispatch with both refusals intact. That is the half `test_brain_reachable.py`
could only ever read *as text*, and it is now [`tests/test_mcp_serves.py`](tests/test_mcp_serves.py).

**And installing it revealed that Heron's MCP server would not start at all on a fresh machine.**
`pip install --user mcp` — the exact line [`tools/HeronRevit.ps1`](tools/HeronRevit.ps1) hands the user —
now resolves to SDK **2.x**, which **deleted `mcp.server.fastmcp`**: `FastMCP` was renamed `MCPServer`.
The server's import was written against 1.x, so it raised `ImportError` before registering a single tool.
**Every Heron tool absent from the host, on any machine installing today, with no Revit and no Windows
involved.** Nothing in the repository could see it, because nothing here had ever imported the SDK — the
entire suite reads that file as text, and text cannot fail an import.

The fix is the import and nothing else: the class is looked up newest-first, because 2.x's `MCPServer`
takes the same `@server.tool()` decorator and the same `run()`. **Proven on both SDK majors installed
side by side**, and validated the way this repository requires — the old line was put back and watched
to fail. `heron_version` now reports which SDK is serving, since *"Heron stopped working"* and *"the SDK
moved underneath it"* look identical from the user's side.

**What genuinely remains of `A8` needs the PC:** a real host over stdio — that Claude Code connects,
renders the docstrings and picks a tool from them. In-process dispatch is a strong signal ahead of that,
not a substitute for it.

**If the tool and this file ever disagree, believe the tool.** It is computed from disk; this file is
typed. That is not a hypothetical — for most of 2026-08-29 this tool reported `UNFINISHED - nothing`
while the entire Phase 2 brain sat unreachable, because it checked that each step's module and test
existed and never asked whether anything called them. The check that catches it was added the same day.

### Then, in order

| | What | Where it happens |
|---|---|---|
| **1** | ~~`R1`~~ **DONE 2026-08-29 — all 21 read back, all 21 confirmed, nothing moved.** What is left of the review is **`R1b`**: show him the trust model working with his own fragments in it, because [D-14](docs/DECISIONS.md) stays *Proposed* until he has seen it | Needs a screen |
| **2** | **`B1` to `B4`** — open Revit 2020, look for the **Heron AI** tab, press **Heron**, then `ping` and `count` | Needs Revit |
| **3** | **`C3`** — with `write.enabled` still **false**, ask for a move and watch it be **refused, by name**. Prove the gate before testing the write, or a passing move proves nothing | Needs Revit |
| **4** | **`D1`–`D3`** — *"move the ducts up 200 mm"*, say yes, then **MEASURE ONE**. The single most important line in the whole register | Needs Revit |
| **5** | **`D5`** — **one** Ctrl+Z puts it all back. Two means Golden Rule 16 is broken | Needs Revit |

Everything else in [`NEEDS-CHECKING.md`](NEEDS-CHECKING.md) follows in the order written there. It is in
dependency order on purpose: nothing in group D can be attempted before group A passes.

### And the ones that need no Revit at all

- **`A7`** — the trained embedding backend has **never run**, and 2026-08-31 sharpened *why* on a second
  and different container. `pip install model2vec` **works** — PyPI is reachable and the package is
  fine. What fails is `StaticModel.from_pretrained("minishlab/potion-base-8M")`, with
  `ProxyError: 403 Forbidden`. **So the precondition is not "a working network", it is reaching
  huggingface.co**, and the register row now names the host instead of saying "a model host" — a
  sentence anyone with a working network would reasonably read as already satisfied. Until it runs,
  search finds words and not meaning, and [`tests/test_embed.py`](tests/test_embed.py) says so in
  measured numbers. **One thing it did prove**: with the package actually installed, the fallback ran
  against a *failed download* rather than a missing import — a branch that had never once executed —
  and it degraded to `lexical` and said so on its first line.
- **`A4` and `A6`** — both need Windows but not Revit. `A4` is the Windows named pipe itself; `A6` is
  thirty seconds confirming the SDK probe reads Windows correctly.
- **`A8` was on this list and nobody knew it**, filed under *needs Windows* when what it needed was
  `pip install mcp`. That is the fourth time something here turned out to be waiting on somebody trying
  it rather than on a machine. **The count is now four, and the standing advice stands: assume the
  fifth is out there.** The one that cost the most was believing an environment-specific wall was a
  property of the project.

### The one thing that was buildable here — now built

**The brain was wired to nothing, and now it is wired.** Eight modules, seven fragments and ten skills
were on disk, tested and passing, with **no MCP tool reaching any of them.** The host talks to Heron only
through the tools in [`mcp/server/heron_tools.py`](mcp/server/heron_tools.py), and every one of them went
straight to the bridge. That left Phase 2's third definition-of-done clause open for **no external reason
at all**, which is why it was worth doing on a machine with no Revit.

**What was added**, all of it read-only and none of it touching a model:

| | |
|---|---|
| [`mcp/server/heron_brain.py`](mcp/server/heron_brain.py) | The one seam between the MCP side and `brain/`. Opens the store, rebuilds it if the machine is fresh, indexes if stale, and hands back rows. **It holds no knowledge of its own** |
| `heron_capabilities` | What Heron knows how to do: ten jobs, which have every part provided, and the seven capabilities nothing provides |
| `heron_resolve` | Who can do one capability, at what risk, on which releases — **asked for by capability, never by fragment id** |
| `heron_lookup` | The user's own sentence resolved to a **capability**, with the provider underneath as evidence rather than as the answer |
| [`tests/test_brain_reachable.py`](tests/test_brain_reachable.py) | Step 12's acceptance test re-run through the seam: add a better provider and the call site is the same line; delete the original and it still answers |

**Two things it deliberately does not do, and every answer says both out loud:**

- **Resolving is not running.** A fragment carries C# in `impl/`, the bridge speaks a fixed set of
  operations, and none of them compiles one — [D-28](docs/DECISIONS.md)'s in-process Roslyn is unbuilt.
  So the host can now learn *what would do the job* and still cannot have it done. A tool that let that
  be inferred would be worse than no tool, because a plan built on it fails at the last step.
- **Nothing underneath is proven.** Every skill and every fragment is still `DRAFT`.

**The version filter reaches the host as a wall, not a preference.** The release comes from the bound
Revit session, read **without ever asking and without claiming a lease** — looking must never be the act
of claiming, which is the lesson `revit_health` learned about the lease one commit after building it.
With no Revit connected the tools still answer and say the filter did not run.

**The original finding came from running `tools/check-metadata.py` and following what it said**, hours
after `check-gaps.py` had reported everything clean. `check-gaps` now has a check for it — and that check
is an **import** check, so treat its green accordingly: it can see that the seam exists, not that a host
ever called through it. That is `A8`.

### The library, and the question that was put to the owner

**This section used to say seven capabilities were wanted by the skills and provided by nothing, and to
ask before building them.** It was right to ask. He answered, and the answer changed the shape of the
work: build the fragments now, re-authored from his earlier library, and check every one of them in Revit
later — *"checking in revit we will do after because that is a big work... mark as not verified and when
pc came we will check."*

**So the library is 86 fragments and every skill has every capability provided.** The seven were written
on 2026-08-29; twenty-five more followed on 2026-08-30, and seven more on 2026-08-31 — those last chosen
by asking the brain the owner's own sentences and reading what came back, rather than by working through
the earlier library in order. `python brain/heron_skill.py` shows no gaps.

**And his second instruction is the one that must not be quietly undone.** None of them inherits the
earlier library's proven status, however well proven it is there:

> *"Even in the aj ai proven fragment dont mark in heron this is proven because we will check each and
> everyone again in heron ai so mark it as a not proven in heron."*

That is [D-44](docs/DECISIONS.md), and it is **enforced rather than remembered** —
[`brain/heron_fragment.py`](brain/heron_fragment.py) refuses a status above `DRAFT` whose proof does not
match the implementation in front of it. The gate had existed and nothing stood on it: a fragment
declaring `PROVEN` on another model's proof passed every check in this repository, which was measured by
writing one.

**What that buys, and what it does not.** All eighty-six fragments compile on all eight releases and
none of them will fail at the PC for a reason a compiler could have found — which on 2026-08-31 stopped
being a figure of speech, when the gate caught a tag accessor that Revit 2027 has removed. Not one has met a model. The debt did
not go away — it got **counted**, which is the whole point of `check-gaps` keeping *unfinished* and
*waiting* in two lists that must never be one.

---

## The sixteenth session, 2026-09-01 — eight fragments, and the search backend hit its ceiling

**What it did:** took the library from **78 to 86**. All 86 compile on all eight releases; all 86 are
`DRAFT`. Two of the eight close a hole where a request to CHANGE the model was being answered by a
read-only report.

| | |
|---|---|
| `CREATE_SHEET` | The missing link in producing a set: `CREATE_SECTION_VIEW` makes the drawing, this makes the sheet, `PLACE_VIEW_ON_SHEET` joins them, `LIST_SHEETS` reads it back. **The number is checked for a clash BEFORE anything is created** — Revit refuses a duplicate by throwing, and a throw part-way through leaves an unnumbered sheet in the register that nobody finds until an issue |
| `CREATE_TEXT_NOTE` | View-only annotation, and the refusals name the fix rather than saying "it did not work". Guarded on 3D views, schedules and templates — three different reasons, three different remedies |
| `LIST_LEVELS` | **Two elevations, and the obvious one is not the one on the drawing.** `Elevation` is measured from the internal origin; `ProjectElevation` is what the level head reads. They agree until somebody moves the base point — so most projects, which is exactly why the difference goes unnoticed until the one where it matters. Both reported when they differ. Sorted by HEIGHT, because "Level 10" sorts before "Level 2" as text |
| `CREATE_LEVEL` | Takes the height in the drawing's terms and converts, by **measuring the base-point shift off an existing level** rather than assuming it is zero. And it says out loud that **a level created through the API has no plan view** — Revit's *tool* makes those, the level does not, and the modeller who looks in the Project Browser and sees nothing concludes it failed when it did not |
| `LIST_WORKSETS` | **A closed workset is why a count comes back low, and nothing else says so.** Elements on one are not loaded: no collector returns them, no schedule counts them. Provides no `elements` on purpose — a `Workset` is not an `Element` and an empty list to look composable is a lie the graph would act on |
| `FIND_DUPLICATE_ELEMENTS` | *"Find duplicate elements"* was answering `RENAME_ELEMENTS`. **One of each cluster is kept and is NOT returned** — that is what makes the result safe to hand to `DELETE_ELEMENTS`. Same type AND same place both required: two different types at one point is a design clash, and filing it here puts a coordination problem in a list people bulk-delete from |
| `PLACE_MEP_FITTING` | *"Join these two"* was answering `CREATE_DUCT` and *"connect these two ducts"* was answering `TRACE_CONNECTIVITY` — **a read-only report answering an instruction to build something.** The fitting is chosen from the geometry (in line + same size → union, in line + different → transition, at an angle → elbow, three → tee, four → cross), which is what Revit does itself. **Nothing is moved to close a gap**: ends that are apart are refused with the distance |
| `CREATE_SCHEDULE` | A schedule VIEW that goes on a sheet, as distinct from `GROUP_AND_COUNT`, which puts numbers in the reply. A field the category does not have is **reported**, not skipped — a silently short schedule prints with a column missing and nobody reading it can tell one was wanted |

### Quoting another fragment's sentence in `purpose` hands it that sentence

The first draft of `LIST_WORKSETS` explained itself by quoting what other fragments answer — *"how many
ducts are there"*, *"is the model clean"*, *"a clash check comes back clean"*. **`purpose` is indexed.**
Those quotes took **four** sentences off the fragments that own them, including `READ_MODEL_WARNINGS`'s
own declared *"is the model clean"* and `SET_ELEMENT_WORKSET`'s *"they are on the wrong workset"* — a
request to change the model, answered by a read. Moving the examples into a `#` comment fixed all four
at once.

> **Illustrate a fragment with the sentences it OWNS.** Another fragment's example goes in a comment,
> where a reader sees it and retrieval does not. The same edit fixed `CREATE_SHEET`, whose purpose named
> `PLACE_VIEW_ON_SHEET` and thereby took *"add the view to the drawing sheet"* off it.

### Two utterances were retired, and the reason is not a rank

`CREATE_TEXT_NOTE` declared *"write this on the drawing"* and *"type this on the plan"*. Both lost — the
first to `WRITE_ELEMENT_PARAMETERS`, which is a **MODIFY**, so an annotation request was reaching a bulk
parameter write. The cause is that **write** and **type** are both Revit nouns, and a sentence whose
strongest word means something else in this domain will keep losing to the fragment that owns that
meaning. No rewording of the losing file changes it. The phrasings now avoid both words and the
ambiguity is recorded in the fragment; if somebody genuinely says one of them it belongs in a glossary
as a site-word mapping, not as an utterance that quietly loses.

### The built-in search backend has saturated, and a test was rewritten to say so

`tests/test_retrieve.py` failed. Its section 1 asserted that *"show me every duct in the model"* returns
at least two of four named claimants; at 86 fragments exactly **one** does, and the shortlist is
warnings, selection, sheets, findings and levels — **nothing about ducts.**

Measured rather than guessed: drop two words and the answer is right.

| query | top 3 |
|---|---|
| `show me every duct in the model` | QA-004, SEL-001, SHT-001 — none about ducts |
| `every duct in the model` | MEP-010, MEP-006, MEP-003 |

So the shortlist is decided by **"show me"**, not by **"duct"**. That phrase now opens an utterance in
half the library (*"show me the levels"*, *"show me the sheets"*, *"show me the warnings"* — each of them
exactly what a modeller says), while the one discriminating word carries almost no weight. The top five
span **0.0017**, well under one rank of fusion: nothing is being ranked at all. **n-gram similarity
separated 28 fragments and does not separate 86.**

**No utterance was weakened to make it green**, and the rewritten check says so in the file. What it
asserts now is what is true and stable — that the discriminating word still works when the saturating
phrase is out of the way — which is precisely the gap **`A7`** would close. A7 is the trained embedding
backend that has never run because the weights host is unreachable from here. It has been the least
urgent blocker on the register for weeks. **It is now the most urgent one**, and this is the first
measurement that says why.

> A test assertion was rewritten this session. That is worth flagging rather than burying: it is exactly
> the move that turns a real regression into a green tick. The defence is that the file had done it once
> before at 28 fragments, for the same reason, and recorded it; that the new assertion measures
> something *harder* rather than something looser; and that the old claim is left in place above the new
> one with the numbers that falsified it.

**What none of this is.** Not one fragment has met a model. `check-gaps` reports **0 unfinished, 55
waiting**, and counts **86** below `PROVEN`.

---

## The fifteenth session, 2026-09-01 — eight fragments, and an agent nobody owned

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
complete.** [`tools/check-metadata.py`](tools/check-metadata.py) *does* audit agent ids against the
registry, and *deliberately* skips `brain/fragments` — its own comment gives the reason, and the reason
is good: a fragment's metadata standard is its `fragment.yaml`, and adding a `Heron-` header beside it
would give two places to update and one that goes stale. So fragments sat outside the only check that
looks, and [`brain/heron_fragment.py`](brain/heron_fragment.py) — the one that *does* read them — was
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

## The fourteenth session, 2026-09-01 — four fragments, two filters, and one that was already there

**What it did:** took the library from **66 to 70**. All 70 compile on all eight releases; all 70 are
`DRAFT`. Six candidates went in; **one turned out not to need building at all**, which is the part worth
reading.

| | |
|---|---|
| `FILTER_ELEMENTS_IN_ROOM` | **A room is a VOLUME, not a rectangle.** An L-shaped room's bounding box covers ground outside it, so a box test puts the neighbour's diffuser in your schedule — quietly, and it looks right until somebody counts on site. Uses Revit's own point-in-room test, and handles **Space as well as Room**, because an MEP model carries Spaces where the architectural one carries Rooms |
| `FILTER_ELEMENTS_BY_TYPE` | *Select All Instances*, as a request. **Whole model or this view is an input, not a default** — the two differ by an order of magnitude, and choosing silently is how a change meant for one floor reaches nine |
| `READ_ELEMENT_MATERIAL` | Every material with its volume, not the first — a wall has every layer of its build-up. A zero-volume finish is still **named**, because dropping those loses exactly what a finishes schedule is about |
| `COPY_PARAMETER_VALUE` | Compares storage types **before writing anything**, and a mismatch stops the whole batch. It copies what is **stored**, never what is displayed: a length shown as "2400 mm" is stored in feet, and copying the display into text writes a unit-suffixed string no later calculation can use |

### The one that did not need building

*"What is the elevation of this"* was answering `READ_ROOM_GEOMETRY` and looked like a missing
capability. **It was not.** `READ_ELEMENT_LEVEL` already returns the level and the offset, and those two
*are* the elevation. The gap was in the **words**, not in the library — so two utterances were added and
no fragment was written.

> A second fragment computing the same number from the same inputs is a duplicate that can one day
> disagree with the first, and nothing would say which was right. **Check whether the library already
> composes the answer before adding to it** — the same check `count them by level` passes, since
> `READ_ELEMENT_LEVEL` into `GROUP_AND_COUNT` is that job already.

### The compile harness gained a namespace, and that is a promise to unbuilt work

`FILTER_ELEMENTS_IN_ROOM` needs `Room.IsPointInRoom`, which lives in `Autodesk.Revit.DB.Architecture` —
not among the namespaces the harness supplied. The alternative was hand-rolling point-in-polygon over the
boundary segments, **which is precisely how an L-shaped room gets answered wrongly**, and that is the case
the fragment exists to get right.

> **The `USINGS` list in [`tools/check-fragments-compile.py`](tools/check-fragments-compile.py) is a
> contract with unbuilt work.** It declares what a fragment may assume is in scope, so
> [D-28](docs/DECISIONS.md)'s Roslyn executor must supply the same set. A namespace added here and not
> there compiles green and fails at the PC — the second obligation this library has placed on that
> executor, after the `REVIT20xx` compile symbols.

**What none of this is.** Not one has met a model. `check-gaps` counts **70** fragments below `PROVEN`.

---

## The thirteenth session, 2026-09-01 — seven more, and the question that costs an hour

**What it did:** took the library from **60 to 66**, plus one. All 66 compile on all eight releases; all
66 are `DRAFT`.

| | |
|---|---|
| `DIAGNOSE_VISIBILITY` | ***"Why can I not see my ducts"* — the most expensive question in Revit**, because there are at least eight unrelated answers and no one place to look. **It asks the ground truth first**: a collector scoped to the view returns what the view actually contains, so everything after is explaining a fact rather than predicting one. **It reports EVERY cause, not the first** — two at once is routine, and fixing one leaves it still invisible. And it **names what it did not read** rather than inventing the likeliest, because an invented cause reads exactly like a found one |
| `SET_VIEW_RANGE` | The commonest of those causes. **Reports the range before changing it, always** — setting one blind is how a coordinated drawing starts showing the storey above, and the old numbers are the way back |
| `SET_VIEW_SCALE` | Changing scale changes every tag and dimension's apparent size. A template usually **owns** scale, so a batch that refuses is pointing at the template, not the views |
| `FIND_VIEWS_SHOWING_ELEMENT` | **Ask before deleting.** An element on four issued sheets is a different decision from one on none, and Revit will not warn. Genuinely expensive — one collector pass per view, because Revit keeps no reverse index — and it says so rather than pretending otherwise |
| `ARRAY_ELEMENTS` | **The direction is normalised**, so a vector handed in between two points 12 m apart does not silently multiply the pitch twelvefold. Count includes the original, as Revit's own array does. Plain copies, not a linked Array element — which changes what every later edit does |
| `SET_ELEMENT_LEVEL` | **The one with the trap, and the trap is the point.** Height is an OFFSET from the level, so changing the level and leaving the offset makes a duct at 2800 above Level 1 into one 2800 above Level 2 — three metres higher, silently, found when somebody sections through it. The offset is recomputed and **the absolute height verified afterwards** |

### `DIAGNOSE_VISIBILITY` and `FIND_VIEWS_SHOWING_ELEMENT` are the same read asked two ways

One view, many causes — or one element, many views. Worth knowing together, because the second is what
turns *"can I delete this"* into an answer.

### An invented agent id, caught before the tool caught it

`ARRAY_ELEMENTS` was first written claiming `HERON-REVIT-GEO-007`, **which does not exist** — there is no
GEO agent in the registry, and every other geometry fragment claims `HERON-REVIT-ELE-010`. It was found
by checking rather than by assuming, which is the third invented agent id this repository has seen and
the first not to reach a checker.

**And `SET_VIEW_CROP`'s guard from the last session has a sibling worth noting**: two of this batch's
fragments (`SET_VIEW_RANGE`, `SET_VIEW_SCALE`) take an `apply` flag so the same fragment answers *"what
is it"* and *"change it"*. That is deliberate — the read is what makes the write reversible, and
splitting them would let somebody set a value without ever recording what it was.

**What none of this is.** Not one has met a model. `check-gaps` counts **66** fragments below `PROVEN`.

---

## The twelfth session, 2026-09-01 — seven more, weighted to the MEP job

**What it did:** took the library from **53 to 60**, chosen the same way. All 60 compile on all eight
releases; all 60 are `DRAFT`. The batch leans deliberately towards the work the owner actually does —
three MEP capabilities and three drawing-production ones.

| | |
|---|---|
| `MEASURE_MEP_VELOCITY` | *"Is this duct too small for the flow"* — which returned `CREATE_DUCT`. **Computes velocity from flow and area AND reads Revit's own, then reports both**: `RBS_VELOCITY` is derived from the size and flow last calculated, so a duct resized afterwards keeps reporting the old figure and looks fine on a schedule. Two numbers disagreeing is the finding |
| `SET_MEP_INSULATION` | Ducts and pipes in one selection, because a modeller does not sort a run by discipline first. **Existing insulation is replaced, not added to** — Revit carries two layers happily and the run then occupies a size nobody drew. **A clash check run before insulating is a clash check of the wrong model** |
| `READ_MEP_SYSTEM` | The system **name** and the system **type** are different things and "system" means both: two ducts can share *Supply Air* and be on separate systems, which is exactly what somebody is checking. Read as what the model **says** — `TRACE_CONNECTIVITY` is what goes and looks |
| `CREATE_3D_VIEW` | The name is set **after** the view exists, so a duplicate name leaves a usable view rather than an exception and an orphan. `named` is a separate output for that reason |
| `SET_VIEW_CROP` | Crop, section box and isolate are all asked for as *"just show me this bit"* and do different things. **Only the crop changes what prints** — on a view already on a sheet, whatever falls outside is gone from the issued drawing and no reviewer can tell it was ever there |
| `PLACE_VIEW_ON_SHEET` | Asks `CanAddViewToSheet` **before** attempting. A view can be on only one sheet, which is the cause of most refusals here, and the fix is to duplicate it — a different fragment |
| `MEASURE_ELEMENT_VOLUME` | Surface area is the MEP number: lagging and painting are bought by the square metre. Measured from the **solids**, walking nested geometry — reading only the top level finds nothing for most equipment. **Insulation is not included**, so pricing lagging means measuring the insulation elements |

### What the checkers caught this time

**`check-structure` caught a house-rule break the compiler was happy with.** The insulation fragment
used fully-qualified `Autodesk.Revit...` type names, which crosses the adapter boundary
([docs/16 §4](docs/16-version-support-strategy.md)) — every other fragment relies on the harness's own
`using` lines. It compiled perfectly on all eight releases; only the structure rule saw it.

> **And then the fix's own comment failed the same check**, because the rule matches on TEXT and the
> sentence explaining it spelled the namespace out. Reworded rather than teaching the checker to parse
> comments: a blunt rule that catches its own explanation costs one rewording, and a comment-aware
> parser costs something that can be wrong. Same family as `check-gaps` reporting its own regex as two
> undeclared agents.

**One error was caught by re-reading before compiling, not by a tool.** `SET_VIEW_CROP` first guarded
with `!view.CropBoxActive && !view.CanBePrinted`, which refuses on the wrong condition entirely and
would have compiled green. The rule it wanted was: a template or a view that cannot be printed.

**Retrieval, measured on 317 declared sentences**: words **92% first, 99% in the top three**; nearness
**67% and 84%**. Contested: **25 of 317**, all judgements. The risk-direction section is empty for the
reason it now states itself.

**What none of this is.** Not one of the seven has met a model. `check-gaps` counts **60** fragments
below `PROVEN`.

---

## The eleventh session, 2026-08-31 — seven more, and a retraction

**What it did:** took the library from **46 to 53**, and **withdrew a finding the previous session
reported.** All 53 compile on all eight releases; all 53 are `DRAFT`.

### The retraction, first, because it was told to the owner as fact

The tenth session added a check for *a question answered by something that writes*, said it **found six
and that four were pre-existing library defects**, and named them. **All six were false.** Nothing in the
library was wrong. The check was.

**How it was wrong, in two stages, and the second is the instructive one.**

| | |
|---|---|
| **First** | It measured `taken` — the **keyword ranking**. Its own printed claim was *"a caller acting on the top hit"*, and no caller uses the keyword route alone |
| **Then** | Corrected to `retrieve` — the fused stage. **Still wrong, and far less obviously.** The host calls `find`, which tries the **identity** and cache short circuits *first*. `retrieve` is fusion alone |
| **Why that hid it** | `check-routing` asks every fragment **its own declared utterances** — and those are exactly the sentences `find` answers by identity, *before any ranking runs*. So the check was ranking sentences the host never ranks |

Verified directly: all six resolve to their own fragment on route `identity`.

> **The rule this leaves.** A check that makes a claim about **consequence** must call the same entry
> point the system calls — not the stage that looks like it. A per-route diagnostic may report any route
> it likes; it may not describe one route's ranking as what the system does. The section above it has
> always said which route it measures, and was right to.

**The check is kept, now calling `find`, and it prints its own limit rather than a bare green.** It is
empty **by construction** while the identity route holds, and its emptiness says the identity route
works — not that no question can reach a writer. **The real risk surface is paraphrase**, which no
fragment declares and this corpus therefore does not contain. What it still catches: a declared question
that stops matching by identity, or two fragments declaring one sentence where the survivor writes.

**One thing from that session survives on its own merits** — `TAG_ELEMENTS` really was carrying a
composition sentence that belonged at the step it starts from, and moving it was right for reasons that
have nothing to do with the faulty check.

### The seven

| | |
|---|---|
| `FIND_CLASHES` | Element-against-element, **solids not bounding boxes** — a box around a diagonal duct overlaps everything in the rectangle it spans, which is how a clash report grows to hundreds of rows nobody reads. **Not `CHECK_OBSTRUCTIONS`**, which casts a ray from a *point* before anything is modelled; the two are asked for in the same words |
| `GROUP_ELEMENTS` | Reads the member count back **from the group**, not from the input. Creating a group creates the condition that makes `MOVE_ELEMENTS` report blocked |
| `UNGROUP_ELEMENTS` | Hands the released members back as `elements`, so *ungroup then move* composes. Grouped, pinned and owned all present as *"it will not move"* and have three different fixes |
| `READ_ELEMENT_PHASE` | **Both** phases. Created-in-Existing and created-in-Existing-**and-demolished** read identically until the second is asked for, and they are opposite instructions on site |
| `SET_ELEMENT_WORKSET` | `worksetId` is an **int**, deliberately — a `WorksetId` is not an `ElementId` and was untouched by 2024's 64-bit change |
| `APPLY_VIEW_TEMPLATE` | A template **overrules** hand-applied graphics rather than deleting them, silently. That is the conflict the grayout job has to route around |
| `DUPLICATE_VIEW` | The option has **no default**: `AsDependent` is not a copy in the sense anybody means, and picking it by accident produces a view that mysteriously refuses changes later |

**No new routing problems.** The seven introduced none, and the risk-direction section is empty for the
right reason. Contested sentences: **21 of 282**, all judgements.

**What none of this is.** Not one of the seven has met a model. `check-gaps` counts **53** fragments
below `PROVEN`.

---

## The tenth session, 2026-08-31 — seven more, and a new check that found four old defects

**What it did:** took the library from **39 to 46**, chosen the same way — the brain asked the owner's
own sentences, and the answers read. All 46 compile on all eight releases; all 46 are `DRAFT`.

The holes it closed were answered wrongly rather than not at all: *"change the name of these"* returned
`SET_MEP_SIZE`, *"who owns this"* returned `TRACE_CONNECTIVITY`, *"what is this element"* returned
`MEASURE_ELEMENT_LENGTHS`, and **deleting was missing entirely**.

| | |
|---|---|
| `DELETE_ELEMENTS` | The most destructive thing in the library. **Reports the set Revit actually removed, never the input count** — a wall takes its doors and windows, a duct takes its fittings, and `alsoWent` is the difference between a tidy-up and an accident. Run inside a rolled-back transaction, the same call is a deletion **preview** |
| `RENAME_ELEMENTS` | Find-and-replace inside the name, because *"change SUP to SUPPLY"* is the shape the job takes. An element whose name does not contain the text is `notMatched`, never counted |
| `CHANGE_ELEMENT_TYPE` | Reads the type before **and after**: `ChangeTypeId` sometimes returns quietly having done nothing, the same silent no-op as the move that moved nothing |
| `DESCRIBE_ELEMENTS` | *"What is this"* — category, family, type. Read from the **type**, not the instance name, and the id is formatted rather than read as a number, which is what keeps it clear of 2024's 64-bit `ElementId` |
| `READ_ELEMENT_OWNERSHIP` | Which elements another user holds, **before** a batch stops at the eleventh with ten already changed. It is `E8` in the register, and it never checks anything out — looking must not be the act of claiming |
| `READ_ELEMENT_LEVEL` | The level **and the offset**, because the level alone misleads: a duct on Level 1 with a 3800 mm offset sits above Level 2's floor and correctly reports Level 1 |
| `SET_VIEW_SECTION_BOX` | Box the clash and look at it. Sets the box **and switches it on** — setting without activating leaves the view identical, which reads as the call having done nothing |

### The new check, and why it is not just another collision report

`check-routing` reported eighteen contested sentences and could not tell the harmless ones from the
dangerous ones. Two of the new collisions were the same shape: **a question answered by a fragment that
writes to the model.** *"What category is this"* landed on the fragment that overrides category
graphics; *"check the tagging on this drawing"* landed on the one that places tags.

> **That is different in kind from a collision between two reads.** A caller acting on the top hit does
> not get a slightly worse answer to its question — it **changes the model in reply to one**. Risk is
> already declared on every fragment, so the check costs a lookup.

**It reported six on its first run and ALL SIX WERE FALSE. Corrected 2026-08-31 — see the eleventh
session below.** They were an artefact of the check measuring the keyword ranking of sentences the host
never ranks at all. Do not act on the four "pre-existing library defects" this paragraph used to name:
*"list every duct in the model"*, *"these ones"*, *"the ceiling height in here"* and *"follow the pipe"*
are each served correctly, by their own fragment, through the identity route.

**One of the six was a genuine over-claim and the fix stands on its own merits.** `TAG_ELEMENTS` carried the
utterance *"tag the ones that are not tagged yet"* — which is find-**then**-tag, a composition, the layer
Steps 10 and 11 already established no fragment can win. Held on the writing fragment, it dragged
`FIND_UNTAGGED_ELEMENTS`' own read sentence across with it. Moved to the step the composition **starts
at**, which hands `elements` straight to the tagger. Six became five.

**What none of this is.** Not one of the seven has met a model. `check-gaps` counts **46** fragments
below `PROVEN`, and compiling proves the API surface agrees and nothing about whether a duct is deleted,
renamed or measured correctly.

---

## The ninth session, 2026-08-31 — seven fragments, and three of them were found by tools

**What it did:** took the library from **32 to 39**, chosen by evidence rather than by picking from the
owner's earlier library in order. All 39 compile on all eight releases; all 39 are `DRAFT`
([D-44](docs/DECISIONS.md)).

**How the seven were chosen.** The brain was asked the sentences the owner actually says, and the
answers were read. Some were not near misses but opposites: *"how many metres of pipe"* returned
`CREATE_DUCT`, *"renumber the doors"* returned `SNAP_TO_GRID`, and **nothing could undo a hide** — Heron
could take things off a view with `HIDE_ELEMENTS` and `ISOLATE_ELEMENTS` and had no way to put them back,
so every hide was a one-way door. Those holes picked the batch.

| | |
|---|---|
| `SHOW_ELEMENTS` | Closes the one-way door. **Undoing a hide is not the mirror of doing one**: a permanent hide is undone element by element, a temporary one cannot be — Revit's only exit is leaving the mode, which brings back everything. So the temporary path ignores its own element list and says `temporaryModeCleared` rather than a count that would imply a precision it has not got |
| `SET_PIN_STATE` | The other half of the move defect this repository already found: Revit's move returns normally and moves nothing for a pinned element. Reports what **changed**, counting already-pinned separately |
| `MEASURE_ELEMENT_LENGTHS` | The takeoff question. Anything without a length is **named, never counted as zero** — a fitting scored 0 mm gives a total that looks complete and is short |
| `RENUMBER_SEQUENTIAL` | Does **not** decide the order — that is the caller's. Checks collisions across the whole batch **before writing anything**, because a half-renumbered corridor has two schemes in it and no record of where it stopped |
| `MEASURE_MEP_SLOPE` | Computed from the endpoints, not read from the slope parameter — a pipe whose parameter says 1% and whose ends are level is flat. Being a ratio, it is the one measurement in the library that **cannot carry a unit error** |
| `TAG_ELEMENTS` | One tag per element, skipping the already-tagged: a second pass otherwise stacks a duplicate exactly on top of the first, invisible until somebody drags one |
| `FIND_UNTAGGED_ELEMENTS` | **Written because the dependency graph said it had to exist** — see below |

### Three defects the tools found, and only one of them was in the C#

**1. The compile gate caught a version break that no documentation would have.** `find-untagged-elements`
was written against `IndependentTag.TaggedElementId`, which compiled clean on 2020 and 2022 and **failed
on 2027 — the property is removed there.** Its replacement, `GetTaggedLocalElementIds()`, **arrived in
2022**. The overlap is 2022–2026 and **no single accessor spans the supported releases**, so the
implementation splits on a compile symbol. This is the owner's own named problem — *"it errors on a newer
Revit"* — caught before the machine instead of mid-job.

> **It puts a requirement on unbuilt work.** This is the first fragment in the library to need a
> `REVIT20xx` compile symbol, so [D-28](docs/DECISIONS.md)'s in-process Roslyn executor **must define the
> same symbols MSBuild does**. A host defining none takes the `#else` branch and breaks on 2020 and 2021
> only — which is the release the owner actually runs.

**2. The dependency graph refused a fragment that compiled perfectly.** `TAG_ELEMENTS` took an
`alreadyTagged` list and **nothing on disk could produce one**, so `test_graph.py` reported an action
nothing can feed: a composition that cannot be assembled however well each half compiles. The easy fix
was to mark that input as coming from the request, which would have made the graph quiet and the problem
permanent — nobody can type a list of element ids. So the missing piece was written instead, and it
answers a real question of its own: *"which ducts have not been tagged"*.

**3. `check-routing` caught the new fragment stealing another's sentence — and the first fix made it
worse.** `RENUMBER_SEQUENTIAL` was answering *"set the room number"*, which is
`WRITE_ELEMENT_PARAMETERS`' own declared utterance and a plain single write. The fix written first added
a disclaimer to `purpose` quoting that sentence — **and `purpose` is indexed**
([`heron_search.py`](brain/heron_search.py) puts it in the searchable text), so the fragment was handed
one more copy of the words it was losing on and did not move.

> **The general rule, and it applies to every routing table in the library: a disclaimer written into an
> indexed field makes the fragment claim the sentence it is disclaiming.** Routing tables belong in
> comments, where a human reads them and the index does not. Contested sentences went 16 → 15 of 205 once
> the quote was removed from `purpose`.

**And a test fixture that had expired twice was made to derive itself.** `test_graph.py` hardcoded the
providers of `elements` — one until 2026-08-29, two until this session, and each time a correct library
failed the test and the fix was to type one more path. It now finds them from disk. Its first version
matched the same two lines under `needs:` as well, which broke the composition from the consumer's side
and passed for the wrong reason; it is scoped to the `provides:` block.

**What none of this is.** Not one of the seven has met a model. They are `DRAFT`, `check-gaps` counts
**39** fragments below `PROVEN`, and compiling proves the API surface agrees and nothing whatever about
whether a duct is measured in millimetres or feet.

---

## The eighth session, 2026-08-31 — the server that would not have started

**What it did:** installed the MCP SDK for the first time in this project's life, which took `A8` from
*needs Windows* to *mostly done* and, in the same ten minutes, found that **Heron's MCP server would not
start at all on a machine installing today.**

**The defect, and why nothing here could see it.** `pip install --user mcp` — the line
[`tools/HeronRevit.ps1`](tools/HeronRevit.ps1) hands the user, unpinned — resolved to 1.x when the server
was written and resolves to **2.x** now. 2.x **deleted `mcp.server.fastmcp`**: `FastMCP` was renamed
`MCPServer`. The import was written against 1.x, so the server raised `ImportError` before registering a
single tool — **every Heron tool absent from the host**, with no Revit and no Windows anywhere in the
failure. Every test in this repository reads that file **as text**, and text cannot fail an import. The
one technique nobody had used was the obvious one: install the dependency and start the thing.

| | |
|---|---|
| **The fix** | The import, and nothing else. The class is looked up **newest first**, because 2.x's `MCPServer` takes the same `@server.tool()` decorator and the same `run()` — measured, not assumed, with both SDKs installed side by side. This is [D-05](docs/DECISIONS.md)'s rule about Revit releases applied to a Python dependency: an unlisted version must fail **loudly**, so the last `except` re-raises naming the install line rather than leaving an `ImportError` about a module the user never typed |
| **The check that would have caught it** | [`tests/test_mcp_serves.py`](tests/test_mcp_serves.py) — the SDK's own registry against the source text, every description, every argument schema, and the three brain tools called through the SDK's own dispatch. **Validated by putting the defect back** and watching it fail under 2.x, which is the standard this repository already holds `check-api-surface.py` to |
| **`heron_version` now reports the SDK** | *"Heron stopped working"* and *"the SDK moved underneath it"* are indistinguishable from the user's side, and this tool's stated job is what to say when something is wrong |

**A skipped suite is now WAITING, not `ok`.** The SDK is an optional dependency, so on a machine without
it that test cannot run — and `check-gaps.py` sees **only exit codes**, since it sends stdout to
`DEVNULL`. A suite that skipped would have been reported `ok`. So a skip exits **3**, and `check-gaps`
reads it as WAITING: the same distinction the whole tool is built on, extended to the one place it could
not reach. This is the *"a killed run prints a header with nothing under it, and a blank reads as a
pass"* failure, caught before it happened rather than after.

**Two register rows were wrong in a way that would have failed a correct Heron**, and both were found by
running what they asked for rather than by reading them:

- **`A8`'s PASS text was stale.** It required *ten jobs, **four** with every part provided, and the seven
  missing capabilities named*. The truth is **10, 10 and none** — the row was written on 2026-08-29
  *before* the seven capabilities were written later that same day, and never revisited. As worded, it
  would have failed a Heron that was working correctly.
- **`A8` also required `heron_lookup` to answer `FILTER_ELEMENTS_BY_CATEGORY`.** It answers
  `SET_SELECTION`. That is **the assertion Steps 10 and 11 already retired**, copied into the register
  and left behind when they retired it: *"select all the ducts"* is filter-**then**-select, a composition,
  and a composition is what a **skill** names. It has been retired here too, **with its reasoning
  written down**, because deleting it quietly would have looked identical and taught nobody anything.

**`A7` is still blocked, and the row now names the right wall.** `pip install model2vec` **succeeds** —
PyPI is reachable. `StaticModel.from_pretrained("minishlab/potion-base-8M")` fails with
`ProxyError: 403 Forbidden`. The precondition was written as *"a machine that can reach a model host"*,
which anyone with a working network would read as already satisfied; it now says **huggingface.co**. And
the attempt proved one thing for free: with the package installed, the fallback ran against a **failed
download** instead of a missing import — a branch that had never once executed — and degraded to
`lexical`, saying so on its first line.

> **The lesson, and it is the same one four times now.** *"It needs Windows"* was a guess that got
> written down as a fact and inherited. It needed `pip install mcp`. The count of things believed to be
> waiting on a machine that were waiting on somebody trying them is now **four** — the .NET SDK, the
> WindowsDesktop targets, the bridge round trip, and this. **Assume the fifth exists.**

---

## The seventh session, 2026-08-30 — the library, and two bugs it found

**What it did:** took the fragment library from 7 to **32**, re-authored from the owner's earlier Brain
under [D-25](docs/DECISIONS.md) — studied and rewritten, never copied, split where a 243-line original
was really two jobs. All 32 compile on all eight releases. All 32 are `DRAFT` ([D-44](docs/DECISIONS.md)).

**Two real bugs came out of the studying, and they are the same bug wearing different clothes.** Both are
this project's defining failure: **reporting the number that was ASKED FOR as the number that HAPPENED.**

| | |
|---|---|
| **The move path** | Revit's move call returns normally and moves nothing for a pinned element **or a group member**. Heron skipped pinned ones — but *a duct in a group is not pinned*, so it passed the filter and the wrong count reached the answer **and the audit log**. `RevitWrite` now probes positions before and after: **moved / partly / blocked / unverified**. `E10` is the check |
| **The parameter path** | Revit accepts a size, returns **TRUE**, throws nothing, then **snaps** it to the nearest size the type carries — ask a pipe for 77 and it comes out 80. `WRITE_ELEMENT_PARAMETERS` trusted that return value. Its own proof case asked a *human* to check the stored number matched, which the code had no way of knowing |

**The parameter fix is worth reading before writing anything that sets a value.** It needs no string
parsing and no unit handling, because it uses the snap's own timing: *straight after the set the
parameter still reports what was asked for, and the snap happens at REGENERATION.* So read the internal
double, regenerate once for the batch, read it again, compare. Two numbers from the same API in the same
units. `SET_MEP_SIZE` carries the same check for sizes reached by `BuiltInParameter` — deliberately a
second home, because a name lookup for *"Width"* finds nothing on a French install.

**A new tool, and it earned itself on its first run.** Adding a fragment can make an **existing one
unfindable**, silently, and nothing here would have noticed. It had already happened twice.
[`tools/check-routing.py`](tools/check-routing.py) asks every fragment its own declared words back to the
search. It found sixteen contested sentences; **three were real errors** — a filter claiming two of
`TRACE_CONNECTIVITY`'s sentences, and an override fragment claiming the grayout **skill's**. Fixing those
took the words route to **100% in the top three**. The other thirteen are genuine English ambiguities and
were left alone.

> **It never fails a build, on purpose.** A collision is a judgement, not a defect. A checker that failed
> here would teach people to weaken their own utterances to buy back a rank — and that is the one
> response ruled out, because taking *"show me just these"* off the isolate fragment would make the
> isolate unfindable in order to protect a number.

**One decision is waiting for the owner at the PC.** `SET_CATEGORY_GRAPHICS` made the grayout skill
wrong: greying every wall one at a time gives the right drawing today and a wrong one tomorrow, because a
wall drawn afterwards keeps its normal graphics and nobody finds out until it prints. The skill now greys
the background **by category**. Whether he wants that, or wants it selection-scoped so he can grey some
walls and not others, is a modelling preference the API does not settle — it is written into
[`brain/skills/mep-grayout.yaml`](brain/skills/mep-grayout.yaml) as a decision for him, not a default
someone chose quietly.

**And four tools were found answering when they should have declined** — retrieval printing `nothing
matched` against an **empty store** (indistinguishable from a real miss), searching unknown flags as
text, a structure checker failing purely on **timing** if a compile was running, and a test asserting a
fact about the corpus while its comment claimed it tested the backend. None changed a recorded number.
All four would have corrupted a later one.

---

> **If you read only one thing:** three walls fell on one day, none of which needed a Windows machine.
>
> **The C# compiles.** Revit 2020 through **2027**, every project, zero warnings — and the Revit-free bridge
> host *runs*, all 32 checks passing including the whole lease. It caught two real defects on its first
> run, both of which reading had already missed twice ([docs/30](docs/30-compiling-away-from-windows.md)).
>
> **Every open question is answered — 41 of 41**, and **24 decisions** were taken (D-20 to D-43). Nothing
> gates any phase. Several were settled by *looking* rather than deciding: at a system already doing the
> job, and at Heron's own code, where Q-13's answer had been running since Step 1.
>
> **The Constitution is accepted and binding** — all 30 Articles, after Ajmal asked for every one to be
> read out rather than tapping yes. Reading it aloud found three stale statements inside it.
>
> **PHASE 2 STARTED, 2026-08-28.** The owner has **no Revit for about a week** and said so plainly:
> *"checking in Revit is not possible within 1 week, so keep the checking process as a document and
> start Phase 2 — we need to finish that."* [`NEEDS-CHECKING.md`](NEEDS-CHECKING.md) is now a **record
> rather than a gate**, and [27 — Build Order](docs/27-build-order.md) carries **Steps 7 to 14**, written
> that day because Phase 2 had never been broken into steps. Seven of the eight need no Revit; only
> Step 14's proof does.
>
> **Step 7 is built and proven-as-far-as-it-can-be**: the fragment's shape on disk, and the validator
> that refuses a proof with no negative case. `python tests/test_fragment_store.py`. The library grew to
> **7** fragments over Steps 9–14; every one is `DRAFT` and **none has met a model** — which is what
> DRAFT means, and why they get no register row.
>
> **Step 8 is built too**: one knowledge store per scope, as one file each. A cross-scope query is
> impossible to *write* — the API takes one scope and `ATTACH` is refused by name, which is the one
> loophole in "one file per scope". Project knowledge with no project identified is **refused, never
> defaulted**, because a wrong guess writes one client's knowledge into another's file.
> `python tests/test_scope_store.py`.
>
> **Step 9 is built**: finding a fragment by exact words, on three routes that name themselves —
> `identity` (one lookup, no search), `cache` (this wording was resolved before), `keywords` (FTS5).
> **Only a `PROVEN` fragment may run off an exact match without asking**, and two fragments claiming one
> sentence is a *miss* rather than a coin toss. It found a real gap in Step 7 on its first run: fragments
> had no `utterances`, so `OST_DuctCurves` matched nothing — [09 §2](docs/09-skills-and-fragments.md) had
> asked for them and Step 7 had not built them. Now required.
> `python tests/test_search.py`.
>
> **Step 10 is built, and it is the one to read the caveats on.** Embeddings are local, offline and need
> nothing installed — but the built-in backend is **character n-grams, not meaning**, and the tests say
> so in measured numbers rather than in prose: synonyms score **zero or less** (`diffuser`/`grille`
> −0.136), while plurals, word endings and word order all work. The trained backend that *would*
> understand synonyms **has never run**: `huggingface.co` is refused by this container's network, so no
> weights could be fetched. `A7` in the register is that run, and it needs **no Revit and no Windows** —
> any machine with a working network will do.
>
> It also recorded its own successor's acceptance test: on *"show me every duct"* the keyword route
> ranked the right fragment first and the vector route ranked it second, so **Step 11 must fuse them.**
> **That assertion has since been retired, and the reasoning is below** — it held at two fragments and
> stopped at seven, because the sentence is a composition and a composition is what a *skill* names.
> `python tests/test_embed.py`.
>
> **Step 11 is built, and it passed that test at the time — read how, and read what happened to the
> test.** The version filter runs **first, as a wall**: a fragment declared for 2021 is not returned for 2025, proven by making it the best possible
> textual match and watching it stay absent. The two routes are then **fused** rather than chosen
> between, weighted by which embedding backend is running, because Step 10 measured the built-in one as
> not-meaning and an equal vote would over-trust it.
>
> Two of its own claims were wrong and the tests caught both. The quality nudge was **eight times larger
> than one rank of fusion** — it could have jumped a `PROVEN` fragment eight places over better matches,
> which is precisely what its docstring said it could not do. And *"both routes agree"* turns out to mean
> **nothing** while the library is smaller than the retrieval pool: the nearness route ranks every
> eligible fragment, so everything agrees, including a question about cats. The answer now says so in
> its own note. `python tests/test_retrieve.py`.
>
> **Step 12 is built** — the capability registry, and it passes its own acceptance test: a second
> provider is added and **the call site is the same line of code**, then the first is deleted and the
> same line still answers. A capability nobody provides **is** the gap, so there is no second list to
> keep in step.
>
> Its one real design decision: **almost everything is derived rather than stored**, which is
> [D-40](docs/DECISIONS.md) applied. Risk in particular — it already has two homes (the tool registry
> and each fragment) and a third declaration would guarantee that one day two disagree and nobody knows
> which is true. So a capability's risk is the **highest among its providers**, computed — and providers
> that disagree about it are **reported as a defect**, because a thing that reads and a thing that
> modifies are not two implementations of one capability. A declared value would have hidden exactly
> that. Platform support is an **intersection** for the same reason: a union would claim 2027 because
> one provider manages it, then hand back one that does not. `python tests/test_capability.py`.
>
> **PHASE 2 IS BUILT IN FULL — all eight steps, 7 to 14.** Step 13 is the dependency graph, where every
> edge but one is computed on demand and the deriver was **shown catching a break** before its clean
> answers were believed. Step 14 is ten skills, each naming **capabilities and never fragments**.
>
> **`python tools/check-gaps.py` is the one command to run first now.** It sweeps everything — the build
> order against disk, every test, every checker, every agent id, the library, the registry, the graph,
> the register — and sorts it into **UNFINISHED** and **WAITING**, with the exit code following only the
> first. It now reports **nothing unfinished** and **55 waiting**: 47 need a real Revit, 3 need Windows,
> 1 needs a reachable network, 3 need a conversation with the owner. **It reported nothing unfinished
> until 2026-08-29 for the wrong reason**, when the reachability check was added; that earlier green was
> real about the build order and wrong about the repository. This one was earned by wiring the brain up —
> but read it knowing what the new check asks: whether the seam **exists**, not whether a host has ever
> called through it. That second question is `A8`.
>
> **It found real defects on its first runs, including two in itself.** A scanner that scans itself finds
> itself — it reported its own regex as two undeclared agents. It also caught a second invented agent id
> in as many steps. And growing the library from 2 fragments to 7 exposed a **query bug that was
> invisible at two**: every word was prefix-matched, so `in*`, `me*` and `the*` outvoted the one word in
> the sentence that carried meaning.
>
> **One assertion was retired rather than repaired, and the reasoning matters.** Steps 10 and 11 both
> asserted that *"show me every duct"* must rank the duct **filter** first. That held at two fragments
> and stopped at seven — and the honest reading is not that retrieval got worse, it is that the
> assertion asked the wrong layer. That sentence is filter-**then**-select: a composition, which is what
> a **skill** names. **Next is proof, and it needs the machine.**
>
> **What that did NOT change:** `write.enabled` is still `false` and every parked item is still unproven.
> `R1` — read the day's decisions back — **did not happen before Phase 2 began**, contrary to the
> instruction that asked for it; that was overridden by the owner, which is his to do. **It happened on
> 2026-08-29 instead, and all twenty-one were confirmed with nothing moved** — so the cost of doing it
> late was, on this occasion, nothing measurable. **`A1`, `A2`, `A3` and `A5` are done** — the whole of
> Group A except the Windows named pipe in `A4`, the probe check in `A6`, and `A8`.
>
> Before writing any code, run `python tools/check-compile.py`. It takes minutes, it now covers **all
> eight releases**, and it is no longer somebody else's job. On a fresh Linux box it needs
> `apt-get install -y dotnet-sdk-10.0` first — the .NET 8 package builds only 2020–2024.

> ## ⚠️ READ THIS FIRST — you are probably on a machine with no Revit
>
> The owner is continuing this work **from mobile**, in Claude Code. **There is no Revit there**, and
> there is no Windows, so **nothing in this repository that touches Revit can be tested.**
>
> That does not mean stop. It means **be honest about which half you are in**, and mark every piece of
> Revit-side work as untested until it has met a real Revit on the owner's own machine.
>
> **What still works away from Revit:** [§5](#5-what-you-can-and-cannot-do-without-revit).
> **What must be re-tested on return:** [§6](#6-the-return-to-the-machine-checklist) — keep it up to date.

Then read [docs/README.md](docs/README.md) for the map and
[docs/27-build-order.md](docs/27-build-order.md) for what to build.

---

## 1. Where the project stands, in one paragraph

**Phase 0 is complete.** Steps 1 to 5 of 6 are built and **proven in real Revit 2020 and 2024**: the
bridge, the thread hop onto Revit's own thread, a working MCP server inside Claude Code, *"select all
ducts"* with an audit trail, and one-chat-one-Revit binding that fails closed. The repository is
**private**.

**Step 6 — the first write — is BUILT, COMPILED, AND STILL UNPROVEN, and the gap between those words is
the whole story of this section.** It was written on a phone, on a machine with no Revit, no Windows and
no .NET SDK. **A compiler has now read every line of it** — Revit 2020, 2021, 2022, 2023 and 2024, zero
warnings — which it had not on 2026-08-27, and which cost one 2020-only defect to find out. The chat half
is tested and passing. But it has still **never loaded into Revit and has never moved anything**. Do not
read "Step 6 compiles" as "Step 6 works": compiling proves the API surface agrees, and says nothing
whatever about whether a duct moves 200 millimetres or 200 feet.

**Heron can no longer be read-only by construction, so it is read-only by default instead.** That is a
real weakening and it was made deliberately: until 2026-08-27 there was no transaction code in the
repository at all, and the guarantee needed no trust. Now there is, and the guarantee rests on
`write.enabled` defaulting to `false` in `HeronPermissions`. **Leave it false until the write path has
been through a real Revit.** [`NEEDS-CHECKING.md`](NEEDS-CHECKING.md) is how that happens.

**Four things arrived after the write path, because auditing found them missing rather than anybody
remembering them.** The **lease** ([D-22](docs/DECISIONS.md)) — Step 5 deferred it to *"Phase 1 with
writes"* and Step 6 is that write; a second chat is now refused instead of cutting the first off mid-job.
The **Failure Analysis Agent**, which never blind-retries and fails closed. The **tool registry**, so risk
is declared in a table rather than as a literal buried in the write path. And **configuration and health**,
which between them exposed two silent bugs — `write.enabled` could never have been switched on, and the
two halves disagreed about how long to wait.

**All 21 Golden Rules are now official** ([Q-19](docs/OPEN-QUESTIONS.md), accepted 2026-08-28) — including
the four Step 6 was built to obey. They were accepted *before* Step 6 is proven, deliberately: a rule that
only binds once the code passes is not a rule the code was ever held to.

**PHASE 1 IS BUILT IN FULL — all eleven items, not just Step 6's seven.** The last three were found by
auditing Phase 1 against its own list rather than against the build order, and none of them had been
flagged anywhere:

| | |
|---|---|
| **The audit says WHICH elements** | It logged `moved: 4`. A count cannot answer the question anybody asks after something goes wrong — *which* ducts — and that is the entire point of an append-only record. It now carries every moved element's `UniqueId`, the document identity and the undo entry name, under one Workflow ID |
| **The Golden Test Library** | 17 cases: the 7 things Phase 0 proved, and 10 that have never met Revit. Each records the files its proof rested on, so `python tests/test_golden.py` reports the proofs that have gone **stale**. It currently reports all seven as stale, which is exactly true |
| **The Workflow Engine** | Checkpoints and resume. Built to spec and covered by its own suite — and **nothing calls it yet**, deliberately. See the untested table in [§3](#3-what-is-proven-and-what-is-only-built) |

**Phase 1's own definition of done is not met, and cannot be met here:** *"a wrong instruction can be
reversed with one Ctrl+Z, and a failed operation leaves the model untouched."* Both are written, both are
covered by reasoning, and **neither has been witnessed.**

**PHASE 2 IS BUILT — all eight steps, 7 to 14, on 2026-08-28 and 2026-08-29.** It was unblocked by the
twenty-four decisions taken on 2026-08-28 (D-20 to D-43), which closed **every open question in the
project, 41 of 41**, and then it was built. What is on disk: eight Python modules under `brain/`, seven
fragments, ten skills, **nine** new test suites, and [`tools/check-gaps.py`](tools/check-gaps.py) — the
sweep that looks for what is missing rather than waiting to be told. The ninth suite is
[`tests/test_brain_reachable.py`](tests/test_brain_reachable.py), and it arrived last with the seam that
made any of the other eight reachable from a conversation.

**Phase 2's own definition of done is NOT met, and only two thirds of the reason is the missing Revit.**
The [build order](docs/27-build-order.md) states it as three clauses: *ten real skills work, none
hard-coded; a re-authored capability carries its own proof; and the Orchestrator resolves through
capabilities rather than agent names.* The first two need a model — all ten skills and all seven
fragments are `DRAFT`, and [D-30](docs/DECISIONS.md) promotes on a proof containing a negative case.

> ### The third clause was not blocked by anything — and is now done
>
> **RESOLVED 2026-08-29, later the same day.** [`mcp/server/heron_brain.py`](mcp/server/heron_brain.py)
> is the seam and three read-only tools stand on it — `heron_capabilities`, `heron_resolve`,
> `heron_lookup` — each asking for a **capability** and never for a fragment. `check-gaps.py` now reports
> **nothing unfinished**. What follows is the finding as it stood, kept because the *way* it was missed
> matters more than the fix, and because two limits it names are still true: **resolving is not running**
> (there is no executor, [D-28](docs/DECISIONS.md) is unbuilt), and the tools have **never been served by
> a real `FastMCP`** — that is `A8` in [`NEEDS-CHECKING.md`](NEEDS-CHECKING.md).
>
> **The brain is wired to nothing.** Found on 2026-08-29 by running `tools/check-metadata.py` and
> following what it said. The Orchestrator is **the host's job, not Heron's** — `HERON-ORC-MAIN-001` is
> listed as host-provided under [D-01](docs/DECISIONS.md) and
> [docs/02 §7](docs/02-architecture-overview.md), which is correct and deliberate. But the host reaches
> Heron **only through MCP tools**, and the seven tools in
> [`mcp/server/heron_tools.py`](mcp/server/heron_tools.py) are all `revit_*` — every one goes straight
> to the bridge. **Nothing outside `brain/` and `tests/` imports the brain at all:**
>
> ```bash
> grep -rn "heron_capability\|heron_retrieve\|heron_skill\|heron_fragment" --include=*.py . \
>   | grep -v "^./brain/\|^./tests/"      # one hit, and it is a comment
> ```
>
> So eight modules, seven fragments and ten skills are **built, tested, and unreachable from the
> conversation.** The host cannot resolve a request through a capability because it has no tool that
> asks. That is Phase 2's third clause, and it needs **no Revit, no Windows and no network** — it is the
> one genuinely buildable thing left in this repository.
>
> **`check-gaps.py` did not catch this**, which is worth more than the finding. It checked that each
> step's module and test were on disk, which they were, and never asked whether anything *calls* them.
> A check has since been added so the tool stops reporting a green it has not earned — but the lesson
> generalises: **a checker built from a build order can only ever ask whether the build order was
> followed.**

| | |
|---|---|
| **D-23 / D-24** | The knowledge store is **SQLite, one file per scope**, decided on the per-user no-admin install constraint rather than on retrieval quality. Embeddings are **local** — now on the re-indexing argument, since D-26 narrowed the confidentiality one |
| **D-25** | The existing libraries are **studied and re-authored, never imported.** Ajmal's, and it deleted a planned import pipeline rather than choosing between libraries |
| **D-26** | **The model file is never uploaded.** Refined three times in one day, each time looser: the line is the `.rvt`/`.rfa`, not the information. Project names, counts, sizes and reasoning travel like any conversation |
| **D-27** | **There are no personas.** One voice; the *shape* of the answer follows the shape of the request. Settled by reading two assistants already doing this job daily, both of which switch nothing |
| **D-28** | Generated code is **Roslyn C#, in process** — not pyRevit, whose Routes server would put HTTP inside an add-in that has no network code at all. Closes Q-37 as not applicable |
| **D-29** | A fragment is a **composable piece**, not a whole answer: filter + action, with a recipe as a named third kind |
| **D-30** | A fragment is promoted by **one recorded proof with a negative case**, not by a count of runs — because the defect that matters is one that *succeeds while doing nothing*, and that passes a thousand runs |
| **D-31** | Product / data / derived were **already separated** since Step 1, and the updater half was verified rather than assumed |

**And the rest of the day closed every remaining question.** `D-32` to `D-43`, in the same conversation:

| | |
|---|---|
| **D-32** | **v1 must be able to change the model**, and reading is what gets used first. Recorded as read-only and **reversed within the hour** when the same question was asked again |
| **D-33** | **Heron never assumes an input. It asks — and it asks once.** There is no confidence threshold, because a number invented today is a number tuned tomorrow |
| **D-34** | Heron's wording is **English**; understanding the user is the host's job, not Heron's |
| **D-35** | A shared fragment may carry code, and an **unapproved one is refused, not warned about** — a warning hands the decision to whoever is least able to judge it |
| **D-36** | **No warranty**, and it was already in place twice. `DISCLAIMER.md` makes four promises that are **still unproven** |
| **D-37** | The name is **Heron AI** — *chosen, not cleared*. No trademark search has been done |
| **D-38** | **GitHub now**, App Store possible, nothing built for it. Autodesk's requirements have **not been read**, and were not written from memory |
| **D-39** | Shadow mode is approved on an **analysed disagreement**, never a count of agreements |
| **D-40** | The dependency graph is SQLite, and **an edge is derived before it is stored** |
| **D-41** | **Single-user now**; company knowledge is a git repo and the admin is the reviewer — D-35 at a smaller radius |
| **D-42** | The **public install command is deferred**; `setup.ps1` is what is proven |
| **D-43** | **The Constitution is accepted — all 30 Articles, binding** |

**`R1` is done — 2026-08-29, and it covered D-23 to D-43 rather than the five it asked for.** All
twenty-one were read back and confirmed; the three carrying real consequence (D-33's boundary, D-26,
D-32) were put to him one at a time and none moved. **`R1b` still needs a screen**, since
[D-14](docs/DECISIONS.md) stays *Proposed* until he has seen the trust model working. They are Accepted and are being built on; the review confirms each still says what he meant
and fills in detail left out. See the block at the top of [DECISIONS.md](docs/DECISIONS.md).

**Two were reversed within hours of being recorded**, D-26 three times and D-32 once. Neither was a
mistake — each was a first answer sharpened once its consequence was visible, which is the whole argument
for that read-back.

---

## 2. What exists

```
revit/          Part 1 — loads into Revit.exe. C#. Changing it needs a Revit RESTART
  Heron.Revit.Addin/   ribbon toggle, icons, the ExternalEvent dispatcher, the operations
  Heron.Bridge/        named-pipe server, per-session token. No Revit reference
mcp/            Part 2 — the bridge to the AI host. Python, outside Revit
  client/              discovery, retries, doctor. Dependency-free on purpose
  server/              the MCP server and the session binding
brain/          Part 3 — knowledge. BUILT IN PHASE 2, and all of it runs without Revit
  heron_fragment.py    the fragment's shape on disk, and the validator that enforces it
  heron_scope.py       one SQLite file per scope; a cross-scope query cannot be written
  heron_search.py      exact words — identity, cache, FTS5
  heron_embed.py       nearness, local and offline. Read its caveats before trusting it
  heron_retrieve.py    the two fused, behind a hard Revit-version filter
  heron_capability.py  ask for what you want done, never for who does it
  heron_graph.py       what breaks if this changes. Every edge but one is derived
  heron_skill.py       a skill names capabilities, never fragments
  fragments/           7, all DRAFT — none has met a model
  skills/              10, all DRAFT — same
platform/       Part 4 — the Kernel
  Heron.Core/          HeronPaths, HeronConfig, HeronIdentity, HeronAudit
tests/          17 suites. 16 run anywhere; test_bridge_roundtrip needs its host built first
tools/          scripts that keep the repo honest, plus setup and deploy
docs/           41 documents — specification, decisions, questions, roadmap
.claude/        skills and agents that ship with the repo
```

Each part has a `README.md` saying what belongs there and **when to fix things there**.

---

## 3. What is proven, and what is only built

**This distinction matters more than anything else in this document**, and it matters more than ever
now that the next stretch of work happens where Revit cannot be reached.

### Proven against a real Revit

| | |
|---|---|
| The bridge answers `ping` | Revit 2020 and 2024 |
| One button connects **and** disconnects; the icon shows which | Toggled repeatedly, every transition logged |
| Per-session token, minted per connect | 2024's token on the 2020 pipe → `unauthorized` |
| Newest connection wins — **the pipe only, since Step 6** | *"A newer connection took the session"*, older one dropped. Still true of the transport; a lease now decides who may actually send anything ([D-22](docs/DECISIONS.md)). **The lease itself is unproven** |
| Two Revits at once, separate pipes | `heron.2024.*` and `heron.2020.*` together |
| **The thread hop** | `5,844 elements` from 2024, `3,167` from 2020 |
| **"Revit is busy" instead of a hang** | Dialog open → clean refusal after 10s, recovers by itself |
| **The MCP server inside Claude Code** | Answered from both live Revits, no command run |
| **"Select all ducts"** | 4 found and **highlighted on screen**, confirmed by eye |
| The audit trail | Every request recorded — op, outcome, document, timing |
| **Fails closed when the chosen Revit closes** | 2024 closed with 2020 open → Heron **stopped** |
| `setup.ps1` end to end | Detects, builds, deploys all three in one run |
| Installer skips only the **open** release | 2020 and 2024 open → both skipped by name, 2027 installed |
| Each build carries the right runtime | `net472` / `net48` / `net10.0-windows`, read from the deployed DLLs |

### Built, never met a real Revit

| | |
|---|---|
| **Revit 2027** | Builds and installs. Never launched — the owner says his 2027 does not work |
| **Naming the session in a selection answer** | Fixed after both models turned out to be called `Project1`. Needs a Claude restart to go live, then one look |
| **Anything committed from mobile after 2026-08-28** | **Assume untested** — but no longer assume unread: `python tools/check-compile.py` runs on mobile too. Add it to [§6](#6-the-return-to-the-machine-checklist) |
| **The whole of Step 6** | **Compiles** on 2020–2027 — all eight releases, 0 warnings (2026-08-28). Never loaded into Revit, and has never moved anything |
| `RevitWrite.cs` — preview, re-count, TransactionGroup, rollback | The one file that can change a model. It compiles; every **behavioural** claim in it is still unverified. Its `DocumentKey` was the one thing the compiler caught — `Document.CreationGUID` does not exist in Revit 2020 |
| `HeronUnits`, `HeronPermissions`, `HeronStop` | Kernel C#. Plain arithmetic and flags. Compiles; `HeronUnits` is exactly what `D3` exists to measure |
| The Emergency Stop ribbon button | New `PushButtonData` in a file whose ribbon currently works. **If Heron will not load after this, look here first** |
| **The lease** (`HeronLease`) | Refuses a second chat instead of cutting the first off. **Now exercised end to end against the compiled bridge** (2026-08-28) — claim, renew, refuse a second chat, never cut off the first, exempt `ping`/`info`. Not against Revit, and not over a Windows pipe: group H is what remains |
| **The audit's element list** | Every moved element's `UniqueId` now goes into the log. Never seen against a real model, and a move of several hundred elements writes a correspondingly long line |
| **The Workflow Engine** (`heron_workflow.py`) | Built to spec and covered by its own suite, but **nothing calls it yet** — and that is deliberate, not an oversight. Phase 1's only multi-stage flow is preview→apply, which the add-in already sequences better because it is the side that can re-count against the live model. Its real customer is Phase 2's 18-stage pipeline. Proven by its tests, unproven in use |
| **Bridge protocol 2** | A request now carries a `client` id. An add-in still on protocol 1 will refuse to talk — which is correct, and means the add-in MUST be rebuilt and redeployed. The handshake itself is proven: `info` reports `protocolVersion 2` |
| `revit_preview_move`, `revit_apply_move`, `revit_use_this_model` | The three new MCP tools. Never seen by a running host |

> The chat side of Step 6 **is** tested and passing — distance parsing, document pinning, single-use
> approval: `python tests/test_write_safety.py`. That test says nothing whatsoever about the
> transaction, the rollback or the move, and says so itself.

### Built in Phase 2, and none of it has met a model

| | |
|---|---|
| **The eight `brain/` modules** | Each has its own suite and each passes — the fragment store, the scope store, exact-word search, nearness, fusion, the capability registry, the graph, and skills. What they are proven to do is **behave as specified against fixtures**. Not one of them has been handed a real Revit's answer |
| **86 fragments, all `DRAFT`** | `DRAFT` is not a shortcut — it is [D-30](docs/DECISIONS.md) being obeyed. A fragment is promoted by one recorded proof **containing a negative case**, and a negative case needs a model. They stay DRAFT until then, and they are the reason `check-gaps` lists **eighty-six** items under *needs a real Revit*. **Since 2026-08-29 all of them at least COMPILE** — on all eight releases, 2020 to 2027 ([`tools/check-fragments-compile.py`](tools/check-fragments-compile.py)). That is not behaviour, but it does mean none of them will fail at the PC for a reason a compiler could have found |
| **10 skills, all `DRAFT`** | Each names **capabilities and never fragments**, and each carries the words Ajmal actually says rather than the words the technique is named after. Whether any of them does what it says is unknown |
| **The trained embedding backend** | **The highest-value item that needs no Revit, and 2026-08-30 sharpened what it buys.** [`brain/retrieval-history.md`](brain/retrieval-history.md) tracks one query across eight library sizes (7 → 32) *and* now measures a second way: every fragment's own declared words asked back to the search — 169 sentences, **words 92% first and 100% in the top three, nearness 60% and 82%**. That corrects the older headline in this file's own history: the nearness route has **not** collapsed in general. It handles **vocabulary overlap** and fails at **disambiguation**, which is why the tracked query — a sentence several fragments fairly claim — sits mid-library while a sentence naming one fragment comes back first. So `A7` should be expected to change the **contested** lookups, not every lookup. Never run — `huggingface.co` is refused by this container, and by a second one on 2026-08-31, where the package installed cleanly from PyPI and only the **weights** download was refused (`ProxyError: 403`). The backend that *is* running is character n-grams, which measurably does not do synonyms (`diffuser`/`grille` scored −0.136). Needs no Revit and no Windows |
| **The three brain MCP tools** | `heron_capabilities`, `heron_resolve`, `heron_lookup` — and the seam under them, [`mcp/server/heron_brain.py`](mcp/server/heron_brain.py). **A real MCP SDK has now served them** (2026-08-31): all ten tools registered with their descriptions and argument schemas, and all three answering through the SDK's own dispatch with both refusals surviving the round trip — [`tests/test_mcp_serves.py`](tests/test_mcp_serves.py). That is no longer a text read. **It is still not a host**: nothing here shows Claude Code connecting over stdio, rendering a docstring or choosing a tool from it, and that is what is left of `A8`. **Doing it found that the server would not have started at all** on a machine installing today — see the eighth session below |

### Does not exist at all

| | |
|---|---|
| ~~Any way for the host to reach the brain~~ | **BUILT 2026-08-29.** Three read-only tools on one seam. Moved to the table above, which is where an untested thing belongs |
| **Any way to RUN a fragment** | **The gap that wiring the brain up revealed rather than closed.** A fragment carries C# under `impl/`, the bridge speaks a fixed set of operations, and **none of them compiles or executes one** — [D-28](docs/DECISIONS.md) chose Roslyn in-process and it is not built. So a request now resolves all the way to *this capability, provided by that fragment*, and then stops. Every brain tool says so on every answer, because a host that inferred otherwise would build a plan that fails at its last step |
| ~~Seven capabilities the skills ask for~~ | **WRITTEN 2026-08-29**, taking the library to 14 that day and to **32** by 2026-08-30. **All ten skills have every capability provided** — `python brain/heron_skill.py` shows no gaps. Each one compiles on all eight releases and each carries proof cases with a negative case, **and not one has met a model**: they are `DRAFT`, which is what makes them the seven newest rows of Revit-checking debt rather than seven finished things |

> Nothing here is known-broken. Several things are **untested**, which is different and more honest.

---

## 4. The things that will bite you

Each of these cost real time. They are in the order they were learned.

1. **The Revit API can only be called from Revit's own thread, inside an API context.** An MCP server is
   a separate process and cannot call it at all. Everything marshals through one `ExternalEvent`.
   [docs/03 §4](docs/03-heron-revit.md)

2. **A named pipe needs `CreateNewInstance`, not just `ReadWrite`.** Without it only the *first*
   listener can be created and every retry fails with "access denied".

3. **The discovery file must never carry the document name.** Storing it produced the stale-name trap in
   the owner's earlier work. [docs/25 §2a](docs/25-multi-session-and-binding.md)

4. **"No reply" does not mean "dead".** Test the *process*, not the reply — and check it is still the
   right *program*, because Windows reuses process ids.

5. **An assumption is not a choice.** If one Revit was open, Heron *assumed* it. If the user answered,
   they *chose*. Conflate the two and a second Revit opening mid-chat silently sends everything to the
   first one. [docs/25](docs/25-multi-session-and-binding.md), and `tests/test_session_binding.py`.

6. **Naming the document is not always enough.** Two sessions can both have `Project1` open — it
   happened. Name the session too.

7. **Two waits, not one.** *"Did Revit pick it up?"* and *"having started, did it finish?"* are
   different questions. Collapsing them tells the user Revit is busy while it is actually working.

8. **Retry only what never left the machine.** A failed connect can always be retried. A **lost answer**
   cannot — Step 6 writes must pass `idempotent=False` or a move happens twice.

9. **`Platform=x64` puts build output in `bin/x64/$Configuration`.** Scripts should *find* the
   assembly, not assume the path shape.

10. **An empty array passed to a PowerShell parameter arrives as `$null`**, and `@($null)` has one
    element. This turned "no Revit open" into "one Revit open" and blocked every install.

11. **A contract name becomes a C# variable, so it has to be able to be one.** `FRG-QA-001` declared a
    value called `checked` — a reserved C# keyword — and its own code read `if (checked == 0)`. It could
    never have compiled, and it passed every check for as long as nothing compiled a fragment.
    `heron_fragment.py` now refuses reserved words and non-identifiers outright.

12. **A `#line` path must be absolute.** The fragment compile harness pointed errors back at each
    fragment's own file with a repo-relative `#line`. That built fine on `net472` and `net48` and failed
    on every `net8`/`net10` release with `CS1504 source file could not be opened` — the newer compiler
    *resolves* that path rather than only printing it, and resolves it against the project. Found by
    running all eight releases instead of the one that happened to work.

13. **Revit's move call returns normally and moves nothing, for a group member.** No exception, no
    return value, no warning. Counting *"it did not throw"* as *"it moved"* reports **"Moved 5,
    skipped 0"** for five air terminals that have not shifted a millimetre — the "succeeded and did
    nothing" failure [D-30](docs/DECISIONS.md) exists to catch, in the one place Heron can actually
    change a model. Heron skipped **pinned** elements, which is one half of the case; a group member
    is not pinned and went straight through. `RevitWrite` now probes each position before and after
    and reports four outcomes — moved, partly, blocked, unverified — instead of the count it asked
    for. **Found by studying the owner's earlier library rather than by reading Heron's own code**,
    which had been read several times. `E10` is the check.

---

## 5. What you can and cannot do without Revit

### Works anywhere, including mobile

```bash
python tools/check-docs.py             # links, Golden Rule / decision / question references
python tools/check-metadata.py         # the standard, registry vs code, and version agreement
python tools/check-structure.py        # layout, layering, paths, and the PowerShell ANSI trap
python tests/test_session_binding.py   # one chat one Revit, all four cases — pure Python
python tests/test_write_safety.py      # Step 6's CHAT half — distances, pinning, approval
python tests/test_failure_analysis.py  # never blind-retries, and fails closed
python tests/test_tool_registry.py     # both languages agree on what may write
python tests/test_config_and_health.py # settings agree; health means something
python tests/test_workflow.py          # stages resume, stale inputs re-run, no blind retry
python tests/test_golden.py            # which proofs still stand against the current code

# Phase 2, added 2026-08-28 / 29. All eight run anywhere, none needs Revit
python tests/test_fragment_store.py    # identity survives a rename; a proof with no negative case is refused
python tests/test_scope_store.py       # one file per scope, and a cross-scope query cannot be written
python tests/test_search.py            # exact words - identity, cache, FTS5
python tests/test_embed.py             # nearness, and the measured proof that it is NOT meaning
python tests/test_retrieve.py          # the two fused, behind a hard Revit-version filter
python tests/test_capability.py        # a provider is swapped and the call site does not change
python tests/test_graph.py             # the deriver shown catching a break before it was believed
python tests/test_skills.py            # a skill names capabilities, never fragments

# The seam, added 2026-08-29. Also needs no Revit
python tests/test_brain_reachable.py   # the host resolves through a capability, not a fragment id

# Served by a REAL SDK, added 2026-08-31. Needs `pip install --user mcp`,
# no Revit and no Windows. With no SDK it exits 3 and check-gaps reads that
# as WAITING - a skip reported as `ok` is a green nobody earned
python tests/test_mcp_serves.py        # every tool served, with its description and its arguments

# The fragments, compiled for the first time - 2026-08-29. Needs the .NET SDK,
# no Revit and no Windows. ~10 minutes for all eight releases
python tools/check-fragments-compile.py   # every fragment, every release it claims

# Did a new fragment make an OLD one unfindable? - added 2026-08-30.
# Run it after adding any fragment. It never fails a build; it reports judgements
python tools/check-routing.py             # every fragment, asked its own words back
```

> **Set `HERON_KNOWLEDGE` before anything that touches `brain/`.** On Windows the stores go under
> `%APPDATA%`; anywhere else there is no such folder and the store has nowhere to live, so the tools stop
> with a named error rather than inventing a location. `export HERON_KNOWLEDGE=~/.heron` is enough. The
> stores are **derived** — deleting them is always a safe recovery, and `python brain/heron_scope.py
> --rebuild` puts them back.

**18 suites; 16 pass here with nothing installed at all, 514 individual `ok`/`PASS` lines** — derived,
not typed: `for f in tests/test_*.py; do python3 "$f"; done | grep -cE '^\s*(ok|PASS)\b'`. The other two
each want one thing first, and **neither is a Revit**: `test_bridge_roundtrip.py` needs its host built
(below), and `test_mcp_serves.py` needs `pip install --user mcp`, which takes it to **531**.

**And one command that runs all of the above and then looks for what is missing:**

```bash
python tools/check-gaps.py             # UNFINISHED vs WAITING. The exit code follows only the first
```

It is the first thing to run in a new session and the last thing to run before saying anything is
finished. **It found real defects on its first runs, including two in itself** — a scanner that scans
itself reported its own regex as two undeclared agents — and it caught a second invented agent id in as
many steps. Growing the library from 2 fragments to 7 exposed a query bug that had been **invisible at
two**: every word was prefix-matched, so `in*`, `me*` and `the*` outvoted the one word in the sentence
that carried meaning.

**And two more things that were believed to need Windows, and do not** (2026-08-28,
[docs/30](docs/30-compiling-away-from-windows.md)):

```bash
python tools/check-compile.py                  # Revit 2020-2027, all four projects, 0 warnings

dotnet build tests/Heron.Bridge.TestHost -p:RevitVersion=2024 -p:HeronTfm=net8.0
python tests/test_bridge_roundtrip.py          # 32 checks, including the whole lease
```

The first needs the .NET SDK, which Linux distributions package — Microsoft's CDN is often blocked from a
container and that is the wall earlier sessions hit. The second runs because `Heron.Bridge` has no Revit
reference, so it compiles for `net8.0` and .NET implements named pipes on Unix as a socket.

**What that still does not cover is the Windows named pipe itself** — its naming, its security
descriptor, and the `CreateNewInstance` flag in note 2 of [§4](#4-the-things-that-will-bite-you). `A4`
in the register means the Windows run, and a POSIX pass is a strong signal ahead of it rather than a
substitute for it.

**A third thing, added 2026-08-28** — it reads what the releases actually ship:

```bash
python tools/check-api-surface.py              # every Revit member Heron calls, on 2020 THROUGH 2027
```

It was built while 2025–2027 were being skipped by the compile gate, to leave them with something rather
than nothing. **That skip is gone** — see below — so this is no longer their only cover, and it is still
worth running: it reads the **shipped** assemblies for all eight releases, where a compile reads the
NuGet reference packages for the one release it is building. It looks up the **compiled** add-in's
reference tables — exactly what the code calls. **All 103 exist on every release from 2020 to 2027.** It
matches by name, so a changed signature would still only show up in a real compile; it supplements the
compile gate and says so on every run. **It was validated before its clean result was believed**, by
putting the `CreationGUID` defect back and watching it fail.

**And the skip itself turned out to be one more environment-specific claim, found the same day.** The
newest three releases were reported SKIPPED off Windows as *needing the Windows Desktop SDK*. Those
targets are a property of **the installed SDK package**, not of the operating system: Ubuntu's
`dotnet-sdk-10.0` ships them and its `dotnet-sdk-8.0` does not, and `check-compile.py` was passing
`-p:EnableWindowsTargeting=true` — the flag whose whole purpose is the non-Windows case — **only on
Windows**. With the .NET 10 SDK installed, **all eight releases compile here**, add-in included, 0
warnings. [docs/30 §2a](docs/30-compiling-away-from-windows.md) has the account and the two-directional
validation that was done before the eight greens were believed.

- **All documentation, decisions, specifications and open questions.**
- **The session binding**, in full. Its test fakes the world and exercises the real logic.
- **Reading and reasoning about the C#.** Just not running it.
- **Compiling it, on every supported release** — 2020 to 2027, since the .NET 10 SDK arrived on this
  side of the fence. Two things this repository "could not do here" have now turned out to be things
  nobody had attempted; assume the third is out there and go looking before writing the sentence.

### Cannot be done without Revit — and cannot be faked

- Anything that loads into `Revit.exe`: the ribbon, the icons, the dispatcher, the operations.
- `tools/setup.ps1` and `tools/deploy-addin.ps1` — they need Revit installed, and Windows.
- `test_bridge_roundtrip.py` needs **Windows named pipes**, so it will not run on a phone either.
- Every claim in [§3](#3-what-is-proven-and-what-is-only-built) marked *proven against a real Revit*.

> **If you change C# from mobile, you cannot know it works — but you can now know it BUILDS, so build
> it.** `python tools/check-compile.py` is minutes, needs nothing installed but the .NET SDK, and it
> catches the entire "worked in 2020, broke in 2024" class. Compiling is not testing: say which one you
> did in the commit message, and add the rest to the checklist below. A commit that reads as though it
> were tested is worse than one that admits it was not.

---

## 6. The return-to-the-machine checklist

**The list lives in [`NEEDS-CHECKING.md`](NEEDS-CHECKING.md), not here.**

Every item is unproven, every item has an ID (`A1`, `D3`) so it can be named in a message without
being described again, and they are in dependency order — nothing in group D can be attempted before
group A passes.

It is kept as ONE register rather than a copy in each file, because two lists of the same thing drift
and this repository has been bitten by that more than once. Add to it every time something is built
away from Revit; delete an item only when it has actually passed.

**The single most important line in it is `D3`:** move the ducts 200 mm, then *measure one*. That is
what catches a unit error, and a unit error is the failure that looks fine until somebody measures it
months later.

The one-command path back to a working machine:

```powershell
powershell -ExecutionPolicy Bypass -File tools\setup.ps1
```

It checks Python first, detects installed Revit versions, refuses only the ones that are **open**, and
deploys per-user with no administrator rights. Then start Revit → **Heron AI** tab → **Heron** (click to
connect, again to disconnect), and:

```bash
python mcp/client/heron_bridge_client.py ping
python mcp/client/heron_bridge_client.py count
```

Anything wrong → `python mcp/client/heron_bridge_client.py doctor` prints everything needed to diagnose it.

**For unattended testing**, set `bridge.autoConnect = true` in `%APPDATA%\Heron\config\heron.config`
and the bridge starts without a button press. Default is `false` on purpose — a Revit that was never
connected being invisible is a safety property. **Put it back afterwards.**

---

## 7. The decisions you must not quietly undo

> ### ⛔ Heron is the only codebase you write to
>
> **The owner's instruction, 2026-08-28.** There are two other repositories on this machine — an existing
> Revit add-in and a knowledge package. Both are **reference only**:
>
> - **Never commit to them, never open a pull request on them, never update or upgrade them.** Every
>   change goes to Heron.
>
>   *One exception exists and is closed:* a commit and draft PR were made to the knowledge package on
>   2026-08-28 before this rule was given. They were withdrawn, and then the owner allowed that one
>   through — *"do it for AJ AI, this time only, next time no need"* — so
>   [AJ-AI-Brain#47](https://github.com/Ajmalpshaik/AJ-AI-Brain/pull/47) is open on purpose. **It is not a
>   precedent.** If you find yourself about to add a second one, the answer is no.
> - **Read them freely** — they solve overlapping problems and their scars are worth more than their
>   features. [PROPOSALS Part E](docs/PROPOSALS.md) is what that study produced.
> - **Never copy code or text out of them.** Understand the mechanism, then write it for Heron, in
>   Heron's shape, with Heron's reasoning. His words: *"study and use and make it part of our heron,
>   blindly copy paste dont do it."*
> - **Do not cite them as Heron's authority.** He stops using both once Heron is finished, so a note
>   whose argument is *"go read that other repository"* becomes worthless on that day. State the
>   reasoning here, in full, so it stands on its own.

Twenty-two are in [docs/DECISIONS.md](docs/DECISIONS.md). These are the load-bearing ones:

| | |
|---|---|
| **D-01** | Heron runs as a **Claude Code plugin** |
| **D-02** | **Named pipes**, per-PID. Local-only by construction — the add-in has *no network code at all* |
| **D-05** | **Revit 2020 → latest.** Never extrapolate the runtime table forward; an unlisted release is a build error |
| **D-06** | **C# for Revit, Python for everything outside it.** Settled again on evidence in [Q-39](docs/OPEN-QUESTIONS.md) |
| **D-09** | One `ExternalEvent`, one queue, one handler |
| **D-15** | **Where the field notes disagree with a specification, the field notes win.** Observed beats designed |
| **D-17** | Runtime state is machine-local; user data roams; the audit log stays with the data because it is evidence |
| **D-18** | The Transaction Agent belongs to **Step 6**, not Step 2 — a write path built before its rails ships without them |
| **D-19** | **Writing is off by default** until the write path has met a real Revit. Read-only stopped being structural the moment Step 6 existed; this is what replaced it |
| **D-20** | Millimetres to feet is **arithmetic, not `UnitUtils`** — exact, and nothing for Autodesk to move under it across 2020–2027 |
| **D-21** | Failure analysis is a **table, not a model call** — Heron's failures are its own bounded set of codes |
| **D-22** | A second chat is **refused, not allowed to take over**. *"Newest connection wins"* now describes the pipe only |
| **D-25** | The existing libraries are **studied and re-authored, never imported.** Ajmal's. Nothing is copied; a capability is written from scratch here and starts unproven, whatever status it held where it was read |
| **D-26** | **The model file is never uploaded** — the `.rvt`/`.rfa`, not the information. Refined three times in one day, each looser; read the final rule, not the earlier framings that same-day commits still quote |
| **D-27** | **There are no personas.** One voice; the answer's *shape* follows the request's shape |
| **D-28** | Generated code is **Roslyn C#, in process.** No Python runtime, and **no HTTP server enters the add-in** — the add-in having zero network code is a structural guarantee, not a setting |
| **D-30** | A fragment is promoted by **one proof containing a negative case**, never by a count of successful runs |
| **D-32** | **v1 must be able to change the model.** Reading is what gets used first, but a Heron that cannot change anything is a report tool, not the product |
| **D-33** | **Never assume an input — ask, and ask once.** No confidence threshold: a number invented today is tuned tomorrow, and the first tune to reduce interruptions starts it guessing |
| **D-35** | An unapproved shared fragment is **refused, not warned about**. A warning hands the decision to whoever is least able to judge it |
| **D-39** | Shadow mode is approved on an **analysed disagreement**, never a count of agreements. Agreement is weak evidence; a thing that does nothing agrees with everything |
| **D-43** | **The Constitution is binding** — all 30 Articles. Its own Amendment clause applies: never weakened silently, and never by an agent |

**Golden Rules** — **21, all official** — are in [docs/14](docs/14-golden-rules.md). 16–21 cover undo,
preview-before-modify, sandboxing, permission escalation, document pinning and stale reads, and were
**accepted on 2026-08-28** ([Q-19](docs/OPEN-QUESTIONS.md)) *while Step 6 remained unproven*. That order
was deliberate: a rule that only binds once the code passes is not a rule the code was ever held to.
**They are exactly what Step 6 builds.**

---

## 8. What is waiting on the owner

**Every question is answered — 41 of 41 — and nothing blocks any phase.** What is left is **three review
item at the PC** (`R1b` — `R1` and `R2` were closed on 2026-08-29) **and two choices Phase 2 created by
finishing, both of which have since been made.** The copyright
line was the last outstanding confirmation and it was given on 2026-08-28.

| | |
|---|---|
| ~~`R1` — read the day's decisions back~~ | **DONE 2026-08-29.** All twenty-one read back, all confirmed. It happened in conversation rather than at the PC, and **after** Phase 2 was built rather than before — both recorded rather than smoothed over. What that cost turned out to be **nothing measurable**: the two that had been reversed within hours did not move a fourth time, and no missing detail surfaced. `R2` went with it — D-26 and D-32 were the two put to him individually |
| **`R1b` — show him the trust model working** | [D-14](docs/DECISIONS.md) stays **Proposed**. He agreed the direction and said *"show me it working at the PC first."* Use the framing that landed: a family has **a maker** and **an approval status**, and nobody would put those on one dropdown. Phase 2 may be designed against the two axes meanwhile; it may not be called settled |
| ~~Copyright~~ | **CONFIRMED 2026-08-28 — Ajmal PS is correct.** Checked consistent in all four places it appears: the Apache appendix in `LICENSE`, `NOTICE`, `<Company>` in `Directory.Build.props`, and `README.md`. The Apache appendix is filled in rather than left as the `[name of copyright owner]` placeholder, which is the one that is usually missed |

**And two choices that did not exist until Phase 2 was built.** Neither is a question the code is stuck
on — both are the owner's to make, and both were deliberately left rather than decided quietly:

| | |
|---|---|
| ~~Write the seven missing capabilities, or wait?~~ | **DECIDED AND DONE 2026-08-29 — he asked for the no-PC work to be finished.** All seven are written and compile on all eight releases. The argument that settled it: **not writing them was itself outstanding non-Revit work.** Written, what remains is their PROOF, and proof is Revit-checking — which is exactly the only thing his standing instruction wants left. **One question came out of the work and is still open**: whether a layout that Revit refuses part of should place the rest or roll back entirely (Golden Rule 16 says roll back; a modeller may well want the 37). See `place-family-instances` |
| ~~Wire the brain to the host now, or after the Revit checks?~~ | **DECIDED AND DONE 2026-08-29 — he chose to wire it now.** Three read-only tools on one seam; `check-gaps.py` reports nothing unfinished. It cost the register one new row (`A8`, two minutes at the PC) and moved no other row, so his standing instruction that *only Revit-checking should remain outstanding* holds: what remains is 47 Revit rows, 3 Windows, 1 network, 3 conversations |

**Two things were answered by NOT answering them, and both are publication tasks rather than gaps:**
[Q-38](docs/OPEN-QUESTIONS.md) — the public install command — and the Autodesk App Store requirements in
[D-38](docs/DECISIONS.md). Both need **current documentation read at the time**, and writing either from
memory is the failure this repository has already had twice. `tools\setup.ps1` is the proven route
meanwhile.

---

## 9. Next — read the decisions back, then prove what is built

**Step 6 is built. Phase 2 is built. Do not build either again.** All seven items the build order asks
of Step 6 are in the repository, and so is everything an audit turned up afterwards: the lease, the
Failure Analysis Agent, the tool registry, the configuration and health agents. Steps 7 to 14 are all on
disk with a test each. 59 agent ids are claimed by code and every one exists in the registry.

**`python tools/check-gaps.py` is what says this, rather than this paragraph.** Run it first. It now
reports **nothing** unfinished — the brain was reachable from nothing on the morning of 2026-08-29 and
was wired up the same day — and everything else it lists is **waiting on a machine or on a
conversation** —
which is not the same as being finished, and the tool is careful to say so in its own closing line:
*"waiting is not failing — but a waiting item is still UNPROVEN, and no number of days spent waiting
makes `D3` any more true."*

**`R1` is no longer the first thing at the PC — it is done** (2026-08-29, all twenty-one confirmed).
What remains of the review is `R1b`: show him the trust model
working, because [D-14](docs/DECISIONS.md) is still *Proposed*. His instruction — *"now we just recorded,
but we will do it one more time"*.

**It was meant to happen before Phase 2 was built, and it did not.** The owner overrode it — his to do —
and Phase 2 was built on those decisions instead. So the read-back is now a review of code that already
exists, which is **weaker than it was meant to be**, because a decision reviewed after the code exists
gets defended rather than examined. That is a reason to do it early, not a reason to drop it.

**The evidence for that pass is two reversals on the day itself.** D-26 moved three times, each looser.
D-32 was recorded as *"v1 is read-only"* and reversed to *"it must change things too"* when the same
question was asked again an hour later. Neither was a mistake — each was a first answer sharpened once its
consequence became visible, which is exactly what a read-back is for.

**Then the register, and a compiler is no longer what is missing.** `A1`, `A2` and `A3` are done — see
[docs/30](docs/30-compiling-away-from-windows.md) for how, in one command:

```bash
python tools/check-compile.py     # 2020-2027, all four projects, no Windows and no Revit needed
```

**It found the thing it was built to find, on its first run.** `RevitWrite.DocumentKey()` used
`Document.CreationGUID`, which **does not exist in Revit 2020** — it compiled clean on 2024 and failed on
2020. That property was named in the register as a *likely* problem spot, by reading; reading had already
passed it twice. The fix is not a `#if`: the Project Information element's `UniqueId` is created with the
document, survives save, rename and move, and exists on every release from 2020 to 2027.

**And running the bridge found a second one, in the test rather than the code.**
`test_bridge_roundtrip.py` asserted that nothing held the lease at a point where an earlier unknown-op
probe had already claimed it — the bridge was right and the check was false. It had been written from
reading, in a file that could not run on the machine it was written on. That file now runs on both
platforms, one shim, one set of assertions, and passes 32 checks including the whole lease.

**What no compiler will ever do is tell you a duct moved the right distance.** `D3` is still the line
that matters most in this repository.

### The two things left that need Windows but NOT Revit

```
A4   python tests/test_bridge_roundtrip.py    the WINDOWS named pipe itself
A6   python tools/check-compile.py 2025       that the new SDK probe reads Windows correctly
```

Everything in `A4` except the Windows pipe is already passing — its naming, its security descriptor and
the `CreateNewInstance` flag are what remain, and no amount of Linux gets at them.

**`A5` was here and is done.** All eight releases compile off Windows with the .NET 10 SDK; the row that
said otherwise had assumed an operating system where the answer was a package.

**`A6` is `A5`'s own bill.** Deciding what to skip is now done by looking for the WindowsDesktop targets
under each installed SDK rather than by asking what operating system this is — and that probe has only
ever run on Linux. If it misreads Windows it would skip the three releases **on the one machine where
they already built**, so it is worth thirty seconds at the PC. Writing a check for a machine you cannot
run it on is how this file gets its rows.

### Then the rest of the register, in order

47 items need a real Revit. They are in dependency order and each says what PASS actually looks like.
The three that matter most:

| | |
|---|---|
| **C3** | With `write.enabled` still false, a move must be **refused**, naming the setting. Prove the gate before testing the write, or a passing move proves nothing |
| **D3** | Move the ducts 200 mm, then **MEASURE ONE.** The single most important line in the register — a unit error is the failure that looks fine until somebody measures it months later |
| **D5** | **One** Ctrl+Z puts it all back, as a single undo entry. Two means Golden Rule 16 is broken |

### Record what you prove, as you prove it

`tests/golden/cases.py` is the permanent record — 17 cases, each with what to do and what PASS looks
like. When you prove one, set its `status` to `PROVEN`, the date, the Revit versions, and stamp its
`fingerprint` (run `python tests/test_golden.py --stamp` to see the value; it prints and writes nothing,
because a fingerprint is a claim that a person watched it work).

That is what makes the next regression findable. All seven Phase 0 proofs currently read **STALE** —
proven against a build that no longer exists — and that is the library doing its job, not a fault.

### Three things that are not in the register

**1. The add-in must be rebuilt and redeployed.** The bridge protocol is now **2** — a request carries a
chat id, and an add-in still on protocol 1 will refuse to talk. That refusal is correct behaviour, but if
an old DLL is still deployed it will look like Heron has stopped working.

**2. Seven capabilities are wanted and unprovided**, listed at the top of this file. `check-gaps` reports
them, the register does not, and that is on purpose: a register row is a **test somebody must run**, and
these are **code somebody must decide to write**. Do not add them to `NEEDS-CHECKING.md` — it would turn
a list of things waiting on a machine into a list of things waiting on a decision, and those are the two
categories this repository has spent two sessions learning to keep apart.

**3. The brain is not reachable from the host**, so Phase 2's third done-clause is open — and unlike
the rest of the register, **nothing external is stopping it.** One MCP tool that resolves a request
through the capability registry is what the clause asks for. It is in [§1](#1-where-the-project-stands-in-one-paragraph)
with the evidence, and in [§8](#8-what-is-waiting-on-the-owner) as the choice it creates.

---

## 10. How to work on this

- **The owner is a BIM modeller, not a developer.** Explain in BIM terms. He delegates technical choices
  and is right more often than not about product ones — the folder restructure, the metadata standard,
  the toggle button, *"prove it yourself"* and *"study it but take none of its names"* were all his.
- **Do not hand him commands to run when you can run them yourself.** He called that out, fairly.
- **Field evidence beats specification.** The sharpest findings in this repository came from running
  things, not from any document.
- **Ask what already exists before designing anything.** On 2026-08-28 it worked four times out of five:
  three questions were settled by reading a system already doing the job, and `Q-13`'s answer had been
  running inside Heron since Step 1 while the question sat open. Look first; decide second.
- **An environment-specific block belongs in a sentence that names the environment.** *"The C# cannot be
  compiled here"* was true of one container that could not reach one download server. Written without its
  environment it became a fact about the project, and three sessions inherited it.
- **Read a document aloud before asking anybody to accept it.** Ajmal asked for all 30 Constitution
  Articles to be read out rather than tapping yes. Reading them found **three stale statements inside**,
  including eight Articles citing a *"Proposed"* Golden Rule that had been official since that morning.
  None changed what an Article required; all would have been read as current by whoever implements it.
- **Explain in the user's own materials, not in the abstract.** The trust model was explained once as
  *"lifecycle and source axes"* and he said plainly he did not follow it. Explained as a Revit family
  having **a maker** and **an approval status** — two things nobody would put on one dropdown — he agreed
  at once. The second explanation is also a better argument, which is usually the way round it goes.
- **A count cannot express a nuance, and should not be taught to.** Q-34 is *agreed but not signed off*.
  The question counter reads it as answered; the prose beside it carries the rest. The moment a counter
  needs to understand nuance it stops being a fact about the rows.
- **Say what is untested.** "It builds" is not "it works". On mobile it can now genuinely be *built* —
  which is a real rung above where this project was, and still two below *proven*.
- **Reference material is studied, never copied.** His earlier repositories are read for their
  reasoning; none of their names, dependencies or branding come across.
- **Every number in the docs should be derived, not typed.** The tooling exists; use it.
- **A library too small to be wrong proves nothing.** The retrieval query prefix-matched every word, so
  `in*`, `me*` and `the*` outvoted the one word that carried meaning. At **two** fragments it looked
  correct; at **seven** it was plainly broken. Nothing about the code changed in between. Before
  believing a component that ranks, sorts or matches, ask how many items it has ever been given.
- **Write a threshold as arithmetic, not as prose.** The quality nudge was documented as *"it can never
  outrank a better match"* and was in fact **eight times larger than one rank of fusion** — it could have
  jumped a `PROVEN` fragment eight places. The docstring was confident and wrong; the check that caught
  it was one subtraction. Any sentence of the form *"small enough that…"* is a calculation somebody has
  not done yet.
- **Retire an assertion with its reasoning, or repair it — never quietly loosen it.** *"Show me every
  duct must rank the duct filter first"* held at two fragments and failed at seven. The honest reading
  was not that retrieval got worse: that sentence is filter-**then**-select, a composition, which is
  what a **skill** names. The assertion was asking the wrong layer. Deleting it without that paragraph
  would have looked identical and taught nobody anything.
- **"Studied, not copied" fails silently unless somebody checks.** [D-25](docs/DECISIONS.md) was being
  obeyed in intent and broken in fact — a level lookup was verbatim with one variable renamed, under a
  commit message that said *"written fresh"*. It surfaced only because the owner asked outright. The
  answer was yes, and both pieces were rewritten. **Do the side-by-side yourself before the commit**,
  not when asked; the method is in [docs/31](docs/31-studying-the-existing-libraries.md).
- **A scanner that scans itself finds itself.** `check-gaps.py` reported its own regex as two undeclared
  agents on its first run. Funny once; worth remembering as the general shape — a tool that reads the
  repository is part of the repository.
- Commits are authored **Ajmal PS**. The repo is **private**.

---

*Phase 0 is finished and proven. Step 6 and the whole of Phase 2 are finished and proven of nothing —
built carefully, obeying rules that are now binding, tested where testing was possible, and compiled on
every release from 2020 to 2027. **Seventy fragments and ten skills** sit at `DRAFT`, which is not a shortcut
but [D-30](docs/DECISIONS.md) being obeyed: promotion needs one proof containing a negative case, and a
negative case needs a model. `python tools/check-gaps.py` is now the file that answers "what is left",
because it is computed from disk and this one is not — and where they disagree, believe the tool. It
currently says **nothing** here is unfinished and **55 are waiting: 48 on a Revit, plus the 86 unproven
fragments as one further item, 3 on Windows, 1 on a network that can reach the weights host, 1 on the
owner, and 1 on an optional dependency this machine does not have.** That breakdown sums to its own
total, which the sentence it replaces did not.
Almost nothing in this repository is waiting on another session; it is waiting on a machine — and **four
times now**, something believed to be waiting on a machine was waiting on somebody trying it. The
newest was `A8`, filed under *needs Windows* when what it needed was `pip install mcp` — and trying it
found that Heron's MCP server would not have started at all on a machine installing today. `D3` is
still the line that matters most: move the ducts 200 mm, then measure one.*
