# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Does any file this repository would publish carry a credential?

    python tools/check-secrets.py

Exits 0 when no tracked file carries one, 1 when one does, and 2 when it
could not look: no git, not a checkout, no Secrets block in .gitignore, or a
tracked file it could not read. A 2 is never a pass.

WHY THIS EXISTS (D-143)
------------------------
This repository is public (D-07), and Article 17 says a credential never goes
into a source file or a commit. docs/12 s5a asked for two layers: a redactor on
the way OUT, which is brain/heron_secrets.py, and a scan before anything is
published, which docs/12 and docs/17 both recorded as "not built" on
2026-10-18. CONTRIBUTING.md closes a change that carries a credential without
review - but that is a reviewer's check, and a reviewer reads the diff, not
the tree.

The owner made it a GATE rather than a report the same day: a key that reaches
main is published to every clone at once, and a report that does not fail the
build is read after the merge.

TWO QUESTIONS, EACH ANSWERED FROM THE ONE PLACE THAT ALREADY OWNS IT
---------------------------------------------------------------------
  THE CONTENTS of every tracked file, against heron_secrets.PATTERNS - the
  shapes the redactor already removes from a log. There is no second list
  here. A shape added there is caught here with no edit, and a list typed in
  this file would disagree with the redactor the first time somebody added a
  shape to one of them.

  THE NAMES of every tracked file, against the "Secrets" block of .gitignore,
  matched by git itself (`git ls-files --cached --ignored`), so a `!` line
  there is honoured exactly as git honours it. .gitignore stops a NEW file. It
  does nothing about one added with `git add -f`, or one tracked before its
  line was written - and those are exactly the files that reach history. When
  the block cannot be found the run exits 2 rather than checking against a
  list typed here, the rule heron_commit follows for its own block.

  The names are matched IGNORING CASE, on every system. git takes its case
  rule from core.ignorecase, which is true on a Windows checkout and unset on
  the Linux runner CI uses, so without this a tracked `.ENV` or `Server.PEM`
  failed on the owner's laptop and passed the gate that decides.

Only TRACKED files are read, because only they are published. A real .env in
a checkout, ignored as it should be, is not this tool's business.

WHAT IT READS, EXACTLY
-----------------------
  - The file as it is in the working tree, so a key pasted and not yet staged
    is found before it is pushed.
  - A symbolic link is never followed: what git publishes for one is the text
    of its target, so that text is what is scanned.
  - A file that starts with a UTF-16 byte-order mark is read as UTF-16 - it is
    what Windows PowerShell 5 writes with `>`, and its every second byte is a
    NUL, so the test for "not text" would otherwise skip it whole.
  - Lines are counted on "\\n" alone, as git and GitHub count them.

WHAT IT DOES NOT DO
--------------------
  - It never prints a value. A finding names the file, line, column and shape,
    which is the rule heron_commit applies to a commit message - and a file
    NAME goes through the redactor before it is printed, because a name can
    be a token too.
  - It does not read git HISTORY. A key committed and later deleted is still in
    every clone, and this tool cannot see it. A key that was ever pushed is
    public: rotate it, whatever this tool says today.
  - It does not read a file that is not text - a NUL byte in its first 8 KB
    and no UTF-16 mark. It counts those and prints the count, and its PASS
    line speaks only of the files it read.
  - It cannot recognise a secret shaped like ordinary text - the limit the
    redactor's own docstring names.

A tracked file it cannot open makes the run exit 2, because a pass would claim
a file nobody read. A tracked file missing from the working tree - deleted and
not yet committed - is counted and said, and is not a failure: it will not be
published as it was.

A FAKE IN A TEST
-----------------
A test that needs a token-shaped string builds it from pieces, the way
heron_secrets.FAKE_FORGE_TOKEN does, so no literal of that shape sits in the
source. That is the whole exception mechanism, and there is no list of allowed
findings: an allowance keyed on a file would let a real key into that file.
"""

from __future__ import annotations

import argparse
import codecs
import io
import os
import re
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_secrets as SECRETS  # noqa: E402

GIT = "git"

# The block is found by its banner line. The first rule after the banner closes
# the banner itself; the next rule opens the following block, and ends this one.
BANNER = re.compile(r"^#\s+Secrets\s*$")
RULE = re.compile(r"^#\s*=+\s*$")

# How much of a file is read to decide whether it is text - git's own test.
SNIFF = 8000

UTF16_MARKS = (codecs.BOM_UTF16_LE, codecs.BOM_UTF16_BE)


def secrets_block(ignore):
    """The entries of .gitignore's Secrets block, in order - [] when absent."""
    try:
        lines = io.open(ignore, encoding="utf-8").read().splitlines()
    except (IOError, OSError, ValueError):
        return []
    start = None
    for at, line in enumerate(lines):
        if BANNER.match(line.strip()):
            start = at + 1
            break
    if start is None:
        return []
    out = []
    rules = 0
    for line in lines[start:]:
        line = line.strip()
        if RULE.match(line):
            rules += 1
            if rules == 2:
                break
            continue
        if line and not line.startswith("#"):
            out.append(line)
    return out


