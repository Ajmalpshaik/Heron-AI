#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
requirements-lock.txt - the pinned packages the one-step installer puts on a PC.

WHAT THIS PROVES
----------------
The SHAPE that makes `pip install --require-hashes -r requirements-lock.txt`
safe to run on a modeller's PC:

    every package is pinned to one exact version, once
    every package carries at least one sha256 hash - pip refuses the whole
      file under --require-hashes if one line has none
    everything requirements.txt REQUIRES is in it, and so is mcp - the two
      docs/WHAT-TO-INSTALL.md rows 4 and 5 that the installer now covers
    cryptography and cffi are pinned, because `pip install mcp` unpinned is
      what pulled a broken cryptography on 2026-09-16
    the Python it says it needs is the one PythonSetup refuses below
    the file PythonSetup names is this file

WHAT IT CANNOT PROVE
--------------------
That the versions work on Windows. The header of requirements-lock.txt says
what was run on it and where; NEEDS-CHECKING Group EP owes the Windows run.
Nothing here touches the network.

    python tests/test_python_lock.py

Exit 0 = the lock has its shape. Exit 1 = it does not, and what is wrong is named.
"""

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LOCK = os.path.join(ROOT, "requirements-lock.txt")
REQUIRED = os.path.join(ROOT, "requirements.txt")
SETUP = os.path.join(ROOT, "platform", "Heron.Installer", "PythonSetup.cs")

FAILURES = []

PIN = re.compile(r"^([A-Za-z0-9][A-Za-z0-9._-]*)==([^\s\\;]+)")
HASH = re.compile(r"--hash=sha256:([0-9a-f]+)")


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def read(path):
    with io.open(path, encoding="utf-8") as handle:
        return handle.read()


def key(name):
    """A package name the way pip compares them."""
    return re.sub(r"[-_.]+", "-", name).lower()


def entries(text):
    """[(name, version, [hashes])] - one per requirement, continuation lines joined."""
    found = []
    for block in re.split(r"\n(?=\S)", text):
        first = block.strip().split("\n", 1)[0]
        if not first or first.startswith("#"):
            continue
        pin = PIN.match(first)
        found.append((pin.group(1) if pin else first,
                      pin.group(2) if pin else None,
                      HASH.findall(block)))
    return found


def required_names(text):
    """The package names requirements.txt lists, comments and blanks left out."""
    names = []
    for line in text.splitlines():
        line = line.split("#", 1)[0].strip()
        if line:
            names.append(re.split(r"[<>=!~;\[\s]", line, maxsplit=1)[0])
    return names


def main():
    for path in (LOCK, REQUIRED, SETUP):
        if not os.path.exists(path):
            print("FAILED  %s is missing" % os.path.relpath(path, ROOT))
            return 1

    lock = read(LOCK)
    setup = read(SETUP)
    found = entries(lock)
    pinned = dict((key(name), version) for name, version, _ in found)

    print("Every package pinned, once, and hashed")
    check(len(found) > 0, "the lock lists packages at all: %d" % len(found))
    unpinned = [name for name, version, _ in found if version is None]
    check(not unpinned, "every line is name==version%s"
          % ("" if not unpinned else " - not: %s" % ", ".join(unpinned)))
    names = [key(name) for name, _, _ in found]
    twice = sorted(set(n for n in names if names.count(n) > 1))
    check(not twice, "no package is listed twice%s"
          % ("" if not twice else " - %s" % ", ".join(twice)))
    bare = [name for name, _, hashes in found if not hashes]
    check(not bare, "every package carries a sha256 hash - one without makes pip "
          "refuse the whole file%s" % ("" if not bare else " - none on: %s" % ", ".join(bare)))
    short = [name for name, _, hashes in found if any(len(h) != 64 for h in hashes)]
    check(not short, "and every hash is 64 hex characters%s"
          % ("" if not short else " - not on: %s" % ", ".join(short)))
    check("-e " not in lock and "git+" not in lock and "http://" not in lock,
          "nothing is fetched from a folder, a repository or plain http")

    print()
    print("What a PC needs is in it")
    for name in required_names(read(REQUIRED)):
        check(key(name) in pinned, "requirements.txt requires %s, and the lock pins it" % name)
    check("mcp" in pinned, "mcp is pinned - the line between Claude Code and Heron")
    for name in ("cryptography", "cffi"):
        check(key(name) in pinned,
              "%s is pinned - unpinned, `pip install mcp` pulled a broken one" % name)

    print()
    print("It says what was measured, and the installer agrees with it")
    check("NOT RUN ON WINDOWS" in lock,
          "the header says it has not been run on Windows, rather than implying it")
    check("test_mcp_serves" in lock and "--require-hashes" in lock,
          "and names what WAS run on it")
    stated = re.search(r"PYTHON 3\.(\d+) OR LATER", lock)
    oldest = re.search(r"OldestMinor\s*=\s*(\d+)", setup)
    check(stated is not None and oldest is not None
          and stated.group(1) == oldest.group(1),
          "the Python it says it needs (3.%s) is the one PythonSetup refuses below (3.%s)"
          % (stated.group(1) if stated else "?", oldest.group(1) if oldest else "?"))
    check('LockFile = "requirements-lock.txt"' in setup,
          "PythonSetup installs from this file and no other")
    check('"--require-hashes"' in setup,
          "and passes --require-hashes, without which the hashes here check nothing")

    print()
    if FAILURES:
        print("FAILED (%d)" % len(FAILURES))
        for one in FAILURES:
            print("  - %s" % one)
        return 1

    print("requirements-lock.txt pins and hashes every package, and covers what")
    print("Heron requires. WHETHER THESE VERSIONS WORK ON WINDOWS IS NOT PROVED")
    print("HERE - NEEDS-CHECKING Group EP.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
