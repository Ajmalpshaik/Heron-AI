# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-RPT-VAL-004
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Report validation - does the report say what the data says.

    python brain/heron_report.py

WHAT IT IS FOR (docs/28, HERON-RPT-VAL-004)
--------------------------------------------
"Does the report say what the data says. Counts match the query, nothing
dropped silently, every figure traceable to its source. A REPORT
CLAIMING 47 FAILURES WHEN THERE ARE 52 IS WORSE THAN NO REPORT." T2,
risk READ.

That last sentence is the reason the agent exists, and it is worth
saying why: a wrong report is not a missing report with a defect. It is
a confident answer somebody acts on, and it removes the doubt that would
otherwise have made them check.

FOUR CHECKS, AND THE FIRST ONE IS FREE
----------------------------------------
    IT IS WHAT THE DATA MAKES     HERON-RPT-RND-002 is deterministic and
                                  hands out a sha of its own content. So
                                  the data is re-rendered here and the
                                  two are compared. Anything edited
                                  after rendering fails, whatever the
                                  edit was - and it costs one hash.

    THE COUNT MATCHES THE QUERY   The register's own example. If the
                                  query says 52 and the report holds 47
                                  rows, the report is wrong even though
                                  every one of its rows is right.

    NOTHING WAS DROPPED SILENTLY  RND-002 names every empty cell in
                                  `gaps`. A report with blanks and no
                                  gaps declared is one that lost
                                  something quietly, which is the
                                  failure Golden Rule 14 is about.

    EVERY FIGURE HAS A SOURCE     Every number in the document must come
                                  from the data, the column names or the
                                  counts. This one works on a report
                                  NOBODY RENDERED HERE - a hand-written
                                  summary, or one a model wrote - which
                                  is exactly where a figure with no
                                  source turns up.

WHY THE FOURTH IS NOT COVERED BY THE FIRST
--------------------------------------------
The first check only applies to a report that carries a sha. A report
assembled anywhere else has none, and that is the report most likely to
contain a number somebody was sure about. So the fourth runs on
everything, tokenised so that 47 inside 1478 is not a match.

