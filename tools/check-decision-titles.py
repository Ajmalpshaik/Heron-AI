# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
A decision keeps the title it was first written with.

    python tools/check-decision-titles.py
    python tools/check-decision-titles.py --published origin/main

Reads every decision heading in docs/DECISIONS.md - `## D-NN`, a dash, a title -
and the git history of that file, and fails when a number's heading no longer
carries the title that number was first written with. Exits 0 when every title
holds, 1 on a finding, and 2 when it could not look: no git, or a clone whose
history has been cut short.

WHY IT EXISTS
-------------
DECISIONS.md is append-only. A reversal gets a NEW entry, so a number means one
decision for ever, and every `[D-NN](...)` written anywhere relies on that.
tools/check-docs.py checks that no number is DEFINED twice at once. Nothing
checked that a number KEPT its decision, and two did not: D-45 from 2026-09-02
and D-46 from 2026-09-06 each named a different decision from the one first
written under it, the first ones had left the log, and nothing noticed for
three weeks. They are back as D-97 and D-98 (FRAGMENT-ISSUES row 5b-157).

WHY IT READS HISTORY, AND NOT A LIST OF TITLES KEPT IN THE TREE
---------------------------------------------------------------
Because of how those two were lost, which was not by an edit. One branch wrote
D-45 to D-49 on 2026-08-30 and 08-31; main wrote a different D-45 on 08-31
without having seen them; and the merge of 2026-09-02, 1f884ae, kept main's
DECISIONS.md whole and dropped the branch's five. A list of titles kept in the
tree goes through a merge like that exactly as the log does, and comes out
agreeing with it. Only the history still holds both sides.

That merge took three more than the two row 5b-157 restored: the first D-47,
D-48 and D-49 left the log in it too, and on 2026-09-06 their numbers were given
to new decisions. They are back as D-101 to D-103 (row 5b-171).

WHAT COUNTS AS AN EXCUSE, AND WHERE IT LIVES
--------------------------------------------
A changed title passes only when the decision says why, on a **Numbering:**
line under its heading that QUOTES the title the number was first written with.
A title that moved to another number passes only when that line NAMES the number
it was first written under. D-45 to D-49, D-97, D-98 and D-101 to D-103
carry exactly those lines.
The excuse lives in the decision, where a person following a citation reads it,
not in this file.

KNOWN is the other kind: titles that changed before this check existed, each
named with its commits rather than excused in bulk. An entry that stops being
needed fails the run, so the list can only get shorter.

--published REF
---------------
The history that counts as written: HEAD's by default. CI passes HEAD^1, the
branch a pull request merges into, so a title a branch changed before it was
ever merged is a draft and not a rewrite. That matters most in the case this
exists for. A branch that numbered its decision D-99, and finds main has since
given D-99 to another, does the right thing by renumbering its own - and against
its own history that reads as D-99 changing title. Against main's it does not.

