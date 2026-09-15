# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-USR-MEM-002
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
User memory - never-stored is checked first, and it is a refusal.

    python tests/test_memory.py

WHAT IT PROVES
  1. ARTICLE 17 SAYS WHAT THE AGENT SAYS IT SAYS, read out of the
     Constitution.

  2. THE SECRET NEVER APPEARS IN THE ANSWER. Not in the refusal, not in
     a count, not anywhere - which is article 17's "never include one in
     a result" applied to this agent's own output.

  3. NEVER-STORED IS CHECKED FIRST. A candidate that is wrong three ways
     is refused for the credential, and the same candidate without one
     is refused for the next thing - so the order is proved, not stated.

  4. THE SHAPES ARE heron_secrets', CALLED NOT COPIED - all six, and no
     pattern of its own.

  5. SUPERSESSION REPLACES AND RECORDS. The old value survives in
     `replaces`, and the same value is not a contradiction.

  6. EXPIRY IS APPLIED, NOT INVENTED - a TTL must arrive, and no
     duration is chosen here.

  7. NOTHING IS REFUSED FOR BEING SEEN TOO FEW TIMES.

  8. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_memory as MEM                                     # noqa: E402
import heron_secrets as SECRETS                                # noqa: E402
import heron_scope as SCOPE                                    # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_memory.py"),
                    encoding="utf-8").read()
    code = whole.split("\nfrom __future__", 1)[1]
    logic = code.split("\ndef main(")[0]

    def ask(candidates, **kw):
        answer = MEM.decide(candidates, **kw)
        if answer.get("refused"):
            reached.add(answer["refused"])
        for row in (answer.get("refused_names") or []):
            reached.add(row["refused"])
        return answer

    def one(**kw):
        base = {"about": "a", "what": "something", "scope": "user", "seen": 3}
        base.update(kw)
        return base

    TOKEN = SECRETS.FAKE_FORGE_TOKEN

    print("1. Article 17 says what the agent says it says")
    rules = " ".join(io.open(os.path.join(ROOT, "HERON_CONSTITUTION.md"),
                             encoding="utf-8").read().split())
    check("Secrets live in the credential store, nowhere else" in rules,
          "secrets live in the credential store, nowhere else")
    check("Never include one in a result" in rules,
          "and never include one in a result")
    memory = " ".join(io.open(os.path.join(ROOT, "docs",
                                           "10-memory-and-knowledge.md"),
                              encoding="utf-8").read().split())
    check("**replaces** it and records the replacement" in memory,
          "docs/10 s276: new knowledge replaces old AND records it")
    check("It does not sit beside it" in memory, "it does not sit beside it")
    check("Temporary memory has a TTL" in memory,
          "and temporary memory has a TTL")

    print("\n2. The secret never appears in the answer")
    leaked = ask([one(about="key", what="use " + TOKEN)])
    check(leaked["refused_names"][0]["refused"] == "NEVER_STORED",
          "a candidate carrying a token is refused")
    check(TOKEN not in str(leaked),
          "and the token is NOWHERE in the whole answer")
    check(TOKEN[:12] not in str(leaked),
          "not even the first twelve characters of it")
    check("[redacted]" not in str(leaked),
          "and no redacted copy either - it was refused, not cleaned")
    check(leaked["refused_names"][0]["shapes"] == ["github token"],
          "what comes back is the SHAPE that was found: %s"
          % leaked["refused_names"][0]["shapes"])
    check("what" not in leaked["refused_names"][0],
          "the candidate's text is not carried into the refusal at all")
    check(not leaked["store"] and not leaked["supersede"],
          "and it is in no other list - refused WHOLE")
    check("article 17" in leaked["refused_names"][0]["why"].lower(),
          "the refusal cites the article")
    check("redact-and-store" in whole,
          "and the agent says why it does not redact and store")

    print("\n3. Never-stored is checked first")
    # WRONG THREE WAYS: a credential, a scope that is not one, no TTL.
    worst = ask([{"about": "k", "what": TOKEN, "scope": "nonsense",
                  "seen": 1}])
    check(worst["refused_names"][0]["refused"] == "NEVER_STORED",
          "a candidate wrong three ways is refused for the CREDENTIAL")
    clean = ask([{"about": "k", "what": "ordinary", "scope": "nonsense",
                  "seen": 1}])
    check(clean["refused_names"][0]["refused"] == "NOT_A_SCOPE",
          "and the same candidate without one is refused for the scope - "
          "so the first answer was the order, not the only answer")
    order = logic.index("NEVER_STORED") < logic.index("NOT_A_SCOPE")
    check(order, "the code checks it first too")

    print("\n4. The shapes are heron_secrets', called not copied")
    check("SECRETS.Secrets()" in logic, "the agent calls heron_secrets")
    for copied in ("re.compile", "ghp_", "sk-", "AKIA", "xox", "PATTERNS ="):
        check(copied not in logic,
              "and holds no pattern of its own (%s)" % copied)
    for shape, pattern in SECRETS.PATTERNS:
        sample = {"github token": TOKEN,
                  "github pat": "github_pat_" + "A1b2C3d4E5f6G7h8I9j0",
                  "provider key": "sk-" + "A1b2C3d4E5f6G7h8",
                  "aws access key": SECRETS.FAKE_CLOUD_KEY,
                  "slack token": "xoxb-" + "1234567890AB",
                  "bearer token": "bearer " + "A1b2C3d4E5f6G7h8I9j0K1"}[shape]
        answer = ask([one(about="k", what="value " + sample)])
        check(answer["refused_names"][0]["refused"] == "NEVER_STORED",
              "a %s is caught" % shape)
        check(sample not in str(answer), "  and does not reach the answer")

    print("\n5. Supersession replaces and records")
    changed = ask([one(about="units", what="metres")],
                  remembered=[{"about": "units", "what": "millimetres",
                               "since": "2026-06-01", "source": "Ajmal"}])
    check(len(changed["supersede"]) == 1 and not changed["store"],
          "a contradicting value supersedes rather than being stored beside")
    replaced = changed["supersede"][0]["replaces"]
    check(replaced["what"] == "millimetres",
          "the OLD value survives in `replaces`: %s" % replaced["what"])
    check(replaced["since"] == "2026-06-01" and replaced["source"] == "Ajmal",
          "with when it was set and who set it - not only a count")
    check("does not sit beside it" in changed["supersede"][0]["why"],
          "and the reason quotes docs/10 s276")
    same = ask([one(about="units", what="millimetres")],
               remembered=[{"about": "units", "what": "millimetres"}])
    check(len(same["store"]) == 1 and not same["supersede"],
          "the SAME value is not a contradiction and does not supersede")
    unheld = ask([one(about="brand new", what="x")],
                 remembered=[{"about": "units", "what": "mm"}])
    check(len(unheld["store"]) == 1 and not unheld["supersede"],
          "and something nothing is held about is simply stored")

    print("\n6. Expiry is applied, not invented")
    check(ask([one(scope=SCOPE.TEMPORARY)])["refused_names"][0]["refused"]
          == "NO_TTL",
          "temporary with no TTL is refused")
    timed = ask([one(scope=SCOPE.TEMPORARY, ttl="1 hour")])
    check(timed["store"][0]["expiry"] == {"rule": "ttl", "ttl": "1 hour"},
          "and one with a TTL carries it: %s" % timed["store"][0]["expiry"])
    project = ask([one(scope=SCOPE.PROJECT, project="Tower A")])
    check(project["store"][0]["expiry"]["rule"]
          == "archived when the project closes",
          "a project candidate archives on close")
    check(project["store"][0]["expiry"]["project"] == "Tower A",
          "naming which project")
    plain = ask([one(scope="user")])
    check(plain["store"][0]["expiry"] is None,
          "and a scope with no expiry rule gets none invented")
    for number in ("hour", "day", "week", "3600", "24", "30"):
        check(number not in logic or number in ("day",),
              "no duration is written into the agent (%s)" % number)

    print("\n7. Nothing is refused for being seen too few times")
    for seen in (1, 2, 100):
        answer = ask([one(seen=seen)])
        check(len(answer["store"]) == 1,
              "seen %d time(s) is stored" % seen)
        check(answer["store"][0]["evidence"]["seen"] == seen,
              "  with the count travelling in `evidence`")
    check(ask([one(seen=1, window="one day")])["store"][0]["evidence"]
          ["window"] == "one day",
          "and the window beside it, so 'seen once, on one day' reads "
          "itself")
    check(ask([{"about": "a", "what": "x", "scope": "user"}]
              )["refused_names"][0]["refused"] == "NOT_A_CANDIDATE",
          "while `seen` ABSENT is refused - it is not `seen: 1`, and "
          "treating it as one would invent the evidence")
    check(any("every cutoff is invented" in line
              for line in ask([one()])["unjudged"]),
          "the answer says why no cutoff is applied")

    print("\n8. Every failure is named and reached")
    check(ask(None).get("refused") == "NOTHING_TO_DECIDE",
          "nothing handed in is refused")
    check(ask([]).get("refused") == "NOTHING_TO_DECIDE", "and so is empty")
    for bad, why in (("not a map", "a candidate that is not a map"),
                     ({"what": "x", "scope": "user", "seen": 1},
                      "one with no `about` - a memory nobody can name can "
                      "never be replaced, so it would accumulate beside its "
                      "own contradictions"),
                     ({"about": "a", "scope": "user", "seen": 1},
                      "one with no `what`"),
                     ({"about": "a", "what": "x", "seen": 1},
                      "and one with no scope")):
        answer = ask([bad])
        check(answer["refused_names"][0]["refused"] == "NOT_A_CANDIDATE", why)
    for writing in ("open(", "sqlite", "json.dump", "os.remove", "write("):
        check(writing not in logic, "the agent never uses %s" % writing)
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-USR-MEM-002.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 5, "the contract declares 5 failures")
    for failure in named:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    spare = sorted(reached - set(named))
    check(not spare, "and nothing else was refused%s"
          % ("" if not spare else ": %s" % ", ".join(spare)))
    check(len(plain["unjudged"]) == 4, "four things are left unjudged")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    never-stored first, and the secret never reaches the answer")
    return 0


if __name__ == "__main__":
    sys.exit(main())
