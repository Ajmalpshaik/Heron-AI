# Needs checking — Group AX

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-27 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group AX - `create-view-filters-by-rule` and `set-view-filter-categories`: named view filters from a parameter name and a category list, and their categories set afterwards (2026-09-27)

**Two fragments from one sitting on 2026-09-27, in Project1 on Revit 2024, and both are DRAFT.**
[`create-view-filters-by-rule`](../../brain/fragments/create-view-filters-by-rule/fragment.yaml) -
CREATE_VIEW_FILTERS_BY_RULE, FRG-VIEW-111 - creates one or more rule-based view filters in the project's
Filters dialog: each has its own name, all share the same categories, and each has one rule "parameter equals
value". None of them is put on a view. The parameter is typed by the name the Filters dialog shows, and every category is
checked against it before anything is created. It compiles on every release from 2020 to 2027. It has run in
a chat on the owner's working model, and **a chat run is not a proof**: no fingerprint was taken and nothing
was rolled back ([D-30](../DECISIONS.md)), so no row below is closed by it.

**Why it exists.** The owner asked for three filters - Supply Air, Return Air and Exhaust Air, each on
System Classification, for Air Terminals, Ducts, Flex Ducts, Duct Fittings and Duct Accessories, created and
put on no view. `CREATE_VIEW_FILTER` takes its parameter as an id that cannot be typed by name, and needs
a view and a colour, so it always applies the filter. `CREATE_VIEW_FILTERS_BY_VALUE` takes its values and
categories from elements handed to it: the model's 90 air terminals carry only Supply Air and Return Air,
so it could never make the Exhaust Air filter.

**The working run, 2026-09-27 - a chat run, not a proof.** "heron ai bulding", Revit 2024, Heron session
46596, Changes on. Three `revit_change` calls, each with every value typed and nothing selected:

1. The five categories **plus Duct Systems**, run as the negative case on the owner's word that Revit will
   not take Duct Systems with this rule. **Revit took it.** `GetFilterableParametersInCommon` lists System
   Classification for Duct Systems, and `ElementFilterIsAcceptableForParameterFilterElement` passed, so all
   three filters were created and **kept** with six categories. Read back from Revit: *Air Terminals, Duct
   Accessories, Duct Fittings, Duct Systems, Ducts, Flex Ducts; rule System Classification equals 'Supply
   Air'*. The same for Return Air and Exhaust Air. They catch 45, 45 and 0 elements today, all of them Air
   Terminals. The rule is on built-in parameter id -1140325. **This is not what the owner asked for.**
   The session did not undo it, because Heron's rule is that undo is the modeller's own Ctrl+Z, and
   nothing in the library edited a filter's categories ([row 5b-241](../FRAGMENT-ISSUES.md)).
2. Air Terminals, Ducts and **Walls**, with the three names that now exist, so a failed check still
   could not create anything. `cannotFilter` 1: *Walls - it cannot be filtered by 'System
   Classification'*. `created` 0, and `refused` began *"NOTHING WAS CREATED"*.
3. The five categories asked for. `alreadyExists` 3, `created` 0, and `refused` said the three were
   *"left exactly as they were, not overwritten"*.

**The owner's decision, relayed by the chat he was working in: fix it through Heron, not by hand.**
He asked for no Duct Systems, and for every other duct category. So a second fragment was built in the same
change: [`set-view-filter-categories`](../../brain/fragments/set-view-filter-categories/fragment.yaml) -
SET_VIEW_FILTER_CATEGORIES, FRG-VIEW-112. It sets an existing filter's category list by the filter's name,
keeps its rule, and touches no view. A category the rule cannot apply to is left out and named, and the
rest are still set. It compiles on every release from 2020 to 2027. Three more `revit_change` calls, the
same model and session:

4. `filterNames=ZZ No Such Filter Heron` - `notFound` 1, `changed` 0, and `refused` *"none of the names is a
   rule-based filter in this project - nothing was changed"*.
