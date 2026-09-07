<!--
Heron-Agent:  HERON-IMP-FEX-004
Heron-Step:   14
Heron-Status: DRAFT
Heron-Since:  0.1.0
Heron-Layer:  brain
See docs/29-metadata-standard.md
-->

# 31 — Studying the existing libraries

**The owner has a working fragment library of several hundred entries. None of it is imported. All of it
is worth reading.** This document is how one becomes the other.

[D-25](DECISIONS.md) settled the principle — *studied and re-authored, never imported* — and deleted a
planned import pipeline rather than choosing between libraries. What it did not give was a **method**,
and without one "study it" becomes "read it and copy it more slowly."

> **The owner's instruction, 2026-08-28**, and it is the whole of this document:
> *"You can check and study everything, but don't copy-paste blindly. Check, study, and take what you
> need... 1. Check and edit. 2. If you want to add, add. 3. If you want to split, split."*

---

## 1. What is actually there

Read once, on 2026-08-28, to size the job rather than to take anything:

| | |
|---|---|
| **398** working C# fragments | organised as filters, actions, creators, recipes, context and commands |
| **221** carrying a recorded verification | against a real model, most dated 2026-08-06/07 |
| The largest areas | sheets and views (54), reporting (43), structural changes (33), QA checks (30), colour and graphics (25), parameters and naming (21) |
| **12** skills | the real jobs: HVAC terminal layout, space airflow, duct routing, fire sprinkler layout, MEP grayout, connectivity verify, MEP trace, family creation, visual reporting |

### The full sweep, 2026-09-06 — every eligible file read, and what is left

The 2026-08-28 sizing above was a read to *size* the job. On 2026-09-06 the library was put on the
owner's machine at `D:\Ajmal\AJ AI Brain`, and every eligible file was read and classified: twelve
readers, one source folder each, then a second agent per claim told to **refute** it.

| Read | Already covered by Heron | Impossible via the API | **Genuinely missing** |
|---|---|---|---|
| **330** | 317 | 4 | **9** |

> **The covered column read 316 until 2026-09-06 and the row did not add up: 316 + 4 + 9 = 329
> against 330 read.** The refuted claim was struck off the missing column and never added to the
> covered one. It is one file and it changes no conclusion, which is exactly why it survived being
> printed twice in two documents — a total nobody adds up is not a check.

Of the 398 files, **68 were never fragment work** — 44 `recipes/` (these become skills), 12 `context/`
([D-46](DECISIONS.md)), 8 `commands/` (native Revit commands), 3 `examples/` and 1 `lib/`. That leaves
the 330 above. Ten claims were raised; one was refuted; **nine stand:**

| Source | The job, and why nothing here covers it |
|---|---|
| `reporting/action-plan-shortest-route` | Compute the cheapest way to connect a loose set of points and report the ORDER plus total run length — tree (MST), chain (2-opt), or finishing each room before moving on. **Nothing in the library decides an order.** `CREATE_ELECTRICAL_RUN` consumes points already sequenced and `RENUMBER_SEQUENTIAL` states outright that it does not choose the order — so both currently depend on a human to supply what this would compute |
| `sheets-views/action-set-view-properties` | Set a VIEW's **Phase and Phase Filter** in bulk. `SET_VIEW_PROPERTIES` was re-authored narrower — scale, detail level, visual style — and `SET_ELEMENT_PHASE` writes elements, not views |
| `structural-changes/action-auto-size-pipe` | Size pipes from the flow they already carry — bore = flow / target velocity, snapped UP to a real size, reporting resulting velocity |
| `structural-changes/action-batch-upgrade-revit-files` | Upgrade a FOLDER of `.rfa` / `.rft` / `.rte` to the running Revit, as windowless background documents |
| `structural-changes/action-purge-unused` | Report unused content beyond view templates and filters — materials nothing references (paint-only use included) and GroupTypes with no placed instance |
| `qa-checks/action-check-open-pipe-ends` | Cap open pipe ends via `PlumbingUtils.PlaceCapOnOpenEnds`, sized and connected |
| `creators/create-dimension` | One dimension string across Grids and/or Levels — the setting-out dimension |
| `filters/filter-by-types` | Return the TYPE elements themselves (`FamilySymbol`, `DuctType`, `WallType`) as an actionable set |
| `filters/filter-by-view-templates` | Return View Templates themselves as an actionable set, narrowed by name and by usage |

