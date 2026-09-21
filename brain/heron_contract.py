# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-AHR-CON-017
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
The Agent Contract - the interface between agents, and the only place it lives.

    python brain/heron_contract.py                     validate every contract
    python brain/heron_contract.py OLD.yaml NEW.yaml   is the change BREAKING

WHAT A CONTRACT IS FOR (docs/18 s3, docs/28 HERON-AHR-CON-017)
--------------------------------------------------------------
An agent is a component with a contract: what goes in, what comes out, which
tools it may use, how long it may take, how it fails, and when a failure may be
retried. Without one, the answer to "can agent B use agent A" is read off
agent A's source code - so every caller depends on an implementation rather
than on a promise, and A can never be replaced.

This module is the first of the 146 agents that need no Revit, and it is first
because all 145 others declare themselves to it. The order is in
docs/work-notes/plans/agent-build-order-2026-09-13.md.

WHAT A CONTRACT MUST NOT CARRY: TIER AND RISK
---------------------------------------------
docs/28-agent-registry.md already states every agent's tier and risk level. A
contract that restates them creates two homes for one fact, and one day they
disagree and nobody knows which is true - the exact drift heron_capability.py
was written to avoid for a capability's risk. So a contract declaring `tier` or
`risk` is REFUSED, and the registry is read for those instead.

WHY BLIND RETRY IS REFUSED
--------------------------
D-21: Heron's failures are its own bounded set of codes, so classifying one is
a table and never a guess. A contract may retry, but it must name the failure
states it retries - `retry.on-failures` is checked against `failures`. "Retry
anything" is how a MODIFY that half-succeeded gets run a second time.

THE FIELD IS `on-failures` AND NOT `on`, WHICH IS NOT A STYLE CHOICE
--------------------------------------------------------------------
YAML 1.1 reads a bare `on` as the boolean true, so `on:` becomes the key True
and the list under it is lost. The first contract written against this module
declared `on: [CONTRACT_UNREADABLE]` and was correctly refused for naming no
failure state - the contract was right and the field name was the defect. It
cost ten minutes here; in a retry rule nobody reads until something fails, it
would have cost a great deal more.

WHY A CONTRACT MUST DECLARE AT LEAST ONE FAILURE
------------------------------------------------
An agent declaring no failure state is claiming it cannot fail. Golden Rule 14
says a skipped item is recorded and never silently discarded; a caller cannot
record what the contract never told it could happen.

THE BREAKING-CHANGE RULE
------------------------
The registry row asks this module to "detect breaking contract changes across
249 agents". A change is BREAKING when it can make a caller that was correct
yesterday wrong today:

    removed an input field ......... the caller still sends it
    added a REQUIRED input ......... the caller does not send it
    optional input became required . the caller does not send it
    changed a field's type ......... the caller sends or reads the wrong shape
    removed an output field ........ the caller reads a field that is gone
    removed a failure state ........ the caller handles one that never arrives
    added a failure state .......... the caller has no branch for it
    the timeout fell ............... a call that finished inside the old
                                     promise can fail under the new one

THE TIMEOUT RULE IS THE EIGHTH AND IT WAS MISSING FROM THIS TABLE UNTIL
2026-09-21, while compare() enforced it. Measured: 60s -> 30s came back
BREAKING and 60s -> 120s COMPATIBLE, which is right - raising a promise
costs nobody anything, so only the downward direction counts. A reader
working from a seven-row table would have lowered a timeout expecting
COMPATIBLE. Row 5b-87.

The added-failure-state row is included deliberately, and it is the one
people argue about. A
caller that handles failures exhaustively - which is the shape this repository
asks for - has no branch for a state that did not exist when it was written.
Calling that COMPATIBLE would be comfortable and wrong.

