#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
"Has main moved?" - asked at the last moment it can still change the outcome:
just before a pull request is merged or marked ready.

A PreToolUse hook, wired from .claude/settings.json so it runs in every
session. When the tool call about to run would MERGE a pull request or MARK
ONE READY, it fetches origin/main - with a timeout - and tells the AI which
commits on main this branch does not have. Every other tool call it lets pass
without a word.

WHY
---
On 2026-09-22 a whole pull request duplicated one that had merged an hour
earlier. Nothing was wrong with either on its own; the second session simply
never learned that main had moved. The commit titles are the cheapest place to
find that out, and the moment before a merge is the last one where finding it
out still saves anything.

ADVICE, NEVER A GATE
--------------------
It never decides. The output carries no permissionDecision at all, so the host
runs its normal permission flow exactly as if this hook did not exist, and the
advice arrives as context the AI reads (and a message the person sees). A merge
that main has overtaken is sometimes still right - a one-line docs fix does
not care - and a person, not a hook, is the one who knows that.

So every failure is silence. A payload that will not parse, no git, a fetch
that times out or is refused: the tool call goes ahead untouched. When the
fetch fails the comparison is still made, against origin/main as last fetched,
and the advice says so.

WHAT COUNTS AS A MERGE OR A "READY"
-----------------------------------
  * Bash: `gh pr merge` and `gh pr ready` (not `gh pr ready --undo`, which
    turns a pull request BACK into a draft), and a REST or GraphQL merge or
    ready sent with `gh api` or `curl`. Only at a command position, so a
    `grep 'gh pr merge'` over a document is not mistaken for one.
  * A GitHub MCP tool: merge_pull_request, enable_pr_auto_merge (a merge that
    happens later), and update_pull_request when it sets draft to false.
    update_pull_request_branch is NOT one - it merges main INTO the branch,
    which is the cure rather than the risk. The matcher in settings.json
    anchors each name, and this file checks the name again, because a matcher
    is a regular expression that matches anywhere unless it is told otherwise.

A cloud session has no `gh`, and merges through the MCP tools, so a Bash-only
hook would never have fired where most of Heron's pull requests are written.

WHICH BRANCH
------------
The one checked out where the session is working - the payload's `cwd`. An MCP
merge names a pull request by number, not by branch, so the advice names the
branch it compared rather than claiming it is the pull request's. In a Heron
session they are the same branch.

THIS IS FOR DEVELOPING HERON. IT IS NOT PART OF WHAT A MODELLER INSTALLS.
"""

import json
import os
import re
import sys

HOOK = "heron-main-moved"

# The fetch is the one slow thing here. Long enough for a slow link, short
# enough that a hung one costs a pause rather than the session; the hook's own
# timeout in .claude/settings.json is set above it so the host never cancels
# the hook before this gives up on the fetch.
FETCH_SECONDS = 20
GIT_SECONDS = 5

# How many missing commits are named. More than this and the count says it -
# the first titles are what show whether one of them did this work.
SHOW = 10

# A command position: the start, or after a separator or an opening bracket.
_AT = r"(?:^|[;&|(\n`]|\$\()\s*(?:sudo\s+)?(?:env\s+)?(?:\w+=\S*\s+)*"
BASH_MERGE = re.compile(_AT + r"gh\s+pr\s+merge\b")
BASH_READY = re.compile(_AT + r"gh\s+pr\s+ready\b(?![^\n;&|]*--undo)")
BASH_API = re.compile(
    _AT + r"(?:gh\s+api|curl)\b[^\n;&|]*?"
    r"(pulls/\d+/merge\b|mergePullRequest|enablePullRequestAutoMerge"
    r"|markPullRequestReadyForReview)")

MCP_TOOL = re.compile(r"^mcp__.+__([a-z_]+)$")


def action_of(payload):
    """'merge', 'ready' or None - what the tool call is about to do."""
    tool = payload.get("tool_name")
    args = payload.get("tool_input")
    if not isinstance(tool, str) or not isinstance(args, dict):
        return None
    if tool == "Bash":
        command = args.get("command")
        if not isinstance(command, str):
            return None
        if BASH_MERGE.search(command):
            return "merge"
        if BASH_READY.search(command):
            return "ready"
        api = BASH_API.search(command)
        if api:
            return "ready" if "Ready" in api.group(1) else "merge"
        return None
    named = MCP_TOOL.match(tool)
    if not named:
        return None
    verb = named.group(1)
    if verb in ("merge_pull_request", "enable_pr_auto_merge"):
        return "merge"
    if verb == "update_pull_request":
        draft = args.get("draft")
        if draft is False or (isinstance(draft, str) and draft.lower() == "false"):
            return "ready"
    return None


def run(argv, where, seconds):
    """(exit code, stdout) - or (None, why) when it could not run or timed out."""
    import subprocess
    env = dict(os.environ)
    # Never wait on a person: no terminal prompt for a password, and no
    # credential-manager window on Windows. A fetch that needs either fails,
    # and the comparison falls back to what was last fetched.
    env["GIT_TERMINAL_PROMPT"] = "0"
    env["GCM_INTERACTIVE"] = "never"
    try:
        done = subprocess.run(argv, cwd=where, capture_output=True,
                              timeout=seconds, env=env)
    except subprocess.TimeoutExpired:
        return None, "timed out after %d s" % seconds
    except (OSError, subprocess.SubprocessError, ValueError) as exc:
        return None, "could not run (%s)" % type(exc).__name__
    return done.returncode, done.stdout.decode("utf-8", "replace").strip()


def git(args, where, seconds=GIT_SECONDS):
    """git's answer, or None."""
    code, out = run(["git"] + list(args), where, seconds)
    return out if code == 0 else None


