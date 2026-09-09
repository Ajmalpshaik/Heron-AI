#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Does adding the composition graph as a third retrieval stream help? Measure it.

    python tools/measure-graph.py
    python tools/measure-graph.py --weight 0.3 --seeds 5

Q-52, run rather than argued. docs/33 s5.16: `gbrain` reports +31.4 points P@5
from a graph stream over its graph-disabled variant, on a corpus of prose about
people. Heron's graph is a different object - `composes_into` / `composes_from`,
derived from the contracts: A provides what B needs - so the direction is
evidence and the magnitude is nothing. This is Heron's own number.

WHERE THE GROUND TRUTH COMES FROM, AND WHAT IT IS WORTH
--------------------------------------------------------
Every fragment declares a `semantic-identity`: one sentence saying what it is
for. By construction, that sentence should retrieve that fragment. So the
library is its own answer key - 360 questions whose right answer is known
without anybody labelling anything.

THAT IS A REAL ANSWER KEY AND AN EASY ONE, and both halves matter. It is real
because nobody wrote it to make retrieval look good; it has been the matching
text since Step 7. It is easy because a query that IS the declared phrasing is
the best possible input, and no modeller types that.

So the same measurement runs over four query shapes, three of them degraded on
purpose:

    exact       the declared phrasing, unchanged
    no-first    the first word dropped        ("show me every duct" -> "me ...")
    content     words of 4+ characters only   - roughly what a hurried person types
    half        the first half of the words   - a sentence trailing off

The degraded shapes are where a graph could earn its place: they are the cases
where the right answer is NOT already first, and a neighbour of a good hit is a
plausible way to reach it.

WHAT THE GRAPH STREAM IS, HERE
-------------------------------
gbrain's edges join documents that reference each other. Heron's join fragments
whose CONTRACTS fit. So "expand the query through the graph" means: take the
fragments the existing two routes ranked highest, and offer what composes with
them.

    seeds     the top N of the fused baseline
    stream    every neighbour of a seed, ranked by how many seeds reach it
    fusion    a third RRF term at `--weight`, alongside words and nearness

THE HONEST PREDICTION, WRITTEN BEFORE THE RUN
----------------------------------------------
This is recorded so the result cannot be read as whatever was hoped for.

For a query whose right answer is ALREADY first, a third stream can only leave
it there or push it down: every neighbour it adds is a competitor. So a gain,
if there is one, has to come from the degraded shapes, and a loss will show up
first in P@1 on the exact shape. A tool that can only report good news is not a
measurement.

