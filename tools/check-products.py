#!/usr/bin/env python3
# Heron-Agent:  HERON-INS-PKG-012, HERON-INS-ORC-001
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Is the product manifest one a Revit could survive?

    python tools/check-products.py
    python tools/check-products.py --file <a copy>     check some other file

WHY THIS EXISTS
---------------
D-93 says the list of Heron products is a MANIFEST read as data, so that
adding Heron Structure next year is a line in a file rather than a rebuild of
the installer. The price of that is a file nothing compiles: a typo in it
reaches a modeller's machine untouched by every other gate in this folder.

A TRAILING NEWLINE WALKED PAST ALL THREE FORMAT PATTERNS until 2026-09-22,
because in Python `$` also matches just before a final newline. `\Z` matches
the end and nothing else. The GUID was caught downstream anyway - the .addin
comparison is exact - and the id and the folder were not.

Three of the mistakes it can carry are silent until Revit refuses to start:

    a duplicate addInId      Revit keys add-ins by that GUID. Two manifests
                             sharing one is a LOAD FAILURE, not a warning,
                             and nothing in source ever looks wrong (D-88)
    a partOf typo            names a heading that does not exist, so a tick
                             disappears from the installer window with no
                             error anywhere
    a stale revit list       L3 from AJ Tools: a hardcoded version list was
                             left behind after per-version builds landed, and
                             the installer SILENTLY installed nothing at all
                             on three releases while the document advertised
                             them. Nothing failed. It just skipped

THE VERSION LIST IS DERIVED, NEVER TYPED HERE
---------------------------------------------
R-39 and AGENTS.md say the same thing from two directions: never type a number
a command can derive. So the supported releases come from
brain/heron_dotnet.py RELEASES - the one place in the repository that maps a
Revit release to its runtime, and the place Directory.Build.props is already
reconciled against. This file asserts nothing about which releases exist; it
only refuses a manifest naming one the repository does not build.

WHAT IT CANNOT ASK
------------------
Whether Revit ACCEPTS the GUIDs. Only a real Revit answers that, and that is
Stage 2 of the plan, not this script. A green run here is not an install and
must never be reported as one.

