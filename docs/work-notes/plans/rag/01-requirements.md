# RAG — the requirement note

> **Type:** Operational work note. **Not specification.** Where a sentence here disagrees with the
> [Constitution](../../../../HERON_CONSTITUTION.md), the [Golden Rules](../../../14-golden-rules.md) or
> [DECISIONS.md](../../../DECISIONS.md), **those win and this note is out of date.**
> **Status:** **Active — nothing built from it yet.** Opened 2026-09-10.
> **Owner:** Ajmal PS.
> **Closure condition:** every row in §6 is either **DONE with evidence** or **withdrawn with a reason**.
> **Where the durable part goes when this retires:** the requirements that survive belong in
> [`docs/05`](../../../05-heron-brain.md) and [`docs/10`](../../../10-memory-and-knowledge.md); the
> decisions belong in [DECISIONS.md](../../../DECISIONS.md); the measurements belong in
> [`brain/retrieval-history.md`](../../../../brain/retrieval-history.md). **This file is then deleted.**

---

## 1. What this note is, and what it is not

It is **one page that says what Heron's RAG has to do**, gathered from the specification so that a
build can start without re-reading nine documents and guessing which sentence still applies.

It is **not** a new specification. Every row in §6 cites where it came from. If a row has no source it
is not a requirement, it is somebody's opinion, and it does not belong here.

The other two notes in this folder:

| | |
|---|---|
| [`00-structure.md`](00-structure.md) | **What kind of RAG this is** — the six structural decisions. **Read it first; structure decides these requirements** |
| [`02-implementation.md`](02-implementation.md) | **How** it gets built — stages, and what proves each one |
| [`03-working-note.md`](03-working-note.md) | **Where it actually stands** — the live log, dated |

---

## 2. The decisions already taken — do not re-open these

Four questions that look open are closed. Re-opening one costs a day and lands in the same place.

| | Question | Answer | Where |
|---|---|---|---|
| **D-23** | Which vector store? | **SQLite + `sqlite-vec` + FTS5**, one file per scope. Decided on the *installation* constraint — Heron installs per-user with no admin rights, and anything needing a service breaks the locked-down machines it is built for | [Q-10](../../../OPEN-QUESTIONS.md), [05 §5](../../../05-heron-brain.md) |
| **D-24** | Local or cloud embeddings? | **Local by default, cloud opt-in per scope.** The deciding argument is re-indexing: a per-call cost makes rebuilding something to avoid, and an index nobody rebuilds quietly stops matching the disk | [Q-11](../../../OPEN-QUESTIONS.md), [05 §6](../../../05-heron-brain.md) |
| **D-26** | What may leave the machine? | **The model FILE never leaves — no `.rvt`, no `.rfa`.** Project names, content and typing in the cloud are not an issue. This took three passes in one day to land, and the final pass is the rule | [Q-12](../../../OPEN-QUESTIONS.md) |
| **GR 5 / GR 11** | Scope mixing, and index authority | Knowledge is **separated, never pooled** — one store per scope. The index is **derived, never authoritative** — deleting it must always be a safe recovery action | [14 — Golden Rules](../../../14-golden-rules.md) |

**Why D-26 matters more than it looks.** Today the brain never touches a model file, so the rule costs
nothing. The moment documents are ingested it becomes real: a project folder holds `.rvt` files next to
the PDFs, and an ingester that walks a folder will find them. **The rule has to be enforced by the
ingester refusing the extension, not by the user remembering.**

---

## 3. The department on paper — seventeen agents

From [`docs/28`](../../../28-agent-registry.md) rows `HERON-RAG-LIB-001` to `HERON-RAG-RSH-017`, with
[`05 §3`](../../../05-heron-brain.md) as the prose. **Nine have code standing on them; eight have
nothing.** Derive it rather than trusting this table — the command is under it.

| Agent | Does | Code today |
|---|---|---|
| `HERON-RAG-LIB-001` Librarian | Decides **which scopes** to search | [`brain/heron_scope.py`](../../../../brain/heron_scope.py) |
| `HERON-RAG-DIS-002` Knowledge Discovery | Finds potentially relevant knowledge | **nothing** |
| `HERON-RAG-RET-003` Retriever | Retrieves candidates | [`brain/heron_search.py`](../../../../brain/heron_search.py) |
| `HERON-RAG-FMT-004` Fragment Matcher | Request against existing fragments | partial — style only, in [`heron_fragment.py`](../../../../brain/heron_fragment.py) |
| `HERON-RAG-SMT-005` Skill Matcher | Applicable skills | [`mcp/server/heron_brain.py`](../../../../mcp/server/heron_brain.py) |
| `HERON-RAG-RNK-006` Ranking | Fuses keyword and vector, ranks | [`brain/heron_retrieve.py`](../../../../brain/heron_retrieve.py) |
| `HERON-RAG-CTX-007` Context Builder | Assembles minimal context | [`brain/heron_context.py`](../../../../brain/heron_context.py) |
| `HERON-RAG-EMB-008` Embedding | Creates embeddings | [`brain/heron_embed.py`](../../../../brain/heron_embed.py) |
| `HERON-RAG-VEC-009` Vector Search | Searches the vector index | `heron_embed.nearest` |
| `HERON-RAG-IDX-010` Index Manager | Maintains indexes | `heron_search.ensure_tables` |
| `HERON-RAG-RIX-011` Re-index | Re-indexes **on change**, by content hash | **nothing** — rebuild is a hand-typed `--rebuild` |
| `HERON-RAG-DUP-012` Duplicate Detection | Duplicates **at write time** | **nothing** |
| `HERON-RAG-VAL-013` Knowledge Validation | Checks knowledge before it is used | **nothing** |
| `HERON-RAG-CIT-014` Citation / Source | Provenance. **No source, no claim** | **nothing** |
| `HERON-RAG-CNF-015` Knowledge Conflict | Two sources disagree | **nothing** |
| `HERON-RAG-EVO-016` Knowledge Evolution | Restructures when it stops fitting | **nothing** |
| `HERON-RAG-RSH-017` Research | Answers what Heron cannot, **everything cited** | **nothing** |

