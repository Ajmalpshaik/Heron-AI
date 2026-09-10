# 24 — The Unified Trust Model

> Resolves a conflict across all four specification documents.
> **[NOTE]** blocks are engineering commentary. This document proposes a resolution and needs
> confirmation — see [Q-34](OPEN-QUESTIONS.md).

---

## 1. The problem

Across the four documents, **six different vocabularies** describe how much Heron trusts something.
They overlap, they disagree, and they are applied to the same objects.

| # | Source | Vocabulary |
|---|---|---|
| 1 | [Part 1 §18](00-master-specification.md) — fragment lifecycle | `DISCOVERED` → `DRAFT` → `TESTING` → `VALIDATED` → `PROVEN` → `PRODUCTION` → `DEPRECATED` → `ARCHIVED` |
| 2 | [Part 2 §21](00b-master-specification-agent-os.md) — knowledge trust | `EXPERIMENTAL` → `UNVERIFIED` → `TESTED` → `VALIDATED` → `PROVEN` → `PRODUCTION` → `DEPRECATED` |
| 3 | [Part 3 §10](00c-master-handover-baseline.md) — agent status | `PROPOSED` → `DESIGN` → `DEVELOPMENT` → `TRAINING` → `TESTING` → `SHADOW` → `APPROVED` → `PRODUCTION` → `DEPRECATED` → `ARCHIVED` |
| 4 | [Part 3 §44](00c-master-handover-baseline.md) — skill lifecycle | `DRAFT` → `TESTING` → `VALIDATED` → `PROVEN` → `PRODUCTION` → `DEPRECATED` |
| 5 | [Part 4 §38](00d-additional-requirements.md) — trust levels | `UNKNOWN` → `EXPERIMENTAL` → `TESTED` → `VERIFIED` → `PROVEN` → `OFFICIAL` |
| 6 | [Part 4 §47](00d-additional-requirements.md) — knowledge levels | `RAW` → `TESTED` → `VERIFIED` → `PROVEN` |

**Why this is a real problem, not a tidiness complaint:**

- Retrieval ranks by trust ([20 §3](20-knowledge-trust-and-conflict.md)). Six scales means six answers
  to *"is this trustworthy enough to run on a live model?"*
- Promotion gates are defined per-vocabulary. A fragment at `VALIDATED` and a fragment at `VERIFIED` are
  not comparable, and nobody can say which is safer.
- [Golden Rule 6](14-golden-rules.md) — *experimental must stay separate from production* — is
  unenforceable if "experimental" means four different things.
- [Golden Rule 10](14-golden-rules.md) requires every important object to have a lifecycle. It does not
  survive six of them.

---

## 2. **[NOTE]** Why the vocabularies kept multiplying

They are not six versions of one idea. They are **two different ideas that were repeatedly collapsed
into one linear scale**:

| Idea | Question it answers | Changes over time? |
|---|---|---|
| **Lifecycle** | *How far has this been proven?* | Yes — it advances |
| **Source** | *Where did this come from?* | No — it is fixed at creation |

`OFFICIAL` (§38) is not a stage past `PROVEN` — it means *"shipped by Heron"*, which is a **source**.
`UNKNOWN` (§38) is not a stage before `EXPERIMENTAL` — it means *"provenance unclear"*, also a **source**.

Once those two are separated, the six collapse to two clean axes and everything lines up.

---

## 3. Proposed resolution — two orthogonal axes

### Axis 1 — Lifecycle (one vocabulary, all objects)

Applies identically to **fragments, skills, capabilities and agents**.

| Stage | Meaning | Gate to enter |
|---|---|---|
| `DISCOVERED` | Exists; identity assigned; nothing proven | Has identity + metadata + stated purpose |
| `DRAFT` | Designed and implemented; not yet tested. *(Agents: includes design, build and training)* | Implementation exists |
| `TESTING` | Tests exist and are running | Tests written by someone other than the implementer ([Golden Rule 7](14-golden-rules.md)) |
| `VALIDATED` | Tests pass on **every declared supported version** | Full matrix green |
| `SHADOW` | Running against real requests; **output not used** | Validated + a shadow plan ([18 §4](18-agent-operating-system.md)) |
| `PROVEN` | N successful real executions, no unexplained failures, no user corrections | N met *(suggest 10)* |
| `PRODUCTION` | Trusted by default | **Human approval.** Explicit, recorded |
| `DEPRECATED` | Superseded, unreliable, or incompatible | — |
| `ARCHIVED` | Retained for history | Never deleted |

**Absorbing the old vocabularies:**

- Agent `PROPOSED`/`DESIGN`/`DEVELOPMENT`/`TRAINING` → `DISCOVERED` and `DRAFT`
- Agent `APPROVED` → `PRODUCTION`
- `EXPERIMENTAL`/`UNVERIFIED`/`RAW` → `DISCOVERED`/`DRAFT`
- `TESTED` → `TESTING`/`VALIDATED`
- `VERIFIED` → `SHADOW`/`PROVEN`

