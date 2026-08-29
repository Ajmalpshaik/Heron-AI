# brain/ — Part 3, Heron Brain

**Knowledge.** Runs outside Revit. **Phase 2 started 2026-08-28 — Steps 7 to 12 done, 6 of its 8.**

| | |
|---|---|
| Language | Python |
| Runs | outside Revit |
| Status | **being built** — [docs/27](../docs/27-build-order.md), Steps 7 to 14 |

## What is here now

| | |
|---|---|
| [`heron_fragment.py`](heron_fragment.py) | **Step 7.** What a fragment IS on disk, and the validator that will not let it lie. Identity is not the filename; the contract is data, not prose; a proof without a negative case is refused |
| [`fragments/`](fragments/) | The library. Two so far, both `DRAFT` — written by hand to prove the shape, and **neither has met a real model** |
| [`heron_search.py`](heron_search.py) | **Step 9.** Finding a fragment by exact words. Three routes and it says which answered: `identity` (one lookup, no search), `cache` (this wording was resolved before), `keywords` (FTS5, ranked). Only a **PROVEN** fragment may run off an exact match without asking |
| [`heron_capability.py`](heron_capability.py) | **Step 12.** Ask for *what you want done*, never for *who does it*. Add a provider, retire one, split one into three — **no call site changes**. Almost all of it is **derived** from the fragments (D-40), including risk, which already has two homes and must not gain a third |
| [`heron_retrieve.py`](heron_retrieve.py) | **Step 11.** The whole lookup: a structured filter **first**, then keywords and nearness over the survivors, fused by reciprocal rank. **The Revit version filter is a wall** — an incompatible fragment is not demoted, it is absent — and what was excluded is reported with its reason |
| [`heron_embed.py`](heron_embed.py) | **Step 10.** Finding a fragment by something other than its exact words. **Two backends**: `lexical` is built in, offline, needs nothing installed — and is measured, in numbers, as *not meaning*; `model` is a trained encoder used when one is present, and **has never run** (`A7`). Content-hashed, so re-indexing unchanged files costs nothing |
| [`heron_scope.py`](heron_scope.py) | **Step 8.** One knowledge store per scope, as one file each. A cross-scope query is impossible to *write*: the API takes one scope and `ATTACH` is refused by name. The stores are **derived** — delete them all and `--rebuild` puts them back |

Run `python brain/heron_fragment.py` to validate the library and
`python brain/heron_scope.py` to see the stores, and
`python brain/heron_retrieve.py "select all ducts" --revit 2024` to look something up.
`test_fragment_store.py`, `test_scope_store.py` and `test_search.py` are the checks behind them.

## What is still to come

Steps 13 and 14: the dependency graph and the exact-match short circuit · local
embeddings · hybrid retrieval · the capability registry · the dependency graph · ten real skills.

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
