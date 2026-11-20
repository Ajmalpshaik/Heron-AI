#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The usage counter: numbers on this PC, "helped" only when said, under one rank.

    python tests/test_usage_counter.py

D-70 decided Heron keeps a usage counter; D-191 is the owner's yes, 2026-11-20,
to its four open parts. Issue #1006 WN-12, R-16 and R-25:

  1. offered counts a clause once per showing; nothing counts it as helped
  2. helped is the modeller's word - refused for a clause the store lacks
  3. rate() is helped over offered, and None when nobody said anything
  4. the nudge settles a tie, and is held below one fusion rank
  5. the counter lives BESIDE the store: deleting the store keeps it, and a
     restored store's clauses still carry their counts (Golden Rule 11)
  6. no question text is ever kept
  7. the brain seam counts what it shows, and heron_helped counts the word
  8. the release builder refuses a counter file - it never leaves (D-70)

Every store here is a folder this suite makes and removes.
"""

import datetime
import importlib.util
import io
import json
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
sys.path.insert(0, os.path.join(ROOT, "tests", "support"))
import scratch_audit                                            # noqa: E402

# This run's audit lines go to a scratch folder, never the owner's real
# trail (FRAGMENT-ISSUES row 5b-1070).
scratch_audit.plant()

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def write(folder, name, body):
    path = os.path.join(folder, name)
    with io.open(path, "w", encoding="utf-8") as handle:
        handle.write(body)
    return path


# Two documents whose one clause is WORD FOR WORD the same, so the two routes
# score them identically: a dead heat, which is the only thing the nudge may
# settle. The titles differ so the two are two documents.
CLAUSE = (u"\n\n4.2 Insulation\n\nChilled water pipework shall be insulated "
          u"to 40 mm where it runs through a ceiling void.\n")
QUESTION = "how thick is chilled water pipe insulation in a ceiling void"


def main():
    home = tempfile.mkdtemp(prefix="heron-usage-kb-")
    papers = tempfile.mkdtemp(prefix="heron-usage-papers-")
    os.environ["HERON_KNOWLEDGE"] = home
    try:
        import heron_scope as SCOPE
        import heron_ingest as INGEST
        import heron_search as SEARCH
        import heron_embed as EMBED
        import heron_retrieve as RETRIEVE
        USAGE = None
        try:
            import heron_usage as USAGE
        except ImportError:
            pass
        check(USAGE is not None, "brain/heron_usage.py exists")
        if USAGE is None:
            raise SystemExit(1)

        store = SCOPE.open_scope(SCOPE.COMPANY)
        try:
            INGEST.ensure_tables(store)
            for name in ("Acme Standard A 2026", "Acme Standard B 2026"):
                INGEST.ingest(store, write(papers, name + ".md", name + CLAUSE),
                              title=name)
            SEARCH.index_chunks(store)
            EMBED.index_chunks(store)
            path = USAGE.usage_path(store.path)

            print("1. offered counts each showing once - and never as helped")
            first = RETRIEVE.find_documents(store, QUESTION)
            ids = [c["id"] for c in first.candidates if c.get("kind") == "chunk"]
            clause_ids = [c["id"] for c in first.candidates
                          if "4.2" in str(c.get("locator"))]
            check(len(clause_ids) == 2, "both copies of clause 4.2 are found: %r"
                  % clause_ids)
            check(USAGE.offered(path, ids + ids[:1]), "the count is kept")
            counts = USAGE.load(path)
            check(all(counts[i]["offered"] == 1 for i in ids),
                  "every clause shown counts once, a repeat in one showing too")
            check(all(counts[i]["helped"] == 0 for i in ids),
                  "and showing a clause never counts it as helped")
            print()

            print("2. helped is the modeller's word - and refused for a clause not here")
            refused = False
            try:
                USAGE.helped(store, "no-such-chunk")
            except USAGE.UnknownClause as why:
                refused = "not a clause" in str(why)
            check(refused, "a chunk id this store does not hold is refused by name")
            check("no-such-chunk" not in USAGE.load(path),
                  "and nothing is counted against it")
            day = datetime.date(2026, 11, 20)
            # The copy the tie put SECOND - the only one a nudge may lift. The
            # two must really be a dead heat for this to mean anything, which
            # check 4 asserts before it relies on it.
            winner = clause_ids[-1]
            entry = USAGE.helped(store, winner, today=day)
            check(entry["helped"] == 1 and entry["offered"] == 1
                  and entry["last_helped"] == "2026-11-20",
                  "it counts one helped, keeps the offered count, and the date: %r"
                  % entry)
            print()

            print("3. rate() - helped over offered; None when nobody said anything")
            check(USAGE.rate({"offered": 4, "helped": 1}) == 0.25, "1 of 4 is 0.25")
            check(USAGE.rate({"offered": 3, "helped": 0}) is None,
                  "never said is None, not 0 - no opinion is a different fact")
            check(USAGE.rate({"offered": 1, "helped": 5}) == 1.0,
                  "and it never passes 1")
            print()

            print("4. the nudge settles a tie - and never beats a better match")
            check(0 < RETRIEVE.USAGE_SPAN < RETRIEVE.ONE_RANK,
                  "USAGE_SPAN is above nothing and below one rank: %.6f < %.6f"
                  % (RETRIEVE.USAGE_SPAN, RETRIEVE.ONE_RANK))

            # A DEAD HEAT, built directly: rank fusion never gives two clauses
            # the same score through the routes, because each route ranks one
            # above the other. Two candidates with one fused score stand in.
            tied = [RETRIEVE.Candidate("a", {}), RETRIEVE.Candidate("b", {})]
            for one in tied:
                one.fused = 0.03
            added = RETRIEVE.apply_usage(tied[1], {"b": {"offered": 2, "helped": 2}})
            nothing = RETRIEVE.apply_usage(tied[0], {"b": {"offered": 2, "helped": 2}})
            ranked = sorted(tied, key=lambda c: (-c.score, c.id))
            check(ranked[0].id == "b" and nothing == 0.0,
                  "in a dead heat, the clause said to help comes first; the "
                  "other gets no nudge at all")
            check(abs(added - RETRIEVE.USAGE_SPAN) < 1e-15,
                  "helped every time shown is the whole USAGE_SPAN, no more")
            half = RETRIEVE.Candidate("c", {})
            check(abs(RETRIEVE.apply_usage(half, {"c": {"offered": 4, "helped": 2}})
                      - RETRIEVE.USAGE_SPAN / 2) < 1e-15,
                  "and helped half the times shown is half of it")

            # THE SAME TWO RULES WHEN A RE-RANKER SETS THE ORDER - asked of
            # _rerank directly, with a scorer this suite supplies, so every
            # machine checks it whether or not a cross-encoder is installed.
            # On that backend the cross-encoder's score decides; the usage
            # nudge rides in the tie-break (the fused score), so it decides
            # only between clauses the cross-encoder scored the same.
            was = RETRIEVE.RERANK.scores
            try:
                for label, given, expected in (
                        ("scored the same", [0.5, 0.5], ["b", "a"]),
                        ("scored a above b", [0.9, 0.1], ["a", "b"])):
                    pair = [RETRIEVE.Candidate("a", {}), RETRIEVE.Candidate("b", {})]
                    pair[0].fused = pair[1].fused = 0.03
                    RETRIEVE.apply_usage(pair[1], {"b": {"offered": 1, "helped": 1}})
                    RETRIEVE.RERANK.scores = (lambda q, ps, given=given:
                                              list(given[:len(ps)]))
                    got = [c.id for c in RETRIEVE._rerank("q", pair, lambda c: c.id)]
                    check(got == expected,
                          "re-ranked, the cross-encoder %s: %r - %s" % (
                              label, got,
                              "the clause said to help wins the tie"
                              if expected[0] == "b" else
                              "the nudge never beats the cross-encoder's order"))
            finally:
                RETRIEVE.RERANK.scores = was

            # THROUGH THE REAL STACK, ON WHATEVER BACKEND THIS MACHINE HAS. The
            # order is the LIST's order - never assumed to be the score's,
            # because with a re-ranker the cross-encoder sets it. `winner` is
            # the copy that came second before anything was said.
            again = RETRIEVE.find_documents(store, QUESTION)
            order = [c["id"] for c in again.candidates if c["id"] in clause_ids]
            before = [c["id"] for c in first.candidates if c["id"] in clause_ids]
            check(before[-1] == winner,
                  "before anything was said, %s came second" % winner[:12])
            old = dict((c["id"], c) for c in first.candidates)
            reranked = old[before[0]].get("rerank_score") is not None
            if reranked:
                # The deciding value is the cross-encoder's. Only an exact tie
                # lets the nudge decide.
                tied = old[before[0]]["rerank_score"] == old[winner]["rerank_score"]
                backend = "%s+rerank" % EMBED.backend()[0]
            else:
                # The deciding value is the fused score; a gap inside
                # USAGE_SPAN is one the nudge may close.
                tied = (old[before[0]]["score"] - old[winner]["score"]
                        < RETRIEVE.USAGE_SPAN)
                backend = EMBED.backend()[0]
            expected = [winner, before[0]] if tied else before
            check(order == expected,
                  "on this machine's backend (%s), the two copies %s - so after "
                  "the modeller said the second helped, the order is %r"
                  % (backend, "tied" if tied else "were not tied, and it "
                     "never beats a better match", [o[:8] for o in order]))
            told = [c for c in again.candidates if c["id"] == winner][0]
            check("helped 1 of 1" in told.get("why", "")
                  and told.get("usage", {}).get("helped") == 1,
                  "and the answer says what was counted: %r" % told.get("why"))
            check(again.contest is not None and again.contest.nudge_name == "usage"
                  and again.contest.nudged,
                  "and the contest names the usage nudge as the one in play")
            moved = told["score"] - [c for c in first.candidates
                                     if c["id"] == winner][0]["score"]
            check(abs(moved - RETRIEVE.USAGE_SPAN) < 1e-12,
                  "it moved by exactly USAGE_SPAN (%.6f) - half a rank"
                  % moved)
            print()

            print("6. no question text is ever kept")
            raw = io.open(path, encoding="utf-8").read()
            check(not any(word in raw for word in ("chilled", "ceiling", "thick")),
                  "the counter file holds no word of the question")
            check(set(json.loads(raw)["clauses"][winner]) <= {"offered", "helped",
                                                              "last_helped"},
                  "only the three numbers D-191 names, per clause")
            store_path = store.path
        finally:
            store.close()

        print()
        print("5. beside the store, not in it - Golden Rule 11")
        check(os.path.dirname(path) == os.path.dirname(store_path)
              and path.endswith(USAGE.USAGE_SUFFIX),
              "the counter sits beside the store's own file")
        os.remove(store_path)
        check(os.path.isfile(path), "deleting the store leaves the counter")
        store = SCOPE.open_scope(SCOPE.COMPANY)
        try:
            INGEST.restore(store)
            SEARCH.index_chunks(store)
            EMBED.index_chunks(store)
            back = RETRIEVE.find_documents(store, QUESTION)
            same = [c for c in back.candidates if c["id"] == winner]
            check(bool(same) and same[0].get("usage", {}).get("helped") == 1,
                  "a restored store gets the same chunk ids back, so the count "
                  "still reaches the same clause")
        finally:
            store.close()
        check(USAGE.forget(path) and not os.path.exists(path),
              "forget() deletes the counter, and only the counter")
        print()

        print("7. the brain seam counts what it shows; heron_helped counts the word")
        try:
            import heron_brain as BRAIN
        except ImportError:
            BRAIN = None
        if BRAIN is None:
            print("  -     NOT RUN: mcp/server/heron_brain.py could not be imported")
        else:
            BRAIN.standards(QUESTION, ["company"], limit=5)
            seen = USAGE.load(path)
            check(set(clause_ids) <= set(seen) and all(seen[i]["offered"] >= 1
                                                       for i in clause_ids),
                  "heron_standards counted every clause it showed as offered")
            check(all(seen[i]["helped"] == 0 for i in seen),
                  "and counted none as helped")
            said = BRAIN.helped(winner, scope="company")
            check(said["helped"] == 1 and "not your question" in said["said"]
                  and "nothing is sent" in said["said"],
                  "heron_helped counts it and says plainly what is kept: %r"
                  % said["said"][:70])
            bad = None
            try:
                BRAIN.helped(winner, scope="everywhere")
            except ValueError as why:
                bad = str(why)
            check(bad is not None, "an unknown scope is refused, never guessed")
            source = io.open(os.path.join(ROOT, "mcp", "server", "heron_tools.py"),
                             encoding="utf-8").read()
            check('"heron_helped":             (READ,    None)' in source,
                  "heron_helped is registered READ, sending nothing to Revit")
        print()

        print("8. the release builder refuses a counter file - it never leaves (D-70)")
        spec = importlib.util.spec_from_file_location(
            "build_release_assets", os.path.join(ROOT, "tools",
                                                 "build-release-assets.py"))
        tool = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(tool)
        name = "company" + USAGE.USAGE_SUFFIX
        check(any(name.endswith(shape)
                  for shape in getattr(tool, "STORE_SHAPES", ())),
              "%s is one of the shapes the download refuses" % USAGE.USAGE_SUFFIX)
    finally:
        shutil.rmtree(home, ignore_errors=True)
        shutil.rmtree(papers, ignore_errors=True)
        os.environ.pop("HERON_KNOWLEDGE", None)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1
    print("PASSED - the counter keeps numbers beside the store, counts helped")
    print("only on the modeller's word, settles a tie and stays under one rank.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
