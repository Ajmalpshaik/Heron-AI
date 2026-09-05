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

    The key itself is the document's Project Information UniqueId - the same
    identity RevitWrite pins a document by, chosen because it is created with
    the document and survives save, rename and move. It is NOT the file name:
    naming a store after a file means renaming the file loses the knowledge,
    and the stale-name trap in docs/25 section 2a is the same mistake at a
    different layer.
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
        self.execute(
            "INSERT OR REPLACE INTO fragments "
            "(id, capability, semantic_identity, kind, status, domain, risk, "
            " folder, revit) VALUES (?,?,?,?,?,?,?,?,?)",
            (frag.id,
             frag.data.get("capability", ""),
             frag.semantic_identity or "",
             frag.kind or "",
             frag.status or "",
             frag.data.get("domain", ""),
             frag.data.get("risk", ""),
             FRAG.repo_relative(frag.folder),
             ",".join(frag.supported)))
        self.db.commit()

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
    found, problems = FRAG.load_all()
    store = open_scope(scope, project_key)
    try:
        store.execute("DELETE FROM fragments")
        for frag in found.values():
            if not FRAG.validate(frag):
                store.put_fragment(frag)
        return store.count(), problems
    finally:
        store.close()


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
                indexed = SEARCH.index(store)
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
