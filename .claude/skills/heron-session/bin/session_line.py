#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
One line when a session starts: where this branch stands against main, and how
many fragments are proven.

A SessionStart hook, wired from .claude/settings.json so it runs in every
session. It prints something like

    Heron: branch claude/x is 3 behind and 2 ahead of origin/main (as last
    fetched). Fragments: 328 PROVEN, 68 DRAFT of 396.

WHY THESE TWO FACTS AND NO OTHERS
---------------------------------
"Behind main" is the one that cost a whole pull request on 2026-09-22: a
session built work that had merged an hour earlier, because nothing told it
main had moved. The fragment counts are the question every session asks first
and AGENTS.md answers with a command; saying them here saves the command and
never states a number anybody typed - both are DERIVED on the spot, from git
and from each fragment's own `heron-status:` line.

WHAT IT DELIBERATELY DOES NOT DO
--------------------------------
  * IT DOES NOT FETCH. A session start is not the moment to wait on a network,
    so the comparison is with origin/main AS LAST FETCHED and the line says
    so. The moment that has to be current is the one before a merge, and the
    other hook in this folder (main_moved.py) fetches exactly then.
  * IT IS SILENT ON FAILURE. No git, no origin/main, no fragments, a bad
    payload - whatever part cannot be derived is left out, and if nothing can
    be, nothing is printed and the exit is 0. A status line that errors at the
    start of every session is a status line somebody switches off.
  * IT CHANGES NOTHING. Two read-only git questions and some file reads.

WINDOWS
-------
Run as `python "$CLAUDE_PROJECT_DIR/..."`, which Git Bash expands on Windows
the same way bash does on Linux. The output is JSON with every non-ASCII
character escaped, because a Windows console redirected to a pipe encodes in
the ANSI code page and dies on a character it has no byte for - the trap the
heron-ship skill describes, which would turn this line into a traceback.

THIS IS FOR DEVELOPING HERON. IT IS NOT PART OF WHAT A MODELLER INSTALLS.
"""

import glob
import io
import json
import os
import re
import subprocess
import sys

HOOK = "heron-session-line"

# Local git questions only. Generous for a busy Windows disk, and still far
# inside the hook's own timeout in .claude/settings.json.
GIT_SECONDS = 5

STATUS = re.compile(r"^heron-status:\s*(\S+)", re.M)


def git(args, where):
    """git's answer, stripped, or None. Never raises."""
    try:
        done = subprocess.run(["git"] + list(args), cwd=where,
                              capture_output=True, timeout=GIT_SECONDS)
    except (OSError, subprocess.SubprocessError, ValueError):
        return None
    if done.returncode != 0:
        return None
    return done.stdout.decode("utf-8", "replace").strip()


def branch_part(where):
    """'branch X is N behind and M ahead of origin/main (as last fetched)'."""
    top = git(["rev-parse", "--show-toplevel"], where)
    if not top:
        return None, None
    name = git(["rev-parse", "--abbrev-ref", "HEAD"], top)
    if not name:
        return None, top
    if name == "HEAD":
        short = git(["rev-parse", "--short", "HEAD"], top) or "?"
        name = "a detached HEAD at %s" % short
    else:
        name = "branch %s" % name
    counts = git(["rev-list", "--left-right", "--count",
                  "origin/main...HEAD"], top)
    if not counts:
        return "%s (no origin/main to compare with)" % name, top
    try:
        behind, ahead = [int(x) for x in counts.split()]
    except ValueError:
        return name, top
    if behind == 0 and ahead == 0:
        where_it_is = "level with origin/main"
    else:
        where_it_is = "%d behind and %d ahead of origin/main" % (behind, ahead)
    return "%s is %s (as last fetched)" % (name, where_it_is), top


def fragment_part(top):
    """'Fragments: N PROVEN, M DRAFT of T', read from each fragment's card."""
    counts = {}
    total = 0
    for card in glob.glob(os.path.join(top, "brain", "fragments", "*",
                                       "fragment.yaml")):
        try:
            with io.open(card, encoding="utf-8", errors="replace") as handle:
                found = STATUS.search(handle.read())
        except OSError:
            continue
        if not found:
            continue
        total += 1
        counts[found.group(1)] = counts.get(found.group(1), 0) + 1
    if not total:
        return None
    named = ["%d %s" % (counts[k], k) for k in ("PROVEN", "DRAFT") if k in counts]
    other = total - sum(counts.get(k, 0) for k in ("PROVEN", "DRAFT"))
    if other:
        named.append("%d other" % other)
    return "Fragments: %s of %d" % (", ".join(named), total)


def line_for(payload):
    """The whole line, or None when nothing could be derived."""
    where = payload.get("cwd") if isinstance(payload, dict) else None
    if not isinstance(where, str) or not os.path.isdir(where):
        where = os.environ.get("CLAUDE_PROJECT_DIR") or os.getcwd()
    first, top = branch_part(where)
    second = fragment_part(top or where)
    parts = [p for p in (first, second) if p]
    if not parts:
        return None
    return "Heron: " + ". ".join(parts) + "."


def main():
    session = ""
    try:
        raw = sys.stdin.read()
        payload = json.loads(raw) if raw.strip() else {}
        if isinstance(payload, dict):
            session = payload.get("session_id") or ""
        text = line_for(payload)
    except Exception:                               # noqa: BLE001 - silent by design
        text = None
    if text:
        print(json.dumps({
            "hookSpecificOutput": {"hookEventName": "SessionStart",
                                   "additionalContext": text},
            "systemMessage": text,
        }, ensure_ascii=True))
        sys.stdout.flush()
    try:
        sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
        import hook_log
        hook_log.record(HOOK, "said" if text else "silent", text or "", session)
    except Exception:                               # noqa: BLE001 - a diary, not a gate
        pass
    return 0


if __name__ == "__main__":
    sys.exit(main())
