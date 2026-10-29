# Session note — THE ROUTING GUARDS PASS ON THE OWNER'S BACKEND, BY CARD WORDS ALONE

> **Archived session note** from 2026-10-29. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-10-29 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-10-29 — THE ROUTING GUARDS PASS ON THE OWNER'S BACKEND, BY CARD WORDS ALONE

**NEW.** [#759](https://github.com/Ajmalpshaik/Heron-AI/pull/759): `tests/test_routing_phrases.py` went
from seven failing checks to none on the trained model with the re-ranker, and `tests/test_retrieve.py`'s
duct check passes on lexical and on the model again. All of it is card words: utterance ORDER on six PROVEN
cards (the re-ranker reads a card's first eight, D-134), `REPORT_LOCATION`'s identity,
`CREATE_FROM_ROOM_BOUNDARIES`' purpose order, and two selection cards' linked-model mechanics moved from
the purpose into a comment. No code changed and no test was edited; every number, trade and margin is in
[row 5b-1293](../fragment-issues/section-5b-rows-401-425.md).

**MISTAKES worth not repeating.**

- **A venv with sentence-transformers is not a re-ranker.** Until `python brain/heron_rerank.py --fetch
  --yes` has fetched the weights the backend is `absent`, and `tests/test_routing_phrases.py` then runs on
  the model alone without saying so - the first "re-ranker" run of this sitting was not one.
  `tools/score-routing.py --backend model+rerank` refuses instead (NOT ASKED), so trust that.
- **Identical ranks after a change mean the harness, not the card.** An offline screen fed variant files
  nested one key too deep, ignored them, and printed the same ranks for every variant.
- **Reorder before you reword.** Naming doors and windows in `SELECT_BY_HOST`'s identity fixed the
  re-ranker and tied 5b-319's placement request onto the read with no re-ranker: an identity is read by
  all three routes, the order of utterances only by the re-ranker.

**TO DO.**

- On the owner's PC: `python tests/test_routing_phrases.py` and `python tests/test_retrieve.py`. The
  re-ranker margin for *place the views on this sheet* is 0.01 of a point.
- *are room areas taken to the wall finish* on the model with no re-ranker: not repairable by card words
  (row 5b-1293 says why); D-128's question flag is the designed guard.
- `tests/test_retrieve.py` on the re-ranker: four checks fail on `main` since D-134, and a fifth counts
  duct fragments by `FRG-MEP-` id only. Whether those checks are measured before re-ranking or rewritten
  is the owner's call; nothing was edited.
- 34 more cards still carry D-59's long linked-model paragraph in their purposes - `grep -l "LINKED
  MODELS ARE READ ONLY WHEN ASKED FOR" brain/fragments/*/fragment.yaml`. Only two were measured and moved.
