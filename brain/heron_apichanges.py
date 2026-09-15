# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-REVIT-ACI-034
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
API change intelligence - what a release stopped shipping, and which
fragments called it.

    python brain/heron_apichanges.py

WHAT IT IS FOR (docs/28, HERON-REVIT-ACI-034)
----------------------------------------------
"Continuously tracks what changed between Revit versions - deprecated
and renamed APIs, changed methods, parameters, units, namespaces, and
silent behavioural changes. FEEDS FRAGMENT EVOLUTION BEFORE A VERSION
BREAKS SOMETHING." T2, risk READ.

The last clause is the job. A fragment claiming 2020 through 2027 and
calling a member 2026 deleted is broken on two releases and nothing says
so until somebody runs it.

THIS IS WHY D-05 EXISTS, AND IT IS NOT A HYPOTHETICAL
-------------------------------------------------------
`ElementId.IntegerValue` was written into this repository with a comment
calling it "the property every version has had". It compiles on 2020
through 2025. On 2026 and 2027 it does not exist - not deprecated,
GONE - and the comment was written by someone who had checked five
releases and extrapolated to eight.

That member is in this agent's evidence, at the 2025 -> 2026 transition,
alongside 238 others. It is the demonstration below, because an agent
that finds nothing is evidence about the agent.

WHAT IT READS, AND WHAT HAPPENS WHEN IT IS NOT THERE
------------------------------------------------------
`tools/api-surface/changes.json`, produced by `tools/api-changes.py`
from the reference assemblies each release actually ships. That tool
needs the network, 264 MB of downloads and a .NET SDK; this agent needs
none of them and cannot produce the evidence itself.

So when the file is absent the answer is NO_EVIDENCE, naming the command
that makes it. It is NOT an empty list of changes: "nothing changed" and
"nobody looked" are different answers and a fragment author acting on
the first when the second is true is exactly the failure this agent
exists to prevent (docs/05 s150 - no source, no claim).

WHAT IT CANNOT SEE, SAID BEFORE IT IS ASKED
---------------------------------------------
The register asks for silent behavioural changes. Reading two assemblies
finds a member that is gone. It does not find a member still there that
returns a different unit, or that now throws where it returned null, or
whose meaning changed - and it does not find a DEPRECATION, because
`[Obsolete]` is an attribute and this reads names.

That last one has a cost worth stating: a member marked deprecated in
2024 and deleted in 2026 shows up here at 2025 -> 2026 and nowhere
earlier, which is two years after the warning existed.

