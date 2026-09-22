#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The one file nothing compiles, and the anchor that lets a newline through.

    python tests/test_check_products.py

`tools/check-products.py` is the gate on `platform/heron-products.json` -
the manifest D-93 makes the single list of what Heron installs. Its own
words: "the price of that is a file nothing compiles: a typo in it reaches a
modeller's machine untouched by every other gate in this folder."

It is the fifth of the seven CI gates that had never been opened, and this
is the first suite it has ever had.

THE THREE FORMAT PATTERNS END IN `$`, AND IN PYTHON `$` MATCHES BEFORE A
FINAL NEWLINE. Measured 2026-09-22:

    FOLDER  'Heron\\n'                                  matched
    ID      'heron-doc\\n'                              matched
    GUID    '7A1F4C62-...-1C0A6F2D5B41\\n'              matched

`FOLDER` carries a comment saying why it exists - "A separator here would
write outside the release folder, and `..` would write outside Addins
altogether" - so it is a safety check, and a safety check with a documented
purpose should not have an input that walks past it. JSON carries a newline
in a string quite happily.

EVERY CASE BUILDS ITS OWN MANIFEST, by reading the real one, mutating a copy
in a temp folder and passing it with `--file`. The repository's own manifest
is never written to, and `revit/` and `brain/heron_dotnet.py` are read as
they are - which is right, because a manifest that disagrees with the files
beside it is the thing this gate is for.

WHAT IT CANNOT DO: it cannot ask whether Revit ACCEPTS these GUIDs. Only a
real Revit answers that, the tool says so itself, and a green run here is
not an install.

    python tests/test_check_products.py
