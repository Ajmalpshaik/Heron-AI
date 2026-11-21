#!/usr/bin/env python3
# Heron-Agent:  HERON-SKL-VAL-004
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
A SKILL's composition proof - judged from two plan runs, drafted, and signed
only by a person. FRAGMENT-ISSUES row 141.

    python brain/heron_skill_proof.py draft <skill> --positive <rec> --negative <rec>
    python brain/heron_skill_proof.py review <skill>
    python brain/heron_skill_proof.py accept <skill> --by "Your Name"

WHY A SKILL NEEDS A PROOF OF ITS OWN
------------------------------------
A skill is a COMPOSITION: its steps, in an order, each handing on to the next.
Every step may be a PROVEN fragment and the skill still be unproved, because
what the fragments' proofs say is that each does its job alone - not that the
first hands the second what it needs, nor that the last answers the skill's
question. That is the claim this file judges, and it is a different claim
from any fragment's, so it is recorded in a file of its own
(`brain/skill-proofs/<skill>.yaml`), the way `brain/agent-proofs/` is apart
from the cards.

WHAT THE TWO RUNS ARE
---------------------
`mcp/client/heron_bridge_client.py plan` runs the plan and writes a run record
per leg; `tools/run-skill.py` drives both. This file needs no Revit.

  POSITIVE   the plan on a model where the skill's answer is something
  NEGATIVE   the same plan where the answer must be nothing (D-30)

WHAT IS JUDGED, IN ORDER - the first that fails is the verdict
---------------------------------------------------------------
  1. THE SAME PLAN in both legs, and the plan of THIS skill: the same steps
     in the same order, and every step one of the skill's own capabilities,
     or a setup step the job marked `setup`.
  2. EVERY STEP OF THE POSITIVE RAN. A step that failed stopped the plan
     (row 148), and a plan that did not run proves nothing.
  3. EACH STEP TOOK WHAT ITS PRODUCER LEFT. A step that named the step before
     it (`expect`) and still bound its inputs from the SELECTION answered from
     something else; the add-in refuses a chain left by another run, and this
     reads the binding note for the rest.
  4. THE POSITIVE'S LAST STEP ANSWERED - asked of that fragment's declared
     results exactly as `heron_validate.positive_worked` asks a fragment's.
  5. THE NEGATIVE RAN TO ITS LAST STEP, and was asked whether it ran BEFORE
     whether it is empty (row 5b-1884).
  6. THE NEGATIVE'S LAST STEP IS EMPTY - `heron_validate.looks_empty`.

A capability the skill declares that the run never exercised is a GAP in the
draft, not a refusal: a skill can carry a step that applies only to some
models ("ONLY when the model has it"), and the person signing decides whether
the run covers the claim. The draft says which.

THE ONE RULE, as `heron_validate`'s: nothing here writes a skill's
`heron-status`, and nothing here signs. `accept` takes a person's name, typed
at the moment, refuses a draft that is not PASS or whose skill or fragments
have changed since the run, and writes the proof file. Promoting the skill is
the owner's decision and not this file's.
"""

import argparse
import datetime
import hashlib
import io
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
sys.path.insert(0, HERE)

import yaml                                                    # noqa: E402

import heron_skill as SKILL                                    # noqa: E402
import heron_validate as HV                                    # noqa: E402

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")
except AttributeError:
    pass

DRAFTS_DIR = os.path.join(ROOT, "brain", "proof-drafts")
RUNS_DIR = os.path.join(DRAFTS_DIR, "runs")
PROOFS_DIR = os.path.join(ROOT, "brain", "skill-proofs")

PASS = "PASS"
NOT_THE_PLAN = "NOT THE PLAN"
DID_NOT_RUN = "DID NOT RUN"
BOUND_ELSEWHERE = "BOUND ELSEWHERE"
POSITIVE_EMPTY = "POSITIVE EMPTY"
NEGATIVE_DID_NOT_RUN = "NEGATIVE DID NOT RUN"
NEG_NOT_EMPTY = "NEGATIVE NOT EMPTY"

# The binding note's words for a step that took the selection, not a chain.
FROM_SELECTION = "from the selection"


def run_path(skill_id, phase):
    """Where a leg's run record lives - gitignored with every other run record."""
    return os.path.join(RUNS_DIR, "skill-%s.%s.json" % (skill_id, phase))


