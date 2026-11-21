#!/usr/bin/env python3
# Heron-Agent:  HERON-SKL-VAL-004
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A skill's plan is run whole, judged, and signed only by a person - row 141.

    python tests/test_skill_runner.py

WHAT THIS HOLDS, WITHOUT REVIT
------------------------------
  1. THE CLIENT RUNS THE PLAN (`heron_bridge_client.py plan`), through a fake
     Revit: every step in order on one lease; the first resets the chain; a
     step with `expect: previous` sends the step before it as `expectChain`;
     a READ step goes as `run_fragment_read` and a MODIFY step as
     `run_fragment_write` with NO `apply`, so it is rolled back; a PUBLISH
     step refuses the plan before Revit is asked anything; a failed step
     stops the plan and the rest are recorded notRun; the run record is
     written where `--out` says.
  2. THE JUDGE (`brain/heron_plan_proof.py`) - PASS on the count-elements
     plan as it ran on 2026-09-19 (8 ducts, then a level with none), and each
     refusal on the record that earns it: a step the skill does not declare,
     a positive step that failed, a step that expected its producer and bound
     the selection, an empty answer, a negative that did not run - said as
     "did not run", never "came back with content" - and a negative that
     found something. A capability never exercised is a gap, not a refusal.
  3. ACCEPT - refuses no name, a draft that is not PASS, and a skill or step
     that changed since the run; signs a PASS into brain/skill-proofs/ and
     leaves the skill's own file, and its status, untouched.
  4. THE DRIVER (`tools/run-skill.py`) - refuses a job with FILL IN, a step
     the skill does not declare and a PUBLISH step, sending nothing; a dry
     run sends nothing; a run with a fake client drafts PASS; a positive that
     stopped runs no negative.

