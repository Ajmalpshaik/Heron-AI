# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-AHR-BLD-004
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The Builder - derived from the contract, writes no test, runs nothing.

    python tests/test_builder.py

WHAT IT PROVES
  1. IT RUNS NOTHING. No exec, no eval, no compile, no import of what it
     produced, and no sandbox. Generating code and then executing it is
     exactly where containment would have to be real, and Q-56 says it is
     not. This is the check that keeps that answer honest.

  2. IT WRITES NO TEST, AND SAYS WHICH ONE IS OWED. docs/24 refuses TESTING
     when the test's author and the implementer match, so a test this agent
     wrote could never carry the agent into TESTING. The suite proves the
     refusal downstream is real by asking HERON-AHR-DEP-012.

  3. THE IMPLEMENTATION IS DERIVED FROM THE CONTRACT. Change the contract's
     inputs and the signature changes; change its failures and FAILURES
     changes. Nothing is read from a stored template.

  4. REQUIRED INPUTS COME FIRST, and optional ones default to None - a
     Python signature cannot be written the other way round.

  5. THE GENERATED HEADER CLAIMS NO AGENT AND READS DISCOVERED, so a stub
     cannot raise the built count. That mistake has happened here once.

  6. IT NEVER OVERWRITES, and the destinations are checked before any of
     them is written.

  7. A DRY RUN WRITES NOTHING, and a real write puts the file exactly where
     it said it would.

  8. A CONTRACT THAT DOES NOT VALIDATE IS NOT IMPLEMENTED.

  9. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE AND REACHED
     BY THIS SUITE.
