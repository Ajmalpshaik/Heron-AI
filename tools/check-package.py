# Heron-Agent:  HERON-DEV-REL-018, HERON-INS-RVT-003
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Would the thing we deliver actually install and load?

    python tools/check-package.py

WHY A GREEN BUILD IS NOT AN ANSWER TO THAT
-------------------------------------------
Every other gate in this folder asks about the source. None of them reads
Heron.addin - not one, before this file - and the manifest is what Revit reads
first. Rename the entry class and every test still passes, every release still
compiles, and Revit says "cannot run the external application Heron AI" with
nothing to say why. That failure reaches a modeller and reaches nothing else.

So this asks the delivery questions that can be asked honestly on a machine
with no Windows, no Revit and no compiler:

   1  the manifest parses, and carries every element Revit requires
   2  AddInId is a GUID
   3  FullClassName names a class that EXISTS and is an IExternalApplication
   4  Assembly matches the assembly the project actually builds
   5  the literal line tools/deploy-addin.ps1 rewrites is still there to rewrite
   6  no <ManifestSettings> - it arrived at Revit 2026 and CRASHES 2025 and
      older, and Heron ships one manifest to all eight releases
   7  nothing redistributes Autodesk's assemblies
   8  the install path is per-user on every release - the no-admin promise
   9  every supported release has a runtime row, and a compile symbol
  10  the ribbon's icons exist

WHAT IT CANNOT ASK, AND DOES NOT PRETEND TO
--------------------------------------------
Whether the install works. Whether an upgrade from the previous version keeps a
user's settings. Whether Revit loads the add-in. Whether a rollback recovers.
Those need Windows, a Revit and a previous version installed, and they are
listed at the end of the run as what is still owed rather than quietly left
out. docs/07 owns the promise; this file owns only the part of it a script can
hold Heron to.

