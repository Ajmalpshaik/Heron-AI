# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-RAG-RNK-006
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
The usage counter - which clauses helped, kept on this PC as numbers.

    python brain/heron_usage.py --show --scope company
    python brain/heron_usage.py --helped <chunk id> --scope company
    python brain/heron_usage.py --forget --scope company

D-70 decided that Heron keeps it; D-190 is the owner's yes, 2026-11-20, to its
four open parts. R-16 and R-25 of the RAG requirement note.

WHAT IT KEEPS, AND NOTHING ELSE (D-190 clauses 1 and 4)
--------------------------------------------------------
Per clause - the chunk id a citation already carries:

    offered       how many times Heron showed this clause to the chat
    helped        how many times the modeller SAID it helped
    last_helped   the date of the last time they said so

Never the question, never the answer, never a sentence anybody typed. D-70's
shape was "QCS clause 21.4  helped 12 times", and the owner's reason for that
shape - "if we keep everything by note that will be big" - is why this is one
small file of counts that does not grow with use the way a log would.

"HELPED" IS SAID, NEVER GUESSED (D-190 clause 2)
------------------------------------------------
Only helped() moves the helped count, and only the modeller's own word reaches
it - the `heron_helped` tool, called when they say an answer was right, or this
command line. Nothing here infers it from a clause being cited, an answer being
kept, or a question not being asked again. A guessed signal would teach the
ranking whatever the guess got wrong.

LESS THAN ONE RANK (D-190 clause 3)
-----------------------------------
rate() is helped / offered, a number from 0 to 1. heron_retrieve multiplies it
by USAGE_SPAN, which is held below one fusion rank - so a clause that helped
every time settles a tie and never beats a clause the routes ranked higher.
Recency is recorded (`last_helped`) and reported, and NOT weighted: the owner
agreed the date is kept, and no weight for it was asked for or given.

