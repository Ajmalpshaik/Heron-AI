# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Two lifecycle ladders, one confirmed and one proposed, and the code is split.

    python tests/test_lifecycle_ladder.py

THIS SUITE DECIDES NOTHING. It makes a policy conflict visible and stops it
drifting further, which is what AGENTS.md asks for when two sources
disagree: record both, name the governing decision, name the observed
evidence, and name who resolves it.

THE TWO LADDERS
----------------
CONFIRMED - docs/00-master-specification.md section 18, the fragment
lifecycle, eight stages and no SHADOW:

    DISCOVERED DRAFT TESTING VALIDATED PROVEN PRODUCTION DEPRECATED ARCHIVED

PROPOSED - docs/24-trust-model.md Axis 1, the same eight with SHADOW
inserted between VALIDATED and PROVEN. That document's own header says it
"proposes a resolution and needs confirmation - see Q-34", and D-14 stays
Proposed: the owner answered "yes, but show me on screen first" twice,
eleven days apart, and R1b in NEEDS-CHECKING.md is the confirmation.
Only a screen closes it.

WHY THIS SUITE EXISTS
----------------------
On 2026-09-21 the declarations were split across the two. Two modules have
adopted an unconfirmed proposal and four have not: tools/check-metadata.py
accepts `Heron-Status: SHADOW` and brain/heron_deployment.py promotes
through it with its own gate, while heron_fragment.can_promote answers
"'SHADOW' is not a lifecycle state" and STATUSES.index raises ValueError
for a fragment already there, and heron_capability.TRUST.get(status, 0)
would rank it 0 - the value of DEPRECATED, below DRAFT and DISCOVERED.

So a file can be given the status, pass the gate, be promoted through it,
and break three modules. Nothing carries SHADOW today, so it is latent
either way.

AND THE FIRST VERSION OF THIS SUITE GOT IT BACKWARDS
------------------------------------------------------
It read docs/24 as policy and required every declaration to match it -
which would have pushed three more modules onto the unconfirmed side, in a
repository whose own rule is that running code never silently rewrites
policy and a rule is never edited to match code. The mistake was not
reading far enough: docs/24's header, docs/README.md ("two items need
confirmation before building"), D-14 and Q-34 all say the same thing, and
any one of them was enough. FRAGMENT-ISSUES row 5b-75 records it.

WHAT IT FAILS ON
-----------------
A declaration matching NEITHER ladder. Everything else is reported, and the
split is printed in full so nobody has to grep for it.
"""

from __future__ import annotations

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_fragment as FRAG                                 # noqa: E402
import heron_capability as CAP                                # noqa: E402
import heron_deployment as DEP                                # noqa: E402
import heron_retrieve as RET                                  # noqa: E402

SPEC = os.path.join(ROOT, "docs", "00-master-specification.md")
TRUST_MODEL = os.path.join(ROOT, "docs", "24-trust-model.md")

# Both sections are found by heading rather than by line number, so moving
# one does not silently empty this suite.
SPEC_SECTION = "## 18. Fragment Lifecycle"
AXIS_1 = "### Axis 1"

# What each declaration deliberately leaves out. A subset with no DECLARED
# exclusions cannot be told from an accident, which is how two of these were
# nearly mis-read as complete.
RETIRED_TWO = frozenset(("DEPRECATED", "ARCHIVED"))
EXCLUDES = {
    ("brain/heron_retrieve.py", "OFFERABLE"): (
        RETIRED_TWO,
        "they exist so a record is never destroyed (Golden Rule 4), not so "
        "they can be handed back as answers"),
    ("brain/heron_deployment.py", "LADDER"): (
        RETIRED_TWO,
        "retirement is not promotion - heron_deployment.RETIRED holds those"),
}

# The one declaration that is the COMPLEMENT rather than a subset.
COMPLEMENT = ("brain/heron_deployment.py", "RETIRED")

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def section(path, heading, pattern):
    """Stage names matching `pattern` inside one section of one document."""
    try:
        with io.open(path, encoding="utf-8") as fh:
            text = fh.read()
    except IOError:
        return []
    at = text.find(heading)
    if at < 0:
        return []
    after = at + len(heading)
    ends = [e for e in (text.find("\n## ", after),
                        text.find("\n### ", after)) if e > 0]
    block = text[at:min(ends)] if ends else text[at:]
    return [m.group(1) for m in re.finditer(pattern, block, re.M)]


def from_check_metadata():
    """check-metadata.py's STATUSES, read without importing the gate."""
    with io.open(os.path.join(ROOT, "tools", "check-metadata.py"),
                 encoding="utf-8") as fh:
        m = re.search(r'^STATUSES\s*=\s*\{[^}]*\}', fh.read(), re.M | re.S)
    if not m:
        return ()
    scope = {}
    exec(compile(m.group(0), "<check-metadata>", "exec"), scope)
    return tuple(scope["STATUSES"])