def git(args, root):
    """(exit code, stdout) of git run in root - (None, b"") when git is absent."""
    try:
        done = subprocess.run([GIT, "-C", root] + list(args),
                              stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    except OSError:
        return None, b""
    return done.returncode, done.stdout


def shown(raw):
    """A path as it may be printed: never undecodable, never a token."""
    if isinstance(raw, bytes):
        raw = raw.decode("utf-8", "backslashreplace")
    return SECRETS.Secrets().redact(raw)[0]


def as_text(data):
    """The file's text, or None when it is not text."""
    if data.startswith(UTF16_MARKS):
        return data.decode("utf-16", "replace")
    if b"\0" in data[:SNIFF]:
        return None
    return data.decode("utf-8", "replace")


def scan_text(text):
    """[(line, column, shape)] for every credential shape in the text."""
    found = []
    for number, line in enumerate(text.split("\n"), 1):
        line = line.rstrip("\r")
        for shape, pattern in SECRETS.PATTERNS:
            for match in pattern.finditer(line):
                found.append((number, match.start() + 1, shape))
    return found


def _say(out, text):
    try:
        out.write(text)
    except UnicodeEncodeError:
        out.write(text.encode("ascii", "backslashreplace").decode("ascii"))


def run(root=ROOT, out=None):
    """Scan the checkout at root. Returns the exit code; writes the report."""
    out = out or sys.stdout
    block = secrets_block(os.path.join(root, ".gitignore"))
    if not block:
        _say(out, "check-secrets: could not run - no entries in a '#  Secrets' "
                  "block of .gitignore, so nothing says which file names are "
                  "credentials. A list typed here would be a guess.\n")
        return 2

    code, listed = git(["ls-files", "-z"], root)
    if code is None:
        _say(out, "check-secrets: could not run - git was not found.\n")
        return 2
    if code != 0:
        _say(out, "check-secrets: could not run - '%s' is not a git "
                  "checkout.\n" % root)
        return 2
    tracked = [path for path in listed.split(b"\0") if path]

    code, matched = git(["-c", "core.ignorecase=true", "ls-files", "-z",
                         "--cached", "--ignored"]
                        + ["--exclude=%s" % entry for entry in block], root)
    if code != 0:
        _say(out, "check-secrets: could not run - git could not match the "
                  "Secrets block against the tracked files.\n")
        return 2
    named = [path for path in matched.split(b"\0") if path]

    base = os.fsencode(root)
    findings, unreadable = [], []
    read = not_text = missing = 0
    for path in tracked:
        full = os.path.join(base, path)
        try:
            if os.path.islink(full):
                data = os.fsencode(os.readlink(full))
            else:
                with open(full, "rb") as handle:
                    data = handle.read()
        except FileNotFoundError:
            missing += 1            # deleted here and not yet committed
            continue
        except (IOError, OSError) as exc:
            unreadable.append((path, type(exc).__name__))
            continue
        text = as_text(data)
        if text is None:
            not_text += 1
            continue
        read += 1
        for line, column, shape in scan_text(text):
            findings.append((path, line, column, shape))

    _say(out, "check-secrets: %d tracked file(s); %d read as text, %d not text "
              "and not read%s%s\n"
              % (len(tracked), read, not_text,
                 ", %d missing from the working tree" % missing
                 if missing else "",
                 ", %d could not be opened" % len(unreadable)
                 if unreadable else ""))
    _say(out, "  shapes: %d, from heron_secrets.PATTERNS\n"
              % len(SECRETS.PATTERNS))
    _say(out, "  names:  %d entries, from the Secrets block of .gitignore, "
              "case ignored\n" % len(block))

    for path in named:
        _say(out, "FAIL  %s - tracked, and its name is one the Secrets block "
                  "of .gitignore refuses\n" % shown(path))
    for path, line, column, shape in findings:
        _say(out, "FAIL  %s:%d:%d - %s\n" % (shown(path), line, column, shape))
    if named or findings:
        _say(out, "\n%d finding(s). No value is printed. Take it out of the "
                  "change; if it was ever pushed it is public already, so "
                  "rotate it too. A test that needs a fake builds it from "
                  "pieces, as heron_secrets.FAKE_FORGE_TOKEN does.\n"
                  % (len(named) + len(findings)))
        return 1

    if unreadable:
        for path, why in unreadable:
            _say(out, "NOT READ  %s - %s\n" % (shown(path), why))
        _say(out, "could not run to the end - %d tracked file(s) could not be "
                  "opened, and a pass would claim them.\n" % len(unreadable))
        return 2

    _say(out, "PASS  no tracked file that was read carries a credential "
              "shape, and none has a name the Secrets block refuses\n")
    return 0


def main(argv=None):
    parser = argparse.ArgumentParser(
        description="Fail when a tracked file carries a credential shape or "
                    "has a name the Secrets block of .gitignore refuses.")
    parser.parse_args(argv)
    return run()


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
