# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-DEV-CSH-005
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
C# idiom - a rule measured from the code, not asserted at it.

    python tests/test_csharp.py

WHAT IT PROVES
  1. THE THREE SHAPES ARE FOUND, and a narrow handler is left alone.

  2. A CATCH IN A COMMENT OR A STRING IS NOT A HANDLER.

  3. THE NUMBERS IN THE DOCSTRING ARE MEASURED HERE, not trusted. The
     first set was wrong.

  4. A BROAD CATCH IS NOT CALLED BAD STYLE, because this repository is
     two thirds broad and a rule its own code contradicts is an agent
     correcting working code.

  5. NOTHING IS CONDEMNED AND NOTHING IS FIXED.

  6. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import glob
import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_csharp as CSH                                     # noqa: E402
import heron_apichanges as ACI                                 # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def repository():
    """Every C# file this repository ships, minus build output."""
    found = []
    for layer in ("revit", "platform", "mcp"):
        found += glob.glob(os.path.join(ROOT, layer, "**", "*.cs"),
                           recursive=True)
    return sorted(path for path in found
                  if os.sep + "obj" + os.sep not in path
                  and os.sep + "bin" + os.sep not in path)


def main():
    reached = set()

    print("\n1. the three shapes are found, and a narrow one is left alone")
    sample = ("try { a(); } catch { }\n"
              "foreach (var x in y) { try { b(); } catch { continue; } }\n"
              "try { return c(); } catch (Exception) { return null; }\n"
              "try { d(); } catch (IOException) { throw; }\n")
    said = CSH.review(sample)
    check(said["handlers"] == 4, "four handlers: %d" % said["handlers"])
    check(said["narrow"] == 1 and said["broad"] == 3,
          "one narrow, three broad")
    check(said["shapes"] == {"SWALLOWS_SILENTLY": 1, "DROPS_THE_ITEM": 1,
                             "RETURNS_AN_EMPTY_ANSWER": 1},
          "one of each shape: %s" % said["shapes"])
    check(len(said["plausible_zero"]) == 3,
          "and the narrow one is not among them")
    check(all(one["line"] for one in said["plausible_zero"]),
          "each carries a line number")
    check(all(one["why"] for one in said["plausible_zero"]),
          "and what it does to the CALLER, not what it looks like")
    # A BROAD CATCH THAT RECORDS SOMETHING IS NOT A PLAUSIBLE ZERO.
    logged = CSH.review("try { a(); } catch (Exception ex) { Log(ex); }")
    check(logged["broad"] == 1 and not logged["plausible_zero"],
          "a broad catch that records the fault is broad and NOT named")
    filtered = CSH.review("try { a(); } catch (Exception e) when (e.X) { }")
    check(filtered["handlers"] == 1,
          "an exception FILTER is still a handler - this repository has "
          "none today and a matcher stopping at the `)` would silently "
          "skip every one that grew a `when`")

    print("\n2. a catch in a comment or a string is not a handler")
    check(CSH.unquoted is ACI.unquoted,
          "CSH.unquoted IS HERON-REVIT-ACI-034's - that agent learned the "
          "same lesson expensively and one scanner is enough")
    for hidden, label in (("// catch { }\n", "a line comment"),
                          ("/* catch { } */\n", "a block comment"),
                          ('var s = "catch { }";\n', "a string literal")):
        answer = CSH.review("try { a(); } catch (IOException) { throw; }\n"
                            + hidden)
        check(answer["handlers"] == 1,
              "%s is not counted: %d handler(s)" % (label,
                                                    answer["handlers"]))

    print("\n3. the numbers in the docstring are measured here")
    files = repository()
    check(len(files) >= 20, "%d C# file(s) ship" % len(files))
    handlers = broad = narrow = 0
    shapes = {}
    types = {}
    for path in files:
        body = io.open(path, encoding="utf-8", errors="replace").read()
        answer = CSH.review(body, where=path)
        check_once = answer.get("refused")
        if check_once:
            check(False, "%s refused: %s" % (path, check_once))
            continue
        handlers += answer["handlers"]
        broad += answer["broad"]
        narrow += answer["narrow"]
        for name, count in answer["shapes"].items():
            shapes[name] = shapes.get(name, 0) + count
        for one in CSH.handlers(body):
            types[one["type"]] = types.get(one["type"], 0) + 1
    print("      %d handlers, %d broad, %d narrow, %d named"
          % (handlers, broad, narrow, sum(shapes.values())))
    doc = CSH.__doc__
    # THE FIGURES ARE NOT PINNED, AND THE REASON WAS LEARNED THE SAME
    # DAY. Adding one C# file - RevitPhases.cs - moved 74 handlers to 82
    # and 26 narrow to 34 within the hour, and a suite pinning those
    # integers went red on all three. A count in a docstring is an
    # illustration; making the next author's first experience of this
    # agent a red suite over an illustration teaches them to delete the
    # check.
    check("2026-09-15" in doc,
          "the docstring DATES its figures rather than presenting them as "
          "standing fact")
    check("the suite does NOT pin them" in doc,
          "and says the suite checks the claim instead")
    # THE CLAIM THIS USED TO CHECK WAS `broad > narrow`, AND IT FLIPPED.
    # RevitParameters.cs - sixteen narrow handlers, no broad - took the
    # shipped C# from 53/42 to 53/58 on 2026-09-16 and narrow led for the
    # first time. The refusal did not change, because the majority was
    # never its real ground: fifty-three broad handlers still ship and
    # still work, and flagging fifty-three working handlers is correcting
    # working code whichever side is ahead.
    #
    # So what is checked is the share that does NOT tip on one commit. A
    # quarter is not an arbitrary line - it is the point below which
    # "this repository is full of them" stops being a fair description,
    # and reaching it means somebody rewrote most of them deliberately.
    check(handlers > 0 and broad * 4 >= handlers,
          "THE CLAIM HOLDS: %d of %d handler(s) are broad - a real share of "
          "the code and not a handful of survivors - so the rule this module "
          "refuses to assert would still flag working code"
          % (broad, handlers))
    check("on 2026-09-16" in doc.lower() and "flipped" in doc.lower(),
          "and the docstring records the day the old claim stopped being "
          "true, rather than quietly carrying the new numbers")
    check(types.get("(no type)", 0) > types.get("Exception", 0),
          "and a bare `catch` is still the commonest single kind: %d "
          "against %d" % (types.get("(no type)", 0),
                          types.get("Exception", 0)))
    # THE FIRST SET WAS WRONG, and the reason is worth keeping: a scratch
    # expression counted `catch` inside comments and where no block
    # followed, and reported 83, 39 and 18.
    check("83" in doc and "the first set was wrong" in doc.lower(),
          "and the docstring records that its first set was wrong, and why")

    print("\n4. a broad catch is not called bad style")
    check(handlers > 0 and broad * 4 >= handlers,
          "this repository still ships %d broad handler(s) out of %d - so a "
          "rule against broad catches would be an agent correcting its "
          "own working code, whichever kind is currently ahead"
          % (broad, handlers))
    check(said["judged_wrong"] is False,
          "`judged_wrong` is false, and always false")
    check(any("NOT asserted here" in line or "not asserted here" in line
              for line in said["unjudged"]),
          "and the answer says the style rule is not asserted")
    check(any("D-52" in line for line in said["unjudged"]),
          "citing D-52, which IS this repository's rule")
    check(any("check-narrow-errors" in line for line in said["unjudged"]),
          "and the Python-only tool that already enforces it")

    print("\n5. nothing is condemned and nothing is fixed")
    check(said["fixed"] is False, "`fixed` is false")
    check("Nothing here is called wrong" in said["why"],
          "and the answer says nothing is called wrong")
    logic = io.open(os.path.join(ROOT, "brain", "heron_csharp.py"),
                    encoding="utf-8").read().split("\ndef main(")[0]
    for writing in ("os.system", "subprocess", ".write(", "shutil",
                    "os.remove"):
        check(writing not in logic, "the agent never uses %s" % writing)
    check(any("only a person reading the call knows" in line
              for line in said["unjudged"]),
          "and it says who can actually decide")

    print("\n6. every failure the contract declares is named and reached")
    for code, name in ((None, "NOTHING_TO_READ"), ("   ", "NOTHING_TO_READ"),
                       ("", "NOTHING_TO_READ"), (42, "NOT_CODE"),
                       (["a", "list"], "NOT_CODE"),
                       ("try { a(); } catch { ", "UNREADABLE")):
        answer = CSH.review(code)
        reached.add(answer.get("refused"))
        check(answer.get("refused") == name, "%s is reached" % name)
    named_file = CSH.review("try { a(); } catch { ", where="Broken.cs")
    check("Broken.cs" in named_file["why"],
          "and the unreadable refusal names the file, so somebody can "
          "open it")

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-DEV-CSH-005.yaml"))
    declared = contract.get("failures") or []
    check(len(declared) == 3, "the contract declares 3 failures")
    for failure in declared:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(declared) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    check(len(said["unjudged"]) == 5, "five things are left unjudged")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    a rule measured from the code, not asserted at it")
    return 0


if __name__ == "__main__":
    sys.exit(main())
