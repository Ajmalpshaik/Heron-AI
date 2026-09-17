# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-DEV-RGR-014
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
The build matrix, against the one that was recorded - per release, per field.

    python brain/heron_buildmatrix.py
    python brain/heron_buildmatrix.py --update      record what it is now

WHAT IT IS FOR (docs/28, HERON-DEV-RGR-014)
--------------------------------------------
"Golden-file comparison across supported versions." T1.

WHICH VERSIONS, AND WHOSE - THE PART D-75 HAD TO SETTLE FIRST
---------------------------------------------------------------
This row had two candidates and the decision ruled BOTH out:
tests/golden/cases.py and tests/test_golden.py are the FRAGMENT side, and
D-75 says a Development agent acts on Heron itself. So the row had no file
at all, and the subject had to be named before one could be written.

It is Heron's own build matrix: what each supported Revit release resolves
to BEFORE anything is compiled - its target framework, its .NET major,
whether it needs the WindowsDesktop targets, and its compile symbols.

docs/28 names the caller in its own words, in the same department, under
HERON-DEV-NUP-019: the .NET Update Agent "retargets a framework, bumps
packages, migrates project files - REGRESSION MATRIX MUST PASS BEFORE IT IS
ACCEPTED". This is that matrix, and until now nothing held it.

WHY THE FILE IS NOT CALLED AFTER THE AGENT
-------------------------------------------
tools/new-agent.py derived `heron_regression_test.py` from the row's name,
and tests/test_references.py rejected it: **no module name in brain/ may be
a prefix of another**, and `heron_regression.py` (HERON-FRG-REG-006, the
fragment one) already existed. That rule is not cosmetic - the rename and
search this repository does over module names cannot tell a whole name from
the start of a longer one.

So the file is named for what it compares. The scaffolder does not know the
rule and will make the same name again; that is written down in PROPOSALS
rather than fixed here.

WHY A COMMITTED FILE AND NOT A RECOMPUTED ONE
-----------------------------------------------
A baseline regenerated on every run compares the matrix against itself and
can never disagree with anything. So brain/regression-matrix.json is
committed, and moving it is a deliberate act: `--update` prints the diff
first and writes second.

That is the whole mechanism. A retarget that updates the baseline in the
same change RECORDS the move rather than catching it - which is fine when
somebody meant it, and is the reason the flag exists rather than the
behaviour being automatic.

THE MATRIX IS READ, NEVER MIRRORED
------------------------------------
Directory.Build.props is the one place that maps a release to a runtime,
and HERON-FRG-MTX-009 is the one thing that parses it. This calls that
rather than keeping a second table - the same reason heron_dotnet.py gives
for importing it instead of copying it, and the same drift the props file's
own comment warns about, where '>= 2027' silently claimed every future
Revit is .NET 10.

What this DOES read for itself is the compile symbols, because nothing else
did. They are derived from the same file, by the same shape of condition.

WHAT IT DOES NOT DO
---------------------
**It compiles nothing.** tools/check-compile.py is the gate and it needs a
.NET SDK; this needs no toolchain at all, which is the point - it answers on
any machine, in under a second, before a build is started.

**Agreeing with the golden is not a build.** A matrix can be right and the
code still not compile. The honest sentence travels in `unjudged`.