def draft_path(skill_id):
    return os.path.join(DRAFTS_DIR, "skill-%s.yaml" % skill_id)


def proof_path(skill_id):
    return os.path.join(PROOFS_DIR, "%s.yaml" % skill_id)


def _phase(step, phase):
    """A plan step in the shape heron_validate judges a fragment's phase in."""
    return {"phase": phase, "ok": bool(step.get("ok")),
            "error": step.get("error"), "message": step.get("message"),
            "provides": step.get("provides") or {}}


def fingerprint(skill, frags):
    """The skill's own file and every step's fragment, as one hash.

    A proof is of THIS composition: the skill as written and the C# each step
    ran. Any of them changing makes the proof stale, as a fragment's own
    fingerprint does for its proof."""
    digest = hashlib.sha256()
    with io.open(skill.path, "rb") as fh:
        digest.update(fh.read())
    for frag in frags:
        digest.update((frag.slug + ":" + frag.fingerprint()).encode("utf-8"))
    return digest.hexdigest()[:16]


def _steps(record):
    return [s for s in (record or {}).get("steps") or [] if isinstance(s, dict)]


def judge(skill, positive, negative, library):
    """(verdict, why, gaps, frags) for one skill from its two run records."""
    by_slug = dict((f.slug, f) for f in library)
    pos, neg = _steps(positive), _steps(negative)
    gaps = []

    # 1. The same plan, and this skill's.
    names = [s.get("name") for s in pos]
    if not pos:
        return NOT_THE_PLAN, "the positive record has no steps", gaps, []
    if negative is not None and [s.get("name") for s in neg] != names:
        return (NOT_THE_PLAN, "the negative ran %s and the positive %s - one plan, "
                "both legs" % ([s.get("name") for s in neg], names), gaps, [])
    frags = []
    needs = set(skill.needs())
    for step in pos:
        frag = by_slug.get(step.get("name"))
        if frag is None:
            return NOT_THE_PLAN, "step %s names no fragment" % step.get("name"), gaps, []
        frags.append(frag)
        if not step.get("setup") and frag.data.get("capability") not in needs:
            return (NOT_THE_PLAN, "step %d, %s, is %s, which %s does not declare - "
                    "mark a step that only arranges the model `setup: true`"
                    % (step.get("step") or 0, frag.slug, frag.data.get("capability"),
                       skill.id), gaps, frags)
    exercised = set(f.data.get("capability") for s, f in zip(pos, frags)
                    if not s.get("setup"))
    for capability in sorted(needs - exercised):
        gaps.append("%s is declared by the skill and this run never exercised it - "
                    "say why before signing" % capability)

    # 2. Every positive step ran.
    for step in pos:
        if not step.get("ok"):
            if step.get("notRun"):
                why = "step %d, %s, was not run: %s" % (step.get("step") or 0,
                                                        step.get("name"), step.get("message"))
            else:
                why = "step %d, %s - %s" % (step.get("step") or 0, step.get("name"),
                                            HV.phase_failure(_phase(step, "positive")))
            return DID_NOT_RUN, why, gaps, frags

    # 3. Each step took what its producer left.
    for step in pos[1:]:
        bound = (step.get("bound") or "").lower()
        if step.get("expect") and FROM_SELECTION in bound:
            return (BOUND_ELSEWHERE, "step %d, %s, expected %s and bound %s"
                    % (step.get("step") or 0, step.get("name"), step.get("expect"),
                       step.get("bound")), gaps, frags)
        if not step.get("expect") and not step.get("setup"):
            gaps.append("step %d, %s, named no producer, so the add-in did not check "
                        "where its inputs came from - its binding note: %s"
                        % (step.get("step") or 0, step.get("name"),
                           step.get("bound") or "none"))

    # 4. The positive's answer.
    last, frag = pos[-1], frags[-1]
    positive_phase = _phase(last, "positive")
    reason, detail = HV.positive_worked(positive_phase, frag)
    negative_phase = _phase(neg[-1], "negative") if neg else None
    if reason and HV._flag_flipped(positive_phase, negative_phase, frag):
        reason = None
    if (reason == "POSITIVE UNREADABLE" and negative_phase is not None
            and negative_phase.get("ok")
            and HV.things_made(positive_phase, negative_phase, frag)):
        reason = None
    if reason:
        return POSITIVE_EMPTY, "%s - %s" % (reason, detail), gaps, frags

    # 5. The negative ran - asked before whether it is empty (row 5b-1884).
    if negative is None:
        return NEGATIVE_DID_NOT_RUN, "there is no negative run record", gaps, frags
    for step in neg:
        if not step.get("ok"):
            if step.get("notRun"):
                why = "step %d, %s, was not run: %s" % (step.get("step") or 0,
                                                        step.get("name"), step.get("message"))
            else:
                why = "step %d, %s - %s" % (step.get("step") or 0, step.get("name"),
                                            HV.phase_failure(_phase(step, "negative")))
            return (NEGATIVE_DID_NOT_RUN, "the negative did not run to its last step: %s"
                    % why, gaps, frags)

    # 6. The negative's answer is nothing.
    roles, names_ = HV.contract_of(frag)
    if not HV.looks_empty(negative_phase, roles, names_, HV.string_results(frag)):
        said = negative_phase.get("provides") or {}
        return (NEG_NOT_EMPTY, "the negative's last step, %s, came back with content "
                "(%s) - the leg that must find nothing found something"
                % (frag.slug, ", ".join("%s %s" % (k, said[k]) for k in sorted(said))),
                gaps, frags)

    return PASS, "every step ran, each took what its producer left, and the answer " \
                 "was there in the positive and nothing in the negative", gaps, frags


