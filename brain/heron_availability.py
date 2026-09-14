# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-KRN-MAV-017
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Model Availability & Fallback - is the provider actually there, and what then.

    python brain/heron_availability.py     a worked example of each outcome

WHAT THE REGISTER ASKS FOR
---------------------------
"Is the provider actually reachable - auth, latency, context size, local or
cloud. On failure, routes to the fallback AND MARKS THE RESULT AS DEGRADED, so
it never counts as evidence toward promotion."

The second half is the half that is easy to skip and expensive to skip. Under
docs/24 a fragment is promoted on N successful real executions with no
unexplained failures. An answer produced by the second-choice provider, because
the first was down, is not evidence about the fragment - it is evidence about
the fallback. Counting it promotes work on a run nobody would have chosen.

So `degraded` rides on the result, and `counts_as_evidence()` is the one line
every caller should be asking rather than deciding for itself.

FOUR REASONS AN ADAPTER IS NOT AVAILABLE, AND THEY ARE NOT THE SAME
--------------------------------------------------------------------
  unreachable     nothing answered
  auth            it answered and said no - retrying will not help
  slow            it answered too late to be useful for this kind of work
  too small       its context limit is under what THIS request needs, which
                  is not the adapter being down. It will serve the next,
                  smaller request perfectly well

Collapsing them into "unavailable" produces a system that retries an auth
failure for ever and gives up on a provider that was merely busy once.