```bash
grep -rn "HERON-RAG" --include="*.py" --include="*.cs" . | grep -v '\.git/'
```

**Read the split, not the count.** The nine that exist are all on the *lookup* side — find a fragment,
rank it, build a context. The eight that do not exist are all on the *knowledge* side — ingest, cite,
de-duplicate, validate, reconcile. That is not an accident of ordering. It is §5.

---

## 4. What stands today, measured rather than remembered

Measured **2026-09-10**, in this container:

```bash
python brain/heron_fragment.py | tail -3      # 360 well-formed, 180 PROVEN
python brain/heron_embed.py                   # Backend: model - a trained model
python tests/test_embed.py                    # exit 0
python tests/test_retrieve.py                 # exit 0
```

The retrieval stack that [`05 §4`](../../../05-heron-brain.md) recommends is **built, all five stages**:
structured filter first, hybrid keyword and vector, reciprocal rank fusion, a quality signal, and an
exact-match short circuit. It works, it is tested, and it answers in one call.

**And `A7` is closed** — the trained encoder that had never run now runs. `Backend: model`.

---

## 5. The finding that shapes this whole track

**The knowledge store holds no knowledge. It holds fragments.**

Every table in it is about Heron's own code library:

| Table | Created by | About |
|---|---|---|
| `meta`, `fragments` | `heron_scope.py` | fragments |
| `vectors` — its `id` is a fragment id | `heron_embed.py` | fragments |
| `fragment_text` (FTS5), `utterances`, `identities` | `heron_search.py` | fragments |
| `capabilities`, `capabilities_wanted` | `heron_capability.py` | fragments |
| `skill_needs` | `heron_graph.py` | skills |

```bash
grep -rnE "CREATE (VIRTUAL )?TABLE" brain/*.py     # the whole schema, in one command
```

There is **no table a document can go in.** No standard, no specification, no project folder, no
company note, no drawing register. So:

- [`05 §8`](../../../05-heron-brain.md) requires that any claim about ISO 19650, QCS, Ashghal or a
  company standard **carries a citation to an indexed source document**, and calls an uncited standards
  answer *a bug, not a low-confidence answer*. **There are no indexed source documents.** The rule
  cannot be obeyed or disobeyed; it has nothing to act on.
- [`heron_context.py`](../../../../brain/heron_context.py) already tells the truth about this. Its
  `STANDARDS` path **refuses by name** — it wants clauses, no clause store exists, and it says so
  rather than quietly degrading. That refusal is the honest placeholder this track replaces.
- The knowledge scopes exist as **folders and a refusal**, correctly — `heron_scope.SCOPES` is
  `GLOBAL, COMPANY, PROJECT, USER, TEMPORARY, EXPERIMENTAL`. What they do not yet hold is anything
  a person put there. **And there are six, where [`05 §2`](../../../05-heron-brain.md) names seven:
  `COMMUNITY` has no scope in the code.** That is a gap to settle when community knowledge becomes
  real, not a defect today — recorded so it is not discovered by somebody trying to ingest into it.

**This is what "make RAG" means.** The retrieval half is built and tested. The knowledge half has not
started. Everything in §6 follows from that one sentence.

---

## 6. The requirements

**How to read the state column.** `DONE` means code exists **and** something checks it. `PART` means it
exists and something about it falls short of its source. `NONE` means no code carries it. `BLOCKED`
means it cannot start until an earlier row lands.

### A — Scope and separation

| | Requirement | Source | State | How you check |
|---|---|---|---|---|
| **R-01** | One knowledge store per scope, as one file each | GR 5, [05 §2](../../../05-heron-brain.md) | **DONE** | `python tests/test_scope_store.py` |
| **R-02** | A cross-scope query is impossible to **write**, not merely discouraged — `ATTACH` refused by name | GR 5, D-33 | **DONE** | `CrossScopeRefused` in `heron_scope.py` |
| **R-03** | Project knowledge refuses to open when no project is identified — a guess writes one client's knowledge into another's file | D-33 | **DONE** | `heron_scope.resolve` |
| **R-04** | A store is named so that renaming the model does not lose the knowledge | `heron_scope.py` header | **DONE** | `_safe_key` |
| **R-05** | Ingesting into a scope obeys the same wall as querying it — **a document lands in exactly one scope** | GR 5 | **DONE 2026-09-11** — `heron_ingest.py`, `tests/test_ingest.py` | `ingest()` takes a **Store**, which is already one scope. There is no argument that could take two |

### B — Ingestion: the half that does not exist

