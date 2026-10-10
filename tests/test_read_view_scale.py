#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
READ_VIEW_SCALE answers what scale a view is drawn at, and never gives a sheet
a number.

    python tests/test_read_view_scale.py

WHY IT EXISTS - FRAGMENT-ISSUES ROW 146
---------------------------------------
"what is the scale of this view" reached SET_VIEW_SCALE, a write, on the
trained model and on spelling, because nothing in the library read a view's
scale. READ_VIEW_SCALE (FRG-VIEW-120, brain/fragments/read-view-scale) was
written for it on 2026-10-09.

An adversarial review of that change then turned the fragment's three central
claims upside down, one at a time, and every check that runs off Revit stayed
green: a sheet let through the list of kinds drawn at a scale (so it would be
printed with a number), the view-template test reversed (so "held by its view
template" and "its own" swap), and the ratio typed as "1 : 100" for every view.
It also showed that the routing half was guarded only by
tests/test_routing_phrases.py section 8, which exits 3 - COULD NOT RUN - on
every machine without the trained model, CI included. This suite is the guard
that runs everywhere: no Revit, and no trained model - sections 1 to 6 need no
store at all, and section 7 builds a private one on whichever backend the
machine has.

A second review found the card's own wording taking change requests from
SET_VIEW_SCALE on ranking, and six more claims of the C# that could be turned
upside down with every check green: a perspective given a number, a view with
no scale counted but never named, a view with no template told "held by its
view template" the other way round, a row without the view's type, a scale of
0 printed as "1 : 0", and a view with no scale left out of the count. Sections
2b, 3, 6 and 7 are for those.

WHAT IT CHECKS, AS TEXT
  1. THE CARD. READ_VIEW_SCALE reads (its risk is below the write line, read
     from the operation registry - Golden Rule 19) and declares row 146's two
     sentences, and no other card declares them. That is what makes identity
     answer them before any ranking, on either backend; tools/check-routing.py,
     which CI runs, then asks each declared sentence back.
  2. A NUMBER ONLY FOR A KIND DRAWN AT A SCALE. In impl/any/fragment.cs the
     kinds `noScaleBecause` lets through are exactly the pinned list below; a
     sheet, a schedule, a walkthrough, a rendering or a report is never on it;
     a 3D view is let through only when IsPerspective is false - the test
     itself is pinned, not just the word; and the loop asks that question
     before it reads View.Scale, and skips on a reason.
  2b. EVERY VIEW WITH NO SCALE IS COUNTED AND NAMED. Each of the three ways a
     view ends with no number - its kind, View.Scale throwing, View.Scale at 0
     or less - adds to `noScale`, names the view in `findings` with the reason,
     and skips it. The 0-or-less guard sits between the read and the ratio. The
     label every row and finding starts with carries the view's type.
  3. THE TEMPLATE'S OWN INCLUDE LIST DECIDES "HELD". A scale parameter the
     template can control and does NOT leave free is what returns "holds", and
     "holds" is what the row reports as "held by its view template" - and only
     for a view that HAS a template: a ViewTemplateId equal to
     InvalidElementId is the view's own.
  4. THE RATIO IS THE VIEW'S OWN NUMBER. "1 : " is followed by what View.Scale
     returned, never by a number typed in the code.
  5. IT CHANGES NOTHING. No transaction, no assignment to Scale, no Set.
  6. ITS SEARCHED WORDS HOLD NO CHANGE VERB - set, change, update, adjust,
     modify, edit, alter, apply, make - in semantic-identity, the utterances
     or `purpose`.

Comments are removed before any of it is read, so a rule described in words
does not count. Each pin names the line it expects; an equivalent rewrite of
the C# turns this red and must be pinned again on purpose, which is the point.

WHAT IT ASKS THE SEARCH - SECTION 7
On a private store built from this checkout (never the shared one, row
5b-233), through heron_retrieve.find, the function heron_lookup calls: row
146's two sentences are answered here by identity; the ten change requests a
first draft of this card took from SET_VIEW_SCALE are never answered here; and
the words route alone - what check-routing's list of contested sentences reads
- never ranks this card first for a sentence SET_VIEW_SCALE declares. It runs
on whichever backend the machine has, which on CI is spelling, and each of
those holds on both backends. It does not ask that a change reaches
SET_VIEW_SCALE: that depends on every other card, and
tests/test_routing_phrases.py asks it on the trained model.

WHAT IT CANNOT DO
Run the fragment. That View.Scale on a real floor plan matches the Properties
palette, that a sheet and a perspective are told apart in Revit, which
parameter a template's include list carries for View Scale (and on 2027
whether it is one of the two that release adds), and what Revit's view title
prints are NEEDS REAL REVIT - the proof plan is
tools/jobs/read-view-scale-project1-2026-10-09.yaml. A green run here means
the rules are written, not that they have been seen to work.
"""

import importlib.util
import io
import os
import re
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

CARD_DIR = os.path.join(ROOT, "brain", "fragments", "read-view-scale")
IMPL = os.path.join(CARD_DIR, "impl", "any", "fragment.cs")
CAPABILITY = "READ_VIEW_SCALE"

# Row 146's sentences: the ones that reached SET_VIEW_SCALE before this READ.
ROW_146 = (
    "what is the scale of this view",
    "what scale is this plan at",
)

# The kinds the card's purpose names as drawn at a scale ("plans, ceiling
# plans, structural and area plans, elevations, sections, details, drafting
# views, legends"). 3D is checked on its own: only when it is not a perspective.
DRAWN = {
    "FloorPlan", "CeilingPlan", "EngineeringPlan", "AreaPlan", "Elevation",
    "Section", "Detail", "DraftingView", "Legend",
}

# Kinds with no scale of their own. A number for any of these is the confident
# wrong answer the card exists to refuse.
NEVER = (
    "DrawingSheet", "Schedule", "ColumnSchedule", "PanelSchedule", "Walkthrough",
    "Rendering", "Report", "CostReport", "LoadsReport", "ProjectBrowser",
    "SystemBrowser", "Internal", "Undefined",
)

# The write this card is the read half of.
WRITE = "SET_VIEW_SCALE"

# Change requests a first draft of this card took from SET_VIEW_SCALE, asked on
# heron_retrieve.find over private stores on 2026-10-09. The first five are the
# review's: on spelling the first four went to the read, on the trained model
# "update the view scale" and "set the scale of these views". The other five
# were asked in the repair before the wording was chosen and went to the read
# too ("modify" and "edit" on both backends, "update the scale of these views"
# on the model, "alter" and "adjust the scale of this view" on spelling). No
# card declares any of them on 2026-10-09, so each is ranked.
TAKEN_FROM_THE_WRITE = (
    "set the view scale",
    "set view scale",
    "update the view scale",
    "adjust the view scale",
    "set the scale of these views",
    "modify the view scale",
    "edit the view scale",
    "update the scale of these views",
    "alter the scale of this view",
    "adjust the scale of this view",
)

# The verbs of a change request, in the forms a sentence or a purpose uses.
# None may be in the words this card is searched by.
CHANGE_VERBS = frozenset((
    "set", "sets", "setting", "change", "changes", "changed", "changing",
    "update", "updates", "updated", "updating", "adjust", "adjusts", "adjusted",
    "adjusting", "modify", "modifies", "modified", "modifying", "edit", "edits",
    "edited", "editing", "alter", "alters", "altered", "altering", "apply",
    "applies", "applied", "applying", "make", "makes", "making",
))

failures = []
passes = [0]


def check(ok, what):
    print("  %-5s %s" % ("ok" if ok else "FAIL", what))
    if ok:
        passes[0] += 1
    else:
        failures.append(what)


def read(path):
    # Normalised at the read: a CRLF checkout must not move a match.
    if not os.path.isfile(path):
        return ""
    with io.open(path, encoding="utf-8") as handle:
        return handle.read().replace("\r\n", "\n")


def code_only(text):
    """The C# with its comments removed and its strings kept, so a rule
    written in a comment never satisfies a check."""
    out = []
    i, n = 0, len(text)
    while i < n:
        c = text[i]
        if c == '"':
            j = i + 1
            while j < n and text[j] != '"':
                j += 2 if text[j] == "\\" else 1
            out.append(text[i:j + 1])
            i = j + 1
        elif text.startswith("//", i):
            while i < n and text[i] != "\n":
                i += 1
        elif text.startswith("/*", i):
            end = text.find("*/", i + 2)
            i = n if end < 0 else end + 2
        else:
            out.append(c)
            i += 1
    return "".join(out)


def body(code, declaration):
    """The braces of the lambda or block opened right after DECLARATION, or ""."""
    at = code.find(declaration)
    if at < 0:
        return ""
    start = code.find("{", at)
    if start < 0:
        return ""
    depth, i = 0, start
    while i < len(code):
        c = code[i]
        if c == '"':
            i += 1
            while i < len(code) and code[i] != '"':
                i += 2 if code[i] == "\\" else 1
        elif c == "{":
            depth += 1
        elif c == "}":
            depth -= 1
            if depth == 0:
                return code[start:i + 1]
        i += 1
    return ""


def card_of(capability):
    """The one card on disk declaring CAPABILITY, or None when there is not exactly one."""
    import heron_fragment as FRAG
    on_disk, _problems = FRAG.load_all()
    found = [f for f in on_disk.values() if f.data.get("capability") == capability]
    return found[0] if len(found) == 1 else None


def write_line():
    """(ladder, threshold) from the operation registry - Golden Rule 19."""
    path = os.path.join(ROOT, "tools", "generate-jobs.py")
    spec = importlib.util.spec_from_file_location("heron_tool_generate_jobs", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    _name, threshold, ladder = module.write_threshold()
    return ladder, threshold


def the_card():
    print("1. THE CARD READS AND DECLARES ROW 146'S SENTENCES, AND NO OTHER CARD DOES")
    import heron_fragment as FRAG
    import heron_search as SEARCH

    on_disk, _problems = FRAG.load_all()
    mine = [f for f in on_disk.values() if f.data.get("capability") == CAPABILITY]
    check(len(mine) == 1, "one card is %s: %d found" % (CAPABILITY, len(mine)))

    ladder, threshold = write_line()
    risk = (mine[0].data.get("risk") or "").upper() if len(mine) == 1 else ""
    check(bool(risk) and ladder.get(risk, threshold) < threshold,
          "%s reads: risk %s, below the write line" % (CAPABILITY, risk or "none"))

    owners = {}
    for frag in on_disk.values():
        for said in [frag.data.get("semantic-identity") or ""] + frag.utterances():
            owners.setdefault(SEARCH.normalise(said), set()).add(frag.data.get("capability"))
    for text in ROW_146:
        claimed = sorted(owners.get(SEARCH.normalise(text), set()))
        check(claimed == [CAPABILITY], "%r is declared by %s alone - declared by %s"
              % (text, CAPABILITY, ", ".join(claimed) or "nobody"))


def the_kinds(code):
    print("\n2. A NUMBER IS GIVEN ONLY TO A KIND OF VIEW DRAWN AT A SCALE")
    rule = body(code, "noScaleBecause = view =>")
    check(bool(rule), "fragment.cs has the `noScaleBecause` rule")

    allowed = set()
    let_through = re.findall(r"if\s*\(((?:[^()]|\([^()]*\))*)\)\s*return\s+null\s*;", rule)
    for condition in let_through:
        allowed.update(re.findall(r"ViewType\.(\w+)", condition))
    check(allowed == DRAWN, "the kinds given a scale are exactly the drawn ones: %s"
          % (", ".join(sorted(allowed)) or "none found"))
    for kind in NEVER:
        check(kind not in allowed, "ViewType.%s is never let through to View.Scale" % kind)
    check(bool(rule) and rule.count("return null") == len(let_through),
          "every `return null` in the rule is one of those - no other way through")

    three = re.search(r"kind\s*==\s*ViewType\.ThreeD\s*\)(.*?)return\s+perspective\s*\?\s*"
                      r"\"[^\"]+\"[^:]*:\s*null\s*;", rule, re.S)
    check(three is not None and "IsPerspective" in three.group(1),
          "a 3D view is let through only when IsPerspective is false")
    # The line above only asks that the word is there. A review reversed it
    # (`!three.IsPerspective`) and this stayed green, so the test itself is
    # pinned: a perspective is what MAKES `perspective` true, and true is what
    # returns the reason.
    within = three.group(1) if three else ""
    check(re.search(r"\bthree\s*=\s*view\s+as\s+View3D\s*;", within) is not None
          and re.search(r"\bperspective\s*=\s*three\s*!=\s*null\s*&&\s*three\.IsPerspective\s*;",
                        within) is not None,
          "`perspective` is true exactly for a View3D whose IsPerspective is true - not reversed")
    check(re.search(r"return\s+perspective\s*\?\s*\"a perspective", rule) is not None,
          "and a perspective is given the reason, never let through to a number")

    asked = code.find("noScaleBecause(view)")
    skip = re.search(r"if\s*\(\s*why\s*!=\s*null\s*\)\s*\{[^{}]*continue\s*;\s*\}", code)
    read_at = code.find("= view.Scale;")
    check(0 <= asked and skip is not None and asked < skip.start() < read_at,
          "the loop asks the kind first and skips on a reason before it reads View.Scale")


def the_refusals(code):
    print("\n2b. EVERY VIEW WITH NO SCALE IS COUNTED AND NAMED, WITH ITS REASON")
    # Three ways a view ends with no number: its kind (`why`), View.Scale
    # throwing, and View.Scale giving 0 or less. Each must count it in
    # `noScale`, name it in `findings` by its label and say why, and skip it.
    # A review deleted the findings line from the first and the count from it,
    # and the check above - any block that ends in `continue` - stayed green.
    blocks = (
        ("its kind has no scale", r"if\s*\(\s*why\s*!=\s*null\s*\)\s*(\{[^{}]*\})",
         r"findings\.Add\(\s*label\s*\+\s*\": no scale - \"\s*\+\s*why\b"),
        ("Revit would not give a scale",
         r"scale\s*=\s*view\.Scale\s*;\s*\}\s*catch\s*\(\s*Exception\s+ex\s*\)\s*(\{[^{}]*\})",
         r"findings\.Add\(\s*label\s*\+\s*\": no scale - Revit would not give one: \"\s*\+\s*ex\.Message"),
        ("Revit gave 0 or less", r"if\s*\(\s*scale\s*<=\s*0\s*\)\s*(\{[^{}]*\})",
         r"findings\.Add\(\s*label\s*\+\s*\": no scale - Revit gave \"\s*\+\s*scale\.ToString\("),
    )
    for what, head, named in blocks:
        found = re.search(head, code)
        block = found.group(1) if found else ""
        check(bool(block), "the branch for %s is there" % what)
        check(re.search(r"\bnoScale\s*\+\+\s*;", block) is not None,
              "  %s: the view is counted in `noScale`" % what)
        check(re.search(named, block) is not None,
              "  %s: it is named in `findings` by its label, with the reason" % what)
        check(re.search(r"continue\s*;\s*\}$", block) is not None,
              "  %s: and it is skipped, so no row is written for it" % what)

    # A scale of 0 or less is never printed as a ratio: the guard is `<= 0`, it
    # sits between the read and the ratio, and nothing else builds a ratio.
    guard = re.search(r"if\s*\(\s*scale\s*<=\s*0\s*\)", code)
    read_at = code.find("= view.Scale;")
    ratio_at = code.find("var ratio")
    check(guard is not None and 0 <= read_at < guard.start() < ratio_at,
          "a scale of 0 or less is refused (`scale <= 0`) after View.Scale is read "
          "and before the ratio is built")

    # Each row - and each finding - names the view's type, which the label
    # carries; the rows and the no-scale findings are built from the label.
    label = re.search(r"\bvar\s+label\s*=\s*([^;]*);", code)
    check(label is not None and re.search(r"\bview\.ViewType\b", label.group(1)) is not None
          and re.search(r"\bname\b", label.group(1)) is not None,
          "the label every row and finding starts with carries the view's name and its type: %s"
          % (label.group(1).strip() if label else "not found"))
    check(re.search(r"rows\.Add\(\s*label\b", code) is not None,
          "each row starts with that label")


def the_template(code):
    print("\n3. THE TEMPLATE'S OWN INCLUDE LIST DECIDES WHETHER THE SCALE IS HELD")
    rule = body(code, "templateOnScale = template =>")
    check(bool(rule), "fragment.cs has the `templateOnScale` rule")

    left = re.search(r"(\w+)\s*=\s*template\.GetNonControlledTemplateParameterIds\(\)", rule)
    check(left is not None and re.search(
              r"\bfree\s*=\s*new\s+HashSet<ElementId>\(\s*%s\b" % re.escape(left.group(1)),
              rule) is not None,
          "`free` is what the template leaves out of its include list")
    may = re.search(r"(\w+)\s*=\s*template\.GetTemplateParameterIds\(\)", rule)
    check(may is not None and re.search(
              r"\bcanControl\s*=\s*new\s+HashSet<ElementId>\(\s*%s\b" % re.escape(may.group(1)),
              rule) is not None,
          "`canControl` is everything the template could control")

    holds = re.findall(r"if\s*\(([^;]*?)\)\s*return\s+\"holds\"\s*;", rule)
    check(rule.count('return "holds"') == 1 and len(holds) == 1
          and re.fullmatch(r"\s*!\s*free\.Contains\(\s*parameter\.Id\s*\)\s*", holds[0]),
          "\"holds\" is returned only for a scale parameter NOT left free: %s"
          % (holds[0].strip() if holds else "not found"))
    control = re.search(r"if\s*\(\s*!\s*canControl\.Contains\(\s*parameter\.Id\s*\)\s*\)"
                        r"\s*continue\s*;", rule)
    at_holds = rule.find('return "holds"')
    check(control is not None and control.start() < at_holds,
          "and only after a parameter the template cannot control is skipped")

    check(re.search(r"if\s*\(\s*held\s*==\s*\"holds\"\s*\)\s*\{\s*whose\s*=\s*"
                    r"\"held by its view template[^;]*;\s*heldByTemplate\s*\+\+\s*;", code)
          is not None,
          "\"holds\" is reported as held by its view template, and counted")
    check(re.search(r"else\s+if\s*\(\s*held\s*==\s*\"free\"\s*\)\s*\{\s*whose\s*=\s*"
                    r"\"its own", code) is not None,
          "\"free\" is reported as the view's own")
    check(re.search(r"whose\s*=\s*applies\s*==\s*\"holds\"\s*\?\s*"
                    r"\"a view template, and every view following it", code) is not None,
          "a view template named directly says so when it holds the scale")

    # "HELD BY ITS VIEW TEMPLATE" ONLY FOR A VIEW THAT HAS ONE. A review turned
    # `==` into `!=` below, so every view following a template was told "its
    # own - no view template", and every check here stayed green.
    none = re.search(r"else\s+if\s*\(\s*view\.ViewTemplateId\s*==\s*ElementId\.InvalidElementId\s*\)"
                     r"\s*\{\s*whose\s*=\s*\"its own - no view template\"\s*;\s*\}", code)
    check(none is not None,
          "a view whose ViewTemplateId IS InvalidElementId - no template - is its own")
    fetched = re.search(r"doc\.GetElement\(\s*view\.ViewTemplateId\s*\)", code)
    held = code.find("\"held by its view template")
    check(none is not None and fetched is not None
          and none.end() <= fetched.start() < held,
          "only past that test is the view's template read back, and only then can it be "
          "\"held by its view template\"")


def the_ratio(code):
    print("\n4. THE RATIO IS THE VIEW'S OWN NUMBER")
    reads = re.findall(r"\bscale\s*=\s*view\.Scale\s*;", code)
    check(len(reads) == 1, "View.Scale is read once into `scale`: %d read(s)" % len(reads))
    check(re.search(r"var\s+ratio\s*=\s*\"1 : \"\s*\+\s*scale\.ToString\(", code) is not None,
          "the ratio is \"1 : \" and that number")
    literals = re.findall(r'"(?:[^"\\]|\\.)*"', code)
    typed = [s for s in literals if re.search(r"\b1\s*:\s*\d", s)]
    check(bool(literals) and not typed,
          "no ratio is typed in the code: %s" % (", ".join(typed) or "none"))
    check(re.search(r"rows\.Add\(\s*label\s*\+\s*\" \"\s*\+\s*ratio\b", code) is not None,
          "each row carries that ratio")


def the_read_only(code):
    print("\n5. IT CHANGES NOTHING")
    for pattern, what in (
            (r"\b(?:Sub)?Transaction(?:Group)?\b", "transaction"),
            (r"\.Scale\s*=(?!=)", "assignment to Scale"),
            (r"\.Set\s*\(", "parameter Set"),
            (r"\bViewTemplateId\s*=(?!=)", "change of view template"),
            (r"\.Delete\s*\(", "Delete")):
        check(bool(code) and re.search(pattern, code) is None, "no %s" % what)


def the_words():
    print("\n6. THE CARD'S SEARCHED WORDS HOLD NO CHANGE VERB")
    # heron_search and heron_embed read semantic-identity, the utterances, the
    # capability, the domain and `purpose`; comments never. A READ whose
    # searched text says "set" or "change" is pulling change requests towards
    # itself, so the words below stay in comments, where they are not searched.
    frag = card_of(CAPABILITY)
    if frag is None:
        check(False, "one card is %s, so its words can be read" % CAPABILITY)
        return
    searched = " ".join([frag.data.get("semantic-identity") or ""] + frag.utterances()
                        + [frag.data.get("purpose") or ""])
    words = set(re.findall(r"[a-z]+", searched.lower()))
    said = sorted(words & CHANGE_VERBS)
    check(bool(words) and not said,
          "semantic-identity, utterances and purpose say none of: %s - found %s"
          % (", ".join(sorted(CHANGE_VERBS)), ", ".join(said) or "none"))


def the_routing():
    print("\n7. ASKED BACK ON A PRIVATE STORE BUILT FROM THIS CHECKOUT, ON THIS MACHINE'S BACKEND")
    import heron_embed as EMBED
    import heron_scope as SCOPE
    import heron_search as SEARCH
    import heron_capability as CAP
    import heron_retrieve as RETRIEVE

    backend, _why = EMBED.backend()
    print("  backend: %s" % backend)
    check(os.path.abspath(SCOPE.knowledge_dir()) == os.path.abspath(os.environ["HERON_KNOWLEDGE"]),
          "the store is this run's own folder, never the shared one")
    built, problems = SCOPE.rebuild()
    check(built > 100 and not problems,
          "the library was built: %d cards, %d problem(s)" % (built, len(problems)))
    store = SCOPE.open_scope(SCOPE.GLOBAL)
    try:
        SCOPE.refresh(store)
        CAP.rebuild(store)
        SEARCH.index(store)
        EMBED.index(store)
        rows = dict((r["id"], r) for r in store.fragments())
        by_capability = dict((r["capability"], r["id"]) for r in rows.values())
        mine = by_capability.get(CAPABILITY)
        check(mine is not None, "%s is in the store" % CAPABILITY)

        def answered(text):
            answer = RETRIEVE.find(store, text)
            return answer, (rows.get(answer.fragment_id) or {}).get("capability")

        print("  a. row 146's questions are answered here by identity")
        for text in ROW_146:
            answer, got = answered(text)
            check(answer.route == "identity" and got == CAPABILITY,
                  "  %r -> %s, %s" % (text, got, answer.route))

        print("  b. a change request a first draft of this card took is never answered here")
        for text in TAKEN_FROM_THE_WRITE:
            answer, got = answered(text)
            check(got != CAPABILITY, "  %r -> %s, %s" % (text, got, answer.route))

        # What tools/check-routing.py's SENTENCES TWO FRAGMENTS BOTH WANT reads:
        # the words route alone, over each sentence a card declares. The first
        # draft put this card first for two of SET_VIEW_SCALE's own.
        print("  c. the words route never puts this card first for a sentence %s declares"
              % WRITE)
        theirs = card_of(WRITE)
        check(theirs is not None and bool(theirs.utterances()),
              "one card is %s, and it declares sentences" % WRITE)
        size = store.count()
        for said in (theirs.utterances() if theirs is not None else []):
            first = [h["id"] for h in SEARCH.keywords(store, said, limit=size)][:1]
            top = (rows.get(first[0]) or {}).get("capability") if first else None
            check(top != CAPABILITY, "  %r: words route #1 is %s" % (said, top or "nothing"))
    finally:
        store.close()


def main():
    home = tempfile.mkdtemp(prefix="heron-read-view-scale-")
    os.environ["HERON_KNOWLEDGE"] = home
    try:
        the_card()
        code = code_only(read(IMPL))
        print()
        check(bool(code), "brain/fragments/read-view-scale/impl/any/fragment.cs is there")
        the_kinds(code)
        the_refusals(code)
        the_template(code)
        the_ratio(code)
        the_read_only(code)
        the_words()
        the_routing()
    finally:
        shutil.rmtree(home, ignore_errors=True)

    print()
    if failures:
        print("FAILED - %d check(s), %d passed:" % (len(failures), passes[0]))
        for one in failures:
            print("  - %s" % one)
        return 1
    print("PASSED - %d checks. Written as text; how it behaves in Revit is NEEDS REAL REVIT."
          % passes[0])
    return 0


if __name__ == "__main__":
    sys.exit(main())
