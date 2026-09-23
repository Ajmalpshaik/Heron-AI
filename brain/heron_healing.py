# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-OPS-HEA-006
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Self-Healing - it repairs derived state freely and proposes everything else.

    python brain/heron_healing.py

WHAT IT IS FOR (docs/28, HERON-OPS-HEA-006)
--------------------------------------------
"Repairs derived state freely; proposes everything else." T1, risk MODIFY -
the one agent in Operations that may actually change something, and the
whole design is the line it may not cross.

WHERE THE LINE IS
------------------
DERIVED state is anything that can be REBUILT from a source of truth. The
agent map, the fragment catalogue, the counts in the register - each has a
generator, and running it again produces the same file. Losing one costs
the seconds it takes to rebuild.

EVERYTHING ELSE is a source of truth: the fragments themselves, the
contracts, the rows a person wrote in the register, a decision, an open
question, a proof draft, a Revit model. Losing one loses something nobody
can regenerate, and "it looked stale" is not a reason to find out.

The line is NOT "does this look harmless". It is: **is there a named thing
that rebuilds it?** If there is, repair means running that. If there is
not, this agent proposes and stops.

IT FAILS CLOSED ON ANYTHING IT CANNOT CLASSIFY
------------------------------------------------
An artefact not in the derived register is NOT_KNOWN_TO_BE_DERIVED and is
proposed, never repaired - even when the name looks obviously like a cache.
An agent with MODIFY that guesses at the line is an agent with MODIFY that
will eventually guess wrong, and the failure is silent: a regenerated file
looks exactly like a correct one.

A REBUILDER WITH NO SOURCE REBUILDS NOTHING
---------------------------------------------
This is the second half of the rule and the easier one to miss. The agent
map is derived from the register; if the register is gone, running the
generator produces an EMPTY map that looks current. So a repair checks that
the source exists first, and refuses with SOURCE_IS_MISSING when it does
not - naming the source, because that is the real problem and the stale
derived file was only the symptom.

IT NEVER DELETES, AND IT RUNS NOTHING ITSELF
----------------------------------------------
`repair()` returns the command that rebuilds - it does not execute it. A
generated file is rewritten by its generator, in a process somebody started
on purpose, and this agent is the thing that says WHICH generator and WHY,
not the thing that runs it. Q-56 asked what contains a process Heron
starts, and D-84 answered it: nothing does, and no separate process is
being built. So an agent with MODIFY that also spawned things would be building
the containment that decision declined.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# WHAT IS DERIVED, WHAT REBUILDS IT, AND WHAT IT IS DERIVED FROM.
# Every entry is a thing a generator in this repository really writes. An
# artefact absent from here is not derived as far as this agent is
# concerned, whatever its name suggests.
DERIVED = {
    "docs/agent-map.html": {
        "rebuilt_by": "python tools/generate-agent-map.py",
        "from": ["docs/28-agent-registry.md"],
    },
    "docs/fragment-catalog.html": {
        "rebuilt_by": "python tools/generate-fragment-catalog.py",
        "from": ["brain/fragments"],
    },
    "docs/28-agent-registry.md#counts": {
        "rebuilt_by": "python tools/recount-agent-registry.py",
        "from": ["docs/28-agent-registry.md"],
        "note": "the COUNTS in that file are derived; the ROWS are not. "
                "recount rewrites the totals and leaves the rows a person "
                "wrote alone, which is why this entry names a fragment of "
                "the file rather than the file",
    },
}

# Named so a refusal can say what kind of thing it is protecting, rather
# than only that it will not touch it.
#
# A REGISTER SPLIT INTO FILES KEEPS ITS NAME. Each decision has had its own
# file since #303, each NEEDS-CHECKING group since #313, and the register's
# done checks their own files since #303 - and a marker naming only the old
# single file let every one of them fall through to the generic refusal:
# still refused, no longer saying what it protects (FRAGMENT-ISSUES row
# 5b-172). The archive's README is its tool's index, so only its group files
# are named. OPEN-QUESTIONS.md is one file per section since 2026-09-23.
SOURCES_OF_TRUTH = (
    ("brain/fragments", "the fragment library - each one is written and "
                        "proved, and nothing regenerates a proof"),
    ("brain/agents", "the agent contracts, which are the promises"),
    ("docs/DECISIONS.md", "decisions, which are a record of what was chosen "
                          "and cannot be re-derived from the code"),
    ("docs/decisions/", "one decision's full record - what was chosen, "
                        "which cannot be re-derived from the code"),
    ("docs/OPEN-QUESTIONS.md", "open questions"),
    ("docs/open-questions/", "one section of the open questions, which only "
                             "the owner answers"),
    ("docs/NEEDS-CHECKING.md", "the register of what nothing has proved"),
    ("docs/needs-checking/", "one group of the register of what nothing "
                             "has proved"),
    ("docs/needs-checking-archive/group-", "the register's done checks, "
                                           "their words unchanged from "
                                           "NEEDS-CHECKING.md"),
    ("brain/proof-drafts", "proof drafts - evidence from real runs, which "
                           "is the one thing in this repository that a "
                           "machine cannot make more of"),
    (".rvt", "a Revit model. Not Heron's to repair under any circumstances"),
    (".rfa", "a Revit family"),
)


