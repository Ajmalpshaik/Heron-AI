<!--
Heron-Agent:  none
Heron-Step:   18
Heron-Status: DRAFT
Heron-Since:  0.1.0
Heron-Layer:  brain
See docs/29-metadata-standard.md
-->

# 51 — Studying another Revit MCP platform: a scoreboard, and what Heron should build from it

> **Status: research note, 2026-11-16, corrected 2026-11-17. Nothing copied.** Asked by the owner:
> *"Research this repo, each and everything... whatever things you find it is best to adopt... make a
> scoreboard like my MCP and his MCP. Not only MCP, the platform itself."*
>
> **The project is not named here, nor its author** - Heron takes the idea, never the code, so it has no
> reason to carry the name ([D-177](decisions/D-177.md)). It is an open-source Revit MCP platform under
> the MIT licence, read at one commit on 2026-11-16. Below it is called **the other platform**.
>
> **How it was read.** In full, by six readers in parallel: every Markdown file, every C# file of its
> add-in, core library, batch supervisor and updater, its whole Python server, and its build, CI and
> installer folders; its tests were skimmed for approach. Nothing of it was built or run. A seventh
> reader mapped Heron with the four labels Heron always uses, and the key claims on both sides were then
> checked by hand.
>
> **Corrected 2026-11-17.** Checking each item before building it found that five were already in
> Heron, which the first map had missed: the per-release check, a lost answer read by the request's own
> name, a picture of chosen elements, registering Heron with the AI app, and release checksums. §2 and
> §3 now say so.

---

## 0. In one paragraph

The other platform is a **finished product around a smaller, riskier core**. Its changes are **on from
install**, and its confirmation is a token **the AI app can send back by itself** (its own docs say so).
But much *around* the change is built further than Heron's: a batch over a folder of models, audit
reports in Excel, HTML and BCF, MSI installers with Group Policy templates, build provenance on every
release, and a smoke run on every Revit year it supports. **Heron leads where it matters most - safety,
proof per operation, and the depth of its library and MEP engineering. The other platform leads on the
platform around the change.** On the scoreboard (§2) the other platform is ahead overall; most of its
lead is work Heron has not built yet, not work Heron built worse.

---

## 1. What it is, measured

