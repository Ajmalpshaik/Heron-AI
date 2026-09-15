# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-DOC-SKL-003
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The Skill Documentation Agent - the catalogue it generates.

    python tests/test_skill_catalog.py

WHY THIS EXISTS
---------------
tests/test_catalog.py states the rule: a generator that only draws needs
no test, one that CONCLUDES does. This one concludes twice - a skill's
effective status, and which releases it cannot run on - and either can
be wrong while the page still renders perfectly.

WHAT IT PROVES
  1. THE CHAIN IS THE WEAKEST LINK, not the first, not the commonest and
     not the best. One DRAFT fragment under a skill makes the chain
     DRAFT however many PROVEN ones sit beside it.

  2. A CAPABILITY NOBODY PROVIDES IS WORSE THAN ANY STATUS, including
     DISCOVERED - it is a missing link, not a weak one.

  3. UNREACHABLE IS PER RELEASE. A skill claiming eight releases whose
     fragments cover two is reported dead on six, each naming what is
     missing - never as one overall figure, which is what hides it.

  4. THE DECLARED STATUS AND THE EFFECTIVE ONE ARE KEPT APART. A card
     can say anything; the chain underneath is the fact, and the page
     shows both rather than picking one.

  5. EVERY SKILL IN THE LIBRARY APPEARS EXACTLY ONCE.

  6. THE PAGE IS REAL OUTPUT: the placeholder is gone, the embedded data
     is valid JSON, and its counts match the library it was built from.

