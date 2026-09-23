# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
tools/check-decision-titles.py fails when a decision's heading stops carrying
the title its number was first written with, and passes on today's log.

    python tests/test_check_decision_titles.py

Every case builds its own small git repository in a temporary folder, so it
needs git and nothing else - not this repository's history, which CI's
checkout does not have.

WHAT IS PROVED
--------------
  1. a title that holds passes, and a changed one fails - committed or not;
  2. a Numbering line excuses a change only when it QUOTES the first title,
     and a moved title only when the line NAMES its first number - D-4 is not
     D-45;
  3. a decision that leaves the log fails;
  4. THE WAY D-45 TO D-49 WERE LOST fails: two branches each write a number,
     and a merge keeps one side's log whole - so the lost side is found only by
     reading the history that was thrown away - whichever side wrote first;
  5. a branch that renumbers its own decision after main took the number is
     not failed against main's history, which is why CI passes --published
     HEAD^1;
  6. a decision record whose heading drifts from the log fails, and so does a
     record the log no longer holds;
  7. KNOWN can only shrink: an entry the history does not bear out, or one no
     longer needed, fails;
  8. a shallow clone exits 2 - NOT RUN, never a pass;
  9. TODAY'S LOG PASSES, over a history carrying every retitle the real one
     does: D-45 and D-46 pass because their Numbering lines quote their first
     titles, D-97 and D-98 because theirs name D-45 and D-46 - and each FAILS
     the moment its line is taken away. A changed title in today's log fails;
 10. the real repository passes, where its history is whole;
 11. CI runs it, with the whole history fetched for it.
