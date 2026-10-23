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
    python tools/batch-prove.py jobs.yaml --release 2025   PROVEN ones, on one more release
    python tools/batch-prove.py jobs.yaml --unattended     a night with nobody at the keyboard

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

**`--release <year>` is not that flag - it asks a different question (D-131).**
A proof on 2024 is evidence about 2024 and no other release, and the claim is
2020 to 2027. With `--release` the rule turns round rather than off: ONLY a
fragment at PROVEN or PRODUCTION whose proof still matches its code is run, and
only when neither its proof nor a fresh `proof.releases` entry already names
that release - those are `ALREADY` still, and anything not yet proven is `NOT
PROVEN`. Its run record and draft are `<slug>@<year>`, never the proof's own,
and a run whose record names another Revit is `WRONG RELEASE`. The draft is
signed with `heron_validate.py accept --release`, which writes one entry and
leaves `proof:` and `heron-status` exactly as they were - and the before/after
status check below holds for this mode as for every other.

**2. It passed fragments that had done nothing.** `heron_validate` judges
whether the NEGATIVE came back empty - which is D-30's leg, and which a fragment
that does nothing at all satisfies trivially. So the POSITIVE is judged too, and
by one rule: **a declared result has to have moved off zero.**

    `read-graphic-overrides` leaves `overrides` as an OverrideGraphicSettings
    OBJECT. Reading that as "not a count, therefore non-zero, therefore it did
    something" passed a fragment whose only real result, `withOverride`, was 0
    in BOTH legs. **Unreadable is not evidence of work** - the same rule
    `_as_count` already follows for the negative, applied to the positive.

    **A sentence is the one result that is not a count.** A provide declared
    `type: string` AND `role: result` is found when it says something and
    nothing when it is empty or `(null)`, in BOTH legs, so the same words in
    each are content in the negative and cannot pass. Beside a count or a
    flag the count decides, because a sentence that reads back the state is
    filled whether or not anything changed. `edit-revision`'s `changed` was
    POSITIVE UNREADABLE with both legs held (row 5b-411); see
    `string_results` and `positive_worked` in heron_validate.

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

UNATTENDED, FOR A NIGHT
-----------------------
`--unattended` is the overnight run, and it is here because two nights running
(2026-10-21 and 2026-10-22) a session wrote the same private wrapper round this
file to prove fragments while the owner slept. What the wrapper added is now
section 6, and nothing in it judges differently: a job is run, drafted and
judged exactly as without the flag. It only decides whether to ask again.

    * Revit held by another chat - "in use by another chat", session_in_use,
      model_in_use, "Could not identify the active model" - on a job whose
      verdict is DID NOT RUN or NO NEGATIVE: wait 30 s, ask again, up to 25
      tries in all.
    * Revit busy: close Revit's own "Virtual Memory - High Usage" warning and
      NOTHING ELSE, print every other window that Revit has open, wait 10 s,
      ask again. On Windows only; anywhere else it says so and closes nothing.
    * One line per fragment, appended with the time to a log, and the models
      handed back after each fragment so another chat can get in between.
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
# Which releases a fragment is already proven on (D-131) - the matrix's reading,
# the one `accept --release` asks too.
import heron_matrix as MX                                       # noqa: E402

# The underscore names below - `_as_count`, `_as_found` and `_is_helper_object` - are
# reached into deliberately. They are the validation agent's private
# workings, and normally that would be a reason to leave them alone; here the
# alternative is a second copy of the reasoning they carry, judging the same run
# records differently from the drafts written beside them. A private name shared
# is a smaller cost than a public judgement forked.
import heron_validate as HV                                     # noqa: E402

# THE CLIENT'S OWN RISK READERS, IMPORTED AND NOT COPIED - the argument made
# above about `looks_empty`, and the one tools/generate-jobs.py makes about
# `RUNNABLE_RISKS`. `validate` asks `risk_refusal` before it sends anything, so
# the dry run asks the same function: a second reader here would be a second
# opinion about which fragments a batch may send, and the client's is the one
# that decides.
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))
import heron_bridge_client as BRIDGE                            # noqa: E402

try:
    import yaml
