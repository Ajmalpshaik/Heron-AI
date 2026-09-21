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

  8. THE JSON CHECK READS NO MORE THAN IT MUST, AND ANSWERS THE SAME.
     Proved by COUNTING THE OPENS, not by timing: a file too big for the
     sniff whose head cannot begin a JSON value is never opened a second
     time, and one that really does start like JSON still is. Row 5b-88.
"""

import io
import json
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


class CountingIO(object):
    """
    `io`, but it remembers which paths were opened through it.

    Row 5b-88 is a claim about how much gets READ, and a timing test for
    that is a flaky test. This counts instead: dropped in as the module's
    `io`, it makes "it did not open the file again" something a check can
    assert. It delegates everything - the bytes are the real bytes.
    """

    def __init__(self):
        self.opened = []

    def open(self, path, *args, **kwargs):
        self.opened.append(path)
        return io.open(path, *args, **kwargs)


def opens_during_shape(path):
    """(the shape card, how many times `path` was opened to get it)."""
    counter = CountingIO()
    real = CLS.io
    CLS.io = counter
    try:
        card = CLS._shape(path)
    finally:
        CLS.io = real
    return card, counter.opened.count(path)


def parses_the_old_way(path):
    """What _shape used to do: json.load the whole file, every time."""
    try:
        with io.open(path, encoding="utf-8") as handle:
            json.load(handle)
        return True
    except (ValueError, IOError, OSError, UnicodeDecodeError):
        return False


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

        print("\n8. the JSON check reads no more than it must (row 5b-88)")
        # One open is the sniff, which every text file pays and which is
        # bounded. A SECOND open is the whole file being read, and that is
        # the cost row 5b-88 is about. Sizes are just over SNIFF - the
        # claim is about opens, so it holds at 100 KB and at 100 GB alike.
        pad = "the duct run above the corridor ceiling was rerouted. "
        big = CLS.SNIFF * 12
        cases = os.path.join(yard, "sizes")
        os.makedirs(cases)
        filler = (pad * ((big // len(pad)) + 1))[:big]
        write(cases, "small.json", '{"revit": "2024"}')
        write(cases, "small.txt", "Ducts shall be insulated.")
        write(cases, "big-prose.txt", filler)
        write(cases, "big-truthy.txt", "truthy " + filler)
        write(cases, "big-true.txt", "true")
        write(cases, "big.json", '{"pad": "%s"}' % filler)
        write(cases, "big-broken.json", '{"pad": "%s' % filler)

        # name, does it parse, how many opens it may cost
        expected = (("small.json", True, 1),
                    ("small.txt", False, 1),
                    ("big-prose.txt", False, 1),
                    ("big-truthy.txt", False, 1),
                    ("big-true.txt", True, 1),
                    ("big.json", True, 2),
                    ("big-broken.json", False, 2))
        for name, parses, allowed in expected:
            full = os.path.join(cases, name)
            card, opens = opens_during_shape(full)
            check(card["json"] is parses,
                  "%s parses as JSON: %s" % (name, parses))
            check(card["json"] == parses_the_old_way(full),
                  "%s gets the SAME answer as reading the whole file"
                  % name)
            check(opens == allowed,
                  "%s costs %d open(s), and the bound is %d"
                  % (name, opens, allowed))

        # The head test has to be sound, not a guess: every character RFC
        # 8259 lets a JSON value begin with must be in the module's list,
        # or a real JSON file larger than the sniff is dropped unread.
        # getattr, not CLS.JSON_STARTS: a missing name would raise here
        # and the checks below would never run, turning fifteen readable
        # failures into one traceback. Same lesson as row 5b-85.
        starts = getattr(CLS, "JSON_STARTS", "")
        words = getattr(CLS, "JSON_WORDS", ())
        for opener in '{[" -0123456789'.replace(" ", ""):
            check(opener in starts,
                  "a JSON value may begin with %r, and the module knows"
                  % opener)
        check(tuple(sorted(words)) == ("false", "null", "true"),
              "and the three bare literals are matched whole, so `truthy` "
              "is not mistaken for `true`")
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
