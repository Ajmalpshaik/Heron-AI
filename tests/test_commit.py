# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-GIT-CMT-004
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Commits - the generated list is read, and the wording is nobody's here.

    python tests/test_commit.py

WHAT IT PROVES
  1. THE GENERATED LIST IS READ OUT OF .gitignore's OWN BLOCK, and it is
     what that file says - matched against the file. A .gitignore
     without the block REFUSES rather than falling back to a guess.

  2. EACH GENERATED PAGE IS REFUSED, by full path and by bare name, and
     an ordinary .html is not.

  3. A REVIT MODEL IS REFUSED BY ITS NAME ALONE, for every extension on
     heron_release's list - which is that module's object.

  4. NO MESSAGE SHAPE IS ENFORCED. A one-word message, a rambling one
     and one that breaks every convention anybody might adopt all pass -
     because no convention has been adopted, and one enforced here would
     become it.

  5. THE MESSAGE IS READ FOR A CREDENTIAL, refused whole, and the value
     is nowhere in the answer.

  6. NOTHING IS STAGED AND NO FILE IS OPENED except .gitignore.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_commit as CMT                                     # noqa: E402
import heron_release as RELEASE                                # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []

TOKEN = "ghp_" + "A" * 36


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_commit.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]
    ignored = io.open(CMT.IGNORE, encoding="utf-8").read()

    print("\n1. the generated list is read out of .gitignore")
    pages = CMT.generated()
    check(pages, "it found %d: %s" % (len(pages), ", ".join(pages)))
    for one in pages:
        check("\n%s\n" % one in ignored,
              "'%s' is a line in .gitignore" % one)
    check("skill-catalog.html" in pages and "api-reference.html" in pages,
          "including the two added this session, with no edit to the agent")
    # A LIST INVENTED HERE would check a commit against this file's
    # opinion rather than against what the repository says.
    where = tempfile.mkdtemp()
    try:
        empty = os.path.join(where, ".gitignore")
        io.open(empty, "w", encoding="utf-8").write("*.pyc\n")
        check(CMT.generated(empty) == [],
              "a .gitignore without the block yields no list")
        blind = CMT.stage(["brain/x.py"], "x", ignore=empty)
        reached.add(blind.get("refused"))
        check(blind["refused"] == "NO_IGNORE_LIST",
              "and staging is REFUSED rather than checked against a guess")
        check(CMT.generated("/nowhere/.gitignore") == [],
              "so is an unreadable one")
    finally:
        shutil.rmtree(where)

    print("\n2. each generated page is refused")
    for one in pages:
        answer = CMT.stage([one], "a message")
        reached.add(answer.get("refused"))
        check(answer.get("refused") == "CARRIES_A_GENERATED_PAGE",
              "'%s' is refused" % one)
    deep = CMT.stage(["some/where/skill-catalog.html"], "a message")
    check(deep.get("refused") == "CARRIES_A_GENERATED_PAGE",
          "and by its bare name wherever it sits")
    check(CMT.stage(["docs/hand-written.html"], "a message")["prepared"],
          "while an ordinary .html stages fine - it is the LIST that "
          "decides, not the extension")
    named = CMT.stage(["agent-map.html"], "a message")
    check(named["paths"] == ["agent-map.html"]
          and named["generated"] == pages,
          "and the answer shows what it was checked against")

    print("\n3. a Revit model is refused by its name alone")
    check(CMT.NEVER_LEAVES is RELEASE.NEVER_LEAVES,
          "the extension list is heron_release's object, not a copy")
    for extension in CMT.NEVER_LEAVES:
        answer = CMT.stage(["models/Tower A%s" % extension], "a message")
        reached.add(answer.get("refused"))
        check(answer.get("refused") == "CARRIES_A_MODEL",
              "%s is refused" % extension)
    check("stays in every clone" in
          CMT.stage(["a.rvt"], "m")["why"],
          "with the reason a commit is different: it stays in every clone "
          "until somebody rewrites it for everybody")

    print("\n4. no message shape is enforced")
    for message in ("wip", "fix", "asdf",
                    "FEAT(scope)!: a conventional commit",
                    "a very long message " * 20,
                    "no verb no capital no full stop"):
        check(CMT.stage(["brain/x.py"], message)["prepared"] is True,
              "'%s' passes" % (message[:32].strip() + ("..." if
                                                       len(message) > 32
                                                       else "")))
    check(any("would become it" in line for line in
              CMT.stage(["brain/x.py"], "wip")["unjudged"]),
          "and the answer says why: no convention is adopted, and one "
          "enforced here would become it")
    blank = CMT.stage(["brain/x.py"], "   ")
    reached.add(blank.get("refused"))
    check(blank["refused"] == "NO_MESSAGE" and blank.get("asked"),
          "an ABSENT message is still refused, and asked for - that is "
          "not a shape question")

    print("\n5. the message is read for a credential")
    leaky = CMT.stage(["brain/x.py"], "the token is %s" % TOKEN)
    reached.add(leaky.get("refused"))
    check(leaky["refused"] == "CARRIES_A_SECRET",
          "a credential in the message is refused")
    check(TOKEN not in repr(leaky),
          "and the value is nowhere in the answer")
    check(leaky["found"], "only the kind: %s" % ", ".join(leaky["found"]))
    check("every clone and every mirror at once" in leaky["why"],
          "with the reason a commit message is worse than a report")

    print("\n6. nothing is staged and no file is opened but .gitignore")
    check(logic.count("io.open(") == 1,
          "there is exactly one open in the module, and it is the "
          ".gitignore read")
    for reaching in ("subprocess", "socket", "urllib", "write("):
        check(reaching not in logic, "nothing here uses %s" % reaching)
    good = CMT.stage(["brain/heron_commit.py", "tests/test_commit.py"],
                     "GitHub: the Commit Agent")
    check("Nothing was staged" in good["why"], "and the answer says so")
    check(any("nothing here opened one" in line
              for line in good["unjudged"]),
          "including that only the file NAMES were checked")

    print("\n7. every declared failure is named and reached")
    for paths, message, name in (
            ([], "x", "NOTHING_TO_STAGE"),
            ([None], "x", "NOT_A_PATH"),
            (["  "], "x", "NOT_A_PATH"),
            (["/etc/passwd"], "x", "OUTSIDE_THE_REPOSITORY"),
            (["../elsewhere"], "x", "OUTSIDE_THE_REPOSITORY")):
        answer = CMT.stage(paths, message)
        reached.add(answer.get("refused"))
        check(answer.get("refused") == name, "%s is reached" % name)

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-GIT-CMT-004.yaml"))
    named_failures = contract.get("failures") or []
    check(len(named_failures) == 8, "the contract declares 8 failures")
    for failure in named_failures:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named_failures) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    check(len(good["unjudged"]) == 4, "four things are left unjudged")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the generated list is read, the wording is nobody's here")
    return 0


if __name__ == "__main__":
    sys.exit(main())
