# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-LRN-OBS-001
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Learning observation - a corrected success is a failure signal.

    python tests/test_observe.py

WHAT IT PROVES
  1. FAILURES COME FIRST, whatever order they arrived in.

  2. A SUCCESS SOMEBODY CORRECTED IS ITS OWN SIGNAL - not clean, and not
     filed with the successes.

  3. NO RATE IS RETURNED, at any mix, and the counts are there instead.

  4. NOTHING IS CONCLUDED - no pattern, no suggestion, no key that
     appears only when something looks bad.

  5. THE WINDOW TRAVELS, because what was not recorded is not what did
     not happen.

  6. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_observe as OBS                                    # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def decision_log():
    """DECISIONS.md and every record under docs/decisions/. Since 2026-09-23
    each decision's full text is its own file and DECISIONS.md is the index
    (tools/split-decisions.py), so what a decision says is in one of them."""
    paths = [os.path.join(ROOT, "docs", "DECISIONS.md")]
    folder = os.path.join(ROOT, "docs", "decisions")
    if os.path.isdir(folder):
        paths += [os.path.join(folder, n) for n in sorted(os.listdir(folder)) if n.endswith(".md")]
    return " ".join(io.open(p, encoding="utf-8").read() for p in paths)


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_observe.py"),
                    encoding="utf-8").read()
    code = whole.split("\nfrom __future__", 1)[1]
    logic = code.split("\ndef main(")[0]

    def ask(runs):
        answer = OBS.observe(runs)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    def run(when, outcome="ok", **kw):
        base = {"workflow": "w", "when": when, "outcome": outcome}
        base.update(kw)
        return base

    print("1. Failures come first")
    row = [line for line in io.open(
        os.path.join(ROOT, "docs", "28-agent-registry.md"),
        encoding="utf-8").read().splitlines() if "LRN-OBS-001" in line][0]
    check("Failure is the higher-value signal" in row,
          "docs/28 says failure is the higher-value signal")
    mixed = ask([run("2026-09-01"), run("2026-09-02", "failed"),
                 run("2026-09-03", corrections=["by hand"]),
                 run("2026-09-04", "failed")])
    signals = [one["signal"] for one in mixed["observations"]]
    check(signals == ["failed", "failed", "corrected", "clean"],
          "handed in mixed, they come back failures first: %s"
          % ", ".join(signals))
    check(OBS.SIGNALS == ("failed", "corrected", "clean"),
          "and the order is declared, not incidental")
    check([one["when"] for one in mixed["observations"][:2]]
          == ["2026-09-02", "2026-09-04"],
          "with the failures in the order they happened")
    check("higher-value signal" in mixed["observations"][0]["why"],
          "and each failure says why it is worth more")

    print("\n2. A corrected success is its own signal")
    corrected = ask([run("2026-09-01", "ok",
                         corrections=["user re-tagged 6 of 47"])])
    one = corrected["observations"][0]
    check(one["signal"] == "corrected",
          "it is not 'clean' although its outcome is ok")
    check(one["outcome"] == "ok",
          "and its outcome is still reported as ok, unchanged")
    check(one["corrections"] == ["user re-tagged 6 of 47"],
          "with what was corrected carried through")
    check("every outcome count loses" in one["why"],
          "and why every outcome count loses it")
    check(corrected["counts"]["clean"] == 0
          and corrected["counts"]["corrected"] == 1,
          "it is counted as corrected and not as clean")
    plain = ask([run("2026-09-01", "ok")])
    check(plain["observations"][0]["signal"] == "clean",
          "while the same run with no correction is clean")
    check(ask([run("2026-09-01", "ok", corrections=[])]
              )["observations"][0]["signal"] == "clean",
          "and an empty correction list is not a correction")

    print("\n3. No rate is returned")
    for runs in ([run("1", "failed")], [run("1")],
                 [run("1"), run("2", "failed")],
                 [run("%d" % n) for n in range(20)]):
        answer = ask(runs)
        for rate in ("rate", "percent", "percentage", "ratio", "score",
                     "success_rate", "health"):
            check(rate not in answer, "no '%s' key" % rate)
    check(sorted(mixed["counts"]) == ["clean", "corrected", "failed"],
          "the counts are there instead: %s" % mixed["counts"])
    check(sum(mixed["counts"].values()) == mixed["of"] == 4,
          "and they add up to what was handed in")
    check(any("D-39" in line for line in mixed["unjudged"]),
          "and the answer cites D-39 for why there is no rate")
    decisions = " ".join(decision_log().split())
    check("disagreement" in decisions.lower(),
          "which really is about a disagreement log rather than a rate")

    print("\n4. Nothing is concluded")
    for concluding in ("pattern", "always", "recommend", "suggest",
                       "cause", "verdict", "alert"):
        check(concluding not in mixed,
              "no '%s' key however bad it looks" % concluding)
    worst = ask([run("%d" % n, "failed") for n in range(9)])
    check(sorted(worst) == sorted(plain),
          "nine failures give the same keys as one clean run")
    check(any("an observer that concludes is a judge with no appeal" in line
              for line in worst["unjudged"]),
          "and the answer says why an observer must not conclude")
    check("HERON-LRN-ANA-002" in whole,
          "naming the agent whose job that is")

    print("\n5. The window travels")
    check(mixed["window"] == {"first": "2026-09-01", "last": "2026-09-04"},
          "first and last are in the answer: %s" % mixed["window"])
    check(any("not the same as what did not happen" in line
              for line in mixed["unjudged"]),
          "and the answer says what was not recorded is not what did not "
          "happen")

    print("\n6. Every failure is named and reached")
    check(ask(None).get("refused") == "NOTHING_TO_OBSERVE",
          "nothing handed in is refused")
    check("most misleading answer" in ask([]).get("why", ""),
          "because reporting nothing wrong after seeing nothing is the "
          "most misleading answer it could give")
    for bad, why in (("not a map", "a run that is not a map"),
                     ({"when": "d", "outcome": "ok"}, "one with no workflow"),
                     ({"workflow": "w", "outcome": "ok"}, "one with no when"),
                     ({"workflow": "w", "when": "d"}, "and one with no "
                                                      "outcome")):
        check(ask([bad]).get("refused") == "NOT_A_RUN", why)
    for outcome in ("partial", "maybe", "PASSED", "1"):
        check(ask([run("d", outcome)]).get("refused") == "NOT_AN_OUTCOME",
              "'%s' is not an outcome - a third would be a judgement"
              % outcome)
    check(ask([run("d", "OK")])["observed"] is True,
          "while case does not matter")
    for storing in ("open(", "write(", "sqlite", "json.dump"):
        check(storing not in logic, "the agent never uses %s" % storing)
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-LRN-OBS-001.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 3, "the contract declares 3 failures")
    for failure in named:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    check(len(mixed["unjudged"]) == 4, "four things are left unjudged")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    failures first, and a corrected success is one of them")
    return 0


if __name__ == "__main__":
    sys.exit(main())
