# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The provider adapter - asserted over a real socket, not over a dict.

    python tests/test_provider.py

WHY A STUB SERVER AND NOT FIXTURES
------------------------------------
NEEDS-CHECKING K3's whole complaint about the three agents underneath this one
is that they were "written from documentation, against probes a test wrote". A
test that hands `judge()` another hand-built dict repeats exactly that.

So this stands up a real HTTP server on a real port and lets the adapter talk
to it: real sockets, real status codes, real JSON, real timeouts. Everything
between `urllib` and `judge()` is exercised rather than assumed.

WHAT THIS STILL DOES NOT PROVE, AND K3 KEEPS IT
-------------------------------------------------
**A real Ollama's field names.** The stub answers what Ollama's API documents;
whether a live Ollama actually calls them `prompt_eval_count` and `eval_count`
is unproven until one runs. That is precisely what K3 asks for and it stays
open - see the row this suite does NOT close.

WHAT IT PROVES
  1. THE PROBE'S SHAPE IS judge()'s, fed through the real judge() rather than
     compared against a list copied into here.

  2. A PROVIDER THAT IS NOT THERE IS UNREACHABLE - proved against this
     machine, where nothing is listening. A real result, not a fixture.

  3. A 401 IS AUTH, NOT DOWN. Retrying an auth failure for ever is the exact
     failure heron_availability's four states exist to prevent.

  4. `auth` IS ABSENT WHEN NOTHING ASKED, and judge() refuses to read that as
     a yes. The adapter must not fill it in to be polite.

  5. A SLOW PROVIDER IS SLOW, NOT DOWN. It answered; it was just late.

  6. USAGE PASSES THROUGH UNRENAMED, and usage_tokens() refuses to invent a
     number when the provider reported none - D-58 with a socket attached.

  7. THE ADAPTER REGISTERS AS LOCAL, AND CONFIDENTIAL ROUTING PICKS IT. That
     branch of heron_router has never had a local adapter to prove against,
     which is why docs/37 puts a local one first.

  8. NOTHING RAISES. Every failure comes back as a declared refusal, because
     an exception reaching judge() is a crash where a state belongs.
