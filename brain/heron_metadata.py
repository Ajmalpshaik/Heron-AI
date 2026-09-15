# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-IMP-MEX-006
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Metadata extraction - three fields are in the file, and the author is not.

    python brain/heron_metadata.py

WHAT IT IS FOR (docs/28, HERON-IMP-MEX-006)
--------------------------------------------
"Author, version, dependencies, target Revit versions." T2, risk READ.
Step 7 of docs/00 s28's sixteen.

THE FOUR FIELDS ARE NOT FOUR OF THE SAME THING
------------------------------------------------
Three of them are written in the file when they are written at all, and
finding them is reading:

    dependencies     `using X;`, `import X`, `PackageReference`, `#r`
    target releases  `#if REVIT2024`, `REVIT2025_OR_GREATER`
    version          AssemblyVersion, __version__, <Version>, Heron-Since

The fourth is not. An author is a PERSON, and almost no working file
says who wrote it. Every mechanical way to produce one is a way to
attribute somebody's work to somebody else:

    the folder name          `AJ-Tools` is where it was kept, not who
                             wrote it. Half of any such folder is
                             collected from forums and colleagues.
    the repository owner     whoever cloned it last
    git blame                who last touched a line, which on imported
                             code is whoever ran the formatter

So `author` is never inferred. It is asked for, once, per file that
does not state one - D-33 - and this is the field where guessing is not
a small error but a false claim about a person.

WHY THAT MATTERS MORE HERE THAN ANYWHERE ELSE IN THE PIPELINE
---------------------------------------------------------------
docs/10 s5 constraint 4 is provenance: source path, original filename,
import date, importing version. Those are facts about the IMPORT and
this agent records them as such. Authorship is a fact about the WORK,
and D-66 - a shipped unit says where it came from - is the reason a
wrong one travels: it gets written into the fragment, shipped, and then
cited by everything downstream.

WHAT IS A DEPENDENCY AND WHAT IS THE FRAMEWORK IS NOT DECIDED HERE
--------------------------------------------------------------------
`using System;` and `using Newtonsoft.Json;` look identical to a
reader. Splitting them needs a list of what ships with the runtime,
which would be a table nobody measured and which changes per release.

Two facts are reported instead: every import AS WRITTEN, and every
`PackageReference` the project DECLARES. An import with no matching
declaration is either the framework or an undeclared dependency, and
which is left to the reader - named in `unjudged` rather than silently
resolved.

