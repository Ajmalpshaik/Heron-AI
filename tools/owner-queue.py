# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""What is waiting on the OWNER, derived from the registers rather than typed.

WHY THIS EXISTS
---------------
The owner's obligations were spread across five registers and two work notes,
and there was no way to see them at once except by reading all seven. Every
attempt to keep a typed summary of them has gone stale, three times in one day
on 2026-09-12 alone:

  OPEN-QUESTIONS.md   said "1 open" while three questions were open
  NEEDS-CHECKING.md   said "two rows have been added" while it was six
  HANDOVER.md 1-3     described a repository of 7 DRAFT fragments and 17 suites

Each was a sentence somebody wrote once and nobody re-derived. So this prints
the list instead of a document carrying it. FOR-THE-OWNER.md is the structure
and this is the content.

WHAT IT IS NOT
--------------
It reads the registers and reports them. It decides nothing, and a row it
cannot classify is printed under UNCLASSIFIED rather than dropped - a queue
that silently loses an item is worse than no queue. It exits 0 always: a list
of work waiting on a person is not a build failure.
"""
import os
import re
import glob
import io
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# The registers are full of em dashes and arrows, and this tool's whole point is
# that the OWNER runs it - on Windows, where the console is cp1252 and cannot
# encode any of them. The first run printed "server receives ?" for exactly that
# reason, which is `test_ingest`'s failure (A14) reproduced in the tool written
# to report it. Reconfigure where Python allows it, and fall back to replacing
# the characters rather than dying half way down a list.
try:                                           # Python 3.7+
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except (AttributeError, ValueError):           # pragma: no cover - old or piped
    pass


def say(text=""):
    """print(), but a console that cannot encode a dash does not kill the run."""
    try:
        print(text)
    except UnicodeEncodeError:
        enc = getattr(sys.stdout, "encoding", None) or "ascii"
        print(text.encode(enc, "replace").decode(enc, "replace"))


def read(rel):
    p = os.path.join(ROOT, rel)
    if not os.path.exists(p):
        return None
    with open(p, encoding="utf-8") as fh:
        return fh.read()


# THE REGISTER IS SEVERAL FILES SINCE 2026-09-23, AND IS STILL READ AS ONE.
# tools/needs-checking-register.py puts every group's file back under its
# heading in NEEDS-CHECKING.md, links as they were written, so this tool reads
# exactly the text it read when the register was one file. It reads through
# `read` above, so a suite that serves this tool a register is still obeyed.
# A group file that is missing raises - a register that quietly drops a group
# is the failure the register records against itself.
def _register_reader():
    import importlib.util
    here = os.path.dirname(os.path.abspath(__file__))
    spec = importlib.util.spec_from_file_location(
        "needs_checking_register", os.path.join(here, "needs-checking-register.py"))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


NCR = _register_reader()


def needs_checking_text():
    return NCR.register_text(ROOT, read)


# PROPOSALS.md IS ONE FILE PER SECTION SINCE 2026-09-23, and is still read
# as one text - by tools/register-text.py, the reader of every register
# tools/split-register.py splits, through `read` above as the register above
# is. A file the page names that is missing raises, for the same reason.
def _text_reader():
    import importlib.util
    here = os.path.dirname(os.path.abspath(__file__))
    spec = importlib.util.spec_from_file_location(
        "register_text", os.path.join(here, "register-text.py"))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


RT = _text_reader()


def proposals_text():
    return RT.register_text("docs/PROPOSALS.md", read)


def open_questions_text():
    return RT.register_text("docs/OPEN-QUESTIONS.md", read)


# What the owner has to have in front of him. The order is the order he can
# actually act in: the things needing nothing come first.
BUCKETS = [
    ("A DECISION - needs nothing but you", ("decision",)),
    ("ONE DOCUMENT - a numbered spec", ("document",)),
    ("REVIT OPEN", ("revit",)),
    ("THE PC - Windows, no Revit needed", ("windows",)),
    ("A NETWORK this container has not got", ("network",)),
    ("UNCLASSIFIED - read the row", ("other",)),
]


def classify(text):
    """Most specific first. `on the PC` is an explicit statement of what the row
    needs and beats an incidental mention of a model host further down it -
    A14 says "On the PC: python tests/test_context.py" and was filed under
    NETWORK until this order was fixed."""
    t = text.lower()
    if "revit" in t and "no revit" not in t:
        return "revit"
    if "on the pc" in t or "owner's pc" in t or "windows" in t or "setup.ps1" in t:
        return "windows"
    if "huggingface" in t or "sentence-transformers" in t or "reach a model" in t:
        return "network"
    if "document" in t and ("numbered" in t or "clause" in t):
        return "document"
    return "other"


def open_questions():
    """Same rule check-docs.py uses, so the two can never disagree - and
    the same text: the register as one, every tier's file back in place."""
    src = open_questions_text()
    if src is None:
        return []
    out = []
    for block in re.split(r"\n(?=### )", src):
        head = re.match(r"### (.*?)(Q-\d+[a-z]?)\s*—\s*(.*)", block)
        if not head:
            continue
        if "✅" in head.group(1):
            continue
        mark = re.search(r"\*\*Answer:\*?\*?", block)
        body = re.split(r"\n---", block[mark.end():])[0].strip() if mark else ""
        if len(body) > 5:
            continue
        title = head.group(3).split("*(")[0].strip()
        out.append((head.group(2), title, "decision", "OPEN-QUESTIONS.md"))
    return out


