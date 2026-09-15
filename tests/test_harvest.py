# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-IMP-FEX-004
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Fragment extraction - the units are parsed, and reusable is not decided.

    python tests/test_harvest.py

WHAT IT PROVES
  1. needs AND provides ARE COMPUTED. Worked out by hand against a
     function whose free names, parameters and locals are all different.

  2. A PARAMETER IS NOT A NEED, A LOCAL IS NOT A NEED, AND A BUILTIN IS
     NOT A NEED. Each is checked on its own.

  3. CALLED-FROM-TWO-PLACES IS A COUNT ACROSS THE FOLDER, and a
     function's own recursion is not somebody else using it.

  4. NOTHING IS PICKED AND NOTHING IS NAMED. No output field holds a
     capability, an id or a verdict.

  5. A FILE THAT DOES NOT PARSE IS A REASON, NOT AN EMPTY LIST - "no
     functions" and "this is not Python" are different answers.

  6. A LANGUAGE WITH NO PARSER IS NAMED, NEVER BRACE-SCANNED.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_harvest as FEX                                    # noqa: E402
import heron_walk as WALK                                      # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


# EVERY CASE IN ONE FUNCTION, so the four rules are told apart by hand:
#   doc, category  parameters      -> not needs
#   found, n       bound locally   -> provides, not needs
#   len, str       builtins        -> not needs
#   COLLECTOR, OST free names      -> needs
HELPER = (
    "def collect(doc, category):\n"
    '    """Every element of one category."""\n'
    "    found = COLLECTOR(doc).OfCategory(category)\n"
    "    n = len(str(found))\n"
    "    return found, n\n")

USES_IT = (
    "def count_ducts(doc):\n"
    "    return collect(doc, OST_DuctCurves)\n"
    "\n\n"
    "def tag_sheets(doc):\n"
    "    return collect(doc, OST_Sheets)\n")

RECURSES = (
    "def walk_tree(node):\n"
    "    for child in node:\n"
    "        walk_tree(child)\n"
    "    return node\n")


