#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   14
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Compile every fragment's C#, against every Revit release it claims, with no
Revit and no Windows.

    python tools/check-fragments-compile.py              every declared release
    python tools/check-fragments-compile.py 2020 2024    just these
    python tools/check-fragments-compile.py --keep       leave the build tree

WHY THIS EXISTS
---------------
`tools/check-compile.py` compiles the four PROJECTS. Until this file, **nothing
compiled the fragments** - and a fragment is C# that will one day be handed to
Roslyn and run inside Revit (D-28). So the library's whole point, the part that
does the work, was the one part no compiler had ever read.

That gap is not hypothetical here. Compiling the projects for the first time
found `Document.CreationGUID` missing in Revit 2020 - it built clean on 2024,
and reading had passed over it twice. The fragments have had neither a compiler
nor a Revit, which is strictly worse.

The cost of finding it here is a minute. The cost of finding it at the PC is a
round trip through Revit, and the owner's time is the scarce thing.

A FRAGMENT IS A SNIPPET, SO IT IS COMPILED INSIDE A HARNESS
-----------------------------------------------------------
`impl/*/fragment.cs` is not standalone: it assumes names are in scope and leaves
names behind for whatever is composed after it (D-29). So each one is wrapped in
a method whose PARAMETERS ARE ITS DECLARED `needs`, generated from the contract
rather than guessed.

AND THE CONTRACT IS CHECKED, NOT JUST THE SYNTAX
------------------------------------------------
After the snippet, each name the contract `provides` is assigned to a local of
the declared type. So a fragment that promises `IList<Element> elements` and
leaves something else - or leaves nothing of that name at all - FAILS. That is a
contract drift check, and it is the half worth more than the syntax: a snippet
that compiles while breaking its promise is exactly the fragment that composes
into something broken later, far from here.

WHAT IT STILL CANNOT DO
-----------------------
It proves the API surface agrees. It says NOTHING about behaviour - whether the
filter finds the right elements, whether a duct moves 200 mm or 200 feet. That
needs a real model, it is D-30's proof with a negative case, and no compiler
will ever stand in for it.
"""

import argparse
import io
import os
import re
import shutil
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FRAGMENTS = os.path.join(ROOT, "brain", "fragments")

# Generated under the repo ON PURPOSE: Directory.Build.props maps a Revit
# release to its runtime, and that mapping is D-05's single home. A project
# built outside the tree would not see it, and copying the table in here would
# be a second copy to keep in step - the failure this repository keeps having.
BUILD = os.path.join(ROOT, "build", "fragment-check")

USINGS = [
    "System",
    "System.Collections.Generic",
    "System.Linq",
    "Autodesk.Revit.ApplicationServices",
    "Autodesk.Revit.DB",
    # Rooms live here. Added 2026-09-01 for FILTER_ELEMENTS_IN_ROOM, which
    # needs Room.IsPointInRoom - the API designed for exactly that question.
    # The alternative was hand-rolling point-in-polygon over the boundary
    # segments, which is precisely how an L-shaped room gets answered wrongly,
    # and that is the case the fragment exists to get right.
    #
    # THIS LIST IS A CONTRACT WITH UNBUILT WORK. It declares what a fragment
    # may assume is in scope, so D-28's in-process Roslyn executor must supply
    # the same set. A namespace added here and not there compiles green and
    # fails at the PC.
    "Autodesk.Revit.DB.Architecture",
    "Autodesk.Revit.DB.Mechanical",
    "Autodesk.Revit.DB.Plumbing",
    "Autodesk.Revit.DB.Electrical",
    "Autodesk.Revit.DB.Structure",
    "Autodesk.Revit.UI",
    # Extensible storage - the data an add-in writes INTO the file, which is
    # invisible everywhere in Revit's own interface. Added 2026-09-06 for
    # REPORT_ADDIN_DATA. There is no way to reach a Schema without naming its
    # namespace, and writing it out in full inside the fragment is worse than
    # declaring it here: tools/check-structure.py forbids `Autodesk.Revit` in
    # brain/ precisely so a fragment can only use what THIS LIST says it has.
    # A fully-qualified name smuggles in a namespace the executor was never
    # told to supply - green here, missing at the PC.
    #
    # A namespace whose types a fragment could reach ANOTHER way does not
    # belong here. The Revit exceptions namespace was deliberately NOT added
    # for COMPARE_MODELS: it matches the two exception type NAMES instead,
    # which needs nothing declared.
    "Autodesk.Revit.DB.ExtensibleStorage",
    # Revit's analysis visualisation - the gradient overlay with a legend that
    # a value per element is painted with. Added 2026-09-06 for
    # SHOW_ANALYSIS_HEATMAP. Like extensible storage above, there is no way to
    # reach a spatial field manager or a display style without naming this
    # namespace, and writing it out in full inside a fragment is worse:
    # tools/check-structure.py forbids `Autodesk.Revit` in brain/ exactly so a
    # fragment can only use what THIS LIST says it has. Adding it here is a
    # promise the executor must keep too.
    "Autodesk.Revit.DB.Analysis",
]

CSPROJ = """<Project Sdk="Microsoft.NET.Sdk">
  <!-- GENERATED by tools/check-fragments-compile.py. Not tracked; not edited. -->
  <PropertyGroup>
    <TargetFramework>$(HeronTfm)</TargetFramework>
    <AssemblyName>Heron.FragmentCheck</AssemblyName>
    <RootNamespace>Heron.FragmentCheck</RootNamespace>
    <EnableDefaultCompileItems>true</EnableDefaultCompileItems>
    <!-- A fragment legitimately leaves names for a later fragment to use, so
         "assigned but never used" is not a defect here. Everything else is. -->
    <NoWarn>$(NoWarn);CS0219;CS0168</NoWarn>
    <!-- The SAME compilation symbols the add-in is built with, mirrored from
         Directory.Build.props. Without them a fragment cannot express a version
         split that the add-in handles routinely - and some splits have no
         single expression: IndependentTag.GetTaggedLocalElementIds does not
         exist before 2022, and the singular property it replaced is deprecated
         after. A fragment forced to pick one is simply wrong on half the range.
         If Directory.Build.props gains a symbol, add it here too, or fragments
         and the add-in stop being compiled the same way. -->
    <DefineConstants>$(DefineConstants);REVIT$(RevitVersion)</DefineConstants>
    <DefineConstants Condition="'$(RevitVersion)' &gt;= '2024'">$(DefineConstants);REVIT2024_OR_GREATER</DefineConstants>
    <DefineConstants Condition="'$(RevitVersion)' &gt;= '2025'">$(DefineConstants);REVIT2025_OR_GREATER</DefineConstants>
    <DefineConstants Condition="'$(RevitVersion)' &gt;= '2026'">$(DefineConstants);REVIT2026_OR_GREATER</DefineConstants>
    <DefineConstants Condition="'$(RevitVersion)' &gt;= '2027'">$(DefineConstants);REVIT2027_OR_GREATER</DefineConstants>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Nice3point.Revit.Api.RevitAPI" Version="$(RevitVersion).*" ExcludeAssets="runtime" />
    <PackageReference Include="Nice3point.Revit.Api.RevitAPIUI" Version="$(RevitVersion).*" ExcludeAssets="runtime" />
  </ItemGroup>