A BREAKING change must raise the MAJOR version. That is checkable, so it is
checked here rather than asked for in a review.
"""

import io
import os
import sys

try:
    import yaml
except ImportError:                                          # pragma: no cover
    sys.stderr.write(
        "Heron's brain needs PyYAML to read an agent contract.\n"
        "  pip install --user pyyaml\n"
        "It installs per-user and needs no administrator rights - D-01.\n")
    raise

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CONTRACTS_DIR = os.path.join(ROOT, "brain", "agents")
REGISTRY = os.path.join(ROOT, "docs", "28-agent-registry.md")

REQUIRED = ("agent", "version", "input", "output",
            "allowed-tools", "timeout-seconds", "failures", "retry")

# Declared in docs/28 and read from there. A contract naming either is refused.
OWNED_BY_THE_REGISTRY = ("tier", "risk")

# The shapes a field may take. Deliberately small: a contract is read by the
# person writing the next agent, and a type list long enough to need looking up
# is a type list nobody reads.
#
# `callable` was added 2026-09-14, and the argument for one more word is the
# argument against the alternative. Three agents take a READER rather than a
# value - HERON-OPS-SCH-001 asks whether a person is working, HERON-OPS-UPD-010
# whether Revit holds unsaved work, HERON-INS-DEP-005 what actually imports -
# because a caller that can STATE those answers is a caller that can state the
# convenient one. Declared as `map`, each contract then spent four lines of
# description explaining that it is not a map: the declaration saying one thing
# and the prose correcting it is worse than an eighth word.
TYPES = ("string", "integer", "number", "boolean", "list", "map", "path",
         "callable")


def registry_ids():
    """Every agent id in docs/28, as a set. The registry is the register."""
    ids = set()
    with io.open(REGISTRY, encoding="utf-8") as fh:
        for line in fh:
            if line.startswith("| `HERON-"):
                ids.add(line.split("`")[1])
    return ids


def load(path):
    """
    One contract, as plain data. Never a class - it is data (D-29).

    The two ways this fails are the two its own contract declares, and each
    says which it was: a path that is not there (CONTRACT_NOT_FOUND) and a
    file that is there and will not parse (CONTRACT_UNREADABLE). They call
    for different things - the first is a wrong name, the second is a real
    file somebody broke - so a reader is told which one happened.
    """
    try:
        with io.open(path, encoding="utf-8") as fh:
            text = fh.read()
    except IOError as exc:
        raise IOError("CONTRACT_NOT_FOUND: %s (%s)" % (path, exc))
    try:
        return yaml.safe_load(text) or {}
    except yaml.YAMLError as exc:
        raise ValueError("CONTRACT_UNREADABLE: %s does not parse as "
                         "YAML - %s" % (path, exc))


def _semver(value):
    parts = str(value).split(".")
    if len(parts) != 3 or not all(p.isdigit() for p in parts):
        return None
    return tuple(int(p) for p in parts)


def _fields(contract, side):
    """
    The input or output block, as {name: declaration mapping}.

    Every layer of it is tolerated, because compare() is called on exactly the
    contracts that are wrong. `input: {scope: string}` is valid YAML and a
    field whose declaration is a bare string; reaching .get() on it raised
    AttributeError, so the gate printed a traceback instead of a verdict at
    the precise moment somebody needed the verdict.
    """
    block = contract.get(side) if isinstance(contract, dict) else None
    if not isinstance(block, dict):
        return {}
    return {name: spec if isinstance(spec, dict) else {}
            for name, spec in block.items()}


def validate(contract, known_ids, where="contract"):
    """
    Everything wrong with this contract, as sentences a person can act on.

    Empty list = nothing found. It never raises on a malformed contract: a
    validator that crashes on the input it exists to judge reports nothing at
    all, and the file that broke it is the one somebody needs told about.
    """
    problems = []

    def say(text):
        problems.append("%s: %s" % (where, text))

    if not isinstance(contract, dict):
        say("is not a mapping - a contract is data, not prose (D-29)")
        return problems

    for field in REQUIRED:
        if field not in contract:
            say("has no '%s'" % field)

    for field in OWNED_BY_THE_REGISTRY:
        if field in contract:
            say("declares '%s' - docs/28-agent-registry.md owns that, and two "
                "homes for one fact is how they come to disagree" % field)

    agent = contract.get("agent")
    if agent and agent not in known_ids:
        say("AGENT_NOT_IN_REGISTRY: names agent '%s', which is not in "
            "docs/28-agent-registry.md - an "
            "agent that is not in the register does not exist" % agent)

    if "version" in contract and _semver(contract["version"]) is None:
        say("has version '%s', which is not MAJOR.MINOR.PATCH"
            % contract["version"])

    for side in ("input", "output"):
        block = contract.get(side)
        if block is None:
            continue
        if not isinstance(block, dict):
            say("'%s' is not a mapping of field name to declaration" % side)
            continue
        for name, spec in block.items():
            if not isinstance(spec, dict):
                say("%s field '%s' is not a mapping" % (side, name))
                continue
            kind = spec.get("type")
            if kind not in TYPES:
                say("%s field '%s' has type '%s' - one of: %s"
                    % (side, name, kind, ", ".join(TYPES)))
            if not str(spec.get("description", "")).strip():
                say("%s field '%s' has no description - the reader is the "
                    "person writing the next agent" % (side, name))
            if side == "input":
                marker = spec.get("required")
                if marker is None:
                    problems.append(
                        "input field '%s' does not say whether it is "
                        "required. A missing marker reads as optional, and a "
                        "caller generated from it sends the wrong shape."
                        % name)
                elif not isinstance(marker, bool):
                    # "false" is a non-empty string, so it read as REQUIRED -
                    # the opposite of what somebody wrote.
                    problems.append(
                        "input field '%s' declares required: %r, which is not "
                        "true or false. A quoted 'false' is a non-empty "
                        "string and reads as required." % (name, marker))
            if side == "output" and "required" in spec:
                say("output field '%s' declares 'required' - an output is "
                    "either promised or not declared at all" % name)

    timeout = contract.get("timeout-seconds")
    if timeout is not None and (not isinstance(timeout, int) or timeout <= 0):
        say("has timeout-seconds '%s' - a positive whole number of seconds"
            % timeout)

    failures = contract.get("failures")
    if failures is not None:
        if not isinstance(failures, list) or not failures:
            say("declares no failure state - an agent that cannot fail is a "
                "claim no caller can plan around (Golden Rule 14)")
        elif not all(isinstance(f, str) and f.isupper() for f in failures):
            say("has a failure state that is not an UPPER_CASE name")

    tools = contract.get("allowed-tools")
    if tools is not None and not isinstance(tools, list):
        say("'allowed-tools' is not a list - use [] for an agent that calls "
            "no tool, so that silence is a decision rather than an omission")

    retry = contract.get("retry")
    if retry is not None:
        if not isinstance(retry, dict):
            say("'retry' is not a mapping of attempts and on")
        else:
            attempts = retry.get("attempts")
            on = retry.get("on-failures") or []
            if True in retry or "on" in retry:
                say("declares 'retry.on' - YAML reads a bare `on` as the "
                    "boolean true and loses the list under it. The field is "
                    "'on-failures'")
            if not isinstance(attempts, int) or attempts < 0:
                say("retry.attempts is '%s' - 0 means never" % attempts)
            if not isinstance(on, list):
                say("retry.on-failures is not a list of failure state names")
            else:
                unknown = [f for f in on if f not in (failures or [])]
                if unknown:
                    say("retry.on-failures names %s, which is not in "
                        "'failures' - a "
                        "retry aimed at a failure the contract does not "
                        "declare is a blind retry (D-21)"
                        % ", ".join(sorted(unknown)))
                if isinstance(attempts, int) and attempts > 0 and not on:
                    say("retries %d time(s) but names no failure state to "
                        "retry - blind retry is refused (D-21)" % attempts)
    return problems


def compare(old, new):
    """
    (verdict, reasons) for a contract change. Verdict is one of:

        IDENTICAL   nothing a caller can see has changed
        COMPATIBLE  a caller that was correct stays correct
        BREAKING    a caller that was correct can now be wrong

    Both sides are read defensively. Comparing against a contract that does not
    parse is exactly when somebody needs a verdict, so a missing block is
    treated as empty rather than as a reason to stop.
    """
    reasons = []

    # BOTH ROOTS NORMALISED FIRST. _fields() tolerated a list or scalar root,
    # and then the identity check below reached .get() on it - so the two-file
    # gate still ended in AttributeError for exactly the malformed contract it
    # is run against.
    old = old if isinstance(old, dict) else {}
    new = new if isinstance(new, dict) else {}

    # ONE CANNOT BE A VERSION OF THE OTHER. Two contracts naming different
    # agents with the same interface used to compare IDENTICAL, so picking the
    # wrong pair of files passed the change gate without a word.
    old_agent = old.get("agent") if isinstance(old, dict) else None
    new_agent = new.get("agent") if isinstance(new, dict) else None
    if old_agent and new_agent and old_agent != new_agent:
        return "BREAKING", [
            "these are contracts for different agents - %s and %s. One cannot "
            "be a version of the other, so there is no change to judge."
            % (old_agent, new_agent)]

    for side in ("input", "output"):
        was, now = _fields(old, side), _fields(new, side)
        for name in sorted(set(was) - set(now)):
            reasons.append("%s field '%s' was removed" % (side, name))
        for name in sorted(set(now) & set(was)):
            before, after = was[name] or {}, now[name] or {}
            if before.get("type") != after.get("type"):
                reasons.append("%s field '%s' changed type from %s to %s"
                               % (side, name, before.get("type"),
                                  after.get("type")))
            if side == "input" and not before.get("required") \
                    and after.get("required"):
                reasons.append("input field '%s' became required" % name)
        if side == "input":
            for name in sorted(set(now) - set(was)):
                if (now[name] or {}).get("required"):
                    reasons.append("required input field '%s' was added" % name)

    was_f = set(old.get("failures") or [])
    now_f = set(new.get("failures") or [])
    for name in sorted(was_f - now_f):
        reasons.append("failure state '%s' was removed" % name)
    for name in sorted(now_f - was_f):
        reasons.append("failure state '%s' was added - a caller that handles "
                       "failures exhaustively has no branch for it" % name)

    # A SHORTER TIMEOUT IS BREAKING. A call that legitimately finished under
    # the old promise can time out under the new one, which is a caller that
    # was correct becoming a caller that fails. Raising it costs nobody
    # anything, so only the downward direction counts.
    was_timeout, now_timeout = (old.get("timeout-seconds"),
                                new.get("timeout-seconds"))
    if isinstance(was_timeout, int) and isinstance(now_timeout, int) \
            and now_timeout < was_timeout:
        reasons.append("the timeout fell from %ds to %ds - a call that "
                       "finished inside the old promise can fail under the "
                       "new one" % (was_timeout, now_timeout))

    if reasons:
        verdict = "BREAKING"
    else:
        visible = [
            _fields(old, "input") != _fields(new, "input"),
            _fields(old, "output") != _fields(new, "output"),
            (old.get("timeout-seconds") != new.get("timeout-seconds")),
            (old.get("allowed-tools") or []) != (new.get("allowed-tools") or []),
            (old.get("retry") or {}) != (new.get("retry") or {}),
        ]
        verdict = "COMPATIBLE" if any(visible) else "IDENTICAL"

    before, after = _semver(old.get("version")), _semver(new.get("version"))
    if before and after:
        if verdict == "BREAKING" and after[0] <= before[0]:
            reasons.append("the version went %s -> %s, and a breaking change "
                           "raises the MAJOR number"
                           % (old.get("version"), new.get("version")))
        elif verdict != "IDENTICAL" and after <= before:
            reasons.append("the contract changed but the version did not rise "
                           "(%s -> %s)" % (old.get("version"),
                                           new.get("version")))
    return verdict, reasons


def contracts():
    """Every contract file on disk, as (path, data), sorted by path."""
    found = []
    if not os.path.isdir(CONTRACTS_DIR):
        return found
    for name in sorted(os.listdir(CONTRACTS_DIR)):
        if name.endswith((".yaml", ".yml")):
            path = os.path.join(CONTRACTS_DIR, name)
            found.append((path, load(path)))
    return found


def main(argv):
    if len(argv) == 3:
        old, new = load(argv[1]), load(argv[2])
        verdict, reasons = compare(old, new)
        print("%s" % verdict)
        for line in reasons:
            print("  - %s" % line)
        if verdict == "BREAKING":
            print()
            print("  Raise the MAJOR version, and tell every caller listed in")
            print("  the agent's dependencies. A contract is a promise.")
        return 1 if verdict == "BREAKING" else 0

    if len(argv) != 1:
        print("usage: heron_contract.py [OLD.yaml NEW.yaml]")
        return 2

    known = registry_ids()
    found = contracts()
    print("AGENT CONTRACTS   brain/agents/")
    print("=" * 67)
    problems = []
    for path, data in found:
        broken = validate(data, known, os.path.basename(path))
        problems.extend(broken)
        # A contract whose root is a list or a scalar is exactly what
        # validate() just reported on, so the line reporting it must not be
        # the line that raises AttributeError on data.get().
        data = data if isinstance(data, dict) else {}
        print("  %-5s %-26s v%-8s %d in, %d out, %d failure(s)"
              % ("FAIL" if broken else "ok",
                 data.get("agent", "?"), data.get("version", "?"),
                 len(_fields(data, "input")), len(_fields(data, "output")),
                 len(data.get("failures") or [])))
    print()
    print("  %d contract(s) of the 250 agents in the register" % len(found))
    if problems:
        print()
        for line in problems:
            print("  %s" % line)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
