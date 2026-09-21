# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-AHR-MEN-014
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Agent Mentor - the senior on the team, who is not assumed to be right.

    python brain/heron_mentor.py

WHAT IT IS FOR (docs/28, HERON-AHR-MEN-014)
--------------------------------------------
"The senior on the team. Pairs a new agent with the proven agent that
currently owns that capability, compares their output through Shadow Mode,
and explains why they diverged. Active only while the new agent is in
SHADOW."

ACTIVE ONLY IN SHADOW, AND THE STAGE IS READ
---------------------------------------------
docs/24 puts SHADOW between VALIDATED and PROVEN: the new agent runs beside
the one doing the work and its answers count for nothing. That is the only
window where a mentor means anything - before it there is nothing to run,
after it the new agent is the one being trusted.

So the student's stage is READ OUT OF THE REGISTER. There is no argument
that says which stage it is in. HERON-AHR-DEP-012 learned that over three
review rounds: a caller that can state its own stage is a caller that can
skip the ladder.

THE MENTOR MUST BE PROVEN, AND NOTHING IS
------------------------------------------
A mentor at DRAFT has never met a real model (docs/24), so pairing with one
teaches the student whatever that draft got wrong - and now with a senior's
authority attached. The Trainer refuses to offer a DRAFT agent as an
approved example for exactly the same reason.

No agent in the register is above DRAFT as this is written, so every real
pairing today ends in NO_PROVEN_OWNER. That is the correct answer and it is
returned rather than softened. Mentoring is not available yet because
nothing has been proven yet, and saying so is the whole value of the check.

THE SENIOR IS NOT THE ANSWER KEY. THIS IS THE IMPORTANT ONE
------------------------------------------------------------
When the two diverge, the obvious move is to treat the proven agent as
correct and the new one as wrong. It is also how a bug gets canonised: the
proven agent is proven against the cases somebody thought of, and a new
agent disagreeing with it is exactly as likely to have found one of those
cases as to have got it wrong.

So `compare()` reports THAT they diverged and WHERE, and never which is
right. The verdict on a divergence is a question for a person or a model,
and Golden Rule 7 says the machine does not close it.

WHAT IT DOES NOT DO
--------------------
It does not run anything. It pairs, and it compares two outputs it is
handed. Running an agent is the Sandbox's job (HERON-AHR-SBX-016), and the
sandbox's own docstring says plainly what it does and does not contain.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# docs/24. The one stage a mentor means anything in.
ACTIVE_STAGE = "SHADOW"

# Who may be a mentor. The same two stages the Trainer will take an approved
# example from, for the same reason.
MENTOR_STAGES = ("PROVEN", "PRODUCTION")


def owners(capability, records):
    """
    Agents whose role reads as covering `capability`, best first.

    Word overlap, borrowed from HERON-AHR-WFP-015 rather than written again
    - one crude matcher in the repository is better than two that disagree.
    It is crude, its own author says so, and nothing here treats a high
    score as an answer: candidates are OFFERED, and an ambiguous field is
    refused rather than resolved.
    """
    import heron_workforce as WFP
    found = []
    for agent_id in sorted(records or {}):
        record = records[agent_id] or {}
        role = record.get("role") or record.get("name") or ""
        score = WFP.overlap(capability, role)
        if score:
            found.append({"agent": agent_id, "score": round(score, 2),
                          "state": record.get("state"), "role": role})
    return sorted(found, key=lambda m: (-m["score"], m["agent"]))


