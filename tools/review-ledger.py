# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Which files have been READ, word by word, and which have not.

    python tools/review-ledger.py                          # the balance
    python tools/review-ledger.py --next 20                # what to read next
    python tools/review-ledger.py --mark PATH clean
    python tools/review-ledger.py --mark PATH issue --note 5b-3
    python tools/review-ledger.py --stale                  # marks that no longer apply
    python tools/review-ledger.py --history PATH           # what this file has been through

Always exits 0. This reports; it does not gate.

WHY THIS EXISTS
---------------
The check-*.py gates - `ls tools/check-*.py | wc -l` of them, and the number
is derived there rather than typed here, because this file of all files has no
business carrying a count it cannot invalidate - check RULES: does it compile,
is the metadata there,
does the routing resolve, does the licence header exist. Not one of them
records that a file was READ. So a second session had no way to know the first
had already read a file, and the only honest thing it could do was read it
again. Across the whole sweep that is every file read twice, and the second
pass is indistinguishable from the first in every report Heron prints.

No total is typed in this file. Run it and it derives one - a number written
into a docstring is a cache with no invalidation, and this tool exists to
argue that point one layer down.

THE MARK CARRIES THE CONTENT HASH, AND THAT IS THE WHOLE DESIGN
---------------------------------------------------------------
open-defects.py already names the failure this file could easily become:

    A PROSE TOTAL IS A CACHE WITH NO INVALIDATION.

A bare "checked" tick is exactly that. Marked today, the file edited tomorrow,
and the tick still reads "checked" while describing content nobody has seen.
It is worse than no tick, because a session trusts it and skips the file.

So every row carries the git blob hash of the file AS IT WAS READ. The hash is
not a convenience column, it IS the invalidation: when the file changes by one
byte the hash stops matching and the row goes STALE on its own. Nobody has to
remember to withdraw it, and no session has to trust that somebody did.

WHAT IS IN SCOPE
----------------
Every tracked file EXCEPT *.yaml under brain/, which keeps its own gates and
the retrieval evals - the owner's decision, 2026-09-20. Binaries are out too,
because "word by word" does not mean anything for them.

THE LEDGER IS APPEND-ONLY
-------------------------
Rows are never rewritten and never removed. The newest row for a path is the
one that counts; the rows behind it are the history of what that file has been
through. A file that was read clean, then edited, then found to have a defect
reads in that order, and --history prints it.

A FINDING IS RECORDED, NOT FIXED
---------------------------------
Marking a file `issue` does not change the file. The defect itself belongs in
docs/FRAGMENT-ISSUES.md section 5b, and --note carries its row number so the
next session can go straight to it. That separation is deliberate: a sweep
that also fixes things stops being a sweep, and the reader loses track of
which files have actually been read.
"""

import argparse
import datetime
import io
import os
import re
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LEDGER = os.path.join(ROOT, "docs", "REVIEW-LEDGER.tsv")

# The same file in git's spelling, so in_scope() can leave it out of its own
# sweep, and the register the `issue` notes have to point into.
LEDGER_TRACKED = "docs/REVIEW-LEDGER.tsv"
REGISTER = os.path.join(ROOT, "docs", "FRAGMENT-ISSUES.md")

COLUMNS = ["when", "who", "path", "blob", "verdict", "note"]
VERDICTS = ("clean", "issue")

# Out of scope. Word by word means nothing for these.
BINARY_EXT = (".png", ".jpg", ".jpeg", ".gif", ".ico", ".pdf", ".dll",
              ".zip", ".rvt", ".rfa", ".rte")


def out(s=""):
    sys.stdout.write(s + "\n")


def git(args, stdin_text=None):
    """Run git in the repo and return stdout. stdin is ALWAYS closed - an open
    stdin is how a script like this hangs for two minutes per call."""
    p = subprocess.run(
        ["git"] + args,
        cwd=ROOT,
        input=("" if stdin_text is None else stdin_text),
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        universal_newlines=True,
    )
    if p.returncode != 0:
        out("git %s failed: %s" % (" ".join(args), p.stderr.strip()))
        return ""
    return p.stdout


def in_scope():
    """Every tracked file the sweep covers, in git's own path spelling."""
    paths = []
    for line in git(["ls-files"]).splitlines():
        p = line.strip()
        if not p:
            continue
        if p.startswith("brain/") and p.endswith(".yaml"):
            continue          # brain yaml keeps its own gates
        if p.lower().endswith(BINARY_EXT):
            continue
        if p == LEDGER_TRACKED:
            # THE LEDGER CANNOT REVIEW ITSELF, and leaving it in was not a
            # harmless oddity: marking it computes its hash and then APPENDS
            # the mark to that same file, so the hash is wrong the instant it
            # is written. The row would be stale before the command returned,
            # and the sweep could never report every file read - it would
            # always be one short, for ever, with no way to close it.
            # Reported by a Codex review on PR #219.
            continue
        paths.append(p)
    return sorted(paths)


