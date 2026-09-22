#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The dependency report's exit code, and the size it does not type.

    python tests/test_check_dependencies.py

THE FIRST SUITE THIS TOOL HAS EVER HAD. Measured 2026-09-22: `tools/` holds
47 tools and SIX of them are named by no suite at all - measure-graph,
check-dependencies, module-reach, generate-decision-summary,
resign-machine-proofs and recount-agent-registry. This is one of them, and
it was picked because R-72 makes four specific claims about it and nothing
anywhere re-checks them:

    It exits 1 only on a missing REQUIRED package - failing on an absent
    optional one would be arguing with R-42. It also fails on a malformed
    manifest entry, proved by introducing both shapes and watching it exit
    1. Size is never typed: where the code that downloads owns the figure,
    it is read from there.

"Proved by introducing both shapes" was done once, by hand, on 2026-09-11.
That is a measurement, not a guard - the tool can drift away from it on any
afternoon and nothing says so. This is the guard.

EVERY CASE BUILDS ITS OWN MANIFESTS. A suite that read the real
requirements.txt would be asserting what happens to be installed on the
machine running it, which is a different question and one that changes
without anybody editing anything.

WHAT IT CANNOT DO: it does not prove the manifests are RIGHT - that the
import name beside a pip name is the one the package actually installs.
Nothing here can, and `check-dependencies` trusts them by design.

    python tests/test_check_dependencies.py
