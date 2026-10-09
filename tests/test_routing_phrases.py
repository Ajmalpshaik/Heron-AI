#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Sentences a real session sent to the wrong tool, asked again on the search a
modeller's Heron actually uses.

    python tests/test_routing_phrases.py

WHY IT EXISTS - FRAGMENT-ISSUES 5b-319
--------------------------------------
On 2026-10-04 a session building a test building through Heron asked
heron_lookup "place doors and windows hosted in walls at points", and the
answer was REPORT_SPACE_ENVELOPE - a READ that takes off every Space's walls
for the loads - with PLACE_HOSTED_FAMILY, the tool that places them, seventh.
A modeller asking for a door was told about a take-off.

Nothing in the repository could have seen it coming:

  * check-routing asks each card its OWN declared words back, and those
    answer by identity before any ranking runs;
  * score-routing and check-risk-crossings report and exit 0 whatever they
    find (D-100), and the second asks questions, never changes;
  * all three run on whatever backend the machine has, and CI has none but
    the spelling one - where this sentence was answered correctly all along.

The cause was two cards. PLACE_HOSTED_FAMILY's version 3 opened its purpose
with its sill rules, and the trained model reads only the first 512
word-pieces of a card, so the door tool stopped looking like one: nearness
rank 1 before that change, 35 after. And the words route matches a typed word
as a prefix - "walls" never matches "wall" - while every phrasing that card
declared was singular and the take-off's were plural.

