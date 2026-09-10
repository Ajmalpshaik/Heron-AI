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
| **R-05** | Ingesting into a scope obeys the same wall as querying it — **a document lands in exactly one scope** | GR 5 | **NONE** | nothing ingests |

### B — Ingestion: the half that does not exist

| | Requirement | Source | State | Note |
|---|---|---|---|---|
| **R-06** | A document can be put into a scope at all — PDF, Word, Markdown, plain text | [05 §1](../../../05-heron-brain.md), [00b §19](../../../00b-master-specification-agent-os.md) | **NONE** | no table, no reader |
| **R-07** | A document is stored as **chunks**, each independently retrievable | [05 §4](../../../05-heron-brain.md) | **NONE** | |
| **R-08** | Chunking does not break the exact tokens BIM runs on — `OST_DuctCurves`, a parameter GUID, a clause number | [05 §4](../../../05-heron-brain.md) | **NONE** | the requirement most likely to be got wrong quietly |
| **R-09** | Every chunk carries provenance: **which file, which page or clause, when indexed, who added it** | [05 §8](../../../05-heron-brain.md), `HERON-RAG-CIT-014` | **NONE** | without this, R-21 is impossible |
| **R-10** | The ingester **refuses `.rvt` and `.rfa` by extension** — D-26 enforced by code, never by the user remembering | D-26 | **NONE** | §2 |
| **R-11** | A document is indexed by **content hash**, so re-ingesting an unchanged file costs nothing | [05 §7](../../../05-heron-brain.md) | **DONE for fragments**, **NONE for documents** | `heron_embed` already does it — copy the mechanism, do not invent one |
| **R-12** | Deleting the whole index and rebuilding it is a **safe recovery action**, for documents as it already is for fragments | GR 11, [05 §7](../../../05-heron-brain.md) | **BLOCKED** on R-06 | the source files stay the only authority |

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
| **R-21** | **No source, no claim.** A standards answer without a citation is a **bug**, not a low-confidence answer | [05 §8](../../../05-heron-brain.md) | **NONE** | the most credibility-carrying row on this page |
| **R-22** | A citation resolves to something a human can open — file, page, clause | `HERON-RAG-CIT-014` | **NONE** | |
| **R-23** | Retrieved knowledge is **checked before it is used** | `HERON-RAG-VAL-013` | **NONE** | |
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
| **R-31** | Every retrieval measurement records **date, corpus size, and which backend answered** | [`retrieval-history.md`](../../../../brain/retrieval-history.md) | **RULE HELD, PRACTICE BROKEN** | see below |
| **R-32** | Routing measured by `check-routing.py`, intrusion by `check-intrusion.py` — and neither may weaken a fragment's own words to buy a rank | `tools/` | **DONE**, but **stale** | both last recorded on `lexical`, at 32 and 59 fragments |
| **R-33** | Document retrieval gets its own measurement from its first day | this note | **NONE** | do not let it start unmeasured, the way the fragment side nearly did |

**R-31 is violated right now, and it is the cheapest thing on this page to fix.**
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
| **R-34** | An answer says **how contested it was** — the spread across the shortlist in units of one fusion rank, and whether the routes agreed | [00 §3.1](00-structure.md) | **NONE** | `heron_retrieve.py` computes all of it and throws it away |
| **R-35** | A shortlist that is effectively tied **says so, in words** — not only as a number | [00 §3.1](00-structure.md), S-3 | **NONE** | at 14 fragments the top five spanned 0.0021 where one rank is 0.00026, and nobody was told |
| **R-36** | A chunk knows its **parent**, so a clause can be returned with the section it sits in | [00 §3.3](00-structure.md) | **NONE** | extends `heron_context`'s existing `depth`, never a second mechanism |
| **R-37** | Hierarchy depth is **arbitrary, stored as a parent link** — not a fixed Part/Section/Clause | [00 §3.3](00-structure.md), S-2 | **NONE** | fixed levels are a guess about documents nobody has read yet |
| **R-38** | The Librarian picks **one** scope. Two scopes means **two queries**, never one merged query, and `CrossScopeRefused` is unchanged | [00 §3.4](00-structure.md), GR 5, D-33 | **NONE** | **the trap in this whole track.** A `UNION` here is a contractual problem, not a technical one |
| **R-39** | A **third retrieval route follows graph edges**, over **documents only**, and **only if a density count passes first** | [00 §3.2](00-structure.md), [34 §2.13](../../../34-patterns-adapted.md) | **NONE, and conditional** | **the same route over the fragment graph was measured at six settings and lost every one.** Density decides it: median 50 neighbours, worst 230, is competitors rather than signal |
| **R-40** | Document nodes and their edges are **derived on demand**, like every edge but one | [00 §3.2](00-structure.md), D-40 | **NONE** | a stored document edge is a cache that goes stale |
| **R-41** | A cross-encoder re-ranks the top ~20, and **its absence makes Heron slower to be right, never broken** | [00 §3.5](00-structure.md), [05 §4.4](../../../05-heron-brain.md) | **NONE** | the same fallback contract `heron_embed.py` already honours |
| **R-42** | The re-ranker installs **per-user with no administrator rights** | [00 §3.5](00-structure.md), D-01 | **NONE** | `model2vec` already proved this is possible |
| **R-43** | The context packet carries **citation, confidence, and what is missing** — enough that the host answers without inventing | [00 §3.6](00-structure.md) | **NONE** | this is what "generation" means here |
| **R-44** | **No language model inside `brain/`.** The host writes the reply the user reads | [00 §3.6](00-structure.md), D-01 | **DONE, and must stay done** | `check-metadata.py` prints it as fact |
| **R-45** | When the clause store exists, the `STANDARDS` refusal **narrows** to *"nothing indexed covers this"*. It never softens into a guess | [00 §3.6](00-structure.md) | **NONE** | a system that refused honestly while empty and guessed once full would be worse than the one that refused |

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
