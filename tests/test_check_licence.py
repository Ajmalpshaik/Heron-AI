#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A German word counted as a licence, and a list you cannot finish reading.

    python tests/test_check_licence.py

`tools/check-licence.py` answers Q-53 (D-66) and is one of the gates a
pull request has to pass. Its own docstring states the rule it must never
break:

    A file with no marker at all is reported as UNMARKED rather than as
    clean - "no evidence of a problem" and "evidence of no problem" are
    different findings and a tool that merges them is the tool
    scientific-agent-skills already has.

TWO WAYS IT MERGED THEM, both measured 2026-09-22 on trees written for the
purpose:

  * `LICENCE_NAME` matched a bare `mit` case-insensitively, so an imported
    unit whose only occurrence was the German word - `gemessen mit dem
    Werkzeug` - came back 1 CLEAN. The same file without that word comes
    back UNMARKED, correctly;

  * an unmarked list longer than forty rows printed forty and said
    `..and 5 more (--all)`, and `--all` printed THE SAME FORTY. The remedy
    the gate names does not work, so five units cannot be seen at all.

NEITHER IS A LIVE FAILURE TODAY and the suite says so rather than
overstating it: all 406 units declare `source: OFFICIAL`, no file carries
the word `mit`, and the tool reports 406 clean. Both holes are in the branch
that the first IMPORTED unit lands on - which is the future docs/09's
COMMUNITY PACKAGES plans for, and the whole reason this tool exists.

EVERY CASE BUILDS ITS OWN TREE, with `tool.ROOT` pointed at a temp folder. A
suite that scanned `brain/` would assert today's library, which is what CI
already does.

WHAT IT CANNOT DO: it is not legal advice either, and it does not read
licence text for meaning. It asks only whether the gate sees what its own
docstring says it sees.

    python tests/test_check_licence.py
