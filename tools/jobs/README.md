# Job files for `batch-prove`

The job files of past proving rounds, and [`example.yaml`](example.yaml) to start from. How to arrange a job, and
the five rules, are in [`.claude/skills/fragment-proving/SKILL.md`](../../.claude/skills/fragment-proving/SKILL.md);
the runner is [`tools/batch-prove.py`](../batch-prove.py) and the skeleton writer
[`tools/generate-jobs.py`](../generate-jobs.py) ([`tools/README.md`](../README.md) has both).

## The files of 2026-11-21 - one for every DRAFT card whose NEEDS-CHECKING group had none

Written in a cloud session with **no Revit**, from each card's `tests/cases.yaml` and its group's rows.
**NOT RUN.** Every file was checked with `--dry-run` on 2026-11-21: no `REFUSED`, no `NO FRAGMENT`. A dry run
cannot see a model, so a wrong view name or a missing type only shows on the PC.

**For each file:** open the models in the "Open" column, then

```bash
python tools/batch-prove.py tools/jobs/<file> --dry-run
python tools/batch-prove.py tools/jobs/<file> --session <pid>
```

Each file's header says what the model must hold first, and each job's `note:` what pass looks like.

**Run order matters where one card is in two files** - a run record and a draft are kept per card, so the second
run replaces the first:

- `create-sheets` - run [`create-sheets-oz-2026-11-21.yaml`](create-sheets-oz-2026-11-21.yaml) (Group OZ, the
  table), read its draft, then [`pe-project1-oz-copy-revit2020-2026-11-21.yaml`](pe-project1-oz-copy-revit2020-2026-11-21.yaml)
  (Group PE, placeholders).
- `extend-roof-to-roof` (version 2) - [`extend-roof-to-roof-oy-2026-11-20.yaml`](extend-roof-to-roof-oy-2026-11-20.yaml)
  (OY2, OY3), then [`pk-pg-oy-roofjoin-oy-revit2024-2026-11-21.yaml`](pk-pg-oy-roofjoin-oy-revit2024-2026-11-21.yaml)
  (OY6, OY7, Wing F added), then on Revit 2020 [`oy-roofjoin-oy-revit2020-2026-11-21.yaml`](oy-roofjoin-oy-revit2020-2026-11-21.yaml)
  (OY8). Read each draft before the next.
- The two Group OK cards already have unsigned drafts from kept runs - read and sign those first, or the OK file
  replaces them.
- A card with an older owed group (OQ before PK, PG before PK, PB before PK, OR16 before PK) has ONE job here on
  its current code, and its note names every row it answers.

**Proved, then the code moved:** `python tools/check-signatures.py` lists them under STALE. Every STALE card that is
DRAFT is in one of the tables below.

**A value in CAPITALS in a job file** - `"LINE L1'S ID"`, `"THE ACTIVE OPTION'S NAME"`, `LEG-FORM-ID` - is a value
nobody recorded. Type the model's own value in before the run: the dry run does not catch it (row 5b-264). They are
all in the list *Values that need the model's own name* below.

### By release, then file

