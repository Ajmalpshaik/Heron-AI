# Needs checking — Group AI

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group AI - the development hooks, on the owner's PC ([PR #308](https://github.com/Ajmalpshaik/Heron-AI/pull/308))

**Tracked as GitHub issue [#972](https://github.com/Ajmalpshaik/Heron-AI/issues/972) since 2026-11-16** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**No Revit is needed for any of these** - a Windows session with Git Bash, opened in the Heron folder. The
hooks are wired from [`.claude/settings.json`](../../.claude/settings.json) and are for developing Heron only.
They were proved on Linux, in the cloud session that built them; the Windows half is what is owed.
[`heron-session`](../../.claude/skills/heron-session/SKILL.md) and
[`heron-guard`](../../.claude/skills/heron-guard/SKILL.md) say what each one does.

*Placed 2026-09-23 by earlier-brain package C1. AI2 and AI3 guard against the defects in FRAGMENT-ISSUES
rows 5b-164 and 5b-165.*

| # | Check | Expected |
|---|---|---|
| ~~**AI1**~~ | ~~Open Claude Code in the Heron folder: a new session~~ | **PASSED 2026-09-23 on the owner's PC** - Windows 11 Pro, build 10.0.26200, Git Bash `MINGW64`, in a session the desktop app opened in its own worktree of the Heron folder. Before anything else it was told *"Heron: branch claude/heron-ai-qa-checks-45ea1c is level with origin/main (as last fetched). Fragments: 323 PROVEN, 73 DRAFT of 396."* The grep printed `73 heron-status: DRAFT` and `323 heron-status: PROVEN`, and `ls` found 396 `fragment.yaml`: all three numbers agree. *Level with* is the line's own wording for 0 behind and 0 ahead (`session_line.py`). `python --version` in Git Bash said `Python 3.11.9`. *It was to look like:* Before anything else the session is told one line - *"Heron: branch ... is N behind and M ahead of origin/main (as last fetched). Fragments: P PROVEN, D DRAFT of T."* - and its counts agree with `grep -h '^heron-status:' brain/fragments/*/fragment.yaml \| sort \| uniq -c`. No line at all means the hook did not run: try `python --version` in Git Bash first |
| ~~**AI2**~~ | In a fresh session, without loading any skill, ask for a new file under `brain/` holding the Revit… — [full row](../needs-checking-archive/group-ai.md#row-ai2) | **PASSED 2026-09-23 on the owner's PC** |
| ~~**AI3**~~ | In the same session, ask for a short note carrying Arabic text in a new file under `docs/`, then… — [full row](../needs-checking-archive/group-ai.md#row-ai3) | **PASSED 2026-09-23 on the owner's PC** |
| **AI4** | After a day of ordinary work, run `python tools/hook-report.py` | **NOT RUN - WAITING, 2026-09-23 on the owner's PC: the diary is an hour old, not a day.** Run early to see what it holds, it printed *"Hook log: C:\Users\…\AppData\Local\Heron\logs\heron-hooks.jsonl"*, *"found through Heron's log folder"* - the first half of the pass, met - and *"7 decision(s) from 2 hook(s) in 2 session(s); 0 unreadable line(s)"*, with `heron-guard` at *"4 decision(s) in 2 session(s)"*: a decision in both sessions it records. Its first line is 16:02Z that day, so it cannot speak for a day yet - run it again after one. The third hook, `main_moved.py`, is missing by design: it writes a line only for a merge or a "ready" (`main()` in that file). *It was to look like:* It names the diary in Heron's log folder - the one the add-in writes its own log to - and `heron-guard` shows decisions in every session of that day. A hook that never appears is not running |

---
