# 27 — Build Order

> **The one-by-one implementation sequence.** Each step is small, independently verifiable, and proves
> one specific thing. Do not start a step until the previous one works twice from a cold start.
>
> This exists because [ROADMAP](ROADMAP.md) says *what* each phase delivers, not *what to do on Monday*.

---

## The rule for every step

> **A step is done when you can demonstrate it, not when the code is written.**

Each step below has a **Prove it** line. If you cannot do that thing, the step is not finished —
regardless of how much of it is built.

---

**Which agents each step builds:** [08 — Agent Catalogue](08-agent-catalog.md).
All of Phase 0 and 1 needs **45 of the 250 agents** — under a fifth. Steps 1-6 build 10, 3, 7, 7, 6 and 9
respectively, and the large majority are plain deterministic modules with no model call at all.

---

## Step 1 — Prove the pipe *(no Revit API at all)*

**Build:** a minimal C# Revit add-in that does one thing — opens a named pipe called
`heron.{revitVersion}.{pid}`, writes `%LOCALAPPDATA%\Heron\bridges\<pid>.json`, and answers `ping` with `pong`.
Plus a throwaway Python script that reads the discovery folder and connects.

**Deliberately not in this step:** any Revit API call, any element, any document. The add-in loads and
talks. Nothing more.

**Prove it:** start Revit, run the Python script, see `pong`. Close Revit, run it again, get a clean
*"no Revit running"* — not a hang, not a stack trace.

**Why first:** it settles the whole delivery chain — does the `.addin` manifest work, does the assembly
load, is the deploy path right, does the pipe name work, does discovery work — **without needing a single
line of Revit API knowledge.** If this does not work, nothing else can, and every later bug would be
tangled up in it.

**Settles:** [Q-2](OPEN-QUESTIONS.md) transport, and the deployment path from [07 §5](07-installation-and-update.md).

---

## Step 2 — Prove the thread hop *(the hardest constraint in the platform)*

**Build:** add one `ExternalEvent`, one request queue, one `IExternalEventHandler`. Add one operation:
*count the elements in the active document.*

**Prove it:** Python asks, and a **real number** comes back from a real model. Then, with a modal dialog
open in Revit, ask again — and get a clean *"Revit is busy"* rather than a hang.

**Why second:** this is [A1](PROPOSALS.md) — the constraint absent from all four specification documents
and confirmed by the field notes. *"The AI's script runs on the same thread that draws the screen."*
Everything Heron will ever do passes through this one mechanism. Get it wrong and every later feature
inherits the mistake.

**Watch for:** never cache a `Document` across invocations · nothing blocks the main thread ·
`Raise()` is a request, not a guarantee.

**Settles:** [Q-4](OPEN-QUESTIONS.md), and validates [D-09](DECISIONS.md) in practice.

---

## Step 3 — Make it an MCP server

**Build:** wrap the Python side as a real MCP server with **one** tool, `revit_health`. Register it with
Claude Code.

**Prove it:** type *"is Revit working?"* in Claude Code and get a real answer sourced from a real Revit.

**Why third:** this is the first moment the whole chain exists — sentence → model → MCP → pipe → Revit →
back. Everything after this is adding capability to a chain that already works, which is a much easier
kind of problem than making the chain exist.

**Settles:** [D-01](DECISIONS.md) in practice.

---

## Step 4 — The first real BIM answer

**Build:** `revit_select_by_category`. Category resolution for one category — ducts.

**Prove it:** say *"select all ducts"* and watch the selection change on screen. The result names the
document: *"Selected 126 ducts in Tower-A.rvt"* — never just *"selected 126 ducts"*.

**Why fourth:** this is the Phase 0 goal, and it is the first moment Heron does something a BIM modeller
would actually want.

**Also build here:** the audit log. One entry per request, keyed by Workflow ID. It is three lines of code
now and the foundation of the cost meter, the *"what did Heron change?"* report and the Capability Gap
report later.

---

## Step 5 — More than one Revit

**Build:** discovery across several bridges, a numbered picker, and binding for the session.

**Prove it:** open Revit 2024 and Revit 2020 together. Ask for ducts. Get asked which one, answer `1`,
and have every later request go there. Close that Revit — everything stops and says so, rather than
sliding onto the other one.

