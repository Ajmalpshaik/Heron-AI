# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
A decision another one supersedes says so, where a citation of it lands.

    python tools/check-supersessions.py

Exits 0 when every supersession is written at both ends, 1 on a finding, and 2
when it could not look: no docs/DECISIONS.md, or one holding no decision. A 2
is never a pass. tools/check-docs.py runs it as its section 13, so it runs
wherever that gate does - on a laptop before a push, and in CI.

WHY THIS EXISTS (D-144)
-----------------------
DECISIONS.md is append-only: a decision that is reversed keeps its words, and
a NEW decision says what it supersedes. The reader that arrangement protects
is the one following a citation - and every `[D-NN](DECISIONS.md#d-nn-...)` in
the repository lands on the OLD decision's heading in the index, not on the
new one. So the old entry has to say it was superseded, or the reader takes a
rule that no longer holds for one that does.

On 2026-10-19 four supersessions said so at both ends and six did not: D-24,
D-37, D-99 twice, D-101 and D-105 were each superseded, in full or in part, by
an accepted decision that said so in its own header, and nothing under their
own headings said anything. A reader sent to D-105 read "the chat only" with
no word that D-108 had put a Companion page beside it.

It was found by reading another project's documentation audit (docs/33 s5.1,
third pass), which fails a superseded architecture record that names no
successor. That project writes the supersession at the OLD end. Heron's log
writes it at the NEW end, so the check here starts from there - and runs both
ways.

WHAT IT ASKS, BOTH WAYS
-----------------------
  FORWARD. A decision in force whose header carries a `**Supersedes...**`
  field linking [D-X] - "Supersedes:", "Supersedes in part:", "Supersedes one
  sentence of:" and the rest - owes D-X's entry in docs/DECISIONS.md a
  `**Superseded...**` field linking it back, "Superseded by:" or "Superseded in
  part by:", on a line under D-X's heading. The new decision's header is read
  from its full record, docs/decisions/D-NN.md, AND from its index entry,
  because the index copy of an older entry is sometimes cut short.

  BACK. Every `**Superseded...**` field in an index entry links only decisions
  that exist and that say, in a `**Supersedes...**` field of their own, that
  they supersede it. A note naming the wrong successor sends the reader to the
  wrong place, which is worse than no note.

IN FORCE means a Status that does not start with "Proposed" or "Superseded".
A proposed decision has changed nothing yet - the owner may send it back - so
the old entry owes nothing until it is accepted, and the check asks for the
note on the day it becomes true. A decision that has itself been superseded no
longer changes anything: D-104 superseded one sentence of D-01, and D-105
restored it the same day. Each one skipped is printed with its reason, so a
pending note is seen rather than lost.

WHAT IT CANNOT SEE
------------------
It reads links, not sentences. A `**Supersedes...**` field that links a
decision only to name something beside it - D-74's "the MECHANISM of D-73's
neighbour" - reads as superseding it; D-74 is proposed, so it is printed as
pending and owes nothing today. A supersession written only in a decision's
prose, with no field, is invisible here. And the banner some full records
carry above their Status line, `> **SUPERSEDED BY ...**`, is a courtesy this
does not ask for: the index entry is where a citation lands, and its "Full
record" link sits directly under the note.
"""

import argparse
import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

HEADING = re.compile(u"^## (D-[0-9]+) +\u2014 +(.+?)\\s*$")
# A field opens a line with ** or follows a middle dot and a space on one.
FIELD_BREAK = re.compile(u"\\s\u00b7\\s(?=\\*\\*)")
LINKED = re.compile(r"\[D-([0-9]+)\]\(")


def number(decision_id):
    """D-07 -> 7: the log writes D-01 and D-126, and a link may write either."""
    return int(decision_id.split("-", 1)[1])


def anchor(heading_text):
    """The GitHub anchor for a heading, derived the way check-docs.py does:
    each space one hyphen, runs NOT collapsed, so ' - ' leaves two."""
    slug = re.sub(r"[^\w\s-]", "", heading_text.lower())
    return slug.strip().replace(" ", "-")


def header_of(lines):
    """A decision's header: the lines before its first heading."""
    out = []
    for line in lines:
        if line.startswith("#"):
            break
        out.append(line)
    return out


def fields(lines):
    """The bold fields in a header, each as its text after the opening **.

    A line that opens with neither ** nor a quote, a heading or a table row
    continues the field above it, as a wrapped line does; a blank line ends it.
    """
    logical = []
    for line in lines:
        if line.startswith("**"):
            logical.append(line)
        elif (line.strip() and logical and logical[-1] is not None
              and not line.startswith((">", "#", "|"))):
            logical[-1] += " " + line.strip()
        else:
            logical.append(None)
    found = []
    for line in logical:
        if line is None:
            continue
        for part in FIELD_BREAK.split(line):
            if part.startswith("**"):
                found.append(part[2:])
    return found


def linked(field, own):
    """The decisions a field links, by number, never the decision's own."""
    return set(int(n) for n in LINKED.findall(field)) - {own}


