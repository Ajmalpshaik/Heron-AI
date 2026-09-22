# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-RAG-RNK-006
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The routing checker's own command line - the two ways it could not answer.

    python tests/test_check_routing.py

WHY THIS EXISTS
---------------
`tools/check-routing.py` is one of the twelve runs `.github/workflows/gates.yml`
makes, and it had no suite at all. Both things checked here are cases where the
tool is supposed to REFUSE, and a refusal is the one behaviour nobody notices
is broken: the happy path is exercised on every pull request, and neither of
these is.

WHAT IT PROVES
  1. `--revit` WITH NO VALUE IS A REFUSAL, NOT A TRACEBACK. Row 5b-104. The
     flag's value was read as `argv[i + 1]` with no guard, so `--revit` last on
     the line came back as `IndexError: list index out of range` and exit 1 -
     four lines above a comment about not answering with a traceback.

  2. A FLAG STANDING WHERE A RELEASE SHOULD BE IS REFUSED TOO. Otherwise the
     library is filtered to the Revit release called `--rebuild`, and an empty
     result is printed as a routing measurement.

  3. NO KNOWLEDGE STORE IS EXIT 2 AND A SENTENCE. Row 5b-71: until 2026-09-21
     this raised `ValueError` from four frames down while `gates.yml` claimed
     the tool would "say so rather than failing when there is none". That was
     fixed and nothing held it.

WHAT IT DOES NOT PROVE
  Anything about the routing result itself. This checker is a REPORT - it exits
  0 whatever collisions it finds, because a collision is a judgement and not a
  defect - so there is no verdict here to test. What is testable is the two
  cases where it declines to produce one at all.
"""

from __future__ import print_function

import importlib.util
import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

spec = importlib.util.spec_from_file_location(
    "heron_check_routing", os.path.join(ROOT, "tools", "check-routing.py"))
CR = importlib.util.module_from_spec(spec)
spec.loader.exec_module(CR)

FAILURES = []


def check(ok, said):
    print("  %s  %s" % ("ok  " if ok else "FAIL", said))
    if not ok:
        FAILURES.append(said)


class _Captured(object):
    """stderr, held so a refusal's WORDS can be checked and not only its code."""

    def __init__(self):
        self.said = io.StringIO()
        self.was = None

    def __enter__(self):
        self.was = sys.stderr
        sys.stderr = self.said
        return self

    def __exit__(self, *_):
        sys.stderr = self.was
        return False

    def text(self):
        return self.said.getvalue()


def main():
    print("1. --revit WITH NO VALUE IS REFUSED, NOT A TRACEBACK")
    # RAISING IS THE FAILURE, so it is caught and recorded as one rather than
    # ending the suite. A check that errors has proved nothing
    # (.claude/skills/heron-ship/SKILL.md s2a).
    for argv, what in (
            (["--revit"], "--revit last on the line"),
            (["--revit", "--rebuild"], "a flag standing where a release should be")):
        code = None
        try:
            with _Captured() as out:
                code = CR.main(list(argv))
        except BaseException as raised:          # noqa: BLE001 - that IS the check
            check(False, "%s is refused rather than raising %s"
                         % (what, type(raised).__name__))
            continue
        check(code == 2,
              "%s exits 2 - the code this repository uses for 'the tool could "
              "not do its job', so nothing reads as a routing result" % what)
        check("--revit" in out.text(),
              "and the refusal NAMES the flag and the shape of a value, "
              "rather than printing a stack")
    print()

    print("2. IT REFUSES BEFORE IT OPENS ANYTHING")
    # The guard has to sit above the imports, or a typo still pays for
    # heron_scope, heron_search, heron_embed and heron_retrieve first.
    source = io.open(os.path.join(ROOT, "tools", "check-routing.py"),
                     encoding="utf-8").read()
    body = source.split("\ndef main(", 1)[1]
    guard = body.find("--revit needs a release")
    imports = body.find("import heron_scope")
    check(guard != -1 and imports != -1 and guard < imports,
          "the flag refusal comes before the brain imports, so a typo costs "
          "nothing")
    print()

    print("3. NO KNOWLEDGE STORE IS EXIT 2 AND A SENTENCE, NOT A TRACEBACK")
    import heron_scope as SCOPE
    was = os.environ.pop("HERON_KNOWLEDGE", None)
    try:
        if SCOPE.knowledge_dir() is not None:
            # A MACHINE WITH %APPDATA% HAS SOMEWHERE TO KEEP KNOWLEDGE, so
            # this case cannot be arranged here and is NOT reported as a pass.
            # Row 5b-71 is about the Linux runner, which is where it bit.
            print("  NOT RUN  this machine has a knowledge folder without "
                  "HERON_KNOWLEDGE (%APPDATA%), so the refusal cannot be "
                  "arranged - it is not a pass")
        else:
            code = None
            try:
                with _Captured() as out:
                    code = CR.main([])
            except BaseException as raised:      # noqa: BLE001 - that IS the check
                check(False, "no knowledge store is refused rather than "
                             "raising %s from four frames down"
                             % type(raised).__name__)
            if code is not None:
                check(code == 2,
                      "no knowledge store exits 2 - NOT 0, because nothing "
                      "was checked and a green run would be a lie, and NOT 1, "
                      "because nothing failed either")
                check("COULD NOT RUN" in out.text()
                      and "HERON_KNOWLEDGE" in out.text(),
                      "and it says so in words, naming the variable that "
                      "fixes it - gates.yml has claimed this since the day "
                      "the tool was wired in")
    finally:
        if was is not None:
            os.environ["HERON_KNOWLEDGE"] = was
    print()

    print("4. THE RISK LADDER IS THE REGISTER'S, AND AN UNKNOWN LEVEL IS -1")
    check(CR.rung("READ") < CR.rung("MODIFY") < CR.rung("ADMIN"),
          "READ sits below MODIFY sits below ADMIN - the crossing this tool "
          "separates from an ordinary collision is a question answered by a "
          "write, and that only means anything if the order is right")
    check(CR.rung("nonsense") == -1,
          "and a level nobody declared is -1 rather than an exception or a "
          "quiet zero, so it can never out-rank READ")
    print()

    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - the routing checker refuses a typo and a missing store by")
    print("name, and it refuses before it loads anything.")
    print()
    print("It proves NOTHING about the routing result. That is a report and a")
    print("finding in it is a question for a person, so there is no verdict")
    print("here to test - only the two cases where it declines to give one.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
