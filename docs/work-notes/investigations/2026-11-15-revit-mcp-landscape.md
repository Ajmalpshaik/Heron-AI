# Revit MCP landscape: others caught up on undo, not on proof

2026-11-15. Active — a one-session research note. Retire it (delete it) once the owner has read it and anything worth keeping has moved into docs/26 or a register. *(Tracked as GitHub issue [#1008](https://github.com/Ajmalpshaik/Heron-AI/issues/1008) since 2026-11-16 (WN-36).)*

Method: the search run of 2026-11-15 found 548 GitHub repositories. 82 were studied file by file (cloned and read), plus pyRevit's own documentation and one helper add-in. The other 466 were swept by README and file list (358 Revit-related, 50 other BIM tools, 58 not BIM at all), and 22 of those were cloned to check one idea in code. Nothing from any repo was built, installed or run. The GitHub API and several vendor pages (help.autodesk.com, aecpilot.com, nonica.io and others) were blocked in this session, so star counts come from the search run's listing or from github.com pages read the same day, and every Autodesk, price and product fact comes from search-engine summaries. Those need confirming on the vendor's own page; section "Autodesk and the paid tools" marks them. This note updates [docs/26](../../26-prior-art-revit-mcp.md) (2026-08-27, re-checked 2026-09-22).

---

## In one minute

- **The field caught up on undo and on choosing a Revit.** pyRevit 7 now ships its own MCP server (GPL-3.0, experimental, off by default): a named pipe, Revit picked by year or process id, the document pinned, one change = one undo entry, and a Keep or Discard prompt with the changed elements selected. bimwright/rvt-mcp and sharafutdinovdi/revit-model-mcp both handle two Revits of the same version.
- **Nobody caught up on proof.** No project or product found keeps a record that each operation was right on a named real model, with a negative case. Paid tools advertise action counts (100+, 191, 340+), never correctness.
- **Heron still starts with changes switched off and still runs no AI-written code, but it is not alone in the second.** KenLP/RevitMCPServer, oakplank/RevitMCP, Autodesk's server and several small projects have no code tool either. The big ones mostly do: rvt-mcp, revit-verify and transom-revit ship a run-any-C# tool switched on, and 58 long-tail READMEs offer one. Only two projects seen (constructelligence-lab, vibe-bimer) start with writes off.
- **Autodesk's own server now writes, a little.** Since about 2026-09-29 there are Read, Write and Experimental installers for Revit 2027.2. Write adds about three tools; delete sits in Experimental. Since 2027.1 a change made through MCP carries an "AI" prefix in the Undo list and is logged in the journal. All from search summaries - confirm on Autodesk's pages.
- **The most valuable lesson is about Discard.** A rollback can report success and still leave the model changed. pyRevit 7 found this on workshared models and now re-checks the model after every rollback. Heron's own register has the same failure open since 2026-09-13 ([NEEDS-CHECKING A16b](../../needs-checking/group-a.md)). That is the recommended next step.
- **The best ideas to take** (re-authored, never copied): a Delete box that counts what Revit really deletes (RevitCortex, MIT); approval tied to the exact request and to the model as it was (KenLP, MIT); an automatic read-back after each change (KenLP); a write switch the AI cannot flip (vibe-bimer, MIT).
- **docs/26 has errors.** The biggest family talks raw TCP, not WebSocket, and listens on every network interface. AB.RevitMcp has had a gated run-any-C# tool since 2026-09-02. LuDattilo's server already had 138 tools and confirm dialogs. The "two Revits of the same version" lead is gone.
- **Installing is Heron's most visible gap.** Horizun ships a signed, per-year, self-checking Setup; AB.RevitMcp ships an .msi; every paid tool sells "one click, no Python". Heron is a Python script.
- **A paid market for local Revit connectors now exists**, about $10 a month to €199 a year per seat (vendor prices), and Adelphos AI aims an MEP "Revit Copilot" at Heron's MEP wedge (vendor claim, beta).
- **Four things checked in Heron's own code** (section below): the pipe is current-user only and refuses network logons; each Revit year gets its own build; a proof's fingerprint covers the fragment's source but no add-in binary hash was seen; and nothing seen stops an AI with file access from editing `write.enabled` in heron.config.

---

## The best parts out there that Heron does not have

*done 2026-11-19: every idea below has a register home (WN-30). The untaken ones - 6, 9, 11, 13, 14, 15, idea 10's journal half and three of the smaller ideas - are filed undecided as [PROPOSALS](../../PROPOSALS.md) F46 to F55, and the same section's table says where each of the others went: a decision, a NEEDS-CHECKING group, an issue, or already held.*

House rule ([docs/31](../../31-studying-the-existing-libraries.md), D-25): every idea below is re-authored in Heron's own words and code. No code, wording, names or branding come across, whatever the licence. MIT and Apache sources are credited when an idea is used; GPL, AGPL, no-licence and noncommercial sources give the idea only.

Ranked by value to a modeller's safety and trust first, then by effort. **Effort is my judgement, not a measurement.** Where several projects do the same thing it is one row, with the best example first.

| # | Idea (plain words) | Who does it best (repo) | Licence | What it would change in Heron | Effort (judgement) |
|---|---|---|---|---|---|
| 1 | **After Discard or Undo, check the model really went back.** Re-read what the change touched and say plainly if anything stayed. On a workshared model Revit can borrow elements and flush its undo list, and the rollback still reports "done". | pyrevitlabs/pyRevit 7 agent runtime (`rollback_incomplete`); tzur669-lab/revit-claude-agent (`commit_verified`) | GPL-3.0: idea only (pyRevit); MIT (tzur669-lab) | Keep/Discard box, `revit_undo_last`, proofs ("nothing was kept") | Small to medium |
| 2 | **The Delete box counts what Revit really deletes.** Run the delete inside the change, collect every id that went (hosted doors, dependants), then roll back. RevitCortex's own comment: a preview of deleting a Level said 1 element; the real delete removed about 100. | LuDattilo/RevitCortex; Al-Qublawi/AB.RevitMcp (requested ids against collateral ids) | MIT | Delete box (built, not yet run in Revit) - check first whether it already counts this way | Small |
| 3 | **Approval tied to the exact request and to the model as it was.** One use, a few minutes, refused if the inputs, the document or the model changed since the preview. The AI can never press "approve" itself. | KenLP/RevitMCPServer (`Approval.cs`); also bimwright/rvt-mcp (grants bound to the request and document revision), luoyuke/RevitAgenticAICompanion (document revision counter), Sam-AEC/aec-model-bridge (approve calls the AI cannot reach) | MIT (KenLP, luoyuke); Apache-2.0 (rvt-mcp); GPL: idea only (Sam-AEC) | Delete box, `revit_preview_move` then `revit_apply_move`, a DRAFT's one run, Admin and Publish | Medium |
| 4 | **Automatic read-back after every change.** Re-read what the change said it touched and add `verify: passed / failed / skipped` beside `outcome:`. Today the human check is the Keep box. | KenLP/RevitMCPServer (WriteGate); Daii1414/revit-verify (`verify_assert`); 5vbkgsghhh-hash/kir ("witness") | MIT; Apache-2.0 | Write path, the `revit_change` reply, Keep box | Medium |
| 5 | **A write switch the AI cannot flip.** "Changes on" counts only if the setting sits where the modeller's own Windows account cannot write it (an admin-owned file or policy), so an AI with file tools cannot switch it on. | vibe-bimer/smart-revit (admin-owned policy file); sharafutdinovdi/revit-model-mcp (workstation lock plus registry policy) | MIT | `write.enabled`, Admin, Publish in heron.config; Settings window; installer | Medium - needs an owner decision, because it touches the write boundary |
| 6 | **Revit warnings and dialogs during a change.** Warnings shown in the Keep box (or rolled back unless accepted). A dialog that pops up mid-change parks the job; it is answered against a fresh look, and the change is never sent twice. Dialogs about delete, purge, sync or overwrite are never answered automatically. | bimwright/rvt-mcp (warning policy, failure ledger, Issues window); ArchitectureWorld/revit-mcp-automation (parked dialogs); KenLP/RevitMCPServer (an unknown dialog is "recorded, never answered") | Apache-2.0; MIT | Write path, Keep box, bridge | Medium |
| 7 | **A change that timed out is never re-sent blind.** Each change gets an id. After a timeout Heron asks Revit what happened ("it may still finish - check the model") instead of trying again. | bimwright/rvt-mcp (`revit_get_operation_status`); LuDattilo/RevitCortex (honest wording, late finish written to the audit); General-Soju/BimOnMcp (idempotency key, `unknown` state) | Apache-2.0; MIT | Bridge, `outcome:` words, run record | Medium |
| 8 | **A check never passes on missing data.** A fourth answer, "could not decide - data missing", plus a coverage figure (checked / applicable / missing) beside any pass count. | PerSpec-Tech/Datum-Compliance-Checker (`unverifiable`, questions ranked by how many checks they unlock); KenLP/autoaudit-bim | Apache-2.0; MIT | `heron_check`, `heron_checklist` | Small |
| 9 | **A code check on fragments before they run.** Refuse Save, Sync, Export, other open documents, file and network calls at compile time. A READ fragment that changed the model is caught and rolled back. | 5vbkgsghhh-hash/kir (`CodePolicy`); Zhenko-J/rift (every read inside a group that is always rolled back); studioliquid-lab/revit-mcp-tool-generator (preview code cannot write) | Apache-2.0; MIT | Checks (a new CI gate over the library), fragment compile for DRAFTs, READ and ANALYZE runs | Medium to large |
| 10 | **Proof records pin what was tested.** Add the add-in build's hash, the exact Revit build, and Revit's own journal lines naming the transaction. | scwlkr/RevitThyme (hash-pinned records, failures kept); danlion333/revit_mcp (Revit's journal as an independent witness) | GPL-3.0: idea only (RevitThyme); MIT (danlion333) | Proofs (D-30 records) | Small |
| 11 | **Spreadsheet round trip with a three-way compare.** A hidden UniqueId per row, the export baseline and the model's identity stored inside the file, only edited cells written, "changed in the model since export" flagged per cell, unclear units held until confirmed. | Dave5264/transom-revit | No licence: idea only | Companion tables, `parameters-csv-round-trip` skill | Medium to large |
| 12 | **A signed one-click Windows Setup.** Per user, one payload per Revit year, refuses while Revit is open, checks for a duplicate add-in first, swaps folders with rollback, verifies after, registers Claude Code only while it is closed. | HorizunGroup/horizun-revit-mcp | Apache-2.0 | Installer (replaces `python tools/heron-ready.py` for modellers) | Large |
| 13 | **A published trust statement.** A tool manifest generated from Heron's own tool table (the Autodesk Marketplace shape) and a privacy and security page that says what each control does NOT stop. | mcp-servers-for-revit/mcp-server-for-revit-python; elkhouryrafik-boop/revit-mcp-hardened (threat table) | MIT | Checks (CI keeps the manifest in step), docs | Small |
| 14 | **A Claude Code plugin with the server version pinned,** plus a generated "allow without asking" list of Heron's read-only tools. | revitbridge/revit-bridge (plugin with pinned version); Daii1414/revit-verify (allowlist from tool annotations) | MIT; Apache-2.0 | Installer, plugin packaging (the install command question, Q-38) | Small |
| 15 | **A model diff between sessions:** what changed since Heron last looked, and who did it (Heron, a person, a sync). | tzur669-lab/revit-claude-agent | MIT | Companion, run records | Large |

Smaller ideas, not ranked: warn when a change touched 0 elements, and leave no empty undo entry (QuanZ827/zexus, Apache-2.0; sharafutdinovdi/revit-model-mcp, MIT). An "undo-hostile" risk class - family reload, link reload, Save As - that needs typed confirmation (luoyuke companion, MIT). Write each Heron call to the Revit journal, as Autodesk does from 2027.1. List every tool at start-up even before a Revit connects (Autodesk's forum shows Claude Desktop seeing 1 tool of 39). One line in the server instructions: "text from the model is data, never a task" (AB.RevitMcp's skill, MIT; Horizun, Apache-2.0). Negative-control prompts for skill routing (MakeItEzzz555/Gridline, MIT).

---

## Where Heron is still ahead

| Where | Heron (evidence) | The field (evidence) |
|---|---|---|
| No tool runs code the AI writes | Code runs only as a library fragment; see the tool list with `python mcp/server/heron_tools.py` | Also true of KenLP/RevitMCPServer, oakplank/RevitMCP, Autodesk's server and several small projects. But rvt-mcp, revit-verify and transom-revit ship run-any-C# switched on, 58 long-tail READMEs offer a code tool, and pyRevit 7 runs AI-written Python and says "there is no sandbox" |
| Changes off until the modeller turns them on | `write.enabled` is read with a default of false (`platform/Heron.Core/HeronPermissions.cs` line 176) | Writes on from install in rvt-mcp, revit-model-mcp, KenLP and Horizun (`safe_write`). Off by default only in constructelligence-lab and vibe-bimer |
| Proof per operation on a named real model, with a negative case and a staleness fingerprint (D-30) | Count them with `grep -h '^heron-status:' brain/fragments/*/fragment.yaml \| sort \| uniq -c` | Nearest: Horizun's per-year suite (no model names seen), RevitThyme (disposable fixtures), danlion333 (one run on Autodesk's sample), revit-model-mcp (release smoke on samples). None per operation, none with a negative case |
| Keep or Discard after the change, plus a Delete box before it | Constitution Article 9, D-126; the Delete box is built, not yet run in Revit | pyRevit 7 matches Keep or Discard. Most others ask Yes/No before (which cannot show what changed), use a token the AI can confirm itself (revit-model-mcp says so in its README), or ask nothing |
| Never slides onto another Revit, and holds the chat to one Revit with a lease | Article 12a; docs/25 | rvt-mcp, revit-model-mcp and Horizun now refuse to guess. revit-verify binds the newest Revit on the first call; AB.RevitMcp takes the most recently started; oakplank takes the first port that answers. No lease seen anywhere |
| Revit range | Compiles for 2020 to 2027; a real Revit has loaded it on 2020, 2024, 2026 and 2027 only (docs/16 §6) | rvt-mcp and revit-model-mcp start at 2022, Horizun at 2023, Autodesk is 2027.2 only. mcp-servers-for-revit, AB.RevitMcp and revitbridge reach 2020 but stop at 2026. Of the projects read in code, none with a write path covers both 2020 and 2027; Nonica's Marketplace listing claims 2020-2027 (vendor claim; another page says 2022-2027) |
| Never calls a model "compliant" | Server instructions | autoaudit-bim prints "Compliant: 45 / 52"; spec-compliance grades a run 100% with 86% of the data missing |
| Holds no AI keys | The host is Claude Code (D-01) | ClaudeRevit keeps the key in a plain environment variable; RevitMCPBridge2026 in a plain JSON file |

---

## Where Heron's lead has shrunk since 2026-09-22

| What docs/26 counted as Heron's | Who has it now (licence) | Evidence | What still separates Heron |
|---|---|---|---|
| Keep or Discard with the change selected and isolated, one undo entry, the document pinned, the Revit chosen | pyrevitlabs/pyRevit 7 agent runtime (GPL-3.0), experimental, off by default | pyRevit `docs/agent-runtime.md` (guarded run, `document_changed`, `revit` argument takes a year or process id) | pyRevit runs whatever Python the AI writes. Heron runs only library fragments, with proofs, and reaches 2020 to 2023 |
| Two Revits of the same version (docs/26 §8.4 item 3) | bimwright/rvt-mcp (Apache-2.0); sharafutdinovdi/revit-model-mcp (MIT); HorizunGroup/horizun-revit-mcp (Apache-2.0); kolosimo/MCP-x-REVIT-2025 (no licence) | rvt-mcp README "Multiple Revit instances"; revit-model-mcp `docs/actions.md`; Horizun `docs/security-model.md` §4-5 | Heron's lease and "never slide". But rvt-mcp also has a generation counter, so a call made before a switch cannot land after it - Heron has no such counter seen |
| One named undo entry per change | sharafutdinovdi/revit-model-mcp ("MCP (client): summary"), AB.RevitMcp, pyRevit 7, LoadingAI and ClaudeRevit (one per chat turn); Autodesk 2027.1 puts "AI" on the undo entry | `docs/actions.md`; `ToolRouter.cs`; Autodesk AEC blog 2026-06-16 (search summary) | Heron also rolls the whole request back on any failure and asks Keep or Discard |
| Undo only Heron's own last change | revit-model-mcp's `revit_undo_last` refuses unless Revit's last undo entry is its own | revit-model-mcp `docs/actions.md`, `UndoTracker.cs` | Heron's guarded undo (D-157) exists but its code is still DRAFT (`revit/Heron.Revit.Addin/RevitUndo.cs` header) |
| A write path on Revit 2027 | Autodesk's Write installer, Revit 2027.2, about 3 write tools | Search summaries of Autodesk help and blog.autodesk.io - confirm | Heron's library covers far more write work, with Keep or Discard, and keeps 2020 to 2026 where Autodesk is not |
| Release evidence per Revit year | Horizun (5 years x 246 probes, each report hashed); revit-model-mcp (per-year PASS table on named sample models) | Horizun `docs/release-evidence.json`; revit-model-mcp `docs/validation.md` | Heron's evidence is per operation, on named models, with a negative case. But Heron's 2021-2023 and 2025 are NOT RUN, and those two projects have a per-year table Heron lacks |
| Nobody else had a real installer | Horizun (signed Setup), AB.RevitMcp (.msi), revit-model-mcp (MSI with SCCM, Intune and GPO guides), Nonica and AEC Pilot (one click, vendor claim) | Horizun `installer/horizun-mcp.iss`; AB CHANGELOG 1.5.0; revit-model-mcp `docs/admin/deployment.md` | Nothing yet - Heron installs with a Python script |

What still separates Heron, in one paragraph: proof per operation on named real models with a negative case and a staleness fingerprint; no run-any-code tool at all; changes off until the modeller turns them on; Keep or Discard after every chat change plus a Delete box before deletes; a session lease that never slides onto another Revit; and a build that reaches 2020 to 2023. Others have one or two of these; nobody else has them together. The first, proof per operation, is the one nobody else is close to.

---

## Corrections to docs/26

Each row is what this study's code reading found against what [docs/26](../../26-prior-art-revit-mcp.md) says. The owner resolves them by editing docs/26.

| What docs/26 says | What the code shows | Source file |
|---|---|---|
| §1 and §6: mcp-servers-for-revit talks **WebSocket** | Raw TCP JSON-RPC to a hard-wired port 8080. The add-in listens on **every network interface with no password**; issue #54 open since 2026-09-08. The `ws` package is declared but never imported | mcp-servers-for-revit `plugin/Core/SocketService.cs` lines 87 and 105; `server/src/utils/SocketClient.ts` (commit 86cf705) |
| §1: mcp-servers-for-revit has "30+" tools | 26 tool registrations (three tool files are empty); 23 Revit commands in `command.json` | `server/src/tools/*.ts`, `command.json` (86cf705) |
| §4 Gap 1: "single WebSocket connection model" | The limit is a fixed port. A second Revit's listener fails to start, the error is swallowed, and the button still reads "Open Server" | `plugin/Core/SocketService.cs` line 114; `plugin/Core/MCPServiceConnection.cs` line 28 |
| §1: the family listed as live projects | The three originals (revit-mcp, revit-mcp-plugin, revit-mcp-commandset) are archived since 2026-02-25 | Each repo's README; the org's repositories page |
| §4: both families ship code execution, "apparently ungated" | Still true of the C# family. The Python sibling now shows the whole code in a Yes/No dialog in Revit before it runs (since 2026-10-14), and its own SECURITY.md says it is not sandboxed | mcp-server-for-revit-python `revit_mcp/security.py`, `SECURITY.md` (6ca46b3) |
| §1: LuDattilo/revit-mcp-server talks **WebSocket** | TCP on loopback, first free port 8080 to 8089, written to `mcp-port.txt`; the server reads the newest Revit year's file | `plugin/Core/SocketService.cs`; `server/src/utils/SocketClient.ts`, `ConnectionManager.ts` (2c33b84) |
| §8.2: LuDattilo has **124** tools (§1 said 138) | **138** registrations in 138 files (its README says both 138 and 124) | `server/src/tools/` (2c33b84) |
| §8.2: LuDattilo has "standard transactions, not grouped" and nothing more | The grouping part holds (115 Transactions, 7 groups). But confirm dialogs in 13 handlers (2026-04-07) and `dryRun` in 30 files (2026-04-03) existed before the re-check. Its `send_code_to_revit` is ungated. No commits since 2026-04-09; the author moved to RevitCortex | Commits 219b8f3, 0cf2e37, 71e5a9e; `commandset/Commands/ExecuteDynamicCode/ExecuteCodeEventHandler.cs` |
| §8.2: AB.RevitMcp has **"no eval escape hatch"** | `revit_execute_code` exists since v1.1.0 (2026-09-02): compile-any-C#, three gates (a server flag, a ribbon toggle off by default, `confirm: true`), Revit 2020-2024 only. Its README contradicts itself | `src/AB.RevitMcp.Contracts/Tools/ToolCatalog.Code.cs`; `src/AB.RevitMcp.Addin/Tools/CodeTools.cs` (fe4f688) |
| §8.2: AB's dry-run "executes for real then rolls back and reports the blast radius" (reads as every write) | Dry-run covers **only 3 destructive tools** (delete, purge, unload links). Ordinary writes cannot be dry-run | `src/AB.RevitMcp.Addin/Tools/DestructiveTools.cs` (18 hits; 0 in every other tools file) |
| §8.2: AB "simultaneous instances not addressed" | Each Revit is found by process id, but with several open the server takes the most recently started | `src/AB.RevitMcp.Server/Bridge/BridgeConnection.cs` lines 131-169 |
| §8.2 and §8.4 item 3: nobody else matches per-process pipes, discovery and binding; Heron alone handles two Revits of the same version | rvt-mcp: one discovery file per process, binds only on an exact match (else `AMBIGUOUS_TARGET`), checks document identity before each write. revit-model-mcp: one pipe per process, heartbeat files, writes need an exact document id or process id. Horizun refuses two same-year Revits until one is chosen. pyRevit 7 takes a year or process id. No lease seen in any of them | rvt-mcp README "Multiple Revit instances", `ARCHITECTURE.md` (b44ee0b8); revit-model-mcp `docs/actions.md` (be74eed9); Horizun `docs/security-model.md` §4-5; pyRevit `docs/agent-runtime.md` |
| §8.2: KenLP has 94 tools, dry-run on every write, a port per Revit version | Now 100 MCP tools. Since 2026-10-04 also an opt-in approval token bound to the exact inputs and active document, a read-back after commit and a hashed audit log. A second Revit of the same year still fails to bind | KenLP `CHANGELOG.md`; `src/RevitMCP.Core/Approval.cs`; `src/RevitAddin/App.cs` lines 102-104 (fdf6efa) |
| §5: pyRevit means pyRevit Routes over a fixed HTTP port | Routes listens on all interfaces by default and does not pass HTTP headers to handlers (revit-mcp-hardened says it read pyRevit's source; not checked here). pyRevit 7 now ships a separate MCP server over a named pipe | revit-mcp-hardened `SECURITY.md` (43afdb7); pyRevit `docs/agent-runtime.md` |
| §6: real variety is only "WebSocket versus pyRevit HTTP" | Named pipes are now common: AB.RevitMcp, Horizun, revit-model-mcp, RevitThyme, bim-agent, kir, pyRevit 7, and rvt-mcp from 2025. The biggest family is raw TCP | See group a, c, d, e, f, g, h below |
| §1: oakplank/RevitMCP with no detail | 49 tools, MIT, no code tool; pyRevit Routes over HTTP on 48885/48884/48886 and the first port that answers wins; delete in "individual" mode leaves one undo entry per element | `lib/RevitMCP_ExternalServer/tools/registry.py`; `core/runtime_config.py` line 17; `lib/routes/element_operation_routes.py` (32c9ae8) |
| §8.1: Autodesk does not create or modify elements; a write server is "coming" | Since about 2026-09-29: Read, Write and Experimental installers on Revit 2027.2. Write adds about 3 tools; Experimental about 28 more, delete among them | Not code - search summaries of Autodesk help and blog.autodesk.io; confirm on Autodesk's page |
| §8.4 item 1: none ships evidence that an operation was ever correct | Still true in that exact form, but closer now: Horizun's hashed per-year release evidence, RevitThyme's hash-pinned records on fixtures, danlion333's run on Autodesk's sample witnessed by Revit's journal, revit-model-mcp's per-year smoke. None per operation, none with a negative case | Horizun `docs/release-evidence.json`; RevitThyme `verification/`; danlion333 `docs/verification/README.md` |

---

## Autodesk and the paid tools

Every fact here comes from search-engine summaries, because help.autodesk.com and the vendor sites were blocked in this session. The last column says what to confirm.

### Autodesk

| Fact (as summarised on 2026-11-15) | Source | Confirm on Autodesk's own page? |
|---|---|---|
| Three separate installers: **Read, Write, Experimental**. Write includes Read; Experimental includes both. A firm can install Read only | Autodesk help; Autodesk Developer Blog | Yes |
| Write and Experimental available since about **2026-09-29** | buildtechscope; blog.autodesk.io | Yes |
| Supported "only on Revit 2027.2" (help) against "2027.2 or later" (developer blog) - the two conflict | Autodesk help; blog.autodesk.io | Yes |
| Write adds **about 3** model-changing tools (parameters, move, rotate). Experimental adds about 28 more, **delete** among them. One forum user's add-in reported 39 tools | revit.news; Autodesk forum | Yes - the full tool table was not readable |
| From 2027.1, every MCP tool call is logged in the Revit **journal**, and a change made through MCP has an **"AI" prefix in the Undo list** | Autodesk AEC blog, 2026-06-16 | Yes |
| Autodesk's own advice: review proposed changes before accepting them, and back up the model before writing | Autodesk help; revit.news | Yes |
| Whether a Write request is one undo entry, or rolls back a partial failure | Not found | Yes |
| No Autodesk code-execution tool found | aecomcps | - |
| Claude Desktop showed only `get_running_revit_instances`: the server adds its other tools after it connects to Revit | Autodesk forum | - |
| A locked-down office asked whether the add-on sends telemetry; no official answer found | Autodesk forum | Yes |
| The Assistant (test, Sep 2026) creates schedules and tags, but cannot modify existing elements | ENGtechnica, summarising AEC Magazine | Secondary source |
| AU 2026 previewed a "next-generation Assistant", agent-first, **no release date, no price** | Autodesk newsroom and press release | Yes |
| Design & Make Marketplace **MCP publisher guide**: a JSON manifest of tools, app model, Autodesk APIs used and AI providers, plus a signed declaration on data handling, user consent and secrets | aps.autodesk.com; Autodesk MCP FAQ PDF | Yes |

What it means for Heron: on 2027, Autodesk now treats a named undo entry and a journal line as normal. Heron's edge there is Keep or Discard, the whole-request rollback, the proofs and the size of the library. And the forum complaint is a cheap lesson: list every tool at start-up, even before a Revit connects.

### Paid tools

| Product | What it is (vendor claim) | Price (vendor claim - confirm) | Revit | Note for Heron |
|---|---|---|---|---|
| Nonica AI Connector | Free tier reads and analyses; Pro adds editing | $10 a month (Marketplace) against €85 a year (an ArchiLabs page) - conflict | 2020-2027 or 2022-2027 - conflict | "Read free, write opt-in", sold through Autodesk's Marketplace |
| AEC Pilot | 100+ actions; one-click setup for Claude Desktop, Claude Code, Codex and Cursor; "no Python" | 14 days free, then €199 per seat per year | 2023-2027 | A "set up my AI app" step |
| HMK Pilot | Revit, AutoCAD, Civil 3D, Navisworks; 340+ actions | Not found | 2023-2027 | May be the same vendor as AEC Pilot - not confirmed |
| ArchiLabs | "AI teammate" across Revit and AutoCAD | From $19 a month plus credits | - | Its many comparison pages are marketing, not evidence |
| SWAPP.AI | Agent now branded "Frank", in Revit and ArchiCAD | Enterprise quote | - | Still the knowledge-layer occupant docs/26 §8.3 named |
| Kestrel Labs | Code compliance inside Revit on licensed ICC code text, about 30 s per review | Not found | Not found | Clause-level citations with the source edition |
| EvolveLab Glyph 3.0 | An AI assistant parses the request, then prebuilt Glyph operations run | - | - | The same shape as Heron |
| Adelphos AI Revit Copilot | 163 commands and 191 tools "purpose-built for MEP"; beta from Q2 2026 | Not found | 2022-2026 | **The first product aimed at Heron's MEP wedge** - watch it |
| BIM Automation Studio | MCP setup as a service | $1,250 one-time onboarding plus per-seat fees | - | - |

No seller found publishes a proof or a correctness benchmark. The only measured test of an AI working a live Revit through MCP (NovaShang/bimdown-format-comparison, run by an interested party, one model) scored 59% with a run-code tool. AEC Magazine's hands-on piece (2026-09-09) says to break a job into small trained steps - which is what `heron_method` already does. Revit servers are now listed on the official MCP registry (through mirrors), Glama, mcpservers.org, Smithery and mcp.so; Heron was not searched for by name, to keep the owner's details out of outside queries.

---

## Four things checked in Heron's own code during this research

| # | What was checked | What the code shows | Evidence | What it means |
|---|---|---|---|---|
| a | Is Heron's pipe open to other users or the network? | The pipe allows only the current Windows user, and denies NT AUTHORITY\NETWORK, so a logon over the network is refused even for the same user | `revit/Heron.Bridge/BridgeServer.cs` lines 342-357 | The "pairing secret" idea from mcp-server-for-revit-python matters less for Heron: it guards against an unpaired remote or browser caller, which Heron's pipe already refuses |
| b | Does one net48 build serve both Revit 2023 and 2024? (Horizun's installer warns this fails at the first missing method) | Each Revit year gets its own output folder and its own `REVITyyyy` compile symbol | `Directory.Build.props` lines 99 and 134 | Horizun's warning does not apply to Heron |
| c | What does a proof's fingerprint cover? | It hashes the fragment's C# source (`source_hash` over `impl/any/fragment.cs`, and `frag.fingerprint()` over the files under `impl/`) | `brain/heron_validate.py` lines 591-613 and 771 | A hash of the add-in binary or of the Revit build was not seen in the proof draft. **Not fully checked** - worth a look (RevitThyme pins binary hashes in its records) |
| d | Can an AI host switch writes on by editing a file? | `heron.config` is a plain key/value file. Heron's code never applies config from a request (Golden Rule 19: there is a Load and a Save but no ApplyFromRequest). The heron-guard hook has no rule for this file | `platform/Heron.Core/HeronConfig.cs` lines 19-42; `.claude/skills/heron-guard/bin/heron_guard.py` | Nothing seen stops an AI host with file-write access from editing `write.enabled` in the file. **Seen in code, not tested.** Worth an owner decision; vibe-bimer's admin-owned policy file is the example (ranked idea 5) |

Also: the idea of measuring what Heron's tool list costs a chat is **already built** - [docs/04 §8.8](../../04-heron-mcp.md) "What the tool list costs a chat". Nothing to add there.

---

## Group by group

One table per research note. Licences come from each repo's own LICENSE file, except the long tail, where they come from GitHub's licence detector in the search run. "Idea only" means GPL, AGPL, no licence, source-available or noncommercial.

### a. The mcp-servers-for-revit family (8 repos, file by file)

| Repo | Licence | One-line verdict | Best part |
|---|---|---|---|
| mcp-servers-for-revit/mcp-servers-for-revit | MIT | The family's working bridge (about 450 stars): well packaged and easy to extend, but it listens on the whole network with no password and its run-any-C# tool has no gate | Drop-in command sets per Revit year; one release tag builds per-year ZIPs and an npm package |
| mcp-servers-for-revit/revit-mcp | MIT | The original server; archived 2026-02-25 | A tool folder that registers itself |
| mcp-servers-for-revit/revit-mcp-plugin | MIT | The original add-in; archived; same open listener | Command-set loader with a version filter and a per-command on/off switch |
| mcp-servers-for-revit/revit-mcp-commandset | MIT | The original command set; archived | An unmerged branch with one result shape for every tool, and the "operate element" tool split by risk |
| mcp-servers-for-revit/mcp-server-for-revit-python | MIT | Now the family's safety and publishing leader: a pairing secret, a Yes/No review in Revit before 8 write actions, plain privacy and security pages; open pull requests say pairing fails on pyRevit 6.5 and 7 | A Marketplace-format tool manifest generated from the code and checked in CI |
| mcp-servers-for-revit/mcp-server-for-revit-dotnet | No licence (idea only) | A small demo with an in-Revit chat calling DeepSeek; reads its key from a fixed path on the author's PC; over 1,000 built files committed | In-Revit window, then a local MCP client, then an AI endpoint |
| mcp-servers-for-revit/revit-mcp-templates | MIT (holder left blank) | A `dotnet new` template with one hello command; stale | Scaffold a new command in one command |
| mcp-servers-for-revit/revit-mcp-plugin-debug | MIT | A desktop tester that sends one command by hand | Drive the bridge with no AI in the loop |

### b. Large tool counts in C#: LuDattilo, DTDucas, ZedMoster, PiggyAndrew (6 repos)

| Repo | Licence | One-line verdict | Best part |
|---|---|---|---|
| LuDattilo/RevitCortex | MIT | The broadest here (288 tools, 2023-2027) with real craft, but writes on by default, 220 plain transactions, a regex-gated code tool, an "Auto" mode with no time limit, and drifting to a paid "Premium" with licensing and opt-in telemetry | A delete preview that runs the real delete, counts the cascade and rolls back |
| LuDattilo/revit-mcp-server | MIT | No commits since 2026-04-09; the author moved to RevitCortex; its code tool is ungated | A `fields` parameter that trims big replies; reply size logged per tool |
| DTDucas/RevitMCPSDK | MIT | The thin base library under the whole family; carries no risk, version or proof data | One NuGet package line per Revit year; a Revit error-code range |
| ZedMoster/revit-mcp | MIT | A Python front end for a closed add-in; its suggested client config auto-approves delete and generic calls | `call_func`: third-party methods called by name, so the tool list does not grow |
| ZedMoster/xml.Revit.MCPServer | No licence (idea only) | An example extension for the closed xml.Revit binaries | - |
| PiggyAndrew/revit_mcp_dotnet_template | MIT | A one-tool teaching template; reads the result before Revit has run the command | Typed tool inputs as a small C# class |

### c. Safety-first, set 1 (3 repos)

| Repo | Licence | One-line verdict | Best part |
|---|---|---|---|
| bimwright/rvt-mcp | Apache-2.0 | A mature all-C# gateway (256 tools, 2022-2027) with the best bridge and failure engineering; writes and run-any-C# on by default; one undo entry only for batches and some workflows | Revit chosen by process id with the document checked before each write; warnings roll back by default; timed-out writes reconciled by operation id |
| Sam-AEC/aec-model-bridge | GPL-3.0 plus exception plus commercial (idea only) | The strongest human-approval design, but only on its dev branch, no write verified in a live Revit, no document binding | The person approves a hash of exactly what was shown, and the AI can never call "approve" |
| sharafutdinovdi/revit-model-mcp | MIT | One named undo per change, a workstation-wide read-only lock and IT policy, the best per-year evidence of the three; actions on by default, and its confirm token "does not prove human approval" | A read-only lock for every Revit of the user, fail-closed, with a registry policy above it |

### d. Safety-first, set 2 (6 repos)

| Repo | Licence | One-line verdict | Best part |
|---|---|---|---|
| Al-Qublawi/AB.RevitMcp | MIT | A careful write path and a clean .msi; idle since 2026-10-13; has the triple-gated code tool docs/26 missed; takes the newest Revit | A delete dry-run that lists collateral deletions; an offline load doctor |
| KenLP/RevitMCPServer | MIT | The strongest runtime write safety in its group (preview token bound to exact inputs and document, read-back after commit, hashed audit); 2025-2027 only; a second same-year Revit fails | Approval token bound to the command, the exact inputs and the document |
| KenLP/autoaudit-bim | MIT | The best audit discipline: rules as data, a static lint, fenced AI drafting, provenance; demo data only; prints "Compliant" | "Missing the data needed to decide" as an answer that never passes |
| elkhouryrafik-boop/revit-mcp-hardened | MIT | Honest hardening of the Python bridge; confirms pyRevit Routes listens on all interfaces by default (claimed from pyRevit's source) | Profiles enforced by not registering tools; a test that every route is gated or names its exemption |
| Ziad-Amr1/bim-ai-agent | MIT | A teaching demo: a person clicks to run each command; its "revert" is a second change | - |
| Daii1414/revit-verify | Apache-2.0 | A fork of rvt-mcp for 2025-2026 with the richest "verify" toolkit; run-any-C# on by default with no gate; binds the newest Revit on the first call | Check baselines pinned to the model's identity; an undated or mismatched one is refused as stale |

### e. In-process agents and AI-written code (9 repos)

| Repo | Licence | One-line verdict | Best part |
|---|---|---|---|
| QuanZ827/zexus | Apache-2.0 | One run-code tool; its "yes" detector counts almost any message (even "y" inside a word) as approval | A warning when a change touched 0 elements |
| 5vbkgsghhh-hash/kir | Apache-2.0 | The most rigorous AI-code project: a typed program checked against the live model, a code policy, the run bound to the source hash and document revision, a read-back "witness" | A semantic code policy checked before code runs |
| luoyuke/RevitAgenticAICompanion | MIT | "Bounded" planning: the document revision is checked at approve and again at run; its preview has no rollback | A document revision counter; an "undo-hostile" risk class |
| nikolaikrasilnikov/Revit-Bridge | No licence (idea only) | The strongest host-side guard for AI C#: blocks Save, Sync and Export at compile time; catches a read that wrote, or another document that changed | "A read that wrote" is a violation and is rolled back |
| shtirlitsDva/cad-mcp | No licence (idea only) | One ungated persistent C# session; two useful loader patterns | "Test, then Live" for batches |
| goswinr/Caddis | MIT | The most mature cross-version script host (2024-2027), one group per run; no code policy, no approval | Picks .NET 8 or .NET 10 at run time |
| Innoprovn/vn-revit-mcp | GPL-3.0 plus exception (idea only) | `run_csharp` previews by rollback by default; write mode commits without review; takes the topmost Revit window | Read mode = run, then roll back |
| H8DIDI/tianheluo-core | MIT | A plugin host for Revit 2026 with hot reload; not an MCP server | Load the add-in's bytes into a throwaway context so the file never locks |
| permitzip/mutinybim | Apache-2.0 | Not Revit: a browser BIM engine | - |

### f. pyRevit- and Python-based (11 repos)

| Repo | Licence | One-line verdict | Best part |
|---|---|---|---|
| pyrevitlabs/pyRevit (v7 agent runtime) | GPL-3.0 (idea only) | The biggest new fact: a built-in MCP server close to Heron's design (pipe, Revit by year or process id, document pin, one undo, Keep or Discard); runs AI-written Python, "no sandbox" | Re-checks the model after a rollback and reports `rollback_incomplete` |
| Demolinator/revit-mcp-server | MIT | Cleaned up since 2026-09-22, not made safer: 48 tools, one port, an ungated code tool | A test that pins every tool's name and inputs |
| Demolinator/revit-mcp-plugin | Apache-2.0 (bundled server MIT) | A real Claude Code marketplace plugin; its bundled server is stale; its skill plans raw code as a tier | One-command plugin install with the server bundled |
| oakplank/RevitMCP | MIT | The most complete pyRevit-Routes project; no code tool; safe delete defaults; no single undo; the first port that answers wins | Delete defaults: preview on, confirm off, at most 25 elements |
| andyluu98/vn-revit-mcp | MIT | A small, careful server of fixed scripts on pyRevit 7's runtime; one write tool, preview first | Excel takeoff that never writes formulas and adds a source sheet |
| Hazemalisakr/pyRevit-MCP-Server-Bridge | MIT (holder OakPlank) | An uncredited older copy of oakplank with three run-code paths, two of them hidden | - |
| danlion333/revit_mcp | MIT | A minimal, honest bridge with a recorded run on a named Revit sample model, witnessed by Revit's own journal | Revit's journal as an independent witness |
| fmg75/CLI_Revit | Apache-2.0 | 5 tools fronting about 161 scripts; measured where the time goes; thin safety | Latency per phase: start-up, queue, time in Revit |
| tzur669-lab/revit-claude-agent | MIT | The deepest memory: snapshot, diff and "who changed it" (AI, person, sync); rests on an ungated code tool | A model diff between sessions, with attribution |
| revitbridge/revit-bridge | MIT | Every value carries its source; a token bound to the exact spec, but the AI can mint it itself | Open questions tagged by who can answer them |
| revitbridge/revit-bridge-addin | MIT | The add-in behind revit-bridge; Roslyn with a reference whitelist and a blocked-symbol list | A diagnostic id for each forbidden symbol |

### g. Popular and installer-focused (8 repos)

| Repo | Licence | One-line verdict | Best part |
|---|---|---|---|
| HorizunGroup/horizun-revit-mcp | Apache-2.0 | The most finished installer seen: signed, per user, per year, transactional; strong pipe security; typed writes on by default | A Setup that refuses while Revit runs and rolls back a failed install |
| IbrahimFahdah/revit-claude-mcp | MPL-2.0 (ideas only, copy no files) | "All versions" is two builds picked by hand from a ZIP, and the "2024 and below" build calls 2024+ API | A Claude Desktop bundle (.mcpb) |
| schauh11/revit-mcp-server | MIT | Not MCP: an in-Revit chat with several AI providers; regex intent forcing; 2026 only | Compound tools, e.g. create a sheet and place a view |
| GraphBIM/RevitMCPGraphQL | No licence (idea only) | A GraphQL read surface is a real idea, but a raw mutation path is ungated and a wrong link name silently falls back to the host model | Field selection in one composable read |
| shuotao/REVIT_MCP_study | No LICENSE file; package.json says MIT (idea only) | A large teaching project; its value is the knowledge system (94 SOP files, 61 skills, "anti-lessons") | An append-only anti-lessons file and a "what the tools cannot do" file |
| okuno-dsi/revit-mcp-toolkit | Apache-2.0 | The largest command surface (492 command files) with a durable job queue; high-risk methods need a preview then a token | A confirm token bound to a document fingerprint and a time limit |
| General-Soju/BimOnMcp | MIT | Revit, Navisworks, AutoCAD and Dynamo over per-process pipes; the installer writes the Claude config; ungated IronPython | A job protocol with instance and document pinning, idempotency and an explicit "unknown" |
| Thestreetarckitect/revit-2027-mcp | Apache-2.0 | A binary repackaging of rvt-mcp v0.5.0 with no source; a credential is committed in the repo | - |

### h. Very many tools, several apps, in-Revit chat (14 repos)

| Repo | Licence | One-line verdict | Best part |
|---|---|---|---|
| DharshanParasu/RevitMCPBridge2026 | MIT (a copy of WeberG619's project) | 1,000+ methods behind 8 MCP tools; writes on; its smoke reports grade themselves 100% with nothing run | Methods found by attribute; one passthrough keeps the tool list short |
| hmertkaramann/aec-pilot-mcp | Proprietary (no code) | README only; "550+" cannot be checked | - |
| BURAKAFRIDI92/BURAK-BIM | Apache-2.0 | "About 1,100 tools", padded by generated catalogues; run-code on | Per-client tool caps and schema clean-up |
| rui-branco/revit-mcp | MIT | 88 tools, HTTP with no password, .NET 8 only | A readable log of the dialogs it answered |
| UV-Tech/revit-claude-mcp | MIT | Small; delete asks nothing | - |
| Dave5264/transom-revit | No licence (idea only) | The best Excel round trip seen; run-any-C# on by default | A three-way compare with a hidden UniqueId anchor and a unit confirm step |
| scwlkr/RevitThyme | GPL-3.0 (idea only) | The strongest evidence culture: records pin the source, every DLL hash and the Revit build; fixtures, not named models; 2027 only | Declared result checks kept apart from the outcome |
| expert-terminal/bim-agent | MIT | Honest about what was tested; Yes/No per write; 2026 only | A delete preview by a rolled-back transaction |
| constructelligence-lab/revit-mcp | MIT | Writes off behind two switches; only 2 write tools; mock tests | Local photoreal renders through Blender |
| loadingcloud001/LoadingAI-Revit | MIT | The only in-Revit chat with a real write gate; run only on 2027 | A review card with "deny with a reason" and "edit the inputs" |
| Sideswipe3751/bim-bridge | PolyForm Noncommercial (Apache only at an older commit) - idea only | An agent-independent gateway over a patched third-party Revit MCP | A grant over hashes of the request, the target and the effect, used once |
| BIM-MODE/BIM-MODE-Connector | Apache-2.0 | Installer and docs only; nothing checkable | Refuse network (UNC) paths in file tools |
| armanwu/revit-2027-mcp | MIT | A real .NET 10 build; HTTP with no password; delete asks nothing | - |
| roubaudal-maker/ClaudeRevit | MIT | An in-Revit Claude chat, one group per prompt, no write approval, the key in a plain environment variable | Element ids shown as clickable links |

### i. Skills, API-docs servers and knowledge packs (16 repos)

| Repo | Licence | One-line verdict | Best part |
|---|---|---|---|
| kaitpw/Rvt_Docs_MCP | No licence (idea only) | Scrapes two third-party sites; a credential is committed in the repo | - |
| Rckov/RevitDex | No licence (idea only) | Indexes the installed RevitAPI.dll, with a hash and an Autodesk-signer check | Stamp API evidence with the assembly's hash and signer |
| Cazorlas/RevitDocs.Mcp | MIT | Live rvtdocs search with the cache state on every answer; a licence ledger for code samples | A "cached, stale" label; a new-release watch |
| trgiangv/rvtdocs-mcp | MIT | Lists what is present per year; silently falls back to 2025 for an unknown year | Present, added and removed, per year |
| DTDucas/chm-converter | MIT | Converts the SDK help file to Markdown; "vectorDB" claimed, not seen in code | - |
| hamzaabduljabbar/autoConst-cutsheet-to-family | AutoConst source-available (idea only) | The best evidence chain seen: every value has a page and a quote; a blind second reading; a built-against-spec score | A blind second reader that must agree before the family is built |
| hamzaabduljabbar/autoconst-claude-bim-health-scorecard | AutoConst source-available (idea only) | An A-F grade; its YAML rulebook is not what its engine runs | Severity by project stage |
| hamzaabduljabbar/autoconst-spec-compliance-claude | AutoConst source-available (idea only) | PASS / FAIL / MISSING with page-anchored clauses, but the grade reads 100% with 86% of the data missing | MISSING as its own state |
| MakeItEzzz555/Gridline | MIT (Blender parts GPL) | Skills with measured host activation and negative controls; no Revit tool | Negative-control prompts per skill |
| zsenarchitect/archi-magic | MIT | Skills on pyRevit 7's runtime; a failure table counted from real sessions | "Top failures" shown where the next script is written |
| hamzaabduljabbar/autoConst-p6-revit-4d | AutoConst source-available (idea only) | Schedule-to-model 4D; questions as data; runs code through `execute_revit_code` | Questions with options, a default and a reason, carried across revisions |
| ProfRino/Nonica-Revit-MCP-Skill | No licence (idea only) | Two skill editions, one lean for small local models | A decision-tree edition for small models |
| AUTOM8LABS/mcp-connector-skills | No licence (idea only) | One operator skill per host app | "A capped preview is not a complete inventory" |
| kilkellym/archsmarter-ai-experiments | MIT | JSON design intent plus a script that builds it | A schema plus a semantic validator |
| kilkellym/ArchSmarterRevitMcpServer | No licence (idea only) | A tool layer; its lesson is that families need step-by-step feedback | A family-document guard on every family tool |
| NicheSam/revit-mcp-research-partner | No licence (idea only) | A prose skill; depth scaled by risk | - |

### j. Beyond GitHub: Autodesk samples (3 repos; products are in the section above)

| Repo | Licence | One-line verdict | Best part |
|---|---|---|---|
| autodesk-platform-services/aps-sample-mcp-server-revit-automation | MIT (Autodesk) | A "vibe-coded" sample: 3 tools run headless on cloud models; HTTP with no password while it holds cloud credentials | A headless cloud route (Design Automation) |
| autodesk-platform-services/aps-sample-revit-mcp-tools-bundle | MIT (Autodesk) | Plain transactions; after a write it syncs with central and relinquishes on its own - exactly what Heron's Article 10 forbids | Open a big model in two passes, with link worksets closed |
| chuongmep/mcp-revit-sample-au2025 | MIT | An AU 2025 class sample over a CSV, not a live Revit; one advertised tool never registers | - |

Also named in note j, not studied: NonicaTeam/AI-Connector-for-Revit (Nonica's public repo), EvgenVer/AECPilot (relation to AEC Pilot unknown), labsfuture/revit-converter-remote-mcp-release, bimgeek/speckle-mcp, Daviidro/speckle-mcp, SamllPigYanDong/revit_mcp.

### k. The long tail (466 swept by README and file list)

18 small repos with an idea the bigger projects lack:

| Repo | Licence | One-line verdict | Best part |
|---|---|---|---|
| ArchitectureWorld/revit-mcp-automation | MIT | Runs its own Revit 2020; reuses the big family's tools | A dialog mid-change parks the job, is answered against a fresh snapshot, and the write is never resent; dangerous dialogs are never auto-clicked |
| Zhenko-J/rift | Apache-2.0 | Pre-alpha; writes only to a file named as a sandbox copy | Every read inside a group that is always rolled back; signed, replay-proof pipe messages |
| vibe-bimer/smart-revit | MIT | Read-only until an administrator allows actions | Write permission in an admin-owned policy the user's own account cannot write |
| unknown-sama-ff/revit-auto | Apache-2.0 | A standing approval scoped to named files | The permission file is fingerprinted at start; any edit blocks writes until reload |
| PerSpec-Tech/Datum-Compliance-Checker | Apache-2.0 | Pune bylaw checks; a hackathon build | An "unverifiable" outcome; missing inputs asked in order of how many checks they unlock |
| realitymatrix/openstrate-outlet-height | MIT | One outlet-height rule, on the desktop and in the cloud | "Measured, not skipped": what could not be changed is still reported, with the reason |
| aisbergen/RevitHistoryLogger | No licence (idea only) | Logs the modeller's own commands for the AI | A "what changed in this session" reader built on DocumentChanged |
| huynvk19-cell/claude-using-revit | No licence (idea only) | Drafting rules learnt from the user's corrections | A corrections log with status and "superseded by" |
| phamduybill2005-creator/CH-M-I-M-AGENT | MIT | An agent benchmark; no Revit case yet | Six hard fails, such as "said it worked, but the state check failed" |
| NovaShang/bimdown-format-comparison | No licence (idea only) | Run by an interested party; one AI model | The only measured MCP-to-live-Revit test: 59% with a run-code tool |
| CHMOSE023/RevitMCP | No licence (idea only) | Revit 2019-2024 from one source | Measured the cost of duplicated reply text; compact by default |
| BhaveshY/revit-mcp-next | MIT | A small bridge with a careful installer | An installer dry run; an exact "fix:" line on every error |
| studioliquid-lab/revit-mcp-tool-generator | MIT | The AI writes new pyRevit tools; verified on 2024 only | A static check that preview code cannot write |
| kolosimo/MCP-x-REVIT-2025 | No licence (idea only) | Copies Autodesk's preview tool names on Revit 2025 | Tool names compatible with Autodesk's server |
| laytonluo/revit-dwg-to-dxf-skill | MIT | A skill document, not a tool | Read CAD underlay text through a temporary DXF |
| byggstyrning/revit-pset-validator | MIT | A narrow check of IFC property-set files | Rules ported from Revit's open IFC exporter |
| Messonjia/revit-elec-mcp | No licence (idea only) | Electrical checks to NEC, in development | Breaker and motor-circuit checks, a discipline Heron lacks |
| edmaceymacleod/ca_elevation_review | Apache-2.0 | An as-built check from phone photos and LiDAR; not yet run in Revit | A table of what the check can and cannot honestly verify |

The other 340 Revit repos. No new idea for Heron in these: mostly copies of an upstream, single-version bridges, guides, installers or empty repos. Listed so nothing is lost. The verdict is note k's one-line reading of the README and file list.

| Repo | Licence | One-line verdict | Best part |
|---|---|---|---|
| datadrivenconstruction/CAD-BIM-to-Code-Automation-Pipeline-DDC-Workflow-with-LLM-ChatGPT | MIT | Offline converters (RVT/IFC/DWG/DGN to Excel) plus n8n and LLM-written Python analysis; no Revit API, no MCP | - |
| aussieBIMguru/BimGo | MIT-0 | First-person walkthrough app plus slim Revit add-in; live session pushes edits back to Revit; offline .bimgo file keeps an edit journal (undo/redo) to push later | - |
| EverseDevelopment/ZoidbergAI.Addin | MIT | 2024 tutorial add-in that runs AI-generated code in Revit (YouTube course companion) | - |
| aydimir95/RevitMCPInstaller.exe | none (idea only) | README and .addin only: a Windows installer (binary, no source) for the original revit-mcp plugin; no licence | - |
| ChimingLu/AutodeskDynamo_MCP | Apache-2.0 | Dynamo-through-MCP bridge (Traditional Chinese); v3.6 adds a Dynamo node registry and reusable graph patterns; Apache-2.0 | - |
| OriAshkenazi/ai-pyrevit-developer-template | MIT | pyRevit developer template with a Codex plan-to-tasks workflow; a developer tool, not a modeller tool | - |
| Utopia5327/claude-plugin-for-revit-bim | MIT | Claude Code plugin of 25 markdown skills and 3 agents (Revit, Dynamo, ACC knowledge); no Revit connection | - |
| ara3d/bim-open-toolkit | MIT | Stores a model as plain tables (BIM Open Schema); each question becomes a small graph a person and an agent edit the same way, so the answer comes with its steps | - |
| imkcrevit/RevitMCP_Blog | none (idea only) | Blog companion: all-C# MCP inside Revit, README says tested on 2021, 2023-2026; no licence | - |
| datadrivenconstruction/ChatGPT-with-Revit-IFC | MIT | Streamlit chatbot over an Excel export of RVT/IFC; no live Revit | - |
| archsalem101/Cutom_Revit_MCP_server | MIT | TypeScript MCP server to C# add-in over HTTP :8080; generic read/create tools | - |
| Evgivachev/erp-public | MIT | Public showcase page (no source) for a Russian electrical (lighting, circuits, panels) Revit plugin with a Claude/MCP lighting-layout demo; claimed, not seen in code | - |
| nguyenngocdue/DeepBIM-MCP-Revit-Plugin | MIT | TypeScript server, C# add-in and command set; README credits mcp-servers-for-revit architecture | - |
| romanzarkhin/revit-mcp | MIT | Copy of the original revit-mcp TypeScript server | - |
| ryanchan720/copilot-for-revit | MIT | Chinese "AI is the brain, plugin the hands" command framework; part of a small ecosystem of repos | - |
| zymorel/revit-mcp | none (idea only) | Python stdio server to C# add-in over WebSocket 8765, Revit 2024-2027, BIM/GIS tools (French); no licence | - |
| abdallahmostafataha6-blip/A-M-T | none (idea only) | Marketing page (HTML/images) for a Revit productivity add-in; no source | - |
| annquin/revit-bim-workspace | none (idea only) | Personal pyRevit + RevitMCP workspace with a memory folder and structural notes | - |
| antonhofstader/Revit-2026-MCP-Server | MIT | Small Revit 2026 MCP (3 C# files, Python server) | - |
| BIM4GIB/MCP.Revit | none (idea only) | Claude-to-Revit MCP whose README carries a per-release ElementId table (2022 onward); no licence | - |
| cleeefyy/acc-revit | none (idea only) | No README; six Python files (description: LLM code compliance for Revit); no licence | - |
| cleeefyy/doc_m_compliance_checker_Revit | none (idea only) | No README; four C# files (description: UK Approved Document M compliance with an LLM pipeline); no licence | - |
| DAS-WNorth/RevitMCP | none (idea only) | WebSocket :27015 bridge with a code-execution tool; no licence | - |
| EliLeung/Revit-MCP | Apache-2.0 | Revit 2026: several MCP client connections to one Revit, with a connection-management window; README says real multi-chat end-to-end validation is not done; Apache-2.0 | Several chat clients on one Revit (worth a second look later) |
| JaggerYu-ArchAI/archai-revitmcp-dynamo | none (idea only) | DeepSeek-based Dynamo/Revit design assistant, 40 tools, Revit 2024; no licence | - |
| jmcouffin/revit_claude_mcp | none (idea only) | Empty (title-only README) | - |
| LIUruchuan/revit-spatial-export | GPL-3.0 (idea only) | Revit 2021 add-in: whole model to AI-readable spatial JSON (mm, explicit coordinate basis) and back to rebuild elements; GPL-3.0 (ideas only) | - |
| MennoMekes/Python-scripts-for-Dynamo | none (idea only) | Two Claude-written Dynamo Python scripts; no README | - |
| rrdls/revit-mcp | none (idea only) | ChatGPT runs focused C# snippets in Revit; Inno installer; no licence | - |
| tell2jyoti/RevitBIMIntelligence | MIT | In-Revit agent with 9 typed C# tools for questions about the model | - |
| Venkatchavan/OpenAEC-MCP | Apache-2.0 | "Safety-first" MCP platform that separates protocol from host adapters; authenticated Streamable HTTP; Phase 1; Apache-2.0 | - |
| AlejoDuarte23/VIKTOR-REVIT-MCP | none (idea only) | VIKTOR app plus a forked pyRevit MCP extension | - |
| alrigithub/revit-mcp-stack | none (idea only) | 3XN office bridge, Claude Code to Revit 2025: run C#/Python, PNG results, company tools attach; no licence | - |
| Ariantabib/Arian-Revit-MCP-Plugin | MIT | In-Revit MCP for 2027: family-from-scratch, warning/dialog silencing, Dynamo, arbitrary Revit API code; MIT | - |
| BhaveshY/revit-mcp-codex-plugin | MIT | Codex plugin for revit-mcp-next; Windows 11 + Revit 2024 only | - |
| bic98/paulga-mcp | none (idea only) | README only: stdio MCP inside the closed PAULGA Studio app driving Revit/AutoCAD/Rhino/SketchUp/ZWCAD/GstarCAD | - |
| BIMO-Tools/Revit-Python-Scripts | MIT | Script library for the BIMO "Run Python" preset tool, documented for agent discovery | - |
| Booster1985/scanbim-mcp | none (idea only) | Cloudflare Workers over APS: upload/convert/view/clash for 50+ formats; cloud, not live Revit; no licence | - |
| CyberPotato0416/MCP-Tools-extension | GPL-3.0 (idea only) | pyRevit toolset packaging MCP-research functions for users without an agent; GPL-3.0 | - |
| dashy2004/CDCRD-Digital | Apache-2.0 | Dominican Republic building code with page-level source references, linked to Revit/ETABS/SAFE/Civil 3D; Apache-2.0 | - |
| Davud77/bp-revit-tools | AGPL-3.0 (idea only) | BotPlus RT: Russian Revit 2026 suite (HVAC/plumbing/electrical tabs) with an MCP panel; AGPL-3.0 | - |
| felipemotelewicz-prog/instalador-revit-com-ia | none (idea only) | README for AutoRevit.exe (Portuguese course installer: Claude Desktop to Revit, AutoCAD, Obsidian); no source | - |
| formcraft/formcraft-revit-tools | none (idea only) | Office Revit suite tied to a written BIM standard (FMC-BIM-STD-002) with an MCP; ~2000 test-path files; "Rhino proof before Revit"; no licence | - |
| h4child/mcp-revit | MIT | Portuguese Python server + .NET add-in for Revit 2025 | - |
| hungpixi/revit-mcp | MIT | "Production-grade" TS + C# bridge for Revit and Dynamo (Vietnamese/English); marketing-heavy README | - |
| HuuPhucGL/HP-skill-revit-VN | MIT | Vietnamese Claude skills for townhouse drawings on top of mcp-servers-for-revit | - |
| jdwit/revit-open-pointcloud | MIT | Links open point-cloud formats (PLY/E57) into Revit, documented for agents too; MIT | - |
| KuklevDS/clawd-workspace-public | none (idea only) | No README; personal OpenClaw agent workspace (BIM bill-of-quantities, tenders) with Revit MCP pieces; no licence | - |
| kyawswar-22/gemini-revit-mcp | MIT | Gemini web chat through the MCP SuperAssistant browser extension and pyRevit; user reads the code then clicks Run | - |
| labsfuture/revit-converter-remote-mcp-release | MIT | Release-only: hosted Revit converter (RVT to XKT/IFC/DWG) MCP from Labs Future | - |
| MahmoudEldesokey/giraffe | none (idea only) | No README, 5 uploaded files; describes Gemini driving Rhino/Revit/3ds Max; nothing to inspect | - |
| Manoz2201/DynamoRevit-MCP-Tools | none (idea only) | Tiny pyRevit/Dynamo scripts; README misdefines MCP as a generic "framework" | - |
| mzahirnia30-cell/deadalus | unclear (idea only) | Empty (two-line README describing an Electron + QLoRA idea) | - |
| NilayDev/revit-mcp-claude | none (idea only) | One JS file and a Claude Desktop setup note | - |
| OOXGN-OOX/RevitMCP | AGPL-3.0 (idea only) | Russian local MCP bridge for Revit 2026 (C# + Node), v0.0.1; AGPL-3.0 | - |
| paulohenriquesn/revitapi-claude-context | none (idea only) | Claude Code plugin of 2,163 markdown files: the Revit API 2026 reference that auto-loads when Revit code is touched; no licence | - |
| polaris97s/Aeclyra-revit2018.3-MCP | MIT | Local loopback MCP for Revit 2018.3 only (API 18.3), TS server + C# add-in + Inno installer; MIT | - |
| pradeepmisal/AutoDesk-Revit-ClashGuard-MCP | none (idea only) | MEP clash detection/prioritisation through Claude; 55 MB repo, mostly a vendored Python tree; no licence | - |
| PranayS27/MCP-Revit-Project | none (idea only) | Empty (two-line README; "MCP" read as "Model Coordination") | - |
| Quincy987/VibeModel | none (idea only) | AI chat panel inside Revit 2023; project journal.md with one line per modifying command and per chat turn, [source: ...] markers in answers (Services/Chat/ProjectJournal.cs); no licence | Project journal plus source markers (worth a second look later) |
| RAJ-IITROORKEE/revit-datumm-plugin | none (idea only) | DATUMM add-in + TypeScript MCP + hosted connector + contracts + Inno installer source, Revit 2026 patch-range policy; no licence | - |
| rameztawfik/Revit-MCP | none (idea only) | pyRevit/IronPython stdio MCP, 2 Python files | - |
| ru5hikesh/Revit-MCP | none (idea only) | WPF (.NET 8) desktop assistant with Gemini and a .NET MCP server over named pipes | - |
| rvbuilds/revit-api-tools | MIT | Same code and README as ru5hikesh/Revit-MCP (Gemini WPF assistant) | - |
| ryanolson/dynamo-mcp | MIT | Rust MCP that indexes Dynamo documentation for search; no live Dynamo or Revit | - |
| sergiomauz/mcp-server-dotnet-bimengine | MIT | .NET MCP SDK test written for a job application ("to be shown to Aurora") | - |
| Simone-Balin/revit-mcp-claude-code-setup | none (idea only) | Single-file guide to connect Claude Code to Sam-AEC's Revit MCP | - |
| TripleHHH-ui/Revit_Automation_Server | none (idea only) | Dynamo script + Python bridging Revit and Notion through Claude | - |
| username-200/MCP-revit | none (idea only) | Russian wrapper around LuDattilo/revit-mcp-server: install guide, sheet-set templates, survey notes | - |
| uzair-codes/Connecting-Claude-with-Revit-2024 | none (idea only) | Single-file setup guide (pyRevit + mcp-server-for-revit-python) | - |
| wyuebei-cloud/autodesk-product-help-mcp | MIT | Hermes Agent skill pointing at Autodesk's official Product Help MCP (docs search) | - |
| xupeiwust/archbuilder-ai-system | none (idea only) | "ArchBuilder.AI" hybrid desktop-cloud concept with a large spec; little working Revit code | - |
| xxHRUSHIKESHxx/BIMIntelligence | none (idea only) | Groq LLM + structured tool calling inside Revit for questions | - |
| zsl95903-ui/BIM-AI-Studio | Apache-2.0 | Revit 2027 local-first layer: typed tool contracts, main-thread dispatch, transactions with rollback, named pipe, MCP forwarding; Apache-2.0 | - |
| 21nkant/bob-the-builder-showcase | none (idea only) | Showcase only (source private): electrical-prefab copilot proposing cited changes that are written only after engineer approval; claimed, not seen in code | - |
| 3d-mcp/revit-mcp | MIT | ArchBench pyRevit connector, experimental alpha with a written validation record that says which tests are not native-Revit tests; MIT | - |
| 5788324/Yang-Agent_Revit | none (idea only) | Personal assistant: AI-readable model reports, low-risk fixes only after confirmation; no licence | - |
| 6Nail/revit-mcp-setup | none (idea only) | One-command PowerShell install of pyRevit 7 + oakplank/RevitMCP for Claude Desktop (Russian) | - |
| 7alexhuang-ux/RevitMCP-Addin | none (idea only) | Gemini-first Revit MCP with Ollama fallback (Traditional Chinese) | - |
| 7alexhuang-ux/Slide-REVIT_MCP_Study | none (idea only) | No README; study slides tooling | - |
| Aaradhya-Dev-Tamrakar/revit-mcp | none (idea only) | Small pyRevit/ExternalEvent bridge, part of an "autodesk-mcp" family of repos | - |
| abrahamlinh-code/KGS-Revit-MCP | none (idea only) | Office repo merging the source of three popular Revit MCP servers (KGS Architecture, Vietnam); no licence | - |
| AchyErrorJ/RevitMCP | none (idea only) | Python MCP + C# add-in for 2024-2026, family editor work in progress; 157 MB; no licence | - |
| adelinoles/765t-development-Revit | MIT | 765T Revit Agent product repo (Vietnamese), 424 C# files, a 'mission approval' path, install/verify/smoke scripts; MIT | - |
| aecoder-br/revit-ai-powered-plugin-template | MIT | Multi-version add-in template with WPF, local MCP bridge, AI gateway, agent docs; MIT | - |
| AhmedXAlDeeb/RevitLLMBackend- | none (idea only) | FastAPI + LangChain + Ollama code-compliance review backend for Revit exports | - |
| ajayabnave-ctrl/Revit-AJ-MCP-Assistant | MIT | Revit 2025+ plugin + MCP with an ISO 19650-style MIDP master drawing list sheet-batch tool; MIT | - |
| AleksandrPidlozhevich/RevitMCP | MIT | .NET MCP client that calls a chosen LLM, WiX installer; MIT | - |
| AlexandroLeone/revit-mcp | none (idea only) | Empty (placeholder note) | - |
| alexiVera/guia-mcp-revit-python | none (idea only) | One HTML guide page (Spanish) | - |
| alexiVera/guia-pyrevit-mcp | none (idea only) | One HTML guide page (Spanish) | - |
| AlexSk-sys/revit-mcp-bridge-public | none (idea only) | JSON-RPC 2.0 TCP bridge for a live Revit 2019 session, driven by the stock mcp-server-for-revit; no licence | - |
| AltControlIT/revit-mcp-sdk | MIT | Copy of the revit-mcp SDK interfaces (2025) | - |
| AndreyStartsev/revit-mcp | MIT | pyRevit bridge tuned for Google Antigravity; MIT | - |
| Andy-rba30/revit-mcp | MIT | pyRevit MCP for 2024-2027, "48 tools"; same README text as DavidChin0 and Mvclr repos | - |
| Andyhdr/Bimwired-skill | unclear (idea only) | German electrical-design knowledge skill (DIN 18015-1, VDE) plus a public read-only MCP; markdown only | - |
| angellofinetti/IA-BIM-Electrico | none (idea only) | Team project outline (Spanish electrical BIM), no code | - |
| anntfd-debug/RevitMCP | MIT | Empty | - |
| antonrathor24-glitch/revit-mcp-ai | none (idea only) | Empty | - |
| Arielpapu420/Creador-de-video-para-redes-revit-estructural | none (idea only) | Empty (one-line README about a video script) | - |
| astroarkhe/ASSILI-MCP-SERVER | none (idea only) | Empty | - |
| AtanasGe/DynamoMCP | none (idea only) | Empty | - |
| audunopdal/RevitMCP | none (idea only) | Read-only C# MCP for Revit 2026 over a current-user-only named pipe, API work inside ExternalEvent; ships two .rvt test models; no licence | - |
| avbsab-wq/revit-mcp-server | none (idea only) | Empty | - |
| aveusalex/mcp-servers-for-revit | MIT | Office fork of mcp-servers-for-revit: replaces the plugin's listener with a loopback broker (127.0.0.1:8090) needing a shared token, with JSONL audit; MIT | Replaces the open listener with a loopback broker, shared token and audit |
| Axotopia/revitmcp | MIT | Proxy putting Autodesk's Revit MCP behind AnythingLLM for local LLMs ("intent engineering" MVP); MIT | - |
| aydimir95/revit-mcp-plugin | MIT | Fork of revit-mcp-plugin "up to Revit 2024" | - |
| ayfadili2022-glitch/revit_claude | none (idea only) | French scaffold of an "audit-first MVP" production assistant from PRD documents; no licence | - |
| AZZRE/revit-ai-verification | none (idea only) | README-only portfolio: "AI proposes, engineers approve, the system verifies" with Ollama, Revit 2024; claimed, not seen in code | - |
| B45Labs/B45Labs_Connect | none (idea only) | Empty | - |
| bctw123/revit-mcp | none (idea only) | Empty | - |
| bhanuabbichetty/RevitAIConnector | none (idea only) | Revit 2025 + Cursor connector, "70+ tools", workflow hints, export validation; no licence | - |
| BIM-Dad/IRIS-codex-revit-mcp | none (idea only) | Read-first sheet/view checks PoC with a phase-1 validation note | - |
| BIMBrain/McpRevitPlus | MIT | Empty initial commit | - |
| bimmonkey-ai/bimmonkey-claude-plugin | unclear (idea only) | Plugin listing for a closed product that learns a firm's drawing standards from its permit sets; claimed, not seen in code | - |
| Breakstrings/31052026 | Apache-2.0 | "GABIM" drawing-to-BIM claim for Revit 2026 (grids and levels in code); Apache-2.0 | - |
| carlofr/MODELO-IA | none (idea only) | Empty | - |
| catsony5-web/revit-mcp | none (idea only) | Korean: HTTP MCP inside the add-in (127.0.0.1:8090) with C# execution, Revit 2024, PowerShell setup with a recorded clean-clone check | - |
| changgeng1009/horizun-revit-mcp | none (idea only) | Simplified-Chinese adaptation notes for HorizunGroup/horizun-revit-mcp, releases only | - |
| Chayan-Paul/PaulAI-Revit-Copilot | none (idea only) | Showcase README (no source) for a local-LLM sheet automation | - |
| chen-dongchai/RevitMcp | MIT | Revit 2022 MCP template (Chinese); MIT | - |
| congtytnhhtvxdanhnguyen-hash/mcp-servers-for-revit-2027 | MIT | Copy of mcp-servers-for-revit with a Revit 2027 Curve.Intersect fix | - |
| cosentinor/mcp-revit-server | none (idea only) | Small Python MCP + Revit extension | - |
| creativeengineer224-a11y/Revit-AI-Assistant | none (idea only) | Empty | - |
| cxcc5876-maker/mcp-REVIT | MIT | Copy of mcp-servers-for-revit (172 MB with binaries) | - |
| d14znsol/bimvn-revit-mcp | unclear (idea only) | Vietnamese DSCons MEP MCP for Codex/Claude Code/Antigravity, step reports in Word | - |
| Daniel26-lgtm/MCP-revit | none (idea only) | Empty | - |
| DavidChin0/revit-mcp-stdio | MIT | Same pyRevit "48 tools" server as Andy-rba30, with IronPython snippets | - |
| Davidvolkmar/MCP-REVIT-VOLK | Apache-2.0 | Copy of bimwright/rvt-mcp v1.2.2 (assigned elsewhere) | - |
| Daviidro/awesome-aec-mcp | MIT | Curated list, 16 AEC MCP entries | - |
| Davud77/WEAM.Revit.AI | AGPL-3.0 (idea only) | Russian Revit 2026 add-in: preview_tag_mep/apply_tag_mep pairs for MEP tags, A3 sheets, read_documentation; AGPL-3.0 | - |
| desarrollospaloblanco/mcp-revit | MIT | Spanish fork of mcp-servers-for-revit focused on parameter read/write | - |
| desarrolloteambimcivil/tbc_mcp_revit | Apache-2.0 | Rebranded derivative of bimwright/rvt-mcp (same tagline), v1.3.4; Apache-2.0 | - |
| desarrolloteambimcivil/tbc_mcp_setup | Apache-2.0 | One Inno Setup installer for the TBC Revit + AutoCAD MCP suite; Apache-2.0 | - |
| Desoxi1/ARQ-IA | none (idea only) | Chat inside Revit 2027 and Dynamo 4.0.2 connected to Codex; no licence | - |
| dolanklock/Revit-MCP-Claude | none (idea only) | Markdown-only notes for a read-only Claude Desktop server | - |
| domewaradet/MCP-Revit | none (idea only) | No README; Revit add-in + Python server | - |
| DonaldLu/Sinotech_MCP | MIT | Sinotech (Taiwan) TS server + C# add-in, Revit 2020-2026, TCP 8080; MIT | - |
| douglascg7-art/mcp-server-for-revit-python | none (idea only) | Empty | - |
| Dreamsigner/Dreamsign_Revit_MCP | MIT | French Revit 2027 MCP: the add-in makes the user explicitly choose the authorised project; validation report separates automated tests from real trials; MIT | - |
| edsonlimaarq-sudo/Edson | none (idea only) | Empty | - |
| eichler-ai/connectors | Apache-2.0 | MCP connectors for Revit, Rhino and Excel with a shared hub and SDK; 299 test paths; Apache-2.0 | One hub fanning out to many live app instances (worth a second look later) |
| ELMOSK/revit_mcp | none (idea only) | A specification essay, no code | - |
| Elprofessor1990/revit-mcp-server-nbs-nordic | MIT | Fork of LuDattilo/revit-mcp-server adding NBS Nordic specification data to the same MCP; MIT | - |
| Emmagrace0130/Architecture-LLM | Apache-2.0 | Local building-code RAG/GraphRAG + floor plans + Revit add-in; Apache-2.0 | - |
| emnss91/claude_mcp_revit | none (idea only) | Empty | - |
| enternalsaga/revit-agentic-flow | none (idea only) | Vietnamese install guide wrapped around mcp-servers-for-revit | - |
| erivas-droid/revit-bimwright-herramientas | none (idea only) | Extra C# tools for bimwright/rvt-mcp, tested on Revit 2025 (Spanish) | - |
| esanzgadea/brief-to-bim | none (idea only) | Brief to generative layout (BSP circulation) to Revit via Dynamo; proof of concept | - |
| eska5/Claude_higgsfield_revit_wizualizacje | none (idea only) | Article companion: Claude + Higgsfield image generation + Revit 2027 renders (541 MB) | - |
| ewuiyud/revit-mcp-dify | MIT | mcp-servers-for-revit adapted for self-hosted Dify, batch parameter commands | - |
| Fananiel/my-revit-mcp | none (idea only) | Copy of the revit-mcp server/plugin pair | - |
| Fanchiang0418/20261002_Revit-MCP-Learning | none (idea only) | Learning notes (and a snack file) | - |
| fankai0102/mcp-revit-blender | none (idea only) | Empty | - |
| felipemotelewicz-prog/autorevit-us | none (idea only) | Empty (course installer placeholder) | - |
| fulviofb/revit-claude-mcp | MIT | Portuguese guide: Claude Code to Revit 2027.3 through pyRevit 7's agent runtime | - |
| gabrieloliveira-crypto/zeit-revit-mcp-server | MIT | Same pyRevit "48 tools" server with a Portuguese partner-install note | - |
| Ganesh-Chavhan/RevitMCP_PLugin | none (idea only) | No README; first commit of a small C# plugin (31 MB, mostly binaries) | - |
| GGomesBRA/revit-mcp | none (idea only) | Two-line README, Python code | - |
| Giovanni1708/MCP-Revit-V2.0 | MIT | Copy of mcp-server-for-revit-python with a Dutch install guide | - |
| haivo088/261011_bimtech-revit-mcp | none (idea only) | Vietnamese learning build (Claude Desktop to Revit 2025) with recorded test results on Revit | - |
| HamezMM/Claude_Code_Revit_Test | none (idea only) | Claude Code experiment, a to-do list README; no licence | - |
| hazzem0/HZ-BIM | MIT | MCP + Revit and Navisworks plugins, "267 tools", built-in Gemini chat, Power BI export, Setup.exe; same codebase as hienphan0111/BIM-Bot; MIT | - |
| HelloHowAreYouHaveANiceDay/bim-cli-plugin | MIT | Claude Code plugin for the bim-cli product (Revit 2024-2027, drawing-set PDF checks, US site/flood data); MIT | - |
| hienphan0111/BIM-Bot | MIT | Same product as hazzem0/HZ-BIM (description says 183 tools, Revit 2020-2027); MIT | - |
| hilldevelopment/hill-revit-mcp | none (idea only) | Add-in + MCP built on the mcp-servers-for-revit architecture; no licence | - |
| HoareLea/CJ_Revit-mcp-main | MIT | Copy of the original revit-mcp TypeScript server | - |
| huangruipao/RevitMCP | unclear (idea only) | 2025 DDD/CQRS-layered Revit MCP skeleton (Chinese) with unit tests | - |
| huy17012001huy-cyber/dscons-revit-mcp | unclear (idea only) | Same DSCons MEP MCP as d14znsol/bimvn-revit-mcp | - |
| inno-arq/claude-revit-bridge | none (idea only) | One-click Inno installer for a Revit bridge; no licence | - |
| iofbim/IofBIM_revitMCP | Apache-2.0 | Add-in exposing strongly typed commands over HTTP JSON; Apache-2.0 | - |
| ioseluiz/mcp_server_copilot_revit | none (idea only) | No README; Azure-deployed Python server for Microsoft Copilot | - |
| ioseluiz/my_revit_mcp_server | none (idea only) | No README; doors/windows tools | - |
| ioseluiz/revit_mcp_copilot_studio | none (idea only) | No README; Copilot Studio connector with Inno installer | - |
| isaddiq/Claude-BIM-development-skill | none (idea only) | Claude Code skill marketplace for BIM software development (Revit API, Unity, IFC) | - |
| Iteran96/claude-revit-integration | none (idea only) | Empty | - |
| jabluetooth/AEC-Agent | none (idea only) | AutoCAD + Revit agent focused on Los Angeles codes (CFC, NFPA 72, CEC, CPC, CMC, Title 24), ~886 test paths; no licence | - |
| JacksonPinto/RevitMCP_Install | GPL-3.0 (idea only) | Zipped .bat + PowerShell installer for a Revit MCP server; GPL-3.0 | - |
| jadjhaddad/aec-scaffold-mcp | none (idea only) | No README; MCP wrapping an "aec" scaffolding CLI for Autodesk/CSi/Dynamo plugins | - |
| jaredholloway94/Revit-MCP | none (idea only) | No README; Python tools incl. conduit/cable tray and annotation | - |
| Jarvi-Hou/OpenRevit | MIT | Revit 2024 MCP that keeps a minimal stable tool surface and composes workflows in the MCP client; MIT | - |
| jasonwarta/pyrevit-mcp | none (idea only) | Claude Code to Revit via pyRevit Routes, with an "adversarial review" fix round | - |
| JCast26/RevitMcp | none (idea only) | Spanish .NET MCP with named pipe; structural capital-reinforcement tools; no licence | - |
| JefaturaBIM94/RevitMCPBOQ | none (idea only) | Quantity take-off/BOQ engine for electrical and HVAC preconstruction through MCP (Spanish); sibling Navisworks repo; no licence | - |
| jesselei1102-hue/Revit-MCP-Server-Apps | MIT | mcp-servers-for-revit plus a human-in-the-loop Schedule App (filterable table, pending edits before Apply, checks tab); MIT | - |
| jiangkai002/revit-agent-cli | none (idea only) | LLM writes C# for Revit; desktop workbench + CLI + online demo, Inno installer; no licence | - |
| jmcouffin/llms-revit-api | none (idea only) | A single llms.txt file for the Revit API | - |
| JobiyoAI/RevitAIChat | MIT | React web chat front-end for a Revit MCP; MIT | - |
| josedelgadoq/my-revit-mcp | none (idea only) | Empty | - |
| josedelgadoq/revit-mcp | none (idea only) | Small add-in + Python server for clash detection between linked models (empty README) | - |
| Kaankrby/Parametrically-Compass | MIT | Offline Turkish building-regulation reader in pyRevit plus read-only MCP resources for agents (text, tables, SVG figures, archive PDF compare); MIT | Regulation text that keeps "awaiting confirmation" states (worth a second look later) |
| Kaiiser-ai/Revit-MCP-integration | MIT | Small Claude-to-Revit integration with an ethics/safety guideline document | - |
| kazmi-y/revit-mcp | MIT | Copy of the original revit-mcp TypeScript server | - |
| KazuhiroTakahashiAQ/revit-plugin-framework | none (idea only) | Revit 2024+ add-in scaffold meant to be handed to an AI chat to generate plugins | - |
| KazuhiroTakahashiAQ/RevitMcpPlugin | none (idea only) | Japanese: add-in hosts a localhost MCP endpoint, Claude Desktop reaches it through a stdio bridge | - |
| kdg200121-hash/revit-mcp-bridge-updates | none (idea only) | Update packages only (213 MB), no README | - |
| KenjiIT/autocad-knowledge | none (idea only) | Vietnamese knowledge pages for reading Revit-exported DWGs with an AutoCAD MCP | - |
| KenLP/bim-ai-architect | MIT | Markdown skill: a decision framework for which parts of a BIM AI app must be deterministic code and which an LLM; MIT | - |
| KevinKTM/revit-mcp | MIT | Copy of the original revit-mcp TypeScript server | - |
| KevinKTM/revit-mcp-commandset | MIT | Copy of revit-mcp-commandset | - |
| klim-bim/revit-basic-mcp | MIT | Minimal MCP with a code-execution tool and a model-snapshot tool; MIT | - |
| kmezacivil-byte/revit-mcp | none (idea only) | Peruvian in-add-in MCP for Revit 2021-2027 (v2 dropped the Python server), WPF windows; no licence | - |
| lachiemyatt/spec-assistant | none (idea only) | Claude project (prompts) for interior finish specs and schedules; no code | - |
| Lalitchitoshiya/Revit-MCP | none (idea only) | Specification-first build (docs are the source of truth), Phase 5 partial; no licence | - |
| laytonluo/revit-mcp-create-plan-views | none (idea only) | Batch plan-view creation via Claude Code + Revit MCP; beam pyRevit extension | - |
| LEEYUSEONG33/P6ix-Revit-MCP | MIT | Korean: Claude Desktop MCP plus a WebView2 dashboard inside Revit; MIT | - |
| libarquitectura-cmd/CLAUDE-REVIT | none (idea only) | Empty | - |
| Long0308/LPOpenBIM-RevitMCPSkill-backup | none (idea only) | Backup of a large Vietnamese MEP toolkit (591 MB, 890 md files) with an "AI agent governance" doc set; no licence | - |
| Longtran2404/chinh-thang-revit-mcp | Apache-2.0 | Private edition of bimwright/rvt-mcp with materials tools and an installed-Revit verification note; Apache-2.0 | - |
| louishhp/louis-revit-ai-mcp | none (idea only) | Vietnamese MCP built on rvt-mcp's architecture, Revit 2022-2027, early | - |
| Lubimka-dev/Revit-MCP-Releases | none (idea only) | Release feed only, no source | - |
| luisorbaiceta/oa-revit-mcp | none (idea only) | 2025 revit-mcp-style plugin with docs | - |
| maengenhariaarquitetura/conecta-revit | none (idea only) | Portuguese plugin linking Revit to Claude Desktop, Inno installer; no licence | - |
| Mahesh-Peddinti/mcp-servers-for-revit | MIT | Copy of mcp-servers-for-revit | - |
| mamun-7/Revit-MCP | MIT | Copy of the original revit-mcp TypeScript server | - |
| Marcin-Szul/mcp-integration-system | none (idea only) | No README; TypeScript orchestration around NonicaTab MCP | - |
| MasterMurali/RevitMCPPlugin | none (idea only) | No README; small C# plugin | - |
| MasterMurali/RevitMCPServer | none (idea only) | No README; small TypeScript server | - |
| mathieucostudio-ui/MCP-REVIT | MIT | Copy of mcp-servers-for-revit with an OpenRouter rename | - |
| matthewemerykeck24601/CursorRevitMCP | none (idea only) | Workspace: APS viewer, "Monty"/"Alice" assistant, Revit MCP; no licence | - |
| MAvinash24/autodesk-cad-mcp-agent | MIT | "Enterprise-grade" FastAPI + C++ geometry + RAG demo; its file list has no Revit add-in or C# | - |
| meet-rehman/revit-mcp-case-study | none (idea only) | Write-up: two existing connectors tested on Autodesk's Snowdon Towers sample; what worked, failed, and what not to trust | - |
| MervenurGombeci/mcp-revit-github | none (idea only) | Empty | - |
| mjcobb/PyRevit-Hatching-Extension | none (idea only) | Claude-written pyRevit extension that fills cut elements in a section | - |
| Mohamed-Elnahla/revit-mcp-github-copilot | MIT | Copy of revit-mcp TypeScript server adjusted for GitHub Copilot | - |
| mostafamahereid1946-spec/architecture-mep-bim-skill | MIT | "ArchMind" Claude skill for country-correct drawings across AutoCAD/Revit/SketchUp/3ds Max; MIT | - |
| MShaharyar1/RevitMcp | MIT | 3-file add-in hosting MCP over HTTP localhost:48884 inside Revit (via mcp-remote); MIT | - |
| Mvclr/revit-mcp-server | MIT | Same pyRevit "48 tools" server, tuned for ChatGPT over streamable HTTP, Inno installer | - |
| mydktb/MCP-server-for-Autodesk-Revit | none (idea only) | MKRevitMCP: read/write, warning audit, plans/sheets, a warning handler; no licence | - |
| narbeh82/Revit-join-elements-Claude-C- | MIT | Two files, Claude-written Join Elements code | - |
| Neonarjun/pyrevit-ai-one-click-installer | MIT | One PowerShell script: installs pyRevit 7 and switches on its built-in MCP agent runtime | - |
| nguyenngocdue/revit-mcp-server | none (idea only) | TypeScript MCP server with an .agents folder | - |
| ninhpham96/Revit-mcp-autotest | none (idea only) | Claude Code skill that makes testing through the real Revit/AutoCAD host mandatory, not just a clean build, with incident write-ups | - |
| northlarkhq/awesome-engineering-mcp | CC-BY-4.0 | Generated list of engineering MCPs (from Tresca: licence, versions, install steps, sources); CC-BY-4.0 | - |
| oakplank/revit-mcp | none (idea only) | No README; early 2025 C# plugin by the RevitMCP author | - |
| OliveiraCotta/Assistente_BIM_OCA | none (idea only) | No README; Portuguese Revit assistant uploads | - |
| OnePowerUser88/pyRevitFoundryMCP | unclear (idea only) | Static-analysis MCP for pyRevit extension developers (no Revit runtime); GPL-3.0 | - |
| onkarcctech/Revit-Wall-Automation-MCP-Server | none (idea only) | Tiny wall-creation demo | - |
| osc-bouw/RevitMCP | GPL-3.0 (idea only) | Dutch Revit 2025 add-in hosting a local MCP server inside Revit; GPL-3.0 | - |
| pahanitsch/revitapidocs-mcp | MIT | Revit API docs (descriptions, remarks, exceptions) + 15 years of The Building Coder as an MCP, no keys or embeddings; MIT | - |
| ParkerCai/cria-revit-mcp | Apache-2.0 | "Local-first, stateless MCP gateway" for Revit, 387 C# files, 74 test paths; Apache-2.0 | - |
| PATAGONIABIM/revit-mcp-revit-plugin | none (idea only) | Revit 2026 C# plugin + Node MCP, Dynamo bits (Spanish/English) | - |
| paulkotov/revit-mcp-server | none (idea only) | Small TS server + C# add-in | - |
| Pazran/mcp-revit-api | none (idea only) | Revit API docs (CHM converted to Markdown) as a search MCP | - |
| pedrobonilla198710-eng/revit-mcp | none (idea only) | Empty | - |
| phantom-logic/Automation-Pipeline-DDC-Workflow-with-LLM-ChatGPT | MIT | Copy of the datadrivenconstruction pipeline README | - |
| Plarmindo/revit-mcp-plugin | MIT | Copy of revit-mcp-plugin (2025) | - |
| Plarmindo/revit-mcp-python | MIT | Copy of mcp-server-for-revit-python | - |
| predator230814/RevitMCP | none (idea only) | BIM-IT: "vendor-neutral capability layer" for Revit with ADRs; the assistant is a separate process + DockablePane client; 705 C# files, 187 test paths; no licence | - |
| pyaephyoekyawjp/revit-mcp-bridge | MIT | Archived copy of the revit-mcp TypeScript server (points to the mono-repo) | - |
| RAEN-DT/PyNetVSCode | MIT | VS Code extension: federated BIM viewer + MCP into Navisworks, Revit, Civil 3D, Rhino, Tekla, RIB Presto; MIT | - |
| rafaelcruzmartins/mcp-revit-2026 | none (idea only) | Portuguese tutorials for Demolinator/revit-mcp-server via pyRevit | - |
| RAJ-IITROORKEE/revit-mcp-commandset-datumm | none (idea only) | Empty | - |
| RAJ-IITROORKEE/Revit-mcp-datumm | none (idea only) | Empty | - |
| RatebOmari/RevitMCP | none (idea only) | Empty (title-only README) | - |
| raventhetan/warehouse-generator-mcp | none (idea only) | Warehouse buildings by natural language through pyRevit Routes (Russian) | - |
| RavinduWeerakoon/revit-mcp-client | MIT | Copy of the original revit-mcp TypeScript server | - |
| realitymatrix/openstrate-revit-agent | MIT | WinUI chat-window Revit agent: proposes changes for human approval, renders views and looks at them, documented threat model after a security review; MIT | Threat model written after a security review (worth a second look later) |
| realitymatrix/openstrate-revit-bridge | MIT | Two-day hiring demo: live Revit as tool server to QA scan-to-BIM IFC deliverables; MIT | - |
| reneruano95/revit-mcp-mep | none (idea only) | MEP-focused MCP spec + Python; duct resizing from CFM read off the model; no licence | - |
| revitbridge/.github | none (idea only) | Organisation profile (revit-bridge itself is studied elsewhere) | - |
| revitbridge/revit-api-docs | MIT | Read-only Revit 2026 API reference search MCP (28,862 entries) for revit-bridge; MIT | - |
| revitbridge/revit-bridge-addin | MIT | revit-bridge add-in: 26 JSON-RPC commands incl. dynamic C#, Revit 2020-2021 and 2023-2026, local TCP or remote WebSocket, per-year pairing; MIT | - |
| revitbridge/revit-bridge-web | MIT | Container demo host relaying remote Revit add-ins to a web UI; MIT | - |
| reylorente1/revit-mcp-2026-Rey | MIT | Empty | - |
| Rirychh/revit-mcp | MIT | Traditional-Chinese TS server + C# add-in, deploy script for installed Revit 2022-2027; MIT | - |
| RiseNJ-YF/RevitPointCloudMCP | none (idea only) | In-Revit MCP that reads linked point clouds and creates walls/floors/ceilings from them; no licence | - |
| RonySalluca/MCP-Revit-Installer | MIT | Menu installer/repair for an existing Revit MCP across several Revit versions (downloads the official scripts); MIT | - |
| rtineoSTV/revit-mcp | MIT | Copy of mcp-server-for-revit-python | - |
| rubenmendoza1290/RevitMCP | none (idea only) | Minimal named-pipe scaffold for Revit 2026 modelled on NONICA's connector, align-to-reference tool; no licence | - |
| rubimtech/revit_knowledge_mcp | none (idea only) | Russian Revit knowledge MCP: semantic search over Qdrant collections plus other search modes | - |
| rugstan/revit-claude-for-yc | none (idea only) | Empty | - |
| rusbaltabek-commits/mcp-server-for-revit-python | MIT | Private working copy of mcp-server-for-revit-python | - |
| s81/Autodesk--mcp | MIT | One MCP server for live Revit and AutoCAD 2022-2027 sessions on the same PC over named pipes; MIT | - |
| SahinIkbal/RevitMCPTest | MIT | Named-pipe IPC demo add-in (2025) | - |
| samsonleegh/revit-coach | none (idea only) | Claude Project instructions for a Revit coach with Singapore CORENET X / IFC-SG notes | - |
| satviksinha-cctech/revit-property-extractor-mcp | none (idea only) | APS-based (cloud) property extraction and audit of an uploaded .rvt | - |
| ScanBIM-Labs/revit-mcp | none (idea only) | 8 APS (cloud) tools: elements, schedules, clash, IFC export; no live Revit | - |
| scotmos/revit-mcp-copilot-integration | GPL-3.0 (idea only) | HTTP REST wrapper so Copilot Studio / Power Automate can call a Revit MCP; GPL-3.0 | - |
| shenghungLin/https-github.com-shuotao-REVIT_MCP_study | none (idea only) | Empty | - |
| shervindavarifard/revit-mcp-bridge | none (idea only) | Revit-Claude bridge with a WiX installer; no licence | - |
| SHMH-MCPs/revit-mcp-releases | none (idea only) | Release-only free add-in for Revit 2023-2027 with Microsoft sign-in; no source | - |
| Shubham-L131225/REVITL_MCP | none (idea only) | No README; one uploaded archive | - |
| SimplyCoded/revit-mcp-bridge | none (idea only) | Node server + C# plugin to query and validate a structural model (Claude/Gemini CLI) | - |
| sindhupasham277/BIM_AGENT | MIT | "Agent NEXO": in-Revit agent, every change previewed as a dry run then applied; MIT | - |
| SinoDonald/Sinotech_MCP | MIT | Same Sinotech code as DonaldLu/Sinotech_MCP | - |
| SinoDonald/TYBIM_MCP | none (idea only) | Taiwanese Revit MCP with per-release MSI (2024-2026); builds columns/beams/slabs/walls from linked CAD; no licence | - |
| sky92archangel/revit-mcp-local-bridge | MIT | Command bridge for Revit 2020-2026 with one setup.exe that finds installed Revits; besides MCP it accepts plain inbox/outbox JSON files under %LOCALAPPDATA% with idempotent command ids; MIT | Plain-file inbox/outbox mode beside MCP (worth a second look later) |
| soewal19/eracore-revit-mcp | none (idea only) | pyRevit MCP described as hexagonal/DDD "walking skeleton" | - |
| Solken-bim-mx/solken-mcp-revit | MIT | Copy of mcp-servers-for-revit with a unit-conversion fix in parameter updates | - |
| SomeshProgARC/pox-revit-mcp | MIT | Design documents only for a read-only ChatGPT-compatible Revit bridge | - |
| sonhp91/Revit-MCP | none (idea only) | Empty | - |
| strahser/e-RevitMcp- | none (idea only) | OpenCode agent opens an .rvt and runs plugin windows, pipes/ducts (Russian) | - |
| Strategie-Digitali/villa-savoye-bimcraft-vs-revit | unclear (idea only) | Same prompt, same Claude model: Villa Savoye built in BIMCraft and in Revit 2026, all outputs published for checking | - |
| STVDigitalDelivery/revit-mcp | none (idea only) | Empty | - |
| SubarashiCode/Claude-Skills---Revit | MIT | Empty | - |
| sultandroid/SultanRevitMCP | none (idea only) | One factory's Revit system: bridge, skills, defaults, title blocks, per-project notes | - |
| SweMohdFaizan/RevIQ | unclear (idea only) | Source-available reference only (implementation closed): architecture, security model, wire contracts; claims Revit 2020-2027 | - |
| Taeky-Han/MCP | none (idea only) | No README; Korean MCP servers + E2E tooling | - |
| thaiscarvalhoarq-ux/https-github.com-mcp-servers-for-revit-mcp-server-for-revit-python | none (idea only) | Empty | - |
| thanhtranarch/t3lab-claude.skills-revit | MIT | AEC Claude skills: coordination, clash, PDF markup comparison, RFI aggregation; MIT | - |
| Thestreetarckitect/revit-family-creator | none (idea only) | Claude Code skill + example .rfa for authoring parametric families through rvt-mcp, with a gap analysis | - |
| tjxme/revit-family-maker | none (idea only) | Three markdown files: a Claude skill prompt for parametric family generation; no code | - |
| TomasMunoz77/stack | none (idea only) | Curated directory (Spanish, Chile) of AI-to-construction-software connectors | - |
| tony-hoang-ai-automation/revit-api-2027-with-claude | none (idea only) | No README; large Revit 2027 workspace whose last commit (2026-08-26) reads 'fix(security): strip injected remote-eval payload from analyze-llms-txt.js'; no licence | - |
| Top-Matrix/PyRevitMCPConfigurator | unclear (idea only) | Console app that sets up the mcp-server-for-revit-python extension for several agents | - |
| toshioams/MCP-Revit-Robot-Autocad | none (idea only) | Structural office tools across Revit, Robot and AutoCAD via MCP (Portuguese) | - |
| tperez-dat/RevitMCPServer | none (idea only) | Empty (title-only README) | - |
| Trungdev1611/RevitAPI_docs_mcp | none (idea only) | Local Revit API docs lookup MCP (Vietnamese), no dependencies | - |
| TStrathaus/revit-mcp-status | none (idea only) | Public status page scoring a private Revit-MCP (point clouds, 2D plans, IFC storeys to native elements) and listing open tasks; no licence | - |
| tuanank2112-crypto/revit-ai-native | none (idea only) | "Native agent runtime" for Revit 2024 over a named pipe, with a two-storey house build plan and an E2E runbook; no licence | - |
| vahidrahimiarchitect/Revit-AI-BIM-Agent-claude | MIT | README-only multi-agent framework claim; no code | - |
| VanChung003/Revit-Antigravity-MCP | MIT | pyRevit bridge for Antigravity, 2021-2027, presentation-plan colouring; MIT | - |
| verbenedictos08/revit-claude-connector | none (idea only) | Scaffold whose installer is built in GitHub Actions so no Visual Studio is needed; Inno Setup | - |
| Vik154/Revit_MCP_Assistant | MIT | No README; small C# assistant; MIT | - |
| viktor-platform/revit-viktor-local-mcp | none (idea only) | VIKTOR's official template: VIKTOR chat agent with read and colour tools for a live Revit | - |
| vizzedev/MCP_projeto_executivo_Revit | none (idea only) | Claude skill running a Brazilian architecture project from empty file to issue, office profile in JSON; no licence | - |
| vudang-95/MCP-Revit-Agent | none (idea only) | Vietnamese Revit 2021-2026 bridge with no Node.js and no outside URLs | - |
| warlends-source/civil-senior | MIT | Structural design agent for Brazilian NBR codes across Revit, Civil 3D, Robot and SMath; MIT | - |
| wbh0168/revit_mcp | MIT | Early 2025 TypeScript server (Chinese/English) | - |
| White167/WhiteRevit-releases | MIT | Installer-only fork of RevitCortex for Revit 2025 (Russian) | - |
| wisdjsjskdjskkwdj/antigravity-revit2018mcp | MIT | Single Python file driving Revit 2018 from Antigravity (draws grids/walls/columns from a screenshot or dimensions); MIT | - |
| wyyyz1937365497/mcp-server-for-revit-python | none (idea only) | Empty | - |
| Xhuynh16/Revit-mcp-plugin-glab | none (idea only) | Workspace for Claude Code driving Revit and Rhino/Grasshopper | - |
| xiuchuan666/revit-tunnel-mcp | none (idea only) | Shield-tunnel MCP (Revit 2025, pyRevit Routes): preview returns a SHA-256 token of the plan and create is refused unless it matches (revit_tunnel_segment_contract.py); no licence | Create refused unless it matches the hash of the previewed plan |
| xppp3082/Revit_MCP | MIT | Copy of revit-mcp-plugin with a command set | - |
| yakovman1/RevitMCPchat | Apache-2.0 | Dockable chat add-in for Revit 2022-2024 whose .NET 8 agent drives a separately installed rvt-mcp; Apache-2.0 | - |
| yetsin7/RevitMCP | none (idea only) | Spanish-documented bridge for Claude/OpenAI Agents SDK/Ollama via mcphost | - |
| yllld/MCP-Revit-Families | MIT | pyRevit MCP for the Family Editor in Revit 2021: check the open RFA, prepare a change plan, apply, with "source-traceable" adapters; MIT | - |
| ynan3000/emperor-revit | MIT | Free pyRevit HVAC toolkit with a Claude chat; MIT | - |
| yogisugiono598-netizen/yg-labs-pyrevit | MIT | Solo-architect pyRevit extension: 35 buttons, 29 HTTP routes, file bridge to Claude Desktop; MIT | - |
| YonseiBIG/revit-mcp-BIG | MIT | Fork of revit-mcp with "NADIA" element-type tools (Yonsei) | - |
| YonseiBIG/revit-mcp-commandset-BIG | MIT | Fork of revit-mcp-commandset with NADIA commands | - |
| YonseiBIG/revit-mcp-plugin-BIG | MIT | Fork of revit-mcp-plugin, translated | - |
| YUiCHI-Ai/Revit-MCP-Analysis- | none (idea only) | Analysis notes (Japanese), no code | - |
| YuWang2/revit_mcp | none (idea only) | Empty | - |
| YYChen168/CAD-Terrain-Revit-MCP | none (idea only) | Learning project: CAD terrain/building heights to Revit 2024; README states end-to-end not yet verified | - |
| zainislit7-jpg/revit-mcp | none (idea only) | Revit 2027 + AutoCAD 2027 MCP "with a token-lean design"; no licence | - |
| zbanner-wq/wardcraft-aecdm-mcp | MIT | Extended copy of Autodesk's AEC Data Model MCP sample (cloud) with all Revit categories; MIT | - |
| zmq6931/revit-mcp-Revit2022 | none (idea only) | No README; Revit 2022 JS + scripts (43 MB) | - |

50 repos for other BIM tools (Navisworks, Archicad, IFC, Rhino and others), listed, not studied. The verdict is the tool and the repo's own description.

| Repo | Licence | One-line verdict | Best part |
|---|---|---|---|
| 2ez4gcx/sketchup-mcp | MIT | SketchUp: SketchUp MCP server: let Claude and other AI assistants work directly in SketchUp - quantity takeoff, modelling, materia... | - |
| abdullah13793-commits/indeklima-twin | MIT | IFC / openBIM: Does every room keep its design promise IFC + sensor data - room-level verdicts and diagnosed faults, written back to IF... | - |
| aec-platform/bimq | MIT | IFC / openBIM: Read-only BIM query server for agents: structured queries over IFC/gbXML with a policy file, deterministic results, and... | - |
| alesdev88/Archicad-MCP | MIT | Archicad: MCP server for Archicad 29  connects Claude and other MCP clients to a running Archicad: delivery-readiness QA from YAML... | - |
| andyluu98/vn-mcp-blender | MIT | Blender/Bonsai (IFC): MCP server giúp AI dựng mô hình nhà 3D trong Blender từ dữ liệu kiến trúc Việt Nam: tường, sàn, cột, dầm, cầu thang, mái... | - |
| ascriba/freecad-bim-agent | MIT | FreeCAD: This repository contains a MCP server that provides a bridge between AI  and FreeCAD. Built on the FastMCP framework, it... | - |
| Barkie721/claude_sld_generator_system | none (idea only) | Electrical one-line diagrams: Deterministic, NEC-driven generator for electrical single-line diagrams: narrative to topology JSON, layout, SVG, Revit... | - |
| bcllcc/modulor | MIT | Geometry kernel: Agent-native 2D drafting + 3D modeling kernel - the geometry layer for the agent era. JSON in, geometry out. | - |
| BIM-MODE/BIM-MODE-AutoCAD-MCP | none (idea only) | AutoCAD: Security-hardened Model Context Protocol (MCP) server and stdio bridge for Autodesk AutoCAD (2021-2025) by BIM-MAN Compa... | - |
| Bimetrik/bimetrik-mcp | Apache-2.0 | Bimetrik (business): Official Bimetrik MCP server for AI assistants (Claude, ChatGPT, Gemini CLI, Cursor, VS Code, Codex): profitability, P&L... | - |
| Boti-Ormandi/archicad-mcp | MIT | Archicad: MCP server for Archicad automation via Archicad's built-in JSON API, with Tapir add-on support | - |
| chronista-club/archicad-mcp | MIT | Archicad: MCP server for Archicad  C++ Add-On SDK for full BIM access via Unix Socket. Low-latency alternative to JSON API. | - |
| Daviidro/speckle-mcp | MIT | Speckle: Connect Revit, Rhino, AutoCAD and more to AI agents via Speckle. Manage projects, models and versions through natural la... | - |
| hajun011103/ga-layout-optimizer | none (idea only) | Layout optimiser (IFC): Drawing-in, smaller-layout-out: function-preserving building & plant layout optimization (Claude Code plugin + MCP serve... | - |
| HorizunGroup/horizun-msproject-mcp | MIT | MS Project: MCP server for Microsoft Project: no Java, no Project install. Critical-path engine, DCMA 14-point assessment, verified... | - |
| HorizunGroup/naviscoord-mcp | MIT | Navisworks: Open-source MCP server for Autodesk Navisworks Manage 2024-2026: AI-assisted BIM clash detection, root-cause analysis, c... | - |
| Impertio-Studio/Speckle-Claude-Skill-Package | MIT | Speckle: 25 deterministic Claude AI skills for Speckle Data Platform. Object model, GraphQL API, Python/C SDKs, connectors (Revit... | - |
| JardiMargalefAgusti/bSDD-mcp | none (idea only) | IFC / openBIM: MCP server for the buildingSMART Data Dictionary (bSDD) API - BIM enrichment | - |
| jeffersonbim/skill-information-manager-ifc | MIT | IFC / openBIM: Lite Claude Code skill for IFC analysis (inventory, Revit-IFC mapping, IDS, bSDD, BCF) - fork of Information-Manager-IFC... | - |
| jmcaamanog/CE3X-MCP-Server | MIT | CE3X energy certificates: Servidor MCP para Automatizar Certificados de Eficiencia Energética con IA (Claude, Gemini, Cursor) Diseñado bajo la per... | - |
| JoniNykanen/ai-for-structural-engineers | Apache-2.0 | Eurocode calcs: AI for structural engineers and engineering students: verified Eurocode calculations in Python (Finnish NA), free PyNite... | - |
| justkmode/docctl-mcp | none (idea only) | ISO 19650 document control: An AI document controller for construction projects. docctl-mcp checks a project folder against ISO 19650 naming rules a... | - |
| kanatip-k/foundation-ontology-mcp | none (idea only) | IFC / openBIM: Read-only Python MCP server for validating BIM foundation elements against a versioned ontology. | - |
| KenLP/acc-forma-mcp-server | unclear (idea only) | Autodesk Forma (ACC): Safety-first MCP server for Autodesk Forma (ACC): use the hosted service at mcp.bimlynx.com, or self-host with your own... | - |
| lduan-tech/Drafting | none (idea only) | Precast drafting (AviCAD): Claude Code skills for precast concrete drafting: PDF shop drawings to AviCAD/IDAT PXML, and 3D BIM model builds (Revit/... | - |
| louistrue/ifcx-mcp | none (idea only) | IFC / openBIM: MCP server for authoring IFC5/IFCX building information models through AI assistants. Supports spatial structures, geome... | - |
| Makata-Studio/bim-mcp-docs | none (idea only) | Bim design review: Bim MCP server  connect your AI agent to Bim design-review | - |
| masoodkaramoozian/ifc-agent-tools | MIT | IFC / openBIM: MCP server and Claude skill for AI agents on BIM/IFC: quantity take-off, clash detection, natural-ventilation screening... | - |
| MeltFlexDevs/aibimtool-skills | MIT | IFC / openBIM: AI agent skills for designing buildings as IFC4 BIM models with AIBIMTool. Brief in, BIM model out: Claude, ChatGPT, Cur... | - |
| mikhalchankasm/NavisWorksMaster | MIT | Navisworks: Navisworks Manage 2024–2027 plugin suite and local MCP server for BIM coordinators  clash detection, coloring, viewpoint... | - |
| OpenAEC-Foundation/OpenAEC-Workspace-Composer | MIT | Claude Code workspace composer: Desktop app (Tauri 2 + SolidJS) to compose Claude Code workspaces with curated skill packages, MCP servers, hooks, comma... | - |
| PetarStoev02/archicad-mcp | none (idea only) | Archicad: Demo MCP server that lets Claude, Codex and other AI agents drive a running Archicad (25+) via its JSON API and the Tapi... | - |
| razbakov/bim-portal-mcp | MIT | German BIM Portal API: MCP Server for German BIM Portal REST API | - |
| roynertr/cobie-mcp | MIT | IFC / openBIM: MCP server for COBie Excel validation, updates, and PDF extraction | - |
| serjk12/ifc-bim-knowledge-base | none (idea only) | IFC / openBIM: База знаний по IFC / openBIM / Revit: стандарты, регламенты, open source и реальные вопросы людей со всего мира  для ген... | - |
| SherinWorin/sherin-tekla-mcp | unclear (idea only) | Tekla: MCP server for Tekla Structures 2025 - let Claude read, report on and safely model in Tekla through controlled C tools (... | - |
| SHMH-MCPs/navis-mcp-releases | none (idea only) | Navisworks: Navisworks MCP: free Navisworks Manage 2025–2027 plug-in with an MCP server. AI clash detection, sets, viewpoints and co... | - |
| Show2Instruct/bonsai-mcp | MIT | Blender/Bonsai (IFC): A Model Context Protocol server bridging Claude, Cursor, or other MCP clients to a live Blender + Bonsai (BlenderBIM) se... | - |
| Show2Instruct/ifc-bonsai-mcp | MIT | Blender/Bonsai (IFC): An MCP server that connects LLMs to IFC-based BIM workflows in Blender using the Bonsai add-on. | - |
| simerionon/Text2Floorplan | MIT | Floor-plan generation: Generate editable 2D floor plans and 3D models directly from text with Flow Matching. Runs locally without LLMs, API key... | - |
| Slimouzi/audit-bim-mcp | Apache-2.0 | IFC / openBIM: MCP server FastMCP pour l'audit BIM d'une maquette IFC contre le Cahier des Charges BIM I3F (CCH V3.x). Agents : Require... | - |
| Stv-devl/PlanPilotMCP | MIT | FreeCAD: MCP server that lets Claude draw 2D plans, 3D models and BIM data in FreeCAD | - |
| SUNY-MEC-MEIC-Lab/ga-layout-optimizer | none (idea only) | Layout optimiser (IFC): Drawing-in, smaller-layout-out: function-preserving building & plant layout optimization (Claude Code plugin + MCP serve... | - |
| tanishqbhattad/rhino-mcp | MIT | Rhino: Rhino MCP server: AI-assisted architectural modelling in Rhino 8 with Claude, ChatGPT, Codex, Gemini or local Ollama. 12... | - |
| thilo-h/render-director | MIT | Render pipeline: Director-generator-validator loop that turns BIM renders into photorealistic architectural visualizations: Claude writes... | - |
| vizletstudio/vizlet-claude-plugin | MIT | Model viewer: Claude Code plugin for Vizlet: lets Claude open, review and share 3D, CAD, BIM and point-cloud models as live, no-login... | - |
| wrzonance/SpecR | MIT | Specifications (DOCX): A system for construction document specification automation with round-trip DOCX support | - |
| xuantinhnbs-rgb/autocad-trimble-mcp | MIT | AutoCAD: MCP servers that let Claude and other AI tools drive AutoCAD 2022+ (49 tools, via COM) and Trimble Connect for Desktop (... | - |
| xuantinhnbs-rgb/navisworks-mcp | MIT | Navisworks: MCP server that lets an AI drive Autodesk Navisworks over COM  model tree, IFC/Revit properties, search, colour, isolate... | - |
| zeussilver/bim-ifc-mcp | none (idea only) | IFC / openBIM: MCP server that lets Claude screen IFC (BIM) models with deterministic, tested checks: room areas, door widths, fire doo... | - |

---

## The one recommended next step

**Make Heron check, after every Discard, every Undo and every proof's "nothing was kept", that the model really went back - and say so in Revit words if it did not.** Re-read the elements the change touched (Heron already selects them for the Keep box) and compare them with what they were before. For example: "Revit said the change was rolled back, but 8 view names still read HERONL2." Heron's whole promise is "one Undo takes it back". Its own register shows that promise failing silently and intermittently - 8 views stayed renamed after Revit reported the rollback ([NEEDS-CHECKING A16b](../../needs-checking/group-a.md), open since 2026-09-13) - and pyRevit 7 found the same on workshared models. It reuses the change set Heron already has, it is small, and it turns a silent failure into one the modeller sees. (The idea is pyRevit's, GPL: idea only; tzur669-lab's commit check is MIT.)

What follows it, in order:

1. Make the Delete box count what Revit really deletes: run the delete, collect every id that went, roll back - and confirm the roll back with the check above. *(Tracked as GitHub issue [#1005](https://github.com/Ajmalpshaik/Heron-AI/issues/1005) since 2026-11-16 (WN-9).)*
2. Point the same read-back at Keep: `verify: passed / failed / skipped` beside `outcome:` on every change.
3. Tie each approval (Delete box, apply-move, a DRAFT's one run, Admin and Publish) to the exact request and the model's state: one use, a few minutes. *(Tracked as GitHub issue [#1005](https://github.com/Ajmalpshaik/Heron-AI/issues/1005) since 2026-11-16 (WN-10).)*
4. Owner decision, recorded as a decision: where `write.enabled`, Admin and Publish may live, so that an AI with file access cannot switch them on.
5. Add the add-in build's hash and the exact Revit build to every proof record.

---

## Sources

Every GitHub repository the research notes cite, then every web page they cite. Addresses seen inside code (local ports, AI service endpoints) are not sources and are left out. Heron's own files are named by path in the sections above.

### GitHub repositories studied file by file (notes a to j, 84)

- [5vbkgsghhh-hash/kir](https://github.com/5vbkgsghhh-hash/kir) · [Al-Qublawi/AB.RevitMcp](https://github.com/Al-Qublawi/AB.RevitMcp) · [andyluu98/vn-revit-mcp](https://github.com/andyluu98/vn-revit-mcp) · [armanwu/revit-2027-mcp](https://github.com/armanwu/revit-2027-mcp)
- [autodesk-platform-services/aps-sample-mcp-server-revit-automation](https://github.com/autodesk-platform-services/aps-sample-mcp-server-revit-automation) · [autodesk-platform-services/aps-sample-revit-mcp-tools-bundle](https://github.com/autodesk-platform-services/aps-sample-revit-mcp-tools-bundle) · [AUTOM8LABS/mcp-connector-skills](https://github.com/AUTOM8LABS/mcp-connector-skills) · [BIM-MODE/BIM-MODE-Connector](https://github.com/BIM-MODE/BIM-MODE-Connector)
- [bimwright/rvt-mcp](https://github.com/bimwright/rvt-mcp) · [BURAKAFRIDI92/BURAK-BIM](https://github.com/BURAKAFRIDI92/BURAK-BIM) · [Cazorlas/RevitDocs.Mcp](https://github.com/Cazorlas/RevitDocs.Mcp) · [chuongmep/mcp-revit-sample-au2025](https://github.com/chuongmep/mcp-revit-sample-au2025)
- [constructelligence-lab/revit-mcp](https://github.com/constructelligence-lab/revit-mcp) · [Daii1414/revit-verify](https://github.com/Daii1414/revit-verify) · [danlion333/revit_mcp](https://github.com/danlion333/revit_mcp) · [Dave5264/transom-revit](https://github.com/Dave5264/transom-revit)
- [Demolinator/revit-mcp-plugin](https://github.com/Demolinator/revit-mcp-plugin) · [Demolinator/revit-mcp-server](https://github.com/Demolinator/revit-mcp-server) · [DharshanParasu/RevitMCPBridge2026](https://github.com/DharshanParasu/RevitMCPBridge2026) · [DTDucas/chm-converter](https://github.com/DTDucas/chm-converter)
- [DTDucas/RevitMCPSDK](https://github.com/DTDucas/RevitMCPSDK) · [elkhouryrafik-boop/revit-mcp-hardened](https://github.com/elkhouryrafik-boop/revit-mcp-hardened) · [expert-terminal/bim-agent](https://github.com/expert-terminal/bim-agent) · [fmg75/CLI_Revit](https://github.com/fmg75/CLI_Revit)
- [General-Soju/BimOnMcp](https://github.com/General-Soju/BimOnMcp) · [goswinr/Caddis](https://github.com/goswinr/Caddis) · [GraphBIM/RevitMCPGraphQL](https://github.com/GraphBIM/RevitMCPGraphQL) · [H8DIDI/tianheluo-core](https://github.com/H8DIDI/tianheluo-core)
- [hamzaabduljabbar/autoconst-claude-bim-health-scorecard](https://github.com/hamzaabduljabbar/autoconst-claude-bim-health-scorecard) · [hamzaabduljabbar/autoConst-cutsheet-to-family](https://github.com/hamzaabduljabbar/autoConst-cutsheet-to-family) · [hamzaabduljabbar/autoConst-p6-revit-4d](https://github.com/hamzaabduljabbar/autoConst-p6-revit-4d) · [hamzaabduljabbar/autoconst-spec-compliance-claude](https://github.com/hamzaabduljabbar/autoconst-spec-compliance-claude)
- [Hazemalisakr/pyRevit-MCP-Server-Bridge](https://github.com/Hazemalisakr/pyRevit-MCP-Server-Bridge) · [hmertkaramann/aec-pilot-mcp](https://github.com/hmertkaramann/aec-pilot-mcp) · [HorizunGroup/horizun-revit-mcp](https://github.com/HorizunGroup/horizun-revit-mcp) · [IbrahimFahdah/revit-claude-mcp](https://github.com/IbrahimFahdah/revit-claude-mcp)
- [Innoprovn/vn-revit-mcp](https://github.com/Innoprovn/vn-revit-mcp) · [kaitpw/Rvt_Docs_MCP](https://github.com/kaitpw/Rvt_Docs_MCP) · [KenLP/autoaudit-bim](https://github.com/KenLP/autoaudit-bim) · [KenLP/RevitMCPServer](https://github.com/KenLP/RevitMCPServer)
- [kilkellym/archsmarter-ai-experiments](https://github.com/kilkellym/archsmarter-ai-experiments) · [kilkellym/ArchSmarterRevitMcpServer](https://github.com/kilkellym/ArchSmarterRevitMcpServer) · [loadingcloud001/LoadingAI-Revit](https://github.com/loadingcloud001/LoadingAI-Revit) · [LuDattilo/revit-mcp-server](https://github.com/LuDattilo/revit-mcp-server)
- [LuDattilo/RevitCortex](https://github.com/LuDattilo/RevitCortex) · [luoyuke/RevitAgenticAICompanion](https://github.com/luoyuke/RevitAgenticAICompanion) · [MakeItEzzz555/Gridline](https://github.com/MakeItEzzz555/Gridline) · [mcp-servers-for-revit/mcp-server-for-revit-dotnet](https://github.com/mcp-servers-for-revit/mcp-server-for-revit-dotnet)
- [mcp-servers-for-revit/mcp-server-for-revit-python](https://github.com/mcp-servers-for-revit/mcp-server-for-revit-python) · [mcp-servers-for-revit/mcp-servers-for-revit](https://github.com/mcp-servers-for-revit/mcp-servers-for-revit) · [mcp-servers-for-revit/revit-mcp](https://github.com/mcp-servers-for-revit/revit-mcp) · [mcp-servers-for-revit/revit-mcp-commandset](https://github.com/mcp-servers-for-revit/revit-mcp-commandset)
- [mcp-servers-for-revit/revit-mcp-plugin](https://github.com/mcp-servers-for-revit/revit-mcp-plugin) · [mcp-servers-for-revit/revit-mcp-plugin-debug](https://github.com/mcp-servers-for-revit/revit-mcp-plugin-debug) · [mcp-servers-for-revit/revit-mcp-templates](https://github.com/mcp-servers-for-revit/revit-mcp-templates) · [NicheSam/revit-mcp-research-partner](https://github.com/NicheSam/revit-mcp-research-partner)
- [nikolaikrasilnikov/Revit-Bridge](https://github.com/nikolaikrasilnikov/Revit-Bridge) · [oakplank/RevitMCP](https://github.com/oakplank/RevitMCP) · [okuno-dsi/revit-mcp-toolkit](https://github.com/okuno-dsi/revit-mcp-toolkit) · [permitzip/mutinybim](https://github.com/permitzip/mutinybim)
- [PiggyAndrew/revit_mcp_dotnet_template](https://github.com/PiggyAndrew/revit_mcp_dotnet_template) · [ProfRino/Nonica-Revit-MCP-Skill](https://github.com/ProfRino/Nonica-Revit-MCP-Skill) · [pyrevitlabs/pyRevit](https://github.com/pyrevitlabs/pyRevit) · [QuanZ827/zexus](https://github.com/QuanZ827/zexus)
- [Rckov/RevitDex](https://github.com/Rckov/RevitDex) · [revitbridge/revit-bridge](https://github.com/revitbridge/revit-bridge) · [revitbridge/revit-bridge-addin](https://github.com/revitbridge/revit-bridge-addin) · [roubaudal-maker/ClaudeRevit](https://github.com/roubaudal-maker/ClaudeRevit)
- [rui-branco/revit-mcp](https://github.com/rui-branco/revit-mcp) · [Sam-AEC/aec-model-bridge](https://github.com/Sam-AEC/aec-model-bridge) · [schauh11/revit-mcp-server](https://github.com/schauh11/revit-mcp-server) · [scwlkr/RevitThyme](https://github.com/scwlkr/RevitThyme)
- [sharafutdinovdi/revit-model-mcp](https://github.com/sharafutdinovdi/revit-model-mcp) · [shtirlitsDva/cad-mcp](https://github.com/shtirlitsDva/cad-mcp) · [shuotao/REVIT_MCP_study](https://github.com/shuotao/REVIT_MCP_study) · [Sideswipe3751/bim-bridge](https://github.com/Sideswipe3751/bim-bridge)
- [Thestreetarckitect/revit-2027-mcp](https://github.com/Thestreetarckitect/revit-2027-mcp) · [trgiangv/rvtdocs-mcp](https://github.com/trgiangv/rvtdocs-mcp) · [tzur669-lab/revit-claude-agent](https://github.com/tzur669-lab/revit-claude-agent) · [UV-Tech/revit-claude-mcp](https://github.com/UV-Tech/revit-claude-mcp)
- [ZedMoster/revit-mcp](https://github.com/ZedMoster/revit-mcp) · [ZedMoster/xml.Revit.MCPServer](https://github.com/ZedMoster/xml.Revit.MCPServer) · [Ziad-Amr1/bim-ai-agent](https://github.com/Ziad-Amr1/bim-ai-agent) · [zsenarchitect/archi-magic](https://github.com/zsenarchitect/archi-magic)

### GitHub repositories in the long-tail sweep (note k, 407)

- [21nkant/bob-the-builder-showcase](https://github.com/21nkant/bob-the-builder-showcase) · [2ez4gcx/sketchup-mcp](https://github.com/2ez4gcx/sketchup-mcp) · [3d-mcp/revit-mcp](https://github.com/3d-mcp/revit-mcp) · [5788324/Yang-Agent_Revit](https://github.com/5788324/Yang-Agent_Revit)
- [6Nail/revit-mcp-setup](https://github.com/6Nail/revit-mcp-setup) · [7alexhuang-ux/RevitMCP-Addin](https://github.com/7alexhuang-ux/RevitMCP-Addin) · [7alexhuang-ux/Slide-REVIT_MCP_Study](https://github.com/7alexhuang-ux/Slide-REVIT_MCP_Study) · [Aaradhya-Dev-Tamrakar/revit-mcp](https://github.com/Aaradhya-Dev-Tamrakar/revit-mcp)
- [abdallahmostafataha6-blip/A-M-T](https://github.com/abdallahmostafataha6-blip/A-M-T) · [abdullah13793-commits/indeklima-twin](https://github.com/abdullah13793-commits/indeklima-twin) · [abrahamlinh-code/KGS-Revit-MCP](https://github.com/abrahamlinh-code/KGS-Revit-MCP) · [AchyErrorJ/RevitMCP](https://github.com/AchyErrorJ/RevitMCP)
- [adelinoles/765t-development-Revit](https://github.com/adelinoles/765t-development-Revit) · [aec-platform/bimq](https://github.com/aec-platform/bimq) · [aecoder-br/revit-ai-powered-plugin-template](https://github.com/aecoder-br/revit-ai-powered-plugin-template) · [AhmedXAlDeeb/RevitLLMBackend-](https://github.com/AhmedXAlDeeb/RevitLLMBackend-)
- [aisbergen/RevitHistoryLogger](https://github.com/aisbergen/RevitHistoryLogger) · [ajayabnave-ctrl/Revit-AJ-MCP-Assistant](https://github.com/ajayabnave-ctrl/Revit-AJ-MCP-Assistant) · [AlejoDuarte23/VIKTOR-REVIT-MCP](https://github.com/AlejoDuarte23/VIKTOR-REVIT-MCP) · [AleksandrPidlozhevich/RevitMCP](https://github.com/AleksandrPidlozhevich/RevitMCP)
- [alesdev88/Archicad-MCP](https://github.com/alesdev88/Archicad-MCP) · [AlexandroLeone/revit-mcp](https://github.com/AlexandroLeone/revit-mcp) · [alexiVera/guia-mcp-revit-python](https://github.com/alexiVera/guia-mcp-revit-python) · [alexiVera/guia-pyrevit-mcp](https://github.com/alexiVera/guia-pyrevit-mcp)
- [AlexSk-sys/revit-mcp-bridge-public](https://github.com/AlexSk-sys/revit-mcp-bridge-public) · [alrigithub/revit-mcp-stack](https://github.com/alrigithub/revit-mcp-stack) · [AltControlIT/revit-mcp-sdk](https://github.com/AltControlIT/revit-mcp-sdk) · [AndreyStartsev/revit-mcp](https://github.com/AndreyStartsev/revit-mcp)
- [Andy-rba30/revit-mcp](https://github.com/Andy-rba30/revit-mcp) · [Andyhdr/Bimwired-skill](https://github.com/Andyhdr/Bimwired-skill) · [andyluu98/vn-mcp-blender](https://github.com/andyluu98/vn-mcp-blender) · [angellofinetti/IA-BIM-Electrico](https://github.com/angellofinetti/IA-BIM-Electrico)
- [annquin/revit-bim-workspace](https://github.com/annquin/revit-bim-workspace) · [anntfd-debug/RevitMCP](https://github.com/anntfd-debug/RevitMCP) · [antonhofstader/Revit-2026-MCP-Server](https://github.com/antonhofstader/Revit-2026-MCP-Server) · [antonrathor24-glitch/revit-mcp-ai](https://github.com/antonrathor24-glitch/revit-mcp-ai)
- [ara3d/bim-open-toolkit](https://github.com/ara3d/bim-open-toolkit) · [ArchitectureWorld/revit-mcp-automation](https://github.com/ArchitectureWorld/revit-mcp-automation) · [archsalem101/Cutom_Revit_MCP_server](https://github.com/archsalem101/Cutom_Revit_MCP_server) · [Ariantabib/Arian-Revit-MCP-Plugin](https://github.com/Ariantabib/Arian-Revit-MCP-Plugin)
- [Arielpapu420/Creador-de-video-para-redes-revit-estructural](https://github.com/Arielpapu420/Creador-de-video-para-redes-revit-estructural) · [ascriba/freecad-bim-agent](https://github.com/ascriba/freecad-bim-agent) · [astroarkhe/ASSILI-MCP-SERVER](https://github.com/astroarkhe/ASSILI-MCP-SERVER) · [AtanasGe/DynamoMCP](https://github.com/AtanasGe/DynamoMCP)
- [audunopdal/RevitMCP](https://github.com/audunopdal/RevitMCP) · [aussieBIMguru/BimGo](https://github.com/aussieBIMguru/BimGo) · [avbsab-wq/revit-mcp-server](https://github.com/avbsab-wq/revit-mcp-server) · [aveusalex/mcp-servers-for-revit](https://github.com/aveusalex/mcp-servers-for-revit)
- [Axotopia/revitmcp](https://github.com/Axotopia/revitmcp) · [aydimir95/revit-mcp-plugin](https://github.com/aydimir95/revit-mcp-plugin) · [aydimir95/RevitMCPInstaller.exe](https://github.com/aydimir95/RevitMCPInstaller.exe) · [ayfadili2022-glitch/revit_claude](https://github.com/ayfadili2022-glitch/revit_claude)
- [AZZRE/revit-ai-verification](https://github.com/AZZRE/revit-ai-verification) · [B45Labs/B45Labs_Connect](https://github.com/B45Labs/B45Labs_Connect) · [Barkie721/claude_sld_generator_system](https://github.com/Barkie721/claude_sld_generator_system) · [bcllcc/modulor](https://github.com/bcllcc/modulor)
- [bctw123/revit-mcp](https://github.com/bctw123/revit-mcp) · [bhanuabbichetty/RevitAIConnector](https://github.com/bhanuabbichetty/RevitAIConnector) · [BhaveshY/revit-mcp-codex-plugin](https://github.com/BhaveshY/revit-mcp-codex-plugin) · [BhaveshY/revit-mcp-next](https://github.com/BhaveshY/revit-mcp-next)
- [bic98/paulga-mcp](https://github.com/bic98/paulga-mcp) · [BIM-Dad/IRIS-codex-revit-mcp](https://github.com/BIM-Dad/IRIS-codex-revit-mcp) · [BIM-MODE/BIM-MODE-AutoCAD-MCP](https://github.com/BIM-MODE/BIM-MODE-AutoCAD-MCP) · [BIM4GIB/MCP.Revit](https://github.com/BIM4GIB/MCP.Revit)
- [BIMBrain/McpRevitPlus](https://github.com/BIMBrain/McpRevitPlus) · [Bimetrik/bimetrik-mcp](https://github.com/Bimetrik/bimetrik-mcp) · [bimmonkey-ai/bimmonkey-claude-plugin](https://github.com/bimmonkey-ai/bimmonkey-claude-plugin) · [BIMO-Tools/Revit-Python-Scripts](https://github.com/BIMO-Tools/Revit-Python-Scripts)
- [Booster1985/scanbim-mcp](https://github.com/Booster1985/scanbim-mcp) · [Boti-Ormandi/archicad-mcp](https://github.com/Boti-Ormandi/archicad-mcp) · [Breakstrings/31052026](https://github.com/Breakstrings/31052026) · [byggstyrning/revit-pset-validator](https://github.com/byggstyrning/revit-pset-validator)
- [carlofr/MODELO-IA](https://github.com/carlofr/MODELO-IA) · [catsony5-web/revit-mcp](https://github.com/catsony5-web/revit-mcp) · [changgeng1009/horizun-revit-mcp](https://github.com/changgeng1009/horizun-revit-mcp) · [Chayan-Paul/PaulAI-Revit-Copilot](https://github.com/Chayan-Paul/PaulAI-Revit-Copilot)
- [chen-dongchai/RevitMcp](https://github.com/chen-dongchai/RevitMcp) · [ChimingLu/AutodeskDynamo_MCP](https://github.com/ChimingLu/AutodeskDynamo_MCP) · [CHMOSE023/RevitMCP](https://github.com/CHMOSE023/RevitMCP) · [chronista-club/archicad-mcp](https://github.com/chronista-club/archicad-mcp)
- [cleeefyy/acc-revit](https://github.com/cleeefyy/acc-revit) · [cleeefyy/doc_m_compliance_checker_Revit](https://github.com/cleeefyy/doc_m_compliance_checker_Revit) · [congtytnhhtvxdanhnguyen-hash/mcp-servers-for-revit-2027](https://github.com/congtytnhhtvxdanhnguyen-hash/mcp-servers-for-revit-2027) · [cosentinor/mcp-revit-server](https://github.com/cosentinor/mcp-revit-server)
- [creativeengineer224-a11y/Revit-AI-Assistant](https://github.com/creativeengineer224-a11y/Revit-AI-Assistant) · [cxcc5876-maker/mcp-REVIT](https://github.com/cxcc5876-maker/mcp-REVIT) · [CyberPotato0416/MCP-Tools-extension](https://github.com/CyberPotato0416/MCP-Tools-extension) · [d14znsol/bimvn-revit-mcp](https://github.com/d14znsol/bimvn-revit-mcp)
- [Daniel26-lgtm/MCP-revit](https://github.com/Daniel26-lgtm/MCP-revit) · [DAS-WNorth/RevitMCP](https://github.com/DAS-WNorth/RevitMCP) · [dashy2004/CDCRD-Digital](https://github.com/dashy2004/CDCRD-Digital) · [datadrivenconstruction/CAD-BIM-to-Code-Automation-Pipeline-DDC-Workflow-with-LLM-ChatGPT](https://github.com/datadrivenconstruction/CAD-BIM-to-Code-Automation-Pipeline-DDC-Workflow-with-LLM-ChatGPT)
- [datadrivenconstruction/ChatGPT-with-Revit-IFC](https://github.com/datadrivenconstruction/ChatGPT-with-Revit-IFC) · [DavidChin0/revit-mcp-stdio](https://github.com/DavidChin0/revit-mcp-stdio) · [Davidvolkmar/MCP-REVIT-VOLK](https://github.com/Davidvolkmar/MCP-REVIT-VOLK) · [Daviidro/awesome-aec-mcp](https://github.com/Daviidro/awesome-aec-mcp)
- [Daviidro/speckle-mcp](https://github.com/Daviidro/speckle-mcp) · [Davud77/bp-revit-tools](https://github.com/Davud77/bp-revit-tools) · [Davud77/WEAM.Revit.AI](https://github.com/Davud77/WEAM.Revit.AI) · [desarrollospaloblanco/mcp-revit](https://github.com/desarrollospaloblanco/mcp-revit)
- [desarrolloteambimcivil/tbc_mcp_revit](https://github.com/desarrolloteambimcivil/tbc_mcp_revit) · [desarrolloteambimcivil/tbc_mcp_setup](https://github.com/desarrolloteambimcivil/tbc_mcp_setup) · [Desoxi1/ARQ-IA](https://github.com/Desoxi1/ARQ-IA) · [dolanklock/Revit-MCP-Claude](https://github.com/dolanklock/Revit-MCP-Claude)
- [domewaradet/MCP-Revit](https://github.com/domewaradet/MCP-Revit) · [DonaldLu/Sinotech_MCP](https://github.com/DonaldLu/Sinotech_MCP) · [douglascg7-art/mcp-server-for-revit-python](https://github.com/douglascg7-art/mcp-server-for-revit-python) · [Dreamsigner/Dreamsign_Revit_MCP](https://github.com/Dreamsigner/Dreamsign_Revit_MCP)
- [edmaceymacleod/ca_elevation_review](https://github.com/edmaceymacleod/ca_elevation_review) · [edsonlimaarq-sudo/Edson](https://github.com/edsonlimaarq-sudo/Edson) · [eichler-ai/connectors](https://github.com/eichler-ai/connectors) · [EliLeung/Revit-MCP](https://github.com/EliLeung/Revit-MCP)
- [ELMOSK/revit_mcp](https://github.com/ELMOSK/revit_mcp) · [Elprofessor1990/revit-mcp-server-nbs-nordic](https://github.com/Elprofessor1990/revit-mcp-server-nbs-nordic) · [Emmagrace0130/Architecture-LLM](https://github.com/Emmagrace0130/Architecture-LLM) · [emnss91/claude_mcp_revit](https://github.com/emnss91/claude_mcp_revit)
- [enternalsaga/revit-agentic-flow](https://github.com/enternalsaga/revit-agentic-flow) · [erivas-droid/revit-bimwright-herramientas](https://github.com/erivas-droid/revit-bimwright-herramientas) · [esanzgadea/brief-to-bim](https://github.com/esanzgadea/brief-to-bim) · [eska5/Claude_higgsfield_revit_wizualizacje](https://github.com/eska5/Claude_higgsfield_revit_wizualizacje)
- [EverseDevelopment/ZoidbergAI.Addin](https://github.com/EverseDevelopment/ZoidbergAI.Addin) · [Evgivachev/erp-public](https://github.com/Evgivachev/erp-public) · [ewuiyud/revit-mcp-dify](https://github.com/ewuiyud/revit-mcp-dify) · [Fananiel/my-revit-mcp](https://github.com/Fananiel/my-revit-mcp)
- [Fanchiang0418/20261002_Revit-MCP-Learning](https://github.com/Fanchiang0418/20261002_Revit-MCP-Learning) · [fankai0102/mcp-revit-blender](https://github.com/fankai0102/mcp-revit-blender) · [felipemotelewicz-prog/autorevit-us](https://github.com/felipemotelewicz-prog/autorevit-us) · [felipemotelewicz-prog/instalador-revit-com-ia](https://github.com/felipemotelewicz-prog/instalador-revit-com-ia)
- [formcraft/formcraft-revit-tools](https://github.com/formcraft/formcraft-revit-tools) · [fulviofb/revit-claude-mcp](https://github.com/fulviofb/revit-claude-mcp) · [gabrieloliveira-crypto/zeit-revit-mcp-server](https://github.com/gabrieloliveira-crypto/zeit-revit-mcp-server) · [Ganesh-Chavhan/RevitMCP_PLugin](https://github.com/Ganesh-Chavhan/RevitMCP_PLugin)
- [GGomesBRA/revit-mcp](https://github.com/GGomesBRA/revit-mcp) · [Giovanni1708/MCP-Revit-V2.0](https://github.com/Giovanni1708/MCP-Revit-V2.0) · [h4child/mcp-revit](https://github.com/h4child/mcp-revit) · [haivo088/261011_bimtech-revit-mcp](https://github.com/haivo088/261011_bimtech-revit-mcp)
- [hajun011103/ga-layout-optimizer](https://github.com/hajun011103/ga-layout-optimizer) · [HamezMM/Claude_Code_Revit_Test](https://github.com/HamezMM/Claude_Code_Revit_Test) · [hazzem0/HZ-BIM](https://github.com/hazzem0/HZ-BIM) · [HelloHowAreYouHaveANiceDay/bim-cli-plugin](https://github.com/HelloHowAreYouHaveANiceDay/bim-cli-plugin)
- [hienphan0111/BIM-Bot](https://github.com/hienphan0111/BIM-Bot) · [hilldevelopment/hill-revit-mcp](https://github.com/hilldevelopment/hill-revit-mcp) · [HoareLea/CJ_Revit-mcp-main](https://github.com/HoareLea/CJ_Revit-mcp-main) · [HorizunGroup/horizun-msproject-mcp](https://github.com/HorizunGroup/horizun-msproject-mcp)
- [HorizunGroup/naviscoord-mcp](https://github.com/HorizunGroup/naviscoord-mcp) · [huangruipao/RevitMCP](https://github.com/huangruipao/RevitMCP) · [hungpixi/revit-mcp](https://github.com/hungpixi/revit-mcp) · [HuuPhucGL/HP-skill-revit-VN](https://github.com/HuuPhucGL/HP-skill-revit-VN)
- [huy17012001huy-cyber/dscons-revit-mcp](https://github.com/huy17012001huy-cyber/dscons-revit-mcp) · [huynvk19-cell/claude-using-revit](https://github.com/huynvk19-cell/claude-using-revit) · [imkcrevit/RevitMCP_Blog](https://github.com/imkcrevit/RevitMCP_Blog) · [Impertio-Studio/Speckle-Claude-Skill-Package](https://github.com/Impertio-Studio/Speckle-Claude-Skill-Package)
- [inno-arq/claude-revit-bridge](https://github.com/inno-arq/claude-revit-bridge) · [iofbim/IofBIM_revitMCP](https://github.com/iofbim/IofBIM_revitMCP) · [ioseluiz/mcp_server_copilot_revit](https://github.com/ioseluiz/mcp_server_copilot_revit) · [ioseluiz/my_revit_mcp_server](https://github.com/ioseluiz/my_revit_mcp_server)
- [ioseluiz/revit_mcp_copilot_studio](https://github.com/ioseluiz/revit_mcp_copilot_studio) · [isaddiq/Claude-BIM-development-skill](https://github.com/isaddiq/Claude-BIM-development-skill) · [Iteran96/claude-revit-integration](https://github.com/Iteran96/claude-revit-integration) · [jabluetooth/AEC-Agent](https://github.com/jabluetooth/AEC-Agent)
- [JacksonPinto/RevitMCP_Install](https://github.com/JacksonPinto/RevitMCP_Install) · [jadjhaddad/aec-scaffold-mcp](https://github.com/jadjhaddad/aec-scaffold-mcp) · [JaggerYu-ArchAI/archai-revitmcp-dynamo](https://github.com/JaggerYu-ArchAI/archai-revitmcp-dynamo) · [JardiMargalefAgusti/bSDD-mcp](https://github.com/JardiMargalefAgusti/bSDD-mcp)
- [jaredholloway94/Revit-MCP](https://github.com/jaredholloway94/Revit-MCP) · [Jarvi-Hou/OpenRevit](https://github.com/Jarvi-Hou/OpenRevit) · [jasonwarta/pyrevit-mcp](https://github.com/jasonwarta/pyrevit-mcp) · [JCast26/RevitMcp](https://github.com/JCast26/RevitMcp)
- [jdwit/revit-open-pointcloud](https://github.com/jdwit/revit-open-pointcloud) · [JefaturaBIM94/RevitMCPBOQ](https://github.com/JefaturaBIM94/RevitMCPBOQ) · [jeffersonbim/skill-information-manager-ifc](https://github.com/jeffersonbim/skill-information-manager-ifc) · [jesselei1102-hue/Revit-MCP-Server-Apps](https://github.com/jesselei1102-hue/Revit-MCP-Server-Apps)
- [jiangkai002/revit-agent-cli](https://github.com/jiangkai002/revit-agent-cli) · [jmcaamanog/CE3X-MCP-Server](https://github.com/jmcaamanog/CE3X-MCP-Server) · [jmcouffin/llms-revit-api](https://github.com/jmcouffin/llms-revit-api) · [jmcouffin/revit_claude_mcp](https://github.com/jmcouffin/revit_claude_mcp)
- [JobiyoAI/RevitAIChat](https://github.com/JobiyoAI/RevitAIChat) · [JoniNykanen/ai-for-structural-engineers](https://github.com/JoniNykanen/ai-for-structural-engineers) · [josedelgadoq/my-revit-mcp](https://github.com/josedelgadoq/my-revit-mcp) · [josedelgadoq/revit-mcp](https://github.com/josedelgadoq/revit-mcp)
- [justkmode/docctl-mcp](https://github.com/justkmode/docctl-mcp) · [Kaankrby/Parametrically-Compass](https://github.com/Kaankrby/Parametrically-Compass) · [Kaiiser-ai/Revit-MCP-integration](https://github.com/Kaiiser-ai/Revit-MCP-integration) · [kanatip-k/foundation-ontology-mcp](https://github.com/kanatip-k/foundation-ontology-mcp)
- [kazmi-y/revit-mcp](https://github.com/kazmi-y/revit-mcp) · [KazuhiroTakahashiAQ/revit-plugin-framework](https://github.com/KazuhiroTakahashiAQ/revit-plugin-framework) · [KazuhiroTakahashiAQ/RevitMcpPlugin](https://github.com/KazuhiroTakahashiAQ/RevitMcpPlugin) · [kdg200121-hash/revit-mcp-bridge-updates](https://github.com/kdg200121-hash/revit-mcp-bridge-updates)
- [KenjiIT/autocad-knowledge](https://github.com/KenjiIT/autocad-knowledge) · [KenLP/acc-forma-mcp-server](https://github.com/KenLP/acc-forma-mcp-server) · [KenLP/bim-ai-architect](https://github.com/KenLP/bim-ai-architect) · [KevinKTM/revit-mcp](https://github.com/KevinKTM/revit-mcp)
- [KevinKTM/revit-mcp-commandset](https://github.com/KevinKTM/revit-mcp-commandset) · [klim-bim/revit-basic-mcp](https://github.com/klim-bim/revit-basic-mcp) · [kmezacivil-byte/revit-mcp](https://github.com/kmezacivil-byte/revit-mcp) · [kolosimo/MCP-x-REVIT-2025](https://github.com/kolosimo/MCP-x-REVIT-2025)
- [KuklevDS/clawd-workspace-public](https://github.com/KuklevDS/clawd-workspace-public) · [kyawswar-22/gemini-revit-mcp](https://github.com/kyawswar-22/gemini-revit-mcp) · [labsfuture/revit-converter-remote-mcp-release](https://github.com/labsfuture/revit-converter-remote-mcp-release) · [lachiemyatt/spec-assistant](https://github.com/lachiemyatt/spec-assistant)
- [Lalitchitoshiya/Revit-MCP](https://github.com/Lalitchitoshiya/Revit-MCP) · [laytonluo/revit-dwg-to-dxf-skill](https://github.com/laytonluo/revit-dwg-to-dxf-skill) · [laytonluo/revit-mcp-create-plan-views](https://github.com/laytonluo/revit-mcp-create-plan-views) · [lduan-tech/Drafting](https://github.com/lduan-tech/Drafting)
- [LEEYUSEONG33/P6ix-Revit-MCP](https://github.com/LEEYUSEONG33/P6ix-Revit-MCP) · [libarquitectura-cmd/CLAUDE-REVIT](https://github.com/libarquitectura-cmd/CLAUDE-REVIT) · [LIUruchuan/revit-spatial-export](https://github.com/LIUruchuan/revit-spatial-export) · [Long0308/LPOpenBIM-RevitMCPSkill-backup](https://github.com/Long0308/LPOpenBIM-RevitMCPSkill-backup)
- [Longtran2404/chinh-thang-revit-mcp](https://github.com/Longtran2404/chinh-thang-revit-mcp) · [louishhp/louis-revit-ai-mcp](https://github.com/louishhp/louis-revit-ai-mcp) · [louistrue/ifcx-mcp](https://github.com/louistrue/ifcx-mcp) · [Lubimka-dev/Revit-MCP-Releases](https://github.com/Lubimka-dev/Revit-MCP-Releases)
- [luisorbaiceta/oa-revit-mcp](https://github.com/luisorbaiceta/oa-revit-mcp) · [maengenhariaarquitetura/conecta-revit](https://github.com/maengenhariaarquitetura/conecta-revit) · [Mahesh-Peddinti/mcp-servers-for-revit](https://github.com/Mahesh-Peddinti/mcp-servers-for-revit) · [MahmoudEldesokey/giraffe](https://github.com/MahmoudEldesokey/giraffe)
- [Makata-Studio/bim-mcp-docs](https://github.com/Makata-Studio/bim-mcp-docs) · [mamun-7/Revit-MCP](https://github.com/mamun-7/Revit-MCP) · [Manoz2201/DynamoRevit-MCP-Tools](https://github.com/Manoz2201/DynamoRevit-MCP-Tools) · [Marcin-Szul/mcp-integration-system](https://github.com/Marcin-Szul/mcp-integration-system)
- [masoodkaramoozian/ifc-agent-tools](https://github.com/masoodkaramoozian/ifc-agent-tools) · [MasterMurali/RevitMCPPlugin](https://github.com/MasterMurali/RevitMCPPlugin) · [MasterMurali/RevitMCPServer](https://github.com/MasterMurali/RevitMCPServer) · [mathieucostudio-ui/MCP-REVIT](https://github.com/mathieucostudio-ui/MCP-REVIT)
- [matthewemerykeck24601/CursorRevitMCP](https://github.com/matthewemerykeck24601/CursorRevitMCP) · [MAvinash24/autodesk-cad-mcp-agent](https://github.com/MAvinash24/autodesk-cad-mcp-agent) · [meet-rehman/revit-mcp-case-study](https://github.com/meet-rehman/revit-mcp-case-study) · [MeltFlexDevs/aibimtool-skills](https://github.com/MeltFlexDevs/aibimtool-skills)
- [MennoMekes/Python-scripts-for-Dynamo](https://github.com/MennoMekes/Python-scripts-for-Dynamo) · [MervenurGombeci/mcp-revit-github](https://github.com/MervenurGombeci/mcp-revit-github) · [Messonjia/revit-elec-mcp](https://github.com/Messonjia/revit-elec-mcp) · [mikhalchankasm/NavisWorksMaster](https://github.com/mikhalchankasm/NavisWorksMaster)
- [mjcobb/PyRevit-Hatching-Extension](https://github.com/mjcobb/PyRevit-Hatching-Extension) · [Mohamed-Elnahla/revit-mcp-github-copilot](https://github.com/Mohamed-Elnahla/revit-mcp-github-copilot) · [mostafamahereid1946-spec/architecture-mep-bim-skill](https://github.com/mostafamahereid1946-spec/architecture-mep-bim-skill) · [MShaharyar1/RevitMcp](https://github.com/MShaharyar1/RevitMcp)
- [Mvclr/revit-mcp-server](https://github.com/Mvclr/revit-mcp-server) · [mydktb/MCP-server-for-Autodesk-Revit](https://github.com/mydktb/MCP-server-for-Autodesk-Revit) · [mzahirnia30-cell/deadalus](https://github.com/mzahirnia30-cell/deadalus) · [narbeh82/Revit-join-elements-Claude-C-](https://github.com/narbeh82/Revit-join-elements-Claude-C-)
- [Neonarjun/pyrevit-ai-one-click-installer](https://github.com/Neonarjun/pyrevit-ai-one-click-installer) · [nguyenngocdue/DeepBIM-MCP-Revit-Plugin](https://github.com/nguyenngocdue/DeepBIM-MCP-Revit-Plugin) · [nguyenngocdue/revit-mcp-server](https://github.com/nguyenngocdue/revit-mcp-server) · [NilayDev/revit-mcp-claude](https://github.com/NilayDev/revit-mcp-claude)
- [ninhpham96/Revit-mcp-autotest](https://github.com/ninhpham96/Revit-mcp-autotest) · [northlarkhq/awesome-engineering-mcp](https://github.com/northlarkhq/awesome-engineering-mcp) · [NovaShang/bimdown-format-comparison](https://github.com/NovaShang/bimdown-format-comparison) · [oakplank/revit-mcp](https://github.com/oakplank/revit-mcp)
- [OliveiraCotta/Assistente_BIM_OCA](https://github.com/OliveiraCotta/Assistente_BIM_OCA) · [OnePowerUser88/pyRevitFoundryMCP](https://github.com/OnePowerUser88/pyRevitFoundryMCP) · [onkarcctech/Revit-Wall-Automation-MCP-Server](https://github.com/onkarcctech/Revit-Wall-Automation-MCP-Server) · [OOXGN-OOX/RevitMCP](https://github.com/OOXGN-OOX/RevitMCP)
- [OpenAEC-Foundation/OpenAEC-Workspace-Composer](https://github.com/OpenAEC-Foundation/OpenAEC-Workspace-Composer) · [OriAshkenazi/ai-pyrevit-developer-template](https://github.com/OriAshkenazi/ai-pyrevit-developer-template) · [osc-bouw/RevitMCP](https://github.com/osc-bouw/RevitMCP) · [pahanitsch/revitapidocs-mcp](https://github.com/pahanitsch/revitapidocs-mcp)
- [ParkerCai/cria-revit-mcp](https://github.com/ParkerCai/cria-revit-mcp) · [PATAGONIABIM/revit-mcp-revit-plugin](https://github.com/PATAGONIABIM/revit-mcp-revit-plugin) · [paulkotov/revit-mcp-server](https://github.com/paulkotov/revit-mcp-server) · [paulohenriquesn/revitapi-claude-context](https://github.com/paulohenriquesn/revitapi-claude-context)
- [Pazran/mcp-revit-api](https://github.com/Pazran/mcp-revit-api) · [pedrobonilla198710-eng/revit-mcp](https://github.com/pedrobonilla198710-eng/revit-mcp) · [PerSpec-Tech/Datum-Compliance-Checker](https://github.com/PerSpec-Tech/Datum-Compliance-Checker) · [PetarStoev02/archicad-mcp](https://github.com/PetarStoev02/archicad-mcp)
- [phamduybill2005-creator/CH-M-I-M-AGENT](https://github.com/phamduybill2005-creator/CH-M-I-M-AGENT) · [phantom-logic/Automation-Pipeline-DDC-Workflow-with-LLM-ChatGPT](https://github.com/phantom-logic/Automation-Pipeline-DDC-Workflow-with-LLM-ChatGPT) · [Plarmindo/revit-mcp-plugin](https://github.com/Plarmindo/revit-mcp-plugin) · [Plarmindo/revit-mcp-python](https://github.com/Plarmindo/revit-mcp-python)
- [polaris97s/Aeclyra-revit2018.3-MCP](https://github.com/polaris97s/Aeclyra-revit2018.3-MCP) · [pradeepmisal/AutoDesk-Revit-ClashGuard-MCP](https://github.com/pradeepmisal/AutoDesk-Revit-ClashGuard-MCP) · [PranayS27/MCP-Revit-Project](https://github.com/PranayS27/MCP-Revit-Project) · [predator230814/RevitMCP](https://github.com/predator230814/RevitMCP)
- [pyaephyoekyawjp/revit-mcp-bridge](https://github.com/pyaephyoekyawjp/revit-mcp-bridge) · [Quincy987/VibeModel](https://github.com/Quincy987/VibeModel) · [RAEN-DT/PyNetVSCode](https://github.com/RAEN-DT/PyNetVSCode) · [rafaelcruzmartins/mcp-revit-2026](https://github.com/rafaelcruzmartins/mcp-revit-2026)
- [RAJ-IITROORKEE/revit-datumm-plugin](https://github.com/RAJ-IITROORKEE/revit-datumm-plugin) · [RAJ-IITROORKEE/revit-mcp-commandset-datumm](https://github.com/RAJ-IITROORKEE/revit-mcp-commandset-datumm) · [RAJ-IITROORKEE/Revit-mcp-datumm](https://github.com/RAJ-IITROORKEE/Revit-mcp-datumm) · [rameztawfik/Revit-MCP](https://github.com/rameztawfik/Revit-MCP)
- [RatebOmari/RevitMCP](https://github.com/RatebOmari/RevitMCP) · [raventhetan/warehouse-generator-mcp](https://github.com/raventhetan/warehouse-generator-mcp) · [RavinduWeerakoon/revit-mcp-client](https://github.com/RavinduWeerakoon/revit-mcp-client) · [razbakov/bim-portal-mcp](https://github.com/razbakov/bim-portal-mcp)
- [realitymatrix/openstrate-outlet-height](https://github.com/realitymatrix/openstrate-outlet-height) · [realitymatrix/openstrate-revit-agent](https://github.com/realitymatrix/openstrate-revit-agent) · [realitymatrix/openstrate-revit-bridge](https://github.com/realitymatrix/openstrate-revit-bridge) · [reneruano95/revit-mcp-mep](https://github.com/reneruano95/revit-mcp-mep)
- [revitbridge/.github](https://github.com/revitbridge/.github) · [revitbridge/revit-api-docs](https://github.com/revitbridge/revit-api-docs) · [revitbridge/revit-bridge-web](https://github.com/revitbridge/revit-bridge-web) · [reylorente1/revit-mcp-2026-Rey](https://github.com/reylorente1/revit-mcp-2026-Rey)
- [Rirychh/revit-mcp](https://github.com/Rirychh/revit-mcp) · [RiseNJ-YF/RevitPointCloudMCP](https://github.com/RiseNJ-YF/RevitPointCloudMCP) · [romanzarkhin/revit-mcp](https://github.com/romanzarkhin/revit-mcp) · [RonySalluca/MCP-Revit-Installer](https://github.com/RonySalluca/MCP-Revit-Installer)
- [roynertr/cobie-mcp](https://github.com/roynertr/cobie-mcp) · [rrdls/revit-mcp](https://github.com/rrdls/revit-mcp) · [rtineoSTV/revit-mcp](https://github.com/rtineoSTV/revit-mcp) · [ru5hikesh/Revit-MCP](https://github.com/ru5hikesh/Revit-MCP)
- [rubenmendoza1290/RevitMCP](https://github.com/rubenmendoza1290/RevitMCP) · [rubimtech/revit_knowledge_mcp](https://github.com/rubimtech/revit_knowledge_mcp) · [rugstan/revit-claude-for-yc](https://github.com/rugstan/revit-claude-for-yc) · [rusbaltabek-commits/mcp-server-for-revit-python](https://github.com/rusbaltabek-commits/mcp-server-for-revit-python)
- [rvbuilds/revit-api-tools](https://github.com/rvbuilds/revit-api-tools) · [ryanchan720/copilot-for-revit](https://github.com/ryanchan720/copilot-for-revit) · [ryanolson/dynamo-mcp](https://github.com/ryanolson/dynamo-mcp) · [s81/Autodesk--mcp](https://github.com/s81/Autodesk--mcp)
- [SahinIkbal/RevitMCPTest](https://github.com/SahinIkbal/RevitMCPTest) · [samsonleegh/revit-coach](https://github.com/samsonleegh/revit-coach) · [satviksinha-cctech/revit-property-extractor-mcp](https://github.com/satviksinha-cctech/revit-property-extractor-mcp) · [ScanBIM-Labs/revit-mcp](https://github.com/ScanBIM-Labs/revit-mcp)
- [scotmos/revit-mcp-copilot-integration](https://github.com/scotmos/revit-mcp-copilot-integration) · [sergiomauz/mcp-server-dotnet-bimengine](https://github.com/sergiomauz/mcp-server-dotnet-bimengine) · [serjk12/ifc-bim-knowledge-base](https://github.com/serjk12/ifc-bim-knowledge-base) · [shenghungLin/https-github.com-shuotao-REVIT_MCP_study](https://github.com/shenghungLin/https-github.com-shuotao-REVIT_MCP_study)
- [SherinWorin/sherin-tekla-mcp](https://github.com/SherinWorin/sherin-tekla-mcp) · [shervindavarifard/revit-mcp-bridge](https://github.com/shervindavarifard/revit-mcp-bridge) · [SHMH-MCPs/navis-mcp-releases](https://github.com/SHMH-MCPs/navis-mcp-releases) · [SHMH-MCPs/revit-mcp-releases](https://github.com/SHMH-MCPs/revit-mcp-releases)
- [Show2Instruct/bonsai-mcp](https://github.com/Show2Instruct/bonsai-mcp) · [Show2Instruct/ifc-bonsai-mcp](https://github.com/Show2Instruct/ifc-bonsai-mcp) · [Shubham-L131225/REVITL_MCP](https://github.com/Shubham-L131225/REVITL_MCP) · [simerionon/Text2Floorplan](https://github.com/simerionon/Text2Floorplan)
- [Simone-Balin/revit-mcp-claude-code-setup](https://github.com/Simone-Balin/revit-mcp-claude-code-setup) · [SimplyCoded/revit-mcp-bridge](https://github.com/SimplyCoded/revit-mcp-bridge) · [sindhupasham277/BIM_AGENT](https://github.com/sindhupasham277/BIM_AGENT) · [SinoDonald/Sinotech_MCP](https://github.com/SinoDonald/Sinotech_MCP)
- [SinoDonald/TYBIM_MCP](https://github.com/SinoDonald/TYBIM_MCP) · [sky92archangel/revit-mcp-local-bridge](https://github.com/sky92archangel/revit-mcp-local-bridge) · [Slimouzi/audit-bim-mcp](https://github.com/Slimouzi/audit-bim-mcp) · [soewal19/eracore-revit-mcp](https://github.com/soewal19/eracore-revit-mcp)
- [Solken-bim-mx/solken-mcp-revit](https://github.com/Solken-bim-mx/solken-mcp-revit) · [SomeshProgARC/pox-revit-mcp](https://github.com/SomeshProgARC/pox-revit-mcp) · [sonhp91/Revit-MCP](https://github.com/sonhp91/Revit-MCP) · [strahser/e-RevitMcp-](https://github.com/strahser/e-RevitMcp-)
- [Strategie-Digitali/villa-savoye-bimcraft-vs-revit](https://github.com/Strategie-Digitali/villa-savoye-bimcraft-vs-revit) · [studioliquid-lab/revit-mcp-tool-generator](https://github.com/studioliquid-lab/revit-mcp-tool-generator) · [Stv-devl/PlanPilotMCP](https://github.com/Stv-devl/PlanPilotMCP) · [STVDigitalDelivery/revit-mcp](https://github.com/STVDigitalDelivery/revit-mcp)
- [SubarashiCode/Claude-Skills---Revit](https://github.com/SubarashiCode/Claude-Skills---Revit) · [sultandroid/SultanRevitMCP](https://github.com/sultandroid/SultanRevitMCP) · [SUNY-MEC-MEIC-Lab/ga-layout-optimizer](https://github.com/SUNY-MEC-MEIC-Lab/ga-layout-optimizer) · [SweMohdFaizan/RevIQ](https://github.com/SweMohdFaizan/RevIQ)
- [Taeky-Han/MCP](https://github.com/Taeky-Han/MCP) · [tanishqbhattad/rhino-mcp](https://github.com/tanishqbhattad/rhino-mcp) · [tell2jyoti/RevitBIMIntelligence](https://github.com/tell2jyoti/RevitBIMIntelligence) · [thaiscarvalhoarq-ux/https-github.com-mcp-servers-for-revit-mcp-server-for-revit-python](https://github.com/thaiscarvalhoarq-ux/https-github.com-mcp-servers-for-revit-mcp-server-for-revit-python)
- [thanhtranarch/t3lab-claude.skills-revit](https://github.com/thanhtranarch/t3lab-claude.skills-revit) · [Thestreetarckitect/revit-family-creator](https://github.com/Thestreetarckitect/revit-family-creator) · [thilo-h/render-director](https://github.com/thilo-h/render-director) · [tjxme/revit-family-maker](https://github.com/tjxme/revit-family-maker)
- [TomasMunoz77/stack](https://github.com/TomasMunoz77/stack) · [tony-hoang-ai-automation/revit-api-2027-with-claude](https://github.com/tony-hoang-ai-automation/revit-api-2027-with-claude) · [Top-Matrix/PyRevitMCPConfigurator](https://github.com/Top-Matrix/PyRevitMCPConfigurator) · [toshioams/MCP-Revit-Robot-Autocad](https://github.com/toshioams/MCP-Revit-Robot-Autocad)
- [tperez-dat/RevitMCPServer](https://github.com/tperez-dat/RevitMCPServer) · [TripleHHH-ui/Revit_Automation_Server](https://github.com/TripleHHH-ui/Revit_Automation_Server) · [Trungdev1611/RevitAPI_docs_mcp](https://github.com/Trungdev1611/RevitAPI_docs_mcp) · [TStrathaus/revit-mcp-status](https://github.com/TStrathaus/revit-mcp-status)
- [tuanank2112-crypto/revit-ai-native](https://github.com/tuanank2112-crypto/revit-ai-native) · [unknown-sama-ff/revit-auto](https://github.com/unknown-sama-ff/revit-auto) · [username-200/MCP-revit](https://github.com/username-200/MCP-revit) · [Utopia5327/claude-plugin-for-revit-bim](https://github.com/Utopia5327/claude-plugin-for-revit-bim)
- [uzair-codes/Connecting-Claude-with-Revit-2024](https://github.com/uzair-codes/Connecting-Claude-with-Revit-2024) · [vahidrahimiarchitect/Revit-AI-BIM-Agent-claude](https://github.com/vahidrahimiarchitect/Revit-AI-BIM-Agent-claude) · [VanChung003/Revit-Antigravity-MCP](https://github.com/VanChung003/Revit-Antigravity-MCP) · [Venkatchavan/OpenAEC-MCP](https://github.com/Venkatchavan/OpenAEC-MCP)
- [verbenedictos08/revit-claude-connector](https://github.com/verbenedictos08/revit-claude-connector) · [vibe-bimer/smart-revit](https://github.com/vibe-bimer/smart-revit) · [Vik154/Revit_MCP_Assistant](https://github.com/Vik154/Revit_MCP_Assistant) · [viktor-platform/revit-viktor-local-mcp](https://github.com/viktor-platform/revit-viktor-local-mcp)
- [vizletstudio/vizlet-claude-plugin](https://github.com/vizletstudio/vizlet-claude-plugin) · [vizzedev/MCP_projeto_executivo_Revit](https://github.com/vizzedev/MCP_projeto_executivo_Revit) · [vudang-95/MCP-Revit-Agent](https://github.com/vudang-95/MCP-Revit-Agent) · [warlends-source/civil-senior](https://github.com/warlends-source/civil-senior)
- [wbh0168/revit_mcp](https://github.com/wbh0168/revit_mcp) · [White167/WhiteRevit-releases](https://github.com/White167/WhiteRevit-releases) · [wisdjsjskdjskkwdj/antigravity-revit2018mcp](https://github.com/wisdjsjskdjskkwdj/antigravity-revit2018mcp) · [wrzonance/SpecR](https://github.com/wrzonance/SpecR)
- [wyuebei-cloud/autodesk-product-help-mcp](https://github.com/wyuebei-cloud/autodesk-product-help-mcp) · [wyyyz1937365497/mcp-server-for-revit-python](https://github.com/wyyyz1937365497/mcp-server-for-revit-python) · [Xhuynh16/Revit-mcp-plugin-glab](https://github.com/Xhuynh16/Revit-mcp-plugin-glab) · [xiuchuan666/revit-tunnel-mcp](https://github.com/xiuchuan666/revit-tunnel-mcp)
- [xppp3082/Revit_MCP](https://github.com/xppp3082/Revit_MCP) · [xuantinhnbs-rgb/autocad-trimble-mcp](https://github.com/xuantinhnbs-rgb/autocad-trimble-mcp) · [xuantinhnbs-rgb/navisworks-mcp](https://github.com/xuantinhnbs-rgb/navisworks-mcp) · [xupeiwust/archbuilder-ai-system](https://github.com/xupeiwust/archbuilder-ai-system)
- [xxHRUSHIKESHxx/BIMIntelligence](https://github.com/xxHRUSHIKESHxx/BIMIntelligence) · [yakovman1/RevitMCPchat](https://github.com/yakovman1/RevitMCPchat) · [yetsin7/RevitMCP](https://github.com/yetsin7/RevitMCP) · [yllld/MCP-Revit-Families](https://github.com/yllld/MCP-Revit-Families)
- [ynan3000/emperor-revit](https://github.com/ynan3000/emperor-revit) · [yogisugiono598-netizen/yg-labs-pyrevit](https://github.com/yogisugiono598-netizen/yg-labs-pyrevit) · [YonseiBIG/revit-mcp-BIG](https://github.com/YonseiBIG/revit-mcp-BIG) · [YonseiBIG/revit-mcp-commandset-BIG](https://github.com/YonseiBIG/revit-mcp-commandset-BIG)
- [YonseiBIG/revit-mcp-plugin-BIG](https://github.com/YonseiBIG/revit-mcp-plugin-BIG) · [YUiCHI-Ai/Revit-MCP-Analysis-](https://github.com/YUiCHI-Ai/Revit-MCP-Analysis-) · [YuWang2/revit_mcp](https://github.com/YuWang2/revit_mcp) · [YYChen168/CAD-Terrain-Revit-MCP](https://github.com/YYChen168/CAD-Terrain-Revit-MCP)
- [zainislit7-jpg/revit-mcp](https://github.com/zainislit7-jpg/revit-mcp) · [zbanner-wq/wardcraft-aecdm-mcp](https://github.com/zbanner-wq/wardcraft-aecdm-mcp) · [zeussilver/bim-ifc-mcp](https://github.com/zeussilver/bim-ifc-mcp) · [Zhenko-J/rift](https://github.com/Zhenko-J/rift)
- [zmq6931/revit-mcp-Revit2022](https://github.com/zmq6931/revit-mcp-Revit2022) · [zsl95903-ui/BIM-AI-Studio](https://github.com/zsl95903-ui/BIM-AI-Studio) · [zymorel/revit-mcp](https://github.com/zymorel/revit-mcp)

### GitHub repositories named in passing, not studied (5)

- [bimgeek/speckle-mcp](https://github.com/bimgeek/speckle-mcp) · [EvgenVer/AECPilot](https://github.com/EvgenVer/AECPilot) · [NonicaTeam/AI-Connector-for-Revit](https://github.com/NonicaTeam/AI-Connector-for-Revit) · [SamllPigYanDong/revit_mcp](https://github.com/SamllPigYanDong/revit_mcp)
- [WeberG619/RevitMCPBridge2026](https://github.com/WeberG619/RevitMCPBridge2026)

### Autodesk (help, blogs, AU, APS, Marketplace, forum, newsroom)

- <https://help.autodesk.com/cloudhelp/ENU/ADSKMCP-RevitMcp/files/ADSKMCP_RevitMcp_revit_mcp_server_html.html>
- <https://www.autodesk.com/blogs/aec/2026/06/17/revit-public-mcp-server/>
- <https://blog.autodesk.io/revit-mcp-server-update/>
- <https://help.autodesk.com/cloudhelp/ENU/ADSKMCP-RevitMcp/files/ADSKMCP_RevitMcp_setting_up_revit_mcp_server_html.html>
- <https://www.autodesk.com/support/technical/article/caas/sfdcarticles/sfdcarticles/How-to-download-the-Revit-MCP-server.html>
- <https://www.autodesk.com/autodesk-university/class/AI-Assistants-Connected-by-Claude-MCP-and-GPT-Agents-Using-Autodesk-Forma-Revit-and-Autodesk-Platform-Services-Workflows-2026>
- <https://help.autodesk.com/view/ADSKMCP/ENU/?guid=revitmcp_tools>
- <https://forums.autodesk.com/t5/revit-mep-forum/revit-mcp-server-tech-preview-launch/td-p/14262011>
- <https://www.autodesk.com/blogs/aec/2026/06/16/whats-new-in-revit-2027-1/>
- <https://conferences.autodesk.com/flow/autodesk/au2026/exhibitorcatalog/page/catalog/exhibitor/1774444428444001xupG>
- <https://adsknews.autodesk.com/en/news/autodesk-design-make-vision-au-2026/>
- <https://adsknews.autodesk.com/en/pressrelease/autodesk-advances-agentic-ai-in-its-three-industry-clouds/>
- <https://investors.autodesk.com/news-releases/news-release-details/autodesk-advances-agentic-ai-its-three-industry-clouds>
- <https://aps.autodesk.com/blog/au-2026-autodesk-building-ai-design-and-make>
- <https://www.autodesk.com/autodesk-university/class/AI-Driven-Revit-Vibe-Coding-Transformation-BIM-Workflow-Customization-2025>
- <https://blog.autodesk.io/design-make-marketplace/>
- <https://aps.autodesk.com/marketplace/getting-started>
- <https://aps.autodesk.com/app-store/mcp-publisher-guide>
- <https://damassets.autodesk.net/content/dam/autodesk/www/pdfs/mcp-faq.pdf>
- <https://aps.autodesk.com/marketplace/mcp-publisher-guide>
- <https://aps.autodesk.com/app-store/publisher-center/revit>
- <https://appstore.autodesk.com/assets/public/legal/PublisherAgreement.pdf>
- <https://aps.autodesk.com/blog/building-custom-mcp-servers-autodesk-platform-services>
- <https://aps.autodesk.com/blog/authentication-patterns-aps-mcp-servers>
- <https://marketplace.autodesk.com/apps/29edb5ce-fedc-4581-bc24-f03040d21056>
- <https://marketplace.autodesk.com/apps/c3a89233-73ba-4e1e-b436-905d32996efa>
- <https://marketplace.autodesk.com/apps/64aff156-c166-4dd5-93f4-d79aa0ed3502>
- <https://www.autodesk.com/integrations/partner/kestrel-labs>
- <https://forums.autodesk.com/t5/revit-structure-forum/revit-mcp-server-tech-preview-launch/td-p/14262013>
- <https://www.autodesk.com/autodesk-university/class/MCP-on-the-Autodesk-Platform-Three-Production-Servers-Across-the-Full-Stack-2026>
- <https://www.autodesk.com/autodesk-university/class/Innovators-Stage-Skills-to-Pay-the-Bills-Creating-Claude-Skills-for-Autodesk-Workflows-2026>
- <https://www.autodesk.com/autodesk-university/class/Using-MCP-to-Build-Self-Improving-AEC-Data-Workflows-2026>

### Commercial products and their pages

- <https://archilabs.ai/posts/commercial-revit-mcp-server-vendors>
- <https://revitmcp-com.pages.dev/>
- <https://archilabs.ai/competitor-comparison/autodesk-revit-mcp-vs-nonica>
- <https://www.aecpilot.com/revit-mcp>
- <https://www.aecpilot.com/>
- <https://www.aecpilot.com/revit-mcp-comparison>
- <https://www.aecpilot.com/academy>
- <https://www.aecpilot.com/autocad-mcp#hmktools>
- <https://archilabs.ai/pricing>
- <https://datadrivenaec.com/tools/archilabs>
- <https://www.bimcopilot.com/blog/the-best-revit-plugins-in-2026-ai-artificial-intelligence>
- <https://archilabs.ai/competitor-comparison/revit-ai-automation-pricing-features>
- <https://www.g2.com/products/archilabs/pricing>
- <https://archilabs.ai/competitor-comparison/best-revit-ai-assistant-2026>
- <https://swapp.ai/>
- <https://archilabs.ai/competitor-comparison/archilabs-vs-swapp>
- <https://swapp.ai/llms.txt>
- <https://www.aecplustech.com/tools/swapp>
- <https://www.mannpublications.com/mannreport/2026/06/11/kestrel-labs-unveils-first-ai-powered-compliance-platform-built-natively-inside-bim/>
- <https://www.aecplustech.com/blog/kestrel-labs-bringing-code-compliance-inside-bim-model>
- <https://www.saasrise.com/deals/kestrel-labs-raises-215m-preseed-round-to-launch-aipowered-revit-code-compliance-platform-1433c47c-9a12-4c69-9723-ab621eb1c294>
- <https://evolvelab.io/glyph>
- <https://forum.evolvelab.io/t/glyph-release-3-0/11180>
- <https://adelphos.ai/apps/revit-copilot>
- <https://bim-automation-studio.github.io/pricing.html>

### MCP registries and directories

- <https://mcpservers.org/servers/nonicateam/ai-connector-for-revit>
- <https://glama.ai/mcp/servers/i9uhjqvx65>
- <https://mcpservers.org/servers/hmktools-com-revit-mcp>
- <https://glama.ai/mcp/servers/Demolinator/revit-mcp-server>
- <https://mcpservers.org/servers/revit-mcp/revit-mcp-python>
- <https://lobehub.com/pl/mcp/bimgeek-speckle-mcp>
- <https://langdb.ai/app/mcp-servers/speckle-mcp-server-00ac18f2-4b13-494d-9bd8-d6f5fe53dabe>
- <https://glama.ai/mcp/servers/Daviidro/speckle-mcp>
- <https://www.getdrio.com/mcp/io-github-sam-aec-autodesk-revit-mcp-server/md>
- <https://www.getdrio.com/mcp/io-scanbimlabs-revit/md>
- <https://www.getdrio.com/mcp/io-github-bimwright-rvt-mcp/md>
- <https://www.getdrio.com/mcp/io-github-labsfuture-revit-converter-remote-mcp-release/md>
- <https://www.getdrio.com/mcp/io-github-horizungroup-horizun-revit-mcp>
- <https://socket.dev/npm/package/@shuotao/revit-mcp-server>
- <https://glama.ai/mcp/servers/jillt5141p>
- <https://glama.ai/mcp/connectors/io.scanbimlabs/revit-mcp>
- <https://smithery.ai/server/@SamllPigYanDong/revit_mcp>
- <https://mcp.so/servers/revit-mcp-zedmoster?tab=config>
- <https://mcp.so/ja/servers/revit-mcp?tab=config>
- <https://hol.org/registry/agent/uaid:aid:4QkzujJmXA5QDNY9UBNtDoxAswwLxEW6odJpBYvwYwAGnkPnRnCBvSJYzVViRMdsf6>
- <https://playbooks.com/mcp/revit>
- <https://claudemarket.ai/mcp/Sam-AEC/Autodesk-Revit-MCP-Server>
- <https://policylayer.com/policies/mcp-servers-for-revit-revit-mcp>
- <https://educasium.com/en/ressources/mcps/revit-mcp>

### Reviews, press and setup guides

- <https://aecomcps.com/guides/revit-mcp-options-compared>
- <https://architosh.com/2026/04/autodesk-revit-2027-big-new-ai-and-graphics-changes/>
- <https://buildtechscope.com/revit-2027-mcp-server-ai-bim/>
- <https://www.revit.news/2026/11/revit-mcp-server-setup-guide/>
- <https://www.revit.news/2026/11/autodesk-university-2026-what-project-intelligence-means-for-revit/>
- <https://zadax.net/en/blog/revit-2027-released-ai-assistant-forma-connection-and-accelerated-graphics/>
- <https://cadpilot.com/home/company/news/revit-2027.2-update.html>
- <https://cadpilot.com/home/company/news/revit-interoperability-2027.2-update.html>
- <https://goto.archi/blog/post/whats-new-in-revit-2027>
- <https://engtechnica.com/ai-moves-from-reading-revit-models-to-building-them/>
- <https://engtechnica.com/autodesk-pushes-ai-beyond-individual-design-tools-with-a-connected-assistant/>
- <https://www.engineering.com/autodesk-previews-agentic-ai-for-connected-project-workflows/>
- <https://architosh.com/2026/10/au2026-autodesk-shifts-ai-from-chatbots-to-agentic-ecosystems/>
- <https://architosh.com/2026/06/aia26-kestrel-labs-native-bim-compliance-platform/>
- <https://dancumberlandlabs.com/blog/revit-construction/>
- <https://aidirectory.aecmag.com/entry/glyph/>
- <https://aecmag.com/ai/evolvelab-bringing-ai-to-architecture/>
- <https://aecmag.com/bim/giving-ai-actual-write-access-in-revit/>
- <https://aecmag.com/technology/aec-magazine-september-october-2026-out/>
- <https://archicoder.com/posts/revit-mcp-what-autodesks-server-can-and-cannot-do/>
- <https://blog.bimsmith.com/Revit-2027-What-the-Built-In-MCP-Server-Actually-Does-in-Practice>
- <https://huskybim.com/learn/revit-mcp>
- <https://bimtopia.com/bimtopia-academy/installing-the-revit-mcp-server>

### Research papers and benchmarks

- <https://www.iaarc.org/publications/fulltext/ISARC2026_1096.pdf>
- <https://arxiv.org/html/2606.20146v1>
- <https://www.sciencedirect.com/science/article/abs/pii/S0926580525004145>
- <https://www.researchgate.net/publication/400983065_Orchestrating_LLM-Powered_Workflows_for_Autodesk_Revit_via_Model_Context_Protocol_A_Multi-Agent_Framework_for_Intelligent_BIM_Automation>
- <https://arxiv.org/pdf/2606.02470>

### GitHub pages that are not a single repository

- <https://github.com/orgs/mcp-servers-for-revit/repositories>
