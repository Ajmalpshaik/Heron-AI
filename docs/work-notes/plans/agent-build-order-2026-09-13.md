# The build order for the agents that need no Revit

**Status: ACTIVE.** Written 2026-09-13, after the owner asked for the order that makes each agent
easier to build than the one before it — *"if we create something it can be used for next creation
easily"*.

**Closure condition:** deleted when every block below is built or has moved into
[NEEDS-CHECKING](../../NEEDS-CHECKING.md) or [OPEN-QUESTIONS](../../OPEN-QUESTIONS.md). It is a
schedule, not a register.

> Where this disagrees with [DECISIONS.md](../../DECISIONS.md), the
> [Golden Rules](../../14-golden-rules.md) or the [Constitution](../../../HERON_CONSTITUTION.md),
> **those win.** It continues [next-steps-2026-09-12](next-steps-2026-09-12.md) Track B item B4,
> which said only *"keep building agents"* and left the order to whoever picked it up.

---

## The numbers, and where they come from

**There are none written here.** This plan was drafted against a snapshot and the snapshot moved
within the same day it was written, which is the whole argument. Derive them instead:

```bash
python tools/agent-count.py                       # planned, built, host, left
python brain/heron_validation.py --all            # how many of the built ones pass
```

The one split this plan actually rests on is **Revit Engineering against everything else**: those
agents are C# that must run inside `Revit.exe`, so they cannot be finished on a machine with no Revit,
and every other department can. Derive that too rather than reading a number here:

```bash
python - <<'PY'
import importlib.util
spec = importlib.util.spec_from_file_location("ac", "tools/agent-count.py")
ac = importlib.util.module_from_spec(spec); spec.loader.exec_module(ac)
agents, _h, _t = ac.registry(); built = ac.built(); host = set(ac.host_provided() or {})
left = [(a, d) for a, d in agents.items() if a not in built and a not in host]
revit = [a for a, d in left if d["dept"] == "Revit Engineering"]
print("%d left, %d of them Revit, %d buildable with no Revit"
      % (len(left), len(revit), len(left) - len(revit)))
PY
```

**The blocks below are the order, not the arithmetic.**

---

## The principle

> **Build the seams before the agents that plug into them.**

An agent built before its seam exists invents one. Twenty agents built that way is twenty private
conventions, and the twenty-first cannot reuse any of them. So the order below is not by department
and not by how much each agent is wanted — it is by **how many later agents can reuse it**.

Three seams carry almost all of that leverage:

| Seam | Reused by |
|---|---|
| **The agent contract** — what an agent declares | every agent in the plan |
| **The model-call path** — intent in, answer out | the 51 T2 and 18 T3 |
| **The agent registry and sandbox** — how one is registered, run and retired | every agent in the plan |

Built in that order, every later agent is **assembly rather than invention**.

---

## Block 0 — the contract *(built 2026-09-13)*

| Agent | Tier | Where |
|---|---|---|
| `HERON-AHR-CON-017` Agent Contract Agent | T1 | `brain/heron_contract.py` |

What an agent declares: input, output, allowed tools, timeout, failure states, retry rules, version.
Plus the thing the registry row asks for and nothing else in the repository does — **it detects a
breaking contract change** between two versions of a contract, rather than leaving it to be found by
whatever called the agent.

Two rules in it are worth naming here because every later block inherits them:

- **Tier and risk are NOT declared in a contract.** [The registry](../../28-agent-registry.md) already
  carries them. A second declaration is the drift this repository keeps having to write about.
- **`retry.on-failures` must name the failure states it retries.** Blind retry is refused —
  [D-21](../../DECISIONS.md) settles that Heron's failures are a bounded set and the classification is
  a table.

Also built, and the reason this block is first: **`tools/new-agent.py`** scaffolds the next agent from
its registry row — module, contract and test, with the metadata header already right. It implements no
agent of its own (`Heron-Agent: none`), and it is what makes block 1 onwards cheap.

