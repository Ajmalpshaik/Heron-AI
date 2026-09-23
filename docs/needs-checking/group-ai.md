# Needs checking — Group AI

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group AI - the development hooks, on the owner's PC ([PR #308](https://github.com/Ajmalpshaik/Heron-AI/pull/308))

**No Revit is needed for any of these** - a Windows session with Git Bash, opened in the Heron folder. The
hooks are wired from [`.claude/settings.json`](../../.claude/settings.json) and are for developing Heron only.
They were proved on Linux, in the cloud session that built them; the Windows half is what is owed.
[`heron-session`](../../.claude/skills/heron-session/SKILL.md) and
[`heron-guard`](../../.claude/skills/heron-guard/SKILL.md) say what each one does.

*Placed 2026-09-23 by earlier-brain package C1. AI2 and AI3 guard against the defects in FRAGMENT-ISSUES
rows 5b-164 and 5b-165.*

| # | Check | Expected |
|---|---|---|
| **AI1** | Open Claude Code in the Heron folder: a new session | Before anything else the session is told one line - *"Heron: branch ... is N behind and M ahead of origin/main (as last fetched). Fragments: P PROVEN, D DRAFT of T."* - and its counts agree with `grep -h '^heron-status:' brain/fragments/*/fragment.yaml \| sort \| uniq -c`. No line at all means the hook did not run: try `python --version` in Git Bash first |
| **AI2** | In a fresh session, without loading any skill, ask for a new file under `brain/` holding the Revit vendor namespace in a comment | Refused before the file is written, with the guard's reason. Written means the guard is not running in every session - row 5b-164 again |
| **AI3** | In the same session, ask for a short note carrying Arabic text in a new file under `docs/`, then delete it | Written, with no refusal. A refusal naming `UnicodeDecodeError` means the Windows code-page trap is back - row 5b-165 |
| **AI4** | After a day of ordinary work, run `python tools/hook-report.py` | It names the diary in Heron's log folder - the one the add-in writes its own log to - and `heron-guard` shows decisions in every session of that day. A hook that never appears is not running |

---
