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

---

# 8 — Re-check, 2026-09-22: the field moved, and two of the three gaps are now occupied

> Carried out at the owner's request, four weeks after §1–§7. **§4 and §7 above are now partly stale —
> read this section before relying on them.** Each row is what the project's own documentation states
> about itself, and adoption figures are recorded because they say what the claims are worth.

## 8.1 Autodesk now ships its own MCP server

| Fact | Source |
|---|---|
| **Revit 2027 ships a Public MCP Server (Tech Preview)**, a separate addon downloaded under a Revit 2027 entitlement | [Autodesk AEC blog, 2026-06-17](https://www.autodesk.com/blogs/aec/2026/06/17/revit-public-mcp-server/) |
| What it does: find elements, check parameters and counts, **bulk parameter edits**, view snapshots, list sessions, open views, select and zoom, export views | [Revit 2027 help](https://help.autodesk.com/cloudhelp/2027/ENU/Revit-WhatsNew/files/GUID-97697CBF-0E11-484E-96E5-4277E3E8D61F.htm) |
| What it does **not** do: create or modify elements. Autodesk states those arrive later through a **dedicated write server**, read and write kept separate on purpose | Autodesk AEC blog, as above |
| **Autodesk Assistant** (Tech Preview) — product help, model queries, task automation, inside Revit, Autodesk account required | [Revit 2027 help](https://help.autodesk.com/cloudhelp/2027/ENU/Revit-WhatsNew/files/GUID-68D8FE6D-C5B0-4503-AE27-02C715BAC25B.htm) |

**[NOTE]** Third-party write-ups describe the standalone addon as *seven read-only tools*; Autodesk's own
page describes bulk parameter editing through the Assistant. The two are not the same surface. What is
not in dispute: **element creation and modification are not shipped, and are announced as coming.**

**Consequence for Heron.** The tool layer is not merely table stakes ([§7](#7-honest-summary)) — it is on
a published path to becoming a free Autodesk component. Growing Heron's tool or fragment count toward
*coverage* is building the part the vendor is commoditising. The version span is the exception:
Autodesk's is **2027 only**.

## 8.2 Open source closed the write-safety gap

[§4 Gap 2](#gap-2--none-of-them-has-write-safety) said no surveyed project had write safety. That is no
longer true.

| Project | Revit | Tools | Write safety | Multi-Revit | Adoption |
|---|---|---|---|---|---|
| [Al-Qublawi/AB.RevitMcp](https://github.com/Al-Qublawi/AB.RevitMcp) | **2020–2026** | 78 (24 read / 48 write / 6 destructive) | `TransactionGroup` assimilated to **one undo step**; **dry-run executes for real then rolls back and reports the blast radius**; destructive tools need literal `confirm: true`; schema validated twice; **no eval escape hatch** | **named pipes**, ACL'd to the Windows user; simultaneous instances not addressed | MIT, 2 stars, 10 commits |
| [KenLP/RevitMCPServer](https://github.com/KenLP/RevitMCPServer) | 2025–2027 | 94 | `dryRun: true` on **every** write tool; batch folds N steps into **one atomic undo**, rollback on first failure | **auto-assigned port per Revit *version*** — side-by-side releases, not two of one release | MIT, 10 stars, live-Revit smoke suite plus golden fixtures |
| [LuDattilo/revit-mcp-server](https://github.com/LuDattilo/revit-mcp-server) | 2023–2027 | 124 | standard transactions, **not** grouped into one undo | — | — |

**What survives of Gap 1.** Nobody matches per-PID pipes plus a discovery directory, session binding and
a lease ([25](25-multi-session-and-binding.md)). KenLP covers **two versions** open at once, which is the
common case. Heron covers **two of the same version**, which is the comparison-and-borrowing case
([25 §6a–6b](25-multi-session-and-binding.md)). The advantage is real, and narrower than §4 claimed.

**What does not survive.** "No rollback mechanism documented in any project surveyed" is out of date. Two
projects document single-undo grouping and dry-run preview — the substance of
[Golden Rules 16–21](14-golden-rules.md) — one of them across **2020–2026**, under MIT.

## 8.3 The knowledge layer has a funded commercial occupant

[§7](#7-honest-summary) point 3 claimed everything above the tool layer was unoccupied. Against open
source, still true. Against the market, no.

[**SWAPP.AI**](https://swapp.ai/) states it is *"grounded in your firm's own standards, QA rules, and
production history — inside Revit and ArchiCAD"*, builds a persistent model of a firm's standards from
its existing projects, and turns each correction into memory for the next project. That is Heron's
[10 §5a](10-memory-and-knowledge.md) thesis, as a product, in two authoring tools.

Adjacent: EvolveLab Glyph (documentation automation), [ArchiLabs](https://archilabs.ai/posts/revit-ai)
(natural-language automation recipes), Kestrel Labs (in-Revit code compliance), and Autodesk's own free
Model Checker (rule-based checksets).

## 8.4 What is left that no one else has

1. **Proof discipline.** [D-30](DECISIONS.md): every fragment recorded against a **named model**, with a
   **negative case** and a **staleness fingerprint** — 328 of 396 at this date. The surveyed projects
   ship *runtime* safety (dry-run, transaction grouping). None ships *evidence that the operation was
   ever correct*, kept and re-derivable. A dry-run tells a user what is about to happen; a proof tells
   them it has been right before. Those are different products.
2. **Revit 2020–2027 with a write path.** Autodesk: 2027. KenLP: 2025+. AB.RevitMcp: 2020–2026, no 2027.
   Heron spans all eight releases. This window closes as firms upgrade — a head start, not a moat.
3. **Two Revits of the same version** ([25](25-multi-session-and-binding.md)).

## 8.5 Revised recommendation

| Decision | Revised position |
|---|---|
| Grow fragment count toward coverage | **Stop counting it as progress.** It is the commoditised layer, and every fragment still marked `DRAFT` has never met a model - [README](../README.md) gives the command that counts them |
| Write safety | **Read [AB.RevitMcp](https://github.com/Al-Qublawi/AB.RevitMcp)'s `TransactionGroup` and dry-run design against Heron's own write path** before assuming Heron's is better. Study it; do not take its code ([31](31-studying-the-existing-libraries.md)) |
| Autodesk's write server | **Plan to sit on top of it for 2027**, not against it. Heron's own bridge keeps 2020–2026, where Autodesk will not be |
| Where remaining effort goes | **Proof, standards and the signed audit trail** — and a wedge SWAPP does not hold: **MEP, and Qatar/QCS/Ashghal practice** |

## 8.6 Sources for this section

- [Autodesk — Introducing the Revit Public MCP Server, 2026-06-17](https://www.autodesk.com/blogs/aec/2026/06/17/revit-public-mcp-server/)
- [Autodesk Revit 2027 help — Revit Public MCP Server (Tech Preview)](https://help.autodesk.com/cloudhelp/2027/ENU/Revit-WhatsNew/files/GUID-97697CBF-0E11-484E-96E5-4277E3E8D61F.htm)
- [Autodesk Revit 2027 help — Autodesk Assistant (Tech Preview)](https://help.autodesk.com/cloudhelp/2027/ENU/Revit-WhatsNew/files/GUID-68D8FE6D-C5B0-4503-AE27-02C715BAC25B.htm)
- [Al-Qublawi/AB.RevitMcp](https://github.com/Al-Qublawi/AB.RevitMcp) · [KenLP/RevitMCPServer](https://github.com/KenLP/RevitMCPServer) · [LuDattilo/revit-mcp-server](https://github.com/LuDattilo/revit-mcp-server)
- [SWAPP.AI](https://swapp.ai/) · [ArchiLabs — Revit 2027 AI review](https://archilabs.ai/posts/revit-2027-ai-review-whats-real-what-still-isnt) · [BIMsmith — what the built-in MCP server does in practice](https://blog.bimsmith.com/Revit-2027-What-the-Built-In-MCP-Server-Actually-Does-in-Practice)
