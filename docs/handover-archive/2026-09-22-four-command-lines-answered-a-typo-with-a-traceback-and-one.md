# Session note — FOUR COMMAND LINES ANSWERED A TYPO WITH A TRACEBACK, AND ONE ANSWERED IT

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — FOUR COMMAND LINES ANSWERED A TYPO WITH A TRACEBACK, AND ONE ANSWERED IT

**[Row 5b-104](../FRAGMENT-ISSUES.md). FIXED.** And `brain/heron_search.py` read end to end with
**nothing found** — recorded below so nobody reads it again.

**THE SCAN WAS WRONG ABOUT TWO OF THE SIX IT FOUND, SO EVERY ONE WAS RUN.** `grep` for `argv[i + 1]`
hits eight sites. `heron_ingest`, `heron_ground` and `tools/check-products.py` all refuse properly —
`check-products` with `if i + 1 >= len(argv)` three lines above the read. **That is [row
5b-95](../FRAGMENT-ISSUES.md)'s lesson for the fifth time this week: shape is not behaviour.**

What the four actually did, each with the flag last on the line:

| | measured |
|---|---|
| `brain/heron_conflict.py --scopes`, `--project` | `IndexError: list index out of range`, exit 1 |
| `brain/heron_retrieve.py --revit` | the same |
| `tools/check-routing.py --revit` | the same |
| `brain/heron_company.py --subject`, `--stage` | **exit 0**, and it searched for `'how thick is duct insulation --subject'` |

**THE COMPANY ONE IS WHY THIS IS ONE ROW AND NOT THREE.** It is *guarded* — `and i + 1 < len(argv)` —
so instead of crashing, the flag falls through to `words.append` and becomes part of the question. The
comment three lines above it says exactly that must not happen: *"a `--subject` swallowed into the
question would search for the word `--subject`"*. **The guard written to prevent it is what produced
it**, and an honest empty answer about a question nobody asked is worse than a crash, because it reads
as a measurement.

**AND TWO OF THE OTHER THREE SAT UNDER A COMMENT DESCRIBING THEIR OWN FAILURE.**
`heron_retrieve.main` refuses every *unknown* flag by name three lines below, because *"`--rebuild` was
silently searched for, matched nothing, and printed 'nothing matched', which reads as a measured result
rather than a typo"* — and the only flag it **has** crashed. `check-routing.main` states [row
5b-71](../FRAGMENT-ISSUES.md)'s rule four lines below: exit 2 and a sentence, never an unhandled traceback.

**The house answer already existed in the same stage**: `heron_research._flag` refuses by name and
exits 2, and `heron_ingest._flag` adds the reason for refusing a value that is itself a flag.

**Shown to FAIL, and every check FAILS rather than raises:**

| suite | red against the code as found |
|---|---|
| `tests/test_conflict.py` §12 | **3** |
| `tests/test_retrieve.py` §8 | **2** |
| `tests/test_company.py` §9 | **4** |
| `tests/test_check_routing.py` (new) | **red — after printing a full routing table for the Revit release called `--rebuild`** |

That last one is the defect demonstrating itself, and it is why `--revit --rebuild` is a check and not
only `--revit`.

**`tools/check-routing.py` HAD NO SUITE AT ALL**, though `gates.yml` runs it as one of its twelve. It
has one now, and that suite is also **the first thing ever to hold [row 5b-71](../FRAGMENT-ISSUES.md)** —
*no knowledge store is exit 2 and a sentence*, which was fixed on 2026-09-21 and held by nothing since.

> **THE RETRIEVE CHECK HAD TO BE GUARDED TWICE, and that is heron-ship §2a in one line.** Against the
> module as found, `--revit --rebuild` does **not** raise on the flag — it takes `--rebuild` as the
> release and walks on into `open_scope()`, which raises because the block above has already removed
> `HERON_KNOWLEDGE`. The first version of the check ended the suite in a traceback, **which proves
> nothing**. Every check in this change catches `BaseException` and records it as a **FAIL**.

