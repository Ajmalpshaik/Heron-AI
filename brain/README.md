# brain/ — Part 3, Heron Brain

**Knowledge.** Runs outside Revit. **Not built yet — Phase 2.**

| | |
|---|---|
| Language | Python |
| Runs | outside Revit |
| Status | empty by design — see [docs/ROADMAP.md](../docs/ROADMAP.md) |

## What will be here

RAG and retrieval · the vector store · fragments · skills · the capability registry ·
memory scopes · the trust model · learning.

## Why it's empty

[docs/27](../docs/27-build-order.md) builds one thin vertical slice first. Until the bridge works and
Heron can read a model, a knowledge system has nothing to be knowledgeable *about* — and its contracts
would be written against assumptions rather than experience.

The field notes are the evidence for that: the stale-name trap, the active-document hazard and the
connect-time snapshot were all found by **running** the software, not by specifying it.

## Rules for this folder

1. **Never references Revit.** Not the API, not the add-in. It talks to `mcp/`.
2. **The vector index is derived, never authoritative.** Deleting it must always be a safe recovery
   action. [Golden Rule 11](../docs/14-golden-rules.md)
3. **One store per knowledge scope**, so a cross-project query is impossible by construction rather
   than merely discouraged. [Golden Rule 5](../docs/14-golden-rules.md)
