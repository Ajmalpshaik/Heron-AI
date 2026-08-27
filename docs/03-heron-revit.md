# 03 — Heron Revit (Part 1)

> Derived from [Master Specification](00-master-specification.md) §4 (Part 1), §11, §12, §13, §24.
> **[NOTE]** blocks are engineering commentary added during review. This document carries the
> hardest technical constraints in the whole platform — read it before designing anything else.

---

## 1. Responsibility

Everything that directly touches Revit: the add-in, ribbon, connection, document, views, elements, parameters, transactions, families, worksets, links, warnings, selection, model operations, the Revit API itself, Revit versions, and deployment.

## 2. Revit Agent Department

| Agent | Responsibility | Likely tier |
|---|---|---|
| Revit Connection Agent | Is Revit reachable, is a session live | T1 |
| Revit Document Agent | Active document, open documents, doc state | T1 |
| Revit Application Agent | Application-level API surface | T1 |
| Revit UI Agent | UI-thread concerns, dialogs, task dialogs | T1 |
| Revit Ribbon Agent | Ribbon tab, panels, buttons | T1 |
| Revit Selection Agent | Read and set selection | T1 |
| Revit Element Agent | Element CRUD, geometry access | T1 |
| Revit Category Agent | Category resolution and mapping | T1 |
| Revit Parameter Agent | Read/write parameters, shared params | T1 |
| Revit Family Agent | Families, types, loading, placement | T1 |
| Revit View Agent | Views, sheets, view templates | T1 |
| Revit Workset Agent | Worksets, ownership, sync | T1 |
| Revit Link Agent | Linked models, coordination | T1 |
| Revit Transaction Agent | Transactions and transaction groups | T1 |
| Revit Warning Agent | Warnings, failure handling | T1 |
| Revit Performance Agent | Timing, element counts, cost limits | T1 |
| Revit Export Agent | Export operations | T1 |
| Revit Import Agent | Import operations | T1 |
| Revit API Agent | API knowledge and correct usage | T2 |
| Revit Version Agent | Which Revit versions are installed / targeted | T1 |
| Revit Compatibility Agent | Will this fragment run on this version | T1/T2 |
| Revit Deployment Agent | Build and deploy the add-in | T1 |

## 3. Revit Plugin Health

The **Revit Plugin Health Agent** verifies:

- Is Revit installed?
- Is the required Revit version available?
- Is the Heron Add-in installed?
- Is the Add-in loaded?
- Is the ribbon available?
- Is the MCP connection active?
- Is the correct MCP version running?
- Is the Revit document available?
- Are required tools registered?

**[NOTE]** Health must be a **single structured object** with one overall verdict plus per-check detail, not a wall of text. It is consumed by the installer, by the self-healing system, by the user-facing "is Heron working?" answer, and by the failure analyser. One shape, four consumers.

---

## 4. **[NOTE — CRITICAL]** The Revit API threading constraint

**This is the constraint the entire platform is built around, and the master specification does not mention it.**

The Revit API can only be called:

- from the **Revit main thread**, and
- inside a **valid Revit API context** — i.e. within an `IExternalCommand.Execute`, an `IExternalEventHandler.Execute`, an `Idling` handler, or a document/application event handler.

An MCP server is a **separate process**. It cannot call the Revit API. Not from a background thread, not by holding a `Document` reference, not ever. Attempting it throws `InvalidOperationException` or corrupts the session.

### Consequence

Every single Revit operation must be **marshalled back onto the Revit main thread**. The standard mechanism is:

```text
External process (MCP server)
        |
        |  IPC (see §5)
        v
Heron Add-in listener (background thread inside Revit.exe)
        |
        |  enqueue request
        v
ExternalEvent.Raise()
        |
        |  Revit calls back on the main thread, in API context
        v
IExternalEventHandler.Execute(UIApplication)
        |
        |  do the real Revit work here
        v
result -> back through IPC -> MCP -> AI
```

### Rules this forces

