# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-WSP-PTH-007
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Paths - product, data and derived, in one place instead of five.

    python brain/heron_paths.py

WHAT IT IS FOR (docs/28, HERON-WSP-PTH-007)
--------------------------------------------
"Product / data / derived separation enforced in code." T1. The register
says ENFORCED, not described, which is the difference between a table in a
document and a function other agents ask.

WHY THIS EXISTS AT ALL
------------------------
docs/06 s2 splits the workspace three ways and four agents built before
this one each carried their own copy of that split: HERON-OPS-UPD-010
refusing a product update that touches data, HERON-INS-CFG-006 splitting
machine from portable, HERON-INS-BRN-007 refusing to rebuild a knowledge
store, HERON-INS-RAG-008 rebuilding an index freely. Four copies of one
rule is four places for it to drift, and the drift would be silent - each
agent would still pass its own tests.

So the table lives here and they ask. HERON-OPS-UPD-010 was changed to do
that in the same commit, which is the only way to tell whether one place
is really enough.

THE THREE CLASSES, AND THE ONE QUESTION EACH ANSWERS
------------------------------------------------------
  PRODUCT   Core, Agents, Revit, MCP, Packages. Replaced wholesale on
            update; the user never edits it. Losing it costs a download.
  DATA      Brain, Skills, Fragments, Memory, Projects, Company, Logs,
            Backup. Belongs to the user and must survive every update,
            uninstall and reinstall. Losing it loses something nobody can
            regenerate.
  DERIVED   Cache, vector indexes. Safe to delete at any time, because
            something rebuilds them.

CLASSIFICATION SAYS WHAT IT KNOWS; PERMISSION ERRS TOWARD PROTECTION
----------------------------------------------------------------------
Those are two different jobs and merging them is how an unknown folder
gets treated as safe. `classify()` answers UNKNOWN when it does not
recognise a path, honestly - docs/06 s2 names twenty folders and a
workspace has more than twenty things in it.

`may()` reads UNKNOWN as DATA. Not because it is, but because nothing can
show it is not, and the two wrong answers are not the same size: treating
product as data delays an update, and treating data as product destroys a
modeller's fragment library. The asymmetry is the whole reason to have a
default at all.

DATA IS TESTED FIRST
----------------------
"Brain agents" carries a word from both lists. The order is not
arbitrary - it is the same asymmetry one level down.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

PRODUCT = "product"
DATA = "data"
DERIVED = "derived"
UNKNOWN = "unknown"

# docs/06 s2's own three lists. DATA is tested first: see the docstring.
FOLDERS = (
    (DATA, ("brain", "skills", "fragments", "memory", "projects", "company",
            "logs", "backup")),
    (DERIVED, ("cache", "index", "indexes", "vector")),
    (PRODUCT, ("core", "agents", "revit", "mcp", "packages")),
)

MEANING = {
    PRODUCT: "replaced wholesale on update; the user never edits it. "
             "Losing it costs a download.",
    DATA: "belongs to the user and must survive every update, uninstall "
          "and reinstall. Losing it loses something nobody can regenerate.",
    DERIVED: "safe to delete at any time, because something rebuilds it.",
    UNKNOWN: "not one of the twenty folders docs/06 s2 names.",
}

# What each operation may touch. The five are the operations agents in
# this repository actually perform, and each row is that agent's rule
# rather than a general policy invented here.
OPERATIONS = {
    "product-update": {
        PRODUCT: True, DERIVED: True, DATA: False,
        "who": "HERON-OPS-UPD-010",
        "why": "docs/07 s7 rule 6: never touch the data class during a "
               "product update.",
    },
    "uninstall": {
        PRODUCT: True, DERIVED: True, DATA: False,
        "who": "HERON-INS-PKG-012",
        "why": "docs/06 s2: data must survive every update, uninstall and "
               "reinstall - so uninstalling cleanly means the product goes "
               "and the data stays.",
    },
    "repair": {
        PRODUCT: False, DERIVED: True, DATA: False,
        "who": "HERON-OPS-HEA-006",
        "why": "derived state is repaired freely because a generator "
               "rebuilds it; everything else is proposed.",
    },
    "cleanup": {
        PRODUCT: False, DERIVED: True, DATA: False,
        "who": "HERON-WSP-CLN-009",
        "why": "the register row says archives, never deletes - and the "
               "only class it may remove outright is the one something "
               "rebuilds.",
    },
    "backup": {
        PRODUCT: False, DERIVED: False, DATA: True,
        "who": "HERON-WSP-BAK-010",
        "why": "the register row says it backs up the data class and NOT "
               "the derived index - backing up something a generator "
               "rebuilds is paying to store a copy of a command.",
    },
}


