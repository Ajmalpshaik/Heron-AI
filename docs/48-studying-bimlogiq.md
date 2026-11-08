<!--
Heron-Agent:  none
Heron-Step:   18
Heron-Status: DRAFT
Heron-Since:  0.1.0
Heron-Layer:  brain
See docs/29-metadata-standard.md
-->

# 48 — Studying BIMLOGIQ: what a commercial Revit AI does, and what Heron should build from it

> **Status: research note, 2026-11-08. Nothing built, nothing copied.** Asked by the owner: *"research
> bimlogiq.com - what capabilities and tools they have - we need to make ours and make it part of us."*
>
> **How it was read.** `bimlogiq.com` and `aecmag.com` were **outside this cloud session's network**, so
> nothing below was fetched from their pages directly. Every BIMLOGIQ fact comes from **web-search
> summaries** of their own site, their docs, the Autodesk App Store listing, ARKANCE reseller pages and
> AEC Magazine (sources in §7). Most of it is **the vendor's own claim** - nothing here was run.
> A second pass with `bimlogiq.com` allowed in the environment's network settings should check §1.

---

## 0. In one paragraph

BIMLOGIQ (Sydney) sells three Revit add-ins: **Argus** (an agentic chat that drives Revit, renamed from
*Copilot* in April 2026), **Smart Annotation** (tagging and dimensioning without overlaps) and
**Smart Schematics** (a 2D duct/pipe schematic from the 3D model). **Heron already does the agent part
more safely** - Keep or Discard, one undo, a proof per tool - and is deeper on MEP engineering. **Where
BIMLOGIQ is ahead is product polish around repeat work:** saving a prompt as a typed command, sharing
annotation templates, one-click QA checklists, and the 3D-to-schematic drawing. §5 is the build order.

---

## 1. What BIMLOGIQ has

### 1.1 Argus - the agent (was *Copilot*)

| Feature | What it does, per the vendor |
|---|---|
| Agentic chat | Plans the steps for a goal, chains its tools, runs end to end, adapts as it goes |
| Commands | **60+ public commands** out of the box. Each is a small in-Revit app with its own inputs |
| Save a prompt as a command | Any prompt can be saved as a **typed command with named inputs**, published to a **firm-wide library**, and a starred command **pinned to the Revit ribbon** |
| Command editor | Built-in editor: `Component.tsx` (the input form), C# blocks (`main.cs`, `load-*.cs`, `utils.cs`), live preview, **Preview / Validate / Save**. The AI writes the command; **nothing is applied without the user's approval** |
| Dynamo to command | Reads a Dynamo graph, finds every input node, builds a typed command from it |
| `@` mention | Type `@` in chat to point at one Revit element |
| QA/QC | Rules written in plain language become checks. **Checklists** audit a whole model in one click (Aug 2026); a shared **check library** |
| Model checks | Find duplicated and overlapping elements; clashes between this model and links in a view; elements overlapping the title block |
| Views and sheets | Sheets from Excel; duplicate views with scope box; sections per room; wall sections; dimensions for grids and levels |
| Rebar | Reinforcement for columns, beams, foundations, floors, walls **from a spec or an attached PDF**, with a checklist for anchorage, terminations and bar-length limits (Jul 2026) |
| Family creation | Parametric families from scratch - reference planes, parameters, types; started with casework (Aug 2026) |
| MEP systems | Reasons over a system **as a network through its connectors**, even when parts are not connected (Jun 2026). **Model MEP networks from a CAD drawing** (Sep 2026, no detail found) |
| Restroom design | Listed for Sep 2026; AEC Magazine calls it an ADA bathroom agent. No detail found |
| ACC | Autodesk Platform Services / ACC connection, recursive folder search with wildcards (May 2026) |
| Revit | 2022 - 2026 |
| LLM | **Not named** in anything found |

### 1.2 Smart Annotation

| Feature | What it does, per the vendor |
|---|---|
| Tag and Arrange | One click: tags the selected elements in a view and places them **without overlapping** other annotation or model |
| Dimension and Arrange | Dimensions in plans, elevations and details **by template rules** - which categories (ducts, pipes, walls), new tags only or new and existing |
| Batch Annotation | The same template across many views |
| Templates | Settings saved as templates, **exported as a file and shared** with the team |
| Covers | Linked-file elements, fabrication parts, nested families, 3D views, custom leader shapes, tag alignment |
| Claim | "Up to 70%" faster tagging - vendor figure, not tested |

### 1.3 Smart Schematics

