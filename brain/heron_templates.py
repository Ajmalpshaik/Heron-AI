# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-WSP-TPL-006
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Templates - a template NAMES, it never CARRIES.

    python brain/heron_templates.py

WHAT IT IS FOR (docs/28, HERON-WSP-TPL-006)
--------------------------------------------
"Workspace and project templates." T1, risk MODIFY. A template is a
named shape a new project starts from: these folders, under this place,
with these names. It is applied by a caller; this agent only says what
applying it would do.

THE WORD MEANS TWO OPPOSITE THINGS
------------------------------------
In a Revit practice "template" means the `.rte` - the project template
file the office starts every job from, and the `.rft` family templates
beside it. In this specification it also means Heron's own thing: a
saved shape of folders. The two carry opposite rules.

    Heron's template       a list of names. Heron writes it, reads it,
                           ships it, and it is a few hundred bytes.

    a Revit template       docs/12 s97: "a .rvt, an .rfa, a family or
                           project template is NEVER uploaded". The line
                           is drawn at THE FILE, not the information.

An agent that handles "templates" and does not know which one it has is
the one that puts an office's `.rte` somewhere it should never go.

SO THE RULE IS STRUCTURAL, NOT REMEMBERED
-------------------------------------------
A template entry is a NAME and a reason. It is never a file body and
never a path to one. Then there is nothing inside a template that COULD
be uploaded, and docs/12 s97 holds because there is nothing to break it
with - rather than because somebody remembered the rule at the right
moment.

Both halves are refused, and separately, because they are different
mistakes:

    `Standard.rte` named        A_REVIT_FILE_IS_NAMED. Even as a bare
                                name. A template that names the office
                                .rte is one `open()` away from carrying
                                it.
    an entry with a body        TEMPLATE_CARRIES_A_FILE. `bytes`,
                                `source`, `path` - anything that makes
                                the template a container.

AND ONE THING FOUND WHILE BUILDING IT
---------------------------------------
docs/00 s1115 lists "Project templates" as a marketplace package, and
docs/00d s325 lists "company templates". A marketplace package is, by
definition, a file that leaves. docs/12 s97 says a project template
never does. The two lines are about different senses of the word - or
they are a contradiction - and nothing written says which. Recorded as
PROPOSALS F12. This agent takes the narrow reading: what may be shipped
is a list of names, never a `.rte`.

A RESERVED NAME IS REFUSED, AND THIS IS THE SHARP ONE
-------------------------------------------------------
HERON-WSP-PTH-007's `classify()` answers on the first DATA word it finds
ANYWHERE in a path. So:

    classify("Cache")                      -> DERIVED
    classify("Projects/Tower A/Cache")     -> DATA

The same leaf name, two different classes, decided by what is above it.
A cleanup that clears DERIVED spares one and not the other; a tool
holding only the leaf gets the opposite answer from one holding the
path. A project folder called `Cache`, `Backup`, `Brain` or `Core` is
therefore a folder whose fate depends on who is asking - so a template
may not name one at all.

NOTHING IS CREATED
--------------------
`existing` and `is_file` are handed in, the same shape
HERON-WSP-CRE-002 and HERON-INS-BRN-007 use. An agent that looks and
then acts on what it saw is the shape of the mistake. A folder already
there is LEFT ALONE and listed - re-creating it is not the danger,
emptying it is, and this does neither.
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_paths as PATHS  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# docs/12 s97 - the line is the file, not the information.
REVIT_FILES = (".rvt", ".rfa", ".rte", ".rft")

# Keys that would make a template a CONTAINER rather than a list of names.
CARRIES = ("bytes", "body", "content", "data", "source", "from", "file",
           "path", "url")

# Every word docs/06 s2's classes are made of, flattened. A template may
# not name one: its class would then depend on what sits above it.
RESERVED = tuple(sorted(
    word for _klass, words in PATHS.FOLDERS for word in words))


def _named(entry):
    """The folder name an entry gives, or None if it gives none."""
    if isinstance(entry, dict):
        for key in ("folder", "name"):
            if str(entry.get(key) or "").strip():
                return str(entry[key]).strip()
        return None
    text = str(entry or "").strip()
    return text or None


def _carried(entry):
    """Which CARRIES key an entry uses, or None."""
    if not isinstance(entry, dict):
        return None
    for key in sorted(entry):
        if str(key).strip().lower() in CARRIES:
            return str(key)
    return None


def _revit(text):
    """Which Revit extension a name ends in, or None."""
    low = str(text or "").strip().lower()
    for extension in REVIT_FILES:
        if low.endswith(extension):
            return extension
    return None


