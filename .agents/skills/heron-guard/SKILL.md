---
name: heron-guard
description: Refuses an edit that would put the Revit vendor namespace outside revit/, at the moment the edit is proposed rather than when somebody remembers to run the gate. Deny-tier, fails closed, and disabled with HERON_GUARD=off. This is for developing Heron and is not part of what a modeller installs.
allowed-tools:
  - Read
hooks:
  PreToolUse:
    - matcher: "Write|Edit|MultiEdit"
      hooks:
        - type: command
          command: "python .Codex/skills/heron-guard/bin/heron_guard.py"
          statusMessage: "Checking the adapter boundary..."
---

# Heron Guard — the adapter boundary, before the edit lands

**One rule, checked at the moment it can still be cheap.**
[`tools/check-structure.py`](../../../tools/check-structure.py) already refuses the Revit vendor
namespace outside `revit/` — [docs/16 §4](../../../docs/16-version-support-strategy.md), the boundary
that keeps the core testable without Revit. This asks the same question **when the edit is proposed**,
instead of when somebody remembers to run the sweep.

**Why that difference is the whole point.** A gate somebody has to remember is a gate that is skipped on
the day it matters. On 2026-09-09 the author of `brain/heron_context.py` broke that exact boundary
**while writing a comment explaining it** — quoting the namespace in order to say the file did not
contain it — and found out only because the sweep happened to be run afterwards.

## What it does not do

- **It does not replace the sweep.** It sees one file's proposed content. `check-structure.py` still
  checks the path-manager rule, the layering rules, every part's README, the PowerShell encoding trap,
  and every file that reached disk another way. Run it before you push, as before.
- **It does not reach a model, a fragment, or a user.** Hooks are the **host's** mechanism and
  [D-01](../../../docs/DECISIONS.md) gives the host orchestration. **This is for developing Heron. It is
  not part of what a modeller installs**, and it must never be mistaken for one.
- **It checks one thing.** Not metadata headers, not links, not counts. A hook with false positives is a
  hook somebody turns off, and then the boundary is gone along with the noise.

## How it behaves, and why each choice was made

Three of these come from `garrytan/gstack`'s `check-freeze.sh`, read at `c8f0c4e`
([33 §5.7](../../../docs/33-external-repository-research.md)). The mechanism is **re-authored, not
copied** ([D-25](../../../docs/DECISIONS.md)), and in **Python rather than bash** — Heron is developed on
Windows, where a bash hook simply would not run.

| | |
|---|---|
| **The decision is nested** under `hookSpecificOutput` | A `permissionDecision` at the top level is ignored — *"silently no-ops the block"*. A hook that looks like it works and refuses nothing |
| **A crash denies** | An unexpected exit with nothing on stdout is read as **permission**. Every path out of the hook prints a decision, and a failure prints a deny |
| **Deny-tier, fails closed** | *"A boundary that fails open is not a boundary."* gstack's `careful` is ask-tier and fails the other way, deliberately |
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
  | python .Codex/skills/heron-guard/bin/heron_guard.py
```

A refusal prints a decision. An allowed edit prints nothing at all.
