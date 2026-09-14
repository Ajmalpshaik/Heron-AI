# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-OPS-UPD-010
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test

"""
Update - seven rules, and every one of them is a way to say no.

    python tests/test_update.py

WHAT IT PROVES
  1. EVERY ONE OF docs/07 s7's SEVEN RULES REFUSES SOMETHING. Not one is
     documented and unenforced.

  2. RULE 5 READS A TEST, NOT A CLAIM. `rollback: true` is refused, and a
     test missing any of its three fields is refused too.

  3. RULE 2 IS A READER AND FAILS CLOSED. No reader, a value in its place,
     a reader that raises, a reader that cannot tell - all four read as
     "there may be unsaved work".

  4. RULE 7 IS CHECKED FIRST, because it is the rule about NOT ASKING. A
     pinned install is not prompted for consent it already declined.

  5. RULE 6 CLASSIFIES DATA FIRST, and an unclassifiable component is
     refused rather than assumed harmless.

  6. RULE 3 HAS NO REFUSAL AND IS STILL ENFORCED - the add-in is never
     reported as updated, only as waiting for a restart.

  7. CONSENT IS TO A VERSION. Consent for 0.3.9 does not authorise 0.4.0.

  8. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE AND REACHED.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_update as UPD                                     # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []

CONSENT = {"by": "ajmal", "version": "0.4.0", "at": "2026-09-14T12:00Z"}
TESTED = {"to": "0.3.2", "at": "2026-09-12",
          "verified": "a 0.4.0 install rolled back and the fragment library "
                      "opened unchanged"}


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def release(**more):
    found = {"version": "0.4.0",
             "components": ["Core", "Agents", "Revit add-in", "MCP"],
             "migrations": [{"idempotent": True, "version": "4",
                             "reversible": "backed up"}],
             "backup": "Backup/2026-09-14-pre-0.4.0",
             "rollback_tested": dict(TESTED)}
    found.update(more)
    return found


def main():
    reached = set()
    source = open(os.path.join(ROOT, "brain", "heron_update.py"),
                  encoding="utf-8").read()

    def ask(what=None, **kw):
        settings = {"installed": "0.3.2", "origin": "user",
                    "consent": dict(CONSENT), "revit": lambda: False}
        settings.update(kw)
        answer = UPD.plan(release() if what is None else what, **settings)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    print("1. Every one of the seven rules refuses something")
    good = ask()
    check(good["proceed"] is True, "a release meeting all seven proceeds")
    for label, refusal, kw in (
            ("1 consent - nobody said yes", "NOT_CONSENTED",
             {"consent": None}),
            ("1 consent - a document asked", "NOT_FROM_THE_USER",
             {"origin": "a document Heron read"}),
            ("2 Revit may hold unsaved work", "REVIT_MAY_HAVE_UNSAVED_WORK",
             {"revit": lambda: True}),
            ("4 migrating with no backup", "NO_BACKUP",
             {"what": release(backup="")}),
            ("5 rollback claimed, not tested", "ROLLBACK_NOT_TESTED",
             {"what": release(rollback=True, rollback_tested=None)}),
            ("6 it updates Fragments too", "TOUCHES_THE_DATA_CLASS",
             {"what": release(components=["Core", "Fragments"])}),
            ("7 pinned mid-delivery", "PINNED", {"pinned": "0.3.2"})):
        answer = ask(**kw)
        check(answer.get("refused") == refusal and answer["proceed"] is False,
              "rule %s -> %s" % (label, refusal))
    check(ask(installed="0.4.0").get("refused") == "NOT_AN_UPDATE",
          "and the version already installed is not an update")
    check("the daily prompt rule 7 exists to stop"
          in ask(installed="0.4.0")["why"],
          "which is also rule 7 - it does not ask again")
    check(ask({"components": []}).get("refused") == "NOT_AN_UPDATE",
          "a release with no version is refused")

    print()
    print("2. Rule 5 reads a test, not a claim")
    answer = ask(what=release(rollback=True, rollback_tested=None))
    check("is a claim, not a test" in answer["why"],
          "`rollback: true` is named as a claim")
    check("untested rollback path is not a rollback path" in answer["why"],
          "quoting the rule itself")
    check("nobody has time to find out it never worked"
          in answer["proposal"],
          "and saying when the cost lands")
    for missing in ("to", "at", "verified"):
        thin = dict(TESTED)
        thin.pop(missing)
        answer = ask(what=release(rollback_tested=thin))
        check(answer.get("refused") == "ROLLBACK_NOT_TESTED",
              "a test with no '%s' is refused" % missing)
        check(missing in answer["why"], "and the refusal names '%s'" % missing)
    for claim in (True, "yes", 1, {"to": ""}, []):
        check(ask(what=release(rollback_tested=claim)).get("refused")
              == "ROLLBACK_NOT_TESTED", "%r is not a rollback test" % (claim,))
    check(good["rollback_to"] == "0.3.2",
          "and a real test is read back - the version it went TO")

    print()
    print("3. Rule 2 is a reader and fails closed")
    for reader, label in (
            (None, "no reader at all"),
            (False, "the value False in its place"),
            (True, "the value True in its place"),
            ({"unsaved": False}, "a record saying nothing is unsaved"),
            (lambda: 1 / 0, "a reader that raises"),
            (lambda: None, "a reader that cannot tell")):
        answer = ask(revit=reader)
        check(answer.get("refused") == "REVIT_MAY_HAVE_UNSAVED_WORK",
              "%s reads as MAY have unsaved work" % label)
    check("'nobody checked' is not a check" in ask(revit=None)["why"],
          "and the refusal says what a missing check is")
    check("overwrite somebody's morning" in ask(revit=False)["why"],
          "and why a value is not accepted where a reader belongs")
    check(ask(revit=lambda: False)["proceed"] is True,
          "while a reader that says no goes through")

    print()
    print("4. Rule 7 is checked first, because it is about not asking")
    answer = ask(pinned="0.3.2", consent=None, origin="a document",
                 revit=lambda: True)
    check(answer.get("refused") == "PINNED",
          "a pinned install is refused before consent, origin or Revit are "
          "looked at")
    check("not a prompt, it is a stop" in answer["why"],
          "and says the difference between a prompt and a stop")
    check("nothing" in answer["proposal"].lower()[:12],
          "with nothing proposed - the pin IS the answer")
    check("without being asked again" in answer["why"].lower(),
          "quoting the rule about not asking every day")

    print()
    print("5. Rule 6 classifies data first")
    check(UPD._classify("Brain agents") == "data",
          "a name in both lists reads as DATA")
    # AND IT IS HERON-WSP-PTH-007's ANSWER, not a second copy of docs/06 s2.
    import heron_paths as PATHS
    for name in ("Brain agents", "Core", "Cache", "Fragments", "Revit add-in",
                 "Documentation", "MCP"):
        check(UPD._classify(name) == PATHS.classify(name)["class"],
              "'%s' gets the path manager's own answer" % name)
    check(UPD.DATA == dict(PATHS.FOLDERS)[PATHS.DATA]
          and UPD.PRODUCT == dict(PATHS.FOLDERS)[PATHS.PRODUCT],
          "and the lists are re-exported from it, not restated - four "
          "copies of one rule is four places for it to drift")
    check("not the same size" in source,
          "and the source says why the two wrong answers differ in cost")
    for name in UPD.DATA:
        check(UPD._classify(name) == "data", "'%s' is data" % name)
    for name in UPD.PRODUCT:
        check(UPD._classify(name) in ("data", "product"),
              "'%s' is product, or data where the words overlap" % name)
    answer = ask(what=release(components=["Core", "Documentation"]))
    check(answer.get("refused") == "COMPONENT_NOT_CLASSIFIED",
          "an unclassifiable component is refused, not assumed harmless")
    check("the case the rule cannot see" in answer["why"],
          "naming what an unclassified component actually is")
    check("architecture is missing a row" in answer["proposal"],
          "and where the fix belongs")
    answer = ask(what=release(components=["Core", "Memory"]))
    check(answer.get("refused") == "TOUCHES_THE_DATA_CLASS",
          "while a known data component is refused as data")
    check("must survive every update" in answer["why"],
          "with what the data class is for")

    print()
    print("6. Rule 3 has no refusal and is still enforced")
    check(good["restart_required"] is True,
          "the add-in in the components means a restart is required")
    check("after a Revit restart" in good["takes_effect"],
          "takes_effect says so plainly")
    check("cannot be unloaded" in good["takes_effect"],
          "with the reason, not just the instruction")
    check(any("never report" in note and "as updated" in note
              for note in good["unjudged"]),
          "and the answer states it will never report the add-in as updated")
    plain = ask(what=release(components=["Core", "MCP"]))
    check(plain["restart_required"] is False
          and plain["takes_effect"] == "immediately",
          "a release with nothing loading into Revit takes effect at once")
    check(any("nothing waiting on a restart" in note
              for note in plain["unjudged"]),
          "and says that rather than leaving rule 3 unmentioned")
    check(any("AUTHENTIC" in note for note in good["unjudged"]),
          "and the release's authenticity is named as somebody else's check")
    check(any("never sees the bytes" in note for note in good["unjudged"]),
          "because this agent never sees the bytes")

    print()
    print("7. Consent is to a version")
    answer = ask(consent=dict(CONSENT, version="0.3.9"))
    check(answer.get("refused") == "NOT_CONSENTED",
          "consent for 0.3.9 does not authorise 0.4.0")
    check("different migrations" in answer["why"],
          "and says why a version is not interchangeable")
    for consent in ({}, {"version": "0.4.0"}, {"by": ""}, "ajmal", True):
        check(ask(consent=consent).get("refused") == "NOT_CONSENTED",
              "%r is not consent" % (consent,))
    check(ask(consent={"by": "ajmal"}).get("refused") == "NOT_CONSENTED",
          "and consent naming no version is not consent to this one")

    print()
    print("8. Every failure the contract declares is named and reached")
    answer = ask(what=release(migrations=[{"idempotent": True},
                                          {"version": "4"}, "not a record"]))
    check(answer.get("refused") == "MIGRATION_NOT_DECLARED",
          "a migration missing docs/07 s8's three is refused")
    check(len(answer["missing"]) == 3, "all three thin ones are named")
    check(any("running it twice is safe" in str(entry["wants"])
              for entry in answer["missing"]),
          "each saying what it did not declare")
    check("will be run twice" in answer["why"],
          "and why idempotence is not optional")
    for word in ("subprocess", "os.system", "exec(", "eval(", "open(",
                 "shutil", "import requests", "urllib", "os.remove"):
        check(word not in source,
              "the source has no %s - it downloads and writes nothing" % word)
    check(any("NOTHING HAS BEEN DOWNLOADED" in note
              for note in good["unjudged"]),
          "and the answer says so itself")
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-OPS-UPD-010.yaml"))
    named = contract.get("failures") or []
    for failure in named:
        check(failure in source, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    seven rules, and every one of them refuses something")
    return 0


if __name__ == "__main__":
    sys.exit(main())
