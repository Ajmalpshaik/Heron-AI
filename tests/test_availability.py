# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-KRN-MAV-017
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Availability and fallback - a degraded answer is never evidence.

    python tests/test_availability.py

WHAT IT PROVES
  1. THE FOUR REASONS AN ADAPTER IS NOT AVAILABLE ARE TOLD APART: unreachable,
     auth, slow, too small. Collapsing them into "unavailable" builds a system
     that retries an auth failure for ever and gives up on a provider that was
     busy once.

  2. TOO SMALL IS ABOUT THE REQUEST, NOT THE PROVIDER. The same adapter that
     cannot take a 9,000-token request serves a 500-token one in the same
     breath.

  3. AN UNPROBED ADAPTER IS TREATED AS UNREACHABLE, not as fine. Assuming the
     optimistic answer for an unknown provider is how a failure surfaces three
     layers away from its cause.

  4. THE FIRST CHOICE, WHEN IT IS THERE, IS NOT DEGRADED AND IS EVIDENCE.

  5. A FALLBACK IS MARKED DEGRADED, AND A DEGRADED RESULT IS NOT EVIDENCE
     TOWARD PROMOTION (docs/24). This is the half of the register's row that
     is easy to skip: the answer is evidence about the fallback, not about the
     work.

  6. A CONFIDENTIAL SCOPE DOES NOT FALL BACK OFF THE MACHINE. The recovery
     path is exactly where a leak arrives - the local adapter is down and
     something reaches for the cloud one that is right there.

  7. NOTHING AVAILABLE IS A REFUSAL NAMING EVERY ADAPTER TRIED AND WHY.

  8. AN UNKNOWN INTENT COMES BACK AS THE ROUTER'S REFUSAL, not as an
     exception from inside this module.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_availability as AVAIL                            # noqa: E402
import heron_router as ROUTER                                 # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def two_adapters():
    router = ROUTER.Router()
    router.register("on-machine", ROUTER.LOCAL, intents=("CLASSIFY",))
    router.register("claude-code", ROUTER.HOST, strong=True)
    return router