</Project>
"""


def load_fragments():
    """(id, folder, revit list, needs, provides, impl path) for each fragment."""
    try:
        import yaml
    except ImportError:
        sys.stderr.write("This needs PyYAML: pip install --user pyyaml\n")
        raise

    found = []
    for name in sorted(os.listdir(FRAGMENTS)):
        folder = os.path.join(FRAGMENTS, name)
        manifest = os.path.join(folder, "fragment.yaml")
        if not os.path.isfile(manifest):
            continue
        data = yaml.safe_load(io.open(manifest, encoding="utf-8").read())
        contract = data.get("contract") or {}

        impl_root = os.path.join(folder, "impl")
        if not os.path.isdir(impl_root):
            continue
        for variant in sorted(os.listdir(impl_root)):
            source = os.path.join(impl_root, variant, "fragment.cs")
            if not os.path.isfile(source):
                continue
            found.append({
                "id": data.get("id") or name,
                "folder": name,
                "variant": variant,
                "source": source,
                "revit": [str(v) for v in (data.get("revit") or [])],
                "needs": contract.get("needs") or [],
                "provides": contract.get("provides") or [],
            })
    return found


def harness(fragment):
    """One fragment, wrapped so a compiler can read it.

    `#line` points the compiler back at the fragment's own file, so an error
    reads as brain/fragments/x/impl/any/fragment.cs(42) rather than naming a
    generated file the reader has never seen and cannot fix.
    """
    body = io.open(fragment["source"], encoding="utf-8").read()
    # ABSOLUTE, and it has to be. A repo-relative #line built fine on net472
    # and net48 and failed on every net8/net10 release with CS1504 "source file
    # could not be opened" - the newer compiler resolves the path itself rather
    # than only printing it, and resolves it against the project rather than the
    # working directory. Found by running all eight releases instead of one.
    relative = os.path.abspath(fragment["source"]).replace("\\", "/")
    safe = re.sub(r"[^A-Za-z0-9]", "_", "%s_%s" % (fragment["id"], fragment["variant"]))

    params = ", ".join("%s %s" % (n["type"], n["name"]) for n in fragment["needs"])

    after = []
    for provided in fragment["provides"]:
        # THE CONTRACT CHECK. A promise of IList<Element> that leaves a
        # different type, or leaves nothing of that name, stops being a
        # documentation error and becomes a compiler error.
        after.append("            %s __provides_%s = %s;"
                     % (provided["type"], provided["name"], provided["name"]))
        after.append("            GC.KeepAlive(__provides_%s);" % provided["name"])

    return (
        "// GENERATED by tools/check-fragments-compile.py - do not edit.\n"
        + "".join("using %s;\n" % u for u in USINGS)
        + "\nnamespace Heron.FragmentCheck\n{\n"
        + "    internal static class Check_%s\n    {\n" % safe
        + "        internal static void Run(%s)\n        {\n" % params
        + '#line 1 "%s"\n' % relative
        + body.rstrip("\n") + "\n"
        + "#line default\n"
        + ("\n".join(after) + "\n" if after else "")
        + "        }\n    }\n}\n"), safe


def build(version, fragments, keep):
    """Compile every fragment that claims this release. Returns (ok, failures)."""
    want = [f for f in fragments if version in f["revit"]]
    if not want:
        return True, [], 0

    where = os.path.join(BUILD, version)
    shutil.rmtree(where, ignore_errors=True)
    os.makedirs(where)

    by_file = {}
    for fragment in want:
        text, safe = harness(fragment)
        path = os.path.join(where, "%s.cs" % safe)
        io.open(path, "w", encoding="utf-8").write(text)
        by_file[os.path.relpath(fragment["source"], ROOT).replace("\\", "/")] = fragment["id"]

    io.open(os.path.join(where, "FragmentCheck.csproj"), "w",
            encoding="utf-8").write(CSPROJ)

    run = subprocess.run(
        ["dotnet", "build", os.path.join(where, "FragmentCheck.csproj"),
         "-p:RevitVersion=%s" % version, "-v", "quiet", "--nologo"],
        stdout=subprocess.PIPE, stderr=subprocess.STDOUT, cwd=ROOT)
    out = run.stdout.decode("utf-8", "replace")

    # WHICH FRAGMENT. An error inside the snippet names the fragment's own file
    # (the #line directive above); an error in the generated wrapper names the
    # wrapper, whose filename carries the id. Matching on "fragment.cs" alone
    # blamed whichever fragment happened to be first in the dictionary - every
    # path ends in that name.
    blame = {}
    for source, fid in by_file.items():
        blame[source] = fid
    safe_names = {}
    for fragment in want:
        _text, safe = harness(fragment)
        safe_names[safe] = fragment["id"]

    failures, seen = [], set()
    for line in out.splitlines():
        if ": error " not in line:
            continue
        tidy = line.strip()
        if tidy in seen:          # MSBuild repeats the summary at the end
            continue
        seen.add(tidy)
        flat = tidy.replace("\\", "/")
        blamed = None
        for source, fid in blame.items():
            if source in flat:
                blamed = fid
                break
        if blamed is None:
            for safe, fid in safe_names.items():
                if safe + ".cs" in flat:
                    blamed = fid
                    break
        failures.append((blamed or "?", tidy))

    if not keep:
        shutil.rmtree(where, ignore_errors=True)
    return run.returncode == 0, failures, len(want)


def main():
    parser = argparse.ArgumentParser(add_help=True)
    parser.add_argument("versions", nargs="*")
    parser.add_argument("--keep", action="store_true",
                        help="leave the generated project for inspection")
    args = parser.parse_args()

    fragments = load_fragments()
    if not fragments:
        print("No fragments with an impl/ found. Nothing to compile.")
        return 1

    declared = sorted(set(v for f in fragments for v in f["revit"]))
    versions = args.versions or declared
    unknown = [v for v in versions if v not in declared]
    if unknown:
        print("No fragment claims Revit %s. Declared: %s"
              % (", ".join(unknown), ", ".join(declared)))
        return 2

    print("%d fragment implementation(s) across %d release(s)."
          % (len(fragments), len(versions)))
    print()

    bad = []
    for version in versions:
        ok, failures, count = build(version, fragments, args.keep)
        print("Revit %s  %s  (%d fragment(s) claim it)"
              % (version, "ok  " if ok else "FAIL", count))
        for fid, line in failures:
            print("    %-14s %s" % (fid, line))
        if not ok:
            bad.append(version)

    print()
    if bad:
        print("FAILED on %s." % ", ".join(bad))
        print("A fragment that does not compile cannot be proven against a")
        print("model, so this is worth fixing before the machine is in front")
        print("of you rather than after.")
        return 1

    print("Every fragment compiles on every release it claims.")
    print()
    print("That is the API surface agreeing, and the contract being kept -")
    print("each fragment leaves what it promised, at the declared type. It is")
    print("NOT evidence that any of them DOES the right thing: that needs a")
    print("real model and a proof with a negative case (D-30).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
