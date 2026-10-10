# Heron-Agent:  HERON-RAG-LIB-001
# Heron-Step:   8
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
One knowledge store per scope, as one file each - Golden Rule 5 made physical.

    python brain/heron_scope.py              what exists, and where
    python brain/heron_scope.py --rebuild    re-index every scope from disk

Step 8 of docs/27-build-order.md.

WHY FILES AND NOT TABLES
------------------------
In a server, a scope is a column and separation is a WHERE clause somebody can
forget. As one file per scope it is the filesystem, and forgetting is not
available. D-23 chose this on the install constraint - Heron installs per-user
with no administrator rights, so a store needing a service breaks on exactly
the locked-down machines Heron is for - and Golden Rule 5 asks for the
separation to be PHYSICAL.

This is a commercial requirement before it is a technical one (docs/10 s2):
Project A and Project B routinely belong to different clients, often under NDA.
Leakage between them is a contractual breach, not an inconvenience.

THE ONE THING THIS FILE IS FOR
------------------------------
**A cross-scope query must be impossible to WRITE, not merely absent.** Two
things make that true rather than aspirational:

  1. open() takes ONE scope. There is no parameter anywhere that accepts two,
     so there is no call to write.
  2. ATTACH is refused. SQLite's ATTACH DATABASE is the one mechanism that
     could reach a second file through a connection that legitimately holds
     one, so every statement is checked for it and refused by name. Without
     this, rule 1 is a convention with a loophole.

