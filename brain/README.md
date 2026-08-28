# brain/ — Part 3, Heron Brain

**Knowledge.** Runs outside Revit. **Phase 2 started 2026-08-28 — Step 7 of 8 is in.**

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

Run `python brain/heron_fragment.py` to validate the library, and
`python tests/test_fragment_store.py` for the 22 checks behind it.

## What is still to come

Steps 8 to 14: the scope as a file · keyword search and the exact-match short circuit · local
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
