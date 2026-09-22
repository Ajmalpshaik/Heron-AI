#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A measurement tool that ran with a setting nobody asked for.

    python tests/test_measure_graph.py

`tools/measure-graph.py` answers Q-52 by running it: does the composition
graph earn a place as a third retrieval stream? It is the last of the SIX
tools in `tools/` that no suite names, and the numbers it prints are in
docs/33 s5.16.

ITS SETTINGS WERE READ BY HAND FROM argv, AND A MISSPELLING WAS SILENCE.
Measured 2026-09-22:

    --wieght 0.05 --sedes 1   ran the whole measurement at weight=0.30
                              seeds=5, printed those as the settings, and
                              exited 0
    --weight                  IndexError: list index out of range
    --weight abc              ValueError: could not convert string to float
    --seeds 2.5               ValueError: invalid literal for int()

A number recorded for a setting nobody asked for is worse than no number,
because it is quoted afterwards.

IT DOES NOT RUN THE MEASUREMENT. A full run opens the knowledge store,
re-indexes it and asks 396 fragments four ways - minutes, and it writes
outside the repository. So `main()` is called only with argv it must REFUSE,
which returns before the store is opened, and everything else here is the
pure arithmetic: the query shapes, the rank, the tally, and the fusion.

WHAT IT CANNOT DO: it does not check whether the graph helps. That is what
the tool itself is for, and its answer is in docs/33.

    python tests/test_measure_graph.py
"""

import contextlib
import importlib.util
import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOL = os.path.join(ROOT, "tools", "measure-graph.py")

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def load():
    """The tool as a module, or None.

    It imports five brain modules at module level, so a missing optional
    dependency makes this None and the suite says NOT RUN rather than
    reporting a pass it did not earn.
    """
    try:
        spec = importlib.util.spec_from_file_location("measure_graph", TOOL)
        module = importlib.util.module_from_spec(spec)
        with contextlib.redirect_stdout(io.StringIO()):
            spec.loader.exec_module(module)
        return module
    except BaseException:                          # noqa: BLE001
        return None


class Hit(object):
    """A candidate shaped the way heron_retrieve returns them."""

    def __init__(self, ident, score):
        self.id = ident
        self.score = score


def main():
    print(__doc__.strip().splitlines()[0])
    print()

    tool = load()
    if tool is None:
        print("  tools/measure-graph.py would not load. It imports five brain")
        print("  modules, so this is usually a missing dependency rather than")
        print("  a defect. NOT RUN - and that is not a pass.")
        return 3

    check(True, "tools/measure-graph.py loads")

    print()
    print("1. The four query shapes, and the ones it declines to repeat")
    shapes = getattr(tool, "shapes", None)
    check(callable(shapes), "it still has shapes()")
    if callable(shapes):
        got = dict(shapes("show me every duct on this level"))
        check(got.get("exact") == "show me every duct on this level",
              "exact is the sentence unchanged")
        check(got.get("no-first") == "me every duct on this level",
              "no-first drops the first word, and gives %r"
              % got.get("no-first"))
        check(got.get("content") == "show every duct this level",
              "content keeps words of four or more, and gives %r"
              % got.get("content"))
        check(got.get("half") == "show me every",
              "half is the first half of the words, and gives %r"
              % got.get("half"))
        # A degraded shape identical to the exact one would be the same
        # question asked twice and counted twice.
        only_long = dict(shapes("ducts levels walls floors"))
        check("content" not in only_long,
              "content is skipped when every word is already long enough")
        check("no-first" not in dict(shapes("ducts")),
              "no-first is skipped for a one-word sentence")

    print()
    print("2. The rank, and the answer that is not there at all")
    rank_of = getattr(tool, "rank_of", None)
    check(callable(rank_of), "it still has rank_of()")
    if callable(rank_of):
        check(rank_of(["a", "b", "c"], "a") == 1, "first is rank 1")
        check(rank_of(["a", "b", "c"], "c") == 3, "third is rank 3")
        check(rank_of(["a", "b"], "z") is None,
              "an answer that never came back is None, not a large rank - a "
              "miss counted as rank 999 would still add to MRR")

    print()
    print("3. The tally counts a miss as a miss")
    Tally = getattr(tool, "Tally", None)
    check(Tally is not None, "it still has Tally")
    if Tally is not None:
        t = Tally()
        for rank in (1, 1, 4, None):
            t.add(rank)
        at1, at5, mrr = t.row()
        check(t.asked == 4 and t.missing == 1,
              "four asked, one missing, and it says %d and %d"
              % (t.asked, t.missing))
        check(abs(at1 - 50.0) < 1e-9, "P@1 is 50%%, and it is %.1f" % at1)
        check(abs(at5 - 75.0) < 1e-9, "P@5 is 75%%, and it is %.1f" % at5)
        check(abs(mrr - (1 + 1 + 0.25) / 4) < 1e-9,
              "MRR divides by everything asked, misses included - it is %.3f"
              % mrr)

    print()
    print("4. An empty ranking keeps its shape")
    # A bare [] here raised ValueError in the caller, which always unpacks
    # `ids, _known`. `--revit 2019` empties the library at the version wall,
    # so the one setting most worth measuring was the one that crashed.
    with_graph = getattr(tool, "with_graph", None)
    check(callable(with_graph), "it still has with_graph()")
    if callable(with_graph):
        ids, known = with_graph(None, [], 0.3, 5, {}, {})
        check(ids == [] and known == {},
              "an empty ranking returns two values, not one")

        print()
        print("5. A neighbour of a seed is offered, and the graph is the")
        print("   only difference between the two rankings")
        ranked = [Hit("a", 1.0), Hit("b", 0.5)]
        plain, _k = with_graph(None, ranked, 0.0, 2, {}, {})
        check(plain == ["a", "b"],
              "at weight 0 the ranking is unchanged, and it is %r" % plain)
        pushed, _k = with_graph(None, ranked, 99.0, 2, {},
                                {"a": {"z"}, "b": {"z"}})
        check(pushed[0] == "z",
              "a neighbour both seeds reach can outrank them at a high "
              "enough weight, and the order is %r" % pushed)
        check(set(pushed) == {"a", "b", "z"},
              "and nothing the baseline found is dropped")

    print()
    print("6. A SETTING NOBODY ASKED FOR IS REFUSED, NOT RUN")
    settings_from = getattr(tool, "settings_from", None)
    check(callable(settings_from),
          "the settings are read by something a test can call, so a bad one "
          "is refused BEFORE the store is opened")
    if callable(settings_from):
        for label, argv in (("a misspelt flag", ["--wieght", "0.05"]),
                            ("a second misspelt flag", ["--sedes", "1"]),
                            ("a flag with no value", ["--weight"]),
                            ("a weight that is not a number",
                             ["--weight", "abc"]),
                            ("seeds that are not whole", ["--seeds", "2.5"]),
                            ("a bare word", ["sweep"])):
            said = io.StringIO()
            try:
                with contextlib.redirect_stdout(said):
                    code = tool.main(argv)
            except BaseException as raised:         # noqa: BLE001
                code = "raised %s" % type(raised).__name__
            check(code == 2,
                  "%s is refused with 2, and it gives %r" % (label, code))
            check("weight=" not in said.getvalue(),
                  "%s: and no measurement was run" % label)

        good = settings_from(["--weight", "0.05", "--seeds", "3"])
        check(good[-1] is None,
              "and a setting it does have is accepted: %r" % (good,))

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - the arithmetic is what it says, and a setting nobody")
    print("asked for is refused rather than quietly replaced by a default.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
