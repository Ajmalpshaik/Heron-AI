#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   14
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A sheet is named by its NUMBER, the view in front says its id, and one of two
parameters that share a name is named by its built-in or its GUID. Runs
without Revit.

    python tests/test_sheet_by_number.py

WHY THIS EXISTS
---------------
Two gaps met on Revit 2020 (session 29500, add-in 0.3.0) on 2026-11-24, on a
real job model, 4355-BHVD-3D-60P00-BL003A, 15 sheets, three of them named
"Unnamed":

  * FRAGMENT-ISSUES row 5b-2015. revit_view_image with `view` empty answered
    "A picture of 'Unnamed' (DrawingSheet)", and `view=Unnamed` refused with
    "DrawingSheet: Unnamed" offered three times - none of them usable. So
    which sheet was in front could not be told.
  * Row 5b-2016. SELECT_BY_PARAMETER_VALUE refused every sheet for "Sheet
    Number", rightly - a sheet carries it twice - and "Sheet Number
    {SHEET_NUMBER}" answered "NOTHING carries it".

WHAT IT CHECKS, IN THE ONE PLACE A COMPILER CANNOT
--------------------------------------------------
The add-in and the fragment touch Autodesk types, so no test host here can
link them (test_binding_note.py gives the reason). This reads the C# as text,
comments taken out so a sentence cannot satisfy a check - the technique
test_view_type_words.py uses - and RUNS the one piece of Python, the
answer's words (`view_named` in mcp/server/heron_inventory.py), taken out of
the file by its syntax tree so no MCP SDK is needed:

  1. the binder's OneView reads a sheet number and a view id ONLY where no
     view carries the text as its name, and lists sheets by number;
  2. the picture tool's ViewNamed does the same through the same helpers, and
     its answer carries the view's id and a sheet's number, for a named view
     only;
  3. revit_views gives the view in front by id, and a sheet by number;
  4. the answer's words name a sheet by its number and any view by its id -
     and read exactly as before when an older add-in sends neither;
  5. SELECT_BY_PARAMETER_VALUE reaches "Name {BUILT_IN}" and "Name {GUID}"
     through one lookup that the shared-name guard asks too, a name as typed
     still through LookupParameter, and the refusal names both forms.

