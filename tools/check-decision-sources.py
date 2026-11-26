# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
An accepted decision links where the owner's yes can be read (D-206).

    python tools/check-decision-sources.py          # D-206 and later: the gate
    python tools/check-decision-sources.py --all    # every decision; the older ones only reported

Exits 0 when every Accepted decision from D-206 on has an `**Accepted:**` line
that links where its acceptance can be read, 1 on a finding, and 2 when it
could not look: no decision to read, or the header reader it shares with
tools/check-supersessions.py would not load. A 2 is never a pass. CI's job
"The gates that must pass" runs it.

WHY THIS EXISTS (row 5b-1986, issue #1262)
------------------------------------------
On 2026-11-21 a cloud session wrote D-196 Accepted, quoting the owner: "5b-1903:
yes, also check handed elements". On 2026-11-22 he said he never said it. The
only source the Accepted line gave was that session's own conversation, which
nobody else can open, and two earlier decisions, D-191 and D-192, had been
written the same way - both later confirmed, but only by asking him. Nothing
checked where a quoted yes was given: check-decision-titles reads titles and
Numbering lines, check-supersessions reads Supersedes fields.

Asked on 2026-11-26 "When a session writes 'the owner said yes', must it link
to where you said it?", the owner chose "Yes, and a checker blocks it". D-206
is that rule; this is the checker.

WHAT COUNTS AS A LINK - D-206 says it, and LINKS below is the same list
-----------------------------------------------------------------------
  * a pull request or an issue of this repository, by its full URL:
    https://github.com/Ajmalpshaik/Heron-AI/pull/N or .../issues/N, with a
    comment's #issuecomment- anchor where there is one;
  * a Claude Code session the owner can open: https://claude.ai/code/session_...

A bare "#1234" does not count, and neither does a commit's sha: a commit is
what a session wrote, not where the owner said anything, and a bare number is
a link only on GitHub's own pages. A link in another field of the header - an
**Answers:** line, an **Affects:** line - does not count either: the
**Accepted:** line is the one that says where the yes was given.

WHICH DECISIONS IT ASKS
-----------------------
Every decision numbered D-206 or later whose Status starts with "Accepted".
Proposed, Not accepted, Rejected and Superseded decisions owe nothing - D-206
says a session that heard a yes only in its own chat writes the decision
Proposed, and that is a pass here. A decision Accepted with no **Accepted:**
line at all fails too: it says nobody accepted it, anywhere.

Decisions D-00 to D-205 are GRANDFATHERED BY NUMBER (FIRST below). They were
written before the rule, many "in conversation", and the log is append-only:
they are not rewritten to pass. `--all` reads them anyway and prints how each
would fare, as a report - their findings never change the exit code. D-205 is
inside that number because a sibling session was writing it the same day as
D-206.

It reads a decision's full record, docs/decisions/D-NN.md, and its entry in
docs/DECISIONS.md, field by field, with check-supersessions.py's own reader - one
reading of a decision's header, not two.

WHAT IT CANNOT SEE
------------------
It sees that a link is there and has the right shape. It does not open it, so
it cannot tell whether the words are at the other end, whether the session or
the comment is the one the owner spoke in, or whether a comment posted under
his GitHub login was typed by him or by a session - D-206 says a reviewer
still opens the link. A placeholder that has the shape of a session id passes.
"""

import argparse
import importlib.util
import io
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)

# The first decision the rule binds: D-206 itself. Everything below it was
# written before the rule existed (see the docstring).
FIRST = 206

# What counts as a place the owner's yes can be read - D-206, item 1.
LINKS = (
    ("a pull request or issue of this repository",
     re.compile(r"https://github\.com/ajmalpshaik/heron-ai/(?:pull|issues)/[0-9]+",
                re.IGNORECASE)),
    ("a Claude Code session",
     re.compile(r"https://claude\.ai/code/session_[A-Za-z0-9]{8,}")),
)

RECORD = re.compile(r"^D-([0-9]+)\.md$")
# "Accepted", "Accepted, nothing built", "✅ Accepted · built" - and not "Not accepted".
ACCEPTED = re.compile(r"^[^A-Za-z]*Accepted\b")

OK, FOUND, COULD_NOT = 0, 1, 2


def _say(out, text):
    try:
        out.write(text)
    except UnicodeEncodeError:
        out.write(text.encode("ascii", "backslashreplace").decode("ascii"))


def _reader():
    """check-supersessions.py as a module - the one reader of a decision's
    header fields - or None when it will not load."""
    path = os.path.join(HERE, "check-supersessions.py")
    try:
        spec = importlib.util.spec_from_file_location("check_supersessions", path)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        return module
    except (IOError, OSError, SyntaxError, ImportError, AttributeError):
        return None


def links(field):
    """The checkable links in one field's text, in the order written: [(kind, url)]."""
    found = []
    for kind, pattern in LINKS:
        for m in pattern.finditer(field):
            found.append((m.start(), kind, m.group(0)))
    return [(kind, url) for _, kind, url in sorted(found)]


def decisions(root, reader):
    """{number: (id, [header fields])}: every decision in the record folder or
    the index, its full record's fields first and its index entry's after."""
    index = reader.read_log(root) or {}
    folder = os.path.join(root, "docs", "decisions")
    numbers = set(index)
    if os.path.isdir(folder):
        for name in os.listdir(folder):
            m = RECORD.match(name)
            if m:
                numbers.add(int(m.group(1)))
    out = {}
    for n in numbers:
        did = index[n][0] if n in index else "D-%02d" % n
        every = reader.fields(reader.record_header(root, did))
        if n in index:
            every += reader.fields(index[n][2])
        out[n] = (did, every)
    return out


def judge(did, every):
    """(verdict, words) for one decision: verdict is 'skip', 'ok' or 'fail'."""
    status = None
    accepted = []
    for field in every:
        if field.startswith("Status:") and status is None:
            status = field.split("**", 1)[-1].strip()
        elif field.startswith("Accepted:"):
            accepted.append(field.split("**", 1)[-1].strip())
    if status is None:
        return "skip", "%s states no Status" % did
    if not ACCEPTED.match(status):
        return "skip", "%s is %s" % (did, status.split(u" · ")[0][:60])
    if not accepted:
        return "fail", ("NO LINE  %s is Accepted and its header has no **Accepted:** line "
                        "saying who accepted it and where. Add one that links where the "
                        "words can be read, or write it Proposed (D-206)" % did)
    found = []
    for field in accepted:
        found += links(field)
    if not found:
        return "fail", ("NO LINK  %s is Accepted, and its **Accepted:** line links nowhere the "
                        "owner's words can be read. Link the pull-request or issue comment, or "
                        "the claude.ai session, where he gave them - or write the decision "
                        "Proposed for him or the coordinator to confirm (D-206)" % did)
    return "ok", "%s - %s: %s" % (did, found[0][0], found[0][1])


def run(root=ROOT, out=None, every=False, first=FIRST):
    """Check the decisions at root. Returns the exit code; writes the report."""
    out = out or sys.stdout
    reader = _reader()
    if reader is None:
        _say(out, "check-decision-sources: could not run - tools/check-supersessions.py, "
                  "whose header reader this uses, would not load\n")
        return COULD_NOT
    found = decisions(root, reader)
    if not found:
        _say(out, "check-decision-sources: could not run - no decision in docs/DECISIONS.md "
                  "or docs/decisions/, so the log has moved under this tool\n")
        return COULD_NOT

    passed, skipped, failed, earlier, earlier_fail = [], [], [], [], []
    for n in sorted(found):
        did, fields = found[n]
        verdict, words = judge(did, fields)
        if n < first:
            if every:
                if verdict == "fail":
                    earlier_fail.append(words)
                elif verdict == "ok":
                    earlier.append(words)
            continue
        {"ok": passed, "skip": skipped, "fail": failed}[verdict].append(words)

    before = sum(1 for n in found if n < first)
    _say(out, "check-decision-sources: %d decision(s) read; from D-%d on, %d accepted with a "
              "link, %d not accepted, %d failing; %d before D-%d, grandfathered (D-206)\n"
              % (len(found), first, len(passed), len(skipped), len(failed), before, first))
    for words in passed:
        _say(out, "  ok       %s\n" % words)
    for words in skipped:
        _say(out, "  skipped  %s - owes no link\n" % words)
    if every:
        _say(out, "\nBEFORE D-%d - a report; nothing here changes the exit code\n" % first)
        for words in earlier:
            _say(out, "  ok       %s\n" % words)
        for words in earlier_fail:
            _say(out, "  would fail  %s\n" % words.split("  ", 1)[-1].split(". ")[0])
        _say(out, "  %d of %d would pass, %d would fail\n"
                  % (len(earlier), before, len(earlier_fail)))
    for words in failed:
        _say(out, "  %s\n" % words)
    if failed:
        _say(out, "FAILED  %d finding(s) - an Accepted decision whose acceptance cannot be "
                  "read anywhere\n" % len(failed))
        return FOUND
    _say(out, "PASS  every Accepted decision from D-%d on links where its acceptance can "
              "be read\n" % first)
    return OK


def main(argv=None):
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except AttributeError:
            pass
    parser = argparse.ArgumentParser(
        description="Fail when an Accepted decision from D-206 on has no **Accepted:** line "
                    "linking where its acceptance can be read (D-206).")
    parser.add_argument("--all", action="store_true",
                        help="also report every decision before D-206 - never a failure")
    args = parser.parse_args(argv)
    return run(every=args.all)


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