except ImportError:                                          # pragma: no cover
    import heron_install as INSTALL
    sys.stderr.write("A job file is YAML, so this needs PyYAML. In the Heron folder:\n"
                     "  %s\n" % INSTALL.install_line("pip install --user pyyaml"))
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
NOT_PROVEN = "NOT PROVEN"
WRONG_RELEASE = "WRONG RELEASE"
NOT_IN_A_BATCH = "WILL NOT RUN IN A BATCH"
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
    NOT_PROVEN: ("--release proves again only what is PROVEN, on a proof that "
                 "still stands. Prove it without --release first. Nothing was "
                 "sent to Revit"),
    WRONG_RELEASE: ("the run record names another Revit than --release. Connect "
                    "that release, or pin it with --session"),
    NOT_IN_A_BATCH: ("risk ADMIN or PUBLISH - D-106 lets no batch send it. "
                     "Prove it by name. Nothing was sent to Revit"),
    NO_FRAGMENT: "no fragment of that name in the library",
    REFUSED: "the job file could not be run as written",
    TIMEOUT: "Revit did not answer in time. Try a smaller selection",
    DID_NOT_RUN: "the positive case never ran, so there is nothing to judge",
    POSITIVE_EMPTY: "it ran and found nothing. The POSITIVE was arranged wrong",
    POSITIVE_UNREADABLE: "nothing it returned can be read as a quantity",
    NO_NEGATIVE: "D-30's second leg is missing",
    NEG_NOT_EMPTY: "the negative returned content. A FINDING, not a proof",
}

ORDER = [WOULD_RUN, PASS, ALREADY, NOT_PROVEN, NOT_IN_A_BATCH, POSITIVE_EMPTY,
         POSITIVE_UNREADABLE, NEG_NOT_EMPTY, NO_NEGATIVE, DID_NOT_RUN, TIMEOUT,
         WRONG_RELEASE, REFUSED, NO_FRAGMENT]


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
            # FRAGMENT-ISSUES row 149: `set` went to every setup step AND to
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


def job_refusal(job, library, release=None):
    """Why this job cannot be run as written, or None.

    Every one of these is checked BEFORE Revit is touched, so a batch of fourteen
    does not discover on the eleventh that a name was mistyped on the first.

    `release` is `--release`: the question becomes "proven, but not yet on
    THIS release?" - see release_refusal.
    """
    frag = library.get(job["fragment"])
    if frag is None:
        return NO_FRAGMENT, "no fragment called '%s'" % job["fragment"]

    if release:
        verdict, why = release_refusal(frag, release)
        if verdict:
            return verdict, why
    elif frag.status in FINISHED:
        # HOLE 1. Not an error and not a failure - finished work, reported as
        # finished. The first batch spent a whole pass re-proving fifteen of
        # these and reported six passes for it.
        return ALREADY, "heron-status is %s" % frag.status

    # THE SETUP STEPS COME BEFORE THE FRAGMENT'S OWN RISK, because the bucket
    # below hands a person a by-name command - and that command fails too when
    # one of its steps cannot be sent.
    for step in job["setup"]:
        if step not in library:
            return REFUSED, "the setup names '%s', which is not a fragment" % step

        # A STEP ABOVE MODIFY IS NEVER SENT, WHATEVER THE FLAGS. `validate`
        # asks `risk_refusal` of every step with no switch at all: a step is an
        # ARRANGEMENT, and nothing about arranging a proof needs to export,
        # save or set up the project (D-106). This only checked that the step
        # was a fragment, so such a job read WOULD RUN and came back DID NOT
        # RUN - the twin of the fragment's own risk below (row 5b-440). Unlike
        # that one there is no command that sends it, so the job is REFUSED.
        step_root, step_slug = card_root(library[step]), library[step].slug
        step_refusal = BRIDGE.risk_refusal(step_root, step_slug)
        if step_refusal:
            step_risk = BRIDGE.fragment_risk(os.path.join(
                step_root, "brain", "fragments", step_slug, "fragment.yaml"))
            if step_risk in BRIDGE.SWITCHED_OPERATIONS:
                return REFUSED, (
                    "the setup step '%s' is risk %s, and `validate` sends no "
                    "setup step above MODIFY, whatever the flags - arranging a "
                    "proof never needs to export, save or set up the project. "
                    "Arrange it with steps at MODIFY or below" % (step, step_risk))
            return REFUSED, "the setup step: %s" % step_refusal

    # A WRITE SETUP MUST RUN IN ITS TYPED ORDER, AND A STEP THAT CHANGES THE
    # MODEL TAKES NO SETUP VALUES - `validate --write` refuses both before it
    # sends anything, so a dry run that said WOULD RUN here would be the row
    # 5b-440 shape again (row 5b-917). `.get`, because `job_refusal` takes
    # partial dicts.
    if job.get("write"):
        step_risks = [
            (step, BRIDGE.fragment_risk(os.path.join(
                card_root(library[step]), "brain", "fragments",
                library[step].slug, "fragment.yaml")))
            for step in job["setup"]]
        setup_refusal = BRIDGE.setup_order_refusal(step_risks)
        if setup_refusal is None and (job.get("setup-set")
                                      or job.get("negative-setup-set")):
            setup_refusal = BRIDGE.setup_values_refusal(step_risks)
        if setup_refusal:
            return REFUSED, setup_refusal

    # THE FRAGMENT'S OWN RISK, READ FROM ITS CARD BY THE CLIENT'S OWN READER.
    # `validate` without --allow-publish refuses a PUBLISH or ADMIN fragment
    # before it sends anything (D-106 point 6: no batch sends one), and this
    # never asked - so the dry run said WOULD RUN, "runnable as written", and
    # the real run came back DID NOT RUN. Measured 2026-10-12 on
    # add-family-shared-parameters at risk: ADMIN (row 5b-440).
    #
    # IT IS NOT REFUSED AS A BROKEN JOB, AND --allow-publish IS NOT ADDED. The
    # job is fine; a batch is the wrong way to send it. So it gets its own
    # bucket, naming the one command that does send it - typed by a person,
    # per fragment, which is the decision D-106 asks for.
    refusal = risk_refusal(job, frag, release)
    if refusal:
        return refusal

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

    # A CHAIN WITH VALUES OF ITS OWN NEEDS THEM ON THE NEGATIVE LEG TOO. The
    # client gives the negative leg's setup chain `negative-setup-set`, else
    # the NEGATIVE-SET - never the positive's `setup-set`. So a job whose chain
    # needs other values than its fragment sends the chain the fragment's
    # values on the negative leg, the chain refuses, and the fragment never
    # runs - while a dry run said WOULD RUN (row 5b-242).
    if (job.get("setup-set") and job["negative-set"]
            and not job.get("negative-setup-set")):
        return REFUSED, ("`setup-set:` gives the setup chain its own values, and "
                         "the negative leg has none: its chain would be handed "
                         "the `negative-set:` instead and refuse before the "
                         "fragment runs. Add `negative-setup-set:` - the same "
                         "values, unless the negative is arranged differently")

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


