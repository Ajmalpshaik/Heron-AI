#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
`tools/api-surface --members`: what a call looks like on each release.

    python tests/test_api_members.py

WHY IT EXISTS
-------------
Heron's two readers of the Revit API match by NAME. `check-api-surface.py`
says so ("a member that still exists but changed SIGNATURE passes here") and
so does `api-changes.py` ("[Obsolete] is an attribute and this reads names").
`--members` is the lookup that reads what they cannot: parameters, return
type, get/set and the [Obsolete] mark, release by release. Package C3 of the
earlier-brain plan asked for it, and the fragment-writing agent planned after
it reads signatures through it.

WHAT IT PROVES, against assemblies this suite builds for itself
  1. A CHANGED SIGNATURE IS TWO LINES, each carrying the releases that have
     it exactly - the case name-matching reads as "no change".
  2. A SETTER THAT WENT AWAY is a line of its own; get and set are read.
  3. [Obsolete] IS READ, message and all, only where it is marked - and an
     error-level mark says so.
  4. Parameters keep their names, `out` and their defaults; `static` is
     marked; an enum's values come in number order, a late one saying so.
  5. Type.Member narrows; a short name meaning two types is refused with
     both named (exit 2); a name matching nothing exits 1 with what it
     nearly matched; nothing to read is NOT RUN (exit 3) - never "no such
     type".
  6. Years collapse only when they are consecutive: 2091, 2092 and 2094
     must never print as 2091-2094.

WHY FIXTURES AND NOT THE CACHE
------------------------------
The real cache is 264 MB of Autodesk's assemblies fetched from NuGet, is
gitignored, and CI's test job never fetches it. Two tiny libraries built
here make every claim above checkable anywhere a .NET SDK is. When the cache
IS present, section 7 also reads the one change this project was caught by,
ElementId.IntegerValue, off the real releases; when it is not, that section
says it did not run and proves nothing either way.

The fixtures use a namespace of their own. `check-structure.py` refuses the
Revit vendor namespace outside revit/ and tools/, in fixtures too.

IT EXITS 3 WHEN .NET IS ABSENT, and that is NOT a pass - nothing was checked.
"""

import io
import os
import re
import shutil
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOL = os.path.join(ROOT, "tools", "api-surface")
READER = os.path.join(TOOL, "bin", "Debug", "ApiSurface.dll")
CACHE = os.path.join(TOOL, ".assemblies")
COULD_NOT_RUN = 3

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


OLDER = """
namespace Fixture.Api
{
    public enum Mode { First = 0, Second = 1 }

    public class Widget
    {
        public Widget(string name) { }
        public int Count(string name) { return 0; }
        public double Size { get; set; }
        [System.Obsolete("Use Size instead.")]
        public int OldSize { get { return 0; } }
        public static bool TryGet(string key, out int value) { value = 0; return false; }
        public string Label(string text, int times = 2) { return text; }
    }
}

namespace Fixture.Other
{
    public enum Mode { Only = 0 }
}
"""

# Count gains a parameter and a wider return type, Size loses its public
# setter, OldSize goes, Retire arrives already marked as an ERROR, and Mode
# gains a value. Everything else is identical, and must read as one line.
NEWER = """
namespace Fixture.Api
{
    public enum Mode { First = 0, Second = 1, Third = 2 }

    public class Widget
    {
        public Widget(string name) { }
        public long Count(string name, bool deep) { return 0; }
        public double Size { get; private set; }
        public static bool TryGet(string key, out int value) { value = 0; return false; }
        public string Label(string text, int times = 2) { return text; }
        [System.Obsolete("Gone next release.", true)]
        public void Retire() { }
    }
}

