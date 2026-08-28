# Open Questions

> Everything that must be decided before Heron AI can be built, gathered from reviewing the
> [Master Specification](00-master-specification.md).
> Answer inline under each question. Once answered, the answer is promoted into [DECISIONS.md](DECISIONS.md).
>
> **Priority:** 🔴 blocks all work · 🟠 blocks a major area · 🟡 needed soon · 🔵 can wait

**Progress: 40 answered · 1 open · nothing blocking any phase**

**This line is checked, not trusted.** `python tools/check-docs.py` derives both numbers from the
questions themselves and fails if they disagree with this sentence. It said *14 answered · 26 open* until
2026-08-28, when the real figures were 20 and 20 — six questions had been answered and the sentence stayed
still, which is the exact failure the tooling in [`tools/`](../tools/README.md) exists to prevent.

**Nothing gates Phase 2 any more.** The last four — `Q-7a`, `Q-8`, `Q-9`, `Q-13` — were answered on
2026-08-28 as [D-28](DECISIONS.md) to [D-31](DECISIONS.md), and answering `Q-7a` closed `Q-37` with it.

Three of the four were settled by looking rather than deciding: at a system already doing the job
(`Q-7a`, `Q-8`, `Q-9`) and at Heron's own code, where `Q-13`'s answer had been running since Step 1.
**Ask what already exists before designing** — it worked four times out of five today.

`Q-20` closed the same day and it reshaped the plan rather than confirming it: **v1 is read-only**
([D-32](DECISIONS.md)). Asked which job he wanted first, Ajmal chose *answering questions about the
model*, and *reading first, writing soon after*. The write groups of the register now gate **v1.1**
instead of standing between him and something usable.

**Only `Q-35` remains open** — read and confirm the Constitution's 30 Articles. It belongs with the `R1`
read-back at the PC, because confirming a 30-article document by tapping an option is not confirming it.

**`Q-34` counts as answered here but is not closed.** Ajmal agreed the direction on 2026-08-28 and asked
to see it working at the PC first, so [D-14](DECISIONS.md) stays **Proposed** and the confirmation is
`R1b` in [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md). The count above cannot express *agreed but not
signed off*; this sentence is where that lives.

Everything else was answered on 2026-08-28. The last four — `Q-29`, `Q-31`, `Q-32`, `Q-38` — are
[D-39](DECISIONS.md) to [D-42](DECISIONS.md); `Q-38` is answered *as far as it honestly can be*, with the
public install command deferred to publication because the question's own warning says an install command
that does not work is worse than none.

**Q-12 and Q-40 were both answered on 2026-08-28.** Local only, every project
([D-26](DECISIONS.md)) — and the line falls at **the model, not the answer**: a count or a size list is
the job and may travel; the model, or a dump amounting to one, never does. That turned Q-40 from a
question about redacting words into a rule about tools, which is cheaper to build and easier to explain
to a client.

*(Five new questions — Q-29 to Q-33 — come from [Master Specification Part 2](00b-master-specification-agent-os.md).
None of them block Phase 0 either; they shape Phases 2–5.)*

---

## Tier 1 — Blocking

> ✅ **All clear.** Every question that blocked Phase 0 has been answered — see
> [D-01](DECISIONS.md) through [D-10](DECISIONS.md).
>
> **Phase 0 is unblocked.** It starts on the owner's go-ahead ([D-00](DECISIONS.md)).

The one sub-decision that was open inside [D-04](DECISIONS.md) closed on 2026-08-28:

### 🟠 Q-7a — Which scripting runtime for the sandbox?