| | Requirement | Source | State | Note |
|---|---|---|---|---|
| **R-06** | A document can be put into a scope at all — PDF, Word, Markdown, plain text | [05 §1](../../../05-heron-brain.md), [00b §19](../../../00b-master-specification-agent-os.md) | **DONE for `.md`, `.txt`, `.docx`; PDF needs an optional reader** | `.docx` is a zip of XML and needs nothing. **PDF is the one format that cannot be standard library** — optional, loud, and names what to install. S-4 decides the rest by reading one real section |
| **R-07** | A document is stored as **chunks**, each independently retrievable | [05 §4](../../../05-heron-brain.md) | **DONE 2026-09-11** — `heron_ingest.py`, `tests/test_ingest.py` | |
| **R-08** | Chunking does not break the exact tokens BIM runs on — `OST_DuctCurves`, a parameter GUID, a clause number | [05 §4](../../../05-heron-brain.md) | **DONE 2026-09-11** — `heron_ingest.py`, `tests/test_ingest.py` | the requirement most likely to be got wrong quietly. **R-68 is its sharper twin** and matters more. Five token shapes asserted, and the test also asserts the text **really was cut somewhere** — otherwise it passes by splitting nothing |
| **R-09** | Every chunk carries provenance: **which file, which page or clause, when indexed, who added it** | [05 §8](../../../05-heron-brain.md), `HERON-RAG-CIT-014` | **DONE 2026-09-11** — `heron_ingest.py`, `tests/test_ingest.py` | and the **title comes from the document, not the filename** — `qcs-sec-21-final-v3` is not something a person can check against a printed standard |
| **R-10** | The ingester **refuses `.rvt` and `.rfa` by extension** — D-26 enforced by code, never by the user remembering | D-26 | **DONE 2026-09-11** — `heron_ingest.py`, `tests/test_ingest.py` | by name, and **before the file is opened**, so a 400 MB model is refused without being read. `.rte`/`.rft` are **deliberately not refused** — that was proposed and **waits on the owner's word** |
| **R-11** | A document is indexed by **content hash**, so re-ingesting an unchanged file costs nothing | [05 §7](../../../05-heron-brain.md) | **DONE for both, 2026-09-11** | the instruction was followed: `blake2b`, the same mechanism `heron_embed` uses, not a second one |
| **R-12** | Deleting the whole index and rebuilding it is a **safe recovery action**, for documents as it already is for fragments | GR 11, [05 §7](../../../05-heron-brain.md) | **DONE 2026-09-11** — `heron_ingest.py`, `tests/test_ingest.py` | delete and re-ingest gives **identical chunks — same count, same locators, same text** — and the original file is untouched, because the store never held it |

### C — Retrieval

| | Requirement | Source | State | Note |
|---|---|---|---|---|
| **R-13** | Structured filter **first** — scope, domain, Revit version, status. A SQL `WHERE`, not a search | [05 §4.1](../../../05-heron-brain.md) | **DONE** | `heron_retrieve.py` |
| **R-14** | The Revit version filter is a **wall**, never a soft signal — an incompatible fragment is absent, not demoted | [05 §8.1](../../../05-heron-brain.md) | **DONE** | `test_retrieve.py`, check 7 |
| **R-15** | Hybrid: keyword **and** vector over the survivors, fused by reciprocal rank | [05 §4.2–4.3](../../../05-heron-brain.md) | **DONE** | |
| **R-16** | Re-rank the top ~20 by quality signals — status, success rate, recency, version match | [05 §4.4](../../../05-heron-brain.md) | **PART** | **the spec says re-rank; the code applies a nudge deliberately smaller than one fusion rank, so it settles a tie and cannot overturn a better match. Success rate and recency are not signals at all — nothing records them.** A decision waiting to be written, not a bug |
| **R-17** | Exact-match short circuit — the 400th *"select all ducts"* costs **one lookup**, not a pipeline; and only a `PROVEN` fragment runs off it without asking | [05 §4.5](../../../05-heron-brain.md) | **DONE** | `heron_search` identity route |
| **R-18** | Retrieval reports **what was excluded and why** — never a silent empty answer | `heron_retrieve.py` | **DONE** | *"they EXIST but are not for this release"* |
| **R-19** | An **empty store refuses** rather than answering *nothing matched* — the two sentences are different and one of them is a lie | [`retrieval-history.md`](../../../../brain/retrieval-history.md), 2026-08-30 | **DONE for fragments** | **must be repeated for documents.** It was found by measuring, not by review |
| **R-20** | Documents are retrievable **alongside** fragments, and a result says which kind each hit is | [05 §1](../../../05-heron-brain.md) | **NONE** | |

### D — Trust, citation and conflict

| | Requirement | Source | State | Note |
|---|---|---|---|---|
| **R-21** | **No source, no claim.** A standards answer without a citation is a **bug**, not a low-confidence answer | [05 §8](../../../05-heron-brain.md) | **NONE** | the most credibility-carrying row on this page. **Section H is how it stops being untestable** |
| **R-22** | A citation resolves to something a human can open — file, page, clause | `HERON-RAG-CIT-014` | **NONE** | |
| **R-23** | Retrieved knowledge is **checked before it is used** | `HERON-RAG-VAL-013` | **NONE** | **Section I is its mechanism**, the way section H is R-21's |
| **R-24** | Two sources disagreeing is **surfaced and asked about**, not silently resolved by rank | `HERON-RAG-CNF-015`, [docs/20](../../../20-knowledge-trust-and-conflict.md) | **NONE** | |
| **R-25** | A trust score combining accuracy, freshness, usage, success rate and source trust feeds ranking | [00b](../../../00b-master-specification-agent-os.md) | **NONE** | R-16 is waiting on this |
| **R-26** | The Research agent may answer from **outside** Heron's knowledge, and everything it returns is cited | `HERON-RAG-RSH-017` | **NONE** | |

### E — Maintenance

| | Requirement | Source | State | Note |
|---|---|---|---|---|
| **R-27** | Re-index **on change**, by content hash and not by mtime — otherwise every git operation triggers a full re-index | [05 §7](../../../05-heron-brain.md), `HERON-RAG-RIX-011` | **NONE** as a trigger | the hashing exists; nothing fires it |
| **R-28** | Duplicate detection at **write** time, not only at read time | `HERON-RAG-DUP-012` | **NONE** | |
| **R-29** | The user never manages the vector database by hand | [05 §7](../../../05-heron-brain.md) | **PART** | `--rebuild` is currently typed by a person |
| **R-30** | Local embedding by default; **cloud is opt-in per scope** | D-24 | **PART** | local runs; **there is no opt-in path and no per-scope setting**, so the choice cannot currently be made at all |

### F — Measurement

