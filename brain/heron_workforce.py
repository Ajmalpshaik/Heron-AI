# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-AHR-WFP-015
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Workforce Planning - the agent that says no.

    python brain/heron_workforce.py "Duct Counter" "counts ducts in a view"

WHAT IT IS FOR (docs/28, HERON-AHR-WFP-015)
--------------------------------------------
"Before anything is hired: does a capability already cover this, can an
existing agent be extended, is this a fragment rather than an agent? THIS IS
THE GUARD AGAINST AGENT EXPLOSION."

250 agents is a number this repository had to correct once already, upward,
because departments were added faster than anybody added them up. The failure
mode is not one bad agent - it is thirty agents that each do a tenth of
something an existing one already does, and a register nobody can hold in their
head.

So this is built BEFORE the factory that can create agents, not after it. Built
after, it guards nothing: by then the 125 department agents exist.

THE LADDER, CHEAPEST QUESTION FIRST
------------------------------------
  1  is there already an agent whose job this is        ALREADY_AN_AGENT
  2  does a fragment already provide this capability    ALREADY_A_CAPABILITY
  3  is this one composable Revit operation             THIS_IS_A_FRAGMENT
  4  is there a neighbour that could absorb it          EXTEND_EXISTING
  5  none of the above                                  PROPOSE_HIRING

Three of those five are a no, and that is the intended ratio. An agent is the
most expensive answer available - it needs a contract, a test, a place in the
register and somebody to maintain it - so it is the last one tried, never the
first.

PROPOSE_HIRING IS A PROPOSAL. Golden Rule 7: no agent approves itself, and
docs/28 gives the Agent Creator permission to assign PROPOSED and nothing
beyond it. This module's highest verdict is a recommendation with its reasons
attached, for a person to accept or refuse.

