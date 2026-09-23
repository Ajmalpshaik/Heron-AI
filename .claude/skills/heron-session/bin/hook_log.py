#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
What Heron's development hooks decided, one line each, and where that is kept.

    python .claude/skills/heron-session/bin/hook_log.py    # where the log is, or why there is none
    python tools/hook-report.py                            # what is in it

WHY A HOOK WRITES DOWN WHAT IT DECIDED
--------------------------------------
A hook that only nags gets switched off, and a hook that never fires may as
well not exist - and from inside one session nobody can tell which of the two
they are looking at. So every Heron hook appends one line per decision: which
hook, what it decided, what it said, when, and in which session. The report
counts them, so the question is answered from evidence rather than from
somebody's impression of a week.

It is also the evidence for the reason these hooks moved to
.claude/settings.json at all: the guard used to run only in a session that had
loaded its skill (docs/work-notes/plans/earlier-brain/01-findings.md, H2). A
log with the guard's decisions in every session is what "runs everywhere" looks
like when it is true.

WHERE, AND WHY IT IS NEVER A TYPED PATH
---------------------------------------
Only the path manager builds a Heron path (the revit-addin-conventions skill,
"Only the path manager builds paths"). Python cannot call HeronPaths, which is
C#, so this asks the Python seams that already resolve Heron's folders:

  1. HERON'S LOG FOLDER - HeronPaths.Logs, the folder the add-in writes its own
     daily log to, as mcp/client/heron_bridge_client.LOG_DIR resolves it.
     DERIVED: machine-local and disposable, which is exactly what a log of
     hook decisions is. %LOCALAPPDATA%\Heron\logs on Windows, and elsewhere
     the ~/.local/share/Heron/logs that .NET names there.

  2. THE KNOWLEDGE FOLDER - heron_scope.knowledge_dir(), only when the first
     cannot be used: when it would land inside this repository, or is not an
     absolute path at all. Until 2026-09-23 that was every Linux machine -
     the bridge client read only %LOCALAPPDATA% and answered a RELATIVE path
     there (FRAGMENT-ISSUES row 5b-168) - so a cloud session's diary went to
     HERON_KNOWLEDGE, the one folder it is always given (docs/38).

  3. NEITHER - no log, silently. A hook never fails, and never refuses, because
     it could not write its diary. `hook-report.py` says which of the three
     happened, so the silence is visible to anyone who asks.

A folder INSIDE this repository is refused whichever seam offered it. A log
committed by accident is a log published, and HERON_KNOWLEDGE can point
anywhere.

ONE LINE PER DECISION, AND BOUNDED
----------------------------------
JSON, one object per line, every non-ASCII character escaped - so the file
reads the same in any code page, and a torn or half-written line costs that
line only. What a hook said is kept to its first few hundred characters: the
report groups decisions by what was said, and a whole paragraph is not needed
for that.

When the file passes about a megabyte it becomes the one older copy and a new
file starts, so the two together stay near two megabytes whatever happens.
Nothing here ever deletes anything else.