Exit 0 = every offline question answered yes.
Exit 1 = one of them answered no, and the answer names the file.
"""

import importlib.util
import io
import os
import re
import sys
import xml.etree.ElementTree as ET

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

MANIFEST = "revit/Heron.Revit.Addin/Heron.addin"
ADDIN_PROJ = "revit/Heron.Revit.Addin/Heron.Revit.Addin.csproj"
PROPS = "Directory.Build.props"
DEPLOY = "tools/deploy-addin.ps1"
SETUP = "tools/setup.ps1"
ADDIN_SRC = "revit/Heron.Revit.Addin"

REQUIRED_ELEMENTS = ("Name", "Assembly", "AddInId", "FullClassName", "VendorId")
GUID = re.compile(r"^[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-"
                  r"[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}$")

# The all-user locations. Heron promises it never needs either, and the
# promise is worth checking rather than repeating: docs/07 section 5 says
# per-user "matters when the target is a BIM modeller on a locked-down
# corporate machine". Revit 2027 moved the all-user path from ProgramData to
# Program Files, which is precisely why naming both is not enough on its own -
# the test is that NEITHER appears.
ADMIN_PATHS = ("ProgramData", "Program Files")


def w(s):
    sys.stdout.write(s.encode("ascii", "replace").decode("ascii"))


def read(rel):
    try:
        return io.open(os.path.join(ROOT, rel), encoding="utf-8-sig",
                       errors="replace").read()
    except OSError:
        return None


def releases():
    """
    The supported list, read from check-compile.py rather than repeated.

    It is already stated twice in this folder - check-compile.py and
    check-api-surface.py each declare their own ALL_VERSIONS. A third copy
    would make it three. Recorded rather than fixed here, because merging
    those two is a change to two working gates and belongs in its own review.
    """
    path = os.path.join(ROOT, "tools", "check-compile.py")
    spec = importlib.util.spec_from_file_location("heron_check_compile", path)
    if spec is None or spec.loader is None:
        return None
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return list(module.ALL_VERSIONS)


TFM_ROW = re.compile(r"<HeronTfm\s+Condition=\"([^\"]+)\"\s*>([^<]+)</HeronTfm>")
CLAUSE = re.compile(r"'\$\((\w+)\)'\s*(==|!=|>=|<=|>|<)\s*'([^']*)'")


def runtime_map(props, versions):
    """
    Which runtime each release would actually build against.

    A row can be a RANGE - "'$(RevitVersion)' >= '2021' AND <= '2024'" covers
    2022 and 2023, and the first version of this check looked for the literal
    string "2022" in the file and raised both of them. That is the crying-wolf
    failure tools/check-revit-gate.py records at length: a finding that sends
    somebody to add a row that is already there.

    So the conditions are evaluated rather than searched. A condition this
    cannot parse is REPORTED, never assumed true - a version-support checker
    that guesses is a version-support checker that is sometimes not applied.
    """
    hits = {}
    unreadable = []
    for condition, tfm in TFM_ROW.findall(props.replace("&gt;", ">").replace("&lt;", "<")):
        clauses = CLAUSE.findall(condition)
        if not clauses:
            unreadable.append("cannot read the condition %r on a HeronTfm row" % condition)
            continue
        # The fallback row keys off HeronTfm itself, not the release.
        if all(name != "RevitVersion" for name, _, _ in clauses):
            continue
        for version in versions:
            if all(_holds(version, op, value)
                   for name, op, value in clauses if name == "RevitVersion"):
                hits[version] = tfm.strip()
    return hits, unreadable


def _holds(version, op, value):
    try:
        left, right = int(version), int(value)
    except ValueError:
        left, right = version, value
    return {"==": left == right, "!=": left != right, ">=": left >= right,
            "<=": left <= right, ">": left > right, "<": left < right}[op]


def classes_in_addin():
    """Every class the add-in declares, and what it implements."""
    found = {}
    for name in sorted(os.listdir(os.path.join(ROOT, ADDIN_SRC))):
        if not name.endswith(".cs"):
            continue
        text = read("%s/%s" % (ADDIN_SRC, name)) or ""
        namespace = None
        m = re.search(r"^\s*namespace\s+([\w.]+)", text, re.M)
        if m:
            namespace = m.group(1)
        for m in re.finditer(r"^\s*(?:public|internal)\s+(?:sealed\s+|abstract\s+|static\s+)*"
                             r"class\s+(\w+)\s*(?::\s*([^\{]+))?", text, re.M):
            full = "%s.%s" % (namespace, m.group(1)) if namespace else m.group(1)
            bases = [b.strip() for b in (m.group(2) or "").split(",") if b.strip()]
            found[full] = {"file": "%s/%s" % (ADDIN_SRC, name), "bases": bases}
    return found


def manifest_problems(raw, assembly_name, classes):
    """
    Every question the manifest itself can be asked, taking the text rather
    than the path - so a test can hand it a broken manifest and check that the
    answer is no. A checker nobody has ever seen say no is a checker nobody
    knows the meaning of.
    """
    problems = []
    checked = 0

    try:
        root = ET.fromstring(raw)
    except ET.ParseError as exc:
        return ["%s is not well-formed XML - %s" % (MANIFEST, exc)], 1

    addins = root.findall("AddIn")
    checked += 1
    if not addins:
        problems.append("%s declares no <AddIn>" % MANIFEST)

    for addin in addins:
        kind = addin.get("Type", "")
        values = {}
        for element in REQUIRED_ELEMENTS:
            node = addin.find(element)
            checked += 1
            if node is None or not (node.text or "").strip():
                problems.append("%s: <AddIn> has no <%s>. Revit requires it"
                                % (MANIFEST, element))
            else:
                values[element] = node.text.strip()

        checked += 1
        if "AddInId" in values and not GUID.match(values["AddInId"]):
            problems.append("%s: AddInId '%s' is not a GUID"
                            % (MANIFEST, values["AddInId"]))

        if "Assembly" in values and assembly_name:
            checked += 1
            expected = assembly_name + ".dll"
            if os.path.basename(values["Assembly"].replace("\\", "/")) != expected:
                problems.append(
                    "%s: <Assembly> is '%s' but %s builds '%s'. Revit would look "
                    "for a file that is not there"
                    % (MANIFEST, values["Assembly"], ADDIN_PROJ, expected))

        if "FullClassName" in values:
            checked += 1
            wanted = values["FullClassName"]
            entry = classes.get(wanted)
            if entry is None:
                problems.append(
                    "%s: FullClassName '%s' names no class in %s/. Revit refuses "
                    "the add-in and does not say why" % (MANIFEST, wanted, ADDIN_SRC))
            elif kind == "Application" and "IExternalApplication" not in entry["bases"]:
                problems.append(
                    "%s: FullClassName '%s' is a class in %s, but it does not "
                    "implement IExternalApplication"
                    % (MANIFEST, wanted, entry["file"]))

    checked += 1
    if "ManifestSettings" in raw:
        problems.append(
            "%s contains <ManifestSettings>. It arrived at Revit 2026 and Revit "
            "2025 and older CRASH reading it - and this one manifest is deployed "
            "to every supported release" % MANIFEST)

    return problems, checked


def main():
    problems = []
    checked = 0

    # --- 1, 2, 3, 4, 6: the manifest ---------------------------------------
    raw = read(MANIFEST)
    if raw is None:
        w("%s is missing. Revit reads this before it reads anything else.\n" % MANIFEST)
        return 1

    proj = read(ADDIN_PROJ) or ""
    m = re.search(r"<AssemblyName>([^<]+)</AssemblyName>", proj)
    assembly_name = m.group(1).strip() if m else None
    classes = classes_in_addin()

    found, asked = manifest_problems(raw, assembly_name, classes)
    problems += found
    checked += asked
    if any("not well-formed" in item for item in found):
        w("%s\n" % found[0])
        return 1

    # --- 5: the rewrite the deploy script performs --------------------------
    deploy = read(DEPLOY) or ""
    checked += 1
    literal = re.search(r'\.Replace\(\s*"([^"]+)"', deploy)
    if literal:
        needle = literal.group(1)
        if needle not in raw:
            problems.append(
                "%s rewrites the literal %r, and %s no longer contains it. "
                "String.Replace does not fail when it matches nothing - the "
                "manifest would deploy pointing at the wrong path"
                % (DEPLOY, needle, MANIFEST))

    # --- 7: Autodesk's assemblies are never redistributed -------------------
    checked += 1
    if "ExcludeAssets=\"runtime\"" not in proj and "<Private>false</Private>" not in proj:
        problems.append(
            "%s does not keep the Revit API references out of the output. They "
            "are Autodesk's and are not redistributable (docs/07 section 4)" % ADDIN_PROJ)
    checked += 1
    if "RevitAPI*" not in deploy:
        problems.append(
            "%s no longer excludes RevitAPI* when copying assemblies - it would "
            "redistribute Autodesk's own DLLs" % DEPLOY)

    # --- 8: per-user, and ONE script owns the path ---------------------------
    #
    # The first version of this asked every installer whether it mentioned
    # %APPDATA%, and raised setup.ps1 - which does not, because it CALLS
    # deploy-addin.ps1 and should not know the path at all. That is the same
    # rule platform/Heron.Core/HeronPaths.cs already carries for the product:
    # one owner builds a path, everybody else asks it. Raising setup.ps1 would
    # have sent somebody to give it a second copy of the path, which is the
    # defect, not the fix.
    deploy_text = read(DEPLOY) or ""
    checked += 1
    if "APPDATA" not in deploy_text:
        problems.append("%s does not install under %%APPDATA%% - Heron promises a "
                        "per-user install that needs no administrator" % DEPLOY)
    setup_text = read(SETUP) or ""
    checked += 1
    if "deploy-addin.ps1" not in setup_text:
        problems.append("%s no longer calls %s. One script owns the install path; "
                        "a second copy of it is how two parts of Heron come to "
                        "disagree about where the add-in lives" % (SETUP, DEPLOY))
    for rel, text in ((DEPLOY, deploy_text), (SETUP, setup_text)):
        checked += 1
        for bad in ADMIN_PATHS:
            if re.search(r"Addins[^\n]*%s|%s[^\n]*Addins" % (re.escape(bad), re.escape(bad)),
                         text):
                problems.append("%s puts an add-in under '%s' - that needs "
                                "administrator rights, which Heron promises not to"
                                % (rel, bad))

    # --- 9: every release has a runtime and a symbol -------------------------
    props = read(PROPS) or ""
    versions = releases()
    if versions is None:
        problems.append("could not read the supported release list from "
                        "tools/check-compile.py")
    else:
        runtimes, unreadable = runtime_map(props, versions)
        for line in unreadable:
            problems.append("%s: %s" % (PROPS, line))
        for version in versions:
            checked += 1
            if not runtimes.get(version):
                problems.append(
                    "%s has no row that matches Revit %s, so a build for it would "
                    "take the fallback runtime and produce an add-in that does not "
                    "load" % (PROPS, version))
        # A _OR_GREATER symbol that is referenced and never defined takes the
        # wrong branch silently - the version skill says so in those words.
        used = set()
        for dirpath, dirnames, filenames in os.walk(os.path.join(ROOT, "revit")):
            dirnames[:] = [d for d in dirnames if d not in ("bin", "obj", ".vs")]
            for name in filenames:
                if not name.endswith(".cs"):
                    continue
                body = io.open(os.path.join(dirpath, name), encoding="utf-8",
                               errors="replace").read()
                used |= set(re.findall(r"REVIT\d{4}_OR_GREATER", body))
        for symbol in sorted(used):
            checked += 1
            if symbol not in props:
                problems.append(
                    "%s uses %s and %s never defines it - the #if silently takes "
                    "the wrong branch" % ("revit/", symbol, PROPS))

    # --- 10: the ribbon's icons exist ---------------------------------------
    app = read("revit/Heron.Revit.Addin/HeronApplication.cs") or ""
    for icon in sorted(set(re.findall(r'"([\w.-]+\.png)"', app))):
        checked += 1
        if not os.path.exists(os.path.join(ROOT, ADDIN_SRC, "Resources", icon)):
            problems.append("the ribbon asks for Resources/%s and it is not in the "
                            "repository - the button deploys blank" % icon)
    checked += 1
    if "Resources" not in proj:
        problems.append("%s does not copy Resources to the output, so the icons "
                        "never travel with the assembly" % ADDIN_PROJ)

    # --- report --------------------------------------------------------------
    w("Manifest:         %s\n" % MANIFEST)
    w("Releases:         %s\n" % (", ".join(versions) if versions else "unknown"))
    if versions:
        mapped, _ = runtime_map(read(PROPS) or "", versions)
        w("Runtimes:         %s\n"
          % ", ".join("%s %s" % (v, mapped.get(v, "NONE")) for v in versions))
    w("Classes read:     %d in %s/\n" % (len(classes), ADDIN_SRC))
    w("Questions asked:  %d\n\n" % checked)

    if problems:
        w("PACKAGING PROBLEMS (%d):\n" % len(problems))
        for p in problems:
            w("  - %s\n" % p)
        w("\n")
        return 1

    w("Every offline delivery question answered. The manifest parses, its entry\n"
      "class exists and is an IExternalApplication, the deploy rewrite still\n"
      "matches, nothing redistributes Autodesk's assemblies, the install stays\n"
      "per-user, and every supported release has a runtime row.\n\n")
    w("STILL NOT ANSWERABLE HERE - each needs Windows and a Revit:\n")
    w("  - the add-in actually loads, on each release\n")
    w("  - an upgrade over a previous version keeps the user's settings\n")
    w("  - a rollback recovers a working install\n")
    w("  - Revit discovers the manifest from the per-user folder\n")
    w("  A green run here is not an install, and must never be reported as\n")
    w("  one. That has not changed and cannot: every question above is about\n")
    w("  what Revit DOES, and nothing in this file has ever seen a Revit.\n")
    w("\n")
    w("  ALL FOUR WERE ANSWERED ON 2026-09-19, on Revit 2020, 2024 and 2027\n")
    w("  on the owner's PC, from commit 48ae1dc - docs/07 section 10 has the\n")
    w("  journal lines and the hashes. Answering them found one defect: the\n")
    w("  add-in had NO rollback path at all, while brain/heron_update.py was\n")
    w("  refusing to ship any release that could not prove one had been\n")
    w("  tested. Built and proved in the same run; FRAGMENT-ISSUES row 141.\n")
    w("\n")
    w("  READ THAT AS HISTORY, NOT AS A PASS. It records one build on one\n")
    w("  machine on one day. A release cut from a later commit owes these\n")
    w("  four answers again, and this gate still cannot give them.\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
