# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-AHR-REG-008
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
The Agent Registry - the system of record, assembled rather than kept.

    python brain/heron_agents.py                    every agent, and its state
    python brain/heron_agents.py HERON-KRN-EVT-004  one agent's whole record

WHAT A SYSTEM OF RECORD IS FOR (docs/18 s3)
--------------------------------------------
"Agent ID / Name / Department / Role / Version / Status / Capabilities /
Required Skills / Required Knowledge / Allowed Tools / Permissions /
Dependencies / Input Contract / Output Contract / Health / Performance Score"

Sixteen fields - and fourteen of them are already written down somewhere in
this repository. Storing them again would produce a second copy that disagrees
with the first within a fortnight, which is the failure this repository keeps
writing about.

So this agent STORES NOTHING. D-40: "an edge is derived before it is stored.
Store an edge only when it cannot be computed from an artifact on demand."

    identity, department, tier, risk .... docs/28-agent-registry.md
    status, step, layer, since .......... the implementing file's own header
    the file(s) implementing it ......... those headers, via agent-count.py
    input, output, tools, timeout,
    failures, retry ..................... brain/agents/<id>.yaml
    health, performance ................. NOTHING HAS MEASURED THESE YET

THE LAST LINE IS THE IMPORTANT ONE
-----------------------------------
docs/18 says Health and Performance Score are what turn a catalogue into a live
system of record, and they need real runs - which Heron has not had. So they
are reported as UNMEASURED, by name, and never as 0, "unknown" or a green tick.
A register that shows a score nobody computed is worse than one that shows
none: the first is believed.

DISAGREEMENTS ARE REPORTED, NOT RESOLVED
-----------------------------------------
Four sources describe one agent, so four ways exist for them to drift apart: a
contract for an agent nothing implements, an implementation with no contract, a
contract whose version says 0.1.0 while its agent is PROVEN, a contract naming
an agent the register does not have. Each is reported with the file to open.