### All nine are built, and the audit's blind spot is not closed

**Built 2026-09-06 as TEN fragments, because one of the nine was two jobs.** `action-purge-unused`
covered materials and group definitions, which share no mechanism and no sentence: a material is
proved unused by walking every element, every type and every painted face, and a group definition by
asking whether anything is placed. They are `FIND_UNUSED_MATERIALS` and `FIND_UNUSED_GROUP_TYPES`.
Splitting is what [`HANDOVER.md` §9a](HANDOVER.md) step 2 asks for; folding them together would
have meant a mode string, and a mode string is the shape that hands a whole list to a delete on a
typo.

| Source | Built as |
|---|---|
| `reporting/action-plan-shortest-route` | `PLAN_CONNECTION_ORDER` — computes, draws nothing |
| `sheets-views/action-set-view-properties` | `SET_VIEW_PHASE` |
| `structural-changes/action-auto-size-pipe` | `AUTO_SIZE_PIPE` |
| `structural-changes/action-batch-upgrade-revit-files` | `UPGRADE_FAMILY_FILES` |
| `structural-changes/action-purge-unused` | `FIND_UNUSED_MATERIALS` **and** `FIND_UNUSED_GROUP_TYPES` |
| `qa-checks/action-check-open-pipe-ends` | `CAP_OPEN_PIPE_ENDS` — caps only; the reporting half already existed |
| `creators/create-dimension` | `DIMENSION_GRIDS_AND_LEVELS` |
| `filters/filter-by-types` | `SELECT_TYPES` |
| `filters/filter-by-view-templates` | `SELECT_VIEW_TEMPLATES` |

**Building them found a defect in a fragment that was already here.** `FIND_UNUSED_DEFINITIONS`
walked views only, so a view template used solely as a view family type's default — the template new
views of that kind start with — was reported unused. Nothing points at it, and deleting it changes
what every new plan is created with with nothing on screen saying so. It was caught because
`SELECT_VIEW_TEMPLATES` asked the same question a third way and the two answers disagreed. Both check
both kinds of use now.

**NINE WAS A FLOOR, NOT A CEILING, AND THE METHOD WAS WHY.** Ten *missing* claims were handed to a
refuter. **The 317 *already covered* claims were not.** The refutation pass was built to stop a
duplicate being written, which is the cheap failure; a false "covered" is the expensive one, because
it ends the search.

### The refutation pass on the covered side, 2026-09-06 — five more found

**That blind spot is now closed.** All 321 remaining files - 317 claimed covered, 4 claimed
impossible - were put to the opposite test: *name the Heron capability that does this job, or the
claim fails.* A claim backed only by a plausible-sounding neighbour was not accepted; where the
neighbour's own purpose narrowed it, the source file was read and the fragment's contract checked.

| | Claimed | Held | Refuted |
|---|---|---|---|
| Already covered | 317 | **312** | **5** |
| Impossible via the API | 4 | **4** | 0 |

> **One of those five was then refuted in turn, by building it — see below. The final count is 312
> covered, 5 impossible, 13 missing, and all 13 are built.**

**The four impossible verdicts all held, and the source library had already proved each one itself** -
`Document.Phases` is read-only and no Phase-creation method exists anywhere in the API surface;
`PHASE_NAME` is read-only so a phase cannot be renamed; there is no Scope Box creation call, which
also kills the delete-and-recreate route to resizing one.

**The five raised, of which four were real** - all four built 2026-09-06 as
`ARRAY_ELEMENTS_RADIAL`, `SET_GLOBAL_PARAMETER`, `ZOOM_TO_ELEMENTS` and `SELECT_SCOPE_BOXES`:

