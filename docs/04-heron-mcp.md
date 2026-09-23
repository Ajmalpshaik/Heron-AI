# 04 — Heron MCP (Part 2)

> Derived from [Master Specification](00-master-specification.md) §5, §14.
> **[NOTE]** blocks are engineering commentary added during review.

---

## 1. Responsibility

The controlled communication bridge between the AI layer and the Revit environment.

MCP Server, MCP tools, tool registration, connection management, request routing, authentication, health monitoring, connection recovery, capability discovery, MCP version compatibility.

**The MCP layer must not accumulate business logic.** It transports and validates. It does not decide what a duct is.

## 2. MCP Agent Department

| Agent | Responsibility | Tier |
|---|---|---|
| MCP Connection Agent | Establish and hold the connection to Revit | T1 |
| MCP Server Agent | Server lifecycle | T1 |
| MCP Tool Registry Agent | Which tools exist, their schemas | T1 |
| MCP Configuration Agent | Server configuration | T1 |
| MCP Health Agent | Liveness and readiness | T1 |
| MCP Authentication Agent | Who may call | T1 |
| MCP Version Agent | Protocol and Heron version | T1 |
| MCP Compatibility Agent | Client/server/add-in version matching | T1 |
| MCP Recovery Agent | Reconnect, replay, backoff | T1 |
| MCP Logging Agent | Structured request/response logging | T1 |
| MCP Security Agent | Permission enforcement at the boundary | T1 |

**[NOTE]** Every agent in this department is T1 — deterministic code. Nothing in the transport layer should ever require a model call. If it does, the logic is in the wrong layer.

---

## 3. **[NOTE]** Tool granularity — the central MCP design decision

There are two opposite ways to expose Revit through MCP, and the choice shapes everything.

### Option A — Thin, generic tools (few, powerful)

```text
revit_query(filter)        -> elements
revit_modify(operation)    -> result
revit_execute(script)      -> result
```

- Very flexible; the AI composes behaviour.
- **Dangerous:** `revit_execute` with arbitrary code is effectively remote code execution against the user's live project model.
- Hard to permission meaningfully — one tool covers both "count the ducts" and "delete the building".
- Poor for the Fragment system: nothing reusable is captured.

### Option B — Thick, specific tools (many, narrow)

```text
revit_select_by_category(category)
revit_move_elements(uniqueIds, vector)
revit_get_parameter(uniqueIds, parameterName)
revit_set_parameter(uniqueIds, parameterName, value)
```

- Each tool has a clear risk level and a clear permission gate.
- Each maps naturally onto a **Fragment**.
- Testable, cacheable, auditable.
- Costs more to build; the tool list grows.

**Recommendation: Option B as the contract, with a strictly gated escape hatch.**

The generic `revit_execute` capability should exist only:

- in **Developer Persona**,
- behind an explicit `ADMIN` permission,
- and ideally **never against a live project model** without a preview or a detached copy.

Tracked as decision **D-03**, [Q-5](OPEN-QUESTIONS.md).

**[NOTE]** There is a second reason to prefer B. MCP tool schemas are sent to the model on every request. A catalogue of 300 fine-grained Revit tools is a large, permanent context cost. The mitigation is **capability discovery** (already in the spec §5): expose a small stable core set plus a `heron_find_capability` search tool, and register the specific tool only once it is actually needed. This should be designed in from the start, not retrofitted.

---

## 4. **[NOTE]** Where the permission gate lives

Permission enforcement must happen **at the MCP boundary, in the add-in**, not only in the AI layer.

Reason: the AI layer is a persuadable component. Prompt injection through an imported document, a model comment, a family name, or a shared parameter description could in principle induce the model to call a destructive tool. The boundary that enforces "delete requires confirmation" must be code the model cannot talk its way past.

Concretely:

1. Every tool declares a **risk level** in its registry entry.
2. The add-in enforces the gate — high-risk tools return `REQUIRES_CONFIRMATION` and a description of the intended effect, rather than executing.
3. Confirmation is granted by the **user**, through a UI Heron controls, and is scoped to that one call.
4. The audit log records the request, the gate decision and the confirmation.

This is the practical implementation of Golden Rule 9.