Exit 0 = every offline question answered yes.
Exit 1 = one of them answered no, and the answer names the product.
"""

import importlib.util
import io
import json
import os
import re
import sys
import xml.etree.ElementTree as ET

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

MANIFEST = os.path.join("platform", "heron-products.json")
PROPS = "Directory.Build.props"
ADDIN_SRC = "revit"

# Every field Stage 1 of the plan requires, plus 'state'. An entry carrying
# something else is refused rather than ignored: an unknown key is either a
# typo for a real field - in which case the real field is silently missing -
# or a setting somebody expects to do something, and nothing reads it.
FIELDS = ("id", "name", "description", "tab", "addin", "assembly", "addInId",
          "folder", "revit", "requires", "version", "partOf", "state")

# The four an entry needs to install itself. A heading carries none of them.
#
# `folder` is where the files land under Addins\<version>\, and it is NOT
# derivable from the assembly name: heron-bridge's assembly is
# Heron.Revit.Addin.dll and its folder is Heron. That is the live layout on
# every machine Heron is installed on, written by deploy-addin.ps1 since Step
# 1, so deriving it would move an existing install and orphan the copy Revit
# is already loading.
INSTALLABLE = ("addin", "assembly", "addInId", "folder")

# SHIPPED   the files exist and a user may be offered this
# PROVING   the files exist, they build, and they are a SHAPE PROOF carrying
#           a dummy button. An installer MUST NOT offer one. Stage 2 of the
#           plugin-extension plan creates these and deletes them again
# PLANNED   the files do not exist yet
#
# PROVING was added because the checker asked for it. It refused the manifest
# when Stage 2's .addin files appeared beside a PLANNED row - "either the
# files shipped and this row is stale, or the file is a leftover, and both are
# worth a person looking". A person looked, and the answer was that there is a
# third thing a product can be. Silence would have been the dishonest option:
# a file Revit will load is a file the one true list has to account for.
STATES = ("SHIPPED", "PROVING", "PLANNED")

# The two states whose files must be on disk, and must agree with the row.
ON_DISK = ("SHIPPED", "PROVING")

# \Z AND NOT $, IN ALL THREE. In Python `$` also matches just before a final
# newline, so every one of these accepted a value ending in one - measured
# 2026-09-22: 'Heron\n' was a folder name, 'heron-doc\n' was an id, and
# '7A1F...5B41\n' was a GUID. JSON carries a newline in a string quite
# happily, and this is the file nothing compiles, so a value that walks past
# a format check here reaches a machine untouched. \Z matches the end and
# nothing else.
ID = re.compile(r"^[a-z][a-z0-9]*(-[a-z0-9]+)*\Z")

# A single folder name, not a path. A separator here would write outside the
# release folder, and `..` would write outside Addins altogether.
FOLDER = re.compile(r"^[A-Za-z0-9][A-Za-z0-9._-]*\Z")
GUID = re.compile(r"^[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-"
                  r"[0-9A-F]{4}-[0-9A-F]{12}\Z")

META = {"heron-agent": str, "heron-step": int, "heron-status": str,
        "heron-since": str, "heron-layer": str}


def w(s):
    """stdout that survives a cp1252 console - the owner runs this on Windows."""
    sys.stdout.write(s.encode("ascii", "replace").decode("ascii"))


def releases():
    """
    The Revit releases this repository builds, read rather than typed.

    brain/heron_dotnet.py is the single source: it holds RELEASES and it
    already reconciles that list against Directory.Build.props, so a release
    added in one place and not the other is caught there rather than here.
    tools/ reading brain/ is allowed - D-48.
    """
    path = os.path.join(ROOT, "brain", "heron_dotnet.py")
    spec = importlib.util.spec_from_file_location("heron_dotnet_for_products", path)
    if spec is None or spec.loader is None:
        return None
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return [str(r) for r in module.RELEASES]


def build_version():
    """The one version in Directory.Build.props, which stamps the assemblies."""
    try:
        text = io.open(os.path.join(ROOT, PROPS), encoding="utf-8").read()
    except (IOError, OSError):
        return None
    m = re.search(r"<Version>([^<]+)</Version>", text)
    return m.group(1).strip() if m else None


def shipped_addin(name):
    """
    The .addin as it exists in the repository today, or None.

    SEARCHED ACROSS THE WHOLE OF revit/, not in one named folder. An earlier
    version of this looked only in revit/Heron.Revit.Addin, so when Stage 2's
    Heron.Doc.addin and Heron.Tools.addin arrived in folders of their own the
    checker could not see them and passed a manifest it should have
    questioned. A product whose manifest names a file that is not there is the
    'missing asset' Stage 1 asks this checker to refuse - as far as a machine
    with no release to download can ask it.
    """
    for dirpath, dirnames, filenames in os.walk(os.path.join(ROOT, ADDIN_SRC)):
        dirnames[:] = [d for d in dirnames if d not in ("bin", "obj")]
        if name in filenames:
            path = os.path.join(dirpath, name)
            try:
                return ET.parse(path).getroot()
            except ET.ParseError:
                return False      # exists, will not parse - a different fault
            except (IOError, OSError):
                return None
    return None


def element(root, tag):
    node = root.find(".//" + tag)
    return None if node is None or node.text is None else node.text.strip()


def check_metadata(doc, problems):
    """docs/29's five fields, carried as data keys the way a fragment.yaml does."""
    for field, kind in sorted(META.items()):
        if field not in doc:
            problems.append("the manifest itself is missing '%s' (docs/29)" % field)
        elif not isinstance(doc[field], kind):
            problems.append("the manifest's '%s' should be a %s"
                            % (field, kind.__name__))
    if doc.get("heron-layer") not in (None, "platform"):
        problems.append("the manifest says heron-layer '%s'; it lives in "
                        "platform/" % doc.get("heron-layer"))


