---
name: heron-guard
description: Refuses, at the moment it is proposed, an edit that would put the Revit vendor namespace outside revit/ - and, since D-166, a write or shell command that would turn on Changes, Admin or Publish in heron.config or write Heron's registry policy. Wired from .claude/settings.json so it runs in every session opened in this folder. Deny-tier, fails closed, and disabled with HERON_GUARD=off. The boundary rule is for developing Heron and is not part of what a modeller installs; the switch rule guards any Claude Code session opened here, a modeller's included.
allowed-tools:
  - Read
---

# Heron Guard — the adapter boundary, before the edit lands

**Two rules, each checked at the moment it can still be cheap** - the adapter boundary, below, and since
2026-11-16 the owner's switches ([the second rule](#the-second-rule--the-owners-switches-d-166)).
[`tools/check-structure.py`](../../../tools/check-structure.py) already refuses the Revit vendor
namespace outside `revit/` (and `tools/`, whose scripts talk about the code rather than being it - both
let it through there) — [docs/16 §4](../../../docs/16-version-support-strategy.md), the boundary
that keeps the core testable without Revit. This asks the same question **when the edit is proposed**,
instead of when somebody remembers to run the sweep.

**Why that difference is the whole point.** A gate somebody has to remember is a gate that is skipped on
the day it matters. On 2026-09-09 the author of `brain/heron_context.py` broke that exact boundary
**while writing a comment explaining it** — quoting the namespace in order to say the file did not
contain it — and found out only because the sweep happened to be run afterwards.

## Where it is wired — `.claude/settings.json`, and not this file

**Corrected 2026-09-23.** This skill used to declare the hook in its own frontmatter, gstack's shape
([Q-49](../../../docs/OPEN-QUESTIONS.md)). **A hook declared there is registered only when the skill is
invoked**, so in every session that never loaded `heron-guard` the boundary was not guarded at all.
Proven 2026-09-22 by hand: the script refused a forbidden edit piped into it, and the same edit made
through the editor in a normal session went straight through.

So [`.claude/settings.json`](../../settings.json) wires it now - for `Write`, `Edit` and `MultiEdit`
until 2026-11-16, and since D-166 also for `Bash`, `PowerShell` and the MCP tools that write a file or
run a command (`write_file`, `edit_block`, `start_process`, `interact_with_process`), one anchored
pattern - as
`python` on `.claude/skills/heron-guard/bin/heron_guard.py` under `$CLAUDE_PROJECT_DIR` — or, when the
script is not there, under the repository the hook runs in
([`heron-session`](../heron-session/SKILL.md) says why) — every session, whether or not anybody reads
this page. **The frontmatter declares nothing, on purpose:** the host keeps a
skill's copy of a hook separate from the settings' copy, so declaring it in both would run it twice.
[`tests/test_heron_guard.py`](../../../tests/test_heron_guard.py) holds both halves, and runs the exact
command `settings.json` gives.

**Every decision is written down** — each allow, deny, crash and switched-off edit is one line in the
hooks' diary outside this repository ([`heron-session`](../heron-session/SKILL.md) says where, and why
it is never a typed path). `python tools/hook-report.py` then shows from evidence that the guard runs in
every session and how often it refuses. The line is written after the decision is printed, and a diary
that cannot be written is a missing line — never a refusal.

**A hook the host cancels at its timeout decides nothing, and the edit goes ahead** — the host's rule,
not this hook's, and one more reason `check-structure.py` still runs before every push.

## What it does not do

- **It does not replace the sweep.** It sees one file's proposed content. `check-structure.py` still
  checks the path-manager rule, the layering rules, every part's README, the PowerShell encoding trap,
  and every file that reached disk another way. Run it before you push, as before.
- **It does not reach a model, a fragment, or a user.** Hooks are the **host's** mechanism and
  [D-01](../../../docs/DECISIONS.md) gives the host orchestration. **This is for developing Heron. It is
  not part of what a modeller installs**, and it must never be mistaken for one.
- **It checks two things**, and no more. Not metadata headers, not links, not counts. A hook with false
  positives is a hook somebody turns off, and then the boundary is gone along with the noise. It said
  *"one thing"* until the second rule landed, 2026-11-16.

## The second rule — the owner's switches, D-166

[D-166](../../../docs/decisions/D-166.md) layer 1, accepted by the owner from proposal
[F45](../../../docs/proposals/f45.md). Changes, Admin and Publish are `write.enabled`, `admin.enabled` and
`publish.enabled` in `%APPDATA%\Heron\config\heron.config` - a file every program running as the
modeller can write, read fresh on every request. So the hook also refuses:

