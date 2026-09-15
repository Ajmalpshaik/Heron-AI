# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-IMP-FEX-004
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Fragment extraction - a function is a unit, and being used twice is the
only evidence of reuse that is arithmetic.

    python brain/heron_harvest.py [folder]

WHAT IT IS FOR (docs/28, HERON-IMP-FEX-004)
--------------------------------------------
"Pulls REUSABLE implementation units out of existing code." T3, risk
READ. Step 4 of docs/00 s28's sixteen, and the step
HERON-IMP-MAIN-001 measured an import stopping one short of.

TWO WORDS IN THAT ROW, AND ONLY ONE OF THEM IS MECHANICAL
-----------------------------------------------------------
    IMPLEMENTATION UNIT   a function. Parsed, not guessed.
    REUSABLE              a judgement. Not made here.

Finding the functions in a Python file is `ast` - the language's own
parser, exact, and it cannot be wrong about where a function starts and
ends. Deciding that one of them is worth keeping is language, and D-34
says Heron builds nothing for that.

So every unit comes back with the EVIDENCE a person or a host would
weigh, and nothing is picked.

THE ONE SIGNAL THAT IS ARITHMETIC
-----------------------------------
A function CALLED FROM MORE THAN ONE PLACE in the folder has already
been reused, by the person who wrote it. That is not an opinion about
reusability - it is a count, and it is the strongest evidence in the
whole import. It is reported as `calledBy`, and a function nothing calls
is reported as exactly that rather than as a poor candidate.

WHY PYTHON, AND WHAT HAPPENS TO EVERYTHING ELSE
-------------------------------------------------
docs/10 s5 names the folders this feature exists for: `AJ-Tools`,
`PyRevit-Tools`, `AEB-Tools`. pyRevit tooling is Python, so Python is
where the user's own library actually lives.

Heron has no C# parser on this side. A `.cs` file is reported as a file
whose units could not be read, naming what is missing - NOT skipped, and
NOT guessed at with a brace scan. A brace scan gets a string containing
`{` wrong, and it gets it wrong silently.

needs AND provides ARE COMPUTED, AND THAT IS THE USEFUL PART
-------------------------------------------------------------
D-29 gives a fragment a contract of `needs` and `provides`, and
docs/09's own argument is that this makes composition checkable before
Revit is involved. For a Python function both are static facts:

    needs       names it READS that it never binds, and that are not
                builtins or its own parameters
    provides    names it BINDS that outlive it - its parameters are not
                among them, and neither is anything local it deletes

That is real analysis rather than a reading of the prose, and it is
what turns somebody's script into something that can be composed.

IT NAMES NOTHING AND WRITES NOTHING
-------------------------------------
No id, no capability, no file. HERON-IMP-REN-010 already argued that
one out: deriving `FRG-ELE-042` from `CountDucts.py` means picking an
area and a number, and turning `CountDucts` into `COUNT_DUCTS` is naming
what code DOES, which is language.