def main():
    print("1, 2 and 3. The four states, told apart")
    up = {"reachable": True, "auth": True, "latency_ms": 40,
          "context_limit": 8000}
    check(AVAIL.judge(up)[0] == AVAIL.OK, "a healthy probe is ok")
    check(AVAIL.judge({"reachable": False})[0] == AVAIL.UNREACHABLE,
          "nothing answering is unreachable")
    check(AVAIL.judge({"reachable": True, "auth": False})[0] == AVAIL.AUTH,
          "answering and refusing the credentials is auth, not unreachable")
    check("retrying will not change that"
          in AVAIL.judge({"reachable": True, "auth": False})[1],
          "and the reason says retrying will not help")
    slow = {"reachable": True, "auth": True, "latency_ms": 9000}
    check(AVAIL.judge(slow)[0] == AVAIL.SLOW, "too late to answer is slow")
    check(AVAIL.judge(up, needs_context=9000)[0] == AVAIL.TOO_SMALL,
          "a request over the context limit is too small")
    check(AVAIL.judge(up, needs_context=500)[0] == AVAIL.OK,
          "and the same adapter serves the smaller request")
    check(AVAIL.judge(None)[0] == AVAIL.UNREACHABLE,
          "an adapter nobody probed is unreachable, never assumed fine")

    check(AVAIL.judge({"reachable": True})[0] == AVAIL.AUTH,
          "a probe that never checked the credentials is not authorised")
    check("not a yes" in AVAIL.judge({"reachable": True})[1],
          "and it says a question nobody asked is not a yes")
    check(AVAIL.judge({"reachable": True, "auth": True,
                       "error": "timed out"})[0] == AVAIL.PROBE_FAILED,
          "a probe that did not complete is PROBE_FAILED, not 'down'")

    print()
    print("4 and 5. Degraded rides on the answer, and is not evidence")
    router = two_adapters()
    healthy = AVAIL.resolve(router, "CLASSIFY",
                            {"on-machine": up,
                             "claude-code": {"reachable": True, "auth": True}})
    check(healthy["adapter"] == "on-machine" and not healthy["degraded"],
          "the first choice, when it is there, is not degraded")
    check(AVAIL.counts_as_evidence(healthy),
          "and it counts as evidence toward promotion")

    fell_back = AVAIL.resolve(router, "CLASSIFY",
                              {"on-machine": {"reachable": False},
                               "claude-code": {"reachable": True,
                                               "auth": True}})
    check(fell_back["adapter"] == "claude-code" and fell_back["degraded"],
          "the fallback answers, and is marked degraded")
    check(not AVAIL.counts_as_evidence(fell_back),
          "a degraded answer does NOT count as evidence toward promotion")
    check("evidence about the fallback" in fell_back["why"],
          "and the reason says why that is, not just that it is")
    check("on-machine" in fell_back["why"],
          "the why names what was asked first and what was wrong with it")

    print()
    print("6. Confidential does not fall back off the machine")
    private = AVAIL.resolve(router, "CLASSIFY",
                            {"on-machine": {"reachable": False},
                             "claude-code": {"reachable": True,
                                             "auth": True}},
                            confidential=True)
    check(private.get("refused") == "NO_CONFIDENTIAL_FALLBACK",
          "with the local adapter down, the cloud one is refused, not used")
    check("adapter" not in private, "and no adapter is returned at all")
    check(not AVAIL.counts_as_evidence(private),
          "a refusal is not evidence either - there is no result to weigh")

    print()
    print("6b. A probe that did not complete keeps its retryable name")
    router = two_adapters()
    all_failed = AVAIL.resolve(router, "CLASSIFY",
                               {"on-machine": {"error": "timed out"},
                                "claude-code": {"error": "timed out"}})
    check(all_failed.get("refused") == "PROBE_FAILED",
          "when every probe failed, the answer is PROBE_FAILED")
    check("nothing is known about any provider" in all_failed["why"],
          "and it says nothing is known either way")
    mixed = AVAIL.resolve(router, "CLASSIFY",
                          {"on-machine": {"error": "timed out"},
                           "claude-code": {"reachable": False}})
    check(mixed.get("refused") == "NOTHING_AVAILABLE",
          "a mix of a failed probe and a dead provider is not retryable")

    print()
    print("6c. A missing degradation marker is not evidence")
    for result in ({"adapter": "x"}, {"adapter": "x", "degraded": None},
                   {"adapter": "x", "degraded": "no"}):
        check(not AVAIL.counts_as_evidence(result),
              "%r does not count - only an explicit False does" % (result,))
    check(AVAIL.counts_as_evidence({"adapter": "x", "degraded": False}),
          "and an explicit first-choice outcome does")

    print()
    print("7. Nothing available names everything tried")
    router = ROUTER.Router()
    router.register("first", ROUTER.HOST)
    router.register("second", ROUTER.CLOUD)
    nothing = AVAIL.resolve(router, "CLASSIFY",
                            {"first": {"reachable": True, "auth": False},
                             "second": {"reachable": False}})
    check(nothing.get("refused") == "NOTHING_AVAILABLE",
          "with both down, the answer is a refusal")
    check("first" in nothing["why"] and "second" in nothing["why"],
          "and it names both adapters and what each did")

    print()
    print("8. An unknown intent is the router's refusal, not an exception")
    raised = False
    try:
        answer = AVAIL.resolve(router, "GUESS_A_BIT", {})
    except Exception:                                        # noqa: BLE001
        raised = True
        answer = {}
    check(not raised, "an undeclared intent does not raise")
    check(answer.get("refused") == "UNKNOWN_INTENT",
          "it comes back as the router's own refusal code")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the fallback answers, and never pretends to be evidence")
    return 0


if __name__ == "__main__":
    sys.exit(main())