"""

import io
import json
import os
import sys
import threading
import time

from http.server import BaseHTTPRequestHandler, HTTPServer

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_availability as AVAIL     # noqa: E402
import heron_provider as PROV          # noqa: E402
import heron_router as ROUTER          # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


class Stub(BaseHTTPRequestHandler):
    """An Ollama-shaped server. `behaviour` is set on the class per test."""

    behaviour = "ok"

    def log_message(self, *args):
        pass                              # a test that prints a web log is noise

    def _send(self, code, payload):
        body = json.dumps(payload).encode("utf-8")
        self.send_response(code)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def do_GET(self):
        if Stub.behaviour == "auth":
            return self._send(401, {"error": "unauthorized"})
        if Stub.behaviour == "slow":
            time.sleep(0.4)
        if Stub.behaviour == "teapot":
            return self._send(418, {"error": "no"})
        self._send(200, {"models": [{"name": "stub-model:latest"}]})

    def do_POST(self):
        length = int(self.headers.get("Content-Length") or 0)
        asked = json.loads(self.rfile.read(length) or b"{}")
        if Stub.behaviour == "no-usage":
            return self._send(200, {"model": asked.get("model"),
                                    "response": "ready"})
        self._send(200, {
            "model": asked.get("model"),
            "response": "ready",
            # OLLAMA'S OWN FIELD NAMES. If a real Ollama differs, K3 is what
            # finds out - this stub can only be as right as its documentation.
            "prompt_eval_count": 7,
            "eval_count": 3,
            "total_duration": 1234567,
        })


def serve(behaviour):
    """A stub on a free port. Returns (base_url, shutdown)."""
    Stub.behaviour = behaviour
    server = HTTPServer(("127.0.0.1", 0), Stub)
    thread = threading.Thread(target=server.serve_forever)
    thread.daemon = True
    thread.start()
    return "http://127.0.0.1:%d" % server.server_port, server.shutdown


def main():
    print("THE PROVIDER ADAPTER, OVER A REAL SOCKET")
    print("=" * 72)

    print("\n1. the probe's shape is judge()'s, and a live one is judged")
    base, stop = serve("ok")
    try:
        result = PROV.probe(base)
        state, why = AVAIL.judge(result)
        check(state == AVAIL.OK, "a reachable provider is judged OK: %s" % why)
        check(result.get("auth") is True,
              "auth is True because a real request came back 2xx - the "
              "question WAS asked")
        check(result.get("models") == ["stub-model:latest"],
              "and the installed models are reported: %s" % result.get("models"))
        check(isinstance(result.get("latency_ms"), int),
              "with a measured latency, not an assumed one")
        check(result.get("context_limit") is None,
              "context_limit is absent because nothing reported one - "
              "unknown is not too small, and judge() skips it")
    finally:
        stop()

    print("\n2. a provider that is not there is UNREACHABLE (real, not a fixture)")
    # NOTHING IS LISTENING ON THIS PORT. This is a real refused connection on
    # a real machine, which is the first non-fixture result these three agents
    # have ever been given.
    dead = PROV.probe("http://127.0.0.1:1", timeout=2.0)
    state, why = AVAIL.judge(dead)
    check(state == AVAIL.UNREACHABLE,
          "nothing listening is judged UNREACHABLE: %s" % why)
    check(dead.get("reachable") is False, "and the probe said so itself")

    print("\n3. a 401 is AUTH, never down")
    base, stop = serve("auth")
    try:
        refused = PROV.probe(base)
        state, why = AVAIL.judge(refused)
        check(state == AVAIL.AUTH, "judged AUTH: %s" % why)
        check(refused.get("reachable") is True,
              "and reachable stays TRUE - something is there and it said no. "
              "Calling it unreachable would retry a refusal for ever")
    finally:
        stop()

    print("\n4. auth absent when nothing asked, and judge() will not assume")
    check("auth" not in dead,
          "the unreachable probe does not claim auth either way")
    state, why = AVAIL.judge({"reachable": True, "latency_ms": 1})
    check(state == AVAIL.AUTH and "nobody asked" in why,
          "and a probe that never asked is refused, not waved through")

    print("\n5. a slow provider is SLOW, not down")
    base, stop = serve("slow")
    try:
        late = PROV.probe(base)
        state, why = AVAIL.judge(late, slow_ms=100)
        check(state == AVAIL.SLOW, "judged SLOW against a 100 ms ceiling: %s" % why)
        check(late.get("reachable") is True,
              "and it is still reachable - it answered, it was just late")
    finally:
        stop()

    print("\n6. usage passes through unrenamed, and nothing is invented")
    base, stop = serve("ok")
    try:
        answer = PROV.call("stub-model:latest", "say ready", base_url=base)
        check(answer.get("ok") is True, "the call came back: %s" % answer.get("why"))
        check(answer.get("text") == "ready", "with the provider's own text")
        check(set(answer["usage"]) == {"prompt_eval_count", "eval_count",
                                       "total_duration"},
              "and the provider's OWN field names, unrenamed: %s"
              % sorted(answer["usage"]))
        total, which = PROV.usage_tokens(answer["usage"])
        check(total == 10 and "prompt_eval_count" in which,
              "usage_tokens adds only what was reported, and names the "
              "fields it added: %s (%s)" % (total, which))
        # THE REAL RESULT GOES IN, not a dict assembled here. Assembling one
        # is how the first version of this check passed while call() was
        # missing the `adapter` field counts_as_evidence() also reads - the
        # exact shape mismatch docs/37 predicted, in a different function.
        check(AVAIL.counts_as_evidence(answer) is True,
              "and the REAL result counts as evidence - it carries both "
              "fields that one line reads, adapter and degraded")
        check(AVAIL.counts_as_evidence(
            dict(answer, degraded=True)) is False,
            "while the same answer from a fallback does not")
    finally:
        stop()

    base, stop = serve("no-usage")
    try:
        bare = PROV.call("stub-model:latest", "say ready", base_url=base)
        check(bare.get("ok") is True and bare["usage"] == {},
              "a provider that reports no usage gets an empty usage, not a guess")
        total, why = PROV.usage_tokens(bare["usage"])
        check(total is None and "estimate" in why,
              "and usage_tokens REFUSES to invent one: %s" % why[:60])
    finally:
        stop()

    print("\n7. it registers as LOCAL, and confidential routing picks it")
    router = ROUTER.Router()
    PROV.register(router)
    check(router.route("CLASSIFY").get("adapter") == "ollama",
          "the router routes CLASSIFY to it")
    confidential = router.route("CLASSIFY", confidential=True)
    check(confidential.get("adapter") == "ollama",
          "and confidential work reaches it BECAUSE it is LOCAL - a branch "
          "that had no local adapter to prove against until now")
    check("REASON" not in PROV.DEFAULT_INTENTS
          and router.route("REASON").get("adapter") is None,
          "REASON is not offered by default - whether a local model is "
          "strong enough is a property of the model, not of the adapter")

    print("\n8. nothing raises; every failure is a declared refusal")
    for args, expected in (
            (("", "say ready"), "NO_MODEL"),
            (("stub-model", "   "), "NOTHING_ASKED"),
            (("stub-model", "say ready"), "UNREACHABLE")):
        answer = PROV.call(args[0], args[1], base_url="http://127.0.0.1:1",
                           timeout=2.0)
        check(answer.get("refused") == expected,
              "%s refused as %s" % (args[0] or "<no model>", expected))
        # A REFUSAL MUST NEVER COUNT AS EVIDENCE. counts_as_evidence() reads
        # only `adapter` and `degraded`, so a refusal that carried both would
        # promote work that never ran.
        check(AVAIL.counts_as_evidence(answer) is False,
              "  and does not count as evidence toward anything")
    base, stop = serve("teapot")
    try:
        odd = PROV.probe(base)
        state, why = AVAIL.judge(odd)
        check(state == AVAIL.PROBE_FAILED,
              "an answer that is neither refusal nor result is PROBE_FAILED - "
              "nothing is known either way: %s" % why[:50])
    finally:
        stop()

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the adapter answers over a real socket; K3 still owes a "
          "real Ollama")
    return 0


if __name__ == "__main__":
    sys.exit(main())
