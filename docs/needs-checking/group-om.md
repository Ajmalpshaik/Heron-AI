# Needs checking — Group OM

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group OM - moving one filter up or down a view's filter list: `REORDER_VIEW_FILTERS`, DRAFT until proved (2026-11-19)

Asked for on Revit 2020 on 2026-11-19: a Duct Accessories filter set green, moved to the TOP of a view's
filter list, under two filters that also cover Duct Accessories and set black lines - where two filters set
the same thing on the same element, the higher one wins. No capability reordered a view's filters.

[`reorder-view-filters`](../../brain/fragments/reorder-view-filters/fragment.yaml) **version 1, DRAFT.**
Revit has no member that SETS the order on any release, so a move takes filters off the view and puts them
back, each one's row - overrides, Visibility, and from 2021 Enable Filter - read first, written back and
read back. It compiles on every release it claims (`tools/check-fragments-compile.py`, 2026-11-19, all
eight). **It has not run in Revit.** Nothing in the add-in changed: the card travels as source from the
checkout.

**What each release lets it check, and what these rows test:** from 2021 the order is Revit's own
(`GetOrderedFilters`), read before and read back after. On 2020 the API cannot read the Filters tab's order,
so the order before is the modeller's typed reading (`currentOrder`) and the order after is written, never
read back. Two things are stated only in the 2021-and-later references - that the plain list `GetFilters`
gives is sorted by id, and that `AddFilter` puts a filter at the BOTTOM - so on 2020 both are **assumed**
until OM10 measures them.

