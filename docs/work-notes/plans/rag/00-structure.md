# RAG — the structure

> **Type:** Operational work note. **Not specification.** Where a sentence here disagrees with the
> [Constitution](../../../../HERON_CONSTITUTION.md), the [Golden Rules](../../../14-golden-rules.md) or
> [DECISIONS.md](../../../DECISIONS.md), **those win and this note is out of date.**
> **Status:** **Active — the shape is decided, nothing is built.** Opened 2026-09-10.
> **Owner:** Ajmal PS. **Decided by him, 2026-09-10:** all six structural questions are in scope.
> **This note does not write a decision into [DECISIONS.md](../../../DECISIONS.md).** A work note does
> not get to do that on its own. When this page is agreed, §1 becomes **a numbered decision there**
> and the durable half moves to [`docs/05`](../../../05-heron-brain.md). **The number is allocated
> when it is written, not now** — `check-docs.py` refuses a decision number that nothing defines, and
> it refused the one this note first guessed at.
> **Read this first.** [`01-requirements.md`](01-requirements.md) says *what*,
> [`02-implementation.md`](02-implementation.md) says *how*, [`03-working-note.md`](03-working-note.md)
> says *where it stands*. **Structure decides all three**, so it sits ahead of them.

---

## 1. The decision

Six structural questions were put to the owner on 2026-09-10. His answer was **all six**. Heron's RAG
is not a search box with a filter on it; it is the full shape — it searches again when it should, it
follows relationships and not only words, it knows a clause sits inside a section, it decides where to
look, it re-reads its own shortlist, and it hands back something an answer can be *grounded in*.

**What that does not mean.** It is not one batch. [`ROADMAP.md`](../../../ROADMAP.md)'s governing idea
is that a thing built all at once is designed against assumptions that turn out to be wrong, and it is
right. **All six is the target; §6 is the order.** Nothing here is dropped — the order exists so each
piece can be proved rather than believed.

---

## 2. What "all six" actually costs — read this before §3

Two of the six are **not new machinery at all.** They are things the brain already knows and does not
say out loud. That is not a lucky accident; it follows from a decision taken long ago.

[**D-01**](../../../DECISIONS.md) makes Claude Code the host, and the host does the thinking a chatbot
would do. `check-metadata.py` prints it as fact:

```text
Provided by the host, so no file here implements them (docs/02 section 7):
  - HERON-ORC-INT-002    the host classifies what is being asked
  - HERON-ORC-SUM-006    the host writes the reply the user reads
```

`heron_context.py` says the same from the other side: **it does not classify what the user meant**, and
an assumed path says it was assumed.

So the six split three ways:

| | The six | What it really is |
|---|---|---|
| **1** Searches again | **Reporting.** The host already loops. It cannot loop well because retrieval never tells it *"this was a coin toss"* | say the confidence out loud |
| **6** Answers from what it found | **Reporting, plus one check.** The host writes the answer — that is D-01. The brain owes it a packet it cannot invent from, **and a way to say a draft drifted from it** | citations in the packet, and §3.6a |
| **3** Knows a clause is inside a section | **Decided at ingestion.** Cannot be retrofitted cheaply — chunking is where hierarchy is won or lost | get it right in Stage 1 |
| **2** Follows relationships | New machinery — a third retrieval route, and **conditional**: the same idea over the *fragment* graph was measured at six settings and lost ([`34 §2.13`](../../../34-patterns-adapted.md)) | a density count decides whether it is built at all |
| **4** Decides where to look | New machinery — the Librarian | must not become a merged query (§3.4) |
| **5** Re-reads its shortlist | New machinery — a re-ranker model | optional and absent-tolerant, like the encoder |

**In Revit terms:** you thought you were being asked to build six new tools. Two of them are ticking
*"show me the warnings"* on tools you already have.

---

## 3. The six, one at a time

### 3.1 It searches again — *the honest-confidence question*

**What it means here.** Not a loop inside Python. The host asks, reads, and asks again — it already
can. What stops it working is that **retrieval answers with the same face whether it is sure or
guessing.**

**And the evidence is already recorded.** At 14 fragments the top five spanned **0.0021**, where one
rank of fusion is **0.00026** — [`retrieval-history.md`](../../../../brain/retrieval-history.md) calls
that *"noise rather than ranking. A reader who takes the top hit as 'the answer' is reading a coin
toss."* **Nobody is told that at query time.** Today's run is the same shape: five candidates, and
`ISOLATE_ELEMENTS` and `CREATE_DUCT` tie at 0.0164 exactly.