def check_one(p, index, known, supported, version, problems):
    """Everything answerable about a single entry, without looking at the others."""
    where = p.get("id") or "product %d" % index

    missing = [f for f in FIELDS if f not in p]
    if missing:
        problems.append("%s: missing %s" % (where, ", ".join(missing)))
        return
    unknown = [f for f in p if f not in FIELDS]
    if unknown:
        problems.append("%s: carries %s, which nothing reads. A key nothing "
                        "reads is either a typo for a real one or a setting "
                        "that silently does nothing"
                        % (where, ", ".join(sorted(unknown))))

    if not isinstance(p["id"], str) or not ID.match(p["id"]):
        problems.append("%s: id must be lower case, hyphenated, e.g. heron-doc"
                        % where)

    folder = p["folder"]
    if folder is not None and (not isinstance(folder, str)
                               or not FOLDER.match(folder)
                               or folder in (".", "..")):
        problems.append(
            "%s: folder '%s' is not a plain folder name. A separator or a '..' "
            "here would write outside the Revit Addins folder" % (where, folder))

    for field in ("name", "description", "tab", "version"):
        if not isinstance(p[field], str) or not p[field].strip():
            problems.append("%s: %s must be a non-empty line" % (where, field))

    if p["state"] not in STATES:
        problems.append("%s: state '%s' is not one of %s"
                        % (where, p["state"], ", ".join(STATES)))

    # --- the releases it claims ------------------------------------------
    if not isinstance(p["revit"], list) or not p["revit"]:
        problems.append("%s: revit must list at least one release" % where)
    else:
        for r in p["revit"]:
            if str(r) not in supported:
                problems.append(
                    "%s: claims Revit %s, which this repository does not "
                    "build. Supported: %s. A release named here and built "
                    "nowhere installs NOTHING, silently - that is L3"
                    % (where, r, ", ".join(supported)))
        if sorted(set(map(str, p["revit"]))) != sorted(map(str, p["revit"])):
            problems.append("%s: revit repeats a release" % where)

    # --- the version it came from ----------------------------------------
    if version and p["state"] in ON_DISK and p["version"] != version:
        problems.append(
            "%s: version %s, but Directory.Build.props stamps the assemblies "
            "%s. Heron would offer one number and install another"
            % (where, p["version"], version))

    # --- what it points at -----------------------------------------------
    for field in ("partOf",):
        target = p[field]
        if target is None:
            continue
        if not isinstance(target, str) or target not in known:
            problems.append(
                "%s: %s names '%s', which is not a product in this manifest. "
                "This is the typo that shows up as a missing tab rather than "
                "as an error" % (where, field, target))
        elif target == p["id"]:
            problems.append("%s: partOf names itself" % where)

    if not isinstance(p["requires"], list):
        problems.append("%s: requires must be a list, empty if nothing is "
                        "needed" % where)
    else:
        for need in p["requires"]:
            if need not in known:
                problems.append(
                    "%s: requires '%s', which is not a product in this "
                    "manifest" % (where, need))
            elif need == p["id"]:
                problems.append("%s: requires itself" % where)


