# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
What licence is on the knowledge Heron ships, before Heron's users redistribute it.

    python tools/check-licence.py            # every fragment and skill
    python tools/check-licence.py --all      # including the ones that are fine

Q-53, answered as D-66. Exits 1 on a finding, 0 when there is nothing to say.

WHY THIS EXISTS, AND IT IS NOT HYPOTHETICAL
--------------------------------------------
K-Dense-AI/scientific-agent-skills, read at file level on 2026-09-09
(docs/33 s5.9): its README says the project is MIT and that you may "modify,
distribute, and use freely". FOUR of its 163 skills carry
"(c) 2025 Anthropic, PBC. All rights reserved." A fifth is MIT under a
different copyright holder. Nothing on the landing page says so. Their own
skill scanner checks security and NEVER LOOKS AT A LICENCE.

Heron is walking into the same position: docs/17 publishes Heron under Apache
2.0 (D-08), docs/09 plans COMMUNITY PACKAGES, and Golden Rule 19 already names
imported text as a source Heron reads. So Heron will ship other people's words
to other people, and until this file existed no Heron tool mentioned a licence.

THE ONE RULE THIS ENCODES
--------------------------
READ THE FILES, NOT THE LANDING PAGE. The failure above is not that somebody
lied - it is that the top-level claim and the file-level truth were different,
and only listing the files showed it. So this walks every file of every
fragment and skill and looks for the markers IN THE CONTENT, then compares what
it found against what the folder DECLARED. A declaration that agrees with
nothing is worth as much as no declaration.

