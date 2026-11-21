# Needs checking — Group PN

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group PN - REPORT_CATEGORY_OVERRIDES version 6: a whole-dialog read that failed says NOT READ, never "shown" or "no override" - DRAFT until proved (2026-11-21)

**Tracked as GitHub issue [#1221](https://github.com/Ajmalpshaik/Heron-AI/issues/1221) since 2026-11-21** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** [row 5b-1959](../fragment-issues/row-5b-1959.md), issue [#1207](https://github.com/Ajmalpshaik/Heron-AI/issues/1207),
[D-52](../DECISIONS.md) - a failed read is never a no. In version 5's whole-dialog read (`allCategories` true) a
category whose hidden state Revit would not give read *"shown"*, one whose override would not read *"no override"*,
one whose dialog row would not read *"not a row of this view's Visibility/Graphics"*, and an element whose own override
would not read was left out of *"No element ... carries its own override"* - the same words as a plain one.
[`REPORT_CATEGORY_OVERRIDES`](../../brain/fragments/report-category-overrides/fragment.yaml) **version 6** writes
*"NOT READ - Revit says: ..."* on such a row (a throw, or an override Revit gives back as nothing), lists the row
whatever else it holds, names a category whose kind or sub-categories will not read, and counts what was not read in
a sentence after the first line of `modelCategoryList`, `subCategoryList`, `annotationCategoryList` and
`elementOverrideList` (an element with its ids) - its totals count only what WAS read, and the sentence rides only on
an answer with one, so an answer with none reads as version 5's. Built in a cloud container with no Revit: it compiles for 2020 to
2027 (`tools/check-fragments-compile.py`) and **has met no model**. Its C# moved, so it is **DRAFT** until this group
runs and Ajmal PS signs (D-30). Rebuild and restart Revit first (`python tools/heron-ready.py`).

**Run it after [Group PK](group-pk.md)'s PK7** (version 5), on the same scratch copy of P1102 MEP in
`%TEMP%\heron-work\pk`, Revit 2020 - the card's first release - never a working model. The lines below run in
PowerShell in the Heron folder with `HERON_CLIENT_ID=ajmal-pc` and `< /dev/null`, and `--session <process id>` when two
Revits are connected. Read the document name on every reply.

| ID | Do this | Pass looks like |
|---|---|---|
| **PN1** | *Negative first - the plan PK7 used, with NOTHING overridden at category level, `allCategories` true.* The PN1 line | `modelCategoryList` lists only categories with host elements in the view, each *"V/G: no override"* - check three of them in Visibility/Graphics, Model Categories tab, as plain; `subCategoryList` and `annotationCategoryList` read 0; `elementOverrideList` *"No element in '<plan>' carries its own override"*. **No "NOT READ" anywhere and no "would not give all of their Visibility/Graphics" sentence** while every read came back. A row reading NOT READ here fails the check unless Revit's own words beside it say why, and those words are written down |
| **PN2** | *Positive - the whole dialog of a view that overrides a great deal: Group ED's plan, the one that overrides 77 of 85 model categories for the linked architecture, `allCategories` true.* The PN2 line | Every row as version 5 gave it where its reads came back. **Write down how many rows say NOT READ** - it is the measurement row 5b-1959 asked for, whether Revit ever refuses a row of an ordinary plan. For each one: Revit's words written down, the category checked in Visibility/Graphics, the sentence after the section's first line counting exactly those rows, and the hidden and overridden totals NOT counting what Revit would not give. Zero is a pass and is the expected answer - RevitAPI.xml gives `GetCategoryOverrides` one throw beyond a bad id, for a view type the block's caller already refuses. **A zero leaves the NOT READ path itself not run on any model**: say so in the proof's words, so a signature here is read as "unchanged where every read came back", never as "a failed read seen to say NOT READ" |

### The command lines, by row

**PN1** - the plan with nothing overridden:

```text
python mcp/client/heron_bridge_client.py fragment report-category-overrides --set "view=<the plan>"
  --set "allCategories=true"
```

**PN2** - Group ED's plan:

```text
python mcp/client/heron_bridge_client.py fragment report-category-overrides --set "view=<Group ED's plan>"
  --set "allCategories=true"
```

### Results

Nothing run yet.
