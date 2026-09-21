# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The brain's half of the audit trail - never fatal, and never a guess.

    python tests/test_audit.py

WHY THIS EXISTS, AND WHY IT DID NOT UNTIL NOW
----------------------------------------------
`brain/heron_audit.py` had NO SUITE. Nothing under `tests/` imported it, and
the BUILD ORDER check in `tools/check-gaps.py` covers the fourteen steps
docs/27 describes - this file carries `Heron-Step: 17`, so nothing noticed.

It is not an incidental module. It is Golden Rule 14's requirement for the
half of Heron that never reaches Revit, D-62's deliverable, and it writes
into a file that is APPEND-ONLY AND NEVER PRUNED - so a line written wrong
today is wrong for ever, and its own docstring argues at length about
exactly that. None of that argument was held by anything. Row 5b-91.

WHAT IT PROVES
  1. NEVER FATAL, AND THAT INCLUDES BUILDING THE ROW. A `numbers` value
     that is not a whole number costs that one field - named on the line,
     because Golden Rule 14 does not allow a silent discard - and never the
     line, and never the request it was only supposed to describe.

  2. NUMBERS ARE WRITTEN AS NUMBERS. The file's founding lesson, from the
     day `ms` went in quoted and a reader compared "9" against "6620" as
     text. The log is never pruned, so a quoted number is permanent.

  3. THE USER'S SENTENCE IS NEVER WRITTEN. Not by any of the four writers,
     and not by `record` unless a caller hands it one under its own name.

  4. EVERY ok=false LINE CARRIES A CODE, AND `heron_gaps` FILES IT AS A
     CORRECT REFUSAL. This is the neighbour's own founding mistake - its
     loudest error was the executor behaving correctly - so the proof runs
     the codes through `analyse()` rather than asserting the strings.

  5. A REFUSAL IS RECORDED, NOT DROPPED. A path that refuses every time
     must not look like a path nobody used.

  6. TWO WRITERS, ONE TRAIL. The whole answer to Q-44 is that the reader
     already merges: `audit-brain-YYYYMM.jsonl` matches the glob, and an
     add-in file and this one come back as one list sorted by `at`.

  7. NOWHERE TO WRITE IS NOT AN ERROR, and it is reported rather than
     raised - which is what makes a developer machine cost nothing.
