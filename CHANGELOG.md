# Changelog

What changed in each release of Heron AI, newest first, in plain words. Each release's page on
[GitHub Releases](https://github.com/Ajmalpshaik/Heron-AI/releases) carries the files, their SHA-256
checksums, and exactly what was run before it was published. Versions are `major.minor.patch`; while
Heron is `0.x`, a minor step can change how things work.

**The counts below are what each tag held**, frozen with it. Any one can be checked with
`git grep -h '^heron-status:' <tag> -- 'brain/fragments/*/fragment.yaml' | sort | uniq -c`; what holds
today is the same command on `main`, and the [Revit tool catalogue](docs/guide/revit-tool-catalogue.md).

---

## v0.3.0 - 2026-11-22

**The release that tells a modeller what Heron can do and how to install it, after a week of field
reports fixed.** 119 pull requests were merged between the v0.2.0 tag and this release's own pull
request - every one is listed at the end of this section.

### New for a modeller

- **The [Revit tool catalogue](docs/guide/revit-tool-catalogue.md)** - every live tool, by job: what it
  does, how you reach it, what to select and say, what you get, and the Revit releases it is built for
  and proved on. Written by `tools/generate-tool-catalogue.py` from the tool cards and the ribbon code,
  so nothing on it is typed by a person, and a test fails when it falls behind the cards.
- **[Installing Heron](docs/guide/install.md)** - the install from download to first request, which
  Revit releases were actually run, turning on changes, troubleshooting, updating and removing.
- **The README is a landing page** with short guide pages ([#1092](https://github.com/Ajmalpshaik/Heron-AI/pull/1092)),
  and an Install section for modellers now the "please do not install" notice is gone
  ([D-190](docs/decisions/D-190.md), [#1132](https://github.com/Ajmalpshaik/Heron-AI/pull/1132)).
- **New tools, all DRAFT until proved:** extend a roof to meet another roof
  ([#1147](https://github.com/Ajmalpshaik/Heron-AI/pull/1147), [#1168](https://github.com/Ajmalpshaik/Heron-AI/pull/1168)),
  report roof slopes ([#1192](https://github.com/Ajmalpshaik/Heron-AI/pull/1192)), move a view filter up
  or down its list ([#1027](https://github.com/Ajmalpshaik/Heron-AI/pull/1027)), and check levels and
  grids against a linked model. **Shape wall tops to the roof** is new and proved
  ([#890](https://github.com/Ajmalpshaik/Heron-AI/pull/890)).
- **"Make sheets" and "make sheets from a spreadsheet" are one tool**, CREATE_SHEETS v4
  ([D-193](docs/DECISIONS.md), [#1157](https://github.com/Ajmalpshaik/Heron-AI/pull/1157)).
- **Location data** - a room's or space's name and number into two parameters, with linked rooms when
  asked: ASSIGN_LOCATION_DATA v3, DRAFT ([#1050](https://github.com/Ajmalpshaik/Heron-AI/pull/1050)).

### Fixed from field reports

- **Tools repaired after the owner's reports** from real jobs: tags, moves and schedules
  ([#887](https://github.com/Ajmalpshaik/Heron-AI/pull/887)); parameters, placement and types
  ([#889](https://github.com/Ajmalpshaik/Heron-AI/pull/889)); roofs, wall tops and deleting
  ([#890](https://github.com/Ajmalpshaik/Heron-AI/pull/890)); MEP, view, filter and picture tools
  ([#1107](https://github.com/Ajmalpshaik/Heron-AI/pull/1107)); 21 model tools
  ([#1101](https://github.com/Ajmalpshaik/Heron-AI/pull/1101)); family-editor tools and connector faces
  ([#1108](https://github.com/Ajmalpshaik/Heron-AI/pull/1108), [#1236](https://github.com/Ajmalpshaik/Heron-AI/pull/1236));
  view filters read from the view template ([#1133](https://github.com/Ajmalpshaik/Heron-AI/pull/1133));
  flex duct and camera framing ([#1134](https://github.com/Ajmalpshaik/Heron-AI/pull/1134)); a ceiling
  sprinkler's level ([#1173](https://github.com/Ajmalpshaik/Heron-AI/pull/1173)).
- **A fixed tool goes back to DRAFT until it is proved again**, because a proof is about the code it was
  taken on (D-30). So this release has **fewer** usable tools than v0.2.0, not more: 65 cards went from
  PROVEN back to DRAFT with their fixes, and 23 were proved and signed on Revit 2024
  ([#1016](https://github.com/Ajmalpshaik/Heron-AI/pull/1016), [#1178](https://github.com/Ajmalpshaik/Heron-AI/pull/1178), [#1186](https://github.com/Ajmalpshaik/Heron-AI/pull/1186)).
  At this tag: **482 PROVEN, 129 DRAFT, 135 retired** (v0.2.0: 524, 83, 134).
- **Asking in your own words reaches the right tool more often** - plain questions no longer land on a
  tool that writes, and many requests that missed now reach their tool
  ([#1140](https://github.com/Ajmalpshaik/Heron-AI/pull/1140), [#1183](https://github.com/Ajmalpshaik/Heron-AI/pull/1183),
  [#1190](https://github.com/Ajmalpshaik/Heron-AI/pull/1190), [#1199](https://github.com/Ajmalpshaik/Heron-AI/pull/1199),
  [#1258](https://github.com/Ajmalpshaik/Heron-AI/pull/1258) and others below).

### Safer and clearer in Revit

- **Every answer ends with one outcome word**, so a chat cannot report a refusal as a success
  ([#1064](https://github.com/Ajmalpshaik/Heron-AI/pull/1064), [#1109](https://github.com/Ajmalpshaik/Heron-AI/pull/1109), [#1181](https://github.com/Ajmalpshaik/Heron-AI/pull/1181)).
- **A refusal says whether the rollback held**; a read that would commit a change is refused; a stopped
  Admin or Publish job says its outside work stands ([#1105](https://github.com/Ajmalpshaik/Heron-AI/pull/1105), [#1182](https://github.com/Ajmalpshaik/Heron-AI/pull/1182), [#1211](https://github.com/Ajmalpshaik/Heron-AI/pull/1211)).
- **The bridge keeps listening, and two open models with the same title are told apart**
  ([#1146](https://github.com/Ajmalpshaik/Heron-AI/pull/1146)).
- **A tool is skipped on a Revit release it does not claim** ([#1195](https://github.com/Ajmalpshaik/Heron-AI/pull/1195)),
  and **a tool held at DRAFT on purpose is refused as held, with its reason** ([#1255](https://github.com/Ajmalpshaik/Heron-AI/pull/1255)).
- **A write's read-back says "asked, did not change"** for elements Revit left as they were
  ([#1238](https://github.com/Ajmalpshaik/Heron-AI/pull/1238)).

### Under the hood

- **Search compares vectors in plain Python; `sqlite-vec` is removed** ([D-194](docs/DECISIONS.md), [#1189](https://github.com/Ajmalpshaik/Heron-AI/pull/1189)).
- **`opentelemetry-api` 1.44.0 -> 1.45.2** in the lock, for two security advisories ([#897](https://github.com/Ajmalpshaik/Heron-AI/pull/897)).
- **Every new finding becomes a GitHub issue at once** ([D-187](docs/DECISIONS.md), [#1097](https://github.com/Ajmalpshaik/Heron-AI/pull/1097)),
  and the defect and check registers were moved to issues and archived ([#961](https://github.com/Ajmalpshaik/Heron-AI/pull/961), [#1017](https://github.com/Ajmalpshaik/Heron-AI/pull/1017), [#1020](https://github.com/Ajmalpshaik/Heron-AI/pull/1020), [#1232](https://github.com/Ajmalpshaik/Heron-AI/pull/1232)).
- **CI builds the bridge test host and runs the MCP suites on the pinned lock** ([#1229](https://github.com/Ajmalpshaik/Heron-AI/pull/1229)).
- **The version is 0.3.0** in `Directory.Build.props`, the bridge client and `platform/heron-products.json`.

### Still owed

- **Not signed.** No code-signing certificate yet; the Setup's name says `unsigned` ([D-173](docs/decisions/D-173.md), [D-190](docs/decisions/D-190.md)).
- **The v0.3.0 Setup itself** - the release notes say what was run with it and what was not.
- **Revit 2021, 2022, 2023 and 2025 have never been run.** On 2026 and 2027 Heron is built and was
  installed by v0.2.0's Setup, with next to no tool proved on them; Revit loading a Setup's own build is
  still to be checked.

<details>
<summary>Every pull request merged between v0.2.0 and this release (119)</summary>

- [#871](https://github.com/Ajmalpshaik/Heron-AI/pull/871) 2026-11-17 - Register cleanup, fragment-issue fixes, the owner's decisions recorded, and every decided item built
- [#886](https://github.com/Ajmalpshaik/Heron-AI/pull/886) 2026-11-16 - Repair rows 5b-1750, 5b-1710, 5b-1711: family in front read back, a region of a plan, a store warning that names what to do
- [#887](https://github.com/Ajmalpshaik/Heron-AI/pull/887) 2026-11-16 - Field reports #853/#854: tags, move and schedules repaired - six cards back to DRAFT, Group OC to prove them
- [#889](https://github.com/Ajmalpshaik/Heron-AI/pull/889) 2026-11-16 - Field report #853: parameters, placement and types - rows 5b-1703 to 5b-1708, five cards back to DRAFT, Group OB
- [#890](https://github.com/Ajmalpshaik/Heron-AI/pull/890) 2026-11-16 - Field reports #852-#854: slope chosen roof edges, wall tops shaped to the roof, column top level, walls a delete detaches - Group OE (DO NOT MERGE until proven)
- [#895](https://github.com/Ajmalpshaik/Heron-AI/pull/895) 2026-11-16 - Rows 5b-1739, 5b-1740 and 5b-1821: circuit-naming asks when two parameters share a word, need notes below a name are served, run records say the door's word
- [#897](https://github.com/Ajmalpshaik/Heron-AI/pull/897) 2026-11-16 - opentelemetry-api 1.44.0 -> 1.45.2 in requirements-lock.txt, for Dependabot alerts 1 and 2 (GHSA-xcq7-p3pf-p88h, GHSA-mf5v-w3w3-f6cg)
- [#961](https://github.com/Ajmalpshaik/Heron-AI/pull/961) 2026-11-16 - Open defect rows moved to 63 GitHub issues (#898-#960), finished rows closed
- [#1010](https://github.com/Ajmalpshaik/Heron-AI/pull/1010) 2026-11-18 - Research note 51 and its build order: QA reports, office checklists, Group Policy, provenance; D-177 to D-180
- [#1016](https://github.com/Ajmalpshaik/Heron-AI/pull/1016) 2026-11-17 - Night 2026-11-16/17 on Revit 2024: Group OI - 21 DRAFT cards proved and signed by Ajmal PS, rows 5b-1841..1843
- [#1017](https://github.com/Ajmalpshaik/Heron-AI/pull/1017) 2026-11-16 - Questions, NEEDS-CHECKING groups and work-note items tracked as GitHub issues (#962-#1015)
- [#1018](https://github.com/Ajmalpshaik/Heron-AI/pull/1018) 2026-11-16 - Archive tools: NEEDS-CHECKING layout no longer crashes (5b-1583, 5b-1752); moved rows can be archived
- [#1019](https://github.com/Ajmalpshaik/Heron-AI/pull/1019) 2026-11-17 - Sitting 2026-11-16/17: Groups OF, OB, EO, NV on Revit 2024 - 6 PASS, 2 NEEDS REVIEW, the rest NOT RUN with why, row 5b-1844 (records only)
- [#1020](https://github.com/Ajmalpshaik/Heron-AI/pull/1020) 2026-11-17 - Archive: 509 finished or GitHub-tracked defect rows and 169 struck checks moved out of the two big registers
- [#1021](https://github.com/Ajmalpshaik/Heron-AI/pull/1021) 2026-11-19 - refit-room-elevations v2: find the elevation marker's room through a floor plan (row 5b-1859, repairs 5b-1843)
- [#1022](https://github.com/Ajmalpshaik/Heron-AI/pull/1022) 2026-11-19 - Row 5b-1847: plain "add a wall" sentences reach CREATE_WALL - repairs row 5b-1844
- [#1023](https://github.com/Ajmalpshaik/Heron-AI/pull/1023) 2026-11-19 - Reference planes on a coloured subcategory: widen CREATE_LINE_STYLE and SET_FAMILY_PLANE_REFERENCE (both DRAFT)
- [#1024](https://github.com/Ajmalpshaik/Heron-AI/pull/1024) 2026-11-19 - Row 5b-1850: FRAME_ELEMENTS_IN_CAMERA measures the eye from the lowest level, not the busiest
- [#1025](https://github.com/Ajmalpshaik/Heron-AI/pull/1025) 2026-11-19 - Row 5b-1851: an unasked save when ACTIVATE_DOCUMENT switches - v3 reads it back
- [#1026](https://github.com/Ajmalpshaik/Heron-AI/pull/1026) 2026-11-19 - D-166 is built, not yet run in Revit: its Owed section and the D-19, D-106, D-150 lines say so
- [#1027](https://github.com/Ajmalpshaik/Heron-AI/pull/1027) 2026-11-19 - REORDER_VIEW_FILTERS at DRAFT: move one filter up or down a view's filter list
- [#1037](https://github.com/Ajmalpshaik/Heron-AI/pull/1037) 2026-11-19 - Group OK: tracked as GitHub issue #1034
- [#1045](https://github.com/Ajmalpshaik/Heron-AI/pull/1045) 2026-11-19 - Issues #1036, #1038, #1039 for what #1010 left owed: Group OJ, Q-93, row 5b-1848
- [#1046](https://github.com/Ajmalpshaik/Heron-AI/pull/1046) 2026-11-19 - Row 5b-1850 moves to issue #908: the camera fix's Revit run is owed there
- [#1048](https://github.com/Ajmalpshaik/Heron-AI/pull/1048) 2026-11-19 - Row 5b-1700 moved to issue #910: its one open question is the owner's
- [#1049](https://github.com/Ajmalpshaik/Heron-AI/pull/1049) 2026-11-19 - Row 5b-1851 and Group OL: tracked as GitHub issue #1035
- [#1050](https://github.com/Ajmalpshaik/Heron-AI/pull/1050) 2026-11-20 - ASSIGN_LOCATION_DATA v3: room or space name and number into two parameters, storey checked, linked rooms when asked - DRAFT
- [#1061](https://github.com/Ajmalpshaik/Heron-AI/pull/1061) 2026-11-20 - Tick what #871 finished, and bring ISSUES.md up to date
- [#1064](https://github.com/Ajmalpshaik/Heron-AI/pull/1064) 2026-11-20 - Chat-side outcome words, server fixes and job methods (#1043, #952, #953, #960)
- [#1065](https://github.com/Ajmalpshaik/Heron-AI/pull/1065) 2026-11-20 - Stage 2: #1032 resolve names a retired tool's replacement; #937, #1015, #1007 registers and work notes tidied
- [#1068](https://github.com/Ajmalpshaik/Heron-AI/pull/1068) 2026-11-20 - Fix 15 rows of issues #936, #938, #939, #940: register tools, gates, test sweep, brain
- [#1073](https://github.com/Ajmalpshaik/Heron-AI/pull/1073) 2026-11-20 - Revit runs 2026-11-20: #814, #1030 closed; #1042, #1044, #899, #904, #909, #1036 runs (rows 5b-1870, 1871, 1876, 1881, 1882)
- [#1074](https://github.com/Ajmalpshaik/Heron-AI/pull/1074) 2026-11-20 - Owner's answers in code: empty absent binds, dictionary entries whole, one-reason refusals as strings, honest rollback verdict (#1041, #912, #911)
- [#1075](https://github.com/Ajmalpshaik/Heron-AI/pull/1075) 2026-11-20 - Owner's answers for #1038, #916, #911 and #925; return card v10 skips a blank cell first
- [#1076](https://github.com/Ajmalpshaik/Heron-AI/pull/1076) 2026-11-20 - Seven Revit tools changed on the owner's answers: #924, #921, #926 (5b-1427, 5b-414)
- [#1084](https://github.com/Ajmalpshaik/Heron-AI/pull/1084) 2026-11-20 - Rows 5b-472, 5b-1548, 5b-1671: one QA label, stale DRAFT words, roof joins (#926, #918)
- [#1085](https://github.com/Ajmalpshaik/Heron-AI/pull/1085) 2026-11-20 - After #1068: owner's answer on 5b-1162, new rows moved to issues #1081-#1083, PC runs NI7 and NI8 tracked
- [#1086](https://github.com/Ajmalpshaik/Heron-AI/pull/1086) 2026-11-20 - Revit runs 2026-11-20, part 2: HP1-HP5, BB4, DW3, DW4, JG3 held; row 5b-1221 finding
- [#1091](https://github.com/Ajmalpshaik/Heron-AI/pull/1091) 2026-11-20 - Revit runs 2026-11-20, part 3: BC5, EE7, NG6, CL9, CL10 held; EE8 (b), CL11 2020 half
- [#1092](https://github.com/Ajmalpshaik/Heron-AI/pull/1092) 2026-11-20 - Docs: README becomes a landing page, with short linked guide pages and brand diagrams
- [#1093](https://github.com/Ajmalpshaik/Heron-AI/pull/1093) 2026-11-20 - Revit runs 2026-11-20, part 4: NP5, BZ7 held; CI6 Material half
- [#1094](https://github.com/Ajmalpshaik/Heron-AI/pull/1094) 2026-11-20 - Register tidy-up: #1066 recorded, next-steps note retired, spec parts 00-00d read whole, sheets duplicate check (#937 #1007 #1015 #1066 #1072 #1080)
- [#1096](https://github.com/Ajmalpshaik/Heron-AI/pull/1096) 2026-11-20 - Knowledge library rules (#1006): version check, add-on sizes, packet confidence, licence and BIM-standard records, plain words
- [#1097](https://github.com/Ajmalpshaik/Heron-AI/pull/1097) 2026-11-20 - Every new finding is a GitHub issue at once (D-187): open-issue tool and heron-issue-recorder agent
- [#1098](https://github.com/Ajmalpshaik/Heron-AI/pull/1098) 2026-11-20 - Windows-PC test fixes: three suites (#1055) and the routing suites on the trained model (#958)
- [#1101](https://github.com/Ajmalpshaik/Heron-AI/pull/1101) 2026-11-20 - Fix 21 model tools for #946-#950, #1005, 5b-1881 and 5b-1221 - all DRAFT, Group OU
- [#1105](https://github.com/Ajmalpshaik/Heron-AI/pull/1105) 2026-11-20 - Add-in fixes for #932, #933, #934, #1057: refusals say whether the rollback held, waits name their run, undo reaches the run record, bridge caps an answer
- [#1106](https://github.com/Ajmalpshaik/Heron-AI/pull/1106) 2026-11-20 - Fix seven register, proof, gate and resolver issues (#1056, #1062, #1067, #1071, #1081, #1083)
- [#1107](https://github.com/Ajmalpshaik/Heron-AI/pull/1107) 2026-11-20 - MEP, view, filter and picture tools: fixes for #942, #943, #951, #1053, #1058, #935, #1060 (DRAFT until Group OR runs)
- [#1108](https://github.com/Ajmalpshaik/Heron-AI/pull/1108) 2026-11-20 - Family-editor fixes: 13 tools back to DRAFT with Group OT, tall geometry in columns, connector face pick (#944, #945, #1029, #1028, #1051, #1044)
- [#1109](https://github.com/Ajmalpshaik/Heron-AI/pull/1109) 2026-11-20 - Chat-side outcome words, the model hold after a restart, health and the pin, two add-in fixes (#952 #953 #960 #1031 #1033 #1043 #1082)
- [#1120](https://github.com/Ajmalpshaik/Heron-AI/pull/1120) 2026-11-20 - Revit 2024 runs: OI28, OM2-OM6, DX4, EA2, DH9/ER10 2024 halves; 5b-1842 cause
- [#1124](https://github.com/Ajmalpshaik/Heron-AI/pull/1124) 2026-11-20 - Routing: the last two renames on the owner's backend (#958, row 5b-1882)
- [#1129](https://github.com/Ajmalpshaik/Heron-AI/pull/1129) 2026-11-20 - Track rows 5b-1884 and 5b-1923 in #1125; record the owner's answer on 5b-1862
- [#1131](https://github.com/Ajmalpshaik/Heron-AI/pull/1131) 2026-11-20 - D-187 write-backs for #1101's findings: issues #1126, #1128, #1130
- [#1132](https://github.com/Ajmalpshaik/Heron-AI/pull/1132) 2026-11-20 - README: modellers can install now - an Install section for the unsigned v0.2.0 (D-190)
- [#1133](https://github.com/Ajmalpshaik/Heron-AI/pull/1133) 2026-11-20 - View filter template checks read from the template: APPLY_VIEW_FILTER v3 and COPY_VIEW_FILTERS v3 (rows 5b-1916, 5b-1917)
- [#1134](https://github.com/Ajmalpshaik/Heron-AI/pull/1134) 2026-11-20 - convert-to-flex-duct v2 (#1119), frame-elements-in-camera v8 (5b-1870), Group JB always vertical (5b-1883)
- [#1135](https://github.com/Ajmalpshaik/Heron-AI/pull/1135) 2026-11-21 - Usage counter (#1006 WN-12, D-191): which clauses helped, on the modeller's word, nudging less than one rank
- [#1137](https://github.com/Ajmalpshaik/Heron-AI/pull/1137) 2026-11-20 - Docs tidy-up: PROJECT-MAP names heron-issue-recorder, Status links point at What is proven, stale words of rows 5b-1892/1894/1895/1897 (#1113 #1117 #1099)
- [#1139](https://github.com/Ajmalpshaik/Heron-AI/pull/1139) 2026-11-20 - Tool fixes: bare-catch share, code read as links, one tree per score-routing run (#1122, #1095, #1089)
- [#1140](https://github.com/Ajmalpshaik/Heron-AI/pull/1140) 2026-11-20 - Routing: plain questions off writes, requests to their tools, two lookup checks (#954-#959, #1039, #1054, #1078)
- [#1141](https://github.com/Ajmalpshaik/Heron-AI/pull/1141) 2026-11-20 - Row 5b-1882 part 1: the owner's insulation question reaches READ_ELEMENT_PARAMETERS
- [#1146](https://github.com/Ajmalpshaik/Heron-AI/pull/1146) 2026-11-20 - Issue #1102: the bridge keeps listening, same-title models told apart, the chat reads the add-in's new fields
- [#1147](https://github.com/Ajmalpshaik/Heron-AI/pull/1147) 2026-11-20 - EXTEND_ROOF_TO_ROOF (DRAFT): a roof by extrusion extended to meet another roof, measured by rays (#1087)
- [#1148](https://github.com/Ajmalpshaik/Heron-AI/pull/1148) 2026-11-20 - Diagrams in the other docs move to the brand palette - colours only (#1116, D-149)
- [#1150](https://github.com/Ajmalpshaik/Heron-AI/pull/1150) 2026-11-21 - D-192 (Proposed): a new family and an export of families open no transaction group in the model (#1121, #941)
- [#1152](https://github.com/Ajmalpshaik/Heron-AI/pull/1152) 2026-11-21 - Row 5b-1882 tracked as issue #1127 (D-187 housekeeping after #1140)
- [#1154](https://github.com/Ajmalpshaik/Heron-AI/pull/1154) 2026-11-21 - Checking-tool fixes: row_links, batch-prove legs and retired cards, next-id reads issues (#1138, #1149, #1153, #1145)
- [#1155](https://github.com/Ajmalpshaik/Heron-AI/pull/1155) 2026-11-21 - The owner's delegated choices: NO6 is a Revit run, the ribbon note becomes docs/52, two agents are T1 (#1015 #1007 #937)
- [#1157](https://github.com/Ajmalpshaik/Heron-AI/pull/1157) 2026-11-21 - "Make sheets" and "Make sheets from a spreadsheet" become one tool: CREATE_SHEETS v4 (DRAFT), D-193 (#1080)
- [#1168](https://github.com/Ajmalpshaik/Heron-AI/pull/1168) 2026-11-21 - Roof by footprint extended from 2022 (EXTEND_ROOF_TO_ROOF v2, DRAFT); door and roof questions reach their reads (#1143, #1130, #1078)
- [#1171](https://github.com/Ajmalpshaik/Heron-AI/pull/1171) 2026-11-21 - batch-prove gives the refusal as DID NOT RUN's reason; ACTIVATE_DOCUMENT is offered for opening a file by path (rows 5b-1942, 5b-1943)
- [#1173](https://github.com/Ajmalpshaik/Heron-AI/pull/1173) 2026-11-21 - REPORT_SPRINKLER_NETWORK v2: a ceiling sprinkler's level from Schedule Level (#1126); Group PD for row 106
- [#1178](https://github.com/Ajmalpshaik/Heron-AI/pull/1178) 2026-11-21 - Revit 2024 runs 2026-11-21: OI31 and Group OY pass, OU21/OU22 on 2024, four faults recorded
- [#1179](https://github.com/Ajmalpshaik/Heron-AI/pull/1179) 2026-11-21 - Record row 5b-1952: questions about a Revit file by its path miss READ_MODEL_FILE_INFO (#1177)
- [#1180](https://github.com/Ajmalpshaik/Heron-AI/pull/1180) 2026-11-21 - No-Revit fixes for #950, #1005, #1112, #1114: five cards DRAFT (Group PE), open-documents read answers whatever the pin
- [#1181](https://github.com/Ajmalpshaik/Heron-AI/pull/1181) 2026-11-21 - Every tool that labels its ending says an outcome word; the doors and heron_resolve choose a provider in one order (#952 #953 #1033 #1043)
- [#1182](https://github.com/Ajmalpshaik/Heron-AI/pull/1182) 2026-11-21 - Add-in: a stopped Admin/Publish job says its outside work stands; a read that commits is refused; memory and pipe-client log lines (#1151, #932)
- [#1183](https://github.com/Ajmalpshaik/Heron-AI/pull/1183) 2026-11-21 - Routing: a level's elevation, a roof by footprint, filter order, joins, spreadsheet return and datum copy reach their tools (#1158 #1159 #1054, part of #956 #1039)
- [#1184](https://github.com/Ajmalpshaik/Heron-AI/pull/1184) 2026-11-21 - Skills, job methods and two tool messages (#959, #960, #1125)
- [#1185](https://github.com/Ajmalpshaik/Heron-AI/pull/1185) 2026-11-21 - Family tools for #1111: divided-surface counts, a loop crossing itself, lists printed twice - all DRAFT, Group PB
- [#1186](https://github.com/Ajmalpshaik/Heron-AI/pull/1186) 2026-11-21 - Group OX on Revit 2024: OX1 and OX2 pass (D-192)
- [#1189](https://github.com/Ajmalpshaik/Heron-AI/pull/1189) 2026-11-21 - Search: name a card the model cuts before its job (5b-320); D-194 - vectors compared in plain Python, sqlite-vec removed (5b-1171, 5b-1955); 5b-107 closed
- [#1190](https://github.com/Ajmalpshaik/Heron-AI/pull/1190) 2026-11-21 - Routing words: questions that reached a write now reach the read (#1130 fixed; #954, #955, #1078, #1127 partly)
- [#1191](https://github.com/Ajmalpshaik/Heron-AI/pull/1191) 2026-11-21 - A level's elevation goes to the PROVEN write: row 5b-1938's sentences move to WRITE_ELEMENT_PARAMETERS (5b-1950 part, 5b-1947 tried)
- [#1192](https://github.com/Ajmalpshaik/Heron-AI/pull/1192) 2026-11-21 - REPORT_ROOF_SLOPES (DRAFT), a read for a roof's pitch; roof count and touching questions reach reads (#1161)
- [#1193](https://github.com/Ajmalpshaik/Heron-AI/pull/1193) 2026-11-21 - Object-style material questions reach LIST_CATEGORIES more often (#1188, half repaired)
- [#1195](https://github.com/Ajmalpshaik/Heron-AI/pull/1195) 2026-11-21 - The doors skip a card that does not claim the Revit in front (row 5b-1131, #953)
- [#1198](https://github.com/Ajmalpshaik/Heron-AI/pull/1198) 2026-11-21 - A skill runner (row 141), and the ten skill job files made current (row 5b-1956)
- [#1199](https://github.com/Ajmalpshaik/Heron-AI/pull/1199) 2026-11-21 - Routing, the rest of #956: keynote, text-note wording, viewports and walls not joining fixed; 1832, 1946, 1950 moved on
- [#1205](https://github.com/Ajmalpshaik/Heron-AI/pull/1205) 2026-11-21 - Grids say what they made in findings, an offset copy needs only the offset, a list printed twice is named (#1174, #1175, #1162)
- [#1206](https://github.com/Ajmalpshaik/Heron-AI/pull/1206) 2026-11-21 - Issues #944 and #945: the Revit checks rows 5b-1351 and 5b-1702 still owe, and OT29 and OT30 brought up to date
- [#1208](https://github.com/Ajmalpshaik/Heron-AI/pull/1208) 2026-11-21 - Open items of #935, #1057, #1006: an owed Revit reading (OR29), the owner's questions recorded (Q-95, Q-96), Q-94 answered, open-issue.py finds colour-marked questions
- [#1210](https://github.com/Ajmalpshaik/Heron-AI/pull/1210) 2026-11-21 - Job methods say each card's real status; builders work step 5 keeps the face (#960)
- [#1211](https://github.com/Ajmalpshaik/Heron-AI/pull/1211) 2026-11-21 - A lost read answer says it reached Revit; a carried id list mixing two models is refused; D-195 Proposed for Finalize (#932, #933, #934)
- [#1212](https://github.com/Ajmalpshaik/Heron-AI/pull/1212) 2026-11-21 - WN-7 (#1005): eight reading tools count what they pass over (D-64) - all DRAFT, Group PK
- [#1215](https://github.com/Ajmalpshaik/Heron-AI/pull/1215) 2026-11-21 - Warnings name their elements; a picture's longest side is the width asked (5b-1889, 5b-1890, #1112) - Group PM
- [#1223](https://github.com/Ajmalpshaik/Heron-AI/pull/1223) 2026-11-21 - Row 132: three skill sentences reach the read that answers them (#959)
- [#1224](https://github.com/Ajmalpshaik/Heron-AI/pull/1224) 2026-11-21 - Issue #1028: no save-helper model - the family in front is saved after the group; family checks owed
- [#1226](https://github.com/Ajmalpshaik/Heron-AI/pull/1226) 2026-11-21 - Show door offers only what it runs (#1176); file-info, roof pitch and object-style routing (#1177, #1187, #1188); #1039 and #1169 measured
- [#1228](https://github.com/Ajmalpshaik/Heron-AI/pull/1228) 2026-11-21 - House-rule skills checked against main: four corrected, one defect recorded (row 5b-1963, #1222)
- [#1229](https://github.com/Ajmalpshaik/Heron-AI/pull/1229) 2026-11-21 - CI tests job: build the bridge host, run the MCP SDK suites on the pinned lock - not-runnable list 10 -> 1
- [#1230](https://github.com/Ajmalpshaik/Heron-AI/pull/1230) 2026-11-21 - Job methods stop serving a closed GUID gap and three stale status lines (#960, row 5b-1981)
- [#1232](https://github.com/Ajmalpshaik/Heron-AI/pull/1232) 2026-11-21 - Register audit: every owed group tracked by an open issue, done checks taken off (D-187)
- [#1235](https://github.com/Ajmalpshaik/Heron-AI/pull/1235) 2026-11-21 - Work notes: three finished notes retired, every open item linked to its issue
- [#1236](https://github.com/Ajmalpshaik/Heron-AI/pull/1236) 2026-11-21 - ADD_FAMILY_CONNECTOR v7 - only a face facing out, at the point given, counts (#1029); #1051 closed out, item D fixed (#1227) - DRAFT, Group PO
- [#1237](https://github.com/Ajmalpshaik/Heron-AI/pull/1237) 2026-11-21 - Gate knows a declared dropped count; category overrides says NOT READ on a failed read
- [#1238](https://github.com/Ajmalpshaik/Heron-AI/pull/1238) 2026-11-22 - Row 5b-1903, D-196: a write's read-back says "asked, did not change" for handed elements Revit did not change (closes #1057)
- [#1239](https://github.com/Ajmalpshaik/Heron-AI/pull/1239) 2026-11-21 - Status pages after the merges of 2026-11-21: handover, balance of work, README's proven line
- [#1240](https://github.com/Ajmalpshaik/Heron-AI/pull/1240) 2026-11-22 - Thirteen open 5b rows moved to issues; five fixed (5b-1833, 1834, 1837, 1838, 1883) with Group PP
- [#1241](https://github.com/Ajmalpshaik/Heron-AI/pull/1241) 2026-11-21 - Review of the 2026-11-21 merges: stale text corrected, retired cards refused by two report tools, one owner decision recorded (5b-1967)
- [#1255](https://github.com/Ajmalpshaik/Heron-AI/pull/1255) 2026-11-21 - A tool held at DRAFT on purpose is refused as held, with its reason (row 5b-1960, #1209)
- [#1256](https://github.com/Ajmalpshaik/Heron-AI/pull/1256) 2026-11-21 - Row 132: record the owner's-PC re-ranker result (#1223)
- [#1257](https://github.com/Ajmalpshaik/Heron-AI/pull/1257) 2026-11-21 - Job files for every DRAFT card whose NEEDS-CHECKING group had none, per release and model, with an index (OY6-OY8 included) - not run
- [#1258](https://github.com/Ajmalpshaik/Heron-AI/pull/1258) 2026-11-21 - Routing words: duct tags, load units, area schemes and doors in a wall reach their tool; 5b-1832 fixed (#955, #956, #958, #1127)
- [#1261](https://github.com/Ajmalpshaik/Heron-AI/pull/1261) 2026-11-21 - PC run: re-ranked routing for rows 5b-1548, 5b-1671, 5b-472 (#1090)
- [#1263](https://github.com/Ajmalpshaik/Heron-AI/pull/1263) 2026-11-22 - Row 5b-1986 (#1262): a decision marked Accepted on words the owner did not say
- [#1264](https://github.com/Ajmalpshaik/Heron-AI/pull/1264) 2026-11-22 - Housekeeping 2026-11-22: branches cleared, one stale pointer fixed

</details>

---

## v0.2.0 - 2026-11-16

**One file to install.** `HeronSetup-0.2.0-unsigned.exe` puts Heron into every Revit on the PC, 2020 to
2027, for your own Windows user, with no administrator rights, and adds Heron to **Settings › Apps ›
Installed apps** with Uninstall ([D-167](docs/decisions/D-167.md)). Its window is Heron's own branded
installer, with two ticks: *Set up Heron's AI side too* and *Add Heron to Claude Code*.

- Run on the owner's PC on 2026-11-16: installed into Revit 2020, 2024, 2026 and 2027, its Python side
  said READY, and Uninstall took everything back out. Revit 2021, 2022, 2023 and 2025 not run.
- Not signed. At this tag: 524 PROVEN, 83 DRAFT, 134 retired.
- [Release page](https://github.com/Ajmalpshaik/Heron-AI/releases/tag/v0.2.0).

## v0.1.0 - 2026-10-08

**The first release**, published on the owner's word so a new teammate could install Heron.
`heron-project.zip` holds the Heron folder with `HeronInstaller.exe` at its top; the installer
downloads the add-in for each Revit ticked and checks every file against `checksums.txt`. Python and
its packages were set up by hand. Not signed. At this tag: 321 PROVEN, 158 DRAFT.
[Release page](https://github.com/Ajmalpshaik/Heron-AI/releases/tag/v0.1.0).
