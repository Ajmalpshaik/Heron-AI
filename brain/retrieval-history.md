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
