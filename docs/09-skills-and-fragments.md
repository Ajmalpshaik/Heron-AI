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
