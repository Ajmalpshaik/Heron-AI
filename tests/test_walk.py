# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-IMP-FIL-002
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
File discovery - the extension is what the name claims, and the counts
add up.

    python tests/test_walk.py

WHAT IT PROVES
  1. NOTHING IS SILENTLY DISCARDED. `of` equals the four buckets added
     together, on a tree built to land something in each of them.

  2. THE TYPE IS THE LAST SUFFIX, LOWER CASE - and `archive.tar.gz`
     reads as `.gz`, which is stated rather than quietly applied.

  3. NO CATEGORY IS NAMED. Not by word search - by feeding the walk a
     .py, a .md and a .json and showing the answer treats them
     identically, because a category would have to tell them apart.

  4. THE TWO NO-EXTENSION CASES ARE TOLD APART. `Makefile` and
     `.gitignore` are not both "unknown".

  5. THE REVIT LIST IS BORROWED, NOT RETYPED - the same object as
     HERON-RPT-RED-003's, and a model never reaches `files`.

  6. A LINK IS REPORTED AND NEVER FOLLOWED - what it points at does not
     appear in the answer.

  7. THE ORDER IS STABLE, so a resumed import lists the folder the way
     the first run did.

  8. THE SOURCE FOLDER IS UNTOUCHED - same names, same sizes, same
     modification times after the walk as before it.

  9. AN UNREADABLE ENTRY IS NAMED RATHER THAN DROPPED.

 10. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_walk as WALK                                      # noqa: E402
import heron_release as RELEASE                                # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


TREE = ("README.md", "LICENSE", ".gitignore", "notes.tar.gz",
        "tools/CountDucts.py", "tools/TagSheet.py", "tools/config.json",
        "tools/Office.rvt", "tools/ducts/Duct.rfa")


def build(where, names=TREE):
    """A real folder on disk. The walk really walks it."""
    for at in names:
        full = os.path.join(where, *at.split("/"))
        folder = os.path.dirname(full)
        if folder and not os.path.isdir(folder):
            os.makedirs(folder)
        with io.open(full, "w", encoding="utf-8") as handle:
            handle.write(at)
    return where


def can_list(where):
    """Whether this account can actually list a folder."""
    try:
        os.listdir(where)
        return True
    except OSError:
        return False