def classify(path):
    """
    {klass, why} - product, data, derived, or UNKNOWN.

    Honest about not knowing. docs/06 s2 names twenty folders and a
    workspace has more than twenty things in it, so a fourth answer is
    the truthful one rather than a gap.
    """
    name = str(path or "").strip().lower()
    if not name:
        return {"class": UNKNOWN, "matched": None,
                "why": "nothing was named, so nothing can be classified."}
    # Tokenised on every separator a real name uses, so this answers for a
    # PATH ("Brain/global.db") and for a component NAME ("Brain agents")
    # alike - HERON-OPS-UPD-010 asks with the second kind.
    for character in ("\\", "-", "_", " ", ".", ","):
        name = name.replace(character, "/")
    words_here = [piece for piece in name.split("/") if piece]
    for klass, words in FOLDERS:
        for word in words:
            if word in words_here:
                return {"class": klass, "matched": word,
                        "why": "'%s' is %s: %s" % (word, klass,
                                                   MEANING[klass])}
    return {"class": UNKNOWN, "matched": None,
            "why": "%s matches none of the twenty folders docs/06 s2 names. "
                   "That is the truthful answer, not a gap - a workspace "
                   "has more than twenty things in it." % path}


def may(operation, path):
    """
    {allowed, class, why} - may this operation touch this path?

    UNKNOWN is read as DATA here and only here. Not because it is, but
    because nothing can show it is not, and treating data as product
    destroys a fragment library while treating product as data delays an
    update.
    """
    rule = OPERATIONS.get(str(operation or "").strip().lower())
    if rule is None:
        return {"allowed": False, "refused": "NOT_AN_OPERATION",
                "why": "'%s' is not one of %s. Each of those is an agent's "
                       "own rule rather than a policy invented here, and "
                       "this agent does not make up a sixth."
                       % (operation, ", ".join(sorted(OPERATIONS)))}

    found = classify(path)
    if not str(path or "").strip():
        return {"allowed": False, "refused": "NO_PATH",
                "why": "no path was given, so there is nothing to decide "
                       "about."}

    klass = found["class"]
    read_as = DATA if klass == UNKNOWN else klass
    allowed = bool(rule[read_as])
    return {
        "allowed": allowed, "class": klass, "read_as": read_as,
        "rule": rule["who"],
        "why": "%s may%s touch %s. %s %s"
               % (operation, "" if allowed else " NOT", read_as, rule["why"],
                  "" if klass != UNKNOWN else
                  "%s is UNKNOWN and is read as data: nothing can show it is "
                  "not the user's, and treating data as product destroys a "
                  "fragment library while treating product as data delays an "
                  "update." % path),
    }


def main(argv):
    print("PATHS   product, data and derived, in one place instead of five")
    print("=" * 72)

    paths = ["Core/Heron.dll", "Fragments/duct-sizing", "Cache/vectors.db",
             "Brain/global.db", "Packages/mep-pack", "Logs/audit.jsonl",
             "Documentation/readme.md"]
    for path in paths:
        found = classify(path)
        print("  %-26s %-9s %s" % (path, found["class"],
                                   found["why"][:40]))

    print()
    print("  And what each operation may touch:")
    print("  %-18s %s" % ("", "  ".join("%-9s" % path.split("/")[0]
                                        for path in paths[:5])))
    for operation in sorted(OPERATIONS):
        row = ["%-9s" % ("yes" if may(operation, path)["allowed"] else "NO")
               for path in paths[:5]]
        print("  %-18s %s" % (operation, "  ".join(row)))

    print()
    print("  An unknown path is read as DATA, and only for permission:")
    found = classify("Documentation/readme.md")
    answer = may("cleanup", "Documentation/readme.md")
    print("    classify -> %s    may(cleanup) reads it as -> %s"
          % (found["class"], answer["read_as"]))
    print("    %s" % answer["why"][-150:])

    print()
    print("  Four agents each carried their own copy of docs/06 s2 before")
    print("  this. Four copies of one rule is four places for it to drift,")
    print("  and the drift would be silent - each would still pass its own")
    print("  tests. HERON-OPS-UPD-010 asks this one now.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
