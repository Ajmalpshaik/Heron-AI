# Open questions — Answered

> One section of [the register](../OPEN-QUESTIONS.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## Answered

### ✅ Q-30 — Who routes models — Heron or Claude Code? — *closed by Part 4 §24*

Part 4 §24's **AI Model Abstraction Layer** resolves this by separating two things that were conflated:

```text
Heron AI Interface  ->  Model Router  ->  Provider Adapter  ->  Model
    (Heron: intent)          (pluggable: host when hosted, Heron for batch work)
```

Heron always declares *intent* ("this needs strong reasoning"); resolution to a specific model is
pluggable. Under [D-01](../DECISIONS.md) Claude Code resolves conversational work; Heron's Python side
resolves its own batch work through the same interface. Neither half hard-codes a model id.

It also makes local/cloud routing (§25) a configuration choice rather than an architectural one — a
project marked confidential selects a local provider adapter, and nothing above that layer needs to know.

→ [23 §8](../23-heron-kernel.md), [19 §3](../19-context-and-cost.md)


### ✅ Q-2 — Transport between MCP server and add-in? → **Named pipes**

C# add-in is the pipe server, Python MCP server is the client, pipe name encodes Revit version + PID.
Local-only by construction. → [D-02](../DECISIONS.md)

### ✅ Q-39 — Must the user install Python? → **Yes, and the installer says so**

**Node is not the free option it looked like.** Claude Code ships as a native binary, not an npm
package, so it does **not** require Node — checked on the development machine, where Claude Code is not
an npm global and Node is a hand-downloaded folder. Neither runtime is pre-installed on a fresh machine,
so "the user already has it" was simply false, and the install cost is the same either way.

That leaves one question that actually differs: **the brain needs Python.** RAG, embeddings and vector
search live in Python's ecosystem ([05](../05-heron-brain.md)), which is what [D-06](../DECISIONS.md) was
decided on. Choosing Node for the MCP server would not avoid Python — it would ship **two** runtimes
instead of one, and put a language boundary where no process boundary exists.

Both can be bundled into a single executable later, so bundling does not favour either.

→ **Python.** [D-06](../DECISIONS.md) stands, and installation lists Python as a prerequisite rather than
discovering it on someone else's machine. Bundling stays open under [Q-38](from-master-specification-part-2.md#q-38--what-is-the-exact-install-command-new).

**And it does not need administrator rights**, which was the real worry. Verified on the development
machine: its Python is a Microsoft Store build living in `AppData\Local`, and the `mcp` package sits in
a per-user site-packages folder. Nothing went near `Program Files` or the registry.

| Needs admin | |
|---|---|
| Python, per-user | **No.** `winget install Python.Python.3.12 --scope user`, or the Microsoft Store |
| The `mcp` package | **No.** `pip install --user mcp` |
| The Revit add-in | **No.** Per-user add-in folder, which is why [D-05](../DECISIONS.md) chose it |
| Git | **Not needed at all** for a released install — only to build from source |
| .NET SDK | **Not needed at all** for a released install — only to build from source |

So the complete list for an ordinary user is **Claude Code, Revit, Python** — and none of it requires
IT approval. `tools/setup.ps1` now detects Python and the `mcp` package and prints the exact per-user
command when either is missing, rather than leaving someone to wonder why asking Claude a question does
nothing.

It **tells** rather than installs. Pulling a language runtime onto somebody's machine unasked is the
kind of thing a careful user and a corporate laptop are both right to refuse.

### ✅ Q-4 — `ExternalEvent` or `Idling`? → **`ExternalEvent`, one queue, one handler**

`Idling` used only for a liveness heartbeat. Heron must surface "Revit is busy" rather than hanging.
→ [D-09](../DECISIONS.md)

### ✅ Q-36 — Lease or takeover when two chats target the same Revit? → **Lease (option B)**

Built in Step 6 as `HeronLease`. A second chat is **refused** with a message saying what is happening and
when it clears, rather than taking the session and chopping whatever the first was doing. Scoped to the
Revit **process**, not the document — the contention is at the pipe. `ping` and `info` are exempt, which
is what finally makes `(free)` / `(in use)` truthful in the picker and ends a person being used as a
lock. It cannot block a rollback: the lease is checked when a request arrives, and a rollback happens
inside a request already admitted.

**One thing the question got slightly wrong**, worth keeping: option B does *not* "remove the hazard
entirely". The pipe is displaced when a second chat CONNECTS, before the lease can be consulted, so the
first chat still loses one in-flight reply. The lease removes the **takeover**; it narrows the
**interruption**. → [D-22](../DECISIONS.md), [25 §3](../25-multi-session-and-binding.md)

### ✅ Q-19 — Accept proposed Golden Rules 16–21? → **Accepted, 2026-08-28**

All six are now official and binding, on the same footing as 1–15. Four of them — **16** (one user action,
one undo), **17** (no autonomous write without a preview), **20** (bind the document, not just the
session) and **21** (re-read before acting; a preview expires) — are exactly what Step 6 was built to
obey.

**Accepted while Step 6 is still unproven, and deliberately so.** A rule that only becomes binding once
the code passes is not a rule the code was ever held to. Settling the standard first is what makes the
checking that follows a test of the code rather than a negotiation about the standard.

18 (generated code never touches a live model on its first run) and 19 (no text Heron reads may raise
its own permission level) bind work that does not exist yet. That is the right time to accept them —
before there is any code with an interest in the answer. → [14](../14-golden-rules.md)

### ✅ Q-14 — How is testing against real Revit done? → **A written register, in dependency order**

[`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — every unproven claim as a numbered item (`A1`, `D3`), in
dependency order, each stating what PASS actually looks like. Items are added whenever something is built
away from Revit, and deleted only when they have actually passed.

Three things make it work rather than being a to-do list:

- **Dependency order.** Nothing in group D can be attempted before group A compiles. Working down instead
  of around is what stops a "pass" that was never really tested.
- **What needs Revit is separated from what does not.** Group A needs Windows and the SDK only — and the
  round-trip test proves the bridge *and* most of the lease there, before Revit is ever opened.
- **One register, not one per document.** [HANDOVER](../HANDOVER.md) §6 points at it rather than keeping
  a copy, because two lists of the same thing drift.

**What it does not answer:** automated testing against a real Revit, in CI. That needs a machine with
Revit installed and is a Phase 2 question. This is the manual practice, written down — which is what was
actually being asked for.

### ✅ Q-5 — MCP tool granularity? → **Thick and specific**

Each tool maps onto a fragment and carries its own risk level. Generic `revit_execute` only in
Developer Persona behind `ADMIN`. Capability discovery keeps the context cost down. → [D-03](../DECISIONS.md)

### ✅ Q-7 — How does generated code execute? → **Hybrid**

Scripting sandbox while DRAFT/TESTING, compiled signed C# for PRODUCTION. The `PROVEN → PRODUCTION`
gate is where compilation happens. Sub-question Q-7a (which scripting runtime) was answered on
2026-08-28 — **Roslyn C#, in process** ([D-28](../DECISIONS.md)).
→ [D-04](../DECISIONS.md)

### ✅ Q-27 — Which licence? → **Apache 2.0**

Chosen over MIT for its explicit patent grant and warranty disclaimer, which matter for software that
writes to live client models; over GPL because many construction firms forbid GPL internally.
→ [D-08](../DECISIONS.md)

### ✅ Q-28 — When does the repo go public? → **When licence + safety files exist AND there is working code**

Safety files completed 2026-08-27. Remaining condition: Phase 0 working code. → [D-10](../DECISIONS.md)

### ✅ Q-1 — Where does Heron run? → **Claude Code plugin**

Claude Code is the conversation layer and agent host. Heron supplies skills, subagents, an MCP server and
the Revit add-in. → [D-01](../DECISIONS.md)

### ✅ Q-3 — Which Revit versions? → **2020 through latest, and every future release**

Accepts two API breaks (`ElementId` 64-bit at 2024, .NET 8 at 2025). Requires multi-targeting from one
source tree and an adapter layer from the first line of code. → [D-05](../DECISIONS.md), [16](../16-version-support-strategy.md)

### ✅ Q-6 — What language? → **C# for Revit, Python for the brain**

The language boundary sits exactly where the process boundary already had to be.
→ [D-06](../DECISIONS.md)

### ✅ Q-21 — Commercial model? → **Free and open source**

Public GitHub, installable by anyone. Autodesk App Store later, also free. → [D-07](../DECISIONS.md), [17](../17-open-source-and-distribution.md)

### ✅ Q-22 — First users? → **Everyone**

Not personal tooling and not company-internal. The installer, health checks and persona system are
therefore real scope, and defaults must be safe for the most restricted user. → [D-07](../DECISIONS.md)

### ✅ Q-23 — Relationship to the existing AJ-Tools family → **Upgrade and absorb**

The owner's earlier brain work is the reference for the brain layer, and his earlier connector work for the Revit bridge.
Their ideas are taken, upgraded and reshaped to the Heron architecture — after documentation is finalised,
on the owner's signal. `AJ-Tools` / `PyRevit-Tools` / `AEB-Tools` are candidates for the first knowledge
import (Q-16). → [D-06](../DECISIONS.md)