| | |
|---|---|
| Parts | A Revit add-in (C#), a core library with **no Revit reference** (unit-tested without Revit), a batch supervisor, an updater, a Python MCP server |
| Revit | **2022 - 2027** only. net48 for 2022-2024, net8 for 2025-2026, net10 for 2027 |
| Tools | **89**, asserted by its own test; 36 annotated read-only |
| Transport | A named pipe per Revit process; a file channel as fallback; optional HTTP on loopback with a token; and the whole server over **SSH from a Mac** |
| Tests | About 1,089 C# tests and about 900 Python tests, none needing Revit; **31 tests that run inside a real Revit**, opt-in |
| Real-Revit evidence | A release smoke script run per year - load, ping, move then undo, picture, read-only refusal - with a per-year PASS table on Autodesk sample models, and a batch of 13 real models (11 completed) |
| Honest limits | "The token does not prove human approval." Its AI-evaluation and performance tables are **empty on purpose**. Only one AI client has a recorded live run |
| Stale in places | Its own docs disagree with its code in about eight places |

Heron's side, the same day. Every count is a command, not a number in this document:

- Tools: `python mcp/server/heron_tools.py`. Off Windows only the library tools are served ([D-132](decisions/D-132.md)).
- Fragments by state: `grep -h '^heron-status:' brain/fragments/*/fragment.yaml | sort | uniq -c`.
- Revit releases loaded in a real Revit: [docs/16 §6](16-version-support-strategy.md) - 2020, 2024, 2026, 2027.

---

## 2. The scoreboard

**Scores are judgement, 0-5, made on the evidence in the last column, not measurements.** A feature that
is only built scores lower than one run in a real Revit, on either side. Heron's evidence uses the four
labels from [AGENTS.md](../AGENTS.md): BUILT, RUN IN REAL REVIT, DESIGNED ONLY, NOT PRESENT.

| Area | Heron | Other | The evidence |
|---|:-:|:-:|---|
| **Safety** | | | |
| Changes off until the modeller turns them on | **5** | 2 | Heron: off by default, and the AI cannot switch them on ([D-166](decisions/D-166.md), built, not yet run). Other: changes on from install; read-only is opt-in |
| A real person approves the change | **4** | 2 | Heron: Keep or Discard inside Revit, run on 2024 and 2027 ([D-126](decisions/D-126.md)); Ask to Keep is off by default ([D-138](decisions/D-138.md)). Other: a token the AI app can confirm itself |
| One undo per change, full rollback on failure | 5 | 5 | Both run in a real Revit, and both leave no empty undo entry ([D-160](decisions/D-160.md)) |
| Undo only our own last change | 3 | **4** | Heron's guard tracks more - the modeller's own commits, sync, reload - but is BUILT, not run ([D-157](decisions/D-157.md)). The other's is run in its release smoke |
| Several Revits open - never guess which | 4 | 4 | Heron: the lease, never slides, run on 2020 + 2024; two models made from one template still share the key a move checks (row 5b-1060, open as [issue 933](https://github.com/Ajmalpshaik/Heron-AI/issues/933)). Other: an exact document or process id for any change |
| **Change engine** | | | |
| Preview before change | 3 | **4** | Heron: preview then apply a move, run on 2024; `dry_run` on any change BUILT ([D-155](decisions/D-155.md)). Other: `dry_run` on most actions, run |
| Read-back after the change | 2 | **4** | Heron: values before and after BUILT, not run ([D-156](decisions/D-156.md)). Other: before and after on every write, and what Revit deleted at commit |
| Lost reply - find out what happened | 3 | **5** | Heron: every change named by a request id, written into its run record, read back by it - BUILT, not run ([D-161](decisions/D-161.md), [Group NW](needs-checking/group-nw.md)). Other: the same idea, run |
| **Model work** | | | |
| Breadth of model work | **5** | 3 | Heron: hundreds of operations, each PROVEN on a named model. Other: 89 tools, plus raw C# for the rest |
| MEP engineering | **4** | 1 | Heron: a large PROVEN MEP domain, plus HVAC, fire, loads and sprinkler engineering BUILT ([41](41-hvac-design.md) to [47](47-sprinkler-layout-from-the-model.md)). Other: one tool that draws an MEP run |
| Families | **4** | 3 | Heron: a large PROVEN family domain; family skills DRAFT ([43](43-building-revit-families.md)). Other: family audit and edit, one family skill |
| Model QA and reports | 2 | **5** | Heron: QA checklists BUILT, not run ([50](50-qa-checklists.md)); CSV and print only. Other: rule packs, history, Excel, HTML, BCF, an issue register with pictures |
| Many models at once | 1 | **5** | Heron: NOT PRESENT for project models. Other: a separate Revit per model, resumes after a restart |
| Export | 3 | **4** | Heron: PDF, DWG, FBX, image, CSV PROVEN; IFC and NWC DRAFT. Other: PDF, DWG, IFC, CSV, NWC with a Navisworks settings check |
| **Evidence** | | | |
| Proof per operation on a named model | **5** | 3 | Heron: [D-30](decisions/D-30.md) - a negative case and a staleness fingerprint on every PROVEN card, though most are proved on one release and signed by one person. Other: a per-year smoke and in-Revit tests, no per-operation proof |
| Revit releases covered and run | 3 | **4** | Heron compiles 2020-2027 and has run on four of eight; 2021-2023 and 2025 are installed on no PC reached ([Group EQ](needs-checking/group-eq.md)). Other: 2022-2027, every year built, installed and run |
| AI picks the right tool | **4** | 3 | Heron: routing checked on every PR, frozen sentences from real sessions. Other: a full AI evaluation harness, no results published |
| Honest about limits | **5** | 4 | Both strong. Heron keeps four states apart everywhere; the other's docs contradict its code in places |
| **Platform** | | | |
| Install and update | 2 | **5** | Heron: an unsigned per-user Setup that registers Heron with Claude Code ([D-167](decisions/D-167.md)), one release run on one laptop; update is notify only. Other: two MSIs, a Claude Desktop bundle, a server .exe, guides for SCCM, Intune and GPO |
| Office IT control | 2 | **5** | Heron: three registry ceilings, fail-closed (`HeronApplication.cs`, `HeronMachinePolicy`). Other: ten policies with ADMX templates and an ACL check on its policy file |
| In-Revit screens | 3 | **4** | Heron: ribbon, Companion, Settings and Activity pane, mostly BUILT, not run. Other: an Activity pane with Select, Zoom, Isolate and Undo per row |
| Saved commands | 2 | **4** | Heron: saved commands BUILT, not run ([49](49-saved-commands-and-the-heron-panel.md)). Other: saved tools on 24 ribbon slots, each revision reviewed before its first run |
| Knowledge, standards, site words | **4** | 1 | Heron: standards answered only from a cited source, unknown words looked up, never guessed ([D-33](decisions/D-33.md), [D-34](decisions/D-34.md)). Other: prompts and one guide |
| Release integrity | 2 | **4** | Heron: `checksums.txt` of every asset (`tools/build-release-assets.py`); unsigned, no build provenance. Other: checksums, build provenance and SBOMs; code signing prepared, not active |
| Privacy | 3 | **4** | Both: no telemetry. The other also hides folder paths in replies, on by default |
| Remote use from a Mac or the cloud | 0 | **4** | Heron: NOT PRESENT ([D-132](decisions/D-132.md)). Other: the whole server over SSH, run from a Mac |
| CI gates | 4 | **5** | Heron: twelve PR gates, compile 2020-2027, every suite. Other: also installer smoke tests, CodeQL, OpenSSF Scorecard, actions pinned by SHA |
| **Total, of 135** | **87** | **101** | Heron leads 9 areas, the other 16, two even. By group: Safety 21-17 and Evidence 17-14 to Heron; Change engine 8-13, Model work 19-21 and Platform 22-36 to the other |

**Read it this way:** Heron is the safer engine with the deeper library. The other platform is the more
complete product. None of its leads asks Heron to make anything less safe.

---

## 3. What to build

**Every item is Heron's own design, written from the idea; no code, name or wording is taken**
([D-25](decisions/D-25.md), [D-177](decisions/D-177.md)).

### 3.1 Already in Heron - owed a run, not a build

| Item | Where it is | What it is waiting for |
|---|---|---|
| A check on every Revit release | [Group EQ](needs-checking/group-eq.md), and `batch-prove --release` ([D-131](decisions/D-131.md)) | Revit 2021, 2022, 2023 and 2025 on a PC; the 2026 repair, [Group GW](needs-checking/group-gw.md) |
| A lost answer, read by the request's own name | [D-161](decisions/D-161.md) | [Group NW](needs-checking/group-nw.md). Its last loose end - the add-in's *"This run is not recorded"* for a box still open, which was not so - was corrected on 2026-11-17 (issue [934](https://github.com/Ajmalpshaik/Heron-AI/issues/934)) |
| A picture of chosen elements | [D-123](decisions/D-123.md) - a temporary 3D view boxed round them | [Group ER](needs-checking/group-er.md) |
| Registering Heron with the AI app | the installer's Claude Code entry (`platform/Heron.Installer/ClaudeCodeEntry.cs`) - one key, nothing else touched | the installer's run on a PC |
| Release checksums | `checksums.txt` (`tools/build-release-assets.py`) | nothing |

### 3.2 To build, in this order

| # | What Heron would build | Why it matters | Heron today | Size |
|---|---|---|---|---|
| 1 | **QA reports a coordinator can send.** A checklist run written to Excel and to one self-contained HTML page; then BCF issues with a camera, for Navisworks and ACC | The QA checklist exists; what it produced stopped at CSV and print | **Excel and HTML BUILT 2026-11-17** ([D-178](decisions/D-178.md)), tested without Revit; BCF and pictures not built | M |
| 2 | **An office checklist with project overrides.** A company base checklist a project list extends, allowed to change only severity, threshold and on/off, with a fingerprint of the resolved list on every report | One office standard, many projects, no silent drift | **BUILT 2026-11-17** ([D-179](decisions/D-179.md)), tested without Revit | S |
| 3 | **Hide folder paths in replies**, as a setting | A project's folder names can say more than the model | **Dropped 2026-11-17** - the owner: a path is a location, not the model ([D-180](decisions/D-180.md), not accepted; [Q-40](open-questions/from-master-specification-part-2.md) stands). Nothing of it kept in code | S |
| 4 | **Group Policy templates for the three ceilings** - ADMX and ADML for `AllowChanges`, `AllowAdmin`, `AllowPublish` | IT sets Heron's ceiling the way it sets everything else | **BUILT 2026-11-17** ([`platform/policy/`](../platform/policy/README.md)), tested without Windows; not run on a domain | S |
| 5 | **Build provenance on every release asset**, beside the checksums | Says who built a download, which a checksum cannot | **BUILT 2026-11-17** (`.github/workflows/release.yml`); first runs at the next tagged release | S |
| 6 | **Grids and levels against a link.** Match by name, then by geometry; report the perpendicular offset and angle; refuse a tilted link for levels | Setting-out checks before every issue | **BUILT 2026-11-17 as `CHECK_DATUMS_AGAINST_LINK`, DRAFT** - compiled 2020-2027; its proof is [Group OJ](needs-checking/group-oj.md) | M |
| 7 | **Many models overnight, read only.** A separate Revit per model, the year picked from the file, an unknown dialog stops that model and never clicks through, resumes after a restart; one report at the end | "Check every model in this folder" is a weekly coordinator job | NOT PRESENT for project models - **decided 2026-11-19, not built** ([D-181](decisions/D-181.md), [Q-93](open-questions/tier-3.md)): in the one Revit the modeller connected Heron to, one model at a time, and opening read-only is a read. Not a separate Revit per model. Building it is a later job, started when the owner asks | L |
| 8 | **The host AI tested end to end.** Real questions, the tools it should call in order, checked against the model's own answer - calibrated by a deliberately broken answer that must fail | Heron measures its own search; nobody measures the host's choice | **First half BUILT 2026-11-17:** [`tools/score-host.py`](../tools/score-host.py) scores a recorded chat's Heron calls against the modeller's words, calibrated by broken expectations; driving a live chat is not built | L |

Items 3 to 5 need no new decision. Items 6 and 8 each want one. Item 1's first half has [D-178](decisions/D-178.md), item 2 [D-179](decisions/D-179.md), item 7 [D-181](decisions/D-181.md).

---

## 4. Revit lessons worth recording now

Each is the other platform's observation, written in its docs or code comments; **none has been checked
in a Heron run.**

| What it records | Why Heron cares |
|---|---|
| `DocumentChanged` reports the name the inner transaction had at commit, **not** the name the group was renamed to before Assimilate | Heron's undo guard compares the names it hears - exactly what [Group NU](needs-checking/group-nu.md) asks |
| Deleting a link type unloads its document, and **rollback does not load it again** | No Heron card removes links today. Any card that does can never be previewed by "run, then roll back" - Discard would not restore it |
| Revit raises group errors only at commit, so a preview must commit the inner transaction and roll back the group to see them | Heron's dry run ([D-155](decisions/D-155.md)) should be checked to take the same path as a kept change |
| Resolve a failure only with *Fix elements* or *Set value*; other resolutions can delete, detach or move elements | Heron rolls back on any error (`RevitWrite.cs`, `CollectWarnings`) - stricter, and keeps that |
| Pinned, hosted or constrained elements can silently not move; compare bounds afterwards | Read-back ([D-156](decisions/D-156.md)) should report "asked to move, did not" |
| An ExternalEvent is not serviced while a modal dialog is open | Explains a "Revit is busy" refusal with a dialog open |
| `OpenAndActivateDocument` can open a second copy of a model already open | Matters to any card that opens a model |
| The save-reminder dialog has a stable id, `TaskDialog_Project_Not_Saved_Recently` | For batch work (item 7) |
| On 2022-2023, element ids above 2,147,483,647 fail | Heron's 2020-2023 paths use 32-bit ids |
| Revit's "Always Load" choice for an unsigned add-in is kept only after Revit closes normally, and resets when the DLL changes | Every Heron update re-asks until signing is settled (Q-77) |
| Pin the Revit API reference to the lowest point release of each year | The add-in then loads on the first update of each year |

---

## 5. What not to take

- **Changes on from install.** Heron keeps them off ([D-166](decisions/D-166.md)).
- **A confirmation the AI can give itself.** Heron's approval is a person in Revit ([D-126](decisions/D-126.md)).
- **Raw C# from the chat with no sandbox.** Heron writes a DRAFT fragment instead, which runs once
  behind Keep or Discard and then needs its proof ([D-119](decisions/D-119.md), [D-121](decisions/D-121.md)).
- **A silent update in the background.** Heron's update stays notify only.
- **Revit's non-public internals**, reached by reflection.
- **A checksum from the same release as proof of who made it.** It proves a download is whole, not
  where it came from.
- **An approval key stored beside what it approves**, under the same user.

---

## 6. Licence and safety scan

- **MIT.** The ideas are re-authored in any case; no code was taken.
- **Network:** update checks, the configured Revit endpoint, an optional Revit Server call, and in its
  AI evaluation the AI providers' APIs. No telemetry found.
- **Dynamic code:** only its documented C# runner. No obfuscation, no hardcoded credentials.
- **Worth knowing:** its auto-update checks a SHA-256 from the same release and no signature.

---

## 7. Open points for the owner

1. **The per-release check** needs Revit 2021, 2022, 2023 and 2025 on a PC. Is any of them available?
2. **Items 1, 6, 7 and 8** each need a decision before they are built.
3. **Remote use from a Mac or the cloud** is NOT PRESENT by decision ([D-132](decisions/D-132.md)). The
   other platform shows it can be done over SSH without opening a port. Reopen it, or leave it?
