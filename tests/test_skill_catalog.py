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
    rows, problems, where_meta = tool.collect()
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
    check(isinstance(where_meta, dict) and "why" in where_meta
          and "taken" in where_meta,
          "and collect() hands back where the WORDS half came from, so the "
          "page can say it is a recording rather than a run")

    print("\n5. every skill appears exactly once")
    found, _ = SKILL.load_all()
    ids = [one["id"] for one in rows]
    check(sorted(ids) == sorted(found), "all %d of them" % len(found))
    check(len(ids) == len(set(ids)), "and none of them twice")
    check(all(one["utterances"] for one in rows),
          "each carrying the words somebody actually says")

    print("\n6. the WORDS are the other half, and an absent one says so")
    # A MISSING RECORDING MUST NOT READ AS A CLEAN ONE. That is the whole
    # difference between `words: null` and `0 crossings`, and it is the
    # reason routing() returns a reason rather than an empty measurement.
    nowhere = tempfile.mkdtemp()
    try:
        meta, recorded = tool.routing(os.path.join(nowhere, "not-here"))
        check(recorded == {}, "a missing folder yields no measurement")
        check(meta.get("why"), "and says WHY rather than returning a zero "
                               "dressed as a measurement (D-52)")
        meta, recorded = tool.routing(nowhere)
        check(recorded == {} and meta.get("why"),
              "an empty folder is named too, with the command that fills it")
        check("prove-skill" in (meta.get("why") or ""),
              "and the reason names the tool that writes one")

        bad = os.path.join(nowhere, "routing-2026-01-01.json")
        io.open(bad, "w", encoding="utf-8").write(u"{ not json")
        meta, recorded = tool.routing(nowhere)
        check(recorded == {} and "could not be read" in (meta.get("why") or ""),
              "and an unreadable recording is reported, never swallowed")
    finally:
        shutil.rmtree(nowhere)

    # `said` is counting, not judging: `where` was decided by
    # check-skill-routing.classify when the measurement was taken, and a
    # second opinion here would be a second opinion.
    counted = tool.said([
        ("a", "COUNT_ELEMENTS", "READ", "identity", "reach"),
        ("b", "MIRROR_ELEMENTS", "MODIFY", "hybrid", "crossing"),
        ("c", "READ_MEP_SYSTEM", "READ", "hybrid", "miss"),
        ("d", "HIGHLIGHT_VS_REST", "MODIFY", "hybrid", "escalation"),
    ], ["COUNT_ELEMENTS"])
    check(counted["total"] == 4 and counted["reach"] == 1,
          "said() counts what reached (%d of %d)"
          % (counted["reach"], counted["total"]))
    check(counted["crossing"] == ["b"],
          "and separates a question answered by a write BY NAME")
    check(sorted(counted["elsewhere"]) == ["c", "d"],
          "from the ones that merely landed elsewhere")
    check(len(counted["landed"]) == 4,
          "and keeps every landing so a card can show each sentence")

    measured = [one for one in rows if one["words"]]
    check(all(("words" in one) for one in rows),
          "every row carries the key, measured or not")
    if measured:
        check(all(one["words"]["total"] == len(one["utterances"])
                  for one in measured),
              "and a measured row covers every utterance the skill declares")
        # THE CASE THE HALF WAS ADDED FOR. A chain of PROVEN fragments and a
        # skill whose own words do not reach it are the same card without it.
        split = [one for one in measured
                 if one["effective"] == "PROVEN"
                 and one["words"]["reach"] < one["words"]["total"]]
        print("       %d skill(s) have a PROVEN chain AND a word that does "
              "not reach - invisible on the chain alone" % len(split))

    print("\n7. the page is real output")
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
        check("CHAIN:" in payload["subtitle"]
              and "WORDS:" in payload["subtitle"],
              "the subtitle carries BOTH halves, so neither reads as the whole")
        check("NOT re-measured here" in payload["footer"],
              "and the footer says the words are a recording, not a run")
        check("NOT MEASURED" in text,
              "the page can say NOT MEASURED - an absent recording is not a "
              "clean one")
    finally:
        shutil.rmtree(where)

    print("\n8. a recording outlives the sentences it counted")
    # THE SKILL IS TAKEN FROM THE LIBRARY, never typed - a name written here
    # goes stale the day somebody renames a card, and this suite would then
    # be checking nothing while still passing.
    standing = sorted((one for one in found.values()
                       if len(one.utterances()) > 1), key=lambda one: one.id)
    check(standing, "the library has a skill with more than one sentence")
    REAL_ID = standing[0].id
    REAL_SAYS = list(standing[0].utterances())
    print("       using %s, which says %d sentence(s)"
          % (REAL_ID, len(REAL_SAYS)))
    # NOTHING HERE EDITS brain/skills. Row 142 is what a suite that rewrites
    # the real library costs, and this checkout is shared - so the SKILL is
    # left alone and the RECORDING is the thing written for the case.
    check(tool.ROUTING.words_moved([["a", "X", "READ", "identity", "reach"]],
                           ["a"]) == ([], []),
          "the same sentence measured and said has moved nothing")
    check(tool.ROUTING.words_moved([["a", "X", "READ", "identity", "reach"]],
                           []) == (["a"], []),
          "a sentence measured and no longer said is GONE")
    check(tool.ROUTING.words_moved([], ["b"]) == ([], ["b"]),
          "a sentence said today and never measured is FRESH")

    where = tempfile.mkdtemp()
    try:
        live, dead = REAL_SAYS[0], "a sentence this skill has never said"
        io.open(os.path.join(where, "routing-2000-01-01.json"), "w",
                encoding="utf-8").write(json.dumps({
                    "taken": "2000-01-01", "revit": "2024",
                    "index": {"md5": "0" * 32},
                    "skills": {REAL_ID: [
                        [live, "COUNT_ELEMENTS", "READ", "identity", "reach"],
                        [dead, "MIRROR_ELEMENTS", "MODIFY", "hybrid",
                         "crossing"]]}}))

        rows, _, meta = tool.collect(recordings=where)
        card = [one for one in rows if one["id"] == REAL_ID][0]
        check(meta.get("file") == "routing-2000-01-01.json",
              "the written recording is the one that was read")
        check(card["words_moved"]["gone"] == [dead],
              "the phrase the skill no longer says is named as GONE")
        check(sorted(card["words_moved"]["fresh"]) == sorted(REAL_SAYS[1:]),
              "and every sentence it says that was never measured is FRESH")
        check(card["words"]["crossing"] == [dead],
              "the record KEEPS the crossing it found")
        check(card["words"]["crossing_live"] == [],
              "and a crossing on a sentence nobody says any more is not live")
        others = [one for one in rows if one["id"] != REAL_ID]
        check(all(one["words"] is None and one["words_moved"] is None
                  for one in others),
              "a skill the recording does not cover stays NOT MEASURED")

        # THE DANGER IS NEVER SUPPRESSED BY THE STALENESS. A crossing on a
        # sentence the skill STILL says survives an out-of-date recording.
        io.open(os.path.join(where, "routing-2000-01-02.json"), "w",
                encoding="utf-8").write(json.dumps({
                    "taken": "2000-01-02", "index": {"md5": "0" * 32},
                    "skills": {REAL_ID: [
                        [live, "MIRROR_ELEMENTS", "MODIFY", "hybrid",
                         "crossing"]]}}))
        rows, _, _ = tool.collect(recordings=where)
        card = [one for one in rows if one["id"] == REAL_ID][0]
        check(card["words_moved"]["fresh"] == REAL_SAYS[1:],
              "the recording is still out of date")
        check(card["words"]["crossing_live"] == [live],
              "and the crossing it found on a live sentence is STILL live")

        # A RECORDING THAT LOOKS PERFECT ON ITS OWN. Every sentence it holds
        # reaches, so `reach == total` and nothing but the staleness keeps
        # this card out of the skills counted as whole.
        io.open(os.path.join(where, "routing-2000-01-03.json"), "w",
                encoding="utf-8").write(json.dumps({
                    "taken": "2000-01-03", "index": {"md5": "0" * 32},
                    "skills": {REAL_ID: [
                        [live, "COUNT_ELEMENTS", "READ", "identity",
                         "reach"]]}}))
        rows, _, _ = tool.collect(recordings=where)
        card = [one for one in rows if one["id"] == REAL_ID][0]
        check(card["words"]["reach"] == card["words"]["total"] == 1,
              "the recording reads perfectly on its own - every word it "
              "holds reaches")

        # THE WHOLE PAGE, with that recording, through main(). The output is
        # redirected BEFORE the module is loaded, because OUT is read at
        # import - setting it after writes the real skill-catalog.html, which
        # is a suite changing the tree it is testing (row 142).
        out = os.path.join(where, "page.html")
        keep_out = os.environ.get("HERON_SKILL_CATALOG_OUT")
        os.environ["HERON_SKILL_CATALOG_OUT"] = out
        try:
            again = load_tool()
            again.RECORDINGS = where
            check(again.main() == 0, "the page generates against it")
        finally:
            if keep_out is None:
                os.environ.pop("HERON_SKILL_CATALOG_OUT", None)
            else:
                os.environ["HERON_SKILL_CATALOG_OUT"] = keep_out
        check(os.path.exists(out) and os.path.abspath(again.OUT) == out,
              "and it went where it was pointed, not over the real page")
        text = io.open(out, encoding="utf-8").read()
        payload = json.loads(re.search(r"const DATA = (\{.*\});",
                                       text, re.S).group(1))
        check("OUT OF DATE" in payload["subtitle"],
              "and the subtitle says a recording is OUT OF DATE")
        check("OUT OF DATE" in text,
              "the page can print it on the card too")
        stale = [one for one in payload["rows"]
                 if one["words_moved"] and (one["words_moved"]["gone"]
                                            or one["words_moved"]["fresh"])]
        check(len(stale) == 1 and stale[0]["id"] == REAL_ID,
              "exactly the skill whose words moved, and no other")
        # A COMPLAINT WITH NO STALENESS, WHICH IS WHERE THE SUBTITLE BROKE.
        # The two caveats were written as `shaky + stale if stale else ""`,
        # which Python reads as `(shaky + stale) if stale else ""` - so with
        # no stale recording the COMPLAINT sentence vanished with it. The
        # page computed the number and dropped it, inside the change that
        # added the row about pages dropping what they computed.
        io.open(os.path.join(where, "routing-2000-01-04.json"), "w",
                encoding="utf-8").write(json.dumps({
                    "taken": "2000-01-04", "index": {"md5": "0" * 32},
                    "skills": {REAL_ID: [
                        [one, "COUNT_ELEMENTS", "READ", "hybrid", "reach",
                         ["a coin toss"] if one == live else []]
                        for one in REAL_SAYS]}}))
        rows, _, _ = tool.collect(recordings=where)
        card = [row for row in rows if row["id"] == REAL_ID][0]
        check(not card["words_moved"]["gone"]
              and not card["words_moved"]["fresh"],
              "a recording covering every sentence is NOT out of date")
        check(card["words"]["reach_unsettled"] == [live],
              "and the one complaint is on a sentence that reached")

        keep_out = os.environ.get("HERON_SKILL_CATALOG_OUT")
        out2 = os.path.join(where, "page2.html")
        os.environ["HERON_SKILL_CATALOG_OUT"] = out2
        try:
            third = load_tool()
            third.RECORDINGS = where
            check(third.main() == 0, "the page generates")
        finally:
            if keep_out is None:
                os.environ.pop("HERON_SKILL_CATALOG_OUT", None)
            else:
                os.environ["HERON_SKILL_CATALOG_OUT"] = keep_out
        said = json.loads(re.search(
            r"const DATA = (\{.*\});",
            io.open(out2, encoding="utf-8").read(), re.S).group(1))["subtitle"]
        check("THE RETRIEVER COMPLAINED" in said,
              "the subtitle says so WITHOUT a stale recording beside it")
        check("OUT OF DATE" not in said,
              "and does not claim a staleness it has not got")

        # AND IT IS NOT COUNTED AS REACHING. This is the whole point of the
        # exclusion: the recording says every word it holds reaches, and the
        # skill has since grown three the recording never saw.
        check("0 have every word reaching" in payload["subtitle"],
              "a recording that reads perfectly and is out of date counts "
              "as NEITHER whole nor short - it counts as out of date")
    finally:
        shutil.rmtree(where)
    # WHAT THE RETRIEVER SAID, CARRIED ONTO THE CARD (register row 157).
    # A recording taken before prove-skill recorded it has FIVE fields and
    # must still read - an old one reports no complaint, which is the
    # absence of one and not a clean answer.
    five = tool.said([["a", "X", "READ", "identity", "reach"]], ["X"])
    check(five["unsettled"] == [] and five["reach_unsettled"] == [],
          "a five-field recording reads, and claims no complaint")
    six = tool.said([["a", "X", "READ", "hybrid", "reach", ["a coin toss"]],
                     ["b", "Y", "MODIFY", "hybrid", "crossing", ["a coin toss"]],
                     ["c", "Z", "READ", "identity", "reach", []]], ["X"])
    check(six["unsettled"] == ["a", "b"],
          "every sentence the retriever complained about is named")
    check(six["reach_unsettled"] == ["a"],
          "and the ones that REACHED are counted apart - a crossing is "
          "already loud, a quiet reach is the one nobody looks at")
    check(six["reach"] == 2,
          "a complaint does not take a sentence out of `reach` - it DID "
          "reach, and whether that spends it is a reader's call")
    check(six["landed"][0]["told"] == ["a coin toss"],
          "and the retriever's own words ride on the sentence they are about")

    check(tool.ROUTING.words_moved.__module__
          == tool.ROUTING.classify.__module__,
          "the rule lives beside classify(), in the module both tools read "
          "it from - one copy, or they disagree about the same skill")
    check(tool.ROUTING.words_moved.__doc__
          and "store" in tool.ROUTING.words_moved.__doc__.lower(),
          "and the rule says in its own words why it reads the phrases and "
          "not the store")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the chain underneath is the fact, and the words are\n        the other half of it")
    return 0


if __name__ == "__main__":
    sys.exit(main())
