# 26 — Prior Art: Existing Revit MCP Servers

> Research carried out 2026-08-27, at the owner's request, into what already exists for connecting
> AI to Revit over MCP — including pyRevit-based approaches.
>
> **Conclusion up front: the bridge problem is well-travelled and the tool layer is a solved shape.
> But every project surveyed shares two gaps that Heron's design already closes — and one of them
> Heron closes because of the owner's own field experience, not because of anything published.**

---

## 1. What exists

At least eight public Revit MCP projects. The substantial ones:

| Project | Stack | Transport | Tools | Revit |
|---|---|---|---|---|
| [mcp-servers-for-revit](https://github.com/mcp-servers-for-revit/mcp-servers-for-revit) | TypeScript MCP server + **C# add-in** + command set | **WebSocket** | 30+ | 2020–2026 |
| [LuDattilo/revit-mcp-server](https://github.com/LuDattilo/revit-mcp-server) | same family | WebSocket | **138** | 2023–2026 |
| [Demolinator/revit-mcp-server](https://github.com/Demolinator/revit-mcp-server) | **Python/FastMCP** + **pyRevit Routes** | **HTTP :48884** | 48 | 2024–2027 |
| [PiggyAndrew/revit_mcp](https://github.com/piggyandrew/revit_mcp) | TypeScript | WebSocket | — | — |
| [oakplank/RevitMCP](https://github.com/oakplank/RevitMCP) | pyRevit extension + Python server | — | — | — |

---

## 2. What this confirms about Heron's decisions

| Heron decision | Status |
|---|---|
| **[D-01](DECISIONS.md) / [D-06](DECISIONS.md)** — MCP server in one language, C# add-in for Revit | ✅ **Confirmed.** `mcp-servers-for-revit` is exactly this shape: server + C# add-in + command set |
| **[D-03](DECISIONS.md)** — thick, specific tools | ✅ **Confirmed.** 30, 48 and 138 granular tools across the three biggest projects. Nobody ships a thin generic API |
| **[D-05](DECISIONS.md)** — Revit 2020 → latest, multi-targeted | ✅ **Confirmed in production.** `mcp-servers-for-revit` targets .NET Framework 4.8 for Revit 2020–2024 and .NET 8 for 2025–2026, from one project. The multi-targeting approach in [16](16-version-support-strategy.md) is what shipping projects actually do |
| **[16 §1](16-version-support-strategy.md)** version matrix | ✅ **Independently corroborated**, and **extended** — see §3 |

**[NOTE]** The version matrix corroboration matters. [16](16-version-support-strategy.md) recorded the
runtime table as *"a starting hypothesis to verify, not fact"*. Two independent shipping projects use
the same split. It is no longer a hypothesis.

---

## 3. New fact: **Revit 2027 uses .NET 10**

> *"Revit 2027 requires pyRevit with .NET 10 support"* — [Demolinator/revit-mcp-server](https://github.com/Demolinator/revit-mcp-server)

[16 §1](16-version-support-strategy.md) listed 2027 as *"unknown — do not assume"*. It is now a data
point, though still worth confirming against the SDK before it is relied on.

**This means [D-05](DECISIONS.md) spans three runtimes, not two:**

| Revit | Runtime |
|---|---|
| 2020 | .NET Framework 4.7.2 |
| 2021–2024 | .NET Framework 4.8 |
| 2025–2026 | .NET 8 |
| **2027** | **.NET 10** |

That is a third target framework in the multi-target build (`net48` + `net8.0-windows` + `net10.0-windows`),
and a third row in every regression matrix. It does not change the strategy — the adapter layer absorbs
it — but it makes the case for that layer stronger, and it confirms this will keep happening roughly
every two years.

---

## 4. **The two gaps every existing project shares**

### Gap 1 — none of them handles more than one Revit

| Project | Multi-instance |
|---|---|
| `mcp-servers-for-revit` | Not addressed. Single WebSocket connection model |
| `Demolinator/revit-mcp-server` | **No support.** Architecture assumes one Revit at `localhost:48884`. No mechanism to select among open instances |

**[NOTE — this is the sharpest finding in the research.]**

A fixed port, or a single connection, is **precisely the failure the owner's own field notes describe**:

> *"Before 2026-08-20 every Revit tried to use one shared line and the second one simply refused to
> start."* — [field notes](00e-field-notes-proven-bridge.md)

The published ecosystem is sitting on a bug he already found and fixed. Heron's per-PID pipes, discovery
directory, session binding, live session list and lease ([25](25-multi-session-and-binding.md)) are not
catching up to the state of the art — **they are ahead of it**, and they came from running the software,
not from reading about it.

For a BIM professional this is not an edge case. Comparing two models, borrowing a family, or working
while the AI works all require a second Revit ([25 §6a–6b](25-multi-session-and-binding.md)).

### Gap 2 — none of them has write safety

> *"No documented write-safety guardrails; users should 'Save / Save-As the model to disk' periodically
> for persistence."* — [Demolinator/revit-mcp-server](https://github.com/Demolinator/revit-mcp-server)
>
> No rollback mechanism documented in any project surveyed.

None of the projects documents: a single-undo guarantee · preview before modify · document pinning ·
re-read before acting · permission gating by risk level · an audit trail.

And both major families ship an **arbitrary code execution tool** — `send_code_to_revit`,
*"arbitrary IronPython execution"* — apparently ungated.

**[NOTE]** This is exactly what [D-03](DECISIONS.md) said to put behind `ADMIN` in Developer Persona
only. Shipping it open means any prompt injection through a family name or an imported document is a
route to arbitrary code against a live client model ([Golden Rule 19](14-golden-rules.md)).

The advice *"save periodically"* is the tell. It is a reasonable thing to say when there is no undo
guarantee — and it puts the safety burden on the user, which is the opposite of
[Golden Rule 1](14-golden-rules.md).

**Golden Rules 16–21 are not industry standard practice. That is the point.** They are what
would make Heron safe to approve for live project work when the alternatives are not.

---

## 5. **pyRevit Routes — a real option, and a real trap**

`Demolinator/revit-mcp-server` uses a feature worth knowing about:

> `AI Client ──stdio/SSE/HTTP──> MCP Server (Python/FastMCP) ──HTTP :48884──> pyRevit Routes ──> Revit API`

**pyRevit ships an HTTP routes server.** Enable it in pyRevit Settings, register route handlers from a
pyRevit extension, and IronPython executes inside Revit's process. A ready-made bridge, no add-in to
write.

### Why this is attractive

- The bridge already exists and is maintained by someone else.
- It answers **[Q-7a](OPEN-QUESTIONS.md)** — pyRevit is a proven scripting runtime for the
  DRAFT/TESTING half of [D-04](DECISIONS.md)'s hybrid model, and the owner already has `PyRevit-Tools`.
- Python end to end on the Heron side, matching [D-06](DECISIONS.md).

### Why it must not replace [D-02](DECISIONS.md)

**A fixed port is a single-instance design.** `localhost:48884` cannot serve two Revits, which
reintroduces exactly the failure of §4 — the one the owner already fixed once.

Unless pyRevit Routes can bind a **per-process port** and advertise it for discovery, adopting it as the
transport would be a regression against Heron's most field-proven advantage.

**Recommendation:**

| Layer | Choice |
|---|---|
| **Transport** | **Keep [D-02](DECISIONS.md)** — per-PID named pipes + discovery directory. Multi-Revit is non-negotiable |
| **Scripting execution** | **Evaluate pyRevit seriously** for the sandbox half of [D-04](DECISIONS.md). It is proven, the owner knows it, and it saves building an execution layer |

**To verify:** can pyRevit Routes bind a configurable port per Revit process, and can that port be
discovered? If yes, it becomes a viable transport too. If no, it remains a scripting option only.
Tracked as [Q-37](OPEN-QUESTIONS.md).

---

## 6. Smaller practical findings

| Finding | Consequence |
|---|---|
| Tools take **millimetres**, converting to Revit's internal **feet** | Heron must fix a unit convention at the tool boundary and state it. Silent unit mismatch is a classic source of wrong-by-1000 errors |
| `Demolinator` **requires an active document** | Matches [25 §4](25-multi-session-and-binding.md) — but Heron pins the document rather than depending on which is active |
| Tool counts of 30 / 48 / 138 | Confirms the context-cost concern in [04 §3](04-heron-mcp.md). At 138 tools, schemas on every request are a real cost — capability discovery is needed, not optional |
| Several are **forks of one original** | The ecosystem is narrow. Genuine architectural diversity is limited to WebSocket-vs-pyRevit-HTTP |

---

## 7. Honest summary

**Heron is not entering an empty field.** Connecting an AI to Revit over MCP is done, repeatedly, and
the tool layer is a solved shape — granular tools, C# add-in or pyRevit, 30–138 operations. Heron should
expect no credit for that part and should reuse what it can.

**What is genuinely unoccupied:**

1. **More than one Revit.** Nobody handles it. Heron already has the design, from field experience.
2. **Write safety.** No undo guarantee, no preview, no document pinning, no permission gating, no audit
   trail anywhere in the surveyed projects — and ungated arbitrary code execution in the biggest ones.
3. **Everything above the tool layer.** Fragments, skills, capability registry, knowledge, trust
   lifecycle, learning from delivered models ([10 §5a](10-memory-and-knowledge.md)). Every project
   surveyed is a *tool bridge*. None is a knowledge platform.

Points 2 and 3 are where the four specification documents actually point, and point 1 is a free
advantage the owner already earned. The tool layer is table stakes — worth building competently and
quickly, and not worth being precious about.

---

## Sources

- [mcp-servers-for-revit](https://github.com/mcp-servers-for-revit/mcp-servers-for-revit)
- [LuDattilo/revit-mcp-server](https://github.com/LuDattilo/revit-mcp-server)
- [Demolinator/revit-mcp-server](https://github.com/Demolinator/revit-mcp-server)
- [PiggyAndrew/revit_mcp](https://github.com/piggyandrew/revit_mcp)
- [oakplank/RevitMCP](https://github.com/oakplank/RevitMCP)
- [mcp-server-for-revit-python](https://github.com/mcp-servers-for-revit/mcp-server-for-revit-python)
