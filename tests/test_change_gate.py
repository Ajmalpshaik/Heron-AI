#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The change gate: scope against intent, and evidence before and after.

WHY THE NEGATIVE CASES ARE MOST OF THIS FILE
---------------------------------------------
A scope checker that never says no is worse than no scope checker, because it
is believed. So the checks below are mostly about what the gate REFUSES:

  - an unrelated file is SPLIT, not a warning
  - an intent naming a folder that is not a part of this repository is
    BLOCKED, not best-effort
  - a change with no evidence is not a PASS
  - a suite that COULD NOT RUN does not count as one that passed
  - a GATE that could not run does not count as one that ran, which is the
    same rule one level up and was NOT true until 2026-09-21
  - two unknowns are not a match

    python tests/test_change_gate.py
"""

import importlib.util
import io
import json
import os
import shutil
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

FAILURES = []


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def load(name, filename):
    """A tool with a hyphen in its name cannot be imported by name."""
    path = os.path.join(ROOT, "tools", filename)
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


CHANGE = load("heron_check_change", "check-change.py")

# THE VERDICT MUST NOT DEPEND ON WHAT IS SITTING IN THE WORKING TREE.
#
# The comment under 'A gate recorded as NOT RUN' already learned half of this:
# the gates a change owes are read from the tool rather than typed, because a
# diff touching revit/ raises a signal and the suite went red beside a branch
# that had nothing to do with it. THE SCOPE HALF WAS STILL LIVE.
# check-change.py reads the working tree, so ONE untracked file in no declared
# part - a scratch folder, an editor artefact, `.agents/` on the owner's PC -
# turns every verdict below into SPLIT before the evidence is ever weighed,
# and the checks fail for a reason that has nothing to do with the gate.
#
# Measured 2026-09-22: with `.agents/` present, four failures. With that one
# folder moved aside and nothing else touched, the suite passed. CI clones
# into a clean tree, so it is green there forever and this only ever appears
# on a machine somebody actually works in.
#
# `HEAD..HEAD` is an EMPTY range: no file is in scope, so nothing can be out
# of it, and the evidence record - which is what these checks are ABOUT - is
# the only thing left to decide the verdict. The two calls that check argument
# handling exit 2 before any diff is read, so they neither need this nor have
# it.
ISOLATED = ["--range", "HEAD..HEAD"]
EVIDENCE = load("heron_change_evidence", "change-evidence.py")


def classify(path, areas):
    struct = CHANGE.layering()
    known = set(struct.PARTS) | set(struct.SUPPORT)
    allowed = set()
    for name in areas:
        allowed |= set(struct.ALLOWED.get(name, set()))
    allowed -= set(areas)
    return CHANGE.classify(path, set(areas), allowed, known)


def main():
    print("Scope: a file is judged by the part that owns it, not by its words")
    check(classify("brain/heron_retrieve.py", ["brain"]) == CHANGE.REQUIRED,
          "a file in a declared part is required")
    check(classify("brain/heron_retrieve.py", ["mcp"]) == CHANGE.SUPPORTING,
          "brain is SUPPORTING work for an mcp change - mcp may depend on brain")
    check(classify("revit/Heron.Bridge/BridgeServer.cs", ["mcp"]) == CHANGE.UNRELATED,
          "revit is UNRELATED to an mcp change - mcp may NOT depend on revit")
    check(classify("platform/Heron.Core/HeronPaths.cs", ["brain"]) == CHANGE.SUPPORTING,
          "everything may depend on platform, so platform is supporting")
    check(classify("brain/heron_retrieve.py", ["platform"]) == CHANGE.UNRELATED,
          "and platform may depend on nothing, so the reverse is unrelated")

    print()
    print("Scope: the three classes that are counted rather than questioned")
    check(classify("tests/test_retrieve.py", ["brain"]) == CHANGE.TESTS,
          "a test is a test whatever part the change declared")
    check(classify("docs/05-heron-brain.md", ["brain"]) == CHANGE.DOCS,
          "so is documentation")
    check(classify("brain/README.md", ["brain"]) == CHANGE.DOCS,
          "including a README inside the declared part")
    check(classify("Directory.Build.props", ["revit"]) == CHANGE.BUILD,
          "build and config are separated even inside a declared part")
    check(classify(".github/workflows/gates.yml", ["tools"]) == CHANGE.BUILD,
          "CI counts as build/config")
    check(classify("revit/Heron.Revit.Addin/Heron.Revit.Addin.csproj", ["revit"])
          == CHANGE.BUILD,
          "a project file is build/config BEFORE it is a file in a declared part - "
          "a change to how Heron is built must never hide inside one")

    print()
    print("Scope: a folder no rule recognises always raises")
    check(classify("experiments/scratch.py", ["brain", "tools"]) == CHANGE.UNRELATED,
          "an unknown top-level folder is unrelated even when several parts "
          "were declared")

    print()
    print("Obligations: risk sets the floor, signals add to it")
    low = CHANGE.obliged("low", {})
    check("check-structure" in low and "tests" in low,
          "even a low-risk change owes the three gates and the suites")
    check("human-review" not in low, "and does not owe a human")
    high = CHANGE.obliged("high", {})
    check("human-review" in high, "a high-risk change owes a human review")
    trust = CHANGE.obliged("low", {"trust": {}})
    check("human-review" in trust,
          "touching a permission file owes a human whatever the declared risk - "
          "the diff outranks the label")
    frag = CHANGE.obliged("low", {"fragment-behaviour": {}})
    check("REAL-REVIT-PROOF" in frag,
          "changing a fragment owes a run against a model (D-30), not a compile")
    pkg = CHANGE.obliged("medium", {"packaging": {}})
    check("check-package" in pkg, "touching delivery owes the packaging gate")

    print()
    print("Intent: three fields, and an unusable one stops the run")
    with tempfile.NamedTemporaryFile("w", suffix=".md", delete=False) as handle:
        handle.write("# a note\n\nintent: stop the duct filter losing its words\n"
                     "area: brain\nrisk: low\n\nmore prose\n")
        note = handle.name
    try:
        fields, err = CHANGE.read_intent_file(note)
        check(err is None and fields.get("area") == "brain",
              "the three fields are read out of an ordinary work note")
        check(fields.get("intent").startswith("stop the duct"),
              "and the intent survives being surrounded by prose")
    finally:
        os.unlink(note)

    out = subprocess.run([sys.executable, os.path.join(ROOT, "tools", "check-change.py"),
                          "--intent", "x", "--area", "nonsense", "--risk", "low"],
                         capture_output=True, text=True, cwd=ROOT)
    check(out.returncode == 2, "an area that is not a part of this repository is "
                               "BLOCKED (exit 2), not judged anyway")
    check("nonsense" in out.stdout, "and the message names it")

    out = subprocess.run([sys.executable, os.path.join(ROOT, "tools", "check-change.py"),
                          "--area", "brain", "--risk", "low"],
                         capture_output=True, text=True, cwd=ROOT)
    check(out.returncode == 2, "so is a change with no intent at all")

    print()
    print("Evidence: what moved, decided on sets rather than totals")
    before = {"gates": {"check-docs": {"result": "PASS"},
                        "tests": {"result": "PASS"}},
              "tests": {"test_a.py": 0, "test_b.py": 1, "test_c.py": 3},
              "counts": {"fragments": 360}}
    same = {"gates": {"check-docs": {"result": "PASS"},
                      "tests": {"result": "PASS"}},
            "tests": {"test_a.py": 0, "test_b.py": 1, "test_c.py": 3},
            "counts": {"fragments": 360}}
    check(EVIDENCE.compare(before, same)["ruling"] == "NO CHANGE MEASURED",
          "nothing moved reads as NO CHANGE MEASURED - never as 'fine'")

    worse = json.loads(json.dumps(same))
    worse["tests"]["test_a.py"] = 1
    result = EVIDENCE.compare(before, worse)
    check(result["ruling"] == "REVERT", "a suite that passed and now fails is REVERT")
    check(any("test_a.py" in line for line in result["regressions"]),
          "and the record names which one")

    better = json.loads(json.dumps(same))
    better["tests"]["test_b.py"] = 0
    check(EVIDENCE.compare(before, better)["ruling"] == "KEEP",
          "a suite that failed and now passes, with nothing broken, is KEEP")

    mixed = json.loads(json.dumps(same))
    mixed["tests"]["test_b.py"] = 0
    mixed["tests"]["test_a.py"] = 1
    check(EVIDENCE.compare(before, mixed)["ruling"] == "REVERT",
          "one fix and one break is REVERT - a fix does not pay for a regression")

    print()
    print("Evidence: could-not-run is neither a pass nor a failure")
    arrived = json.loads(json.dumps(same))
    arrived["tests"]["test_c.py"] = 0
    result = EVIDENCE.compare(before, arrived)
    check(result["ruling"] == "NO CHANGE MEASURED",
          "a suite going from could-not-run to pass is not an improvement in "
          "the code - somebody installed something")
    check(any("test_c.py" in line for line in result["incomparable"]),
          "and it is reported as not comparable rather than dropped")

    vanished = json.loads(json.dumps(same))
    del vanished["tests"]["test_a.py"]
    result = EVIDENCE.compare(before, vanished)
    check(any("test_a.py" in line for line in result["incomparable"]),
          "a suite that ran before and not after is not comparable either")
    check(not result["regressions"],
          "and is not counted as a regression, because nobody measured it")

    gate = EVIDENCE.tests_gate({"a.py": 0, "b.py": 3}, "")
    check(gate["result"] == "PASS" and "could not run" in gate["detail"],
          "a suite that could not run does not fail the gate, and is named anyway")
    gate = EVIDENCE.tests_gate({"a.py": 1}, "")
    check(gate["result"] == "FAIL", "a real failure does fail it")
    gate = EVIDENCE.tests_gate({}, "not run")
    check(gate["result"] == "NOT RUN", "and no suites at all is NOT RUN, not PASS")

    # AND THE PRINTED SUMMARY HAS TO SAY THE SAME THING AS THE GATE. It did
    # not: `bad` was every non-zero code, so one record printed
    #
    #     tests   PASS   2 ran, 1 could not run (test_mcp_serves.py)
    #     2 ran, 1 did not pass
    #
    # three lines apart - the gate right, the headline merging NOT RUN into
    # failed. A record exists to be believed, and a reader who reads the
    # second line has been told the opposite of the first. Row 5b-60.
    caught, held = io.StringIO(), sys.stdout
    try:
        sys.stdout = caught
        EVIDENCE.show({"tests": {"a.py": 0, "b.py": 1, "c.py": 3},
                       "gates": {}, "counts": {}})
    finally:
        sys.stdout = held
    said = caught.getvalue()
    check("3 ran, 1 did not pass, 1 could not run" in said,
          "the printed summary counts failed and could-not-run separately")
    check("b.py" in said and "c.py" in said,
          "and names both, so neither is a number the reader has to trust")
    check("could not run" in said.split("c.py", 1)[-1][:40],
          "with c.py marked as could not run rather than as a failure")

    print()
    print("Evidence: a claim and a measurement are different kinds of fact")
    stated = EVIDENCE.stated_gates(["check-compile=PASS:ran on the PC"])
    check(stated["check-compile"]["source"] == "stated",
          "a gate run elsewhere is marked stated, never derived")
    check(EVIDENCE.stated_gates(["check-compile=PROBABLY"]) == {},
          "and a result that is not PASS, FAIL or NOT_RUN is refused")

    print()
    print("A gate that was already failing is not this change")
    result = EVIDENCE.compare(before, worse)
    check(result["regressed"]["suites"] == ["test_a.py"],
          "the regression is reported as a NAME as well as a sentence, so a "
          "reader downstream need not parse English")
    check("test_b.py" not in result["regressed"]["suites"],
          "and a suite failing on both sides is not in it")

    both_red = {"gates": {"tests": {"result": "FAIL"}},
                "tests": {"test_a.py": 1}, "counts": {},
                "compared_to": {"label": "before"},
                "regressed": {"gates": [], "suites": []},
                "regressions": []}
    import tempfile as _tf
    handle = _tf.NamedTemporaryFile("w", suffix=".json", delete=False)
    json.dump(both_red, handle)
    handle.close()
    try:
        out = subprocess.run([sys.executable, os.path.join(ROOT, "tools", "check-change.py"),
                              "--intent", "anything", "--area",
                              ",".join(sorted(CHANGE.layering().PARTS) +
                                       sorted(CHANGE.layering().SUPPORT)),
                              "--risk", "low", "--evidence", handle.name] + ISOLATED,
                             capture_output=True, text=True, cwd=ROOT)
        check("Failing before too:     tests" in out.stdout,
              "a gate failing on BOTH sides is named as pre-existing")
        check("a gate this change owes was run and failed" not in out.stdout,
              "and is not given as a reason to revise - otherwise every verdict "
              "on a machine without the MCP SDK would be REVISE for ever")

        no_baseline = dict(both_red)
        no_baseline.pop("compared_to")
        io.open(handle.name, "w", encoding="utf-8").write(json.dumps(no_baseline))
        out = subprocess.run([sys.executable, os.path.join(ROOT, "tools", "check-change.py"),
                              "--intent", "anything", "--area",
                              ",".join(sorted(CHANGE.layering().PARTS) +
                                       sorted(CHANGE.layering().SUPPORT)),
                              "--risk", "low", "--evidence", handle.name] + ISOLATED,
                             capture_output=True, text=True, cwd=ROOT)
        check("was run and failed" in out.stdout,
              "with NO before-measurement the same record blocks - nothing can "
              "tell a pre-existing failure from a new one, so it says the "
              "cautious thing")
    finally:
        os.unlink(handle.name)

    print()
    print("A gate recorded as NOT RUN has not been run")
    # change-evidence keeps three states apart - PASS, FAIL and NOT RUN - and
    # check-change asked only whether the gate's NAME was in the record. So a
    # capture whose --tests matched no suite recorded `tests: NOT RUN` and the
    # verdict was PASS, "every gate it owes was run". Found by watching it
    # happen on this branch's own evidence. FRAGMENT-ISSUES section 5b.
    folder = tempfile.mkdtemp(prefix="heron-gate-")
    record = os.path.join(folder, "record")

    # EVERY GATE THE TOOL COULD EVER OWE, read from the tool rather than
    # typed. The first version of this listed the five a medium-risk change
    # owes, and then passed or failed depending on WHAT WAS IN THE WORKING
    # TREE: a diff touching revit/ raises the revit-version signal, which
    # owes check-compile and check-api-surface, and those were not in the
    # record - so the suite went red on the next branch to edit an add-in
    # file, for a reason that has nothing to do with what it is testing.
    # Caught by that happening. A test whose verdict depends on the diff it
    # happens to be run beside is not testing the thing it names.
    possible = set(CHANGE.ALWAYS)
    for owed in CHANGE.BY_RISK.values():
        possible |= set(owed)
    for owed in CHANGE.BY_SIGNAL.values():
        possible |= set(owed)
    every = dict((name, "PASS") for name in sorted(possible))

    def verdict(gates):
        io.open(record, "w", encoding="utf-8").write(json.dumps({
            "commit": "0" * 12, "gates": dict(
                (name, {"result": state, "exit": 0 if state == "PASS" else None,
                        "source": "derived", "detail": ""})
                for name, state in gates.items()),
            "tests": {}, "counts": {},
        }))
        out = subprocess.run(
            [sys.executable, os.path.join(ROOT, "tools", "check-change.py"),
             "--intent", "anything", "--area",
             ",".join(sorted(CHANGE.layering().PARTS) +
                      sorted(CHANGE.layering().SUPPORT)),
             "--risk", "medium", "--evidence", record] + ISOLATED,
            capture_output=True, text=True, cwd=ROOT)
        return out.stdout

    said = verdict(every)
    check("Owed and not run" not in said,
          "with every owed gate PASS, nothing is reported as owed and not run")

    said = verdict(dict(every, tests="NOT RUN"))
    check("REVISE" in said and "tests" in said,
          "and with the tests gate NOT RUN the verdict is REVISE, naming it")
    check("Owed and not run:       tests" in said,
          "said in as many words, rather than left to be worked out")
    check("Gates actually run:" not in said.split("In the record, NOT RUN")[0]
          or "tests" not in said.split("Gates actually run:")[1].split("\n")[0],
          "and it is NOT listed under 'Gates actually run', which is the half "
          "a reader sees")

    said = verdict(dict(every, tests="FAIL"))
    check("REVISE" in said,
          "a gate that RAN and failed is still REVISE - this fix must not "
          "have quietly turned a failure into a missing gate")
    shutil.rmtree(folder, ignore_errors=True)

    print()
    print("The gate refuses to call an unevidenced change a pass")
    out = subprocess.run([sys.executable, os.path.join(ROOT, "tools", "check-change.py"),
                          "--intent", "anything", "--area",
                          ",".join(sorted(CHANGE.layering().PARTS) +
                                   sorted(CHANGE.layering().SUPPORT)),
                          "--risk", "low"] + ISOLATED,
                         capture_output=True, text=True, cwd=ROOT)
    check(out.returncode == 1 and "no evidence record" in out.stdout,
          "with every part declared, so nothing can be out of scope, the verdict "
          "is still REVISE - a change with no evidence is not a pass")

    print()
    if FAILURES:
        print("FAILED")
        for f in FAILURES:
            print("  - %s" % f)
        return 1
    print("PASSED - scope is structural, obligations follow the diff, and")
    print("         'could not run' never reads as 'passed'.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
