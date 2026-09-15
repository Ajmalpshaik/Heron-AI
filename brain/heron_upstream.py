# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-GIT-CHG-009
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Change detection - which fragments a change touched, and which skills
that reaches.

    python brain/heron_upstream.py

WHAT IT IS FOR (docs/28, HERON-GIT-CHG-009)
--------------------------------------------
"Detects upstream changes affecting fragments or skills." T1, risk READ.
It is handed the paths a change touched and answers what is downstream
of them.

THE SECOND STEP IS THE ONE NOBODY DOES BY HAND
------------------------------------------------
Seeing that `brain/fragments/count-them/impl` changed is easy: the diff
says so. Seeing that four skills route through the capability that
fragment provides is not, and that is the step this exists for.

A skill is affected because something it NEEDS changed, not because its
own file did. So the answer separates the two - `skills_changed` are the
cards somebody edited, `skills_reached` are the ones nobody touched and
everybody forgot.

EVERY CAPABILITY HAS EXACTLY ONE PROVIDER, WHICH MAKES THIS SHARPER
---------------------------------------------------------------------
Measured on 2026-09-15: 360 fragments, 360 capabilities, and not one of
them has a second provider. So a fragment is not one of several ways to
do a thing - it is the only way, and every skill needing its capability
stops when it does. Each reached skill says which capability carried the
change to it and whether anything else provides it.

A PATH IT CANNOT ATTRIBUTE IS REPORTED, NEVER DROPPED
-------------------------------------------------------
Golden Rule 14. A change detector that silently ignores what it does not
recognise reports "nothing downstream" for a change it did not
understand, which is the same sentence as "nothing downstream" for a
change that genuinely has none. Those are different facts.