| Source | The job, and why nothing here does it |
|---|---|
| `move-copy-rotate/action-array-elements` (radial mode) | Copies swept around a CENTRE POINT through a sweep angle. `ARRAY_ELEMENTS` takes a direction, a spacing and a count - it is linear and only linear. The library's own precedent settles this: a linear array is worth a fragment rather than N copies, so a radial one is too |
| `parameters-naming/action-report-global-parameters` (set mode) | SET a global parameter's value. `REPORT_GLOBAL_PARAMETERS` says *"creating or setting one is deliberately not here ... a MODIFY job"* - and no other fragment picked it up. A global's value is written through `GlobalParameter.SetValue`, not through a parameter on an element, so `WRITE_ELEMENT_PARAMETERS` cannot reach it. Carries two real refusals: a formula-driven global rejects the write, and a reporting one can never be set |
| ~~`sheets-views/action-add-schedule-calculated-field` (formula mode)~~ | **WRONG - this claim was refuted the same day, and the correction is below.** It is a fifth IMPOSSIBLE, not a gap |
| `visibility/action-show-elements` | Navigate the view to elements - `UIDocument.ShowElements`. Nothing here calls it. `SET_SELECTION` selects and does not move the view, which leaves a user staring at another level with something highlighted off screen; and **`SHOW_ELEMENTS` is a name trap** - it means UNHIDE, so *"show me these"* reaches the wrong fragment entirely |
| `filters/by-view-and-sheet/filter-by-scope-box` | Scope boxes as an actionable element SET, narrowed by name. `READ_SCOPE_BOX_EXTENT` finds ONE by name and returns its corners; `ASSIGN_SCOPE_BOX_TO_VIEW` acts on one by name. Nothing enumerates them or hands them back as elements to rename, report on or delete. **Structurally the same gap as `SELECT_VIEW_TEMPLATES`**, which was found and built the same day |

### A gap claim of my own, refuted by trying to build it

**The schedule CALCULATED VALUE column is not a gap. It is impossible on every release, and the
claim above was made on somebody else's guess.** The source file says the capability *"was added to
the public API in a later Revit release (2022+) as `ScheduleField.SetFormula(string)`"* and then says
plainly that this is untested - *"that's the shape to try ... and re-verify against that version"*.
That hedge was read as a version note and not as the speculation it was.

**The compile gate settled it in one run.** Declaring all eight releases and probing the candidates:

    ScheduleField.SetFormula              does not exist on 2020 through 2027
    ScheduleField.Formula                 no such property
    ScheduleDefinition.AddCalculatedField no such method
    ScheduleDefinition.AddCalculatedParameter  no such method

`ScheduleDefinition.AddField(ScheduleFieldType.Formula)` **does** compile on every release, which is
the trap: a formula field can be added and there is no way to give it a formula. Reading the shipped
assembly confirms the shape - every `Calculated*` member on the schedule side is a **Get**
(`GetCalculatedValueName`, `GetCalculatedValueText`, `GetCellCalculatedValue`). **A calculated column
can be read and cannot be authored.**

**The lesson is the one this repository already had, arriving from a new direction.** *"Impossible on
version X"* recorded without naming the version becomes a lie the day another Revit is installed -
that is why the source library re-checked its own purge note. This is the mirror: **a POSSIBILITY
recorded without a version, on a call nobody ran, is equally a lie**, and it is more dangerous
because it sends somebody off to build something that cannot exist. The four probe lines that settled
it cost less than writing the fragment's purpose would have.

**The arithmetic closes at:** 312 covered + **5** impossible + **13** missing (all 13 now built) =
**330**.

**What the refuted five have in common is worth more than the five.** Four of them sit *inside* a
fragment that covers the neighbouring case and says so in its own purpose - "not a formula column",
"deliberately not here", "linear along a direction". **A fragment that names its own boundary is the
best gap detector in this repository, and reading those sentences is cheaper than reading the source
library.** The fifth was found by a name collision rather than a boundary: two different jobs both
called "show elements".

