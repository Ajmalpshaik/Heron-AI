# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-AHR-REG-008
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The agent registry - assembled from whoever owns each field, and nothing kept.

    python tests/test_agents.py

WHAT IT PROVES
  1. A RECORD IS ASSEMBLED FROM FOUR SOURCES AND MATCHES EACH OF THEM: the
     register for identity and tier, the file header for status and layer,
     agent-count for which files implement it, the contract for the interface.

  2. NOTHING IS STORED. There is no file this module writes and no cache it
     reads - asserted by deleting nothing and re-deriving twice, and by the
     absence of any write in the source.

  3. HEALTH AND PERFORMANCE ARE REPORTED AS UNMEASURED, BY NAME - never 0,
     never "unknown", never a green tick. docs/18 says these are what make it
     a live system of record; nothing has run, so nothing may claim them.

  4. AN AGENT NOT IN THE REGISTER HAS NO RECORD. An agent is in the register
     before it is anywhere else, and inventing a record for one would be the
     register describing the system instead of being it.

  5. A CONTRACT FOR AN AGENT NOTHING IMPLEMENTS IS REPORTED - a promise with
     nobody keeping it.

  6. A CONTRACT FOR AN AGENT THE REGISTER DOES NOT HAVE IS REPORTED.

  7. THE BACKLOG IS KEPT SEPARATE FROM THE DISAGREEMENTS. 81 built agents have
     no contract; printing those in the same list as a contract for a
     non-existent agent buries the rare finding under the expected one.

  8. THE HEADER AUDIT IS NOT REPEATED HERE. check-metadata.py owns it, and a
     second implementation of one check is a second answer to one question.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_agents as REG                                    # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    agents, claims, host = REG._agent_count()
    deals = REG.contracts()

    print("1. A record matches every source it came from")
    found = REG.record("HERON-KRN-EVT-004", agents, claims, host, deals)
    check(found["name"] == agents["HERON-KRN-EVT-004"]["name"]
          and found["tier"] == agents["HERON-KRN-EVT-004"]["tier"],
          "identity and tier come from docs/28-agent-registry.md")
    check("brain/heron_events.py" in found["files"],
          "the implementing file comes from the Heron-Agent headers")
    check(found["state"] == REG.header_of("brain/heron_events.py")["status"],
          "the state is the implementing file's own Heron-Status")
    check(found["contract"] == "brain/agents/HERON-KRN-EVT-004.yaml"
          and found["timeout_seconds"] == 30
          and "HANDLER_FAILED" in found["failures"],
          "the interface comes from the contract, not from the source")

    unbuilt = [a for a in agents
               if a not in claims and a not in host]
    if unbuilt:
        blank = REG.record(unbuilt[0], agents, claims, host, deals)
        check(blank["state"] == "NOT BUILT" and blank["files"] == [],
              "an agent nothing implements reads NOT BUILT, with no files")
    host_id = sorted(host)[0]
    check(REG.record(host_id, agents, claims, host, deals)["state"] == "HOST",
          "a host-provided agent reads HOST, not NOT BUILT")

    print()
    print("1b. The header comes from an implementation, never from a test")
    sorted_test_first = {
        "HERON-KRN-EVT-004": ["tests/test_events.py", "brain/heron_events.py"]}
    from_impl = REG.record("HERON-KRN-EVT-004", agents, sorted_test_first,
                           host, deals)
    check(from_impl["layer"] == "brain",
          "with a test sorting first, the layer is still the module's")
    for agent_id in ("HERON-DOC-FRG-004", "HERON-WSP-BAK-010",
                     "HERON-WSP-RST-011"):
        if agent_id in claims:
            found_one = REG.record(agent_id, agents, claims, host, deals)
            check(found_one["layer"] != "test",
                  "%s no longer reports layer: test" % agent_id)

    print()
    print("1c. A test-only claim is not an implementation")
    test_only = {"HERON-KRN-EVT-004": ["tests/test_events.py"]}
    check(REG.record("HERON-KRN-EVT-004", agents, test_only, host,
                     deals)["state"] == "NOT BUILT",
          "an agent claimed only by a test reads NOT BUILT")
    for agent_id in ("HERON-RAG-RIX-011", "HERON-RAG-DUP-012"):
        if agent_id in claims:
            check(REG.record(agent_id, agents, claims, host,
                             deals)["state"] == "NOT BUILT",
                  "%s is no longer DRAFT off its test's header" % agent_id)

    print()
    print("1d. The whole register comes back when no agent is named")
    everything = REG.records(agents, claims, host, deals)
    check(len(everything) == len(agents),
          "every agent in the register has a record")
    check(everything["HERON-KRN-EVT-004"]["name"] == "Event Bus",
          "and each one is the record, not a tally")

    print()
    print("2. Nothing is stored")
    source = io.open(os.path.join(ROOT, "brain", "heron_agents.py"),
                     encoding="utf-8").read()
    for forbidden in ('"w"', "'w'", "makedirs", "json.dump", "yaml.dump"):
        check(forbidden not in source,
              "the module contains no %s - it derives, it does not keep"
              % forbidden)
    twice = REG.record("HERON-KRN-EVT-004", agents, claims, host, deals)
    check(twice == found, "deriving the same record twice gives the same thing")

    print()
    print("3. Health and performance are unmeasured, by name")
    check(found["unmeasured"] == list(REG.UNMEASURED),
          "the record names the fields nothing has measured")
    check("health" in found["unmeasured"]
          and "performance score" in found["unmeasured"],
          "and they are the two docs/18 asks for")
    for key in ("health", "performance", "score"):
        check(key not in found or found.get(key) is None,
              "no %s value is present to be believed" % key)

    print()
    print("4. An agent not in the register has no record")
    check(REG.record("HERON-MADE-UP-999", agents, claims, host, deals) is None,
          "an unregistered id gets None, never an invented record")

    print()
    print("5, 6 and 7. What is reported, and what is kept apart from it")
    invented = {"HERON-NOT-REAL-001": ("brain/agents/fake.yaml",
                                       {"agent": "HERON-NOT-REAL-001"})}
    lines = REG.disagreements(agents, claims, host, invented)
    check(any("which is not in docs/28" in line for line in lines),
          "a contract for an agent the register does not have is reported")

    orphan_id = unbuilt[0] if unbuilt else None
    if orphan_id:
        orphan = {orphan_id: ("brain/agents/orphan.yaml",
                              {"agent": orphan_id})}
        lines = REG.disagreements(agents, claims, host, orphan)
        check(any("nobody keeping it" in line for line in lines),
              "a contract for an agent nothing implements is reported")

    real = REG.disagreements(agents, claims, host, deals)
    backlog = REG.without_contract(agents, claims, deals)
    check(len(backlog) > 0, "the no-contract backlog is not empty today")
    check(not any("has no contract" in line for line in real),
          "and none of it appears in the disagreement list")
    check(all(agent_id in claims for agent_id, _path in backlog),
          "every row in the backlog is an agent something implements")

    print()
    print("8. The header audit is not reimplemented here")
    for owned in ("Heron-Since", "STATUSES", "LAYERS"):
        check(source.count(owned) <= 1,
              "the module does not re-audit %s - check-metadata.py owns it"
              % owned)

    print()
    print("X. Two implementations that disagree are reported, not resolved")
    # Taking files[0] makes the answer depend on FILENAME ORDER: two files
    # claiming one agent with different Heron-Status headers meant the
    # register quietly reported whichever sorted first, and a lifecycle
    # decision downstream read a stale stage as current.
    agents, claims, host = REG._agent_count()
    multi = [a for a, files in claims.items()
             if len([f for f in files if not f.startswith("tests/")]) > 1]
    check(multi, "%d agent(s) really are implemented by more than one file"
          % len(multi))
    contested = [a for a in agents
                 if (REG.record(a, agents, claims, host) or {})
                 .get("header_disagreements")]
    check(contested == [],
          "and none of the 250 disagree about status or version today")

    # A LAYER DIFFERENCE IS NOT A DISAGREEMENT. Four agents legitimately
    # span brain and bridge, or revit and platform, and calling that
    # contested on four agents is how a real warning gets ignored.
    spanning = [a for a in agents
                if len((REG.record(a, agents, claims, host) or {})
                       .get("layers") or []) > 1]
    check(spanning, "%d agent(s) span more than one layer, and none is "
                    "reported as contested" % len(spanning))
    for agent_id in spanning:
        record = REG.record(agent_id, agents, claims, host)
        check(not record["header_disagreements"],
              "%s spans %s and is not contested"
              % (agent_id, "/".join(record["layers"])))

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the record is assembled, and what nobody measured says so")
    return 0


if __name__ == "__main__":
    sys.exit(main())