namespace Fixture.Other
{
    public enum Mode { Only = 0 }
}
"""

# One release with the older library, three with the newer - and a gap at
# 2093, so the collapsing rule has something to get wrong.
RELEASES = [("2090", OLDER), ("2091", NEWER), ("2092", NEWER), ("2094", NEWER)]

ROW = re.compile(r"^  (\d{4}(?:(?:-|, )\d{4})*)\s+"
                 r"(ctor|property|method|operator|field|value|event|type|\?)\s+(.*)$")
MARK = re.compile(r"^\s+OBSOLETE in (.+?): (.*)$")


def _asked(what):
    """`dotnet <what>`, or None when dotnet cannot answer at all."""
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
    """The newest framework an installed SDK can build AND a runtime can run.

    The same rule tests/test_binding_note.py gives its reasons for: the
    fixtures are BUILT, so they need an SDK that can target them, and the
    reader is RUN, so it needs a runtime. None when no pair meets.
    """
    runtimes = _majors(_asked("--list-runtimes"), "Microsoft.NETCore.App")
    sdks = _majors(_asked("--list-sdks"))
    if not runtimes or not sdks:
        return None
    usable = [m for m in runtimes if m <= max(sdks)]
    return "net%d.0" % max(usable) if usable else None


def _run(command, cwd=None):
    done = subprocess.run(command, cwd=cwd, stdout=subprocess.PIPE,
                          stderr=subprocess.STDOUT)
    return done.returncode, done.stdout.decode("utf-8", "replace")


def _fixture(yard, tfm, release, source):
    """One 'release': a library built from `source`, alone in its folder."""
    project = os.path.join(yard, "src-" + release)
    os.makedirs(project)
    with io.open(os.path.join(project, "Fixture.Api.csproj"), "w",
                 encoding="utf-8") as handle:
        handle.write(
            '<Project Sdk="Microsoft.NET.Sdk">\n'
            "  <PropertyGroup>\n"
            "    <TargetFramework>%s</TargetFramework>\n"
            "    <AssemblyName>Fixture.Api</AssemblyName>\n"
            "    <ImplicitUsings>disable</ImplicitUsings>\n"
            "    <Nullable>disable</Nullable>\n"
            "  </PropertyGroup>\n"
            "</Project>\n" % tfm)
    with io.open(os.path.join(project, "Fixture.cs"), "w",
                 encoding="utf-8") as handle:
        handle.write(source)

    out = os.path.join(yard, "out-" + release)
    code, said = _run(["dotnet", "build", project, "-o", out, "--nologo",
                       "-v", "q"])
    if code != 0:
        return None, said

    # ONLY the library. A build folder also holds a .deps.json and a .pdb,
    # and the reader reads every .dll in the folder it is given.
    folder = os.path.join(yard, release)
    os.makedirs(folder)
    shutil.copy(os.path.join(out, "Fixture.Api.dll"), folder)
    return folder, said


def rows(said):
    """[(span, kind, text, [(marked span, message)])] from the reader's answer."""
    out = []
    for line in said.splitlines():
        row = ROW.match(line)
        if row:
            out.append([row.group(1), row.group(2), row.group(3).rstrip(), []])
            continue
        mark = MARK.match(line)
        if mark and out:
            out[-1][3].append((mark.group(1), mark.group(2)))
    return out


def find(found, text):
    """The rows whose text is exactly `text`."""
    return [row for row in found if row[2] == text]