| Refused | Allowed |
|---|---|
| A write to a file named `heron.config`, **anywhere on disk**, that sets one of the three keys to a true word - `true`, `1`, `yes`, `on`, the words `HeronConfig.GetBool` reads | Reading it - `cat`, `grep`, `Get-Content`, the Read tool |
| An Edit that changes only the value, with the key in the text it replaces | Setting a switch to **false** - the safe side |
| A command that names `heron.config` and sets a key on, or names a key and the word *true* - a `sed` or a `-replace` that swaps false for true | `write.askBeforeKeeping`, `companion.enabled` and every other setting |
| A command that writes `SOFTWARE\Policies\Heron` - `reg add`, `Set-ItemProperty` and the like - and a `.reg` file naming that key | Reading the key; a document or test that names it, **written with the editor** - a shell heredoc carrying a `reg add` line for a document is refused like the command itself, as this session found on 2026-11-16 |

**The refusal sends the AI to the modeller** - *turn it on in Heron's Settings (Heron > AI Bridge >
Settings)* - and never names `HERON_GUARD=off`, because the reader of a refusal is the AI it refused. The
hatch is for the person, and is written here.

**It is a tripwire, not a lock.** It does not stop a script that writes the file another way (a `.py` or
`.ps1` written first, then run), a host that does not load this repository's settings - Claude Desktop,
Cowork, Claude Code opened in another folder - or an AI that edits `.claude/settings.json`. D-166 layer
2, the switches leaving `heron.config`, is the lock. Every refusal is a line in the hooks' diary, so
`python tools/hook-report.py` shows whether an AI ever tried.

**Seen on a real host, 2026-11-16**, in a cloud session on Linux: the host ran the hook on a real `Bash`
call and a real `Write`, each putting `write.enabled = true` into a scratch `heron.config`, refused both,
wrote nothing, and `hook-report.py` listed the two refusals. **Not yet seen on the owner's Windows PC.**
`tests/test_heron_guard.py` section 9 holds the rule, and holds its keys and true words to
`HeronPermissions.cs` and `heron_config.py`.

## How it behaves, and why each choice was made

Three of these come from `garrytan/gstack`'s `check-freeze.sh`, read at `c8f0c4e`
([33 §5.7](../../../docs/33-external-repository-research.md)). The mechanism is **re-authored, not
copied** ([D-25](../../../docs/DECISIONS.md)), and in **Python rather than bash** — Heron is developed on
Windows, where a bash hook simply would not run.

| | |
|---|---|
| **The decision is nested** under `hookSpecificOutput` | A `permissionDecision` at the top level is ignored — *"silently no-ops the block"*. A hook that looks like it works and refuses nothing |
| **A crash denies** | An unexpected exit with nothing on stdout is read as **permission**, so a failure prints a **deny** rather than dying quietly. An **allow prints nothing** — silence is what the host reads as *no objection*, which is what an allow wants. `tests/test_heron_guard.py` pins both halves |
| **Deny-tier, fails closed** | *"A boundary that fails open is not a boundary."* gstack's `careful` is ask-tier and fails the other way, deliberately |
| **The payload is read as UTF-8 bytes** | Because a crash refuses, anything that crashes the hook for a reason unrelated to the boundary refuses a good edit. On Windows a piped stdin is decoded in the ANSI code page, which has no character for five byte values UTF-8 uses constantly — **an edit carrying Arabic was refused for that**, found 2026-09-23. `tests/test_heron_guard.py` section 4a sends Arabic under that code page |
| **`HERON_GUARD=off` turns it off** | Not optional for a fail-closed hook. One bad edit away from a repository nobody can work in, and the person who needs the hatch is the one whose tooling is already broken |

**The pattern is a second copy of `check-structure.py`'s, and
[`tests/test_heron_guard.py`](../../../tests/test_heron_guard.py) asserts the two are identical.**
`check-structure.py` cannot be imported — it runs its whole sweep at import time — so the rule is
restated and a test holds the two together, which is the same answer `tests/test_fragment_imports.py`
already gives for the executor's import list.

**It builds the namespace from parts rather than writing it out**, because `check-structure.py` greps
file text and would otherwise fail this file for containing the string it exists to forbid.

## Trying it

```bash
echo '{"tool_input":{"file_path":"brain/x.py","content":"using Autodesk.Revit.DB;"}}' \
  | python .claude/skills/heron-guard/bin/heron_guard.py
```

A refusal prints a decision. An allowed edit prints nothing at all. The second rule is tried the same
way, with `"tool_name":"Bash"` and a `"command"` that appends a switch line to `heron.config` - from your
own terminal: in a Claude Code session opened here, the hook refuses the command that would pipe it.
