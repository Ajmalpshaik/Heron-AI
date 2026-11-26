#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A tool's own declared words, asked on a Revit release it leaves out, name that
tool - whatever the ranking. Row 5b-1837, issue #1217. Runs without Revit, on
a scratch knowledge store built from this checkout.

    python tests/test_walled_own_words.py

WHAT HAPPENED
-------------
`heron_brain.lookup` names a tool the version wall left out only when the
request, ranked again with the wall lifted, reaches it FIRST. A tool's exact
declared phrase reaches it by identity only where it is offered, so on a
release it leaves out its own words were ranked like any sentence and another
tool could come first: on Revit 2024 "dimension the radius" - CREATE_RADIAL_DIMENSION's
words, declared 2025 on - was offered LABEL_FAMILY_RADIUS, a family's radius
label, and nothing named the tool the words were written for. Measured
2026-11-16 on seven phrases of five tools; tools/check-routing.py cannot see
it, because it asks with no release, where identity answers every tool's own
words exactly.

WHAT THIS HOLDS
---------------
  1. The measured phrases, each on a release its tool leaves out, name that
     tool - as `other_release`, or as the tool `done_here_by` stands in for.
  2. EVERY release-limited tool's every declared phrase, on every release it
     leaves out, names that tool the same way - the check the row owed.
  3. On a release the tool IS declared for, its own words still reach it by
     identity, and nothing is named beside it.

WHAT THIS DOES NOT PROVE: whether the tool offered in its place does the job
(each tool's own question), or what the trained model (`model`, the owner's
PC) ranks first. Only the identity table is read for (1) and (2), so they hold
on every backend; this checkout's own backend is what (3) ran on.
"""

from __future__ import print_function

import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "tests", "support"))

import scratch_audit                                            # noqa: E402

# This run's audit lines go to a scratch folder, never the owner's real trail
# (row 5b-1070) - and its store is a scratch one, never the shared one every
# live chat reads (row 5b-233).
scratch_audit.plant()
SCRATCH = tempfile.mkdtemp(prefix="heron-walled-words-")
os.environ["HERON_KNOWLEDGE"] = SCRATCH

import heron_brain as BRAIN                                     # noqa: E402

FAILURES = []

# Row 5b-1837's seven, each on the releases it was measured on.
MEASURED = (
    ("dimension the radius", "CREATE_RADIAL_DIMENSION", ("2020", "2024")),
    ("dimension this radius", "CREATE_RADIAL_DIMENSION", ("2020", "2024")),
    ("show the radius on the drawing", "CREATE_RADIAL_DIMENSION", ("2020", "2024")),
    ("make a pdf of the drawings", "EXPORT_SHEETS_TO_PDF", ("2020", "2021")),
    ("draw the property line through these points", "CREATE_PROPERTY_LINE", ("2024", "2026")),
    ("mark out a planted bed on the site", "EDIT_TOPOSOLID", ("2020", "2023")),
    ("why does the link show the wrong level in this plan", "REPORT_LINKED_VIEW_USAGE",
     ("2020", "2023")),
)


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def named(found):
    """The walled tool the answer names, or None."""
    other = found.get("other_release") or {}
    if other.get("capability"):
        return other["capability"]
    instead = (found.get("done_here_by") or {}).get("instead_of") or {}
    return instead.get("capability")


def releases(row):
    return [v for v in (row["revit"] or "").split(",") if v]


def measured_section():
    print("The phrases row 5b-1837 measured")
    for words, capability, on in MEASURED:
        for release in on:
            found = BRAIN.lookup(words, revit=release)
            check(named(found) == capability,
                  "\"%s\" on Revit %s names %s (offered %s, named %s)"
                  % (words, release, capability, found.get("capability"), named(found)))


def library_section():
    print()
    print("Every release-limited tool's own words, on every release it leaves out")
    _S, _C, _SK, SEARCH, _E, RETRIEVE = BRAIN._brain()
    with BRAIN._Open() as store:
        rows = [r for r in store.fragments() if r["status"] in RETRIEVE.OFFERABLE]
        every = sorted(set(v for r in rows for v in releases(r)))
        limited = [r for r in rows if set(every) - set(releases(r))]
        asks = []
        for row in limited:
            phrases = sorted(set(p["phrase"] for p in store.execute(
                "SELECT phrase FROM identities WHERE fragment_id = ?", (row["id"],)).fetchall()))
            for phrase in phrases:
                if SEARCH.short_circuit(store, phrase)[0] != row["id"]:
                    continue    # shared by two cards: the identity table keeps neither
                asks.append((row, phrase, releases(row)))
    check(len(limited) > 0 and len(asks) > 0,
          "the library has release-limited tools with declared words to ask (%d tools, %d "
          "phrases)" % (len(limited), len(asks)))
    missed = []
    for row, phrase, theirs in asks:
        for release in every:
            if release in theirs:
                continue
            found = BRAIN.lookup(phrase, revit=release)
            if named(found) != row["capability"]:
                missed.append("\"%s\" on %s -> offered %s, named %s, not %s"
                              % (phrase, release, found.get("capability"), named(found),
                                 row["capability"]))
    for line in missed[:15]:
        print("        " + line)
    check(not missed, "each one names its own tool (%d missed)" % len(missed))


def unchanged_section():
    print()
    print("On a release the tool is declared for, nothing changes")
    _S, _C, _SK, _SEARCH, _E, _RETRIEVE = BRAIN._brain()
    with BRAIN._Open() as store:
        declared = dict((r["capability"], releases(r)) for r in store.fragments())
    for words, capability, _on in MEASURED:
        own = declared.get(capability) or []
        check(bool(own), "%s is declared for a release at all" % capability)
        if not own:
            continue
        found = BRAIN.lookup(words, revit=own[-1])
        check(found.get("capability") == capability and found.get("route") == "identity"
              and not named(found),
              "\"%s\" on Revit %s reaches %s by identity, nothing named beside it (got %s by %s)"
              % (words, own[-1], capability, found.get("capability"), found.get("route")))


def main():
    try:
        measured_section()
        library_section()
        unchanged_section()
    finally:
        shutil.rmtree(SCRATCH, ignore_errors=True)
    print()
    if FAILURES:
        print("FAILED - %d check(s)" % len(FAILURES))
        return 1
    print("PASSED - a tool's own words, asked on a release it leaves out, name that tool.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
