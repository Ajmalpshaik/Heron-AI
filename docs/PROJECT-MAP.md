# Project map

**The fastest orientation in this repository.** Which folder owns what, where to start a change, and
which source wins when two disagree.

For *which document covers what*, use [README.md](README.md) — the documentation index. This file maps
**code and folders**, not documents.

---

## A. How a request travels

```text
The modeller                "select all ducts"
   │
Claude Code                 the host: conversation, intent, orchestration   (D-01)
   │  MCP (stdio)
mcp/server/                 tool surface — transport only, no BIM knowledge
   │
brain/                      which capability is wanted, which fragment provides it
   │  back through mcp/client/
   │  named pipe (local only, never a socket)
revit/Heron.Bridge/         the pipe server, with no Revit reference at all
   │
revit/Heron.Revit.Addin/    inside Revit.exe
   │  ExternalEvent
Revit's main thread         the only thread the Revit API may be touched from
   │
The model                   selection changes on screen · one audit line written
```

Two things this diagram is easy to misread:

- **The host decides what the user meant, not Heron** ([D-01](DECISIONS.md)). Heron resolves a
  *capability*; it never classifies intent.
- **Resolving is not running.** The brain answering *"`select-ducts` provides that"* has changed
  nothing. Running a fragment that **writes** is a separate operation, and `write.enabled` defaults to
  `false`.

---

## B. Folder map

Dependency rules below are not prose — they are enforced by
[`tools/check-structure.py`](../tools/check-structure.py), which fails the build if they are broken.

| | May depend on | Never |
|---|---|---|
| `platform/` | **nothing** | anything |
| `revit/` | `platform` | `brain`, `mcp` |
| `brain/` | `platform` | `revit`, `mcp` |
| `mcp/` | `platform`, `brain` | `revit` |
| `tests/` | all four | — |

`Autodesk.Revit` types may appear **only inside `revit/`**. Anywhere else is a crossed boundary, and
the [`heron-guard`](../.claude/skills/heron-guard/SKILL.md) hook refuses the edit as it is proposed.

### The four product parts

| Folder | Owns | Must not own | Start at | Gate after changing it |
|---|---|---|---|---|
| [`platform/`](../platform/README.md) | Kernel plumbing — paths, config, identity, audit, lease, permissions, emergency stop, units | **Any BIM knowledge.** If a duct is named in here, the boundary is crossed | `Heron.Core/HeronPaths.cs` — the only thing that builds a Heron path | `check-structure.py` · `check-compile.py` |
| [`revit/`](../revit/README.md) | Everything loaded into `Revit.exe` — ribbon, commands, the pipe server, the fragment executor | Network of any kind. Business decisions that could live outside Revit | `Heron.Revit.Addin/HeronApplication.cs` (add-in entry) · `Heron.Bridge/BridgeServer.cs` (the pipe) | `check-compile.py` · `check-api-surface.py` · **a real Revit** |
| [`mcp/`](../mcp/README.md) | Transport between the host and Revit, and the single seam to the brain | BIM logic, knowledge, or any judgement about what the user meant | `server/heron_mcp_server.py` (the MCP entry) · `client/heron_bridge_client.py` (the pipe client) · `server/heron_brain.py` (the **only** door to `brain/`) | `test_mcp_serves.py` · `test_served_claims.py` — both need the MCP SDK |
| [`brain/`](../brain/README.md) | Knowledge — fragments, skills, capabilities, retrieval, context, the graph | Anything that must import a Revit type. It has to stay runnable on a machine with no Revit | `heron_fragment.py` (what a fragment is) · `heron_retrieve.py` (the lookup) · `heron_capability.py` (ask for what you want done) | `heron_fragment.py` · `check-routing.py` · `check-fragments-compile.py` |

### The supporting folders

| Folder | Owns | Start at |
|---|---|---|
| [`tools/`](../tools/README.md) | Every gate, report and generator — the scripts that keep the prose honest | [`tools/README.md`](../tools/README.md), one section per tool |
| [`tests/`](../tests/README.md) | The suites, the golden library, the .NET test host | [`tests/README.md`](../tests/README.md) — **read the exit codes section first** |
| `docs/` | Permanent specification, architecture, decisions and registers | [README.md](README.md) |
| [`docs/work-notes/`](work-notes/README.md) | Temporary operational work — what is going on right now | [work-notes/README.md](work-notes/README.md) |
| `.claude/` | House rules as skills, and four prototype agents. **The only skills tree** | [`.claude/skills/README.md`](../.claude/skills/README.md) |
| `.codex/` | The same four agents for a second host, pointing at the same skills | `.codex/config.toml` |

---

## C. Change map — where to start

