#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   7
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Heron compiles on its OWN C# compiler on a .NET Revit, whatever another add-in
loaded first - FRAGMENT-ISSUES row 5b-575.

    python tests/test_script_isolation.py

THE ROW. On a .NET Revit (2025, 2026, 2027) every add-in shares Revit's
default load context, and an assembly name is loaded there once, by whichever
add-in asks first. On 2026-10-17, on the owner's laptop, AJ Tools had loaded
Microsoft.CodeAnalysis 4.14 before Heron on Revit 2026.5; Heron's own 4.8 C#
layer bound to that core and every fragment compile threw TypeLoadException -
"Method 'CommonGetSemanticModel' ... does not have an implementation". On
2027.3 AJ Tools had loaded all four files first and Heron compiled on AJ
Tools' compiler, by accident.

THE REPAIR. revit/Heron.Revit.Addin/HeronScriptHost.cs: on 2025 and later
Heron.Scripting.dll - the engine and the stack guard - is loaded into a load
context of Heron's own, where the compiler comes from Heron's folder and
everything else from the default one.

WHAT THIS RUNS. tests/Heron.ScriptIsolation.TestHost, a process that plays
Revit: another add-in's newer Roslyn is loaded into its default context by
path first, and Heron's folder is the host's own output, packaged by the same
Heron.Scripting.targets the add-in imports. Seven cases, one process each,
because a load context cannot be emptied:

    --first none|core|all [--other-after]  x  --door heron|direct

`heron` is the add-in's door - THE FIX - and must pass every check in every
case. `direct` is the engine in the default context, which is where the
add-in's Roslyn calls ran until the repair; it is a CONTROL and must reproduce
the row - the exact TypeLoadException with `core` first, the other add-in's
compiler with `all` first, and the other add-in shut out of its own compiler
when Heron went first. A control that stops reproducing means the case no
longer models Revit, and every `heron` pass beside it means nothing - so a
control that passes is this suite failing.

AND TWO THINGS READ FROM THE FILES, no build needed: only the two engine files
under revit/Heron.Revit.Addin name the compiler's namespace, and the add-in's
project gives 2025 and later no compile-time Roslyn reference and builds those
two into Heron.Scripting instead - so a Roslyn type named anywhere else in the
add-in fails the 2025-2027 compile rather than binding to another add-in's copy.

WHAT IT DOES NOT SAY. That Revit itself resolves add-in assemblies the way
this host's resolver does, or that a fragment compiles in Revit 2026 with AJ
Tools installed. That is NEEDS-CHECKING group GW, on the owner's laptop.

PROVED TO GO RED: with HeronScriptHost.CompilerContext.Load returning null for
every name - the compiler then falls through to the default context, the
binding before the repair - the heron door fails in all four of its cases
(the PR that added this suite records the run).

EXIT 3 when no .NET SDK and runtime meet here: NOT a pass, nothing was checked.
"""

import os
import re
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
sys.stderr.reconfigure(encoding="utf-8", errors="replace")

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
HOST = os.path.join(ROOT, "tests", "Heron.ScriptIsolation.TestHost")
ADDIN = os.path.join(ROOT, "revit", "Heron.Revit.Addin")
COULD_NOT_RUN = 3

# The files that may name the compiler's namespace, and the only ones
# Heron.Scripting builds.
ENGINE_FILES = ("HeronScriptEngine.cs", "HeronStackGuard.cs")

CASES = [
    ("--first none --door heron", "Heron alone"),
    ("--first core --door heron", "another add-in's core loaded first - Revit 2026.5"),
    ("--first all --door heron", "all four of its files first - Revit 2027.3"),
    ("--first none --door heron --other-after", "Heron first, then the other add-in"),
    ("--first core --door direct", "CONTROL: the old binding, core first"),
    ("--first all --door direct", "CONTROL: the old binding, all four first"),
    ("--first none --door direct --other-after", "CONTROL: the old binding, Heron first"),
]

_checks = 0
_failures = []


def check(ok, what):
    global _checks
    _checks += 1
    print("  %s  %s" % ("ok  " if ok else "FAIL", what))
    if not ok:
        _failures.append(what)


def read(path):
    try:
        with open(path, encoding="utf-8") as f:
            return f.read()
    except OSError:
        return None


def code_only(text):
    """C# with // and /* */ comments and string literals blanked out."""
    text = re.sub(r"/\*.*?\*/", "", text, flags=re.S)
    text = re.sub(r"//[^\n]*", "", text)
    return re.sub(r'@?"(?:[^"\\\n]|\\.)*"', '""', text)


