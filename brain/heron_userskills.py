# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-USR-SKL-003
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
User skills - personal stays personal until explicitly raised.

    python brain/heron_userskills.py

WHAT IT IS FOR (docs/28, HERON-USR-SKL-003)
--------------------------------------------
"Keeps this user's own learned skills separate from shared ones, and
manages promotion upward. PERSONAL STAYS PERSONAL UNTIL EXPLICITLY
RAISED." T1, risk MODIFY. Nothing is moved: a plan comes back.

THE LADDER IS THE SCOPE MEANINGS, NOT A LIST INVENTED HERE
------------------------------------------------------------
HERON-RAG-LIB-001 says what each scope reaches, and the ladder falls
out of it:

    user      "working preferences and personal patterns, SHARED WITH
              NOBODY"
    company   "company standards and approved knowledge"
    global    "general Heron knowledge, SHARED WITH EVERYONE"

So upward is user -> company -> global, and that is the whole ladder.

PROJECT IS SIDEWAYS, AND ITS OWN MEANING SAYS SO
--------------------------------------------------
    project   "one project only - NEVER SHARED SIDEWAYS"

A personal skill raised into a project is not promoted, it is handed to
one client; a project skill raised out is the breach PROPOSALS F14 is
about. Both are refused here rather than being a special case somebody
has to remember.

ONE STEP AT A TIME
--------------------
user -> global in one move is not a fast promotion, it is a skipped
review: the company step is where somebody who is not the author looks
at it before everyone gets it. Refused as SKIPS_A_STEP.

THE AUTHOR MAY NOT APPROVE THEIR OWN RAISE
--------------------------------------------
Golden Rule 7 says "no agent approves itself" and is written about
agents - "the builder is not the tester, the tester is not the
deployer". This applies the same shape to a person, and that IS an
extension rather than a quotation. It is made deliberately and said out
loud in every answer, because the moment a skill leaves `user` is the
moment it starts being trusted by somebody who never chose it.

docs/09 s94's gate table asks for "Human approval. Explicit, recorded,
one person" at the equivalent fragment step and does not say the person
may not be the author. So this is stricter than what is written, and a
reader deserves to know which.

SEPARATE MEANS SEPARATE
-------------------------
A skill in `user` scope belongs to one person. Asked for somebody
else's, this refuses rather than returning it with a note - "shared with
nobody" has no reading in which a note makes it shareable.
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_scope as SCOPE  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# Read off HERON-RAG-LIB-001's own meanings: shared with nobody, then the
# company's, then everyone's.
LADDER = (SCOPE.USER, SCOPE.COMPANY, SCOPE.GLOBAL)

# Its own meaning says "never shared sideways", so it is not a rung.
SIDEWAYS = SCOPE.PROJECT

# What a raise must carry to be one.
A_RAISE_CARRIES = (
    ("skill", "which skill is being raised"),
    ("to", "the rung it would reach"),
    ("by", "who asked - a raise is EXPLICIT or it has not happened"),
    ("approved_by", "who agreed, and not the author"),
)


def _rung(scope):
    """Where a scope sits on the ladder, or None if it is not on it."""
    scope = str(scope or "").strip().lower()
    return LADDER.index(scope) if scope in LADDER else None


