# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-RPT-RND-002
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Report rendering - data in, document out, same input same output.

    python brain/heron_render.py

WHAT IT IS FOR (docs/28, HERON-RPT-RND-002)
--------------------------------------------
"Deterministic templates -> page, PDF, schedule, CSV, image. NO MODEL
CALL - data in, document out, same input same output." T1, risk READ.

The register put the guarantee in the row, so the file is built around
proving it rather than asserting it.

SAME INPUT, SAME OUTPUT - AND IT IS CHECKABLE FROM OUTSIDE
------------------------------------------------------------
Every answer carries a `sha` of its own content. Two renders of the same
data have the same one, and a caller that keeps it can tell whether a
document it is holding is still the document the data produces. That is
what makes determinism a property rather than a promise, and
HERON-RPT-VAL-004 is the agent that will use it.

Three things would break it quietly and none of them is here: no clock,
no random, and no reliance on the order a dict happened to be built in.
Columns are either DECLARED by the caller or derived as the sorted union
of every key seen, so the same rows in a different order render the same
document.

A FIGURE IS NEVER REFORMATTED INTO A DIFFERENT FIGURE
-------------------------------------------------------
No rounding, no thousands separators, no unit conversion, no truncation.
`0.30000000000000004` renders as `0.30000000000000004`, because a report
that quietly tidies a number is a report whose figures cannot be checked
against the query that produced them - and docs/28 gives
HERON-RPT-VAL-004 exactly that job: "a report claiming 47 failures when
there are 52 is worse than no report."

Tidying belongs upstream, where somebody decides it and it is visible.

NOTHING IS DROPPED SILENTLY
-----------------------------
Every row in comes out. A row missing a column renders an empty cell AND
is named in `gaps`, so "blank" and "we lost it" are different answers -
Golden Rule 14.

