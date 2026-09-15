# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-FRG-CRE-007
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Authoring a fragment - the gate is the Matcher's, not a percentage.

    python tests/test_generate.py

WHAT IT PROVES
  1. docs/09 s151 ASKS FOR >=80%, AND THE MATCHER RETURNS NO SCORE -
     both read from the source rather than described. That is F24, and
     the gate is built in the Matcher's vocabulary instead.

  2. THE GATE IS STRICTER THAN THE PERCENTAGE. Anything in `matched` or
     `partial` refuses, and the two refuse under different names so a
     caller can tell "already done" from "start from it".

  3. THE GATE RUNS FIRST. A draft that is also incomplete, at the wrong
     status and short of cases still comes back as the Matcher's
     refusal.

  4. A NEGATIVE CASE IS REQUIRED. A positive one alone shows the
     fragment does something; only a negative shows it is not doing it
     to everything.

  5. THE CAPABILITY MUST BE FREE, and an id already in use is refused
     separately - they are different collisions.

  6. IT ENTERS AT DRAFT AND THE FOLDER IS DERIVED, never typed.

  7. THE REQUIRED FIELDS ARE heron_fragment.REQUIRED's, split on the
     metadata prefix.

  8. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_generate as CRE                                   # noqa: E402
import heron_fragment as FRAG                                  # noqa: E402
import heron_matcher as FMT                                    # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []

# The Matcher's real shape. `ran` is False on its success path too -
# it means nothing was EXECUTED, which is that agent's whole point.
EMPTY = {"ran": False, "matched": [], "partial": [], "excluded": []}
CASES = {"positive": [{"given": "a model with ducts"}],
         "negative": [{"given": "a model with none", "expect": "empty"}]}