def main():
    tfm = _tfm()
    if tfm is None:
        print("COULD NOT RUN - no framework here that an installed SDK can")
        print("  build AND an installed runtime can run. This suite builds")
        print("  its fixtures and the reader, then runs the reader.")
        print("  Linux:   apt-get install -y dotnet-sdk-10.0")
        print("  This is exit 3, which is NOT a pass: nothing was checked.")
        return COULD_NOT_RUN

    code, said = _run(["dotnet", "build", TOOL, "--nologo", "-v", "q"])
    if code != 0 or not os.path.isfile(READER):
        print("FAILED - tools/api-surface did not build.")
        for line in said.splitlines()[-25:]:
            print("    %s" % line)
        return 1

    yard = tempfile.mkdtemp(prefix="heron-api-members-")
    try:
        folders = []
        for release, source in RELEASES:
            folder, said = _fixture(yard, tfm, release, source)
            if folder is None:
                print("FAILED - the %s fixture did not build for %s." % (release, tfm))
                for line in said.splitlines()[-25:]:
                    print("    %s" % line)
                return 1
            folders.append(folder)

        def ask(*query):
            return _run(["dotnet", READER, "--members"] + list(query) + folders)

        print("\n1. a changed signature is two lines, each with its own releases")
        code, said = ask("Fixture.Api.Widget")
        check(code == 0, "a type that exists answers exit 0 (%d)" % code)
        found = rows(said)
        older = find(found, "int Count(string name)")
        newer = find(found, "long Count(string name, bool deep)")
        check(len(older) == 1 and older[0][0] == "2090",
              "`int Count(string name)` is 2090's alone - %s"
              % [row[0] for row in older])
        check(len(newer) == 1 and newer[0][0] == "2091-2092, 2094",
              "`long Count(string name, bool deep)` is 2091-2092, 2094 - %s"
              % [row[0] for row in newer])
        check(bool(older and newer) and all(row[1] == "method" for row in older + newer),
              "and both are called methods")

        print("\n2. get and set are read, so a setter that went away is its own line")
        both = find(found, "double Size { get; set; }")
        read_only = find(found, "double Size { get; }")
        check(len(both) == 1 and both[0][0] == "2090",
              "`{ get; set; }` on 2090 - %s" % [row[0] for row in both])
        check(len(read_only) == 1 and read_only[0][0] == "2091-2092, 2094",
              "`{ get; }` after the public setter went - %s"
              % [row[0] for row in read_only])

        print("\n3. [Obsolete] is read, with its message, only where it is marked")
        old_size = find(found, "int OldSize { get; }")
        check(len(old_size) == 1 and old_size[0][0] == "2090",
              "OldSize is on 2090 and nowhere after")
        check(old_size and old_size[0][3] == [("2090", '"Use Size instead."')],
              "and carries its mark and message: %s"
              % (old_size[0][3] if old_size else None))
        retire = find(found, "void Retire()")
        check(retire and retire[0][3] == [("2091-2092, 2094",
                                           'ERROR: "Gone next release."')],
              "an error-level mark says ERROR: %s"
              % (retire[0][3] if retire else None))
        unmarked = [row[2] for row in found
                    if row[3] and row[2] not in ("int OldSize { get; }",
                                                  "void Retire()")]
        check(bool(found) and not unmarked,
              "and nothing unmarked is reported obsolete: %s" % unmarked)

        print("\n4. parameters, static, defaults and enums")
        check(find(found, "static bool TryGet(string key, out int value)")
              and find(found, "static bool TryGet(string key, out int value)")[0][0]
              == "2090-2092, 2094",
              "`static` and `out` are kept, and an unchanged member is ONE "
              "line across all four")
        check(bool(find(found, "string Label(string text, int times = 2)")),
              "a default value is printed")
        check(bool(find(found, "new Widget(string name)")),
              "a constructor reads as `new Widget(...)`")
        code, said = ask("Fixture.Api.Mode")
        values = [(row[2], row[0]) for row in rows(said) if row[1] == "value"]
        check([text for text, _ in values]
              == ["First = 0", "Second = 1", "Third = 2"],
              "enum values come in number order: %s" % values)
        check(dict(values).get("Third = 2") == "2091-2092, 2094",
              "and the late one says when it arrived")

        print("\n5. what it refuses, and how")
        code, said = ask("Widget.Count")
        narrowed = rows(said)
        check(code == 0 and len(narrowed) == 2
              and all("Count(" in row[2] for row in narrowed),
              "Type.Member prints that member alone (%d line(s))" % len(narrowed))
        code, said = ask("Mode")
        check(code == 2, "a short name meaning two types exits 2 (%d)" % code)
        check("Fixture.Api.Mode" in said and "Fixture.Other.Mode" in said,
              "and names both rather than picking one")
        code, said = ask("Widge")
        check(code == 1, "a name matching nothing exits 1 (%d)" % code)
        check("Fixture.Api.Widget" in said,
              "and offers what it nearly matched")
        empty = os.path.join(yard, "nothing-here")
        os.makedirs(empty)
        code, said = _run(["dotnet", READER, "--members", "Widget", empty])
        check(code == COULD_NOT_RUN and "NOT RUN" in said,
              "a folder with nothing to read is NOT RUN, exit 3 (%d) - "
              "never 'no such type'" % code)

        print("\n6. years collapse only when they are consecutive")
        spans = set(row[0] for row in found)
        check("2090-2094" not in spans and "2091-2094" not in spans,
              "no line claims 2093, which was never read: %s" % sorted(spans))
    finally:
        shutil.rmtree(yard, ignore_errors=True)

    print("\n7. the real releases, when they are cached here")
    cached = sorted(name for name in (os.listdir(CACHE) if os.path.isdir(CACHE) else [])
                    if name.isdigit())
    if not {"2023", "2024", "2025", "2026"} <= set(cached):
        print("  NOT RUN - tools/api-surface/.assemblies does not hold 2023 to 2026")
        print("  here (`python tools/check-api-surface.py` fetches them). Sections")
        print("  1 to 6 do not need it; this one proves nothing either way.")
    else:
        code, said = _run(["dotnet", READER, "--members", "ElementId.IntegerValue"])
        real = rows(said)
        check(code == 0 and len(real) == 1 and real[0][2] == "int IntegerValue { get; }",
              "ElementId.IntegerValue reads as `int IntegerValue { get; }`")
        span = real[0][0] if real else ""
        check(span.endswith("2025") and "2026" not in span,
              "and stops at 2025, which is where the compiler put it: %s" % span)
        check(real and real[0][3] and real[0][3][0][0].startswith("2024"),
              "and is marked obsolete from 2024 - the two years of warning a "
              "name-only reader cannot see: %s" % (real[0][3] if real else None))

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    --members reads signatures, setters and [Obsolete], release by release")
    return 0


if __name__ == "__main__":
    sys.exit(main())
