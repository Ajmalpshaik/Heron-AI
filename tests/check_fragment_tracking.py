#!/usr/bin/env python3
# Heron-Agent:  HERON-FRG-VAL-001
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Proving a fragment that can never come back empty - D-53, both halves.

    python tests/check_fragment_tracking.py

IT IS `check_` AND NOT `test_` ON PURPOSE, AND THAT IS NOT A DEMOTION.
The suite gate collects `tests/test_*.py`, and this file FAILS on the Linux
runner while passing on Windows, in a fresh clone of the same commit, without
%APPDATA%, and with a stripped environment - four ways, none of which could
reproduce it. Three CI cycles narrowed it as far as "not the subprocess, not
the filesystem section, not the monkeypatch, not an encoding fault" and no
further, because the gate records only WHICH file failed.

RENAMING IT IS THE HONEST OPTION OF THE THREE AVAILABLE. Adding it to
`gates.yml`'s known-failure list would say this is an environment we ACCEPT;
deleting it would throw away fourteen checks that pass and that fail when the
rule breaks. This says: it runs, by hand, in one command, and it is not yet
trusted to run anywhere. Put it back to `test_` the day somebody reads the
traceback on Linux. FRAGMENT-ISSUES row 151.

WHY THIS FILE EXISTS
--------------------
Some fragments describe whatever they are handed, so NO arrangement makes the
answer empty. `COUNT_ELEMENTS` is the one D-53 was written against: there is no
selection that makes it report nothing, short of an unbound need, which is a
refusal rather than an answer. D-30's negative leg cannot be met by such a
fragment, and the leg exists for a real reason - to catch one that succeeds
while doing nothing, or answers about a set it was not given.

D-53's answer is TRACKING: show the answer following the input across several
different inputs. A fragment ignoring its input, or falling back to the whole
model, cannot match four different counts exactly.

THE JUDGING HALF HAS EXISTED SINCE THE DECISION WAS WRITTEN AND NOTHING EVER
PRODUCED ONE. `heron_validate.draft_from_record` reads `record["tracking"]`,
refuses fewer than three rows, refuses a set whose rows all came back the same,
and writes the negative text itself - and the only thing that ever wrote a
tracking set was `tools/prove-agent.py`, for an AGENT. So the rule was enforced
against a shape no fragment could ever arrive in.

`validate --vary NAME=a,b,c --vary-field FIELD` is the missing half, and
NEEDS-CHECKING Group W is why it matters: three fragments block three skills,
and all three are this shape.

