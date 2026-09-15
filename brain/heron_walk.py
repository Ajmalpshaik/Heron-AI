# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-IMP-FIL-002
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
File discovery - the extension is what the NAME claims, and that is the
whole of what this agent will say.

    python brain/heron_walk.py

WHAT IT IS FOR (docs/28, HERON-IMP-FIL-002)
--------------------------------------------
"Walks the folder, identifies file types." T1, risk READ. Step 2 of
docs/00 s28's sixteen, and the first one that touches somebody else's
disk.

IT NAMES NO CATEGORY, AND THAT IS THE POINT OF THE ROW
--------------------------------------------------------
The very next agent in the register - HERON-IMP-CLS-003, "code,
documentation, config, metadata, asset" - is T2, a model call. If a
walk could sort files into those five by extension, that row would not
need a model.

So the type this agent reports is the EXTENSION, lower case, and
nothing else. `.py` is not "code". A `.txt` can hold C#, a `.md` can
hold a licence, and `report.pdf.exe` is neither. What the file IS gets
decided by the agent whose row says so.

THE TWO NAMES THAT CARRY NO EXTENSION ARE NOT THE SAME CASE
-------------------------------------------------------------
`os.path.splitext` gives an empty extension twice over, for two
different reasons, and a manifest that calls both "unknown" hides one
of them:

    Makefile      the name carries no dot at all
    .gitignore    the whole name IS the dot-suffix - splitext reads it
                  as the name, not as an extension

Both come back in `unnamed` with which case they are. Neither is
guessed at.

ONLY THE LAST SUFFIX IS AN EXTENSION
--------------------------------------
`archive.tar.gz` reads as `.gz`. Splitting further means deciding that
`.tar` is a type and `.2026` in `backup.2026.zip` is not, and that
table does not exist anywhere in this project. Stated rather than
silently applied.

os.walk THROWS FOLDERS AWAY WITHOUT SAYING SO
-----------------------------------------------
Its default `onerror` is None, which means a folder that cannot be
listed - permissions, a broken mount, a path too long for Windows - is
skipped and the walk returns as though it were empty. That is Golden
Rule 14 exactly, so `onerror` is supplied and those folders come back
in `unreadable`. A manifest built on a silent skip is a manifest that
under-reports the import.

THE ORDER IS SORTED, BECAUSE THE IMPORT IS RESUMABLE
------------------------------------------------------
docs/10 s5 constraint 5: "a folder with thousands of files will fail
partway through at some point." Filesystem order is arbitrary and not
stable between runs, so resuming at file 4,000 only means something if
run two lists the folder the same way run one did. Entries are sorted.

A LINK IS NOT FOLLOWED
------------------------
Constraint 1 is that import never modifies the source folder, and the
matching half is that it never leaves it either. A symlink can point
anywhere on the machine - a home directory, a client project, the C
drive - and following one would put files in the manifest that the
user never offered. They are reported in `not_followed` and never
opened.

NOTHING IS SKIPPED
--------------------
No `.git`, no `node_modules`, no `bin`. A skip list is a guess about
somebody else's folder and this agent has none, so the counts are the
real counts. Whether the pipeline SHOULD filter version-control and
build folders is a real question with a real cost behind it, and it is
recorded as PROPOSALS F25 rather than answered here.

NOTHING IS OPENED AND NOTHING IS WRITTEN
------------------------------------------
Names, sizes and the shape of the tree. No file content is read, which
is why this row is READ and why it can run over a client folder
without the credential question APR-014 has to ask.
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_release as RELEASE  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# HERON-RPT-RED-003's list, borrowed rather than retyped. A Revit model
# is somebody's building - HERON-IMP-APR-014 refuses one at the manifest
# door, and this agent keeps it out of the manifest in the first place.
NEVER_IMPORTED = RELEASE.NEVER_LEAVES

# The two ways a name can carry no extension. Both are reported; neither
# is called "unknown".
NO_DOT = "the name carries no dot at all"
ALL_DOT = "the whole name is the dot-suffix, so splitext reads it as the name"