def manage(skills, raising=None, reader=None):
    """
    {mine, raise_, refused_names, why, unjudged} - or a refusal.

    Nothing is moved. `reader` is who is asking, so a personal skill
    belonging to somebody else is refused rather than returned.
    """
    if not skills:
        return {"raised": False, "refused": "NOTHING_TO_MANAGE",
                "why": "no skills were handed in. An empty answer saying "
                       "this user has learned nothing is a statement about "
                       "the call."}

    who = str(reader or "").strip().lower()
    mine, held, refused = [], {}, []
    for skill in skills:
        if not isinstance(skill, dict):
            refused.append({"skill": repr(skill)[:50],
                            "refused": "NOT_A_SKILL",
                            "why": "each skill is {name, scope, author}."})
            continue
        name = str(skill.get("name") or "").strip()
        scope = str(skill.get("scope") or "").strip().lower()
        author = str(skill.get("author") or "").strip()
        if not name or not scope or not author:
            refused.append({"skill": name or None,
                            "refused": "NOT_A_SKILL",
                            "why": "carries no %s. A skill with no author "
                                   "cannot be raised by anybody but its "
                                   "author, because nobody knows who that "
                                   "is."
                                   % ("name" if not name else
                                      "scope" if not scope else "author")})
            continue
        if scope not in SCOPE.SCOPES:
            refused.append({"skill": name, "refused": "NOT_A_SKILL",
                            "why": "'%s' is not a scope. Known: %s - read "
                                   "from HERON-RAG-LIB-001."
                                   % (scope, ", ".join(SCOPE.SCOPES))})
            continue

        held[name] = {"name": name, "scope": scope, "author": author}
        if scope != SCOPE.USER:
            continue
        # NOBODY ASKING IS NOT EVERYBODY ASKING. With no `reader` the test
        # below was skipped entirely, so a call that said nothing about
        # who was asking came back holding EVERY author's personal skills
        # - the one answer the scope HERON-RAG-LIB-001 calls "shared with
        # nobody" exists to prevent. An unnamed reader is refused rather
        # than treated as a matching one.
        if not who:
            refused.append({"skill": name, "refused": "NOBODY_IS_ASKING",
                            "why": "'%s' is %s's, in the scope "
                                   "HERON-RAG-LIB-001 calls 'shared with "
                                   "nobody', and this call names no "
                                   "reader. Nothing here can show the "
                                   "asker is %s, and an unnamed reader "
                                   "matching everybody is the widest "
                                   "possible reading of 'nobody'."
                                   % (name, author, author)})
            continue
        # SEPARATE MEANS SEPARATE.
        if author.lower() != who:
            refused.append({"skill": name, "refused": "NOT_YOURS",
                            "why": "'%s' is %s's, in the scope "
                                   "HERON-RAG-LIB-001 calls 'shared with "
                                   "nobody'. Refused rather than returned "
                                   "with a note - there is no reading of "
                                   "'nobody' in which a note makes it "
                                   "shareable." % (name, author)})
            continue
        mine.append(held[name])

    planned = []
    for asked in (raising or []):
        if not isinstance(asked, dict):
            refused.append({"skill": repr(asked)[:50],
                            "refused": "NOT_A_RAISE",
                            "why": "each raise is {skill, to, by, "
                                   "approved_by}."})
            continue
        name = str(asked.get("skill") or "").strip()
        missing = [field for field, _why in A_RAISE_CARRIES
                   if not str(asked.get(field) or "").strip()]
        if missing:
            refused.append({"skill": name or None, "refused": "NOT_A_RAISE",
                            "missing": missing,
                            "why": "carries no %s. %s"
                                   % (", ".join("`%s`" % each
                                                for each in missing),
                                      " ".join(why for field, why
                                               in A_RAISE_CARRIES
                                               if field in missing))})
            continue
        if name not in held:
            refused.append({"skill": name, "refused": "NOT_A_RAISE",
                            "why": "'%s' is not one of the %d skills handed "
                                   "in." % (name, len(held))})
            continue

        skill = held[name]
        to = str(asked["to"]).strip().lower()
        from_rung, to_rung = _rung(skill["scope"]), _rung(to)

        if skill["scope"] == SIDEWAYS or to == SIDEWAYS:
            refused.append({"skill": name, "refused": "SIDEWAYS_IS_NOT_UP",
                            "why": "'%s' is the scope HERON-RAG-LIB-001 "
                                   "calls 'one project only - never shared "
                                   "sideways'. A personal skill raised into "
                                   "a project is handed to one client; a "
                                   "project skill raised out is the breach "
                                   "PROPOSALS F14 is about."
                                   % SIDEWAYS})
            continue
        if from_rung is None or to_rung is None:
            refused.append({"skill": name, "refused": "NOT_A_RAISE",
                            "why": "the ladder is %s, and '%s' is not on it."
                                   % (" -> ".join(LADDER),
                                      skill["scope"] if from_rung is None
                                      else to)})
            continue
        if to_rung <= from_rung:
            refused.append({"skill": name, "refused": "NOT_A_RAISE",
                            "why": "'%s' is already at '%s' and this would "
                                   "%s. This agent manages promotion "
                                   "UPWARD; going the other way is a copy "
                                   "somebody else owns."
                                   % (name, skill["scope"],
                                      "leave it there" if to_rung == from_rung
                                      else "lower it to '%s'" % to)})
            continue
        if to_rung - from_rung > 1:
            refused.append({"skill": name, "refused": "SKIPS_A_STEP",
                            "why": "'%s' to '%s' skips '%s'. That is not a "
                                   "fast promotion, it is a skipped review: "
                                   "the company rung is where somebody who "
                                   "is not the author looks at it before "
                                   "everyone gets it."
                                   % (skill["scope"], to,
                                      LADDER[from_rung + 1])})
            continue

        approver = str(asked["approved_by"]).strip()
        if approver.lower() == skill["author"].lower():
            refused.append({"skill": name,
                            "refused": "THE_AUTHOR_MAY_NOT_APPROVE",
                            "why": "%s wrote '%s' and would also be "
                                   "approving it out of their own scope. "
                                   "Golden Rule 7 - no agent approves "
                                   "itself, the builder is not the tester - "
                                   "applied to a person. That IS an "
                                   "extension rather than a quotation, and "
                                   "it is made deliberately: this is the "
                                   "moment a skill starts being trusted by "
                                   "somebody who never chose it."
                                   % (approver, name)})
            continue

        planned.append({"skill": name, "from": skill["scope"], "to": to,
                        "by": str(asked["by"]).strip(),
                        "approved_by": approver, "author": skill["author"],
                        "why": "one rung, %s -> %s, asked by %s and approved "
                               "by %s, who did not write it."
                               % (skill["scope"], to, asked["by"], approver)})

    return {
        "raised": False, "mine": mine, "raise_": planned,
        "refused_names": refused, "of": len(skills),
        "ladder": list(LADDER),
        "why": "%d skill(s), %d personal to %s, %d raise(s) planned, %d "
               "refused. Nothing was moved."
               % (len(skills), len(mine), reader or "nobody named",
                  len(planned), len(refused)),
        "unjudged": [
            "NOTHING WAS MOVED. A plan comes back and a caller carries it "
            "out.",
            "PERSONAL STAYED PERSONAL. Nothing is raised without an "
            "explicit raise naming who asked and who approved - a skill "
            "does not drift upward because it was useful.",
            "THE AUTHOR-MAY-NOT-APPROVE RULE IS AN EXTENSION, NOT A "
            "QUOTATION. Golden Rule 7 is written about agents, and docs/09 "
            "s94 asks for 'human approval, explicit, recorded, one person' "
            "without saying the person may not be the author. This is "
            "stricter than what is written and a reader deserves to know "
            "which.",
            "WHETHER A SKILL IS GOOD ENOUGH TO RAISE IS NOT JUDGED HERE. "
            "What was checked is that the raise is one rung, upward, not "
            "sideways, and approved by somebody who did not write it.",
        ],
    }


