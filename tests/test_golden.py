#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The Golden Test Library runner. Runs without Revit - because it does not run the
cases, it audits them.

The cases themselves can only be proven by a person watching Revit. What this
does is three things no person reliably does by hand:

1. **Checks the library is coherent** - no duplicate ids, no case claiming PROVEN
   with no date, no case depending on a file that no longer exists.
2. **Recomputes each proof's fingerprint** and reports the ones that have gone
   STALE - proven against code that has since changed.
3. **Says what is proven and what is not**, in one number, so "is Heron working?"
   has an answer that is not somebody's memory.

WHY 2 MATTERS. On 2026-08-28 Steps 1, 4 and 5 stopped being proven, because
Step 6 modified six files their proofs rested on. That was found by reading a
diff. This finds it by running.

    python tests/test_golden.py
"""

import hashlib
import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "tests", "golden"))

from cases import CASES, PROVEN, PENDING, FAILED        # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def fingerprint(paths):
    """
    One hash over the exact bytes of every file a proof rested on.

    Content, not timestamps: a file touched but unchanged must not invalidate a
    proof, and a file changed back to what it was must not either. Sorted, so
    the order the case happens to list them in cannot change the answer.
    """
    digest = hashlib.sha256()
    for path in sorted(paths):
        full = os.path.join(ROOT, path)
        if not os.path.exists(full):
            return None
        digest.update(path.encode("utf-8"))
        digest.update(io.open(full, "rb").read())
    return digest.hexdigest()[:16]


def main():
    print("The library is coherent")
    ids = [c["id"] for c in CASES]
    check(len(ids) == len(set(ids)), "no duplicate case ids")

    for c in CASES:
        missing = [p for p in c["depends"] if not os.path.exists(os.path.join(ROOT, p))]
        check(not missing,
              "%s depends only on files that exist%s"
              % (c["id"], "" if not missing else "  <- MISSING: " + ", ".join(missing)))

    for c in CASES:
        if c["status"] == PROVEN:
            check(bool(c["proven_on"]), "%s is PROVEN and carries a date" % c["id"])
            check(bool(c["revit"]), "%s is PROVEN and names the Revit version(s)" % c["id"])
        else:
            check(not c["proven_on"],
                  "%s is not PROVEN, so it carries no proof date" % c["id"])

    print()
    print("Which proofs still stand against the CURRENT code")
    stale, standing = [], []
    for c in CASES:
        if c["status"] != PROVEN:
            continue
        now = fingerprint(c["depends"])
        if c["fingerprint"] is None:
            stale.append((c, "proven against an unknown build"))
        elif c["fingerprint"] != now:
            stale.append((c, "the code it rested on has changed"))
        else:
            standing.append(c)

    for c, why in stale:
        print("  STALE %-6s %-45s %s" % (c["id"], c["capability"], why))
    for c in standing:
        print("  ok    %-6s %-45s proven %s" % (c["id"], c["capability"], c["proven_on"]))

    print()
    print("Where Heron actually stands")
    proven = [c for c in CASES if c["status"] == PROVEN]
    pending = [c for c in CASES if c["status"] == PENDING]
    failed = [c for c in CASES if c["status"] == FAILED]

    print("  %d cases" % len(CASES))
    print("  %d recorded as proven, of which %d STALE and %d still standing"
          % (len(proven), len(stale), len(standing)))
    print("  %d never run against Revit" % len(pending))
    if failed:
        print("  %d FAILED when last run" % len(failed))

    check(not failed, "no case is currently recorded as FAILED")

    print()
    if stale:
        print("  %d proof(s) need re-taking. That is not a fault - it is what happens" % len(stale))
        print("  when code moves under a proof, and the point of recording it.")
        print("  Re-prove them, then stamp the fingerprint with --stamp.")
    if pending:
        print("  %d case(s) have never met Revit. See NEEDS-CHECKING.md." % len(pending))

    print()
    if FAILURES:
        print("FAILED")
        for f in FAILURES:
            print("  - %s" % f)
        return 1

    print("PASSED - the library is coherent. It says nothing about whether Heron works;")
    print("that is what the STALE and never-run counts above are for.")
    return 0


def stamp():
    """
    Rewrite each PROVEN case's fingerprint to the current code.

    Run this ONLY straight after re-proving a case in Revit. Running it without
    re-proving turns a stale proof into a false one, which is worse than having
    no library at all - it would report green over code nobody has ever run.
    """
    path = os.path.join(ROOT, "tests", "golden", "cases.py")
    text = io.open(path, encoding="utf-8").read()
    changed = 0
    for c in CASES:
        if c["status"] != PROVEN:
            continue
        now = fingerprint(c["depends"])
        if now and c["fingerprint"] != now:
            print("  %s  %s -> %s" % (c["id"], c["fingerprint"], now))
            changed += 1
    print()
    print("  %d case(s) would be stamped." % changed)
    print("  Editing is deliberate and manual: set fingerprint on each case above.")
    print("  Nothing was written - a fingerprint is a claim that somebody watched it work.")
    return 0


if __name__ == "__main__":
    sys.exit(stamp() if "--stamp" in sys.argv else main())