THIS IS FOR DEVELOPING HERON. IT IS NOT PART OF WHAT A MODELLER INSTALLS.
"""

import io
import json
import os
import sys
import time

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)),
                                    "..", "..", "..", ".."))

NAME = "heron-hooks.jsonl"
OLDER = "heron-hooks.1.jsonl"

# Past this, the current file becomes the older copy. About a megabyte is a
# few thousand decisions - weeks of editing - and the report reads both.
KEEP_BYTES = 1000000

# What a hook said is cut to this. The report groups by it; a refusal's whole
# paragraph adds nothing a person reading the report needs.
SAID_CHARS = 300


def _inside(path, root):
    """True when `path` is `root` or anything under it."""
    try:
        full = os.path.realpath(path)
        base = os.path.realpath(root)
        return os.path.commonpath([full, base]) == base
    except ValueError:
        # Different drives on Windows: certainly not inside.
        return False


def _usable(candidate):
    """The candidate if it is absolute and outside this repository, else None."""
    if not candidate or not isinstance(candidate, str):
        return None
    if not os.path.isabs(candidate):
        return None
    if _inside(candidate, ROOT):
        return None
    return candidate


def _heron_log_folder():
    """HeronPaths.Logs, as the Python side of Heron resolves it."""
    where = os.path.join(ROOT, "mcp", "client")
    if where not in sys.path:
        sys.path.insert(0, where)
    import heron_bridge_client as BRIDGE           # noqa: E402 - the seam
    return BRIDGE.LOG_DIR


def _knowledge_folder():
    """The knowledge folder - HERON_KNOWLEDGE in a cloud session."""
    where = os.path.join(ROOT, "brain")
    if where not in sys.path:
        sys.path.insert(0, where)
    import heron_scope as SCOPE                     # noqa: E402 - the seam
    return SCOPE.knowledge_dir()


def folder():
    """(folder, how) - where the log lives, or (None, why there is none).

    Never raises. Each seam is asked in turn and the first usable answer wins.
    """
    reasons = []
    for ask, name in ((_heron_log_folder, "Heron's log folder"),
                      (_knowledge_folder, "the knowledge folder")):
        try:
            got = ask()
        except Exception as exc:                    # noqa: BLE001 - see below
            # A seam that cannot even be imported is one answer among three,
            # not a reason for a hook to fail.
            reasons.append("%s could not be asked (%s)" % (name, type(exc).__name__))
            continue
        usable = _usable(got)
        if usable:
            return usable, name
        if not got:
            reasons.append("%s is not set on this machine" % name)
        elif not os.path.isabs(got):
            reasons.append("%s resolves to a relative path (%s), which would "
                           "land inside the working folder" % (name, got))
        else:
            reasons.append("%s is inside this repository (%s)" % (name, got))
    return None, "; ".join(reasons)


def path():
    """(file, how) - the log file, or (None, why there is none)."""
    where, how = folder()
    if where is None:
        return None, how
    return os.path.join(where, NAME), how


def record(hook, decision, said="", session=""):
    """Append one decision. Returns the file written, or None. NEVER raises.

    A hook calls this after it has decided, and nothing it returns may change
    the decision - a diary that fails is a missing diary line, not a refusal.
    """
    try:
        target, _how = path()
        if target is None:
            return None
        where = os.path.dirname(target)
        if not os.path.isdir(where):
            os.makedirs(where)
        try:
            if os.path.getsize(target) > KEEP_BYTES:
                os.replace(target, os.path.join(where, OLDER))
        except OSError:
            pass
        line = json.dumps({
            "when": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
            "hook": str(hook),
            "decision": str(decision),
            "said": " ".join(str(said or "").split())[:SAID_CHARS],
            "session": str(session or ""),
        }, ensure_ascii=True, sort_keys=True)
        with io.open(target, "a", encoding="utf-8", newline="\n") as handle:
            handle.write(line + "\n")
        return target
    except Exception:                               # noqa: BLE001 - see docstring
        return None


def read(target=None):
    """(records, unreadable lines) from the older copy, then the current file.

    A line that does not parse is counted, never fatal - the file is appended
    to by several processes, and a torn line must not cost the rest.
    """
    if target is None:
        target, _how = path()
    records, bad = [], 0
    if not target:
        return records, bad
    older = os.path.join(os.path.dirname(target), OLDER)
    for one in (older, target):
        if not os.path.isfile(one):
            continue
        with io.open(one, encoding="utf-8", errors="replace") as handle:
            for raw in handle:
                raw = raw.strip()
                if not raw:
                    continue
                try:
                    got = json.loads(raw)
                except ValueError:
                    bad += 1
                    continue
                if isinstance(got, dict) and got.get("hook"):
                    records.append(got)
                else:
                    bad += 1
    return records, bad


def main():
    target, how = path()
    if target is None:
        print("No hook log on this machine: %s." % how)
        return 0
    print("Hook log: %s" % target)
    print("found through %s" % how)
    return 0


if __name__ == "__main__":
    sys.exit(main())
