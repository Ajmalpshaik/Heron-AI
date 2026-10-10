#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   14
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A fragment is compiled with the release symbols the add-in was built with.

    python tests/test_fragment_symbols.py

WHY THIS EXISTS - FRAGMENT-ISSUES ROW 5b-181
-------------------------------------------
The add-in compiles a fragment with Roslyn inside Revit, and until 2026-10-09
it told Roslyn nothing about the release. So `#if REVIT2020 || REVIT2021` was
false on Revit 2020, every version branch in the library compiled its #else
there, and every one of those #else branches calls a member the older
releases have not got. `tools/check-fragments-compile.py` compiles the same
files WITH the symbols, through Directory.Build.props, so the gate reported
green on 2020 for code the add-in would never compile there.

Measured 2026-10-09, not read: the gate's own 2020 build tree
(`check-fragments-compile.py --keep 2020`, exit 0), rebuilt with
`-p:DefineConstants=TRACE` so no release symbol reaches it - which is what the
add-in did - fails with CS1061 / CS1503 on exactly the fragments this suite's
host lists as compiling a different branch on 2020, and on no other.

The fix is `revit/Heron.Revit.Addin/HeronFragmentSymbols.cs`: the symbols this
build of the add-in was compiled with, read back from its own #if constants
and put in front of every script as `#define` lines and a `#line 1`. This
suite holds it to that, in two halves.

WITHOUT A COMPILER - read from the files
  1. the helper reads back EVERY symbol Directory.Build.props can define, on
     any release, each under its own #if - so a release added to the props
     and not to the helper fails here, not in front of a modeller;
  2. every script RevitFragment compiles goes through the helper, and through
     it BEFORE the stack guard, which parses the source too;
  3. no fragment's #if names anything but a release symbol. The gate's build
     also defines DEBUG, TRACE and the framework's own symbols; the add-in
     hands over release symbols only, so a fragment that tested one of those
     would compile one branch in the gate and the other in Revit - this row
     again, by another door.

WITH A COMPILER - `tests/Heron.StackGuard.TestHost`, built once per release
  4. the host, built with each release's DefineConstants exactly as the add-in
     is, reads back exactly the symbols the props give that release;
  5. its own checks pass there: a one-line `#if` script RUNS true with the
     header and false without it, the same compiler error is reported at the
     same line and column with and without, and the guard reaches a lambda
     only the release's own branch holds;
  6. every fragment whose #if names a symbol of that release is one the host
     sees compiling different code with the header than without.

IT EXITS 3 WHEN .NET IS ABSENT, AFTER THE FIRST HALF HAS RUN - 3 only if that
half passed, 1 if it did not. Exit 3 is NOT a pass: the half that runs
anything was not run.

