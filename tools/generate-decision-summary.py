# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""Rebuild the Status summary table in DECISIONS.md from the decisions themselves.

WHY THIS EXISTS
---------------
The table was written by hand and stopped at D-50 while the file went on to
D-70. Twenty decisions were missing from the index of decisions, including
D-70, which the owner had answered the day before. Nobody removed them; the
table simply stopped being updated, and no gate could tell.

That is the same defect found three other times on 2026-09-12 - a count typed
once and never re-derived - and PROPOSALS F3 had recorded it as "stops at D-50"
without anybody measuring how far behind it was.

WHAT IT PRESERVES, AND WHY THAT MATTERS
---------------------------------------
The status cells carry information that is NOT derivable from the decision:
"read back 2026-09-06" records a conversation, not a fact on disk. So an
existing row's status cell is kept VERBATIM. Only rows that do not exist yet
are built, from the decision's own `**Status:**` and `**Date:**` line.

This is the difference between generating a file and overwriting one. A
generator that discarded those dates would lose the record of every read-back
the owner has ever done, and it would look like tidying.

USAGE
-----
    python tools/generate-decision-summary.py            rewrite in place
    python tools/generate-decision-summary.py --check    exit 1 if stale

`--check` is what CI runs. It writes nothing.
"""
import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PATH = os.path.join(ROOT, "docs", "DECISIONS.md")

HEADING = re.compile(r"^## (D-\d+)\s+—\s+(.+?)\s*$")
STATUS = re.compile(r"^\*\*Status:\*\*\s*(.+?)\s*(?:·|$)")
DATE = re.compile(r"\*\*Date:\*\*\s*([0-9]{4}-[0-9]{2}-[0-9]{2})")
ROW = re.compile(r"^\|\s*\[(D-\d+)\]\([^)]*\)\s*\|(.*)\|(.*)\|\s*$")


def w(s):
    """stdout that survives a cp1252 console - the owner runs this on Windows."""
    sys.stdout.write(s.encode("ascii", "replace").decode("ascii"))


def anchor_of(text):
    """GitHub's anchor rule, spelled exactly as tools/check-docs.py spells it.

    Runs of spaces are NOT collapsed, which is what makes ' - ' produce two
    hyphens. Getting this wrong silently breaks every link in the table.
    """
    slug = re.sub(r"[^\w\s-]", "", text.lower())
    return re.sub(r"\s", "-", slug)


def main():
    check_only = "--check" in sys.argv
    src = io.open(PATH, encoding="utf-8").read()
    lines = src.split("\n")

    # --- the decisions themselves ---
    decisions = []                      # (id, title, derived status)
    for i, line in enumerate(lines):
        m = HEADING.match(line)
        if not m:
            continue
        status, date = "", ""
        for probe in lines[i + 1:i + 7]:
            if probe.startswith("## "):
                break
            s = STATUS.match(probe)
            if s and not status:
                status = s.group(1).strip()
            d = DATE.search(probe)
            if d and not date:
                date = d.group(1)
        mark = {"Accepted": "✅", "Proposed": "⏳", "Fulfilled": "✔",
                "Superseded": "↩"}.get(status.split()[0] if status else "", "•")
        derived = "%s %s" % (mark, status or "status not stated")
        if date:
            derived += " · %s" % date
        decisions.append((m.group(1), m.group(2), derived))

    if not decisions:
        w("No decisions found - DECISIONS.md has moved under this tool. Stopping.\n")
        return 1

    # --- the table as it stands, so curated status cells survive ---
    start = end = None
    for i, line in enumerate(lines):
        if line.strip() == "## Status summary":
            start = i
        elif start is not None and line.startswith("## "):
            end = i
            break
    if start is None:
        w("No '## Status summary' heading. Stopping rather than guessing where it goes.\n")
        return 1
    end = end if end is not None else len(lines)

    existing = {}
    for line in lines[start:end]:
        m = ROW.match(line)
        if m:
            existing[m.group(1)] = m.group(3).strip()

    # --- rebuild ---
    table = [
        "## Status summary",
        "",
        "> **Generated — do not edit this table by hand.** `python tools/generate-decision-summary.py`",
        "> rebuilds it from the decisions below, and CI fails if it is stale. It was hand-written until",
        "> 2026-09-12, by which time it stopped at **D-50** while the file had reached **D-70** — twenty",
        "> decisions missing from the index of decisions, with nothing able to notice.",
        ">",
        "> **A status cell is kept verbatim once written.** *\"read back 2026-09-06\"* records a",
        "> conversation, not a fact on disk, so the generator never overwrites one — it only fills in",
        "> rows that do not exist yet.",
        "",
        "| # | Decision | Status |",
        "|---|---|---|",
    ]
    added = []
    for ident, title, derived in decisions:
        anchor = anchor_of("%s — %s" % (ident, title))
        status = existing.get(ident)
        if status is None:
            status = derived
            added.append(ident)
        table.append("| [%s](#%s) | %s | %s |" % (ident, anchor, title, status))
    table.append("")

    out = "\n".join(lines[:start] + table + lines[end:])

    if out == src:
        w("Status summary is current: %d decision(s), none missing.\n" % len(decisions))
        return 0

    if check_only:
        w("STALE: the Status summary does not match the decisions.\n")
        if added:
            w("  missing from the table: %s\n" % ", ".join(added))
        w("  Fix: python tools/generate-decision-summary.py\n")
        return 1

    io.open(PATH, "w", encoding="utf-8", newline="").write(out)
    w("Rewrote the Status summary: %d decision(s).\n" % len(decisions))
    if added:
        w("  added %d missing row(s): %s\n" % (len(added), ", ".join(added)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
