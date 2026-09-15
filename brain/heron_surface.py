# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-IMP-SEX-005
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Skill extraction - what a person can CLICK, found two different ways.

    python brain/heron_surface.py [folder]

WHAT IT IS FOR (docs/28, HERON-IMP-SEX-005)
--------------------------------------------
"Identifies USER-FACING capabilities in existing tooling." T3, risk
READ. Step 5 of docs/00 s28's sixteen.

IT IS NOT HERON-IMP-FEX-004 WITH A DIFFERENT WORD
---------------------------------------------------
That agent finds IMPLEMENTATION units - the functions a script is built
out of. This one finds what a PERSON can ask for, and on a real pyRevit
library those are different things by a factor of ten: forty helper
functions behind four buttons.

heron_skill says what a skill is: a name, a purpose, and the words
somebody actually says to ask for it. So the thing being looked for
here is the button, not the function behind it.

TWO SIGNALS, AND THEY ARE NOT THE SAME KIND OF EVIDENCE
---------------------------------------------------------
    A BUNDLE FOLDER is a STATED CONVENTION. pyRevit puts every button
    in a folder whose name ends `.pushbutton`, and the panels and tabs
    around it end `.panel` and `.tab`. That is a rule somebody else
    wrote down, so it is held here as data with its source named, and a
    caller whose tooling uses a different one can replace it.

    AN ENTRY POINT is a STRUCTURAL FACT. A script that no other script
    imports, and that has statements at module level rather than only
    definitions, is something a person runs. That needs no convention
    at all and holds for a loose folder of scripts.

Both are reported, separately, and where they agree the answer says so -
a `script.py` inside a `Count Ducts.pushbutton` folder is a capability
by both readings, and that is the strongest evidence here.

WHAT THE USER SEES IS READ, NEVER INVENTED
--------------------------------------------
pyRevit shows a button's folder name, and a `__title__` in the script
overrides it. Both are facts in the files. So the name comes back as
written - underscores intact, because turning `Count_Ducts` into
`Count Ducts` is a guess about somebody's display convention, and
pyRevit's own rule for that is not written down in this repository.

THE UTTERANCES ARE NOT PRODUCED HERE, AND THAT IS THE SAME ARGUMENT
---------------------------------------------------------------------
HERON-SKL-CRE-002 refuses to author a skill with fewer than two real
utterances and asks for them instead: "an invented utterance is worse
than a missing one - it sits in the card looking like evidence, and the
skill gets routed by a sentence nobody has ever said."

A button called `Count Ducts` is not an utterance. It is a label. So
every capability found here comes back with its label, its script and
its docstring, and the utterances are ASKED FOR - which is why docs/28
makes this row T3 and why nothing here writes a skill.

