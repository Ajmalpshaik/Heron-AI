# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-DEV-CSH-005
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
C# idiom - the plausible zero, in the language the gate does not cover.

    python brain/heron_csharp.py

WHAT IT IS FOR (docs/28, HERON-DEV-CSH-005)
--------------------------------------------
"C# Agent. C# language and idiom." T2.

IT HOLDS NO OPINION ABOUT C#, AND THAT IS DELIBERATE
------------------------------------------------------
"Idiom" invites a style guide written from general knowledge, and a rule
this repository does not follow is worse than no rule - it is an agent
correcting working code on an authority it does not have.

So the first thing built was a MEASUREMENT of what this repository's C#
actually does, and it immediately killed the obvious rule. Measured by
this module on 2026-09-15, across 24 C# files in revit/, platform/ and
mcp/, there were 82 catch clauses:

    30   catch  with no type at all
    18   catch (Exception)
    34   a narrow type

A "narrow exception types only" rule would be false about the larger
half of the code that already ships. It is not asserted here.

THE FIGURES ARE DATED AND THE CLAIM IS WHAT IS CHECKED
--------------------------------------------------------
Every one of those numbers moves the next time somebody writes a C#
file, so the suite does NOT pin them - pinning a count means the next
author's first experience of this agent is a red suite over a number
that was only ever an illustration.

That was learned the same day: adding RevitPhases.cs moved 74 to 82 and
26 to 34 within the hour, and the first version of the suite went red on
all three figures.

AND THE CLAIM ITSELF HAD TO BE REPLACED, ON 2026-09-16
--------------------------------------------------------
The claim the suite checked used to be "broad still outnumbers narrow,
so the rule this module refuses to assert is one its own repository
would fail". On 2026-09-16 that stopped being true. One new file -
RevitParameters.cs, sixteen narrow handlers and not one broad - took the
shipped C# from 53 broad against 42 narrow to 53 against 58, and narrow
led for the first time.

The refusal did not change, because the majority was never the real
reason for it. FIFTY-THREE BROAD HANDLERS STILL SHIP AND STILL WORK, and
an agent that flags fifty-three working handlers is correcting working
code on an authority it does not have - which is as true at 58-53 as it
was at 42-53. A claim that can flip on one commit was the wrong claim to
hang a refusal on, and it flipped on one commit.

So what the suite checks now is the part that does not tip: that broad
handlers remain a real share of the code rather than a handful of
survivors. Crossing THAT would take somebody deliberately rewriting
most of them, which is an event worth a red suite.

Whether this module should now begin asserting the rule is a question
for a person, and it is written up as F37 in docs/PROPOSALS.md rather
than decided here.

AND THE FIRST SET WAS WRONG FOR A DIFFERENT REASON
----------------------------------------------------
A scratch expression written to survey the code before this module
existed reported 83, 39 and 18. It counted `catch` written inside
comments, and counted the word where no block followed. That is not
drift, it is a worse instrument, and it is why the figures above come
from running this module rather than from reading the code by eye.

WHAT IS ASSERTED IS THIS REPOSITORY'S OWN RULE, IN ITS OTHER LANGUAGE
----------------------------------------------------------------------
D-52 is the PLAUSIBLE ZERO: an answer that is wrong in a way nobody
doubts. `tools/check-narrow-errors.py` enforces it and enforces it
WELL - and only over Python, only over sqlite3 handlers, because that
is where it kept happening. Its own docstring says reviews found the
same defect in four files on four consecutive days.

Nothing checks the same shape in C#. Of the 48 broad handlers there:

    14   swallow silently - an empty block
    10   `continue` - the item is dropped out of a loop
     3   return null, false or an empty collection

Twenty-seven handlers where a fault and the normal case come back
identical. That is D-52's shape exactly, and Golden Rule 14 - never
silently discard - is the same objection said another way.

IT NAMES THEM. IT DOES NOT CONDEMN THEM
-----------------------------------------
Some of those handlers are right. Iterating every value of a large
enumeration and skipping the ones that will not resolve is a normal
thing to do, and this repository does it on purpose. Whether a swallow
is correct depends on what can actually be thrown there, and only a
person reading the call knows that.