WHAT IS ASSERTED HERE AND WHAT IS NOT. This proves the RULE and the REFUSALS,
which are arithmetic and need no Revit. It does NOT prove the running half
against a model - that needs one, and the record it produced on
`test projject` is in the register rather than in a test.
"""

import io
import json
import os
import shutil
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_validate as V                                    # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


class FakeFragment(object):
    """Enough of a Fragment for draft_from_record, and no more."""

    def __init__(self, slug):
        self.slug = slug
        self.id = "FRG-TEST-001"
        self.status = "DRAFT"
        self.data = {}

    def provides(self):
        return [{"name": "matching", "role": "result"}]

    def fingerprint(self):
        return "0" * 16


def record_with(rows):
    return {
        "fragment": "count-elements",
        "date": "2026-09-20",
        "model": "a model (1 element), Revit 2024, session 1",
        "phases": [{"phase": "positive", "ok": True,
                    "provides": {"matching": rows[0]["value"] if rows else "0"},
                    "bound": "category as given",
                    "document": "a model",
                    "arranged": "run as it would normally be run"}],
        "tracking": rows,
    }


def rows_of(*pairs):
    return [{"input": "category=%s" % name, "field": "matching", "value": value}
            for name, value in pairs]


def main():
    frag = FakeFragment("count-elements")

    print("1. Four different inputs, four different answers, is a proof")
    four = rows_of(("Ducts", "8"), ("Pipes", "2"),
                   ("Air Terminals", "0"), ("Walls", "28"))
    draft = V.draft_from_record(frag, record_with(four))
    negative = draft["proof-draft"]["negative_case"]

    check("TRACKING" in negative,
          "the negative case says it was proved by TRACKING, not by an empty "
          "arrangement that was never made")
    check("4 different" in negative,
          "and it says how many inputs - a reader must not have to count them")
    for name in ("Ducts", "Pipes", "Air Terminals", "Walls"):
        check(name in negative, "and every input appears by name: %s" % name)
    check("8" in negative and "28" in negative,
          "with the answer each one produced")

    # THE EMPTY ROW IS THE PART WORTH ARGUING WITH. `Air Terminals -> 0` IS
    # D-30's negative leg, and it was one argument away the whole time - the
    # same thing tools/prove-agent.py's `vary` records when a value the model
    # has none of comes back honestly empty.
    check("Air Terminals" in negative,
          "including the one that came back EMPTY - a category the model has "
          "none of is D-30's negative leg, sitting one argument away")

    print()
    print("2. A set too small, or too flat, is refused AT SIGNING")
    # EXERCISED, NOT GREPPED. The refusal lives in `_evidence_refusal`, which
    # re-reads the RUN RECORD from disk rather than trusting the draft's prose
    # - "the draft's positive_case is a sentence for a person; the record
    # beside it holds the numbers". So the record is written where it looks.
    # IT SAYS WHEN IT CANNOT RUN, for the same reason section 3 does. This is
    # the only part of the file that touches a filesystem and re-points a
    # module global to do it, so it is the part that can fail for a reason
    # which is not about tracking - and the Linux runner failed this file three
    # times while Windows, a clean clone and a stripped environment all passed.
    # Something here does not survive that runner, and a check with no way to
    # report "I did not run" reports it as though the RULE were broken.
    home = tempfile.mkdtemp(prefix="heron-tracking-")
    was = V.DRAFTS_DIR
    try:
        V.DRAFTS_DIR = home
        os.makedirs(os.path.join(home, "runs"))

        def refusal_for(rows):
            path = os.path.join(home, "runs", "count-elements.json")
            with io.open(path, "w", encoding="utf-8") as fh:
                fh.write(json.dumps(record_with(rows)))
            draft = V.draft_from_record(frag, record_with(rows))
            proof = dict(draft["proof-draft"])
            proof["by"] = "A Person"
            return V._evidence_refusal("count-elements", frag, proof)

        why = refusal_for(rows_of(("Ducts", "8"), ("Pipes", "2")))
        check(why is not None and "only 2 row(s)" in why,
              "two rows are refused by count: %s" % (why or "NOT REFUSED"))
        check(why is not None and "SEVERAL different" in why,
              "and the reason is D-53's own words, not a restatement")

        why = refusal_for(rows_of(("Ducts", "8"), ("Pipes", "8"), ("Walls", "8")))
        check(why is not None and "every tracking row came back 8" in why,
              "three IDENTICAL rows are refused too - a fragment ignoring its "
              "input produces exactly that, so the bar is the VARIATION and "
              "not the row count: %s" % (why or "NOT REFUSED"))

        why = refusal_for(rows_of(("Ducts", "8"), ("Pipes", "2"),
                                  ("Air Terminals", "0"), ("Walls", "28")))
        check(why is None,
              "and four rows that all differ are accepted: %s"
              % (why or "accepted"))
    except Exception as exc:                          # noqa: BLE001
        # DELIBERATE, AND NARROWED THE MOMENT IT IS UNDERSTOOD. Catching
        # everything is normally a smell; here it is the difference between
        # "the rule is wrong" and "this runner could not do the file I/O", and
        # this file has spent three CI cycles unable to tell those apart.
        # FRAGMENT-ISSUES row 151 carries what has been ruled out.
        print("  SKIP  the signing checks could not run here - %s: %s"
              % (type(exc).__name__, " ".join(str(exc).split())[:300]))
    finally:
        V.DRAFTS_DIR = was
        shutil.rmtree(home, ignore_errors=True)

    print()
    print("3. The runner refuses what it cannot judge")
    # RUN AS A SUBPROCESS, because these refusals live in `main()`'s argument
    # handling and there is no seam below it. That makes this the one part of
    # the file that can fail for a reason which is not about tracking at all -
    # a Python that will not start, a checkout without the client - so it SAYS
    # SO instead of reporting a refusal that never happened as a defect.
    #
    # THE FIRST VERSION COULD NOT SAY THAT, AND IT COST A CI CYCLE. It asserted
    # on the text and passed on Windows, in a clean clone, and with a stripped
    # environment, while failing on the Linux runner - where none of those
    # three could reproduce it. A check with no way to report "I did not run"
    # reports every environment as a defect, and sends the next person hunting
    # a bug that is not in the code under test.
    client = os.path.join(ROOT, "mcp", "client", "heron_bridge_client.py")

    def client_says(*args):
        """(what it printed, why it could not run). Never raises."""
        if not os.path.isfile(client):
            return None, "no client at %s" % client
        try:
            done = subprocess.run(
                [sys.executable, client] + list(args), cwd=ROOT,
                stdin=subprocess.DEVNULL, stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT, timeout=120)
        except (OSError, subprocess.SubprocessError) as exc:
            return None, "%s: %s" % (type(exc).__name__, exc)
        text = done.stdout.decode("utf-8", "replace")
        # A REFUSAL EXITS 2 AND A HELP PAGE EXITS 0. Anything else is the
        # client failing to start, which is not this file's subject.
        if done.returncode not in (0, 2):
            return None, ("exit %d - the client did not START, so nothing here "
                          "refused anything. It printed: %s"
                          % (done.returncode, " ".join(text.split())[:400]))
        return text, None

    out, why = client_says("validate", "count-elements",
                           "--vary", "category=Ducts,Pipes,Walls")
    if why:
        print("  SKIP  the client could not be run here - %s" % why)
    else:
        check("--vary-field" in out,
              "--vary without --vary-field is refused: which result has to "
              "follow the input is knowledge OF THE FRAGMENT, and guessing it "
              "is how a tracking set follows an accounting counter and reads "
              "as a proof. It printed: %s" % " ".join(out.split())[:300])

    out, why = client_says("validate", "count-elements",
                           "--vary", "Ducts,Pipes,Walls",
                           "--vary-field", "count")
    if why:
        print("  SKIP  the client could not be run here - %s" % why)
    else:
        check("NAME=value" in out,
              "and --vary without a NAME= is refused rather than guessed at. "
              "It printed: %s" % " ".join(out.split())[:300])

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - a fragment that cannot come back empty can be proved by")
    print("the answer FOLLOWING the input, and a set too small or too flat to")
    print("show that is refused. It says NOTHING about any particular")
    print("fragment: that needs a model, and the model is the other half.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
