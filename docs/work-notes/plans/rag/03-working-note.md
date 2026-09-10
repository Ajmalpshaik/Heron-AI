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
| **W-5** | [`brain/README.md`](../../../../brain/README.md), the dependency table | `pyyaml` is the whole list | **`model2vec` is used too** — it is what makes `Backend: model` work. Somebody following the instructions exactly installs `pyyaml`, gets the weaker backend, and is told nothing |
| **W-7** | This plan itself, about ten times | `QCS 2014 §21.3.2 Insulation`, `Section 21 Mechanical` | **Invented as an illustration and never verified.** Whether QCS Section 21 is the mechanical section is not known here. **A plan about not fabricating clause numbers, fabricating a clause number** — left visible, flagged in [`00-structure.md` §3.3](00-structure.md), and replaced when Q-A names the real section |
| **W-6** | The repository has **no `requirements.txt`, no `pyproject.toml`, no `setup.py`** | — | [`tools/setup.ps1`](../../../../tools/setup.ps1) builds and deploys the **add-in** and installs no Python package at all. [`docs/07`](../../../07-installation-and-update.md) specifies an installer that *"checks required dependencies"* and a Dependency Agent that *"check[s] and install[s]"* them — **designed, not built.** So the Python half of Heron is installed by hand, from a list that is wrong (W-5) |

**W-5 and W-6 were found on 2026-09-10 by the owner asking a question** — *does a new person
installing from GitHub get this automatically?* The answer is that the **add-in half installs itself
and the Python half does not**, and this track makes it sharper rather than causing it: the plan adds
**optional** packages (the re-ranker, possibly a PDF reader), and every one of them degrades silently
when absent. **Silent degradation plus an install list nobody can follow is how a user ends up on the
weaker backend permanently.** W-6 is the gap; a dependency manifest would close both.

**W-4 is Stage 0** and is fixed by doing the work, not by editing the file. **W-1 to W-3 are three
sentences** and could be corrected in ten minutes — deliberately not done here, because a documentation
fix folded into a planning commit is the widening this folder forbids. They are logged so whoever picks
up Stage 0 fixes them in the same breath as the measurement, which is where they belong.

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
