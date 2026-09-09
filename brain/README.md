# brain/ — Part 3, Heron Brain

**Knowledge.** Runs outside Revit. **All eight Phase 2 steps are built — 7 to 14. None of it is proven.**

**And since 2026-08-29 it is reachable.** Everything here was imported by nothing but its own tests until
[`mcp/server/heron_brain.py`](../mcp/server/heron_brain.py) was written — complete, tested, and invisible
to any conversation. **Four** MCP tools now stand on that seam: `heron_capabilities`, `heron_resolve`,
`heron_lookup` and, since 2026-09-09, `heron_context`. **They ask for a capability and never for a
fragment**, which is what keeps everything in here replaceable.

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
| [`fragments/`](fragments/) | The library. **360 as of 2026-09-08 — 308 `DRAFT`, 52 `PROVEN`.** Do not trust those three numbers over the tools: `python brain/heron_fragment.py` counts and validates them from disk, and it is right where this line has gone stale. This row once said *thirty-two, every one DRAFT*, and stayed saying it for a week after neither half was true. Every one **compiles**, on all eight releases, via [`tools/check-fragments-compile.py`](../tools/check-fragments-compile.py). Its first run found one that never could have: `FRG-QA-001` had a value called `checked`, a reserved C# keyword |
| [`heron_search.py`](heron_search.py) | **Step 9.** Finding a fragment by exact words. Three routes and it says which answered: `identity` (one lookup, no search), `cache` (this wording was resolved before), `keywords` (FTS5, ranked). Only a **PROVEN** fragment may run off an exact match without asking |
| [`heron_skill.py`](heron_skill.py) · [`skills/`](skills/) | **Step 14.** What the user can *ask for*, in their own words. A skill names **capabilities, never fragments** — so a fragment can be replaced without editing a skill, and a skill can be written **before** the fragment that will serve it. Ten of them, all `DRAFT` |
| [`heron_graph.py`](heron_graph.py) | **Step 13.** *What breaks if this changes.* Every edge but one is **computed from the fragments on demand** (D-40) — the only stored edge is a skill's requirement, which no artifact underneath carries. Names the dangerous case out loud: a **sole provider**, because whatever asked for its capability never named it |
| [`heron_capability.py`](heron_capability.py) | **Step 12.** Ask for *what you want done*, never for *who does it*. Add a provider, retire one, split one into three — **no call site changes**. Almost all of it is **derived** from the fragments (D-40), including risk, which already has two homes and must not gain a third |
| [`heron_retrieve.py`](heron_retrieve.py) | **Step 11.** The whole lookup: a structured filter **first**, then keywords and nearness over the survivors, fused by reciprocal rank. **The Revit version filter is a wall** — an incompatible fragment is not demoted, it is absent — and what was excluded is reported with its reason |
| [`retrieval-history.md`](retrieval-history.md) | **What was measured, and at what library size.** Quote this file, never a remembered figure — the owner's earlier library once had three accuracy numbers in circulation at once because the early scores recorded no corpus size. It currently records the built-in backend **collapsing as the library grows**, which is the evidence for `A7` |
| [`heron_embed.py`](heron_embed.py) | **Step 10.** Finding a fragment by something other than its exact words. **Two backends**: `lexical` is built in, offline, needs nothing installed — and is measured, in numbers, as *not meaning*; `model` is a trained encoder used when one is present, and **has never run** (`A7`). Content-hashed, so re-indexing unchanged files costs nothing |
| [`heron_scope.py`](heron_scope.py) | **Step 8.** One knowledge store per scope, as one file each. A cross-scope query is impossible to *write*: the API takes one scope and `ATTACH` is refused by name. The stores are **derived** — delete them all and `--rebuild` puts them back |
| [`heron_context.py`](heron_context.py) | **The Context Manager** — [docs/19 §1–§2](../docs/19-context-and-cost.md), specified and never implemented until 2026-09-09 ([32 §4.1](../docs/32-master-architecture-reconciliation.md)). Four paths, each with a **declared list of parts it may carry**, and a part outside it **raises** — docs/19: *exceeding a budget is a bug in retrieval, not a reason to raise the budget*. The budget is a parts list rather than a token count because Heron has no tokeniser and the host counts tokens ([D-58](../docs/DECISIONS.md)); size is reported and never enforced. **The request crosses byte for byte** — `OST_DuctCurves` is the load-bearing half of a BIM sentence and is what a compressor damages first, and since 2026-09-09 that is a **branch** (`FULL_ONLY`) rather than a promise. **Depth** — `abstract` / `overview` / `full` — says how much of each part is carried, taking a generation packet from **5,737 characters to 372** with the request unchanged; a part that lost something says **how much**, and a part that carried all of itself says nothing extra ([34 §2.1–2.2](../docs/34-patterns-adapted.md)). **It does not classify what the user meant** ([D-01](../docs/DECISIONS.md)); an assumed path says it was assumed. A path whose source does not exist — `STANDARDS` wants clauses and no clause store exists — is **refused by name rather than quietly degraded** |

```bash
python brain/heron_fragment.py                              # validate the library
python brain/heron_skill.py                                 # the skills, and the capability gaps
python brain/heron_retrieve.py "select all ducts" --revit 2024
python brain/heron_graph.py FRG-ELE-001                     # what breaks if this changes
python tools/check-gaps.py                                  # unfinished, versus only waiting
```

## What is still to come

**Proof, mostly.** **52 fragments are `PROVEN`; the other 308 and all ten skills are `DRAFT`.** Phase 2's
definition of done is *"ten real skills **work**"* — ten are written, and the word that needs a Revit is
still the last one. The 52 are what one night with a real model bought; the arithmetic on the rest has
not changed, only the size of it - and the ten fragments added on 2026-09-08, four for
switching project and view and six for the review's N01 to N06, arrived `DRAFT` like
everything else.

**And a WRITE path for the executor.** Reading a model through a fragment works: the executor is built
and runs one read-only. What does not exist is running a fragment that CHANGES anything — that is a
separate operation, deliberately, and `write.enabled` defaults to `false` until a real Revit has been
through [NEEDS-CHECKING.md](../docs/NEEDS-CHECKING.md). So a request resolves to *this capability,
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

| Needed by | For |
|---|---|
| `pyyaml` | reading `fragment.yaml`. `pip install --user pyyaml` |

## Rules for this folder

1. **Never references Revit.** Not the API, not the add-in. It talks to `mcp/`.
2. **The vector index is derived, never authoritative.** Deleting it must always be a safe recovery
   action. [Golden Rule 11](../docs/14-golden-rules.md)
3. **One store per knowledge scope**, so a cross-project query is impossible by construction rather
   than merely discouraged. [Golden Rule 5](../docs/14-golden-rules.md)
