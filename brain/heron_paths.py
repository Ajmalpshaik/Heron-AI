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

THERE IS ALREADY A PLATFORM HALF, AND IT ANSWERS A DIFFERENT QUESTION
-----------------------------------------------------------------------
`platform/Heron.Core/HeronPaths.cs` carries the same agent id and was
built at Step 1. **It was found by running tools/agent-count.py after
this module was written, not before** - the search that preceded it
looked at brain, mcp, revit and tools and missed platform, which is the
same class of mistake as a grep that finds nothing because the pattern
cannot see what is there.

They are not duplicates, and the split is worth stating rather than
assuming:

  HeronPaths.cs   WHERE things live - the three root directories, built
                  for the add-in and the .NET side. "Nothing else may
                  build a Heron path."
  this module     WHICH CLASS a name belongs to, and which operation may
                  touch it. Built for the agents, which reason about
                  names long before anything opens a file.

What they must agree on is the meaning of the three words, and the suite
asserts that by reading the C# file's own definitions - because two
implementations of one agent that disagree about what DATA means is
exactly the drift this module exists to prevent, one layer up.

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
recognise a path, honestly - a workspace has more in it than any
document lists.

AND THERE ARE TWO KINDS OF UNKNOWN, WHICH THIS SAID WERE ONE
--------------------------------------------------------------
Until 2026-09-21 every message here said "the twenty folders docs/06 s2
names", and it is not twenty and they are not all named. MEASURED:

    19   folders docs/06 s2 NAMES, in its own tree
    14   of those its table gives a CLASS to
     5   it names and never classifies - Community, Configuration,
         Documentation, RAG, Tests
    17   words this module matches on: those 14, plus index, indexes
         and vector as its reading of the table's "vector indexes"

So all five of those come back UNKNOWN, and the sentence told the reader
they were not in docs/06 at all. They are. What is missing is a CLASS
for them, and that gap is the document's - Q-13 is the open question
about this whole split - so this module names the gap rather than
inventing five answers the document declined to give. `main()` below
demonstrated the bug as a feature, printing Documentation as an unknown
path; it now says which kind of unknown it is.

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

# NAMED BY docs/06 s2 AND GIVEN NO CLASS BY IT. Not an oversight here: the
# document's own table stops at fourteen of the nineteen it draws, and
# choosing a class for these five is Q-13's to settle, not this module's.
# They are listed so the refusal can say WHICH kind of unknown it is -
# "docs/06 does not name it" and "docs/06 names it and does not classify
# it" send a reader to two different places.
UNCLASSIFIED = ("community", "configuration", "documentation", "rag", "tests")

MEANING = {
    PRODUCT: "replaced wholesale on update; the user never edits it. "
             "Losing it costs a download.",
    DATA: "belongs to the user and must survive every update, uninstall "
          "and reinstall. Losing it loses something nobody can regenerate.",
    DERIVED: "safe to delete at any time, because something rebuilds it.",
    UNKNOWN: "docs/06 s2 gives it no class, either because it does not "
             "name it at all or because it names it and stops there.",
}

# Whether a class is IN SCOPE for an operation - and what being in scope
# MEANS differs per operation, which is why each row carries its own
# `why` rather than sharing a verb. A product update WRITES, an uninstall
# and a cleanup REMOVE, a repair REGENERATES, a backup COPIES. Flattening
# those into one verb would make backup's row read as permission to
# delete the data class.
#
# The five are the operations agents in this repository actually perform,
# and each row is that agent's own rule rather than a policy invented here.
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

    Honest about not knowing, and honest about WHICH not-knowing it is.
    A workspace has more in it than any document lists, so a fourth
    answer is the truthful one rather than a gap - but five of the
    folders docs/06 s2 draws in its own tree are never given a class by
    it, and saying those are "not in docs/06" sends a reader looking in
    the wrong place. `unnamed` in the answer tells the two apart.
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
    named_unclassified = [w for w in UNCLASSIFIED if w in words_here]
    if named_unclassified:
        return {"class": UNKNOWN, "matched": named_unclassified[0],
                "unnamed": False,
                "why": "'%s' IS one of the nineteen folders docs/06 s2 draws, "
                       "and is one of the five it never gives a class to - "
                       "%s. That is a gap in the document rather than in this "
                       "table, and choosing a class for them is Q-13's to "
                       "settle, so nothing is invented here."
                       % (named_unclassified[0], ", ".join(UNCLASSIFIED))}
    return {"class": UNKNOWN, "matched": None, "unnamed": True,
            "why": "%s matches none of the folders docs/06 s2 classifies, "
                   "and is not one of the five it names without classifying. "
                   "That is the truthful answer, not a gap - a workspace has "
                   "more in it than any document lists." % path}


def may(operation, path):
    """
    {allowed, class, why} - is this class in scope for this operation?

    In scope means different things per operation - write, remove,
    regenerate, copy - and the `why` says which. A caller reading only
    `allowed` for `backup` would have permission to COPY, never to
    delete.

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
    print("  An unknown path is read as DATA, and only for permission -")
    print("  and the TWO KINDS of unknown are told apart:")
    for path in ("Documentation/readme.md", "Sketches/idea.txt"):
        found = classify(path)
        answer = may("cleanup", path)
        print("    %-24s %-8s named by docs/06: %-5s reads as %s"
              % (path, found["class"],
                 "no" if found.get("unnamed") else "YES", answer["read_as"]))
    print()
    print("    Documentation is one of the FIVE docs/06 s2 draws and never")
    print("    classifies - %s." % ", ".join(UNCLASSIFIED))
    print("    Saying it is 'not in docs/06' sent a reader to the wrong page.")

    print()
    print("  Four agents each carried their own copy of docs/06 s2 before")
    print("  this. Four copies of one rule is four places for it to drift,")
    print("  and the drift would be silent - each would still pass its own")
    print("  tests. HERON-OPS-UPD-010 asks this one now.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