def main():
    confirmed = tuple(section(SPEC, SPEC_SECTION, r'^([A-Z]{5,})\s*$'))
    proposed = tuple(section(TRUST_MODEL, AXIS_1, r'^\|\s*`([A-Z]+)`\s*\|'))

    print("\nThe two ladders, read out of the documents that carry them")
    check(len(confirmed) >= 8,
          "docs/00 s18 (CONFIRMED) names %d: %s"
          % (len(confirmed), ", ".join(confirmed)))
    check(len(proposed) >= 8,
          "docs/24 Axis 1 (PROPOSED) names %d: %s"
          % (len(proposed), ", ".join(proposed)))
    if not confirmed or not proposed:
        print("\nFAILED - a ladder could not be read, so this suite compared "
              "nothing. That is not a pass.")
        return 1

    extra = tuple(s for s in proposed if s not in confirmed)
    check(extra == ("SHADOW",),
          "the proposal adds %s and nothing else, so this suite is looking "
          "at the one difference it was written for"
          % (", ".join(extra) or "nothing"))

    print("\nWhich ladder each declaration follows")
    rows = (
        ("brain/heron_fragment.py", "STATUSES", tuple(FRAG.STATUSES)),
        ("brain/heron_capability.py", "TRUST", tuple(CAP.TRUST)),
        ("brain/heron_retrieve.py", "OFFERABLE", tuple(RET.OFFERABLE)),
        ("brain/heron_retrieve.py", "QUALITY", tuple(RET.QUALITY)),
        ("brain/heron_deployment.py", "LADDER", tuple(DEP.LADDER)),
        (COMPLEMENT[0], COMPLEMENT[1], tuple(DEP.RETIRED)),
        ("tools/check-metadata.py", "STATUSES", from_check_metadata()),
    )

    following = {"CONFIRMED": [], "PROPOSED": [], "BOTH": [], "NEITHER": []}
    for rel, name, got in rows:
        held = set(got)
        if (rel, name) == COMPLEMENT:
            # The two stages reached deliberately rather than by climbing.
            # They are the same on either ladder, so this one cannot take a
            # side and says so.
            side = "BOTH" if held == set(RETIRED_TWO) else "NEITHER"
        else:
            out, _ = EXCLUDES.get((rel, name), (frozenset(), None))
            sides = [label for label, ladder in
                     (("CONFIRMED", confirmed), ("PROPOSED", proposed))
                     if held == set(ladder) - set(out)]
            side = sides[0] if len(sides) == 1 else (
                "BOTH" if sides else "NEITHER")
        following[side].append("%s %s" % (rel, name))
        check(side != "NEITHER",
              "%-42s %-10s %s"
              % (rel, name,
                 side if side != "NEITHER" else
                 "<- matches NEITHER ladder: %s" % sorted(held)))

    print("\nThe split, in one place")
    for label in ("CONFIRMED", "PROPOSED", "BOTH", "NEITHER"):
        if following[label]:
            print("  %-10s %d" % (label, len(following[label])))
            for one in following[label]:
                print("               %s" % one)

    if following["CONFIRMED"] and following["PROPOSED"]:
        print("\n  BOTH SIDES ARE POPULATED, AND THAT IS THE OPEN ITEM.")
        print("  docs/24 says of itself that it 'proposes a resolution and")
        print("  needs confirmation - see Q-34'. D-14 stays Proposed: the")
        print("  owner said 'yes, but show me on screen first', twice,")
        print("  eleven days apart. R1b in NEEDS-CHECKING.md is the")
        print("  confirmation, and only a screen closes it.")
        print()
        print("  So this is NOT failed here. Moving a module to either side")
        print("  is the owner's call, not a tidy-up - FRAGMENT-ISSUES row")
        print("  5b-75 records an attempt to tidy it the wrong way, and why")
        print("  it was withdrawn.")

    print()
    if FAILURES:
        print("FAILED")
        for one in FAILURES:
            print("  - %s" % one)
        print("\n  A declaration matching NEITHER ladder is the one thing")
        print("  this suite refuses. Bring it onto one of them - or, if it")
        print("  is a different ladder entirely, this suite is the wrong")
        print("  place for it.")
        return 1
    print("PASSED - every declaration follows one of the two ladders, and the "
          "split is\n         reported rather than resolved. Q-34 and R1b own "
          "resolving it.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
