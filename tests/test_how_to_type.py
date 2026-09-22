#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   14
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Every value a caller types anywhere in the library has a hint, and no hint
offers a way to type something Revit refuses. Needs no Revit.

    python tests/test_how_to_type.py

WHY. `how_to_type` is the one copy of "what to write in this blank" - the
server prints it beside every value heron_resolve says a caller supplies, and
tools/generate-jobs.py prints it in every job file. Measured on 2026-09-22
across all 765 caller-supplied values, 58 distinct types:

  - 14 types got NO hint, including `double` (102 values) and `int` (27),
    where a modeller types "250mm" and FromRequest refuses it.
  - FOUR got a WRONG one. `IList<Reference>`, `IList<Color>` and both
    request-sourced dictionaries matched the generic list rule and were told
    "comma separated". Revit refuses a Reference by name, and splits the other
    three on SEMICOLONS - so following the hint was the one way guaranteed to
    fail.

The existing check in test_generate_jobs.py names five hand-picked shapes. A
hand-picked list is how the other 53 went unexamined.

WHAT IT PROVES
  1. Every caller-supplied type in the library has a hint, OR is named in
     NO_HINT_ON_PURPOSE with a reason. Read through heron_bridge_client's
     fragment_needs - the reader the server uses, which
     test_fragment_needs_reader.py already holds against PyYAML - so this is
     not a second parser with its own opinion of what a need is.
  2. Every type Revit refuses says CANNOT BE TYPED and never offers a
     separator. The refusals come from receivable() in generate-jobs.py, whose
     RECEIVABLE list test_generate_jobs.py already checks against the C#.
  3. The three semicolon-shaped types say SEMICOLONS and not "comma separated"
     - and the two dictionary hints carry NamedValues' OWN example strings,
     read out of RevitFragment.cs, so the hint and the refusal cannot disagree.
  4. The plain-value traps carry FromRequest's own words: "250, not 250mm" is
     read out of the C# and required in both number hints.
  5. Every NO_HINT_ON_PURPOSE entry has a reason AND still returns no hint, so
     the list cannot keep a type that has since been given one.
  6. brain/, mcp/server/ and mcp/client/ share no module name. heron_brain.py
     puts brain/ at sys.path[0], so a shared name is silently shadowed - the
     import succeeds, binds the wrong module and fails one layer later. It
     happened on 2026-09-22 to a module called heron_contract.

WHAT IT CANNOT DO
  It does not prove a hint is TRUE beyond the strings it reads out of the C#.
  FromRequest is the authority and needs Revit to run. A refusal whose wording
  disagrees with a hint is a bug in how_to_type, not in whoever typed it.

  Clause 3 names its three types by hand. A NEW semicolon-shaped type would
  pass clause 1 on any non-empty hint, including a wrong "comma separated".
  Deriving the separator from the C# was considered and rejected: the split
  hides behind five different helper methods, and a parser that misread one
  would be a false green with a confident name.
