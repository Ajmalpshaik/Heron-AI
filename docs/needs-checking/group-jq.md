# Needs checking — Group JQ

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-29 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group JQ - an adaptive component's points read back with their Orientation: `REPORT_FAMILY_TEMPLATE` version 4 (2026-10-29)

Asked for by the owner on 2026-10-29 (*"build the three widenings too"*), knowing that a change under a PROVEN
card's `impl/` makes its proof stale. Recorded as waiting in
[the facade plan](../work-notes/plans/facade-capability.md) and in
[`set-adaptive-point-orientation`](../../brain/fragments/set-adaptive-point-orientation/fragment.yaml)'s routing
table: *"what orientation are the adaptive points"* had no reader, and a question must not land on a write
([D-86](../DECISIONS.md)). [Group JK](group-jk.md) is the write; this group is the read that checks it.

**What changed.** [`report-family-template`](../../brain/fragments/report-family-template/fragment.yaml)
(FRG-DOC-037) version 4: each row of `pointList` keeps what version 3 printed - kind, placement number, id,
position - and adds the point's **Orientation**: *"placement point 1 <id> at 0, 0, 0 mm, Orientation Host and
Loop System (xyz)"*. One finding says the placement points' Orientations together. The words are
`SET_ADAPTIVE_POINT_ORIENTATION`'s own mapping, copied exactly, so the two cards print the same words for one
value - and [JK5](group-jk.md) checks those words for both. A **shape handle point** reads *"Orientation not read
(shape handle point)"* with no call made; a placement point Revit refuses to read says Revit's reason. Never a
guessed value ([D-52](../DECISIONS.md)). A family with no adaptive points prints nothing new. It still only
reads.

**What Revit's API says.** Read 2026-10-29 with `tools/api-surface --members` over the reference assemblies of
2020 to 2027, one signature on all eight: `AdaptiveComponentFamilyUtils.GetPointOrientationType(Document,
ElementId)` and the enum `AdaptivePointOrientationType` (ToHost, ToHostAndLoopSystem, ToGlobalZthenHost,
ToGlobalXYZ, ToInstanceZthenHost, ToInstance). The `RevitAPI.xml` of all eight releases says the call is *"of an
Adaptive Placement Point"* and throws when the id *"does not correspond to an Adaptive Placement Point"*, or does
not belong to *"an Adaptive Family"*.

**What is done and what is not.** It compiles for all eight releases (`tools/check-fragments-compile.py`,
2026-10-29, in a cloud session). *"what orientation are the adaptive points"* is declared on the card, and the
lookup resolves it there by identity on the container's backend (a scratch store) - before this change it
landed on `SET_ADAPTIVE_POINT_ORIENTATION`, a write. Ranked by words alone it is still second to that write
(`tools/check-routing.py`), and three paraphrases asked without the question flag still reach the write; asked as
questions they reach this card. **Nothing here has run in Revit.** The card went from PROVEN to DRAFT with this
change; version 3's proof (2026-10-18, Family4 and Project1, Revit 2020) is kept on it as a record and is stale,
so **version 3's own cases are owed again** (JQ2).

