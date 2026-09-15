# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-WSP-TPL-006
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test

"""
Templates - a template NAMES, it never CARRIES.

    python tests/test_templates.py

WHAT IT PROVES
  1. THE RULE IT IS BUILT ON IS REALLY IN docs/12 s97, read from that
     document rather than trusted: a .rvt, an .rfa, a family or project
     template is never uploaded, and the line is the FILE.

  2. A REVIT FILE IS REFUSED BY NAME ALONE - all four extensions, with
     nothing attached to them.

  3. A TEMPLATE CANNOT CARRY. Every container key is refused, and the
     code itself opens, reads and copies nothing.

  4. A RESERVED NAME IS REFUSED, and the reason is asserted against
     HERON-WSP-PTH-007 rather than described: the same leaf name
     classifies two different ways depending on what sits above it.

  5. EVERY ENTRY LANDS IN EXACTLY ONE LIST, and a template that half
     applies is refused rather than reported as a success.

  6. NOTHING IS CREATED, NOTHING IS COPIED, NOTHING IS LOOKED UP.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_templates as TPL                                  # noqa: E402
import heron_paths as PATHS                                    # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_templates.py"),
                    encoding="utf-8").read()
    # CODE ONLY. The docstring above it says these words on purpose, and a
    # word search that reads prose has passed five times in this repository
    # while proving nothing.
    code = whole.split("\nfrom __future__", 1)[1]

    def ask(**kw):
        answer = TPL.apply(**kw)
        if answer.get("refused"):
            reached.add(answer["refused"])
        for entry in (answer.get("refused_names") or []):
            reached.add(entry["refused"])
        return answer

    def only(answer, folder):
        for entry in (answer.get("refused_names") or []):
            if entry.get("folder") == folder:
                return entry
        return {}

    print("1. The rule is read from docs/12 s97, not trusted")
    security = io.open(os.path.join(ROOT, "docs",
                                    "12-security-and-permissions.md"),
                       encoding="utf-8").read()
    flat = " ".join(security.split())
    check("a family or project template is **never** uploaded" in flat,
          "docs/12 says a family or project template is never uploaded")
    check("the file, not the information" in flat,
          "and that the line is the FILE, not the information")
    check(sorted(TPL.REVIT_FILES) == [".rfa", ".rft", ".rte", ".rvt"],
          "the agent guards all four of the file kinds that names")

    print("\n2. A Revit file is refused by name alone")
    for extension in TPL.REVIT_FILES:
        named = "Office Standard%s" % extension
        answer = ask(template={"name": "T", "folders": [named]},
                     under=None, existing=None, is_file=None)
        entry = only(answer, named)
        check(entry.get("refused") == "A_REVIT_FILE_IS_NAMED",
              "'%s' is refused with nothing attached to it" % named)
    upper = ask(template={"name": "T", "folders": ["SHELL.RVT"]},
                under=None, existing=None, is_file=None)
    check(only(upper, "SHELL.RVT").get("refused") == "A_REVIT_FILE_IS_NAMED",
          "and in capitals too - an office names files either way")

    print("\n3. A template cannot carry")
    for key in TPL.CARRIES:
        answer = ask(template={"name": "T",
                               "folders": [{"folder": "Models", key: "x"}]},
                     under=None, existing=None, is_file=None)
        entry = only(answer, "Models")
        check(entry.get("refused") == "TEMPLATE_CARRIES_A_FILE",
              "an entry with a '%s' is refused" % key)
    check(len(TPL.CARRIES) >= 8,
          "and there are %d such keys, not one" % len(TPL.CARRIES))
    for forbidden in ("open(", "io.open", "shutil", "copyfile", ".read()",
                      "urlopen", "requests"):
        check(forbidden not in code,
              "the code never uses %s - there is nothing to carry WITH"
              % forbidden)

    print("\n4. A reserved name is refused, and the reason is asserted")
    # THE FINDING, checked against HERON-WSP-PTH-007 itself rather than
    # described: the same leaf, two classes, decided by what is above it.
    leaf = PATHS.classify("Cache")["class"]
    deep = PATHS.classify("Projects/Tower A/Cache")["class"]
    check(leaf == PATHS.DERIVED, "classify('Cache') is derived")
    check(deep == PATHS.DATA, "classify('Projects/Tower A/Cache') is data")
    check(leaf != deep,
          "so a project folder called Cache has two classes, and a cleanup "
          "and a backup would disagree about it")
    reserved = ask(template={"name": "T", "folders": ["Cache", "Backup",
                                                      "Brain", "Core"]},
                   under="Projects/Tower A", existing=None, is_file=None)
    for word in ("Cache", "Backup", "Brain", "Core"):
        check(only(reserved, word).get("refused") == "RESERVED_WORKSPACE_NAME",
              "'%s' is refused" % word)
    # DERIVED from the path table, so the two cannot drift apart.
    from_table = set(word for _klass, words in PATHS.FOLDERS for word in words)
    check(set(TPL.RESERVED) == from_table,
          "and the reserved list IS the path table's %d words, not a second "
          "copy to drift" % len(from_table))

    print("\n5. Every entry lands in exactly one list")
    mixed = ask(template={"name": "MEP", "folders":
                          ["Models", "Sheets", "Cache", "Std.rte",
                           {"folder": "Families", "source": "L:/x"},
                           {"why": "no name at all"}, "Locked"]},
                under="Projects/Tower A", existing=["Sheets"],
                is_file=["Locked"])
    landed = (len(mixed["create"]) + len(mixed["exists"])
              + len(mixed["refused_names"]))
    check(landed == mixed["of"] == 7,
          "7 entries in, 7 accounted for - %d create, %d exist, %d refused"
          % (len(mixed["create"]), len(mixed["exists"]),
             len(mixed["refused_names"])))
    check([row["folder"] for row in mixed["create"]] == ["Models"],
          "only Models is left to create")
    check(mixed["exists"][0]["folder"] == "Sheets",
          "Sheets is already there and is LEFT, not refused and not remade")
    check(only(mixed, "Locked").get("refused") == "A_FILE_HOLDS_THAT_NAME",
          "a file holding the name is refused")
    unnamed = [row for row in mixed["refused_names"]
               if row.get("refused") == "NOT_A_TEMPLATE"]
    check(len(unnamed) == 1 and "folder" not in unnamed[0],
          "an entry naming no folder is refused, not skipped")
    check(mixed["create"][0]["class"] == PATHS.DATA,
          "and what IS created classifies data, under Projects/Tower A")

    print("\n6. Nothing is created, copied, or looked up")
    check(mixed["created"] is False, "`created` is false")
    check(all(row["created"] is False for row in
              (mixed, reserved, upper)), "on every answer")
    check("os.mkdir" not in code and "makedirs" not in code,
          "the code makes no folder")
    check("os.listdir" not in code and "os.walk" not in code,
          "and looks nothing up - `existing` is HANDED IN")
    seen = {"asked": []}
    ask(template={"name": "T", "folders": ["Models"]}, under=None,
        existing=None, is_file=lambda name: seen["asked"].append(name))
    check(seen["asked"] == ["Models"],
          "is_file is asked, once per folder, and its answer is used")

    print("\n7. A template that cannot be applied is refused whole")
    for bad, why in ((None, "None is not a template"),
                     ("MEP", "a string is not a template"),
                     ({"folders": ["A"]}, "a template with no name"),
                     ({"name": "T"}, "a template naming no folders"),
                     ({"name": "T", "folders": []}, "an empty folder list")):
        answer = ask(template=bad, under=None, existing=None, is_file=None)
        check(answer.get("refused") == "NOT_A_TEMPLATE", why)
    for outside in ("Core", "Cache", "Packages"):
        answer = ask(template={"name": "T", "folders": ["Models"]},
                     under=outside, existing=None, is_file=None)
        check(answer.get("refused") == "WOULD_LAND_OUTSIDE_DATA",
              "landing under '%s' is refused - a template makes the user's "
              "work, never the product" % outside)
    unknown = ask(template={"name": "T", "folders": ["Models"]},
                  under="Z:/somewhere", existing=None, is_file=None)
    check(not unknown.get("refused"),
          "and an UNKNOWN landing is allowed - it fails closed toward the "
          "user's, the way HERON-WSP-PTH-007 does")

    print("\n8. What it does not judge, and every failure reached")
    check(len(mixed["unjudged"]) == 4, "four things are left unjudged")
    check(any("F12" in line for line in mixed["unjudged"]),
          "including that docs/00 s1115 ships 'Project templates' while "
          "docs/12 s97 says one is never uploaded - PROPOSALS F12")
    master = io.open(os.path.join(ROOT, "docs", "00-master-specification.md"),
                     encoding="utf-8").read()
    check("Project templates" in master,
          "and docs/00 really does list Project templates as a package")
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-WSP-TPL-006.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 6, "the contract declares 6 failures")
    for failure in named:
        check(failure in code, "the code names %s" % failure)
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
    print("PASS    a template names, it never carries")
    return 0


if __name__ == "__main__":
    sys.exit(main())
