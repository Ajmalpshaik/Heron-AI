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

The `2026-09-10 / model` row above was carried across from a RAG working note (§3), retired
2026-09-12, where it was measured on a machine that could reach a model. **It now lives in this file
and nowhere else** - see the 2026-09-10 section above. **It is not re-derived here and must not
be quoted as if it were** — its words column reads *"outside the first 100"* because that run capped
its list at 100.

### What comparing the two rows actually shows

**Same query. Same corpus size. Two backends.** That is the comparison the working note (retired
2026-09-12) said nothing had, because the jump
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

## 2026-09-11 — the first fabrication rate, and the mechanism that was measured out

**R-54: a fabrication rate is recorded with its date, its corpus size and ITS THRESHOLDS**, because
without the thresholds beside it the number is not comparable to the next one. `brain/heron_ground.py`,
`tests/test_ground.py`.

**6 chunks, one document, `lexical` backend.** Twelve claims against the clauses they cite — six true,
six invented:

| | flagged | checked |
|---|---|---|
| six TRUE claims | **0** | 4 |
| six FALSE claims | **6** | 6 |

**No false positives. Every invention caught.** Two of the six true claims carried no fact at all and
were **skipped rather than checked** — R-50, and it is why the denominator is 4 and not 6.

**Thresholds in force:** `quote 0.90 coverage` · `reference, numeric, paraphrase — no ratio gate`.

### The mechanism that was measured and rejected, which is the more useful half

The first version of this check flagged on a **similarity ratio** against a per-kind threshold. It got
**two of the six false claims wrong** — *"insulated to 25mm except within 10m"* against a source saying
**3m**, and *"density 96 kg/m3"* against a source saying **48** — because the prose around the number
was nearly identical and the ratio carried them over the line. **One character is the whole
fabrication, and a ratio is at its blindest exactly there.**

So the ratio was measured properly, on six true paraphrases and six wrong claims all carrying a fact
the source did carry:

| | range |
|---|---|
| six true paraphrases | **0.222 – 0.682** |
| six wrong claims | **0.204 – 0.588** |

**They overlap almost entirely.** *"25mm insulation is required on ducts"* — true — scores **0.222**,
**below** *"drainage shall fall at 25mm"* at **0.236**. Any threshold on that column either flags a
modeller's own wording or passes a wrong claim, and flagging a modeller's own wording is what
[R-51](../docs/work-notes/plans/rag/01-requirements.md) exists to prevent.

**So the ratio is reported and never enforced**, and what catches an invention is structural:

> **An added fact is a flag, whatever the ratio says.** A number, a dimension, a gradient or a clause
> reference the source does not carry is invented, and no amount of surrounding agreement vouches for
> it.

### And one gate that survived, because a quotation is a different claim

A claim in quotation marks asserts it **IS** the source's words, so the right question is not *how
similar* but **is it in there** — the share of the quoted text appearing verbatim in the source:

| | coverage |
|---|---|
| true quotations | **1.000, 1.000** |
| misquotations | **0.265, 0.167** |

A clean separation with nothing between. **0.90 is not tuned to that gap** — it is what *"these are
the source's words"* means, allowing an ellipsis and nothing more.

> **This is not [R-55](../docs/work-notes/plans/rag/01-requirements.md) being bent.** R-55 forbids
> lowering a threshold to reduce flags. What changed here is the **mechanism**, and it changed because
> a measurement said the old one did not work — which is the one reason this repository accepts, and
> the same method that settled the graph route at six settings.

### Read the caveats, they are larger than the numbers

- **Twelve claims, hand-written, against a document written for the tests by the same hand.** This is
  a baseline to beat, not evidence the check is right.
- **`lexical` again** — though this check does not use an embedding at all, so the backend affects
  only which clauses the packet carried.
- **It proves the answer did not invent the numbers in it. It does not prove the answer is good.**
  Those are different claims and only the second one needs a person.

---

## 2026-09-11 — the document graph's density, and the route that still has no vote

**Stage 5 begins with a count, not with code**, because the same idea over the **fragment** graph was
measured at six settings and **lost every one** ([`34 §2.13`](../docs/34-patterns-adapted.md)) — and
the property that decided it was **density**: a median of **50** neighbours per fragment, **worst
230**, so *"the neighbours of the best hit"* was a large slice of the library added as competitors.

**A document graph is a different graph, so the finding does not transfer. The test that decided it
does.** `heron_graph.document_density()`:

| | fragments | documents |
|---|---|---|
| chunks / fragments counted | 360 | **62** |
| **median neighbours** | **50** | **7** |
| **worst** | **230** | **12** |
| isolated | — | 1 |

