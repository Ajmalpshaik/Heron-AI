#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A fragment it could not read, and a signature count that quietly dropped.

    python tests/test_check_signatures.py

`tools/check-signatures.py` guards the scarcest input this library has. It
was written on 2026-09-13 because thirteen fragments the owner had already
signed were sitting at DRAFT, so the next proving round offered them to him
to prove AGAIN - which is the complaint that started that session, in his
own words. A signature spent twice for one fragment is the one waste nothing
else notices.

TWO THINGS MEASURED 2026-09-22, on libraries built by copying real fragments
and mutating the copies:

  * `findings()` calls `F.load_all()` and DISCARDS the problems it returns.
    Three fragments, one of them a `fragment.yaml` that will not parse, and
    the answer is `Signatures in the library: 1` - exit 0, the broken one
    never named, and nothing anywhere saying a fragment could not be read.
    If the one it could not read is the one carrying an unused signature,
    this gate says nothing is waiting and the owner signs it again. That is
    the failure the tool exists to prevent, one level up;

  * a proof signed `Claude Opus 5, at Ajmal PS's PC` is COUNTED as a
    signature and listed like any other. D-30 says the machine gathers
    evidence and a PERSON signs, and `resign-machine-proofs.py` exists
    because sixteen fragments carried that exact string - "it is not a
    signature, it is the thing the rule exists to forbid, written into the
    field meant to prevent it". That one is MEASURED HERE AND NOT GUARDED:
    row 5b-131 is open on where the answer to "is this a person's name?"
    should live, since both tools need it and neither should hold a second
    copy of the other's word list, and asserting today's behaviour would
    lock in the thing the row asks about.

EVERY CASE BUILDS ITS OWN LIBRARY, by copying real fragments into a temp
folder and pointing `heron_fragment.FRAGMENTS_DIR` at it. The copies are
mutated, never the originals, so this asserts the TOOL's behaviour and not
today's library.

WHAT IT CANNOT DO: it does not judge a proof. `can_promote` does that, and
this tool asks it rather than re-deriving the answer.

    python tests/test_check_signatures.py
