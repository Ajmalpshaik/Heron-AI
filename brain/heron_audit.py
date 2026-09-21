# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
The brain's half of the audit trail. Q-44, answered as D-62.

    python brain/heron_audit.py            # where it writes, and what is there

WHY THIS FILE EXISTS AT ALL
----------------------------
`HeronAudit` is C# in the add-in, so the trail only ever knew what reached
Revit. A request answered entirely by the brain - heron_lookup, heron_resolve,
heron_capabilities - left NO RECORD. tools/measure-routes.py could therefore
report the STRUCTURAL route share (the library asked its own declared
phrasings) and never the LIVE one, which is the only half that says anything
about how people actually use Heron.

Golden Rule 14 says every important autonomous operation must be auditable.
Deciding WHICH FRAGMENT ANSWERS A REQUEST is not a small operation.

WHY A SECOND FILE AND NOT THE SAME ONE
---------------------------------------
docs/19 s7 asks for "one append-only record, many readers". Two PROCESSES
appending to one file is not that - it is an interleaving problem with a
partial line at the end of it, across a C#/Python boundary where no lock is
shared. That was the whole reason Q-44 was a question and not a patch.

The answer was already in the reader. heron_gaps.read() globs the audit
directory for `audit-*.jsonl`, parses one JSON object per line, skips a bad
line rather than dying on it, and SORTS EVERY ENTRY BY `at`. It has always
merged files. It does not care how many there are or who wrote them.

So: one file per WRITER, merged at read time by the reader that already merges.
`audit-brain-YYYYMM.jsonl` matches the glob, so nothing downstream changes -
not heron_gaps.read(), not heron-backup.py, not heron_validate.py. The
alternative, "two homes for one fact", is not what this is: there is one home,
the directory, and the two files are how two processes write into it safely.

WHAT IS NEVER WRITTEN HERE, AND WHY
------------------------------------
NOT THE USER'S SENTENCE. The trail carries project information already and
docs/12 s5 puts it under the same egress rules as everything else. The live
route share needs the ROUTE and whether it resolved - it does not need the
words. Recording the sentence would add a class of content to an append-only,
never-pruned file for a report that does not read it.

The utterance cache is where a wording is kept (heron_search.py), and that is
a local SQLite store under the user's own data - a different file, a different
lifetime, and one a person can delete without losing the trail.

WHY THE HEADER CLAIMS NO AGENT
-------------------------------
`HERON-MCP-LOG-010` is the row this serves - "structured request/response
logging into the audit log" - and it is deliberately NOT claimed. That row and
`HERON-KRN-LOG-006` both say the trail is KEYED BY WORKFLOW ID, and no workflow
id reaches the brain: the add-in mints one per request and the MCP tools the
host calls directly carry none. Every line here goes out with an empty
`workflow`, which is honest and is not the row's job done.

D-58 set the precedent in the same words - a file stays `Heron-Agent: none`
until it serves the whole row, because check-metadata.py reporting an agent
BUILT is a claim somebody will rely on. When the workflow id crosses the seam,
this header changes and D-61's cache gets its evidence in the same change.

NUMBERS ARE WRITTEN AS NUMBERS. HeronAudit.cs carries a long comment about the
day `ms` went in quoted and a reader compared "9" against "6620" as text. Old
lines keep their quotes for ever because the log is never pruned. This writer
does not add to that pile.
"""

import io
import json
import os
import sys
import datetime

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)

import heron_gaps as GAPS


# The op names the brain writes. They are the MCP tool names without their
# prefix, so a reader joining this trail to mcp/server/heron_tools.py does not
# have to guess a mapping.
LOOKUP = "brain_lookup"
RESOLVE = "brain_resolve"
CONTEXT = "brain_context"
CATALOGUE = "brain_catalogue"


def catalogue(skills, capabilities, gaps, ms=None, directory=None):
    """What Heron knows, asked for as a whole. Never fails, so ok is always
    true - the numbers are the content."""
    return record(
        CATALOGUE, ok=True,
        numbers={"skills": skills, "capabilities": capabilities, "gaps": gaps,
                 "ms": ms},
        directory=directory)


def current_file(directory=None):
    """One file per month, matching HeronAudit's own rhythm and its glob."""
    target = directory if directory is not None else GAPS.audit_dir()
    if not target:
        return None
    stamp = datetime.datetime.utcnow().strftime("%Y%m")
    return os.path.join(target, "audit-brain-%s.jsonl" % stamp)


