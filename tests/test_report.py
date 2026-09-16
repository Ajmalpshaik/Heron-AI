# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-RPT-VAL-004
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Report validation - does the report say what the data says.

    python tests/test_report.py

WHAT IT PROVES
  1. AN HONEST REPORT PASSES, so the checks below are not simply strict.

  2. AN EDIT AFTER RENDERING IS CAUGHT - including the one that let it
     through on its first run, where the content changed and the `sha`
     was left behind.

  3. THE REGISTER'S OWN EXAMPLE IS CAUGHT: a report of 47 when the query
     found 52.

  4. A FIGURE WITH NO SOURCE IS CAUGHT ON A REPORT NOBODY RENDERED
     HERE - and 47 inside 1478 is not a match.

  5. BLANKS WITH NO GAPS DECLARED ARE CAUGHT.

  6. IT DOES NOT CLAIM THE DATA IS RIGHT.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_report as VAL                                     # noqa: E402
import heron_render as RND                                     # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def found(answer, name):
    return [one for one in answer.get("findings", [])
            if one["finding"] == name]


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_report.py"),
                    encoding="utf-8").read()
    code = whole.split("\nfrom __future__", 1)[1]
    logic = code.split("\ndef main(")[0]

    def ask(report, rows, **kw):
        answer = VAL.validate(report, rows, **kw)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    rows = [{"element": "Duct 1", "fails": 2},
            {"element": "Duct 2", "fails": 0},
            {"element": "Duct 3", "fails": 11}]
    report = RND.render("csv", rows)

    print("1. An honest report passes")
    honest = ask(report, rows, query={"found": 3})
    check(honest["sound"] is True, "nothing is found in a true report")
    check(honest["findings"] == [], "the findings list is empty")
    check(honest["rows"] == 3 and honest["figures"] > 0,
          "and it says what it compared: %d rows, %d figures"
          % (honest["rows"], honest["figures"]))
    check(honest["remade"] == report["sha"],
          "the re-render matched, by content")
    for kind in RND.KINDS:
        other = RND.render(kind, rows)
        check(ask(other, rows)["sound"] is True,
              "  and a true %s report passes too" % kind)

    print("\n2. An edit after rendering is caught")
    # THE ONE THAT GOT THROUGH: content changed, `sha` left behind.
    edited = dict(report, content=report["content"].replace("11", "1"))
    caught = ask(edited, rows)
    check(not caught["sound"], "an edited document is not sound")
    check(found(caught, "SHA_DOES_NOT_DESCRIBE_THE_CONTENT"),
          "the stale hash is its own finding - that is what an edit leaves")
    check(found(caught, "CONTENT_DOES_NOT_MATCH_THE_DATA"),
          "and the content does not match the data")
    check("compares CONTENT rather than the two hashes"
          in found(caught, "SHA_DOES_NOT_DESCRIBE_THE_CONTENT")[0]["why"],
          "the finding says why hashes are not compared to each other")
    # And an edit that updates the hash honestly is STILL caught by the data.
    import hashlib
    honest_edit = dict(report, content=edited["content"],
                       sha=hashlib.sha256(
                           edited["content"].encode("utf-8")).hexdigest())
    still = ask(honest_edit, rows)
    check(not found(still, "SHA_DOES_NOT_DESCRIBE_THE_CONTENT"),
          "an edit that re-hashes honestly passes the hash check")
    check(found(still, "CONTENT_DOES_NOT_MATCH_THE_DATA"),
          "and is still caught by the data, which is the check that "
          "matters")
    different = ask(report, rows[:2])
    check(found(different, "CONTENT_DOES_NOT_MATCH_THE_DATA"),
          "a report about different data is caught the same way")

    print("\n3. The register's own example")
    miscounted = ask(report, rows, query={"found": 52})
    one = found(miscounted, "COUNT_DOES_NOT_MATCH")
    check(one, "the query found 52 and three rows arrived")
    check("worse than no report" in one[0]["why"],
          "and the finding quotes docs/28: worse than no report")
    check(one[0]["query"] == 52 and one[0]["data"] == 3,
          "with both numbers, so it can be acted on")
    check(ask(report, rows, query={"found": 3})["sound"],
          "while a matching count is sound")
    check(ask(report, rows, query={"found": "not a number"})["sound"],
          "and a count that is not a number is not treated as zero")
    lying = ask(dict(report, rows=99), rows)
    check(found(lying, "COUNT_DOES_NOT_MATCH"),
          "a report claiming 99 rows over three is caught without any query")

    print("\n4. A figure with no source, on a report nobody rendered")
    invented = ask({"content": "47 failures were found\n"}, rows)
    figures = found(invented, "A_FIGURE_WITH_NO_SOURCE")
    check(figures and figures[0]["figures"] == ["47"],
          "'47' comes from nowhere and is named")
    check(invented["remade"] is None,
          "and the report carries no sha, so nothing could be re-rendered "
          "- which is why this check runs on everything")
    check(ask({"content": "1478 things\n"}, rows)["sound"] is False,
          "1478 is also unaccounted for here")
    inside = ask({"content": "1478 things\n"},
                 [{"n": 1478}])
    check(inside["sound"] is True,
          "but when 1478 IS in the data it is accounted for")
    check(ask({"content": "47 things\n"}, [{"n": 1478}])["sound"] is False,
          "and 47 is NOT matched by 1478 - the tokens have boundaries")
    check(ask({"content": "2 and 0 and 11\n"}, rows)["sound"] is True,
          "every figure that is in the data passes")
    check(ask({"content": "no numbers at all\n"}, rows)["sound"] is True,
          "and a document with no figures has none to account for")

    print("\n5. Blanks with no gaps declared")
    ragged = [{"a": 1, "b": 2}, {"a": 3}]
    rendered = RND.render("csv", ragged)
    check(len(rendered["gaps"]) == 1, "RND-002 declares the one gap")
    check(ask(rendered, ragged)["sound"] is True,
          "and a report that declares it is sound")
    hidden = dict(rendered, gaps=[])
    check(found(ask(hidden, ragged), "GAPS_NOT_DECLARED"),
          "one that declares none is caught")
    check(found(ask(hidden, ragged), "GAPS_NOT_DECLARED")[0]["empty"] == 1,
          "with how many cells were actually empty")

    print("\n6. It does not claim the data is right")
    check(any("cannot be" in line.lower() and "data is right" in line.lower()
              for line in honest["unjudged"]),
          "the answer says whether the data is right is not checked")
    check(any("faithfully reports wrong data" in line
              for line in honest["unjudged"]),
          "and that a faithful report of wrong data passes everything")
    check(len(honest["unjudged"]) == 4, "four things are left unjudged")
    for changing in ("open(", "write(", "os.remove", "subprocess"):
        check(changing not in logic, "and the agent never uses %s" % changing)

    print("\n7. Every failure is named and reached")
    for bad, why in ((None, "None is not a report"),
                     ("a report", "a string is not one"),
                     ({}, "a map with no content"),
                     ({"content": "  "}, "and one whose content is blank")):
        check(ask(bad, rows).get("refused") == "NOT_A_REPORT", why)
    check(ask(report, None).get("refused") == "NOTHING_TO_CHECK",
          "no data is refused - a report validated against nothing passes")
    check(ask(report, []).get("refused") == "NOTHING_TO_CHECK",
          "and so is an empty list")
    check(ask(report, ["not a row"]).get("refused") == "NOT_A_ROW",
          "a row that is not a map is refused")
    print()
    print("R. THE SECOND CODEX REVIEW - a report is allowed a title")
    made = RND.render("page", rows, title="Clashes by level")
    answer = VAL.validate({"content": made["content"], "sha": made["sha"],
                           "kind": "page", "columns": made["columns"],
                           "title": made["title"]}, rows)
    check(not [one for one in (answer.get("findings") or [])
               if one["finding"] == "CONTENT_DOES_NOT_MATCH_THE_DATA"],
          "a legitimately TITLED report matches its data - the re-render "
          "dropped the title, so the finding fired on the presence of a "
          "heading rather than on anything wrong")
    edited = made["content"].replace("|", "| ", 1)
    answer = VAL.validate({"content": edited, "sha": made["sha"],
                           "kind": "page", "columns": made["columns"],
                           "title": made["title"]}, rows)
    check([one for one in (answer.get("findings") or [])
           if one["finding"] == "CONTENT_DOES_NOT_MATCH_THE_DATA"],
          "  and a report edited AFTER rendering is still caught")

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-RPT-VAL-004.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 3, "the contract declares 3 failures")
    for failure in named:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    for finding in ("SHA_DOES_NOT_DESCRIBE_THE_CONTENT",
                    "CONTENT_DOES_NOT_MATCH_THE_DATA", "COUNT_DOES_NOT_MATCH",
                    "GAPS_NOT_DECLARED", "A_FIGURE_WITH_NO_SOURCE"):
        check(finding in logic, "the code names finding %s" % finding)

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    an edit is caught, and 47 is not 1478")
    return 0


if __name__ == "__main__":
    sys.exit(main())
