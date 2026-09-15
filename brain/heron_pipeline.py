# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-IMP-MAIN-001
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
The import pipeline - sixteen steps, read from the specification, and how
far they actually reach today.

    python brain/heron_pipeline.py [folder]

WHAT IT IS FOR (docs/28, HERON-IMP-MAIN-001)
----------------------------------------------
"Owns the import pipeline end to end. NEVER MODIFIES THE SOURCE FOLDER."
T2, risk MODIFY.

THE ORDER IS READ, NOT REMEMBERED
-----------------------------------
docs/00 s28 numbers the sixteen steps and docs/10 s5 repeats them as a
chain. `steps()` parses that numbered list out of the specification, so
a step added there appears here without an edit and a step removed
stops being expected. A pipeline that carries its own copy of its own
order is a pipeline that will eventually run a different one.

EVERY STEP IS MAPPED TO AN AGENT, AND THE UNCERTAIN ONES SAY SO
-----------------------------------------------------------------
docs/00 s29 lists the fourteen agents in ID ORDER, not in step order,
so the mapping between the two lists is nowhere written down. Most rows
are unmistakable - "walks the folder, identifies file types" is steps 1
and 2 in the register's own words. Two are a reading, and they are
marked as one rather than asserted:

    13. Update metadata    HERON-IMP-MEX-006 EXTRACTS metadata. Writing
                           it back is a transformation, which is
                           HERON-IMP-MIG-009's row.
    16. Save               Nobody's row says "save". PROPOSALS F26 is
                           the same seam from the other side: both
                           indexers read the library, so step 15 cannot
                           see an item until step 16 has run.

THREE STEPS ARE ONE QUESTION, AND THAT IS NOT A DEFECT
--------------------------------------------------------
Steps 3, 6 and 10 - "identify code", "identify documentation",
"classify content" - all land on HERON-IMP-CLS-003, whose row is "code,
documentation, config, metadata, asset". They are one question asked
three times in the specification's prose. Reported as three steps with
one owner rather than quietly collapsed, because the numbering is what
a reader follows.

HOW FAR AN IMPORT GETS TODAY, MEASURED RATHER THAN CLAIMED
------------------------------------------------------------
`run` executes the part that needs neither a model nor a person - the
walk, and the brief HERON-IMP-CLS-003 prepares - and then STOPS, saying
which step it stopped at and what that step is waiting for. Three of
the fourteen agents are not built (HERON-IMP-FEX-004, SEX-005 and
MIG-009, all T3), and an import cannot pass step 4 until they are.

Saying so is the point. docs/10 s5 constraint 5 makes the import
resumable, and resuming means knowing exactly where it stopped.