def extension_of(name):
    """The last suffix, lower case. `archive.tar.gz` reads as `.gz`."""
    return os.path.splitext(str(name))[1].lower()


def _why_unnamed(name):
    """Which of the two no-extension cases a name is."""
    return ALL_DOT if str(name).startswith(".") else NO_DOT


def walk(folder):
    """
    {walked, files, unnamed, never_imported, not_followed, unreadable} -
    or a refusal. Nothing is opened and nothing is written.
    """
    if not str(folder or "").strip():
        return {"walked": False, "refused": "NOTHING_TO_WALK",
                "why": "no folder was handed in. `Import this folder` "
                       "needs the folder."}

    folder = os.path.abspath(str(folder).strip())
    if not os.path.isdir(folder):
        return {"walked": False, "refused": "NOT_A_FOLDER",
                "why": "%s is %s. A walk needs a folder."
                       % (folder,
                          "a file" if os.path.isfile(folder)
                          else "not there")}

    files, unnamed, refused, links, unreadable = [], [], [], [], []

    def _cannot(error):
        unreadable.append({
            "at": os.path.relpath(getattr(error, "filename", folder), folder),
            "why": "%s. os.walk would have skipped this folder without "
                   "saying so." % (getattr(error, "strerror", None)
                                   or error.__class__.__name__)})

    for here, folders, names in os.walk(folder, onerror=_cannot,
                                        followlinks=False):
        # Sorted in place, so the walk descends in a stable order and a
        # resumed import lists the folder the way the first run did.
        folders.sort()
        for name in sorted(folders):
            full = os.path.join(here, name)
            if os.path.islink(full):
                links.append({"name": name,
                              "at": os.path.relpath(full, folder),
                              "is": "folder",
                              "why": "a link leads out of the folder being "
                                     "imported, and the folder is what was "
                                     "offered"})

        for name in sorted(names):
            full = os.path.join(here, name)
            at = os.path.relpath(full, folder)
            if os.path.islink(full):
                links.append({"name": name, "at": at, "is": "file",
                              "why": "a link leads out of the folder being "
                                     "imported, and the folder is what was "
                                     "offered"})
                continue

            suffix = extension_of(name)
            if suffix in NEVER_IMPORTED:
                refused.append({"name": name, "at": at, "extension": suffix,
                                "why": "a Revit model is somebody's "
                                       "building, not knowledge"})
                continue

            try:
                size = os.path.getsize(full)
            except OSError as error:
                unreadable.append({
                    "at": at,
                    "why": "%s. It was listed and then could not be "
                           "measured." % (error.strerror
                                          or error.__class__.__name__)})
                continue

            card = {"name": name, "at": at, "extension": suffix,
                    "bytes": size}
            files.append(card)
            if not suffix:
                card["because"] = _why_unnamed(name)
                unnamed.append(card)

    types = {}
    for card in files:
        types[card["extension"]] = types.get(card["extension"], 0) + 1

    of = len(files) + len(refused) + len(links) + len(unreadable)
    return {
        "walked": True,
        "root": folder,
        "of": of,
        "files": files,
        "types": types,
        "unnamed": unnamed,
        "never_imported": refused,
        "not_followed": links,
        "unreadable": unreadable,
        "why": "%d entries under %s: %d to carry forward across %d %s, "
               "%d Revit %s kept out, %d %s not followed, %d could not be "
               "read."
               % (of, folder, len(files), len(types),
                  "extension" if len(types) == 1 else "extensions",
                  len(refused), "model" if len(refused) == 1 else "models",
                  len(links), "link" if len(links) == 1 else "links",
                  len(unreadable)),
        "unjudged": [
            "WHAT ANY OF THESE FILES IS. The extension is what the NAME "
            "claims and nothing was opened to check it. Code, "
            "documentation, config, metadata or asset is "
            "HERON-IMP-CLS-003's row, and that row is T2 because the "
            "question needs more than a suffix.",
            ("%d %s carry no extension, which is two cases and not one: %s. "
             "Neither was guessed at."
             % (len(unnamed), "name" if len(unnamed) == 1 else "names",
                "; ".join(sorted(set(card["because"]
                                     for card in unnamed))))
             if unnamed else
             "every name carried an extension, so the two no-extension "
             "cases did not arise."),
            "ONLY THE LAST SUFFIX WAS READ. `archive.tar.gz` is `.gz` "
            "here. Splitting further needs a table saying `.tar` is a "
            "type and `.2026` is not, and no such table exists in this "
            "project.",
            ("%d %s reported and not followed. A link can point anywhere "
             "on the machine, and the folder is what was offered."
             % (len(links), "link was" if len(links) == 1 else "links were")
             if links else
             "no links were found, so nothing pointed out of the folder."),
            ("%d %s could not be read and %s named rather than dropped - "
             "os.walk's default is to skip them without saying so (GR 14)."
             % (len(unreadable),
                "entry" if len(unreadable) == 1 else "entries",
                "is" if len(unreadable) == 1 else "are")
             if unreadable else
             "everything offered could be listed and measured."),
            "NOTHING WAS SKIPPED. No .git, no node_modules, no build "
            "folder was filtered out - a skip list is a guess about "
            "somebody else's folder. Whether the pipeline should filter "
            "them is PROPOSALS F25.",
            "NOTHING WAS OPENED AND NOTHING WAS WRITTEN. Names, sizes and "
            "the tree. The source folder is untouched, which is docs/10 "
            "s5 constraint 1.",
        ],
    }


