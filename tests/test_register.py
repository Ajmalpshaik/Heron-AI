# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-INS-MCP-004
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test

"""
MCP registration - the triangle agrees, or nothing is registered.

    python tests/test_register.py

WHAT IT PROVES
  1. THE MOST COMMON REAL-WORLD FAILURE IS CAUGHT AND WORDED AS docs/04 s5
     WORDS IT - which release, which version is there, which is needed,
     and that Revit must restart.

  2. OLDER AND NEWER ARE DIFFERENT ANSWERS, because the remedy differs.
     Telling somebody to restart Revit when the SERVER is behind wastes
     the one action they were willing to take.

  3. THERE IS NO PARTIAL REGISTRATION. A refused release is refused, and
     no release agreeing is a refusal rather than an empty success.

  4. THE RANGE IS DECLARED, NOT INFERRED.

  5. THE VERSION IS ADVERTISED, NOT STATED - a release nothing advertised
     is refused, not assumed absent-and-fine.

  6. THE REVIT TABLE IS D-05's AND IS NOT EXTRAPOLATED.

  7. IT IS ADMIN, FROM THE USER, AND REGISTERS NOTHING.

  8. IT IS HONEST THAT THE PROTOCOL CORNER OF THE TRIANGLE IS UNCHECKED.

  9. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE AND REACHED.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_register as REG                                   # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []

SIGNED = {"by": "ajmal", "at": "2026-09-14T12:00Z"}


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def server(**more):
    found = {"name": "heron", "version": "0.4.0", "needs_addin": "0.4",
             "command": "python mcp/server/heron_server.py"}
    found.update(more)
    return found


def main():
    reached = set()
    source = open(os.path.join(ROOT, "mcp", "server", "heron_register.py"),
                  encoding="utf-8").read()
    DEFAULT = object()

    def ask(what=DEFAULT, **kw):
        kw.setdefault("origin", "user")
        kw.setdefault("approval", dict(SIGNED))
        kw.setdefault("addins", {"2025": "0.4.0"})
        answer = REG.register(server() if what is DEFAULT else what, **kw)
        if answer.get("refused"):
            reached.add(answer["refused"])
        for entry in (answer.get("refused_releases") or []):
            reached.add(entry["refused"])
        return answer

    print("1. The most common real-world failure, worded as docs/04 s5")
    answer = ask(addins={"2024": "0.3.1", "2025": "0.4.1"})
    check(answer["register"] is True, "the release that agrees registers")
    check([entry["release"] for entry in answer["releases"]] == ["2025"],
          "and only that one")
    stale = answer["refused_releases"][0]
    check(stale["refused"] == "ADDIN_IS_OLDER", "the stale one is refused")
    # docs/04 s5's OWN sentence, matched against the document.
    doc = open(os.path.join(ROOT, "docs", "04-heron-mcp.md"),
               encoding="utf-8").read()
    wanted = ("Heron add-in in Revit 2024 is version 0.3.1, this Heron "
              "needs 0.4.x. Restart Revit to finish updating")
    check(wanted in " ".join(doc.split()),
          "that exact sentence is in docs/04 s5...")
    check(stale["why"].startswith(wanted),
          "...and the refusal starts with it, word for word")
    check("most common real-world failure" in stale["why"],
          "with the reason it is the one worth catching")

    print()
    print("2. Older and newer are different answers")
    answer = ask(addins={"2025": "0.5.0"})
    entry = answer["refused_releases"][0]
    check(entry["refused"] == "ADDIN_IS_NEWER", "a newer add-in is its own case")
    check("the SERVER is behind, not Revit" in entry["why"],
          "saying which side is behind")
    check("Restarting Revit would change nothing" in entry["why"],
          "and that the obvious remedy is the wrong one here")
    check("Restart Revit to finish updating" not in entry["why"],
          "so the older message is not reused for it")
    check(ask(addins={"2025": "0.4.9"})["register"] is True,
          "while a patch difference inside the declared major.minor agrees")
    check(ask(addins={"2025": "1.4.0"})["refused_releases"][0]["refused"]
          == "ADDIN_IS_NEWER", "a major bump is newer")
    check(ask(addins={"2025": "0.3.9"})["refused_releases"][0]["refused"]
          == "ADDIN_IS_OLDER", "and a minor behind is older")

    print()
    print("3. There is no partial registration")
    answer = ask(addins={"2024": "0.3.1"})
    check(answer.get("refused") == "NOTHING_TO_TALK_TO",
          "no release agreeing is a refusal, not an empty success")
    check(answer["register"] is False, "and register is False")
    check("never silently degrade" in answer["why"],
          "quoting docs/04 s5")
    check("worse than a clean refusal" in answer["why"],
          "and why a half-updated pair is worse than nothing")
    check(answer["refused_releases"], "with the releases still named")
    good = ask(addins={"2024": "0.4.0", "2025": "0.4.0"})
    check(any("NO PARTIAL REGISTRATION" in note
              for note in good["unjudged"]),
          "and a successful answer says there is no partial state")
    for word in ("read-only", "read_only", "degraded", "partial"):
        check(word not in str(good.get("entry")),
              "the entry has no '%s' mode" % word)

    print()
    print("4. The range is declared, not inferred")
    for wants in ("", "latest", "0", "~0.4", ">=0.4", "x.y", None):
        answer = ask(server(needs_addin=wants))
        check(answer.get("refused") == "NO_COMPATIBLE_RANGE",
              "needs_addin=%r is not a declared range" % (wants,))
    answer = ask(server(needs_addin=""))
    check("silently widens by one release" in answer["why"],
          "and says what inferring it would cost")
    check("somebody signs the day it changes" in answer["proposal"],
          "with the fix being a declaration somebody owns")

    print()
    print("5. The version is advertised, not stated")
    for addins in ({}, None, [], {"2025": ""}, {"2025": None},
                   {"2025": "unknown"}):
        answer = ask(addins=addins)
        check(answer.get("refused") == "NOTHING_TO_TALK_TO",
              "addins=%r leaves nothing to be compatible with" % (addins,))
    answer = ask(addins={})
    check("cannot reach Revit" in answer["why"],
          "and registering anyway would wire up a Heron that cannot reach "
          "Revit")
    check(any("ADVERTISED, not read" in note for note in good["unjudged"]),
          "the answer says the versions arrived rather than being read")

    print()
    print("6. The Revit table is D-05's")
    check(REG.REVIT_VERSIONS == ("2020", "2021", "2022", "2023", "2024",
                                 "2025", "2026", "2027"),
          "the table is 2020-2027")
    import heron_fragment as FRG
    check(REG.REVIT_VERSIONS == FRG.REVIT_VERSIONS,
          "and it is the same table the fragment loader carries")
    for release in ("2019", "2028", "2029", "twenty-25", ""):
        answer = ask(addins={release: "0.4.0"})
        check(answer.get("refused") == "REVIT_NOT_SUPPORTED",
              "Revit %r is not supported" % release)
    check("never extrapolated forward" in ask(addins={"2029": "0.4.0"})["why"],
          "and the table is never extrapolated forward")

    print()
    print("7. It is ADMIN, from the user, and registers nothing")
    for origin in ("a document Heron read", "a community package", None, ""):
        check(ask(origin=origin).get("refused") == "NOT_FROM_THE_USER",
              "%r cannot register a server" % origin)
    for approval in (None, {}, True, {"at": "T"}, "ajmal"):
        check(ask(approval=approval).get("refused") == "NOT_APPROVED",
              "%r is not an approval" % (approval,))
    check("permission decision before it is a configuration one"
          in ask(approval=None)["why"],
          "and says why registering is a permission decision")
    for word in ("subprocess", "os.system", "exec(", "eval(", "open(",
                 "json.dump", "shutil", "import requests"):
        check(word not in source, "the source has no %s" % word)
    check(any("NOTHING WAS REGISTERED" in note for note in good["unjudged"]),
          "the answer says nothing was registered")
    check(any("from inside the thing being configured" in note
              for note in good["unjudged"]),
          "and why editing a host's config here would be wrong")

    print()
    print("8. The protocol corner of the triangle is unchecked, and said so")
    note = [line for line in good["unjudged"] if "protocol" in line]
    check(note, "the answer names the protocol version")
    check("not checked here" in note[0], "as not checked")
    check("invented" in note[0],
          "because nothing here declares which protocol versions Heron "
          "speaks, so a check would compare against an invented number")
    check("triangle" in doc, "and the triangle really is docs/04 s5's")

    print()
    print("9. Every failure the contract declares is named and reached")
    for empty in ({}, None, {"name": "heron"}, "heron"):
        check(ask(empty).get("refused") == "NOTHING_TO_REGISTER",
              "%r describes no server" % (empty,))
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-INS-MCP-004.yaml"))
    named = contract.get("failures") or []
    for failure in named:
        check(failure in source, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the triangle agrees, or nothing is registered")
    return 0


if __name__ == "__main__":
    sys.exit(main())
