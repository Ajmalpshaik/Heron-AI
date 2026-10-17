#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The rows NEEDS-CHECKING Group DA hands CREATE_FAMILY_FORMS, read by the
fragment's own reader before anybody carries them to a Revit.

    python tests/test_family_forms_rows.py

WHY THIS EXISTS
---------------
Group DA's runs need a Revit PC, which is the slow and expensive place to find
a typo in a profile. Every row the group's commands send - the rows files in
`tools/jobs/family-forms-*.rows.txt`, DA1's job values, and the single-form
calls DA4 writes out in `docs/needs-checking/group-da.md` - is read here by the
code that will read it in Revit, and each refusal the group arranges is read
for the words its row says it must answer with.

THE FRAGMENT'S OWN TEXT, NOT A SECOND READER
--------------------------------------------
`brain/fragments/create-family-forms/impl/any/fragment.cs` reads and checks
every row before it builds anything, and that half touches the model only for
the plane names, the materials and Revit's shortest curve. So the source is
cut at its `---- building, then reading back` line and compiled inside a
method whose parameters are its `needs`, the way
`tools/check-fragments-compile.py` wraps a fragment. A second implementation
written to test the first is how `binds:` came to be honoured by one half and
ignored by the other (FRAGMENT-ISSUES row 96); this has no copy of the rules.

