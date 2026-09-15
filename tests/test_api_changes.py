# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-REVIT-ACI-034
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
API change intelligence - the member that actually caught this project
out, found in real fragment C#.

    python tests/test_api_changes.py

WHAT IT PROVES
  1. IT FINDS THE ONE THAT HAPPENED. ElementId.IntegerValue is in the
     evidence at 2026, which is where the compiler put it.

  2. IT FINDS IT IN CODE WRITTEN THE WAY THIS LIBRARY WRITES CODE -
     `using` at the top, short name below. A fully-qualified matcher
     finds NOTHING in 360 real fragments and clears every one.

  3. A COMMENT IS NOT A CALL. Three real fragments name IntegerValue in
     a comment saying why they do not use it, and all three were
     reported as breaking until comments were stripped.

  4. NOBODY LOOKED IS NOT NOTHING CHANGED.

  5. A VERSION GUARD IS READ, AND WHAT IS LEFT OVER-REPORTS OUT LOUD.

  6. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import glob
import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_apichanges as ACI                                 # noqa: E402
import heron_fragment as FRAG                                  # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []

# The member this project wrote down as "the property every version has
# had", which 2026 and 2027 do not have. TAKEN FROM THE AGENT, not
# retyped - check-structure.py refuses the Revit namespace outside
# revit/, and it is right to here as well.
CAUGHT_US = ACI.CAUGHT_US

# Written the way a fragment writes it, because that is the only way it
# is ever written here.
REAL_SHAPE = ("ElementId id = element.Id;\n"
              "int number = id.IntegerValue;\n")


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def library():
    """Every real fragment with the C# it actually ships."""
    out = []
    for frag in FRAG.load_all()[0].values():
        code = "".join(
            io.open(path, encoding="utf-8", errors="replace").read()
            for path in sorted(glob.glob(os.path.join(frag.folder, "impl",
                                                      "*", "fragment.cs"))))
        out.append({"id": frag.id, "code": code,
                    "folder": frag.folder.replace("\\", "/"),
                    "revit": [str(one) for one
                              in (frag.data.get("revit") or [])]})
    return out


