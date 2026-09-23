# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-OPS-HEA-006
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Self-Healing - the line is a named rebuilder, not a harmless-looking name.

    python tests/test_healing.py

WHAT IT PROVES
  1. EVERY ENTRY IN THE DERIVED REGISTER IS REAL. Each names a generator
     this repository actually has, and a source that actually exists - a
     register of rebuilders that do not exist is worse than none.

  2. IT FAILS CLOSED ON ANYTHING ELSE. A path that looks exactly like a
     cache is refused, because a name is not evidence that something can
     rebuild it and a wrong guess here is silent.

  3. A SOURCE OF TRUTH IS REFUSED BY NAME, saying what kind of thing it is
     - fragments, decisions, proof drafts, a Revit model. So is each file of
     a register that was split into files: one decision, one group of
     NEEDS-CHECKING, one group of its done checks (row 5b-172), one section
     of the open questions.

  4. A REBUILDER WITH NO SOURCE REBUILDS NOTHING. Running a generator
     against a missing source produces an EMPTY artefact that looks current,
     and the refusal names the source rather than the symptom.

  5. NOTHING IS EXECUTED AND NOTHING IS DELETED. The command comes back as
     text. An agent with MODIFY that also spawned processes would be
     building the containment D-84 declined to build.

  6. TRIAGE RETURNS TWO LISTS, so a caller never gets one it must sort.

  7. THE SPLIT IS EXHAUSTIVE - every artefact lands in exactly one list.

  8. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE AND REACHED.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_healing as HEA                                   # noqa: E402
