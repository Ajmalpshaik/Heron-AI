# Session note — ONE UNDERSCORE TURNED OFF THE GOLDEN RULE 19 GUARD ON AN MCP SERVER

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — ONE UNDERSCORE TURNED OFF THE GOLDEN RULE 19 GUARD ON AN MCP SERVER

**[Row 5b-118](../FRAGMENT-ISSUES.md), FIXED.** `brain/heron_tooling.py` read end to end — 319 lines, 2
public functions, **one suite**, nothing imports it.

Registering an additional MCP server means Heron will call tools it did not write, chosen by
descriptions it did not write, so this agent refuses a server that does not enumerate its tools in
advance. **The check is `if kind == "mcp-server"`, a single literal** — and
`KINDS = ("cli", "utility", "mcp-server")`, declared at module level under a comment explaining what
each kind is, **is referenced nowhere.**

**Measured, with no tools enumerated and everything else in order:**

| `kind` | verdict |
|---|---|
| `mcp-server` | **`TOOLS_NOT_ENUMERATED`** ✓ |
| `MCP-Server` | **`TOOLS_NOT_ENUMERATED`** ✓ |
| `mcp_server` | **`ready: True`** |
| `mcpserver` | **`ready: True`** |
| `mcp server` | **`ready: True`** |
| `banana` | **`ready: True`** |
| *(absent)* | **`ready: True`** |

each under the line:

> some-mcp registers as a mcp_server and **brings no tools Heron calls**

That is the one sentence the guard exists to make impossible. And `mcp_server` is not an exotic typo —
**it is how this repository spells its own `heron_mcp_server.py`.**

**The module claims the opposite, twice.** Its docstring: *"That is this agent's reading of Golden
Rule 19 … and it is the reading that **fails closed**"*, and its own answer repeats it in `unjudged`:
*"The readings all fail closed."* **It failed open on every spelling but one.**

**And the suite holds that claim as a string.** `tests/test_tooling.py` asserts
`any("fail closed" in note ...)` over the answer's own prose — which checks the sentence is **said**,
not that it is **true**. [Rows 5b-100](../FRAGMENT-ISSUES.md) and 5b-101's shape, here holding a claim
that was false.

A `kind` outside `KINDS` is refused with a new **`KIND_NOT_DECLARED`** before any of the seven steps
run. **`KINDS` is now the thing that decides**, which is what it was written to be. Case still passes
— `kind` is lowered first, so `MCP-Server` is a spelling of the declared kind and still has to
enumerate its tools.

```bash
python tests/test_tooling.py      # section 7b, 10 red against the module as found
```

**The four checks that nothing declared was lost were green BEFORE the fix** — all three of `KINDS`
accepted, and `MCP-Server` still refused for the right reason.

**One line of dead text went with it**: the register step's fallback `kind or "tool of unstated kind"`
cannot be reached now that the kind is one of three, and a branch nothing can take is the shape rows
5b-96, 5b-97 and 5b-100 are all about.

**What is right here is worth knowing**, because it is unusual: detection is treated as **execution**
and needs its own permission — running a program to ask what it is **is** running it, a distinction
`HERON-INS-DEP-005` does not need because its detection is an import; permission must name **this**
tool, never be a blanket one; `verify` is a separate step from `install`, because a package manager
exiting 0 says a download finished; and **NEVER SILENT is enforced last**, over every line rather than
only the ones written before the check.
