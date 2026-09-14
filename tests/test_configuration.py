# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-INS-CFG-006
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test

"""
Configuration - two halves, and only one of them may leave the machine.

    python tests/test_configuration.py

WHAT IT PROVES
  1. THE HALF COMES FROM docs/21 s9's LIST, never from the caller - and
     every key in that list is in this agent's table.

  2. A KEY OUTSIDE THE LIST IS REFUSED, not sorted by resemblance.
     `revit-path` is one character from a real key and is still refused.

  3. THE VALUE IS CHECKED, NOT JUST THE KEY. A portable setting carrying
     a drive letter, a home directory, a user variable, a UNC share or a
     loopback name is refused - D-07 makes a committed leak permanent.

  4. THE WALK CARRIES THE PATH, so a credential nested under `api-key`
     inside a map is caught by the name that gives it away.

  5. A SECRET IS NEVER CONFIGURATION - article 17, by shape AND by key
     name.

  6. IT IS ADMIN BOTH WAYS - origin and signature, and an unsigned change
     is one the audit trail cannot attribute.

  7. IT IS HONEST ABOUT WHAT THE SHAPE CHECK MISSES, and about the one
     setting an open decision would remove.

  8. NOTHING IS WRITTEN.

  9. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE AND REACHED.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_configuration as CFG                              # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []

SIGNED = {"by": "ajmal", "at": "2026-09-14T12:00Z"}


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def settings(**more):
    found = {"supported-revit-versions": ["2024", "2025"],
             "revit-paths": {"2025": "C:\\Program Files\\Autodesk\\Revit 2025"},
             "enabled-skills": ["duct-sizing"],
             "company-standards": {"duct-naming": "SYS-LEVEL-NNN"},
             "update-policies": {"auto": False, "pinned": "0.3.2"}}
    found.update(more)
    return found


def main():
    reached = set()
    source = open(os.path.join(ROOT, "brain", "heron_configuration.py"),
                  encoding="utf-8").read()

    # A SENTINEL, not None: `None` is one of the inputs under test, and a
    # helper that read it as "use the default" would quietly skip the case.
    DEFAULT = object()

    def ask(what=DEFAULT, **kw):
        kw.setdefault("origin", "user")
        kw.setdefault("approval", dict(SIGNED))
        answer = CFG.split(settings() if what is DEFAULT else what, **kw)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    print("1. The half comes from docs/21 s9's list")
    section = open(os.path.join(ROOT, "docs",
                                "21-resilience-and-operations.md"),
                   encoding="utf-8").read()
    section = section.split("## 9. Configuration management")[1][:900]
    flat = " ".join(section.split()).lower()
    for key in CFG.SETTINGS:
        words = key.replace("-", " ")
        check(words in flat or key.split("-")[-1] in flat,
              "'%s' comes from the docs/21 s9 paragraph itself" % key)
    check(set(CFG.SETTINGS.values()) == {"machine", "portable"},
          "and every one is in exactly one of the two halves")
    good = ask()
    check(sorted(good["machine"]) == ["revit-paths",
                                      "supported-revit-versions"],
          "the machine-specific ones land in the machine half")
    check(sorted(good["portable"]) == ["company-standards", "enabled-skills",
                                       "update-policies"],
          "and the portable ones in the portable half")
    check(good["shareable"] == sorted(good["portable"]),
          "`shareable` is the portable half, named")
    check("Only the portable half may be shared" in good["why"],
          "and the sentence says which half may be committed")

    print()
    print("2. A key outside the list is refused")
    for key in ("revit-path", "enabled_agent", "telemetry", "log-level",
                "ai_provider", "company-standard", ""):
        answer = ask({key: "x"})
        check(answer.get("refused") == "SETTING_NOT_IN_THE_LIST",
              "'%s' is refused" % key)
    answer = ask({"revit-path": "x"})
    check("does not sort it into a half by resemblance" in answer["why"],
          "and says it will not guess the half - 'revit-path' is one "
          "character from a real key")
    check("somebody signs that" in answer["proposal"],
          "with the fix being a decision, not a patch")
    check(ask({"ENABLED-SKILLS": ["x"]}).get("refused") is None,
          "while case and spacing are read, not refused on spelling")

    print()
    print("3. The value is checked, not just the key")
    for value, what in (("C:\\Users\\ajmal\\skills", "a drive letter"),
                        ("D:\\skills", "another drive letter"),
                        ("/home/ajmal/skills", "a home directory"),
                        ("/Users/ajmal/skills", "a mac home directory"),
                        ("~/skills", "a home-relative path"),
                        ("%APPDATA%\\heron", "a user variable"),
                        ("%USERPROFILE%\\x", "another user variable"),
                        ("$HOME/skills", "a shell variable"),
                        ("\\\\practice-nas\\bim", "a UNC share"),
                        ("http://localhost:8080", "a loopback name"),
                        ("http://127.0.0.1:8080", "a loopback address")):
        answer = ask(settings(**{"enabled-skills": [value]}))
        check(answer.get("refused") == "MACHINE_VALUE_IN_PORTABLE",
              "a portable setting carrying %s is refused" % what)
    answer = ask(settings(**{"enabled-skills": ["C:\\x"]}))
    check("The key being portable is not enough" in answer["why"],
          "saying the key is not what gets committed")
    check("D-07" in answer["why"] and "permanent" in answer["why"],
          "and citing the decision that makes a leak permanent")
    check("refer to it from the portable one by name, not by location"
          in answer["proposal"],
          "with what to do instead")
    # THE MACHINE HALF MAY CARRY MACHINE PATHS - that is what it is for.
    check(ask()["machine"]["revit-paths"]["2025"].startswith("C:\\"),
          "while the MACHINE half carries a drive letter without complaint "
          "- that is the whole point of having two halves")

    print()
    print("4. The walk carries the path")
    answer = ask(settings(**{"company-standards":
                             {"templates": {"sheet": "\\\\nas\\t.rvt"}}}))
    check(answer.get("refused") == "MACHINE_VALUE_IN_PORTABLE",
          "a value two levels down is still checked")
    check(answer["setting"] == "company-standards.templates.sheet",
          "and the refusal names the PATH, not the top-level key")
    answer = ask(settings(**{"enabled-skills": ["ok", "~/x"]}))
    check(answer["setting"] == "enabled-skills[1]",
          "a list index is part of the path too")
    check("[(path, setting)]" in source,
          "the walk returns (path, value) pairs by construction")

    print()
    print("5. A secret is never configuration")
    for value, what in (("sk-" + "A1b2C3d4" * 3, "an API key prefix"),
                        ("ghp_" + "A1b2C3d4" * 3, "a forge token"),
                        ("github_pat_" + "x" * 20, "a fine-grained token"),
                        ("xoxb-1-2-3", "a chat token"),
                        ("-----BEGIN RSA PRIVATE KEY-----", "a private key"),
                        ("AKIAIOSFODNN7EXAMPLE", "a cloud key id")):
        answer = ask(settings(**{"ai-provider": value}))
        check(answer.get("refused") == "SECRET_IN_CONFIGURATION",
              "%s is refused" % what)
    for key in ("api-key", "token", "secret", "password", "credential",
                "api_key"):
        answer = ask(settings(**{"ai-provider": {"name": "anthropic",
                                                 key: "hunter2"}}))
        check(answer.get("refused") == "SECRET_IN_CONFIGURATION",
              "a nested key named '%s' is refused whatever its value looks "
              "like" % key)
    answer = ask(settings(**{"ai-provider": {"api-key": "hunter2"}}))
    check("article 17" in answer["why"],
          "citing the article that puts secrets in the credential store")
    check("a settings file is both" in answer["why"],
          "and that a settings file is a source file AND a commit")
    check("A handle travels; a value does not" in answer["proposal"],
          "with the credential manager's own rule as the fix")
    check(ask(settings(**{"ai-provider": {"name": "anthropic",
                                          "api-key": ""}}))
          .get("refused") is None,
          "while an EMPTY value under a secret-named key is not a secret - "
          "there is nothing there to leak")

    print()
    print("6. It is ADMIN both ways")
    for origin in ("a document Heron read", "a community package", None,
                   "", "the installer"):
        check(ask(origin=origin).get("refused") == "NOT_FROM_THE_USER",
              "%r cannot write configuration" % origin)
    check("security boundary" in ask(origin=None)["proposal"],
          "and the refusal says configuration is a security boundary")
    for approval in (None, {}, True, {"by": "ajmal"}, {"at": "T"},
                     "ajmal", {"by": "", "at": "T"}):
        check(ask(approval=approval).get("refused") == "NOT_APPROVED",
              "%r is not an approval" % (approval,))
    check("audit trail cannot attribute" in ask(approval=None)["why"],
          "because docs/21 s9 asks for changes to be auditable")
    check(good["record"] == {"by": "ajmal", "at": "2026-09-14T12:00Z",
                             "settings": sorted(settings())},
          "and a successful split carries the auditable record")

    print()
    print("7. It is honest about what the shape check misses")
    missed = [note for note in good["unjudged"] if "does NOT catch" in note]
    check(missed, "the answer says what the check does not catch")
    for gap in ("relative path", "client's name", "hostname"):
        check(gap in missed[0], "naming %s as a gap" % gap)
    check("narrows the leak; it does not close it" in missed[0],
          "and is plain about the size of the claim")
    routing = [note for note in good["unjudged"] if "model-routing" in note]
    check(routing and "D-65" in routing[0],
          "and that model-routing follows the list until D-65 is answered")
    check("remove the setting rather than move it" in routing[0],
          "saying what answering it would actually do")

    print()
    print("8. Nothing is written")
    check(good["wrote"] is False, "`wrote` is False even on success")
    for word in ("subprocess", "os.system", "exec(", "eval(", "open(",
                 "shutil", "os.remove", "json.dump", "yaml.dump"):
        check(word not in source, "the source has no %s" % word)
    check(any("NOTHING WAS WRITTEN" in note for note in good["unjudged"]),
          "and the answer says so")
    check(any("read before it is applied" in note
              for note in good["unjudged"]),
          "with why that makes 'changes auditable' easier to keep")

    print()
    print("9. Every failure the contract declares is named and reached")
    for empty in ({}, None, [], ""):
        check(ask(empty).get("refused") == "NOTHING_TO_WRITE",
              "%r is nothing to write" % (empty,))
    check("not a minimal one" in ask({})["why"],
          "and an empty configuration is not a minimal one")
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-INS-CFG-006.yaml"))
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
    print("PASS    two halves, and only one of them may leave the machine")
    return 0


if __name__ == "__main__":
    sys.exit(main())
