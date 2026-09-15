# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-IMP-DUP-007
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Duplicate detection - "nothing comparable" is not "nothing like it".

    python tests/test_duplicates.py

WHAT IT PROVES
  1. THE SIGNATURE TEST IS HERON-FRG-MRG-004's FUNCTION, by identity -
     so the two agents cannot disagree about what the same shape means.

  2. AN ITEM WITH ONLY A NAME IS `unchecked`, NEVER `new`. That is the
     whole agent: an import arrives as raw code, so the comparisons
     mostly cannot run, and answering "no duplicate" then is a silent
     pass.

  3. AN ITEM THAT WAS REALLY COMPARED AND MATCHED NOTHING IS `new`, and
     carries what it was compared by.

  4. THE THREE KINDS OF MATCH ARE REPORTED APART, and each says how
     strong it is - decisive, a question, or weakest.

  5. COMPARING AGAINST AN EMPTY LIBRARY IS REFUSED.

  6. IT READS NOTHING FROM DISK AND CREATES NOTHING.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_duplicates as DUP                                 # noqa: E402
import heron_merge as MRG                                      # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []

DOC = [{"name": "doc", "type": "Document"}]
INT = [{"name": "count", "type": "int"}]

LIBRARY = [
    {"id": "count-them", "name": "count elements",
     "capability": "COUNT_ELEMENTS",
     "contract": {"needs": DOC, "provides": INT}},
    {"id": "filter-them", "name": "filter by category",
     "capability": "FILTER_ELEMENTS",
     "contract": {"needs": DOC,
                  "provides": [{"name": "elements", "type": "IList"}]}}]


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_duplicates.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]

    print("\n1. the signature test is HERON-FRG-MRG-004's function")
    check(DUP.signature is MRG.signature,
          "DUP.signature IS MRG.signature - the same object, not a copy")
    check(DUP.signature({"contract": {"needs": DOC, "provides": INT}})[0]
          == MRG.signature(LIBRARY[0])[0],
          "so the same contract gives the same shape on both sides")
    check("def signature" not in logic,
          "and this module defines no signature function of its own")

    print("\n2. an item with only a name is unchecked, never new")
    bare = DUP.look([{"name": "CountDucts.py"}], fragments=LIBRARY)
    check([one["item"] for one in bare["unchecked"]] == ["CountDucts.py"],
          "it lands in `unchecked`")
    check(bare["new"] == [] and bare["already"] == [],
          "and in NEITHER `new` nor `already`")
    check(bare["unchecked"][0]["compared_by"] == ["name"],
          "with what could be compared: name, and nothing else")
    check("reporting it as new would be reporting a check that did not "
          "happen" in bare["unchecked"][0]["why"],
          "and why that is not the same as new")
    check(any("silent pass" in line for line in bare["unjudged"]),
          "the answer calls it the silent pass this project keeps "
          "catching itself in")

    print("\n3. an item really compared and matching nothing is new")
    fresh = DUP.look([{"name": "BrandNew.py", "capability": "SOMETHING_ELSE",
                       "contract": {"needs": [], "provides": []}}],
                     fragments=LIBRARY)
    check([one["item"] for one in fresh["new"]] == ["BrandNew.py"],
          "it lands in `new`")
    check(fresh["new"][0]["compared_by"] == ["capability", "signature",
                                             "name"],
          "carrying all three things it was compared by")
    check(fresh["unchecked"] == [],
          "and nothing is unchecked")
    # A CAPABILITY ALONE IS ENOUGH TO HAVE REALLY COMPARED.
    half = DUP.look([{"name": "X.py", "capability": "SOMETHING_ELSE"}],
                    fragments=LIBRARY)
    check([one["item"] for one in half["new"]] == ["X.py"],
          "a capability with no contract is still a real comparison")
    check(half["new"][0]["compared_by"] == ["capability", "name"],
          "compared by capability and name")

    print("\n4. the three kinds of match are reported apart")
    same_capability = DUP.look(
        [{"name": "anything", "capability": "COUNT_ELEMENTS"}],
        fragments=LIBRARY)
    hit = same_capability["already"][0]["matches"][0]
    check(hit["by"] == "capability" and hit["is"] == "count-them",
          "a capability match names the fragment that has it")
    check("decisive rather than suggestive" in hit["why"],
          "and says it is decisive - one provider per capability")
    same_shape = DUP.look(
        [{"name": "TagSheet.py", "capability": "TAG_SHEET",
          "contract": {"needs": DOC, "provides": INT}}], fragments=LIBRARY)
    shaped = same_shape["already"][0]["matches"][0]
    check(shaped["by"] == "signature",
          "a signature match is reported as a signature match")
    check("is a QUESTION" in shaped["why"],
          "and says it is a question, not an answer - the plumbing can "
          "match while the job differs")
    same_name = DUP.look([{"name": "Count Elements"}], fragments=LIBRARY)
    check(same_name["already"][0]["matches"][0]["by"] == "name",
          "a name match is caught through case and spacing")
    check("weakest of the three" in
          same_name["already"][0]["matches"][0]["why"],
          "and is called the weakest")
    both = DUP.look([{"name": "count elements",
                      "capability": "COUNT_ELEMENTS"}], fragments=LIBRARY)
    check(sorted(one["by"] for one in both["already"][0]["matches"])
          == ["capability", "name"],
          "an item matching two ways reports BOTH, rather than the first")
    check(DUP.BY == ("capability", "signature", "name"),
          "and the order they are named in is strongest first")

    print("\n5. comparing against an empty library is refused")
    for kwargs in ({}, {"fragments": []}, {"fragments": [], "skills": []}):
        answer = DUP.look([{"name": "x"}], **kwargs)
        reached.add(answer.get("refused"))
        check(answer.get("refused") == "NO_LIBRARY",
              "an empty library is refused")
    check("An answer that cannot be wrong is not evidence"
          in DUP.look([{"name": "x"}])["why"],
          "because every item would come back new")
    check(DUP.look([{"name": "x"}], skills=LIBRARY)["looked"],
          "while skills alone are a library too")

    print("\n6. it reads nothing from disk and creates nothing")
    imports = sorted(line.split()[1] for line in logic.split("\n")
                     if line.startswith("import "))
    check(imports == ["heron_merge", "os", "sys"],
          "the whole import list is os, sys and HERON-FRG-MRG-004: %s"
          % ", ".join(imports))
    for writing in ("open(", "write(", "listdir", "load_all", "walk("):
        check(writing not in logic, "the agent never uses %s" % writing)
    check(any("NOTHING WAS CREATED" in line for line in fresh["unjudged"]),
          "and the answer says nothing was created, which is the point of "
          "running before anything is")

    print("\n7. every declared failure is named and reached")
    for these, kwargs, name in (
            ([], {"fragments": LIBRARY}, "NOTHING_TO_CHECK"),
            (["a string"], {"fragments": LIBRARY}, "NOT_AN_ITEM"),
            ([{"name": "  "}], {"fragments": LIBRARY}, "NOT_AN_ITEM"),
            ([{"name": "x"}, {"name": "X"}], {"fragments": LIBRARY},
             "DUPLICATE_NAME")):
        answer = DUP.look(these, **kwargs)
        reached.add(answer.get("refused"))
        check(answer.get("refused") == name, "%s is reached" % name)

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-IMP-DUP-007.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 4, "the contract declares 4 failures")
    for failure in named:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    check(len(fresh["unjudged"]) == 4, "four things are left unjudged")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    nothing comparable is not nothing like it")
    return 0


if __name__ == "__main__":
    sys.exit(main())
