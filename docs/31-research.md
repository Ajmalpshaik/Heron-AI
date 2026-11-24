<!--
Heron-Agent:  HERON-IMP-FEX-004
Heron-Step:   14
Heron-Status: DRAFT
Heron-Since:  0.1.0
Heron-Layer:  brain
See docs/29-metadata-standard.md
-->

# 31 — Research: what Heron learned from other projects

**One page for everything Heron has learned by reading other people's work.** It merges the former
research notes 26, 31, 33, 34, 35, 39, 45, 48 and 51, on 2026-11-24, at the owner's request, to cut
weight. Only the durable part is kept: the method, the verdicts, the lessons and what is still owed.
**Git history holds the full originals** - every evidence table, commit and walkthrough.

Every project studied is left unnamed here ([D-177](decisions/D-177.md)). Heron's own evidence - its
files, decisions and commands - is named as always.

---

## 1. The method: studied, never imported

[D-25](decisions/D-25.md) is the rule. **Every outside library or project is reference material.
Nothing is copied.** A capability that earns a place is re-authored from scratch, in Heron's shape,
under Heron's splitting and metadata rules, and checked in Heron.

- **The unit of work is a capability, not a file.** Five versions of one job read elsewhere become one
  Heron fragment. A count of files somewhere else is never a target.
- **A re-authored fragment starts at `DRAFT`**, whatever it was where it was read
  ([D-44](decisions/D-44.md)), and earns `PROVEN` only on a named real model with a negative case
  ([D-30](decisions/D-30.md)).
- **The project or person an idea came from is not named** - not in a note, card, skill, comment,
  commit or pull request, and not by a link or a path that carries the name ([D-177](decisions/D-177.md)).
- **A commercial product is studied as a black box only.** Reading its public pages and watching a trial
  on a test model is allowed; decompiling it, or taking its code, command names, screen wording,
  templates or branding, is not. Read its licence (EULA) before any trial is used for study.

---

## 2. What travels, and what cannot

| | Travels? | Why |
|---|---|---|
| **The mechanism** | Yes | *A wall stores its level one way and an MEP curve another* is a fact about Revit, and facts are not owned |
| **The scar** | Yes | *Leaving out one parameter made a level filter match nothing and report success.* The most valuable thing in any library, and the least visible |
| **The shape of a good proof** | Yes | D-30's negative case came from proofs elsewhere that recorded what came back empty |
| **The code** | No | Written fresh, against Heron's contract |
| **The words** | No | Heron's reasoning in Heron's sentences, so the note still reads when the other project is gone |
| **The names** | No | No file, folder, tool or brand names |
| **The proof** | No | Different code. A proof of the other is not a proof of this |
| **The authority** | No | *"Go and read that project"* is worthless the day it is retired. The reasoning is written here in full |

**The cost, plainly:** studying is cheap, re-authoring is moderate, and **proving is the expensive part,
gated on a real Revit on the owner's PC**. A plan that ignores that arrives with many DRAFT fragments and
no way to trust one.

---

## 3. The owner's rules, made operational

The owner's instruction, 2026-08-28: *"Check, study, and take what you need... 1. Check and edit. 2. If
you want to add, add. 3. If you want to split, split."* For each candidate, in order; **stop at the first
rule that applies.**

**Rule 0 - does it earn a place?** Does Heron already cover it? Answer by **naming the Heron capability
that does the job, or the claim fails** - resemblance is not enough. Two fragments answering one sentence
is the duplication [D-29](decisions/D-29.md) exists to prevent. If Heron covers it, skip it and record
nothing.

**Rule 1 - check and edit** (the default path):

1. Read the whole file, comments first. The comments carry the scars.
2. State the contract - `needs` and `provides` - before any code. If it cannot be stated cleanly, the
   thing is two jobs and Rule 3 applies.
3. Write it fresh against that contract, not a transcription with the names changed.
4. Carry the knowledge, and the *why*, in Heron's own words. A fix with no recorded reason gets removed.
5. Name it to Heron's standard ([29 §2](29-metadata-standard.md)). The name it had elsewhere carries no
   weight.
6. Set `heron-status: DRAFT` and write the proof's positive and negative cases without the proof.

**The check that makes Rule 1 real: put the two files side by side before committing.** The first
fragment re-authored under this rule, on 2026-08-28, was in fact copied line for line and claimed as
fresh. Naming the same Revit constants is not copying; the same structure, control flow, naming,
comments and design decisions is. **The honest test: can I say what I decided differently, and why?** If
not, no re-authoring happened.