#### `brain/heron_scope.py` — the loophole it exists to close is open through a public attribute

**[Row 5b-108](../FRAGMENT-ISSUES.md). OPEN, measured, not fixed — and it is the one on this list that
touches Golden Rule 5.** 462 lines, read end to end.

The module's headline is *"A cross-scope query must be impossible to **WRITE**, not merely absent"*,
and it names the two things that make it so: `open_scope` takes one scope, and **ATTACH is refused** —
*"SQLite's ATTACH DATABASE is the one mechanism that could reach a second file through a connection
that legitimately holds one ... **Without this, rule 1 is a convention with a loophole.**"*

**The check lives in `Store.execute()`. `Store.db` is the raw `sqlite3.Connection` and it is public.**

Measured end to end on a temporary knowledge folder:

| route | what happened |
|---|---|
| `store.execute("ATTACH …")` | **refused by name**, `CrossScopeRefused` |
| `store.db.execute("ATTACH …")` | **attached the company store to the global store's connection, and read a value straight out of it** |

**And `store.db` is not an obscure corner.** `store.db.execute` / `.executescript` is the normal way
DDL is run here — **8 call sites across 5 modules**: `heron_search` (3), `heron_embed` (2),
`heron_capability`, `heron_ingest`, `heron_graph`.

> **THE SEVERITY, STATED HONESTLY. This is a hole in a guarantee, not a live leak.** Nothing in the
> repository writes an `ATTACH` through either route, and all eight `store.db` uses are ordinary
> `CREATE TABLE` / `ALTER TABLE`. What is false is the *impossible to write* claim — which is the
> claim the module is built around, and `docs/10 §2` calls Golden Rule 5 a **contractual** matter
> rather than a technical one.
>
> **Two mechanisms, and choosing is a judgement.** `sqlite3.Connection.set_authorizer` denying
> `SQLITE_ATTACH` closes **every** route at the engine, in a few lines, with no call site changed —
> the database refusing rather than a regular expression. Wrapping the connection keeps the named
> refusal this module is careful about but touches how every DDL site is written. **Probably both.**
> A change to a boundary like this deserves its own commit and its own test.

#### `brain/heron_embed.py` — the write side got the fix twice and the read side never did

**[Row 5b-107](../FRAGMENT-ISSUES.md). OPEN, measured, not fixed.** 579 lines, read end to end.

`nearest()`'s own comment inside the scoring loop says *"A vector from a different **backend** or
dimension. Not comparable, and quietly comparing it would produce a confident wrong number."* **The
test beside it is `len(got) != len(want)`.** The row's `backend` column is never read — the query
selects `id, embedding` filtered on `kind` alone.

**Measured:** a row stamped `model:model2vec:minishlab/potion-base-8M` carrying 256 floats is scored
against a **lexical** query vector and comes back in the result. `DIMS` is 256, and a trained model at
256 is not hypothetical — `HERON_EMBED_MODEL` lets a person name any model.

**The file already names this failure twice, and both fixes landed on the WRITE side.**
`_WHICH_MODEL`: *"at the same dimension it computes meaningless cross-model dot products ... the
semantic route quietly stops working and nothing says so."* `encoder()`: it exists because resolving
the model per **row** inside one pass stored two encoders' work under one name. **`nearest()` is the
read side, and it still calls `vector()`, which resolves the model per call.**

**The window is narrow and it is stated rather than talked up.** `heron_brain._Open` runs
`EMBED.index(store)` on every open, and `index()` re-embeds any row whose stamp differs from the
encoder answering now — so the store is normally normalised before anything is compared. What is left
is the gap **between that pass and the `nearest()` call in the same request**: `brain.warm()` imports
the trained encoder on a background thread at server start, so a warm-up finishing in that gap gives a
MODEL query vector over a LEXICAL store at equal dimensions, with nothing to catch it.

