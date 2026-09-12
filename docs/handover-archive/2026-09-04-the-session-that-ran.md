# HANDOVER — the session that ran 2026-09-04

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**What happened, in one line: the library went from 202 fragments to 210, and every one of the eight was
built because a real sentence was measurably reaching a fragment that WRITES.**

**The selection rule did the work again, and this time it found a cluster rather than eight singles.**
Asking `heron_brain.lookup` the owner's own sentences against the library as it stood, every question
about a schedule was being answered by `ADD_SCHEDULE_FIELDS` — the fragment that adds a column. It had
become the sink for the whole subject because it was the only schedule *editor* in the library. Two of
the misroutes were not near misses at all:

| The sentence | Where it went | What that would have done |
|---|---|---|
| *"remove a column from the schedule"* | `ADD_SCHEDULE_FIELDS` | put the column back |
| *"filter the schedule to level 2 only"* | `SET_ELEMENT_LEVEL` | **moved the ducts to another level** |

The second is this project's whole reason for existing, found in its own library: a request to hide rows
in a table, resolving to a fragment that changes the building, and the schedule looking right afterwards.

**Reading the neighbours paid again, and differently.** `FIND_SCHEDULES` already named *"adding columns,
exporting to CSV, placing on a sheet"* in its own purpose as the things it hands schedules to — **two of
the three had never been built.** `PLACE_VIEW_ON_SHEET` was refusing schedules correctly, naming *"a
schedule that needs a different call"*, with nothing to hand them to. The library had been describing
this batch for weeks. `CREATE_SCHEDULE` carried a routing row reading *"nothing in the library exports
anything yet"*, true when written and left standing after five export fragments landed — **a routing row
that ages into a lie points a reader at nothing**, and it is the cheapest kind of drift to miss because
nothing about it looks wrong.

**Two capabilities were found to be impossible, and are recorded rather than left to be re-derived:**

- **A calculated (formula) column cannot be created.** `ScheduleField` carries no formula member on any
  release 2020 → 2027. The field type exists in the enum; the expression cannot be written.
- **A schedule column's UNIT cannot be set in one implementation.** It is `UnitType` on 2020 and
  `GetSpecTypeId` from 2022 — the first gone by 2027, the second never on 2020. Every fragment here is
  one implementation for all eight releases, so this is not a fragment.

Both are routing rows naming Revit's own route, in both fragments a reader could land on.

**The metadata gate caught an eighth shape, and it is one an existence check structurally cannot reach.**
`ScheduleField.HorizontalAlignment` looks like it takes `HorizontalAlignmentStyle`. Both that enum and
`ScheduleHorizontalAlignment` exist on every release — so **every name check passes on both and exactly
one compiles.** It was settled by decoding the property signature's type token out of the metadata blob:
`ScheduleHorizontalAlignment`, on 2020 and on 2027. Where two plausible types both exist, presence proves
nothing.

**And the probe itself was wrong first.** It reported `ScheduleFilterType` missing on all three releases
when it is present on all of them — it was splitting a bare type name into a type plus a member. *A
checker that finds nothing is evidence about the checker; one that finds something FALSE is the same
lesson with the sign flipped, and it is more convincing because it looks like a result.*

**One thing believed blocked was not.** `test_mcp_serves` was recorded here as blocked on the MCP SDK.
`pip install mcp` ran it, and it passed — 18 suites, **17 pass, 1 blocked**. That is the fifth time
something filed under *needs a machine* needed somebody trying it. The .NET SDK genuinely is blocked:
the proxy answers `403` to the CONNECT for `builds.dotnet.microsoft.com` and names the denial.

### What the next session should do, in order

1. **`python tools/check-gaps.py` first.** Computed from disk; it wins over every sentence here.
2. **On any machine with the .NET SDK: `python tools/check-fragments-compile.py`.** **Eighty**
   fragments now owe a real compile. This is `A9` and it is still the largest thing needing no Revit.
3. **With Revit open: start proving.** All 218 are `DRAFT`. [D-30](../DECISIONS.md) means a proof needs
   a case that comes back EMPTY, not just one that works.
4. **To carry the library build on:** [§9a](../HANDOVER.md#9a-continuing-the-library-build--the-recipe-so-another-session-can-just-start).
   Run the lookup sweep first. Un-mined source areas left are the rest of `actions/reporting/`,
   `actions/color-graphics/`, `actions/visibility/` and most of `filters/`.

**Measured gaps left open on purpose, with the sentences that found them** — each one is a real misroute,
none was guessed at:

| The sentence | Where it goes today |
|---|---|
| *"save the view as an image"* | `CREATE_SELECTION_FILTER` |
| *"which title block is on each sheet"* / *"change the title block"* | `CREATE_SHEETS` — a **write** for a question |
| *"what is the bounding box of this duct"* | `FILTER_ELEMENTS_IN_ROOM` |
| *"what parameters can I filter on"* | `REPORT_GLOBAL_PARAMETERS` — global parameters are a different thing |
| *"which families are nested inside this one"* | `CHECK_FAMILY_STANDARDS` |
| *"what is the pressure drop in this duct run"* | `SET_MEP_SLOPE` — a **write** for a question |
| *"report the routing preferences of this pipe type"* | `CREATE_MEP_SYSTEM_TYPE` |

**One observation for whoever reads `check-intrusion` next:** two of this batch's fragments
(`READ_SCHEDULE_CONTENTS`, `EXPORT_SCHEDULE_TO_CSV`) went straight into its top twelve. That is the
generic-phrasing effect the tool already documents — *"read me what is in the schedule"* shares words
with half the library — and the tool's own guidance holds: **those are real sentences and none of them
may be taken away to buy a number.** Left as they are, deliberately.

---