**Rule 2 - add.** Add what the contract needs, the release list explicitly
([D-05](decisions/D-05.md)), and a negative case even where the original had none. Add nothing
speculative.

**Rule 3 - split.** Split when the extracted part has at least two real consumers. A `provides` list with
two unrelated entries is usually two fragments. Over-splitting is equally real and passes every check a
tool can run.

---

## 4. What the owner's own library taught

The owner's earlier fragment library was read in full on 2026-09-06: **330 files of fragment work, closing
at 312 covered, 5 impossible through the API and 13 missing - all 13 built.** Each built capability has a
folder in [`brain/fragments/`](../brain/fragments/); among them `PLAN_CONNECTION_ORDER`, `SET_VIEW_PHASE`,
`AUTO_SIZE_PIPE`, `UPGRADE_FAMILY_FILES`, `FIND_UNUSED_MATERIALS`, `FIND_UNUSED_GROUP_TYPES`,
`CAP_OPEN_PIPE_ENDS`, `SELECT_TYPES`, `SELECT_VIEW_TEMPLATES`, `ARRAY_ELEMENTS_RADIAL`,
`SET_GLOBAL_PARAMETER`, `ZOOM_TO_ELEMENTS` and `SELECT_SCOPE_BOXES`. What it taught:

- **A false "covered" is the expensive mistake**, because it ends the search. The first pass refuted only
  the *missing* claims; putting the *covered* ones to a refuter found four more real gaps.
- **A fragment that names its own boundary is the best gap detector in the repository.** Four of the five
  refuted claims sat inside a fragment whose purpose said "not here" or "linear only".
- **Matching file names against fragment words does not work.** It scored known gaps 0.67 to 1.00 against
  unrelated fragments, and was dropped rather than reported.
- **A possibility recorded without a release, on a call nobody ran, is as false as an impossibility
  recorded without one.** A schedule's calculated column was claimed buildable; a compile probe on every
  release showed it can be read and never authored.
- **A number got by subtraction is not a measurement.** The library had been called exhausted on an
  estimate; nobody had read the remaining files.
- **Creators need no fourth kind.** They are `kind: action` with `risk: MODIFY`; `kind` says how a thing
  composes, `risk` says what it may do.

---

## 5. What this is not

- **Not a migration.** No batch, no importer, no mapping file. Every fragment is its own act of
  understanding. [D-97](decisions/D-97.md) asks for the library to be re-authored in full and proved in
  one pass later; that changes *how many*, not *how*. Its condition: **a mechanism more than one fragment
  needs is written once and composed.**
- **Not a race to a number.** Ten fragments that are proven, composable and used beat three hundred that
  are none of those.
- **Not one-directional.** An improvement found while re-authoring stays here; the other library is read-only.

---

## 6. Studying an outside project - the research rule

**Binding.** Moved into the former note 31 on 2026-11-21 from the upgrade plan, a work note retired that
day; what that plan produced is the Improvement Gate ([D-68](decisions/D-68.md),
[D-69](decisions/D-69.md), and §7 rows 2.15 to 2.19 below). Sections 1 to 5 are about the owner's own
library; this one is about any other project Heron reads for an idea - a repository, a product, a paper.
**The outside project is a research reference, never a supplier of code.** Every idea taken is rewritten
for Heron's constraints and checked against the current repository before it is built, and an idea is
taken only when it solves a measured Heron problem: *do not chase every new feature.*

### 6.1 Research does not finish when a plan is written

For every feature, fix, refactor or upgrade that uses an idea from another project, the session building
it does this again **at implementation time**:

1. Open the other project.
2. Read its current README **and the implementation files** behind the idea, not the README alone.
3. Check whether it has changed - moved, archived, rewritten - since the note that recorded the idea.
4. Confirm the licence and provenance **at the exact path**, before using anything beyond a general idea.
5. Compare its mechanism with what Heron does now.
6. Write the Heron design before writing code.
7. Build only what fits Heron's architecture and product goals.
8. Re-read the other project whenever a behaviour, boundary, lifecycle or failure case is unclear.
9. Never fill an uncertainty with a guess when the source can be read.
10. Record why Heron's version is different, where that difference matters.

**A previous note, a memory or an earlier AI summary of the other project is not the source.** README for
orientation, implementation for truth; where they disagree, the implementation wins.

