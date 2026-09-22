#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   3
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  bridge
# See docs/29-metadata-standard.md

"""
What a capability asks its CALLER to supply, and how to type each one.

WHY THIS EXISTS. Every fragment already declares its contract - 396 of 396,
with no exceptions - in its own fragment.yaml, and `source: request` already
marks the values the caller has to write. None of that reached the person
doing the writing. heron_resolve reported risk, area, releases and proven
state, and said nothing at all about what to type, so the only way to learn a
capability's inputs was to call it wrong and read the complaint.

Measured on 2026-09-22, against SET_CATEGORY_GRAPHICS: four refusals to learn
one fixed fact. Call one gave the parameter NAMES, call two that a view wants
its exact Project Browser name, call three the semicolon syntax, call four the
nine override settings. Then it ran. The same capability's second and third
use cost one call each, because by then the recipe was known. A different
fragment taking the SAME nine settings cost the discovery again, because
nothing said they were the same.

So the cost is not in the work. It is in finding out how to ask for it, paid
once per fragment per conversation, and it is avoidable: the answer was on
disk the whole time.

WHERE THE WORDS COME FROM. The add-in is the authority on what a typed value
may look like - RevitFragment.cs turns each string into the type the contract
declares, and refuses with a sentence saying what to type instead. The hints
below are that same guidance, moved to where it can be read BEFORE the refusal
rather than after it. They are not a second opinion, and they must not drift
into one: tests/test_needs.py fails if any caller-supplied type in the library
has no hint here, so a new type shows up as a failing test rather than as a
blank line in an answer - which reads exactly like "nothing to type" and sends
the caller back to guessing.

NO PyYAML. The brain guards its own yaml import because a missing PyYAML must
not take the server down with it; a module in the answer path should not
reintroduce that risk for a block of text. The parse here is deliberately
small and reads only the two things this file needs.

WHY NOT `heron_contract`, WHICH IS WHAT THIS READS. That name is taken, by
brain/heron_contract.py - the AGENT contract, a different thing entirely.
heron_brain.py inserts brain/ at sys.path[0], AHEAD of mcp/server/, so a
module here sharing a name with one there is silently shadowed: the import
succeeds, binds the wrong module, and fails later at the first attribute.
That happened while this file was being written and cost a debugging round,
so the rule is worth stating - a new module under mcp/server/ must not take a
name that brain/ already uses.
"""

import os
import re

# The store keeps a fragment's folder RELATIVE to the repo root -
# "brain/fragments/set-category-graphics". Resolving that against the current
# working directory would make this file's answer depend on where the server
# happened to be started from: correct from the repo root, silently empty from
# anywhere else, and empty is indistinguishable here from "this fragment needs
# nothing typed". So it is anchored to this file's own location instead.
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


# ---------------------------------------------------------------------------
# How each type is typed
# ---------------------------------------------------------------------------
#
# Keyed by the INNER type - the list wrapper is stripped first, because
# IList<Category>, List<Category> and ICollection<Category> are one rule and
# were three in every message that ever explained them.

