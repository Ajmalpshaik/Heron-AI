# Open questions — Tier 1

> One section of [the register](../OPEN-QUESTIONS.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## Tier 1 — Blocking

> ✅ **All clear.** Every question that blocked Phase 0 has been answered — see
> [D-01](../DECISIONS.md) through [D-10](../DECISIONS.md).
>
> **Phase 0 is unblocked.** It starts on the owner's go-ahead ([D-00](../DECISIONS.md)).

The one sub-decision that was open inside [D-04](../DECISIONS.md) closed on 2026-08-28:

### 🟠 Q-7a — Which scripting runtime for the sandbox?

[D-04](../DECISIONS.md) settled *hybrid* — scripting while a fragment is in DRAFT/TESTING, compiled C# for
PRODUCTION. Which scripting runtime is still open: **pyRevit**, **IronPython**, **Python.NET**, or
**Roslyn scripting** (C# without compiling to an assembly).

The owner's existing `PyRevit-Tools` work is the strongest available evidence and should be reviewed
before choosing. Decidable during Phase 0 rather than before it, since Phase 0 generates no code.

**Research favours pyRevit.** A shipping Revit MCP server executes IronPython inside Revit via pyRevit's
built-in Routes server -- proven, maintained by someone else, and the owner already knows it
([26](../26-prior-art-revit-mcp.md)).

→ [09 §10](../09-skills-and-fragments.md)

**Answer: Roslyn C# scripting, in process, through the existing bridge — not pyRevit. See
[D-28](../DECISIONS.md).** The research note above favoured pyRevit; reading a system already doing this job
daily points the other way for three reasons the research could not show. What Ajmal actually runs today
is **C#, not Python**. pyRevit Routes is an **HTTP server**, and Heron's add-in has *no network code at
all* — verified against the source — so adopting it would trade [D-02](../DECISIONS.md)'s structural
local-only guarantee for a configuration promise. And one language means **one compile gate**: C#
fragments go through `check-compile.py` and `check-api-surface.py`; Python fragments would go through
neither.

**This closes [Q-37](from-master-specification-part-2.md#q-37--can-pyrevit-routes-bind-a-per-process-port-new-from-research) as not
applicable** — Heron does not use Routes.

---
