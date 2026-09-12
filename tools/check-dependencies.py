# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Which Python packages Heron has, which it has not, and what each one buys.

    python tools/check-dependencies.py

Exits 1 only when a REQUIRED package is missing. A missing optional package
is the normal case and exits 0 - that is the whole point of it being
optional, and a checker that failed on one would be arguing with R-42.

WHY THIS EXISTS
---------------
W-5 and W-6, both found on 2026-09-10 by the owner asking one question:
does a new person installing from GitHub get this automatically?

The answer was no, twice over. `tools/setup.ps1` builds and deploys the
add-in for every Revit on the machine and installs no Python package at
all; and the only list of Python packages anywhere was one row of a table
in brain/README.md that said `pyyaml` when the code imports six things.
Somebody following the instructions exactly installed pyyaml, got the
weaker retrieval backend, and was told nothing.

Silent degradation is correct - R-42 requires it. Silent degradation plus
an install list nobody can follow is how a person ends up on the weaker
backend permanently, believing they are on the better one.

So the list moved into requirements.txt and requirements-optional.txt,
where pip can read it, and this tool reads the SAME two files rather than
carrying a second copy. R-71 says it in one line: the list cannot go stale
if nothing types it twice.

WHAT IT DELIBERATELY DOES NOT DO
--------------------------------
It does not install anything. R-74 and R-77 are the automatic half and
they are not built - this is the reporting half, and saying so is better
than a tool that half-installs.

It does not check that an installed version is the RIGHT one. That is
R-78, also not built. `installed` here means importable, nothing more.

It does not print a size it made up. Where a size is known it comes from
the code that owns the download - heron_rerank.announcement() prints it
before any network call - and where nothing owns it, this tool measures
the package on THIS disk or says it cannot.
"""

from __future__ import annotations

import importlib.util
import os
import pathlib
import sys

HERE = pathlib.Path(__file__).resolve().parent
ROOT = HERE.parent

REQUIRED = ROOT / "requirements.txt"
OPTIONAL = ROOT / "requirements-optional.txt"

# The comment shape both manifests use, documented in their own headers:
#     # <import name> | <what it is for> | <what happens without it>
FIELDS = 3


class Package:
    def __init__(self, pip_name, module, purpose, without):
        self.pip_name = pip_name
        self.module = module
        self.purpose = purpose
        self.without = without

    @property
    def installed(self):
        """Importable, and nothing stronger. R-78 is the version question."""
        try:
            return importlib.util.find_spec(self.module) is not None
        except (ImportError, ValueError):
            # A half-removed package can leave a spec that raises on lookup.
            # That is not installed, whatever the directory listing says.
            return False


def read_manifest(path):
    """Parse one manifest into Packages, pairing each requirement with the
    comment directly above it.

    A requirement with no comment above it is a defect in the manifest, not
    something to paper over: the whole reason this file is the single source
    is that it carries the purpose too. It is reported, not skipped.
    """
    packages, problems = [], []
    if not path.exists():
        problems.append("%s does not exist" % path.name)
        return packages, problems

    previous = None
    for number, raw in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
        line = raw.strip()
        if not line:
            previous = None
            continue
        if line.startswith("#"):
            previous = (number, line.lstrip("#").strip())
            continue

        pip_name = line.split("#")[0].strip()
        if previous is None:
            problems.append("%s line %d: `%s` has no comment above it, so "
                            "nothing says what it is for" % (path.name, number, pip_name))
            previous = None
            continue

        parts = [p.strip() for p in previous[1].split("|")]
        if len(parts) != FIELDS:
            problems.append("%s line %d: the comment above `%s` has %d field(s), "
                            "expected %d - `<import name> | <for> | <without>`"
                            % (path.name, previous[0], pip_name, len(parts), FIELDS))
            previous = None
            continue

        packages.append(Package(pip_name, parts[0], parts[1], parts[2]))
        previous = None

    return packages, problems


def on_disk(module):
    """Bytes this module occupies on THIS machine, or None if absent.

    ITS OWN FILES ONLY. A package that pulls in another - sentence-transformers
    pulls torch, which is most of its 500 MB to 2 GB - measures small here and
    is not small to install. The caller says so rather than letting the number
    be read as an install size.
    """
    try:
        spec = importlib.util.find_spec(module)
    except (ImportError, ValueError):
        return None
    if spec is None:
        return None

    roots = []
    if spec.submodule_search_locations:
        roots = [pathlib.Path(p) for p in spec.submodule_search_locations]
    elif spec.origin and os.path.sep in str(spec.origin):
        roots = [pathlib.Path(spec.origin)]

    total = 0
    for root in roots:
        try:
            if root.is_dir():
                total += sum(f.stat().st_size for f in root.rglob("*") if f.is_file())
            elif root.is_file():
                total += root.stat().st_size
        except OSError:
            # Unreadable is not zero. Better to say nothing than to print a
            # size that is wrong in the direction of "this is free".
            return None
    return total or None


def megabytes(size):
    return "%.1f MB" % (size / 1e6)


def announced_size(module):
    """The size the code that owns the download states, if any owns it.

    Only heron_rerank does today, and it prints it before the network is
    touched so a person can stop it. Read rather than copied: a number
    copied here goes stale there.
    """
    if module != "sentence_transformers":
        return None
    brain = str(ROOT / "brain")
    if brain not in sys.path:
        sys.path.insert(0, brain)
    try:
        import heron_rerank
    except Exception:
        return None
    try:
        for line in heron_rerank.announcement().splitlines():
            stripped = line.strip()
            if stripped.startswith("size"):
                return stripped.split(None, 1)[1].strip()
    except Exception:
        return None
    return None


def report(title, packages, indent="  "):
    for package in packages:
        mark = "present" if package.installed else "MISSING"
        print("%s%-22s %s" % (indent, package.pip_name, mark))
        print("%s%-22s for      %s" % (indent, "", package.purpose))
        if package.installed:
            size = on_disk(package.module)
            if size is not None:
                print("%s%-22s on disk  %s (its own files - not what it pulled in)"
                      % (indent, "", megabytes(size)))
        else:
            print("%s%-22s without  %s" % (indent, "", package.without))
            stated = announced_size(package.module)
            if stated:
                print("%s%-22s size     %s - said by the code that downloads it"
                      % (indent, "", stated))
            print("%s%-22s install  pip install --user %s"
                  % (indent, "", package.pip_name))
        print()


def main():
    required, problems = read_manifest(REQUIRED)
    optional, more = read_manifest(OPTIONAL)
    problems += more

    print("REQUIRED - Heron does not run without these")
    print()
    report("required", required)

    print("OPTIONAL - every one of these degrades rather than breaks")
    print()
    report("optional", optional)

    absent_required = [p for p in required if not p.installed]
    absent_optional = [p for p in optional if not p.installed]

    if problems:
        print("THE MANIFEST ITSELF HAS A PROBLEM")
        for problem in problems:
            print("  %s" % problem)
        print()

    if absent_required:
        print("A REQUIRED PACKAGE IS MISSING. Heron will not run:")
        for package in absent_required:
            print("  pip install --user %s" % package.pip_name)
        return 1

    if absent_optional:
        print("%d of %d optional package(s) absent. That is not a fault - Heron"
              % (len(absent_optional), len(optional)))
        print("answers without them and says which one did not run.")
    else:
        print("Every package in both manifests is installed.")

    if problems:
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
