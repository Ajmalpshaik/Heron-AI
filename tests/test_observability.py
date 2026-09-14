# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-OPS-OBS-011
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Observability - it measures what Heron can see and invents none of the rest.

    python tests/test_observability.py

WHAT IT PROVES
  1. THE THREE FIELDS HERON CANNOT SEE ARE PRESENT AND ANSWERED "NO". D-58
     gives token usage and cost per request to the host and replaces model
     calls per request. A missing key reads as an oversight; a present one
     saying no reads as a decision.

  2. THE ROUTES IT COUNTS ARE heron_search's OWN, not a second list that
     drifts from the first.

  3. A PERCENTAGE OF NOTHING IS NULL, NOT 0%. "0% answered without
     thinking" reads as a system that thinks about everything.

  4. A ROUTE NOBODY RECOGNISES IS IN NEITHER HALF. Guessing which side it
     falls on would put an invented number into the one metric docs/19 s5's
     rule is measured by.

  5. THE MEAN IS ABSENT ON PURPOSE, and the median and the slowest 5% are
     both there - one number cannot answer "usually fine" and "how bad does
     it get".

  6. A RECORD WITH NO DURATION STILL COUNTS TOWARD THE SHARE and does not
     count toward latency, and the report says how many.

  7. NOTHING IS ESTIMATED. No record is invented and no total is derived
     from a rate.

  8. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE AND REACHED.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_observability as OBS                             # noqa: E402
import heron_contract as CON                                  # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def runs(**counts):
    found = []
    for route, how_many in counts.items():
        found += [{"route": route, "milliseconds": 10.0}
                  for _ in range(how_many)]
    return found


