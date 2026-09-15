# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-IMP-SEX-005
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Skill extraction - a button is not a function, and a label is not an
utterance.

    python tests/test_surface.py

WHAT IT PROVES
  1. A HELPER IS NOT A CAPABILITY. A file that is imported and only
     defines things is found by neither signal.

  2. THE TWO SIGNALS ARE INDEPENDENT. A bundle button with no top-level
     code is found by convention alone; a loose script nothing imports
     is found by structure alone; one that is both says so.

  3. A PANEL IS NOT A BUTTON. Furniture is told apart from what a person
     clicks.

  4. THE LABEL COMES FROM WHERE IT SAYS IT DOES, and `__title__` beats
     the folder, which beats the file name.

  5. THE LABEL IS NOT TIDIED - an underscore survives.

  6. NO UTTERANCE AND NO RISK. No output field holds either, and the
     question asks for both.

  7. THE CONVENTION IS DATA - handed different suffixes, it finds
     different buttons.

  8. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_surface as SEX                                    # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def write(where, at, text):
    full = os.path.join(where, *at.split("/"))
    folder = os.path.dirname(full)
    if folder and not os.path.isdir(folder):
        os.makedirs(folder)
    with io.open(full, "w", encoding="utf-8") as handle:
        handle.write(text)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_surface.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]

    yard = tempfile.mkdtemp(prefix="heron-surface-test-")
    try:
        where = os.path.join(yard, "AJ-Tools")
        os.makedirs(where)
        # A helper: imported, defines only.
        write(where, "helpers.py", "def collect():\n    return []\n")
        # Convention only: inside a pushbutton, defines only, imported by
        # nothing - but it does not RUN, so structure alone would miss it.
        write(where, "MEP.tab/Ducts.panel/Count_Ducts.pushbutton/script.py",
              '__title__ = "Count_Ducts"\n'
              '__doc__ = "Counts every duct."\n'
              "def run():\n    return 1\n")
        # Structure only: a loose script that runs and nothing imports.
        write(where, "tag_sheets.py",
              '"""Tag every sheet."""\n'
              "from helpers import collect\n"
              "for sheet in collect():\n    pass\n")
        # Both.
        write(where, "MEP.tab/Ducts.panel/Purge.pushbutton/script.py",
              "from helpers import collect\n"
              "print(collect())\n")
        # Furniture.
        write(where, "MEP.tab/Ducts.panel/script.py", "# panel\n")

        answer = SEX.facing(where)
        by_label = dict((card["label"], card) for card in
                        answer["capabilities"])

        print("\n1. a helper is not a capability")
        check(answer["found"] is True, "the folder was read")
        check("helpers" not in by_label,
              "helpers.py is not a capability: %s"
              % ", ".join(sorted(by_label)))
        helper = SEX._module(os.path.join(where, "helpers.py"))[0]
        check(SEX.runs_at_import(helper) is False,
              "it has nothing that runs at module level")

        print("\n2. the two signals are independent")
        check(by_label["Count_Ducts"]["byConvention"] is True
              and by_label["Count_Ducts"]["byStructure"] is False,
              "a button that only defines is found by CONVENTION alone")
        check(by_label["tag_sheets"]["byStructure"] is True
              and by_label["tag_sheets"]["byConvention"] is False,
              "a loose script that runs is found by STRUCTURE alone")
        purge = [card for card in answer["capabilities"]
                 if card["at"].endswith("Purge.pushbutton/script.py")
                 or card["at"].endswith("Purge.pushbutton\\script.py")][0]
        check(purge["both"] is True, "and one is found by both")
        check(answer["agreed"] == [purge["label"]],
              "which is the only one agreed: %s" % answer["agreed"])
        check(answer["capabilities"][0]["both"] is True,
              "the agreed one is listed first")

        print("\n3. a panel is not a button")
        check(len(answer["furniture"]) == 1,
              "the panel script is furniture (%d)" % len(answer["furniture"]))
        check(".panel" in answer["furniture"][0]["bundle"],
              "and it is the .panel: %s" % answer["furniture"][0]["bundle"])
        check(all(".panel" not in str(card["bundle"])
                  for card in answer["capabilities"]),
              "no capability came from a panel")

        print("\n4. the label comes from where it says it does")
        check(by_label["Count_Ducts"]["labelFrom"] == SEX.TITLE,
              "__title__ wins: %s" % by_label["Count_Ducts"]["labelFrom"])
        check(purge["label"] == "Purge"
              and purge["labelFrom"] == "the bundle folder",
              "the bundle folder is next: %r from %r"
              % (purge["label"], purge["labelFrom"]))
        check(by_label["tag_sheets"]["labelFrom"] == "the file name",
              "and the file name last")
        check(by_label["tag_sheets"]["says"] == "Tag every sheet.",
              "a module docstring is read as what it says")
        check(by_label["Count_Ducts"]["says"] == "Counts every duct.",
              "and a __doc__ assignment too")

        print("\n5. the label is not tidied")
        check(by_label["Count_Ducts"]["label"] == "Count_Ducts",
              "the underscore survives: %r"
              % by_label["Count_Ducts"]["label"])
        check("replace(\"_\"" not in logic and "replace('_'" not in logic,
              "and the module never replaces one")

        print("\n6. no utterance and no risk")
        check(answer["wrote"] is False, "`wrote` is false")
        for card in answer["capabilities"]:
            check(not any(key in card for key in
                          ("utterances", "risk", "id", "skill")),
                  "%s carries no utterance and no risk: %s"
                  % (card["label"], ", ".join(sorted(card))))
        asked = answer["asks"][0]["question"]
        check("SAY" in asked and "risk" in asked,
              "and the question asks for both")
        check("label is not an utterance" in asked,
              "saying why a label is not one")

        print("\n7. the convention is data")
        mine = SEX.facing(where, bundles=[".widget"])
        check(all(card["byConvention"] is False
                  for card in mine["capabilities"]),
              "handed .widget, nothing is found by convention")
        check(mine["furniture"] == [],
              "and nothing is furniture either")
        check([card["label"] for card in mine["capabilities"]]
              != [card["label"] for card in answer["capabilities"]],
              "so the answer really follows the suffixes it was given")
        check(SEX.bundle_of("MEP.tab/Ducts.panel/Purge.pushbutton/script.py")
              == (".pushbutton", "Purge"),
              "and the INNERMOST bundle is the one taken")

        print("\n8. every failure is named and reached")
        for these, name in ((None, "NOTHING_TO_WALK"),
                            (os.path.join(yard, "gone"), "NOT_A_FOLDER")):
            said = SEX.facing(these)
            reached.add(said.get("refused"))
            check(said.get("refused") == name, "%s is reached" % name)
        said = SEX.facing(None, walked={"walked": False,
                                        "refused": "NOTHING_TO_READ",
                                        "why": "handed a refusal"})
        reached.add(said.get("refused"))
        check(said.get("refused") == "NOTHING_TO_READ",
              "NOTHING_TO_READ is reached")

        contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                         "HERON-IMP-SEX-005.yaml"))
        named = contract.get("failures") or []
        check(len(named) == 3, "the contract declares 3 failures")
        for failure in named:
            check(failure in logic or failure in ("NOTHING_TO_WALK",
                                                  "NOT_A_FOLDER"),
                  "the code names or carries %s" % failure)
        unreached = sorted(set(named) - reached)
        check(not unreached,
              "and every one was reached above%s"
              % ("" if not unreached else ": %s" % ", ".join(unreached)))
        check(len(answer["unjudged"]) == 6, "six things are left unjudged")
    finally:
        shutil.rmtree(yard, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    a button is not a function, and a label is not an "
          "utterance")
    return 0


if __name__ == "__main__":
    sys.exit(main())
