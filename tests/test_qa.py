# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-DEV-QA-016
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The final gate - it runs nothing, and what must pass is asked of CI.

    python tests/test_qa.py

WHAT IT PROVES
  1. WHAT MUST PASS IS THE WORKFLOW'S LIST, PARSED. Add a job to a
     workflow and the requirement moves with it.

  2. NOTHING IS RUN AND NOTHING IS FIXED, by the imports and by the
     source.

  3. A CHECK NOBODY RAN IS NOT A CHECK THAT PASSED, and a check of a
     previous version is a check of something else.

  4. THE SECURITY REVIEW IS HERON-DEV-SEC-009's ANSWER, carried through
     unaltered - the same objects, not a copy of the rule.

  5. NEVER THE IMPLEMENTER, structurally.

  6. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import ast
import io
import os
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_qa as QA                                          # noqa: E402
import heron_security as SEC                                   # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []

CHANGE = {"name": "a change", "risk": "MODIFY", "by": "Ajmal"}


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_qa.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]

    sha = QA.fingerprint(CHANGE)
    review = {"by": "Reviewer", "of": sha}
    must = QA.required()
    every = [{"check": name, "passed": True, "of": sha} for name in must]

    print("\n1. what must pass is the workflow's list, parsed")
    check(must, "the real workflow names %d check(s): %s"
                % (len(must), ", ".join(must)))
    workflow = io.open(QA.WORKFLOW, encoding="utf-8").read()
    for name in must:
        check(name in workflow, "  %s is in the file" % name)
    # THE REQUIREMENT MOVES WITH THE FILE. A typed list survives a word
    # search; it does not survive a new job.
    handle, path = tempfile.mkstemp(suffix=".yml")
    os.close(handle)
    io.open(path, "w", encoding="utf-8").write(
        workflow + "\n      - name: a new gate\n"
                   "        run: python tools/check-something-new.py\n")
    try:
        grown = QA.required(path)
        check("tools/check-something-new.py" in grown,
              "a job added to the workflow becomes required without this "
              "file being touched")
        check(len(grown) == len(must) + 1,
              "  and nothing else moved: %d -> %d" % (len(must), len(grown)))
        said = QA.gate(CHANGE, every, approved_by="Reviewer",
                       reviewed=review, checks=grown)
        check(said.get("refused") == "CHECK_MISSING"
              and said["missing"] == ["tools/check-something-new.py"],
              "  and a change that did not run it is refused for exactly "
              "that check")
    finally:
        os.unlink(path)
    check(QA.required(os.path.join(ROOT, "no-such-workflow.yml")) == (),
          "an unreadable workflow requires nothing...")
    check(QA.gate(CHANGE, every, checks=[]).get("refused")
          == "NOTHING_IS_REQUIRED",
          "...and requiring nothing is refused, not passed - a file that "
          "cannot be read and one that requires nothing come back the same")

    print("\n2. nothing is run and nothing is fixed")
    imports = sorted(set(
        node.names[0].name.split(".")[0] if isinstance(node, ast.Import)
        else (node.module or "").split(".")[0]
        for node in ast.walk(ast.parse(whole))
        if isinstance(node, (ast.Import, ast.ImportFrom))))
    check("subprocess" not in imports and "os" in imports,
          "it imports no subprocess: %s" % ", ".join(imports))
    check("yaml" not in imports,
          "and no yaml - brain/heron_dotnet.py hoisting one killed the CI "
          "compile job, which installs a .NET SDK and nothing else")
    for writing in ("os.system", "subprocess", "os.remove", "shutil",
                    ".write("):
        check(writing not in logic, "the agent never uses %s" % writing)
    # IT DOES OPEN A FILE - that is how it reads the workflow - so the
    # check is that every open is a READ. A word search for `open(` hits
    # the one legitimate use and says nothing about mode.
    writes = []
    for node in ast.walk(ast.parse(whole)):
        if not isinstance(node, ast.Call):
            continue
        name = getattr(node.func, "attr", getattr(node.func, "id", ""))
        if name != "open":
            continue
        mode = next((one.value for one in node.keywords
                     if one.arg == "mode"), None)
        if len(node.args) > 1 and isinstance(node.args[1], ast.Constant):
            mode = node.args[1]
        if mode is not None and "r" not in str(getattr(mode, "value", "")):
            writes.append(node.lineno)
    check(not writes,
          "and every open() in it is a read: %d call(s), none with a write "
          "mode" % sum(1 for node in ast.walk(ast.parse(whole))
                       if isinstance(node, ast.Call)
                       and getattr(node.func, "attr",
                                   getattr(node.func, "id", "")) == "open"))
    good = QA.gate(CHANGE, every, approved_by="Reviewer", reviewed=review)
    check(good["ranAnything"] is False and good["fixedAnything"] is False,
          "`ranAnything` and `fixedAnything` are both false, and always")
    check(any("NOTHING WAS RUN HERE" in line for line in good["unjudged"]),
          "and the answer says so first")

    print("\n3. a check nobody ran is not a check that passed")
    for missing in (every[:-1], every[1:], []):
        answer = QA.gate(CHANGE, missing, approved_by="R", reviewed=review)
        check(answer.get("refused") == "CHECK_MISSING",
              "%d of %d results is CHECK_MISSING"
              % (len(missing), len(must)))
    one_failed = [dict(one, passed=False) if one["check"] == must[0] else one
                  for one in every]
    answer = QA.gate(CHANGE, one_failed, approved_by="R", reviewed=review)
    check(answer.get("refused") == "CHECK_FAILED"
          and [x["check"] for x in answer["failed"]] == [must[0]],
          "one failing check is CHECK_FAILED, and it is named")
    later = dict(CHANGE, name="a change ")
    answer = QA.gate(later, every, approved_by="R", reviewed=review)
    check(answer.get("refused") == "STALE_RESULT",
          "one space added after the checks ran is STALE_RESULT")
    check(answer["stale"][0]["isNow"] == QA.fingerprint(later)[:16],
          "  with the fingerprint recomputed here, not trusted")
    no_sha = [{"check": name, "passed": True} for name in must]
    answer = QA.gate(CHANGE, no_sha, approved_by="R", reviewed=review)
    check(answer["passed"] is True,
          "a result carrying NO fingerprint is accepted - most checks do "
          "not record what they ran against, and refusing them would make "
          "this unusable")
    check(any("recorded NONE" in line for line in answer["unjudged"]),
          "  and that concession is stated rather than hidden")
    check(sorted(answer["unfingerprinted"]) == sorted(must),
          "  with every unfingerprinted check named in the answer itself, "
          "not only in prose")

    print("\n4. the security review is its owner's answer")
    check(QA.fingerprint is SEC.fingerprint,
          "QA.fingerprint IS SEC.fingerprint - two ways of fingerprinting "
          "one change is how two agents disagree about staleness")
    check(QA.security_review is SEC.review,
          "and QA.security_review IS SEC.review")
    answer = QA.gate(CHANGE, every, approved_by="R")
    check(answer.get("refused") == "NOT_SECURITY_REVIEWED",
          "a MODIFY change with no review is refused")
    theirs = SEC.review(CHANGE, reviewed=None)
    check(answer["security"]["refused"] == theirs["refused"],
          "  in HERON-DEV-SEC-009's own words: %s" % theirs["refused"])
    check(theirs["refused"] in answer["why"],
          "  and its refusal is quoted rather than translated")
    low = dict(CHANGE, risk="READ")
    passed = QA.gate(low, [{"check": name, "passed": True} for name in must],
                     approved_by="R")
    check(passed["passed"] is True,
          "a READ change needs no security review and can pass")
    check(passed["security"]["required"] is False
          and "not required" in passed["why"],
          "  and the answer says the review was not required rather than "
          "implying one happened")

    print("\n5. never the implementer")
    for who in ("Ajmal", "ajmal", "  AJMAL "):
        answer = QA.gate(CHANGE, every, approved_by=who, reviewed=review)
        check(answer.get("refused") == "APPROVED_BY_THE_IMPLEMENTER",
              "%r approving their own change is refused" % who)
    check("never the implementer" in QA.gate(
        CHANGE, every, approved_by="Ajmal", reviewed=review)["why"],
        "quoting the register's own words")
    answer = QA.gate(CHANGE, every, reviewed=review)
    check(answer.get("refused") == "NOBODY_IS_APPROVING",
          "every check passing and nobody approving does NOT pass")
    check("Golden Rule 7" in answer["why"],
          "  because `passed` with nobody's name on it is this agent "
          "approving its own report")

    print("\n6. every failure the contract declares is named and reached")
    for kw, name in (
            (dict(change=None), "NOTHING_TO_GATE"),
            (dict(change="a string"), "NOT_A_SUBMISSION"),
            (dict(change=CHANGE, results=["a string"]), "NOT_A_RESULT"),
            (dict(change=CHANGE, results=[{"passed": True}]), "NOT_A_RESULT"),
            (dict(change=CHANGE, results=every, checks=[]),
             "NOTHING_IS_REQUIRED"),
            (dict(change=CHANGE, results=every[:-1], approved_by="R",
                  reviewed=review), "CHECK_MISSING"),
            (dict(change=CHANGE, results=one_failed, approved_by="R",
                  reviewed=review), "CHECK_FAILED"),
            (dict(change=dict(CHANGE, name="x"), results=every,
                  approved_by="R", reviewed=review), "STALE_RESULT"),
            (dict(change=CHANGE, results=every, approved_by="R"),
             "NOT_SECURITY_REVIEWED"),
            (dict(change=CHANGE, results=every, reviewed=review),
             "NOBODY_IS_APPROVING"),
            (dict(change=CHANGE, results=every, approved_by="Ajmal",
                  reviewed=review), "APPROVED_BY_THE_IMPLEMENTER")):
        answer = QA.gate(**kw)
        reached.add(answer.get("refused"))
        check(answer.get("refused") == name, "%s is reached" % name)

    print()
    print("R. THE SECOND CODEX REVIEW - a result nothing can tie to this "
          "change")
    signed = [{"check": name, "passed": True, "of": QA.fingerprint(CHANGE)}
              for name in must]
    mixed = list(signed[:-1]) + [{"check": must[-1], "passed": True}]
    answer = QA.gate(CHANGE, mixed, approved_by="R", reviewed=review)
    reached.add(answer.get("refused"))
    check(answer.get("refused") == "RESULT_IS_UNFINGERPRINTED",
          "a result with NO fingerprint beside one that HAS is refused - "
          "once a check in this run recorded what it ran against, one "
          "that did not is a check that did not record rather than one "
          "that cannot")
    check(answer["unfingerprinted"] == [must[-1]],
          "  and `unfingerprinted` names it, so the hole is structural "
          "rather than only described in prose")
    answer = QA.gate(CHANGE, no_sha, approved_by="R", reviewed=review)
    check(answer["passed"] is True and
          sorted(answer["unfingerprinted"]) == sorted(must),
          "a run where NO result carries one still passes - the "
          "concession stands - but every check is named in "
          "`unfingerprinted` rather than silently counted as fresh")

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-DEV-QA-016.yaml"))
    named = contract.get("failures") or []
    check(len(named) == len(set(named)),
          "the contract declares each failure once")
    for failure in named:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    check(len(good["unjudged"]) == 5, "five things are left unjudged")
    check(not (contract.get("allowed-tools") or []),
          "and the contract allows no tools - it is a gate, not a runner")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    it runs nothing, and what must pass is asked of CI")
    return 0


if __name__ == "__main__":
    sys.exit(main())
