# Heron-Agent:  HERON-RAG-RNK-006
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Which fragments turn up in shortlists they have no claim on?

    python tools/check-intrusion.py
    python tools/check-intrusion.py --top 20

THE QUESTION check-routing.py CANNOT ASK
----------------------------------------
`check-routing.py` asks whether each fragment still wins its OWN sentences. It
is a per-fragment question and it says nothing about the shortlist a user
actually sees. A fragment can win every sentence it declares and still appear
in the top five for forty sentences belonging to other people - and that is what
degrades a shortlist, because five slots filled by three plausible answers and
two irrelevant ones is a worse answer than three.

So this asks the opposite: for every utterance in the library, who ELSE came
back? A fragment's intrusion count is how many shortlists it appeared in that
were not about it.

WHAT AN INTRUSION IS NOT
------------------------
It is NOT automatically a defect, for the same reason a routing collision is
not. Two fragments can both fairly answer one sentence, and a shortlist is meant
to hold more than one candidate - that is why it is a shortlist. A high count
means a fragment is reached by a lot of vocabulary, which is worth LOOKING at
and is not by itself wrong.

WHAT IT MEASURED, THE FIRST TIME IT RAN (2026-08-31, 59 fragments, lexical)
--------------------------------------------------------------------------
The obvious suspect was PURPOSE LENGTH. Fragments here carry long purposes -
21 to 523 words, median 200 - and the whole purpose is indexed, so a long one
has more terms to match on. That hypothesis was tested and is WRONG, or nearly:

    correlation(purpose words, intrusions) = 0.313, over 330 utterances

Quote what THIS TOOL prints, never this line. An earlier hand-written probe on
the same day gave 0.234 over 312 utterances - a different corpus one commit
back, not a different finding. Two numbers for one measurement is how a figure
becomes a remembered composite that matches no run that ever happened, which is
a failure this repository has already had once and now has a checker for.

Weak either way. The counter-examples are decisive rather than marginal: `set-selection`
has the SHORTEST purpose but one of the highest intrusion counts, and
`group-by-assembly` has one of the longest with ZERO. Writing shorter purposes
would not have fixed this, and it is worth having measured before anyone spends
a day shortening them.

What the top of the list actually shares is GENERIC UTTERANCE VOCABULARY -
"show me...", "what ... do we have", "which ... are in this model", "get the
...". Every one of those is a real sentence a modeller says, and none of them
should be taken away: that is the one response brain/retrieval-history.md rules
out. The lexical backend matches the function words and cannot tell that "show
me the drawing list" and "show me every duct" want different things.

So the finding is about the RETRIEVAL LAYER, not about any fragment - the same
place D-47 landed from the other direction. Register item A7, the trained
embedding backend, is the thing that would separate them, and it has never run
here.

WHY IT IS NOT A GATE
--------------------
Exits 0 whatever it finds, for the same reason check-routing.py does. A gate
here would be a gate on how ordinary somebody's phrasing is.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

FRAGMENTS = os.path.join(ROOT, "brain", "fragments")


def correlation(xs, ys):
    """Pearson, written out - one number, and no dependency for it."""
    if len(xs) < 2:
        return 0.0
    mx = sum(xs) / float(len(xs))
    my = sum(ys) / float(len(ys))
    top = sum((x - mx) * (y - my) for x, y in zip(xs, ys))
    left = sum((x - mx) ** 2 for x in xs)
    right = sum((y - my) ** 2 for y in ys)
    bottom = (left * right) ** 0.5
    return top / bottom if bottom else 0.0