def advice_for(payload, action):
    """(decision, text) - text is None when there is nothing to say."""
    where = payload.get("cwd")
    if not isinstance(where, str) or not os.path.isdir(where):
        where = os.environ.get("CLAUDE_PROJECT_DIR") or os.getcwd()
    top = git(["rev-parse", "--show-toplevel"], where)
    if not top:
        return "no-git", None

    # The refspec is spelled out: a clone made for one branch fetches `main`
    # into FETCH_HEAD only, and origin/main would never move.
    code, why = run(["git", "fetch", "--quiet", "--no-tags", "origin",
                     "+refs/heads/main:refs/remotes/origin/main"],
                    top, FETCH_SECONDS)
    fetched = code == 0
    if code not in (None, 0):
        why = "git fetch exited %d" % code

    count = git(["rev-list", "--count", "HEAD..origin/main"], top)
    if count is None:
        return "no-main", None
    try:
        missing = int(count)
    except ValueError:
        return "no-main", None
    if missing == 0:
        return "up-to-date", None

    branch = git(["rev-parse", "--abbrev-ref", "HEAD"], top) or "HEAD"
    if branch == "HEAD":
        branch = "a detached HEAD"
    titles = git(["log", "--format=%h %s", "-n", str(SHOW),
                  "HEAD..origin/main"], top) or ""
    lines = ["Heron, before this %s: origin/main has %d commit%s that %s does "
             "not have%s:"
             % ("merge" if action == "merge" else "pull request is marked ready",
                missing, "" if missing == 1 else "s", branch,
                "" if fetched else
                " (could not fetch just now - %s - so this is main as last "
                "fetched)" % why)]
    lines += ["  " + one for one in titles.splitlines() if one.strip()]
    if missing > SHOW:
        lines.append("  ... and %d more" % (missing - SHOW))
    lines.append("If one of them already did this pull request's work, stop and "
                 "say so. Otherwise bring main in and run the gates again before "
                 "going on. This is advice - nothing was blocked.")
    return "advised", "\n".join(lines)


def main():
    try:
        raw = sys.stdin.read()
        payload = json.loads(raw) if raw.strip() else {}
        if not isinstance(payload, dict):
            return 0
        action = action_of(payload)
        if action is None:
            # Not a merge and not a "ready": the overwhelming majority of
            # tool calls, and nothing to say about any of them.
            return 0
        _decision, text = advice_for(payload, action)
    except Exception:                               # noqa: BLE001 - advice only
        return 0
    if text:
        print(json.dumps({
            "hookSpecificOutput": {"hookEventName": "PreToolUse",
                                   "additionalContext": text},
            "systemMessage": text,
        }, ensure_ascii=True))
    return 0


if __name__ == "__main__":
    sys.exit(main())
