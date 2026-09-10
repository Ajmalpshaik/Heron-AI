# RAG — the implementation note

> **Type:** Operational work note. **Not specification.** Where a sentence here disagrees with the
> [Constitution](../../../../HERON_CONSTITUTION.md), the [Golden Rules](../../../14-golden-rules.md) or
> [DECISIONS.md](../../../DECISIONS.md), **those win and this note is out of date.**
> **Status:** **Active — no stage started.** Opened 2026-09-10.
> **Reads with:** [`00-structure.md`](00-structure.md) — what kind of RAG this is, and the order ·
> [`01-requirements.md`](01-requirements.md) — every stage below closes numbered rows from its §6,
> and a stage that closes no row should not be built.
> **Progress is not recorded here.** It is recorded in [`03-working-note.md`](03-working-note.md).
> This file says what the plan *is*; that one says what actually happened.

> **⚠ The stage list in §3 to §9 is SUPERSEDED.** It was written before the six structural
> questions were decided on 2026-09-10. **The order to follow is
> [`00-structure.md` §6](00-structure.md)**, which keeps every stage below and adds **0b** (say the
> confidence out loud) and hierarchy inside Stage 1. The stage *contents* here are still correct and
> still the detail — it is the sequence that moved.

---

## 1. The whole job, in one sentence

**Heron can already find its own code. It cannot yet read a document.** This track gives the knowledge
store somewhere to put a document, a way to get it back with a citation that opens, and a refusal when
there is nothing to cite.

Everything else on this page is that sentence, in an order that keeps each step provable.

---

## 2. Rules that bind every stage

These are not new. They are the repository's own, gathered here so that no stage has to rediscover one.

| Rule | Where it comes from | What it forbids |
|---|---|---|
| **The index is derived, never authoritative** | [GR 11](../../../14-golden-rules.md), [05 §7](../../../05-heron-brain.md) | Storing the only copy of anything inside the store. Deleting the index must stay a safe recovery action **after** documents exist, not only before |
| **One store per scope, cross-scope impossible to write** | [GR 5](../../../14-golden-rules.md), D-33 | An ingester that takes a list of scopes. It takes **one** |
| **No `.rvt`, no `.rfa`, ever** | D-26 | Trusting the user to point the ingester somewhere safe |
| **Content hash, never mtime** | [05 §7](../../../05-heron-brain.md) | Re-embedding after a `git checkout` |
| **Never type a count a command can derive** | [`work-notes/README.md`](../../README.md) | Writing *"1,200 chunks indexed"* into any document here |
| **Record date, corpus size and backend, or do not record** | [`retrieval-history.md`](../../../../brain/retrieval-history.md) | A retrieval number with no library size beside it |
| **Never weaken a source's own words to improve a number** | [`retrieval-history.md`](../../../../brain/retrieval-history.md) | Trimming a document's text because it intrudes on shortlists |
| **The metadata header** | [`docs/29`](../../../29-metadata-standard.md) | A new `.py` in `brain/` without `Heron-Agent`, `Heron-Step`, `Heron-Status`, `Heron-Since`, `Heron-Layer` |
| **`brain/` never references Revit** | [`brain/README.md`](../../../../brain/README.md) | Any `Autodesk.Revit.*` import creeping in through a document reader |
| **A defect found while working is recorded, not fixed** | [`work-notes/README.md`](../../README.md) | Widening a reviewable stage into an unreviewable one |

**Gates before any push**, in the order the [`heron-ship`](../../../../.claude/skills/heron-ship/SKILL.md)
skill gives them:

```bash
python tools/check-docs.py          # links, and every count that can be derived
python tools/check-metadata.py
python tools/check-structure.py
python tools/check-gaps.py          # unfinished versus merely waiting
```

---

## 3. Stage 0 — measure what already exists, and write it down

**Closes:** R-31, R-32. **Needs:** nothing — no PC, no Revit, no network beyond the model already
cached. **Cost:** an hour. **Do this first even if the rest is postponed.**

[`brain/retrieval-history.md`](../../../../brain/retrieval-history.md) is the file that exists to stop
a retrieval number being quoted from memory. It currently has **eleven rows, all `lexical`**, the last
at **59** fragments on 2026-08-31. The library is now **360** and the backend is **`model`**. The file
is describing a system that no longer exists.

**What to run**, exactly as its own §*How to add a line* says:

```bash
python brain/heron_scope.py --rebuild
python brain/heron_retrieve.py "show me every duct in the model" --revit 2024
python brain/heron_embed.py
python brain/heron_fragment.py | tail -3
python tools/check-routing.py
python tools/check-intrusion.py
```

**What to write:** a new dated section in `retrieval-history.md` — not an edit to the old rows, which
are true of the backend they name. The measurement already taken is in
[`03-working-note.md`](03-working-note.md); it is evidence, not a substitute for running it again on
the machine that will carry the work.

**What NOT to do.** Both suites carry a prediction that the trained backend would break them. It did
not break them — `test_embed.py` and `test_retrieve.py` both exit 0. **Do not repair the
assertions to match a story.** The test comment says it plainly: *"when it does, re-read it rather than
repairing it."* The finding that the prediction was wrong is worth more than a green tick that agrees
with an old comment.

**Done when:** `retrieval-history.md` has a section headed with today's date, the fragment count and
the word `model`, and the two checkers have a recorded run at the current size.

---

## 3a. Stage 0b — say the confidence out loud, then act on it

**Closes:** R-34, R-35, and **R-56 to R-62**. **Needs:** nothing — no PC, no Revit, no documents.
**Do it second, straight after Stage 0.**

Every number this stage needs **already exists and is discarded**. `heron_retrieve.py` computes the
fused score, both route ranks, and whether the routes agreed; it returns a ranked list and throws the
rest away.

### 3a.1 One measurement, three thresholds

Build the measurement **once**. Three things then read it:

| | Threshold | Behaviour |
|---|---|---|
| **Report** | none | say how contested the shortlist was, in words and in units of one fusion rank |
| **Drop** | per-candidate | a candidate below it is **removed from the shortlist and counted** |
| **Refuse** | all candidates | nothing above the floor means the question is **refused by name** |

**Three separate implementations would produce three numbers that disagree**, and the disagreement
would surface as a shortlist that says it is confident about candidates it also dropped.

### 3a.2 The order to build it

| | Step | Test it with |
|---|---|---|
| 1 | **Expose what is already computed** — spread across the shortlist, in units of one fusion rank (R-34) | one clear question and one genuinely ambiguous one; the reported spread differs in the right direction |
| 2 | **Say it in words as well as a number** (R-35) | `heron_retrieve` already prints *"both routes agree"*; extend that vocabulary rather than adding a second one |
| 3 | **Pool awareness** (R-61) | with fewer than 20 eligible candidates, agreement is reported as **not evidence** — the case `retrieval-history.md` recorded |
| 4 | **Drop, and count** (R-56, R-57) | the 59-fragment case: a question about ducts must not return `find-sheets`, and must **say** it dropped it |
| 5 | **Refuse** (R-58, R-59) | a question with no BIM content is refused; and the refusal is **not** the same sentence as an empty store or an all-version-blocked result |

**Step 4 has a recorded test case already written for you.** At 59 fragments the top five for *"show me
every duct in the model"* were `dimension-mep-runs`, `find-views`, `set-selection`, `find-sheets`,
`dimension-family-instances` — and the file says plainly that three of them **have no claim on that
sentence at all**, two of which **write to the model**. If step 4 works, that shortlist gets shorter.

**What this stage must not do.**

- **Do not add a keyword list of BIM words** (R-60). It would be wrong the week it was written.
- **Do not classify the question** (R-62). D-01 gives that to the host. The brain says *nothing here has
  a claim*; it does not say *you meant something else*.
- **Do not move a floor to make a report look better** (R-55, and it extends here). If the drops are
  wrong, the measurement is wrong.

**Done when:** a question with no BIM content is refused by name; the 59-fragment shortlist case drops
its no-claim candidates and says so; the three nothings read differently; and each is a test.

---

## 4. Stage 1 — a document can go in

**Closes:** R-05, R-06, R-07, R-08, R-09, R-10, R-11. **Needs:** nothing. **This is the stage the whole
track exists for.**

### 4.1 The schema

Two tables, in the scope's own file, beside the fragment tables — **not** in a second database. One
file per scope is what makes GR 5 physical, and a separate document store would break that on day one.

```text
documents        id            content hash of the file, so re-ingest is free
                 scope         the store it went into (redundant, and worth it — see below)
                 path          where it came from
                 title         what a citation shows a human
                 kind          pdf | docx | md | txt
                 added_utc     when
                 added_by      who
                 source_trust  who says this is authoritative

chunks           id            document id + ordinal
                 document_id   FK
                 ordinal       position in the document
                 locator       page number, clause number, heading — what a citation POINTS AT
                 text          the chunk itself
```