Turns the 3D duct or pipe model into a **2D schematic in one click**, with simplified calculations such
as pressure drop. No current status or price found.

### 1.4 Price (indicative, AUD, billed yearly, from an ARKANCE page)

Copilot/Argus about 56 a month per user; Smart Annotation PRO about 200 a month. Trials of 14 to 30
days. Smart Schematics not listed.

---

## 2. Feature by feature against Heron

**Heron column is from the fragment cards and brain files in this repository on 2026-11-08** - read
from `brain/fragments/*/fragment.yaml` (`heron-status:`), `brain/skills/` and `brain/*.py`. A status is
the card's own; nothing here was run in Revit.

| BIMLOGIQ | Heron today | Verdict |
|---|---|---|
| Agent that plans and chains | The host plans ([D-01](DECISIONS.md)); `heron_lookup` + `heron_method` + `brain/skills/*.yaml` give the steps | **Have** |
| Approval before a change | `revit_change` asks **Keep or Discard**, one named undo ([D-126](DECISIONS.md)) | **Have - stricter** |
| 60+ commands | The fragment library - `ls brain/fragments \| wc -l`, status by `grep -h '^heron-status:' brain/fragments/*/fragment.yaml \| sort \| uniq -c` | **Have - far more** |
| Save a prompt as a typed command, firm library, pin to ribbon | `brain/heron_userskills.py` (DRAFT) - personal to shared ladder, plans only. No "save this chat as a command", no ribbon pin | **Gap** |
| Command editor, AI writes C#, validate | `heron-fragment-author` agent writes a DRAFT fragment and compiles every release. No editor inside Revit | **Partly** |
| Dynamo graph to command | Nothing | **Gap** |
| `@` element mention | Companion shows the live selection ([40](40-heron-companion.md)) | **Partly** |
| Tag without overlaps | `tag-elements-in-view`, `arrange-tags`, `stack-tags`, `find-overlapping-tags`, `offset-tags-from-host` - all PROVEN. Arrange is a tidy-up after placing, not one placement pass | **Partly** |
| Dimension by template rules | `create-dimension` (MEP runs merged in 2026-11-01), `dimension-element-faces`, `dimension-grids-and-levels`, `dimension-rooms`, `dimension-wall-openings` | **Partly** - no rule template |
| Annotation template, exported and shared | Nothing | **Gap** |
| Batch across many views | Nothing found as one job | **Gap** |
| Linked-file tagging | `tag-elements-in-view` tags rooms through a link (card v7) | **Partly** - other categories unsure |
| Fabrication-part tagging | Fabrication fragments exist; tagging them not found | **Unsure** |
| 3D to 2D schematic | `report-mep-pressure-drop`, `heron_hvac` duct friction. No schematic drawing | **Gap** |
| Plain-language QA rule | `heron_modelqa.py`, `check-model-standards`, many `check-*` fragments, `heron-model-auditor` agent. No sentence-to-rule step, no saved checklist | **Partly** |
| Duplicates and overlaps | `find-duplicate-elements` PROVEN | **Have** |
| Clash with links | `find-clashes` PROVEN | **Have** |
| Title block overlap | `report-sheet-title-blocks` exists; overlap check not found | **Unsure** |
| Sheets from Excel | `create-sheets` PROVEN; `import-parameter-values` DEPRECATED. No Excel reader in front of it | **Partly** |
| Views, sections per room | `duplicate-views`, `create-section-view`, `create-room-elevations` | **Have** |
| Rebar from a spec or PDF | rebar fragments (`place-beam-rebar`, `place-column-rebar` ...) and `reinforce-concrete` skill. No spec/PDF reading, no anchorage checklist | **Partly** |
| Parametric family agent | `family-creation` and the other `brain/skills/*-family-creation.yaml` skills, [43](43-building-revit-families.md) | **Have - deeper** |
| MEP network reasoning | `trace-connectivity` DRAFT, `trace-system` skill, `brain/heron_graph.py` | **Partly** |
| MEP from a CAD drawing | `extract-cad-curves` PROVEN reads the lines; nothing turns them into ducts or pipes | **Gap** |
| Restroom layout | Nothing | **Gap** (architectural) |
| ACC / cloud folders | Nothing - Heron is local by design | **Gap, needs a decision** |
| Revit releases | 2020 - 2027 ([16](16-version-support-strategy.md)) | **Heron wider** |

### Where Heron is already ahead

