# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-SKL-CRE-002
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Skill authoring - it writes the card, at DRAFT, and it does not make up
how anybody talks.

    python brain/heron_authoring.py

WHAT IT IS FOR (docs/28, HERON-SKL-CRE-002)
--------------------------------------------
"Authors a new skill - name, description, example utterances, required
fragments, preconditions, risk level." T3, risk MODIFY. It is the one
agent in this department that writes a file.

THE UTTERANCES ARE GIVEN, NEVER INVENTED
------------------------------------------
This is the whole argument with the row. "Example utterances" sounds
like something to generate, and generating them means deciding how
somebody asks for things - which is exactly what D-34 says Heron builds
nothing to do.

An invented utterance is worse than a missing one. A missing one leaves
a gap somebody fills the first time they ask; an invented one sits in
the card looking like evidence, and the skill gets routed by a sentence
nobody has ever said.

So: fewer than two real ones and the agent REFUSES and asks, which is
D-33 exactly - never assume an input, ask once. Two is heron_skill's own
bar, read from there rather than restated: "one is a name; two is the
beginning of knowing how somebody actually asks".

IT ENTERS AT DRAFT, AND THE AUTHOR DOES NOT GET A SAY
-------------------------------------------------------
The status is written by this agent, not copied from the draft. A draft
arriving with a status above DRAFT is refused rather than quietly
lowered - D-35, unapproved is refused and not warned about. docs/09's
lifecycle is a ladder and nothing may start half way up it.

A CARD MAY NOT DECLARE LESS RISK THAN IT NEEDS
------------------------------------------------
The same rule HERON-SKL-CMP-005 applies to a graph, applied to one card
against the fragments that would serve it. Declaring MODIFY work as READ
is not a small error: the card is the one place a person looks before
running the thing. Declaring higher is allowed and reported.

NOTHING IS OVERWRITTEN
------------------------
An id already in the library, or a file already at the path, is refused.
GR 14 - never silently discard. An authoring agent that overwrites is
an editing agent that lost its warning, and HERON-SKL-UPD-003 is the one
allowed to revise.

STEP AND VERSION COME OUT OF THIS FILE'S OWN HEADER
-----------------------------------------------------
A skill authored here came into existence at the step this agent runs
at, in the version this agent is. Both are read from the header above
rather than typed twice, so they cannot drift from it.
"""

from __future__ import annotations

import io
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import yaml  # noqa: E402

import heron_fragment as FRAG  # noqa: E402
import heron_skill as SKILL  # noqa: E402
import heron_secrets as SECRETS  # noqa: E402
import heron_capability as CAP  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

VERSIONS = FRAG.REVIT_VERSIONS
RISK = CAP.RISK_ORDER

# The one state a new skill may enter at. docs/09's ladder, read from
# HERON-FRG-VAL-001 rather than spelled here.
ENTERS_AT = FRAG.STATUSES[FRAG.STATUSES.index("DRAFT")]

# What the AUTHOR supplies: heron_skill's own required list, less the
# metadata this agent writes itself. Derived, so a field added to
# heron_skill.REQUIRED becomes required here without an edit.
WRITES = tuple(field for field in SKILL.REQUIRED if field.startswith("heron-"))
AUTHORS = tuple(field for field in SKILL.REQUIRED
                if not field.startswith("heron-"))


def _header(field):
    """A value out of this file's own metadata header."""
    for line in io.open(os.path.abspath(__file__),
                        encoding="utf-8").read().split("\n")[:12]:
        if line.startswith("# %s:" % field):
            return line.split(":", 1)[1].strip()
    raise ValueError("this file has no %s, which docs/29 requires" % field)


def _where(path):
    """A path a person can read - relative inside the repo, whole outside.

    os.path.relpath RAISES on Windows across drives rather than returning
    something useless, and this function is called while BUILDING AN ERROR
    MESSAGE - so the crash replaced the refusal it was trying to word. A draft
    written to a temp folder does exactly that: mkdtemp() is on C: and the
    repository is on D:. heron_fragment.repo_relative falls back to the
    absolute path, which is what "whole outside" already asked for.
    """
    near = FRAG.repo_relative(path)
    return path if near.startswith("..") else near


def _least(risk):
    """Where a risk sits, or None if it is not one."""
    risk = str(risk or "").strip().upper()
    return RISK.index(risk) if risk in RISK else None


