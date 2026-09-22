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
     it is silent, exit 0, when there is nothing it can derive; and it is
     fast on this repository.
  1. "HAS MAIN MOVED?" fires on a merge and a "ready" - by gh, by gh api, and
     by the GitHub MCP tools - fetches origin/main, and names the commits the
     branch does not have. It stays silent for everything else, including
     `gh pr ready --undo`, update_pull_request_branch and a grep that merely
     mentions `gh pr merge`.
  2. IT NEVER BLOCKS. No output carries a permissionDecision, whatever
     happens - a bad payload, no git, a fetch that fails.
  3. A FETCH THAT CANNOT FINISH IS CUT OFF, and the comparison still happens
     against origin/main as last fetched, and says so.
  4. THE WIRING. .claude/settings.json runs the session line at every
     session start and the main-moved hook for the right tools, with the
     matcher anchored so update_pull_request_branch is not mistaken for
     update_pull_request, and the exact commands it gives run under bash.

Every repository here is a throwaway with its own local `origin`, so nothing
in this suite ever needs the network.

WHAT IT DOES NOT PROVE. That the host runs the hooks, or that Git Bash on
Windows expands the command the way bash does here. Both need a real session
on the owner's PC - the same limit test_heron_guard.py states.
"""

import glob
import io
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
import time

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SKILL = os.path.join(ROOT, ".claude", "skills", "heron-session")
BIN = os.path.join(SKILL, "bin")
MOVED = os.path.join(BIN, "main_moved.py")
LINE = os.path.join(BIN, "session_line.py")
SETTINGS = os.path.join(ROOT, ".claude", "settings.json")

sys.path.insert(0, BIN)

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except AttributeError:
    pass

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def clean_env(**extra):
    """This process's environment without the variables that steer a hook."""
    env = dict(os.environ)
    for name in ("LOCALAPPDATA", "HERON_KNOWLEDGE", "CLAUDE_PROJECT_DIR"):
        env.pop(name, None)
    env.update(extra)
    return env


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
    print("PASSED - the session line and the main-moved advice say what is")
    print("true, and neither ever blocks.")
    print()
    print("It does not prove the host runs it, or that Git Bash on Windows")
    print("expands the command the same way. That needs the owner's PC.")
    return 0


def run_all(home):
    origin, seed, work = world(home)
    env = clean_env()
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

    proven = 0
    for card in glob.glob(os.path.join(ROOT, "brain", "fragments", "*",
                                       "fragment.yaml")):
        with io.open(card, encoding="utf-8") as handle:
            if re.search(r"^heron-status:\s*PROVEN\b", handle.read(), re.M):
                proven += 1
    t0 = time.time()
    parsed, raw, code = hook(LINE, {"cwd": ROOT, "session_id": "s-real"}, env)
    took = time.time() - t0
    check("%d PROVEN" % proven in context_of(parsed),
          "on this repository it states the PROVEN count grep derives (%d)"
          % proven)
    check(took < 5.0,
          "and it is fast: %.2f s on this repository, well inside the "
          "settings' timeout" % took)

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
        parsed, raw, code = hook(MOVED, payload, env)
        check(code == 0 and raw == "", "%s: silent" % label)

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
    parsed, raw, code = hook(MOVED, merge, env)
    check(code == 0 and raw == "",
          "once main is merged in there is nothing to say, and it says nothing")

    print()
    print("3. The wiring - .claude/settings.json runs both, for the right calls")
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


if __name__ == "__main__":
    sys.exit(main())