def check_shape(products, problems):
    """
    Heading or installable - derived from partOf and from nothing else.

    D-93: 'the same field drives the window's indentation and nothing is
    special-cased in the code'. So headingness is not a field somebody could
    set wrongly. An entry is a heading exactly when another entry names it,
    and this is where that is enforced in both directions.
    """
    by_id = dict((p["id"], p) for p in products if isinstance(p.get("id"), str))
    parents = set(p["partOf"] for p in products if p.get("partOf"))

    for p in products:
        pid = p.get("id")
        if not isinstance(pid, str):
            continue
        heading = pid in parents

        if heading:
            carried = [f for f in INSTALLABLE if p.get(f) is not None]
            if carried:
                problems.append(
                    "%s is a heading - %s join it - so it installs nothing "
                    "itself and %s must be null"
                    % (pid, ", ".join(sorted(q["id"] for q in products
                                             if q.get("partOf") == pid)),
                       ", ".join(carried)))
        else:
            absent = [f for f in INSTALLABLE if p.get(f) is None]
            if absent:
                problems.append(
                    "%s installs itself - nothing names it in partOf - so it "
                    "needs %s" % (pid, ", ".join(absent)))

        # A piece of a piece. Nothing in the plan allows two levels, and the
        # installer window draws exactly one indent (R-33, R-36).
        parent = by_id.get(p.get("partOf"))
        if parent is not None and parent.get("partOf"):
            problems.append(
                "%s joins %s, which itself joins %s. The window draws one "
                "indent, not a tree" % (pid, parent["id"], parent["partOf"]))

        # A piece cannot ship into a tab that does not.
        if (parent is not None and p.get("state") == "SHIPPED"
                and parent.get("state") != "SHIPPED"):
            problems.append(
                "%s is SHIPPED but the tab it joins, %s, is %s"
                % (pid, parent["id"], parent.get("state")))


def check_folders(products, problems):
    """
    One product, one folder - R-41, and it is AJ Tools' lesson L5.

    Two products installing into one folder means each carries its own
    dependencies into the same place, and two different versions of the same
    helper assembly overwrite each other. The loser fails at runtime looking
    like a bug in whichever product loaded second.
    """
    seen = {}
    for p in products:
        folder, pid = p.get("folder"), p.get("id", "?")
        if not isinstance(folder, str):
            continue
        if folder.lower() in seen:
            problems.append(
                "SHARED folder: %s and %s both install into '%s'. Each product "
                "carries its own dependencies, so they would overwrite each "
                "other" % (seen[folder.lower()], pid, folder))
        else:
            seen[folder.lower()] = pid


def check_guids(products, problems):
    """
    THE ONE THIS FILE EXISTS FOR.

    Revit keys add-ins by AddInId. Two manifests carrying the same GUID is a
    load failure with no useful message, and no compiler, test or gate in this
    repository can see it - D-88.
    """
    seen = {}
    for p in products:
        guid = p.get("addInId")
        pid = p.get("id", "?")
        if guid is None:
            continue
        if not isinstance(guid, str) or not GUID.match(guid):
            problems.append(
                "%s: addInId '%s' is not a GUID in upper case, "
                "8-4-4-4-12" % (pid, guid))
            continue
        if guid in seen:
            problems.append(
                "DUPLICATE addInId: %s and %s both claim %s. Revit keys "
                "add-ins by this GUID, so installing both is a load failure "
                "and neither tab appears" % (seen[guid], pid, guid))
        else:
            seen[guid] = pid
    return len(seen)


def check_assets(products, problems):
    """
    Does the file this entry names exist, and does it agree with this row?

    A release asset cannot be checked from here - there is no release. What
    CAN be checked is the manifest each product ships, because it is in the
    repository, and a row disagreeing with it is a row that installs the wrong
    thing while looking right.
    """
    checked = 0
    for p in products:
        pid, name, state = p.get("id", "?"), p.get("addin"), p.get("state")
        if not isinstance(name, str):
            continue

        root = shipped_addin(name)

        if state in ON_DISK:
            if root is None:
                problems.append(
                    "%s is %s but %s is nowhere under %s/. A product offered "
                    "to a user with no file behind it is an install that "
                    "fails at the last step" % (pid, state, name, ADDIN_SRC))
                continue
            if root is False:
                problems.append("%s: %s will not parse as XML" % (pid, name))
                continue

            checked += 1
            real = element(root, "AddInId")
            if real and p.get("addInId") and real.upper() != p["addInId"].upper():
                problems.append(
                    "%s: this manifest says addInId %s, %s says %s. Revit "
                    "reads the second one" % (pid, p["addInId"], name, real))
            real = element(root, "Assembly")
            if real and p.get("assembly") and os.path.basename(real) != p["assembly"]:
                problems.append(
                    "%s: this manifest says assembly %s, %s says %s"
                    % (pid, p["assembly"], name, real))

        elif state == "PLANNED" and root not in (None, False):
            problems.append(
                "%s is PLANNED but %s already exists in %s. Either the files "
                "shipped and this row is stale, or the file is a leftover. "
                "Both are worth a person looking" % (pid, name, ADDIN_SRC))
    return checked


