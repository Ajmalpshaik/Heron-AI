# Needs checking — Group DG

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-12 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group DG - every tag on an old element made again on the element that replaced it, in every view (2026-10-12)

On 2026-10-12, on Revit 2020, three in-place split AC outdoor units in project 4355-BHVD-3D-60P00-BL006A
were replaced by a loadable family. The old units carried 16 tags of *TCM_TG_HVAC Equipment: Equipment
Tag* across 9 floor plans and sections, all with leaders, and no Heron tool could move them: no Revit
release lets a tag be pointed at another element. [FRAGMENT-ISSUES row 5b-442](../fragment-issues/section-5b-rows-401-425.md).

**What was built - a new capability, after `TAG_ELEMENTS` was weighed and kept apart.**
`COPY_TAGS_TO_REPLACEMENTS` (FRG-VIEW-121) reads a CSV of old id, new id; takes the new elements from a
filter or the selection; finds every tag in the model by what it points at; and makes each one again on
the new element - same view, same type, head, leader, end condition, free end, elbow, orientation and
(2022 on) rotation - reading each back and naming per view what did not hold. It never deletes an old
tag: deleting the old element does that, on the owner's yes.

**Measured without Revit, 2026-10-12.** The tag members it uses were read off `RevitAPI.xml` for all
eight releases (the 2022 split is looked up by name at run time). It compiles on all eight
(`tools/check-fragments-compile.py`), and "copy the tags from the old units to the new units" reaches it
by identity on Revit 2020. **Nothing below has run in Revit.** The arrangement is in
[`copy-tags-to-replacements-2026-10-12.yaml`](../../tools/jobs/copy-tags-to-replacements-2026-10-12.yaml),
read by `batch-prove --dry-run` as 1 WOULD RUN.

**What the code assumes and only Revit can say** - each is reported in the answer rather than refused:
that `IndependentTag.Create` by type works for an equipment tag on Revit 2020, or that the by-category
fallback plus `ChangeTypeId` does; that an elbow written on an ATTACHED leader holds (FRG-VIEW-088
measured that it may not - the end is never freed to force it); that a free end and a head written
after creation stay where they were put; and that a view's collector says whether the new element is
seen there.

Never on 4355-BHVD-3D-60P00-BL006A or any working project. With `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null` on every line.

| # | Run | Look for |
|---|---|---|
| **DG1** | Arrange on **Revit 2020**: a new project from the Mechanical template, saved as **Heron Copy Tags Test**; three Mechanical Equipment instances A, B, C on Level 1; A tagged with an equipment tag in the Level 1 plan (leader, end Attached, an elbow dragged in), again in the same plan with a FREE leader end, and in a section through A with no leader; B and C untagged. The two CSVs in `%TEMP%\heron-work\copy-tags\` with the ids read off Manage > IDs of Selection | `pairs-positive.csv` is `Old,New` then `<A>,<B>`; `pairs-negative.csv` is `Old,New` then `<C>,<B>` |
| **DG2** | `python tools/batch-prove.py tools/jobs/copy-tags-to-replacements-2026-10-12.yaml`, PROJECT-TITLE filled | positive: `made` 3 - two in the plan, one in the section. negative (C, no tags anywhere): `made` 0, `madeTags` empty, `oldWithNoTags` naming C, and `report` saying C has no tag on it anywhere so nothing was made. Look at the views afterwards - B untagged: rollback has failed on this PC before (BD1) |
| **DG3** | By hand through `revit_change`, DG2's positive kept | each new tag on B: Properties shows A's tag type; its head on A's tag's head; Leader on in the plan and off in the section; the free one's end on the same point; **record whether the attached elbow held** - `report` names it if not. A's three tags still there |
| **DG4** | The same `revit_change` again, straight after | `made` 0 and `alreadyThere` naming all three of A's tags - nothing doubled |
| **DG5** | The refusals: undo DG3 with one Ctrl+Z, hide B in the section (Hide in View > Elements) and run the positive again; a CSV line `<A>,<A>`; a pair whose new id is a duct not handed in | the section tag in `refused` with "not seen in this view"; `badRows` 1; `rowsUnmatched` 1; nothing made for them |
| **DG6** | The second route: delete A on the test copy | A's tags go with it; the tags on B stay, in every view |
| **DG7** | DG2 and DG3 on **Revit 2024** and **2027**, the arrangement made again from that release's template | the same answers, and on 2024 a tag with a rotation angle keeps it. Compiled on all eight releases; what Revit DOES was measured on none |
| **DG8** | The signature, on the owner's word only, after DG2, DG3 and DG6 | `python brain/heron_validate.py accept copy-tags-to-replacements --by "Ajmal PS"` |