def release_refusal(frag, release):
    """(verdict, why) when `--release` must not run this fragment, else (None, None).

    HOLE 1 TURNED ROUND, NOT SWITCHED OFF. Re-proving what is finished on the
    release it was proved on still buys nothing and is still ALREADY; what
    `--release` runs is a fragment proven SOMEWHERE ELSE and not yet here.
    A proof the code has moved under is not "proven somewhere": proving it
    again comes first, without --release.
    """
    if frag.status not in FINISHED:
        return NOT_PROVEN, ("heron-status is %s - a first proof is a run "
                            "without --release" % frag.status)
    if frag.proof_is_stale():
        return NOT_PROVEN, ("its proof is STALE - the code moved under it. Prove "
                            "it again without --release first (D-30)")
    if release not in frag.supported:
        return REFUSED, ("it does not claim Revit %s - its `revit:` is %s"
                         % (release, ", ".join(frag.supported)))
    fresh, _stale = MX.proven_releases(frag)
    if release in fresh:
        return ALREADY, ("already proved on Revit %s, by %s" % (
            release, "its proof" if fresh[release] == "proof"
            else "an entry of proof.releases"))
    return None, None


def record_name(fragment, release=None):
    """The run record a job writes: the proof's own, or `<slug>@<year>`."""
    return os.path.join("brain", "proof-drafts", "runs", "%s.json"
                        % HV.proof_key(fragment, release=release))


def card_root(frag):
    """The checkout a library fragment's card sits in, for the client's readers.

    `risk_refusal` takes a checkout and a name and builds the card's path
    itself, so the checkout is read off where the card was FOUND rather than
    assumed to be this one. A fragment with no folder - nothing load_all
    returns - is read against this checkout, and an absent card there is
    refused as an unreadable risk, which is what the client would do.
    """
    folder = getattr(frag, "folder", None)
    if not folder:
        return ROOT
    return os.path.dirname(os.path.dirname(os.path.dirname(folder)))


def risk_refusal(job, frag, release=None):
    """(verdict, why) when the client would not send this job's fragment.

    PUBLISH and ADMIN are NOT_IN_A_BATCH with the by-name command; an
    unreadable or unknown risk is REFUSED with the client's own words, because
    `validate` refuses those whatever the flags.
    """
    root = card_root(frag)
    name = frag.slug
    refusal = BRIDGE.risk_refusal(root, name)
    if refusal is None:
        return None
    risk = BRIDGE.fragment_risk(os.path.join(root, "brain", "fragments", name,
                                             "fragment.yaml"))
    if risk not in BRIDGE.SWITCHED_OPERATIONS:
        return REFUSED, refusal

    # THE COMMAND THAT DOES SEND IT. `--allow-publish` and `--write` are both
    # needed: the client lets these levels through only as their own write
    # operation, and Revit still refuses unless the owner's switch is on.
    # OVER THE KEYS validate_command READS, because job_refusal is called with
    # partial job dicts (see its `keep-chain` comment) and a refusal must not
    # crash on a key the job leaves out.
    whole = {"in": None, "negative-in": None, "cross": None, "setup": [],
             "keep-chain": False, "set": {}, "negative-set": {},
             "setup-set": {}, "negative-setup-set": {}}
    whole.update(job)
    whole["write"] = True
    argv = validate_command(whole, record_name(name, release))[2:]
    argv.insert(1, "--allow-publish")
    return NOT_IN_A_BATCH, (
        "risk %s; prove it by name with `heron_bridge_client.py %s`, with %s "
        "and Changes on in Revit's Heron ribbon"
        % (risk, readable_command(argv), BRIDGE.SWITCH_NAMES[risk]))


