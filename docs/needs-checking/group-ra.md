# Needs checking — Group RA

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in this
> file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back into the
> register for every tool that reads the register.

## Group RA - REPORT_LEVEL_ELEVATIONS version 7, the one datum tool: CHECK_DATUMS_AGAINST_LINK merged in, copy-monitored levels and grids, a link placed twice refused until its copy is named - DRAFT until proved (2026-11-26)

**Tracked as GitHub issue [#1461](https://github.com/Ajmalpshaik/Heron-AI/issues/1461) since 2026-11-26** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** the owner's answer of 2026-11-26 to [row 5b-1878](../fragment-issues/row-5b-1878.md) (C), issue
[#1088](https://github.com/Ajmalpshaik/Heron-AI/issues/1088): *"2 tools are there for the same purpose, level
checking or datum checking. Levels or grids - 3 types are there: one is in the model, one is in the linked model,
another is copy-monitored level or grid. If you can add all in one tool that is better, so with one tool all can
cover."* [`CHECK_DATUMS_AGAINST_LINK`](../../brain/fragments/check-datums-against-link/fragment.yaml) (PROVEN on
Revit 2020, [Group OJ](group-oj.md)) is merged into
[`REPORT_LEVEL_ELEVATIONS`](../../brain/fragments/report-level-elevations/fragment.yaml) and DEPRECATED. Version 7
holds the retired card's whole check beside the level heights, lists every copy-monitored level and grid here in
`monitored` and measures it against the link placement it watches, and - [D-207](../decisions/D-207.md), *"Ask
which copy"* - never measures a link placed more than once through one placement until the copy is named. The card
was DRAFT at version 6 and stays DRAFT; the merge moved a PROVEN check into a DRAFT card, so the check is unproven
until this group is run and signed (D-30). Its [`tests/cases.yaml`](../../brain/fragments/report-level-elevations/tests/cases.yaml)
holds every case; these rows are the run.

**What is done and what is not.** In code; compiles for 2020 to 2027 (`tools/check-fragments-compile.py`,
2026-11-26, a Linux container); `Element.IsMonitoringLinkElement` and `GetMonitoredLinkElementIds` are on every
release (`tools/api-surface --members Element`). **Never run in Revit.** That `GetMonitoredLinkElementIds` hands
back the LINK INSTANCE a datum watches - not the element inside the link - is read from the API's own description,
not seen: RA3 settles it. Within the watched placement a monitored datum is matched by name, then by lining up, as
every other datum is.

**D-204 still applies to the level heights.** The storeys line and the two heights cannot come back empty, so that
part is signed on the by-hand reading D-204 names (the storeys line against Manage > Levels). The link check has an
empty leg of its own - RA1 - and is signed on the run.

**The model: scratch, never a working one** - Revit 2020 first (the retired card's release), then 2024. The
retired card's own arrangement, [Group OJ](group-oj.md)'s and its `tests/cases.yaml`: `datum-host.rvt` with
`datum-link.rvt` linked Origin to Origin, saved under `%TEMP%\heron-work\ra\`.

| ID | Do this | Pass looks like |
|---|---|---|
| **RA1** | **Negative, first.** `REPORT_LEVEL_ELEVATIONS` with includeLinks true, toleranceMm 5 | failures 0; failingIds and failingElements EMPTY; grid B *"aligned \| 3.00 mm"*; compared 7, aligned 7; the level heights and storeys as version 6 gave them |
| **RA2** | The same with toleranceMm left out | failures 1, grid B *"off \| 3.00 mm"*, failingIds holding grid B only; every other row as Group OJ's OJ positive; `linkUsed` the one placement; `against` on every row |
| **RA3** | Tie this model's `Level 2` to the link's with Collaborate > Copy/Monitor > Select Link > Monitor, save; run with nothing given, then with includeLinks true | Nothing given: `monitored` names *"level 'Level 2' copy-monitors 'datum-link.rvt : 1 : ...'"*, `report` empty, compared 0, a `findings` line saying links were not asked for - no link opened. includeLinks true: Level 2's row says *"copy-monitored from this link"*, aligned. Then raise the LINK's Level 2 by 50 mm, save, reload: Level 2 off at +50.00 mm |
| **RA4** | **Negative.** Place `datum-link.rvt` a second time 100 mm up (Insert > Link Revit, then Move), Level 2's monitor left on the first; includeLinks true | `findings` naming BOTH placements and asking for the copy - *"None of its placements was measured; the first is never taken"*; only Level 2 measured, against *"... (copy-monitored only)"*, aligned. Then remove the monitor (Stop Monitoring) and run again: NOTHING COMPARED, `refused` naming both placements, compared 0 |
| **RA5** | RA4's model, linkName the SECOND placement's full name | compared 7; both levels off at +100.00 mm, *"the link's is higher"*; every grid as RA2 |
| **RA6** | **Negative.** datums `walls`; then toleranceMm 0; then linkName `Structural` | each refused naming the value, compared 0, findings ending *"NOTHING WAS COMPARED."* - and the level heights still answered above it |
| **RA7** | RA1, RA2 and RA3 on Revit 2024 | the same answers; ids in full |

Then the owner signs the card from the run on its proof (the [`fragment-proving`](../../.claude/skills/fragment-proving/SKILL.md)
skill) - never a session.
