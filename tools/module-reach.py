#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Who imports each module in brain/, and who does not.

    python tools/module-reach.py            the four buckets, and the counts
    python tools/module-reach.py --list     and every module in each

WHY THIS EXISTS
---------------
`mcp/README.md` describes the state of this repository before
`mcp/server/heron_brain.py` was built, in its own words: the transport-only
rule "was being kept by HAVING NO ROUTE AT ALL: eight brain modules, seven
fragments and ten skills, imported by nothing but their own tests, and
therefore UNREACHABLE FROM ANY CONVERSATION."

One named seam was built so the rule could hold without that. This counts
the same thing again, so the number that seam was built to fix can be read
rather than assumed. On 2026-09-21 it was ninety - row 5b-83 in
docs/FRAGMENT-ISSUES.md, which is OPEN and says plainly that the
MEASUREMENT is the finding and the judgement is somebody's to make.

THIS TOOL DECIDES NOTHING, AND THAT IS NOT MODESTY.
----------------------------------------------------
A module imported only by its own test is not automatically wrong. `brain/`
may be a library that the fragment and capability layers draw on, with the
`Heron-Agent:` header naming who OWNS a module rather than who CALLS it. So
this prints buckets and exits 0 whatever it finds - a report, in this
repository's sense: a finding here is a question for a person.

ONE HOP IS NOT REACH, SO BOTH NUMBERS ARE PRINTED
-------------------------------------------------
The four buckets count ONE hop: a module imported by another brain module
lands in the first bucket whether or not that neighbour is itself reached by
anything. That is honest as far as it goes, and it is not the sentence
mcp/README.md makes. Measured 2026-09-22 on this repository: the first
bucket held 53, and only 17 of the 145 modules were reachable from `mcp/` by
FOLLOWING imports. Thirty-six were reached only by neighbours that are
themselves unreachable from any conversation - closed loops inside `brain/`.

So the walk is printed beside the buckets rather than instead of them.
Neither number stands in for the other: the buckets say who imports a
module, the walk says whether a conversation can get to it.

