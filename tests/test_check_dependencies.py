#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The dependency report's exit code, the size it does not type, and the version it checks.

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
    # The lock this suite reads is one it writes - an empty one unless a case
    # says otherwise - so a machine whose PyYAML is older than the real pin
    # does not turn section 1 red for being that machine.
    was_lock = getattr(tool, "LOCK", None)
    tool.LOCK = pathlib.Path(home) / "no-lock.txt"
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
        # R-77: the trained embedding announces its own size, in heron_embed.
        # (The re-ranker did too, until D-201 removed it.)
        said = tool.announced_size("model2vec")
        check(bool(said), "a size comes back for model2vec")
        if said:
            brain = os.path.join(ROOT, "brain")
            if brain not in sys.path:
                sys.path.insert(0, brain)
            import heron_embed                      # noqa: E402
            check(said in heron_embed.announcement(),
                  "read from heron_embed.announcement(), the code that owns "
                  "that download and prints it before the network is touched")
            check(said not in io.open(TOOL, encoding="utf-8").read(),
                  "and not typed in this tool")
        check(tool.announced_size("sentence_transformers") is None,
              "and the removed re-ranker's package is announced by nothing "
              "(D-201)")
        check(tool.announced_size("mcp") is None,
              "and nothing is invented for a package no code announces")

        # R-79: THE SYSTEM REQUIREMENTS PAGE SAYS THE SAME SIZES AS THE CODE
        # THAT DOWNLOADS. They are typed there because a person reads them
        # before installing anything - so this holds them to the code, and a
        # size changed in one place and not the other turns this red. The
        # page is docs/guide/system-requirements.md, linked from README.md.
        page = os.path.join(ROOT, "docs", "guide", "system-requirements.md")
        body = (io.open(page, encoding="utf-8").read()
                if os.path.isfile(page) else "")
        check("# System requirements" in body,
              "docs/guide/system-requirements.md exists (R-79)")
        readme = io.open(os.path.join(ROOT, "README.md"), encoding="utf-8").read()
        check("docs/guide/system-requirements.md" in readme,
              "and README.md links to it")
        import heron_embed as _embed                # noqa: E402
        import heron_ingest as _ingest              # noqa: E402
        for owner, size in (("heron_embed", getattr(_embed, "SIZE", None)),
                            ("heron_ingest",
                             getattr(_ingest, "PDF_READER_SIZE", None))):
            check(bool(size) and size in body,
                  "and it states %s.SIZE word for word: %r" % (owner, size))
        check("Minimum" in body and "not yet measured" in body,
              "and it gives the minimum, and says what is not yet measured")

        print()
        print("6. `installed` is importable; the VERSION is a second question (R-78)")
        here = tool.Package("PyYAML", "yaml", "reading yaml", "nothing runs")
        gone = tool.Package("nope", "no_such_module_xyz", "x", "y")
        check(here.installed is True, "a module that imports reads present")
        check(gone.installed is False, "and one that does not reads MISSING")

        # Every floor and pin here is written relative to nothing on this
        # machine: 999 is above any PyYAML, 0.0.1 below any. A suite that
        # asserted the installed version would be asserting the machine.
        def verdict(spec="", pins=None):
            try:
                package = tool.Package("PyYAML", "yaml", "reading yaml",
                                       "nothing runs", spec)
            except TypeError:
                # A Package that takes no specifier is the failure itself -
                # heron-ship 2a: fail, never crash.
                return None, "Package() takes no version specifier"
            got = getattr(package, "version_verdict", None)
            if got is None:
                return None, "no version_verdict at all"
            return got(pins or {})

        state, said = verdict(">=999")
        check(state == tool.OUT_OF_DATE if hasattr(tool, "OUT_OF_DATE") else False,
              "below the manifest's own floor reads NEEDS AN UPDATE: %r" % said)
        state, said = verdict(">=0.0.1")
        check(state == "ok", "above it reads ok: %r" % said)
        state, said = verdict(pins={"pyyaml": "999.0"})
        check(state == getattr(tool, "OUT_OF_DATE", object()),
              "older than the installer's pinned version reads NEEDS AN UPDATE")
        check("requirements-lock.txt" in (said or ""),
              "and names where the right version came from")
        state, said = verdict(pins={"pyyaml": "0.0.1"})
        check(state == "ok" and "not failed" in (said or ""),
              "NEWER than the pin is said and not failed - nothing measured it")
        state, said = verdict()
        check(state == "unchecked" and "only presence" in (said or ""),
              "with no floor and no pin it says it checked presence only, "
              "rather than passing a check that checked nothing")
        state, said = verdict("~=6.0")
        check(state == "unchecked" and "not applied" in (said or ""),
              "a specifier shape it does not read is said, never guessed at")

        release = getattr(tool, "release", None)
        if release is not None:
            check(release("6.0.3") == (6, 0, 3), "6.0.3 reads (6, 0, 3)")
            check(release("0.1.9a1") == (0, 1, 9),
                  "a pre-release suffix is cut off, not read as newer")
            check(release("abc") is None, "and a non-number is None")
        else:
            check(False, "the tool has release() at all")

        print()
        print("7. An out-of-date version exits 1; the manifest line carries the floor")
        code, spoke = run(required="# yaml | reading yaml | nothing runs\n"
                                   "PyYAML>=999\n")
        check(code == 1, "a required package below its floor exits 1, and it "
                         "exits %r" % code)
        check("NEEDS AN UPDATE" in (spoke or ""), "and says NEEDS AN UPDATE")
        check("pip install --user --upgrade PyYAML" in (spoke or ""),
              "and gives the line that updates it")
        code, spoke = run(required="# yaml | reading yaml | nothing runs\n"
                                   "PyYAML>=0.0.1\n")
        check(code == 0, "the same line with a floor it meets exits 0, and it "
                         "exits %r" % code)
        lock = pathlib.Path(home) / "lock.txt"
        lock.write_text("pyyaml==999.0 \\\n    --hash=sha256:00\n",
                        encoding="utf-8")
        tool.LOCK = lock
        code, spoke = run()
        check(code == 1 and "older than 999.0" in (spoke or ""),
              "a version older than the lock's pin exits 1 and names the pin")
    finally:
        tool.REQUIRED, tool.OPTIONAL = was
        tool.LOCK = was_lock
        shutil.rmtree(home, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - a missing REQUIRED package, a broken manifest and a")
    print("version older than the right one exit 1, and every size it prints")
    print("is read rather than typed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
