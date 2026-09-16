# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-DOC-VAL-009, HERON-DOC-RDM-007, HERON-DOC-ARC-006
# Heron-Step:   1
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The documentation guard - and that it can see what it is claimed to see.

    python tests/test_docs_guard.py

WHY THIS FILE EXISTS
  F23 asked whether HERON-DOC-RDM-007 and HERON-DOC-ARC-006 should be
  built beside `tools/check-docs.py` or folded into it, and the owner
  folded them (D-77). Folding closes two rows WITHOUT WRITING A LINE OF
  CODE, which makes it exactly the kind of claim this repository keeps
  finding it has believed without checking - a claim quietly ageing into
  a fact.

  So the claim is checked rather than asserted: an error is PLANTED where
  each row says the guard should be looking, and the guard has to find
  it. This repository's own rule is prove the pattern can see what you
  know is there, and it was broken four times in the session that wrote
  this.

WHAT IT PROVES
  1. THE CLAIM IS DERIVED, NOT TYPED. The three rows come from
     check-docs.py's own header and are checked against docs/28 - so a
     renamed row fails here rather than going unnoticed.

  2. THE GUARD PASSES ON THE TREE AS IT STANDS. Without this the two
     findings below mean nothing: a checker that always fails would
     "catch" a planted error by accident.

  3. A FALSE COUNT PLANTED IN README.md IS CAUGHT - which is what
     HERON-DOC-RDM-007 asks of it, and why that row folds in here.

  4. A BROKEN REFERENCE PLANTED IN AN ARCHITECTURE DOC IS CAUGHT - which
     is HERON-DOC-ARC-006's half, and the reason the sweep has to reach
     `docs/`, not only the README.

  5. IT WRITES NOTHING. A guard that edits what it guards is a generator.
     These two rows were folded as the GUARDING half ONLY, and D-77 says
     so; this is what makes that limitation a fact rather than a promise.

WHAT IT DOES NOT PROVE
  That the README is WRITTEN by anybody. Nothing generates it, and after
  D-77 nothing is scheduled to. The rows are closed on the guarding half
  and the register says which half that is.
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

GUARD = os.path.join("tools", "check-docs.py")
ARCH = os.path.join("docs", "06-heron-platform.md")

# Copied rather than checked in place: this suite plants deliberate errors,
# and the repository it is running in may have somebody else working in it.
#
# `.claude` IS COPIED, and leaving it out was the first version's bug. The
# guard sweeps 140 markdown files and ten of them live there; a copy without
# them fails on links into files that were never missing, which would have
# made every finding below meaningless. `worktrees` is skipped by NAME,
# which is what check-docs.py itself skips and what stops a worktree copying
# itself.
SKIP = (".git", "worktrees", "bin", "obj", "node_modules",
        "__pycache__", ".vs")

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def claimed_ids(path):
    """The agent ids a file's header claims, read the way agent-count reads."""
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
    """(exit code, output) from running the guard inside a tree."""
    done = subprocess.run(
        [sys.executable, GUARD], cwd=where,
        stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=280)
    return done.returncode, done.stdout.decode("utf-8", "replace")


def fingerprint(where):
    """Every markdown file's bytes, so a write of any kind is visible."""
    prints = {}
    for dirpath, dirnames, filenames in os.walk(where):
        dirnames[:] = [d for d in dirnames if d not in SKIP]
        for name in filenames:
            if not name.endswith(".md"):
                continue
            path = os.path.join(dirpath, name)
            with io.open(path, "rb") as handle:
                prints[os.path.relpath(path, where)] = hashlib.sha256(
                    handle.read()).hexdigest()
    return prints


def main():
    print("THE DOCUMENTATION GUARD, AND WHAT IT CAN SEE")
    print("=" * 72)

    print("\n1. the claim is derived, not typed")
    ids = claimed_ids(os.path.join(ROOT, GUARD))
    import heron_contract as CON
    known = CON.registry_ids()
    check(len(ids) == 3,
          "check-docs.py claims three rows: %s" % ", ".join(sorted(ids)))
    check(all(one in known for one in ids),
          "and every one of them is a real row in docs/28")
    check("HERON-DOC-RDM-007" in ids and "HERON-DOC-ARC-006" in ids,
          "including the two D-77 folded in")

    home = tempfile.mkdtemp(prefix="heron-docs-guard-")
    tree = os.path.join(home, "repo")
    try:
        shutil.copytree(ROOT, tree, ignore=shutil.ignore_patterns(*SKIP))

        print("\n2. the guard passes on the tree as it stands")
        before = fingerprint(tree)
        code, _out = run_guard(tree)
        check(code == 0,
              "a clean copy of this repository passes, so a failure below "
              "is the planted error and not the weather")

        print("\n5. and it wrote nothing while doing it")
        # Asserted HERE, against the clean run, because a guard is at its
        # most tempting to "fix things up" on the pass that finds nothing
        # wrong. D-77 folded these rows as the GUARDING half only.
        after = fingerprint(tree)
        changed = sorted(k for k in before
                         if before[k] != after.get(k)) + sorted(
                             set(after) - set(before))
        check(not changed,
              "not one markdown file changed: %d checked%s"
              % (len(before),
                 "" if not changed else ", but %s did" % ", ".join(changed)))

        print("\n3. a false count planted in README.md is caught "
              "(HERON-DOC-RDM-007)")
        readme = os.path.join(tree, "README.md")
        kept = io.open(readme, encoding="utf-8").read()
        io.open(readme, "w", encoding="utf-8").write(
            kept + "\n\nOf these, 99 answered and 3 open.\n")
        code, out = run_guard(tree)
        check(code != 0, "the guard fails on a README that claims 99 answered")
        tail = out.split("THE SAME CLAIM", 1)[-1]
        check("README.md" in tail,
              "and names README.md as the file that said it")
        check("99" in tail, "quoting the number it did not believe")
        io.open(readme, "w", encoding="utf-8").write(kept)

        print("\n4. a broken reference planted in an architecture doc is "
              "caught (HERON-DOC-ARC-006)")
        arch = os.path.join(tree, ARCH)
        check(os.path.isfile(arch),
              "%s is there to plant one in" % ARCH.replace(os.sep, "/"))
        kept = io.open(arch, encoding="utf-8").read()
        io.open(arch, "w", encoding="utf-8").write(
            kept + "\n\nSee [the platform rules]"
                   "(#there-is-no-heading-with-this-name-anywhere).\n")
        code, out = run_guard(tree)
        check(code != 0,
              "the guard fails on an architecture doc with a dead anchor")
        head = out.split("2. GOLDEN RULES", 1)[0]
        check("06-heron-platform.md" in head,
              "and names the architecture doc, so the sweep reaches docs/ "
              "and not only the README")
        io.open(arch, "w", encoding="utf-8").write(kept)

        code, _out = run_guard(tree)
        check(code == 0,
              "and with both planted errors removed it passes again - so "
              "each finding was the plant and nothing else")
    finally:
        shutil.rmtree(home, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the guard sees what D-77 claims it sees")
    return 0


if __name__ == "__main__":
    sys.exit(main())
