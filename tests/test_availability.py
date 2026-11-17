# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-KRN-MAV-017
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Availability - the degraded mark, and a degraded answer is never evidence.

    python tests/test_availability.py

BROUGHT TO THE ROW ON PURPOSE, 2026-11-16 (row 5b-1747, part 2)
-----------------------------------------------------------------
docs/28's row for HERON-KRN-MAV-017 was rewritten on 2026-11-15 (PROPOSALS
F20): whether a provider is reachable, and the fallback when it is not, are
the host's, which makes every call (D-65); the agent keeps the one clause
D-65 leaves Heron - it reads that a result came from a fallback and marks it
degraded, so it never counts as evidence toward promotion. Until this
change the suite proved the old row - four probe states told apart, and a
fallback chosen here - so a module that did what the row now forbids passed
it. Sections 1, 2, 3 and 6 below are new and were seen to FAIL on the old
module; 4 and 5 keep the degraded-mark and evidence checks the old suite
already had.

WHAT IT PROVES
  1. THE MODULE NO LONGER JUDGES A PROVIDER OR CHOOSES A FALLBACK: no
     judge(), no resolve(), no probe states, and it imports nothing - not
     the router, not a network library. Reachability is the host's (D-65).

  2. mark() READS THE HOST'S WORD. A fallback is marked degraded, the first
     choice is not, and the result passed in is never changed.

  3. A QUESTION NOBODY ANSWERED IS NOT A NO. Left out, None, or anything but
     an explicit False is marked degraded.

  4. A DEGRADED RESULT IS NOT EVIDENCE TOWARD PROMOTION (D-30, D-65), and a
     missing or malformed marker is not evidence either.

  5. A REFUSAL IS NEVER MARKED AND NEVER EVIDENCE: mark() refuses
     NOT_A_RESULT, and the refusal carries no `adapter`.

  6. THE DOCSTRING CITES D-30 AS IT STANDS, not docs/24's "N successful real
     executions" that D-30 replaced, and the contract is the cut module's.
"""

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_availability as AVAIL                            # noqa: E402
import heron_contract as CON                                  # noqa: E402

FAILURES = []

# What the cut will call its refusal. Read off the module when it has one;
# the literal is only so the checks below still RUN, and fail, on a module
# that does not (.claude/skills/heron-ship, section 2a).
NOT_A_RESULT = getattr(AVAIL, "NOT_A_RESULT", None) or "NOT_A_RESULT"


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def marked(result, *fell_back):
    """mark(), asked rather than called blind - an old module has none."""
    fn = getattr(AVAIL, "mark", None)
    if fn is None:
        return {}
    try:
        return fn(result, *fell_back)
    except TypeError:
        return {}


def main():
    whole = io.open(os.path.join(ROOT, "brain", "heron_availability.py"),
                    encoding="utf-8").read()
    doc = whole.split('"""', 2)[1]
    flat = " ".join(doc.split())
    code = whole.split('"""', 2)[2]

    print("1. It no longer judges a provider or chooses a fallback (D-65)")
    for name in ("judge", "resolve", "UNREACHABLE", "AUTH", "SLOW",
                 "TOO_SMALL", "PROBE_FAILED", "SLOW_MS"):
        check(not hasattr(AVAIL, name),
              "it has no %s - reachability and the fallback are the host's"
              % name)
    # Every import statement after the docstring, at any depth - the old
    # module reached the router from inside main().
    imported = sorted(set(
        (plain or source).split(".")[0]
        for plain, source in re.findall(
            r"^\s*(?:import\s+([A-Za-z_][\w.]*)"
            r"|from\s+([A-Za-z_][\w.]*)\s+import\b)", code, re.M)))
    check(imported == ["sys"],
          "it imports only sys - no router, no network: %s"
          % ", ".join(imported))
    check(callable(getattr(AVAIL, "mark", None)),
          "and it has mark(), the one thing the row says it does")

    print()
    print("2. mark() reads the host's word, and changes nothing it was given")
    answered = {"adapter": "the-host", "text": "three ducts"}
    fallback = marked(answered, True)
    check(fallback.get("degraded") is True,
          "a result the host says came from a fallback is degraded")
    check(fallback.get("text") == "three ducts"
          and fallback.get("adapter") == "the-host",
          "and keeps everything it carried")
    first = marked(answered, False)
    check(first.get("degraded") is False,
          "a result the host says came from the first choice is not")
    check("degraded" not in answered,
          "the result passed in is never changed - mark() returns a copy")

    print()
    print("3. A question nobody answered is not a no")
    check(marked(answered).get("degraded") is True,
          "with fell_back left out, the result is marked degraded")
    for said in (None, "no", 0, ""):
        check(marked(answered, said).get("degraded") is True,
              "fell_back=%r is not an explicit False, so it is degraded"
              % (said,))

    print()
    print("4. A degraded result is not evidence toward promotion")
    check(AVAIL.counts_as_evidence(first),
          "a first-choice result counts as evidence")
    check(not AVAIL.counts_as_evidence(fallback),
          "a degraded one does NOT")
    check(not AVAIL.counts_as_evidence(marked(answered)),
          "nor does one whose origin nobody stated")
    for result in ({"adapter": "x"}, {"adapter": "x", "degraded": None},
                   {"adapter": "x", "degraded": "no"}):
        check(not AVAIL.counts_as_evidence(result),
              "%r does not count - only an explicit False does" % (result,))
    check(AVAIL.counts_as_evidence({"adapter": "x", "degraded": False}),
          "and an explicit first-choice outcome does")

    print()
    print("5. A refusal is never marked and never evidence")
    reached = set()
    for given in ({"refused": "NOTHING_AVAILABLE"}, {"text": "no adapter"},
                  {"adapter": "x", "refused": "AUTH"}, None, "a string"):
        answer = marked(given, False)
        reached.add(answer.get("refused"))
        check(answer.get("refused") == NOT_A_RESULT,
              "%r is refused as %s" % (given, NOT_A_RESULT))
        check("adapter" not in answer and "degraded" not in answer,
              "  and the refusal carries neither `adapter` nor a mark")
        check(not AVAIL.counts_as_evidence(answer),
              "  so it is not evidence either")

    print()
    print("6. The docstring cites D-30 as it stands, and the contract matches")
    check("successful real executions" not in flat,
          "it no longer cites docs/24's 'N successful real executions'")
    check("D-30" in flat and "ONE RECORDED PROOF" in flat
          and "negative case" in flat,
          "it cites D-30: one recorded proof, with a negative case")
    check("D-65" in flat and "the host's" in flat,
          "and D-65: reachability and the fallback are the host's")
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-KRN-MAV-017.yaml"))
    named = contract.get("failures") or []
    check(named == [NOT_A_RESULT],
          "the contract declares the one failure the cut module has: %s"
          % ", ".join(named))
    check(not set(contract.get("input") or {}) & {"probes", "intent",
                                                  "needs-context",
                                                  "confidential"},
          "and takes no probe, intent or scope - those were the fallback's")
    check(set(contract.get("input") or {}) == {"result", "fell-back"},
          "it takes a result and the host's word on it")
    check(not CON.validate(contract, CON.registry_ids()),
          "the contract is well formed")
    for failure in named:
        check(failure in code, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every declared failure was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the degraded mark rides on the result, and is never "
          "evidence")
    return 0


if __name__ == "__main__":
    sys.exit(main())
