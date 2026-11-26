# Heron-Agent:  HERON-RAG-EMB-008, HERON-RAG-VEC-009
# Heron-Step:   10
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Finding a fragment by something other than its exact words.

    python brain/heron_embed.py "stop the air going the wrong way"

Step 10 of docs/27-build-order.md.

EMBEDDINGS ARE COMPUTED ON THIS MACHINE. FULL STOP.
---------------------------------------------------
D-24 and D-26: local, no cloud opt-in, no setting, no API key, no account. A
site visit, a locked-down network and a laptop on a plane all have to work, and
re-indexing has to be FREE or it stops happening - an index nobody rebuilds
quietly stops matching what is on disk.

ONE BACKEND, BUILT IN, THE SAME ON EVERY PC (D-203)
---------------------------------------------------
  lexical   Hashed character n-grams. No download, no dependency, no network -
            it works the second Heron is installed.

            MEASURED 2026-08-28, not asserted (cosine, this exact code):

              duct / ducts            0.559   plurals and word endings, yes
              duct / ductwork         0.596
              sprinkler / sprinklers  0.818
              "select all ducts" /
                "all ducts selected"  0.845   word order does not matter
              duct / ducte            0.600   an inserted letter, yes
              duct / duckt            0.300   weakly
              duct / duc              0.447   a truncated word, yes
              duct / dcut             0.000   A SWAPPED PAIR: NOTHING
              diffuser / grille      -0.136   SYNONYMS: NOTHING
              level / floor          -0.091

            So: plurals, endings, word order and MOST typos - but a transposed
            pair destroys every n-gram at once and scores zero, and synonyms
            score zero or less because nothing here has been taught anything.

            IT IS NOT MEANING. "diffuser" and "grille" are the same thing to a
            modeller and are unrelated to this backend. Calling it semantic
            search would be a lie, so it is not called that anywhere - and the
            first draft of this docstring claimed it caught "dcut", which the
            measurement above disproved.

