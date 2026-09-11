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

TWO BACKENDS, AND THE DIFFERENCE IS NOT A DETAIL
------------------------------------------------
  lexical   BUILT IN. Hashed character n-grams. No download, no dependency, no
            network - it works the second Heron is installed.

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

  model     A TRAINED sentence model, used when one is present. This is the one
            that actually understands that a grille and a diffuser are cousins.

WHY BOTH, RATHER THAN JUST THE GOOD ONE. Heron installs per-user with NO
ADMINISTRATOR RIGHTS (D-01, proven end to end in Phase 0), and a trained model
means a download that a locked-down corporate machine may refuse. A Heron that
cannot search until somebody wins an argument with IT is a Heron that does not
get used. So the weak backend is always there and always works, the good one is
used when it can be, and `backend()` says out loud which one answered.

**Measured 2026-08-28: the trained backend could not be tested in the container
this was written in** - huggingface.co is refused by that network's policy
(`connect_rejected`), so no weights could be fetched. That is a fact about ONE
CONTAINER and it is written here with its environment attached, because the
same sentence written without one has already cost this project three sessions.
`A7` in NEEDS-CHECKING.md is the run on a machine that can reach a model.
"""

import io
import os
import threading
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
MODEL = "model"


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


def model_vector(text, dims=DIMS):
    """A trained model's embedding, when one is installed. None when not."""
    encoder = _load_model()
    if encoder is None:
        return None
    got = encoder(text)
    length = math.sqrt(sum(x * x for x in got))
    return [x / length for x in got] if length else got


_MODEL_CACHE = []

# Set while a background warm-up is importing the trained encoder. While it is
# set, _load_model() returns None IMMEDIATELY rather than importing on the
# calling thread - see warm() for why that matters.
_WARMING = threading.Event()

# The warm-up thread itself, so _load_model() can tell it apart from a caller.
_WARM_THREAD = [None]


def warm():
    """Import the trained encoder on a BACKGROUND thread, and return at once.

    WHY THIS EXISTS, measured rather than reasoned about, 2026-09-06.

    `import model2vec` costs about 1.0 s in a fresh process. Called for the
    first time from INSIDE an MCP request handler - which runs on the asyncio
    event loop - it was still inside `create_module`, loading a native
    extension, 40 seconds later, and past 170 seconds in another run. The
    handler never replied, the host waited forever, and a real Claude Code tool
    call sat there for thirty minutes. The tool itself is fine: the same
    function answers in 6.2 s in-process and through the SDK's own dispatch.

    The failure needed BOTH halves and neither is wrong alone. Before
    model2vec was installed, this import raised ImportError instantly and Heron
    degraded to `lexical`, so the handler always answered. Installing it to
    prove the search understands meaning (`A7`) is what made `A8` hang.

    So: a heavy optional import never happens on a request thread. It happens
    here, once, at startup, off the loop - and until it finishes, every caller
    gets `lexical`, which is exactly the degradation this module already
    describes and reports on its first line. A slower answer that arrives beats
    a better one that does not.
    """
    if _MODEL_CACHE or _WARMING.is_set():
        return
    _WARMING.set()

    def run():
        try:
            _load_model()
        finally:
            _WARMING.clear()

    t = threading.Thread(target=run, name="heron-embed-warm", daemon=True)
    _WARM_THREAD[0] = t
    t.start()


def _load_model():
    """Whatever trained encoder this machine has, or None.

    Tried in order of how little they cost to install. Each is optional by
    construction: an ImportError here is a normal condition, not a fault, and
    it must never stop Heron starting.
    """
    if _MODEL_CACHE:
        return _MODEL_CACHE[0]

    if _WARMING.is_set() and threading.current_thread() is not _WARM_THREAD[0]:
        # A warm-up is already importing this on its own thread. Do NOT import
        # it here as well: this call may be on an event loop, and that is the
        # 30-minute hang warm() exists to prevent. Answer lexically for now.
        #
        # The thread check is not decoration. Without it the WARM-UP THREAD
        # hits this guard itself, returns None, clears the flag, and loads
        # nothing at all - after which the next request imports on the event
        # loop exactly as before. That was this fix's first version, and the
        # stack dump that caught it looked identical to the stack it was
        # meant to remove.
        return None

    name = os.environ.get("HERON_EMBED_MODEL")

    try:
        from model2vec import StaticModel
        which = name or "minishlab/potion-base-8M"
        model = StaticModel.from_pretrained(which)
        _WHICH_MODEL[0] = "model2vec:%s" % which
        _MODEL_CACHE.append(lambda t: list(model.encode([t])[0]))
        return _MODEL_CACHE[0]
    except Exception:
        pass

    try:
        from sentence_transformers import SentenceTransformer
        which = name or "all-MiniLM-L6-v2"
        model = SentenceTransformer(which)
        _WHICH_MODEL[0] = "sentence-transformers:%s" % which
        _MODEL_CACHE.append(lambda t: list(model.encode(t)))
        return _MODEL_CACHE[0]
    except Exception:
        pass

    _MODEL_CACHE.append(None)
    return None