"""

import contextlib
import importlib.util
import io
import os
import re
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOL = os.path.join(ROOT, "tools", "check-signatures.py")

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def load():
    """(the tool, heron_fragment, heron_validate), or three Nones."""
    try:
        sys.path.insert(0, os.path.join(ROOT, "brain"))
        import heron_fragment
        import heron_validate
        spec = importlib.util.spec_from_file_location("check_signatures", TOOL)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        return module, heron_fragment, heron_validate
    except BaseException:                          # noqa: BLE001
        return None, None, None


def main():
    print(__doc__.strip().splitlines()[0])
    print()

    tool, F, V = load()
    if tool is None:
        print("  tools/check-signatures.py would not load. It imports")
        print("  heron_fragment, so this is usually a missing dependency")
        print("  rather than a defect. NOT RUN - and that is not a pass.")
        return 3

    check(True, "tools/check-signatures.py loads")
    runner = getattr(tool, "main", None)
    check(callable(runner), "and it still has main()")
    check(getattr(F, "FRAGMENTS_DIR", None) is not None,
          "and heron_fragment names the folder it reads, so a test can "
          "point it elsewhere")
    if not callable(runner) or getattr(F, "FRAGMENTS_DIR", None) is None:
        print()
        print("FAILED")
        for line in FAILURES:
            print("  - %s" % line)
        return 1

    # A signed fragment to copy. Picked by its SHAPE rather than by name, so
    # a rename in the library does not turn this suite red for no reason.
    folders, _problems = F.load_all()
    signed = sorted((f for f in folders.values()
                     if (f.status or "") == "PROVEN"
                     and (getattr(f, "proof", None) or {}).get("by")),
                    key=lambda f: f.slug)
    if len(signed) < 2:
        print("  fewer than two signed fragments in the library, so there is")
        print("  nothing to copy. NOT RUN - and that is not a pass.")
        return 3

    home = tempfile.mkdtemp(prefix="heron-signatures-test-")
    was = F.FRAGMENTS_DIR
    try:
        def restamp(place):
            """Re-record each copy's fingerprint against itself.

            THE FINGERPRINT INCLUDES THE FILE'S PATH, and says so: a fragment
            read from outside the repository comes back with `..` segments,
            so every copy reads STALE on arrival. Re-stamping makes the copy
            fresh by construction, which is what lets sections 2 and 5 ask
            about something other than staleness. Section 4 changes the
            implementation AFTER this, so the one case that is about
            staleness still is.
            """
            F.FRAGMENTS_DIR = place
            try:
                copies, _ = F.load_all()
                for one in copies.values():
                    fresh = one.fingerprint()
                    if not fresh:
                        continue
                    path = os.path.join(one.folder, "fragment.yaml")
                    text = io.open(path, encoding="utf-8").read()
                    io.open(path, "w", encoding="utf-8", newline="").write(
                        V.splice_fingerprint(text, fresh))
            finally:
                F.FRAGMENTS_DIR = was

        def build(mutate=None, after=None):
            """A little library of two signed fragments, then mutated."""
            place = tempfile.mkdtemp(dir=home)
            slugs = [one.slug for one in signed[:2]]
            for one in signed[:2]:
                shutil.copytree(one.folder, os.path.join(place, one.slug))
            if mutate:
                mutate(place, slugs)
            restamp(place)
            if after:
                after(place, slugs)
            return place

        def edit(place, slug, old, new):
            path = os.path.join(place, slug, "fragment.yaml")
            text = io.open(path, encoding="utf-8").read()
            assert old in text, "the fixture did not change: %r" % old
            io.open(path, "w", encoding="utf-8", newline="").write(
                text.replace(old, new, 1))

        def run(place, argv=()):
            """(exit code, what it printed) against a library built here."""
            F.FRAGMENTS_DIR = place
            said = io.StringIO()
            try:
                with contextlib.redirect_stdout(said):
                    code = runner(list(argv))
            except BaseException as raised:         # noqa: BLE001
                code = "raised %s" % type(raised).__name__
            finally:
                F.FRAGMENTS_DIR = was
            return code, said.getvalue()

        def counted(spoke):
            m = re.search(r"Signatures in the library: (\d+)", spoke)
            return int(m.group(1)) if m else None

        first, second = signed[0].slug, signed[1].slug

        print()
        print("1. Two signed, promoted fragments are counted and left alone")
        code, spoke = run(build())
        check(code == 0, "it exits 0, and it exits %r" % code)
        check(counted(spoke) == 2,
              "both signatures are counted, and it says %r" % counted(spoke))
        check("UNUSED" not in spoke and "HELD" not in spoke,
              "and neither is reported, because neither is waiting")

        print()
        print("2. THE WASTE IT EXISTS TO FIND")
        # Signed, nothing blocking, still DRAFT. Nothing stands between the
        # signature and PROVEN except an edit nobody made.
        code, spoke = run(build(lambda p, s: edit(
            p, s[0], "heron-status: PROVEN", "heron-status: DRAFT")))
        check(code == 1, "a signature going unused FAILS the gate, and it "
                         "exits %r" % code)
        check("UNUSED" in spoke and first in spoke,
              "and the fragment is named")

        print()
        print("3. A hold is not a gap, and it says so in its own file")
        def held(place, slugs):
            edit(place, slugs[0], "heron-status: PROVEN", "heron-status: DRAFT")
            edit(place, slugs[0], "\nrevit:",
                 "\nproof-held: waiting on a second route nobody has run\nrevit:")
        code, spoke = run(build(held))
        check(code == 0, "a declared hold does NOT fail the gate, and it "
                         "exits %r" % code)
        check("HELD" in spoke and "second route nobody has run" in spoke,
              "and the reason from the file is printed, so a reader sees why")

        print()
        print("4. A FRAGMENT IT COULD NOT READ IS NAMED, NOT DROPPED")
        # load_all() returns its problems and they were discarded, so a
        # broken fragment simply lowered the count. If the unreadable one is
        # the one carrying an unused signature, this gate says nothing is
        # waiting and the owner signs it twice.
        def broken(place, slugs):
            path = os.path.join(place, slugs[0], "fragment.yaml")
            text = io.open(path, encoding="utf-8").read()
            io.open(path, "w", encoding="utf-8", newline="").write(
                "summary: [unclosed\n" + text)
        code, spoke = run(build(after=broken))
        check(code != 0,
              "a library it could not read in full does not pass quietly, "
              "and it exits %r" % code)
        check(first in spoke,
              "and the fragment it could not read is NAMED")
        check(counted(spoke) != 2 and "could not" in spoke.lower(),
              "and the count is not left standing on its own as though it "
              "were the whole library")

        print()
        print("5. A machine's name in `by:` - MEASURED, NOT GUARDED")
        # D-30: the machine gathers evidence and a PERSON signs, and
        # resign-machine-proofs.py exists because sixteen fragments carried
        # exactly this string in exactly this field - "not a signature, it is
        # the thing the rule exists to forbid, written into the field meant
        # to prevent it". This tool counts it as one.
        #
        # NOT ASSERTED EITHER WAY, on purpose. Row 5b-131 records the
        # measurement and the question it turns on: WHERE the answer to
        # "is this a person's name?" should live, since both this gate and
        # resign-machine-proofs need it and neither should hold a second
        # copy of the other's word list. Asserting today's behaviour here
        # would lock in the thing the row is open about.
        code, spoke = run(build(lambda p, s: edit(
            p, s[0], "  by: ", "  by: Claude Opus 5, at ")))
        print("        measured: the count reads %r and the gate exits %r"
              % (counted(spoke), code))

        print()
        print("6. What --all does is what --all is documented to do")
        place = build()
        plain = run(place)[1]
        both = run(place, ["--all"])[1]
        promise = re.search(r"--all\s+#\s*(.+)", tool.__doc__ or "")
        check(promise is not None, "the usage line documents --all")
        if promise:
            says = promise.group(1).strip()
            listed = [one.slug for one in signed[:2] if one.slug in both]
            check(("what is fine" not in says) or len(listed) == 2,
                  "it promises %r, so either it lists them or it does not "
                  "promise to - it listed %d of 2" % (says, len(listed)))

    finally:
        F.FRAGMENTS_DIR = was
        shutil.rmtree(home, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - a fragment it could not read is named, the count says")
    print("how many it is over, and --all does what --all is documented to do.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
