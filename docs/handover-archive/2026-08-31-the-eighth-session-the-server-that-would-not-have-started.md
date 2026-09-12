# The eighth session, 2026-08-31 — the server that would not have started

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**What it did:** installed the MCP SDK for the first time in this project's life, which took `A8` from
*needs Windows* to *mostly done* and, in the same ten minutes, found that **Heron's MCP server would not
start at all on a machine installing today.**

**The defect, and why nothing here could see it.** `pip install --user mcp` — the line
[`tools/HeronRevit.ps1`](../../tools/HeronRevit.ps1) hands the user, unpinned — resolved to 1.x when the server
was written and resolves to **2.x** now. 2.x **deleted `mcp.server.fastmcp`**: `FastMCP` was renamed
`MCPServer`. The import was written against 1.x, so the server raised `ImportError` before registering a
single tool — **every Heron tool absent from the host**, with no Revit and no Windows anywhere in the
failure. Every test in this repository reads that file **as text**, and text cannot fail an import. The
one technique nobody had used was the obvious one: install the dependency and start the thing.

| | |
|---|---|
| **The fix** | The import, and nothing else. The class is looked up **newest first**, because 2.x's `MCPServer` takes the same `@server.tool()` decorator and the same `run()` — measured, not assumed, with both SDKs installed side by side. This is [D-05](../DECISIONS.md)'s rule about Revit releases applied to a Python dependency: an unlisted version must fail **loudly**, so the last `except` re-raises naming the install line rather than leaving an `ImportError` about a module the user never typed |
| **The check that would have caught it** | [`tests/test_mcp_serves.py`](../../tests/test_mcp_serves.py) — the SDK's own registry against the source text, every description, every argument schema, and the three brain tools called through the SDK's own dispatch. **Validated by putting the defect back** and watching it fail under 2.x, which is the standard this repository already holds `check-api-surface.py` to |
| **`heron_version` now reports the SDK** | *"Heron stopped working"* and *"the SDK moved underneath it"* are indistinguishable from the user's side, and this tool's stated job is what to say when something is wrong |

**A skipped suite is now WAITING, not `ok`.** The SDK is an optional dependency, so on a machine without
it that test cannot run — and `check-gaps.py` sees **only exit codes**, since it sends stdout to
`DEVNULL`. A suite that skipped would have been reported `ok`. So a skip exits **3**, and `check-gaps`
reads it as WAITING: the same distinction the whole tool is built on, extended to the one place it could
not reach. This is the *"a killed run prints a header with nothing under it, and a blank reads as a
pass"* failure, caught before it happened rather than after.

**Two register rows were wrong in a way that would have failed a correct Heron**, and both were found by
running what they asked for rather than by reading them:

- **`A8`'s PASS text was stale.** It required *ten jobs, **four** with every part provided, and the seven
  missing capabilities named*. The truth is **10, 10 and none** — the row was written on 2026-08-29
  *before* the seven capabilities were written later that same day, and never revisited. As worded, it
  would have failed a Heron that was working correctly.
- **`A8` also required `heron_lookup` to answer `FILTER_ELEMENTS_BY_CATEGORY`.** It answers
  `SET_SELECTION`. That is **the assertion Steps 10 and 11 already retired**, copied into the register
  and left behind when they retired it: *"select all the ducts"* is filter-**then**-select, a composition,
  and a composition is what a **skill** names. It has been retired here too, **with its reasoning
  written down**, because deleting it quietly would have looked identical and taught nobody anything.

**`A7` is still blocked, and the row now names the right wall.** `pip install model2vec` **succeeds** —
PyPI is reachable. `StaticModel.from_pretrained("minishlab/potion-base-8M")` fails with
`ProxyError: 403 Forbidden`. The precondition was written as *"a machine that can reach a model host"*,
which anyone with a working network would read as already satisfied; it now says **huggingface.co**. And
the attempt proved one thing for free: with the package installed, the fallback ran against a **failed
download** instead of a missing import — a branch that had never once executed — and degraded to
`lexical`, saying so on its first line.

> **The lesson, and it is the same one four times now.** *"It needs Windows"* was a guess that got
> written down as a fact and inherited. It needed `pip install mcp`. The count of things believed to be
> waiting on a machine that were waiting on somebody trying them is now **four** — the .NET SDK, the
> WindowsDesktop targets, the bridge round trip, and this. **Assume the fifth exists.**

---