WHETHER A DEPENDENCY IS ALLOWED IS SOMEBODY ELSE'S ROW
--------------------------------------------------------
HERON-INS-SUP-013 refuses an unapproved package and HERON-INS-DEP-005
confirms one. This agent finds them and judges none of it.
"""

from __future__ import annotations

import io
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_fragment as FRAG  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# D-05's list, borrowed. A release named in a file that is not on it is
# reported as named-and-unsupported, never quietly accepted.
RELEASES = FRAG.REVIT_VERSIONS

# The one field that is never derived, with the reason in the question so
# whoever answers knows why they are being asked.
ASKED_FOR = {
    "author": "who wrote this? It is not in the file, and every "
              "mechanical answer is wrong in a way that travels - a "
              "folder name is where it was kept, a repository owner is "
              "whoever cloned it, and git blame on imported code is "
              "whoever ran the formatter",
}

# How much of a file is read. A header is at the top, and an import list
# that has not started in 200 lines is not going to.
HEAD = 200

_MARKERS = {
    "dependencies": (
        re.compile(r"^\s*using\s+([A-Za-z_][\w.]*)\s*;", re.M),
        re.compile(r"^\s*import\s+([A-Za-z_][\w.]*)", re.M),
        re.compile(r"^\s*from\s+([A-Za-z_][\w.]*)\s+import\b", re.M),
        re.compile(r'#r\s+"([^"]+)"'),
    ),
    "packages": (
        re.compile(r'PackageReference\s+Include="([^"]+)"'),
    ),
    "version": (
        re.compile(r'AssemblyVersion\s*\(\s*"([0-9][^"]*)"'),
        re.compile(r'__version__\s*=\s*["\']([^"\']+)'),
        re.compile(r"<Version>([^<]+)</Version>"),
        re.compile(r"Heron-Since:\s*(\S+)"),
    ),
    "author": (
        re.compile(r'AssemblyCompany\s*\(\s*"([^"]+)"'),
        re.compile(r"<Authors>([^<]+)</Authors>"),
        re.compile(r"^\s*(?://|#|\*)?\s*@?[Aa]uthor\s*[:=]\s*(\S.*?)\s*$",
                   re.M),
        re.compile(r"Copyright\s*\(c\)\s*(?:\d{4}\s*)?([A-Za-z][^\n,.]*)"),
    ),
}

# A release marker, in the two shapes the C# actually uses.
_RELEASE = re.compile(r"\bREVIT(20\d\d)(?:_OR_GREATER)?\b")


def _hits(text, which):
    """Every distinct match, in the order first seen."""
    out = []
    for pattern in _MARKERS[which]:
        for found in pattern.findall(text):
            found = str(found).strip()
            if found and found not in out:
                out.append(found)
    return out


def _releases(text):
    """(named, unsupported) - D-05: an unlisted release is never a guess."""
    named, outside = [], []
    for year in _RELEASE.findall(text):
        target = named if year in RELEASES else outside
        if year not in target:
            target.append(year)
    return sorted(named), sorted(outside)


def read(items, root=None):
    """
    {read, found, asks, unreadable} - or a refusal. Nothing is written
    and no author is inferred.
    """
    items = list(items or [])
    if not items:
        return {"read": False, "refused": "NOTHING_TO_READ",
                "why": "no items were handed in. HERON-IMP-CLS-003's "
                       "classified groups carry the paths."}

    found, asks, unreadable = [], [], []
    for item in items:
        item = getattr(item, "data", item)
        if isinstance(item, str):
            item = {"at": item}
        if not isinstance(item, dict) or not str(item.get("at") or "").strip():
            return {"read": False, "refused": "NOT_AN_ITEM",
                    "why": "%r is not an item. Each carries `at` - where the "
                           "file is, relative to the folder being imported."
                           % (item,)}

        at = str(item["at"]).strip()
        path = os.path.join(root, at) if root else at
        try:
            with io.open(path, encoding="utf-8", errors="replace") as handle:
                text = "".join(handle.readlines()[:HEAD])
        except (IOError, OSError) as error:
            unreadable.append({"at": at,
                               "why": "%s. It was named and could not be "
                                      "opened." % (error.strerror
                                                   or "unreadable")})
            continue

        named, outside = _releases(text)
        imports = _hits(text, "dependencies")
        declared = _hits(text, "packages")
        versions = _hits(text, "version")
        authors = _hits(text, "author")

        card = {
            "at": at,
            "imports": imports,
            "packages": declared,
            "revit": named,
            "revitOutsideSupport": outside,
            "version": versions[0] if versions else None,
            "versionsSeen": versions,
            "author": authors[0] if authors else None,
            # docs/10 s5 constraint 4. Facts about the IMPORT, not about
            # the work - which is the whole difference from `author`.
            "provenance": {"from": at,
                           "name": os.path.basename(at)},
        }
        found.append(card)

        if not card["author"]:
            asks.append({"at": at, "field": "author",
                         "question": ASKED_FOR["author"]})

    # A PROJECT DECLARES FOR ITS SOURCES, so `undeclared` is computed over
    # the whole set rather than per file. Matching a .cs file's `using`
    # lines against its own PackageReference list would report every import
    # as undeclared, because the declarations are in the .csproj next to it.
    everything = set()
    for card in found:
        everything.update(card["packages"])
    for card in found:
        card["undeclared"] = [name for name in card["imports"]
                              if not any(name.startswith(p) or
                                         p.startswith(name)
                                         for p in everything)]

    return {
        "read": True,
        "of": len(items),
        "declared": sorted(everything),
        "found": found,
        "asks": asks,
        "unreadable": unreadable,
        "why": "%d item%s: %d read, %d need an author, %d could not be "
               "opened. %d named a Revit release%s."
               % (len(items), "" if len(items) == 1 else "s", len(found),
                  len(asks), len(unreadable),
                  len([c for c in found if c["revit"]]),
                  "" if len([c for c in found if c["revit"]]) == 1 else "s"),
        "unjudged": [
            ("NO AUTHOR WAS INFERRED, AND %d %s ASKED FOR. A folder name is "
             "where code was kept, a repository owner is whoever cloned it, "
             "and git blame on imported code is whoever ran the formatter. "
             "This is the one field where a guess is a false claim about a "
             "person (D-66 - a shipped unit says where it came from, so a "
             "wrong one travels)."
             % (len(asks), "IS" if len(asks) == 1 else "ARE")
             if asks else
             "every file stated an author, so none had to be asked for. "
             "None was inferred either."),
            "WHAT IS A DEPENDENCY AND WHAT IS THE FRAMEWORK. `using System` "
            "and `using Newtonsoft.Json` read identically. Every import is "
            "reported as written and every declared PackageReference beside "
            "it; an import with no declaration is either the framework or "
            "an undeclared dependency, and splitting them needs a list of "
            "what ships with each runtime that nobody has measured.",
            "WHETHER ANY DEPENDENCY IS ALLOWED. HERON-INS-SUP-013 refuses "
            "an unapproved package and HERON-INS-DEP-005 confirms one. This "
            "agent finds them.",
            ("%d FILE%s NAMED A RELEASE HERON DOES NOT SUPPORT: %s. Reported "
             "rather than dropped or extrapolated from - D-05 says an "
             "unlisted release is an error, never a guess."
             % (len([c for c in found if c["revitOutsideSupport"]]),
                "" if len([c for c in found
                           if c["revitOutsideSupport"]]) == 1 else "S",
                ", ".join(sorted(set(year for c in found
                                     for year in c["revitOutsideSupport"]))))
             if any(c["revitOutsideSupport"] for c in found) else
             "every release named was one of the %d Heron supports."
             % len(RELEASES)),
            "A FILE THAT NAMES NO RELEASE IS NOT A FILE THAT RUNS ON ALL OF "
            "THEM. It is a file that says nothing, and HERON-IMP-CMP-008 "
            "returns an empty list for exactly that case rather than eight "
            "releases.",
            "ONLY THE FIRST %d LINES WERE READ. A header is at the top and "
            "an import list that has not started by then is not going to - "
            "but a version buried at the bottom of a long file is missed, "
            "and that is a limit rather than an absence." % HEAD,
        ],
    }


def main(argv):
    import shutil
    import tempfile

    print("METADATA EXTRACTION   three fields are in the file, the author "
          "is not")
    print("=" * 72)
    print("\nnever derived: %s" % ", ".join(sorted(ASKED_FOR)))
    print("releases Heron knows: %s" % ", ".join(RELEASES))

    where = tempfile.mkdtemp(prefix="heron-metadata-")
    try:
        files = {
            "CountDucts.cs": (
                '// @author: A. Shaik\n'
                # NOT THE REAL REVIT API NAMESPACE. check-structure.py
                # refuses that one anywhere outside revit/ and is right
                # to; the fixture only needs an import nothing declares.
                'using System;\nusing Nice3point.Revit.Extensions;\n'
                'using Newtonsoft.Json;\n'
                '[assembly: AssemblyVersion("2.1.0")]\n'
                '#if REVIT2024_OR_GREATER\n// newer path\n#endif\n'
                '#if REVIT2020\n// older path\n#endif\n'),
            "tagsheet.py": (
                '"""Tag a sheet."""\n'
                '__version__ = "0.4"\n'
                'import os\nfrom collections import OrderedDict\n'),
            "Old.cs": ('#if REVIT2018\n// long gone\n#endif\n'
                       'using System;\n'),
            "Tools.csproj": ('<Project>\n<Version>3.0.1</Version>\n'
                             '<Authors>Acme Engineering</Authors>\n'
                             '<PackageReference Include="Newtonsoft.Json" '
                             'Version="13.0.1" />\n</Project>\n'),
        }
        for at, text in files.items():
            with io.open(os.path.join(where, at), "w",
                         encoding="utf-8") as handle:
                handle.write(text)

        answer = read(sorted(files) + ["gone.cs"], root=where)
        print("\n%s" % answer["why"])
        for card in answer["found"]:
            print("\n  %s" % card["at"])
            print("    author        %s" % (card["author"] or "NOT STATED"))
            print("    version       %s" % (card["version"] or "-"))
            print("    revit         %s"
                  % (", ".join(card["revit"]) or "none named"))
            if card["revitOutsideSupport"]:
                print("    unsupported   %s"
                      % ", ".join(card["revitOutsideSupport"]))
            print("    imports       %s"
                  % (", ".join(card["imports"]) or "-"))
            if card["packages"]:
                print("    declares      %s" % ", ".join(card["packages"]))
            if card["undeclared"]:
                print("    undeclared    %s" % ", ".join(card["undeclared"]))
        for card in answer["unreadable"]:
            print("\n  %-16s %s" % (card["at"], card["why"][:40]))

        print("\nasked for, never derived")
        for ask in answer["asks"]:
            print("  %-16s %s" % (ask["at"], ask["question"][:50]))

        print("\nrefused")
        for these in ([], [123], [{"kind": "fragment"}]):
            bad = read(these, root=where)
            print("  %-16s %s" % (bad["refused"], bad["why"][:46]))

        print("\nwhat this agent does not judge")
        for line in answer["unjudged"]:
            print("  - %s" % line)
    finally:
        shutil.rmtree(where, ignore_errors=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