CONFIDENTIALITY NARROWS THE FALLBACK TOO
-----------------------------------------
The recovery path is exactly where a leak arrives: the first choice is local,
it is down, and something reaches for the cloud adapter that is right there.
This asks the router for candidates with the same `confidential` flag, so the
narrowing cannot be lost on the way to the fallback. When that leaves nothing,
the answer is a refusal.
"""

import sys

UNREACHABLE, AUTH, SLOW, TOO_SMALL, OK = (
    "unreachable", "auth", "slow", "too small", "ok")

# A probe that could not be taken at all, which is not the same as a
# provider that is down: it says HERON did not manage to ask. That is the
# one state worth a retry, and the contract's retry rule names it alone.
PROBE_FAILED = "probe failed"

# Above this, an adapter is answering too late to be worth waiting for. It is
# a ceiling on the PROBE, not on the work - a provider that takes three
# seconds to say hello is not one to hand a queue of a thousand classifications.
SLOW_MS = 3000


def judge(probe, needs_context=0, slow_ms=SLOW_MS):
    """
    (state, why) for one adapter's probe result.

    The probe is data - {reachable, auth, latency_ms, context_limit} - so this
    is testable without a network, which is the only way it gets tested at all.
    """
    if not probe:
        return UNREACHABLE, "nothing answered"
    if probe.get("error"):
        return PROBE_FAILED, ("PROBE_FAILED: the probe did not complete "
                              "(%s), so nothing is known about the "
                              "provider either way" % probe["error"])
    if not probe.get("reachable", False):
        return UNREACHABLE, "nothing answered"
    if not probe.get("auth", True):
        return AUTH, ("it answered and refused the credentials - retrying "
                      "will not change that")
    latency = probe.get("latency_ms")
    if latency is not None and latency > slow_ms:
        return SLOW, "it took %d ms to answer, over the %d ms ceiling" % (
            latency, slow_ms)
    limit = probe.get("context_limit")
    if needs_context and limit is not None and needs_context > limit:
        return TOO_SMALL, ("this request needs %d and its limit is %d - it is "
                           "not down, it is too small for THIS one"
                           % (needs_context, limit))
    return OK, "reachable, authorised and large enough"


def resolve(router, intent, probes, needs_context=0, confidential=False,
            slow_ms=SLOW_MS):
    """
    Pick an adapter that is actually there. Data in, data out, no network.

    `probes` is {adapter name: probe dict}. An adapter with no probe is
    treated as unreachable rather than as fine: an unprobed provider is an
    unknown one, and assuming the optimistic answer is how a system comes to
    report a failure from three layers down.
    """
    intent = (intent or "").upper()

    # The router owns which adapters COULD serve this, and which of them a
    # confidential scope may consider. Asking it rather than re-deciding here
    # is what keeps the narrowing in one place - and an unknown intent is its
    # refusal to word, not this module's.
    try:
        candidates = [name for name, _kind
                      in router.candidates(intent, confidential)]
    except KeyError:
        routed = router.route(intent, confidential)
        return {"refused": routed.get("refused", "NOTHING_AVAILABLE"),
                "why": routed.get("why", "no adapter offers %s" % intent),
                "degraded": False}

    if not candidates:
        routed = router.route(intent, confidential)
        return {"refused": routed.get("refused", "NOTHING_AVAILABLE"),
                "why": routed.get("why", "no adapter offers %s" % intent),
                "degraded": False}

    tried = []
    for position, name in enumerate(candidates):
        state, why = judge(probes.get(name), needs_context, slow_ms)
        if state == OK:
            if position == 0:
                return {"adapter": name, "degraded": False,
                        "why": "%s answered and is the first choice" % name}
            return {
                "adapter": name, "degraded": True,
                "why": "%s was used instead of %s, which %s. The answer is "
                       "still an answer, but it is not evidence about the "
                       "work - it is evidence about the fallback."
                       % (name, tried[0][0], tried[0][1]),
            }
        tried.append((name, why))

    # Names against names. Comparing the router's (name, kind) pairs against
    # this list of names would be unequal every time, which reads as "there
    # was an off-machine option" even when there was not.
    off_machine = {name for name, _kind in router.candidates(intent, False)}
    if confidential and off_machine - set(candidates):
        return {"refused": "NO_CONFIDENTIAL_FALLBACK", "degraded": False,
                "why": "every on-machine adapter is unavailable (%s). An "
                       "adapter that could answer exists off the machine, and "
                       "this scope is confidential, so it was not used."
                       % "; ".join("%s %s" % (n, w) for n, w in tried)}

    return {"refused": "NOTHING_AVAILABLE", "degraded": False,
            "why": "no adapter for %s is available: %s"
                   % (intent, "; ".join("%s %s" % (n, w) for n, w in tried))}


def counts_as_evidence(result):
    """
    May this result be counted toward promotion (docs/24)?

    One line, in one place, so that no caller decides it privately. A refusal
    is not evidence either - there is no result to weigh.
    """
    return bool(result) and "adapter" in result and not result.get("degraded")


def main(argv):
    import os
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    import heron_router as ROUTER

    router = ROUTER.Router()
    router.register("on-machine", ROUTER.LOCAL,
                    intents=("CLASSIFY", "EXTRACT"))
    router.register("claude-code", ROUTER.HOST, strong=True)

    print("MODEL AVAILABILITY   four outcomes, and what each one means")
    print("=" * 67)

    cases = [
        ("everything up", {"on-machine": {"reachable": True, "auth": True,
                                          "latency_ms": 40,
                                          "context_limit": 8000},
                           "claude-code": {"reachable": True, "auth": True}},
         0, False),
        ("first choice down", {"on-machine": {"reachable": False},
                               "claude-code": {"reachable": True,
                                               "auth": True}}, 0, False),
        ("first choice too small", {"on-machine": {"reachable": True,
                                                   "auth": True,
                                                   "context_limit": 4000},
                                    "claude-code": {"reachable": True,
                                                    "auth": True}},
         9000, False),
        ("confidential, local down", {"on-machine": {"reachable": False},
                                      "claude-code": {"reachable": True,
                                                      "auth": True}},
         0, True),
    ]
    for label, probes, needs, private in cases:
        answer = resolve(router, "CLASSIFY", probes, needs, private)
        print("  %-24s %-14s degraded=%-5s evidence=%s"
              % (label,
                 answer.get("adapter", answer.get("refused")),
                 answer.get("degraded"),
                 counts_as_evidence(answer)))
        print("      %s" % answer["why"])
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
