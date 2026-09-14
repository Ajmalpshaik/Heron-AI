# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-AHR-VAL-013
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Agent Validation - everything checkable, before anything activates it.

    python brain/heron_validation.py HERON-KRN-EVT-004
    python brain/heron_validation.py --all

WHAT THE REGISTER ASKS FOR
---------------------------
"Before activation: is this agent correct, does it meet its contract, does it
follow the architecture, does it duplicate an existing agent, is its risk level
right. NEVER THE AGENT THAT BUILT IT."

The last four words are a rule, not a note, and they are enforced: pass
`built_by` and validation refuses when it names this agent. Golden Rule 7 - no
agent approves itself - and the register gives the Agent Creator permission to
assign PROPOSED and nothing beyond it.

AND `built_by` IS NOT OPTIONAL IN PRACTICE
-------------------------------------------
Leaving it out skips that check entirely, and until 2026-09-14 the answer
still came back as plain "PASS" - which the deployment ladder reads exactly
like an independent one. The validator could approve its own implementation
by being asked politely.

A run with no builder named now returns PASS_UNVERIFIED, and the VALIDATED
gate will not take it. The verdict word itself carries the difference,
because a marker in a field beside it is a marker somebody has to remember
to read. `--all` reports it as its own column rather than counting it as
REFUSED: an unchecked thing reported as a failed one is this file's own
mistake in reverse.

VALIDATION IS NOT ACTIVATION
-----------------------------
A PASS here means every check that can be run without a model found nothing.
It does not deploy, it does not raise a status, and it does not approve. The
Agent Deployment Agent activates, and a person still signs (D-30).

THE CHECK THAT IS WORTH THE MOST
---------------------------------
A declared failure state that appears nowhere in the implementation or its test
is a failure nobody has seen happen. It is the cheapest lie a contract can
tell: the agent looks like it handles four things going wrong, and the first
time one of them does, the handling turns out to be a word in a YAML file.