def _model(record):
    steps = _steps(record)
    named = (steps[-1].get("document") if steps else None) or (record or {}).get("document") \
        or (record or {}).get("activeDocument") or "(no model named)"
    return "%s, Revit %s, session %s" % (named, (record or {}).get("revit") or "?",
                                         (record or {}).get("session") or "?")


def _case(record):
    lines = []
    for step in _steps(record):
        provides = step.get("provides") or {}
        said = ", ".join("%s %s" % (k, provides[k]) for k in sorted(provides))
        lines.append("%d. %s - %s%s%s" % (
            step.get("step") or 0, step.get("name"),
            "ok" if step.get("ok") else (step.get("error") or "not run"),
            (", " + said) if said else "",
            (" <- %s" % step.get("bound")) if step.get("bound") else ""))
    return lines


def draft(skill, positive, negative, library):
    """The draft a person reads before signing. Never signs."""
    verdict, why, gaps, frags = judge(skill, positive, negative, library)
    return {
        "skill": skill.id,
        "verdict": verdict,
        "why": why,
        "proof-draft": {
            "date": (positive or {}).get("date") or datetime.date.today().isoformat(),
            "by": "",
            "model": _model(positive),
            "negative_model": _model(negative) if negative else HV.NOT_ESTABLISHED,
            "positive_case": _case(positive),
            "negative_case": _case(negative) if negative else [HV.NOT_ESTABLISHED],
            "steps": [f.slug for f in frags],
            "fingerprint": fingerprint(skill, frags) if frags else "",
        },
        "gaps": gaps,
    }


def write_draft(skill_id, body):
    if not os.path.isdir(DRAFTS_DIR):
        os.makedirs(DRAFTS_DIR)
    path = draft_path(skill_id)
    with io.open(path, "w", encoding="utf-8") as fh:
        fh.write(yaml.safe_dump(body, allow_unicode=True, sort_keys=False,
                                default_flow_style=False, width=88))
    return path


def read_draft(skill_id):
    path = draft_path(skill_id)
    if not os.path.isfile(path):
        return None
    with io.open(path, "r", encoding="utf-8") as fh:
        return yaml.safe_load(fh.read())


