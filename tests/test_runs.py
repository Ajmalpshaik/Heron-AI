# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-FRG-PRF-002
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Fragment performance - the arithmetic is imported, the findings are new.

    python tests/test_runs.py

WHAT IT PROVES
  1. THE COUNTING IS HERON-SKL-PRF-006's, BY RESULT. The same runs
     handed to both give identical per-release rows - so there is one
     implementation, not two that agree today.

  2. ITS REFUSALS ARE PASSED THROUGH IN ITS OWN WORDS, with `refused_by`
     naming it - never re-worded, and never re-raised under a new name.

  3. A PROOF THE RUNS CONTRADICT IS REPORTED AND NOT RESOLVED. The
     answer names both records and picks neither.

  4. A PROOF ON A RELEASE WITH NO RUNS IS A DIFFERENT FINDING from one
     the runs contradict, and the two never both fire.

  5. A RUN ON A RELEASE THAT DOES NOT BUILD IS REPORTED - and with no
     matrix the answer says the check did not happen, rather than
     implying it passed.

  6. THE PROOF'S RELEASE IS READ BY HERON-FRG-MTX-009, not re-parsed.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_runs as RUNS                                      # noqa: E402
import heron_performance as SKL                                # noqa: E402
import heron_matrix as MTX                                     # noqa: E402
import heron_fragment as FRAG                                  # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []

EIGHT = list(FRAG.REVIT_VERSIONS)


def card(**changes):
    one = {"id": "FRG-T-001", "revit": ["2023", "2024", "2025"]}
    one.update(changes)
    return one


