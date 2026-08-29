# brain/ — Part 3, Heron Brain

**Knowledge.** Runs outside Revit. **All eight Phase 2 steps are built — 7 to 14. None of it is proven.**

**And since 2026-08-29 it is reachable.** Everything here was imported by nothing but its own tests until
[`mcp/server/heron_brain.py`](../mcp/server/heron_brain.py) was written — complete, tested, and invisible
to any conversation. Three MCP tools now stand on that seam: `heron_capabilities`, `heron_resolve` and
`heron_lookup`. **They ask for a capability and never for a fragment**, which is what keeps everything in
here replaceable. What they still cannot do is **run** one: a fragment's C# has no route to Revit, and
[D-28](../docs/DECISIONS.md)'s in-process Roslyn is not built.

| | |
|---|---|
| Language | Python |
| Runs | outside Revit |
| Status | **being built** — [docs/27](../docs/27-build-order.md), Steps 7 to 14 |

## What is here now

| | |
|---|---|
| [`heron_fragment.py`](heron_fragment.py) | **Step 7.** What a fragment IS on disk, and the validator that will not let it lie. Identity is not the filename; the contract is data, not prose; a proof without a negative case is refused |
| [`fragments/`](fragments/) | The library. **Fourteen**, every one `DRAFT` — and **not one has met a real model**. All fourteen do now **compile**, on all eight releases, via [`tools/check-fragments-compile.py`](../tools/check-fragments-compile.py) (2026-08-29). Its first run found one that never could have: `FRG-QA-001` had a value called `checked`, a reserved C# keyword |
| [`heron_search.py`](heron_search.py) | **Step 9.** Finding a fragment by exact words. Three routes and it says which answered: `identity` (one lookup, no search), `cache` (this wording was resolved before), `keywords` (FTS5, ranked). Only a **PROVEN** fragment may run off an exact match without asking |
| [`heron_skill.py`](heron_skill.py) · [`skills/`](skills/) | **Step 14.** What the user can *ask for*, in their own words. A skill names **capabilities, never fragments** — so a fragment can be replaced without editing a skill, and a skill can be written **before** the fragment that will serve it. Ten of them, all `DRAFT` |
| [`heron_graph.py`](heron_graph.py) | **Step 13.** *What breaks if this changes.* Every edge but one is **computed from the fragments on demand** (D-40) — the only stored edge is a skill's requirement, which no artifact underneath carries. Names the dangerous case out loud: a **sole provider**, because whatever asked for its capability never named it |
| [`heron_capability.py`](heron_capability.py) | **Step 12.** Ask for *what you want done*, never for *who does it*. Add a provider, retire one, split one into three — **no call site changes**. Almost all of it is **derived** from the fragments (D-40), including risk, which already has two homes and must not gain a third |
| [`heron_retrieve.py`](heron_retrieve.py) | **Step 11.** The whole lookup: a structured filter **first**, then keywords and nearness over the survivors, fused by reciprocal rank. **The Revit version filter is a wall** — an incompatible fragment is not demoted, it is absent — and what was excluded is reported with its reason |
| [`retrieval-history.md`](retrieval-history.md) | **What was measured, and at what library size.** Quote this file, never a remembered figure — the owner's earlier library once had three accuracy numbers in circulation at once because the early scores recorded no corpus size. It currently records the built-in backend **collapsing as the library grows**, which is the evidence for `A7` |
| [`heron_embed.py`](heron_embed.py) | **Step 10.** Finding a fragment by something other than its exact words. **Two backends**: `lexical` is built in, offline, needs nothing installed — and is measured, in numbers, as *not meaning*; `model` is a trained encoder used when one is present, and **has never run** (`A7`). Content-hashed, so re-indexing unchanged files costs nothing |
| [`heron_scope.py`](heron_scope.py) | **Step 8.** One knowledge store per scope, as one file each. A cross-scope query is impossible to *write*: the API takes one scope and `ATTACH` is refused by name. The stores are **derived** — delete them all and `--rebuild` puts them back |

```bash
python brain/heron_fragment.py                              # validate the library
python brain/heron_skill.py                                 # the skills, and the capability gaps
python brain/heron_retrieve.py "select all ducts" --revit 2024
python brain/heron_graph.py FRG-ELE-001                     # what breaks if this changes
python tools/check-gaps.py                                  # unfinished, versus only waiting
```

## What is still to come

**Proof, and a way to run any of it.** Every fragment and every skill here is `DRAFT`. Phase 2's
definition of done is *"ten real skills **work**"* — ten are written, and the word that needs a Revit is
the last one.

**And an executor, which nothing has yet.** A fragment carries C# under `impl/`, the bridge speaks a
fixed set of operations, and none of them compiles one. So a request can now be resolved all the way to
*this capability, provided by that fragment* and then stop. That is not a gap in this folder — it is
[D-28](../docs/DECISIONS.md), Roslyn in-process, unbuilt — but it is the reason no answer here may imply
it can act.

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