IT READS THE LIBRARY, IT DOES NOT READ GIT
--------------------------------------------
The paths come in. Nothing here runs a command, opens a socket or
decides what "upstream" means - HERON-GIT-MAIN-001 owns repository
interaction and docs/28 gives it PUBLISH for a reason.
"""

from __future__ import annotations

import os
import posixpath
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_fragment as FRAG  # noqa: E402
import heron_skill as SKILL  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

FRAGMENTS = "brain/fragments"
SKILLS = "brain/skills"


def _tidy(path):
    """One repository-relative path, in the shape git prints them."""
    return posixpath.normpath(str(path).strip().replace("\\", "/"))


def _card(thing):
    return getattr(thing, "data", thing)


def look(paths, fragments=None, skills=None):
    """
    {fragments, skills_changed, skills_reached, unattributed} - or a
    refusal. Nothing is written and no command is run.
    """
    if not paths:
        return {"looked": False, "refused": "NOTHING_TO_CHECK",
                "why": "no paths were handed in. 'Nothing is downstream' "
                       "is the answer to a change that touched nothing, "
                       "and it must not also be the answer to not being "
                       "asked."}

    seen = []
    for path in paths:
        if not isinstance(path, str) or not path.strip():
            return {"looked": False, "refused": "NOT_A_PATH",
                    "why": "%r is not a path. Each is one "
                           "repository-relative path, in the shape git "
                           "prints them." % (path,)}
        tidy = _tidy(path)
        if os.path.isabs(tidy) or tidy.startswith(".."):
            return {"looked": False, "refused": "OUTSIDE_THE_REPOSITORY",
                    "why": "'%s' points outside the repository. Nothing "
                           "here can say what is downstream of a file this "
                           "project does not contain, and answering "
                           "'nothing' would be a statement about the path "
                           "rather than about the change." % tidy}
        seen.append(tidy)

    if fragments is None:
        found, _ = FRAG.load_all()
        fragments = list(found.values())
    if skills is None:
        found, _ = SKILL.load_all()
        skills = list(found.values())

    # capability -> the fragments that provide it. Every one of them has
    # exactly one today, which is what makes a single change reach so far.
    provided = {}
    by_slug = {}
    for entry in fragments:
        card = _card(entry)
        if not isinstance(card, dict):
            continue
        # THE FOLDER NAME, not the id. A path says
        # brain/fragments/count-elements/..., and `count-elements` is
        # the folder - heron_fragment exposes it as a property off the
        # object, so it is not in `.data` and cannot be read from there.
        slug = str(getattr(entry, "slug", None) or card.get("slug")
                   or "").strip()
        capability = str(card.get("capability") or "").strip()
        if slug:
            by_slug[slug] = capability
        if capability:
            provided.setdefault(capability, []).append(
                str(card.get("id") or slug))

    known_skills = {}
    for entry in skills:
        card = _card(entry)
        if not isinstance(card, dict):
            continue
        who = str(card.get("id") or "").strip()
        if who:
            known_skills[who] = [str(one).strip()
                                 for one in (card.get("needs") or [])
                                 if str(one).strip()]

    touched, changed_cards, leftover = {}, {}, []
    for path in seen:
        if path.startswith(FRAGMENTS + "/"):
            slug = path[len(FRAGMENTS) + 1:].split("/")[0]
            if slug in by_slug:
                touched.setdefault(slug, []).append(path)
            else:
                leftover.append({"path": path,
                                 "why": "under %s but '%s' is not a "
                                        "fragment in the library"
                                        % (FRAGMENTS, slug)})
        elif path.startswith(SKILLS + "/"):
            who = posixpath.basename(path).rsplit(".", 1)[0]
            if who in known_skills:
                changed_cards.setdefault(who, []).append(path)
            else:
                leftover.append({"path": path,
                                 "why": "under %s but '%s' is not a skill "
                                        "in the library" % (SKILLS, who)})
        else:
            # REPORTED, NEVER DROPPED. "Nothing downstream" must not be
            # the answer to a change nothing here understood.
            leftover.append({"path": path,
                             "why": "not under %s or %s, so nothing here "
                                    "knows what depends on it"
                                    % (FRAGMENTS, SKILLS)})

    moved = sorted(by_slug[slug] for slug in touched if by_slug[slug])
    reached = []
    for who in sorted(known_skills):
        carried = sorted(set(known_skills[who]) & set(moved))
        if not carried or who in changed_cards:
            continue
        reached.append({
            "skill": who, "through": carried,
            "only_provider": [one for one in carried
                              if len(provided.get(one, [])) == 1],
            "why": "nobody edited %s. It routes through %s, and %s"
                   % (who, ", ".join(carried),
                      "no other fragment provides %s"
                      % " or ".join(one for one in carried
                                    if len(provided.get(one, [])) == 1)
                      if any(len(provided.get(one, [])) == 1
                             for one in carried) else
                      "something else provides each of them")})

    return {
        "looked": True, "of": len(seen),
        "fragments": [{"slug": slug, "capability": by_slug[slug],
                       "paths": sorted(touched[slug])}
                      for slug in sorted(touched)],
        "capabilities": moved,
        "skills_changed": sorted(changed_cards),
        "skills_reached": reached,
        "unattributed": leftover,
        "why": "%d path(s): %d fragment(s) touched carrying %d "
               "capabilit%s, %d skill card(s) edited, %d skill(s) reached "
               "without being edited, %d path(s) nothing here could "
               "attribute."
               % (len(seen), len(touched), len(moved),
                  "y" if len(moved) == 1 else "ies", len(changed_cards),
                  len(reached), len(leftover)),
        "unjudged": [
            "%s" % ("%d SKILL(S) NOBODY EDITED ARE DOWNSTREAM OF THIS "
                    "CHANGE: %s. That is the step this agent exists for - "
                    "the diff shows the fragment, it does not show the "
                    "skills routing through the capability."
                    % (len(reached),
                       ", ".join(one["skill"] for one in reached))
                    if reached else
                    "no skill is reached through a capability here. Either "
                    "nothing downstream depends on what moved, or nothing "
                    "that moved was a fragment."),
            "%s" % ("%d PATH(S) COULD NOT BE ATTRIBUTED AND ARE LISTED "
                    "RATHER THAN DROPPED (GR 14): %s. 'Nothing downstream' "
                    "must not be the answer to a change nothing here "
                    "understood."
                    % (len(leftover),
                       ", ".join(one["path"] for one in leftover))
                    if leftover else
                    "every path handed in was attributed to a fragment or "
                    "a skill."),
            "WHETHER ANY OF IT STILL WORKS. Nothing here ran a test, a "
            "fragment or a skill. It reports what a change can reach, "
            "which is not the same as what it broke.",
            "WHAT 'UPSTREAM' MEANS. The paths came in; nothing here ran a "
            "command, opened a socket or read git. HERON-GIT-MAIN-001 owns "
            "repository interaction and docs/28 gives it PUBLISH.",
        ],
    }


def main(argv):
    print("CHANGE DETECTION   what a change touched, and what it reaches")
    print("=" * 72)

    answer = look(["brain/fragments/count-elements/impl/count.cs",
                   "brain/fragments/count-elements/fragment.yaml",
                   "brain/fragments/filter-elements-by-category/impl.cs",
                   "brain/skills/size-breakdown.yaml",
                   "docs/09-skills-and-fragments.md",
                   "brain/fragments/no-such-thing/impl.cs"])

    print("\n%s" % answer["why"])
    print("\nfragments touched")
    for one in answer["fragments"]:
        print("  %-22s %s" % (one["slug"], one["capability"]))
    print("\nskill cards edited")
    for who in answer["skills_changed"]:
        print("  %s" % who)
    print("\nskills reached that nobody edited")
    for one in answer["skills_reached"]:
        print("  %-22s %s" % (one["skill"], one["why"]))
    print("\nnot attributed")
    for one in answer["unattributed"]:
        print("  %-42s %s" % (one["path"], one["why"]))

    print("\nrefused")
    for paths in ([], [None], ["/etc/passwd"], ["../elsewhere/x"]):
        bad = look(paths)
        print("  %-24s %s" % (bad["refused"], bad["why"][:42]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