NOTHING IS WRITTEN AND NO RISK IS DECLARED
--------------------------------------------
A button that deletes elements and a button that counts them look
identical from the folder name. Declaring a risk level from a label
would be the understatement HERON-SKL-CMP-005 exists to refuse, so the
risk is asked for too.
"""

from __future__ import annotations

import ast
import io
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_walk as WALK  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

PARSED = ".py"

# pyRevit's own bundle suffixes. SOMEBODY ELSE'S CONVENTION, held as data
# with its source named rather than as a rule of Heron's - a caller whose
# tooling uses different ones passes its own.
BUNDLES = (".pushbutton", ".pulldown", ".splitbutton", ".splitpushbutton",
           ".stack", ".panel", ".tab", ".extension", ".smartbutton",
           ".linkbutton", ".invokebutton", ".urlbutton", ".toggle",
           ".nobutton", ".content", ".panelbutton")

# The bundle kinds a PERSON clicks. A `.panel` or a `.tab` is furniture
# around them, and reporting a tab as a capability would count the shelf
# as a book.
CLICKED = (".pushbutton", ".pulldown", ".splitbutton", ".splitpushbutton",
           ".smartbutton", ".linkbutton", ".invokebutton", ".urlbutton",
           ".toggle", ".panelbutton")

# What pyRevit reads out of a script to label and describe a button.
TITLE = "__title__"
DOC = "__doc__"

# Statements that are a DEFINITION rather than something that runs. A
# module holding only these is a library; anything else at module level
# is a script somebody executes.
_DEFINITIONS = (ast.Import, ast.ImportFrom, ast.FunctionDef,
                ast.AsyncFunctionDef, ast.ClassDef)


def bundle_of(at, bundles=None):
    """
    (kind, label) for the innermost bundle folder in a path, or None.

    The INNERMOST, because a button lives inside a panel inside a tab and
    the button is the thing a person clicks.
    """
    suffixes = tuple(bundles or BUNDLES)
    for part in reversed(str(at or "").replace("\\", "/").split("/")[:-1]):
        for suffix in suffixes:
            if part.lower().endswith(suffix):
                return suffix, part[:-len(suffix)]
    return None


def _module(path):
    """(tree, problem) for one Python file."""
    try:
        source = io.open(path, encoding="utf-8", errors="replace").read()
    except (IOError, OSError) as error:
        return None, "could not be opened: %s" % (error.strerror or error)
    try:
        return ast.parse(source), None
    except SyntaxError as error:
        return None, ("does not parse as Python: %s at line %s"
                      % (error.msg, error.lineno))


def _dunder(tree, name):
    """A module-level `__title__` or `__doc__` string, or None."""
    for node in tree.body:
        if not isinstance(node, ast.Assign):
            continue
        for target in node.targets:
            if getattr(target, "id", None) != name:
                continue
            if isinstance(node.value, ast.Constant) \
                    and isinstance(node.value.value, str):
                return node.value.value
    return None


def runs_at_import(tree):
    """
    Whether the module has statements that RUN, not only definitions.

    A docstring is not one. A module of imports, functions and classes is
    a library; anything else at the top level is something executed.
    """
    for index, node in enumerate(tree.body):
        if isinstance(node, _DEFINITIONS):
            continue
        if index == 0 and isinstance(node, ast.Expr) \
                and isinstance(node.value, ast.Constant) \
                and isinstance(node.value.value, str):
            continue                      # the module docstring
        if isinstance(node, ast.Assign):
            names = [getattr(one, "id", "") for one in node.targets]
            if all(name.startswith("__") and name.endswith("__")
                   for name in names if name):
                continue                  # __title__, __doc__ and friends
        return True
    return False


def _imports(tree):
    """Every module name this file imports, first segment only."""
    out = []
    for node in ast.walk(tree):
        if isinstance(node, ast.Import):
            for one in node.names:
                out.append(one.name.split(".")[0])
        elif isinstance(node, ast.ImportFrom) and node.module:
            out.append(node.module.split(".")[0])
    return out


def facing(folder, walked=None, bundles=None):
    """
    {found, capabilities, furniture, unparsed} - or a refusal. No
    utterance and no risk is produced.
    """
    if walked is None:
        walked = WALK.walk(folder)
    if not walked or not walked.get("walked"):
        return {"found": False,
                "refused": (walked or {}).get("refused", "NOTHING_TO_READ"),
                "why": (walked or {}).get(
                    "why", "no folder was handed in. HERON-IMP-FIL-002 walks "
                           "one and this reads what it found.")}

    root = walked.get("root")
    python = [card for card in walked["files"]
              if card["extension"] == PARSED]

    trees, unparsed = {}, []
    for card in python:
        path = os.path.join(root, card["at"]) if root else card["at"]
        tree, problem = _module(path)
        if problem:
            unparsed.append({"at": card["at"], "why": problem})
            continue
        trees[card["at"]] = tree

    # WHICH MODULES ARE IMPORTED BY ANOTHER. A script nothing imports is
    # a leaf, and a leaf that runs is something a person runs.
    imported = set()
    for tree in trees.values():
        imported.update(_imports(tree))

    capabilities, furniture = [], []
    for at in sorted(trees):
        tree = trees[at]
        stem = os.path.splitext(os.path.basename(at))[0]
        bundle = bundle_of(at, bundles)
        runs = runs_at_import(tree)
        leaf = stem not in imported

        card = {
            "at": at,
            "module": stem,
            "bundle": bundle[0] if bundle else None,
            "label": _dunder(tree, TITLE) or (bundle[1] if bundle else stem),
            "labelFrom": (TITLE if _dunder(tree, TITLE)
                          else "the bundle folder" if bundle
                          else "the file name"),
            "says": _dunder(tree, DOC) or ast.get_docstring(tree) or None,
            "runsAtImport": runs,
            "importedByAnother": not leaf,
            "byConvention": bool(bundle and bundle[0] in CLICKED),
            "byStructure": runs and leaf,
        }
        card["both"] = card["byConvention"] and card["byStructure"]

        if bundle and bundle[0] not in CLICKED:
            furniture.append(dict(card, why="a %s is furniture around the "
                                            "buttons, not something a "
                                            "person clicks" % bundle[0]))
        elif card["byConvention"] or card["byStructure"]:
            capabilities.append(card)

    both = [card for card in capabilities if card["both"]]
    return {
        "found": True,
        "root": root,
        "of": len(capabilities),
        "capabilities": sorted(capabilities,
                               key=lambda card: (not card["both"],
                                                 card["at"])),
        "agreed": [card["label"] for card in both],
        "furniture": furniture,
        "unparsed": unparsed,
        "wrote": False,
        "asks": [{
            "question": "for each of these %d capabilit%s: what does somebody "
                        "SAY to ask for it, in their own words, and what risk "
                        "does it carry? A label is not an utterance and a "
                        "folder name says nothing about whether it deletes."
                        % (len(capabilities),
                           "y" if len(capabilities) == 1 else "ies"),
            "capabilities": [{"label": card["label"], "at": card["at"],
                              "says": card["says"],
                              "byConvention": card["byConvention"],
                              "byStructure": card["byStructure"]}
                             for card in capabilities],
        }],
        "why": "%d user-facing capabilit%s in %d Python file%s: %d found by "
               "BOTH the bundle convention and the structure, %d by one of "
               "them. %d folder%s of furniture, %d file%s unparsed. Nothing "
               "was written."
               % (len(capabilities), "y" if len(capabilities) == 1 else "ies",
                  len(python), "" if len(python) == 1 else "s", len(both),
                  len(capabilities) - len(both), len(furniture),
                  "" if len(furniture) == 1 else "s", len(unparsed),
                  "" if len(unparsed) == 1 else "s"),
        "unjudged": [
            "NO UTTERANCE WAS PRODUCED. A button called `Count Ducts` is a "
            "LABEL, not something somebody says. HERON-SKL-CRE-002 refuses "
            "a skill with fewer than two real utterances for the same "
            "reason: an invented one sits in the card looking like "
            "evidence, and the skill gets routed by a sentence nobody has "
            "ever said (D-34).",
            "NO RISK WAS DECLARED. A button that deletes elements and a "
            "button that counts them are the same folder name. Reading a "
            "risk level off a label would be the understatement "
            "HERON-SKL-CMP-005 exists to refuse.",
            ("%d CAPABILIT%s FOUND BY BOTH READINGS: %s. The bundle "
             "convention is somebody else's rule and the structure is a "
             "fact about the code, so agreeing is the strongest evidence "
             "here - and where they disagree, both are still reported."
             % (len(both), "Y" if len(both) == 1 else "IES",
                ", ".join(sorted(card["label"] for card in both)))
             if both else
             "nothing was found by both readings, so every capability here "
             "rests on one signal - which is reported per capability."),
            "THE LABEL IS READ, NOT TIDIED. `Count_Ducts` comes back with "
            "its underscore: turning it into `Count Ducts` is a guess about "
            "somebody's display convention, and pyRevit's own rule for that "
            "is not written down in this repository.",
            ("%d BUNDLE FOLDER%s IS FURNITURE - a panel or a tab is what "
             "the buttons sit in, and counting one as a capability would "
             "count the shelf as a book."
             % (len(furniture), "" if len(furniture) == 1 else "S")
             if furniture else
             "no panel or tab folder was found, so nothing had to be told "
             "apart from the buttons."),
            "NOTHING WAS WRITTEN. No skill file, no id, no name of Heron's "
            "own - HERON-SKL-CRE-002 authors a skill and this hands it what "
            "it would need.",
        ],
    }


def main(argv):
    import shutil
    import tempfile

    print("SKILL EXTRACTION   what a person can click, found two ways")
    print("=" * 72)
    print("\nbundle suffixes (pyRevit's convention, replaceable): %s"
          % ", ".join(CLICKED[:4]) + ", ...")

    where = argv[0] if argv else None
    made = None
    if not where:
        made = where = tempfile.mkdtemp(prefix="heron-surface-")
        button = os.path.join(where, "MEP.tab", "Ducts.panel",
                              "Count Ducts.pushbutton")
        os.makedirs(button)
        with io.open(os.path.join(button, "script.py"), "w",
                     encoding="utf-8") as handle:
            handle.write('__title__ = "Count Ducts"\n'
                         '__doc__ = "Counts every duct in the model."\n'
                         "from helpers import collect\n"
                         "print(len(collect()))\n")
        panel = os.path.join(where, "MEP.tab", "Ducts.panel")
        with io.open(os.path.join(panel, "script.py"), "w",
                     encoding="utf-8") as handle:
            handle.write("# panel furniture\n")
        with io.open(os.path.join(where, "helpers.py"), "w",
                     encoding="utf-8") as handle:
            handle.write("def collect():\n    return []\n")
        with io.open(os.path.join(where, "tag_sheets.py"), "w",
                     encoding="utf-8") as handle:
            handle.write('"""Tag every sheet."""\n'
                         "from helpers import collect\n"
                         "for sheet in collect():\n"
                         "    pass\n")

    try:
        answer = facing(where)
        print("\n%s" % answer["why"])
        print("\n%-16s %-9s %-9s %s"
              % ("label", "bundle", "structure", "from"))
        for card in answer["capabilities"]:
            print("  %-14s %-9s %-9s %s"
                  % (card["label"],
                     "yes" if card["byConvention"] else "-",
                     "yes" if card["byStructure"] else "-",
                     card["labelFrom"]))
            if card["says"]:
                print("  %-14s says: %s" % ("", card["says"][:44]))
        for card in answer["furniture"]:
            print("\n  FURNITURE  %-18s %s" % (card["at"], card["why"][:40]))
        for card in answer["unparsed"]:
            print("\n  UNPARSED   %-18s %s" % (card["at"], card["why"][:40]))
        for ask in answer["asks"]:
            print("\n  ASKS  %s" % ask["question"][:66])

        print("\nrefused")
        for bad in (None, os.path.join(ROOT, "no-such-folder")):
            said = facing(bad)
            print("  %-20s %s" % (said["refused"], said["why"][:42]))

        print("\nwhat this agent does not judge")
        for line in answer["unjudged"]:
            print("  - %s" % line)
    finally:
        if made:
            shutil.rmtree(made, ignore_errors=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
