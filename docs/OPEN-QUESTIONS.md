# Open Questions

> | | |
> |---|---|
> | **Type** | **Permanent register.** Append-only, and **never deleted** — this is where work notes empty into |
> | **For** | Anyone about to guess at something the owner has not decided |
> | **Authority** | An answer here is **promoted into [DECISIONS](DECISIONS.md)** and that entry becomes the authority |
> | **Waiting on you?** | `python tools/owner-queue.py` — **never a list typed on this page** |
> | **Adding to it** | **A question must never live only in `work-notes/`** — that folder is deleted at the end of its life. On 2026-09-12 two questions were found doing exactly that while this file said one was open |
> | **Its numbers** | `python tools/check-docs.py` derives answered-vs-open and **fails** if the Progress line disagrees |

> **Read [FOR-THE-OWNER.md](FOR-THE-OWNER.md) first if you are the owner.** It is the one page that
> says what is waiting on you, across every register, without holding a list of its own.


> Everything that must be decided before Heron AI can be built, gathered from reviewing the
> [Master Specification](00-master-specification.md).
> Answer inline under each question. Once answered, the answer is promoted into [DECISIONS.md](DECISIONS.md).
>
> **Priority:** 🔴 blocks all work · 🟠 blocks a major area · 🟡 needed soon · 🔵 can wait

**Progress: 52 answered · 3 open · nothing blocking any phase**

**The count moved 1 → 3 on 2026-09-12 without anybody asking anything new.** `Q-54` and `Q-55` were
raised on 2026-09-10 and 2026-09-11 and had been living in a work note — `Q-D` and `Q-E` in
`docs/work-notes/plans/rag/03-working-note.md` — which is a file whose lifecycle ends in deletion. They
were carried here when it was retired. **Two questions were owed the whole time and this register did
not know**, which is the argument for a question never living anywhere but this file.

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

**`Q-41` is the forty-second question, and the first one the OWNER asked** — raised and answered on
2026-09-06, during the decision read-back rather than by any specification. He confirmed
[D-16](DECISIONS.md) as written and then asked for something it does not allow: a job done in one
project, repeated in another. Answered **both** readings, promoted as [D-47](DECISIONS.md), and it
blocks no phase. **The count moved 41→42 in one message**, which is worth noticing: this file's counts
have only ever grown by a document being adopted, and a question from him lands exactly the same way.

`Q-35` closed on 2026-08-28: Ajmal asked for all 30 Articles to be **read out**, and
accepted them after reading ([D-43](DECISIONS.md)). The Constitution is binding.

**One answer is agreed but not signed off**, and no count can show that — see `Q-34` below.

**`Q-34` counts as answered here but is not closed.** Ajmal agreed the direction on 2026-08-28 and asked
to see it working at the PC first, so [D-14](DECISIONS.md) stays **Proposed** and the confirmation is
`R1b` in [`NEEDS-CHECKING.md`](NEEDS-CHECKING.md). The count above cannot express *agreed but not
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