def main(argv):
    print("USER SKILLS   personal stays personal until explicitly raised")
    print("=" * 72)

    print("\nthe ladder, off HERON-RAG-LIB-001's own meanings")
    for rung in LADDER:
        print("  %-10s %s" % (rung, SCOPE.SCOPE_MEANING[rung]))
    print("  %-10s %s  (not a rung)"
          % (SIDEWAYS, SCOPE.SCOPE_MEANING[SIDEWAYS]))

    skills = [
        {"name": "tag-my-way", "scope": "user", "author": "Ajmal"},
        {"name": "office-sheets", "scope": "company", "author": "Ajmal"},
        {"name": "someone-elses", "scope": "user", "author": "Sara"},
        {"name": "tower-a-only", "scope": "project", "author": "Ajmal"},
    ]

    answer = manage(skills, reader="Ajmal", raising=[
        {"skill": "tag-my-way", "to": "company", "by": "Ajmal",
         "approved_by": "Sara"},
        {"skill": "tag-my-way", "to": "global", "by": "Ajmal",
         "approved_by": "Sara"},
        {"skill": "office-sheets", "to": "global", "by": "Ajmal",
         "approved_by": "Ajmal"},
        {"skill": "tower-a-only", "to": "company", "by": "Ajmal",
         "approved_by": "Sara"},
        {"skill": "tag-my-way", "to": "company", "by": "Ajmal"}])

    print("\n%s" % answer["why"])
    for row in answer["mine"]:
        print("  mine      %-16s by %s" % (row["name"], row["author"]))
    for row in answer["raise_"]:
        print("  RAISE     %-16s %s" % (row["skill"], row["why"][:52]))
    for row in answer["refused_names"]:
        print("  refused   %-16s %s" % (row.get("skill") or "?",
                                        row["refused"]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