def blobs(paths):
    """Content hash of each path as it stands in the WORKING TREE, one git call.

    Not `git ls-files -s`, which reports the index - a file edited and not yet
    staged would keep its old hash there and a stale mark would read as valid.

    A TRACKED FILE THAT IS NOT ON DISK IS NORMAL, AND USED TO BREAK EVERYTHING.
    `git ls-files` still lists a file deleted but not yet staged, and
    `hash-object --stdin-paths` fails on the whole batch when one path is
    missing - so a single in-progress deletion returned an empty dict, `state()`
    gave up, and the command printed an error instead of the balance for the
    other eleven hundred files. Missing paths are now left OUT of the result and
    the caller reports them as deleted, which is a fact worth showing rather
    than a reason to show nothing. Reported by a Codex review on PR #219.
    """
    if not paths:
        return {}
    here = [p for p in paths if os.path.isfile(os.path.join(ROOT, p))]
    if not here:
        return {}
    text = git(["hash-object", "--stdin-paths"], stdin_text="\n".join(here) + "\n")
    shas = [l.strip() for l in text.splitlines() if l.strip()]
    if len(shas) != len(here):
        out("Could not hash every file: asked for %d paths, got %d hashes."
            % (len(here), len(shas)))
        out("Refusing to report a balance from an incomplete hash set.")
        return {}
    return dict(zip(here, shas))


def read_ledger():
    """Every row, oldest first. A malformed row is reported, not skipped silently."""
    rows, bad = [], []
    if not os.path.exists(LEDGER):
        return rows, bad
    with io.open(LEDGER, "r", encoding="utf-8") as f:
        for n, line in enumerate(f, 1):
            line = line.rstrip("\r\n")
            if not line or line.startswith("#"):
                continue
            cells = line.split("\t")
            if cells[0] == "when":
                continue                      # the header row
            if len(cells) != len(COLUMNS):
                bad.append((n, line))
                continue
            row = dict(zip(COLUMNS, cells))
            # A VERDICT NOBODY DEFINED IS NOT A PASS. The row shapes were
            # checked and the verdict was not, so `cleen` - a typo, or the
            # wreckage of a hand-resolved merge conflict - fell through the
            # `clean` branch in state() and made the sweep SKIP a file that has
            # no valid review at all. It did not even show up in the malformed
            # list, because six columns were present. Unknown verdicts are
            # malformed now, so the file returns to the queue and the row is
            # named. Reported by a Codex review on PR #219.
            if row["verdict"] not in VERDICTS:
                bad.append((n, line))
                continue
            rows.append(row)
    return rows, bad


def latest_by_path(rows):
    """Append-only: the last row written for a path is the one that counts."""
    latest = {}
    for r in rows:
        latest[r["path"]] = r
    return latest


def state():
    """The balance: what is read, what is stale, what was never opened."""
    paths = in_scope()
    now = blobs(paths)
    if not now:
        return None
    rows, bad = read_ledger()
    latest = latest_by_path(rows)

    clean, found, stale, unchecked, gone = [], [], [], [], []
    for p in paths:
        # Tracked, but not on disk - a deletion that has not been staged yet.
        # Named rather than counted as unread: nobody needs to read a file that
        # is being removed, and calling it unread would keep the sweep one
        # short with no way to close it.
        if p not in now:
            gone.append(p)
            continue
        r = latest.get(p)
        if r is None:
            unchecked.append(p)
        elif r["blob"] != now[p]:
            stale.append(p)               # read once, edited since
        elif r["verdict"] == "issue":
            found.append((p, r))          # read, and something was wrong
        else:
            clean.append(p)

    live = set(paths)
    orphans = sorted(q for q in latest if q not in live)

    return {"paths": [p for p in paths if p in now], "now": now,
            "latest": latest, "bad": bad,
            "clean": clean, "found": found, "stale": stale,
            "unchecked": unchecked, "orphans": orphans, "gone": gone}


def by_dir(paths):
    counts = {}
    for p in paths:
        top = p.split("/")[0] if "/" in p else "(root)"
        counts[top] = counts.get(top, 0) + 1
    return counts


