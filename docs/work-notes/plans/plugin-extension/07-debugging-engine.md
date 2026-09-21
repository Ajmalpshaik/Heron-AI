# Phase 2 — the debugging engine: what Heron already has, and what it has not

> **Type:** Operational work note. **Not specification.** Where a sentence here disagrees with the
> [Constitution](../../../../HERON_CONSTITUTION.md), the [Golden Rules](../../../14-golden-rules.md) or
> [DECISIONS.md](../../../DECISIONS.md), **those win and this note is out of date.**
> **Status:** **Active — surveyed 2026-09-21, nothing built.** **Owner:** Ajmal PS.

---

## 1. What was asked for

> Owner, 2026-09-21: *"In the AJ tool, there is a debugging option. Before, I would go to the folder and
> tell the existing tools, 'This is the issue; this is the error that came up.' Then it would fix it
> automatically: it runs fragments into the library, studies it, runs that specific part, finds the
> issues, and automatically fixes them … I think we already have it. You can check, find it."*

**Answer: about two-thirds of it exists, and the missing third is the part that matters most.** Every
piece below was read on 2026-09-21; none of it was assumed.

---

## 2. The loop he described, against what is in the repository

| | Step | Heron has | State |
|---|---|---|---|
| 1 | *"tell it: this is the error"* | **nothing** — no entry point takes a user's error report | ✗ |
| 2 | *"runs fragments into the library, studies it"* | the fragment store, exact-word search, embeddings, the capability registry | built |
| 3 | *"runs that specific part"* | **[D-28](../../../DECISIONS.md)'s executor** — compiles a fragment's C# inside Revit's own process and runs it. `run_fragment` read-only, `run_fragment_write` wrapped in a `TransactionGroup` | **built, and has met a real model** |
| 4 | *"finds the issues"* | `heron_unit_test.py` (why each suite failed) · `check-fragments-compile.py` · `heron_failure.py` · `heron_diagnose.py` | built, `DRAFT` |
| 5 | *"automatically fixes them"* | **nothing that repairs CODE.** See §3 — this is the gap | ✗ |
| 6 | *test it again* | `heron_regression.py` (every supported version, rejects unsafe changes) · `heron_sandbox.py` · `tools/batch-prove.py` | built, `DRAFT` |
| 7 | *deploy* | `deploy-addin.ps1` · the promotion path in `heron_fragment.py` | built |

**Steps 2, 3, 4, 6 and 7 are there. Steps 1 and 5 are not, and nothing joins any of them into a loop.**

---

## 3. The gap, stated exactly — Heron repairs SITUATIONS, not CODE

This is the finding that matters, and it is easy to miss because the file names suggest otherwise.

| Agent | Repairs | Not |
|---|---|---|
| `heron_failure.py` `HERON-ORC-FAIL-004` | classifies a failed **request** into six next steps. **Never blind-retries** | — |
| `heron_fix.py` `HERON-ORC-FIX-005` | applies the **one** repair failure analysis chose | **five of the six it refuses by name** |
| `heron_healing.py` `HERON-OPS-HEA-006` | **derived** state — anything rebuildable from a source of truth | anything else; it proposes instead |
| `heron_repair.py` `HERON-WSP-REP-004` | **file placement**, dry-run by default | file contents |

**Read what `heron_fix` is actually fixing** and the point is plain — `no_document` (no model is open),
`read_only`, `write_disabled`, `stopped` (the Emergency Stop is on), `preview_expired`. These are
**states of the world**, not bugs.

Its own file says so:

> *"THE REPAIR IS SUPPLIED, NOT INVENTED. Most FIX_FIRST codes are not a machine's to fix. 'No model is
> open', 'a dialog is open or a command is running', 'the Emergency Stop is on' — Heron cannot open a
> model, close somebody's dialog or overrule a stop."*

**So `heron_fix.py` is deliberately built NOT to invent a repair.** That is a design decision with a
reason behind it, not an unfinished feature — and it means **the code-fixing agent is a new agent, not
an upgrade of this one.**

### What IS genuinely built and working

| | |
|---|---|
| **`HERON-OPS-DIA-005`, the Self-Diagnostics Agent** | **Built 2026-09-07.** `mcp/server/heron_diagnose.py`, served as `heron_diagnose`. *"Diagnose Heron"* in one report. It **composes** rather than reimplements — health for the live picture, the brain seam for knowledge, the Compatibility Matrix for releases. Its contract is that **it never raises** |
| **[D-28](../../../DECISIONS.md)'s fragment executor** | Compiles and runs a fragment's C# **inside Revit's own process**, against the assemblies Revit actually loaded. This is step 3 of his loop, already working against a real model |
| **`HeronStackGuard`** | A runaway fragment comes back as a catchable finding instead of taking Revit and the unsaved model with it. **Proved in front of Revit 2026-09-16** |

