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
| Stage 0 — measure and record | **DONE for `lexical`, BLOCKED for `model`.** [`retrieval-history.md`](../../../../brain/retrieval-history.md) now carries 360-fragment rows, both checkers run at 360, and the two backends compared at the same corpus size for the first time. **No `model` run was taken** — this container refuses `huggingface.co`, and the row says so rather than borrowing one |
| Stage 0b — say the confidence out loud | **DONE for the reporting half (R-34, R-35, R-61). NOT built: the acting half (R-56 to R-59).** `heron_retrieve.Contest`, `tests/test_contest.py`. The floor those need is **derived from a measurement or not set** (R-60), and the measurement says **not on this backend** — twelve questions, every column overlapping. **W-8** |
| Stage 1 — a document can go in | **DONE 2026-09-11.** `documents` and `chunks` tables, [`brain/heron_ingest.py`](../../../../brain/heron_ingest.py), hierarchy at arbitrary depth, the heading path, the rule-and-exception split, and `--boundaries` for a person to read. **PDF is the one format that needs an optional reader** — everything else is standard library |
| Stage 2 — a document comes back out | **DONE 2026-09-11.** `find_documents()`, a chunk FTS index, chunk vectors, and the first document measurement. **Alongside fragments, never fused with them** |
| Stage 3 — citation, refusal, fabrication check | **DONE 2026-09-11, with R-45 part-done and saying which part.** `heron_ground.py`, the guard, citations bound to the exact chunk |
| Stage 4 — the Librarian picks the scope | **DONE 2026-09-11.** `heron_retrieve.librarian()` — one query per scope, two labelled answers, nothing pooled |
| Stage 5 — document nodes, and the density count | **COUNTED 2026-09-11.** Edges derived, density an order of magnitude below the fragment graph, **and the route still has no weight** |
| Stage 6 — maintenance | **DONE 2026-09-11.** Re-index on change by content hash, duplicate clauses at write time, and the host rebuilds both halves |
| Stage 7 — the re-ranker | **HALF DONE 2026-09-11, and the half that is done is the one this machine can prove.** [`brain/heron_rerank.py`](../../../../brain/heron_rerank.py) is the seam, bounded to twenty pairs in one place; `tests/test_rerank.py` asserts that with nothing installed the shortlist comes back in *exactly* fusion's order, and the tool says `Re-rank: absent` out loud. **The after-measurement is NOT taken** — no cross-encoder has ever run here, because the weights need `huggingface.co`. **W-9**, and `A10` in [NEEDS-CHECKING.md](../../../NEEDS-CHECKING.md) |
| Stage 8 — trust and conflict | **not started.** Needs Stage 3, which is done. Q-C is the decision it is actually waiting on |
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
| **W-1** | [`brain/README.md`](../../../../brain/README.md), the `heron_embed.py` row | the `model` backend *"has never run (`A7`)"* | it runs. `python brain/heron_embed.py` prints `Backend: model` — **on a machine that can fetch the weights.** ✅ **CLOSED 2026-09-11**, and rewritten to say **when it runs and when it does not**, because *"it runs"* flat is the same kind of environment-free sentence as the one it replaces |
| **W-2** | [`brain/README.md`](../../../../brain/README.md), the `retrieval-history.md` row | the file *"currently records the built-in backend collapsing as the library grows, which is the evidence for `A7`"* | `A7` is closed. The row describes the evidence for a thing that has already happened. ✅ **CLOSED 2026-09-11.** And *"collapsing"* was the wrong word: measured at 360, the tracked fragment sits at **118th of 360 by words** against **17th of 59** — it is **sinking in proportion**, not falling out |
| **W-3** | [`tests/test_embed.py`](../../../../tests/test_embed.py), closing text | *"it could not be tested here, because this container's network refuses huggingface.co. A7 in NEEDS-CHECKING.md is that run"* | the suite **just ran on the model backend** and printed `Backend in use: model` eight lines above this sentence. ✅ **CLOSED 2026-09-11 — and not by swapping one fixed claim for another.** The sentence was **false** on 2026-09-10 and is **true again** in the container it was corrected in, so it now **reports which backend answered THIS run**. A sentence that flips with the network is a guess about the machine, not a finding |
| **W-4** | [`brain/retrieval-history.md`](../../../../brain/retrieval-history.md) | eleven rows, every one `lexical`, last at 59 fragments | 360 fragments, `model`. **The file written to prevent a stale retrieval number is carrying one.** ⚠️ **HALF CLOSED 2026-09-11.** The corpus size is fixed — rows at 360, both checkers run there, and the two backends compared at one size. **The `model` row is still owed** and cannot be taken where `huggingface.co` is blocked. **It is not borrowed from §3 and re-presented as today's run** |
| **W-5** | [`brain/README.md`](../../../../brain/README.md), the dependency table | `pyyaml` is the whole list | **`model2vec` is used too** — it is what makes `Backend: model` work. Somebody following the instructions exactly installs `pyyaml`, gets the weaker backend, and is told nothing |
| **W-7** | This plan itself, about ten times | `QCS 2014 §21.3.2 Insulation`, `Section 21 Mechanical` | **Invented as an illustration and never verified.** Whether QCS Section 21 is the mechanical section is not known here. **A plan about not fabricating clause numbers, fabricating a clause number** — left visible, flagged in [`00-structure.md` §3.3](00-structure.md), and replaced when Q-A names the real section |
| **W-6** | The repository has **no `requirements.txt`, no `pyproject.toml`, no `setup.py`** | — | [`tools/setup.ps1`](../../../../tools/setup.ps1) builds and deploys the **add-in** and installs no Python package at all. [`docs/07`](../../../07-installation-and-update.md) specifies an installer that *"checks required dependencies"* and a Dependency Agent that *"check[s] and install[s]"* them — **designed, not built.** So the Python half of Heron is installed by hand, from a list that is wrong (W-5) |

| **W-8** | The floor that [R-56](01-requirements.md) and R-58 stand on | Stage 0b would *"drop candidates with no claim"* and *"refuse a question nothing covers"*, needing nothing | **Found 2026-09-11 by building the measurement first, which is why it was built first.** [R-60](01-requirements.md) says the floor is derived from that measurement and from nothing else — and **at 360 fragments on `lexical` the measurement does not separate a BIM question from a question about cats.** Twelve questions, **every column overlaps**: *"how do I bake sourdough bread"* has the widest winning gap of all twelve. **Reciprocal rank fusion keeps order and discards strength**, so the fused score never could; and the two surviving magnitudes fail because `heron_embed`'s own docstring says the built-in backend **"IS NOT MEANING"**. **So R-56 to R-59 are blocked on the `model` backend, which is blocked on the network.** Numbers in [`retrieval-history.md`](../../../../brain/retrieval-history.md) |

| **W-9** | Stage 7's own done-when, in [`02-implementation.md` §10](02-implementation.md) | a before-and-after measurement exists at the same corpus size | **only the before exists.** The seam is built and the absent half is measured — at 360 fragments the tracked question comes back at **0.0246 with a 2.1-rank lead, identical to the Stage 0b run**, so the seam is provably inert when nothing is installed. **No cross-encoder has ever run in this repository.** `huggingface.co` answers `CONNECT tunnel failed, response 403` from this container (re-checked 2026-09-11) while `pypi.org` answers 200, so it is that host's policy rather than a broken network — the same block [`heron_embed.py`](../../../../brain/heron_embed.py) recorded on 2026-08-28. **Nothing was estimated to fill the gap and no stub's number was written down as a result.** `A10` is the run |

**W-5 and W-6 were found on 2026-09-10 by the owner asking a question** — *does a new person
installing from GitHub get this automatically?* The answer is that the **add-in half installs itself
and the Python half does not**, and this track makes it sharper rather than causing it: the plan adds
**optional** packages (the re-ranker, possibly a PDF reader), and every one of them degrades silently
when absent. **Stage 7 made that concrete on 2026-09-11**: `sentence-transformers` is the third
optional package and the first one that **announces itself before it downloads** — 500 MB to 2 GB,
said before rather than during. W-5's table still does not mention it, and deliberately: W-5 belongs
to the install thread, and fixing it inside Stage 7's commit is the widening this folder forbids. **Silent degradation plus an install list nobody can follow is how a user ends up on the
weaker backend permanently.** W-6 is the gap; a dependency manifest would close both.

**W-4 is Stage 0** and is fixed by doing the work, not by editing the file. **W-1 to W-3 are three
sentences** and could be corrected in ten minutes — deliberately not done here, because a documentation
fix folded into a planning commit is the widening this folder forbids. They are logged so whoever picks
up Stage 0 fixes them in the same breath as the measurement, which is where they belong.

> **Closed 2026-09-11, in that same breath, exactly as this paragraph asked.** W-1, W-2 and W-3 are
> corrected; W-4 is half closed and says which half. **W-5, W-6 and W-7 are untouched** — W-5 and W-6
> belong to the install thread (R-71 to R-79) and W-7 waits on Q-A naming a real clause. Closing them
> here would be the widening this folder forbids, in the commit that closed the rest.

---

## 5. Decisions waiting on the owner

None of these blocks Stage 0. **All four block Stage 1**, and guessing any of them means rewriting the
ingester.

### Q-A — which documents go in first? ✅ **ANSWERED 2026-09-11**

