# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-IMP-MIG-009
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Migration - what the translation has to satisfy, and why it is not done
here.

    python brain/heron_translate.py

WHAT IT IS FOR (docs/28, HERON-IMP-MIG-009)
--------------------------------------------
"Transforms imported content into Heron's architecture." T3, risk
MODIFY. Step 12 of docs/00 s28's sixteen, and the last unbuilt row in
its department.

THE TRANSFORM IS A TRANSLATION, AND D-28 IS WHY
-------------------------------------------------
docs/10 s5 names the folders this feature exists for - `AJ-Tools`,
`PyRevit-Tools`, `AEB-Tools` - and pyRevit tooling is Python.
HERON-IMP-FEX-004 harvests Python functions out of exactly those.

D-28 settled what a fragment is, in 2026-08-28's own words: "Roslyn C#
scripting, in process... NO PYTHON RUNTIME IS EMBEDDED." So a harvested
function is not a fragment that needs tidying. It is code in one
language that has to become code in another, and nothing mechanical
does that.

D-28's THIRD reason is this agent's whole brief, and it is worth
quoting because it is the cost of getting this wrong:

    "Python fragments would pass through neither [compile gate] -
     leaving the version boundary uncovered for exactly the code most
     likely to be newest, which is the failure this project already hit
     once this same day."

SO IT PREPARES AND IT CHECKS; IT DOES NOT TRANSLATE
-----------------------------------------------------
`brief` gathers everything a translation needs, from the agents that
already produced it - the source, its computed contract, the releases
it claims, where it belongs - and states the gates the result must
pass, by name.

`check` takes a PROPOSED C# fragment back and reports what can be
verified here without a compiler. It never says the translation is
correct: a compiler says that, and only behaviour in Revit says the
rest.

THE ONE CHECK WORTH MORE THAN THE REST
----------------------------------------
A proposal that still PARSES AS PYTHON has not been translated. That is
a cheap, exact test for the failure that matters most - a migration
that renamed a file and changed nothing - and it is stated with its
limit: a single expression can be valid in both languages, so parsing
as Python is evidence and not proof.