WHAT IT CANNOT JUDGE, IT SAYS
------------------------------
"Is this agent correct" is not a checkable question, and reporting it as
checked would make this whole module a rubber stamp. It goes into `unjudged`
with the rest - the wording of the instruction, whether the risk level suits
what the agent actually does, whether a near-duplicate is genuinely the same
job. Those want a model or a person, and the report says which.
"""

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

# A failure state or an allowed tool whose name says it writes, on an agent
# the register calls read-only. Named here rather than guessed at each call.
WRITING_WORDS = ("WRITE", "MODIFY", "DELETE", "CREATE", "PUBLISH", "SYNC")
READ_ONLY_RISKS = ("READ", "ANALYZE")

SELF = "HERON-AHR-VAL-013"


def _sources():
    import heron_agents as REG
    import heron_contract as CON
    agents, claims, host = REG._agent_count()
    return REG, CON, agents, claims, host


def validate(agent_id, built_by=None, agents=None, claims=None, host=None,
             deals=None, risks=None):
    """
    {verdict, findings, unjudged} for one agent. Never raises, never activates.
    """
    REG, CON, all_agents, all_claims, all_host = _sources()
    agents = all_agents if agents is None else agents
    claims = all_claims if claims is None else claims
    host = all_host if host is None else host
    deals = REG.contracts() if deals is None else deals

    # Normalised, because the gate is the whole point of the agent. An
    # identifier that differs only in case is the same agent, and "the
    # validator approved work attributed to itself" is not a sentence that may
    # depend on somebody's shift key.
    if built_by and str(built_by).strip().upper() == SELF:
        return {"verdict": "REFUSED", "refused": "VALIDATOR_IS_THE_BUILDER",
                "findings": ["%s built this agent and cannot also validate it. "
                             "No agent approves itself (Golden Rule 7)."
                             % built_by],
                "unjudged": []}

    if agent_id not in agents:
        return {"verdict": "REFUSED", "refused": "NO_SUCH_AGENT",
                "findings": ["'%s' is not in docs/28-agent-registry.md. An "
                             "agent is in the register before it is anywhere "
                             "else." % agent_id],
                "unjudged": []}

    row = agents[agent_id]
    files = sorted(claims.get(agent_id, []))
    findings, unjudged = [], []

    if not files and agent_id not in host:
        return {"verdict": "REFUSED", "refused": "NOT_IMPLEMENTED",
                "findings": ["nothing implements %s yet, so there is nothing "
                             "to validate." % agent_id],
                "unjudged": []}

    implementation = [f for f in files if not f.startswith("tests/")]
    tests = [f for f in files if f.startswith("tests/")]

    # 1. Somebody other than the implementation asserts it works.
    if not tests:
        findings.append(
            "no test claims %s. docs/24 puts TESTING behind 'tests written by "
            "someone other than the implementer' - a file that only asserts "
            "itself is not that." % agent_id)
    if tests and not implementation:
        findings.append("only a test claims %s - the test is asserting "
                        "something nothing implements." % agent_id)

    # 2. The contract exists and is well formed.
    contract_path, contract = deals.get(agent_id, (None, None))
    if not contract:
        findings.append(
            "no contract in brain/agents/ for %s. A caller has to read the "
            "source to know what it takes, which is what a contract is for."
            % agent_id)
    else:
        broken = CON.validate(contract, set(agents), contract_path)
        findings.extend(broken)

    # 3. A declared failure nothing mentions is a failure nobody has seen.
    if contract:
        text = ""
        for path in files:
            try:
                text += io.open(os.path.join(ROOT, path), encoding="utf-8",
                                errors="replace").read()
            except IOError:                                  # pragma: no cover
                continue
        for failure in contract.get("failures") or []:
            if failure not in text:
                findings.append(
                    "the contract declares %s and no file implementing or "
                    "testing %s mentions it. A failure nothing exercises is a "
                    "failure nobody has seen happen." % (failure, agent_id))

    # 4. The risk level and what the contract implies.
    risk = (risks or {}).get(agent_id) or _risk_from_registry(agent_id)
    if risk in READ_ONLY_RISKS and contract:
        declared = " ".join(list(contract.get("failures") or [])
                            + [str(t) for t in
                               (contract.get("allowed-tools") or [])]).upper()
        for word in WRITING_WORDS:
            if word in declared:
                unjudged.append(
                    "the register calls %s a %s agent, and its contract names "
                    "%s. That may be right - a READ agent can refuse a write -"
                    " but a person should look." % (agent_id, risk, word))
                break

    # 5. Does it duplicate something already here.
    import heron_workforce as WFP
    proposal = WFP.assess(row["name"], row.get("does") or row["name"],
                          agents={k: v for k, v in agents.items()
                                  if k != agent_id},
                          capabilities={})
    if proposal.get("verdict") == WFP.ALREADY_AN_AGENT:
        # A QUESTION, NOT A FINDING. Workforce Planning says in its own
        # docstring that word overlap cannot tell two descriptions of one
        # job from two jobs described alike - so refusing an agent on it
        # would be this module trusting a measure its author does not.
        # It caught Workforce Planning itself against the Fragment
        # Matcher Agent, which are not the same job at all.
        unjudged.append("it shares wording with %s - %s. Word overlap "
                        "cannot tell whether that is the same job."
                        % (proposal["matches"][0]["id"],
                           proposal["matches"][0]["name"]))

    unjudged.append("is this agent CORRECT - not whether it is well formed, "
                    "but whether what it does is the right thing. No check "
                    "answers that; a person or a model does.")
    unjudged.append("is its risk level right for what it actually does - the "
                    "register declares one, and nothing here can read intent.")

    # A PASS THAT COULD NOT CHECK INDEPENDENCE IS NOT A PASS DEPLOYMENT MAY
    # USE. Without `built_by` the check above - "the validator did not build
    # this" - never ran, and until 2026-09-14 the answer still came back as
    # plain "PASS", which HERON-AHR-DEP-012's VALIDATED gate reads exactly
    # like an independent one. The verdict word itself now carries the
    # difference, because a marker in a field beside it is a marker somebody
    # has to remember to read.
    if not str(built_by or "").strip():
        unjudged.append(
            "WHO BUILT THIS WAS NOT SAID, so the one check this agent exists "
            "for - that the validator is not the builder (Golden Rule 7) - "
            "did not run. The verdict is PASS_UNVERIFIED and the deployment "
            "ladder will not take it: pass built_by to get a usable PASS.")
        return {"verdict": "REFUSED" if findings else "PASS_UNVERIFIED",
                "findings": findings, "unjudged": unjudged,
                "files": files, "contract": contract_path}

    return {"verdict": "REFUSED" if findings else "PASS",
            "findings": findings, "unjudged": unjudged,
            "files": files, "contract": contract_path}


def _risk_from_registry(agent_id):
    """
    The Risk column for one agent, read from the register.

    Read here rather than carried by agent-count.py because this is the only
    consumer, and a field nothing reads is a field that goes stale unnoticed.
    """
    path = os.path.join(ROOT, "docs", "28-agent-registry.md")
    with io.open(path, encoding="utf-8") as fh:
        for line in fh:
            if line.startswith("| `%s`" % agent_id):
                cols = [c.strip() for c in line.split("|")]
                if len(cols) >= 6:
                    return re.sub(r"\*|↗", "", cols[5]).strip()
    return None


def main(argv):
    REG, _CON, agents, claims, host = _sources()
    deals = REG.contracts()

    if len(argv) == 2 and argv[1] != "--all":
        answer = validate(argv[1].upper(), agents=agents, claims=claims,
                          host=host, deals=deals)
        print("AGENT VALIDATION   %s" % argv[1].upper())
        print("=" * 68)
        print("  verdict  %s" % answer["verdict"])
        for line in answer["findings"]:
            print("    FAIL   %s" % line)
        print()
        print("  NOT JUDGED HERE:")
        for line in answer["unjudged"]:
            print("    %s" % line)
        return 0 if answer["verdict"] == "PASS" else 1

    print("AGENT VALIDATION   every built agent, before anything activates one")
    print("=" * 72)
    # THE REPORT ASKS WITHOUT A BUILDER, so every clean answer comes back
    # PASS_UNVERIFIED - the independence check cannot run over a whole
    # register nobody has told it who built what. Counting that as REFUSED
    # would be this file's own mistake in reverse: an unchecked thing
    # reported as a failed one. Three buckets, and the middle one says which
    # command turns it into a real PASS.
    passed, unverified, refused = [], [], []
    for agent_id in sorted(claims):
        if agent_id not in agents:
            continue
        answer = validate(agent_id, agents=agents, claims=claims, host=host,
                          deals=deals)
        verdict = answer["verdict"]
        bucket = (passed if verdict == "PASS"
                  else unverified if verdict == "PASS_UNVERIFIED"
                  else refused)
        bucket.append((agent_id, answer))

    print("  PASS              %d" % len(passed))
    print("  PASS_UNVERIFIED   %d   nothing here said who BUILT them, so the"
          % len(unverified))
    print("                        one check this agent exists for could not")
    print("                        run. `heron_validation.py <ID>` is the")
    print("                        same; pass built_by from code for a PASS.")
    print("  REFUSED           %d" % len(refused))
    print()
    print("  The most common finding, and what it means:")
    counted = {}
    for _agent_id, answer in refused:
        for line in answer["findings"]:
            key = "no contract" if "no contract" in line else (
                "no test" if "no test claims" in line else "other")
            counted[key] = counted.get(key, 0) + 1
    for key in sorted(counted, key=lambda k: -counted[k]):
        print("    %-14s %d" % (key, counted[key]))
    print()
    print("  A PASS is not an activation. Deployment activates, and a person")
    print("  still signs (D-30).")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