NOTHING IS WRITTEN AND THE SOURCE FOLDER IS NOT TOUCHED
---------------------------------------------------------
Constraint 1, and the register puts it in this agent's own row in bold.
Every agent this one calls is READ; the MODIFY ones are past the point
it reaches. The suite snapshots the folder - every name, size and
modification time - before and after, and compares.
"""

from __future__ import annotations

import io
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_classify as CLS  # noqa: E402
import heron_walk as WALK  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

SPECIFICATION = os.path.join(ROOT, "docs", "00-master-specification.md")
REGISTER = os.path.join(ROOT, "docs", "28-agent-registry.md")

_NUMBERED = re.compile(r"^\s*(\d{1,2})\.\s+(\S.*?)\s*$")

# Step number -> (agent, whether the agent's own register row names this
# step in so many words). The uncertain rows are marked, not asserted -
# docs/00 s29 lists the agents in ID order and nothing anywhere maps the
# two lists onto each other.
OWNERS = {
    1:  ("HERON-IMP-FIL-002", True),
    2:  ("HERON-IMP-FIL-002", True),
    3:  ("HERON-IMP-CLS-003", True),
    4:  ("HERON-IMP-FEX-004", True),
    5:  ("HERON-IMP-SEX-005", True),
    6:  ("HERON-IMP-CLS-003", True),
    7:  ("HERON-IMP-MEX-006", True),
    8:  ("HERON-IMP-DUP-007", True),
    9:  ("HERON-IMP-CMP-008", True),
    10: ("HERON-IMP-CLS-003", True),
    11: ("HERON-IMP-REN-010", True),
    12: ("HERON-IMP-ARC-011", True),
    13: ("HERON-IMP-MIG-009", False),
    14: ("HERON-IMP-VAL-012", True),
    15: ("HERON-IMP-IDX-013", True),
    16: ("HERON-IMP-MIG-009", False),
}

WHY_UNCERTAIN = {
    13: "HERON-IMP-MEX-006 EXTRACTS metadata; writing it back is a "
        "transformation, which is HERON-IMP-MIG-009's row. Nothing says "
        "which of the two owns this step",
    16: "no agent's row says `save`. PROPOSALS F26 is the same seam from "
        "the other side - both indexers read the library, so step 15 "
        "cannot see an item until step 16 has run",
}

# What the deterministic prefix can reach with no model and no person.
# Step 3 is where a human or a host has to answer, and that is the honest
# end of an unattended run.
UNATTENDED = (1, 2)


def steps(path=None):
    """
    The sixteen, read out of docs/00 s28's numbered list.

    Parsed rather than copied: a step added to the specification shows up
    here with no edit, and one removed stops being expected.
    """
    try:
        text = io.open(path or SPECIFICATION, encoding="utf-8").read()
    except (IOError, OSError):
        return {}

    body = text.split("## 28. Knowledge Import System", 1)
    if len(body) < 2:
        return {}
    body = body[1].split("\n## ", 1)[0]

    out = {}
    for line in body.split("\n"):
        found = _NUMBERED.match(line)
        if not found:
            continue
        out[int(found.group(1))] = found.group(2).rstrip(".")
    return out


def built(register=None):
    """Which Import agents a source file claims, read from the register."""
    try:
        text = io.open(register or REGISTER, encoding="utf-8").read()
    except (IOError, OSError):
        text = ""
    known = set(re.findall(r"`(HERON-IMP-[A-Z]+-\d+)`", text))

    here = os.path.dirname(os.path.abspath(__file__))
    claimed = set()
    for name in sorted(os.listdir(here)):
        if not name.endswith(".py"):
            continue
        try:
            head = io.open(os.path.join(here, name),
                           encoding="utf-8").read(400)
        except (IOError, OSError):
            continue
        for found in re.findall(r"HERON-IMP-[A-Z]+-\d+", head):
            claimed.add(found)
    return known, claimed


def plan(path=None, register=None):
    """
    {planned, steps, unbuilt, uncertain} - or a refusal. Nothing runs.
    """
    order = steps(path)
    if not order:
        return {"planned": False, "refused": "NO_SPECIFICATION",
                "why": "docs/00 s28's numbered list could not be read, so "
                       "there is no order to follow. The pipeline does not "
                       "keep a copy of its own order."}

    known, claimed = built(register)
    rows, unbuilt, uncertain = [], [], []
    # AN AGENT THIS TABLE NAMES THAT THE REGISTER DOES NOT KNOW means the
    # table above has drifted from docs/28 - a row renamed or retired
    # while this mapping stayed put. Reported rather than trusted, because
    # the mapping is the one thing here that IS a copy.
    stranger = sorted(set(agent for agent, _ in OWNERS.values())
                      - known) if known else \
        sorted(set(agent for agent, _ in OWNERS.values()))
    for number in sorted(order):
        agent, certain = OWNERS.get(number, (None, False))
        card = {"step": number, "does": order[number], "agent": agent,
                "certain": certain, "built": agent in claimed}
        if not certain:
            card["why"] = WHY_UNCERTAIN.get(number, "")
            uncertain.append(card)
        if agent and agent not in claimed:
            unbuilt.append(card)
        rows.append(card)

    missing = [number for number in order if number not in OWNERS]
    return {
        "planned": True,
        "of": len(order),
        "steps": rows,
        "unbuilt": unbuilt,
        "uncertain": uncertain,
        "unmapped": missing,
        "unknown_agents": stranger,
        "reaches": max([0] + [card["step"] for card in rows
                              if card["built"] and
                              all(other["built"] for other in rows
                                  if other["step"] <= card["step"])]),
        "why": "%d steps, %d agent%s, %d not built, %d mapped on a reading "
               "rather than on a row%s."
               % (len(order), len(set(c["agent"] for c in rows if c["agent"])),
                  "" if len(set(c["agent"] for c in rows
                                if c["agent"])) == 1 else "s",
                  len(set(c["agent"] for c in unbuilt)), len(uncertain),
                  "" if not stranger else
                  ". %d the register does not name: %s"
                  % (len(stranger), ", ".join(stranger))),
    }


def run(folder, path=None, register=None):
    """
    {ran, stopped_at, waiting_for} - or a refusal. THE SOURCE FOLDER IS
    NEVER MODIFIED: every agent reached here is READ.
    """
    mapped = plan(path, register)
    if not mapped.get("planned"):
        return mapped

    walked = WALK.walk(folder)
    if not walked.get("walked"):
        return {"ran": False, "refused": walked.get("refused"),
                "why": walked.get("why")}

    briefed = CLS.brief(walked)
    order = dict((card["step"], card) for card in mapped["steps"])

    # WHERE IT STOPS AND WHY, as a fact rather than a failure. Step 3 is
    # the first that needs an answer nothing here can produce.
    stopped = max(UNATTENDED) + 1
    waiting = order.get(stopped, {})
    reason = ("it needs an answer. %s is T2 - the evidence is gathered and "
              "the question goes to the host" % waiting.get("agent"))
    if waiting.get("agent") and not waiting.get("built"):
        reason = ("%s is not built. docs/28 makes it T3, an agentic loop"
                  % waiting["agent"])

    absent = sorted(set(card["agent"] for card in mapped["unbuilt"]))
    return {
        "ran": True,
        "root": walked["root"],
        "modified_the_source": False,
        "walked": walked,
        "briefed": briefed if briefed.get("briefed") else None,
        "of": mapped["of"],
        "done": sorted(UNATTENDED),
        "stopped_at": stopped,
        "waiting_for": {"step": stopped, "does": waiting.get("does"),
                        "agent": waiting.get("agent"), "why": reason},
        "questions": (briefed.get("asks") or []) if briefed.get("briefed")
                     else [],
        "unbuilt": absent,
        "why": "%d of %d steps ran unattended. Stopped at step %d (%s): %s. "
               "%d question%s ready for the host. Nothing was written and "
               "the source folder was not touched."
               % (len(UNATTENDED), mapped["of"], stopped,
                  waiting.get("does"), reason,
                  len((briefed.get("asks") or [])
                      if briefed.get("briefed") else []),
                  "" if len((briefed.get("asks") or [])
                            if briefed.get("briefed") else []) == 1 else "s"),
        "unjudged": [
            "THE ORDER WAS READ FROM docs/00 s28, NOT REMEMBERED. A step "
            "added to the specification appears here with no edit, and one "
            "removed stops being expected. A pipeline carrying a copy of "
            "its own order eventually runs a different one.",
            ("%d STEP%s MAPPED ON A READING RATHER THAN ON AN AGENT'S ROW: "
             "%s. docs/00 s29 lists the agents in ID order and nothing maps "
             "the two lists onto each other, so these are marked rather "
             "than asserted."
             % (len(mapped["uncertain"]),
                "" if len(mapped["uncertain"]) == 1 else "S",
                ", ".join("%d %s" % (card["step"], card["does"])
                          for card in mapped["uncertain"]))
             if mapped["uncertain"] else
             "every step's owner is named in that agent's own register "
             "row."),
            ("THE RUN STOPPED AT STEP %d FOR AN ANSWER, NOT FOR A "
             "MISSING AGENT - %s is built and is T2. The first step with "
             "NO agent behind it is step %d, %s. %s %s not built, and "
             "docs/28 makes each of them T3."
             % (stopped, waiting.get("agent"),
                mapped["unbuilt"][0]["step"], mapped["unbuilt"][0]["does"],
                ", ".join(absent), "is" if len(absent) == 1 else "are")
             if absent else
             "every agent this pipeline needs is built; the run stopped at "
             "step %d only because that step needs an answer." % stopped),
            "STEPS 3, 6 AND 10 ARE ONE QUESTION. `identify code`, `identify "
            "documentation` and `classify content` all land on "
            "HERON-IMP-CLS-003, whose row covers all five categories. "
            "Reported as three steps with one owner rather than collapsed, "
            "because the numbering is what a reader follows.",
            "NOTHING WAS WRITTEN AND THE SOURCE FOLDER WAS NOT TOUCHED. "
            "Every agent reached here is READ - docs/10 s5 constraint 1, "
            "and the register puts it in this agent's own row in bold.",
        ],
    }


def main(argv):
    import shutil
    import tempfile

    print("IMPORT PIPELINE   sixteen steps, and how far they reach today")
    print("=" * 72)

    mapped = plan()
    print("\n%s" % mapped["why"])
    print("\n%-5s %-34s %-22s %s" % ("step", "does", "agent", "state"))
    for card in mapped["steps"]:
        print("  %-3d %-34s %-22s %s"
              % (card["step"], card["does"], card["agent"] or "-",
                 "built" if card["built"] else "NOT BUILT"))
    for card in mapped["uncertain"]:
        print("\n  READING  step %d -> %s" % (card["step"], card["agent"]))
        print("           %s" % card["why"])

    where = argv[0] if argv else None
    made = None
    if not where:
        made = where = tempfile.mkdtemp(prefix="heron-pipeline-")
        os.makedirs(os.path.join(where, "tools"))
        for at, text in (("README.md", "# AJ-Tools"),
                         ("settings.json", '{"revit": "2024"}'),
                         (os.path.join("tools", "CountDucts.py"), "pass"),
                         (os.path.join("tools", "TagSheet.py"), "pass")):
            with io.open(os.path.join(where, at), "w",
                         encoding="utf-8") as handle:
                handle.write(text)
    try:
        answer = run(where)
        print("\n%s" % answer["why"])
        print("\n  ran        steps %s"
              % ", ".join(str(n) for n in answer["done"]))
        print("  stopped    step %d - %s"
              % (answer["stopped_at"], answer["waiting_for"]["does"]))
        print("  waiting    %s" % answer["waiting_for"]["why"])
        print("  not built  %s" % ", ".join(answer["unbuilt"]))
        for ask in answer["questions"]:
            print("  QUESTION   %s" % ask["group"])

        print("\nrefused")
        for bad in (None, os.path.join(ROOT, "no-such-folder")):
            said = run(bad)
            print("  %-20s %s" % (said["refused"], said["why"][:42]))
        said = plan(path=os.path.join(ROOT, "docs", "README.md"))
        print("  %-20s %s" % (said["refused"], said["why"][:42]))

        print("\nwhat this agent does not judge")
        for line in answer["unjudged"]:
            print("  - %s" % line)
    finally:
        if made:
            shutil.rmtree(made, ignore_errors=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