**So the work is:** every answer carries how contested it was — the spread across the shortlist in
units of *one fusion rank*, whether the routes agreed, and whether anything in the shortlist has a real
claim on the sentence. `heron_retrieve.py` already computes all of it and throws it away.

**In Revit terms:** a schedule that says *"126 ducts"* and a schedule that says *"126 ducts, and 40 of
them have no system assigned"*. Same query. Only one of them lets you decide what to do next.

**Must not break:** nothing. This adds output.
**Proved by:** two queries — one clear, one genuinely ambiguous — where the reported confidence differs
in the right direction, and a test that asserts it.
**Cost:** small. **Needs:** nothing. **Can be done today.**

---

### 3.2 It follows relationships — *corrected 2026-09-10, and now conditional*

**What it means here.** A third route beside words and nearness: follow edges. *This clause governs
insulation → insulation belongs to duct systems → this fragment reads a duct's insulation.* No text
match anywhere in that chain.

**What exists.** [`heron_graph.py`](../../../../brain/heron_graph.py) — Step 13, *what breaks if this
changes*. Every edge but one is **computed from the fragments on demand** ([D-40](../../../DECISIONS.md));
the only stored edge is a skill's requirement. It already names the dangerous case out loud: a **sole
provider**, because whatever asked for its capability never named it.

**What is missing.** Documents are not in the graph, because documents are not anywhere (see
[`01-requirements.md` §5](01-requirements.md)). A clause has no node, so it has no edges.

> ### ⚠ This section was written without checking, and the repository had already tried it
>
> [**`34 §2.13`**](../../../34-patterns-adapted.md) records a third retrieval stream **measured
> and rejected**: [`tools/measure-graph.py`](../../../../tools/measure-graph.py), 360 questions,
> four query shapes, **six settings — all six lost.** The gentlest cost 1.1 points of P@1, the
> strongest 14, and **P@5 never improved at any setting**, so it did not widen recall either — the
> one thing a graph stream is supposed to be good at.
>
> **And it named the property that decides it: density.** Heron's composition graph runs a
> **median of 50 neighbours per fragment, worst 230**. A fragment providing `IList<Element>`
> composes with most of the library, so *"the neighbours of the best hit"* is not a signal — it is
> a large slice of the library added as competitors.
>
> **What that kills, and what it leaves standing:**
>
> | | |
> |---|---|
> | ❌ **Dead** | Feeding the **fragment composition graph** into retrieval as a third stream. Measured, six settings, rejected. Do not re-propose it without new evidence |
> | ⏸ **Open, unmeasured** | A **document** graph — *clause governs system, system has elements* — is **not that graph**. It does not exist and its density is unknown |
> | ✅ **The rule that survives** | **Measure the neighbour count before building anything.** If document edges are as dense as fragment edges, this loses the same way for the same reason |
>
> So 3.2 is **conditional**, not planned. It earns a stage by passing a density check, and the
> check is cheap — it is a count, not an experiment.

**The rule this route inherits.** `heron_retrieve.py` is explicit that **route weights follow
measurement, not feeling** — with equal weights, fusion is symmetric and the winner is decided by
whatever the tiebreak happens to be, which on its very first run was *alphabetical order*. So the edge
route **gets no vote until it has been measured**, exactly as the nearness route had to earn its own.

**In Revit terms:** text search is *"find every element with 'insulation' in a parameter"*. The graph
is *"select this duct, then Select Connected"*. The second one finds things that never say the word.

**Must not break:** D-40 — edges stay derived. A stored document edge is a cache that will go stale.
**Gated by:** the density count above. **A count, before a line of retrieval code.**
**Proved by:** one question answerable *only* by following an edge, which the words and nearness routes
both miss, and a recorded measurement before the route is given any weight — the same bar
[`tools/measure-graph.py`](../../../../tools/measure-graph.py) already set and the fragment graph failed.
**Cost:** medium, and **possibly zero** — the density count may end it.
**Needs:** documents to exist first.

---

### 3.3 It knows a clause sits inside a section — *the one that cannot be retrofitted*

