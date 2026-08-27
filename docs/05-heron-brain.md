# 05 — Heron Brain (Part 3)

> Derived from [Master Specification](00-master-specification.md) §6, §15, §26, §27.
> Skills and Fragments have their own document: [09](09-skills-and-fragments.md).
> Memory and knowledge scopes have their own document: [10](10-memory-and-knowledge.md).
> **[NOTE]** blocks are engineering commentary added during review.

---

## 1. Responsibility

The intelligence and knowledge layer: Skills, Fragments, RAG, vector database, embeddings, knowledge library, personal memory, project memory, company knowledge, community knowledge, fragment lifecycle, fragment evolution, knowledge validation, knowledge ranking.

## 2. Knowledge is separated, never pooled

```text
CORE KNOWLEDGE
|
+-- COMPANY KNOWLEDGE
|
+-- PROJECT KNOWLEDGE
|
+-- USER KNOWLEDGE
|
+-- COMMUNITY KNOWLEDGE
|
+-- TEMPORARY KNOWLEDGE
|
+-- EXPERIMENTAL KNOWLEDGE
```

This is **Golden Rule 5**. A polluted brain is unsearchable and untrustworthy, and in a consultancy setting mixing Project A's knowledge into Project B is a commercial and contractual problem, not just a technical one.

---

## 3. RAG Department

| Agent | Responsibility | Tier |
|---|---|---|
| RAG Librarian Agent | Decide *which scopes* to search | T1/T2 |
| Knowledge Discovery Agent | Find potentially relevant information | T1 |
| Retriever Agent | Retrieve candidates | T1 |
| Fragment Matcher Agent | Match request against existing fragments | T2 |
| Skill Matcher Agent | Find applicable skills | T2 |
| Ranking Agent | Rank retrieved information | T1/T2 |
| Context Builder Agent | Assemble task context | T1 |
| Embedding Agent | Create embeddings | T1 |
| Vector Search Agent | Search the vector DB | T1 |
| Index Manager Agent | Maintain indexes | T1 |
| Re-index Agent | Update indexes on change | T1 |
| Duplicate Detection Agent | Find duplicate knowledge | T1 |
| Knowledge Validation Agent | Check retrieved information | T2 |
| Citation/Source Agent | Track provenance | T1 |
| Knowledge Evolution Agent | Restructure knowledge when required | T3 |

---

## 4. **[NOTE]** Retrieval is not only vector search

The spec leans heavily on embeddings. For this domain that is the wrong default on its own.

BIM and Revit queries are full of **exact tokens** that embeddings handle badly: `OST_DuctCurves`, `BuiltInParameter.RBS_DUCT_BOTTOM_ELEVATION`, `ElementId`, a shared-parameter GUID, `Revit 2024`. Pure semantic search will confidently return the *nearly* right parameter, which in a model-modifying system is worse than returning nothing.

**Recommended retrieval stack:**

1. **Structured filter first** — scope, domain, Revit version, status. This is a SQL `WHERE`, not a search. It eliminates most of the corpus for free and enforces Golden Rule 5 at the query level.
2. **Hybrid search** — BM25/FTS keyword search **and** vector search over the survivors.
3. **Reciprocal rank fusion** to merge the two result sets.
4. **Re-rank** the top ~20 by fragment quality signals (status, success rate, recency, version match).
5. **Exact-match short circuit** — if a proven fragment's semantic identity matches the request exactly, skip the model entirely and execute it.

That last point matters most: the common case ("select all ducts", asked for the 400th time) should cost **one lookup**, not a retrieval pipeline.

## 5. **[NOTE]** Vector store choice

Requirements: local-first, embeddable, no server to install, works offline, handles metadata filtering well, and survives being copied around with the workspace folder.

| Option | Assessment |
|---|---|
| **SQLite + `sqlite-vec`** | Single file, zero install, real SQL for the structured filter, FTS5 for keyword search — all three retrieval stages in one engine. Strong default. |
| **LanceDB** | Good embedded option, columnar, strong filtering. |
| **ChromaDB** | Easy to start, heavier at scale, weaker structured filtering. |
| **Qdrant / Weaviate / pgvector** | Server-based. Rejects the "user installs nothing" requirement. |

**Recommendation: SQLite + `sqlite-vec` + FTS5.** One file per knowledge scope keeps scope separation physical rather than merely logical — which makes Golden Rule 5 impossible to violate by accident, and makes "archive this project" a file copy.

Tracked as [Q-10](OPEN-QUESTIONS.md).

## 6. **[NOTE]** Embedding model choice — the offline question

Embeddings can be produced by a cloud API or a local model.

| | Cloud embeddings | Local embeddings |
|---|---|---|
| Quality | Higher | Good enough for this corpus |
| Cost | Per token, forever, including re-indexing | Zero |
| Offline | No | Yes |
| **Sends project data off the machine** | **Yes** | **No** |

The last row is decisive for BIM consultancy work. Indexing a project's knowledge means sending project content to a third party. Many client contracts — and effectively all government or defence work — prohibit that.

**Recommendation: local embedding model by default** (a small sentence-transformer class model), with cloud embeddings as an opt-in for users who have no such restriction. This also makes re-indexing free, which the background system (§48) depends on.

Tracked as [Q-11](OPEN-QUESTIONS.md) and the wider data-residency question [Q-12](OPEN-QUESTIONS.md).

---

## 7. Vector database maintenance

Automatic background operations: new fragment indexing, updated fragment indexing, deleted fragment removal, embedding updates, duplicate detection, index repair, stale knowledge detection, metadata synchronisation.

The user never manages the vector database manually.

**[NOTE]** Two things make this safe and cheap:

- **Content-hash based indexing.** Re-embed only when content actually changed, not when a file's mtime moved. Otherwise every git operation triggers a full re-index.
- **The index is derived, never authoritative.** Fragments and skills live as files with metadata (git-tracked, human-readable, diffable). The vector DB is a rebuildable cache. If it is ever corrupt, deleting it must be a safe recovery action. Never store the only copy of knowledge inside the vector store.

---

## 8. **[NOTE]** Knowledge validation is where trust is won or lost

`Knowledge Validation Agent` and `Citation/Source Agent` are listed almost in passing, but they carry the platform's credibility.

Two failure modes to design against explicitly:

1. **Confident wrong retrieval.** A fragment written for Revit 2021 gets applied in 2025 and silently misbehaves. Mitigation: version compatibility is a **hard filter before ranking**, never a soft signal.
2. **Invented standards.** §47 already forbids this. Enforce it structurally: any claim about ISO 19650, QCS, Ashghal or a company standard must carry a citation to an indexed source document. **No source, no claim.** A standards answer with no citation is a bug, not a low-confidence answer.
