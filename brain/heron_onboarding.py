# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-INS-ONB-010
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
First-run onboarding - once, and then nothing.

    python brain/heron_onboarding.py

WHAT IT IS FOR (docs/28, HERON-INS-ONB-010)
--------------------------------------------
"Guides the user once, then gets out of the way." T2, risk READ. docs/00d
s33 puts it the same way: "Then guide the user once. After that, normal
usage should be almost invisible."

ONCE IS THE WHOLE INSTRUCTION, SO IT IS THE WHOLE DESIGN
-----------------------------------------------------------
Every onboarding anybody has ever disliked was one that did not stop. So
a second run is REFUSED - not shortened, not "a quick reminder". Getting
out of the way means there is no second version to fall back to, and an
agent with a shorter second message is an agent that will be run twice.

Whether it has already run is HANDED IN, not decided here: it is a fact
about this install that outlives any one process, and an agent that kept
it in memory would introduce itself again after every restart.

IT DOES NOT GUIDE A USER THROUGH A BROKEN INSTALL
---------------------------------------------------
It reads HERON-INS-HLT-009's REPORT, not its `complete()`. Those are
different questions and the difference matters here: `complete()` is step
16's sentence and is all-or-nothing, so taking it would mean one WARNING
anywhere silences the entire guide - and most first runs have one.

A FAILED install gets nothing, because a cheerful tour of features that do
not work teaches the user that Heron's own account of itself is
unreliable, on their first contact with it. Anything less than FAILED is
guided, MINUS the topics whose own step is not HEALTHY. Those are dropped
and named, never demonstrated: "ask Heron about a PDF" is a bad first
instruction on a machine that cannot read one, and it is the kind of bad
instruction that looks fine to whoever wrote the guide.

AND IT EXPLAINS AWAY WHAT IS MERELY ABSENT
--------------------------------------------
An optional capability missing is not a broken step - the install can be
complete and Heron still have no PDF reader. HERON-INS-DEP-005 already
produces the sentence for each of those, so those sentences are carried
through unchanged rather than reworded here.

IT CHOOSES WHAT TO SAY, NOT HOW TO SAY IT
-------------------------------------------
The register marks this row T2. D-01 gives the wording and the level to
the host (HERON-ORC-PER-003, HERON-ORC-SUM-006), so this agent returns
the TOPICS - what is worth showing, in order, with what each rests on -
and the host writes the sentences a person reads. That is why nothing
here calls a model: the one scoped call that makes this T2 belongs to the
host, and duplicating it would be two agents phrasing the same greeting.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# HERON-OPS-HLT-004's word, through HERON-INS-HLT-009. The only state that
# silences the guide completely.
FAILED = "FAILED"

# docs/00d s33's own four, plus what each one needs to be worth showing.
# A topic whose capability is missing is dropped rather than demonstrated.
TOPICS = (
    ("knowledge-base", "where what Heron knows about this project lives",
     "brain-initialised"),
    ("vector-db", "how Heron finds the right thing rather than the first",
     "rag-initialised"),
    ("agents", "what Heron can be asked to do", "tools-registered"),
    ("skills", "the jobs it already knows how to do", "skills-installed"),
    ("fragments", "the Revit work it can carry out", "fragments-indexed"),
)


def guide(install=None, already_guided=False, degraded=None):
    """
    {topics, left_out, why} - or a refusal. It says nothing twice.

    `install` is HERON-INS-HLT-009's REPORT - not its `complete()`, which
    is all-or-nothing and would let one WARNING silence the whole guide.
    `degraded` is HERON-INS-DEP-005's list, carried through unchanged.
    """
    if already_guided:
        return {"guide": False, "refused": "ALREADY_GUIDED",
                "why": "this install has been guided. docs/00d s33: guide "
                       "the user once, then normal usage should be almost "
                       "invisible - so there is no shorter second version "
                       "to fall back on. An agent with one of those is an "
                       "agent that will be run twice."}

    if not isinstance(install, dict) or not install.get("steps"):
        return {"guide": False, "refused": "NO_INSTALL_REPORT",
                "why": "nothing says whether the install worked. docs/07 s1 "
                       "puts the health check at step 15 and this after it, "
                       "in that order, and guiding somebody through an "
                       "install nobody checked is the same as guiding them "
                       "through a broken one - it just takes longer to find "
                       "out."}

    if install.get("state") == FAILED:
        return {"guide": False, "refused": "INSTALL_FAILED",
                "state": install.get("state"),
                "why": "HERON-INS-HLT-009 reports the install FAILED. A "
                       "cheerful tour of features that do not work teaches "
                       "the user that Heron's own account of itself is "
                       "unreliable, on their first contact with it.",
                "proposal": "fix it, then check it again. This agent is "
                            "READ and does neither."}

    # A step that is not HEALTHY takes its topic out of the guide.
    unhealthy = set()
    for entry in (install.get("steps") or []):
        if isinstance(entry, dict) and entry.get("state") != "HEALTHY":
            unhealthy.add(entry.get("step"))

    missing = {}
    for entry in (degraded or []):
        if isinstance(entry, dict) and entry.get("say"):
            missing[str(entry.get("package") or "").strip()] = entry["say"]

    topics, left_out = [], []
    for name, what, step in TOPICS:
        if step in unhealthy:
            left_out.append({"topic": name, "why": "%s is not HEALTHY, so "
                                                   "showing this would "
                                                   "demonstrate something "
                                                   "that does not work"
                                                   % step})
            continue
        topics.append({"topic": name, "shows": what, "rests_on": step})

    if not topics:
        return {"guide": False, "refused": "NOTHING_WORKS_YET",
                "left_out": left_out,
                "why": "every topic rests on a step that is not HEALTHY. "
                       "There is nothing to introduce, and introducing it "
                       "anyway is the failure this agent exists to avoid."}

    return {
        "guide": True, "topics": topics, "left_out": left_out,
        "say_about_degraded": sorted(missing.values()),
        "why": "%d topic(s) worth showing, %d left out, once."
               % (len(topics), len(left_out)),
        "unjudged": [
            "THIS IS WHAT TO SAY, NOT HOW TO SAY IT. D-01 gives the wording "
            "and the level to the host (HERON-ORC-PER-003, "
            "HERON-ORC-SUM-006), which is why nothing here calls a model - "
            "the one scoped call that makes this row T2 is the host's, and "
            "duplicating it would be two agents phrasing one greeting.",
            "WHETHER THIS INSTALL WAS ALREADY GUIDED WAS HANDED IN. It is a "
            "fact about the install that outlives any one process, and an "
            "agent keeping it in memory would introduce itself again after "
            "every restart.",
            "%s"
            % ("%d optional capability(ies) are absent, and the sentences "
               "about them are HERON-INS-DEP-005's own rather than invented "
               "here." % len(missing) if missing else
               "no optional capability is absent, so nothing had to be "
               "explained away."),
            "nothing was recorded. Whatever remembers that this install has "
            "been guided has to be told by the caller - this agent is READ "
            "and writes nothing, including that.",
        ],
    }


def main(argv):
    print("FIRST-RUN ONBOARDING   once, and then nothing")
    print("=" * 72)

    healthy = {"state": "HEALTHY",
               "steps": [{"step": step, "state": "HEALTHY"}
                         for _, _, step in TOPICS]}
    degraded = [{"package": "pypdf",
                 "say": "Heron is running without pypdf: PDFs cannot be "
                        "ingested. `pip install --user pypdf` gets it."}]

    answer = guide(install=healthy, degraded=degraded)
    print("  %s" % answer["why"])
    for entry in answer["topics"]:
        print("    %-16s %s" % (entry["topic"], entry["shows"]))
    print("  and the one thing to explain away:")
    print("    %s" % answer["say_about_degraded"][0][:96])

    print()
    print("  A topic whose step is not HEALTHY is dropped, not demonstrated:")
    partly = dict(healthy, steps=[
        {"step": step, "state": "HEALTHY" if step != "rag-initialised"
         else "DEGRADED"} for _, _, step in TOPICS])
    answer = guide(install=partly)
    print("    showing %d, left out %d: %s"
          % (len(answer["topics"]), len(answer["left_out"]),
             answer["left_out"][0]["why"][:64]))

    print()
    print("  And it says nothing twice:")
    for label, kw in (("already guided", {"install": healthy,
                                          "already_guided": True}),
                      ("no install report", {}),
                      ("the install FAILED",
                       {"install": dict(healthy, state="FAILED")}),
                      ("nothing works yet",
                       {"install": dict(healthy, state="DEGRADED", steps=[
                           {"step": step, "state": "DEGRADED"}
                           for _, _, step in TOPICS])})):
        answer = guide(**kw)
        print("    %-24s %s" % (label, answer["refused"]))
        print("        %s" % answer["why"][:88])

    print()
    print("  There is no shorter second version to fall back on. An agent")
    print("  with one of those is an agent that will be run twice.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