def main(argv):
    top = 12
    if "--top" in argv:
        top = int(argv[argv.index("--top") + 1])

    import heron_scope as SCOPE
    import heron_search as SEARCH
    import heron_embed as EMBED
    import heron_fragment as FRAG
    import heron_retrieve as RETRIEVE

    # NO STORE, NO CHECK - AND SAY SO IN ONE LINE RATHER THAN A TRACEBACK.
    # With no %APPDATA% and no HERON_KNOWLEDGE there is nowhere to keep a
    # knowledge store, and heron_scope raises ValueError from four frames
    # down. Until 2026-09-21 that arrived as an unhandled traceback and exit
    # 1, while .github/workflows/gates.yml said of this tool and its pair
    # that they "say so rather than failing when there is none" - measured on
    # Linux with the variable unset, they did not say so and they did fail.
    # EXIT 2, which is the code this repository uses for "the tool could not
    # do its job": nothing was checked, so it is NOT a pass. Row 5b-71.
    if SCOPE.knowledge_dir() is None:
        sys.stderr.write(
            "COULD NOT RUN: no %APPDATA% and no HERON_KNOWLEDGE, so there is\n"
            "nowhere to keep a knowledge store. Set HERON_KNOWLEDGE to a\n"
            "folder - an empty one is enough - and run this again.\n")
        return 2

    on_disk = len([
        name for name in os.listdir(FRAGMENTS)
        if os.path.exists(os.path.join(FRAGMENTS, name, "fragment.yaml"))
    ])
    store = SCOPE.open_scope(SCOPE.GLOBAL)
    # Same guard check-routing.py carries, and for the same reason: a stale
    # store reports a library it does not hold, and the numbers below would be
    # about a smaller library than the one on disk.
    if store.count() != on_disk:
        was = store.count()
        store.close()
        SCOPE.rebuild()
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        print("  (store held %d of %d fragment(s) on disk - rebuilt)"
              % (was, on_disk))

    try:
        SEARCH.index(store)
        EMBED.index(store)

        found, _problems = FRAG.load_all()
        words = dict((fid, len((frag.data.get("purpose", "") or "").split()))
                     for fid, frag in found.items())

        intrusions = dict((fid, 0) for fid in found)
        own = dict((fid, 0) for fid in found)
        sentences = 0

        for fid, frag in found.items():
            for said in frag.utterances():
                sentences += 1
                shortlist, _excluded = RETRIEVE.retrieve(store, said, limit=5)
                for candidate in shortlist:
                    if candidate.id == fid:
                        own[fid] += 1
                    elif candidate.id in intrusions:
                        intrusions[candidate.id] += 1

        backend, _why = EMBED.backend()
        rows = sorted(((intrusions[i], words.get(i, 0), i) for i in found),
                      reverse=True)

        print("Fragments: %d   utterances: %d   backend: %s"
              % (len(found), sentences, backend))
        print()
        print("APPEARS IN SHORTLISTS IT DOES NOT OWN (top %d):" % top)
        print()
        for count, length, fid in rows[:top]:
            slug = found[fid].slug
            print("  %4d of %-4d shortlists   %4d words   %-14s %s"
                  % (count, sentences, length, fid, slug))

        clean = [(c, w, i) for c, w, i in rows if c == 0]
        if clean:
            print()
            print("REACHED ONLY BY ITS OWN WORDS (%d):" % len(clean))
            for _count, length, fid in clean[:top]:
                print("  %4d words   %-14s %s" % (length, fid, found[fid].slug))

        r = correlation([w for _c, w, _i in rows], [c for c, _w, _i in rows])
        print()
        print("correlation(purpose words, intrusions) = %.3f" % r)
        if abs(r) < 0.4:
            print()
            print("  Weak. PURPOSE LENGTH IS NOT WHAT DRIVES THIS, which is worth")
            print("  knowing before anybody spends a day shortening prose: the")
            print("  shortest purpose in the library is among the most intrusive")
            print("  and one of the longest intrudes on nothing at all.")
            print("  What the top of the list shares is generic phrasing -")
            print('  "show me...", "which ... are in this model". Those are real')
            print("  sentences and none of them may be taken away to buy a")
            print("  number; see brain/retrieval-history.md. It is the retrieval")
            print("  layer, not the fragments - A7 is what would separate them.")
        else:
            print()
            print("  STRONG ENOUGH TO ACT ON, and that is a CHANGE: it measured")
            print("  0.234 on 2026-08-31 and the conclusion drawn then was that")
            print("  length does not drive intrusion. Re-read that conclusion")
            print("  before trusting it - the library has moved underneath it.")

        print()
        print("An intrusion is not a defect. A shortlist is MEANT to hold more")
        print("than one candidate, and two fragments can fairly answer one")
        print("sentence. This exits 0: a gate here would be a gate on how")
        print("ordinary somebody's phrasing is.")
        return 0
    finally:
        store.close()


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