| | Requirement | Source | State | Note |
|---|---|---|---|---|
| **R-31** | Every retrieval measurement records **date, corpus size, and which backend answered** | [`retrieval-history.md`](../../../../brain/retrieval-history.md) | **DONE for `lexical` 2026-09-11, still owed for `model`** | rows at 360 and both backends compared at one size. The `model` row needs a machine that can reach `huggingface.co` |
| **R-32** | Routing measured by `check-routing.py`, intrusion by `check-intrusion.py` — and neither may weaken a fragment's own words to buy a rank | `tools/` | **DONE**, re-run at **360** on 2026-09-11 | both exit 0. Words route **#1 for 81%, top three for 96%** of 2038 utterances; worst intruder in **64** shortlists it does not own. Still `lexical` — see R-31 |
| **R-33** | Document retrieval gets its own measurement from its first day | this note | **NONE** | do not let it start unmeasured, the way the fragment side nearly did |

> ### ✅ Closed 2026-09-11, and it produced something the plan did not expect
>
> [`retrieval-history.md`](../../../../brain/retrieval-history.md) now carries rows at **360**, both
> checkers have a run there, and the two backends are compared **at the same corpus size** — which is
> the comparison [`03-working-note.md` §3.2](03-working-note.md) said nothing had, because the jump
> from 59 to 360 moved corpus size and backend at once. Measuring the **old backend at the new size**
> separates them for nothing: the words route's decline is **corpus size**, the nearness route's
> improvement is **the backend**.
>
> **The `model` row is still owed.** It cannot be taken where `huggingface.co` is blocked, and it was
> **not** borrowed from an earlier session and re-presented as a fresh run.

**The paragraph below is what that closed. Kept because the finding is the file's own failure mode.**

**R-31 was violated, and it was the cheapest thing on this page to fix.**
[`brain/retrieval-history.md`](../../../../brain/retrieval-history.md) has **eleven rows and every one
says `lexical`**. The last is 2026-08-31 at **59** fragments. Today the library is **360** and the
backend is **`model`**. The record is six times out of date on corpus size and wrong on the backend —
which is the exact failure that file was written to prevent, happening to that file. The measurement
taken today is in [`03-working-note.md`](03-working-note.md) and has **not** been moved into it,
because this folder's own rule is that **a defect found while planning is recorded, not fixed.**

### G — The structure decided on 2026-09-10

**All six structural questions are in scope** — [`00-structure.md` §1](00-structure.md). These rows are
what that decision creates. Every one is **NONE** today; the point of listing them is that several are
cheap now and expensive later.

| | Requirement | Source | State | Note |
|---|---|---|---|---|
| **R-34** | An answer says **how contested it was** — the spread across the shortlist in units of one fusion rank, and whether the routes agreed | [00 §3.1](00-structure.md) | **DONE 2026-09-11** | `heron_retrieve.Contest`, `tests/test_contest.py`. It also reports the **two magnitudes fusion discards** — bm25 and raw nearness — because a floor can only ever be derived from those |
| **R-35** | A shortlist that is effectively tied **says so, in words** — not only as a number | [00 §3.1](00-structure.md), S-3 | **DONE 2026-09-11** | *"A COIN TOSS"* when the top two are inside **one rank of fusion** — which is narrower than the quality nudge, so status alone could have set that order. The one comparison it makes, and it is arithmetic rather than a dial |
| **R-36** | A chunk knows its **parent**, so a clause can be returned with the section it sits in | [00 §3.3](00-structure.md) | **DONE 2026-09-11** — `heron_ingest.py`, `tests/test_ingest.py` | |
| **R-37** | Hierarchy depth is **arbitrary, stored as a parent link** — not a fixed Part/Section/Clause | [00 §3.3](00-structure.md), S-2 | **DONE 2026-09-11** — `heron_ingest.py`, `tests/test_ingest.py` | and a dotted clause takes its parent from **the document's own numbering** — `4.1.1`'s parent is `4.1` — which is stronger evidence than where it sat in a stack |
| **R-38** | The Librarian picks **one** scope. Two scopes means **two queries**, never one merged query, and `CrossScopeRefused` is unchanged | [00 §3.4](00-structure.md), GR 5, D-33 | **NONE** | **the trap in this whole track.** A `UNION` here is a contractual problem, not a technical one |
| **R-39** | A **third retrieval route follows graph edges**, over **documents only**, and **only if a density count passes first** | [00 §3.2](00-structure.md), [34 §2.13](../../../34-patterns-adapted.md) | **NONE, and conditional** | **the same route over the fragment graph was measured at six settings and lost every one.** Density decides it: median 50 neighbours, worst 230, is competitors rather than signal |
| **R-40** | Document nodes and their edges are **derived on demand**, like every edge but one | [00 §3.2](00-structure.md), D-40 | **NONE** | a stored document edge is a cache that goes stale |
| **R-41** | A cross-encoder re-ranks the top ~20, and **its absence makes Heron slower to be right, never broken** | [00 §3.5](00-structure.md), [05 §4.4](../../../05-heron-brain.md) | **NONE** | the same fallback contract `heron_embed.py` already honours |
| **R-42** | The re-ranker installs **per-user with no administrator rights** | [00 §3.5](00-structure.md), D-01 | **NONE** | `model2vec` already proved this is possible |
| **R-43** | The context packet carries **citation, confidence, and what is missing** — enough that the host answers without inventing | [00 §3.6](00-structure.md) | **NONE** | this is what "generation" means here |
| **R-44** | **No language model inside `brain/`.** The host writes the reply the user reads | [00 §3.6](00-structure.md), D-01 | **DONE, and must stay done** | `check-metadata.py` prints it as fact |
| **R-45** | When the clause store exists, the `STANDARDS` refusal **narrows** to *"nothing indexed covers this"*. It never softens into a guess | [00 §3.6](00-structure.md) | **NONE** | a system that refused honestly while empty and guessed once full would be worse than the one that refused |