def files_section():
    print("The files: who may name the compiler, and what 2025 and later compile")

    named = []
    for name in sorted(os.listdir(ADDIN)):
        if not name.endswith(".cs") or name in ENGINE_FILES:
            continue
        text = code_only(read(os.path.join(ADDIN, name)) or "")
        if re.search(r"\bMicrosoft\.CodeAnalysis\b|\b(CSharpScript|ScriptOptions|ScriptState|"
                     r"CSharpSyntaxRewriter|CSharpSyntaxTree)\b", text):
            named.append(name)
    check(not named, "no add-in file but %s names a Roslyn type%s"
          % (" and ".join(ENGINE_FILES), "" if not named else " - " + ", ".join(named)))

    fragment = code_only(read(os.path.join(ADDIN, "RevitFragment.cs")) or "")
    check("HeronScriptHost.Compile(" in fragment and "HeronScriptHost.Run(" in fragment,
          "RevitFragment compiles and runs through HeronScriptHost")

    project = read(os.path.join(ADDIN, "Heron.Revit.Addin.csproj")) or ""
    roslyn = re.findall(r"<ItemGroup([^>]*)>\s*<PackageReference Include=\"Microsoft\.CodeAnalysis[^\"]*\"",
                        project)
    check(len(roslyn) == 1 and "&lt; '2025'" in roslyn[0],
          "the add-in references the compiler package on releases before 2025 only%s"
          % ("" if len(roslyn) == 1 else " - %d unconditioned or extra reference(s)" % len(roslyn)))
    removed = re.search(r"<ItemGroup Condition=\"'\$\(RevitVersion\)' &gt;= '2025'\">\s*"
                        r"<Compile Remove=\"([^\"]*)\"", project)
    check(removed is not None and all(f in removed.group(1) for f in ENGINE_FILES),
          "and on 2025 and later builds neither engine file into itself")
    check(re.search(r"<Import Project=\"[^\"]*Heron\.Scripting\.targets\" Condition=\"'\$\(RevitVersion\)' "
                    r"&gt;= '2025'\"", project) is not None,
          "but imports Heron.Scripting.targets, which puts the compiler beside it")

    scripting = read(os.path.join(ROOT, "revit", "Heron.Scripting", "Heron.Scripting.csproj")) or ""
    linked = re.findall(r'<Compile Include="\.\.\\Heron\.Revit\.Addin\\([^"]+)"', scripting)
    check(sorted(linked) == sorted(ENGINE_FILES),
          "Heron.Scripting links exactly the two engine files - %s" % ", ".join(linked))
    references = re.findall(r'<(?:Package|Project)?Reference Include="([^"]+)"',
                            re.sub(r"<!--.*?-->", "", scripting, flags=re.S))
    revit = [r for r in references if "Revit" in r or "Autodesk" in r]
    check(references and not revit,
          "and references no Revit assembly - a RevitAPI type inside Heron's context would be "
          "a second copy%s" % ("" if not revit else " - " + ", ".join(revit)))

    host = code_only(read(os.path.join(ADDIN, "HeronScriptHost.cs")) or "")
    branched = [f for f in ENGINE_FILES
                if "#if" in code_only(read(os.path.join(ADDIN, f)) or "")]
    check("#if REVIT2025_OR_GREATER" in host and not branched,
          "the version branch lives in HeronScriptHost, and neither engine file has one%s"
          % ("" if not branched else " - " + ", ".join(branched)))
    print()


# -- the build, as tests/test_stack_guard.py does it -------------------------

def _asked(what):
    try:
        out = subprocess.check_output(["dotnet", what], stderr=subprocess.STDOUT)
    except Exception:                                      # noqa: BLE001
        return None
    return out.decode("utf-8", "replace")


def _majors(said, prefix=None):
    out = []
    for line in (said or "").splitlines():
        parts = line.split()
        if prefix:
            if not line.startswith(prefix):
                continue
            parts = parts[1:]
        if not parts:
            continue
        try:
            out.append(int(parts[0].split(".")[0]))
        except ValueError:
            continue
    return sorted(set(out))


def _tfm():
    """The newest framework both an installed SDK can build and a runtime can run."""
    runtimes = _majors(_asked("--list-runtimes"), "Microsoft.NETCore.App")
    sdks = _majors(_asked("--list-sdks"))
    if not runtimes or not sdks:
        return None
    usable = [m for m in runtimes if 8 <= m <= max(sdks)]
    return "net%d.0" % max(usable) if usable else None


def main():
    files_section()

    tfm = _tfm()
    if tfm is None:
        print("COULD NOT RUN the cases - no .NET 8 or later here that an installed SDK can")
        print("  build AND an installed runtime can run. This suite builds its host and runs")
        print("  it, so it needs both. Linux: apt-get install -y dotnet-sdk-10.0")
        print("  This is exit 3, which is NOT a pass: the cases were not checked.")
        return 1 if _failures else COULD_NOT_RUN

    out_dir = "bin/x64/Isolation-%s/" % tfm
    built = subprocess.run(
        ["dotnet", "build", HOST, "-p:RevitVersion=2026", "-p:HeronTfm=%s" % tfm,
         "-p:OutputPath=%s" % out_dir],
        stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    if built.returncode != 0:
        print("FAILED - the test host did not build for %s." % tfm)
        said = (built.stdout or b"").decode("utf-8", "replace").strip()
        for line in said.splitlines()[-25:]:
            print("    %s" % line)
        return 1

    dll = os.path.join(HOST, out_dir, "Heron.ScriptIsolation.TestHost.dll")
    print("The cases, each in its own process, on %s" % tfm)
    for arguments, what in CASES:
        ran = subprocess.run(["dotnet", dll] + arguments.split(),
                             stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
        said = (ran.stdout or b"").decode("utf-8", "replace")
        check(ran.returncode == 0, "%s (%s)" % (what, arguments))
        if ran.returncode != 0:
            for line in said.strip().splitlines():
                print("        %s" % line)
    print()

    if _failures:
        print("FAILED - %d of %d:" % (len(_failures), _checks))
        for failure in _failures:
            print("  " + failure)
        return 1
    print("PASSED - %d checks" % _checks)
    return 0


if __name__ == "__main__":
    sys.exit(main())