PDF AND IMAGE ARE RENDERED BY SOMEBODY ELSE
---------------------------------------------
This agent turns data into document CONTENT. Turning a page into a PDF
or a PNG is a renderer's job and it is not run here: the page comes
back, with `needs` naming what would convert it. Nothing in this file
executes anything, and a document that arrived by running a converter
would not be reproducible from the data alone anyway.
"""

from __future__ import annotations

import hashlib
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# docs/28's own five, in its order.
KINDS = ("page", "pdf", "schedule", "csv", "image")

# The two this agent produces content for but does not convert.
CONVERTED = {"pdf": "a PDF writer", "image": "an image renderer"}


def _cell(value):
    """A value as text, unchanged. No rounding and no separators."""
    if value is None:
        return ""
    if value is True:
        return "true"
    if value is False:
        return "false"
    return str(value)


def _csv(text):
    """One field, quoted only when it must be."""
    if any(mark in text for mark in (",", '"', "\n", "\r")):
        return '"%s"' % text.replace('"', '""')
    return text


def render(kind, rows, columns=None, title=None):
    """
    {content, kind, sha, gaps, why} - or a refusal.

    Nothing is executed and nothing is written. The same rows give the
    same content and the same sha, every time.
    """
    kind = str(kind or "").strip().lower()
    if kind not in KINDS:
        return {"rendered": False, "refused": "NOT_A_KIND",
                "why": "'%s' is not one of %s. docs/28 names five and a "
                       "sixth is not invented here - each one is a template, "
                       "and a template nobody wrote cannot be deterministic."
                       % (kind, ", ".join(KINDS))}

    if not rows:
        return {"rendered": False, "refused": "NOTHING_TO_RENDER",
                "why": "no rows were given. An empty document reporting "
                       "nothing found is a statement about the data, and "
                       "this is a statement about the call - they must not "
                       "share an answer."}

    seen = []
    for row in rows:
        if not isinstance(row, dict):
            return {"rendered": False, "refused": "NOT_A_ROW",
                    "why": "%r is not a row. Each is a map of column to "
                           "value." % (row,)}
        seen.append(row)

    if columns is None:
        # DERIVED, and sorted: the same rows in a different order must give
        # the same document, and dict order is not a fact about the data.
        columns = sorted(set(str(key) for row in seen for key in row))
    else:
        columns = [str(each) for each in columns]
    if not columns:
        return {"rendered": False, "refused": "NOTHING_TO_RENDER",
                "why": "the rows carry no columns, so there is nothing to "
                       "put in a document."}

    gaps = []
    table = []
    for index, row in enumerate(seen):
        cells = []
        for column in columns:
            if column not in row:
                gaps.append({"row": index, "column": column,
                             "why": "not in this row. Rendered as an empty "
                                    "cell AND named here, so 'blank' and "
                                    "'we lost it' are different answers - "
                                    "Golden Rule 14."})
            cells.append(_cell(row.get(column)))
        table.append(cells)

    heading = str(title or "").strip()
    if kind == "csv":
        lines = [",".join(_csv(each) for each in columns)]
        lines += [",".join(_csv(cell) for cell in cells) for cells in table]
        content = "\n".join(lines) + "\n"
    elif kind == "schedule":
        width = [max(len(columns[i]),
                     *(len(cells[i]) for cells in table))
                 for i in range(len(columns))]
        lines = ["  ".join(columns[i].ljust(width[i])
                           for i in range(len(columns))).rstrip()]
        lines.append("  ".join("-" * width[i] for i in range(len(columns))))
        for cells in table:
            lines.append("  ".join(cells[i].ljust(width[i])
                                   for i in range(len(columns))).rstrip())
        content = "\n".join(lines) + "\n"
    else:
        # page, and the content a pdf or an image would be made from.
        lines = ["# %s" % heading] if heading else []
        lines.append("| %s |" % " | ".join(columns))
        lines.append("|%s|" % "|".join("---" for _ in columns))
        for cells in table:
            lines.append("| %s |" % " | ".join(cells))
        content = "\n".join(lines) + "\n"

    answer = {
        "rendered": True, "kind": kind, "content": content,
        "columns": columns, "rows": len(table), "gaps": gaps,
        "sha": hashlib.sha256(content.encode("utf-8")).hexdigest(),
        "why": "%d row(s) and %d column(s) as %s. %s"
               % (len(table), len(columns), kind,
                  "No clock, no random, and no dict order - the same rows "
                  "give the same sha every time."),
        "unjudged": [
            "NOTHING WAS EXECUTED AND NOTHING WAS WRITTEN. No model was "
            "called: this is a template, and docs/28 puts 'no model call' "
            "in the row itself.",
            "NO FIGURE WAS REFORMATTED. No rounding, no separators, no unit "
            "conversion - a report that quietly tidies a number is one "
            "whose figures cannot be checked against the query that "
            "produced them, which is HERON-RPT-VAL-004's whole job.",
            "%s" % ("%d cell(s) were empty because the column was not in "
                    "the row, and each is named in `gaps`. Blank and lost "
                    "are different answers." % len(gaps) if gaps else
                    "every row carried every column."),
            "WHETHER THE DATA IS RIGHT IS NOT JUDGED HERE. This turns rows "
            "into a document; whether the rows answer the question is "
            "HERON-RPT-VAL-004's, and whether the document may leave the "
            "machine is HERON-RPT-RED-003's.",
        ],
    }
    if kind in CONVERTED:
        answer["needs"] = CONVERTED[kind]
        answer["of"] = "page"
        answer["why"] = ("%s The content is the PAGE; converting it needs "
                         "%s, which is not run here - a document that "
                         "arrived by running a converter would not be "
                         "reproducible from the data alone."
                         % (answer["why"], CONVERTED[kind]))
    return answer


def main(argv):
    print("REPORT RENDERING   data in, document out, same input same output")
    print("=" * 72)

    rows = [{"element": "Duct 418302", "size": "300x300", "fails": 2},
            {"element": "Duct 418303", "size": "250x250", "fails": 0},
            {"element": "Duct 418304", "fails": 11, "note": "no size set"}]

    for kind in KINDS:
        answer = render(kind, rows, title="Duct check")
        print("\n--- %s ---%s" % (kind,
                                  "  (needs %s)" % answer["needs"]
                                  if answer.get("needs") else ""))
        print(answer["content"].rstrip())

    answer = render("csv", rows)
    print("\nsha  %s" % answer["sha"])
    print("again %s" % render("csv", rows)["sha"])
    print("gaps  %s" % ", ".join("row %d has no %s" % (g["row"], g["column"])
                                 for g in answer["gaps"]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
