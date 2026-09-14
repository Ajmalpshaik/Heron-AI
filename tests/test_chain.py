# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The whole chain, once, end to end - do the seams actually fit together.

    python tests/test_chain.py

WHY THIS EXISTS, AND WHY IT IS NOT ANOTHER UNIT TEST
-----------------------------------------------------
Thirteen agents are built and every one passes its own suite. That proves each
behaves as written. It does not prove they FIT: every one of those suites hands
its subject a dictionary the test wrote itself, and a seam is exactly the place
where the shape one agent returns and the shape the next one expects can drift
apart without either test noticing.

The owner asked the right question - "is this working or not?" - and the honest
answer was: each fitting is bench-tested, and the pipe run has never had water
in it. This is the water.

WHAT IT DOES
-------------
It walks ONE agent through its whole life, using the real modules, and at each
step the INPUT is the previous step's OUTPUT rather than something written
here:

     1  Workforce Planning   is a new agent even warranted
     2  Agent Contract       its interface, validated
     3  Instructions         what it will be told, assembled
     4  Model Router         who would answer, by intent
     5  Availability         are they there, and is the answer degraded
     6  Budget               may it be afforded, and what did it cost
     7  Event Bus            who hears that it ran
     8  Secret Manager       nothing it says reaches a log with a token in it
     9  Sandbox              its first run, every door shut
    10  Validation           everything checkable, checked
    11  Deployment           the gates, one at a time, up to a signature
    12  Retirement           archived, never deleted
    13  Agent Registry       the record, assembled from all of the above

WHAT IT STILL DOES NOT PROVE
------------------------------
No model was called - no adapter exists to call. No Revit was touched. Nothing
here is evidence toward promotion under docs/24, and every result that came
back marked degraded or sandboxed says so itself. A green run here means the
parts fit. It does not mean Heron works, and the day somebody quotes it as if
it did, this paragraph is the answer.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_agents as REG                                    # noqa: E402
import heron_availability as AVAIL                            # noqa: E402
import heron_budget as BUDGET                                 # noqa: E402
import heron_contract as CON                                  # noqa: E402
import heron_deployment as DEP                                # noqa: E402
import heron_events as EVENTS                                 # noqa: E402
import heron_instructions as INS                              # noqa: E402
import heron_retirement as RET                                # noqa: E402
import heron_router as ROUTER                                 # noqa: E402
import heron_sandbox as BOX                                   # noqa: E402
import heron_secrets as SECRETS                               # noqa: E402
import heron_validation as VAL                                # noqa: E402
import heron_workforce as WFP                                 # noqa: E402

FAILURES = []

# The agent walked through the chain. A real registered one, because the
# registry, validation and deployment all read the register - a made-up id
# would only prove the chain works for something that does not exist.
SUBJECT = "HERON-KRN-EVT-004"