WHAT IT CANNOT DO
-------------------
It cannot tell whether the DATA is right. A report that faithfully
reports wrong data passes every check here, and the answer says so
rather than implying a verdict it did not make.
"""

from __future__ import annotations

import hashlib
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_render as RENDER  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# A number as it appears in a document. Tokenised on boundaries so that 47
# inside 1478 is not a match, and the sign and decimals travel with it.
NUMBER = re.compile(r"(?<![\w.])-?\d+(?:\.\d+)?(?:[eE][-+]?\d+)?(?![\w.])")


def _figures(text):
    """Every number-looking token in a document, in order."""
    return NUMBER.findall(str(text or ""))


def _sources(rows, columns, counts):
    """Every figure the data can account for."""
    known = set()
    for row in rows:
        for value in row.values():
            known.update(_figures(value))
            known.add(str(value))
    for column in (columns or []):
        known.update(_figures(column))
    for count in counts:
        known.add(str(count))
    return known


def validate(report, rows, query=None):
    """
    {sound, findings, why, unjudged} - or a refusal.

    Nothing is changed. `rows` is the data the report claims to be
    about, handed in.
    """
    if not isinstance(report, dict) or not str(
            report.get("content") or "").strip():
        return {"checked": False, "refused": "NOT_A_REPORT",
                "why": "a report is the answer HERON-RPT-RND-002 returns, "
                       "or at least a map carrying `content`. %s is not "
                       "one, and a report nobody can read cannot be checked "
                       "against anything."
                       % (type(report).__name__ if not isinstance(
                           report, dict) else "one with no content")}

    if not rows:
        return {"checked": False, "refused": "NOTHING_TO_CHECK",
                "why": "no data was handed in, so there is nothing to check "
                       "the report against. A report validated against "
                       "nothing would pass, which is worse than not "
                       "checking it."}
    for row in rows:
        if not isinstance(row, dict):
            return {"checked": False, "refused": "NOT_A_ROW",
                    "why": "%r is not a row of data." % (row,)}

    content = str(report["content"])
    findings = []

    # 1. IT IS WHAT THE DATA MAKES - free, because RND-002 is deterministic.
    remade = None
    if report.get("sha") and report.get("kind"):
        # THE TITLE TOO. A heading is part of what RND-002 makes, and
        # re-rendering without it made every legitimately titled report
        # come back CONTENT_DOES_NOT_MATCH_THE_DATA - the finding fired
        # on the presence of a title rather than on anything wrong.
        remade = RENDER.render(report["kind"], rows,
                               columns=report.get("columns"),
                               title=report.get("title"))
        if remade.get("refused"):
            findings.append({
                "finding": "CONTENT_DOES_NOT_MATCH_THE_DATA",
                "why": "the data will not render as %s at all (%s), so the "
                       "report cannot be what this data makes."
                       % (report["kind"], remade["refused"])})
        else:
            # THE CONTENT, NOT THE CLAIMED sha. Comparing the two hashes
            # let an EDITED document through on its first run: the edit
            # changed the content and left `sha` alone, so the claim still
            # matched the re-render. A stale hash is exactly what somebody
            # editing a document would leave behind.
            actual = hashlib.sha256(
                content.encode("utf-8")).hexdigest()
            if actual != report["sha"]:
                findings.append({
                    "finding": "SHA_DOES_NOT_DESCRIBE_THE_CONTENT",
                    "claimed": report["sha"][:16], "actual": actual[:16],
                    "why": "the report's own `sha` is not the hash of its "
                           "own content, so it was changed after it was "
                           "rendered and the hash was left behind. A stale "
                           "hash is what an edit leaves, which is why the "
                           "check below compares CONTENT rather than the "
                           "two hashes."})
            if remade["content"] != content:
                findings.append({
                    "finding": "CONTENT_DOES_NOT_MATCH_THE_DATA",
                    "remade": remade["sha"][:16],
                    "why": "re-rendering this data gives a different "
                           "document. HERON-RPT-RND-002 is deterministic, "
                           "so the report was changed after it was "
                           "rendered, or it is about different data. "
                           "Either way it is not what this data makes."})

    # 2. THE COUNT MATCHES THE QUERY - the register's own example.
    counts = []
    claimed = (query or {}).get("found") if isinstance(query, dict) else None
    if claimed is not None:
        counts.append(claimed)
        try:
            claimed = int(claimed)
        except (TypeError, ValueError):
            claimed = None
    if claimed is not None and claimed != len(rows):
        findings.append({
            "finding": "COUNT_DOES_NOT_MATCH",
            "query": claimed, "data": len(rows),
            "why": "the query found %d and %d row(s) were handed in. A "
                   "report claiming %d failures when there are %d is worse "
                   "than no report - docs/28's own words - because every "
                   "row in it can be right while the report is wrong."
                   % (claimed, len(rows), min(claimed, len(rows)),
                      max(claimed, len(rows)))})
    if report.get("rows") is not None and report["rows"] != len(rows):
        findings.append({
            "finding": "COUNT_DOES_NOT_MATCH",
            "report": report["rows"], "data": len(rows),
            "why": "the report says it holds %d row(s) and %d were handed "
                   "in." % (report["rows"], len(rows))})
    counts.append(len(rows))
    if report.get("rows") is not None:
        counts.append(report["rows"])

    # 3. NOTHING WAS DROPPED SILENTLY.
    short = sum(1 for row in rows
                for column in (report.get("columns") or [])
                if column not in row)
    declared = len(report.get("gaps") or [])
    if short and declared != short:
        findings.append({
            "finding": "GAPS_NOT_DECLARED",
            "empty": short, "declared": declared,
            "why": "%d cell(s) have no value in the data and %d are "
                   "declared in `gaps`. A report with blanks and no gaps "
                   "declared lost something quietly, and Golden Rule 14 is "
                   "about exactly that." % (short, declared)})

    # 4. EVERY FIGURE HAS A SOURCE - and this one runs on a report nobody
    #    rendered here, which is where an invented number turns up.
    known = _sources(rows, report.get("columns"), counts)
    unsourced = sorted(set(figure for figure in _figures(content)
                           if figure not in known))
    if unsourced:
        findings.append({
            "finding": "A_FIGURE_WITH_NO_SOURCE",
            "figures": unsourced,
            "why": "%s appear(s) in the document and in none of the data, "
                   "the column names or the counts. Tokenised on "
                   "boundaries, so 47 inside 1478 is not a match - these "
                   "really are unaccounted for."
                   % ", ".join("'%s'" % each for each in unsourced[:5])})

    return {
        "checked": True, "sound": not findings, "findings": findings,
        "rows": len(rows), "figures": len(_figures(content)),
        "remade": remade["sha"] if remade and not remade.get("refused")
        else None,
        "why": "%d row(s), %d figure(s) in the document: %s"
               % (len(rows), len(_figures(content)),
                  "nothing found." if not findings else
                  "%d finding(s) - %s."
                  % (len(findings),
                     ", ".join(sorted(set(one["finding"]
                                          for one in findings))))),
        "unjudged": [
            "WHETHER THE DATA IS RIGHT IS NOT CHECKED AND CANNOT BE. A "
            "report that faithfully reports wrong data passes every check "
            "here. What was checked is whether the REPORT says what the "
            "DATA says.",
            "%s" % ("THE DOCUMENT WAS RE-RENDERED AND COMPARED AS "
                    "CONTENT, not as two hashes - an edit leaves the old "
                    "hash behind, so comparing the claims would have let "
                    "one through. HERON-RPT-RND-002 being deterministic is "
                    "what makes the re-render worth doing."
                    if remade is not None else
                    "THE REPORT CARRIES NO sha OR NO kind, so it could not "
                    "be re-rendered and compared. That is the report most "
                    "likely to hold a number somebody was sure about, "
                    "which is why the figure check below runs on "
                    "everything."),
            "EVERY FIGURE IN THE DOCUMENT WAS LOOKED FOR IN THE DATA, the "
            "column names and the counts - %d of them. A figure that is "
            "not there is unaccounted for, not wrong: the data may simply "
            "not have been handed in whole." % len(_figures(content)),
            "NOTHING WAS CHANGED. This agent reads a report and the data "
            "behind it; correcting either is somebody else's, and "
            "HERON-RPT-RED-003 is the gate that decides whether it may "
            "leave at all.",
        ],
    }


def main(argv):
    print("REPORT VALIDATION   does the report say what the data says")
    print("=" * 72)

    rows = [{"element": "Duct 1", "fails": 2},
            {"element": "Duct 2", "fails": 0},
            {"element": "Duct 3", "fails": 11}]
    report = RENDER.render("csv", rows)

    honest = validate(report, rows, query={"found": 3})
    print("\nhonest report: %s" % honest["why"])

    print("\nand four ways to be wrong")
    edited = dict(report, content=report["content"].replace("11", "1"))
    for name, args in (
            ("edited after rendering", (edited, rows, {"found": 3})),
            ("query found 52", (report, rows, {"found": 52})),
            ("a figure from nowhere",
             ({"content": "47 failures were found\n"}, rows, None)),
            ("blanks with no gaps",
             ({"content": "x", "columns": ["element", "fails", "note"],
               "gaps": []}, rows, None))):
        answer = validate(*args)
        print("\n  %-24s %s" % (name, answer["why"]))
        for one in answer["findings"]:
            print("      %-32s %s" % (one["finding"], one["why"][:44]))

    print("\nwhat this agent does not judge")
    for line in honest["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