WHAT IT DOES NOT SAY: that the branch now compiled on 2020 works in front of
Revit 2020. The gate compiles that branch against 2020's reference assemblies;
whether it does the right thing needs that release and a model.
"""

import importlib.util
import io
import os
import re
import subprocess
import sys

for _stream in (sys.stdout, sys.stderr):
    try:
        _stream.reconfigure(encoding="utf-8", errors="replace")
    except (AttributeError, ValueError):
        pass

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

HOST = os.path.join(ROOT, "tests", "Heron.StackGuard.TestHost")
HELPER = os.path.join(ROOT, "revit", "Heron.Revit.Addin", "HeronFragmentSymbols.cs")
EXECUTOR = os.path.join(ROOT, "revit", "Heron.Revit.Addin", "RevitFragment.cs")
FRAGMENTS = os.path.join(ROOT, "brain", "fragments")
COULD_NOT_RUN = 3

FAILURES = []
CHECKS = [0]


def check(condition, what):
    CHECKS[0] += 1
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def read(path):
    try:
        return io.open(path, encoding="utf-8").read()
    except (IOError, OSError):
        return None


def props_symbols():
    """{release: [symbol, ...]} as Directory.Build.props defines them.

    heron_buildmatrix owns the reading of the props file and heron_dotnet the
    list of releases; a third copy of either here would be the one to drift.
    """
    import heron_buildmatrix as BM
    import heron_dotnet as NET
    return dict((str(r), BM.symbols(r)) for r in NET.RELEASES)


def fragment_conditions():
    """{fragment folder: set of identifiers its #if / #elif lines name}."""
    found = {}
    for name in sorted(os.listdir(FRAGMENTS)):
        impl = os.path.join(FRAGMENTS, name, "impl")
        if not os.path.isdir(impl):
            continue
        for variant in sorted(os.listdir(impl)):
            text = read(os.path.join(impl, variant, "fragment.cs")) or ""
            for line in text.splitlines():
                directive = re.match(r"\s*#\s*(if|elif)\b(.*)", line)
                if not directive:
                    continue
                names = set(re.findall(r"[A-Za-z_]\w*", directive.group(2)))
                names -= {"true", "false"}
                found.setdefault(name, set()).update(names)
    return found


def stack_guard_tfm():
    """The framework tests/test_stack_guard.py would build its host for.

    Borrowed rather than copied: the same host, so the same answer to "which
    framework can this machine both build and run", and that function's
    docstring records the two review findings a fresh copy would have to
    re-learn.
    """
    spec = importlib.util.spec_from_file_location(
        "heron_stack_guard_suite", os.path.join(ROOT, "tests", "test_stack_guard.py"))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module._tfm()


def without_a_compiler(table, conditions):
    print("Without a compiler - read from the files")
    print()

    every = set()
    for symbols in table.values():
        every.update(symbols)
    check(len(table) > 0 and all(table.values()),
          "Directory.Build.props gives every release at least one symbol")

    # 1. THE HELPER READS BACK EVERY SYMBOL, EACH UNDER ITS OWN #if.
    helper = read(HELPER)
    check(helper is not None, "the helper exists - %s" % os.path.relpath(HELPER, ROOT))
    pairs = re.findall(r'#if\s+(\w+)\s*\n\s*symbols\.Add\("(\w+)"\);\s*\n\s*#endif',
                       helper or "")
    crossed = ["#if %s adds %s" % (a, b) for a, b in pairs if a != b]
    check(not crossed, "each #if in the helper adds its own name and no other%s"
          % ("" if not crossed else " - " + "; ".join(crossed)))
    captured = set(a for a, b in pairs if a == b)
    missing = sorted(every - captured)
    check(not missing, "the helper reads back every symbol the props can define%s"
          % ("" if not missing else " - missing " + ", ".join(missing)))
    extra = sorted(captured - every)
    check(not extra, "and none the props never define%s"
          % ("" if not extra else " - " + ", ".join(extra)))

    # 2. EVERY SCRIPT GOES THROUGH IT, AND THROUGH IT BEFORE THE GUARD.
    executor = read(EXECUTOR) or ""
    creates = len(re.findall(r"CSharpScript\.Create\b", executor))
    guarded = re.findall(r"HeronStackGuard\.Apply\(([^;]*)\);", executor)
    check(creates > 0, "RevitFragment compiles scripts at all")
    check(len(guarded) == creates,
          "every script RevitFragment compiles is guarded first (%d compiled, %d guarded)"
          % (creates, len(guarded)))
    bare = [g for g in guarded if not g.strip().startswith("HeronFragmentSymbols.Prepend(")]
    check(guarded and not bare,
          "and every one the guard reads carries the release symbols already%s"
          % ("" if not bare else " - bare: " + "; ".join(bare)))

    # 3. NO FRAGMENT TESTS A SYMBOL THE ADD-IN DOES NOT HAND OVER.
    check(len(conditions) > 0, "some fragment carries an #if to read at all")
    strays = sorted("%s: %s" % (name, ", ".join(sorted(names - every)))
                    for name, names in conditions.items() if names - every)
    check(not strays, "every fragment #if names release symbols only%s"
          % ("" if not strays else " - " + "; ".join(strays)))
    print()


def run_host(release, tfm):
    """(exit code, output) of the host built for one release, or (None, why)."""
    out_dir = "bin/x64/Symbols-%s-%s/" % (tfm, release)
    built = subprocess.run(
        ["dotnet", "build", HOST, "-p:RevitVersion=%s" % release,
         "-p:HeronTfm=%s" % tfm, "-p:OutputPath=%s" % out_dir],
        stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    if built.returncode != 0:
        said = (built.stdout or b"").decode("utf-8", "replace").strip()
        return None, "\n".join(said.splitlines()[-15:])
    dll = os.path.join(HOST, out_dir, "Heron.StackGuard.TestHost.dll")
    if not os.path.exists(dll):
        return None, "built, and %s is not there" % dll
    ran = subprocess.run(["dotnet", dll, "--symbols"],
                         stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    return ran.returncode, (ran.stdout or b"").decode("utf-8", "replace")


def said(output, label):
    """The ';'-separated list on the host's `label:` line, or None if absent."""
    for line in output.splitlines():
        if line.startswith(label + ":"):
            value = line[len(label) + 1:].strip()
            return [v for v in value.split(";") if v]
    return None


def with_a_compiler(table, conditions, tfm):
    print("With a compiler - the host built once per release, for %s" % tfm)
    print()
    for release in sorted(table):
        code, output = run_host(release, tfm)
        if code is None:
            check(False, "%s: the host builds - it did not:\n%s" % (release, output))
            continue

        # 4. EXACTLY THE PROPS' SYMBOLS.
        defined = said(output, "defined")
        check(defined is not None and sorted(defined) == sorted(table[release]),
              "%s: the add-in's helper reads back %s - got %s"
              % (release, ", ".join(table[release]),
                 "nothing" if defined is None else ", ".join(defined) or "none"))

        # 5. THE HOST'S OWN CHECKS.
        check(code == 0, "%s: the host's own checks pass (exit %s)" % (release, code))
        if code != 0:
            for line in output.splitlines()[-27:]:
                print("        %s" % line)

        # 6. EVERY FRAGMENT TESTING ONE OF THIS RELEASE'S SYMBOLS IS READ
        #    DIFFERENTLY NOW. Derived from the fragments, never listed here.
        changed = said(output, "changed")
        expected = sorted(name for name, names in conditions.items()
                          if names & set(table[release]))
        unseen = sorted(set(expected) - set(changed or []))
        check(changed is not None and not unseen,
              "%s: every fragment whose #if names a %s symbol compiles a different "
              "branch with the symbols than without%s"
              % (release, release,
                 "" if not unseen else " - unchanged: " + ", ".join(unseen)))
        print("        %s: %d fragment(s) now compile a different branch%s"
              % (release, len(changed or []),
                 "" if not changed else " - " + ", ".join(changed)))
    print()


def main():
    print("The release symbols a fragment is compiled with (row 5b-181)")
    print("=" * 62)
    print()

    table = props_symbols()
    conditions = fragment_conditions()
    without_a_compiler(table, conditions)

    tfm = stack_guard_tfm()
    if tfm is None:
        print("COULD NOT RUN the half with a compiler - no framework here that an")
        print("  installed SDK can build AND an installed runtime can run.")
        print("  Linux:   apt-get install -y dotnet-sdk-10.0")
        if FAILURES:
            print("FAILED - %d of %d checks, in the half that did run." % (len(FAILURES), CHECKS[0]))
            return 1
        print("This is exit 3, which is NOT a pass: half of it was not checked.")
        return COULD_NOT_RUN

    with_a_compiler(table, conditions, tfm)

    if FAILURES:
        print("FAILED - %d of %d checks:" % (len(FAILURES), CHECKS[0]))
        for failure in FAILURES[:25]:
            print("  " + failure.splitlines()[0])
        return 1
    print("PASSED - %d checks, on %d releases." % (CHECKS[0], len(table)))
    print()
    print("What this does NOT say: that the branch now compiled on an older release")
    print("works in front of that Revit. That needs the release itself, and a model.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