WHAT THIS DOES NOT MEASURE
---------------------------
Whether a modeller gets a better answer. It measures whether the fragment whose
own sentence was typed comes back first, which is a proxy chosen because it is
honest and available, not because it is the question. D-30 needs a real model,
and nothing here has met one.
"""

import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_scope as SCOPE                                    # noqa: E402
import heron_search as SEARCH                                  # noqa: E402
import heron_embed as EMBED                                    # noqa: E402
import heron_retrieve as RETRIEVE                              # noqa: E402
import heron_graph as GRAPH                                    # noqa: E402

POOL = 20
LIMIT = 5


# ---------------------------------------------------------------------------
# The query shapes
# ---------------------------------------------------------------------------

def shapes(text):
    """(name, query) for each way of asking the same thing.

    Deterministic on purpose - no sampling, so two runs of this tool on one
    library produce the same numbers and a change in them is a change in
    Heron rather than in the dice.
    """
    words = text.split()
    out = [("exact", text)]
    if len(words) > 1:
        out.append(("no-first", " ".join(words[1:])))
    content = [w for w in words if len(w) >= 4]
    if content and len(content) != len(words):
        out.append(("content", " ".join(content)))
    if len(words) > 3:
        out.append(("half", " ".join(words[:len(words) // 2])))
    return out


# ---------------------------------------------------------------------------
# The two rankings
# ---------------------------------------------------------------------------

def baseline(store, text, revit=None):
    """Today's fused ranking, as a list of ids, best first.

    Calls heron_retrieve.retrieve() - the fused ranker - and NOT find(),
    which would answer an exact declared phrasing from the identity table
    without ranking anything. Measuring find() on this answer key would
    measure the short circuit and report it as retrieval.
    """
    ranked, _excluded = RETRIEVE.retrieve(store, text, revit=revit,
                                          limit=POOL, pool=POOL)
    return ranked


def neighbour_map(fragments):
    """Every fragment's neighbours, computed ONCE.

    composes_into() walks the whole library per call. Called per seed per
    query that is 360 x 5 x 1,260 contract comparisons, and the first run of
    this tool took 275 seconds because of it. The map is the same answer
    computed once - and it makes a weight sweep affordable, which is what
    turns "the graph lost" into "the graph lost at every setting tried".
    """
    out = {}
    for fragment_id in fragments:
        out[fragment_id] = set(GRAPH.composes_into(fragment_id, fragments)
                               + GRAPH.composes_from(fragment_id, fragments))
        out[fragment_id].discard(fragment_id)
    return out


def with_graph(store, ranked, weight, seeds, fragments, nbrs=None):
    """The same ranking with a third stream fused in.

    Rebuilt from the baseline's own candidates rather than re-running the
    routes, so the only difference between the two numbers is the graph. A
    second call to retrieve() would also re-run the embedding route, and any
    difference there would land in this measurement wearing the graph's name.
    """
    scored = dict((c.id, c.score) for c in ranked)
    if not ranked:
        # THE SAME SHAPE AS THE NON-EMPTY PATH. Returning a bare [] here raised
        # ValueError in the caller, which always unpacks `ids, _known`. It
        # never fired in the recorded run because no query came back empty -
        # but `--revit 2019` empties the whole library at the version wall, so
        # the one setting most worth measuring was the one that crashed.
        # Found by Codex on PR #44, 2026-09-09.
        return [], {}

    # The seeds are the fragments the existing routes were most sure of.
    reached = {}
    for seed in ranked[:seeds]:
        for neighbour in (nbrs or {}).get(seed.id, ()):
            reached[neighbour] = reached.get(neighbour, 0) + 1

    # Ranked by how many seeds reach it - the graph's own opinion of strength.
    order = sorted(reached.items(), key=lambda kv: (-kv[1], kv[0]))
    for rank, (fragment_id, _hits) in enumerate(order, start=1):
        if rank > POOL:
            break
        scored[fragment_id] = (scored.get(fragment_id, 0.0)
                               + weight / (RETRIEVE.RRF_K + rank))

    known = dict((c.id, c) for c in ranked)
    return sorted(scored, key=lambda i: (-scored[i], i)), known


def rank_of(ids, truth):
    """1-based rank of the right answer, or None if it is not in the list."""
    for i, fragment_id in enumerate(ids, start=1):
        if fragment_id == truth:
            return i
    return None


# ---------------------------------------------------------------------------
# Scoring
# ---------------------------------------------------------------------------

class Tally(object):
    def __init__(self):
        self.asked = 0
        self.at1 = 0
        self.at5 = 0
        self.mrr = 0.0
        self.missing = 0

    def add(self, rank):
        self.asked += 1
        if rank is None:
            self.missing += 1
            return
        if rank == 1:
            self.at1 += 1
        if rank <= 5:
            self.at5 += 1
        self.mrr += 1.0 / rank

    def pct(self, n):
        return (100.0 * n / self.asked) if self.asked else 0.0

    def row(self):
        return (self.pct(self.at1), self.pct(self.at5),
                (self.mrr / self.asked) if self.asked else 0.0)


def main(argv):
    weight = 0.3
    seeds = 5
    revit = None
    if "--weight" in argv:
        weight = float(argv[argv.index("--weight") + 1])
    if "--seeds" in argv:
        seeds = int(argv[argv.index("--seeds") + 1])
    if "--revit" in argv:
        revit = argv[argv.index("--revit") + 1]

    store = SCOPE.open_scope(SCOPE.GLOBAL)
    try:
        if store.count() == 0:
            built, problems = SCOPE.rebuild()
            store.close()
            store = SCOPE.open_scope(SCOPE.GLOBAL)
            if problems:
                for line in problems:
                    print("  PROBLEM %s" % line)
        SEARCH.index(store)
        EMBED.index(store)

        backend, _why = EMBED.backend()
        fragments = GRAPH._loaded()

        # THE ANSWER KEY MUST OBEY THE SAME VERSION WALL AS RETRIEVAL.
        # Without this, `--revit 2020` asked about fragments retrieval had
        # correctly removed and scored their correct absence as a miss,
        # depressing P@1, P@5 and MRR in BOTH arms. It did not touch the
        # recorded Q-52 numbers, which were run with no --revit and therefore
        # no wall - but it would have made any release-specific re-run lie.
        # Found by Codex on PR #44, 2026-09-09.
        rows = [r for r in store.fragments() if r["semantic_identity"]]
        if revit:
            eligible, _excluded = RETRIEVE.eligible(store, revit)
            allowed = set(r["id"] for r in eligible)
            before = len(rows)
            rows = [r for r in rows if r["id"] in allowed]
            if len(rows) != before:
                print("  version wall    %d of %d fragments do not support "
                      "%s, so they are not asked about either"
                      % (before - len(rows), before, revit))
        print("MEASURING THE GRAPH AS A THIRD RETRIEVAL STREAM")
        print("=" * 70)
        print("Q-52, run rather than argued. docs/33 s5.16.")
        print()
        print("  library        %d fragments, %d with a declared phrasing"
              % (store.count(), len(rows)))
        print("  vector backend %s" % backend)
        print("  graph          %d fragments loaded for composition"
              % len(fragments))
        print("  settings       weight=%.2f  seeds=%d  pool=%d"
              % (weight, seeds, POOL))
        print()
        print("  The answer key is each fragment's own semantic-identity: the")
        print("  sentence that should return it. Real, and easy - so three")
        print("  degraded shapes are measured beside the exact one.")
        print()

        nbrs = neighbour_map(fragments)

        # EVERY SETTING TRIED, not one. A single setting that loses is a
        # setting; a sweep that loses at all of them is an answer. The
        # baseline is computed once per query and reused for every setting,
        # so the arms differ only in the graph.
        settings = [(weight, seeds)]
        if "--sweep" in argv:
            settings = [(0.05, 3), (0.10, 3), (0.30, 3),
                        (0.05, 1), (0.10, 5), (0.30, 5)]

        base = {}
        graph = dict((s, {}) for s in settings)
        for row in rows:
            truth = row["id"]
            for name, query in shapes(row["semantic_identity"]):
                ranked = baseline(store, query, revit=revit)
                base.setdefault(name, Tally()).add(
                    rank_of([c.id for c in ranked], truth))
                for setting in settings:
                    w, sd = setting
                    ids, _known = with_graph(store, ranked, w, sd, fragments,
                                             nbrs)
                    graph[setting].setdefault(name, Tally()).add(
                        rank_of(ids, truth))

        edge_counts = [len(v) for v in nbrs.values()]

        print("%-10s %-28s %-28s" % ("", "TODAY (words + nearness)",
                                     "WITH THE GRAPH"))
        print("%-10s %8s %8s %8s   %8s %8s %8s"
              % ("shape", "P@1", "P@5", "MRR", "P@1", "P@5", "MRR"))
        print("-" * 70)
        first = settings[0]
        for name in ("exact", "no-first", "content", "half"):
            if name not in base:
                continue
            b1, b5, bm = base[name].row()
            g1, g5, gm = graph[first][name].row()
            print("%-10s %7.1f%% %7.1f%% %8.3f   %7.1f%% %7.1f%% %8.3f"
                  % (name, b1, b5, bm, g1, g5, gm))

        if len(settings) > 1:
            print()
            print("EVERY SETTING TRIED - P@1 on each shape, minus today")
            print("-" * 70)
            print("%-14s %9s %9s %9s %9s" % ("weight/seeds", "exact",
                                             "no-first", "content", "half"))
            for setting in settings:
                w, sd = setting
                cells = []
                for name in ("exact", "no-first", "content", "half"):
                    if name not in base:
                        cells.append("      -")
                        continue
                    cells.append("%+8.1f" % (graph[setting][name].row()[0]
                                             - base[name].row()[0]))
                print("%-14s %s" % ("%.2f / %d" % (w, sd), " ".join(cells)))

        print()
        print("MOVEMENT (with the graph, minus today)")
        print("-" * 70)
        worse = better = 0
        for name in ("exact", "no-first", "content", "half"):
            if name not in base:
                continue
            b1, b5, bm = base[name].row()
            g1, g5, gm = graph[first][name].row()
            d1, d5, dm = g1 - b1, g5 - b5, gm - bm
            for delta in (d1, d5):
                if delta < -0.05:
                    worse += 1
                elif delta > 0.05:
                    better += 1
            print("%-10s P@1 %+6.1f   P@5 %+6.1f   MRR %+7.3f"
                  % (name, d1, d5, dm))

        if edge_counts:
            edge_counts.sort()
            print()
            print("THE GRAPH ITSELF")
            print("-" * 70)
            print("  neighbours per fragment (sample of %d): median %d, "
                  "worst %d, none for %d of them"
                  % (len(edge_counts), edge_counts[len(edge_counts) // 2],
                     edge_counts[-1],
                     sum(1 for n in edge_counts if n == 0)))

        print()
        if better and not worse:
            print("READ IT AS: the graph helped and cost nothing measurable.")
        elif worse and not better:
            print("READ IT AS: the graph cost something and returned nothing.")
        elif better and worse:
            print("READ IT AS: a TRADE, not a win. Which side matters is a")
            print("decision about what a wrong first answer costs a modeller,")
            print("and that is the owner's, not this tool's.")
        else:
            print("READ IT AS: no measurable difference either way.")

        print()
        print("Not a gate; exits 0. It measures whether the fragment whose own")
        print("sentence was typed comes back first - a proxy chosen because it")
        print("is honest and available, not because it is the question. D-30")
        print("needs a real model and nothing here has met one.")
        return 0
    finally:
        store.close()


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