**An order of magnitude sparser, and for a reason that is intelligible rather than lucky.** A
fragment providing `IList<Element>` composes with most of the library — that is what made it dense.
**A clause cannot do that.** Its neighbours are its one parent, the clauses sharing that parent, its
own children, and the clauses its text names. **The document's own numbering bounds the count**, and
no clause can be the parent of two hundred others unless the document really does number them that
way — in which case they really are its subsections.

### So the route is built and it still has NO WEIGHT, which is step 4 exactly

`heron_graph.document_neighbours()` derives three kinds of edge — **parent**, **sibling**, and a
clause whose **text names another clause's number**. The third is the only reason a graph route is
worth considering at all: *"labelling shall be in accordance with 21.3.1"* links two clauses that
share no subject and no vocabulary, so neither the words route nor the nearness route can find it.

**Nothing in retrieval reads any of it**, and a test asserts that. Fusion still has exactly two
weighted routes. **An edge route gets no vote until a measurement earns it one — the same bar the
nearness route had to clear**, and the same bar `34 §2.13` set for the fragment graph before it
failed.

### What this count is NOT

**62 chunks, in four documents, all written for these tests by the same hand.** The density is
*structurally* bounded, which is the part that generalises; the *number* is about this corpus. A real
QCS section with thirty clauses under one subsection would push the median up — and still nowhere
near 50.

**So the gate is passed provisionally and the weight is not granted.** Granting it needs a tracked
question set on a real corpus, and a question set over documents this session wrote would measure the
documents rather than the route.

**D-40 holds: every edge is derived on demand.** There is no edge table, and a test asserts there is
none — a stored document edge is a cache that goes stale the moment a document is re-ingested.

---

## 2026-09-11 — Stage 7: the re-ranker seam, and the half of it that cannot be measured here

**Stage 7 asks for a before and an after at the same corpus size. Only the before exists, and this
section is about being clear which is which.**

`brain/heron_rerank.py` is the seam: a cross-encoder reads the top ~20 (question, passage) pairs
together and may re-order them, which neither existing route can do because both score the question
and the passage separately. `tests/test_rerank.py` is the check.

### What was measured — 2026-09-11, 360 fragments, backend `lexical`, re-ranker `absent`

`show me every duct in the model`, Revit 2024:

| | |
|---|---|
| re-ranker reported | **`absent`** — printed by the tool, on the line under the route |
| best | `FRG-SEL-030` `ZOOM_TO_ELEMENTS`, **0.0246** |
| winner's lead | **2.1 rank(s)** |
| shortlist spread | 31.6 rank(s) |
| found by both routes | 3 of 5 |
| best bm25 / best nearness | −4.3609 / 0.4924 |

**The top score and the winner's lead are IDENTICAL to the Stage 0b run above** — 0.0246 and 2.1
ranks, recorded before this file existed. That is the result: **with nothing installed the seam is
inert, and it was checked against a recorded number rather than asserted in a docstring.**

Its cost, on the same machine and the same question:

| | |
|---|---|
| a whole `retrieve()` | **0.0074 s** (mean of 20) |
| the seam, re-ranker absent, over 20 candidates | **0.000004 s** (mean of 200) |
| share of one query | **0.05 %** |

So the four-microsecond path builds twenty passage strings it then throws away, because `scores()` is
asked before anything knows whether a backend exists. **Measured rather than tidied**: removing it
would put a second place in the code that decides whether a re-ranker is present, and 0.05 % of a
query is not a reason to have two.

### What was NOT measured, and it is the half Stage 7 is actually about

**No cross-encoder has ever run in this repository.** The weights need `huggingface.co`, and the
container this was written in refuses it:

```
$ curl https://huggingface.co/api/models/...
curl: (56) CONNECT tunnel failed, response 403
```

Re-checked 2026-09-11. `pypi.org` answers 200 from the same container, so this is that host's policy
and not a broken network — the same block `heron_embed.py` recorded on 2026-08-28, still in force.

**Nothing was estimated to fill the gap, and no number from a stub was written down as a result.**
`tests/test_rerank.py` injects a stub scorer to prove the plumbing — that the order changes, that at
most twenty pairs are scored, that a backend which throws is absorbed. A stub's opinion about which
clause answers a question is this session's opinion wearing a model's clothes, and a re-ranker's whole
claim is that it improves an order. **An improvement nobody measured is a feeling.**

> **The after belongs on a machine that can reach a model**, at 360 fragments and 62 chunks, on the
> same tracked question and the same twelve of the Stage 0b run. `A10` in
> [`docs/NEEDS-CHECKING.md`](../docs/NEEDS-CHECKING.md) is that run.

### The size, announced before anything downloads

`python brain/heron_rerank.py` prints the package, what it is for, **500 MB to 2 GB**, and
`pip install --user`, and it needs no network to print any of it — which is the only way an
announcement can come before the download it is warning about (R-77, R-42, D-01). The figure is the
one [R-79](../docs/work-notes/plans/rag/01-requirements.md) records from the field reading on
2026-09-10; it is **not** measured here, because measuring it needs the host that is blocked.
`pip download --no-deps sentence-transformers torch` is the command that confirms it.

