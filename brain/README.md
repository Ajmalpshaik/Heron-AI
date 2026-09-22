# brain/ — Part 3, Heron Brain

**Knowledge.** Runs outside Revit. **All eight Phase 2 steps are built — 7 to 14. None of it is proven.**

**And since 2026-08-29 it is reachable.** Everything here was imported by nothing but its own tests until
[`mcp/server/heron_brain.py`](../mcp/server/heron_brain.py) was written — complete, tested, and invisible
to any conversation. MCP tools now stand on that seam — `python mcp/server/heron_tools.py` lists every
one of them and marks the only one that can change a model. **Do not trust a count typed here**: this
line said *four* and named them, and by the time `heron_check` and `heron_standards` were added it was
a stale description of which surfaces a caller can reach, which is the one kind of list worth deriving.
**They ask for a capability and never for a fragment**, which is what keeps everything in here
replaceable.

**Running one is a bridge operation rather than an MCP tool**, and the two are not the same door.
[D-28](../docs/DECISIONS.md)'s in-process Roslyn landed on 2026-09-06 as
[`RevitFragment.cs`](../revit/Heron.Revit.Addin/RevitFragment.cs). `run_fragment_read` opens no
transaction, so Revit itself refuses any change. **`run_fragment_write` exists too** — declared `MODIFY`
in [`HeronOperationRegistry`](../platform/Heron.Core/HeronOperationRegistry.cs), gated by
`write.enabled` which defaults to false ([D-19](../docs/DECISIONS.md)), and rolled back unless the
caller passes `apply` ([D-55](../docs/DECISIONS.md)).

> This paragraph said *"running a fragment that WRITES is a separate operation that still does not
> exist"* until 2026-09-09. It did exist by then, and had for a day. Found while wiring `heron_context`
> onto the same seam — which is the pattern: a sentence about a gap has to be corrected when the gap
> closes, or it becomes the most convincing wrong documentation in the repository ([D-54](../docs/DECISIONS.md)).

| | |
|---|---|
| Language | Python |
| Runs | outside Revit |
| Status | **being built** — [docs/27](../docs/27-build-order.md), Steps 7 to 14 |

## What is here now