THE INDEX IS DERIVED, NEVER AUTHORITATIVE (docs/05 s7, Golden Rule 11)
----------------------------------------------------------------------
Fragments live as files. Everything in these databases is a copy, put there to
be searched. **Deleting every .db must be a safe recovery action** that costs a
rebuild and loses nothing - `--rebuild` is that action, and the tests prove it
by deleting the lot and rebuilding.
"""

import io
import os
import re
import sys
import json
import hashlib
import sqlite3

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_fragment as FRAG                                 # noqa: E402

# docs/10 section 1. PROJECT is the only one that is per-something; the rest are
# one apiece.
GLOBAL = "global"
COMPANY = "company"
PROJECT = "project"
USER = "user"
TEMPORARY = "temporary"
EXPERIMENTAL = "experimental"

SCOPES = (GLOBAL, COMPANY, PROJECT, USER, TEMPORARY, EXPERIMENTAL)

SCOPE_MEANING = {
    GLOBAL:       "general Heron knowledge, shared with everyone",
    COMPANY:      "company standards and approved knowledge",
    PROJECT:      "one project only - never shared sideways",
    USER:         "working preferences and personal patterns, shared with nobody",
    TEMPORARY:    "short-term task context, discarded",
    EXPERIMENTAL: "unproven knowledge, quarantined",
}

SCHEMA_VERSION = 1

# Any statement that could reach a second database file. Checked as words, so
# a fragment whose text happens to contain "detach" in prose is not caught.
FORBIDDEN = re.compile(r"\b(attach|detach)\b", re.IGNORECASE)


class CrossScopeRefused(Exception):
    """Raised when a statement tries to reach beyond its own scope file."""


# The columns of a `fragments` row, in the order `row_of` fills them. ONE list,
# because `put_fragment` writes a row and `refresh` compares one, and two
# spellings of the same row are how a field gets written and never compared.
ROW_FIELDS = ("id", "capability", "semantic_identity", "kind", "status",
              "domain", "risk", "folder", "revit")


def row_of(frag):
    """The row this card puts in the store, as a tuple in ROW_FIELDS order."""
    return (frag.id,
            frag.data.get("capability", ""),
            frag.semantic_identity or "",
            frag.kind or "",
            frag.status or "",
            frag.data.get("domain", ""),
            frag.data.get("risk", ""),
            FRAG.repo_relative(frag.folder, getattr(frag, "root", None)),
            ",".join(frag.supported))


def knowledge_dir():
    """%APPDATA%\\Heron\\knowledge.

    DATA rather than DERIVED, and the distinction is HeronPaths' own: this is
    the user's knowledge, it roams with them, and a cache wipe must never take
    it. HERON_KNOWLEDGE overrides it - which the tests use, and which is also
    the only way to run this on a machine with no %APPDATA% at all, where a
    good deal of Heron gets written.
    """
    override = os.environ.get("HERON_KNOWLEDGE")
    if override:
        return override
    appdata = os.environ.get("APPDATA")
    if not appdata:
        return None
    return os.path.join(appdata, "Heron", "knowledge")


def scope_path(scope, project_key=None):
    """The one file this scope lives in."""
    if scope not in SCOPES:
        raise ValueError(
            "%r is not a knowledge scope. Known: %s" % (scope, ", ".join(SCOPES)))

    base = knowledge_dir()
    if base is None:
        raise ValueError(
            "No %APPDATA% and no HERON_KNOWLEDGE, so there is nowhere to keep "
            "knowledge. Set HERON_KNOWLEDGE to a folder.")

    if scope != PROJECT:
        return os.path.join(base, "%s.db" % scope)

    if not project_key:
        # Never a default. A project store with no project named is how one
        # client's knowledge lands in another's file, and D-33 says Heron does
        # not assume an input - it asks, once.
        raise ValueError(
            "The project scope needs a project key and was given none. Heron "
            "does not guess which project this is: guessing wrong writes one "
            "client's knowledge into another's file, which is a contractual "
            "breach rather than a bug (docs/10 section 2). Ask, then pass it.")

    return os.path.join(base, "projects", "%s.db" % _safe_key(project_key))


def _safe_key(project_key):
    """A project key, reduced to something that can be a filename.

    The key itself is the model's own id, Document.CreationGUID, which the
    add-in sends as `creationGuid` and DocumentPin.project_key returns (D-113).
    It survives save, rename and move. It is NOT the file name: naming a store
    after a file means renaming the file loses the knowledge, and the
    stale-name trap in docs/25 section 2a is the same mistake at a different
    layer.

    IT WAS THE PROJECT INFORMATION UniqueId UNTIL 2026-10-06, and this said
    that id is created with the document. It is created with the TEMPLATE, so
    every model made from one template shared one store (FRAGMENT-ISSUES
    5b-324). What was kept under it is asked about, never used:
    heron_earlier.
    """
    return re.sub(r"[^A-Za-z0-9._-]", "-", str(project_key))[:120]


# ---------------------------------------------------------------------------
# The store
# ---------------------------------------------------------------------------

class Store(object):
    """One scope. One file. One connection.

    There is deliberately no way to hold two of these open against one query.
    Reaching a second scope means opening a second Store and doing the joining
    in Python, where it is visible, deliberate and loggable - which is what
    docs/10 section 2 rule 4 asks for when it says a cross-project search is an
    explicit, logged opt-in whose results say which project each answer is from.
    """

    def __init__(self, scope, project_key=None):
        self.scope = scope
        self.project_key = project_key
        self.path = scope_path(scope, project_key)

        folder = os.path.dirname(self.path)
        if folder and not os.path.isdir(folder):
            os.makedirs(folder)

        self.db = sqlite3.connect(self.path)
        self.db.row_factory = sqlite3.Row
        self._create()

    # -- the guard ----------------------------------------------------------

    def execute(self, sql, args=()):
        """Every statement goes through here, and none of them may ATTACH.

        SQLite will happily open a second database file through a connection
        that legitimately holds one. That is the single loophole in "one file
        per scope", so it is closed by name rather than by hoping nobody
        writes it.
        """
        if FORBIDDEN.search(sql):
            raise CrossScopeRefused(
                "This statement would reach outside the %s scope, and no "
                "statement may: ATTACH is how one connection reads a second "
                "database file, which is exactly the cross-scope query Golden "
                "Rule 5 exists to make impossible. Open a second Store instead, "
                "where the crossing is visible and can be logged."
                % self.scope)
        return self.db.execute(sql, args)

    def _create(self):
        # The store carries a COPY of what the files say, so it can be searched.
        # It is never the only copy of anything - see this module's header.
        self.db.executescript("""
            CREATE TABLE IF NOT EXISTS meta (
                key   TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS fragments (
                id                TEXT PRIMARY KEY,
                capability        TEXT NOT NULL,
                semantic_identity TEXT NOT NULL,
                kind              TEXT NOT NULL,
                status            TEXT NOT NULL,
                domain            TEXT NOT NULL,
                risk              TEXT NOT NULL,
                folder            TEXT NOT NULL,
                revit             TEXT NOT NULL
            );
        """)
        self.db.execute(
            "INSERT OR REPLACE INTO meta (key, value) VALUES (\'scope\', ?)",
            (self.scope,))
        self.db.execute(
            "INSERT OR REPLACE INTO meta (key, value) VALUES (\'schema\', ?)",
            (str(SCHEMA_VERSION),))
        if self.project_key:
            self.db.execute(
                "INSERT OR REPLACE INTO meta (key, value) VALUES (\'project\', ?)",
                (str(self.project_key),))
        self.db.commit()

    # -- contents -----------------------------------------------------------

    def put_fragment(self, frag):
        self.put_row(frag)
        self.db.commit()

    def put_row(self, frag):
        """put_fragment WITHOUT the commit, for a caller whose transaction
        commits the row together with what it records about the row -
        `refresh()`, where committing each on its own is a race (review of
        PR #353)."""
        self.execute(
            "INSERT OR REPLACE INTO fragments (%s) VALUES (?,?,?,?,?,?,?,?,?)"
            % ", ".join(ROW_FIELDS),
            row_of(frag))

    def fragments(self):
        return [dict(r) for r in self.execute(
            "SELECT * FROM fragments ORDER BY id").fetchall()]

    def count(self):
        return self.execute("SELECT COUNT(*) AS n FROM fragments").fetchone()["n"]

    def close(self):
        self.db.close()

    def __repr__(self):
        return "<Store %s %s>" % (self.scope, self.path)


def open_scope(scope, project_key=None):
    """The ONLY way in. One scope, never a list - there is no signature here
    that a cross-scope query could be written against."""
    return Store(scope, project_key)

# ---------------------------------------------------------------------------
# Resolving the scope - before retrieval, never by inference
# ---------------------------------------------------------------------------

def resolve(document=None, wanted=PROJECT):
    """Which scope, and which project. Returns (scope, project_key).

    docs/10 section 2 rule 2: the scope is resolved BEFORE retrieval, from the
    active Revit document, and is NEVER inferred by a model. So this takes
    facts and returns an answer or an error - it never picks a likely one.

    `document` is what the bridge reports about the open model, and the only
    field that matters here is its stable key. The document's NAME is
    deliberately not used: two models are routinely both called Project1 - it
    has already happened here - and naming a store after a file means renaming
    the file loses the knowledge.
    """
    if wanted not in SCOPES:
        raise ValueError("%r is not a knowledge scope" % wanted)

    if wanted != PROJECT:
        return wanted, None

    key = (document or {}).get("project_key")
    if not key:
        raise ValueError(
            "Project knowledge was asked for and no project is identified. "
            "Heron does not guess this and there is no default: the wrong "
            "guess writes one client's knowledge into another's store. Ask "
            "which model this is, once (D-33), then pass its key.")

    label = (document or {}).get("project_name")
    if label:
        remember_label(key, label)
    return PROJECT, key


# The label index. The KEY is identity; the label is only so a human opening
# the folder can tell which file is which. Exactly the split Step 7 made for
# fragments - an id that never changes, and a name that may.
def _labels_path():
    base = knowledge_dir()
    return os.path.join(base, "projects", "labels.json") if base else None


def remember_label(project_key, label):
    path = _labels_path()
    if not path:
        return
    folder = os.path.dirname(path)
    if not os.path.isdir(folder):
        os.makedirs(folder)
    known = read_labels()
    known[_safe_key(project_key)] = label
    io.open(path, "w", encoding="utf-8").write(
        json.dumps(known, indent=2, sort_keys=True))


def read_labels():
    path = _labels_path()
    if not path or not os.path.exists(path):
        return {}
    try:
        return json.loads(io.open(path, encoding="utf-8").read())
    except ValueError:
        # A corrupt label file loses nothing that matters - the keys are still
        # the filenames. Never let it stop Heron starting.
        return {}


# ---------------------------------------------------------------------------
# Rebuild - the proof that the index is derived
# ---------------------------------------------------------------------------

def rebuild(scope=GLOBAL, project_key=None):
    """Re-index every fragment on disk into one scope. Returns how many.

    Heron's own fragments are GLOBAL knowledge - they ship with it and belong
    to everyone. A project's own fragments would be rebuilt from that project's
    folder into its own store by the same function.
    """
    # THE CARDS ARE MARKED BEFORE THEY ARE READ, so a card saved while this
    # runs carries a newer mark than the one recorded and `refresh` looks at
    # it again, rather than trusting a row read from the older bytes.
    marks = cards_on_disk()
    raws = dict((name, _card(FRAG.FRAGMENTS_DIR, name)) for name in marks)
    found, problems = FRAG.load_all()
    store = open_scope(scope, project_key)
    try:
        store.execute("DELETE FROM fragments")
        rows_from = {}
        for frag in found.values():
            if not FRAG.validate(frag):
                store.put_fragment(frag)
                raw = raws.get(os.path.basename(frag.folder))
                if raw is not None:
                    rows_from[os.path.basename(frag.folder)] = _digest(raw)
        _record(store, _key(FRAG.FRAGMENTS_DIR), marks, rows_from)
        store.db.commit()
        return store.count(), problems
    finally:
        store.close()


# ---------------------------------------------------------------------------
# Refresh - a card changed on disk reaches the store without a rebuild
# ---------------------------------------------------------------------------
#
# FRAGMENT-ISSUES row 5b-249. The search reads a card's purpose and utterances
# from the FILE on every open, but its semantic identity, status, capability,
# domain and risk from the store's ROW - and only `rebuild()` wrote rows, which
# `heron_brain._Open` calls only for an empty store. So a merged change to a
# card's identity was searched under the OLD identity beside the NEW purpose,
# for as long as nobody rebuilt: on 2026-09-28 PR #351 renamed FRG-VIEW-110 and
# "color hvac zones in this plan" still answered CREATE_HVAC_ZONE after the
# merge, until one row was put back by hand.
#
# WHAT IT DOES: rewrites the row of a card whose fragment.yaml is not the one
# the row was made from, when the row differs. Two records keep that cheap
# enough to run on every lookup. Per card folder, the stat marks of the cards
# last looked at - a stat walk costs milliseconds, and a card whose mark has
# not moved is not even opened. Per store, a digest of the bytes each row was
# made from - so a reader with no marks of its own reads bytes rather than
# parsing every card (seconds) to find they already match.
#
# WHAT IT NEVER DOES, AND WHY. It never rewrites a row that was made from the
# bytes on disk now, and in a PRIVATE store it never ADDS a row and never
# REMOVES one - that is what `rebuild()` is for. Each of those is somebody
# else's write it would undo: a suite that plants a DRAFT row in a throwaway
# store, or a session proving an unmerged card with the one row it put into
# the shared store and will take out again.
#
# THE SHARED STORE GAINS AND LOSES WHAT MAIN GAINS AND LOSES - row 5b-406.
# Until 2026-10-10 a merged NEW card needed `--rebuild` by hand on every PC,
# and the owner's store sat at 480 rows beside 481 cards with LIST_MATERIALS
# unreachable. `_library_moves()` below says which cards and rows part
# company, by folder name; only the shared store does anything with it, and
# only from a main checkout on branch main.
#
# AND THE SHARED STORE FOLLOWS THE MAIN CHECKOUT - row 131's race, and the
# reason for `refreshes_from()` below.

CARDS_SEEN = "cards_seen:"      # + the card folder the marks describe
ROWS_FROM = "rows_from"

# BUMP THIS WHENEVER row_of() DERIVES A ROW DIFFERENTLY. The records above say
# "this row was made from these bytes", and without the version a code-only
# change to how a row is made would match every record the old code wrote and
# never reach a row - heron_search.INDEX_FORMAT's reason, one table over.
# Found by review on PR #353.
ROW_FORMAT = 1


def cards_on_disk(folder=None):
    """Each card's fragment.yaml as [mtime_ns, size], by folder name.

    A stat walk - no file is opened. Everything a row holds comes from
    fragment.yaml, so nothing else can change a row. A git checkout rewrites
    the files it changes and leaves the rest alone, which is exactly the set
    worth reading again.

    WHAT IT CANNOT SEE: a card replaced by different bytes of the same length
    with its old mtime kept - a copy that preserves timestamps, on top of a
    file of exactly the same size. git never does that; `--rebuild` is the
    answer when something else has. The alternative is reading every card on
    every lookup, and `heron_search.disk_status` makes the same trade.
    """
    folder = folder or FRAG.FRAGMENTS_DIR
    marks = {}
    if not os.path.isdir(folder):
        return marks
    for name in sorted(os.listdir(folder)):
        mark = _mark(folder, name)
        if mark is not None:
            marks[name] = mark
    return marks


def _mark(folder, name):
    """One card's [mtime_ns, size], or None when it has no fragment.yaml."""
    try:
        stat = os.stat(os.path.join(folder, name, "fragment.yaml"))
    except OSError:
        return None
    return [stat.st_mtime_ns, stat.st_size]


def _same_folder(a, b):
    """The same folder, however it is spelt - a junction or a symbolic link
    included, which is how a HERON_KNOWLEDGE naming the shared folder by
    another path would otherwise pass for private. Found by review on #353."""
    return os.path.normcase(os.path.realpath(a)) == \
        os.path.normcase(os.path.realpath(b))


def _key(folder):
    """The marks' key for one card folder, written one way on every OS."""
    return CARDS_SEEN + os.path.normcase(os.path.abspath(folder)).replace(
        os.sep, "/")


def _card(folder, name):
    """A card's fragment.yaml as bytes, or None when there is none to read."""
    try:
        with open(os.path.join(folder, name, "fragment.yaml"), "rb") as handle:
            return handle.read()
    except (IOError, OSError):
        return None


def _digest(raw):
    digest = hashlib.blake2b(digest_size=12)
    digest.update(("rows/%d/" % ROW_FORMAT).encode("utf-8"))
    digest.update(raw)
    return digest.hexdigest()


def _meta(store, key):
    """What `_record` kept under `key`, or None when there is none to trust.

    None - not {} - for a store written before this existed, or by another
    ROW_FORMAT: an absent record is not a clean one (D-52), so every card is
    looked at once.
    """
    row = store.execute(
        "SELECT value FROM meta WHERE key = ?", (key,)).fetchone()
    if row is None:
        return None
    try:
        value = json.loads(row["value"])
    except ValueError:
        return None
    if not isinstance(value, dict) or value.get("format") != ROW_FORMAT:
        return None
    kept = value.get("kept")
    return kept if isinstance(kept, dict) else None


def _record(store, key, marks, rows_from):
    """The marks last looked at, and which bytes each row came from.

    NOT COMMITTED HERE. The caller commits, so these records land in the same
    transaction as the rows they describe, or not at all.
    """
    for name, kept in ((key, marks), (ROWS_FROM, rows_from)):
        store.execute(
            "INSERT OR REPLACE INTO meta (key, value) VALUES (?, ?)",
            (name, json.dumps({"format": ROW_FORMAT, "kept": kept},
                              sort_keys=True)))
    # A worktree's marks outlive the worktree. They go when its folder does,
    # or the store would keep one record for every chat there ever was.
    for row in store.execute("SELECT key FROM meta WHERE key LIKE ?",
                             (CARDS_SEEN + "%",)).fetchall():
        if not os.path.isdir(row["key"][len(CARDS_SEEN):]):
            store.execute("DELETE FROM meta WHERE key = ?", (row["key"],))


def shared_dir():
    """The one knowledge folder every checkout on this machine reads, or None.

    Heron/knowledge under %APPDATA% - `knowledge_dir()` with HERON_KNOWLEDGE
    left out, because a folder somebody pointed HERON_KNOWLEDGE at is theirs.
    """
    appdata = os.environ.get("APPDATA")
    return os.path.join(appdata, "Heron", "knowledge") if appdata else None


def main_checkout(root=None):
    """The checkout a linked worktree was added from, or None.

    Git writes `.git` as a FILE in a linked worktree - one line naming its
    folder under the main repository's .git - and as a FOLDER in the checkout
    it was cloned into. That folder's `commondir` leads back to the main .git,
    whose parent is the main checkout. `root` itself when it is not a linked
    worktree: the main checkout, or an installed Heron with no .git at all.
    None when the pointers lead nowhere - a moved repository, a bare one.
    """
    root = root or ROOT
    pointer = os.path.join(root, ".git")
    if not os.path.isfile(pointer):
        return root
    try:
        line = io.open(pointer, encoding="utf-8").read().strip()
        if not line.startswith("gitdir:"):
            return None
        gitdir = os.path.join(root, line.split(":", 1)[1].strip())
        common = io.open(os.path.join(gitdir, "commondir"),
                         encoding="utf-8").read().strip()
    except (IOError, OSError):
        return None
    common = os.path.normpath(os.path.join(gitdir, common))
    if os.path.basename(common) != ".git":
        return None
    main = os.path.dirname(common)
    return main if os.path.isdir(os.path.join(main, "brain", "fragments")) \
        else None


def refreshes_from(root=None):
    """Where the rows of the store `knowledge_dir()` names are refreshed from.

    Returns (cards, checkout), or None for "nowhere":
      (this checkout's cards, None)   a PRIVATE store - HERON_KNOWLEDGE points
                                      somewhere of its own: a suite, a CI job,
                                      a session measuring on a scratch store.
      (main's cards, main)            the SHARED store, asked from anywhere.
      None                            the shared store, from a worktree whose
                                      main checkout cannot be found.

    FRAGMENT-ISSUES row 131: the shared store is ONE file every checkout on
    the machine reads, and a chat started in a worktree runs its own copy of
    the server - five were running on the owner's PC when this was written. A
    worktree's cards are that session's UNMERGED work, or a library older than
    main, and refreshing from them would push either into every other chat's
    answers. So the shared store follows the MAIN checkout, which a merge
    fast-forwards, and every chat reads the main checkout's cards to do it -
    a chat opened before a merge brings the row level as surely as one opened
    after. (The first version compared the asking checkout's own card with
    main's and refreshed only when they matched, which left exactly those
    older chats unable to; found by review on PR #353.)

    The main checkout's cards are read AS the main checkout reads them -
    `FRAG.load(folder, root=main)` - because a PROVEN card's proof
    fingerprint is taken over paths relative to the checkout, and read from
    here with this checkout's paths it would fail its own validation.
    """
    base = knowledge_dir()
    shared = shared_dir()
    if not base or not shared or not _same_folder(base, shared):
        return FRAG.FRAGMENTS_DIR, None
    main = main_checkout(root)
    if main is None:
        return None
    if _same_folder(main, FRAG.ROOT):
        return FRAG.FRAGMENTS_DIR, None
    return os.path.join(main, "brain", "fragments"), main


def on_main_branch(root=None):
    """True when `root` is a checkout of branch main, or has no git at all.

    Read from .git/HEAD rather than by running git - this is asked on every
    lookup into the shared store. A linked worktree has a .git FILE, and is
    never the main checkout. heron_brain._on_main_branch asks the same of the
    store warm-up.
    """
    root = root or ROOT
    dot_git = os.path.join(root, ".git")
    if not os.path.exists(dot_git):
        return True
    if not os.path.isdir(dot_git):
        return False
    try:
        with io.open(os.path.join(dot_git, "HEAD"), encoding="utf-8") as handle:
            head = handle.read().strip()
    except (IOError, OSError):
        return False
    return head == "ref: refs/heads/main"


def _folder_name(folder):
    """A row's card folder, as the name its card has on disk. The row writes
    it against its checkout's root, with that system's separator - so only
    the last part is compared."""
    return (folder or "").replace("\\", "/").rstrip("/").rsplit("/", 1)[-1]


def _library_moves(store, cards, checkout, marks):
    """(missing, doomed): what `refresh()` adds to and takes from the store.

    `missing` - the card folders in `marks` that no row names. `doomed` -
    {folder name: [ids]} for the rows whose card folder has gone from `cards`
    altogether. Both empty unless every one of these holds, because each is a
    write somebody else would lose:

      THE SHARED STORE. A private one is a suite's, a CI job's or a
      measurement's, built with exactly the rows it wants.
      A MAIN CHECKOUT ON BRANCH MAIN, or an installed Heron. On a feature
      branch the main folder's cards are unmerged (row 5b-233), and a
      worktree's never reach here at all (`refreshes_from`, row 131).
      A LIBRARY THAT HOLDS A CARD. One that arrived empty is not one that
      removed everything - _Open's own rule, an empty library is not an
      empty answer - and would otherwise take every row with it.

    And a row is doomed only when it was MADE FROM MAIN'S CARD - the folder
    is in ROWS_FROM - and the folder itself has gone, not merely its
    fragment.yaml, which git writes anew when it changes. A row a session
    planted for its unmerged card was never made from main, and is left.

    By FOLDER NAME, from the stat walk `refresh()` already took and one read
    of the rows, so a lookup between two merges parses nothing. What a folder
    name cannot see: a card whose id changed inside its own folder. Its folder
    still has a row, so nothing is added, and `--rebuild` is still the answer.
    """
    if not marks:
        return set(), {}
    base, shared = knowledge_dir(), shared_dir()
    if not base or not shared or not _same_folder(base, shared):
        return set(), {}
    if not on_main_branch(checkout or FRAG.ROOT):
        return set(), {}
    by_folder = {}
    for row in store.execute("SELECT id, folder FROM fragments").fetchall():
        by_folder.setdefault(_folder_name(row["folder"]), []).append(row["id"])
    missing = set(name for name in marks if name not in by_folder)
    doomed = {}
    extra = [name for name in by_folder if name not in marks]
    if extra:
        made = _meta(store, ROWS_FROM) or {}
        for name in extra:
            if name in made and not os.path.isdir(os.path.join(cards, name)):
                doomed[name] = by_folder[name]
    return missing, doomed


def refresh(store, root=None):
    """Rewrite the rows whose card changed on disk - and in the shared store,
    add the cards main gained and drop the rows of cards it lost (row 5b-406,
    `_library_moves`). Returns the ids whose row it wrote or removed.

    Nothing is opened when no card's mark moved and no card is missing a row,
    which is every lookup between two merges. A card that loads and does not
    validate keeps the row it had: rebuild() would drop it, and a lookup that
    loses a working capability over a card that fails a check is worse than
    one answering from the version before. A NEW one that does not validate
    is not added - and in the shared store it is parsed again on each lookup
    until it does, because the file that finishes it may not be fragment.yaml.

    A CARD IS MARKED AS LOOKED AT ONLY WHEN ITS BYTES HAVE BEEN DEALT WITH.
    One that could not be READ or PARSED - a Windows lock, a file caught
    half-saved - is tried again on the next lookup rather than trusted to a
    mark that never met the bytes (D-48; review on PR #353). So is a card the
    store holds that parses and does not VALIDATE: a lookup racing a checkout
    that has written fragment.yaml but not yet tests/cases.yaml or the impl
    would otherwise never look again, because the files that finish the card
    do not move fragment.yaml's mark (second review on PR #353). A card the
    store does NOT hold is marked either way: in a private store it is never
    added, and in the shared store a card with no row is looked at whatever
    its mark says - which is also how a card an older lookup marked, before
    adding existed, still gets in.

    ONE SHORT TRANSACTION, AND EVERYTHING SLOW OUTSIDE IT. The cards are read
    and parsed holding no lock. Then, under BEGIN IMMEDIATE, the records and
    the rows are read again, each card is stat'ed again and left unmarked if
    its mark moved since it was read, the rows are written, and this reader's
    marks and digests are MERGED into the records as they stand now - and all
    of it commits at once. Before this, two chats refreshing the shared store
    while the main checkout was being fast-forwarded could interleave: one
    wrote a row from the card BEFORE the fast-forward after the other had
    recorded the marks and digest of the card AFTER it, and that row stayed
    stale until the card changed again (second review on PR #353). Every chat
    on the PC shares that store, so the transaction holds only stat calls, a
    read of the rows and the rows that changed.
    """
    source = refreshes_from(root)
    if source is None:
        return []
    cards, checkout = source
    key = _key(cards)

    marks = cards_on_disk(cards)
    seen = _meta(store, key)
    missing, doomed = _library_moves(store, cards, checkout, marks)
    if seen == marks and not missing and not doomed:
        return []
    rows_from = _meta(store, ROWS_FROM) or {}
    # A row about to be dropped no longer holds its id: a card main moved to
    # a new folder is added under it in the same transaction.
    held = set(row["id"] for row in store.execute(
        "SELECT id FROM fragments").fetchall()) - \
        set(fid for ids in doomed.values() for fid in ids)

    # READ AND PARSE, HOLDING NO LOCK. A card worth recording is kept as its
    # name, the mark taken BEFORE its bytes were read, their digest, the card
    # (None when the row was made from these bytes) and whether it validates.
    looked = []
    for name, mark in sorted(marks.items()):
        moved = seen is None or seen.get(name) != mark
        if not moved and name not in missing:
            continue
        raw = _card(cards, name)
        if raw is None:
            continue                  # not marked: tried again next lookup
        digest = _digest(raw)
        if name not in missing and rows_from.get(name) == digest:
            looked.append((name, mark, digest, None, True))
            continue
        try:
            frag = FRAG.load(os.path.join(cards, name), root=checkout)
            valid = not FRAG.validate(frag)
        except ValueError:
            continue
        except Exception:                      # noqa: BLE001 - deliberate
            # D-48, as load_all() has it: one card that fails in a way
            # load() did not turn into a ValueError costs that card, never
            # the lookup this runs inside.
            continue
        adding = name in missing and valid and frag.id and frag.id not in held
        if not moved and not adding:
            continue                  # marked already, and nothing to add
        if valid or not (frag.id and frag.id in held):
            looked.append((name, mark, digest, frag, valid))

    gone = seen is not None and any(name not in marks for name in seen)
    if not looked and not gone and not doomed:
        return []

    rewritten = []
    store.execute("BEGIN IMMEDIATE")
    done = False
    try:
        kept = dict((name, mark) for name, mark in
                    (_meta(store, key) or {}).items() if name in marks)
        now_from = _meta(store, ROWS_FROM) or {}
        for name, ids in sorted(doomed.items()):
            # Asked again under the lock: still made from main, still gone.
            if name not in now_from or os.path.isdir(os.path.join(cards, name)):
                continue
            for fid in ids:
                row = store.execute("SELECT folder FROM fragments WHERE id = ?",
                                    (fid,)).fetchone()
                if row is not None and _folder_name(row["folder"]) == name:
                    store.execute("DELETE FROM fragments WHERE id = ?", (fid,))
                    rewritten.append(fid)
            now_from.pop(name, None)
        now_held = dict((row["id"], tuple(row[f] for f in ROW_FIELDS))
                        for row in store.fragments())
        for name, mark, digest, frag, valid in looked:
            if _mark(cards, name) != mark:
                continue              # saved again since it was read
            if frag is None:
                if now_from.get(name) == digest:
                    kept[name] = mark
                continue
            row = now_held.get(frag.id) if frag.id else None
            if row is None:
                if valid and name in missing:
                    store.put_row(frag)           # main gained it: row 5b-406
                    now_held[frag.id] = row_of(frag)
                    now_from[name] = digest
                    rewritten.append(frag.id)
                kept[name] = mark
                continue
            if not valid:
                continue
            if row != row_of(frag):
                store.put_row(frag)
                rewritten.append(frag.id)
            now_from[name] = digest
            kept[name] = mark
        _record(store, key, kept, now_from)
        store.db.commit()
        done = True
    finally:
        if not done:
            store.db.rollback()
    return rewritten


def existing():
    """Every scope file that is actually on disk, with its size."""
    base = knowledge_dir()
    if not base or not os.path.isdir(base):
        return []

    out = []
    for scope in SCOPES:
        if scope == PROJECT:
            continue
        p = os.path.join(base, "%s.db" % scope)
        if os.path.exists(p):
            out.append((scope, None, p, os.path.getsize(p)))

    projects = os.path.join(base, "projects")
    if os.path.isdir(projects):
        labels = read_labels()
        for name in sorted(os.listdir(projects)):
            if not name.endswith(".db"):
                continue
            key = name[:-3]
            out.append((PROJECT, labels.get(key, key),
                        os.path.join(projects, name),
                        os.path.getsize(os.path.join(projects, name))))
    return out


def main(argv):
    base = knowledge_dir()
    if not base:
        print("No %APPDATA% and no HERON_KNOWLEDGE set, so there is nowhere")
        print("to keep knowledge. Set HERON_KNOWLEDGE to a folder.")
        return 1

    if argv[:1] == ["--rebuild"]:
        count, problems = rebuild()
        for line in problems:
            print("  skipped: %s" % line)

        # THE SEARCH INDEX AND THE EMBEDDINGS ARE REBUILT HERE TOO, AND WERE NOT
        # UNTIL 2026-09-01. `rebuild()` above refills the fragments table and
        # nothing else, so this command left the identity table and the vectors
        # holding whatever the last run put there. Measured: after adding an
        # utterance and running ONLY this command, the new sentence resolved to
        # a DIFFERENT fragment - the stale index answered, confidently, by the
        # hybrid route, while the identity route that should have caught it
        # exactly had never heard of it.
        #
        # It went unseen because every tool that reads the index calls
        # SEARCH.index itself first - check-routing does, the brain does - so
        # the only person who could hit it was somebody rebuilding BY HAND and
        # then asking a question, which is exactly what this command is for and
        # exactly what its own help text told them to do.
        #
        # `rebuild()` is left cheap on purpose: it is a library function and a
        # caller that only wants the metadata should not pay for embeddings.
        # The command promises a re-index, so the command does one.
        indexed = None
        try:
            import heron_search as SEARCH
            import heron_embed as EMBED

            store = open_scope(GLOBAL)
            try:
                # `force=True` BECAUSE THIS IS THE EXPLICIT RECOVERY PATH.
                # `index()` skips when the library digest is unchanged, which
                # is right for the read path and wrong here: somebody typing
                # --rebuild has usually just deleted or damaged a store, and a
                # skip would hand them back the broken one. Found by review on
                # PR #198, which named this command by name.
                indexed, _ = SEARCH.index(store, force=True)
                EMBED.index(store)
                store.db.commit()
            finally:
                store.close()
        except Exception as exc:                        # noqa: BLE001
            print("  the fragments were rebuilt, but the search index was NOT:")
            print("  %s: %s" % (type(exc).__name__, exc))
            print("  Queries will answer from the OLD index until that is fixed.")

        print("Re-indexed %d fragment(s) into the global scope." % count)
        if indexed is not None:
            print("Searchable text and embeddings rebuilt for %d." % indexed)
        print()
        print("That is the whole recovery story: these files are DERIVED.")
        print("Delete every one of them and this command puts them back.")
        return 0

    print("Knowledge lives in %s" % base)
    print()
    rows = existing()
    if not rows:
        print("  nothing yet - run --rebuild")
    for scope, label, path, size in rows:
        print("  %-13s %-28s %6d bytes" % (scope, label or SCOPE_MEANING[scope][:28], size))
    print()
    print("One file per scope, so a cross-scope query is not something anyone")
    print("can write - not something they are asked not to.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
