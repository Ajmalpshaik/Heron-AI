# 19 — Context Management, Model Routing & Cost

> Derived from [Master Specification Part 2](00b-master-specification-agent-os.md) §14–§18, §58–§60.
> **[NOTE]** blocks are engineering commentary added during review.

---

## 1. Context Manager

The Context Manager decides what each agent actually receives:

what the user said · what project is active · what Revit version is active · what knowledge is
relevant · what fragment is relevant · what skill is relevant · what previous conversation is relevant ·
what technical information is required

> **Only required information should be passed to the agent.**

**[NOTE]** This is one of the highest-leverage components in the platform and it is easy to
under-build. AI systems do not fail loudly when over-fed context — they fail *quietly*, by attending to
the wrong thing and producing a confident, plausible, wrong answer. In a system that then writes to a
live project model, that is the dangerous failure mode.

The Context Manager should be **T1 wherever possible** — deterministic filtering by project, version,
scope and capability, not a model deciding what is relevant. Using a model to choose context for
another model doubles cost to solve a problem that structured filters solve for free.

---

## 2. Context Compression

With 20,000 fragments, 5,000 skills and 500 agents, nothing can send everything.

```text
User Request -> Semantic Classification -> Relevant Domain -> Relevant Project
-> Relevant Capability -> Relevant Knowledge -> Minimal Context
```

Reduces token usage, latency, confusion, hallucination and cost.

**[NOTE]** Note the order: **domain and project narrow before semantics do.** That is correct and worth
protecting — it is a structured filter (cheap, exact, and it enforces
[Golden Rule 5](14-golden-rules.md) scope isolation at query time) applied before anything expensive
runs. The same principle as the retrieval stack in [05 §4](05-heron-brain.md).

**[NOTE]** A concrete budget is worth setting now, because "minimal context" is otherwise unfalsifiable:

| Path | Context budget |
|---|---|
| Cached utterance → known skill | none — no model call at all |
| Simple BIM task | intent + active project + Revit version + one matched capability |
| Standards check | the above + the specific standard clauses cited, not the whole standard |
| Code generation | the above + the closest existing fragment + its tests + the API surface used |

If a context assembly exceeds its budget, that is a bug in retrieval, not a reason to raise the budget.

---

## 3. Model Router

| Task | Model class |
|---|---|
| Simple task | Fast model |
| Complex reasoning | Stronger reasoning model |
| Code generation | Coding model |
| Document classification | Efficient model |
| Critical architectural decision | High-reasoning model + validation |

**[NOTE — tension with [D-01](DECISIONS.md), needs resolving]**

Heron runs as a **Claude Code plugin**. In that arrangement **Claude Code chooses the model**, not
Heron. A Heron-owned Model Router partly duplicates something the host already does, and the two could
disagree.

Three ways to reconcile it:

| Option | How | Assessment |
|---|---|---|
| **A. Host routes; Heron declares intent** | Heron's agent/subagent definitions declare their reasoning needs; Claude Code resolves them to models | Simplest. Works today. Heron never hard-codes a model id. **Recommended.** |
| **B. Heron routes for its own calls** | Heron's Python brain calls model APIs directly for T2 work, routing itself | Full control and real cost savings on high-volume T2 work. But adds an API key, a second billing relationship, and duplicated capability |
| **C. Hybrid** | Host routes conversational work; Heron routes internal batch work (embedding, classification, bulk scoring) | Likely the real end state. Batch classification of 20,000 fragments should not run on a frontier model |

**Recommendation: A now, C later.** The routing *intent* — which is what §17 actually specifies — gets
recorded in the capability registry as the **cost tier** regardless of who resolves it. That way the
design survives either choice.

Tracked as [Q-30](OPEN-QUESTIONS.md).

**[NOTE]** Whatever is chosen, never hard-code model identifiers in agent definitions. Models change
faster than architectures. Declare *"this needs strong reasoning"*, and resolve it in one place.

---

## 4. Model Fallback

```text
Primary Model -> Failure -> Fallback Model -> Validation
```

> The system should never silently produce an inferior result without awareness.

**[NOTE]** Two rules that make this safe rather than merely resilient:

1. **A fallback result is marked as such** — in the audit log, in the confidence value, and in the
   fragment's execution history. A result produced under fallback must never be counted as evidence
   toward promoting a fragment to `PROVEN`.
2. **Fallback does not apply to high-risk work.** If a `MODIFY` operation's reasoning step fell back to
   a weaker model, stop and tell the user rather than proceeding with reduced confidence. Falling back
   is fine for classification; it is not fine for something about to change 247 ducts.

---

## 5. Cost Optimization

> - If a **proven fragment exists** → **DO NOT GENERATE NEW CODE.**
> - If an **exact answer exists in trusted knowledge** → do not reason expensively.

**[NOTE]** Part 2 states from the *waste* side what [02 §6](02-architecture-overview.md) derived from
the *architecture* side and §83 states from the *modularity* side. All three converge on the same rule,
which is a strong signal it is correct:

> **The common case must be deterministic. The model is for the uncommon case.**

Made concrete as an enforced pipeline order:

```text
1. Utterance cache hit?          -> execute, 0 model calls
2. Capability exact match?       -> execute proven fragment, 0 model calls
3. Capability semantic match?    -> 1 T2 call to confirm intent, then execute
4. No capability?                -> full workflow (expensive, tell the user)
```

Steps 1 and 2 must be tried **before** any model is invoked, structurally — not as an optimisation
added later. If step 4 is ever reached for *"select all ducts"* after the first time, something is broken.

---

## 6. Caching

Caches: capability lookup · fragment lookup · API knowledge · version compatibility ·
dependency analysis · project context.

> Cache invalidation must happen when relevant knowledge changes.

**[NOTE]** The [event system](18-agent-operating-system.md) is what makes invalidation tractable —
`FragmentUpdated` invalidates the fragment and capability caches; `RevitOpened` invalidates version and
project context. Without events, cache invalidation becomes guesswork and stale results are worse than
no cache.

**[NOTE]** Add one cache Part 2 does not list, and which pays for itself faster than any of the others:

> **Utterance cache** — normalised user phrasing → resolved capability.
> After the first *"select all ducts"*, every later identical request skips intent detection entirely.

Invalidate on `SkillCreated`, `FragmentApproved` and project change. In real BIM work the same handful
of phrases recur constantly, so hit rates should be high.

---

## 7. Observability

Metrics: task latency · agent latency · retrieval latency · model latency · token usage ·
success rate · failure rate · retry count · rollback count.

**[NOTE]** Two additions worth making, both cheap:

| Metric | Why |
|---|---|
| **Model calls per user request** | The single number that tells you whether the T1/T2/T3 discipline is holding. If it drifts from ~1 to ~12, the architecture is eroding. |
| **Cost per user request** | Makes the [visible cost meter](PROPOSALS.md) possible, and turns cost from a monthly surprise into a live signal. |

**[NOTE]** These metrics are also the input to the **Capability Gap** report and the
**Agent Optimizer**. They should be written into the same audit log
([12 §5](12-security-and-permissions.md)) rather than a separate telemetry system — one append-only
record, many readers.