---

### H — The fabrication check, taken 2026-09-10

**Taken by the owner** from an outside repository and **re-authored, never imported**
([D-25](../../../DECISIONS.md)) — the reading is
[`the investigation` §3.1](../../investigations/rag-engineering-practices-2026-09-10.md), the design is
[`00-structure.md` §3.6a](00-structure.md). It is what makes **R-21** testable instead of merely stated.

| | Requirement | Source | State | Note |
|---|---|---|---|---|
| **R-46** | Every sentence of a proposed answer that makes a **factual claim about an indexed source** is compared against the chunk it cites, before the answer is shown | [00 §3.6a](00-structure.md) | **NONE** | R-21's enforcement, and R-21 has had none |
| **R-47** | The comparison calls **no model and touches no network**. Standard library only | [00 §3.6a](00-structure.md), [34 §2.4](../../../34-patterns-adapted.md) | **NONE** | *deterministic before the model*, applied to the output instead of the input. It must run on a machine with no keys |
| **R-48** | **Both sides are normalised identically** before comparing — case, punctuation, whitespace, and the forms BIM text genuinely varies in: `150mm` / `150 mm`, `DN150` / `150Ø`, `2024` / `Revit 2024`, `§21.3.2` / `21.3.2` | [00 §3.6a](00-structure.md) | **NONE** | **a spelling difference must never read as an invention.** The unit list is Heron's own, and is where this differs most from what was read |
| **R-49** | The threshold is **per kind of claim**, never one global number — a quoted clause is held tighter than a paraphrase of one | [00 §3.6a](00-structure.md) | **NONE** | one number for every kind of sentence is either too loose for quotes or too tight for prose |
| **R-50** | **Only checkable sentences are checked** — those carrying a number, a dimension, a clause reference, a category name or a parameter name | [00 §3.6a](00-structure.md) | **NONE** | a sentence with no fact in it cannot fabricate one, and flagging it teaches people to ignore flags |
| **R-51** | **A claim that says LESS than its source passes.** Understating is not fabricating | [00 §3.6a](00-structure.md) | **NONE** | *"insulated"* is not a fabrication when the clause says *"insulated to 25mm"*. **The row most likely to be got wrong** |
| **R-52** | The report records **the thresholds it used, how many claims were checked, and every flag with its ratio** | [00 §3.6a](00-structure.md) | **NONE** | the denominator is what makes two runs comparable; a flag count alone is not a measurement |
| **R-53** | The check **flags, never rewrites** | [00 §3.6a](00-structure.md), D-30 | **NONE** | the brain cannot write the answer, so it must not repair one either |
| **R-54** | The fabrication rate is recorded in [`retrieval-history.md`](../../../../brain/retrieval-history.md) **with its date, corpus size and thresholds** | R-31, [00 §3.6a](00-structure.md) | **NONE** | the rule every other number here obeys |
| **R-55** | **A threshold is never lowered to reduce flags** | [`retrieval-history.md`](../../../../brain/retrieval-history.md) | **NONE** | the same refusal that kept *"show me just these"* on the isolate fragment. Tuning a threshold to make a report look better is the measurement protecting itself |

---

### I — The other three, taken 2026-09-10

**Taken by the owner** the same day as §H, from the same reading, and **re-authored, never imported**
([D-25](../../../DECISIONS.md)). Designed together in [`00-structure.md` §3.7](00-structure.md), because
R-56 to R-62 are **one measurement at three thresholds** — report it (R-34), act on it (R-56), refuse on
it (R-58) — and building them as three mechanisms produces three numbers that disagree.

| | Requirement | Source | State | Note |
|---|---|---|---|---|
| **R-56** | A candidate that clears the structured filter but has **no real claim on the question** is **dropped, not ranked last** | [00 §3.7a](00-structure.md), `HERON-RAG-VAL-013` | **BLOCKED on the `model` backend — W-8** | R-23's mechanism. **A shortlist is not the top five of everything** — at 59 fragments [`retrieval-history.md`](../../../../brain/retrieval-history.md) recorded three of the top five with no claim on the sentence, two of which **write to the model** |
| **R-57** | A dropped candidate is **counted and reported** — the answer says how many were dropped, and on what floor | [00 §3.7a](00-structure.md), R-18 | **BLOCKED on R-56** | the same habit as *"they EXIST but are not for this release"*. A silent drop is indistinguishable from a retrieval that never found it |
| **R-58** | When **nothing** clears the floor, the question is **refused by name** — before generation, before cost | [00 §3.7b](00-structure.md) | **BLOCKED on the `model` backend — W-8** | ask Heron about cats today and it answers with a confident ranked shortlist |
| **R-59** | The refusal distinguishes **three different nothings**: the store is empty · everything was blocked for this release · nothing here covers that | [00 §3.7b](00-structure.md) | **first two DONE, third BLOCKED on R-58** | they need three different actions from the reader, so one message for all three is a wrong answer twice |
| **R-60** | The floor is **derived from the same measurement as R-34** — never from a hand-written list of in-domain words | [00 §3.7b](00-structure.md) | **HELD, and it is what blocks R-56 to R-59** | a keyword list would be wrong the week it was written and nobody would maintain it |
| **R-61** | The floor is **pool-aware** — below a small pool, route agreement is not evidence | [`retrieval-history.md`](../../../../brain/retrieval-history.md) | **DONE 2026-09-11** | already measured: under a pool of 20, *"both routes agree"* is true of everything, *"including a question about cats"* |
| **R-62** | **No classifier in `brain/`.** The brain reports *nothing here has a claim*; deciding what the user meant is the host's act | D-01, [00 §3.7b](00-structure.md) | **DONE, and must stay done** | `heron_context.py` already refuses to classify, and an assumed path says it was assumed |
| **R-63** | A packet part drawn from a document carries the id of the **exact chunk**, not of the document | [00 §3.7c](00-structure.md) | **NONE** | sharpens R-09 rather than replacing it |
| **R-64** | That binding **survives into the draft answer**, so R-46's comparison has a defined target | [00 §3.7c](00-structure.md) | **NONE** | **the quiet prerequisite of §H** — you cannot compare a claim to its source without recording which source |
| **R-65** | A claim with **no chunk pointer is uncited** — R-21 applies, so it is a bug and not a low-confidence answer | [00 §3.7c](00-structure.md), R-21 | **NONE** | *"per the specification"* is not a citation |

