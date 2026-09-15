# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-WSP-CRE-002
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Folder creation - the structure docs/06 s2 draws, and the five it does not
classify.

    python brain/heron_folders.py

WHAT IT IS FOR (docs/28, HERON-WSP-CRE-002)
--------------------------------------------
"Creates the structure." T1, risk MODIFY. The structure is docs/06 s2's
own tree, read from that document rather than retyped here.

THE THING THIS AGENT FOUND ON ITS FIRST RUN
---------------------------------------------
docs/06 s2 draws a tree of NINETEEN folders and then, immediately below
it, a table putting folders into product, data and derived. **The table
covers fourteen of them.** Five appear in the tree and in no class:

    RAG   Community   Configuration   Tests   Documentation

That is not cosmetic. The class is the only thing that decides whether a
product update may replace a folder wholesale (docs/07 s7 rule 6),
whether a cleanup may delete it outright, and whether a backup covers it.
`Configuration` is the one to look at first: if it is product, an update
replaces a practice's settings and their security policy with the
shipped defaults, and nothing in the specification currently says it is
not. Recorded as PROPOSALS F11.

SO AN UNCLASSIFIED FOLDER IS CREATED, AND NEVER WRITTEN INTO
--------------------------------------------------------------
This agent fails closed the way HERON-WSP-PTH-007 does - an unknown class
reads as DATA, because nothing can show it is not the user's. Making an
empty folder is safe whatever its class; what is not safe is any later
operation treating it as replaceable. So the five are created and named,
every time, in their own list.

AN EXISTING FOLDER IS LEFT, AND A FILE IN ITS PLACE IS REFUSED
----------------------------------------------------------------
Two different things that a creation step gets wrong together:

  a folder already there     fine, and left alone. Re-creating it is
                             not the danger; emptying it is, and this
                             does neither.
  a FILE with that name      refused. Something is already using the
                             name and making a folder over it either
                             fails or destroys it, depending on the
                             platform - and neither is a thing to find
                             out during an install.

