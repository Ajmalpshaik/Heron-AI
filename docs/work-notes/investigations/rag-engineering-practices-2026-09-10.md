# Engineering practices from two production RAG systems — read 2026-09-10

> **Renamed 2026-09-11**, from a filename that was the author's account name. **A file should be
> named for what is in it**, and this repository goes public — a page titled after a person reads as
> being *about* them rather than about the two ideas taken from their work. The sources are named in
> §1 and credited in [`README.md`](../../../README.md), which is where credit belongs.

> **Type:** Operational work note — a research note from one session. **Not specification.**
> Where a sentence here disagrees with the [Constitution](../../../HERON_CONSTITUTION.md), the
> [Golden Rules](../../14-golden-rules.md) or [DECISIONS.md](../../DECISIONS.md), **those win.**
> **Status:** **ALL FOUR ✅ ROWS TAKEN by Ajmal PS on 2026-09-10**, and all four are in the RAG plan —
> §3.1 as [`00-structure.md` §3.6a](../plans/rag/00-structure.md) with requirements **R-46 to R-55**;
> §3.2, §3.3 and §3.4 as [`§3.7`](../plans/rag/00-structure.md) with **R-56 to R-65**. **The three ⏸
> owner's calls are still open.** This note retires once the taken rows are built and recorded in
> [`docs/34`](../../34-patterns-adapted.md).
> **Asked for by:** Ajmal PS, 2026-09-10 — *"check this profile, is there anything we can use for our
> project, not only RAG, for complete Heron AI."*
> **The rule this obeys:** [**D-25**](../../DECISIONS.md) — **studied and re-authored, never imported.**
> No code, no name, no library from any repository below is proposed for use here.
> **Where the durable part goes:** an accepted pattern belongs in
> [`docs/34`](../../34-patterns-adapted.md) beside the sixteen already there, and a rejected one belongs
> there too — that file records both. **This note is then deleted.**

---

## 1. What was read

`github.com/jamwithai` — Shirin Khosravi Jam and Shantanu Ladhwe, Berlin. Five public repositories,
read from their own pages and source on 2026-09-10.

| Repository | Stars | Last touched | Worth reading? |
|---|---|---|---|
| `production-agentic-rag-course` | 8,871 | 2026-06-05 | **Yes — the substance.** A seven-week build of a production RAG system |
| `observable-job-agent` | 272 | 2026-08-13 | **Yes — the more valuable of the two for Heron**, despite being 30× smaller |
| `beginner-local-rag-system` | 662 | 2025-06-29 | Skim. Local, private, offline — the right instinct, a heavier stack than Heron may have |
| `ai-ml-small-projects` | 40 | 2026-02-24 | No |
| `jamwithai` | 77 | 2026-07-21 | Profile README |

**The small one matters more than the popular one.** The course teaches a system built from nine
services. The job agent is one person's agent that takes **honesty** as its engineering problem — and
that is Heron's problem, not throughput.

---

## 2. The most useful finding: six things Heron already does

Listed first because it is the most valuable result, and the easiest to under-value. Two people with
8.9k stars, building something completely unlike a Revit add-in, arrived independently at six rules
this repository already holds. **That is evidence the rules are not idiosyncratic.**

| They do | Heron already | Where |
|---|---|---|
| Keyword search **first**, semantic added on top — *"solid search foundations enhanced with AI, not AI-first"* | FTS5 was Step 9; embeddings were Step 10 | [`05 §4`](../../05-heron-brain.md) argues it from the other end: BIM requests are full of exact tokens embeddings handle worst |
| Reciprocal rank fusion to merge keyword and vector | The same, with the same reasoning about not *picking* between routes | `heron_retrieve.py` |
| **"The human applies. The agent never submits."** | **The machine never signs a proof.** And `write.enabled` defaults to `false` | [D-30](../../DECISIONS.md), [D-19](../../DECISIONS.md) |
| A **grounding contract** — the voice surface may only say what the checkpoint holds, and never generates on its own | The brain hands the host a packet; **the host writes the reply** and cannot invent past the parts list, which **raises** | [D-01](../../DECISIONS.md), `heron_context.py` |
| **Named failures replace silent ones** — *quota exhausted, key rejected, timeout* | *"they EXIST but are not for this release"*, and an empty store that refuses rather than saying *nothing matched* | `heron_retrieve.py`, [`retrieval-history.md`](../../../brain/retrieval-history.md) |
| **230 tests that run with no keys and no network** | Every gate here runs offline; the container has no Revit and no `dotnet` and the suites still say something | `tests/` |