WHAT IT CANNOT DO WITHOUT A MODEL, AND SAYS SO
-----------------------------------------------
Word overlap finds "Duct Counter" against "Revit Element Agent" only if they
share words. Two descriptions of one job, written by two people, often share
almost none - and deciding they are the same job is exactly the judgement a
model is for. So it asks the Model Router for that judgement, and when no
adapter is registered it reports the question as UNJUDGED rather than
returning a clean "no overlap found". A silent "no" from a check that never
ran is how the guard fails.
"""

import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

# Words that carry no meaning for an overlap comparison. Short on purpose: a
# long stop list quietly deletes the domain words that matter, and "view",
# "model" and "element" are exactly the words two proposals would share.
STOPWORDS = frozenset("""
a an the and or of for to in on at by with from that this it its is are be
agent heron revit which what when how all any every one two some more most
""".split())

# How much of the shorter description has to be shared before it is worth a
# person's attention. Low enough to surface a near-duplicate, high enough that
# two unrelated proposals sharing "view" do not match.
CLOSE = 0.45
WORTH_MENTIONING = 0.25

ALREADY_AN_AGENT = "ALREADY_AN_AGENT"
ALREADY_A_CAPABILITY = "ALREADY_A_CAPABILITY"
THIS_IS_A_FRAGMENT = "THIS_IS_A_FRAGMENT"
EXTEND_EXISTING = "EXTEND_EXISTING"
PROPOSE_HIRING = "PROPOSE_HIRING"

CAPABILITY_SHAPE = re.compile(r"^[A-Z][A-Z0-9]*(_[A-Z0-9]+)+$")


def words(text):
    """
    The meaningful words, singularised crudely.

    "ducts" and "duct" have to meet, and a stemmer is a dependency for one
    trailing letter. Words of three letters or fewer keep their s: "views"
    would become "view" correctly but "gas" would become "ga".
    """
    found = set()
    for word in re.findall(r"[a-z0-9]+", (text or "").lower()):
        if word in STOPWORDS or len(word) <= 2:
            continue
        if len(word) > 4 and word.endswith("s") and not word.endswith("ss"):
            word = word[:-1]
        found.add(word)
    return found


def overlap(left, right):
    """
    How much of the SHORTER description the two share, 0.0 to 1.0.

    Against the shorter one rather than the union, because a two-word proposal
    fully contained in a long responsibility is a duplicate - and dividing by
    the union would score that 0.2 and let it through.

    ONE SHARED WORD IS NEVER A MATCH, whatever the arithmetic says. The first
    draft scored "Duct Counter - counts ducts in a view" against "Revit View
    Agent" at 1.00: they share "view", and after the stop words that name has
    one word in it, so the division was by one. A rule that scores a
    coincidence at certainty is worse than no rule, because it is the rule
    that gets believed.
    """
    a, b = words(left), words(right)
    if not a or not b:
        return 0.0
    shared = a & b
    if len(shared) < 2:
        return 0.0
    return len(shared) / float(min(len(a), len(b)))


def fragment_capabilities():
    """
    {capability: [fragment id]} read from the fragments themselves.

    load_all() returns (fragments by id, problems) and the problems are not
    this module's to report - heron_fragment.py and its own gate own them.
    Taking the first half and dropping the second is deliberate rather than
    careless, which is why it is said here.
    """
    import heron_fragment as FRAG
    fragments, _problems = FRAG.load_all()
    found = {}
    for fragment in fragments.values():
        capability = fragment.data.get("capability")
        if capability:
            found.setdefault(capability, []).append(fragment.id)
    return found


def assess(name, purpose, capability=None, department=None,
           agents=None, capabilities=None, router=None):
    """
    {verdict, matches, unjudged, why} for a proposed agent.

    `router` is optional and is asked for the judgement word overlap cannot
    make. Without one the question is reported as unjudged - never answered.
    """
    if not (name or "").strip() or not (purpose or "").strip():
        return {"refused": "NOTHING_TO_ASSESS",
                "why": "a proposal needs a name and a purpose. The purpose is "
                       "what every check below reads, so a vague one gets a "
                       "vague answer."}

    if agents is None:
        import heron_agents as REG
        agents, _claims, _host = REG._agent_count()
    if capabilities is None:
        capabilities = fragment_capabilities()

    described = "%s %s" % (name, purpose)
    matches, unjudged = [], []

    # 1. An agent whose job this already is.
    # Name AND responsibility. An agent's NAME reduces to one or two
    # meaningful words - "Revit Workset Agent" is {workset} once the stop list
    # has run - and one word can never reach the two-word floor. Matching on
    # the name alone made every proposal look new, which for a guard against
    # too many agents is the failure that matters.
    scored = []
    for agent_id, row in agents.items():
        does = row.get("does") or ""
        score = max(overlap(described, row["name"]),
                    overlap(purpose, row["name"]),
                    overlap(purpose, "%s %s" % (row["name"], does)))
        if score >= WORTH_MENTIONING:
            scored.append((score, agent_id, row))
    scored.sort(reverse=True, key=lambda item: (item[0], item[1]))
    for score, agent_id, row in scored[:5]:
        matches.append({"kind": "agent", "id": agent_id, "name": row["name"],
                        "department": row["dept"], "score": round(score, 2)})

    if scored and scored[0][0] >= CLOSE:
        score, agent_id, row = scored[0]
        return _verdict(ALREADY_AN_AGENT, matches, unjudged,
                        "%s (%s) already has this job. Two agents doing a "
                        "tenth of each other's work is how a register stops "
                        "fitting in anybody's head."
                        % (agent_id, row["name"]), router, described, agents)

    # 2. A capability a fragment already provides - by name when one is given,
    # and by words when one is not. The register's question is "does a
    # capability already cover this", and a check that only fires on an exact
    # id answers a narrower question than the one asked.
    for known in sorted(capabilities):
        score = overlap(purpose, known.replace("_", " "))
        if score >= WORTH_MENTIONING:
            matches.append({"kind": "capability", "id": known,
                            "name": ", ".join(sorted(capabilities[known])[:3]),
                            "score": round(score, 2)})
            if score >= CLOSE and not capability:
                return _verdict(ALREADY_A_CAPABILITY, matches, unjudged,
                                "%s already covers this, provided by %s. Ask "
                                "for the capability, never for who does it."
                                % (known, ", ".join(sorted(
                                    capabilities[known])[:3])),
                                router, described, agents)

    if capability and capability in capabilities:
        matches.append({"kind": "capability", "id": capability,
                        "name": ", ".join(sorted(capabilities[capability])),
                        "score": 1.0})
        return _verdict(ALREADY_A_CAPABILITY, matches, unjudged,
                        "%s is already provided by %s. Ask for the capability, "
                        "never for who does it."
                        % (capability, ", ".join(sorted(
                            capabilities[capability]))),
                        router, described, agents)

    # 3. One composable Revit operation is a fragment, not an agent (D-29).
    if capability and CAPABILITY_SHAPE.match(capability):
        return _verdict(THIS_IS_A_FRAGMENT, matches, unjudged,
                        "a named capability in one operation's shape is a "
                        "FRAGMENT - a composable piece of the how, with a "
                        "declared contract (D-29). An agent is the expensive "
                        "answer, and this does not need one.",
                        router, described, agents)

    # 4. A neighbour that could absorb it.
    if department:
        neighbours = [(score, agent_id, row) for score, agent_id, row in scored
                      if row["dept"] == department]
        if neighbours:
            score, agent_id, row = neighbours[0]
            return _verdict(EXTEND_EXISTING, matches, unjudged,
                            "%s (%s) is in the same department and close "
                            "enough to absorb this. Extending one agent costs "
                            "a method; hiring costs a contract, a test, a "
                            "register row and somebody to maintain it."
                            % (agent_id, row["name"]),
                            router, described, agents)

    return _verdict(PROPOSE_HIRING, matches, unjudged,
                    "nothing found that covers this. That is a PROPOSAL and "
                    "not an approval - no agent approves itself (Golden Rule "
                    "7), and the Agent Creator may only ever assign PROPOSED.",
                    router, described, agents)


def _verdict(verdict, matches, unjudged, why, router, described, agents):
    """Attach the judgement a model would have made, or say it was not made."""
    question = ("is '%s' the same job as one of the %d agents already in the "
                "register, in different words?" % (described, len(agents)))
    if router is None:
        unjudged.append(question + " - no router was given, so nothing asked")
    else:
        routed = router.route("CLASSIFY")
        if "adapter" not in routed:
            unjudged.append("%s - %s" % (question, routed.get("why", "")))
        else:
            unjudged.append("%s - would be asked of %s; this module does not "
                            "call it yet" % (question, routed["adapter"]))
    return {"verdict": verdict, "matches": matches, "unjudged": unjudged,
            "why": why}


def main(argv):
    if len(argv) < 3:
        print("usage: python brain/heron_workforce.py NAME PURPOSE "
              "[CAPABILITY] [DEPARTMENT]")
        return 2

    answer = assess(argv[1], argv[2],
                    capability=argv[3] if len(argv) > 3 else None,
                    department=argv[4] if len(argv) > 4 else None)

    print("WORKFORCE PLANNING   the expensive answer is the last one tried")
    print("=" * 70)
    if "refused" in answer:
        print("  refused  %s" % answer["why"])
        return 1
    print("  verdict  %s" % answer["verdict"])
    print("  why      %s" % answer["why"])
    if answer["matches"]:
        print()
        print("  what it found:")
        for match in answer["matches"]:
            print("    %-10s %-22s %-34s %.2f"
                  % (match["kind"], match["id"], match["name"][:34],
                     match["score"]))
    print()
    print("  NOT JUDGED:")
    for line in answer["unjudged"]:
        print("    %s" % line)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