**[NOTE]** `SHADOW` is the genuinely new stage, from [Part 2 §10](00b-master-specification-agent-os.md),
and it belongs in the common lifecycle rather than only in the agent one. A *fragment* can shadow too —
run in parallel with the production fragment, results compared, output discarded. That is the cheapest
possible way to gather the evidence `PROVEN` requires without risking a model.

### Axis 2 — Source (fixed at creation, never advances)

| Source | Meaning |
|---|---|
| `OFFICIAL` | Shipped with Heron |
| `COMPANY` | Approved by the user's organisation |
| `PROJECT` | Created for one specific project |
| `USER` | Created by this user |
| `COMMUNITY` | Contributed by a third party |
| `IMPORTED` | Brought in from an external folder; origin path recorded |
| `UNKNOWN` | Provenance unclear — **quarantine** |

**[NOTE]** Source maps directly onto the knowledge scopes ([10](10-memory-and-knowledge.md)) and onto
the knowledge hierarchy ordering ([20 §2](20-knowledge-trust-and-conflict.md)). That is not a
coincidence — it is the same distinction, and unifying them removes yet another parallel concept.

### Derived — the four knowledge levels (§47) as a coarse band

Part 4's four levels are genuinely useful for **ranking and for talking to users**. They become a
derived band over lifecycle, not a seventh vocabulary:

| Level | Lifecycle stages | May be used for |
|---|---|---|
| **L1 — RAW** | `DISCOVERED`, `DRAFT` | Nothing automatic. Inspection only |
| **L2 — TESTED** | `TESTING`, `VALIDATED` | `READ` / `ANALYZE` / `SUGGEST` |
| **L3 — VERIFIED** | `SHADOW`, `PROVEN` | `MODIFY` **with a preview the user accepts** |
| **L4 — PROVEN** | `PRODUCTION` | `MODIFY` unattended |

**[NOTE]** This table is where the trust model stops being bookkeeping and starts doing work. It is the
direct implementation of proposed [Golden Rule 17](14-golden-rules.md) — *no autonomous write to a live
model without a preview or a proven fragment* — and it makes [Golden Rule 6](14-golden-rules.md)
mechanically enforceable: L1 knowledge is structurally unable to reach a live model.

---

## 4. Selection formula

Retrieval preference, in strict order:

| | | |
|---|---|---|
| 1 | **HARD FILTER** | version compatibility — non-matching is unselectable, never ranked down |
| 2 | **HARD FILTER** | scope — Golden Rule 5, enforced at query time |
| 3 | **HARD FILTER** | level >= required for the operation's risk |
| 4 | **RANK** | knowledge hierarchy by source (Project > Company > User > Community > Official) |
| 5 | **RANK** | level, descending |
| 6 | **RANK** | success rate for this Revit version |
| 7 | **RANK** | recency |

**[NOTE]** Steps 1–3 are filters, not weights. This is the difference between *"we preferred a safer
fragment"* and *"an unsafe fragment could not be chosen"*. For anything that writes to a model, only the
second is acceptable.

**[NOTE]** Step 4 puts *source* above *level* deliberately: a `PROVEN` project-specific fragment should
beat a `PRODUCTION` generic one, because the project's own standard is the more correct answer. That is
[Part 2 §20](00b-master-specification-agent-os.md)'s hierarchy, and it is how a competent engineer
weighs sources. The override must be stated to the user, not silent
([20 §2](20-knowledge-trust-and-conflict.md)).

---

## 5. What this replaces

| Document | Vocabulary | Status |
|---|---|---|
| Part 1 §18 | fragment lifecycle | **superseded** — absorbed into Axis 1 |
| Part 2 §21 | knowledge trust | **superseded** — absorbed into Axis 1 |
| Part 3 §10 | agent status | **superseded** — absorbed into Axis 1 |
| Part 3 §44 | skill lifecycle | **superseded** — absorbed into Axis 1 |
| Part 4 §38 | trust levels | **split** — lifecycle half into Axis 1, `OFFICIAL`/`UNKNOWN` into Axis 2 |
| Part 4 §47 | knowledge levels | **kept**, as a derived band over Axis 1 |

Nothing is lost. Every distinction any of the six drew is still expressible — several of them more
precisely than before, because an object can now be `PROVEN` **and** `COMMUNITY`, which no single scale
could say.

---

## 6. **[NOTE]** One caution about numbers

Trust is a measurement, and measurements mislead when their basis is hidden. Three rules, repeated from
[18 §5](18-agent-operating-system.md) and [20 §9](20-knowledge-trust-and-conflict.md) because they apply
to everything in this document:

1. **Success ≠ correct.** A fragment that selects 0 ducts "succeeded". Validation must assert the
   *expected* outcome, not merely the absence of an exception.
2. **A user correction is a failure**, even when the operation technically succeeded. Undo or rephrase
   immediately after is the strongest negative signal available.