---

### J — What the field reading added, 2026-09-10

**From** [`the field reading`](../../investigations/rag-state-of-the-art-2026-09-10.md) — LightRAG,
RAGFlow, contextual retrieval, Docling, current chunking practice, CPU re-rankers and the embedded
vector stores. **Re-authored, never imported** ([D-25](../../../DECISIONS.md)). Design in
[`00-structure.md` §3.8](00-structure.md). **All five are chunker-time decisions**, which is why they
are cheap now and expensive later.

| | Requirement | Source | State | Note |
|---|---|---|---|---|
| **R-66** | A chunk is embedded and indexed **with its heading path prepended** — *QCS 2014 → Section 21 → 21.3 Ductwork → 21.3.2* | [00 §3.8a](00-structure.md) | **STORED 2026-09-11; prepended when Stage 2 indexes** | `heading_path` on the row, `indexed_text()` on the chunk. The published form of this cuts retrieval failures by **49%, and 67% with re-ranking** |
| **R-67** | That context is **read off the document's structure, never generated by a model** | [00 §3.8a](00-structure.md), D-24 | **DONE 2026-09-11** — `heron_ingest.py`, `tests/test_ingest.py` | **the whole point.** The test asserts every locator in a path is one an **ancestor row also holds** — which is what *derived rather than generated* has to mean to be checkable at all |
| **R-68** | **A chunk is never split between a rule and its exception.** A candidate split immediately before *except*, *unless*, *provided that*, *save that* or *however* **is not a split point** | [00 §3.8b](00-structure.md) | **DONE 2026-09-11** — `heron_ingest.py`, `tests/test_ingest.py`, **and its test was written first** | **the worst thing this system could do.** Three more qualifiers were added to the five named: *other than*, *save where*, *save as*, *but not*. When no legal split point exists the chunk stays **oversized and is flagged** — refusing to cut is a real outcome |
| **R-69** | A document's **chunk boundaries can be shown to a person before the document is trusted** | [00 §3.8c](00-structure.md), D-30 | **DONE 2026-09-11** — `heron_ingest.py`, `tests/test_ingest.py` | `--boundaries <id>`. It ends by naming **how many were split by length rather than structure**, because those are the ones worth reading |
| **R-70** | The **vector count is reported**, against the ceiling [D-23](../../../DECISIONS.md)'s choice implies — `sqlite-vec` brute-force stays fast below roughly **500,000** vectors | [00 §3.8d](00-structure.md) | **DONE 2026-09-11** | the ingester prints the chunk count and the ceiling every run. Said while the number is small, so the day it approaches is a day somebody **notices** rather than a day retrieval quietly gets slow |

---

### K — Installing it, asked for by the owner 2026-09-10

**Owner's instructions, 2026-09-10, in two parts.** First: *"the new installer also needs to install this
automatically, and mention it in the README."* Then, sharpened: *"if you are installing something purely
for Heron AI, mention that in the README — that this thing we need for Heron AI, and it will be
automatically installed, and while they are notified. Also you need to check that it is installed; if
yes, is that the right one, or does it need to update. And they need to understand the size of the
items, like system requirements."* **R-78 and R-79 come from the second half, and neither was in the
plan before he said it.** Raised by him after asking whether a new person cloning from GitHub gets the dependencies
automatically — the answer was **no**, recorded as **W-5** and **W-6** in
[`03-working-note.md` §4](03-working-note.md).

**This belongs to [`docs/07`](../../../07-installation-and-update.md), not to RAG.** It is written here
because **this track is what makes it urgent**: every piece the plan adds is optional and **degrades
silently**, and silent degradation plus an install list nobody can follow is how somebody runs the weak
version of Heron for months and judges the product on it. When these are built, they move to `docs/07`
and leave this note.

