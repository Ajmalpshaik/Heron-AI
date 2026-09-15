# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-GIT-REP-002
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Repositories - a password nobody can pattern-match is still a password.

    python tests/test_repository.py

WHAT IT PROVES
  1. THE PATTERN LIST ALONE IS NOT ENOUGH, measured rather than assumed:
     heron_secrets catches `https://a:ghp_...@host` and does NOT catch
     `https://a:hunter2@host`. The second is caught here by its SHAPE.

  2. NEITHER THE VALUE NOR THE URL IS IN THE ANSWER - in this one case
     the url IS the credential, so returning it would be the leak.

  3. AN UNDECLARED REMOTE IS REFUSED, not listed as a curiosity, and a
     DECLARED one that is missing is only reported.

  4. WITH NOTHING DECLARED, NO COMPARISON IS CLAIMED. "Not carrying a
     token" is not the same as "somewhere you meant", and the answer
     says which of those it checked.

  5. THE STRUCTURE IS NAMED, NEVER RE-DERIVED - and the agents named
     really do own tools/check-structure.py.

  6. SETTINGS ARE NOT GUESSED AT.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_repository as REP                                 # noqa: E402
import heron_secrets as SECRETS                                # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []

TOKEN = "ghp_" + "A" * 36
HOME = "https://github.com/Ajmalpshaik/Heron-AI.git"


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_repository.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]
    keeper = SECRETS.Secrets()

    print("\n1. the pattern list alone is not enough")
    shaped = "https://ajmal:hunter2@github.com/x/y.git"
    patterned = "https://ajmal:%s@github.com/x/y.git" % TOKEN
    # MEASURED, NOT ASSUMED. This is the whole reason the shape check
    # exists, so it is checked rather than described.
    check(keeper.redact(patterned)[1] != [],
          "heron_secrets catches a token in a url")
    check(keeper.redact(shaped)[1] == [],
          "and does NOT catch 'hunter2' - no pattern can")
    caught = REP.inspect([{"name": "origin", "url": shaped}])
    reached.add(caught.get("refused"))
    check(caught["refused"] == "CARRIES_A_SECRET",
          "this agent catches it anyway")
    check(caught["by"] == "its shape",
          "and says HOW: by its shape, not by the pattern list")
    check("by construction" in caught["why"],
          "anything between :// and @ with a colon in it is a credential "
          "by construction, whoever issued it")
    known = REP.inspect([{"name": "origin", "url": patterned}])
    check(known["refused"] == "CARRIES_A_SECRET"
          and known["by"] == "the pattern list",
          "a recognised token is still attributed to the pattern list")
    # AND A CLEAN URL IS NOT FLAGGED.
    for clean in (HOME, "git@github.com:x/y.git", "ssh://git@host/x.git",
                  "https://github.com/x/y.git"):
        check(REP.inspect([{"name": "origin", "url": clean}])["inspected"],
              "'%s' is clean" % clean)

    print("\n2. neither the value nor the url is in the answer")
    check(TOKEN not in repr(known), "the token is nowhere in the answer")
    check("hunter2" not in repr(caught),
          "and neither is the password")
    check(shaped not in repr(caught) and patterned not in repr(known),
          "nor is the URL - in this one case the url IS the credential, "
          "so returning it would be the leak")
    check(caught["remote"] == "origin" and caught["found"],
          "what comes back is the remote's NAME and the kind: %s"
          % ", ".join(caught["found"]))

    print("\n3. an undeclared remote is refused")
    strayed = REP.inspect(
        [{"name": "origin", "url": HOME},
         {"name": "mirror", "url": "https://example.invalid/copy.git"}],
        [{"name": "origin", "url": HOME}])
    reached.add(strayed.get("refused"))
    check(strayed["refused"] == "UNDECLARED_REMOTE"
          and strayed["remotes"] == ["mirror"],
          "the remote pointing somewhere undeclared is refused, and named")
    check("a destination nobody chose" in strayed["why"],
          "with the reason: a push goes to whatever the remote points at")
    # A MISSING DECLARED ONE IS ONLY REPORTED.
    short = REP.inspect([{"name": "origin", "url": HOME}],
                        [{"name": "origin", "url": HOME},
                         {"name": "upstream",
                          "url": "https://github.com/heron/x.git"}])
    check(short["inspected"] and short["missing"] == ["upstream"],
          "a DECLARED remote that is not configured is only reported - it "
          "breaks nothing that is not already broken")
    check("not configured" in short["unjudged"][0],
          "and the answer says which")
    # THE NAME IS NOT THE DESTINATION.
    renamed = REP.inspect([{"name": "somewhere-else", "url": HOME}],
                          [{"name": "origin", "url": HOME}])
    check(renamed["inspected"],
          "a declared destination under a different remote NAME is fine - "
          "it is the url that decides where a push goes")

    print("\n4. with nothing declared, no comparison is claimed")
    alone = REP.inspect([{"name": "origin", "url": HOME}])
    check(alone["inspected"], "it is answered")
    check(alone["remotes"][0]["declared"] is False,
          "and nothing is marked declared")
    check("nothing was declared to check them against" in alone["why"],
          "the answer says nothing was declared")
    check("is not the same as 'somewhere you meant'"
          in alone["unjudged"][0],
          "and that 'not carrying a token' is not 'somewhere you meant'")

    print("\n5. the structure is named, never re-derived")
    check(alone["structure"]["checked"] is False,
          "`checked` is false, always")
    owner = alone["structure"]["owned_by"][0]
    check(owner["where"] == "tools/check-structure.py",
          "the owner is named by path")
    header = io.open(os.path.join(ROOT, "tools", "check-structure.py"),
                     encoding="utf-8").read().split("\n")[0]
    for agent in ("HERON-WSP-VAL-003", "HERON-AHR-MON-011"):
        check(agent in header,
              "%s really does claim that file, per its own header" % agent)
        check(agent in owner["what"], "and the answer names it")
    check("PROPOSALS F17" in alone["unjudged"][1],
          "citing the finding that stopped a duplicate being built before")
    # AND IT ENFORCES NONE OF ITS OWN. A word search would only find the
    # module NAMING the owner, so the proof is behavioural: the
    # structure answer is identical whatever it is handed.
    elsewhere = REP.inspect([{"name": "x", "url": "git@host:y.git"}])
    check(elsewhere["structure"] == alone["structure"],
          "the structure answer is byte-identical for a different "
          "repository - nothing about it is derived from the input")
    check(elsewhere["structure"]["checked"] is False,
          "and it is false there too, rather than sometimes checking")

    print("\n6. settings are not guessed at")
    check(alone["settings"]["read"] is False, "`read` is false, always")
    check("D-01" in alone["settings"]["why"],
          "with the reason: the API is the host's under D-01")
    imports = sorted(line.split()[1] for line in logic.split("\n")
                     if line.startswith("import "))
    check(imports == ["heron_secrets", "os", "re", "sys"],
          "the whole import list is os, re, sys and the credential store: "
          "%s - nothing there could resolve a host" % ", ".join(imports))
    check(any("read as text" in line for line in alone["unjudged"]),
          "and the answer says a url was read as TEXT - nothing resolved "
          "a host or asked a server who it was")

    print("\n7. every declared failure is named and reached")
    for remotes, wanted, name in (
            ([], [], "NOTHING_TO_CHECK"),
            (["a string"], [], "NOT_A_REMOTE"),
            ([{"name": "origin"}], [], "NOT_A_REMOTE"),
            ([{"url": HOME}], [], "NOT_A_REMOTE")):
        answer = REP.inspect(remotes, wanted)
        reached.add(answer.get("refused"))
        check(answer.get("refused") == name, "%s is reached" % name)

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-GIT-REP-002.yaml"))
    declared = contract.get("failures") or []
    check(len(declared) == 4, "the contract declares 4 failures")
    for failure in declared:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(declared) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    check(len(alone["unjudged"]) == 4, "four things are left unjudged")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    a password nobody can pattern-match is still a password")
    return 0


if __name__ == "__main__":
    sys.exit(main())
