# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-IMP-REN-010
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Import filing - one name is a consequence, the others are choices.

    python tests/test_filing.py

WHAT IT PROVES
  1. THE FOLDER IS DERIVED, by heron_fragment's own function - the same
     object, and this module compiles no pattern of its own.

  2. WHAT CANNOT BE DERIVED IS ASKED FOR, with the stated SHAPE in the
     question so the answer can be right first time.

  3. A BAD NAME COMES BACK IN HERON-FRG-VAL-001's OWN WORDS - identical
     to calling naming_problems directly.

  4. A KIND NOBODY FILES IS ASKED ABOUT, not guessed at.

  5. NOTHING IS MOVED AND NO ID IS CHANGED.

  6. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_filing as FIL                                     # noqa: E402
import heron_fragment as FRAG                                  # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_filing.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]

    print("\n1. the folder is derived")
    check(FIL.folder_for is FRAG.folder_for,
          "FIL.folder_for IS FRAG.folder_for - the same object")
    for capability in ("COUNT_DUCTS", "FILTER_ELEMENTS_BY_CATEGORY",
                       "TAG_SHEET", "EXPORT_VIEWS_TO_DWG"):
        answer = FIL.file_it([{"name": "x.py", "kind": "fragment",
                               "capability": capability,
                               "id": "FRG-ELE-001"}])
        want = "brain/fragments/%s" % FRAG.folder_for(capability)
        check(answer["placed"][0]["goes"] == want,
              "%s -> %s" % (capability, want))
    # NO PATTERN OF ITS OWN. A word search would only find the module
    # QUOTING the stated shape into its question, which is the right
    # thing to do - so the proof is the import list.
    imports = sorted(line.split()[1] for line in logic.split("\n")
                     if line.startswith("import "))
    check(imports == ["heron_fragment", "os", "sys"],
          "the whole import list is os, sys and heron_fragment - it does "
          "not even import `re`, so it compiles nothing: %s"
          % ", ".join(imports))
    check("re.compile" not in logic,
          "and compiles no pattern of its own")
    one = FIL.file_it([{"name": "s", "kind": "skill", "id": "count-things"}])
    check(one["placed"][0]["goes"] == "brain/skills/count-things.yaml",
          "and a skill is <id>.yaml")
    check(one["placed"][0]["derived_from"] == "id",
          "each placement says what it was derived from")

    print("\n2. what cannot be derived is asked for")
    for missing, name in ((["capability", "id"], "CountDucts.py"),
                          (["id"], "HasCapability.py"),
                          (["capability"], "HasId.py")):
        item = {"name": name, "kind": "fragment"}
        if "capability" not in missing:
            item["capability"] = "COUNT_DUCTS"
        if "id" not in missing:
            item["id"] = "FRG-ELE-001"
        answer = FIL.file_it([item])
        check(answer["asks"] and answer["asks"][0]["needs"] == missing,
              "missing %s is asked for" % " and ".join(missing))
        check(answer["placed"] == [],
              "  and nothing is placed on a guess")
    asked = FIL.file_it([{"name": "x.py", "kind": "fragment"}])["asks"][0]
    # THE SHAPE IS IN THE QUESTION.
    check("SCREAMING_SNAKE_CASE" in asked["asked"]["capability"],
          "the capability question carries its shape")
    check("FRG-<AREA>-<NNN>" in asked["asked"]["id"]
          and "ELE" in asked["asked"]["id"],
          "and the id question carries its shape and the real area list")
    check(sorted(FRAG.AREAS)[0] in FIL.ASKED_FOR["id"],
          "which is heron_fragment's AREAS, not a list written here")
    check("HERON-NAM-GEN-001" in asked["why"],
          "naming the agent left unbuilt for the same reason")

    print("\n3. a bad name comes back in the owner's own words")
    bad = {"name": "BadName.py", "kind": "fragment",
           "capability": "CountDucts", "id": "FRG-SHT-043"}
    mine = FIL.file_it([bad])
    check(mine["problems"] and mine["problems"][0]["by"]
          == "HERON-FRG-VAL-001",
          "a capability in the wrong shape is refused, and attributed")
    theirs = FRAG.naming_problems(FRAG.Fragment(
        {"id": "FRG-SHT-043", "capability": "CountDucts"},
        os.path.join(ROOT, "brain", "fragments",
                     FRAG.folder_for("CountDucts"))))
    check(mine["problems"][0]["problems"] == theirs,
          "identical to calling naming_problems directly - %d problem(s)"
          % len(theirs))
    area = FIL.file_it([{"name": "x.py", "kind": "fragment",
                         "capability": "COUNT_DUCTS",
                         "id": "FRG-ZZZ-001"}])
    check(area["problems"] and "ZZZ" in area["problems"][0]["problems"][0],
          "an area heron_fragment does not know is refused too")
    check(area["placed"] == [],
          "and a refused name is not also placed")

    print("\n4. a kind nobody files is asked about")
    for kind in ("document", "asset", "config"):
        answer = FIL.file_it([{"name": "x", "kind": kind}])
        check(answer["asks"] and answer["asks"][0]["needs"] == [],
              "a %r is asked about rather than filed" % kind)
    doc = FIL.file_it([{"name": "README.md", "kind": "document"}])
    check("choosing where somebody else's work lives" in doc["asks"][0]["why"],
          "with why: inventing a third convention would be this agent "
          "choosing where somebody else's work lives")
    check(doc["placed"] == [] and doc["problems"] == [],
          "and it is in neither of the other two lists")

    print("\n5. nothing is moved and no id is changed")
    good = FIL.file_it([{"name": "TagSheet.py", "kind": "fragment",
                         "capability": "TAG_SHEET", "id": "FRG-SHT-042"}])
    check(good["moved"] is False, "`moved` is false, and always false")
    check(good["placed"][0]["id"] == "FRG-SHT-042",
          "the id comes back exactly as supplied")
    check("Nothing was moved" in good["why"], "and the answer says so")
    for writing in ("os.rename", "shutil", "write(", "makedirs", "open("):
        check(writing not in logic, "the agent never uses %s" % writing)
    check(any("a name is not an identity" in line.lower()
              for line in good["unjudged"]),
          "and the answer says a name is not an identity")
    check(any("HERON-IMP-MIG-009" in line for line in good["unjudged"]),
          "naming who does the moving")

    print("\n6. every failure the contract declares is named and reached")
    for these, name in (
            ([], "NOTHING_TO_FILE"),
            (["a string"], "NOT_AN_ITEM"),
            ([{"name": "x"}], "NOT_AN_ITEM"),
            ([{"kind": "fragment"}], "NOT_AN_ITEM"),
            ([{"name": "x", "kind": "fragment"},
              {"name": "x", "kind": "skill"}], "NOT_AN_ITEM")):
        answer = FIL.file_it(these)
        reached.add(answer.get("refused"))
        check(answer.get("refused") == name, "%s is reached" % name)

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-IMP-REN-010.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 2, "the contract declares 2 failures")
    for failure in named:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    check(len(good["unjudged"]) == 4, "four things are left unjudged")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    one name is a consequence, the others are choices")
    return 0


if __name__ == "__main__":
    sys.exit(main())