# WHICH trained model, not just THAT one is trained - and the difference is a
# corrupted index. A vector was stamped `backend = "model"`, so changing
# HERON_EMBED_MODEL, or installing model2vec beside sentence-transformers,
# produced a DIFFERENT encoder wearing the same label. Every unchanged chunk
# then satisfied the cache check and kept vectors from the old model: at a
# different dimension nearest() silently discards them all, and at the same
# dimension it computes meaningless cross-model dot products. Either way the
# semantic route quietly stops working and nothing says so. Found by a review
# 2026-09-11.
_WHICH_MODEL = [None]


def stamp():
    """What to record beside a vector so a changed encoder invalidates it.

    The backend name for `lexical`, which is one built-in implementation and
    cannot change under a caller. The backend name AND the model for `model`,
    which can.
    """
    name, _why = backend()
    if name == MODEL and _WHICH_MODEL[0]:
        return "%s:%s" % (name, _WHICH_MODEL[0])
    return name


def backend():
    """Which backend is in use, and what that means. Reported, never assumed."""
    if _load_model() is not None:
        return MODEL, "a trained model - this one understands meaning"
    return LEXICAL, ("built-in character n-grams - tolerant of spelling and "
                     "word order, but NOT meaning")


def vector(text):
    got = model_vector(text)
    return got if got is not None else lexical_vector(text)


# ---------------------------------------------------------------------------
# Storing vectors, in the scope's own file
# ---------------------------------------------------------------------------
#
# D-23 names sqlite-vec, and it is used when it loads. It does not always load:
# some Python builds ship with extension loading compiled out, and that is
# exactly the locked-down machine Heron's install promise is about. So there is
# a plain fallback that computes the same cosine in Python over the same stored
# bytes - identical answers, slower, and it needs nothing installed at all.
#
# Both keep the vectors IN THE SCOPE'S OWN FILE, which is the part that matters:
# one file per scope stays true either way (Golden Rule 5), and a vector never
# outlives the scope it belongs to.

def _pack(vec):
    return struct.pack("%df" % len(vec), *vec)


def _unpack(blob):
    return list(struct.unpack("%df" % (len(blob) // 4), blob))


def _try_vec_extension(db):
    try:
        import sqlite_vec
    except ImportError:
        return False
    try:
        db.enable_load_extension(True)
        sqlite_vec.load(db)
        db.enable_load_extension(False)
        return True
    except (AttributeError, sqlite3.OperationalError):
        # Extension loading compiled out, or refused. Normal on a hardened
        # build, and the fallback below gives the same answers.
        return False


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


def index(store, force=False):
    """Embed every fragment whose text has CHANGED. Returns (embedded, skipped).

    Content-hashed, per docs/05 s7: a git checkout moves every file's mtime and
    changes none of their content, so anything keyed on time re-embeds the whole
    library for nothing. Keyed on content, that costs zero.
    """
    ensure_tables(store)
    name = stamp()
    on_disk, _ = FRAG.load_all()

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
            (row["id"], digest, name, _pack(vector(text)), FRAGMENT))
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

    name = stamp()
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
            (row["id"], digest, name, _pack(vector(text)), CHUNK))
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
            # A vector from a different backend or dimension. Not comparable,
            # and quietly comparing it would produce a confident wrong number.
            continue
        scored.append((row["id"], sum(a * b for a, b in zip(want, got))))

    scored.sort(key=lambda pair: -pair[1])
    return scored[:limit]


def main(argv):
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