**This closes [Q-37](#q-37--can-pyrevit-routes-bind-a-per-process-port-new-from-research) as not
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

**This does not answer [Q-12](#q-12--what-is-the-data-confidentiality-position) and must not be read as
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

### 🟠 Q-41 — Can a job done in one project be repeated in another? *(new, 2026-09-06)*

**Raised by the owner during the D-16 read-back**, unprompted and in his own words: *"maybe from one
project refer same, like that need to do in another project."*

He confirmed [D-16](DECISIONS.md) as written and then asked for something D-16 does not allow. That is
worth stating plainly rather than filing as a feature: **every binding rule in Heron assumes one job, one
document.** [Golden Rule 20](14-golden-rules.md) pins the target document by identity and forbids
following the active window; [D-22](DECISIONS.md) refuses a second chat rather than letting it take over;
[D-16](DECISIONS.md) builds a picker precisely because a job belongs to exactly one Revit. A job that
starts in Tower A and lands in Podium crosses all three.

**Two readings, and they are different products:**

| Reading | What it means | Rough cost |
|---|---|---|
| **A — repeat the action** | *"Do to Podium what you just did to Tower A."* Heron remembers the operation, not the result, and re-runs it against a second document — re-resolving every element by its own identity, because element ids do not carry across models | Moderate. Needs a replayable record of a job, and a second binding |
| **B — copy the content** | *"Bring Tower A's view filters / line styles / parameters into Podium."* Standard Revit transfer-project-standards territory, and the library already has `COPY_VIEW_FILTERS`, `REMAP_LINE_STYLES` and `COPY_FROM_LINK` | Low. Mostly written already |

**Which one he means is not yet established** and the question stays open until he says. **B is nearly
free and A is a change to the binding model** — so guessing wrong is expensive in one direction and
wasteful in the other, which is exactly the case [D-33](DECISIONS.md) says to ask about rather than assume.

**Whichever it is, one thing does not move:** a write into a second document is still a write, so
[Golden Rule 17](14-golden-rules.md)'s preview and [Golden Rule 16](14-golden-rules.md)'s single undo
apply to the second model as much as the first — and an undo cannot span two documents, so a job that
touches two models cannot honestly be one undo. **That alone may decide the shape of the answer.**

**Answer: BOTH — 2026-09-06. See [D-47](DECISIONS.md).** Asked which of the two he meant, and told plainly
that one was nearly free and the other changed the binding model, the owner answered *"BOTH"*.

So neither is dropped, and the order is decided by cost rather than by preference: **B ships first**
because most of it exists, and **A follows** because it needs a replayable record of a job and a second
binding before it can be honest.

**And the undo problem is settled by Revit, not by us.** Revit keeps a separate undo stack per document,
so a job spanning two models **cannot** be one Ctrl+Z, and no design makes it one.
[Golden Rule 16](14-golden-rules.md) is therefore restated rather than broken: **one undo per document**,
and Heron must say so before it starts — *"this touches two models; undoing in Podium will not undo Tower
A."* Saying it afterwards would be the failure Rule 16 exists to prevent.

---

## Tier 3 — Needed soon

### ✅ Q-53 — What checks the licence of knowledge Heron imports, before Heron's users redistribute it? → **A gate that reads the files** *(asked and answered 2026-09-09)*

Found by reading [`K-Dense-AI/scientific-agent-skills`](https://github.com/K-Dense-AI/scientific-agent-skills)
at file level ([33 §5.9](33-external-repository-research.md)), and it is not hypothetical — it is a live
example of the failure.

**Its README says the project is MIT and that you may *"modify, distribute, and use freely."* Four of
its 163 skills carry `© 2025 Anthropic, PBC. All rights reserved.`** A fifth is MIT under a different
copyright holder. Nothing on the landing page says so; only listing the licence files does. **Their own
skill scanner checks security and never looks at a licence.**

**Heron is walking into the same position.** [17](17-open-source-and-distribution.md) publishes Heron
under Apache 2.0 ([D-08](DECISIONS.md)); [09](09-skills-and-fragments.md) plans **community packages**,
which [Golden Rule 19](14-golden-rules.md) already names as a source Heron reads. So Heron will import
knowledge somebody else wrote, and Heron's users will redistribute what Heron ships.

**At the time this was asked, none of Heron's tools mentioned a licence.** `check-metadata.py` checks headers,
`check-structure.py` checks boundaries, `check-docs.py` checks claims. Nothing checks what an imported
package permits.

**Three shapes:**

1. **A declared field.** A package manifest names its licence, and `check-metadata.py` refuses one that
   does not — the same shape as the header rule that already works. Cheapest, and it trusts the
   declaration.
2. **A gate with an allow-list.** Only licences compatible with Apache 2.0 may be imported, checked by
   a tool. Stronger, and it needs a list somebody maintains.
3. **Nothing, and say so.** Imports are the user's responsibility, stated plainly in
   [17](17-open-source-and-distribution.md) rather than left unsaid.

**What is not in doubt:** [Golden Rule 12](14-golden-rules.md) already stops Heron publishing private
knowledge automatically. Nothing yet stops Heron *carrying* somebody else's knowledge under a licence
its users cannot honour, and the difference between those two is the whole question.

**Answer: A GATE, AND ITS ONE RULE IS READ THE FILES — 2026-09-09. See
[D-66](DECISIONS.md).** [`tools/check-licence.py`](../tools/check-licence.py), exits 1 on a finding.

**The rule comes straight from the failure.** Nobody lied: the top-level claim and the file-level truth
were different, and only listing the files showed it. So the tool walks every file of every fragment and
skill, finds the markers **in the content**, and compares them with what the unit declared. It keeps
**unmarked** apart from **clean** — *"no evidence of a problem"* and *"evidence of no problem"* are
different findings, and merging them is the tool that project already has.

**Today: 370 units, all clean, exit 0.** Which on its own proves nothing — so
[`tests/test_licence_check.py`](../tests/test_licence_check.py) rebuilds the exact failure and checks
each marker is caught: a reservation of rights nested under a folder declaring `source: OFFICIAL`, an
incompatible name, a compatible one, and Heron's own copyright. **A licence checker that has only ever
seen a clean library is a plausible zero ([D-52](DECISIONS.md)) wearing a gate's clothes.**

**It also locks a false positive.** The first pattern matched `rule.GetCriterion(c) as
PrimarySizeCriterion` and reported that fragment as somebody else's work. `(c)` now counts only next to
a year — **a licence tool that cries wolf is a tool somebody turns off**, and this one would have been
turned off over a cast.


---

### ✅ Q-52 — Should the composition graph become a third retrieval stream? → **No. Measured.** *(asked and answered 2026-09-09)*

Found by reading [`rohitg00/agentmemory`](https://github.com/rohitg00/agentmemory) at file level
([33 §5.5](33-external-repository-research.md)). It fuses **three** streams — BM25, vector and a graph —
by weighted RRF at `RRF_K = 60`. [`heron_retrieve.py`](../brain/heron_retrieve.py) fuses **two**, at the
same constant, chosen independently. [`heron_graph.py`](../brain/heron_graph.py) exists and is not one
of them; its only production caller is [`check-gaps.py`](../tools/check-gaps.py).

**The obvious repair is probably wrong, and that is why this is a question.** agentmemory's graph stream
expands *entities found in the query* — a recall widener, closer to Heron's keyword route than to
anything else. Heron's graph answers a different question: `composes_into` / `composes_from`, derived
from the contracts, *A provides what B needs*.

**That is a composition graph, not an entity graph.** Fusing it into retrieval mixes *"which fragment
answers this request"* with *"which fragment goes next to that one"*, and a strong helper could outrank
the fragment that actually answers — `set-selection` beating the filter whose output it selects.

**Three shapes:**

1. **Leave it.** Retrieval answers one question and the graph answers another, and keeping them apart
   is why neither is confused today.
2. **A third fused stream**, weighted low, so composition settles near-ties and cannot overturn a
   clearly better match — which is exactly the reasoning already written into
   [`heron_retrieve.py`](../brain/heron_retrieve.py)'s status nudge.
3. **A second answer, not a fused one:** the best match is returned, and *what composes with it* is
   returned beside it, labelled. No ranking is touched and the modeller gets the next step named.

**This one can be measured rather than argued.** [`measure-routes.py`](../tools/measure-routes.py) gives
the before, and [D-39](DECISIONS.md) already requires a disagreement to be analysed rather than
counted.

**AND SOMEBODY HAS MEASURED IT — added 2026-09-09 from
[`garrytan/gbrain`](https://github.com/garrytan/gbrain)** ([33 §5.16](33-external-repository-research.md)),
which the owner named the same day. It reports **+31.4 points P@5 from the graph stream** over its own
graph-disabled variant and over vector-only RAG, on a 240-page corpus.

**It also changes which comparison this question should have been making.** The caution above is about
`agentmemory`'s graph, which widens recall by expanding *entities in the query*. **gbrain's edges are
derived from references already in the content, deterministically, with no model call** — which is
[D-40](DECISIONS.md) word for word, and what [`heron_graph.py`](../brain/heron_graph.py) already does
from the contracts. **The closer analogue is the one reporting the large lift.**

**What does not transfer is the number.** Their corpus is prose about people and companies, where an
edge is a cross-reference between documents. Heron's is fragments, where an edge means *this one's
output fits that one's input*. **Take the direction as evidence and the magnitude as nothing.** Shape 2
or 3 above is now the more likely answer, and it is still a measurement to run rather than a decision to
take on somebody else's corpus.

**Answer — measured on 2026-09-09 with [`tools/measure-graph.py`](../tools/measure-graph.py). It is
NO for shape 2, and the measurement does not touch shape 3.**

The answer key is each fragment's own `semantic-identity` — 360 questions whose right answer is known
because nobody wrote it to make retrieval look good. Four query shapes, three degraded on purpose,
because a graph can only help where the answer is **not** already first.

| shape | P@1 today | P@1 with the graph | |
|---|---|---|---|
| `exact` | **95.6%** | 94.4% | −1.1 |
| `no-first` | **93.1%** | 92.2% | −0.8 |
| `content` | **90.4%** | 89.0% | −1.4 |
| `half` | **70.8%** | 69.2% | −1.7 |

**Six settings were tried and all six lost** — weights 0.05, 0.10 and 0.30 against 1, 3 and 5 seeds.
The gentlest costs 1.1 points of P@1; the strongest costs 14. **P@5 never improved at any setting**, so
the graph did not even widen recall, which is the one thing it was supposed to be good at.

**And the reason is in the graph, not in the fusion:**

> neighbours per fragment: **median 50, worst 230**, and none at all for 68 of them.

**gbrain's graph is sparse and Heron's is dense.** A page mentions three people; a fragment providing
`IList<Element>` composes with every fragment that needs one — most of the library. So *"the neighbours
of the best hit"* is not a signal here, it is a large slice of the library added as competitors. **That
is what does not transfer, and it is a property of the corpus rather than of the idea.**

**Two honest limits on this number.** The vector route ran on the `lexical` backend, not the trained
one — which makes the baseline *weaker*, so a stronger baseline can only make the graph's job harder,
and the conclusion is robust in the direction that matters. And the answer key is easy: a real modeller
never types a fragment's declared sentence. **Both point the same way.** A re-run on the owner's PC now
costs about fifteen seconds.

**Shape 3 is untouched and still open as an idea** — returning *what composes with the best match*,
labelled, beside the answer rather than fused into the ranking. Nothing measured here bears on it,
because it changes no ranking. If it is ever wanted it is a separate question, not this one.

---

### 🟠 Q-51 — What guards the path from retrieval to context, on the day Heron indexes text it did not write? *(new, 2026-09-09)*

Found by reading [`ruvnet/ruflo`](https://github.com/ruvnet/ruflo) at file level
([33 §5.2](33-external-repository-research.md)). Its `agentdb-retrieval-guard.ts` scans every chunk
coming back from vector search **before** it is assembled into a prompt, and reports 93–100% undefended
attack success against poisoned memory entries.

**Heron already enforces [Golden Rule 19](14-golden-rules.md) where it counts, and that half is done.**
Risk comes from the operation registry inside the add-in, never from the request — no text arriving on
the pipe can raise Heron's permission level.

**The half not yet faced.** Rule 19 governs what text may *authorise*; it says nothing about the text
itself travelling upward. Heron makes no model calls ([D-01](DECISIONS.md), [D-58](DECISIONS.md)), so
Heron cannot be injected — **Heron is the carrier**. [`heron_context.py`](../brain/heron_context.py)
assembles parts, stamps each with `source`, and hands them to the host.

Today that is safe for a reason with an expiry date: **every source is Heron's own** — the request, the
fragment library, its tests, the API surface. The exception is already written into the part list:

```python
STANDARD = "standard"        # the clauses cited - source does not exist yet
```

**That source is exactly what the RAG work creates** — project standards, specifications, family
descriptions, imported packages. Retrieval becoming useful and Heron carrying text it did not write are
the same event, so the guard belongs in the design of the index rather than bolted on after it.

**Three shapes, and they are not equivalent:**

1. **Mark, do not scan.** Every part already carries `source`; a part from an indexed document is
   labelled untrusted and the host decides. Cheapest, honest, and it puts the judgement where
   [D-01](DECISIONS.md) already puts the model.
2. **Scan and flag.** Heron screens retrieved text and annotates what looks like an instruction. Needs a
   pattern library Heron would have to own and keep current.
3. **Scan and refuse.** Strict mode. A false positive silently drops the clause a modeller needed, which
   is the plausible-zero failure this repository legislates against.

**One detail worth keeping whatever is chosen**, because it is not obvious: ruflo **flags oversized
chunks rather than truncating them** — truncating lets an attacker pad a payload past the scanner's own
window.

**What is not in doubt:** a retrieved clause is data. The question is only whether Heron says so, and to
whom.

**NOT NOW — and there is now a tripwire that says when. Owner's call, 2026-09-09.**

**The guard stays unbuilt, deliberately.** Every part Heron carries today comes from a source Heron
wrote: the caller's own request, the fragment library, its test cases, the executor's import list. A
scanner written against no corpus is a scanner written against a guess — and the same day supplied the
evidence rather than the opinion, when [`Q-52`](OPEN-QUESTIONS.md)'s +31.4 from somebody else's corpus
lost at all six settings against Heron's own.

**What was built instead is [`tests/test_carried_sources.py`](../tests/test_carried_sources.py)** — a
tripwire, not a guard. It asserts that **every part, on every path, at every depth, has a source that is
one of Heron's own labels or a file inside this repository.** 2,160 parts checked; all of them Heron's.

**It fires the day that stops being true**, and its failure message says so in as many words: *this is
probably not a bug, it is Q-51 becoming live — decide the guard now, with the index rather than after
it.* It also watches the two places the change would show first: the `STANDARDS` path still refusing for
want of a clause store, and `heron_context.py` still carrying the countdown comment
`STANDARD = "standard"  # the clauses cited - source does not exist yet`.

**Checked against what it will actually meet**, rather than assumed: a QCS clause reference, an imported
community package, a specification PDF on the user's disk, and text read out of a Revit model are all
classified **foreign**; the request, the capability, a fragment and the import list are all classified
**Heron's**. Eight of eight. **A tripwire that cannot trip is decoration.**

**It reads no part's body and looks for no pattern in one.** It cannot tell a safe clause from a hostile
one and does not pretend to. It answers one question — *is Heron still only carrying its own words?* —
which is the question whose answer changing is what makes this one urgent.

**So this question stays open on purpose**, and it is no longer something anybody has to remember.

---

### 🔵 Q-54 — Is the cloud-embedding opt-in worth building, or is it a switch nobody will turn on? *(moved here 2026-09-12)*

**Carried in as `Q-D` from `docs/work-notes/plans/rag/03-working-note.md` §5 when that note was
retired.** It was the note's only home and a work note is deleted at the end of its life, so it comes
here rather than going with it.

[D-24](DECISIONS.md) already says **local by default, cloud opt-in per scope** — and
[Q-11](#q-11--local-or-cloud-embeddings) is where that was settled. **This is the sharper question
that survived it:** there is **no opt-in path and no per-scope setting**, so the choice D-24 describes
cannot currently be made at all. That is `R-30`.

Given [D-26](DECISIONS.md) allows project content in the cloud, the opt-in may be worth building — or
it may be a switch with no user.

*Cheapest honest answer if unsure:* leave it unbuilt and **record that decision**, rather than building
a switch nobody turns on. An unbuilt setting that is written down is cheaper than a built one that is
not used, and either is better than a specification promising a choice the code cannot offer.

---

### 🟡 Q-55 — May an INGEST read another scope? *(raised 2026-09-11 by building Stage 8; moved here 2026-09-12)*

**Carried in as `Q-E` from `docs/work-notes/plans/rag/03-working-note.md` §5 when that note was
retired.**

[docs/20 §4](20-knowledge-trust-and-conflict.md) asks for conflict detection **at write time** as well
as read time, with a good reason: resolving a conflict once, at ingest, is far cheaper than resolving
it on every query, and it stops the knowledge base accumulating contradictions in the first place.

**It cannot be built without answering a contractual question first.** At read time the crossing is
already authorised — a host asked one question of two named scopes, and `librarian()` opens each on its
own. **At write time nothing has been asked of anybody.** Finding a conflict at ingest means reading
ANOTHER scope while writing into this one, and [D-33](DECISIONS.md) makes cross-scope a **contractual**
matter rather than a technical one.

*Worth saying out loud:* the read-time version already crosses, and deliberately keeps that crossing to
**a number and a clause number, never a clause**. A write-time version could hold to the same limit.
Whether it may happen at all is still the owner's call.

---

### ✅ Q-50 — Should a preview select the elements it is about to change? → **Yes, both sets, capped at 500** *(asked and answered 2026-09-09)*

Found by reading [`affaan-m/ECC`](https://github.com/affaan-m/ECC) at file level
([33 §5.1](33-external-repository-research.md)). Its `plan-canvas` skill opens a plan **in the human's
browser** so they can point at the element they mean instead of describing it, and its own reason is
that *"move this, change that"* is easier pointed at than typed.

**For a modeller the browser is the wrong canvas and the right one is already open.** Heron's preview
today is a sentence — *"would move 34 ducts up 200 mm in Tower-A, skipping 6."* Everything needed to
show it instead is already in memory: [`RevitWrite.cs`](../revit/Heron.Revit.Addin/RevitWrite.cs) holds
`preview.Ids` and `preview.Skipped` when the preview is offered, and
[`set-selection`](../brain/fragments/set-selection/) already exists.

**The second half is the better half.** Selecting the *skipped* set shows what Heron decided not to
touch — which is `Q-46` answered by showing rather than by wording, and no sentence carries it as well
as thirty highlighted ducts.

**Why it is a question and not a fix.** Three reasons, and each is a real objection:

1. It changes a **confirmation path**, and [Golden Rule 9](14-golden-rules.md) says that path is gated
   in code, not improved on initiative.
2. Changing the selection **destroys the selection the modeller had**, which on a busy day is the thing
   they spent two minutes building. It may need to be offered rather than done.
3. A preview that highlights 4,000 elements is not a preview, it is a mess. There is a count above which
   showing is worse than telling, and nobody has said what it is.

**A second half arrived the same day**, from the index at
[33 §5.10](33-external-repository-research.md): the **"approve with changes"** pattern — *"modifying
tool input before execution… the reference design for safe-by-default harnesses that don't simply block
or permit."*

**Heron's approval is binary on purpose**, and that stays: the token is minted for one preview, a
non-matching token is refused, and the set is re-counted against the live model before anything moves.
**But binary is about the token, not about the conversation.** A modeller who says *"yes, but 150 not
200"* starts the whole request again today. The Heron-shaped version is not executing something
modified — it is **taking the correction and producing a new preview at once**, guarantee intact. That
is a better question than "should the preview select things", and it may be the same answer.

**Answer: YES — 2026-09-09. See [D-60](DECISIONS.md).** The owner: *"yes I think it's good."* Asked
where showing stops being better than telling, he set the number himself: **500**.

**The three objections are answered one at a time, because each was real.**

**1. [Golden Rule 9](14-golden-rules.md) is not touched, and that is a fact about what changes rather
than a promise.** The confirmation path is the token: minted for one preview, refused when it does not
match, and the set re-counted against the live model before anything moves. **Selecting elements writes
nothing and mints nothing.** It changes what the modeller can see while deciding, which is the one thing
Rule 9 was never guarding. A preview that highlights and a preview that does not must accept and refuse
exactly the same tokens, and that is testable rather than asserted.

**2. The modeller's own selection is saved first and put back.** Nobody said this; it is the safe
default and it is recorded here as such so it can be overruled. A selection built over two minutes on a
busy federated model is work, and destroying it to show a preview trades one annoyance for a worse one.
So the current selection is captured before the preview highlights anything and restored when the
preview is declined or expires. **Accepting is the one case where it is not restored** — after a change
lands, the elements that changed are what the modeller wants selected.

**3. 500, and above it Heron tells instead of shows.** Roughly one busy MEP level. Above the cap the
preview stays the sentence it is today and says why: *"4,120 elements — too many to highlight."*
**The cap is a setting with 500 as its default**, not a constant, because the number that is a mess on a
laptop on site is not the number that is a mess on a workstation.

**Both sets are selected, and the skipped one is the half that earns this.** *"Would move 34 ducts up
200 mm in Tower-A, skipping 6"* — the 6 are the sentence nobody reads carefully. Thirty highlighted
ducts and six in a second colour is `Q-46` answered by showing instead of by wording. The cap applies to
each set on its own: a job that skips 4 out of 4,120 can still show the 4.

**The "approve with changes" half stays open, and stays out of this decision.** Taking *"yes, but 150
not 200"* and producing a new preview at once is a better question than this one and it is a different
change — the guarantee holds only because the token is minted fresh, so it is a new preview, never a
modified execution. It is not part of D-60 and it is not answered here.


---

### ✅ Q-49 — Should Heron's gates run automatically, or stay checks a person remembers? → **YES, as a hook in a skill** *(asked and answered 2026-09-09)*

Found by reading [`affaan-m/ECC`](https://github.com/affaan-m/ECC) at file level
([33 §5.1](33-external-repository-research.md)). Its `hooks/hooks.json` registers **PreToolUse** hooks
that run before a tool does and can refuse it. **Heron has no `.claude/settings.json` and no hooks of
any kind.**

Heron holds the rules — `check-structure.py` refuses `Autodesk.Revit` outside `revit/`,
`check-metadata.py` refuses a file without a header, `check-docs.py` refuses a broken link — but every
one of them runs **only when a person types it**. This repository has already paid for that once: four
binding rules were described as proposals in `README.md` for eight days
([32 §4](32-master-architecture-reconciliation.md)) because nothing checked the sentence on the day it
went stale.

**What is not in doubt:** the rules are right and they are cheap. What is in doubt is whether the
enforcement belongs to Heron at all — hooks are a **host** mechanism, and [D-01](DECISIONS.md) gives the
host orchestration. A `.claude/settings.json` in this repository is a convenience for whoever develops
Heron; it is not part of what a modeller installs, and it must never be confused with one.

**Three shapes:**

1. **Nothing.** The gates are run before a push, and the discipline holds because the person is careful.
   It has held for 34 commits.
2. **A pre-commit hook**, which is git's and belongs to the repository rather than to any agent.
3. **A `.claude/settings.json` PreToolUse hook**, which is the host's and only helps whoever uses that
   host.
4. **The hook declared in a skill's own frontmatter** — found on 2026-09-09 in
   [`garrytan/gstack`](https://github.com/garrytan/gstack) ([33 §5.7](33-external-repository-research.md)),
   whose `careful`, `freeze` and `guard` skills each carry their `PreToolUse` entry inline. **The guard
   installs with the capability**, so the two cannot drift apart, and
   [`.claude/skills/`](../.claude/skills/) already exists here. This is the best of the four.

**If the answer is yes, three traps come with it**, each of which silently turns a hook into
decoration. All three are quoted from `freeze/bin/check-freeze.sh`, which learned them the hard way:

- **The decision must be nested under `hookSpecificOutput`.** *"Claude Code ignores a top-level
  `permissionDecision`, which silently no-ops the block."*
- **A hook that dies is read as permission.** *"Any unexpected non-zero death… would otherwise exit with
  no decision JSON, which Claude Code treats as non-blocking — the edit proceeds."* Their fix is an
  `EXIT` trap that emits a deny.
- **Polarity is a decision.** *"freeze is a DENY-tier hook, so an unreadable payload DENIES (fail
  closed)… **a boundary that fails open is not a boundary**."* Their `careful` is ask-tier and fails the
  other way, deliberately.

**Answer — the owner, 2026-09-09: *"yes add the hooks"*.** Shape 4, which was the recommendation.

**Built as [`.claude/skills/heron-guard/`](../.claude/skills/heron-guard/SKILL.md)** — one hook, one
rule: the Revit vendor namespace outside `revit/` is **refused at the moment the edit is proposed**,
rather than when somebody remembers the sweep.

**One rule and not five, deliberately.** Not metadata headers, not links, not counts. **A hook with
false positives is a hook somebody turns off**, and then the boundary is gone along with the noise.
`check-structure.py` still runs the full sweep and still owns everything else it checks.

**All three traps honoured, and each is asserted in
[`tests/test_heron_guard.py`](../tests/test_heron_guard.py):** the decision is nested under
`hookSpecificOutput`; **a crash denies** rather than being read as permission; the polarity is deny-tier
and fails closed. **Plus a fourth thing gstack also ships and the question did not name** — `HERON_GUARD=off`,
because a fail-closed hook that cannot be turned off is one bad edit from a repository nobody can work
in.

**Two adaptations rather than a copy** ([D-25](DECISIONS.md)): it is **Python, not bash**, because Heron
is developed on Windows where a bash hook would simply not run; and the pattern is **built from parts**,
because `check-structure.py` greps file text and would otherwise fail the hook for containing the string
it exists to forbid — which is exactly how a docstring in `heron_context.py` broke the same rule the
same day.

**And it is for developing Heron. It is not part of what a modeller installs** — hooks are the host's
mechanism, [D-01](DECISIONS.md) gives the host orchestration, and nothing in it reaches a model, a
fragment, or a user.

---

### ✅ Q-48 — Should a fragment that READS look inside loaded links? → **Yes, when the modeller asks for it** *(asked and answered 2026-09-09)*

[`tools/check-revit-gate.py`](../tools/check-revit-gate.py) asked question 8 of the fourteen — *are
linked documents handled correctly?* — of all 360 fragments. **62 of them collect from the host document,
declare a reading risk, and say nothing about links anywhere.**

**This is the one finding on that list that is about what a modeller sees rather than about code.** In
Qatar MEP work a model is federated as a matter of course: the architecture is a link, the structure is
a link, and frequently the MEP a coordinator is checking is a link too. A fragment that collects only
the host returns a **confident smaller number**, and nothing in the answer says a link was skipped.
[`HANDOVER.md`](HANDOVER.md) has already hit this from the other side while proving — some cases have to
be **drawn in the host** because the model's content is linked.

**The 45 fragments that WRITE are correctly excluded and that is an API fact, not a judgement.** A
linked element belongs to another document and cannot be changed through this one; you would have to
open the linked file. So a writer collecting the host only is not under-reaching in the way a reader is.

**Why it is a question and not a fix.** Three answers, and they are genuinely different products:

1. **Per fragment.** Each declares whether it reads links. Honest, and it is 62 edits plus a rule for
   every fragment written afterwards.
2. **A platform rule.** Reading spans loaded links by default, and a fragment opts out. One decision,
   but it changes what **every existing recorded proof measured** — a count taken on the host is not the
   count the same fragment would return afterwards, so [D-30](DECISIONS.md) fingerprints would be
   answering a different question than the one they were signed for.
3. **The caller chooses**, as an input the way [D-54](DECISIONS.md) made a view an input. Most flexible,
   and it adds a need to 62 contracts.

**What is NOT in doubt:** an answer that silently omits linked elements is the plausible-zero failure
this repository already legislates against — the same shape as `FILTER_ELEMENTS_BY_CATEGORY` reporting
`unresolvedLevel` so a broken lookup cannot read as a clean count. Whatever is decided, **the answer has
to say whether links were included.**

`python tools/check-revit-gate.py --list links` names the 62.

**Independent evidence arrived on 2026-09-09**, from
[`tirth8205/code-review-graph`](https://github.com/tirth8205/code-review-graph) read at file level
([33 §5.6](33-external-repository-research.md)). Its `uncertainty.py` exists because *"a bare
`result_count: 0` is ambiguous — it can mean **this graph cannot see that relationship**."* Same failure,
different industry, and they reached it with no knowledge of Heron. A fragment that collects the host
and stays silent about links is that sentence exactly.

**Answer: THE MODELLER DECIDES, JOB BY JOB — 2026-09-09. See [D-59](DECISIONS.md).** Reading 3,
in the owner's own words: *"modeller will decide and he know if need to check from linked model or this
model, they know."*

**That rejects reading 2 for the reason the question already gave.** A platform rule that spans links by
default would change what every recorded proof measured — a count taken on the host is not the count the
same fragment returns afterwards — so every [D-30](DECISIONS.md) fingerprint would be answering a
question it was never signed for. Nothing is re-proved to add a feature.

**And it rejects reading 1, which is the less obvious half.** Per-fragment declaration is honest but it
takes the choice away from the person who has the model open. The same fragment is wanted both ways on
different days: *how many ducts on Level 2* is a host question when the modeller is checking their own
work and a federated one when they are checking a coordination issue. A fragment that decided once
cannot serve both, and the modeller would have to know which of two fragments to ask for — which is
exactly the knowledge [09](09-skills-and-fragments.md) exists to stop them needing.

**So it arrives as an input, the way [D-54](DECISIONS.md) made a view an input.** Two fields, and the
second is not optional:

| Field | Where | What it is |
|---|---|---|
| `includeLinks` | `needs`, `source: request` | The modeller's call. **Absent means host only** — the behaviour every existing proof measured |
| `linksSearched` | `provides` | How many linked documents were actually read. **0 with `includeLinks` set means "you asked, none are loaded"**, which is [D-52](DECISIONS.md)'s rule again |

**The second field is the part that answers the question's own warning.** *"Whatever is decided, the
answer has to say whether links were included."* A boolean echo of the input would not say it — it would
repeat what was asked for, not what happened. A count says both.


---

### ✅ Q-47 — There are two capability-gap paths and only one of them can ever fire → **Wire it up, for the missing case only** *(asked and answered 2026-09-09)*

[`tools/check-reachable.py`](../tools/check-reachable.py) found it, and the shape is `Q-43`'s exactly:
**`heron_capability.want()` is called from two tests and from no production code.**

It is the only writer of the `capabilities_wanted` table. Its own docstring says what that costs:

> This is what turns *"we have no fragment for that"* from a **silence** into a **finding**.

So the table is always empty, and `heron_capability.gaps(store)` can only ever return what its caller
passes in `required=`.

**The live path is a different one, and it works.** `heron_brain.catalogue()` computes gaps from the
**skills' own declared `missing` capabilities**, never from `capabilities_wanted`. That is what
`heron_capabilities` reports and what `tests/test_brain_reachable.py` checks, and it is sound —
this entry originally suspected that test of passing for the wrong reason, and it does not.

**So the question is which path is the real one.** Two mechanisms answer *"what does somebody need
that nobody provides"*, one derived from skills and one stored on request. [D-40](DECISIONS.md) is the
rule they are measured against: *an edge is derived before it is stored; store one only when it cannot
be computed from an artifact on demand.* A skill's requirement is computable and is computed. A
**request** for something no skill declares is not computable from any artifact — which is the case
`want()` exists for, and the one that never happens because nothing calls it.

Three ways out, and they are genuinely different:

1. **Wire `want()` up** — a lookup that resolves to nothing records what was asked for. That makes the
   stored half real, and makes `heron_gaps`'s *"things Heron could not do"* answerable from wants as
   well as from failures.
2. **Delete it** — accept that the derived path is the whole answer and stop carrying a table nothing
   fills. Cheapest, and it loses the case above.
3. **Leave it** — and then it should be written down as deliberate, the way the Workflow Engine's
   is in [`HANDOVER.md`](HANDOVER.md), so the next tool that finds it does not raise it again.

**Answer: WIRE IT UP, AND ONLY FOR THE CASE THAT IS GENUINELY MISSING — 2026-09-09. See
[D-63](DECISIONS.md).** Reading 1, at `heron_brain.resolve()`.

**[D-40](DECISIONS.md) points at it rather than away.** A skill's requirement is computable and is
computed. A request for a capability **no artifact declares** cannot be derived by any pass over the
library, because it is not in the library — only somebody asking reveals it. That is the case `want()`
exists for.

**The distinction that makes this one line and not a judgement call was already in that function:**

| What `resolve()` found | Recorded? |
|---|---|
| A provider exists, this release is not on its list | **No** |
| Nobody provides it at all | **Yes** |

**The first row is the important half.** A version wall is not a missing capability. Recording it would
put something on the gap report **that already exists**, and Agent HR would commission a fragment to
build it twice — precisely the failure `heron_gaps.py` was corrected for, where the loudest error in the
trail is the executor behaving correctly.

**What is recorded is Heron's own capability name, never the user's sentence.** And a failed *lookup* is
still not recorded: it has no capability name, only words, and putting a sentence in that column would
make it a different table wearing the same name.


---

### ✅ Q-44 — Should the brain record its own answers in the audit trail? → **Yes, in its own file** *(asked and answered 2026-09-09)*

**The trail only knows what reached Revit.** `HeronAudit` is C# in the add-in, so a request answered
entirely by the brain — `heron_resolve`, `heron_lookup`, `heron_capabilities` — leaves **no record at
all**. [`tools/measure-routes.py`](../tools/measure-routes.py) can therefore report the STRUCTURAL route
share (the library asked its own phrasings) and never the LIVE one (what people actually typed), which
is the only half that says anything about real use.

[19 §7](19-context-and-cost.md) asks for exactly this and names where it should go:

> written into the same audit log rather than a separate telemetry system — one append-only record,
> many readers.

**Why it is a question.** That file is written by C# in the add-in and would then also be written by
Python in the MCP server — **two processes appending to one file that is deliberately never pruned**
([`HeronAudit`](../platform/Heron.Core/HeronAudit.cs)). Interleaved writes, a partial line, and a
reader that must survive both are a design, not a patch. The alternatives are a second file the readers
join (two homes for one fact) or nothing (and the LIVE half stays unmeasurable for ever).

[Golden Rule 14](14-golden-rules.md) — *every important autonomous operation must be auditable* — points
at doing it. Deciding **which fragment answers a request** is not a small operation.

**Answer: YES, AND THE ANSWER WAS ALREADY IN THE READER — 2026-09-09. See [D-62](DECISIONS.md).**

**One file per writer, in one directory, merged at read time.** `heron_gaps.read()` globs the audit
directory for `audit-*.jsonl`, skips a bad line rather than dying on it, and **sorts every entry by
`at`**. It has always merged files and does not care how many there are or who wrote them.
`audit-brain-YYYYMM.jsonl` matches that glob, so **nothing downstream changed** — not the reader, not
[`heron-backup.py`](../tools/heron-backup.py), not `heron_validate.py`, and not one line of C#.

**That is not the "second file the readers join" the question warned about.** There is one home, the
directory. Two files are how two processes write into it with no shared lock, which is the problem that
made this a question rather than a patch.

**What is never written there: the user's sentence.** The live route share needs the route and whether
it resolved. It does not need the words, and this file is append-only and never pruned
([12 §5](12-security-and-permissions.md)). The wording lives in the utterance cache instead — a local store,
a different lifetime, and one a person can delete without losing the trail.

**A refusal is recorded as `ok: false` with its reason**, because a trail holding only the successes
makes a capability nobody provides look like one nobody asked for — and that is exactly the line the
Capability Gap Agent reads.

**It cannot break a request.** `_audit()` returns a no-op if the module will not import, and `record()`
returns False rather than raising when there is nowhere to write — which on Linux with no `APPDATA` is
every call. A logger that can break the request it was only supposed to describe has the priority
backwards.

**It does not claim `HERON-MCP-LOG-010`.** That row says *keyed by Workflow ID*, and every line goes out
with an empty `workflow`. [D-58](DECISIONS.md)'s precedent, in the same words.


---

### ✅ Q-45 — Do the Model Router and Fallback belong in Heron at all? → **The degraded rule stays, the routing goes** *(asked and answered 2026-09-09)*

[19 §3](19-context-and-cost.md) and [19 §4](19-context-and-cost.md) specify a Model Router (task →
model class) and a Fallback (provider unreachable → route elsewhere, and **mark the result degraded**).
Neither exists.

[D-58](DECISIONS.md) has just established that **Heron makes no model calls** — [D-01](DECISIONS.md)
puts every one of them in the host, and `heron_embed` runs locally with no tokens and no cost. On that
reasoning a router here would route nothing.

**But not all of it is the host's.** *"Mark the result degraded, so it never counts as evidence toward
promotion"* is a **trust** rule, and trust is Heron's ([24](24-trust-model.md), [D-30](DECISIONS.md)).
`HERON-KRN-MAV-017` in [28](28-agent-registry.md) carries exactly that clause.

So the question is a split, not a yes or no: **does Heron keep the degraded-result rule and hand the
routing to the host, or do both sections retire?** Answering it decides whether two specified
components get built or struck — and leaving it open is how a specification quietly accumulates
sections nobody will ever implement.

**Answer: THE SPLIT — 2026-09-09. See [D-65](DECISIONS.md).** Routing goes to the host, the
degraded-result rule stays with Heron.

**The kept clause is not a leftover; it is the only one of the three that was ever Heron's.** Choosing a
model class and re-routing round a dead provider are operational choices about a resource Heron does not
hold. *"Never counts as evidence toward promotion"* is a statement about **what may be believed**, and
belief is what [24](24-trust-model.md) and [D-30](DECISIONS.md) govern. `HERON-KRN-MAV-017` keeps that
clause and loses the rest.

**Why a degraded run cannot promote anything.** [D-30](DECISIONS.md) needs a positive case, a negative
case, a named model and a staleness fingerprint. A result produced after a provider fell over is missing
the one thing a proof is for — the guarantee that what ran is what was meant to run. Promoting on it
would make the lifecycle decorative, which is the same sentence [`heron_search.py`](../brain/heron_search.py)
uses about running a DRAFT fragment off an exact word match.

**[19 §3–§4](19-context-and-cost.md) are marked as the host's rather than struck.** A reader who finds
a section missing re-specifies it. A specification that quietly loses a section teaches nothing; one
that says who owns it teaches the boundary — [D-58](DECISIONS.md)'s lesson, on a second row.


---

### ✅ Q-46 — Is "reports nothing it turned down" a defect class or a design spread? → **A rule, fired only on the empty answer** *(asked and answered 2026-09-09)*

[`tools/check-revit-gate.py`](../tools/check-revit-gate.py) asks question 14 of the fourteen — *is
rollback/error reporting clear?* — and **143 of 360 fragments name nothing they refused, skipped or
could not resolve.**

[D-52](DECISIONS.md) is the rule it is measured against: *a count of what was turned down is not a count
of what was found.* And the library's own best fragments treat it as load-bearing —
`FILTER_ELEMENTS_BY_CATEGORY` reports `unresolvedLevel` precisely so a broken level lookup reads as
*"12 found, 12 with no level"* instead of as a plausible zero.

**143 was too many to be a defect list and too many to dismiss**, so the rule was looked for in the
library's own practice rather than reasoned about — and it is there, clearly:

| | reports a refusal | silent |
|---|---|---|
| a fragment that **WRITES** | **172** | 30 |
| a fragment that **READS** | 45 | **113** |

**85% against 28%.** The norm exists and it is not uniform: naming what you refused is already what a
writing fragment does. Reading is where the silence lives, and reading is where the plausible zero does
too.

**So the question was narrowed to the shape [D-52](DECISIONS.md) is actually about — a fragment that
GOES LOOKING and can DROP something on the way — and it is now 59.** One that counts a list it was
handed cannot skip anything, however silent it is.

**A worked example from those 59, and it is not hypothetical.** `filter-elements-by-type` returns
`found: 0` when its exemplar has no type, and **nothing in the answer separates that from "there are
none of this type."** That is exactly why `FILTER_ELEMENTS_BY_CATEGORY` reports `unresolvedLevel` — so
a broken lookup reads as *"12 found, 12 with no level"* rather than as a plausible zero. One fragment in
the library already solved this; 59 have not.

**What is left to decide is the rule, not the list:** must every fragment that collects and drops name
what it dropped, and does that become a validator rule the way the contract's shape is
([`heron_fragment.py`](../brain/heron_fragment.py)), or guidance? A validator rule means 59 edits and
every future fragment held to it.

`python tools/check-revit-gate.py --list reporting` names them.

**A cheaper answer arrived on 2026-09-09**, from
[`tirth8205/code-review-graph`](https://github.com/tirth8205/code-review-graph) read at file level
([33 §5.6](33-external-repository-research.md)). The reason to hesitate here is cost — every fragment
naming what it turned down makes every answer longer. **They measured it the other way round:**

> One short sentence on the empty case is therefore a **token saving, not a cost**: it replaces a
> multi-thousand-token fallback search with roughly thirty tokens of honesty.

And three choices make that hold, all three available here:

1. **Attach the marker only when the result is empty** — so *"every response that carries results stays
   byte-identical to before."* Nothing already working gets longer.
2. **Hard-cap it.** Honesty with a ceiling cannot grow into a paragraph.
3. **Keep the blind-spot list as data, not conditionals**, and delete an entry when the gap closes —
   which is [D-54](DECISIONS.md) built into the structure instead of relied on as a discipline.

**So the choice above may not be "59 edits or guidance".** A third shape exists: one rule that fires
only on the empty case, capped, applied once.

**Answer: A RULE, AND THE THIRD SHAPE — 2026-09-09. See [D-64](DECISIONS.md).** Not 59 hand
edits and not guidance.

**The dropped-count is a declared output**, and *that one exists* is the rule — never that a particular
word does. `unresolvedLevel`, `skipped`, `refused` all satisfy it, because what was dropped varies by
fragment. A rule about the word would be a naming convention wearing a correctness rule's clothes.

**The marker rides only on the empty answer**, so every answer that carries results stays byte-identical
to today. Nothing that already works gets longer, which is what made the cost objection dissolve rather
than be traded off. **Capped**, so honesty cannot grow into a paragraph.

**It is checked today and validated later, and that order is deliberate.**
[`check-revit-gate.py`](../tools/check-revit-gate.py)'s question 14 now reads the **contract** as well
as the code — declare a dropped-count in `provides` and it passes even where the code reads silent. The
59 are a checked worklist and every fragment written afterwards meets the same question on the same run.
Putting it in [`heron_fragment.validate()`](../brain/heron_fragment.py) today would make 59 fragments
invalid and fail every gate in the repository on a library that is not broken. **`validate()` is where
it moves once the 59 are cleared, and moving it is how the worklist is declared finished.**


---

### ✅ Q-43 — What may be written into the utterance cache, and by what? → **Only a run that came back** *(asked and answered 2026-09-09)*

**Found by building, not by reading.** [`tools/measure-routes.py`](../tools/measure-routes.py) parses
the tree for real calls to `heron_search.remember()` — the only function that writes the utterance
cache — and finds exactly one, in [`tests/test_search.py`](../tests/test_search.py). **No production
code calls it.** Not `ask()`, not `find()`, not any MCP tool.

So route 2 can never fire for a real user. The cache is built, tested, indexed, and permanently empty.

That matters because of where the specification puts it. [19 §5](19-context-and-cost.md) makes it
**step 1** of the pipeline that must run before any model is invoked, and [19 §6](19-context-and-cost.md)
calls it *"the one that pays for itself faster than any of the others"*.

**Why this is a question and not a patch.** The obvious wiring — have `find()` call `remember()` with
whatever it just returned — is the dangerous one:

| | |
|---|---|
| A keyword answer is a **candidate**, not a decision | `heron_search.py` says so in its own words: *"a candidate, never a decision"* |
| Caching it makes the guess **permanent** | the next identical wording returns by route 2 and **never searches at all** |
| The wrong answer then looks like the fast answer | which is the confident-wrong-retrieval failure [05 §4](05-heron-brain.md) built the keyword layer to avoid |

**So the real question is what counts as confirmed.** Candidates, none of them obviously right:

1. **The fragment actually ran and returned a result.** Strongest evidence, and it only exists after
   execution — so nothing is cached for a request that was merely resolved.
2. **The user accepted the candidate** — but [D-01](DECISIONS.md) puts that conversation in the host,
   which does not call back into the brain today.
3. **The identity route resolved it** — safe, and worthless: those already answer in one lookup, so
   caching them saves nothing.

**Also unanswered: what invalidates it.** [19 §6](19-context-and-cost.md) names `SkillCreated`,
`FragmentApproved` and project change. `recall()` already deletes a row whose fragment has vanished,
which is the *deleted* case only — a fragment that is **edited** keeps its id and its stale cache entry.

**Answer: A COMPLETED RUN, AND NOTHING ELSE — 2026-09-09. See [D-61](DECISIONS.md).** Candidate 1,
and the other two fail on their own terms rather than on preference: **the user accepted it** is a rule
nothing can evaluate, because [D-01](DECISIONS.md) puts that conversation in the host and the host does
not call back; **the identity route resolved it** is safe and worthless, because those already answer in
one lookup.

**The decision is in the signature, not in this paragraph.** `remember(store, text, fragment_id,
evidence)`, and **`evidence` has no default**. A default would make the safe call and the dangerous call
look identical at the call site, and the dangerous one is what somebody reaches for while wiring this up
in a hurry. Anything but `RAN` raises `NotEvidence`. Six values were tried in
[`tests/test_search.py`](../tests/test_search.py) — `keywords`, `hybrid`, `identity`, `cache`, `None`,
`""` — and none of them wrote a row.

**Staleness is answered with [D-30](DECISIONS.md)'s own hash rather than a second mechanism.** The row
carries the fragment's `fingerprint()` from the moment it was written, and `index()` calls
`forget_stale()`. Re-indexing is when Heron re-reads the files, so re-indexing is when a row written
against the old bytes dies. A cached wording and a recorded proof go out of date for the same reason and
must not be able to disagree about when. The check is deliberately **not** in `recall()`: route 2 is the
fast route, and hashing implementation files on it would spend the saving it exists to make.

**The cache is still empty, and the reason changed.** The evidence only exists after a run, a run
happens in the add-in, and **no workflow id crosses the seam into the brain** — so nothing can join a
wording to a run that succeeded. [`measure-routes.py`](../tools/measure-routes.py) was corrected in the
same change per [D-54](DECISIONS.md): it used to say *"where remember() should be called from is a real
decision"*, and that decision is now made. **One seam remains and it is named.**


---

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

Not a count, for the same reason [D-30](#q-9--what-promotes-a-fragment-to-production) is not one.
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
[`NEEDS-CHECKING.md`](NEEDS-CHECKING.md).

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

**Answer:** ACCEPTED, 2026-08-28 — all 30 Articles, binding. See [D-43](DECISIONS.md).**

Offered acceptance, deferral, or having all 30 read out, **Ajmal asked for all 30 to be read**, and
accepted them after reading. That is the difference between a confirmation and a tap, and it is why the
option was offered: a 30-article document accepted by pressing a button is not accepted, it is unread.

**Reading it aloud found three stale statements in it**, none of which changed what an Article requires
and all of which would have been read as current by whoever implements enforcement: it described its basis
as *"Golden Rules 1–15 (official) and 16–19 (proposed)"* — wrong twice, since all **21** became official
earlier the same day; **eight Articles cited a "Proposed" Golden Rule** that was no longer proposed; and
it said an agent receives *"not all 27"* Articles when there are **30**. All corrected on acceptance.

**Accepting the Articles does not make them true.** 8, 9, 11, 12a, 12b and 12c are precisely what groups
`C`, `D` and `E` of [`NEEDS-CHECKING.md`](NEEDS-CHECKING.md) exist to test.

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
to a client, and it happens to be the same thing [Q-5](#q-5--mcp-tool-granularity--thick-and-specific)
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
open until the repository goes public ([Q-28](#q-28--when-does-the-repo-go-public--when-licence--safety-files-exist-and-there-is-working-code)):
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
standard `.addin` manifest, and Apache 2.0. [Q-38](#q-38--what-is-the-exact-install-command-new) is the
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
discovering it on someone else's machine. Bundling stays open under [Q-38](#q-38--what-is-the-exact-install-command-new).

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

[`NEEDS-CHECKING.md`](NEEDS-CHECKING.md) — every unproven claim as a numbered item (`A1`, `D3`), in
dependency order, each stating what PASS actually looks like. Items are added whenever something is built
away from Revit, and deleted only when they have actually passed.

Three things make it work rather than being a to-do list:

- **Dependency order.** Nothing in group D can be attempted before group A compiles. Working down instead
  of around is what stops a "pass" that was never really tested.
- **What needs Revit is separated from what does not.** Group A needs Windows and the SDK only — and the
  round-trip test proves the bridge *and* most of the lease there, before Revit is ever opened.
- **One register, not one per document.** [HANDOVER](HANDOVER.md) §6 points at it rather than keeping
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
