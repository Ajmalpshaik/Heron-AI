# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The session hooks - told where the branch stands, and never blocked by it.

    python tests/test_heron_session.py

.claude/skills/heron-session/ holds the hooks that tell a Heron development
session where its branch stands. Each is tested the way tests/test_heron_guard.py
tests the guard: JSON piped into the script exactly as the host would pipe it,
and the answer read back off stdout.

WHAT THIS PROVES
  0. THE SESSION LINE says where the branch stands against origin/main and
     how many fragments are PROVEN and DRAFT, both DERIVED; it does NOT fetch;
     it is silent, exit 0, when there is nothing it can derive, and in a
     modeller's chat - the main checkout on branch main. On this repository
     it states the count grep derives, and fast, with that rule set aside;
     with the rule in place it keeps it, whichever checkout this is - CI
     checks a push to main out on branch main. On any system but Windows it
     ends by saying there is no Revit here and Heron serves only its library
     tools (D-132); asked as Windows, the line is exactly what it was.
  1. "HAS MAIN MOVED?" fires on a merge and a "ready" - by gh, by gh api, and
     by the GitHub MCP tools - fetches origin/main, and names the commits the
     branch does not have. It stays silent for everything else, including
     `gh pr ready --undo`, update_pull_request_branch and a grep that merely
     mentions `gh pr merge`.
  2. IT NEVER BLOCKS. No output carries a permissionDecision, whatever
     happens - a bad payload, no git, a fetch that fails.
  3. A FETCH THAT CANNOT FINISH IS CUT OFF, and the comparison still happens
     against origin/main as last fetched, and says so.
  4. THE DIARY. Each decision is one line in a log found through Heron's own
     path helpers - never inside this repository, bounded in size, and never
     a reason for a hook to fail when it cannot be written. A call the hook
     had nothing to decide about writes nothing.
  5. THE WIRING. .claude/settings.json runs the session line at every
     session start, the main-moved hook for the right tools, with the
     matcher anchored so update_pull_request_branch is not mistaken for
     update_pull_request, and the local-only note on every prompt - and the
     exact commands it gives run under bash, and still find their scripts
     when $CLAUDE_PROJECT_DIR names another folder - the real scripts, by
     what they answer. With no script to find at all, outside any git
     repository, the three advice hooks are silent and exit 0, and the
     guard still fails closed (OPEN-QUESTIONS Q-80).
  6. THE LOCAL-ONLY NOTE warns when a prompt is marked LOCAL and the session
     is in a cloud container - by the documented CLAUDE_CODE_REMOTE, by the
     two variables seen in one on 2026-10-17, or, failing both, by not being
     Windows. The same prompt on Windows, an ordinary Revit question in the
     cloud, a prompt that only QUOTES the marker, and a payload that will not
     parse are all silence and exit 0. It never blocks, and the prompt's
     words never reach the diary.

Every repository here is a throwaway with its own local `origin`, so nothing
in this suite ever needs the network.

