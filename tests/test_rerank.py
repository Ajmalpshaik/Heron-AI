# Heron-Agent:  HERON-RAG-RNK-006
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Stage 7 - the shortlist is read again, and the absence of a re-ranker is fine.

    python tests/test_rerank.py

WHAT IT PROVES
  1. ABSENT IS THE NORMAL CASE AND IT IS NOT A FAILURE. With no re-ranker
     installed `scores()` returns None rather than raising, `backend()` says
     so in words, and the shortlist comes back in EXACTLY the order fusion
     produced - the same order, not a fallback order. R-41.

  2. A RE-RANKER IS ALLOWED TO OVERTURN FUSION. That is the one thing it is
     for, and it is what separates it from the quality nudge, which must
     never overturn anything. Both rules are asserted here, against each
     other.

  3. THE SCORE IS NOT POLLUTED. `Candidate.score` stays fused + nudge whatever
     the re-ranker thinks. Every number in heron_retrieve.py is measured in
     ONE_RANK and the nudge is bounded against ONE_RANK; folding a
     cross-encoder's scale in would unbound the nudge silently.

  4. AT MOST TWENTY PAIRS ARE EVER SCORED. docs/05 s4.4's "top ~20" as a
     CEILING enforced in one place, because a re-ranker over a whole library
     turns one question into a scan.

  5. RE-RANKING RUNS BEFORE THE CUT TO `limit`. With limit=5 and a re-rank of
     five, fusion's 7th can never become the answer - and fusion's 7th
     becoming the answer is the entire improvement being installed.

  6. EVERY WAY A RE-RANKER CAN MISBEHAVE IS ABSORBED. It throws; it returns
     the wrong number of scores. Both mean "fusion's order stands", and
     neither may mean "no answer".

  7. THE CONTEST NEVER EXPLAINS A RE-RANKED ORDER WITH A FUSION GAP. The gap
     describes the shortlist the re-ranker was HANDED. Reporting it as the
     reason for the order shown would be the most confident wrong sentence on
     that page - and without the sort in Contest it would also report a
     NEGATIVE spread and call every re-ranked answer a coin toss.

  8. THE SIZE IS ANNOUNCED BEFORE ANY DOWNLOAD, and the install is per-user.
     R-77 and R-42. `announcement()` needs no network to produce, which is the
     only way it can be printed before the network is used.

  9. THE RE-RANKER'S SCORE TRAVELS OUT THROUGH THE ANSWER. A review on
     2026-09-11 found the citation working in-process and absent through the
     MCP seam - a feature that does not serialise does not exist in
     production. Same mistake, same shape, asserted here instead.

WHAT THIS IS NOT
----------------
IT IS NOT A MEASUREMENT OF RE-RANKING. The stub below is this session's
opinion about which passage answers a question, wearing a model's clothes. A
test may inject a scorer to prove the plumbing; a measurement may not.

Stage 7 asks for a before and an after at the same corpus size. The before is
in brain/retrieval-history.md. The after has NOT been taken - this container's
network refuses huggingface.co, so no cross-encoder weights can be fetched.
That clause is open, it is recorded as open in docs/NEEDS-CHECKING.md as `A10`,
and nothing here stands in for it.
"""

import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


DOCUMENT = """Heron Test Standard 2026

Section 9 Thermal Insulation

9.1 Ductwork

9.1.1 Thickness

Ducts shall be insulated to 25mm, except where installed within a conditioned
space and serving a terminal within 3m.

9.1.2 Vapour Barrier

A continuous vapour barrier shall be applied over the insulation.

9.1.3 Supports

Insulation shall not be compressed at a support.

Section 12 Sanitary Drainage

12.1 Gradients