def classify(artefact):
    """
    {derived, rebuilt_by, from, why} for one artefact.

    Anything not in DERIVED comes back derived=False. That is the fail-closed
    half: a name that looks like a cache is not evidence that something can
    rebuild it.
    """
    artefact = str(artefact or "").strip()
    if artefact in DERIVED:
        entry = DERIVED[artefact]
        return {"derived": True, "rebuilt_by": entry["rebuilt_by"],
                "from": list(entry["from"]),
                "why": entry.get("note", "a generator in this repository "
                                         "writes it, and running that "
                                         "generator produces it again")}
    for marker, what in SOURCES_OF_TRUTH:
        if marker in artefact:
            return {"derived": False, "rebuilt_by": None, "from": [],
                    "why": "%s is %s. Nothing regenerates it."
                           % (artefact, what)}
    return {"derived": False, "rebuilt_by": None, "from": [],
            "why": "%s is not in the derived register, and this agent does "
                   "not guess. A name that looks like a cache is not "
                   "evidence that anything can rebuild it, and a wrong "
                   "guess here is silent - a regenerated file looks exactly "
                   "like a correct one." % artefact}


def repair(artefact, root=None):
    """
    {repaired, command, why} - or a refusal. Nothing is executed or deleted.

    A derived artefact comes back with the COMMAND that rebuilds it, for a
    process somebody started on purpose to run. This agent says which
    generator and why; it is not the thing that runs it.
    """
    root = root or ROOT
    found = classify(artefact)

    if not found["derived"]:
        return {"repaired": False, "refused": "NOT_KNOWN_TO_BE_DERIVED",
                "why": found["why"],
                "proposal": "look at %s by hand, or add it to "
                            "heron_healing.DERIVED with the generator that "
                            "rebuilds it and what it is rebuilt FROM - both, "
                            "because a rebuilder with no source rebuilds "
                            "nothing." % artefact}

    # A REBUILDER WITH NO SOURCE REBUILDS NOTHING. Running the generator
    # against a missing source produces an EMPTY artefact that looks
    # current, which is worse than the stale one it replaced.
    missing = [source for source in found["from"]
               if not os.path.exists(os.path.join(root,
                                                  source.split("#")[0]))]
    if missing:
        return {"repaired": False, "refused": "SOURCE_IS_MISSING",
                "why": "%s is derived from %s, and %s is not there. Running "
                       "%s now would produce an EMPTY %s that looks current - "
                       "the stale file was the symptom and this is the "
                       "problem."
                       % (artefact, ", ".join(found["from"]),
                          ", ".join(missing), found["rebuilt_by"], artefact),
                "proposal": "restore %s first. Nothing about the derived "
                            "file is worth fixing until it has something to "
                            "derive from." % ", ".join(missing)}

    return {"repaired": True, "command": found["rebuilt_by"],
            "from": found["from"],
            "why": "%s is derived: %s rebuilds it from %s. The command is "
                   "returned rather than run - a generator belongs in a "
                   "process somebody started on purpose, and D-84 settled "
                   "that nothing contains one this agent starts."
                   % (artefact, found["rebuilt_by"], ", ".join(found["from"]))}


def triage(artefacts, root=None):
    """
    {repairable, proposed, why} for a list - what diagnostics handed over.

    The split IS the answer. A caller gets two lists and can run one of
    them; it never gets a single list it has to sort itself.
    """
    if not artefacts:
        return {"refused": "NOTHING_TO_HEAL",
                "why": "nothing was reported broken. An empty repair run is "
                       "not a healthy system and is not an unhealthy one - "
                       "it is a run nobody asked for."}

    repairable, proposed = [], []
    for artefact in artefacts:
        answer = repair(artefact, root=root)
        (repairable if answer.get("repaired") else proposed).append(
            dict(answer, artefact=artefact))
    return {"repairable": repairable, "proposed": proposed,
            "why": "%d of %d can be rebuilt from something; the other %d "
                   "need a person, and each says why."
                   % (len(repairable), len(artefacts), len(proposed))}


def main(argv):
    print("SELF-HEALING   derived state freely, everything else proposed")
    print("=" * 72)

    answer = triage([
        "docs/agent-map.html",
        "docs/fragment-catalog.html",
        "brain/fragments/select-all-ducts.yaml",
        "docs/DECISIONS.md",
        "brain/proof-drafts/runs/align-mep-elevation.json",
        "Project1 work_ajmal.al.rvt",
        "some/cache/that/looks/temporary.tmp",
    ])
    print("  %s" % answer["why"])

    print()
    print("  REPAIRED FREELY (the command, not the running of it):")
    for entry in answer["repairable"]:
        print("    %-34s %s" % (entry["artefact"], entry["command"]))

    print()
    print("  PROPOSED, NEVER REPAIRED:")
    for entry in answer["proposed"]:
        print("    %-34s %s" % (entry["artefact"], entry["refused"]))
        print("        %s" % entry["why"][:88])

    print()
    print("  A rebuilder with no source rebuilds nothing:")
    answer = repair("docs/agent-map.html", root="/nowhere-at-all")
    print("    %s" % answer["refused"])
    print("      %s" % answer["why"][:92])
    print("      %s" % answer["proposal"][:92])

    print()
    print("  The line is NOT 'does this look harmless'. It is: is there a")
    print("  NAMED thing that rebuilds it? An agent with MODIFY that guesses")
    print("  at that line will eventually guess wrong, and the failure is")
    print("  silent - a regenerated file looks exactly like a correct one.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