`scope` is stored on the row even though the file already is the scope. It costs one column and it
means a store that is ever copied, merged or recovered by hand carries its own answer to *whose
knowledge is this?* — which is a contractual question, not a technical one.

`locator` is the column the entire citation requirement rests on. **A chunk with no locator can be
retrieved but cannot be cited**, and R-21 says an uncitable standards answer is a bug. Get this column
right before writing a single reader.

### 4.2 The ingester

New file, `brain/heron_ingest.py`, carrying `Heron-Agent: HERON-RAG-DIS-002` and the rest of the
header [`docs/29`](../../../29-metadata-standard.md) requires.

It takes **one scope**, never a list. It refuses:

| Refuses | Because |
|---|---|
| `.rvt`, `.rfa` | **D-26.** By extension, before the file is opened. Not a warning — a refusal, by name |
| `.rte`, `.rft` — Revit templates | **Not D-26**, which names only RVT and RFA. Proposed here on the same reasoning and **needs the owner's word** before it is coded, because a refusal nobody agreed to is as surprising as a leak |
| A project scope with no project identified | D-33, already how `heron_scope.resolve` behaves — call it, do not re-implement it |
| A file it cannot read | Naming the file. A reader that silently skips is how half a standard goes missing without anybody noticing |

It re-uses the content-hash mechanism [`heron_embed.py`](../../../../brain/heron_embed.py) already has.
**Do not write a second one.** The existing one is measured, and two hashing schemes in one store is a
future afternoon lost to *which one is stale?*

### 4.3 Chunking, and the one thing that must not go wrong

[`05 §4`](../../../05-heron-brain.md) is explicit that this domain is full of exact tokens embeddings
handle badly — `OST_DuctCurves`, `BuiltInParameter.RBS_DUCT_BOTTOM_ELEVATION`, a shared-parameter GUID,
`Revit 2024`, `QCS 2014 §21.3.2`. A chunker that splits on a fixed character count **will** cut one of
those in half, and the half is worse than useless: it retrieves confidently and is wrong.

So: **split on structure first — heading, clause, paragraph — and only fall back to length.** A chunk
that had to be split by length says so on the row, so a later reader can see which chunks are the
risky ones rather than guessing.

**Write the test before the chunker.** Feed it a paragraph containing each of the five token shapes
above and assert none of them is split. That test is the requirement; the chunker is an implementation
of it.

#### And three more chunker-time decisions, added 2026-09-10

All from [`the field reading`](../../investigations/rag-state-of-the-art-2026-09-10.md), all designed
in [`00-structure.md` §3.8](00-structure.md), and **all cheap here and expensive anywhere else.**

| | Do | Test it with |
|---|---|---|
| **R-66, R-67** | Prepend the **heading path** to a chunk's indexed text — *QCS 2014 → Section 21 → 21.3 Ductwork → 21.3.2* — read from structure, **never generated** | a clause found by a question using its **section's** vocabulary and not its own. And a check that the path came from the parent link, not from a model |
| **R-68** | **A rule and its exception stay in one chunk.** A candidate split immediately before *except* / *unless* / *provided that* / *save that* / *however* is **not** a split point | *"Ducts shall be insulated … except where installed within conditioned spaces."* One chunk. **Write this test first** |
| **R-69** | The ingester can **print one document's chunk boundaries** for a person to read | run it on the first real standard and read the output before trusting anything downstream |

**R-68 is the one to write first, ahead of even the token test.** Get it wrong and Heron states the
opposite of a requirement **with a citation attached** — which is more convincing than any uncited
guess, and is the worst output this system is capable of.

### 4.4 What Stage 1 does NOT do

No retrieval. No citation. No re-index trigger. A document goes in and can be counted; nothing reads it
back yet. **Resist finishing the round trip here** — the ingester is the half that has to be right,
and reviewing it alongside a retrieval change is reviewing neither.

**Done when:** a real document — pick a QCS section or an ISO 19650 extract — goes into the company
scope, `.rvt` in the same folder is refused **by name**, re-running the ingester on unchanged files
re-embeds nothing, and deleting the store and re-ingesting produces the same chunk count.

---

## 5. Stage 2 — a document comes back out

**Closes:** R-12, R-19, R-20. **Needs:** Stage 1.