WHAT STANDS IN FOR THE MODEL - and what this therefore assumes
--------------------------------------------------------------
A family holding the metric Generic Model template's three datums - the level
"Ref. Level" and the reference planes "Center (Left/Right)" and
"Center (Front/Back)", all at 0 - no family parameters and no materials, and
Revit's shortest curve as the reply in row 5b-400 carried it on Revit 2024
(0.7803686370625 mm). The stand-ins are a namespace of this file's own; no
vendor type is named (`tools/check-structure.py`). The temporary folder is the
process's own, set through TMPDIR to a scratch folder: once as one folder (the
office PC) and once as a GUID-named sub-folder (the owner's laptop, row 5b-430).

WHAT IT CANNOT SAY
------------------
Nothing past the reader. The build half - sketch planes, `NewExtrusion`,
`NewRevolution`, every read-back, the one undo entry - never runs here, so a
pass is NOT a proof and moves no row: Group DA is NEEDS REAL REVIT. And the
temporary-folder check runs on this system's paths, not Windows'.

IT EXITS 3 WHEN .NET IS ABSENT, and that is NOT a pass (tests/README.md).
"""

import io
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
import uuid

import yaml

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FRAGMENT = os.path.join(ROOT, "brain", "fragments", "create-family-forms", "impl", "any",
                        "fragment.cs")
JOBS = os.path.join(ROOT, "tools", "jobs")
DA1_JOB = os.path.join(JOBS, "family-forms-many-2026-10-11.yaml")
TEN = os.path.join(JOBS, "family-forms-ten-2026-10-17.rows.txt")
SHALLOW = os.path.join(JOBS, "family-forms-shallow-row-2026-10-17.rows.txt")
BLEND = os.path.join(JOBS, "family-forms-blend-row-2026-10-17.rows.txt")
PUMP_SET = os.path.join(JOBS, "family-forms-pump-set-2026-10-17.rows.txt")
ROW7 = os.path.join(JOBS, "family-forms-row7-plane-2026-10-17.rows.txt")
GROUP = os.path.join(ROOT, "docs", "needs-checking", "group-da.md")
CUT = "// ---- building, then reading back"
COULD_NOT_RUN = 3
# A command line as group-da.md writes one to be copied - never a sentence that
# only mentions a tool.
COMMAND = re.compile(r"HERON_CLIENT_ID=\S+ python mcp/client/heron_bridge_client\.py fragment ")

# Revit 2024's ShortCurveTolerance in millimetres, read off the reply in
# FRAGMENT-ISSUES row 5b-400 ("shortest 0.7803686370625"). A stand-in for the
# model, never a number the fragment holds.
SHORTEST_MM = "0.7803686370625"

FAILURES = []


def check(ok, what):
    print("  %s  %s" % ("ok  " if ok else "FAIL", what))
    if not ok:
        FAILURES.append(what)
    return ok


def _asked(what):
    """`dotnet <what>`, or None when dotnet cannot answer at all."""
    try:
        out = subprocess.check_output(["dotnet", what], stderr=subprocess.STDOUT)
    except Exception:                                      # noqa: BLE001
        return None
    return out.decode("utf-8", "replace")


def _majors(said, prefix=None):
    """The MAJOR version of every line dotnet listed, highest last."""
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
    """The newest framework an installed SDK can build AND a runtime can run -
    tests/test_binding_note.py's rule, for the reasons it gives."""
    runtimes = _majors(_asked("--list-runtimes"), "Microsoft.NETCore.App")
    sdks = _majors(_asked("--list-sdks"))
    if not runtimes or not sdks:
        return None
    usable = [m for m in runtimes if m <= max(sdks)]
    return "net%d.0" % max(usable) if usable else None


STAND_INS = r"""
namespace HeronFormsRows.Stand
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public enum BuiltInParameter { DATUM_TEXT }

    public class XYZ
    {
        public XYZ(double x, double y, double z) { X = x; Y = y; Z = z; }
        public double X { get; private set; }
        public double Y { get; private set; }
        public double Z { get; private set; }
    }

    public class Plane
    {
        public Plane(XYZ origin) { Origin = origin; }
        public XYZ Origin { get; private set; }
    }

    public class Parameter
    {
        private readonly string _text;
        public Parameter(string text) { _text = text; }
        public string AsString() { return _text; }
    }

    public class Element { public string Name { get; set; } }

    public class ReferencePlane : Element
    {
        public XYZ Normal { get; set; }
        public XYZ Origin { get; set; }
        public Parameter get_Parameter(BuiltInParameter which)
        {
            return which == BuiltInParameter.DATUM_TEXT ? new Parameter(Name) : null;
        }
        public Plane GetPlane() { return new Plane(Origin); }
    }

    public class Level : Element { public double Elevation { get; set; } }

    public class Material : Element { }

    public class Definition { public string Name { get; set; } }

    public class FamilyParameter
    {
        public Definition Definition { get; set; }
        public bool IsReporting { get; set; }
    }

    public class FamilyManager
    {
        public FamilyManager() { Parameters = new List<FamilyParameter>(); }
        public List<FamilyParameter> Parameters { get; private set; }
    }

    public class Application { public double ShortCurveTolerance { get; set; } }

    public class Document
    {
        public Document() { Elements = new List<Element>(); FamilyManager = new FamilyManager(); }
        public bool IsFamilyDocument { get; set; }
        public Application Application { get; set; }
        public FamilyManager FamilyManager { get; private set; }
        public List<Element> Elements { get; private set; }

        // The metric Generic Model template's three datums, all at 0.
        public static Document Scratch(bool project, double shortestMm)
        {
            var doc = new Document
            {
                IsFamilyDocument = !project,
                Application = new Application { ShortCurveTolerance = shortestMm / 304.8 },
            };
            if (project) return doc;
            doc.Elements.Add(new Level { Name = "Ref. Level", Elevation = 0 });
            doc.Elements.Add(new ReferencePlane { Name = "Center (Left/Right)",
                Normal = new XYZ(1, 0, 0), Origin = new XYZ(0, 0, 0) });
            doc.Elements.Add(new ReferencePlane { Name = "Center (Front/Back)",
                Normal = new XYZ(0, 1, 0), Origin = new XYZ(0, 0, 0) });
            return doc;
        }
    }

    public class FilteredElementCollector : IEnumerable<Element>
    {
        private readonly IEnumerable<Element> _from;
        public FilteredElementCollector(Document doc) { _from = doc.Elements; }
        private FilteredElementCollector(IEnumerable<Element> from) { _from = from; }
        public FilteredElementCollector OfClass(Type type)
        {
            return new FilteredElementCollector(_from.Where(e => type.IsInstanceOfType(e)).ToList());
        }
        public IEnumerator<Element> GetEnumerator() { return _from.GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
    }
}
"""

HARNESS = r"""
namespace HeronFormsRows
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using HeronFormsRows.Stand;

    public static class Reader
    {
        public static string Read(Document doc, string forms)
        {
// ---- the fragment's own text, up to its build half ----
%(snippet)s
// ---- what it decided, for the test to read ----
            var heronRows = new List<object>();
            for (var heronRow = 0; heronRow < rowKind.Count; heronRow++)
                heronRows.Add(new
                {
                    kind = rowKind[heronRow],
                    plane = rowPlane[heronRow].Item4,
                    loops = rowLoops[heronRow].Select(l => l.Select(s => s.Select(v => Math.Round(v, 6))
                        .ToArray()).ToArray()).ToArray(),
                    start = rowFrom[heronRow],
                    end = rowTo[heronRow],
                    axis = rowAxis[heronRow],
                    solid = rowSolid[heronRow],
                    byCategory = rowByCategory[heronRow],
                    shows = rowShows[heronRow],
                });
            return System.Text.Json.JsonSerializer.Serialize(new
            {
                refused = refused,
                notAFamily = notAFamily,
                rowCount = rowCount,
                rows = heronRows,
            });
        }
    }

    public static class Program
    {
        public static int Main(string[] args)
        {
            var shortest = double.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture);
            using (var cases = System.Text.Json.JsonDocument.Parse(File.ReadAllText(args[0])))
            {
                foreach (var one in cases.RootElement.EnumerateArray())
                {
                    var doc = Document.Scratch(one.GetProperty("project").GetBoolean(), shortest);
                    string said;
                    try { said = Reader.Read(doc, one.GetProperty("forms").GetString()); }
                    catch (Exception failure)
                    {
                        said = System.Text.Json.JsonSerializer.Serialize(new { error = failure.ToString() });
                    }
                    Console.WriteLine(one.GetProperty("label").GetString() + "\t" + said);
                }
            }
            return 0;
        }
    }
}
"""

PROJECT = """<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>%s</TargetFramework>
    <LangVersion>latest</LangVersion>
    <AssemblyName>HeronFormsRows</AssemblyName>
    <NoWarn>CS0168;CS0219</NoWarn>
  </PropertyGroup>
</Project>
"""


def reader_text():
    """The fragment's source up to its build half. None, with a failure, when
    the line it is cut at has gone - the cut is this file's one assumption
    about the source, so it is checked rather than trusted."""
    text = io.open(FRAGMENT, encoding="utf-8").read()
    at = text.find(CUT)
    if not check(at > 0, "fragment.cs still has its `%s` line to cut at" % CUT):
        return None
    return text[:at]


def build(work, tfm):
    """Build the reader host in `work`. The dll's path, or None."""
    snippet = reader_text()
    if snippet is None:
        return None
    os.makedirs(work, exist_ok=True)
    with io.open(os.path.join(work, "Program.cs"), "w", encoding="utf-8") as fh:
        fh.write(STAND_INS + HARNESS.replace("%(snippet)s", snippet))
    with io.open(os.path.join(work, "HeronFormsRows.csproj"), "w", encoding="utf-8") as fh:
        fh.write(PROJECT % tfm)
    out = os.path.join(work, "out")
    built = subprocess.run(["dotnet", "build", work, "-o", out, "--nologo", "-v", "q"],
                           stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    if not check(built.returncode == 0, "the fragment's reader builds for %s" % tfm):
        for line in (built.stdout or b"").decode("utf-8", "replace").splitlines()[-25:]:
            print("    %s" % line)
        return None
    return os.path.join(out, "HeronFormsRows.dll")


def read(dll, cases, temporary):
    """Run `cases` - (label, forms, project) - with the temporary folder set to
    `temporary`. {label: what the reader decided}."""
    path = os.path.join(os.path.dirname(dll), "cases-%s.json" % uuid.uuid4().hex)
    with io.open(path, "w", encoding="utf-8") as fh:
        json.dump([{"label": l, "forms": f, "project": p} for l, f, p in cases], fh)
    env = dict(os.environ, TMPDIR=temporary)
    ran = subprocess.run(["dotnet", dll, path, SHORTEST_MM], stdout=subprocess.PIPE,
                         stderr=subprocess.STDOUT, env=env)
    said = (ran.stdout or b"").decode("utf-8", "replace")
    out = {}
    for line in said.splitlines():
        label, _, answer = line.partition("\t")
        try:
            out[label] = json.loads(answer)
        except ValueError:
            out[label] = {"error": line}
    check(ran.returncode == 0 and len(out) == len(cases),
          "the reader answered every case (exit %d)" % ran.returncode)
    return out


def rows_named(refusal):
    """The row numbers a refusal names."""
    return sorted(set(int(n) for n in re.findall(r"\bRow (\d+)\b", refusal or "")))


def da1_values():
    """DA1's positive and negative `forms`, as its job file sends them."""
    job = yaml.safe_load(io.open(DA1_JOB, encoding="utf-8"))["jobs"][0]
    return job["set"]["forms"], job["negative-set"]["forms"]


def da4_rows():
    """DA4's single-form calls in group-da.md, each turned back into a row:
    `--set` values joined as a row of CREATE_FAMILY_FORMS takes them."""
    text = io.open(GROUP, encoding="utf-8").read()
    rows = []
    for line in text.splitlines():
        if not COMMAND.match(line):
            continue
        found = re.search(r" create-family-(extrusion|revolution) (.*)$", line)
        if not found:
            continue
        values = [a or b for a, b in re.findall(r'--set (?:"([^"]*)"|(\S+))', found.group(2))]
        kind = "extrusion" if found.group(1) == "extrusion" else "revolve"
        rows.append("kind=%s %s" % (kind, " ".join(values)))
    return rows


def geometry(row):
    """What a single-form call carries: everything but material and visibility."""
    return {k: row[k] for k in ("kind", "plane", "loops", "start", "end", "axis", "solid")}


def main():
    print("The rows Group DA sends CREATE_FAMILY_FORMS, read by its own reader")
    for path in (TEN, SHALLOW, BLEND, PUMP_SET, ROW7, DA1_JOB, GROUP):
        check(os.path.isfile(path), "%s is there" % os.path.relpath(path, ROOT))
    if FAILURES:
        return 1

    tfm = _tfm()
    if tfm is None:
        print("  NOT RUN - no framework here that an installed SDK can build AND")
        print("  a runtime can run. Linux: apt-get install -y dotnet-sdk-10.0")
        return COULD_NOT_RUN

    work = tempfile.mkdtemp(prefix="heron-forms-rows-")
    try:
        return run(work, tfm)
    finally:
        shutil.rmtree(work, ignore_errors=True)


def run(work, tfm):
    dll = build(os.path.join(work, "host"), tfm)
    if dll is None:
        return 1

    # THE TWO PCs. The office one: the temporary folder is the user's own. The
    # laptop: a GUID-named sub-folder of it, and the chat writes to the
    # parent's heron-work (row 5b-430).
    office = os.path.join(work, "office", "Temp") + os.sep
    laptop_parent = os.path.join(work, "laptop", "Temp")
    laptop = os.path.join(laptop_parent, str(uuid.uuid4())) + os.sep
    outside = os.path.join(work, "outside")
    places = {
        "office-ten": os.path.join(office, "heron-work", "da", "forms-ten.txt"),
        "office-shallow": os.path.join(office, "heron-work", "da", "forms-shallow.txt"),
        "office-blend": os.path.join(office, "heron-work", "da", "forms-blend.txt"),
        "office-pump": os.path.join(office, "heron-work", "da", "forms-pump-set.txt"),
        "office-row7": os.path.join(office, "heron-work", "da", "forms-row7.txt"),
        "outside": os.path.join(outside, "forms-ten.txt"),
        "laptop-ten": os.path.join(laptop_parent, "heron-work", "da", "forms-ten.txt"),
        "laptop-elsewhere": os.path.join(laptop_parent, "elsewhere", "rows.txt"),
        "laptop-workshop": os.path.join(laptop_parent, "heron-workshop", "rows.txt"),
    }
    sources = {"office-shallow": SHALLOW, "office-blend": BLEND, "office-pump": PUMP_SET,
               "office-row7": ROW7}
    for key, path in places.items():
        os.makedirs(os.path.dirname(path), exist_ok=True)
        shutil.copyfile(sources.get(key, TEN), path)
    os.makedirs(laptop, exist_ok=True)

    positive, negative = da1_values()
    singles = da4_rows()
    office_cases = [
        ("da1-positive", positive, False),
        ("da1-negative", negative, False),
        ("da2", places["office-ten"], False),
        ("da3-shallow", places["office-shallow"], False),
        ("da3-blend", places["office-blend"], False),
        ("da3-outside", places["outside"], False),
        ("da3-project", places["office-ten"], True),
        ("da5", places["office-pump"], False),
        ("da6-negative", places["office-row7"], False),
    ] + [("da4-%d" % (i + 1), row, False) for i, row in enumerate(singles)]
    laptop_cases = [
        ("da8-positive", places["laptop-ten"], False),
        ("da8-elsewhere", places["laptop-elsewhere"], False),
        ("da8-workshop", places["laptop-workshop"], False),
    ]
    said = read(dll, office_cases, office)
    said.update(read(dll, laptop_cases, laptop))
    for label, answer in said.items():
        if "error" in answer:
            check(False, "%s: the reader threw - %s" % (label, answer["error"][:300]))
    if FAILURES:
        return 1

    def clean(label):
        answer = said[label]
        return answer.get("refused") is None and not answer.get("notAFamily")

    print("\nDA1 - the job file's two values")
    check(clean("da1-positive") and said["da1-positive"]["rowCount"] == 10,
          "the positive reads as ten rows, none refused")
    refusal = said["da1-negative"]["refused"] or ""
    print("    refused: %s" % refusal)
    check(rows_named(refusal) == [7] and '"Ref Lvl"' in refusal,
          "the negative is refused naming row 7 and its plane, and no other row")

    print("\nDA2 - the ten rows as a file under heron-work")
    check(clean("da2"), "the file is read and every row reads clean")
    check(said["da2"]["rows"] == said["da1-positive"]["rows"],
          "the file's rows read exactly as DA1's value does - the comment lines dropped")

    print("\nDA3 - the four refusals")
    refusal = said["da3-shallow"]["refused"] or ""
    print("    0.5 mm:   %s" % refusal)
    check(rows_named(refusal) == [3] and "0.5 mm deep" in refusal and "Make it at least" in refusal,
          "row 3, 0.5 mm deep, refused naming its depth and the least that builds")
    refusal = said["da3-blend"]["refused"] or ""
    print("    blend:    %s" % refusal)
    check(rows_named(refusal) == [10] and 'kind is "blend"' in refusal
          and "an extrusion or a revolve" in refusal,
          "the blend row refused by number - a row is an extrusion or a revolve")
    refusal = said["da3-outside"]["refused"] or ""
    print("    outside:  %s" % refusal)
    check("is not in the temporary folder, %s" % office in refusal and "nor in" not in refusal,
          "a file outside the temporary folder is refused naming that folder")
    answer = said["da3-project"]
    print("    project:  %s" % answer["refused"])
    check(answer["notAFamily"] is True and answer["rowCount"] == 0,
          "a project in front is notAFamily, and no row is read")
    # NOTHING BUILT rests on one line of the source: the build half runs only
    # while `refused` is null. The rows that read clean are still listed, so
    # the guard is what is read, not the lists.
    after = io.open(FRAGMENT, encoding="utf-8").read().split(CUT, 1)[1]
    first = [line.strip() for line in after.splitlines()[1:] if line.strip()][:1]
    check(first == ["if (refused == null)"],
          "the build half opens with `if (refused == null)`, so a refused list builds nothing")

    print("\nDA4 - the single-form calls written in group-da.md")
    check(len(singles) == 10, "group-da.md writes out ten single-form calls (%d)" % len(singles))
    ten = said["da2"]["rows"]
    for i in range(min(len(singles), len(ten))):
        answer = said["da4-%d" % (i + 1)]
        check(clean("da4-%d" % (i + 1)) and len(answer["rows"]) == 1
              and geometry(answer["rows"][0]) == geometry(ten[i]),
              "call %d carries row %d's kind, plane, profile, extent and solid" % (i + 1, i + 1))

    print("\nDA5 - the pump-set list")
    answer = said["da5"]
    print("    rows: %d" % answer["rowCount"])
    most = int(re.search(r"var mostRows = (\d+);", io.open(FRAGMENT, encoding="utf-8").read())
               .group(1))
    if answer.get("refused"):
        print("    refused: %s" % answer["refused"])
    check(clean("da5"), "every row of the pump set reads clean")
    # The pump set it stands for was 107 forms (row 5b-378); "about 100".
    check(90 <= answer["rowCount"] <= min(120, most),
          "about the pump set's size, and inside the fragment's own limit of %d" % most)
    check(sum(1 for r in answer["rows"] if not r["solid"]) > 0
          and sum(1 for r in answer["rows"] if r["kind"] == "revolve") > 0,
          "it holds revolves and voids as well as solid extrusions")

    print("\nDA6 - DA1's negative as a file, for 2020 and 2027")
    check(said["da6-negative"]["refused"] == said["da1-negative"]["refused"]
          and said["da6-negative"]["rows"] == said["da1-negative"]["rows"],
          "the file is refused exactly as DA1's negative value is")

    print("\nDA8 - the laptop's GUID-named temporary folder")
    check(clean("da8-positive") and said["da8-positive"]["rows"] == ten,
          "the parent's heron-work is read on the first call")
    shared = os.path.join(laptop_parent, "heron-work") + os.sep
    for label in ("da8-elsewhere", "da8-workshop"):
        refusal = said[label]["refused"] or ""
        print("    %s: %s" % (label[4:], refusal))
        check(laptop in refusal and ("nor in " + shared) in refusal and said[label]["rowCount"] == 0,
              "%s refused naming both folders read, no row read" % label[4:])

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    every row Group DA sends reads as its row says - the reader only;")
    print("        nothing here has built a form. Group DA is NEEDS REAL REVIT.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