**The third row is the one to notice.** They wrote it as a product promise. Heron reached it from a
different direction — an agent that can stamp 193 fragments is the fastest machine ever built for making
an unproven claim look proven — and landed on the same sentence. **Two people who have never seen this
repository put the human at the last step for the same reason.**

---

## 3. What transfers

Marked the way [`docs/34`](../../34-patterns-adapted.md) marks things, so an accepted row can move there
unchanged.

### 3.1 ✅ **TAKEN 2026-09-10** — a fabrication check that uses no model at all

> **Taken by the owner on 2026-09-10 and folded into the plan the same day.** The design is
> [`plans/rag/00-structure.md` §3.6a](../plans/rag/00-structure.md), the requirements are **R-46 to
> R-55** in [`plans/rag/01-requirements.md` §H](../plans/rag/01-requirements.md), and the build order
> is [`plans/rag/02-implementation.md` §6.1](../plans/rag/02-implementation.md). **Re-authored, not
> copied** — the normalisation list below is Heron's own, because BIM text varies in units and clause
> numbers rather than in the things their corpus varied in.

**The single most valuable idea on the profile**, and it lands squarely on the requirement this
repository has no way to satisfy: [R-21](../plans/rag/01-requirements.md) — *no source, no claim* — which
[`05 §8`](../../05-heron-brain.md) calls **a bug, not a low-confidence answer.**

Their `validation.py` checks every claim in generated text against the source corpus, and **calls no
model to do it**. The mechanism:

1. **Normalise both sides identically** before comparing — lowercase, strip punctuation, collapse
   whitespace, expand numeric suffixes (`10m` → `10 million`), normalise frequency words (`daily` →
   `day`), standardise units. So a spelling difference cannot look like an invention.
2. **Compare with `difflib.SequenceMatcher`** — a ratio from 0 to 1. Standard library. No network.
3. **Threshold per kind of claim, not one global number** — a rewritten bullet must stay within `0.65`
   of the item it came from; a skill must match corpus vocabulary at `0.85`; prose in a cover letter is
   allowed `0.55`, because prose legitimately paraphrases.
4. **Only check what is checkable** — in prose, only *factual-looking* sentences: ones carrying digits,
   years, or capitalised multi-word names. A sentence with no facts in it cannot fabricate one.
5. **Allow a claim that says LESS than the source.** *"AWS"* passes when the corpus says *"basic AWS"*.
   Understating is not fabricating.
6. **Try pairs when one match fails** — a real sentence often blends two sources, so before flagging,
   it tries combinations of the top references.
7. **The report records the thresholds it used**, so two runs are comparable.

**Measured effect, in their own numbers:** fabrication rate **0.2768 → 0.1288**.

**What it becomes here.** A standards answer is checkable the same way: every sentence Heron produces
about a clause is compared against the clause text it cites, deterministically, offline, with a
threshold. **R-21 stops being a rule nobody can test and becomes a number that can go in
[`retrieval-history.md`](../../../brain/retrieval-history.md)** beside the retrieval figures.

**And it is already this repository's house style.** [`34 §2.4`](../../34-patterns-adapted.md) —
*deterministic before the model, as an order*. This is that rule applied to the output rather than the
input.

**In Revit terms:** it is a model audit. Not *"does this look right"* — every dimension in the sheet
compared against the element it dimensions, automatically, and the ones that disagree listed with how
far off they are.

**What it must not become.** A threshold that gets lowered when it flags too much. That is the same
failure as narrowing a retrieval assertion to keep it passing, which
[`retrieval-history.md`](../../../brain/retrieval-history.md) refused three times.

---

### 3.2 ✅ **TAKEN 2026-09-10** — the citation points at the item, not the document