**What it means here.** A standard is Part → Section → Clause. The **clause** is the citable unit —
`QCS 2014 §21.3.2` is what a person can check. The **section** is what makes it make sense.

**Retrieve the small thing; return it with its parent available.** That is the whole idea, and it is a
choice made in the chunker, not in retrieval.

**What exists to build on.** `heron_context.py` already has exactly this concept for fragments:
**depth** — `abstract` / `overview` / `full` — taking a generation packet from **5,737 characters to
372** with the request unchanged, and a part that lost something **says how much**. Hierarchy for
documents is that idea extended, **not a second mechanism.**

**In Revit terms:** the detail callout versus the sheet it lives on. You cite the callout. You cannot
read it without knowing which sheet it came from.

**Must not break:** R-08 — chunking must not split `OST_DuctCurves`, a parameter GUID, or a clause
number. Structure-first splitting serves both goals, which is why they are one decision.
**Proved by:** a document with three levels goes in, a clause comes back, and the clause knows its
parent. And the token test from [`02-implementation.md` §4.3](02-implementation.md) still passes.
**Cost:** small **if decided now**, large later. **Needs:** to be settled before the first chunk is
written.

---

### 3.4 It decides where to look — *the one with a trap in it*

**What it means here.** `HERON-RAG-LIB-001`, the Librarian, picks the scope before anything is
searched. Today the caller names one and cross-scope is impossible to *write* — `ATTACH` is refused by
name, and a project store refuses to open when no project is identified.

**The trap.** *"An agent that decides which scopes to search"* reads as *"an agent that searches
several"*. Implemented that way it is a `UNION`, and a `UNION` is one client's knowledge in the same
result set as another's — which [D-33](../../../DECISIONS.md) calls a **contractual** problem, not a
technical one.

**The rule, written down before anybody codes it:**

> The Librarian decides **which one**. If it needs two, it makes **two separate queries** and says which
> answer came from where. It never merges them into one query, and `CrossScopeRefused` stays exactly as
> it is.

**In Revit terms:** you can open two models side by side. You do not copy one into the other to compare
them.

**Must not break:** GR 5, and it must not become a reason to soften `CrossScopeRefused`.
**Proved by:** a question that legitimately needs company **and** project knowledge produces two
queries and two labelled answers — and an attempt to write it as one query still raises.
**Cost:** small. **Needs:** documents in more than one scope.

---

### 3.5 It re-reads its own shortlist — *the one that must stay optional*

**What it means here.** A cross-encoder reads (question, chunk) pairs together and scores them
properly, instead of comparing two vectors made independently. It is the single biggest quality jump
available, and it is slow, so it runs on the top ~20 only — which is exactly what
[`05 §4.4`](../../../05-heron-brain.md) already specifies.

**What exists.** A **nudge**, deliberately smaller than one rank of fusion, so it settles a tie and
cannot overturn a better match. Its first version was **eight times too big** and a test caught it
because the test asserted the arithmetic rather than the intention. **Two of the four signals
[`05 §4.4`](../../../05-heron-brain.md) names do not exist at all** — nothing records success rate,
nothing records recency of use.

**The rule it inherits from the encoder.** `heron_embed.py` has two backends and **reports which one
answered**; when the trained model is absent it falls back and says so. The re-ranker does the same: an
installation without it is slower to be right, never broken. [D-01](../../../DECISIONS.md)'s promise —
per-user install, no administrator rights — binds it, and `model2vec` already proved that is possible.

**In Revit terms:** the difference between sorting a schedule by a parameter and actually opening each
view to look. You would not do it for 360 rows. You would do it for the shortlist of 20.

**Must not break:** the nudge's arithmetic rule, and the absent-backend fallback.
**Proved by:** a recorded measurement, before and after, at the same corpus size and on the same
questions — and a run with the re-ranker uninstalled that still answers.
**Cost:** medium. **Needs:** retrieval worth re-ranking, and §3.1 so ties are visible first.

---

### 3.6 It answers from what it found — *and from today, it is checked*

**What it means here — and what it does not.** It does **not** mean putting a language model inside
`brain/`. [D-01](../../../DECISIONS.md) settled that: **the host writes the reply the user reads.**

What the brain owes is a **packet the host cannot invent from** — the retrieved text, its citations,
its confidence, and an explicit statement of what is *missing*. That is `heron_context.py`, which
exists, has four paths, a declared parts list per path, and **raises** when a part outside the list is
carried.