---

## 2026-09-10 — the first `model` backend numbers ever recorded, 360 fragments

**Moved here 2026-09-12 from `docs/work-notes/plans/rag/03-working-note.md` §3, when that note was
retired.** It was held there because the track *"has not yet earned a section in this file"*; the track
is finished, so the measurement belongs where every other measurement lives. **Recorded as taken, not
re-run** — the container that produced it could reach `huggingface.co` and the one retiring the note
cannot, and a borrowed number presented as today's run is the drift this file exists to prevent.

```bash
python brain/heron_embed.py                     # Backend: model
python brain/heron_fragment.py | tail -3        # 360 well-formed, 180 PROVEN
python brain/heron_retrieve.py "show me every duct in the model" --revit 2024
```

**`show me every duct in the model`, Revit 2024, 360 fragments, backend `model`:**

| Fragment | Status | Score | Found by |
|---|---|---|---|
| `FRG-MEP-031` `SELECT_BY_INSULATION` | PROVEN | 0.0318 | words #3 + nearness #3 — both agree |
| `FRG-SEL-009` `SELECT_BY_HOST` | DRAFT | 0.0278 | words #13 + nearness #11 |
| `FRG-MEP-030` `SELECT_BY_MEP_SYSTEM` | PROVEN | 0.0254 | words #18 + nearness #20 |
| `FRG-VIEW-002` `ISOLATE_ELEMENTS` | DRAFT | 0.0164 | words #1 |
| `FRG-MEP-003` `CREATE_DUCT` | DRAFT | 0.0164 | nearness #1 |

The duct filter itself, `FRG-ELE-001`, measured by `test_embed.py`: **`#- of 100` by words, #19 of 100
by nearness.** Against the last recorded line — 2026-08-31, 59 fragments, `lexical` — **17th of 59 by
words, 36th of 59 by nearness.**

### How to read that, and how not to

**Two variables moved at once.** The corpus went 59 → 360 *and* the backend went `lexical` → `model`.
Neither number separates them and no honest reading of this pair can. `check-routing.py` and
`check-intrusion.py` are what separate them, and **neither has been run on the trained backend.**

**`#- of 100` means outside the first hundred, not 360th** — the list is capped at 100. A real
degradation on the words route, and not as large as it looks.

**The nearness route improved.** 36th of 59 is mid-library; 19th of 100 is the top fifth. That is the
trained encoder doing something the character n-grams did not.

**This query was already retired as an instrument, and that still holds.** This file concluded on
2026-08-30 that *"show me every duct in the model"* is **filter-then-show — a composition, and a
composition is what a skill names, not a fragment.** Three fragments fairly claim that sentence, so a
poor result here is partly the question being asked of the wrong layer. **It must not be reported as
the trained backend failing.** Kept for continuity with the earlier rows and nothing more.

**One thing is worth a second look regardless.** `CREATE_DUCT` — a fragment that **writes** — is
nearness #1 for a sentence beginning *"show me"*. The same shape appeared at 59 fragments on `lexical`,
where two dimensioning fragments that write reached a shortlist for the same question. A trained
encoder was expected to separate *"show me"* from *"make me"*. **On this evidence it has not**, and
that is a question for the proper instruments above, not a conclusion.

---

## 2026-09-12 — the trained backend puts the vapour barrier above the thickness clause

**Measured on the owner's Windows PC, where the trained backend is actually installed.** This is the
first time a *document* question has been asked of `model` rather than `lexical`, and it is the first
recorded case of the trained backend being **worse** than the fallback on a specific question.

`tests/test_document_retrieval.py` asks *"how thick should duct insulation be"* of a two-clause
fixture:

| Locator | Heading | Its text |
|---|---|---|
| `9.1.1` | Thickness | *"Ducts shall be insulated to 25mm…"* |
| `9.1.2` | Vapour Barrier | *"A continuous vapour barrier shall be applied over the **insulation**."* |

| Rank | Locator | Fused score |
|---|---|---|
| **1** | `9.1.2` Vapour Barrier | 0.032522474881015336 |
| 2 | `9.1.1` Thickness | 0.032266458495966696 |
| 3 | `9.1` Ductwork | 0.032002048131080390 |

### The gap is one rank of fusion, which this repository already calls noise

**0.000256** separates first from second — and that is exactly `1/62 − 1/63`.
[`heron_retrieve.py`](heron_retrieve.py) says it in its own comment: *"One rank of fusion is
1/(K+1) − 1/(K+2) = 0.000264 at K=60."* Both clauses are **rank 1 in one route and 2nd or 3rd in the
other**; the entire result is decided by a single rank in a single route.

