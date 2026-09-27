# Needs checking — Group AY

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-27 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group AY - `offset-tags-from-host`: every tag moved a set paper distance off its own element (2026-09-27)

**One fragment from one sitting on 2026-09-27, in "heron ai bulding" on Revit 2024, and it is DRAFT.**
[`offset-tags-from-host`](../../brain/fragments/offset-tags-from-host/fragment.yaml) -
OFFSET_TAGS_FROM_HOST, FRG-VIEW-113 - moves each tag handed in to so many paper millimetres right and up of
ITS OWN element, and switches its leader on (end attached), off, or leaves it. Air terminal, duct, pipe,
equipment, door and window tags, and room, space and area tags; which tags, how far and the leader are all
values. It compiles on every release from 2020 to 2027. It has run in a chat on the owner's working model,
and **a chat run is not a proof**: no fingerprint was taken and nothing was rolled back
([D-30](../DECISIONS.md)), so no row below is closed by it.

**Why it exists.** On FloorPlan "Level 1 - Air Terminal Layout" (1:75, sheet M-104) the 90 air terminal tags
sat on top of their diffusers after the tag family was edited. Every tag fragment in the library decides the
position by its own rule - spread apart, stacked, one point for all, room centre - and none takes a distance
from each tag's own element. The owner asked for it general: *"when you are creating something it should be
useful for the next thing also"*.

**The working run, 2026-09-27 - a chat run, not a proof.** "heron ai bulding", Revit 2024, Heron session
46596, Changes on. Each call was `revit_change` SELECT_BY_CATEGORY_NAME `categoryName=Air Terminal Tags`,
`inViewOnly=FloorPlan: Level 1 - Air Terminal Layout` (90 found), then OFFSET_TAGS_FROM_HOST with
`expect_from` naming it, `offsetRightMm=8`, `offsetUpMm=6`, `addLeader=true`:

1. `tagCategories=Duct Tags` - the negative. `moved` 0, `otherCategory` 90, nothing else in any list.
   M-104's viewport box centre (311.78, 303.18) mm before and after.
2. `tagCategories=Air Terminal Tags` - `moved` 90, `leadersOn` 90, every head read back within 0 mm of its
   target on paper, `fromBoxCentre` 0, nothing refused. M-104 (311.78, 303.18) mm before and after -
   **it did not move**, so the put-back was not needed. **Kept.**
3. The same again - `moved` 0, `alreadyThere` 90, `leadersOn` 90, M-104 (311.78, 303.18) mm. This is the
   read-back: the target is measured from each diffuser, so a second run finds every tag there.

Then `revit_read` REPORT_TAGS_AND_TARGETS on the same 90: `tagCount` 90, `tagTargets` 90, `orphanTags` 0,
`multiTags` 0 - every tag still points at one element. **The viewport did not shift in any of the three
runs**, although two earlier tag changes in this view moved it 30 and 43 mm the same day; what moved it then
is not known, and the put-back branch has never run on a model (AY3).

Every row runs on a TEST COPY of "heron ai bulding" - never the working model.

| # | Check | Expected |
|---|---|---|
| **AY1** | The two legs ([D-30](../DECISIONS.md)) - **a draft, not yet signed.** With `HERON_CLIENT_ID=ajmal-pc` set and the test copy in front, `python tools/batch-prove.py tools/jobs/offset-tags-from-host-heron-ai-bulding-2026-09-27.yaml --dry-run`, then the same without `--dry-run`. The positive asks for 10 and 4 mm, not the 8 and 6 the copy already has, so every tag has to move. **Do not sign yet** - the draft's `second_route` reads NOT ESTABLISHED until AY2 is done | `PASS`. The positive: `moved` 90, `leadersOn` 90, `report` saying every head within 0 mm. The negative - `tagCategories=Duct Tags` on the same 90: `moved` 0, `otherCategory` 90. `viewportKept` the same before and after on both legs. Afterwards the tags are back at 8 and 6 - both legs are rolled back |
| **AY2** | The second route, THEN the signature. On the WORKING model after the chat run: pick three diffuser tags on the plan and read Leader (checked) and Leader End Condition (Attached) in Properties, and measure one head from its diffuser with Revit's Measure tool on the sheet. Write what was seen into the `second_route` field of the draft `brain/proof-drafts/` holds for `offset-tags-from-host`. Only then `python brain/heron_validate.py accept offset-tags-from-host --by "Ajmal PS"` and set `heron-status: PROVEN` | Leader on, end Attached, and the head 8 mm right and 6 mm up of the diffuser on paper - **read on screen, not inferred from `moved`** |
| **AY3** | The viewport put-back. On a test copy, turn M-104's view Crop Region OFF, then in a chat run SELECT_BY_CATEGORY_NAME and OFFSET_TAGS_FROM_HOST with `offsetRightMm=60` so the tags reach past the plan's edge and the box has to grow | `viewportKept` either unchanged, or "PUT BACK" with both positions. **Read the sheet**: whether putting the box centre back is right when the box changed SIZE is the open question - the plan itself may then sit off by half the growth |
| **AY4** | Other tag kinds, one chat run each on a test copy: duct tags (`addLeader=keep`, measured from the duct's middle), door tags, and room tags (`tagCategories=Room Tags`, `addLeader=true`) | Each moved, `fromBoxCentre` 0, nothing in `notSupported`; the room tags' leaders on and their end still inside the room - **a room tag has no end condition**, so `leaderNotAsAsked` stays empty |