def cmd_status(st):
    total = len(st["paths"])
    read_n = len(st["clean"]) + len(st["found"])

    out("Files READ word by word - docs/REVIEW-LEDGER.tsv\n")
    out("  read and clean      %5d" % len(st["clean"]))
    out("  read, issue found   %5d" % len(st["found"]))
    out("  ---------------------------")
    out("  read                %5d  of %d" % (read_n, total))
    out("  never opened        %5d" % len(st["unchecked"]))
    out("  STALE - changed     %5d   (read once, edited since - read again)"
        % len(st["stale"]))
    if st["gone"]:
        out("  tracked, not on disk%5d   (a deletion not staged yet - not counted above)"
            % len(st["gone"]))
    out("")
    out("  left to read        %5d" % (len(st["unchecked"]) + len(st["stale"])))

    left = st["unchecked"] + st["stale"]
    if left:
        out("\nWhere the work is left:")
        counts = by_dir(left)
        for d in sorted(counts, key=lambda k: -counts[k]):
            out("  %-12s %5d" % (d, counts[d]))

    if st["found"]:
        out("\nRead, and something was wrong - the defect is in section 5b:")
        for p, r in st["found"]:
            note = r["note"] or "(no row number written - find it in 5b)"
            out("  %-60s  %s" % (p, note))

    if st["orphans"]:
        out("\nRows for files that are no longer tracked (renamed or deleted):")
        for p in st["orphans"]:
            out("  %s" % p)

    if st["bad"]:
        out("\nMalformed ledger rows - fix these by hand, they are being ignored:")
        for n, line in st["bad"]:
            out("  line %d: %s" % (n, line[:90]))

    out("\n  python tools/review-ledger.py --next 20   # what to read next")


def cmd_next(st, n):
    # Stale first: those were read once and have changed since, so they are the
    # ones where the ledger is currently telling a session something untrue.
    queue = st["stale"] + st["unchecked"]
    if not queue:
        out("Nothing left. All %d files in scope have been read at their "
            "current content." % len(st["paths"]))
        return
    out("Next %d of %d left to read (STALE first, then never opened):\n"
        % (min(n, len(queue)), len(queue)))
    stale = set(st["stale"])
    for p in queue[:n]:
        out("  %-6s %s" % ("STALE" if p in stale else "", p))
    out("\nWhen a file is done:")
    out("  python tools/review-ledger.py --mark <path> clean")
    out("  python tools/review-ledger.py --mark <path> issue --note <5b row>")


def cmd_stale(st):
    if not st["stale"]:
        out("No stale marks. Every row describes the file as it stands now.")
        return
    out("Read once, edited since - the old mark no longer applies to %d file(s):\n"
        % len(st["stale"]))
    for p in st["stale"]:
        r = st["latest"][p]
        out("  %s" % p)
        out("      read %s by %s, verdict %s" % (r["when"], r["who"], r["verdict"]))
        out("      was %s   now %s" % (r["blob"][:12], st["now"][p][:12]))


def cmd_history(path):
    rows, _ = read_ledger()
    mine = [r for r in rows if r["path"] == path]
    if not mine:
        out("No row for %s - it has never been read." % path)
        return
    out("%s\n" % path)
    for r in mine:
        out("  %s  %-12s %-6s %s  %s"
            % (r["when"], r["who"], r["verdict"], r["blob"][:12], r["note"]))


_SECTION_5B = "## 5b. HERON'S OWN DEFECTS found by reading"
_SECTION_6 = "## 6. WHAT CANNOT BE RUN AT ALL"
_ROW = re.compile(r"^\|\s*\*{0,2}(\d+)\*{0,2}\s*\|")
_CITED = re.compile(r"^5b-(\d+)$", re.IGNORECASE)


def _row_id(text):
    """`5b-3` -> 3. Anything else -> None, so prose in a note is ignored
    rather than mistaken for a row that does not exist."""
    m = _CITED.match(text.strip().rstrip(".,;"))
    return int(m.group(1)) if m else None


def register_rows():
    """Every row number in section 5b, or None if the section cannot be read.

    None and an empty set are different answers and the caller treats them
    differently: an empty 5b means nothing has been recorded yet, and an
    unreadable one means this tool cannot check the reference at all - in which
    case it refuses rather than writing a mark nobody has verified.
    """
    try:
        with io.open(REGISTER, "r", encoding="utf-8") as handle:
            src = handle.read()
    except (IOError, OSError):
        return None
    if _SECTION_5B not in src:
        return None
    body = src[src.index(_SECTION_5B):]
    if _SECTION_6 in body:
        body = body[:body.index(_SECTION_6)]
    return set(int(m.group(1)) for m in
               (_ROW.match(line) for line in body.split("\n")) if m)