> **The comment is wrong either way and that half needs no judgement. The behaviour half does:**
> filtering `nearest()` to the current stamp would make that request return **nothing** rather than
> noise, and a silent nothing is [D-52](../DECISIONS.md)'s plausible zero. What a mismatch should
> *produce* is a decision, not a repair.

#### Two more read end to end, and one of them had a finding

**`brain/heron_rerank.py` (348 lines) — NOTHING FOUND, and it is the best-held module in `brain/`.**
Eleven sections in `tests/test_rerank.py` and every claim the file makes has one, including the newest
guard: a backend returning the **wrong number** of scores is refused rather than aligned by guesswork.
Worth knowing rather than re-deriving: the offline switch is **per-load and never process-wide**,
because the first version set `HF_HUB_OFFLINE` globally while `heron_brain.warm()` starts the
**encoder's** loader on another thread — so retrieval could silently drop to `lexical` for the life of
the process on a machine with a perfectly good network; `warm()` imports torch on a background thread
because `A8` is thirty real minutes of an MCP call waiting on a 1.0 s import on the event loop, and
`_load()`'s thread check is **load-bearing**, not decoration; and `scores()` returns `None` for every
way it can fail, because a caller that must wrap it in a `try` has a re-ranker that can break the
answer.

**`brain/heron_skill.py` (240 lines) — [row 5b-106](../FRAGMENT-ISSUES.md), OPEN and not fixed here.**
The module states its central rule in capitals — *"A SKILL NAMES CAPABILITIES, NEVER FRAGMENTS"* — and
`validate()` carries the matching refusal word for word. **The test behind that sentence is
`capability.isupper()`, and a fragment id passes it**: measured, `needs: [FRG-ELE-001]` validates
clean, because `'FRG-ELE-001'.isupper()` is `True`. What it refuses is a lowercase string and a
non-string, and nothing else.

**And the consequence is a wrong instruction rather than silence.** `main()` lists anything no fragment
provides under **CAPABILITY GAPS**, closing *"It is what to build next, in the order real work asks for
it - not a guess"* — so naming a fragment produces an instruction to build a capability called
`FRG-ELE-001`.

**The repair needs no judgement, and the measurement is why.** A capability is SCREAMING_SNAKE and a
fragment id is not: **0 of 396 fragment ids** match `^[A-Z][A-Z0-9_]*$` — every one carries hyphens —
and **396 of 396 capabilities** do. Checked against every `needs` entry that exists: **38 across the
ten skills, 22 distinct, and the stricter rule refuses none of them.**

#### `brain/heron_search.py` — read end to end, 1,047 lines, NOTHING FOUND

The thinnest-held unread live-path module left after `heron_ingest`. **Recorded so nobody reads it
again.**

**One dead branch, and it is not a defect.** In `index()`'s clash handling,
`if clash and clash["fragment_id"] == "__ambiguous__": continue` can never be reached — the branch
above it already catches an ambiguous row, because `"__ambiguous__"` is not this fragment's id, and
produces the same state. Identical behaviour, so no row.

**Four public functions are named by no suite** — `library_digest`, `ensure_chunk_table`,
`chunk_breadth`, `match_breadth` — **and all four are reached internally.** That is the measurement
that stopped a row being written out of a shape.

**Checked rather than assumed**: `match_breadth`'s `keep` and `chunk_breadth`'s `statuses` both default
to the **old** behaviour their own docstrings call a defect — and **both** call sites in
`heron_retrieve` pass the narrowing argument, so the fix is complete and the default is only backwards
compatibility.

Worth knowing rather than re-deriving: `_fts_query` prefix-matches only words over three characters,
because five stopword prefixes outvoted the one word that carried meaning; `INDEX_FORMAT` must be
bumped whenever `index()` derives anything differently or a **code-only repair never takes effect**;
the skip is taken only when **both** derived tables have rows, because a digest says the input is
unchanged and not that the output is there; `may_run_unasked` can only ever **withhold**, and only
when the index and the file disagree; and `remember()` takes `RAN` and nothing else, because caching
a candidate makes a guess the fast path.
