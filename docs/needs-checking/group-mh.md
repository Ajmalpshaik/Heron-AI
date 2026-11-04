# Needs checking — Group MH

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group MH - MEP fabrication parts changed to another service or size, hung with a named hanger button, and placed one at a time from a named button: `CHANGE_FABRICATION_SERVICE_SIZE`, `ADD_FABRICATION_HANGERS`, `PLACE_FABRICATION_PART` (2026-11-02)

Asked for in the modeller's words: *"change these fabrication parts to the High Pressure service"*, *"change the
size of these fabrication ducts to 400x300"*, *"add hangers to these fabrication ducts every 1500 mm using the Rod
Hanger button"*, *"put a hanger on this fabrication pipe"*, *"place a fabrication straight from the Low Pressure
service, button Straight, at this point on Level 1"* and *"add a fabrication elbow from the Copper Plumbing
service"* - Revit's Modify | Fabrication Parts > Change Service and Change Size, a hanger button of the MEP
Fabrication Parts palette, and any other button of it. No card did any of these: a grep of every fragment that day
for `FabricationNetworkChangeService`, `CreateHanger` and `FabricationPart.Create` found nothing, and
`brain/heron_retrieve.py` sent all six sentences to `CONVERT_TO_FABRICATION` first. heron-duplicate-checker's method
found look-alikes only - `CONVERT_TO_FABRICATION` turns design ducts into fabrication parts, `SET_MEP_SIZE` sets a
design duct's size parameters, `DISTRIBUTE_ALONG_RUN` spaces a loadable hanger family along a design run,
`CREATE_MEP_RUNS` draws design ducts - so three new cards, all writes. Change Service and Change Size share one card:
they are one Revit object with one selection, one failure report and one read-back.

| What | Where | What it does |
|---|---|---|
| `CHANGE_FABRICATION_SERVICE_SIZE` (MODIFY) | [`brain/fragments/change-fabrication-service-size`](../../brain/fragments/change-fabrication-service-size/fragment.yaml) | Revit's **Change Service** on the fabrication parts handed in, to the loaded service **named by its whole name or abbreviation - never guessed** (not named, not matched or matched twice changes nothing and **lists the loaded services**), from a palette named (Revit restricted to it) or the first one not excluded; and/or **Change Size** of the straights to width x depth or one diameter in mm, each straight's present size found in Revit's own list of straight sizes and mapped to the new one. **No configuration** refused in Revit words; anything not a fabrication part named in `notFabrication` and left. Each part's service and size **before and after**, every part Revit re-made or made, and Revit's failures in words (`changed`, `notChanged`, `notFabrication`) |
| `ADD_FABRICATION_HANGERS` (MODIFY) | [`brain/fragments/add-fabrication-hangers`](../../brain/fragments/add-fabrication-hangers/fragment.yaml) | Hangers from the **hanger button named exactly** - from each straight's own service or a loaded service named - on the fabrication straights handed in: one in the middle, or `ceil(length / spacing)` evenly at no more than the spacing apart, half a space in from each end; attached to the structure above when asked. No button named, one not found, or one that is not a hanger places nothing and **lists that service's hanger buttons**. Fittings and hangers handed in, and straights Revit refuses, named. Each hanger read back with its host and distance (`hangers`, `notPlaced`, `notFabrication`) |
| `PLACE_FABRICATION_PART` (MODIFY) | [`brain/fragments/place-fabrication-part`](../../brain/fragments/place-fabrication-part/fragment.yaml) | One part from the **button named exactly** on the loaded **service named** - a straight, an elbow, any fitting - on the level named or the plan in front, its origin moved to the point (mm, z above the level) and turned by an angle; at a size given, which Revit matches to a condition, or the button's first condition. A hanger button is pointed at adding hangers; a service, button or level not found places nothing and **lists what there is**. The part's id, service, button, size, length and origin read back; connected to nothing (`placed`) |

**What is done and what is not.** All three compile for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-02, a Linux container). Read off the shipped assemblies with
`tools/api-surface --members`: `FabricationNetworkChangeService` (its `ChangeService` third int a **group id to 2021
and a palette id from 2022**, the same type), `FabricationPartSizeMap` (its seven-argument constructor likewise),
`FabricationNetworkChangeServiceResult`; `FabricationPart.CreateHanger` (three forms, unchanged), `FabricationPart.Create`
(three forms, unchanged), `GetHostedInfo`, `Origin`; `FabricationService` button sets **groups to 2021 and palettes
from 2022** (`#if REVIT2020 || REVIT2021`). The change service and the size map live in a namespace the executor does
not import (`HeronFragmentImports`), so `CHANGE_FABRICATION_SERVICE_SIZE` finds them beside `FabricationPart` and calls
them **by reflection**, as `CONVERT_TO_FABRICATION` does; `check-api-surface` cannot see a member named in a string, so
those were checked by hand. `AlignPartByInsertionPoint` takes an enum from that namespace too, so the placed part is
moved by `ElementTransformUtils` instead. **Nothing here has run in Revit.** All three cards are DRAFT. **Only Revit
can settle**: whether Change Service keeps a part's id or re-makes it, and what it does to fittings; whether the
palette id Change Service takes is the palette's index, as these cards pass it; how Revit's list of straight sizes
writes a round straight's depth; whether the hanger distance is measured from the connector the card picks; and where
a new part's origin sits on it. MH2 to MH5 RECORD each.