### 6.2 What is never done

- copying a large implementation and renaming it;
- importing another project's folder structure because it looks clean;
- taking another project's terms where Heron already has a stable word of its own;
- adding a dependency only to reproduce another project's architecture;
- pasting another project's prompts, skills, tests or workflows into Heron unchanged;
- copying code without checking the licence;
- changing Heron's architecture only to match the other project.

The allowed route is **study → understand → take the principle → map it to Heron → design → build → test →
record**. If reusing code ever seemed genuinely necessary, that is a separate licence and provenance
decision for the owner, never ordinary implementation - and [D-25](decisions/D-25.md) and
[AGENTS.md](../AGENTS.md) say Heron does not do it.

### 6.3 Naming

**[D-177](decisions/D-177.md) settles this, 2026-11-17: an idea taken from another project is described in
Heron's own words, and the project, its repository, its product and its author are not named** - not in a
research note, proposal, decision, card, skill, code comment, commit message or pull request, and not by a
link, a path or a quotation that carries the name. Heron's own evidence is named as always. No code is
taken (its point 4), so no attribution file arises.

### 6.4 When it worked

An idea from another project has been taken well when it was **understood independently, adapted to Heron,
tested against Heron's own requirements, and rejected when it did not fit** - and the change it led to went
through the same loop as any other: understand → baseline → research → diagnose → design → build →
critique → verify → record. The objective is not more AI; it is a Heron that gets cleaner, safer, easier to
test and upgrade, and harder to break.

---

## 7. The patterns, and what each became

*How* other projects got their result, and what that is worth here - read between 2026-09-09 and
2026-10-19. **No code from any of them.** The number in the first column is the
pattern's number in the former note 34, kept so older references can be followed.

