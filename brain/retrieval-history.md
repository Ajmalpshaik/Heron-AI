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
| 2026-08-30 | 25 | **3rd** | **not in the top 5** | 5th | `lexical` |

**The words route is flat at 3rd across every size measured. The nearness route has collapsed and
stayed collapsed.** At 17 it ranked a *move* fragment first for a question about ducts; at 19 the duct
filter is still outside the shortlist, and `report-findings` has ranked first after fusion at every size
since 14. No meaning-based encoder would do either.

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

## How to add a line

Run the measurement, do not estimate it:

```bash
python brain/heron_retrieve.py "show me every duct in the model" --revit 2024
python brain/heron_embed.py                                    # says which backend answered
python brain/heron_fragment.py | tail -3                       # the library size
```

Record the **date, the fragment count, and which backend answered**. A line missing any of the three is
the drift this file exists to prevent.