Their rewritten CV bullets carry a `corpus_ref` — a pointer back to the **specific source item** the
bullet came from, not to the CV as a whole.

**This is [R-09](../plans/rag/01-requirements.md) and [R-22](../plans/rag/01-requirements.md) at the
right granularity**, and it settles a design question the RAG notes left open: the citation's target is
the **chunk**, and the chunk's `locator` is what makes it openable. A citation that says *"QCS 2014"*
is decoration. One that says *"QCS 2014 §21.3.2"* is checkable.

It also makes §3.1 possible — you cannot compare a claim against its source unless you recorded which
source it came from.

---

### 3.3 ✅ **TAKEN 2026-09-10** — refuse the out-of-domain question before retrieving, not after

Their pipeline runs a **guardrail first**: is this question even in the domain? Out-of-domain queries
are stopped before retrieval, specifically to prevent hallucination.

**Heron has nothing here, and it already knows it.**
[`retrieval-history.md`](../../../brain/retrieval-history.md) says so almost as a joke — below a pool of
20, *"both routes agree"* is true of everything, *"including a question about cats"*. Ask Heron
something with no BIM content and it returns a ranked shortlist of fragments with a straight face.

**What it becomes here.** A question nothing in the library has a real claim on is answered *"nothing
here covers that"* — the same shape as the empty-store refusal and the `STANDARDS` refusal, both of
which already exist. **Heron has the refusal habit; it does not have this refusal.**

---

### 3.4 ✅ **TAKEN 2026-09-10** — grade what came back, before using it

Between retrieval and generation they insert **document grading**: is what came back actually relevant?
Only graded documents reach the generator.

**This is the missing half of [`00-structure.md` §3.1](../plans/rag/00-structure.md).** That section
says retrieval should report how contested its answer was, using numbers it already computes. Grading is
the same idea one step later — and together they are what makes a second pass worth running, because the
host needs to know *the shortlist is weak* to decide to ask again.

---

### 3.5 ⏸ OWNER'S CALL — the rewrite loop, and its hard cap

If the graded results are thin, they **rewrite the query and retrieve again — at most twice.**

The cap is the part worth copying. An agentic loop with no bound is a cost and latency risk, and
[`19 §5`](../../19-context-and-cost.md) exists to avoid exactly that.

**The open question is who loops.** [D-01](../../DECISIONS.md) puts classification with the host, so the
natural shape here is *the brain reports weakness, the host rephrases* — not a loop inside `brain/`.
That is [S-1](../plans/rag/00-structure.md)'s shape and it needs the owner's word.

---

### 3.6 ⏸ OWNER'S CALL — section-aware chunking, and whether chunks overlap

They chunk **by section, with overlap**, and say plainly that this beats naive fixed-size splitting.

The first half confirms [`00-structure.md` §3.3](../plans/rag/00-structure.md), which was already
decided that way. **The overlap is new and is a real choice** — overlap improves recall at a boundary
and costs storage and duplicate hits. Not decided here.

---

### 3.7 ⏸ OWNER'S CALL — time every stage, not just the whole call

They record a span per retrieval stage, and per source, specifically to find which one is slow.

Heron has [`heron_audit.py`](../../../brain/heron_audit.py) — what the brain did — but no per-stage
timing. Cheap to add, and it is the difference between *"retrieval is slow"* and *"the vector route is
slow at 360 fragments"*.

---

### 3.8 ❌ DOES NOT TRANSFER — the entire stack, and it is not close

Between them these projects run **Airflow, OpenSearch, PostgreSQL, Redis, Docker, Ollama, LangGraph,
Langfuse, Opik, Jina, FastAPI, Streamlit, Gradio, Next.js**.

**Every one of the storage and orchestration pieces needs a service, a server or a container.**
[D-01](../../DECISIONS.md)'s promise is per-user install with **no administrator rights**, on locked-down
machines — and [D-23](../../DECISIONS.md) chose SQLite over exactly this class of tool, on exactly this
argument, before any of it was read.

So: **not one dependency below is proposed.** Note also that the course reaches for a *remote embedding
API* (Jina), which [D-24](../../DECISIONS.md) already settled the other way — local by default, because a
per-call cost makes re-indexing something to avoid, and an index nobody rebuilds stops matching the disk.

