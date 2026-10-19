#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Does the supersession check see what it says it sees, and nothing more?

    python tests/test_check_supersessions.py

`tools/check-supersessions.py` (D-144), run by check-docs.py as its section 13,
fails when a decision in force supersedes one whose entry in docs/DECISIONS.md
does not say so, and when an entry names a successor that does not.

EVERY CASE BUILDS ITS OWN LOG, in a temp folder, and points the tool at it. A
suite that read this checkout's log would be asserting today's decisions, which
is what check-docs already does and a different question from whether the
check works.

It does not prove that a note, once there, is TRUE - that the line under D-24
says correctly what D-82 changed. That is a reader's job, and the check says so
in its own docstring.
"""

import importlib.util
import io
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOL = os.path.join(ROOT, "tools", "check-supersessions.py")

FAILURES = []
MADE = []
DASH = u"—"
DOT = u"·"


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def load():
    """The tool as a module, or None - its name has hyphens in it."""
    try:
        spec = importlib.util.spec_from_file_location("check_supersessions",
                                                      TOOL)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        return module
    except Exception as exc:                      # noqa: BLE001 - reported
        print("  could not load %s: %s" % (TOOL, exc))
        return None


def entry(did, title, *lines):
    """One index entry: heading, its fields, and the link to its record."""
    body = u"\n".join(lines)
    return (u"## %s %s %s\n\n%s\n\n**Full record:** [`decisions/%s.md`]"
            u"(decisions/%s.md)\n\n" % (did, DASH, title, body, did, did))


def record(did, title, *lines):
    return (u"# %s %s %s\n\n%s\n\n### Context\n\nWords.\n"
            % (did, DASH, title, u"\n".join(lines)))


def log(entries, records=None, index=True):
    """A repository holding docs/DECISIONS.md and docs/decisions/."""
    root = tempfile.mkdtemp(prefix="heron-supersede-")
    MADE.append(root)
    os.makedirs(os.path.join(root, "docs", "decisions"))
    if index:
        with io.open(os.path.join(root, "docs", "DECISIONS.md"), "w",
                     encoding="utf-8") as f:
            f.write(u"# Decision Log\n\n> A preamble.\n\n" + u"".join(entries)
                    + u"## Status summary\n\n| [D-02](#x) | y |\n")
    for did, text in (records or {}).items():
        with io.open(os.path.join(root, "docs", "decisions", did + ".md"), "w",
                     encoding="utf-8") as f:
            f.write(text)
    return root


def run(tool, root):
    out = io.StringIO()
    code = tool.run(root=root, out=out)
    return code, out.getvalue()


OLD_TITLE = "The old way"
NEW_TITLE = "The new way, kept"
NEW_LINK = "[D-02](#d-02--the-new-way-kept)"
OLD_LINK = "[D-01](#d-01--the-old-way)"


def main():
    tool = load()
    if tool is None:
        print("FAILED  tools/check-supersessions.py could not be loaded")
        return 1

    print("1. A supersession written at both ends passes")
    code, out = run(tool, log([
        entry("D-01", OLD_TITLE, u"**Status:** Accepted %s **Date:** 2026-01-01"
              % DOT, u"**Superseded by:** %s - all of it" % NEW_LINK),
        entry("D-02", NEW_TITLE, u"**Status:** Accepted %s **Date:** 2026-01-02"
              % DOT, u"**Supersedes:** %s in full" % OLD_LINK)]))
    check(code == 0 and "PASS" in out, "exit 0 and PASS")
    check("2 decision(s) read, 1 supersession(s) declared" in out,
          "and it says what it read - the pass is not vacuous")

    print()
    print("2. The old entry saying nothing fails, and says what to write")
    code, out = run(tool, log([
        entry("D-01", OLD_TITLE, u"**Status:** Accepted"),
        entry("D-02", NEW_TITLE, u"**Status:** Accepted",
              u"**Supersedes in part:** %s item 2" % OLD_LINK)]))
    check(code == 1, "exit 1")
    check("MISSING  D-02 supersedes D-01, and D-01's entry" in out,
          "the finding names both decisions")
    check(NEW_LINK in out,
          "and gives the link to write, with the anchor check-docs derives")

    print()
    print("3. A proposed successor owes nothing yet, and is still printed")
    code, out = run(tool, log([
        entry("D-01", OLD_TITLE, u"**Status:** Accepted"),
        entry("D-02", NEW_TITLE, u"**Status:** Proposed %s **Date:** x" % DOT,
              u"**Supersedes:** %s" % OLD_LINK)]))
    check(code == 0, "exit 0")
    check("pending  D-02 supersedes D-01 - D-02 is proposed" in out,
          "the pending supersession is named, not dropped")

    print()
    print("4. A successor that has itself been superseded owes nothing")
    code, out = run(tool, log([
        entry("D-01", OLD_TITLE, u"**Status:** Accepted"),
        entry("D-02", NEW_TITLE,
              u"**Status:** Superseded by D-03 the same day %s **Date:** x"
              % DOT, u"**Supersedes:** one sentence of %s" % OLD_LINK)]))
    check(code == 0 and "skipped  D-02 supersedes D-01" in out,
          "exit 0, and the skip is named with its reason")

    print()
    print("5. A note naming a decision that does not say so is WRONG")
    code, out = run(tool, log([
        entry("D-01", OLD_TITLE, u"**Status:** Accepted",
              u"**Superseded by:** %s" % NEW_LINK),
        entry("D-02", NEW_TITLE, u"**Status:** Accepted",
              u"**Keeps:** %s" % OLD_LINK)]))
    check(code == 1 and "WRONG    D-01's entry says D-02 superseded it, and "
          "D-02's own Supersedes field does not name D-01" in out,
          "exit 1, naming the note that points the wrong way")

    print()
    print("6. A note naming a decision the log does not hold is WRONG")
    code, out = run(tool, log([
        entry("D-01", OLD_TITLE, u"**Status:** Accepted",
              u"**Superseded by:** [D-09](#d-09--gone)")]))
    check(code == 1 and "docs/DECISIONS.md holds no D-09" in out,
          "exit 1, naming the missing decision")

    print()
    print("7. Superseding a decision the log does not hold fails")
    code, out = run(tool, log([
        entry("D-02", NEW_TITLE, u"**Status:** Accepted",
              u"**Supersedes:** [D-07](#d-07--gone)")]))
    check(code == 1 and "D-02 supersedes D-07, which docs/DECISIONS.md does "
          "not hold" in out, "exit 1")

    print()
    print("8. The full record's header counts when the index copy is cut short")
    code, out = run(tool, log(
        [entry("D-01", OLD_TITLE, u"**Status:** Accepted"),
         entry("D-02", NEW_TITLE, u"**Status:** Accepted %s **Date:** x"
               % DOT)],
        {"D-02": record("D-02", NEW_TITLE, u"**Status:** Accepted",
                        u"**Supersedes the framing of** %s" % OLD_LINK)}))
    check(code == 1 and "D-02 supersedes D-01" in out,
          "a Supersedes field only in decisions/D-02.md is read")

    print()
    print("9. A banner in the full record does not stand in for the index note")
    code, out = run(tool, log(
        [entry("D-01", OLD_TITLE, u"**Status:** Accepted"),
         entry("D-02", NEW_TITLE, u"**Status:** Accepted",
               u"**Supersedes:** %s" % OLD_LINK)],
        {"D-01": record("D-01", OLD_TITLE,
                        u"> **SUPERSEDED BY %s.** Nothing below was edited."
                        % NEW_LINK, u"", u"**Status:** Accepted")}))
    check(code == 1 and "MISSING  D-02 supersedes D-01" in out,
          "the note is owed where a citation lands - the index")

    print()
    print("10. A field wrapped onto a second line still links what it names")
    code, out = run(tool, log([
        entry("D-01", OLD_TITLE, u"**Status:** Accepted"),
        entry("D-02", NEW_TITLE, u"**Status:** Accepted",
              u"**Supersedes the MECHANISM of**",
              u"%s's guard, which stays" % OLD_LINK)]))
    check(code == 1 and "D-02 supersedes D-01" in out,
          "the link on the continuation line is read")

    print()
    print("11. A link in another field on the same line is not a supersession")
    code, out = run(tool, log([
        entry("D-01", OLD_TITLE, u"**Status:** Accepted"),
        entry("D-03", "Another", u"**Status:** Accepted"),
        entry("D-02", NEW_TITLE, u"**Status:** Accepted",
              u"**Supersedes:** %s in full %s **Restores:** one sentence of "
              u"[D-03](#d-03--another) %s **Keeps:** [D-03](#d-03--another)"
              % (OLD_LINK, DOT, DOT),
              u"**Superseded by:** [D-02](#self) - its own name, ignored")]))
    check(code == 1 and "D-02 supersedes D-01" in out
          and "supersedes D-03" not in out,
          "only the Supersedes field counts, and a decision never its own")

    print()
    print("12. With no log, or no decision in it, it could not look")
    code, out = run(tool, log([], index=False))
    check(code == 2 and "could not be read" in out,
          "no docs/DECISIONS.md exits 2")
    code, out = run(tool, log([]))
    check(code == 2 and "holds no '## D-NN' heading" in out,
          "a log with no decision exits 2, never a pass")

    print()
    print("13. The anchor is the one this repository's links already use")
    check(tool.anchor(u"D-99 %s A change asked for in a chat is kept at once, "
                      u"with no preview, and Article 9 says so" % DASH)
          == "d-99--a-change-asked-for-in-a-chat-is-kept-at-once-with-no-"
             "preview-and-article-9-says-so",
          "D-99's heading gives the anchor D-126 links it by")

    for root in MADE:
        shutil.rmtree(root, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    a supersession is asked for at both ends, and only where "
          "the log says one happened")
    return 0


if __name__ == "__main__":
    sys.exit(main())
