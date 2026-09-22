#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A signature written into YAML, and the fragment that has to survive it.

    python tests/test_resign_signature.py

`tools/resign-machine-proofs.py` replaces the `by:` line of a recorded proof
with a person's name. It is one of the SIX tools in `tools/` that no suite
names, and it is the one that WRITES - into `brain/fragments/`, the library
Golden Rule 4 says a record is never destroyed in.

`heron_validate.py accept` does the same act next door and does it with two
guards this tool has neither of: it refuses a `--by` that is blank, and it
re-reads the file it just wrote and RESTORES the original if anything moved.

EVERY CASE BUILDS ITS OWN FRAGMENT TREE, in a temp folder, with
`tool.FRAGMENTS` pointed at it. A suite that ran against brain/fragments/
would be editing the library to test the editor.

WHAT IT CANNOT DO: it does not judge the evidence in a proof, and it does not
prove Revit ever produced one. That needs a model, see D-30.

    python tests/test_resign_signature.py
"""

import contextlib
import importlib.util
import io
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOL = os.path.join(ROOT, "tools", "resign-machine-proofs.py")

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def load():
    """The tool as a module, or None - its name has a hyphen, so it is
    loaded by path. A rename is ONE clean failure, never a traceback."""
    try:
        spec = importlib.util.spec_from_file_location("resign_machine_proofs",
                                                      TOOL)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        return module
    except BaseException:                          # noqa: BLE001
        return None


# One fragment carrying exactly the shape the tool exists to fix: a machine in
# the `by:` field and no date. The other keys are there to be left alone.
MACHINE_SIGNED = """id: probe
summary: a probe
proof:
  by: Claude Opus 5, at Ajmal PS's PC
  model: PIPE (3,332 elements), Revit 2020, session 8084
  positive_case: it did the thing
  fingerprint: abc123