**And it already refuses honestly.** The `STANDARDS` path raises `SourceMissing`:

> *"the STANDARDS path needs the clauses a check CITES, and a scope store holds `fragments` and `meta`
> only — there is no clause store in this installation. Refusing rather than returning a …"*

**That refusal is the whole design, and it is the thing most likely to be lost.** When the clause store
exists, the path must narrow to *"nothing indexed covers this"* — it must **not** soften into an
answer. A system that refused honestly while empty and began guessing once full would be worse than the
one that refused.

**In Revit terms:** a schedule shows what is in the model. It does not invent a fire rating for a wall
that has none — it shows the cell empty, and you go and fill it in. That empty cell is the feature.

**And from 2026-09-10 there is a third part.**

- **The check.** *No source, no claim* is a rule nobody can test. **It becomes a number.** Every
  sentence in a proposed answer that makes a factual claim is compared against the chunk it cites —
  **deterministically, with no model call and no network** — and what disagrees is flagged with how
  far off it is. Taken from an outside repository and re-authored:
  [`the reading` §3.1](../../investigations/jamwithai-repositories-2026-09-10.md). Mechanism below.

#### 3.6a The check, and why it belongs here

**Heron does not write the answer, so how can Heron check it?** Because checking and writing are
different acts, and this repository has already separated them once.

[`heron_validate.py`](../../../../brain/heron_validate.py) — the Fragment Validation Agent —
**gathers the evidence for a proof and never signs one**, because [D-30](../../../DECISIONS.md) says an
agent that can stamp 193 fragments is the fastest machine ever built for making an unproven claim look
proven. **This is that shape again, one layer up:**

> **The brain cannot write the answer. It can refuse to endorse one.**

So the check is a call the host makes *back* into the brain, with its draft and the packet the draft was
built from. It returns a report, **never a rewrite**. An answer that fails is flagged, not silently
repaired — a checker that quietly fixes its own findings is how a wrong answer becomes an invisible one.

**Whose responsibility it is.** `HERON-RAG-CIT-014` in [`docs/28`](../../../28-agent-registry.md), whose
registry line already reads *"Tracks provenance. **No source, no claim**"*. That row has had no code
since it was written; this is what it was for. It is **not** `HERON-RAG-VAL-013`, which checks
*retrieved* knowledge before use — that is §3.4, a different act at a different moment.

**In Revit terms:** a model audit, not a review meeting. Not *"does this look right"* — every dimension
compared against the element it dimensions, automatically, and the ones that disagree listed with how
far off they are. Nobody argues with the list; they go and look.

**Must not break:** D-01 — the check never becomes a writer. The parts-list `raise`. The refusal. And
**a threshold is never lowered to reduce flags**, which is the same rule as never weakening a fragment's
declared words to buy a rank.
**Proved by:** a fabricated sentence is **flagged with its ratio**; a sentence that merely *understates*
its source **passes**; and the whole thing runs with no network and no keys.
**Cost:** small on top of §3.3 and citations — the comparison is standard library.
**Needs:** citations, so it follows them.

---

## 4. The shape, when all six are in

```text
                    a sentence a modeller actually said
                                   |
                                   v
  [3.4]  the LIBRARIAN picks the scope                        HERON-RAG-LIB-001   NEW
         one scope. Two scopes means TWO queries, never one merged one
                                   |
                                   v
  [ - ]  the STRUCTURED FILTER, as a plain SQL WHERE          already built
         scope, Revit version, status, domain - a WALL, never a soft signal
                                   |
                                   v
  [3.2]  THREE ROUTES over the survivors
             words       FTS5, exact tokens                   already built
             nearness    the trained encoder                  already built
             edges       the graph - finds what never says the word      NEW, CONDITIONAL
                         over DOCUMENTS only. The same route over the fragment
                         graph was measured at six settings and lost every one
                                   |
                                   v
  [ - ]  FUSION by reciprocal rank                            already built
         the third route gets NO WEIGHT until it is measured
                                   |
                                   v
  [3.5]  the RE-RANKER re-reads the top 20                    NEW, and OPTIONAL
         absent = slower to be right, never broken
                                   |
                                   v
  [3.3]  the CHUNK, with its PARENT available                 decided in the chunker
  [3.6]  the CONTEXT PACKET                                   heron_context.py
             the parts, at a depth, each saying what it lost
             + the CITATION, which opens                                  NEW
             + the CONFIDENCE, and what was tied                          NEW  [3.1]
             + what is MISSING - or a refusal by name
                                   |
                                   v
         the HOST writes the answer the user reads            D-01. NEVER the brain
                                   |
                    not enough? ask again, with better words
                                   |
                                   +---------> back to the top    <-- [3.1] is what makes
                                                                      this loop worth running
```

