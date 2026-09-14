# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-AHR-TRN-005
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Agent Trainer - what a new agent is told, and the examples it is not given.

    python brain/heron_trainer.py
    python brain/heron_trainer.py HERON-AHR-WFP-015

WHAT IT IS FOR (docs/28, HERON-AHR-TRN-005)
--------------------------------------------
"Supplies architecture, standards, security rules, approved examples."

Four things, and this file is mostly about the fourth.

THE SECURITY RULES ARE ASSEMBLED, NEVER COPIED
-----------------------------------------------
The rules an agent is given come from heron_instructions (HERON-KRN-PRO-011),
which assembles Constitution articles rather than reproducing them and
refuses an instruction that repeats an article's wording. The Trainer calls
it and does not paraphrase it. A training pack that restated the rules would
be a second copy of the Constitution that nobody updates when the first one
changes.

AND ONLY THE LEVELS AN INSTRUCTION REALLY COVERS
-------------------------------------------------
docs/12 defines seven risk levels. `brain/instructions/` holds three
instructions. The first version of this file mapped everything above
ANALYZE onto `agent.modify` - so a SUGGEST agent, whose whole boundary is
that it proposes and never acts, was handed the text describing how to
change a model, with no line anywhere saying it may only propose.

That is the training layer erasing a distinction the security model
depends on, and erasing it silently: nothing diffs a training pack. So the
four uncovered levels are REFUSED by name, each saying which instruction
has to be written. An agent given the wrong permission class's rules is
worse off than one that was not trained at all.

WHICH RULES DEPEND ON RISK, AND RISK IS THE REGISTER'S
-------------------------------------------------------
A READ agent is told its permission stops at reading; a MODIFY agent must
not be. docs/28-agent-registry.md owns risk, so the Trainer reads the row
and chooses; it never assigns.

And a row with an em-dash in the Risk column has NOT been assigned READ - it
has not been assigned anything, which is a different fact. Flattening the
two would hand a new agent whichever rules happened to be the default,
which is the quiet version of getting permissions wrong. So it refuses:
NO_RISK_ASSIGNED, naming the row to fill in. 57 of the register's rows are
in that state as this is written, which is the number the refusal exists to
surface.

THE STANDARDS ARE THE GATES THAT ACTUALLY RUN
----------------------------------------------
Not a sentence naming four documents. `tools/check-*.py` is listed from
disk, so the standards an agent is trained on are the checks it will really
be measured by, and a gate added tomorrow is in the pack the same day. A
typed list is a count with extra steps, and this repository has the register
of times that went wrong.

THE APPROVED EXAMPLES, WHICH IS THE POINT
------------------------------------------
An "approved example" is an agent at PROVEN or PRODUCTION. Nothing lower
qualifies, and the reason is not tidiness: a DRAFT agent has never met a
real model (docs/24), so handing one over as an example teaches the new
agent whatever that draft got wrong, with the Trainer's authority behind it.

As this is written NO agent in the register is above DRAFT. So the pack
comes back with an empty example list and says so in `unjudged`, every time,
rather than substituting the nearest thing. An empty list that says it is
empty is worth more than a full one nobody checked.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOLS_DIR = os.path.join(ROOT, "tools")

# docs/24's ladder. Only these two are examples anything should copy.
APPROVED_STAGES = ("PROVEN", "PRODUCTION")

# Risk word -> the instruction that carries the right rules. A risk word
# nobody recognises is not quietly read as the mildest one.
#
# ONLY THE LEVELS AN INSTRUCTION ACTUALLY EXISTS FOR. The first version
# mapped everything above ANALYZE to `agent.modify`, whose text describes
# how to CHANGE a model and contains no line saying "you may only propose".
# A SUGGEST agent read that as its rules; so did EXECUTE, PUBLISH and ADMIN.
# docs/12 defines seven levels and brain/instructions/ holds three, and
# collapsing the difference in the training layer erases it everywhere
# downstream - silently, because a training pack is not diffed against
# anything.
BY_RISK = {
    "READ": "agent.read",
    "ANALYZE": "agent.read",
    "MODIFY": "agent.modify",
}