**Why fifth and not first:** it needs steps 1–4 to exist before it can be tested at all. But it comes
before any write, because [the field notes](00e-field-notes-proven-bridge.md) show this is where silent
wrong-model damage begins.

**Not yet:** the lease, the `(free)`/`(in use)` column, full document pinning. Those arrive in Phase 1
with writes.

---

## ⛔ Gate — Phase 0 ends here

Everything so far is **read-only**. Heron cannot change a model yet, which is why none of it has needed
a permission gate, a transaction, an undo guarantee or a preview.

**The moment Step 6 begins, that changes**, and the safety rules stop being theory.

Do not skip ahead. A write path built before the safety path is a write path that will ship without one.

---

## Step 6 — The first write, with the safety rails

**Build, in this order, as one step:**

1. **Transaction Safety Agent** — one named `TransactionGroup` per operation, complete rollback on
   failure *([Golden Rule 16](14-golden-rules.md))*
2. **Preview** — *"This will move 247 ducts up 200 mm. 12 are owned by another user and will be
   skipped."* *([Golden Rule 17](14-golden-rules.md))*
3. **Re-count immediately before executing** the accepted preview *([Golden Rule 21](14-golden-rules.md))*
4. **Document pinning** by identity *([Golden Rule 20](14-golden-rules.md))*
5. **Permission gate in the add-in** — declared risk level per tool *([Q-5](OPEN-QUESTIONS.md), Part 4 §29)*
6. **Emergency Stop** in the ribbon
7. Then, and only then: `revit_move_elements`

**Prove it:** *"move them up 200 mm"* → preview → accept → it moves → **one Ctrl+Z puts it back exactly**.
Then force a failure mid-operation and confirm the model is untouched.

**Why the rails come before the write:** they are not features to add to a working write, they are the
conditions under which writing is allowed at all. Built after, each one becomes a retrofit through code
that already assumes it can just write.

---

## ⛔ Gate — Phase 1 ends here, and it ends UNPROVEN

Step 6 is written, compiles on all eight Revit releases, and has **never loaded into Revit**. Phase 1's
own definition of done — *one Ctrl+Z puts it back, a failure leaves the model untouched* — has not been
witnessed. [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) is that debt, in full.

**Phase 2 starts anyway, on the owner's instruction (2026-08-28):** *"checking in Revit is not possible
within 1 week, so keep the checking process as a document and start Phase 2."* The register is therefore
a **record, not a gate**. Two things follow and neither is optional:

- **`write.enabled` stays `false`.** Phase 2 is knowledge and retrieval; none of it needs to write to a
  model, and nothing below asks for that flag to move.
- **Every Phase 2 step below is provable WITHOUT Revit**, except the last. That is not a coincidence —
  the steps were ordered so that a week with no Revit is a week of real work rather than a week of
  writing code nobody can check.

---

## Phase 2 — Steps 7 to 14

**Goal:** stop hard-coding skills. Start accumulating them.

**Eight steps, one per provable thing.** [ROADMAP](ROADMAP.md) lists Phase 2 as eight items; these are
those items in dependency order, each with the one thing it proves. Nothing here existed as a step
before 2026-08-28 — the build order stopped at Step 6 and said only *"Steps 7+ follow Phase 2"*, which
is a destination and not a Monday.

| Step | Builds | Roadmap item | Needs Revit |
|---|---|---|---|
| **7** | The fragment on disk | storage format · knowledge identity | no |
| **8** | The scope as a file | SQLite one file per scope · Golden Rule 5 | no |
| **9** | Finding it by exact words | FTS5 · exact-match short circuit · utterance cache | no |
| **10** | Finding it by meaning | local embeddings · vector search | no |
| **11** | The two together | hybrid retrieval · fusion · the hard version filter | no |
| **12** | The Capability Registry | capability registry | no |
| **13** | The dependency graph | dependency graph | no |
| **14** | Ten real skills | study and re-author the existing libraries | **yes, for its proof** |

---

### Step 7 — The fragment on disk *(no database yet)*

**Build:** the on-disk shape of one fragment, and the validator that enforces it. One folder per
fragment: `fragment.yaml` carrying identity, purpose, the **contract**, version compatibility and
status; `impl/`; `tests/`; `history.jsonl`.

Three things this step settles, all of them from decisions already taken:

- **Identity is never the filename** ([09 §4](09-skills-and-fragments.md)). Rename the folder and it is
  the same knowledge object.