**What was built differs, and §8 says where.** No tool returns `REQUIRES_CONFIRMATION`. The add-in's
gate is one switch per risk level, and `revit_change` keeps its change at once with no preview — a
recorded exception, [D-99](DECISIONS.md#d-99--a-change-asked-for-in-a-chat-is-kept-at-once-with-no-preview-and-article-9-says-so).

---

## 5. **[NOTE]** Version compatibility triangle

Three independently versioned pieces must agree:

```text
Heron MCP Server  <---->  Heron Revit Add-in  <---->  Revit itself
       ^
       |
   MCP protocol version / AI client
```

A user will end up with a new MCP server and an old add-in — this is the most common real-world failure mode, because the add-in requires a Revit restart to update and the server does not.

Requirements:

- The add-in advertises its version on connect.
- The server refuses to operate outside a declared compatible range, with a clear message: *"Heron add-in in Revit 2024 is version 0.3.1, this Heron needs 0.4.x. Restart Revit to finish updating."*
- Never silently degrade. A half-updated pair that mostly works is worse than a clean refusal.

---

## 6. **[NOTE]** Failure and recovery behaviour

The MCP Recovery Agent must distinguish clearly between:

| Condition | Correct response |
|---|---|
| Revit not running | Tell the user. Do not retry in a loop. |
| Revit running, add-in not loaded | Health check + install/repair guidance. |
| Add-in loaded, pipe not answering | Bounded reconnect with backoff. |
| Pipe answering, Revit busy (modal dialog, user mid-command) | Wait with a visible "Revit is busy" state, then time out cleanly. |
| Operation ran and failed inside Revit | Not a transport failure — pass the real error up to Failure Analysis. |

The spec's rule applies at every level: **do not blindly retry the same failed action.** Retry is correct only for transport-level faults, never for a Revit operation that genuinely failed.

---

## 6b. **[NOTE]** Unavailable, unsupported and not-detected are three different answers

§6 separates five failure conditions because collapsing them produces one useless sentence. The same
rule applies one level up, to the question *"can Heron do this right now?"*, and it was not being
applied.

[`heron_capability.resolve()`](../brain/heron_capability.py) returns `None`, correctly, for reasons that
are not alike: nothing provides the capability at all · something does, but not on this Revit release ·
something does, and nothing has told Heron which release this is · something does, and the trust gate
will not permit it · something does, and there is no Revit connected to run it in.

**A planner given `None` can only say "I cannot".** The five sentences a modeller needs are completely
different — a capability gap, a version answer, a button to press, a setting to change, and *"nothing
has told me which Revit this is"* — and only the last four are actionable.

So the verdict carries its reason. [`mcp/server/heron_runtime.py`](../mcp/server/heron_runtime.py) turns
the five into named outcomes beside `AVAILABLE`, and two properties of it are the part worth keeping
rather than the vocabulary:

**Every fact arrives as an argument.** It discovers nothing itself, exactly as
[`heron_health.assess()`](../mcp/server/heron_health.py) does, which is what makes it testable on a
machine with no Revit, no bridge and no config — and that is the only way it was ever going to be tested.

**Not detected is not unavailable.** If nothing has said which Revit is in front of Heron, whether a
2027-only provider applies is *unknown*, not *no*. Reporting unknown as a refusal is a guess presented
as a finding, and it is what makes a platform feel broken when it is merely uninformed. The order the
verdicts are decided in follows from that: the certain facts answer first, and a release nobody has
stated cannot answer before a provider list that genuinely has nothing.

**Nothing offers it as a tool yet**, deliberately — that changes the risk table in §4's sense and is a
decision with a human in it. [`PROPOSALS.md` F1](PROPOSALS.md).

---

## 6a. **[NOTE]** Units — fix the convention at the tool boundary

Revit stores lengths internally in **decimal feet**, regardless of what the user sees on screen.
Surveyed Revit MCP servers take **millimetres** at the tool boundary and convert
([26 §6](26-prior-art-revit-mcp.md)).

Heron must fix this explicitly, because a silent unit mismatch is the classic wrong-by-304.8 error and
it is silent right up until someone looks at the model.

**Rules:**

1. **Tool schemas declare the unit in the parameter name or description**, always — `offsetMm`, not
   `offset`.
2. **Conversion happens once, at the add-in boundary**, never scattered through fragments.
3. **Results state the unit they are reporting in.** *"Moved 247 ducts up 200 mm"*, never *"moved 247
   ducts up 200"*.
4. **The preview shows the unit** ([Golden Rule 17](14-golden-rules.md)) — it is the last point at which
   a human can catch a factor-of-1000 mistake.
5. **Never infer the unit from the user's phrasing.** *"Move it up 200"* is ambiguous and must be
   clarified, not guessed — the same principle as never guessing which Revit
   ([25 §3](25-multi-session-and-binding.md)).

**[NOTE]** Angles, areas and volumes have the same problem and are easier to get wrong because they are
less often checked by eye. The convention should cover every unit type, not just length.

---

## 7. **[NOTE]** Long-running operations

Some operations legitimately take minutes (large model export, a standards check across 200k elements). MCP tool calls should not simply block for that long.

Design for it from the start:

- Long operations return a **job id** immediately.
- `heron_job_status(jobId)` polls; progress flows back to the user as `Working... 40%`.
- The Revit-side handler must yield between chunks so Revit stays responsive.
- Every job is cancellable, and cancellation rolls back the transaction group.

If this is not designed in early it becomes very expensive to retrofit, because it changes every tool signature.

---

## 8. **[NOTE]** What every chat gets from the server — built 2026-09-23

Four things, each derived from something that already owned the answer, so none of them is a second copy
to drift. The tool list itself is `python mcp/server/heron_tools.py`, never a list typed here.

### 8.1 A door that only reads — `revit_read`

The add-in has had `run_fragment_read` since [D-28](DECISIONS.md): it runs a fragment with **no transaction
open**, so Revit itself refuses any change, and it is declared `ANALYZE`, below the read-only ceiling —
so it runs with **Changes OFF**. Until this tool nothing but the command line sent it, and a chat could
reach a read only through `revit_change`, with Changes ON, inside a transaction.

- **Its ceiling is its own declared risk.** `revit_read` is `ANALYZE` because its operation is, and
  `heron_tools.door_refusal` refuses any fragment declared above that — `EXECUTE` included, because a
  fragment that only changes the selection needs no transaction and Revit would not stop it. An
  unreadable risk is refused too. The refusal comes **before** any session is bound or any code is read.
- **It aims at the pinned model** through the same helper `revit_change` uses (`_aim_at_pin`), and says
  so when that model is not the one in front.
- **It says how far what it ran is proven**, read from the fragment's own files: `PROVEN` on a named
  model and unchanged since, `PROVEN` but its code has moved (so the proof is stale — [D-30](DECISIONS.md)),
  or never proved.
- **It is not the generic executor [D-03](DECISIONS.md#d-03--mcp-tool-granularity-thick-and-specific)
  rules out.** It takes a capability, never code: the source that runs is the library's own fragment,
  with its declared risk and its proof.

`tests/test_read_door.py` holds the ceiling over every fragment in the library, and
`tests/test_mcp_serves.py` calls the tool through a real SDK against a stand-in Revit.

### 8.2 `revit_change` keeps its change at once — and says so

The owner chose on 2026-09-23 to keep `revit_change` changing at once rather than add a preview
([D-99](DECISIONS.md#d-99--a-change-asked-for-in-a-chat-is-kept-at-once-with-no-preview-and-article-9-says-so)),
and Article 9 carries it as its one recorded exception. D-99 names what stands in for the preview. It
also records what it does **not** change: **Article 7** still asks for explicit confirmation of a
destructive change, and on this path nothing in code asks for it —
[FRAGMENT-ISSUES row 5b-161](FRAGMENT-ISSUES.md).

### 8.3 The rules reach the AI — the server's instructions

The server is built with **instructions**: the Instruction Registry's `host.chat`
([`brain/instructions/host.chat.yaml`](../brain/instructions/host.chat.yaml)), with the Constitution's
Articles **assembled at start-up, never copied** ([23 §9](23-heron-kernel.md)). A host reads them once
per chat, so they are short — `tests/test_instructions.py` holds a word budget — and they carry three
house rules beside the Articles: take a change back with **Revit's own Undo**, never with a reversing
change; a passing check means the model meets the values checked, **never "compliant"** — the engineer or
the authority decides; and **an unfamiliar word is looked up, and asked about** when nothing records it
([D-33](DECISIONS.md#d-33--heron-never-assumes-an-input-it-asks--and-it-asks-once),
[D-34](DECISIONS.md#d-34--herons-own-wording-is-english-understanding-the-user-is-not-herons-job)).

If they cannot be assembled the server **still starts**, and says so in the same place the rules would
have been. The Constitution's Enforcement table now says which Articles reach every chat this way and
which do not, and the registry test fails when the two disagree.

### 8.4 Safety labels — advice to the host, never the gate

Every tool carries MCP's `readOnlyHint`, `destructiveHint` and `idempotentHint`, **read off its risk in
`heron_tools.TOOLS` as it registers** — never typed at a tool:

| Risk | Read-only | Destructive | Idempotent |
|---|---|---|---|
| `READ`, `ANALYZE`, `SUGGEST` | yes | no | yes |
| `EXECUTE` | no | no | yes |
| `MODIFY`, `PUBLISH`, `ADMIN` | no | yes | **no** — asking twice can do the work twice |

`openWorldHint` is not set: risk says what a tool can change, not where it reaches. A host may call a
read-only tool without asking, which is why a **write labelled read-only** is the one wrong label that
matters, and why the labels are derived rather than written. **The gate stays in the add-in (§4).**
