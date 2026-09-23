# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The hook report - does it count what the hooks wrote, and only that?

    python tests/test_hook_report.py

tools/hook-report.py reads the diary every Heron hook appends to and says how
often each one decided something, in how many sessions, and what it said most.
It exists so "does the guard run in every session?" and "does this hook only
nag?" are answered from evidence.

WHAT THIS PROVES
  1. IT COUNTS PER HOOK: decisions, sessions, and each kind of decision, from
     a log written here with known contents.
  2. IT SHOWS WHAT A HOOK SAID MOST, and how often it spoke at all - the
     number that finds a hook that only nags.
  3. A TORN LINE IS COUNTED, NEVER FATAL. The log is appended to by several
     processes; one bad line must not hide the rest.
  4. THE OLDER COPY IS READ TOO, so a rotation does not halve the history.
  5. --days keeps only the recent decisions.
  6. NO LOG IS AN ANSWER, NOT A CRASH - and it exits 0 whatever it finds,
     because it is a report, not a gate.
  7. It finds the log the SAME way the hooks write it, through
     hook_log.path(), rather than through a path of its own.
"""

import io
import json
import os
import shutil
import subprocess
import sys
import tempfile
import time

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOL = os.path.join(ROOT, "tools", "hook-report.py")

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except AttributeError:
    pass

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def report(args, env):
    got = subprocess.run([sys.executable, TOOL] + list(args),
                         capture_output=True, text=True, encoding="utf-8",
                         env=env, timeout=60)
    return got.returncode, got.stdout + got.stderr


def entry(hook, decision, said="", session="s1", when=None):
    return json.dumps({"when": when or time.strftime("%Y-%m-%dT%H:%M:%SZ",
                                                     time.gmtime()),
                       "hook": hook, "decision": decision, "said": said,
                       "session": session})


def main():
    print("THE HOOK REPORT")
    print("=" * 72)
    home = tempfile.mkdtemp(prefix="heron-hook-report-")
    try:
        run_all(home)
    finally:
        shutil.rmtree(home, ignore_errors=True)
    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1
    print("PASSED - the report counts what the hooks wrote, per hook and per")
    print("session, survives a torn line and a rotation, and exits 0.")
    return 0


def run_all(home):
    env = dict(os.environ)
    for name in ("LOCALAPPDATA", "XDG_DATA_HOME", "APPDATA", "HERON_KNOWLEDGE"):
        env.pop(name, None)
    # The per-user local folder, as .NET reads it: LOCALAPPDATA on Windows,
    # XDG_DATA_HOME elsewhere. Both, so this runs the same on either.
    env["LOCALAPPDATA"] = env["XDG_DATA_HOME"] = home
    logs = os.path.join(home, "Heron", "logs")
    os.makedirs(logs)
    refusal = ("This edit would put the Revit vendor namespace in brain, and "
               "it may appear only inside revit/ or tools/")
    old = "2020-01-01T00:00:00Z"
    with io.open(os.path.join(logs, "heron-hooks.1.jsonl"), "w",
                 encoding="utf-8") as handle:
        handle.write(entry("heron-guard", "allow", session="s0", when=old) + "\n")
    with io.open(os.path.join(logs, "heron-hooks.jsonl"), "w",
                 encoding="utf-8") as handle:
        for line in (entry("heron-guard", "allow", session="s1"),
                     entry("heron-guard", "allow", session="s2"),
                     entry("heron-guard", "deny", refusal, session="s2"),
                     entry("heron-guard", "deny", refusal, session="s2"),
                     '{"when": "2026-09-23T00:00:00Z", "hook": "torn',
                     entry("heron-main-moved", "advised",
                           "Heron, before this merge: origin/main has 2 commits",
                           session="s2"),
                     entry("heron-main-moved", "up-to-date", session="s3"),
                     entry("heron-session-line", "said",
                           "Heron: branch x is level with origin/main.",
                           session="s3")):
            handle.write(line + "\n")

    print()
    print("1. It counts per hook, from the log the hooks write")
    print("-" * 72)
    code, out = report([], env)
    print("\n".join("      | " + l for l in out.splitlines()))
    check(code == 0, "it exits 0 - a report, not a gate")
    check(os.path.join(logs, "heron-hooks.jsonl") in out,
          "it found the log through hook_log.path(), in Heron's log folder")
    check("heron-guard - 5 decision(s) in 3 session(s)" in out,
          "the guard: five decisions in three sessions, the older copy included")
    check("allow 3, deny 2" in out, "each kind of decision, counted")
    check("spoke in 2 of them (40%)" in out,
          "how often it said anything at all - the nagging number")
    check("2x  This edit would put the Revit vendor namespace" in out,
          "and what it said most, with how often")
    check("heron-main-moved - 2 decision(s) in 2 session(s)" in out
          and "advised 1, up-to-date 1" in out,
          "the main-moved hook, counted separately")
    check("heron-session-line - 1 decision(s)" in out,
          "and the session line")
    check("1 unreadable line(s)" in out,
          "a torn line is COUNTED and shown, never fatal")
    check("8 decision(s) from 3 hook(s) in 4 session(s)" in out,
          "and the totals add up across every hook and both files")

    print()
    print("2. --days keeps only the recent ones")
    print("-" * 72)
    code, out = report(["--days", "30"], env)
    check(code == 0 and "heron-guard - 4 decision(s) in 2 session(s)" in out,
          "the decision from 2020 is left out of the last thirty days")

    print()
    print("3. No log is an answer")
    print("-" * 72)
    empty = os.path.join(home, "empty")
    os.makedirs(empty)
    code, out = report([], dict(env, LOCALAPPDATA=empty, XDG_DATA_HOME=empty))
    check(code == 0 and "does not exist yet" in out,
          "a machine where no hook has decided anything yet says so, exit 0")
    # No USABLE folder: Heron's log folder would land inside this repository,
    # which the diary refuses, and there is no knowledge folder - APPDATA
    # goes too, because on Windows the knowledge folder is found through it.
    inside = os.path.join(ROOT, "tests")
    bare = dict(env, LOCALAPPDATA=inside, XDG_DATA_HOME=inside)
    bare.pop("APPDATA", None)
    code, out = report([], bare)
    check(code == 0 and "No hook log on this machine" in out,
          "a machine with no usable log folder says why, and still exits 0")
    code, out = report(["--log", os.path.join(logs, "heron-hooks.jsonl")], bare)
    check(code == 0 and "heron-guard - 5 decision(s)" in out,
          "--log reads a named file, whatever this machine would have used")


if __name__ == "__main__":
    sys.exit(main())
