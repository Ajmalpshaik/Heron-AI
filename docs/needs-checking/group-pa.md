# Needs checking — Group PA

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group PA - REPORT_SPRINKLER_NETWORK version 2: a ceiling-hosted sprinkler's level read from its Schedule Level, as FILTER_ELEMENTS_BY_CATEGORY reads it - DRAFT until proved (2026-11-21)

**Tracked as GitHub issue [#1160](https://github.com/Ajmalpshaik/Heron-AI/issues/1160) since 2026-11-21** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** row [5b-1910](../fragment-issues/row-5b-1910.md), GitHub issue
[#1126](https://github.com/Ajmalpshaik/Heron-AI/issues/1126). Version 1 named an element's level from
`LevelId`, then an MEP curve's start level, then the family's Level - never Schedule Level - so a sprinkler
on a ceiling's face or a level's plane, whose family Level is empty (row
[5b-342](../fragment-issues/section-5b-rows-326-350.md)) and whose only level is its Schedule Level, was
written `"level": null`. Its own proof of 2026-11-02 on *P1102 MEP* shows all six heads of *Fire Protection
Wet 1* that way, beside pipes and fittings on *Level 2*. Version 2 asks `LevelId`, then
`FILTER_ELEMENTS_BY_CATEGORY`'s five level parameters in its order, and one that holds no level is passed
over (row [5b-335](../fragment-issues/section-5b-rows-326-350.md)); the Space probe for a head above its
Space (row [5b-338](../fragment-issues/section-5b-rows-326-350.md)) asks at that same level. The card was
changed in its C# in a cloud container with no Revit, its version raised and set to DRAFT; its version 1
proof is kept on the card as the record. **Nothing here has run in Revit.** It compiles on every release it
claims (`tools/check-fragments-compile.py`, the cloud container's .NET SDK, 2026-11-21), and
`tests/test_sprinkler_takeoff.py` reads the lookup's order in the source - and failed on version 1's. No
add-in code changed, so nothing needs rebuilding.

**The model is a scratch copy, never a working model** - *P1102 MEP* as version 1's proof ran it, Revit
2020 first. The card only reads; nothing is kept. Then sign the card on PA1 and PA2, as its old proof was.

| Tool | Was | Now | Row | Checks |
|---|---|---|---|---|
| [`report-sprinkler-network`](../../brain/fragments/report-sprinkler-network/fragment.yaml) (`REPORT_SPRINKLER_NETWORK`) | PROVEN v1 | DRAFT v2 | [5b-1910](../fragment-issues/row-5b-1910.md) | PA1, PA2 |

| ID | Do this | Pass looks like |
|---|---|---|
| **PA1** | *`REPORT_SPRINKLER_NETWORK` v2, positive.* Revit 2020, a scratch copy of *P1102 MEP*: one head of *Fire Protection Wet 1* (a *M_Sprinkler - Pendent - Hosted*) selected, its Properties palette open - write down its Schedule Level and whether a *Level* or *Reference Level* row shows at all. Run `systemName=Fire Protection Wet 1` | `elementCount` 29 and the same elements, connectors and spaces as version 1's proof; every one of the six heads carries `"level"` equal to the Schedule Level written down - **never `null`**, as version 1 wrote all six; every pipe and fitting still `"level":"Level 2"`, as version 1's proof - the new order asks an MEP curve's start level last, so a pipe reading another level is a FAIL |
| **PA2** | *`REPORT_SPRINKLER_NETWORK` v2, negative - the level is read, never guessed.* The same scratch copy: change ONE head's Schedule Level by hand in Properties to a level it does not hang under (*Level 1*, the head under *Level 2*'s ceiling). If Revit greys Schedule Level out for this family, write that down and run (b) alone. (a) Run the same request. (b) Undo the change and run `systemName=ZZZ No Such System` | (a) that head's `"level"` is *Level 1*, as Properties shows - never *Level 2*, which its height would suggest, and never `null`; the other five heads as in PA1. (b) `elementCount` 0, `system` null, the finding *"No fire protection system is named ..."*, as version 1's negative |

**Not here:** rows that need a Revit run BEFORE any fix can be written - 106 (the clearance checks'
speed on a real MEP model), 1121 and 1441 (links, issue [#946](https://github.com/Ajmalpshaik/Heron-AI/issues/946)),
1831 (issue [#948](https://github.com/Ajmalpshaik/Heron-AI/issues/948)) - are in their own rows and issues,
untouched.
