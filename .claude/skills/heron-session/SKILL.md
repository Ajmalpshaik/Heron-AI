---
name: heron-session
description: What a Heron development session is told without asking - just before a pull request is merged or marked ready, which commits on origin/main the branch does not have. Advice only, never a gate. Use when a "main has moved" note needs explaining, when the hook seems silent or noisy, or when asked whether it runs at all. For developing Heron; not part of what a modeller installs.
allowed-tools:
  - Read
---

# Heron Session — told where you stand, without asking

**Wired from [`.claude/settings.json`](../../settings.json), so it runs in every session** — not from
this file's frontmatter, which the host registers only when the skill is invoked. That was the trap
[`heron-guard`](../heron-guard/SKILL.md) fell into, and the reason it moved too.

| | when | what it says | can it block? |
|---|---|---|---|
| `bin/main_moved.py` | before a pull request is **merged** or **marked ready** | the commits on origin/main this branch does not have, freshly fetched | **No** |

**Why.** On 2026-09-22 a whole pull request duplicated one that had merged an hour earlier. Neither was
wrong on its own; the second session never learned main had moved. The moment before a merge is the last
one where learning it still saves anything.

## "Has main moved?"

It fires on `gh pr merge`, `gh pr ready` (not `--undo`, which makes a draft again), a merge or ready sent
through `gh api` or `curl`, and the GitHub MCP tools `merge_pull_request`, `enable_pr_auto_merge` and
`update_pull_request` **with `draft: false`**. A cloud session has no `gh` and merges through the MCP
tools, so a Bash-only hook would never have fired where most of Heron's pull requests are written.
`update_pull_request_branch` is deliberately not on the list: it brings main **into** the branch, which
is the cure. The matcher in `settings.json` anchors each name, because the host reads a matcher like
that as a regular expression that matches anywhere; the script checks the name again.

It fetches `origin/main` — 20 seconds at most, never waiting on a password prompt — then names up to ten
commits the branch does not have. **Its output carries no `permissionDecision`**, so the host's normal
permission flow runs exactly as if the hook were not there: the advice arrives as context for the AI and
a message for the person. A failed fetch still compares, against origin/main as last fetched, and says
so. Main not moved: silence. Anything else it cannot do — no git, a payload it cannot read — silence too.

It compares **the branch checked out where the session works**, the payload's `cwd`. An MCP merge names
a pull request by number, not by branch, so the advice names the branch it compared rather than claiming
it is the pull request's.

## What this deliberately does not do

- **It never blocks.** A merge that main has overtaken is sometimes still right — a one-line docs fix
  does not care — and a person is the one who knows.
- **It does not replace the gates.** Being level with main says nothing about whether the change is
  correct; [`heron-ship`](../heron-ship/SKILL.md) still decides that.
- **It does not reach a model, a fragment or a modeller.** Hooks are the host's mechanism
  ([D-01](../../../docs/DECISIONS.md)). **This is for developing Heron and is not part of what a
  modeller installs.**

## On Windows

The command in `settings.json` is `python "$CLAUDE_PROJECT_DIR/..."`, which Git Bash expands the way
bash does on Linux — Claude Code runs hooks through Git Bash on Windows when it is installed. The hook
prints JSON with every non-ASCII character escaped, because a redirected Windows console encodes in the
ANSI code page and dies on a character it has no byte for ([`heron-ship`](../heron-ship/SKILL.md) §2).
**Proved on Linux only so far** — a real Windows session is the proof still owed.

## Trying it

```bash
echo '{"tool_name": "Bash", "tool_input": {"command": "gh pr merge 1"}, "cwd": "."}' \
  | python .claude/skills/heron-session/bin/main_moved.py
```

That fetches `origin/main` for real. [`tests/test_heron_session.py`](../../../tests/test_heron_session.py)
pipes JSON into the hook against a throwaway repository with its own `origin`, so the suite never needs
the network.