| Revit | File | Open | Cards (rows) |
|---|---|---|---|
| 2020 | [`fix-p1102-arch-revit2020-2026-11-21.yaml`](fix-p1102-arch-revit2020-2026-11-21.yaml) | P1102 ARCH | `convert-cad-to-directshape` (I2 (group-i.md line 64), OI morning list (group-oi.md line 120)) |
| 2020 | [`fix-p1102-mep-revit2020-2026-11-21.yaml`](fix-p1102-mep-revit2020-2026-11-21.yaml) | P1102 MEP | `describe-blank-parameters` (V (group-v.md line 126), W (group-w.md lines 35, 58-60), X (group-x.md line 84), OI morning list (group-oi.md line 124)) |
| 2020 | [`hy-rebar-hy-revit2020-2026-11-21.yaml`](hy-rebar-hy-revit2020-2026-11-21.yaml) | rebar-hy scratch (name not recorded) | `place-fabric-reinforcement` (HY12 (group-hy.md line 67); OI morning list) |
| 2020 | [`ld-pk-steel-ld-revit2020-2026-11-21.yaml`](ld-pk-steel-ld-revit2020-2026-11-21.yaml) | steel-ld | `report-structural-connections` (LD2, PK3 (W1 leg)) |
| 2020 | [`ok-column-scratch-1044-revit2020-2026-11-21.yaml`](ok-column-scratch-1044-revit2020-2026-11-21.yaml) | Column Scratch 1044 (%TEMP%\heron-work\issues-1120) | `set-family-plane-reference` (OK3, OK4, OK5) |
| 2020 | [`ok-heron-nf-generic-revit2020-2026-11-21.yaml`](ok-heron-nf-generic-revit2020-2026-11-21.yaml) | Heron NF Generic (%TEMP%\heron-work\family-2d-widen) | `create-line-style` (OK1, OK2) |
| 2020 | [`oq-oq-blank-revit2020-2026-11-21.yaml`](oq-oq-blank-revit2020-2026-11-21.yaml) | oq-blank (%TEMP%\heron-work\oq\oq-blank.rvt, OQ13's revisions made by hand first) | `edit-revision-numbering-sequence` (OQ13, OQ14) |
| 2020 | [`or-nr-spaces-host-2-revit2020-2026-11-21.yaml`](or-nr-spaces-host-2-revit2020-2026-11-21.yaml) | NR Spaces Host 2 | `sync-spaces-with-linked-rooms` (OR19 (OQ9, OQ10 need oq-mep.rvt, not made)) |
| 2020 | [`or-nr-spaces-host-revit2020-2026-11-21.yaml`](or-nr-spaces-host-revit2020-2026-11-21.yaml) | NR Spaces Host | `create-hvac-zone` (OR4, OR5) |
| 2020 | [`or-p1102-mep-nr-revit2020-2026-11-21.yaml`](or-p1102-mep-nr-revit2020-2026-11-21.yaml) | P1102 MEP NR | `create-view-filter` (OR10 (a) as negative, (b)'s effect on an untemplated plan as positive) |
| 2020 | [`or-p1102-mep-revit2020-2026-11-21.yaml`](or-p1102-mep-revit2020-2026-11-21.yaml) | P1102 MEP | `set-mep-justification` (OR1, OR3 (c)), `place-accessory-on-run` (OR8 (c)(d), OR9 (a); OR7 by hand), `check-minimum-clearance` (OR24), `frame-elements-in-camera` (OR23), `count-by-spatial-container` (OR25/OR26 label check on real spaces; OR25 arrangement and OR26 (b) by hand), `move-annotation` (OR12), `export-model-to-ifc` (OR22) |
| 2020 | [`or-project1-mech-revit2020-2026-11-21.yaml`](or-project1-mech-revit2020-2026-11-21.yaml) | Project1 | `report-view-work-plane` (OR11) |
| 2020 | [`ot-family1-detail-item-revit2020-2026-11-21.yaml`](ot-family1-detail-item-revit2020-2026-11-21.yaml) | Family1 (Metric Detail Item, never saved); Project1 | `report-family-template` (OT19, OT20) |
| 2020 | [`ot-family1-generic-revit2020-2026-11-21.yaml`](ot-family1-generic-revit2020-2026-11-21.yaml) | Family1 (Metric Generic Model, never saved, Width); Project1 | `report-family-type-catalog` (OT21, OT22), `set-family-settings` (OT23, OT24) |
| 2020 | [`ot-leg-length-generic-revit2020-2026-11-21.yaml`](ot-leg-length-generic-revit2020-2026-11-21.yaml) | the Leg Length scratch family, in front | `array-family-forms` (OT5, OT6) |
| 2020 | [`ot-port-right-generic-revit2020-2026-11-21.yaml`](ot-port-right-generic-revit2020-2026-11-21.yaml) | the Port Right scratch family, in front | `link-family-parameter` (OT13, OT14) |
| 2020 | [`ot-project1-rooms-revit2020-2026-11-21.yaml`](ot-project1-rooms-revit2020-2026-11-21.yaml) | Project1 | `filter-elements-in-room` (OT25, OT26) |
| 2020 | [`ot-pump-accessory-revit2020-2026-11-21.yaml`](ot-pump-accessory-revit2020-2026-11-21.yaml) | the scratch pump family (Revit 2020 Metric Pipe Accessory template), in front | `add-family-connector` (OT15, OT16) |
| 2020 | [`ot-round-void-generic-revit2020-2026-11-21.yaml`](ot-round-void-generic-revit2020-2026-11-21.yaml) | the host scratch family with Round void loaded, in front | `place-family-instances` (OT11, OT12) |
| 2020 | [`ot-spring-generic-revit2020-2026-11-21.yaml`](ot-spring-generic-revit2020-2026-11-21.yaml) | the spring scratch family, in front | `flex-family` (OT9, OT10) |
| 2020 | [`ot-void-array-generic-revit2020-2026-11-21.yaml`](ot-void-array-generic-revit2020-2026-11-21.yaml) | the plate/void-array scratch family, in front | `combine-family-forms` (OT7, OT8, OT31) |
| 2020 | [`ou-heron-sheet-sets-ka-revit2020-2026-11-21.yaml`](ou-heron-sheet-sets-ka-revit2020-2026-11-21.yaml) | Heron Sheet Sets KA | `manage-sheet-sets` (OU8) |
| 2020 | [`ou-heron-wall-finish-revit2020-2026-11-21.yaml`](ou-heron-wall-finish-revit2020-2026-11-21.yaml) | Heron Wall Finish | `create-from-room-boundaries` (OU16; HELD on HX1, HX2 (HX3 kept, HX4 signing)) |
| 2020 | [`ou-house-scratch-1042-revit2020-2026-11-21.yaml`](ou-house-scratch-1042-revit2020-2026-11-21.yaml) | House Scratch 1042 | `flip-elements` (OU6) |
| 2020 | [`ou-house-scratch-2-revit2020-2026-11-21.yaml`](ou-house-scratch-2-revit2020-2026-11-21.yaml) | House Scratch 2 | `edit-text-values` (OU1, OU3(a) (OU2 and OU3(b) by hand)) |
| 2020 | [`ou2-diffuser-scratch-revit2020-2026-11-21.yaml`](ou2-diffuser-scratch-revit2020-2026-11-21.yaml) | (the diffuser scratch model - needs its own name) | `read-element-parameters` (OU12) |
| 2020 | [`ou2-p1102-mep-revit2020-2026-11-21.yaml`](ou2-p1102-mep-revit2020-2026-11-21.yaml) | P1102 MEP | `set-element-level` (OU11, OE6 (held by row 5b-1831)) |
| 2020 | [`ou2-p1102-str-revit2020-2026-11-21.yaml`](ou2-p1102-str-revit2020-2026-11-21.yaml) | P1102 STR | `report-structural-loads` (OU15, NX2) |
| 2020 | [`oy-roofjoin-oy-revit2020-2026-11-21.yaml`](oy-roofjoin-oy-revit2020-2026-11-21.yaml) | (the owner's Revit 2020 roofjoin-oy rebuild - its name to be written in) | `extend-roof-to-roof` (OY6, OY7 (first half) - Revit 2024 file; OY8 (with OY3 on 2020 as its positive) - Revit 2020 file) |
| 2020 | [`pa-pk-ox-p1102-mep-revit2020-2026-11-21.yaml`](pa-pk-ox-p1102-mep-revit2020-2026-11-21.yaml) | P1102 MEP; Project1 | `report-sprinkler-network` (PA1, PA2 (b)), `report-view-filters` (PK8 (OR13 to OR15 by hand first)), `export-families` (OX3 (OX4 is Revit 2024, by hand)) |
| 2020 | [`pb-p1102-family-revit2020-2026-11-21.yaml`](pb-p1102-family-revit2020-2026-11-21.yaml) | P1102 Family | `create-family-forms` (PB5, PB6) |
| 2020 | [`pb-p1102-mep-family-revit2020-2026-11-21.yaml`](pb-p1102-mep-family-revit2020-2026-11-21.yaml) | P1102 MEP Family | `report-family-forms` (PB8, PB7 in spirit (which all)) |
| 2020 | [`pe-p1102-mep-revit2020-2026-11-21.yaml`](pe-p1102-mep-revit2020-2026-11-21.yaml) | P1102 MEP (scratch copy of rme_advanced_sample_project.rvt) | `report-connectors` (PE2) |
| 2020 | [`pe-project1-oz-copy-revit2020-2026-11-21.yaml`](pe-project1-oz-copy-revit2020-2026-11-21.yaml) | OZ's saved Project1 copy, in front | `create-sheets` (PE3 (after OZ)) |
| 2020 | [`pe-project1-systems-revit2020-2026-11-21.yaml`](pe-project1-systems-revit2020-2026-11-21.yaml) | Project1 (Systems template, filter Domestic, {3D} with no filter) | `set-view-filter-categories` (PE1 (also EE)), `apply-view-filter` (PE4, OR27 (also OM)) |
| 2024 | [`a-ec-workshared-scratch-revit2024-2026-11-21.yaml`](a-ec-workshared-scratch-revit2024-2026-11-21.yaml) | the workshared local copy (name not recorded); night-scratch | `set-worksharing-ownership` (EC12, EC part 9 (group-ec.md lines 37, 56); OI morning list (PUBLISH)), `sync-with-central` (A19; OI morning list (PUBLISH); model from EC part 9) |
| 2024 | [`fix-project1-work-revit2024-2026-11-21.yaml`](fix-project1-work-revit2024-2026-11-21.yaml) | Project1 work_ajmal.al; night-scratch | `create-workset-3d-views` (I1 (group-i.md line 52), V (group-v.md line 77), OI morning list (group-oi.md line 121)) |
| 2024 | [`i-project1-work-revit2024-2026-11-21.yaml`](i-project1-work-revit2024-2026-11-21.yaml) | Project1 work_ajmal.al | `set-design-option` (I1 (group-i.md line 51); OI morning list (owner's hands)) |
| 2024 | [`lo-pk-fabrication-lo-revit2024-2026-11-21.yaml`](lo-pk-fabrication-lo-revit2024-2026-11-21.yaml) | fabrication-lo | `report-fabrication-setup` (LO2, PK6) |
| 2024 | [`mg-heron-mg-analytical-revit2024-2026-11-21.yaml`](mg-heron-mg-analytical-revit2024-2026-11-21.yaml) | Heron MG Analytical | `associate-analytical-elements` (MG5 (group-mg.md line 65), MG4 for M9; OI morning list) |
| 2024 | [`ml-params-ml-revit2024-2026-11-21.yaml`](ml-params-ml-revit2024-2026-11-21.yaml) | params-ml | `remove-project-parameter` (ML6 (group-ml.md line 76); OI morning list (ADMIN)) |
| 2024 | [`mo-heron-mo-host-revit2024-2026-11-21.yaml`](mo-heron-mo-host-revit2024-2026-11-21.yaml) | Heron MO Host | `publish-coordinates` (MO6 (group-mo.md), MO0/MO1 models; OI morning list (ADMIN)) |
| 2024 | [`oi-roofjoin-oy-revit2024-2026-11-21.yaml`](oi-roofjoin-oy-revit2024-2026-11-21.yaml) | roofjoin-oy | `export-model-to-nwc` (OI morning list only (no group of its own)) |
| 2024 | [`oi-snowdon-hvac-revit2024-2026-11-21.yaml`](oi-snowdon-hvac-revit2024-2026-11-21.yaml) | Snowdon Towers Sample HVAC | `repoint-view-reference` (OI morning list only; arrangement from REPORT_VIEW_REFERENCES' signed proof (Snowdon HVAC, a reference section)) |
| 2024 | [`ol-en-heronnest-car-revit2024-2026-11-21.yaml`](ol-en-heronnest-car-revit2024-2026-11-21.yaml) | HeronNest_Car (in front); HeronNest_Hub (behind, one unsaved form) | `save-document` (EN11 (a); OL1 context) |
| 2024 | [`op-project1-work-revit2024-2026-11-21.yaml`](op-project1-work-revit2024-2026-11-21.yaml) | Project1 work_ajmal.al | `set-view-section-box` (OP7), `set-view-template-control` (OP8) |
| 2024 | [`op-snowdon-scratch-revit2024-2026-11-21.yaml`](op-snowdon-scratch-revit2024-2026-11-21.yaml) | Snowdon-scratch_ajmal.al (workshared; worksets Shared Levels and Grids, Workset1) | `rename-workset` (OP6) |
| 2024 | [`oq-oq-snowdon-revit2024-2026-11-21.yaml`](oq-oq-snowdon-revit2024-2026-11-21.yaml) | oq-snowdon (%TEMP%\heron-work\oq\oq-snowdon.rvt, six links beside it, loaded) | `reload-links` (OQ7, OQ8) |
| 2024 | [`or-heron-oc-tags-revit2024-2026-11-21.yaml`](or-heron-oc-tags-revit2024-2026-11-21.yaml) | Heron OC Tags | `arrange-tags` (OR18's question on OC6's pair; OQ1, OQ2) |
| 2024 | [`or-snowdon-schematic-proof-revit2024-2026-11-21.yaml`](or-snowdon-schematic-proof-revit2024-2026-11-21.yaml) | Snowdon HVAC - schematic proof (with the other Snowdon models beside it, per Group MW) | `draw-mep-schematic` (OR6 (MW2's arrangement)) |
| 2024 | [`oros-snowdon-scratch-ajmal-revit2024-2026-11-21.yaml`](oros-snowdon-scratch-ajmal-revit2024-2026-11-21.yaml) | Snowdon-scratch_ajmal.al | `copy-view-filters` (OS11; OR17's negative; OR17 order arrangement and OR28 by hand) |
| 2024 | [`ot-p1102-box-revit2024-2026-11-21.yaml`](ot-p1102-box-revit2024-2026-11-21.yaml) | P1102 Box (%TEMP%\heron-work\prove-1102\fam, M_Box.rfa copy) in Revit 2024 | `divide-family-surface` (OT17, OT18) |
| 2024 | [`ou-heron-repairs-test-revit2024-2026-11-21.yaml`](ou-heron-repairs-test-revit2024-2026-11-21.yaml) | Heron Repairs Test | `divide-parts` (OU20) |
| 2024 | [`ou-snowdon-scratch-revit2024-2026-11-21.yaml`](ou-snowdon-scratch-revit2024-2026-11-21.yaml) | Snowdon-scratch_ajmal.al | `auto-size-mep` (OU19) |
| 2024 | [`ou2-heron-reads-test-revit2024-2026-11-21.yaml`](ou2-heron-reads-test-revit2024-2026-11-21.yaml) | Heron Reads Test | `set-material-properties` (OU10) |
| 2024 | [`ou2-oq-snowdon-revit2024-2026-11-21.yaml`](ou2-oq-snowdon-revit2024-2026-11-21.yaml) | oq-snowdon; Family1 | `report-model-dashboard` (OU14, OQ3, OQ4), `set-link-room-bounding` (OU9) |
| 2024 | [`ou2-workshared-scratch-revit2024-2026-11-21.yaml`](ou2-workshared-scratch-revit2024-2026-11-21.yaml) | (local A of the OU18 scratch central - needs its own name); Heron Reads Test | `report-element-ownership` (OU18) |
| 2024 | [`pb-pk-p1102-box-revit2024-2026-11-21.yaml`](pb-pk-p1102-box-revit2024-2026-11-21.yaml) | P1102 Box; P1102 Light24 | `report-divided-surfaces` (PB1, PB2 (v3, Group PB), PK5 (withNodes true, surface leg)), `set-divided-surface-layout` (PB3 (second layout), PB4) |
| 2024 | [`pi-scratch-revit2024-2026-11-21.yaml`](pi-scratch-revit2024-2026-11-21.yaml) | the PI scratch project (new, default template, %TEMP%\heron-work\pi) - needs the model's own name; NOT roofjoin-oy, which already holds the kept grid Wing | `create-grids` (PI1 (positive), PI4 (negative) in the job; PI2, PI3 as rolled-back `fragment create-grids --write` lines in the header) |
| 2024 | [`pj-scratch-roof-revit2024-2026-11-21.yaml`](pj-scratch-roof-revit2024-2026-11-21.yaml) | the PJ scratch project (new, default template, one footprint roof on Level 1, %TEMP%\heron-work\pj) - needs the model's own name; NOT roofjoin-oy, which holds four roofs | `copy-elements` (PJ1 (positive), PJ3 (negative) in the job; PJ2 + PJ4 as one `validate --out brain/proof-drafts/runs/copy-elements-pj2.json` line in the header) |
| 2024 | [`pk-pg-oy-roofjoin-oy-revit2024-2026-11-21.yaml`](pk-pg-oy-roofjoin-oy-revit2024-2026-11-21.yaml) | roofjoin-oy (with one wall and Wing F added by hand, saved) | `report-roof-slopes` (PG1, PG2 (Group PG, v1, run first), PK4 (wall and main roof legs)), `extend-roof-to-roof` (OY6, OY7 (first half) - Revit 2024 file; OY8 (with OY3 on 2020 as its positive) - Revit 2020 file) |
| 2024 | [`pk-snowdon-scratch-revit2024-2026-11-21.yaml`](pk-snowdon-scratch-revit2024-2026-11-21.yaml) | Snowdon-scratch_ajmal.al; Project1 | `list-linked-models` (OQ5, OQ6 (v2, Group OQ, run first), PK2 (v3)), `report-linked-view-usage` (PK1 (negative and Snowdon legs)) |
| 2027 | [`a-ma1-scratch-revit2027-2026-11-21.yaml`](a-ma1-scratch-revit2027-2026-11-21.yaml) | the MA1-style scratch (name not recorded) | `create-radial-dimension` (A19; OI morning list (group-oi.md line 124); MA1 arrangement borrowed from the sibling CREATE_ARC_LENGTH_DIMENSION) |

### Cards with no batch job, and the command instead

A card that cannot come back empty (D-53 tracking), needs the owner's hands between legs, or cannot be bound from
a command line. A command here is the arrangement as far as the records go - read the group's row before running it.

| Card | Rows | Why not a batch job | Command |
|---|---|---|---|
| `activate-document` | OL0, OL2, OL4, OL5; OS3 | OL0's two files are not made and their full path is not recorded; and Group OL's own D-30 pair is a switch both ways, which a batch judges NEG NOT EMPTY (see faults). Prove it one at a time after OL2 and OL4, as group-ol.md says. | `After OL0, in Revit 2020, ol-host open in a window and saved, ol-family in front holding an unsaved reference plane: python mcp/client/heron_bridge_client.py validate activate-document --set "documentPath=<ol-host.rvt's full path>" --negative-set "documentPath=<ol-family.rfa's full path>" (group-ol.md). A negative the contract itself can judge empty is version 2's: --negative-set "documentPath=C:\nope\missing.rfa" (OL5).` |
| `check-link-coordinates` | OU4 | OU4's negative is the SAME project after the owner unloads the second link in Manage Links - a state change between legs that only his hands can make (a link unload is outside Revit's undo, so no rolled-back setup step can do it), and no second document with every link unloaded is recorded for negative-in. validate's own pause between legs fits it exactly; version 1's Family1 negative (no links) would not test row 5b-1122. | `HERON_CLIENT_ID=ou4 python mcp/client/heron_bridge_client.py validate --in "Project1" --out brain/proof-drafts/runs/check-link-coordinates.json --set toleranceMm=0 check-link-coordinates # positive with one link loaded (by shared coordinates) and one unloaded; at the NEGATIVE CASE prompt unload the second link in Manage Links, then press Enter. Link source: C:/Users/ajmal/AppData/Local/Temp/heron-work/elion-proofs/files/HeronLinkSource.rvt` |
| `export-parameters-to-csv` | OU7 | Cannot come back empty (D-53): rows is one per element in every leg, OU7's negative included ('still one row per element'); version 1 was proved by tracking. Also PUBLISH - no batch sends it. | `HERON_CLIENT_ID=ou7 python mcp/client/heron_bridge_client.py validate --in "test projject" --out brain/proof-drafts/runs/export-parameters-to-csv.json --allow-publish --write --setup select-by-category-name --setup set-selection --set "inViewOnly={3D}" --set "parameterNames=Mark, Comments" --set "csvPath=C:/Users/ajmal/AppData/Local/Temp/heron-work/ou7-export.csv" --vary "categoryName=Ducts,Walls,Pipes" --vary-field rows export-parameters-to-csv # Publish switch ON (owner's); version 1's tracking set on this model was Ducts 6, Walls 19, Pipes 2 in {3D}. Then OU7's ambiguous/notCarried reading by hand on the doubled-Comments copy (parameterNames=Mark, Comments, ZZZ).` |
| `link-files` | OU5 | OU5 needs a FRESH Revit session with no link run before it and the positive KEPT (D-119/D-120); a batch run first would be a rolled-back link of the same file - the very run OU5 saves for last (row 5b-1121) - and no batch keeps. | `1) In a fresh Revit 2020 session, through the chat or: python mcp/client/heron_bridge_client.py try-once --in "<fresh scratch>" link-files --set "paths=C:/Users/ajmal/AppData/Local/Temp/heron-work/elion-proofs/files/HeronLinkSource.rvt" --set placement=origin --set "viewName=" (Keep is the owner's). 2) Insert > Link Revit the same file by hand in a copy - the second route. 3) HERON_CLIENT_ID=ou5 python mcp/client/heron_bridge_client.py validate --in "<fresh scratch>" --out brain/proof-drafts/runs/link-files.json --write --kept --negative-set "paths=C:/Users/ajmal/AppData/Local/Temp/heron-work/elion-proofs/files/HeronLinkSource.rvt" --negative-set placement=origin --negative-set "viewName=" link-files (negative: the same path again - linked 0, notAttached empty, alreadyLinked names it). 4) Last, once: a rolled-back run then the same again, --out under %TEMP%\heron-work, for 5b-1121's ALREADY OPEN line.` |
| `list-categories` | OQ11, OQ12 | Cannot come back empty: names, bindable and objectStyles are filled in every document (v3 was signed by D-53 tracking, Cinematic ID Test, Revit 2020; the narrowed batch read NEG NOT EMPTY, row 5b-630). | `HERON_CLIENT_ID=ajmal-pc python mcp/client/heron_bridge_client.py validate --in oq-blank --vary "categories=Generic Models,Doors,Walls" --vary-field styleRows list-categories < /dev/null (after OQ11's material is set on Generic Models; then read OQ11/OQ12's last two columns - material and kind - by eye, and OQ12's Generic Annotation family by hand)` |
| `measure-available-fall` | OI morning list only ('an input shape no job can type') | Cannot be run at all as written: upstream and downstream are two single-INSTANCE Element needs, and the only way to bind one is the word 'selected', which means the ONE element selected (RevitFragment.cs OneElement, ~lines 6855-6928). Both bind the same element, so the card can only ever measure an element against itself. Needs a card or binder change, not an arrangement. | - |
| `measure-distance` | PE5 | Two singular Element needs (first, second). The binder refuses typed text for an instance Element (OneElement, RevitFragment.cs ~6920) and 'selected' binds the same one element to both (section-6 correction of 2026-09-19), so no batch or validate line can hand it two different elements. PE5's own command lines would be refused. | - |
| `place-flow-arrows` | I2 (group-i.md line 69), OI morning list (group-oi.md line 118); fragment-issues section-3i.md line 95, section-3b-i.md line 30 | Needs the owner's hands: a flow-arrow family loaded into the scratch model and its family and type names written down. Nothing a set-up step can make (Group OI). modify-never-run.yaml already holds a job with the CONFIRM names; a new file would carry the same unknown. Once loaded, the one-at-a-time line below (negative: a family name not loaded - cases.yaml 'refused saying so and nothing placed', a different VALUE). | `HERON_CLIENT_ID=ajmal-pc python mcp/client/heron_bridge_client.py validate --in "Snowdon-scratch_ajmal.al" --out brain/proof-drafts/runs/place-flow-arrows.json --write --setup filter-elements-by-category --setup set-selection --set category=Ducts --set "inViewOnly=FloorPlan: M1" --set "view=FloorPlan: M1" --set "arrowFamilyName=<the family he loaded>" --set "arrowTypeName=<its type>" --set spacingMm=2000 --negative-set category=Ducts --negative-set "inViewOnly=FloorPlan: M1" --negative-set "view=FloorPlan: M1" --negative-set "arrowFamilyName=Heron No Such Arrow" --negative-set "arrowTypeName=<its type>" --negative-set spacingMm=2000 place-flow-arrows (22 ducts in FloorPlan: M1 as recorded in snowdon-round3.yaml; whether they carry a flow direction is not recorded - placed 0 with every duct in noDirection is the model, not the card)` |
| `place-hosted-family` | OU23 | No recorded rig: every value OU23 needs - door type, three points on interior walls, the toRoomAt and hinge points - is unrecorded, and no setup step can build the walls (create-wall's `points` is the same name as this card's `points`). Leg (b) also needs rooms placed and KEPT. Version 4's rig (Project2, never saved) had a room and exterior walls only. | `After the owner builds and records the rig: HERON_CLIENT_ID=ou23 python mcp/client/heron_bridge_client.py validate --in "<rig>" --out brain/proof-drafts/runs/place-hosted-family.json --write --set "symbol=<door type>" --set "level=<level>" --set "points=<p1; p2; p3>" --set "toRoomAt=<r1; r2; r3>" --set "hingeAt=<h1; h2; h3>" --set sillHeight= --set rotation= --negative-set ... (every value again, toRoomAt of the one door on the wall's own line) place-hosted-family` |
| `read-room-geometry` | OU17 | Cannot come back empty (D-53): boundary, area and height are declared results and are filled for any placed room, so OU17's plain-room negative (holeLoops and holes empty) still reads NEG NOT EMPTY; the card's own proof of 2026-09-13 was tracking. Prove by tracking holeLoops across three rooms with 2, 1 and 0 shafts - the third room (one shaft) is added so the field moves across three values, which D-53 requires. | `python mcp/client/heron_bridge_client.py validate --in "<OU17 scratch model>" --out brain/proof-drafts/runs/read-room-geometry.json --vary "room=<room around two shafts>\|<room around one shaft>\|<plain rectangular room>" --vary-field holeLoops read-room-geometry (expect holeLoops 2, 1, 0 entries; on the two-shaft room each list 4 curves and holes the same 8 curves in one list)` |
| `refresh-view` | V (group-v.md lines 90, 115), OI morning list (D-53) | Can never come back empty and has NO input to track: it needs only doc and uidoc, so validate --vary has nothing to vary (D-53 needs three values of an input), and cases.yaml's 'no UI document' negative is unreachable - the add-in always builds a UIDocument, even for a document not in front (revit/Heron.Revit.Addin/RevitFragment.cs lines 250-269). Neither leg D-30 asks for can be arranged; owner's call (a decision). | `No proof command exists. The watched positive only (cases.yaml): change a tagged value with another card, then HERON_CLIENT_ID=ajmal-pc python mcp/client/heron_bridge_client.py fragment refresh-view --write </dev/null and look at the tag on screen.` |
| `report-category-overrides` | OR16 and PK7 - not batched | Cannot come back empty: hiddenCategories (role: result) lists every category switched off in the view - every view switches some off - so its v4 proof was by TRACKING (D-53), and PK7's 'nothing overridden' negative would read NEG NOT EMPTY in batch-prove and at accept. OR16 (template Include column ticked/unticked) and PK7's halftone-by-hand leg are hand runs read off overrideList. | `HERON_CLIENT_ID=ajmal-pc python mcp/client/heron_bridge_client.py validate --in "P1102 MEP" --out brain/proof-drafts/runs/report-category-overrides.json --vary "view=Level 2 HVAC Plan,Callout of Level 1 HVAC WSHP TYP,Level 1 - Lighting Plan" --vary-field hiddenCategories report-category-overrides (Revit 2020; three P1102 MEP views recorded in tools/jobs/p1102-round2-reads and round9; if two views give the same hiddenCategories heron_validate refuses - swap one view; then python brain/heron_validate.py draft report-category-overrides --from brain/proof-drafts/runs/report-category-overrides.json)` |
| `report-level-elevations` | OU13 | Never empty and nothing to vary: `storeys` (role: result) is written on every run - even '(this model has no levels)' - and `levels` is filled on any project, so every negative reads NEG NOT EMPTY; the card's only input is includeLinks (a bool), so --vary tracking cannot follow three inputs either. See faults. Read OU13 by hand: | `python mcp/client/heron_bridge_client.py fragment --in "<diffuser scratch model>" report-level-elevations (Level 1's Building Story un-ticked first; the storeys line must count 5 model elements on Level 1 - 3 diffusers + 2 walls; version 4 counted 2). How it is then signed is the owner's call.` |
| `report-open-documents` | PE6 | Takes nothing but the model, so both legs would be the same run (generate-jobs: D-53 tracking); and PE6 is about the MCP server's revit_read routing through a chat, not a batch. | `PE6 in a chat: pin to a scratch family, save and close it, then revit_read REPORT_OPEN_DOCUMENTS; negative revit_read REPORT_LEVEL_ELEVATIONS still refused naming the closed family. Tracking for the card itself: HERON_CLIENT_ID=ajmal-pc python mcp/client/heron_bridge_client.py fragment report-open-documents < /dev/null read with 1, then 2, then 3 projects open - count must follow.` |
| `report-project-units` | CJ4, CJ5 (group-cj.md lines 55-56), OI morning list (D-53) | D-53 tracking, one at a time. The card takes no request input, so --vary has nothing to vary, and cases.yaml's tracking set-up (SET_PROJECT_UNITS as a rolled-back setup) is refused: set-project-units is ADMIN and an ADMIN setup step is refused whatever the flags. CJ4's route: three KEPT states on a scratch, read after each, then put back. | `On a scratch (CJ ran Project2, Revit 2024), Admin and Changes on, HERON_CLIENT_ID=ajmal-pc, stdin closed: (1) python mcp/client/heron_bridge_client.py fragment --in "<scratch>" report-project-units; (2) python mcp/client/heron_bridge_client.py fragment --in "<scratch>" set-project-units --allow-publish --write --apply --set "settings=Cooling Load=Btu/h;Heating Load=Btu/h;Air Flow=Cubic feet per minute, CFM"; (3) report again - exactly those three rows changed; (4) set-project-units again with the three rows step 1 printed (put back); (5) report again - identical to step 1. Then the owner judges the three readings (CJ4); second route Manage > Project Units.` |
| `show-analysis-heatmap` | OI morning list (group-oi.md line 124); fragment-issues section-3g-iii.md line 80 (DID NOT RUN) | No arrangement can run, by batch or by validate: `values` is IDictionary(ElementId,double) bound only from the chain, by name. Across the library the only card that provides a need named `values` of that type is dimension-rooms (MODIFY), and it keys it by the DIMENSIONS it makes (impl line 232: values[dimension.Id]), which have no faces and are not the elements the heatmap is handed - every element would land in noValue. Every per-element double the library has (measure-mep-velocity velocity, read-space-loads airflowLs, measure-run-quantities lengths ...) is under another name, and the chain has no renaming. generate-jobs marks it unarrangeable for the same reason. Needs a card that leaves `values` keyed by element, or a D-54 shape for the dictionary - a decision, not a job. | - |

### Cards already covered by an earlier job file

DRAFT cards whose group's own job file still matches the card. Nothing new was written for them.

| Card | Group | File |
|---|---|---|
| `add-area-based-load` | MI | [`electrical-wire-type-area-load-nodes-mi-2026-11-02.yaml`](electrical-wire-type-area-load-nodes-mi-2026-11-02.yaml) |
| `add-fabrication-hangers` | MH | [`fabrication-change-hangers-place-mh-2026-11-02.yaml`](fabrication-change-hangers-place-mh-2026-11-02.yaml) |
| `add-family-type-parameter` | BV | [`family-kinds-2026-10-02.yaml`](family-kinds-2026-10-02.yaml) |
| `add-stairs-landing` | LS | [`shaped-stairs-ls-2026-11-02.yaml`](shaped-stairs-ls-2026-11-02.yaml) |
| `adjust-analytical-elements` | MG | [`analytical-elements-mg-revit2024-2026-11-14.yaml`](analytical-elements-mg-revit2024-2026-11-14.yaml) |
| `assign-location-data` | ON | [`assign-location-data-v3-on-2026-11-19.yaml`](assign-location-data-v3-on-2026-11-19.yaml) |
| `change-fabrication-service-size` | MH | [`fabrication-change-hangers-place-mh-2026-11-02.yaml`](fabrication-change-hangers-place-mh-2026-11-02.yaml) |
| `check-datums-against-link` | OJ | [`check-datums-against-link-oj-2026-11-17.yaml`](check-datums-against-link-oj-2026-11-17.yaml) |
| `connect-into-main` | LP | [`flex-and-connect-into-lp-2026-11-02.yaml`](flex-and-connect-into-lp-2026-11-02.yaml) |
| `convert-to-fabrication` | LO | [`fabrication-setup-and-convert-lo-2026-11-02.yaml`](fabrication-setup-and-convert-lo-2026-11-02.yaml) |
| `create-arc-length-dimension` | MA | [`create-arc-length-dimension-ma-2026-11-02.yaml`](create-arc-length-dimension-ma-2026-11-02.yaml) |
| `create-energy-model` | MM | [`mechanical-energy-gbxml-mm-2026-11-02.yaml`](mechanical-energy-gbxml-mm-2026-11-02.yaml) |
| `create-property-line` | MJ | [`toposurface-property-line-mj-2026-11-02.yaml`](toposurface-property-line-mj-2026-11-02.yaml) |
| `create-rebar-shape` | MN | [`rebar-shape-symbols-sketch-stair-mn-2026-11-02.yaml`](rebar-shape-symbols-sketch-stair-mn-2026-11-02.yaml) |
| `draw-mep-from-cad` | MX | [`draw-mep-from-cad-mx-2026-11-09.yaml`](draw-mep-from-cad-mx-2026-11-09.yaml) |
| `import-gbxml` | MM | [`mechanical-energy-gbxml-mm-2026-11-02.yaml`](mechanical-energy-gbxml-mm-2026-11-02.yaml) |
| `insert-views-from-file` | MO | [`offset-insert-views-systems-analysis-mo-2026-11-03.yaml`](offset-insert-views-systems-analysis-mo-2026-11-03.yaml) |
| `optimize-fabrication-lengths` | LO | [`fabrication-setup-and-convert-lo-2026-11-02.yaml`](fabrication-setup-and-convert-lo-2026-11-02.yaml) |
| `pick-on-screen` | NH | [`pick-on-screen-nh-2026-11-11.yaml`](pick-on-screen-nh-2026-11-11.yaml) |
| `place-column-rebar` | HY | [`rebar-hy-2026-10-19.yaml`](rebar-hy-2026-10-19.yaml) |
| `place-fabric-sheet` | KF | [`place-fabric-sheet-kf-2026-11-01.yaml`](place-fabric-sheet-kf-2026-11-01.yaml) |
| `place-fabrication-part` | MH | [`fabrication-change-hangers-place-mh-2026-11-02.yaml`](fabrication-change-hangers-place-mh-2026-11-02.yaml) |
| `place-legend-components` | NZ | [`legend-components-nz-2026-11-15.yaml`](legend-components-nz-2026-11-15.yaml) |
| `place-reinforcement-symbols` | MN | [`rebar-shape-symbols-sketch-stair-mn-2026-11-02.yaml`](rebar-shape-symbols-sketch-stair-mn-2026-11-02.yaml) |
| `place-span-direction-symbols` | KM | [`place-span-direction-symbols-km-2026-11-01.yaml`](place-span-direction-symbols-km-2026-11-01.yaml) |
| `place-structural-connections` | LE | [`place-structural-connections-le-2026-11-01.yaml`](place-structural-connections-le-2026-11-01.yaml) |
| `refit-room-elevations` | DN | [`gap-fill-views-2026-10-12.yaml`](gap-fill-views-2026-10-12.yaml) |
| `reorder-view-filters` | OM | [`reorder-view-filters-snowdon-2026-11-19.yaml`](reorder-view-filters-snowdon-2026-11-19.yaml) |
| `report-active-workset` | KO | [`report-active-workset-ko-2026-11-01.yaml`](report-active-workset-ko-2026-11-01.yaml) |
| `report-bounding-box` | NK | [`proven-reads-repaired-nk-2026-11-11.yaml`](proven-reads-repaired-nk-2026-11-11.yaml) |
| `report-structural-settings` | MF | [`structural-energy-settings-reads-revit2024-2026-11-14.yaml`](structural-energy-settings-reads-revit2024-2026-11-14.yaml) |
| `run-heating-cooling-loads` | LU | [`run-heating-cooling-loads-lu-2026-11-02.yaml`](run-heating-cooling-loads-lu-2026-11-02.yaml) |
| `run-systems-analysis` | MO | [`offset-insert-views-systems-analysis-mo-2026-11-03.yaml`](offset-insert-views-systems-analysis-mo-2026-11-03.yaml) |
| `set-active-workset` | KN | [`set-active-workset-kn-2026-11-01.yaml`](set-active-workset-kn-2026-11-01.yaml) |
| `set-parameter-values-by-id` | OH | [`field-853-repairs-oh-2026-11-16.yaml`](field-853-repairs-oh-2026-11-16.yaml) |

### Values that need the model's own name

Nothing recorded them, so none was invented. Write each one into its file before the run.

- `activate-document` - documentPath needs the FULL path of %TEMP%\heron-work\ol\ol-host.rvt and ol-family.rfa. The card does not expand %TEMP%, and the user folder differs between his PCs in the records (C:\Users\AjmalAlavudheen in the v2 proof, C:/Users/ajmal in the 2020 job files), so the path cannot be named here. OL0 (both files made, saved and open in a window) has not been run.
- `add-family-connector` - the scratch family's own document title (no group row, cases.yaml or job file records it) - in: left out, the job runs on the document in front; add in: once saved
- `add-family-connector` - OT15's first call and OT16's second need the raised face's id / the plane's id - left as by-hand lines in the note; the batch pair (Discharge Flange Top facing up vs Discharge Face facing up) needs no id
- `apply-view-filter` - OR27's filter on P1102 MEP NR is 'any filter' - no filter name is recorded there, so OR27 (a)-(c) stay by hand
- `apply-view-filter` - the new project's title is 'Project1' only if Revit gives it that title - write the real one in in:
- `arrange-tags` - FloorPlan: Level 1's scale is not recorded - the gap in the run record is 2 mm times it. OR18's own plan (P1102 MEP NR, 349 tags, Revit 2020, kept once) stays the owner's chat check; OQ1/OQ2's oq-blank.rvt is not made.
- `array-family-forms` - the scratch family's own document title (no group row, cases.yaml or job file records it) - in: left out, the job runs on the document in front; add in: once saved
- `array-family-forms` - LEG-FORM-ID placeholder: the Leg End extrusion's unique id, known only once the owner makes it
- `associate-analytical-elements` - none typed - but M9 must be KEPT with try-once (MG4) and M9 + B1 selected by hand before the run; setup is empty, the card binds Revit's selection
- `auto-size-mep` - OU19's Revit 2020 leg: no 2020 model with a 500 L/s branch, and no duct id for one, is recorded - needs the model's own name and the branch's id; only the 2024 leg is in the job, on version 1's proof model and ducts (ids from the proof record: positive 1447716, 1447725 sized; negative 1448416, 1448425, 1448439 in noFlow)
- `check-link-coordinates` - OU4's scratch project holding two Revit links (one loaded, placed by shared coordinates; one unloaded) is not named anywhere - 'Project1' is the never-saved scratch of rounds 16-19 (2026-10-18), used here only as the likely title; type the real one
- `combine-family-forms` - the scratch family's own document title (no group row, cases.yaml or job file records it) - in: left out, the job runs on the document in front; add in: once saved
- `combine-family-forms` - PLATE-ID, VOID-GROUP-ID (first id of the kept array's members), LINES-GROUP-ID (hand-made group of two model lines) placeholders - exist only after the owner keeps the arrangement (ARRAY_FAMILY_FORMS is DRAFT, so a try-once)
- `convert-cad-to-directshape` - What Roofing Detail.dwg holds was never recorded; it sits in a drafting view (round 7 selected it in 'Roofing Termination Detail'), so it may be 2D with no solids -) POSITIVE EMPTY (created 0, skippedSolids 0). If so the card needs the owner's hands: a 3D DWG imported into the copy and its category name (the file name as the model shows it) written in categoryName. No 3D DWG is recorded anywhere.
- `copy-elements` - `in:` - group-pj.md names no title for its new scratch project; left out, header tells the owner to add `in:` once it is saved and named or to have it in front
- `copy-view-filters` - OR17's arrangement (a filter moved out of id order with REORDER_VIEW_FILTERS and one with Enable Filter unticked, kept) and OR28's two templated target plans are not recorded on any model
- `count-by-spatial-container` - OR25's two-storey scratch model (Building Story on both levels, stacked spaces, a duct accessory in the void) is not made or named; the job proves the v3 label fix on P1102 MEP's own spaces with the v2 proof's ZZZNOTHINGHERE negative. OR25 itself is one validate on that model, containerKind=space.
- `create-family-forms` - PB's own OT2/OT3 Metric Generic Model family has no recorded name; the v4 proof's P1102 Family is used
- `create-grids` - `in:` - group-pi.md names no title for its new scratch project; left out, header tells the owner to add `in:` once it is saved and named or to have it in front
- `create-hvac-zone` - OR4 asks two spaces; only Vest. 101's id (918729) is recorded, so the job selects Level 1's 31 spaces by level (NR7's count)
- `create-radial-dimension` - the scratch's document name - MA1 never recorded one, so no in:
- `create-radial-dimension` - arc line L1's element id (placeholder "line L1'S ID", as the MA job file does)
- `create-radial-dimension` - a straight grid named A must be added - cases.yaml's negative; MA1 has no grid
- `create-sheets` - the saved copy's document title (in:) - recorded nowhere, OZ's file leaves it out too
- `create-sheets` - PE3's own negative needs 'a number already in the register' - OZ1 records it, nowhere yet; the job's negative is a title block name the model has not got instead
- `create-workset-3d-views` - The two user worksets' names were never recorded - not needed to run; the note asks for them to be written on the row.
- `describe-blank-parameters` - Comments on these ten ducts was never read on P1102 MEP (on Snowdon-scratch M1 all 22 were blank, row 81). If all ten carry one, the positive is empty from the model; any parameter the ducts carry and leave empty is then the positive.
- `edit-text-values` - OU1 names only 'a scratch copy of rac_basic_sample_project.rvt (Level 1 id 311, view 312)'; 'House Scratch 2' is the copy Group HP ran the same rename on (HP1-HP4, 2026-11-20, level 311, view 312). If another copy is used, its name goes in in:
- `export-families` - folder written as C:/Users/ajmal/AppData/Local/Temp/heron-work/ox/families (OX3's %TEMP%\heron-work\ox\families spelled as other job files spell %TEMP%); the owner makes it empty first
- `export-model-to-ifc` - the folder C:/Users/ajmal/AppData/Local/Temp/heron-work/prove-1102/exports is the one export-views-to-dwg's proof recorded on this model; the card refuses a missing folder
- `export-model-to-nwc` - the floor plan's exact name in roofjoin-oy - 'FloorPlan: Level 1' is night-scratch's (same DefaultMetric template), not recorded for roofjoin-oy
- `export-model-to-nwc` - whether the Navisworks NWC exporter is installed for Revit 2024 on the PC - not recorded
- `export-parameters-to-csv` - OU7's negative needs a copy whose walls carry Comments TWICE (Comments bound to Walls again as a project parameter) - no such model is named; its name goes in --in for that run
- `extend-roof-to-roof` - Wing F must be added to roofjoin-oy by hand (OY6) before the 2024 file runs
- `extend-roof-to-roof` - OY8's Revit 2020 model: the only recorded roofjoin-oy was saved by Revit 2024 and cannot open in 2020 - a rebuild by OY1's recipe plus Wing F is needed and its document name written into in:
- `filter-elements-in-room` - in: "Project1" is the title Revit gives a first new project (recorded as a Revit 2020 scratch model in other job files, not in group OT); if the rooms project is saved or titled otherwise, its own title. The rooms/doors/windows are to be built; Room 9 must have no window of its own (the negative depends on it)
- `flex-family` - the scratch family's own document title (no group row, cases.yaml or job file records it) - in: left out, the job runs on the document in front; add in: once saved
- `flip-elements` - OU6's own negative door (no room either side on any phase) has no recorded id - left to a by-hand run
- `link-family-parameter` - the scratch family's own document title (no group row, cases.yaml or job file records it) - in: left out, the job runs on the document in front; add in: once saved
- `link-family-parameter` - CAP-FORM-ID placeholder: the thin extrusion's unique id
- `link-files` - OU5's fresh scratch project is not named; 'Project1' would be Revit's default title for an unsaved one
- `list-linked-models` - OQ names its copies oq-snowdon.rvt / oq-blank.rvt for the same roles; the file uses the recorded Snowdon-scratch_ajmal.al and Project1 (PK2, cases.yaml, v1 proof) - swap in: / negative-in: if the OQ copies are the ones open
- `measure-distance` - PE0's ids of two touching ducts and two ducts 500 mm apart - not recorded, and could not be typed anyway
- `move-annotation` - OR12 names a 3.5mm Arial note; that text type is not recorded in P1102 MEP - the job uses 1.5mm Arial, recorded there by p1102-round11. The chain create-text-note -) elements is OS10's case (row 5b-1406), not yet run in Revit.
- `place-accessory-on-run` - OR7 names M_Balancing Damper - Rectangular: Standard, but no file path for it is recorded; the job loads M_Fire Damper - Rectangular - Simple, whose Revit 2020 US Metric library path IS recorded (p1102-round17). Write the balancing damper's path in to use OR7's family.
- `place-accessory-on-run` - OR7 (the lone duct along Y - version 2's first case) needs a saved scratch metric project the owner names; not in the batch because any duct handed in lands in `placed` (impl/any/fragment.cs adds the accessory to placed whether or not both ends join), so a creation-chain job has no empty negative.
- `place-fabric-reinforcement` - the rebar-hy scratch's document name (not recorded; no in:)
- `place-fabric-reinforcement` - a loaded fabric sheet type's name (HY12 says 'a loaded fabric sheet type by name', never recorded) - the negative, run first, lists them
- `place-family-instances` - the scratch family's own document title (no group row, cases.yaml or job file records it) - in: left out, the job runs on the document in front; add in: once saved
- `place-family-instances` - the Structural Precast library path of Round void : HWC 10 cm is not recorded - the owner loads it by hand (or a scratch -200 mm WP-based family, whose Family : Type then replaces symbol)
- `place-flow-arrows` - arrowFamilyName and arrowTypeName: no flow-arrow family is recorded LOADED in any model (Group I I2: "No symbol 'M_Air Flow Arrow : Standard' is loaded" in Snowdon; p1102-round16 header: no flow-arrow family in the 2024 Snowdon sample; modify-never-run.yaml's 'Flow Arrow : Standard' is marked CONFIRM). The owner's 'Diffuser Flow Arrow_DRAFT.rfa' is his own family, which Group NC says is not to be used for proofs.
- `place-hosted-family` - OU23's scratch project (walls, no rooms) - its name
- `place-hosted-family` - the door type (M_Single-Flush: 0915 x 2134mm is recorded in Group HX/OH, not for OU23)
- `place-hosted-family` - the level
- `place-hosted-family` - three door points on interior walls, a toRoomAt 600 mm into each swing side (two against Revit's default facing), a hinge point at a jamb for each
- `place-hosted-family` - (c)'s toRoomAt on the wall's own line
- `publish-coordinates` - the link INSTANCE's name as the Project Browser shows it (MO6 writes '(the instance's name)') - placeholder THE LINK INSTANCE'S NAME
- `read-element-parameters` - the diffuser scratch model's document title (in:) - no row records one; the header lists what it must hold (Level 1, Level 2 at 3000 mm, two walls, three M_Supply Diffuser - Circular - Round Neck - Ceiling Mounted on Level 1's plane, nothing else of Air Terminals) and asks the owner to write the name on OU12
- `read-room-geometry` - the scratch model's document title
- `read-room-geometry` - the names of the rooms (a room around two 4-line room-separation boxes, a room around one, a plain rectangular room) - none recorded
- `repoint-view-reference` - the target section view's name (placeholder ANOTHER SECTION VIEW'S NAME) - never recorded
- `repoint-view-reference` - the reference section must be drawn ('Reference other view') and its MARKER selected by hand; setup is empty
- `report-connectors` - PE0's round duct, rectangular duct and lighting fixture ids (for PE2's shape checks, by hand) - not recorded
- `report-divided-surfaces` - PB2 asks a new empty project; the recorded empty model of the last proof (P1102 Light24, a family) is used. PK5's divided path (model line, Divide Path 60) is a hand step
- `report-element-ownership` - local A's document title (in:) - no row records the scratch central; the positive goes to the document in front unless typed
- `report-element-ownership` - the arrangement needs the owner's hands: a central with exactly two walls, two locals under two user names, a move synchronised from B, a wall drawn in A unsynchronised
- `report-fabrication-setup` - LO1's second scratch project with NO fabrication configuration (the clean negative-in) was never named; the job uses PK6's ZZNOTASERVICE value and is expected to read NEG NOT EMPTY on configurationName (see faults)
- `report-family-forms` - PB7 proper needs OT3's family ()60 forms, name not recorded) and 20 of its form ids - not recorded, so PB7 is a hand run
- `report-family-template` - Family1 is recorded (OT19) but Revit gives the next free number: if another new family was made first in the session (the generic Family1 file makes one), the tab title replaces it
- `report-family-type-catalog` - same Family1 next-free-number caveat
- `report-level-elevations` - the diffuser scratch model's document title
- `report-linked-view-usage` - no record says which Snowdon-scratch view shows a link By Linked View or Custom; if none, the positive is EMPTY and one plan must be set by hand. PK1's Project1-with-a-link leg is a hand run
- `report-roof-slopes` - PG1/PK4's wall: roofjoin-oy as recorded (OY1 and its 2026-11-21 run) has no wall - the owner must draw one anywhere inside x 0..24000, y -6000..38000 mm and save; the negative selects Walls in a box round the whole model
- `report-structural-loads` - OU15's second release: 'a Revit 2024 scratch structural project' - no name recorded; left out of the batch, by hand
- `report-view-filters` - Project1 on Revit 2020 must hold no filter (PK8); if it has some, use P1102 Family (v5 proof's negative)
- `report-view-work-plane` - OR11's negative is a sheet; the mechanical template has none - the job uses its schedule Space Outdoor Air Schedule, as version 1's proof did
- `save-document` - HeronNest_Hub.rfa's full path is the one the v2 proof recorded (C:\Users\AjmalAlavudheen\AppData\Local\Temp\heron-work\nest); if the PC's %TEMP% differs, the owner writes the Hub's own path
- `set-design-option` - the one design option's name (placeholder THE ACTIVE OPTION'S NAME) - Group I records only that it has one; the negative lists the real names
- `set-design-option` - the option must be made ACTIVE by hand from the status bar - no API does it
- `set-element-level` - OU11's own legs (level-based mechanical equipment at 1200 mm, the ceiling diffuser on Level 1's plane) are on the unnamed diffuser scratch model - by hand, not in the batch
- `set-family-settings` - same Family1 next-free-number caveat
- `set-material-properties` - OU10's positive (Keywords carried ONCE) needs a material with one parameter called Keywords - no recorded model has one
- `set-view-filter-categories` - PE0's own filter on the PE Project1 copy is not recorded - used the Systems template's Domestic filter instead (recorded in view-filters-project1-revit2020-2026-10-15.yaml)
- `set-view-filter-categories` - the new project's title is 'Project1' only if Revit gives it that title
- `set-view-template-control` - Whether the template 'Mechanical Plan' (recorded in sweep-project1-2.yaml) includes View Scale is not recorded - check in View Templates first
- `set-worksharing-ownership` - the workshared local copy's document name (EC12 '(the local copy)'), so no in:
- `sync-with-central` - the workshared local copy's document name (EC12 writes '(the local copy)'), so no in:
