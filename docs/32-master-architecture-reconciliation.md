<!--
Heron-Agent:  HERON-DOC-VAL-009
Heron-Step:   17
Heron-Status: DRAFT
Heron-Since:  0.1.0
Heron-Layer:  brain
See docs/29-metadata-standard.md
-->

# 32 — The Master Architecture document, reconciled

**[`HERON_AI_MASTER_ARCHITECTURE.md`](../HERON_AI_MASTER_ARCHITECTURE.md) arrived in this repository on
2026-09-09 as a single commit and referenced nothing already here.** This file is the audit that
document itself demands before anything is built from it — its §3 (*"Existing Project Comes First"*),
its §17 Phase 1 (*"Audit and Architecture Map"*), its §21 (*"Required Research Output Before Coding"*)
and its §24 (*"Do not treat this document as permission for a giant rewrite"*).

It is not a review of whether the incoming document is good. Most of it is. It is the answer to the
only question that decides what happens next:

> **Which parts of it does Heron already have, which parts would make Heron worse, and which parts are
> genuinely missing?**

Every row below cites a file. Where a claim could not be checked, it says so rather than rounding up.

---

## 1. The thing to get right before anything else — what the document thinks Heron is

The incoming document's header states its purpose:

> *"AI engineering system for the existing Heron/Revit development codebase"*

and §1 lists what Heron must improve: *development speed, code quality, debugging, planning, code
review*. Read plainly, that describes a **harness that helps a developer work on the Heron codebase**.

**That is not what Heron AI is.** [01 — Vision & Principles](01-vision-and-principles.md) and the
[README](../README.md) describe a **BIM-modeller-facing platform**: somebody types *"select all ducts"*
and Heron does the analysis, retrieval, code, validation and execution behind it. The user is a
modeller, not a developer, and the [Golden Rules](14-golden-rules.md) open with it — *the user focuses
on BIM, Heron handles the technical complexity.*

**Two products, and building the wrong one is the single most expensive mistake available here.** A
session that read the incoming document alone would start building a Roslyn code graph over
`revit/Heron.Revit.Addin/` to help a developer refactor it, and every hour of that is off-mission.

### But the split is not clean, and pretending it is would be the second mistake

Heron **writes and runs C# against the Revit API as its product**. A fragment body is compiled inside
Revit's own process against the assemblies Revit has loaded ([D-28](DECISIONS.md)). So the incoming
document's engineering concerns — API correctness, transaction safety, version compatibility,
evidence before *done* — are **product requirements here, not developer conveniences**. Its §14 Revit
Validation Gate is about Heron's output, not about Heron's source.

**So the reconciliation is:**

| The incoming document's… | Standing here |
|---|---|
| **engineering discipline** (§2, §4, §9, §14, §15, §19, §20, §22, §23) | **Applies directly.** Most of it is already this repository's practice, some of it word for word |
| **platform modules** (§6) | **Mostly already built**, under other names — §2 below maps each one |
| **framing of Heron as a developer-assist harness** (header, §1, §3) | **Rejected.** It is not the product. See [D-57](DECISIONS.md) |
| **external repository research programme** (§10, §11) | **Not started, and legitimate.** §4 below |

---

## 2. The mapping — every module of §6, against what is on disk

Counts here are **derived, not typed**. The commands that produce them are given, because this
repository has been wrong about its own numbers four times and every one was caught by re-deriving.

```bash
ls brain/fragments | wc -l
grep -h '^heron-status:' brain/fragments/*/fragment.yaml | sort | uniq -c
python tools/agent-count.py
```

| Incoming § | What it asks for | Heron already has | Verdict |
|---|---|---|---|
| **6.1** Context Engine | task-aware retrieval, ranking, dedup, version awareness, traceability | [`brain/heron_retrieve.py`](../brain/heron_retrieve.py) — structured filter first, then keywords + vectors over the survivors, fused by reciprocal rank, and it reports what was excluded and why | **Built** |
| **6.1** …token budgeting, compression | — | **[19 — Context & Cost](19-context-and-cost.md) specifies seven pieces and there is no Context Manager, no budget, no compression, no model router, no fallback and no token or cost accounting.** `grep -rl "ContextManager\|context_manager\|heron_context"` over the whole repository returns nothing. Two of the seven are partly covered by accident of other work — `heron_search` has a `cache` route, and `heron_gaps` reports Revit-side latency (§4.2) | **GAP — the largest real one** |
| **6.2** Repository code graph (project→file→class→method→calls) | Roslyn-grade code intelligence over the Heron source | [`brain/heron_graph.py`](../brain/heron_graph.py) is a **fragment and capability** graph, not a source-code graph. Different object entirely | **Rejected for now — §5** |
| **6.3** Memory Engine, categories, no full-history storage | [`brain/heron_scope.py`](../brain/heron_scope.py) — one store per scope as one file each, [Golden Rule 5](14-golden-rules.md) made physical; a cross-scope query is impossible to *write*, not merely absent. [10](10-memory-and-knowledge.md), [20](20-knowledge-trust-and-conflict.md) | **Built, and stricter** |
| **6.4** Revit Knowledge Engine, version awareness | 360 fragments, [`brain/heron_matrix.py`](../brain/heron_matrix.py) (`CLAIMED` / `COMPILES` / … never merged), [16 — Version Support](16-version-support-strategy.md), the compile gate over 2020–2027 | **Built, and stricter** |
| **6.4** Verified / Project-proven / Version-sensitive / Unverified | [24 — The Unified Trust Model](24-trust-model.md) — two orthogonal axes, which exist **because six competing vocabularies had to be resolved into them** | **Rejected — §5** |
| **6.5** Skill System — trigger, context, steps, constraints, validation | [`brain/heron_skill.py`](../brain/heron_skill.py) and [`brain/skills/`](../brain/skills/) — 10 skills, each naming **capabilities, never fragments**. [09](09-skills-and-fragments.md) | **Built** |
| **6.6** Planning Engine, lightweight for small tasks | [`mcp/server/heron_workflow.py`](../mcp/server/heron_workflow.py) — Workflow Engine and checkpoints. The *planner* itself is the host's by [D-01](DECISIONS.md) | **Built, split differently — §3** |
| **6.7** Eight specialist roles | [28 — The Complete Agent Registry](28-agent-registry.md), 250 agents by department. `python tools/agent-count.py` says how many have code | **Built, at far finer grain** |
| **6.8** Review Engine — requirement, API, version, transaction, performance, dead code | `tools/check-*.py` (`ls tools/*.py \| wc -l`), plus [`brain/heron_validate.py`](../brain/heron_validate.py) and [13 — Testing & Quality](13-testing-and-quality.md) | **Built** |
| **6.9** Optional council, evidence over majority | Shadow Mode, [18](18-agent-operating-system.md) — and [D-39](DECISIONS.md) already says approval comes from an **analysed disagreement, not a count of agreements** | **Built, and stricter** |
| **7** Safe self-improvement, with a human gate | [Golden Rule 13](14-golden-rules.md), Article IV of the [Constitution](../HERON_CONSTITUTION.md), and [`heron_validate.py`](../brain/heron_validate.py) whose whole first rule is *it never writes `heron-status`* | **Built, and stricter** |
| **8** Speed strategy, latency per stage | **Half of it exists and the halves are not interchangeable.** [`brain/heron_gaps.py`](../brain/heron_gaps.py) reads the audit trail and reports median and worst milliseconds **per fragment and per operation** — the Revit side. **The brain times nothing**: `heron_search`, `heron_embed` and `heron_retrieve` contain no clock at all | **Partial — §4.2** |
| **9** Evidence-based done | [D-30](DECISIONS.md) — a proof needs a positive case, a negative case and a staleness fingerprint | **Built, and stricter** |
| **14** Revit Validation Gate, 14 questions | [03 — Heron Revit](03-heron-revit.md), the [fragment-proving skill](../.claude/skills/fragment-proving/SKILL.md), [D-51](DECISIONS.md), [D-53](DECISIONS.md) | **Built — but see §4.3** |
| **15** Task Execution Contract, phases A–G | [27 — Build Order](27-build-order.md) and this repository's working practice | **Built** |
| **18** Evaluation suite and benchmarks | [`tests/golden/cases.py`](../tests/golden/cases.py) remembers proofs and expires them; it does **not** measure retrieval precision, token cost or first-pass correctness | **Partial — §4** |
| **19** Security, repository content as data not instructions | [12 — Security & Permissions](12-security-and-permissions.md), Article III. The specific *prompt-injection* framing is thinner here than in the incoming document | **Adopt the wording — §4.4** |
| **20** Git discipline | Already the practice: small diffs, no force push, a changed-file report | **Built** |

