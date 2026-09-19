#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The job-file generator, checked without Revit.

    python tests/test_generate_jobs.py

WHY THIS ONE HAS A TEST. The same reason `tests/test_batch_prove.py` does: it
CONCLUDES. It decides which fragments can be attempted, which need the write
path, and what their inputs are called - and every one of those is a decision
somebody would otherwise make by reading, which is where the six mistyped input
names of 2026-09-09 came from.

THE TEST THAT MATTERS MOST IS THE FIRST ONE. `generate-jobs.py` carries a
transcription of the types `RevitFragment.FromRequest` will accept, because the
authority is C# inside the add-in and nothing in Python can call it. A
transcription drifts. This reads the branches out of the C# and fails when the
two disagree - which turns a copy that is remembered into a copy that is checked,
and those are different things.

Everything here runs on the real library and the real C#, and none of it touches
Revit. Reading files IS the whole job.
"""

import io
import os
import re
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "tools"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))

import yaml                                                       # noqa: E402

import heron_fragment as HF                                       # noqa: E402

# tools/generate-jobs.py is not an importable module name - the hyphen is right
# for a command and wrong for an import - so it is loaded by path, the same way
# `tests/test_batch_prove.py` loads the runner and the same way a person runs it.
try:
    import importlib.util as _util

    def _load(path, name):
        spec = _util.spec_from_file_location(name, path)
        module = _util.module_from_spec(spec)
        spec.loader.exec_module(module)
        return module
except ImportError:                                          # pragma: no cover
    import imp

    def _load(path, name):
        return imp.load_source(name, path)


GJ = _load(os.path.join(ROOT, "tools", "generate-jobs.py"), "generate_jobs")
BP = _load(os.path.join(ROOT, "tools", "batch-prove.py"), "batch_prove")

REVIT_FRAGMENT = os.path.join(ROOT, "revit", "Heron.Revit.Addin", "RevitFragment.cs")

FAILURES = []


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


class FakeFragment(object):
    """Only what the generator reads: a slug, a status, a risk and a contract."""

    def __init__(self, slug, needs=None, provides=None, risk="READ",
                 status="DRAFT"):
        self.slug = slug
        self.status = status
        self.data = {"risk": risk}
        self._needs = needs or []
        self._provides = provides or []

    def needs(self):
        return self._needs

    def provides(self):
        return self._provides


# ---------------------------------------------------------------------------
# The transcription, checked against the C# it was transcribed from
# ---------------------------------------------------------------------------

def accepted_by_revit():
    """The types `FromRequest` has a branch for, read out of the add-in.

    The method ends its accepting half at a marker comment - `named refusals` -
    below which `XYZ` and `ElementId` are matched only in order to be REFUSED by
    name. Splitting there is what keeps a refusal from being read as an
    acceptance.
    """
    source = io.open(REVIT_FRAGMENT, encoding="utf-8").read()
    body = source[source.index("private static object FromRequest"):
                  source.index("private static object Shape")]
    accepting = body[:body.index("---- named refusals")]
    return set(re.findall(r'wanted\s*==\s*"([^"]+)"', accepting))


def test_receivable_agrees_with_the_add_in():
    print("What this tool thinks Revit accepts is what Revit accepts")

    theirs, ours = accepted_by_revit(), set(GJ.RECEIVABLE)

    missing = sorted(theirs - ours)
    check(not missing,
          "every type FromRequest accepts is in RECEIVABLE (missing: %s)"
          % (", ".join(missing) or "none"))

    # THE OTHER DIRECTION MATTERS MORE. A type here that the add-in does NOT
    # accept means a job emitted for a fragment that will refuse the moment it
    # reaches the model - the exact failure 3h.4 asked this tool to prevent.
    extra = sorted(ours - theirs)
    check(not extra,
          "nothing in RECEIVABLE is unknown to FromRequest (extra: %s)"
          % (", ".join(extra) or "none"))


def test_an_id_is_resolved_by_its_name_not_by_its_type():
    print("An ElementId need is judged on its NAME, the way the add-in judges it")

    # MEASURED BEFORE IT WAS WRITTEN, 2026-09-19. `create-view-filter` was
    # emitted as arrangeable, a job was built from it, and Revit refused the
    # moment it arrived: "'parameterId' is an id, and Heron resolves one by
    # NAMING the thing it belongs to ... There is no rule for this name yet".
    # The TYPE was receivable and the NAME was not, and only the type was being
    # asked about - so the job file read like a worklist and one slot bought
    # nothing. FRAGMENT-ISSUES row 139, the same shape as row 13.
    known = GJ.id_need_names()
    check(len(known) > 10,
          "the add-in's id names are READ OUT OF RevitFragment.cs, not typed "
          "here (%d found)" % len(known))
    check("levelId" in known and "categoryIds" in known,
          "and the set is the right one - levelId and categoryIds are in it")

    ok, _ = GJ.receivable("ElementId", "levelId")
    check(ok, "a name OneIdNamed has a rule for is receivable")

    ok, _ = GJ.receivable("ICollection<ElementId>", "categoryIds")
    check(ok, "and so is a LIST of them, because the list branch calls "
              "OneIdNamed once per part")

    ok, why = GJ.receivable("ElementId", "parameterId")
    check(not ok,
          "and `parameterId` is NOT - this is the case that was measured "
          "against a model rather than reasoned about")
    check("no rule for this one" in (why or ""),
          "and the reason says WHY rather than restating the type name")

    # THE DIRECTION THAT MATTERS. Nothing the tool EMITS may carry an id need
    # the add-in cannot resolve, or the job is refused on arrival - which is
    # worse than not emitting it, because a refusal costs a Revit slot and
    # reads as a fragment defect.
    unknown = []
    for frag in library().values():
        for need in frag.needs():
            if HF.need_source(need) != "request":
                continue
            declared = (need.get("type") or "").replace(" ", "")
            if "ElementId" not in declared:
                continue
            name = need.get("name")
            if name and name not in known:
                emitted, _ = GJ.receivable(declared, name)
                if emitted:
                    unknown.append("%s.%s (%s)" % (frag.slug, name, declared))
    check(not unknown,
          "no fragment in the library is offered with an id need the add-in "
          "has no rule for (%s)" % (", ".join(sorted(unknown)) or "none"))


def test_the_shapes_d54_refuses_are_refused_here():
    print("The shapes D-54 refuses are marked, not emitted")

    # SIX NAMES LEFT THIS LIST ON 2026-09-13, and the list is shorter on
    # purpose rather than by neglect. `ElementId` and its collection forms,
    # `View3D`, `Color` and `Material` are all RECEIVABLE now - an id is
    # resolved by NAMING the thing it belongs to and taking its `.Id`, which is
    # also why the add-in never constructs one and so never meets the 2024 type
    # change that was this row's whole reason. FRAGMENT-ISSUES row 28.
    #
    # They are asserted in the direction they moved, just below, rather than
    # quietly dropped - a shorter list is how a claim disappears unnoticed.
    for kind in ("IDictionary<ElementId, string>",
                 "IList<Reference>",
                 "IFCVersion"):
        ok, why = GJ.receivable(kind)
        check(not ok and why, "%s is refused, with a reason" % kind)

    # AND TWO MORE MOVED ON 2026-09-19, off the list directly above this one -
    # asserted in the direction they moved for the same reason every other
    # departure here is. `IList<Element>` is the SECOND SET, named by category
    # the way the first set is found; `FamilyInstance` is an electrical panel
    # by its own Panel Name. Both were on this roll-call until that day.
    #
    # `IFCVersion` and `IList<Reference>` STAYED, and the two are not the same
    # kind of staying. An enum whose members differ per release is a decision
    # nobody has made; a FACE is a thing a keyboard cannot say. Only the first
    # is waiting on anything.
    for kind in ("IList<Element>", "List<Element>", "ICollection<Element>",
                 "FamilyInstance"):
        ok, _ = GJ.receivable(kind)
        check(ok, "%s can be typed in - it could not before 2026-09-19" % kind)

    # AND THE SIX THAT MOVED. This half fails if somebody takes them back out.
    for kind in ("ElementId", "IList<ElementId>", "ICollection<ElementId>",
                 "View3D", "Color", "Material"):
        ok, _ = GJ.receivable(kind)
        check(ok, "%s can be typed in - it could not before 2026-09-13" % kind)

    # FOUR MORE MOVED ON 2026-09-14, at the owner's request, and they are
    # asserted in the direction they moved for the same reason the six above
    # are: a list that only ever gets shorter is how a claim disappears without
    # anybody deciding to drop it.
    #
    # `IList<Reference>` STAYED, and the difference is worth stating. A
    # Reference is a FACE - picked with a mouse, on a particular solid, in a
    # particular view. There is no text that names one, so it is not a rule
    # waiting to be written; it is a thing a keyboard cannot say.
    # FRAGMENT-ISSUES row 91.
    for kind in ("IList<IList<XYZ>>", "OverrideGraphicSettings",
                 "ForgeTypeId", "ParameterValue"):
        ok, _ = GJ.receivable(kind)
        check(ok, "%s can be typed in - it could not before 2026-09-14" % kind)

    # And the reason is the ONE Revit gives, not a restatement of the type name.
    # A reader learning that a point waits on a UNITS decision and an id on a
    # 2024 TYPE CHANGE has learned two different problems with two different
    # fixes; "unsupported" twice teaches neither.
    # THIS USED TO ASK AN ID WHICH CHANGE IT WAS WAITING ON, and an id waits on
    # nothing now. A dictionary took its place because it is the clearest
    # surviving case of a reason that teaches something: the blank holds one
    # value and a pair needs two, which is a different problem from "not
    # supported" and has a different fix.
    _, mapping = GJ.receivable("IDictionary<ElementId, string>")
    check("pairs" in mapping,
          "a dictionary says it is the PAIR that cannot be typed: %r" % mapping)

    # AND WHAT IS LEFT OF THE NESTING REFUSAL. Pairs are typeable now; a THIRD
    # level of nesting is not, and the row is kept rather than deleted so that
    # an absent shape does not read as an overlooked one.
    _, nested = GJ.receivable("IList<IList<IList<XYZ>>>")
    check(nested and "nests them" in nested,
          "points nested deeper than pairs say it is the NESTING that is "
          "refused, not the point: %r" % nested)

    # THE PIPE IS THE HALF THAT IS EASY TO MISS, so the hint has to carry it.
    # "semicolons" alone is true WITHIN a pair and silently wrong between them.
    pairs = GJ.how_to_type("IList<IList<XYZ>>")
    check("PIPE" in pairs, "pairs of points name the pipe: %r" % pairs)
    check("MILLIMETRES" in pairs, "and still name the unit")


def test_a_point_is_millimetres_and_says_so():
    print("A point can be typed in, and the blank beside it names the unit")

    for kind in ("XYZ", "IList<XYZ>", "List<XYZ>", "ICollection<XYZ>",
                 "IEnumerable<XYZ>"):
        ok, why = GJ.receivable(kind)
        check(ok, "%s can be typed in (%s)" % (kind, why or "yes"))

    # THE UNIT IS THE WHOLE DECISION, so the hint has to carry it. A point typed
    # in metres is a thousand times wrong and looks exactly like a right one -
    # nothing downstream catches that, which is why D3 exists at all.
    single = GJ.how_to_type("XYZ")
    check("MILLIMETRES" in single, "the hint names the unit: %r" % single)
    check("x,y,z" in single, "and the shape of one point")

    many = GJ.how_to_type("IList<XYZ>")
    check("MILLIMETRES" in many, "a list names the unit too")
    check("semicolon" in many, "and says what separates two points: %r" % many)

    # A flat comma list must NOT be offered for points - "0,0,0,1000,0,0" is two
    # points only if you already know they come in threes, and a list with a
    # number missing becomes a different, valid-looking list.
    check("comma separated" not in many,
          "and does not offer the flat comma list every other list uses")

    # PAIRS OF POINTS BECAME TYPEABLE ON 2026-09-14 - `create-line` wanted them
    # and the third separator was decided rather than deferred again. Nested a
    # level deeper than THAT is still refused, and the refusal blames the
    # NESTING rather than the point: the unit question is settled and must not
    # read as open.
    ok, _ = GJ.receivable("IList<IList<XYZ>>")
    check(ok, "pairs of points can be typed in")

    ok, why = GJ.receivable("IList<IList<IList<XYZ>>>")
    check(not ok, "points nested a level deeper than pairs are still refused")
    check(why and "millimetre" in why,
          "and the refusal states the settled rule rather than reopening it")


def test_an_element_is_a_type_and_a_list_of_them_is_still_refused():
    print("`Element` resolves to a TYPE; the instance boundary stays")

    # WHY THIS ASSERTION MOVED, 2026-09-10 - defect row 13.
    #
    # It used to read `receivable("Element")` -> True, on the reasoning that
    # `Element` "resolves to an element TYPE by name and refuses a particular
    # wall or duct". THE FIRST HALF WAS TRUE AND THE SECOND WAS NOT. Nothing
    # refused: `OneElement` was `OneOfClass(doc, typeof(ElementType), ...)`, so
    # a typed name resolved to a TYPE successfully and the fragment ran on it.
    # All fourteen bare `Element` needs in the library mean an INSTANCE, and all
    # twelve fragments came back 0 with no error and a findings line naming the
    # type - the confident meaningless answer this repository exists to refuse.
    #
    # The boundary the old comment describes is now REAL, and the need's NAME is
    # what draws it, because the type cannot: `wallType` and `reference` are both
    # written `Element`. So the old assertion still holds for a name meaning a
    # type, and is inverted for a name meaning an instance.
    ok, _ = GJ.receivable("Element", "wallType")
    check(ok, "a single Element CAN be typed in when the name means a type")

    refused, why = GJ.receivable("Element", "reference")
    check(not refused, "and CANNOT when the name means one particular element")
    check(why and "PARTICULAR" in why,
          "and the refusal says which of the two it hit")

    # AND AN UNNAMED ONE IS REFUSED, not accepted. A caller with no name to offer
    # cannot be shown to mean a type, and guessing "type" is how this defect read
    # as twelve fragment failures for a day.
    blind, _ = GJ.receivable("Element")
    check(not blind, "an Element with no need-name is refused, not assumed a type")

    # AND THE LIST FORM CAME LATER, BY A DIFFERENT RULE - 2026-09-19, and the
    # difference is the whole reason the singular is still refused.
    #
    # This block used to read "a list of them is not", on the reasoning that the
    # three fragments declaring `IList<Element>` want INSTANCES and a list of
    # type names is not what they are asking for. THE FIRST HALF WAS RIGHT AND
    # IS WHY THE RULE IS NOT THE SINGULAR ONE: a list of elements is not
    # resolved by naming each one, it is resolved by naming a CATEGORY and
    # taking what is in it. The ambiguity `OneElement` refuses - which of three
    # walls did you mean - cannot arise when the answer is allowed to be plural,
    # the same argument the id list already made in 2026-09-14.
    ok, _ = GJ.receivable("IList<Element>")
    check(ok, "a list of them IS typeable, by category, since 2026-09-19")

    # AND THE SINGULAR DID NOT MOVE WITH IT. One particular element still has
    # no name of its own, and a category naming one element is a coincidence
    # rather than a rule.
    refused, why = GJ.receivable("Element", "host")
    check(not refused, "one particular element is still refused")

    # The hint a person reads beside the blank names what to write. It no longer
    # has to warn that an instance cannot be given: since row 13 an instance-
    # meaning need is refused by `receivable` and never reaches a job file at
    # all, so the warning would be addressed to a blank that cannot exist.
    hint = GJ.how_to_type("Element")
    check("TYPE" in hint, "the hint says a TYPE is wanted")
    check("Basic Wall" in hint, "and shows the shape of the name to write")


def test_the_narrowed_declarations_resolve():
    print("A contract that says which type it wants can be handed one")

    # Eight contracts were narrowed off `Element` on 2026-09-09, and the point of
    # narrowing is that the search is confined: "Generic - 200mm" is unique among
    # WALL types where it may not be among every element type in the model.
    for kind in ("WallType", "FloorType", "CeilingType", "FilledRegionType",
                 "Phase", "FilterElement", "HostObjAttributes", "MEPCurveType"):
        ok, why = GJ.receivable(kind)
        check(ok, "%s can be typed in (%s)" % (kind, why or "yes"))

    # FamilySymbol was not narrowed off anything - it came with the same
    # mechanism, and it is what set-sheet-title-block and distribute-along-run
    # were waiting on. FRAGMENT-ISSUES section 6 counted four fragments on it.
    ok, _ = GJ.receivable("FamilySymbol")
    check(ok, "and so can a family type, which nothing had to be narrowed for")

    # THE LIBRARY IS THE REAL CHECK. Whatever was narrowed, every one of those
    # needs has to be something Revit can now receive - a contract narrowed to a
    # type with no rule would be a REGRESSION dressed as precision.
    found, _ = HF.load_all()
    stranded = []
    for frag in found.values():
        for need in frag.needs():
            declared = (need.get("type") or "")
            if HF.need_source(need) != "request":
                continue
            if declared in ("WallType", "FloorType", "CeilingType",
                            "FilledRegionType", "Phase", "FilterElement",
                            "HostObjAttributes", "MEPCurveType"):
                ok, why = GJ.receivable(declared)
                if not ok:
                    stranded.append("%s %s (%s)" % (frag.slug, need.get("name"), declared))
    check(not stranded, "no fragment was narrowed onto a type Revit cannot take (%s)"
          % ("; ".join(stranded[:3]) or "none"))


# ---------------------------------------------------------------------------
# The five shapes that gained a rule on 2026-09-19, and the one that did not
# ---------------------------------------------------------------------------
#
# PR #190 measured 44 DRAFT fragments that could not be arranged and PROPOSED a
# rule for six shapes, changing no code. These are those proposals, built - and
# each test names the fragments its rule frees, counted off the real library
# rather than carried in a sentence. A count in prose goes stale silently; a
# count derived here fails the day it stops being true.
#
# EVERY ONE OF THEM ALSO HAS TO SURVIVE `test_receivable_agrees_with_the_add_in`
# above, which reads the branches out of `RevitFragment.cs`. That is what makes
# these assertions about REVIT rather than about a Python set.


_LIBRARY = []


def library():
    """The real fragment library, keyed BY SLUG - `main()`'s own first act.

    `HF.load_all()` keys by ID, and `chain_provides` looks the setup chain up
    by slug. Re-keying here rather than reaching for `load_all` directly is
    what makes these tests ask the generator's question rather than one that
    merely resembles it.

    READ ONCE. 395 fragments off the disk is a second and a half, the tests
    below ask for the library eleven times, and a suite that takes a minute is
    a suite people stop running between edits.
    """
    if not _LIBRARY:
        found, _ = HF.load_all()
        _LIBRARY.append(dict((frag.slug, frag) for frag in found.values()))
    return _LIBRARY[0]


def unblocked_by(shape):
    """Fragments whose ONLY blocker is a caller value of this shape.

    Read off the real library, the real chain and the real registry - the same
    three inputs `generate-jobs.py` uses - so this counts what would actually be
    emitted rather than what a comment claims.

    OVER THE WHOLE LIBRARY, NOT OVER `GJ.candidates()`, AND THAT DISTINCTION IS
    THE TEST'S WHOLE MEANING. `candidates()` is a WORK QUEUE: DRAFT and never
    yet in front of a model. A fragment leaves it the moment somebody proves
    the thing - which is success, not change. Asked through that filter, this
    check answered "nothing frees them" on 2026-09-19 within hours of the rules
    landing, because `check-room-mep-completeness` and
    `rotate-elements-about-axis` had been PROVED using the very rules it was
    asserting about. The rule frees a SHAPE; which fragments happen to be
    unproven today is a fact about the backlog and has nothing to do with
    whether the resolver works. Same failure as a hard-coded count in a README,
    and it fails in the direction that looks like the feature broke.
    """
    found = library()
    supply = GJ.chain_provides(found)
    _, ordinal, ladder = GJ.write_threshold()

    wanted = shape.replace(" ", "")
    freed = []
    for slug in sorted(found):
        frag = found[slug]
        asks = [n for n in frag.needs()
                if HF.need_source(n) == "request"
                and (n.get("type") or "").replace(" ", "") == wanted]
        if not asks:
            continue
        if GJ.blockers(frag, supply, ordinal, ladder):
            continue
        freed.append(slug)
    return sorted(freed)


def test_a_roof_type_is_named_like_the_other_three():
    print("A roof type is named the way a wall type is")

    ok, why = GJ.receivable("RoofType")
    check(ok, "RoofType can be typed in (%s)" % (why or "yes"))

    # THE LOOKUP WAS NEVER THE PROBLEM, and that is why this one is a single
    # row rather than a parser. `RoofType` derives from `HostObjAttributes`,
    # which has been accepted since 2026-09-09, so the same object arrived
    # happily under the base name and was refused under its own - exactly the
    # shape `DuctType` and `PipeType` were in until 2026-09-13.
    ok, _ = GJ.receivable("HostObjAttributes")
    check(ok, "and its base class was already accepted, which is why")

    freed = unblocked_by("RoofType")
    check(freed == ["create-roof"],
          "it frees create-roof and nothing else: %s" % (freed or "nothing"))


def test_the_second_set_is_named_by_category():
    print("A caller-supplied set of elements is named by category")

    for kind in ("IList<Element>", "List<Element>", "ICollection<Element>",
                 "IEnumerable<Element>"):
        ok, why = GJ.receivable(kind)
        check(ok, "%s can be typed in (%s)" % (kind, why or "yes"))

    # THE CLAIM THE RULE RESTS ON, CHECKED RATHER THAN ASSERTED. The rule is
    # "always the SECOND set", and it is only safe because the FIRST set of
    # every fragment asking for one arrives down the chain. If a fragment ever
    # declares a request-sourced list of elements with no host-sourced one
    # beside it, "second set" stops being true and the category rule has to be
    # thought about again rather than inherited.
    found = library()
    firstless = []
    for frag in found.values():
        asks = [n for n in frag.needs()
                if HF.need_source(n) == "request"
                and (n.get("type") or "").replace(" ", "") == "IList<Element>"]
        if not asks:
            continue
        chained = [n for n in frag.needs()
                   if HF.need_source(n) != "request"
                   and (n.get("type") or "").replace(" ", "") == "IList<Element>"]
        if not chained:
            firstless.append(frag.slug)
    check(not firstless,
          "every fragment asking for one has a first set too (%s)"
          % ("; ".join(sorted(firstless)) or "all three do"))

    # AND THE HINT CARRIES BOTH HALVES. A category name here means every
    # element of that category in the MODEL - there is no view where this is
    # resolved - and `selected` is refused, which is the word somebody reaches
    # for first. A blank with neither warning beside it is how the second set
    # quietly becomes the first one again.
    hint = GJ.how_to_type("IList<Element>")
    check("CATEGORY" in hint, "the hint says a category names it: %r" % hint)
    check("MODEL" in hint, "and that it means the whole model, not a view")
    check("selected" in hint, "and that the selection is refused here")

    freed = unblocked_by("IList<Element>")
    check(freed == ["check-room-mep-completeness", "connect-air-terminals",
                    "propose-mep-openings"],
          "it frees the three second-set fragments: %s" % (freed or "nothing"))


def test_a_line_is_two_points_and_curves_are_pairs_of_them():
    print("A line and a list of curves read the point-pair spelling")

    for kind in ("Line", "IList<Curve>", "List<Curve>", "ICollection<Curve>",
                 "IEnumerable<Curve>"):
        ok, why = GJ.receivable(kind)
        check(ok, "%s can be typed in (%s)" % (kind, why or "yes"))

    # NO NEW SPELLING, AND THE HINTS HAVE TO PROVE IT. Both are the pair
    # parser settled on 2026-09-14: the unit is millimetres and the separator
    # between two points is a semicolon, with a PIPE between curves. A hint
    # offering "comma separated" would be describing the ordinates of one
    # point and would read as the list separator.
    line = GJ.how_to_type("Line")
    check("MILLIMETRES" in line, "a line names the unit: %r" % line)
    check("SEMICOLON" in line, "and what separates its two ends")
    check("comma separated" not in line, "and does not offer the flat list")

    curves = GJ.how_to_type("IList<Curve>")
    check("MILLIMETRES" in curves, "a curve list names the unit: %r" % curves)
    check("PIPE" in curves, "and the separator between two curves")
    check("arc" in curves, "and says an arc cannot be written this way")

    freed = unblocked_by("Line")
    check(freed == ["rotate-elements-about-axis"],
          "a Line frees the rotation axis and nothing else: %s"
          % (freed or "nothing"))

    # AND THE DIMENSION DOES NOT COME BACK WITH IT. `create-linear-dimension`
    # declares a Line AND an `IList<Reference>`, and a face is not a rule
    # waiting to be written. This is the assertion that stops the Line rule
    # being sold as having freed the dimensions - PR #190 said so in words and
    # a word is not a check.
    found = library()
    supply = GJ.chain_provides(found)
    _, ordinal, ladder = GJ.write_threshold()
    still = GJ.blockers(found["create-linear-dimension"], supply, ordinal, ladder)
    check(any("face" in r.lower() for r in still),
          "a linear dimension is still blocked, on the face: %s" % (still or "nothing"))

    freed = unblocked_by("IList<Curve>")
    check(freed == ["place-line-based-family"],
          "a curve list frees the line-based family and nothing else: %s"
          % (freed or "nothing"))


def test_a_panel_is_named_by_its_own_panel_name():
    print("An electrical panel is named by its Panel Name")

    ok, why = GJ.receivable("FamilyInstance")
    check(ok, "FamilyInstance can be typed in (%s)" % (why or "yes"))

    # AND THE SINGULAR `Element` DID NOT MOVE WITH IT, which is the boundary
    # this rule has to stay on the right side of. A panel is nameable because
    # its PANEL NAME is its own - like a room's Name parameter, and unlike
    # `Element.Name` on an instance, which gives the TYPE's name and would
    # match every panel of that type in the building.
    refused, _ = GJ.receivable("Element", "host")
    check(not refused, "one particular element is still refused")

    ok, _ = GJ.receivable("SpatialElement")
    check(ok, "and a room is nameable for exactly the same reason")

    # THE HINT HAS TO SAY WHICH NAME. The family type is right there in the
    # Properties palette, it is the obvious thing to type, and it resolves to
    # nothing - so the blank has to say PANEL NAME before anybody fills it.
    hint = GJ.how_to_type("FamilyInstance")
    check("PANEL NAME" in hint, "the hint names the parameter: %r" % hint)
    check("blank" in hint, "and says blank leaves the circuit unassigned")

    freed = unblocked_by("FamilyInstance")
    check(freed == ["create-electrical-circuit"],
          "it frees the circuit and nothing else: %s" % (freed or "nothing"))


def test_an_arc_was_left_unwritten_and_the_reason_still_holds():
    print("An Arc rule was proposed, refused, and the reason is checked here")

    # THE SIXTH PROPOSAL WAS TO WRITE NOTHING, and this test is what keeps that
    # decision honest rather than merely recorded. An Arc is three points and a
    # day's work; the argument against it is that its ONLY customer in the
    # library also needs a face, so the resolver would ship reachable by
    # nobody. That argument is true of the library as it stands and stops being
    # true the moment a second fragment declares an Arc - at which point this
    # fails and the decision gets made again with the new fact in hand.
    ok, why = GJ.receivable("Arc")
    check(not ok and why, "an Arc is still refused, with a reason")

    found = library()
    wants = sorted(frag.slug for frag in found.values()
                   for need in frag.needs()
                   if HF.need_source(need) == "request"
                   and (need.get("type") or "").replace(" ", "") == "Arc")
    check(wants == ["create-angular-dimension"],
          "one fragment in the library asks for one: %s" % (wants or "none"))

    faces = [need.get("name") for need in found["create-angular-dimension"].needs()
             if "Reference" in (need.get("type") or "")]
    check(faces,
          "and it also needs a face, which no rule can ever supply: %s" % faces)


def test_every_shape_with_a_new_syntax_has_a_hint():
    print("A shape a person cannot guess at carries a hint beside the blank")

    # THE BLANK IS WHERE THE MISTAKE HAPPENS. A shape whose syntax is not
    # obvious from its type name and whose hint is empty is a value somebody
    # will type wrongly - that is the whole reason `how_to_type` exists, and
    # nothing checked that a newly receivable shape had been given one.
    for kind in ("RoofType", "IList<Element>", "Line", "IList<Curve>",
                 "FamilyInstance"):
        hint = GJ.how_to_type(kind)
        check(hint and hint.strip(),
              "%s has something written beside its blank: %r" % (kind, hint))


def test_the_second_set_refuses_the_selection_word_in_the_add_in():
    print("The add-in refuses `selected` for a second set, and says why")

    # THE ONE CLAIM THE PYTHON TRANSCRIPTION CANNOT SEE. `RECEIVABLE` records
    # that a shape is accepted; it cannot record that this one is accepted
    # NARROWLY. A list of ids takes the word `selected` and a list of elements
    # deliberately does not - because the first set of all three fragments
    # asking for one already arrives that way, so the word would hand the same
    # elements to both roles and the answer would mean nothing.
    #
    # Read out of the C# for the same reason `accepted_by_revit` is: the
    # authority is the add-in, and a rule nobody checks is a rule that drifts.
    source = io.open(REVIT_FRAGMENT, encoding="utf-8").read()
    opens = source.find("private static object ManyByCategory")
    closes = source.find("private static object OnePanelNamed")
    # FOUND RATHER THAN INDEXED, so an add-in without the method reports a
    # failed check instead of a traceback that stops the rest of the suite.
    check(opens >= 0 and closes > opens,
          "the add-in has a ManyByCategory to read")
    body = source[opens:closes] if (opens >= 0 and closes > opens) else ""

    check("IsSelectionWord(text)" in body,
          "the second set asks whether the selection word was typed")
    check("SECOND set" in body,
          "and the refusal says it is the second set")
    check("Name a category" in body,
          "and says what to type instead")


def test_spaces_in_a_type_do_not_change_the_answer():
    print("A type is compared the way FromRequest compares it")

    ok, _ = GJ.receivable("IList< string >")
    check(ok, "IList< string > is the same type as IList<string>")


# ---------------------------------------------------------------------------
# The write path - Golden Rule 19
# ---------------------------------------------------------------------------

def test_the_write_threshold_comes_from_the_registry():
    print("`write: true` is read from the tool registry, never assumed")

    name, ordinal, ladder = GJ.write_threshold()
    ops = GJ.declared_operations()

    check(name == ops[GJ.WRITE_OP],
          "the threshold IS the risk run_fragment_write declares (%s)" % name)
    check(ordinal == ladder[name], "and its place on the ladder is the enum's")
    check(ladder[ops[GJ.READ_OP]] < ordinal,
          "the read executor sits below it, so a read is never sent as a write")

    # The ladder is HeronRisk's, not a second opinion about it.
    for level in ("READ", "ANALYZE", "SUGGEST", "EXECUTE", "MODIFY", "PUBLISH",
                  "ADMIN"):
        check(level in ladder, "HeronRisk declares %s" % level)
    check(ladder["MODIFY"] > ladder["EXECUTE"] > ladder["READ"],
          "and the order is the one HeronPermissions declares")


def test_a_broken_registry_stops_the_run_rather_than_guessing():
    print("A registry that cannot answer stops it, and says so")

    real = GJ.declared_operations
    try:
        GJ.declared_operations = lambda path=None: {GJ.READ_OP: "ANALYZE"}
        try:
            GJ.write_threshold()
            check(False, "a missing run_fragment_write row is refused")
        except SystemExit as exc:
            check("Golden Rule 19" in str(exc),
                  "and the refusal says why it may not guess instead")

        # The dangerous rearrangement: the write executor no longer above the
        # read one. Every `write:` line would be wrong, and silently.
        GJ.declared_operations = lambda path=None: {GJ.READ_OP: "MODIFY",
                                                    GJ.WRITE_OP: "MODIFY"}
        try:
            GJ.write_threshold()
            check(False, "a write path no longer above the read path is refused")
        except SystemExit as exc:
            check("would be a guess" in str(exc),
                  "and says nothing was generated rather than generating it")
    finally:
        GJ.declared_operations = real


def test_risk_decides_the_write_path_for_every_fragment_in_the_library():
    print("Every fragment's write path follows its own declared risk")

    name, ordinal, ladder = GJ.write_threshold()
    found, _ = HF.load_all()

    # MODIFY is at the threshold and EXECUTE is below it - `set-selection`'s own
    # comment says so in as many words: changing what is highlighted is not a
    # change to the model.
    check(ladder["MODIFY"] >= ordinal, "a MODIFY fragment needs the write path")
    check(ladder["EXECUTE"] < ordinal, "an EXECUTE fragment does not")
    check(ladder["READ"] < ordinal, "and neither does a READ one")

    unreadable = [f.slug for f in found.values()
                  if f.data.get("risk") not in ladder]
    check(not unreadable,
          "every fragment declares a risk HeronRisk knows (%s)"
          % (", ".join(sorted(unreadable)[:5]) or "all of them do"))


# ---------------------------------------------------------------------------
# Which fragments are attempted at all
# ---------------------------------------------------------------------------

def test_finished_work_is_never_emitted():
    print("A fragment that is not DRAFT is not a candidate")

    found, _ = HF.load_all()
    library = dict((f.slug, f) for f in found.values())
    picked = set(GJ.candidates(library))

    wrong = sorted(s for s in picked if library[s].status != "DRAFT")
    check(not wrong, "nothing at PROVEN or PRODUCTION is picked up (%s)"
          % (", ".join(wrong[:5]) or "none"))

    # `batch-prove` would refuse them anyway and report ALREADY. Leaning on that
    # refusal to find out is the mistake that cost a whole pass on 2026-09-09,
    # and a generator that emits them has moved the mistake rather than fixed it.
    for slug in sorted(picked)[:40]:
        verdict, _ = BP.job_refusal(
            {"fragment": slug, "setup": GJ.SETUP_CHAIN, "set": {},
             "negative-set": {"categoryName": "x"}, "negative-in": None,
             "expect": None, "cross": None}, library)
        if verdict == BP.ALREADY:
            check(False, "%s would be reported ALREADY" % slug)
            break
    else:
        check(True, "and the runner reports ALREADY for none of them")


def test_a_fragment_already_run_is_left_alone():
    print("A fragment with a run record is not offered again")

    found, _ = HF.load_all()
    library = dict((f.slug, f) for f in found.values())
    picked = set(GJ.candidates(library))

    recorded = set()
    if os.path.isdir(GJ.RUNS):
        recorded = set(os.path.splitext(f)[0] for f in os.listdir(GJ.RUNS)
                       if f.endswith(".json"))
    overlap = sorted(picked & recorded)
    check(not overlap, "nothing in brain/proof-drafts/runs/ is re-offered (%s)"
          % (", ".join(overlap[:5]) or "none"))


def test_a_fragment_with_nothing_to_vary_is_marked():
    print("A fragment with no second leg is marked, not emitted")

    supply = {"elements": "IList<Element>"}
    _, ordinal, ladder = GJ.write_threshold()

    lonely = FakeFragment("report-open-documents",
                          needs=[{"name": "doc", "type": "Document"},
                                 {"name": "app", "type": "Application"}])
    reasons = GJ.blockers(lonely, supply, ordinal, ladder)
    check(any("same run twice" in r for r in reasons),
          "both legs would be the same run, so it says so")
    check(any("TRACKING" in r for r in reasons),
          "and it points at tracking rather than leaving a dead end")


def test_two_needs_filled_from_one_value_are_marked():
    print("Two needs bound to one chain value are marked")

    supply = {"elements": "IList<Element>"}
    _, ordinal, ladder = GJ.write_threshold()

    # find-nearest-elements: the things to measure FROM and the things to
    # measure TO, and the chain leaves one selection.
    pair = FakeFragment("find-nearest-elements", needs=[
        {"name": "elements", "type": "IList<Element>"},
        {"name": "targets", "type": "IList<Element>", "binds": "elements"},
    ])
    reasons = GJ.blockers(pair, supply, ordinal, ladder)
    check(any("same set" in r for r in reasons),
          "it says both would arrive as the same set")


def test_a_need_the_chain_cannot_fill_is_marked():
    print("A fragment-sourced need the setup chain does not leave is marked")

    supply = {"elements": "IList<Element>"}
    _, ordinal, ladder = GJ.write_threshold()

    fed = FakeFragment("sum-by-group", needs=[
        {"name": "quantities", "type": "IDictionary<ElementId, double>"},
    ])
    reasons = GJ.blockers(fed, supply, ordinal, ladder)
    check(any("setup chain does not leave one" in r for r in reasons),
          "it names what the chain does not provide")


def test_a_risk_out_of_reach_is_marked():
    print("A fragment Heron will not run is marked before it costs a slot")

    supply = {"elements": "IList<Element>"}
    _, ordinal, ladder = GJ.write_threshold()

    for risk in ("PUBLISH", "ADMIN"):
        out = FakeFragment("export-model-to-nwc", risk=risk,
                           needs=[{"name": "elements", "type": "IList<Element>"}])
        reasons = GJ.blockers(out, supply, ordinal, ladder)
        check(any("out of reach" in r for r in reasons),
              "risk: %s is marked, not emitted" % risk)

    check("PUBLISH" not in GJ.CLIENT.RUNNABLE_RISKS
          and "ADMIN" not in GJ.CLIENT.RUNNABLE_RISKS,
          "and the list it reads is the client's own, not a copy")


# ---------------------------------------------------------------------------
# What comes out - the shape, and the blanks
# ---------------------------------------------------------------------------

def generated():
    """The whole file, generated from the real library."""
    found, _ = HF.load_all()
    library = dict((f.slug, f) for f in found.values())
    name, ordinal, ladder = GJ.write_threshold()
    return GJ.build(library, ordinal, name, ladder)


def test_the_output_is_the_shape_batch_prove_already_parses():
    print("What comes out is example.yaml's shape, read by the real reader")

    text, counts, jobs, blocked = generated()

    parsed = yaml.safe_load(text)
    check(isinstance(parsed, dict), "it is a mapping")
    check("model" in parsed and "defaults" in parsed and "jobs" in parsed,
          "with model, defaults and jobs - the keys example.yaml uses")

    example = yaml.safe_load(io.open(
        os.path.join(ROOT, "tools", "jobs", "example.yaml"), encoding="utf-8"))
    check(set(parsed) <= set(example),
          "and no key example.yaml has not got (%s)"
          % ", ".join(sorted(set(parsed) - set(example))))
    check(set(parsed["defaults"]) <= set(example["defaults"]),
          "the same inside defaults: (%s)"
          % ", ".join(sorted(set(parsed["defaults"]) - set(example["defaults"]))))

    if jobs:
        keys = set()
        for row in parsed["jobs"]:
            keys |= set(row)
        allowed = set(["fragment", "setup", "set", "negative-set", "write",
                       "cross", "in", "negative-in", "expect", "timeout", "note"])
        check(keys <= allowed, "and every job key is one read_jobs reads (%s)"
              % ", ".join(sorted(keys - allowed)))


def test_the_generated_file_is_runnable_once_the_blanks_are_filled():
    print("It dry-runs clean against the real library")

    text, counts, jobs, blocked = generated()
    if not jobs:                                             # pragma: no cover
        check(True, "nothing to emit today, so nothing to check")
        return

    found, _ = HF.load_all()
    library = dict((f.slug, f) for f in found.values())

    # `read_jobs` reads a PATH, so the filled-in file goes to disk the way a
    # person's would. Filling in every blank with one word is not an ARRANGEMENT
    # - a real one needs the model open, and rule 2 is what decides it - but it
    # is exactly what the dry run checks: names, statuses, the setup chain, and
    # whether a negative case is arranged at all. None of those depend on which
    # category was typed.
    handle, path = tempfile.mkstemp(suffix=".yaml")
    os.close(handle)
    try:
        with io.open(path, "w", encoding="utf-8", newline="\n") as fh:
            fh.write(text.replace(GJ.FILL_IN, "Ducts"))
        rows, problems = BP.read_jobs(path)
        check(not problems, "the job file reads without complaint (%s)"
              % "; ".join(problems))
        check(len(rows) == len(jobs),
              "and every job survives the read (%d of %d)" % (len(rows), len(jobs)))

        refused = []
        for job in rows:
            verdict, why = BP.job_refusal(job, library)
            if verdict is not None:
                refused.append("%s: %s - %s" % (job["fragment"], verdict, why))
        check(not refused, "and none is refused by the dry run (%s)"
              % ("; ".join(refused[:3]) or "none"))
    finally:
        os.unlink(path)


def test_the_category_and_the_view_are_left_blank():
    print("The two judgement values are blank, and marked")

    text, counts, jobs, blocked = generated()
    parsed = yaml.safe_load(text)

    for where in ("set", "negative-set"):
        for name in GJ.SHARED_INPUTS:
            check(parsed["defaults"][where].get(name) == GJ.FILL_IN,
                  "defaults %s %s is blank" % (where, name))

    # AND NOTHING WAS QUIETLY FILLED IN ANYWHERE. A single guessed value is what
    # produced eleven confident meaningless results in one batch.
    guessed = []
    for row in parsed["jobs"]:
        for where in ("set", "negative-set"):
            for name, value in (row.get(where) or {}).items():
                if value != GJ.FILL_IN:
                    guessed.append("%s %s %s=%r" % (row["fragment"], where,
                                                    name, value))
    check(not guessed, "no value in any job was guessed (%s)"
          % ("; ".join(guessed[:3]) or "none"))

    check(parsed["model"].startswith(GJ.FILL_IN), "and the model line is blank")


def test_the_input_names_are_the_contract_s_own():
    print("Every input name is spelled from contract.needs")

    text, counts, jobs, blocked = generated()
    parsed = yaml.safe_load(text)
    found, _ = HF.load_all()
    library = dict((f.slug, f) for f in found.values())

    wrong, covered = [], []
    for row in parsed["jobs"]:
        frag = library[row["fragment"]]
        declared = set(n.get("name") for n in frag.needs()
                       if HF.need_source(n) == "request")
        typed = set(row.get("set") or {}) - set(GJ.SHARED_INPUTS)
        for name in sorted(typed - declared):
            wrong.append("%s does not need %r" % (frag.slug, name))
        for name in sorted(declared - typed - set(GJ.SHARED_INPUTS)):
            wrong.append("%s needs %r and it was not emitted" % (frag.slug, name))
        covered += sorted(typed)

    check(not wrong, "every name matches the contract exactly (%s)"
          % ("; ".join(wrong[:4]) or "none"))
    check(covered or not parsed["jobs"],
          "and %d caller value(s) were spelled out" % len(covered))


def test_no_expect_line_is_invented():
    print("`expect:` is offered as a comment, never chosen")

    text, counts, jobs, blocked = generated()
    parsed = yaml.safe_load(text)

    invented = [row["fragment"] for row in parsed["jobs"] if row.get("expect")]
    check(not invented, "no job carries an expect: this tool chose (%s)"
          % (", ".join(invented[:3]) or "none"))
    if jobs:
        check("expect:" in text,
              "but the names to choose from are written down for a person")


def test_nothing_touches_status_or_proof():
    print("It writes no heron-status and no proof")

    text, _, _, _ = generated()
    for forbidden in ("heron-status:", "heron_status", "proof:"):
        check(forbidden not in text,
              "the generated file contains no %r" % forbidden)


def test_the_blocked_are_listed_with_a_reason_each():
    print("Every fragment not emitted says why")

    text, counts, jobs, blocked = generated()
    check(counts["seen"] == counts["jobs"] + counts["blocked"],
          "every candidate is either a job or a marked one, never dropped")
    for slug, reasons in blocked:
        if not reasons:                                      # pragma: no cover
            check(False, "%s was blocked with no reason" % slug)
            break
        if slug not in text:                                 # pragma: no cover
            check(False, "%s was blocked but not written down" % slug)
            break
    else:
        check(True, "%d marked fragment(s), each with a reason in the file"
              % counts["blocked"])


def main():
    for test in (test_receivable_agrees_with_the_add_in,
                 test_an_id_is_resolved_by_its_name_not_by_its_type,
                 test_the_shapes_d54_refuses_are_refused_here,
                 test_an_element_is_a_type_and_a_list_of_them_is_still_refused,
                 test_a_point_is_millimetres_and_says_so,
                 test_the_narrowed_declarations_resolve,
                 test_a_roof_type_is_named_like_the_other_three,
                 test_the_second_set_is_named_by_category,
                 test_a_line_is_two_points_and_curves_are_pairs_of_them,
                 test_a_panel_is_named_by_its_own_panel_name,
                 test_an_arc_was_left_unwritten_and_the_reason_still_holds,
                 test_every_shape_with_a_new_syntax_has_a_hint,
                 test_the_second_set_refuses_the_selection_word_in_the_add_in,
                 test_spaces_in_a_type_do_not_change_the_answer,
                 test_the_write_threshold_comes_from_the_registry,
                 test_a_broken_registry_stops_the_run_rather_than_guessing,
                 test_risk_decides_the_write_path_for_every_fragment_in_the_library,
                 test_finished_work_is_never_emitted,
                 test_a_fragment_already_run_is_left_alone,
                 test_a_fragment_with_nothing_to_vary_is_marked,
                 test_two_needs_filled_from_one_value_are_marked,
                 test_a_need_the_chain_cannot_fill_is_marked,
                 test_a_risk_out_of_reach_is_marked,
                 test_the_output_is_the_shape_batch_prove_already_parses,
                 test_the_generated_file_is_runnable_once_the_blanks_are_filled,
                 test_the_category_and_the_view_are_left_blank,
                 test_the_input_names_are_the_contract_s_own,
                 test_no_expect_line_is_invented,
                 test_nothing_touches_status_or_proof,
                 test_the_blocked_are_listed_with_a_reason_each):
        test()
        print("")

    if FAILURES:
        print("%d failure(s):" % len(FAILURES))
        for failure in FAILURES:
            print("  - %s" % failure)
        return 1
    print("The generator spells the names, reads the risk, and guesses nothing.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