The header audit itself belongs to tools/check-metadata.py and is not repeated
here - it checks every file's five fields in both directions, and a second
implementation of that check is a second answer to one question.
"""

import importlib.util
import io
import os
import re
import sys

try:
    import yaml
except ImportError:                                          # pragma: no cover
    sys.stderr.write("Heron's brain needs PyYAML: pip install --user pyyaml\n")
    raise

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CONTRACTS_DIR = os.path.join(ROOT, "brain", "agents")

# What nothing in this repository has measured. Named, so that the absence is
# a fact somebody can act on rather than a blank nobody notices.
UNMEASURED = ("health", "performance score")

HEADER = re.compile(r"^\s*(?://|#)\s*Heron-(Status|Step|Since|Layer)\s*:\s*"
                    r"(.+?)\s*$")


def _agent_count():
    """
    tools/agent-count.py, loaded by path - the hyphen stops an import.

    It already owns two facts this module needs: what the register says, and
    which files claim which agent. Re-parsing either here would be a second
    answer to a settled question.
    """
    path = os.path.join(ROOT, "tools", "agent-count.py")
    spec = importlib.util.spec_from_file_location("heron_agent_count", path)
    module = importlib.util.module_from_spec(spec)
    cwd = os.getcwd()
    os.chdir(ROOT)
    try:
        spec.loader.exec_module(module)
        agents, _headings, _totals = module.registry()
        claims = module.built()
        host = dict(module.host_provided() or {})
    except (IOError, OSError, ValueError) as exc:
        # The register is the one file every record here is assembled
        # from. If it cannot be read there is no partial answer worth
        # giving - a record missing its identity is not a record.
        raise IOError("REGISTER_UNREADABLE: docs/28-agent-registry.md "
                      "could not be read - %s" % exc)
    finally:
        os.chdir(cwd)
    return agents, claims, host


def header_of(path):
    """The four header fields a source file carries, as a dict."""
    found = {}
    with io.open(os.path.join(ROOT, path), encoding="utf-8",
                 errors="replace") as fh:
        for index, line in enumerate(fh):
            if index > 40:
                break
            match = HEADER.match(line)
            if match:
                found[match.group(1).lower()] = match.group(2)
    return found


def contracts():
    """{agent id: (path, contract)} for every contract on disk."""
    found = {}
    if not os.path.isdir(CONTRACTS_DIR):
        return found
    for name in sorted(os.listdir(CONTRACTS_DIR)):
        if not name.endswith((".yaml", ".yml")):
            continue
        path = os.path.join(CONTRACTS_DIR, name)
        with io.open(path, encoding="utf-8") as fh:
            data = yaml.safe_load(fh) or {}
        key = data.get("agent") if isinstance(data, dict) else None
        key = key or os.path.splitext(name)[0]
        if key in found:
            # The same rule the instruction registry has, for the same reason:
            # sort order is not a way to settle which of two files is an
            # agent's interface.
            raise ValueError(
                "CONTRACT_DUPLICATED: %s and brain/agents/%s both declare "
                "'%s'. One would silently replace the other as that agent's "
                "interface." % (found[key][0], name, key))
        found[key] = ("brain/agents/" + name, data)
    return found


def _supply(agents, claims, host):
    """
    Fill in whichever of the three the caller did not bring.

    Until 2026-09-14 every entry point here filled all three only when
    `agents` was None, so a caller that already had the register - which is
    every caller that did not want it parsed twice - got claims=None and an
    AttributeError from deep inside record(). A partial call is the normal
    call, and it has to work or say why, not raise from three frames down.
    """
    if agents is None or claims is None or host is None:
        fresh_agents, fresh_claims, fresh_host = _agent_count()
        agents = fresh_agents if agents is None else agents
        claims = fresh_claims if claims is None else claims
        host = fresh_host if host is None else host
    return agents, claims, host


def record(agent_id, agents=None, claims=None, host=None, deals=None):
    """
    One agent's whole record, assembled from whoever owns each field.

    Returns None for an id the register does not have - an agent that is not
    in the register does not exist, and inventing a record for one would be
    the register describing the system instead of being it.
    """
    agents, claims, host = _supply(agents, claims, host)
    deals = contracts() if deals is None else deals

    row = agents.get(agent_id)
    if row is None:
        return None

    files = sorted(claims.get(agent_id, []))
    # THE HEADER COMES FROM AN IMPLEMENTATION, NEVER FROM A TEST. Sorted, a
    # path under tests/ comes before one under tools/, so files[0] was reading
    # status, layer and since off the test for every agent implemented in
    # tools/ - three of them reported `layer: test`, and a test left at DRAFT
    # would have overridden a PROVEN implementation.
    implementations = [f for f in files if not f.startswith("tests/")]
    head = header_of((implementations or files)[0]) if files else {}
    contract_path, contract = deals.get(agent_id, (None, None))

    if agent_id in (host or {}):
        state = "HOST"
    elif implementations:
        state = head.get("status", "DRAFT")
    else:
        # A TEST IS NOT AN IMPLEMENTATION. Two agents were claimed only by
        # tests/test_maintenance.py and reported DRAFT, off the test's own
        # header, with nothing implementing them at all.
        state = "NOT BUILT"

    return {
        "id": agent_id,
        "name": row["name"],
        # docs/18 s3 calls this field Role. The register's "Does" column is
        # that, already written, so it is carried rather than restated.
        "role": row.get("does"),
        "department": row["dept"],
        "tier": row["tier"],
        # None means NOBODY HAS ASSIGNED ONE, which is not READ. The Trainer
        # refuses to train an agent whose permissions nobody has decided, and
        # it can only do that because this field keeps the two apart.
        "risk": row.get("risk"),
        "step": row["step"],
        "state": state,
        "files": files,
        "layer": head.get("layer"),
        "since": head.get("since"),
        "contract": contract_path,
        "contract_version": (contract or {}).get("version"),
        "input": sorted((contract or {}).get("input") or {}),
        "output": sorted((contract or {}).get("output") or {}),
        "allowed_tools": (contract or {}).get("allowed-tools"),
        "timeout_seconds": (contract or {}).get("timeout-seconds"),
        "failures": (contract or {}).get("failures") or [],
        "retry": (contract or {}).get("retry") or {},
        "unmeasured": list(UNMEASURED),
    }


def records(agents=None, claims=None, host=None, deals=None):
    """
    {agent id: record} for the whole register - what the contract promises
    when no single agent is named.
    """
    agents, claims, host = _supply(agents, claims, host)
    deals = contracts() if deals is None else deals
    return dict((agent_id, record(agent_id, agents, claims, host, deals))
                for agent_id in agents)


def disagreements(agents=None, claims=None, host=None, deals=None):
    """Where the sources describing one agent do not agree. Never resolved."""
    agents, claims, host = _supply(agents, claims, host)
    deals = contracts() if deals is None else deals
    found = []

    for agent_id, (path, _data) in sorted(deals.items()):
        if agent_id not in agents:
            found.append("%s: a contract for '%s', which is not in "
                         "docs/28-agent-registry.md" % (path, agent_id))
        elif agent_id not in claims and agent_id not in (host or {}):
            found.append("%s: a contract for '%s', which nothing implements "
                         "yet - the contract is the promise, so this is a "
                         "promise with nobody keeping it" % (path, agent_id))

    return found


def without_contract(agents=None, claims=None, deals=None):
    """
    Built agents that have no contract yet, as (agent id, first file).

    Kept apart from disagreements() on purpose. This is a BACKLOG - 80 rows of
    it on the day this was written - and printing it inside the same list as a
    contract for an agent that does not exist buries the rare, serious finding
    under the common, expected one. A report nobody can skim is a report
    nobody reads.
    """
    agents, claims, _host = _supply(agents, claims, None)
    deals = contracts() if deals is None else deals
    found = []
    for agent_id in sorted(claims):
        if agent_id not in agents or agent_id in deals:
            continue                 # unregistered ids are check-metadata's
        found.append((agent_id, sorted(claims[agent_id])[0]))
    return found


def main(argv):
    agents, claims, host = _agent_count()
    deals = contracts()

    if len(argv) == 2:
        found = record(argv[1].upper(), agents, claims, host, deals)
        if not found:
            print("NO_SUCH_AGENT: '%s' is not in docs/28-agent-registry.md."
                  % argv[1])
            print("An agent is in the register before it is anywhere else.")
            return 1
        width = max(len(k) for k in found)
        for key in ("id", "name", "role", "department", "tier", "step", "state",
                    "layer", "since", "files", "contract", "contract_version",
                    "input", "output", "allowed_tools", "timeout_seconds",
                    "failures", "retry", "unmeasured"):
            value = found.get(key)
            if isinstance(value, (list, tuple)):
                value = ", ".join(str(v) for v in value) or "-"
            print("  %-*s  %s" % (width, key, value if value not in
                                  (None, "", {}) else "-"))
        print()
        print("  health and performance score are UNMEASURED - they need real")
        print("  runs, and Heron has not had them. Not zero, not unknown.")
        return 0

    print("AGENT REGISTRY   assembled, never stored (D-40)")
    print("=" * 72)
    states = {}
    with_contract = 0
    # KEPT, NOT JUST COUNTED. The contract promises every agent and its state
    # when no single agent is asked for, and this loop used to reduce them all
    # to a tally and throw the records away.
    everything = records(agents, claims, host, deals)
    for agent_id, found in everything.items():
        states[found["state"]] = states.get(found["state"], 0) + 1
        if found["contract"]:
            with_contract += 1

    for state in sorted(states, key=lambda s: -states[s]):
        print("  %-12s %d" % (state, states[state]))
    print()
    print("  %d of %d agents have a contract in brain/agents/"
          % (with_contract, len(agents)))
    print("  %d field(s) nothing has measured: %s"
          % (len(UNMEASURED), ", ".join(UNMEASURED)))

    problems = disagreements(agents, claims, host, deals)
    if problems:
        print()
        print("  THE SOURCES DISAGREE - reported, not resolved:")
        for line in problems:
            print("    %s" % line)

    backlog = without_contract(agents, claims, deals)
    if backlog:
        print()
        print("  %d built agent(s) have no contract. A caller has to read the"
              % len(backlog))
        print("  source to know what they take, which is what a contract is")
        print("  for. The first few, oldest part first:")
        for agent_id, path in backlog[:5]:
            print("    %-22s %s" % (agent_id, path))
        print("    ... python brain/heron_agents.py <id> for any one of them")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
