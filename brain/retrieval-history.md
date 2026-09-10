# Retrieval — what was measured, and when

**Quote this file, never a remembered figure.** Every line records the library size it was taken at,
because a retrieval number without a corpus size is not comparable to anything.

That rule is not theoretical. The owner's earlier library had **three different accuracy figures in
circulation at once** because the early scores recorded no model, no chunk size and no corpus size, and
the library grew underneath them. This file exists so that cannot happen here.

---

## "show me every duct in the model" → `FRG-ELE-001`

One query, tracked across every library size so far. It is the query Steps 10 and 11 were built against,
so it is the one with history.

| Date | Fragments | By words | By nearness | Fused | Backend |
|---|---|---|---|---|---|
| 2026-08-28 | 7 | top 3 | top 3 | **1st** | `lexical` |
| 2026-08-29 | 14 | 3rd | 5th | 4th | `lexical` |
| 2026-08-29 | 17 | 3rd | not in the top 5 | 5th | `lexical` |
| 2026-08-29 | 19 | 3rd | not in the top 5 | 5th | `lexical` |
| 2026-08-30 | 25 | 3rd | not in the top 5 | 5th | `lexical` |
| 2026-08-30 | 28 | **5th** | **not in the top 5** | **not in the top 5** | `lexical` |
| 2026-08-30 | 30 | 5th | not in the top 5 (**15th of 30**) | not in the top 5 | `lexical` |
| 2026-08-30 | 32 | **7th of 32** | not in the top 5 (**17th of 32**) | not in the top 5 | `lexical` |
| 2026-08-31 | 47 | 12th of 47 | not in the top 5 | not in the top 5 | `lexical` |
| 2026-08-31 | 59 | **17th of 59** | **36th of 59** | **20th of 59** | `lexical` |
| 2026-09-10 | 360 | outside the first 100 | **19th** | not in the shortlist | **`model`** |
| 2026-09-11 | 360 | **118th of 360** | **194th of 360** | not in the shortlist | `lexical` |

**At 59 the decline is no longer just the duct filter sinking - the shortlist itself has stopped being
made of fragments that fairly claim the sentence.** The top five are now `dimension-mep-runs`,
`find-views`, `set-selection`, `find-sheets`, `dimension-family-instances`. `set-selection` has a real
claim on *"show me"*. **`find-sheets` and the two dimensioning fragments have no claim on this sentence
at all**, and two of them WRITE to the model.

That is a different and worse finding than the earlier rows, which recorded one fragment ranking badly
while the shortlist stayed sensible. It is recorded here rather than asserted away: `test_retrieve.py`
had a check that at least two fragments with a real claim came back, and at 59 that check FAILS. It was
narrowed once at 7 fragments and again at 28. **Narrowing it a third time would be the measurement
protecting itself**, so the claim was withdrawn instead and this paragraph carries what actually
happened.

The spread across those five is **0.0023** - one rank of fusion is 0.00026 - so they are close but no
longer the flat tie the 28-fragment row described. Ordering among them is still not ranking in any
meaningful sense.

---

## Intrusion: who turns up in shortlists they have no claim on

Measured 2026-08-31, 59 fragments, 330 utterances, `lexical` backend, by
[`tools/check-intrusion.py`](../tools/check-intrusion.py) - **run the tool, do not quote these
numbers.** `check-routing.py` asks whether each fragment wins its OWN sentences; that is a per-fragment
question and says nothing about the shortlist a user sees. This asks the opposite.

| Fragment | In shortlists it does not own | Purpose length |
|---|---|---|
| `find-sheets` | 58 of 330 | 291 words |
| `align-mep-elevation` | 52 of 330 | 182 words |
| `dimension-family-instances` | 42 of 330 | 438 words |
| `move-to-ray-hit` | 42 of 330 | 122 words |
| `snap-to-grid` | 41 of 330 | 146 words |

**The obvious explanation was tested and is wrong.** Purposes here run 21 to 523 words and the whole
purpose is indexed, so length looked like the cause:

    correlation(purpose words, intrusions) = 0.313