Drainage carrying soil shall fall at not less than 1:100.
"""


def candidate(R, fid, fused):
    """A Candidate with only its fused score set.

    Built rather than retrieved, for the reason tests/test_contest.py already
    gives: a shortlist out of the real library changes whenever a fragment is
    added, and an assertion about it measures the library instead of the code.
    """
    got = R.Candidate(fid, {"capability": fid, "status": "DRAFT",
                            "semantic_identity": fid, "domain": "test"})
    got.fused = fused
    got.keyword_rank = 1
    got.vector_rank = 1
    return got


def absent(RERANK):
    """Put the module in the state every un-installed machine is in."""
    RERANK._CACHE[:] = [None]


def installed(RERANK, scorer):
    """Install a stub scorer. NOT a re-ranker - a way to exercise the seam."""
    RERANK._CACHE[:] = [scorer]


def main():
    import heron_rerank as RERANK
    import heron_retrieve as R

    print("1. ABSENT is a normal state, not a failure (R-41)")
    absent(RERANK)
    name, why = RERANK.backend()
    check(name == RERANK.ABSENT, "backend() reports 'absent'")
    check("never broken" in why or "not broken" in why,
          "and says in words that this is slower to be right, not broken")
    check(RERANK.scores("a question", ["one", "two"]) is None,
          "scores() returns None rather than raising")
    check(RERANK.scores("a question", []) is None,
          "and None for an empty shortlist too, without touching a backend")
    print()

    print("2. ABSENT leaves the order EXACTLY as fusion made it")
    ranked = [candidate(R, "a", 0.30), candidate(R, "b", 0.20),
              candidate(R, "c", 0.10)]
    got = R._rerank("a question", ranked, R._fragment_passage)
    check([c.id for c in got] == ["a", "b", "c"],
          "the same order, not a fallback order")
    check(all(c.rerank_rank is None for c in got),
          "no candidate carries a re-rank position")
    check("re-ranker" not in got[0].why(),
          "and why() does not mention a re-ranker that did not run")

    # A NULL COLUMN MUST NOT RAISE, and this is the one way the seam could
    # break R-41 while appearing to honour it: the passages are built BEFORE
    # anything knows whether a backend exists, so a null here would raise on
    # every query on every machine - including every machine with nothing
    # installed, which is all of them today.
    bare = R.Candidate("z", {"capability": None, "status": "DRAFT",
                             "semantic_identity": None, "domain": None,
                             "heading_path": None, "text": None})
    bare.fused = 0.05
    try:
        R._fragment_passage(bare)
        R._chunk_passage(bare)
        survived = True
    except Exception:
        survived = False
    check(survived,
          "a candidate with null columns builds an empty passage rather than "
          "raising - on either side")
    print()

    print("3. A re-ranker IS allowed to overturn fusion - unlike the nudge")
    # The nudge is bounded BELOW one fusion rank precisely so it cannot do
    # this. The re-ranker is not bounded, because reading the pair together is
    # a better reason to re-order than a status field is.
    installed(RERANK, lambda pairs: [float(i) for i in range(len(pairs))])
    ranked = [candidate(R, "a", 0.30), candidate(R, "b", 0.20),
              candidate(R, "c", 0.10)]
    before = [c.score for c in ranked]
    got = R._rerank("a question", ranked, R._fragment_passage)
    check([c.id for c in got] == ["c", "b", "a"],
          "fusion's last is first when the re-ranker says so")
    check([c.rerank_rank for c in got] == [1, 2, 3],
          "each carries its re-rank position")
    check("re-ranker #1" in got[0].why(),
          "and why() names it, so an order nobody can explain is impossible")
    check(sorted(c.score for c in got) == sorted(before),
          "THE SCORES DID NOT MOVE - score is still fused + nudge")
    check(got[0].score < got[-1].score,
          "so the winner now has the LOWEST fused score, which is the honest "
          "record of what happened rather than a rewritten one")
    print()

    print("4. At most twenty pairs are ever scored (docs/05 s4.4)")
    seen = []

    def counting(pairs):
        seen.append(len(pairs))
        return [float(i) for i in range(len(pairs))]

    installed(RERANK, counting)
    many = [candidate(R, "f%02d" % i, 1.0 - i * 0.01) for i in range(30)]
    got = R._rerank("a question", many, R._fragment_passage)
    check(seen == [RERANK.SHORTLIST],
          "exactly %d pairs were scored, out of 30 offered"
          % RERANK.SHORTLIST)
    check(len(got) == 30, "and all 30 are still returned")
    tail = got[RERANK.SHORTLIST:]
    check([c.id for c in tail] == ["f%02d" % i for i in range(20, 30)],
          "the ten it never saw keep their fusion order")
    check(all(c.rerank_rank is None for c in tail),
          "and carry no re-rank position, because nothing scored them")
    print()

    print("5. A backend that MISBEHAVES means fusion's order, never no answer")

    def throwing(_pairs):
        raise RuntimeError("a passage longer than the model's window")

    installed(RERANK, throwing)
    check(RERANK.scores("a question", ["one", "two"]) is None,
          "a backend that throws is absorbed into None")
    ranked = [candidate(R, "a", 0.30), candidate(R, "b", 0.20)]
    got = R._rerank("a question", ranked, R._fragment_passage)
    check([c.id for c in got] == ["a", "b"],
          "and the shortlist still comes back, in fusion's order")

    installed(RERANK, lambda pairs: [0.5])
    check(RERANK.scores("a question", ["one", "two"]) is None,
          "a backend returning too few scores is REFUSED, not aligned by "
          "guesswork - a silent misalignment would re-order by nothing at all")
    print()

    print("6. The Contest does not explain a re-ranked order with a fusion gap")
    installed(RERANK, lambda pairs: [float(i) for i in range(len(pairs))])
    ranked = [candidate(R, "a", 0.30), candidate(R, "b", 0.20),
              candidate(R, "c", 0.10)]
    got = R._rerank("a question", ranked, R._fragment_passage)
    contest = R.Contest(got, 200, 20, 40)
    check(contest.reranked,
          "the contest DERIVES that a re-ranker set this order - a caller "
          "cannot forget to tell it")
    check("RE-RANKER set this order" in contest.sentence(),
          "and says so first")
    check("COIN TOSS" not in contest.sentence(),
          "it does NOT call this a coin toss - the fusion gap did not decide "
          "anything here")
    check(contest.spread >= 0,
          "and the fusion spread is not NEGATIVE, which it would be if the "
          "spread were read off the re-ranked order instead of the scores")
    absent(RERANK)
    plain = R.Contest([candidate(R, "a", 0.30), candidate(R, "b", 0.20)],
                      200, 20, 40)
    check(not plain.reranked and "RE-RANKER" not in plain.sentence(),
          "and with no re-ranker the sentence is unchanged from Stage 0b's")
    print()

    print("7. The size is announced BEFORE any download (R-77), per-user (R-42)")
    said = RERANK.announcement()
    check(RERANK.SIZE in said, "the size is stated")
    check("sentence-transformers" in said, "the package is named")
    check("pip install --user" in said,
          "the install is per-user, no administrator rights (D-01)")
    check("OPTIONAL" in said.upper(),
          "and it says out loud that this is optional")
    check("still answers" in said.lower() or "still answer" in said.lower(),
          "and that Heron answers without it")
    print()

    print("8. warm() never raises, and never blocks the caller")
    RERANK._CACHE[:] = []
    RERANK.warm()
    check(True, "warm() returned")
    if RERANK._WARM_THREAD[0] is not None:
        RERANK._WARM_THREAD[0].join(timeout=30)
    check(not RERANK._WARMING.is_set(),
          "and the warm-up cleared its own flag rather than leaving the module "
          "permanently 'loading' - the encoder's first version of this fix did "
          "exactly that and loaded nothing at all")
    print()

    print("9. Through the real stack: re-rank happens BEFORE the cut to limit")
    home = tempfile.mkdtemp(prefix="heron-rerank-")
    papers = tempfile.mkdtemp(prefix="heron-rerank-papers-")
    os.environ["HERON_KNOWLEDGE"] = home
    try:
        import heron_scope as SCOPE
        import heron_search as SEARCH
        import heron_embed as EMBED
        import heron_ingest as I

        store = SCOPE.open_scope(SCOPE.GLOBAL)
        try:
            SEARCH.ensure_tables(store)
            EMBED.ensure_tables(store)
            path = os.path.join(papers, "heron-test-standard.md")
            with open(path, "w", encoding="utf-8") as handle:
                handle.write(DOCUMENT)
            I.ingest(store, path, added_by="tests")
            SEARCH.index_chunks(store)
            EMBED.index_chunks(store)

            question = "what insulation do ducts need"

            absent(RERANK)
            fused, _ = R.documents(store, question, limit=99)
            check(len(fused) > 3,
                  "there are more than three chunks to choose between")

            installed(RERANK, lambda pairs: [float(i)
                                             for i in range(len(pairs))])
            top, _ = R.documents(store, question, limit=2)
            check([c.id for c in top] == [c.id for c in reversed(fused)][:2],
                  "with limit=2 the answer is fusion's LAST two - so the "
                  "re-rank ran over the pool and not over the two it was "
                  "asked for")

            print()
            print("10. The re-ranker's score travels out of the answer")
            answer = R.find_documents(store, question)
            check(all("rerank_score" in c for c in answer.candidates),
                  "every chunk candidate carries rerank_score")
            check(any(c["rerank_score"] is not None
                      for c in answer.candidates),
                  "and it is a number when a re-ranker ran - a feature that "
                  "does not serialise does not exist in production")
            absent(RERANK)
            answer = R.find_documents(store, question)
            check(all(c["rerank_score"] is None for c in answer.candidates),
                  "and None, not absent, when none ran - so a reader can tell "
                  "'no opinion' from 'no field'")
        finally:
            store.close()
    finally:
        os.environ.pop("HERON_KNOWLEDGE", None)
        shutil.rmtree(home, ignore_errors=True)
        shutil.rmtree(papers, ignore_errors=True)
        absent(RERANK)
    print()

    print("11. The module needs nothing installed to be imported at all")
    check(RERANK.SHORTLIST == 20, "SHORTLIST is docs/05 s4.4's twenty")
    check(RERANK.ABSENT != RERANK.CROSS_ENCODER,
          "and the two states are distinguishable by name")
    print()

    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - the re-ranker seam is in, bounded to twenty pairs, and the")
    print("state this machine is actually in - nothing installed - leaves the")
    print("answer byte-for-byte what it was and says so out loud.")
    print()
    print("IT PROVES NOTHING ABOUT WHETHER RE-RANKING HELPS. The after")
    print("measurement Stage 7 asks for needs weights from huggingface.co,")
    print("which this container refuses. That clause is open and recorded as")
    print("open - see A10 in docs/NEEDS-CHECKING.md.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