# The levels docs/12 defines that no instruction covers yet. Refused by
# name rather than mapped to the nearest thing: an agent given the wrong
# permission class's rules is worse off than one that was not trained.
NO_INSTRUCTION_YET = {
    "SUGGEST": "agent.suggest - it proposes and never acts, and no existing "
               "instruction says that",
    "EXECUTE": "agent.execute - it runs an operation without changing the "
               "model, which is neither of the two that exist",
    "PUBLISH": "agent.publish - it puts something where others will read it",
    "ADMIN": "agent.admin - the level that can change permissions, which "
             "needs its own rules more than any of them",
}


def standards():
    """
    The gates that actually run, listed from tools/ rather than typed.

    A gate added tomorrow is in tomorrow's training pack without anybody
    remembering to add it here.
    """
    if not os.path.isdir(TOOLS_DIR):
        return []
    return sorted("tools/%s" % n for n in os.listdir(TOOLS_DIR)
                  if n.startswith("check-") and n.endswith(".py"))


def approved_examples(records):
    """
    Agent ids at PROVEN or PRODUCTION, with what each one is an example OF.

    Empty is the honest answer today and is returned as such. Nothing here
    falls back to "the closest we have".
    """
    found = []
    for agent_id in sorted(records or {}):
        record = records[agent_id] or {}
        if str(record.get("state") or "").upper() in APPROVED_STAGES:
            found.append({"agent": agent_id,
                          "state": record.get("state"),
                          "does": record.get("does") or record.get("name")})
    return found


def train(agent_id, agents=None, records=None, known=None, rules=None):
    """
    {pack, unjudged, why} for an agent in the register - or a refusal.

    The pack is assembled on every call and stored nowhere (D-40). A stored
    training pack is a copy of the Constitution with a date on it.
    """
    agent_id = str(agent_id or "").strip()
    if not agent_id:
        return {"refused": "NO_SUCH_AGENT",
                "why": "no agent was named. Training is per agent because "
                       "the rules depend on what that agent is allowed to do."}

    if agents is None:
        import heron_agents as REG
        try:
            agents, _claims, _host = REG._agent_count()
        except IOError as exc:
            return {"refused": "REGISTER_UNREADABLE", "why": str(exc)}

    row = agents.get(agent_id)
    if row is None:
        return {"refused": "NO_SUCH_AGENT",
                "why": "%s is not in docs/28-agent-registry.md. An agent that "
                       "is not in the register does not exist, and training "
                       "one would be training an assumption." % agent_id}

    # AN EM-DASH IS NOT "READ". It is nobody having decided, and the two
    # must not collapse into the same training pack.
    risk = (row.get("risk") or "").strip().upper()
    if not risk:
        return {"refused": "NO_RISK_ASSIGNED",
                "why": "the Risk column of %s's row is empty, so nothing here "
                       "knows which rules it should be given. That is not the "
                       "same as READ and is not treated as it. Fill the "
                       "column in docs/28-agent-registry.md." % agent_id}

    if risk in NO_INSTRUCTION_YET:
        return {"refused": "NO_INSTRUCTION_FOR_RISK",
                "why": "%s is %s and no instruction covers that level. What "
                       "is missing is %s. Until it exists this agent will not "
                       "hand over another level's rules: giving a %s agent "
                       "`agent.modify` tells it how to change a model and "
                       "says nothing about the boundary it actually has."
                       % (agent_id, risk, NO_INSTRUCTION_YET[risk], risk)}

    instruction_id = BY_RISK.get(risk)
    if instruction_id is None:
        return {"refused": "UNKNOWN_RISK_LEVEL",
                "why": "'%s' is not one of docs/12's risk levels (%s). A word "
                       "nobody recognises is not read as the mildest one."
                       % (risk, ", ".join(sorted(set(BY_RISK)
                                                 | set(NO_INSTRUCTION_YET))))}

    import heron_instructions as PRO
    try:
        text, articles_used = PRO.compose(instruction_id, known=known,
                                          rules=rules)
    except KeyError as exc:
        return {"refused": "INSTRUCTION_NOT_FOUND",
                "why": "the instruction '%s' this risk level needs could not "
                       "be assembled: %s. The pack is not returned without "
                       "it - an agent running with a rule it was never given "
                       "is the failure this refusal exists for."
                       % (instruction_id, exc)}
    except ValueError as exc:
        return {"refused": "INCLUDE_CYCLE", "why": str(exc)}

    if records is None:
        import heron_agents as REG
        records = REG.records(agents=agents)
    examples = approved_examples(records)

    pack = {
        "agent": agent_id,
        "risk": risk,
        "instruction": instruction_id,
        "rules": text,
        "articles": list(articles_used),
        "architecture": {
            "layer": "brain, mcp, revit or platform - docs/02, and "
                     "tools/check-structure.py refuses a dependency that "
                     "crosses the wrong way (D-48)",
            "contract": "brain/agents/<id>.yaml, which is data and not "
                        "prose (D-29)",
            "header": "the five-field metadata header, docs/29",
        },
        "standards": standards(),
        "examples": examples,
    }

    unjudged = []
    if not examples:
        unjudged.append(
            "NO APPROVED EXAMPLE EXISTS. An example is an agent at PROVEN or "
            "PRODUCTION (docs/24) and the register has none, so this pack "
            "hands over nothing to copy. A DRAFT agent has never met a real "
            "model; offering one as an example would teach whatever it got "
            "wrong, with this agent's authority behind it.")
    unjudged.append(
        "is this pack ENOUGH for this agent? Nothing here can tell. It "
        "assembles what applies; whether what applies covers the job is a "
        "reading, and docs/28 makes this agent T2 - one scoped call, and no "
        "adapter to make it.")
    if not pack["standards"]:
        unjudged.append(
            "no gates were found under tools/, so the standards list is "
            "empty because nothing was looked at - not because there are "
            "none.")

    return {"pack": pack, "unjudged": unjudged,
            "why": "%s is %s, so it is given '%s': %d article(s), %d gate(s), "
                   "%d approved example(s). The rules are assembled by "
                   "HERON-KRN-PRO-011 and not copied here."
                   % (agent_id, risk, instruction_id, len(articles_used),
                      len(pack["standards"]), len(examples))}