1. **One request queue, one `ExternalEvent`, one handler.** Not one event per operation — Revit has a limited appetite for registered external events, and a single queue makes ordering, cancellation and timeouts tractable.
2. **Every operation is asynchronous from the caller's point of view.** The MCP tool call returns after the round-trip completes; the tool layer must own timeouts.
3. **`ExternalEvent.Raise()` is a request, not a guarantee.** Revit fires it when idle. If a modal dialog is open, or the user is mid-command, nothing happens until they finish. Heron must detect and surface "Revit is busy" rather than hanging.
4. **Nothing may block the Revit main thread.** No synchronous waits inside the handler. A long operation reports progress and yields.
5. **Never cache a `Document` or `Element` across handler invocations.** Cache `UniqueId` strings and re-resolve. See §7.

**[NOTE]** An alternative to `ExternalEvent` is the `Idling` event, which lets Heron poll a queue. It is more forgiving about API context but fires constantly and is easy to abuse into a performance problem. Recommendation: `ExternalEvent` as the primary mechanism, `Idling` only for lightweight liveness/heartbeat. Tracked as [Q-4](OPEN-QUESTIONS.md).

---

## 5. **[NOTE — CRITICAL]** Transport between the MCP server and the add-in

The spec assumes the bridge works but never defines it. Options:

| Transport | Pros | Cons |
|---|---|---|
| **Named pipes** (`\\.\pipe\heron`) | Windows-native, fast, no port conflicts, local-only by construction, ACL-securable | Windows-only (fine — Revit is Windows-only) |
| **Localhost HTTP / WebSocket** | Trivial to debug, any language, easy streaming | Port conflicts, firewall prompts, must bind `127.0.0.1` only, needs auth or any local process can drive Revit |
| **gRPC over localhost** | Typed contracts, streaming, codegen | Heavier, more moving parts |
| **File/socket hybrid** | Simple | Latency, polling, fragile |

**Recommendation: named pipes**, with the add-in as the pipe *server* (it has the longer lifetime and owns the Revit session) and the MCP server as the client. Local-only by construction removes an entire class of security problem, and pipe ACLs restrict access to the current user.

**Multi-instance problem:** a user can have Revit 2023 and Revit 2025 open at the same time, or two documents in one Revit. The pipe name must therefore encode the Revit version and process ID (`heron.{version}.{pid}`), and Heron must handle "which Revit do you mean?" as a first-class case — either by asking, or by binding to the session that has focus.

Tracked as decision **D-02**, [Q-2](OPEN-QUESTIONS.md).

---

## 6. **[NOTE — CRITICAL]** Undo, transactions, and the one-Ctrl+Z guarantee

The user must always be able to undo anything Heron did with **one** undo.

Proposed hard rule, to become Golden Rule 16:

> Every Heron operation that modifies the model runs inside exactly one `TransactionGroup`, assimilated on success, named after what the user asked for.

So "move ducts 200 mm up" appears in Revit's undo stack as a single entry reading **"Heron: Move ducts 200 mm up"**, regardless of how many transactions the implementation used internally.

Rules:

1. Read-only operations open **no** transaction.
2. A failed operation **rolls back completely**. No partial model changes, ever.
3. `TransactionGroup.Assimilate()` on success; `RollBack()` on any failure.
4. Never leave a transaction open across an `ExternalEvent` boundary.
5. Failure handling (`IFailuresPreprocessor`) is explicit — silently swallowing Revit warnings is how models get quietly corrupted.

---

## 7. **[NOTE]** Element identity across calls

`ElementId` is **not stable**. It is per-document and per-session, and it changes on copy/paste, on some sync operations, and between the central and local file.

- Use `Element.UniqueId` (a GUID-based string) for anything Heron stores, remembers, or sends across the IPC boundary.
- Resolve `UniqueId → Element` inside the handler, on demand.
- **Revit 2024 changed `ElementId` to 64-bit.** `ElementId.IntegerValue` was deprecated in favour of `ElementId.Value` (`long`). Any code targeting both 2023 and 2024+ needs conditional compilation or reflection. This is a concrete, immediate example of the reverse-compatibility problem in §11 of the spec.

