# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-SKL-RES-001
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Skill research - what is already there, what would have to be built, and
which question nothing here can answer.

    python brain/heron_prebuild.py

WHY THE FILE IS NOT CALLED heron_skillresearch
-----------------------------------------------
It was, for one commit. `heron_skill` is HERON-SKL-VAL-004 and no module
name in brain/ may be a prefix of another - tests/test_references.py
enforces it, because HERON-NAM-REF-007 finds a name by searching for it
and a name that is a prefix of another finds both.

WHAT IT IS FOR (docs/28, HERON-SKL-RES-001)
--------------------------------------------
"Before building: does this skill already exist, what fragments would it
need, what does the standard say." T2, risk READ. Three questions, and
they do not have the same kind of answer - so they do not come back
looking the same.

1. DOES IT ALREADY EXIST - STRUCTURALLY YES, BY MEANING NO
------------------------------------------------------------
A skill IS what it needs. Two skills declaring the same capabilities in
the same domain are one job under two names, and that is a fact rather
than an opinion. So is an id already taken, and so is an utterance
already routed somewhere.

What is NOT decidable here is whether "select all ducts" and "pick every
duct" are the same sentence. D-34 is explicit that Heron builds nothing
to understand language, so there is no synonym table and there will not
be one. That question is the one scoped call that makes this row T2, and
under D-01 it belongs to the host.

What this agent hands the host is a SHORTLIST rather than the library:
the skills sharing at least one capability with the proposal. A skill
sharing no capability is not a near-duplicate in any sense that matters,
because a skill is what it needs.

2. WHAT FRAGMENTS WOULD IT NEED - AND SERVED IN 2024 IS NOT SERVED
--------------------------------------------------------------------
Each capability is looked up in the fragment library. A capability
nobody provides is the gap docs/18 wants visible.

A capability provided only on SOME of the releases the skill declares is
a gap wearing a provider's coat, and it is reported separately rather
than counted as served. This is HERON-SKL-PRF-006's lesson one step
earlier: a thing that works in one release and not another has a fine
overall figure and a broken release.

3. WHAT DOES THE STANDARD SAY - NOTHING HERE HOLDS A STANDARD
---------------------------------------------------------------
The whole Standards department is unbuilt. HERON-STD-ISO-003's rule is
"cites, never invents", and with nothing to cite the only honest answer
is that there is nothing to cite. So the question comes back NAMED and
UNANSWERED, with the agents that own it.

