# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-WSP-PLC-005
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test

"""
File placement - the DECLARED kind decides, and a name is never a place.

    python tests/test_placement.py

WHAT IT PROVES
  1. THE EIGHT KINDS ARE docs/06 s2's DATA ROW EXACTLY - asserted
     against HERON-WSP-PTH-007's own table, not against a list typed
     here. Nothing product, nothing derived, none missing.

  2. THE KIND DECIDES AND THE NAME DOES NOT. The same name placed under
     two kinds lands in two folders; a name that looks like a path
     chooses nothing and is refused.

  3. A NAME IS REFUSED, NEVER CLEANED UP - and the suite MEASURES what
     cleaning up costs, by running heron_scope and watching three
     project names arrive at one file. PROPOSALS F14.

  4. AN ANSWER IS CHECKED, NOT TRUSTED. The one question this tier may
     put is put only for an unknown kind, and its answer goes through
     the same rules.

  5. NOTHING IS WRITTEN AND NOTHING IS LOOKED UP.

  6. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_placement as PLC                                  # noqa: E402
import heron_paths as PATHS                                    # noqa: E402
import heron_scope as SCOPE                                    # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_placement.py"),
                    encoding="utf-8").read()
    # CODE ONLY - the docstring says these words on purpose.
    code = whole.split("\nfrom __future__", 1)[1]

    def ask(artefact, **kw):
        answer = PLC.place(artefact, **kw)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    print("1. The eight kinds are docs/06 s2's data row exactly")
    data_row = set()
    derived = set()
    product = set()
    for klass, words in PATHS.FOLDERS:
        target = {PATHS.DATA: data_row, PATHS.DERIVED: derived,
                  PATHS.PRODUCT: product}[klass]
        target.update(words)
    placed = set(folder.lower() for folder in PLC.KINDS.values())
    check(placed == data_row,
          "the eight folders ARE the %d data words, read from the path "
          "table" % len(data_row))
    check(not (placed & product),
          "none of them is product - an update replaces product wholesale "
          "and the artefact would be gone without a word")
    check(not (placed & derived),
          "and none is derived - a thing that can be rebuilt needs no "
          "decision about where it belongs")
    check(len(PLC.KINDS) == len(placed) == 8,
          "eight kinds, eight folders, one each")

    print("\n2. The kind decides; the name does not")
    same = "helper"
    one = ask({"kind": "skill", "name": same})
    two = ask({"kind": "fragment", "name": same})
    check(one["folder"] == "Skills" and two["folder"] == "Fragments",
          "the SAME name under two kinds lands in two folders")
    check(one["of"] == two["of"] == same,
          "and the name is carried through, having chosen nothing")
    looks = ask({"kind": "fragment", "name": "Skills/helper.py"})
    check(looks.get("refused") == "NAME_IS_NOT_A_PLACE",
          "a name that looks like a place is refused, not obeyed")
    golden = io.open(os.path.join(ROOT, "docs", "14-golden-rules.md"),
                     encoding="utf-8").read()
    check("data, never instruction" in golden,
          "and Golden Rule 19 really does say content is data, never "
          "instruction")
    check("Golden Rule 19" in whole,
          "which the agent cites as the reason")

    print("\n3. A name is refused, never cleaned up - and here is the cost")
    # MEASURED, not described. heron_scope reduces a project key to a safe
    # filename, and three different projects arrive at one file.
    was = os.environ.get("HERON_KNOWLEDGE")
    os.environ["HERON_KNOWLEDGE"] = os.path.join(ROOT, ".cache-test-placement")
    landed = set()
    try:
        for key in ("Tower B", "Tower/B", "Tower-B"):
            # scope_path only JOINS - nothing is created and nothing opened.
            landed.add(SCOPE.scope_path("project", key))
    finally:
        if was is None:
            os.environ.pop("HERON_KNOWLEDGE", None)
        else:
            os.environ["HERON_KNOWLEDGE"] = was
    check(len(landed) == 1,
          "heron_scope sends 'Tower B', 'Tower/B' and 'Tower-B' to ONE "
          "file: %s" % os.path.basename(sorted(landed)[0]))
    check("one client" in io.open(os.path.join(ROOT, "brain",
                                               "heron_scope.py"),
                                  encoding="utf-8").read(),
          "and its own docstring calls that one client's knowledge in "
          "another's file")
    for bad in ("Tower/B", "Tower\\B", "../..", "Tower:B", "", "  "):
        answer = ask({"kind": "memory", "name": "x"}, project=bad)
        check(answer.get("refused") == "NAME_IS_NOT_A_PLACE",
              "the agent refuses the project %r rather than reducing it"
              % bad)
    good = ask({"kind": "memory", "name": "duct-sizes"}, project="Tower B")
    check(good["folder"] == "Projects/Tower B/Memory",
          "and a plain project name is taken AS GIVEN, space and all")
    check(good["project"] == "Tower B",
          "unchanged - a reduced name is a different name")
    check(good["class"] == PATHS.DATA, "and it classifies data")
    check(any("F14" in line for line in good["unjudged"]),
          "the answer says so itself, and names PROPOSALS F14")

    print("\n4. An answer is checked, not trusted")
    puts = {"count": 0}

    def answering(value):
        def asked(kind):
            puts["count"] += 1
            return value
        return asked

    known = ask({"kind": "fragment", "name": "x"},
                asked=answering("skill"))
    check(known["folder"] == "Fragments" and known["asked"] is None,
          "a KNOWN kind is not asked about at all")
    check(puts["count"] == 0, "the question was not put")
    helped = ask({"kind": "snippet", "name": "x"},
                 asked=answering("fragment"))
    check(helped["folder"] == "Fragments" and helped["kind"] == "fragment",
          "an unknown kind IS asked, and a good answer is used")
    check(helped["asked"] == "fragment", "and the answer is reported")
    check(puts["count"] == 1, "exactly one question was put")
    for nonsense in ("Core", "Cache", "anywhere", "", "Packages"):
        answer = ask({"kind": "snippet", "name": "x"},
                     asked=answering(nonsense))
        check(answer.get("refused") == "THE_ANSWER_IS_NOT_A_PLACE",
              "the answer %r is refused - Core and Cache included" % nonsense)
    silent = ask({"kind": "snippet", "name": "x"}, asked=None)
    check(silent.get("refused") == "NOT_A_KIND",
          "and with nothing to ask, an unknown kind is refused, not guessed")

    print("\n5. Nothing is written, nothing is looked up")
    check(good["placed"] is False and one["placed"] is False,
          "`placed` is false on every answer")
    for forbidden in ("open(", "makedirs", "os.mkdir", "shutil",
                      "os.listdir", "os.walk", "os.path.exists"):
        check(forbidden not in code,
              "the code never uses %s" % forbidden)
    check(len(good["unjudged"]) == 4, "four things are left unjudged")

    print("\n6. Every failure the contract declares is named and reached")
    for bad, why in ((None, "None is not an artefact"),
                     ({"name": "x"}, "an artefact with no kind"),
                     ({"kind": "  ", "name": "x"}, "a blank kind")):
        answer = ask(bad)
        check(answer.get("refused") == "NO_KIND", why)
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-WSP-PLC-005.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 4, "the contract declares 4 failures")
    for failure in named:
        check(failure in code, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    # Nothing declared that the code cannot produce, either.
    spare = sorted(set(reached) - set(named))
    check(not spare,
          "and the code refuses nothing the contract does not declare%s"
          % ("" if not spare else ": %s" % ", ".join(spare)))

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the kind decides, and a name is never a place")
    return 0


if __name__ == "__main__":
    sys.exit(main())