| # | Pattern | Verdict | Where it lives in Heron |
|---|---|---|---|
| 2.1 | **Tiered depth** - carry an abstract, an overview or the full body, only as deep as the task needs | BUILT | `ABSTRACT` / `OVERVIEW` / `FULL` in [`heron_context.py`](../brain/heron_context.py). With 2.2, a generation packet went from 5,737 characters to 372, **the request byte for byte at every depth**. The cases abstract counts positive and negative apart, because D-30 makes the negative load-bearing |
| 2.2 | **A marker only on the case that could mislead** - an empty or cut answer says so; a full one stays unchanged | BUILT | `Part.cut` and `Context.reduced` in `heron_context.py`, printed as a `CUT` line by the CLI and the `heron_context` tool ([`tests/test_brain_reachable.py`](../tests/test_brain_reachable.py)); the same rule for fragments is [D-52](decisions/D-52.md) and [D-64](decisions/D-64.md) |
| 2.3 | **Compress what came back, never what was asked** | ALREADY HELD, made stricter | `FULL_ONLY = (REQUEST, SITUATION)` in `heron_context.py`: the code path that would shorten a request does not exist. No compressor is built |
| 2.4 | **Deterministic before the model, as an order** | ALREADY HELD | [19 §5](19-context-and-cost.md), [02 §6](02-architecture-overview.md) |
| 2.5 | **Undeclared is refused** | ALREADY HELD, more strictly | a branch every operation passes in `revit/Heron.Revit.Addin/RevitOperations.cs`, not a policy file a path can forget |
| 2.6 | **No completion claim without fresh evidence** | ALREADY HELD, more strictly | [D-30](decisions/D-30.md) - a proof format a tool checks, not prose a model is asked to obey |
| 2.7 | **The store must be free to rebuild** | ALREADY HELD | [Golden Rule 11](14-golden-rules.md), [D-24](decisions/D-24.md). A memory built by model calls cannot be rebuilt, so it goes stale |
| 2.8 | **The harness does not decide when to stop** | ALREADY HELD | [D-01](decisions/D-01.md): whoever runs the loop decides |
| 2.9 | **Enforce at the moment of the act**, with a hook that blocks the edit | BUILT | [`heron-guard`](../.claude/skills/heron-guard/SKILL.md), wired from [`.claude/settings.json`](../.claude/settings.json) - a hook declared in a skill's front matter runs only when that skill is loaded. One rule, not five; nested decision; **a crash denies**; `HERON_GUARD=off` as the hatch. [`tests/test_heron_guard.py`](../tests/test_heron_guard.py) |
| 2.10 | **Fact-forcing beats "are you sure?"; pointing beats typing** | DECISION [D-60](decisions/D-60.md) | a preview selects what it would change and skip; Revit's Keep or Discard box draws it (`HeronKeepQuestion.cs`) |
| 2.11 | **Guard the path from retrieval into context** | DECISION [D-81](decisions/D-81.md) | carried text is **stamped, not scanned**: every part Heron did not write names its source |
| 2.12 | **An inventory of what imported material permits** | DECISION [D-66](decisions/D-66.md) | [`tools/check-licence.py`](../tools/check-licence.py) reads the files, not the landing page |
| 2.13 | **A third retrieval stream, from a graph** | REJECTED, measured | [`tools/measure-graph.py`](../tools/measure-graph.py): six settings all lost - P@1 down 1.1 to 14 points, P@5 never better. Heron's graph is dense (median 50 neighbours a fragment, worst 230), so a hit's neighbours are a large slice of the library, not a signal |
| 2.14 | **A council of models judging each other; a swarm with consensus** | REJECTED | many model calls a question ([19 §5](19-context-and-cost.md)); Heron has one Revit, one pipe, one queue ([D-09](decisions/D-09.md)). Kept for a future Shadow Mode ([D-39](decisions/D-39.md)): **anonymise *and* shuffle** the answers judged, or position bias becomes a bias toward production |
| 2.15 | **Judge the finished diff against a one-line intent** | BUILT | [`tools/check-change.py`](../tools/check-change.py) ([D-68](decisions/D-68.md)) - structural, from the layering table, not word overlap; three fields, `intent`, `area`, `risk`. Baseline, one change, keep or revert: [`tools/change-evidence.py`](../tools/change-evidence.py) |
| 2.16 | **A file no rule recognises widens the checks** | BUILT | `check-change.py`: an unknown folder is `unrelated` even when declared; an unusable intent exits `BLOCKED` |
| 2.17 | **Check the thing delivered, not the tree it was built from** | BUILT | [`tools/check-package.py`](../tools/check-package.py) reads `Heron.addin` - a wrong entry class, an unbuilt assembly, a `<ManifestSettings>` that crashes 2025 and older - and prints what it cannot answer |
| 2.18 | **Mock only what you genuinely cannot have, and make a mock look like one** | RULE | [13 §3a](13-testing-and-quality.md). Heron never mocks Revit: a mocked Revit would give green suites and no knowledge |
| 2.19 | **A finding carries the evidence that settles it** | ALREADY HELD | `check-revit-gate.py` (verdicts about evidence), `check-routing.py`; every `check-change.py` signal names its paths |
| 2.20 | **Scan what is about to be published, against lists that already exist** | BUILT | [`tools/check-secrets.py`](../tools/check-secrets.py) ([D-143](decisions/D-143.md)): the shapes are `heron_secrets.PATTERNS`, the names `.gitignore`'s; tracked files only; no value printed; no allow-list |
| 2.21 | **A superseded record says so where a citation of it lands** | BUILT | [`tools/check-supersessions.py`](../tools/check-supersessions.py), run by `check-docs.py` ([D-144](decisions/D-144.md)) |
| 2.22 | **A fabrication check that calls no model** | BUILT | §7.1 |
| 2.23 | **A chunk carries its heading path; a rule is never cut from its exception** | BUILT | §7.2 |
| - | **A trust table of who is speaking** (modeller, host, pipe, linked document, imported package, indexed standard...) | BUILT | [24 §7](24-trust-model.md) - every row derived from an existing rule |
| - | **A benchmark is positive and negative samples** | NOT BUILT | the shape is D-30's; the cases must be real Revit tasks only the owner can write |
| - | **A prefix cache wants volatile parts last; Heron's packet puts the request first** | RECORDED, no change | Heron makes no model call and cannot see the host's cache ([D-58](decisions/D-58.md)); a reader gets the request first, on purpose |
| - | **Capture everything, compress it with a model, apply "instincts" at a confidence score** | REJECTED | a model call per batch (D-58); a confidence count is what `heron_promotion.py` refuses - one success is not proof |
| - | **An in-Revit editor where the AI writes C# for a saved command** | REJECTED | a second, ungated route to new C# would bypass proof; the `heron-fragment-author` agent and the compile gate are that route |

**Corrections a second pass made to these, kept as rules:** a repository can move or be archived - check
before planning against it; self-improvement is bounded and evaluation-gated (baseline, one change,
re-measure, keep or revert), never an agent rewriting itself; one authority behind many interfaces, and
the UI is never where Revit logic lives; discover the Revit release, document and capabilities rather than
guess them; packaging is part of quality.

