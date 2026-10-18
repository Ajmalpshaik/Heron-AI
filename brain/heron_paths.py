# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-WSP-PTH-007
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Paths - product, data and derived, in one place instead of five.

    python brain/heron_paths.py
    python brain/heron_paths.py --where    the home, and the old places it carries from
    python brain/heron_paths.py --carry    carry from the old places again, on purpose

WHAT IT IS FOR (docs/28, HERON-WSP-PTH-007)
--------------------------------------------
"Product / data / derived separation enforced in code." T1. The register
says ENFORCED, not described, which is the difference between a table in a
document and a function other agents ask.

THERE IS ALREADY A PLATFORM HALF, AND IT ANSWERS A DIFFERENT QUESTION
-----------------------------------------------------------------------
`platform/Heron.Core/HeronPaths.cs` carries the same agent id and was
built at Step 1. **It was found by running tools/agent-count.py after
this module was written, not before** - the search that preceded it
looked at brain, mcp, revit and tools and missed platform, which is the
same class of mistake as a grep that finds nothing because the pattern
cannot see what is there.

They are not duplicates, and the split is worth stating rather than
assuming:

  HeronPaths.cs   WHERE things live - the three root directories, built
                  for the add-in and the .NET side. "Nothing else may
                  build a Heron path."
  this module     WHICH CLASS a name belongs to, and which operation may
                  touch it. Built for the agents, which reason about
                  names long before anything opens a file.

What they must agree on is the meaning of the three words, and the suite
asserts that by reading the C# file's own definitions - because two
implementations of one agent that disagree about what DATA means is
exactly the drift this module exists to prevent, one layer up.

WHY THIS EXISTS AT ALL
------------------------
docs/06 s2 splits the workspace three ways and four agents built before
this one each carried their own copy of that split: HERON-OPS-UPD-010
refusing a product update that touches data, HERON-INS-CFG-006 splitting
machine from portable, HERON-INS-BRN-007 refusing to rebuild a knowledge
store, HERON-INS-RAG-008 rebuilding an index freely. Four copies of one
rule is four places for it to drift, and the drift would be silent - each
agent would still pass its own tests.

So the table lives here and they ask. HERON-OPS-UPD-010 was changed to do
that in the same commit, which is the only way to tell whether one place
is really enough.

THE THREE CLASSES, AND THE ONE QUESTION EACH ANSWERS
------------------------------------------------------
  PRODUCT   Core, Agents, Revit, MCP, Packages. Replaced wholesale on
            update; the user never edits it. Losing it costs a download.
  DATA      Brain, Skills, Fragments, Memory, Projects, Company, Logs,
            Backup. Belongs to the user and must survive every update,
            uninstall and reinstall. Losing it loses something nobody can
            regenerate.
  DERIVED   Cache, vector indexes. Safe to delete at any time, because
            something rebuilds them.

CLASSIFICATION SAYS WHAT IT KNOWS; PERMISSION ERRS TOWARD PROTECTION
----------------------------------------------------------------------
Those are two different jobs and merging them is how an unknown folder
gets treated as safe. `classify()` answers UNKNOWN when it does not
recognise a path, honestly - a workspace has more in it than any
document lists.

AND THERE ARE TWO KINDS OF UNKNOWN, WHICH THIS SAID WERE ONE
--------------------------------------------------------------
Until 2026-09-21 every message here said "the twenty folders docs/06 s2
names", and it is not twenty and they are not all named. MEASURED:

    19   folders docs/06 s2 NAMES, in its own tree
    14   of those its table gives a CLASS to
     5   it names and never classifies - Community, Configuration,
         Documentation, RAG, Tests
    17   words this module matches on: those 14, plus index, indexes
         and vector as its reading of the table's "vector indexes"