WHAT IT CANNOT SEE
------------------
A shallow clone holds no history, and a gate that passed on the history it could
not see would be passing on nothing - so it exits 2 there, and says how to fetch
the rest. CI's checkout is shallow unless told otherwise; the gates job fetches
the whole history for this. "First" is by commit time, the only order two
branches share. And it reads headings, not the text under them: a decision
whose words were rewritten under the same heading is not something a title can
show.
"""

import argparse
import io
import os
import re
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
LOG = "docs/DECISIONS.md"
RECORDS = "docs/decisions"

NL = chr(10)
DASHES = chr(0x2014) + chr(0x2013) + "-"
FENCE = "`" * 3

# Any level: the log's first commit wrote its decisions as `###`, and a first
# title read at the wrong level is a first title missed.
HEADING = re.compile("^#{1,6} +(D-[0-9]+) +[" + DASHES + "]+ +(.+?) *$")
NUMBERING = "**Numbering:**"
OK, FOUND, COULD_NOT = 0, 1, 2

# Titles that changed before this check existed. Each is keyed by number and
# holds the title the number was FIRST written with, as the history shows it,
# and what happened. An entry is accepted only while the history still shows
# that first title and the heading still differs from it without a Numbering
# line saying so - anything else fails the run as stale.
KNOWN = {
    "D-00": ("Documentation-first, no implementation yet",
             "retitled the day it was written: 05a4b05 to 190b034, 2026-08-27"),
    "D-01": ("Execution host *(pending)*",
             "a pending placeholder, answered the day it was written: "
             "05a4b05 to 190b034, 2026-08-27"),
    "D-02": ("MCP " + chr(0x2194) + " add-in transport *(pending)*",
             "a pending placeholder, answered the day it was written: "
             "05a4b05 to f1cf4ec, 2026-08-27"),
    "D-03": ("MCP tool granularity *(pending)*",
             "a pending placeholder, answered the day it was written: "
             "05a4b05 to f1cf4ec, 2026-08-27"),
    "D-04": ("Generated code execution model *(pending)*",
             "a pending placeholder, answered the day it was written: "
             "05a4b05 to f1cf4ec, 2026-08-27"),
    "D-26": ("Project content never leaves the machine",
             "retitled the day it was written, 19f9dfb to c1a61ad, 2026-08-28 - "
             "the log's read-back says it moved three times that day"),
    "D-32": ("v1 answers questions about the model, and does not change it",
             "reversed within the hour of being written, 8cd98a3 to b950ff8, "
             "2026-08-28 - the log's read-back says so"),
}


class CouldNot(Exception):
    """This tool could not look. Exit 2, never a pass."""


def git(root, args, text=True):
    try:
        out = subprocess.run(["git", "-C", root] + args, capture_output=True)
    except OSError as exc:
        raise CouldNot("git could not be run: %s" % exc)
    if out.returncode != 0:
        said = out.stderr.decode("utf-8", "replace").strip().splitlines()
        raise CouldNot("git %s failed: %s" % (" ".join(args[:2]), said[-1] if said else "no message"))
    return out.stdout.decode("utf-8", "replace") if text else out.stdout


def headings(text):
    """[(number, title)] of the decision headings in one version of the log.

    A heading inside a fenced block is an example, not a decision - the log's
    own Format section carries `## D-NN` as a template."""
    found, fenced = [], False
    for line in text.replace(chr(13), "").split(NL):
        if line.lstrip().startswith(FENCE):
            fenced = not fenced
            continue
        if fenced:
            continue
        m = HEADING.match(line)
        if m:
            found.append((m.group(1), m.group(2)))
    return found


def entries(text):
    """{number: (title, [Numbering lines])} for the log as it stands."""
    out, current, fenced = {}, None, False
    for line in text.replace(chr(13), "").split(NL):
        if line.lstrip().startswith(FENCE):
            fenced = not fenced
            continue
        if fenced:
            continue
        m = HEADING.match(line)
        if m:
            current = m.group(1)
            if current not in out:
                out[current] = (m.group(2), [])
            continue
        if line.startswith("#"):
            current = None
        elif current and line.startswith(NUMBERING):
            out[current][1].append(line)
    return out


def history(root, published):
    """
    Every (number, title) the log has ever carried in `published`'s history,
    with the commit that first carried it: {(number, title): (time, sha)}.
    """
    if git(root, ["rev-parse", "--is-shallow-repository"]).strip() == "true":
        raise CouldNot("this clone's history is cut short (shallow), so what a "
                       "decision was FIRST written with cannot be known here. "
                       "Fetch the rest - `git fetch --unshallow` - and run it again.")
    git(root, ["rev-parse", "--verify", "--quiet", published + "^{commit}"])
    commits = git(root, ["log", "--full-history", "--format=%H %ct", published, "--", LOG]).split(NL)
    commits = [c.split(" ") for c in commits if c.strip()]
    if not commits:
        raise CouldNot("%s has no history in %s" % (LOG, published))
    batch = "".join("%s:%s%s" % (sha, LOG, NL) for sha, _ in commits).encode("utf-8")
    try:
        out = subprocess.run(["git", "-C", root, "cat-file", "--batch"],
                             input=batch, capture_output=True)
    except OSError as exc:
        raise CouldNot("git could not be run: %s" % exc)
    if out.returncode != 0:
        raise CouldNot("git cat-file failed")
    blob, at, seen = out.stdout, 0, {}
    for sha, when in commits:
        end = blob.index(NL.encode("ascii"), at)
        header = blob[at:end].decode("utf-8", "replace").split(" ")
        at = end + 1
        if len(header) < 3 or header[1] != "blob":
            continue                        # the log did not exist at this commit
        size = int(header[2])
        text = blob[at:at + size].decode("utf-8", "replace")
        at += size + 1
        for pair in headings(text):
            key = (int(when), sha)
            if pair not in seen or key < seen[pair]:
                seen[pair] = key
    return seen, len(commits)


def firsts(seen):
    """The first title of every number, and the first number of every title."""
    title_of, number_of = {}, {}
    for (number, title), key in seen.items():
        if number not in title_of or key < title_of[number][1]:
            title_of[number] = (title, key)
        if title not in number_of or key < number_of[title][1]:
            number_of[title] = (number, key)
    return title_of, number_of


def names(line, number):
    """True when `line` names `number` as a whole id - D-4 is not D-45."""
    return re.search("(^|[^0-9A-Za-z-])" + number + "($|[^0-9])", line) is not None


def when(root, key):
    stamp = git(root, ["show", "-s", "--format=%cs", key[1]]).strip()
    return "%s, %s" % (key[1][:7], stamp)


class Report(object):
    def __init__(self):
        self.failed, self.accepted, self.known = [], [], []
        self.commits = 0
        self.decisions = 0


def audit(root=ROOT, published="HEAD", known=None):
    """Report for the log at `root` against the history of `published`."""
    known = KNOWN if known is None else known
    report = Report()
    try:
        text = io.open(os.path.join(root, LOG), encoding="utf-8").read()
    except (IOError, OSError) as exc:
        raise CouldNot("could not read %s: %s" % (LOG, exc))
    now = entries(text)
    report.decisions = len(now)
    seen, report.commits = history(root, published)
    title_of, number_of = firsts(seen)

    for number in sorted(now, key=lambda n: int(n[2:])):
        title, lines = now[number]
        first = title_of.get(number)
        if first and first[0] != title:
            if any(first[0] in line for line in lines):
                report.accepted.append('%s  first written as "%s" (%s); its Numbering line quotes it'
                                       % (number, first[0], when(root, first[1])))
            elif number in known and known[number][0] == first[0]:
                report.known.append('%s  first "%s" - %s' % (number, first[0], known[number][1]))
            else:
                report.failed.append(
                    '%s was first written as "%s" (%s) and its heading now says "%s". '
                    "A new decision takes a new number. If the number really was reused, "
                    "give the decision a Numbering line that quotes the first title."
                    % (number, first[0], when(root, first[1]), title))
        moved = number_of.get(title)
        if moved and moved[0] != number:
            if any(names(line, moved[0]) for line in lines):
                report.accepted.append("%s  its title was first written under %s (%s); its Numbering line names %s"
                                       % (number, moved[0], when(root, moved[1]), moved[0]))
            else:
                report.failed.append(
                    '"%s" was first written as %s (%s) and is now %s. A decision that moves '
                    "number needs a Numbering line naming the number it had."
                    % (title, moved[0], when(root, moved[1]), number))

    for number in sorted(set(title_of) - set(now), key=lambda n: int(n[2:])):
        title, key = title_of[number]
        report.failed.append('%s was written as "%s" (%s) and is no longer in the log. '
                             "Nothing leaves it: a reversal gets a new entry."
                             % (number, title, when(root, key)))

    # A DECISION WRITTEN SECOND UNDER A NUMBER IS LOST THE SAME WAY, AND THE
    # THREE RULES ABOVE CANNOT SEE IT. Main writes D-02; a branch that has not
    # seen it writes its own D-02; the merge keeps main's log whole. Main's
    # D-02 still has its first title, nothing moved and no number left - and
    # the branch's decision is gone. Every title the log ever carried must
    # still be in it; a number's FIRST title is judged by the first rule.
    written = set(title for title, _ in now.values())
    for (number, title), key in sorted(seen.items(), key=lambda pair: pair[1]):
        if title in written or title_of[number][0] == title:
            continue
        report.failed.append('"%s" was written as %s (%s), when %s already meant "%s", and it is '
                             "no longer in the log. A number means one decision - the later one "
                             "needs a number of its own." % (title, number, when(root, key), number,
                                                             title_of[number][0]))

    for number in sorted(known, key=lambda n: int(n[2:])):
        first = title_of.get(number)
        entry = now.get(number)
        if not first or first[0] != known[number][0]:
            report.failed.append('KNOWN names %s as first written "%s", and the history does not '
                                 "show that. Correct the entry or remove it." % (number, known[number][0]))
        elif not entry or entry[0] == first[0] or any(first[0] in line for line in entry[1]):
            report.failed.append("KNOWN still excuses %s, which needs no excuse now. "
                                 "Remove the entry - the list only shrinks." % number)

    folder = os.path.join(root, RECORDS)
    for name in sorted(os.listdir(folder)) if os.path.isdir(folder) else []:
        m = re.match("^(D-[0-9]+)[.]md$", name)
        if not m:
            continue
        try:
            with io.open(os.path.join(folder, name), encoding="utf-8-sig") as handle:
                top = headings(handle.readline())
        except (IOError, OSError) as exc:
            report.failed.append("%s/%s could not be read: %s" % (RECORDS, name, exc))
            continue
        number = m.group(1)
        if number not in now:
            report.failed.append("%s/%s is a decision record with no heading in the log - "
                                 "a decision the index no longer holds." % (RECORDS, name))
        elif not top or top[0] != (number, now[number][0]):
            report.failed.append("%s/%s does not open with the log's heading, "
                                 '"# %s %s %s" - one of the two changed.'
                                 % (RECORDS, name, number, chr(0x2014), now[number][0]))
    return report


def main(argv=None):
    # The titles carry characters the Windows code page cannot encode - an
    # arrow, a dash - and a redirected print dies on them there
    # (.claude/skills/heron-ship section 2). UTF-8 keeps them intact.
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except AttributeError:
            pass
    parser = argparse.ArgumentParser(description="A decision keeps the title it was first written with.")
    parser.add_argument("--published", default="HEAD",
                        help="the history that counts as written - HEAD by default; CI passes HEAD^1")
    args = parser.parse_args(argv)
    print("check-decision-titles - a decision keeps the title it was first written with")
    print()
    try:
        report = audit(ROOT, args.published)
    except CouldNot as exc:
        print("NOT RUN - %s" % exc)
        return COULD_NOT
    print("  %d decisions in %s, read against %d commits of its history (%s)"
          % (report.decisions, LOG, report.commits, args.published))
    for heading, rows in (("ACCEPTED - the decision's own Numbering line says why", report.accepted),
                          ("KNOWN - changed before this check existed, each named rather than excused", report.known),
                          ("FAILED", report.failed)):
        if rows:
            print()
            print(heading)
            for row in rows:
                print("  " + row)
    print()
    if report.failed:
        print("FAIL - %d finding(s). A number is one decision for ever." % len(report.failed))
        return FOUND
    print("PASS - every decision carries the title it was first written with, or says why not.")
    return OK


if __name__ == "__main__":
    sys.exit(main())