### 7.1 A fabrication check that calls no model

The idea: compare every claim in a generated answer with the source it came from, **with no model** - both
sides normalised, only fact-bearing sentences checked.

**Built as** [`brain/heron_ground.py`](../brain/heron_ground.py) - `check(draft, packet)`, which the host
calls back with its draft and the packet it came from - tested by [`tests/test_ground.py`](../tests/test_ground.py),
and reached from a chat as the `heron_check` tool. It **flags and never rewrites**, and uses only `difflib`
and `re`. **Fabrication is an added fact, not a missing one:** a claim whose every number, dimension,
clause reference and named thing is in its source cannot have invented one, so a true but less specific
sentence is never flagged. **Only a quotation has a gate**, and it is containment. A similarity threshold
was measured and **removed, not lowered** - true paraphrases (0.222 to 0.682) and wrong claims (0.204 to
0.588) overlapped almost entirely. The first fabrication rate, with its caveats, is in
[`brain/retrieval-history.md`](../brain/retrieval-history.md). A run on a real numbered standard is
[NEEDS-CHECKING Group OO](needs-checking/group-oo.md).

### 7.2 A chunk carries its heading path, and a rule is never cut from its exception

The idea: put a short context in front of each chunk before it is embedded, so a chunk is not read alone;
and never split a rule from the exception that qualifies it - **a rule returned without its exception is
worse than a miss**, because it states the opposite of the requirement with a citation attached.

**Built in** [`brain/heron_ingest.py`](../brain/heron_ingest.py), tested by
[`tests/test_ingest.py`](../tests/test_ingest.py). **The context is the clause's heading path, read off the
document and never generated**, so re-indexing stays free ([D-24](decisions/D-24.md)). A split just before
*except*, *unless*, *provided that*, *however* and similar words is not a split point (`QUALIFIERS`); with
no legal split the chunk stays oversized and is flagged. `--boundaries <document id>` prints a document's
chunk boundaries for a person to read once before it is trusted. Every ingest prints the chunk count
against [D-23](decisions/D-23.md)'s ceiling, which the field puts at roughly 500,000 vectors for brute-force
search. **Not taken:** a graph a model builds over every chunk, a multi-service stack, evaluation judged by
a model, and a local parser with its own layout models (write the parser first; see Group OO).

### 7.3 The lesson behind the patterns

**A rule in code beats a rule in prose** - and *a boundary that fails open is not a boundary.* Heron's
*compression never touches the request* was a tested docstring; it is now a branch, and the path that
would break it does not exist.

---

## 8. Licences

One studied project was **AGPL-3.0** across nearly all its files, with a few folders under other licences. Heron is Apache 2.0
([D-08](decisions/D-08.md)), so its source was never opened - its idea (tiered depth) came from the README
alone. The rule the licence audit left: **read the copyright line, per directory, not per repository.**
Among the projects read, one README said *"use freely"* over skills marked *all rights reserved*, one had
no licence at all where MIT was assumed, one carried another project inside it, and one shipped a
placeholder licence file. Heron's answer is a gate that reads the files ([D-66](decisions/D-66.md)).

---

## 9. Where Heron stands against other Revit AI and MCP tools

Dated observations, from reading the code of open-source Revit MCP projects (2026-08-27, re-checked
2026-09-22 and 2026-11-15), the public pages of a commercial Revit AI (2026-11-08, by search summary, not
run), and one open-source Revit MCP platform read in full (2026-11-16). The field moves; re-check before
relying on any line.

**Heron is ahead on:**

- **Proof per operation** - a record on a named real model, with a negative case and a staleness
  fingerprint ([D-30](decisions/D-30.md)). Still Heron's alone on 2026-11-15; others have per-release
  smoke runs, none per operation, none with a negative case.
- **The lease, and never sliding onto another Revit** ([25](25-multi-session-and-binding.md),
  Constitution Article 12a). Several projects now handle two Revits of the same version; none has a lease.
- **Safety by default** - changes off until the modeller turns them on in Revit
  ([D-166](decisions/D-166.md)); a person approves Keep or Discard ([D-126](decisions/D-126.md)), not a
  token the AI can send back itself; Heron runs only library fragments, never code it is handed
  ([D-28](decisions/D-28.md)). The biggest open family still shipped an ungated run-any-code tool on
  2026-11-15.
