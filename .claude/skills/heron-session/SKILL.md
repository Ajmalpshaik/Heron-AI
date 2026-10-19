---
name: heron-session
description: What a Heron development session is told without asking - at the start, where this branch stands against origin/main and how many fragments are PROVEN and DRAFT, and on any system but Windows that no Revit can be reached and Heron serves only its library tools; before a prompt marked LOCAL, when the session is a cloud container, one line telling the AI to say so and stop; just before a pull request is merged or marked ready, which commits on origin/main the branch does not have. Advice only, never a gate. Also where every Heron hook writes down what it decided, and how to read that with tools/hook-report.py. Use when the session-start line or a "main has moved" note needs explaining, when a hook seems silent or noisy, or when asked whether the hooks run at all. For developing Heron; not part of what a modeller installs.
allowed-tools:
  - Read
---

# Heron Session — told where you stand, without asking

**Three hooks and one diary, wired from [`.claude/settings.json`](../../settings.json) so they run in every session** —
not from this file's frontmatter, which the host registers only when the skill is invoked. That was the
trap [`heron-guard`](../heron-guard/SKILL.md) fell into, and the reason it moved too.

| | when | what it says | can it block? |
|---|---|---|---|
| `bin/session_line.py` | every session start | *"Heron: branch X is N behind and M ahead of origin/main (as last fetched). Fragments: P PROVEN, D DRAFT of T."* — and on any system but Windows, *"Not Windows: no Revit in this session - Heron serves only its library tools …"* | **No** |
| `bin/main_moved.py` | before a pull request is **merged** or **marked ready** | the commits on origin/main this branch does not have, freshly fetched | **No** |
| `bin/local_only.py` | before every prompt | only for a prompt **marked LOCAL** in a **cloud container**: *"This prompt is marked LOCAL (it needs Revit on the owner's PC) and this is a cloud container with no Revit. Say so in one line and stop. Tell the owner to start it on the Revit PC."* | **No** |
| `bin/hook_log.py` | after every decision any Heron hook makes | nothing — it writes one line to a log outside the repository | **No** |

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
- **Silent in a modeller's chat** — the main checkout with branch `main` out, where the owner's Revit
  chats run. It reads `.git/HEAD` to decide and runs **no** git there: the line helps a developer, cost
  0.9 s, and put repository words in front of a Revit question (measured 2026-10-01). A worktree or any
  other branch gets the line as before.
- **Says when the machine is not Windows.** On Linux or macOS — a cloud container above all — the line
  ends *"Not Windows: no Revit in this session - Heron serves only its library tools, no revit_ tool -
  anything marked LOCAL or NEEDS REAL REVIT cannot run here."* Off Windows the `heron` MCP server serves
  its library alone and no `revit_` tool ([D-132](../../../docs/DECISIONS.md); until 2026-10-17 it
  exited there), so such a task is stopped before its first step. On 2026-10-17 a LOCAL SESSION task (NEEDS-CHECKING D3) was started in a
  cloud session and spent its first steps finding that out. Derived from `os.name`, the same test the
  server makes, and said only on a line that already has something to say. **On Windows the line is
  unchanged.**

## A LOCAL prompt in a cloud container

**Why a third hook.** The session line says "Not Windows", but a SessionStart hook cannot see the prompt,
so on 2026-10-17 the LOCAL SESSION task still spent a whole turn finding out it could do nothing.
`bin/local_only.py` runs on **UserPromptSubmit**, the one event that sees both the prompt and the machine.
When both say "this cannot run here" it adds one line of context for the AI — say so in one line, stop,
and tell the owner to start it on the Revit PC. Otherwise silence.

- **Marked LOCAL, narrowly — and only in the prompt's first paragraph.** A line that *begins*
  `LOCAL SESSION` (capitals), a prompt that begins `Local:`, or a line that begins by saying to run it on
  the Windows PC (*"Run this on the Windows PC"*, *"This must run on the Windows PC"*). Markdown in front
  — `#`, `>`, `-`, `**` — is allowed. **A marker in the middle of a sentence is a mention, not a mark:**
  the prompt that asked for this hook quotes *"LOCAL SESSION — run this on the Windows PC"* mid-sentence,
  and a plain "contains" fires on it (checked 2026-10-17). **Further down is a hand-on, not a mark:** a
  cloud task's `## LOCAL SESSION (later)` section or `- Run this on the Windows PC afterwards` step must
  not stop that task. A mark missed costs less than one invented — the session-start line still says
  Not Windows. A prompt that only names Revit, or "local session" in lower case, never fires.