- **The contract is data, not prose** ([D-29](DECISIONS.md)). A fragment declares what it *needs* in
  scope and what it *provides* — so a filter that provides nothing an action needs is a defect a tool
  finds, not a defect Revit finds.
- **A proof carries a negative case** ([D-30](DECISIONS.md)). The validator refuses to record a
  `PRODUCTION` promotion whose proof has no case that should come back empty.

**Deliberately not in this step:** any database, any search, any embedding. Files and one validator.

**Prove it:** write two fragments by hand, rename both folders, and the validator still identifies them
and reports the pair's contracts as composable. Then strip the negative case out of one proof and watch
the promotion be **refused**, naming what is missing.

**Why first:** everything after this indexes fragments. Index the wrong shape and every later step
inherits it — and the shape is the one part that must stay human-readable and reviewable in a pull
request, because that review is the mechanism the lifecycle depends on.

---

### Step 8 — The scope as a file *(Golden Rule 5, made physical)*

**Build:** the knowledge store. **One SQLite file per scope** ([D-23](DECISIONS.md)) — Global, Company,
Project, User, Temporary, Experimental ([10 §1](10-memory-and-knowledge.md)) — and the resolver that
picks the scope **before** retrieval, from the active document, never by inference
([10 §2](10-memory-and-knowledge.md)).

**Prove it:** a cross-scope query must be **impossible to write**, not merely absent — the retrieval API
takes one scope and has no parameter that accepts two. Then delete a project scope file and confirm the
others are untouched and Heron still starts.

**Watch for:** the index is **derived, never authoritative** ([05 §7](05-heron-brain.md)). Deleting every
`.db` must be a safe recovery action that costs a rebuild and loses nothing.

---

### Step 9 — Finding it by exact words *(and the short circuit that skips everything)*

**Build:** FTS5 keyword search over each scope, and the **exact-match short circuit** — when a request
matches a proven fragment's semantic identity exactly, execute it and skip retrieval entirely
([05 §4](05-heron-brain.md)). Plus the **utterance cache** the roadmap asks for.

**Prove it:** *"select all ducts"*, asked twice. The second one takes the short circuit, and a counter
says so out loud. Then ask for `OST_DuctCurves` by name and get the exact fragment — the token class
embeddings handle worst.

**Why keywords before meaning:** BIM requests are full of exact tokens — `OST_DuctCurves`,
`RBS_DUCT_BOTTOM_ELEVATION`, a shared-parameter GUID. Semantic search returns the *nearly* right
parameter with confidence, and in a system that can modify a model that is worse than returning nothing.

---

### Step 10 — Finding it by meaning *(local, offline, no account)*

**Build:** embeddings computed **on the machine** ([D-24](DECISIONS.md), [D-26](DECISIONS.md) — no cloud
opt-in, no setting), stored with `sqlite-vec`, indexed **by content hash** so a git checkout does not
trigger a full re-embed ([05 §7](05-heron-brain.md)).

**Prove it:** ask in words the fragment does not contain and still find it. Then **turn the network off**
and do it again. Installing Heron must still need no API key and no account.

**Watch for:** the embedding backend is the one part of this phase with a real install cost, and Heron's
install promise is per-user with no administrator rights ([D-01](DECISIONS.md), proven in Phase 0). Put
it behind the retrieval interface so it is swappable — [05 §5](05-heron-brain.md) already names that
interface as the seam. **A backend that cannot be installed without admin rights fails this step**, no
matter how well it scores.

---

### Step 11 — The two together *(and the filter that must never be a suggestion)*

**Build:** the full retrieval stack in the order [05 §4](05-heron-brain.md) sets out — structured filter
first (scope, domain, Revit version, status) as a plain SQL `WHERE`; then BM25 and vector over the
survivors; reciprocal rank fusion; then re-rank the top ~20 on fragment quality.

**Prove it:** a fragment declared for Revit 2021 only is **never returned** for a 2025 request — not
ranked lower, not returned. Prove it by making that fragment the best possible textual match and
confirming it still does not appear.

**Why that is the acceptance test:** version compatibility is a **hard filter before ranking, never a
soft signal** ([05 §8](05-heron-brain.md)). A fragment written for 2021 applied in 2025 is the
confident-wrong-retrieval failure, and it is silent.

