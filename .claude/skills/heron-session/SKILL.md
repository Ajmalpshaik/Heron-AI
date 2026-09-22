---
name: heron-session
description: What a Heron development session is told without asking - at the start, where this branch stands against origin/main and how many fragments are PROVEN and DRAFT; just before a pull request is merged or marked ready, which commits on origin/main the branch does not have. Advice only, never a gate. Use when the session-start line or a "main has moved" note needs explaining, when a hook seems silent or noisy, or when asked whether the hooks run at all. For developing Heron; not part of what a modeller installs.
allowed-tools:
  - Read
---

# Heron Session — told where you stand, without asking

**Two hooks, wired from [`.claude/settings.json`](../../settings.json) so they run in every session** —
not from this file's frontmatter, which the host registers only when the skill is invoked. That was the
trap [`heron-guard`](../heron-guard/SKILL.md) fell into, and the reason it moved too.

| | when | what it says | can it block? |
|---|---|---|---|
| `bin/session_line.py` | every session start | *"Heron: branch X is N behind and M ahead of origin/main (as last fetched). Fragments: P PROVEN, D DRAFT of T."* | **No** |
| `bin/main_moved.py` | before a pull request is **merged** or **marked ready** | the commits on origin/main this branch does not have, freshly fetched | **No** |

**Why these two.** On 2026-09-22 a whole pull request duplicated one that had merged an hour earlier.
Neither was wrong on its own; the second session never learned main had moved. The start of a session
and the moment before a merge are the two places that knowledge is cheapest and still changes something.

## The session line

- **Derived, never typed.** Behind and ahead from `git rev-list`; the fragment counts from each
  `brain/fragments/*/fragment.yaml`'s own `heron-status:` line — the count
  [AGENTS.md](../../../AGENTS.md) gives as a command.
- **It does not fetch.** A session start is not the moment to wait on a network, so it compares with
  origin/main **as last fetched** and says so. The moment that must be current is the merge, and the
  other hook fetches then.
- **Silent on failure.** A part that cannot be derived is left out; if nothing can be, nothing is
  printed and the exit is 0. A status line that errors at the start of every session is one somebody
  switches off.
- **Fast** — two local git questions and a few hundred small file reads; the suite times it on this
  repository.

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

- **Neither hook blocks.** A merge that main has overtaken is sometimes still right — a one-line docs fix
  does not care — and a person is the one who knows.
- **It does not replace the gates.** Being level with main says nothing about whether the change is
  correct; [`heron-ship`](../heron-ship/SKILL.md) still decides that.
- **It does not reach a model, a fragment or a modeller.** Hooks are the host's mechanism
  ([D-01](../../../docs/DECISIONS.md)). **This is for developing Heron and is not part of what a
  modeller installs.**

## On Windows

The commands in `settings.json` are `python "$CLAUDE_PROJECT_DIR/..."`, which Git Bash expands the way
bash does on Linux — Claude Code runs hooks through Git Bash on Windows when it is installed. Each hook
prints JSON with every non-ASCII character escaped, because a redirected Windows console encodes in the
ANSI code page and dies on a character it has no byte for ([`heron-ship`](../heron-ship/SKILL.md) §2).
**Proved on Linux only so far** — the first real Windows session start, showing the line, is the proof
still owed.

## Trying them

```bash
echo '{"cwd": "."}' | python .claude/skills/heron-session/bin/session_line.py
echo '{"tool_name": "Bash", "tool_input": {"command": "gh pr merge 1"}, "cwd": "."}' \
  | python .claude/skills/heron-session/bin/main_moved.py
```

The second fetches `origin/main` for real. [`tests/test_heron_session.py`](../../../tests/test_heron_session.py)
pipes JSON into both against a throwaway repository with its own `origin`, so the suite never needs the
network.