WHAT IT CANNOT DO: say that Revit resolves the sheet or the parameter. Those
runs are NEEDS-CHECKING Group QF.
"""

import ast
import io
import os
import re
import sys

for _stream in (sys.stdout, sys.stderr):
    try:
        _stream.reconfigure(encoding="utf-8", errors="replace")
    except (AttributeError, ValueError):
        pass

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ADDIN = os.path.join(ROOT, "revit", "Heron.Revit.Addin")
FRAGMENT = os.path.join(ROOT, "revit", "Heron.Revit.Addin", "RevitFragment.cs")
CAPTURE = os.path.join(ADDIN, "RevitCapture.cs")
VIEWS = os.path.join(ADDIN, "RevitViews.cs")
INVENTORY = os.path.join(ROOT, "mcp", "server", "heron_inventory.py")
SELECTOR = os.path.join(ROOT, "brain", "fragments", "select-by-parameter-value", "impl", "any",
                        "fragment.cs")

FAILED = []


def check(ok, said):
    print("  %s  %s" % ("ok  " if ok else "FAIL", said))
    if not ok:
        FAILED.append(said)


def body_of(text, signature):
    """One method, signature to closing brace; None when it is not there."""
    start = text.find(signature)
    if start < 0:
        return None
    opened = text.find("{", start)
    if opened < 0:
        return None
    depth = 0
    for at in range(opened, len(text)):
        if text[at] == "{":
            depth += 1
        elif text[at] == "}":
            depth -= 1
            if depth == 0:
                return text[start:at + 1]
    return None


def code_only(text):
    """The code without its comments, so a sentence cannot satisfy a check."""
    text = re.sub(r"//[^\n]*", "", text or "")
    return re.sub(r"/\*.*?\*/", "", text, flags=re.S)


def read(path):
    return io.open(path, encoding="utf-8").read()


def binder(fragment):
    print("\n1. the binder's OneView: a sheet by number, a view by id - only where no name decides")
    for signature in ("internal static ViewSheet SheetNumbered(", "internal static View ViewWithId(",
                      "internal static string ViewLabel("):
        check(body_of(fragment, signature) is not None, "RevitFragment has %s" % signature.strip("("))
    one = code_only(body_of(fragment, "private static object OneView("))
    check(one.find("ViewsNamed(doc, text)") >= 0
          and 0 <= one.find("ViewsNamed(doc, text)") < one.find("SheetNumbered(doc, text)"),
          "a view's own name is looked for before any sheet number")
    empty = one.find("if (found.Count == 0)")
    check(empty >= 0 and empty < one.find("SheetNumbered(doc, text)")
          and empty < one.find("ViewWithId(doc, text)"),
          "the number and the id are read only when no view carries the text as its name")
    check("view is ViewSheet ? ViewLabel(view)" in one,
          "sheets that share a name are listed by number in the refusal")
    check("!sheet.Id.Equals(byId.Id)" in one,
          "text that is one sheet's number and another view's id is refused, never read as either")
    ids = code_only(body_of(fragment, "internal static View ViewWithId("))
    check("view.Id.ToString() == asked" in ids and "new ElementId(" not in ids,
          "an id is matched as text - no ElementId built from a number (D-05)")
    label = code_only(body_of(fragment, "internal static string ViewLabel("))
    check('"Sheet " + number + " - " + name' in label,
          "a sheet is written \"Sheet <number> - <name>\"")
    sheet = code_only(body_of(fragment, "internal static ViewSheet SheetNumbered("))
    check('" - "' in sheet and "OrdinalIgnoreCase" in sheet and "anyCase == 1" in sheet,
          "and that is read back - the name after \" - \" must be the sheet's; two matching "
          "only in any case is no answer")


def picture(capture, views):
    print("\n2. the picture tool reads the same ways, and says which view it pictured")
    named = code_only(body_of(capture, "private static View ViewNamed("))
    check("RevitFragment.SheetNumbered(doc, name)" in named
          and "RevitFragment.ViewWithId(doc, name)" in named,
          "ViewNamed reads a sheet number and a view id through the binder's helpers")
    check(named.find("if (found.Count == 0)") < named.find("RevitFragment.SheetNumbered("),
          "only where no view carries the text as its name")
    check("view is ViewSheet ? RevitFragment.ViewLabel(view)" in named,
          "three sheets called \"Unnamed\" are offered by their numbers")
    check("byId.IsTemplate" in named, "a template's id is never pictured")
    exported = code_only(body_of(capture, "private static string Exported("))
    check('Json.Str("viewId", viewId)' in exported and 'Json.Str("sheetNumber"' in exported,
          "the answer carries the view's id and a sheet's number")
    check('if (mode == "view")' in exported
          and exported.find('if (mode == "view")') < exported.find("viewId = view.Id.ToString()"),
          "for a named view only - a temporary view's id is gone once rolled back")

    print("\n3. revit_views gives the view in front by id, and a sheet by number")
    check('Json.Str("activeViewId"' in views and 'Json.Str("activeSheetNumber", SheetNumberOf(active))' in views,
          "activeViewId and activeSheetNumber are in the answer")


def words(inventory):
    print("\n4. the answer's words")
    tree = ast.parse(inventory)
    found = [node for node in tree.body
             if isinstance(node, ast.FunctionDef) and node.name == "view_named"]
    check(len(found) == 1, "heron_inventory.py has view_named")
    if not found:
        return
    space = {}
    exec(compile(ast.Module(body=found, type_ignores=[]), INVENTORY, "exec"), space)
    view_named = space["view_named"]
    check(view_named("Level 1", "FloorPlan") == "'Level 1' (FloorPlan)",
          "an older add-in, sending neither, reads exactly as before")
    check(view_named("Level 1", "FloorPlan", "4521") == "'Level 1' (FloorPlan, view id 4521)",
          "any view is given with its id")
    said = view_named("Unnamed", "DrawingSheet", "9567876", "4355-BH-VD-VL224880584")
    check(said == "sheet 4355-BH-VD-VL224880584 - 'Unnamed' (DrawingSheet, view id 9567876)",
          "a sheet by its number first - found %r" % said)
    check("view_named(reply.get(\"view\"), reply.get(\"viewType\"), reply.get(\"viewId\")" in inventory
          and "reply.get(\"activeViewId\")" in inventory,
          "revit_view_image and revit_views both say it")
    check("a sheet NUMBER, or a view's element id" in inventory,
          "and the tool's own words tell the chat a sheet number and an id are taken")


def selector(code):
    print("\n5. SELECT_BY_PARAMETER_VALUE: one of two same-named parameters, by built-in or GUID")
    code = code_only(code)
    check("Guid.TryParse(inBraces, out guid)" in code
          and "Enum.TryParse(inBraces, false, out builtIn)" in code
          and "Enum.IsDefined(typeof(BuiltInParameter), builtIn)" in code,
          "\"Name {GUID}\" and \"Name {BUILT_IN}\" are both read")
    check("char.IsLetter(inBraces[0])" in code and "inBraces.IndexOf(',') < 0" in code,
          "a number, or two names joined, never reads as a built-in")
    check("one.Definition.Name == calledAs" in code,
          "only while that parameter is still called the name before the braces")
    shared = body_of(code, "Func<Element, bool> nameIsShared = element =>")
    check(shared is not None and "calledOn(element).Count" in shared and "calledOn(type).Count" in shared,
          "the shared-name guard counts through the same lookup the value is read by")
    look = body_of(code, "Func<Element, Parameter> lookUp = owner =>")
    check(look is not None and "if (!qualified) return owner.LookupParameter(wantName);" in look,
          "a name as typed is still Revit's own LookupParameter - version 4 unchanged")
    check(code.count("lookUp(element)") >= 2 and code.count("lookUp(type)") >= 2
          and "element.LookupParameter(wantName)" not in code,
          "the text and the number are both read through it, on the element and on its type")
    check('"Name the one meant as " + string.Join(" or ", reachedAs.ToArray())' in code,
          "the refusal for a doubled name names each one in the form that reaches it")


def main():
    print("A sheet by its number, the view in front by id, a doubled name by its second name "
          "(rows 5b-2015, 5b-2016)")
    try:
        fragment, capture, views = read(FRAGMENT), read(CAPTURE), read(VIEWS)
        inventory, code = read(INVENTORY), read(SELECTOR)
    except (IOError, OSError) as exc:
        print("COULD NOT RUN - %s" % exc)
        return 3
    binder(fragment)
    picture(capture, views)
    words(inventory)
    selector(code)
    print()
    if FAILED:
        print("FAILED - %d check(s):" % len(FAILED))
        for said in FAILED:
            print("  - %s" % said)
        return 1
    print("PASS    a sheet answers to its number, the view in front says its id, and a doubled "
          "parameter name can be named by its built-in or GUID")
    return 0


if __name__ == "__main__":
    sys.exit(main())
