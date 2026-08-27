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
All of Phase 0 and 1 needs about **20 of the ~150 agents**, and 17 of those are plain deterministic
modules with no model call at all.

---

## Step 1 — Prove the pipe *(no Revit API at all)*

**Build:** a minimal C# Revit add-in that does one thing — opens a named pipe called
`heron.{revitVersion}.{pid}`, writes `%APPDATA%\Heron\bridges\<pid>.json`, and answers `ping` with `pong`.
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

## What comes after

Steps 7+ follow [ROADMAP Phase 2](ROADMAP.md) — capability registry, fragments, knowledge, RAG. That is
where Heron stops being a tool bridge and starts being a platform, and it is also where it starts doing
things [the existing Revit MCP servers](26-prior-art-revit-mcp.md) do not.

But none of it matters until Step 6 works.

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
