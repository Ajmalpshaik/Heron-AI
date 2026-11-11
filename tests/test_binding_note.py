#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   14
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The binding note counts what was OFFERED, not just what survived.

    python tests/test_binding_note.py

WHY THIS EXISTS
---------------
FRAGMENT-ISSUES row 75. `RevitFragment.Shape()` revives a carried value by
walking each `ElementId` through `doc.GetElement(id)` on the HOST document,
swallowing the miss and keeping what resolved. Zero survivors is handled
honestly - it returns null and the caller says "nothing usable survived". A
PARTIAL revival was not: 1128 linked walls carried over, two ids happened to
name real elements in the host document, and the note read

    elements from select-from-link (2)

which is indistinguishable from a deliberate narrowing to two.

`HeronBindingNote.Size` is now the ONLY thing that tells those apart, and **a
compiler cannot catch a regression in its wording or in the order of its two
arguments** - both are `object`. That gap is what this closes, and it was
raised as a review finding on the pull request that introduced the fix rather
than being noticed by its author.

HOW IT RUNS WITHOUT REVIT
-------------------------
`HeronBindingNote.cs` touches no Autodesk type - it counts two collections -
so `tests/Heron.BindingNote.TestHost` links it BY SOURCE, the same pattern
`Heron.StackGuard.TestHost` and `Heron.Banner.TestHost` already use and for
the reason their project files give: a project reference would drag in
RevitAPI.dll and make the one testable piece untestable on any machine
without Revit. Linking the source also keeps exactly ONE copy of the rule -
a second implementation written to test the first is how `binds:` came to be
honoured by the Python half and ignored by the C# executor (row 96).

IT EXITS 3 WHEN .NET IS ABSENT, and that is NOT a pass - `check-gaps.py` and
`change-evidence.py` both read 3 as "could not run", which is a fourth state
beside PASS, FAIL and NEEDS REAL REVIT. A machine with no SDK learns that it
did not check, rather than that everything is fine.

MEASURED AGAINST THE PREVIOUS IMPLEMENTATION, which is what makes it a test
rather than a description: built against a copy of `Size` as it stood before
the fix - one that ignored what was offered - **3 of the 7 checks fail and
the host exits 1**. The four that still pass are the ones that must not move:
equal counts, no carried value, and a scalar.

AND ROW 75's STOP, 2026-10-09. Counting the loss was the half that could not
break a caller; the other half is that a carry from a LINK is not looked up
in the host at all. The chain records, element by element, which document
each came from (`HeronBindingNote.OtherDocument`), and the binder refuses a
need whose carry came from somewhere else, naming the link
(`HeronBindingNote.CarriedFromElsewhere`). The host asks both by reflection,
so on the code as it stood they fail as checks rather than as a build error;
`linked_carry_is_refused` below reads that RevitFragment.cs writes the record
and asks before `Shape`. Neither has been seen in a real Revit - that needs a
link, and is owed.