def fragment(proof_model=None, **changes):
    """A real Fragment object, so `proof` and `slug` behave as they do."""
    data = card(**changes)
    if proof_model is not None:
        data["proof"] = {"model": proof_model}
    return FRAG.Fragment(data, os.path.join(ROOT, "brain", "fragments",
                                            "a-test-fragment"))


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_runs.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]

    some = [{"revit": "2024", "outcome": "ok", "seconds": 1.2},
            {"revit": "2024", "outcome": "failed", "seconds": 0.4},
            {"revit": "2023", "outcome": "ok", "seconds": 0.9}]

    print("\n1. the counting is HERON-SKL-PRF-006's, by result")
    mine = RUNS.measure(fragment(), some)
    theirs = SKL.measure(some, declared=["2023", "2024", "2025"])
    check(mine["versions"] == theirs["versions"],
          "the per-release rows are IDENTICAL to what that agent returns")
    check(mine["untested"] == theirs["untested"] == ["2025"],
          "and so is the untested list")
    check(mine["never_succeeded"] == theirs["never_succeeded"],
          "and so is never_succeeded")
    check(RUNS.FAILED is SKL.FAILED and RUNS.OK is SKL.OK,
          "the outcome words are that module's objects, not copies")
    check(mine["unjudged"][:4] == theirs["unjudged"],
          "its four unjudged lines come through unchanged, and this "
          "agent adds its own after them")
    check(len(mine["unjudged"]) == 7,
          "seven in all - four inherited, three about the fragment")

    print("\n2. its refusals pass through in its own words")
    for runs, name in (([], "NOTHING_TO_MEASURE"),
                       ([{"revit": "1999", "outcome": "ok"}],
                        "NOT_A_VERSION"),
                       ([{"revit": "2024", "outcome": "maybe"}],
                        "NOT_AN_OUTCOME"),
                       (["not a dict"], "NOT_A_RUN")):
        answer = RUNS.measure(fragment(), runs)
        check(answer.get("refused") == name, "%s comes through" % name)
        check(answer.get("refused_by") == "HERON-SKL-PRF-006",
              "  named as HERON-SKL-PRF-006's, not re-raised as this "
              "agent's")
        check(answer["why"] == SKL.measure(runs, declared=["2023"])["why"],
              "  in that agent's own words, unaltered")
    check("NOTHING_TO_MEASURE" not in logic and "NOT_AN_OUTCOME"
          not in logic,
          "and this module does not name those failures at all - it "
          "could not raise one if it wanted to")

    print("\n3. a contradicted proof is reported, not resolved")
    told = RUNS.measure(fragment(proof_model="Tower A, Revit 2024"), some)
    check(told["proof_on"] == "2024", "the proof stands on 2024")
    got = told["contradicted"]
    check(got and got["revit"] == "2024" and got["failed"] == 1
          and got["runs"] == 2,
          "and 1 of 2 runs there failed, both counts named")
    check("nothing here picks between them" in got["why"],
          "with neither record overruled - a proof is a person's evidence "
          "and a failure is a machine's")
    clean = RUNS.measure(fragment(proof_model="Tower A, Revit 2023"), some)
    check(clean["contradicted"] is None,
          "a proof on a release whose runs all passed is not contradicted")

    print("\n4. an unwatched proof is a different finding")
    quiet = RUNS.measure(fragment(proof_model="Tower A, Revit 2025"), some)
    check(quiet["unwatched_since_proof"] is not None
          and quiet["unwatched_since_proof"]["revit"] == "2025",
          "a proof on a release with NO runs is reported")
    check(quiet["contradicted"] is None,
          "and it is not also 'contradicted' - nothing contradicted it")
    check("not the same as evidence that it still works"
          in quiet["unwatched_since_proof"]["why"],
          "with the reason: one recorded event is what D-30 asks for")
    check(not (told["contradicted"] and told["unwatched_since_proof"]),
          "the two findings never both fire for one release")

    print("\n5. a run on a release that does not build")
    watched = RUNS.measure(
        fragment(revit=EIGHT), some,
        matrix={"2024": MTX.PROVEN, "2023": MTX.UNKNOWN,
                "2025": MTX.COMPILES})
    off = watched["ran_where_it_does_not_build"]
    check([one["revit"] for one in off] == ["2023"],
          "2023 carries runs and the matrix calls it unknown")
    check(off[0]["state"] == MTX.UNKNOWN and off[0]["runs"] == 1,
          "the answer names the state and the count")
    check("One of the two records is wrong about the world" in off[0]["why"],
          "and says so, rather than deciding which")
    check(watched["ready_to_prove"] == ["2025"],
          "2025 builds, is declared and has no runs - ready to prove")
    check(MTX.COMPILES in RUNS.BUILT and MTX.PROVEN in RUNS.BUILT
          and MTX.UNKNOWN not in RUNS.BUILT,
          "the states that count as built are HERON-FRG-MTX-009's names")
    blind = RUNS.measure(fragment(), some)
    check(blind["ran_where_it_does_not_build"] == []
          and blind["ready_to_prove"] == [],
          "with no matrix, neither list is filled")
    check(any("NO MATRIX WAS HANDED IN" in line
              for line in blind["unjudged"]),
          "and the answer says the check did not happen, rather than "
          "implying it passed")

    print("\n6. the proof's release is read by the agent that owns it")
    one = fragment(proof_model="Tower A, Revit 2024")
    check(RUNS.measure(one, some)["proof_on"] == MTX.proof_release(one)[0],
          "proof_on is exactly MTX.proof_release's answer")
    # AMBIGUOUS IS NOT GOOD NEWS.
    two = RUNS.measure(fragment(proof_model="Revit 2023 and Revit 2024"),
                       some)
    check(two["proof_on"] is None and "more than one"
          in two["no_proof_because"],
          "a proof naming two releases gives NO release and says why")
    check(two["contradicted"] is None and two["unwatched_since_proof"]
          is None,
          "and neither proof finding fires on it")
    none = RUNS.measure(fragment(), some)
    check(none["proof_on"] is None
          and none["no_proof_because"] == "no proof",
          "a fragment with no proof says 'no proof'")

    print("\n7. every declared failure is named and reached")
    for frag, runs, name in (
            (None, some, "NOT_A_FRAGMENT"),
            ("a string", some, "NOT_A_FRAGMENT"),
            ({"id": "  "}, some, "NOT_A_FRAGMENT"),
            (card(revit=[]), some, "NO_DECLARED_VERSIONS")):
        answer = RUNS.measure(frag, runs)
        reached.add(answer.get("refused"))
        check(answer.get("refused") == name, "%s is reached" % name)
    check(RUNS.measure(card(revit=[]), some).get("asked"),
          "and a fragment declaring no releases is ASKED which it supports")

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-FRG-PRF-002.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 2,
          "the contract declares 2 failures - the ones this agent raises")
    for failure in named:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the arithmetic is imported, the findings are new")
    return 0


if __name__ == "__main__":
    sys.exit(main())