HINTS = {
    # -- plain values -------------------------------------------------------
    #
    # `string` maps to None: KNOWN to this table, with no format to give. The
    # declared type already says "string", and a line reading "any text" under
    # it is a row of noise - `create-pipe-segment` takes six of them. None and
    # ABSENT are different answers here and `known()` is what tells them
    # apart, so a type nobody has taught this file still fails its test.
    "string": None,
    "int": "digits only - 250, not 250mm",
    "double": "digits only - 250 or 250.5, not 250mm",
    "bool": "true or false",

    # -- named things in the model -----------------------------------------
    #
    # Each of these is looked up BY NAME in the open model, so the answer to
    # "what do I type" is always "what the Project Browser shows", and the
    # refusal when it is wrong lists what the model actually has.
    "View": 'the exact Project Browser name - "1 - Mech". '
            'Two views may share one, and then it is "FloorPlan: L2"',
    "Level": "the level name, exactly as the Project Browser shows it",
    "Category": 'a category name - "Walls"',
    "BuiltInCategory": 'a category name - "Walls"',
    "Phase": "the phase name",
    "Material": "the material name",
    "FilterElement": "the filter's name",
    "FamilySymbol": "the type name, as the type selector shows it",
    "FamilyInstance": "the family instance's type name",
    "SpatialElement": "the room or space name",
    "RevitLinkInstance": "the linked model's name",
    "View3D": "the 3D view's name",
    "ViewDuplicateOption": "Duplicate, WithDetailing or AsDependent",
    "ForgeTypeId": "the unit or spec name",
    "ParameterValue": "the value to store, written as the parameter reads it",

    # -- element types, all one rule ---------------------------------------
    "WallType": "the wall type's name",
    "FloorType": "the floor type's name",
    "CeilingType": "the ceiling type's name",
    "RoofType": "the roof type's name",
    "FilledRegionType": "the filled region type's name",
    "HostObjAttributes": "the host type's name",
    "MEPCurveType": "the duct or pipe type's name",
    "DuctType": "the duct type's name",
    "FlexDuctType": "the flex duct type's name",
    "PipeType": "the pipe type's name",
    "MechanicalSystemType": "the mechanical system type's name",
    "PipingSystemType": "the piping system type's name",
    "MEPSystemType": "the system type's name",

    # -- values with no name to look up ------------------------------------
    #
    # Built from what was typed rather than resolved, so the SHAPE is the
    # whole rule and there is nothing to list on a wrong answer.
    "XYZ": 'three numbers in MILLIMETRES, comma separated - "0,0,2700"',
    "Color": 'three numbers 0-255, comma separated - "255,0,0" is red',
    "OverrideGraphicSettings":
        'name=value, semicolons between - "halftone=true; transparency=50". '
        "Takes halftone, transparency, detail-level, projection-line-colour, "
        "cut-line-colour, surface-colour, cut-colour, projection-line-weight "
        "and cut-line-weight",

    # -- geometry written as points ----------------------------------------
    "Line": 'two points - a SEMICOLON between the ends, commas between their '
            'three MILLIMETRE ordinates - "0,0,0; 5000,0,0"',
    "Curve": 'a line as two points - "0,0,0; 5000,0,0"',

    # -- tables ------------------------------------------------------------
    #
    # Spelled without spaces because FromRequest strips them before it
    # compares; the library declares these WITH a space and matches anyway.
    "IDictionary<string,double>":
        'name=value, semicolons between - "supply=250; return=200"',
    "IDictionary<string,string>":
        'name=value, semicolons between - "Level 1=Ground; Level 2=First"',

    # -- the ones that are refused, said plainly ---------------------------
    #
    # An absent type looks identical to an overlooked one, so these say WHY
    # rather than going missing. Reference is refused BY NAME in
    # RevitFragment.cs. Arc and IFCVersion are not refused by name - they have
    # no branch in FromRequest at all, which reads to a caller as the same
    # thing and is worth saying before the call rather than after it.
    "Reference": "CANNOT BE TYPED - a Reference is a face, picked with the "
                 "mouse. No text names one",
    "Arc": "CANNOT BE TYPED - FromRequest has no rule for an Arc",
    "IFCVersion": "CANNOT BE TYPED - FromRequest has no rule for an "
                  "IFCVersion",
    "Element": "an element TYPE, by name. A specific instance has no name of "
               "its own - use the selection instead",
    "ElementId": "NAME the thing, never its number - the id is taken from "
                 "whatever the name finds",
}

# Separators, where a list is written differently from "comma separated".
# Everything not named here splits on commas, which is Parts() in the add-in.
LIST_NOTE = {
    "XYZ": "semicolons between points, commas between their three ordinates",
    "ElementId": "comma separated, or the word `selected` for the whole "
                 "selection",
}

_LIST = re.compile(r"^(?:I?List|ICollection|IEnumerable)<(.+)>$")


def _inner(declared):
    """
    The type inside a list wrapper, and whether there was one.

    SPACES ARE STRIPPED FIRST, because FromRequest strips them before it
    compares - `(type ?? "").Replace(" ", "")`. The library declares
    `IDictionary<string, double>` with a space and the add-in matches it
    anyway, so a hint table that kept the space would report a gap on a type
    that is in fact handled, which is the one direction this file must not be
    wrong in.

    Nested lists unwrap once only - IList<IList<XYZ>> is points in pairs, and
    its rule is the XYZ rule with a separator on top, not a different type.
    """
    flat = (declared or "").replace(" ", "")
    match = _LIST.match(flat)
    if not match:
        return flat, False
    inside = match.group(1)
    again = _LIST.match(inside)
    return (again.group(1) if again else inside), True


