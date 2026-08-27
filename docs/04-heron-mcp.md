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