def apply(template, under=None, existing=None, is_file=None):
    """
    {create, exists, refused, why, unjudged} - or a refusal.

    Nothing is created and nothing is copied. The answer is a list a
    caller acts on.
    """
    if not isinstance(template, dict):
        return {"created": False, "refused": "NOT_A_TEMPLATE",
                "why": "a template is a map with a name and a list of "
                       "folders. %s is not one, and guessing a shape out "
                       "of it is how a template ends up carrying a file."
                       % type(template).__name__}

    name = str(template.get("name") or "").strip()
    if not name:
        return {"created": False, "refused": "NOT_A_TEMPLATE",
                "why": "the template has no name. A shape applied to a "
                       "project with nothing to call it cannot be named in "
                       "a log afterwards, so it cannot be undone either."}

    where = str(under or "").strip()
    if where:
        landing = PATHS.classify(where)
        if landing["class"] in (PATHS.PRODUCT, PATHS.DERIVED):
            return {"created": False, "refused": "WOULD_LAND_OUTSIDE_DATA",
                    "why": "'%s' is %s. A template makes the user's work, "
                           "never the product - and %s is replaced whole by "
                           "an update or cleared by a cleanup, which would "
                           "take the project with it. %s"
                           % (where, landing["class"], landing["class"],
                              landing["why"])}

    folders = template.get("folders")
    if not isinstance(folders, (list, tuple)) or not folders:
        return {"created": False, "refused": "NOT_A_TEMPLATE",
                "why": "template '%s' names no folders. An empty template "
                       "applied to a project reports success and changes "
                       "nothing, which reads exactly like a working one."
                       % name}

    here = set(str(each).strip() for each in (existing or []))
    taken = is_file if callable(is_file) else (
        lambda folder: folder in set(is_file or []))

    create, already, refused = [], [], []
    for entry in folders:
        folder = _named(entry)
        if folder is None:
            refused.append({"entry": repr(entry)[:60],
                            "refused": "NOT_A_TEMPLATE",
                            "why": "this entry names no folder. A template "
                                   "that half-applies is worse than one "
                                   "that refuses."})
            continue

        # The Revit rule first, and on the bare name: `Standard.rte` is a
        # Revit file whether or not anything is attached to it.
        extension = _revit(folder)
        if extension is not None:
            refused.append({"folder": folder,
                            "refused": "A_REVIT_FILE_IS_NAMED",
                            "why": "'%s' names a %s. docs/12 s97 draws the "
                                   "line at THE FILE, not the information: "
                                   "a .rvt, an .rfa, a family or project "
                                   "template is never uploaded. A template "
                                   "that names the office's own file is one "
                                   "read away from carrying it."
                                   % (folder, extension)})
            continue

        carried = _carried(entry)
        if carried is not None:
            refused.append({"folder": folder,
                            "refused": "TEMPLATE_CARRIES_A_FILE",
                            "why": "'%s' has a '%s'. A template NAMES, it "
                                   "never CARRIES - and that is what keeps "
                                   "docs/12 s97 true by construction "
                                   "rather than by remembering it."
                                   % (folder, carried)})
            continue

        word = folder.strip().lower()
        if word in RESERVED:
            refused.append({"folder": folder,
                            "refused": "RESERVED_WORKSPACE_NAME",
                            "why": "'%s' is one of the twenty words docs/06 "
                                   "s2's classes are made of. A project "
                                   "folder with that name is classified "
                                   "DATA when asked with its full path and "
                                   "%s when asked with the leaf alone, so "
                                   "a cleanup and a backup would disagree "
                                   "about it."
                                   % (folder,
                                      PATHS.classify(folder)["class"])})
            continue

        if taken(folder):
            refused.append({"folder": folder,
                            "refused": "A_FILE_HOLDS_THAT_NAME",
                            "why": "something is already using the name "
                                   "'%s'. Making a folder over a file "
                                   "either fails or destroys it depending "
                                   "on the platform." % folder})
            continue

        if folder in here:
            already.append({"folder": folder,
                            "why": "already there, and left alone. A "
                                   "template applied twice must not empty "
                                   "what the first one filled."})
            continue

        create.append({"folder": folder,
                       "under": where or None,
                       "class": PATHS.classify(
                           "%s/%s" % (where, folder) if where
                           else folder)["class"]})

    landed = len(create) + len(already) + len(refused)
    return {
        "created": False, "template": name, "under": where or None,
        "create": create, "exists": already, "refused_names": refused,
        "of": len(folders),
        "why": "template '%s': %d entry(s), %d to create, %d already there, "
               "%d refused. Nothing was created and nothing was copied."
               % (name, len(folders), len(create), len(already),
                  len(refused)),
        "unjudged": [
            "NOTHING WAS CREATED AND NOTHING WAS COPIED. The list comes "
            "back and a caller makes the folders - and what is already "
            "there was HANDED IN, not looked up.",
            "EVERY ENTRY LANDS IN EXACTLY ONE LIST (%d of %d). A template "
            "that applied four of six and reported success is the failure "
            "this shape exists to prevent." % (landed, len(folders)),
            "A TEMPLATE NAMES, IT NEVER CARRIES. No entry here holds a "
            "file body or a path to one, so there is nothing docs/12 s97 "
            "could be broken with. That is the whole of the guarantee - "
            "it is not a check a caller may skip.",
            "WHAT MAY BE SHIPPED IS A LIST OF NAMES. docs/00 s1115 lists "
            "'Project templates' as a marketplace package and docs/12 s97 "
            "says a project template is never uploaded. Nothing written "
            "says which sense of the word each means. PROPOSALS F12.",
        ],
    }


def main(argv):
    print("TEMPLATES   a template names, it never carries")
    print("=" * 72)

    answer = apply(
        {"name": "MEP Project",
         "folders": ["Models", "Sheets", "Exports",
                     "Cache",
                     "Office Standard.rte",
                     {"folder": "Families", "source": "L:/BIM/Families"}]},
        under="Projects/Tower A",
        existing=["Sheets"])

    print("\n%s" % answer["why"])
    for row in answer["create"]:
        print("  create   %-12s  %s" % (row["folder"], row["class"]))
    for row in answer["exists"]:
        print("  exists   %-12s  left alone" % row["folder"])
    for row in answer["refused_names"]:
        print("  REFUSED  %-12s  %s" % (row.get("folder", "?"),
                                        row["refused"]))

    print("\nthe same leaf name, two classes, decided by what is above it:")
    for path in ("Cache", "Projects/Tower A/Cache"):
        print("  %-24s %s" % (path, PATHS.classify(path)["class"]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