def known(declared):
    """
    Whether this file has been taught this type at all.

    Separate from `hint` because they answer different questions and the test
    needs the first one. A type with nothing useful to say about its format
    (`string`) and a type nobody has taught this file both give no text; only
    the second is a defect, and only this tells them apart.
    """
    return _inner(declared)[0] in HINTS


def hint(declared):
    """
    How to type one declared type, or None when there is nothing to add.

    None is returned in two cases that look the same here and are told apart
    by `known`: a type whose format needs no explaining, and a type this file
    has never heard of. Both print nothing - a plausible-looking hint invented
    for an unknown type is the one failure mode worse than the silence it
    replaces.

    A LIST STILL GETS ITS SEPARATOR even when the inner type has no text of
    its own: how to write several is format, whatever the one is.
    """
    inner, is_list = _inner(declared)
    if inner not in HINTS:
        return None
    text = HINTS[inner]
    if not is_list:
        return text
    note = LIST_NOTE.get(inner, "comma separated")
    return note if text is None else "%s. %s" % (text, note)


# ---------------------------------------------------------------------------
# Reading what one fragment declares
# ---------------------------------------------------------------------------

_CONTRACT = re.compile(r"\ncontract:\n(.*?)(?=\n[a-zA-Z][\w-]*:\s*\n|\Z)", re.S)
_NEEDS = re.compile(r"[ \t]*needs:\n(.*?)(?=\n[ \t]{2}[a-z]|\Z)", re.S)
_FIELD = re.compile(r"^[ \t]*(name|type|source):[ \t]*(.+?)[ \t]*$", re.M)


def _text(folder):
    """
    One fragment.yaml, read with its line endings normalised at the door.

    autocrlf is on system-wide here, so a checkout carries CRLF and anything
    matching on "\\n" downstream sees a stray carriage return inside every
    captured value. Normalising at the READ is the only place it can be done
    once.
    """
    if not folder:
        return None
    path = os.path.join(folder, "fragment.yaml")
    if not os.path.isabs(folder) and not os.path.exists(path):
        path = os.path.join(ROOT, folder, "fragment.yaml")
    if not os.path.exists(path):
        return None
    with open(path, encoding="utf-8", errors="replace") as handle:
        return handle.read().replace("\r\n", "\n").replace("\r", "\n")


def needs(folder):
    """
    The values this fragment's CALLER has to supply, in declared order.

    Only `source: request` is returned. A need sourced from the model or from
    ambient state is Heron's to find, and listing it would tell the caller to
    type something that is not theirs to type - which is the same wrong turn
    as not listing what is.

    Returns [] when the folder is missing or declares nothing. An empty list
    means "nothing for you to supply", which is a legitimate answer and is
    printed as one.
    """
    text = _text(folder)
    if not text:
        return []

    block = _CONTRACT.search("\n" + text)
    if not block:
        return []
    found = _NEEDS.search(block.group(1))
    if not found:
        return []

    out = []
    # Each entry starts at a "- name:" and runs to the next one.
    for chunk in re.split(r"\n[ \t]*-[ \t]+(?=name:)", "\n" + found.group(1)):
        fields = dict(_FIELD.findall(chunk))
        if fields.get("source") != "request" or not fields.get("name"):
            continue
        declared = (fields.get("type") or "").strip().strip('"\'')
        out.append({"name": fields["name"].strip().strip('"\''),
                    "type": declared,
                    "hint": hint(declared),
                    "known": known(declared)})
    return out


def block(folder, indent="  "):
    """
    The `takes` lines for one fragment, ready to print, or [] if it needs
    nothing typed.

    Formatted here rather than at the call site because heron_resolve is not
    the only answer that should carry it, and a second copy of this layout
    would drift from the first.
    """
    rows = needs(folder)
    if not rows:
        return []

    width = max(len(r["name"]) for r in rows)
    lines = ["%stakes  %d value(s) you supply:" % (indent, len(rows))]
    for row in rows:
        lines.append("%s  %-*s  %s" % (indent, width, row["name"], row["type"]))
        if row["hint"]:
            lines.append("%s  %-*s  %s" % (indent, width, "", row["hint"]))
    return lines