---

## 5. What depends on what

| This | Cannot start before | Because |
|---|---|---|
| **3.1** confidence | — | The numbers already exist and are discarded |
| **3.3** hierarchy | — | **It is decided in the chunker.** Late means re-ingesting everything |
| **3.4** librarian | documents in two scopes | Nothing to choose between |
| **3.2** edges | documents ingested, **and a density count** | A clause with no node has no edges — and a dense graph is competitors, not signal |
| **3.6** grounded packet | citations | The packet's job is to carry them |
| **3.5** re-ranker | retrieval working, **and 3.1** | You cannot see a re-ranker help if ties were invisible before it |

**Two are free of every dependency, and they are the two most easily left until last.** 3.1 because it
looks like polish; 3.3 because it looks like a detail inside chunking. **3.3 is the one that gets
expensive if postponed** — every other item can be added to a working system, and hierarchy cannot,
because it is the shape of the data.

---

## 6. The order, revised by this decision

This replaces the stage order in [`02-implementation.md`](02-implementation.md), which was written
before the six were decided.

| Stage | What | The six | Needs |
|---|---|---|---|
| **0** | Measure the trained backend and record it; fix the four stale sentences | — | nothing |
| **0b** | **Say the confidence out loud** | **3.1** | nothing |
| **1** | `documents` + `chunks` tables, the ingester, **hierarchy in the chunker** | **3.3** | nothing |
| **2** | Documents come back out, alongside fragments | — | Stage 1 |
| **3** | Citations, the refusal that must not soften, **and the fabrication check** | **3.6**, **3.6a** | Stage 2 |
| **4** | The Librarian picks the scope | **3.4** | documents in two scopes |
| **5** | Document nodes, **then a density count**, and the edge route **only if it passes** | **3.2** | Stage 2 |
| **6** | Maintenance — re-index on change, duplicates | — | Stage 2 |
| **7** | The re-ranker, measured before and after | **3.5** | Stage 2, and 0b |
| **8** | Trust, conflict, research | — | Stage 3 |

**0b is new and it is deliberately early.** It costs almost nothing, it needs nothing, and every later
measurement is read against it. Without it, Stage 7 cannot show the re-ranker helped, because nobody
recorded what a tie looked like before.

**None of stages 0 to 8 needs Revit, and none needs the PC.**

---

## 7. What this changes in the other three notes

| Note | Change | Done? |
|---|---|---|
| [`01-requirements.md`](01-requirements.md) | A new section **G** — the structural requirements the six create, R-34 onward | **yes** |
| [`02-implementation.md`](02-implementation.md) | Its stage list is superseded by §6 above; a pointer added at its head | **yes** |
| [`03-working-note.md`](03-working-note.md) | The decision logged, dated | **yes** |

---

## 8. Still not decided

The four owner questions in [`03-working-note.md` §5](03-working-note.md) stand — which documents
first, whether the store keeps the file or points at it, whether Heron records success rate and
recency, and whether cloud embedding is wanted at all. **None blocks Stage 0 or 0b.**

Deciding all six adds three more, and each is small enough to answer in a sentence when we reach it:

| | Question | Raised by | Cheapest safe default |
|---|---|---|---|
| **S-1** | When the Librarian needs two scopes, does Heron ask two questions itself, or hand the choice back to the host? | 3.4 | **Hand it back.** D-01 says the host classifies; two scopes is a classification |
| **S-2** | How deep does a document's hierarchy go — Part / Section / Clause, or arbitrary depth? | 3.3 | **Arbitrary, stored as a parent link.** Fixed levels are a guess about documents nobody has read yet |
| **S-3** | Is the confidence a number, or a sentence? | 3.1 | **Both, and the sentence is the contract.** `heron_retrieve` already says *"both routes agree"* in words, and words are what survived every other measurement in this repository |

**Nothing on this page needs a decision to start.** Stage 0 and 0b can run tomorrow.