**One method was tried and thrown away, and that is worth recording so nobody repeats it.** Matching
source file names against fragment vocabulary looked promising and does not work: six of the nine
*already-known* gaps score 0.67-1.00 against unrelated fragments - `create-dimension.cs` matched
`REPORT_GLOBAL_PARAMETERS` at 1.00. **A tool that cannot find the gaps you already know cannot be
trusted to find the ones you do not**, and reporting its silence as reassurance would have been worse
than not running it.

A cross-check on 2026-09-06 raised confidence without closing it:

- **Matching source file names against fragment vocabulary does not work, and was abandoned rather
  than reported.** Six of the nine KNOWN gaps score 0.67 to 1.00 against unrelated fragments —
  `create-dimension.cs` matched `REPORT_GLOBAL_PARAMETERS` at 1.00. Name similarity separates
  nothing, and a tool that cannot find the gaps already known cannot be trusted to find new ones.
- **About fifteen of the most suspicious files were then read by hand, and no new gap was found.**
  The two likeliest turned out to be covered well: `filter-by-element-intersection` by
  `SELECT_TOUCHING`, which carries the same connector scar; `filter-by-solid-intersection` by
  `SELECT_IN_REGION`, which deliberately folded the fast and exact tests into one flag.

Fifteen of 317 is a sample, not a proof. **Closing this needs the covered claims put to a refuter the
way the missing ones were**, and until that runs the honest statement is *nine were found and nine
are built*, never *nine were all there were*.

**Why this needed doing at all.** [`HANDOVER.md`](HANDOVER.md) had recorded the library as
**EXHAUSTED of fragment-shaped work**. That was an estimate standing in for a count: roughly 60 were
judged worth adding, 55 were built, and 60 minus 55 was treated as "finished". **Nobody read the
remaining files.** The denominators were right; the conclusion was not.

**The lesson is the one this repository keeps relearning.** A number derived by subtraction is not a
measurement. `tools/check-gaps.py` exists because counts asserted by hand drift; this is the same
failure one level up — a *conclusion* asserted by hand, about work nobody had looked at.

**The shape confirms [D-29](DECISIONS.md) rather than merely agreeing with it.** That library
independently arrived at filter + action + recipe — the same three kinds, for the same reason: most
requests split into *which elements* and *what to do to them*, and the ones that genuinely cannot be
split are named separately so they cannot pretend otherwise.

**It also has two kinds Heron does not**, and that is a finding rather than a gap to copy:
**creators** (36 — elements that do not exist yet and must be made) and **context** (12 — what is open,
which view, which document).

**Neither gets a new `kind` in Heron, on today's reading.** A creator's contract is
`provides: elements` — the same as a filter's. What differs is that it *writes*, and that is already
carried by `risk: MODIFY` rather than needing a fourth kind; `kind` answers *how does this compose*,
`risk` answers *what may it do*, and collapsing the two would make both worse. Context reads the
session rather than elements, which is a filter whose `provides` is a document or a view.

This is recorded as a **judgement, not a decision**, and deliberately: nothing here has written a
creator yet. Revisit it when the first one is re-authored and the contract is in front of you — that is
the same *look first, decide second* that settled four questions on 2026-08-28 by reading a system
already doing the job. **If a real creator does not fit, this paragraph is wrong and the fix is a
decision entry, not a quiet edit.**

---

## 2. What travels, and what cannot

This is the part that costs the most and is easiest to get wrong.