Weak, and the counter-examples are decisive rather than marginal: `set-selection` has the SHORTEST
purpose in the library and one of the highest intrusion counts, while `group-by-assembly` has one of the
longest and intrudes on nothing. **Shortening prose would not have fixed this**, and that is worth
having measured before somebody spends a day doing it.

What the top of the list shares is generic phrasing - *"show me..."*, *"which ... are in this model"*,
*"what ... do we have"*. Every one is a real sentence a modeller says and none may be taken away to buy
a number. So the finding is about the retrieval layer rather than any fragment, which is where
[D-47](../docs/DECISIONS.md) arrived from the other direction, and **A7** - the trained embedding
backend, never yet run here - is the thing that would separate *"show me the drawing list"* from
*"show me every duct"*.

**The words route held 3rd from 7 to 25 fragments, then stepped to 5th at 28 and stayed there. The
nearness route collapsed early and has stayed collapsed.** At 17 it ranked a *move* fragment first for a
question about ducts; at 19 the duct filter is still outside the shortlist, and `report-findings` ranked
first after fusion at every size from 14 to 25. No meaning-based encoder would do either.

> This paragraph read *"flat at 3rd across every size measured"* until 2026-08-30, and it had been false
> since the 28-fragment row was added directly above it. The correction is recorded rather than quietly
> made because it is the exact failure this file was written to prevent, occurring **inside this file**:
> a summary sentence ageing past the table it summarises. The two sections below explain the step; the
> sentence a reader meets first must not contradict them.

The rows at 17, 19 and 25 are worth reading together: **the library grew by nearly half and nothing
moved.** Words 3rd, nearness absent, fused 5th, three times running. The degradation is not a slope that
will keep sliding - it fell off between 7 and 14 and has been flat ever since.

That is a more useful shape than a decline. A slope would mean the corpus is diluting a working signal
and would eventually need a bigger index or a smaller pool. A cliff followed by a flat line means the
built-in backend has **no useful signal for this question at all** past a handful of fragments, and no
amount of tuning the retrieval around it will help. Only `A7` will.

That is not a defect in the fragments and it is not retrieval "getting worse" in general — it is exactly
what [`heron_embed.py`](heron_embed.py) measures the built-in backend to be: **character n-grams, not
meaning**. More fragments means more of them score spuriously close.

> **"Collapsed" is about THIS QUESTION, not about the route.** The 169-sentence sweep at the bottom of
> this file measures the same backend putting the right fragment first 60% of the time and in the top
> three 82% of the time, when the question names one fragment. Read the two together: the backend handles
> vocabulary overlap and fails at disambiguation, and this query is a disambiguation question.

**`A7` is the fix, and it has never run.** It needs a network that can reach a model host — no Revit and
no Windows. On the evidence above it is no longer a nice-to-have: it is the difference between a search
that finds the right fragment and one that finds a fragment.

### The fused scores are effectively tied, which matters more than the ranks

At 14 fragments the top five spanned **0.0021**, where one rank of fusion is **0.00026**. The order among
them is noise rather than ranking. A reader who takes the top hit as "the answer" is reading a coin toss.

### Two thresholds this crossed

- **The pool.** `heron_retrieve` ranks against a pool of 20. Below that the nearness route returns every
  eligible fragment, so *"both routes agree"* is true of everything — including a question about cats —
  and the answer says so. **The library passed 20 on 2026-08-29** (21 eligible, counting a test's own
  synthetic fragments), so agreement is starting to be real evidence. `tests/test_retrieve.py` reported
  that as a failure first, which was the system working.
- **The assertions.** Two tests were rewritten twice as the library grew, which was the signal that they
  were measuring the CORPUS rather than the backend. They now assert the backend's weakness as an
  **absence** — the nearness route does *not* find the duct filter in its top 3 — which is stable as
  fragments are added and **will fail when `A7` lands**. That failure is the thing worth being told
  about.

---

## 2026-08-30 — the words route moved, and it was not corpus size

The keyword route sat at **3rd across five measurements** — 7, 14, 17, 19 and 25 fragments. At 28 it is
**5th**, and the duct filter has left the fused shortlist entirely.

