#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The move pre-check skips what changed in central, and says so by reason.

    python tests/test_central_skip.py

WHY THIS EXISTS
---------------
`RevitWrite.Partition` decides, before any transaction opens, which elements a
move leaves alone. Until 2026-09-23 it knew two reasons - pinned, and owned by
another user - and reported both under one blended sentence, "pinned or owned
by someone else". On a workshared model there is a third that Revit refuses in
the same way: an element somebody changed and synchronised since this copy
last reloaded. Revit's own word for `ModelUpdatesStatus.UpdatedInCentral` is
that "a reload latest will be required before it can be modified in the
current model". Package C3 of the earlier-brain plan.

WHAT IT PROVES, reading RevitWrite.cs as TEXT - it calls the Revit API and
cannot run off a Revit, so this is a strong test of the missing lines and a
weak one of behaviour. The behaviour is the PC's, on a workshared central.
  1. The central status is asked only of a workshared model, only after
     ownership - so an element somebody holds keeps the reason it always had.
  2. UpdatedInCentral and DeletedInCentral are skipped. NotYetInCentral - the
     ducts drawn in this copy and not yet synchronised - is NOT.
  3. It runs before the TransactionGroup opens: Revit's documentation says the
     value "may not be dependable in the middle of a local transaction".
  4. The reason reaches the answer, one reason at a time: the preview's
     summary, a `skipReasons` field on both replies, and the audit.
  5. The four enum values it names exist, with those names, on all eight
     releases - read by `tools/api-surface --members` when the release
     assemblies are cached here, and said NOT RUN when they are not.