| | |
|---|---|
| [`heron_fragment.py`](heron_fragment.py) | **Step 7.** What a fragment IS on disk, and the validator that will not let it lie. Identity is not the filename; the contract is data, not prose; a proof without a negative case is refused |
| [`fragments/`](fragments/) | The library. **No count here** — this row carried *360, 193 `DRAFT`, 167 `PROVEN`* until 2026-09-21, the fourth copy of that pair to go stale, in a table headed *what is here now* ([row 5b-57](../docs/FRAGMENT-ISSUES.md)). Take them from the tools instead: `python brain/heron_fragment.py` counts and validates them from disk, and it is right where this line has gone stale. This row once said *thirty-two, every one DRAFT*, and stayed saying it for a week after neither half was true. Every one **compiles**, on all eight releases, via [`tools/check-fragments-compile.py`](../tools/check-fragments-compile.py). Its first run found one that never could have: `FRG-QA-001` had a value called `checked`, a reserved C# keyword |
| [`heron_search.py`](heron_search.py) | **Step 9.** Finding a fragment by exact words. Three routes and it says which answered: `identity` (one lookup, no search), `cache` (this wording was resolved before), `keywords` (FTS5, ranked). Only a **PROVEN** fragment may run off an exact match without asking |
| [`heron_skill.py`](heron_skill.py) · [`skills/`](skills/) | **Step 14.** What the user can *ask for*, in their own words. A skill names **capabilities, never fragments** — so a fragment can be replaced without editing a skill, and a skill can be written **before** the fragment that will serve it. Ten of them, all `DRAFT` |
| [`heron_graph.py`](heron_graph.py) | **Step 13.** *What breaks if this changes.* Every edge but one is **computed from the fragments on demand** (D-40) — the only stored edge is a skill's requirement, which no artifact underneath carries. Names the dangerous case out loud: a **sole provider**, because whatever asked for its capability never named it |
| [`heron_capability.py`](heron_capability.py) | **Step 12.** Ask for *what you want done*, never for *who does it*. Add a provider, retire one, split one into three — **no call site changes**. Almost all of it is **derived** from the fragments (D-40), including risk, which already has two homes and must not gain a third |
| [`heron_retrieve.py`](heron_retrieve.py) | **Step 11.** The whole lookup: a structured filter **first**, then keywords and nearness over the survivors, fused by reciprocal rank. **The Revit version filter is a wall** — an incompatible fragment is not demoted, it is absent — and what was excluded is reported with its reason |
| [`heron_rerank.py`](heron_rerank.py) | **Stage 7 of the RAG plan.** Reads the top ~20 **(question, passage) pairs together**, which neither retrieval route can do — and it is the largest optional piece in Heron, so `python brain/heron_rerank.py` names the package, the **500 MB – 2 GB** and the `--user` install **before** anything downloads (R-77, [D-01](../docs/DECISIONS.md)). **With nothing installed the shortlist comes back in exactly fusion's order and says so** — `Re-rank: absent` from the retrieval command, and a `re-rank` line on every `heron_lookup` through the MCP seam, beside which nearness backend answered — measured at 360 fragments as byte-identical to the recorded before, and costing 0.05 % of a query. **No cross-encoder has ever run here**: the weights need `huggingface.co`, so the before-and-after Stage 7 asks for is owed, and `A10` in [NEEDS-CHECKING.md](../docs/NEEDS-CHECKING.md) is that run |
| [`retrieval-history.md`](retrieval-history.md) | **What was measured, and at what library size.** Quote this file, never a remembered figure — the owner's earlier library once had three accuracy numbers in circulation at once because the early scores recorded no corpus size. It records the built-in backend **sinking in proportion as the library grows**, and since 2026-09-11 it also records **both backends measured at the same library size** — which is the comparison `A7` was opened to get, and `A7` is closed |
| [`heron_embed.py`](heron_embed.py) | **Step 10.** Finding a fragment by something other than its exact words. **Two backends**: `lexical` is built in, offline, needs nothing installed — and is measured, in numbers, as *not meaning*; `model` is a trained encoder **used when one is present** — it ran on 2026-09-10 at 360 fragments and put the tracked duct filter **19th where the built-in one puts it 194th** ([`retrieval-history.md`](retrieval-history.md)), and it loads **only where the weights can be fetched**, so a container that blocks `huggingface.co` gets `lexical` and is told so. Content-hashed, so re-indexing unchanged files costs nothing — and since Stage 6 `--refresh` **fires** that check: a changed file is re-ingested and **retires its predecessor, naming it**, a moved file **deletes nothing**, and a clause byte-identical to one already held is reported at write time |
| [`heron_scope.py`](heron_scope.py) | **Step 8.** One knowledge store per scope, as one file each. A cross-scope query is impossible to *write*: the API takes one scope and `ATTACH` is refused by name. The stores are **derived** — delete them all and `--rebuild` puts them back |
| [`heron_context.py`](heron_context.py) | **The Context Manager** — [docs/19 §1–§2](../docs/19-context-and-cost.md), specified and never implemented until 2026-09-09 ([32 §4.1](../docs/32-master-architecture-reconciliation.md)). Four paths, each with a **declared list of parts it may carry**, and a part outside it **raises** — docs/19: *exceeding a budget is a bug in retrieval, not a reason to raise the budget*. The budget is a parts list rather than a token count because Heron has no tokeniser and the host counts tokens ([D-58](../docs/DECISIONS.md)); size is reported and never enforced. **The request crosses byte for byte** — `OST_DuctCurves` is the load-bearing half of a BIM sentence and is what a compressor damages first, and since 2026-09-09 that is a **branch** (`FULL_ONLY`) rather than a promise. **Depth** — `abstract` / `overview` / `full` — says how much of each part is carried, taking a generation packet from **5,737 characters to 372** with the request unchanged; a part that lost something says **how much**, and a part that carried all of itself says nothing extra ([34 §2.1–2.2](../docs/34-patterns-adapted.md)). **It does not classify what the user meant** ([D-01](../docs/DECISIONS.md)); an assumed path says it was assumed. A path whose source is not there is **refused by name rather than quietly degraded**, and since Stage 3 that refusal **narrows** rather than softening: `STANDARDS` says *no document is indexed*, *ingested but not indexed*, or *nothing indexed covers this* — three different sentences, and none of them is an answer. It carries the clause's own words for grounding **separately from the label wrapped round them**, because a document titled `QCS 2014` was otherwise supplying the year 2014 as evidence |
| [`heron_validate.py`](heron_validate.py) | **Step 17.** The Fragment Validation Agent. It gathers the evidence for a proof and **never signs one** — a person does, because an agent that can stamp 193 fragments is the fastest machine ever built for making an unproven claim look proven ([D-30](../docs/DECISIONS.md)). Its drafts land in [`proof-drafts/`](proof-drafts/) |
| [`heron_matrix.py`](heron_matrix.py) | **Step 17.** The Compatibility Matrix Agent — every fragment against every Revit release, so a release-specific hole is visible before somebody finds it in a model |
| [`heron_gaps.py`](heron_gaps.py) | **Step 17.** The Capability Gap Agent — what Heron was asked for and could not do. Not the same as `tools/check-gaps.py`, which reports what is unfinished in the repository |
| [`heron_ingest.py`](heron_ingest.py) | **Stage 1 of the RAG plan.** The half of the knowledge store that did not exist — a `documents` and a `chunks` table, and a document that can go into **one** scope. The file is **pointed at, never copied**; `.rvt` and `.rfa` are refused **by extension before the file is opened** ([D-26](../docs/DECISIONS.md)); a rule and its exception are never split into two chunks, and no exact token is ever cut. Every chunk is marked `untrusted` — [Golden Rule 19](../docs/14-golden-rules.md), and this module is the day that starts mattering. Since Stage 2 a clause **comes back out**, in its own labelled answer beside the fragments — never fused with them, and **never behind the Revit version wall**, because a clause declares no release and running it through that filter would delete every document the moment a question names one |
| [`heron_ground.py`](heron_ground.py) | **Stage 3 of the RAG plan.** Checks a proposed answer against the clauses it cites, and **flags rather than rewrites** — the brain cannot write the answer ([D-01](../docs/DECISIONS.md)), so it must not repair one either. **No model, no network**: `difflib` and `re`. A claim that says LESS than its source **passes** — understating is not fabricating, and a checker that got that wrong would be switched off in a week. An **added fact** is a flag whatever the similarity says; the similarity ratio was measured against true paraphrases and wrong claims, could not separate them, and is reported rather than enforced |
| [`heron_conflict.py`](heron_conflict.py) | **Stage 8 of the RAG plan.** Two scopes answering one question with **different numbers** now say so — company `30mm` against project `40mm` at clause 3.1, the case Stage 4 left sitting. **It surfaces and settles nothing** (R-24): both clauses still come back under their own scopes, the [docs/20 §2](../docs/20-knowledge-trust-and-conflict.md) hierarchy is named and **explicitly not applied**, and a test asserts the module has no `winner` and no `resolve`. **What crosses the scope wall is a number and a clause number, never a clause** — one store open at a time (Golden Rule 5). It compares **units, not subjects**, so it cannot tell two clauses are the same requirement, and it inherits retrieval's missing floor — both said in the report rather than left to be found |
| [`heron_research.py`](heron_research.py) | **Stage 9, the last one, and it never fetches.** The Research agent's name suggests reaching outside; what it actually does is say what Heron's own scopes did **not** answer, and what an outside answer must carry. [D-01](../docs/DECISIONS.md) puts every model call in the host because Heron is a per-user install with no admin rights and no server — **the network is the same boundary**, and the offline premise is explicit: *site visits, locked-down networks, a laptop on a plane*. A test asserts the **absence** of a fetch. The other half checks an external answer's **citations**: nothing cited is `UNCITED` ([05 §8](../docs/05-heron-brain.md) — a bug, not low confidence), a citation nobody could look up is `VAGUE` and says which of document / edition / locator is missing, and all three is `WELL_FORMED` — **still `UNVERIFIED`, because Heron has not read the source.** The only route out of `UNVERIFIED` is ingesting the document it cites, which is one command and is in every brief |
| [`heron_audit.py`](heron_audit.py) | **Step 17.** The brain's half of the audit trail. The add-in records what reached the model; this records what the brain did, so a request answered entirely here still leaves a trace ([D-62](../docs/DECISIONS.md)) |