revit: ["2020"]
"""


def main():
    print(__doc__.strip().splitlines()[0])
    print()

    # ASK BEFORE CALLING - heron-ship section 2a.
    tool = load()
    check(tool is not None, "tools/resign-machine-proofs.py loads")
    if tool is None:
        print()
        print("FAILED")
        for line in FAILURES:
            print("  - %s" % line)
        return 1

    try:
        import yaml
    except ImportError:
        print("PyYAML is not installed, and this suite reads what the tool")
        print("wrote. NOT RUN - this is not a pass.")
        return 3

    was = tool.FRAGMENTS
    try:
        def run_all(argv, tree):
            """(exit code, what it printed, {slug: the file afterwards})."""
            home = tempfile.mkdtemp(prefix="heron-resign-test-")
            for slug, body in tree.items():
                path = os.path.join(home, slug, "fragment.yaml")
                os.makedirs(os.path.dirname(path))
                io.open(path, "w", encoding="utf-8", newline="").write(body)
            tool.FRAGMENTS = home
            said = io.StringIO()
            try:
                with contextlib.redirect_stdout(said):
                    code = tool.main(["resign-machine-proofs.py"] + argv)
            except SystemExit as stop:              # argparse
                code = stop.code
            except BaseException as raised:         # noqa: BLE001
                code = "raised %s" % type(raised).__name__
            after = dict((slug, io.open(os.path.join(home, slug,
                                                     "fragment.yaml"),
                                        encoding="utf-8").read())
                         for slug in tree)
            shutil.rmtree(home, ignore_errors=True)
            return code, said.getvalue(), after

        def run(argv, text=MACHINE_SIGNED):
            """The one-fragment case, which is most of them."""
            code, spoke, after = run_all(argv, {"probe": text})
            return code, spoke, after["probe"]

        def parsed(after):
            """The proof block the tool left behind, or None if the fragment
            no longer reads as YAML at all."""
            try:
                whole = yaml.safe_load(after)
            except Exception:                       # noqa: BLE001
                return None
            if not isinstance(whole, dict):
                return None
            return whole

        print("1. The ordinary case: a machine's name out, a person's name in")
        code, spoke, after = run(["--by", "Ajmal PS", "--date", "2026-09-22"])
        book = parsed(after)
        check(code == 0, "it exits 0, and it exits %r" % code)
        check(book is not None, "the fragment still reads as YAML")
        if book:
            proof = book.get("proof") or {}
            check(proof.get("by") == "Ajmal PS",
                  "the name that comes back is the name that was typed")
            check(str(proof.get("date")) == "2026-09-22",
                  "and it is dated, because a proof nobody can date is a "
                  "proof nobody can question")
            check(proof.get("fingerprint") == "abc123"
                  and proof.get("model", "").startswith("PIPE"),
                  "every other line of the proof is exactly as it was")

        print()
        print("2. --list changes nothing at all")
        code, spoke, after = run(["--list"])
        check(code == 0 and after == MACHINE_SIGNED,
              "the file is byte-identical after --list")
        check("Claude Opus 5" in spoke, "and it names what it found")

        print()
        print("3. A NAME IS NOT YAML. Whatever is typed, the fragment survives")
        # Nobody is attacking this tool - it is run by hand by the one person
        # whose name goes in. These are the shapes an ORDINARY name has.
        for label, name in (("a colon in the name", "Ajmal: PS"),
                            ("a hash in the name", "Ajmal #2"),
                            ("a quote in the name", "Ajmal 'AJ' PS"),
                            ("a newline in the name", "Ajmal PS\nstatus: PROVEN")):
            code, spoke, after = run(["--by", name, "--date", "2026-09-22"])
            book = parsed(after)
            wrote = (book.get("proof") or {}).get("by") if book else None
            if code == 0:
                check(book is not None,
                      "%s: it said it signed, so the fragment must still read "
                      "as YAML" % label)
                check(wrote == name,
                      "%s: the name that comes back is the name that was "
                      "typed, and it is %r" % (label, wrote))
                check(book is not None and "status" not in book,
                      "%s: and nothing new appeared at the top level of the "
                      "fragment" % label)
            else:
                check(after == MACHINE_SIGNED,
                      "%s: it refused, so the fragment is untouched" % label)

        print()
        print("4. A DATE IS NOT YAML EITHER")
        code, spoke, after = run(["--by", "Ajmal PS", "--date", "it's"])
        book = parsed(after)
        if code == 0:
            check(book is not None,
                  "a date with a quote in it leaves a fragment that still "
                  "reads as YAML")
        else:
            check(after == MACHINE_SIGNED,
                  "it refused, so the fragment is untouched")

        print()
        print("5. A blank name is not a signature")
        # heron_validate.accept refuses this in one line: "accept needs a
        # person's name". The same act must not have two answers.
        code, spoke, after = run(["--by", "   ", "--date", "2026-09-22"])
        check(code != 0, "a name that is only spaces is refused, and it "
                         "exits %r" % code)
        check(after == MACHINE_SIGNED,
              "and the fragment is untouched - an unsigned proof is the one "
              "thing D-30 exists to prevent")

        print()
        print("6. A fragment that cannot be written leaves ALL of them alone")
        # machine_signed() finds its fragments by REGEX and never parses one,
        # so a fragment.yaml broken for some unrelated reason was found and
        # rewritten like any other. Two here, and `a-good` sorts first, so it
        # is built before the broken one is reached.
        broken = MACHINE_SIGNED.replace("summary: a probe",
                                        "summary: [unclosed")
        code, spoke, after = run_all(["--by", "Ajmal PS", "--date",
                                      "2026-09-22"],
                                     {"a-good": MACHINE_SIGNED,
                                      "b-broken": broken})
        check(code != 0, "it refuses rather than signing, and it exits %r" % code)
        check("NOTHING WAS WRITTEN" in spoke, "and says nothing was written")
        check(after["a-good"] == MACHINE_SIGNED,
              "and the fragment it had already built is still as it was - a "
              "run that fails on the ninth of sixteen must not leave eight "
              "rewritten")
        check(after["b-broken"] == broken, "as is the one it could not write")

        print()
        print("7. Nothing to do is said, not done")
        code, spoke, after = run(["--by", "Ajmal PS"],
                                 text=MACHINE_SIGNED.replace(
                                     "Claude Opus 5, at Ajmal PS's PC",
                                     "Ajmal PS"))
        check(code == 0 and "Nothing to do" in spoke,
              "a library with no machine-signed proof is left alone")
    finally:
        tool.FRAGMENTS = was

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - whatever name is typed, the fragment still reads as YAML")
    print("and the name that comes back is the name that went in.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