1. **Safety.** Keep or Discard, one undo per request, `write.enabled` off by default, never syncs on its
   own. BIMLOGIQ says it shows code before running; nothing found says one undo or rollback.
2. **Proof.** Each tool is PROVEN against a named model with a negative case ([D-30](DECISIONS.md)).
   Nothing comparable found for BIMLOGIQ.
3. **MEP engineering.** HVAC loads, ASHRAE 62.1, duct sizing ([41](41-hvac-design.md)), sprinkler layout
   and hydraulics ([42](42-fire-protection-design.md), [46](46-sprinkler-hydraulics-from-the-model.md),
   [47](47-sprinkler-layout-from-the-model.md)). BIMLOGIQ shows only "simplified calculations".
4. **Revit 2020 and 2021, and 2027.** BIMLOGIQ lists 2022 - 2026.
5. **Any host, local.** MCP, no vendor cloud, no per-seat price.

---

## 3. Reverse engineering - what is allowed

**Black-box study: yes. Decompiling their add-in: no.**

- **Allowed:** reading their public docs, release notes, videos and App Store pages; running a **trial**
  and watching what it does on our own test model - inputs, outputs, how it asks, how long it takes. That
  is learning a behaviour, and Heron re-authors the behaviour its own way.
- **Not allowed:** decompiling their DLLs, copying their C#, their command names, their screen wording,
  their templates or their branding. Heron's rule is **studied and re-authored, never imported**
  ([D-25](DECISIONS.md), [31](31-studying-the-existing-libraries.md)), and AGENTS.md forbids mixing
  another project's code or wording into Heron. Most commercial add-in licences also forbid
  decompiling - **their exact EULA was not read**, so check it before any trial is used for study.
- **What to watch in a trial** (the black-box list): how Tag and Arrange picks a tag position; what a
  Dimension template holds; how a saved command's inputs are typed; what a QA checklist reports.

---

## 4. What not to take

- **A command editor with TSX and C# inside Revit.** Heron's fragment route (card + C# + proof +
  compile for every release) already does this with a gate. A second, ungated route to new C# would
  bypass proof. Keep the author agent; do not build an in-Revit code editor.
- **ACC and cloud folders** - only after an owner decision; Heron is local by design and this would
  need sign-in, tokens and a network host.
- **Restroom/ADA layout** - architectural, and a standard Heron has no source for. Low priority for an
  MEP modeller.

---

## 5. What to build - one order

Each item goes through **`heron-duplicate-checker` first**, then `heron-fragment-author` or a skill, at
DRAFT, until proven on a real model. Ordered by value to an MEP modeller against effort.