"""

import contextlib
import copy
import importlib.util
import io
import json
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOL = os.path.join(ROOT, "tools", "check-products.py")
MANIFEST = os.path.join(ROOT, "platform", "heron-products.json")

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def load():
    """The tool as a module, or None - its name has hyphens in it."""
    try:
        spec = importlib.util.spec_from_file_location("check_products", TOOL)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        return module
    except BaseException:                          # noqa: BLE001
        return None


def main():
    print(__doc__.strip().splitlines()[0])
    print()

    tool = load()
    check(tool is not None, "tools/check-products.py loads")
    runner = getattr(tool, "main", None) if tool else None
    check(callable(runner), "and it still has main()")
    if not callable(runner):
        print()
        print("FAILED")
        for line in FAILURES:
            print("  - %s" % line)
        return 1

    try:
        real = json.loads(io.open(MANIFEST, encoding="utf-8-sig").read())
    except (IOError, OSError, ValueError):
        print("  platform/heron-products.json is not readable, so there is")
        print("  nothing to mutate. NOT RUN - and that is not a pass.")
        return 3

    installs = [p for p in real.get("products", [])
                if isinstance(p, dict) and p.get("addInId")]
    if len(installs) < 2:
        print("  fewer than two installable products, so the duplicate cases")
        print("  cannot be built. NOT RUN - and that is not a pass.")
        return 3

    home = tempfile.mkdtemp(prefix="heron-products-test-")
    was = sys.argv
    try:
        def run(doc=None, text=None, argv=None):
            """(exit code, what it printed) against a manifest built here."""
            path = os.path.join(home, "m%d.json" % len(os.listdir(home)))
            io.open(path, "w", encoding="utf-8", newline="").write(
                text if text is not None else json.dumps(doc, indent=2))
            sys.argv = (["check-products.py"] + list(argv)
                        if argv is not None
                        else ["check-products.py", "--file", path])
            said = io.StringIO()
            try:
                with contextlib.redirect_stdout(said):
                    code = runner()
            except BaseException as raised:         # noqa: BLE001
                code = "raised %s" % type(raised).__name__
            finally:
                sys.argv = was
            return code, said.getvalue()

        def mutated(change):
            doc = copy.deepcopy(real)
            change(doc, [p for p in doc["products"] if p.get("addInId")])
            return doc

        print("1. The manifest as it stands answers every offline question")
        code, spoke = run(copy.deepcopy(real))
        check(code == 0, "it exits 0 on the real manifest, and it exits %r"
                         % code)
        check("Every offline question answered" in spoke, "and says so")
        check("needs a real Revit" in spoke,
              "and says what it still cannot ask - a green run here is not "
              "an install")

        print()
        print("2. THE ONE THIS FILE EXISTS FOR - a duplicate addInId")
        # Revit keys add-ins by that GUID. Two manifests sharing one is a
        # load failure with no useful message, and nothing in source looks
        # wrong (D-88).
        code, spoke = run(mutated(
            lambda d, ins: ins[1].__setitem__("addInId", ins[0]["addInId"])))
        check(code == 1 and "DUPLICATE addInId" in spoke,
              "two products claiming one GUID is refused")

        print()
        print("3. The typo that shows up as a missing tab, not as an error")
        code, spoke = run(mutated(
            lambda d, ins: ins[0].__setitem__("partOf", "heron-nosuchthing")))
        check(code == 1 and "not a product in this manifest" in spoke,
              "a partOf naming nothing is refused")

        print()
        print("4. A release named here and built nowhere - AJ Tools' L3")
        code, spoke = run(mutated(
            lambda d, ins: ins[0]["revit"].append("2019")))
        check(code == 1 and "installs NOTHING, silently" in spoke,
              "a release this repository does not build is refused, and the "
              "reason names what it would cost")
        code, spoke = run(mutated(
            lambda d, ins: ins[0]["revit"].append(ins[0]["revit"][0])))
        check(code == 1 and "repeats a release" in spoke,
              "and so is one named twice")

        print()
        print("5. One product, one folder - R-41, and L5")
        code, spoke = run(mutated(
            lambda d, ins: ins[1].__setitem__("folder", ins[0]["folder"])))
        check(code == 1 and "SHARED folder" in spoke,
              "two products installing into one folder is refused - each "
              "carries its own dependencies and they would overwrite")

        print()
        print("6. The version it offers is the version it installs")
        code, spoke = run(mutated(
            lambda d, ins: ins[0].__setitem__("version", "9.9.9")))
        check(code == 1 and "install another" in spoke,
              "a version disagreeing with Directory.Build.props is refused")

        print()
        print("7. A key nothing reads is a typo for one that matters")
        code, spoke = run(mutated(
            lambda d, ins: ins[0].__setitem__("assmebly", "Heron.dll")))
        check(code == 1 and "which nothing reads" in spoke,
              "an unknown key is refused rather than ignored")

        print()
        print("8. A FOLDER NAME IS A FOLDER NAME")
        # The pattern exists because "a separator here would write outside
        # the release folder, and `..` would write outside Addins
        # altogether". `$` matches before a final newline, so a value ending
        # in one walked straight past it.
        code, spoke = run(mutated(
            lambda d, ins: ins[0].__setitem__("folder", "../evil")))
        check(code == 1 and "not a plain folder name" in spoke,
              "a folder that climbs out is refused")
        code, spoke = run(mutated(
            lambda d, ins: ins[0].__setitem__("folder",
                                              ins[0]["folder"] + "\n")))
        check(code == 1,
              "and so is one ending in a newline, and it exits %r" % code)
        code, spoke = run(mutated(
            lambda d, ins: ins[0].__setitem__("id", ins[0]["id"] + "\n")))
        check(code == 1, "an id ending in a newline is refused, and it "
                         "exits %r" % code)
        code, spoke = run(mutated(
            lambda d, ins: ins[0].__setitem__("addInId",
                                              ins[0]["addInId"] + "\n")))
        check(code == 1, "and a GUID ending in a newline, and it exits %r"
                         % code)
        code, spoke = run(mutated(
            lambda d, ins: ins[0].__setitem__("addInId",
                                              ins[0]["addInId"].lower())))
        check(code == 1 and "not a GUID in upper case" in spoke,
              "and one in lower case, which is the ordinary typo")

        print()
        print("9. It has to parse before anything about it can be true")
        code, spoke = run(text="{not json")
        check(code == 1 and "not valid JSON" in spoke,
              "a manifest that will not parse is refused, and says why")
        code, spoke = run(argv=["--file"])
        check(code == 1 and "needs a path" in spoke,
              "--file with nothing after it is refused rather than a "
              "traceback")
    finally:
        sys.argv = was
        shutil.rmtree(home, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - every offline question it claims to answer, answered on a")
    print("manifest written to break it, and a newline is not a folder name.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
