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


def _load_model():
    """Whatever trained encoder this machine has, or None.

    Tried in order of how little they cost to install. Each is optional by
    construction: an ImportError here is a normal condition, not a fault, and
    it must never stop Heron starting.
    """
    if _MODEL_CACHE:
        return _MODEL_CACHE[0]

    name = os.environ.get("HERON_EMBED_MODEL")

    try:
        from model2vec import StaticModel
        model = StaticModel.from_pretrained(name or "minishlab/potion-base-8M")
        _MODEL_CACHE.append(lambda t: list(model.encode([t])[0]))
        return _MODEL_CACHE[0]
    except Exception:
        pass

    try:
        from sentence_transformers import SentenceTransformer
        model = SentenceTransformer(name or "all-MiniLM-L6-v2")
        _MODEL_CACHE.append(lambda t: list(model.encode(t)))
        return _MODEL_CACHE[0]
    except Exception:
        pass

    _MODEL_CACHE.append(None)
    return None


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


def ensure_tables(store):
    store.db.executescript("""
        CREATE TABLE IF NOT EXISTS vectors (
            id        TEXT PRIMARY KEY,
            text_hash TEXT NOT NULL,
            backend   TEXT NOT NULL,
            embedding BLOB NOT NULL
        );
    """)
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
    name, _why = backend()
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
            "INSERT OR REPLACE INTO vectors (id, text_hash, backend, embedding) "
            "VALUES (?,?,?,?)", (row["id"], digest, name, _pack(vector(text))))
        embedded += 1

    store.db.commit()
    return embedded, skipped


def nearest(store, text, limit=5):
    """The closest fragments to this text. [(id, similarity), ...], best first.

    Similarity, not distance: 1.0 is identical and 0.0 is unrelated, because
    every caller and every log line reads better that way round.
    """
    ensure_tables(store)
    want = vector(text)
    rows = store.execute("SELECT id, embedding FROM vectors").fetchall()

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
