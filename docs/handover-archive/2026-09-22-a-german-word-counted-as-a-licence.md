# Session note — A GERMAN WORD COUNTED AS A LICENCE

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — A GERMAN WORD COUNTED AS A LICENCE

**[Row 5b-129](../FRAGMENT-ISSUES.md), FIXED.** `tools/check-licence.py` read end to end — 288
lines, never opened before. The **second** of the seven unread CI gates, taken next because a
defect in it is the one that ships somebody else's rights with Heron.

It answers **Q-53** as [D-66](../DECISIONS.md), and its own docstring states the rule it must never
break:

> A file with no marker at all is reported as UNMARKED rather than as clean — *"no evidence of a
> problem"* and *"evidence of no problem"* are different findings, and a tool that merges them is
> the tool `scientific-agent-skills` already has.

**Two ways it merged them**, both measured on trees written for the purpose.

**One.** `LICENCE_NAME` matched a bare `mit` case-insensitively:

| the imported unit's only marker | the answer |
|---|---|
| `note: gemessen mit dem Werkzeug` | **1 clean** |
| the same file without that word | **1 unmarked**, correctly |

Heron reads standards, and a standard is not always in English.

**Two.** An unmarked list longer than forty printed forty and said `..and 5 more (--all)` — and
**`--all` printed the same forty**. Five units could not be seen by any documented means.

**Not an R-82 breach, and not claimed as one** — [rows 5b-100 and
5b-111](../FRAGMENT-ISSUES.md) both recorded that distinction, and R-82 is about the scanner's
*window* rather than a report's margin. The defect here is on the tool's own terms: it names a
remedy, and the remedy does nothing.

**Neither is a live failure today, and saying so is part of the finding**: all 406 units declare
`source: OFFICIAL`, no file carries the word `mit`, and the tool reports 406 clean. Both holes are
in the branch the first **imported** unit lands on — the future
[docs/09](../09-skills-and-fragments.md)'s COMMUNITY PACKAGES plans for, and the whole
reason this tool exists.

### What it does now

A bare `MIT` is matched **case-sensitively**, in its own pattern beside the case-insensitive list:
the licence is written in capitals and the German word is not, and `MIT License` was already
matched either way. Both real forms still work and both are checked. `--all` prints the whole
unmarked list, and the *and N more* line is derived from what was actually shown rather than from
a second copy of the number forty.

**The gate reads the same on the repository**: 406 units, 406 clean, 0 findings, 0 unmarked.

**`tests/test_check_licence.py`**: **3 red** against the module as found, with everything around
them green before and after — all three findings it exists to make, both ways of being unmarked,
and the two false positives an earlier round already took out of it (a C# cast read as a copyright
holder, and a year with no name after it).

### The docstring is one of the best in the repository

It names the live example behind Q-53 — `K-Dense-AI/scientific-agent-skills`, MIT on the landing
page, **four of its 163 skills carrying "All rights reserved"**, and their own skill scanner never
looking at a licence — and states the one rule it encodes: **read the files, not the landing
page**.

And the parts that are right are argued rather than assumed: `COPYRIGHT` requires a **year** beside
the marker, because the first version reported a C# cast and *"a licence tool that cries wolf is a
tool somebody turns off"*; a holder must contain three letters, so `Copyright 2026` with nothing
after it is a marker with no holder in it; `repo_relative` falls back to an absolute path because
`os.path.relpath` **raises** across Windows drives; that duplication of `heron_fragment`'s rule is
argued rather than accidental, since importing it would cost PyYAML on the bare machine where you
check the licence of something you just downloaded; and `.claude/skills` is deliberately out of
`units()`, because Q-53 is about what Heron's *users* redistribute and scanning the toolbox
alongside the cargo would bury the finding.

**One thing recorded rather than fixed**: `--all` is the only flag and an unknown one is ignored in
silence — the same shape as rows [5b-112](../FRAGMENT-ISSUES.md) and [5b-127](../FRAGMENT-ISSUES.md) —
but here it costs only a missing CLEAN section and cannot move the exit code.

**Five CI gates remain unopened**: `check-fragments-compile`, `check-products`, `check-metadata`,
`check-intrusion`, `check-signatures`.