def pair(student_id, capability, mentor_id=None, records=None):
    """
    {pairing, unjudged, why} - or a refusal.

    The student's stage and the mentor's are both read from the register.
    Neither is a parameter.
    """
    student_id = str(student_id or "").strip()
    if not student_id:
        return {"refused": "NO_SUCH_AGENT",
                "why": "no student was named. A pairing has two sides and "
                       "this one decides whether there is a window at all."}

    if not str(capability or "").strip():
        return {"refused": "NO_CAPABILITY_NAMED",
                "why": "a mentor is the proven agent that owns a CAPABILITY. "
                       "Without one there is nothing to look up, and pairing "
                       "by department would pair by filing."}

    if records is None:
        import heron_agents as REG
        try:
            records = REG.records()
        except IOError as exc:
            return {"refused": "REGISTER_UNREADABLE", "why": str(exc)}

    student = records.get(student_id)
    if not student:
        return {"refused": "NO_SUCH_AGENT",
                "why": "%s is not in docs/28-agent-registry.md." % student_id}

    # THE STAGE IS READ, NOT TAKEN. There is no `stage` argument here for
    # the same reason HERON-AHR-DEP-012 has none.
    stage = str(student.get("state") or "").upper()
    if stage != ACTIVE_STAGE:
        return {"refused": "NOT_IN_SHADOW",
                "why": "%s is at %s. A mentor is active only while the new "
                       "agent is in SHADOW (docs/24) - before that there is "
                       "nothing running to compare, and after it the new "
                       "agent is the one being trusted."
                       % (student_id, stage or "no stage")}

    candidates = owners(capability, records)
    proven = [c for c in candidates
              if str(c.get("state") or "").upper() in MENTOR_STAGES
              and c["agent"] != student_id]

    if mentor_id:
        mentor_id = str(mentor_id).strip()
        if mentor_id == student_id:
            return {"refused": "MENTOR_IS_THE_STUDENT",
                    "why": "%s cannot mentor itself. An agent comparing its "
                           "own output against its own output learns the one "
                           "thing it already believed (Golden Rule 7)."
                           % student_id}
        mentor = records.get(mentor_id)
        if not mentor:
            return {"refused": "NO_SUCH_AGENT",
                    "why": "%s is not in docs/28-agent-registry.md."
                           % mentor_id}
        if str(mentor.get("state") or "").upper() not in MENTOR_STAGES:
            return {"refused": "MENTOR_NOT_PROVEN",
                    "why": "%s is at %s. A mentor must be PROVEN or in "
                           "PRODUCTION: one that has never met a real model "
                           "teaches whatever it got wrong, with a senior's "
                           "authority on it."
                           % (mentor_id, mentor.get("state") or "no stage")}
        # AND BEING PROVEN IS NOT THE SAME AS OWNING THIS CAPABILITY.
        # docs/28 defines the mentor as "the proven agent that CURRENTLY
        # OWNS that capability". Checking only the stage let a shadow
        # duct-sizing agent be paired with a proven payroll auditor - both
        # halves of the sentence true, the pairing meaningless.
        if mentor_id not in [c["agent"] for c in candidates]:
            return {"refused": "MENTOR_DOES_NOT_OWN_IT",
                    "why": "%s is proven, and nothing in its role reads as "
                           "covering '%s'. A mentor is the proven agent that "
                           "OWNS the capability (docs/28) - proven at "
                           "something else teaches something else. %s"
                           % (mentor_id, capability,
                              ("The register offers: %s."
                               % ", ".join(c["agent"] for c in proven[:3]))
                              if proven else "Nothing proven covers it.")}
    else:
        if not proven:
            return {"refused": "NO_PROVEN_OWNER",
                    "why": "no PROVEN agent's role covers '%s'. %d agent(s) "
                           "read as covering it and none is above DRAFT, so "
                           "there is nobody to learn from yet. Mentoring is "
                           "not available until something is proven, and that "
                           "is the answer rather than the nearest match."
                           % (capability, len(candidates))}
        if len(proven) > 1 and proven[0]["score"] == proven[1]["score"]:
            return {"refused": "AMBIGUOUS_OWNER",
                    "why": "%d proven agents tie at %.2f for '%s' - %s. Word "
                           "overlap cannot choose between them and will not "
                           "pretend to; name the mentor."
                           % (len(proven), proven[0]["score"], capability,
                              ", ".join(c["agent"] for c in proven[:3]))}
        mentor_id = proven[0]["agent"]

    pairing = {"student": student_id, "mentor": mentor_id,
               "capability": capability, "stage": stage,
               "candidates": candidates[:5]}

    unjudged = [
        "is this mentor the right one? The match is word overlap borrowed "
        "from HERON-AHR-WFP-015, which its own author says cannot tell two "
        "descriptions of one job from two jobs described alike.",
        "nothing has been RUN. This pairs and compares; running the student "
        "is HERON-AHR-SBX-016's job: it is WATCHED, NOT CONTAINED, which is "
        "what D-84 settled and what the top of its own file says.",
    ]
    return {"pairing": pairing, "unjudged": unjudged,
            "why": "%s is in SHADOW and paired with %s for '%s'. %d "
                   "candidate(s) read as covering it."
                   % (student_id, mentor_id, capability, len(candidates))}


