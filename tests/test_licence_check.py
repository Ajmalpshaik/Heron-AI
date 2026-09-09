# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Proves tools/check-licence.py FIRES, which a clean run does not.

    python tests/test_licence_check.py

A licence checker that reports nothing on a clean library and has never been
shown a dirty one is a plausible zero (D-52) wearing a gate's clothes. So this
builds the exact failure Q-53 was written about - K-Dense-AI's README says MIT
while four of its skills say "(c) 2025 Anthropic, PBC. All rights reserved." -
and checks that each marker is caught on its own.

IT ALSO PROVES THE FALSE POSITIVE STAYS FIXED. The first version of the
copyright pattern matched this line of ordinary C#:

    var size = rule.GetCriterion(c) as PrimarySizeCriterion;

and reported report-routing-preferences as somebody else's work. A licence tool
that cries wolf is a tool somebody turns off, and it would have been turned off
over a cast. `(c)` counts only next to a year.
"""

import io
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "tools"))

import importlib.util
spec = importlib.util.spec_from_file_location(
    "check_licence", os.path.join(ROOT, "tools", "check-licence.py"))
LIC = importlib.util.module_from_spec(spec)
spec.loader.exec_module(LIC)

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def write(folder, name, body):
    path = os.path.join(folder, name)
    parent = os.path.dirname(path)
    if parent and not os.path.isdir(parent):
        os.makedirs(parent)
    io.open(path, "w", encoding="utf-8").write(body)
    return path


def main():
    print("Does the licence check actually fire?")
    print("=" * 70)

    work = tempfile.mkdtemp(prefix="heron-lic-")
    try:
        print()
        print("1. The live failure: a reservation of rights inside the files")
        print("-" * 70)
        unit = os.path.join(work, "borrowed-skill")
        write(unit, "fragment.yaml", u"source: OFFICIAL\nid: FRG-X-001\n")
        write(unit, "impl/any/fragment.cs",
              u"// Copyright 2025 Anthropic, PBC. All rights reserved.\nvar x = 1;\n")
        reserved, licences, holders = LIC.inspect(unit)
        check(bool(reserved), "the reservation is found in a nested file")
        check(any("Anthropic" in h for h in holders),
              "and the holder is read out: %s" % sorted(holders))
        check(LIC.foreign(holders), "which is not Heron's own")

        print()
        print("2. An incompatible licence name is caught by name")
        print("-" * 70)
        unit = os.path.join(work, "gpl-thing")
        write(unit, "fragment.yaml", u"source: COMMUNITY\n")
        write(unit, "NOTICE.md", u"Licensed under the GPL-3 for all users.\n")
        _r, licences, _h = LIC.inspect(unit)
        bad = [l for l in licences
               if not any(ok in l for ok in LIC.COMPATIBLE)]
        check(bool(bad), "GPL-3 is not on the compatible list: %s" % bad)

        print()
        print("3. A COMPATIBLE licence is not reported")
        print("-" * 70)
        unit = os.path.join(work, "mit-thing")
        write(unit, "fragment.yaml", u"source: COMMUNITY\n")
        write(unit, "NOTICE.md", u"Licensed under the MIT license.\n")
        _r, licences, _h = LIC.inspect(unit)
        bad = [l for l in licences
               if not any(ok in l for ok in LIC.COMPATIBLE)]
        check(not bad, "MIT passes (%s)" % sorted(licences))

        print()
        print("4. THE FALSE POSITIVE STAYS FIXED")
        print("-" * 70)
        unit = os.path.join(work, "ordinary-csharp")
        write(unit, "fragment.yaml", u"source: OFFICIAL\n")
        write(unit, "impl/any/fragment.cs",
              u"var size = rule.GetCriterion(c) as PrimarySizeCriterion;\n"
              u"var other = thing.Get(c) as Something;\n")
        _r, _l, holders = LIC.inspect(unit)
        check(not LIC.foreign(holders),
              "`(c)` in a cast is not a copyright holder (%s)"
              % (sorted(holders) or "nothing found"))

        print()
        print("5. Heron's own marker is not somebody else's work")
        print("-" * 70)
        unit = os.path.join(work, "ours")
        write(unit, "fragment.yaml", u"source: OFFICIAL\n")
        write(unit, "README.md", u"Copyright 2026 Heron AI\n")
        _r, _l, holders = LIC.inspect(unit)
        check(not LIC.foreign(holders),
              "a Heron copyright is not foreign (%s)" % sorted(holders))

        print()
        print("6. The real library is clean, and now that means something")
        print("-" * 70)
        out = io.StringIO()
        stdout, sys.stdout = sys.stdout, out
        try:
            code = LIC.main([])
        finally:
            sys.stdout = stdout
        text = out.getvalue()
        check(code == 0, "check-licence.py exits 0 on the library today")
        check("0 finding(s)" in text, "with no findings")
        check("370 unit(s) checked" in text or "unit(s) checked" in text,
              "having actually checked the units")
    finally:
        shutil.rmtree(work, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1
    print("PASSED - the checker catches a reservation of rights, an")
    print("incompatible licence and a foreign holder, passes a compatible")
    print("one, and does not mistake a C# cast for a copyright.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
