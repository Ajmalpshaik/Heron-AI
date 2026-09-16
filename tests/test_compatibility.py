# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-IMP-CMP-008
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Import compatibility - unknown is an empty list, not eight releases.

    python tests/test_compatibility.py

WHAT IT PROVES
  1. THE RUNTIME LOOKUP IS HERON-FRG-MTX-009's FUNCTION, by identity,
     and its answers match Directory.Build.props itself.

  2. UNKNOWN IS AN EMPTY LIST. An import declaring nothing supports
     nothing KNOWN - and the answer says so in its own field rather than
     leaving a reader to infer it from a short list.

  3. A RUNTIME NOBODY BUILDS FOR IS ITS OWN ANSWER, carrying the ones
     that are - which is the question its author needs answered.

  4. DECLARED AND DERIVED DISAGREEING IS REPORTED, NOT RESOLVED, and the
     answer names what each side has that the other has not.

  5. AN UNREADABLE BUILD FILE REFUSES rather than falling back to a
     guess.

  6. NOTHING IS COMPILED.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_compatibility as CMP                              # noqa: E402
import heron_matrix as MTX                                     # noqa: E402
import heron_fragment as FRAG                                  # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_compatibility.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]
    props = io.open(MTX.PROPS, encoding="utf-8").read()

    print("\n1. the runtime lookup is HERON-FRG-MTX-009's")
    check(CMP.runtimes is MTX.runtimes,
          "CMP.runtimes IS MTX.runtimes - the same object")
    check(CMP.VERSIONS is FRAG.REVIT_VERSIONS,
          "and the release list is heron_fragment's object")
    table = CMP.runtimes()
    check(sorted(table) == sorted(FRAG.REVIT_VERSIONS),
          "the table covers every supported release")
    # AND ITS ANSWERS ARE THE BUILD FILE'S.
    for release, target in sorted(table.items()):
        check(target in props,
              "%s -> %s, and %s appears in Directory.Build.props"
              % (release, target, target))
    # NO TARGET LITERAL. A bare "net" search matches ordinary prose, so
    # the check is against the actual targets the build uses.
    written = [one for one in set(table.values()) if '"%s"' % one in logic]
    check(not written,
          "this module writes none of the %d .NET targets%s"
          % (len(set(table.values())),
             "" if not written else ": %s" % ", ".join(written)))

    print("\n2. unknown is an empty list")
    blank = CMP.look()
    check(blank["looked"] is True, "it is answered, not refused")
    check(blank["revit"] == [], "and `revit` is EMPTY")
    check(blank["unknown"] is True,
          "with `unknown` true, so a reader is not left inferring it "
          "from a short list")
    check("not eight releases" in blank["why"],
          "the answer says an empty list and not eight releases")
    check("largest silent pass in this project" in blank["unjudged"][0],
          "and why: claiming 2020-2027 on the strength of nobody having "
          "said otherwise")
    check(len(FRAG.REVIT_VERSIONS) == 8,
          "there really are eight it could have claimed")

    print("\n3. a runtime nobody builds for is its own answer")
    for target in ("net6.0", "net7.0", "netstandard2.0"):
        answer = CMP.look(runtime=target)
        check(answer["unbuilt"] and answer["unbuilt"]["runtime"] == target,
              "%r is named as unbuilt" % target)
        check(answer["revit"] == [],
              "  and supports nothing known")
    odd = CMP.look(runtime="net6.0")
    check(sorted(odd["unbuilt"]["built_for"])
          == sorted(set(table.values())),
          "carrying what IS built for: %s"
          % ", ".join(sorted(odd["unbuilt"]["built_for"])))
    check(CMP.look(runtime="net48")["revit"]
          == ["2021", "2022", "2023", "2024"],
          "while a runtime that IS built for resolves to its releases")
    check(CMP.look(runtime="NET48")["revit"] == ["2021", "2022", "2023",
                                                 "2024"],
          "and case does not make it a different runtime")

    print("\n4. disagreement is reported, not resolved")
    both = CMP.look(declared=list(FRAG.REVIT_VERSIONS),
                    runtime="net8.0-windows")
    check(both["disagree"] is not None, "the two sources disagree")
    check(both["disagree"]["only_declared"]
          == ["2020", "2021", "2022", "2023", "2024", "2027"],
          "six releases are declared and not implied by the runtime")
    check(both["disagree"]["only_from_runtime"] == [],
          "and none the other way round")
    check("deciding which is wrong needs the code"
          in both["disagree"]["why"],
          "neither is resolved - the declaration is a claim and the "
          "runtime is a fact about the code")
    check(both["revit"] == ["2025", "2026"],
          "and `revit` is the intersection, which is the cautious half")
    agreed = CMP.look(declared=["2025", "2026"], runtime="net8.0-windows")
    check(agreed["disagree"] is None and agreed["revit"] == ["2025", "2026"],
          "two sources agreeing report no disagreement")

    print("\n5. an unreadable build file refuses")
    was = MTX.PROPS
    try:
        MTX.PROPS = "/nowhere/Directory.Build.props"
        answer = CMP.look(declared=["2024"])
        reached.add(answer.get("refused"))
        check(answer.get("refused") == "NO_RUNTIME_TABLE",
              "with no table, the whole answer is refused")
        check("would be this file's opinion rather than what the build "
              "actually does" in answer["why"],
              "rather than falling back to a guess")
    finally:
        MTX.PROPS = was
    check(CMP.look(declared=["2024"])["looked"] is True,
          "and it works again once the real file is back")

    print("\n6. nothing is compiled")
    imports = sorted(line.split()[1] for line in logic.split("\n")
                     if line.startswith("import "))
    check(imports == ["heron_fragment", "heron_matrix", "os", "sys"],
          "the import list is os, sys and the two agents it reads: %s"
          % ", ".join(imports))
    for reaching in ("subprocess", "dotnet", "msbuild", "write(", "open("):
        check(reaching not in logic, "nothing here uses %s" % reaching)
    check(any("HERON-FRG-MTX-009 owns what COMPILES" in line
              for line in blank["unjudged"]),
          "and the answer says who owns what compiles, from tests rather "
          "than assumption")

    print("\n7. every declared failure is named and reached")
    for declared, name in ((["2028"], "NOT_A_VERSION"),
                           (["2019"], "NOT_A_VERSION"),
                           (["twenty-four"], "NOT_A_VERSION")):
        answer = CMP.look(declared=declared)
        reached.add(answer.get("refused"))
        check(answer.get("refused") == name, "%r is %s" % (declared, name))

    print()
    print("R. THE SECOND CODEX REVIEW - a declaration against a runtime "
          "nobody builds for")
    answer = CMP.look(declared=["2025"], runtime="net6.0")
    check(answer["revit"] == [],
          "declaring 2025 while targeting a runtime no release here uses "
          "supports NO release known - it used to come back supported "
          "and undisputed while `unbuilt` in the same answer said no "
          "release uses that runtime, and both cannot be true")
    check(answer.get("disagree") and answer["disagree"]["only_declared"]
          == ["2025"],
          "  and the conflict is named, neither side resolved")
    check(bool(answer.get("unbuilt")),
          "  with the unbuilt runtime still reported")
    answer = CMP.look(declared=[], runtime="net6.0")
    check(answer.get("unknown") is True,
          "an import declaring nothing AND naming an unbuilt runtime is "
          "still UNKNOWN, which is an empty list rather than eight "
          "releases")

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-IMP-CMP-008.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 2, "the contract declares 2 failures")
    for failure in named:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    check(len(blank["unjudged"]) == 4, "four things are left unjudged")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    unknown is an empty list, not eight releases")
    return 0


if __name__ == "__main__":
    sys.exit(main())
