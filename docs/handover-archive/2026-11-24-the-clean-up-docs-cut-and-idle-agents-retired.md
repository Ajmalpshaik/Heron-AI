# Session note — THE CLEAN-UP OF 2026-11-24: DOCS CUT, IDLE AGENTS RETIRED

> **Session note** from 2026-11-24, written straight into this folder as its README asks. Nothing here is
> specification: where it disagrees with [DECISIONS.md](../DECISIONS.md), the
> [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those win**. A
> note records what was true on its own day.

---

### 2026-11-24 — THE CLEAN-UP OF 2026-11-24: DOCS CUT, IDLE AGENTS RETIRED

The owner asked for an audit of whether Heron is over-engineered, and then for the clean-up it found. All of
it was cloud work: nothing here ran in Revit, and nothing a modeller uses changed.

**NEW - merged on main**

| Pull request | What it did |
|---|---|
| #1352 | HANDOVER.md cut to one page (the old one kept whole in this folder); 141 old sitting notes deleted; nine research notes merged into [31 research](../31-research.md); finished rows removed from the two register archives |
| #1368 | [D-198](../decisions/D-198.md): 78 agents that nothing called retired, code removed; `tools/agent-count.py` gained a RETIRED state |
| #1374 | [D-199](../decisions/D-199.md): 14 more retired - update, trust, release, tagging, versioning, the import pipeline, the workflow engine |
| #1386 | [D-200](../decisions/D-200.md): 11 more idle modules removed; 6 kept because a helper prompt, a test or a modeller skill uses them |
| #1389 | Helpers moved out of `mcp/server/heron_mcp_server.py` into three files (8,217 to 7,532 lines); the 58 served tools unchanged in name, schema and order |
| #1395 | `tools/new-agent.py` refuses a RETIRED or DEFERRED row - row 5b-2038, issue #1393 |
| #1396 | Two history notes moved out of DECISIONS.md and NEEDS-CHECKING.md (small cut: both pages are already indexes that tools and 650+ links read) |

Kept on the owner's word: the company-standards modules (idle, to be wired in after the DRAFT tools are proved)
and `brain/heron_agents.py` (now named in [AGENTS.md](../../AGENTS.md)). The 135 DEPRECATED fragment cards stay in
the live library: the MCP server reads their `superseded-by` to send an old capability name to its successor.

**MISTAKES worth not repeating**

- A first "who calls this" count matched module names in comments, so it called modules used that were not. Count
  only a real import, a load by file name, or a command that runs the file. The loose count could only keep code,
  so nothing was removed wrongly.
- Two pull requests were merged with main a few commits ahead. Bring main in and rerun the gates first.

**TO DO**

1. The owner's PC: sign the proof drafts waiting there, and prove the DRAFT tools (another session is on it).
2. Owner's call: move the 58 tools themselves out of the server file - that needs the served-order rule changed.
   Recommended: leave it.
3. Row 5b-2039, issue #1394: twelve suites read the server file by a typed path; switch them to
   `heron_tools.server_text()`.
4. The four `session/S*` branches are merged but could not be deleted from the cloud session; delete them on GitHub.
