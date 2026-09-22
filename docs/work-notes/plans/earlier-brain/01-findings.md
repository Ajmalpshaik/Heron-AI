# 01 — What was checked, and what it found

> **Type:** Operational work note. **Not specification.** Part of [the earlier-brain plan](README.md).
> **Status:** Complete as a record, 2026-09-22/23. **Owner:** Ajmal PS.

---

## 1. How it was read

Seven readers, one area each, on 2026-09-22 — each told to prove any *"Heron already has this"* by reading
the Heron file that does the job, never by a similar name. Then **every claim this folder relies on was
re-checked by hand** before it was written down; a claim that was not re-checked says **reported**.

| Area of the earlier library | What it held | Verdict for Heron |
|---|---|---|
| Hooks and automation | 9 hook scripts: search on every message, session status, per-edit consistency check, run log, a "this script already exists" warning, end-of-turn reminders | Mostly **Heron better**. Two mechanisms worth rebuilding: hooks wired so they run in **every** session, and a check placed at the last moment it can still change the outcome |
| Agents, instructions, packaging | 4 agents, an always-on rules file, a plugin manifest | A read-only model reader (**already built in Heron by #298**, see §4), a fragment writer, and rules that reach the AI in every chat |
| Skills and recipes | 12 skills, 44 recipes | **Heron has the pieces and not the method** — its skills list capabilities with no order |
| Knowledge notes | 61 notes, mostly Revit behaviour observed on real models | About ten **traps** Heron's fragments can hit and do not record, and the owner's words |
| MCP server and native tools | 27 tools | Mostly **Heron better**; one real gap — read-only jobs need write permission in Heron |
| Search, API index, run logs | a hybrid search, a 28-question score, two logs | Heron's search is better built but **has never been scored on the owner's own questions** |
| Tools and checkers | a 13-check consistency checker and others | **Three checks** would catch drift that is in Heron today |
| Notes on outside Revit libraries | 12 study notes | Three items: link-aware ceiling checks, "changed in central?", placeholder sheets |
| Voice narration | a speech system | **Not taken** — the owner's own experiment, declined 2026-09-22 |

## 2. What Heron already does better — and so was not taken

| The earlier library's way | Heron's way, and the evidence |
|---|---|
| Runs any C# pasted into a chat, and warns afterwards when a saved script already did the job | **No tool takes code.** Every change runs a named fragment with a contract, a declared risk and one undo — `python mcp/server/heron_tools.py` lists the tools; [D-03](../../../DECISIONS.md), [Golden Rule 18](../../../14-golden-rules.md) |
| A hook appends each Revit call to a log, and only in one AI app | The add-in writes the log itself, **refused calls included**, whichever app is talking to it — `HeronAudit.Record` in [`RevitDispatcher.cs`](../../../../revit/Heron.Revit.Addin/RevitDispatcher.cs) (verified) |
| Rewrites the question through a word-swap table before searching | **Forbidden by [D-34](../../../DECISIONS.md)**: understanding language is the host's job. Heron routes by capability and checks the Revit version and each fragment's risk first |
| Injects a search result and a "most used" list into every message | Costs context on every message; Heron answers when asked ([D-01](../../../DECISIONS.md), [19](../../../19-context-and-cost.md)) |
| End-of-turn reminders | **They never reached the AI.** The script that ran them together passes a child's warning on only when the child *fails*, and every reminder exited 0 — so each one was dropped (read in the earlier library's combining script; not run) |
| A knowledge graph and a notes vault | Its own record says the graph tool was called **zero** times and the vault never opened; Heron measured a graph and found it does not help its search (`tools/measure-graph.py`) |
| A separate API index built from one running Revit | Heron checks every called member against all eight releases — `tools/check-api-surface.py` |
| Packaged as a plugin so the server is found from any folder | **Already answered for Heron**: the installer asks where the project folder is, and Claude Code is opened there — [Q-PE-14](../plugin-extension/03-open-questions.md), answered 2026-09-22 |
| Old second-hand notes of NFPA numbers | The right source is the company's **licensed, project-adopted edition**, loaded into company scope ([10](../../../10-memory-and-knowledge.md), [20](../../../20-knowledge-trust-and-conflict.md)) |

## 3. Dangers the comparison exposed in Heron itself