WHAT IT DOES NOT PROVE. That the host runs the hooks, or that Git Bash on
Windows expands the command the way bash does here. Both need a real session
on the owner's PC - the same limit test_heron_guard.py states.
"""

import glob
import io
import json
import ntpath
import os
import re
import shutil
import site
import subprocess
import sys
import tempfile
import time

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SKILL = os.path.join(ROOT, ".claude", "skills", "heron-session")
BIN = os.path.join(SKILL, "bin")
MOVED = os.path.join(BIN, "main_moved.py")
LINE = os.path.join(BIN, "session_line.py")
DIARY = os.path.join(BIN, "hook_log.py")
LOCAL = os.path.join(BIN, "local_only.py")
SETTINGS = os.path.join(ROOT, ".claude", "settings.json")

sys.path.insert(0, BIN)
sys.path.insert(0, os.path.join(ROOT, "tests", "support"))
import scratch_audit                                            # noqa: E402

# This run's audit lines go to a scratch folder, never the owner's real
# trail (FRAGMENT-ISSUES row 5b-1070).
scratch_audit.plant()

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except AttributeError:
    pass

FAILURES = []


def outside(path, root, paths=os.path):
    """True when PATH is neither ROOT nor under it. Two Windows drives share
    no common path, and commonpath raises rather than answer - which is an
    answer: a folder on another drive is outside. hook_log._inside says the
    same thing the same way."""
    full, base = paths.realpath(path), paths.realpath(root)
    try:
        return paths.commonpath([full, base]) != base
    except ValueError:
        return True


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def clean_env(**extra):
    """This process's environment without the variables that steer a hook.

    APPDATA goes too: on Windows the knowledge folder was found through it, so
    leaving it would make "no folder at all" impossible to set up there. Since
    D-135 it is HERON_DATA's, or the Windows profile's - so HERON_DATA goes as
    well, and a case that wants no folder makes the profile unusable.

    PYTHON'S OWN USER FOLDER STAYS WHERE IT WAS. On Windows Python finds a
    `pip install --user` package through APPDATA as well, so a child started
    without it had no PyYAML on a PC that installed it that way - the
    knowledge folder "could not be asked (ModuleNotFoundError)" and section 4
    failed on the import, not on the hook (row 5b-386). PYTHONUSERBASE names
    that folder directly, and no Heron path is resolved through it.

    The three variables that say "cloud container" go too, because this suite
    runs in one as often as not, and local_only.py reads them.
    """
    env = dict(os.environ)
    for name in ("LOCALAPPDATA", "XDG_DATA_HOME", "APPDATA", "HERON_KNOWLEDGE",
                 "HERON_DATA", "CLAUDE_PROJECT_DIR", "CLAUDE_CODE_REMOTE",
                 "CLAUDE_CODE_REMOTE_SDK_URL", "CCR_AGENT_PROXY_ENABLED"):
        env.pop(name, None)
    env.setdefault("PYTHONUSERBASE", site.getuserbase())
    env.update(extra)
    return env


def local_env(folder, **extra):
    """clean_env with Heron's per-user local folder pointed at `folder`.

    .NET reads LOCALAPPDATA on Windows and XDG_DATA_HOME elsewhere, and the
    bridge client resolves Heron's log folder the same way, so both are set
    and the suite steers the diary on either system.
    """
    return clean_env(LOCALAPPDATA=folder, XDG_DATA_HOME=folder, **extra)


def hook(script, payload, env):
    """(parsed output or None, raw stdout, exit code) - run as the host would."""
    got = subprocess.run([sys.executable, script],
                         input=payload if isinstance(payload, str)
                         else json.dumps(payload),
                         capture_output=True, text=True, encoding="utf-8",
                         env=env, timeout=120)
    raw = got.stdout.strip()
    try:
        parsed = json.loads(raw) if raw else None
    except ValueError:
        parsed = "NOT JSON"
    return parsed, raw, got.returncode


def hook_utf8(script, payload, env):
    """hook(), with the payload sent as RAW UTF-8 bytes, the way the host sends
    it - json.dumps would escape every non-ASCII character and hide the case.
    """
    got = subprocess.run([sys.executable, script],
                         input=json.dumps(payload, ensure_ascii=False)
                         .encode("utf-8"),
                         capture_output=True, env=env, timeout=120)
    raw = got.stdout.decode("utf-8", "replace").strip()
    try:
        parsed = json.loads(raw) if raw else None
    except ValueError:
        parsed = "NOT JSON"
    return parsed, raw, got.returncode


def context_of(parsed):
    """What the hook told the AI, or ''."""
    if not isinstance(parsed, dict):
        return ""
    return (parsed.get("hookSpecificOutput") or {}).get("additionalContext", "")


def no_decision(parsed):
    """True when nothing in the output could block or allow a tool call."""
    if parsed is None:
        return True
    if not isinstance(parsed, dict):
        return False
    inner = parsed.get("hookSpecificOutput") or {}
    return ("permissionDecision" not in parsed
            and "permissionDecision" not in inner
            and "decision" not in parsed)


def git(where, *args):
    done = subprocess.run(["git"] + list(args), cwd=where, capture_output=True,
                          text=True, encoding="utf-8", timeout=60)
    if done.returncode != 0:
        raise RuntimeError("git %s failed: %s" % (" ".join(args), done.stderr))
    return done.stdout.strip()


def commit(where, message):
    name = "f%d.txt" % int(time.time() * 1000000)
    with io.open(os.path.join(where, name), "w", encoding="utf-8") as handle:
        handle.write(message + "\n")
    git(where, "add", name)
    git(where, "commit", "-q", "-m", message)


def configure(where):
    git(where, "config", "user.email", "test@example.invalid")
    git(where, "config", "user.name", "Heron test")
    git(where, "config", "commit.gpgsign", "false")


def load_settings():
    try:
        with io.open(SETTINGS, encoding="utf-8") as handle:
            return json.load(handle)
    except (IOError, OSError, ValueError):
        return {}


def commands(event):
    """[(matcher, hook entry)] for one event in .claude/settings.json."""
    found = []
    for group in ((load_settings().get("hooks") or {}).get(event) or []):
        for one in group.get("hooks") or []:
            found.append((group.get("matcher", ""), one))
    return found


def fragment(where, name, status):
    folder = os.path.join(where, "brain", "fragments", name)
    os.makedirs(folder)
    with io.open(os.path.join(folder, "fragment.yaml"), "w",
                 encoding="utf-8") as handle:
        handle.write("id: %s\nheron-status: %s\nrisk: READ\n" % (name, status))


def diary_lines(folder):
    """Every line in the diary kept in `folder`, parsed."""
    path = os.path.join(folder, "heron-hooks.jsonl")
    if not os.path.isfile(path):
        return []
    with io.open(path, encoding="utf-8") as handle:
        return [json.loads(line) for line in handle if line.strip()]


def diary_path(env):
    """(file, how) - hook_log.path() as a fresh process with `env` sees it."""
    got = subprocess.run(
        [sys.executable, "-c",
         "import json, sys; sys.path.insert(0, %r); import hook_log; "
         "print(json.dumps(hook_log.path()))" % BIN],
        capture_output=True, text=True, encoding="utf-8", env=env, timeout=60)
    try:
        return tuple(json.loads(got.stdout))
    except ValueError:
        return None, "hook_log.path() did not answer: %s" % got.stderr.strip()


def main_checkout_on_main(where):
    """True when `where` is the main checkout - .git a folder, not a linked
    worktree's file - with branch main out. Asked of git, not read the way
    session_line.py reads it, so the suite's answer does not share its code.
    """
    if not os.path.isdir(os.path.join(where, ".git")):
        return False
    done = subprocess.run(["git", "symbolic-ref", "-q", "HEAD"], cwd=where,
                          capture_output=True, text=True, encoding="utf-8",
                          timeout=60)
    return done.returncode == 0 and done.stdout.strip() == "refs/heads/main"


def rule_set_aside(home):
    """A script, written in `home`, that runs the session line as the host
    does with one thing changed: the modeller's-chat rule is switched off. So
    the count and the time are measured on this repository whatever it has
    checked out - CI checks a push to main out on branch main, where the line
    as it is says nothing (row 5b-279).
    """
    path = os.path.join(home, "rule_set_aside.py")
    with io.open(path, "w", encoding="utf-8") as handle:
        handle.write("import sys\n"
                     "sys.path.insert(0, %r)\n"
                     "import session_line\n"
                     "session_line.modeller_checkout = lambda where: False\n"
                     "sys.exit(session_line.main())\n" % BIN)
    return path


def world(home):
    """A throwaway origin, a seed that pushes to it, and a working clone."""
    origin = os.path.join(home, "origin.git")
    seed = os.path.join(home, "seed")
    work = os.path.join(home, "work")
    git(home, "init", "-q", "--bare", origin)
    git(origin, "symbolic-ref", "HEAD", "refs/heads/main")
    git(home, "clone", "-q", origin, seed)
    configure(seed)
    git(seed, "checkout", "-q", "-b", "main")
    commit(seed, "The first commit on main")
    git(seed, "push", "-q", "origin", "main")
    git(home, "clone", "-q", origin, work)
    configure(work)
    git(work, "checkout", "-q", "-b", "claude/some-work")
    fragment(work, "alpha", "PROVEN")
    fragment(work, "beta", "PROVEN")
    fragment(work, "gamma", "DRAFT")
    git(work, "add", "brain")
    commit(work, "Work on the branch")
    return origin, seed, work


def main():
    print("THE SESSION HOOKS")
    print("=" * 72)

    home = tempfile.mkdtemp(prefix="heron-session-")
    try:
        run_all(home)
    finally:
        shutil.rmtree(home, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1
    print("PASSED - the session line, the main-moved advice and the local-only")
    print("note say what is true, none ever blocks, and each decision is one")
    print("diary line outside this repository.")
    print()
    print("It does not prove the host runs it, or that Git Bash on Windows")
    print("expands the command the same way. That needs the owner's PC.")
    return 0


def run_all(home):
    origin, seed, work = world(home)
    # Heron's log folder is resolved from the per-user local folder, so a
    # throwaway one keeps every diary line this suite causes inside `home`.
    local = os.path.join(home, "local")
    os.makedirs(local)
    logs = os.path.join(local, "Heron", "logs")
    env = local_env(local)
    plain = os.path.join(home, "plain")
    os.makedirs(plain)

    print()
    print("0. The session line - derived, not fetched, silent when it cannot say")
    print("-" * 72)
    start = {"session_id": "s-1", "hook_event_name": "SessionStart",
             "source": "startup", "cwd": work}
    parsed, raw, code = hook(LINE, start, env)
    text = context_of(parsed)
    check(code == 0, "it exits 0")
    check(isinstance(parsed, dict) and (parsed.get("hookSpecificOutput") or {})
          .get("hookEventName") == "SessionStart",
          "the line is nested under hookSpecificOutput, naming SessionStart")
    check("claude/some-work" in text, "it names the branch: %s" % text)
    check("0 behind and 1 ahead of origin/main" in text,
          "and where it stands against origin/main, derived by git")
    check("(as last fetched)" in text,
          "and says the comparison is with origin/main AS LAST FETCHED")
    check("2 PROVEN, 1 DRAFT of 3" in text,
          "and the fragment counts, read from each fragment's own card")
    check(isinstance(parsed, dict) and parsed.get("systemMessage") == text,
          "the person is shown the same line the AI is given")
    check(no_decision(parsed), "and it decides nothing")
    check(raw.isascii() if hasattr(raw, "isascii") else True,
          "every character of the output is ASCII")

    # NOT WINDOWS. Off Windows the heron MCP server serves its library alone
    # and no revit_ tool (D-132; it exited there until 2026-10-17), so a
    # session started there has no Revit - and on
    # 2026-10-17 a task marked LOCAL SESSION was started in a cloud session
    # with nothing at the start saying so. The piped hook answers for the
    # system running this suite; line_for() is asked for the other one, so
    # both halves are proved on either machine.
    was = ("Heron: branch claude/some-work is 0 behind and 1 ahead of "
           "origin/main (as last fetched). Fragments: 2 PROVEN, 1 DRAFT of 3.")
    if os.name != "nt":
        check(text.startswith(was + " Not Windows:")
              and "no Revit" in text and "only its library tools" in text
              and "no revit_ tool" in text and "NEEDS REAL REVIT" in text,
              "on this machine, which is not Windows, the line ends by saying "
              "there is no Revit here and only the library tools are served: "
              "%s" % text)
    else:
        check(text == was,
              "on this machine, which is Windows, the line is exactly what "
              "it was: %s" % text)
    try:
        import session_line as LINE_MODULE
    except ImportError as exc:
        check(False, "session_line.py can be imported (%s)" % exc)
        LINE_MODULE = None
    if LINE_MODULE is not None:
        asks = LINE_MODULE.line_for.__code__.co_argcount >= 2
        check(asks, "line_for() can be asked about a system other than the "
                    "one it runs on")
        as_windows = LINE_MODULE.line_for(start, "nt") if asks else None
        as_linux = LINE_MODULE.line_for(start, "posix") if asks else None
        check(as_windows == was,
              "asked as Windows, the line is exactly what it was - no word "
              "about the machine: %s" % as_windows)
        check(bool(as_linux) and as_linux.startswith(was + " Not Windows:")
              and "no Revit" in as_linux,
              "asked as Linux or macOS, the same line ends with the sentence "
              "that stops a task needing Revit: %s" % as_linux)
        check(asks and LINE_MODULE.line_for(dict(start, cwd=plain),
                                            "posix") is None,
              "and the machine alone is not a Heron line: a folder with no "
              "git and no fragments still gets NOTHING")

    commit(seed, "Somebody else merged first (#901)")
    git(seed, "push", "-q", "origin", "main")
    parsed, raw, code = hook(LINE, start, env)
    check("0 behind" in context_of(parsed),
          "main moving on the far side is NOT seen - the line never fetches, "
          "which is why it says 'as last fetched'")

    parsed, raw, code = hook(LINE, dict(start, cwd=plain), env)
    check(code == 0 and raw == "",
          "in a folder with no git and no fragments it prints NOTHING and "
          "exits 0 - silent on failure")
    parsed, raw, code = hook(LINE, "this is not json", env)
    check(code == 0 and raw == "",
          "a payload that will not parse is silence too, not a traceback")

    # A MODELLER'S CHAT: the main checkout with branch main out. The same
    # commit, cards and git as the line above - only the branch's name differs
    # - so the silence can only come from the rule, not from a missing part.
    git(work, "checkout", "-q", "-B", "main", "claude/some-work")
    try:
        parsed, raw, code = hook(LINE, start, env)
        check(code == 0 and raw == "",
              "in the main checkout on branch main - a modeller's chat - it "
              "prints NOTHING, though git and the cards are both there")
        parsed, raw, code = hook(LINE, dict(start, cwd=os.path.join(work, "brain")), env)
        check(code == 0 and raw == "",
              "and nothing from a folder inside that checkout either")
    finally:
        git(work, "checkout", "-q", "claude/some-work")
    parsed, raw, code = hook(LINE, start, env)
    check("claude/some-work" in context_of(parsed),
          "back on a branch, the line is said again")

    # THIS REPOSITORY, in whatever checkout runs the suite. A pull request is
    # checked out as a detached merge and the line speaks; a push to main is
    # checked out on branch main, a modeller's chat by the rule above, and a
    # check that expected the line there failed on every push from #379 on
    # (row 5b-279). So the count and the time are measured with the rule set
    # aside, and the rule is held to whichever checkout this is.
    proven = 0
    for card in glob.glob(os.path.join(ROOT, "brain", "fragments", "*",
                                       "fragment.yaml")):
        with io.open(card, encoding="utf-8") as handle:
            if re.search(r"^heron-status:\s*PROVEN\b", handle.read(), re.M):
                proven += 1
    real = {"cwd": ROOT, "session_id": "s-real"}
    t0 = time.time()
    parsed, raw, code = hook(rule_set_aside(home), real, env)
    took = time.time() - t0
    check("Fragments: %d PROVEN" % proven in context_of(parsed),
          "on this repository, with the modeller's-chat rule set aside, it "
          "states the PROVEN count grep derives (%d)" % proven)
    check(took < 5.0,
          "and it is fast: %.2f s on this repository, well inside the "
          "settings' timeout" % took)
    parsed, raw, code = hook(LINE, real, env)
    if main_checkout_on_main(ROOT):
        check(code == 0 and raw == "",
              "with the rule in place it prints NOTHING here: this repository "
              "is the main checkout with branch main out, as CI checks out a "
              "push to main")
    else:
        check("Fragments: %d PROVEN" % proven in context_of(parsed),
              "with the rule in place it says the same count here: this "
              "repository is not the main checkout with branch main out")

    print()
    print("1. Has main moved? - fires on a merge or a 'ready', and only then")
    print("-" * 72)

    merge = {"session_id": "s-2", "hook_event_name": "PreToolUse",
             "tool_name": "Bash", "cwd": work,
             "tool_input": {"command": "gh pr merge 12 --squash"}}
    parsed, raw, code = hook(MOVED, merge, env)
    advice = context_of(parsed)
    check(code == 0, "it exits 0")
    check(isinstance(parsed, dict) and (parsed.get("hookSpecificOutput") or {})
          .get("hookEventName") == "PreToolUse",
          "the advice is nested under hookSpecificOutput, naming PreToolUse")
    check("Somebody else merged first (#901)" in advice,
          "it FETCHED, and names the commit on main the branch does not have")
    check("1 commit that claude/some-work does not have" in advice,
          "and says how many, and which branch it compared: %s"
          % advice.split("\n")[0])
    check("nothing was blocked" in advice,
          "and says in words that it blocked nothing")
    check(no_decision(parsed),
          "and its output carries NO permissionDecision - the host's own "
          "permission flow runs exactly as if the hook were not there")
    check(isinstance(parsed, dict) and parsed.get("systemMessage") == advice,
          "the person is shown the same advice the AI is given")
    check(raw.isascii() if hasattr(raw, "isascii") else True,
          "every character of the output is ASCII, so a Windows console "
          "redirected to a pipe cannot die on it")

    for label, payload in (
            ("gh pr ready", {"tool_name": "Bash",
                             "tool_input": {"command": "cd x && gh pr ready 12"}}),
            ("a REST merge through gh api",
             {"tool_name": "Bash",
              "tool_input": {"command": "gh api -X PUT repos/o/r/pulls/12/merge"}}),
            ("a GraphQL 'ready' through gh api",
             {"tool_name": "Bash",
              "tool_input": {"command": "gh api graphql -f query='mutation "
                                        "{ markPullRequestReadyForReview(input: "
                                        "{pullRequestId: \"x\"}) { clientMutationId } }'"}}),
            ("the MCP merge tool",
             {"tool_name": "mcp__github__merge_pull_request",
              "tool_input": {"owner": "o", "repo": "r", "pullNumber": 12}}),
            ("the MCP auto-merge tool",
             {"tool_name": "mcp__github__enable_pr_auto_merge",
              "tool_input": {"owner": "o", "repo": "r", "pullNumber": 12}}),
            ("update_pull_request with draft false",
             {"tool_name": "mcp__github__update_pull_request",
              "tool_input": {"owner": "o", "repo": "r", "pullNumber": 12,
                             "draft": False}})):
        payload = dict(payload, cwd=work, session_id="s-2")
        parsed, raw, code = hook(MOVED, payload, env)
        check(code == 0 and "Somebody else merged first" in context_of(parsed)
              and no_decision(parsed),
              "%s: advised, and not blocked" % label)

    for label, payload in (
            ("an ordinary command", {"tool_name": "Bash",
                                     "tool_input": {"command": "ls -la"}}),
            ("a grep that only MENTIONS gh pr merge",
             {"tool_name": "Bash",
              "tool_input": {"command": "grep -n 'gh pr merge' notes.md"}}),
            ("gh pr ready --undo, which makes a draft again",
             {"tool_name": "Bash",
              "tool_input": {"command": "gh pr ready 12 --undo"}}),
            ("update_pull_request with draft true",
             {"tool_name": "mcp__github__update_pull_request",
              "tool_input": {"pullNumber": 12, "draft": True}}),
            ("update_pull_request that does not touch draft",
             {"tool_name": "mcp__github__update_pull_request",
              "tool_input": {"pullNumber": 12, "title": "x"}}),
            ("update_pull_request_branch, which brings main IN - even "
             "carrying draft false, the NAME decides",
             {"tool_name": "mcp__github__update_pull_request_branch",
              "tool_input": {"pullNumber": 12, "draft": False}}),
            ("a Write", {"tool_name": "Write",
                         "tool_input": {"file_path": "x",
                                        "content": "gh pr merge"}})):
        payload = dict(payload, cwd=work, session_id="s-3")
        before = len(diary_lines(logs))
        parsed, raw, code = hook(MOVED, payload, env)
        check(code == 0 and raw == "" and len(diary_lines(logs)) == before,
              "%s: silent, and no diary line - there was nothing to decide"
              % label)

    # Windows decodes a piped stdin in the ANSI code page, which has no
    # character for five byte values UTF-8 uses - Arabic among them.
    # PYTHONIOENCODING stands in for Windows.
    parsed, raw, code = hook_utf8(MOVED, dict(merge, tool_input={
        "command": u"gh pr merge 12 --subject '\u0641\u064a'"}),
        dict(env, PYTHONIOENCODING="cp1252"))
    check(code == 0 and "#901" in context_of(parsed),
          "a merge whose command carries Arabic is still advised under the "
          "Windows code page, not lost to a decoding crash")

    parsed, raw, code = hook(MOVED, "this is not json", env)
    check(code == 0 and raw == "",
          "a payload that will not parse: silence and exit 0, never a block")
    parsed, raw, code = hook(MOVED, dict(merge, cwd=plain), env)
    check(code == 0 and raw == "",
          "a merge asked outside any git repository: silence and exit 0")

    print()
    print("2. A fetch that fails still compares, and one that hangs is cut off")
    print("-" * 72)
    commit(seed, "A second change on main (#902)")
    git(seed, "push", "-q", "origin", "main")
    git(work, "remote", "set-url", "origin", os.path.join(home, "no-such.git"))
    parsed, raw, code = hook(MOVED, merge, env)
    advice = context_of(parsed)
    check("could not fetch just now" in advice and "as last fetched" in advice,
          "a fetch that fails says so, and compares with main as last fetched")
    check("#901" in advice and "#902" not in advice and no_decision(parsed),
          "so it names what the last fetch brought and nothing newer - and "
          "still blocks nothing")
    git(work, "remote", "set-url", "origin", origin)

    try:
        import main_moved as MOVED_MODULE
    except ImportError as exc:
        check(False, "main_moved.py can be imported (%s)" % exc)
        MOVED_MODULE = None
    if MOVED_MODULE is not None:
        t0 = time.time()
        code, why = MOVED_MODULE.run(
            [sys.executable, "-c", "import time; time.sleep(30)"], home, 1)
        took = time.time() - t0
        check(code is None and "timed out" in (why or ""),
              "a command that does not finish is cut off at its timeout: %s"
              % why)
        check(took < 10.0, "and the hook moves on (%.1f s, not 30)" % took)
        limit = [one.get("timeout", 600) for _m, one in commands("PreToolUse")
                 if "main_moved.py" in one.get("command", "")]
        check(bool(limit) and MOVED_MODULE.FETCH_SECONDS < limit[0],
              "the fetch gives up (%d s) before the host would cancel the "
              "hook (%s s), so the hook always gets to say something"
              % (MOVED_MODULE.FETCH_SECONDS, limit[0] if limit else "?"))

    git(work, "fetch", "-q", "origin")
    git(work, "merge", "-q", "--no-edit", "origin/main")
    before = len(diary_lines(logs))
    parsed, raw, code = hook(MOVED, merge, env)
    check(code == 0 and raw == "",
          "once main is merged in there is nothing to say, and it says nothing")
    after = diary_lines(logs)
    check(len(after) == before + 1 and after[-1:] != []
          and after[-1].get("decision") == "up-to-date",
          "but the diary records that it checked: up-to-date")

    print()
    print("4. The diary - one line per decision, outside the repository")
    print("-" * 72)
    entries = diary_lines(logs)
    hooks = set(e.get("hook") for e in entries)
    check("heron-session-line" in hooks and "heron-main-moved" in hooks,
          "both hooks wrote to the diary in Heron's log folder (%d lines)"
          % len(entries))
    check(bool(entries) and all(
        set(e) >= set(["when", "hook", "decision", "said", "session"])
        for e in entries),
          "every line says when, which hook, what it decided and said, and "
          "in which session")
    check(any(e.get("decision") == "advised" and "#901" in e.get("said", "")
              and e.get("session") == "s-2" for e in entries),
          "an advised merge is recorded with what it said and its session")
    check(any(e.get("hook") == "heron-session-line"
              and e.get("decision") == "said" for e in entries)
          and any(e.get("hook") == "heron-session-line"
                  and e.get("decision") == "silent" for e in entries),
          "the session line records both what it said and when it was silent")
    check(outside(logs, ROOT), "and the folder is outside this repository: %s" % logs)
    # Row 5b-186: on the owner's PC the temp folder is on C: and the checkout
    # on D:, and commonpath RAISES for two drives - the suite died here with
    # 19 checks unreported. Asked of Windows paths on any machine:
    try:
        across = outside(r"C:\Users\me\AppData\Local\Temp\logs", r"D:\Heron-AI", ntpath)
    except ValueError as error:
        across = "raised %s" % error
    check(across is True, "a folder on another drive is outside it, and asking does not raise (%s)" % across)

    kb = os.path.join(home, "kb")
    os.makedirs(kb)
    inside = os.path.join(ROOT, "tests")
    target, how = diary_path(local_env(inside, HERON_KNOWLEDGE=kb))
    check(target == os.path.join(kb, "heron-hooks.jsonl"),
          "when Heron's log folder cannot be used, the knowledge folder is "
          "used instead - found through %s" % how)
    # A relative answer is refused too. The bridge client no longer gives one
    # on any system (FRAGMENT-ISSUES row 5b-168), so it is made here the only
    # way left: a home folder that is itself relative.
    target, how = diary_path(clean_env(HOME="not-absolute",
                                       USERPROFILE="not-absolute"))
    check(target is None and "relative" in (how or ""),
          "with neither, there is NO diary - and the reason names the "
          "relative path that would have landed in the working folder")
    target, how = diary_path(local_env(
        inside, HERON_KNOWLEDGE=os.path.join(ROOT, "docs")))
    check(target is None and "inside this repository" in (how or ""),
          "a folder INSIDE this repository is refused, whichever helper "
          "offered it")

    blocked = os.path.join(home, "a-file-not-a-folder")
    with io.open(blocked, "w", encoding="utf-8") as handle:
        handle.write("x")
    parsed, raw, code = hook(MOVED, dict(merge, tool_input={
        "command": "gh pr merge 13"}), local_env(blocked))
    check(code == 0 and no_decision(parsed),
          "a diary that cannot be written costs the hook nothing")

    try:
        import hook_log as LOG
    except ImportError as exc:
        check(False, "hook_log.py can be imported (%s)" % exc)
        LOG = None
    if LOG is not None:
        check(LOG.KEEP_BYTES <= 2000000,
              "the diary is bounded: %d bytes, then it becomes the one older "
              "copy" % LOG.KEEP_BYTES)
        rotate = os.path.join(home, "rotate")
        folder = os.path.join(rotate, "Heron", "logs")
        os.makedirs(folder)
        big = os.path.join(folder, "heron-hooks.jsonl")
        line = json.dumps({"when": "2026-01-01T00:00:00Z", "hook": "old",
                           "decision": "x", "said": "", "session": ""})
        with io.open(big, "w", encoding="utf-8") as handle:
            handle.write((line + "\n") * (LOG.KEEP_BYTES // len(line) + 2))
        hook(LINE, start, local_env(rotate))
        check(os.path.isfile(os.path.join(folder, "heron-hooks.1.jsonl"))
              and os.path.getsize(big) < 2000,
              "past the limit the file becomes the older copy and a new one "
              "starts")

    print()
    print("5. The wiring - .claude/settings.json runs both, for the right calls")
    print("-" * 72)
    starts = [one for _m, one in commands("SessionStart")
              if "session_line.py" in one.get("command", "")]
    check(len(starts) == 1 and "$CLAUDE_PROJECT_DIR" in starts[0].get(
        "command", ""),
          "SessionStart runs session_line.py, found from $CLAUDE_PROJECT_DIR")
    if starts and os.name != "nt" and shutil.which("bash"):
        got = subprocess.run(["bash", "-c", starts[0]["command"]],
                             input=json.dumps(start), capture_output=True,
                             text=True, encoding="utf-8",
                             env=dict(env, CLAUDE_PROJECT_DIR=ROOT), timeout=120)
        check(got.returncode == 0 and "claude/some-work" in got.stdout,
              "and the exact command it gives prints the line under bash")
    pre = [(m, one) for m, one in commands("PreToolUse")
           if "main_moved.py" in one.get("command", "")]
    check(len(pre) == 1, "PreToolUse runs main_moved.py exactly once")
    matcher, entry = pre[0] if pre else ("(none)", {})
    command = entry.get("command", "")
    check("$CLAUDE_PROJECT_DIR" in command,
          "as a command found from $CLAUDE_PROJECT_DIR: %s" % command)
    # The host tests a matcher like this with JavaScript's RegExp.test, which
    # is unanchored - Python's re.search is the same for this pattern. So each
    # name must carry its own anchors, or update_pull_request would also send
    # update_pull_request_branch here.
    for name in ("Bash", "mcp__github__merge_pull_request",
                 "mcp__github__update_pull_request",
                 "mcp__github__enable_pr_auto_merge"):
        check(re.search(matcher, name) is not None,
              "the matcher sends %s to the hook" % name)
    for name in ("mcp__github__update_pull_request_branch", "Write", "Edit",
                 "BashOutput", "mcp__github__create_pull_request"):
        check(re.search(matcher, name) is None,
              "and does NOT send %s" % name)
    if os.name != "nt" and shutil.which("bash") and command:
        got = subprocess.run(["bash", "-c", command],
                             input=json.dumps(dict(merge, tool_input={
                                 "command": "gh pr ready 12"})),
                             capture_output=True, text=True, encoding="utf-8",
                             env=dict(env, CLAUDE_PROJECT_DIR=ROOT), timeout=120)
        check(got.returncode == 0,
              "the exact command settings.json gives runs under bash and "
              "exits 0")
    else:
        print("  note  the settings command was not run: no bash here, or "
              "Windows - a real session on the PC proves that half")

    prompts = [(m, one) for m, one in commands("UserPromptSubmit")
               if "local_only.py" in one.get("command", "")]
    check(len(prompts) == 1, "UserPromptSubmit runs local_only.py exactly once")
    pmatcher, pentry = prompts[0] if prompts else ("", {})
    pcommand = pentry.get("command", "")
    # The script is run by `python`, as the others are; since 2026-10-19 the
    # command first looks for it, so `python` is no longer its first word.
    check(re.search(r'(^|; )python "', pcommand) is not None
          and "$CLAUDE_PROJECT_DIR" in pcommand,
          "as the same python command, found from $CLAUDE_PROJECT_DIR: %s"
          % pcommand)
    check(not pmatcher,
          "with no matcher - the host runs a UserPromptSubmit hook on every "
          "prompt and would ignore one")
    check(0 < (pentry.get("timeout") or 0) <= 15,
          "and a short timeout (%s s) - it runs before every prompt"
          % pentry.get("timeout"))
    if os.name != "nt" and shutil.which("bash") and pcommand:
        got = subprocess.run(["bash", "-c", pcommand],
                             input=json.dumps({"session_id": "s-5",
                                               "prompt": "LOCAL SESSION - prove D3"}),
                             capture_output=True, text=True, encoding="utf-8",
                             env=dict(env, CLAUDE_PROJECT_DIR=ROOT,
                                      CLAUDE_CODE_REMOTE="true"), timeout=120)
        check(got.returncode == 0 and "cloud container" in got.stdout,
              "and the exact command it gives warns under bash in a cloud "
              "container")
    else:
        print("  note  the prompt hook's command was not run: no bash here, "
              "or Windows - a real session on the PC proves that half")

    # A session started with no folder and then moved into Heron keeps the
    # first folder in $CLAUDE_PROJECT_DIR, while the host runs Heron's hooks
    # from Heron. python could not open the script there, exited 2, and 2 is
    # "block" - every prompt in that session was refused (2026-10-18). So each
    # command falls back to the repository the hook runs in.
    every = [one for event in ("SessionStart", "UserPromptSubmit", "PreToolUse")
             for _m, one in commands(event)]
    if os.name != "nt" and shutil.which("bash") and every:
        for one in every:
            wired = one.get("command", "")
            got = subprocess.run(["bash", "-c", wired],
                                 input=json.dumps({
                                     "session_id": "s-5w", "cwd": ROOT,
                                     "source": "startup", "prompt": "hello",
                                     "tool_name": "Bash",
                                     "tool_input": {"command": "ls"}}),
                                 capture_output=True, text=True,
                                 encoding="utf-8", cwd=ROOT,
                                 env=dict(env, CLAUDE_PROJECT_DIR=plain),
                                 timeout=120)
            check(got.returncode == 0 and "can't open file" not in got.stderr,
                  "with $CLAUDE_PROJECT_DIR naming another folder, the command "
                  "still finds its script and exits 0: %s"
                  % wired.rsplit("/", 1)[-1].rstrip('"'))

        # Exit 0 alone would pass for a command that found no script and gave
        # up quietly. So each fallback is asked something only ITS script
        # answers: the guard refuses the forbidden edit, and the prompt hook
        # warns on a LOCAL prompt in a cloud container.
        def run_wired(wired, payload, cwd, **extra):
            return subprocess.run(["bash", "-c", wired],
                                  input=json.dumps(payload),
                                  capture_output=True, text=True,
                                  encoding="utf-8", cwd=cwd,
                                  env=dict(env, CLAUDE_PROJECT_DIR=plain,
                                           **extra),
                                  timeout=120)

        guard = [one.get("command", "") for _m, one in commands("PreToolUse")
                 if "heron_guard.py" in one.get("command", "")]
        vendor = "Autodesk" + ".Revit"
        forbidden = {"session_id": "s-5g", "cwd": ROOT, "tool_name": "Write",
                     "tool_input": {"file_path": os.path.join(
                         ROOT, "brain", "x.py"),
                         "content": "using %s.DB;" % vendor}}
        got = run_wired(guard[0], forbidden, ROOT) if guard else None
        check(got is not None and '"deny"' in got.stdout,
              "and the guard found that way is the real one - it REFUSES the "
              "forbidden edit")
        got = run_wired(pcommand, {"session_id": "s-5p",
                                   "prompt": "LOCAL SESSION - prove D3"},
                        ROOT, CLAUDE_CODE_REMOTE="true")
        check(got.returncode == 0 and "cloud container" in got.stdout,
              "and the prompt hook found that way is the real one - it warns "
              "on a LOCAL prompt in a cloud container")

        # Outside any git repository the fallback has nothing to find. The
        # three advice hooks are never a gate, so they say nothing and exit 0
        # - where python would have exited 2 on a path that is not there, and
        # 2 is "block" to the host. GIT_CEILING_DIRECTORIES stops git looking
        # above `plain`, wherever the temporary folder is.
        nowhere = dict(GIT_CEILING_DIRECTORIES=os.path.dirname(plain))
        for one in every:
            wired = one.get("command", "")
            if "heron_guard.py" in wired:
                continue
            got = run_wired(wired, {"session_id": "s-5n", "cwd": plain,
                                    "source": "startup",
                                    "prompt": "LOCAL SESSION - prove D3",
                                    "tool_name": "Bash",
                                    "tool_input": {"command": "gh pr merge 1"}},
                            plain, CLAUDE_CODE_REMOTE="true", **nowhere)
            check(got.returncode == 0 and not got.stdout.strip()
                  and not got.stderr.strip(),
                  "outside any git repository, with no script to find, the "
                  "advice hook is silent and exits 0: %s"
                  % wired.rsplit("/", 1)[-1].rstrip('"'))
        # The guard is deny-tier, and is left failing closed there on
        # purpose: with no script to run, a refusal is the boundary holding.
        # Whether that should change is the owner's call - OPEN-QUESTIONS
        # Q-80. This pins today's answer, so a change to it is deliberate.
        got = run_wired(guard[0], forbidden, plain, **nowhere) if guard \
            else None
        check(got is not None and got.returncode != 0,
              "and the guard, in the same place, still does NOT exit 0 - it "
              "fails closed until the owner decides Q-80")
    else:
        print("  note  the commands were not run with another folder in "
              "$CLAUDE_PROJECT_DIR: no bash here, or Windows - a real session "
              "on the PC proves that half")

    print()
    print("6. The local-only note - a LOCAL prompt in a cloud container, and only then")
    print("-" * 72)
    # The line the owner asked for, word for word - typed here rather than
    # read from the script, so the suite does not share its answer.
    cloud_said = ("This prompt is marked LOCAL (it needs Revit on the owner's "
                  "PC) and this is a cloud container with no Revit. Say so in "
                  "one line and stop. Tell the owner to start it on the Revit PC.")
    # The prompt that was started in a cloud session on 2026-10-17, with a
    # line of Arabic so the Windows code page case below has a byte to die on.
    incident = (u"LOCAL SESSION — run this on the Windows PC with Revit "
                u"2024.\n\nProve NEEDS-CHECKING D3 against the open model. "
                u"في")
    cloud = local_env(local, CLAUDE_CODE_REMOTE="true")
    ask = {"session_id": "s-6", "hook_event_name": "UserPromptSubmit",
           "cwd": work, "prompt": incident}

    before = len(diary_lines(logs))
    t0 = time.time()
    parsed, raw, code = hook(LOCAL, ask, cloud)
    took = time.time() - t0
    check(code == 0, "it exits 0")
    check(isinstance(parsed, dict) and (parsed.get("hookSpecificOutput") or {})
          .get("hookEventName") == "UserPromptSubmit",
          "the note is nested under hookSpecificOutput, naming UserPromptSubmit")
    check(context_of(parsed) == cloud_said,
          "the 2026-10-17 prompt, in a cloud container (CLAUDE_CODE_REMOTE=true), "
          "gets exactly the one line: %s" % context_of(parsed))
    check(no_decision(parsed), "and it decides nothing - the prompt goes ahead")
    check(raw.isascii() if hasattr(raw, "isascii") else True,
          "every character of the output is ASCII")
    check(took < 5.0, "and it is fast: %.2f s, before every prompt" % took)
    after = diary_lines(logs)
    last = after[-1] if after else {}
    check(len(after) == before + 1 and last.get("hook") == "heron-local-only"
          and last.get("decision") == "warned-cloud"
          and last.get("session") == "s-6",
          "the diary records the warning and its session: %s" % last)
    check("NEEDS-CHECKING" not in json.dumps(last),
          "and never the prompt's own words")

    observed = local_env(local, CLAUDE_CODE_REMOTE_SDK_URL=
                         "https://example.invalid/v1/code/sessions/x",
                         CCR_AGENT_PROXY_ENABLED="1")
    parsed, raw, code = hook(LOCAL, ask, observed)
    check(code == 0 and context_of(parsed) == cloud_said,
          "with only the two variables seen in a cloud container on 2026-10-17 "
          "- no CLAUDE_CODE_REMOTE - it is the same line")

    for label, prompt in (
            ("a heading '## LOCAL SESSION'", "## LOCAL SESSION\n\nProve D3."),
            ("a prompt that starts 'Local:'", "Local: prove D3 on the open model"),
            ("a bold '**Local:**'", "**Local:** prove D3"),
            ("a line saying 'Run this on the Windows PC'",
             "Prove D3.\nRun this on the Windows PC with Revit 2024 open."),
            ("a line saying it must run on the Windows PC",
             "This must run on the Windows PC, with the model open.")):
        parsed, raw, code = hook(LOCAL, dict(ask, prompt=prompt), cloud)
        check(code == 0 and context_of(parsed) == cloud_said
              and no_decision(parsed),
              "%s: warned, and not blocked" % label)

    # The prompt that asked for this hook QUOTES the marker in the middle of a
    # sentence. A plain "contains" would have stopped the very task that built
    # the hook, so a marker counts only where a line begins.
    this_task = (u"## Cloud session — no Revit needed\n\n## Why\n"
                 u"On 2026-10-17 a suggested task that began \"LOCAL SESSION "
                 u"— run this on the Windows PC with Revit 2024\" was "
                 u"started as a cloud session.\n\n## Steps\n- **Local-only:** "
                 u"the prompt starts with or contains `LOCAL SESSION`, starts "
                 u"with `Local:`, or says it must run \"on the Windows PC\".\n")
    for label, prompt in (
            ("an ordinary Revit question", "Select all ducts in the active view"),
            ("a question naming Revit AND the Windows PC",
             "Why does Revit 2024 on the Windows PC not show the Heron ribbon?"),
            ("'local session' in ordinary prose",
             "In a local session, does the guard hook run?"),
            ("a sentence that only mentions a LOCAL SESSION task",
             "Why did the LOCAL SESSION task stop at its first step?"),
            ("the prompt that asked for this hook, which quotes the marker "
             "mid-sentence", this_task),
            ("a cloud prompt with a LOCAL SESSION section for later",
             "## Cloud part - no Revit needed\nWrite the fragment.\n\n"
             "## LOCAL SESSION (the owner, later)\nProve it on the model."),
            ("a cloud prompt whose later step says to run it on the Windows PC",
             "Write the fragment at DRAFT.\n\n- Run this on the Windows PC "
             "afterwards to prove it."),
            ("an empty prompt", "")):
        before = len(diary_lines(logs))
        parsed, raw, code = hook(LOCAL, dict(ask, prompt=prompt), cloud)
        check(code == 0 and raw == "" and len(diary_lines(logs)) == before,
              "%s, in a cloud container: silent, and no diary line" % label)

    # THE SAME PROMPT LOCALLY. Piped, the hook answers for the machine running
    # this suite; advice_for() is asked for the other one, so both halves are
    # proved on either machine - the way line_for() is asked in section 0.
    parsed, raw, code = hook(LOCAL, ask, env)
    if os.name == "nt":
        check(code == 0 and raw == "",
              "piped on this machine, which is Windows, with no cloud "
              "variable: silent")
    else:
        check(code == 0 and "not Windows" in context_of(parsed),
              "piped on this machine, which is not Windows, with no cloud "
              "variable: the fallback line: %s" % context_of(parsed))
    try:
        import local_only as LOCAL_MODULE
    except ImportError as exc:
        check(False, "local_only.py can be imported (%s)" % exc)
        LOCAL_MODULE = None
    advise = getattr(LOCAL_MODULE, "advice_for", None)
    asks = advise is not None and advise.__code__.co_argcount >= 3
    check(asks, "advice_for() can be asked about an environment and a system "
                "other than the ones it runs on")
    if asks:
        decision, said = advise(ask, {}, "nt")
        check(said is None and decision == "local-here",
              "the same prompt on Windows with no cloud variable - the owner's "
              "PC - is SILENT (%s)" % decision)
        decision, said = advise(ask, {"CLAUDE_CODE_REMOTE": "false"}, "nt")
        check(said is None,
              "CLAUDE_CODE_REMOTE set to anything but true is not a cloud")
        decision, said = advise(ask, {"CLAUDE_CODE_REMOTE": "true"}, "nt")
        check(said == cloud_said,
              "a cloud session on Windows is still not the owner's PC: warned")
        decision, said = advise(ask, {}, "posix")
        check(decision == "warned-not-windows" and bool(said)
              and "not Windows" in said and "no Revit" in said
              and "cloud" not in said,
              "with no cloud variable, not being Windows is the fallback - and "
              "the line says so rather than claim a cloud: %s" % said)

    for label, payload in (
            ("a payload that will not parse", "this is not json"),
            ("an empty stdin", ""),
            ("a JSON list", "[1, 2]"),
            ("a prompt that is not text",
             json.dumps(dict(ask, prompt=["LOCAL SESSION"])))):
        parsed, raw, code = hook(LOCAL, payload, cloud)
        check(code == 0 and raw == "", "%s: silence and exit 0" % label)

    parsed, raw, code = hook_utf8(LOCAL, ask,
                                  dict(cloud, PYTHONIOENCODING="cp1252"))
    check(code == 0 and context_of(parsed) == cloud_said,
          "the real prompt sent as raw UTF-8, Arabic and all, is still read "
          "under the Windows code page")


if __name__ == "__main__":
    sys.exit(main())
