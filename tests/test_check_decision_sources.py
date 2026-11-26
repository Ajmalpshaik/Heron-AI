# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Does the decision-source check refuse an Accepted decision whose yes cannot be
read anywhere, and nothing else?

    python tests/test_check_decision_sources.py

`tools/check-decision-sources.py` (D-206, row 5b-1986) fails when a decision
from D-206 on is Accepted and its **Accepted:** line links neither a pull
request or issue of this repository nor a claude.ai session.

EVERY CASE BUILDS ITS OWN LOG in a temp folder, as the supersession suite does,
so a case asserts the check and not today's decisions. The last two read the
real repository: D-206 itself must pass, and CI must run the check.

It does not prove a link leads to the owner's words - the check reads a link's
shape, and says so in its own docstring.

WHAT IS PROVED
--------------
  1. an Accepted decision linking a claude.ai session passes, and the run says
     what it read - the pass is not vacuous;
  2. a pull-request link and an issue-comment link pass;
  3. D-196'S FORM FAILS: Accepted on a quote "in conversation", no link;
  4. Accepted with no **Accepted:** line at all fails;
  5. what D-206 says does not count fails: a bare #N, a commit sha, another
     repository's issue, a session URL with no id;
  6. a link in another field - Answers, Affects - is not the Accepted line's;
  7. Proposed, Not accepted and Superseded owe nothing, and are printed;
  8. D-00 to D-205 are grandfathered: they never fail the run, and --all
     reports them without changing the exit code;
  9. an Accepted line wrapped onto a second line is read whole, and a field
     after it on the same line is not;
 10. a decision only in the index - not yet split - is read there;
 11. no decision anywhere exits 2, never a pass;
 12. the real repository passes, with D-206 among what it checked;
 13. CI's gates job runs it.
