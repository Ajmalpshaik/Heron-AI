# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-SKL-VAL-004
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Run a SKILL's plan on a real model, both legs, and draft its composition
proof. FRAGMENT-ISSUES row 141.

    python tools/run-skill.py <job.yaml> --dry-run     check the job; send nothing
    python tools/run-skill.py <job.yaml>               run both legs, then draft
    python tools/run-skill.py <job.yaml> --session 63712

WHY THIS EXISTS
---------------
No skill had ever been proved, and row 141 named why: three runners each did a
third of the job. `batch-prove.py` judges and writes, one fragment at a time,
and refuses a PROVEN one - most of a skill's steps. `prove` runs a chain but
only reads, and judges nothing. `validate` judges one fragment. A skill is the
composition, so this runs the plan WHOLE - chain, write and judgement:

  1. `heron_bridge_client.py plan` runs every step in order on one lease and
     one chain, the positive leg and then the negative, each into a run
     record. A change is rolled back, always: a plan run keeps nothing.
  2. `brain/heron_skill_proof.py` judges the two records and writes a draft.
  3. A person reads the draft and signs it with
     `python brain/heron_skill_proof.py accept <skill> --by "Name"`.
     Nothing here signs, and nothing here writes a skill's status.

THE JOB FILE
------------
    skill: count-elements
    model: "the model these values were arranged for"   # recorded, never enforced
    document: "..."             # optional: run on this open model, not the one in front
    negative_document: "..."    # optional: the negative on another open model
    steps:
      - name: select-by-category-name      # a step that only ARRANGES the model
        setup: true
        values: {categoryName: Ducts}
      - name: filter-elements-by-category
        values: {category: Ducts, levelId: "Level 1"}
        negative_values: {levelId: "Level 9"}    # merged over values, negative leg only
      - name: count-elements
        expect: previous                    # take the step before's values, or refuse

`expect: previous` sends the step's `expectChain`, so the add-in refuses a
chain some other run left (docs/36) - row 148's stale selection, closed by the
add-in rather than by reading a note afterwards. A value still reading
FILL IN refuses the job before anything is sent.

EXIT CODES: 0 the draft is PASS; 1 it is not, or a leg failed; 2 the job was
refused, or this machine cannot run it (no Revit - a cloud container says so).
"""

import argparse
import io
import json
import os
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
sys.path.insert(0, os.path.join(ROOT, "brain"))

import yaml                                                    # noqa: E402

import heron_skill as SKILL                                    # noqa: E402
import heron_skill_proof as PROOF                              # noqa: E402
import heron_validate as HV                                    # noqa: E402

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")
except AttributeError:
    pass

CLIENT = os.path.join(ROOT, "mcp", "client", "heron_bridge_client.py")
FILL_IN = "FILL IN"
# What a plan run may carry - a change is rolled back; a file is not unwritten.
PLAN_RISKS = ("READ", "ANALYZE", "SUGGEST", "EXECUTE", "MODIFY")


def load_job(path):
    with io.open(path, "r", encoding="utf-8") as fh:
        return yaml.safe_load(fh.read()) or {}


def check_job(job, skills, library):
    """[problems] - a job that would be refused, said before anything is sent."""
    problems = []
    skill = skills.get(job.get("skill"))
    if skill is None:
        return ["the job names no skill Heron has (%r)" % job.get("skill")]
    steps = job.get("steps")
    if not isinstance(steps, list) or not steps:
        return ["the job has no steps"]
    by_slug = dict((f.slug, f) for f in library)
    needs = set(skill.needs())
    for number, step in enumerate(steps, 1):
        if not isinstance(step, dict):
            problems.append("step %d is not a step" % number)
            continue
        frag = by_slug.get(step.get("name"))
        if frag is None:
            problems.append("step %d names no fragment (%r)" % (number, step.get("name")))
            continue
        risk = (frag.data.get("risk") or "").upper()
        if risk not in PLAN_RISKS:
            problems.append("step %d, %s, is %s - a plan run keeps nothing, and rolling "
                            "back does not unwrite a file" % (number, frag.slug, risk or "?"))
        if not step.get("setup") and frag.data.get("capability") not in needs:
            problems.append("step %d, %s, is %s, which %s does not declare - mark a "
                            "step that only arranges the model `setup: true`"
                            % (number, frag.slug, frag.data.get("capability"), skill.id))
        if step.get("expect") and number == 1:
            problems.append("step 1 cannot expect a step before it")
        if step.get("expect") not in (None, "previous"):
            problems.append("step %d: expect is `previous` or left out" % number)
        for key in ("values", "negative_values"):
            given = step.get(key) or {}
            if not isinstance(given, dict):
                problems.append("step %d: %s is not name: value pairs" % (number, key))
                continue
            for name, value in given.items():
                if FILL_IN in str(value):
                    problems.append("step %d, %s: %s %s still reads %s"
                                    % (number, frag.slug, key, name, FILL_IN))
    return problems


def plan_for(job, phase):
    """The plan file the client reads, for one leg."""
    steps = []
    for step in job.get("steps") or []:
        values = dict((str(k), str(v)) for k, v in (step.get("values") or {}).items())
        if phase == "negative":
            values.update(dict((str(k), str(v))
                               for k, v in (step.get("negative_values") or {}).items()))
        row = {"name": step.get("name"), "values": values}
        if step.get("expect"):
            row["expect"] = step.get("expect")
        if step.get("setup"):
            row["setup"] = True
        steps.append(row)
    document = job.get("negative_document") if phase == "negative" else None
    return {"skill": job.get("skill"), "phase": phase,
            "document": document or job.get("document"), "steps": steps}


def client_line(plan_file, record, session=None):
    line = [sys.executable, CLIENT, "plan", plan_file, "--out", record]
    if session:
        line += ["--session", str(session)]
    return line


def mark_setup(record, plan):
    """The client records what ran; which steps only arranged is the job's word."""
    if record is None:
        return None
    for row, step in zip(record.get("steps") or [], plan["steps"]):
        if step.get("setup"):
            row["setup"] = True
    return record