"""

import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_builder as BLD                                   # noqa: E402
import heron_contract as CON                                  # noqa: E402

FAILURES = []

CONTRACT = {
    "agent": "HERON-AHR-BLD-004",
    "version": "1.0.0",
    "input": {"view": {"type": "string", "required": True,
                       "description": "The view to read."},
              "limit": {"type": "number", "required": False,
                        "description": "How many at most."}},
    "output": {"findings": {"type": "list",
                            "description": "What it found."}},
    "allowed-tools": [],
    "timeout-seconds": 30,
    "failures": ["NO_VIEW", "NOTHING_FOUND"],
    "retry": {"attempts": 0, "on-failures": []},
}


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    known = CON.registry_ids()
    reached = set()
    source = open(os.path.join(ROOT, "brain", "heron_builder.py"),
                  encoding="utf-8").read()

    def ask(**kw):
        kw.setdefault("contract", CONTRACT)
        kw.setdefault("built_by", "a session")
        kw.setdefault("known_ids", known)
        kw.setdefault("name", "Duct Sizing Reviewer")
        answer = BLD.build(**kw)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    print("1. It runs nothing")
    for call in ("exec(", "eval(", "compile(", "subprocess", "importlib",
                 "__import__", "heron_sandbox", "runpy"):
        check(call not in source,
              "the source contains no %s" % call)
    answer = ask()
    check(any("NOTHING WAS RUN" in note for note in answer["unjudged"]),
          "and every answer says so, naming the decision behind it")
    # D-84, not Q-56. The question was answered on 2026-09-20 - keep what
    # exists, build no separate process, take the word out - and this file
    # asserted the source called it OPEN for a day after that (row 5b-56).
    check("D-84" in source,
          "and the source cites the decision that settled it, not the "
          "question as though it were still open")

    print()
    print("2. It writes no test, and says which one is owed")
    answer = ask()
    check(list(answer["files"]) == ["brain/heron_duct_sizing_reviewer.py"],
          "one file is planned, and it is not a test")
    check(answer["test_owed"] == "tests/test_duct_sizing_reviewer.py",
          "and the test that is owed is named")
    check(answer["implemented_by"] == "a session",
          "the implementer is recorded for the gate that compares them")
    # THE GATE DOWNSTREAM IS REAL, asked rather than asserted. The record
    # is this agent's own, with its stage set back to DRAFT - a fake one
    # would also be missing the files the gate reads.
    import heron_agents as REG
    import heron_deployment as DEP
    draft = dict(REG.record("HERON-AHR-BLD-004"), state="DRAFT")
    seen = {"files": draft["files"]}
    same = DEP.activate(
        "HERON-AHR-BLD-004", "TESTING", records=lambda _id: draft,
        validation=seen,
        evidence={"implemented-by": "a session", "test-author": "a session"})
    check(not same["activated"] and same["refused"] == "GATE_NOT_MET",
          "and HERON-AHR-DEP-012 really does refuse when they match")
    different = DEP.activate(
        "HERON-AHR-BLD-004", "TESTING", records=lambda _id: draft,
        validation=seen,
        evidence={"implemented-by": "a session", "test-author": "the owner"})
    check(different["activated"],
          "and lets it through when they differ - so the omission is right")
    check("def test" not in source and "tests/" not in str(answer["files"]),
          "the Builder writes nothing under tests/")

    print()
    print("3. The implementation is derived from the contract")
    text = ask()["files"]["brain/heron_duct_sizing_reviewer.py"]
    check("def run(view, limit=None):" in text,
          "the signature is the contract's inputs")
    other = dict(CONTRACT,
                 input={"sheet": {"type": "string", "required": True,
                                  "description": "The sheet."}},
                 failures=["NO_SHEET"])
    changed = ask(contract=other)["files"][
        "brain/heron_duct_sizing_reviewer.py"]
    check("def run(sheet):" in changed,
          "change the inputs and the signature changes with them")
    check('"NO_SHEET",' in changed and '"NO_VIEW",' not in changed,
          "change the failures and FAILURES changes with them")
    check("findings" in text,
          "and the return line names the contract's output")
    # NOT "the word template does not appear" - it appears in the docstring
    # saying why there isn't one. The real claim is that no module-level
    # constant holds a file body, and that implementation() reads the
    # contract rather than substituting into stored text.
    import inspect
    constants = [value for name, value in vars(BLD).items()
                 if name.isupper() and isinstance(value, str)]
    check(not any("# -*- coding" in value for value in constants),
          "no module constant holds a file body")
    body = inspect.getsource(BLD.implementation)
    check("contract" in body and ".format(" not in body
          and "% (" not in body.split("lines = [")[0],
          "implementation() reads the contract instead of filling a form")

    print()
    print("4. Required inputs come first")
    both = dict(CONTRACT,
                input={"zzz_optional": {"type": "string", "required": False,
                                        "description": "Last alphabetically."},
                       "aaa_required": {"type": "string", "required": True,
                                        "description": "First."}})
    text = ask(contract=both)["files"][
        "brain/heron_duct_sizing_reviewer.py"]
    check("def run(aaa_required, zzz_optional=None):" in text,
          "required first, optional defaulted - the only order Python takes")

    print()
    print("5. The generated header claims no agent")
    text = ask()["files"]["brain/heron_duct_sizing_reviewer.py"]
    check("# Heron-Agent:  none" in text,
          "Heron-Agent is none, so agent-count.py does not count it")
    check("# Heron-Status: DISCOVERED" in text,
          "and the status is DISCOVERED - identity assigned, nothing proven")
    check("HERON-AHR-BLD-004" in text,
          "the agent it is FOR is named in prose, where no scanner reads it "
          "as a claim")
    check("NotImplementedError" in text,
          "and the body raises rather than returning something plausible")

    print()
    print("6. It never overwrites")
    answer = ask(name="Contract")
    check(answer.get("refused") == "WOULD_OVERWRITE",
          "a module name already on disk is refused")
    check("brain/heron_contract.py" in answer["why"],
          "and the file that stopped it is named")

    print()
    print("7. A dry run writes nothing, and a real write lands where it said")
    folder = tempfile.mkdtemp(prefix="heron-builder-")
    try:
        answer = ask(root=folder)
        check(answer["written"] == [],
              "a dry run reports nothing written")
        check(os.listdir(folder) == [],
              "and really wrote nothing")
        answer = ask(root=folder, write=True)
        planned = sorted(answer["files"])
        check(answer["written"] == planned,
              "a real write reports exactly what it planned")
        for path in planned:
            check(os.path.exists(os.path.join(folder, path)),
                  "and %s is on disk" % path)
        again = ask(root=folder, write=True)
        check(again.get("refused") == "WOULD_OVERWRITE",
              "and running it twice refuses rather than replacing it")
    finally:
        shutil.rmtree(folder, ignore_errors=True)

    print()
    print("8. A contract that does not validate is not implemented")
    check(ask(contract=None).get("refused") == "NO_CONTRACT",
          "no contract is refused")
    check(ask(contract={"version": "1.0.0"}).get("refused") == "NO_CONTRACT",
          "and so is a mapping with no agent")
    check(ask(contract=dict(CONTRACT, version="nope")).get("refused")
          == "CONTRACT_INVALID",
          "a contract that does not validate is refused, with the reason")
    check(ask(built_by="").get("refused") == "NO_BUILDER_NAMED",
          "an unnamed builder is refused - the gate needs somebody to compare")
    check(ask(layer="nowhere").get("refused") == "NO_SUCH_LAYER",
          "a layer that does not exist is refused, naming the ones that do")
    # A DESTINATION WHOSE PARENT IS A FILE. A permission-denied path is no
    # good here: this container runs as root and would create it.
    blocked = tempfile.mkdtemp(prefix="heron-builder-blocked-")
    try:
        with open(os.path.join(blocked, "brain"), "w") as handle:
            handle.write("not a directory")
        answer = ask(root=blocked, write=True)
        check(answer.get("refused") == "WRITE_FAILED",
              "a write that cannot happen is refused, not raised")
        check(answer.get("written") == [],
              "and the refusal lists what had been written before it failed")
    finally:
        shutil.rmtree(blocked, ignore_errors=True)

    print()
    print("9. Every failure the contract declares is named and reached")
    declared = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-AHR-BLD-004.yaml"))
    named = declared.get("failures") or []
    for failure in named:
        check(failure in source, "the code names %s" % failure)
    unreached = sorted(set(named) - reached - {"REGISTER_UNREADABLE"})
    check(not unreached,
          "and every state but REGISTER_UNREADABLE was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    it writes code and it does not run it")
    return 0


if __name__ == "__main__":
    sys.exit(main())