# ---------------------------------------------------------------------------
# 3. Judging - and the half heron_validate does not do
# ---------------------------------------------------------------------------



def why_not_empty(phase, roles, names, strings=()):
    """The entries that made `looks_empty` say no, and only those.

    A negative case that returns content is a FINDING, and a finding a person
    cannot act on is half a finding. Listing every declared name would print the
    zeros and the bookkeeping alongside the one value that actually came back -
    which is the reading this whole file exists to save somebody.

    `strings` is read the way `looks_empty` reads it, or a one-word sentence
    result that made it say no would be missing from the list (5b-411).
    """
    guilty = []
    for key, value in sorted((phase.get("provides") or {}).items()):
        if key not in names:
            continue
        if roles.get(key) == "accounting":
            continue
        if key in HV.NOTE_KEYS:
            continue
        if key in strings:
            if HV._as_found(value) != 0:
                guilty.append("%s %s" % (key, value))
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

    # A SENTENCE RESULT IS READ THE SAME WAY IN BOTH LEGS. `positive_worked`
    # reads a declared string result as found or nothing; the negative must
    # too, or the same one-word answer in both legs reads as found in one and
    # as a helper object in the other, and passes (5b-411).
    roles, names = contract_of(frag)
    strings = HV.string_results(frag)
    empty = HV.looks_empty(negative, roles, names, strings)

    if verdict is not None:
        if not empty:
            return verdict, "%s; the negative did not come back empty either" % reason
        return verdict, reason
    if not empty:
        returned = why_not_empty(negative, roles, names, strings)
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


def run_one(job, env, quiet=True, session=None, release=None, transcript=None):
    """Run and draft one fragment. Returns (record, verdict, why).

    `record` is None when the run produced nothing to judge. With `release`
    the record is `<slug>@<year>.json`, so a run on another release never
    replaces the record a proof was signed on (D-131).

    `transcript`, a list when given, gets everything `validate` printed.
    `--unattended` reads the reason a run was refused there: the verdict's
    own line carries only the first line, and "Could not identify the active
    model:" is followed by the sentence that says why.
    """
    record_path = os.path.join(RUNS, "%s.json"
                               % HV.proof_key(job["fragment"], release=release))
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
    if transcript is not None:
        transcript.append(output)
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


def draft(fragment, record_path, env, release=None):
    """Turn a run record into a proof draft. A draft, never a proof."""
    argv = [sys.executable, VALIDATOR, "draft", fragment, "--from", record_path]
    if release:
        argv += ["--release", release]
    result = subprocess.run(argv, cwd=ROOT, env=env, stdout=subprocess.PIPE,
                            stderr=subprocess.STDOUT)
    return result.returncode == 0


# ---------------------------------------------------------------------------
# 5. The batch
# ---------------------------------------------------------------------------

def statuses(library):
    return dict((slug, frag.status) for slug, frag in library.items())


def wrong_release(record, release):
    """Why this run is no evidence about `release`, or None. D-131.

    `--release` is what the person SAID; the record says where it RAN - the
    client writes the connected Revit's release into `model:`. With two Revits
    open and no `--session`, those can differ, and a 2024 run filed as 2025
    would be the one mistake this mode exists to make impossible.
    """
    named, why = HV.record_release(record)
    if why:
        return why
    if named != release:
        return "it ran on Revit %s, and this batch is for %s" % (named, release)
    return None


def report(results, dry_run, release=None):
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
        if release:
            print("  python brain/heron_validate.py review <fragment>@%s" % release)
            print("  python brain/heron_validate.py accept <fragment> --release %s "
                  "--by \"Your Name\"" % release)
        else:
            print("  python brain/heron_validate.py review <fragment>")
            print("  python brain/heron_validate.py accept <fragment> --by \"Your Name\"")
    print("NOTHING WAS ACCEPTED AND NOTHING WAS PROMOTED. A PASS here means the "
          "evidence held,")
    print("not that the fragment is proved - that is a person's signature, and "
          "it is still owed.")