def compare(student_output, mentor_output):
    """
    {verdict, differences, unjudged} - and never which one is right.

    AGREED or DIVERGED. On a divergence it reports every field that differs
    and stops: the proven agent is proven against the cases somebody thought
    of, and a new agent disagreeing with it is as likely to have found one
    of those as to be wrong. Deciding is a person's or a model's, and
    Golden Rule 7 says the machine does not close it.
    """
    if not isinstance(student_output, dict) \
            or not isinstance(mentor_output, dict):
        return {"refused": "NOT_COMPARABLE",
                "why": "both sides of a comparison are an agent's output "
                       "mapping. Comparing two strings would report a "
                       "difference in spelling as a difference in judgement."}

    differences = []
    for key in sorted(set(student_output) | set(mentor_output)):
        in_student = key in student_output
        in_mentor = key in mentor_output
        if not in_mentor:
            differences.append({"field": key, "kind": "ONLY_THE_STUDENT",
                                "student": student_output[key],
                                "mentor": None})
        elif not in_student:
            differences.append({"field": key, "kind": "ONLY_THE_MENTOR",
                                "student": None,
                                "mentor": mentor_output[key]})
        elif student_output[key] != mentor_output[key]:
            differences.append({"field": key, "kind": "DIFFERENT_VALUE",
                                "student": student_output[key],
                                "mentor": mentor_output[key]})

    verdict = "AGREED" if not differences else "DIVERGED"
    unjudged = []
    if differences:
        unjudged.append(
            "WHICH ONE IS RIGHT IS NOT ANSWERED HERE, and it is not the "
            "mentor by default. A proven agent is proven against the cases "
            "somebody thought of; a new agent disagreeing with it is as "
            "likely to have found one of those as to have got it wrong. "
            "Treating the senior as the answer key is how a bug becomes the "
            "specification.")
    else:
        unjudged.append(
            "agreeing is not being right. Both may be wrong in the same way "
            "- they were built from the same documents by the same hands.")

    return {"verdict": verdict, "differences": differences,
            "unjudged": unjudged,
            "why": "%d field(s) differ" % len(differences) if differences
                   else "every field matches"}


def main(argv):
    import heron_agents as REG
    records = REG.records()

    print("AGENT MENTOR   the senior, who is not the answer key")
    print("=" * 70)

    real = "HERON-AHR-WFP-015"
    for label, kwargs in [
            ("no capability named", dict(student_id=real, capability="")),
            ("a student the register does not carry",
             dict(student_id="HERON-AHR-NOPE-999",
                  capability="agent contract interface")),
            ("a real agent, which is at DRAFT and not SHADOW",
             dict(student_id=real, capability="agent contract interface")),
    ]:
        answer = pair(records=records, **kwargs)
        print("  %-44s %s" % (label, answer.get("refused", "paired")))
        print("      %s" % answer["why"][:96])

    # SHADOW is a stage nothing has reached, so it is staged here rather than
    # claimed - the register is not edited to make a demo work.
    staged = dict((k, dict(v)) for k, v in records.items())
    staged[real]["state"] = "SHADOW"
    answer = pair(real, "agent contract interface", records=staged)
    print("  %-44s %s" % ("the same agent, staged into SHADOW",
                          answer.get("refused", "paired")))
    print("      %s" % answer["why"][:96])

    # And the same pairing once the candidate is proven - staged too, for
    # the same reason: nothing in the register is above DRAFT.
    staged["HERON-AHR-CON-017"]["state"] = "PROVEN"
    answer = pair(real, "agent contract interface", records=staged)
    print("  %-44s %s" % ("...with a PROVEN owner to pair it with",
                          answer.get("refused", "paired")))
    print("      %s" % answer["why"][:96])

    print()
    print("  Two outputs, compared:")
    for label, a, b in [
            ("the same answer", {"count": 12}, {"count": 12}),
            ("a different number", {"count": 12}, {"count": 11}),
            ("a field only one of them has",
             {"count": 12, "units": "mm"}, {"count": 12})]:
        answer = compare(a, b)
        print("    %-30s %-9s %s"
              % (label, answer["verdict"], answer["why"]))
        print("        %s" % answer["unjudged"][0][:88])
    print()
    print("  Nothing above decided who was right. On a divergence that is a")
    print("  question for a person or a model - the senior being proven is")
    print("  not evidence that the junior is wrong.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