So all five of those come back UNKNOWN, and the sentence told the reader
they were not in docs/06 at all. They are. What is missing is a CLASS
for them, and that gap is the document's - Q-13 is the open question
about this whole split - so this module names the gap rather than
inventing five answers the document declined to give. `main()` below
demonstrated the bug as a feature, printing Documentation as an unknown
path; it now says which kind of unknown it is.

`may()` reads UNKNOWN as DATA. Not because it is, but because nothing can
show it is not, and the two wrong answers are not the same size: treating
product as data delays an update, and treating data as product destroys a
modeller's fragment library. The asymmetry is the whole reason to have a
default at all.

DATA IS TESTED FIRST
----------------------
"Brain agents" carries a word from both lists. The order is not
arbitrary - it is the same asymmetry one level down.

AND WHERE THE PYTHON SIDE'S OWN DATA LIVES (D-135)
----------------------------------------------------
Everything above answers WHICH CLASS. `data_home()` answers one WHERE, the
only one this module owns: the folder for the DATA Heron's Python side writes
- the knowledge store, each project's memory and the brain's audit.

It is %USERPROFILE%\\.heron on Windows, and NOT %APPDATA%\\Heron, because of
what a Microsoft Store Claude does. Windows keeps a private copy of
%APPDATA% and %LOCALAPPDATA% for that app and every process it starts -
Claude's shell and every heron MCP server - and deletes the copy when the
app is reset or uninstalled (rows 5b-416 to 5b-419). Measured on the owner's
laptop on 2026-10-17: once a folder exists in both the copy and the real
disk, a new file, a new folder, an edit to a file both hold and a
write-then-rename all land in the copy - and the copy held Heron\\knowledge,
Heron\\audit and Heron\\config. The profile and the temporary folder were
seen real from both sides the same night.

The add-in's own DATA - its settings and its audit trail - stays where
HeronPaths.Data puts it: Revit is not a child of the app, so it writes the
real disk. The Companion's notes are DERIVED and moved to the temporary
folder on both sides; `heron_bridge_client.temp_folder()` owns that one.