---

### Step 12 — The Capability Registry *(the highest-leverage single piece)*

**Build:** the registry the roadmap calls Phase 2's highest-leverage component — **separate from the
agent registry**, carrying cost tier and risk level per capability, and the resolution path that lets
the Orchestrator ask for a **capability** rather than an agent by name.

**Prove it:** add a second provider of an existing capability and confirm **no call site changes**. Then
remove the first provider and confirm the request still resolves.

**Why here and not later:** retrofitting it means rewriting every call site. It is also what stops the
Orchestrator accumulating domain knowledge — which is the failure that makes an orchestrator
unmaintainable.

---

### Step 13 — The dependency graph *(derived before stored)*

**Build:** the graph — skills → fragments → API → runtime — in SQLite beside the knowledge store
([D-40](DECISIONS.md)). **An edge is stored only when it cannot be computed from an artifact on demand.**
A fragment's declared inputs, an assembly's references, a manifest's contents are all *read*, not stored.

**Prove it:** *"what breaks if this fragment changes"* returns the right set. And, because most of the
graph is computed, **the deriver itself must be shown to catch a defect it is known to contain** before
its clean output is believed — the same standard `check-api-surface.py` was held to.

---

### Step 14 — Ten real skills, none of them hard-coded

**Build:** ten skills that work end to end through the machinery above. The material comes from
**studying the existing libraries and re-authoring what earns a place** ([D-25](DECISIONS.md)) — read for
the mechanism, then written here in Heron's shape, with Heron's reasoning, carrying **none** of their
names, dependencies or branding. Each starts at `DRAFT` whatever status it held where it was read.

**Prove it:** ten skills resolve through capabilities rather than agent names, and each carries **its
own proof with a negative case** ([D-30](DECISIONS.md)).

> **This is the one step in Phase 2 that needs a real Revit**, and only for the last word. The skills can
> be written, composed, validated and unit-tested without one; what cannot be faked is the proof that
> each actually does what it says against a model. Until then they are `DRAFT`, and
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) carries the debt like every other unproven thing here.

**Phase 2's definition of done:** ten real skills work, none hard-coded; a capability re-authored from an
existing library carries its own proof; and the Orchestrator resolves through capabilities rather than
agent names. **Two of those three can be finished without Revit. The first cannot**, and saying so now is
cheaper than discovering it in week two.

---

## Honest scale — Phase 2

| Step | Roughly |
|---|---|
| 7 — fragment on disk | small. Shape and a validator |
| 8 — scope as a file | small |
| 9 — exact words | small. The short circuit is the valuable half |
| 10 — meaning | **medium, and the one with an install risk.** Budget time for the backend choice |
| 11 — the two together | medium. The hard version filter is the part to get right |
| 12 — Capability Registry | **medium-large, and the highest leverage in the phase** |
| 13 — dependency graph | small, if D-40's derive-before-store rule is held to |
| 14 — ten skills | **the largest, and the only one gated on a machine** |

Steps 7–11 are a working knowledge store that can find things. That is the milestone worth aiming at
first — before it, nothing accumulates; after it, everything does.

---

## What comes after

**Steps 7 to 14 are above** — written out on 2026-08-28, when Phase 2 started. That is where Heron stops
being a tool bridge and starts being a platform, and it is also where it starts doing things
[the existing Revit MCP servers](26-prior-art-revit-mcp.md) do not.

This section used to read *"Steps 7+ follow ROADMAP Phase 2"* and end with **"but none of it matters
until Step 6 works."** The second half was overtaken by events rather than proved wrong: Step 6 still
does not *work* in the sense that sentence meant, and Phase 2 began anyway because the owner has no
Revit for a week and there is a phase's worth of work that does not need one. The debt did not go away —
it moved to [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) and stayed there.

---

## Honest scale

| Step | Roughly |
|---|---|
| 1 — pipe | small. Mostly plumbing and getting the add-in to load |
| 2 — thread hop | small in code, **the highest-risk step**. Budget time to get it right |
| 3 — MCP server | small |
| 4 — first tool | small |
| 5 — multi-Revit | medium |
| 6 — write + rails | **the largest step in Phase 0/1.** Do not compress it |

Steps 1–4 are a working end-to-end system. That is the milestone worth aiming at first — everything
before it is unfinished, and everything after it is improvement.