def author(draft, skills=(), fragments=(), into=None):
    """
    Writes one skill card at DRAFT and returns {path, card} - or refuses.

    Nothing is overwritten and no utterance is invented.
    """
    if not draft:
        return {"authored": False, "refused": "NOTHING_TO_AUTHOR",
                "why": "no draft was handed in. An empty card is not a "
                       "skill nobody has written yet, it is nothing."}

    draft = getattr(draft, "data", draft)
    if not isinstance(draft, dict):
        return {"authored": False, "refused": "NOT_A_DRAFT",
                "why": "%r is not a draft. One is a skill card without its "
                       "metadata: %s." % (draft, ", ".join(AUTHORS))}

    said = draft.get("heron-status")
    if said and str(said).strip().upper() != ENTERS_AT:
        return {"authored": False, "refused": "NOT_A_DRAFT",
                "why": "the draft arrives at %s. A new skill enters the "
                       "lifecycle at %s and nowhere else - docs/09's ladder "
                       "starts at the bottom, and a status above it is "
                       "refused rather than quietly lowered."
                       % (said, ENTERS_AT)}

    missing = [field for field in AUTHORS
               if draft.get(field) in (None, "", [], {})]
    if missing:
        return {"authored": False, "refused": "INCOMPLETE",
                "why": "the draft is missing %s. Read from "
                       "heron_skill.REQUIRED rather than listed here, so a "
                       "field added there becomes required here with no "
                       "edit." % ", ".join(missing)}

    spoken = [line for line in (draft.get("utterances") or [])
              if str(line or "").strip()]
    if len(spoken) < 2:
        return {"authored": False, "refused": "TOO_FEW_UTTERANCES",
                "asked": "What else would you say to ask for this?",
                "why": "%d utterance(s) were given and two is the bar - "
                       "'one is a name; two is the beginning of knowing how "
                       "somebody actually asks', which is heron_skill's "
                       "wording, not this agent's. The second one is ASKED "
                       "FOR, never generated: D-34 means Heron builds "
                       "nothing to decide how a person talks, and an "
                       "invented utterance sits in the card looking like "
                       "evidence." % len(spoken)}

    wants = [str(each).strip() for each in (draft.get("needs") or [])
             if str(each).strip()]
    known = {}
    for entry in fragments:
        card = getattr(entry, "data", entry)
        if not isinstance(card, dict):
            continue
        known[str(card.get("id") or "").strip()] = card

    named = sorted(set(wants) & set(one for one in known if one))
    if named:
        return {"authored": False, "refused": "NAMES_A_FRAGMENT",
                "why": "`needs` lists %s, which %s fragment id%s, not a "
                       "capabilit%s. A skill that names a fragment cannot "
                       "survive that fragment being replaced, split or "
                       "retired - which is the whole reason the registry "
                       "exists."
                       % (", ".join("'%s'" % one for one in named),
                          "is a" if len(named) == 1 else "are",
                          "" if len(named) == 1 else "s",
                          "y" if len(named) == 1 else "ies")}

    releases = [str(each).strip() for each in (draft.get("revit") or [])]
    strangers = sorted(set(one for one in releases if one not in VERSIONS))
    if strangers:
        return {"authored": False, "refused": "NOT_A_VERSION",
                "why": "the draft declares %s. Known: %s - and D-05 does "
                       "not extrapolate."
                       % (", ".join("'%s'" % one for one in strangers),
                          ", ".join(VERSIONS))}

    mine = _least(draft.get("risk"))
    if mine is None:
        return {"authored": False, "refused": "NOT_A_RISK",
                "why": "risk %r is not one of %s - imported from "
                       "HERON-KRN-CAP-008 rather than listed here."
                       % (draft.get("risk"), ", ".join(RISK))}

    # WHAT THE WORK ACTUALLY COSTS, from the fragments that would serve
    # it. Tracked SEPARATELY from what the card declares - seeding it
    # with the declaration would make the answer unable to come back
    # lower, and "declares more than it needs" is the whole cautious case.
    serving, through = None, None
    for card in known.values():
        if str(card.get("capability") or "").strip() not in wants:
            continue
        theirs = _least(card.get("risk"))
        if theirs is not None and (serving is None or theirs > serving):
            serving, through = theirs, card.get("capability")
    if serving is not None and serving > mine:
        return {"authored": False, "refused": "RISK_IS_UNDERSTATED",
                "declared": RISK[mine], "needed": RISK[serving],
                "through": through,
                "why": "the card declares %s and needs %s through %s. The "
                       "card is the one place a person looks before running "
                       "the thing, so it is refused rather than raised "
                       "quietly." % (RISK[mine], RISK[serving], through)}

    where = into or SKILL.SKILLS_DIR
    who = str(draft.get("id")).strip()
    path = os.path.join(where, "%s.yaml" % who)

    # AN ID IS A NAME, NOT A PATH. `id: "../agents/ESCAPED"` joined
    # cleanly and wrote an agent-shaped YAML beside the skill library;
    # an absolute id could land under any directory that exists. Nothing
    # restricted the syntax - not here and not in HERON-SKL-VAL-004 - and
    # the drafts this agent writes are MODEL-GENERATED, which is exactly
    # the input a path has to be checked against rather than trusted.
    #
    # The comparison is HERON-KRN-SEC-012's `_inside`, bound rather than
    # rewritten: it was written for this class of question and it already
    # handles the three things a startswith() gets wrong - case, shape
    # and the boundary between "Heron-AI" and "Heron-AI-notes".
    #
    # Found by a review on 2026-09-15.
    # A SEPARATOR MAKES IT NOT A NAME, wherever it points. `sub/dir/X`
    # stays inside the library and still is not an id - it asks for a
    # folder nobody made, and the write dies with FileNotFoundError
    # halfway through authoring. The boundary check below stays as the
    # backstop for `..` and for an absolute path.
    # BOTH SEPARATORS, ON EITHER PLATFORM. `..\windows` is one filename
    # on Linux and an escape on Windows - and Revit is Windows-only, so
    # the machine that matters is the one where it escapes. Checking only
    # os.sep means this passes here and fails where it counts.
    if (who != os.path.basename(who) or os.path.isabs(who) or not who
            or "/" in who or "\\" in who or who in (".", "..")):
        return {"authored": False, "refused": "ID_IS_NOT_A_NAME",
                "why": "'%s' is not a skill id. An id names one skill and "
                       "is one word - it is not a path, and one carrying a "
                       "separator either writes a file somewhere nobody is "
                       "looking for it or asks for a folder that does not "
                       "exist." % who}

    if not SECRETS._inside(path, where):
        return {"authored": False, "refused": "ID_IS_NOT_A_NAME",
                "why": "'%s' resolves to %s, which is outside %s. A skill "
                       "id names a skill; it is not a path, and one "
                       "carrying a separator or climbing with `..` writes "
                       "a file somewhere nobody is looking for it."
                       % (who, os.path.abspath(path), os.path.abspath(where))}
    taken = sorted(one for one in
                   (str(getattr(entry, "data", entry).get("id") or "").strip()
                    for entry in skills) if one == who)
    if taken or os.path.exists(path):
        return {"authored": False, "refused": "ALREADY_EXISTS",
                "why": "'%s' is %s. Nothing is overwritten here - GR 14 - "
                       "and HERON-SKL-UPD-003 is the agent allowed to "
                       "revise an existing skill."
                       % (who, "already in the library" if taken else
                          "already a file at %s" % _where(path))}

    card = dict((field, draft[field]) for field in AUTHORS)
    card["heron-status"] = ENTERS_AT
    card["heron-step"] = int(_header("Heron-Step"))
    card["heron-since"] = _header("Heron-Since")
    card["heron-layer"] = "brain"
    for field in draft:
        if field not in card and not field.startswith("heron-"):
            card[field] = draft[field]

    if not os.path.isdir(where):
        os.makedirs(where)
    io.open(path, "w", encoding="utf-8").write(_render(card))

    # NOT KNOWN is not the same as EQUAL. With no fragment serving any
    # of these capabilities, nothing here knows what the work costs, and
    # saying the declaration "equals" it would be inventing the number.
    cautious = serving is not None and serving < mine
    return {
        "authored": True, "path": path, "card": card,
        "status": ENTERS_AT, "declared": RISK[mine],
        "needs_at_least": RISK[serving] if serving is not None else None,
        "cautious": cautious,
        "why": "'%s' written at %s with %d utterance(s), all of them "
               "given. %s"
               % (who, ENTERS_AT, len(spoken),
                  "It declares %s and needs %s - allowed and reported."
                  % (RISK[mine], RISK[serving]) if cautious else
                  "It declares %s." % RISK[mine]),
        "unjudged": [
            "NOT ONE UTTERANCE WAS INVENTED. All %d came in with the draft. "
            "D-34 means Heron builds nothing to decide how a person talks, "
            "and an invented utterance sits in the card looking like "
            "evidence." % len(spoken),
            "IT ENTERS AT %s AND THE AUTHOR GOT NO SAY. docs/09's ladder "
            "starts at the bottom; a draft arriving higher is refused, not "
            "quietly lowered." % ENTERS_AT,
            "WHETHER THE SKILL IS ANY GOOD. Nothing here ran it. "
            "HERON-SKL-VAL-004 validates the card and D-30 wants one "
            "recorded proof against a real model before it moves.",
            "%s" % ("IT DECLARES %s AND NEEDS ONLY %s. Allowed and "
                    "reported - an author being cautious about their own "
                    "skill is not an error, and refusing it would teach "
                    "people to declare the minimum."
                    % (RISK[mine], RISK[serving]) if cautious else
                    "the declared risk equals what the fragments serving "
                    "it carry." if serving is not None else
                    "NO FRAGMENT HANDED IN SERVES ANY OF THESE "
                    "CAPABILITIES, so nothing here knows what the work "
                    "costs. The declared risk was neither confirmed nor "
                    "contradicted - which is not the same as agreeing "
                    "with it."),
            "THE STEP AND VERSION CAME OUT OF THIS AGENT'S OWN HEADER "
            "(%s, %s), not a second copy that could drift from it."
            % (card["heron-step"], card["heron-since"]),
        ],
    }


