#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The two keynote-file cards can each come back EMPTY in their D-30 negative.

    python tests/test_keynote_file_cards.py

WHY IT EXISTS - FRAGMENT-ISSUES ROWS 5b-1647, 5b-1737 AND 5b-1738
------------------------------------------------------------------
LOAD_KEYNOTE_FILE (FRG-VIEW-169) and REPORT_KEYNOTE_FILE (FRG-VIEW-170) are
DRAFT, and field report Ajmalpshaik/Heron-AI#854 (Revit 2024, `villa 2`) asked
for them proved: 47 keynote tags print blank until a project keynote file can
be loaded from chat. Read before that sitting, both would have failed it with
nothing wrong in Revit:

  * 5b-1647, measured on Revit 2020: version 1's only `role: result` was
    `loadResult`, a sentence filled either way - "loaded" or "nothing loaded -
    There is no file at ..." - so its negative read as content.
  * 5b-1737, read: the read's job file ran its negative on a second project
    that names a keynote file too, so `keynoteFile` and `tableEntries` were
    full in the negative by the judge's own rule.
  * 5b-1738, read: "Revit reports the file as ..., not the path typed" was a
    string compare, so C:/A/b.txt and C:\\A\\b.txt read as two files.

WHAT IT CHECKS
  1. LOAD_KEYNOTE_FILE's contract: `loadedFile` is its string result,
     `loadResult` is accounting, and the version is above 1.
  2. ITS C#, comments removed: `loadedFile` is only ever set to "" or to
     `fileAfter`, and only under a test that nothing was refused.
  3. THE JUDGE, on the real contract: row 5b-1647's two legs as version 2
     would leave them - the negative reads EMPTY (heron_validate.looks_empty),
     the positive does not. On version 1's contract the same negative is
     content, so this goes red there.
  4. THE PATH COMPARE goes through Path.GetFullPath, never a bare
     string.Equals of the typed path and Revit's.
  5. REPORT_KEYNOTE_FILE: a family document is answered before the keynote
     table is read, and nothing is assigned to its results on that branch.
  6. THE JOB FILE: the read's negative is a family, not a project naming a
     file, and the load is judged on `loadedFile`.