Documents join the retrieval that already works rather than getting a pipeline of their own. The
structured filter comes first exactly as it does for fragments; the difference is that a document has
no Revit version, so **the version wall must not silently exclude every document** — that is the one
place this stage can go wrong quietly.

Three things this stage owes:

1. **A result says which kind of thing each hit is.** A fragment and a clause are not interchangeable
   and a shortlist that mixes them without saying so is worse than either alone.
2. **An empty document store refuses**, and says *empty*, not *nothing matched*. This exact defect was
   found on the fragment side on 2026-08-30 by measuring — a query against an empty store printed the
   same words as a genuine miss, and a number recorded from that run would have been a measurement of
   an empty database. **It is already known; do not rediscover it.**
3. **A measurement from day one** (R-33). One tracked question against the document corpus, recorded
   with date, corpus size and backend, the same way the fragment side is.

**Done when:** a question answered from an ingested document returns the chunk, `check-docs.py` still
passes, and `retrieval-history.md` has a first document row.

---

## 6. Stage 3 — the citation, the refusal, and the check

**Closes:** R-21, R-22, **and R-46 to R-55**. **Needs:** Stage 2. **This is the stage that decides
whether Heron is trustworthy.**

[`05 §8`](../../../05-heron-brain.md): a claim about ISO 19650, QCS, Ashghal or a company standard
**must** carry a citation to an indexed source. **No source, no claim.** An uncited standards answer is
a bug.

**Three parts.** The second is the one people skip; the third was taken from outside on 2026-09-10,
and it is what makes the first two enforceable rather than merely stated — §6.1 below.

- **The citation.** Every answer built from a chunk carries `title`, `locator`, and enough to open the
  file. A citation a human cannot follow is decoration.
- **The refusal.** When nothing is indexed that supports the claim, Heron **says so and stops**. Today
  `heron_context.py`'s `STANDARDS` path already refuses by name because no clause store exists. **When
  the clause store exists, that refusal must not soften into a guess** — it must narrow to *"nothing
  indexed covers this"*. A path that refused honestly while empty and started guessing once full would
  be a worse system than the one that refused.

### 6.1 The check — how it gets built

New file, `brain/heron_ground.py`, carrying `Heron-Agent: HERON-RAG-CIT-014` and the rest of the header
[`docs/29`](../../../29-metadata-standard.md) requires. It takes **a proposed answer and the packet it
was built from**, and returns a report. **It never returns a corrected answer** (R-53).

**Build R-63 to R-65 first, in the same stage.** The check compares a claim against *the chunk it
cites*, so until a packet part carries the id of the exact chunk and that binding survives into the
draft, the comparison has **no defined target** and step 2 below is comparing against a guess.

Build it in this order — each step is testable alone, and the last two are where the judgement lives:

| | Step | Test it with |
|---|---|---|
| 1 | **Normalise** — one function, both sides, identical (R-48) | pairs differing only in unit form: `150mm` / `150 mm`, `DN150` / `150Ø`, `§21.3.2` / `21.3.2`. All must normalise equal |
| 2 | **Compare** — a similarity ratio from the standard library, no model (R-47) | a sentence against itself is 1.0; against unrelated text, low. Run it with no network and no keys |
| 3 | **Select** — which sentences are checkable at all (R-50) | a sentence with a dimension is checked; *"this is worth reviewing"* is not |
| 4 | **Threshold** — per kind of claim, from a named table (R-49) | a quoted clause below its threshold flags; a paraphrase at the same ratio does not |
| 5 | **The understating rule** (R-51) | *"insulated"* against *"insulated to 25mm"* **passes** |
| 6 | **Report** — thresholds used, claims checked, every flag with its ratio (R-52) | two runs at different thresholds produce reports that can be compared |

**Write step 5's test first.** It is the difference between a checker people trust and one they switch
off. A modeller who writes *"the duct needs insulation"* about a clause saying *"ducts in unconditioned
spaces shall be insulated to 25mm"* has said something **true and less specific** — and a checker that
calls that a fabrication will be ignored within a week.

**What it must not become.** A threshold that drops whenever the report is noisy (R-55). If the flags
are wrong, the **normaliser or the selector** is wrong — fix the step that is wrong and record what it
was, the way [`retrieval-history.md`](../../../../brain/retrieval-history.md) records the three times an
utterance was left alone rather than weakened.

**The first number goes into [`retrieval-history.md`](../../../../brain/retrieval-history.md)** with its
date, corpus size and thresholds (R-54). Without the thresholds beside it a fabrication rate is not
comparable to the next one — the same lesson that file was written to teach about retrieval scores.

