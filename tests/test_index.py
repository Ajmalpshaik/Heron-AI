# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-IMP-IDX-013
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Import indexing - it builds no index, and presented is not accepted.

    python tests/test_index.py

WHAT IT PROVES
  1. IT BUILDS NO INDEX. The four functions it runs are the SAME OBJECTS
     as HERON-RAG-IDX-010's and HERON-RAG-EMB-008's, and running the
     agent has the same effect on the library as calling them.

  2. PRESENTED IS NOT ACCEPTED. HERON-IMP-APR-014's own answer, handed
     straight through, is refused - and that answer is the real one,
     produced by calling that agent.

  3. AN UNSIGNED ACCEPTANCE IS REFUSED (Golden Rule 7).

  4. DISCOVERED IS OFFERABLE, so indexing is the step that makes an
     import findable. Both lists are the owners' own objects, not copies.

  5. WHAT THE LIBRARY DOES NOT HOLD IS REPORTED, not counted as indexed.

  6. A SKILL IS NOT SILENTLY DROPPED - and the claim behind that is
     checked against heron_search and heron_embed themselves.

  7. NO FRAGMENT FILE IS TOUCHED.

  8. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def snapshot(where):
    """Every file under a folder, with its size and modification time."""
    out = {}
    for here, folders, names in os.walk(where):
        for name in names:
            full = os.path.join(here, name)
            stat = os.stat(full)
            out[os.path.relpath(full, where)] = (stat.st_size, stat.st_mtime)
    return out


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_index.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]

    import heron_index as IDX
    import heron_embed as EMBED
    import heron_import as IMPORT
    import heron_retrieve as RETRIEVE
    import heron_search as SEARCH
    import heron_skill as SKILL
    import heron_contract as CON

    print("\n1. it builds no index")
    check(IDX.INDEXES["fragment"] == (("keyword", SEARCH.index),
                                      ("vector", EMBED.index)),
          "the fragment indexes ARE heron_search.index and heron_embed.index")
    check(IDX.INDEXES["document"] == (("keyword", SEARCH.index_chunks),
                                      ("vector", EMBED.index_chunks)),
          "the document indexes ARE the two index_chunks")
    for kind in IDX.INDEXES:
        for named, run in IDX.INDEXES[kind]:
            check(run.__module__ in ("heron_search", "heron_embed"),
                  "%s %s lives in %s, not here" % (kind, named,
                                                   run.__module__))
    check(IDX.AN_ITEM_CARRIES is IMPORT.AN_ITEM_CARRIES,
          "the item shape IS HERON-IMP-APR-014's, not a restatement")

    home = tempfile.mkdtemp(prefix="heron-index-test-")
    was = os.environ.get("HERON_KNOWLEDGE")
    os.environ["HERON_KNOWLEDGE"] = home
    try:
        import heron_scope as SCOPE
        SCOPE.rebuild()
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        held = sorted(row["id"] for row in store.fragments())
        check(len(held) > 1, "the library holds %d fragments" % len(held))

        def accepted(items, **changes):
            card = {"accepted": True, "by": "Ajmal", "items": items}
            card.update(changes)
            return card

        def item(**changes):
            card = {"id": held[0], "name": "CountDucts.py",
                    "kind": "fragment", "from": "AJ-Tools/CountDucts.py"}
            card.update(changes)
            return card

        library = snapshot(os.path.join(ROOT, "brain", "fragments"))
        answer = IDX.index_accepted(store, accepted([item()]))
        check(answer["indexed"] is True, "an accepted result is indexed")

        # THE EFFECT IS THE PROOF. Calling the two indexers directly
        # afterwards writes the same rows over the same library - so the
        # agent ran them rather than doing something of its own.
        mine = answer["ran"]["fragment keyword"]["written"]
        # CALLING IT AGAIN NOW SKIPS, AND THAT IS THE POINT OF THE SKIP.
        # `index()` is content-hashed since FRAGMENT-ISSUES row 136, so a
        # second call over an unchanged library writes nothing and reports
        # (0, rows). It used to rebuild every time, which is what let a
        # question asked from one checkout wipe another's declarations. So
        # the comparison is against what the SKIP reports, and `force=True`
        # is what asks for the old behaviour deliberately.
        written, skipped = SEARCH.index(store)
        check(written == 0 and skipped == mine,
              "the keyword index wrote %d rows, and calling it directly now "
              "skips %d - the same library, unchanged" % (mine, skipped))
        check(mine == len(held),
              "and that is every fragment in the library, not just the one "
              "imported - the keyword index rebuilds the lot")
        wrote, skipped = EMBED.index(store)
        check(wrote == 0 and skipped == len(held),
              "the vector index has nothing left to do (%d unchanged), "
              "because the agent already embedded them" % skipped)

        print("\n2. presented is not accepted")
        real = IMPORT.present({"items": [{"name": "CountDucts.py",
                                          "kind": "fragment",
                                          "from": "AJ-Tools"}]})
        check(real.get("presented") is True,
              "HERON-IMP-APR-014 presents this manifest")
        said = IDX.index_accepted(store, real)
        reached.add(said.get("refused"))
        check(said.get("refused") == "NOT_ACCEPTED",
              "and its answer handed straight through is REFUSED")
        check("accepts nothing" in said["why"],
              "the reason names what that agent does, not a field name")

        print("\n3. an unsigned acceptance is refused")
        for who in (None, "", "   "):
            said = IDX.index_accepted(store, accepted([item()], by=who))
            reached.add(said.get("refused"))
            check(said.get("refused") == "NOT_ACCEPTED",
                  "by=%r is refused" % who)
        check("Golden Rule 7" in said["why"], "and the reason names GR 7")

        print("\n4. DISCOVERED is offerable, so indexing makes it findable")
        check(IDX.OFFERABLE is RETRIEVE.OFFERABLE,
              "IDX.OFFERABLE IS RETRIEVE.OFFERABLE - the same object")
        check(IDX.ENTERS_AT is IMPORT.ENTERS_AT,
              "IDX.ENTERS_AT IS HERON-IMP-APR-014's - the same object")
        check(IDX.ENTERS_AT in IDX.OFFERABLE,
              "%s is in OFFERABLE, which is why the answer says so out loud"
              % IDX.ENTERS_AT)
        findable = [line for line in answer["unjudged"]
                    if IDX.ENTERS_AT in line and "OFFERABLE" in line]
        check(len(findable) == 1,
              "and exactly one unjudged line says it")

        print("\n5. what the library does not hold is reported")
        answer = IDX.index_accepted(store, accepted(
            [item(), item(id="FRG-ZZZ-999", name="NeverSaved.py")]))
        missing = [card["id"] for card in answer["not_in_the_library"]]
        check(missing == ["FRG-ZZZ-999"],
              "the unsaved fragment is named: %s" % ", ".join(missing))
        check([card["id"] for card in answer["found"]] == [held[0]],
              "and it is not in `found`")
        check(len(answer["found"]) + len(answer["not_in_the_library"])
              + len(answer["no_index"]) == answer["of"],
              "every accepted item is in exactly one bucket (%d)"
              % answer["of"])

        print("\n6. a skill is not silently dropped")
        answer = IDX.index_accepted(store, accepted(
            [item(), item(id="count-things", name="count things",
                          kind="skill"),
             item(id="DOC-1", name="AEB standard", kind="poster")]))
        kinds = sorted(card["kind"] for card in answer["no_index"])
        check(kinds == ["poster", "skill"],
              "both unindexable kinds are named: %s" % ", ".join(kinds))
        # THE CLAIM IS ABOUT THE OTHER TWO MODULES, SO IT IS CHECKED THERE.
        # A word search in this file would only find heron_index saying it.
        for module in (SEARCH, EMBED):
            source = io.open(module.__file__, encoding="utf-8").read()
            check("skill" not in source.lower(),
                  "%s does not mention a skill anywhere" % module.__name__)
        check(hasattr(SKILL, "load_all"),
              "and heron_skill.load_all is how a skill is found instead")

        print("\n7. no fragment file is touched")
        check(snapshot(os.path.join(ROOT, "brain", "fragments")) == library,
              "every fragment file is as it was - index rows only")

        print("\n8. every failure is named and reached")
        for these, name in (
                (None, "NOTHING_TO_INDEX"),
                (accepted([]), "NOTHING_TO_INDEX"),
                ("a string", "NOT_ACCEPTED"),
                ({"accepted": False, "by": "Ajmal", "items": [item()]},
                 "NOT_ACCEPTED"),
                (accepted(["a string"]), "NOT_AN_ITEM"),
                (accepted([item(id="")]), "NOT_AN_ITEM"),
                (accepted([item(**{"from": ""})]), "NOT_AN_ITEM")):
            said = IDX.index_accepted(store, these)
            reached.add(said.get("refused"))
            check(said.get("refused") == name, "%s is reached" % name)
        said = IDX.index_accepted(None, accepted([item()]))
        reached.add(said.get("refused"))
        check(said.get("refused") == "NO_LIBRARY", "NO_LIBRARY is reached")

        print("\nR. THE SECOND CODEX REVIEW - `by` must be a person")
        for machine in ("ci", "bot", "pipeline", "script"):
            said = IDX.index_accepted(store,
                                      accepted([item()], by=machine))
            reached.add(said.get("refused"))
            check(said.get("refused") == "ACCEPTED_BY_A_MACHINE",
                  "'%s' accepting an import is refused - the gate asked "
                  "only that `by` was non-empty, so an agent id or a CI "
                  "job read as signed while nobody had looked" % machine)

        contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                         "HERON-IMP-IDX-013.yaml"))
        named = contract.get("failures") or []
        check(len(named) == len(set(named)),
              "the contract declares each failure once")
        for failure in named:
            check(failure in logic, "the code names %s" % failure)
        unreached = sorted(set(named) - reached)
        check(not unreached,
              "and every one was reached above%s"
              % ("" if not unreached else ": %s" % ", ".join(unreached)))
        check(len(answer["unjudged"]) == 7, "seven things are left unjudged")
    finally:
        if was is None:
            os.environ.pop("HERON_KNOWLEDGE", None)
        else:
            os.environ["HERON_KNOWLEDGE"] = was
        shutil.rmtree(home, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    it builds no index, and presented is not accepted")
    return 0


if __name__ == "__main__":
    sys.exit(main())
