# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-NAM-REF-007
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Reference update - five places, and "none found" is not an answer.

    python tests/test_references.py

WHAT IT PROVES
  1. THE FIVE PLACES ARE docs/28's OWN LIST, read out of the register
     row rather than typed here.

  2. "NOTHING FOUND" IS REFUSED UNTIL ALL FIVE WERE SEARCHED - including
     the case that matters, four of five with nothing found, which reads
     exactly like the safe answer.

  3. A NAME INSIDE A LONGER NAME IS REFUSED, not edited and not
     skipped - and the whole-word case beside it IS edited, so the
     difference is the boundary and not the file.

  4. THE NAME IS DATA, NOT A PATTERN. A name carrying regex punctuation
     searches for itself and nothing else.

  5. THE CLAIM THE MODULE MAKES ABOUT brain/ IS TRUE TODAY, checked
     against the folder - no module name is a prefix of another - so the
     docstring cannot quietly stop being right.

  6. NOTHING IS WRITTEN AND NOTHING IS OPENED.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE AND
     REACHED, and the code refuses nothing the contract omits.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_references as REF                                 # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_references.py"),
                    encoding="utf-8").read()
    code = whole.split("\nfrom __future__", 1)[1]
    ALL = list(REF.PLACES)

    def ask(rename, **kw):
        answer = REF.plan(rename, **kw)
        if answer.get("refused"):
            reached.add(answer["refused"])
        for entry in (answer.get("refused_names") or []):
            reached.add(entry["refused"])
        return answer

    print("1. The five places are docs/28's own list")
    register = io.open(os.path.join(ROOT, "docs", "28-agent-registry.md"),
                       encoding="utf-8").read()
    row = [line for line in register.splitlines() if "NAM-REF-007" in line]
    check(len(row) == 1, "the register has one row for this agent")
    said = row[0].lower()
    for place in ALL:
        check(place in said, "it names '%s'" % place)
    check("no broken references" in said,
          "and says NO BROKEN REFERENCES")
    check(len(ALL) == 5, "the module carries five, not four or six")

    print("\n2. Nothing found is refused until all five were searched")
    nothing = ask({"from": "heron_update", "to": "heron_revision"},
                  found=[], searched=ALL)
    check(not nothing.get("refused") and nothing["edit"] == [],
          "with all five searched, no occurrences is a clean answer")
    check(nothing["searched"] == ALL, "and the answer lists all five")
    # THE ONE THAT MATTERS. Four of five, nothing found - it reads exactly
    # like the answer above.
    for missing in ALL:
        short = ask({"from": "heron_update", "to": "heron_revision"},
                    found=[], searched=[p for p in ALL if p != missing])
        check(short.get("refused") == "NOT_SEARCHED_EVERYWHERE",
              "missing only '%s' is refused, with nothing found either way"
              % missing)
        check(short["missed"] == [missing],
              "  and the answer names what was missed")
    none_at_all = ask({"from": "heron_update", "to": "heron_revision"},
                      found=[], searched=None)
    check(none_at_all.get("refused") == "NOT_SEARCHED_EVERYWHERE",
          "and searching nowhere is refused too, not treated as a default")
    check(len(none_at_all["missed"]) == 5, "with all five named")
    wrong = ask({"from": "a", "to": "b"},
                searched=ALL + ["tests"])
    check(wrong.get("refused") == "NOT_A_PLACE",
          "a sixth place is refused - a misspelt one would quietly reduce "
          "the count the whole answer rests on")

    print("\n3. A name inside a longer name is refused, not skipped")
    mixed = ask({"from": "heron_update", "to": "heron_revision"},
                found=[{"where": "brain/heron_migration.py",
                        "text": "from heron_update import X"},
                       {"where": "brain/other.py",
                        "text": "import heron_updates_helper"},
                       {"where": "brain/third.py",
                        "text": "my_heron_update_thing()"}],
                searched=ALL)
    check(len(mixed["edit"]) == 1,
          "the whole-word one is rewritten")
    check(mixed["edit"][0]["line"] == "from heron_revision import X",
          "and the rewritten line is exact: %s" % mixed["edit"][0]["line"])
    partial = [row["where"] for row in mixed["refused_names"]
               if row["refused"] == "AMBIGUOUS_MATCH"]
    check(sorted(partial) == ["brain/other.py", "brain/third.py"],
          "both longer names are refused - one a suffix, one in the middle")
    check(len(mixed["edit"]) + len(mixed["refused_names"]) == mixed["of"] == 3,
          "and every occurrence lands in exactly one list")
    check("skipped" in code,
          "the code says why refusing beats skipping")

    print("\n4. The name is data, not a pattern")
    # A name with regex punctuation must search for ITSELF.
    dotted = ask({"from": "a.b", "to": "a.c"},
                 found=[{"where": "x", "text": "axb"}], searched=ALL)
    check(dotted["refused_names"] and
          dotted["refused_names"][0]["refused"] == "NOT_A_SITE",
          "'a.b' does not match 'axb' - the dot is a dot")
    real = ask({"from": "a.b", "to": "a.c"},
               found=[{"where": "x", "text": "see a.b here"}], searched=ALL)
    check(len(real["edit"]) == 1 and real["edit"][0]["line"] == "see a.c here",
          "while a real 'a.b' is found and rewritten")
    compiled = code.count("re.compile(")
    check(compiled == 1,
          "the module compiles exactly one pattern (%d), and it is the "
          "word character - never one built out of the name" % compiled)
    check("_WORD = re.compile" in code and "re.compile(name" not in code
          and "re.escape" not in code,
          "so a name is never turned into a pattern, escaped or otherwise")
    starry = REF.occurrences("heron*update and heron_update", "heron*update")
    check(len(starry) == 1 and starry[0]["whole"] is True,
          "a '*' in a name is a '*', matching once and not everything")

    print("\n5. The claim about brain/ is checked, not asserted")
    names = sorted(name[:-3] for name in
                   os.listdir(os.path.join(ROOT, "brain"))
                   if name.endswith(".py") and not name.startswith("__"))
    prefixes = [(a, b) for a in names for b in names
                if a != b and b.startswith(a)]
    check(not prefixes,
          "no module name in brain/ is a prefix of another (%d modules)%s"
          % (len(names),
             "" if not prefixes else ": %s" % prefixes[:3]))
    check("is a prefix of another" in " ".join(whole.split()),
          "and the module says so in words rather than with a number that "
          "goes stale (%d modules today)" % len(names))

    print("\n6. Nothing is written and nothing is opened")
    check(mixed["updated"] is False, "`updated` is false")
    for forbidden in ("open(", "shutil", "os.rename", "os.listdir",
                      "os.walk", "write("):
        check(forbidden not in code, "the code never uses %s" % forbidden)
    check(len(mixed["unjudged"]) == 4, "four things are left unjudged")
    check(any("CLAIMED SEARCHED" in line for line in mixed["unjudged"]),
          "including that five CLAIMED searches is all that can be checked "
          "here, and the difference from five careful ones is the risk")

    print("\n7. Every failure is named and reached")
    for bad, why in ((None, "None is not a rename"),
                     ({"to": "b"}, "a rename with no old name"),
                     ({"from": "a"}, "one with no new name"),
                     ({"from": "a", "to": "a"}, "and one that changes "
                                                "nothing")):
        check(ask(bad, searched=ALL).get("refused") == "NOT_A_RENAME", why)
    for site, why in (("not a map", "a site that is not a map"),
                      ({"text": "heron_update"}, "one with no place"),
                      ({"where": "x"}, "one with no line"),
                      ({"where": "x", "text": "nothing here"},
                       "and one whose line has gone stale")):
        answer = ask({"from": "heron_update", "to": "heron_revision"},
                     found=[site], searched=ALL)
        check([row["refused"] for row in answer["refused_names"]]
              == ["NOT_A_SITE"], why)
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-NAM-REF-007.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 5, "the contract declares 5 failures")
    for failure in named:
        check(failure in code, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    spare = sorted(reached - set(named))
    check(not spare,
          "and the code refuses nothing the contract omits%s"
          % ("" if not spare else ": %s" % ", ".join(spare)))

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    five places, and none found is not an answer")
    return 0


if __name__ == "__main__":
    sys.exit(main())
