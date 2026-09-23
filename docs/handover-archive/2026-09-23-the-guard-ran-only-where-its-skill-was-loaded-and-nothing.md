# Session note — THE GUARD RAN ONLY WHERE ITS SKILL WAS LOADED, AND NOTHING SAID WHEN MAIN HAD MOVED

> **Archived session note** from 2026-09-23. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-23 — THE GUARD RAN ONLY WHERE ITS SKILL WAS LOADED, AND NOTHING SAID WHEN MAIN HAD MOVED

A cloud session, no Revit: **earlier-brain package C1**, merged as
[PR #308](https://github.com/Ajmalpshaik/Heron-AI/pull/308) on the owner's word the same day. Everything
here is for developing Heron; none of it reaches a model or a modeller.

- **New:** `.claude/settings.json` now runs three hooks in every session. `heron-guard` moved there from
  its skill's frontmatter, where it had run only in sessions that loaded the skill (row 5b-158). The new
  [`heron-session`](../../.claude/skills/heron-session/SKILL.md) skill adds one line at session start - where
  the branch stands against `origin/main`, and the PROVEN and DRAFT counts - and, before a pull request is
  merged or marked ready, the commits on main the branch does not have. Advice only. Every hook writes one
  line per decision to a diary outside the repository; `python tools/hook-report.py` reads it.
- **New:** `check-docs.py` names every file in `tools/` and every skill folder, reads every tracked file for
  control characters, double-encoded text and conflict markers, and derives the MCP tool total. It runs
  faster than before - the history-word test is asked once per line now - with byte-identical output.
- **Mistake worth not repeating:** a test that pipes JSON into a hook through `json.dumps` escapes every
  non-ASCII character, so it cannot see what a real host sends. That hid the Windows code-page crash
  (row 5b-159) until the payload was sent as raw UTF-8.
- **To do:** NEEDS-CHECKING **Group AH** - four checks on the Windows PC, no Revit. Rows 5b-161 (the cloud
  setup script installed nothing, so the `heron` MCP server could not start), 5b-162 and 5b-163 are open
  and belong to other files' owners.