AND A FRAGMENT'S OWN RULE, 2026-11-11 - rows 5b-1120 and 5b-1549.
SET_ROUTING_PREFERENCE found a fitting by its type name alone, so "Standard"
bound whichever family the collector met first. Its rule now takes "Family:
Type", the spelling the binder takes - but a fragment is compiled on its own
inside Revit and cannot call `HeronBindingNote`, so the rule is plain text
handling in the fragment between two PART NAMES marker lines. This suite
copies those lines, unchanged, into one generated file (a scratch folder,
removed afterwards), the host compiles it in beside the binder's rule, and
the host's last section asks both the row's own names. ONE copy: the lines
checked are the lines that run in Revit. On the fragment as it stood there
is no block, the host is built without one, version 1's rule stands in, and
8 of that section's 18 checks fail (measured 2026-11-11). `routing_part` below
reads the two things the host cannot: that the fragment binds a part only
when exactly ONE fits, and that the rule swap sits in a SubTransaction rolled
back on a refusal (row 5b-1549). Whether Revit's SubTransaction really puts a
removed rule back needs a Revit, and is owed (NEEDS-CHECKING Group NL).
"""

import io
import os
import re
import shutil
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
HOST = os.path.join(ROOT, "tests", "Heron.BindingNote.TestHost")
COULD_NOT_RUN = 3


def _asked(what):
    """`dotnet <what>`, or None when dotnet cannot answer at all."""
    try:
        out = subprocess.check_output(["dotnet", what],
                                      stderr=subprocess.STDOUT)
    except Exception:                                      # noqa: BLE001
        return None
    return out.decode("utf-8", "replace")


def _majors(said, prefix=None):
    """The MAJOR version of every line dotnet listed, highest last.

    `--list-runtimes` prints "Microsoft.NETCore.App 10.0.12 [path]" and
    `--list-sdks` prints "10.0.112 [path]", so the version is the first field
    for one and the second for the other. One reader with a prefix rather
    than two, because two would drift.
    """
    out = []
    for line in (said or "").splitlines():
        parts = line.split()
        if prefix:
            if not line.startswith(prefix):
                continue
            parts = parts[1:]
        if not parts:
            continue
        try:
            out.append(int(parts[0].split(".")[0]))
        except ValueError:
            continue
    return sorted(set(out))


def _tfm():
    """The newest framework BOTH an installed SDK can build and a runtime can run.

    A RUNTIME AND AN SDK ARE DIFFERENT THINGS AND THIS SUITE NEEDS BOTH. Only
    an SDK BUILDS an assembly; only a runtime RUNS one. This suite builds its
    host and then runs it, so it needs a target that is under the newest SDK
    and present among the runtimes.

    ASKING EITHER ONE ALONE PICKS A TARGET THAT CANNOT BE BUILT, and both
    mistakes were review findings on PR #212 rather than guesses:

      - runtimes alone: a machine with a runtime and NO SDK returns a target,
        `dotnet build` fails, and a missing dependency is reported as a
        FAILING REPOSITORY;
      - and adding a bare "is there any SDK" is not enough either - a .NET 10
        runtime beside only the .NET 8 SDK still selects `net10.0`, which
        that SDK cannot target, and the build fails the same way.

    Both end as exit 1 where the honest answer is exit 3. That is exactly the
    confusion FRAGMENT-ISSUES row 162 is about, in the suite that row's own
    repair is named after, which is why it is worth this much care.

    Returns None when no runtime and SDK meet.
    """
    runtimes = _majors(_asked("--list-runtimes"), "Microsoft.NETCore.App")
    sdks = _majors(_asked("--list-sdks"))
    if not runtimes or not sdks:
        return None
    # An SDK builds for its own major and older, so the target cannot be
    # newer than the newest SDK - and it must be one a runtime can run.
    usable = [m for m in runtimes if m <= max(sdks)]
    return "net%d.0" % max(usable) if usable else None


CALL_SITE = os.path.join(ROOT, "revit", "Heron.Revit.Addin", "RevitFragment.cs")

# THE REVIVED VALUE FIRST, THE CARRIED ONE SECOND, AT THE PRODUCTION CALL.
# Written as a pattern rather than a fixed string so that reformatting the
# line does not fail the suite, while swapping the two DOES.
BINDS = re.compile(r"Size\(\s*shaped\s*:\s*shaped\s*,\s*before\s*:\s*value\s*\)")


def crosses_the_seam():
    """Does RevitFragment still pass SURVIVED first and OFFERED second?

    WHY THIS IS HERE AND NOT IN THE TEST HOST. The host links
    HeronBindingNote.cs by source and proves the FORMATTER: given (2, 1128)
    it writes "2 of 1128". It cannot prove the call, because RevitFragment.cs
    touches Autodesk types and linking it would drag in RevitAPI.dll - the
    whole reason the formatter was split out.

    So the host's argument-order check only verifies the order the host
    itself supplied. Swap the production call to Size(before: ..., shaped:
    ...) and every check in the host still passes while a modeller reads
    "1128 of 2" - the loss reported backwards, which is worse than the "(2)"
    row 75 started with. Both arguments are `object`, so no compiler catches
    it. A Codex review on PR #212 raised exactly this, and it was right.

    Reading the one line is crude. It is also the only thing here that can
    fail when that line changes, and a crude check that fails beats a
    thorough one that cannot.
    """
    try:
        text = io.open(CALL_SITE, encoding="utf-8").read()
    except (IOError, OSError) as exc:
        return False, "could not read %s (%s)" % (CALL_SITE, exc)
    if BINDS.search(text):
        return True, "binds shaped: shaped, before: value"
    loose = re.findall(r"\+ Size\([^)]*\)", text)
    return False, ("the binding note's call does not pass the revived value "
                   "as `shaped` and the carried one as `before`. Found: %s"
                   % (loose or "no call at all"))


def linked_carry_is_refused(text):
    """Does the chain RECORD a linked carry, and the binder REFUSE it first?

    FRAGMENT-ISSUES row 75, the half the host cannot see. The host proves
    what OtherDocument writes down and what CarriedFromElsewhere says; only
    RevitFragment.cs shows that `Remember` writes the record at all, that
    nothing writes the chain around it, and that `BindNeeds` asks before
    `Shape` revives the ids against the HOST - after it is too late, which is
    the 2-of-1128 bind the row measured.

    Each answer is (ok, what was checked). Every position is checked for -1
    before it is compared, because `str.find` returns -1 for an absent string
    and a bare `a < b` then passes loudest when the guard has been deleted
    (heron-ship 2a).
    """
    found = []

    records = "HeronBindingNote.OtherDocument(" in text
    found.append((records, "Remember records which document each carried "
                           "element came from"))

    kept = "chain.Keep(variable.Name, ids, elsewhere)" in text
    found.append((kept, "and keeps that record beside the ids it carried"))

    # A write to Values that bypasses Keep leaves a record describing a value
    # that is no longer there - a host list refused as a link's, or a link's
    # list bound because the record was cleared without it.
    around = re.findall(r"chain\.Values\.Clear\(\)|chain\.Values\[[^\]]*\]\s*=(?!=)",
                        text)
    found.append((not around, "nothing writes or clears the chain's values "
                              "around that record%s"
                  % ("" if not around else " - found: %s" % ", ".join(around))))

    asks = text.find("HeronBindingNote.CarriedFromElsewhere(")
    shapes = text.find("var shaped = Shape(value, type, target);")
    found.append((asks >= 0 and shapes >= 0 and asks < shapes,
                  "BindNeeds asks CarriedFromElsewhere BEFORE Shape looks the "
                  "ids up in the host (asked at %d, Shape at %d)" % (asks, shapes)))

    refused = re.search(r'if \(foreign != null\) return Json\.Error\("needs_unbound", '
                        r'foreign\);', text)
    found.append((refused is not None,
                  "and returns its refusal instead of binding"))

    # The chain must look the record up under the key it READ, which is
    # `created` when a creator's output filled `elements`.
    both = (re.search(r"value = carried\[wanted\];\s*carriedAs = wanted;", text)
            is not None
            and re.search(r'value = carried\["created"\];\s*carriedAs = "created";',
                          text) is not None)
    found.append((both, "under the key the chain was read by - the need's, or "
                        "'created'"))
    return found


def typed_ids_only_on_a_read(text):
    """Are typed element ids matched only for `elementIds`, only on a read?

    FRAGMENT-ISSUES 5b-441, the half the host cannot see. The host proves what
    TypedIds, TypedIdsNotFound and TypedIdsOnAWrite say; only RevitFragment.cs
    shows that the binder asks `reading` BEFORE it matches anything, that the
    main bind passes `!writing`, and that a setup step - which runs inside the
    group a write opened - passes false. Every position is checked for -1
    first (heron-ship 2a).
    """
    found = []
    keyed = text.find('if (need == "elementIds")')
    asks = text.find("if (!reading)", keyed if keyed >= 0 else 0)
    matches = text.find("return ElementsWithTypedIds(doc, typed, out problem);",
                        keyed if keyed >= 0 else 0)
    found.append((keyed >= 0 and asks >= 0 and matches >= 0 and keyed < asks < matches,
                  "typed ids are keyed on `elementIds` and refused on a write BEFORE "
                  "anything is matched (at %d, %d, %d)" % (keyed, asks, matches)))
    found.append((re.search(r"supplied, expect, !writing,\s*out prologue, out binding\)",
                            text) is not None,
                  "the fragment's own bind says it reads only when it does not write"))
    found.append((re.search(r"client, supplied, null, false,\s*out stepPrologue", text)
                  is not None,
                  "a setup step is never a read for this rule"))
    # Code only - a comment explaining why the constructor is avoided names it.
    built = [line.strip() for line in text.splitlines()
             if "new ElementId(" in line and not line.strip().startswith("//")]
    found.append((not built,
                  "and no ElementId is built from a number anywhere in the binder%s"
                  % ("" if not built else " - found: %s" % built)))
    return found


ROUTING = os.path.join(ROOT, "brain", "fragments", "set-routing-preference",
                       "impl", "any", "fragment.cs")
PART_NAMES_START = "// ---- PART NAMES"
PART_NAMES_END = "// ---- END PART NAMES"


def part_names_block():
    """The fragment's own lines between its two PART NAMES markers, or None.

    Unchanged, comments and all: what the host compiles is what Revit runs.
    None when either marker is missing or they are out of order - the
    fragment as it stood before row 5b-1120 has neither.
    """
    try:
        lines = io.open(ROUTING, encoding="utf-8").read().replace("\r\n", "\n").split("\n")
    except (IOError, OSError):
        return None
    starts = [i for i, line in enumerate(lines) if line.startswith(PART_NAMES_START)]
    ends = [i for i, line in enumerate(lines) if line.startswith(PART_NAMES_END)]
    if len(starts) != 1 or len(ends) != 1 or ends[0] <= starts[0]:
        return None
    return "\n".join(lines[starts[0] + 1:ends[0]])


def part_names_source(folder, block):
    """Write the block into one C# file the host can compile, and return its path.

    The method's own parameters are named a, b and c so that they can never
    collide with a name the fragment's lines declare.
    """
    path = os.path.join(folder, "FragmentPartNames.cs")
    with io.open(path, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(
            "// Generated by tests/test_binding_note.py from\n"
            "// brain/fragments/set-routing-preference/impl/any/fragment.cs.\n"
            "// Never edited by hand and never committed.\n"
            "using System;\n"
            "using System.Collections.Generic;\n"
            "using System.Linq;\n"
            "\n"
            "namespace Heron.BindingNote.TestHost\n"
            "{\n"
            "    internal static class FragmentPartNames\n"
            "    {\n"
            "        internal static List<int> Named(IList<string> a, IList<string> b, string c)\n"
            "        {\n"
            + block + "\n"
            "            return partsNamed(a, b, c);\n"
            "        }\n"
            "    }\n"
            "}\n")
    return path


def routing_part(block):
    """What the host cannot see of rows 5b-1120 and 5b-1549, read off the code.

    The host proves what the fragment's name rule ANSWERS. Only the fragment
    shows that a part is bound when exactly one fits and never otherwise, and
    that RemoveRule and AddRule sit inside a SubTransaction that is rolled
    back when the swap is refused. Comment lines are removed first, so a rule
    described in words does not count. Every position is checked for -1
    before it is compared (heron-ship 2a).
    """
    found = []
    found.append((block is not None,
                  "SET_ROUTING_PREFERENCE carries its part-name rule between "
                  "two PART NAMES marker lines"))
    try:
        text = io.open(ROUTING, encoding="utf-8").read()
    except (IOError, OSError):
        text = ""
    code = "\n".join(line for line in text.replace("\r\n", "\n").split("\n")
                     if not line.strip().startswith("//"))

    binds = re.findall(r"\bwantedPart\s*=(?!=)\s*([^;]*);", code)
    found.append((binds == ["null", "parts[fitting[0]]"],
                  "a part is bound in ONE place, from the rule's answer - found: %s"
                  % (binds or "no assignment at all")))
    found.append((re.search(r"var fitting = partsNamed\(partFamilies, partNames, wantedName\);"
                            r"\s*if \(fitting\.Count == 1\)\s*\{\s*wantedPart = ", code)
                  is not None,
                  "and only when exactly ONE part fits the name (row 5b-1120)"))
    # The branch runs to the `else` after it. Format items put braces inside
    # its strings, so it is read to that `else`, never to the first brace.
    several = re.search(r"else if \(fitting\.Count > 1\)\s*\{(.*?)proceed = false;\s*\}\s*else\b",
                        code, re.S)
    found.append((several is not None and "nearMisses.Add(" in several.group(1)
                  and "wantedPart" not in several.group(1),
                  "two or more that fit are listed, none is bound, and it stops there"))

    opened = code.find("using (var swap = new SubTransaction(doc))")
    started = code.find("swap.Start();")
    removed = code.find("manager.RemoveRule(")
    added = code.find("manager.AddRule(")
    kept = code.find("swap.Commit();")
    order = [opened, started, removed, added, kept]
    found.append((min(order) >= 0 and order == sorted(order),
                  "the rule is removed and added again INSIDE a SubTransaction, "
                  "kept after both (row 5b-1549) - positions %s" % order))
    found.append((code.count("manager.RemoveRule(") == 1 and code.count("manager.AddRule(") == 1,
                  "and nowhere outside it"))
    found.append((re.search(r"else\s*\{\s*swap\.RollBack\(\);", code) is not None,
                  "a swap that does not read back as asked is rolled back"))
    found.append((re.search(r"catch \(Exception error\)\s*\{\s*"
                            r"if \(swap\.HasStarted\(\) && !swap\.HasEnded\(\)\) "
                            r"swap\.RollBack\(\);", code) is not None,
                  "and so is one Revit refuses by throwing"))
    return found


def main():
    """Hand the fragment's own rule to the host, run the suite, and clear up."""
    block = part_names_block()
    folder = tempfile.mkdtemp(prefix="heron-binding-note-")
    try:
        handed = part_names_source(folder, block) if block is not None else None
        return _suite(handed, block)
    finally:
        shutil.rmtree(folder, ignore_errors=True)