def run_leg(job, phase, session, runner=subprocess.call):
    """(exit code, record or None). The run record is removed first, so a
    record on disk afterwards is THIS run's (batch-prove's rule, row 87)."""
    skill_id = job.get("skill")
    record = PROOF.run_path(skill_id, phase)
    plan_file = record.replace(".json", ".plan.json")
    if not os.path.isdir(os.path.dirname(record)):
        os.makedirs(os.path.dirname(record))
    if os.path.isfile(record):
        os.remove(record)
    plan = plan_for(job, phase)
    with io.open(plan_file, "w", encoding="utf-8") as fh:
        fh.write(json.dumps(plan, indent=1, ensure_ascii=False))
    code = runner(client_line(plan_file, record, session))
    if not os.path.isfile(record):
        return code, None
    with io.open(record, "r", encoding="utf-8") as fh:
        return code, mark_setup(json.load(fh), plan)


def main(argv=None, runner=subprocess.call):
    parser = argparse.ArgumentParser(description="Run a skill's plan and draft its proof.")
    parser.add_argument("job")
    parser.add_argument("--dry-run", action="store_true", help="check the job; send nothing")
    parser.add_argument("--session", help="which Revit, by process id")
    args = parser.parse_args(argv)

    try:
        job = load_job(args.job)
    except (OSError, yaml.YAMLError) as exc:
        print("the job %s cannot be read: %s" % (args.job, exc))
        return 2
    skills = SKILL.load_all()[0]
    library = HV.load_library()
    problems = check_job(job, skills, library)
    if problems:
        print("REFUSED - nothing was sent to Revit:")
        for problem in problems:
            print("  - %s" % problem)
        return 2

    skill = skills[job["skill"]]
    print("%s (%s) - %d step(s); model: %s"
          % (skill.id, skill.data.get("risk"), len(job["steps"]),
             job.get("model") or "(not recorded)"))
    for phase in ("positive", "negative"):
        plan = plan_for(job, phase)
        print("  %s: %s" % (phase, " -> ".join(
            "%s%s" % (s["name"], " (setup)" if s.get("setup") else "") for s in plan["steps"])))
    if args.dry_run:
        print("DRY RUN - the job reads right and nothing was sent. Each leg runs as:")
        for phase in ("positive", "negative"):
            record = PROOF.run_path(skill.id, phase)
            print("  " + " ".join(client_line(record.replace(".json", ".plan.json"),
                                              record, args.session)))
        return 0

    code, positive = run_leg(job, "positive", args.session, runner)
    if positive is None:
        print("The positive leg left no run record (exit %s) - nothing ran, so there is "
              "nothing to judge. On a machine with no Revit that is expected." % code)
        return 2
    negative = None
    if all(s.get("ok") for s in positive.get("steps") or []):
        _code, negative = run_leg(job, "negative", args.session, runner)
    else:
        print("The positive leg stopped, so the negative was not run.")
    body = PROOF.draft(skill, positive, negative, library)
    path = PROOF.write_draft(skill.id, body)
    print("")
    print("%s  %s - %s" % (body["verdict"], skill.id, body["why"]))
    for gap in body["gaps"]:
        print("  gap: %s" % gap)
    print("draft: %s" % path)
    print("Nothing was signed. Read the draft; then a person signs it:")
    print('  python brain/heron_skill_proof.py accept %s --by "Your Name"' % skill.id)
    return 0 if body["verdict"] == PROOF.PASS else 1


if __name__ == "__main__":
    sys.exit(main())
