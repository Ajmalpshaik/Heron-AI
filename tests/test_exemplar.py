# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-STD-REF-010
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Reference model profiler - the client's model does not survive it.

    python tests/test_exemplar.py

WHAT IT PROVES
  1. THE MODEL IS DISCARDED. Every name carries a client project code,
     and the code appears NOWHERE in the finished profile - searched for
     across every string in the answer, not asserted about a field.

  2. THE CHECK IS REAL. Handed a profile that does carry a name, it
     reports it rather than claiming success.

  3. BOTH NUMBERS TRAVEL AND NO LINE IS DRAWN. Count and proportion for
     the dominant shape and for every exception, and no threshold field
     anywhere.

  4. AN EXCEPTION IS NOT A VIOLATION - it is recorded under that name,
     with its own numbers.

  5. IT PROPOSES AND ENTERS AT THE BOTTOM RUNG, by HERON-IMP-APR-014's
     own constant.

  6. THE SHAPE RULE IS HERON-STD-NAM-004's OBJECT, so a profile and a
     later check are the same arithmetic.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_exemplar as REF                                   # noqa: E402
import heron_convention as NAM                                 # noqa: E402
import heron_import as IMPORT                                  # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


CLIENT = "QA-2026-ASHGHAL"

NAMES = ([{"name": "%s-MEP-DUCT-SUPPLY-L%02d" % (CLIENT, n),
           "kind": "element"} for n in range(1, 41)]
         + [{"name": "%s duct copy %d" % (CLIENT, n), "kind": "element"}
            for n in (1, 2)]
         + [{"name": "%s-M-%03d" % (CLIENT, n), "kind": "sheet"}
            for n in range(1, 13)]
         + [{"name": "%s_M_099" % CLIENT, "kind": "sheet"}])


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_exemplar.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]

    answer = REF.profile(NAMES, categories=["OST_DuctCurves"])

    print("\n1. the model is discarded")
    check(answer["profiled"] is True, "the names were read")
    everything = " ".join(REF._strings(answer))
    check(CLIENT not in everything,
          "the client code %r appears NOWHERE in the profile" % CLIENT)
    for card in NAMES[:5] + NAMES[-5:]:
        check(card["name"] not in everything,
              "%r did not survive" % card["name"][:28])
    check(answer["kept_nothing"] is True, "`kept_nothing` says so")
    check(answer["survived"] == [], "and nothing is listed as surviving")
    check(answer["of"] == len(NAMES),
          "while the COUNT of what went in is kept (%d)" % answer["of"])

    print("\n2. the check is real")
    leaky = dict(answer)
    leaky["note"] = "taken from %s-M-001" % CLIENT
    survived = REF.kept_nothing(leaky, [card["name"] for card in NAMES])
    check(survived == ["%s-M-001" % CLIENT],
          "a leak through a field nobody thought about is found: %s"
          % survived)
    check(REF.kept_nothing(answer, [card["name"] for card in NAMES]) == [],
          "and the real profile still comes back clean")

    print("\n3. both numbers travel and no line is drawn")
    by_kind = dict((one["kind"], one) for one in answer["patterns"])
    element = by_kind["element"]
    check(element["of"] == 42, "42 element names (%d)" % element["of"])
    check(element["dominant"]["of"] == 40,
          "40 take the dominant shape (%d)" % element["dominant"]["of"])
    check(element["dominant"]["proportion"] == 95,
          "which is 95%% (%d%%)" % element["dominant"]["proportion"])
    for one in element["exceptions"]:
        check("of" in one and "proportion" in one,
              "the exception %r carries both numbers too" % one["shape"])
    for field in ("threshold", "ok", "wrong", "violation", "score"):
        check(field not in everything.split(),
              "no %r anywhere in the answer" % field)
    check(sorted(REF.PROFILE_CARRIES) == sorted(REF.PROFILE_CARRIES),
          "and the profile's own field list is declared")

    print("\n4. an exception is not a violation")
    check(len(element["exceptions"]) == 1,
          "one exception in the elements (%d)" % len(element["exceptions"]))
    check(element["exceptions"][0]["of"] == 2,
          "carried by two names (%d)" % element["exceptions"][0]["of"])
    check("exceptions" in element and "violations" not in element,
          "it is called an exception: %s" % ", ".join(sorted(element)))
    check(any("not a violation" in line.lower()
              for line in answer["unjudged"]),
          "and the answer says why that matters")
    sheets = by_kind["sheet"]
    check("_" in sheets["exceptions"][0]["separators"],
          "the underscore sheet is its own shape, and its separators are "
          "BOTH the ones in the name: %s"
          % sheets["exceptions"][0]["separators"])

    print("\n5. it proposes and enters at the bottom rung")
    check(answer["adopted"] is False, "`adopted` is false")
    check(REF.ENTERS_AT is IMPORT.ENTERS_AT,
          "REF.ENTERS_AT IS IMPORT.ENTERS_AT - the same object")
    check(answer["enters_at"] == "DISCOVERED",
          "and it is %r" % answer["enters_at"])
    check(any("explicit yes" in line for line in answer["unjudged"]),
          "the answer says adoption needs one")
    check(any("CONFLICT" in line for line in answer["unjudged"]),
          "and that disagreeing with a standard is a conflict, not an "
          "overwrite")

    print("\n6. the shape rule is HERON-STD-NAM-004's object")
    check(REF.shape_of is NAM.shape_of,
          "REF.shape_of IS NAM.shape_of - the same function")
    check(REF.KINDS is NAM.KINDS, "and the kinds are that agent's too")
    # THE POINT OF SHARING IT: a name checked later has to skeleton the
    # same way, or the comparison means nothing.
    later = NAM.look([{"name": "%s-MEP-DUCT-RETURN-L07" % CLIENT,
                       "kind": "element"}])
    check(later["kinds"][0]["commonest"] == element["dominant"]["shape"],
          "the same structure gives the same shape through both agents: %r"
          % element["dominant"]["shape"])

    print("\n6a. and the shape carries the client's own project code")
    # NOT A TEST BUG - A FINDING. Every name in this model starts with the
    # project code, so the dominant shape has segments no OTHER project
    # will ever have. Stripping them would mean guessing which segment is
    # the project, and that is PROPOSALS F30.
    other = NAM.look([{"name": "MEP-DUCT-SUPPLY-L07", "kind": "element"}])
    check(other["kinds"][0]["commonest"] != element["dominant"]["shape"],
          "a name WITHOUT the project code does not match the profile: "
          "%r vs %r" % (other["kinds"][0]["commonest"],
                        element["dominant"]["shape"]))
    check(any("project code" in line.lower() for line in answer["unjudged"]),
          "and the answer says so rather than leaving it to be discovered")

    print("\n7. every failure is named and reached")
    for these, name in (([], "NOTHING_TO_PROFILE"),
                        (None, "NOTHING_TO_PROFILE"),
                        ([123], "NOT_A_NAME"),
                        ([{"kind": "sheet"}], "NOT_A_NAME"),
                        ([{"name": "  "}], "NOT_A_NAME")):
        said = REF.profile(these)
        reached.add(said.get("refused"))
        check(said.get("refused") == name, "%s is reached" % name)

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-STD-REF-010.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 2, "the contract declares 2 failures")
    for failure in named:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    check(len(answer["unjudged"]) == 7, "seven things are left unjudged")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the client's model does not survive it")
    return 0


if __name__ == "__main__":
    sys.exit(main())
