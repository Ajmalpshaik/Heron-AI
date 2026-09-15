# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-RAG-FMT-004
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Fragment matching - a near match is not a match.

    python tests/test_matcher.py

WHAT IT PROVES
  1. THE VERSION IS A WALL, APPLIED BEFORE THE CAPABILITY - a
     wrong-version fragment that matches perfectly otherwise is still
     excluded, and with no version named the agent REFUSES rather than
     matching everything.

  2. A NEAR MATCH IS UNREACHABLE FROM `matched`. One missing need is
     enough, and what is missing is named.

  3. THE MATCH IS ON THE CONTRACT, NOT THE NAME. A card whose id says
     one thing and whose contract says another is judged on the
     contract.

  4. NOTHING MATCHING PRODUCES A BRIEF, not an empty list.

  5. STATUS IS REPORTED AND NOT PREFERRED - a DRAFT match and a PROVEN
     partial keep their places.

  6. IT RUNS ON THE REAL LIBRARY. All 360 cards load and are matched
     against a real capability without the agent falling over.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_matcher as FMT                                    # noqa: E402
import heron_fragment as FRAG                                  # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def card(identifier, capability, revit, needs=(), provides=(),
         status="PROVEN"):
    return {"id": identifier, "capability": capability,
            "heron-status": status, "source": "OFFICIAL", "risk": "READ",
            "revit": list(revit),
            "contract": {
                "needs": [{"name": name, "source": "request"}
                          for name in needs],
                "provides": [{"name": name} for name in provides]}}


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_matcher.py"),
                    encoding="utf-8").read()
    code = whole.split("\nfrom __future__", 1)[1]
    logic = code.split("\ndef main(")[0]

    def ask(request, fragments, **kw):
        answer = FMT.match(request, fragments, **kw)
        if answer.get("refused"):
            reached.add(answer["refused"])
        for row in (answer.get("excluded") or []):
            reached.add(row["refused"])
        return answer

    want = {"capability": "DO_A_THING", "has": ["view"], "wants": ["done"]}

    print("1. The version is a wall, applied before the capability")
    perfect_but_old = card("FRG-X-001", "DO_A_THING", ["2021"],
                           needs=["view"], provides=["done"])
    walled = ask(want, [perfect_but_old], revit="2024")
    check(not walled["matched"],
          "a fragment that matches in every other way is not matched")
    check(not walled["partial"],
          "and it is not in `partial` either - it is not a weaker "
          "candidate, it is not a candidate")
    check(walled["excluded"][0]["refused"] == "WRONG_REVIT_VERSION",
          "it is excluded, with the reason")
    check(walled["excluded"][0]["declares"] == ["2021"],
          "and what it declares is reported, so the exclusion can be "
          "checked")
    check("wall, not a weighting" in walled["excluded"][0]["why"],
          "the refusal says a wall and not a weighting")
    check(ask(want, [perfect_but_old]).get("refused") == "NO_REVIT_VERSION",
          "with NO version named the agent refuses rather than matching "
          "everything")
    check(ask(want, [perfect_but_old], revit="2019").get("refused")
          == "NO_REVIT_VERSION",
          "and a release this project does not support is refused too - "
          "D-05 does not extrapolate")
    for release in FRAG.REVIT_VERSIONS:
        check(not ask(want, [card("FRG-X-002", "DO_A_THING", [release],
                                  needs=["view"], provides=["done"])],
                      revit=release).get("refused"),
              "  %s is accepted, read from heron_fragment" % release)

    print("\n2. A near match is unreachable from `matched`")
    short = card("FRG-X-003", "DO_A_THING", ["2024"],
                 needs=["view", "filter"], provides=["done"])
    near = ask(want, [short], revit="2024")
    check(not near["matched"], "one missing need is enough: nothing matched")
    check(near["partial"][0]["missing"] == ["filter"],
          "and the missing need is named: %s" % near["partial"][0]["missing"])
    check("half-work" in near["partial"][0]["why"],
          "the reason says it would run, half-work and look like a success")
    undelivered = ask({"capability": "DO_A_THING", "has": ["view"],
                       "wants": ["done", "count"]},
                      [card("FRG-X-004", "DO_A_THING", ["2024"],
                            needs=["view"], provides=["done"])],
                      revit="2024")
    check(not undelivered["matched"]
          and undelivered["partial"][0]["undelivered"] == ["count"],
          "an output the request asked for and the card does not declare "
          "is the same kind of miss")
    both = ask({"capability": "DO_A_THING", "has": [], "wants": ["count"]},
               [short], revit="2024")
    check(sorted(both["partial"][0]["missing"]) == ["filter", "view"]
          and both["partial"][0]["undelivered"] == ["count"],
          "and both halves are reported together, not one at a time")

    print("\n3. The match is on the contract, not the name")
    lying = {"id": "FRG-filter-by-size-001", "capability": "DO_A_THING",
             "heron-status": "PROVEN", "source": "OFFICIAL", "risk": "READ",
             "revit": ["2024"],
             "contract": {"needs": [], "provides": []}}
    named = ask({"capability": "DO_A_THING", "has": [], "wants": ["size"]},
                [lying], revit="2024")
    check(not named["matched"],
          "a card whose id says 'filter-by-size' and whose contract "
          "declares no size does not match")
    check(named["partial"][0]["undelivered"] == ["size"],
          "and the contract is what the answer talks about")
    almost = ask({"capability": "APPLY_VIEW_FILTER", "has": [], "wants": []},
                 [card("FRG-X-005", "CREATE_VIEW_FILTER", ["2024"])],
                 revit="2024")
    check(not almost["matched"] and not almost["partial"],
          "and a NEAR CAPABILITY is not a near match at all - "
          "CREATE_VIEW_FILTER is not APPLY_VIEW_FILTER")
    check("equal, not similar" in whole,
          "the agent says capability equality is exact")

    print("\n4. Nothing matching produces a brief")
    nothing = ask({"capability": "NOBODY_DOES_THIS", "has": [],
                   "wants": ["x"]},
                  [short, perfect_but_old], revit="2024")
    check(nothing["brief"] is not None, "a brief comes back")
    check(nothing["brief"]["capability"] == "NOBODY_DOES_THIS"
          and nothing["brief"]["revit"] == "2024",
          "naming what was wanted and on which release")
    check(nothing["brief"]["excluded_for_version"] == 1,
          "and how many were excluded for their version: %d"
          % nothing["brief"]["excluded_for_version"])
    with_near = ask({"capability": "DO_A_THING", "has": [],
                     "wants": ["done"]}, [short], revit="2024")
    check(with_near["brief"]["nearest"] == "FRG-X-003"
          and sorted(with_near["brief"]["missing"]) == ["filter", "view"],
          "when something came close, the brief names it and what it was "
          "short of")
    found = ask(want, [card("FRG-X-006", "DO_A_THING", ["2024"],
                            needs=["view"], provides=["done"])],
                revit="2024")
    check(found["brief"] is None,
          "and there is no brief when something matched - it is not a key "
          "that is always there saying nothing")

    print("\n5. Status is reported and not preferred")
    mixed = ask(want, [card("FRG-DRAFT-1", "DO_A_THING", ["2024"],
                            needs=["view"], provides=["done"],
                            status="DRAFT"),
                       card("FRG-PROVEN-1", "DO_A_THING", ["2024"],
                            needs=["view", "extra"], provides=["done"],
                            status="PROVEN")], revit="2024")
    check([one["id"] for one in mixed["matched"]] == ["FRG-DRAFT-1"],
          "the DRAFT fragment that fits IS the match")
    check([one["id"] for one in mixed["partial"]] == ["FRG-PROVEN-1"],
          "and the PROVEN one that does not fit is partial")
    check(mixed["matched"][0]["status"] == "DRAFT",
          "the status is on the match, reported")
    check(all(key in mixed["matched"][0] for key in ("status", "source",
                                                     "risk")),
          "with source and risk beside it, so the caller can weigh them")
    # STRUCTURAL, not a word search: "ranking" and "weighting" appear in the
    # agent's own sentences saying it does NOT do them.
    for ranking in ("sorted(matched", "matched.sort", "sorted(self",
                    "key=lambda one: one[\"status\"]"):
        check(ranking not in logic, "nothing sorts the matches (%s)" % ranking)
    check("sorted(partial" in logic,
          "the one sort in the agent is over PARTIALS, to find the nearest "
          "for the brief - not over matches")
    check([one["id"] for one in mixed["matched"] + mixed["partial"]]
          == ["FRG-DRAFT-1", "FRG-PROVEN-1"],
          "and both lists come back in the order the cards were handed in")

    print("\n6. It runs on the real library")
    library, problems = FRAG.load_all()
    check(len(library) >= 350, "%d fragment cards load" % len(library))
    real = ask({"capability": "APPLY_VIEW_FILTER",
                "has": ["view", "filter", "overrides", "visible"],
                "wants": ["applied"]}, library, revit="2024")
    check(not real.get("refused"), "the whole library matches without error")
    check(real["of"] == len(library),
          "every card was considered: %d of %d" % (real["of"], len(library)))
    landed = (len(real["matched"]) + len(real["partial"])
              + len(real["excluded"]))
    check(landed <= real["of"],
          "and every card is in at most one list (%d of %d - the rest are "
          "a different capability)" % (landed, real["of"]))
    check(real["matched"] or real["partial"],
          "something in the real library answers APPLY_VIEW_FILTER: %d "
          "matched, %d partial"
          % (len(real["matched"]), len(real["partial"])))

    print("\n7. Every failure is named and reached")
    for bad, why in ((None, "None is not a request"),
                     ("APPLY_VIEW_FILTER", "a string is not a request"),
                     ({}, "a request with no capability"),
                     ({"capability": "  "}, "and a blank one")):
        check(ask(bad, [short], revit="2024").get("refused")
              == "NOT_A_REQUEST", why)
    check(ask(want, [], revit="2024").get("refused") == "NOTHING_TO_MATCH",
          "no candidates is refused - RET-003 retrieves them, this agent "
          "does not go looking")
    for broken, why in (("not a card", "a card that is not a map"),
                        ({"capability": "DO_A_THING"}, "one with no id"),
                        ({"id": "FRG-X-009"}, "and one with no capability")):
        check(ask(want, [broken], revit="2024").get("refused")
              == "NOT_A_FRAGMENT", why)
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-RAG-FMT-004.yaml"))
    named_failures = contract.get("failures") or []
    check(len(named_failures) == 4, "the contract declares 4 failures")
    for failure in named_failures:
        check(failure in logic, "the code names %s" % failure)
    check("WRONG_REVIT_VERSION" in logic,
          "and the exclusion reason is named too")
    unreached = sorted(set(named_failures) - reached)
    check(not unreached,
          "every declared failure was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    for forbidden in ("open(", "os.listdir", "subprocess", "exec("):
        check(forbidden not in logic, "the code never uses %s" % forbidden)
    check(found["ran"] is False and len(found["unjudged"]) == 4,
          "`ran` is false and four things are left unjudged")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    a near match is not a match, and the version is a wall")
    return 0


if __name__ == "__main__":
    sys.exit(main())