**The models are scratch copies of Revit's own samples, never a working model** - Revit 2024's
`Snowdon Towers Sample HVAC.rvt` copied under `%TEMP%\heron-work` (the job file's model), and for the 2020
rows a copy of a Revit 2020 sample (`rac_basic_sample_project.rvt` or the Systems template's sample) with
three or more filters put on one plan by `APPLY_VIEW_FILTER`. Every write below is rolled back -
`heron_bridge_client.py validate reorder-view-filters --write ...` - unless the row says kept. **This card,
DRAFT, has ONE kept run for version 1: `try-once`, on the modeller's own ask in the chat, on a saved scratch
copy with the view in front ([D-119](../decisions/D-119.md), [D-122](../decisions/D-122.md)) - OM8 takes it,
because on Revit 2020 the Filters tab by eye is the only check of the order there is.** `APPLY_VIEW_FILTER`
is PROVEN and may be kept through `revit_change` to arrange a row, in the scratch copy only.

| ID | Do this | Pass looks like |
|---|---|---|
| **OM1** | *Arrange, Revit 2024, nothing written.* On the Snowdon scratch copy, `REPORT_VIEW_FILTERS` with `viewName` = `FloorPlan: L2`. Write down the four filters in the order it numbers them, each one's row, and whether its heading says a view template controls them. From the same reply's project list, write down one filter that is in the project but NOT on L2. Open Visibility/Graphics > Filters on L2 and compare | The same four, in the same order, in the reply and in the Filters tab, and **no view template controlling L2's filters** (if one does, every positive here is refused - choose another plan and change the job file). Put the **row-4** name into `filterName` in `tools/jobs/reorder-view-filters-snowdon-2026-11-19.yaml` (it carries `Domestic`, the one name recorded on that view, as a placeholder) |
| **OM2** | *Positive, Revit 2024, rolled back.* `python tools/batch-prove.py tools/jobs/reorder-view-filters-snowdon-2026-11-19.yaml` | Positive: `moved true`, `reAdded 3`, `orderVerified` "read back from Revit", `orderBefore` OM1's order, `orderAfter` the row-4 filter first and the other three in OM1's order. After the run the Filters tab still shows OM1's order - the write was rolled back |
| **OM3** | *Negative, Revit 2024 - the same run's second leg.* `filterName` = `ZZZ NOT ON THIS VIEW`, a name no filter in the project carries | `moved false`, `orderBefore` and `orderAfter` EMPTY, `reAdded 0`, `refused` *"No filter called 'ZZZ NOT ON THIS VIEW' in this project. Nothing was changed."* with the four filters on L2 listed top to bottom. **A returned refusal, not a throw** - a thrown negative is not a proof. Verdict PASS |
| **OM4** | *Negative, Revit 2024 - a filter in the project but not on the view, rolled back.* `filterName` = the filter OM1 wrote down as not on L2, `view` = `FloorPlan: L2` | Refused *"The filter '...' is in this project but is not on 'L2' (FloorPlan), so there is nothing to move"*, the four on L2 listed, and `APPLY_VIEW_FILTER` named as the tool that puts a filter on a view. `moved false`, both order lists EMPTY, `reAdded 0` |
| **OM5** | *An override and an unticked Enable Filter survive the move - THE FIRST POSITIVE TO CHECK. Revit 2024.* With `APPLY_VIEW_FILTER` kept in the scratch copy, give the filter OM1 read in **row 2** a red projection line (`overrides` = `projection-line-colour=255,0,0`) and `enabled=false`; read it back with `REPORT_VIEW_FILTERS`. Then `REORDER_VIEW_FILTERS` on L2, `filterName` = the row-4 filter, `position` = `top`, rolled back | `moved true`, `reAdded 3`, and **no throw** - the row-2 filter is one of the three taken off and put back, and the fragment's own read-back compares its overrides, Visibility and Enable Filter against what they were before and stops the call if any differs. `orderAfter` shows it in row 3. A throw naming a projection line property or *"Enable Filter"* is the harm this tool could do quietly, caught - a FAIL, written down word for word |
| **OM6** | *A view whose template owns its filters. Revit 2024.* Give a scratch plan a view template that includes V/G Overrides Filters, and ask for any move on that plan. Then ask the same move with the template's name as `view`. Then untick V/G Overrides Filters in that template's Include column (kept, scratch copy) and ask the first move on the plan again. All three rolled back | First: refused, naming the template and saying to ask again with the template as the view - nothing written. Second: `moved true` and a finding naming how many views follow the template. Third: `moved true` on the plan itself - the include list is read FROM THE TEMPLATE ([row 5b-1854](../fragment-issues/row-5b-1854.md)); a refusal here is that defect |
| **OM7** | *Revit 2020, NO `currentOrder`, three or more filters.* On the 2020 scratch plan, `position` = `top` on the bottom filter and `currentOrder` left out, rolled back. Then the same with `position` = `bottom` on the top filter | First: **refused** - Revit 2020 cannot tell Heron the order in the Filters tab, so moving one would put the others in an order nobody chose; it asks for `currentOrder`. Nothing written. Second (a move to the bottom needs no reading): `moved true`, `reAdded 1`, `orderBefore` EMPTY - never the API's id order - `orderVerified` "written, not readable on Revit 2020" |
| **OM8** | *Revit 2020, WITH `currentOrder`, checked by eye - this card's one kept run.* Save the 2020 scratch copy and bring its plan to the front. Read the Filters tab top to bottom. On the modeller's ask: `python mcp/client/heron_bridge_client.py try-once reorder-view-filters --view "<the plan>" --set "view=<the plan>" --set "filterName=<the bottom filter>" --set position=top --set "currentOrder=<the tab's names, top to bottom, separated by commas>"` - the modeller presses **Keep**. Open the Filters tab | The Filters tab shows the moved filter in row 1 and the others in the order typed, each row looking as it did. `orderVerified` "written, not readable on Revit 2020". **Write down what the tab shows** - it is the only check of the order on this release. A Discard leaves nothing behind and the run is not repeated for this version |
| **OM9** | *Revit 2020, a `currentOrder` that does not name the view's filters.* Leave one filter out of `currentOrder`; then type one twice. Both rolled back | Each refused, naming the filter missing or named twice. Nothing written |
| **OM10** | *Revit 2020 probe - does the API's id order ever match the Filters tab, and does `AddFilter` append at the bottom?* On a fresh 2020 scratch plan carrying no filters, using three filters that already exist in the project: (1) **rolled back, in one call** - a scratch C# probe from the session's scratchpad (never a DRAFT applied by raw bridge) that adds the three NEWEST first and OLDEST last, out of id order, and returns `GetFilters()` in the order Revit gives it; (2) then the same three added the same way with `APPLY_VIEW_FILTER`, one call each, **kept in the scratch copy only**, and `REPORT_VIEW_FILTERS` (which on 2020 lists them as Revit returns them) read beside the Filters tab | **Write down all three orders.** The tab showing the order they were added (newest first) while the API gives oldest first confirms the API cannot be trusted for the order on 2020 - OM7's refusal is needed. The API giving the order added would mean 2020 CAN read it, and the card's 2020 half is stricter than it needs to be - a question for the owner, not a change made here. The tab showing each added filter at the BOTTOM confirms the assumption every 2020 move rests on; a filter appearing anywhere else means the 2020 half of the card is wrong and must refuse every 2020 move until mended |

**The proof is OM2 and OM3**, read with OM4 and OM5. Read `brain/proof-drafts/reorder-view-filters.yaml`
and the findings it quotes - the runner cannot read a sentence - then sign on the owner's word only:
`python brain/heron_validate.py accept reorder-view-filters --by "Ajmal PS"`. The second route is
Visibility/Graphics > Filters on the view, and `REPORT_VIEW_FILTERS` on it. **A Revit 2020 proof entry
(`proof.releases`) needs OM7 to OM10 first** - the request that started this was on Revit 2020.