| | Travels | Why |
|---|---|---|
| **The mechanism** | ✅ | *There is no single "get an element's level" API; a wall stores it one way and an MEP curve another* is a fact about Revit. Facts are not owned |
| **The scar** | ✅ | *Omitting one parameter made a level filter match zero elements and report success.* This is the most valuable thing in any library and the least visible |
| **The shape of a good proof** | ✅ | Already taken: [D-30](DECISIONS.md)'s negative case came from reading proofs there that record what came back **empty** |
| **The code** | ❌ | Never. Written fresh here, in Heron's shape, against Heron's contract |
| **The words** | ❌ | Never. Heron's reasoning in Heron's sentences, or the note becomes unreadable the day the other library stops being used |
| **The names** | ❌ | No file names, folder names, tool names or branding come across |
| **The proof** | ❌ | **A fragment verified there arrives here `DRAFT`.** It is different code; a proof of the other is not a proof of this. [D-25](DECISIONS.md) says so explicitly |
| **The authority** | ❌ | *"Go and read that other repository"* is worthless the day it is retired. State the reasoning here, in full, so it stands alone |

> **The consequence, stated plainly because it is expensive:** 221 verified fragments there become 221
> **unproven** fragments here, each owing its own proof with a negative case against a real model. Study
> is cheap. Re-authoring is moderate. **Proving is the expensive part and it is gated on the owner's
> machine.** Any plan that ignores that arithmetic is planning to arrive with several hundred DRAFT
> fragments and no way to trust one of them.

---

## 3. The method — the owner's three rules, made operational

For each candidate, in order. **Stop at the first rule that applies; do not run all three.**

### Rule 0 — Does it earn a place at all?

Asked first, because the cheapest fragment is the one not written.

- **Does Heron already cover it?** `python brain/heron_fragment.py` lists what exists. Two fragments
  answering one sentence is the duplication [D-29](DECISIONS.md) exists to prevent.
- **Is the job one the owner actually does?** The 12 skills are the evidence for that, not the fragment
  count. **A large library does not know which of its own entries matter**, and cannot, unless something
  recorded every real run from the beginning — which is not a criticism of any library, it is what
  happens when usage tracking is added after the entries. Heron records usage from its first day
  precisely so it never has to guess this about itself, and **re-authoring everything would import the
  guesswork along with the fragments.** Take the jobs that are done; let the rest wait for a request.
- If neither, **skip it and record nothing.** A library of everything is a library nobody searches.

### Rule 1 — Check and edit

The default path. Read the fragment for **what it knows**, then write Heron's own from that
understanding:

1. **Read the whole file, comments first.** The comments carry the scars; the code carries the result.
   A comment saying *"this one matters for MEP — the other four are not present at all"* is worth more
   than the four lines beneath it.
2. **State the contract before writing any code** — `needs` and `provides`, named and typed. If that
   cannot be stated cleanly, the fragment is doing two jobs and Rule 3 applies.
3. **Write it fresh against that contract.** Not a transcription with the names changed.
4. **Carry the knowledge into Heron's own words**, including *why*. A fix whose reason is not recorded
   gets removed by the next person who thinks it looks redundant.
5. **Name it for what it really does, to Heron's standard** — [29 §2](29-metadata-standard.md): a
   `FRG-<AREA>-<NNN>` id that carries no name and no kind, a verb-first `SCREAMING_SNAKE` capability, and
   a folder derived from that capability rather than invented. **The name a fragment had where it was
   read carries no weight here** — it was named for that library's folders, and half of what makes this
   one searchable is that every name is built the same way.
6. **Set `heron-status: DRAFT` and write the proof's cases without the proof.** State what the positive
   and negative cases must be, so whoever reaches a Revit knows exactly what to run.

