# Needs checking — Group RD

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in this
> file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back into the
> register for every tool that reads the register.

## Group RD - four reports given an answer that can read empty: DESCRIBE_BLANK_PARAMETERS v2, REPORT_ELEMENT_DEPENDENCIES v3, REPORT_CONSTRAINTS v3, REPORT_LEVEL_ELEVATIONS v8 - DRAFT until proved (2026-11-26)

**Tracked as GitHub issue [#1482](https://github.com/Ajmalpshaik/Heron-AI/issues/1482) since 2026-11-26** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** the overnight proving run of 2026-11-26 on Revit 2020, a scratch copy of the Revit 2020 MEP sample, failed
four cards only because their negative leg could never read empty - the shape of issues
[#1477](https://github.com/Ajmalpshaik/Heron-AI/issues/1477), [#1478](https://github.com/Ajmalpshaik/Heron-AI/issues/1478)
and [#1480](https://github.com/Ajmalpshaik/Heron-AI/issues/1480) (rows 5b-2067, 5b-2068, 5b-2070 and 5b-2071, written
on the proving branch). Each card's C# changed so that a case with nothing to say leaves nothing to count:

- [`DESCRIBE_BLANK_PARAMETERS`](../../brain/fragments/describe-blank-parameters/fragment.yaml) **version 2** - a new
  count, `flagged` (`role: result`): how many elements the findings name. The first version gave prose alone, and prose
  is never judged as a quantity (D-51).
- [`REPORT_ELEMENT_DEPENDENCIES`](../../brain/fragments/report-element-dependencies/fragment.yaml) **version 3** -
  `dependents` is keyed only by an element something goes with, as `joined` already was; `goesTotal` keeps its value
  and becomes `role: accounting`, because it always counts the elements given.
- [`REPORT_CONSTRAINTS`](../../brain/fragments/report-constraints/fragment.yaml) **version 3** - `constrainedBy` and
  `lockedTo` are keyed only by an element something holds.
- [`REPORT_LEVEL_ELEVATIONS`](../../brain/fragments/report-level-elevations/fragment.yaml) **version 8** - a document
  with no levels leaves `storeys` empty, where version 7 said *"Every level is marked Building Story - 0 level(s)
  read"*. [D-204](../decisions/D-204.md) is unchanged: a model with levels is still signed on its by-hand reading.

**What is done and what is not.** In code; compiles for 2020 to 2027 (`tools/check-fragments-compile.py`,
2026-11-26, a Linux container). `heron_validate.looks_empty` was run on each card's contract with the night's
negative answers in the new shape: all four read EMPTY, and the same answers in the old shape still read as
content. **Never run in Revit.** These checks replace nothing in [Group QT](group-qt.md) or [Group RA](group-ra.md):
those run on the new versions too, and an element version 2 or 7 gave an empty list or line now has none.

**The model: scratch, never a working one** - Revit 2020, the night's own arrangement: a scratch copy of the Revit
2020 MEP sample, and for RD4 the area tag family it was run on. Batch-provable: the job file names each card with a
positive and a negative selection.

| ID | Do this | Pass looks like |
|---|---|---|
| **RD1** | `DESCRIBE_BLANK_PARAMETERS` after READ_ELEMENT_PARAMETERS: positive Comments on ducts some of which have none; **negative** Length on 10 ducts that all have a length | Positive: one finding naming the blanks, `flagged` equal to that number. Negative: `findings` 0 item(s), `flagged` 0 - **NEG EMPTY**, no longer *"returned content"* |
| **RD2** | `REPORT_ELEMENT_DEPENDENCIES`: positive a tagged duct or a door hosted in a wall; **negative** the night's three free walls | Positive: the tag among `dependents`, `goesTotal` the number Revit's Delete box shows. Negative: `dependents` 0 entry(ies), every result 0, the three walls in `standalone`, `goesTotal` 3 - **NEG EMPTY** |
| **RD3** | Lock a dimension between two walls on the scratch copy, save. `REPORT_CONSTRAINTS`: positive those two walls; **negative** the night's three free walls | Positive: each wall naming the other in `lockedTo`, the dimension in `constrainedBy`. Negative: `constrainedBy` and `lockedTo` 0 entry(ies), the three in `unconstrained` - **NEG EMPTY** |
| **RD4** | `REPORT_LEVEL_ELEVATIONS` on the MEP sample (positive), then on the area tag family (**negative**) | Positive: `storeys` as version 7 gave it on the sample. Negative: `storeys` 0 item(s) and no line saying every level is marked; `findings` opening *"0 level(s), bottom to top"* - **NEG EMPTY** |

Then the owner signs each card from its run (the [`fragment-proving`](../../.claude/skills/fragment-proving/SKILL.md)
skill) - never a session. `REPORT_LEVEL_ELEVATIONS` stays signed as D-204 says for its level heights.
