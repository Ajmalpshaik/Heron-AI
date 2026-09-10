# RAG — the working note

> **Type:** Operational work note. **Not specification.** Where a sentence here disagrees with the
> [Constitution](../../../../HERON_CONSTITUTION.md), the [Golden Rules](../../../14-golden-rules.md) or
> [DECISIONS.md](../../../DECISIONS.md), **those win and this note is out of date.**
> **Status:** **Active — planning done, no stage started.**
> **Owner:** Ajmal PS. **Opened:** 2026-09-10. **Branch:** `plan/rag-knowledge-library`.
> **Closure condition:** the four sentences in [`01-requirements.md` §8](01-requirements.md) are all
> true, and every measurement below has been moved into
> [`brain/retrieval-history.md`](../../../../brain/retrieval-history.md).
> **Reads with:** [`00-structure.md`](00-structure.md) — what kind of RAG, and the order ·
> [`01-requirements.md`](01-requirements.md) — what it must do ·
> [`02-implementation.md`](02-implementation.md) — how each stage is built.

---

## 1. What this file is for

The other two notes do not change much. **This one changes every session.** It carries what was
actually run, what it printed, what was surprising, what is waiting on a decision, and what was found
and deliberately left alone.

**Nothing here is done because this file says so.** Every claim names the command that shows it.

---

## 2. Where it stands

| | |
|---|---|
| **The shape** | **DECIDED 2026-09-10 — all six.** [`00-structure.md`](00-structure.md). Becomes **a numbered decision** when the note is agreed; not written to [DECISIONS.md](../../../DECISIONS.md) yet |
| Stage 0 — measure and record | **not started.** One measurement taken (§3) and **not yet written into `retrieval-history.md`** |
| Stage 0b — say the confidence out loud | **not started.** New, from the decision. Costs almost nothing, needs nothing, and every later measurement is read against it |
| Stage 1 — a document can go in | **not started.** No `documents` table, no `chunks` table, no ingester. **Now also carries hierarchy** — the one thing that cannot be retrofitted cheaply |
| Stages 2 to 8 | **not started**, blocked on Stage 1 |
| Blocking anybody? | **No.** Nothing on this track needs Revit, the PC, or a model to be open |

---

## 3. Measurement — 2026-09-10, 360 fragments, `model` backend

**The first numbers recorded for the trained backend, at any library size.** Recorded here rather than
in [`brain/retrieval-history.md`](../../../../brain/retrieval-history.md) because that file is
permanent and this track has not yet earned a section in it — Stage 0 is where it moves.

```bash
python brain/heron_embed.py                                        # Backend: model
python brain/heron_fragment.py | tail -3                           # 360 well-formed, 180 PROVEN
python brain/heron_retrieve.py "show me every duct in the model" --revit 2024
python tests/test_embed.py                                         # prints the measured ranks
```

### 3.1 The tracked query

`show me every duct in the model`, Revit 2024, 360 fragments, backend `model`:

| Fragment | Status | Score | Found by |
|---|---|---|---|
| `FRG-MEP-031` `SELECT_BY_INSULATION` | PROVEN | 0.0318 | words #3 + nearness #3 — both agree |
| `FRG-SEL-009` `SELECT_BY_HOST` | DRAFT | 0.0278 | words #13 + nearness #11 |
| `FRG-MEP-030` `SELECT_BY_MEP_SYSTEM` | PROVEN | 0.0254 | words #18 + nearness #20 |
| `FRG-VIEW-002` `ISOLATE_ELEMENTS` | DRAFT | 0.0164 | words #1 |
| `FRG-MEP-003` `CREATE_DUCT` | DRAFT | 0.0164 | nearness #1 |

The duct filter itself, `FRG-ELE-001` (`filter-elements-by-category`), measured by `test_embed.py`:

> `duct filter is #- of 100 by words, #19 of 100 by nearness`

Against the last recorded line, 2026-08-31, 59 fragments, `lexical`: **17th of 59 by words, 36th of 59
by nearness.**

### 3.2 How to read that, and how not to

**Two variables moved at once.** The corpus went from 59 to 360 *and* the backend changed from
`lexical` to `model`. Neither number below separates them, and no honest reading of this pair can.
Separating them is what `check-routing.py` and `check-intrusion.py` are for, and **neither has been run
on the trained backend** — that is Stage 0, and it is the reason Stage 0 exists.

**`#- of 100` means outside the first hundred, not 360th.** The list is capped at 100. It is a real
degradation on the words route and it is not as large as it looks.

**The nearness route improved.** 36th of 59 is the middle of the library; 19th of 100 is the top fifth.
That is the trained encoder doing something the character n-grams did not.