`carry_forward()` copies what the old places hold into the home and deletes
nothing. `home()` runs it once on a PC, by itself, and leaves a note.
"""

import hashlib
import io
import json
import ntpath
import os
import shutil
import sqlite3
import sys
import time
import urllib.parse

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

PRODUCT = "product"
DATA = "data"
DERIVED = "derived"
UNKNOWN = "unknown"

# docs/06 s2's own three lists. DATA is tested first: see the docstring.
FOLDERS = (
    (DATA, ("brain", "skills", "fragments", "memory", "projects", "company",
            "logs", "backup")),
    (DERIVED, ("cache", "index", "indexes", "vector")),
    (PRODUCT, ("core", "agents", "revit", "mcp", "packages")),
)

# NAMED BY docs/06 s2 AND GIVEN NO CLASS BY IT. Not an oversight here: the
# document's own table stops at fourteen of the nineteen it draws, and
# choosing a class for these five is Q-13's to settle, not this module's.
# They are listed so the refusal can say WHICH kind of unknown it is -
# "docs/06 does not name it" and "docs/06 names it and does not classify
# it" send a reader to two different places.
UNCLASSIFIED = ("community", "configuration", "documentation", "rag", "tests")

MEANING = {
    PRODUCT: "replaced wholesale on update; the user never edits it. "
             "Losing it costs a download.",
    DATA: "belongs to the user and must survive every update, uninstall "
          "and reinstall. Losing it loses something nobody can regenerate.",
    DERIVED: "safe to delete at any time, because something rebuilds it.",
    UNKNOWN: "docs/06 s2 gives it no class, either because it does not "
             "name it at all or because it names it and stops there.",
}

# Whether a class is IN SCOPE for an operation - and what being in scope
# MEANS differs per operation, which is why each row carries its own
# `why` rather than sharing a verb. A product update WRITES, an uninstall
# and a cleanup REMOVE, a repair REGENERATES, a backup COPIES. Flattening
# those into one verb would make backup's row read as permission to
# delete the data class.
#
# The five are the operations agents in this repository actually perform,
# and each row is that agent's own rule rather than a policy invented here.
OPERATIONS = {
    "product-update": {
        PRODUCT: True, DERIVED: True, DATA: False,
        "who": "HERON-OPS-UPD-010",
        "why": "docs/07 s7 rule 6: never touch the data class during a "
               "product update.",
    },
    "uninstall": {
        PRODUCT: True, DERIVED: True, DATA: False,
        "who": "HERON-INS-PKG-012",
        "why": "docs/06 s2: data must survive every update, uninstall and "
               "reinstall - so uninstalling cleanly means the product goes "
               "and the data stays.",
    },
    "repair": {
        PRODUCT: False, DERIVED: True, DATA: False,
        "who": "HERON-OPS-HEA-006",
        "why": "derived state is repaired freely because a generator "
               "rebuilds it; everything else is proposed.",
    },
    "cleanup": {
        PRODUCT: False, DERIVED: True, DATA: False,
        "who": "HERON-WSP-CLN-009",
        "why": "the register row says archives, never deletes - and the "
               "only class it may remove outright is the one something "
               "rebuilds.",
    },
    "backup": {
        PRODUCT: False, DERIVED: False, DATA: True,
        "who": "HERON-WSP-BAK-010",
        "why": "the register row says it backs up the data class and NOT "
               "the derived index - backing up something a generator "
               "rebuilds is paying to store a copy of a command.",
    },
}


def classify(path):
    """
    {klass, why} - product, data, derived, or UNKNOWN.

    Honest about not knowing, and honest about WHICH not-knowing it is.
    A workspace has more in it than any document lists, so a fourth
    answer is the truthful one rather than a gap - but five of the
    folders docs/06 s2 draws in its own tree are never given a class by
    it, and saying those are "not in docs/06" sends a reader looking in
    the wrong place. `unnamed` in the answer tells the two apart.
    """
    name = str(path or "").strip().lower()
    if not name:
        return {"class": UNKNOWN, "matched": None,
                "why": "nothing was named, so nothing can be classified."}
    # Tokenised on every separator a real name uses, so this answers for a
    # PATH ("Brain/global.db") and for a component NAME ("Brain agents")
    # alike - HERON-OPS-UPD-010 asks with the second kind.
    for character in ("\\", "-", "_", " ", ".", ","):
        name = name.replace(character, "/")
    words_here = [piece for piece in name.split("/") if piece]
    for klass, words in FOLDERS:
        for word in words:
            if word in words_here:
                return {"class": klass, "matched": word,
                        "why": "'%s' is %s: %s" % (word, klass,
                                                   MEANING[klass])}
    named_unclassified = [w for w in UNCLASSIFIED if w in words_here]
    if named_unclassified:
        return {"class": UNKNOWN, "matched": named_unclassified[0],
                "unnamed": False,
                "why": "'%s' IS one of the nineteen folders docs/06 s2 draws, "
                       "and is one of the five it never gives a class to - "
                       "%s. That is a gap in the document rather than in this "
                       "table, and choosing a class for them is Q-13's to "
                       "settle, so nothing is invented here."
                       % (named_unclassified[0], ", ".join(UNCLASSIFIED))}
    return {"class": UNKNOWN, "matched": None, "unnamed": True,
            "why": "%s matches none of the folders docs/06 s2 classifies, "
                   "and is not one of the five it names without classifying. "
                   "That is the truthful answer, not a gap - a workspace has "
                   "more in it than any document lists." % path}


def may(operation, path):
    """
    {allowed, class, why} - is this class in scope for this operation?

    In scope means different things per operation - write, remove,
    regenerate, copy - and the `why` says which. A caller reading only
    `allowed` for `backup` would have permission to COPY, never to
    delete.

    UNKNOWN is read as DATA here and only here. Not because it is, but
    because nothing can show it is not, and treating data as product
    destroys a fragment library while treating product as data delays an
    update.
    """
    rule = OPERATIONS.get(str(operation or "").strip().lower())
    if rule is None:
        return {"allowed": False, "refused": "NOT_AN_OPERATION",
                "why": "'%s' is not one of %s. Each of those is an agent's "
                       "own rule rather than a policy invented here, and "
                       "this agent does not make up a sixth."
                       % (operation, ", ".join(sorted(OPERATIONS)))}

    found = classify(path)
    if not str(path or "").strip():
        return {"allowed": False, "refused": "NO_PATH",
                "why": "no path was given, so there is nothing to decide "
                       "about."}

    klass = found["class"]
    read_as = DATA if klass == UNKNOWN else klass
    allowed = bool(rule[read_as])
    return {
        "allowed": allowed, "class": klass, "read_as": read_as,
        "rule": rule["who"],
        "why": "%s may%s touch %s. %s %s"
               % (operation, "" if allowed else " NOT", read_as, rule["why"],
                  "" if klass != UNKNOWN else
                  "%s is UNKNOWN and is read as data: nothing can show it is "
                  "not the user's, and treating data as product destroys a "
                  "fragment library while treating product as data delays an "
                  "update." % path),
    }


# ------------------------------------------------------------ the home (D-135)

#: The folder under the user's profile that holds the DATA the Python side
#: writes. A dot, because "Heron" is a name a person may well give the folder
#: they unzip the PRODUCT into - and docs/06 s2 keeps the two apart.
HOME_FOLDER = ".heron"

#: The note the first carry leaves in the home: what it copied, from where.
CARRIED_NOTE = "carried.json"

#: Taken by the process that is carrying, so two chats starting at once do not
#: both merge the same audit lines. One older than this was left by a process
#: that died, and is taken over.
CARRYING_LOCK = "carrying.lock"
LOCK_SECONDS = 600

#: How long a process that finds the lock taken waits for the carry to end.
#: The laptop's whole carry - 39 files, 25,000 audit lines - is a second or two.
WAIT_SECONDS = 60

#: A Microsoft Store Claude's package folders under %LOCALAPPDATA%\Packages.
STORE_CLAUDE = "AnthropicPBC.Claude_"

#: What is carried from an old place, and how: the whole knowledge folder,
#: file by file; and the BRAIN's audit files only, merged line by line. The
#: add-in's trail and its settings are not the Python side's - they stay where
#: the add-in writes and reads them.
CARRIED = (("knowledge", None), ("audit", "audit-brain-"))

#: SQLite's own side files. A store is carried through SQLite's backup, which
#: reads them; copied raw, they would be a second, torn copy of it.
SIDE_FILES = ("-wal", "-shm", "-journal", ".part")

# Homes this process has already looked at, so the note is read once.
_carried_here = set()


def data_home(environ=None, system=None):
    """The folder for the DATA Heron's Python side writes, or None.

    HERON_DATA when set, on any system - which is what the tests plant, and a
    person's own choice of folder. On Windows, %USERPROFILE%\\.heron: outside
    %APPDATA% and %LOCALAPPDATA%, which a Microsoft Store Claude keeps a
    private copy of and a Reset of that app deletes (D-135). Anywhere else,
    None - there is no profile convention to follow, and a machine with none
    keeps nothing, as it did when this was %APPDATA% and Linux had none.

    Pure: it looks at nothing on disk. `home()` is the one that carries.
    """
    env = os.environ if environ is None else environ
    chosen = (env.get("HERON_DATA") or "").strip()
    if chosen:
        return chosen
    if (os.name if system is None else system) != "nt":
        return None
    profile = (env.get("USERPROFILE") or "").strip()
    # A profile that is not a whole path would put the home inside whatever
    # folder the process started in - none is the honest answer (row 5b-168).
    if not profile or not ntpath.isabs(profile):
        return None
    return os.path.join(profile, HOME_FOLDER)


def old_homes(environ=None):
    """Where the Python side kept its DATA before D-135 - each Store Claude's
    private copy of %APPDATA%\\Heron first, then %APPDATA%\\Heron as this
    process sees it. Only the folders that exist.

    THE COPY FIRST, AND BY ITS OWN PATH. Inside such an app, %APPDATA%\\Heron
    shows the copy's file wherever both hold one, so the copy is what every
    chat has been using; outside it, %APPDATA%\\Heron is the real folder and
    the copy is reachable only by its LocalCache path. Naming the copy
    explicitly makes a carry from either side take what the chats wrote.
    """
    env = os.environ if environ is None else environ
    found = []
    local = (env.get("LOCALAPPDATA") or "").strip()
    if local:
        packages = os.path.join(local, "Packages")
        try:
            names = sorted(os.listdir(packages))
        except OSError:
            names = []
        for name in names:
            if name.startswith(STORE_CLAUDE):
                folder = os.path.join(packages, name, "LocalCache", "Roaming", "Heron")
                if os.path.isdir(folder):
                    found.append(folder)
    roaming = (env.get("APPDATA") or "").strip()
    if roaming and os.path.isdir(os.path.join(roaming, "Heron")):
        found.append(os.path.join(roaming, "Heron"))
    return found


def _digest(path):
    hasher = hashlib.sha256()
    with open(path, "rb") as handle:
        for block in iter(lambda: handle.read(1 << 20), b""):
            hasher.update(block)
    return hasher.hexdigest()


def _readonly_uri(path):
    """SQLite's read-only URI for a file, drive letter and spaces included."""
    spelled = os.path.abspath(path).replace("\\", "/")
    if not spelled.startswith("/"):
        spelled = "/" + spelled
    return "file://" + urllib.parse.quote(spelled, safe="/:") + "?mode=ro"


