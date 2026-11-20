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

IT CHECKS THE VERSION, NOT ONLY THAT THE PACKAGE IS THERE (R-78)
---------------------------------------------------------------
"Installed" and "correct" are different answers, and a component that loads
an old version and half-works is worse than one that refuses. So every
installed package is held against the one version Heron has a record of
being RIGHT:

  - a floor written on the manifest line itself (`PyYAML>=6.0`), which pip
    reads too, so it is never typed in a second place; and
  - the version requirements-lock.txt pins - the set the one-step installer
    puts on a PC, each one measured before it was pinned.

Older than either is reported as NEEDS AN UPDATE, with the line that does it,
and exits 1. Newer than the lock is said and not failed: nothing measured it,
and calling it wrong would be a guess. A package with no floor and no pin is
said to have been checked for presence only - the honest answer, rather than
a version check that checked nothing. No floor is written on either manifest
today, because none has been measured against an older release; requirements.txt
says so in its own header.

It does not print a size it made up. Where a size is known it comes from
the code that owns the download - heron_rerank.announcement() and
heron_embed.announcement() print it before any network call - and where
nothing owns it, this tool measures the package on THIS disk or says it
cannot.
"""

from __future__ import annotations

import importlib.metadata
import importlib.util
import os
import pathlib
import re
import sys

HERE = pathlib.Path(__file__).resolve().parent
ROOT = HERE.parent

REQUIRED = ROOT / "requirements.txt"
OPTIONAL = ROOT / "requirements-optional.txt"
LOCK = ROOT / "requirements-lock.txt"

# A requirement line's name and what follows it: `PyYAML`, `PyYAML>=6.0`.
_REQUIREMENT = re.compile(r"^([A-Za-z0-9][A-Za-z0-9._-]*)\s*(.*)$")
# One clause of a specifier: `>=6.0`. Only the operators below are read; any
# other is reported as not understood, never guessed at.
_CLAUSE = re.compile(r"^(>=|==|>|<=|<|!=)\s*([0-9][0-9A-Za-z.+-]*)$")
# A pinned line in the lock: `pyyaml==6.0.3 \`.
_PIN = re.compile(r"^([A-Za-z0-9][A-Za-z0-9._-]*)==([0-9][0-9A-Za-z.+-]*)")

OUT_OF_DATE = "NEEDS AN UPDATE"

# The comment shape both manifests use, documented in their own headers:
#     # <import name> | <what it is for> | <what happens without it>
FIELDS = 3


def normalise(name):
    """A distribution name as pip compares it (PEP 503): `PyYAML` is `pyyaml`."""
    return re.sub(r"[-_.]+", "-", name).lower()


def release(version):
    """The numeric release of a version, as a tuple: `6.0.3` -> (6, 0, 3).

    A pre-release or local suffix is cut off rather than interpreted - `0.1.9a1`
    reads as (0, 1, 9). That can only make an old version look as new as its
    release, never make a new one look old, so it cannot invent an update.
    None for something that does not start with a number.
    """
    parts = []
    for piece in str(version or "").split("."):
        digits = re.match(r"\d+", piece)
        if not digits:
            break
        parts.append(int(digits.group(0)))
        if digits.group(0) != piece:
            break
    return tuple(parts) or None


def compare(left, right):
    """-1, 0 or 1 for two release tuples, padding the shorter with zeros."""
    width = max(len(left), len(right))
    left = left + (0,) * (width - len(left))
    right = right + (0,) * (width - len(right))
    return (left > right) - (left < right)


def read_lock(path=None):
    """{normalised name: pinned version} from requirements-lock.txt.

    Empty when the file is not there - a checkout without the lock has no
    measured set to hold a version against, which is said, not failed.
    """
    path = path or LOCK
    pins = {}
    try:
        text = path.read_text(encoding="utf-8")
    except OSError:
        return pins
    for raw in text.splitlines():
        got = _PIN.match(raw.strip())
        if got:
            pins[normalise(got.group(1))] = got.group(2)
    return pins


class Package:
    def __init__(self, pip_name, module, purpose, without, spec=""):
        self.pip_name = pip_name
        self.module = module
        self.purpose = purpose
        self.without = without
        self.spec = spec

    @property
    def installed(self):
        """Importable, and nothing stronger. version_verdict() is R-78."""
        try:
            return importlib.util.find_spec(self.module) is not None
        except (ImportError, ValueError):
            # A half-removed package can leave a spec that raises on lookup.
            # That is not installed, whatever the directory listing says.
            return False

    @property
    def version(self):
        """The installed distribution's version, or None if it has none."""
        try:
            return importlib.metadata.version(self.pip_name)
        except (importlib.metadata.PackageNotFoundError, ValueError):
            return None

    def version_verdict(self, pins):
        """(state, sentence) - is the installed version the RIGHT one (R-78)?

        state is OUT_OF_DATE, "ok" or "unchecked". Only a package that is
        installed has a verdict; the caller asks for it only then.
        """
        have = self.version
        if have is None:
            return "unchecked", ("its version cannot be read, so only presence "
                                 "was checked")
        mine = release(have)
        floors = []
        for clause in [c.strip() for c in self.spec.split(",") if c.strip()]:
            got = _CLAUSE.match(clause)
            if not got:
                return "unchecked", ("%s - the manifest's `%s` is not a "
                                     "shape this check reads, so it was not "
                                     "applied" % (have, clause))
            floors.append((got.group(1), got.group(2)))

        for operator, wanted in floors:
            want = release(wanted)
            if mine is None or want is None:
                continue
            order = compare(mine, want)
            held = {">=": order >= 0, ">": order > 0, "==": order == 0,
                    "<=": order <= 0, "<": order < 0, "!=": order != 0}[operator]
            if not held:
                return OUT_OF_DATE, ("%s, and the manifest asks for %s%s"
                                     % (have, operator, wanted))

        pinned = pins.get(normalise(self.pip_name))
        if pinned and mine is not None and release(pinned) is not None:
            order = compare(mine, release(pinned))
            if order < 0:
                return OUT_OF_DATE, ("%s, older than %s - the version "
                                     "Heron's installer puts on a PC "
                                     "(requirements-lock.txt)" % (have, pinned))
            if order > 0:
                return "ok", ("%s, newer than the %s Heron's installer "
                              "pins - not measured, and not failed for it"
                              % (have, pinned))
            return "ok", ("%s, the one Heron's installer pins" % have)

        if floors:
            return "ok", "%s, which meets the manifest's %s" % (
                have, self.spec)
        return "unchecked", ("%s - no version is recorded as the right "
                             "one for it, so only presence was checked" % have)


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

        requirement = line.split("#")[0].strip()
        named = _REQUIREMENT.match(requirement)
        pip_name = named.group(1) if named else requirement
        spec = named.group(2).strip() if named else ""
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

        packages.append(Package(pip_name, parts[0], parts[1], parts[2], spec))
        previous = None

    return packages, problems