5. The three names, with Air Terminals, Ducts, Flex Ducts, Duct Fittings, Duct Accessories, Duct
   Insulations, Duct Linings and Duct Placeholders. `changed` 3, `leftOut` 0, and **kept**. Read back from
   Revit, each: *Air Terminals, Duct Accessories, Duct Fittings, Duct Insulations, Duct Linings, Duct
   Placeholders, Ducts, Flex Ducts; rule System Classification equals* its own name. Duct Systems is gone.
   They still catch 45, 45 and 0 Air Terminals.
6. The same call again - `alreadySet` 3, `changed` 0.

A `revit_read` of REPORT_VIEW_FILTERS then returned three filters in the project, checked against 22 views,
and all three unused - on no view, as asked. Mechanical Equipment was left out on the owner's word, not
by either tool: it is not a duct item, and it can sit on more than one system.

**The measuring mistake is the session's, and it is written down so it is not repeated:** a negative whose
outcome is in doubt is measured without keeping it -
`python brain/heron_validate.py` without `--write`, or `batch-prove` with its rollback - never through
`revit_change`, which keeps what it does.

| # | Check | Expected |
|---|---|---|
| **AX1** | **On the WORKING model, by eye.** Manage, Filters: select Supply Air, then Return Air, then Exhaust Air | Each has Air Terminals, Duct Accessories, Duct Fittings, Duct Insulations, Duct Linings, Duct Placeholders, Ducts and Flex Ducts ticked - **not** Duct Systems - and the rule *System Classification equals* its own name. None is in any view's Visibility/Graphics Filters tab. This is what calls 5 and 6 read back; the dialog is the second route for both fragments |
| **AX2** | The two legs ([D-30](../DECISIONS.md)) - **a draft, not yet signed.** On a TEST COPY, with `HERON_CLIENT_ID=ajmal-pc` set and the copy in front: `python tools/batch-prove.py tools/jobs/create-view-filters-by-rule-project1-2026-09-27.yaml --dry-run`, then the same without `--dry-run`. **Do not sign yet** - the draft's `second_route` reads NOT ESTABLISHED until AX3 is done, and `accept` does not refuse that | `PASS`. The positive: `created` 2, and `readBack` naming the five categories and each rule, catching 45 Air Terminals each on a copy of the model as it stood on 2026-09-27. The negative: `created` 0, and `cannotFilter` naming Walls. `POSITIVE EMPTY` with `alreadyExists` filled means the copy already held the HERON PROOF names. Afterwards Manage, Filters holds neither HERON PROOF filter - both legs are rolled back |
| **AX3** | The second route, THEN the signature. Revit's own Manage, Filters dialog, on the working model after AX1, or on the test copy after the positive leg is run and KEPT in a chat. Write what was seen into the `second_route` field of the draft that `brain/proof-drafts/` holds for `create-view-filters-by-rule`, with the model and the date. Only then sign it with `python brain/heron_validate.py accept create-view-filters-by-rule --by "Ajmal PS"` and set `heron-status: PROVEN` | The dialog shows the categories ticked and the rule exactly as `readBack` reported them. **Read on screen, not inferred from `created`** |
| **AX4** | Whether capitals matter on 2023 and later. On a test copy, in a chat: the five categories, `filterNames=HERON CASE`, `parameterName=System Classification`, `matchValues=supply air` - lower case | Either catches the same 45 as "Supply Air" (capitals do not matter) or 0 (they do). **Write down which**: the three-argument rule used on 2020 to 2022 is passed "not case sensitive", so a 0 here would mean the same filter catches different elements on different Revit releases |
| **AX5** | The two legs for `set-view-filter-categories` - **a draft, not yet signed.** On a TEST COPY saved AFTER call 5 above, so it holds the three filters: `python tools/batch-prove.py tools/jobs/set-view-filter-categories-project1-2026-09-27.yaml --dry-run`, then the same without `--dry-run`. Sign only after AX1 is written into the draft's `second_route` | `PASS`. The positive: `changed` 2 and `readBack` naming Air Terminals and Ducts only, for Supply Air and Return Air. The negative - Walls alone: `changed` 0, `leftOut` naming Walls, and `refused` saying each was left as it was, never emptied. `POSITIVE EMPTY` with `notFound` filled means the copy is older than the filters. Afterwards Manage, Filters shows the eight categories again - both legs are rolled back |