def _copy(source, target):
    """Copy one file into a place it is not yet, all or nothing.

    A store goes through SQLite's backup, read-only: a chat on an older Heron
    may be writing it this moment, and a byte copy of a store being written
    is a torn store. Anything that is not a store is copied as it is. Either
    way the copy is written beside the target and renamed into place."""
    folder = os.path.dirname(target)
    if not os.path.isdir(folder):
        os.makedirs(folder)
    partial = target + ".part"
    if source.lower().endswith(".db"):
        try:
            reading = sqlite3.connect(_readonly_uri(source), uri=True)
            try:
                writing = sqlite3.connect(partial)
                try:
                    reading.backup(writing)
                finally:
                    writing.close()
            finally:
                reading.close()
            os.replace(partial, target)
            return
        except sqlite3.DatabaseError:
            # Named .db and not a store: copied as it is, below.
            if os.path.exists(partial):
                os.remove(partial)
    shutil.copy2(source, partial)
    os.replace(partial, target)


def _merge_lines(source, target):
    """Add to `target` every line of `source` it does not hold. The number added.

    An audit file is append-only and every line is one whole record, so the
    union of two is the trail of both - and a second merge adds nothing."""
    with io.open(source, encoding="utf-8", errors="replace", newline="") as handle:
        wanted = [row.strip() for row in handle if row.strip()]
    held, ends_clean = set(), True
    if os.path.isfile(target):
        with io.open(target, encoding="utf-8", errors="replace", newline="") as handle:
            text = handle.read()
        held = set(row.strip() for row in text.splitlines() if row.strip())
        ends_clean = not text or text.endswith("\n")
    adding = []
    for row in wanted:
        if row not in held:
            held.add(row)
            adding.append(row)
    if adding:
        folder = os.path.dirname(target)
        if not os.path.isdir(folder):
            os.makedirs(folder)
        with io.open(target, "a", encoding="utf-8", newline="") as handle:
            handle.write(("" if ends_clean else "\n") + "\n".join(adding) + "\n")
    return len(adding)


