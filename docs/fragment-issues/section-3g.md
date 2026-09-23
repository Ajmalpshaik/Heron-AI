# Fragment issues — section 3g

> One section of [the register](../FRAGMENT-ISSUES.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## 3g. RUNNING FRAGMENTS IN BULK IS A WORKING NEED, NOT A TESTING ONE

Raised by the owner, 2026-09-09: *"maybe sometimes while we are working we need to run fragments in
bulk — is there an option?"*

**There is, and today depended on it.** `prove` runs several fragments in ONE process, ONE lease, with
D-29's chain carrying values between them:

```
prove select-by-category-name set-selection --set categoryName=Ducts --set "inViewOnly=FloorPlan: M1"
```

Two fragments, one job: the first finds 22 ducts, the second selects them. Every selection-based proof
today went through that, and the binding note on the answer says where the elements came from —
*"elements from select-by-category-name (22)"*.

### The name is the problem

**`prove` is a working command wearing a testing name.** Nobody doing real work would think to reach for
it, and that alone hides the capability the library already has. Renaming or aliasing it to `run` is
ten minutes and costs nothing.

### The real version is designed and unbuilt

| | |
|---|---|
| `HERON-KRN-WFL-007` **Workflow Engine** | *"Ordering, retries, timeouts, rollback, checkpoints, resume. Never decides…"* |
| `HERON-ORC-MAIN-001` **Orchestrator** | Understands the request, selects capability, builds and runs the work |

[docs/11](../11-orchestration-and-workflows.md) is the whole design, and its own reference workflow for
*"select all ducts"* is eleven steps long — so multi-fragment work is not an edge case in this
architecture, it is the ordinary case.

### The ORDER is already computed — asked 2026-09-09, and the answer is better than expected

*"For arranging the correct order, is there something?"*

**Yes, and it works today.** `brain/heron_graph.py` (`HERON-KRN-DEP-013`) derives the edges from the
contracts — *"fragment → fragment, from the contracts: A provides what B needs"*:

```
$ python brain/heron_graph.py FRG-SEL-001          # set-selection
  could run after it   nothing
  could run before it  FRG-SEL-002, FRG-ELE-001, FRG-MEP-030 … 50 fragments
```

It says so itself: *"Every line above was computed just now from the fragments themselves. Nothing here
is stored, so nothing here is stale."* That is [D-40](../DECISIONS.md) — an edge is derived, never written
down twice.

**So ordering is not a missing capability. It is a missing COMMAND.** The graph answers *"what can run
before this one"* for a single fragment. What a bulk runner needs is the other shape: *"here are five
fragments — what order?"*, which is a topological sort over edges that already exist.

`HERON-DEV-PLN-002` **Planning Agent — "Sequences the work"** is the registry slot for the judgement
part, and is unbuilt. But the hard half — knowing which fragment can feed which, across all 360 — is
done.

### Three different things, worth not confusing

| Thing | For | State |
|---|---|---|
| `prove` | Run several fragments now, in a line | **Works today**, misnamed |
| `tools/batch-prove.py` | Run many PROOFS and judge them | Being built by a separate session |
| Workflow Engine | Real work: retries, rollback, resume | **Designed, not built** |

### What today showed is missing, concretely

**When a chain half-succeeded there was no resume.** `select-by-category-name` would run, `set-selection`
would refuse because the category was not in that view, and the only option was to re-run both. On a
two-step chain that is trivial. On the eleven-step workflow docs/11 describes, it is not — and that is
exactly the gap `HERON-KRN-WFL-007` exists to fill.

---