A PARTIAL STRUCTURE THAT LOOKS COMPLETE IS THE FAILURE
--------------------------------------------------------
So the answer names every folder of the nineteen in exactly one list,
and the suite checks the three lists add back up to the tree. An install
that made sixteen folders and reported success is the shape of the
problem HERON-INS-HLT-009 exists for, one step earlier.
"""

import io
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

import heron_paths as PATHS                                    # noqa: E402

# Where the tree really is. Read, not retyped: a second copy of a
# nineteen-line list is a second copy to drift.
SOURCE = os.path.join("docs", "06-heron-platform.md")


def structure(root=None):
    """
    The folder names docs/06 s2 draws, in its order - or a refusal.

    Parsed from the document so this agent cannot disagree with it. The
    alternative is a list here that somebody updates in one place.
    """
    path = os.path.join(root or ROOT, SOURCE)
    try:
        with io.open(path, encoding="utf-8") as fh:
            text = fh.read()
    except IOError as failure:
        return None, ("%s could not be read (%s), and the structure is not "
                      "retyped here - a second copy of a nineteen-line list "
                      "is a second copy to drift."
                      % (SOURCE, type(failure).__name__))

    block = text.split("## 2. Workspace architecture")
    if len(block) < 2:
        return None, ("%s has no '## 2. Workspace architecture' section, so "
                      "the tree this agent builds cannot be read from the "
                      "document that owns it." % SOURCE)

    found = re.findall(r"(?m)^\+--\s*(\S+)\s*$", block[1][:1200])
    if not found:
        return None, ("the section is there but no `+-- Folder` lines were "
                      "found in it. The drawing changed shape, and guessing "
                      "the folders from prose is how a structure quietly "
                      "loses one.")
    return found, None


def plan(existing=None, is_file=None, root=None):
    """
    {create, exists, refused_names, unclassified, why} - or a refusal.

    Nothing is created. `existing` and `is_file` are handed in, the same
    shape HERON-INS-BRN-007 uses, because an agent that looks and then
    acts on what it saw is the shape of the mistake.
    """
    folders, why_not = structure(root)
    if folders is None:
        return {"created": False, "refused": "NO_STRUCTURE", "why": why_not}

    here = set(str(name).strip() for name in (existing or []))
    taken = is_file if callable(is_file) else (
        lambda name: name in set(is_file or []))

    create, already, refused, unclassified = [], [], [], []
    for name in folders:
        klass = PATHS.classify(name)["class"]
        entry = {"folder": name, "class": klass}

        if taken(name):
            refused.append(dict(entry, refused="A_FILE_HOLDS_THAT_NAME",
                                why="something is already using the name "
                                    "'%s'. Making a folder over a file "
                                    "either fails or destroys it depending "
                                    "on the platform, and neither is a "
                                    "thing to find out during an install."
                                    % name))
            continue

        if name in here:
            already.append(dict(entry, why="already there, and left alone. "
                                           "Re-creating it is not the "
                                           "danger; emptying it is, and "
                                           "this does neither."))
            continue

        create.append(entry)
        if klass == PATHS.UNKNOWN:
            unclassified.append(dict(entry, why="docs/06 s2 draws this "
                                                "folder and does not put it "
                                                "in any class. Read as DATA "
                                                "here, because nothing can "
                                                "show it is not the user's "
                                                "- see PROPOSALS F11."))

    landed = len(create) + len(already) + len(refused)
    return {
        "created": False, "create": create, "exists": already,
        "refused_names": refused, "unclassified": unclassified,
        "of": len(folders),
        "why": "%d folder(s) in docs/06 s2's tree: %d to create, %d already "
               "there, %d refused. %d of the ones to create have no class "
               "at all. Nothing was created."
               % (len(folders), len(create), len(already), len(refused),
                  len(unclassified)),
        "unjudged": [
            "NOTHING WAS CREATED. The list comes back and a caller makes "
            "the folders - and what is already there was HANDED IN, not "
            "looked up, because an agent that looks and then acts on what "
            "it saw is the shape of the mistake.",
            "EVERY FOLDER LANDS IN EXACTLY ONE LIST (%d of %d). An install "
            "that made sixteen of nineteen and reported success is the "
            "problem HERON-INS-HLT-009 exists for, one step earlier."
            % (landed, len(folders)),
            "%s"
            % ("RAG, Community, Configuration, Tests and Documentation are "
               "drawn by docs/06 s2 and classified by nothing. Making an "
               "empty folder is safe whatever its class; what is not safe "
               "is a later operation treating one as replaceable - and "
               "`Configuration` read as PRODUCT means an update replaces a "
               "practice's settings and security policy with the shipped "
               "defaults. PROPOSALS F11."
               if unclassified else
               "every folder to be created has a class."),
            "the structure was READ from %s rather than retyped, so this "
            "agent cannot disagree with the document that owns it."
            % SOURCE,
        ],
    }


def main(argv):
    print("FOLDER CREATION   the structure, and the five with no class")
    print("=" * 72)

    answer = plan(existing=["Core", "Brain", "Cache"],
                  is_file=["Documentation"])
    print("  %s" % answer["why"])

    print()
    print("  TO CREATE:")
    for entry in answer["create"]:
        print("    %-16s %s" % (entry["folder"], entry["class"]))

    print()
    print("  ALREADY THERE, LEFT ALONE:")
    for entry in answer["exists"]:
        print("    %-16s %s" % (entry["folder"], entry["class"]))

    print()
    print("  REFUSED:")
    for entry in answer["refused_names"]:
        print("    %-16s %s" % (entry["folder"], entry["refused"]))
        print("        %s" % entry["why"][:88])

    print()
    print("  NO CLASS IN docs/06 s2 AT ALL:")
    for entry in answer["unclassified"]:
        print("    %s" % entry["folder"])
    print()
    print("  %s" % answer["unjudged"][2][:280])
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