| | What | State |
|---|---|---|
| **H1** | `revit_change` never called `risk_refusal`, so with Changes ON a chat could run the 15 PUBLISH/ADMIN fragments the command line refuses — sync with central, create workset, save, upgrade family files. The owner's audit log shows none ever ran that way (all 49 such runs were 2026-09-08 to 09-12, before the tool existed) | **Fixed on a branch**, verified with a test seen to fail first — PR [#299](https://github.com/Ajmalpshaik/Heron-AI/pull/299), draft, waiting for the owner. Register row 5b-155 is reserved for it |
| **H2** | The `heron-guard` hook runs only in a session where its skill has been loaded — skill-frontmatter hooks start when the skill is invoked (Claude Code docs, `code.claude.com/docs/en/skills.md`). **Verified 2026-09-22**: the guard script refused a forbidden edit fed to it by hand, and the same edit made through the editor in a normal session was let through | Package **C1** |
| **H3** | `revit_change` keeps a change with **no preview** (`"apply": "true"` always, [`heron_mcp_server.py`](../../../../mcp/server/heron_mcp_server.py)). [Constitution Article 9](../../../../HERON_CONSTITUTION.md) requires an accepted preview for any change not made by a PRODUCTION fragment, and no fragment is PRODUCTION. The server's header says the tool was written this way at the owner's instruction; **no decision records the exception** | **Owner's decision D1** — package **C2** |
| **H4** | Fragment writes have **no Revit failure handling**: `RevitFragment.cs` carries no failures preprocessor, while `RevitWrite.cs` (the move path) does (verified by search) | Package **C3** |
| **H5** | `connect-air-terminals` says in its code *"NOTHING IS MOVED"*; the same Revit call was observed elsewhere lifting a terminal 625 mm to meet the duct | Package **C4** |
| **H6** | Resizing a connected run was observed elsewhere to leave fittings at the old size and add transitions; Heron's sizing fragments say nothing about it (verified: no mention in their cards) | Package **C4** |
| **H7** | Two decision numbers were reused — D-45 and D-46 now hold different decisions from the ones older documents cite | Handed to the session splitting the registers on 2026-09-22; it verified it and is putting the recovery to the owner. **Not this plan's** |
| **H8** | Drift the earlier checker's checks would catch, **verified 2026-09-22**: 9 tools not named in [`tools/README.md`](../../../../tools/README.md); two control bytes (backspace, form feed) on one line of `docs/FRAGMENT-ISSUES.md`; [33](../../../33-external-repository-research.md) and [34](../../../34-patterns-adapted.md) each say Heron has 14 MCP tools while the server's own list is longer | Package **C1** |

**Noticed in passing and not touched:** the table in [`docs/work-notes/README.md`](../../README.md) does
not list the ten files in `plans/plugin-extension/`, though its own completeness check says every note
should be listed.

## 4. Already done elsewhere — do not rebuild

**A read-only model reader exists.** `heron-model-auditor` landed in #298 on 2026-09-22: a background agent
with a list of read-only Heron tools and nothing on it that can change the model or the chat, held honest
by `tests/test_agent_tools.py`. Package **C2** only adds the new read-only door to its list.

## 5. What the owner actually used — counts only

From the earlier library's own run log, on the owner's PC only (never in git). **723 runs** between
2026-08-13 and 2026-08-27; the job is named in **120** of them — the rest were one-off code, which Heron
does not allow. The jobs, described in Heron's terms rather than the source's names:

| The job | Runs | Heron capability | Status in Heron (verified 2026-09-23) |
|---|---|---|---|
| Select elements by category | 39 | `FILTER_ELEMENTS_BY_CATEGORY` | PROVEN |
| Select elements by id | 20 | `FILTER_ELEMENTS_BY_ID` | PROVEN |
| Report clashes | 20 | `FIND_CLASHES` | **DRAFT** |
| Supply ducting for a room, FCU to diffusers | 13 | **none as a whole** | pieces only |
| Route an MEP run | 11 | **none as a whole** | pieces only |
| Highlight one set, grey the rest | 10 | `HIGHLIGHT_VS_REST` | PROVEN |
| Report connectors | 9 | `REPORT_CONNECTORS` | PROVEN |
| Dimension rooms | 8 | `DIMENSION_ROOMS` | PROVEN |
| Select by category and family | 6 | `SELECT_BY_FAMILY` | PROVEN |
| Count by group | 5 | `GROUP_AND_COUNT` | PROVEN |
| Draw a main duct with an end cap | 4 | `CREATE_DUCT` (no cap) | PROVEN, part only |
| Connect a terminal branch | 4 | `CONNECT_AIR_TERMINALS` | **DRAFT** |
| Centre room tags | 3 | `CENTER_ROOM_TAGS` | **DRAFT** |
| Report location | 3 | `REPORT_LOCATION` | PROVEN |
| Select by several categories | 3 | `SELECT_BY_CATEGORIES` | PROVEN |

**What it points at:** prove `FIND_CLASHES`, `CONNECT_AIR_TERMINALS` and `CENTER_ROOM_TAGS` early
([05](05-pc-proving.md)); and **duct routing is 32 of the 120 named runs with no Heron skill** — package
**C9**. Usage is not proof ([D-30](../../../DECISIONS.md)); the counts are bursty (every named run falls in
five days). Derive a fragment's status now with
`grep -h '^heron-status:' brain/fragments/<name>/fragment.yaml`.
