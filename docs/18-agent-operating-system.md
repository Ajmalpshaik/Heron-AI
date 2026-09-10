# 18 — Agent Operating System

> Derived from [Master Specification Part 2](00b-master-specification-agent-os.md) §1–§14, §51, §83.
> **[NOTE]** blocks are engineering commentary added during review.
>
> This document covers the biggest structural idea in Part 2: the agent framework is an
> **operating system**, not a collection of agents.

---

## 1. The core shift

Part 1 described *which* agents exist. Part 2 describes *how they are governed*.

The Agent OS owns: registration · discovery · activation · execution · communication · permissions ·
health · versions · dependencies · lifecycle · performance · retirement · replacement.

**Agents do not manage these things for themselves.** An agent is a component with a contract; the OS
decides when it runs, what it may touch, and whether it still deserves to exist.

**[NOTE]** This is what makes the "10 → 1,000 agents without redesigning the core" requirement (§2)
achievable. Scale does not come from writing agents well — it comes from agents being *governed
uniformly*. Every agent that manages its own lifecycle is a special case, and 1,000 special cases is
not a platform.

---

## 2. **Capability Registry — the most valuable idea in Part 2**

> Agent identity and capability identity are **separate**. The Orchestrator searches capabilities,
> never agent names.

```text
Capability:  SELECT_DUCTS
Provided by: Revit Selection Agent
Related:     OST_DuctCurves
```

### Why this matters more than it first appears

Without it, the Orchestrator would have to know which agent does what — which means the Orchestrator
accumulates domain knowledge, which is exactly the monolith both specifications forbid
([Part 1 §50](00-master-specification.md), [Part 2 §83](00b-master-specification-agent-os.md)).

With it:

| Benefit | Consequence |
|---|---|
| **Agents become replaceable** | Swap `Revit Selection Agent v1` for `v2`, or split it into three, and no user workflow changes |
| **The Orchestrator stays thin** | It matches capability to request. It never knows what a duct is |
| **Retirement is safe** | An agent can be retired the moment another provides its capabilities |
| **Discovery is semantic, not lexical** | *"Find all unconnected ducts"* resolves to `DUCT_CONNECTION_ANALYSIS`, not to "the agent with 'duct' in its name" |
| **Gap detection becomes trivial** | A capability with no provider *is* the capability gap |

**[NOTE]** The Capability Registry is also where the [T1/T2/T3 tiering](02-architecture-overview.md)
belongs. A capability entry should declare its **cost tier** and **risk level**, so the Orchestrator can
answer "can I do this without a model call?" and "does this need confirmation?" before selecting anything.

### Recommended capability entry

| Field | Value |
|---|---|
| Capability ID | `SELECT_ELEMENTS_BY_CATEGORY` |
| Semantic identity | stable, survives renaming |
| Domain | Revit / Selection |
| Providers | [agent ids, ordered by trust score] |
| Input contract | JSON Schema |
| Output contract | JSON Schema |
| Risk level | READ \| ANALYZE \| SUGGEST \| EXECUTE \| MODIFY \| PUBLISH \| ADMIN |
| Cost tier | T1 \| T2 \| T3 |
| Platform support | Revit 2020-2027 |
| Related fragments | [fragment ids] |
| Status | PROPOSED ... PRODUCTION |

---

## 3. Agent Registry

```text
Agent ID / Name / Department / Role / Version / Status / Capabilities
Required Skills / Required Knowledge / Allowed Tools / Permissions / Dependencies
Input Contract / Output Contract / Health / Performance Score
Creation Date / Last Updated / Owner
```

Example: `HERON-REVIT-SEL-001` — Revit Selection Agent — Revit Engineering — element selection —
Revit 2020–2027 — `PRODUCTION`.

**[NOTE]** Part 2's registry adds **Performance Score** and **Health** to Part 1's list, which is what
turns the registry from a catalogue into a live system of record. Combined with the three fields
recommended in [06 §3](06-heron-platform.md) — tier, risk level, contract schema path — the registry
becomes the single place that answers *what can Heron do, how much does it cost, how risky is it, and
is it working?*

---

## 4. Agent onboarding — and **Shadow Mode**

```text
PROPOSED -> DESIGNED -> IMPLEMENTED -> TRAINING -> TESTING
-> SHADOW MODE -> APPROVED -> PRODUCTION
```

> **Shadow Mode:** the agent observes and makes recommendations **without modifying production data**.

**[NOTE — this closes a gap flagged in the Part 1 review]**

[PROPOSALS A12](PROPOSALS.md) raised that autonomous agent creation had no hard safety boundary.
Shadow Mode is the mechanism that was missing, and it is a better answer than the "human approves
activation" rule proposed there — because it produces *evidence* rather than just requiring a signature.

An agent in Shadow Mode runs on real requests, produces real recommendations, and is scored against
what actually happened — but its output never reaches the model. After N shadow runs you know whether
it works, rather than guessing.

**Recommendation: keep both.** Shadow Mode produces the evidence; a human still approves
`SHADOW MODE → APPROVED`. Evidence plus a signature. See [Golden Rule 7](14-golden-rules.md) — no agent
approves itself.

### How Shadow Mode works concretely

This needs defining, because "observe without modifying" means different things for different agents:

| Agent type | Shadow behaviour |
|---|---|
| **Read-only (T1)** | Run normally, compare output against the production agent's output |
| **Analysis / ranking (T2)** | Run in parallel, log both results, score agreement |
| **Model-modifying (`MODIFY`)** | Produce the **preview** only. Never open a transaction. Compare its intended change against what the production path actually did |
| **Code-generating (T3)** | Generate and test in the sandbox. Never promote to PRODUCTION |