> ### ⚠️ The check that makes Rule 1 real — and it failed the first time it was needed
>
> **On 2026-08-28, the first fragment re-authored under this document was copied, not re-authored.**
> The level lookup came across essentially line for line, with one variable renamed, and the commit
> claimed it had been "written fresh". Nobody caught it; the owner asked *"did you take anything?"* and
> the answer only became clear when the two files were put side by side.
>
> **So put them side by side. Every time, before committing.** Not from memory — memory is what produced
> the wrong claim. If the two read the same line for line, it was copied, whatever the intention was.
>
> **What is genuinely unavoidable, and is not copying:** the *facts*. Five `BuiltInParameter` names in
> the order they must be tried IS the knowledge — there is no second correct set, and naming the same
> Revit constants is not taking anything. Two lines of Revit API with one correct form will look alike
> in any two codebases.
>
> **What is not unavoidable:** the structure, the control flow, the naming, the comments, and above all
> **the design decisions**. That is where re-authoring actually happens. The rewritten version of that
> fragment stores the parameter order as a **list** rather than a chain of `??`, and **counts and
> reports the elements whose level it could not resolve at all** instead of silently dropping them —
> so a broken lookup reads as *"12 found, 12 with no level"* rather than as a plausible zero. Neither
> decision came across; both are Heron's, and the second is the better answer to the very failure the
> original fragment was written to record.
>
> **The honest test is not "does it look different."** It is: *can I say what I decided differently, and
> why?* If the answer is nothing, no re-authoring happened.

### Rule 2 — If you want to add, add

Heron's version may be **better than what was read**, and where it is, that is the point of re-authoring
rather than importing:

- **Add what the contract needs.** Heron's fragments declare their inputs and outputs; many read
  fragments have inputs as edit-me-first literals at the top of a file.
- **Add the version range explicitly.** `revit: [...]` as a list — never a range that claims releases
  nobody has tried ([D-05](DECISIONS.md)).
- **Add a negative case to the test file even when the original had none.** [D-30](DECISIONS.md) is not
  optional here and a fragment that cannot state one is not yet understood.
- **Add nothing speculative.** An input nobody asked for is a parameter to maintain and test forever.

### Rule 3 — If you want to split, split

Where one read fragment is really two jobs joined:

- **Split when the extracted part has at least two real consumers**, actual or clearly imminent
  ([09 §6](09-skills-and-fragments.md)). Reuse justifies a split; tidiness does not.
- **The reliable signal is the contract.** A fragment whose `provides` list has two unrelated entries is
  usually two fragments — one filter and one action that were written together because they were needed
  together once.
- **The opposite is equally real.** Over-decomposition — ten three-line fragments behind nine layers of
  indirection — is worse than one clear fragment, and it passes every check a tool can run.

---

## 4. What this is NOT

> ### ⚠️ The scope rule in this section was REVISED on 2026-08-30 — see [D-45](DECISIONS.md)
>
> This section used to say *"most of the 398 will correctly never be made"* and *"not a race to a
> number"*, and Rule 0 above used to ask whether a job is one the owner actually does. **The owner
> decided otherwise**: the library is re-authored **in full** now, and proved in one concentrated Revit
> pass later, because checking costs little per fragment and a great deal per sitting.
>
> **What that changes:** how many. **What it does not change:** anything else on this page. Rules 0–3
> are untouched and still binding — studied and re-authored, never copied; the contract stated before
> the code; a negative case in every proof. Rule 0 still asks **does Heron already cover it**, because
> duplication is still the thing to avoid; it no longer asks whether the job is one somebody has
> requested.
>
> D-45 also carries a condition that belongs here: **a mechanism more than one fragment needs is written
> once and composed**, not copied. That is Rule 3 applied up front. It is what keeps a single
> misunderstood mechanism from becoming eighty fixes on the first day at the machine.

- **Not a migration.** There is no batch, no importer and no mapping file. Every fragment is a separate
  decision — a separate act of understanding, which is what D-45 leaves untouched even as it asks for
  all of them.
- **Not a race to a number.** *"Heron has 398 fragments"* would be a claim about a folder. Ten fragments
  that are proven, composable and used beat three hundred that are none of those.
- **Not one-directional.** A mechanism understood well enough to re-author is usually understood well
  enough to improve, and the improvement stays here — the other library is **read-only**, permanently
  ([HANDOVER §7](HANDOVER.md)).

---

## 5. Where it happens in the build

[Step 14](27-build-order.md) is the first ten, and it is the last step of Phase 2 because the machinery
has to exist before a fragment can be stored, found, composed or trusted. **After Phase 2 this becomes a
standing activity rather than a step** — the library grows when a real job needs something, which is the
only signal that has ever been worth building from.