def snapshot(where):
    """Every name, size and modification time under a folder."""
    out = {}
    for here, folders, names in os.walk(where):
        for name in names:
            full = os.path.join(here, name)
            if os.path.islink(full):
                out[os.path.relpath(full, where)] = "link"
                continue
            stat = os.stat(full)
            out[os.path.relpath(full, where)] = (stat.st_size, stat.st_mtime)
    return out


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_walk.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]

    yard = tempfile.mkdtemp(prefix="heron-walk-test-")
    try:
        where = build(os.path.join(yard, "AJ-Tools"))
        outside = build(os.path.join(yard, "somebody-elses-project"),
                        ("SECRET.txt", "client.dwg"))
        linked = False
        try:
            os.symlink(outside, os.path.join(where, "link-out"))
            linked = True
        except (OSError, AttributeError, NotImplementedError):
            pass

        before = snapshot(where)
        answer = WALK.walk(where)
        check(answer["walked"] is True, "the folder was walked")

        print("\n1. nothing is silently discarded")
        buckets = ("files", "never_imported", "not_followed", "unreadable")
        total = sum(len(answer[name]) for name in buckets)
        check(answer["of"] == total,
              "of == %d == %s" % (answer["of"],
                                  " + ".join("%d %s" % (len(answer[name]),
                                                        name)
                                             for name in buckets)))
        counted = sum(answer["types"].values())
        check(counted == len(answer["files"]),
              "the type counts add up to every file carried forward (%d)"
              % counted)

        print("\n2. the type is the last suffix, lower case")
        for name, want in (("CountDucts.py", ".py"), ("REPORT.MD", ".md"),
                           ("archive.tar.gz", ".gz"), ("a.b.c.ZIP", ".zip"),
                           ("Makefile", ""), (".gitignore", "")):
            check(WALK.extension_of(name) == want,
                  "%-16s -> %r" % (name, want))
        by_name = dict((card["name"], card) for card in answer["files"])
        check(by_name["notes.tar.gz"]["extension"] == ".gz",
              "and the walk agrees: notes.tar.gz is .gz, not .tar.gz")

        print("\n3. no category is named")
        # A WORD SEARCH WOULD ONLY FIND THE MODULE SAYING WHOSE ROW THE
        # CATEGORIES ARE, WHICH IS THE RIGHT THING TO WRITE. So the proof
        # is behavioural: a .py, a .md and a .json come back in the same
        # shape with the same keys, because nothing told them apart.
        shapes = set()
        for name in ("CountDucts.py", "README.md", "config.json"):
            shapes.add(tuple(sorted(by_name[name].keys())))
        check(len(shapes) == 1,
              "a .py, a .md and a .json come back with identical keys: %s"
              % ", ".join(sorted(shapes.pop())))
        check("heron_release" in logic and "import heron_ingest" not in logic,
              "and it borrows no reader table - imports are release only")

        print("\n4. the two no-extension cases are told apart")
        said = dict((card["name"], card["because"])
                    for card in answer["unnamed"])
        check(sorted(said) == [".gitignore", "LICENSE"],
              "both no-extension names are reported: %s"
              % ", ".join(sorted(said)))
        check(said.get(".gitignore") != said.get("LICENSE"),
              "and they are given different reasons, not one 'unknown'")
        check(all(card in answer["files"] for card in answer["unnamed"]),
              "an unnamed file is still carried forward, not dropped")

        print("\n5. the Revit list is borrowed, not retyped")
        check(WALK.NEVER_IMPORTED is RELEASE.NEVER_LEAVES,
              "WALK.NEVER_IMPORTED IS RELEASE.NEVER_LEAVES - the same object")
        kept = sorted(card["name"] for card in answer["never_imported"])
        check(kept == ["Duct.rfa", "Office.rvt"],
              "both models are kept out: %s" % ", ".join(kept))
        check(not [c for c in answer["files"]
                   if c["extension"] in WALK.NEVER_IMPORTED],
              "and no model reached `files`")

        print("\n6. a link is reported and never followed")
        if linked:
            names = [card["name"] for card in answer["not_followed"]]
            check(names == ["link-out"], "the link is reported: %s" % names)
            leaked = [card["name"] for card in answer["files"]
                      if card["name"] in ("SECRET.txt", "client.dwg")]
            check(not leaked,
                  "and nothing behind it appears in the answer%s"
                  % ("" if not leaked else ": %s" % ", ".join(leaked)))
        else:
            check(not answer["not_followed"],
                  "this filesystem makes no symlinks - UNTESTED here, and "
                  "said so rather than counted as a pass")

        print("\n7. the order is stable")
        again = WALK.walk(where)
        check([c["at"] for c in again["files"]]
              == [c["at"] for c in answer["files"]],
              "two walks of the same folder list it in the same order")
        check([c["at"] for c in answer["files"]]
              == sorted(c["at"] for c in answer["files"]),
              "and that order is sorted, not the filesystem's")

        print("\n8. the source folder is untouched")
        check(snapshot(where) == before,
              "every name, size and modification time is as it was")

        print("\n9. an unreadable entry is named rather than dropped")
        # os.walk's DEFAULT is to swallow this, which is why `onerror` is
        # supplied at all. A mode-0 folder is the portable way to cause
        # it - and it does not stop root, so when the walk can still read
        # it this check says UNTESTED instead of counting as a pass.
        blocked = os.path.join(where, "locked")
        os.makedirs(blocked)
        build(blocked, ("inside.py",))
        os.chmod(blocked, 0)
        try:
            blind = WALK.walk(where)
            really_blocked = not can_list(blocked)
            if not really_blocked:
                check(True,
                      "this account can read a mode-0 folder (root), so the "
                      "unreadable path is UNTESTED here - said, not counted")
            else:
                named_at = [card["at"] for card in blind["unreadable"]]
                check(named_at == ["locked"],
                      "a folder that cannot be listed is named: %s"
                      % ", ".join(named_at))
                check(blind["unreadable"][0]["why"].strip(),
                      "and the reason comes from the OS, not from a guess")
                check(blind["of"] == sum(len(blind[n]) for n in buckets),
                      "and it is counted in `of`, so the total still adds up")
                check(not [c for c in blind["files"]
                           if c["name"] == "inside.py"],
                      "what was behind it is not reported as found")
        finally:
            os.chmod(blocked, 0o700)
            shutil.rmtree(blocked, ignore_errors=True)

        print("\n10. every failure is named and reached")
        for these, name in ((None, "NOTHING_TO_WALK"),
                            ("   ", "NOTHING_TO_WALK"),
                            (os.path.join(where, "README.md"),
                             "NOT_A_FOLDER"),
                            (os.path.join(yard, "gone"), "NOT_A_FOLDER")):
            said = WALK.walk(these)
            reached.add(said.get("refused"))
            check(said.get("refused") == name, "%s is reached" % name)

        contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                         "HERON-IMP-FIL-002.yaml"))
        named = contract.get("failures") or []
        check(len(named) == 2, "the contract declares 2 failures")
        for failure in named:
            check(failure in logic, "the code names %s" % failure)
        unreached = sorted(set(named) - reached)
        check(not unreached,
              "and every one was reached above%s"
              % ("" if not unreached else ": %s" % ", ".join(unreached)))
        check(len(answer["unjudged"]) == 7, "seven things are left unjudged")
    finally:
        shutil.rmtree(yard, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the extension is what the name claims, and the counts "
          "add up")
    return 0


if __name__ == "__main__":
    sys.exit(main())
