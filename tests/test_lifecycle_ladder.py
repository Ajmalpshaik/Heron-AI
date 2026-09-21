# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
One lifecycle ladder, declared six times, and four copies were a rung short.

    python tests/test_lifecycle_ladder.py

`docs/24-trust-model.md` exists because six lifecycle vocabularies had grown
up side by side. Its Axis 1 reconciled them into one, and says what it
applies to: **"fragments, skills, capabilities and agents"**. That table is
the one home, and this suite reads the ladder out of it rather than out of
any of the copies.

WHY THIS SUITE EXISTS
----------------------
On 2026-09-21 the ladder was declared in six places and FOUR were missing
`SHADOW`:

    tools/check-metadata.py    STATUSES   has it - a file may be marked it
    brain/heron_deployment.py  LADDER     has it - promotion goes through it
    brain/heron_fragment.py    STATUSES   did NOT
    brain/heron_capability.py  TRUST      did NOT
    brain/heron_retrieve.py    OFFERABLE  did NOT
    brain/heron_retrieve.py    QUALITY    did NOT

`can_promote(frag, "SHADOW")` answered *"not a lifecycle state"*, and a
fragment already at SHADOW made `STATUSES.index` raise `ValueError`.
`TRUST.get(status, 0)` filed the unknown status as **0** - the value of
`DEPRECATED` - so a SHADOW provider ranked below `DRAFT` and below
`DISCOVERED`. And in the module that answers a modeller's request, a SHADOW
fragment was never OFFERED at all - while docs/24 puts SHADOW at
**L3 - VERIFIED**, beside `PROVEN`, allowed to MODIFY with a preview the
user accepts. Nothing carried SHADOW that day, so it was latent: it would
have bitten on the first day Shadow Mode was used, which is the mechanism
Golden Rule 13 and D-84 both lean on. FRAGMENT-ISSUES row 5b-75.

THE LAST TWO WERE FOUND BY AN EARLIER DRAFT OF THIS SUITE, AND IT PASSED
THEM. `OFFERABLE` and `QUALITY` are both subsets by design, and a subset
with no DECLARED exclusions cannot be told from an accident - so each one
now has to name what it leaves out and why, in `EXCLUDES` below. That is
the half of this suite worth copying.

`brain/heron_deployment.py` had already written the rule this broke:

    The ladder is the trust model; an optional rung is not a rung.

THE ONE HOME IS THE DOCUMENT, NOT A MODULE
--------------------------------------------
`tests/test_supported_releases.py` holds five copies of the Revit release
list against `heron_dotnet.RELEASES`, because a module owns that fact. Here
no module owns it - four of them carry it and docs/24 is what they were
reconciled to - so the document is read and the modules are checked against
it. Same answer, different one home.

WHAT IT DOES NOT COVER
-----------------------
`heron_ingest.STATUSES` is `("DRAFT", "REVIEWED", "RETIRED")` and is a
different ladder entirely - Golden Rule 10's knowledge lifecycle, not this
one. It is named in `NOT_THIS_LADDER` rather than pattern-matched away.

Nothing here says a stage is implemented. It says every declaration of the
ladder agrees with the document that owns it.
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

# Where docs/24's ladder lives, named by its heading rather than by a line
# number so moving the section does not silently empty this suite.
TRUST_MODEL = os.path.join(ROOT, "docs", "24-trust-model.md")
AXIS_1 = "### Axis 1"

# A ladder that is NOT this one, with the reason. Golden Rule 10's knowledge
# lifecycle is three words that happen to include DRAFT; it is not a short
# copy of the nine.
NOT_THIS_LADDER = {
    ("brain/heron_ingest.py", "STATUSES"):
        "Golden Rule 10's knowledge lifecycle (DRAFT, REVIEWED, RETIRED), a "
        "different ladder that shares one word",
}

# Where a fifth copy would be found. tests/ is excluded: a suite may use
# three stages as a fixture, and a fixture is not a declaration.
ROOTS = ("brain", "mcp", "platform", "revit", "tools")
SKIP_FOLDERS = ("__pycache__", "bin", "obj", ".vs", "fragments")

