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
    QUESTIONS  must not be answered by a tool that changes the model (D-86).
               The write line is read from the operation registry, never
               typed here (Golden Rule 19)
    NO TOOL    nothing in Heron does it (5b-322); it must not be answered by
               a tool that changes something else
    OWN        the take-off's own sentences must still reach it

A sentence here is never declared word for word in any card - that would be
answered by identity and test nothing - and that is checked too.

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
phrasings written before any wording was chosen, and those are not here.
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
    ("place a skylight in the roof at a point", "PLACE_HOSTED_FAMILY",
     "5b-326: asked by a session 2026-10-06, answered CREATE_ROOF"),
    ("create an area scheme", "SET_AREA_VOLUME_COMPUTATIONS",
     "5b-337: before the Area and Volume Computations tools, answered APPLY_COLOR_FILL_SCHEME"),
    ("rename the rentable area scheme to net lettable", "SET_AREA_VOLUME_COMPUTATIONS",
     "5b-337: answered APPLY_COLOR_FILL_SCHEME"),
]

QUESTIONS = [
    ("read the thermal U value of every floor type and roof type",
     "5b-319: asked by the same session, answered CREATE_ROOF"),
    ("is volume computation switched on",
     "5b-337: a first draft of SET_AREA_VOLUME_COMPUTATIONS answered it - its card quoted the question"),
    ("check if volume calculation is enabled",
     "5b-337: answered SET_SCHEDULE_FIELD_TOTALS"),
]

NO_TOOL = [
    ("set the project location latitude and longitude to Doha",
     "5b-322: nothing in Heron sets the project location"),
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
            + [t for t, _w in NO_TOOL] + [t for t, _c in OWN])


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

        print("\n4. A QUESTION IS NOT ANSWERED BY A CHANGE")
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