import heron_contract as CON                                  # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    source = open(os.path.join(ROOT, "brain", "heron_healing.py"),
                  encoding="utf-8").read()

    def ask(artefact, **kw):
        answer = HEA.repair(artefact, **kw)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    print("1. Every entry in the derived register is real")
    check(HEA.DERIVED, "the register is not empty")
    for artefact, entry in sorted(HEA.DERIVED.items()):
        script = entry["rebuilt_by"].split()[-1]
        check(os.path.exists(os.path.join(ROOT, script)),
              "%s names %s, which exists" % (artefact, script))
        for origin in entry["from"]:
            check(os.path.exists(os.path.join(ROOT, origin.split("#")[0])),
                  "and is derived from %s, which exists" % origin)
    # THE COUNTS ARE DERIVED AND THE ROWS ARE NOT, and the register says so.
    counts = HEA.DERIVED["docs/28-agent-registry.md#counts"]
    check("ROWS are not" in counts.get("note", ""),
          "the register entry for docs/28 says the COUNTS are derived and "
          "the rows a person wrote are not")

    print()
    print("2. It fails closed on anything else")
    for looks_safe in ("some/cache/file.tmp", "build/output.o",
                       ".cache/index.db", "tmp/scratch", "generated/thing"):
        answer = ask(looks_safe)
        check(answer.get("refused") == "NOT_KNOWN_TO_BE_DERIVED",
              "'%s' is refused despite the name" % looks_safe)
    answer = ask("some/cache/file.tmp")
    check("not evidence" in answer["why"],
          "and the refusal says a name is not evidence")
    check("add it to heron_healing.DERIVED" in answer["proposal"],
          "with the one thing that would change the answer")
    check("rebuilt FROM" in answer["proposal"],
          "and that BOTH halves are needed, not just the generator")

    print()
    print("3. A source of truth is refused by name")
    for path, word in (("brain/fragments/x.yaml", "fragment"),
                       ("docs/DECISIONS.md", "decision"),
                       ("docs/OPEN-QUESTIONS.md", "open question"),
                       ("docs/NEEDS-CHECKING.md", "proved"),
                       ("brain/agents/X.yaml", "promise"),
                       ("brain/proof-drafts/runs/a.json", "evidence"),
                       ("Project1.rvt", "Revit model"),
                       ("Duct.rfa", "Revit family")):
        answer = ask(path)
        check(answer.get("refused") == "NOT_KNOWN_TO_BE_DERIVED"
              and word in answer["why"],
              "%s is refused as %s" % (path, word))
    check("under any circumstances" in ask("a.rvt")["why"],
          "and the model is refused in those words")
    # A REGISTER SPLIT INTO FILES IS STILL REFUSED BY NAME. The generic
    # refusal names the path, and "docs/decisions/" alone would satisfy a
    # check for "decision" - so each check also asks for the sentence only a
    # named source of truth gets. Row 5b-172.
    for path, word in (("docs/decisions/D-30.md", "chosen"),
                       ("docs/needs-checking/group-j.md", "proved"),
                       ("docs/needs-checking-archive/group-a.md", "done checks"),
                       ("docs/open-questions/tier-3.md", "open questions")):
        answer = ask(path)
        check(answer.get("refused") == "NOT_KNOWN_TO_BE_DERIVED"
              and "Nothing regenerates it" in answer["why"]
              and word in answer["why"],
              "%s, one file of a split register, is refused as %s"
              % (path, word))
    answer = ask("docs/needs-checking-archive/README.md")
    check("Nothing regenerates it" not in answer["why"],
          "and the archive's README, which its tool rewrites, is not called "
          "the register's done checks")

    print()
    print("4. A rebuilder with no source rebuilds nothing")
    answer = ask("docs/agent-map.html", root="/nowhere-at-all")
    check(answer.get("refused") == "SOURCE_IS_MISSING",
          "a missing source refuses the repair")
    check("EMPTY" in answer["why"],
          "and says the generator would produce an empty file")
    check("looks current" in answer["why"],
          "which is worse than the stale one, because it looks current")
    check("docs/28-agent-registry.md" in answer["proposal"],
          "and the proposal names the SOURCE, not the symptom")
    answer = ask("docs/agent-map.html")
    check(answer.get("repaired") is True,
          "with the source present it is repairable")

    print()
    print("5. Nothing is executed and nothing is deleted")
    for word in ("subprocess", "os.system", "exec(", "eval(", "os.remove",
                 "shutil.rmtree", "unlink", "os.rmdir", "open("):
        check(word not in source, "the source has no %s" % word)
    answer = ask("docs/agent-map.html")
    check(answer["command"].startswith("python tools/"),
          "the command comes back as text")
    check("returned rather than run" in answer["why"],
          "and says it was not run")
    check("D-84" in answer["why"],
          "citing the decision that makes running it somebody else's, "
          "rather than a question answered on 2026-09-20 (row 5b-56)")

    print()
    print("6. Triage returns two lists")
    answer = HEA.triage(["docs/agent-map.html", "docs/DECISIONS.md"])
    check(sorted(answer) == ["proposed", "repairable", "why"],
          "two lists and a sentence")
    check([e["artefact"] for e in answer["repairable"]]
          == ["docs/agent-map.html"],
          "the derived one is repairable")
    check([e["artefact"] for e in answer["proposed"]] == ["docs/DECISIONS.md"],
          "and the source of truth is proposed")
    empty = HEA.triage([])
    check(empty.get("refused") == "NOTHING_TO_HEAL",
          "nothing reported broken is refused, not reported as healthy")
    reached.add("NOTHING_TO_HEAL")

    print()
    print("7. The split is exhaustive")
    everything = (sorted(HEA.DERIVED)
                  + ["docs/DECISIONS.md", "a.rvt", "who/knows.bin"])
    answer = HEA.triage(everything)
    landed = ([e["artefact"] for e in answer["repairable"]]
              + [e["artefact"] for e in answer["proposed"]])
    check(sorted(landed) == sorted(everything),
          "every artefact lands in exactly one list, none in both or neither")
    check(len(answer["repairable"]) == len(HEA.DERIVED),
          "and the repairable ones are exactly the derived register")

    print()
    print("8. Every failure the contract declares is named and reached")
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-OPS-HEA-006.yaml"))
    named = contract.get("failures") or []
    for failure in named:
        check(failure in source, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    it repairs what something can rebuild, and nothing else")
    return 0


if __name__ == "__main__":
    sys.exit(main())