def record(op, ok, fields=None, numbers=None, directory=None):
    """
    One line. Returns True when it was written, False when there was nowhere
    to write it - which is normal, not an error.

    NEVER FATAL, and never silent about being non-fatal. A trail that can take
    down the request it is recording is worse than no trail; HeronAudit.cs
    swallows three exception types for the same reason. The difference is that
    this returns whether it wrote, so a test can tell "nowhere to write" apart
    from "wrote it".

    THAT APPLIES TO BUILDING THE ROW AND NOT ONLY TO WRITING IT. A `numbers`
    value that is not a whole number costs that one field - named in
    `dropped` on the same line, because Golden Rule 14 says nothing is
    discarded silently - and never the line, and never the request.

    There is no APPDATA on Linux and no HERON_AUDIT in a plain checkout, so on
    a developer machine this writes nothing and costs nothing.
    """
    path = current_file(directory)
    if not path:
        return False

    row = {
        "at": datetime.datetime.utcnow().strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z",
        "workflow": "",          # the brain has no workflow id yet - see D-61
        "op": op,
        "ok": bool(ok),
    }
    for key, value in sorted((fields or {}).items()):
        if value is None:
            continue
        row[key] = str(value)
    # THE CONVERSION USED TO SIT OUTSIDE THE `try` BELOW, so a value that
    # is not a whole number took down the request this was only supposed to
    # DESCRIBE - out of a function whose docstring says NEVER FATAL in
    # capitals. Measured 2026-09-21: ms="fast" and float("nan") both raised
    # ValueError, and float("inf") raised OverflowError, which was not even
    # in that except list. Row 5b-91.
    #
    # NOT reachable from any caller as things stand - every one of them
    # passes a len() or a rowcount - and that is the dangerous half rather
    # than the reassuring one: the early return above means this code only
    # ever runs where the trail really writes, which is somebody's machine
    # and not this container.
    #
    # A BAD NUMBER COSTS THAT ONE FIELD, NEVER THE LINE AND NEVER THE
    # REQUEST. It is not written as a string either: the docstring's own
    # "NUMBERS ARE WRITTEN AS NUMBERS" is about the day `ms` went in quoted
    # and a reader compared "9" against "6620" as text, and the log is never
    # pruned, so a quoted number is for ever. Golden Rule 14 says it is not
    # dropped silently, so `dropped` names the key on the line itself.
    dropped = []
    for key, value in sorted((numbers or {}).items()):
        if value is None:
            continue
        try:
            row[key] = int(value)
        except (ValueError, TypeError, OverflowError):
            dropped.append(key)
    if dropped:
        row["dropped"] = ",".join(dropped)

    try:
        directory_of = os.path.dirname(path)
        if directory_of and not os.path.isdir(directory_of):
            os.makedirs(directory_of)
        with io.open(path, "a", encoding="utf-8") as handle:
            handle.write(json.dumps(row, sort_keys=True) + u"\n")
        return True
    except (IOError, OSError, ValueError):
        # Same bargain as HeronAudit.cs: a full disk costs the entry, never
        # the request that was being recorded.
        return False


# The refusal codes this writer uses. They are the keys heron_gaps.analyse()
# classifies by, and every ok=false line MUST carry one.
#
# WITHOUT THEM A CORRECT REFUSAL READS AS A FAULT. analyse() buckets a failed
# row by its `error`, defaulting to "(none)" and landing in `unclassified` - so
# a request Heron honestly could not answer would have inflated the failure
# count in the very report built to tell defects from correct refusals apart.
# Codex found it on PR #44 and it is this file's neighbour's own founding
# mistake: heron_gaps was corrected because its loudest error was the executor
# behaving correctly.
NO_CAPABILITY = "no_capability"     # the words matched nothing Heron provides
NO_PROVIDER = "no_provider"         # known capability, no fragment for it
CONTEXT_REFUSED = "context_refused" # over budget, or a source not installed


def lookup(route, capability, provider, candidates, excluded, ms=None,
           directory=None):
    """
    A request resolved to a capability. THE SENTENCE IS NOT RECORDED.

    `excluded` is here because D-52 is the rule this trail would otherwise
    break in its own turn: a line saying "route=hybrid, capability=X" and
    nothing about what the version wall removed reports what was found and not
    what was turned down.
    """
    return record(
        LOOKUP, ok=bool(provider),
        fields={"route": route, "capability": capability, "provider": provider,
                "error": None if provider else NO_CAPABILITY},
        numbers={"candidates": candidates, "excluded": excluded, "ms": ms},
        directory=directory)


def resolve(capability, provider, ok, revit=None, ms=None, directory=None):
    """A capability asked for by name. The name is Heron's own, not the user's."""
    return record(
        RESOLVE, ok=ok,
        fields={"capability": capability, "provider": provider, "revit": revit,
                "error": None if ok else NO_PROVIDER},
        numbers={"ms": ms}, directory=directory)


def context(path, depth, parts, characters, refused=None, ms=None,
            directory=None):
    """
    A context assembled, or refused and why.

    A REFUSAL IS RECORDED AS ok=false WITH ITS REASON, never dropped. A trail
    that only holds the assemblies makes a path that refuses every time look
    like a path nobody used.
    """
    return record(
        CONTEXT, ok=refused is None,
        fields={"path": path, "depth": depth, "refused": refused,
                # A REFUSAL IS NOT A DEFECT, and it is not UNCLASSIFIED either.
                # `refused` carries the sentence; `error` carries the code, so
                # heron_gaps files it as a correct refusal rather than under
                # "(none)". Leaving the code out was the first fix's own
                # version of the bug it was fixing.
                "error": None if refused is None else CONTEXT_REFUSED},
        numbers={"parts": parts, "characters": characters, "ms": ms},
        directory=directory)


def main(argv):
    directory = GAPS.audit_dir()
    print("Heron brain audit trail")
    print("=" * 70)
    if not directory:
        print("Nowhere to write: neither HERON_AUDIT nor APPDATA is set.")
        print("That is normal on Linux and in a plain checkout. The brain")
        print("records nothing and the request is unaffected.")
        return 0

    print("Directory:  %s" % directory)
    print("This month: %s" % os.path.basename(current_file() or "-"))
    entries, skipped = GAPS.read(directory)
    mine = [e for e in entries if str(e.get("op", "")).startswith("brain_")]
    print()
    print("%d entry(ies) in the directory, %d written by the brain, %d "
          "unreadable line(s)." % (len(entries), len(mine), skipped))
    print()
    print("heron_gaps.read() globs audit-*.jsonl and sorts by `at`, so the")
    print("add-in's file and this one are already one trail to every reader.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