WHAT IT DOES NOT PROVE. That the page LOOKS right. Nothing here renders it.
"""

import importlib.util
import io
import json
import os
import re
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_skill as SKILL                                   # noqa: E402
import heron_fragment as FRAG                                 # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def load_tool():
    """The generator, loaded by path - its filename has hyphens in it."""
    path = os.path.join(ROOT, "tools", "generate-skill-catalog.py")
    spec = importlib.util.spec_from_file_location("heron_skill_catalog", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def served(capability, revit):
    """One row of `under`, as collect() builds them."""
    return {"capability": capability, "by": ["a-fragment"],
            "status": "PROVEN", "revit": list(revit)}


def main():
    tool = load_tool()

    print("\n1. the chain is the weakest link")
    check(tool.weakest(["PROVEN", "PROVEN", "DRAFT"]) == "DRAFT",
          "one DRAFT among two PROVEN makes the chain DRAFT")
    check(tool.weakest(["DRAFT", "PROVEN"])
          == tool.weakest(["PROVEN", "DRAFT"]),
          "and the order they were read in makes no difference")
    check(tool.weakest(["PRODUCTION", "PROVEN", "VALIDATED"]) == "VALIDATED",
          "three good ones still come back as the lowest of them")
    check(tool.weakest(["PROVEN"]) == "PROVEN",
          "and a chain that is PROVEN all the way down says so")
    # THE LADDER IS docs/09's, not a second copy.
    check(tool.weakest(list(FRAG.STATUSES)) == FRAG.STATUSES[0],
          "the whole ladder handed in returns its bottom rung (%s), so "
          "the order used is heron_fragment's" % FRAG.STATUSES[0])

    print("\n2. nothing providing it is worse than any status")
    check(tool.UNSERVED not in FRAG.STATUSES,
          "UNSERVED is not a rung on the ladder - it is the absence of one")
    check(tool.weakest(["PRODUCTION", tool.UNSERVED]) == tool.UNSERVED,
          "a missing link beats a PRODUCTION one")
    check(tool.weakest([tool.UNSERVED, FRAG.STATUSES[0]]) == tool.UNSERVED,
          "and it beats %s too, the lowest rung there is" % FRAG.STATUSES[0])
    check(tool.weakest([]) == tool.UNSERVED,
          "a skill needing nothing at all is UNSERVED, not PROVEN - "
          "nothing was checked, which is not the same as everything passing")
    check(tool.weakest(["NOT A STATUS"]) == tool.UNSERVED,
          "and a status off the ladder is not quietly treated as good")

    print("\n3. unreachable is per release, never overall")
    under = [served("EARLY", ["2020", "2021"]),
             served("LATE", ["2024", "2025", "2026"])]
    claims = ["2020", "2021", "2024", "2025", "2026", "2027"]
    dead = tool.unreachable(under, claims)
    check([one["revit"] for one in dead]
          == ["2020", "2021", "2024", "2025", "2026", "2027"],
          "a skill claiming six releases served by two disjoint fragments "
          "is dead on ALL six - neither capability covers any release both "
          "of them do")
    check(dead[0]["missing"] == ["LATE"]
          and dead[2]["missing"] == ["EARLY"],
          "and each release names WHICH capability is missing there, not "
          "just that something is")
    whole = [served("A", list(FRAG.REVIT_VERSIONS)),
             served("B", list(FRAG.REVIT_VERSIONS))]
    check(tool.unreachable(whole, list(FRAG.REVIT_VERSIONS)) == [],
          "a skill whose capabilities cover every release it claims is "
          "dead on none")
    check(tool.unreachable(whole, []) == [],
          "and a skill declaring no release is dead on none - there is "
          "nothing to be dead on")
    # ONE FIGURE WOULD HIDE THIS. Seven of eight releases work.
    narrow = tool.unreachable([served("A", ["2024"])],
                              list(FRAG.REVIT_VERSIONS))
    check(len(narrow) == len(FRAG.REVIT_VERSIONS) - 1,
          "a capability on one release leaves %d of the %d claimed dead, "
          "each listed" % (len(narrow), len(FRAG.REVIT_VERSIONS)))

    print("\n4. the declared status and the chain are kept apart")
    rows, problems = tool.collect()
    check(not problems, "the real library reads clean%s"
          % ("" if not problems else ": %s" % "; ".join(problems[:3])))
    check(all("declared_status" in one and "effective" in one
              for one in rows),
          "every row carries BOTH - a card can say anything, the chain "
          "underneath is the fact")
    apart = [one for one in rows
             if one["declared_status"] != one["effective"]]
    check(all(one["effective"] in FRAG.STATUSES + (tool.UNSERVED,)
              for one in rows),
          "and the effective status is always a real rung or UNSERVED")
    print("       %d of %d row(s) declare something other than their chain"
          % (len(apart), len(rows)))

    print("\n5. every skill appears exactly once")
    found, _ = SKILL.load_all()
    ids = [one["id"] for one in rows]
    check(sorted(ids) == sorted(found), "all %d of them" % len(found))
    check(len(ids) == len(set(ids)), "and none of them twice")
    check(all(one["utterances"] for one in rows),
          "each carrying the words somebody actually says")

    print("\n6. the page is real output")
    where = tempfile.mkdtemp()
    try:
        out = os.path.join(where, "page.html")
        was = os.environ.get("HERON_SKILL_CATALOG_OUT")
        os.environ["HERON_SKILL_CATALOG_OUT"] = out
        try:
            again = load_tool()
            check(again.main() == 0, "the generator runs and returns 0")
        finally:
            if was is None:
                os.environ.pop("HERON_SKILL_CATALOG_OUT", None)
            else:
                os.environ["HERON_SKILL_CATALOG_OUT"] = was
        text = io.open(out, encoding="utf-8").read()
        check("__DATA__" not in text,
              "the placeholder is gone - the page carries real data")
        found_json = re.search(r"const DATA = (\{.*\});", text, re.S)
        check(found_json is not None, "and the payload is where the page "
                                      "expects it")
        payload = json.loads(found_json.group(1))
        check(len(payload["rows"]) == len(found),
              "the embedded data holds all %d skill(s)" % len(found))
        check(str(len(found)) in payload["subtitle"],
              "the subtitle's count is the one that was counted")
        check("NOT a claim that anything has been run" in payload["footer"],
              "and the footer says being in here is not evidence")
    finally:
        shutil.rmtree(where)

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the chain underneath is the fact")
    return 0


if __name__ == "__main__":
    sys.exit(main())