"""

import contextlib
import importlib.util
import io
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOL = os.path.join(ROOT, "tools", "check-licence.py")

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def load():
    """The tool as a module, or None - its name has a hyphen in it."""
    try:
        spec = importlib.util.spec_from_file_location("check_licence", TOOL)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        return module
    except BaseException:                          # noqa: BLE001
        return None


def main():
    print(__doc__.strip().splitlines()[0])
    print()

    tool = load()
    check(tool is not None, "tools/check-licence.py loads")
    runner = getattr(tool, "main", None) if tool else None
    check(callable(runner), "and it still has main()")
    check(getattr(tool, "ROOT", None) is not None,
          "and names the tree it walks, so a test can point it elsewhere")
    if not callable(runner) or getattr(tool, "ROOT", None) is None:
        print()
        print("FAILED")
        for line in FAILURES:
            print("  - %s" % line)
        return 1

    home = tempfile.mkdtemp(prefix="heron-licence-test-")
    was = tool.ROOT
    try:
        def run(files, argv=()):
            """(exit code, what it printed) against a tree built here."""
            place = tempfile.mkdtemp(dir=home)
            for rel, body in files.items():
                path = os.path.join(place, *rel.split("/"))
                if not os.path.isdir(os.path.dirname(path)):
                    os.makedirs(os.path.dirname(path))
                io.open(path, "w", encoding="utf-8",
                        newline="").write(body)
            tool.ROOT = place
            said = io.StringIO()
            try:
                with contextlib.redirect_stdout(said):
                    code = runner(list(argv))
            except BaseException as raised:         # noqa: BLE001
                code = "raised %s" % type(raised).__name__
            finally:
                tool.ROOT = was
            return code, said.getvalue()

        def counted(spoke, word):
            """The number the summary line states for one bucket."""
            for line in spoke.splitlines():
                if "unit(s) checked" in line:
                    for part in line.split(":")[1].split(","):
                        if word in part:
                            return int(part.strip().split()[0])
            return None

        print("1. The three findings it exists to make")
        code, spoke = run({"brain/skills/a.yaml":
                           "id: a\nsource: IMPORTED\n"
                           "# (c) 2025 Anthropic, PBC. All rights reserved.\n"})
        check(code == 1 and "RESERVES RIGHTS" in spoke,
              "a reservation of rights is a finding and exits 1")

        code, spoke = run({"brain/skills/b.yaml":
                           "id: b\nsource: IMPORTED\n# Licensed under GPL-3\n"})
        check(code == 1 and "not on the compatible list" in spoke,
              "a licence that is not redistributable under Apache 2.0 is a "
              "finding")

        code, spoke = run({"brain/skills/c.yaml":
                           "id: c\nsource: OFFICIAL\n"
                           "# Copyright 2025 Somebody Else Ltd\n"})
        check(code == 1 and "another copyright holder" in spoke,
              "a unit declaring OFFICIAL that names somebody else is a "
              "finding")

        print()
        print("2. UNMARKED is not CLEAN - the rule its docstring states")
        code, spoke = run({"brain/skills/d.yaml": "id: d\nname: no source\n"})
        check(counted(spoke, "unmarked") == 1 and counted(spoke, "clean") == 0,
              "a unit declaring no source at all is unmarked, not clean")

        code, spoke = run({"brain/skills/e.yaml":
                           "id: e\nsource: IMPORTED\nnote: nothing here\n"})
        check(counted(spoke, "unmarked") == 1,
              "and so is an imported unit with no licence anywhere")

        code, spoke = run({"brain/skills/f.yaml":
                           "id: f\nsource: IMPORTED\n"
                           "# MIT License, Copyright 2025 Somebody Ltd\n"})
        check(counted(spoke, "clean") == 1,
              "while an imported unit that names a compatible licence is "
              "clean")

        print()
        print("3. A GERMAN WORD IS NOT A LICENCE")
        # `mit` matched case-insensitively as a bare word, so any prose
        # containing it turned an unlicensed import into a clean one. Heron
        # reads standards, and a standard is not always in English.
        code, spoke = run({"brain/skills/g.yaml":
                           "id: g\nsource: IMPORTED\n"
                           "note: gemessen mit dem Werkzeug\n"})
        check(counted(spoke, "unmarked") == 1,
              "an imported unit whose only 'mit' is the German word is still "
              "unmarked, and it reads %r" % counted(spoke, "unmarked"))
        check(counted(spoke, "clean") == 0,
              "and it is not counted clean")

        # The licence itself must still be recognised, in both the forms a
        # file actually writes it.
        code, spoke = run({"brain/skills/h.yaml":
                           "id: h\nsource: IMPORTED\nlicence: MIT\n"})
        check(counted(spoke, "clean") == 1,
              "`licence: MIT` is still a licence")
        code, spoke = run({"brain/skills/i.yaml":
                           "id: i\nsource: IMPORTED\n# The MIT License\n"})
        check(counted(spoke, "clean") == 1,
              "and so is `The MIT License`")

        print()
        print("4. A LIST THAT STOPS SHORT NAMES A REMEDY THAT WORKS")
        many = dict(("brain/skills/s%02d.yaml" % n, "id: s%02d\n" % n)
                    for n in range(45))
        code, spoke = run(many)
        rows = sum(1 for l in spoke.splitlines() if l.startswith("  skill"))
        check(rows == 40 and "more" in spoke,
              "without --all it prints forty and says how many it cut")
        code, spoke = run(many, ["--all"])
        rows = sum(1 for l in spoke.splitlines() if l.startswith("  skill"))
        check(rows == 45,
              "and --all prints all forty-five, because that is what the "
              "message tells you to run - it printed %d" % rows)

        print()
        print("5. It does not cry wolf over C#")
        # The first version of the copyright pattern reported a cast -
        # `rule.GetCriterion(c) as PrimarySizeCriterion` - and a licence tool
        # that cries wolf is one somebody turns off.
        code, spoke = run({"brain/fragments/x/fragment.yaml":
                           "id: x\nsource: OFFICIAL\n",
                           "brain/fragments/x/Impl.cs":
                           "var size = rule.GetCriterion(c) as "
                           "PrimarySizeCriterion;\n"})
        check(counted(spoke, "clean") == 1 and code == 0,
              "a C# cast is not a copyright holder")

        code, spoke = run({"brain/fragments/y/fragment.yaml":
                           "id: y\nsource: OFFICIAL\n# Copyright 2026\n"})
        check(counted(spoke, "clean") == 1,
              "and a year with no name after it is a marker with no holder "
              "in it")
    finally:
        tool.ROOT = was
        shutil.rmtree(home, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - unmarked stays unmarked, and the remedy the gate names")
    print("is one that works.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
