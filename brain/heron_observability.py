# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-OPS-OBS-011
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Observability - latency, and the share answered with no thinking at all.

    python brain/heron_observability.py

WHAT IT IS FOR (docs/28, HERON-OPS-OBS-011, as corrected by D-58)
------------------------------------------------------------------
The register row used to read "latency, token usage, model calls per
request, cost per request", and **three of those four describe something
Heron cannot see**. D-01 puts every model call in the host; `heron_embed`
runs locally with no tokens, no account and no cost (D-24, D-26). A file
claiming this agent would have reported it BUILT while three quarters of
its declared job stayed impossible.

D-58 split the row three ways rather than deleting it:

    latency                      Heron's, and measured
    token usage, cost/request    the HOST's. Not estimated here, ever
    model calls per request      REPLACED by the share of requests Heron
                                 answered with NO MODEL NEEDED AT ALL

THE THIRD LINE IS THE SUBSTANCE, NOT A CONSOLATION
----------------------------------------------------
docs/19 s5 sets the rule the original metric existed to protect: the
identity and cache routes must be tried BEFORE any model, structurally.
"If step 4 is ever reached for 'select all ducts' after the first time,
something is broken."

Counting the host's model calls was a proxy for that rule holding. The
share Heron answered deterministically measures the same rule DIRECTLY,
from Heron's own side of the wire, with nothing to ask anybody for.
`heron_search.ask()` already reports which route answered - identity,
cache, keywords or nothing - so this counts what is already being said.

A PERCENTAGE OF NOTHING IS NOT ZERO PER CENT
----------------------------------------------
No requests means unmeasured, and it says so. "0% answered without
thinking" reads as a system that thinks about everything; "no requests yet"
reads as what it is. The agent registry learned this about health scores
and it is the same mistake in a different column.

THE MEAN IS NOT REPORTED, AND THAT IS DELIBERATE
--------------------------------------------------
A mean latency hides the tail, and the tail is the whole reason anybody
measures latency. This reports the median and the slowest 5% - the numbers
that answer "is it usually fine" and "how bad does it get" separately,
which one number cannot.

A ROUTE NOBODY RECOGNISES IS COUNTED SEPARATELY
-------------------------------------------------
Not folded into "it thought about it", and not into "it did not". An
unknown route is a record this agent does not understand, and guessing
which side of the line it falls on would put an invented number into the
one metric the rule above is measured by.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# brain/heron_search.py's routes. The first two answered without asking
# anything - that is the measurement docs/19 s5 cares about.
NO_THINKING = ("identity", "cache")
THINKING = ("keywords", "nothing")

# D-58. Named here so the report carries the reason rather than a blank.
THE_HOSTS = {
    "token_usage":
        "D-01 puts every model call in the host, so the tokens are counted "
        "there or not at all. A provider's usage API reports an ACCOUNT "
        "TOTAL FOR A PERIOD, not what this request cost - the per-request "
        "attribution docs/19 s7 asks for exists only inside the process "
        "that made the call",
    "cost_per_request":
        "the same arithmetic limit, and D-58 says plainly it is not a "
        "confidentiality one: project names, content and reasoning may go "
        "to the cloud, only the RVT and RFA files may not",
    "model_calls_per_request":
        "Heron cannot count the host's calls. D-58 REPLACED this with the "
        "share of requests answered with no model needed at all, which "
        "measures docs/19 s5's rule directly rather than by proxy",
}


def _percentile(sorted_values, fraction):
    """The value at `fraction` through an already-sorted list."""
    if not sorted_values:
        return None
    index = int(round(fraction * (len(sorted_values) - 1)))
    return sorted_values[index]