| I need to… | Start here | Then |
|---|---|---|
| Change what happens **inside Revit** | [`revit/README.md`](../revit/README.md) | `revit-addin-conventions` and `revit-ribbon-and-windows` skills. Remember: a loaded assembly cannot be unloaded — every change needs a Revit restart |
| Add or change a **fragment** | [`brain/README.md`](../brain/README.md) → `heron_fragment.py` | [09 — Skills & Fragments](09-skills-and-fragments.md). A re-authored fragment arrives **unproven** whatever it was elsewhere ([D-44](DECISIONS.md)) |
| **Prove** a fragment against a model | [`fragment-proving`](../.claude/skills/fragment-proving/SKILL.md) skill | [NEEDS-CHECKING.md](NEEDS-CHECKING.md). A proof needs a named model, a negative case and a fingerprint ([D-30](DECISIONS.md)) |
| Add or change an **MCP tool** | [`mcp/README.md`](../mcp/README.md) → `server/heron_tools.py` | [04 — Heron MCP](04-heron-mcp.md). Transport only — no BIM logic |
| Change **which Revit versions** are supported | [16 — Version Support](16-version-support-strategy.md) | `Directory.Build.props` maps release → runtime. `revit-version-support` skill has the API breaks |
| Understand **why** a design is the way it is | [DECISIONS.md](DECISIONS.md) | Then the numbered document for that area |
| Find out **what is not proven yet** | `python tools/check-gaps.py` | [NEEDS-CHECKING.md](NEEDS-CHECKING.md). Never read a count out of prose |
| **Continue unfinished work** | [HANDOVER.md](HANDOVER.md) | [work-notes/](work-notes/README.md) for anything active |
| Know **what to run before pushing** | [`heron-ship`](../.claude/skills/heron-ship/SKILL.md) skill | It names the three gates that must pass and the checks that need a machine this container has not got |

---

## D. Truth hierarchy — which source wins

Two different questions, and mixing them is how a defect gets promoted into policy.

### What Heron is *allowed* to do

1. [**HERON_CONSTITUTION.md**](../HERON_CONSTITUTION.md) — 30 Articles, binding.
2. [**14 — Golden Rules**](14-golden-rules.md) — 21 rules, all official.
3. [**DECISIONS.md**](DECISIONS.md) — accepted decisions.

**An implementation that conflicts with these is a defect.** Running code does not silently amend
policy, and a rule is never edited to match code that broke it.

### What Heron *actually does* right now

1. **The code**, and tool output derived from it — `heron_fragment.py`, `check-gaps.py`, `agent-count.py`.
2. **A recorded proof** against a named model, with a negative case and a fingerprint.
3. **Metadata** — a fragment's own `heron-status`.

These can correct an out-of-date sentence, and routinely do. But **a passing checker proves only what
it implements**: `check-structure.py` exiting 0 says the layering holds, not that the code is right.

### What explains the intent

Specifications and permanent architecture — `docs/00`–`00e`, `docs/01`–`34`. Historical specifications
keep their historical wording. Where one is superseded, point at the deciding decision rather than
rewriting the history.

### What is only context

Registers ([NEEDS-CHECKING](NEEDS-CHECKING.md), [FRAGMENT-ISSUES](FRAGMENT-ISSUES.md),
[OPEN-QUESTIONS](OPEN-QUESTIONS.md)), the [handover](HANDOVER.md), and everything in
[work-notes/](work-notes/README.md). Useful, dated, and never authority.

**A retrieval or vector index is a derived view, never canonical** — [Golden Rule 11](14-golden-rules.md).

### When two sources disagree

Record **both**, name the governing decision, name the observed evidence, and name who resolves it.
Do **not** pick whichever makes the cleanup easier, and do not close a policy conflict by yourself.

---

## E. Routes by role

### BIM modeller or coordinator

[01 — Vision & Principles](01-vision-and-principles.md) → the [README](../README.md) status section →
[NEEDS-CHECKING.md](NEEDS-CHECKING.md).

What matters to you: **read and write are different things.** Everything proven so far is read-only.
`write.enabled` defaults to `false`. A fragment marked `DRAFT` has never met a model — 193 of 360 at
the last count, and you should derive it rather than believe that number:

```bash
grep -h '^heron-status:' brain/fragments/*/fragment.yaml | sort | uniq -c
```

### BIM manager

[16 — Version Support](16-version-support-strategy.md) for what is supported ·
[12 — Security & Permissions](12-security-and-permissions.md) for approval and data scope ·
[HERON_CONSTITUTION.md](../HERON_CONSTITUTION.md) for the binding rules.

**Declared support and tested releases are not the same list.** The compile gate covers 2020–2027; a
real Revit has been used on 2020 and 2024.

### Developer

This file's §B and §C → [`CONTRIBUTING.md`](../CONTRIBUTING.md) → the README of the folder you are
changing → the [`heron-ship`](../.claude/skills/heron-ship/SKILL.md) skill before you push.

### AI agent

[**AGENTS.md**](../AGENTS.md) first. It is short and it is the only file written for you.

### Project owner

[HANDOVER.md](HANDOVER.md) answers *what is going on* ·
[work-notes/](work-notes/README.md) answers *what is being worked on* ·
`python tools/check-gaps.py` answers *what is genuinely unfinished versus only waiting for a machine*.

---

## F. Words used here

Full definitions: [15 — Glossary](15-glossary.md). The eight that cause the most confusion:

| Term | In one line |
|---|---|
| **Fragment** | One reusable unit of implementation — C# plus metadata. The thing that does the work |
| **Capability** | *What you want done*, named without naming who does it. Skills ask for these, never for a fragment |
| **Skill** | What a user can ask for in their own words. Names capabilities, never fragments |
| **Host** | The AI application providing the conversation — Claude Code ([D-01](DECISIONS.md)). Not part of Heron |
| **MCP** | The tool interface between the host and Heron |
| **Bridge** | The local named pipe carrying a request into Revit. Never a network socket |
| **ExternalEvent** | The only safe way to reach the Revit API — it schedules work onto Revit's main thread |
| **Proof** | A recorded run against a **named real model**, with a negative case and a staleness fingerprint. A compile is not a proof; a passing test is not a proof ([D-30](DECISIONS.md)) |