WHAT IT CANNOT SEE
------------------
A module reached by anything other than an `import` or a `from ... import`.
Measured 2026-09-21 and there is no such route: the only three `importlib`
uses outside tests each load one named file, and nothing resolves an
arbitrary module by name. If that ever changes, this tool starts
undercounting and the line below is where to fix it.
"""

from __future__ import annotations

import argparse
import ast
import collections
import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BRAIN_DIR = os.path.join(ROOT, "brain")

# Where an importer can live. `.claude` is in here because a hook is a real
# caller - leaving it out would report a module as unreached when a skill's
# hook imports it every time somebody edits a file.
SEARCHED = ("brain", "mcp", "tools", "tests", ".claude")
SKIP_DIRS = {"__pycache__", "bin", "obj", ".vs", ".git", "node_modules"}


def brain_modules():
    """Every importable module in brain/, by the name an import would use."""
    out = {}
    if not os.path.isdir(BRAIN_DIR):
        return out
    for fn in sorted(os.listdir(BRAIN_DIR)):
        if fn.endswith(".py") and not fn.startswith("_"):
            out[fn[:-3]] = "brain/" + fn
    return out


def importers_of(names):
    """module name -> the set of files that import it, itself excluded."""
    found = collections.defaultdict(set)
    for top in SEARCHED:
        base = os.path.join(ROOT, top)
        if not os.path.isdir(base):
            continue
        for dirpath, dirnames, files in os.walk(base):
            dirnames[:] = [d for d in dirnames if d not in SKIP_DIRS]
            for fn in files:
                if not fn.endswith(".py"):
                    continue
                path = os.path.join(dirpath, fn)
                try:
                    with io.open(path, encoding="utf-8") as handle:
                        tree = ast.parse(handle.read())
                except (IOError, SyntaxError, ValueError):
                    # A file this cannot parse is reported as nothing rather
                    # than skipped silently - see the count printed at the end.
                    found["__unparsed__"].add(os.path.relpath(path, ROOT))
                    continue
                me = os.path.splitext(fn)[0]
                for node in ast.walk(tree):
                    if isinstance(node, ast.Import):
                        heads = [a.name.split(".")[0] for a in node.names]
                    elif isinstance(node, ast.ImportFrom) and node.module:
                        heads = [node.module.split(".")[0]]
                    else:
                        continue
                    for head in heads:
                        if head in names and head != me:
                            found[head].add(os.path.relpath(path, ROOT))
    return found


def where(rel):
    top = rel.replace(os.sep, "/").split("/")[0]
    return top if top in SEARCHED else "other"


def reached_from(top, names, found):
    """Every brain module a file under `top` can get to by following imports.

    ONE HOP IS NOT REACH. `found` says who imports each module; walking it
    says whether anything under `top` can arrive at a module at all, however
    many brain modules it goes through on the way. A module imported only by
    a neighbour that nothing reaches is not reached.
    """
    by_file = collections.defaultdict(set)      # importing file -> modules
    for name, who in found.items():
        for rel in who:
            by_file[rel.replace(os.sep, "/")].add(name)

    seen = set()
    queue = [name for rel, mods in by_file.items() if where(rel) == top
             for name in mods]
    while queue:
        name = queue.pop()
        if name in seen:
            continue
        seen.add(name)
        queue.extend(by_file.get(names[name], ()))
    return seen


def main():
    parser = argparse.ArgumentParser(
        description="Who imports each module in brain/, and who does not.")
    parser.add_argument("--list", action="store_true",
                        help="name every module in each bucket")
    args = parser.parse_args()

    names = brain_modules()
    if not names:
        sys.stderr.write(
            "COULD NOT RUN: no modules found in %s, so this compared "
            "nothing. That is not a clean report.\n" % BRAIN_DIR)
        return 2

    found = importers_of(set(names))
    unparsed = found.pop("__unparsed__", set())

    buckets = collections.OrderedDict((
        ("reached by mcp/ or another brain module", []),
        ("reached only by a tool", []),
        ("imported ONLY by its own test", []),
        ("imported by nothing at all", []),
    ))
    labels = list(buckets)

    for name in sorted(names):
        who = found.get(name, set())
        kinds = {where(p) for p in who}
        if not who:
            buckets[labels[3]].append(name)
        elif kinds == {"tests"}:
            buckets[labels[2]].append(name)
        elif kinds <= {"tests", "tools"}:
            buckets[labels[1]].append(name)
        else:
            buckets[labels[0]].append(name)

    print("\nModules in brain/: %d\n" % len(names))
    for label in labels:
        print("  %-42s %d" % (label, len(buckets[label])))
        if args.list:
            for name in buckets[label]:
                print("        %s" % name)

    # THE SAME QUESTION, ASKED THE WAY mcp/README.md ASKS IT. The buckets
    # above count one hop; this follows the imports. Printed beside them, not
    # instead of them - the buckets say who imports a module, this says
    # whether a conversation can arrive at it.
    reach = reached_from("mcp", names, found)
    print()
    print("  %-42s %d" % ("reachable from mcp/ by following imports",
                          len(reach)))
    if args.list:
        for name in sorted(reach):
            print("        %s" % name)
    gap = len(buckets[labels[0]]) - len(reach)
    if gap > 0:
        print("\n  %d of the %d in the first bucket are reached only by "
              "neighbours\n  that nothing reaches - closed loops inside "
              "brain/." % (gap, len(buckets[labels[0]])))

    if unparsed:
        print("\n  %d file(s) could not be parsed and count as nothing:"
              % len(unparsed))
        for rel in sorted(unparsed):
            print("        %s" % rel)

    print()
    print("A REPORT, NOT A GATE - exit 0 whatever it finds. A module imported")
    print("only by its own test is not automatically wrong: brain/ may be a")
    print("library the fragment and capability layers draw on, and the")
    print("Heron-Agent header may name who OWNS a module rather than who")
    print("calls it. Row 5b-83 in docs/FRAGMENT-ISSUES.md is OPEN on exactly")
    print("that question and says the measurement is the finding.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
