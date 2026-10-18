# 09 — Skills & Fragments

> Derived from [Master Specification](00-master-specification.md) §16–25, §30, §42.
> **[NOTE]** blocks are engineering commentary added during review.

---

## 1. The distinction

The spec uses both terms extensively but never defines the boundary. Proposed definition, to be confirmed:

| | **Skill** | **Fragment** |
|---|---|---|
| Answers | *What the user can ask for* | *How it is actually done* |
| Audience | User-facing | Internal |
| Example | "Select Ducts" | `filter-elements-by-category`, `get-active-selection`, `set-selection` |
| Cardinality | One skill uses many fragments | One fragment serves many skills |
| Named in | BIM language | Technical capability language |
| Changes when | The user's need changes | The API or implementation changes |

A skill is a **capability the user recognises**. A fragment is a **reusable implementation unit**. The generic `Select Elements` fragment in §21 is reused by Select Ducts, Select Pipes, Select Cable Trays, Select Equipment and Select Doors — five skills, one fragment.

Tracked as [Q-8](OPEN-QUESTIONS.md).

> **As built (2026-10-18):** answered by
> [D-29](DECISIONS.md#d-29--a-fragment-is-a-composable-piece-not-a-whole-answer). The skill is
> confirmed as proposed, and the fragment is a composable piece with a declared contract. A third kind,
> the **recipe**, is named for a job that genuinely cannot be composed. Skills are
> [`brain/skills/*.yaml`](../brain/skills/). They name **capabilities, never fragments**, so a fragment
> can be replaced without editing a skill. The chat gets a skill's ordered steps from `heron_method`.
> Fragments are reached by capability through `revit_read`, `revit_show` and `revit_change`
> ([04 §3](04-heron-mcp.md#3-note-tool-granularity--the-central-mcp-design-decision)).

---

## 2. Skill metadata

```text
Skill ID
Name
Description
Domain
Required Agents
Required Fragments
Revit Versions
.NET Versions
Dependencies
Risk Level
Status
Success Rate
Created Date
Updated Date
```

**[NOTE]** Add: **Example utterances** (what the user might actually say) and **Preconditions** (document open, elements selected, worksharing state). Utterances drive matching quality directly; preconditions let Heron fail early with a useful message instead of failing deep inside a transaction.

> **As built (2026-10-18):** both additions were taken. Every skill file carries `utterances` and
> `preconditions`, beside `needs`, the capabilities it uses. `python brain/heron_skill.py` validates the
> skills and counts them.

---

## 3. Fragment content

A fragment is not a snippet of code. It carries:

purpose · inputs · expected output · implementation · API requirements · dependencies · version compatibility · validation rules · usage history · success rate · failure history · related fragments · related skills

## 4. Knowledge identity (§30)

```text
Fragment ID
Semantic Identity
Domain
Capability
Version
Dependencies
Metadata
History
```

Identity is **never the filename**. Rename the file and Heron still knows it is the same knowledge object.

**[NOTE]** Recommendation on storage format: **one folder per fragment**, containing a metadata file (YAML front-matter or JSON), the implementation(s), and tests. Human-readable, git-diffable, mergeable, and reviewable in a pull request. A fragment stored only as a database row cannot be code-reviewed, and code review is the mechanism §40 depends on.

Version-specific implementations live side by side:

```text
fragments/select-elements-by-category/
  fragment.yaml            <- identity, metadata, compatibility
  impl/net48/...           <- Revit 2020-2024
  impl/net8/...            <- Revit 2025+
  tests/
  history.jsonl            <- usage, success, failure
```

> **As built (2026-10-18):** one folder per fragment, as recommended, under
> [`brain/fragments/`](../brain/fragments/). The layout differs in two ways:
>
> ```text
> brain/fragments/<name>/
>   fragment.yaml            <- identity, contract, risk, revit:, runtime:, utterances, proof
>   impl/any/fragment.cs     <- one implementation for every release
>   tests/cases.yaml
> ```
>
> - **There are no per-runtime folders.** Where releases differ, the one file branches on the compile
>   symbols (`REVIT2024_OR_GREATER` and the rest, [16](16-version-support-strategy.md)). Check with
>   `ls brain/fragments/*/impl/ | sort | uniq -c`.
> - **There is no `history.jsonl`.** What ran is in the audit trail and the run records
>   ([`HeronRuns.cs`](../platform/Heron.Core/HeronRuns.cs)), and a fragment's proof is the `proof:` block
>   of its own `fragment.yaml`.
>
> `python brain/heron_fragment.py` validates every card and counts them.

---

## 5. Fragment lifecycle

```text
DISCOVERED -> DRAFT -> TESTING -> VALIDATED -> PROVEN -> PRODUCTION -> DEPRECATED -> ARCHIVED
```

> A fragment should not automatically become production knowledge simply because it worked once.

**[NOTE]** The spec gives the states but not the **gates**. Without explicit criteria, promotion becomes a judgement call an agent will make inconsistently. Proposed gates:

| Transition | Gate |
|---|---|
| DISCOVERED → DRAFT | Has identity + metadata + a stated purpose |
| DRAFT → TESTING | Has tests; passes Fragment Validation |
| TESTING → VALIDATED | Tests pass on **every declared supported version** |
| VALIDATED → PROVEN | N successful real executions, zero unexplained failures, no user corrections *(N to be set — suggest 10)* |
| PROVEN → PRODUCTION | **Human approval.** Explicit, recorded, one person |
| any → DEPRECATED | Replaced, unreliable, or incompatible |
| DEPRECATED → ARCHIVED | After a retention period; never deleted |

The `PROVEN → PRODUCTION` human gate is the fragment-level equivalent of the agent-approval boundary in [06 §4](06-heron-platform.md). It is the point where knowledge starts being trusted without review, and it should cost one human click.

Tracked as [Q-9](OPEN-QUESTIONS.md) for the value of N and who may approve.

> **As built (2026-10-18): superseded by
> [D-30](DECISIONS.md#d-30--a-fragment-is-promoted-by-one-recorded-proof-not-by-a-count-of-runs), not
> a count.** A fragment is promoted on **one recorded proof against a real model**. The proof has a
> positive case, a negative case, and a second route to the answer where one exists, and the person who
> ran it signs it with their name and the date. Its fingerprint makes it stale the moment the code
> changes. The statuses in use are `DRAFT` and `PROVEN`. Count each with
> `grep -h '^heron-status:' brain/fragments/*/fragment.yaml | sort | uniq -c`. No fragment is
> `PRODUCTION`. At run time, `revit_change` keeps a change only from a fragment whose proof still stands
> ([D-117](DECISIONS.md#d-117--a-change-is-kept-only-from-a-fragment-whose-proof-still-stands)). A
> DRAFT may run once on the model in front, behind Revit's Keep or Discard box, and the modeller judges
> that run
> ([D-119](DECISIONS.md#d-119--a-draft-fragment-may-run-once-on-the-model-in-front-behind-a-keep-or-discard-prompt-in-revit-and-article-11-says-so),
> [D-122](DECISIONS.md#d-122--under-article-19-the-modeller-judges-a-drafts-one-run-and-the-agent-that-wrote-it-only-starts-it)).
> Unifying the status words is still only proposed
> ([D-14](DECISIONS.md#d-14--unify-six-status-vocabularies-into-two-orthogonal-axes)).

---

## 6. Fragment Split / Merge / Evolution

- **Split** (§21) — decompose a compound fragment into reusable units. Drives reuse.
- **Merge** (§22) — detect near-identical fragments; propose merge / replace / keep separate / deprecate.
- **Evolution** (§23) — decide: unchanged, improved, adapter, version-specific, split, merge, deprecate.

**[NOTE]** All three **propose**; none may apply autonomously to anything at `PRODUCTION`. Splitting a production fragment silently changes the behaviour of every skill that depends on it. These agents should open a proposal — in practice, a pull request — which is exactly what the GitHub department is for.

**[NOTE]** Split has a real failure mode: over-decomposition. Ten fragments each three lines long, with nine layers of indirection, is worse than one clear fragment. Suggested guard: a fragment should only be split when the extracted part has **at least two distinct consumers, actual or clearly imminent**. Reuse justifies a split; tidiness does not.

---

## 7. Version compatibility (§24)

Every code-bearing fragment records its Revit and .NET compatibility, **determined from the build environment, never guessed**.

Compatibility is a **hard filter before ranking**, not a ranking signal. A fragment that does not support the running Revit version must be structurally unable to be selected. See [05 §8](05-heron-brain.md).

> **As built (2026-10-18):** each card declares `revit:` and `runtime:`, and retrieval's version filter
> leaves an incompatible fragment out rather than ranking it low
> ([05 §4](05-heron-brain.md#4-note-retrieval-is-not-only-vector-search)).
> `python tools/check-fragments-compile.py` compiles every fragment for every release, and
> `tools/check-api-surface.py` checks the Revit members they call. A compile is not a proof. Which
> releases a proof names is [NEEDS-CHECKING Group EQ](needs-checking/group-eq.md).

## 8. Regression testing (§25)

On every change to an existing fragment:

1. Identify supported versions
2. Build/test each supported implementation
3. Compare previous behaviour
4. Detect breaking changes
5. **Reject unsafe changes**
6. **Preserve the previous working implementation**

This is Golden Rule 4 made executable. See [13 — Testing & Quality](13-testing-and-quality.md).

---

## 9. Code generation rule (§42)

Generated code must know and follow: folder structure, naming conventions, metadata standards, coding standards, supported Revit versions, supported .NET versions, existing services, existing fragments, existing agents, dependency rules.

**Reuse existing approved components before generating anything new** (Golden Rule 3).

**[NOTE]** Enforce this mechanically rather than by instruction. Before the Code Generation Agent runs, the Fragment Matcher must have searched and reported. If a proven fragment covers ≥80% of the request, generation is not permitted to start from scratch — it must start from that fragment. Otherwise the knowledge base fills with near-duplicates, and the Merge Agent spends its life cleaning up after the Generation Agent.

> **As built (2026-10-18): enforced, without the percentage.** The Fragment Matcher answers `matched`,
> `partial` or neither, never a score. The Fragment Creation Agent refuses to author when either list is
> non-empty ([`heron_generate.py`](../brain/heron_generate.py),
> [`heron_matcher.py`](../brain/heron_matcher.py)). [PROPOSALS F24](proposals/f24.md) says why `partial`
> is stricter than *"≥80%"*. The host-side loop that searches first and then writes the fragment is the
> [`heron-fragment-author`](../.claude/agents/heron-fragment-author.md) subagent.

---

## 10. **[NOTE]** The runtime execution question

This is unresolved in the spec and it is architecturally decisive: **when Heron generates new code, how does that code actually run inside Revit?**

| Approach | How | Verdict |
|---|---|---|
| **A. Compile & load at runtime** | Roslyn compiles C#, assembly is loaded into Revit | Assemblies **cannot be unloaded** from .NET Framework (Revit ≤2024). Every iteration leaks. Version churn during development makes this painful. `AssemblyLoadContext` helps only on .NET 8 (Revit 2025+). |
| **B. Scripting layer** | Python (pyRevit / IronPython / Python.NET) or Roslyn *scripting* | Iterate freely, no assembly leak, fast feedback. Slower execution; needs a runtime dependency. |
| **C. Precompiled only** | Fragments ship as compiled, signed, tested assemblies | Safest and fastest. New capability requires a build + release cycle, so "Heron writes a new tool during the session" is not immediate. |
| **D. Hybrid** | Scripting for DRAFT/TESTING, precompiled for PRODUCTION | Iteration where it is needed, safety where it matters. |

**Recommendation: D.** It maps exactly onto the fragment lifecycle already specified — experimentation happens in a scripting sandbox, and the `PROVEN → PRODUCTION` gate is precisely where a fragment gets compiled, signed and shipped. The lifecycle in §18 turns out to *describe* this hybrid, which is a good sign the design is coherent.

The user's existing pyRevit work is directly relevant here and may already answer half the question.

Tracked as decision **D-04**, [Q-7](OPEN-QUESTIONS.md).

> **As built (2026-10-18):** [D-04](DECISIONS.md#d-04--generated-code-execution-hybrid) settled *hybrid*,
> and [D-28](DECISIONS.md#d-28--generated-code-is-c-compiled-at-run-time-in-process) named the runtime:
> **Roslyn C# scripting, in process, through the bridge**, with no Python runtime and no HTTP server in
> the add-in. Today every fragment, DRAFT or PROVEN, is compiled when it runs, inside Revit
> ([`RevitFragment.cs`](../revit/Heron.Revit.Addin/RevitFragment.cs)). The compile is cached by the
> fragment's source, so a changed fragment compiles fresh. On Revit 2025 and later the compiler is loaded
> into a load context of Heron's own ([revit/README](../revit/README.md)). The precompiled `PRODUCTION`
> half of the hybrid is not built, because no fragment is `PRODUCTION`.