WHAT IT ASKS
------------
Each sentence below, through heron_retrieve.find - the function heron_lookup
calls - on a PRIVATE store built from this checkout. Never the store every
chat on the machine reads (row 5b-233).

    CHANGES    must reach the tool that makes that change
    QUESTIONS  a question, or a request to select, must not be answered by a
               tool that changes the model (D-86; 5b-329).
               The write line is read from the operation registry, never
               typed here (Golden Rule 19)
    NO TOOL    nothing in Heron does it; it must not be answered by a tool
               that changes something else. Empty since 5b-322's sentence got
               its tool on 2026-10-06 and moved to CHANGES
    READS      a question about the project's own setup reaches its READ
    OWN        the take-off's own sentences must still reach it
    DECLARED   a question that reached a write, declared on the READ that
               answers it (row 146's repair: declaring, not demoting) - it
               must be that card's own, that card must read, and the search
               must answer it by identity

A sentence in any other table is never declared word for word in any card -
that would be answered by identity and test nothing - and that is checked too.

ONLY ON THE TRAINED MODEL, AND IT SAYS SO
-----------------------------------------
The owner's PC ranks with the trained embedding. A machine without it ranks
by spelling, which heron_embed's own docstring calls NOT MEANING, and the two
disagree on exactly these sentences: measured 2026-10-04, "add windows to the
outside walls" reaches the door tool on the model and DIMENSION_WALL_OPENINGS
on spelling. One table asserted on both would be wrong on one of them, and a
table per backend would be a list of excused failures. So with no trained
model this exits 3 - COULD NOT RUN, which is not a pass - and
.github/workflows/gates.yml lists it as not runnable there.

NOT A SCORE
-----------
A handful of sentences that once went wrong, each with its row. It is not the
owner's answer key (tests/data/owner-questions.yaml, which only he changes),
and it is not a target to tune cards against: the cards were checked on
phrasings written before any wording was chosen, and those are not here -
but for the few of 5b-321's and 5b-337's that moved off a write by more than
a rank, kept as guards once the wording was chosen. The rest of those sets,
and the ones that did not move, are in the register rows.
"""

import importlib.util
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

for _stream in (sys.stdout, sys.stderr):
    try:
        _stream.reconfigure(encoding="utf-8", errors="replace")
    except (AttributeError, ValueError):
        pass

# (sentence, the capability that must answer it, why it is here)
CHANGES = [
    ("place doors and windows hosted in walls at points", "PLACE_HOSTED_FAMILY",
     "5b-319: asked by a session 2026-10-04, answered REPORT_SPACE_ENVELOPE"),
    ("place a door in this wall", "PLACE_HOSTED_FAMILY",
     "5b-319: answered DIMENSION_WALL_OPENINGS"),
    ("add windows to the outside walls", "PLACE_HOSTED_FAMILY",
     "5b-319: answered REPORT_SPACE_ENVELOPE"),
    ("put doors in the walls at these points", "PLACE_HOSTED_FAMILY",
     "5b-319: answered CREATE_WALL"),
    # 5b-322 was a NO TOOL row until SET_PROJECT_LOCATION was built
    # (2026-10-06); the sentence it guarded now has a tool, and must reach it.
    ("set the project location latitude and longitude to Doha", "SET_PROJECT_LOCATION",
     "5b-322: asked by the loads session 2026-10-04, when nothing could"),
    ("turn true north thirty degrees to the east", "SET_PROJECT_LOCATION",
     "5b-322: Manage > Position, part of the same job"),
    ("show cooling and heating loads in Btu/h and airflow in CFM", "SET_PROJECT_UNITS",
     "5b-322: NEEDS-CHECKING CC12 needs a model in Btu/h and CFM"),
    ("place a skylight in the roof at a point", "PLACE_HOSTED_FAMILY",
     "5b-326: asked by a session 2026-10-06, answered CREATE_ROOF"),
    ("create an area scheme", "SET_AREA_VOLUME_COMPUTATIONS",
     "5b-337: before the Area and Volume Computations tools, answered APPLY_COLOR_FILL_SCHEME"),
    ("rename the rentable area scheme to net lettable", "SET_AREA_VOLUME_COMPUTATIONS",
     "5b-337: answered APPLY_COLOR_FILL_SCHEME"),
    ("change the area scheme name from rentable to lettable", "SET_AREA_VOLUME_COMPUTATIONS",
     "5b-337: still answered APPLY_COLOR_FILL_SCHEME, a different change, after the tools existed"),
    # Row 146, 2026-10-09: the first draft of READ_VIEW_SCALE's purpose said
    # "scale" and "view" more often than SET_VIEW_SCALE, and a review asked
    # these back on private stores - the read took the first two from the
    # write on the model and the first, third, fourth and fifth on spelling
    # (where this suite cannot run; tests/test_read_view_scale.py asks them
    # there).
    ("update the view scale", "SET_VIEW_SCALE",
     "146: a first draft of READ_VIEW_SCALE answered it on both backends"),
    ("set the scale of these views", "SET_VIEW_SCALE",
     "146: a first draft of READ_VIEW_SCALE answered it on the model"),
    ("set the view scale", "SET_VIEW_SCALE",
     "146: a first draft of READ_VIEW_SCALE answered it on spelling"),
    ("set view scale", "SET_VIEW_SCALE",
     "146: a first draft of READ_VIEW_SCALE answered it on spelling"),
    ("adjust the view scale", "SET_VIEW_SCALE",
     "146: a first draft of READ_VIEW_SCALE answered it on spelling"),
    # Row 146, 2026-10-09: the first draft of LIST_VIEWS_ON_SHEET named the
    # write and said "placed" in its purpose and "on sheet a-101" in its
    # utterances, and the read took these from the write on the model.
    ("place these views on sheet A-101", "PLACE_VIEWS_ON_SHEET",
     "146: a first draft of LIST_VIEWS_ON_SHEET answered it"),
    ("place the views on this sheet", "PLACE_VIEWS_ON_SHEET",
     "146: a first draft of LIST_VIEWS_ON_SHEET answered it"),
    # And a later draft declared "list the viewports on this sheet", and the
    # read took this one - claimed in ALIGN_VIEWPORTS_ACROSS_SHEETS' routing
    # table, never declared - on the model.
    ("line the viewports up", "ALIGN_VIEWPORTS_ACROSS_SHEETS",
     "146: a later draft of LIST_VIEWS_ON_SHEET answered it"),
]

QUESTIONS = [
    ("read the thermal U value of every floor type and roof type",
     "5b-319: asked by the same session, answered CREATE_ROOF"),
    ("where is this project", "5b-322: a question about the site never reaches its setter"),
    ("which units does this model show cooling load in",
     "5b-322: a question about the units never reaches their setter"),
    ("does this project use daylight saving",
     "5b-346: answered SAVE_DOCUMENT before the site's read could say it"),
    ("is project north rotated from true north",
     "5b-346: answered ROTATE_ELEMENTS_ABOUT_AXIS before the site's read could say it"),
    ("are the project units metric or imperial",
     "5b-346: answered CREATE_PIPE_SEGMENT before the units' read existed"),
    ("is volume computation switched on",
     "5b-337: a first draft of SET_AREA_VOLUME_COMPUTATIONS answered it - its card quoted the question"),
    ("check if volume calculation is enabled",
     "5b-337: answered SET_SCHEDULE_FIELD_TOTALS"),
    ("are room areas taken to the wall finish",
     "5b-337: answered PLACE_ROOM_AT_POINT until the read's opening named the wall faces"),
    # 5b-321: questions about the doors and windows already in a wall, written
    # 2026-10-09 before SELECT_BY_HOST's opening was reworded. Each answered
    # PLACE_HOSTED_FAMILY, a change, on the cards as they stood.
    ("count the doors in these walls",
     "5b-321: answered PLACE_HOSTED_FAMILY"),
    ("select every window in this wall",
     "5b-321: answered PLACE_HOSTED_FAMILY"),
    ("find the doors in the selected walls",
     "5b-321: answered PLACE_HOSTED_FAMILY"),
    # A REQUEST TO SELECT IS HELD TO THE SAME RULE - nothing in it asks for a
    # change. 5b-329: "select every wall of type Curtain Wall" reached
    # SET_CURTAIN_WALL_GRID, and these paraphrases, written before any card
    # was reworded, reached it or SET_CURTAIN_WALL_MULLIONS on 2026-10-06.
    ("select all walls of type Curtain Wall",
     "5b-329: answered SET_CURTAIN_WALL_MULLIONS"),
    ("select all the curtain walls of type Storefront",
     "5b-329: answered SET_CURTAIN_WALL_GRID"),
    ("select all elements of type Curtain Wall",
     "5b-329: answered SET_CURTAIN_WALL_GRID"),
    ("select all instances of the Storefront curtain wall type",
     "5b-329: answered SET_CURTAIN_WALL_GRID"),
    # NOT HERE: "select every wall that uses the Curtain Wall type", the fifth
    # paraphrase. SET_CURTAIN_WALL_GRID and REPORT_CURTAIN_WALL_TYPE fuse to the
    # same score on it, and the PROVEN nudge hands the tie to the change - still
    # OPEN in 5b-329. A guard that a status change flips is not a guard.
    # Row 146: rewordings of LIST_VIEWS_ON_SHEET's question, written by its
    # repair and its second review on 2026-10-09, before its purpose said
    # "legends". Both reached CREATE_LEGEND_VIEW on the model while the card
    # said only "a legend".
    ("which legends are on this sheet",
     "146: answered CREATE_LEGEND_VIEW"),
    ("what legends are on this sheet",
     "146: answered CREATE_LEGEND_VIEW"),
]

# A question about the project's own setup reaches the tool that READS it.
# Three site questions still reach SET_PROJECT_LOCATION and are NOT here -
# row 5b-346 records them, measured; this guards the ones the change moved.
READS = [
    ("where is this project", "REPORT_LOCATION"),
    ("is project north rotated from true north", "REPORT_LOCATION"),
    ("are the project units metric or imperial", "REPORT_PROJECT_UNITS"),
]

NO_TOOL = []

# A question measured reaching a write, with a READ that truly answers it -
# so the repair is one line in that READ's utterances, and identity answers it
# before ranking runs (rows 116 and 146). (sentence, the READ, why)
DECLARED = [
    ("how high is this off the floor", "READ_ELEMENT_LEVEL",
     "146: answered SET_ROOM_LIMITS on the model, MOVE_TO_RAY_HIT on spelling"),
    ("find the fire dampers", "SELECT_BY_FAMILY",
     "146: answered PLACE_ACCESSORY_ON_RUN on spelling"),
    ("what levels are in this model", "LIST_LEVELS",
     "146: answered CREATE_LEVELS on spelling"),
    ("what is the insulation thickness", "READ_ELEMENT_PARAMETERS",
     "146: answered SET_MEP_INSULATION on both"),
    ("what is the sill height of these windows", "READ_ELEMENT_PARAMETERS",
     "5b-321: answered PLACE_HOSTED_FAMILY on the model"),
    # 146's last READ gap: nothing read a view's scale until READ_VIEW_SCALE
    # (2026-10-09), so the question could only land on the write.
    ("what is the scale of this view", "READ_VIEW_SCALE",
     "146: answered SET_VIEW_SCALE on both"),
    ("what scale is this plan at", "READ_VIEW_SCALE",
     "146: answered CREATE_CALLOUT on the model"),
    # Row 146's gap, not a mis-route: LIST_SHEETS only COUNTS a sheet's views,
    # so no READ named them until LIST_VIEWS_ON_SHEET was written (2026-10-09).
    ("what views are on this sheet", "LIST_VIEWS_ON_SHEET",
     "146: answered PLACE_VIEWS_ON_SHEET on both"),
]

OWN = [
    ("read every space's envelope", "REPORT_SPACE_ENVELOPE"),
    ("read every Space's walls, windows and roofs for the load calculation",
     "REPORT_SPACE_ENVELOPE"),
    ("take off the walls and windows of every space for the loads",
     "REPORT_SPACE_ENVELOPE"),
]

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def write_line():
    """(ladder, threshold) from the operation registry - Golden Rule 19."""
    path = os.path.join(ROOT, "tools", "generate-jobs.py")
    spec = importlib.util.spec_from_file_location("heron_tool_generate_jobs", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    _name, threshold, ladder = module.write_threshold()
    return ladder, threshold


def sentences():
    return ([t for t, _c, _w in CHANGES] + [t for t, _w in QUESTIONS]
            + [t for t, _c in READS] + [t for t, _w in NO_TOOL] + [t for t, _c in OWN])


def main():
    home = tempfile.mkdtemp(prefix="heron-routing-phrases-")
    os.environ["HERON_KNOWLEDGE"] = home
    store = None
    try:
        import heron_embed as EMBED
        backend, why = EMBED.backend()
        if backend != EMBED.MODEL:
            print("Sentences a session sent to the wrong tool")
            print("  COULD NOT RUN - no trained embedding on this machine:")
            print("    %s" % why)
            print("\n  This is exit 3, which is NOT a pass: nothing was asked. The")
            print("  sentences are measured on the search the owner's Heron uses,")
            print("  and spelling alone disagrees with it on them (see the top of")
            print("  this file). Install the model and run it again:")
            print("    pip install --user model2vec")
            return 3

        import heron_scope as SCOPE
        import heron_search as SEARCH
        import heron_capability as CAP
        import heron_retrieve as RETRIEVE
        import heron_fragment as FRAG

        print("1. A PRIVATE STORE, BUILT FROM THIS CHECKOUT, ON THE TRAINED MODEL")
        check(os.path.abspath(SCOPE.knowledge_dir()) == os.path.abspath(home),
              "the store is this run's own folder, never the shared one")
        built, problems = SCOPE.rebuild()
        check(built > 100 and not problems,
              "the library was built: %d cards, %d problem(s)" % (built, len(problems)))
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        SCOPE.refresh(store)
        CAP.rebuild(store)
        SEARCH.index(store)
        EMBED.index(store)
        stamps = set(r["backend"] for r in store.execute(
            "SELECT backend FROM vectors WHERE COALESCE(kind, 'fragment') = 'fragment'"))
        check(stamps and all(s.startswith(EMBED.MODEL) for s in stamps),
              "every card's vector came from the trained model: %s" % ", ".join(sorted(stamps)))

        rows = dict((r["id"], r) for r in store.fragments())
        ladder, threshold = write_line()

        def ask(text):
            answer = RETRIEVE.find(store, text)
            row = rows.get(answer.fragment_id) or {}
            return answer, row.get("capability"), (row.get("risk") or "").upper()

        def writes(risk):
            return ladder.get(risk, -1) >= threshold

        print("\n2. NONE OF THEM IS DECLARED WORD FOR WORD, SO RANKING DECIDES")
        on_disk, _ = FRAG.load_all()
        declared = set()
        for frag in on_disk.values():
            for said in [frag.data.get("semantic-identity") or ""] + frag.utterances():
                declared.add(SEARCH.normalise(said))
        for text in sentences():
            check(SEARCH.normalise(text) not in declared,
                  "no card declares %r" % text)

        print("\n3. A CHANGE ASKED FOR REACHES THE TOOL THAT MAKES IT")
        for text, want, why in CHANGES:
            answer, got, _risk = ask(text)
            check(got == want, "%r -> %s (%s)" % (text, got, why))
            check(answer.route != "identity",
                  "  and it was ranked, not matched word for word (%s)" % answer.route)

        print("\n4. A QUESTION OR A SELECTION IS NOT ANSWERED BY A CHANGE")
        for text, why in QUESTIONS:
            _answer, got, risk = ask(text)
            check(not writes(risk), "%r -> %s, %s (%s)" % (text, got, risk or "no risk", why))

        print("\n5. A CHANGE NOTHING MAKES IS NOT ANSWERED BY ANOTHER CHANGE")
        for text, why in NO_TOOL:
            _answer, got, risk = ask(text)
            check(not writes(risk), "%r -> %s, %s (%s)" % (text, got, risk or "no risk", why))

        print("\n6. THE TAKE-OFF STILL ANSWERS ITS OWN")
        for text, want in OWN:
            _answer, got, _risk = ask(text)
            check(got == want, "%r -> %s" % (text, got))

        print("\n7. A QUESTION ABOUT THE PROJECT'S SETUP REACHES ITS READER")
        for text, want in READS:
            _answer, got, risk = ask(text)
            check(got == want and not writes(risk), "%r -> %s, %s" % (text, got, risk or "no risk"))

        print("\n8. A QUESTION THAT REACHED A WRITE IS DECLARED ON THE READ THAT ANSWERS IT")
        owners = {}
        for frag in on_disk.values():
            for said in [frag.data.get("semantic-identity") or ""] + frag.utterances():
                owners.setdefault(SEARCH.normalise(said), set()).add(frag.data.get("capability"))
        risk_of = dict((frag.data.get("capability"), (frag.data.get("risk") or "").upper())
                       for frag in on_disk.values())
        for text, want, why in DECLARED:
            claimed = sorted(owners.get(SEARCH.normalise(text), set()))
            check(claimed == [want], "%r is declared by %s alone (%s) - declared by %s"
                  % (text, want, why, ", ".join(claimed) or "nobody"))
            check(want in risk_of and not writes(risk_of.get(want)),
                  "  and %s reads: %s" % (want, risk_of.get(want) or "no such card"))
            answer, got, _risk = ask(text)
            check(answer.route == "identity" and got == want,
                  "  and the search answers it by identity: %s, %s" % (got, answer.route))
    finally:
        if store is not None:
            store.close()
        shutil.rmtree(home, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for one in FAILURES:
            print("  - %s" % one)
        return 1
    print("PASSED - every sentence reached what it asked for.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