[D-04](DECISIONS.md) settled *hybrid* — scripting while a fragment is in DRAFT/TESTING, compiled C# for
PRODUCTION. Which scripting runtime is still open: **pyRevit**, **IronPython**, **Python.NET**, or
**Roslyn scripting** (C# without compiling to an assembly).

The owner's existing `PyRevit-Tools` work is the strongest available evidence and should be reviewed
before choosing. Decidable during Phase 0 rather than before it, since Phase 0 generates no code.

**Research favours pyRevit.** A shipping Revit MCP server executes IronPython inside Revit via pyRevit's
built-in Routes server -- proven, maintained by someone else, and the owner already knows it
([26](26-prior-art-revit-mcp.md)).

→ [09 §10](09-skills-and-fragments.md)

**Answer: Roslyn C# scripting, in process, through the existing bridge — not pyRevit. See
[D-28](DECISIONS.md).** The research note above favoured pyRevit; reading a system already doing this job
daily points the other way for three reasons the research could not show. What Ajmal actually runs today
is **C#, not Python**. pyRevit Routes is an **HTTP server**, and Heron's add-in has *no network code at
all* — verified against the source — so adopting it would trade [D-02](DECISIONS.md)'s structural
local-only guarantee for a configuration promise. And one language means **one compile gate**: C#
fragments go through `check-compile.py` and `check-api-surface.py`; Python fragments would go through
neither.

**This closes [Q-37](#-q-37--can-pyrevit-routes-bind-a-per-process-port-new-from-research) as not
applicable** — Heron does not use Routes.

---

## Tier 2 — Blocks a major area

### 🟠 Q-8 — Confirm the Skill vs Fragment definition

Proposed: **Skill** = what the user can ask for (BIM language, user-facing).
**Fragment** = how it is done (technical, internal, reused across many skills).

→ [09 §1](09-skills-and-fragments.md)

**Answer: confirmed, with the part that decides the design added — a fragment is a *composable piece*, not
a whole how. See [D-29](DECISIONS.md).**

In a library of several hundred working fragments the unit is smaller than a job: a **filter** answers
*which elements*, an **action** answers *what to do to them*, and they are joined, each declaring what it
needs in scope and what it leaves. A job that genuinely cannot be composed is a **recipe** — a named third
kind, so it cannot quietly become a giant fragment.

That matters because reading *"fragment = how it is done"* as one fragment per job grows the library one
entry per sentence a user might say, reuses nothing, and needs a separate proof for every entry.

---

### 🟠 Q-9 — What promotes a fragment to PRODUCTION?

How many successful executions (suggest **N = 10**)? Who approves the final gate — always you, or can a
company BIM lead approve for their team? Now that the platform is open source, does a maintainer approve
community fragments?

→ [09 §5](09-skills-and-fragments.md)

**Answer: not a count at all — one recorded proof, and it must include a negative case. See
[D-30](DECISIONS.md).**

A working library shows why a count is the wrong gate, with a real defect: a fragment whose level filter
matched **zero** elements **and reported success**. That passes ten runs, and a thousand. A count measures
that nothing threw, which is not the property anyone cares about — what caught it was a comparison, 3
against 0, side by side.

So the gate is one dated proof against a real model carrying a **positive** case, a **negative** case
(*it returns nothing when it should*), and a **second route** to the answer where one exists.

**Who approves:** whoever ran it, under their name and date. A company's BIM lead may approve for that
company's scope, because the proof travels with the fragment as evidence a later reader can judge rather
than trust. A community submission meets the same bar; one without a negative case is returned, not
reviewed.

---

### 🟠 Q-10 — Which vector store?

*Recommendation:* SQLite + `sqlite-vec` + FTS5 — one file per knowledge scope, zero install, and all
three retrieval stages in one engine.

→ [05 §5](05-heron-brain.md)

**Answer: the recommendation, taken — see [D-23](DECISIONS.md).** Decided on the installation
constraint rather than on retrieval quality: Heron installs per-user with no administrator rights, and a
store needing a service breaks exactly the locked-down machines it is built for. One file per scope also
makes Golden Rule 5's scope separation a fact of the filesystem instead of a `WHERE` clause somebody can
forget.

---

### 🟠 Q-11 — Local or cloud embeddings?

*Recommendation:* local by default — free re-indexing, works offline, and no project content leaves the
machine. Cloud as opt-in.

→ [05 §6](05-heron-brain.md)

**Answer: local by default, cloud opt-in per scope — see [D-24](DECISIONS.md).** The deciding argument is
re-indexing: a per-call cost makes rebuilding the index something to avoid, and an index nobody rebuilds
quietly stops matching what is on disk.

**This does not answer [Q-12](#-q-12--what-is-the-data-confidentiality-position) and must not be read as
answering it.** Asked directly on 2026-08-28, Ajmal's reply — *"now we are in Claude, am I right, so make
it in this; when we are on the PC I will pull that there and we will test everything"* — was about where
the **work** happens, not about what **project content** may leave a machine. That is a contractual
question about client and authority work, it is his to answer, and it stays open. Local-by-default is the
setting that is safe to hold while it is open.

---

### 🟠 Q-12 — What is the data confidentiality position?

What may be sent to a model provider, from which projects? Is a fully local/offline mode a requirement
or a nice-to-have?

Sharper now that Heron is a public product: **other companies** will run it on **their** clients' models,
under NDAs you have never seen. The default must be safe for the most restricted user, not the least.

*Recommendation:* hybrid, enforced structurally — a project marked confidential is *incapable* of egress.

→ [12 §4](12-security-and-permissions.md)

**Answer: the model FILE is never uploaded; everything else about the work is fine. See
[D-26](DECISIONS.md).**

**It took three passes in one day to land there, and the final one is the rule.** The first answer was the
strictest position in the table above — *nothing leaves* — and two clarifications narrowed it. Ajmal,
finally and plainly: *"Any project name, data, typing, or content being in the cloud is not an issue ...
The main thing is that we should not upload the model itself, specifically the RVT or RFA files ... Do not
push the models."*

So the line is **the file, not the information**. A `.rvt`, a `.rfa`, a family or project template — never.
Project names, element counts, sizes, room names, engineering reasoning, code — that is the work, and it
travels like any other conversation with an assistant.

Third part of the same instruction: **project knowledge stays segregated.** Already the design — Golden
Rule 5's *one store per scope*, made literal by [D-23](DECISIONS.md) — and Rule 5's wording has been
broadened to name the project scope it always covered.

**The earlier framings are recorded rather than erased**, in D-26, because commits from the same day quote
them and a reader has to know which version won.

---

### 🟠 Q-13 — Where do product, data and derived files live?

Now critical: the repository is public, so **client data must be physically incapable of reaching it**.

*Recommendation:* product under the install location, data under the user profile, derived under a cache
location — and the updater physically unable to write to the data class.

→ [06 §2](06-heron-platform.md), [17 §2](17-open-source-and-distribution.md)

**Answer: the recommendation, and it is already built. See [D-31](DECISIONS.md).**

`HeronPaths` has drawn these three classes since Step 1 and is the only place allowed to construct a Heron
path — `check-structure.py` fails anything else that tries. PRODUCT is replaced wholesale on update; DATA
is `%APPDATA%\Heron` and roams, so a preference follows the person; DERIVED is `%LOCALAPPDATA%\Heron` and
deliberately does **not** roam, because a bridge file announcing process 24156 on another PC is
meaningless.

The updater half was **verified rather than assumed**: `deploy-addin.ps1` writes only into the Revit
add-ins folder, never into `%APPDATA%\Heron`. So the public-repository worry is answered structurally —
client data cannot reach the repository because it is never written inside it.

**A question answered by code that already existed.** Worth asking of the other open questions before
designing anything for them.

---

### 🟠 Q-15 — Is persona automatic, manual, or both?

*Recommendation:* infer a default, display it, let the user pin it. Silent mode-switching is a common
source of distrust.

→ [01 §4](01-vision-and-principles.md)

**Answer: neither — the question had the wrong axis in it. See [D-27](DECISIONS.md).**

Two assistants doing this job daily for months were read for this question, at Ajmal's suggestion. Neither
switches persona at all, and neither has needed to. **One voice — plain language, always.** What actually
varies is the **shape of the answer**, and it follows the **shape of the request**: a count gets a number,
a breakdown gets a schedule-style table, a narrowed set gets the items and their ids, finished work gets a
short close, and two comparable numbers get a picture unasked.

That dissolves the distrust the recommendation was trying to manage. Inferring a persona is guessing about
a person — wrong sometimes and invisible when wrong. Inferring an answer's shape is reading the request —
deterministic, and visible when it is wrong.

---

## Tier 3 — Needed soon

### 🟡 Q-16 — Which existing repositories are imported first?

`AJ-Tools`, `PyRevit-Tools`, `AEB-Tools` — which are in scope for the first knowledge import, and roughly
how many tools/fragments do they hold?

Note: some are private and may contain client-specific work. Anything imported must be reviewed before it
can reach a public repository.

→ [10 §5](10-memory-and-knowledge.md)

**Answer: all of them, as reference — and none of them is imported. See [D-25](DECISIONS.md).**
Ajmal, 2026-08-28: *"use them as a reference only ... In our Heron AI, it should be written completely
from scratch ... study each and every line, word by word, and create it as a new file."*

This answers a different question than the one asked, and the difference matters: there is **no import
pipeline to build**, and the duplicate detection an import would have needed largely goes with it. The
question of *which first* dissolves — reading is cheap and carries no risk; only re-authoring costs
anything, and that is decided one capability at a time.

The confidentiality note above is resolved by the same decision rather than by review: nothing is copied,
so no client-specific content can arrive by being carried across.

**Answer:**

---

### 🟡 Q-17 — Interface language

English only, or does Heron need to understand instructions in other languages used on site?

**Answer: Heron's own wording is English; understanding the user is not Heron's job at all. See
[D-34](DECISIONS.md).**

Two questions here, not one. Asked the first, Ajmal chose **English only for now** — and declined the
*"built ready for Arabic"* option, so no translation scaffolding is written either. The cost of adding it
later is a pass over every user-facing message, and that is the accepted price rather than a hidden one.

**The second half answers itself.** Heron never interprets language: that happens in the host before
Heron is called ([D-01](DECISIONS.md)). A request in Arabic, in mixed Arabic and English, or dictated
roughly, already works. So Heron builds no phrase list and no parser for near-misses — that would be a
worse copy of something the host already does, needing maintenance forever.

A **site word that means a Revit word** is a third thing and is not a language problem: it is knowledge,
it belongs in the knowledge store, and Phase 2 owns it. Meanwhile an unfamiliar term is a **question**,
never a quiet reinterpretation — [D-33](DECISIONS.md).

---

### 🟡 Q-18 — Can community packages contain executable code?

Installing a package means running third-party code inside Revit, inside the user's project. Signing,
source allowlist, version pinning — or declarative skills/fragments only, no executables?

Now a real security question rather than a hypothetical one, since anyone can publish.

→ [06 §10](06-heron-platform.md)

**Answer: yes, code is allowed — and an unapproved fragment is REFUSED, not warned about. See
[D-35](DECISIONS.md).** Ajmal chose *"yes, but only after review and approval"*.

Under [D-28](DECISIONS.md) a fragment is C# compiled and run inside Revit, so a shared fragment is
executable code by construction — this was never a hypothetical.

**The decision is written around the way that choice fails, not around the way it works.** A gate that
depends on somebody remembering to look decays: submissions outpace reading, a backlog forms, and
*approved* quietly comes to mean *nobody objected*. That is the option he rejected, reached by drift. So
there is **no warning dialog** — a warning hands the decision to the person least able to judge it and
most likely to click through. Unapproved does not run.

**Approval and proof are the same gate**, which is what makes the reviewer's job finite: the record
required is the one [D-30](DECISIONS.md) already demands, and a submission without a negative case is
returned rather than reviewed.

**One thing must be built now:** the fragment format carries an approval record from its first version.
Retrofitting provenance into a format already in use touches every file.

---

### 🟡 Q-20 — What is the v1 definition of done?

*Recommendation:* "select all ducts" and "move them 200 mm up" working end-to-end, on one Revit version,
with undo, audit log and a preview — and nothing else.

→ [ROADMAP.md](ROADMAP.md)

**Answer: v1 ships with both — it answers questions AND can change the model. See
[D-32](DECISIONS.md).** Asked which job he wanted first, Ajmal chose **answering questions about the
model**. Asked whether v1 must also change things, he answered **"it must change things too"**.

Those are not in conflict: **first-to-use and finished are different things.** Answering questions is the
daily work and the half already proven, so it is what gets used first — but a Heron that cannot change
anything is a report tool, not the product.

So the recommendation above stands after all, widened: v1 is select-and-move *plus* the questions. Writing
stays **off by default** ([D-19](DECISIONS.md)), which is about the setting a user turns on, not about
whether the capability ships.

**Recorded as read-only first and reversed within the hour** — see D-32, where the reversal is kept in
view rather than tidied away.

---

## From Master Specification Part 2

### 🟠 Q-29 — How does Shadow Mode work per agent type?

[Part 2 §10](00b-master-specification-agent-os.md) introduces Shadow Mode — an agent observes and
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

→ [18 §4](18-agent-operating-system.md)

**Answer: the table stands as proposed; promotion needs an analysed DISAGREEMENT plus a signature, not a
count of runs. See [D-39](DECISIONS.md).**

Not a count, for the same reason [D-30](#-q-9--what-promotes-a-fragment-to-production) is not one.
**Agreement is weak evidence**: two implementations are often wrong the same way, because the second was
written by somebody who read the first — and an agent that silently does nothing agrees with everything. A
hundred agreements prove less than one disagreement somebody sat down and explained.

So shadow mode's product is a **disagreement log**, not an agreement rate — a percentage would invite a
threshold, and a threshold is what [D-33](DECISIONS.md) already refused. And an agent that never disagrees
is a **finding to investigate**, not a pass: it is either not running, not seeing the same inputs, or a
copy of the thing it shadows.

---

### 🟠 Q-38 — What is the exact install command? *(new)*

[07 §1a](07-installation-and-update.md) settles the *shape* of installation: one documented command that
fetches a **signed release**, never *"paste this URL and let the AI run what it finds"* — which is a
supply-chain attack pattern and the thing a contractor's IT department is trained to refuse.

What is not settled is the command itself:

| Option | Notes |
|---|---|
| **Claude Code plugin install** *(preferred)* | Matches [D-01](DECISIONS.md). Needs verifying against current plugin documentation — an install command that does not work is worse than none |
| **A release script** | `irm <release-url> \| iex` style. Works today, but is closer to the pattern being avoided and needs signing to be defensible |
| **Manual** | Download the release, run `deploy-addin.ps1`. Always available as the fallback, and what a cautious IT department will prefer |

Blocks nothing now — it is needed before the repository goes public ([D-10](DECISIONS.md)), because the
README's first command is the first impression.

**Answer: deferred to publication — and the question's own warning is why. See [D-42](DECISIONS.md).**

The preferred option *"needs verifying against current plugin documentation"*, and *"an install command
that does not work is worse than none."* That documentation has not been read here, and writing one from
memory is exactly the failure this repository has had twice — most recently the same day, when a Revit API
property that reads like the obvious choice turned out not to exist before 2024. **A wrong install command
fails on a stranger's machine, at the first thing they ever try.**

**What is settled because it is proven:** `tools\setup.ps1` — one command, detects every installed Revit,
builds for each, deploys per-user with no admin rights, run end to end in Phase 0. That is the fallback
this question calls *always available*, and the route a cautious IT department prefers.

This sits on the **publication** checklist beside reading the App Store requirements
([D-38](DECISIONS.md)) — both are *read the current documentation* tasks, deferred for the same reason and
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
| **No** | It stays a **scripting-execution** option only (Q-7a). [D-02](DECISIONS.md) named pipes remain the transport -- multi-Revit is not negotiable |

-> [26](26-prior-art-revit-mcp.md)

**Answer: closed as not applicable — Heron does not use Routes, on either count.**
[D-28](DECISIONS.md) settled Q-7a as **Roslyn C# in process**, which removes the scripting-execution use
the "No" row kept it alive for. And the transport question is settled harder than "No": Routes is an
**HTTP server**, while Heron's add-in contains **no network code at all** — verified against the source,
not assumed. Adopting it would trade a structural guarantee for a configuration promise.

**The port question therefore never needs answering.** Both rows of the table above lead to the same
place, which is the sign that the question had already been overtaken.

**Kept rather than deleted** because the reasoning is the useful part: a fixed `localhost:48884` is
single-instance by construction, and multi-Revit is not negotiable ([D-02](DECISIONS.md)). That argument
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

→ [24 — The Unified Trust Model](24-trust-model.md) · decision **D-14**

**Answer: the direction is agreed; the confirmation is deferred to the PC. [D-14](DECISIONS.md) stays
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
[24](24-trust-model.md) rather than in this file: **a vocabulary that answers two questions at once will
keep splitting**, and it split six times here. Added to `R1` in
[`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md).

---

### 🟠 Q-35 — Confirm the Heron Constitution? *(new)*

Requested in [Part 4 §46](00d-additional-requirements.md). Written as
[HERON_CONSTITUTION.md](../HERON_CONSTITUTION.md) — **30 Articles** across knowledge, the user's model,
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

**Answer:**

---


### 🟡 Q-31 — What stores the dependency graph?

[Part 2 §41](00b-master-specification-agent-os.md) requires a graph over skills, fragments, Revit API
surfaces, runtimes, packages, capabilities and agents — so the blast radius of a change is computable.

*Recommendation:* **SQLite with recursive queries**, beside the knowledge store ([Q-10](#)). This is
ordinary relational data; a dedicated graph database is not warranted. Both specifications mention a
"Knowledge Graph", but §41 is the only place one is actually specified — build exactly this and not more.

→ [21 §1](21-resilience-and-operations.md)

**Answer: SQLite as recommended — and an edge is DERIVED before it is stored. See
[D-40](DECISIONS.md).** Same engine as [D-23](DECISIONS.md), ordinary relational data, and §41's graph
only rather than a general knowledge graph.

The second rule came out of today's work and matters more than the storage choice.
[`check-api-surface.py`](../tools/check-api-surface.py) answers *which Revit API members does Heron depend
on* by reading the compiled assembly, so the answer **cannot go stale**. A hand-maintained table of the
same facts drifts the first time somebody changes code without updating it — and a stale dependency graph
is worse than none, because blast radius is exactly what people trust it for.

So: store an edge only when it cannot be computed from an artifact on demand. What can be read is read.

---

### 🟡 Q-32 — How far does multi-user / Admin Mode go?

[Part 2 §72–§73](00b-master-specification-agent-os.md) describe enterprise user management and enforced
policy. Heron is a single-user Claude Code plugin — there is no server to enforce anything.

| Option | Shape |
|---|---|
| **A. Single-user only** *(recommended now)* | Scopes are folders. "Company knowledge" is a shared repo each user syncs. No enforcement |
| **B. Company knowledge as a private git repo** *(recommended next)* | Admin = whoever reviews the pull requests. Uses machinery already specified in §38 |
| **C. Full enterprise server** | Central service, user directory, enforced policy. A different product — only on real demand |

Most of what a BIM manager actually wants — *"everyone uses our approved standards and tools"* — is
delivered by B without any infrastructure.

→ [22 §4](22-users-modes-and-extensibility.md)

**Answer: A now, B when there is demand, C not without it. See [D-41](DECISIONS.md).**

The deciding fact is in the question: **there is no server to enforce anything.** User management in a
product with no enforcement point is a settings screen describing a policy nothing can apply.

**B costs almost nothing new**, which is the real finding: a scope is already its own file in a folder
([D-23](DECISIONS.md)), so making that folder a git repository adds no Heron code at all. And the admin
mechanism already exists — it is [D-35](DECISIONS.md) at a smaller radius. A company approving fragments
for its staff and a maintainer approving them for everyone are the **same gate, the same approval record,
and the same refusal when it is missing.** One mechanism, two uses.

**Nothing is enforced and the documentation must not imply otherwise.** A reviewed shared repository
delivers *everyone uses our approved standards*; nothing delivers stopping somebody who does not want to.

---

### 🟡 Q-33 — Confidence thresholds for asking the user

[Part 2 §22](00b-master-specification-agent-os.md) says knowledge conflicts fall back to asking the
user "if confidence is insufficient". [§55](00b-master-specification-agent-os.md) says to ask only at
meaningful boundaries.

Both are right, and both need a number. What confidence level triggers a question? And are answers
**recorded as decisions** so the same question is not asked again next week?

*(Recommendation: yes — an unanswered-then-re-asked question is worse than a guess.)*

→ [20 §4](20-knowledge-trust-and-conflict.md), [21 §3](21-resilience-and-operations.md)

**Answer: there is no threshold — Heron never assumes an input, and it asks once. See
[D-33](DECISIONS.md).** Ajmal, asked plainly: **"always ask before assuming anything."**

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

[D-26](DECISIONS.md) settled that **Heron** never sends project content anywhere. It cannot settle what
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

→ [12 §4](12-security-and-permissions.md), [D-26](DECISIONS.md)

**Answer: no redaction. Work answers may travel; the model may not.** Ajmal, 2026-08-28, asked directly:
*"how many ducts are there? That is no issue ... the work-related everything in the cloud, no issue. But
the entire model, it should not go to the cloud like that."*

So the concern this question was raised about turns out not to be the document's name or a room name in a
reply — it is **bulk**. A model, or a dump amounting to one. Heron keeps naming the document in its
answers, which is what Golden Rule 20 wants anyway, and no redaction layer is built.

**What replaces it is a rule about tools rather than about wording:** a tool answers a question and never
returns the model — see [D-26](DECISIONS.md). That is cheaper to build than redaction, easier to explain
to a client, and it happens to be the same thing [Q-5](#-q-5--mcp-tool-granularity--thick-and-specific)
already asked for.

**Revisit if that ever stops being true** — the first tool that wants to return thousands of rows is the
moment to re-read this, not the moment to quietly make an exception.

---

## Tier 4 — Strategic

### 🔵 Q-24 — Name and trademark

"Heron" is widely used in software. Worth checking before branding, packaging and an app-store listing exist.

**Answer: the name is Heron AI. It was CHOSEN, not cleared. See [D-37](DECISIONS.md).**

Ajmal kept the name and declined the offer to check for an existing product first. **So no trademark or
existing-product search has been done** — this question's own concern, that the name is widely used in
software, stands unexamined. Recorded plainly so that a later session reading *"Q-24 answered"* does not
conclude otherwise.

A legitimate choice for a free tool with no branding to defend. The **technical** window to rename stays
open until the repository goes public ([Q-28](#-q-28--when-does-the-repo-go-public--when-licence--safety-files-exist-and-there-is-working-code)):
today it is a mechanical change across 53 code files; afterwards it breaks installed add-ins and user
folder paths. If a check is ever wanted, before publication is the moment it is cheap — and the last
one.

---

### 🔵 Q-25 — Liability

If a Heron-generated change causes a defect in a delivered model, who is responsible? Now a public-product
question, not a personal one. Partly addressed by an explicit disclaimer ([17 §5](17-open-source-and-distribution.md))
and by the licence choice (Q-27).

**Answer: no warranty, the standard open-source position — and it is already in place twice. See
[D-36](DECISIONS.md).** Apache 2.0 carries it as licence; [`DISCLAIMER.md`](../DISCLAIMER.md) already says
it in plain words a modeller will read. Nothing new is written.

Two things are recorded with it. **`DISCLAIMER.md` is load-bearing**: it promises a preview, a single undo
entry, skipped owned elements and no unprompted sync to central — all of which are **unproven today**, so
it must move with the code rather than after it. And this records a choice, **not legal advice**: it holds
for Heron as it is now, free and open source. If Heron is ever sold or supplied as part of a paid service,
reopen it rather than assume it carries.

---

### 🔵 Q-26 — Autodesk App Store requirements

Confirmed as a later goal ([D-07](DECISIONS.md)). Their review constrains packaging, permissions and
installer behaviour — cheaper to read the requirements before the installer is finalised than after.

**Answer: GitHub now; the App Store door is kept open by not closing it, and nothing is built for it. See
[D-38](DECISIONS.md).**

**The requirements have NOT been read, and that is the honest half of this answer.** This question asked
for them to be read before the installer is finalised. Writing Autodesk's current packaging, signing and
review rules from memory would repeat the exact failure this repository has already had twice with the
Revit API — a confident answer nobody checked. **Read them from Autodesk, at the time, or not at all**; so
the reading is deferred along with the listing, and it is the **first** task if one is ever attempted, not
the last.

Four things are already true and may or may not help — stated as facts, not as compliance claims: per-user
install with no admin rights (proven), **no network code in the add-in** (verified against the source), a
standard `.addin` manifest, and Apache 2.0. [Q-38](#-q-38--what-is-the-exact-install-command-new) is the
live piece and belongs to the GitHub route.

---

## Answered

### ✅ Q-30 — Who routes models — Heron or Claude Code? — *closed by Part 4 §24*

Part 4 §24's **AI Model Abstraction Layer** resolves this by separating two things that were conflated:

```text
Heron AI Interface  ->  Model Router  ->  Provider Adapter  ->  Model
    (Heron: intent)          (pluggable: host when hosted, Heron for batch work)
```

Heron always declares *intent* ("this needs strong reasoning"); resolution to a specific model is
pluggable. Under [D-01](DECISIONS.md) Claude Code resolves conversational work; Heron's Python side
resolves its own batch work through the same interface. Neither half hard-codes a model id.

It also makes local/cloud routing (§25) a configuration choice rather than an architectural one — a
project marked confidential selects a local provider adapter, and nothing above that layer needs to know.

→ [23 §8](23-heron-kernel.md), [19 §3](19-context-and-cost.md)


### ✅ Q-2 — Transport between MCP server and add-in? → **Named pipes**

C# add-in is the pipe server, Python MCP server is the client, pipe name encodes Revit version + PID.
Local-only by construction. → [D-02](DECISIONS.md)

### ✅ Q-39 — Must the user install Python? → **Yes, and the installer says so**

**Node is not the free option it looked like.** Claude Code ships as a native binary, not an npm
package, so it does **not** require Node — checked on the development machine, where Claude Code is not
an npm global and Node is a hand-downloaded folder. Neither runtime is pre-installed on a fresh machine,
so "the user already has it" was simply false, and the install cost is the same either way.

That leaves one question that actually differs: **the brain needs Python.** RAG, embeddings and vector
search live in Python's ecosystem ([05](05-heron-brain.md)), which is what [D-06](DECISIONS.md) was
decided on. Choosing Node for the MCP server would not avoid Python — it would ship **two** runtimes
instead of one, and put a language boundary where no process boundary exists.

Both can be bundled into a single executable later, so bundling does not favour either.

→ **Python.** [D-06](DECISIONS.md) stands, and installation lists Python as a prerequisite rather than
discovering it on someone else's machine. Bundling stays open under [Q-38](#-q-38--what-is-the-exact-install-command-new).

**And it does not need administrator rights**, which was the real worry. Verified on the development
machine: its Python is a Microsoft Store build living in `AppData\Local`, and the `mcp` package sits in
a per-user site-packages folder. Nothing went near `Program Files` or the registry.

| Needs admin | |
|---|---|
| Python, per-user | **No.** `winget install Python.Python.3.12 --scope user`, or the Microsoft Store |
| The `mcp` package | **No.** `pip install --user mcp` |
| The Revit add-in | **No.** Per-user add-in folder, which is why [D-05](DECISIONS.md) chose it |
| Git | **Not needed at all** for a released install — only to build from source |
| .NET SDK | **Not needed at all** for a released install — only to build from source |

So the complete list for an ordinary user is **Claude Code, Revit, Python** — and none of it requires
IT approval. `tools/setup.ps1` now detects Python and the `mcp` package and prints the exact per-user
command when either is missing, rather than leaving someone to wonder why asking Claude a question does
nothing.

It **tells** rather than installs. Pulling a language runtime onto somebody's machine unasked is the
kind of thing a careful user and a corporate laptop are both right to refuse.

### ✅ Q-4 — `ExternalEvent` or `Idling`? → **`ExternalEvent`, one queue, one handler**

`Idling` used only for a liveness heartbeat. Heron must surface "Revit is busy" rather than hanging.
→ [D-09](DECISIONS.md)

### ✅ Q-36 — Lease or takeover when two chats target the same Revit? → **Lease (option B)**

Built in Step 6 as `HeronLease`. A second chat is **refused** with a message saying what is happening and
when it clears, rather than taking the session and chopping whatever the first was doing. Scoped to the
Revit **process**, not the document — the contention is at the pipe. `ping` and `info` are exempt, which
is what finally makes `(free)` / `(in use)` truthful in the picker and ends a person being used as a
lock. It cannot block a rollback: the lease is checked when a request arrives, and a rollback happens
inside a request already admitted.

**One thing the question got slightly wrong**, worth keeping: option B does *not* "remove the hazard
entirely". The pipe is displaced when a second chat CONNECTS, before the lease can be consulted, so the
first chat still loses one in-flight reply. The lease removes the **takeover**; it narrows the
**interruption**. → [D-22](DECISIONS.md), [25 §3](25-multi-session-and-binding.md)

### ✅ Q-19 — Accept proposed Golden Rules 16–21? → **Accepted, 2026-08-28**

All six are now official and binding, on the same footing as 1–15. Four of them — **16** (one user action,
one undo), **17** (no autonomous write without a preview), **20** (bind the document, not just the
session) and **21** (re-read before acting; a preview expires) — are exactly what Step 6 was built to
obey.

**Accepted while Step 6 is still unproven, and deliberately so.** A rule that only becomes binding once
the code passes is not a rule the code was ever held to. Settling the standard first is what makes the
checking that follows a test of the code rather than a negotiation about the standard.

18 (generated code never touches a live model on its first run) and 19 (no text Heron reads may raise
its own permission level) bind work that does not exist yet. That is the right time to accept them —
before there is any code with an interest in the answer. → [14](14-golden-rules.md)

### ✅ Q-14 — How is testing against real Revit done? → **A written register, in dependency order**

[`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — every unproven claim as a numbered item (`A1`, `D3`), in
dependency order, each stating what PASS actually looks like. Items are added whenever something is built
away from Revit, and deleted only when they have actually passed.

Three things make it work rather than being a to-do list:

- **Dependency order.** Nothing in group D can be attempted before group A compiles. Working down instead
  of around is what stops a "pass" that was never really tested.
- **What needs Revit is separated from what does not.** Group A needs Windows and the SDK only — and the
  round-trip test proves the bridge *and* most of the lease there, before Revit is ever opened.
- **One register, not one per document.** [HANDOVER](../HANDOVER.md) §6 points at it rather than keeping
  a copy, because two lists of the same thing drift.

**What it does not answer:** automated testing against a real Revit, in CI. That needs a machine with
Revit installed and is a Phase 2 question. This is the manual practice, written down — which is what was
actually being asked for.

### ✅ Q-5 — MCP tool granularity? → **Thick and specific**

Each tool maps onto a fragment and carries its own risk level. Generic `revit_execute` only in
Developer Persona behind `ADMIN`. Capability discovery keeps the context cost down. → [D-03](DECISIONS.md)

### ✅ Q-7 — How does generated code execute? → **Hybrid**

Scripting sandbox while DRAFT/TESTING, compiled signed C# for PRODUCTION. The `PROVEN → PRODUCTION`
gate is where compilation happens. Sub-question Q-7a (which scripting runtime) was answered on
2026-08-28 — **Roslyn C#, in process** ([D-28](DECISIONS.md)).
→ [D-04](DECISIONS.md)

### ✅ Q-27 — Which licence? → **Apache 2.0**

Chosen over MIT for its explicit patent grant and warranty disclaimer, which matter for software that
writes to live client models; over GPL because many construction firms forbid GPL internally.
→ [D-08](DECISIONS.md)

### ✅ Q-28 — When does the repo go public? → **When licence + safety files exist AND there is working code**

Safety files completed 2026-08-27. Remaining condition: Phase 0 working code. → [D-10](DECISIONS.md)

### ✅ Q-1 — Where does Heron run? → **Claude Code plugin**

Claude Code is the conversation layer and agent host. Heron supplies skills, subagents, an MCP server and
the Revit add-in. → [D-01](DECISIONS.md)

### ✅ Q-3 — Which Revit versions? → **2020 through latest, and every future release**

Accepts two API breaks (`ElementId` 64-bit at 2024, .NET 8 at 2025). Requires multi-targeting from one
source tree and an adapter layer from the first line of code. → [D-05](DECISIONS.md), [16](16-version-support-strategy.md)

### ✅ Q-6 — What language? → **C# for Revit, Python for the brain**

The language boundary sits exactly where the process boundary already had to be.
→ [D-06](DECISIONS.md)

### ✅ Q-21 — Commercial model? → **Free and open source**

Public GitHub, installable by anyone. Autodesk App Store later, also free. → [D-07](DECISIONS.md), [17](17-open-source-and-distribution.md)

### ✅ Q-22 — First users? → **Everyone**

Not personal tooling and not company-internal. The installer, health checks and persona system are
therefore real scope, and defaults must be safe for the most restricted user. → [D-07](DECISIONS.md)

### ✅ Q-23 — Relationship to the existing AJ-Tools family → **Upgrade and absorb**

The owner's earlier brain work is the reference for the brain layer, and his earlier connector work for the Revit bridge.
Their ideas are taken, upgraded and reshaped to the Heron architecture — after documentation is finalised,
on the owner's signal. `AJ-Tools` / `PyRevit-Tools` / `AEB-Tools` are candidates for the first knowledge
import (Q-16). → [D-06](DECISIONS.md)
