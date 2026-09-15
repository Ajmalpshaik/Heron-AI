# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-WSP-REP-004
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test

"""
Folder repair - dry-run by default, and never across a class boundary.

    python tests/test_repair.py

WHAT IT PROVES
  1. DRY-RUN IS IN THE SIGNATURE, not in a setting - and `apply=True`
     still moves nothing. The module touches no disk at all.

  2. A MOVE THAT CHANGES CLASS IS REFUSED, in every direction, with what
     that particular crossing would actually cost.

  3. A MOVE INSIDE ONE CLASS IS PROPOSED - so the refusals above are the
     rule and not a broken function.

  4. A TAKEN DESTINATION IS REFUSED, never overwritten.

  5. WHERE A FILE BELONGS IS TAKEN AS GIVEN - the module never re-derives
     the layout.

  6. THE CLASS ANSWER IS HERON-WSP-PTH-007's OWN.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE AND REACHED.
"""

import inspect
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_repair as REP                                     # noqa: E402
import heron_paths as PATHS                                    # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    source = open(os.path.join(ROOT, "brain", "heron_repair.py"),
                  encoding="utf-8").read()

    def ask(findings, **kw):
        answer = REP.plan(findings, **kw)
        if answer.get("refused"):
            reached.add(answer["refused"])
        for entry in (answer.get("refused_moves") or []):
            reached.add(entry["refused"])
        return answer

    inside = [{"path": "brain/a.py", "belongs": "brain/tools/a.py"}]

    print("1. Dry-run is in the signature")
    check(inspect.signature(REP.plan).parameters["apply"].default is False,
          "`apply` defaults to False in the signature itself")
    check(ask(inside)["applied"] is False, "a plain run applies nothing")
    check(ask(inside, apply=True)["applied"] is False,
          "and apply=True applies nothing either")
    check("moved nothing" in ask(inside, apply=True)["why"],
          "saying so in the sentence")
    for word in ("os.rename", "shutil", "os.remove", "os.makedirs",
                 "open(", "subprocess", "os.environ", "getenv"):
        check(word not in source,
              "the source has no %s - no disk, and no setting that could "
              "flip the default" % word)
    check(any("does not change that" in note
              for note in ask(inside)["unjudged"]),
          "and the answer says apply does not change it")

    print()
    print("2. A move that changes class is refused, in every direction")
    crossings = [
        ("Fragments/x.yaml", "Core/x.yaml", "data", "product",
         "modeller's work"),
        ("Core/x.dll", "Fragments/x.dll", "product", "data",
         "old code that looks current"),
        ("Core/x.dll", "Cache/x.dll", "product", "derived",
         "cleanup deletes derived"),
        ("Cache/x.db", "Brain/x.db", "derived", "data",
         "come back beside the copy"),
        ("Documentation/x.md", "Core/x.md", "unknown", "product",
         "UNKNOWN"),
    ]
    for path, to, was, goes, says in crossings:
        answer = ask([{"path": path, "belongs": to}])
        entry = answer["refused_moves"][0]
        check(entry["refused"] == "WOULD_CROSS_A_CLASS_BOUNDARY",
              "%s -> %s is refused" % (was, goes))
        check(entry["from_class"] == was and entry["to_class"] == goes,
              "  naming both classes")
        check(says.lower() in entry["why"].lower(),
              "  and what THAT crossing costs: %s" % says)
    check(not ask([{"path": "Fragments/x", "belongs": "Core/x"}])["moves"],
          "and a refused crossing is not also proposed")

    print()
    print("3. A move inside one class is proposed")
    for path, to in (("brain/a.py", "brain/tools/a.py"),
                     ("Core/a.dll", "Core/lib/a.dll"),
                     ("Cache/a.db", "Cache/old/a.db"),
                     ("who/knows.bin", "who/else/knows.bin")):
        answer = ask([{"path": path, "belongs": to}])
        check(len(answer["moves"]) == 1 and not answer["refused_moves"],
              "%s -> %s is proposed" % (path, to))
    answer = ask([{"path": "Core/a.dll", "belongs": "Core/lib/a.dll"}])
    check(answer["moves"][0]["class"] == PATHS.PRODUCT,
          "carrying the class it stays in")
    check("placement and nothing else" in answer["moves"][0]["why"],
          "and saying that is all it is")

    print()
    print("4. A taken destination is refused")
    answer = ask(inside, exists=["brain/tools/a.py"])
    check(answer["refused_moves"][0]["refused"] == "DESTINATION_TAKEN",
          "a destination in the list is refused")
    check("destroys the evidence" in answer["refused_moves"][0]["why"],
          "saying overwriting destroys what is needed to decide")
    check(ask(inside, exists=lambda p: p.endswith("a.py"))["refused_moves"],
          "and a READER is accepted in place of a list")
    check(not ask(inside, exists=lambda p: False)["refused_moves"],
          "while a reader saying no lets it through")
    check(any("stale listing" in note for note in ask(inside)["unjudged"]),
          "and the answer says what a stale answer would cost")

    print()
    print("5. Where a file belongs is taken as given")
    check("belongs" in source and "PARTS" not in source,
          "the module reads `belongs` and carries no layout table")
    check(any("VAL-003" in note for note in ask(inside)["unjudged"]),
          "and says whose answer it is")
    check(ask([{"path": "brain/a.py", "belongs": "COMPLETE/NONSENSE/a.py"}])
          ["refused_moves"],
          "a nonsense destination is judged on CLASS, not second-guessed "
          "on layout")

    print()
    print("6. The class answer is the path manager's own")
    for path in ("Fragments/x", "Core/x", "Cache/x", "who/knows"):
        answer = ask([{"path": path, "belongs": "Core/y"}])
        entry = (answer["moves"] + answer["refused_moves"])[0]
        expected = PATHS.classify(path)["class"]
        got = entry.get("from_class") or entry.get("class")
        check(got == expected,
              "%s is %s - HERON-WSP-PTH-007's answer" % (path, expected))
    check("PATHS.classify" in source, "it calls classify()")
    limit = [n for n in ask(inside)["unjudged"] if "SOURCE REPOSITORY" in n]
    check(limit, "and the answer is honest that a checkout is not a workspace")
    check("reads as the DATA class" in limit[0],
          "naming exactly how the repo's own brain/ is misread")

    print()
    print("7. Every failure the contract declares is named and reached")
    for empty in ([], None, ""):
        check(ask(empty).get("refused") == "NOTHING_MISPLACED",
              "%r reports nothing misplaced" % (empty,))
    check("a run nobody asked for" in ask([])["why"],
          "and an empty run is not a tidy workspace")
    for bad in ([{"path": "a"}], [{"belongs": "b"}], [{}], ["a string"],
                [{"path": "", "belongs": ""}]):
        check(ask(bad).get("refused") == "NOT_A_FINDING",
              "%r is not a finding" % (bad,))
    check("only where it is" in ask([{"path": "a"}])["why"],
          "and it says which half was missing")
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-WSP-REP-004.yaml"))
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
    print("PASS    dry-run by default, and never across a class boundary")
    return 0


if __name__ == "__main__":
    sys.exit(main())