**The models - scratch, never a working family.** Revit 2020 first, where version 3 was proved. Saved under
`%TEMP%\heron-work\group-jq\` unless named otherwise:

- *Heron JK Orientation* - [Group JK](group-jk.md)'s rig **after JK2 was kept**: four placement points, every
  one at *Host and Loop System (xyz)*. If JK2 was discarded, set the four by hand in Properties and save - this
  group proves the reader, not the write.
- *Heron JQ Adaptive Untouched* - a new family from the metric *Generic Model Adaptive* template, four placement
  points made (by hand, or `PLACE_ADAPTIVE_POINTS` *"0,0,0; 1000,0,0; 1000,1000,0; 0,1000,0"* kept), no
  Orientation ever changed, saved.
- *Heron JQ Plain* - a new family from the plain metric *Generic Model* template, nothing built, saved.

The job file is [`tools/jobs/report-family-template-orientation-2026-10-29.yaml`](../../tools/jobs/report-family-template-orientation-2026-10-29.yaml)
(`--dry-run` reads it back and sends nothing: *1 WOULD RUN*, 2026-10-29). Every line below with
`HERON_CLIENT_ID=ajmal-pc` and `--session <process id>` when two Revits are connected.

| # | Run | Look for |
|---|---|---|
| **JQ0** | *Arrange, by hand.* The three families above, open; nothing of the owner's | Their titles in the window list; *Heron JK Orientation* showing four numbered points |
| **JQ1** | *Record by eye.* In *Heron JK Orientation* and *Heron JQ Adaptive Untouched*, select each point; read Properties > Orientation | **Record** the words for each point. JQ3 and JQ5 are judged against what is recorded here, not against what the card says Revit should show |
| **JQ2** | *Version 3's cases again - its proof is stale.* First, read only: [`report-family-template-units-2026-10-12.yaml`](../../tools/jobs/report-family-template-units-2026-10-12.yaml) on Revit 2020 and [`report-family-template-omniclass-2026-10-11.yaml`](../../tools/jobs/report-family-template-omniclass-2026-10-11.yaml) on Revit 2024, each with the arrangement its header names; then by hand the wall-based, line-based and Curtain Panel Pattern Based cases in [the card's cases](../../brain/fragments/report-family-template/tests/cases.yaml), and the Detail Item and metric negatives | Each answers as its case says, word for word as version 3 did - the kind, units line, settings, planes, lines, classification codes, the project's `notAFamily` true. **Run before JQ3:** all three job files write the same proof draft, and JQ3's is the one signed. A difference from version 3 is a defect in version 4, never a new expectation |
| **JQ3** | *The positive - the D-30 positive.* `python tools/batch-prove.py tools/jobs/report-family-template-orientation-2026-10-29.yaml --session <pid>`, its positive leg, in *Heron JK Orientation* | `pointList`: placement points 1 to 4, each with its id and position, then *", Orientation Host and Loop System (xyz)"* - JQ1's words and JK's `pointOrientations` words; a finding *"Orientation of the adaptive points, as Properties shows it on each: point(s) 1, 2, 3, 4 Host and Loop System (xyz). ..."* |
| **JQ4** | *The negative - the D-30 negative.* The job's negative leg, in *Heron JQ Plain* | `pointList` empty, *adaptive points: 0* in `templateReport`, and **no** finding about Orientation. **FAIL:** any Orientation printed for a family with no adaptive points |
| **JQ5** | *A value that must not be carried over.* Read only, in *Heron JQ Adaptive Untouched* | Every placement point's row ends *", Orientation Instance (xyz)"* (or what JQ1 recorded for it), and the finding names points 1 to 4 at that. **FAIL:** any point reading *Host and Loop System*, or any placement point left with no Orientation |
| **JQ6** | *A shape handle point.* [JK7](group-jk.md)'s arrangement - one shape handle point added to *Heron JK Orientation* and kept; read only | Its row ends *", Orientation not read (shape handle point)"*, and the finding says *1 shape handle point(s) not read*. **Record** whether Properties shows an Orientation row for a shape handle point at all |
| **JQ7** | *A pattern-based panel.* A new family from the metric *Curtain Panel Pattern Based* template, read only - the same family as [JK9](group-jk.md) | **Record** whether its placement points read an Orientation, or each says *"not read - Revit gave none: ..."* with Revit's reason. Either is an answer; **FAIL** only on a value Properties does not show |
| **JQ8** | *Routing, on the PC's backend.* *"what orientation are the adaptive points"* through `heron_lookup`, after the store is rebuilt from this commit | It reaches `REPORT_FAMILY_TEMPLATE`, a read - not `SET_ADAPTIVE_POINT_ORIENTATION` and not `PLACE_ADAPTIVE_POINTS`. **Record** where *"which orientation do the adaptive points have"* lands, asked as a question and not: it must never be run as a change. The first sentence is [JK12](group-jk.md)'s too; one look answers both |
| **JQ9** | *The signature, on the owner's word only*, after JQ2, JQ3, JQ4 and JQ5 pass | `python brain/heron_validate.py accept report-family-template --by "Ajmal PS"` and `heron-status` PROVEN in the same change |

A row that fails stays open; nothing in a row changes to make it pass. JQ3 and JQ4 on a second release (2024 or
later) are a [D-131](../decisions/D-131.md) entry after the signature, not part of it.
