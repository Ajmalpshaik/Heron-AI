# Needs checking — Group CT

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-07 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group CT - groups in the family being edited: GROUP_ELEMENTS version 2 (2026-10-07)

`GROUP_ELEMENTS` (FRG-ELE-012) version 2 groups the selected elements in a project, as version 1 did, OR in
the family open in the Family Editor, where version 1 was refused every time and never said why
([row 5b-362](../fragment-issues/section-5b-rows-351-375.md)). New in it: a `name` value - the group type's
name, EMPTY for Revit's own "Group 1"; a name another group type already has, or one with a character Revit
forbids, refused before anything is made; `refusalReason` in plain words, Revit's own message quoted;
`groupName`; and `findings` saying where it grouped and what Revit added. The group and its name are one
sub-transaction, so a name Revit refuses takes the group back with it.

Version 1's proof (2026-09-13, test projject, Revit 2024) is in git history; version 2 changes `impl/`, so it
is DRAFT. **Compiled 2020 to 2027** (`tools/check-fragments-compile.py`, every fragment ok, 2026-10-07).
**Nothing below has run.** No add-in change - fragment source is live, so no redeploy and no Revit restart
for a run from this branch. NEVER on Family1 or any of the owner's saved families: a scratch family made
for it.

**THE ARRANGEMENT - made KEPT, once, in a new scratch family.** `CREATE_FAMILY_DOCUMENT` from *Metric
Generic Model* gives a family; with it in front, and `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>`
and `< /dev/null` on every line, four boxes in a row along X - A and B on the left, C and D on the right:

```
python mcp/client/heron_bridge_client.py fragment create-family-extrusion --write --apply --set "workPlane=Ref. Level" --set "profile=rect -500,-50 -400,50" --set startMm=0 --set endMm=200 --set solid=true
python mcp/client/heron_bridge_client.py fragment create-family-extrusion --write --apply --set "workPlane=Ref. Level" --set "profile=rect -250,-50 -150,50" --set startMm=0 --set endMm=200 --set solid=true
python mcp/client/heron_bridge_client.py fragment create-family-extrusion --write --apply --set "workPlane=Ref. Level" --set "profile=rect 150,-50 250,50" --set startMm=0 --set endMm=200 --set solid=true
python mcp/client/heron_bridge_client.py fragment create-family-extrusion --write --apply --set "workPlane=Ref. Level" --set "profile=rect 400,-50 500,50" --set startMm=0 --set endMm=200 --set solid=true
```

Then A and B selected by hand (Ctrl+click in the 3D view) and grouped KEPT under the name the negative
case collides with:

```
python mcp/client/heron_bridge_client.py fragment group-elements --write --apply --set "name=Taken name"
```

Every run below is rolled back (`validate --write`). C and D are chosen by region - written *REGION* below:
`--setup select-in-region --setup set-selection --set "minMm=100,-100,-10" --set "maxMm=600,100,300"
--set exact=false --set "categories=Generic Models"`, and a `--negative-set` repeats those four values.
Whether `select-in-region` finds a family's forms by its category has not been measured: if it finds none,
select C and D by hand and leave REGION out.

| # | Run | Look for |
|---|---|---|
| **CT1** | `validate --in "<family>" --write REGION --set "name=Bracket parts" --negative-set "name=Taken name" group-elements` | `grouped` 2 or more - more only if Revit added a sketch or sketch plane, and then a finding names each kind; `groupName` "Bracket parts"; `refused` false; `findings` names the family and the lighter routes; the group ROLLED BACK |
| **CT2** | the negative half of CT1: `name=Taken name` | `refused` true; `refusalReason` names "Taken name" as already used; `groupId` null, `grouped` 0, `groupName` empty - no "Group 1" left behind |
| **CT3** | CT1 with `name=` EMPTY | `groupName` is Revit's own; RECORD which ("Group 1", or the next free number) |
| **CT4** | REGION narrowed to C alone: `minMm=100,-100,-10`, `maxMm=300,100,300` | refused - only one element; nothing grouped. Version 1's own negative, now in a family |
| **CT5** | `name=Parts:A` | refused before anything is made; the reason lists the characters Revit forbids in a name |
| **CT6** | `name=taken name` - the kept group's name in other letters | RECORD what Revit does. Either it groups as "taken name", or it refuses the name INSIDE the sub-transaction: then `refusalReason` quotes Revit, says the group was taken back, and nothing is left. The only row that reaches the sub-transaction's rollback |
| **CT7** | the arrangement's kept "Taken name" run, read back | RECORD `grouped` and `findings` - did Revit add a sketch or a plane? - and Revit's warnings in the family: NO "group changed outside group edit mode". The model is regenerated before grouping for exactly that remark of Revit's |
| **CT8** | THE PROJECT CASE: `python tools/batch-prove.py tools/jobs/test-projject-wall-rig-2026-09-13.yaml --only group-elements --session <pid>` on test projject | version 1's answers again - grouped 4 / refused false, then grouped 0 / refused true - now with `refusalReason` saying only one element was given, and `groupName` Revit's own. The job passes `name` empty since 2026-10-07 |
| **CT9** | the scratch family after CT1 to CT6 | ONE group, "Taken name" - no "Bracket parts", no "Group 1". Look, do not assume: a rename has survived a reported rollback before ([row 19](../fragment-issues/section-5-rows-001-025.md), OPEN), and this tool renames a group type |
| **CT10** | the signature | `python brain/heron_validate.py accept group-elements --by "Ajmal PS"` on the CT1/CT2 record, on the owner's word only |
| **CT11** | the second route | the Family Editor's Project Browser > Groups, and Edit Group on the kept group, to see its members |
| **CT12** | CT1 on **Revit 2020** and **2027** | the same answers. Compiled on all eight releases; not run there |

**Routing, measured 2026-10-07 on private stores, main at 6aec4773 against this branch, on the trained model and the spelling one - not proof.** The owner's kind of sentence now reaches it: *group these forms in this family*, *group these parts in the family editor*, *make the small parts one group* and *make a model group* by identity; *group the selected extrusions*, *there are lots of small parts in this family, group the related ones*, *group the small parts of this family* and *name the group Bracket parts* by ranking. On main those went to `REPORT_FAMILY_FORMS`, `COMBINE_FAMILY_FORMS`, `SELECT_GROUP_MEMBERS`, `GROUP_BY_ASSEMBLY`, `SELECT_SUBCOMPONENTS`, `FIND_UNUSED_GROUP_TYPES` or `ADD_FAMILY_TYPE_PARAMETER`. Questions about what a group holds - *what is in this group*, *what is inside this group*, *list the members of this group*, *show me what is in the group* - still reach `SELECT_GROUP_MEMBERS`, and *repeat this part many times in the family* still reaches `ARRAY_FAMILY_FORMS`: a first wording of the purpose took that one, and was cut. **Ripples, each between two writers neither of which does the job:** *make a family from these elements* moves from `SET_FAMILY_CATEGORY` to here on the trained model, and *group these by level* from `SET_SCHEDULE_SORT_GROUP` to here on the spelling one. *count these grouped by type* moved the other way, from `SET_SCHEDULE_SORT_GROUP` to the read `GROUP_AND_COUNT`. `check-routing`: the same exit, no routing-table claim lost, *group these* no longer contested; on the words route alone, *what is in this group* - `SELECT_GROUP_MEMBERS`'s own sentence, answered by identity - now has this card first in place of another, and *count these elements by room* is a new contest between two other cards. `check-intrusion`: the same exit, and this card is not among the most intrusive.