WHAT IT CANNOT DO
-----------------
Run a plan in Revit. Whether the add-in keeps a rolled-back step's values in
the chain, and whether count-elements answers 0 from a filter that found
nothing, are NEEDS-CHECKING Group PH.
"""

import contextlib
import importlib.util
import io
import json
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
_SCRATCH = tempfile.mkdtemp(prefix="heron-skill-runner-")
os.environ["HERON_CONFIG"] = os.path.join(_SCRATCH, "heron.config")

sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))
sys.path.insert(0, os.path.join(ROOT, "brain"))

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except AttributeError:
    pass

import heron_bridge_client as CLIENT                          # noqa: E402
import heron_skill as SKILL                                   # noqa: E402
import heron_validate as HV                                   # noqa: E402

PROOF = None
try:
    import heron_plan_proof as PROOF                         # noqa: E402
except ImportError as exc:
    print("brain/heron_plan_proof.py cannot be imported: %s" % exc)

_spec = importlib.util.spec_from_file_location(
    "run_skill", os.path.join(ROOT, "tools", "run-skill.py"))
RUN = None
if os.path.isfile(os.path.join(ROOT, "tools", "run-skill.py")):
    RUN = importlib.util.module_from_spec(_spec)
    _spec.loader.exec_module(RUN)

FAILURES = []

FILTER = "filter-elements-by-category"
COUNT = "count-elements"
MODIFY_CARD = "add-beam-annotations"
PUBLISH_CARD = "export-parameters-to-csv"


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


# ---------------------------------------------------------------------------

class FakeBridge(object):
    revit_version = "2024"
    pid = 4242

    def __init__(self, answers):
        self.answers = answers
        self.sent = []

    def request(self, op, op_args=None, **kwargs):
        if op == "count_elements":
            return {"ok": True, "document": "Fake model", "count": 3}
        self.sent.append((op, dict(op_args or {})))
        return self.answers.get((op_args or {}).get("name"),
                                {"ok": True, "document": "Fake model", "provides": {}})

    def release(self):
        pass

    def close(self):
        pass


@contextlib.contextmanager
def fake_revit(answers):
    bridge = FakeBridge(answers)
    asked = {"discover": 0}

    def discover(prune=True):
        asked["discover"] += 1
        return [bridge], [], [], []

    saved = (CLIENT.refuse_off_windows, CLIENT.discover)
    CLIENT.refuse_off_windows = lambda: False
    CLIENT.discover = discover
    out = io.StringIO()
    try:
        with contextlib.redirect_stdout(out):
            yield bridge, asked, out
    finally:
        CLIENT.refuse_off_windows, CLIENT.discover = saved


def plan_file(steps, phase="positive"):
    path = os.path.join(_SCRATCH, "plan-%s-%d.json" % (phase, len(os.listdir(_SCRATCH))))
    with io.open(path, "w", encoding="utf-8") as fh:
        fh.write(json.dumps({"skill": "count-elements", "phase": phase, "steps": steps}))
    return path


GOOD = {FILTER: {"ok": True, "document": "Fake model",
                 "provides": {"elements": "8 item(s) [1, 2, 3, 4, 5, 6, 7, 8]"}},
        COUNT: {"ok": True, "document": "Fake model",
                "provides": {"count": "8", "countedNothing": "false"},
                "bound": "elements from filter-elements-by-category (8)"}}


def section_1():
    print("1. the client runs the plan")
    cmd_plan = getattr(CLIENT, "cmd_plan", None)
    check(cmd_plan is not None, "heron_bridge_client has a plan command (cmd_plan)")
    if cmd_plan is None:
        return
    out_path = os.path.join(_SCRATCH, "record.json")
    steps = [{"name": FILTER, "values": {"category": "Ducts"}},
             {"name": COUNT, "expect": "previous"}]
    with fake_revit(GOOD) as (bridge, asked, out):
        code = cmd_plan(plan_file(steps), out=out_path)
    ops = [op for op, _ in bridge.sent]
    check(code == 0 and ops == ["run_fragment_read", "run_fragment_read"],
          "two READ steps, each as run_fragment_read, exit 0 (%r, %r)" % (code, ops))
    first, second = bridge.sent[0][1], bridge.sent[1][1]
    check(first.get("chain") == "reset" and "expectChain" not in first,
          "the first step resets the chain")
    check(second.get("expectChain") == FILTER and "chain" not in second,
          "a step that expects the previous one sends it as expectChain")
    check(first.get("values") == [{"name": "category", "value": "Ducts"}],
          "a step's values go with it, as the {name, value} pairs caller_values makes")
    record = json.load(io.open(out_path, encoding="utf-8")) if os.path.isfile(out_path) else {}
    check([s.get("name") for s in record.get("steps", [])] == [FILTER, COUNT]
          and record["steps"][1].get("provides", {}).get("count") == "8"
          and record["steps"][1].get("bound", "").endswith("(8)"),
          "the run record holds each step, what it provided and how it bound")

    with fake_revit({}) as (bridge, asked, out):
        code = cmd_plan(plan_file([{"name": FILTER}, {"name": MODIFY_CARD}]))
    sent = dict((args.get("name"), (op, args)) for op, args in bridge.sent)
    op, args = sent.get(MODIFY_CARD, (None, {}))
    check(op == "run_fragment_write" and "apply" not in args,
          "a MODIFY step goes as run_fragment_write with no apply - rolled back (%r)" % op)

    with fake_revit({}) as (bridge, asked, out):
        code = cmd_plan(plan_file([{"name": FILTER}, {"name": PUBLISH_CARD}]))
    check(code == 2 and asked["discover"] == 0 and not bridge.sent,
          "a PUBLISH step refuses the whole plan before Revit is asked (exit %r)" % code)

    refused = {FILTER: {"ok": False, "error": "needs_request_values",
                        "message": "category was not given"}}
    out_path = os.path.join(_SCRATCH, "record-stopped.json")
    with fake_revit(refused) as (bridge, asked, out):
        code = cmd_plan(plan_file(steps), out=out_path)
    record = json.load(io.open(out_path, encoding="utf-8")) if os.path.isfile(out_path) else {}
    rows = record.get("steps", [])
    check(code == 1 and [a.get("name") for _, a in bridge.sent] == [FILTER],
          "a failed step stops the plan - nothing after it is sent (exit %r)" % code)
    check(len(rows) == 2 and rows[1].get("notRun") and FILTER in (rows[1].get("message") or ""),
          "and the step after it is recorded notRun, naming the step that stopped it")


# ---------------------------------------------------------------------------

def record(steps, phase="positive", document="Fake model"):
    return {"skill": "count-elements", "phase": phase, "date": "2026-11-21",
            "revit": "2024", "session": 4242, "activeDocument": document, "steps": steps}


def step(n, name, ok=True, provides=None, bound=None, expect=None, **extra):
    row = {"step": n, "name": name, "ok": ok, "provides": provides or {},
           "bound": bound, "expect": expect, "document": "Fake model"}
    row.update(extra)
    return row


def pos_steps(count="8", bound="elements from filter-elements-by-category (8)"):
    return [step(1, FILTER, provides={"elements": "8 item(s) [1, 2]"}),
            step(2, COUNT, provides={"count": count, "countedNothing": "false"},
                 bound=bound, expect=FILTER)]


def neg_steps(count="0", ok=True):
    second = (step(2, COUNT, provides={"count": count, "countedNothing": "true"},
                   bound="elements from filter-elements-by-category (0)", expect=FILTER)
              if ok else step(2, COUNT, ok=False, error="chain_mismatch",
                              message="the chain was left by another run", expect=FILTER))
    return [step(1, FILTER, provides={"elements": "0 item(s)"}), second]


def section_2(skill, library):
    print("2. the judge")
    if PROOF is None:
        check(False, "brain/heron_plan_proof.py exists to judge a plan")
        return
    verdict, why, gaps, _ = PROOF.judge(skill, record(pos_steps()),
                                        record(neg_steps(), "negative"), library)
    check(verdict == PROOF.PASS, "the count-elements plan as it ran - 8, then 0 - is PASS (%s)" % why)
    check(not gaps, "both capabilities exercised, every step named its producer - no gaps %r" % gaps)

    wrong = [step(1, FILTER, provides={"elements": "8 item(s)"}),
             step(2, MODIFY_CARD, provides={"tagged": "3"}, expect=FILTER)]
    verdict, why, _, _ = PROOF.judge(skill, record(wrong), record(wrong, "negative"), library)
    check(verdict == PROOF.NOT_THE_PLAN, "a step the skill does not declare is NOT THE PLAN (%s)" % why)

    failed = [step(1, FILTER, ok=False, error="needs_request_values", message="no category"),
              step(2, COUNT, ok=False, notRun=True, message="filter failed before it")]
    verdict, why, _, _ = PROOF.judge(skill, record(failed), record(neg_steps(), "negative"), library)
    check(verdict == PROOF.DID_NOT_RUN and "needs_request_values" in why,
          "a positive step that failed is DID NOT RUN, in its own error (%s)" % why)

    verdict, why, _, _ = PROOF.judge(
        skill, record(pos_steps(count="1", bound="elements from the selection (1)")),
        record(neg_steps(), "negative"), library)
    check(verdict == PROOF.BOUND_ELSEWHERE,
          "row 148's stale selection - expected its producer, bound the selection (%s)" % why)

    verdict, why, _, _ = PROOF.judge(skill, record(pos_steps(count="0")),
                                     record(neg_steps(), "negative"), library)
    check(verdict == PROOF.POSITIVE_EMPTY, "a positive answering 0 is POSITIVE EMPTY (%s)" % why)

    verdict, why, _, _ = PROOF.judge(skill, record(pos_steps()),
                                     record(neg_steps(ok=False), "negative"), library)
    check(verdict == PROOF.NEGATIVE_DID_NOT_RUN and "chain_mismatch" in why
          and "came back with content" not in why,
          "a negative that did not run says so, never 'came back with content' (%s)" % why)

    verdict, why, _, _ = PROOF.judge(skill, record(pos_steps()),
                                     record(neg_steps(count="5"), "negative"), library)
    check(verdict == PROOF.NEG_NOT_EMPTY, "a negative answering 5 is NEGATIVE NOT EMPTY (%s)" % why)

    one = [step(1, COUNT, provides={"count": "8"}, bound="elements from the selection (8)")]
    verdict, why, gaps, _ = PROOF.judge(
        skill, record(one), record([step(1, COUNT, provides={"count": "0"})], "negative"), library)
    check(verdict == PROOF.PASS and any("FILTER_ELEMENTS_BY_CATEGORY" in g for g in gaps),
          "a declared capability never exercised is a gap for the person signing, "
          "not a refusal (%s; %s)" % (verdict, gaps))


def section_3(skill, library):
    print("3. accept - a person signs, and only a PASS")
    if PROOF is None:
        check(False, "brain/heron_plan_proof.py exists to sign a proof")
        return
    work = tempfile.mkdtemp(prefix="heron-skill-proofs-")
    saved = (PROOF.DRAFTS_DIR, PROOF.RUNS_DIR, PROOF.PROOFS_DIR)
    PROOF.DRAFTS_DIR = os.path.join(work, "drafts")
    PROOF.RUNS_DIR = os.path.join(work, "drafts", "runs")
    PROOF.PROOFS_DIR = os.path.join(work, "skill-proofs")
    before = io.open(skill.path, "rb").read()
    try:
        skills = {skill.id: skill}
        body = PROOF.draft(skill, record(pos_steps()), record(neg_steps(), "negative"), library)
        PROOF.write_draft(skill.id, body)
        check(body["proof-draft"]["by"] == "", "the draft carries no signature")
        code, message = PROOF.accept(skill.id, "  ", skills=skills, library=library)
        check(code == 2 and not os.path.isdir(PROOF.PROOFS_DIR),
              "no name, no signature: %s" % message)

        bad = PROOF.draft(skill, record(pos_steps(count="0")), record(neg_steps(), "negative"),
                          library)
        PROOF.write_draft(skill.id, bad)
        code, message = PROOF.accept(skill.id, "A Person", skills=skills, library=library)
        check(code == 1 and not os.path.isdir(PROOF.PROOFS_DIR),
              "a draft that is not PASS is not signed: %s" % message)

        stale = dict(body, **{"proof-draft": dict(body["proof-draft"], fingerprint="0" * 16)})
        PROOF.write_draft(skill.id, stale)
        code, message = PROOF.accept(skill.id, "A Person", skills=skills, library=library)
        check(code == 1 and "changed" in message,
              "a skill or step changed since the run is not signed: %s" % message)

        PROOF.write_draft(skill.id, body)
        code, message = PROOF.accept(skill.id, "A Person", skills=skills, library=library)
        signed = os.path.join(PROOF.PROOFS_DIR, "%s.yaml" % skill.id)
        check(code == 0 and os.path.isfile(signed), "a PASS is signed by a person: %s" % message)
        text = io.open(signed, encoding="utf-8").read() if os.path.isfile(signed) else ""
        check("by: A Person" in text and FILTER in text and "fingerprint:" in text,
              "the proof file names who signed, the steps and the fingerprint")
        check(io.open(skill.path, "rb").read() == before,
              "the skill's own file - its heron-status with it - is untouched")

        # THE GUARD BEHIND THAT, MADE TO FIRE. A copy of the skill, and a
        # proof path pointed at the copy itself, so writing the proof moves
        # the skill's file under accept. It must say so, not report signed.
        copy = os.path.join(work, "copy", os.path.basename(skill.path))
        os.makedirs(os.path.dirname(copy))
        shutil.copy(skill.path, copy)
        twin = SKILL.load(copy)
        PROOF.write_draft(skill.id, body)
        real_path = PROOF.proof_path
        PROOF.proof_path = lambda skill_id: copy
        try:
            code, message = PROOF.accept(skill.id, "A Person", skills={skill.id: twin},
                                         library=library)
        finally:
            PROOF.proof_path = real_path
        check(code == 1 and "status" in message,
              "a write that moved the skill's own file is caught, not reported signed (%r: %s)"
              % (code, message))
    finally:
        PROOF.DRAFTS_DIR, PROOF.RUNS_DIR, PROOF.PROOFS_DIR = saved
        shutil.rmtree(work, ignore_errors=True)


def section_4(skill, library):
    print("4. the driver")
    if RUN is None or PROOF is None:
        check(False, "tools/run-skill.py exists to run a skill's plan")
        return
    skills = {skill.id: skill}
    job = {"skill": skill.id, "model": "a test model",
           "steps": [{"name": FILTER, "values": {"category": "Ducts", "levelId": "Level 1"},
                      "negative_values": {"levelId": "Level 9"}},
                     {"name": COUNT, "expect": "previous"}]}
    check(RUN.check_job(job, skills, library) == [], "a well-formed job is not refused")
    filled = json.loads(json.dumps(job))
    filled["steps"][0]["values"]["levelId"] = "FILL IN"
    check(any("FILL IN" in p for p in RUN.check_job(filled, skills, library)),
          "a value still reading FILL IN is refused")
    stray = json.loads(json.dumps(job))
    stray["steps"].append({"name": PUBLISH_CARD})
    problems = RUN.check_job(stray, skills, library)
    check(any("PUBLISH" in p for p in problems) and any("does not declare" in p for p in problems),
          "a PUBLISH step, and one the skill does not declare, are refused: %r" % problems)
    negative = RUN.plan_for(job, "negative")
    check(negative["steps"][0]["values"] == {"category": "Ducts", "levelId": "Level 9"}
          and negative["steps"][1].get("expect") == "previous",
          "the negative leg merges negative_values over values")

    work = tempfile.mkdtemp(prefix="heron-run-skill-")
    saved = (PROOF.DRAFTS_DIR, PROOF.RUNS_DIR, PROOF.PROOFS_DIR)
    PROOF.DRAFTS_DIR = os.path.join(work, "drafts")
    PROOF.RUNS_DIR = os.path.join(work, "drafts", "runs")
    PROOF.PROOFS_DIR = os.path.join(work, "skill-proofs")
    job_path = os.path.join(work, "job.yaml")
    import yaml
    io.open(job_path, "w", encoding="utf-8").write(yaml.safe_dump(job))
    calls = []

    def runner(line, answers):
        calls.append(line)
        phase = "negative" if ".negative." in line[3] else "positive"
        rows = answers[phase]
        with io.open(line[5], "w", encoding="utf-8") as fh:
            fh.write(json.dumps(record(rows, phase)))
        return 0

    real = SKILL.load_all
    SKILL.load_all = lambda: (skills, [])
    try:
        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            code = RUN.main([job_path, "--dry-run"], runner=lambda line: calls.append(line))
        check(code == 0 and not calls, "a dry run sends nothing (exit %r)" % code)

        answers = {"positive": pos_steps(), "negative": neg_steps()}
        with contextlib.redirect_stdout(out):
            code = RUN.main([job_path], runner=lambda line: runner(line, answers))
        check(code == 0 and len(calls) == 2 and calls[0][2] == "plan",
              "a run sends the plan twice, positive then negative, and drafts PASS (exit %r)" % code)
        body = PROOF.read_draft(skill.id) or {}
        check(body.get("verdict") == PROOF.PASS and body["proof-draft"]["by"] == "",
              "the draft is PASS and unsigned")

        del calls[:]
        stopped = {"positive": [step(1, FILTER, ok=False, error="needs_request_values",
                                     message="no category"),
                                step(2, COUNT, ok=False, notRun=True, message="stopped")],
                   "negative": neg_steps()}
        with contextlib.redirect_stdout(out):
            code = RUN.main([job_path], runner=lambda line: runner(line, stopped))
        check(code == 1 and len(calls) == 1,
              "a positive that stopped runs no negative, and is not PASS (exit %r, %d sent)"
              % (code, len(calls)))
    finally:
        SKILL.load_all = real
        PROOF.DRAFTS_DIR, PROOF.RUNS_DIR, PROOF.PROOFS_DIR = saved
        shutil.rmtree(work, ignore_errors=True)


def main():
    skill = SKILL.load_all()[0]["count-elements"]
    library = HV.load_library()
    section_1()
    print("")
    section_2(skill, library)
    print("")
    section_3(skill, library)
    print("")
    section_4(skill, library)
    print("")
    shutil.rmtree(_SCRATCH, ignore_errors=True)
    if FAILURES:
        print("%d failure(s):" % len(FAILURES))
        for failure in FAILURES:
            print("  - %s" % failure)
        return 1
    print("A skill's plan runs whole, is judged, and only a person signs it.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