**The cause is vocabulary collision, not dilution.** Three visibility fragments were added, and
`isolate-elements` declares *"show me just these"* among its phrasings. The query begins *"show me"*. It
now ranks **first**, and `set-elements-selection` is second.

**The retrieval is not wrong. The question is ambiguous.** *"Show me every duct in the model"* can
reasonably mean *which* ducts (a filter), or *put them on the screen* (isolate, or select). Three
fragments now fairly claim that sentence, and no ranking of fragments can settle it — because the
sentence is filter-**then**-show, and a composition is what a **skill** names, not a fragment.

[docs/27](../docs/27-build-order.md) reached that conclusion once already, at 7 fragments, and retired an
assertion for it. This is the same finding arriving with much more force: the query has been measuring
the wrong layer since, and at 28 fragments that is no longer arguable.

**What to do about it, and what not to.** Do not rewrite `isolate-elements`' phrasings to protect this
measurement — *"show me just these"* is exactly what somebody says when they want an isolate, and taking
it away would make the isolate unfindable to buy a number. The answer is that this query belongs to the
**skill** layer, and the measurement here should follow a query that genuinely names one fragment.

**Two things this does NOT change.** The nearness route is still absent, still for the reason recorded
above. And `A7` is still the fix for that half — this finding is about the OTHER route, and the two are
independent.

---

## 2026-08-30 — 30 fragments, and the collision prediction held

Two more view fragments were added (`RESET_GRAPHIC_OVERRIDES`, `SET_CATEGORY_GRAPHICS`) and the words
route **did not move**: 5th at 28, 5th at 30.

That is worth more than another flat row, because the finding above made a claim that could be wrong.
It said the drop from 3rd to 5th was **vocabulary collision** — `isolate-elements` claiming *"show me
just these"* — and not the view area growing. Those two explanations predict different things. Dilution
predicts that any further view fragment pushes the duct filter down again. Collision predicts that a view
fragment which does **not** claim the query's words costs nothing.

Neither new fragment says *"show me"*. The rank held. **The collision explanation predicted that and the
dilution explanation did not**, so the earlier conclusion is now tested rather than merely reasoned.

**The nearness route was measured precisely this time, and the number is worth having: 15th of 30.**
Dead centre. Not "absent because the shortlist is short" — actually mid-pack out of the whole eligible
library, which is what no signal looks like when you measure it rather than infer it. Every earlier row
recorded only *"not in the top 5"*, which is compatible with 6th; this one is not.

### Two defects in the measuring tool, found by measuring

Neither changes a number above. Both would have corrupted a future line.

- **An empty store printed `nothing matched`.** On a fresh machine `heron_retrieve.py` answered a
  question against a store holding nothing, and said the same words it says for a genuine miss. A line
  in this file recorded from that would be a measurement of an empty database, indistinguishable from a
  measurement of the library. It now refuses, names the store as empty, and points at the rebuild.
- **An unknown flag became part of the question.** `--rebuild` is not a flag this tool has, so it was
  searched for as text, matched nothing, and printed the same `nothing matched`. It is now refused by
  name.

Both are the same shape as the failure this whole file exists to prevent: **a tool that answers when it
should decline.** A wrong number gets caught eventually; a plausible number taken on a broken run gets
quoted for months.

---

## 2026-08-30 — a second instrument, and it corrects the headline

At 32 fragments the tracked query moved again: the duct filter is **7th by
words**, pushed down by `CREATE_DUCT` and `SET_MEP_SIZE` declaring *"duct"* in
phrasings of their own. That is the third time this one query has moved for a
reason that is nothing to do with the backend, and it is the last time it will
be asked to carry the measurement alone.

### The instrument that replaced it

[`tools/check-routing.py`](../tools/check-routing.py) asks **every fragment's own
declared utterances** back to the search and checks the fragment that claimed the
sentence comes back first. 169 sentences across 32 fragments, measured 2026-08-30:

| | #1 | top 3 |
|---|---|---|
| by words | **156 of 169 (92%)** | **169 of 169 (100%)** |
| by nearness | 102 of 169 (60%) | 138 of 169 (82%) |

