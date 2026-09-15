# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-RPT-RED-003
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Report release - a report is an egress surface, and the line is the file.

    python tests/test_release.py

WHAT IT PROVES
  1. D-26's TWO COLUMNS ARE WHAT THE AGENT SAYS THEY ARE, read out of
     DECISIONS.md - including its warning about which version won.

  2. PROJECT NAMES ARE NOT STRIPPED. The content comes back untouched
     and the name is still in it, because D-26 puts it in the
     "fine in the conversation" column. docs/28 still says "strips".

  3. A REVIT BINARY NEVER LEAVES - all four extensions, by name alone.

  4. A CREDENTIAL IS REFUSED WHOLE, and its value is nowhere in the
     answer.

  5. TWO PROJECTS IN ONE REPORT IS REFUSED, and absence of a policy is
     not permission.

  6. NOTHING IS SENT AND NOTHING IS CHANGED.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_release as RED                                    # noqa: E402
import heron_secrets as SECRETS                                # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_release.py"),
                    encoding="utf-8").read()
    code = whole.split("\nfrom __future__", 1)[1]
    logic = code.split("\ndef main(")[0]

    def ask(report, **kw):
        answer = RED.release(report, **kw)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    CLEAN = {"content": "Tower A has 47 ducts over 300x300.\n"}
    OK = {"to": "client@example.com", "projects": ["Tower A"],
          "policy": {"Tower A": "yes"}}
    TOKEN = SECRETS.FAKE_FORGE_TOKEN

    print("1. D-26's two columns are what the agent says")
    decisions = " ".join(io.open(os.path.join(ROOT, "docs", "DECISIONS.md"),
                                 encoding="utf-8").read().split())
    check("The model file is never uploaded" in decisions,
          "D-26: the model file is never uploaded")
    check("a reader needs to know which version won" in decisions,
          "and it warns that a reader needs to know which version won")
    check("from *nothing may travel* toward *the file may not travel*"
          in decisions,
          "having moved from 'nothing may travel' to 'the file may not "
          "travel'")
    for travels in ("Project names, file names, content names",
                    "Element data — counts, sizes, parameters",
                    "Engineering ideas, reasoning, and code"):
        check(travels in decisions,
              "  '%s' is in the FINE column" % travels[:34])
    check("project-based knowledge must be kept segregated" in decisions,
          "and project knowledge stays segregated")
    check(sorted(RED.NEVER_LEAVES) == [".rfa", ".rft", ".rte", ".rvt"],
          "the agent guards four extensions")

    print("\n2. Project names are not stripped")
    passed = ask(CLEAN, **OK)
    check(passed["may_release"] is True, "a clean report may go")
    check("Tower A" in CLEAN["content"],
          "and 'Tower A' is still in the content - it was never touched")
    check(passed["projects"] == ["Tower A"],
          "the project is named in the answer, not hidden")
    for stripping in ("replace(", "sub(", "anonymis", "anonymiz",
                      "strip_project", "mask("):
        check(stripping not in logic,
              "the agent never uses %s on the content" % stripping)
    # It DOES call redact() - to DETECT a credential - and throws the
    # cleaned copy away. That is the distinction the whole file is about,
    # so it is checked precisely rather than banned by name.
    check("_clean, shapes = SECRETS.Secrets().redact(content)" in logic,
          "it calls redact() only to DETECT, and names the cleaned copy "
          "`_clean` to throw it away")
    check("content = " not in logic.split("content = str(report")[1],
          "and `content` is never reassigned after it is read, so no "
          "cleaned version can reach an answer")
    register = io.open(os.path.join(ROOT, "docs", "28-agent-registry.md"),
                       encoding="utf-8").read()
    check("Strips project identifiers" in register,
          "while docs/28's row still says 'Strips project identifiers'")
    check("F20" in whole, "the agent records that as PROPOSALS F20")
    check(any("D-26's RULE RATHER THAN AN OVERSIGHT" in line
              for line in passed["unjudged"]),
          "and every answer says the not-stripping is D-26's rule rather "
          "than an oversight")
    check(passed["travels"] == list(RED.TRAVELS) and len(RED.TRAVELS) == 5,
          "the answer carries what IS fine, not only what is not")

    print("\n3. A Revit binary never leaves")
    for extension in RED.NEVER_LEAVES:
        answer = ask({"content": "see attached",
                      "attachments": ["Tower A%s" % extension]}, **OK)
        check(answer.get("refused") == "CARRIES_A_MODEL_FILE",
              "'Tower A%s' is refused" % extension)
        check(answer["files"] == ["Tower A%s" % extension],
              "  and named")
    check(ask({"content": "x", "attachments": ["REPORT.RVT"]}, **OK
              ).get("refused") == "CARRIES_A_MODEL_FILE",
          "in capitals too")
    check(ask({"content": "x", "attachments": ["notes.pdf", "table.csv"]},
              **OK)["may_release"] is True,
          "while a PDF and a CSV are fine - the line is the Revit file")
    for opening in ("open(", "os.stat", "os.path.getsize", "read("):
        check(opening not in logic,
              "and the attachment is never opened (%s) - a name is enough"
              % opening)

    print("\n4. A credential is refused whole")
    leaked = ask({"content": "the key is " + TOKEN}, **OK)
    check(leaked.get("refused") == "CARRIES_A_SECRET",
          "a report carrying a token is refused")
    check(TOKEN not in str(leaked), "and the token is nowhere in the answer")
    check(TOKEN[:12] not in str(leaked), "not even part of it")
    check("[redacted]" not in str(leaked),
          "and no redacted copy - refused, not cleaned")
    check(leaked["shapes"] == ["github token"],
          "what comes back is the shape: %s" % leaked["shapes"])
    check("somebody should look at" in leaked["why"],
          "the refusal says why cleaning and sending would be wrong")
    check("article 17" in leaked["why"].lower(), "and cites article 17")

    print("\n5. Two projects, and absence is not permission")
    mixed = ask(CLEAN, to="x", projects=["Tower A", "Tower B"],
                policy={"Tower A": "yes", "Tower B": "yes"})
    check(mixed.get("refused") == "MIXES_PROJECTS",
          "two projects in one report is refused even when both allow it")
    check(mixed["projects"] == ["Tower A", "Tower B"], "and both are named")
    check("does not leak into project B" in mixed["why"],
          "quoting D-26's third point")
    silent = ask(CLEAN, to="x", projects=["Tower A"])
    check(silent.get("refused") == "THE_PROJECT_FORBIDS_IT",
          "a project with no declared policy is refused")
    check("Absence is not permission" in silent["why"],
          "because absence is not permission")
    for said in ("no", "false", "never", "none", "NEVER"):
        check(ask(CLEAN, to="x", projects=["Tower A"],
                  policy={"Tower A": said}).get("refused")
              == "THE_PROJECT_FORBIDS_IT",
              "'%s' is a refusal" % said)
    check(ask(CLEAN, to="x", projects=["Tower A"],
              policy={"tower a": "yes"})["may_release"] is True,
          "and a policy matches the project without case")
    check(ask(CLEAN, to="x")["may_release"] is True,
          "while a report drawing on NO named project is not blocked by a "
          "policy nobody needed")

    print("\n6. Nothing is sent and nothing is changed")
    check(passed["released"] is False, "`released` is false even when it may")
    for sending in ("smtp", "requests", "urlopen", "socket", "subprocess",
                    "shutil", "write("):
        check(sending not in logic, "the agent never uses %s" % sending)
    check(len(passed["unjudged"]) == 4, "four things are left unjudged")
    check(any("should be sent" in line.lower()
              for line in passed["unjudged"]),
          "including that whether it SHOULD be sent is a person's")

    print("\n7. Every failure is named and reached")
    for bad, why in ((None, "None is not a report"),
                     ("a report", "a string is not one"),
                     ({}, "a map with no content"),
                     ({"content": " "}, "and blank content")):
        check(ask(bad, **OK).get("refused") == "NOT_A_REPORT", why)
    for nowhere in (None, "", "   "):
        check(ask(CLEAN, to=nowhere).get("refused") == "NO_DESTINATION",
              "no destination (%r) is refused before anything else"
              % nowhere)
    # THE ORDER: a report wrong four ways is refused for the destination.
    worst = ask({"content": "key " + TOKEN, "attachments": ["m.rvt"]},
                to="", projects=["A", "B"])
    check(worst.get("refused") == "NO_DESTINATION",
          "a report wrong four ways is refused for the destination first - "
          "every other rule is a rule about where it is going")
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-RPT-RED-003.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 6, "the contract declares 6 failures")
    for failure in named:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    spare = sorted(reached - set(named))
    check(not spare, "and nothing else was refused%s"
          % ("" if not spare else ": %s" % ", ".join(spare)))

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the line is the file, and nothing was stripped")
    return 0


if __name__ == "__main__":
    sys.exit(main())
