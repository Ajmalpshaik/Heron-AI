# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-WSP-CRE-002
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test

"""
Folder creation - the structure docs/06 s2 draws, and the five it does not
classify.

    python tests/test_folders.py

WHAT IT PROVES
  1. THE STRUCTURE IS READ FROM docs/06 s2, not retyped - and it really
     is the tree that document draws.

  2. FIVE OF THE NINETEEN HAVE NO CLASS, which is a fact about the
     specification and is asserted against the document itself.

  3. EVERY FOLDER LANDS IN EXACTLY ONE LIST. A partial structure that
     looks complete is the failure this guards.

  4. AN EXISTING FOLDER IS LEFT; A FILE HOLDING THE NAME IS REFUSED.
     Two different things a creation step gets wrong together.

  5. NOTHING IS CREATED, AND NOTHING IS LOOKED UP.

  6. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_folders as CRE                                    # noqa: E402
import heron_paths as PATHS                                    # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    source = io.open(os.path.join(ROOT, "brain", "heron_folders.py"),
                     encoding="utf-8").read()

    def ask(**kw):
        answer = CRE.plan(**kw)
        if answer.get("refused"):
            reached.add(answer["refused"])
        for entry in (answer.get("refused_names") or []):
            reached.add(entry["refused"])
        return answer

    print("1. The structure is read from docs/06 s2")
    folders, why_not = CRE.structure()
    check(folders and not why_not, "the tree parses out of the document")
    check(len(folders) == 19, "it has 19 folders, not a number typed here")
    # DERIVED FROM THE DOCUMENT, independently of the agent's own parser.
    doc = io.open(os.path.join(ROOT, "docs", "06-heron-platform.md"),
                  encoding="utf-8").read()
    drawn = doc.split("## 2. Workspace architecture")[1][:1200]
    check(all(("+-- " + name) in drawn for name in folders),
          "and every name it returns really is drawn in that section")
    check(folders[0] == "Core" and folders[-1] == "Backup",
          "in the document's own order, first to last")
    for name in ("Core", "Brain", "Cache", "Configuration"):
        check(name in folders, "'%s' is in the tree" % name)
    check("SOURCE = os.path.join" in source and "06-heron-platform" in source,
          "the module names the document it reads")

    print()
    print("2. Five of the nineteen have no class")
    answer = ask()
    unclassified = sorted(e["folder"] for e in answer["unclassified"])
    check(unclassified == ["Community", "Configuration", "Documentation",
                           "RAG", "Tests"],
          "exactly these five: %s" % ", ".join(unclassified))
    # ASSERTED AGAINST THE DOCUMENT, not against the agent.
    # WORD BOUNDARIES, because "RAG" is inside "Fragments" and a substring
    # check said the class table mentions RAG when it does not. The test
    # caught it; the same slip in the agent would have hidden a folder.
    import re
    table = doc.split("| **Product** |")[1][:600]
    for name in unclassified:
        check(not re.search(r"\b" + name + r"\b", table, re.I),
              "'%s' really is absent from docs/06 s2's class table" % name)
    for name in ("Core", "Brain", "Cache"):
        check(re.search(r"\b" + name + r"\b", table, re.I),
              "while '%s' is in it" % name)
    check(re.search(r"\bFragments\b", table, re.I)
          and not re.search(r"\bRAG\b", table, re.I),
          "and Fragments being there is exactly why the boundary matters - "
          "'RAG' is inside 'F-RAG-ments'")
    check(all(e["class"] == PATHS.UNKNOWN for e in answer["unclassified"]),
          "and each is reported UNKNOWN rather than guessed")
    check(any("F11" in note for note in answer["unjudged"]),
          "the answer points at the finding")
    check(any("Configuration" in note and "shipped defaults" in note
              for note in answer["unjudged"]),
          "and names what Configuration read as PRODUCT would cost")

    print()
    print("3. Every folder lands in exactly one list")
    answer = ask(existing=["Core", "Brain"], is_file=["Documentation"])
    landed = ([e["folder"] for e in answer["create"]]
              + [e["folder"] for e in answer["exists"]]
              + [e["folder"] for e in answer["refused_names"]])
    check(sorted(landed) == sorted(folders),
          "all 19, none twice and none missing")
    check(len(landed) == len(set(landed)), "and none in two lists")
    check(answer["of"] == 19, "`of` says how many there are to check against")
    check(any("EXACTLY ONE LIST" in note for note in answer["unjudged"]),
          "and the answer says the split is exhaustive")
    check(any("sixteen of nineteen" in note for note in answer["unjudged"]),
          "naming the failure it guards")

    print()
    print("4. An existing folder is left; a file holding the name is refused")
    answer = ask(existing=["Core", "Brain", "Cache"])
    check([e["folder"] for e in answer["exists"]] == ["Core", "Brain",
                                                      "Cache"],
          "the three present ones are left alone")
    check("Re-creating it is not the danger" in answer["exists"][0]["why"],
          "saying re-creating is not the danger")
    check("emptying it is" in answer["exists"][0]["why"],
          "and what is")
    answer = ask(is_file=["Logs"])
    entry = answer["refused_names"][0]
    check(entry["refused"] == "A_FILE_HOLDS_THAT_NAME",
          "a file holding a folder name is refused")
    check("depending on the platform" in entry["why"],
          "saying the outcome differs by platform")
    check("during an install" in entry["why"],
          "and when you would find out")
    check("Logs" not in [e["folder"] for e in answer["create"]],
          "and it is not also proposed for creation")
    check(ask(is_file=lambda n: n == "Tests")["refused_names"][0]["folder"]
          == "Tests", "a READER is accepted in place of a list")

    print()
    print("5. Nothing is created, and nothing is looked up")
    agent_code = source.split('"""', 2)[2].split("def main(")[0]
    for word in ("os.makedirs", "os.mkdir", "mkdir", "shutil", "os.remove",
                 "os.listdir", "os.walk", "glob", "subprocess"):
        check(word not in agent_code,
              "the agent has no %s" % word)
    check(ask()["created"] is False, "`created` is False")
    check(any("NOTHING WAS CREATED" in note for note in ask()["unjudged"]),
          "and the answer says so")
    check(any("HANDED IN, not looked up" in note
              for note in ask()["unjudged"]),
          "and that what exists was handed in")
    # THE ONE FILE IT DOES OPEN IS THE DOCUMENT, WHICH IS THE POINT.
    check("io.open" in agent_code and "06-heron-platform" in agent_code,
          "the one thing it reads is the document that owns the structure - "
          "which is why it cannot disagree with it")

    print()
    print("6. Every failure the contract declares is named and reached")
    answer = CRE.plan(root="/nowhere-at-all")
    reached.add(answer.get("refused"))
    check(answer.get("refused") == "NO_STRUCTURE",
          "with the document missing, it refuses")
    check("not retyped here" in answer["why"],
          "and says why it does not fall back to a copy")
    folders_gone, why_not = CRE.structure(root="/nowhere-at-all")
    check(folders_gone is None and "could not be read" in why_not,
          "structure() says the same, on its own")
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-WSP-CRE-002.yaml"))
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
    print("PASS    read from the document, and five folders have no class")
    return 0


if __name__ == "__main__":
    sys.exit(main())