def draft(**changes):
    card = {"id": "FRG-T-001", "semantic-identity": "count the ducts",
            "kind": "query", "domain": "revit.reporting",
            "capability": "COUNT_DUCTS", "version": "1.0.0",
            "source": "OFFICIAL", "risk": "READ",
            "purpose": "Counts ducts and says which filter it used.",
            "contract": {"needs": [], "provides": []},
            "revit": ["2024"], "runtime": "net8.0-windows",
            "utterances": ["how many ducts"]}
    card.update(changes)
    return card


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_generate.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]
    doc = " ".join(io.open(
        os.path.join(ROOT, "docs", "09-skills-and-fragments.md"),
        encoding="utf-8").read().replace("*", "").split())
    matcher = io.open(os.path.join(ROOT, "brain", "heron_matcher.py"),
                      encoding="utf-8").read()

    print("\n1. docs/09 asks for 80%, and the Matcher returns no score")
    check("If a proven fragment covers ≥80% of the request, generation is "
          "not permitted to start from scratch" in doc,
          "docs/09 s151 carries the 80% sentence verbatim")
    check("the Fragment Matcher must have searched and reported" in doc,
          "and requires the Matcher to have reported first")
    # THE PRODUCER THAT DOES NOT PRODUCE IT. Checked on a SUCCESSFUL
    # match - a refusal obviously carries no score, and proving it there
    # would prove nothing.
    found, _ = FRAG.load_all()
    real = [one for one in found.values()
            if one.data.get("capability") == "COUNT_ELEMENTS"]
    answer = FMT.match({"capability": "COUNT_ELEMENTS", "has": [],
                        "wants": []}, real, revit="2024")
    check(not answer.get("refused"),
          "a real match succeeds - %d matched, %d partial - and `ran` is "
          "False even so, because nothing was EXECUTED"
          % (len(answer.get("matched") or []),
             len(answer.get("partial") or [])))
    check(answer.get("ran") is False,
          "which is why a refusal is told by `refused` and never by `ran`")
    check("score" not in answer and "coverage" not in answer,
          "and carries no score and no coverage: %s"
          % ", ".join(sorted(answer)))
    for one in (answer.get("matched") or []) + (answer.get("partial") or []):
        check("score" not in one and "coverage" not in one,
              "  nor does any row it returns")
    check("a near match is not a match" in matcher.lower(),
          "and its module says why - a near match is not a match")
    check(any("F24" in line for line in
              CRE.author(draft(), EMPTY, CASES)["unjudged"]),
          "so this agent's answer points at PROPOSALS F24")

    print("\n2. the gate is stricter than the percentage")
    hit = CRE.author(draft(), dict(EMPTY, matched=[{"fragment": "FRG-X"}]),
                     CASES)
    reached.add(hit.get("refused"))
    check(hit["refused"] == "REUSE_EXISTS", "`matched` refuses")
    near = CRE.author(draft(), dict(EMPTY, partial=[{"fragment": "FRG-Y"}]),
                      CASES)
    reached.add(near.get("refused"))
    check(near["refused"] == "START_FROM_IT", "`partial` refuses too")
    check(hit["refused"] != near["refused"],
          "under DIFFERENT names, so a caller can tell 'already done' "
          "from 'start from it'")
    check(near["found"] == ["FRG-Y"] and near["where"] == "partial",
          "and the answer names what to start from: %s"
          % ", ".join(near["found"]))
    check("Golden Rule 3" in hit["why"],
          "citing reuse proven knowledge before creating new")
    # A PERCENTAGE WOULD LET 79% THROUGH. `partial` does not.
    check(CRE.author(draft(), dict(EMPTY, excluded=[{"fragment": "FRG-Z"}]),
                     CASES)["authored"] is True,
          "while `excluded` - wrong Revit release - does not stop it, "
          "because it is not reusable here at all")

    # AND END TO END, against a real Matcher answer rather than a
    # fixture. The first version of this agent read `ran` as failure and
    # would have refused every genuine report.
    real_hit = FMT.match({"capability": "COUNT_ELEMENTS", "has": [],
                          "wants": []}, real, revit="2024")
    check(CRE.author(draft(), real_hit, CASES)["refused"] == "REUSE_EXISTS",
          "a REAL matcher answer that found something refuses the "
          "generation")
    real_miss = FMT.match({"capability": "NOTHING_PROVIDES_THIS",
                           "has": [], "wants": []}, real, revit="2024")
    check(CRE.author(draft(capability="NOTHING_PROVIDES_THIS"), real_miss,
                     CASES)["authored"] is True,
          "and a real one that found nothing lets it through")

    print("\n3. the gate runs first")
    worst = CRE.author(
        dict(draft(id="x", **{"heron-status": "PROVEN"}), capability=""),
        dict(EMPTY, matched=[{"fragment": "FRG-X"}]), None)
    check(worst["refused"] == "REUSE_EXISTS",
          "incomplete AND wrong status AND no cases still comes back as "
          "the Matcher's refusal - the gate is before all of it")
    for report, name in ((None, "NO_MATCHER_REPORT"),
                         ({"ran": False}, "NO_MATCHER_REPORT"),
                         ("a string", "NO_MATCHER_REPORT")):
        got = CRE.author(draft(), report, CASES)
        reached.add(got.get("refused"))
        check(got.get("refused") == name,
              "and no report at all is %s" % name)
    check(CRE.author(draft(), None, CASES).get("asked"),
          "with the question asked: what did the Matcher find?")

    print("\n4. a negative case is required")
    for cases in (None, {}, {"positive": [{"given": "x"}]},
                  {"negative": []}):
        got = CRE.author(draft(), EMPTY, cases)
        reached.add(got.get("refused"))
        check(got.get("refused") == "NO_CASES",
              "%r is not enough" % (cases,))
    bare = CRE.author(draft(), EMPTY, {"positive": [{"given": "x"}]})
    check("not doing it to everything" in bare["why"],
          "because only a negative case shows it is not doing it to "
          "everything")
    check(bare.get("asked"),
          "and it asks what it must come back empty for: %r"
          % bare["asked"])
    only_negative = CRE.author(draft(), EMPTY,
                               {"negative": [{"given": "x"}]})
    check(only_negative["authored"] is True
          and only_negative["cases"] == {"positive": 0, "negative": 1},
          "a negative case alone is enough, and both counts come back")

    print("\n5. the capability must be free")
    taken = [{"id": "FRG-OLD-001", "capability": "COUNT_DUCTS"}]
    clash = CRE.author(draft(library=taken), EMPTY, CASES)
    reached.add(clash.get("refused"))
    check(clash["refused"] == "CAPABILITY_TAKEN",
          "a capability already claimed is refused")
    check(clash["by"] == "FRG-OLD-001", "naming who claims it")
    check("would make its answer arbitrary" in clash["why"],
          "because the Matcher finds a provider BY capability")
    same_id = CRE.author(draft(id="FRG-OLD-001", capability="SOMETHING_ELSE",
                               library=taken), EMPTY, CASES)
    reached.add(same_id.get("refused"))
    check(same_id["refused"] == "ALREADY_EXISTS",
          "and an id already in use is a DIFFERENT refusal")
    check("HERON-FRG-UPD-008" in same_id["why"],
          "naming the agent allowed to change an existing fragment")
    check(CRE.author(draft(capability="BRAND_NEW", library=taken),
                     EMPTY, CASES)["authored"] is True,
          "a free id and a free capability go through")

    print("\n6. it enters at DRAFT and the folder is derived")
    good = CRE.author(draft(), EMPTY, CASES)
    check(good["status"] == CRE.ENTERS_AT == "DRAFT",
          "the status written is DRAFT")
    check(CRE.ENTERS_AT in FRAG.STATUSES,
          "read off docs/09's ladder in heron_fragment")
    high = CRE.author(draft(**{"heron-status": "PROVEN"}), EMPTY, CASES)
    reached.add(high.get("refused"))
    check(high["refused"] == "NOT_A_DRAFT",
          "a draft arriving higher is refused, not lowered")
    check(good["folder"].endswith(FRAG.folder_for("COUNT_DUCTS")),
          "and the folder is heron_fragment.folder_for's: %s"
          % os.path.basename(good["folder"]))
    check(good["card"]["heron-agent"] == "HERON-FRG-CRE-007",
          "the card records which agent authored it")

    print("\n7. the required fields are heron_fragment's")
    check(tuple(sorted(CRE.AUTHORS + CRE.WRITES))
          == tuple(sorted(FRAG.REQUIRED)),
          "AUTHORS and WRITES together ARE heron_fragment.REQUIRED")
    check(not set(CRE.AUTHORS) & set(CRE.WRITES),
          "and no field is in both: the author supplies %d, the agent "
          "writes %d" % (len(CRE.AUTHORS), len(CRE.WRITES)))
    check(all(field in good["card"] for field in FRAG.REQUIRED),
          "so every required field is on the written card")
    for writing in ("open(", "write(", "makedirs", "subprocess"):
        check(writing not in logic, "the agent never uses %s" % writing)

    print("\n8. every declared failure is named and reached")
    for card, report, cases, name in (
            (None, EMPTY, CASES, "NOTHING_TO_AUTHOR"),
            ("a string", EMPTY, CASES, "NOTHING_TO_AUTHOR"),
            ({"id": "x"}, EMPTY, CASES, "INCOMPLETE")):
        got = CRE.author(card, report, cases)
        reached.add(got.get("refused"))
        check(got.get("refused") == name, "%s is reached" % name)

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-FRG-CRE-007.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 9, "the contract declares 9 failures")
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
    print("PASS    the gate is the Matcher's, not a percentage")
    return 0


if __name__ == "__main__":
    sys.exit(main())
