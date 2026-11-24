# Heron AI — Session Handover

> **One page, on purpose.** This is where a new session starts. It says where to look, not what the
> numbers are: every number is derived by a command, because a typed one goes stale. Where this page
> disagrees with the [Constitution](../HERON_CONSTITUTION.md), the [Golden Rules](14-golden-rules.md),
> [DECISIONS.md](DECISIONS.md) or a register, **those win.**
>
> Cut to one page on 2026-11-24 at the owner's request. The long version - the state table, the old
> numbered manual (sections 1 to 10a) and every pointer it held - is kept word for word in
> [the archive](handover-archive/2026-11-24-the-long-handover-kept-whole-when-handover-was-cut-to-one-page.md).

## Starting a session

**Owner, on your PC:** say *"Read HANDOVER.md in Heron-AI and carry on."* Add which Revit and which
model is open, if one is. Scope is Heron-AI and AJ-Tools; `AJ-AI-Brain` is read-only reference.

**Only what waits on you:** [FOR-THE-OWNER.md](FOR-THE-OWNER.md), then `python tools/owner-queue.py`.

**An AI session changing Heron:** read [AGENTS.md](../AGENTS.md) first. It has the reading order and
the rules.

## Where it stands - ask the tools, not this page

| Question | Command or file |
|---|---|
| How many fragments, PROVEN and DRAFT | `grep -h '^heron-status:' brain/fragments/*/fragment.yaml \| sort \| uniq -c` |
| What is unfinished, and what only waits | `python tools/check-gaps.py` (read its buckets, not its exit code) |
| What needs Revit open, in order | `python tools/owner-queue.py` (the *REVIT OPEN* bucket) · [NEEDS-CHECKING](NEEDS-CHECKING.md) |
| Open defects | `python tools/open-defects.py` · [FRAGMENT-ISSUES](FRAGMENT-ISSUES.md) |
| Questions for the owner | [OPEN-QUESTIONS](OPEN-QUESTIONS.md) · `python tools/check-docs.py`, section 4 |
| A PROVEN card whose code moved | `python tools/check-signatures.py` |
| What merged lately | `git log --oneline --first-parent -30 origin/main` |
| What the last session did | the newest row of [the session archive](handover-archive/README.md) |
| What is proven, in plain words | [What is proven](guide/what-is-proven.md) |

**PASS, FAIL, NOT RUN and NEEDS REAL REVIT are four different things.** Never merge them.

## What to do next

1. **Owner's answers first.** Read any answer on an open owner issue and work it - each issue carries
   its own ready prompt.
2. **Revit open on the PC:** prove the DRAFT reads first, then the DRAFT writes, then rebuild and
   deploy the add-in for what is built-not-run. The order is `python tools/owner-queue.py`; the method
   is the [`fragment-proving`](../.claude/skills/fragment-proving/SKILL.md) skill.
3. **No Revit, no answer waiting:** take an open defect (`python tools/open-defects.py`), or a
   cloud-doable row from [the balance of work](work-notes/BALANCE-OF-WORK.md).

## Where the how-to lives

| Job | Read |
|---|---|
| Building a new fragment | the [`heron-fragment-author`](../.claude/agents/heron-fragment-author.md) agent · old [§9a recipe](handover-archive/2026-11-24-the-long-handover-kept-whole-when-handover-was-cut-to-one-page.md#9a-continuing-the-library-build--the-recipe-so-another-session-can-just-start) |
| Proving fragments on a real model | [`fragment-proving`](../.claude/skills/fragment-proving/SKILL.md) skill |
| Before a push or pull request | [`heron-ship`](../.claude/skills/heron-ship/SKILL.md) skill |
| Recording a defect or owed check | the [`heron-issue-recorder`](../.claude/agents/heron-issue-recorder.md) agent |
| Something looks wrong mid-job | old [§4a](handover-archive/2026-11-24-the-long-handover-kept-whole-when-handover-was-cut-to-one-page.md#4a-something-looks-wrong-mid-job--start-here) |
| Several sessions at once | [AGENTS.md](../AGENTS.md) (own worktree, explicit paths) · old [§9b](handover-archive/2026-11-24-the-long-handover-kept-whole-when-handover-was-cut-to-one-page.md#9b-three-sessions-at-once--the-protocol-that-stops-them-colliding) |
| A session with no Revit | old [§10a](handover-archive/2026-11-24-the-long-handover-kept-whole-when-handover-was-cut-to-one-page.md#10a-the-linux-balance-track--what-a-session-with-no-revit-does) |
| Turning on writes after a real run | old [§6 checklist](handover-archive/2026-11-24-the-long-handover-kept-whole-when-handover-was-cut-to-one-page.md#6-the-return-to-the-machine-checklist) · [D-19](decisions/D-19.md) |

## Writing a session note

**Never into this file.** One file per sitting in [`handover-archive/`](handover-archive/README.md),
plus one row in its index. Keep only what is NEW, a MISTAKE worth not repeating, and what is still
TO DO. Anything owed goes to its register and a GitHub issue ([D-187](DECISIONS.md)), not only into a
note.

Change this page only when where-to-look changes. Keep it to one page.