def observe(requests):
    """
    {latency, thinking, host_provided, unjudged, why} - or a refusal.

    `requests` are records: {route, milliseconds}. Nothing is estimated and
    nothing is asked of anybody - this counts what already happened.
    """
    if requests is None:
        return {"refused": "NOTHING_TO_OBSERVE",
                "why": "no requests were given. An empty report would read "
                       "as a quiet system rather than as an unwatched one."}

    if not isinstance(requests, (list, tuple)):
        return {"refused": "NOT_REQUEST_RECORDS",
                "why": "requests are a list of records, each with the route "
                       "that answered and how long it took. A summary "
                       "somebody already made is not a measurement."}

    bad = [r for r in requests if not isinstance(r, dict) or "route" not in r]
    if bad:
        return {"refused": "NOT_REQUEST_RECORDS",
                "why": "%d of %d records do not name the route that answered. "
                       "The route IS the measurement docs/19 s5 asks for, and "
                       "a record without one cannot be counted on either side "
                       "of the line." % (len(bad), len(requests))}

    without, thought, unknown = [], [], {}
    for record in requests:
        route = str(record.get("route") or "").strip().lower()
        if route in NO_THINKING:
            without.append(record)
        elif route in THINKING:
            thought.append(record)
        else:
            # NOT FOLDED EITHER WAY. Guessing which side an unknown route
            # falls on would put an invented number into the one metric
            # docs/19 s5's rule is measured by.
            unknown.setdefault(route or "(no route)", []).append(record)

    timed = sorted(float(r["milliseconds"]) for r in requests
                   if isinstance(r.get("milliseconds"), (int, float)))
    latency = {"measured": len(timed), "of": len(requests),
               "median_ms": _percentile(timed, 0.5),
               "slowest_5_percent_ms": _percentile(timed, 0.95),
               "slowest_ms": timed[-1] if timed else None}

    counted = len(without) + len(thought)
    thinking = {
        "answered_without_thinking": len(without),
        "answered_after_thinking": len(thought),
        "share_without_thinking": (len(without) / float(counted)
                                   if counted else None),
        "unknown_routes": dict((name, len(rows))
                               for name, rows in sorted(unknown.items())),
    }

    unjudged = [
        "TOKEN USAGE, COST PER REQUEST AND MODEL CALLS PER REQUEST are not "
        "here and are not estimated. D-58 gives the first two to the host "
        "and replaces the third; every one of them is in `host_provided` "
        "with the reason rather than left out.",
    ]
    if not counted:
        unjudged.append(
            "NO REQUEST COULD BE COUNTED, so the share is unmeasured rather "
            "than 0%. '0% answered without thinking' reads as a system that "
            "thinks about everything; 'nothing measured yet' reads as what "
            "it is.")
    if unknown:
        unjudged.append(
            "%d record(s) used %d route(s) this agent does not know - %s. "
            "They are counted separately and folded into neither side: "
            "guessing would put an invented number into the one metric "
            "docs/19 s5's rule is measured by."
            % (sum(len(r) for r in unknown.values()), len(unknown),
               ", ".join(sorted(unknown))))
    if not timed:
        unjudged.append(
            "NO RECORD CARRIED A DURATION, so there is no latency here. That "
            "is not a fast system.")
    elif len(timed) < len(requests):
        unjudged.append(
            "%d of %d records carried no duration and are not in the latency "
            "figures. The share above still counts all of them."
            % (len(requests) - len(timed), len(requests)))
    unjudged.append(
        "THE MEAN IS NOT REPORTED. It hides the tail, and the tail is why "
        "anybody measures latency - the median says whether it is usually "
        "fine and the slowest 5% says how bad it gets, which one number "
        "cannot do.")

    share = thinking["share_without_thinking"]
    return {"latency": latency, "thinking": thinking,
            "host_provided": dict(THE_HOSTS), "unjudged": unjudged,
            "why": "%d request(s): %s answered with no model needed, "
                   "median %s, slowest 5%% %s."
                   % (len(requests),
                      "%.0f%%" % (share * 100) if share is not None
                      else "unmeasured",
                      "%.1f ms" % latency["median_ms"]
                      if latency["median_ms"] is not None else "unmeasured",
                      "%.1f ms" % latency["slowest_5_percent_ms"]
                      if latency["slowest_5_percent_ms"] is not None
                      else "unmeasured")}


def main(argv):
    print("OBSERVABILITY   latency, and the share that needed no thinking")
    print("=" * 72)

    requests = (
        [{"route": "identity", "milliseconds": 2.1} for _ in range(60)]
        + [{"route": "cache", "milliseconds": 3.4} for _ in range(20)]
        + [{"route": "keywords", "milliseconds": 41.0} for _ in range(18)]
        + [{"route": "nothing", "milliseconds": 380.0} for _ in range(2)])

    answer = observe(requests)
    print("  %s" % answer["why"])
    print()
    thinking = answer["thinking"]
    print("  answered with no model needed   %d"
          % thinking["answered_without_thinking"])
    print("  answered after searching        %d"
          % thinking["answered_after_thinking"])
    latency = answer["latency"]
    print("  median                          %.1f ms" % latency["median_ms"])
    print("  slowest 5%%                      %.1f ms"
          % latency["slowest_5_percent_ms"])
    print("  slowest                         %.1f ms" % latency["slowest_ms"])

    print()
    print("  NOT MEASURED HERE, and why (D-58):")
    for name, why in sorted(answer["host_provided"].items()):
        print("    %-24s %s" % (name, why[:66]))

    print()
    print("  With one unknown route in the set:")
    answer = observe(requests + [{"route": "telepathy", "milliseconds": 1.0}])
    print("    unknown routes: %s" % answer["thinking"]["unknown_routes"])
    print("    share is still %.0f%% - the unknown one is in neither half"
          % (answer["thinking"]["share_without_thinking"] * 100))

    print()
    print("  With no requests at all:")
    answer = observe([])
    print("    share: %s" % answer["thinking"]["share_without_thinking"])
    print("    %s" % [n for n in answer["unjudged"]
                      if "0%" in n][0][:92])
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