def _say(out, text):
    try:
        out.write(text)
    except UnicodeEncodeError:
        out.write(text.encode("ascii", "backslashreplace").decode("ascii"))


def read_log(root):
    """{number: (id, title, index header lines)} from docs/DECISIONS.md, or
    None when the file cannot be read."""
    try:
        text = io.open(os.path.join(root, "docs", "DECISIONS.md"),
                       encoding="utf-8").read()
    except (IOError, OSError):
        return None
    entries, current = {}, None
    for line in text.splitlines():
        m = HEADING.match(line)
        if m:
            current = (m.group(1), m.group(2), [])
            entries[number(m.group(1))] = current
        elif line.startswith("#"):
            current = None
        elif current is not None:
            current[2].append(line)
    for n, (did, title, lines) in list(entries.items()):
        entries[n] = (did, title, header_of(lines))
    return entries


def record_header(root, decision_id):
    """The header of docs/decisions/D-NN.md, after its title - [] when the
    decision has no file of its own yet."""
    try:
        text = io.open(os.path.join(root, "docs", "decisions",
                                    decision_id + ".md"),
                       encoding="utf-8").read()
    except (IOError, OSError):
        return []
    return header_of(text.splitlines()[1:])


def run(root=ROOT, out=None):
    """Check the log at root. Returns the exit code; writes the report."""
    out = out or sys.stdout
    entries = read_log(root)
    if entries is None:
        _say(out, "check-supersessions: could not run - docs/DECISIONS.md "
                  "could not be read\n")
        return 2
    if not entries:
        _say(out, "check-supersessions: could not run - docs/DECISIONS.md "
                  "holds no '## D-NN' heading, so the log has moved under "
                  "this tool\n")
        return 2

    declares, notes, status = {}, {}, {}
    for n, (did, _title, index_header) in entries.items():
        index_fields = fields(index_header)
        every = fields(record_header(root, did)) + index_fields
        declares[n] = set()
        for field in every:
            if field.startswith("Supersedes"):
                declares[n] |= linked(field, n)
            elif field.startswith("Status:") and n not in status:
                status[n] = field.split("**", 1)[-1].strip()
        notes[n] = set()
        for field in index_fields:
            if field.startswith("Superseded"):
                notes[n] |= linked(field, n)

    def name(n):
        return entries[n][0] if n in entries else "D-%02d" % n

    def link(n):
        did, title = entries[n][0], entries[n][1]
        return "[%s](#%s)" % (did, anchor("%s \u2014 %s" % (did, title)))

    findings, pending, declared = [], [], 0
    for y in sorted(entries):
        for x in sorted(declares[y]):
            declared += 1
            said = status.get(y, "")
            if said.startswith("Proposed"):
                pending.append("  pending  %s supersedes %s - %s is proposed, "
                               "so %s owes its note once %s is accepted\n"
                               % (name(y), name(x), name(y), name(x), name(y)))
            elif said.startswith("Superseded"):
                pending.append("  skipped  %s supersedes %s - %s has itself "
                               "been superseded, so %s owes it no note\n"
                               % (name(y), name(x), name(y), name(x)))
            elif x not in entries:
                findings.append("  MISSING  %s supersedes %s, which "
                                "docs/DECISIONS.md does not hold\n"
                                % (name(y), name(x)))
            elif y not in notes[x]:
                findings.append(
                    "  MISSING  %s supersedes %s, and %s's entry in "
                    "docs/DECISIONS.md does not say so - add a line under "
                    "its heading: **Superseded by:** or **Superseded in part "
                    "by:** %s, and what it changed\n"
                    % (name(y), name(x), name(x), link(y)))
    for x in sorted(entries):
        for y in sorted(notes[x]):
            if y not in entries:
                findings.append("  WRONG    %s's entry says %s superseded it, "
                                "and docs/DECISIONS.md holds no %s\n"
                                % (name(x), name(y), name(y)))
            elif x not in declares[y]:
                findings.append("  WRONG    %s's entry says %s superseded it, "
                                "and %s's own Supersedes field does not name "
                                "%s\n" % (name(x), name(y), name(y), name(x)))

    _say(out, "check-supersessions: %d decision(s) read, %d supersession(s) "
              "declared, %d by a decision not in force\n"
              % (len(entries), declared, len(pending)))
    for line in pending + findings:
        _say(out, line)
    if findings:
        _say(out, "FAILED  %d finding(s) - a citation of the old decision "
                  "lands on an entry that does not say it was superseded, or "
                  "says so wrongly\n" % len(findings))
        return 1
    _say(out, "PASS  every supersession a decision in force declares is "
              "written under the old decision's heading too, and every such "
              "note names a decision that says so\n")
    return 0


def main(argv=None):
    parser = argparse.ArgumentParser(
        description="Fail when a decision in force supersedes one whose entry "
                    "in docs/DECISIONS.md does not say so, or when an entry "
                    "names a successor that does not.")
    parser.parse_args(argv)
    return run()


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
