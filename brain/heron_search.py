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
import sqlite3
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
            hits        INTEGER NOT NULL DEFAULT 1,
            fingerprint TEXT
        );
        CREATE TABLE IF NOT EXISTS identities (
            phrase      TEXT PRIMARY KEY,
            fragment_id TEXT NOT NULL
        );
    """)
    # A store built before D-61 has the table without the column. ADD COLUMN is
    # the whole migration: an existing row keeps a NULL fingerprint, and
    # forget_stale() treats NULL as "written before anyone was checking" and
    # drops it. Losing a cache row costs one lookup; keeping one that no longer
    # matches its fragment is the failure this column exists to stop.
    try:
        store.db.execute("ALTER TABLE utterances ADD COLUMN fingerprint TEXT")
    except sqlite3.OperationalError as exc:
        if "duplicate column" not in str(exc):
            raise
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

    # D-61. The searchable text is rebuilt from the files every time, so a
    # fragment whose purpose changed is searched correctly the moment this
    # runs. The CACHE is the one thing that would survive that unchanged, and
    # it is the one thing that answers WITHOUT SEARCHING. Forgetting the rows
    # written against bytes that have since changed belongs here, in the same
    # pass, for the same reason.
    #
    # on_disk is handed over rather than reloaded: load_all() is the expensive
    # part and D-24 says re-indexing must stay free.
    forget_stale(store, on_disk)

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


# WHAT COUNTS AS EVIDENCE THAT A WORDING RESOLVED CORRECTLY. D-61 answers
# Q-43, and this tuple is the answer rather than a description of it.
#
# THE ONLY ONE ACCEPTED IS `RAN`. A keyword hit is a CANDIDATE, never a
# decision - this file says so in its own header and in ask()'s note - and
# caching a candidate makes the guess permanent: the next identical wording
# returns by route 2 and never searches at all, so a wrong answer becomes the
# FAST answer. That is precisely the confident-wrong-retrieval failure the
# keyword layer was built to avoid, and wiring find() to remember() would have
# built it in one line.
#
# The identity route is not here either, for the opposite reason: it already
# answers in one lookup, so caching it saves nothing and only adds a row that
# can go stale.
RAN = "ran"


class NotEvidence(Exception):
    """Something tried to cache a guess."""


def remember(store, text, fragment_id, evidence):
    """
    Record that this wording resolved to this fragment. Requires evidence.

    `evidence` HAS NO DEFAULT ON PURPOSE. A default would make the safe call
    and the dangerous call look identical at the call site, and the dangerous
    one is the one somebody reaches for when they are wiring this up in a
    hurry. Every caller has to say what it knows, out loud, in the argument.

    The fingerprint of the fragment's implementation is stored with the row.
    recall() does not check it - route 2 is the fast route and hashing files on
    it would be the wrong trade - forget_stale(), which index() calls, does.
    """
    if evidence != RAN:
        raise NotEvidence(
            "the utterance cache takes %r and nothing else, and %r is not it. "
            "A wording may be remembered once the fragment it resolved to has "
            "RUN AND COME BACK - not because retrieval ranked it first. "
            "Caching a candidate turns a guess into the fast path (D-61, "
            "Q-43)." % (RAN, evidence))

    ensure_tables(store)
    key = normalise(text)
    mark = _fingerprint_of(store, fragment_id)

    store.execute(
        "INSERT INTO utterances (utterance, fragment_id, fingerprint) "
        "VALUES (?,?,?) "
        "ON CONFLICT(utterance) DO UPDATE SET hits = hits + 1, "
        "fragment_id = ?, fingerprint = ?",
        (key, fragment_id, mark, fragment_id, mark))
    store.db.commit()


def _fingerprint_of(store, fragment_id):
    """
    The hash of one fragment's implementation, loading ONE folder.

    MEASURED, not assumed. The first version called FRAG.load_all() here, which
    reads all 360 fragment files to use one of them: remember() took 1,522 ms.
    The store already knows this fragment's folder, and loading that one folder
    costs 3.6 ms with the hash itself at 0.2 - so remember() now takes 5.5 ms,
    277 times less, on a path a modeller is waiting on.

    The saving is not why this is written down. The obvious version LOOKED
    fine: load_all() is what index() calls, it was already imported, and the
    cost only appears if somebody times it. That is the shape worth keeping.
    """
    row = store.execute("SELECT folder FROM fragments WHERE id = ?",
                        (fragment_id,)).fetchone()
    if not row or not row["folder"]:
        return None
    folder = row["folder"]
    if not os.path.isabs(folder):
        folder = os.path.join(FRAG.ROOT, folder)
    try:
        frag = FRAG.load(folder)
    except Exception:
        # A fragment the store lists and the disk has lost. recall() already
        # refuses to serve one whose row is gone from `fragments`; this is the
        # other half - remember nothing rather than remember it unmarked, which
        # forget_stale() would drop on the next index anyway.
        return None
    return frag.fingerprint() if frag else None


def forget_stale(store, on_disk=None):
    """
    Drop every cached wording whose fragment is no longer what it was.

    recall() already handles the DELETED fragment - a cache must never
    resurrect one. This is the case it could not see: a fragment that is
    EDITED KEEPS ITS ID, so the row still points at something real and still
    answers, with the wording somebody confirmed against code that has since
    changed underneath it.

    Re-indexing is when Heron re-reads the files, so re-indexing is when a row
    written against the old bytes should die. The hash is Fragment.fingerprint()
    - the same one D-30 uses to call a proof stale - because a cached wording
    and a recorded proof go out of date for exactly the same reason and should
    not be able to disagree about when.

    A NULL fingerprint is a row from before this existed. It is dropped rather
    than trusted: one lookup is a cheap price for never serving a row nobody
    was checking.
    """
    ensure_tables(store)
    rows = store.execute(
        "SELECT utterance, fragment_id, fingerprint FROM utterances").fetchall()
    if not rows:
        return 0

    if on_disk is None:
        on_disk, _ = FRAG.load_all()

    dropped = 0
    for row in rows:
        frag = on_disk.get(row["fragment_id"])
        now = frag.fingerprint() if frag else None
        if row["fingerprint"] is None or now is None or now != row["fingerprint"]:
            store.execute("DELETE FROM utterances WHERE utterance = ?",
                          (row["utterance"],))
            dropped += 1
    if dropped:
        store.db.commit()
    return dropped


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