WHAT IT CANNOT DO
Run either card. That Revit loads, reloads and reads back as the cards say is
NEEDS REAL REVIT - NEEDS-CHECKING Group LZ, the sitting laid out there.
"""

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "tests", "support"))
import scratch_audit                                            # noqa: E402

scratch_audit.plant()

import yaml                                                     # noqa: E402
import heron_fragment as HF                                     # noqa: E402
import heron_validate as HV                                     # noqa: E402

FRAGMENTS = os.path.join(ROOT, "brain", "fragments")
LOAD_DIR = os.path.join(FRAGMENTS, "load-keynote-file")
REPORT_DIR = os.path.join(FRAGMENTS, "report-keynote-file")
JOB = os.path.join(ROOT, "tools", "jobs", "keynote-file-lz-2026-11-02.yaml")

failures = []


def check(ok, what):
    print(("  ok    " if ok else "  FAIL  ") + what)
    if not ok:
        failures.append(what)


def read(path):
    with io.open(path, encoding="utf-8") as handle:
        return handle.read()


def code_of(folder):
    """The fragment's C# with // and /* */ comments removed, so a rule
    described in words does not count."""
    text = read(os.path.join(folder, "impl", "any", "fragment.cs"))
    text = re.sub(r"/\*.*?\*/", "", text, flags=re.S)
    lines = []
    for line in text.splitlines():
        # Strip a // comment that is not inside a string literal.
        out, in_string, i = [], False, 0
        while i < len(line):
            c = line[i]
            if c == '"' and (i == 0 or line[i - 1] != "\\"):
                in_string = not in_string
            if not in_string and line.startswith("//", i):
                break
            out.append(c)
            i += 1
        lines.append("".join(out))
    return "\n".join(lines)


def provides_of(frag):
    return dict((d.get("name"), d) for d in (frag.provides() or [])
                if isinstance(d, dict) and d.get("name"))


# ---------------------------------------------------------------------------
print("1. LOAD_KEYNOTE_FILE's contract")
load = HF.load(LOAD_DIR)
provides = provides_of(load)
loaded_file = provides.get("loadedFile") or {}
check(loaded_file.get("role") == "result"
      and str(loaded_file.get("type")).strip().lower() == "string",
      "`loadedFile` is declared `type: string`, `role: result`")
check((provides.get("loadResult") or {}).get("role") == "accounting",
      "`loadResult` - filled either way - is `role: accounting` (row 5b-1647)")
results = [n for n, d in provides.items() if d.get("role") == "result"]
check(results == ["loadedFile"], "`loadedFile` is its only result (got %s)" % results)
try:
    version = int(load.data.get("version"))
except (TypeError, ValueError):
    version = 0
check(version >= 2, "the card's version is above 1 (got %s)" % load.data.get("version"))

# ---------------------------------------------------------------------------
print("2. LOAD_KEYNOTE_FILE's C# sets `loadedFile` only from a load that held")
code = code_of(LOAD_DIR)
assigned = re.findall(r"\bloadedFile\s*=(?!=)\s*([^;]+);", code)
check(len(assigned) >= 2, "`loadedFile` is declared and set (%d assignment(s))" % len(assigned))
check(all(rhs.strip() in ('""', "fileAfter") for rhs in assigned),
      "every assignment is \"\" or fileAfter (got %s)" % [a.strip() for a in assigned])
guard = re.search(r"var\s+loadDone\s*=\s*([^;]+);", code)
check(guard is not None and "refused.Count == 0" in guard.group(1),
      "it is set only under a test that nothing was refused")
check(re.search(r"if\s*\(\s*loadDone\s*\)\s*loadedFile\s*=\s*fileAfter\s*;", code) is not None,
      "`loadedFile = fileAfter` sits behind that test")
check(not re.search(r"loadedFile\s*=\s*\"nothing", code),
      "no refusal sentence is ever written into `loadedFile`")

# ---------------------------------------------------------------------------
print("3. The judge reads row 5b-1647's negative as EMPTY, its positive as found")
# The legs as the 2020 run left them (row 5b-1647), with version 2's result
# beside them: on the refusal it is empty, on the load it is the file read back.
negative = {"ok": True, "provides": {
    "loadedFile": "",
    "loadResult": "nothing loaded - There is no file at \"C:\\none.txt\" that this PC can see - the keynote file is unchanged.",
    "fileBefore": "RevitKeynotes_Metric.txt (Content), Revit: Loaded",
    "fileAfter": "RevitKeynotes_Metric.txt (Content), Revit: Loaded",
    "entriesBefore": 3840, "entriesAfter": 3840,
    "refused": "1 item(s) [There is no file at ...]",
    "findings": "1 item(s) [Keynote file before: ...]"}}
positive = {"ok": True, "provides": dict(negative["provides"], **{
    "loadedFile": "C:\\ProgramData\\Autodesk\\RVT 2020\\Libraries\\US Metric\\RevitKeynotes_Metric.txt (Absolute), Revit: Loaded",
    "loadResult": "loaded", "refused": "0 item(s)"})}
roles, names = HV.contract_of(load)
strings = HV.string_results(load)
check(HV.looks_empty(negative, roles, names, strings),
      "the negative - a path with no file - reads as nothing found")
check(not HV.looks_empty(positive, roles, names, strings),
      "the positive - the file loaded - reads as something found")

# ---------------------------------------------------------------------------
print("4. The \"not the path typed\" line compares full paths (row 5b-1738)")
check("GetFullPath" in code, "Path.GetFullPath is used")
check(not re.search(r"string\.Equals\(\s*currentAbsolute\s*,\s*wanted\b", code),
      "no bare string.Equals(currentAbsolute, wanted, ...) is left")

# ---------------------------------------------------------------------------
print("5. REPORT_KEYNOTE_FILE answers a family before reading the table (row 5b-1737)")
report = HF.load(REPORT_DIR)
rcode = code_of(REPORT_DIR)
at_family = rcode.find("IsFamilyDocument")
at_table = rcode.find("GetKeynoteTable")
check(at_family != -1, "the C# asks IsFamilyDocument")
check(at_family != -1 and at_table != -1 and at_family < at_table,
      "it asks before KeynoteTable.GetKeynoteTable")
branch = re.search(r"if\s*\(\s*isFamily\s*\)\s*\{(.*?)\}", rcode, flags=re.S)
check(branch is not None and "keynoteFile" not in branch.group(1)
      and "tableEntries" not in branch.group(1),
      "the family branch leaves keynoteFile and tableEntries as they started (empty, 0)")
try:
    rversion = int(report.data.get("version"))
except (TypeError, ValueError):
    rversion = 0
check(rversion >= 2, "the card's version is above 1 (got %s)" % report.data.get("version"))

# ---------------------------------------------------------------------------
print("6. The job file arranges negatives that can be empty")
jobs = (yaml.safe_load(read(JOB)) or {}).get("jobs") or []
by_name = dict((j.get("fragment"), j) for j in jobs if isinstance(j, dict))
read_job = by_name.get("report-keynote-file") or {}
check(read_job.get("negative-in") == "Family1",
      "the read's negative is the family `Family1`, not a project naming a file (got %r)"
      % read_job.get("negative-in"))
load_job = by_name.get("load-keynote-file") or {}
check(load_job.get("expect") == "loadedFile",
      "the load is judged on `loadedFile` (got %r)" % load_job.get("expect"))
check(load_job.get("in") == "keynote-lz" and read_job.get("in") == "keynote-lz",
      "both are pinned to the scratch project `keynote-lz`")

print()
if failures:
    print("%d check(s) failed." % len(failures))
    sys.exit(1)
print("Every check passed. Neither card has run in Revit since: Group LZ.")
