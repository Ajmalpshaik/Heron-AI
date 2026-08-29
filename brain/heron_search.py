# Heron-Agent:  HERON-RAG-RET-003, HERON-RAG-IDX-010
# Heron-Step:   9
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Finding a fragment by the exact words somebody typed.

    python brain/heron_search.py "select all ducts"

Step 9 of docs/27-build-order.md. Keywords BEFORE meaning, deliberately.

WHY EXACT WORDS COME FIRST (docs/05 s4)
---------------------------------------
BIM requests are full of tokens that must match exactly and that embeddings
handle worst: OST_DuctCurves, RBS_DUCT_BOTTOM_ELEVATION, a shared-parameter
GUID, "Revit 2024". Semantic search returns the NEARLY right parameter with
confidence, and in a system that can modify a model, nearly right is worse than
nothing. So this layer exists first and answers on its own wherever it can.

THREE ROUTES, AND ask() SAYS WHICH ONE ANSWERED
-----------------------------------------------
  identity   the request IS a fragment's canonical phrasing. One lookup, no
             search, no model call. The 400th "select all ducts" should cost
             what the 1st did minus the thinking
  cache      this exact wording was resolved before. Also one lookup - it is
             what the user actually says, rather than what the fragment is
             called
  keywords   FTS5 over the scope, ranked. The ordinary path

The route is reported rather than hidden, because "did it think, or did it
remember" is the question anybody debugging this will ask first.

ONLY THE IDENTITY ROUTE MAY RUN WITHOUT ASKING
----------------------------------------------
And only on a PROVEN or PRODUCTION fragment. A DRAFT fragment matching a
sentence exactly is still a fragment nobody has watched work - executing it
because the words lined up would make the lifecycle decorative. The cache
returns a CANDIDATE, never a decision: it records what was resolved before,
including what a human accepted, and replaying that silently would let one
accepted guess harden into a habit.

