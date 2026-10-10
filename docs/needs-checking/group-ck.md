# Needs checking — Group CK

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone.
> **The register's rules, and every group's place in it, are on that page.** A new row for this group
> goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads
> it back into the register for every tool that reads the register.

## Group CK - a project's answers kept under the model's own id, and the template's id asked about (2026-10-06)

**Built 2026-10-06 for [FRAGMENT-ISSUES 5b-324](../fragment-issues/section-5b-rows-301-325.md) under
[D-113](../decisions/D-113.md). Tested without Revit - [`test_document_pin.py`](../../tests/test_document_pin.py),
[`test_earlier_answers.py`](../../tests/test_earlier_answers.py) and the 5b-324 section of
[`test_mcp_serves.py`](../../tests/test_mcp_serves.py) - and NOT YET RUN IN REVIT.** What was measured in
Revit before it was built is in D-113: one read of `Document.CreationGUID` on *Heron loads test*. **Before
any row: the add-in rebuilt and deployed to every installed release, Revit restarted, and the Claude app
restarted** - the new id travels from the add-in, and an add-in older than this sends none. Every row
reads or keeps answers in Heron's own knowledge folder; **none writes to a model.** The owner's files under
`%APPDATA%\Heron\knowledge\projects\` named `8764c510-57b7-44c3-bddf-266d86c26380-0000c160*` are only ever
read: write down their dates and sizes before CK1 and compare after CK3.

| # | Run | Look for |
|---|---|---|
| **CK1** | **The model that found it** - Revit 2024, *Heron loads test* open. Ask Heron to count the ducts, then *"calculate the loads for this building"* | The chat's answer begins **EARLIER ANSWERS - NOT USED HERE** (the Companion's Loads panel shows the same question in its result line after a Recalculate - it does not print the opening answer), listing *Project2* (4 HVAC standards, its load runs, room profiles Office and Reception) and *Heron loads test* (the runs of 2026-10-06), and saying none of it was used. The loads questions are asked - design weather, set points, profiles - and no *Reception* profile appears unless he gives one. **Negative:** the knowledge folder gains nothing named `8764c510...` and every such file has its old date and size |
| **CK2** | **His answer** - he says which name, if any, is *Heron loads test*, and Heron is asked to use it (`heron_earlier_answers`) | A name: *"Copied what was kept for ..."*, and files named `c95db604-0abd-4bc5-9ebd-3ece37b3ce52...` appear beside the old ones - the model's own id, as read in D-113. *None*: *"Recorded: none ..."* and nothing copied. **Negative:** ask for the loads again - the EARLIER ANSWERS heading does not come back, and a second answer is refused |
| **CK3** | **A second model from the same template** - a new project from the default template (or any of his own test projects), counted, then `heron_hvac` *ventilation* | The four standards are asked afresh - not the ones CK1 or CK2 kept - and the EARLIER ANSWERS question is asked for this model too. **Negative:** back in *Heron loads test*, nothing is asked twice |
| **CK4** | **A copy keeps its answers** - two copies of one model one after the other, e.g. `D:\Ajmal\Heron-Proving\Snowdon-scratch.rvt` then `Snowdon-E24-detached.rvt`: count something in the first, answer the four HVAC standards; then *use this model* on the second and ask the same | The second is NOT asked the standards - D-113 measured that copies of one model carry one id, and this is that measurement through the API. **Negative:** CK3's model, from a template, was asked |
| **CK5** | **Revit 2020** - `Desktop\PIPE.rvt` open, counted, then `heron_hvac` *ventilation* with the four standards given | **NOTHING IS KEPT FOR THIS MODEL** and why: Revit 2020 has no id of its own for a model. No new file under `projects\`. **Negative:** the same request in Revit 2024 on CK3's model keeps them |
| **CK6** | **The pin** - pinned to *Heron loads test*, click into CK3's model, ask Heron to select all ducts | The other model's ducts ARE selected - the add-in's own guard still compares the template's id (5b-324, *not fixed here*) - and Heron says so: *"Heron SELECTED ... in <other> before it could tell ..."*, naming both models, never *"Nothing has been sent to Revit"*. **Negative:** the pin stays on *Heron loads test* - ask for the loads again and it is that model's answer |