**Done when:** a standards question with an indexed source answers with an openable citation; the same
question with the source removed **refuses by name**; a deliberately fabricated sentence is **flagged
with its ratio**; a sentence that only understates its source **passes**; it all runs with no network
and no keys; and every one of those is a test.

---

## 7. Stage 4 — maintenance, so it stays true

**Closes:** R-27, R-28, R-29. **Needs:** Stage 2.

| | What | Watch for |
|---|---|---|
| `HERON-RAG-RIX-011` | Re-index **on change**. The hashing exists; nothing fires it | mtime. [05 §7](../../../05-heron-brain.md) names it: mtime means every git operation triggers a full re-index |
| `HERON-RAG-DUP-012` | Duplicates at **write** time | The same standard ingested twice under two filenames is the normal case, not the exotic one |
| R-29 | The user never runs `--rebuild` by hand | `--rebuild` stays, as recovery. It stops being the *only* way |

---

## 8. Stage 5 — trust and conflict

**Closes:** R-23, R-24, R-25, and finishes R-16. **Needs:** Stage 3.

R-16 is the interesting one. [`05 §4.4`](../../../05-heron-brain.md) says re-rank by status, success
rate, recency and version match. What exists is a **nudge**, deliberately smaller than one rank of
fusion, so it settles a tie and cannot overturn a better match — and its first version was eight times
too big, which a test caught because the test asserted the arithmetic rather than the intention.

**Two of those four signals do not exist at all: nothing records success rate, and nothing records
recency of use.** So this stage is not *tune the ranking*. It is: decide whether Heron should record
those signals, and if so where — then write the decision down in
[DECISIONS.md](../../../DECISIONS.md) before touching the weights. **A ranking change with no recorded
reasoning is unreviewable a month later.**

Conflict (`HERON-RAG-CNF-015`, [docs/20](../../../20-knowledge-trust-and-conflict.md)) is the other
half: two sources disagreeing is **surfaced and asked about**, never resolved silently by rank. In a
consultancy that is the difference between a tool and a liability.

---

## 9. Stage 6 — research

**Closes:** R-26. **Needs:** Stage 3. **Last, and it is not optional to put it last.**

`HERON-RAG-RSH-017` answers what Heron's own knowledge cannot, from outside. It is last because an
external answer without a working citation system is exactly the invented-standard failure
[`05 §8`](../../../05-heron-brain.md) forbids — **and it is the most convincing kind of wrong answer
the system can produce.** Build the citation first, then let it reach outside.

---

## 10. Why this order

| Stage | Chosen because |
|---|---|
| 0 measure | Costs an hour, needs nothing, and fixes a rule the repository is currently breaking. Also gives the *before* number every later stage is judged against |
| 1 ingest | Nothing else is possible. Eight of the seventeen agents are waiting on a table that does not exist |
| 2 retrieve | The round trip. The first stage a person can *see* |
| 3 cite | The credibility stage. Everything after it can produce a claim, so it comes before them |
| 4 maintain | Stops the index quietly diverging from the disk |
| 5 trust | Needs real usage to be worth anything |
| 6 research | Most dangerous without 3 |

**None of stages 0 to 5 needs Revit, and none needs the PC.** That is deliberate: this is work that can
run while a model is not open, which is most of the time. Compare the queue in
[`docs/HANDOVER.md`](../../../HANDOVER.md), where the biggest item has been waiting for a machine
since 2026-09-09.

---

## 11. How to tell a stage is actually done

The repository has been bitten by *done* meaning *a note says so*. For every stage:

1. **A command, not a claim.** Name the command whose output shows it works.
2. **A test that fails for the right reason.** Prefer an assertion that will break when the thing it
   describes changes — R-19's empty-store refusal is the model: it asserts an absence, and the absence
   is stable as the corpus grows.
3. **The four gates green** (§2), and `check-gaps.py` read for *unfinished* versus *waiting*.
4. **The durable part written down** — in [`docs/05`](../../../05-heron-brain.md) for behaviour, in
   [DECISIONS.md](../../../DECISIONS.md) for a choice, in
   [`retrieval-history.md`](../../../../brain/retrieval-history.md) for a number.
5. **A dated line in [`03-working-note.md`](03-working-note.md)** saying what happened, including what
   failed. **A stage that records no surprises probably was not measured.**