def carry_forward(home, sources):
    """Copy the DATA the old places hold into `home`. Delete nothing.

    Sources in order, so the first that holds a file is the one carried. A
    knowledge file the home has not got is copied; one it holds the same is
    counted; one it holds DIFFERENTLY is left as the home has it and named in
    `left`, because the home's own file is never overwritten. The brain's
    audit is merged line by line instead. Nothing in a source is changed:
    carried means copied, never moved.

    Returns {home, sources, copied, same, lines, left, failed}.
    """
    report = {"home": home, "sources": list(sources), "copied": 0, "same": 0,
              "lines": 0, "left": [], "failed": []}
    # ONE FILE BY TWO NAMES IS READ ONCE. Inside a Store Claude, %APPDATA%\\Heron
    # shows the copy's own files, so the copy's LocalCache path and the
    # %APPDATA% path open the same file - measured on the laptop, 2026-10-18,
    # where the second look at each store reported it "left", because a store
    # carried through SQLite's backup is not byte for byte its source.
    seen = set()
    for source in sources:
        for part, only in CARRIED:
            top = os.path.join(source, part)
            if not os.path.isdir(top):
                continue
            for here, folders, files in os.walk(top):
                folders.sort()
                for name in sorted(files):
                    if name.endswith(SIDE_FILES):
                        continue
                    if only and not (name.startswith(only) and name.endswith(".jsonl")):
                        continue
                    path = os.path.join(here, name)
                    target = os.path.join(home, os.path.relpath(path, source))
                    try:
                        found = os.stat(path)
                        identity = (found.st_dev, found.st_ino)
                        if found.st_ino and identity in seen:
                            report["same"] += 1
                            continue
                        seen.add(identity)
                        if only:
                            report["lines"] += _merge_lines(path, target)
                        elif not os.path.exists(target):
                            _copy(path, target)
                            report["copied"] += 1
                        elif _digest(path) == _digest(target):
                            report["same"] += 1
                        else:
                            report["left"].append(path)
                    except (IOError, OSError, sqlite3.Error) as error:
                        report["failed"].append("%s: %s" % (path, error))
    return report