def main(argv):
    import shutil
    import tempfile

    print("FILE DISCOVERY   the extension is what the name claims")
    print("=" * 72)
    print("\nnever imported: %s" % ", ".join(NEVER_IMPORTED))
    for name in ("CountDucts.py", "archive.tar.gz", "Makefile",
                 ".gitignore", "REPORT.MD"):
        print("  %-16s -> %r" % (name, extension_of(name)))

    where = argv[0] if argv else None
    made = None
    if not where:
        made = where = tempfile.mkdtemp(prefix="heron-walk-")
        os.makedirs(os.path.join(where, "tools", "ducts"))
        for at, text in (("README.md", "# AJ-Tools"),
                         ("LICENSE", "MIT"),
                         (".gitignore", "*.pyc"),
                         ("notes.tar.gz", "x"),
                         (os.path.join("tools", "CountDucts.py"), "pass"),
                         (os.path.join("tools", "TagSheet.py"), "pass"),
                         (os.path.join("tools", "config.json"), "{}"),
                         (os.path.join("tools", "Office.rvt"), "model"),
                         (os.path.join("tools", "ducts", "Duct.rfa"), "fam")):
            with open(os.path.join(where, at), "w") as handle:
                handle.write(text)
        try:
            os.symlink(os.path.expanduser("~"),
                       os.path.join(where, "home-link"))
        except (OSError, AttributeError, NotImplementedError):
            pass

    try:
        answer = walk(where)
        print("\n%s" % answer["why"])
        print("\ntypes")
        for suffix, count in sorted(answer["types"].items()):
            print("  %-10s %d" % (suffix or "(none)", count))
        print("")
        for card in answer["unnamed"]:
            print("  UNNAMED  %-16s %s" % (card["name"], card["because"]))
        for card in answer["never_imported"]:
            print("  KEPT OUT %-16s %s" % (card["name"], card["why"]))
        for card in answer["not_followed"]:
            print("  LINK     %-16s %s" % (card["name"], card["why"][:40]))
        for card in answer["unreadable"]:
            print("  UNREAD   %-16s %s" % (card["at"], card["why"][:40]))

        print("\nrefused")
        for bad in (None, "   ", os.path.join(ROOT, "brain", "heron_walk.py"),
                    os.path.join(ROOT, "no-such-folder")):
            said = walk(bad)
            print("  %-18s %s" % (said["refused"], said["why"][:46]))

        print("\nwhat this agent does not judge")
        for line in answer["unjudged"]:
            print("  - %s" % line)
    finally:
        if made:
            shutil.rmtree(made, ignore_errors=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
