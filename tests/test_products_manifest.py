#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The product manifest gate. No Windows, no Revit, no compiler.

WHAT IT PROVES
--------------
That tools/check-products.py SAYS NO. A checker that has only ever passed has
never been tested - it could be returning 0 unconditionally and nobody would
know until a duplicate GUID reached a modeller's Revit and neither tab
appeared.

So every case below breaks the real manifest one way, on a copy in a
temporary folder, and requires the checker to refuse it AND to name the
product. The real manifest is never touched.

    python tests/test_products_manifest.py

WHAT IT CANNOT PROVE
--------------------
That Revit accepts the GUIDs, or that two Heron tabs can exist side by side.
Only a real Revit answers that - Stage 2 of the plugin-extension plan. A green
run here is not an install and must never be reported as one.
"""

import copy
import io
import json
import os
import shutil
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CHECKER = os.path.join(ROOT, "tools", "check-products.py")
MANIFEST = os.path.join(ROOT, "platform", "heron-products.json")

FAILURES = []


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def run(path):
    """The checker's real contract: an exit code and a message a person reads."""
    proc = subprocess.Popen(
        [sys.executable, CHECKER, "--file", path],
        stdout=subprocess.PIPE, stderr=subprocess.STDOUT, cwd=ROOT)
    out = proc.communicate()[0].decode("utf-8", "replace")
    return proc.returncode, out


def refuses(tmp, name, mangle, saying):
    """Break one copy one way, and require a refusal that names the fault."""
    doc = json.loads(io.open(MANIFEST, encoding="utf-8-sig").read())
    mangle(doc)
    path = os.path.join(tmp, name + ".json")
    io.open(path, "w", encoding="utf-8").write(json.dumps(doc, indent=2))

    code, out = run(path)
    check(code == 1, "%s: refused (exit 1)" % name)
    check(saying.lower() in out.lower(),
          "%s: and the message says '%s'" % (name, saying))


def product(doc, pid):
    for p in doc["products"]:
        if p["id"] == pid:
            return p
    raise AssertionError("no product %s in the real manifest" % pid)


def main():
    print("The real manifest")
    code, out = run(MANIFEST)
    check(code == 0, "platform/heron-products.json passes (exit 0)")
    if code != 0:
        print(out)
    check("derived" in out,
          "and the release list is DERIVED, not typed into the checker")

    tmp = tempfile.mkdtemp(prefix="heron-products-")
    try:
        print("\nThe fault that Revit cannot survive")
        # D-88. Revit keys add-ins by AddInId, so two products sharing one is
        # a load failure and NEITHER tab appears. Nothing in source looks
        # wrong, no compiler sees it, and no other gate in this repository
        # reads this file.
        refuses(tmp, "duplicate-addInId",
                lambda d: product(d, "heron-doc").__setitem__(
                    "addInId", product(d, "heron-bridge")["addInId"]),
                "duplicate addInId")

        print("\nThe faults that show up as a missing tab")
        refuses(tmp, "partOf-typo",
                lambda d: product(d, "heron-bridge").__setitem__(
                    "partOf", "herron"),
                "not a product in this manifest")

        refuses(tmp, "requires-typo",
                lambda d: product(d, "heron-doc").__setitem__(
                    "requires", ["heron-brige"]),
                "not a product in this manifest")

        refuses(tmp, "duplicate-id",
                lambda d: d["products"].append(
                    copy.deepcopy(product(d, "heron-doc"))),
                "duplicate id")

        print("\nThe fault that installs nothing, silently - L3")
        # AJ Tools' installer carried a hardcoded version list that was left
        # behind after per-version builds landed. It installed nothing at all
        # on three releases and NOTHING FAILED - it simply skipped.
        refuses(tmp, "unsupported-release",
                lambda d: product(d, "heron-doc")["revit"].append("2028"),
                "this repository does not build")

        print("\nThe faults in the shape the window draws")
        refuses(tmp, "heading-that-installs",
                lambda d: product(d, "heron").__setitem__(
                    "assembly", "Heron.dll"),
                "installs nothing itself")

        refuses(tmp, "installable-with-no-guid",
                lambda d: product(d, "heron-doc").__setitem__("addInId", None),
                "needs addInId")

        refuses(tmp, "two-levels-deep",
                lambda d: product(d, "heron").__setitem__("partOf", "heron-doc"),
                "one indent")

        refuses(tmp, "shipped-piece-of-a-planned-tab",
                lambda d: product(d, "heron").__setitem__("state", "PLANNED"),
                "shipped but the tab it joins")

        print("\nThe faults where the row and the shipped file disagree")
        refuses(tmp, "wrong-guid-for-the-addin",
                lambda d: product(d, "heron-bridge").__setitem__(
                    "addInId", "00000000-1111-2222-3333-444444444444"),
                "revit reads the second one")

        # heron-mep is the one with no files at all. heron-doc and
        # heron-tools have Stage 2's shape proofs on disk.
        refuses(tmp, "shipped-with-no-file",
                lambda d: product(d, "heron-mep").__setitem__(
                    "state", "SHIPPED"),
                "is nowhere under revit/")

        refuses(tmp, "proving-with-no-file",
                lambda d: product(d, "heron-mep").__setitem__(
                    "state", "PROVING"),
                "is nowhere under revit/")

        # The reverse: files on disk under a row that says there are none.
        # This is the case that ASKED FOR the PROVING state - the checker
        # refused a PLANNED row whose .addin had appeared, a person looked,
        # and the answer was a third state rather than a looser check.
        refuses(tmp, "planned-but-the-files-are-there",
                lambda d: product(d, "heron-doc").__setitem__(
                    "state", "PLANNED"),
                "already exists")

        print("\nThe faults in the file itself")
        refuses(tmp, "missing-field",
                lambda d: product(d, "heron-doc").pop("description"),
                "missing description")

        refuses(tmp, "unknown-field",
                lambda d: product(d, "heron-doc").__setitem__(
                    "addinId", "typo-for-addInId"),
                "nothing reads")

        refuses(tmp, "bad-state",
                lambda d: product(d, "heron-doc").__setitem__(
                    "state", "MAYBE"),
                "is not one of")

        refuses(tmp, "no-metadata-header",
                lambda d: d.pop("heron-layer"),
                "missing 'heron-layer'")

        # Read as DATA and never executed (R-13, Golden Rule 19), so it has to
        # parse before anything else about it can be true.
        broken = os.path.join(tmp, "not-json.json")
        io.open(broken, "w", encoding="utf-8").write("{ this is not json")
        code, out = run(broken)
        check(code == 1, "not-json: refused (exit 1)")
        check("not valid json" in out.lower(),
              "not-json: and the message says it does not parse")

        missing = os.path.join(tmp, "nowhere.json")
        code, out = run(missing)
        check(code == 1, "a file that is not there: refused (exit 1)")
    finally:
        shutil.rmtree(tmp, ignore_errors=True)

    print("")
    if FAILURES:
        print("FAILED (%d)" % len(FAILURES))
        for f in FAILURES:
            print("  - %s" % f)
        return 1

    print("The product manifest gate holds. It passes the real manifest and")
    print("refuses every fault above by name - including the duplicate GUID,")
    print("which no compiler, test or other gate in this repository can see.")
    print("")
    print("IT STILL PROVES NOTHING ABOUT REVIT. That two Heron tabs can live")
    print("in one Revit is Stage 2, and it needs a Windows machine with Revit")
    print("on it.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
