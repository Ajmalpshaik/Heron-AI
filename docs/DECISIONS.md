# Decision Log

> Every architectural decision that has been **made**, with the reasoning behind it.
> Answers from [OPEN-QUESTIONS.md](OPEN-QUESTIONS.md) are promoted here once settled.
>
> This log is append-only. A decision that is later reversed gets a **new entry** that supersedes
> the old one — the original stays, so the reasoning history is never lost.
> This mirrors Golden Rule 3: never destroy a working record.

---

## Format

Each decision uses this shape:

```markdown
## D-NN — Short title

**Status:** Proposed | Accepted | Superseded by D-NN | Rejected
**Date:** YYYY-MM-DD
**Question:** Q-NN
**Affects:** which documents / components

### Context
What made this decision necessary.

### Decision
What was decided. One or two sentences, stated plainly.

### Alternatives considered
What else was on the table, and why it lost.

### Consequences
What this makes easy. What this makes hard. What it locks in.
```

---

## Decisions

### D-00 — Documentation-first, no implementation yet

**Status:** Accepted
**Date:** 2026-08-27
**Question:** —
**Affects:** the whole repository

#### Context

The full platform vision was specified in one pass. Building from it directly would mean designing
~150 agents against untested assumptions about how Revit, MCP and code execution actually interact.

#### Decision

The repository holds **specification and planning only** until the Tier 1 questions in
[OPEN-QUESTIONS.md](OPEN-QUESTIONS.md) are answered. No implementation code is written before then.

#### Alternatives considered

- **Start building the Revit add-in immediately** — rejected: the transport, execution model and target
  version are all undecided, so any code written now would likely be rewritten.
- **Prototype first, document later** — rejected: the owner explicitly asked for the plan to be captured
  and finalised first, and the specification is large enough that leaving it undocumented risks losing it.

#### Consequences

- The architecture is inspectable and arguable before anything is committed to code.
- The 26 open questions are visible rather than discovered one at a time during implementation.
- Nothing runs yet. The first working code is Phase 0 in [ROADMAP.md](ROADMAP.md).

---

### D-01 — Execution host *(pending)*

**Status:** Proposed
**Question:** [Q-1](OPEN-QUESTIONS.md)

Claude Code plugin, standalone app, or both. Recommendation: host-agnostic engine, Claude Code
front-end first, in-Revit pane later.

---

### D-02 — MCP ↔ add-in transport *(pending)*

**Status:** Proposed
**Question:** [Q-2](OPEN-QUESTIONS.md)

Recommendation: named pipes, add-in as pipe server, pipe name encoding Revit version + PID.

---

### D-03 — MCP tool granularity *(pending)*

**Status:** Proposed
**Question:** [Q-5](OPEN-QUESTIONS.md)

Recommendation: thick, specific tools mapping one-to-one onto fragments. Generic execute only in
Developer Persona behind `ADMIN`.

---

### D-04 — Generated code execution model *(pending)*

**Status:** Proposed
**Question:** [Q-7](OPEN-QUESTIONS.md)

Recommendation: hybrid — scripting for DRAFT/TESTING, compiled for PRODUCTION, matching the fragment lifecycle.

---

*Add new decisions below as they are made.*
