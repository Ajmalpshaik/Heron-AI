# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The Revit validation gate checklist - the checks that FIRE, and the ones that must not.

    python tests/test_revit_gate.py

A checklist over 360 fragments is only worth running if a check that raises 114
rows and a check that raises 1 are both correct. The 1 is the finding; the 114
is the shape of the library. A check that raises 310 - as the links question did
in its first form - is neither, and this file holds the cases that keep them
apart.

WHAT IT PROVES
  1. The questions are the master architecture document's fourteen, in its
     order. Renumbering them silently would make every past finding
     unreadable.
  2. The unit check fires on a length with no stated unit and stays quiet on
     one that says "internal feet". That check found the single real defect in
     the library and its value is entirely in not crying wolf.
  3. A fragment that opens its OWN Transaction is caught. None does today, so
     this is a regression guard rather than a report - Golden Rule 16 gives the
     whole job one TransactionGroup and one undo.
  4. Only the four declared verdicts are ever produced. docs/24 collapsed six
     status vocabularies into two axes and a checklist quietly inventing a
     fifth verdict is how a seventh vocabulary starts.
  5. Question 13 answers differently for a reader and a writer. It said "Revit
     itself refuses any model change" for every fragment until this was
     written, which was the read-only story told about a MODIFY fragment
     months after the write path landed.

WHAT IT DOES NOT PROVE. That a fragment is correct. Nothing static can, and the
tool's own docstring is blunt about the attempt that proved it: searching for
`.Create(` flagged three READ fragments and all three were geometry built in
memory.
"""

import io
import os
import sys
import shutil
import tempfile
import importlib.util

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FAILURES = []


def load(fragments=None):
    path = os.path.join(ROOT, "tools", "check-revit-gate.py")
    spec = importlib.util.spec_from_file_location("check_revit_gate", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    if fragments is not None:
        module.FRAGMENTS = fragments
    return module


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def make(root, slug, yaml_body, cs_body):
    folder = os.path.join(root, slug)
    os.makedirs(os.path.join(folder, "impl", "any"))
    io.open(os.path.join(folder, "fragment.yaml"), "w",
            encoding="utf-8").write(yaml_body)
    io.open(os.path.join(folder, "impl", "any", "fragment.cs"), "w",
            encoding="utf-8").write(cs_body)


BASE = """id: %s
capability: DO_A_THING
risk: %s
revit: ["2024"]
contract:
  needs:
    - name: doc
      type: Document
%s
  provides:
    - name: done
      type: int
utterances:
  - do a thing
"""

# Declares uidoc and not doc - the real zoom-to-elements shape.
UIDOC_ONLY = """id: %s
capability: DO_A_THING
risk: %s
revit: ["2024"]
contract:
  needs:
    - name: uidoc
      type: UIDocument
  provides:
    - name: done
      type: int
utterances:
  - do a thing
"""

DOUBLE = """    - name: distance
      type: double
      source: request
"""


# BASE always declares `doc`, which is right for most cases and useless for
# question 3 - the whole point there is a contract that does NOT declare one.
NODOC = """id: %s
capability: DO_A_THING
risk: %s
revit: ["2024"]
contract:
  needs:
    - name: views
      type: IList<View>
      source: request
  provides:
    - name: done
      type: int
utterances:
  - do a thing