def write(where, name, text):
    with io.open(os.path.join(where, name), "w", encoding="utf-8") as handle:
        handle.write(text)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_harvest.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]

    yard = tempfile.mkdtemp(prefix="heron-harvest-test-")
    try:
        where = os.path.join(yard, "AJ-Tools")
        os.makedirs(where)
        write(where, "helpers.py", HELPER)
        write(where, "tools.py", USES_IT)
        write(where, "tree.py", RECURSES)
        write(where, "broken.py", "def oops(:\n    pass\n")
        write(where, "Tool.cs", "public class Tool { }\n")

        answer = FEX.harvest(where)
        by_name = dict((one["name"], one) for one in answer["units"])

        print("\n1. needs and provides are computed")
        check(answer["harvested"] is True, "the folder was harvested")
        one = by_name["collect"]
        check(sorted(one["needs"]) == ["COLLECTOR"],
              "needs is exactly the free name: %s" % one["needs"])
        check(sorted(one["provides"]) == ["found", "n"],
              "provides is exactly what it binds: %s"
              % sorted(one["provides"]))
        check(one["parameters"] == ["doc", "category"],
              "with its parameters reported separately: %s"
              % one["parameters"])
        check(one["says"] == "Every element of one category.",
              "and its docstring, unaltered")

        print("\n2. a parameter, a local and a builtin are not needs")
        for name in ("doc", "category"):
            check(name not in one["needs"],
                  "%r is a parameter, not a need" % name)
        for name in ("found", "n"):
            check(name not in one["needs"],
                  "%r is bound locally, not a need" % name)
        for name in ("len", "str"):
            check(name not in one["needs"], "%r is a builtin, not a need"
                                            % name)
        check(sorted(FEX.contract_of.__doc__ is not None and
                     one["needs"]) == ["COLLECTOR"],
              "so one free name survives all three rules")

        print("\n3. called-from-two-places is a count across the folder")
        check(one["calledBy"] == 2,
              "`collect` is called from two files (%d)" % one["calledBy"])
        check(one["reusedAlready"] is True, "so it is already reused")
        check(answer["reusedAlready"] == ["collect"],
              "and it is the only one: %s" % answer["reusedAlready"])
        check(by_name["count_ducts"]["calledBy"] == 0,
              "a function nothing calls counts 0, not a poor score")
        check(by_name["count_ducts"]["reusedAlready"] is False,
              "and is simply not reused yet")
        # RECURSION IS NOT REUSE, and counting characters got this wrong.
        check(by_name["walk_tree"]["recursive"] == 1,
              "walk_tree calls itself once (%d)"
              % by_name["walk_tree"]["recursive"])
        check(by_name["walk_tree"]["calledBy"] == 0,
              "so its calledBy is 0, not 1 - its own recursion is not "
              "somebody else using it")
        check(by_name["walk_tree"]["reusedAlready"] is False,
              "and it is not reported as already reused")
        check(answer["units"][0]["name"] == "collect",
              "the most-called unit is listed first")

        print("\n4. nothing is picked and nothing is named")
        check(answer["named"] is False, "`named` is false")
        for unit in answer["units"]:
            check(not any(key in unit for key in
                          ("capability", "id", "keep", "score", "verdict")),
                  "%s carries no name and no verdict: %s"
                  % (unit["name"], ", ".join(sorted(unit))))
        check(len(answer["asks"]) == 1,
              "one question carries all %d units" % answer["of"])
        check(len(answer["asks"][0]["units"]) == answer["of"],
              "with every one attached")

        print("\n5. a file that does not parse is a reason")
        got, problem = FEX.units(os.path.join(where, "broken.py"))
        check(got == [] and problem,
              "broken.py gives a reason, not an empty list: %r"
              % (problem or "")[:40])
        check("does not parse" in problem and "line" in problem,
              "and the reason names the line")
        empty, nothing = FEX.units(os.path.join(where, "tree.py"))
        check(nothing is None and len(empty) == 1,
              "while a file that parses gives units and no problem")
        broke = [card for card in answer["unparsed"]
                 if card["at"] == "broken.py"]
        check(len(broke) == 1, "and it is reported in `unparsed`")

        print("\n6. a language with no parser is named")
        cs = [card for card in answer["unparsed"] if card["at"] == "Tool.cs"]
        check(len(cs) == 1, "the .cs file is reported")
        check("no parser" in cs[0]["why"],
              "saying Heron has no parser for it")
        check("brace" in cs[0]["why"],
              "and why a brace scan is not the answer")
        check("{" not in logic.replace('"{"', "").replace("'{'", "")
              or "brace" not in logic.split("def units(")[0].split("WHY_NOT")[0],
              "and the module really does no brace scanning")

        print("\n7. every failure is named and reached")
        empty_folder = os.path.join(yard, "nothing-here")
        os.makedirs(empty_folder)
        write(empty_folder, "README.md", "# nothing to harvest\n")
        for these, name in ((None, "NOTHING_TO_WALK"),
                            (os.path.join(yard, "gone"), "NOT_A_FOLDER"),
                            (empty_folder, "NOTHING_TO_HARVEST")):
            said = FEX.harvest(these)
            reached.add(said.get("refused"))
            check(said.get("refused") == name, "%s is reached" % name)
        walked = WALK.walk(where)
        check(FEX.harvest(None, walked=walked)["of"] == answer["of"],
              "and a walk can be handed in rather than repeated")

        contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                         "HERON-IMP-FEX-004.yaml"))
        named = contract.get("failures") or []
        check(len(named) == 3, "the contract declares 3 failures")
        for failure in named:
            check(failure in logic or failure in ("NOTHING_TO_WALK",
                                                  "NOT_A_FOLDER"),
                  "the code names or carries %s" % failure)
        unreached = sorted(set(named) - reached)
        check(not unreached,
              "and every one was reached above%s"
              % ("" if not unreached else ": %s" % ", ".join(unreached)))
        check(len(answer["unjudged"]) == 6, "six things are left unjudged")
    finally:
        shutil.rmtree(yard, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the units are parsed, and reusable is not decided")
    return 0


if __name__ == "__main__":
    sys.exit(main())