**Nine of the incoming document's platform modules already exist. Four of those are stricter here than
it asks for.** That is the headline, and it is the reason §24 of that document — *do not treat this as
permission for a giant rewrite* — is the sentence in it that matters most.

---

## 3. Three places where Heron is deliberately different, and the difference is not an oversight

A future session comparing the two documents will read these as gaps. They are not.

### 3.1 The planner lives in the host, not in Heron

Incoming §6.6 wants a Planning Engine inside the system. [D-01](DECISIONS.md) put the orchestrator,
intent classification, persona and summarisation **in Claude Code**, and
[`tools/check-metadata.py`](../tools/check-metadata.py) prints those four agents every run as
*provided by the host, so no file here implements them*. Heron owns the Workflow Engine — ordering,
retries, checkpoints, resumption — which is the half that must survive a crashed session.

Building a second planner inside Heron would duplicate the host and break [Golden Rule 2](14-golden-rules.md).

### 3.2 The version filter is a wall, not a ranking input

Incoming §6.1 lists Revit version awareness among several ranking signals.
[`heron_retrieve.py`](../brain/heron_retrieve.py) makes it a hard `WHERE`: an incompatible fragment is
**absent**, not demoted. The reasoning is written in that file and is worth repeating here, because
treating it as a weight is a natural-looking regression:

> a 2021 fragment applied in 2025 does not announce itself; it runs, half-works, and the damage is
> found later by somebody measuring something.

### 3.3 A fragment's status is never written by a machine

Incoming §7 permits an agent to *store validated fixes*. Here, evidence is gathered by an agent and
**signed by a person** — drafts go to [`brain/proof-drafts/`](../brain/proof-drafts/), never into
`brain/fragments/`. [D-30](DECISIONS.md) exists because an unproven claim quietly ages into a believed
one, and an agent able to stamp 218 fragments is the fastest machine ever built for doing that.

---

## 4. What is genuinely missing — ranked by what it costs to leave undone

Each is put through the incoming document's own §22 decision standard before it is called work.

### 4.1 🔴 The Context Manager — specified in [19](19-context-and-cost.md), never built

**What problem it solves.** Every retrieval today returns what it returns. Nothing budgets tokens,
nothing compresses, nothing caches a resolved wording, nothing records what a request cost.

**Does Heron already solve it?** No. Seven sections of [19](19-context-and-cost.md) — Context Manager,
Compression, Model Router, Fallback, Cost Optimisation, Caching, Observability — have **no
implementation of any kind**. This is the largest specified-but-absent area in the repository, and
until now nothing said so in one place.

**Simplest Heron-native design.** Not a framework. `heron_search.py` already has a `cache` route and
`heron_retrieve.py` already reports what it excluded — the missing piece is a budget the retriever is
handed and a record of what each stage cost.

**New failure modes.** Compression that drops a Revit token (`OST_DuctCurves`, a shared-parameter GUID)
is exactly the failure [05 §4](05-heron-brain.md) built the keyword layer to avoid. **Any compression
must be forbidden from touching the exact-match corpus.**