**And this query was already retired as an instrument, for a reason that still holds.**
[`retrieval-history.md`](../../../../brain/retrieval-history.md) concluded on 2026-08-30 that *"show me
every duct in the model"* is **filter-then-show — a composition, and a composition is what a skill
names, not a fragment.** Three fragments fairly claim that sentence. So a poor result here is partly
the question being asked of the wrong layer, and **it must not be reported as the trained backend
failing.** It is kept for continuity with eleven earlier rows and nothing more.

**One thing in the table is worth a second look regardless of all that.** `CREATE_DUCT` — a fragment
that **writes** — is nearness #1 for a sentence beginning *"show me"*. The same shape appeared at 59
fragments on `lexical`, where two dimensioning fragments that write reached a shortlist for the same
question. A trained encoder was expected to separate *"show me"* from *"make me"*. On this evidence it
has not, and **that is a question for Stage 0's proper instruments**, not a conclusion.

---

## 4. Defects found while planning — recorded, not fixed

[`work-notes/README.md`](../../README.md): *"A defect found while tidying is recorded, not fixed."*
Each of these is a sentence that was true when written and is false now.

| # | Where | Says | Actually |
|---|---|---|---|
| **W-1** | [`brain/README.md`](../../../../brain/README.md), the `heron_embed.py` row | the `model` backend *"has never run (`A7`)"* | it runs. `python brain/heron_embed.py` prints `Backend: model` |
| **W-2** | [`brain/README.md`](../../../../brain/README.md), the `retrieval-history.md` row | the file *"currently records the built-in backend collapsing as the library grows, which is the evidence for `A7`"* | `A7` is closed. The row describes the evidence for a thing that has already happened |
| **W-3** | [`tests/test_embed.py`](../../../../tests/test_embed.py), closing text | *"it could not be tested here, because this container's network refuses huggingface.co. A7 in NEEDS-CHECKING.md is that run"* | the suite **just ran on the model backend** and printed `Backend in use: model` eight lines above this sentence |
| **W-4** | [`brain/retrieval-history.md`](../../../../brain/retrieval-history.md) | eleven rows, every one `lexical`, last at 59 fragments | 360 fragments, `model`. **The file written to prevent a stale retrieval number is carrying one** |

**W-4 is Stage 0** and is fixed by doing the work, not by editing the file. **W-1 to W-3 are three
sentences** and could be corrected in ten minutes — deliberately not done here, because a documentation
fix folded into a planning commit is the widening this folder forbids. They are logged so whoever picks
up Stage 0 fixes them in the same breath as the measurement, which is where they belong.

---

## 5. Decisions waiting on the owner

None of these blocks Stage 0. **All four block Stage 1**, and guessing any of them means rewriting the
ingester.

### Q-A — which documents go in first?

The first document decides the chunker's first test
([`02-implementation.md` §4.3](02-implementation.md)). Candidates, in the order they would most likely
earn their keep in Qatar MEP work: **QCS**, **ISO 19650**, **Ashghal BIM requirements**, a **company
standard**, or a **live project's documents**.

*Not neutral:* a standard with numbered clauses gives `locator` an obvious meaning and makes the
citation requirement testable on day one. A project folder of mixed PDFs does not.

### Q-B — does the store keep the file, or point at it?

If the store holds only chunks and a path, **moving or renaming the source breaks every citation**. If
it copies the file in, the store grows and a project's content is duplicated somewhere the user did not
choose.

*Relevant:* `heron_scope.py` already refuses to name a store after a file, *"because renaming the file
loses the knowledge"* — the same problem, already solved once for fragments. Whatever is chosen should
be consistent with that.

### Q-C — should Heron record success rate and recency of use?

[`01-requirements.md` R-16 and R-25](01-requirements.md). Two of the four ranking signals
[`05 §4.4`](../../../05-heron-brain.md) names **do not exist**, because nothing records them. Recording
them means Heron keeps a history of what was asked and what worked.

*Worth saying out loud:* that is a usage log. D-26 settled what may leave the machine; it did not
settle what is **kept on** it. This is the owner's call.

### Q-D — is cloud embedding actually wanted?

D-24 says local by default, **cloud opt-in per scope** — and there is no opt-in path and no per-scope
setting, so the choice cannot currently be made at all (R-30). Given D-26 allows project content in the
cloud, the opt-in may be worth building, or it may be a setting nobody will ever turn on.

*Cheapest honest answer if unsure:* leave it unbuilt and record the decision, rather than building a
switch with no user.

---

## 6. Log

### 2026-09-10 — track opened