def _render(card):
    """
    The card as YAML, in the card's own order.

    Dumped rather than hand-written. A hand-written line breaks on the
    first purpose containing a colon, and writes `revit: 2024` as an
    INTEGER - which reads back as 2024, not "2024", and every release
    comparison in this project is against strings.
    """
    def block(fields):
        return yaml.safe_dump(
            dict((field, card[field]) for field in fields),
            default_flow_style=False, sort_keys=False, allow_unicode=True)

    rest = [field for field in card if not field.startswith("heron-")]
    return "%s\n%s" % (block(WRITES), block(rest))


def main(argv):
    import shutil
    import tempfile

    print("SKILL AUTHORING   at DRAFT, and nobody's words are invented")
    print("=" * 72)
    print("\nthe author supplies %d field(s), this agent writes %d"
          % (len(AUTHORS), len(WRITES)))
    print("  author:  %s" % ", ".join(AUTHORS))
    print("  agent:   %s" % ", ".join(WRITES))

    fragments = [{"id": "count-them", "capability": "COUNT_ELEMENTS",
                  "risk": "READ"},
                 {"id": "move-them", "capability": "MOVE_ELEMENTS",
                  "risk": "MODIFY"}]
    draft = {"id": "tally-terminals", "name": "How many terminals",
             "domain": "revit.reporting",
             "purpose": "Counts air terminals and says which filter it used.",
             "utterances": ["how many air terminals",
                            "count the terminals on level 2"],
             "needs": ["COUNT_ELEMENTS"],
             "preconditions": ["a document is open"],
             "risk": "READ", "revit": ["2024", "2025"]}

    where = tempfile.mkdtemp()
    try:
        good = author(draft, fragments=fragments, into=where)
        print("\n%s" % good["why"])
        print("\n%s" % io.open(good["path"], encoding="utf-8").read())

        print("refused")
        for bad, label in (
                (author(None, into=where), None),
                (author("x", into=where), None),
                (author(dict(draft, **{"heron-status": "PRODUCTION"}),
                        into=where), None),
                (author({"id": "x"}, into=where), None),
                (author(dict(draft, id="other", utterances=["just one"]),
                        into=where), None),
                (author(dict(draft, id="other", needs=["count-them"]),
                        fragments=fragments, into=where), None),
                (author(dict(draft, id="other", revit=["2028"]),
                        into=where), None),
                (author(dict(draft, id="other", risk="DELETE"),
                        into=where), None),
                (author(dict(draft, id="other", needs=["MOVE_ELEMENTS"]),
                        fragments=fragments, into=where), None),
                (author(draft, fragments=fragments, into=where), None)):
            print("  %-22s %s" % (bad["refused"], bad["why"][:44]))

        print("\nwhat this agent does not judge")
        for line in good["unjudged"]:
            print("  - %s" % line)
    finally:
        shutil.rmtree(where)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