"""

import importlib.util
import io
import os
import shutil
import stat
import subprocess
import sys
import tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
TOOL_PATH = os.path.join(ROOT, "tools", "check-decision-titles.py")

# Loaded so that its ABSENCE is one clean failure, not a traceback that says
# only that something went wrong (.claude/skills/heron-ship section 2a).
try:
    spec = importlib.util.spec_from_file_location("check_decision_titles", TOOL_PATH)
    TOOL = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(TOOL)
except (IOError, OSError, SyntaxError):
    TOOL = None

# Titles out of docs/DECISIONS.md carry an arrow and dashes, and a redirected
# print of one dies in the Windows code page (.claude/skills/heron-ship s2).
for _stream in (sys.stdout, sys.stderr):
    try:
        _stream.reconfigure(encoding="utf-8", errors="replace")
    except AttributeError:
        pass

NL = chr(10)
DASH = chr(0x2014)
FENCE = "`" * 3
FAILURES = []

# What D-45 and D-46 were first written as. Their own Numbering lines quote
# these; the history this suite builds for today's log carries them.
FIRST = {
    "D-45": "The library is built out first, and proved in one pass later",
    "D-46": "A context fragment is consumed by the host, not by another fragment",
}


def check(ok, said):
    print("  %s  %s" % ("ok  " if ok else "FAIL", said))
    if not ok:
        FAILURES.append(said)


def _writable(func, path, _info):
    os.chmod(path, stat.S_IWRITE)       # git marks its objects read-only on Windows
    func(path)


def git(repo, *args, **kw):
    env = dict(os.environ)
    for key in ("GIT_AUTHOR", "GIT_COMMITTER"):
        env[key + "_NAME"] = "suite"
        env[key + "_EMAIL"] = "suite@example.invalid"
        if kw.get("date"):
            env[key + "_DATE"] = kw["date"]
    out = subprocess.run(["git", "-c", "init.defaultBranch=main", "-c", "commit.gpgsign=false",
                          "-c", "core.autocrlf=false", "-C", repo] + list(args),
                         env=env, capture_output=True)
    if out.returncode != 0 and not kw.get("may_fail"):
        raise RuntimeError("git %s: %s" % (" ".join(args), out.stderr.decode("utf-8", "replace")))
    return out.stdout.decode("utf-8", "replace").strip()


def log_text(entries):
    """A small DECISIONS.md: a Format template inside a fence, then the entries."""
    lines = ["# Decision log", "", "## Format", "", FENCE + "markdown",
             "## D-NN " + DASH + " Short title", FENCE, ""]
    for entry in entries:
        number, title = entry[0], entry[1]
        lines += ["## %s %s %s" % (number, DASH, title), "", "**Status:** Accepted"]
        lines += list(entry[2]) if len(entry) > 2 else []
        lines += ["", "**Full record:** [`decisions/%s.md`](decisions/%s.md)" % (number, number), ""]
    return NL.join(lines) + NL


def write(repo, path, text):
    full = os.path.join(repo, *path.split("/"))
    folder = os.path.dirname(full)
    if not os.path.isdir(folder):
        os.makedirs(folder)
    with io.open(full, "w", encoding="utf-8", newline="") as handle:
        handle.write(text)


def commit(repo, date, files, message="change"):
    for path, text in files.items():
        if text is None:
            os.remove(os.path.join(repo, *path.split("/")))
        else:
            write(repo, path, text)
    git(repo, "add", "-A")
    git(repo, "commit", "-q", "--allow-empty", "-m", message, date=date)
    return git(repo, "rev-parse", "HEAD")


def new_repo(parent):
    repo = tempfile.mkdtemp(dir=parent)
    git(repo, "init", "-q")
    return repo


def audit(repo, published="HEAD", known=None):
    """(report, None) or (None, the reason it could not look)."""
    try:
        return TOOL.audit(repo, published, {} if known is None else known), None
    except TOOL.CouldNot as exc:
        return None, str(exc)


def said(report, *words):
    rows = (report.failed if report else [])
    return any(all(w in row for w in words) for row in rows)


def cli(repo, *args):
    """Run the tool itself, from inside `repo`, and return its exit code."""
    target = os.path.join(repo, "tools")
    if not os.path.isdir(target):
        os.makedirs(target)
    shutil.copy(TOOL_PATH, target)
    out = subprocess.run([sys.executable, os.path.join(target, "check-decision-titles.py")] + list(args),
                         capture_output=True)
    return out.returncode, out.stdout.decode("utf-8", "replace")


LOG = "docs/DECISIONS.md"
DAY = ["2026-08-%02dT12:00:00+00:00" % d for d in range(1, 31)]


def main():
    work = tempfile.mkdtemp()
    try:
        run(work)
    finally:
        shutil.rmtree(work, onerror=_writable)
    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        return 1
    print("PASSED  a decision keeps the title it was first written with")
    return 0


def run(work):
    check(TOOL is not None, "tools/check-decision-titles.py exists and loads")
    for name in ("audit", "CouldNot", "KNOWN", "main"):
        check(getattr(TOOL, name, None) is not None, "the tool has %s" % name)
    if FAILURES:
        return

    print()
    print("1. A title that holds passes, and a changed one fails")
    repo = new_repo(work)
    commit(repo, DAY[0], {LOG: log_text([("D-01", "Alpha"), ("D-02", "Beta")])})
    commit(repo, DAY[1], {LOG: log_text([("D-01", "Alpha"), ("D-02", "Beta"), ("D-03", "Gamma")])})
    report, why = audit(repo)
    check(report is not None and not report.failed,
          "two decisions kept and a third added: no finding (%s)" % (why or "%d commits" % report.commits))
    write(repo, LOG, log_text([("D-01", "Alpha"), ("D-02", "Beta, reworded"), ("D-03", "Gamma")]))
    report, _ = audit(repo)
    check(said(report, "D-02", '"Beta"', '"Beta, reworded"'),
          "a heading changed in the working tree fails, naming both titles")
    commit(repo, DAY[2], {})
    report, _ = audit(repo)
    check(said(report, "D-02", '"Beta"'), "and still fails once it is committed")

    print()
    print("2. The excuse is the decision's own Numbering line, and it has to say the right thing")
    vague = ["**Numbering:** this number was renamed."]
    write(repo, LOG, log_text([("D-01", "Alpha"), ("D-02", "Beta, reworded", vague), ("D-03", "Gamma")]))
    report, _ = audit(repo)
    check(said(report, "D-02"), "a Numbering line that does not quote the first title excuses nothing")
    quoting = ['**Numbering:** until 2026-08-03 D-02 was *"Beta"* - that decision is now elsewhere.']
    write(repo, LOG, log_text([("D-01", "Alpha"), ("D-02", "Beta, reworded", quoting), ("D-03", "Gamma")]))
    report, _ = audit(repo)
    check(report is not None and not report.failed and any("D-02" in a for a in report.accepted),
          "one that quotes it is accepted, and reported as accepted rather than silently passed")

    repo = new_repo(work)
    commit(repo, DAY[0], {LOG: log_text([("D-01", "Alpha"), ("D-45", "Delta")])})
    moved = [("D-01", "Alpha"), ("D-45", "Epsilon", ['**Numbering:** D-45 was *"Delta"*, now D-46.']),
             ("D-46", "Delta", ["**Numbering:** restored here; it was D-4, then D-450."])]
    write(repo, LOG, log_text(moved))
    report, _ = audit(repo)
    check(said(report, '"Delta"', "D-45", "D-46"),
          "a title that moved number fails when its line names D-4 and D-450 but not D-45")
    moved[2] = ("D-46", "Delta", ["**Numbering:** restored here; it was D-45 until 2026-08-02."])
    write(repo, LOG, log_text(moved))
    report, _ = audit(repo)
    check(report is not None and not report.failed, "and passes when the line names D-45")

    print()
    print("3. A decision that leaves the log fails")
    repo = new_repo(work)
    commit(repo, DAY[0], {LOG: log_text([("D-01", "Alpha"), ("D-02", "Beta")])})
    write(repo, LOG, log_text([("D-01", "Alpha")]))
    report, _ = audit(repo)
    check(said(report, "D-02", "no longer in the log"), "D-02 gone from the log is a finding")

    print()
    print("4. The way D-45 to D-49 were lost: two branches, and a merge that kept one side whole")
    repo = new_repo(work)
    commit(repo, DAY[0], {LOG: log_text([("D-01", "Alpha")])})
    git(repo, "checkout", "-q", "-b", "side")
    commit(repo, DAY[1], {LOG: log_text([("D-01", "Alpha"), ("D-02", "Written first, on a branch"),
                                         ("D-03", "Also only on the branch")])})
    git(repo, "checkout", "-q", "main")
    commit(repo, DAY[2], {LOG: log_text([("D-01", "Alpha"), ("D-02", "Written second, on main")])})
    git(repo, "merge", "-q", "-s", "ours", "--no-edit", "side", date=DAY[3])
    same = git(repo, "diff", "HEAD^1", "HEAD", "--stat")
    check(same == "", "the merge kept main's log byte for byte - the branch's side is only in history")
    report, why = audit(repo)
    check(said(report, "D-02", '"Written first, on a branch"', '"Written second, on main"'),
          "the number whose first decision was dropped fails (%s)" % (why or "found"))
    check(said(report, "D-03", "no longer in the log"),
          "and so does the number that left with it")
    plain = git(repo, "log", "--format=%h", "HEAD", "--", LOG).split(NL)
    full = git(repo, "log", "--full-history", "--format=%h", "HEAD", "--", LOG).split(NL)
    check(len(full) > len(plain),
          "which only --full-history can see: git's default walk drops the side the merge threw away "
          "(%d commits against %d)" % (len(full), len(plain)))

    # The same merge the other way round: MAIN wrote D-02 first. Its D-02
    # keeps its first title, nothing moved, no number left - and the branch's
    # D-02 is gone all the same.
    repo = new_repo(work)
    commit(repo, DAY[0], {LOG: log_text([("D-01", "Alpha")])})
    git(repo, "checkout", "-q", "-b", "side")
    git(repo, "checkout", "-q", "main")
    commit(repo, DAY[1], {LOG: log_text([("D-01", "Alpha"), ("D-02", "Main wrote this first")])})
    git(repo, "checkout", "-q", "side")
    commit(repo, DAY[2], {LOG: log_text([("D-01", "Alpha"), ("D-02", "The branch wrote this second")])})
    git(repo, "checkout", "-q", "main")
    git(repo, "merge", "-q", "-s", "ours", "--no-edit", "side", date=DAY[3])
    report, _ = audit(repo)
    check(said(report, '"The branch wrote this second"', "D-02", '"Main wrote this first"'),
          "and when main wrote the number first, the branch's decision is found lost too")

    print()
    print("5. A branch that renumbers its own decision is judged against main, as CI does")
    repo = new_repo(work)
    commit(repo, DAY[0], {LOG: log_text([("D-01", "Alpha")])})
    git(repo, "checkout", "-q", "-b", "branch")
    commit(repo, DAY[1], {LOG: log_text([("D-01", "Alpha"), ("D-02", "The branch's decision")])})
    git(repo, "checkout", "-q", "main")
    commit(repo, DAY[2], {LOG: log_text([("D-01", "Alpha"), ("D-02", "Main's decision")])})
    git(repo, "checkout", "-q", "branch")
    git(repo, "merge", "-q", "-s", "ours", "--no-edit", "main", date=DAY[3])
    commit(repo, DAY[4], {LOG: log_text([("D-01", "Alpha"), ("D-02", "Main's decision"),
                                         ("D-03", "The branch's decision")])})
    report, why = audit(repo, "main")
    check(report is not None and not report.failed,
          "against main's history the renumbering is right, and passes (%s)" % (why or "clean"))
    report, _ = audit(repo, "HEAD")
    check(report is not None and report.failed,
          "against the branch's own history its draft D-02 reads as a rewrite - why CI passes HEAD^1")
    git(repo, "checkout", "-q", "main")
    git(repo, "merge", "-q", "--no-ff", "--no-edit", "branch", date=DAY[5])
    report, _ = audit(repo, "HEAD^1")
    check(report is not None and not report.failed,
          "and once merged, HEAD^1 - the base the merge went into - is main's history exactly")

    print()
    print("6. A decision record must open with the log's heading")
    repo = new_repo(work)
    commit(repo, DAY[0], {LOG: log_text([("D-01", "Alpha"), ("D-02", "Beta")]),
                          "docs/decisions/D-01.md": "# D-01 " + DASH + " Alpha" + NL + NL + "Body." + NL,
                          "docs/decisions/D-02.md": "# D-02 " + DASH + " Beta" + NL})
    report, _ = audit(repo)
    check(report is not None and not report.failed, "records that match their headings pass")
    write(repo, "docs/decisions/D-01.md", "# D-01 " + DASH + " Alpha, but not the log's" + NL)
    report, _ = audit(repo)
    check(said(report, "D-01.md", "does not open with"), "a record whose heading drifted fails")
    write(repo, "docs/decisions/D-01.md", "# D-01 " + DASH + " Alpha" + NL)
    write(repo, "docs/decisions/D-07.md", "# D-07 " + DASH + " Nowhere in the log" + NL)
    report, _ = audit(repo)
    check(said(report, "D-07.md", "no heading in the log"), "a record the log no longer holds fails")

    print()
    print("7. KNOWN can only shrink")
    repo = new_repo(work)
    commit(repo, DAY[0], {LOG: log_text([("D-01", "Old words"), ("D-02", "Beta")])})
    commit(repo, DAY[1], {LOG: log_text([("D-01", "New words"), ("D-02", "Beta")])})
    report, _ = audit(repo, known={"D-01": ("Old words", "why it changed")})
    check(report is not None and not report.failed and any("D-01" in k for k in report.known),
          "an entry the history bears out is accepted, and listed")
    report, _ = audit(repo, known={"D-01": ("Words it never had", "why")})
    check(said(report, "KNOWN", "D-01"), "an entry naming a first title the history does not show fails")
    report, _ = audit(repo, known={"D-01": ("Old words", "why"), "D-02": ("Beta", "why")})
    check(said(report, "KNOWN still excuses D-02"), "an entry for a title that never changed fails as stale")
    write(repo, LOG, log_text([("D-01", "New words", ['**Numbering:** it was *"Old words"*.']),
                               ("D-02", "Beta")]))
    report, _ = audit(repo, known={"D-01": ("Old words", "why")})
    check(said(report, "KNOWN still excuses D-01"),
          "and so does one the decision now records itself - remove it")

    print()
    print("8. A shallow clone is NOT RUN, never a pass")
    source = new_repo(work)
    commit(source, DAY[0], {LOG: log_text([("D-01", "Alpha")])})
    commit(source, DAY[1], {LOG: log_text([("D-01", "Changed")])})
    url = "file://" + ("" if source.startswith("/") else "/") + source.replace(os.sep, "/")
    shallow = os.path.join(work, "shallow")
    subprocess.run(["git", "clone", "-q", "--depth", "1", url, shallow], capture_output=True)
    code, out = cli(shallow)
    check(code == 2 and "NOT RUN" in out, "exit %s, and it says NOT RUN rather than passing" % code)
    code, out = cli(source)
    check(code == 1 and "FAIL" in out, "the same log with its history exits 1 (exit %s)" % code)

    print()
    print("9. TODAY'S LOG passes, over a history carrying every retitle the real one does")
    today = io.open(os.path.join(ROOT, *LOG.split("/")), encoding="utf-8").read()
    lines = today.split(NL)
    first = dict((n, t) for n, (t, _why) in TOOL.KNOWN.items())
    first.update(FIRST)
    then, heads = [], {}
    for line in lines:
        m = TOOL.HEADING.match(line)
        if m:
            heads[m.group(1)] = m.group(2)
        if m and m.group(1) in ("D-97", "D-98"):
            continue                                    # restored 2026-09-23: not yet written
        if m and m.group(1) in first:
            line = "## %s %s %s" % (m.group(1), DASH, first[m.group(1)])
        then.append(line)
    check(all(n in heads for n in list(first) + ["D-97", "D-98"]),
          "today's log holds every number the history below retitles")
    repo = new_repo(work)
    commit(repo, "2026-08-27T12:00:00+00:00", {LOG: NL.join(then)})
    records = {}
    folder = os.path.join(ROOT, "docs", "decisions")
    for name in os.listdir(folder):
        records["docs/decisions/" + name] = io.open(os.path.join(folder, name), encoding="utf-8").read()
    records[LOG] = today
    commit(repo, "2026-09-23T12:00:00+00:00", records)
    report, why = audit(repo, known=TOOL.KNOWN)
    check(report is not None and not report.failed,
          "today's log passes (%s)" % (why or "; ".join(report.failed)[:300] or "no finding"))
    accepted = " ".join(report.accepted) if report else ""
    for number in ("D-45", "D-46", "D-97", "D-98"):
        check(number in accepted, "%s is accepted on its Numbering line, and says so" % number)
    check(report is not None and len(report.known) == len(TOOL.KNOWN),
          "every KNOWN entry is still needed (%d of %d)" % (len(report.known) if report else 0, len(TOOL.KNOWN)))

    for number, words in (("D-45", ["D-45", FIRST["D-45"]]), ("D-97", [FIRST["D-45"], "D-45", "D-97"]),
                          ("D-46", ["D-46", FIRST["D-46"]]), ("D-98", [FIRST["D-46"], "D-46", "D-98"])):
        heading = "## %s %s %s" % (number, DASH, heads[number])
        start = today.index(heading)
        cut = today.index("**Numbering:**", start)
        end = today.index(NL, cut)
        write(repo, LOG, today[:cut] + today[end + 1:])
        report, _ = audit(repo, known=TOOL.KNOWN)
        check(said(report, *words), "take %s's Numbering line away and it FAILS" % number)
    heading = "## D-10 %s %s" % (DASH, heads["D-10"])
    write(repo, LOG, today.replace(heading, heading + ", reworded"))
    report, _ = audit(repo, known=TOOL.KNOWN)
    check(said(report, "D-10", heads["D-10"]), "a changed title in today's log FAILS, naming D-10")

    print()
    print("10. The real repository, where its history is whole")
    shallow = subprocess.run(["git", "-C", ROOT, "rev-parse", "--is-shallow-repository"],
                             capture_output=True).stdout.decode("utf-8", "replace").strip()
    if shallow != "false":
        print("  NOT RUN here - this clone is shallow, so its history is not whole. Section 9 ran")
        print("        today's log; the gates job runs this on the real history, fetched whole.")
    else:
        out = subprocess.run([sys.executable, TOOL_PATH], capture_output=True)
        check(out.returncode == 0, "check-decision-titles exits 0 on this repository (exit %d)" % out.returncode)

    print()
    print("11. CI runs it, with the whole history fetched")
    ci = io.open(os.path.join(ROOT, ".github", "workflows", "gates.yml"), encoding="utf-8").read()
    job = ci[ci.index(NL + "  gates:"):ci.index(NL + "  compile:")]
    check("python tools/check-decision-titles.py --published HEAD^1" in job,
          "the gates job runs it against the base a pull request merges into")
    check("fetch-depth: 0" in job, "and fetches the whole history first - a shallow checkout would be NOT RUN")


if __name__ == "__main__":
    sys.exit(main())
