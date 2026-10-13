#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   14
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A category typed by the name Revit shows reaches THAT category. Runs without Revit.

    python tests/test_category_by_name.py

WHY THIS EXISTS
---------------
FRAGMENT-ISSUES row 5b-444. On 2026-10-13, Project2 on Revit 2020.2, two
railings of type "Glass Panel - Bottom Fill" stood on Level 2 and Level 3.
SELECT_BY_CATEGORY_NAME categoryName=Railings found both. FILTER_ELEMENTS_BY_CATEGORY
category=Railings answered matching 0, and SELECT_IN_REGION categories=Railings
over a box holding one answered 0 too.

Both of those declare a `BuiltInCategory` (one, or a list), and the add-in's
binder turns the typed word into one in `OneBuiltInCategory`
(revit/Heron.Revit.Addin/RevitFragment.cs). It tried "OST_" + the word BEFORE
the name Visibility/Graphics shows, and `OST_Railings` is a real member of the
enum - value -2000175, read from the reference assemblies of 2020 and 2024 -
while the category that shows as Railings is `OST_StairsRailing`, -2000126. So
the guess bound a category no railing is in, and the zero was plausible.
SELECT_BY_CATEGORY_NAME matched the name and collected by the category's own
id, which is why it alone was right.

WHAT IT CHECKS, IN THE ONE PLACE A COMPILER CANNOT
--------------------------------------------------
`OneBuiltInCategory` touches Autodesk types, so no test host here can link it
(test_binding_note.py gives the reason at length). This reads its source as
text, the technique test_binding_note.py's call-site checks already use:

  1. the API name exactly as typed - OST_DuctCurves - is tried first;
  2. then the name Revit shows, through `OneCategory`, the same lookup the
     `Category` needs use;
  3. that category is turned into an enum value BY ITS ID - its `Id` read as
     text, the way the binder reads typed ids, and compared with each
     candidate's number, with no ElementId built (row 5b-441's rule) - and
     not by comparing another value's category name, which can stop at a
     second value answering to the same words;
  4. only then the "OST_" + word guess;
  5. and both shapes the two fragments declare - one category and a list -
     still go through it.

Every position is checked for -1 before it is compared: `str.find` returns -1
for an absent string, and a bare `a < b` passes loudest when the step it
orders has been deleted (heron-ship 2a).

WHAT IT CANNOT DO: say that Revit finds the railings. That needs a model -
FRAGMENT-ISSUES 5b-444 names the run that is owed.
"""

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CALL_SITE = os.path.join(ROOT, "revit", "Heron.Revit.Addin", "RevitFragment.cs")
FRAGMENTS = os.path.join(ROOT, "brain", "fragments")

FAILED = []


def check(ok, said):
    print("  %s  %s" % ("ok  " if ok else "FAIL", said))
    if not ok:
        FAILED.append(said)


def body_of(text, signature):
    """The text of one method, from its signature to its closing brace.

    Counted by braces from the first one after the signature. None when the
    signature is not there, so its absence is ONE clean failure rather than a
    traceback that ends the suite.
    """
    start = text.find(signature)
    if start < 0:
        return None
    opened = text.find("{", start)
    if opened < 0:
        return None
    depth = 0
    for at in range(opened, len(text)):
        if text[at] == "{":
            depth += 1
        elif text[at] == "}":
            depth -= 1
            if depth == 0:
                return text[start:at + 1]
    return None


def code_only(text):
    """The method without its comments, so a sentence cannot satisfy a check."""
    text = re.sub(r"//[^\n]*", "", text)
    return re.sub(r"/\*.*?\*/", "", text, flags=re.S)


def declares(folder, need_type):
    """Does this fragment's contract declare a request need of this type?"""
    path = os.path.join(FRAGMENTS, folder, "fragment.yaml")
    try:
        text = io.open(path, encoding="utf-8").read()
    except (IOError, OSError):
        return False
    return re.search(r"^\s*type:\s*%s\s*$" % re.escape(need_type), text, re.M) is not None


def main():
    try:
        text = io.open(CALL_SITE, encoding="utf-8").read()
    except (IOError, OSError) as exc:
        print("FAILED - could not read %s (%s)" % (CALL_SITE, exc))
        return 1

    print("The binder's category-by-name rule, read from RevitFragment.cs")

    found = body_of(text, "private static bool OneBuiltInCategory(")
    check(found is not None, "OneBuiltInCategory is there to read")
    code = code_only(found or "")

    exact = code.find("Enum.Parse(typeof(BuiltInCategory), trimmed, true)")
    shown = code.find("OneCategory(doc, trimmed)")
    guess = code.find('"OST_" + trimmed')

    check(exact >= 0, "the API name exactly as typed is parsed on its own, apart "
                      "from the guess (at %d)" % exact)
    check(shown >= 0, "the name Revit shows is looked up through OneCategory "
                      "(at %d)" % shown)
    check(guess >= 0, "the \"OST_\" + word guess is still there for DuctCurves "
                      "(at %d)" % guess)
    check(exact >= 0 and shown >= 0 and exact < shown,
          "the exact API name is tried BEFORE the shown name")
    check(shown >= 0 and guess >= 0 and shown < guess,
          "the shown name is tried BEFORE the \"OST_\" guess - row 5b-444: "
          "\"Railings\" guessed OST_Railings, which holds no railing")

    reads_id = re.search(r"\w+\.Id\.ToString\(\)", code) is not None
    compares = re.search(r"Convert\.ToInt64\(\s*candidate\b[^)]*\)\s*==\s*\w+", code) is not None
    check(reads_id and compares,
          "the shown category becomes an enum value BY ITS ID - the category's "
          "Id read as text and compared with each candidate's number")
    by_name = re.search(r"GetCategory\(\s*doc\s*,\s*candidate\s*\)", code) is not None
    check(not by_name,
          "and not by asking each candidate for its own category's name")
    # The binder's own rule, 5b-441: no ElementId is built in it at all, so the
    # 2024 move to 64-bit ids is never crossed. test_binding_note.py holds it
    # for the whole binder; this says it for this method in its own words.
    check("new ElementId(" not in code,
          "and no ElementId is built to do it")

    print()
    print("Both fragments of row 5b-444 still reach it")
    single = re.search(r'wanted == "BuiltInCategory"\)\s*\{\s*BuiltInCategory one;\s*'
                       r'if \(OneBuiltInCategory\(doc, text, out one\)\)', text)
    check(single is not None, "a BuiltInCategory need binds through OneBuiltInCategory")
    many = re.search(r'wanted == "IList<BuiltInCategory>".{0,400}?'
                     r'OneBuiltInCategory\(doc, part, out one\)', text, re.S)
    check(many is not None, "and so does each name in an IList<BuiltInCategory>")
    check(declares("filter-elements-by-category", "BuiltInCategory"),
          "filter-elements-by-category declares a BuiltInCategory")
    check(declares("select-in-region", "IList<BuiltInCategory>"),
          "select-in-region declares an IList<BuiltInCategory>")

    print()
    if FAILED:
        print("FAILED - %d check(s)." % len(FAILED))
        return 1
    print("PASSED.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