WHAT IT CANNOT DO, SAID PLAINLY
--------------------------------
It is not legal advice and it does not read licence text for meaning. It finds
three things: a reservation of rights, a licence name, and a copyright holder
who is not the declared one. A file with no marker at all is reported as
UNMARKED rather than as clean - "no evidence of a problem" and "evidence of no
problem" are different findings and a tool that merges them is the tool
scientific-agent-skills already has.
"""

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# Heron ships under Apache 2.0 (D-08). These are the licences whose terms let
# Heron's users redistribute the file with Heron. The list is short and adding
# to it is a decision, not an edit.
COMPATIBLE = ("apache-2.0", "apache 2.0", "apache license",
              "mit", "bsd", "isc", "cc0", "unlicense", "public domain")

# A reservation of rights is the finding that matters most: it is the one that
# says redistribution is NOT granted, and it is what four files in the live
# example carried while their README said the opposite.
RESERVED = re.compile(r"all\s+rights\s+reserved", re.I)
# `(c)` and `©` count ONLY next to a year. The first version of this line did
# not, and it reported report-routing-preferences for the C# on line 128:
#     var size = rule.GetCriterion(c) as PrimarySizeCriterion;
# A licence tool that cries wolf is a tool somebody turns off, and it would
# have been turned off over a cast.
COPYRIGHT = re.compile(
    r"(?:copyright\s+(?:\(c\)|©)?\s*(?:\d{4}(?:\s*-\s*\d{4})?)?"
    r"|(?:\(c\)|©)\s*\d{4}(?:\s*-\s*\d{4})?)"
    r"[,\s]*([^\n\r.;]{2,60})", re.I)
LICENCE_NAME = re.compile(
    r"\b(apache[- ]?2\.0|apache license|mit license|mit\b|bsd[- ]?\d?"
    r"|isc\b|gpl[- ]?\d?|agpl[- ]?\d?|lgpl[- ]?\d?|mpl[- ]?\d?"
    r"|cc0|unlicense|proprietary)\b", re.I)

# Copyright holders that are Heron's own. A marker naming one of these is not
# somebody else's work arriving.
OURS = ("heron", "ajmal")

TEXT = (".yaml", ".yml", ".md", ".cs", ".py", ".json", ".txt", ".ps1")


def read(path):
    try:
        return io.open(path, encoding="utf-8", errors="replace").read()
    except (IOError, OSError):
        return ""


def declared_source(folder):
    """What the unit SAYS it is. OFFICIAL means Heron wrote it."""
    if os.path.isfile(folder):
        found = re.search(r"^source:\s*(\S+)", read(folder), re.M)
        return found.group(1).strip().strip('"\'') if found else None
    for name in ("fragment.yaml", "skill.yaml", "SKILL.md"):
        path = os.path.join(folder, name)
        if os.path.isfile(path):
            found = re.search(r"^source:\s*(\S+)", read(path), re.M)
            if found:
                return found.group(1).strip().strip('"\'')
    return None


def files_of(path):
    """One file, or every text file under one folder."""
    if os.path.isfile(path):
        return [path]
    out = []
    for base, _dirs, files in os.walk(path):
        for name in sorted(files):
            if name.lower().endswith(TEXT):
                out.append(os.path.join(base, name))
    return out


def inspect(folder):
    """Every licence marker in every file of one fragment or skill."""
    reserved, licences, holders = [], set(), set()
    for path in files_of(folder):
        body = read(path)
        if RESERVED.search(body):
            reserved.append(os.path.relpath(path, ROOT))
        for match in LICENCE_NAME.findall(body):
            licences.add(match.lower().strip())
        for match in COPYRIGHT.findall(body):
            who = match.strip(" ,-\t").strip()
            # A holder is a name, and a name has letters. "Copyright 2026"
            # with nothing after it is a marker with no holder in it.
            if who and re.search(r"[A-Za-z]{3}", who):
                holders.add(who)
    return reserved, licences, holders


def foreign(holders):
    return sorted(h for h in holders
                  if not any(mine in h.lower() for mine in OURS))


def units():
    """
    Every unit of KNOWLEDGE Heron ships, as (kind, name, path).

    brain/fragments and brain/skills only. `.claude/skills` is deliberately
    out: those are the skills for DEVELOPING Heron, they run on a maintainer's
    machine, and they are covered by the repository's own LICENSE like any
    other source file. Q-53 is about what Heron's USERS redistribute, and
    scanning the toolbox alongside the cargo would bury the finding that
    matters in six that do not.

    Fragments are folders and skills are single YAML files. Both are walked the
    same way because inspect() takes a path either way.
    """
    found = []
    base = os.path.join(ROOT, "brain", "fragments")
    if os.path.isdir(base):
        for name in sorted(os.listdir(base)):
            folder = os.path.join(base, name)
            if os.path.isdir(folder):
                found.append(("fragment", name, folder))

    base = os.path.join(ROOT, "brain", "skills")
    if os.path.isdir(base):
        for name in sorted(os.listdir(base)):
            if name.endswith((".yaml", ".yml")):
                found.append(("skill", name[:-5] if name.endswith(".yaml")
                              else name[:-4], os.path.join(base, name)))
    return found


def main(argv):
    show_all = "--all" in argv
    out = sys.stdout.write

    out("LICENCE CHECK - what Heron ships, and whether it may\n")
    out("=" * 70 + "\n")
    out("Heron is Apache 2.0 (D-08), so anything shipped with it must be\n")
    out("redistributable on those terms. This reads the FILES, not the\n")
    out("landing page - which is the whole finding behind Q-53.\n\n")

    findings, unmarked, fine = [], [], 0
    everything = units()

    for kind, name, folder in everything:
        source = declared_source(folder)
        reserved, licences, holders = inspect(folder)
        others = foreign(holders)
        incompatible = sorted(l for l in licences
                              if not any(ok in l for ok in COMPATIBLE))

        if reserved:
            findings.append((kind, name,
                             "RESERVES RIGHTS in %s" % ", ".join(reserved[:3])))
        elif incompatible:
            findings.append((kind, name,
                             "carries %s, which is not on the compatible list"
                             % ", ".join(incompatible)))
        elif others and source == "OFFICIAL":
            findings.append((kind, name,
                             "declares source: OFFICIAL and names another "
                             "copyright holder: %s" % ", ".join(others[:3])))
        elif source is None:
            # A SKILL COUNTS TOO. This read `and kind == "fragment"` until
            # 2026-09-09, left over from when .claude/skills was still scanned
            # and none of those declared a source. Once that folder came out of
            # units(), the restriction stopped protecting anything and started
            # hiding something: all ten brain/skills/*.yaml declare no source,
            # so an IMPORTED skill with no licence would have been counted
            # clean by a checker whose whole promise is that unmarked and clean
            # are different findings. Found by Codex on PR #44.
            unmarked.append((kind, name, "declares no source at all"))
        elif source and source != "OFFICIAL" and not licences:
            unmarked.append((kind, name,
                             "declares source: %s and no licence anywhere in "
                             "its files" % source))
        else:
            fine += 1

    if findings:
        out("FINDINGS (%d) - these must be resolved before publication\n"
            % len(findings))
        out("-" * 70 + "\n")
        for kind, name, why in findings:
            out("  %-9s %-34s %s\n" % (kind, name, why))
        out("\n")

    if unmarked:
        out("UNMARKED (%d) - no evidence of a problem is not evidence of\n"
            % len(unmarked))
        out("none. These need a licence before they may be redistributed.\n")
        out("Anything with no foreign holder and nothing reserved is covered\n")
        out("by the repository's own LICENSE; these are the ones that are\n")
        out("not covered by it and say nothing else either.\n")
        out("-" * 70 + "\n")
        for kind, name, why in unmarked[:40]:
            out("  %-9s %-34s %s\n" % (kind, name, why))
        if len(unmarked) > 40:
            out("  ..and %d more (--all)\n" % (len(unmarked) - 40))
        out("\n")

    out("%d unit(s) checked: %d clean, %d finding(s), %d unmarked.\n"
        % (len(everything), fine, len(findings), len(unmarked)))

    if show_all and fine:
        out("\nCLEAN - declared OFFICIAL, no foreign holder, nothing reserved\n")
        out("-" * 70 + "\n")
        for kind, name, folder in everything:
            source = declared_source(folder)
            reserved, licences, holders = inspect(folder)
            if source == "OFFICIAL" and not reserved and not foreign(holders):
                out("  %-9s %s\n" % (kind, name))

    out("\n")
    out("It is not legal advice and it does not read licence text for\n")
    out("meaning. It finds a reservation of rights, a licence name, and a\n")
    out("copyright holder who is not the declared one - which is exactly\n")
    out("what nobody was looking for in the live example.\n")

    return 1 if findings else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