def main(argv):
    import heron_agents as REG
    agents, _claims, _host = REG._agent_count()

    if len(argv) > 1:
        answer = train(argv[1], agents=agents)
        if "refused" in answer:
            print("%s" % answer["refused"])
            print("  %s" % answer["why"])
            return 1
        pack = answer["pack"]
        print("TRAINING PACK   %s" % pack["agent"])
        print("=" * 70)
        print("  risk         %s" % pack["risk"])
        print("  instruction  %s" % pack["instruction"])
        print("  articles     %s" % ", ".join(str(n)
                                              for n in pack["articles"]))
        print("  standards    %d gate(s)" % len(pack["standards"]))
        print("  examples     %d" % len(pack["examples"]))
        for note in answer["unjudged"]:
            print("  unjudged     %s" % note)
        print()
        print(pack["rules"])
        return 0

    print("AGENT TRAINER   what a new agent is told")
    print("=" * 70)

    missing_risk = sorted(a for a, row in agents.items()
                          if not (row.get("risk") or "").strip())
    for agent_id in ("HERON-AHR-WFP-015", "HERON-AHR-DEP-012",
                     "HERON-AHR-CON-017",
                     missing_risk[0] if missing_risk else "HERON-AHR-HR-002",
                     "HERON-AHR-NOPE-999"):
        answer = train(agent_id, agents=agents)
        if "refused" in answer:
            print("  %-22s %s" % (agent_id, answer["refused"]))
            print("      %s" % answer["why"][:96])
        else:
            pack = answer["pack"]
            print("  %-22s %-6s -> %-13s %d article(s), %d gate(s), %d "
                  "example(s)"
                  % (agent_id, pack["risk"], pack["instruction"],
                     len(pack["articles"]), len(pack["standards"]),
                     len(pack["examples"])))
    print()
    print("  %d of %d rows have no risk assigned, and an em-dash is not READ."
          % (len(missing_risk), len(agents)))
    print("  NO agent in the register is at PROVEN or PRODUCTION, so every")
    print("  pack above hands over an empty example list and says so. The")
    print("  nearest thing is not an approved example.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