"""

import contextlib
import importlib.util
import io
import os
import pathlib
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOL = os.path.join(ROOT, "tools", "check-dependencies.py")

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def load():
    """The tool as a module, or None - its name is not importable."""
    try:
        spec = importlib.util.spec_from_file_location("check_dependencies",
                                                      TOOL)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        return module
    except BaseException:                          # noqa: BLE001
        return None


# One well-formed manifest of each kind, used wherever the case under test
# is about the OTHER file.
GOOD_REQUIRED = "# yaml | reading yaml | nothing in brain/ runs\nPyYAML\n"
GOOD_OPTIONAL = ("# model2vec | the trained encoder | "
                 "retrieval falls back to character n-grams\nmodel2vec\n")

# A PACKAGE THAT CANNOT BE INSTALLED, so the missing-optional path is
# reachable on EVERY machine - fixed 2026-09-22.
#
# Section 1 used GOOD_OPTIONAL, which names model2vec, and then asserted that
# the report says it is absent. On a machine where model2vec IS installed -
# the owner's PC - check-dependencies correctly reported it present and said
# nothing about installing it, and three checks failed the tool for being
# RIGHT. The same shape as section 6 of tests/test_api_digest.py: a suite that
# asserts against whatever machine it happens to find is red on exactly the
# machines best equipped to run it, and green elsewhere only by luck.
#
# Section 2 already had the answer sitting beside it - it names
# no_such_module_xyz to reach the missing-REQUIRED path. This is that same
# trick for the optional half, and the third field is kept word for word
# because what is lost is what the check below reads.
ABSENT_OPTIONAL = ("# heron_no_such_encoder | the trained encoder | "
                   "retrieval falls back to character n-grams\n"
                   "heron-no-such-encoder\n")


def main():
    print(__doc__.strip().splitlines()[0])
    print()

    # ASK BEFORE CALLING - heron-ship section 2a. A tool that has been
    # renamed or that will not load is ONE clean failure, never a traceback
    # standing in for every check below it.
    tool = load()
    check(tool is not None, "tools/check-dependencies.py loads")
    if tool is None:
        print()
        print("FAILED")
        for line in FAILURES:
            print("  - %s" % line)
        return 1

    home = tempfile.mkdtemp(prefix="heron-dep-test-")
    was = (tool.REQUIRED, tool.OPTIONAL)
    try:
        def manifest(name, text):
            path = pathlib.Path(home) / name
            path.write_text(text, encoding="utf-8")
            return path

        def run(required=GOOD_REQUIRED, optional=GOOD_OPTIONAL):
            """(exit code, what it printed), on manifests this suite wrote."""
            tool.REQUIRED = (manifest("req.txt", required)
                             if required is not None
                             else pathlib.Path(home) / "not-there.txt")
            tool.OPTIONAL = manifest("opt.txt", optional)
            said = io.StringIO()
            try:
                with contextlib.redirect_stdout(said):
                    code = tool.main()
            except BaseException as raised:         # noqa: BLE001
                return None, "raised %s" % type(raised).__name__
            return code, said.getvalue()

        print("1. A missing OPTIONAL package is the normal case (R-42)")
        code, spoke = run(optional=ABSENT_OPTIONAL)
        check(code == 0,
              "an absent optional package exits 0, and it exits %r" % code)
        check("not a fault" in spoke,
              "and the report says so rather than only being quiet")
        check("pip install --user heron-no-such-encoder" in spoke,
              "and gives the line that installs it")
        check("retrieval falls back to character n-grams" in spoke,
              "and says what is lost without it - which is the whole "
              "reason the manifest carries that field")

        print()
        print("2. A missing REQUIRED package is the one thing that exits 1")
        code, spoke = run(required="# no_such_module_xyz | a | b\n"
                                   "not-a-real-package\n")
        check(code == 1, "a missing required package exits 1, and it exits %r" % code)
        check("Heron will not run" in spoke, "and says Heron will not run")

        print()
        print("3. A malformed manifest entry fails too, in every shape")
        # R-72 says "proved by introducing BOTH shapes". There are more than
        # two, and each one is a different way the single source of truth
        # stops being one.
        for label, required in (
                ("a requirement with no comment above it", "PyYAML\n"),
                ("a comment with two fields", "# yaml | reading yaml\nPyYAML\n"),
                ("a comment with four fields", "# yaml | a | b | c\nPyYAML\n"),
                ("a blank line between the two",
                 "# yaml | reading yaml | nothing runs\n\nPyYAML\n")):
            code, spoke = run(required=required)
            check(code == 1, "%s exits 1, and it exits %r" % (label, code))
            check("THE MANIFEST ITSELF HAS A PROBLEM" in spoke,
                  "and names the manifest rather than the package")

        print()
        print("4. A manifest that is not there is a problem, not an empty list")
        code, spoke = run(required=None)
        check(code == 1, "a missing manifest file exits 1")
        check("does not exist" in spoke,
              "and says so - an absent list is not a system with no "
              "dependencies")

        print()
        print("5. The size is READ from the code that downloads, never typed")
        stated = tool.announced_size("sentence_transformers")
        check(bool(stated), "a size comes back for sentence-transformers")
        if stated:
            source = io.open(TOOL, encoding="utf-8").read()
            check(stated not in source,
                  "and it is NOT in this tool's source: %r" % stated[:24])
            brain = os.path.join(ROOT, "brain")
            if brain not in sys.path:
                sys.path.insert(0, brain)
            import heron_rerank                     # noqa: E402
            check(stated in heron_rerank.announcement(),
                  "it is in heron_rerank.announcement(), which is the code "
                  "that owns the download and prints it before the network "
                  "is touched")
        check(tool.announced_size("model2vec") is None,
              "and nothing is invented for a package no code announces")

        print()
        print("6. `installed` means importable and nothing stronger (R-78)")
        here = tool.Package("PyYAML", "yaml", "reading yaml", "nothing runs")
        gone = tool.Package("nope", "no_such_module_xyz", "x", "y")
        check(here.installed is True, "a module that imports reads present")
        check(gone.installed is False, "and one that does not reads MISSING")
    finally:
        tool.REQUIRED, tool.OPTIONAL = was
        shutil.rmtree(home, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - only a missing REQUIRED package and a broken manifest")
    print("exit 1, and the one size it prints is read rather than typed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
