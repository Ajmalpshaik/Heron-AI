# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-STD-MET-014, HERON-NAM-MET-006
# Heron-Step:   1
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The metadata guard - and that it can see what D-78 claims it sees.

    python tests/test_metadata_guard.py

WHY THIS FILE EXISTS
  F17 found THREE agents owning metadata validity across two departments,
  and the owner folded HERON-NAM-MET-006 into `tools/check-metadata.py`
  (D-78). That closes a row without writing a line of code, which is the
  same shape as D-77 the same day - and the same reason to check rather
  than assert.

  The row's words are "metadata COMPLETENESS and SCHEMA VALIDITY". Those
  are two different claims, so an error is planted for each, plus one for
  the registry-against-code half that HERON-STD-MET-014 already owned.

WHAT IT PROVES
  1. THE CLAIM IS DERIVED, NOT TYPED. Both rows come from
     check-metadata.py's own header and are checked against docs/28.

  2. A CLEAN COPY PASSES. Without this control the three findings below
     mean nothing - and it is not hypothetical here: the first version of
     this suite built a MINIMAL tree instead of copying, and its control
     failed twice on files it had not copied. Each fix revealed another.
     A planted error found in a tree that was already failing is not a
     finding, it is a coincidence.

  3. A MISSING FIELD IS CAUGHT - completeness, the row's first word.

  4. AN INVALID LAYER IS CAUGHT - schema validity, the row's second.

  5. A CLAIM ON AN AGENT THAT DOES NOT EXIST IS CAUGHT - the registry
     against the code, which is the half that makes the standard worth
     having and the half that caught a withdrawn agent's orphan contract
     earlier the same day.

  6. IT WRITES NOTHING.

WHAT IT DOES NOT PROVE
  That anything GENERATES metadata. Nothing does, and after D-78 nothing
  is scheduled to. Like D-77's fold, this is the guarding half only.
"""

import hashlib
import io
import os
import re
import shutil
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

GUARD = os.path.join("tools", "check-metadata.py")
PLANT = os.path.join("brain", "heron_probe_metadata.py")

# The whole tree is copied, not a minimal one. check-metadata.py reads the
# registry, Directory.Build.props and the bridge client's version, and a
# hand-built subset failed on each in turn - see claim 2. `worktrees` is
# skipped by name, which is what stops a worktree copying itself.
SKIP = (".git", "worktrees", "bin", "obj", "node_modules", "__pycache__",
        ".vs")

GOOD = (u"# -*- coding: utf-8 -*-\n"
        u"# Heron-Agent:  none\n"
        u"# Heron-Step:   1\n"
        u"# Heron-Status: DRAFT\n"
        u"# Heron-Since:  0.1.0\n"
        u"# Heron-Layer:  brain\n"
        u"# See docs/29-metadata-standard.md\n")

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def claimed_ids(path):
    """The agent ids a header claims, read the way agent-count reads them."""
    found = set()
    with io.open(path, encoding="utf-8") as handle:
        for i, line in enumerate(handle):
            if i > 40:
                break
            m = re.match(r"^\s*(?://|#)\s*Heron-Agent\s*:\s*(.+?)\s*$", line)
            if not m or m.group(1).strip().lower() == "none":
                continue
            found |= set(a.strip() for a in m.group(1).split(",") if a.strip())
    return found


def run_guard(where):
    done = subprocess.run(
        [sys.executable, GUARD], cwd=where,
        stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=280)
    return done.returncode, done.stdout.decode("utf-8", "replace")


def fingerprint(where):
    """Every source file's bytes, so a write of any kind is visible."""
    prints = {}
    for dirpath, dirnames, filenames in os.walk(where):
        dirnames[:] = [d for d in dirnames if d not in SKIP]
        for name in filenames:
            if not name.endswith((".py", ".cs", ".ps1", ".md")):
                continue
            path = os.path.join(dirpath, name)
            with io.open(path, "rb") as handle:
                prints[os.path.relpath(path, where)] = hashlib.sha256(
                    handle.read()).hexdigest()
    return prints


def plant(tree, body):
    """Write the probe file and return what the guard said about it."""
    io.open(os.path.join(tree, PLANT), "w", encoding="utf-8").write(body)
    code, out = run_guard(tree)
    said = [l.strip() for l in out.split("\n")
            if "heron_probe_metadata" in l and l.strip().startswith("-")]
    return code, said


def main():
    print("THE METADATA GUARD, AND WHAT IT CAN SEE")
    print("=" * 72)

    print("\n1. the claim is derived, not typed")
    ids = claimed_ids(os.path.join(ROOT, GUARD))
    import heron_contract as CON
    known = CON.registry_ids()
    check(ids == {"HERON-STD-MET-014", "HERON-NAM-MET-006"},
          "check-metadata.py claims both rows: %s" % ", ".join(sorted(ids)))
    check(all(one in known for one in ids),
          "and both are real rows in docs/28")

    home = tempfile.mkdtemp(prefix="heron-meta-guard-")
    tree = os.path.join(home, "repo")
    try:
        shutil.copytree(ROOT, tree, ignore=shutil.ignore_patterns(*SKIP))

        print("\n2. a clean copy passes, so the findings below are the plant")
        before = fingerprint(tree)
        code, _out = run_guard(tree)
        check(code == 0,
              "the repository as it stands has clean metadata")

        print("\n6. and it wrote nothing while doing it")
        after = fingerprint(tree)
        changed = sorted(k for k in before if before[k] != after.get(k))
        check(not changed,
              "not one source file changed: %d checked%s"
              % (len(before),
                 "" if not changed else ", but %s did" % ", ".join(changed)))

        # The control again, this time WITH the probe file present and
        # correct - so claims 3 to 5 differ from this by one line each and
        # nothing else.
        code, said = plant(tree, GOOD)
        check(code == 0 and not said,
              "a correctly-headed probe file passes too, so each finding "
              "below is the one line that was changed")

        print("\n3. a missing field is caught (completeness)")
        code, said = plant(tree, GOOD.replace(u"# Heron-Layer:  brain\n", u""))
        check(code != 0, "the guard fails on a file missing Heron-Layer")
        check(any("missing Heron-Layer" in one for one in said),
              "and names the field: %s" % (said[:1] or "nothing said"))

        print("\n4. an invalid layer is caught (schema validity)")
        code, said = plant(tree, GOOD.replace(u"Heron-Layer:  brain",
                                              u"Heron-Layer:  banana"))
        check(code != 0, "the guard fails on a layer that is not a layer")
        check(any("banana" in one and "not one of" in one for one in said),
              "and says what the allowed set is: %s" % (said[:1] or "nothing"))

        print("\n5. a claim on an agent that does not exist is caught")
        code, said = plant(tree, GOOD.replace(u"Heron-Agent:  none",
                                              u"Heron-Agent:  HERON-NOT-REAL-001"))
        check(code != 0, "the guard fails on an id that is not in the registry")
        check(any("not in the registry" in one for one in said),
              "and says so rather than counting it as built: %s"
              % (said[:1] or "nothing said"))

        os.unlink(os.path.join(tree, PLANT))
        code, _out = run_guard(tree)
        check(code == 0,
              "and with the probe removed it passes again - so every "
              "finding was the plant and nothing else")
    finally:
        shutil.rmtree(home, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the guard sees what D-78 claims it sees")
    return 0


if __name__ == "__main__":
    sys.exit(main())