THE CACHE IS A CACHE
--------------------
Losing it costs a slower lookup and nothing else. It is the one thing in a
scope store that is not derivable from the files on disk, which is exactly why
it may never be the reason an answer is right.
"""

import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_scope as SCOPE                                   # noqa: E402
import heron_fragment as FRAG                                 # noqa: E402

# Only these may be executed off an exact identity match without asking.
RUNNABLE_UNASKED = ("PROVEN", "PRODUCTION")

# FTS5's own syntax characters. A user typing "300x300 duct?" is asking a
# question, not writing a query, and an unescaped ? or - is a syntax error
# rather than a poor result - which would read to them as Heron being broken.
_FTS_UNSAFE = re.compile(r'[^\w\s]', re.UNICODE)


def normalise(text):
    """One phrasing, one key. Case, spacing and trailing punctuation are not
    what makes two requests different."""
    return re.sub(r"\s+", " ", (text or "").strip().lower()).rstrip(".!?")


def _fts_query(text):
    """A user's sentence, as something FTS5 will accept.

    Every word becomes a prefix term joined by OR, so a partial word still
    matches and a missing word does not empty the result. Punctuation is
    dropped rather than escaped: it carries no meaning in a lookup and every
    one of these characters means something to FTS5.
    """
    terms = []
    for word in _FTS_UNSAFE.sub(" ", text or "").split():
        if not word:
            continue
        # PREFIX MATCHING ONLY WHERE A PREFIX MEANS SOMETHING.
        #
        # The point of a prefix is to catch word endings - duct, ducts,
        # ducting. A word of three characters or fewer has no stem worth
        # matching: "in*" hits instance, internal, insulation; "me*" hits
        # measure, member, metadata; "the*" hits there, these, thermal. Every
        # one of those is a false hit, and enough of them together outrank the
        # one word in the sentence that actually carried meaning.
        #
        # This was invisible with two fragments and wrong with seven: "show me
        # every duct in the model" ranked the SELECTION fragment above the duct
        # filter, because five stopword prefixes outvoted one real term. Found
        # by the library growing, which is the only way this class of defect
        # ever shows up.
        terms.append('"%s"*' % word if len(word) > 3 else '"%s"' % word)
    return " OR ".join(terms)


class Answer(object):
    """What a lookup found, and how."""

    def __init__(self, route, fragment_id=None, candidates=None, autorun=False,
                 note=""):
        self.route = route                 # identity | cache | keywords | nothing
        self.fragment_id = fragment_id
        self.candidates = candidates or []
        self.autorun = autorun
        self.note = note

    def __repr__(self):
        return "<Answer %s %s%s>" % (self.route, self.fragment_id or "-",
                                     " AUTORUN" if self.autorun else "")


# ---------------------------------------------------------------------------
# The index
# ---------------------------------------------------------------------------

def ensure_tables(store):
    """FTS5 over what the scope knows, plus the utterance cache."""
    store.db.executescript("""
        CREATE VIRTUAL TABLE IF NOT EXISTS fragment_text USING fts5(
            id UNINDEXED,
            semantic_identity,
            capability,
            domain,
            purpose
        );
        CREATE TABLE IF NOT EXISTS utterances (
            utterance   TEXT PRIMARY KEY,
            fragment_id TEXT NOT NULL,
            hits        INTEGER NOT NULL DEFAULT 1
        );
        CREATE TABLE IF NOT EXISTS identities (
            phrase      TEXT PRIMARY KEY,
            fragment_id TEXT NOT NULL
        );
    """)
    store.db.commit()


def index(store):
    """Rebuild the searchable text from the fragments the scope holds.

    Reads the fragment files for their purpose - the store keeps the metadata,
    the file keeps the prose, and duplicating the prose into the store would be
    a second copy to keep in step.
    """
    ensure_tables(store)
    store.execute("DELETE FROM fragment_text")
    store.execute("DELETE FROM identities")

    on_disk, _ = FRAG.load_all()
    indexed = 0

    for row in store.fragments():
        frag = on_disk.get(row["id"])
        purpose = (frag.data.get("purpose", "") if frag else "")
        said = frag.utterances() if frag else []

        # The utterances go into the searchable text as well as into the
        # identity table. They carry the words a modeller actually uses -
        # "ducts", "OST_DuctCurves", "on level 2" - none of which appear in a
        # capability named for the general case.
        store.execute(
            "INSERT INTO fragment_text (id, semantic_identity, capability, "
            "domain, purpose) VALUES (?,?,?,?,?)",
            (row["id"], " ".join([row["semantic_identity"]] + said),
             row["capability"], row["domain"], purpose))

        for phrase in [row["semantic_identity"]] + said:
            key = normalise(phrase)
            if not key:
                continue
            clash = store.execute(
                "SELECT fragment_id FROM identities WHERE phrase = ?",
                (key,)).fetchone()
            if clash and clash["fragment_id"] != row["id"]:
                # Two fragments claiming one phrasing cannot both be the
                # short-circuit answer for it. Neither is, and the sentence
                # falls through to the ranked search where a human can see
                # both - which is the honest outcome, not the tidy one.
                store.execute("DELETE FROM identities WHERE phrase = ?", (key,))
                store.execute(
                    "INSERT OR REPLACE INTO identities (phrase, fragment_id) "
                    "VALUES (?, ?)", (key, "__ambiguous__"))
                continue
            if clash and clash["fragment_id"] == "__ambiguous__":
                continue
            store.execute(
                "INSERT OR REPLACE INTO identities (phrase, fragment_id) "
                "VALUES (?,?)", (key, row["id"]))
        indexed += 1

    store.db.commit()
    return indexed


# ---------------------------------------------------------------------------
# The three routes
# ---------------------------------------------------------------------------

def short_circuit(store, text):
    """Route 1. The request IS a fragment's canonical phrasing.

    Returns (fragment_id, status) or (None, None). No search runs, and nothing
    is asked of a model - docs/05 s4: the common case should cost one lookup.
    """
    row = store.execute(
        "SELECT fragment_id FROM identities WHERE phrase = ?",
        (normalise(text),)).fetchone()
    if not row or row["fragment_id"] == "__ambiguous__":
        # Ambiguous is a miss on purpose. Two fragments answering to one
        # sentence means the short circuit has no right answer, and picking
        # one would be the confident-wrong failure this layer exists to avoid.
        return None, None

    got = store.execute("SELECT status FROM fragments WHERE id = ?",
                        (row["fragment_id"],)).fetchone()
    if not got:
        # The identity table outlived its fragment. Treat as a miss rather than
        # returning an id nothing backs.
        return None, None
    return row["fragment_id"], got["status"]


def recall(store, text):
    """Route 2. This exact wording was resolved before."""
    row = store.execute(
        "SELECT fragment_id, hits FROM utterances WHERE utterance = ?",
        (normalise(text),)).fetchone()
    if not row:
        return None, 0

    alive = store.execute("SELECT 1 FROM fragments WHERE id = ?",
                          (row["fragment_id"],)).fetchone()
    if not alive:
        # The fragment is gone. A cache must never resurrect it.
        store.execute("DELETE FROM utterances WHERE utterance = ?",
                      (normalise(text),))
        store.db.commit()
        return None, 0
    return row["fragment_id"], row["hits"]


def remember(store, text, fragment_id):
    """Record that this wording resolved to this fragment."""
    ensure_tables(store)
    key = normalise(text)
    store.execute(
        "INSERT INTO utterances (utterance, fragment_id) VALUES (?,?) "
        "ON CONFLICT(utterance) DO UPDATE SET hits = hits + 1, fragment_id = ?",
        (key, fragment_id, fragment_id))
    store.db.commit()


def keywords(store, text, limit=5):
    """Route 3. FTS5 over the scope, best first."""
    ensure_tables(store)          # asked before indexing is a normal order
    query = _fts_query(text)
    if not query:
        return []
    rows = store.execute(
        "SELECT t.id, f.capability, f.status, f.kind "
        "FROM fragment_text t JOIN fragments f ON f.id = t.id "
        "WHERE fragment_text MATCH ? ORDER BY rank LIMIT ?",
        (query, limit)).fetchall()
    return [dict(r) for r in rows]


def ask(store, text, limit=5):
    """One question, one Answer, and it says which route produced it."""
    ensure_tables(store)

    fid, status = short_circuit(store, text)
    if fid:
        return Answer("identity", fid,
                      autorun=status in RUNNABLE_UNASKED,
                      note=("%s is %s, so it may run without asking" % (fid, status))
                      if status in RUNNABLE_UNASKED else
                      ("%s matched exactly but is %s - nobody has watched it work, "
                       "so it is offered, not run" % (fid, status)))

    fid, hits = recall(store, text)
    if fid:
        return Answer("cache", fid, note="seen %d time(s) before; a candidate, "
                                         "never a decision" % hits)

    found = keywords(store, text, limit)
    if not found:
        return Answer("nothing", note="no fragment matched these words")
    return Answer("keywords", found[0]["id"], candidates=found,
                  note="%d candidate(s) by keyword" % len(found))


def main(argv):
    if not argv:
        print(__doc__.strip().splitlines()[0])
        print()
        print('  python brain/heron_search.py "select all ducts"')
        return 2

    store = SCOPE.open_scope(SCOPE.GLOBAL)
    try:
        n = index(store)
        text = " ".join(argv)
        answer = ask(store, text)
        print("Asked:  %s" % text)
        print("Route:  %s" % answer.route)
        print("Found:  %s" % (answer.fragment_id or "nothing"))
        print("        %s" % answer.note)
        for c in answer.candidates:
            print("          %-14s %-28s %s" % (c["id"], c["capability"], c["status"]))
        print()
        print("%d fragment(s) indexed in the global scope." % n)
        return 0
    finally:
        store.close()


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
