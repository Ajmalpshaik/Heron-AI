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
`FILTER_ELEMENTS_BY_CATEGORY`'s five level parameters in its order - with the instance's schedule-only level
after Schedule Level, which `REPORT_SPRINKLER_LAYOUT_SPACES` asks and through which its proof of 2026-10-21
put head 545673 on *Level 2* - and one that holds no level is passed
over (row [5b-335](../fragment-issues/section-5b-rows-326-350.md)); the Space probe for a head above its
Space (row [5b-338](../fragment-issues/section-5b-rows-326-350.md)) asks at that same level. The card was
changed in its C# in a cloud container with no Revit, its version raised and set to DRAFT; its version 1
proof is kept on the card as the record. **Nothing here has run in Revit.** It compiles on every release it
claims (`tools/check-fragments-compile.py`, the cloud container's .NET SDK, 2026-11-21), and
`tests/test_sprinkler_takeoff.py` reads the lookup's order in the source - and failed on version 1's. No
add-in code changed, so nothing needs rebuilding.

**The model is a scratch copy, never a working model** - *P1102 MEP* as version 1's proof ran it, Revit
2020 first. The card only reads; nothing is kept. Then sign the card on PA1 and PA2, as its old proof was; PA3 signs nothing.

| Tool | Was | Now | Row | Checks |
|---|---|---|---|---|
| [`report-sprinkler-network`](../../brain/fragments/report-sprinkler-network/fragment.yaml) (`REPORT_SPRINKLER_NETWORK`) | PROVEN v1 | DRAFT v2 | [5b-1910](../fragment-issues/row-5b-1910.md) | PA1, PA2 (PA3 reads another card) |

| ID | Do this | Pass looks like |
|---|---|---|
| ~~**PA1**~~ | *`REPORT_SPRINKLER_NETWORK` v2, positive.* Revit 2020, a scratch copy of *P1102 MEP*: one head of *Fire Protection Wet 1* (a *M_Sprinkler - Pendent - Hosted*) selected, its Properties palette open - write down its Schedule Level and whether a *Level* or *Reference Level* row shows at all. Run `systemName=Fire Protection Wet 1` | `elementCount` 29 and the same elements, connectors and spaces as version 1's proof; every one of the six heads carries `"level"` equal to the Schedule Level written down - **never `null`**, as version 1 wrote all six; every pipe and fitting still `"level":"Level 2"`, as version 1's proof - the new order asks an MEP curve's start level last, so a pipe reading another level is a FAIL |
| ~~**PA2**~~ | *`REPORT_SPRINKLER_NETWORK` v2, negative - the level is read, never guessed.* The same scratch copy: change ONE head's Schedule Level by hand in Properties to a level it does not hang under (*Level 1*, the head under *Level 2*'s ceiling). If Revit greys Schedule Level out for this family, write that down and run (b) alone. (a) Run the same request. (b) Undo the change and run `systemName=ZZZ No Such System` | (a) that head's `"level"` is *Level 1*, as Properties shows - never *Level 2*, which its height would suggest, and never `null`; the other five heads as in PA1. (b) `elementCount` 0, `system` null, the finding *"No fire protection system is named ..."*, as version 1's negative |
| ~~**PA3**~~ | *Not a check of this card - whether `FILTER_ELEMENTS_BY_CATEGORY` v7 has the same gap.* The same scratch copy, nothing changed: `category=Sprinklers` with `levelId` = *Level 2*'s id. It asks five level parameters and not the schedule-only level this card added | The six heads of PA1 come back on *Level 2*. **Six missing, or counted under "no level found", is a finding, not this card's FAIL**: that card misses a face-hosted head's level the way this one did - a new 5b row and its issue (`heron-issue-recorder`), and the schedule-only level added there. Write down which happened either way, since it says which Schedule Level parameter the head carries |

**Not here:** rows that need a Revit run BEFORE any fix can be written - 106 (the clearance checks'
speed on a real MEP model), 1121 and 1441 (links, issue [#946](https://github.com/Ajmalpshaik/Heron-AI/issues/946)),
1831 (issue [#948](https://github.com/Ajmalpshaik/Heron-AI/issues/948)) - are in their own rows and issues,
untouched.

### Results, 2026-11-26 - Revit 2020, session 32484, add-in 0.3.0.0 (file `df0d401c52ca05f4`), main `02780d9a4`

**Model:** *P1102 MEP* - a FRESH scratch copy of Revit 2020's `rme_advanced_sample_project.rvt` in `%TEMP%\heron-work\p1102` (the installed sample untouched; not the 11-02 copy). Every run rolled back or read-only, through `heron_bridge_client.py`.

| Row | Result |
|---|---|
| PA1, run 2026-11-26 (card now version 3) | **PASS for the read** - `systemName=Fire Protection Wet 1`: `elementCount` **29** - 6 Sprinklers, 14 Pipes, 9 Pipe Fittings - and every `"level"` in `networkJson` reads **Level 2**, none `null`. The heads' Schedule Level was not read by hand in Properties. |
| PA2, run 2026-11-26, (b) only | **(b) PASS** - `systemName=ZZZ No Such System`: `elementCount` 0, *"No fire protection system is named "ZZZ No Such System" in this model."* **(a) NOT RUN** - it needs one head's Schedule Level changed by hand. |
| PA3, run 2026-11-26 | **PASS** - `FILTER_ELEMENTS_BY_CATEGORY` `category=Sprinklers`, `levelId=Level 2`: *"6 of 6 scanned element(s) are on level 'Level 2', within Sprinklers. 0 carry no level at all"*, `unresolvedLevel` 0. The six heads are found; no new row. |

**PA2 (a), run 2026-11-26 - Revit 2020, session 27788, add-in file `df0d401c52ca05f4`: PASS** (rolled back). Model *on-mep*,
a fresh copy of the same MEP sample. Not changed by hand: the setup step `SET_PARAMETER_VALUES_BY_ID` (PROVEN) wrote
`key=Mark|in=Sprinklers;1|Schedule Level=Level 1` - head 545673 (Mark 1), whose Schedule Level read *Level 2* - inside
the same rolled-back run, then `REPORT_SPRINKLER_NETWORK` `systemName=Fire Protection Wet 1`: `elementCount` 29, head
545673's `"level"` **Level 1**, every other element Level 2 - never the height's guess, never `null`. The negative
`ZZZ No Such System`: `elementCount` 0. Revit did not grey Schedule Level out for this family.