def main():
    path = MANIFEST
    argv = sys.argv[1:]
    if "--file" in argv:
        i = argv.index("--file")
        if i + 1 >= len(argv):
            w("--file needs a path\n")
            return 1
        path = argv[i + 1]

    full = path if os.path.isabs(path) else os.path.join(ROOT, path)
    try:
        text = io.open(full, encoding="utf-8-sig").read()
    except (IOError, OSError):
        w("FAIL  cannot read %s\n" % path)
        return 1

    try:
        doc = json.loads(text)
    except ValueError as exc:
        w("FAIL  %s is not valid JSON: %s\n" % (path, exc))
        w("      It is read as DATA and never executed, so it has to parse\n"
          "      before anything else about it can be true.\n")
        return 1

    w("Manifest:         %s\n" % path)

    supported = releases()
    if supported is None:
        w("FAIL  cannot read RELEASES from brain/heron_dotnet.py\n")
        return 1
    w("Releases built:   %s   (brain/heron_dotnet.py, derived)\n"
      % ", ".join(supported))

    problems = []
    check_metadata(doc, problems)

    products = doc.get("products")
    if not isinstance(products, list) or not products:
        problems.append("no products: the manifest lists nothing to install")
        products = []

    known = set(p["id"] for p in products
                if isinstance(p, dict) and isinstance(p.get("id"), str))

    seen_ids = {}
    for i, p in enumerate(products, 1):
        if not isinstance(p, dict):
            problems.append("product %d is not an object" % i)
            continue
        pid = p.get("id")
        if isinstance(pid, str):
            if pid in seen_ids:
                problems.append(
                    "DUPLICATE id: two products are both called '%s'. An id "
                    "is how the installer, the release and the Settings panel "
                    "all name the same thing" % pid)
            seen_ids[pid] = i

    version = build_version()
    for i, p in enumerate(products, 1):
        if isinstance(p, dict):
            check_one(p, i, known, supported, version, problems)

    good = [p for p in products if isinstance(p, dict)]
    check_shape(good, problems)
    check_folders(good, problems)
    guids = check_guids(good, problems)
    assets = check_assets(good, problems)

    headings = set(p["partOf"] for p in good if p.get("partOf"))
    counted = ", ".join(
        "%d %s" % (len([p for p in good if p.get("state") == state]), state.lower())
        for state in STATES
        if any(p.get("state") == state for p in good))

    w("Products:         %d  (%s)\n" % (len(good), counted))
    w("Tabs:             %s\n"
      % ", ".join(sorted(set(p["tab"] for p in good
                             if isinstance(p.get("tab"), str)))))
    w("Headings:         %s\n" % (", ".join(sorted(headings)) or "none"))
    w("GUIDs:            %d, all different\n" % guids if not problems
      else "GUIDs:            %d distinct\n" % guids)
    w("Manifests read:   %d in %s\n\n" % (assets, ADDIN_SRC))

    if problems:
        w("PRODUCT MANIFEST PROBLEMS (%d):\n" % len(problems))
        for p in problems:
            w("  - %s\n" % p)
        w("\n")
        return 1

    w("Every offline question answered. Every id and every addInId is unique,\n"
      "every partOf and every requires names a product that is here, every\n"
      "release claimed is one this repository builds, and each shipped product\n"
      "agrees with the .addin it ships.\n\n")
    w("STILL NOT ANSWERABLE HERE - this needs a real Revit:\n")
    w("  - that Revit ACCEPTS these GUIDs and loads two Heron tabs at once\n")
    w("    That is Stage 2 of docs/work-notes/plans/plugin-extension, and a\n")
    w("    green run here is not it.\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
