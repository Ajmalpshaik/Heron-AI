#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
tools/heron-ready.py - a new PC is made ready, or told exactly what it lacks.

    python tests/test_heron_ready.py

A new laptop's first "create a family" took over seven minutes where the
office PC took one or two (2026-10-08): the knowledge store and the search
model were left for the first chat to build, and a missing package left Claude
reading Heron's files instead of calling its tools. The tool exists so a new
PC starts where the old one is. This holds the three things it promises:

  1. A missing REQUIRED package exits 1 and names the command that installs
     it - and builds nothing it cannot build properly.
  2. Its exit code follows its own FIX lines: 0 exactly when none is printed.
  3. Without --check, the knowledge store is BUILT, so the first chat finds
     it built. With --check, nothing is.

Every run points HERON_KNOWLEDGE at a scratch folder, so the shared store on
the machine running this is never touched (row 5b-233).

WHAT IT CANNOT DO: say anything about a real Windows PC - the Microsoft Store
shortcut, %APPDATA%, the add-in folders and Claude Code's own approvals are
not here to look at.
"""

import os
import shutil
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOL = os.path.join(ROOT, "tools", "heron-ready.py")

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def run(knowledge, *args, **env):
    """(exit code, output) of the tool with its store in `knowledge`."""
    environ = dict(os.environ, HERON_KNOWLEDGE=knowledge, PYTHONUTF8="1")
    environ.update(env)
    done = subprocess.run([sys.executable, TOOL] + list(args), cwd=ROOT,
                          env=environ, stdout=subprocess.PIPE,
                          stderr=subprocess.STDOUT, timeout=600)
    return done.returncode, done.stdout.decode("utf-8", "replace")


def main():
    if not os.path.exists(TOOL):
        check(False, "tools/heron-ready.py exists")
        return 1
    work = tempfile.mkdtemp(prefix="heron-ready-")
    try:
        print("1. A missing required package is named, with its command")
        shadow = os.path.join(work, "shadow")
        os.makedirs(shadow)
        with open(os.path.join(shadow, "yaml.py"), "w") as handle:
            handle.write("raise ImportError('PyYAML is not installed here')\n")
        store = os.path.join(work, "no-yaml")
        os.makedirs(store)
        code, out = run(store, PYTHONPATH=shadow)
        check(code == 1, "with no PyYAML it exits 1 (got %d)" % code)
        check("pip install --user -r requirements.txt" in out,
              "and names the command that installs it")
        check(os.listdir(store) == [],
              "and builds no knowledge store without it")

        print()
        print("2. The exit code follows the FIX lines")
        store = os.path.join(work, "check")
        os.makedirs(store)
        code, out = run(store, "--check")
        fixes = [line for line in out.splitlines()
                 if line.strip().startswith("FIX ")]
        check((code == 0) == (not fixes),
              "exit %d with %d FIX line(s)" % (code, len(fixes)))
        check(os.listdir(store) == [],
              "--check builds nothing")

        print()
        print("3. Without --check the store is built before any chat asks")
        store = os.path.join(work, "full")
        os.makedirs(store)
        code, out = run(store)
        check("tool cards ready" in out,
              "it reports the tool cards ready")
        check(any(name.endswith(".db") for name in os.listdir(store)),
              "and the store is on disk in the folder it was given: %s"
              % ", ".join(sorted(os.listdir(store))))
    finally:
        shutil.rmtree(work, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASSED - a missing package is named with its command, the exit code")
    print("follows what is printed, and the store is built before a chat asks.")
    print("It says nothing about a real Windows PC.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