def who():
    """One id per person. HERON_CLIENT_ID first, then git's own idea of who
    this is - never a hardcoded default that quietly attributes work."""
    w = os.environ.get("HERON_CLIENT_ID", "").strip()
    if w:
        return w
    w = git(["config", "user.name"]).strip()
    return w.replace("\t", " ") if w else "unknown"


def cmd_mark(path, verdict, note):
    path = path.replace("\\", "/").strip()
    if verdict not in VERDICTS:
        out("Verdict must be one of: %s" % ", ".join(VERDICTS))
        return
    scope = in_scope()
    if path not in scope:
        out("%s is not in the sweep." % path)
        if path.startswith("brain/") and path.endswith(".yaml"):
            out("brain yaml keeps its own gates - it is deliberately out of scope.")
        elif not os.path.exists(os.path.join(ROOT, path)):
            out("No such tracked file. Check the spelling, forward slashes.")
        return
    if verdict == "issue":
        if not note:
            out("An issue needs --note with its row number in FRAGMENT-ISSUES.md 5b.")
            out("Write the defect there FIRST, then mark the file with its row.")
            out("A file marked `issue` with no row is a finding nobody can find.")
            return

        # AND THE ROW HAS TO EXIST. Requiring the note to be non-empty was not
        # the same as requiring it to POINT anywhere: `--note 5b-999`, or a
        # sentence, was accepted and appended for ever while the tool claimed
        # the defect could be found in section 5b. That is the exact state the
        # refusal above exists to prevent, reached by a typo.
        # Reported by a Codex review on PR #219.
        wanted = register_rows()
        if wanted is None:
            out("Could not read section 5b of docs/FRAGMENT-ISSUES.md, so the")
            out("row in --note cannot be checked. Refusing rather than writing")
            out("a mark whose reference nobody has verified.")
            return
        cited = [c for c in re.split(r"[,\s]+", note) if c.strip()]
        missing = [c for c in cited if _row_id(c) and _row_id(c) not in wanted]
        if not any(_row_id(c) for c in cited):
            out("--note must name at least one row in section 5b, like 5b-3.")
            out("Got: %s" % note)
            return
        if missing:
            out("No such row in FRAGMENT-ISSUES.md section 5b: %s"
                % ", ".join(missing))
            out("Section 5b has %d row(s): %s"
                % (len(wanted), ", ".join("5b-%d" % n for n in sorted(wanted))))
            out("Write the defect there FIRST. Nothing was recorded.")
            return

    sha = blobs([path]).get(path)
    if not sha:
        return
    row = [datetime.date.today().isoformat(), who(), path, sha, verdict,
           (note or "").replace("\t", " ").strip()]

    new = not os.path.exists(LEDGER)
    with io.open(LEDGER, "a", encoding="utf-8", newline="\n") as f:
        if new:
            f.write(u"\t".join(COLUMNS) + u"\n")
        f.write(u"\t".join(row) + u"\n")

    out("Marked %s" % path)
    out("  %s at %s by %s" % (verdict, sha[:12], row[1]))
    if verdict == "issue":
        out("  defect row: FRAGMENT-ISSUES.md 5b -> %s" % note)
    out("  If this file changes, the mark goes stale by itself.")


def main():
    ap = argparse.ArgumentParser(add_help=True, description=__doc__.strip().splitlines()[0])
    ap.add_argument("--next", type=int, metavar="N", help="the next N files to read")
    ap.add_argument("--mark", nargs=2, metavar=("PATH", "VERDICT"),
                    help="record that PATH was read: clean | issue")
    ap.add_argument("--note", default="", help="row number in FRAGMENT-ISSUES.md 5b")
    ap.add_argument("--stale", action="store_true", help="marks that no longer apply")
    ap.add_argument("--history", metavar="PATH", help="what one file has been through")
    a = ap.parse_args()

    if a.mark:
        cmd_mark(a.mark[0], a.mark[1], a.note)
        return 0
    if a.history:
        cmd_history(a.history.replace("\\", "/").strip())
        return 0

    st = state()
    if st is None:
        return 0
    if a.next:
        cmd_next(st, a.next)
    elif a.stale:
        cmd_stale(st)
    else:
        cmd_status(st)
    return 0


if __name__ == "__main__":
    sys.exit(main())