So the answer names each one with its line and what it does, and says
which of the three shapes it is. It does not say "bug", it does not
count them as failures, and it fixes nothing.

COMMENTS AND STRINGS ARE NOT CODE
-----------------------------------
`unquoted` is HERON-REVIT-ACI-034's, bound rather than rewritten. That
agent learned it the expensive way: a member named in a comment saying
why it is NOT used was counted as using it, three times over. A `catch`
written inside a comment is the same mistake waiting.
"""

from __future__ import annotations

import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_apichanges as ACI  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# HERON-REVIT-ACI-034's scanner, bound by identity. It removes comments
# AND string literals in one pass, which two expressions cannot do
# correctly because each case contains the other.
unquoted = ACI.unquoted

# A catch clause and the type it names, if any. The optional `when (...)`
# is an exception FILTER. This repository has none today, and a matcher
# that stopped at the `)` would skip every handler that grew one - which
# is the wrong direction to be wrong in, because a missed handler is
# silently not reported rather than wrongly reported.
CATCH = re.compile(
    r"\bcatch\s*(?:\(\s*([A-Za-z0-9_.]+)[^)]*\))?"
    r"(?:\s*when\s*\([^{]*\))?\s*\{")

# The types that catch everything. A bare `catch` is the broadest of all
# and does not even name what it swallowed.
BROAD = ("", "Exception", "SystemException")

# The three shapes D-52 names, as a C# handler writes them. The order
# matters: an empty block is checked first because every other test
# would also match nothing.
NOTHING = re.compile(r"^\s*$")
DROPS = re.compile(r"^\s*(?:continue|break)\s*;?\s*$")
EMPTY_ANSWER = re.compile(
    r"^\s*return\s+(?:null|false|0|string\.Empty|\"\"|new\s+[^;]*\(\s*\))\s*;"
    r"\s*$")

# What each shape does to the caller, in the words that say why it
# matters rather than what it looks like.
SHAPES = (
    ("SWALLOWS_SILENTLY", NOTHING,
     "the block is empty, so the fault and the normal case leave no "
     "difference behind at all"),
    ("DROPS_THE_ITEM", DROPS,
     "the item leaves the loop and the count comes back smaller, with "
     "nothing saying one was skipped (Golden Rule 14)"),
    ("RETURNS_AN_EMPTY_ANSWER", EMPTY_ANSWER,
     "a fault comes back as `nothing found`, which is D-52's plausible "
     "zero in as many words"),
)

WHY_IT_MATTERS = (
    "D-52 - THE PLAUSIBLE ZERO: an answer that is wrong in a way nobody "
    "doubts. tools/check-narrow-errors.py enforces exactly this and "
    "enforces it well, over Python sqlite3 handlers only, because that "
    "is where it kept happening. The same shape in C# is checked by "
    "nothing.")


def _block(text, brace):
    """The body of the block that opens at `brace`, or None if it never closes."""
    depth, index = 0, brace
    while index < len(text):
        if text[index] == "{":
            depth += 1
        elif text[index] == "}":
            depth -= 1
            if depth == 0:
                return text[brace + 1:index]
        index += 1
    return None


def handlers(code):
    """
    Every catch clause, with what its body does to the caller.

    Read from the code with comments and string literals removed, so a
    `catch` written inside a comment explaining one is not counted as
    one.
    """
    text = unquoted(code)
    found = []
    for match in CATCH.finditer(text):
        kind = match.group(1) or ""
        body = _block(text, match.end() - 1)
        if body is None:
            return None            # unreadable - the caller refuses
        shape = None
        for name, pattern, why in SHAPES:
            if pattern.match(body):
                shape = (name, why)
                break
        found.append({
            "type": kind or "(no type)",
            "broad": kind in BROAD,
            "line": text[:match.start()].count("\n") + 1,
            "shape": shape[0] if shape else None,
            "why": shape[1] if shape else None,
            "body": " ".join(body.split())[:60],
        })
    return found


def review(code, where=None):
    """
    {reviewed, plausible_zero, profile} - or a refusal. Nothing is fixed
    and nothing is called wrong.
    """
    if code is None or (isinstance(code, str) and not code.strip()):
        return {"reviewed": False, "refused": "NOTHING_TO_READ",
                "why": "no code was handed in."}
    if not isinstance(code, str):
        return {"reviewed": False, "refused": "NOT_CODE",
                "why": "%r is not source text. This reads C# as a string."
                       % (code,)}

    found = handlers(code)
    if found is None:
        return {"reviewed": False, "refused": "UNREADABLE",
                "why": "a catch block in %s never closes. Reporting what the "
                       "rest of the file does would mean answering about a "
                       "file nobody can compile."
                       % (where or "this code")}

    broad = [one for one in found if one["broad"]]
    named = [one for one in broad if one["shape"]]
    counts = {}
    for one in named:
        counts[one["shape"]] = counts.get(one["shape"], 0) + 1

    return {
        "reviewed": True,
        "where": where,
        "handlers": len(found),
        "broad": len(broad),
        "narrow": len(found) - len(broad),
        "plausible_zero": named,
        "shapes": counts,
        "fixed": False,
        "judged_wrong": False,
        "why": "%d catch clause%s: %d broad, %d narrow. %d cannot tell a "
               "fault from the normal case. %s"
               % (len(found), "" if len(found) == 1 else "s", len(broad),
                  len(found) - len(broad), len(named),
                  "Nothing here is called wrong." if named
                  else "None of the broad ones returns silently."),
        "unjudged": _unjudged(found, broad, named),
    }


def _unjudged(found, broad, named):
    return [
        "WHETHER ANY OF THESE IS WRONG. Some are right: iterating every "
        "value of a large enumeration and skipping the ones that do not "
        "resolve is a normal thing to do, and this repository does it on "
        "purpose. Whether a swallow is correct depends on what can "
        "actually be thrown there, and only a person reading the call "
        "knows that. %d handler(s) are NAMED, none is condemned."
        % len(named),
        "WHETHER A BROAD CATCH IS BAD STYLE. It is not asserted here, "
        "because it is not true of this repository: broad catches "
        "outnumber narrow ones in revit/, platform/ and mcp/, and 30 "
        "have no type at all. A rule contradicted by the larger half "
        "of the shipping code is an agent correcting working code on an "
        "authority it does not have. %d of the %d here are broad and "
        "that alone is not a finding." % (len(broad), len(found)),
        WHY_IT_MATTERS,
        "WHETHER THE CODE COMPILES, or does the right thing. This reads "
        "text. tools/check-compile.py compiles it on eight releases and "
        "HERON-REVIT-ACI-034 says what those releases removed; neither "
        "of them looks at a catch block, and this does not look at "
        "anything else.",
        "NOTHING WAS FIXED. The answer names a line and says what the "
        "handler does to the caller. What to do about it is the author's.",
    ]


def main(argv):
    print("C# IDIOM   the plausible zero, in the other language")
    print("=" * 72)

    sample = """
    using System;

    internal static class Example
    {
        public static string Read(Document doc)
        {
            // A catch written in a COMMENT is not a handler:
            //     catch { }
            try { return doc.Title; }
            catch (IOException) { throw; }
        }

        public static int Count(Document doc)
        {
            var total = 0;
            foreach (var one in doc.Everything)
            {
                try { total += one.Size; }
                catch { continue; }
            }
            try { doc.Flush(); } catch { }
            return total;
        }

        public static string Key(Document doc)
        {
            try { return doc.Key; }
            catch (Exception) { return null; }
        }
    }
    """

    said = review(sample, where="Example.cs")
    print("\n%s" % said["why"])
    for one in said["plausible_zero"]:
        print("  line %-4d %-24s %s"
              % (one["line"], one["shape"], one["why"][:40]))
    print("\n  narrow, and left alone : %d" % said["narrow"])
    print("  a catch in a comment   : not counted")

    print("\nrefused")
    for code, label in ((None, "nothing"), ("   ", "whitespace"),
                        (42, "not source text"),
                        ("try { x(); } catch { ", "a block that never closes")):
        bad = review(code)
        print("  %-26s %-18s %s" % (label, bad["refused"], bad["why"][:32]))

    print("\nwhat this agent does not judge")
    for line in said["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