**That executor is the expensive part and it is done.** An engine that can already *run one fragment in
isolation inside Revit and survive it crashing* is most of what a debugging loop needs.

---

## 4. What the rules allow, and it is not "fix it silently"

Three rules bear on an engine that repairs Heron's own code. **None of them forbids it. All three shape
it.**

### Golden Rule 13 — no uncontrolled self-modification

> *Heron may **propose** changes to its own core. **It may not apply them.** New agents reach `PROPOSED`
> on their own and go no further without a human.*

And [docs/09 §6](../../../09-skills-and-fragments.md) draws the line where it actually bites:

> *All three propose; **none may apply autonomously to anything at `PRODUCTION`**.*

**So the line is status, not intent:**

| Fragment status | May the engine apply its own fix? |
|---|---|
| `DRAFT` | **Yes** — it is unproven either way, and a proof is owed before it is trusted |
| `PROVEN` / `PRODUCTION` | **No.** It proposes; a person says yes |

That is a workable rule and it costs almost nothing, because **the fragments a debugging engine will be
pointed at are mostly the ones that are failing** — and a failing fragment is rarely one anybody has
proven.

### Golden Rule 18 and Constitution Article 11 — untested code never touches a live model

> *Generated or newly imported code runs in a sandbox or against a detached copy first.*

`heron_sandbox.py` exists for exactly this. **A repaired fragment is newly generated code**, so it goes
through the sandbox before it is offered to a model — which is also the honest answer to *"how do I know
the fix did not make it worse"*.

### Golden Rule 12 — no agent approves itself

> *The builder is not the tester, the tester is not the deployer.*

**So the agent that writes the fix may not be the agent that judges it.** Heron already has the second
one: `heron_regression.py` builds and tests every supported version and **rejects unsafe changes**.

---

## 5. What would actually have to be built

**Two new agents and one loop.** Everything else is wiring what exists.

| | What | Why it is new |
|---|---|---|
| **A** | **An entry point** — takes *"this is the error"* in plain words and finds which fragment, skill or tool is responsible | Nothing today takes a user's error report. `HERON-ORC-INT-002` classifies *debugging* as a kind of request but is **provided by the host** ([D-01](../../../DECISIONS.md)) — Claude Code does the language, and hands on something specific |
| **B** | **A code-repair agent** — proposes a change to a fragment's body | `heron_fix` is deliberately the wrong shape for this (§3), and `heron_evolve` decides *whether* to change a fragment, not *what* to write |
| **C** | **The loop** | reproduce → locate → run in sandbox → repair → regression → prove → promote. Every box but B and the first exists; **nothing connects them** |

### The order to build it in

```text
1  Entry point (A)          -> without it nobody can start the loop
2  Reproduce                -> D-28's executor, already built; run the ONE fragment and capture it
3  The loop, WITHOUT B      -> report the diagnosis and STOP. Useful on its own, and it proves 1-2
4  Repair agent (B)         -> proposes a change; applies only on DRAFT
5  Sandbox + regression      -> both exist; wire them in as the gate before any promotion
6  Prove                    -> tools/batch-prove.py, and D-30's rules apply unchanged
```

**Step 3 is the one to insist on.** An engine that says *"this fragment fails on this input, here is the
line"* and stops is **most of the value and none of the risk** — and it is what makes step 4 safe to
add, because by then the locating half has been watched working.

---

## 6. What this changes about the tool-porting plan

The owner put it in order himself:

> *"We need to implement this first so we can add more tools."*

**And [06](06-porting-method.md) says the porting will break fragment proofs on purpose** — widening a
fragment to match a tool marks its proof `STALE`. So the two fit together:

```text
port a tool  ->  fragment widened  ->  proof goes STALE  ->  the loop re-proves it
     ^                                                              |
     +--------------------- and the next tool -----------------------+
```

The debugging engine is **what makes the porting sustainable**. Without it, every widened fragment is a
manual re-proof.

---

## 7. Open questions

### Q-DE-1 — May the engine apply a fix to a `DRAFT` fragment without asking?

§4 says the rules allow it. **Whether the owner wants it is a different question**, and it is his.
`PROVEN` and `PRODUCTION` are settled — those always propose.

### Q-DE-2 — Does the engine ever touch C# outside `brain/fragments/`?

A fragment body is one thing. The add-in, the bridge and the kernel are another, and Golden Rule 13
calls those *"its own core"*. **Recommended: fragments only, to begin with.** Nothing is lost by
starting narrow, and the blast radius of a wrong answer is one fragment rather than Revit.

### Q-DE-3 — Where does the user report the error from?

The Claude Code chat, a Heron ribbon button, or the Settings panel. Ties to
[Q-PE-1](03-open-questions.md) and to whatever Phase 3 says about the in-Revit surface.

---

**Nothing in this note is built. The survey is the deliverable.**
