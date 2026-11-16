<!--
Heron-Agent:  none
Heron-Step:   18
Heron-Status: DRAFT
Heron-Since:  0.1.0
Heron-Layer:  brain
See docs/29-metadata-standard.md
-->

# 51 — Studying Model MCP for Revit: a scoreboard, and what Heron should build from it

> **Status: research note, 2026-11-16. Nothing built, nothing copied.** Asked by the owner: *"Research
> this repo, each and everything... whatever things you find it is best to adopt... make a scoreboard
> like my MCP and his MCP. Not only MCP, the platform itself."*
>
> **How it was read.** [sharafutdinovdi/revit-model-mcp](https://github.com/sharafutdinovdi/revit-model-mcp)
> (MIT, by Dinar Sharafutdinov) was cloned at commit `bf9b0a03` (release 2.1.0, the same day) and read
> **in full** by six readers working in parallel: every Markdown file, every C# file of the add-in, the
> core library, the batch supervisor and the updater, the whole Python server, and the build, CI and
> installer folders. Their tests were skimmed for approach. **Nothing from it was built or run.** A
> seventh reader mapped what Heron has, with the same four labels Heron always uses. The key claims on
> both sides were then checked by hand against the files.
>
> [26 §9](26-prior-art-revit-mcp.md) and the 2026-11-15 landscape note already looked at this project
> from its README and two docs. This is the full read.

---

## 0. In one paragraph

Model MCP is a **finished product around a smaller, riskier core**. Changes are **on from install**,
and its confirmation is a token **the AI app can send back by itself** (it says so). But everything
*around* the change is better built than Heron's today: a job number for every change and a way to find
out what happened when the reply is lost; a read-only batch over a folder of models; audit reports in
Excel, HTML and BCF; MSI installers with Group Policy templates; checksums, attestation and SBOMs on
every release; and a smoke run on every Revit year it supports. **Heron leads where it matters most -
safety, proof per operation and the depth of its library and MEP engineering. Model MCP leads on the
platform around it.** On the scoreboard (§2) Model MCP is ahead overall. Most of its lead is in areas Heron has
not built yet, not in areas where Heron built worse.

---

## 1. What it is, measured

| | |
|---|---|
| Parts | A Revit add-in (C#), a core library with **no Revit reference** (unit-tested without Revit), a batch supervisor, an updater, a Python MCP server |
| Revit | **2022 - 2027** only. net48 for 2022-2024, net8 for 2025-2026, net10 for 2027 |
| Tools | **89** - asserted by their own test `server/tests/test_store_check.py:196`. Annotated: 36 read-only |
| Transport | A named pipe per Revit process; a file channel as fallback; optional HTTP on loopback with a token; and the whole server over **SSH from a Mac** |
| Tests | About 1,089 C# tests and about 900 Python tests, none needing Revit; **31 tests that run inside a real Revit**, opt-in |
| Real-Revit evidence | A release smoke script, run per year: load, ping, move then undo, picture, read-only refusal. A per-year PASS table on Autodesk sample models; a batch of 13 real models (11 completed) |
| Honest limits | "The token does not prove human approval." Their AI-evaluation and performance tables are **empty on purpose** - no result published. Only one client has a recorded live run (Claude Desktop on a Mac); Claude Code has *"no recorded validation run"* (`docs/tested-clients.md:14`) |
| Stale in places | Their own docs disagree with their code in about eight places - BCF called unbuilt in one doc and shipped in another, a correlation id said not to exist and then described |

Heron's side, for the same day, at commit `f86a584`. Every count below is a command, not a number in this
document:

- Tools: `python mcp/server/heron_tools.py`. Off Windows only the library tools are served ([D-132](decisions/D-132.md)).
- Fragments by state: `grep -h '^heron-status:' brain/fragments/*/fragment.yaml | sort | uniq -c`.
- Revit releases loaded in a real Revit: [docs/16 §6](16-version-support-strategy.md) - 2020, 2024, 2026, 2027.

---

## 2. The scoreboard

**Scores are judgement, 0-5, made on the evidence in the last column, not measurements.** A feature that
is only built scores lower than one that has been run in a real Revit, on either side. Heron's scores
use the four labels from [AGENTS.md](../AGENTS.md): BUILT, RUN IN REAL REVIT, DESIGNED ONLY, NOT PRESENT.

| Area | Heron | Model MCP | The evidence |
|---|:-:|:-:|---|
| **Safety** | | | |
| Changes off until the modeller turns them on | **5** | 2 | Heron: off by default, and the AI cannot switch them on ([D-166](decisions/D-166.md), built, not yet run). Model MCP: actions on from install; read-only is opt-in |
| A real person approves the change | **4** | 2 | Heron: Keep or Discard inside Revit, run on 2024 and 2027 ([D-126](decisions/D-126.md)); Ask to Keep is off by default ([D-138](decisions/D-138.md)). Model MCP: a token the AI app can confirm itself |
| One undo per change, full rollback on failure | 5 | 5 | Both run in a real Revit. Both leave no empty undo entry ([D-160](decisions/D-160.md); their `NoChangeGuardPolicy.cs`) |
| Undo only our own last change | 3 | **4** | Heron's guard is more careful - it tracks the modeller's own commits, sync and reload - but is BUILT, not run ([D-157](decisions/D-157.md)). Theirs is run in the release smoke |
| Several Revits open - never guess which | 4 | 4 | Heron: the lease, never slides, run on 2020 + 2024; two models made from one template still share the key a move checks (row 5b-1060, open). Model MCP: exact document id or process id for any change, process identity checked |
| **Change engine** | | | |
| Preview before change | 3 | **4** | Heron: preview then apply a move, run on 2024; `dry_run` on any change BUILT ([D-155](decisions/D-155.md)). Model MCP: `dry_run` on most actions, run |
| Read-back after the change | 2 | **4** | Heron: before and after values BUILT, not run ([D-156](decisions/D-156.md)). Model MCP: before/after on every write, and catches what Revit deleted at commit |
| Lost reply - find out what happened | 2 | **5** | Heron never blind-retries and names five endings, but has **no job number** (`mcp/server/heron_mcp_server.py` §"WHAT A HOST IS TOLD WHILE A TOOL WAITS"). Model MCP: a record written before sending, a job number, "outcome unknown" never re-runs |
| **Model work** | | | |
| Breadth of model work | **5** | 3 | Heron: hundreds of operations, each PROVEN on a named model. Model MCP: 89 tools, plus raw C# for the rest |
| MEP engineering | **4** | 1 | Heron: a large PROVEN MEP domain, plus HVAC, fire, loads and sprinkler engineering BUILT ([41](41-hvac-design.md)-[47](47-sprinkler-layout-from-the-model.md)). Model MCP: one tool that draws an MEP run |
| Families | **4** | 3 | Heron: a large PROVEN family domain; family skills DRAFT ([43](43-building-revit-families.md)). Model MCP: family audit and edit, one family skill with an evaluation profile |
| Model QA and reports | 2 | **5** | Heron: QA checklists BUILT, not run ([50](50-qa-checklists.md)); CSV and print only. Model MCP: rule packs, history, Excel, HTML, BCF, issue register with pictures |
| Many models at once | 1 | **5** | Heron: NOT PRESENT for project models. Model MCP: a separate Revit per model, resumes after a restart |
| Export | 3 | **4** | Heron: PDF, DWG, FBX, image, CSV PROVEN; IFC and NWC DRAFT. Model MCP: PDF, DWG, IFC, CSV, NWC with a Navisworks settings check |
| **Evidence** | | | |
| Proof per operation on a named model | **5** | 3 | Heron: [D-30](decisions/D-30.md), a negative case and a staleness fingerprint on every PROVEN card - though most are proved on one release, signed by one person. Model MCP: per-year smoke and 31 in-Revit tests, no per-operation proof |
| Revit releases covered and run | 3 | **4** | Heron compiles 2020-2027 but has run on four of eight; 2021-2023 and 2025 NOT RUN. Model MCP: 2022-2027, and every year is built, installed and run |
| AI picks the right tool | **4** | 3 | Heron: routing checked on every PR, frozen sentences from real sessions. Model MCP: a full AI evaluation harness - with no results published |
| Honest about limits | **5** | 4 | Both strong. Heron keeps four states apart everywhere; Model MCP's docs contradict its code in places |
| **Platform** | | | |
| Install and update | 2 | **5** | Heron: an unsigned per-user Setup, one release run on one laptop ([D-167](decisions/D-167.md)); update is notify only. Model MCP: two MSIs, a Claude Desktop bundle, a server .exe, deployment guides for SCCM, Intune and GPO |
| Office IT control | 2 | **5** | Heron: three registry ceilings, fail-closed (`HeronApplication.cs` `HeronMachinePolicy`). Model MCP: ten policies with ADMX templates, an ACL check on the policy file |
| In-Revit screens | 3 | **4** | Heron: ribbon, Companion, Settings and Activity pane - mostly BUILT, not run. Model MCP: Activity pane with Select, Zoom, Isolate and Undo per row; *Connect AI app* |
| Saved commands | 2 | **4** | Heron: saved commands BUILT, not run ([49](49-saved-commands-and-the-heron-panel.md)). Model MCP: *My tools*, 24 ribbon slots, revisions reviewed before first run |
| Knowledge, standards, site words | **4** | 1 | Heron: standards answered only from a cited source, unknown words looked up, never guessed ([D-33](decisions/D-33.md), [D-34](decisions/D-34.md)). Model MCP: prompts and one guide |
| Release integrity | 1 | **4** | Heron: unsigned, no checksums or attestation in `release.yml`. Model MCP: SHA256SUMS, build provenance, two SBOMs; code signing prepared, not active |
| Privacy | 3 | **4** | Both: no telemetry. Model MCP also hides folder paths in replies, on by default in its bundle |
| Remote use from a Mac or the cloud | 0 | **4** | Heron: NOT PRESENT ([D-132](decisions/D-132.md)). Model MCP: the whole server over SSH, run from a Mac |
| CI gates | 4 | **5** | Heron: twelve PR gates, compile 2020-2027, every suite. Model MCP: also installer smoke tests, CodeQL, OpenSSF Scorecard, actions pinned by SHA |
| **Total, of 135** | **85** | **101** | Heron leads 9 areas, Model MCP 16, two even. By group: Safety 21-17 and Evidence 17-14 to Heron; Change engine 7-13, Model work 19-21 and Platform 21-36 to Model MCP |

**Read it this way:** Heron is the safer engine with the deeper library. Model MCP is the more complete
product. Ten of Model MCP's sixteen leads are work Heron has not done yet; none of them asks Heron to
make anything less safe.

---

## 3. What to build - one order

Value to the modeller against effort. **Every item is Heron's own design, written from the mechanism;
no code, name or wording is taken** ([D-25](decisions/D-25.md), [31](31-studying-the-existing-libraries.md)).

| # | What Heron would build | Why it matters | Heron today | Size |
|---|---|---|---|---|
| 1 | **A smoke run on every installed Revit, one table.** One script on the owner's PC: for each Revit year installed - load, ping, one read, one change kept, undo, and the refusal while Changes is off. A fixed PASS / FAIL / NOT RUN row per check | Turns four NOT RUN releases into evidence in one sitting. Their equivalent is `build/release-smoke.ps1` | 2021-2023 and 2025 NOT RUN (docs/16 §6) | S |
| 2 | **A job number for every change, and an answer to "what happened to my last change?"** Write the request down before it is sent; if the reply is lost, answer from the record by its number; a change whose ending is unknown is **never sent again** - say so and look at the model | A dropped reply today leaves the modeller unsure whether a change landed. Completes docs/04 §7's "Working..." | `heron_failure.py` never blind-retries; no job number ([docs/04 §7](04-heron-mcp.md)) | M |
| 3 | **QA reports a coordinator can send.** A checklist run written to Excel and one self-contained HTML page, with a picture per failing check; BCF issues with a camera, for Navisworks and ACC | The QA checklist exists; what it produces stops at CSV and print | [50](50-qa-checklists.md) BUILT; no Excel, HTML or BCF | M |
| 4 | **An office checklist with project overrides.** A company base checklist that a project list extends, allowed to change only severity, threshold and on/off - with a fingerprint of the resolved list on every report | One office standard, many projects, no silent drift | Saved checklists, no inheritance | S |
| 5 | **Many models overnight, read only.** A separate Revit started per model, the year picked from the file, an unknown dialog stops that model and never clicks through, resumes after a restart; one report at the end | "Check every model in this folder" is a weekly coordinator job | NOT PRESENT for project models | L |
| 6 | **Release integrity.** SHA256SUMS and build provenance on every release asset; a check that no Autodesk file is inside any package | A modeller's IT department will ask | Not in `release.yml`; signing open (Q-77) | S |
| 7 | **Group Policy templates for the three ceilings.** ADMX and ADML for `AllowChanges`, `AllowAdmin`, `AllowPublish` | IT sets Heron's ceiling the way they set everything else | Registry ceilings exist, no template | S |
| 8 | **A picture of the elements, not just the view.** A temporary 3D view boxed around the elements, the targets in one colour, everything else faded, exported, then rolled back | "Show me those 12 sprinklers" answered with a picture | `capture_view` BUILT for whole views ([D-123](decisions/D-123.md)), not run | M |
| 9 | **Grids and levels against a link.** Match by name, then by geometry; report the perpendicular offset and angle; refuse a tilted link for levels | Setting-out checks before every issue | `check-link-coordinates` only | M |
| 10 | **Hide folder paths in replies**, as a setting | A project's folder names can say more than the model | NOT PRESENT | S |
| 11 | **Connect the AI app from the ribbon.** Write Heron's entry into Claude Desktop's config, touching only Heron's entry, with a dated backup first | One click instead of a setup page | NOT PRESENT | S |
| 12 | **The host AI tested end to end.** Real questions, the tools it should call in order, checked against the model's own answer - calibrated by a deliberately broken answer that must fail | Heron measures its own search; nobody measures the host's choice | `check-routing`, `score-routing` | L |

Items 1, 6, 7, 10 and 11 are small and need no new decision. Items 2, 3, 5 and 12 each want a decision
first.

---

## 4. Revit lessons worth recording now

Each is their observation, written in their docs or code comments; **none has been checked in a Heron
run.** Each says where it matters to Heron.

| What they record | Why Heron cares |
|---|---|
| `DocumentChanged` reports the name the inner transaction had at commit, **not** the name the group was renamed to before Assimilate (`UndoTracker.cs` 42-50) | Heron's undo guard compares names it hears - this is exactly what [NEEDS-CHECKING Group NU](needs-checking/group-nu.md) asks |
| Deleting a link type unloads its document, and **rollback does not load it again** (`LinkRemoval.cs` 75-77) | No Heron card removes links today. Any card that does can never be previewed by "run, then roll back" - Discard would not restore it |
| Revit raises group errors only at commit, so a preview must commit the inner transaction and roll back the group to see them (`docs/actions.md` 376-387) | Heron's dry run ([D-155](decisions/D-155.md)) should be checked to take the same path as a kept change |
| Resolve a failure only with *Fix elements* or *Set value*; other resolutions can delete, detach or move elements - otherwise roll back (`ActionCommandExecutor.cs` 1651-1709) | Heron rolls back on any error (`RevitWrite.cs` `CollectWarnings`) - stricter, and keeps that |
| Pinned, hosted or constrained elements can silently not move; compare bounds afterwards | Read-back ([D-156](decisions/D-156.md)) should report "asked to move, did not" |
| An ExternalEvent is not serviced while a modal dialog is open; they added an Idling fallback that waits at most 2 s | Explains a "Revit is busy" refusal with a dialog open |
| `OpenAndActivateDocument` can open a second copy of a model already open | Matters to any card that opens a model |
| The save-reminder dialog has a stable id, `TaskDialog_Project_Not_Saved_Recently` | For batch work (item 5) |
| On 2022-2023, element ids above 2,147,483,647 fail | Heron's 2020-2023 paths use 32-bit ids |
| Revit's "Always Load" choice for an unsigned add-in is kept only after Revit closes normally, and resets when the DLL changes | Every Heron update re-asks until signing is settled (Q-77) |
| Pin the Revit API reference to the lowest point release of each year | The add-in then loads on the first update of each year |

---

## 5. What not to take

- **Changes on from install.** Heron keeps them off ([D-166](decisions/D-166.md)).
- **A confirmation the AI can give itself.** Their token "does not prove human approval". Heron's
  approval is a person in Revit ([D-126](decisions/D-126.md)).
- **Raw C# from the chat with no sandbox.** Heron writes a DRAFT fragment instead, which runs once
  behind Keep or Discard and then needs its proof ([D-119](decisions/D-119.md), [D-121](decisions/D-121.md)).
- **A silent update.** Their server refreshes itself in the background through `uvx`
  (`server/revit_model_mcp/updates.py` 141-160). Heron's update stays notify only.
- **Revit's non-public internals.** Their document-tab highlight reaches them by reflection.
- **A checksum from the same release as proof of who made it.** It proves a download is whole, not
  where it came from - their own docs say so (`docs/updates.md` 187).
- **An approval key stored beside what it approves.** Their saved-tool key sits in the same folder,
  under the same user.

---

## 6. Licence and safety scan

- **MIT.** Studying the mechanisms is allowed; Heron re-authors them in any case.
- **Network:** update checks (GitHub API, PyPI), the configured Revit endpoint, an optional Revit Server
  REST call, and in their AI evaluation, the OpenAI and Anthropic APIs. No telemetry found.
- **Dynamic code:** only their documented C# runner. No obfuscation, no hardcoded credentials. Registry
  is read, never written, by the add-in; the installer writes only its own policy values, marked.
- **Worth knowing:** the auto-update checks a SHA-256 from the same release and no signature; their
  winget workflow posts the Microsoft CLA agreement comment automatically.

---

## 7. Open points for the owner

1. **Item 1 first?** It needs the owner's PC with each Revit year installed. Which years are installed
   there today?
2. **Item 2 (job numbers)** and **item 5 (batch)** each need a decision. Say which one comes first.
3. **Remote use from a Mac or the cloud** is NOT PRESENT by decision ([D-132](decisions/D-132.md)).
   Model MCP shows it can be done over SSH without opening a port. Reopen it, or leave it?

---

## 8. Sources

- [sharafutdinovdi/revit-model-mcp](https://github.com/sharafutdinovdi/revit-model-mcp) at `bf9b0a03`:
  `README.md`, `AGENTS.md`, `CHANGELOG.md`, `docs/` (all), `skills/` (all five), `src/` (all),
  `server/` (all), `build/`, `.github/`, `bundle/`
- Heron at `f86a584`: [AGENTS.md](../AGENTS.md), [26](26-prior-art-revit-mcp.md),
  [docs/16 §6](16-version-support-strategy.md), the decisions linked above