def accept(skill_id, by, skills=None, library=None):
    """A PERSON signs a skill's composition proof. (code, message).

    Refuses with no name, with no draft, with a draft that is not PASS, and
    with one whose skill or step fragments changed since it was drafted. Writes
    brain/skill-proofs/<skill>.yaml and never touches the skill's status."""
    if not by or not by.strip():
        return 2, "accept needs a person's name: --by \"Your Name\""
    skills = skills if skills is not None else SKILL.load_all()[0]
    skill = skills.get(skill_id)
    if skill is None:
        return 2, "no skill called '%s'" % skill_id
    body = read_draft(skill_id)
    if body is None:
        return 2, "no draft for '%s' - run tools/run-skill.py first" % skill_id
    if body.get("verdict") != PASS:
        return 1, ("the draft says %s - %s. Only a PASS is signed; arrange the run "
                   "again." % (body.get("verdict"), body.get("why")))
    library = library if library is not None else HV.load_library()
    by_slug = dict((f.slug, f) for f in library)
    proof = dict(body.get("proof-draft") or {})
    frags = [by_slug.get(s) for s in proof.get("steps") or []]
    if not frags or None in frags:
        return 1, "a step the draft names is no longer in the library"
    now = fingerprint(skill, frags)
    if now != proof.get("fingerprint"):
        return 1, ("the skill or one of its steps has changed since this was run "
                   "(fingerprint %s, now %s) - run it again" % (proof.get("fingerprint"), now))
    proof["by"] = by.strip()
    record = {"heron-agent": "HERON-SKL-VAL-004", "heron-step": "17",
              "heron-status": "DRAFT", "heron-since": "0.1.0", "heron-layer": "brain",
              "skill": skill_id}
    record.update(proof)
    record["gaps"] = body.get("gaps") or []
    if not os.path.isdir(PROOFS_DIR):
        os.makedirs(PROOFS_DIR)
    # THE SKILL'S OWN FILE IS READ BEFORE AND AFTER, and must not move. It
    # should never fire - nothing here writes it - and it exists because
    # "nothing here writes it" is the sentence that stops being true when
    # somebody adds a feature. It read the skill object already in memory
    # until a test pointed the proof at the skill's own file and it said
    # "signed".
    status_before = skill.status
    with io.open(skill.path, "rb") as fh:
        skill_before = fh.read()
    with io.open(proof_path(skill_id), "w", encoding="utf-8") as fh:
        fh.write(yaml.safe_dump(record, allow_unicode=True, sort_keys=False,
                                default_flow_style=False, width=88))
    with io.open(skill.path, "rb") as fh:
        if fh.read() != skill_before:
            return 1, ("the skill's own file - its status with it - moved while signing, "
                       "and this file never writes it: read %s before trusting it"
                       % skill.path)
    return 0, ("signed by %s: %s. The skill's heron-status is unchanged (%s) - "
               "promoting it is the owner's decision." % (by.strip(), proof_path(skill_id),
                                                           status_before))


def _load_record(path):
    if not path:
        return None
    with io.open(path, "r", encoding="utf-8") as fh:
        return json.load(fh)


def main(argv=None):
    parser = argparse.ArgumentParser(description="A skill's composition proof.")
    sub = parser.add_subparsers(dest="command")
    d = sub.add_parser("draft", help="judge two plan runs and write the draft")
    d.add_argument("skill")
    d.add_argument("--positive", required=True)
    d.add_argument("--negative")
    r = sub.add_parser("review", help="print a skill's draft")
    r.add_argument("skill")
    a = sub.add_parser("accept", help="a person signs a PASS draft")
    a.add_argument("skill")
    a.add_argument("--by", required=True)
    args = parser.parse_args(argv)

    if args.command == "draft":
        skill = SKILL.load_all()[0].get(args.skill)
        if skill is None:
            print("no skill called '%s'" % args.skill)
            return 2
        try:
            positive = _load_record(args.positive)
            negative = _load_record(args.negative)
        except (OSError, ValueError) as exc:
            print("a run record cannot be read: %s" % exc)
            return 2
        body = draft(skill, positive, negative, HV.load_library())
        path = write_draft(skill.id, body)
        print("%s  %s - %s" % (body["verdict"], skill.id, body["why"]))
        for gap in body["gaps"]:
            print("  gap: %s" % gap)
        print("draft: %s" % path)
        print("Nothing was signed. A person reads it and runs accept.")
        return 0 if body["verdict"] == PASS else 1
    if args.command == "review":
        body = read_draft(args.skill)
        if body is None:
            print("no draft for '%s'" % args.skill)
            return 2
        sys.stdout.write(yaml.safe_dump(body, allow_unicode=True, sort_keys=False))
        return 0
    if args.command == "accept":
        code, message = accept(args.skill, args.by)
        print(message)
        return code
    parser.print_help()
    return 2


if __name__ == "__main__":
    sys.exit(main())