WHY THERE IS NO SECOND, TRAINED BACKEND ANY MORE. Until 2026-11-26 an optional
trained sentence model was used where one was installed. Setup never installed
it, so only the owner's PC ranked on it - a second search no other PC and no CI
run shared. Measured on 753 fragments that day it was not better on his own
questions (first place 30 against lexical's 34, top three 37 against 43) and
sent more plain questions to a change (15 against 9), so the owner removed it
(D-203, superseding D-201's "it stays optional"). One search is one thing to
test, and every modeller has it. `backend()` still says which one answered,
because a reporter that stopped saying it would be trusted to mean the same
thing after the next change too.
"""

import os
import re
import sys
import math
import struct
import sqlite3
import hashlib

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_scope as SCOPE                                   # noqa: E402
import heron_fragment as FRAG                                 # noqa: E402

DIMS = 256

LEXICAL = "lexical"


# ---------------------------------------------------------------------------
# The built-in backend
# ---------------------------------------------------------------------------

_WORD = re.compile(r"[a-z0-9]+")


def _tokens(text):
    """Words, plus the character n-grams inside them.

    The n-grams are what make this tolerant: "duct", "ducts" and "ducting"
    share most of their 3-grams, so they land near each other without anything
    having been taught that they are related. It is spelling, not meaning - but
    a modeller's typo is a spelling problem, and this fixes that much.
    """
    out = []
    for word in _WORD.findall((text or "").lower()):
        out.append(word)
        padded = "^%s$" % word
        for n in (3, 4):
            for i in range(len(padded) - n + 1):
                out.append(padded[i:i + n])
    return out


def lexical_vector(text, dims=DIMS):
    """A hashed bag of those tokens, L2-normalised.

    blake2b rather than Python's own hash(): hash() is SALTED PER PROCESS, so
    the same text would embed differently in every run and the stored vectors
    would silently stop matching anything after a restart. That failure looks
    exactly like "search got worse" and would be very hard to find.
    """
    vec = [0.0] * dims
    for token in _tokens(text):
        digest = hashlib.blake2b(token.encode("utf-8"), digest_size=8).digest()
        bucket = int.from_bytes(digest[:4], "big") % dims
        vec[bucket] += 1.0 if digest[4] & 1 else -1.0

    length = math.sqrt(sum(x * x for x in vec))
    if length == 0.0:
        return vec
    return [x / length for x in vec]


def stamp():
    """What to record beside a vector so a changed encoder invalidates it.

    The backend's name. There is one encoder, built in, so the name is all a
    stamp needs - and a row a removed encoder wrote (stamped `model...`) no
    longer matches it, so `index()` re-embeds that row lexically the first
    time it opens a store written before D-203.
    """
    return LEXICAL


def backend():
    """Which backend is in use, and what that means. Reported, never assumed.

    Always the built-in one since D-203 - said anyway, because every reporter
    prints it (heron_retrieve stores it on the candidate, the CLI prints it,
    the measuring tools read it) and a line saying what answered is how a
    reader knows the answer was not meaning.
    """
    return LEXICAL, ("built-in character n-grams - tolerant of spelling and "
                     "word order, but NOT meaning")


PYTHON_COMPARISON = "python"


def comparison():
    """(how vectors are compared, why) - said, never assumed (PROPOSALS F7).

    Always Python: D-194 (the owner, 2026-11-21) decided every stored vector
    is compared in plain Python and sqlite-vec is not used (row 5b-1171). The
    answer says whether it is installed, because the one wrong belief worth
    preventing is that installing it made search faster.
    """
    try:
        import sqlite_vec  # noqa: F401
        installed = True
    except ImportError:
        installed = False
    return PYTHON_COMPARISON, (
        "vectors are compared in Python - same answers, slower. sqlite-vec is "
        + ("installed but not used: nothing in Heron loads it"
           if installed else "not installed, and would not be used if it were")
        + " (D-194, row 5b-1171).")


def vector(text):
    return lexical_vector(text)


def encoder():
    """(stamp, a function from text to a vector) - TAKEN TOGETHER, once.

    THE STAMP AND THE ENCODER ARE ONE DECISION, and an indexing pass asks for
    them together so it can never store two encoders' work under one name. A
    review found that on 2026-09-11, when a second encoder could finish
    loading half way through a pass; there is one encoder now (D-203), and the
    pair is still taken together so a pass, its rows and its stamp cannot
    disagree.
    """
    return LEXICAL, lexical_vector


# ---------------------------------------------------------------------------
# Storing vectors, in the scope's own file
# ---------------------------------------------------------------------------
#
# EVERY VECTOR IS COMPARED IN PLAIN PYTHON - D-194, the owner's decision of
# 2026-11-21, superseding D-23's "sqlite-vec for meaning" (row 5b-1171). It is
# done over the stored bytes, identical answers, nothing to install, and
# nothing refused on a Python build with extension loading compiled out - the
# locked-down machine Heron's install promise is about. `comparison()` says so,
# and tools/heron-ready.py prints it (PROPOSALS F7). The loader that never had
# a caller, and sqlite-vec in the install lists, came out the same day (row
# 5b-1955); loading it again needs a new decision.
#
# Both keep the vectors IN THE SCOPE'S OWN FILE, which is the part that matters:
# one file per scope stays true either way (Golden Rule 5), and a vector never
# outlives the scope it belongs to.

def _pack(vec):
    return struct.pack("%df" % len(vec), *vec)


def _unpack(blob):
    return list(struct.unpack("%df" % (len(blob) // 4), blob))


FRAGMENT = "fragment"
CHUNK = "chunk"


def ensure_tables(store):
    store.db.executescript("""
        CREATE TABLE IF NOT EXISTS vectors (
            id        TEXT PRIMARY KEY,
            text_hash TEXT NOT NULL,
            backend   TEXT NOT NULL,
            embedding BLOB NOT NULL
        );
    """)
    # ONE TABLE, TWO KINDS OF THING IN IT - and a column that says which.
    #
    # A chunk id and a fragment id cannot collide, so the rows could have
    # shared this table silently. They must not: nearest() would then hand a
    # caller asking about fragments a clause it never asked for, and index()
    # walks the fragments, so chunk rows would be orphans nothing refreshed.
    # A column is cheaper than a second table with a second hashing scheme.
    #
    # ADD COLUMN is the whole migration, the same one heron_search already
    # does for `fingerprint`. A row written before this column reads NULL, and
    # every row written before this column was a fragment - so NULL means
    # fragment, and that is a fact about the history rather than a default.
    try:
        store.db.execute("ALTER TABLE vectors ADD COLUMN kind TEXT")
    except sqlite3.OperationalError as exc:
        if "duplicate column" not in str(exc):
            raise
    store.db.commit()


def _text_for(row, frag):
    """Everything about a fragment worth embedding, as one string.

    The utterances are in here deliberately: they are the words a modeller
    actually uses, and they are what makes an approximate match land on the
    right fragment rather than on the one whose formal name happens to be close.
    """
    said = frag.utterances() if frag else []
    return " ".join([row["semantic_identity"], row["capability"],
                     row["domain"]] + said +
                    ([frag.data.get("purpose", "")] if frag else []))


def index(store, force=False, source=SCOPE.ASK):
    """Embed every fragment whose text has CHANGED. Returns (embedded, skipped).

    Content-hashed, per docs/05 s7: a git checkout moves every file's mtime and
    changes none of their content, so anything keyed on time re-embeds the whole
    library for nothing. Keyed on content, that costs zero.

    THE TEXT COMES FROM `source`'s CARDS - heron_scope.builds_from()'s answer
    for this store's own file unless a caller hands one over: the MAIN
    checkout's cards for the shared store, whichever checkout asks (rows 136
    and 5b-404, the owner's "Always build from main" of 2026-10-21). Read from
    a worktree's own cards, a vector here was that session's unmerged words
    for every chat on the PC. None - main's cards are not on disk anywhere -
    embeds nothing and loads no encoder.
    """
    ensure_tables(store)
    if source is SCOPE.ASK:
        source = SCOPE.builds_from(getattr(store, "path", None))
    if source is None:
        return 0, store.count()
    cards, checkout = source
    name, embed = encoder()
    on_disk, _ = FRAG.load_all(cards, checkout=checkout)

    embedded = skipped = 0
    for row in store.fragments():
        frag = on_disk.get(row["id"])
        text = _text_for(row, frag)
        digest = hashlib.blake2b(text.encode("utf-8"), digest_size=16).hexdigest()

        if not force:
            have = store.execute(
                "SELECT text_hash, backend FROM vectors WHERE id = ?",
                (row["id"],)).fetchone()
            if have and have["text_hash"] == digest and have["backend"] == name:
                skipped += 1
                continue

        store.execute(
            "INSERT OR REPLACE INTO vectors (id, text_hash, backend, embedding, "
            "kind) VALUES (?,?,?,?,?)",
            (row["id"], digest, name, _pack(embed(text)), FRAGMENT))
        embedded += 1

    store.db.commit()
    return embedded, skipped


def index_chunks(store, force=False):
    """Embed every chunk whose text has changed. Returns (embedded, skipped).

    WHAT IS EMBEDDED IS THE HEADING PATH PLUS THE CHUNK, NOT THE CHUNK ALONE.
    That is R-66, and it is the whole of it: a clause embedded in isolation
    has lost the document it came from, and "21.3.2 Insulation" means nothing
    without "Section 21 Mechanical - 21.3 Ductwork" in front of it. The
    published version of this technique generates that context with a model
    call per chunk; here it is read off the document's own structure, so it
    costs nothing and cannot disagree with the hierarchy.

    Returns (0, 0) when nothing has been ingested. A scope with no documents
    is the normal case, not an error.
    """
    ensure_tables(store)
    try:
        rows = store.execute(
            "SELECT id, heading_path, text FROM chunks").fetchall()
    except sqlite3.OperationalError as exc:
        # ONLY "THE TABLE IS NOT THERE". The last twin of a shape a review has
        # now named four times in this repository: a locked, malformed or
        # schema-shifted store counted as "nothing ingested", index_chunks()
        # returned (0, 0), and the caller was told the meaning route had
        # nothing to index rather than that the store is broken. Found while
        # fixing the same line in heron_retrieve.find_documents, by grepping
        # for the shape instead of waiting for the next review to find it.
        if "no such table" not in str(exc):
            raise
        return 0, 0                      # no chunks table: nothing ingested

    name, embed = encoder()
    embedded = skipped = 0
    for row in rows:
        text = "%s\n%s" % (row["heading_path"] or "", row["text"] or "")
        digest = hashlib.blake2b(text.encode("utf-8"),
                                 digest_size=16).hexdigest()
        if not force:
            have = store.execute(
                "SELECT text_hash, backend FROM vectors WHERE id = ?",
                (row["id"],)).fetchone()
            if have and have["text_hash"] == digest and have["backend"] == name:
                skipped += 1
                continue
        store.execute(
            "INSERT OR REPLACE INTO vectors (id, text_hash, backend, embedding, "
            "kind) VALUES (?,?,?,?,?)",
            (row["id"], digest, name, _pack(embed(text)), CHUNK))
        embedded += 1

    # A chunk that no longer exists leaves a vector behind, and a vector with
    # no chunk is a hit that resolves to nothing. Forgetting them here is the
    # same pass, so the two can never drift.
    store.execute(
        "DELETE FROM vectors WHERE kind = ? AND id NOT IN "
        "(SELECT id FROM chunks)", (CHUNK,))
    store.db.commit()
    return embedded, skipped


def nearest(store, text, limit=5, kind=FRAGMENT):
    """The closest things to this text. [(id, similarity), ...], best first.

    Similarity, not distance: 1.0 is identical and 0.0 is unrelated, because
    every caller and every log line reads better that way round.

    `kind` says WHAT to compare against, and it defaults to fragments because
    every caller that existed before documents did meant fragments. Passing
    CHUNK searches the ingested documents instead.

    THE TWO ARE NEVER RETURNED TOGETHER, and that is a decision rather than an
    omission. A chunk ranked first among a hundred and a fragment ranked first
    among four hundred are not comparable numbers, and putting them in one
    list would invent a comparison the scores cannot support - which is the
    same reason Contest reports the discarded magnitudes rather than fusing
    them. Two corpora, two questions, two answers, each labelled.
    """
    ensure_tables(store)
    want = vector(text)
    rows = store.execute(
        "SELECT id, embedding FROM vectors "
        "WHERE COALESCE(kind, ?) = ?", (FRAGMENT, kind)).fetchall()

    scored = []
    for row in rows:
        got = _unpack(row["embedding"])
        if len(got) != len(want):
            # A vector of a different DIMENSION. Not comparable, and quietly
            # comparing it would produce a confident wrong number.
            #
            # THIS TESTS THE LENGTH ONLY - NOT THE BACKEND. The row's
            # `backend` stamp is not read here, so a row another encoder wrote
            # at the same dimension IS compared (FRAGMENT-ISSUES row 5b-107,
            # measured: a model-stamped row of 256 floats scored against a
            # lexical query). `index()` re-embeds a row whose stamp differs on
            # every open, which closes most of that window. A mismatch that
            # slips through at the same length IS COMPARED, below - the
            # owner's decision of 2026-11-21 ("keep comparing as now"), asked
            # on issue #957 under D-52.
            continue
        scored.append((row["id"], sum(a * b for a, b in zip(want, got))))

    scored.sort(key=lambda pair: -pair[1])
    return scored[:limit]


def main(argv):
    # Rows 5b-2041 to 5b-2044: print safely on a cp1252 console.
    import heron_console
    heron_console.safe_streams()

    store = SCOPE.open_scope(SCOPE.GLOBAL)
    try:
        name, why = backend()
        did, skipped = index(store)
        print("Backend: %s - %s" % (name, why))
        print("Indexed: %d embedded, %d unchanged" % (did, skipped))
        if not argv:
            return 0
        text = " ".join(argv)
        print()
        print("Nearest to %r" % text)
        for fid, score in nearest(store, text):
            print("  %6.3f  %s" % (score, fid))
        return 0
    finally:
        store.close()


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