**It never edits a project or the props.** The only thing `--update` writes
is the golden.
"""

from __future__ import annotations

import io
import json
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_dotnet as NET                                     # noqa: E402
# For repo_relative() and nothing else. Every path this module names in a
# refusal can be handed in by the caller - `golden` and `props` are both
# arguments - and `os.path.relpath` RAISES on Windows across drives rather
# than returning something merely useless. It is called while WORDING A
# REFUSAL, so the crash replaces the message: the reader is told nothing at
# all where they were about to be told exactly what to do.
#
# THIS IS THE FOURTH TIME. heron_authoring, heron_tag and heron_context each
# carry a comment about the same defect, and heron_tag's records that the
# first two were byte-identical copies of one another, bug included - all
# three fixed on 2026-09-15.
#
# THIS MODULE WAS WRITTEN TWO DAYS LATER AND ARRIVED WITH IT ANYWAY (PR #171,
# 2026-09-17), which is the part worth keeping. The lesson had been learnt,
# written down three times, and did not reach the next file: nothing in the
# repository could refuse the spelling, because in most places it is correct.
# The only reason this one was caught within hours is that
# tests/test_buildmatrix.py hands in a temp path and somebody ran it on
# Windows - on Linux /tmp and the checkout share a mount and it passes
# whatever the code says, so CI was green. tools/check-gaps.py named it.
#
# heron_fragment owns the one answer; a fourth private copy would be the
# actual mistake.
import heron_fragment as FRAG                                  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

PROPS = os.path.join(ROOT, "Directory.Build.props")
GOLDEN = os.path.join(ROOT, "brain", "regression-matrix.json")

# A `-windows` suffix is what asks for the WindowsDesktop targets, and
# `netN.0` is what names the .NET major. Both are DERIVED from the target
# framework rather than mirrored, which is HERON-DEV-NET-006's own argument
# for why its two tables are checked against the props instead of trusted.
_MAJOR = re.compile(r"^net(\d+)\.")

# <DefineConstants Condition="...">$(DefineConstants);NAME</DefineConstants>
_SYMBOL = re.compile(
    r"<DefineConstants([^>]*)>\$\(DefineConstants\);([^<]+)</DefineConstants>")
_WHEN = re.compile(r"'\$\(RevitVersion\)'\s*(==|&gt;=|&lt;=|>=|<=)\s*'(20\d\d)'")

# The repository-wide rows. They are not per release, and a comparison that
# filed them under one would report a package bump eight times.
REPOSITORY = "(repository)"


def symbols(release, props=None):
    """
    Every compile symbol Directory.Build.props defines for one release.

    Parsed rather than listed, for the reason the props file's own comment
    gives: the table used to read '>= 2027' and silently claimed every
    future Revit is .NET 10. A list typed here would make that mistake
    twice over.
    """
    try:
        text = io.open(props or PROPS, encoding="utf-8").read()
    except (IOError, OSError):
        return []

    out = []
    for condition, name in _SYMBOL.findall(text):
        name = name.strip().replace("$(RevitVersion)", str(release))
        when = _WHEN.search(condition)
        if when:
            how, against = when.group(1), when.group(2)
            how = {"&gt;=": ">=", "&lt;=": "<="}.get(how, how)
            if how == "==" and str(release) != against:
                continue
            if how == ">=" and str(release) < against:
                continue
            if how == "<=" and str(release) > against:
                continue
        elif "Condition" in condition:
            # A condition this cannot read is not a symbol this can claim.
            continue
        if name not in out:
            out.append(name)
    return out


def matrix(props=None, releases=None):
    """
    The live matrix: per release, plus the rows that are per repository.

    Nothing is compiled and no SDK is probed - what a MACHINE has is
    HERON-DEV-NET-006's question, and it changes between machines, so it
    must never end up in a baseline that is committed.
    """
    import heron_matrix as MTX
    table = MTX.runtimes(props)

    wanted = [str(r) for r in releases] if releases else sorted(table)
    per = {}
    for release in wanted:
        tfm = table.get(release)
        if tfm is None:
            continue
        major = _MAJOR.match(tfm)
        per[release] = {
            "tfm": tfm,
            "dotnetMajor": int(major.group(1)) if major else None,
            "needsWindowsDesktop": tfm.endswith("-windows"),
            "symbols": symbols(release, props),
        }

    return {
        "releases": per,
        "projects": list(NET.PROJECTS),
        "packages": NET.packages(),
    }


def compare(golden, now):
    """
    {same, changed, added, removed} - field by field, never release by
    release.

    A release reported only as "changed" sends somebody back to a diff to
    find out what moved, which is the work this exists to do for them.
    """
    was, is_now = golden.get("releases", {}), now.get("releases", {})
    same, changed = [], []

    for release in sorted(set(was) & set(is_now)):
        differences = []
        for field in sorted(set(was[release]) | set(is_now[release])):
            before, after = was[release].get(field), is_now[release].get(field)
            if before != after:
                differences.append({"release": release, "field": field,
                                    "was": before, "now": after})
        if differences:
            changed.extend(differences)
        else:
            same.append(release)

    for field in ("projects", "packages"):
        before, after = golden.get(field), now.get(field)
        if before != after:
            changed.append({"release": REPOSITORY, "field": field,
                            "was": before, "now": after})

    return {
        "same": same,
        "changed": changed,
        "added": sorted(set(is_now) - set(was)),
        "removed": sorted(set(was) - set(is_now)),
    }


def _read(path):
    try:
        return json.loads(io.open(path, encoding="utf-8").read()), None
    except (IOError, OSError):
        return None, "NO_GOLDEN"
    except ValueError:
        return None, "NOT_A_GOLDEN"


def regression(golden=None, update=False, releases=None, props=None):
    """{passed, same, changed, added, removed} - or a refusal."""
    where = golden or GOLDEN
    now = matrix(props, releases)

    if not now["releases"]:
        return {"passed": False, "refused": "NO_MATRIX",
                "ranAnything": False, "fixedAnything": False,
                "why": "%s names no release, so there is no matrix to "
                       "compare. An empty matrix agrees with every baseline "
                       "there is, which is the one answer that must never be "
                       "reported as a pass." % FRAG.repo_relative(props or PROPS)}

    recorded, problem = _read(where)
    if problem == "NOT_A_GOLDEN":
        return {"passed": False, "refused": "NOT_A_GOLDEN",
                "ranAnything": False, "fixedAnything": False, "golden": where,
                "why": "%s is not readable as JSON. A baseline nothing can "
                       "read is a comparison nothing can make - and it must "
                       "not come back looking like agreement." % where}

    if problem == "NO_GOLDEN" and not update:
        return {"passed": False, "refused": "NO_GOLDEN",
                "ranAnything": False, "fixedAnything": False, "golden": where,
                "why": "there is no baseline at %s to compare against. Record "
                       "the matrix as it stands with --update, having first "
                       "satisfied yourself that what it is now is what it "
                       "should be." % FRAG.repo_relative(where)}

    verdict = (compare(recorded, now) if recorded is not None
               else {"same": [], "changed": [], "added": sorted(now["releases"]),
                     "removed": []})

    result = {
        "passed": not verdict["changed"] and not verdict["added"]
                  and not verdict["removed"],
        "of": len(now["releases"]),
        "matrix": now,
        "golden": where,
        "wrote": False,
        "ranAnything": False,
        "fixedAnything": False,
        "unjudged": [
            "agreeing with the baseline is not a build. Whether any release "
            "still compiles is HERON-DEV-BLD-010's answer and needs a .NET "
            "SDK, which nothing here has asked for.",
            "and it is not a load. Whether the add-in starts inside that "
            "Revit needs that Revit (D-30).",
            "a baseline updated in the same change as the retarget RECORDS "
            "the move rather than catching it. That is what --update is for, "
            "and why it is not automatic.",
        ],
    }
    result.update(verdict)

    if update:
        try:
            io.open(where, "w", encoding="utf-8").write(
                json.dumps(now, indent=2, sort_keys=True) + "\n")
        except (IOError, OSError) as problem:
            return dict(result, passed=False, refused="CANNOT_WRITE",
                        why="the baseline at %s could not be written: %s"
                            % (where, problem))
        result["wrote"] = True
        result["fixedAnything"] = True
        result["passed"] = True
        result["why"] = (
            "the baseline now records the matrix as it stands: %d release(s), "
            "%d field(s) moved. Recording a change is not the same as it "
            "having been checked - whoever reads the diff is the check."
            % (len(now["releases"]), len(verdict["changed"])))
        return result

    if not result["passed"]:
        result["refused"] = "DRIFTED"
        parts = []
        if verdict["changed"]:
            parts.append("%d field(s) moved" % len(verdict["changed"]))
        if verdict["added"]:
            parts.append("%s newly targeted" % ", ".join(verdict["added"]))
        if verdict["removed"]:
            parts.append("%s no longer targeted" % ", ".join(verdict["removed"]))
        result["why"] = (
            "the build matrix no longer matches %s: %s. If that was "
            "deliberate, --update records it; if it was not, a release has "
            "moved under the add-in."
            % (FRAG.repo_relative(where), "; ".join(parts)))
        return result

    result["why"] = ("%d release(s) match the recorded matrix in every field."
                     % len(verdict["same"]))
    return result


def main(argv):
    update = "--update" in argv
    out = regression(update=update)

    def w(text):
        sys.stdout.write(text.encode("ascii", "replace").decode("ascii"))

    if out.get("refused") in ("NO_MATRIX", "NO_GOLDEN", "NOT_A_GOLDEN",
                              "CANNOT_WRITE"):
        w("REFUSED  %s\n  %s\n" % (out["refused"], out["why"]))
        return 2

    w("\nTHE BUILD MATRIX   %d release(s) against %s\n"
      % (out["of"], FRAG.repo_relative(out["golden"])))
    w("%s\n" % ("=" * 62))
    for release in sorted(out["matrix"]["releases"]):
        one = out["matrix"]["releases"][release]
        w("  %-6s %-16s %s\n" % (release, one["tfm"],
                                 " ".join(one["symbols"])))

    for one in out["changed"]:
        w("\n  MOVED    %s %s\n           was  %s\n           now  %s\n"
          % (one["release"], one["field"], one["was"], one["now"]))
    for release in out["added"]:
        w("\n  ADDED    %s is targeted and the baseline has never heard of it\n"
          % release)
    for release in out["removed"]:
        w("\n  REMOVED  %s is in the baseline and is no longer targeted\n"
          % release)

    w("\n%s\n" % out["why"])
    for line in out["unjudged"]:
        w("  - %s\n" % line)
    return 0 if out["passed"] else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv))