---

## Block 1 — the kernel seams · 6 agents *(built 2026-09-14)*

Everything with a model call in it waited on these, so they came before the 69 agents that have one.

| Agent | Where | What it settles |
|---|---|---|
| `HERON-KRN-PRO-011` Prompt / Instruction Registry | `brain/heron_instructions.py` | versioned, testable, and Constitution articles **assembled, never copied** — an instruction repeating an article's wording is refused ([23 §9](../../23-heron-kernel.md)) |
| `HERON-KRN-MDL-010` Model Router | `brain/heron_router.py` | Heron declares **intent**; no model id is written anywhere ([23 §8](../../23-heron-kernel.md), closes the [D-11](../../DECISIONS.md) tension). Confidential narrows and never widens |
| `HERON-KRN-MAV-017` Model Availability & Fallback | `brain/heron_availability.py` | a fallback answer is marked degraded, and a degraded answer is **not evidence toward promotion** ([24](../../24-trust-model.md)) |
| `HERON-KRN-TOK-015` Token & Cost Budget | `brain/heron_budget.py` | spend is only what a provider reported — Heron has no tokeniser ([D-58](../../DECISIONS.md)). Background yields before a person's work |
| `HERON-KRN-SEC-012` Secret Manager | `brain/heron_secrets.py` | a handle travels, a value does not, and nothing prints one (Article 17, [12 §5a](../../12-security-and-permissions.md)) |
| `HERON-KRN-EVT-004` Event Bus | `brain/heron_events.py` | handlers notify and may not act; order kept, failures recorded, cycles stopped ([23 §1](../../23-heron-kernel.md)) |

**Three instructions exist so far**, keyed by permission level as the Constitution asks — `agent.base`,
`agent.read`, `agent.modify` — and 13 of the 30 articles are assembled by something.

**What Block 1 deliberately did not do:** none of these calls a model. The router says *who* should
answer, availability says *whether they are there*, the budget says *whether it may be afforded*, and
the instruction registry says *what they would be told*. The call itself belongs to the adapter, and
under [D-01](../../DECISIONS.md) the host owns the conversational half of that.

---

## Block 2 — the agent spine · 6 agents *(built 2026-09-14)*

| Agent | Where | What it settles |
|---|---|---|
| `HERON-AHR-REG-008` Agent Registry | `brain/heron_agents.py` | the record is **assembled, never stored** ([D-40](../../DECISIONS.md)); health and performance report as **unmeasured**, never as zero |
| `HERON-AHR-WFP-015` Workforce Planning | `brain/heron_workforce.py` | the agent that says **no** — three of its five answers are |
| `HERON-AHR-SBX-016` Agent Sandbox | `brain/heron_sandbox.py` | every door shut, every knock **recorded**; a sandboxed run is never evidence |
| `HERON-AHR-VAL-013` Agent Validation | `brain/heron_validation.py` | everything checkable, checked; **never the agent that built it** |
| `HERON-AHR-DEP-012` Agent Deployment | `brain/heron_deployment.py` | [docs/24](../../24-trust-model.md)'s gate table, made into something that refuses. **No machine signs** |
| `HERON-AHR-RET-010` Agent Retirement | `brain/heron_retirement.py` | archive, never delete; rollback is **checked**, not hoped for |

**Workforce Planning is in this block on purpose.** It is the agent that says *no* — does a capability
already cover this, can an existing agent be extended, is this a fragment rather than an agent. Built
late, it guards nothing; built here, it guards every agent of block 4.

**What this block found, the day it was built:** validation refused nine of the ten agents already in
the branch, every one for the same thing — a contract declaring failure states its code never named.
The fix went into the code, not the contracts. `python brain/heron_validation.py --all` is the live
number; on 2026-09-14 it read 13 passing against 81 with no contract at all, which is the backlog
`brain/heron_agents.py` reports.

---

