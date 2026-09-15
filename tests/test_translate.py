# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-IMP-MIG-009
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Migration - it prepares and it checks, and it translates nothing.

    python tests/test_translate.py

WHAT IT PROVES
  1. IT TRANSLATES NOTHING. `translated` is false and no output field
     holds C#.

  2. STILL-PYTHON CATCHES THE MIGRATION THAT CHANGED NOTHING - the
     original's own code is refused, and real C# is not.

  3. THE CARD RULES ARE HERON-FRG-VAL-001's OWN OBJECTS, not a list
     retyped here.

  4. A RELEASE OUTSIDE D-05's EIGHT IS REFUSED, in both directions -
     asking for one, and checking a proposal that claims one.

  5. A RENAME IS NOT A FINDING. Names that vanish are reported as a
     look, and their absence is not a problem.

  6. IT RUNS ON HERON-IMP-FEX-004's REAL OUTPUT.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_translate as MIG                                  # noqa: E402
import heron_harvest as FEX                                    # noqa: E402
import heron_fragment as FRAG                                  # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


PYTHON = ("def collect(doc, category):\n"
          "    found = COLLECTOR(doc).OfCategory(category)\n"
          "    return found\n")

CSHARP = ("var found = new FilteredElementCollector(doc)\n"
          "    .OfCategory(category).ToElements();\n"
          "var n = found.Count;\n")

UNIT = {"name": "collect", "file": "helpers.py", "code": PYTHON,
        "needs": ["COLLECTOR"], "provides": ["found"],
        "says": "Every element of one category."}


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_translate.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]

    print("\n1. it translates nothing")
    answer = MIG.brief(UNIT, releases=["2024"])
    check(answer["briefed"] is True, "the unit was read")
    check(answer["translated"] is False, "`translated` is false")
    check(answer["from"]["language"] == "Python"
          and answer["into"] == MIG.THE_LANGUAGE,
          "it says what it is FROM and what it must become: %s -> %s"
          % (answer["from"]["language"], answer["into"]))
    check(answer["from"]["code"] == PYTHON,
          "the original comes back unaltered")
    check(not any(key in answer for key in ("code", "csharp", "result")),
          "and no field holds a translation: %s" % ", ".join(sorted(answer)))
    check(len(answer["gates"]) == 2,
          "two gates are NAMED rather than run: %s"
          % ", ".join(g["gate"] for g in answer["gates"]))

    print("\n2. still-python catches the migration that changed nothing")
    check(MIG.still_python(PYTHON) is True, "the original parses as Python")
    check(MIG.still_python(CSHARP) is False, "and real C# does not")
    said = MIG.check(PYTHON, UNIT)
    reached.add(said.get("refused"))
    check(said.get("refused") == "STILL_PYTHON",
          "so handing the original back is REFUSED")
    check("renamed a file and changed nothing" in said["why"],
          "with the failure it is for, named")
    good = MIG.check(CSHARP, UNIT)
    check(good["checked"] is True, "while the C# is examined")
    check(good["compiled"] is False, "and nothing was compiled")
    check(MIG.still_python("") is False,
          "an empty proposal is not 'still Python' - it is nothing")

    print("\n3. the card rules are HERON-FRG-VAL-001's own objects")
    check(MIG.MUST_DECLARE is FRAG.REQUIRED,
          "MIG.MUST_DECLARE IS FRAG.REQUIRED - the same tuple")
    check(MIG.RELEASES is FRAG.REVIT_VERSIONS,
          "MIG.RELEASES IS FRAG.REVIT_VERSIONS - the same tuple")
    bare = MIG.check(CSHARP, UNIT)
    missing = [one for one in bare["problems"] if "missing" in one]
    check(len(missing) == 1, "code with no card is a problem")
    check(sorted(missing[0]["missing"]) == sorted(FRAG.REQUIRED),
          "and every required field is named: %d of them"
          % len(missing[0]["missing"]))
    full = dict((field, "x") for field in FRAG.REQUIRED)
    full["code"] = CSHARP
    full["revit"] = ["2024"]
    complete = MIG.check(full, UNIT)
    check(not [one for one in complete["problems"] if "missing" in one],
          "a complete card raises no missing-field problem")

    print("\n4. a release outside D-05's eight is refused")
    for bad in (["2019"], ["2028"], ["2024", "1999"]):
        said = MIG.brief(UNIT, releases=bad)
        reached.add(said.get("refused"))
        check(said.get("refused") == "UNKNOWN_RELEASE",
              "asking for %s is refused" % bad)
    check("D-05" in said["why"], "and the reason names D-05")
    claims = dict(full)
    claims["revit"] = ["2024", "2019"]
    said = MIG.check(claims, UNIT)
    releases = [one for one in said["problems"] if "releases" in one]
    check(len(releases) == 1 and releases[0]["releases"] == ["2019"],
          "and a PROPOSAL claiming one is a problem too: %s"
          % releases[0]["releases"])

    print("\n5. a rename is not a finding")
    renamed = MIG.check("var elements = Collect(doc, category);\n", UNIT)
    check(renamed["namesGone"] == ["found"],
          "the original's name is reported as gone: %s"
          % renamed["namesGone"])
    check(not [one for one in renamed["problems"] if "names" in str(one)],
          "and that is NOT counted as a problem")
    check(any("renam" in line for line in renamed["unjudged"]),
          "the answer says a translation may rename anything")
    check(good["namesKept"] == ["found"],
          "while a proposal keeping the name says so")

    print("\n6. it runs on HERON-IMP-FEX-004's real output")
    yard = tempfile.mkdtemp(prefix="heron-translate-test-")
    try:
        where = os.path.join(yard, "AJ-Tools")
        os.makedirs(where)
        with io.open(os.path.join(where, "helpers.py"), "w",
                     encoding="utf-8") as handle:
            handle.write(PYTHON)
        harvested = FEX.harvest(where)
        check(harvested["of"] == 1, "one unit harvested")
        real = MIG.brief(harvested["units"][0], releases=["2024"])
        check(real["briefed"] is True,
              "and it briefs that agent's own record without translation")
        check(real["from"]["needs"] == ["COLLECTOR"],
              "carrying the computed needs through: %s"
              % real["from"]["needs"])
        check(real["from"]["provides"] == ["found"],
              "and the computed provides: %s" % real["from"]["provides"])
    finally:
        shutil.rmtree(yard, ignore_errors=True)

    print("\n7. every failure is named and reached")
    for these, name in ((None, "NOTHING_TO_MIGRATE"),
                        ({}, "NOTHING_TO_MIGRATE"),
                        ("a string", "NOT_A_UNIT"),
                        ({"name": "  "}, "NOT_A_UNIT")):
        said = MIG.brief(these)
        reached.add(said.get("refused"))
        check(said.get("refused") == name, "%s is reached" % name)
    for these in (None, "", "   "):
        said = MIG.check(these)
        reached.add(said.get("refused"))
        check(said.get("refused") == "NOTHING_TO_CHECK",
              "%r is refused by check" % these)

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-IMP-MIG-009.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 5, "the contract declares 5 failures")
    for failure in named:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    check(len(answer["unjudged"]) == 4, "four things are left unjudged")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    it prepares and it checks, and it translates nothing")
    return 0


if __name__ == "__main__":
    sys.exit(main())