def main():
    reached = set()
    found = ACI.evidence()
    if not found:
        print("\nNO EVIDENCE. tools/api-surface/changes.json is not present.")
        print("Run `python tools/api-changes.py` - it needs the network, a")
        print(".NET SDK and 264 MB of reference assemblies.")
        # Not a failure of this agent. The one thing that CAN be proved
        # without the evidence is that its absence refuses rather than
        # clears, and that is section 4, which runs either way.
        answer = ACI.check([{"id": "x", "revit": ["2026"], "code": ""}],
                           release="2026", path=os.path.join(ROOT, "nope"))
        check(answer.get("refused") == "NO_EVIDENCE",
              "and an absent evidence file refuses rather than clears")
        return 0 if not FAILURES else 1

    print("\n1. it finds the one that happened")
    at = [one["to"] for one in found["transitions"]
          if CAUGHT_US in (one.get("removed") or [])]
    check(at == ["2026"],
          "%s is in the evidence, removed at %s - which is where the "
          "compiler put it" % (CAUGHT_US, ", ".join(at) or "nowhere"))
    check(CAUGHT_US in ACI.removed_in(found, "2026"),
          "and removed_in('2026') names it")
    check(CAUGHT_US not in ACI.removed_in(found, "2025"),
          "and removed_in('2025') does not - 2025 still had it")
    check(len(ACI.removed_in(found, "2026")) > 1,
          "it is not the only one: %d member(s) left at 2026"
          % len(ACI.removed_in(found, "2026")))

    print("\n2. it finds it in code written the way this library writes code")
    said = ACI.check([{"id": "FRG-ELE-001", "revit": ["2026"],
                       "code": REAL_SHAPE}], release="2026")
    check([one["id"] for one in said["breaks"]] == ["FRG-ELE-001"],
          "a short-name call through a variable is caught")
    check(said["breaks"][0]["calls"] == [CAUGHT_US],
          "and the member is named in full")
    # THE MATCHER A FULLY-QUALIFIED SEARCH WOULD BE. This library
    # contains no fully-qualified Revit name at all, so that matcher
    # clears all 360 - the failure this section exists to prevent.
    every = library()
    qualified = [one for one in every if ACI.DB in one["code"]]
    check(not qualified,
          "no real fragment writes a fully-qualified Revit name (%d of %d), "
          "so matching on one would clear the whole library"
          % (len(qualified), len(every)))
    check(ACI.calls(REAL_SHAPE, CAUGHT_US),
          "calls() needs BOTH the member as a word and its type in the file")
    check(not ACI.calls("int number = thing.IntegerValue;", CAUGHT_US),
          "  the member alone is not enough - `ElementId` is not there")
    check(not ACI.calls("ElementId id = element.Id;", CAUGHT_US),
          "  and the type alone is not enough either")

    print("\n3. a comment is not a call")
    commented = ("// ElementId.IntegerValue was removed by 2026, so this\n"
                 "// formats the id instead of reading it as a number.\n"
                 "ElementId id = element.Id;\n"
                 "string number = id.ToString();\n")
    check(not ACI.calls(ACI.code_of(commented), CAUGHT_US),
          "a fragment explaining why it does NOT use the member is clear")
    check(ACI.calls(ACI.code_of(commented + REAL_SHAPE), CAUGHT_US),
          "and one that explains AND uses it is not")
    check("IntegerValue" not in ACI.code_of("/* ElementId.IntegerValue */"),
          "a block comment goes too")
    # THE REAL ONES. Three fragments in this library name it in a comment.
    named = [one for one in every
             if "IntegerValue" in one["code"]
             and "2026" in one["revit"]]
    check(named, "%d real fragment(s) name IntegerValue and claim 2026"
                 % len(named))
    still = [one["id"] for one in named
             if ACI.calls(ACI.code_of(one["code"]), CAUGHT_US)]
    check(not still,
          "and none of them actually calls it: %s"
          % (", ".join(still) or "every one is a comment"))

    print("\n4. nobody looked is not nothing changed")
    missing = ACI.check([{"id": "x", "revit": ["2026"], "code": ""}],
                        release="2026",
                        path=os.path.join(ROOT, "no-such-file.json"))
    check(missing.get("refused") == "NO_EVIDENCE",
          "an absent evidence file is NO_EVIDENCE, not an empty answer")
    check("nobody looked" in missing["why"],
          "and the answer says so in those words")
    check(ACI.MAKE_IT in missing["why"],
          "naming the command that produces it: %s" % ACI.MAKE_IT)
    check(missing.get("clear") is None and missing.get("breaks") is None,
          "and it carries neither a clear list nor a breaks list")

    print("\n5. a version guard is read, and what is left over-reports")
    # THIS LIBRARY SURVIVES A REMOVED MEMBER WITH `#if REVIT2020 ||
    # REVIT2021`, so a reader that ignores guards reports every fragment
    # that ALREADY HANDLED a change as breaking on it. Twelve were, at
    # 2023 alone, before compiled_for() existed.
    guarded = ("#if REVIT2020 || REVIT2021\n"
               "        IndependentTag tag = one;\n"
               "        XYZ end = tag.LeaderEnd;\n"
               "#else\n"
               "        IndependentTag tag = one;\n"
               "        XYZ end = tag.GetLeaderEnd(reference);\n"
               "#endif\n")
    old_api = ACI.DB + "IndependentTag.LeaderEnd"
    check(old_api in ACI.removed_in(found, "2023"),
          "%s really did leave at 2023" % old_api)
    check(ACI.calls(ACI.code_of(guarded, "2021"), old_api),
          "the guarded call IS seen when the release is 2021, where it "
          "compiles")
    check(not ACI.calls(ACI.code_of(guarded, "2023"), old_api),
          "and is NOT seen at 2023, where the compiler never reads it")
    check("GetLeaderEnd" in ACI.code_of(guarded, "2023"),
          "  the #else branch is what survives at 2023")
    check("GetLeaderEnd" not in ACI.code_of(guarded, "2021"),
          "  and the #if branch is what survives at 2021")
    check(ACI.live("REVIT2020 || REVIT2021", "2021")
          and not ACI.live("REVIT2020 || REVIT2021", "2023"),
          "live() reads the condition rather than matching the whole string")
    check(ACI.live("SOMETHING_ELSE", "2023"),
          "and a condition it cannot read is treated as LIVE - an "
          "unreadable guard leaves its contents in, so the answer "
          "over-reports rather than quietly dropping unchecked code")
    # THE REAL FRAGMENT THAT PROMPTED ALL THIS.
    arranged = [one for one in every if one["id"].endswith("-TAG-001")
                or "arrange-tags" in one["folder"]]
    for one in arranged[:1]:
        check(not ACI.calls(ACI.code_of(one["code"], "2023"), old_api),
              "and the real fragment that guards this call is clear at "
              "2023, which is what tools/check-fragments-compile.py says "
              "too")

    # THE LIBRARY IS REPORTED, NOT ASSERTED TO BE CLEAN. This agent
    # declares that it over-reports, so a suite demanding zero would be
    # demanding the opposite of the contract. What is checked is that
    # every flag is the KIND that was declared - a member name two types
    # share - and not a version guard being misread.
    for release in FRAG.REVIT_VERSIONS[1:]:
        answer = ACI.check(every, release=release)
        check(answer["checked"], "%s: %d claim it, %d may break%s"
              % (release, len(answer["breaks"]) + len(answer["clear"]),
                 len(answer["breaks"]),
                 "" if not answer["breaks"] else "  (%s)"
                 % ", ".join(sorted(set(
                     one.rsplit(".", 1)[-1]
                     for card in answer["breaks"]
                     for one in card["calls"])))))
        for card in answer["breaks"]:
            for member in card["calls"]:
                short = member.rsplit(".", 1)[-1]
                where = [one for one in every if one["id"] == card["id"]][0]
                check(ACI.calls(ACI.code_of(where["code"], release), member),
                      "  %s: %s survives a re-read of the same code"
                      % (card["id"], short))
    # A CHECKER THAT FINDS NOTHING IS EVIDENCE ABOUT THE CHECKER until it
    # has been shown to catch something. tools/check-api-surface.py's own
    # docstring says exactly this. So one real fragment is handed back
    # with the call put into it.
    victim = dict(every[0], code=every[0]["code"] + "\n" + REAL_SHAPE,
                  revit=list(FRAG.REVIT_VERSIONS))
    planted = ACI.check([victim], release="2026")
    check([one["id"] for one in planted["breaks"]] == [victim["id"]],
          "and putting the call into a real fragment IS caught - the clean "
          "result above is about the library, not about the checker")

    print("\n6. every failure the contract declares is named and reached")
    for these, release, path, name in (
            ([], "2026", os.path.join(ROOT, "nope.json"), "NO_EVIDENCE"),
            ([], None, None, "NO_RELEASE"),
            ([], "1999", None, "UNKNOWN_RELEASE"),
            ([], FRAG.REVIT_VERSIONS[0], None, "NO_EARLIER_RELEASE"),
            ([], "2026", None, "NOTHING_TO_CHECK"),
            (["a string"], "2026", None, "NOT_A_FRAGMENT")):
        answer = ACI.check(these, release=release, path=path)
        reached.add(answer.get("refused"))
        check(answer.get("refused") == name, "%s is reached" % name)

    logic = io.open(os.path.join(ROOT, "brain", "heron_apichanges.py"),
                    encoding="utf-8").read().split("\ndef main(")[0]
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-REVIT-ACI-034.yaml"))
    named_failures = contract.get("failures") or []
    check(len(named_failures) == 6, "the contract declares 6 failures")
    for failure in named_failures:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named_failures) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))

    good = ACI.check(every, release="2026")
    check(good["fixed"] is False, "`fixed` is false, and always false")
    check(len(good["unjudged"]) == 4, "four things are left unjudged")
    check(ACI.RELEASES is FRAG.REVIT_VERSIONS,
          "the release list IS heron_fragment's - not a copy (D-05)")
    check(any("FRG-EVO-005" in line for line in good["unjudged"]),
          "and the answer names who does the fixing")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the member that caught this project out, found in real C#")
    return 0


if __name__ == "__main__":
    sys.exit(main())