def free_models(conversation, session=None):
    """Hand back the models this batch held, so the next chat need not wait.

    D-141: a model is held by one conversation at a time and freed after three
    quiet minutes. A batch has finished, so it says so at once. Best effort, as
    `Bridge.release` is: a hand-back that fails costs a wait, never the batch.
    Only this batch's conversation can be freed this way - the add-in frees the
    sender's own holds and nobody else's.
    """
    if conversation != BRIDGE.CONVERSATION_ID:
        # An inherited HERON_CONVERSATION_ID is somebody else's conversation;
        # its holds are its own to give back.
        return
    try:
        live = BRIDGE.discover()[0]
    except Exception:                                     # noqa: BLE001 - see above
        return
    for bridge in live:
        if session and str(bridge.pid) != str(session):
            continue
        try:
            reply = bridge.request("release_models", timeout=2.0, response_timeout=20.0)
        except Exception:                                 # noqa: BLE001 - see above
            reply = None
        finally:
            bridge.close()
        if reply and reply.get("message"):
            print("models:    %s" % reply["message"])


def write_report(path, args, results, finished=True):
    """The findings as JSON. `--unattended` writes it after every fragment, so
    a night that dies at three in the morning still says what it found."""
    with io.open(path, "w", encoding="utf-8") as fh:
        fh.write(json.dumps({
            "job_file": args.jobs,
            "date": time.strftime("%Y-%m-%d"),
            "dry_run": args.dry_run,
            "release": args.release,
            "unattended": args.unattended,
            "finished": finished,
            "results": results,
        }, indent=2, sort_keys=True))


# ---------------------------------------------------------------------------
# 6. Unattended - a night with nobody at the keyboard
# ---------------------------------------------------------------------------
#
# EVERY PIECE OF THIS WAS IN A WRAPPER BEFORE IT WAS HERE. On 2026-10-21 and
# again on 2026-10-22 a session proving fragments while the owner slept wrote
# the same 62 lines round this file, in a temp folder, and each of them answers
# something that happened on one of those nights. A wrapper written twice is a
# missing feature; a third copy would have been retyped from memory.
#
# NOTHING HERE JUDGES. A job is run, drafted and judged by `prove`, exactly as
# without the flag. This section only decides whether to ask Revit again, and
# it asks only when a leg never reached the model - DID NOT RUN, NO NEGATIVE.
# A verdict about the fragment is never asked again: a POSITIVE EMPTY on the
# fifth try is the same finding as on the first.

HELD = "held by another chat"
BUSY = "Revit busy"

# WHAT `validate` SAYS WHEN SOMEBODY ELSE HAS THE REVIT OR THE MODEL. The lease
# (session_in_use, HeronLease.cs) and the model hold (model_in_use, D-141) both
# refuse before anything is sent, and both free themselves after a few quiet
# minutes - so waiting is the whole remedy. "Could not identify the active
# model" heads every refusal of `validate`'s opening question; the wrapper saw
# only that first line, and it is kept because a Revit that did not answer at
# all is also worth waiting for at night.
OPENING_REFUSED = "Could not identify the active model"
HELD_ELSEWHERE = ("in use by another chat", "session_in_use", "model_in_use",
                  OPENING_REFUSED)
REVIT_BUSY = ("revit_busy", "Revit is busy")

# ONLY A JOB NOTHING WAS JUDGED ON IS ASKED AGAIN.
NOTHING_JUDGED = (DID_NOT_RUN, NO_NEGATIVE)

HELD_WAIT = 30
BUSY_WAIT = 10
ATTEMPTS = 25

# REVIT 2020 PUTS THIS UP AFTER ABOUT SIXTY JOBS, and while it is up every job
# is refused revit_busy. The add-in cannot answer it: HeronDialogWatch is armed
# only around a job, and a dialog already open before one stops the job from
# starting at all. So it is closed from outside, by its exact name, and no
# other window ever is - a Keep or Discard box is a person's question, and so
# is anything this list does not name.
MEMORY_WARNING = "Virtual Memory - High Usage"
ON_WINDOWS = sys.platform == "win32"
WM_CLOSE = 0x0010
GW_OWNER = 4

LOG_NAME = "batch-prove.log"


def first_line(text):
    """The first line that says something - what a log has room for."""
    lines = [line.strip() for line in (text or "").splitlines() if line.strip()]
    return lines[0] if lines else ""


def refusal_line(text):
    """The line that says why Revit refused, rather than the heading above it.

    "Could not identify the active model:" is a heading; the sentence under
    it names who holds the model, or that Revit is busy.
    """
    for line in (text or "").splitlines():
        if (OPENING_REFUSED not in line
                and any(m in line for m in HELD_ELSEWHERE + REVIT_BUSY)):
            return line.strip()
    return first_line(text)