| # | Build | Why first | Builds on |
|---|---|---|---|
| 1 | **Save this job as a command** - a finished chat job saved as a named recipe with typed inputs, personal first, raised to shared | Turns every good chat into a repeatable button; the biggest everyday win | `heron_userskills.py`, `brain/skills/*.yaml` |
| 2 | **Annotation template + Tag in one pass** - one saved file: which categories, which tag type, leader rule, offset; place and arrange together, no overlaps; **batch over many views** | Most hours on a drawing set go here | `tag-elements-in-view`, `arrange-tags`, `stack-tags` |
| 3 | **Dimension template for MEP runs** - categories, which faces, spacing, string placement, per view | Same template file as #2 | `create-dimension` |
| 4 | **Plain-language QA rule to saved checklist** - the host turns a sentence into a check that maps onto existing `check-*` fragments; the checklist runs in one call and reports. **Built 2026-11-08, not run in Revit: [docs/50](50-qa-checklists.md)** | Heron has the checks; it lacks the saved list | `heron_modelqa.py`, `heron-model-auditor` |
| 5 | **3D to 2D MEP schematic** - one system traced, drawn as single lines on a drafting view, with sizes, flow and pressure drop. **Built 2026-11-08 as `DRAW_MEP_SCHEMATIC` (DRAFT), not run in Revit: [§5.1](#51-item-5---the-mep-schematic)** | Real MEP value, and Heron already has the trace and the maths | `trace-connectivity`, `report-mep-pressure-drop`, `heron_hvac` (`heron_graph.py`, named here first, is the graph of fragments, not of ducts - not used) |
| 6 | **CAD lines to ducts and pipes** - read the CAD layer, ask size and type, draw the run | Common job on Gulf projects with CAD backgrounds | `extract-cad-curves`, `duct-layout` skill |
| 7 | **Sheets from Excel** - read the sheet list from a spreadsheet, preview, create | Small, quick win | `create-sheets` |
| 8 | **Rebar from a spec** - read the bar sizes and covers from a given spec, then place, then check anchorage and bar length | Structural, lower for MEP | rebar fragments, `reinforce-concrete` |
| Later | Dynamo graph to recipe; ACC (after a decision) | | |


### 5.1 Item 5 - the MEP schematic

**Status 2026-11-08: built, DRAFT, compiled for 2020-2027, NOT RUN in Revit.** One new card,
[`draw-mep-schematic`](../brain/fragments/draw-mep-schematic/fragment.yaml) (`DRAW_MEP_SCHEMATIC`, MODIFY):
*"draw a schematic of supply air system SA-01"*, or of the system of the selected element. In one call and one
undo it traces the system through its connectors from the source equipment (or the selected element) to every
terminal, makes a NEW drafting view named after the system (a taken name gets " (2)"), and draws the system as
single lines - an orthogonal tree, main run across, branches square to it on alternating sides, not to scale.
Each straight segment carries Revit's own size, flow and pressure drop, read from the model's parameters;
equipment and terminals carry their Mark or type name; open ends and loops are marked and listed. The 3D model
is not changed.

- **One card, not a chain:** a traced network handed out by a read arrives as its count and first three items,
  and chat values lose their line breaks - so trace, layout and drawing are one script.
- **Revit's numbers, never Heron's:** the one piece of arithmetic is adding the Pressure Drop values of the
  runs and fittings of a segment, formatted by Revit in the project's units; the legend on the view says so.
  A system Revit has not calculated is labelled "not calculated", never 0.
- **The overlap** with `TRACE_CONNECTIVITY`, `REPORT_MEP_PRESSURE_DROP`, `CREATE_DRAFTING_VIEW`,
  `CREATE_TEXT_NOTE` and `DRAW_FAMILY_DETAIL_LINES` - none edited - and the merge planned once it is proved:
  [FRAGMENT-ISSUES row 5b-1479](fragment-issues/section-5b-rows-401-425.md).
- **Before it can be PROVEN:** a read card that finds views must own *"is there already a schematic"* and
  *"show me the schematic"* - today both reach this card on one search backend, and asking to see a
  schematic must never draw a new one ([row 5b-1479](fragment-issues/section-5b-rows-401-425.md)).
- **What a real Revit must show:** [NEEDS-CHECKING Group MW](needs-checking/group-mw.md) - a duct system, a
  pipe loop, open ends, a system with no flow calculated, a large system and its time, Revit 2020 and 2025/2026.

---

## 6. Open points for the owner

1. Allow `bimlogiq.com` in the cloud environment's network so §1 can be checked against their own pages.
2. Take a Smart Annotation and Argus **trial** on a test model for the black-box list in §3? Only after
   reading their EULA.
3. Confirm the order in §5 - item 1 or item 2 first.

---

## 7. Sources

Web-search summaries of these pages, 2026-11-08. None was fetched directly.

- BIMLOGIQ - [home](https://bimlogiq.com/) · [docs](https://bimlogiq.com/docs/home.html) ·
  [What is Argus](https://bimlogiq.com/docs/copilot/docs/what-is-bimlogiq-copilot.html) ·
  [command editor](https://bimlogiq.com/docs/copilot/docs/command-code-editor) ·
  [release timeline](https://bimlogiq.com/whats-new) ·
  [Smart Annotation](https://bimlogiq.com/product/smart-annotation) ·
  [Tag and Arrange](https://bimlogiq.com/docs/smart-annotation/docs/tag-and-arrange.html) ·
  [Dimension and Arrange](https://bimlogiq.com/docs/smart-annotation/docs/dimension-and-arrange.html)
- [AEC Magazine - Argus](https://aecmag.com/bim/bimlogiq-argus-ai-platform-launches-for-revit/) ·
  [AEC Magazine - Copilot](https://aecmag.com/bim/ai-tool-uses-natural-language-to-drive-revit/)
- [ARKANCE - BIMLOGIQ hub](https://arkance.world/au-en/products/bimlogiq/hub) ·
  [ARKANCE - Smart Annotation PRO](https://arkance.world/au-en/products/bimlogiq/smart-annotation-pro)
- [Autodesk App Store - Smart Annotation](https://apps.autodesk.com/RVT/en/Detail/Index?id=777898712817092915&appLang=en&os=Win64) ·
  [Autodesk App Store - Copilot](https://apps.autodesk.com/RVT/en/Detail/Index?id=1840006799718333764&appLang=en&os=Win64)