| | Requirement | Source | State | Note |
|---|---|---|---|---|
| **R-71** | **A dependency manifest exists** — the list of packages is a file a machine reads, not prose in a README somebody edits | W-6 | **NONE** | there is no `requirements.txt`, no `pyproject.toml` and no `setup.py` in the repository. **The list cannot go stale if nothing types it twice** |
| **R-72** | One command reports **every optional dependency: present or missing, and what is lost without it** | [00 §3.9](00-structure.md) | **NONE** | half of it exists — `heron_embed.backend()` already returns the name **and a reason**. What it never says is *what to install to fix it* |
| **R-73** | A component running in **fallback mode says so, and names what would improve it** | R-72, W-5 | **PART** | the backend line says *"NOT meaning"* today and stops there |
| **R-74** | Setup installs the **Python side as well as the add-in**, per-user, with no administrator rights | W-6, D-01 | **NONE** | [`tools/setup.ps1`](../../../../tools/setup.ps1) builds and deploys the add-in for every Revit on the machine in one command, and installs no Python package at all |
| **R-75** | The README states **what is required, what is optional, what each optional one costs in size, and what it buys** | W-5 | **NONE** | measured 2026-09-10: the whole installed Python side is **≈93 MB**. A re-ranker would add **500 MB to 2 GB**, and a document parser several hundred more. **Those two are the only large ones, and a person is entitled to know before installing** |
| **R-76** | The README **names every package as a Heron dependency and says what Heron uses it for** — not a bare list | owner, 2026-09-10 | **NONE** | *"`sqlite-vec` — faster vector search"*, not *"sqlite-vec"*. A name with no purpose beside it is a thing nobody dares remove |
| **R-77** | Installation is **automatic AND announced** — each package named, with **what it is for and how large it is, before the download starts** | owner, 2026-09-10 | **NONE** | announced *before*, so somebody on a slow or metered connection can stop a 2 GB download rather than discover it |
| **R-78** | The check asks **not only "is it installed" but "is it the RIGHT one"** — the version is checked, and an out-of-date one is reported as needing an update | owner, 2026-09-10 | **NONE** | **installed is not the same as correct.** `sqlite-vec` is at `0.1.9` — pre-1.0, where an interface can still move under a caller. A component that loads an old version and half-works is worse than one that refuses |
| **R-79** | A **system requirements** section states the total disk cost — minimum, and with each optional piece — so a person can judge **before** starting | owner, 2026-09-10 | **NONE** | measured 2026-09-10: **≈93 MB minimum**. Plus a re-ranker, **500 MB – 2 GB**. Plus a document parser, several hundred MB. **Nobody should discover that halfway through an install** |

---

### L — Checking the plan against the specification, 2026-09-10

**Owner's instruction:** *"check it against our project specification, our roadmap and our docs — is
what we planned the same, or correct."* Done against the
[Constitution](../../../../HERON_CONSTITUTION.md), the [Golden Rules](../../../14-golden-rules.md), the
[Roadmap](../../../ROADMAP.md) and [`docs/05`](../../../05-heron-brain.md).

**It found one thing that matters more than everything else added today.**

| | Requirement | Source | State | Note |
|---|---|---|---|---|
| **R-80** | **Every ingested document is untrusted text.** Nothing inside one may change what Heron is permitted to do | [GR 19](../../../14-golden-rules.md), [00 §3.10](00-structure.md) | **MARKED 2026-09-11** — `untrusted = 1` on every chunk | **the plan reached 79 requirements without mentioning Golden Rule 19.** The column exists from the first row ever written. **The guard that reads it is R-81, and that is Stage 2** |
| **R-81** | The path **from retrieval into a packet is guarded** — chunks are scanned **before** assembly | [34 §2.11](../../../34-patterns-adapted.md), [00 §3.10](00-structure.md) | **NONE** | `34` calls this *"the most valuable single item the whole programme produced, because it lands on work not yet done."* This plan is that work |
| **R-82** | An oversized or suspicious chunk is **flagged, never truncated** | [34 §2.11](../../../34-patterns-adapted.md) | **DONE for oversized, 2026-09-11** | a block with nowhere legal to cut comes through **whole** and is listed by id. *Suspicious* is Stage 2, with the guard. Truncation lets a payload be **padded past the scanner's window** |
| **R-83** | A document has **identity, version and lifecycle** — not only a content hash | [GR 10](../../../14-golden-rules.md) | **PART — identity and lifecycle DONE 2026-09-11; version is not linked** | `DRAFT · REVIEWED · RETIRED` on the row, and identity is the content hash. **A changed file becomes a different document and nothing joins it to the one it replaces** — so *"which version of QCS is this clause from?"* is answerable and *"what did this clause say before?"* is not. Named rather than counted as done |
| **R-84** | **Ingesting, re-indexing and deleting a document each leave an audit entry** | [GR 14](../../../14-golden-rules.md), [D-62](../../../DECISIONS.md) | **DONE 2026-09-11** — `heron_ingest.py`, `tests/test_ingest.py` | `knowledge.ingest` and `knowledge.forget`, each naming the document and the scope |

**Three rules the plan was already keeping**, checked rather than assumed: **GR 5** scope separation
(R-01 to R-05, R-38), **GR 11** the index is derived (R-12), and **GR 7** — *one agent creates, another
validates* — which is exactly the shape of §H, where the thing that checks an answer is not the thing
that wrote it.

---

### M — Licensing, raised by the owner 2026-09-11

**A licensed standard is not the same kind of object as a company note**, and the difference only shows
up when a store is copied.

| | Requirement | Source | State | Note |
|---|---|---|---|---|
| **R-85** | Every document records **what it is licensed for** — owned outright, licensed per seat, public, or unknown | owner, [34 §2.12](../../../34-patterns-adapted.md) | **NONE** | `34 §2.12` is an open item called *"an inventory of what imported knowledge permits."* **This is that item arriving** |
| **R-86** | A scope containing a **per-seat licensed** document **warns before the store is copied or shared** | R-85, [GR 12](../../../14-golden-rules.md) | **NONE** | *"copy `company.db` to the shared drive"* was suggested in conversation on 2026-09-10 as the way to share company knowledge. **That is also exactly how a bought standard reaches twenty people who did not buy it** |
| **R-87** | **No knowledge store is ever committed to the repository or shipped with Heron** | [17](../../../17-open-source-and-distribution.md), R-85 | **HELD BY ACCIDENT** | the stores live in `%APPDATA%`, outside the repository, so this cannot happen today. **It is held by the file layout rather than by a rule**, and Heron goes public — so it is worth being a rule |

**And an ordering rule, from the owner's own argument:**