def retry_reason(verdict, text):
    """HELD, BUSY or None: whether an unattended run asks this job again.

    `text` is the verdict's own line with what `validate` printed. BUSY is
    asked first: a busy Revit's refusal is headed "Could not identify the
    active model" too, and busy is the one case where waiting alone may
    never help.
    """
    if verdict not in NOTHING_JUDGED:
        return None
    text = text or ""
    if any(marker in text for marker in REVIT_BUSY):
        return BUSY
    if any(marker in text for marker in HELD_ELSEWHERE):
        return HELD
    return None


def revit_pids(session=None):
    """The process ids of the Revits this batch may be talking to.

    `--session` names one. Without it, every Revit in the bridge's address
    book, read off the files' names: nothing is asked of a Revit that is
    busy, and nothing is pruned.
    """
    if session:
        try:
            return [int(session)]
        except ValueError:
            return []
    try:
        names = os.listdir(BRIDGE.DISCOVERY_DIR)
    except OSError:
        return []
    pids = [BRIDGE.pid_from_filename(name) for name in names
            if name.endswith(".json")]
    return sorted(pid for pid in pids if pid is not None)


def revit_windows(pids):
    """Every visible top-level window those Revits own. Windows only.

    A dialog Revit puts up is a top-level window it owns, so the windows of
    its process are enumerated and read by title - the title is what UI
    Automation calls the window's name, and it was the name the wrapper
    matched on both nights.
    """
    import ctypes
    from ctypes import wintypes

    user32 = ctypes.WinDLL("user32", use_last_error=True)
    user32.GetWindowThreadProcessId.argtypes = [wintypes.HWND,
                                                ctypes.POINTER(wintypes.DWORD)]
    user32.GetWindowThreadProcessId.restype = wintypes.DWORD
    user32.IsWindowVisible.argtypes = [wintypes.HWND]
    user32.GetWindowTextLengthW.argtypes = [wintypes.HWND]
    user32.GetWindowTextW.argtypes = [wintypes.HWND, wintypes.LPWSTR, ctypes.c_int]
    user32.GetWindow.argtypes = [wintypes.HWND, wintypes.UINT]
    user32.GetWindow.restype = wintypes.HWND

    wanted = set(pids)
    found = []

    def visit(hwnd, _lparam):
        pid = wintypes.DWORD()
        user32.GetWindowThreadProcessId(hwnd, ctypes.byref(pid))
        if pid.value in wanted and user32.IsWindowVisible(hwnd):
            size = user32.GetWindowTextLengthW(hwnd) + 1
            title = ctypes.create_unicode_buffer(size)
            user32.GetWindowTextW(hwnd, title, size)
            found.append({"hwnd": hwnd, "pid": pid.value, "name": title.value,
                          "owned": bool(user32.GetWindow(hwnd, GW_OWNER))})
        return True

    callback = ctypes.WINFUNCTYPE(wintypes.BOOL, wintypes.HWND, wintypes.LPARAM)
    user32.EnumWindows(callback(visit), 0)
    return found


def post_close(hwnd):
    """WM_CLOSE posted to one window - the window's own close, never a button."""
    import ctypes
    from ctypes import wintypes

    user32 = ctypes.WinDLL("user32", use_last_error=True)
    user32.PostMessageW.argtypes = [wintypes.HWND, wintypes.UINT,
                                    wintypes.WPARAM, wintypes.LPARAM]
    user32.PostMessageW.restype = wintypes.BOOL
    return bool(user32.PostMessageW(hwnd, WM_CLOSE, 0, 0))


def memory_warnings(windows, pids):
    """(to close, left as they are) among those Revits' windows.

    To close: a window whose name is EXACTLY Revit's memory warning - not
    one that contains it, not one that differs only in case. Left: every
    other window with a name, printed so a person decides.
    """
    wanted = set(pids)
    theirs = [w for w in windows if w.get("pid") in wanted]
    close = [w for w in theirs if w.get("name") == MEMORY_WARNING]
    left = [w for w in theirs
            if w not in close and (w.get("name") or "").strip()]
    return close, left


