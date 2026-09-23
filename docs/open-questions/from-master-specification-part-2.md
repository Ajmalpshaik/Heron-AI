# Open questions — From Master Specification Part 2

> One section of [the register](../OPEN-QUESTIONS.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## From Master Specification Part 2

### 🟠 Q-29 — How does Shadow Mode work per agent type?

[Part 2 §10](../00b-master-specification-agent-os.md) introduces Shadow Mode — an agent observes and
recommends without modifying production data. "Observe without modifying" means different things for
different agents, and needs defining:

| Agent type | Proposed shadow behaviour |
|---|---|
| Read-only (T1) | Run normally, compare output against the production agent |
| Analysis / ranking (T2) | Run in parallel, log both, score agreement |
| Model-modifying (`MODIFY`) | Produce the **preview only**. Never open a transaction |
| Code-generating (T3) | Generate and test in the sandbox. Never promote |

Also: how many shadow runs before `SHADOW MODE → APPROVED`, and does a human still sign it off?
*(Recommendation: yes — evidence plus a signature.)*

→ [18 §4](../18-agent-operating-system.md)

**Answer: the table stands as proposed; promotion needs an analysed DISAGREEMENT plus a signature, not a
count of runs. See [D-39](../DECISIONS.md).**

Not a count, for the same reason [D-30](tier-2.md#q-9--what-promotes-a-fragment-to-production) is not one.
**Agreement is weak evidence**: two implementations are often wrong the same way, because the second was
written by somebody who read the first — and an agent that silently does nothing agrees with everything. A
hundred agreements prove less than one disagreement somebody sat down and explained.

So shadow mode's product is a **disagreement log**, not an agreement rate — a percentage would invite a
threshold, and a threshold is what [D-33](../DECISIONS.md) already refused. And an agent that never disagrees
is a **finding to investigate**, not a pass: it is either not running, not seeing the same inputs, or a
copy of the thing it shadows.

---

### 🟠 Q-38 — What is the exact install command? *(new)*

[07 §1a](../07-installation-and-update.md) settles the *shape* of installation: one documented command that
fetches a **signed release**, never *"paste this URL and let the AI run what it finds"* — which is a
supply-chain attack pattern and the thing a contractor's IT department is trained to refuse.

What is not settled is the command itself:

| Option | Notes |
|---|---|
| **Claude Code plugin install** *(preferred)* | Matches [D-01](../DECISIONS.md). Needs verifying against current plugin documentation — an install command that does not work is worse than none |
| **A release script** | `irm <release-url> \| iex` style. Works today, but is closer to the pattern being avoided and needs signing to be defensible |
| **Manual** | Download the release, run `deploy-addin.ps1`. Always available as the fallback, and what a cautious IT department will prefer |

Blocks nothing now — it is needed before the repository goes public ([D-10](../DECISIONS.md)), because the
README's first command is the first impression.

**Answer: deferred to publication — and the question's own warning is why. See [D-42](../DECISIONS.md).**

The preferred option *"needs verifying against current plugin documentation"*, and *"an install command
that does not work is worse than none."* That documentation has not been read here, and writing one from
memory is exactly the failure this repository has had twice — most recently the same day, when a Revit API
property that reads like the obvious choice turned out not to exist before 2024. **A wrong install command
fails on a stranger's machine, at the first thing they ever try.**

**What is settled because it is proven:** `tools\setup.ps1` — one command, detects every installed Revit,
builds for each, deploys per-user with no admin rights, run end to end in Phase 0. That is the fallback
this question calls *always available*, and the route a cautious IT department prefers.

This sits on the **publication** checklist beside reading the App Store requirements
([D-38](../DECISIONS.md)) — both are *read the current documentation* tasks, deferred for the same reason and
cheap at that moment.

---

### 🟡 Q-37 — Can pyRevit Routes bind a per-process port? *(new, from research)*

pyRevit ships an HTTP **Routes server**, and a shipping Revit MCP server uses it as its entire bridge --
no custom add-in needed. Attractive: proven, maintained elsewhere, and the owner already uses pyRevit.

**But it listens on a fixed `localhost:48884`, which is single-instance by construction** -- exactly the
failure the owner's field notes describe and already fixed: *"every Revit tried to use one shared line
and the second one simply refused to start."*

**The question:** can pyRevit Routes bind a **configurable port per Revit process**, and can that port be
discovered?

| Answer | Consequence |
|---|---|
| **Yes** | pyRevit Routes becomes a viable transport, potentially replacing the custom bridge |
| **No** | It stays a **scripting-execution** option only (Q-7a). [D-02](../DECISIONS.md) named pipes remain the transport -- multi-Revit is not negotiable |

-> [26](../26-prior-art-revit-mcp.md)

**Answer: closed as not applicable — Heron does not use Routes, on either count.**
[D-28](../DECISIONS.md) settled Q-7a as **Roslyn C# in process**, which removes the scripting-execution use
the "No" row kept it alive for. And the transport question is settled harder than "No": Routes is an
**HTTP server**, while Heron's add-in contains **no network code at all** — verified against the source,
not assumed. Adopting it would trade a structural guarantee for a configuration promise.

**The port question therefore never needs answering.** Both rows of the table above lead to the same
place, which is the sign that the question had already been overtaken.

**Kept rather than deleted** because the reasoning is the useful part: a fixed `localhost:48884` is
single-instance by construction, and multi-Revit is not negotiable ([D-02](../DECISIONS.md)). That argument
will come back the next time something proposes an HTTP transport.

---

### 🟠 Q-34 — Confirm the unified trust model? *(new)*

Four documents now define **six overlapping status vocabularies** for how much Heron trusts something.
Retrieval ranks by trust and promotion gates are defined per-vocabulary, so this must be settled before
anything is built.

*Proposal:* two orthogonal axes — **Lifecycle** (`DISCOVERED` → … → `ARCHIVED`, one vocabulary for
fragments, skills, capabilities and agents) and **Source** (`OFFICIAL` · `COMPANY` · `PROJECT` · `USER` ·
`COMMUNITY` · `IMPORTED` · `UNKNOWN`). Part 4 §47's four knowledge levels are kept as a derived band
used for ranking and for gating `MODIFY` operations.

→ [24 — The Unified Trust Model](../24-trust-model.md) · decision **D-14**

**Answer: the direction is agreed; the confirmation is deferred to the PC. [D-14](../DECISIONS.md) stays
PROPOSED until then.** Ajmal, 2026-08-28: *"yes, but show me it working at the PC first."*

**Enough to build on, not enough to close.** The two-axis shape is settled well enough that Phase 2's
storage and ranking can be designed against it; what is outstanding is him seeing it on a screen with his
own fragments in it before it becomes binding.

**It took two attempts to explain, and that is recorded because it matters.** The first explanation was
abstract — *"lifecycle and source axes"* — and he said plainly that he did not follow it. The second used
a Revit family: you want to know **who made it** (our office, the manufacturer, downloaded) and **whether
it has been checked** (approved, still being tested), and nobody would put those on one dropdown. He
agreed immediately.

That is the argument for the proposal in one sentence, and it belongs in
[24](../24-trust-model.md) rather than in this file: **a vocabulary that answers two questions at once will
keep splitting**, and it split six times here. Added to `R1` in
[`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md).

---

### 🟠 Q-35 — Confirm the Heron Constitution? *(new)*

Requested in [Part 4 §46](../00d-additional-requirements.md). Written as
[HERON_CONSTITUTION.md](../../HERON_CONSTITUTION.md) — **30 Articles** across knowledge, the user's model,
boundaries, self-modification and conduct.

Reconciled with the Golden Rules rather than duplicating them: Golden Rules are design principles for
people; the Constitution is the runtime-enforceable subset written as prohibitions an agent can obey or
violate.

Worth reviewing specifically:

- Article 9 — *"show before you change"* — is a preview mandatory for **every** non-`PRODUCTION`
  `MODIFY`, or only above a size threshold?
- Article 23 — *"for any `MODIFY`, an answer with no evidence is refused, not downgraded"* — is refusing
  the right default, or too strict for early versions?
- Are 30 Articles too many to inject usefully? *(Mitigated by giving each agent only the Articles
  relevant to its permission level and department.)*

**Answer:** ACCEPTED, 2026-08-28 — all 30 Articles, binding. See [D-43](../DECISIONS.md).**

Offered acceptance, deferral, or having all 30 read out, **Ajmal asked for all 30 to be read**, and
accepted them after reading. That is the difference between a confirmation and a tap, and it is why the
option was offered: a 30-article document accepted by pressing a button is not accepted, it is unread.

**Reading it aloud found three stale statements in it**, none of which changed what an Article requires
and all of which would have been read as current by whoever implements enforcement: it described its basis
as *"Golden Rules 1–15 (official) and 16–19 (proposed)"* — wrong twice, since all **21** became official
earlier the same day; **eight Articles cited a "Proposed" Golden Rule** that was no longer proposed; and
it said an agent receives *"not all 27"* Articles when there are **30**. All corrected on acceptance.

**Accepting the Articles does not make them true.** 8, 9, 11, 12a, 12b and 12c are precisely what groups
`C`, `D` and `E` of [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) exist to test.

---


### 🟡 Q-31 — What stores the dependency graph?

[Part 2 §41](../00b-master-specification-agent-os.md) requires a graph over skills, fragments, Revit API
surfaces, runtimes, packages, capabilities and agents — so the blast radius of a change is computable.

*Recommendation:* **SQLite with recursive queries**, beside the knowledge store ([Q-10](../OPEN-QUESTIONS.md#)). This is
ordinary relational data; a dedicated graph database is not warranted. Both specifications mention a
"Knowledge Graph", but §41 is the only place one is actually specified — build exactly this and not more.

→ [21 §1](../21-resilience-and-operations.md)

**Answer: SQLite as recommended — and an edge is DERIVED before it is stored. See
[D-40](../DECISIONS.md).** Same engine as [D-23](../DECISIONS.md), ordinary relational data, and §41's graph
only rather than a general knowledge graph.

The second rule came out of today's work and matters more than the storage choice.
[`check-api-surface.py`](../../tools/check-api-surface.py) answers *which Revit API members does Heron depend
on* by reading the compiled assembly, so the answer **cannot go stale**. A hand-maintained table of the
same facts drifts the first time somebody changes code without updating it — and a stale dependency graph
is worse than none, because blast radius is exactly what people trust it for.

So: store an edge only when it cannot be computed from an artifact on demand. What can be read is read.

---

### 🟡 Q-32 — How far does multi-user / Admin Mode go?

[Part 2 §72–§73](../00b-master-specification-agent-os.md) describe enterprise user management and enforced
policy. Heron is a single-user Claude Code plugin — there is no server to enforce anything.

| Option | Shape |
|---|---|
| **A. Single-user only** *(recommended now)* | Scopes are folders. "Company knowledge" is a shared repo each user syncs. No enforcement |
| **B. Company knowledge as a private git repo** *(recommended next)* | Admin = whoever reviews the pull requests. Uses machinery already specified in §38 |
| **C. Full enterprise server** | Central service, user directory, enforced policy. A different product — only on real demand |

Most of what a BIM manager actually wants — *"everyone uses our approved standards and tools"* — is
delivered by B without any infrastructure.

→ [22 §4](../22-users-modes-and-extensibility.md)

**Answer: A now, B when there is demand, C not without it. See [D-41](../DECISIONS.md).**

The deciding fact is in the question: **there is no server to enforce anything.** User management in a
product with no enforcement point is a settings screen describing a policy nothing can apply.

**B costs almost nothing new**, which is the real finding: a scope is already its own file in a folder
([D-23](../DECISIONS.md)), so making that folder a git repository adds no Heron code at all. And the admin
mechanism already exists — it is [D-35](../DECISIONS.md) at a smaller radius. A company approving fragments
for its staff and a maintainer approving them for everyone are the **same gate, the same approval record,
and the same refusal when it is missing.** One mechanism, two uses.

**Nothing is enforced and the documentation must not imply otherwise.** A reviewed shared repository
delivers *everyone uses our approved standards*; nothing delivers stopping somebody who does not want to.

---

### 🟡 Q-33 — Confidence thresholds for asking the user

[Part 2 §22](../00b-master-specification-agent-os.md) says knowledge conflicts fall back to asking the
user "if confidence is insufficient". [§55](../00b-master-specification-agent-os.md) says to ask only at
meaningful boundaries.

Both are right, and both need a number. What confidence level triggers a question? And are answers
**recorded as decisions** so the same question is not asked again next week?

*(Recommendation: yes — an unanswered-then-re-asked question is worse than a guess.)*

→ [20 §4](../20-knowledge-trust-and-conflict.md), [21 §3](../21-resilience-and-operations.md)

**Answer: there is no threshold — Heron never assumes an input, and it asks once. See
[D-33](../DECISIONS.md).** Ajmal, asked plainly: **"always ask before assuming anything."**

**That dissolves the first half of the question rather than answering it**, which is the better outcome. A
confidence figure is invented, unjustifiable, and free for any later session to tune — and the first tune
to reduce interruptions starts it guessing about exactly what it was set up to protect. *Never assume* is
a rule; it needs no number and cannot drift.

The second half is answered **yes, and it is load-bearing**: *always ask* without memory becomes noise,
and noise is clicked through unread — worse than not asking. Asking once is what keeps the rule usable.

One boundary is drawn in D-33 that his answer did not mention — a **technical** choice is Heron's own, not
an assumption — and it is flagged for the `R1` read-back rather than treated as settled.

---

### 🟠 Q-40 — Do replies need identifiers redacted before they reach the host? *(new, 2026-08-28)*

[D-26](../DECISIONS.md) settled that **Heron** never sends project content anywhere. It cannot settle what
the **host** sees, because Heron's own answers are the conversation.

Heron names the document on purpose — *"Found 126 ducts in Tower-A.rvt"* — a Phase 0 feature that builds
the habit Golden Rule 20 later enforces. Under a strict reading of D-26 that filename is project content
travelling to a model provider.

So: does a project marked confidential need a mode where the document is called something neutral in
replies, and identifiers are replaced before Heron says them?

**This is a contract question, not a technical one.** It depends on what the NDAs actually forbid, which
nobody here has read. The cost is real on both sides: redaction is work to build, and it makes every
answer harder for the person reading it to trust — *"which model was that again?"* is exactly the
confusion Phase 0 added the document name to prevent.

→ [12 §4](../12-security-and-permissions.md), [D-26](../DECISIONS.md)

**Answer: no redaction. Work answers may travel; the model may not.** Ajmal, 2026-08-28, asked directly:
*"how many ducts are there? That is no issue ... the work-related everything in the cloud, no issue. But
the entire model, it should not go to the cloud like that."*

So the concern this question was raised about turns out not to be the document's name or a room name in a
reply — it is **bulk**. A model, or a dump amounting to one. Heron keeps naming the document in its
answers, which is what Golden Rule 20 wants anyway, and no redaction layer is built.

**What replaces it is a rule about tools rather than about wording:** a tool answers a question and never
returns the model — see [D-26](../DECISIONS.md). That is cheaper to build than redaction, easier to explain
to a client, and it happens to be the same thing [Q-5](answered.md#q-5--mcp-tool-granularity--thick-and-specific)
already asked for.

**Revisit if that ever stops being true** — the first tool that wants to return thousands of rows is the
moment to re-read this, not the moment to quietly make an exception.

---

### ✅ Q-57 — May a fragment or skill that WRITES declare a question? → **Only when answering it REQUIRES the write** *(asked 2026-09-19, answered 2026-09-21)*

[FRAGMENT-ISSUES row 158](../FRAGMENT-ISSUES.md) found **ten fragments and five skills above the write
line declaring a sentence that asks and stops**, in their own `utterances:` blocks. The starkest is
`SET_VIEW_SCALE`, a MODIFY, declaring *"what scale is this view"* — so that question resolves to a
write by `identity`, which short-circuits before ranking runs. **No re-ranking repairs one.**

**Nothing in the repository forbids it.** `grep -iE 'utterance' docs/14-golden-rules.md` returns
nothing; no decision covers it either. The rules that come closest point in opposite directions:
[row 113](../FRAGMENT-ISSUES.md)'s forbidden move is **weakening a declaration to buy a rank**, and its
mirror — deleting a sentence a modeller really says so a sweep comes back clean — would be the same
error. Golden Rule 19 puts the real gate on the OPERATION's risk by name, so none of these can change
a model while the padlock is closed ([row 137](../FRAGMENT-ISSUES.md) traced that end to end).

**And some of the fifteen are probably right, which is why this is a question rather than a defect
list.** *"How many sprinklers do I need"* on `sprinkler-layout` may genuinely belong there — answering
it **is** the layout — unlike *"how many sprinklers on level 2"*, which is a count and was given to
`count-elements`. *"How far apart are these on the drawing"* on `DIMENSION_FAMILY_INSTANCES` says *on
the drawing*, which is arguably an instruction to annotate. And `REPLACE_MATERIAL`'s *"why will this
material not purge"* was moved there **deliberately**, with the reasoning in a comment above it.

So the question is not *are these wrong* but **what is the rule**:

- a blanket *a write never declares a question* — simple, and it would delete sentences that belong;
- *a write may declare a question only when answering it REQUIRES the write* — true of the layout
  skills, false of `SET_VIEW_SCALE`, and it needs a judgement per sentence;
- *no rule* — the padlock is the gate, and a wrong offer is not a wrong change.

**The third is the current state by default, and it is the one nobody chose.** Choosing it explicitly
would at least make `tools/check-declared-questions.py` a report that a reader can close rather than
one that stays open forever.

**Answer: the second one — a write may declare a question ONLY when the question cannot
be answered without performing the write. See [D-86](../DECISIONS.md).** Chosen 2026-09-21, over the
blanket ban and over leaving it unruled. `sprinkler-layout` keeps *"how many sprinklers do I need"*,
because counting them **is** the layout; `SET_VIEW_SCALE` loses *"what scale is this view"*, because
a scale can be read without touching it.

**The rule names the test and does not sort the fifteen** — each still needs a person to
apply it, and that cost was accepted with the answer. **One consequence is sharp enough to record
here and not only in the decision:** a sentence that LEAVES a write needs somewhere to go first.
[Row 146](../FRAGMENT-ISSUES.md) measured a view's scale as the library's one CERTAIN capability gap
— nothing reads it — so removing that sentence before something can answer it drops it
into ranking, where it may find a different writer. **Build the READ, then move the sentence.**

→ [FRAGMENT-ISSUES row 158](../FRAGMENT-ISSUES.md), [row 137](../FRAGMENT-ISSUES.md),
[row 113](../FRAGMENT-ISSUES.md), [14 — Golden Rules](../14-golden-rules.md)

---