**Done.** Read the RAG specification across [`05`](../../../05-heron-brain.md),
[`00b §19`](../../../00b-master-specification-agent-os.md), [`10`](../../../10-memory-and-knowledge.md),
[`19`](../../../19-context-and-cost.md), [`20`](../../../20-knowledge-trust-and-conflict.md) and the
registry rows in [`28`](../../../28-agent-registry.md). Read the code that claims those agent ids.
Ran the library, the backend, both retrieval suites and the tracked query. Wrote these three notes.

**The finding.** Nine of the seventeen RAG agents have code; eight have nothing — and the split is not
random. **Every table in the knowledge store is about fragments.** There is nowhere for a document to
go, so every requirement about standards, citations, company knowledge and provenance has nothing to
act on. The retrieval half of RAG is built and tested; the knowledge half has not started.

**The surprise.** `A7` is closed and the trained backend is live, but **it was never measured** — and
three separate files still describe a system where it has never run (§4). The one instrument for it is
a query that was itself retired as measuring the wrong layer.

**Not done, on purpose.** No code written. No document ingested. The four stale sentences in §4 left
in place. `retrieval-history.md` not touched.

**Next.** Stage 0 — [`02-implementation.md` §3](02-implementation.md). An hour, needs nothing, and it
closes W-1 to W-4 at the same time.

### 2026-09-10 — the shape decided: all six

**Asked.** Six structural questions — does it search again, does it follow relationships, does it know
a clause sits inside a section, who picks the scope, is ranking done by rules or by a model, and does
it answer from what it found or only find.

**Answered by the owner: all six.** Heron's RAG is the full shape, not a search box with a filter on
it. Written up as [`00-structure.md`](00-structure.md), which now sits ahead of the other three
because structure decides requirements and not the other way round.

**The finding that came out of writing it.** Two of the six are **not new machinery**. Because
[D-01](../../../DECISIONS.md) puts the host in charge of classifying the request and writing the
reply, *searching again* and *answering from what it found* are both about **what the brain reports**
— confidence, and a packet with citations in it. `heron_retrieve.py` already computes the confidence
numbers and discards them. So "all six" is a smaller build than it sounds, and the honest reason is
a decision taken months ago rather than anything clever.

**What moved in the plan.** A new **Stage 0b** — say the confidence out loud — placed early because
it needs nothing and every later measurement is read against it. And **hierarchy moved into Stage 1**,
because it is decided in the chunker: it is the one item on the whole list that gets expensive if it
is postponed. Twelve requirements added, **R-34 to R-45**.

**Not done, on purpose.** Nothing written to [DECISIONS.md](../../../DECISIONS.md) — a work note does
not get to record a decision on its own. It gets a number when it is written there.

> **And the gate caught this note trying to allocate one.** The first draft named the next free decision
> number, and `check-docs.py` failed it as **referenced but not defined**. It is right twice over: the
> decision is not recorded, and with another session committing to this repository today, 68 may not
> be free by the time it is. **A decision number is claimed by writing the decision, never by
> planning to.**

**Next.** Unchanged: Stage 0, then 0b. Neither needs Revit, the PC, or any of the four open questions
in §5 to be settled first.

### 2026-09-10 — outside repositories read, and one of them corrected us

**Asked.** Ajmal: read `github.com/jamwithai` and say whether anything there is useful for Heron —
the whole platform, not only RAG.

**Done.** Five repositories read. Written up as
[`../../investigations/jamwithai-repositories-2026-09-10.md`](../../investigations/jamwithai-repositories-2026-09-10.md),
sorted the way [`docs/34`](../../../34-patterns-adapted.md) sorts things: **four ✅ take, three ⏸
owner's call, the entire stack ❌ does not transfer.** Nothing accepted — every row is a proposal.

**The one worth having.** A fabrication check that calls **no model at all** — normalise both sides,
compare with `difflib`, threshold per kind of claim, and understating the source is allowed. It turns
[R-21](01-requirements.md) *no source, no claim* from a rule nobody can test into a number that can be
measured offline with the standard library.

**And it corrected this track.** Reading them sent us to [`34 §2.13`](../../../34-patterns-adapted.md),
which had **already measured a third retrieval stream at six settings and rejected it** — all six lost,
P@5 never improved, and the deciding property is **density**: median 50 neighbours per fragment, worst
230. [`00-structure.md` §3.2](00-structure.md) had proposed exactly that this morning, without checking.
It is now **conditional**: documents only, and gated on a neighbour count that costs nothing to run.

> **This is the second correction in one day made by this repository to itself**, and both came from a
> file somebody wrote so the next person would not repeat the work. The other was `check-docs.py`
> refusing a decision number nothing defines.