**This is the normal outcome**, not a disappointment. [`34`](../../34-patterns-adapted.md) opens with it:
*the pattern transfers; the shape almost never does.*

---

## 4. The correction this research forces on Heron's own RAG notes

**Reading these repositories sent me back to [`34 §2.13`](../../34-patterns-adapted.md), and it
contradicts something written in this repository earlier today.**

[`00-structure.md` §3.2](../plans/rag/00-structure.md) proposes **a third retrieval route that follows
graph edges**. That was written without checking, and `34 §2.13` had already tried it:

> A third retrieval stream was measured — [`tools/measure-graph.py`](../../../tools/measure-graph.py),
> 360 questions, four query shapes, **six settings. All six lost.** The gentlest cost 1.1 points of P@1
> and the strongest 14, and **P@5 never improved at any setting** — so it did not widen recall either,
> which was the one thing it was supposed to be good at.

**And it named the property that decides it: density.** Heron's composition graph has a **median of 50
neighbours per fragment, worst 230**. A fragment providing `IList<Element>` composes with most of the
library, so *"the neighbours of the best hit"* is not a signal — it is a large slice of the library
added as competitors.

**What survives, and what does not.**

| | |
|---|---|
| ❌ **Does not survive** | Feeding the **fragment composition graph** into retrieval as a third stream. Measured, six settings, rejected. §3.2's proposal in its current form is wrong |
| ⏸ **May survive, unmeasured** | A **document** knowledge graph — *clause governs system, system has elements* — is **not the same graph**. It does not exist, and its density is unknown |
| ✅ **Survives regardless** | **The test to apply.** `34 §2.13` did not just reject a feature; it named what decides one. **Before any document edge route is built, measure the neighbour count.** If document edges are as dense as fragment edges, it loses the same way and for the same reason |

**This is the second time in one day that a Heron document was corrected by a Heron document.** It is
also the reason [`33`](../../33-external-repository-research.md) and
[`34`](../../34-patterns-adapted.md) are worth their length: a finding somebody else's corpus reports as
*probably yes* was tested here and came back *no*, and nobody has to spend that week again.

---

## 5. What was deliberately not taken

[D-25](../../DECISIONS.md): **studied and re-authored, never imported.**

- **No code.** Nothing above is copied. §3.1 describes a mechanism in enough detail to write fresh.
- **No names.** Not their module names, their metric names, their variable names.
- **No dependencies.** §3.8.
- **No branding, and no shape.** Their system is a research assistant and a job matcher. Heron is a
  BIM-modeller-facing platform, and [`32 §1`](../../32-master-architecture-reconciliation.md) is the
  standing warning that most studied projects are the other kind of thing.

---

## 6. What to do with this

**All four ✅ rows were taken on 2026-09-10** and all four are in the plan — twenty requirements,
**R-46 to R-65**, with a build order.

**Writing them up found something the reading did not.** §3.1, §3.3 and §3.4 look like three
features and are **one measurement at three thresholds** — *does this candidate have a real claim on
the sentence?* — reported, acted on, and refused on. Built as three they would produce three numbers
that disagree. [`00-structure.md` §3.7](../plans/rag/00-structure.md) is that finding.

**The three ⏸ owner's calls remain open:** the rewrite loop and its cap (§3.5), whether chunks
overlap (§3.6), and per-stage timing (§3.7).

If the owner takes any of them, the durable half moves into
[`docs/34`](../../34-patterns-adapted.md) beside the sixteen already there — including the rejections,
because that file records those too — and this note is deleted.

**§3.1 was the one to take first, and it was taken.** It turns *no source, no claim* from a rule
nobody can check into a number measurable offline, with no model, using the standard library.

**Note on [`docs/34`](../../34-patterns-adapted.md).** That file is titled for **the sixteen
repositories** and its §1 tally describes that one closed programme. This pattern came from a
separate reading on a different day, and its markers there are about reality — `BUILT`,
`ALREADY HELD`, `REJECTED` — not about intent. **So nothing was written into it yet.** The natural
moment is when §6.1 is built, as a new `✅ BUILT` entry. Say if you want it recorded there sooner.
