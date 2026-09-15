# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-IMP-CLS-003
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Content classification - it asks once per kind, and classifies nothing.

    python tests/test_classify.py

WHAT IT PROVES
  1. IT CLASSIFIES NOTHING. Four thousand files of one kind produce ONE
     question and ZERO categories, and no output field holds one until
     an answer comes back.

  2. THE QUESTION COUNT FOLLOWS KINDS, NOT FILES - shown by adding 500
     more files of a kind already seen and watching the count stay put.

  3. THE EVIDENCE IS FACTS ABOUT THE BYTES. A `.txt` holding C# and a
     `.txt` holding prose are the same group, because nothing here reads
     what the words mean - and a `.py` that is really binary is not.

  4. IT RUNS ON HERON-IMP-FIL-002's REAL ANSWER, walked off a real
     folder, and shares that agent's extension rule by identity.

  5. AN INVENTED CATEGORY IS REFUSED, and the five are docs/28's.

  6. AN UNANSWERED GROUP IS NAMED, NOT DROPPED.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_classify as CLS                                   # noqa: E402
import heron_walk as WALK                                      # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def write(where, at, text, binary=False):
    full = os.path.join(where, *at.split("/"))
    folder = os.path.dirname(full)
    if folder and not os.path.isdir(folder):
        os.makedirs(folder)
    mode, data = ("wb", text) if binary else ("w", text)
    with io.open(full, mode, **({} if binary else {"encoding": "utf-8"})) as h:
        h.write(data)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_classify.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]

    yard = tempfile.mkdtemp(prefix="heron-classify-test-")
    try:
        where = os.path.join(yard, "AJ-Tools")
        os.makedirs(where)
        for n in range(400):
            write(where, "tools/Tool%03d.py" % n, "# code\npass\n")
        write(where, "README.md", "# AJ-Tools")
        write(where, "settings.json", '{"revit": "2024"}')
        write(where, "logo.png", b"\x89PNG\r\n\x1a\n\x00\x00", binary=True)

        walked = WALK.walk(where)
        answer = CLS.brief(walked)

        print("\n1. it classifies nothing")
        check(answer["briefed"] is True, "the walk was read")
        check(answer["classified"] == [],
              "`classified` is empty before any answer came back")
        for card in answer["groups"]:
            check("category" not in card,
                  "no group carries a category: %s" % card["group"])
        check(all(len(ask["categories"]) == len(CLS.CATEGORIES)
                  for ask in answer["asks"]),
              "every question offers all %d categories" % len(CLS.CATEGORIES))

        print("\n2. the question count follows kinds, not files")
        check(answer["of"] == 403,
              "403 files walked (%d)" % answer["of"])
        check(len(answer["asks"]) == 4,
              "and 4 questions, not 403: %s"
              % ", ".join(card["group"] for card in answer["groups"]))
        by_group = dict((card["group"], card) for card in answer["groups"])
        python = [g for g in by_group if g.startswith(".py/")][0]
        check(by_group[python]["of"] == 400,
              "the .py group holds all 400 in one question")
        for n in range(400, 900):
            write(where, "tools/More%03d.py" % n, "# code\npass\n")
        bigger = CLS.brief(WALK.walk(where))
        check(bigger["of"] == 903, "903 files after adding 500 more")
        check(len(bigger["asks"]) == len(answer["asks"]),
              "and STILL %d questions - the cost follows kinds"
              % len(answer["asks"]))

        print("\n3. the evidence is facts about the bytes")
        other = os.path.join(yard, "mixed")
        os.makedirs(other)
        write(other, "prose.txt", "Ducts shall be insulated to 30mm.")
        # DELIBERATELY NOT THE REVIT NAMESPACE. check-structure.py refuses
        # that string anywhere outside revit/, and it is right to - the
        # fixture only needs a .txt that holds CODE, not Revit code.
        write(other, "sneaky.txt", "using System.Collections; // it is C#")
        write(other, "notreally.py", b"\x00\x01\x02binary", binary=True)
        mixed = CLS.brief(WALK.walk(other))
        groups = sorted(card["group"] for card in mixed["groups"])
        check(len([g for g in groups if g.startswith(".txt/")]) == 1,
              "prose and C# in two .txt files are ONE group - nothing here "
              "reads what the words mean")
        check(any(g == ".py/binary/plain" for g in groups),
              "but a .py that is really binary is its own group: %s"
              % ", ".join(groups))
        json_group = [card for card in answer["groups"]
                      if card["content"] == "json"]
        check(len(json_group) == 1 and json_group[0]["extension"] == ".json",
              "a file that parses as JSON is marked as such")
        check(any("parses as JSON" in line
                  for line in json_group[0]["evidence"]),
              "and the evidence says so in words the host will read")
        binary = [card for card in answer["groups"]
                  if card["shape"] == "binary"]
        check(len(binary) == 1 and binary[0]["extension"] == ".png",
              "the PNG is binary, by a NUL in the first bytes")

        print("\n4. it runs on FIL-002's real answer")
        check(CLS.extension_of is WALK.extension_of,
              "CLS.extension_of IS WALK.extension_of - one suffix rule")
        check(walked["walked"] is True and walked["root"] == os.path.abspath(where),
              "the walk really walked %s" % where)
        check(sum(card["of"] for card in answer["groups"])
              + len(answer["unreadable"]) == answer["of"],
              "every walked file is in a group or named unreadable")

        print("\n5. an invented category is refused")
        good = dict((card["group"], "code") for card in answer["groups"])
        for made_up in ("spreadsheet", "", "CODE ", "assets"):
            said = CLS.accept(answer, dict(good, **{python: made_up}))
            if made_up.strip().lower() in CLS.CATEGORIES:
                check(said.get("accepted") is True,
                      "%r is accepted - case and spacing are forgiven"
                      % made_up)
                continue
            reached.add(said.get("refused"))
            check(said.get("refused") == "NOT_A_CATEGORY",
                  "%r is refused" % made_up)
        back = CLS.accept(answer, good)
        check(back["accepted"] is True and len(back["classified"]) == 4,
              "and all four groups are accepted when every answer is real")
        check(back["files"] == answer["of"],
              "covering every walked file (%d)" % back["files"])

        print("\n6. an unanswered group is named, not dropped")
        short = dict(good)
        del short[python]
        partial = CLS.accept(answer, short)
        check(len(partial["unclassified"]) == 1,
              "the missing group comes back unclassified")
        check(partial["unclassified"][0]["group"] == python,
              "and it is the one that was left out")
        check(partial["files"] == answer["of"] - 400,
              "its 400 files are NOT counted as classified (%d)"
              % partial["files"])

        print("\n7. every failure is named and reached")
        for these, name in ((None, "NOTHING_TO_CLASSIFY"),
                            ({"walked": False, "refused": "NOT_A_FOLDER"},
                             "NOT_A_WALK"),
                            ("a string", "NOT_A_WALK"),
                            ({"walked": True, "root": where, "files": []},
                             "NOTHING_TO_CLASSIFY")):
            said = CLS.brief(these)
            reached.add(said.get("refused"))
            check(said.get("refused") == name, "%s is reached" % name)
        for these, name in ((None, "NOTHING_TO_CLASSIFY"),
                            ({"briefed": False}, "NOTHING_TO_CLASSIFY")):
            said = CLS.accept(these, {})
            reached.add(said.get("refused"))
            check(said.get("refused") == name, "%s is reached from accept"
                  % name)
        said = CLS.accept(answer, "a string")
        reached.add(said.get("refused"))
        check(said.get("refused") == "NOT_AN_ANSWER", "NOT_AN_ANSWER is reached")

        contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                         "HERON-IMP-CLS-003.yaml"))
        named = contract.get("failures") or []
        check(len(named) == 4, "the contract declares 4 failures")
        for failure in named:
            check(failure in logic, "the code names %s" % failure)
        unreached = sorted(set(named) - reached)
        check(not unreached,
              "and every one was reached above%s"
              % ("" if not unreached else ": %s" % ", ".join(unreached)))
        check(len(answer["unjudged"]) == 5, "five things are left unjudged")
    finally:
        shutil.rmtree(yard, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    it asks once per kind, and classifies nothing")
    return 0


if __name__ == "__main__":
    sys.exit(main())