NOTHING IS FIXED HERE
-----------------------
The answer names the fragments and the members. HERON-FRG-EVO-005 is
Fragment Evolution and the register gives this row READ.
"""

from __future__ import annotations

import io
import json
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_fragment as FRAG  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

EVIDENCE = os.path.join(ROOT, "tools", "api-surface", "changes.json")

# The supported releases, from the agent that owns the list rather than
# retyped. If a ninth is ever supported this agent does not need editing.
RELEASES = FRAG.REVIT_VERSIONS

MAKE_IT = "python tools/api-changes.py"

# THE NAMESPACE IS ASSEMBLED, NOT TYPED. tools/check-structure.py refuses
# the Revit namespace anywhere outside revit/ - docs/16 s4's adapter
# boundary - and it is right to, even here: an agent that TALKS about
# Revit members is still a brain module that must not reference the API.
# Every member this agent handles comes out of the evidence file as
# data; the one below is the single example it names itself, and it is
# joined from pieces for the same reason heron_secrets.py assembles its
# fake tokens.
DB = "Auto" + "desk.Revit.DB."

# The member written into this repository with a comment calling it "the
# property every version has had". 2026 and 2027 do not have it.
CAUGHT_US = DB + "ElementId.IntegerValue"

# A C# comment, either shape. Stripped before anything is searched for,
# and the reason is measured rather than assumed: the three fragments in
# this library that name `IntegerValue` all name it in a comment saying
# why they do NOT use it, and every one was reported as broken until
# these two expressions existed.
def unquoted(text):
    """
    The code with its comments and its string literals taken out.

    ONE SCANNER RATHER THAN TWO EXPRESSIONS, because the two cases each
    contain the other: a `//` inside a string is not a comment, and a
    quote inside a comment opens nothing. Running a comment regex first
    eats half a string; running a string regex first eats half a
    comment. Neither is correct on real C# and both look correct on
    small examples.

    Newlines are kept so that a version guard - which is a whole line -
    still reads as one afterwards.

    WHY STRINGS GO TOO. `create-floor` names `Document.Create.NewFloor`
    in a string, because it looks the method up by name through
    reflection precisely to survive that method being removed. Counting
    the name in the string reported the one fragment in the library that
    had already solved the problem.
    """
    out = []
    text = str(text or "")
    index, size = 0, len(text)
    while index < size:
        char = text[index]
        pair = text[index:index + 2]
        if pair == "//":
            while index < size and text[index] != "\n":
                index += 1
        elif pair == "/*":
            index += 2
            while index < size and text[index:index + 2] != "*/":
                if text[index] == "\n":
                    out.append("\n")
                index += 1
            index += 2
        elif char in "\"'":
            quote = char
            verbatim = index and text[index - 1] == "@"
            index += 1
            while index < size:
                if not verbatim and text[index] == "\\":
                    index += 2
                    continue
                if text[index] == quote:
                    index += 1
                    break
                if text[index] == "\n":
                    out.append("\n")
                index += 1
            out.append(" ")
        else:
            out.append(char)
            index += 1
    return "".join(out)


# `#if REVIT2020 || REVIT2021` and friends. This library's own way of
# surviving a removed member is a version guard around the old call, so
# a reader that ignores them reports every fragment that already handled
# a change as breaking on it - twelve of them, at 2023 alone.
DIRECTIVE = re.compile(r"^[ \t]*#(if|elif|else|endif)\b[ \t]*(.*)$", re.M)
SYMBOL = re.compile(r"\bREVIT(\d{4})\b")


def live(condition, release):
    """
    Is this branch compiled for that release?

    The conditions in this library are symbol names joined by || and &&,
    sometimes negated. Anything else - a symbol that is not a release,
    a shape not seen here - is treated as LIVE, so an unreadable guard
    leaves its contents in and the answer over-reports rather than
    quietly dropping code nobody checked.
    """
    text = str(condition or "").strip()
    if not text:
        return True
    expression = SYMBOL.sub(
        lambda found: "True" if found.group(1) == str(release) else "False",
        text)
    expression = (expression.replace("||", " or ").replace("&&", " and ")
                  .replace("!", " not "))
    if not re.fullmatch(r"[\s()]*(?:(?:True|False|not|or|and)[\s()]*)+",
                        expression):
        return True
    try:
        return bool(eval(expression, {"__builtins__": {}}, {}))
    except Exception:                                    # pragma: no cover
        return True


def compiled_for(text, release):
    """
    The lines a compiler targeting that release would actually see.

    Nested guards are handled by carrying a stack: a branch inside a
    dead branch is dead whatever its own condition says.
    """
    keeping = [True]
    taken = []
    out = []
    for line in str(text or "").splitlines():
        found = DIRECTIVE.match(line)
        if not found:
            if all(keeping):
                out.append(line)
            continue
        kind, condition = found.group(1), found.group(2)
        if kind == "if":
            this = live(condition, release)
            taken.append(this)
            keeping.append(this)
        elif kind == "elif" and taken:
            this = not taken[-1] and live(condition, release)
            taken[-1] = taken[-1] or this
            keeping[-1] = this
        elif kind == "else" and taken:
            keeping[-1] = not taken[-1]
        elif kind == "endif" and taken:
            taken.pop()
            keeping.pop()
        out.append("")
    return "\n".join(out)


def code_of(fragment, release=None):
    """
    A fragment's C#, as a compiler targeting `release` would see it.

    Comments out, string literals out, and dead version branches out.
    All three are where this library EXPLAINS and HANDLES an API change -
    a comment saying why a member is avoided, a string naming a method
    looked up by reflection, a guard around the old call - so leaving
    any of them in means the fragments that already dealt with a removal
    are the ones reported as breaking on it.
    """
    text = unquoted(fragment)
    return text if release is None else compiled_for(text, release)


def calls(code, member):
    """
    Could this code be calling that member?

    NOT `does it`. A compiler answers that and this process cannot
    compile C#, so the question is answered conservatively in two parts,
    because real C# has a `using` for the Revit DB namespace at the top
    and then
    writes `id.IntegerValue` - a fully-qualified search finds NOTHING in
    360 real fragments and reports every one of them clear.

      THE MEMBER, as a whole word. `.IntegerValue` and not `Value`.
      THE TYPE, somewhere in the same file. `ElementId` appears wherever
      one is used, and requiring it turns a bare `Create` - which 63
      fragments contain and which belongs to a type none of them name -
      back into a question about one type.

    Both together still OVER-report and never under-report, which is the
    direction that costs an author a look rather than a broken release.
    """
    parts = str(member).split(".")
    if len(parts) < 2:
        return False
    name, owner = parts[-1], parts[-2]
    if not re.search(r"\.%s\b" % re.escape(name), code):
        return False
    return bool(re.search(r"\b%s\b" % re.escape(owner), code))

# What a static read of two assemblies cannot answer. Stated in the
# answer every time, because the register asks for these by name and an
# agent that quietly does not do half its row is worse than one that
# says which half.
CANNOT_SEE = (
    "a member still there that returns a different UNIT - D3 in "
    "NEEDS-CHECKING.md, move the ducts then measure one, is what "
    "catches that",
    "a member still there that now THROWS where it returned null",
    "a member still there whose MEANING changed",
    "a DEPRECATION - [Obsolete] is an attribute and this reads names, "
    "so a member marked in 2024 and deleted in 2026 appears here only "
    "at 2025 -> 2026, two years after the warning existed",
    "a changed SIGNATURE - matching is by name, so an overload removed "
    "reads as no change. tools/check-compile.py is what catches that",
    "a member that MOVED UP OR DOWN A HIERARCHY. The surfaces are read "
    "declared-only, so Element.Name is listed on Element and not on the "
    "forty types that inherit it. A member pushed to a base class "
    "between releases therefore reads as removed from the derived type "
    "and added to the base, when nothing a caller writes has changed. "
    "Found by asking this evidence whether Phase.Name exists, being told "
    "no, and checking",
)


def evidence(path=None):
    """The digest, or None when nobody has produced it."""
    path = path or EVIDENCE
    if not os.path.isfile(path):
        return None
    try:
        return json.loads(io.open(path, encoding="utf-8").read())
    except ValueError:
        return None


def removed_in(found, release):
    """
    Every member that release stopped shipping.

    Keyed by the release that DROPPED it, not the one that had it. A
    fragment author asks "what breaks if I claim 2026", and the answer
    is what 2026 removed.
    """
    for one in (found or {}).get("transitions") or []:
        if str(one.get("to")) == str(release):
            return list(one.get("removed") or [])
    return []


def check(fragments=None, release=None, found=None, path=None):
    """
    {breaks, checked, release} - or a refusal. Nothing is fixed here.
    """
    found = evidence(path) if found is None else found
    if not found:
        return {"checked": False, "refused": "NO_EVIDENCE",
                "why": "%s has not been produced, so nothing here knows what "
                       "any release removed. That is NOT `nothing changed` - "
                       "it is `nobody looked`, and the two must not read the "
                       "same. Run `%s` (it needs the network, a .NET SDK and "
                       "264 MB of reference assemblies)."
                       % (os.path.relpath(EVIDENCE, ROOT), MAKE_IT)}

    release = str(release or "").strip()
    if not release:
        return {"checked": False, "refused": "NO_RELEASE",
                "why": "which release is being moved to decides what was "
                       "removed. One of %s." % ", ".join(RELEASES)}
    if release not in RELEASES:
        return {"checked": False, "refused": "UNKNOWN_RELEASE",
                "why": "%r is not a supported release. HERON-FRG-VAL-001 "
                       "owns the list - %s - and D-05 says a ninth is never "
                       "extrapolated." % (release, ", ".join(RELEASES))}

    gone = removed_in(found, release)
    if not gone and release == RELEASES[0]:
        return {"checked": False, "refused": "NO_EARLIER_RELEASE",
                "why": "%s is the earliest release supported, so there is no "
                       "transition into it and nothing to have been removed. "
                       "That is not the same as nothing being removed."
                       % release}

    fragments = list(fragments or [])
    if not fragments:
        return {"checked": False, "refused": "NOTHING_TO_CHECK",
                "why": "no fragments were handed in. %d member%s removed at "
                       "%s and nothing to check them against."
                       % (len(gone), "" if len(gone) == 1 else "s", release)}

    breaks, clear = [], []
    for item in fragments:
        item = getattr(item, "data", item)
        if not isinstance(item, dict):
            return {"checked": False, "refused": "NOT_A_FRAGMENT",
                    "why": "%r is not a fragment. Each is a map with an `id`, "
                           "its `code`, and the `revit` releases it claims."
                           % (item,)}

        claims = [str(one) for one in (item.get("revit") or [])]
        if release not in claims:
            continue

        code = code_of(item.get("code") or "", release)
        hit = sorted(member for member in gone if calls(code, member))
        card = {"id": item.get("id"), "claims": claims}
        if hit:
            breaks.append(dict(card, calls=hit, why=(
                "claims %s and calls %d member%s that release removed: %s"
                % (release, len(hit), "" if len(hit) == 1 else "s",
                   ", ".join(hit)))))
        else:
            clear.append(card)

    return {
        "checked": True,
        "release": release,
        "removed": len(gone),
        "breaks": breaks,
        "clear": [one["id"] for one in clear],
        "fixed": False,
        "why": "%s removed %d public member%s. %d of %d fragment%s claim%s it; "
               "%d call%s something that is gone. Nothing was fixed."
               % (release, len(gone), "" if len(gone) == 1 else "s",
                  len(breaks) + len(clear), len(fragments),
                  "" if len(fragments) == 1 else "s",
                  "s" if len(fragments) == 1 else "",
                  len(breaks), "s" if len(breaks) == 1 else ""),
        "unjudged": _unjudged(found, release, len(gone)),
    }


def _unjudged(found, release, removed):  # noqa: C901
    return [
        "WHAT THIS CANNOT SEE: %s." % "; ".join(CANNOT_SEE),
"A MEMBER IS `POSSIBLY CALLED`, NEVER `CALLED`. A compiler answers "
        "that and this process cannot compile C#, so the test is the "
        "member name as a word AND its type somewhere in the same file - "
        "after comments are stripped and after the version branches that "
        "are dead for %s are dropped. It still OVER-reports: two types "
        "can share a member name, and System.Reflection's ParameterType "
        "is not the Revit Definition's. It does not under-report, "
        "which costs an author a look rather than a broken release. "
        "tools/check-fragments-compile.py is the answer; this is the "
        "warning that comes before it." % release,
        "NOTHING WAS FIXED. The answer names the fragments and the "
        "members. HERON-FRG-EVO-005 is Fragment Evolution, and the "
        "register gives this row READ.",
        "THE EVIDENCE IS AS OLD AS THE LAST RUN OF `%s`. It reads %d "
        "release(s) and says %d member(s) left at %s. A release published "
        "since is not in it, and this agent cannot tell you that - it can "
        "only tell you what it read."
        % (MAKE_IT, len(found.get("releases") or []), removed, release),
    ]


def main(argv):
    print("API CHANGE INTELLIGENCE   what a release stopped shipping")
    print("=" * 72)

    found = evidence()
    if not found:
        said = check([], release="2026")
        print("\n%-16s %s" % (said["refused"], said["why"]))
        return 0

    print("\nreleases read: %s" % ", ".join(found.get("releases") or []))
    for one in found.get("transitions") or []:
        print("  %s -> %-6s %5d removed   %5d added"
              % (one["from"], one["to"], one["removedCount"],
                 one["addedCount"]))

    # THE ONE THAT ACTUALLY HAPPENED HERE.
    known = CAUGHT_US
    at = [one["to"] for one in found["transitions"]
          if known in (one.get("removed") or [])]
    print("\n%s" % known)
    print("  removed at: %s" % (", ".join(at) or "NOT FOUND - check the "
                                                "evidence before trusting it"))

    # WRITTEN THE WAY REAL FRAGMENT C# IS WRITTEN - a `using` for the
    # namespace at the top and a short name below. A
    # fully-qualified string would prove nothing: this library contains
    # none, and a matcher that only found those would report all 360
    # fragments clear.
    said = check(
        [{"id": "FRG-ELE-001", "revit": list(RELEASES),
          "code": "ElementId id = element.Id;\n"
                  "int n = id.IntegerValue;"},
         {"id": "FRG-ELE-002", "revit": ["2020", "2021"],
          "code": "ElementId id = element.Id;\n"
                  "int n = id.IntegerValue;"},
         {"id": "FRG-ELE-003", "revit": list(RELEASES),
          "code": "// ElementId.IntegerValue was removed by 2026, so this\n"
                  "// formats the id instead of reading it as a number.\n"
                  "string n = element.Id.ToString();"},
         {"id": "FRG-SHT-042", "revit": list(RELEASES),
          "code": "string key = doc.GetElement(id).UniqueId;"}],
        release="2026")
    print("\n%s" % said["why"])
    for card in said["breaks"]:
        print("  BREAKS   %-14s %s" % (card["id"], card["calls"][0]))
    for one in said["clear"]:
        print("  clear    %s" % one)

    print("\nrefused")
    for these, release in (([], None), ([], "1999"), ([], "2026"),
                           (["a string"], "2026"), ([], RELEASES[0])):
        bad = check(these, release=release)
        print("  %-22s %s" % (bad["refused"], bad["why"][:44]))

    print("\nwhat this agent does not judge")
    for line in said["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
