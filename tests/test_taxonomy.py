# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-NAM-TAX-004
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Taxonomy - it measures the scheme and never changes it.

    python tests/test_taxonomy.py

WHAT IT PROVES
  1. THE SCHEME IS heron_fragment's OWN OBJECT, compared by identity,
     and the module holds no copy of the nine areas.

  2. IT MEASURES EVERY FRAGMENT ON DISK, and every one of them names a
     declared area. The number is not typed here - it was 360 until the
     library passed it (docs/FRAGMENT-ISSUES row 5b-66).

  3. `splits_to_fall` IS ARITHMETIC, checked on numbers whose answer can
     be worked out by hand - including the two edges, not the largest
     and already tied.

  4. NO THRESHOLD AND NO HINT, proved by behaviour: an area holding 99%
     produces the same shape of answer as one holding 11%, with no key
     that judges and no word that recommends.

  5. AN UNDECLARED AREA REFUSES THE WHOLE REPORT - it is not a warning
     beside a usable answer.

  6. A PROPOSAL COMES BACK AS A PROPOSAL, with the real counts in it.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE AND
     REACHED, and the code refuses nothing the contract omits.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_taxonomy as TAX                                   # noqa: E402
import heron_fragment as FRAG                                  # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_taxonomy.py"),
                    encoding="utf-8").read()
    code = whole.split("\nfrom __future__", 1)[1]
    # THE AGENT ITSELF, without its demo. main() reads the fragment cards
    # off disk and names an area to propose about - both fine in a demo
    # and neither allowed in the agent, so the two are checked apart.
    logic = code.split("\ndef main(")[0]

    def ask(ids, **kw):
        answer = TAX.review(ids, **kw)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    print("1. The scheme is heron_fragment's own object")
    check(TAX.FRAG.AREAS is FRAG.AREAS,
          "the area list IS heron_fragment's, not an equal copy")
    for area in FRAG.AREAS:
        check('"%s"' % area not in logic and "'%s'" % area not in logic,
              "the agent holds no literal '%s'" % area)
    check("FRAG.AREAS" in logic, "it reaches for the list through the module")
    check('"VIEW"' in code,
          "and the one place an area IS named is the demo, which is not "
          "the agent")
    fragment_source = io.open(os.path.join(ROOT, "brain",
                                           "heron_fragment.py"),
                              encoding="utf-8").read()
    check("Adding an area is a deliberate edit here" in fragment_source,
          "and heron_fragment really does say adding one is a deliberate "
          "edit there")

    print("\n2. It measures the real fragments on disk")
    folder = os.path.join(ROOT, "brain", "fragments")
    ids = []
    for entry in sorted(os.listdir(folder)):
        card = os.path.join(folder, entry, "fragment.yaml")
        if not os.path.isfile(card):
            continue
        for line in io.open(card, encoding="utf-8"):
            if line.strip().startswith("id:"):
                ids.append(line.split(":", 1)[1].strip().strip('"').strip("'"))
                break
    live = ask(ids)
    check(len(ids) >= 350, "%d fragments on disk declare an id" % len(ids))
    check(not live.get("refused"),
          "and every one names a declared area - the report is clean")
    check(live["of"] == len(ids), "all %d were placed" % len(ids))
    check(sum(row["held"] for row in live["areas"]) == len(ids),
          "and the areas add back up to that")
    check(len(live["areas"]) == len(FRAG.AREAS) == 9,
          "nine areas, as heron_fragment declares")
    check(round(sum(row["share"] for row in live["areas"])) == 100,
          "the shares add to 100%")
    check(live["areas"][0]["held"] >= live["areas"][-1]["held"],
          "and the list is ordered, largest first")

    print("\n3. splits_to_fall is arithmetic, not an opinion")
    for counts, area, wanted, why in (
            ({"A": 100, "B": 55}, "A", 1, "100 against 55: one split gives "
                                          "50, which is under 55"),
            ({"A": 100, "B": 30}, "A", 3, "100 against 30: into four, so "
                                          "three splits"),
            ({"A": 10, "B": 55}, "A", 0, "not the largest already"),
            ({"A": 55, "B": 55}, "A", 0, "tied is not larger"),
            ({"A": 7}, "A", 0, "and one area alone cannot fall")):
        got = TAX._splits_to_fall(counts, area)
        check(got == wanted, "%s -> %d (%s)" % (counts, got, why))
    check(TAX._splits_to_fall({}, "A") == 0,
          "an empty scheme answers zero rather than looping")
    # THE CASE THAT LOOPED FOREVER ON THE FIRST RUN. Dividing a positive
    # count never reaches zero, and one id in one area is what a NEW
    # workspace has - the first thing this agent would be asked about.
    check(TAX._splits_to_fall({"A": 1, "B": 0}, "A") is None,
          "with every other area empty the answer is None, not a number "
          "and not a hang")
    lonely = ask(["FRG-SEL-001"])
    check(lonely["splits_to_fall"] is None,
          "a scheme holding one id answers None")
    check("no number of even splits" in
          " ".join(lonely["unjudged"]).lower(),
          "and says in words that no number of splits would do it")
    check(lonely["areas"][0]["share"] == 100.0,
          "while still reporting the share - 100%, with no verdict "
          "attached to it")

    print("\n4. No threshold and no hint - by behaviour")
    lopsided = ask(["FRG-VIEW-%03d" % n for n in range(1, 100)]
                   + ["FRG-SEL-001"])
    even = ask(["FRG-VIEW-001", "FRG-SEL-001", "FRG-ELE-001", "FRG-QA-001"])
    check(sorted(lopsided) == sorted(even),
          "an area holding 99%% gives the SAME KEYS as one holding 25%% - "
          "nothing appears only when something is wrong")
    for judging in ("warning", "warn", "alert", "recommend", "recommended",
                    "should", "too_many", "advice", "verdict", "severity"):
        check(judging not in lopsided,
              "no '%s' key, whatever the numbers" % judging)
    # And the one place the word DOES appear says the opposite of a verdict.
    check(any("NOTHING IS RECOMMENDED" in line
              for line in lopsided["unjudged"]),
          "the only mention of recommending is `unjudged` saying nothing "
          "is recommended")
    check(lopsided["splits_to_fall"] == 98,
          "99 against 1 needs 98 even splits, and that number is all that "
          "is offered - large, unflattering and not a verdict: %d"
          % lopsided["splits_to_fall"])
    check("No threshold is applied" in lopsided["why"],
          "the answer says plainly that no threshold is applied")
    check(any("hidden rather than avoided" in line
              for line in lopsided["unjudged"]),
          "and why a hint would be a threshold hidden rather than avoided")

    print("\n5. An undeclared area refuses the whole report")
    mixed = ask(["FRG-SEL-001", "FRG-MECH-001", "FRG-MECH-002",
                 "FRG-HVAC-001"])
    check(mixed.get("refused") == "AREA_NOT_DECLARED",
          "three ids name two areas the scheme does not have")
    check(sorted(mixed["undeclared"]) == ["HVAC", "MECH"],
          "and both are named")
    check(mixed["undeclared"]["MECH"] == ["FRG-MECH-001", "FRG-MECH-002"],
          "with the ids that used each")
    check("areas" not in mixed,
          "and NO distribution comes back beside it - an unlisted area is "
          "an error, not a warning next to a usable answer")
    check("MECH" in code,
          "the refusal quotes heron_fragment's own example of what goes "
          "wrong")
    # Genuinely unshaped - note "not-an-id" would NOT be: three parts makes
    # "an" an area, and that is an AREA_NOT_DECLARED rather than a shape.
    shaped = ask(["FRG-SEL-001", "notanid", "FRG-SEL", "a-b-c-d"])
    check(not shaped.get("refused") and len(shaped["unshaped"]) == 3,
          "while ids that are not shaped like one are carried, not refused "
          "and not dropped")
    check(any("Golden Rule 14" in line for line in shaped["unjudged"]),
          "and the answer says which rule that is")

    print("\n6. A proposal comes back as a proposal")
    asked = ask(ids, propose={"do": "split", "area": "VIEW"})
    check(asked.get("refused") == "THE_SCHEME_IS_NOT_CHANGED_HERE",
          "splitting VIEW is refused")
    check(asked["changed"] is False, "and `changed` is false")
    case = asked["case"]
    check("Proposal: split VIEW" in case, "the case names the proposal")
    check(str(live["areas"][0]["held"]) in case,
          "and carries the real count (%d)" % live["areas"][0]["held"])
    check("deliberate edit to heron_fragment.AREAS" in case,
          "and says where the deliberate edit would be made")
    check("No threshold is applied here and none is implied" in case,
          "and that it is not recommending it")
    check("areas" in asked,
          "the measurement comes back too - whoever decides has the "
          "numbers, not an impression")
    for what in TAX.PROPOSALS:
        one = ask(ids, propose={"do": what, "area": "VIEW"})
        check(one.get("refused") == "THE_SCHEME_IS_NOT_CHANGED_HERE",
              "'%s' is refused the same way" % what)
    check(ask(ids, propose={"do": "reorder", "area": "VIEW"}).get("refused")
          == "NOT_A_PROPOSAL",
          "and something that is not one of the five is a different refusal")

    print("\n7. Every failure is named and reached")
    check(ask([]).get("refused") == "NOTHING_TO_CLASSIFY",
          "an empty list is refused - it would report every area unused")
    check(ask(None).get("refused") == "NOTHING_TO_CLASSIFY",
          "and so is None")
    for bad in (None, {}, [], "ELE"):
        if bad is None:
            continue
        check(ask(["FRG-SEL-001"], scheme=bad).get("refused")
              == "NOT_A_SCHEME",
              "%r is not a scheme" % (bad,))
    for forbidden in ("open(", "shutil", "os.rename", "write("):
        check(forbidden not in logic, "the agent never uses %s" % forbidden)
    check("io.open" in code,
          "while the demo does read the cards off disk - that is the demo, "
          "and the agent is handed its ids")
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-NAM-TAX-004.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 5, "the contract declares 5 failures")
    for failure in named:
        check(failure in code, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    spare = sorted(reached - set(named))
    check(not spare,
          "and the code refuses nothing the contract omits%s"
          % ("" if not spare else ": %s" % ", ".join(spare)))
    check(len(live["unjudged"]) == 4, "four things are left unjudged")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    it measures the scheme and never changes it")
    return 0


if __name__ == "__main__":
    sys.exit(main())
