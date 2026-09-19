#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-DEV-RVT-013
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
The Revit Test Agent's running half - many fragments proved in one pass.

    python tools/batch-prove.py jobs.yaml --dry-run     read it, run nothing
    python tools/batch-prove.py jobs.yaml               run it against Revit
    python tools/batch-prove.py jobs.yaml --only set-mep-size

WHY THIS EXISTS
---------------
Proving fragments one at a time costs a round trip each. On 2026-09-09 a
throwaway script proved fourteen in one pass - and then lived in a temp folder
and was retyped twice. This is that script, committed, with the two holes it
had closed rather than remembered.

WHAT IT IS NOT
--------------
**It never accepts and never promotes.** It runs `validate`, drafts what came
back, and judges both halves. Accepting a draft is `heron_validate.py accept`,
and it takes a person's name because that name is the signature
(`brain/proof-drafts/README.md`: *the machine never signs*). Running more
fragments faster only makes that signature matter more.

To be sure rather than to promise it, this reads every fragment's
`heron-status` before the batch and again after, and stops the run if one moved.
Nothing here writes it, so nothing should - and a guarantee that is checked is
worth more than one that is stated.

THE TWO HOLES, WHICH ARE THE WHOLE POINT
----------------------------------------
**1. It re-proved finished work.** The first batch was 15 of 16 fragments
already PROVEN, because the names were picked off a capability list rather than
filtered by status. A fragment at PROVEN or PRODUCTION is refused here, reported
`ALREADY`, and never sent to Revit. There is deliberately no flag to override
that: re-proving after the code changes is a STALENESS question, and
`tests/test_golden.py` already answers it by re-computing fingerprints.

**2. It passed fragments that had done nothing.** `heron_validate` judges
whether the NEGATIVE came back empty - which is D-30's leg, and which a fragment
that does nothing at all satisfies trivially. So the POSITIVE is judged too, and
by one rule: **a declared result has to have moved off zero.**

    `read-graphic-overrides` leaves `overrides` as an OverrideGraphicSettings
    OBJECT. Reading that as "not a count, therefore non-zero, therefore it did
    something" passed a fragment whose only real result, `withOverride`, was 0
    in BOTH legs. **Unreadable is not evidence of work** - the same rule
    `_as_count` already follows for the negative, applied to the positive.

WHERE THE JUDGING COMES FROM
----------------------------
`looks_empty` is IMPORTED from `brain/heron_validate.py`, not copied. The runner
and the drafter have to agree about what "empty" means, and the only way two
copies of that judgement stay in step is by not being two copies. Every lesson
that function carries - D-51's counts decide, D-52's accounting names, "0" is a
string, `(null)` is empty, a bare type name is a helper - arrives here for free
and stays arrived.

`tools/generate-fragment-catalog.py` deliberately does the opposite and keeps its
own regex. That is right for a page that DRAWS and wrong here: this file's whole
job is to judge, and judging differently from the drafter it writes into would be
the drift, not the fix.