- **Library depth and MEP engineering** - HVAC, fire, loads and sprinkler work ([41](41-hvac-design.md) to
  [47](47-sprinkler-layout-from-the-model.md)); families ([43](43-building-revit-families.md)).
- **Standards and site words** answered only from a cited source, never guessed
  ([D-33](decisions/D-33.md), [D-34](decisions/D-34.md)).
- **Releases** - Heron compiles for 2020 to 2027; [16 §6](16-version-support-strategy.md) says which have
  run in a real Revit. The commercial tool lists 2022 to 2026; the platform read in full, 2022 to 2027.

**Heron is behind on the platform around the change:** a batch over a folder of models, QA reports in
Excel, HTML and BCF, MSI installers with IT policy templates, build provenance on every release, a smoke
run on every supported Revit year, read-back and lost-reply recovery already run in Revit, remote use from
a Mac, and product polish around repeat work - commands saved to a firm library and pinned to the ribbon,
shared annotation templates, a visual-programming graph turned into a command. On the 2026-11-16
scoreboard, judged 0 to 5 per area, Heron scored 87 of 135 and the platform 101: **Heron the safer engine
with the deeper library, the other the more complete product.** None of its leads asks Heron to be less
safe.

**What the field taught about direction:**

- **The tool layer is being commoditised.** Autodesk ships its own MCP server on Revit 2027 - read tools,
  and (by search summary of 2026-11-15, still to confirm on Autodesk's page) write and experimental
  installers on 2027.2. **Sit on top of it on 2027; keep Heron's own bridge for 2020 to 2026.** Growing the
  fragment count is not progress in itself.
- **Others caught up on undo and dry run** - grouped single undo and preview are now common. What remains
  Heron's is evidence that an operation was ever right.
- **A funded commercial product holds the "a firm's standards as memory" idea** in two authoring tools.
  Heron's wedge: **MEP, and Qatar/QCS/Ashghal practice**.

**Not taken from them:** changes on from install; a confirmation the AI can give itself; raw C# from the
chat with no sandbox; a silent background update; Revit's non-public internals by reflection; a checksum
from the same release as proof of who built it; an approval key stored beside what it approves; cloud
document folders without an owner decision.

---

## 10. Revit lessons worth keeping

Scars from other people's notes and code. A scar's diagnosis is a hypothesis until something isolates it;
the ones marked *unchecked* have not been run by Heron.

- `NewFootPrintRoof` failing *"Value cannot be null"* was the `out` curve array passed as null - create it
  first (Heron isolated it 2026-09-19; others blamed the active view - [Group AP](needs-checking/group-ap.md) AP4).
- A roof boundary must not repeat its first point - that is an edge of no length.
- A gable slopes its two long eaves, a hip every edge; the per-edge *SlopeAngle* is a rise over a run, not
  an angle.
- Two parameters can share a name on one element (a curtain wall type's *Interior Type*); a lookup by name
  picks one at random - reach them by parameter id ([row 5b-203](fragment-issues/section-5b-rows-201-225.md)).
- A change on a type reaches every placed instance of it - a bigger change than the same write on one
  element.
- Read back what was made; a call that returned is not a call that worked.
- A revision cloud on a sheet, or on a view placed on it, puts the revision on that sheet by itself;
  assigning by hand is for a revision with no cloud.
- A new element may land silently in the active design option - to measure, AP9.
- Revit stores feet and radians; use exact factors, never rounded ones.
- Element ids became 64-bit at 2024; the old integer property is gone by 2026; on 2022 and 2023 ids above
  2,147,483,647 fail (*unchecked*).
- Floor, unit and filter-rule calls changed between releases; the toposolid replaced the toposurface at
  2024; a floor's own boundary edit splits at 2022.
- A schedule's calculated column can be read and never authored, on every release; `Document.Phases` and
  `PHASE_NAME` are read-only; there is no scope box creation call.
- Overwriting a view filter of the same name strips it from every view that used it.
- A zero-area room is two faults - unplaced and unenclosed - kept apart.
- A clash test must use every solid, and count an element with no solid as untested.
- A link must be read with its transform; its *loaded* flag is not stored state. A link with a rotation or
  offset is usually placed by shared coordinates on purpose.
- Project parameters reach schedules; tags need shared parameters.
- The shared parameter file setting is Revit's, not the model's - Undo never restores it, so put it back.
- A mirrored door schedules the wrong hand; mirrored and flipped are different things.
- A level's *Building Story* tick decides where an IFC export files its elements.
- Stairs are refused inside an open transaction.
- `DocumentChanged` reports the inner transaction's name, not the group's renamed name
  ([Group NU](needs-checking/group-nu.md)) (*unchecked*).
- Deleting a link type unloads its document, and rollback does not reload it - such a change cannot be
  previewed by running and rolling back (*unchecked*).
- Revit raises group errors only at commit, so a preview must commit the inner transaction and roll back
  the group to see them (*unchecked*).
- Resolve a failure only with *Fix elements* or *Set value*; other resolutions can delete or move elements.
  Heron rolls back on any error instead.
- Pinned, hosted or constrained elements can silently not move - compare after (*unchecked*).
- An ExternalEvent is not serviced while a modal dialog is open - why Revit can be "busy" with a dialog up.
- `OpenAndActivateDocument` can open a second copy of a model already open (*unchecked*).
- The save-reminder dialog's id is `TaskDialog_Project_Not_Saved_Recently` (*unchecked*).
- Revit's *Always Load* choice for an unsigned add-in is kept only after a normal close, and resets when
  the DLL changes (Q-77) (*unchecked*).
- Build against the lowest point release of each Revit year, so the add-in loads on its first update
  (*unchecked*).
- Revit 2027 runs on .NET 10 - confirmed in [`Directory.Build.props`](../Directory.Build.props).

---

## 11. Still owed

Every *to build*, *worth building, not built* and *open point for the owner* the former notes left, checked
on 2026-11-24 against the registers and the fragment cards. **A card's own `heron-status:` line is the
current word on whether it is proven**; this table does not repeat it.

| Item | From | Status |
|---|---|---|
| A wall's profile - an arched head | 39 §5 | Partly: `SHAPE_WALL_TOPS_TO_ROOF` edits a wall's profile to a roof, 2022 on ([Q-91](open-questions/tier-2.md), [row 5b-1672](fragment-issues/row-5b-1672.md)); an arched head NOT TRACKED |
| Point clouds | 39 §5 | Built as `LINK_FILES` widened - [Group MK](needs-checking/group-mk.md) |
| A curtain wall type's Fixed Number layout | 39 §5 | NOT TRACKED ([Group JJ](needs-checking/group-jj.md) only notes `ADD_CURTAIN_GRID_LINES` refuses it) |
| Grade a change by what it reaches (a type, a level, an issued sheet), not by its verb | 39 §4 | NOT TRACKED - a decision, since it changes every write answer |
| Where each link sits - offset and rotation from the host | 45 §6 | Partly: `CHECK_LINK_COORDINATES` prints a link's difference from its shared coordinates; offset and rotation from the host NOT TRACKED |
| Elements NOT on a workset | 45 §6 | NOT TRACKED |
| A numeric schedule filter | 45 §6 | NOT TRACKED - `SET_SCHEDULE_FILTERS` writes text on purpose (units across releases, its card) |
| Room proportion and minimum size against the modeller's values | 45 §6 | NOT TRACKED |
| Panel sizes on a curtain wall | 45 §6 | Built as `REPORT_CURTAIN_ELEMENTS` version 2 - [Group JR](needs-checking/group-jr.md) |
| A build that can be run twice - a wall already there is not drawn again | 45 §6 | NOT TRACKED for `CREATE_WALL` (`CREATE_WALLS_BY_FACE` already skips, [Group JO](needs-checking/group-jo.md) JO11) |
| Copy to another level, landing on that level | 45 §6 | Built as `COPY_ELEMENTS` version 2 and later |
| Which face of a wall is its outside | 45 §6 | NOT TRACKED |
| Glazing per wall and per room, and each facade's bearing from true north | 45 §6 | Partly: `brain/heron_takeoff.py` measures glazing and facing per Space for loads ([44](44-building-loads-from-the-model.md)); a report of its own NOT TRACKED |
| A report of the project's parameter bindings | 45 §6 | NOT TRACKED ([Group JF](needs-checking/group-jf.md) notes no tool reads them) |
| A takeoff by two keys at once - level and size | 45 §6 | NOT TRACKED - `GROUP_AND_COUNT`, which `SUM_BY_GROUP` merged into, groups by one parameter |
| The IFC schema typed in a chat | 45 §6 | Tracked as a recorded decision - [FRAGMENT-ISSUES section 6](fragment-issues/section-6.md) |
| Elements with no IFC class, through the export mapping | 45 §6 | Tracked - [Group CD](needs-checking/group-cd.md) CD9 |
| A caveat on volume takeoffs - unjoined overlapping elements counted twice | 45 §6 | NOT TRACKED |
| Annotation template and tag in one pass, over many views | 48 §5 item 2 | Built as `TAG_VIEWS_BY_RULES`; a template **file** exported and shared NOT TRACKED |
| Dimension template for MEP runs | 48 §5 item 3 | Built as `DIMENSION_VIEWS_BY_RULES` |
| Items 1 and 4 to 8 - saved commands, QA checklists, MEP schematic, CAD lines to ducts and pipes, sheets from a spreadsheet, rebar from a spec | 48 §5 | Built: [49](49-saved-commands-and-the-heron-panel.md), [50](50-qa-checklists.md), `DRAW_MEP_SCHEMATIC` ([Group MW](needs-checking/group-mw.md)), `DRAW_MEP_FROM_CAD` ([Group MX](needs-checking/group-mx.md), [row 5b-1481](fragment-issues/section-5b-rows-1476-1500.md)), `CREATE_SHEETS` version 4 ([Group OZ](needs-checking/group-oz.md), [D-193](decisions/D-193.md)), `CHECK_REBAR_AGAINST_SPEC` ([Group MZ](needs-checking/group-mz.md)) |
| A visual-programming graph turned into a recipe | 48 §5 | NOT TRACKED |
| Cloud document folders (ACC) | 48 §4-5 | NOT TRACKED - needs an owner decision first |
| Tagging fabrication parts; a title block overlap check | 48 §2 | NOT TRACKED (both "unsure" when written) |
| Allow the commercial tool's site in the network, to re-check its features first-hand | 48 §6.1 | NOT TRACKED |
| A trial of the commercial tool for black-box study, after reading its EULA | 48 §6.2 | NOT TRACKED |
| Which of items 1 and 2 first | 48 §6.3 | Settled - both built |
| QA reports as BCF issues with a camera | 51 §3.2 item 1 | NOT TRACKED (Excel and HTML built, [D-178](decisions/D-178.md)) |
| An office checklist with project overrides | 51 §3.2 item 2 | Built - [D-179](decisions/D-179.md) |
| Hide folder paths in replies | 51 §3.2 item 3 | Dropped - [D-180](decisions/D-180.md) not accepted |
| Group Policy templates for the three ceilings | 51 §3.2 item 4 | Built in [`platform/policy/`](../platform/policy/README.md); a run on a domain NOT TRACKED |
| Build provenance on every release asset | 51 §3.2 item 5 | Built in `.github/workflows/release.yml`; its first run at a tagged release NOT TRACKED |
| Grids and levels against a link | 51 §3.2 item 6 | Built as `CHECK_DATUMS_AGAINST_LINK` - [Group OJ](needs-checking/group-oj.md) |
| Many models overnight, read only | 51 §3.2 item 7 | Decided, not built - [D-181](decisions/D-181.md), [Q-93](open-questions/tier-3.md) |
| The host AI tested end to end | 51 §3.2 item 8 | First half built, [`tools/score-host.py`](../tools/score-host.py); driving a live chat NOT TRACKED |
| Revit 2021, 2022, 2023 and 2025 on a PC for the per-release check | 51 §7.1 | Tracked - [Group EQ](needs-checking/group-eq.md) |
| A decision for items 6 and 8 | 51 §7.2 | NOT TRACKED (items 1 and 7 have [D-178](decisions/D-178.md) and [D-181](decisions/D-181.md)) |
| Remote use from a Mac or the cloud - reopen, or leave? | 51 §7.3 | NOT TRACKED - [D-132](decisions/D-132.md) stands |
| Should what a `revit_` tool returns be screened or labelled as data before a model reads it? | 33 §5.1 | NOT TRACKED - an owner question |
| Measure how often a chat follows Heron's own instructions - from the chats already on the PC, at no model cost | 33 §5.1 | NOT TRACKED - an owner question |
| A generation counter, so a call made before a switch of Revit cannot land after it | 26 §9.2 | NOT TRACKED - an idea Heron has not got |
| Autodesk's write installers on 2027.2, to confirm on Autodesk's own page | 26 §9.3 | NOT TRACKED |

---

↑ [Research](guide/research.md) · [Documentation](README.md) · [Home](../README.md)