## Block 3 — the factory · 8 agents *(built 2026-09-14)*

The hiring lifecycle from [28 §"the full hiring lifecycle"](../../28-agent-registry.md): HR writes the
job description, the Architect designs the contract, the Builder implements, the Creator owns the
pipeline and **may only ever assign `PROPOSED`**, the Trainer supplies the standards, the Mentor pairs
it with the proven agent that owns that capability, the Evaluator scores it after activation, the
Optimizer improves it.

`HERON-AHR-HR-002` · `ARC-003` · `BLD-004` · `CRT-006` · `TRN-005` · `MEN-014` · `EVL-007` · `OPT-009`

**No agent approves itself** ([Golden Rule 7](../../14-golden-rules.md)) and nothing here changes that:
the factory produces evidence, a person still signs.

**And it turned out to be the shape of the whole block rather than a caveat on it.** Each of the eight
is built around something it refuses to do, and in five cases the refusal is the agent declining to do
the thing that would make its own numbers better:

| Agent | What it will not do |
|---|---|
| `HR-002` | write a job description for a proposal Workforce Planning did not clear — the guard has no way round it |
| `ARC-003` | grant a tool the job description never asked for; a contract may narrow a job and never widen one |
| `TRN-005` | read an empty Risk column as `READ`, or hand over a DRAFT agent as an approved example |
| `MEN-014` | name a winner when student and mentor diverge — the senior is proven against the cases somebody thought of |
| `EVL-007` | count a refusal the contract declares as a failure ([`GAP-001`](../../28-agent-registry.md) found 38 of 176 were the executor working) |
| `OPT-009` | propose raising a timeout past the runs that broke it, or declaring a defect's state so it reads as a correct refusal |
| `CRT-006` | assign any status but `PROPOSED`, with ADMIN and no parameter that could carry one in |
| `BLD-004` | run what it generated ([Q-56](../../OPEN-QUESTIONS.md)), or write the test — `docs/24` refuses TESTING when the author and the implementer match |

**Two things the block found in code that already existed.** `tools/agent-count.py` was parsing columns
1–4 and 6 of the register and skipping 5, so **no record had ever carried a risk level**; and
`brain/heron_agents.record()` filled `claims` only when `agents` was omitted, so every caller that
already held the register got an `AttributeError` three frames down. Both are fixed.

---

## Block 4 — the departments

In this order, because each one reuses the blocks above and, where it matters, the department before it.

| Order | Department | Agents | Note |
|---|---|---|---|
| 1 | Operations, Health & Resilience | 9 | all T1, all on the Event Bus |
| 2 | Workspace & Folder Architecture | 8 | |
| 3 | Naming & Taxonomy | 7 | |
| 4 | Documentation | 6 | |
| 5 | GitHub | 10 | |
| 6 | Installation & Update | 10 | PowerShell parts need Windows to **test** |
| 7 | Development | 17 | |
| 8 | Fragment Lifecycle | 7 | |
| 9 | Skill Lifecycle | 5 | |
| 10 | Learning & Self-Growth | 4 | |
| 11 | Reporting & Output | 4 | |
| 12 | User & Personalization | 3 | |
| 13 | Knowledge & RAG | 3 | |
| 14 | Kernel & Platform (remainder) | 2 | Content Trust · Workflow Optimizer |
| 15 | MCP / Bridge | 2 | |
| 16 | Orchestration | 1 | Fix Agent |
| 17 | Import & Migration | 14 | **proof needs a model** |
| 18 | Standards & BIM QA | 13 | **proof needs a model** — last for that reason |

---

## What this plan does NOT claim

**Built is not proven.** Every block above produces code and unit tests on a machine with no Revit.
The last two departments in block 4 need a real model before anything they say can be believed, and the
19 Revit Engineering agents cannot be finished here at all.

That is [D-30](../../DECISIONS.md) and it is not negotiable: **the machine gathers, a person signs.**
