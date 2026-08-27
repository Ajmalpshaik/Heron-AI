# 10 — Memory & Knowledge Scopes

> Derived from [Master Specification](00-master-specification.md) §15, §28–35, §59.
> **[NOTE]** blocks are engineering commentary added during review.

---

## 1. Memory scopes

| Scope | Contents | Shared with |
|---|---|---|
| **Global / Core** | General Heron knowledge | everyone |
| **Company** | Company standards and approved knowledge | the company |
| **Project** | Project-specific information | that project only |
| **User** | Working preferences, successful personal patterns | nobody, by default |
| **Temporary** | Short-term task context | discarded |
| **Experimental** | Unproven knowledge | quarantined |

> These must never be mixed blindly. — **Golden Rule 5**

## 2. Project context isolation

```text
Heron AI
 |
 +-- Company
 |
 +-- Project A
 |     +-- Standards / Skills / Fragments / Memory / Documents
 |
 +-- Project B
       +-- Standards / Skills / Fragments / Memory / Documents
```

Project information must not contaminate another project.

**[NOTE — this is a commercial requirement, not just a technical one]** In consultancy and contracting work, Project A and Project B frequently belong to **different clients**, often under NDA. Leakage between them is a contractual breach, not an inconvenience.

Enforce it physically, not by convention:

1. **One database file per scope.** A cross-project query is then impossible by construction, not merely discouraged.
2. **Scope is resolved before retrieval**, from the active Revit document, and is never inferred by a model.
3. **Promotion is always explicit and upward** — Project → Company → Community requires a human decision at each step. There is no automatic path.
4. **Cross-project search requires an explicit, logged opt-in** ("search all my projects"), and the result must show which project each answer came from.

---

## 3. Personal learning (§34)

If a user repeatedly performs a workflow successfully — *"select all ducts and move them 200 mm up"* — Heron may recognise a reusable personal pattern.

> Personal knowledge stays personal by default. It does not automatically become community knowledge.

**[NOTE]** Two guards worth building in:

- **Learn the pattern, not the numbers.** The reusable thing is *select by category → move by vertical offset*, parameterised. Hard-coding "200 mm" produces a brittle, single-use pattern.
- **Confirm before saving a pattern.** *"You've done this 5 times. Save it as a shortcut?"* Silent learning is unsettling; a one-line confirmation converts it into a feature the user feels ownership of.

---

## 4. Community knowledge (§35)

```text
User Knowledge -> Candidate -> Review -> Validation -> Community Proposal
              -> GitHub Pull Request -> Review -> Approved -> Community Knowledge
```

**User approval is required before personal knowledge is submitted externally.**

**[NOTE]** Strengthen this to a hard rule, because a mistake here is unrecoverable:

> **Nothing derived from a client project may leave the machine without explicit, per-item human review of the actual content being sent.**

A fragment carries usage history, failure history, and sometimes literal parameter names, family names or project identifiers. A "generic" duct-selection fragment can easily contain `PROJ-QA-2026-Ashghal-DuctType-A`. Automated sanitisation is not sufficient — a human must see the exact payload. This is `PUBLISH` level, always, no exceptions and no remembered consent.

---

## 5. Knowledge import (§28, §29)

*"Import this folder into Heron."*

Scan → identify file types → identify code → fragments → skills → documentation → metadata → duplicates → compatibility → classify → rename → move into architecture → update metadata → validate → index → save.

**[NOTE]** This is one of the most immediately valuable features in the spec — the user already has years of accumulated Revit tooling (`AJ-Tools`, `PyRevit-Tools`, `AEB-Tools`), and importing it is what makes Heron useful on day one rather than after a year of accumulation.

Design constraints:

1. **Import never modifies the source folder.** Read-only, always. Copy in; never move or rename in place.
2. **Import produces a reviewable manifest**, not a fait accompli: *"Found 47 candidate fragments, 12 look like duplicates of existing knowledge, 6 have unknown Revit compatibility."* The user reviews before anything is committed.
3. **Everything imported starts at `DISCOVERED`.** Nothing arrives as PRODUCTION knowledge — that would violate Golden Rule 6 on the very first day.
4. **Provenance is recorded** — source path, original filename, import date, importing version. When something behaves strangely in six months, this is the only way to find out where it came from.
5. **Import is resumable.** A folder with thousands of files will fail partway through at some point.

Tracked as [Q-16](OPEN-QUESTIONS.md) — which existing repositories are in scope for the first import.

---

## 6. Naming and identity

Naming must be predictable and searchable (§31), but identity is an **ID**, never a name (§30). Rename freely; identity survives.

**[NOTE]** Order of operations: **identity first, auto-rename second.** An Auto Rename Agent let loose on a knowledge base that still identifies things by filename will silently destroy the reference graph. The Reference Update Agent exists for exactly this reason and must run in the same transaction as any rename.

---

## 7. **[NOTE]** What "memory" must not become

A caution, from experience with systems of this shape.

An unbounded memory that records everything degrades in a predictable way: it accumulates stale facts, contradicts itself, and retrieval quality falls as the corpus grows. The spec's `Knowledge Evolution Agent` and `stale knowledge detection` acknowledge this, and they should be treated as load-bearing rather than nice-to-have.

Three practical defences:

| Defence | Rule |
|---|---|
| **Expiry** | Temporary memory has a TTL. Project memory is archived when the project closes. |
| **Supersession** | New knowledge that contradicts old knowledge **replaces** it and records the replacement. It does not sit beside it. |
| **Budget** | Each scope has a size budget. Approaching it triggers consolidation, not silent growth. |

The measure of a good memory system is not how much it remembers. It is how reliably it returns the right thing — which usually means remembering **less**, and more deliberately.
