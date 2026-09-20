# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The mark that withdraws itself.

    python tests/test_review_ledger.py

WHY THIS EXISTS
---------------
tests/test_catalog.py states the rule this repository works to: a tool that
only DRAWS needs no test, one that CONCLUDES does. `tools/review-ledger.py`
concludes two things a session acts on without re-deriving them - **this file
has already been read, skip it** and **this mark no longer applies, read it
again** - and the first of those, if it is ever wrong, makes a session skip a
file nobody has looked at while reporting that the sweep is complete.

WHAT IT PROVES
  1. A MARK HOLDS WHILE THE FILE DOES NOT CHANGE. Without this leg the tool
     could pass check 2 by calling everything stale, which would be a sweep
     that never finishes and never says so.

  2. AND GOES STALE THE MOMENT IT DOES. This is the whole design. A bare
     "checked" tick is a cache with no invalidation - the failure
     `tools/open-defects.py` names about prose totals, one layer down. The
     hash in the row IS the invalidation, and nothing has to remember to
     withdraw a mark for it to stop counting.

  3. APPEND-ONLY: THE NEWEST ROW FOR A PATH IS THE STATE. The ledger is never
     rewritten, so a file read clean, edited, then found wrong carries three
     rows and the last one is the answer. If an older row could win, a fixed
     file would go on reporting its old defect for ever.

  4. SCOPE IS BY EXTENSION UNDER brain/, NOT BY THE FOLDER. `brain/**.yaml`
     keeps its own gates (the owner's decision, 2026-09-20), but the fragment
     C# beside it is live code that is sent on every call, and excluding the
     whole of brain/ would have quietly dropped every one of them out of
     the sweep. Check 4 derives that count rather than stating it here.

  5. A FILE NOBODY HAS OPENED IS NOT CLEAN. Three states, not two, and the
     difference between "read and fine" and "never looked at" is the only
     reason this ledger exists.

THE LEDGER IS REDIRECTED TO A TEMPORARY FILE, ON PURPOSE AND ON C:
------------------------------------------------------------------
Every check below points `RL.LEDGER` at a scratch file so the real
`docs/REVIEW-LEDGER.tsv` is never written by a test run. On this owner's
machine that scratch file lands on **C:** while the repository is on **D:**,
which is the two-drive arrangement that has already broken one tool here -
`os.path.relpath` RAISES across drives on Windows. So this suite also happens
to be the place that would catch a relative path creeping into the ledger
code, and the note is here so the next reader knows that is not an accident.
"""

import importlib.util
import io
import os
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

spec = importlib.util.spec_from_file_location(
    "heron_review_ledger", os.path.join(ROOT, "tools", "review-ledger.py"))
RL = importlib.util.module_from_spec(spec)
spec.loader.exec_module(RL)

FAILURES = []

# A real tracked file, named rather than picked, so that if it is ever deleted
# this suite says which file went missing instead of failing somewhere vague.
SUBJECT = "tools/check-docs.py"

NOT_ITS_HASH = "0" * 40


def check(ok, said):
    print("  %s  %s" % ("ok  " if ok else "FAIL", said))
    if not ok:
        FAILURES.append(said)


def write_ledger(rows):
    with io.open(RL.LEDGER, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(u"\t".join(RL.COLUMNS) + u"\n")
        for row in rows:
            handle.write(u"\t".join(row) + u"\n")


def row(path, blob, verdict="clean", note="", when="2026-09-20", who="test"):
    return [when, who, path, blob, verdict, note]


def main():
    scratch = tempfile.mkdtemp(prefix="heron-ledger-")
    RL.LEDGER = os.path.join(scratch, "REVIEW-LEDGER.tsv")
    print("ledger redirected to %s\n" % RL.LEDGER)

    scope = RL.in_scope()
    if SUBJECT not in scope:
        print("  FAIL  %s is not tracked any more. This suite names one real\n"
              "        file on purpose; point SUBJECT at another and say so."
              % SUBJECT)
        return 1

    real = RL.blobs([SUBJECT])[SUBJECT]

    print("1. a mark holds while the file does not change")
    write_ledger([row(SUBJECT, real)])
    st = RL.state()
    check(st is not None, "the balance could be derived at all")
    check(SUBJECT in st["clean"], "%s reads as clean at its own hash" % SUBJECT)
    check(SUBJECT not in st["stale"], "and is not called stale while it stands")

    print("\n2. and goes stale the moment it does - the whole design")
    # The same row, the same file, one thing different: the hash no longer
    # describes what is on disk. Nothing else changes, and nothing is asked
    # to notice - the mark has to withdraw itself.
    write_ledger([row(SUBJECT, NOT_ITS_HASH)])
    st = RL.state()
    check(SUBJECT in st["stale"], "a hash that no longer matches is STALE")
    check(SUBJECT not in st["clean"],
          "and the old verdict stops counting as read")

    print("\n3. append-only: the newest row for a path is the state")
    write_ledger([row(SUBJECT, NOT_ITS_HASH, "clean"),
                  row(SUBJECT, real, "issue", "5b-1", when="2026-09-21")])
    st = RL.state()
    found = dict((p, r) for p, r in st["found"])
    check(SUBJECT in found, "the later row wins over the earlier one")
    check(found.get(SUBJECT, {}).get("note") == "5b-1",
          "and carries its 5b row, so the defect can be found")
    check(SUBJECT not in st["stale"],
          "the stale earlier row does not outvote the current one")

    print("\n4. scope is by extension under brain/, not by the folder")
    yaml_in_brain = [p for p in scope
                     if p.startswith("brain/") and p.endswith(".yaml")]
    code_in_brain = [p for p in scope
                     if p.startswith("brain/") and p.endswith(".cs")]
    check(not yaml_in_brain,
          "brain/**.yaml is out - it keeps its own gates")
    check(bool(code_in_brain),
          "brain/**.cs is IN - %d fragment implementations are live code"
          % len(code_in_brain))

    print("\n5. a file nobody has opened is not clean")
    write_ledger([])
    st = RL.state()
    check(SUBJECT in st["unchecked"], "no row means never opened")
    check(SUBJECT not in st["clean"] and SUBJECT not in st["stale"],
          "which is a third state, not a quiet pass")

    try:
        os.remove(RL.LEDGER)
        os.rmdir(scratch)
    except OSError:
        pass

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    a mark carries the hash of what was read, and stops\n"
          "        counting by itself the moment that stops being true")
    return 0


if __name__ == "__main__":
    sys.exit(main())
