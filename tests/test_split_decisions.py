# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
tools/split-decisions.py gives every decision its own file and keeps
DECISIONS.md as an index that every tool still reads the same way.

    python tests/test_split_decisions.py

Every case runs against a SMALL DECISIONS.md BUILT HERE, never the real one.

WHAT IS PROVED
--------------
  1. each decision lands in docs/decisions/D-NN.md with its words unchanged and
     its links re-pointed - a relative link one folder deeper, a link to another
     decision back to its heading in the index, a link to a heading inside a
     decision to the file that heading went to;
  2. the index keeps each decision's heading and, in the six lines under it, the
     Status and Date that tools/generate-decision-summary.py reads - and a link
     to the full record; everything that is not a decision stays as it was;
  3. a second run changes nothing;
  4. it refuses, writing nothing, when a record file already exists for a
     decision the index still holds in full, and when another document links to
     a heading inside a decision.
"""

import importlib.util
import io
import os
import re
import shutil
import sys
import tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)

spec = importlib.util.spec_from_file_location("split_decisions", os.path.join(ROOT, "tools", "split-decisions.py"))
TOOL = importlib.util.module_from_spec(spec)
spec.loader.exec_module(TOOL)

NL = chr(10)
DASH = chr(0x2014)
FAILURES = []


def check(ok, said):
    print("  %s  %s" % ("ok  " if ok else "FAIL", said))
    if not ok:
        FAILURES.append(said)


def decisions_text():
    return NL.join([
        "# Decision log",
        "",
        "Preamble, with [a rule](14-golden-rules.md).",
        "",
        "## Status summary",
        "",
        "| Decision | Status |",
        "|---|---|",
        "| [D-01](#d-01--the-first-one) | Accepted |",
        "",
        "## D-01 " + DASH + " The first one",
        "",
        "**Status:** Accepted " + chr(0xB7) + " **Date:** 2026-08-01 " + chr(0xB7) + " **Answers:** [Q-1](OPEN-QUESTIONS.md)",
        "",
        "### Why the first one",
        "",
        "Because [the tool](../tools/x.py) said so, and [the second](#d-02--the-second-one) agrees.",
        "",
        "### What was decided",
        "",
        "We do X. See [why](#why-the-first-one).",
        "",
        "```markdown",
        "## An example heading inside a code block",
        "```",
        "",
        "Still the first decision.",
        "",
        "## D-02 " + DASH + " The second one",
        "",
        "**Status:** Proposed",
        "**Date:** 2026-08-02",
        "",
        "It builds on [what was decided](#what-was-decided).",
        "",
    ])


def fresh(other=None, existing=False):
    top = tempfile.mkdtemp(prefix="heron-decisions-test-")
    docs = os.path.join(top, "docs")
    os.makedirs(os.path.join(docs, "decisions") if existing else docs)
    path = os.path.join(docs, "DECISIONS.md")
    with io.open(path, "wb") as out:
        out.write(decisions_text().encode("utf-8"))
    if other:
        with io.open(os.path.join(docs, "OTHER.md"), "wb") as out:
            out.write(other.encode("utf-8"))
    if existing:
        with io.open(os.path.join(docs, "decisions", "D-01.md"), "wb") as out:
            out.write(b"# someone was here" + NL.encode("ascii"))
    return top, path, os.path.join(docs, "decisions")


def text(path):
    with io.open(path, encoding="utf-8") as handle:
        return handle.read()


def main():
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(errors="replace")
    top, path, folder = fresh()
    try:
        print("1. each decision gets its own file")
        p = TOOL.plan(path=path, folder=folder, today="2026-09-23")
        check(p.fatal is None and not p.problems, "the plan is clean (%s)" % (p.fatal or "; ".join(p.problems) or "no problems"))
        check(TOOL.write(p) == TOOL.OK and sorted(os.listdir(folder)) == ["D-01.md", "D-02.md"], "two files, named by number")
        one, two = text(os.path.join(folder, "D-01.md")), text(os.path.join(folder, "D-02.md"))
        check(one.startswith("# D-01 " + DASH + " The first one"), "the file opens with the decision's own title")
        check("We do X." in one and "### Why the first one" in one, "its words and its sections are there")
        check("## An example heading inside a code block" in one and "Still the first decision." in one,
              "a ## line inside a code block does not end the decision")
        check("[the tool](../../tools/x.py)" in one, "a relative link is re-pointed one folder deeper")
        check("[the second](../DECISIONS.md#d-02--the-second-one)" in one,
              "a link to another decision goes to its heading in the index")
        check("[why](#why-the-first-one)" in one, "a link to its own section stays in the file")
        check("[what was decided](D-01.md#what-was-decided)" in two,
              "a link to a section of another decision follows that section to its file")
        check("[Q-1](../OPEN-QUESTIONS.md)" in one, "the Status line's own link is re-pointed in the record")

        print("")
        print("2. the index keeps what every tool reads")
        index = text(path).split(NL)
        for ident, status, date in (("D-01", "**Status:** Accepted", "2026-08-01"), ("D-02", "**Status:** Proposed", "2026-08-02")):
            at = next(i for i, l in enumerate(index) if l.startswith("## " + ident))
            window = NL.join(index[at + 1:at + 7])
            check(status in window and date in window,
                  "%s keeps its Status and Date in the six lines generate-decision-summary reads" % ident)
            check(("[`decisions/%s.md`](decisions/%s.md)" % (ident, ident)) in NL.join(index[at:at + 8]),
                  "%s links to its full record" % ident)
        joined = NL.join(index)
        check("We do X." not in joined and "### Why the first one" not in joined, "the bodies left the index")
        check("Preamble, with [a rule](14-golden-rules.md)." in joined and "| [D-01](#d-01--the-first-one) | Accepted |" in joined,
              "what is not a decision stays as it was")
        check(set(re.findall("^## (D-[0-9]+)", joined, re.M)) == {"D-01", "D-02"},
              "check-docs still finds every decision defined")

        print("")
        print("3. a second run changes nothing")
        kept = text(path)
        again = TOOL.plan(path=path, folder=folder, today="2026-09-24")
        check(not again.decisions and not again.problems and TOOL.write(again) == TOOL.OK and text(path) == kept,
              "nothing left to split, not one byte changed")
    finally:
        shutil.rmtree(top)

    print("")
    print("4. what it cannot do safely stops it")
    top, path, folder = fresh(existing=True)
    try:
        kept = text(path)
        p = TOOL.plan(path=path, folder=folder, today="2026-09-23")
        check(any("D-01.md already exists" in x for x in p.problems) and TOOL.write(p) == TOOL.REFUSED
              and text(path) == kept, "an existing record file is never overwritten")
    finally:
        shutil.rmtree(top)
    top, path, folder = fresh(other="See [why](DECISIONS.md#why-the-first-one)." + NL)
    try:
        p = TOOL.plan(path=path, folder=folder, today="2026-09-23")
        check(any("OTHER.md links to #why-the-first-one" in x for x in p.problems) and TOOL.write(p) == TOOL.REFUSED
              and not os.path.isdir(folder), "a link from another document into a decision's section stops it")
    finally:
        shutil.rmtree(top)

    print("")
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        return 1
    print("PASSED")
    return 0


if __name__ == "__main__":
    sys.exit(main())