**Read this as a LOWER BOUND and nothing more.** A fragment's own phrasing shares
vocabulary with its own indexed text, so a pass is close to circular — it shows
lexical overlap working, never meaning. A **failure** is the real information:
another fragment now outranks it for words it claimed, so a request phrased that
way lands somewhere else, silently.

### And it corrects something this file has been saying

The rows above are summarised as the nearness route having **collapsed**, on the
evidence of one query where it ranks the answer dead centre of the library. That
reading is too strong, and the 169-sentence sweep is what shows it: the same
backend puts the right fragment **first 60% of the time and in the top three 82%
of the time** when the question names one fragment.

So the accurate statement is narrower and more useful than "collapsed":

> The built-in backend handles **vocabulary overlap**. It does not handle
> **disambiguation**. Given words that appear in one fragment's phrasings it
> finds that fragment; given a sentence several fragments fairly claim, it
> returns noise.

That sharpens what `A7` is for. It is not needed to look a fragment up — the
built-in backend already does that acceptably. It is needed for the case where
the words alone do not decide, which is exactly the case a character n-gram
model cannot reach and a trained encoder can. The earlier framing would have had
somebody expect `A7` to improve every lookup; it should be expected to change
the contested ones.

### What the sweep found, and what was done about it

Sixteen sentences were claimed by one fragment and answered by another. **Three
were genuine errors and were corrected; thirteen were left alone**, and the split
is the point.

Corrected, because the utterance was wrong rather than unlucky:

- `FILTER_ELEMENTS_BY_ID` claimed *"what is connected to that duct"* and *"trace
  from this element"*. Those are `TRACE_CONNECTIVITY`'s sentences. Turning ids
  into elements is the **first step** of that job, and a fragment claiming the
  whole sentence is the composition mistake this file keeps arriving at.
- It also claimed *"check these elements"*, which is a QA sentence.
- `OVERRIDE_GRAPHICS_IN_VIEW` claimed *"do the grayout"* and *"do the grayout for
  MEP"* — the **skill's** sentences, which the skill declares. And it claimed
  *"grey the background"*, which changed owner when the background became
  category work: **the search had already moved that sentence to the category
  fragment on its own**, which is retrieval agreeing with a design decision
  rather than being corrected into it.

That took the words route to **100% in the top three**: every fragment is now
reachable for every sentence it claims. The remaining thirteen are all #2 or #3 —
`copy` against `mirror` for *"copy them across"*, `hide` against
`category visibility` for *"I do not want to see the ceilings here"*, `rotate`
against `flip` for *"the diffuser is the wrong way round"*. Those are real
ambiguities in English, not defects, and several want the owner's judgement about
which he means.

**What was NOT done, and the tool exits 0 to keep it that way:** no utterance was
weakened to buy back a rank. Taking *"show me just these"* away from the isolate
fragment would make the isolate unfindable in order to protect a number, and a
checker that failed a build over a collision would teach exactly that habit.

---

## 2026-09-11 — 360 fragments, and the two backends compared at the same size

**This file had eleven rows, every one `lexical`, the last at 59 fragments, while the library stood at
360.** That is the drift it was written to prevent, happening to it. Two rows above close it.

```bash
python brain/heron_scope.py --rebuild
python brain/heron_embed.py                    # says which backend answered
python brain/heron_fragment.py                 # the library size
python brain/heron_retrieve.py "show me every duct in the model" --revit 2024
```

### The row this file still does not have, and why

**No measurement on the `model` backend was taken today.** The container this was run in refuses
`huggingface.co` — the proxy answers `403` to `CONNECT`, so no weights can be fetched and
`heron_embed.py` reports `Backend: lexical`. That is the **same block `heron_embed.py` recorded on
2026-08-28**, still in force, and it is a fact about **one container** rather than about Heron.

The `2026-09-10 / model` row above is carried across from
[`docs/work-notes/plans/rag/03-working-note.md` §3](../docs/work-notes/plans/rag/03-working-note.md),
where it was measured on a machine that could reach a model. **It is not re-derived here and must not
be quoted as if it were** — its words column reads *"outside the first 100"* because that run capped
its list at 100.

