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

> **As built (2026-10-18):** [D-03](DECISIONS.md#d-03--mcp-tool-granularity-thick-and-specific) accepted
> Option B. The tool surface is a list of thick, specific tools; `python mcp/server/heron_tools.py`
> prints it with each tool's risk. The fragment library is not one tool per fragment. It is reached
> **by capability** through three doors, `revit_read`, `revit_show` and `revit_change` (§8.1, §8.9), and
> never by code or fragment id. The library's own fragment is what runs. `heron_lookup` is the search
> tool this note asks for, and `heron_method` hands over a job of several steps (§8.7). The small core
> set is `heron_tools.ALWAYS_LOADED`, given to every chat at start; the host's own tool search finds
> the rest (§8.6). No generic `revit_execute` exists. The nearest thing is `revit_change` running a
> library fragment declared `ADMIN`, which works only while the owner's Admin switch is on
> ([D-106](DECISIONS.md#d-106--admin-and-publish-each-get-a-switch-of-their-own-and-only-the-owner-turns-them-on)).

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
gate is one switch per risk level: Changes (`write.enabled`, off by default,
[D-19](DECISIONS.md#d-19--writing-is-off-by-default-until-the-write-path-has-met-a-real-revit)), Admin
and Publish ([D-106](DECISIONS.md#d-106--admin-and-publish-each-get-a-switch-of-their-own-and-only-the-owner-turns-them-on)).
The confirmation is Revit's own **Keep or Discard** box. It appears after the run, while the
transaction group is still open, so Discard takes the whole change back
([D-126](DECISIONS.md#d-126--revit-asks-the-modeller-keep-or-discard-before-a-change-from-a-chat-is-kept),
superseding [D-99](DECISIONS.md#d-99--a-change-asked-for-in-a-chat-is-kept-at-once-with-no-preview-and-article-9-says-so)'s
*kept at once*). It is shown while the owner's *Ask to Keep* switch is on, and that switch is **off by
default**. Off, `revit_change` keeps its change at once, as D-99 recorded
([D-138](DECISIONS.md#d-138--the-keep-or-discard-question-gets-a-switch-on-the-ribbon-and-its-box-can-keep-the-rest-of-one-job-without-asking)).
A DRAFT's one run and a publish are always asked.

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

> **As built (2026-10-18):** the gate is the **protocol number**, not a version range. The add-in
> writes its add-in version and protocol version where the client reads them
> ([`BridgeIdentity.cs`](../revit/Heron.Bridge/BridgeIdentity.cs)). The client refuses a bridge whose
> protocol differs from its own `PROTOCOL_VERSION`, with *"Restart that Revit to finish updating. Heron
> will not talk to it until the two agree"*
> ([`heron_bridge_client.py`](../mcp/client/heron_bridge_client.py), `report_mismatched`). The add-in
> version is shown by `heron_version` and gates nothing.

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

> **As built (2026-10-18):** failure analysis is a table, not a model call
> ([D-21](DECISIONS.md#d-21--failure-analysis-is-a-table-not-a-model-call),
> [`heron_failure.py`](../mcp/server/heron_failure.py)). In the add-in, *busy* and *slow* are two
> different waits ([03 §4](03-heron-revit.md)). An unknown outcome on an operation that can write is
> never classified as retryable. A modeller's **Discard** is never retried: it is their answer, not a
> fault ([D-126](DECISIONS.md#d-126--revit-asks-the-modeller-keep-or-discard-before-a-change-from-a-chat-is-kept)
> item 4, [Article 25](../HERON_CONSTITUTION.md)).

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

### 7.1 The first half is built — Heron says it is still waiting (2026-10-17)

**What is built is the "still working" note, and nothing else in the list above.** No job id, no
`heron_job_status`, no percentage, no cancelling. A tool still blocks until Revit answers; what changed
is that the host is told, while it blocks, that the wait goes on.

- **The tools that send a fragment carry the SDK's `Context`**: `revit_change`, `revit_read`, `revit_show`
  (§8.9), `revit_building_loads`, `revit_sprinkler_hydraulics` and `revit_sprinkler_layout` — the ones
  that send a fragment and can wait
  minutes on a slow fragment, a family build, or a modeller answering a prompt in Revit. The SDK finds
  `ctx` by its type, through `_recorded`'s `functools.wraps`, and leaves it out of every tool's arguments.
- **The bridge client's join loop does the timing.** `Bridge.request(..., on_wait=)` hands the callback
  to `_read_line`, which calls it every `WAIT_NOTE_S` with the seconds waited, until the answer or the
  deadline. The server's callback (`_waiting`) sends an MCP progress notification with
  `anyio.from_thread.run`, because the SDK runs a plain tool on a worker thread. The words are
  `WAITING_ON_REVIT` in `heron_mcp_server.py`.
- **Only a host that sent a `progressToken` is told** (`_progress_token`): no token, no callback, and
  the wait runs exactly as it did before.
- **A fault in a note never costs the answer.** The client drops the callback for the rest of that read.
- **Nothing goes down the pipe.** The protocol is unchanged, and Revit is asked nothing more.

`tests/test_mcp_stdio.py` holds it over real stdio, on any system, against
`tests/support/slow_revit_server.py` — the real server in front of a Revit that takes the test's `SLOW_S`
seconds to answer:
a note every `WAIT_NOTE_S` for the host that asked, none for the host that did not. **What Claude Code
does with the notes** — the docs read on 2026-10-17 — is in
[WHAT-TO-INSTALL §5a](WHAT-TO-INSTALL.md#5a-when-a-revit-job-runs-long--claude-codes-own-limits). **Seeing
one in a real chat with a real Revit** is [NEEDS-CHECKING Group EJ](needs-checking/group-ej.md).

**The follow-up, not built: a heartbeat from Revit itself.** The note today says only that *this side*
is still waiting. It cannot say whether Revit is working, has queued the request behind something else,
or is showing the modeller a prompt — only the add-in knows that. Having the add-in write a heartbeat line
down the pipe while it works would let the note say which, but every reader of the pipe expects exactly
one line per request, so it is a change to the protocol and needs a `PROTOCOL_VERSION` bump on both sides
(`heron_bridge_client.PROTOCOL_VERSION`, and the add-in's). The job id, `heron_job_status` and cancelling
in the list above are still not built either.

---

## 8. **[NOTE]** What every chat gets from the server — built 2026-09-23

Four things, each derived from something that already owned the answer, so none of them is a second copy
to drift. The tool list itself is `python mcp/server/heron_tools.py`, never a list typed here.

### 8.1 A door that only reads — `revit_read`

The add-in has had `run_fragment_read` since [D-28](DECISIONS.md): it runs a fragment with **no transaction
open**, so Revit refuses a change, and the add-in counts the model around every read - a read that moved it
is answered `read_changed_model`, never as a read ([row 5b-350](fragment-issues/section-5b-rows-326-350.md)).
It is declared `ANALYZE`, below the read-only ceiling —
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

> **As built (2026-10-18):** D-99 is superseded. Since
> [D-126](DECISIONS.md#d-126--revit-asks-the-modeller-keep-or-discard-before-a-change-from-a-chat-is-kept),
> Revit can ask the modeller **Keep** or **Discard** before a change is kept. Since
> [D-138](DECISIONS.md#d-138--the-keep-or-discard-question-gets-a-switch-on-the-ribbon-and-its-box-can-keep-the-rest-of-one-job-without-asking),
> the owner's *Ask to Keep* switch on the ribbon decides whether it does, and the switch is **off by
> default**. Off, the behaviour below is what runs. Since
> [D-117](DECISIONS.md#d-117--a-change-is-kept-only-from-a-fragment-whose-proof-still-stands),
> `revit_change` keeps a change only from a fragment whose proof still stands. A DRAFT gets D-119's one
> run, which is always asked. The section below is kept as it was written on 2026-09-23.

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

### 8.5 One request, fewer turns — built 2026-09-30

**The cost was the turns, not the brain.** One chat request was four tool calls — `heron_lookup`, then
`heron_resolve` for what to type, then `revit_read` or `revit_change`, then a read to check — and each
call is a whole host turn of seconds. On top of that, every lookup re-parsed every `fragment.yaml`
before it searched: measured 1.87 s of a 1.95 s `heron_lookup`, where the search itself took 13 ms.

What changed, and what did not:

- **`revit_read` and `revit_change` take `request`**, the user's own words, in place of `capability`.
  They run in that one call **only** when the words are a phrasing a `PROVEN` capability declares (the
  identity route with its run-unasked permission, `heron_search.may_run_unasked`) **and** every value it
  needs is in `values`. Anything else — a `DRAFT` exact match, a ranked guess, a missing value — sends
  **nothing** to Revit and hands back the candidates, **every one** with what it needs typed. Two more
  hand it back too (review on PR #369): a card still marked `PROVEN` whose **code changed since its
  proof** (D-30), and a match where the fragment the door would run is **not the provider** the words
  were matched to. **Never a score
  margin:** `heron_retrieve.Contest` measured that a wide lead does not separate a real question from
  an unreal one, and a threshold set here would be the dial D-33 forbids. **D-01 holds:** an exact
  declared phrasing leaves no meaning to decide, and every other case goes back to the host.
- **`heron_lookup` now carries what to type** for its top match, so `heron_resolve` is no longer a
  step of its own.
- **`heron_fragment.load` keeps a card's parse** until the file's bytes change (a content hash, never
  the mtime), and hands every caller a copy.
- **Every gate is where it was**, called in the same order: `risk_refusal`, `door_refusal`, the
  Changes switch in the add-in, the pinned model, one undo entry, D-99. The words path only chooses the
  capability; the door does the rest exactly as if the host had named it.

Measured in a container with no Revit — the bridge stubbed at the point a request would cross
(`tests/test_mcp_serves.py` §10 holds the behaviour). The sample: *select all ducts*, *how many doors on
level 1*, *set Comments on selected pipes to X*, *list sheets with no views*, *colour supply ducts blue*,
and one exact proven phrasing, *how many metres of duct is there*. A write's count includes the host's
read to check it:

| | tool calls, a read | tool calls, a write | Heron-side time, per request |
|---|---|---|---|
| before | 3 | 4 | 3.7 – 4.1 s |
| after, exact proven phrasing | **1** | 2 — derived, not measured: no write in the sample was one | ~0.09 s |
| after, anything else | 2 | 3 | ~0.09 s |

**NEEDS REAL REVIT:** the time Revit itself takes, and the host's own turn time, were not measured here.

### 8.6 A chat starts ready — built 2026-10-01

**What the owner asked:** *"when a chat starts the first time, agents check things like levels - that is
OK - but it also thinks and does other things. It should start the first-time checks as soon as
possible; for the rest, why do we need to check?"* Revit was never the wait: `revit_levels` answered in
0.11 - 0.14 s every time below. What a chat waited on was Heron's own start-up, one search turn, and
words that had nothing to do with Revit.

**Measured on the owner's PC with Revit 2024 open on *heron ai bulding*** - a fresh server each run, the
same store, three runs each, `revit_levels` read-only (the select was not run, so as not to replace a
selection he was working with):

| first `heron_lookup`, asked this long after the server is ready | `main` | this change |
|---|---|---|
| at once | **3 of 3 never answered** (cut off at 200 s) | 5.3 - 7.6 s |
| 8 s | 6.0 - 7.4 s | 1.8 - 2.3 s |
| 15 s | 7.1 - 7.5 s | 1.8 s |

A chat's first Heron call comes 5 - 12 s after the modeller's first message (twelve Revit chats read
back from the transcripts), and the server starts when the chat opens, so the middle row is the
ordinary case.

**What changed:**

- **The server freezes no more when it is asked straight away** (row 5b-272). The encoder's warm-up
  thread loaded numpy's DLL; a lookup on the event loop asked Revit its version, which starts a reader
  thread; Windows cannot start a thread while another holds its loader lock, and the two waited on each
  other. A stack dump of a frozen `main` shows exactly that. `heron_embed.warm()` now loads numpy on the
  calling thread first (0.24 - 0.36 s), after which the whole encoder loads in 0.7 - 0.8 s instead of
  9 - 14 s.
- **The encoder loads from the copy on disk** (`heron_embed._on_disk`) - no request to the hub, which
  waited out its timeout in four starts of five.
- **The store is prepared before anybody asks** (`heron_brain.warm_store`): the one-time part of a
  lookup - every card parsed, rows refreshed, the first encoder pass - runs on a background thread
  behind the encoder, and a lookup arriving meanwhile waits for it rather than doing it twice. **Only**
  for a private store, an install with no git, or the main checkout on branch `main`: a worktree's
  cards are unmerged work and the shared store is every chat's (rows 131, 136).
- **A few tools are given to the chat before it asks** - the lookup, the doors and the first-time
  checks; `heron_tools.ALWAYS_LOADED` is the list, never this sentence - served with Claude Code's documented
  `_meta["anthropic/alwaysLoad"]`. Most Revit chats spent their first turn searching for a Heron tool
  (5 - 10 s). The cost is about 9,000 characters of descriptions in every chat (measured since by a
  command, §8.8); every other tool is still one search away, and nothing about what a tool may do
  changes.
- **A connection check is answered and nothing more is done.** The owner, 2026-10-01: when he asks
  first whether Heron is connected he wants that answer and then to say what is next - *"only need to
  check the connected or not"*. The chat's rules (`host.chat`) and `revit_health`'s own description
  now say so. Its WARNING about Changes being on is unchanged: it is a safety notice.
- **The developer's session line is silent in a modeller's chat** - the main checkout on `main` - and
  runs no git there ([`heron-session`](../.claude/skills/heron-session/SKILL.md)).

**Not changed, and why:** every gate; the per-lookup check that the cards have not moved - it still
cost about 2 s a lookup on this PC, not the 0.09 s §8.5 measured in a container, and fixing it reopened
#369's content-not-mtime choice (row 5b-271, **fixed 2026-10-09** - §8.7); the owner's own plugins and
settings, which are his.

**NEEDS REAL REVIT, IN A REAL CHAT:** the seconds from a modeller's first message to the first answer,
which includes Claude's own turns - [A21](needs-checking/group-a.md).

### 8.7 A new PC as quick as the old one — built 2026-10-09

**What the owner reported, 2026-10-08:** on a new laptop the first *"create a family"* took more than
seven minutes, where the office PC takes one or two, and the chat was seen reading `.md`, Python and C#
files. Investigated in a container with no Windows PC and no Revit; [row 5b-378](FRAGMENT-ISSUES.md) has
the whole of it.

**The cause Heron owned:** no tool handed over a job's METHOD. `heron_lookup` answered *"create a
family"* with one ranked capability, and the fifteen ordered steps of building a family lived only in
`brain/skills/family-creation.yaml` and [43](43-building-revit-families.md). The office PC's Claude Code
remembers Heron; a new one does not, so it read them off disk.

- **`heron_method`** — a READ tool, given to every chat at start (`heron_tools.ALWAYS_LOADED`), that
  hands over one job's whole method: what must be true first, the steps in order, and every capability
  it uses with its risk and status on this Revit. Empty, it lists every job.
- **`heron_lookup` and every `request=` hand-back name the jobs an answer is one step of**
  (`heron_skill.methods_for`): a skill is named only when it NEEDS a capability retrieval returned, and
  the words of the request only order those (D-01). Not for an exact declared phrasing that may run on
  its own - that request is one step.
- **The chat's rules say it** (`host.chat`): a job of several steps has its method in `heron_method`, and
  Heron's own folder is never read to work out a Revit job. The top of [`AGENTS.md`](../AGENTS.md) - which
  Claude Code reads when there is no `CLAUDE.md` - says the same, and what to do when no `heron` tool is
  listed at all.
- **`tools/heron-ready.py`** builds, once per PC, what a first chat would otherwise build while the
  modeller waits - the knowledge store, the search model - and names every missing package with its
  command ([WHAT-TO-INSTALL 1a](WHAT-TO-INSTALL.md)).
- **A lookup opens no file when nothing moved** (row 5b-271): 1,440 opens to 0 per lookup, measured in a
  container. On the owner's PC those opens were about 1 s of every 2 s lookup - derived, not measured.

**Left for the owner:** a committed `.claude/settings.json` could approve the `heron` server and allow
the read-only Heron tools without a prompt on every new PC. That widens Claude Code's own permissions,
which is his decision and not an agent's.

**MEASURED 2026-10-09 on the new laptop** - Windows 11, Revit 2024, Claude Code 2.1.293, `claude-opus-5-5`
at effort *xhigh* on every request; [row 5b-378](FRAGMENT-ISSUES.md) names the sources. The chat called
`heron_method` before any file read, then read `fragment.yaml` for how to write a value
([row 5b-397](https://github.com/Ajmalpshaik/Heron-AI/pull/462), open in #462). A fire alarm control
panel, 22 extrusions, took 367 s from request to finish; a duty/standby pump set, 107 forms, 792 s.
On 2026-10-09 Revit's own time across all 180 fragment runs was 17.2 s, Heron and the MCP client added a
median 0.68 s a batch, and about 93% of the active time was the model's own turns - extended thinking alone about
609 s. **Nothing on the laptop was broken.** What lengthened those turns and is Heron's: the `revit_change`
hand-back, 322,725 characters across the two builds and about 70% of it helpers and a repeated trailer
([row 5b-407](fragment-issues/section-5b-rows-401-425.md)), and one form per call
([Q-62](OPEN-QUESTIONS.md)).

**NOT MEASURED:** the office PC's minutes for the same family, and the model, effort level and fast-mode
setting its chat runs - which this section never considered and `heron-ready --snapshot` does not record
([Q-61](OPEN-QUESTIONS.md)).

**THE `revit_change` HAND-BACK IS A SHORT CHANGE REPORT SINCE 2026-10-17** ([row 5b-407](fragment-issues/section-5b-rows-401-425.md)).
Each reply says what Revit reported the write changed - added, modified and deleted, by category, a
setup step's apart from the fragment's - and names a **run record**, one file per write under
`HeronPaths.Runs` holding every element id and the whole reply, which `heron_run_record` pages back from
disk without Revit. The KEPT paragraph is printed once per batch of calls and the add-in's one sentence,
carrying the undo rule whole, on the rest; a value the last reply of the same capability showed word for
word is counted rather than printed again. Replayed through the real tool, 172 kept changes shaped from
`CREATE_FAMILY_EXTRUSION`'s code went from 213,332 characters to 160,364, 24.8% less. **NOT RUN IN REVIT** - whether
`DocumentChanged` reaches every commit the write path makes is [Group EV](needs-checking/group-ev.md).

**MEASURED 2026-10-11 on the same laptop** - a 165-form generator (156 extrusions, 5 sweeps, 4 revolves)
built in one chat, `claude-opus-5-5` at effort *xhigh* on every request. 1,423 s from request to finish,
and the model's own turns were 95% of it; one planning turn before the first form took 617 s and 60,600
thinking tokens. Revit's own time over the 198 fragment runs of 2026-10-11 was 23.5 s, two-thirds of it
seven fragments compiling on their first run. Heron's server added a median 24 ms to each call outside Revit (38 to 75 ms
measured from the transcript, which adds Claude Code's handling). The search index ran in one call, a
`heron_lookup` of about 0.44 s: every `revit_change` named its capability, which skips retrieval. **The
trained model and re-ranker installed on 2026-10-10 did not measurably slow family builds** - four builds
on each side of the install, 9.47 s a form before and 9.90 after, well inside either side's spread (7.28 to
16.03 s before, 8.62 to 16.45 after), the model's share 93% both times. Their cost is memory
([WHAT-TO-INSTALL §2](WHAT-TO-INSTALL.md)).

**Calls sent together run one at a time, and that is on purpose.** Every tool call holds the server's one
`_revit_lock` (taken in `_recorded`; its comment reads *ONE CONVERSATION WITH REVIT AT A TIME*), so a batch
of ten `revit_change` calls sent in parallel queues in the server: in 173 back-to-back pairs none overlapped,
and the gaps between them came to about 7.2 s over the whole build. Revit would line them up anyway - the
add-in has one ExternalEvent and one queue (`RevitDispatcher.cs`) - so taking the lock away could save at
most those seconds, and would lose what it guards: a second pipe connection displacing the first and losing
its answer ([40 §4](40-heron-companion.md)). One case costs more, read from the code and not seen in this
build: a `heron_resolve`, `heron_method` or `heron_lookup` that arrives while the knowledge store is still
being prepared waits for it inside the lock (`_PREPARING`, `mcp/server/heron_brain.py`), and every other
tool call waits behind it. The lever Heron has is fewer calls, not parallel ones: `CREATE_FAMILY_FORMS`
(DRAFT, [Q-62](OPEN-QUESTIONS.md)) builds many forms in one. Two small per-call costs the measurement found
are rows [5b-427 and 5b-428](fragment-issues/section-5b-rows-401-425.md); the sweep's shape note that #472
left short is [5b-429](fragment-issues/section-5b-rows-401-425.md).

**NOT MEASURED:** what the 617 s turn spent its thinking on - the thinking text is not stored - and the
office PC ([Q-61](OPEN-QUESTIONS.md)).

**THE NEXT RE-TIMING IS TAKEN THE SAME WAY - built 2026-10-17.** Both measurements above were worked out
by hand from the chat's transcript and the add-in's audit trail, the files [row 5b-378](FRAGMENT-ISSUES.md)
names. [`tools/time-family-build.py`](../tools/time-family-build.py) does that hand's work: given the
chat's `.jsonl` and `audit-YYYYMM.jsonl` it prints request to finish, the model's share, thinking tokens
and seconds, the model and effort the chat ran, the Heron calls and the `revit_change` hand-back, Revit's
own time and its first runs, Heron's time around Revit a batch and a call, and forms built and seconds a
form - each with the file and lines it was read from, and **NOT MEASURED** for what those files cannot
show, never an estimate. Heron's server alone is one of those: it keeps each call's seconds in memory
(`heron_companion.Activity`), so the server figure above came from the MCP client's log, which the tool
does not read. [`tests/test_time_family_build.py`](../tests/test_time_family_build.py) holds it to
synthetic transcripts shaped like both builds, with a negative case. **Not yet run on the owner's two
transcripts**, which are on the laptop; its first real use is the re-timing of `CREATE_FAMILY_FORMS`
([D-116](DECISIONS.md)).

**MEASURED 2026-10-17 - one `CREATE_FAMILY_FORMS` call and the fragment warm-up; the chat re-timing NOT
RUN.** On the laptop's Revit 2024.3 one call built 109 forms - a list the size of the pump set - in 3,536 ms
of Revit's own time, rolled back ([Group DA](needs-checking/group-da.md), DA5), where the pump set's 107 forms
one call each had taken 11 batches ([row 5b-378](FRAGMENT-ISSUES.md)). The warm-up took the first fragment
after a Revit start from 4,329 ms to 1,214 ms - OFF against ON, `report-family-forms`, each read a minute
after connecting, the cached run 2 to 4 ms either way ([D-56](decisions/D-56.md)'s baseline: 5,193 ms cold).
A family built by a chat with `CREATE_FAMILY_FORMS` is **NOT RUN**: `revit_change` runs a PROVEN capability
only ([D-117](decisions/D-117.md)) and the card stays DRAFT until its proof is signed, so
`tools/time-family-build.py` has still not timed a build. The group's results say which add-in each run
used and why the warm-up's switch was read from the Claude app's own copy of `heron.config`
([row 5b-1004](fragment-issues/section-5b-rows-401-425.md)).

**MEASURED 2026-10-17, LATER THE SAME DAY - a family built by a chat with `CREATE_FAMILY_FORMS`, timed by
`tools/time-family-build.py`.** Both cards were PROVEN by then ([pull request #624](https://github.com/Ajmalpshaik/Heron-AI/pull/624)).
The laptop's Revit 2024.3 (release 2024.3.6, session 51856), on the add-in whose files are dated 16:33Z that
day - the one DA2 and the warm-up ran on. The owner asked for *"Pump. Just pump. But. Little bit detailed."*
The chat made the family file from `Metric Mechanical Equipment` (`CREATE_FAMILY_DOCUMENT`) and six materials
in one call. It then built a fixed end-suction pump set in five `CREATE_FAMILY_FORMS` calls, one per group -
base frame, pump body, nozzles, shaft and coupling, motor - and made three `COMBINE_FAMILY_FORMS` cuts: the
anchor holes, the suction bore and the discharge bore. Then it looked, as the family method's step 13 says:
`REPORT_FAMILY_FORMS` read 44 forms of 44 and 3 cuts, and `View 1` was pictured. No connectors:
`ADD_FAMILY_CONNECTOR` is DRAFT. The tool read the chat's transcript and the add-in's `audit-202610.jsonl`,
over the window from the owner's request to the picture (`--since 2026-10-17T17:53:25.525Z --until
2026-10-17T18:01:24.569Z`):

| | |
|---|---|
| request to finish | 479.0 s; waiting on the person 0 s |
| the model's own turns | 357.0 s, 74.5%. Thinking 284.3 s and 24,103 tokens; the longest turn 135.4 s (12,653 thinking tokens), the one that worked out every part before the first group. `claude-opus-5-5`, effort *xhigh*, standard speed on all 28 requests |
| tool calls | 122.1 s; 24 Heron calls, 11 of them `revit_change`, whose hand-back came to 28,698 characters |
| Revit's own time | 101.6 s over 18 fragment runs, median 3,044 ms; the 9 first runs in the session 71.4 s |
| Heron's time around Revit | a median 0.439 s a call (middle half 0.341 to 0.529 s; range 0.314 to 3.475 s, 20 calls) |
| Heron's server alone | **NOT MEASURED** - the tool's own reason: the server keeps each call's seconds in memory only |
| forms built, seconds a form | **NOT MEASURED** - the five replies print `formCount` 4, 10, 10, 6 and 14, and the tool reads none of them ([row 5b-1030](fragment-issues/section-5b-rows-401-425.md)) |

**Most of Revit's own time here was the owner's.** Every kept write showed [D-126](DECISIONS.md)'s *"Keep
what Heron just did?"* box, and the audit's `ms` runs round the whole job, box included. Revit's journal
(`journal.0016.txt`) has ten boxes in the window, every one answered *"Keep it"*: 24.8 s and 24.0 s for the
first two - the materials and the first group, the two runs the audit times at 26,570 and 26,140 ms - and 1.6
to 6.4 s for the other eight, 80.9 s in all. The tool cannot see a box and counts it as Revit's
([row 5b-1031](fragment-issues/section-5b-rows-401-425.md)). The 2026-10-09 and 2026-10-11 builds ran before
D-126 had a box, so they compare with this one on the model's share and on Heron's time around Revit, not on
Revit's own time. The transcript and the audit as read are kept at `%TEMP%\heron-work\pump-1017\` on the
laptop, so the tool can be run on them again once row 5b-1030 is fixed.

**RE-RUN 2026-10-18 - the same build, on the tool with rows 5b-1030 and 5b-1031 fixed** ([pull requests
#634](https://github.com/Ajmalpshaik/Heron-AI/pull/634) and [#635](https://github.com/Ajmalpshaik/Heron-AI/pull/635)).
`tools/time-family-build.py` as on `main` at `e7c0301e`, on the same transcript, audit and window, with
`--journal journal.0016.txt --journal-utc-offset +03:00` added. As it prints them: **forms built 44**, read
from the five `CREATE_FAMILY_FORMS` replies; **seconds a form 10.9 s**, request to finish over forms built;
**Revit's own time less the boxes 20.7 s** of 101.6 s (the 9 first runs 16.3 s of 71.4 s), from ten Keep boxes
inside the chat's runs, every one *"Keep it"*, 80.9 s - the count read from the journal by hand above. Heron's
server alone is still **NOT MEASURED**, for the reason above. Every other figure in the table came out the
same but the `revit_change` hand-back, 28,190 characters, not 28,698: the first run counted each escaped
newline and quote in Heron's wrapped reply twice (row 5b-1030), and the tool as it stood at `5d99a553` still
prints 28,698 and no form on the same files.

### 8.8 What the tool list costs a chat — built 2026-10-17

**What the owner asked:** cut what Heron's tool list costs in every chat. [D-03](DECISIONS.md) accepted
the cost in so many words - *"MCP tool schemas cost context on every request"* - and mitigated it with a
small core set, which since §8.6 is `heron_tools.ALWAYS_LOADED`. Nothing measured what that set or any
other tool cost; §8.6's *about 9,000 characters* was a sentence.

- **`python mcp/server/heron_tools.py --sizes`** prints every tool's description, input schema and whole
  served JSON, largest first, with the ALWAYS_LOADED totals and the size of the instructions every chat
  is given. Descriptions are read off the source with `ast` and need no SDK; schemas are written by the
  SDK and say **NOT MEASURED** without one. `--json` is for a machine, and
  [`change-evidence.py`](../tools/change-evidence.py) now records the description totals in every capture.
- **[`tests/test_tool_sizes.py`](../tests/test_tool_sizes.py)** holds the ALWAYS_LOADED descriptions
  together, and every tool alone, to a budget - and holds that no trim drops an argument's name or the
  sentences the two doors stand on, because a budget teaches people to cut words to buy the number back
  (D-100's first reason). **It is a gate: a tool over budget stops a pull request -
  [D-118](DECISIONS.md#d-118--a-tool-description-over-its-budget-stops-a-pull-request)**, the owner's
  answer of 2026-10-17 to the question D-100 settled the other way for the routing score.
- **The description served is the docstring cleaned, on every Python.** Python 3.12 - the owner's - served
  every line indented four spaces, so the same list cost more on his PC than in a container
  ([row 5b-491](fragment-issues/section-5b-rows-401-425.md)).
- **What left a description is said where it is needed**, and kept as a comment under the docstring,
  which no host is served:

  | what | where the host hears it now |
  |---|---|
  | `expect_from`'s `where k=v` form, and why empty is the safe default | `_contract_lines`, under any capability that takes a value from the fragment before it |
  | D-106's Admin and Publish switches, and what each covers | the refusal names the switch that is off (`heron_failure`); one line in `revit_change` says so, and not to route round it |
  | Articles 7, 9 and 10 in full | `host.chat`, every chat's instructions; `revit_change` keeps a one-line pointer to 7 and 10 |
  | `heron_hvac` and `heron_fire`'s lists of calculations | the catalogue an empty `calculation` returns |
  | the questions an ASK ONCE answer asks | that answer, which names them |
  | the full `inputs` shapes of the loads and sprinkler tools | their top-level keys stay; every question names its own path or its project or profile |

**MEASURED 2026-10-17 through the SDK's own `list_tools`**, 45 tools, characters:

| | `main`, Python 3.12 (the owner's) | `main`, Python 3.13 | this change, either |
|---|---|---|---|
| every description | 47,170 | 44,246 | **36,560** |
| the whole list as served | 69,349 | 66,425 | **58,394** |
| ALWAYS_LOADED's six descriptions | 7,258 | 6,814 | **4,292** |
| ALWAYS_LOADED as served | 10,595 | 10,151 | **7,546** |

The ten largest descriptions, read off the source (so without 3.12's indentation):

| tool | before | after |
|---|---|---|
| `revit_change` | 2,870 | 1,309 |
| `heron_fire` | 2,349 | 1,375 |
| `revit_sprinkler_hydraulics` | 2,075 | 1,412 |
| `revit_building_loads` | 2,008 | 1,325 |
| `revit_sprinkler_layout` | 1,826 | 1,211 |
| `heron_hvac` | 1,773 | 1,094 |
| `revit_read` | 1,520 | 1,040 |
| `heron_check` | 1,256 | 808 |
| `revit_export_check` | 1,201 | 650 |
| `heron_research` | 1,190 | 690 |

SDK 2.3.0 on Python 3.12.3 and 2.2.0 on 3.13.16, in a cloud container. *As served* is the tool's whole JSON
- name, description, input schema, output schema, safety labels and `_meta`; what reaches the model is at
least the name, description and input schema, and the rest is the host's to pass or not. Run
`python mcp/server/heron_tools.py --sizes` for today's figures rather than reading these.

**Not changed, and why:**

- **The Companion ANALYZE tools as `heron_method` jobs** - `revit_building_loads`, the two sprinkler tools,
  `revit_edit_table`. Considered and not done: none is in ALWAYS_LOADED, so each costs a chat only once a
  search loads it, and each opens a Companion panel and hands it the reply, which a method - a list of
  steps for the chat to run - cannot do. Removing a tool the modeller's chats call is the owner's decision.
- **Every tool is served with an `outputSchema`**, because each is declared `-> str` -
  6,343 characters across the list - and each result carries the answer twice, as text and as
  `structuredContent` (`tests/test_mcp_serves.py`'s `text_of` reads the pair). Whether Claude Code puts
  either in the model's context was **not measured**. `structured_output=False` would drop both, and it
  changes the shape of every reply, so it is a question rather than part of this change.
- **`host.chat`** - [`tests/test_instructions.py`](../tests/test_instructions.py) already holds its word
  budget.

**NOT MEASURED, AND IT NEEDS A REAL CHAT:** whether Claude Code chooses the right Heron tool as often from
the shorter descriptions. Nothing in a container can show it.

### 8.9 A door that only shows — `revit_show`, and the temporary isolate — built 2026-10-17

**What was wrong:** *"show me / zoom to / isolate these"* needed Changes ON. The add-in's gate has always
let `EXECUTE` through with Changes off — `HeronPermissions.ReadOnlyCeiling` is `Execute`, *"changes what is
shown, never what exists"*, and [12 §1](12-security-and-permissions.md) gives `EXECUTE` *"change selection,
open a view, zoom"* and no confirmation. Only this side held it back: §8.1's door is `ANALYZE`, so an
`EXECUTE` fragment reached Revit only through `revit_change`, inside the write path's transaction
([row 5b-497](fragment-issues/section-5b-rows-401-425.md)).

- **`revit_show` is the third door.** It sends `run_fragment_show`, declared `EXECUTE` in the add-in and
  routed to the read door's executor with **no transaction open** — so Revit refuses a change to the
  model, and the add-in counts the model around the run as it does a read
  ([row 5b-350](fragment-issues/section-5b-rows-326-350.md)) - and what an `EXECUTE` fragment can change is
  the selection, the camera and the active view.
  Its ceiling is its own row, through the same `door_refusal`: `MODIFY` and above are refused before
  anything is sent, and `revit_read` now sends an `EXECUTE` fragment here rather than to `revit_change`.
  Everything else is §8.1's: the pin, the proof line, the words path, `expect_from`.
- **It is one search away, not given before the chat asks.** `heron_tools.ALWAYS_LOADED`'s six left less
  of §8.8's budget than a door's description takes when this was built - `python mcp/server/heron_tools.py
  --sizes` prints what they cost today - and raising the budget is [D-118](DECISIONS.md)'s to decide. `revit_read`'s
  refusal of an `EXECUTE` capability names it, and its description was trimmed to fit its own budget.
  So do `revit_change`'s two refusals a screen-only job meets first - not proven, and Changes off - and
  `heron_method`'s list of a job's capabilities says `EXECUTE` runs through it.
- **Revit's Temporary Hide/Isolate needs a transaction, so it is not a fragment.** `revit_isolate_temporary`
  and `revit_reset_temporary` call two FIXED operations, `isolate_temporary` and `reset_temporary`
  (`RevitTemporaryView.cs`): one transaction round `IsolateElementsTemporary`, `IsolateCategoriesTemporary`
  or `DisableTemporaryViewMode`, in the active view, taking element ids or BIM category words. A
  transaction opened for fragment code with Changes off would be a write path with no switch in front of
  it; these open one for three Revit calls and nothing a caller sends.
- **A DRAFT runs here and says DRAFT, as a read does.** [D-117](DECISIONS.md) refuses a fragment that is
  not PROVEN only where a change is KEPT; its items 3 and 4 leave a run that keeps nothing, and a read,
  unasked. The show door opens no transaction, so it keeps nothing in the model.
- **Inside a link, too, with no question first.** [Article 7](../HERON_CONSTITUTION.md) asks for
  confirmation before *"anything touching a linked model"*, and selecting or zooming to linked elements is
  not touching it — the owner's answer to [Q-65](OPEN-QUESTIONS.md), [D-124](DECISIONS.md). So
  `ZOOM_TO_LINKED_ELEMENTS` runs through `revit_show` like any other `EXECUTE` fragment.

**Two things a risk word cannot say, and both doors with the switches off refuse them** (rows
[5b-591](fragment-issues/section-5b-rows-401-425.md) and [5b-592](fragment-issues/section-5b-rows-401-425.md)):
code that writes to disk - `heron_tools.disk_refusal`, at `revit_read` and `revit_show`, before a session
is bound - and code that opens a transaction of its own, which the add-in refuses on the read and show
paths and under `noTransactionOpen` (`Heron.Core`'s `HeronSourceScan`). Both read the code with comments
and strings blanked, and neither sees what is done through reflection.

**Opening a view, and what still needs Changes ([D-125](DECISIONS.md)):** `OPEN_VIEW` declares
`noTransactionOpen` - *run me with none open* - which the show door lets through, where the edit-scope need
it declared until then was refused ([row 5b-498](fragment-issues/section-5b-rows-401-425.md)). A fragment
whose code needs Heron's transaction is declared `MODIFY`, so `ISOLATE_ELEMENTS` goes through `revit_change`
with Changes on, and the show door's refusal names `revit_isolate_temporary` for Changes off
([row 5b-499](fragment-issues/section-5b-rows-401-425.md)).

`tests/test_read_door.py` section 10 holds the show door over every fragment in the library,
`tests/Heron.Kernel.TestHost` section 9 runs the gate over every switch combination, and
`tests/test_mcp_serves.py` calls all three tools through a real SDK. **None of it has run in Revit** —
[NEEDS-CHECKING Group ES](needs-checking/group-es.md).

### 8.10 What a chat is not offered — built 2026-10-17

- **Off Windows, the library alone**
  ([D-132](DECISIONS.md#d-132--off-windows-the-heron-server-serves-its-library-alone-and-no-revit-tool)).
  A cloud session has no named pipe and no Revit. The server registers only `heron_tools.LIBRARY` there,
  so no `revit_` tool appears, and its instructions open by saying no Revit can be reached
  (`LIBRARY_ONLY` in `heron_mcp_server.py`). On Windows every tool is served, as before.
  `tests/test_mcp_stdio.py` and `tests/test_tool_registry.py` hold it.
- **A question is never offered a change**
  ([D-128](DECISIONS.md#d-128--a-request-the-host-calls-a-question-is-never-offered-a-capability-that-changes-the-model)).
  When the host calls a request a question, it passes `question` to `heron_lookup`. Every capability
  above the level just below the change door's is then left out, the identity route included, and the
  answer says how many were left out ([`heron_retrieve.py`](../brain/heron_retrieve.py)). The host
  decides that it is a question; Heron does not
  ([D-01](DECISIONS.md#d-01--execution-host-claude-code-plugin)).
