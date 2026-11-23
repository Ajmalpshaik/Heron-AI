# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
tools/open-issue.py - a register finding becomes a GitHub issue, and the
issue's number is written back where the finding is written (D-187).

    python tests/test_open_issue.py

Every register here is a throwaway written by this suite, and gh is a stand-in
that records what it was asked, so nothing here reads the real registers,
needs the network or opens an issue.

WHAT THIS PROVES
  1. A DRY RUN SENDS AND WRITES NOTHING: with no --create, gh is never run
     and every register file keeps its bytes, and the body it prints has the
     parts the owner's issues have - what happened, what is expected, the
     kind line, the checkbox, a ready prompt and "Close this issue when".
  2. --create OPENS ONE ISSUE AND WRITES ITS NUMBER BACK: one POST to the
     REST route, with the kind's label; the row's state then begins MOVED TO
     ISSUE [#N](url) <date>, keeps its old state after "Was:", and still
     splits into three cells - read by open-defects.py's own CELL and MOVED,
     so the register's reader agrees it is moved.
  3. --into ADDS TO AN OPEN ISSUE: the new checkbox goes after the last one
     and its sub-lines; a closed issue, a pull request, or an issue that
     already names the id is refused and nothing is written.
  4. --record WRITES BACK AND SENDS NOTHING.
  5. NOTHING PRIVATE IS SENT: a credential shape, an e-mail address or a
     user's own folder anywhere in the issue refuses the run before gh is
     asked anything; `C:\\Users\\<you>` is not a person's folder.
  6. NO GITHUB IS NOT RUN, NEVER A PASS: no gh, or gh refusing, exits 3 and
     leaves the register as it was.
  7. AN ISSUE ALREADY NAMING THE ID IS NOT OPENED TWICE.
  8. THE OTHER REGISTERS, IN THEIR OWN FORMS: a NEEDS-CHECKING group gets
     its "Tracked as" line under its heading and lists only unstruck checks;
     one check gets a PLAIN line, so its bold id is still written once; a
     question gets its line under its heading - a question marked with its
     priority colour too; an answered question, a
     tracked group, a moved row and a FIXED row are refused.
  9. LINE ENDINGS ARE KEPT: a CRLF file is written back CRLF.

WHAT IT DOES NOT PROVE
  That GitHub answers as the stand-in does - the REST shapes are GitHub's
  documented ones, read on 2026-11-20 from issues the owner already has.
"""

from __future__ import print_function

import importlib.util
import io
import json
import os
import re
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FAILURES = []


def _load(name, filename):
    spec = importlib.util.spec_from_file_location(name, os.path.join(ROOT, "tools", filename))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


TOOL = _load("open_issue", "open-issue.py")
DEFECTS = _load("open_defects", "open-defects.py")
REPO = "owner/repo"
DATE = "2026-11-20"


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


HEADER = "| # | Defect | State |\n|---|---|---|\n"
ROWS = {
    "row-5b-9001.md":
        "# Fragment issues - row 5b-9001\n\n> One row.\n\n" + HEADER +
        "| **9001** | **`SOME_CARD` SAYS DONE WHEN REVIT REFUSED THE CHANGE.** Seen 2026-11-20 on Revit "
        "2024. **Owed:** say refused, and name what Revit said. | **OPEN 2026-11-20** - recorded, not "
        "fixed. |\n",
    "row-5b-9002.md":
        "# Fragment issues - row 5b-9002\n\n> One row.\n\n" + HEADER +
        "| **9002** | **ALREADY MOVED.** | **MOVED TO ISSUE [#77](https://github.com/owner/repo/issues/77) "
        "2026-11-16** - still open. Was: OPEN |\n",
    "row-5b-9003.md":
        "# Fragment issues - row 5b-9003\n\n> One row.\n\n" + HEADER +
        "| **9003** | **A ROW WITH A PERSON IN IT.** Owed: mail ajmal@example.com about it. | **OPEN** |\n",
    "row-5b-9004.md":
        "# Fragment issues - row 5b-9004\n\n> One row.\n\n" + HEADER +
        "| **9004** | **A ROW WITH A FOLDER.** Owed: read the log in C:\\Users\\ajmal\\AppData\\Local. "
        "| **OPEN** |\n",
    "row-5b-9005.md":
        "# Fragment issues - row 5b-9005\n\n> One row.\n\n" + HEADER +
        "| **9005** | **A ROW WITH THE PLACEHOLDER.** Owed: read the log in C:\\Users\\<you>\\AppData\\Local. "
        "| **OPEN** |\n",
    "row-5b-9006.md":
        "# Fragment issues - row 5b-9006\n\n> One row.\n\n" + HEADER +
        "| **9006** | **A ROW WITH A TOKEN, ghp_" + "a" * 36 + ".** | **OPEN** |\n",
    "section-5b-rows-001-025.md":
        "# Section 5b rows 1 to 25\n\n" + HEADER +
        "| **7** | **SEVEN IS OPEN.** Owed: look again. | OPEN - needs a sitting |\n"
        "| **8** | **EIGHT IS FIXED.** | **FIXED 2026-11-01** - done |\n",
    "section-5-rows-001-025.md":
        "# Section 5 rows 1 to 25\n\n" + HEADER +
        "| 3 | **THREE IS OPEN.** | OPEN |\n",
}

GROUP_ZQ = (
    "# Needs checking - Group ZQ\n\n> One group.\n\n"
    "## Group ZQ - a test group (2026-11-20)\n\n"
    "**Why:** a suite needs one.\n\n"
    "| Check | What | Pass |\n|---|---|---|\n"
    "| **ZQ1** | *Positive.* one wall | `done` 1 |\n"
    "| ~~**ZQ2**~~ | *Negative.* no wall | `done` 0 - held 2026-11-20 |\n"
    "| **ZQ3** | *Negative.* a door | refused |\n"
)
GROUP_ZR = (
    "# Needs checking - Group ZR\n\n> One group.\n\n"
    "## Group ZR - a tracked group (2026-11-16)\n\n"
    "**Tracked as GitHub issue [#500](https://github.com/owner/repo/issues/500) since 2026-11-16** - "
    "the checks are there.\n\n"
    "| **ZR1** | *Positive.* | ok |\n"
)
QUESTIONS = (
    "# Tier 1\n\n"
    "### Q-700 \u2014 Should the tool ask first? *(new, 2026-11-20)*\n\n"
    "The question, in full.\n\n"
    "### \u2705 Q-701 \u2014 Already answered *(answered)*\n\n"
    "**Answer:** yes.\n\n"
    "### Q-702 \u2014 Answered but no tick\n\n"
    "**Answer:** no.\n\n"
    # THE REGISTER MARKS AN OPEN QUESTION BY ITS PRIORITY COLOUR (OPEN-QUESTIONS.md:
    # red, orange, yellow, blue), and an answered one may keep its colour - its
    # Answer paragraph is what marks it answered. Until 2026-11-21 the tool read
    # only a bare id or a tick, so it found no colour-marked question at all.
    "### \U0001F535 Q-703 \u2014 Can wait, and carries its colour *(new, 2026-11-21)*\n\n"
    "The question.\n\n"
    "### \U0001F7E1 Q-704 \u2014 Answered, and kept its colour\n\n"
    "**Answer:** yes.\n"
)


def scratch():
    root = tempfile.mkdtemp(prefix="heron-open-issue-")
    fi = os.path.join(root, "docs", "fragment-issues")
    nc = os.path.join(root, "docs", "needs-checking")
    oq = os.path.join(root, "docs", "open-questions")
    for folder in (fi, nc, oq):
        os.makedirs(folder)
    for name, text in ROWS.items():
        with io.open(os.path.join(fi, name), "w", encoding="utf-8", newline="") as out:
            out.write(text)
    for name, text in (("group-zq.md", GROUP_ZQ), ("group-zr.md", GROUP_ZR)):
        with io.open(os.path.join(nc, name), "w", encoding="utf-8", newline="") as out:
            out.write(text)
    with io.open(os.path.join(oq, "tier-1.md"), "w", encoding="utf-8", newline="") as out:
        out.write(QUESTIONS)
    return root


def snapshot(root):
    found = {}
    for folder, _dirs, files in os.walk(os.path.join(root, "docs")):
        for name in files:
            path = os.path.join(folder, name)
            with open(path, "rb") as handle:
                found[os.path.relpath(path, root)] = handle.read()
    return found


def text(root, rel):
    with io.open(os.path.join(root, *rel.split("/")), encoding="utf-8", newline="") as handle:
        return handle.read()


class FakeGh(object):
    """A stand-in gh: answers by route, records every call and payload."""

    def __init__(self, issue=None, search=None, made=1234, fail=None, missing=False):
        self.calls = []
        self.payloads = []
        self.issue = issue
        self.search = search or []
        self.made = made
        self.fail = fail
        self.missing = missing

    def __call__(self, args, root):
        if self.missing:
            raise OSError("No such file or directory: 'gh'")
        payload = None
        if "--input" in args:
            with io.open(args[args.index("--input") + 1], encoding="utf-8") as handle:
                payload = json.load(handle)
        self.calls.append(list(args))
        self.payloads.append(payload)
        if self.fail:
            return 1, "", self.fail
        if "search/issues" in args:
            return 0, json.dumps({"items": self.search}), ""
        if "POST" in args:
            return 0, json.dumps({"number": self.made,
                                  "html_url": "https://github.com/%s/issues/%d" % (REPO, self.made)}), ""
        if "PATCH" in args:
            return 0, json.dumps({"number": self.issue["number"]}), ""
        return 0, json.dumps(self.issue), ""


def run(root, argv, gh=None):
    said = []
    gh = gh or FakeGh()
    code = TOOL.main(argv + ["--repo", REPO, "--date", DATE], root=root, gh=gh, out=said.append)
    return code, "".join(said), gh


def state_of(root, rel, number):
    for line in text(root, rel).split("\n"):
        if re.match(r"^\|\s*\*{0,2}%d\*{0,2}\s*\|" % number, line):
            cells = DEFECTS.CELL.split(line.rstrip("\r"))
            return cells, re.sub(r"[`*]", "", cells[-2]).strip()
    return None, None


def main():
    root = scratch()
    try:
        print("\n1. A dry run sends and writes nothing")
        before = snapshot(root)
        code, said, gh = run(root, ["5b-9001"])
        check(code == 0, "exit 0 (%d)" % code)
        check(gh.calls == [], "gh was never run")
        check(snapshot(root) == before, "every register file keeps its bytes")
        for part in ("**What happened:**", "**What is expected:**", "**Kind: code fix**",
                     "- [ ] **5b-9001**", "### Ready prompt", "### Close this issue when",
                     "Label: bug", "Title: Fix: "):
            check(part in said, "the body has %r" % part)
        check("blob/main/docs/fragment-issues/row-5b-9001.md" in said,
              "the checkbox links the row's own file")
        check("say refused, and name what Revit said" in said,
              "what is expected is the row's Owed part")
        check("`SOME_CARD` says done when Revit refused the change" in said,
              "a capitals lead reads in sentence case, its code and Revit kept")

        print("\n2. --create opens one issue and writes its number back")
        code, said, gh = run(root, ["5b-9001", "--create", "--kind", "revit-run",
                                    "--title", "the card says done when Revit refused"])
        check(code == 0, "exit 0 (%d)%s" % (code, "" if code == 0 else " - " + said[-300:]))
        posts = [i for i, c in enumerate(gh.calls) if "POST" in c]
        check(len(posts) == 1, "one POST")
        if posts:
            call, payload = gh.calls[posts[0]], gh.payloads[posts[0]]
            check(call[:2] == ["api", "repos/%s/issues" % REPO], "to the REST route, not GraphQL")
            check(payload["labels"] == ["needs-revit-run"], "with the kind's label")
            check(payload["title"].startswith("Revit run: the card says done when Revit refused")
                  and payload["title"].endswith("- row 5b-9001"), "the title: kind, words, row")
            check("REVIT RUN issue" in payload["body"], "the prompt names the part to follow")
        cells, said_state = state_of(root, "docs/fragment-issues/row-5b-9001.md", 9001)
        check(cells is not None and len(cells) == 5, "the row still splits into three cells")
        check(bool(said_state) and DEFECTS.MOVED.match(said_state) is not None,
              "open-defects.py's MOVED reads it as moved")
        check(cells is not None and cells[3].startswith(
            " **MOVED TO ISSUE [#1234](https://github.com/owner/repo/issues/1234) 2026-11-20**"),
              "its state begins MOVED TO ISSUE [#1234](url) 2026-11-20")
        check(cells is not None and "Was: **OPEN 2026-11-20** - recorded, not fixed." in cells[3],
              "its old state is kept after Was:")
        code, said, gh = run(root, ["5b-9001", "--create"])
        check(code == 1 and "already tracked" in said and gh.calls == [],
              "run again, it is refused before gh is asked")

        print("\n3. --into adds to an open issue")
        body = ("Intro.\n\n### Rows - tick each one when it is done\n\n"
                "- [x] **5b-1** - one ([row](u))\n- [ ] **5b-2** - two ([row](u))\n"
                "  - Owed: more\n\n### Ready prompt\n\n```text\np\n```\n")
        issue = {"number": 1078, "state": "open", "body": body}
        before = snapshot(root)
        code, said, gh = run(root, ["5b-7", "--into", "1078"], FakeGh(issue=issue))
        check(code == 0 and gh.calls == [] and snapshot(root) == before,
              "without --create it prints the line and sends nothing")
        code, said, gh = run(root, ["5b-7", "--into", "1078", "--create"], FakeGh(issue=issue))
        check(code == 0, "exit 0 (%d)%s" % (code, "" if code == 0 else " - " + said[-300:]))
        patch = [p for c, p in zip(gh.calls, gh.payloads) if "PATCH" in c]
        check(len(patch) == 1, "one PATCH of the issue's body")
        if patch:
            lines = patch[0]["body"].split("\n")
            at = [i for i, l in enumerate(lines) if l.startswith("- [ ] **5b-7**")]
            check(len(at) == 1 and lines[at[0] - 1] == "  - Owed: more",
                  "the new box goes after the last one and its sub-line")
            check("section-5b-rows-001-025.md?plain=1#L5" in patch[0]["body"],
                  "a row in a rows file is linked to its line")
        _cells, said_state = state_of(root, "docs/fragment-issues/section-5b-rows-001-025.md", 7)
        check(bool(said_state) and re.match(r"MOVED TO ISSUE \[#1078\]\([^)]+/1078\) 2026-11-20 ", said_state),
              "the rows file's row names #1078")
        _cells, eight = state_of(root, "docs/fragment-issues/section-5b-rows-001-025.md", 8)
        check(eight.startswith("FIXED"), "the row beside it is untouched")
        for state, what in (("closed", "a closed issue"),):
            before = snapshot(root)
            code, said, gh = run(root, ["row 3", "--into", "9", "--create"],
                                 FakeGh(issue={"number": 9, "state": state, "body": body}))
            check(code == 1 and snapshot(root) == before and not [c for c in gh.calls if "PATCH" in c],
                  "%s is refused and nothing is written" % what)
        code, said, gh = run(root, ["row 3", "--into", "9", "--create"],
                             FakeGh(issue={"number": 9, "state": "open", "body": "",
                                           "pull_request": {"url": "x"}}))
        check(code == 1 and "pull request" in said, "a pull request is refused")
        code, said, gh = run(root, ["5-3", "--into", "9", "--create"],
                             FakeGh(issue={"number": 9, "state": "open", "body": "- [ ] **row 3** - x"}))
        check(code == 1 and "already names" in said, "an issue already naming the row is refused")

        print("\n4. --record writes back and sends nothing")
        code, said, gh = run(root, ["5-3", "--record", "4321"])
        check(code == 0 and gh.calls == [], "exit 0, gh never run")
        _cells, three = state_of(root, "docs/fragment-issues/section-5-rows-001-025.md", 3)
        check(bool(three) and re.match(r"MOVED TO ISSUE \[#4321\]\([^)]+/4321\) 2026-11-20 ", three),
              "section 5's row 3 names #4321")

        print("\n5. Nothing private is sent")
        for ident, what in (("5b-9003", "an e-mail address"), ("5b-9004", "a Windows user's own folder"),
                            ("5b-9006", "github token")):
            before = snapshot(root)
            code, said, gh = run(root, [ident, "--create"])
            check(code == 1 and what in said and gh.calls == [] and snapshot(root) == before,
                  "%s in the row: refused before gh is asked" % what)
        code, said, gh = run(root, ["5b-9005", "--happened", "write to ajmal@example.com"])
        check(code == 1 and "e-mail" in said, "the words given are checked too, even on a dry run")
        code, said, gh = run(root, ["5b-9005"])
        check(code == 0, "C:\\Users\\<you> is a placeholder, not a person's folder")

        print("\n6. No GitHub is NOT RUN, never a pass")
        before = snapshot(root)
        code, said, gh = run(root, ["5b-9005", "--create"], FakeGh(missing=True))
        check(code == 3 and "NOT RUN" in said and "--record N" in said, "no gh: exit 3, says --record")
        check(snapshot(root) == before, "and the register is as it was")
        code, said, gh = run(root, ["5b-9005", "--create"], FakeGh(fail="HTTP 403: Forbidden"))
        check(code == 3 and snapshot(root) == before, "gh refusing: exit 3, nothing written")
        check("Title: " in said, "the body is printed first, to list under Issues to open")

        print("\n7. An issue already naming the id is not opened twice")
        gh = FakeGh(search=[{"number": 88, "title": "Fix: x - row 5b-9005", "body": ""},
                            {"number": 89, "title": "Fix: 5b-90050 is another", "body": ""}])
        code, said, gh = run(root, ["5b-9005", "--create"], gh)
        check(code == 1 and "#88" in said and "#89" not in said
              and not [c for c in gh.calls if "POST" in c], "refused, naming #88 only, no POST")

        print("\n7b. A passing mention is not tracking (rows 5b-1962, 5b-1965)")
        prose = "Check PK4 - after Group PG, and see row 5b-9005 and row 5b-9005's note"
        gh = FakeGh(search=[{"number": 90, "title": "Fix: PK", "body": prose},
                            {"number": 91, "title": "Fix: y", "body": "- [ ] **5b-9005** - y"}])
        code, said, gh = run(root, ["5b-9005", "--create"], gh)
        check(code == 1 and "#91" in said and "#90" not in said,
              "a row named only in prose is not tracked; its checkbox line is")
        gh = FakeGh(search=[{"number": 90, "title": "Fix: PK", "body": prose}])
        code, said, gh = run(root, ["5b-9005", "--create"], gh)
        check(code == 0 and [c for c in gh.calls if "POST" in c], "a passing mention does not block a new issue")
        with io.open(os.path.join(root, "docs", "fragment-issues", "row-5b-9005.md"), "w",
                     encoding="utf-8", newline="") as out:
            out.write(ROWS["row-5b-9005.md"])      # the create above wrote its number back
        code, said, gh = run(root, ["5b-9005", "--into", "9", "--create"],
                             FakeGh(issue={"number": 9, "state": "open", "body": prose}))
        check(code == 0 and [c for c in gh.calls if "PATCH" in c], "--into works when the issue only mentions it")
        grp = TOOL.Finding("group", "Group PG", "docs/needs-checking/group-pg.md", 1, "t", "h", "e")
        grp.items = [("PG1", "first"), ("PG2", "second")]
        check(not TOOL.tracks(prose, grp), "a group named in prose is not tracked")
        check(TOOL.tracks("- [ ] **PG2** - second", grp), "a group's own check line tracks it")
        check(TOOL.tracks("## Group PG\n", grp), "a group's own heading tracks it")

        print("\n8. The other registers, in their own forms")
        code, said, gh = run(root, ["ZQ"])
        check(code == 0 and "**ZQ1**" in said and "**ZQ3**" in said and "**ZQ2**" not in said,
              "a group lists its unstruck checks only")
        check("Label: needs-revit-run" in said and "NEEDS-CHECKING issue" in said,
              "a group is a Revit run, worked as a NEEDS-CHECKING issue")
        code, said, gh = run(root, ["group zq", "--create"])
        check(code == 0, "a group opens")
        lines = text(root, "docs/needs-checking/group-zq.md").split("\n")
        at = [i for i, l in enumerate(lines) if l.startswith("## Group ZQ")][0]
        check(lines[at + 1] == "" and lines[at + 2].startswith(
            "**Tracked as GitHub issue [#1234](https://github.com/owner/repo/issues/1234) since "
            "2026-11-20**") and lines[at + 3] == "", "its line is under its heading, blank around it")
        code, said, gh = run(root, ["ZQ", "--create"])
        check(code == 1 and "already tracked" in said, "a tracked group is refused")
        code, said, gh = run(root, ["ZR1", "--record", "500"])
        check(code == 0 and "already names" in said, "a check its group's issue already holds: nothing to write")
        code, said, gh = run(root, ["ZR1", "--record", "600"])
        zr = text(root, "docs/needs-checking/group-zr.md")
        check(code == 0 and "issues/600) since 2026-11-20: check ZR1" in zr,
              "a check in another issue gets a line of its own")
        check(zr.count("**ZR1**") == 1, "its bold id is still written once")
        code, said, gh = run(root, ["ZQ2"])
        check(code == 1, "a struck check is refused")
        code, said, gh = run(root, ["Q-700", "--record", "701"])
        q = text(root, "docs/open-questions/tier-1.md").split("\n")
        check(code == 0 and q[3] == "" and q[4].startswith("**Tracked as GitHub issue [#701]") and q[5] == "",
              "a question gets its line under its heading")
        code, said, gh = run(root, ["Q-703", "--record", "703"])
        q = text(root, "docs/open-questions/tier-1.md").split("\n")
        at = [i for i, l in enumerate(q) if "Q-703" in l and l.startswith("###")]
        check(code == 0 and len(at) == 1 and q[at[0] + 1] == ""
              and q[at[0] + 2].startswith("**Tracked as GitHub issue [#703]"),
              "a question marked with its priority colour is found, and gets its line under its heading")
        code, said, gh = run(root, ["Q-704", "--record", "704"])
        check(code == 1 and "answered" in said, "a coloured question with an Answer is refused as answered")
        for ident, what in (("Q-701", "a ticked question"), ("Q-702", "a question with an Answer"),
                            ("Q-700", "a question already tracked"), ("5b-9002", "a moved row"),
                            ("5b-8", "a FIXED row"), ("5b-424242", "a row that is not there"),
                            ("not an id", "something that is no id")):
            code, said, gh = run(root, [ident, "--record", "9"])
            check(code == 1, "%s is refused" % what)

        print("\n9. Line endings are kept")
        crlf = os.path.join(root, "docs", "fragment-issues", "row-5b-9100.md")
        with io.open(crlf, "w", encoding="utf-8", newline="") as out:
            out.write("# Fragment issues - row 5b-9100\r\n\r\n" + HEADER.replace("\n", "\r\n") +
                      "| **9100** | **CRLF.** | OPEN |\r\n")
        code, said, gh = run(root, ["5b-9100", "--record", "5"])
        with open(crlf, "rb") as handle:
            data = handle.read()
        check(code == 0 and b"MOVED TO ISSUE" in data and b"\n" not in data.replace(b"\r\n", b""),
              "a CRLF file is written back CRLF")
    finally:
        shutil.rmtree(root, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED")
        for one in FAILURES:
            print("  - %s" % one)
        return 1
    print("PASSED - a finding becomes one issue, its number is written back in the register's own "
          "form, and nothing private or unsent is claimed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
