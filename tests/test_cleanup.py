# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-WSP-CLN-009
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test

"""
Cleanup - unused is a claim, and archiving is the only removal there is.

    python tests/test_cleanup.py

WHAT IT PROVES
  1. THERE IS NO DELETE. Not in the code, and the four lists are values a
     caller acts on - which keeps "archives, never deletes" checkable from
     outside rather than promised from inside.

  2. UNOBSERVED IS NOT UNUSED, and has its own list. An artefact nothing
     watched is left alone however long it has sat there.

  3. A USAGE RECORD NEEDS A WINDOW. "Used 0 times" with no dates is a
     sentence about the logging.

  4. WHAT MAY GO OUTRIGHT IS HERON-WSP-PTH-007's ANSWER, asked rather than
     restated - and an UNKNOWN artefact is archived, not removed.

  5. ONE USE IS ENOUGH TO KEEP SOMETHING.

  6. EVERY ARTEFACT LANDS IN EXACTLY ONE LIST.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE AND REACHED.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_cleanup as CLN                                    # noqa: E402
import heron_paths as PATHS                                    # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []

WINDOW = {"since": "2026-06-01", "until": "2026-09-14"}


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def used(times):
    return dict(WINDOW, uses=times)


def main():
    reached = set()
    source = open(os.path.join(ROOT, "brain", "heron_cleanup.py"),
                  encoding="utf-8").read()

    def sweep(artefacts, usage=None):
        answer = CLN.sweep(artefacts, usage=usage)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    everything = ["Cache/vectors.db", "Fragments/old", "Fragments/live",
                  "Core/Old.dll", "Memory/notes", "who/knows.bin"]
    usage = {"Cache/vectors.db": used(0), "Fragments/old": used(0),
             "Fragments/live": used(41), "Core/Old.dll": used(0),
             "who/knows.bin": used(0)}
    answer = sweep(everything, usage)

    print("1. There is no delete")
    # The module docstring NAMES these to say they are absent, so the
    # check reads the CODE. A word search over prose would have this
    # module failing its own claim for making it.
    code = source.split('"""', 2)[2]
    for word in ("os.remove", "os.unlink", "unlink", "shutil.rmtree",
                 "rmtree", "os.rmdir", "subprocess", "open(", "delete("):
        check(word not in code, "the code has no %s" % word)
    check("no unlink, no rmtree, no os.remove" in " ".join(source.split()),
          "while the docstring names them, which is why the check reads "
          "the code and not the prose")
    check(sorted(answer) == ["archive", "keep", "remove", "unjudged",
                             "unobserved", "why"],
          "four lists, a sentence and the unjudged - nothing else")
    check(any("checkable from outside" in note
              for note in answer["unjudged"]),
          "and the answer says why that keeps the promise checkable")
    check("Nothing was touched" in answer["why"],
          "with the sentence saying nothing happened")

    print()
    print("2. Unobserved is not unused")
    check([entry["artefact"] for entry in answer["unobserved"]]
          == ["Memory/notes"],
          "the one with no record is unobserved")
    check("Memory/notes" not in [entry["artefact"]
                                 for entry in answer["archive"]],
          "and is NOT in the archive list")
    check("Memory/notes" not in [entry["artefact"]
                                 for entry in answer["remove"]],
          "nor the remove list")
    entry = answer["unobserved"][0]
    check("UNOBSERVED and not unused" in entry["why"],
          "saying which of the two it is")
    check("Absence of evidence is not evidence" in entry["why"],
          "and why that distinction is kept")
    check("once a quarter on the job that matters" in entry["why"],
          "with the case it is protecting")
    check(any("UNOBSERVED IS NOT UNUSED" in note
              for note in answer["unjudged"]),
          "and the report repeats it rather than leaving a list to read")

    print()
    print("3. A usage record needs a window")
    for record, missing in (({"uses": 0}, "since"),
                            ({"since": "a", "uses": 0}, "until"),
                            ({"since": "a", "until": "b"}, "uses"),
                            ({}, "everything"),
                            (None, "no record at all"),
                            ("0 uses", "a sentence, not a record")):
        answer = sweep(["Fragments/x"], {"Fragments/x": record})
        check(answer["unobserved"] and not answer["archive"],
              "%s missing -> unobserved, not archived" % missing)
    answer = sweep(["Fragments/x"], {"Fragments/x": {"uses": 0}})
    check("about the logging, not the artefact"
          in " ".join(answer["unobserved"][0]["wants"]),
          "and it says what a record with no window is actually about")
    check(len(answer["unobserved"][0]["wants"]) == 2,
          "naming both fields that are missing, not just the first")
    check(len(CLN.A_USAGE_RECORD_CARRIES) == 3,
          "three fields are required")

    print()
    print("4. What may go outright is the path manager's answer")
    answer = sweep(everything, usage)
    check([entry["artefact"] for entry in answer["remove"]]
          == ["Cache/vectors.db"],
          "only the derived one may be removed")
    check(sorted(entry["artefact"] for entry in answer["archive"])
          == ["Core/Old.dll", "Fragments/old", "who/knows.bin"],
          "data, product AND the unknown one are archived instead")
    for entry in answer["remove"] + answer["archive"]:
        expected = PATHS.may("cleanup", entry["artefact"])
        check(entry["class"] == expected["class"]
              and entry["why"] == expected["why"],
              "%s carries HERON-WSP-PTH-007's own answer, verbatim"
              % entry["artefact"])
    unknown = [entry for entry in answer["archive"]
               if entry["artefact"] == "who/knows.bin"][0]
    check(unknown["class"] == PATHS.UNKNOWN
          and unknown["read_as"] == PATHS.DATA,
          "and an UNKNOWN artefact is archived because it is read as data")
    check("PATHS.may" in source, "the rule is asked, not restated")
    check("derived" not in source.split("def sweep")[1].split("return {")[0],
          "and sweep() never decides the class itself")

    print()
    print("5. One use is enough to keep something")
    check([entry["artefact"] for entry in answer["keep"]]
          == ["Fragments/live"], "the used one is kept")
    check(answer["keep"][0]["uses"] == 41, "with its count")
    check("2026-06-01" in answer["keep"][0]["why"],
          "and the window it was used in")
    one = sweep(["Fragments/x"], {"Fragments/x": used(1)})
    check(one["keep"] and not one["archive"],
          "a single use keeps it - there is no threshold to tune")
    check(any("a number somebody invented" in note
              for note in one["unjudged"]),
          "and the answer says why there is no window default either")

    print()
    print("6. Every artefact lands in exactly one list")
    landed = ([entry["artefact"] for entry in answer["remove"]]
              + [entry["artefact"] for entry in answer["archive"]]
              + [entry["artefact"] for entry in answer["unobserved"]]
              + [entry["artefact"] for entry in answer["keep"]])
    check(sorted(landed) == sorted(everything),
          "all %d, none twice and none missing" % len(everything))
    check(len(landed) == len(set(landed)), "and none in two lists")

    print()
    print("7. Every failure the contract declares is named and reached")
    for empty in ([], None, ""):
        check(sweep(empty).get("refused") == "NOTHING_TO_SWEEP",
              "%r offers nothing" % (empty,))
    check("a run nobody asked for" in sweep([])["why"],
          "and an empty sweep is not a tidy workspace")
    for bad in (["", "x"], ["   "], [None]):
        check(sweep(bad).get("refused") == "NOT_AN_ARTEFACT",
              "%r names nothing" % (bad,))
    for count in ("lots", None, [], {"n": 1}):
        answer = sweep(["x"], {"x": dict(WINDOW, uses=count)})
        if count is None:
            check(answer["unobserved"],
                  "a missing count is unobserved, not unreadable")
            continue
        check(answer.get("refused") == "NOT_A_USAGE_RECORD",
              "%r uses is not a count" % (count,))
    check(sweep(["x"], {"x": dict(WINDOW, uses="lots")})["artefact"] == "x",
          "and the refusal names which artefact")
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-WSP-CLN-009.yaml"))
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
    print("PASS    unused is a claim, and archiving is the only removal")
    return 0


if __name__ == "__main__":
    sys.exit(main())