A PYTHON FUNCTION IS NOT A FRAGMENT YET
-----------------------------------------
Heron's fragments are C# - `impl/*/fragment.cs`, handed to Roslyn and
run inside Revit (D-28). A harvested pyRevit function is a candidate
that still has to be migrated, and that is HERON-IMP-MIG-009's row. The
answer says so rather than letting a manifest imply otherwise.
"""

from __future__ import annotations

import ast
import builtins
import io
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_walk as WALK  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# The one extension this side can parse, and the reason is the parser
# rather than a preference.
PARSED = ".py"

WHY_NOT_PARSED = (
    "Heron has no parser for this on the Python side. A brace scan would "
    "get a string containing `{` wrong and would get it wrong silently, so "
    "the file is reported and its units are not guessed at")

NOT_A_FRAGMENT_YET = (
    "Heron's fragments are C# handed to Roslyn inside Revit (D-28). This "
    "is a candidate that still has to be migrated, which is "
    "HERON-IMP-MIG-009's row")

# THE `builtins` MODULE, NOT `__builtins__`. The latter is a module when
# this file is run directly and a DICT when it is imported, so the demo
# passed and the first import raised. Found by the suite, which is the
# only one of the two that imports it.
_BUILTINS = frozenset(dir(builtins))


class _Names(ast.NodeVisitor):
    """Every name a piece of code reads, and every name it binds."""

    def __init__(self):
        self.read = []
        self.bound = []

    def visit_Name(self, node):
        where = self.bound if isinstance(node.ctx, (ast.Store, ast.Del)) \
            else self.read
        if node.id not in where:
            where.append(node.id)
        self.generic_visit(node)

    def visit_arg(self, node):
        if node.arg not in self.bound:
            self.bound.append(node.arg)
        self.generic_visit(node)

    def visit_alias(self, node):
        name = (node.asname or node.name).split(".")[0]
        if name not in self.bound:
            self.bound.append(name)


def _parameters(node):
    """Every parameter name, in order, including the starred ones."""
    args = node.args
    out = [one.arg for one in
           list(getattr(args, "posonlyargs", [])) + list(args.args)]
    if args.vararg:
        out.append(args.vararg.arg)
    out.extend(one.arg for one in args.kwonlyargs)
    if args.kwarg:
        out.append(args.kwarg.arg)
    return out


def contract_of(node):
    """
    (needs, provides) for one function - static facts, not a reading.

    `needs` is what it reads and never binds, minus its parameters and
    the builtins. `provides` is what it binds that is not a parameter.
    """
    seen = _Names()
    for child in node.body:
        seen.visit(child)
    parameters = _parameters(node)

    needs = [name for name in seen.read
             if name not in seen.bound
             and name not in parameters
             and name not in _BUILTINS]
    provides = [name for name in seen.bound if name not in parameters]
    return needs, provides


def units(path):
    """
    Every function in one Python file, with what it needs and provides.

    Returns (units, problem). A file that does not parse is a problem
    with a reason, never an empty list - "no functions" and "this is not
    valid Python" are different answers.
    """
    try:
        source = io.open(path, encoding="utf-8", errors="replace").read()
    except (IOError, OSError) as error:
        return [], "could not be opened: %s" % (error.strerror or error)

    try:
        tree = ast.parse(source)
    except SyntaxError as error:
        return [], ("does not parse as Python: %s at line %s"
                    % (error.msg, error.lineno))

    lines = source.split("\n")
    out = []
    for node in ast.walk(tree):
        if not isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef)):
            continue
        needs, provides = contract_of(node)
        last = getattr(node, "end_lineno", node.lineno)
        out.append({
            "name": node.name,
            "recursive": _called_inside(node, node.name),
            "at": node.lineno,
            "lines": max(1, last - node.lineno + 1),
            "parameters": _parameters(node),
            "needs": needs,
            "provides": provides,
            "says": ast.get_docstring(node) or None,
            "code": "\n".join(lines[node.lineno - 1:last]),
        })
    return out, None


def _called_inside(node, name):
    """How many times a function calls ITSELF, by the parser rather than
    by counting characters.

    The first version counted `name(` in the function's own source, and
    the DEF LINE matched - so every function looked as though it called
    itself once, and a helper used by two scripts came back as used by
    one. Found by running it: `collect` was called from ducts.py and
    sheets.py and reported calledBy 1.
    """
    seen = 0
    for child in ast.walk(node):
        if not isinstance(child, ast.Call):
            continue
        called = child.func
        if (getattr(called, "id", None) or getattr(called, "attr", None)) \
                == name:
            seen += 1
    return seen


def _calls(path):
    """Every name called anywhere in one file. Counts, not judgements."""
    try:
        tree = ast.parse(io.open(path, encoding="utf-8",
                                 errors="replace").read())
    except (IOError, OSError, SyntaxError):
        return []
    out = []
    for node in ast.walk(tree):
        if not isinstance(node, ast.Call):
            continue
        called = node.func
        name = getattr(called, "id", None) or getattr(called, "attr", None)
        if name:
            out.append(name)
    return out


def harvest(folder, walked=None):
    """
    {harvested, units, unparsed} - or a refusal. Nothing is named,
    nothing is written and nothing is picked.
    """
    if walked is None:
        walked = WALK.walk(folder)
    if not walked or not walked.get("walked"):
        return {"harvested": False,
                "refused": (walked or {}).get("refused", "NOTHING_TO_HARVEST"),
                "why": (walked or {}).get(
                    "why", "no folder was handed in. HERON-IMP-FIL-002 walks "
                           "one and this reads what it found.")}

    root = walked.get("root")
    python = [card for card in walked["files"]
              if card["extension"] == PARSED]
    if not python:
        return {"harvested": False, "refused": "NOTHING_TO_HARVEST",
                "why": "the walk found no %s file. Heron has no parser for "
                       "anything else on this side, and %d file%s of other "
                       "kinds were found."
                       % (PARSED, len(walked["files"]),
                          "" if len(walked["files"]) == 1 else "s")}

    # HOW OFTEN EACH NAME IS CALLED, ACROSS THE WHOLE FOLDER. This is the
    # one piece of evidence about reuse that is arithmetic, so it is
    # counted before anything is judged - and it is counted over every
    # Python file, because a helper called from a sibling script is
    # exactly the case that matters.
    called = {}
    for card in python:
        for name in _calls(os.path.join(root, card["at"])
                           if root else card["at"]):
            called[name] = called.get(name, 0) + 1

    found, unparsed = [], []
    for card in python:
        path = os.path.join(root, card["at"]) if root else card["at"]
        inside, problem = units(path)
        if problem:
            unparsed.append({"at": card["at"], "why": problem})
            continue
        for one in inside:
            # ITS OWN RECURSIVE CALLS ARE NOT SOMEBODY ELSE USING IT.
            out = max(0, called.get(one["name"], 0) - one["recursive"])
            found.append(dict(one, file=card["at"], calledBy=out,
                              reusedAlready=out > 1))

    others = [card for card in walked["files"]
              if card["extension"] != PARSED]
    for card in others:
        unparsed.append({"at": card["at"],
                         "why": "%s files: %s" % (card["extension"] or
                                                  "extensionless",
                                                  WHY_NOT_PARSED)})

    reused = [one for one in found if one["reusedAlready"]]
    return {
        "harvested": True,
        "root": root,
        "of": len(found),
        "units": sorted(found, key=lambda one: (-one["calledBy"],
                                                one["file"], one["at"])),
        "reusedAlready": [one["name"] for one in reused],
        "unparsed": unparsed,
        "named": False,
        "asks": [{
            "question": "which of these %d unit%s is worth keeping, and what "
                        "does each one DO? %d %s already called from more "
                        "than one place, which is the only evidence of reuse "
                        "here that is a count rather than an opinion."
                        % (len(found), "" if len(found) == 1 else "s",
                           len(reused),
                           "is" if len(reused) == 1 else "are"),
            "units": [{"name": one["name"], "file": one["file"],
                       "lines": one["lines"], "calledBy": one["calledBy"],
                       "needs": one["needs"], "provides": one["provides"],
                       "says": one["says"]} for one in found],
        }],
        "why": "%d unit%s in %d Python file%s. %d already called from more "
               "than one place. %d file%s could not be parsed here. Nothing "
               "was named and nothing was picked."
               % (len(found), "" if len(found) == 1 else "s", len(python),
                  "" if len(python) == 1 else "s", len(reused),
                  len(unparsed), "" if len(unparsed) == 1 else "s"),
        "unjudged": [
            "WHICH UNITS ARE REUSABLE. A function is an implementation "
            "unit, which is parsing; `reusable` is a judgement, and D-34 "
            "says Heron builds nothing for that. Every unit comes back "
            "with its evidence and nothing was picked.",
            ("%d UNIT%s ALREADY CALLED FROM MORE THAN ONE PLACE: %s. That "
             "is the one signal here that is arithmetic - somebody has "
             "already reused it. A unit nothing calls is reported as that "
             "and not as a poor candidate."
             % (len(reused), "" if len(reused) == 1 else "S",
                ", ".join(sorted(one["name"] for one in reused)))
             if reused else
             "nothing in this folder is called from more than one place, so "
             "the one arithmetic signal says nothing here."),
            "needs AND provides ARE COMPUTED, NOT READ OUT OF THE PROSE. "
            "`needs` is what a function reads and never binds, minus its "
            "parameters and the builtins; `provides` is what it binds that "
            "is not a parameter. That is what makes composition checkable "
            "before Revit is involved (D-29).",
            ("%d FILE%s COULD NOT BE PARSED HERE, named rather than "
             "skipped: %s." % (len(unparsed),
                               "" if len(unparsed) == 1 else "S",
                               ", ".join(sorted(set(
                                   card["at"].rsplit(".", 1)[-1]
                                   if "." in card["at"] else "(none)"
                                   for card in unparsed))))
             if unparsed else
             "every file the walk carried forward was Python and parsed."),
            "NOTHING WAS NAMED. No id, no capability - "
            "HERON-IMP-REN-010 already argued that out, and deriving "
            "COUNT_DUCTS from CountDucts is naming what code DOES.",
            "A PYTHON FUNCTION IS NOT A FRAGMENT YET. %s"
            % NOT_A_FRAGMENT_YET,
        ],
    }


def main(argv):
    import shutil
    import tempfile

    print("FRAGMENT EXTRACTION   a function is a unit; reusable is a "
          "judgement")
    print("=" * 72)

    where = argv[0] if argv else None
    made = None
    if not where:
        made = where = tempfile.mkdtemp(prefix="heron-harvest-")
        with io.open(os.path.join(where, "ducts.py"), "w",
                     encoding="utf-8") as handle:
            handle.write(
                # NOT THE REAL REVIT API NAMESPACE - check-structure.py
                # refuses that one anywhere outside revit/. The fixture
                # only needs an import the function does not bind.
                "from revit_api import FilteredElementCollector\n"
                "\n\n"
                "def collect(doc, category):\n"
                '    """Every element of one category."""\n'
                "    return FilteredElementCollector(doc)\\\n"
                "        .OfCategory(category).ToElements()\n"
                "\n\n"
                "def count_ducts(doc):\n"
                "    ducts = collect(doc, OST_DuctCurves)\n"
                "    return len(ducts)\n")
        with io.open(os.path.join(where, "sheets.py"), "w",
                     encoding="utf-8") as handle:
            handle.write(
                "def tag_sheets(doc):\n"
                "    sheets = collect(doc, OST_Sheets)\n"
                "    for sheet in sheets:\n"
                "        sheet.Name = PREFIX + sheet.Name\n"
                "    return sheets\n")
        with io.open(os.path.join(where, "broken.py"), "w",
                     encoding="utf-8") as handle:
            handle.write("def oops(:\n    pass\n")
        with io.open(os.path.join(where, "Tool.cs"), "w",
                     encoding="utf-8") as handle:
            handle.write("public class Tool { }\n")

    try:
        answer = harvest(where)
        print("\n%s" % answer["why"])
        print("\n%-14s %-12s %5s %6s  %s"
              % ("unit", "file", "lines", "called", "needs"))
        for one in answer["units"]:
            print("  %-12s %-12s %5d %6d  %s"
                  % (one["name"], one["file"], one["lines"], one["calledBy"],
                     ", ".join(one["needs"]) or "-"))
            print("  %-12s %-12s              provides %s"
                  % ("", "", ", ".join(one["provides"]) or "-"))
        for card in answer["unparsed"]:
            print("\n  NOT PARSED  %-14s %s" % (card["at"], card["why"][:44]))
        for ask in answer["asks"]:
            print("\n  ASKS  %s" % ask["question"][:66])

        print("\nrefused")
        for bad in (None, os.path.join(ROOT, "no-such-folder"),
                    os.path.join(ROOT, "docs")):
            said = harvest(bad)
            print("  %-22s %s" % (said["refused"], said["why"][:42]))

        print("\nwhat this agent does not judge")
        for line in answer["unjudged"]:
            print("  - %s" % line)
    finally:
        if made:
            shutil.rmtree(made, ignore_errors=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