- **A cloud container, asked in order.** `CLAUDE_CODE_REMOTE` set to `true` — what Claude Code documents
  ("never `true` locally", [cloud environments](https://code.claude.com/docs/en/cloud-environments));
  then `CLAUDE_CODE_REMOTE_SDK_URL` and `CCR_AGENT_PROXY_ENABLED` both set, seen in a cloud container on
  2026-10-17 and documented nowhere; then, as the **fallback**, not Windows — the line then says *"this
  machine is not Windows, so it has no Revit"* rather than claim a cloud it cannot see. A cloud session
  on Windows still warns: LOCAL means the owner's PC.
- **Never a gate.** The output carries no `decision`, so the prompt reaches the AI as typed. Any error —
  a payload that will not parse, a prompt that is not text — is silence and exit 0.
- **The diary** gets `warned-cloud`, `warned-not-windows`, or `local-here` (marked, and this is the
  owner's PC). An unmarked prompt writes nothing, and **the prompt's own words are never written**.

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

## The diary, and why its folder is never a typed path

Every Heron hook — these three and [`heron-guard`](../heron-guard/SKILL.md) — appends one JSON line per
decision: which hook, what it decided, what it said, when, and in which session. A call a hook had
nothing to decide about writes nothing.

```bash
python .claude/skills/heron-session/bin/hook_log.py   # where the log is, or why there is none
python tools/hook-report.py                           # how often each hook decided, and what it said
```

**Where is asked, never typed** — only the path manager builds a Heron path
([`revit-addin-conventions`](../revit-addin-conventions/SKILL.md)), and Python cannot call it, so the
Python seams that already resolve Heron's folders are asked in turn:

1. **Heron's log folder** — the one the add-in writes its own daily log to, as
   `mcp/client/heron_bridge_client.LOG_DIR` resolves it. Derived, machine-local and disposable, which is
   what a hook's diary is. `%LOCALAPPDATA%\Heron\logs` on Windows; elsewhere
   `~/.local/share/Heron/logs`, the folder .NET names there (or `$XDG_DATA_HOME/Heron/logs`).
2. **The knowledge folder**, `heron_scope.knowledge_dir()`, only when the first cannot be used — when it
   would land inside this repository, or is not an absolute path. Until 2026-09-23 that was every Linux
   machine: the bridge client answered a *relative* path there
   ([FRAGMENT-ISSUES row 5b-168](../../../docs/FRAGMENT-ISSUES.md)), so a cloud session's diary went to
   `HERON_KNOWLEDGE` ([38](../../../docs/38-the-cloud-environment.md)).
3. **Neither: no diary, silently.** A hook never fails, and never refuses, because it could not write a
   line — and the report says which of the three happened.

A folder **inside** this repository is refused whichever seam offered it: a log committed by accident is
a log published. The file keeps about a megabyte, then becomes the one older copy and a new file starts;
the report reads both. **Tests point every hook they run at a throwaway folder**, so a suite's
deliberate refusals never land in the real diary.

**What the report is for:** a hook that only nags gets switched off, and a hook that never fires is not
there at all — and from inside one session nobody can tell which. `hook-report.py` counts decisions per
hook and per session, how often each hook said anything, and the things it said most, so either is
found from evidence. It is a report: exit 0 whatever it finds.

## What this deliberately does not do

- **No hook here blocks.** A merge that main has overtaken is sometimes still right — a one-line docs fix
  does not care — and a person is the one who knows. A LOCAL prompt in the cloud still reaches the AI;
  the line beside it is what stops the work.
- **It does not replace the gates.** Being level with main says nothing about whether the change is
  correct; [`heron-ship`](../heron-ship/SKILL.md) still decides that.
- **It does not reach a model, a fragment or a modeller.** Hooks are the host's mechanism
  ([D-01](../../../docs/DECISIONS.md)). **This is for developing Heron and is not part of what a
  modeller installs.**

## On Windows

The commands in `settings.json` run `python` on the script under `$CLAUDE_PROJECT_DIR`, which Git Bash
expands the way bash does on Linux — Claude Code runs hooks through Git Bash on Windows when it is
installed. **When the script is not there, the command takes the folder from
`git rev-parse --show-toplevel` instead** — the repository the hook runs in. On 2026-10-18 a session
started with no folder and then moved into Heron kept that first, empty folder in `$CLAUDE_PROJECT_DIR`
while the host ran Heron's hooks: python could not open the script, exited 2, and 2 is "block" to the
host, so every prompt in that session was refused. Piped by hand through Git Bash on Windows the same
day, with an empty folder in that variable, all four commands exit 0;
[`tests/test_heron_session.py`](../../../tests/test_heron_session.py) runs them that way under bash. Each hook
prints JSON with every non-ASCII character escaped, because a redirected Windows console encodes in the
ANSI code page and dies on a character it has no byte for ([`heron-ship`](../heron-ship/SKILL.md) §2).
**Proved on Linux only so far** — the first real Windows session start, showing the line, is the proof
still owed. That the line carries no "Not Windows" sentence there is proved on Linux too, by asking
`line_for()` as `"nt"`; the same Windows session start is its real proof. The same holds for the
LOCAL note: that a LOCAL prompt on the owner's PC is silent is proved by asking `advice_for()` as `"nt"`
with no cloud variable, and a real prompt there is its proof still owed.

## Trying them

```bash
echo '{"cwd": "."}' | python .claude/skills/heron-session/bin/session_line.py
echo '{"tool_name": "Bash", "tool_input": {"command": "gh pr merge 1"}, "cwd": "."}' \
  | python .claude/skills/heron-session/bin/main_moved.py
echo '{"prompt": "LOCAL SESSION - prove D3"}' \
  | CLAUDE_CODE_REMOTE=true python .claude/skills/heron-session/bin/local_only.py
```

The first prints nothing in the main checkout with `main` out — the rule above — so try it from a
worktree or another branch; the suite checks it on this repository with that rule set aside, and holds
the rule to whatever checkout runs it ([row 5b-279](../../../docs/fragment-issues/section-5b-rows-276-300.md)).
The second fetches `origin/main` for real. The third prints the cloud line, and nothing for a prompt
not marked LOCAL. [`tests/test_heron_session.py`](../../../tests/test_heron_session.py)
pipes JSON into all three against a throwaway repository with its own `origin`, so the suite never needs
the network; [`tests/test_hook_report.py`](../../../tests/test_hook_report.py) holds the report to a diary
written with known contents.