### What comparing the two rows actually shows

**Same query. Same corpus size. Two backends.** That is the comparison
[the working note](../docs/work-notes/plans/rag/03-working-note.md) said nothing had, because the jump
from 59 to 360 moved corpus size and backend at once and neither number separated them.

| Route | `lexical` at 360 | `model` at 360 | Reading |
|---|---|---|---|
| words | 118th of 360 | outside the first 100 | **The same, and it must be** — the words route is FTS5 and does not touch the embedding backend at all. Two runs agreeing where they cannot differ is a small check on both |
| nearness | **194th of 360** | **19th** | **The trained encoder is worth roughly ten places in a hundred here.** Middle of the library against the top fifth |

**So the degradation on the words route is CORPUS SIZE and the improvement on nearness is the
BACKEND.** At 59 the duct filter was 17th of 59 by words (29%); at 360 it is 118th of 360 (33%). It is
sinking in proportion to the library rather than falling out of it.

**And the query is still the wrong instrument, which this file decided on 2026-08-30 and still means.**
*"Show me every duct in the model"* is filter-then-show — a composition, which is what a **skill**
names, not a fragment. Three fragments fairly claim it. It is kept for continuity with the rows above
it and for nothing else.

### Both checkers, run at 360 for the first time

`check-routing.py` and `check-intrusion.py` were last recorded at **32** and **59** fragments. Both
exit 0 at 360, on `lexical`:

```bash
python tools/check-routing.py
python tools/check-intrusion.py
```

| | 2038 declared utterances, 360 fragments, `lexical` |
|---|---|
| words route | **#1 for 81%**, top three for **96%** |
| nearness route | **#1 for 54%**, top three for **73%** |
| worst intruder | `FRG-VIEW-094` `arrange-tags-to-view-edges`, in **64** shortlists it does not own |
| correlation(purpose words, intrusions) | **0.151** |

**The words route reached 100% in the top three once, at 59 fragments, and it is 96% at 360.** That is
thirteen times the library for four points, and the four points are **not** a licence to reword
anything: the section above this one says what is never the answer, and it still is not.

**Neither number is comparable to a `model` run, because there is not one.** Both tools report the
backend they ran on, which is the whole reason they print it.

---

---

## 2026-09-11 — how contested a shortlist is, and why no floor was set

**Stage 0b** of the RAG plan: retrieval now reports how contested its answer was — the winner's lead
and the shortlist's spread in units of **one rank of fusion**, how many candidates both routes found,
and the two magnitudes fusion discards. `heron_retrieve.Contest`, and `tests/test_contest.py`.

The plan also asked for two things built on the same measurement: **drop a candidate with no claim**
(R-56) and **refuse a question nothing covers** (R-58). **Neither was built, and this is the
measurement that says why.**

Twelve questions — six about BIM, six with no BIM content at all — at 360 fragments on `lexical`:

| | winner's lead | words route matched | best bm25 | best nearness |
|---|---|---|---|---|
| six BIM questions | 2.1 – 8.1 ranks | 265 – 360 of 360 | −4.27 – −10.37 | 0.19 – 0.59 |
| six not about BIM | 0.9 – 29.9 ranks | 245 – 360 of 360 | −0.00 – −6.59 | 0.15 – 0.47 |

**Every column overlaps.** *"How do I bake sourdough bread"* has the **widest winning gap of all
twelve** and the second most selective words route. *"Tag every mechanical equipment"* is less near
than *"what is the best food for a cat"*. **A floor anywhere on any of these four columns cuts a real
question in order to reach an unreal one.**

**Why the fused score could never have been the answer.** Reciprocal rank fusion keeps **order** and
discards **strength** by construction, so two shortlists look alike from the outside however different
their contents. The cat question's top candidate scores **0.0254**; the tracked duct question's scores
**0.0246**. The cat question scores **higher**.