def main():
    reached = set()
    source = open(os.path.join(ROOT, "brain", "heron_observability.py"),
                  encoding="utf-8").read()

    def ask(requests):
        answer = OBS.observe(requests)
        if "refused" in answer:
            reached.add(answer["refused"])
        return answer

    print("1. The three Heron cannot see are present and answered 'no'")
    answer = ask(runs(identity=1))
    check(sorted(answer["host_provided"])
          == ["cost_per_request", "model_calls_per_request", "token_usage"],
          "all three are keys in host_provided")
    check("D-01" in answer["host_provided"]["token_usage"],
          "token usage cites D-01, which put the calls in the host")
    check("ACCOUNT TOTAL" in answer["host_provided"]["token_usage"],
          "and says why a provider's API cannot supply it either")
    check("not a" in answer["host_provided"]["cost_per_request"]
          and "confidentiality" in answer["host_provided"]["cost_per_request"],
          "cost says plainly this is arithmetic, not confidentiality")
    check("REPLACED" in answer["host_provided"]["model_calls_per_request"],
          "and model calls says it was replaced, not dropped")
    check(any("not estimated" in note for note in answer["unjudged"]),
          "and every report repeats that none of it is estimated")

    print()
    print("2. The routes are heron_search's own")
    import heron_search as SEARCH
    search_source = open(os.path.join(ROOT, "brain", "heron_search.py"),
                         encoding="utf-8").read()
    for route in OBS.NO_THINKING + OBS.THINKING:
        check('Answer("%s"' % route in search_source,
              "'%s' is a route heron_search really returns" % route)
    check(OBS.NO_THINKING == ("identity", "cache"),
          "and the two that answer without asking anything are the two "
          "docs/19 s5 names")
    check(hasattr(SEARCH, "Answer"),
          "heron_search.Answer is what carries the route into these records")

    print()
    print("3. A percentage of nothing is null, not 0%")
    answer = ask([])
    check(answer["thinking"]["share_without_thinking"] is None,
          "no requests gives a null share, not 0.0")
    check(any("0%" in note and "thinks about everything" in note
              for note in answer["unjudged"]),
          "and says why 0% would be the wrong reading")
    check(answer["latency"]["median_ms"] is None,
          "and no latency either, rather than a zero")
    answer = ask(runs(keywords=4))
    check(answer["thinking"]["share_without_thinking"] == 0.0,
          "while a real four-out-of-four that all thought IS 0.0")

    print()
    print("4. An unknown route is in neither half")
    answer = ask(runs(identity=8, keywords=2) + [{"route": "telepathy"}])
    check(answer["thinking"]["unknown_routes"] == {"telepathy": 1},
          "the unknown route is counted by name")
    check(answer["thinking"]["share_without_thinking"] == 0.8,
          "and the share is 8 of 10, not 8 of 11 or 9 of 11")
    check(any("neither" in note for note in answer["unjudged"]),
          "and the report says it was folded into neither side")
    answer = ask(runs(identity=1) + [{"route": ""}])
    check("(no route)" in answer["thinking"]["unknown_routes"],
          "an empty route is named rather than dropped")

    print()
    print("5. The mean is absent on purpose")
    answer = ask([{"route": "identity", "milliseconds": ms}
                  for ms in [1, 1, 1, 1, 1, 1, 1, 1, 1, 900]])
    check("mean" not in answer["latency"] and "average" not in answer["latency"],
          "there is no mean in the report")
    check(answer["latency"]["median_ms"] == 1,
          "the median says it is usually 1 ms...")
    check(answer["latency"]["slowest_ms"] == 900,
          "...and the slowest says it reached 900 ms")
    check(any("hides the tail" in note for note in answer["unjudged"]),
          "and the reason the mean is absent is stated, not left to guess")

    print()
    print("6. A record with no duration counts toward the share only")
    answer = ask([{"route": "identity", "milliseconds": 5.0},
                  {"route": "identity"},
                  {"route": "keywords", "milliseconds": 50.0}])
    check(answer["thinking"]["share_without_thinking"] == 2 / 3.0,
          "all three count toward the share")
    check(answer["latency"]["measured"] == 2 and answer["latency"]["of"] == 3,
          "and the latency figures say 2 of 3 carried a duration")
    check(any("carried no duration" in note for note in answer["unjudged"]),
          "which is raised rather than left in a field nobody reads")
    answer = ask([{"route": "identity"}])
    check(answer["latency"]["median_ms"] is None
          and any("not a fast system" in note
                  for note in answer["unjudged"]),
          "and no durations at all is not a fast system")

    print()
    print("7. Nothing is estimated")
    # NOT a word search - "estimated" appears three times in this file and
    # all three are negations. The claim is that every mention is one.
    import re
    mentions = re.findall(r".{14}estimat\w*", source)
    check(mentions, "the source does mention estimating, to rule it out")
    negated = [m for m in mentions
               if any(word in m.lower()
                      for word in ("not ", "nothing ", "never"))]
    check(len(negated) == len(mentions),
          "and every one of the %d mentions is a negation (%s)"
          % (len(mentions),
             "; ".join(sorted(set(m.strip() for m in mentions)))))
    for word in ("def estimate", "extrapolat", "predict"):
        check(word not in source.lower(),
              "the source has no %s" % word)
    # AND THE NUMBERS ARE ARITHMETIC ON WHAT WAS GIVEN.
    answer = ask(runs(identity=3, keywords=1))
    check(answer["thinking"]["answered_without_thinking"]
          + answer["thinking"]["answered_after_thinking"] == 4,
          "every counted record is one that was handed in - four in, four "
          "counted, none invented")

    print()
    print("8. Every failure the contract declares is named and reached")
    check(ask(None).get("refused") == "NOTHING_TO_OBSERVE",
          "no requests at all is refused - an empty report reads as a quiet "
          "system rather than an unwatched one")
    for bad in ("a summary", 42, {"identity": 60}):
        check(ask(bad).get("refused") == "NOT_REQUEST_RECORDS",
              "%r is refused - a summary somebody made is not a measurement"
              % (bad,))
    check(ask([{"milliseconds": 4.0}]).get("refused") == "NOT_REQUEST_RECORDS",
          "and a record with no route cannot be counted on either side")
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-OPS-OBS-011.yaml"))
    named = contract.get("failures") or []
    for failure in named:
        check(failure in source, "the code names %s" % failure)
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
    print("PASS    it counts what happened and invents none of the rest")
    return 0


if __name__ == "__main__":
    sys.exit(main())