def _note(home, report, how):
    """Add this carry to the home's note."""
    path = os.path.join(home, CARRIED_NOTE)
    noted = {"format": "1", "runs": []}
    if os.path.isfile(path):
        try:
            with io.open(path, encoding="utf-8") as handle:
                noted = json.load(handle)
        except (IOError, OSError, ValueError):
            pass
    run = dict(report)
    run.update({"at": time.strftime("%Y-%m-%dT%H:%M:%S"), "how": how,
                "pid": os.getpid()})
    noted.setdefault("runs", []).append(run)
    if not os.path.isdir(home):
        os.makedirs(home)
    partial = path + ".part"
    with io.open(partial, "w", encoding="utf-8") as handle:
        handle.write(json.dumps(noted, indent=1, sort_keys=True))
    os.replace(partial, path)


def home(environ=None, system=None):
    """`data_home()`, carried into once on this PC before anything uses it.

    The FIRST time a process asks for the default home and finds no note in
    it, the old places are carried in and a note is left saying what was
    done; after that this is one look for the note a process. A HERON_DATA
    folder is somebody's own and is never filled. NEVER FATAL: a carry that
    fails leaves no note, so the next process tries again, and the folder is
    answered either way - a chat must not stop because a copy could not be
    made. `python brain/heron_paths.py --carry` carries again on purpose.
    """
    env = os.environ if environ is None else environ
    found = data_home(env, system)
    if not found or (env.get("HERON_DATA") or "").strip():
        return found
    key = os.path.normcase(os.path.abspath(found))
    if key in _carried_here:
        return found
    _carried_here.add(key)
    try:
        if os.path.isfile(os.path.join(found, CARRIED_NOTE)):
            return found
        if not os.path.isdir(found):
            os.makedirs(found)
        note = os.path.join(found, CARRIED_NOTE)
        lock = os.path.join(found, CARRYING_LOCK)
        # TWO CHATS STARTED TOGETHER: the second WAITS for the first's carry
        # rather than going on with a half-filled home - where it would find
        # no store, build a new one, and leave the old store standing beside it
        # as "left". Up to WAIT_SECONDS, then it goes on regardless.
        deadline = time.time() + WAIT_SECONDS
        while True:
            try:
                handle = os.open(lock, os.O_CREAT | os.O_EXCL | os.O_WRONLY)
                break
            except OSError:
                if os.path.isfile(note):
                    return found                    # the other process finished
                try:
                    age = time.time() - os.path.getmtime(lock)
                except OSError:
                    continue                        # let go just now: try again
                if age >= LOCK_SECONDS:
                    os.remove(lock)                 # left by a process that died
                    continue
                if time.time() >= deadline:
                    return found
                time.sleep(0.25)
        try:
            os.write(handle, str(os.getpid()).encode("ascii"))
            os.close(handle)
            if not os.path.isfile(note):            # finished while this one waited
                _note(found, carry_forward(found, old_homes(env)), "first use")
        finally:
            os.remove(lock)
    except Exception:                               # noqa: BLE001 - never fatal, see above
        pass
    return found