"""


def verdicts_for(tool, root, slug):
    entries = tool.library()
    for entry in entries:
        if entry[1] == slug:
            return tool.ask(*entry)
    raise AssertionError("%s not loaded from %s" % (slug, root))


def main():
    print("1. The questions are the document's fourteen, in its order")
    print("-" * 66)
    tool = load()
    check(len(tool.QUESTIONS) == 14,
          "there are 14 (%d)" % len(tool.QUESTIONS))
    check(tool.QUESTIONS[0].startswith("Which Revit versions"),
          "1 is the release question")
    check(tool.QUESTIONS[10].startswith("Are units"),
          "11 is the units question - the one that found something")
    check(tool.QUESTIONS[12].startswith("Could the change"),
          "13 is the unexpected-modification question")

    work = tempfile.mkdtemp(prefix="heron-gate-")
    try:
        tool = load(work)

        print()
        print("2. The unit check fires only when the unit is unstated")
        print("-" * 66)
        make(work, "silent", BASE % ("FRG-T-001", "MODIFY", DOUBLE),
             "// moves things\nvar n = 0;\n")
        got = verdicts_for(tool, work, "silent")[10]
        check(got[0] == tool.LOOK,
              "a double with no unit anywhere is raised")
        check("NEVER SAYS WHAT UNIT" in got[1],
              "and the message says what is missing")

        make(work, "stated", BASE % ("FRG-T-002", "MODIFY", DOUBLE),
             "// `distance` is internal FEET.\nvar n = 0;\n")
        got = verdicts_for(tool, work, "stated")[10]
        check(got[0] == tool.ANSWERED,
              "'internal FEET' in the code settles it - no cry of wolf")

        make(work, "ratio",
             (BASE % ("FRG-T-003", "MODIFY", DOUBLE)).replace(
                 "      type: double\n",
                 "      type: double\n      # 0 to 1 along the run\n", 1),
             "var n = 0;\n")
        got = verdicts_for(tool, work, "ratio")[10]
        check(got[0] == tool.ANSWERED,
              "'0 to 1' states the domain, which is the unit answer for a ratio")

        make(work, "nodouble", BASE % ("FRG-T-004", "READ", ""), "var n = 0;\n")
        got = verdicts_for(tool, work, "nodouble")[10]
        check(got[0] == tool.ANSWERED,
              "a fragment taking no double has no unit to get wrong")

        print()
        print("3. A fragment opening its own Transaction is caught")
        print("-" * 66)
        make(work, "ownTx", BASE % ("FRG-T-005", "MODIFY", ""),
             "using (var t = new Transaction(doc, \"x\")) { t.Start(); }\n")
        got = verdicts_for(tool, work, "ownTx")[3]
        check(got[0] == tool.LOOK, "it is raised on question 4")
        check("Golden Rule 16" in got[1], "and Golden Rule 16 is named")

        got = verdicts_for(tool, work, "nodouble")[3]
        check(got[0] == tool.BY_DESIGN,
              "a fragment that opens none is BY DESIGN, not merely silent")

        print()
        print("3b. Question 3 asks about an UNDECLARED need, not a missing one")
        print("-" * 66)
        # It asked "does it declare a document" first, and raised all 42
        # fragments that legitimately work on what they are handed. Every one
        # was correct, so the check was crying wolf on the whole list.
        make(work, "handed", NODOC % ("FRG-T-006", "MODIFY"),
             "foreach (var v in views) { v.Scale = 2; }\n")
        got = verdicts_for(tool, work, "handed")[2]
        check(got[0] == tool.ANSWERED,
              "a fragment that needs no document and uses none is ANSWERED, "
              "not raised")

        make(work, "undeclared", NODOC % ("FRG-T-007", "MODIFY"),
             "var n = doc.GetElement(id);\n")
        got = verdicts_for(tool, work, "undeclared")[2]
        check(got[0] == tool.LOOK,
              "a fragment whose CODE uses doc without declaring it IS raised - "
              "an undeclared need cannot be bound")

        # Shaped like the real zoom-to-elements: uidoc IS declared, and doc is
        # derived from it. The first version of this case declared neither and
        # was raised for `uidoc` - correctly. The fixture was wrong, not the
        # tool, which is worth leaving here: a synthetic case that does not
        # match the real one proves something about the fixture.
        make(work, "derived", UIDOC_ONLY % ("FRG-T-008", "MODIFY"),
             "var doc = uidoc.Document;\nvar n = doc.GetElement(id);\n")
        got = verdicts_for(tool, work, "derived")[2]
        check(got[0] == tool.ANSWERED,
              "`var doc = uidoc.Document` is a LOCAL, not an undeclared need - "
              "zoom-to-elements is real, correct, and was the last false "
              "positive this check had")

        make(work, "inacomment", NODOC % ("FRG-T-009", "MODIFY"),
             "// Assumes `doc` and `elements` are in scope.\nvar n = 0;\n")
        got = verdicts_for(tool, work, "inacomment")[2]
        check(got[0] == tool.ANSWERED,
              "`doc` named in a HEADER COMMENT is not a use - almost every "
              "fragment has that line, so a check reading comments would find "
              "doc everywhere and mean nothing")

        print()
        print("3d. Question 7 raises an INCONSISTENCY, not every whole-model scan")
        print("-" * 66)
        WHOLE = "var all = new FilteredElementCollector(doc).ToElements();\n"
        VIEWNEED = ("    - name: view\n      type: View\n"
                    "      source: request\n")
        make(work, "scansall", BASE % ("FRG-T-013", "READ", ""), WHOLE)
        got = verdicts_for(tool, work, "scansall")[6]
        check(got[0] == tool.ANSWERED,
              "a whole-model scan on its own is NOT raised - it is usually the "
              "job, and raising all 114 was raising the shape of the library")
        check("proof" in got[1].lower() or "slow" in got[1].lower(),
              "but it still says what it costs a PROOF, which is where it "
              "actually matters")

        make(work, "hasviewscansall", BASE % ("FRG-T-014", "MODIFY", VIEWNEED),
             WHOLE)
        got = verdicts_for(tool, work, "hasviewscansall")[6]
        check(got[0] == tool.LOOK,
              "handed a view and never scoping to it IS raised - that is an "
              "inconsistency rather than a design")

        make(work, "hasviewscopes", BASE % ("FRG-T-015", "MODIFY", VIEWNEED),
             "var all = new FilteredElementCollector(doc, view.Id).ToElements();\n")
        got = verdicts_for(tool, work, "hasviewscopes")[6]
        check(got[0] == tool.ANSWERED,
              "and one that DOES scope to the view it was handed is fine")

        print()
        print("3c. Question 8 asks a READER about links, never a writer")
        print("-" * 66)
        COLLECTS = "var all = new FilteredElementCollector(doc).ToElements();\n"
        make(work, "readercollects", BASE % ("FRG-T-010", "READ", ""), COLLECTS)
        got = verdicts_for(tool, work, "readercollects")[7]
        check(got[0] == tool.LOOK,
              "a READ fragment that collects and never mentions links IS raised")

        make(work, "writercollects", BASE % ("FRG-T-011", "MODIFY", ""), COLLECTS)
        got = verdicts_for(tool, work, "writercollects")[7]
        check(got[0] == tool.ANSWERED,
              "a MODIFY one is NOT - a linked element belongs to another "
              "document and cannot be changed through this one, so there is "
              "nothing for it to miss")

        make(work, "nocollect", BASE % ("FRG-T-012", "READ", ""),
             "foreach (var v in views) { var n = v.Scale; }\n")
        got = verdicts_for(tool, work, "nocollect")[7]
        check(got[0] == tool.ANSWERED,
              "and a fragment that collects nothing has no link question at "
              "all - asking every fragment raised 310 of 360")

        print()
        print("3e. Question 12 asks what is nullable, not whether a guard exists")
        print("-" * 66)
        make(work, "handedlist", BASE % ("FRG-T-016", "READ", ""),
             "var count = elements.Count;\nvar countedNothing = count == 0;\n")
        got = verdicts_for(tool, work, "handedlist")[11]
        check(got[0] == tool.ANSWERED,
              "a fragment that counts a list it was handed needs no guard - "
              "count-elements, set-selection and group-and-count are all real "
              "and all were raised by the first version")

        make(work, "unguarded", BASE % ("FRG-T-017", "READ", ""),
             "var e = doc.GetElement(id);\nvar n = e.Name;\n")
        got = verdicts_for(tool, work, "unguarded")[11]
        check(got[0] == tool.LOOK,
              "one that calls GetElement and never checks IS raised")
        check("GetElement" in got[1],
              "and the message names WHICH call can hand back null")

        make(work, "guarded", BASE % ("FRG-T-018", "READ", ""),
             "var e = doc.GetElement(id);\nif (e != null) { var n = e.Name; }\n")
        got = verdicts_for(tool, work, "guarded")[11]
        check(got[0] == tool.ANSWERED, "and one that checks is fine")

        make(work, "castonly", BASE % ("FRG-T-019", "READ", ""),
             "var w = e as Wall;\nvar n = w.LevelId;\n")
        got = verdicts_for(tool, work, "castonly")[11]
        check(got[0] == tool.LOOK,
              "`as Wall` is nullable too - a cast that does not hold gives "
              "null rather than throwing")

        print()
        print("4. Question 13 knows a reader from a writer")
        print("-" * 66)
        reader = verdicts_for(tool, work, "nodouble")[12]
        writer = verdicts_for(tool, work, "silent")[12]
        check("NO TRANSACTION OPEN" in reader[1],
              "a READ is answered by the executor opening none")
        check("write.enabled" in writer[1] and "D-55" in writer[1],
              "a MODIFY is answered by the gate, the default and the rollback "
              "- not by pretending Revit refuses it")
        check(reader[1] != writer[1],
              "the two answers are genuinely different text")

        print()
        print("5. No fifth verdict is ever invented")
        print("-" * 66)
        allowed = {tool.ANSWERED, tool.BY_DESIGN, tool.LOOK, tool.NEEDS_RUN}
        seen = set()
        for entry in tool.library():
            for verdict, _detail in tool.ask(*entry):
                seen.add(verdict)
        check(seen <= allowed,
              "only the four declared verdicts appear (%s)"
              % ", ".join(sorted(seen)))
    finally:
        shutil.rmtree(work, ignore_errors=True)

    print()
    print("6. The real library, reported not asserted")
    print("-" * 66)
    tool = load()
    entries = tool.library()
    counts = {}
    for entry in entries:
        for i, (verdict, _d) in enumerate(tool.ask(*entry), 1):
            if verdict == tool.LOOK:
                counts[i] = counts.get(i, 0) + 1
    print("        %d fragments read" % len(entries))
    for i in sorted(counts):
        print("        Q%-3d %4d worth a look" % (i, counts[i]))
    check(len(entries) > 100,
          "the whole library was read, not a corner of it (%d)" % len(entries))
    check(counts.get(4, 0) == 0,
          "no fragment opens its own Transaction (%d) - Golden Rule 16 holds"
          % counts.get(4, 0))

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - the unit check fires on a silent length and stays quiet on")
    print("a stated one, a fragment opening its own transaction is caught, and")
    print("question 13 tells a reader from a writer.")
    print()
    print("It proves nothing about whether a fragment is CORRECT. Nothing")
    print("static can - the tool's own docstring records the attempt that")
    print("proved it.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