Tracked as [Q-29](OPEN-QUESTIONS.md).

---

## 5. Agent Trust Score

A dynamic score from: successful executions · failed executions · user corrections · QA results ·
regression results · performance · security incidents · compatibility · age · usage frequency.

```text
Trust Score: 96.4%      -> Production
Trust Score: Unproven   -> newly created
```

**[NOTE]** Three things to get right, or the score will mislead:

1. **A user correction is a failure**, even when the operation technically succeeded. If the user
   immediately undoes or rephrases, that is the strongest negative signal available.
2. **Trust must be per-capability-per-version, not per-agent.** An agent may be flawless in Revit 2024
   and broken in 2025. A single blended number would hide exactly the failure the platform most needs
   to catch — see [16](16-version-support-strategy.md).
3. **Unproven ≠ 0%.** A new agent has *no data*, which is different from a bad agent. Sample size must
   be visible alongside the score, or 3/3 will read as 100%.

**[NOTE]** Trust score should **order providers in the Capability Registry**. When two agents provide
`SELECT_DUCTS`, the higher-trust one runs and the other becomes the fallback. That makes trust
operational rather than decorative.

---

## 6. Agent HR system

| Role | Function |
|---|---|
| Executive Orchestrator | Declares the need |
| Agent HR | Job description, responsibilities, dependencies, required knowledge and tools |
| Agent Architect | Designs |
| Agent Builder | Implements |
| Training Agent | Supplies standards and domain knowledge |
| QA Agent | Tests |
| Registry Agent | Registers |
| Deployment Agent | Activates |

**[NOTE]** The HR metaphor holds up well, and it enforces Golden Rule 7 structurally: the builder is
not the tester, the tester is not the deployer. That separation is the whole point, not decoration.

**[NOTE]** One caution. This is eight agentic steps to produce one agent — expensive, slow, and only
worth it when the capability is genuinely new. Most "new capabilities" are a **new fragment**, not a
new agent. The gate before the HR pipeline runs should be strict:

> Does an existing agent already provide a capability that covers this, with a different fragment?
> If yes, write the fragment. Do not hire.

Otherwise Heron accumulates near-duplicate agents at exactly the rate the Fragment Merge Agent exists
to prevent for fragments.

---

## 7. Capability gap detection

```text
Capability Gap Detected -> Gap Analysis Agent -> Check Existing Agents
-> Check Existing Skills -> Check Existing Fragments -> Check External Knowledge
-> Determine Requirement -> Propose New Agent
```

**[NOTE]** With a Capability Registry this becomes nearly free: **a requested capability with no
provider is the gap.** No inference needed.

This reinforces the recommendation in [06 §6](06-heron-platform.md) — build the Capability Gap report
early, as a read-only report over the audit log. It is the cheapest way to learn what to build next
from real usage rather than guesswork.

---

## 8. Agent retirement

```text
Active -> Under Review -> Replacement Found -> Migration -> Deprecated -> Archived
```

> Never permanently delete important historical knowledge automatically.

**[NOTE]** With capabilities separated from agents, retirement is genuinely safe: migrate the
capability's provider list, and no caller notices. Without the Capability Registry, retiring an agent
would break every workflow that named it.

---

## 9. Agent communication protocol

```text
REQUEST / TASK / CONTEXT / INPUT / EXPECTED_OUTPUT / CONSTRAINTS
RESULT / ERROR / CONFIDENCE / STATUS
```

Agents must not dump large context into one another. The [Context Manager](19-context-and-cost.md)
decides what each agent actually needs.

**[NOTE]** `CONFIDENCE` is new in Part 2 and is worth taking seriously. It is what makes
[knowledge conflict resolution](20-knowledge-trust-and-conflict.md) and "ask the user when unsure"
possible. An agent that cannot express uncertainty forces the Orchestrator to treat every answer as
equally certain — which is how a plausible wrong answer reaches a live model.

Confidence should be **calibrated against outcomes**, not self-reported and forgotten: log predicted
confidence against actual success, and an agent whose 90% is really 60% shows up in the data.

---

## 10. Event-driven architecture

`FragmentCreated` · `FragmentUpdated` · `FragmentApproved` · `SkillCreated` · `AgentCreated` ·
`AgentFailed` · `AgentRetired` · `PluginInstalled` · `PluginUpdated` · `MCPConnected` ·
`MCPDisconnected` · `RevitOpened` · `RevitClosed` · `RepositoryChanged`

**[NOTE]** Events are what let re-indexing, dependency analysis, health checks and learning react
without the Orchestrator having to call them. That directly serves Golden Rule 8 — background work
stays invisible.

Two constraints:

1. **Events are notifications, not commands.** An event handler may schedule work; it may not perform
   a `MODIFY` or `PUBLISH` action directly. Otherwise the permission model has a side door.
2. **Event handlers must be idempotent.** `FragmentUpdated` will fire twice at some point.

---

## 11. The most important architectural rule (§83)

```text
ONE USER REQUEST -> ONE ORCHESTRATED WORKFLOW -> ONLY REQUIRED AGENTS
-> ONLY REQUIRED KNOWLEDGE -> ONLY REQUIRED TOOLS -> VALIDATED RESULT
```

**[NOTE]** This is the same principle as [02 §6](02-architecture-overview.md) arrived at from the cost
side, and Part 2 §59 arrives at from the waste side. Three independent routes to the same conclusion
is a good sign the architecture is sound.

Stated as an operational target: for a warm, cached *"select all ducts"* the workflow should be
**one capability lookup, one proven fragment, one Revit call** — and, ideally, **zero model calls**
once the utterance is cached.
