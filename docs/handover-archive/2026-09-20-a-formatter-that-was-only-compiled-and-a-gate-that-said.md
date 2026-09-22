# Session note — A FORMATTER THAT WAS ONLY COMPILED, AND A GATE THAT SAID "EVERY PROJECT" WHILE NAMING FIVE OF SIX

> **Archived session note** from 2026-09-20. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-20 — A FORMATTER THAT WAS ONLY COMPILED, AND A GATE THAT SAID "EVERY PROJECT" WHILE NAMING FIVE OF SIX

**PR #212, draft.** Two things, and the second was found by doing the first.

**A review finding was right and is fixed.** `RevitFragment.Size` had become the only thing separating
`(2 of 1128)` from the misleading `(2)` that [row 75](../FRAGMENT-ISSUES.md) is about, and it shipped in
#210 with **no test**. A compiler cannot catch a regression in its wording or in the ORDER of its two
`object` arguments. The rule now lives in `revit/Heron.Revit.Addin/HeronBindingNote.cs`, which touches
no Autodesk type, so `tests/Heron.BindingNote.TestHost` links it **BY SOURCE** — one copy of the rule,
[row 96](../FRAGMENT-ISSUES.md)'s lesson. **It was measured against the previous implementation**: built
against `Size` as it stood before the fix, **3 of 7 checks fail and the host exits 1**, and the four
that still pass are exactly the ones that must not move.

**THE LESSON WORTH CARRYING IS THE SECOND ONE, AND IT IS ABOUT BEING WRONG.**
[Row 161](../FRAGMENT-ISSUES.md): `brain/heron_dotnet.py` listed the projects the compile gate builds
under the comment *"Every project, in dependency order"* while naming **five of six**, and
`Heron.Banner.TestHost` had never been compiled on any release. **The first reading of that was that
somebody forgot, and it was written down that way before being checked.** It was wrong. `5669e7e` left
it out **on purpose** — a `WinExe` WPF project, CI on Linux with `EnableWindowsTargeting`, never
watched go green — and said both where it belonged and what would unblock it. **The reason lived only
in the commit message**, so a held-back project and a forgotten one looked identical.

What caught it was reading the commit that added the file **before** writing the row. That is the
queue rule *"VERIFY BEFORE YOU BELIEVE"* earning its place again, and the row keeps the wrong first
reading in writing because the shape of the mistake is the point.

The condition `5669e7e` set is now met and Banner is listed: `tools/check-compile.py` on **Linux**,
the **10.0.x** SDK, `-p:EnableWindowsTargeting=true` — the same command, OS, SDK line and flag
`gates.yml` itself uses — builds **every project in `PROJECTS` on every release in `RELEASES`**, and
CI has since done the same. No totals are written here on purpose: the gate prints its own, and both
lists are meant to grow. `heron_dotnet.unlisted()` now **fails the gate** on any `.csproj` that
neither `PROJECTS` nor the named `NOT_SHIPPED` accounts for, so the next exclusion has to be readable
where the gate is.

**LEFT FOR THE NEXT SESSION — [row 162](../FRAGMENT-ISSUES.md), recorded and not fixed.** **One suite
more sits in `tests/` than `check-gaps.py` ever runs**, and no total is written here on purpose —
`ls tests/test_*.py | wc -l` against the count the sweep prints is the check, and it is off by one
until this is done. `test_bridge_roundtrip.py` is the one, skipped by name, and unlike row 161 **its
reason is in the right place** — in a comment beside the skip, and correct: with no host binary the
suite `return 1`, which would read as a FAIL for a build step nobody took. The defect is that the
suite has **no exit-3 path**, so it cannot say *"I could not run"*, and a silenced suite prints
nothing at all — not `ok`, not `wait`. The repair is the one `tests/test_binding_note.py` already
uses next door, and a Codex review on PR #212 made the same point about **this paragraph**: a typed
total goes stale the moment a suite is added, while *"exactly one is skipped, and silently"* stays
true and is what the next session actually needs.

**Nothing here needed a Revit, and nothing here is evidence about one.** Compiling is the API surface
agreeing. `(2 of 1128)` has still never been seen in a real binding note, row 75 is still **OPEN**, and
D3 in [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) is still the line that catches a unit error.