"""

import importlib.util
import io
import os
import shutil
import sys
import tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
TOOL = os.path.join(ROOT, "tools", "check-decision-sources.py")

FAILURES = []
MADE = []
DASH = u"—"
DOT = u"·"
SESSION = u"https://claude.ai/code/session_01AbCdEfGhIjKlMnOpQrStUv"
ISSUE = u"https://github.com/Ajmalpshaik/Heron-AI/issues/1262"
PULL = u"https://github.com/Ajmalpshaik/Heron-AI/pull/1238"

for _stream in (sys.stdout, sys.stderr):
    try:
        _stream.reconfigure(encoding="utf-8", errors="replace")
    except AttributeError:
        pass


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def load():
    """The tool as a module, or None - its name has hyphens in it."""
    try:
        spec = importlib.util.spec_from_file_location("check_decision_sources", TOOL)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        return module
    except Exception as exc:                      # noqa: BLE001 - reported
        print("  could not load %s: %s" % (TOOL, exc))
        return None


def record(did, *lines):
    return (u"# %s %s A title\n\n> One record.\n\n%s\n\n### Context\n\nWords.\n"
            % (did, DASH, u"\n".join(lines)))


def entry(did, *lines):
    return (u"## %s %s A title\n\n%s\n\n**Full record:** [`decisions/%s.md`](decisions/%s.md)\n\n"
            % (did, DASH, u"\n".join(lines), did, did))


def log(records=None, entries=None):
    """A repository holding docs/DECISIONS.md and docs/decisions/."""
    root = tempfile.mkdtemp(prefix="heron-sources-")
    MADE.append(root)
    os.makedirs(os.path.join(root, "docs", "decisions"))
    with io.open(os.path.join(root, "docs", "DECISIONS.md"), "w", encoding="utf-8") as f:
        f.write(u"# Decision Log\n\n> A preamble.\n\n" + u"".join(entries or []))
    for did, text in (records or {}).items():
        with io.open(os.path.join(root, "docs", "decisions", did + ".md"), "w",
                     encoding="utf-8") as f:
            f.write(text)
    return root


def accepted(did, where, status=u"Accepted"):
    return {did: record(did, u"**Status:** %s %s **Date:** 2026-11-27 %s **Accepted:** Ajmal PS, "
                             u"2026-11-27, %s" % (status, DOT, DOT, where))}


def run(tool, root, every=False):
    out = io.StringIO()
    code = tool.run(root=root, out=out, every=every)
    return code, out.getvalue()


def main():
    tool = load()
    if tool is None:
        print("FAILED  tools/check-decision-sources.py could not be loaded")
        return 1

    print("1. Accepted, linking a claude.ai session: passes")
    code, out = run(tool, log(accepted("D-207", u"in the session [s](%s): *\"yes\"*" % SESSION)))
    check(code == 0 and "PASS" in out, "exit 0 and PASS")
    check("1 accepted with a link" in out and "ok       D-207 - a Claude Code session" in out,
          "and it names what it read and the link - the pass is not vacuous")

    print()
    print("2. A pull-request link and an issue comment's link pass")
    code, _ = run(tool, log(accepted("D-207", u"on [#1238](%s): *\"merge: yes\"*" % PULL)))
    check(code == 0, "a pull request of this repository")
    code, _ = run(tool, log(accepted("D-207", u"on [#1262](%s#issuecomment-123): *\"yes\"*" % ISSUE)))
    check(code == 0, "an issue comment, by its anchor")

    print()
    print("3. D-196's form - a quote heard in a session's own chat, no link - fails")
    code, out = run(tool, log(accepted(
        "D-207", u"in conversation with the session working issues #935 and #1057, answering row "
                 u"5b-1903: *\"5b-1903: yes, also check handed elements\"*")))
    check(code == 1 and "NO LINK  D-207 is Accepted" in out, "exit 1, NO LINK, naming the decision")
    check("Proposed" in out and "D-206" in out, "and it says the way out: link it, or write it Proposed")

    print()
    print("4. Accepted with no Accepted line at all fails")
    code, out = run(tool, log({"D-207": record("D-207", u"**Status:** Accepted %s **Date:** x" % DOT)}))
    check(code == 1 and "NO LINE  D-207" in out, "exit 1, NO LINE")

    print()
    print("5. What D-206 says does not count, does not")
    for what, where in (
            ("a bare #N", u"on #1262: *\"yes\"*"),
            ("a commit sha", u"in commit 103f08a4c0ffee: *\"yes\"*"),
            ("another repository's issue", u"on https://github.com/someone/else/issues/12"),
            ("a session URL with no id", u"in https://claude.ai/code/session_"),
            ("a chat that is not a Claude Code session", u"in https://claude.ai/chat/0123456789abcdef")):
        code, _ = run(tool, log(accepted("D-207", where)))
        check(code == 1, "%s fails" % what)

    print()
    print("6. A link in another field is not the Accepted line's")
    code, out = run(tool, log({"D-207": record(
        "D-207", u"**Status:** Accepted %s **Date:** x %s **Accepted:** Ajmal PS, in conversation"
                 % (DOT, DOT),
        u"**Answers:** [#1262](%s) %s **Affects:** [s](%s)" % (ISSUE, DOT, SESSION))}))
    check(code == 1 and "NO LINK  D-207" in out, "Answers and Affects links do not carry the yes")

    print()
    print("7. Proposed, Not accepted and Superseded owe nothing")
    for status in (u"Proposed", u"Not accepted - dropped", u"Superseded by D-209"):
        code, out = run(tool, log({"D-207": record(
            "D-207", u"**Status:** %s %s **Date:** x %s **Written by:** a cloud session, "
                     u"heard in its own chat: *\"yes\"*" % (status, DOT, DOT))}))
        check(code == 0 and "skipped  D-207 is %s" % status.split(" ")[0] in out,
              "%s: exit 0, printed as skipped" % status)

    print()
    print("8. D-00 to D-205 are grandfathered by number")
    records = {}
    records.update(accepted("D-191", u"in conversation: *\"yes to all four\"*"))
    records.update(accepted("D-205", u"in conversation: *\"yes\"*"))
    records.update(accepted("D-206", u"in the session [s](%s)" % SESSION))
    root = log(records)
    code, out = run(tool, root)
    check(code == 0 and "2 before D-206, grandfathered" in out,
          "D-191 and D-205, unlinked, do not fail the run")
    check("D-191" not in out, "and are not listed without --all")
    code, out = run(tool, root, every=True)
    check(code == 0, "--all still exits 0 on them")
    check("would fail  D-191 is Accepted" in out and "would fail  D-205" in out
          and "0 of 2 would pass, 2 would fail" in out,
          "and --all reports each, with a count")
    check(tool.FIRST == 206, "the first decision bound is D-206, the rule's own")

    print()
    print("9. A wrapped Accepted line is read whole, and the next field is not part of it")
    code, _ = run(tool, log({"D-207": record(
        "D-207", u"**Status:** Accepted %s **Date:** x %s **Accepted:** Ajmal PS, 2026-11-27, in" % (DOT, DOT),
        u"the session [s](%s), answering Q-1" % SESSION)}))
    check(code == 0, "a link on the continuation line counts")
    code, _ = run(tool, log({"D-207": record(
        "D-207", u"**Status:** Accepted %s **Accepted:** in conversation %s **Asked for by:** [s](%s)"
                 % (DOT, DOT, SESSION))}))
    check(code == 1, "a link in the field after it, on the same line, does not")

    print()
    print("10. A decision only in the index, not yet split, is read there")
    code, out = run(tool, log(entries=[entry(
        "D-207", u"**Status:** Accepted %s **Accepted:** in conversation" % DOT)]))
    check(code == 1 and "NO LINK  D-207" in out, "an index entry with no link fails")
    code, _ = run(tool, log(entries=[entry(
        "D-207", u"**Status:** Accepted %s **Accepted:** on [#1262](%s)" % (DOT, ISSUE))]))
    check(code == 0, "and one with a link passes")

    print()
    print("11. With no decision anywhere it could not look")
    code, out = run(tool, log())
    check(code == 2 and "could not run" in out, "exit 2, never a pass")

    print()
    print("12. The real repository passes, and D-206 is among what it checked")
    code, out = run(tool, ROOT)
    check(code == 0, "exit 0 on this checkout")
    check("ok       D-206 - a Claude Code session: https://claude.ai/code/session_" in out,
          "D-206 is checked and passes on its own Accepted line's link")

    print()
    print("13. CI's gates job runs it")
    try:
        workflow = io.open(os.path.join(ROOT, ".github", "workflows", "gates.yml"),
                           encoding="utf-8").read()
    except (IOError, OSError):
        workflow = ""
    job = workflow.split("\n  gates:", 1)[-1].split("\n  compile:", 1)[0]
    check("python tools/check-decision-sources.py" in job,
          "gates.yml's job The gates that must pass runs it")

    for root in MADE:
        shutil.rmtree(root, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    an Accepted decision from D-206 on links where its yes can be read, or fails")
    return 0


if __name__ == "__main__":
    sys.exit(main())