```bash
python brain/heron_fragment.py                              # validate the library
python brain/heron_skill.py                                 # the skills, and the capability gaps
python brain/heron_retrieve.py "select all ducts" --revit 2024
python brain/heron_graph.py FRG-ELE-001                     # what breaks if this changes
python tools/check-gaps.py                                  # unfinished, versus only waiting
```

## What is still to come

**Proof, mostly.** **316 fragments are `PROVEN`; the other 79 and all ten skills are `DRAFT`** — derive
both with `python brain/heron_fragment.py` rather than reading them here. **This line said 167 and 193
until 2026-09-19**, which was wrong by 149 in one direction and 114 in the other and had been for weeks:
the sentence telling its reader to derive the numbers was itself the reason nobody did. Phase 2's
definition of done is *"ten real skills **work**"* — ten are written, and the word that needs a Revit is
still the last one. The first 52 are what one night with a real model bought; the arithmetic on the rest has
not changed, only the size of it - and the ten fragments added on 2026-09-08, four for
switching project and view and six for the review's N01 to N06, arrived `DRAFT` like
everything else.

**The WRITE path exists now.** Reading a model through a fragment works, and so does changing one:
`run_fragment_write` is a **separate** operation from the read, deliberately — `MODIFY` in the
registry, wrapping the run in a `TransactionGroup` assimilated only on `apply=true` and rolled back
otherwise, so a preview is the run itself undone rather than a simulation that could lie
([D-55](../docs/DECISIONS.md)). **164 `MODIFY` fragments are `PROVEN`** — derive that with
`heron_fragment.py` — so this is built *and* met a model. `write.enabled` still defaults to `false`
until a real Revit has been through [NEEDS-CHECKING.md](../docs/NEEDS-CHECKING.md). So a request resolves to *this capability,
provided by that fragment*, and can be READ all the way through — and no answer here may imply more
than that.