**Verdict: ADOPT, and it needs the baseline in §4.2 first.**

### 4.2 🔴 A baseline for the brain — the Revit side already has one

> *"Without a baseline, improvement cannot be proven."*

**This entry said *"nothing here measures latency"* when it was first written, and that was wrong.**
It was checked an hour later and corrected, which is the only reason it is not now a believed fact —
and it is a fair warning about reading either document without opening the files.

What actually exists, and what does not:

| | |
|---|---|
| **Revit side — measured** | [`brain/heron_gaps.py`](../brain/heron_gaps.py) reads the audit trail and reports **median and worst milliseconds per fragment and per operation**, alongside what failed. [`HeronAudit`](../platform/Heron.Core/HeronAudit.cs) has written a duration per request since Step 4 |
| **Brain side — not measured at all** | `grep -n "perf_counter\|monotonic"` over [`heron_search.py`](../brain/heron_search.py), [`heron_embed.py`](../brain/heron_embed.py) and [`heron_retrieve.py`](../brain/heron_retrieve.py) returns **nothing**. None of them writes to the audit trail either. The whole retrieval stack — the exact place a Context Manager would be judged — is unmeasured |

**And one part of it is not measurable here at all, which is an architecture finding rather than a gap.**
[28](28-agent-registry.md) defines `HERON-OPS-OBS-011`, the Observability Agent, as *"latency, token
usage, model calls per request, cost per request"*. **[D-01](DECISIONS.md) put every model call in the
host.** Heron makes none — `heron_embed` runs a local model with no tokens and no cost. So three of
those four fields describe something Heron cannot see, and building an agent to report them would
produce a meter reading zero and a reader who believed it.

**Verdict: ADOPT, narrowed to the brain's own stage timings**, on the same seam
[`heron_gaps.py`](../brain/heron_gaps.py) already uses so there is one reader of the trail rather than
two. The registry row wants correcting at the same time, because a row asking for the impossible is
worse than a row asking for nothing.

**The value is not hypothetical.** Closing register row `A7` made `heron_capabilities` never reply, and
a real Claude Code tool call sat on it for **thirty minutes** — found with `faulthandler`, not by
reasoning ([D-49](DECISIONS.md)). It was in the retrieval stack: the unmeasured half.

### 4.3 🟠 The Revit Validation Gate as a checklist an agent runs

The fourteen questions in incoming §14 are already Heron's rules — but they are spread across
[03](03-heron-revit.md), the [fragment-proving skill](../.claude/skills/fragment-proving/SKILL.md),
[D-51](DECISIONS.md), [D-53](DECISIONS.md) and [`FRAGMENT-ISSUES.md`](FRAGMENT-ISSUES.md). **Nothing
runs them as a list.** The five mistakes the proving skill names as *nearly every failed proof* are
five of those fourteen questions, which is evidence the checklist form would pay.

**Verdict: ADAPT.** Not a new agent — a checklist [`heron_validate.py`](../brain/heron_validate.py)
already has the evidence to answer, restated as questions it must answer before drafting a proof.

### 4.4 🟠 Repository content is data, not instructions

Incoming §19 states it plainly. Heron's [12](12-security-and-permissions.md) covers permission levels
and egress; the *prompt-injection* case — a fragment's own comment, a doc, an issue body telling an
agent what to do — is named in the [Golden Rules](14-golden-rules.md) only as a proposed rule.

Heron now compiles fragment C# **inside Revit's process**, so this stopped being theoretical.

**Verdict: ADOPT the wording into [12](12-security-and-permissions.md).** No code needed today.

### 4.5 🟡 The external repository research matrix — incoming §10 and §11

Fifteen repositories, of which the document itself says five need their exact repository identified
first. [26 — Prior Art](26-prior-art-revit-mcp.md) is the only research of this kind here, and it
covers Revit MCP servers only.

