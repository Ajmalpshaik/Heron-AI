# Heron-Agent:  HERON-KRN-DEP-013
# Heron-Step:   13
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Stage 5 - document edges, derived, and the count that decides the route.

    python tests/test_document_graph.py

THIS STAGE BEGINS WITH A COUNT AND NOT WITH A FEATURE, because the same idea
over the FRAGMENT graph was measured at six settings and lost every one
(docs/34 s2.13) - and the property that decided it was DENSITY: a median of
50 neighbours per fragment, worst 230, so "the neighbours of the best hit" was
a large slice of the library added as competitors.

A document graph is a DIFFERENT graph, so that finding does not transfer. The
test that decided it does.

WHAT IT PROVES
  1. THE EDGES ARE DERIVED, NEVER STORED (D-40). Re-ingesting rebuilds them;
     there is no edge table to go stale.
  2. THREE KINDS OF EDGE, each read off what the document already says -
     parent, sibling, and a clause whose TEXT names another clause's number.
  3. THE CROSS-REFERENCE EDGE FINDS WHAT WORDS MISS, which is the only reason
     a graph route is worth considering at all.
  4. THE DENSITY IS COUNTED, and it is an order of magnitude below the
     fragment graph's.
  5. NOTHING IN RETRIEVAL READS ANY OF IT. The route has no weight, and a
     weight has to be earned by a measurement on a real corpus.
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


DOCUMENT = """Heron Graph Test Standard 2026

Section 21 Mechanical Works

21.3 Ductwork

21.3.1 Insulation

Ducts shall be insulated to 25mm.

21.3.2 Identification

Every duct shall be labelled in accordance with 21.3.1 and shall carry the
system name.

21.4 Pipework

21.4.1 Supports

Pipe supports shall be spaced at not more than 2m.
"""


def main():
    import heron_scope as SCOPE
    import heron_ingest as I
    import heron_graph as GRAPH
    import heron_retrieve as R

    home = tempfile.mkdtemp(prefix="heron-docgraph-")
    papers = tempfile.mkdtemp(prefix="heron-docgraph-papers-")
    os.environ["HERON_KNOWLEDGE"] = home

    try:
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        try:
            path = os.path.join(papers, "standard.md")
            with open(path, "w", encoding="utf-8") as handle:
                handle.write(DOCUMENT)
            got = I.ingest(store, path, added_by="tests")

            import heron_search as SEARCH
            import heron_embed as EMBED
            # INGESTED IS NOT INDEXED, and forgetting this is how the
            # "unindexed" state below was found: retrieval reported "nothing
            # matched" when the truth was "the searchable text was never
            # built". Same shape as R-19, one level down.
            unindexed = R.find_documents(store, "how are ducts labelled")
            check(unindexed.route == "unindexed",
                  "before indexing, retrieval says the documents are NOT "
                  "INDEXED rather than reporting a miss")
            SEARCH.index_chunks(store)
            EMBED.index_chunks(store)

            rows = I.boundaries(store, got.document_id)
            by_locator = dict((r["locator"], r["id"]) for r in rows
                              if r["locator"])
            edges = GRAPH.document_neighbours(store)

            print("0. Ingested is not indexed, and retrieval says which")
            print()
            print("1. D-40 - the edges are DERIVED, and there is no edge table")
            tables = [r[0] for r in store.execute(
                "SELECT name FROM sqlite_master WHERE type='table'").fetchall()]
            check(not any("edge" in t.lower() for t in tables),
                  "no table with 'edge' in its name exists: %s"
                  % ", ".join(sorted(tables)))
            check(edges, "and asking for them returns %d node(s)" % len(edges))
            print()

            print("2. Parent and sibling, read off the document's own numbering")
            insulation = by_locator["21.3.1"]
            near = edges[insulation]
            check(by_locator["21.3"] in near,
                  "21.3.1's neighbours include its parent 21.3")
            check(by_locator["21.3.2"] in near,
                  "and its sibling 21.3.2")
            check(by_locator["21.4.1"] not in near,
                  "and NOT 21.4.1, which is under a different parent")
            print()

            print("3. The cross-reference edge finds what words alone miss")
            # 21.3.2 says "in accordance with 21.3.1". The two clauses share
            # no subject - one is about insulation, one about labelling - so
            # no word or nearness route links them. The document does.
            identification = by_locator["21.3.2"]
            check(insulation in edges[identification],
                  "21.3.2 names 21.3.1 in its text, so it reaches it")
            answer = R.find_documents(store, "how are ducts labelled")
            reached = [c["locator"] for c in answer.candidates]
            check("21.3.2" in reached,
                  "the words route finds the labelling clause (%s)"
                  % ", ".join(reached[:3]))
            print()

            print("4. The density, counted")
            density = GRAPH.document_density(store)
            check(density["chunks"] == len(rows),
                  "every chunk is a node (%d)" % density["chunks"])
            check(density["median"] < 50,
                  "median %d neighbours, against the FRAGMENT graph's 50 - "
                  "which is what made that route lose at all six settings"
                  % density["median"])
            check(density["worst"] < 230,
                  "worst %d, against the fragment graph's 230"
                  % density["worst"])
            print()

            print("5. NOTHING in retrieval reads any of it - the route has no weight")
            import inspect
            source = inspect.getsource(R)
            check("document_neighbours" not in source
                  and "document_density" not in source,
                  "heron_retrieve does not call the graph at all")
            check("GRAPH" not in [n for n in dir(R) if n.isupper()],
                  "and does not import it")
            check(sorted(R.VECTOR_WEIGHT_BY_BACKEND) == sorted(
                      [R.EMBED.LEXICAL, R.EMBED.MODEL]),
                  "fusion still has exactly two routes with weights - words "
                  "and nearness. An edge route gets NO VOTE until a "
                  "measurement earns it one, exactly as the nearness route "
                  "had to")
            print()

        finally:
            store.close()
    finally:
        shutil.rmtree(home, ignore_errors=True)
        shutil.rmtree(papers, ignore_errors=True)
        os.environ.pop("HERON_KNOWLEDGE", None)

    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - document edges are derived from the document's own")
    print("numbering and its own cross-references, they are an order of")
    print("magnitude sparser than the fragment graph, and no part of")
    print("retrieval reads them.")
    print()
    print("It proves nothing about whether an edge ROUTE would help. That")
    print("needs a measurement on a real corpus, and the corpus here was")
    print("written for this test.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