> **Everything is kept — QCS, NFPA, ASHRAE, Ashghal, company, project. Only the ORDER was in
> question, and the owner set it by an argument this note did not have.**
>
> **His argument:** *"NFPA and QCS, that kind of world data the AI can see easily. Company and
> project data it cannot."* **He is right, and it reorders the library:**
>
> | | AI already knows it | Worth indexing |
> |---|---|---|
> | Project documents | nothing | **highest** |
> | Company standards | nothing | **highest** |
> | QCS / Ashghal | badly — regional and thin | medium |
> | NFPA / ASHRAE | reasonably, in general terms | **lowest** |
>
> **The rule that falls out of it:** *index what the model cannot know.* **With one exception that
> justifies indexing a standard the model half-knows: the model knows the TOPIC and invents the
> CLAUSE NUMBER.** This session produced its own proof — `§21.3.2` was fabricated here and looked
> entirely real ([W-7](#4-defects-found-while-planning--recorded-not-fixed)). Editions compound it:
> QCS 2014 against 2010, NFPA 13 2022 against 2019, blended in memory while a contract names one.
>
> **And the owner's second argument settles keeping them at all:** *"if we keep it, it will improve
> the speed."* Correct, and it is not only speed — a local clause is **milliseconds**, it is the
> **right edition**, and it sends **one clause to the cloud instead of a document**.
>
> **First document: whichever numbered document he can hand over first** — a company standard if one
> is numbered, otherwise one QCS section. **Numbered is the only hard condition**, because clause
> numbers are what make citations testable on day one.


The first document decides the chunker's first test
([`02-implementation.md` §4.3](02-implementation.md)). Candidates, in the order they would most likely
earn their keep in Qatar MEP work: **QCS**, **ISO 19650**, **Ashghal BIM requirements**, a **company
standard**, or a **live project's documents**.

*Not neutral:* a standard with numbered clauses gives `locator` an obvious meaning and makes the
citation requirement testable on day one. A project folder of mixed PDFs does not.

### Q-B — does the store keep the file, or point at it? ✅ **ANSWERED 2026-09-11 — point at it**

> **The file is never copied.** The chunks hold the text, so a citation still reads correctly after
> the file moves; only the convenience of opening it breaks, and Heron says *the source file has
> moved* rather than failing. Copying would duplicate a client's content into `%APPDATA%` where
> nobody chose to put it — and, with a licensed standard, would make every copy of the store a
> redistribution. In the schema at
> [`02-implementation.md` §4.1](02-implementation.md).


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
[`../../investigations/rag-engineering-practices-2026-09-10.md`](../../investigations/rag-engineering-practices-2026-09-10.md),
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

### 2026-09-10 — the fabrication check taken, and folded in

**Owner's instruction:** *"take the fabrication check, add to plan."* Done the same day.

**What was taken.** A way to check a claim against its source that **calls no model and touches no
network** — normalise both sides identically, compare with the standard library, threshold per kind of
claim, check only sentences that carry a fact, and **let a claim that says less than its source pass**.

**Why it matters here.** [R-21](01-requirements.md) — *no source, no claim* — has been the most
credibility-carrying rule in the whole track and the least testable. It is now **ten requirements with a
build order**: R-46 to R-55, [`01-requirements.md` §H](01-requirements.md), built as
[`02-implementation.md` §6.1](02-implementation.md).

**The design decision that made it fit.** Heron does not write the answer — [D-01](../../../DECISIONS.md)
gives that to the host — so at first sight the brain cannot check one. It can:
[`heron_validate.py`](../../../../brain/heron_validate.py) already **gathers the evidence for a proof and
never signs one**. This is that shape one layer up — **the brain cannot write the answer, but it can
refuse to endorse one** — so the check is a call the host makes back into the brain, returning a report
and never a rewrite. It stands on `HERON-RAG-CIT-014`, whose registry line has read *"No source, no
claim"* since it was written and which has never had any code.

**What was re-authored rather than copied** ([D-25](../../../DECISIONS.md)): the normalisation list. Their
corpus varied in frequency words and numeric suffixes; **BIM text varies in units and clause numbers** —
`150mm` / `150 mm`, `DN150` / `150Ø`, `§21.3.2` / `21.3.2`. That list is Heron's own and is the part most
likely to decide whether the check is trusted.

**The row to get right, flagged now rather than found later:** R-51. A modeller who writes *"the duct
needs insulation"* about a clause saying *"insulated to 25mm"* has said something true and less specific.
**A checker that calls that a fabrication will be switched off within a week**, so its test is written
first.

**Not done, on purpose.** Nothing written into [`docs/34`](../../../34-patterns-adapted.md). Its markers
are about reality rather than intent, and its §1 tally describes one closed programme of sixteen
repositories. The entry belongs there as `✅ BUILT` when §6.1 exists.

**Still no code.** Stage 0 and 0b remain next.

### 2026-09-10 — the other three taken, and they turned out to be one thing

**Owner's instruction:** *"take the other three also, add to plan."* Done the same day as §H.

**What was taken.** The item-level citation, the out-of-domain refusal, and grading what came back —
**R-56 to R-65**, [`01-requirements.md` §I](01-requirements.md), designed in
[`00-structure.md` §3.7](00-structure.md) and built as [`02-implementation.md` §3a](02-implementation.md).

**The finding that writing them up produced.** They are not three features. Two of them, plus the
confidence work already taken as §3.1, rest on **one question asked at three strengths**:

> *Does this candidate have a real claim on the sentence that was asked?*

Report it, act on it, refuse on it. **One measurement, three thresholds.** Built as three mechanisms
they would produce three numbers that disagree, and the disagreement would show up as a shortlist
claiming confidence about candidates it had also dropped.

**Two of the three need nothing and fix a defect already on record.** Stage 0b now covers them, and its
test case is already written: at 59 fragments
[`retrieval-history.md`](../../../../brain/retrieval-history.md) recorded that the top five for the
tracked duct question included `find-sheets` and two dimensioning fragments with **no claim on that
sentence at all**, two of which **write to the model**. Today's run at 360 has the same shape —
`CREATE_DUCT` at nearness #1 for a sentence beginning *"show me"*. **Dropping is what fixes that;
ranking never will.**

**The third is the quiet prerequisite of §H.** The fabrication check compares a claim against *the chunk
it cites*. Until a packet part carries the id of the exact chunk and that binding survives into the
draft, the comparison has no defined target. R-63 to R-65 now say so, and §6.1 says build them first.

**What was refused while taking them.** A keyword list of BIM words (R-60) — wrong the week it is
written. And a classifier inside `brain/` (R-62) — [D-01](../../../DECISIONS.md) gives classification to
the host, and `heron_context.py` already refuses to classify. **The brain says nothing here has a claim;
it never says you meant something else.**

**Still no code.** Twenty requirements added today across §H and §I, and the plan now has a Stage 0b
worth doing that needs neither Revit nor the PC.

### 2026-09-10 — the field read, and one technique turned out to be free

**Owner's instruction:** *"go to GitHub and find the best repos, and research on the internet too, not
only GitHub — what is it we are going to do. Research and update."*

**Done.** [LightRAG](https://github.com/HKUDS/LightRAG) and
[RAGFlow](https://github.com/infiniflow/ragflow) as named, plus Anthropic's contextual retrieval, IBM's
Docling, current chunking practice for regulated documents, CPU re-rankers and the embedded vector
stores. Written up as
[`../../investigations/rag-state-of-the-art-2026-09-10.md`](../../investigations/rag-state-of-the-art-2026-09-10.md).
**Folded in as R-66 to R-70**, [§J](01-requirements.md), designed in
[`00-structure.md` §3.8](00-structure.md).

**Five decisions survived it**, which is the more valuable half: D-23, D-01, hierarchical chunking,
clause-level chunking for standards, and the re-ranker being feasible on a CPU. **D-23 gained a number**
— `sqlite-vec`'s brute-force search stays fast below roughly **500,000 vectors**, and Heron is at 360.
That ceiling had never been written down.

**The finding worth having.** The published way to stop a chunk being embedded in isolation is to
prepend a model-generated summary of where it sits — **49% fewer retrieval failures, 67% with
re-ranking** — and it costs **one model call per chunk at index time**, which is precisely the cost
[D-24](../../../DECISIONS.md) exists to avoid. **For a numbered standard, the heading path does the same
job for nothing.** *QCS 2014 → Section 21 → 21.3 Ductwork → 21.3.2* is read off the document, not
generated, and §3.3 had already decided to store it. **The technique arrives without breaking the
decision.**

**The finding worth fearing.** A chunk split between a rule and its exception makes Heron state the
**opposite** of a requirement **with a citation attached** — more convincing than any uncited guess.
QCS and Ashghal are written as rule-then-qualification throughout, so this is the normal case here and
not an edge one. **R-68, and its test is written before the chunker.**

**What was refused.** LightRAG's graph — it needs a model to build it, and
[`34 §2.13`](../../../34-patterns-adapted.md) already measured the shape at six settings. RAGFlow's
stack — Elasticsearch, Redis, MySQL, MinIO, Docker, 16 GB RAM. And **RAGAS and the LLM-judge evaluation
frameworks**, which need a model per evaluation and whose correlation with human judgement is reported
at **0.55**. [`check-routing.py`](../../../../tools/check-routing.py) already reports precision@1 and
recall@3 with no judge at all, and with this morning's fabrication check that is a **fully
deterministic evaluation stack** the frameworks cannot match.

**Left to the owner: S-4** — take Docling or write the parser. **No default offered.** It does exactly
what Stage 1 needs and keeps documents on the machine; it is also by far the largest dependency this
repository would have taken, against a `brain/` that needs `pyyaml` and nothing else.

**Still no code.** Twenty-five requirements added today, R-46 to R-70.

### 2026-09-10 — installing it, and one package actually installed

**Owner's instruction, in two parts.** First: the installer should handle the Python side automatically
and the README should say so. Then, sharpened: **say what each package is FOR**, announce it **while**
installing, **check the version and not just presence**, and state the **size up front like system
requirements**. **R-78 and R-79 come from the second half and were not in the plan before he said them.**

**R-71 to R-79**, [§K](01-requirements.md), designed as [`00-structure.md` §3.9](00-structure.md).

**The sharp one is R-78.** *Installed* is not *correct*. `sqlite-vec` is at **0.1.9** — pre-1.0, where an
interface moving under a caller is normal. **A component that loads an old version and half-works is
worse than one that refuses**, because it fails the way this repository is built to prevent: quietly,
plausibly, and only in the case nobody tested. So the check reports **three** states — missing, present
but out of date, correct.

**And one package was installed, with his authorisation.** `sqlite-vec 0.1.9`, **0.3 MB**, per-user, no
administrator rights. It loads (`vec_version()` answers), and `test_embed`, `test_retrieve` and
`test_scope_store` all still exit 0. **[D-23](../../../DECISIONS.md) named it and the code has always
tried to load it** — until today it fell back to comparing vectors in Python, which is exactly the
silent degradation §3.9 exists to make visible.

**Measured, for R-79:** the whole installed Python side is **≈93 MB** — less than one Revit project
file. The re-ranker would add **500 MB – 2 GB**, a document parser several hundred more. **Those two are
the only large numbers in the entire plan**, and both are optional and both come last.

**Still no code written.**

### 2026-09-10 — the plan re-read, and four defects found in it

**Owner's instruction:** *"check the whole thing once more — is everything covered, do we need to update
or add anything."* Done, and it found four things. **All four were in work written today.**

| # | Found | Fixed |
|---|---|---|
| **1** | **The same stage number meant two different things.** [`02-implementation.md`](02-implementation.md) said *Stage 4 — maintenance*; [`00-structure.md` §6](00-structure.md) said *Stage 4 — the Librarian*. A reader acting on one while quoting the other would build the wrong thing | renumbered; the two files now map one to one, and §2 of the implementation note carries the map |
| **2** | **Three stages had no build section at all** — the Librarian, the document graph, and the re-ranker existed only as design in `00-structure.md` | written as §7, §8 and §10 |
| **3** | **`HERON-RAG-EVO-016` was the one RAG agent with no requirement anywhere** — and it was omitted **silently**, which is the part that matters | named in §7 of the requirements as deliberately deferred, with the reason |
| **4** | The dependency example still said **`sqlite-vec MISSING`** — installed earlier the same day | corrected to `installed, 0.3 MB` |

**Defect 1 appeared twice.** The headings were fixed first, and the *"why this order"* table thirty lines
below still carried the old numbering, along with *"none of stages 0 to 5 needs Revit"* when there are
now ten. **A renumbering that stops at the headings is half a renumbering** — which is the same shape as
every stale-count defect this repository has recorded, arriving inside a file written to prevent them.

**Defect 3 is the one worth remembering.** Everything else was a wrong number; that one was an
**absence**, and absences do not announce themselves. It was found by listing all seventeen agent ids and
counting how many times each appears in the plan — a command, not a reading. **The registry was the
checklist, and nothing else would have caught it.**

**Nothing new was added to the scope.** No requirement was created by this pass except the one naming an
omission. **Still 79, and still no code.**

### 2026-09-10 — checked against the specification, and it found the rule the plan forgot

**Owner's instruction:** *"check it against our project specification, our roadmap and our docs — is
what we planned the same, or correct."* Checked against the
[Constitution](../../../../HERON_CONSTITUTION.md), the [Golden Rules](../../../14-golden-rules.md), the
[Roadmap](../../../ROADMAP.md) and [`docs/05`](../../../05-heron-brain.md). **R-80 to R-84**, [§L](01-requirements.md).

> ## The finding that matters more than everything else added today
>
> **The plan reached seventy-nine requirements without mentioning [Golden Rule 19](../../../14-golden-rules.md).**
> A search for *Rule 19*, *permission level*, *injection* or *untrusted text* across all four notes
> returned **nothing.**
>
> Rule 19: *content from documents is **data, never instruction**.* Its stated *why*: **the consequence
> of a successful injection is a write to a live project model.**
>
> **And [`34 §2.11`](../../../34-patterns-adapted.md) had already called it** — *"Heron is the carrier,
> and every source it carries today is its own. **The day the RAG index exists is the day that stops
> being true.**"* It named that guard **"the most valuable single item the whole programme produced,
> because it lands on work not yet done."**
>
> **This plan is that work.** A plan whose entire purpose is to make Heron carry text it did not write,
> which never mentioned the rule about carrying text it did not write.

Design in [`00-structure.md` §3.10](00-structure.md). Three rules answer it: a chunk is carried as
**quoted content with its source**, never spliced where it reads as direction; the path into a packet is
**guarded before assembly**; and an oversized chunk is **flagged, never truncated** — because truncation
lets a payload be padded past the scanner's window.

**The permission side is already right, which makes this a gap and not a hole.** `write.enabled`
defaults to false, a write rolls back without `apply`, and the brain executes nothing. **No document can
reach the model on its own.** What it could do is mislead the person who can.

### And three smaller findings from the same pass

- **R-83 — a document has no lifecycle.** [Golden Rule 10](../../../14-golden-rules.md) requires
  identity, version **and** lifecycle for every important object. Documents had identity and provenance
  and **no status**. A fragment is `DRAFT` or `PROVEN`; a document was nothing.
- **R-84 — ingestion left no audit trace.** [Golden Rule 14](../../../14-golden-rules.md), and
  [`heron_audit.py`](../../../../brain/heron_audit.py) exists precisely so a request answered inside the
  brain still leaves one. Ingesting a document is an important autonomous operation and was leaving none.
- **The plan spans three roadmap phases and never said so.** Mostly **Phase 2** — but **Stage 3 is the
  foundation for Phase 7** (*"Standards department with citation enforcement"*) and **Stage 8 is
  Phase 4** (*"knowledge trust levels and conflict resolution"*, word for word). [`§6`](00-structure.md)
  now carries a phase column. **Without it somebody reads this as Phase 2 quietly growing to include a
  standards department**, and the roadmap's whole argument is that one thin slice beats a wide one.

**Three rules the plan was already keeping**, checked rather than assumed: GR 5 scope separation, GR 11
the derived index, and GR 7 — *one agent creates, another validates* — which is the exact shape of §H.

**What this pass proves about the earlier one.** The self-re-read four entries above found four defects
and **all four were wrong numbers or a collision**. This pass found an **absence of a safety rule**, and
no amount of re-reading the plan against itself would have found it. **A plan can only be checked
against something outside it.**

**Still no code. 84 requirements.**

### 2026-09-11 — the four decisions answered, and a fifth question that reordered the library

**All four blocking decisions taken.** Q-A, Q-B, S-2 and S-4 — the detail is in
[§5](#5-decisions-waiting-on-the-owner) and [`00-structure.md` §8](00-structure.md).

| | Answer |
|---|---|
| **Q-A** | Whichever **numbered** document he can hand over first. **Numbered is the only hard condition** |
| **Q-B** | **Point at the file. Never copy it.** In the schema |
| **S-2** | **Any depth, as `parent_id`.** One column instead of a guess |
| **S-4** | **Write the parser, and let one real PDF decide.** Docling only if it cannot cope |

**S-4 is the one worth reading twice.** It is not *"write it because dependencies are bad"* — it is
*write it, run it on one real section, and read the output.* 500 MB against a `brain/` that needs 0.7 MB
is not a change to make on a guess, and this repository already has the method: `34 §2.13` settled the
graph route with six measurements rather than an argument.

### And then he asked the question that reordered the library

> *"NFPA and QCS, that kind of world data the AI can see easily. Company and project data it cannot. So
> why do we need to keep NFPA-like files?"*

**He is right about priority and wrong about exclusion, and both halves matter.**

Right: **the value of indexing something is inversely proportional to how well the model already knows
it.** Project and company documents are invisible to it; NFPA and ASHRAE are half-known, licensed and
huge. **R-88.**

Wrong about exclusion, for one reason: **the model knows the topic and invents the clause number.** This
session produced its own proof — `QCS 2014 §21.3.2` was fabricated in this very plan, looked completely
real, and survived ten repetitions until it was checked ([W-7](#4-defects-found-while-planning--recorded-not-fixed)).
Editions compound it: QCS 2014 against 2010, NFPA 13 2022 against 2019, blended in memory while a
contract names exactly one.

**Then he settled it himself:** *"if we keep it, it will improve the speed."* Correct, and it is more
than speed — a local clause is **milliseconds**, it is the **right edition**, and it sends **one clause
to the cloud instead of a whole document**.

**So nothing is excluded. Only the order changed**, and the order is now a requirement rather than a
preference.

### The licensing thread this opened — R-85 to R-87

Raised by him in the same breath. **A licensed standard is not the same kind of object as a company
note**, and the difference only appears when a store is copied. On 2026-09-10 this session suggested
*"copy `company.db` to the shared drive"* as the way to share company knowledge — **which is also
exactly how a bought standard reaches twenty people who did not buy it.**

[`34 §2.12`](../../../34-patterns-adapted.md) is an open item called *"an inventory of what imported
knowledge permits."* **This is that item arriving**, from the owner rather than from the research.

**88 requirements. Still no code.**

### 2026-09-11 — the store was protected and the source was not

**Owner's question:** *"NFPA or licensed files — can we push them, or give them to people? Would GitHub
pushing be an issue? Or are we not pushing those files?"*

**Checked rather than reassured, and he was right to ask.** Two protections already existed and one did
not.

| | |
|---|---|
| ✅ The knowledge store lives in `%APPDATA%\Heron\knowledge` | outside the repository entirely |
| ✅ `.gitignore` blocks `*.db`, `*.sqlite`, `/data/`, `*.rvt`, `*.rfa` | under a heading that reads *"NEVER COMMIT — client data, project knowledge, models"* |
| ❌ **`*.pdf` and `*.docx` were not blocked** | so the **document a store was built FROM** could be committed by one `git add .` |

**The store was protected and the source was not — and the source is the licensed half.** NFPA and
ASHRAE are bought per seat; a client specification is theirs. `.gitignore` opens by saying this
repository becomes public and that *"anything committed here is permanent: forks propagate and GitHub
caches"*, which makes this the kind of mistake that only has to happen once.

**Fixed, and proved rather than asserted.** A file named `QCS-Section-21.pdf` was created in the
repository root; `git status` did not list it, and `git check-ignore -v` named the rule that caught it:

```text
.gitignore:33:*.pdf     QCS-Section-21.pdf
```

The test file was then removed. **`.pdf`, `.docx`, `.doc`, `.xlsx`, `.xls`, `.pptx` and `.ppt` are now
ignored**, with an explicit exception available the way `tests/models/` already has one.

**This closes the practical half of R-87.** The rule still belongs in the requirements — a file layout
that happens to be safe is not the same as a stated rule — but the leak it described is now shut.

### 2026-09-11 — the person putting the document in is a modeller

**Owner:** *"a new installer is like a modeller — they don't know where we need to keep the knowledge.
So inform them where it needs to be kept, how it will be, and how safe it is."* And separately: **BIM
standards are a document kind too.** **R-89 to R-93**, [§N](01-requirements.md).

**Everything written in this plan before today assumes a reader who knows what a scope is.**
[Golden Rule 1](../../../14-golden-rules.md) says the opposite: *the user focuses on BIM, Heron handles
the technical complexity*, and [`docs/01`](../../../01-vision-and-principles.md) says they should never
need to understand vector databases, embeddings or RAG. **The plan had no requirement covering the one
screen it adds.**

**R-92 is the one that was already true and never said.** Deleting everything Heron stored leaves every
original file untouched — the index is derived ([GR 11](../../../14-golden-rules.md)). **It is the
strongest safety promise this system can make, and nothing says it to the person who needs to hear it.**

**And R-93 puts BIM standards in**, where [R-88](01-requirements.md) ranks a **company** BIM standard
above an international one: the model knows nothing about it, it carries no licence problem, and it is
what a modeller opens daily.

### The clause travels; the file does not

He then asked directly whether this means nothing goes to the cloud. **It does not mean that, and the
answer was given precisely rather than reassuringly**, which is what R-91 exists to enforce:

| | Leaves the machine |
|---|---|
| The document file | **never** |
| The **one clause** Heron quotes to answer a question | **yes — with the answer** |
| The rest of the document, and the store | never |

**Because the host writes the answer** ([D-01](../../../DECISIONS.md)) and the host is in the cloud. The
brain finds the clause locally and hands over **that clause**, which is precisely the mechanism that
stops a clause number being invented.

**This is [D-26](../../../DECISIONS.md), already decided by him** — the model file never leaves,
content is acceptable — and it is enforced by R-10, where the ingester refuses `.rvt` and `.rfa` by
extension rather than by the user remembering.

**The one option not taken:** a scope marked confidential, which would **refuse to answer** rather than
send a clause. Named to him, not built, and not proposed — it is cheap now and expensive later, so it is
recorded here rather than left in a conversation.

**93 requirements. Still no code.**

### 2026-09-11 — two corrections from the owner, and the second one unblocked Stage 1

**He corrected R-88's ordering.** This note had *company documents first*, on the argument that the
model knows least about them. He said **BIM and ISO standards first, company last** — and he is right,
because **the two are not the same question:**

| Question | Order |
|---|---|
| What is worth most **to one user**? | project → company → regional → international |
| **What should Heron know out of the box?** | **BIM/ISO → regional → company** |

A company standard helps one company. ISO 19650 helps every user of the product, and does not change
per project. **The first framing was about one person's value; his is about the product, and the product
one governs.** R-88 now carries both, and says which wins.

**With one thing he did not say and which follows immediately: R-88a.** ISO sells ISO 19650 the way NFPA
sells NFPA. *"First priority to load"* and *"ships in the box"* are different sentences and only the
first is true — **no standard is ever bundled with Heron.**

### And the correction that actually unblocked the work

> *"This is the RAG engine we are making, am I right? Not started with the real documents, am I right?"*

**Yes, and it changes what Stage 1 needs.** The plan had drifted into treating *the first document* as
*the start of the library*, which made Stage 1 wait on the owner finding a real company standard, a QCS
section, or a licensed copy of something.

**It does not.** Stage 1 proves that chunking, hierarchy, the heading path, the rule-and-exception split
and the citation all work. **For that it needs one numbered, structured document — any one.** The real
library is loaded afterwards, in the order R-88 now sets, by whoever owns each document.

**R-88b.** Nothing on this track is blocked on the owner any more.

**95 requirement rows — R-01 to R-93, plus R-88a and R-88b. Still no code.**

### 2026-09-11 — a file named for a person, and a thanks that was missing

**Owner:** *"keeping the repo or person name is not good, I think. Also if you are referring to anything
from any repo, mention it in the README, like a thanks section."*

**Both right, and the second is the more important one.**

**The rename.** `jamwithai-repositories-2026-09-10.md` is now
[`rag-engineering-practices-2026-09-10.md`](../../investigations/rag-engineering-practices-2026-09-10.md).
**A file should be named for what is in it.** This repository goes public, and a page titled after
somebody's account reads as being *about them* rather than about the two ideas taken from their work.
Nine references updated; the sources are still named inside, which is where attribution belongs.

**The thanks.** [`README.md`](../../../../README.md) had **no acknowledgements section at all** — after
sixteen repositories were read at file level for [`33`](../../../33-external-repository-research.md) and
[`34`](../../../34-patterns-adapted.md), and five more this week. **Every source was credited in the
research documents and none of it was visible from the front page.**

That is not only manners. [D-25](../../../DECISIONS.md) — *studied and re-authored, never imported* — is
a claim about how this project treats other people's work, and **a claim like that belongs where a
stranger can check it**, next to the list of what was actually read.

**The section names only the projects that changed something**, and points at `33` and `34` for the full
sixteen with what each became — including the rejections, because a rejection with a number is worth as
much as an adoption.

### 2026-09-11 — the first code, and the measurement that stopped half of it

**Stage 0 and the reporting half of Stage 0b.** The first code this track has produced.

**What ran, and what it said.**

```bash
python brain/heron_scope.py --rebuild                 # 360 fragments
python brain/heron_embed.py                           # Backend: lexical
python brain/heron_fragment.py                        # 198 PROVEN
python tools/check-routing.py                         # exit 0, first run at 360
python tools/check-intrusion.py                       # exit 0, first run at 360
```

**Stage 0 is done for one backend and blocked for the other, and the blocked half is not faked.**
`huggingface.co` is refused by this container — the proxy answers **403 to `CONNECT`**, which is the
**same block [`heron_embed.py`](../../../../brain/heron_embed.py) recorded on 2026-08-28**, still in
force. `model2vec` is not installed here either and installing it would not help: the weights come
from the host that is blocked. **So no `model` measurement was taken, and the 2026-09-10 numbers in
§3 above were not copied into `retrieval-history.md` as though they were today's.**

> **What came out of measuring `lexical` at 360 anyway — and it is the thing §3.2 said nobody had.**
>
> §3.2 says two variables moved at once between the last recorded row and today: **the corpus went
> 59 → 360 AND the backend went `lexical` → `model`**, and no honest reading of that pair separates
> them. Measuring the **old backend at the new size** separates them, and costs nothing:
>
> | Route | `lexical` at 360 | `model` at 360 | |
> |---|---|---|---|
> | words | 118th of 360 | outside the first 100 | **the same, and it must be** — the words route never touches the embedding backend, so two runs agreeing where they cannot differ is a small check on both |
> | nearness | **194th of 360** | **19th** | **the trained encoder, measured against its own alternative at one corpus size for the first time** |
>
> **The words route's decline is corpus size. The nearness route's improvement is the backend.**
> 17th of 59 is 29% of the library; 118th of 360 is 33%. It is sinking in proportion rather than
> falling out.

**Stage 0b was built as far as the evidence allows, and no further.**
[`heron_retrieve.Contest`](../../../../brain/heron_retrieve.py) reports the winner's lead and the
shortlist's spread **in units of one rank of fusion**, how many candidates both routes found, the
small-pool caveat (**R-61**, which was already a comment inside `find()` and is now part of the
measurement rather than a second copy of it), and the **two magnitudes fusion discards** — FTS5's bm25
and the raw nearness score, neither of which anything ranks on. `tests/test_contest.py`, 18 checks.

**The one comparison it makes is not a dial.** A gap under **one** rank is a gap narrower than the
quality nudge, so the order **could have been set by the fragments' status** rather than by either
route preferring one. That is arithmetic, and the test asserts it as arithmetic — the way the test
that caught the nudge being eight times too big did.

> ## And then the measurement stopped R-56 to R-59, which is why it was built first
>
> The plan is explicit that **report it, act on it and refuse on it are one measurement at three
> thresholds** ([`00-structure.md` §3.7](00-structure.md)), and that **R-60**'s floor is derived from
> that measurement and from nothing else. **So the measurement was built first, and then it was
> read.** Twelve questions at 360 fragments — six about BIM, six with no BIM content at all:
>
> | | winner's lead | words matched | best bm25 | best nearness |
> |---|---|---|---|---|
> | six BIM | 2.1 – 8.1 ranks | 265 – 360 | −4.27 – −10.37 | 0.19 – 0.59 |
> | six not BIM | 0.9 – 29.9 ranks | 245 – 360 | −0.00 – −6.59 | 0.15 – 0.47 |
>
> **Every column overlaps.** *"How do I bake sourdough bread"* has the **widest winning gap of all
> twelve**. *"What is the best food for a cat"* scores **0.0254** where the tracked duct question
> scores **0.0246** — the cat question wins. **A floor anywhere here cuts a real question to reach an
> unreal one.**
>
> **And the reason is not bad luck.** Reciprocal rank fusion keeps **order** and throws **strength**
> away by construction, so the fused score never could have carried this. The other two columns fail
> for a reason `heron_embed.py` states about itself in capital letters: the built-in backend **"IS NOT
> MEANING"**. Asking it to tell a duct from a cat asks it for the one thing it says it cannot do.
>
> **So the floor is not set. That is a result, not a postponement** — and it belongs on the `model`
> backend, where nearness *is* meaning. **W-8.**

**A first answer was published inside this session and then withdrawn by measuring more.** At three
questions the words route looked selective for real questions and not for unreal ones, and that was
written into the code as a promising shape. At twelve it is not true. **Three questions is an
observation; twelve is the measurement**, and the docstring now says the opposite of what it said an
hour earlier. Recorded because a session that reports only its final answer hides the fact that the
first one was wrong.

**What was refused.** No keyword list of BIM words (**R-60**). No classifier in `brain/` (**R-62**).
No floor chosen because it happened to sort twelve questions nicely (**R-55**). And **nothing drops
and nothing refuses** — `tests/test_contest.py` check 6 asserts that **absence**, so adding it later
takes a deliberate hand rather than a quiet edit.

**Found while working, and fixed rather than recorded, because this change caused it.** Adding a test
suite made a stated count wrong in [`README.md`](../../../../README.md) — it said *"41 test suites"* —
and made a dated count in [`HANDOVER.md`](../../../HANDOVER.md) read as a current one. `check-docs.py` caught both.
[The execution record](../housekeeping-execution-record.md) **quotes** the old README line, so it is
marked as a quotation instead of rewritten — **rewriting a record of what a file said is how a record
stops being one.**

**Next.** Stage 1 — the `documents` and `chunks` tables and `brain/heron_ingest.py`
([`02-implementation.md` §4](02-implementation.md)). **It is blocked by none of this**: no network, no
model, no Revit. R-56 to R-59 wait for a machine that can fetch weights, and the *before* they will be
read against is now recorded.

### 2026-09-11 — Stage 1: the half of the store that did not exist

**`documents` and `chunks`, and [`brain/heron_ingest.py`](../../../../brain/heron_ingest.py).**
Every table beside these two was about Heron's own code library, so every requirement about
citations, standards and provenance had nothing to act on. It has something now.

**Closes R-05 to R-12, R-36, R-37, R-66 to R-70, R-82 and R-84.** `tests/test_ingest.py`, 63 checks.

**Three are closed only in part, and say which part**: R-06 (PDF needs an optional reader), R-80
(marked, and the guard that reads the mark is Stage 2), and **R-83** — a document now has identity and
a lifecycle, but **two versions of one document are not linked**, so *"which edition is this clause
from?"* is answerable and *"what did it say before?"* is not.

**The two tests were written before the chunker, in the order the plan set.**

- **R-68 first.** A rule and its exception never land in different chunks. Five qualifiers were named
  in the plan — *except, unless, provided that, save that, however* — and four more were added while
  writing it: *other than, save where, save as, but not*. **When no legal split point exists the chunk
  stays oversized and is flagged.** Refusing to cut is a real outcome, not a failure.
- **R-08 second.** Five token shapes survive whole. The test also asserts **the text really was cut
  somewhere**, because otherwise it passes by splitting nothing — which is the shape of a test that
  proves its own subject never ran.

**What the design settled, and why each is not a guess.**

| | |
|---|---|
| **Parent from the numbering** | `4.1.1`'s parent is `4.1` **because the document numbered it that way**, and only if that fails does it fall back to the heading stack. The document is better evidence than the position |
| **Title from the document** | not from the filename. `heron_scope.py` already refuses to name a store after a file *"because renaming the file loses the knowledge"* — and a citation reading `qcs-sec-21-final-v3-USE-THIS` is not one a person can check against a printed standard |
| **`.docx` needs nothing** | it is a zip of XML, and its `Heading 2` styles are exactly the structure R-66 wants |
| **PDF is the one exception** | font encodings and compressed streams cannot be standard library. It is **optional and loud**, the contract `heron_embed.py` already honours: absent means a smaller Heron, never a broken one, and it names what would fix it |

> **`MAX_CHARS = 2000` is the one number here that was chosen rather than derived, and it says so in
> the file.** Nearly every chunk is decided by structure — a clause is a chunk because the document
> says so. The limit exists so one unnumbered block does not become a chapter-sized chunk, and every
> chunk it creates records `split_by = "length"` so the risky ones can be **listed rather than
> guessed at**. It is one of the numbers to check when the first real document is read.

**Four defects found by running the thing, not by reading it.** All four were in code written in
this batch.

1. **The command was broken while every test passed.** `main()` upper-cased the scope name against
   `heron_scope`'s lowercase constants, so **every CLI call died** — and all thirty-odd checks were
   green, because they call `ingest()` and never the command. **A test now calls `main()` too.**
2. **Heading-only chunks were stored empty.** *"Section 4 Mechanical Works"* often has no prose of its
   own; the row must exist for its children to point at. An empty chunk is one **retrieval can
   return**, and returning nothing while looking like an answer is the failure this whole plan is
   about. It now holds its own heading as its text.
3. **The splitter took three tries, and only measuring found the second two.** It was **recursive**,
   so a long document ended on `RecursionError` rather than on a chunk. Made iterative, it re-scanned
   everything still to come at every cut — **quadratic**, and measured rather than suspected:

   | | before | after |
   |---|---|---|
   | 102 KB | 0.25 s | 0.01 s |
   | 408 KB | 3.8 s | 0.05 s |
   | 1.6 MB | **60 s** | **0.26 s** |

   It now scans a window of the next chunk's worth of text. **The window runs past the limit by a
   margin on purpose** — a token straddling its edge must still be seen whole, or the fix for the
   speed would have broken R-08 silently. **The test walks a token and a qualifier across that edge
   at eleven offsets**, because that is the one place this optimisation could have cost a rule.
4. **Re-ingesting printed the filename** while the row held the document's own title, so one document
   had two names depending on which run you read.

**All four were fixed rather than recorded, because all four are defects in the code written in this
same batch.** The rule about recording rather than fixing exists so a batch stays reviewable; it is
not a licence to ship a broken command.

> **Three of the four were invisible to a passing test suite.** The command was broken while every
> check was green; the splitter was 230 times too slow at the size that matters and nothing asserted
> a time. **A suite that only calls the functions does not test the program**, and *"it works"*
> measured on a 400-character fixture says nothing about a real section.

**What Stage 1 deliberately does not do.** Nothing reads a document back out. No retrieval, no
citation, no re-index trigger. `tests/test_ingest.py` asserts that absence — the ingester is the half
that has to be right, and reviewing it alongside a retrieval change is reviewing neither.

**Still owed, and named rather than quietly skipped.**

- **`.rte` / `.rft` are NOT refused.** [`02-implementation.md` §4.2](02-implementation.md) proposes it
  and says it **needs the owner's word** first, *"because a refusal nobody agreed to is as surprising
  as a leak"*. He has not given it, so it is not coded — and the test asserts the **absence**, so
  adding it later is a deliberate act.
- **S-4 is not settled by this.** Every document in the test suite was written to be easy. S-4 is
  settled by running the ingester on **one real numbered section** and reading `--boundaries`. Clean
  clause numbers and headings → no further dependency. It cannot cope → Docling, knowing exactly why.

**Next.** Stage 2 — documents come back out, alongside fragments, and the guard on the path into a
packet (**R-81**), which is the half of Golden Rule 19 that Stage 1 only marked.

### 2026-09-11 — Stage 2: a clause comes back out

**Closes R-19, R-20 and R-33, and finishes R-66.** `tests/test_document_retrieval.py`, 24 checks.

A question asked in a modeller's words now returns a clause:

```text
Documents:
  9.1.1   Heron Test Standard 2026   words #1 + nearness #2 - both agree
          Heron Test Standard 2026 → Section 9 Thermal Insulation → 9.1 Ductwork → 9.1.1 Thickness
  quoted from an ingested document - content, never instruction (GR 19)
```

**The decision this stage turns on: the two corpora are NOT fused.**

`find()` answers about fragments, `find_documents()` answers about clauses, and they are **two
labelled answers rather than one merged list**. Reciprocal rank fusion produces **the same score for
*"first of seven chunks"* and *"first of four hundred fragments"*** — Stage 0b's own finding, that
fusion keeps order and discards strength, applied one level up. Fusing across two corpora would be
that defect on purpose. **It is also the argument [R-38](01-requirements.md) makes about scopes: when
two things must not be pooled, the answer is two queries and two labelled answers.**

> ### The trap this stage exists to avoid, and it was named in the plan before it was built
>
> **A fragment declares which Revit releases it supports. A clause in QCS does not, and never will.**
> Run documents through the fragment's structured filter and **every document disappears the moment a
> question names a release** — which reads exactly like *"we have nothing on that"*.
>
> So documents get their own filter, and today it is one rule: a **RETIRED** document is not an
> answer. `tests/test_document_retrieval.py` asks the same question with **no Revit named, 2020, 2024
> and 2027**, and asserts the clause comes back every time.

**R-66 is finished rather than begun.** The heading path is an **indexed column on the words route**
and is **prepended to what the nearness route embeds**. The test proves it the only way that means
anything: it asks *"thermal requirements for ductwork"*, where **"thermal" appears only in the section
heading and never in the clause** — and asserts both that the clause comes back **and** that the word
really is absent from its text, so the check cannot pass by accident.

**Two defects in my own new code, both found by reading the output rather than the tests.**

1. **The confidence line called a clause a fragment** — *"5 fragment(s) were eligible"* about five
   clauses. Wrong in the one word a reader uses to tell the two corpora apart. `Contest` now takes
   the noun.
2. **It explained a tie with a mechanism that was not running.** The coin-toss sentence says the order
   *"could have come from status alone"*, which is true on the fragment side because of the quality
   nudge — and **documents get no nudge**, deliberately: a `DRAFT` clause is not a worse answer than a
   `REVIEWED` one, it is an **unread** one. Borrowing that number would have been borrowing a meaning.

**The first document measurement, and its caveats are bigger than its numbers.** 13 chunks, `lexical`,
**P@1 five of six, recall@3 six of six** — and the documents were written for the tests by the same
hand that wrote the questions, so it is **a baseline to beat, not evidence that chunking works**. In
[`retrieval-history.md`](../../../../brain/retrieval-history.md) with that said plainly.

**The one miss is the more interesting row.** *"When do ducts not need insulating"* wanted the
exception in `4.1.1` and got `9.1.1` first — **the other document's insulation clause, which also
carries an exception**. Both are true answers. That is not a retrieval defect, it is
[R-24](01-requirements.md) in miniature — two sources with a claim on one question — and today ranking
picks one silently. **Recorded rather than tuned.** The answer to it is *surface the conflict*, which
is Stage 8.

**R-81 was named as Stage 2 work in the last PR and it is not in this one, deliberately.** The guard
scans chunks **before they are built into a packet**, and in Stage 2 no chunk reaches a packet —
`heron_context.py` is untouched. A guard on a path nothing walks is a guard that cannot be tested.
**It moves to Stage 3**, where the packet carries chunks and there is a seam to guard. Said here
rather than quietly dropped.

**Next.** Stage 3 — the citation bound to the exact chunk (R-63 to R-65), the `STANDARDS` refusal
narrowing rather than softening (R-45), the guard (R-81), and the fabrication check (R-46 to R-55).

### 2026-09-11 — Stage 3: the citation, the guard, and the check that cannot rewrite

**Closes R-21, R-22, R-46 to R-55, R-63 to R-65, R-80 and R-81.** **R-45 is PART**, and §"what it
could not do" below says which part. `tests/test_ground.py`, 59 checks.

**Three things landed.**

1. **A clause reaches a packet with its citation bound to the exact chunk** (R-63), carried as a
   **quotation with every line prefixed**, and the `STANDARDS` path builds it.
2. **The Golden Rule 19 guard** (R-81) runs **at the seam**, before the part is built —
   [`34 §2.11`](../../../34-patterns-adapted.md) called that *"the most valuable single item the whole
   programme produced, because it lands on work not yet done"*. **The guard that actually holds is the
   quotation, not the pattern list**: a marker on the first line only is one a payload writes past, so
   every line is prefixed.
3. **`brain/heron_ground.py`** — a draft and the packet it came from in, a report out, and **it cannot
   rewrite**. `difflib` and `re`, no model, no network.

> ## The measurement changed the design twice, and that is the entry
>
> **The first version flagged on a similarity ratio**, per kind of claim, as the plan sketched. Run on
> twelve claims — six true, six invented — it got **two of the six inventions wrong**: *"except within
> 10m"* against a source saying **3m**, and *"density 96 kg/m3"* against **48**. The prose was nearly
> identical, so the ratio carried them. **One character is the whole fabrication, and a ratio is at
> its blindest exactly there.**
>
> So the ratio was measured properly, on six true paraphrases against six wrong claims:
>
> | | range |
> |---|---|
> | true paraphrases | **0.222 – 0.682** |
> | wrong claims | **0.204 – 0.588** |
>
> **They overlap almost entirely.** A true claim scores **0.222**, below a wrong one at **0.236**.
> **The ratio cannot do this job**, so it is reported and never enforced — and what catches an
> invention is structural instead: **an added fact is a flag, whatever the ratio says.**
>
> **One gate survived, because a quotation is a different claim.** It asserts it IS the source's words,
> so the question is not *how similar* but **is it in there**. Containment separated cleanly —
> **1.000 and 1.000 against 0.265 and 0.167**, nothing in between.
>
> **After the redesign: 0 false positives of 4 checked, 6 of 6 caught.**
>
> **This is not R-55 being bent.** R-55 forbids lowering a threshold to reduce flags. What changed is
> the **mechanism**, because a measurement said the old one did not work — the same method that
> settled the graph route at six settings. **The distinction is written down so the next reader can
> check it was not a tune wearing a reason.**

**Three defects found by running it, all in this batch's own code.**

1. **The citation marker was being read as a fact.** `[9.1.1]` looks exactly like a clause reference
   because it **is** one — so *"insulated to 25mm [9.1.1]"* appeared to state a clause its source did
   not contain, and **every true sentence was flagged as a lie**. The marker says where a claim came
   from; it is not part of the claim.
2. **The punctuation strip was eating two whole classes of fact before the selector saw them.**
   `1:100` became `1 100` and `50%` became `50`, so **a drainage fall was not a checkable fact at all
   and could be invented freely**. Both now have canonical forms, which also folds the three ways a
   fall is written on three drawings.
3. **`1,500mm` normalised to `1 500mm`.** The thousands rule needed a trailing word boundary that a
   following letter never provides.

### What it could not do, and R-45 is honest about it

**Asked about cats, the `STANDARDS` path returns five clauses, each correctly cited.** That is the
softening R-45 forbids, arriving through the front door.

**It is the same blocked floor as W-8.** Refusing *"nothing indexed covers this"* needs a floor;
[R-60](01-requirements.md) says a floor comes from a measurement; the measurement says none is
derivable on this backend. **A floor invented to fix it is exactly what R-60 forbids.**

So the packet **carries the measurement instead of pretending**: the contest sentence, plus a line
saying plainly that *a clause being cited does not mean it answers the question*. **That is weaker
than a refusal and it is said out loud** — here, in the code, and in the test, which asserts the cats
question still returns clauses **so the gap cannot be quietly forgotten**. When the floor exists, that
check is the one that should change.

**The empty-store refusal did narrow**, and still refuses: it now names the real cause rather than
saying no clause store exists at all.

**Next.** Stage 4 — the Librarian picks the scope, which needs documents in more than one scope. And
**S-4 is still open**: every document in these tests was written to be easy.

### 2026-09-11 — Stage 4: two scopes, two queries, and nothing pooled

**Closes R-38.** `heron_retrieve.librarian()`, `tests/test_librarian.py`, 20 checks.

**The whole stage is one sentence and the danger is in the name of the function.** *"An agent that
decides which scopes to search"* reads as *"an agent that searches several"*, and implemented that way
it is a `UNION` — one client's knowledge in the same result set as another's, which
[D-33](../../../DECISIONS.md) calls a **contractual** problem rather than a technical one.

So the shape is the guarantee: **there is no argument this function could take that pooled two
scopes.** Each is opened as its own `Store`, asked its own question, and returned under its own label.
Nothing compares a score from one store with a score from another — the same reason `find_documents()`
is separate from `find()`.

```text
company            Acme Engineering BIM Standard 2026  3.1   insulate to 30mm
project (Tower B)  Tower B Project Specification       3.1   insulate to 40mm
```

**Two different thicknesses, both correctly reported, and nothing decided between them.** Deciding is
the host's act; **surfacing that they disagree at all is Stage 8**, and this is the first time the
plan's R-24 case exists as something a test can point at rather than a sentence.

**`ATTACH` still raises, and it turned out to have been giving the instruction all along.** Its refusal
already read *"Open a second Store instead, where the crossing is visible and can be logged"* — which
is exactly what this stage built. The test asserts the refusal is **unchanged**.

**One defect in this batch's own code.** The project key was attached to every answer, so a company
answer came back labelled **`company (Tower B)`** — which reads as *Tower B's copy of the company
standard*. It is not; it is the company's one store. **Mislabelling whose knowledge something is, is
the exact confusion Golden Rule 5 exists to prevent**, arriving in the stage written to keep it.

### S-1 — settled by building it, and the two horns did not conflict

> This heading used to put the question's number directly in front of the word for *resolved*, and
> `check-docs.py` failed it — its question-count rule read the pair as a tally and compared it against
> `OPEN-QUESTIONS.md`. **The gate was wrong about the meaning and right to stop the line**: a phrase
> that reads as a count to a checker reads as one to a person skimming too. **The wording changed;
> the checker did not.**

> *When the Librarian needs two scopes, does Heron ask two questions itself, or hand the choice back to
> the host?*

**Both, and they were never opposed.** The brain asks each named scope **separately** — milliseconds
against a local file, so there is no cost argument — and **hands back one labelled answer per scope,
choosing none**. That is *asking two questions* in mechanism and *handing it back* in contract, which
is what [D-01](../../../DECISIONS.md) and R-62 actually require.

**Recorded as the plan's stated default taken, and flagged for the owner rather than closed.**

**Next.** Stage 5 — document nodes, and **the density count that decides whether the edge route is
built at all**. It begins with a count, not with code, and a recorded rejection closes the requirement
just as well as a route does.

### 2026-09-11 — Stage 5: the count, and the route that still has no vote

**Closes R-40, and R-39 provisionally.** `heron_graph.document_neighbours()`,
`tests/test_document_graph.py`, 15 checks.

**This stage begins with a count, not with code**, because the same idea over the **fragment** graph
was measured at six settings and **lost every one** — and the property that decided it was density.

| | fragments | documents |
|---|---|---|
| **median neighbours** | **50** | **7** |
| **worst** | **230** | **12** |

**An order of magnitude sparser, for a reason that is intelligible rather than lucky.** A fragment
providing `IList<Element>` composes with most of the library — that is what made it dense. **A clause
cannot do that.** Its neighbours are its one parent, the clauses sharing that parent, its own
children, and the clauses its text names; **the document's own numbering bounds the count**.

**So step 4 is done exactly as written: the route is built and has NO WEIGHT.** Three kinds of edge —
parent, sibling, and **a clause whose text names another clause's number**, which is the only one
worth having: *"labelling shall be in accordance with 21.3.1"* links two clauses that **share no
subject and no vocabulary**, so neither existing route can find it. **Nothing in retrieval reads any
of it**, and a test asserts that fusion still has exactly two weighted routes.

**The gate is passed PROVISIONALLY and the weight is not granted.** 62 chunks in four documents, all
written for these tests by the same hand. The density is *structurally* bounded — that part
generalises — but the *number* is about this corpus, and a question set over documents this session
wrote would measure the documents rather than the route.

**D-40 holds and is tested as an absence**: there is no edge table, and the test lists the tables to
prove it.

### And the count found a defect nothing else would have

The test ingested a document and **forgot to index it**, and retrieval answered **"nothing in the
indexed documents matched"**. The chunks were there; the searchable text was not. **That is the R-19
defect one level down** — a miss and an unbuilt index reading identically from the outside, which is
exactly how a number recorded from such a run becomes a measurement of nothing.

There is now a fourth state, `unindexed`, that says so and names the two calls that fix it. **Found by
a test making the mistake a caller will make.**

**Next.** Stage 6 — maintenance: re-index on change by content hash, and duplicate detection at write
time.

### 2026-09-11 — Stage 6: the index stays true without anybody remembering

**Closes R-27, R-28, R-29 — and finishes R-83, which Stage 1 left open.**
`tests/test_maintenance.py`, 18 checks.

**R-27 — re-index on change, and the test proves the thing that is easy to get wrong.** It moves a
file's mtime **10,000 seconds** and asserts **nothing re-indexes**. That is the case
[`05 §7`](../../../05-heron-brain.md) names: a `git checkout` touches every file and changes none of
them, so anything keyed on time re-indexes the whole library for nothing.

```text
$ python brain/heron_ingest.py --refresh
  CHANGED    Acme Standard 2026
             6f3aea899d79 is RETIRED and names db1c206a0f26 as its replacement
```

> ### The maintenance closed the gap Stage 1 had to leave open
>
> On the day Stage 1 landed, R-83 was marked **PART** with the reason written out: a changed file
> becomes a **different document** and nothing joined it to the one it replaced, so *"which edition is
> this clause from?"* was answerable and ***"what did it say before?"* was not.**
>
> **Re-indexing is the operation that needed the link**, so it built it. A changed file retires its
> predecessor (Golden Rule 4 — a record is never destroyed) and names its successor in `replaced_by`.
> **The old clauses stay**, so a citation written last month still resolves; they are simply no longer
> offered as answers.

**A missing source deletes nothing**, and the test counts the chunks either side to prove it. That is
Q-B being kept: the store **points at** the file and never held it, so the text and the citations
still read correctly after somebody tidies a folder. **Deleting the knowledge because a file moved
would be the opposite of what pointing at it was for.**

**R-28 — duplicates at write time, with no similarity number anywhere.** The content hash catches two
identical FILES. It does not catch the normal case: the same standard re-exported, or saved with a
cover line added. So a clause is compared for **byte-equality at the same locator** — exact, and
needing no threshold. **W-8 is the record of what happens when a number is invented to decide whether
two things are "the same enough".**

**Reported, never refused.** A project specification quoting a company standard verbatim is Tuesday,
not an error. What a person needs is to be **told**, so they can say which it is.

**R-29, and it had quietly stopped being true.** The host re-indexed the **fragment** half on every
open and **not the document half** — so a document ingested through the host stayed unsearchable until
somebody remembered a command, which is precisely *"the user manages the index by hand"* wearing a
different hat. Both halves now rebuild in the same place. `--rebuild` stays as recovery and has
stopped being the only way.

**What Stage 6 does NOT do, and the test says so.** **Nothing watches the filesystem.** The host
rebuilds on open and a person can ask for a refresh; **a document changed while Heron is open stays
stale until the next open.** A watcher is a different thing with different failure modes, and it was
not asked for.

**Next.** Stage 7 — the re-ranker — **needs the `model` backend and is therefore blocked here**, the
same block as W-8 and Stage 0's missing row. Stage 8 is trust and conflict, and Stage 4 already
produced its first real case: a company standard saying 30mm and a project spec saying 40mm, both
correctly returned, with nothing surfacing that they disagree.

### 2026-09-11 — a review found sixteen things, and every one I checked was real

**An automated reviewer went over the whole track on PR #114 and raised sixteen findings.** I verified
each against the code before touching anything. **Every one I could test reproduced.** They are worth
recording in full, because the pattern in them is more useful than any single fix.

> ## The pattern: three of them made the checker say `ok` about a false claim
>
> Stage 3 exists to catch a fabricated answer. Three separate holes meant it approved one.
>
> | | What happened |
> |---|---|
> | **A category or parameter was never a fact** | `OST_DuctCurves` and `BuiltInParameter.RBS_...` were matched **case-sensitively**, and `facts()` lowercases before running them. **Neither pattern could ever match.** R-50's list of checkable things contained two entries that could not be checked, and a sentence inventing a category was skipped as factless |
> | **A quotation with no number was never checked** | `The clause says "Ducts shall be painted red" [4.1]` has no number, unit or clause reference — so it was **skipped before the quote gate ran**. The one gate that survived measurement never ran on the only kind of claim it was built for |
> | **A shared clause number resolved to the wrong document** | Two documents both numbered `4.1`; the second overwrote the first in a dictionary. A **TRUE claim about the company standard was flagged as a fabrication** because the project spec's `4.1` won. That is R-51's false alarm arriving through the citation instead of the comparison |
>
> **All three are invisible from the outside.** A sentence the checker never looked at and a sentence
> it looked at and approved produce the same report. **A test that only feeds it fabrications it does
> catch cannot find this** — which is what my tests were doing.

**And the one that broke the whole chain at the seam.** Stage 3 binds a claim to the exact chunk it
cites. The **MCP server serialized every `Part` field except `citation`** — so the only path
production has into `heron_context` dropped the chunk id, and the host could never produce the marker
`heron_ground` reads. **The feature worked in-process and did not exist in production.**

> ## And one Golden Rule I broke without noticing
>
> **GR 11: the index is derived, never authoritative — deleting it must always be a safe recovery
> action.** Fragments obey it: delete every store and `--rebuild` reads `brain/fragments/` and puts
> them back.
>
> **Documents did not.** The `documents` table was the **only** record of which external files had
> been ingested, into which scope, with which title, status and trust. Delete a scope file — **the
> documented recovery action** — and all of it was destroyed while every original file sat untouched
> on disk.
>
> There is now an **append-only manifest beside the store, never inside it**, and `restore()` re-reads
> the files it names. The test deletes the store and brings the documents back. **Append-only because
> GR 4 says a record is never destroyed, and a forgotten document stays forgotten** — the manifest
> records that too, so a restore does not resurrect what somebody removed.

**The rest, each verified and fixed.**

| | |
|---|---|
| `--scope` with **no value** silently became `global` | **Golden Rule 5 broken by a typo.** A company document went to the globally shared scope and nothing said so. Now refused |
| A document producing **no chunks** was stored as a success | The **scanned-PDF case** — the most likely way a real standard fails to come in. Now refused, naming OCR |
| An **all-refused** ingest crashed | `no such table: chunks`, *after* printing its refusals. A refusal was the correct and complete answer |
| `heading_path` was built from the **parsing stack** while `parent_id` came from the numbering | Two columns describing one tree, able to disagree — **the same defect I had just fixed for `depth` and left here** |
| Any `OperationalError` read as "no documents" | A malformed database or a lock became a plausible empty answer. `heron_context._indexed` already narrows for this reason and I had not followed it |
| The citation dropped the **file path** | R-22 says a citation resolves to something a human can open; it carried a title and a clause number and nothing openable |
| The `unindexed` route was reported as a genuine miss | The refusal said documents *are* indexed and none covers the request, while its own appended note said the opposite |
| Retired chunks could **consume the route's window** | The lifecycle filter ran after each route had already limited itself. Each route now fills the pool with eligible rows |
| The density **median** took the upper middle | On an even count. That number decides whether a route is viable |
| A **typed suite count** in the README | I had bumped it by hand at every stage — six times — while the same line named the command that derives it. **The bumping was the evidence.** The number is gone |

**And one of the sixteen was only half fixed on the first pass, which is worth its own line.** The
finding asked to *screen AND safely delimit* every document-derived metadata field. I screened them and
**left them undelimited** — so the guard reported a hostile title and the title still sat unquoted in
the part's name. **Screening says there is a problem; delimiting is what stops it mattering.** Every
such value is now whitespace-collapsed and wrapped, because **a line break is the lever** that makes
text read as a new speaker — and nothing is truncated, because R-82 does not stop applying when the
field is small.

**What this says about the tests I wrote.** They were good at asserting the thing I had just built and
poor at asserting what it would do with input I had not thought of. **Every hole above is an input
shape, not a logic error** — a lowercased pattern, a quotation without a number, a clause number that
is not unique, a flag with nothing after it, a file with no text. The suites now carry all of them.


---

### 2026-09-11 — Stage 7: the seam is built, and the measurement it exists for cannot be taken here

**Stage 7's done-when has three clauses. Two are met and the third is not, and this entry is mostly
about not pretending otherwise.**

| Clause | |
|---|---|
| a run with the package uninstalled **still answers and says it is not using it** | ✅ `python brain/heron_retrieve.py "show me every duct in the model" --revit 2024` prints `Re-rank: absent` on the line under the route, and answers exactly as before |
| the **size was announced** | ✅ `python brain/heron_rerank.py` names the package, what it is for, **500 MB to 2 GB**, and `pip install --user` — and needs no network to print any of it |
| a **before-and-after** measurement at the same corpus size | ❌ **only the before exists.** No cross-encoder has ever run here. **W-9**, and `A10` |

### The one arithmetic decision, and it is the reason this stage was easy to get wrong

A cross-encoder's score is on an unrelated scale that differs by model. **Every number in
`heron_retrieve.py` is measured in `ONE_RANK`** — the quality nudge is bounded against it, and
`Contest` reads every spread in it. Folding a re-rank score into `Candidate.score` would have
**silently unbounded the nudge** and turned every spread `Contest` reports into a mixture of two units.

So the re-ranker **re-orders and does not score**. `rerank_score` and `rerank_rank` are carried beside
the fused score, never added to it, and the fused score stays on record underneath — which is also the
only reason a before-and-after is possible at all.

### And the defect that decision created two lines later, found by writing the test

`Contest` computes its spread as `scores[0] - scores[-1]` **over the list it is handed**. Once a
re-ranker re-orders that list, the first element is no longer the highest fused score, so the
subtraction goes **negative** — a shortlist reported as spanning **−3.2 ranks**, and `top_gap < 1.0`
true by construction, so **every re-ranked answer would have been called a coin toss.**

Fixed by sorting the scores before reading them, which changes nothing at all when no re-ranker ran.
**The test asserts `spread >= 0` on a re-ranked shortlist**, because the docstring saying it cannot
happen is what the sixteen-finding review was about.

### The sentence that would have been worse than the number

Even sorted, the fusion gap is a fact about **the shortlist the re-ranker was handed**, not about the
order shown. Reporting *"the winner is 2.1 ranks clear"* about an order a cross-encoder set would be
the most confident wrong sentence on that page. So `Contest` **derives** that a re-ranker ran — from
the candidates, so a caller cannot forget to say so — and leads with it:

> the RE-RANKER set this order, reading each question-and-chunk pair — so the fusion numbers here
> describe the shortlist it was given, not the order shown

### Three things carried over from lessons already paid for

- **`warm()`, from the start rather than after a stack dump.** `A8` was thirty minutes of a real
  Claude Code tool call waiting on a 1.0 s `import model2vec` inside an MCP handler, on the asyncio
  event loop. A torch import is heavier. It loads on a background thread, and the MCP server warms it
  beside the encoder.
- **The score travels out through the seam.** A review found the citation working in-process and
  absent through `mcp/server/heron_brain.py` — a feature that does not serialise does not exist in
  production. `rerank_score` is in the candidate dicts, and a test asserts it.
- **Every way a backend can misbehave is absorbed.** It throws; it returns the wrong number of scores.
  Both mean *fusion's order stands*, and a mismatched count is **refused rather than aligned by
  guesswork** — a silent misalignment would re-order the shortlist by nothing at all.

### What the stub is, and what it is not

`tests/test_rerank.py` injects a stub scorer to prove the plumbing. **A test may inject a scorer; a
measurement may not.** A stub's opinion about which clause answers a question is this session's opinion
wearing a model's clothes, and a re-ranker's entire claim is that it improves an order. Nothing from the
stub is in [`retrieval-history.md`](../../../../brain/retrieval-history.md) as a result.

### One thing measured that was not asked for, because it was cheap and it answers a reviewer

The absent path costs **0.000004 s** over twenty candidates against **0.0074 s** for a whole
`retrieve()` — **0.05 % of a query**. It builds twenty passage strings it then discards, because
`scores()` is asked before anything knows whether a backend exists. **Left that way on purpose**:
avoiding it would put a second place in the code that decides whether a re-ranker is present, and
0.05 % is not a reason to have two.

### And a piece of documentation drift this stage surfaced

[`01-requirements.md` §3](01-requirements.md) mapped seventeen agents to their code and said **"nine
have code standing on them; eight have nothing"** — typed, above the very command that derives it.
Stages 1 to 7 made it wrong four times over: `DIS-002` is `heron_ingest.py`, `CIT-014` is
`heron_ground.py`, `RIX-011` and `DUP-012` are Stage 6, and `RNK-006` is now two files. **The rows are
corrected and the count is gone**, replaced by the command. Same shape as the typed suite count the
review caught — and again, the repeated hand-bumping was the evidence.

**`RIX-011` and `DUP-012` are marked *"no file declares the id"* rather than declared**, because the
behaviour is spread across `heron_ingest`, `heron_search` and `heron_embed` and putting the id on one
of them would be a half-truth in the register that exists to prevent those.

---

### 2026-09-11 — a second and third review round, twenty-three findings, and the one that mattered most

**Two more rounds arrived after the sixteen were closed: fifteen on the Stage 1–6 commit and eight on
Stage 7. Every one was checked against the code and every one was real.** `tests/test_review_findings.py`
holds a check on each, so none comes back silently.

### The finding that would have done real damage

**The fabrication check was endorsing a quotation that reversed its source.**

```
clause   "No ducts shall be installed within the ceiling void unless ..."
quoted   "All ducts shall be installed within the ceiling void unless ..."
scored    0.982   against a gate of 0.90   -->  GROUNDED
```

`coverage()`'s docstring said **containment**; its code took the **longest common run** over the
quote's length, which is a different measurement — a long quotation with a short reversal at its
**start** keeps a very long matching tail. A checker that passes the opposite of a clause is worse
than no checker at all, because the answer now carries a citation *and* a clean report.

It is containment now, literally. **The gate moved to 1.0 and that is not a threshold being tuned
(R-55):** the measurement that set 0.90 recorded a true quotation at exactly **1.000** and a false one
at **0.265**, with nothing between. The 0.90 was slack around a number that had no spread, and the
slack is what the reversal walked through.

### And the same failure by a different route

```
clause   "Duct insulation shall not exceed 25mm"
draft    "Duct insulation shall exceed 25mm [chunk]"      -->  GROUNDED
```

Both yield exactly one fact, `25mm`. **Nothing was added**, so the added-fact rule passed it; the text
is nearly identical, so the ratio was **high** rather than low. Every rule in the file was working and
the answer was still the opposite of its source.

`reverses()` is structural and has **no threshold in it**: strip the negating words from both sides, and
if what is left is identical while the negations differ, one states the opposite of the other. What it
does **not** catch is written into its docstring — a reversal that also rewords leaves different
remainders and is invisible. That is the paraphrase problem R-46's measurement already recorded, and
this file has no model to solve it with (R-47).

> **It passed its own unit check and still returned GROUNDED end to end.** `normalise()` keeps the full
> stop a clause ends with, so the source's last word was `25mm.` and the draft's was `25mm`. Testing the
> helper proved the helper; only testing through `check()` proved the check. Both assertions are in the
> suite now.

### Three things that were complete, tested, and unreachable

This repository has a name for this mistake and still made it three times in one batch.

| | Reachable from | Now |
|---|---|---|
| `heron_ground.check()` | its own CLI, with a draft on disk | the `heron_check` tool and `brain.check_answer()` |
| `heron_ingest.refresh()` | its own CLI | `_reconcile()`, once per process, at open |
| `heron_ingest.restore()` | its own test | the same place, before the indexes |

**R-46 said *"before the answer is shown"* and was marked DONE.** Nothing that shows an answer could
call it. The row now says so.

### The label that became evidence

`as_quoted_source()` prefixes a clause with its document title, and `heron_ground` took `facts()` of
the **whole rendered string**. With a title like `QCS 2014`, the year **2014 became evidence for the
clause** — a draft claiming the clause applies to Revit 2014 came back grounded against text
containing no year at all. The raw clause is carried separately now, as `Part.evidence`. **A label is
not evidence for the thing it labels.**

### An optimisation that broke a Golden Rule, caught by an existing test inside a minute

`SEARCH.index_chunks()` deleted and rebuilt the whole FTS table on **every request**, while the comment
at its call site claimed it was free when unchanged — true of the embedding half, which is
content-hashed, and never true of this one. So it got a fingerprint.

**The first version keyed on the SOURCE only**, so emptying `chunk_text` and asking for a rebuild got
a polite no-op and a silently unsearchable store. Golden Rule 11 says deleting a derived thing is a
**safe recovery action**; a rebuild that declines to rebuild breaks exactly that.
`tests/test_document_retrieval.py` deletes that table and failed at once. The skip now checks the
derived table too.

### The rest, in one line each

| | |
|---|---|
| the re-ranker could **download gigabytes at startup** | `warm()` reached `CrossEncoder()`, which fetches weights. The announcement was real and on the wrong path. The automatic path is offline-only now; `--fetch --yes` is the only thing that downloads |
| the contest **blamed status for gaps status cannot make** | the nudge spans **0.61** of one rank across offerable statuses, and the sentence fired below **1.0** |
| a re-ranked report **called five candidates twenty** | the pool size is carried on the candidate, so a list cut to `limit` still knows what was read |
| a **blank line past the limit** ended the split search | every sentence end inside the limit was skipped and the chunk came back oversized |
| `subject to`, `notwithstanding`, `with the exception of`, `excluding` | all begin a qualification and none was in `QUALIFIERS`. **The list cannot be exhaustive and now says so** |
| a **truncated .docx** was a traceback | the XML parse sat outside the guard that names every other bad document |
| an **unnumbered chunk had no locator** | the comment said *"can be retrieved and CANNOT BE CITED — which R-21 calls a bug"*, and kept it anyway. They get `para-N`, which is a position a person can count to |
| **re-ingesting dropped the caller's status** | and nothing anywhere else moved a document's lifecycle. `status=None` now means *the caller did not say*, so a refresh cannot demote a REVIEWED document |
| a **moved file** never reached the manifest | so `restore()` looked for the old path and restored nothing |
| `restore()` **dropped the title** | the document came back under a filename, changing every citation written against it |
| a failed **manifest write** was ignored | the ingest reported clean while the store became the only registry again. It is a named degraded state now |
| **`1.5 m` and `2.5%` counted as clause references** | false edges in the one count that decides whether the graph route is ever worth a vote |
| `brain/README.md` said **no clause store exists** | two rows of one table contradicting each other about a central route |

### What this round is evidence of

**Three rounds, each after the previous was called done, each finding real things.** The first found
sixteen, the second fifteen, the third eight. Nothing here was a false positive.

The pattern across all three is one thing: **the tests asserted what the code was built to do, and
almost nothing about what it does with input nobody imagined** — a reversed quotation, a title that
is also a year, a deleted index, a file that moved. Every finding above is an input shape or a seam,
not a logic error. The suite is green either way, which is the honest measure of what green is worth.