def close_memory_warning(pids, windows=None, post=None):
    """Close Revit's memory warning and nothing else. Returns lines to print.

    `windows` and `post` are the two halves that need Windows. A test hands
    in its own, so the rule that picks the window is the one tested.
    """
    if windows is None:
        if not ON_WINDOWS:
            return ["not on Windows - no Revit window can be reached from "
                    "here, so nothing was closed"]
        if not pids:
            return ["no Revit in the bridge's address book - nothing was closed"]
        try:
            windows = revit_windows(pids)
        except Exception as exc:                          # noqa: BLE001
            # A closer that fails costs one wait, never the night.
            return ["could not list Revit's windows (%s) - nothing was closed"
                    % exc]
    post = post or post_close

    close, left = memory_warnings(windows, pids)
    lines = []
    for window in close:
        lines.append("%s Revit's \"%s\" warning (Revit %s)" % (
            "closed" if post(window["hwnd"]) else "could not close",
            MEMORY_WARNING, window["pid"]))
    if not close:
        lines.append("no \"%s\" warning is open - nothing was closed"
                     % MEMORY_WARNING)
    for window in left:
        lines.append("left as it is, for a person: \"%s\" (Revit %s)"
                     % (window["name"], window["pid"]))
    return lines


def prove(job, library, env, args, transcript=None):
    """One job, once: run it, draft it, judge it. Returns (verdict, why)."""
    name = job["fragment"]
    record, verdict, why = run_one(job, env, quiet=not args.verbose,
                                   session=args.session, release=args.release,
                                   transcript=transcript)
    if record is not None:
        draft(name, record["run_record"], env, args.release)
        verdict, why = judge(record, library[name], job["expect"])
        elsewhere = (wrong_release(record, args.release)
                     if args.release else None)
        if elsewhere:
            verdict, why = WRONG_RELEASE, elsewhere
    return verdict, why


def prove_unattended(job, library, env, args, wait=time.sleep,
                     close=close_memory_warning, attempts=ATTEMPTS):
    """`prove`, asked again while Revit is held elsewhere or busy.

    Returns (verdict, why, tries). The last try's verdict is the one that
    stands, so a fragment Revit never took is reported DID NOT RUN with
    Revit's own reason, as it would be without the flag.
    """
    tries = 0
    while True:
        tries += 1
        said = []
        try:
            verdict, why = prove(job, library, env, args, said)
        except Exception as exc:                          # noqa: BLE001
            # ONE JOB'S FAULT IS NOT THE NIGHT'S. The wrapper ran each
            # fragment as its own process for this; here it is one job's
            # verdict, and the batch goes on to the next.
            verdict, why = DID_NOT_RUN, (
                "batch-prove failed on this job before judging it - %s: %s"
                % (type(exc).__name__, exc))
        text = "\n".join([why or ""] + said)
        reason = retry_reason(verdict, text)
        if reason is None or tries >= attempts:
            return verdict, why, tries
        if reason == BUSY:
            for line in close(revit_pids(args.session)):
                print("        %s" % line)
            pause = BUSY_WAIT
        else:
            pause = HELD_WAIT
        print("        %s, asking again in %d s (try %d of %d): %s"
              % (reason, pause, tries + 1, attempts, refusal_line(text)))
        wait(pause)


def append_log(path, text, when=None):
    """Append one line to the log, with the time in front. Returns the line."""
    line = "%s  %s" % (when or time.strftime("%Y-%m-%d %H:%M:%S"), text)
    folder = os.path.dirname(path)
    if folder and not os.path.isdir(folder):
        os.makedirs(folder)
    with io.open(path, "a", encoding="utf-8", errors="replace") as fh:
        fh.write(line + "\n")
    return line