# Assembled from pieces: a token-shaped literal in a tracked file is what push
# protection exists to stop.
FAKE_TOKEN = "gh" + "p_" + ("K3j5H7g9F1d3" * 3)


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    carried = {}

    print("1. Workforce Planning - is another agent even warranted")
    answer = WFP.assess("Duct Velocity Checker",
                        "checks duct velocity against the project standard "
                        "and reports where it is exceeded")
    check(answer.get("verdict") in (WFP.PROPOSE_HIRING, WFP.EXTEND_EXISTING,
                                    WFP.ALREADY_AN_AGENT,
                                    WFP.ALREADY_A_CAPABILITY),
          "it returns one of its five verdicts and not an exception")
    check(answer["unjudged"],
          "and carries the question word overlap could not answer")
    carried["verdict"] = answer["verdict"]

    print()
    print("2. Agent Contract - the interface, validated")
    contracts = REG.contracts()
    check(SUBJECT in contracts, "the subject has a contract on disk")
    path, contract = contracts[SUBJECT]
    problems = CON.validate(contract, CON.registry_ids(), path)
    check(problems == [], "and it validates")
    carried["timeout"] = contract.get("timeout-seconds")
    carried["failures"] = contract.get("failures") or []
    check(isinstance(carried["timeout"], int) and carried["failures"],
          "the contract yields a timeout and failure states for later steps")

    print()
    print("3. Instructions - what it would be told")
    text, articles = INS.compose("agent.read")
    check("Your permission stops at reading" in text,
          "a READ agent's instruction assembles")
    check("14" in articles and "22" in articles,
          "and carries the Constitution articles it declared")
    carried["instruction_articles"] = articles

    print()
    print("4. Model Router - who would answer")
    router = ROUTER.Router()
    router.register("on-machine", ROUTER.LOCAL, intents=("CLASSIFY",))
    router.register("the-host", ROUTER.HOST, strong=True)
    routed = router.route("CLASSIFY")
    check("adapter" in routed, "an intent routes to an adapter")
    carried["adapter"] = routed["adapter"]

    print()
    print("5. Availability - and is the answer degraded")
    probes = {carried["adapter"]: {"reachable": True, "auth": True,
                                  "latency_ms": 30, "context_limit": 8000},
              "the-host": {"reachable": True, "auth": True}}
    healthy = AVAIL.resolve(router, "CLASSIFY", probes)
    check(healthy["adapter"] == carried["adapter"],
          "availability probes the adapter the ROUTER chose, not one of its own")
    check(AVAIL.counts_as_evidence(healthy), "a healthy first choice is evidence")

    probes[carried["adapter"]] = {"reachable": False}
    degraded = AVAIL.resolve(router, "CLASSIFY", probes)
    check(degraded["degraded"] and not AVAIL.counts_as_evidence(degraded),
          "with it down, the fallback answers and is not evidence")
    carried["degraded"] = degraded["degraded"]

    print()
    print("6. Budget - may it be afforded, and what did it cost")
    book = BUDGET.Budget()
    book.set_budget(BUDGET.SESSION, 100, "calls")
    allowed = book.may_spend(BUDGET.SESSION)
    check(allowed["allowed"], "the call is affordable")
    book.record(BUDGET.SESSION, 1, "calls",
                reported_by="the %s adapter" % degraded["adapter"])
    check(degraded["adapter"] in book.ledger[-1][3],
          "and the ledger names the adapter AVAILABILITY settled on")
    check(book.remaining(BUDGET.SESSION) == 99,
          "the budget moved by what was reported, not by an estimate")

    print()
    print("7. Event Bus - who hears that it ran")
    bus = EVENTS.Bus()
    heard = []
    bus.subscribe("agent.ran", lambda p: heard.append(p), "recorder")
    published = bus.publish("agent.ran",
                            {"agent": SUBJECT, "adapter": degraded["adapter"],
                             "degraded": carried["degraded"]})
    check(published["delivered"] == 1 and not published["failed"],
          "the run is announced and heard")
    check(heard and heard[0]["degraded"] is carried["degraded"],
          "and what the listener heard carries availability's own verdict")

    print()
    print("8. Secret Manager - nothing reaches a log with a token in it")
    secrets = SECRETS.Secrets(workspace=ROOT)
    line = "agent %s called %s with Bearer %s" % (SUBJECT,
                                                  degraded["adapter"],
                                                  FAKE_TOKEN)
    clean, found = secrets.redact(line)
    check(FAKE_TOKEN not in clean and found,
          "the token is gone from the line that would have been logged")
    check(SUBJECT in clean and degraded["adapter"] in clean,
          "and everything that was not a secret survived")

    print()
    print("9. Sandbox - its first run, every door shut")
    def first_run(world, payload):
        try:
            world.revit("count_elements")
        except BOX.Refused:
            pass
        world.write("experimental", "velocity.seen", 4.1)
        return {"checked": payload.get("category")}

    record = BOX.run(SUBJECT, first_run, {"category": "ducts"},
                     timeout_seconds=carried["timeout"])
    check(record["result"] == {"checked": "ducts"},
          "it ran, and was given the payload")
    check(("LIVE_MODEL_REFUSED", "count_elements") in record["attempts"],
          "its reach for Revit was refused and recorded")
    check(not record["overran"],
          "and it finished inside the CONTRACT's timeout, not one invented here")
    check(not BOX.counts_as_evidence(record),
          "the run is not evidence, for the same reason a degraded one is not")

    print()
    print("10. Validation - everything checkable, checked")
    validation = VAL.validate(SUBJECT, built_by="HERON-AHR-BLD-004")
    check(validation["verdict"] == "PASS", "the subject passes validation")
    for failure in carried["failures"]:
        check(not any(failure in line for line in validation["findings"]),
              "the contract's %s is named somewhere in the code" % failure)
    check(any("CORRECT" in line for line in validation["unjudged"]),
          "and what no check can answer is still reported")

    print()
    print("11. Deployment - the gates, one at a time")
    stage = "DISCOVERED"
    walk = [("DRAFT", {}), ("TESTING", {}),
            ("VALIDATED", {"matrix": "python 3.11 on linux"}),
            ("SHADOW", {"shadow-plan": "beside the log writer"}),
            ("PROVEN", {"real-runs": 12})]
    for to_stage, evidence in walk:
        answer = DEP.activate(SUBJECT, to_stage, from_stage=stage,
                              evidence=evidence, validation=validation)
        check(answer["activated"],
              "%s -> %s on VALIDATION's own verdict" % (stage, to_stage))
        if answer["activated"]:
            stage = to_stage
    refused = DEP.activate(SUBJECT, "PRODUCTION", from_stage=stage,
                           validation=validation)
    check(refused["refused"] == "NEEDS_HUMAN_APPROVAL",
          "and PRODUCTION stops dead without a person")
    signed = DEP.activate(SUBJECT, "PRODUCTION", from_stage=stage,
                          approved_by="the owner", validation=validation)
    check(signed["activated"] and signed["record"]["applied"] is False,
          "a person signs, and even then nothing is applied by a machine")

    print()
    print("12 and 13. The record, and retirement out of it")
    found_record = REG.record(SUBJECT)
    check(found_record["contract"] == path,
          "the registry's record names the same contract step 2 validated")
    check(found_record["timeout_seconds"] == carried["timeout"],
          "and the same timeout the sandbox was run against")
    retired = RET.retire(SUBJECT, "ARCHIVED", reason="the chain test",
                         approved_by="the owner", successor="HERON-KRN-WFL-007",
                         record_of=found_record)
    check(retired["retired"], "retirement accepts the REGISTRY's record as-is")
    check(retired["record"]["kept"]["contract_version"]
          == contract.get("version"),
          "and keeps the contract version the contract itself declared")
    check(retired["record"]["deleted"] is False, "nothing was deleted")

    print()
    print("What this run did NOT prove")
    print("  no model was called - no adapter exists to call")
    print("  no Revit was touched, and 19 agents still need one")
    print("  nothing here is evidence toward promotion under docs/24")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the seams fit: each step ran on the one before it")
    return 0


if __name__ == "__main__":
    sys.exit(main())