WHERE IT LIVES, AND WHY NOT IN THE STORE (D-70's three constraints)
-------------------------------------------------------------------
  NOT IN THE DISPOSABLE INDEX. Golden Rule 11 says a store is rebuilt if it is
      deleted. A count cannot be rebuilt from the documents - it is canonical -
      so it sits BESIDE the store, as heron_ingest's manifest does, and
      deleting the store leaves it. A chunk id is the document's content hash
      and the chunk's place in it, so a restored store gets the same ids back
      and the counts still point at the right clauses.
  PER SCOPE. One file per store. A company store's counts never reach a
      project answer (Golden Rule 5).
  NEVER LEAVES. Nothing here opens a socket, and nothing reads this file but
      this module. It is not attached to anything sent to a model.

It is DELETABLE - forget() removes it, and nothing else is lost.
"""

import datetime
import json
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_scope as SCOPE                                   # noqa: E402

USAGE_SUFFIX = "-usage.json"

# The three numbers a clause carries. Named once, so a reader and a writer
# cannot spell one of them two ways.
OFFERED = "offered"
HELPED = "helped"
LAST_HELPED = "last_helped"


class UnknownClause(Exception):
    """A helped() for a chunk id this store does not hold. Said, never stored."""


def usage_path(store_path):
    """Beside the scope's own file, never inside it - the manifest's rule."""
    base = store_path[:-3] if store_path.endswith(".db") else store_path
    return base + USAGE_SUFFIX


def path_for(scope, project=None):
    """The counter file for a scope, without opening its store."""
    return usage_path(SCOPE.scope_path(scope, project))


def load(path):
    """{chunk id: {offered, helped, last_helped}} - {} when there is none.

    A file that cannot be read is an empty counter, never a crash: the counter
    only ever nudges an order, and an answer must not fail because of it.
    """
    try:
        with open(path, encoding="utf-8") as handle:
            data = json.load(handle)
    except (IOError, OSError, ValueError):
        return {}
    counts = data.get("clauses") if isinstance(data, dict) else None
    return counts if isinstance(counts, dict) else {}


def _save(path, counts):
    """Write whole and replace, so a reader never sees half a file. True if kept."""
    folder = os.path.dirname(path)
    temporary = path + ".tmp"
    try:
        if folder and not os.path.isdir(folder):
            os.makedirs(folder)
        with open(temporary, "w", encoding="utf-8") as handle:
            json.dump({"format": 1, "clauses": counts}, handle, sort_keys=True,
                      indent=1)
        os.replace(temporary, path)
        return True
    except (IOError, OSError):
        return False


def offered(path, chunk_ids):
    """Count each clause shown to the chat once. True if the count was kept.

    Never fatal: a counter that could break the answer it counts is worse than
    none. The same id twice in one call counts once - one answer, one showing.
    """
    ids = [str(one) for one in dict.fromkeys(chunk_ids or []) if one]
    if not ids:
        return True
    counts = load(path)
    for chunk_id in ids:
        entry = counts.setdefault(chunk_id, {OFFERED: 0, HELPED: 0})
        entry[OFFERED] = int(entry.get(OFFERED, 0)) + 1
    return _save(path, counts)


def helped(store, chunk_id, today=None):
    """The modeller said this clause helped. Returns its entry after the count.

    Refused - UnknownClause - for an id the store does not hold, so a typo is
    said rather than counted against nothing. A clause said to help before it
    was ever counted as offered is counted as offered once too, so the rate
    never passes 1.
    """
    chunk_id = str(chunk_id or "").strip()
    row = None
    if chunk_id:
        try:
            row = store.execute("SELECT id FROM chunks WHERE id = ?",
                                (chunk_id,)).fetchone()
        except Exception:                               # noqa: BLE001
            row = None
    if row is None:
        raise UnknownClause(
            "%r is not a clause in this knowledge. Use the chunk id the answer "
            "cited - the one in square brackets - and the same place it came "
            "from (%s)." % (chunk_id, SCOPE.in_plain_words(store.scope)))
    path = usage_path(store.path)
    counts = load(path)
    entry = counts.setdefault(chunk_id, {OFFERED: 0, HELPED: 0})
    entry[HELPED] = int(entry.get(HELPED, 0)) + 1
    entry[OFFERED] = max(int(entry.get(OFFERED, 0)), entry[HELPED])
    entry[LAST_HELPED] = (today or datetime.date.today()).isoformat()
    if not _save(path, counts):
        raise IOError("the counter beside this knowledge could not be written: "
                      "%s" % path)
    return dict(entry)


def rate(entry):
    """helped / offered, from 0 to 1 - or None when nothing was ever said.

    None, not 0, for a clause nobody has said anything about: no opinion is a
    different fact from "offered ten times and never helped".
    """
    if not isinstance(entry, dict):
        return None
    said = int(entry.get(HELPED, 0) or 0)
    shown = int(entry.get(OFFERED, 0) or 0)
    if said <= 0 or shown <= 0:
        return None
    return min(1.0, float(said) / shown)


def forget(path):
    """Delete the counter. True if there is none afterwards. Nothing else goes."""
    try:
        os.remove(path)
    except OSError:
        pass
    return not os.path.exists(path)


def _flag(argv, name):
    if name in argv:
        at = argv.index(name)
        if at + 1 < len(argv) and not argv[at + 1].startswith("--"):
            return argv[at + 1]
        raise ValueError("%s needs a value" % name)
    return None


def main(argv):
    try:
        scope = (_flag(argv, "--scope") or SCOPE.GLOBAL).lower()
        project = _flag(argv, "--project")
        chunk = _flag(argv, "--helped")
    except ValueError as why:
        print("  %s. Nothing was changed." % why)
        return 2
    if scope not in SCOPE.SCOPES:
        print("  %r is not a knowledge scope. Nothing was changed." % scope)
        return 2
    where = SCOPE.in_plain_words(scope, project)
    path = path_for(scope, project)

    if "--forget" in argv:
        gone = forget(path)
        print("The usage counter for the knowledge kept %s is %s." % (
            where, "deleted - nothing else was touched" if gone
            else "STILL THERE - it could not be deleted: %s" % path))
        return 0 if gone else 1

    if chunk:
        store = SCOPE.open_scope(scope, project)
        try:
            entry = helped(store, chunk)
        except UnknownClause as why:
            print("  %s" % why)
            return 2
        finally:
            store.close()
        print("Counted: clause %s helped (%d of %d times it was shown), %s."
              % (chunk, entry[HELPED], entry[OFFERED], entry[LAST_HELPED]))
        return 0

    counts = load(path)
    print("The usage counter for the knowledge kept %s - on this PC, never sent."
          % where)
    if not counts:
        print("  Nothing counted yet.")
        return 0
    for chunk_id in sorted(counts, key=lambda c: (-int(counts[c].get(HELPED, 0)), c)):
        entry = counts[chunk_id]
        print("  %-40s helped %d of %d%s" % (
            chunk_id, int(entry.get(HELPED, 0)), int(entry.get(OFFERED, 0)),
            ", last %s" % entry[LAST_HELPED] if entry.get(LAST_HELPED) else ""))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