def where_and_carry(argv):
    """--where and --carry: the home on this PC, and carrying into it again."""
    found = data_home()
    print("HOME    %s" % (found or "none - no HERON_DATA, and not Windows"))
    for folder in old_homes():
        print("OLD     %s" % folder)
    if "--carry" not in argv or not found:
        return 0
    report = carry_forward(found, old_homes())
    _note(found, report, "--carry")
    print("CARRIED %d file(s) copied, %d already the same, %d audit line(s) added"
          % (report["copied"], report["same"], report["lines"]))
    for path in report["left"]:
        print("LEFT    %s - the home already holds its own file of that name, "
              "kept as the home has it" % path)
    for line in report["failed"]:
        print("FAILED  %s" % line)
    print("Nothing in the old places was changed or deleted.")
    return 1 if report["failed"] else 0


def main(argv):
    if "--where" in argv or "--carry" in argv:
        return where_and_carry(argv)
    print("PATHS   product, data and derived, in one place instead of five")
    print("=" * 72)

    paths = ["Core/Heron.dll", "Fragments/duct-sizing", "Cache/vectors.db",
             "Brain/global.db", "Packages/mep-pack", "Logs/audit.jsonl",
             "Documentation/readme.md"]
    for path in paths:
        found = classify(path)
        print("  %-26s %-9s %s" % (path, found["class"],
                                   found["why"][:40]))

    print()
    print("  And what each operation may touch:")
    print("  %-18s %s" % ("", "  ".join("%-9s" % path.split("/")[0]
                                        for path in paths[:5])))
    for operation in sorted(OPERATIONS):
        row = ["%-9s" % ("yes" if may(operation, path)["allowed"] else "NO")
               for path in paths[:5]]
        print("  %-18s %s" % (operation, "  ".join(row)))

    print()
    print("  An unknown path is read as DATA, and only for permission -")
    print("  and the TWO KINDS of unknown are told apart:")
    for path in ("Documentation/readme.md", "Sketches/idea.txt"):
        found = classify(path)
        answer = may("cleanup", path)
        print("    %-24s %-8s named by docs/06: %-5s reads as %s"
              % (path, found["class"],
                 "no" if found.get("unnamed") else "YES", answer["read_as"]))
    print()
    print("    Documentation is one of the FIVE docs/06 s2 draws and never")
    print("    classifies - %s." % ", ".join(UNCLASSIFIED))
    print("    Saying it is 'not in docs/06' sent a reader to the wrong page.")

    print()
    print("  Four agents each carried their own copy of docs/06 s2 before")
    print("  this. Four copies of one rule is four places for it to drift,")
    print("  and the drift would be silent - each would still pass its own")
    print("  tests. HERON-OPS-UPD-010 asks this one now.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