**The queue the skills produced has been worked.** Writing the skills first ordered it by real demand
rather than by guessing, and on 2026-08-29 all seven were written — so `python brain/heron_skill.py` now
prints **no gaps** and all ten skills have every capability provided. What that bought is a shorter list
of *kinds* of outstanding work, not less of it: the seven are `DRAFT` like the rest, and every one is
waiting on the same machine.

## Why it stayed empty until now

[docs/27](../docs/27-build-order.md) builds one thin vertical slice first. Until the bridge works and
Heron can read a model, a knowledge system has nothing to be knowledgeable *about* — and its contracts
would be written against assumptions rather than experience.

The field notes are the evidence for that: the stale-name trap, the active-document hazard and the
connect-time snapshot were all found by **running** the software, not by specifying it. Step 7 made the
same point on its first day: the `AMBIENT` set in `heron_fragment.py` exists because writing two real
fragments showed that a filter and the action consuming it read as *non-composable* — nothing upstream
provides a `uidoc`. No amount of designing the contract in the abstract produced that; ten minutes of
writing two fragments did.

## Dependencies

`brain/` may have them; **[`mcp/client/`](../mcp/client/) may not.** That rule is about the bridge
client, which has to run on a locked-down machine with nothing installed on it, and conflating the two
layers would cost this one a great deal for no benefit.

What this layer needs must still install **per-user with no administrator rights** — that is
[D-01](../docs/DECISIONS.md)'s promise and Phase 0 proved it end to end on a real machine.

**The list is not here.** It is [`requirements.txt`](../requirements.txt) and
[`requirements-optional.txt`](../requirements-optional.txt), where pip can read it, and this table used
to be a second copy that said `pyyaml` while the code imported six things. Ask the machine instead:

```bash
python tools/check-dependencies.py       # present, missing, and what each one buys
pip install --user -r requirements.txt   # the required half - one package
```

**Do not install the optional half in one command.** One of them is large enough that a person is
entitled to know first — and the figure is **not written here**, because the code that performs the
download owns it: `heron_rerank.announcement()` prints the size before any network call rather than
during, and `check-dependencies.py` above reads it from there. A size copied into a README is a size
that goes stale in the one place it had to be right.

Every optional package degrades rather than breaks: Heron answers without all five and names the one
that did not run.

**What is still owed here, so nobody reads the above as more than it is:** nothing installs
automatically ([R-74](../docs/work-notes/plans/rag/01-requirements.md), R-77 — `tools/setup.ps1` still
deploys the add-in and no Python package), and *installed* above means *importable*, not *the right
version* (R-78).

## Rules for this folder

1. **Never references Revit.** Not the API, not the add-in. It talks to `mcp/`.
2. **The vector index is derived, never authoritative.** Deleting it must always be a safe recovery
   action. [Golden Rule 11](../docs/14-golden-rules.md)
3. **One store per knowledge scope**, so a cross-project query is impossible by construction rather
   than merely discouraged. [Golden Rule 5](../docs/14-golden-rules.md)