WHAT IT CANNOT CHECK, SAID PLAINLY
------------------------------------
That the C# DOES what the Python did. Nothing here runs either. The
compile gate says the API surface agrees, the harness says the contract
is kept, and only a D-30 proof against a real model says the behaviour
survived the crossing.
"""

from __future__ import annotations

import ast
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_fragment as FRAG  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# D-28's answer, and the reason a harvested function is not a fragment.
THE_LANGUAGE = "C#"
WHY_THE_LANGUAGE = (
    "D-28, 2026-08-28: Roslyn C# scripting, in process, and no Python "
    "runtime is embedded. Its third reason is the one that bites - Python "
    "fragments would pass through neither compile gate, leaving the "
    "version boundary uncovered for exactly the code most likely to be "
    "newest")

# What a fragment card must carry, from HERON-FRG-VAL-001 rather than
# retyped. A translation that produces code and no card has produced
# half a fragment.
MUST_DECLARE = FRAG.REQUIRED

# D-05's releases, borrowed. A migration may not claim one outside them.
RELEASES = FRAG.REVIT_VERSIONS

# The gates the translated result has to pass, named rather than run
# here - brain may not import tools/, and neither of these is cheap.
GATES = (
    ("tools/check-fragments-compile.py",
     "compiles every fragment's C# against every release it claims, and "
     "checks the CONTRACT as well as the syntax: each name the fragment "
     "promises is assigned to a local of the declared type, so a snippet "
     "that compiles while breaking its promise fails"),
    ("a D-30 proof against a real model",
     "the only thing that says the behaviour survived the crossing. A "
     "compile says the API surface agrees and nothing more"),
)


def still_python(code):
    """
    Whether a proposed fragment still parses as Python.

    EVIDENCE, NOT PROOF. `return x` is valid in neither language alone
    and a single expression can be valid in both, so this catches the
    migration that renamed a file and changed nothing - not every
    incomplete translation.
    """
    text = str(code or "")
    if not text.strip():
        return False
    try:
        ast.parse(text)
        return True
    except SyntaxError:
        return False


def brief(unit, places=None, releases=None):
    """
    {briefed, from, must, gates} - or a refusal. Nothing is translated.
    """
    if not unit:
        return {"briefed": False, "refused": "NOTHING_TO_MIGRATE",
                "why": "no unit was handed in. HERON-IMP-FEX-004 harvests "
                       "them and this prepares their crossing."}

    unit = getattr(unit, "data", unit)
    if not isinstance(unit, dict) or not str(unit.get("name") or "").strip():
        return {"briefed": False, "refused": "NOT_A_UNIT",
                "why": "%r is not a harvested unit. Each carries a `name`, "
                       "and HERON-IMP-FEX-004's also carry the code, the "
                       "computed needs and provides, and where it came "
                       "from." % (unit,)}

    wanted = [str(one) for one in (releases or [])]
    outside = [one for one in wanted if one not in RELEASES]
    if outside:
        return {"briefed": False, "refused": "UNKNOWN_RELEASE",
                "why": "Heron does not know Revit %s. D-05: an unlisted "
                       "release is an error, never a guess. Known: %s."
                       % (", ".join(outside), ", ".join(RELEASES))}

    return {
        "briefed": True,
        "from": {"name": unit.get("name"), "file": unit.get("file"),
                 "language": "Python", "code": unit.get("code"),
                 "needs": unit.get("needs") or [],
                 "provides": unit.get("provides") or [],
                 "says": unit.get("says")},
        "into": THE_LANGUAGE,
        "releases": wanted,
        "places": places or [],
        "must": [
            "be %s. %s" % (THE_LANGUAGE, WHY_THE_LANGUAGE),
            "carry a card declaring %s - HERON-FRG-VAL-001's own list, so "
            "a translation that produces code and no card has produced "
            "half a fragment." % ", ".join(MUST_DECLARE),
            "claim only releases Heron knows: %s (D-05)."
            % ", ".join(RELEASES),
            "leave what it promises, at the declared type. The harness "
            "checks that, and a snippet that compiles while breaking its "
            "promise is exactly the one that composes into something "
            "broken far from here.",
        ],
        "gates": [{"gate": name, "why": why} for name, why in GATES],
        "translated": False,
        "why": "%r is %d line%s of Python from %s, needing %s and providing "
               "%s. It has to become %s, and nothing here does that."
               % (unit.get("name"), len(str(unit.get("code") or "").split("\n")),
                  "" if len(str(unit.get("code") or "").split("\n")) == 1
                  else "s",
                  unit.get("file") or "an imported folder",
                  ", ".join(unit.get("needs") or []) or "nothing",
                  ", ".join(unit.get("provides") or []) or "nothing",
                  THE_LANGUAGE),
        "unjudged": [
            "THE TRANSLATION ITSELF. Python to %s is language, and nothing "
            "mechanical does it. docs/28 makes this row T3 for exactly "
            "that." % THE_LANGUAGE,
            "WHY IT IS %s AND NOT PYTHON. %s." % (THE_LANGUAGE,
                                                  WHY_THE_LANGUAGE),
            "WHETHER THE RESULT DOES WHAT THE ORIGINAL DID. Nothing here "
            "runs either one. %s" % GATES[1][1],
            ("THE RELEASES ARE THE CALLER'S: %s. A migration that claimed "
             "all eight because the Python ran on the machine it was "
             "written on would be the version boundary going uncovered, "
             "which is D-28's own warning." % ", ".join(wanted)
             if wanted else
             "NO RELEASE WAS CLAIMED. A fragment has to name the ones it "
             "supports, and claiming none is a card that cannot be "
             "filtered before ranking."),
        ],
    }


def check(proposed, unit=None):
    """
    {checked, problems} - or a refusal. This is not a compiler and never
    says the translation is correct.
    """
    code = str(proposed or "").strip() if not isinstance(proposed, dict) \
        else str(proposed.get("code") or "").strip()
    card = proposed if isinstance(proposed, dict) else {}

    if not code:
        return {"checked": False, "refused": "NOTHING_TO_CHECK",
                "why": "no proposed fragment was handed in."}

    if still_python(code):
        return {"checked": False, "refused": "STILL_PYTHON",
                "why": "the proposal still parses as Python, so it has not "
                       "been translated. %s Evidence and not proof - a "
                       "single expression can be valid in both - but a "
                       "migration that renamed a file and changed nothing "
                       "looks exactly like this." % WHY_THE_LANGUAGE}

    problems = []
    absent = [field for field in MUST_DECLARE
              if not str(card.get(field) or "").strip()]
    if absent:
        problems.append({
            "missing": absent,
            "why": "the card is missing %s%s. HERON-FRG-VAL-001 requires "
                   "all %d, and code with no card is half a fragment."
                   % (", ".join(absent[:3]),
                      "" if len(absent) <= 3
                      else " and %d more" % (len(absent) - 3),
                      len(MUST_DECLARE))})

    claimed = [str(one) for one in (card.get("revit") or [])]
    outside = [one for one in claimed if one not in RELEASES]
    if outside:
        problems.append({
            "releases": outside,
            "why": "Revit %s is not a release Heron knows. D-05 - an "
                   "unlisted release is an error, never a guess."
                   % ", ".join(outside)})

    kept, lost = [], []
    for name in ((unit or {}).get("provides") or []):
        (kept if name in code else lost).append(name)

    return {
        "checked": True,
        "language": THE_LANGUAGE,
        "problems": problems,
        "namesKept": kept,
        "namesGone": lost,
        "compiled": False,
        "why": "%d problem%s that can be seen without a compiler. %s "
               "Nothing was compiled and nothing was run."
               % (len(problems), "" if len(problems) == 1 else "s",
                  "%d of the original's names appear in the proposal and "
                  "%d do not." % (len(kept), len(lost)) if (kept or lost)
                  else "No original was handed in to compare against."),
        "unjudged": [
            "WHETHER IT COMPILES. This is not a compiler. %s"
            % GATES[0][1],
            "WHETHER IT DOES WHAT THE ORIGINAL DID. %s" % GATES[1][1],
            ("%d OF THE ORIGINAL'S NAMES DO NOT APPEAR: %s. That is a "
             "LOOK, not a finding - a translation may rename anything it "
             "likes, and the contract is what has to be kept, not the "
             "spelling."
             % (len(lost), ", ".join(lost))
             if lost else
             "every name the original provided appears somewhere in the "
             "proposal, which is a look and not a proof."),
            "PARSING AS PYTHON IS EVIDENCE, NOT PROOF. A single expression "
            "can be valid in both languages. It catches the migration that "
            "renamed a file and changed nothing, which is the one that "
            "matters most.",
        ],
    }


def main(argv):
    print("MIGRATION   what the translation has to satisfy")
    print("=" * 72)
    print("\ninto: %s" % THE_LANGUAGE)
    print("because: %s" % WHY_THE_LANGUAGE)

    unit = {"name": "collect", "file": "helpers.py",
            "needs": ["COLLECTOR"], "provides": ["found", "n"],
            "says": "Every element of one category.",
            "code": "def collect(doc, category):\n"
                    "    found = COLLECTOR(doc).OfCategory(category)\n"
                    "    n = len(str(found))\n"
                    "    return found, n\n"}

    answer = brief(unit, releases=["2024", "2025"])
    print("\n%s" % answer["why"])
    print("\nit must")
    for line in answer["must"]:
        print("  - %s" % line[:70])
    print("\nand then pass")
    for gate in answer["gates"]:
        print("  %-38s %s" % (gate["gate"], gate["why"][:32]))

    print("\nchecking a proposal")
    said = check(unit["code"], unit)
    print("  %-18s %s" % (said["refused"], said["why"][:52]))

    csharp = ("var found = new FilteredElementCollector(doc)"
              ".OfCategory(category).ToElements();\n"
              "var n = found.Count;\n")
    said = check(csharp, unit)
    print("  %-18s %s" % ("checked", said["why"][:60]))
    for problem in said["problems"]:
        print("    PROBLEM  %s" % problem["why"][:56])

    print("\nrefused")
    for these, releases in ((None, None), ("a string", None),
                            ({"name": "  "}, None),
                            (unit, ["2019"])):
        bad = brief(these, releases=releases)
        print("  %-20s %s" % (bad["refused"], bad["why"][:42]))
    bad = check("")
    print("  %-20s %s" % (bad["refused"], bad["why"][:42]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
