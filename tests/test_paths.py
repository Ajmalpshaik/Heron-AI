# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-WSP-PTH-007
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test

"""
Paths - product, data and derived, in one place instead of five.

    python tests/test_paths.py

WHAT IT PROVES
  1. THE THREE LISTS ARE docs/06 s2's TWENTY FOLDERS, every one of them.

  2. DATA IS TESTED FIRST, and "Brain agents" is why.

  3. UNKNOWN IS AN HONEST ANSWER FROM classify() and is read as DATA by
     may() - two different jobs, and merging them is how an unknown folder
     gets treated as safe.

  4. A PATH AND A COMPONENT NAME ARE ANSWERED THE SAME WAY.

  5. EVERY OPERATION'S ROW IS THE AGENT'S OWN RULE, and that agent really
     behaves that way.

  6. HERON-OPS-UPD-010 ASKS THIS AGENT rather than carrying a fifth copy -
     which is the only way to tell whether one place is enough.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE AND REACHED.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_paths as PATHS                                    # noqa: E402
import heron_update as UPD                                     # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    source = open(os.path.join(ROOT, "brain", "heron_paths.py"),
                  encoding="utf-8").read()
    folders = dict(PATHS.FOLDERS)

    print("1. The three lists are docs/06 s2's twenty folders")
    table = open(os.path.join(ROOT, "docs", "06-heron-platform.md"),
                 encoding="utf-8").read()
    table = " ".join(table.split("| **Product** |")[1][:600].split()).lower()
    for klass, words in PATHS.FOLDERS:
        for word in words:
            if word in ("index", "indexes", "vector"):
                continue      # docs/06 s2 writes these as "vector indexes"
            check(word in table,
                  "'%s' (%s) comes from that table" % (word, klass))
    check(len(folders[PATHS.PRODUCT]) == 5
          and len(folders[PATHS.DATA]) == 8,
          "five product folders and eight data ones, as the table has them")
    for klass in (PATHS.PRODUCT, PATHS.DATA, PATHS.DERIVED, PATHS.UNKNOWN):
        check(klass in PATHS.MEANING and len(PATHS.MEANING[klass]) > 30,
              "'%s' says what it means, in a sentence" % klass)

    # AND THE PLATFORM HALF, WHICH CARRIES THE SAME AGENT ID, MUST AGREE
    # ABOUT WHAT THE THREE WORDS MEAN. Two implementations of one agent
    # that disagree about DATA is the drift this module exists to prevent,
    # one layer up.
    # The /// markers are stripped first: without that, "must survive every
    # update, uninstall /// and reinstall" reads as two phrases and the
    # check passes or fails on where somebody wrapped a line.
    csharp = open(os.path.join(ROOT, "platform", "Heron.Core",
                               "HeronPaths.cs"), encoding="utf-8").read()
    csharp = " ".join(csharp.replace("///", " ").split()).lower()
    check("heron-agent:  heron-wsp-pth-007" in csharp
          or "heron-wsp-pth-007" in csharp,
          "platform/Heron.Core/HeronPaths.cs carries this same agent id")
    for klass, phrase in (
            (PATHS.PRODUCT, "replaced wholesale on update"),
            (PATHS.PRODUCT, "the user never edits it"),
            (PATHS.DATA, "must survive every update, uninstall and reinstall"),
            (PATHS.DERIVED, "safe to delete at any")):
        check(phrase in csharp,
              "the C# half defines %s with '%s'..." % (klass, phrase))
        check(phrase in " ".join(PATHS.MEANING[klass].split()).lower(),
              "...and this half says the same words")

    print()
    print("2. Data is tested first")
    check(PATHS.FOLDERS[0][0] == PATHS.DATA, "data is the first list tested")
    check(PATHS.classify("Brain agents")["class"] == PATHS.DATA,
          "so 'Brain agents' - a word from both lists - reads as data")
    check(PATHS.classify("Agents brain")["class"] == PATHS.DATA,
          "and the order of the words does not change it")
    check("not the same size" in source,
          "and the source says why the two wrong answers differ in cost")

    print()
    print("3. UNKNOWN is honest, and only may() reads it as data")
    for path in ("Documentation/readme.md", "Tests/x", "Configuration/y",
                 "who/knows.bin", "Community/pack"):
        check(PATHS.classify(path)["class"] == PATHS.UNKNOWN,
              "'%s' classifies as UNKNOWN" % path)
    answer = PATHS.classify("Documentation/readme.md")
    check("not a gap" in answer["why"],
          "and the answer says that is truthful rather than a gap")
    check(answer["matched"] is None, "with nothing matched")
    for operation in ("cleanup", "repair", "product-update", "uninstall"):
        answer = PATHS.may(operation, "Documentation/readme.md")
        check(answer["read_as"] == PATHS.DATA and answer["allowed"] is False,
              "%s may NOT touch it - unknown is read as data" % operation)
    check(PATHS.may("backup", "Documentation/readme.md")["allowed"] is True,
          "while BACKUP may, which is the same rule pointing the other way")
    answer = PATHS.may("cleanup", "who/knows.bin")
    check(answer["class"] == PATHS.UNKNOWN and answer["read_as"] == PATHS.DATA,
          "the answer reports BOTH - what it is and what it was read as")
    check("destroys a fragment library" in answer["why"],
          "and why the default points that way")

    print()
    print("4. A path and a component name are answered the same way")
    for path, name in (("Core/Heron.dll", "Core"),
                       ("Brain/global.db", "Brain agents"),
                       ("Revit/Addin.dll", "Revit add-in"),
                       ("Cache/vectors.db", "cache"),
                       ("Company/standards.yaml", "company standards")):
        check(PATHS.classify(path)["class"] == PATHS.classify(name)["class"],
              "'%s' and '%s' get the same class" % (path, name))
    check(PATHS.classify("BRAIN\\GLOBAL.DB")["class"] == PATHS.DATA,
          "case and backslashes are read, not refused")
    check(PATHS.classify("brainstorm.md")["class"] == PATHS.UNKNOWN,
          "and a word that merely CONTAINS a folder name does not match - "
          "'brainstorm' is not the Brain folder")

    print()
    print("5. Every operation's row is that agent's own rule")
    check(sorted(PATHS.OPERATIONS) == ["backup", "cleanup", "product-update",
                                       "repair", "uninstall"],
          "there are five, and each names an agent")
    for operation, rule in PATHS.OPERATIONS.items():
        check(rule["who"].startswith("HERON-"),
              "%s cites %s" % (operation, rule["who"]))
        check(len(rule["why"]) > 40, "and says why, in a sentence")
    # AND THE AGENTS REALLY BEHAVE THAT WAY.
    check(PATHS.OPERATIONS["backup"][PATHS.DERIVED] is False,
          "backup excludes derived...")
    backup = open(os.path.join(ROOT, "tools", "heron-backup.py"),
                  encoding="utf-8").read().lower()
    check("derived" in backup,
          "...and tools/heron-backup.py really talks about derived state")
    import heron_healing as HEA
    check(PATHS.OPERATIONS["repair"][PATHS.DERIVED] is True
          and PATHS.OPERATIONS["repair"][PATHS.DATA] is False,
          "repair covers derived and not data...")
    check(HEA.repair("docs/DECISIONS.md").get("refused")
          == "NOT_KNOWN_TO_BE_DERIVED",
          "...and HERON-OPS-HEA-006 really refuses a source of truth")
    check(HEA.repair("docs/agent-map.html").get("repaired") is True,
          "and really repairs a derived one")

    print()
    print("6. HERON-OPS-UPD-010 asks this agent")
    for name in ("Brain agents", "Core", "Cache", "Fragments", "MCP",
                 "Documentation", "Revit add-in"):
        check(UPD._classify(name) == PATHS.classify(name)["class"],
              "'%s' - same answer from both" % name)
    check(UPD.DATA is folders[PATHS.DATA]
          or UPD.DATA == folders[PATHS.DATA],
          "and its lists are this one's, re-exported rather than restated")
    update = open(os.path.join(ROOT, "brain", "heron_update.py"),
                  encoding="utf-8").read()
    check("PATHS.classify" in update, "it calls classify()")
    check('DATA = ("brain"' not in update,
          "and no longer carries its own copy of docs/06 s2")
    check("four places for it to drift" in update,
          "with the reason recorded where the copy used to be")

    print()
    print("7. Every failure the contract declares is named and reached")
    for path in ("", None, "   "):
        answer = PATHS.may("cleanup", path)
        reached.add(answer.get("refused"))
        check(answer.get("refused") == "NO_PATH", "%r is no path" % (path,))
    for operation in ("delete", "", None, "Backup ", "install"):
        if str(operation or "").strip().lower() in PATHS.OPERATIONS:
            continue
        answer = PATHS.may(operation, "Core/x")
        reached.add(answer.get("refused"))
        check(answer.get("refused") == "NOT_AN_OPERATION",
              "'%s' is not an operation" % operation)
    check("does not make up a sixth" in PATHS.may("delete", "Core/x")["why"],
          "and it says it will not invent one")
    check(PATHS.classify("")["class"] == PATHS.UNKNOWN,
          "while classify() of nothing is UNKNOWN rather than a refusal - "
          "it answers, it does not gate")
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-WSP-PTH-007.yaml"))
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
    print("PASS    one table, and the agents that need it ask")
    return 0


if __name__ == "__main__":
    sys.exit(main())