# A module-level constant whose value is a tuple, list, set or dict holding
# lifecycle stage names. Three or more, so a pair of words in passing is not
# mistaken for a declaration of the ladder.
DECLARATION = re.compile(
    r'^([A-Z][A-Z0-9_]*)\s*=\s*[\[\(\{]([^\]\)\}]*)[\]\)\}]', re.M | re.S)

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def the_ladder():
    """The stages docs/24 Axis 1 names, in the order it names them."""
    with io.open(TRUST_MODEL, encoding="utf-8") as fh:
        text = fh.read()
    at = text.find(AXIS_1)
    if at < 0:
        return []
    # To the next heading of the same level or deeper.
    end = text.find("\n### ", at + len(AXIS_1))
    if end < 0:
        end = text.find("\n## ", at + len(AXIS_1))
    block = text[at:end if end > 0 else len(text)]
    return [m.group(1) for m in
            re.finditer(r'^\|\s*`([A-Z]+)`\s*\|', block, re.M)]


def declarations(stages):
    """Every module-level constant that names three or more of the stages."""
    words = set(stages)
    found = []
    for top in ROOTS:
        base = os.path.join(ROOT, top)
        if not os.path.isdir(base):
            continue
        for where, dirs, files in os.walk(base):
            dirs[:] = [d for d in dirs if d not in SKIP_FOLDERS]
            for name in sorted(files):
                if not name.endswith(".py"):
                    continue
                path = os.path.join(where, name)
                rel = os.path.relpath(path, ROOT).replace(os.sep, "/")
                with io.open(path, encoding="utf-8", errors="replace") as fh:
                    text = fh.read()
                for m in DECLARATION.finditer(text):
                    named = set(re.findall(r'[A-Z]{4,}', m.group(2))) & words
                    if len(named) >= 3:
                        line = text[:m.start()].count("\n") + 1
                        found.append((rel, line, m.group(1), named))
    return sorted(found)