3. **Level and success rate are per Revit version.** A blended figure hides exactly the version-specific
   breakage that [D-05](DECISIONS.md) makes most likely. `PROVEN` on 2024 says nothing about 2025.

And sample size travels with the score. **3/3 is not 100%.**

---

## 7. The other trust question — **who is speaking**

**Added 2026-09-09, from reading [`alibaba/open-code-review`](https://github.com/alibaba/open-code-review)
at file level ([33 §5.13](33-external-repository-research.md)).** It ships an `ASSURANCE_CASE.md` whose
first table is not a lifecycle at all:

| their actor | their trust level |
|---|---|
| Local user | Trusted |
| LLM provider API | Semi-trusted — responses validated before use |
| **Git repository** | **Semi-trusted — diffs may contain adversarial content** |
| Network | Untrusted |

**This is not a seventh vocabulary and it is not Axis 3.** §1 collected six scales that all answered
*how proven is this artifact*, and §3 replaced them with two axes that answer it properly. **This
answers a different question** — *how much may I believe what is talking to me right now* — and none of
the six ever asked it. An artifact has a lifecycle and a source. A **speaker** has neither.

Heron already decides this per source; it has never been in one place, which is why
[Q-51](OPEN-QUESTIONS.md) was hard to state. **Every row below is derived from a rule that already
exists** — nothing here is new policy.

| Who or what is speaking | Trust | Because |
|---|---|---|
| **The modeller at the keyboard** | **Trusted — the only source of permission** | [Golden Rule 19](14-golden-rules.md): *permission comes from the user, through Heron's own UI, per action* |
| **The host** (Claude Code) | **Trusted to orchestrate. Never to authorise** | [D-01](DECISIONS.md) gives it conversation, intent and planning. [`RevitDispatcher.cs`](../revit/Heron.Revit.Addin/RevitDispatcher.cs), verbatim: *"The read/change word comes from the tool registry, looked up by operation name (Golden Rule 19). **Nothing in the request decides it**"* |
| **A request arriving on the pipe** | **Trusted as transport. Untrusted as authority** | the boundary is [`RevitOperations.cs`](../revit/Heron.Revit.Addin/RevitOperations.cs)'s guard — *undeclared is refused*, then the stop, then the permission level — reading risk from `HeronOperationRegistry` by operation name. [`heron_bridge_client.py`](../mcp/client/heron_bridge_client.py) checks the same thing earlier and **says in its own words that it is not the boundary**: *"a client-side guard against a MISTAKE, not a boundary against malice — a caller that skips this client is unaffected"* |
| **The model's own content** — *documents, family names, parameter descriptions, imported folders, model text and community packages* | **Semi-trusted. Data, never instruction** | [Golden Rule 19](14-golden-rules.md), quoted verbatim. **That list is the rule's own and is not to be paraphrased** — the first draft of this row wrote *type names, parameter values, comments*, which are three things the rule does not say and two of its six left out |
| **A linked document's content** | **Semi-trusted — and it may be silently absent** | [Q-48](OPEN-QUESTIONS.md): 62 reading fragments collect the host only and say so nowhere |
| **The Revit API's answer** | **Trusted — about the document it names, and no other** | [25 §4](25-multi-session-and-binding.md) — *document binding, the most dangerous finding* — and [25 §5](25-multi-session-and-binding.md)'s stale read. A bare number is how somebody acts on a count from a model they were not looking at, so every answer names its document |
| **An imported community package** | **Semi-trusted until validated — and its licence is unread** | [22 §5](22-users-modes-and-extensibility.md) — *Data isolation* — classes Community Data as *"shipped or downloaded, **treated as untrusted until validated**"*; [Q-53](OPEN-QUESTIONS.md) is that nothing checks what it permits |
| **An indexed standard or specification** | ⚠️ **UNDECIDED — [Q-51](OPEN-QUESTIONS.md)** | the source does not exist yet. [`heron_context.py`](../brain/heron_context.py) says so in the part list: `STANDARD = "standard"  # the clauses cited - source does not exist yet` |
| **The network** | **Not in the threat model at all** | [D-24](DECISIONS.md) and [D-26](DECISIONS.md): local, offline, no account. Heron needs no network to answer |

**Two things this table makes visible that prose did not.**

**One row is undecided and it is the only one.** Everything Heron reads today comes from a source whose
trust is already settled by a binding rule. [Q-51](OPEN-QUESTIONS.md) is the single open cell, and it
opens on the day the RAG index exists — which is the same day retrieval becomes worth having.

**The last row is a strength, and it is easy to trade away by accident.** Most of `ASSURANCE_CASE.md` —
TLS, a semi-trusted provider API, DNS rebinding against a local viewer — describes attack surface Heron
**does not have**, because it makes no model calls ([D-58](DECISIONS.md)) and needs no network. That is
not luck; it is [D-24](DECISIONS.md) and [D-26](DECISIONS.md) being expensive on purpose. Any future
feature that puts Heron on the network is not adding a feature, it is **adding this whole table's worth
of rows**.