**Why the other two columns fail is not a surprise either, once it is said out loud.**
`heron_embed.py`'s own docstring says of the built-in backend: **"IT IS NOT MEANING."** Asking it to
tell a duct from a cat is asking it for the one thing it states it cannot do. And `_fts_query` joins
words with `OR` so a missing word cannot empty a result — correct for a lookup, and it means a sentence
made of ordinary English matches most of the library. *"What is the best food for a cat"* matched
**360 of 360**, because *what*, *is*, *for* and *a* are in every fragment.

> **So the floor is not set, and that is a result rather than a postponement.**
> [R-60](../docs/work-notes/plans/rag/01-requirements.md) says the floor is derived from a measurement
> and from nothing else. This is the measurement, and it says **not on this backend**. The run belongs
> on the `model` backend, where nearness is meaning — and that needs a machine that can reach
> `huggingface.co`.
>
> **A first shape was reported and then withdrawn by measuring more.** At three questions the words
> route looked selective for real questions and not for unreal ones. At twelve it does not. **Three
> questions is an observation; the twelve are the measurement**, and the first version of this note
> said the opposite of what the second one says.

**What was NOT done, deliberately.** No keyword list of BIM words (R-60) — it would be wrong the week
it was written. No classifier in `brain/` (R-62) — [D-01](../docs/DECISIONS.md) gives classifying to
the host. And no floor chosen to make the twelve questions sort nicely, which is
[R-55](../docs/work-notes/plans/rag/01-requirements.md) and the same refusal that kept *"show me just
these"* on the isolate fragment three times above.

---

## 2026-09-11 — the first document measurement, and it is a weak one on purpose

**R-33: document retrieval gets its own measurement from its first day**, so that this half never
reaches the state the fragment half nearly did — working, trusted, and unmeasured.

**13 chunks, two documents, `lexical` backend.** Six questions asked the way a modeller would ask
them, each with the clause it should return:

| Asked | Wanted | Got |
|---|---|---|
| how thick should duct insulation be | 9.1.1 | **9.1.1** |
| do I need a vapour barrier over insulation | 9.1.2 | **9.1.2** |
| what fall for a soil drain | 12.1 | **12.1** |
| what category should ducts carry | 4.1.2 | **4.1.2** |
| when do ducts not need insulating | 4.1.1 | 9.1.1, then **4.1.1** |
| what gradient for drainage | 12.1 | **12.1** |

**P@1 five of six. Recall@3 six of six.**

### Read the caveats before the numbers, because they are larger than the numbers

- **The documents were written for the tests, by the same hand that wrote the questions.** They use
  the vocabulary the questions use. **This is the weakest kind of measurement there is** and it is
  recorded as a baseline to beat, never as evidence that chunking works. The real measurement is one
  real numbered section, and S-4 is still open.
- **13 chunks is below the pool of 20**, so *"both routes agree"* is true of everything here — the
  system says so itself in every answer, which is R-61 doing its job on the document side from the
  first run.
- **`lexical` again.** The trained backend needs `huggingface.co`, still refused here.

### The one miss is the more interesting row

*"When do ducts not need insulating"* wanted the exception in **4.1.1** and got **9.1.1** first — the
other document's insulation clause, which also carries an exception. **Both documents say something
true about when insulation is not required.** That is not a retrieval defect; it is
[R-24](../docs/work-notes/plans/rag/01-requirements.md) in miniature — two sources with a claim on
one question — and today ranking silently picks one. **Recorded rather than tuned**, because the
answer to it is *surface the conflict*, which is Stage 8.

---

## How to add a line

Run the measurement, do not estimate it:

```bash
python brain/heron_scope.py --rebuild                          # FIRST - the store is derived
python brain/heron_retrieve.py "show me every duct in the model" --revit 2024
python brain/heron_embed.py                                    # says which backend answered
python brain/heron_fragment.py | tail -3                       # the library size
```

The rebuild is first because the store is **derived** and a fresh machine has none. That line was
missing until 2026-08-30, and following the recipe without it is what surfaced the two tool defects
recorded above.

Record the **date, the fragment count, and which backend answered**. A line missing any of the three is
the drift this file exists to prevent.