---

## 8. Version & runtime matrix

> **[D-05](DECISIONS.md), 2026-08-27: Heron supports Revit 2020 through the latest release, and every future release.**
>
> Full strategy — multi-targeting, adapter layer, build matrix, test tiering, annual release routine —
> is in **[16 — Version Support Strategy](16-version-support-strategy.md)**. The summary below is the
> constraint; that document is the plan.

**[NOTE — must verify, do not guess]** The spec correctly says the matrix must be **determined from the build environment, not guessed**. Below is the *starting hypothesis to verify against the installed SDKs* — it must not be treated as fact until checked.

| Revit | Runtime (to verify) | Notes |
|---|---|---|
| 2020 | .NET Framework 4.7.2 | |
| 2021 | .NET Framework 4.8 | |
| 2022 | .NET Framework 4.8 | |
| 2023 | .NET Framework 4.8 | |
| 2024 | .NET Framework 4.8 | `ElementId` becomes 64-bit |
| 2025 | .NET 8 | **Hard runtime break** |
| 2026 | .NET 8 | |
| 2027 | unknown | Do not assume |

### The .NET 8 break is the biggest single compatibility fact in the project

Revit 2025 moved from .NET Framework to .NET. **One assembly cannot target both.**

**Settled approach:** multi-targeting from a single source tree —
`<TargetFrameworks>net48;net8.0-windows</TargetFrameworks>` — with per-version compilation symbols and
all version-conditional code confined to an adapter layer. Branch-per-version is explicitly rejected:
a fix would have to be cherry-picked into seven branches forever, and Golden Rule 4 would become
unverifiable because there would be no single thing to test.

**Sequencing:** supporting all eight versions is a requirement of the finished platform, not of the
first commit. The vertical slice is built and proven on **one** version with the multi-target structure
already in place, then the second runtime is added, then the rest fan out. See
[16 §8](16-version-support-strategy.md).

---

## 9. **[NOTE]** Worksharing / central models

Real projects are worksharing-enabled. Heron will hit:

- elements **owned by another user** → modification fails
- elements **not yet checked out** → needs a checkout request
- **sync with central** while Heron is mid-operation
- **workset visibility** hiding elements Heron expects to find

The Revit Workset Agent must handle ownership as a normal, expected outcome — not an error. And Heron must **never** trigger a Sync With Central on its own initiative. That is a `PUBLISH`-level action at minimum.

---

## 10. **[NOTE]** Preview before modify

Permission levels (§54) gate *whether* an action may run. They do not tell the user *what it will do*.

Recommendation: any `MODIFY`-level model operation supports a **dry run** that reports the intended effect without a transaction:

```text
Heron: This will move 247 ducts up by 200 mm.
       12 of them are owned by another user and will be skipped.
       Proceed?
```

This is cheap to build, catches the majority of "the AI did something unexpected to my model" incidents, and is the single feature most likely to make BIM professionals trust the tool.

---

## 11. Deployment

The add-in is deployed as a `.addin` manifest plus assemblies, per Revit version:

```text
%ProgramData%\Autodesk\Revit\Addins\<version>\    (all users)
%AppData%\Autodesk\Revit\Addins\<version>\        (current user)
```

**[NOTE]** Prefer the **per-user** location — it needs no administrator rights, which matters when the target user is a BIM modeller on a locked-down corporate machine. Requiring admin at install time will stop adoption dead in a large contractor's IT environment.

**[NOTE]** Assemblies loaded into Revit cannot be unloaded. Updating Heron's add-in requires a **Revit restart**. The update system (§10) must know this and say so plainly rather than appearing to have updated something that has not changed in memory.

---

## 12. Reverse compatibility (§11 of spec)

When a newer Revit API capability appears:

1. Can the existing implementation still work? → **keep it**
2. Can the capability be added without touching old paths? → **add it**
3. Is an adapter required? → **adapter**
4. Only if none of the above → **version-specific implementation**

Every change runs the [Regression Testing](13-testing-and-quality.md) matrix across all declared supported versions before it can be promoted.