**This is legitimate and unstarted work.** It is ranked last deliberately: nine of the modules those
repositories would be studied *for* already exist here (§2), so the matrix's realistic yield is
narrower than the list implies — and [D-25](DECISIONS.md) plus
[31 — Studying The Existing Libraries](31-studying-the-existing-libraries.md) already fix the method,
which is the part that usually goes wrong.

**Verdict: RESEARCH FURTHER.** Not before §4.1 and §4.2.

---

## 5. Rejected, and why — the part of this file that saves the most time

Rejections are recorded here rather than left as silence, because a future session will otherwise read
the incoming document and re-propose every one of them.

| Rejected | Reason |
|---|---|
| **A Roslyn code graph over the Heron source** (§6.2, §12) | It serves the developer-assist product Heron is not (§1). Its cost is a compiler-platform dependency in a project whose [D-42](DECISIONS.md) install story is *per-user, no administrator rights* — and [`tools/check-structure.py`](../tools/check-structure.py) already enforces the layering a graph would be built to police. **Re-open only if a concrete Heron requirement needs it**, which is the incoming document's own §11 test |
| **The four API-verification labels** — Verified / Project-proven / Version-sensitive / Unverified (§6.4) | [24](24-trust-model.md) exists **because six competing status vocabularies had to be collapsed into two axes**. Adding a seventh vocabulary would undo the single largest piece of clean-up in this specification. The information those four labels carry is already expressible: lifecycle × source, plus the matrix's `CLAIMED`/`COMPILES` split |
| **A second Planning Engine inside Heron** (§6.6) | [D-01](DECISIONS.md) — see §3.1 |
| **Adding a specialist role per §6.7's list of eight** | [28](28-agent-registry.md) already defines 250 agents by department; the eight are a coarser cut of the same thing. Adding them would create a second registry — and [Golden Rule 2](14-golden-rules.md), *one agent one responsibility*, is what the registry exists to hold |
| **Treating §1's improvement list as Heron's success measure** | Development speed and code review quality are how this repository is *worked on*. Heron's success measure is [01](01-vision-and-principles.md)'s: a modeller gets the right BIM outcome, safely, without knowing any of this exists |

---

## 6. What this document does NOT change

- **The four-part specification is still the source of truth.** [00](00-master-specification.md),
  [00b](00b-master-specification-agent-os.md), [00c](00c-master-handover-baseline.md),
  [00d](00d-additional-requirements.md). The incoming document does not supersede any of them and is
  not a fifth part.
- **The [Constitution](../HERON_CONSTITUTION.md) is untouched.** Nothing in the incoming document
  conflicts with any of its 30 Articles; several sections restate them.
- **No Golden Rule is changed, added or renumbered.**
- **`write.enabled` stays where [D-19](DECISIONS.md) put it.** Nothing here asks for it.
- **No fragment status moves.** Reconciling a document proves nothing about a model.
- **[`NEEDS-CHECKING.md`](NEEDS-CHECKING.md) gains no rows**, because nothing here is a claim awaiting
  a Revit. The gaps in §4 are unbuilt work, which is a different thing, and conflating the two is what
  that register exists to prevent.

---

## 7. What a session picking this up should do, in order

1. **Read §1.** If the next thing you are about to build helps a developer edit `revit/`, stop.
2. **§4.2 — the baseline.** Cheapest, gates the rest, and this repository already knows the cost of
   not having it ([D-49](DECISIONS.md)).
3. **§4.1 — the Context Manager**, against that baseline, with compression forbidden from the
   exact-match corpus.
4. **§4.3 and §4.4** — both small, neither blocked by anything.
5. **§4.5** last, and only against a named Heron requirement.

**None of that competes with the proving pass.** Proving fragments needs Revit open and is the
project's critical path; everything in §4 runs on any machine. They are different queues, and this
document does not move anything to the front of the other one.
