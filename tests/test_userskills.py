# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-USR-SKL-003
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
User skills - personal stays personal until explicitly raised.

    python tests/test_userskills.py

WHAT IT PROVES
  1. THE LADDER COMES OFF HERON-RAG-LIB-001's OWN MEANINGS, and project
     is not on it because its meaning says so.

  2. PERSONAL STAYS PERSONAL. Nothing moves without an explicit raise,
     and a raise missing any of its four fields is not one.

  3. ONE STEP AT A TIME - user to global is refused, and the same skill
     gets there in two moves.

  4. THE AUTHOR MAY NOT APPROVE, and the answer says plainly that this
     is stricter than what is written.

  5. SEPARATE MEANS SEPARATE - somebody else's personal skill is not in
     `mine`, and the refusal returns no more than the caller handed in.

  6. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_userskills as SKL                                 # noqa: E402
import heron_scope as SCOPE                                    # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_userskills.py"),
                    encoding="utf-8").read()
    code = whole.split("\nfrom __future__", 1)[1]
    logic = code.split("\ndef main(")[0]

    def ask(skills, **kw):
        answer = SKL.manage(skills, **kw)
        if answer.get("refused"):
            reached.add(answer["refused"])
        for row in (answer.get("refused_names") or []):
            reached.add(row["refused"])
        return answer

    MINE = {"name": "tag-my-way", "scope": "user", "author": "Ajmal"}
    THEIRS = {"name": "sara-way", "scope": "user", "author": "Sara"}

    def raise_(to, by="Ajmal", approved_by="Sara", skill="tag-my-way"):
        return {"skill": skill, "to": to, "by": by,
                "approved_by": approved_by}

    print("1. The ladder comes off HERON-RAG-LIB-001's own meanings")
    check(SKL.LADDER == (SCOPE.USER, SCOPE.COMPANY, SCOPE.GLOBAL),
          "user -> company -> global")
    check("shared with nobody" in SCOPE.SCOPE_MEANING[SCOPE.USER],
          "user is shared with nobody")
    check("shared with everyone" in SCOPE.SCOPE_MEANING[SCOPE.GLOBAL],
          "and global is shared with everyone")
    check("never shared sideways" in SCOPE.SCOPE_MEANING[SCOPE.PROJECT],
          "while project says: never shared sideways")
    check(SKL.SIDEWAYS == SCOPE.PROJECT and SCOPE.PROJECT not in SKL.LADDER,
          "so project is not a rung")
    check(SKL.SCOPE.SCOPE_MEANING is SCOPE.SCOPE_MEANING,
          "and the meanings ARE heron_scope's object, not a copy")
    for rung in SKL.LADDER:
        check('"%s"' % rung not in logic,
              "no literal '%s' in the agent" % rung)

    print("\n2. Personal stays personal")
    quiet = ask([MINE], reader="Ajmal")
    check(quiet["mine"] == [MINE] and not quiet["raise_"],
          "with no raise asked for, nothing moves")
    for field in ("skill", "to", "by", "approved_by"):
        short = dict(raise_("company"))
        del short[field]
        answer = ask([MINE], reader="Ajmal", raising=[short])
        check(answer["refused_names"][0]["refused"] == "NOT_A_RAISE",
              "a raise with no `%s` is not a raise" % field)
        check(field in (answer["refused_names"][0].get("missing") or [])
              or field == "skill",
              "  and the answer names what is missing")
    check(any("EXPLICIT or it has not happened" in line or
              "does not drift upward" in line
              for line in quiet["unjudged"]),
          "the answer says a skill does not drift upward for being useful")

    print("\n3. One step at a time")
    jumped = ask([MINE], reader="Ajmal", raising=[raise_("global")])
    check(jumped["refused_names"][0]["refused"] == "SKIPS_A_STEP",
          "user to global is refused")
    check("skipped review" in jumped["refused_names"][0]["why"],
          "as a skipped review, not a fast promotion")
    step = ask([MINE], reader="Ajmal", raising=[raise_("company")])
    check(len(step["raise_"]) == 1 and step["raise_"][0]["to"] == "company",
          "user to company is one rung and is planned")
    at_company = {"name": "tag-my-way", "scope": "company",
                  "author": "Ajmal"}
    second = ask([at_company], reader="Ajmal", raising=[raise_("global")])
    check(len(second["raise_"]) == 1,
          "and from company, global is the next rung")
    for backwards in ("user", "company"):
        answer = ask([at_company], reader="Ajmal",
                     raising=[raise_(backwards)])
        check(answer["refused_names"][0]["refused"] == "NOT_A_RAISE",
              "'%s' from company is not upward" % backwards)
    for side in ({"name": "p", "scope": "project", "author": "Ajmal"},):
        answer = ask([side], reader="Ajmal",
                     raising=[raise_("company", skill="p")])
        check(answer["refused_names"][0]["refused"] == "SIDEWAYS_IS_NOT_UP",
              "out of a project is sideways, not up")
    into = ask([MINE], reader="Ajmal", raising=[raise_("project")])
    check(into["refused_names"][0]["refused"] == "SIDEWAYS_IS_NOT_UP",
          "and into one is too - a personal skill handed to one client")

    print("\n4. The author may not approve")
    self_signed = ask([MINE], reader="Ajmal",
                      raising=[raise_("company", approved_by="Ajmal")])
    check(self_signed["refused_names"][0]["refused"]
          == "THE_AUTHOR_MAY_NOT_APPROVE",
          "the author approving their own raise is refused")
    check(self_signed["refused_names"][0]["why"].count("extension") == 1,
          "and the refusal says it is an extension")
    check(ask([MINE], reader="Ajmal",
              raising=[raise_("company", approved_by="AJMAL")]
              )["refused_names"][0]["refused"]
          == "THE_AUTHOR_MAY_NOT_APPROVE",
          "matched without case, so a capital does not get round it")
    check(any("EXTENSION, NOT A" in line.upper()
              for line in step["unjudged"]),
          "every answer says the rule is an extension rather than a "
          "quotation")
    golden = " ".join(io.open(os.path.join(ROOT, "docs",
                                           "14-golden-rules.md"),
                              encoding="utf-8").read().split())
    check("No agent approves itself" in golden,
          "Golden Rule 7 really says no AGENT approves itself")
    skills_doc = " ".join(io.open(os.path.join(ROOT, "docs",
                                               "09-skills-and-fragments.md"),
                                  encoding="utf-8").read().split())
    check("Explicit, recorded, one person" in skills_doc,
          "and docs/09 asks for one person, explicit and recorded")
    check("may not be the author" not in skills_doc,
          "without saying the person may not be the author - which is "
          "why this is stricter than what is written")

    print("\n5. Separate means separate")
    both = ask([MINE, THEIRS], reader="Ajmal")
    check([one["name"] for one in both["mine"]] == ["tag-my-way"],
          "somebody else's personal skill is not in `mine`")
    theirs = [row for row in both["refused_names"]
              if row["refused"] == "NOT_YOURS"]
    check(len(theirs) == 1 and theirs[0]["skill"] == "sara-way",
          "it is refused as NOT_YOURS")
    check(set(theirs[0]) == {"skill", "refused", "why"},
          "and the refusal carries no more than the caller handed in: %s"
          % ", ".join(sorted(theirs[0])))
    check("no reading of" in theirs[0]["why"],
          "refused rather than returned with a note")
    shared = ask([{"name": "office", "scope": "company", "author": "Sara"}],
                 reader="Ajmal")
    check(not shared["refused_names"] and not shared["mine"],
          "while somebody else's COMPANY skill is neither refused nor mine "
          "- it is simply not personal")
    unnamed = ask([MINE, THEIRS])
    check(not unnamed["mine"],
          "with no reader named, NO personal skill comes back - this used "
          "to return every author's, on the reasoning that guessing was "
          "wrong either way. Returning them all is the widest possible "
          "guess, and the scope HERON-RAG-LIB-001 calls 'shared with "
          "nobody' is the one thing it may not mean")

    print("\n6. Every failure is named and reached")
    check(ask(None).get("refused") == "NOTHING_TO_MANAGE",
          "nothing handed in is refused")
    check(ask([]).get("refused") == "NOTHING_TO_MANAGE", "and so is empty")
    for bad, why in (("not a map", "a skill that is not a map"),
                     ({"scope": "user", "author": "A"}, "one with no name"),
                     ({"name": "n", "author": "A"}, "one with no scope"),
                     ({"name": "n", "scope": "user"}, "one with no author"),
                     ({"name": "n", "scope": "nowhere", "author": "A"},
                      "and one whose scope is not one")):
        check(ask([bad]).get("refused_names")[0]["refused"] == "NOT_A_SKILL",
              why)
    def refusals(answer):
        """Every refusal in the answer, not whichever landed first."""
        return [one["refused"] for one in (answer["refused_names"] or [])]

    check("NOT_A_RAISE" in refusals(ask([MINE], raising=["not a map"])),
          "a raise that is not a map")
    check("NOT_A_RAISE" in refusals(
        ask([MINE], raising=[raise_("company", skill="nope")])),
          "and one naming a skill nobody handed in")
    for moving in ("open(", "shutil", "os.rename", "write(", "os.remove"):
        check(moving not in logic, "the agent never uses %s" % moving)
    print()
    print("R. THE SECOND CODEX REVIEW - nobody asking is not everybody "
          "asking")
    both = [{"name": "mine", "scope": "user", "author": "Ajmal"},
            {"name": "theirs", "scope": "user", "author": "Someone Else"}]
    answer = SKL.manage(both)
    for row in (answer.get("refused_names") or []):
        reached.add(row.get("refused"))
    check(not answer.get("mine"),
          "with NO reader named, no personal skill comes back - the test "
          "for 'is this yours' was skipped when `reader` was empty, so a "
          "call that said nothing about who was asking got every "
          "author's private skills")
    check(all(row.get("refused") == "NOBODY_IS_ASKING"
              for row in (answer.get("refused_names") or [])),
          "  and each is refused NOBODY_IS_ASKING rather than returned")
    answer = SKL.manage(both, reader="Ajmal")
    check([one["name"] for one in answer["mine"]] == ["mine"],
          "  a named reader still gets their own, and only their own")

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-USR-SKL-003.yaml"))
    named = contract.get("failures") or []
    check(len(named) == len(set(named)),
          "the contract declares each failure once")
    for failure in named:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    spare = sorted(reached - set(named))
    check(not spare, "and nothing else was refused%s"
          % ("" if not spare else ": %s" % ", ".join(spare)))
    check(len(step["unjudged"]) == 4, "four things are left unjudged")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    personal stays personal, and one rung at a time")
    return 0


if __name__ == "__main__":
    sys.exit(main())