THE ARRANGEMENT IS NOT THIS FILE'S JOB
--------------------------------------
Choosing which fragments to attempt, on which selection, with which values, and
what the negative case should be, is where every failure of 2026-09-09 came from
- not from the fragments and not from the runner. That is written down in
`.claude/skills/fragment-proving/SKILL.md` and it is the harder half.
"""

import argparse
import copy
import io
import json
import os
import subprocess
import sys
import time

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_fragment as HF                                     # noqa: E402

# The underscore names below - `_as_count` and `_is_helper_object` - are
# reached into deliberately. They are the validation agent's private
# workings, and normally that would be a reason to leave them alone; here the
# alternative is a second copy of the reasoning they carry, judging the same run
# records differently from the drafts written beside them. A private name shared
# is a smaller cost than a public judgement forked.
import heron_validate as HV                                     # noqa: E402

try:
    import yaml
except ImportError:                                          # pragma: no cover
    sys.stderr.write("A job file is YAML, so this needs PyYAML:\n"
                     "  pip install --user pyyaml\n")
    raise

CLIENT = os.path.join(ROOT, "mcp", "client", "heron_bridge_client.py")
VALIDATOR = os.path.join(ROOT, "brain", "heron_validate.py")
RUNS = os.path.join(ROOT, "brain", "proof-drafts", "runs")

# Statuses that mean the work is finished and must not be repeated. See hole 1.
FINISHED = ("PROVEN", "PRODUCTION")

# Long enough for the worst honest case - two setup steps and one run per phase,
# each capped at 180 seconds by the bridge itself - and short enough that a
# fragment grinding through 307 elements is reported rather than waited out.
# A timeout says nothing about the fragment; it says the selection was too big.
DEFAULT_TIMEOUT = 900


# ---------------------------------------------------------------------------
# 1. The verdicts, and what each one means
# ---------------------------------------------------------------------------
#
# Every one was met on a real run rather than imagined, and the list below is
# the count - typing one into this comment is how a number here goes stale the
# day after it is true. Two pairs deliberately stay apart:
#
#   POSITIVE EMPTY vs POSITIVE UNREADABLE - "it returned zero" and "we could not
#   tell what it returned" are different facts, and collapsing them is exactly
#   the mistake that passed read-graphic-overrides. Neither is a pass.
#
#   NO NEGATIVE vs NEG NOT EMPTY - a leg that did not run and a leg that ran and
#   found something are opposite problems. The first is a missing arrangement;
#   the second is a FINDING about the fragment.

PASS = "PASS"
WOULD_RUN = "WOULD RUN"
# THE RULE THESE TWO NAMES ANSWER NOW LIVES IN heron_validate, beside
# looks_empty. It was defined here alone, and `accept` - the signing gate -
# could not reach it, so a draft whose result was 0 in BOTH legs read as
# complete and was signable. One definition, two callers, 2026-09-12.
contract_of = HV.contract_of
positive_worked = HV.positive_worked

ALREADY = "ALREADY"
NO_FRAGMENT = "NO FRAGMENT"
REFUSED = "REFUSED"
TIMEOUT = "TIMEOUT"
DID_NOT_RUN = "DID NOT RUN"
POSITIVE_EMPTY = "POSITIVE EMPTY"
POSITIVE_UNREADABLE = "POSITIVE UNREADABLE"
NO_NEGATIVE = "NO NEGATIVE"
NEG_NOT_EMPTY = "NEG NOT EMPTY"

# What a person should do about each, in the order the report prints them.
MEANING = {
    PASS: "both halves held. A draft is waiting for a person to read",
    WOULD_RUN: "the job file is runnable as written",
    ALREADY: "already proved. Nothing was sent to Revit",
    NO_FRAGMENT: "no fragment of that name in the library",
    REFUSED: "the job file could not be run as written",
    TIMEOUT: "Revit did not answer in time. Try a smaller selection",
    DID_NOT_RUN: "the positive case never ran, so there is nothing to judge",
    POSITIVE_EMPTY: "it ran and found nothing. The POSITIVE was arranged wrong",
    POSITIVE_UNREADABLE: "nothing it returned can be read as a quantity",
    NO_NEGATIVE: "D-30's second leg is missing",
    NEG_NOT_EMPTY: "the negative returned content. A FINDING, not a proof",
}

ORDER = [WOULD_RUN, PASS, ALREADY, POSITIVE_EMPTY, POSITIVE_UNREADABLE,
         NEG_NOT_EMPTY, NO_NEGATIVE, DID_NOT_RUN, TIMEOUT, REFUSED, NO_FRAGMENT]


# ---------------------------------------------------------------------------
# 2. The job file
# ---------------------------------------------------------------------------

def _merge(base, extra):
    """`set:` and `negative-set:` merge key by key over the defaults.

    Whole-block replacement was the first shape and it is wrong for the commonest
    job there is: every fragment in a batch shares one selection and differs by
    one value. Replacing the block means retyping the selection into every job,
    and the day one copy is retyped wrong the batch proves fragments against a
    selection nobody meant.
    """
    merged = copy.deepcopy(base or {})
    merged.update(extra or {})
    return merged


def read_jobs(path):
    """Every job in the file, defaults folded in. Returns (jobs, problems)."""
    problems = []
    try:
        data = yaml.safe_load(io.open(path, encoding="utf-8"))
    except yaml.YAMLError as exc:
        return [], ["%s is not valid YAML: %s" % (path, str(exc).split("\n")[0])]
    except (IOError, OSError) as exc:
        return [], ["%s could not be read: %s" % (path, exc)]

    if not isinstance(data, dict):
        return [], ["%s must be a mapping with a `jobs:` list" % path]

    defaults = data.get("defaults") or {}
    rows = data.get("jobs")
    if not isinstance(rows, list) or not rows:
        return [], ["%s has no `jobs:` list" % path]

    jobs = []
    for index, row in enumerate(rows, 1):
        if not isinstance(row, dict) or not row.get("fragment"):
            problems.append("job %d has no `fragment:` name" % index)
            continue
        job = {
            "fragment": row["fragment"],
            "setup": row.get("setup", defaults.get("setup")) or [],
            # DEFECT ROW 11. Keep what the setup chain left instead of resetting
            # it before the fragment under test. Per job, and defaulting to
            # False, because the chain outranks the selection - see cmd_validate
            # in the client. A producer -> consumer pair needs it; every
            # arrangement written before 2026-09-10 must not have it.
            "keep-chain": bool(row.get("keep-chain",
                                       defaults.get("keep-chain", False))),
            "set": _merge(defaults.get("set"), row.get("set")),
            "negative-set": _merge(defaults.get("negative-set"),
                                   row.get("negative-set")),
            # THE SETUP CHAIN'S OWN VALUES, AND THEY ARE OPTIONAL ON PURPOSE.
            # FRAGMENT-ISSUES row 147: `set` went to every setup step AND to
            # the fragment as one flat dict, so a chain selecting on
            # `categories` and a fragment asking about `categories` collapsed
            # into one value - silently, and the job still ran, reporting a
            # number about the wrong population. 88 fragment/chain pairs in
            # this library share a caller-supplied name.
            #
            # LEAVE IT OUT AND NOTHING CHANGES. The client falls back to `set`
            # when this is empty, so every job file written before today sends
            # exactly what it sent before.
            "setup-set": _merge(defaults.get("setup-set"), row.get("setup-set")),
            "negative-setup-set": _merge(defaults.get("negative-setup-set"),
                                         row.get("negative-setup-set")),
            "write": bool(row.get("write", defaults.get("write", False))),
            "cross": row.get("cross", defaults.get("cross")),
            "in": row.get("in", defaults.get("in")),
            "negative-in": row.get("negative-in", defaults.get("negative-in")),
            "expect": row.get("expect"),
            "timeout": int(row.get("timeout", defaults.get("timeout",
                                                           DEFAULT_TIMEOUT))),
            "note": row.get("note"),
        }
        if isinstance(job["expect"], str):
            job["expect"] = [job["expect"]]
        jobs.append(job)

    return jobs, problems


def job_refusal(job, library):
    """Why this job cannot be run as written, or None.

    Every one of these is checked BEFORE Revit is touched, so a batch of fourteen
    does not discover on the eleventh that a name was mistyped on the first.
    """
    frag = library.get(job["fragment"])
    if frag is None:
        return NO_FRAGMENT, "no fragment called '%s'" % job["fragment"]

    if frag.status in FINISHED:
        # HOLE 1. Not an error and not a failure - finished work, reported as
        # finished. The first batch spent a whole pass re-proving fifteen of
        # these and reported six passes for it.
        return ALREADY, "heron-status is %s" % frag.status

    for step in job["setup"]:
        if step not in library:
            return REFUSED, "the setup names '%s', which is not a fragment" % step

    if job.get("keep-chain") and not job["setup"]:
        # `.get` AND NOT `[...]`, UNLIKE ITS NEIGHBOURS. `job_refusal` reads a
        # SUBSET of a job's keys and is called with partial dicts - it never
        # touches `write` or `timeout`, and tests/test_generate_jobs.py passes
        # one without them. A required key here would break that contract for a
        # field that is optional by design and False by default.
        #
        # KEEPING THE CHAIN WITH NOTHING TO KEEP IS NOT A NO-OP. Nothing in this
        # job fills the chain, so what survives is whatever an earlier run left,
        # and the fragment would bind from something nobody remembers running.
        # The client refuses this too; catching it here means a batch of
        # fourteen says so before Revit is touched.
        return REFUSED, ("`keep-chain: true` with no `setup:` - there is nothing "
                         "for it to keep, and what would survive is whatever an "
                         "earlier run left behind")

    # `.get` AND NOT `[...]` FOR THE NEW KEY. `build_jobs` always sets it, and
    # a job dict assembled anywhere else - a test fixture, a caller written
    # before today - has no business crashing on a key that is OPTIONAL by
    # design. It reads as absent, which is what it is.
    if (not job["negative-set"] and not job["negative-in"]
            and not job.get("negative-setup-set")):
        # `validate` with no negative arrangement STOPS AND WAITS at the
        # keyboard, which in a batch is a hang rather than a question. It is
        # also the arrangement D-30 exists for, so a job without one is not a
        # job that could ever have passed.
        #
        # `negative-setup-set` COUNTS, and leaving it out of this test made the
        # flag useless from here: a proof whose two legs differ only in the
        # ARRANGEMENT was refused before the client was ever called, with a
        # message naming the two keys it did know. Found by writing such a job
        # and running it, rather than by reading - the client's own half of
        # this had already been fixed and the batch runner still said no.
        return REFUSED, ("no negative arrangement. Give it `negative-set:`, "
                         "`negative-setup-set:` or `negative-in:` - without "
                         "one, `validate` stops and waits for somebody to "
                         "arrange it by hand")

    if job["expect"]:
        names = set(p.get("name") for p in frag.provides() or []
                    if isinstance(p, dict))
        missing = [e for e in job["expect"] if e not in names]
        if missing:
            return REFUSED, ("`expect:` names %s, which this fragment does not "
                             "provide. It provides: %s"
                             % (", ".join(missing), ", ".join(sorted(names))))

    if job["cross"] and job["cross"] not in ("count_elements", "duct"):
        return REFUSED, ("`cross: %s` is not a cross-check. It is "
                         "'count_elements' or 'duct'" % job["cross"])

    return None, None


# ---------------------------------------------------------------------------
# 3. Judging - and the half heron_validate does not do
# ---------------------------------------------------------------------------



def why_not_empty(phase, roles, names):
    """The entries that made `looks_empty` say no, and only those.

    A negative case that returns content is a FINDING, and a finding a person
    cannot act on is half a finding. Listing every declared name would print the
    zeros and the bookkeeping alongside the one value that actually came back -
    which is the reading this whole file exists to save somebody.
    """
    guilty = []
    for key, value in sorted((phase.get("provides") or {}).items()):
        if key not in names:
            continue
        if roles.get(key) == "accounting":
            continue
        if key in HV.NOTE_KEYS:
            continue
        if HV._is_helper_object(value):
            continue
        if HV._as_count(value) != 0:
            guilty.append("%s %s" % (key, value))
    return ", ".join(guilty)


def judge(record, frag, expect=None):
    """One verdict for one fragment, from its run record. Returns (verdict, why).

    THE POSITIVE IS JUDGED FIRST, and that ordering is a decision. A fragment
    that did nothing makes its own negative case meaningless - an empty answer
    from a fragment that always answers empty is not evidence - so when both
    halves have something wrong, the positive is what gets reported and the
    negative's problem is carried in the detail rather than as the headline.
    """
    phases = dict((p.get("phase"), p) for p in record.get("phases", []))
    positive, negative = phases.get("positive"), phases.get("negative")

    if not positive or not positive.get("ok"):
        why = HV.phase_failure(positive)
        if positive and positive.get("error") == "no_reply":
            return TIMEOUT, why
        return DID_NOT_RUN, why

    verdict, reason = positive_worked(positive, frag, expect)

    # A FLAG IS THE EVIDENCE WHEN IT FLIPS, and this runner has to agree with
    # `heron_validate` about that or a job reads PASS here and is then refused
    # at `accept`, or the other way round. Both ask the same function.
    #
    # `_as_count` reads a boolean as zero ON PURPOSE - see _flag_flipped - so a
    # fragment whose only declared result is a bool arrives here as POSITIVE
    # EMPTY however well it was arranged. `apply-view-filter` is that fragment:
    # `applied true` on a view that took the filter against `applied false` on
    # one that refused it, which is D-30's comparison exactly.
    if verdict is not None:
        flipped = HV._flag_flipped(positive, negative, frag)
        if flipped:
            verdict = None
            reason = ("%s is true in the positive and false in the negative"
                      % flipped)

    if not negative or not negative.get("ok"):
        why = HV.phase_failure(negative)
        if negative and negative.get("error") == "no_reply":
            return TIMEOUT, why
        if verdict is not None:
            return verdict, "%s; and the negative did not run - %s" % (reason, why)
        return NO_NEGATIVE, why

    roles, names = contract_of(frag)
    empty = HV.looks_empty(negative, roles, names)

    if verdict is not None:
        if not empty:
            return verdict, "%s; the negative did not come back empty either" % reason
        return verdict, reason
    if not empty:
        returned = why_not_empty(negative, roles, names)
        return NEG_NOT_EMPTY, "the negative returned %s" % (returned or "content")

    return PASS, "positive: %s" % reason


# ---------------------------------------------------------------------------
# 4. Running one job
# ---------------------------------------------------------------------------

def validate_command(job, record_path, session=None):
    """The `heron_bridge_client.py validate` line this job amounts to.

    `--session <pid>` NAMES WHICH REVIT, and it goes in FIRST deliberately.
    The client lifts it out with `pull_session` before it reads `--in`,
    `--negative-in`, `--cross` and `--out` positionally, so its position
    cannot disturb them - but reading "talk to this Revit, then do this" in
    that order is what a person expects.

    WITHOUT IT A BATCH CANNOT SAY WHICH REVIT IT MEANS. With two connected,
    the client takes the first entry in the discovery directory, which is
    sorted by filename and therefore arbitrary. A MODIFY batch aimed at one
    model could run against the other, and nothing in the output would say so.

    THE ORDER IS NOT COSMETIC. `--in`, `--negative-in`, `--cross` and `--out` are
    read POSITIONALLY by the client - it walks pairs off the FRONT of what is
    left and then requires exactly one bare token, the fragment name. So the four
    flags lead and the fragment comes last.

    Written the other way round first, with the name in front, and every job came
    back with the client's usage text instead of a run. Nothing about that is
    visible until it is run, which is why the test parses these lines with the
    client's own `main` rather than checking them by eye.

    `--write`, `--setup` and the `--set` family are lifted out wherever they
    appear, so only these four care.
    """
    argv = [sys.executable, CLIENT, "validate"]
    if session:
        argv += ["--session", str(session)]
    if job["in"]:
        argv += ["--in", job["in"]]
    if job["negative-in"]:
        argv += ["--negative-in", job["negative-in"]]
    if job["cross"]:
        argv += ["--cross", job["cross"]]
    argv += ["--out", record_path]
    if job["write"]:
        argv.append("--write")
    for step in job["setup"]:
        argv += ["--setup", step]
    if job["keep-chain"]:
        argv.append("--keep-chain")
    for key in sorted(job["set"]):
        argv += ["--set", "%s=%s" % (key, job["set"][key])]
    for key in sorted(job["negative-set"]):
        argv += ["--negative-set", "%s=%s" % (key, job["negative-set"][key])]
    for key in sorted(job["setup-set"]):
        argv += ["--setup-set", "%s=%s" % (key, job["setup-set"][key])]
    for key in sorted(job["negative-setup-set"]):
        argv += ["--negative-setup-set",
                 "%s=%s" % (key, job["negative-setup-set"][key])]
    argv.append(job["fragment"])
    return argv


def readable_command(argv):
    """The command line, with the values a person would have to quote, quoted.

    The dry run prints the line it WOULD send, and a printed line gets copied.
    `--set inViewOnly=FloorPlan: M1` pasted into a shell is two arguments and
    selects nothing, which is the failure this whole file exists to stop reading
    as a fragment defect.
    """
    return " ".join('"%s"' % a if " " in a else a for a in argv)


def run_one(job, env, quiet=True, session=None):
    """Run and draft one fragment. Returns (record, verdict, why).

    `record` is None when the run produced nothing to judge.
    """
    record_path = os.path.join(RUNS, "%s.json" % job["fragment"])
    argv = validate_command(job, record_path, session)

    # THE OLD RECORD GOES BEFORE THE RUN, BECAUSE "the file is there" IS NOT
    # "this run wrote it". The check below only ever asked whether the path
    # exists, so a record left by ANY earlier run - another session, another
    # model, another day - was read and judged as if it were this one.
    #
    # Measured 2026-09-14. `transfer-project-parameters-between-documents` is
    # risk: ADMIN, so `validate` refused it and wrote nothing; the runner read a
    # record from 01:45 the previous day, on a different Revit session, and
    # reported POSITIVE EMPTY with numbers nobody had just measured. A refusal
    # had been turned into a result.
    #
    # Deleting first makes the existing check honest: no file means nothing ran,
    # which is exactly what DID NOT RUN is for.
    try:
        if os.path.isfile(record_path):
            os.remove(record_path)
    except OSError:
        pass

    try:
        # stdin is closed rather than inherited. `validate` with no negative
        # arrangement stops and waits at the keyboard, and job_refusal already
        # rejects that shape - but a batch must fail by recording NOT
        # ESTABLISHED rather than by hanging until somebody notices.
        result = subprocess.run(
            argv, cwd=ROOT, env=env, stdin=subprocess.DEVNULL,
            stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
            timeout=job["timeout"])
    except subprocess.TimeoutExpired:
        return None, TIMEOUT, ("nothing came back within %d seconds. A timeout "
                               "says nothing about the fragment - try a smaller "
                               "selection" % job["timeout"])

    output = (result.stdout or b"").decode("utf-8", "replace")
    if not quiet:
        sys.stdout.write(output)

    if not os.path.isfile(record_path):
        first = [line for line in output.splitlines() if line.strip()]
        return None, DID_NOT_RUN, (first[0] if first else
                                   "validate recorded nothing and said nothing")

    try:
        record = json.loads(io.open(record_path, encoding="utf-8").read())
    except ValueError as exc:
        return None, DID_NOT_RUN, "the run record could not be read: %s" % exc

    return record, None, None


def draft(fragment, record_path, env):
    """Turn a run record into a proof draft. A draft, never a proof."""
    argv = [sys.executable, VALIDATOR, "draft", fragment, "--from", record_path]
    result = subprocess.run(argv, cwd=ROOT, env=env, stdout=subprocess.PIPE,
                            stderr=subprocess.STDOUT)
    return result.returncode == 0


# ---------------------------------------------------------------------------
# 5. The batch
# ---------------------------------------------------------------------------

def statuses(library):
    return dict((slug, frag.status) for slug, frag in library.items())


def report(results, dry_run):
    """The findings, worst first, and only what a person has to act on."""
    width = max([len(r["fragment"]) for r in results] + [10])

    print("")
    print("=" * 72)
    for verdict in ORDER:
        rows = [r for r in results if r["verdict"] == verdict]
        if not rows:
            continue
        print("")
        print("%-20s %d  - %s" % (verdict, len(rows), MEANING[verdict]))
        for row in rows:
            print("  %-*s  %s" % (width, row["fragment"], row["why"]))

    print("")
    print("=" * 72)
    counts = ", ".join("%d %s" % (len([r for r in results if r["verdict"] == v]), v)
                       for v in ORDER
                       if any(r["verdict"] == v for r in results))
    print("%d job(s): %s" % (len(results), counts))

    if dry_run:
        print("")
        print("Dry run. Nothing was sent to Revit and nothing was written.")
        return

    passed = [r for r in results if r["verdict"] == PASS]
    print("")
    if passed:
        print("%d draft(s) in brain/proof-drafts/, waiting for a person:" % len(passed))
        print("  python brain/heron_validate.py review <fragment>")
        print("  python brain/heron_validate.py accept <fragment> --by \"Your Name\"")
    print("NOTHING WAS ACCEPTED AND NOTHING WAS PROMOTED. A PASS here means the "
          "evidence held,")
    print("not that the fragment is proved - that is a person's signature, and "
          "it is still owed.")


def main(argv=None):
    parser = argparse.ArgumentParser(
        description="Prove many fragments in one pass. It never signs one.")
    parser.add_argument("jobs", help="a YAML job file")
    parser.add_argument("--dry-run", action="store_true",
                        help="read the job file and check it. Run nothing")
    parser.add_argument("--only", action="append",
                        help="run just this fragment. Repeatable")
    parser.add_argument("--verbose", action="store_true",
                        help="show what validate printed, job by job")
    parser.add_argument("--report", help="write the findings as JSON here")
    parser.add_argument("--client-id", default="heron-batch-prove",
                        help="the HERON_CLIENT_ID every job shares")
    parser.add_argument("--session",
                        help="which Revit, by pid, when more than one is "
                             "connected. List them with `heron_bridge_client.py "
                             "list`. WITH TWO REVITS OPEN AND NO --session THE "
                             "TARGET IS ARBITRARY")
    args = parser.parse_args(argv)

    jobs, problems = read_jobs(args.jobs)
    for problem in problems:
        print("job file: %s" % problem)
    if not jobs:
        return 1

    if args.only:
        jobs = [j for j in jobs if j["fragment"] in args.only]
        if not jobs:
            print("None of %s is in %s" % (", ".join(args.only), args.jobs))
            return 1

    found, unreadable = HF.load_all()
    for problem in unreadable:
        sys.stderr.write("unreadable fragment: %s\n" % problem)
    library = dict((frag.slug, frag) for frag in found.values())

    before = statuses(library)

    # ONE CLIENT ID FOR THE WHOLE BATCH. The lease identifies a CHAT, and a
    # command line that exits after each command leaves one orphaned for five
    # minutes - so the second job of every batch is refused until it expires.
    # Pinning it makes fourteen jobs one conversation with Revit.
    env = dict(os.environ)
    env.setdefault("HERON_CLIENT_ID", args.client_id)

    print("job file:  %s" % args.jobs)
    print("jobs:      %d" % len(jobs))
    print("client id: %s" % env["HERON_CLIENT_ID"])
    print("session:   %s" % (args.session if args.session else
                             "NOT PINNED - whichever Revit sorts first"))
    if args.dry_run:
        print("mode:      DRY RUN - nothing will be sent to Revit")
    print("")

    results = []
    for index, job in enumerate(jobs, 1):
        name = job["fragment"]
        verdict, why = job_refusal(job, library)

        if verdict is None and args.dry_run:
            # WHAT IT WOULD SEND, printed in full. A job file is read by a
            # person before it costs a Revit session, and the line that will
            # actually be run is the only honest thing to show them.
            verdict, why = WOULD_RUN, readable_command(validate_command(
                job, os.path.join("brain", "proof-drafts", "runs",
                                  "%s.json" % name), args.session)[2:])

        elif verdict is None:
            print("[%d/%d] %s" % (index, len(jobs), name))
            record, verdict, why = run_one(job, env, quiet=not args.verbose,
                                           session=args.session)
            if record is not None:
                draft(name, record["run_record"], env)
                verdict, why = judge(record, library[name], job["expect"])
            print("        %s  %s" % (verdict, why))
        else:
            print("[%d/%d] %s" % (index, len(jobs), name))
            print("        %s  %s" % (verdict, why))

        results.append({"fragment": name, "verdict": verdict, "why": why,
                        "note": job.get("note")})

    # NOTHING HERE WRITES heron-status, SO NOTHING SHOULD HAVE. Checked rather
    # than promised - the same way heron_validate.accept reads the file back.
    #
    # THE FINDINGS ARE STILL PRINTED. A long batch's results are the expensive
    # thing in this run, and throwing them away because somebody else accepted a
    # draft in another window would punish the wrong person. Warn loudly, print
    # everything, and exit non-zero.
    found_after, _ = HF.load_all()
    after = statuses(dict((f.slug, f) for f in found_after.values()))
    moved = sorted(slug for slug, was in before.items()
                   if slug in after and after[slug] != was)
    gone = sorted(slug for slug in before if slug not in after)

    if moved or gone:
        print("")
        print("STOP. The fragment library changed while this batch was running, "
              "and nothing here writes it.")
        if moved:
            print("  heron-status moved on: %s" % ", ".join(moved))
        if gone:
            print("  no longer in the library: %s" % ", ".join(gone))
        print("Another session is writing it. Read the findings below, then "
              "check what it did before trusting them.")

    report(results, args.dry_run)

    if args.report:
        with io.open(args.report, "w", encoding="utf-8") as fh:
            fh.write(json.dumps({
                "job_file": args.jobs,
                "date": time.strftime("%Y-%m-%d"),
                "dry_run": args.dry_run,
                "results": results,
            }, indent=2, sort_keys=True))
        print("")
        print("Findings written to %s" % args.report)

    # THE EXIT CODE FOLLOWS THE BATCH, NOT THE FRAGMENTS. A fragment that did
    # not pass is this tool's OUTPUT - the thing it was run to find out - and
    # failing on it would make a successful proving run indistinguishable from a
    # broken one. What earns a non-zero code is the batch being unable to do its
    # job: a name that is not a fragment, or a job file that cannot be run.
    broken = [r for r in results if r["verdict"] in (NO_FRAGMENT, REFUSED)]
    return 1 if (broken or moved or gone) else 0


if __name__ == "__main__":
    sys.exit(main())