def _suite(handed, block):
    tfm = _tfm()
    if tfm is None:
        print("COULD NOT RUN - no framework here that an installed SDK can")
        print("  build AND an installed runtime can run. This suite BUILDS")
        print("  its host and then runs it, so it needs both.")
        print("  Linux:   apt-get install -y dotnet-sdk-10.0")
        print("  This is exit 3, which is NOT a pass: nothing was checked.")
        return COULD_NOT_RUN

    out_dir = "bin/x64/Debug-%s/" % tfm
    # READ THE OUTPUT RATHER THAN ONLY PIPING IT. `subprocess.call` with
    # stdout=PIPE and nobody reading deadlocks as soon as the child fills the
    # OS pipe buffer - 64 KiB on Linux, which a verbose restore failure
    # passes easily. Demonstrated rather than assumed: a child writing 1 MB
    # to a piped stdout under `call` never returns. The suite would then hang
    # until the outer CI timeout instead of saying PASS, FAIL or NOT RUN.
    # `run` reads the pipe, so it cannot fill, and the captured text is then
    # there to PRINT - which the old version threw away, leaving a build
    # failure with no diagnostic. A Codex review on PR #212 raised both.
    # THE FRAGMENT'S OWN RULE, WHEN IT HAS ONE (row 5b-1120). Without it the
    # host still builds, and its last section fails as checks.
    part_names = ["-p:HeronFragmentPartNames=%s" % handed] if handed else []
    built = subprocess.run(
        ["dotnet", "build", HOST, "-p:RevitVersion=2024",
         "-p:HeronTfm=%s" % tfm, "-p:OutputPath=%s" % out_dir] + part_names,
        stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    if built.returncode != 0:
        print("FAILED - the test host did not build for %s." % tfm)
        print("  dotnet build %s -p:RevitVersion=2024 -p:HeronTfm=%s "
              "-p:OutputPath=%s" % (HOST, tfm, out_dir))
        said = (built.stdout or b"").decode("utf-8", "replace").strip()
        for line in said.splitlines()[-25:]:
            print("    %s" % line)
        return 1

    dll = os.path.join(HOST, out_dir, "Heron.BindingNote.TestHost.dll")
    if not os.path.exists(dll):
        print("FAILED - built, and %s is not there." % dll)
        return 1

    # The host prints its own checks; they are the output of this suite.
    sys.stdout.flush()
    ran = subprocess.call(["dotnet", dll])
    if ran != 0:
        # Said here too, or a host built with no block reads only as its own
        # failed checks and never names the reason.
        if block is None:
            print()
            print("  FAIL  %s has no PART NAMES block to hand the host "
                  "(row 5b-1120)" % os.path.relpath(ROUTING, ROOT))
        return ran

    # AND THE ONE CHECK THE HOST CANNOT MAKE, about the call rather than the
    # formatter. Last, so that a failure here is never confused with the
    # formatter's own.
    ok, said = crosses_the_seam()
    print()
    print("  %s  the production call passes the survivor first - %s"
          % ("ok  " if ok else "FAIL", said))

    # The same crude read for row 5b-252's rule: the host proves what
    # AbsentValue answers, and only this can see that the binder asks it.
    try:
        text = io.open(CALL_SITE, encoding="utf-8").read()
    except (IOError, OSError):
        text = ""
    asks = "HeronBindingNote.AbsentValue(type, optional)" in text
    print("  %s  the binder asks AbsentValue before refusing an absent "
          "request need" % ("ok  " if asks else "FAIL"))

    # Row 5b-488's: the card's own default is asked FIRST, and what it names
    # goes through the same conversion as a typed value - so the note says so.
    declared_at = text.find("HeronBindingNote.DeclaredAbsent(optional, absentWord)")
    absent_at = text.find("HeronBindingNote.AbsentValue(type, optional)")
    declares = (declared_at >= 0 and absent_at >= 0 and declared_at < absent_at
                and "givenText = defaulted;" in text
                and "the card's own default (optional)" in text)
    print("  %s  the binder asks DeclaredAbsent before AbsentValue, and reads "
          "the card's default as a typed value" % ("ok  " if declares else "FAIL"))
    asks = asks and declares

    # And row 5b-322's: the host proves what EmptyWhenAbsent answers, and only
    # this can see that the binder asks it before refusing an unfilled list.
    empties = "HeronBindingNote.EmptyWhenAbsent(type, optionalWord)" in text
    print("  %s  the binder asks EmptyWhenAbsent before refusing an unfilled "
          "element list" % ("ok  " if empties else "FAIL"))

    # Row 5b-1320's other half: the host proves what EmptyMeansAbsent answers;
    # only this can see that the binder asks it BEFORE converting a given
    # value - inViewOnly= was looked up as a view named "" and refused.
    asked_at = text.find("HeronBindingNote.EmptyMeansAbsent(givenText, optional)")
    converts_at = text.find("FromRequest(givenText, type, name, target, selected,")
    blanks = asked_at >= 0 and converts_at >= 0 and asked_at < converts_at
    print("  %s  the binder asks EmptyMeansAbsent of a value given, before "
          "converting it (row 5b-1320)" % ("ok  " if blanks else "FAIL"))
    empties = empties and blanks

    linked = linked_carry_is_refused(text)
    for ok_here, said_here in linked:
        print("  %s  %s" % ("ok  " if ok_here else "FAIL", said_here))
    linked_ok = all(ok_here for ok_here, _ in linked)

    typed = typed_ids_only_on_a_read(text)
    for ok_here, said_here in typed:
        print("  %s  %s" % ("ok  " if ok_here else "FAIL", said_here))
    typed_ok = all(ok_here for ok_here, _ in typed)

    # The Python half of the same rule, word for word on the same cases.
    sys.path.insert(0, os.path.join(ROOT, "brain"))
    import heron_fragment as HF
    mirror = [
        ({"name": "elements", "type": "IList<Element>", "optional": "true"}, True),
        ({"name": "elements", "type": "IList< Element >", "optional": " True "}, True),
        ({"name": "elements", "type": "IEnumerable<Element>", "optional": "true"}, True),
        ({"name": "elements", "type": "IList<Element>"}, False),
        ({"name": "elements", "type": "IList<Element>", "optional": "false"}, False),
        ({"name": "ids", "type": "IList<ElementId>", "optional": "true"}, False),
        ({"name": "element", "type": "Element", "optional": "true"}, False),
        ({"name": "elements", "type": "IList<Element>", "source": "request",
          "optional": "true"}, False),
        # 2026-11-01, GROUP_AND_COUNT v2: the host binds it null.
        ({"name": "quantities", "type": "IDictionary<ElementId, double>",
          "optional": "true"}, True),
        ({"name": "quantities", "type": "IDictionary<ElementId, double>"}, False),
        ({"name": "values", "type": "IDictionary<ElementId, string>",
          "optional": "true"}, False),
        # 2026-11-01: a curve list marked optional binds empty, as the host's
        # EmptyWhenAbsent does (SET_VIEW_CROP's `boundary`).
        ({"name": "boundary", "type": "IList<Curve>", "optional": "true"}, True),
        ({"name": "boundary", "type": " IList< Curve > ", "optional": " True "}, True),
        ({"name": "boundary", "type": "IList<Curve>"}, False),
        ({"name": "curve", "type": "Curve", "optional": "true"}, False),
    ]
    agrees = all(HF.need_may_be_empty(need) == want for need, want in mirror)
    print("  %s  heron_fragment.need_may_be_empty answers the %d cases the host "
          "does" % ("ok  " if agrees else "FAIL", len(mirror)))

    # Row 5b-488's mirror: DeclaredAbsent's cases, and what need_may_be_absent
    # then answers. Asked without calling a name the old module may not have.
    absent_text = getattr(HF, "need_absent_text", None)
    print("  %s  heron_fragment has need_absent_text at all"
          % ("ok  " if absent_text is not None else "FAIL"))
    declared = [
        ({"name": "which", "type": "string", "source": "request",
          "optional": "true", "absent": "all"}, "all", True),
        ({"name": "which", "type": "string", "source": "request",
          "optional": " True ", "absent": "  all "}, "all", True),
        ({"name": "which", "type": "string", "source": "request",
          "absent": "all"}, None, False),
        ({"name": "which", "type": "string", "source": "request",
          "optional": "false", "absent": "all"}, None, False),
        ({"name": "which", "type": "string", "source": "request",
          "optional": "true"}, None, False),
        ({"name": "which", "type": "string", "source": "request",
          "optional": "true", "absent": "   "}, None, False),
        ({"name": "includeLinks", "type": "bool", "source": "request",
          "optional": "true"}, None, True),
        # Row 5b-1320: a View marked optional binds null - the whole model.
        ({"name": "inViewOnly", "type": "View", "source": "request",
          "optional": "true"}, None, True),
        ({"name": "inViewOnly", "type": " View ", "source": "request",
          "optional": " True "}, None, True),
        ({"name": "inViewOnly", "type": "View", "source": "request"}, None, False),
        ({"name": "inViewOnly", "type": "View", "source": "request",
          "optional": "false"}, None, False),
        ({"name": "view", "type": "View", "source": "model",
          "optional": "true"}, None, False),
        # 2026-11-01: a view list marked optional binds empty, as the host's
        # AbsentValue does - and only that spelling, only when marked.
        ({"name": "views", "type": "IList<View>", "source": "request",
          "optional": "true"}, None, True),
        ({"name": "views", "type": " IList<View> ", "source": "request",
          "optional": " True "}, None, True),
        ({"name": "views", "type": "IList<View>", "source": "request"},
         None, False),
        ({"name": "names", "type": "IList<string>", "source": "request",
          "optional": "true"}, None, False),
        # 2026-11-01: one Element or one MEPSystemType marked optional binds
        # null, as the host's AbsentValue does - only when marked.
        ({"name": "start", "type": "Element", "source": "request",
          "optional": "true"}, None, True),
        ({"name": "start", "type": " Element ", "source": "request",
          "optional": " True "}, None, True),
        ({"name": "start", "type": "Element", "source": "request"}, None, False),
        ({"name": "systemType", "type": "MEPSystemType", "source": "request",
          "optional": "true"}, None, True),
        ({"name": "systemType", "type": "MEPSystemType", "source": "request"},
         None, False),
        ({"name": "systemType", "type": "PipingSystemType", "source": "request",
          "optional": "true"}, None, False),
        # 2026-11-01: an Element marked optional binds null and a point list
        # empty, as the host's AbsentValue does (CREATE_OPENING's family half).
        ({"name": "host", "type": "Element", "source": "request",
          "optional": "true"}, None, True),
        ({"name": "host", "type": "Element", "source": "request"},
         None, False),
        ({"name": "boundary", "type": "IList<XYZ>", "source": "request",
          "optional": "true"}, None, True),
        ({"name": "point", "type": "XYZ", "source": "request",
          "optional": "true"}, None, False),
    ]
    mirrored = absent_text is not None and all(
        absent_text(need) == text and HF.need_may_be_absent(need) == may
        for need, text, may in declared)
    print("  %s  heron_fragment.need_absent_text and need_may_be_absent answer "
          "the %d cases the host does" % ("ok  " if mirrored else "FAIL",
                                         len(declared)))

    # Rows 5b-1120 and 5b-1549: what the host cannot see of the fragment.
    parts = routing_part(block)
    for ok_here, said_here in parts:
        print("  %s  %s" % ("ok  " if ok_here else "FAIL", said_here))
    parts_ok = all(ok_here for ok_here, _ in parts)

    return 0 if (ok and asks and empties and linked_ok and typed_ok and agrees
                 and mirrored and parts_ok) else 1


if __name__ == "__main__":
    sys.exit(main())
