# Needs checking — Group PF

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group PF - job methods and a skill's plan, run where Revit is: a plan that stops at a failed step, no write method beside a question, builders work, general notes and the casework template (2026-11-21)

**Tracked as GitHub issue [#1170](https://github.com/Ajmalpshaik/Heron-AI/issues/1170) since 2026-11-21** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** issues [#959](https://github.com/Ajmalpshaik/Heron-AI/issues/959) and
[#960](https://github.com/Ajmalpshaik/Heron-AI/issues/960) were worked on 2026-11-21 in a cloud container with no
Revit. Two fixes were made there and each owes a look on the PC: `prove` in
[`mcp/client/heron_bridge_client.py`](../../mcp/client/heron_bridge_client.py) now stops a plan at the step that
failed ([row 148](../fragment-issues/section-5-rows-126-150.md), `tests/test_prove_plan.py`), and
[`heron_skill.methods_for`](../../brain/heron_skill.py) now holds a method to the question ceiling, so a question is
never named a method that changes the model ([row 5b-650](../fragment-issues/section-5b-rows-626-650.md),
`tests/test_brain_reachable.py`). The other rows are what three job methods were always owed and no group held:
[`builders-work-layout`](../../brain/skills/builders-work-layout.yaml) watched through Heron (row 5b-650),
[`update-general-notes`](../../brain/skills/update-general-notes.yaml) on a real view and a legend
([row 5b-940](../fragment-issues/section-5b-rows-926-950.md)), and what the metric casework template holds of its own
([row 5b-1709](../fragment-issues/row-5b-1709.md), part 2). **Nothing here has run in Revit.**

**The model is a SAVED scratch copy, never a working project.** Every run is rolled back unless the row says it is
kept. Command lines in PowerShell in the Heron folder, with `--session <process id>` when two Revits are connected.

| ID | Do this | Pass looks like |
|---|---|---|
| **PF1** | *A plan that stops* (row 148). Revit 2020 or 2024, a scratch copy with ducts on Level 1. `python mcp/client/heron_bridge_client.py prove filter-elements-by-category count-elements --set categoryName=Ducts --set inViewOnly=true`, with ONE floor selected in Revit first - the setup chain's values, which `filter-elements-by-category` cannot bind, as on 2026-09-19. Then the same with `--set category=Ducts --set "levelId=Level 1"` | First run: `filter-elements-by-category` refused (`needs_request_values`), **`count-elements NOT RUN - filter-elements-by-category failed before it`**, no `count` printed, the last line *"0 ran, 1 failed, 1 not run"*, exit 1. Second run: both ok, count-elements' note *"<- elements from filter-elements-by-category (N)"*, *"2 ran, 0 failed"*. Then the first again with `--keep-going`: count-elements runs, its line says it ran after a step that failed |
| **PF2** | *No write method beside a question* (row 5b-650). On the PC's own backend (`model` - re-pointed 2026-11-26: this row asked for model with the re-ranker, and D-201 removed it): `python tests/test_brain_reachable.py` | Exit 0, and the lines *"under the question ceiling (EXECUTE) it names none above it"* and *"the live lookup, asked as a question, names no method above EXECUTE"* both `ok`. Measured here on the spelling backend only |
| **PF3** | *The builders work layout, watched* (row 5b-650). A saved copy of a project with MEP openings in walls and a layout plan and a section; in a chat with Heron, *"set up the builders work layout"*, the modeller answering each question, Keep or Discard on each change | The host calls `heron_method` for `builders-work-layout` and follows its steps; at the GAP step - **A TAG ON AN OPENING INSIDE A LINK** - it says Heron has no tool and leaves it to the modeller, never working round one; step 5, an opening's depth with its outside face kept, is `SET_PARAMETER_VALUES_BY_ID` with `keepFace=outside` (a GAP until 2026-11-21, [row 5b-1957](../fragment-issues/row-5b-1957.md)) - through try-once while that card is DRAFT, and its own check is [Group FE](group-fe.md); it never syncs; every change one entry in Revit's undo list. **Record** each step that did not do what the method says |
| **PF4** | *Does a category override reach a link?* (row 5b-650, its finding 2). A plan over a linked model: `SET_LINK_GRAPHICS`' card says a category override made in the host *"does not reach inside a link"*, while filters made in the host acted on a link's elements on the first layout. Override one MEP category's lines in the host's Visibility/Graphics, rolled back, and look at the link's elements of that category | **Record** whether the link's elements took the override. They did not: the card holds. They did: the card's sentence is wrong - a new row and its issue |
| **PF5** | *General notes, a view and a legend* (row 5b-940). A saved copy with a drafting or plan view holding numbered general notes (a numbered list and a bulleted one) and a legend holding the same. In a chat, *"update the general notes and keep their numbering"* for one wording in each. `EDIT_TEXT_VALUES` is DRAFT again since its version 10 of 2026-11-20 ([row 5b-1221](../fragment-issues/section-5b-rows-1201-1225.md)): while it is, the edit is try-once, as the method's step 4 says, so the modeller asks for that run | The host calls `heron_method` for `update-general-notes`: collects the notes, reads their wording, shows old and new and asks; edits with `replace` across the view or `set` on ONE note only, never `set` across all; reads again. **Each list's numbering and bullets as before**, on the view and on the legend; one undo takes each edit away |
| **PF6** | *What the metric casework template holds* (row 5b-1709, part 2). New family from Revit's own metric casework template (record its file name and the release), nothing drawn; `REPORT_FAMILY_TEMPLATE`, then select all in the default 3D view and the plan, and read what is there | **Record** every element the template carries besides its reference planes and levels - its category, and its extents. A solid or a form reaching (-405, -600) explains the 45 mm and the box in row 5b-1709: the method then names that content and says to delete or keep it. None: say so, and the row's box is still unexplained |
