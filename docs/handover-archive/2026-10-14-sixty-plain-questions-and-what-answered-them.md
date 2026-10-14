# Session note — SIXTY PLAIN QUESTIONS, AND WHAT ANSWERED THEM

> **Session note** from 2026-10-14, written straight into this folder, one file per sitting, as
> [`README.md`](README.md) asks. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**. A note records what was true on its own day.

---

### 2026-10-14 — SIXTY PLAIN QUESTIONS, AND WHAT ANSWERED THEM

The same overnight sitting as [the gap pass](2026-10-14-the-gap-pass-new-tools-for-what-the-library-could-not-do.md),
continued in PR #535, stacked on #534. The owner asked for the work to go on while Revit was busy.

**What it found.** On the trained search - the one the owner's PC uses - a plain question is often
answered by a tool that changes the model. Sixty everyday questions, written before any card was read
and asked through `heron_retrieve.find` on private stores: **26 reached a write on `main`**. The full
list, the read that answers each, and who decides the rest are in
[row 5b-477](../fragment-issues/section-5b-rows-401-425.md). `tools/check-routing.py` cannot see this:
its no-crossing section asks each card's own declared sentences, and identity answers those before any
ranking runs. **A sweep of paraphrases is the only thing that measures it.**

**What was done, DRAFT cards only:**

| Change | What it fixed |
|---|---|
| `REPORT_PROJECT_INFORMATION`, new | *"what is the project number"*, *"who is the project client"* - nothing read Project Information |
| `REPORT_VIEW_SETTINGS`, new | Detail level, visual style, discipline, phase, phase filter - nothing read them |
| `LIST_GRIDS` version 4 | The spacing from each grid to the next running the same way - nothing measured it square to the grids |
| Six questions declared on the read that answers each | Fan coil position, pipe ends, equipment level, duct elevation, section scale, unconnected ducts |
| `REPORT_LOCATION`'s 809-word purpose moved to comments | Its site paragraph began at word-piece 512, past what the trained model reads |
| Question utterances on `REPORT_PROJECT_UNITS`, `REPORT_AREA_VOLUME_COMPUTATIONS` | Rows 5b-356 and 5b-476: two of the suite's failures |
| Two #534 cards reworded | *"when was this model last synced"* reached the Publish ownership tool |

**26 to 15** on the sweep; `tests/test_routing_phrases.py` 5 failures to 3; check-routing's
claimed-and-not-reached 52 to 50 on the trained model and 39 to 35 on spelling. No question that
reached a read moved to a write.

**What it cost, said in the rows.** Four commands now reach a read: *"set cooling load to Btu/h"* and,
on the trained model, *"move the site to these coordinates"* (row 5b-356), and two detail-level
commands (row 5b-477). The safe direction - and each is repaired by one declared sentence on a PROVEN
setter, which is the owner's call.

**How to measure a routing change on the trained model, learned the hard way:**

- **Write the test sentences before reading the cards**, and keep them out of every card while they
  measure. A sentence declared on a card is answered by identity and measures nothing - the eight
  sweep sentences declared here were declared as repairs, after measuring, and measure nothing now.
- **Measure against the commit before, not against nothing**, on fresh private stores, both backends.
  Many contests are exact ties - words #1 + nearness #2 against words #2 + nearness #1 - and the PROVEN
  nudge settles them for the PROVEN card (row 5b-329). One word moves them.
- **Changing one card moves others.** Lengthening a card's utterances let a three-way tie take a
  question from it; removing "saving" from a Publish card stopped it taking *"is daylight saving on"*.
  Re-run the whole check after every change, not only the sentence aimed at.
- **A purpose long enough to pass 512 word-pieces is partly unread** by the trained model. History
  belongs in comments.

**Waiting on the owner:** EC13 and EC14 on a test copy; the tie rule (5b-329); the setters' command
sentences (5b-356, 5b-477); declaring 13 sweep questions on the PROVEN reads that answer them
(5b-477).