def main():
    stages = the_ladder()

    print("\nThe one home - docs/24 Axis 1, the table that reconciled six")
    check(len(stages) >= 8,
          "it names %d stage(s): %s" % (len(stages), ", ".join(stages)))
    check("SHADOW" in stages,
          "SHADOW is one of them - docs/24 says it belongs in the COMMON "
          "lifecycle, not only the agent one")
    if not stages:
        print("\nFAILED - docs/24's Axis 1 table could not be read, so this "
              "suite compared nothing.")
        return 1

    ladder = tuple(stages)
    print("\nEvery declaration agrees with it")

    check(tuple(FRAG.STATUSES) == ladder,
          "brain/heron_fragment.STATUSES%s"
          % ("" if tuple(FRAG.STATUSES) == ladder else
             "  <- has %s ; docs/24 says %s"
             % (list(FRAG.STATUSES), list(ladder))))

    missing = [s for s in ladder if s not in CAP.TRUST]
    check(not missing,
          "brain/heron_capability.TRUST ranks every stage%s"
          % ("" if not missing else "  <- cannot rank %s" % missing))

    if not missing:
        ranked = sorted(ladder, key=lambda s: CAP.TRUST[s])
        retired = [s for s in ladder if s in getattr(DEP, "RETIRED", ())]
        rising = [s for s in ladder if s not in retired]
        # The retired stages are deliberately ranked BELOW the rising ones -
        # a deprecated provider must never be chosen over a draft - so the
        # order to check is the rising part of the ladder.
        check([s for s in ranked if s in rising] == rising,
              "and ranks them in the ladder's own order%s"
              % ("" if [s for s in ranked if s in rising] == rising else
                 "  <- ranks them %s"
                 % [s for s in ranked if s in rising]))

    check(CAP.UNKNOWN_STATUS < min(CAP.TRUST.values()),
          "an unrankable status sorts below every known one (%d < %d)"
          % (CAP.UNKNOWN_STATUS, min(CAP.TRUST.values())))

    whole = tuple(DEP.LADDER) + tuple(DEP.RETIRED)
    check(set(whole) == set(ladder),
          "brain/heron_deployment LADDER + RETIRED%s"
          % ("" if set(whole) == set(ladder) else
             "  <- only there: %s ; only in docs/24: %s"
             % (sorted(set(whole) - set(ladder)),
                sorted(set(ladder) - set(whole)))))
    rising = [s for s in ladder if s not in set(DEP.RETIRED)]
    check(list(DEP.LADDER) == rising,
          "and its rising order matches%s"
          % ("" if list(DEP.LADDER) == rising else
             "  <- has %s ; docs/24 rises %s" % (list(DEP.LADDER), rising)))

    meta = {}
    with io.open(os.path.join(ROOT, "tools", "check-metadata.py"),
                 encoding="utf-8") as fh:
        exec(compile(re.search(r'^STATUSES\s*=\s*\{[^}]*\}',
                               fh.read(), re.M | re.S).group(0),
                     "<check-metadata>", "exec"), meta)
    check(meta["STATUSES"] == set(ladder),
          "tools/check-metadata.STATUSES%s"
          % ("" if meta["STATUSES"] == set(ladder) else
             "  <- only there: %s ; only in docs/24: %s"
             % (sorted(meta["STATUSES"] - set(ladder)),
                sorted(set(ladder) - meta["STATUSES"]))))

    found = declarations(stages)
    print("\n%d declaration(s) found across %s - a fifth is caught the day "
          "it arrives" % (len(found), ", ".join(ROOTS)))

    # AN EMPTY SWEEP AGREES WITH EVERYTHING, which is the failure this
    # repository has been caught by before.
    check(len(found) >= 3,
          "enough to be sweeping something - a sweep that finds nothing "
          "passes perfectly and means the opposite")

    # A DECLARATION THAT IS A SUBSET MUST SAY WHICH STAGES IT LEAVES OUT,
    # AND WHY. This is the half that matters: OFFERABLE and QUALITY were BOTH
    # short of SHADOW and BOTH looked like deliberate subsets, because a
    # subset with no declared exclusions is indistinguishable from an
    # accident. Naming the exclusions makes the accident fail.
    EXCLUDES = {
        ("brain/heron_retrieve.py", "OFFERABLE"): (
            {"DEPRECATED", "ARCHIVED"},
            "they exist so a record is never destroyed (Golden Rule 4), not "
            "so they can be handed back as answers"),
        ("brain/heron_deployment.py", "LADDER"): (
            {"DEPRECATED", "ARCHIVED"},
            "retirement is not promotion - heron_deployment.RETIRED holds "
            "those two"),
        ("brain/heron_deployment.py", "RETIRED"): (
            set(ladder) - {"DEPRECATED", "ARCHIVED"},
            "the two stages that are reached deliberately rather than by "
            "climbing"),
    }
    for rel, line, name, named in found:
        why = NOT_THIS_LADDER.get((rel, name))
        if why:
            print("  --    %s:%d %s is a different ladder: %s"
                  % (rel, line, name, why))
            continue

        unknown = sorted(set(named) - set(ladder))
        if unknown:
            check(False, "%s:%d %s names %s, which docs/24 does not have"
                         % (rel, line, name, unknown))
            continue

        missing = set(ladder) - set(named)
        allowed, reason = EXCLUDES.get((rel, name), (set(), None))
        surprise = sorted(missing - allowed)
        check(not surprise,
              "%s:%d %s%s"
              % (rel, line, name,
                 "" if not surprise else
                 "  <- is short of %s. If that is deliberate, say so in "
                 "EXCLUDES here with the reason; a subset with no declared "
                 "exclusions cannot be told from an accident" % surprise))
        if reason and not surprise:
            print("        (leaves out %s: %s)"
                  % (", ".join(sorted(allowed)) if len(allowed) < 5
                     else "%d stages" % len(allowed), reason))

    print()
    if FAILURES:
        print("FAILED")
        for one in FAILURES:
            print("  - %s" % one)
        print("\n  docs/24 Axis 1 is the ladder. A module that disagrees is a")
        print("  defect, not an amendment - AGENTS.md: running code never")
        print("  silently rewrites policy. Fix the module, or record a")
        print("  decision changing the document first.")
        return 1
    print("PASSED - one ladder, read out of docs/24, and every declaration "
          "of it agrees.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