def needs_checking():
    """Rows that are not struck through, carrying their Group's own wording
    about what the row needs - the heading already says it."""
    src = needs_checking_text()
    if src is None:
        return []
    out, group = [], ""
    for line in src.split("\n"):
        g = re.match(r"^## (Group [A-Z].*)$", line)
        if g:
            group = g.group(1)
            continue
        # [A-Z], not [AR]. It was [AR] until 2026-09-15 and the queue was
        # showing 10 of 61 open rows: groups B, C, D, E, F, G, H, J and K
        # matched nothing and were dropped before `classify` ever saw them,
        # so they never even reached UNCLASSIFIED. That is the failure
        # NEEDS-CHECKING.md records against itself - "a count that quietly
        # omits a whole group is worse than no count" - committed by the
        # one tool whose whole job is not to do it.
        # ONE LETTER OR MORE - [A-Z]+, not [A-Z]. One letter hid every two-letter
        # group - AA, AB and on - until 2026-09-23: checks waiting on the owner that
        # never reached him, in owner-queue.py, check-gaps.py and balance-of-work.py
        # alike. FRAGMENT-ISSUES row 5b-156.
        m = re.match(r"^\|\s*(~~)?\*\*([A-Z]+\d+[a-z]?)\*\*", line)
        if not m:
            continue
        if m.group(1):                      # struck through = closed
            continue
        cells = [c.strip() for c in line.split("|")]
        what = cells[2] if len(cells) > 2 else ""
        what = re.sub(r"[`*~]", "", what)
        out.append((m.group(2), what[:110], classify(group + " " + line), "NEEDS-CHECKING.md"))

    # PROVE THE PATTERN SAW WHAT IS THERE. Counted independently of the
    # walk above, so a narrower pattern is reported rather than obeyed -
    # the lesson this file's own header records three times over.
    shaped = len(re.findall(r"(?m)^\|\s*\*\*[A-Z]+\d+[a-z]?\*\*", src))
    if len(out) != shaped:
        out.append(("!!", "%d open rows are ID-shaped in NEEDS-CHECKING.md and "
                          "this tool matched %d. The pattern cannot see them "
                          "all - fix it before trusting the list below."
                          % (shaped, len(out)), "other", "NEEDS-CHECKING.md"))
    return out


def proposals():
    src = proposals_text()
    if src is None:
        return []
    out = []
    for m in re.finditer(r"^### ([🔴🟠🟡🔵✅])\s*(F\d+)\.\s*(.*)$", src, re.M):
        if m.group(1) == "✅":
            continue
        out.append((m.group(2), m.group(3).strip()[:110], "decision", "PROPOSALS.md"))
    return out


def main():
    rows = open_questions() + needs_checking() + proposals()

    say("What is waiting on the OWNER")
    say("=" * 62)
    say("Derived from the registers just now. Nothing here is typed into a")
    say("document, so it cannot go stale - but each row is a SUMMARY, and the")
    say("register it names is the authority. Read the row before acting.")
    say()

    seen = 0
    for label, keys in BUCKETS:
        got = [r for r in rows if r[2] in keys]
        if not got:
            continue
        seen += len(got)
        say("%s  (%d)" % (label, len(got)))
        say("-" * 62)
        for ident, what, _k, where in got:
            say("  %-5s %-72s %s" % (ident, what, where))
        say()

    say("=" * 62)
    say("%d item(s) waiting on the owner, across %d register(s)."
          % (seen, len({r[3] for r in rows})))
    say()
    say("NOT COVERED HERE, because no register owns them as rows:")
    say("  - open defects in FRAGMENT-ISSUES.md - read section 5")
    say("  - anything in docs/work-notes/plans/ - those are schedules")
    # DERIVED, NOT TYPED. This line said "163 DRAFT fragments" from the day it
    # was written until 2026-09-15, by which time the real number was 62 - and
    # it sat two lines above its own instruction to derive it. A tool that
    # tells you to check a number it has just got wrong teaches the reader to
    # trust neither. It counts them itself now.
    draft = proven = 0
    for path in glob.glob(os.path.join(ROOT, "brain", "fragments",
                                       "*", "fragment.yaml")):
        try:
            with io.open(path, encoding="utf-8") as fh:
                head = fh.read(2048)
        except OSError:
            continue
        match = re.search(r"^heron-status:\s*(\S+)", head, re.M)
        if not match:
            continue
        if match.group(1) == "DRAFT":
            draft += 1
        elif match.group(1) in ("PROVEN", "PRODUCTION"):
            proven += 1
    say("  - the fragment library itself: %d DRAFT fragment(s) need a model, "
        "against %d proved." % (draft, proven))
    say("    Derive it yourself the same way:")
    say("    grep -h '^heron-status:' brain/fragments/*/fragment.yaml"
          " | sort | uniq -c")
    return 0


if __name__ == "__main__":
    sys.exit(main())