The register states one precedence and only one - HERON-STD-PRJ-009
"outranks the company default, and says so". Nothing here ranks the
rest, because the register does not.
"""

from __future__ import annotations

import importlib.util
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_fragment as FRAG  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# D-05's list, read from HERON-FRG-VAL-001 rather than retyped.
VERSIONS = FRAG.REVIT_VERSIONS

# Who owns question 3. None of them is built. The order is the ONE
# precedence docs/28 states - HERON-STD-PRJ-009 "outranks the company
# default, and says so" - and the rest are unranked because the register
# does not rank them.
STANDARD_OWNERS = (
    ("HERON-STD-PRJ-009", "this project's own rules - outranks the "
                          "company default, and says so"),
    ("HERON-STD-CMP-002", "the organisation's own approved standard"),
    ("HERON-STD-BIM-001", "a stated BIM standard"),
    ("HERON-STD-ISO-003", "ISO 19650 and related - cites, never invents"),
)


# WHICH OF THEM EXIST, DERIVED RATHER THAN ASSERTED. This used to be
# hard-coded as "none is built", and by 2026-09-15 three of the four
# were - a stale sentence routing the host away from working agents. The
# rule is agent-count.py's: an agent is built when a source file claims
# it in a `Heron-Agent:` header. Read here rather than imported, because
# importing a standards owner to find out whether it exists is a heavier
# thing than reading ten lines.
# Each owner and the module that implements it. The MAPPING is written
# here; whether the module EXISTS is not - that is looked up, so the
# answer follows the repository rather than a sentence somebody typed.
# Nothing is opened and nothing is imported: locating a module is enough
# to answer "is it built", and this agent reads no files at all.
OWNER_MODULES = {
    "HERON-STD-PRJ-009": "heron_project",
    "HERON-STD-CMP-002": "heron_company",
    "HERON-STD-BIM-001": "heron_bim",
    "HERON-STD-ISO-003": "heron_iso",
}

_BUILT = None


def _built_owners():
    """Which STANDARD_OWNERS exist as a module. Looked up once."""
    global _BUILT
    if _BUILT is not None:
        return _BUILT
    found = set()
    for who, _what in STANDARD_OWNERS:
        module = OWNER_MODULES.get(who)
        if not module:
            continue
        try:
            if importlib.util.find_spec(module) is not None:
                found.add(who)
        except (ImportError, ValueError):
            continue
    _BUILT = found
    return _BUILT


def _card(thing):
    """A plain mapping, whether a Skill object or a dict came in."""
    return getattr(thing, "data", thing)


def _said(line):
    """One utterance, flattened. Exact text only - never a meaning."""
    return " ".join(str(line or "").lower().split())


def research(proposal, skills=(), fragments=()):
    """
    {exists, needs, standard, shortlist, why, unjudged} - or a refusal.

    Nothing is written and nothing is decided. Three questions come back
    answered, shortlisted or named as unanswerable.
    """
    if not proposal:
        return {"researched": False, "refused": "NOTHING_TO_RESEARCH",
                "why": "no proposal was handed in. Researching nothing "
                       "returns 'it does not exist', which is true of "
                       "everything and useful about nothing."}

    proposal = _card(proposal)
    if not isinstance(proposal, dict):
        return {"researched": False, "refused": "NOT_A_PROPOSAL",
                "why": "%r is not a proposal. One is a skill card: "
                       "{id, name, domain, needs, utterances, revit}."
                       % (proposal,)}

    name = str(proposal.get("name") or proposal.get("id") or "").strip()
    if not name:
        return {"researched": False, "refused": "NOT_A_PROPOSAL",
                "why": "the proposal has neither a name nor an id. There "
                       "is nothing to report a finding against."}

    wants = [str(each).strip() for each in (proposal.get("needs") or [])
             if str(each).strip()]
    domain = str(proposal.get("domain") or "").strip().lower()
    releases = [str(each).strip() for each in (proposal.get("revit") or [])]

    strangers = sorted(set(one for one in releases if one not in VERSIONS))
    if strangers:
        return {"researched": False, "refused": "NOT_A_VERSION",
                "why": "the proposal declares %s. Known: %s - and D-05 "
                       "does not extrapolate, so a release off the end of "
                       "the list is refused rather than assumed to behave "
                       "like the one before it."
                       % (", ".join("'%s'" % each for each in strangers),
                          ", ".join(VERSIONS))}

    # ---- 1. DOES IT ALREADY EXIST ----------------------------------
    # Read ONCE. A generator handed in here would be empty by the time
    # the count at the bottom asked how many there had been.
    skills = list(skills or [])
    same_id, same_job, same_words, shortlist = [], [], [], []
    for entry in skills:
        card = _card(entry)
        if not isinstance(card, dict) or not (card.get("id")
                                              or card.get("name")):
            return {"researched": False, "refused": "NOT_A_SKILL",
                    "why": "%r is not a skill card. Comparing a proposal "
                           "against rubbish returns 'it does not exist', "
                           "which is the wrong answer arrived at "
                           "confidently." % (entry,)}
        theirs = [str(each).strip()
                  for each in (card.get("needs") or []) if str(each).strip()]
        where = str(card.get("domain") or "").strip().lower()
        who = str(card.get("id") or card.get("name")).strip()

        if proposal.get("id") and who == str(proposal.get("id")).strip():
            same_id.append(who)
        # A SKILL IS WHAT IT NEEDS. Same capabilities, same domain, one job.
        if wants and set(theirs) == set(wants) and where == domain:
            same_job.append(who)
        overlap = sorted(set(theirs) & set(wants))
        if overlap:
            shortlist.append({"skill": who, "shares": overlap})
        shared = sorted(set(_said(line) for line in
                            (card.get("utterances") or []))
                        & set(_said(line) for line in
                              (proposal.get("utterances") or [])))
        if shared:
            same_words.append({"skill": who, "utterances": shared})

    # ---- 2. WHAT FRAGMENTS WOULD IT NEED ----------------------------
    provided = {}
    for entry in fragments:
        card = _card(entry)
        if not isinstance(card, dict):
            continue
        capability = str(card.get("capability") or "").strip()
        if not capability:
            continue
        provided.setdefault(capability, []).append({
            "fragment": str(card.get("id") or "").strip(),
            "revit": [str(each) for each in (card.get("revit") or [])]})

    served, partly, gaps = [], [], []
    for capability in wants:
        providers = provided.get(capability) or []
        if not providers:
            gaps.append(capability)
            continue
        covered = set()
        for one in providers:
            covered |= set(one["revit"])
        missing = [one for one in releases if one not in covered]
        if missing:
            # A GAP WEARING A PROVIDER'S COAT. Counting this as served is
            # how a skill ships broken on half the releases it claims.
            partly.append({"capability": capability,
                           "by": [one["fragment"] for one in providers],
                           "not_on": missing,
                           "why": "%d fragment(s) provide %s, none of them "
                                  "on %s - and the proposal declares those "
                                  "releases. Served in one release is not "
                                  "served."
                                  % (len(providers), capability,
                                     ", ".join(missing))})
        else:
            served.append({"capability": capability,
                           "by": [one["fragment"] for one in providers]})

    return {
        "researched": True,
        "exists": {
            "id_taken": same_id, "same_job": same_job,
            "same_utterances": same_words,
            "why": "%s" % (
                "'%s' is already taken." % ", ".join(same_id) if same_id else
                "%s declares the same capabilities in the same domain, so it "
                "is this job under another name." % ", ".join(same_job)
                if same_job else
                "no skill declares the same capabilities in the same "
                "domain, and no id or utterance collides. Whether one MEANS "
                "the same is not answered here.")},
        "needs": {"served": served, "partly_served": partly, "gaps": gaps,
                  "why": "%d of %d capabilit%s already provided, %d partly, "
                         "%d unprovided%s."
                         % (len(served), len(wants),
                            "y is" if len(wants) == 1 else "ies are",
                            len(partly), len(gaps),
                            ": %s" % ", ".join(gaps) if gaps else "")},
        "standard": {
            "answered": False, "by": None,
            "owners": [{"agent": who, "holds": what,
                        "built": who in _built_owners()}
                       for who, what in STANDARD_OWNERS],
            "built": sorted(_built_owners()),
            "why": "nothing here holds a standard, and this agent asks "
                   "none of the %d that would. %s HERON-STD-ISO-003's "
                   "rule is 'cites, never invents' - nothing was cited "
                   "here because nothing was asked, which is a different "
                   "answer from there being nothing to ask."
                   % (len(STANDARD_OWNERS),
                      "None of them is built yet."
                      if not _built_owners() else
                      "%d of them %s built and can be asked directly: %s."
                      % (len(_built_owners()),
                         "is" if len(_built_owners()) == 1 else "are",
                         ", ".join(sorted(_built_owners()))))},
        "shortlist": shortlist, "of": len(skills),
        "why": "%s: %d existing skill(s) share a capability, %d capabilit%s "
               "unprovided, and the standards question is unanswered."
               % (name, len(shortlist), len(gaps),
                  "y is" if len(gaps) == 1 else "ies are"),
        "unjudged": [
            "WHETHER ANY OF THESE MEANS THE SAME IN A PERSON'S WORDS. D-34 "
            "is explicit that Heron builds nothing to understand language, "
            "so there is no synonym table here and there will not be one. "
            "That is the one scoped call that makes this row T2, and under "
            "D-01 it belongs to the host.",
            "%s" % ("THE HOST WAS HANDED A SHORTLIST OF %d, NOT THE "
                    "LIBRARY: %s. A skill sharing no capability is not a "
                    "near-duplicate in any sense that matters, because a "
                    "skill IS what it needs."
                    % (len(shortlist),
                       ", ".join(one["skill"] for one in shortlist))
                    if shortlist else
                    "NO EXISTING SKILL SHARES ONE CAPABILITY WITH THIS "
                    "PROPOSAL, so the shortlist handed to the host is "
                    "empty rather than the whole library."),
            "%s" % ("%d CAPABILIT%s PROVIDED ON SOME DECLARED RELEASES AND "
                    "NOT OTHERS: %s. Counted apart from served, never as "
                    "part of it - that is how a skill ships broken on half "
                    "the releases it claims."
                    % (len(partly), "Y IS" if len(partly) == 1 else "IES ARE",
                       ", ".join(one["capability"] for one in partly))
                    if partly else
                    "every provided capability is provided on every release "
                    "the proposal declares."),
            "WHAT THE STANDARD SAYS. The question is named and unanswered, "
            "never guessed - and unanswered HERE because this agent asks "
            "no standards owner, not because none exists. %s Only one "
            "precedence is reported because the register states only one: "
            "HERON-STD-PRJ-009 outranks the company default."
            % ("None of the %d owners is built yet."
               % len(STANDARD_OWNERS) if not _built_owners() else
               "%d of the %d are built (%s), so a caller that needs the "
               "standard has somewhere to go."
               % (len(_built_owners()), len(STANDARD_OWNERS),
                  ", ".join(sorted(_built_owners())))),
            "%s" % ("NOTHING WAS COMPARED BY CAPABILITY SET - the proposal "
                    "declares no needs, and an empty set equals every other "
                    "empty set. That is not a duplicate, so it was not "
                    "reported as one." if not wants else
                    "the capability-set comparison ran against %d declared "
                    "need(s)." % len(wants)),
        ],
    }


def main(argv):
    print("SKILL RESEARCH   what exists, what is missing, what nobody holds")
    print("=" * 72)

    library = [
        {"id": "count-elements", "domain": "revit.reporting",
         "needs": ["FILTER_ELEMENTS_BY_CATEGORY", "COUNT_ELEMENTS"],
         "utterances": ["how many ducts are there"]},
        {"id": "select-elements", "domain": "revit.selection",
         "needs": ["FILTER_ELEMENTS_BY_CATEGORY", "SELECT_ELEMENTS"],
         "utterances": ["select all the ducts"]},
    ]
    fragments = [
        {"id": "filter-by-category", "capability":
            "FILTER_ELEMENTS_BY_CATEGORY", "revit": list(VERSIONS)},
        {"id": "count-them", "capability": "COUNT_ELEMENTS",
         "revit": ["2024", "2025"]},
    ]

    answer = research(
        {"name": "tally the terminals", "domain": "revit.reporting",
         "needs": ["FILTER_ELEMENTS_BY_CATEGORY", "COUNT_ELEMENTS"],
         "utterances": ["how many ducts are there"],
         "revit": ["2023", "2024", "2025"]},
        skills=library, fragments=fragments)

    print("\n%s" % answer["why"])
    print("\n1. does it already exist")
    print("   %s" % answer["exists"]["why"])
    for one in answer["exists"]["same_utterances"]:
        print("   same words as %s: %s"
              % (one["skill"], ", ".join(one["utterances"])))
    print("\n2. what fragments would it need")
    print("   %s" % answer["needs"]["why"])
    for one in answer["needs"]["partly_served"]:
        print("   partly: %s" % one["why"])
    print("\n3. what does the standard say")
    print("   %s" % answer["standard"]["why"])
    for one in answer["standard"]["owners"]:
        print("   %-20s %s" % (one["agent"], one["holds"]))

    print("\nrefused")
    for proposal, skills in (
            (None, []), ("a string", []),
            ({"name": "x", "revit": ["2028"]}, []),
            ({"name": "x"}, ["not a card"])):
        bad = research(proposal, skills=skills)
        print("  %-22s %s" % (bad["refused"], bad["why"][:44]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
