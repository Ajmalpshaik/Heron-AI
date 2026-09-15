# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-GIT-VER-008
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Versioning - the promise docs/17 makes, and the one 0.1.0 cannot keep.

    python tests/test_versioning.py

WHAT IT PROVES
  1. THE FOUR PROMISES ARE docs/17 s8's, READ OUT OF THE DOCUMENT. Not
     four sentences that look like it - the actual table, matched
     against the file on disk.

  2. AT 0.y.z THE MAJOR BUMP IS NOT AVAILABLE, and the answer says the
     promise is NOT kept rather than returning a correct-looking number.
     Above 0 the same input gives the major.

  3. THE ONE COMPONENT WITH NO STATED VERSION RULE GETS NO NUMBER. Not a
     patch, not a minor - none, because inventing one would be writing
     the promise rather than keeping it.

  4. THE HALF OF A PROMISE NO NUMBER CARRIES IS ASKED FOR, NOT ASSUMED.
     Both of them, with their own question.

  5. THE STEP FOLLOWS THE CHANGE: breaking beats added beats fixed, and
     a fix alone never moves the minor.

  6. IT DECIDES NOTHING ABOUT WHAT BROKE, AND TAGS NOTHING.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_versioning as VER                                 # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_versioning.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]
    doc = io.open(os.path.join(ROOT, "docs",
                               "17-open-source-and-distribution.md"),
                  encoding="utf-8").read().lower()

    print("\n1. the four promises are read out of docs/17 s8")
    check(len(VER.PROMISES) == 4, "there are four, no more and no fewer")
    for name in sorted(VER.PROMISES):
        one = VER.PROMISES[name]
        # THE ACTUAL TABLE ROW, matched against the file on disk.
        row = "| %s | %s |" % (name, one["promise"])
        check(row in doc, "docs/17 s8 carries the row %s" % row)

    print("\n2. at 0.y.z the major bump is not available to spend")
    young = VER.version("agent contracts", "0.1.0",
                        breaking=["a required field was added"])
    check(young["wanted"] == "major" and young["step"] == "minor",
          "the promise asks for a major and the minor is what moves")
    check(young["to"] == "0.2.0", "0.1.0 becomes 0.2.0")
    check(young["kept"] is False,
          "and `kept` is FALSE - the answer says the promise is not kept "
          "by this number")
    check("no way to know a contract broke" in young["unjudged"][0],
          "with the reason: a reader seeing 0.2.0 cannot tell that "
          "anything broke")
    grown = VER.version("agent contracts", "1.4.2",
                        breaking=["a required field was added"])
    check(grown["to"] == "2.0.0" and grown["step"] == "major"
          and grown["kept"] is True,
          "the SAME input above 0 gives 2.0.0, major, and the promise IS "
          "kept - so the flag tracks the version, not the change")
    check(VER.version("agent contracts", "0.9.0",
                      added=["a field"])["kept"] is True,
          "and an addition at 0.9.0 keeps its promise - only the major "
          "one is unavailable")

    print("\n3. the component with no version rule gets no number")
    schema = VER.version("fragment metadata schema", "0.1.0",
                         breaking=["`revit` became a list"],
                         evidence="tools/migrate-fragment-revit.py")
    check(schema["versioned"] is True, "it is answered, not refused")
    check(schema["to"] is None and schema["step"] is None,
          "and NO number comes back - not a patch, not a minor")
    check("inventing one would be writing the promise" in schema["why"],
          "because docs/17 s8 promises work here, not a number")
    check(VER.PROMISES["fragment metadata schema"]["on_breaking"] is None,
          "the table itself carries None rather than a default")

    print("\n4. the half no number carries is asked for, never assumed")
    silent = VER.version("supported revit versions", "0.1.0",
                         breaking=["2020"])
    reached.add(silent.get("refused"))
    check(silent["refused"] == "NOT_ANNOUNCED",
          "dropping a Revit release without an announcement is refused")
    check(silent.get("asked"), "and the question comes back: %r"
                              % silent["asked"])
    nothing = VER.version("fragment metadata schema", "0.1.0",
                          breaking=["`revit` became a list"])
    reached.add(nothing.get("refused"))
    check(nothing["refused"] == "NO_MIGRATION",
          "and a schema change with no migration is refused too")
    check(nothing["asked"] != silent["asked"],
          "with a DIFFERENT question - an announcement and a migration "
          "are different work, not one obligation twice")
    check("ALWAYS" in nothing["why"],
          "citing docs/17's universal: a migration is provided, always - "
          "so it needs no threshold to enforce")
    told = VER.version("supported revit versions", "0.1.0",
                       breaking=["2020"], evidence="docs/07 release note")
    check(told["versioned"] and told["evidence"] == "docs/07 release note",
          "handed one in, it goes through and carries it back as given")
    check(VER.version("agent contracts", "0.1.0",
                      breaking=["x"])["versioned"] is True,
          "a component owing nothing is not asked for evidence it does "
          "not owe")

    print("\n5. the step follows the change")
    check(VER.version("mcp tool contracts", "1.0.0",
                      added=["a tool"])["to"] == "1.1.0",
          "an addition moves the minor")
    check(VER.version("mcp tool contracts", "1.0.0",
                      fixed=["a typo"])["to"] == "1.0.1",
          "a fix moves the patch")
    both = VER.version("mcp tool contracts", "1.0.0",
                       breaking=["a field went"], added=["a tool"],
                       fixed=["a typo"])
    check(both["to"] == "2.0.0",
          "and breaking beats added beats fixed - all three together "
          "still give the major")
    check(VER.version("mcp tool contracts", "1.2.3",
                      fixed=["x"])["to"] == "1.2.4",
          "a patch off 1.2.3 is 1.2.4, and nothing else moves")
    check(VER.version("mcp tool contracts", "1.2.3",
                      added=["x"])["to"] == "1.3.0",
          "a minor off 1.2.3 is 1.3.0 - the patch resets")

    print("\n6. it decides nothing about what broke, and tags nothing")
    check(any("not decided here" in line.lower()
              for line in young["unjudged"]),
          "the answer says the breakage came in as a finding")
    # AND IT DOES NOT READ WHAT THE LINES SAY. A word search here would
    # only find docs/17's own word "removal", so the proof is
    # behavioural: change the CONTENT of the breakage and nothing moves.
    plain = VER.version("mcp tool contracts", "1.0.0", breaking=["banana"])
    spelt = VER.version("mcp tool contracts", "1.0.0",
                        breaking=["a required field was removed"])
    check(plain["to"] == spelt["to"] == "2.0.0"
          and plain["step"] == spelt["step"],
          "'banana' and 'a required field was removed' give the identical "
          "answer - the agent counts findings, it does not read them")
    check(len(VER.version("mcp tool contracts", "1.0.0",
                          breaking=["one", "two", "three"])["breaking"]) == 3,
          "and it carries them back unchanged, all three of them")
    # NOTHING IT IMPORTS COULD REACH A REPOSITORY.
    imports = sorted(line.split()[1] for line in logic.split("\n")
                     if line.startswith("import "))
    check(imports == ["os", "re", "sys"],
          "the whole import list is os, re, sys - nothing that could run "
          "git, open a socket or write a file: %s" % ", ".join(imports))

    print("\n7. every declared failure is named and reached")
    for args, kwargs, name in (
            (("nothing promised", "0.1.0"), {"fixed": ["x"]},
             "NOT_A_COMPONENT"),
            (("agent contracts", "one point oh"), {"fixed": ["x"]},
             "NOT_A_VERSION"),
            (("agent contracts", "0.1.0"), {}, "NOTHING_TO_VERSION"),
            (("agent contracts", "0.1.0"), {"fixed": [None]},
             "NOT_A_CHANGE"),
            (("agent contracts", "0.1.0"), {"added": ["  "]},
             "NOT_A_CHANGE")):
        answer = VER.version(*args, **kwargs)
        reached.add(answer.get("refused"))
        check(answer.get("refused") == name, "%s is reached" % name)

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-GIT-VER-008.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 6, "the contract declares 6 failures")
    for failure in named:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    check(len(young["unjudged"]) == 4, "four things are left unjudged")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    what a number promises, and what 0.1.0 cannot")
    return 0


if __name__ == "__main__":
    sys.exit(main())