| | Requirement | Source | State | Note |
|---|---|---|---|---|
| **R-88** | **Two orders, and they are not the same question.** *Value to one user* runs project → company → regional → international, because the model knows least about the first. ***What Heron should know out of the box*** runs the other way: **BIM and ISO standards first, regional next, company last** | owner, 2026-09-11 | **NONE** | **the owner corrected this on 2026-09-11 and he is right.** A company standard helps one company; ISO 19650 helps every user of the product, and is stable across projects. The first framing was about one person's value and the second is about the product — **the product one governs** |
| **R-88a** | **No standard is ever bundled with Heron.** Priority does not mean shipping it | R-85, [17](../../../17-open-source-and-distribution.md) | **NONE** | ISO sells ISO 19650 the way NFPA sells NFPA. *"First priority to load"* and *"ships in the box"* are different sentences, and only the first one is true |
| **R-88b** | **The first document is a TEST of the engine, not the start of the library** | owner, 2026-09-11 | **NONE** | *"this is the RAG engine we are making, not started with the real documents."* **Correct, and it unblocks Stage 1**: it needs one numbered structured document to prove chunking and citation, not the real corpus and not anybody's licensed copy |

---

### N — The person putting a document in is a modeller, 2026-09-11

**Owner's instruction:** *"a new installer is like a modeller — they don't know where we need to keep the
knowledge. So inform them where it needs to be kept, how it will be, and how safe it is — that it will
not do anything anywhere else."*

**This is [Golden Rule 1](../../../14-golden-rules.md) applied to the one screen this plan adds.**
[`docs/01`](../../../01-vision-and-principles.md) already says the user should not need to understand
vector databases, embeddings or RAG. **Everything above this section assumes a reader who does.**

| | Requirement | Source | State | Note |
|---|---|---|---|---|
| **R-89** | A person is **told where knowledge goes, in modeller language**, without learning what a scope is | [GR 1](../../../14-golden-rules.md), owner | **NONE** | *"just for me" · "for the whole company" · "for this project only"* — never `GLOBAL`, `COMPANY`, `PROJECT` |
| **R-90** | Adding a document is **one obvious action**. The user never types a path, names a database, or chooses a folder | [GR 1](../../../14-golden-rules.md), [01](../../../01-vision-and-principles.md) | **NONE** | the store's location is Heron's problem. `heron_scope.py` already decides it and already refuses to guess a project |
| **R-91** | Heron **states plainly what it does and does not do to the file**, before it does it | owner | **NONE** | see the four sentences below. **They must be true, not reassuring** |
| **R-92** | **Deleting everything Heron stored leaves every original file untouched** — and the user is told that | [GR 11](../../../14-golden-rules.md) | **TRUE ALREADY, NEVER SAID** | the index is derived. **The strongest safety promise this system can make, and nothing says it to the person who needs it** |
| **R-93** | **BIM standards are a first-class document kind** — ISO 19650, a company BIM standard, Ashghal BIM requirements, LOD definitions, naming conventions | owner, [bim standards work](../../../00d-additional-requirements.md) | **NONE** | and by [R-88](#l--checking-the-plan-against-the-specification-2026-09-10) a **company** BIM standard outranks an international one: the model knows nothing about it, it carries no licence problem, and it is what a modeller uses daily |

**The four sentences R-91 requires, written here so they cannot drift into marketing:**

> **Your file is not moved.** Heron reads it where it is.
> **Your file is not changed, and not deleted.** Heron only reads.
> **Your file is not uploaded.** When Heron answers a question it quotes the clause it used — that
> sentence travels with the answer, exactly as if you had typed it yourself. **The file never does.**
> **Nothing outside Heron's own folder is touched.**

**The third sentence is the one to keep honest.** *"Nothing leaves your machine"* would be easier to say
and would be **false** — [D-26](../../../DECISIONS.md) settled that the model file never leaves and that
project content in the cloud is acceptable, and a quoted clause is content. **A safety promise that is
90% true is worse than a precise one**, because the 10% is what somebody finds out later.

---

## 7. Deliberately not in scope

Naming these stops the track growing sideways, which is how a reviewable batch becomes an
unreviewable one.

| Not doing | Why |
|---|---|
| Building the eight missing agents as **agents** — separate processes, orchestration | They are *responsibilities*, and a responsibility is met by code that does the job. `HERON-RAG-LIB-001` is a Python module, not a process, and that has been fine |
| A UI for the knowledge library | Nothing to show until R-06 lands |
| Re-opening D-23, D-24 or D-26 | §2 |
| Personal and project **memory** ([docs/10](../../../10-memory-and-knowledge.md)) | Same store, different subject. It follows the document work; it does not share this note |
| **`HERON-RAG-EVO-016`, the Knowledge Evolution Agent** — restructuring the knowledge organisation when it stops fitting | **Found missing by re-reading on 2026-09-10:** it is the one RAG agent with no requirement anywhere in §6, and it was omitted silently rather than deliberately. Naming it here is the fix. It is **T3**, it restructures an organisation that does not exist yet, and it cannot sensibly be specified before something has been organised badly. **Revisit once real documents have been in a real scope long enough to stop fitting** |
| Weakening any fragment's declared words to improve a number | It makes a real fragment unfindable to buy a metric. [`retrieval-history.md`](../../../../brain/retrieval-history.md) has refused this three times, and the refusal is the precedent |
| Anything needing Revit | Nothing on this page does. That is the point of doing it now |

---

## 8. What "done" means

Not *"the code is written"*. Four sentences, each of which somebody can check:

1. **A person can put a real document into a scope**, and get it back with a citation that opens.
2. **A standards question with no indexed source is refused by name**, exactly as `heron_context`'s
   `STANDARDS` path is refused today — the refusal survives the feature landing, it does not soften
   into a guess.
3. **Deleting the whole index and rebuilding it loses nothing**, for documents as for fragments.
4. **[`brain/retrieval-history.md`](../../../../brain/retrieval-history.md) has rows for the `model`
   backend at the current library size, and rows for document retrieval** — each with its date, corpus
   size and backend, so the next reader quotes the file instead of a memory.

Until all four are true this note stays **Active**, whatever the code looks like.
