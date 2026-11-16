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

> **Corrected 2026-11-15** ([§9.1](#91-what-the-code-showed)): `mcp-servers-for-revit` talks raw TCP on
> port 8080, not WebSocket, and listens on every network interface with no password. It registered 26
> tools at the commit read, not "30+", and the family's three original repositories are archived.
> `LuDattilo/revit-mcp-server` talks TCP, not WebSocket; its 138 tools above are right.

---

## 2. What this confirms about Heron's decisions

| Heron decision | Status |
|---|---|
| **[D-01](DECISIONS.md) / [D-06](DECISIONS.md)** — MCP server in one language, C# add-in for Revit | ✅ **Confirmed.** `mcp-servers-for-revit` is exactly this shape: server + C# add-in + command set |
| **[D-03](DECISIONS.md)** — thick, specific tools | ✅ **Confirmed.** 30, 48 and 138 granular tools across the three biggest projects. Nobody ships a thin generic API |
| **[D-05](DECISIONS.md)** — Revit 2020 → latest, multi-targeted | ✅ **Confirmed in production.** `mcp-servers-for-revit` targets .NET Framework 4.8 for Revit 2020–2024 and .NET 8 for 2025–2026, from one project. The multi-targeting approach in [16](16-version-support-strategy.md) is what shipping projects actually do |
| **[16 §1](16-version-support-strategy.md)** version matrix | ✅ **Independently corroborated**, and **extended** — see §3 |

> **Corrected 2026-11-15** ([§9.1](#91-what-the-code-showed)): the first count in the D-03 row is 26,
> not 30. The row's point stands.

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

> **As built (2026-10-18):** confirmed - [`Directory.Build.props`](../Directory.Build.props) maps 2027 to
> `net10.0-windows`, CI compiles 2027 on every push, and a real Revit 2027 loaded the add-in on
> 2026-09-19 ([A12](needs-checking/group-a.md), [16 §1](16-version-support-strategy.md)). The build has
> four target frameworks, not three: `net472` for 2020 as well as the three named above, and no adapter
> layer as §4 of docs/16 drew it - [16 §4](16-version-support-strategy.md) says what stands instead.

---

## 4. **The two gaps every existing project shares**

### Gap 1 — none of them handles more than one Revit

| Project | Multi-instance |
|---|---|
| `mcp-servers-for-revit` | Not addressed. Single WebSocket connection model |
| `Demolinator/revit-mcp-server` | **No support.** Architecture assumes one Revit at `localhost:48884`. No mechanism to select among open instances |

> **Corrected 2026-11-15** ([§9.1](#91-what-the-code-showed), [§9.2](#92-two-revits-of-the-same-version)):
> the limit in `mcp-servers-for-revit` is a fixed TCP port, not a WebSocket connection - a second
> Revit's listener fails, the error is swallowed, and the button still reads "Open Server". The heading
> of this gap is out of date: four projects now handle two Revits of the same version. None has a lease.

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

> **Corrected 2026-11-15** ([§9.5](#95-the-rest-of-the-table)): still true of the C# family. The Python
> sibling now shows the whole code in a Yes/No dialog in Revit before it runs.

**[NOTE]** This is exactly what [D-03](DECISIONS.md) said to put behind `ADMIN` in Developer Persona
only. Shipping it open means any prompt injection through a family name or an imported document is a
route to arbitrary code against a live client model ([Golden Rule 19](14-golden-rules.md)).

> **As built (2026-10-18):** there is no Developer Persona - [D-27](DECISIONS.md#d-27--one-voice-and-the-answers-shape-follows-the-questions-shape)
> settled on 2026-08-28 that Heron has one voice and no personas - and Heron has no tool that runs code
> it is handed. The nearest thing is `revit_change` running a library fragment declared `ADMIN`, which
> works only while the owner's Admin switch is on
> ([D-106](DECISIONS.md#d-106--admin-and-publish-each-get-a-switch-of-their-own-and-only-the-owner-turns-them-on));
> [04 §3](04-heron-mcp.md) has the rest.

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

> **Decided since (2026-08-28):** neither half of the recommendation above was taken.
> [D-28](DECISIONS.md#d-28--generated-code-is-c-compiled-at-run-time-in-process) answered
> [Q-7a](open-questions/tier-1.md): generated code is **C#, compiled at run time with Roslyn, inside
> Revit**, not pyRevit - one language, one compile gate, and no HTTP server beside an add-in that has no
> network code. That closed [Q-37](open-questions/from-master-specification-part-2.md) as not applicable:
> Heron does not use pyRevit Routes, as transport or for scripting. The transport is
> [D-02](DECISIONS.md)'s per-process named pipe.

> **Corrected 2026-11-15** ([§9.4](#94-pyrevit-7-has-its-own-mcp-server)): pyRevit no longer means only
> Routes on a fixed HTTP port. pyRevit 7 ships its own MCP server, separate from Routes: a named pipe,
> the Revit picked by year or process id, Keep or Discard, and one undo entry. D-28 still stands.

---

## 6. Smaller practical findings

| Finding | Consequence |
|---|---|
| Tools take **millimetres**, converting to Revit's internal **feet** | Heron must fix a unit convention at the tool boundary and state it. Silent unit mismatch is a classic source of wrong-by-1000 errors. *Settled since: every length a caller types is millimetres, and the fragment converts it ([D-71](DECISIONS.md#d-71--every-length-a-caller-types-is-millimetres-and-the-fragment-converts-it)); a point crosses as three millimetre numbers ([D-67](DECISIONS.md#d-67--a-point-crosses-as-three-millimetre-numbers))* |
| `Demolinator` **requires an active document** | Matches [25 §4](25-multi-session-and-binding.md) — but Heron pins the document rather than depending on which is active |
| Tool counts of 30 / 48 / 138 | Confirms the context-cost concern in [04 §3](04-heron-mcp.md). At 138 tools, schemas on every request are a real cost — capability discovery is needed, not optional |
| Several are **forks of one original** | The ecosystem is narrow. Genuine architectural diversity is limited to WebSocket-vs-pyRevit-HTTP |

> **Corrected 2026-11-15** ([§9.1](#91-what-the-code-showed), [§9.5](#95-the-rest-of-the-table)): the
> counts read 26 / 48 / 138 at the commits read. The variety is wider than WebSocket against pyRevit
> HTTP: the biggest family is raw TCP, and named pipes are now common.

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

> **Corrected 2026-11-15** ([§9.2](#92-two-revits-of-the-same-version)): point 1 is out of date - four
> projects now handle two Revits of the same version, and what is left is the lease. The range above
> reads 26–138 at the commits read. §8 had already narrowed points 2 and 3.

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

> **Corrected 2026-11-15** ([§9.3](#93-autodesk-now-writes-a-little)): search summaries say Autodesk has
> shipped Read, Write and Experimental installers on Revit 2027.2 since about 2026-09-29, so "not
> shipped" is out of date. Not read on Autodesk's own page - to confirm on Autodesk's page.

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

> **Corrected 2026-11-15** ([§9.1](#91-what-the-code-showed)): three rows were wrong when written.
> AB.RevitMcp has had a gated run-any-C# tool since 2026-09-02, its dry-run covers only 3 destructive
> tools, and with several Revits open it takes the most recently started one. LuDattilo had 138 tools,
> not 124, and already had confirm dialogs and dry-run before this re-check. KenLP has moved on since
> ([§9.5](#95-the-rest-of-the-table)).

**What survives of Gap 1.** Nobody matches per-PID pipes plus a discovery directory, session binding and
a lease ([25](25-multi-session-and-binding.md)). KenLP covers **two versions** open at once, which is the
common case. Heron covers **two of the same version**, which is the comparison-and-borrowing case
([25 §6a–6b](25-multi-session-and-binding.md)). The advantage is real, and narrower than §4 claimed.

> **Corrected 2026-11-15** ([§9.2](#92-two-revits-of-the-same-version)): rvt-mcp, revit-model-mcp,
> Horizun and pyRevit 7 now each handle two Revits of the same version, with per-process discovery and
> binding. What nobody else has is the lease.

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

1. **Proof discipline.** [D-30](DECISIONS.md): every `PROVEN` fragment recorded against a **named model**,
   with a **negative case** and a **staleness fingerprint** — 328 of 396 on 2026-09-22; today's split is
   `grep -h '^heron-status:' brain/fragments/*/fragment.yaml | sort | uniq -c`. The surveyed projects
   ship *runtime* safety (dry-run, transaction grouping). None ships *evidence that the operation was
   ever correct*, kept and re-derivable. A dry-run tells a user what is about to happen; a proof tells
   them it has been right before. Those are different products.
2. **Revit 2020–2027 with a write path.** Autodesk: 2027. KenLP: 2025+. AB.RevitMcp: 2020–2026, no 2027.
   Heron spans all eight releases. This window closes as firms upgrade — a head start, not a moat.
   *As built (2026-10-18): Heron **compiles** for all eight; a real Revit has loaded it on 2020, 2024,
   2026 and 2027, 2021 to 2023 and 2025 are NOT RUN for want of a PC that has them, and on 2026 no
   fragment ran until a repair not yet run there - [16 §6](16-version-support-strategy.md#6-build-and-deployment-matrix)
   has the table and its evidence. "Spans" is a claim per release, not one word for all eight.*
3. **Two Revits of the same version** ([25](25-multi-session-and-binding.md)).
   *Corrected 2026-11-15: no longer Heron's alone - [§9.2](#92-two-revits-of-the-same-version). What is
   left is the lease and never sliding onto another Revit.*

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

---

# 9 — Re-check, 2026-11-15: others caught up on undo, not on proof

> Carried out 2026-11-15, eight weeks after §8, by **reading code**: of the 548 GitHub repositories a
> search found that day, 82 were cloned and read file by file and 466 more were swept by README and file
> list. Nothing was built, installed or run. **§1–§8 above stay as written; a "Corrected 2026-11-15"
> note beside each stale sentence points here.** Every count about another project is a dated
> observation at the commit named, not a live figure. Taken from the working note
> `docs/work-notes/investigations/2026-11-15-revit-mcp-landscape.md`, section "Corrections to docs/26",
> on [pull request 877](https://github.com/Ajmalpshaik/Heron-AI/pull/877). It is named, not linked,
> because the owner retires it.

## 9.1 What the code showed

| Project | What docs/26 said | What the code showed, 2026-11-15 | Source file (commit) |
|---|---|---|---|
| [mcp-servers-for-revit](https://github.com/mcp-servers-for-revit/mcp-servers-for-revit) | §1, §6: **WebSocket** | **Raw TCP** JSON-RPC to a hard-wired **port 8080**. The add-in listens on **every network interface with no password** - its own issue 54, open since 2026-09-08. The `ws` package is declared but never imported | `plugin/Core/SocketService.cs` lines 87 and 105; `server/src/utils/SocketClient.ts` (86cf705) |
| mcp-servers-for-revit | §4 Gap 1: "single WebSocket connection model" | The limit is the fixed port. A second Revit's listener fails to start, the error is swallowed, and the button still reads "Open Server" | `plugin/Core/SocketService.cs` line 114; `plugin/Core/MCPServiceConnection.cs` line 28 |
| mcp-servers-for-revit | §1: "30+" tools | **26** tool registrations (three tool files are empty); 23 Revit commands in `command.json` | `server/src/tools/*.ts`, `command.json` (86cf705) |
| mcp-servers-for-revit | §1: the family listed as live | The three originals - revit-mcp, revit-mcp-plugin, revit-mcp-commandset - are **archived** since 2026-02-25 | each repository's README; the organisation's repositories page |
| [LuDattilo/revit-mcp-server](https://github.com/LuDattilo/revit-mcp-server) | §1: **WebSocket** | **TCP** on loopback, the first free port from 8080 to 8089, written to `mcp-port.txt`; the server reads the newest Revit year's file | `plugin/Core/SocketService.cs`; `server/src/utils/SocketClient.ts`, `ConnectionManager.ts` (2c33b84) |
| LuDattilo/revit-mcp-server | §8.2: **124** tools | **138** registrations in 138 files - §1 was right. Its README says both 138 and 124 | `server/src/tools/` (2c33b84) |
| LuDattilo/revit-mcp-server | §8.2: standard transactions, not grouped, and nothing more | The grouping part holds (115 Transactions, 7 groups). But **confirm dialogs** in 13 handlers (2026-04-07) and **`dryRun`** in 30 files (2026-04-03) were there **before** §8's re-check. Its `send_code_to_revit` is ungated. No commits since 2026-04-09; the author moved to RevitCortex | commits 219b8f3, 0cf2e37, 71e5a9e; `commandset/Commands/ExecuteDynamicCode/ExecuteCodeEventHandler.cs` |
| [Al-Qublawi/AB.RevitMcp](https://github.com/Al-Qublawi/AB.RevitMcp) | §8.2: "no eval escape hatch" | `revit_execute_code` exists **since v1.1.0, 2026-09-02**: compile-any-C# behind three gates - a server flag, a ribbon toggle off by default, and `confirm: true` - on Revit 2020-2024 only. Its README contradicts itself | `src/AB.RevitMcp.Contracts/Tools/ToolCatalog.Code.cs`; `src/AB.RevitMcp.Addin/Tools/CodeTools.cs` (fe4f688) |
| AB.RevitMcp | §8.2: dry-run "executes for real then rolls back" - read as every write | Dry-run covers **only 3 destructive tools**: delete, purge, unload links. Ordinary writes cannot be dry-run | `src/AB.RevitMcp.Addin/Tools/DestructiveTools.cs` (18 hits; 0 in every other tools file) |
| AB.RevitMcp | §8.2: "simultaneous instances not addressed" | Each Revit is found by process id, but **with several open it takes the most recently started** | `src/AB.RevitMcp.Server/Bridge/BridgeConnection.cs` lines 131-169 |

## 9.2 Two Revits of the same version

§8.2 and §8.4 item 3 said Heron alone handled two Revits of the same version. **That is no longer true.**

| Project (licence) | How it handles two of the same version | Source |
|---|---|---|
| [bimwright/rvt-mcp](https://github.com/bimwright/rvt-mcp) (Apache-2.0) | One discovery file per process; binds only on an exact match, else `AMBIGUOUS_TARGET`; checks the document's identity before each write. Also a generation counter, so a call made before a switch cannot land after it | README "Multiple Revit instances", `ARCHITECTURE.md` (b44ee0b8) |
| [sharafutdinovdi/revit-model-mcp](https://github.com/sharafutdinovdi/revit-model-mcp) (MIT) | One pipe per process and heartbeat files; a write needs an exact document id or process id | `docs/actions.md` (be74eed9) |
| [HorizunGroup/horizun-revit-mcp](https://github.com/HorizunGroup/horizun-revit-mcp) (Apache-2.0) | Refuses two Revits of the same year until one is chosen | `docs/security-model.md` §4-5 |
| [pyRevit 7](https://github.com/pyrevitlabs/pyRevit) agent runtime (GPL-3.0) | Takes a year or a process id | `docs/agent-runtime.md` |

**What still separates Heron: the lease, and never sliding onto another Revit** (Constitution Article 12a,
[25](25-multi-session-and-binding.md)). **No project read has a lease.** rvt-mcp's generation counter is
an idea Heron has not got - re-authored, never copied ([31](31-studying-the-existing-libraries.md)).

## 9.3 Autodesk now writes, a little

> **From search summaries only.** help.autodesk.com was blocked in the session that did this research,
> so none of this was read on Autodesk's own page. **To confirm on Autodesk's page** before anything
> relies on it.

| Fact, as summarised on 2026-11-15 | To confirm on Autodesk's page |
|---|---|
| Three separate installers on **Revit 2027.2**: **Read, Write and Experimental**. Write includes Read; Experimental includes both | Yes |
| Write and Experimental available since about **2026-09-29** | Yes |
| Write adds **about 3** model-changing tools (parameters, move, rotate); Experimental about 28 more, **delete** among them | Yes |
| "Only on Revit 2027.2" (help) against "2027.2 or later" (developer blog) - the two conflict | Yes |

This updates §8.1's *"element creation and modification are not shipped"*. §8.5's direction - sit on top
of Autodesk on 2027, keep Heron's own bridge for 2020-2026 - is unchanged.

## 9.4 pyRevit 7 has its own MCP server

Separate from the Routes of §5, and close to Heron's own design ([pyRevit](https://github.com/pyrevitlabs/pyRevit),
`docs/agent-runtime.md`):

- **GPL-3.0**, experimental, **off by default** - the idea only, never the code ([31](31-studying-the-existing-libraries.md)).
- A **named pipe**, the Revit picked **by year or process id**, the **document pinned**.
- **Keep or Discard**, with the changed elements selected, and **one change = one undo entry**.
- It runs whatever Python the AI writes, and says there is no sandbox. Heron runs only library fragments
  ([D-28](DECISIONS.md#d-28--generated-code-is-c-compiled-at-run-time-in-process) still stands).

## 9.5 The rest of the table

The same table had these rows too. Each is a dated observation from that day's reading.

| Where | What changed | Source (commit) |
|---|---|---|
| §4 Gap 2 | The C# family's code tool is still ungated. The Python sibling, [mcp-server-for-revit-python](https://github.com/mcp-servers-for-revit/mcp-server-for-revit-python), now shows the whole code in a Yes/No dialog in Revit before it runs (since 2026-10-14), and its own `SECURITY.md` says it is not sandboxed | `revit_mcp/security.py`, `SECURITY.md` (6ca46b3) |
| §8.2 KenLP | [KenLP/RevitMCPServer](https://github.com/KenLP/RevitMCPServer): 100 tools on 2026-11-15. Since 2026-10-04 also an opt-in approval token bound to the exact inputs and the active document, a read-back after commit, and a hashed audit log. A second Revit of the same year still fails to bind | `CHANGELOG.md`; `src/RevitMCP.Core/Approval.cs`; `src/RevitAddin/App.cs` lines 102-104 (fdf6efa) |
| §5 | pyRevit Routes listens on all interfaces by default and does not pass HTTP headers to handlers - as stated by revit-mcp-hardened, which says it read pyRevit's source. **Not checked** in this research | revit-mcp-hardened `SECURITY.md` (43afdb7) |
| §6 | Named pipes are now common: AB.RevitMcp, Horizun, revit-model-mcp, RevitThyme, bim-agent, kir, pyRevit 7, and rvt-mcp from 2025. The biggest family is raw TCP | the working note's groups a, c to h |
| §1 oakplank | [oakplank/RevitMCP](https://github.com/oakplank/RevitMCP): 49 tools, MIT, no code tool; pyRevit Routes on 48885/48884/48886, and the first port that answers wins; a delete in "individual" mode leaves one undo entry per element | `lib/RevitMCP_ExternalServer/tools/registry.py`; `core/runtime_config.py` line 17; `lib/routes/element_operation_routes.py` (32c9ae8) |
| §8.4 item 1 | Still true in that exact form, but closer: Horizun's hashed per-year release evidence, RevitThyme's hash-pinned records on fixtures, danlion333's run on Autodesk's sample witnessed by Revit's journal, revit-model-mcp's per-year smoke runs. None per operation, none with a negative case | Horizun `docs/release-evidence.json`; RevitThyme `verification/`; danlion333 `docs/verification/README.md` |

## 9.6 What is left that no one else has, revised

1. **Proof discipline** ([D-30](DECISIONS.md)) - still Heron's alone: a record per operation on a named
   real model, with a negative case and a staleness fingerprint. Count the `PROVEN` fragments with
   `grep -h '^heron-status:' brain/fragments/*/fragment.yaml | sort | uniq -c`. Others are closer than
   on 2026-09-22 (§9.5, last row).
2. **Revit 2020–2027 with a write path** - §8.4 item 2, not re-checked here.
3. **The lease, and never sliding onto another Revit** (§9.2) - in place of "two Revits of the same
   version", which others now handle.

## 9.7 Sources for this section

- The working note named at the top of §9, on [pull request 877](https://github.com/Ajmalpshaik/Heron-AI/pull/877)
- [mcp-servers-for-revit](https://github.com/mcp-servers-for-revit/mcp-servers-for-revit) · [mcp-server-for-revit-python](https://github.com/mcp-servers-for-revit/mcp-server-for-revit-python) · [LuDattilo/revit-mcp-server](https://github.com/LuDattilo/revit-mcp-server) · [Al-Qublawi/AB.RevitMcp](https://github.com/Al-Qublawi/AB.RevitMcp) · [KenLP/RevitMCPServer](https://github.com/KenLP/RevitMCPServer) · [oakplank/RevitMCP](https://github.com/oakplank/RevitMCP)
- [bimwright/rvt-mcp](https://github.com/bimwright/rvt-mcp) · [sharafutdinovdi/revit-model-mcp](https://github.com/sharafutdinovdi/revit-model-mcp) · [HorizunGroup/horizun-revit-mcp](https://github.com/HorizunGroup/horizun-revit-mcp) · [pyrevitlabs/pyRevit](https://github.com/pyrevitlabs/pyRevit)