**So the test asserts an ordering the retrieval layer says it cannot resolve.** This file has recorded
the same thing twice before — *"the order among them is noise rather than ranking"* — about fragments.
It is now true of chunks.

### It is the BACKEND, and it was first filed as an operating-system difference

Tested rather than argued, on one machine, one OS, one run each:

```bash
python tests/test_document_retrieval.py                      # backend model   -> exit 1
HERON_EMBED_MODEL=definitely/not-a-real-model-xyz \
  python tests/test_document_retrieval.py                    # backend lexical -> exit 0
```

**CI has only ever passed this because CI cannot reach `huggingface.co`** and falls back to `lexical`.
Every machine that *can* — which is the owner's, and is the configuration Heron is meant to ship in —
fails it. A green CI is not evidence here; it is evidence about CI's network.

### What it says about the trained backend, which is the part worth keeping

The encoder matched the **topic** and missed the **attribute**. *"How thick"* is a question about a
quantity; `9.1.2` contains the word *insulation* and no quantity at all, while `9.1.1` holds the 25mm
and heads itself **Thickness**. The fallback's exact-word route gets this right precisely because it is
not trying to understand.

**This is the same shape as `CREATE_DUCT` ranking nearness #1 for a sentence beginning *"show me"***,
recorded in the 2026-09-10 section above. Twice now, on two different corpora, the trained backend has
failed to separate *what kind of thing is being asked for* from *what the thing is about*.

**Two measurements are not a conclusion**, and this one is a single question on a four-chunk fixture.
It is the third instrument — [NEEDS-CHECKING `A15`](../docs/NEEDS-CHECKING.md) — that would settle it,
and this row is a reason to run it rather than a substitute for it.

---

## The owner's own questions — `tools/score-routing.py`

**Every section above measures sentences somebody wrote for the library.** `check-routing.py` asks each
fragment its own declared words back, which this file rightly calls a near-circular lower bound, and
`check-risk-crossings.py` asks ordinary sentences a session wrote. **None of it was ever asked the way
the owner asks.**

This is. [`tests/data/owner-questions.yaml`](../tests/data/owner-questions.yaml) holds 79 questions
from his own log of real work, 2026-08-13 to 2026-08-27, each with the capability, skill or tool that
should answer it. The answers were chosen from the capability list **before the search had seen any of
the questions**, and he confirmed the whole table on 2026-09-23. The tool asks every question through
`heron_brain.lookup`, the function `heron_lookup` calls, and a recorded run appends one row here:

```bash
python tools/score-routing.py            # score, and say what moved since the last comparable row
python tools/score-routing.py --record   # ...and append the row
```

**A drop is reported, never a gate** — the owner's decision, [D-100](../docs/DECISIONS.md). The tool
names every question that fell; reading them is a person's job.

**How to read a row.**

- **Answer key** fingerprints the questions and their answers together. Two rows are compared only
  when it, the backend and the Revit filter all match. The fragment count may differ, because a new
  fragment taking one of his questions is exactly the drop this exists to catch.
- **1st, 2nd-3rd, 4th-5th, Not found** count the **capability rows only**, by where the right answer
  sat in the shortlist `heron_lookup` shows: the winner, then the others that came close, each
  capability once. The four add up to the number of capability rows in the key.
- **Exact (right)** counts answers that came by an exact declared phrase, which tests a declaration
  rather than the search, and in brackets how many of those were the right answer.
- **Handed a change** counts rows whose right answer changes nothing in the model and whose search
  answer does ([D-86](../docs/DECISIONS.md)). The write line is read from the operation registry.
- **Skill rows**: a skill is never indexed, so a skill row can only be met by one of the steps it
  declares. It is kept out of the four columns because counting a skill's first step, nearly always
  a category filter, would flatter the headline.
- **Per question** is one character per question, in the key's order: `1`–`5` its place and `-` not in
  the shortlist; for a tool or gap row `w` landed on a change, `.` on anything else and `0` on nothing;
  `?` not scored.

**What may never answer a bad row:** rewording a question, changing an answer to match what the search
returns, or adding or weakening an utterance to win it — [row 113](../docs/FRAGMENT-ISSUES.md)'s
forbidden move and [D-34](../docs/DECISIONS.md)'s. An answer changes only on the owner's word, in the
key, with the day he gave it.

| Date | Fragments | Backend | Revit | Answer key | 1st | 2nd-3rd | 4th-5th | Not found | Exact (right) | Handed a change | Skill rows, a step in the top 3 | Per question |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 2026-09-23 | 396 | `lexical` | any | `88c7eadd` | 18 | 8 | 3 | 35 | 1 (0) | 4 | 5 of 7 | `1-1-1---21-1-11.-112---2-w------2-15-1-1---11--24311--122w1w14---w1---w1w-w2--1` |

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
