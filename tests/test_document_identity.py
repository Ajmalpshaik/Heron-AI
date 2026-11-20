#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The add-in tells two open models apart by Revit's own identity, never by title.

    python tests/test_document_identity.py

WHY
---
Two models of one title can be open in one Revit at once - two copies of a
project, or two blank "Project1"s - and a title is then the same for both.
Row 5b-1164 took title-for-identity out of the chain; two places were left:

  THE BINDER (row 5b-1902)   `BindNeeds` in RevitFragment.cs read the
                             selection only when `active.Title ==
                             target.Title`, so the selection in one model
                             could be bound to a run in the other, its ids
                             looked up there by number.
  THE ANSWER (row 5b-1926)   `Report`'s "is the model read the one in front"
                             asked `active.Document.Title == title`, so a
                             model of the same title in front read as this
                             one, and the answer said nothing of the window
                             nobody was looking at.

`Document.Equals` is Revit's answer - "the same document currently opened in
the Revit session" - on every release 2020 to 2027, and `InDocument` and the
Keep question already ask it.

WHAT RUNS HERE AND WHAT CANNOT
------------------------------
The C# is read as TEXT, as the other add-in suites read it: no Revit here.
That two same-titled models really bind and report apart is the PC's:
NEEDS-CHECKING Group OW.

MEASURED AGAINST THE CODE AS IT STOOD: every check fails, no traceback.
"""

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ADDIN = os.path.join(ROOT, "revit", "Heron.Revit.Addin")
FRAGMENT = os.path.join(ADDIN, "RevitFragment.cs")
FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def between(text, start, end, limit=8000):
    """The text from `start` to `end`, or "" when `start` is not there."""
    at = text.find(start)
    if at < 0:
        return ""
    stop = text.find(end, at + len(start))
    return text[at:stop if stop > at else at + limit]


def code_lines(text):
    """Every line that is not a comment - the way a grep of the C# reads it."""
    return [line for line in text.splitlines() if not line.strip().startswith("//")]


def binder(source):
    print("\n1. the binder takes the selection only from the target model itself (5b-1902)")
    block = between(source, "// The selection, read ONCE.", "HOW MANY NEEDS COULD THE SELECTION")
    check(block != "", "the selection block is found in BindNeeds")
    check("active.Equals(target)" in block,
          "the selection is read when the model in front IS the target - Document.Equals")
    check("Title" not in "\n".join(code_lines(block)),
          "and no title is compared there")


def answer(source):
    print("\n2. the answer says 'in front' only of the target model itself (5b-1926)")
    block = between(source, "// WHETHER THE MODEL READ IS THE ONE ON SCREEN.", "var parts =")
    check(block != "", "the in-front block is found in Report")
    check("active.Document.Equals(target)" in block,
          "in front means the model in front IS the target - Document.Equals")
    check("== title" not in block and "Title ==" not in block,
          "and no title is compared there")


def nowhere(paths):
    print("\n3. no add-in file compares two models by title")
    found = []
    pattern = re.compile(r"\.Title\s*==|==\s*[A-Za-z_][A-Za-z_0-9.]*\.Title\b")
    for path in paths:
        text = io.open(path, encoding="utf-8").read()
        for number, line in enumerate(text.splitlines(), 1):
            if line.strip().startswith("//"):
                continue
            if pattern.search(line):
                found.append("%s:%d" % (os.path.basename(path), number))
    check(not found, "no `.Title ==` between two documents%s"
          % ("" if not found else " - found at " + ", ".join(found)))


def main():
    source = io.open(FRAGMENT, encoding="utf-8").read()
    binder(source)
    answer(source)
    nowhere(sorted(os.path.join(ADDIN, name) for name in os.listdir(ADDIN)
                   if name.endswith(".cs")))
    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the add-in tells two open models apart by Revit's identity, not by title")
    return 0


if __name__ == "__main__":
    sys.exit(main())