def fragment_line(name, verdict, why, tries=1):
    """One fragment's line in the log: the fragment, its verdict and the first
    line of why - and how many tries it took, when it took more than one."""
    text = "%s  %s  %s" % (name, verdict, first_line(why))
    if tries > 1:
        text += "  (after %d tries)" % tries
    return text


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
    parser.add_argument("--release",
                        help="prove again, on this Revit release, fragments "
                             "already PROVEN on another (D-131), e.g. 2025. "
                             "Runs only those with no record for it")
    parser.add_argument("--unattended", action="store_true",
                        help="a night with nobody at the keyboard: ask again "
                             "while Revit is held by another chat or busy, "
                             "close Revit's '%s' warning and no other window, "
                             "hand the models back after each fragment, and "
                             "log one line per fragment" % MEMORY_WARNING)
    parser.add_argument("--log",
                        help="append one line per fragment to this file. "
                             "--unattended logs to %s in Heron's log folder "
                             "unless this names another" % LOG_NAME)
    args = parser.parse_args(argv)

    # A U+FFFD IN REVIT'S OUTPUT ENDED A NIGHT. `run_one` decodes what
    # `validate` printed with "replace", and printing that line to a pipe on
    # Windows - cp1252 - raised UnicodeEncodeError and killed the first
    # overnight run, 2026-10-21. Any run whose output goes to a file or a pipe
    # meets it, so this is not `--unattended`'s alone. A stream that cannot be
    # reconfigured - a test's StringIO - is left as it is.
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")

    if args.release and not HV.RELEASE_SHAPE.match(args.release):
        print("--release takes a year, e.g. --release 2025")
        return 1

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

    # AND ONE CONVERSATION FOR THE WHOLE BATCH - D-141. Each job is its own
    # process, and each process makes a conversation of its own, so without
    # this the second job would find the model held by the first and be
    # refused for three minutes. This process's own id, so the hand-back at
    # the end frees exactly what the jobs held. A chat's own server never
    # exports its id, so a batch is never mistaken for the chat that ran it.
    env.setdefault("HERON_CONVERSATION_ID", BRIDGE.CONVERSATION_ID)

    print("job file:  %s" % args.jobs)
    print("jobs:      %d" % len(jobs))
    print("client id: %s" % env["HERON_CLIENT_ID"])
    print("session:   %s" % (args.session if args.session else
                             "NOT PINNED - whichever Revit sorts first"))
    if args.release:
        print("release:   Revit %s - PROVEN fragments not yet proved on it (D-131)"
              % args.release)
    # A DRY RUN SENDS NOTHING, so it has nothing to wait for and nothing to log.
    log = None
    if not args.dry_run and (args.log or args.unattended):
        log = args.log or os.path.join(BRIDGE.LOG_DIR, LOG_NAME)
    if args.dry_run:
        print("mode:      DRY RUN - nothing will be sent to Revit")
    elif args.unattended:
        print("mode:      UNATTENDED - asks again while Revit is held or busy, "
              "closes only Revit's \"%s\" warning" % MEMORY_WARNING)
    if log:
        print("log:       %s" % log)
    print("")

    def note(text):
        """Log a line; a log that cannot be written costs the log, not the batch."""
        if not log:
            return
        try:
            append_log(log, text)
        except (IOError, OSError) as exc:
            print("        the log could not be written: %s" % exc)

    note("batch-prove %s: %d job(s)%s" % (
        args.jobs, len(jobs), ", unattended" if args.unattended else ""))

    results = []
    for index, job in enumerate(jobs, 1):
        name = job["fragment"]
        verdict, why = job_refusal(job, library, args.release)
        tries, sent = 1, False

        if verdict is None and args.dry_run:
            # WHAT IT WOULD SEND, printed in full. A job file is read by a
            # person before it costs a Revit session, and the line that will
            # actually be run is the only honest thing to show them.
            verdict, why = WOULD_RUN, readable_command(validate_command(
                job, record_name(name, args.release), args.session)[2:])

        elif verdict is None:
            print("[%d/%d] %s" % (index, len(jobs), name))
            sent = True
            if args.unattended:
                verdict, why, tries = prove_unattended(job, library, env, args)
            else:
                verdict, why = prove(job, library, env, args)
            print("        %s  %s" % (verdict, why))
        else:
            print("[%d/%d] %s" % (index, len(jobs), name))
            print("        %s  %s" % (verdict, why))

        results.append({"fragment": name, "verdict": verdict, "why": why,
                        "note": job.get("note")})
        note(fragment_line(name, verdict, why, tries))

        if args.unattended and not args.dry_run:
            # BETWEEN FRAGMENTS, AS THE WRAPPER WAS. It ran each fragment as a
            # batch of its own, so the models went back after every one and
            # another chat could get in; and its findings were on disk as each
            # one finished. Both are kept.
            if sent:
                free_models(env["HERON_CONVERSATION_ID"], args.session)
            if args.report:
                write_report(args.report, args, results, finished=False)

    if not args.dry_run:
        free_models(env["HERON_CONVERSATION_ID"], args.session)

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

    report(results, args.dry_run, args.release)

    note("finished: %s" % ", ".join(
        "%d %s" % (len([r for r in results if r["verdict"] == v]), v)
        for v in ORDER if any(r["verdict"] == v for r in results)))

    if args.report:
        write_report(args.report, args, results)
        print("")
        print("Findings written to %s" % args.report)

    # THE EXIT CODE FOLLOWS THE BATCH, NOT THE FRAGMENTS. A fragment that did
    # not pass is this tool's OUTPUT - the thing it was run to find out - and
    # failing on it would make a successful proving run indistinguishable from a
    # broken one. What earns a non-zero code is the batch being unable to do its
    # job: a name that is not a fragment, or a job file that cannot be run -
    # and, with --release, a run on another Revit than the one it was for,
    # because then the whole batch answered a question nobody asked.
    broken = [r for r in results
              if r["verdict"] in (NO_FRAGMENT, REFUSED, WRONG_RELEASE)]
    return 1 if (broken or moved or gone) else 0


if __name__ == "__main__":
    sys.exit(main())