**Routing.** Measured through `heron_retrieve.find` on fresh scratch stores of the tree at `743c641` and of this
change (`model` backend; Revit 2024; with and without the host's question flag), over every declared utterance and
routing-table sentence of every live card (5,630 sentences). Without the flag the six sentences reach these cards -
before, all six reached `CONVERT_TO_FABRICATION` - and with it a read - `REPORT_FABRICATION_SETUP`, or for the size sentence `REPORT_DUCT_CONDUIT_SIZES`. No other sentence reaches
the three new cards. *"place the areas"*, claimed for `PLACE_ROOMS` in `CREATE_ROOM_SEPARATION_LINES`' routing
table, moved to `CREATE_AREA_PLAN` and is declared on `PLACE_ROOMS`, outside `impl/`, fingerprint unchanged -
[row 5b-1436](../fragment-issues/section-5b-rows-401-425.md), FIXED. Seven other sentences changed answer, none away
from a card that claims them - recorded there, not tuned. *"put the duct tags above the ducts with a leader"* left `OFFSET_TAGS_FROM_HOST`, which sits at the last nearness place
the fusion reads; `ADD_FABRICATION_HANGERS`' own identity and purpose were reworded and it is back -
[row 5b-1437](../fragment-issues/section-5b-rows-401-425.md), FIXED; `tests/test_routing_phrases.py` fails exactly its known seven. `tools/check-routing.py` (its own scratch store, before and
after): no question answered by a write; claimed and not reached 66 and 66.

**The model - `fabrication-mh`**, a scratch project from Revit's metric Mechanical template, saved under
`%TEMP%\heron-work`, never a working model. Every write here is ROLLED BACK unless the modeller keeps it. The job
file is
[`tools/jobs/fabrication-change-hangers-place-mh-2026-11-02.yaml`](../../tools/jobs/fabrication-change-hangers-place-mh-2026-11-02.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **MH1** | *Arrange.* `fabrication-mh`: levels "Level 1" and "Level 2", a structural floor above Level 1. Systems > Fabrication > MEP Fabrication Settings, one of Autodesk's sample configurations, **two ductwork services (low and high pressure) and one pipework service** loaded. Read them with `REPORT_FABRICATION_SETUP` (*"list the fabrication services and their buttons"*) - write down every service's exact name, the low pressure service's straight, elbow and hanger buttons, and one button that is not a hanger - **into the job file**. In "Level 1", from the MEP Fabrication Parts palette, the low pressure service: a horizontal rectangular run of two straights about 3000 mm long and an elbow at 2700 mm, at a size the service has; one design duct; one wall. Save. A second scratch project with **no configuration**, holding one duct | The services and buttons as the MEP Fabrication Parts palette shows them |
| **MH2** | *Change Service - D-30 positive and negative, THE NEGATIVE FIRST.* The job file's first job, `--write`, rolled back | Negative, the design duct: nothing changed, named as a design duct. Positive: every part read back on the high pressure service; **RECORD whether the ids stayed or Revit re-made the parts**, and the palette named in the report |
| **MH3** | *Change Size, and never guessed.* By hand with `validate --write`, rolled back: the two straights, size "400x300" (a size the service has); the same with size "150"; one straight, service "High"; one straight, the high pressure service and palette "ZZNOPALETTE"; the second project's duct with any service | Both straights at 400x300, **RECORD what Revit did to the elbow**; "150" not sent, *it takes width x depth*; "High" lists the loaded services; the palette lists the service's palettes; the second project refused in Revit words - nothing changed anywhere |
| **MH4** | *Hangers - D-30 positive and negative, THE NEGATIVE FIRST.* The job file's second job, `--write`, rolled back; then by hand: one straight with no button; with the button that is not a hanger; with `attachToStructure` true and no spacing | Negative, the design duct: no hanger. Positive: two hangers on each straight at 750 and 2250 mm, hosted on it - **measure one from its straight's end with Revit's Measure tool, and RECORD which end**; the elbow *a fitting, not a straight*. No button and the wrong button list the hanger buttons; the attached hanger in the middle, its rods to the floor |
| **MH5** | *Place one part - D-30 positive and negative, THE NEGATIVE FIRST.* The job file's third job, `--write`, rolled back; then by hand: the elbow button with no level and the plan "Level 1" in front, size "400x300"; the straight button on level "Level 9" | Negative, the hanger button: nothing placed. Positive: the straight on "Level 1", **RECORD where its origin reads against the point and the length Revit gave it**; the elbow on "Level 1" at the condition Revit picked; "Level 9" lists the levels |
| **MH6** | *The second route.* Revit's own Change Service, Change Size, a hanger button and a straight button by hand on copies of the same saved file | The same parts changed, the same hangers, the same part placed as the reports gave; Undo takes each back as one entry |