"""

import importlib.util
import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "tools"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))

REVIT_FRAGMENT = os.path.join(ROOT, "revit", "Heron.Revit.Addin",
                              "RevitFragment.cs")
FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def load_generate_jobs():
    spec = importlib.util.spec_from_file_location(
        "generate_jobs", os.path.join(ROOT, "tools", "generate-jobs.py"))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def caller_types(client):
    """Every caller-supplied type in the library, spaces stripped the way
    FromRequest strips them, with one fragment that asks for each."""
    found = {}
    fragments = os.path.join(ROOT, "brain", "fragments")
    for name in sorted(os.listdir(fragments)):
        needs = client.fragment_needs(
            os.path.join(fragments, name, "fragment.yaml")) or []
        for need in needs:
            if need.get("source") == "request":
                kind = (need.get("type") or "").replace(" ", "")
                found.setdefault(kind, name)
    return found


def modules(*parts):
    where = os.path.join(ROOT, *parts)
    return set(f[:-3] for f in os.listdir(where)
               if f.endswith(".py") and not f.startswith("__"))


def main():
    import heron_bridge_client as CLIENT
    import heron_fragment as HF
    GJ = load_generate_jobs()

    # ASKED FOR, NOT ASSUMED - heron-ship s2a. Run against the code as it
    # stood before this change, the set does not exist; that must be ONE clean
    # failure and the rest of the suite must still run and report.
    silent = getattr(HF, "NO_HINT_ON_PURPOSE", None)
    check(silent is not None,
          "heron_fragment declares which types have no hint on purpose")
    if silent is None:
        silent = {}

    csharp = io.open(REVIT_FRAGMENT, encoding="utf-8").read()
    types = caller_types(CLIENT)

    # --- 1. every caller-supplied type has a hint, or a reason -------------
    print()
    print("Every value a caller types has something beside its blank")
    check(len(types) > 40,
          "the library declares %d distinct caller-supplied types - far fewer "
          "means the reader stopped finding them" % len(types))
    blank = sorted(kind for kind in types
                   if not HF.how_to_type(kind) and kind not in silent)
    check(not blank,
          "all %d are hinted or named with a reason%s" % (
              len(types), "" if not blank else " - BLANK: " + ", ".join(
                  "%s (%s)" % (kind, types[kind]) for kind in blank)))

    # --- 2. a refused type is never offered a way to type it ---------------
    print()
    print("A type Revit refuses says so, and offers no syntax")
    # `Element` is refused only when the need's NAME means one particular
    # element; as a TYPE it is typable and has its own test in
    # test_generate_jobs.py. receivable() needs the name to tell them apart,
    # and a type-level question cannot supply one.
    refused = sorted(kind for kind in types
                     if kind != "Element" and not GJ.receivable(kind)[0])
    check(refused, "some caller-supplied types are refused: %s" % refused)
    for kind in refused:
        hint = HF.how_to_type(kind)
        check("CANNOT BE TYPED" in hint and "comma separated" not in hint,
              "%s says CANNOT BE TYPED and offers no separator: %r"
              % (kind, hint))

    # --- 3. the semicolon types say semicolons -----------------------------
    print()
    print("A value Revit splits on semicolons is not told commas")
    for kind in ("IList<Color>", "IDictionary<string,double>",
                 "IDictionary<string,string>"):
        hint = HF.how_to_type(kind)
        check("SEMICOLON" in hint.upper() and "comma separated" not in hint,
              "%s says semicolons, not commas: %r" % (kind, hint))

    # NamedValues builds its refusal around an example of each shape. Read
    # them out of the C# rather than retyping them, so a change on either
    # side goes red here instead of drifting.
    shown = re.findall(r'\\"([^"\\]*=[^"\\]*;[^"\\]*)\\"', csharp)
    numbers = next((s for s in shown if "Walls=" in s), None)
    texts = next((s for s in shown if "view=" in s), None)
    check(numbers and texts,
          "NamedValues' two example strings are in RevitFragment.cs: %r, %r"
          % (numbers, texts))
    check(numbers and numbers in HF.how_to_type("IDictionary<string,double>"),
          "the number-table hint carries NamedValues' own example")
    check(texts and texts in HF.how_to_type("IDictionary<string,string>"),
          "the text-table hint carries NamedValues' own example")

    # --- 4. the plain-value traps, in FromRequest's own words ---------------
    print()
    print("The traps a modeller walks into, in the add-in's own words")
    check("250, not 250mm" in csharp,
          "FromRequest refuses a unit with '250, not 250mm'")
    for kind in ("int", "double"):
        check("not 250mm" in HF.how_to_type(kind),
              "%s warns against typing the unit: %r"
              % (kind, HF.how_to_type(kind)))
    check("true or false" in HF.how_to_type("bool"),
          "bool says true or false, because bool.TryParse refuses yes")
    check("is red" in csharp and "255,0,0" in HF.how_to_type("Color"),
          "Color gives three numbers, as OneColour's refusal does")
    check("never" in HF.how_to_type("ElementId"),
          "an ElementId is a NAME, never its number")
    check("`selected`" in HF.how_to_type("IList<ElementId>"),
          "a LIST of ids also takes the word `selected`")
    check("refused" in HF.how_to_type("View3D"),
          "View3D says a plan is refused")
    for value in ("Duplicate", "WithDetailing", "AsDependent"):
        check(value in HF.how_to_type("ViewDuplicateOption"),
              "ViewDuplicateOption names %s" % value)

    # --- 5. the silent list is honest --------------------------------------
    print()
    print("A type named as silent on purpose really is, and says why")
    for kind, reason in sorted(silent.items()):
        check(bool(reason and reason.strip()),
              "%s carries a reason" % kind)
        check(not HF.how_to_type(kind),
              "%s still has no hint - an entry kept after one was added is "
              "stale" % kind)

    # --- 6. no module shadows another --------------------------------------
    print()
    print("No module name is shared across the three import roots")
    roots = {"brain": modules("brain"),
             "mcp/server": modules("mcp", "server"),
             "mcp/client": modules("mcp", "client")}
    names = sorted(roots)
    for i, one in enumerate(names):
        for two in names[i + 1:]:
            shared = sorted(roots[one] & roots[two])
            check(not shared, "%s and %s share no module name%s"
                  % (one, two, "" if not shared else " - " + ", ".join(shared)))

    print()
    if FAILURES:
        print("%d FAILURE(S):" % len(FAILURES))
        for failure in FAILURES:
            print("  - %s" % failure)
        return 1
    print("All clauses pass. A hint that is PRESENT and consistently shaped is")
    print("not proved TRUE - FromRequest is the authority, and it needs Revit.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