"""

import io
import json
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_audit as AUDIT                                    # noqa: E402
import heron_gaps as GAPS                                      # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def lines(directory):
    """Every row this writer put in the directory, in file order."""
    found = []
    for name in sorted(os.listdir(directory)):
        if not name.endswith(".jsonl"):
            continue
        for line in io.open(os.path.join(directory, name), encoding="utf-8"):
            line = line.strip()
            if line:
                found.append(json.loads(line))
    return found


def main():
    source = io.open(os.path.join(ROOT, "brain", "heron_audit.py"),
                     encoding="utf-8").read()
    yard = tempfile.mkdtemp(prefix="heron-audit-test-")
    try:
        here = os.path.join(yard, "audit")
        os.makedirs(here)

        print("1. never fatal - and that includes building the row")
        # ASK BEFORE YOU CALL is not needed here: `record` has existed all
        # along and it is its BEHAVIOUR that changed. What the old code did
        # was raise, so each of these is the check and the negative proof at
        # once - run against the module as it stood, the first four raise.
        raised = []
        for label, numbers in (("a whole number", {"n": 3}),
                               ("a float, truncated", {"ms": 12.7}),
                               ("a numeric string", {"ms": "12"}),
                               ("a word", {"ms": "fast", "n": 3}),
                               ("not a number at all", {"ms": [], "n": 3}),
                               ("NaN", {"ms": float("nan"), "n": 3}),
                               ("infinity", {"ms": float("inf"), "n": 3})):
            try:
                wrote = AUDIT.record("t", True, numbers=numbers,
                                     directory=here)
                check(wrote is True, "%s: the line is written" % label)
            except Exception as exc:                  # noqa: BLE001
                raised.append(label)
                check(False, "%s: %s ESCAPED - the docstring says NEVER "
                             "FATAL" % (label, type(exc).__name__))
        check(not raised,
              "nothing escaped a function whose promise is that nothing does")

        written = lines(here)
        check(len(written) == 7, "seven lines, one per call (%d)" % len(written))
        kept = [r for r in written if "dropped" in r]
        check(len(kept) == 4, "four of them dropped a field (%d)" % len(kept))
        # `kept` and not `all(...)` alone: an all() over an empty list is
        # True, so against a module that dropped nothing these two would
        # have passed while saying nothing at all.
        check(kept and all(r.get("dropped") == "ms" for r in kept),
              "and each NAMES the field it dropped - Golden Rule 14 does not "
              "allow a silent discard")
        check(kept and all(r.get("n") == 3 for r in kept),
              "while the number beside it survives: a bad field costs that "
              "field, not the line")

        print("\n2. numbers are written as numbers")
        # The file's own founding lesson: `ms` went in quoted once and a
        # reader compared "9" against "6620" as text. Never pruned, so those
        # lines are wrong for ever.
        numeric = [r for r in written if "ms" in r]
        check(numeric and all(isinstance(r["ms"], int) for r in numeric),
              "every `ms` that was written is an int in the JSON, not a "
              "string (%s)" % ", ".join(repr(r["ms"]) for r in numeric))
        check(any(r["ms"] == 12 for r in numeric),
              "12.7 was truncated to 12 rather than quoted or refused")
        check(all(isinstance(r.get("ok"), bool) for r in written),
              "and `ok` is a JSON boolean, not the word")

        print("\n3. the user's sentence is never written")
        for name in ("lookup", "resolve", "context", "catalogue"):
            fn = getattr(AUDIT, name)
            args = fn.__code__.co_varnames[:fn.__code__.co_argcount]
            check(not any(a in ("request", "sentence", "text", "words",
                                "utterance", "query") for a in args),
                  "%s() takes no sentence - its arguments are %s"
                  % (name, ", ".join(args)))
        check("THE SENTENCE IS NOT RECORDED" in source,
              "and the module says so where somebody changing it will read it")

        print("\n4. every ok=false line carries a code heron_gaps can file")
        # THE NEIGHBOUR'S OWN FOUNDING MISTAKE, so this runs the codes
        # through analyse() rather than asserting the strings: a request
        # Heron honestly could not answer used to inflate the failure count
        # in the very report built to tell defects from refusals apart.
        talk = os.path.join(yard, "trail")
        os.makedirs(talk)
        AUDIT.lookup(route="hybrid", capability=None, provider=None,
                     candidates=0, excluded=2, directory=talk)
        AUDIT.resolve(capability="tag-elements", provider=None, ok=False,
                      directory=talk)
        AUDIT.context(path="STANDARDS", depth="deep", parts=0, characters=0,
                      refused="over budget", directory=talk)
        entries, skipped = GAPS.read(talk)
        check(skipped == 0 and len(entries) == 3,
              "three lines read back with nothing skipped")
        check(all(r.get("ok") is False for r in entries),
              "all three are recorded as failures, because they are")
        found = GAPS.analyse(entries)
        check(not found["unclassified"],
              "and NONE of them lands in `unclassified`, which is where a "
              "correct refusal with no code would go: %s"
              % dict(found["unclassified"]))
        check(sum(found["refusals"].values()) == 3,
              "all three file as CORRECT REFUSALS (%d)"
              % sum(found["refusals"].values()))
        check(not found["defects"],
              "and none as a defect - nothing here was Heron's fault")
        for code in (AUDIT.NO_CAPABILITY, AUDIT.NO_PROVIDER,
                     AUDIT.CONTEXT_REFUSED):
            check(code in GAPS.CORRECT_REFUSALS,
                  "%s is a word the reader knows" % code)

        print("\n5. a refusal is recorded, not dropped - and D-52's excluded")
        refused = [r for r in entries if r["op"] == AUDIT.CONTEXT]
        check(len(refused) == 1 and refused[0].get("refused") == "over budget",
              "the context refusal carries its reason as well as its code")
        asked = [r for r in entries if r["op"] == AUDIT.LOOKUP]
        check(asked and asked[0].get("excluded") == 2,
              "and the lookup records what the version wall turned DOWN, not "
              "only what it found - D-52")

        print("\n6. two writers, one trail - the whole answer to Q-44")
        with io.open(os.path.join(talk, "audit-202609.jsonl"), "w",
                     encoding="utf-8") as handle:
            handle.write(json.dumps({"at": "2026-09-01T00:00:00.000Z",
                                     "op": "revit_select", "ok": True}) + "\n")
            handle.write(u"{ this line is truncated\n")
        merged, bad = GAPS.read(talk)
        check(len(merged) == 4,
              "the add-in's file and the brain's come back as ONE list (%d)"
              % len(merged))
        check(bad == 1, "and the truncated line costs one entry, not the report")
        stamps = [str(r.get("at", "")) for r in merged]
        check(stamps == sorted(stamps),
              "sorted by `at` across both files, which is what makes them one "
              "trail rather than two")
        check(os.path.basename(AUDIT.current_file(talk)).startswith(
                  "audit-brain-"),
              "and the brain's name matches the glob the reader already had: "
              "%s" % os.path.basename(AUDIT.current_file(talk)))

        print("\n7. nowhere to write is reported, not raised")
        check(AUDIT.current_file("") is None,
              "no directory means no file, said as None")
        check(AUDIT.record("t", True, directory="") is False,
              "and record() returns False rather than raising - which is why "
              "a developer machine with no APPDATA costs nothing")
        missing = os.path.join(yard, "not", "made", "yet")
        check(AUDIT.record("t", True, directory=missing) is True,
              "a directory that does not exist yet is created, not refused")
        check(os.path.isdir(missing), "and it really is there afterwards")

        print("\n8. no workflow id, and it says so rather than inventing one")
        check(all(r.get("workflow") == "" for r in merged
                  if str(r.get("op", "")).startswith("brain_")),
              "every brain line goes out with an empty `workflow`")
        check("HERON-MCP-LOG-010" in source and "deliberately NOT claimed" in source,
              "and the header claims no agent, because the row it serves says "
              "KEYED BY WORKFLOW ID and no workflow id reaches the brain")
    finally:
        shutil.rmtree(yard, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    a trail that cannot take down the request it records,\n"
          "        and a refusal the report files as a refusal")
    return 0


if __name__ == "__main__":
    sys.exit(main())