"""

import io
import os
import re
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
WRITE = os.path.join(ROOT, "revit", "Heron.Revit.Addin", "RevitWrite.cs")
READER = os.path.join(ROOT, "tools", "api-surface", "bin", "Debug", "ApiSurface.dll")
CACHE = os.path.join(ROOT, "tools", "api-surface", ".assemblies")

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def without_comments(text):
    """The C# with its comments removed and its strings left whole.

    The words this suite looks for are all named in comments too, and a
    comment is not a call - but section 4 reads what the strings SAY, so
    unlike a blanking stripper this keeps them. Character by character,
    because a regex cannot tell a `//` inside a string from a comment.
    """
    out = []
    i, n = 0, len(text)
    while i < n:
        two = text[i:i + 2]
        if two == "//":
            while i < n and text[i] != "\n":
                i += 1
            continue
        if two == "/*":
            end = text.find("*/", i + 2)
            i = n if end < 0 else end + 2
            out.append(" ")
            continue
        if text[i] == '"':
            verbatim = i > 0 and text[i - 1] == "@"
            j = i + 1
            while j < n:
                if verbatim and text[j] == '"' and j + 1 < n and text[j + 1] == '"':
                    j += 2
                    continue
                if not verbatim and text[j] == "\\":
                    j += 2
                    continue
                if text[j] == '"':
                    break
                j += 1
            out.append(text[i:j + 1])
            i = j + 1
            continue
        out.append(text[i])
        i += 1
    return "".join(out)


def body_of(code, header):
    at = code.find(header)
    if at < 0:
        return ""
    start = code.find("{", at)
    depth = 0
    for i in range(start, len(code)):
        if code[i] == "{":
            depth += 1
        elif code[i] == "}":
            depth -= 1
            if depth == 0:
                return code[start:i + 1]
    return ""


def main():
    try:
        raw = io.open(WRITE, encoding="utf-8").read()
    except (IOError, OSError) as why:
        print("FAILED - could not read %s (%s)" % (WRITE, why))
        return 1
    code = without_comments(raw)
    partition = body_of(code, "static void Partition(")

    print("\n1. asked only of a workshared model, and only after ownership")
    workshared = body_of(partition, "if (workshared)")
    owned = workshared.find("GetCheckoutStatus(")
    central = workshared.find("GetModelUpdatesStatus(")
    check(central > 0, "Partition asks WorksharingUtils.GetModelUpdatesStatus, inside "
                       "`if (workshared)`")
    check(0 < owned < central,
          "and asks it AFTER ownership, so an element someone holds keeps its reason")
    check("GetModelUpdatesStatus(" not in partition.replace(workshared, ""),
          "and nowhere outside the workshared branch - a model that is not "
          "workshared has no central to be behind")

    print("\n2. what is skipped, and what is deliberately not")
    for status in ("UpdatedInCentral", "DeletedInCentral"):
        check(re.search(r"ModelUpdatesStatus\.%s\s*\)" % status, workshared) is not None,
              "%s is skipped" % status)
    check("NotYetInCentral" not in workshared,
          "NotYetInCentral is NOT - an element made in this copy is the user's to move")
    check("CurrentWithCentral" not in workshared,
          "and nothing is decided by naming CurrentWithCentral, so a status added "
          "later is movable rather than silently skipped for no stated reason")
    for bucket in ("Pinned", "Owned", "ChangedInCentral", "DeletedInCentral"):
        check("skipped.%s.Add(id)" % bucket in partition,
              "an element skipped as %s is kept under that reason" % bucket)

    print("\n3. before the transaction, never inside it")
    execute = body_of(code, "static string ExecuteMove(")
    check(0 < execute.find("Partition(") < execute.find("new TransactionGroup("),
          "the re-count runs before the TransactionGroup opens")

    print("\n4. the reason reaches the answer, one reason at a time")
    check('" that are pinned or owned by someone else"' not in raw,
          "the blended reason is gone - it named two of four as if they were one")
    why = body_of(code, "public string Why(")
    check(bool(why), "Skips.Why() is there")
    for said in ("pinned", "owned by someone else",
                 "changed in central since this model last reloaded",
                 "Reload", "deleted in central"):
        check(said in why, "Why() can say \"%s\"" % said)
    check(len(re.findall(r'Json\.Str\("skipReasons",\s*skipped\.Why\(\)\)', raw)) == 2,
          "`skipReasons` is on the preview AND the move reply")
    check('"skipReasons", skipped.Why())' in raw
          and re.search(r'KeyValuePair<string, string>\("skipReasons"', raw) is not None,
          "and in the audit record")
    check('"willSkip", skipped.Count' in raw and '"skipped", skipped.Count' in raw,
          "and the two counts the chat already reads are unchanged")
    check("UniqueIds(doc, skipped.All())" in raw,
          "and the audit still lists every skipped element, whatever the reason")

    print("\n5. the four statuses exist, under those names, on every cached release")
    cached = sorted(name for name in (os.listdir(CACHE) if os.path.isdir(CACHE) else [])
                    if name.isdigit())
    if len(cached) < 8 or not os.path.isfile(READER):
        print("  NOT RUN - the eight releases' assemblies or the reader are not here")
        print("  (`python tools/check-api-surface.py` fetches and builds both).")
        print("  Sections 1 to 4 do not need them; this one proves nothing either way.")
    else:
        done = subprocess.run(["dotnet", READER, "--members", "ModelUpdatesStatus"],
                              stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
        said = done.stdout.decode("utf-8", "replace")
        span = "%s-%s" % (cached[0], cached[-1])
        for status in ("CurrentWithCentral", "NotYetInCentral", "DeletedInCentral",
                       "UpdatedInCentral"):
            check(re.search(r"^\s+%s\s+value\s+%s = \d+\s*$" % (re.escape(span), status),
                            said, flags=re.M) is not None,
                  "%s is on %s" % (status, span))
        done = subprocess.run(["dotnet", READER, "--members",
                               "WorksharingUtils.GetModelUpdatesStatus"],
                              stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
        said = done.stdout.decode("utf-8", "replace")
        check("%s  method    static ModelUpdatesStatus GetModelUpdatesStatus"
              "(Document document, ElementId elementId)" % span in said,
              "and GetModelUpdatesStatus has the one signature on all of them")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the move pre-check skips what changed in central, by reason")
    return 0


if __name__ == "__main__":
    sys.exit(main())