def on_disk(module):
    """Bytes this module occupies on THIS machine, or None if absent.

    ITS OWN FILES ONLY. A package that pulls in another - sentence-transformers
    pulls torch, which is most of what it costs - measures small here and
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


# Which brain module owns the download of which optional package, and the
# name under which it states that download's size - printed by the module
# before the network is touched (heron_rerank, heron_embed) or in the
# sentence a reader meets when it is missing (heron_ingest).
ANNOUNCED_BY = {
    "sentence_transformers": ("heron_rerank", "SIZE"),
    "model2vec": ("heron_embed", "SIZE"),
    "pypdf": ("heron_ingest", "PDF_READER_SIZE"),
}


def announced_size(module):
    """The size the code that owns the download states, if any owns it.

    Read rather than copied: a number copied here goes stale there.
    """
    owner = ANNOUNCED_BY.get(module)
    if owner is None:
        return None
    brain = str(ROOT / "brain")
    if brain not in sys.path:
        sys.path.insert(0, brain)
    try:
        speaker = __import__(owner[0])
    except Exception:
        return None
    size = getattr(speaker, owner[1], None)
    return str(size) if size else None


def report(title, packages, indent="  ", pins=None):
    """Print each package, and return the ones whose version needs an update."""
    pins = read_lock() if pins is None else pins
    stale = []
    for package in packages:
        verdict = package.version_verdict(pins) if package.installed else None
        if verdict and verdict[0] == OUT_OF_DATE:
            mark = OUT_OF_DATE
            stale.append(package)
        else:
            mark = "present" if package.installed else "MISSING"
        print("%s%-22s %s" % (indent, package.pip_name, mark))
        print("%s%-22s for      %s" % (indent, "", package.purpose))
        if verdict:
            print("%s%-22s version  %s" % (indent, "", verdict[1]))
            if verdict[0] == OUT_OF_DATE:
                print("%s%-22s update   pip install --user --upgrade %s"
                      % (indent, "", package.pip_name))
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
    return stale


def main():
    required, problems = read_manifest(REQUIRED)
    optional, more = read_manifest(OPTIONAL)
    problems += more

    pins = read_lock()

    print("REQUIRED - Heron does not run without these")
    print()
    stale = report("required", required, pins=pins)

    print("OPTIONAL - every one of these degrades rather than breaks")
    print()
    stale += report("optional", optional, pins=pins)

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

    if stale:
        # R-78: an old version that loads and half-works is worse than an
        # absent one, so this fails where a missing optional package does not.
        print("INSTALLED, BUT NOT THE RIGHT VERSION - update before relying on it:")
        for package in stale:
            print("  pip install --user --upgrade %s" % package.pip_name)
        print()

    if absent_optional:
        print("%d of %d optional package(s) absent. That is not a fault - Heron"
              % (len(absent_optional), len(optional)))
        print("answers without them and says which one did not run.")
    else:
        print("Every package in both manifests is installed.")

    if problems or stale:
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
