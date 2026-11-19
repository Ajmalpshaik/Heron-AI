# FRAGMENT-ISSUES archive — section 5b, rows 401 to 425

> **Finished rows, moved out of the live register.** These are rows from section 5b of
> [`FRAGMENT-ISSUES.md`](../FRAGMENT-ISSUES.md) — Heron's own defects found by reading the repository — whose state said they were
> finished when they were moved. Each still has its line in the register: the same number, a
> one-line title, the opening of its state, and a link to its full text here.
>
> **Nothing below was rewritten** except its links, which were re-pointed one folder deeper and
> each checked to reach the same file it reached before. Where a row disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win** — a row records what was true on
> the day it was written.
>
> **Nothing new is written here.** A new defect goes in the register, and a row here that turns
> out not to be finished is reopened **there**, on its own line.
>
> Rows 401 to 425 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-402

*Moved from the register on 2026-11-17.*

**Defect.** **`tests/test_mcp_serves.py` DOES NOT RETURN INSIDE `check-gaps`' 300-SECOND CEILING IN A CLOUD CONTAINER, SO `check-gaps` LISTS IT UNFINISHED.** Found 2026-10-09 running `python tools/check-gaps.py` end to end for the first time in the project review of 2026-10-08 (its plan retired 2026-11-19 - [the work-notes README](../work-notes/README.md)) (at low priority, beside two other agents' builds, on 4 CPUs): *HUNG test_mcp_serves.py - still running after 300s, gave up*. Timed side by side under the same load: `main` at 661384f6 took 445 s and this branch 562 s, both exit 0. So the suite was already over the ceiling before this branch's new sections (row 5b-233's lookup, row 5b-274's activity list) were added; they add about a quarter. `SUITE_TIMEOUT` in [`tools/check-gaps.py`](../../tools/check-gaps.py) says 300 s is about four times the slowest suite measured there, 78 s on 2026-09-10. NOT measured on an unloaded machine or on the owner's PC.

**State.** **MOVED TO ISSUE [#939](https://github.com/Ajmalpshaik/Heron-AI/issues/939) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not fixed.** Either the suite is made faster or `check-gaps` gives it a ceiling of its own, and the choice is for whoever owns that rule; raising the one ceiling for every suite would let a hung suite wait longer before it is named.

---

### Row 5b-403

*Moved from the register on 2026-11-17.*

**Defect.** **THE COMPANION'S PAGE ACTIONS THAT REACH REVIT STILL GUESS HOW A CALL ENDED, OR WRITE REFUSED FOR A REPLY THAT IS NOT OK.** Carried from [row 5b-389](../fragment-issues/section-5b-rows-376-400.md) on 2026-10-09, when that row's `revit_*` half was fixed. Four page actions in mcp/server/heron_mcp_server.py reach Revit and write their own line on the activity list. `_apply_change` (companion_apply) records revit_change's answer with no outcome, so that line is guessed from its first line. It calls `_through(revit_change, origin="companion")` with no reply_out, a line tests/test_companion.py pins as text. `_apply_table` (companion_table_apply) writes `refused` only when the fragment's resultJson says nothing was applied, and no outcome otherwise, so a lost answer or revit_busy is guessed. `_sprinkler_layout_place` (companion_sprinkler_layout_place) writes `refused` for any revit_change reply that is not ok, so a lost placement answer shows as refused. Its good line is guessed, and its earlier refusals write no line. `_load_settings` (companion_load, the page's Load from Revit) writes `refused` for any revit_read reply that is not ok, so a lost answer or revit_busy shows as refused. Its good line is guessed. Its earlier returns (NotBound, list_views not answered, the pin's refusal, no active view) write no line at all. Since row 5b-389, `_through` hands the body's own label back in reply_out["ended"], and that is the hook the repair can use. Read in the source, NOT RUN.

**State.** **MOVED TO ISSUE [#952](https://github.com/Ajmalpshaik/Heron-AI/issues/952) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not fixed.** The repair records the label `_through` hands back in reply_out["ended"] on each page action's line. `_apply_change` needs a reply_out on its `_through` call, which changes the line tests/test_companion.py pins, so the Companion's owner should agree to that first.

---

### Row 5b-408

*Moved from the register on 2026-11-17.*

**Defect.** **`.claude/skills/heron-session/bin/hook_log.py` LINE 39 HOLDS `\H` IN AN ORDINARY DOCSTRING - `%LOCALAPPDATA%\Heron\logs` - SO UNDER PYTHON 3.12 A HOOK THAT IMPORTS IT PRINTS *"SyntaxWarning: invalid escape sequence '\H'"* TO ITS STDERR.** The same class as [row 379](../fragment-issues/section-5b-rows-376-400.md), one file over. Seen 2026-10-09 in the session [row 5b-378](../fragment-issues/section-5b-rows-376-400.md) measured: two hook results in that chat's transcript carry it - the SessionStart hook's stderr, 204 bytes, and the PreToolUse hook's on the chat's only Write, 226 bytes. Reproduced 2026-10-10 on the owner's PC, Python 3.12.10, by compiling the file with warnings shown: `hook_log.py:39: SyntaxWarning: invalid escape sequence '\H'`. `heron_guard.py`, `main_moved.py` and `session_line.py` each import it to write their diary line. **It repeats because no Python 3.12 cache of the file is kept:** the main checkout's `bin/__pycache__` holds only `hook_log.cpython-311.pyc`, so every 3.12 run compiles it again - why 3.12 writes none there was not checked. **Cosmetic today:** the diary line is still written and no hook's decision changes. A later Python is due to make the sequence an error, and `import hook_log` sits inside guards written never to raise (`heron_guard.diary` catches `Exception`), so the hooks' diary would then stop without a word. A doubled backslash or a raw docstring ends it; row 379's test, `tests/test_placement.py` section 7, is the shape of the guard. **Recorded, not fixed:** this change records measurements only.

**State.** **FIXED 2026-10-11 by [#473](https://github.com/Ajmalpshaik/Heron-AI/pull/473).** The backslashes are doubled, and the same defect in `tests/test_bridge_roundtrip.py` and `tools/check-products.py`, found by compiling all 536 tracked Python files, is fixed with it. `tests/test_compiles_quietly.py` compiles every tracked file from source and fails on any warning: on main before the change it named all three files; after, 0 warnings.

---

### Row 5b-413

*Moved from the register on 2026-11-17.*

**Defect.** **"KEEP ONLY THE UNIQUE DATES TYPED ON THE SHEETS" COULD NOT BE ANSWERED: BOTH DATE TOOLS LOST THE DATES TO THE REPLY'S THREE-ITEM CUT.** Found 2026-10-10 on the owner's model `4355-BHVD-3D-50C10-BL001A` (Revit 2020, session 36948). `FIND_DATES_IN_VIEWS` read every text note and found 7 dates, but `findings` is a list and `RevitFragment.Describe` sends its first three, so 5 of the 7 never reached the reply. `EXTRACT_DATES_FROM_TEXT` needs a selection and returns a dictionary, which is sent as a count alone. Not a new defect - the same cut as the earlier rows on truncated lists; this row is the job it blocked. **Answered by a new tool, DRAFT:** `LIST_DATES_ON_SHEETS` ([its card](../../brain/fragments/list-dates-on-sheets/fragment.yaml)) reads every sheet with nothing selected, keeps only text notes a sheet owns, and leaves the whole list as one string, `dateList`, which the reply prints in full. It ran on that model - by mistake before its trial: the chat was still pinned to it while a scratch project was in front. It only reads and nothing changed. It returned the same 7 dates, on 15 of the 19 sheets. Proof plan: [Group CY](../needs-checking/group-cy.md).

**State.** **ANSWERED BY A NEW TOOL, DRAFT.** The general cut stays as recorded.

---

### Row 5b-414

*Moved from the register on 2026-11-17.*

**Defect.** **ON REVIT 2020 A QUESTION ABOUT THE LETTERS NOW REACHES A WRITE, AND THE ISSUED-REVISION RULE WOULD REFUSE A LETTER-LIST CHANGE THAT CHANGES NO PRINTED LETTER.** Found 2026-10-10 widening `EDIT_REVISION_NUMBERING_SEQUENCE` to 2020 and 2021 (version 2, DRAFT). (1) *"what letters does the alphanumeric revision numbering use"* went to `LIST_REVISIONS` and now reaches this write on 2020, as it already reached `CREATE_REVISION_NUMBERING_SEQUENCE` on 2022 and later. Guarded: a question carries no values, and the tool refuses when nothing was asked for. (2) *"change the sheet numbers to start at 100"* reaches this tool on 2022 and later and now on 2020 too - wrong before this change. (3) **A decision for the owner:** a list change is refused while any ISSUED revision uses that numbering. On `4355-BHVD-3D-50C10-BL001A` the list reads `AA, BB, CC, D, E ...` and the wanted one `AA, BB, CC, DD ...`, which leaves AA to CC printing exactly what they print now - the rule refuses it all the same. Whether to refuse only when an issued revision's printed letter would really change is the owner's call; unchanged here.

**State.** **MOVED TO ISSUE [#926](https://github.com/Ajmalpshaik/Heron-AI/issues/926) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN.** (1) and (2) recorded; (3) waits for the owner.

---

### Row 5b-415

*Moved from the register on 2026-11-17.*

**Defect.** **ON THE OWNER'S LAPTOP WINDOWS BLOCKS FRESHLY BUILT TEST PROGRAMS, SO `tools/api-surface` AND PART OF `tests/test_fragment_symbols.py` CANNOT RUN THERE.** Found 2026-10-10 by two sessions. Windows App Control refused the freshly built `ApiSurface.exe` (exit 1) and `ApiSurface.dll` (exit 127), and the 2020 and 2026 parts of `test_fragment_symbols` (0x800711C7). The machine, not the change: in one session the same commit passed from a fresh folder. Every member the new and widened fragments use was looked up in `RevitAPI.xml` of all eight reference packages instead. **Read these as NOT RUN on that PC, never as a pass or as a failure of the change.**

**State.** **MOVED TO ISSUE [#914](https://github.com/Ajmalpshaik/Heron-AI/issues/914) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN** - a condition of the machine.

---

### Row 5b-416

*Moved from the register on 2026-11-17.*

**Defect.** **REVIT'S COMPANION BUTTON LOOKS FOR CHAT NOTES IN THE REAL `%LOCALAPPDATA%\Heron\companion`, AND ON A PC WHOSE CLAUDE APP IS THE MICROSOFT STORE (MSIX) BUILD EVERY CHAT WRITES ITS NOTE INTO THE APP'S PRIVATE COPY - SO THE BUTTON IS EXPECTED TO SAY NO CHAT IS RUNNING WHILE ONE IS.** Found 2026-10-10 on the owner's laptop (Windows 11), by reading the source beside what his own Terminal panel and Claude's tool shell each see. **The redirection, verified that day from both sides:** the Claude desktop app is the packaged `AnthropicPBC.Claude_fnn82j28hfe8t` (in `C:\Program Files\WindowsApps`). Windows sends every write its child processes make under `%APPDATA%` and `%LOCALAPPDATA%` to `%LOCALAPPDATA%\Packages\AnthropicPBC.Claude_fnn82j28hfe8t\LocalCache\Roaming` and `...\LocalCache\Local`, and shows only those processes a merged view. The children are Claude Code's `claude.exe`, Claude's Bash and PowerShell tools, and every heron MCP server (`python mcp/server/heron_mcp_server.py`, parent `claude.exe` under the packaged `Claude.exe`). Revit is not one of them. **The two halves:** `mcp/companion/heron_companion.py` `companion_dir()` (line 1269) and `publish()` (line 1482) write `chat-<pid>.json` to `%LOCALAPPDATA%\Heron\companion`, which lands in the private copy. `revit/Heron.Revit.Addin/HeronCompanionCommands.cs` `Pick()` (line 85) lists `HeronPaths.Companion` - `platform/Heron.Core/HeronPaths.cs` line 95, the real folder - finds nothing, and `Execute` shows *"No Claude chat with Heron is running on this PC"*. **Seen that day:** from the owner's terminal the real folder held no `chat-*.json`; from Claude's shell the private copy's `Local\Heron\companion` held 86, dated 2026-10-09 and 10 (left by chats that ended without removing them; the button ignores a note older than 30 s). Reading the other way works, which is why Revit tools work there: the add-in's `live`, `bridges` and `config` files are real, and the container reads them through. A packaged app's child has no package identity to ask for: `GetCurrentPackageFullName` answered 15700 (no package) from inside Claude. **Not checked:** the button pressed in Revit on this laptop; a Store-built Claude on any other PC; and what the merged view shows when one file name is in both places - the private copy's `Roaming\Heron\config` and `Local\Heron\bridges` folders exist but were EMPTY that day, so nothing the add-in writes is shadowed yet.

**State.** **MOVED TO ISSUE [#900](https://github.com/Ajmalpshaik/Heron-AI/issues/900) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired; NEEDS REAL REVIT** - [NEEDS-CHECKING BK14](../needs-checking/group-bk.md). Where the note should go so both sides see it - a folder Windows does not redirect, or the add-in also reading the private copy - is part of [Q-63](../OPEN-QUESTIONS.md), Ajmal PS's to decide. **FIXED IN CODE 2026-10-18 - [D-135](../decisions/D-135.md), NOT RUN IN REVIT.** Both halves now name `%TEMP%\Heron\companion`: the chat through `heron_bridge_client.temp_folder()`, which reads TMP, TEMP and the profile in .NET's order, and the add-in through `HeronPaths.Companion`, built on `Path.GetTempPath()` under a new `HeronPaths.SharedDerived`, still DERIVED. A folder and a file written there from inside the app were on the real disk when read from outside, 2026-10-17. `tests/test_data_home.py` section 6 and `tests/Heron.Kernel.TestHost` section 2 hold the two sides together; each FAILED on the code before. Owed: the add-in rebuilt and deployed from this change - an add-in deployed before it still looks in `%LOCALAPPDATA%\Heron\companion` - then [BK14](../needs-checking/group-bk.md) pressed in a Revit the owner started. **PRESSED 2026-10-18 IN REVIT 2024 ([HF4](../needs-checking/group-hf.md)): STILL FAILS, for a new reason - [row 1100](../FRAGMENT-ISSUES.md#).** The chats' notes were in `%TEMP%\Heron\companion`, as D-135 means; Revit runs with a temporary folder of its own, a GUID-named sub-folder of that one, and the add-in looked there. **FIXED IN CODE AGAIN 2026-10-18 - [D-136](../decisions/D-136.md), [#657](https://github.com/Ajmalpshaik/Heron-AI/pull/657), NOT RUN IN REVIT:** both halves step out of a GUID-named temporary folder, so the add-in reads the user's `%TEMP%\Heron\companion` from inside Revit too - [row 1100](../FRAGMENT-ISSUES.md#). The re-press is owed, as [HF4](../needs-checking/group-hf.md).

---

### Row 5b-418

*Moved from the register on 2026-11-17.*

**Defect.** **ON A PC WHOSE CLAUDE APP IS THE MICROSOFT STORE BUILD, WHAT HERON'S PYTHON SIDE WRITES UNDER APPDATA - FOR A PERSON TO OPEN, OR FOR A TOOL RUN OUTSIDE CLAUDE TO READ - EXISTS ONLY IN THE APP'S PRIVATE COPY.** Found 2026-10-10 on the owner's laptop, from the redirection [row 5b-416](../fragment-issues/section-5b-rows-401-425.md) records. Each writer below resolves its folder from `%APPDATA%` or `%LOCALAPPDATA%` inside the MCP server, a child of the packaged app: the knowledge store `global.db` and each project's memory (`brain/heron_scope.py` `knowledge_dir()`, used by `heron_designbasis.py`, `heron_earlier.py`, `heron_building_loads.py` `_folder()`, `heron_sprinkler_run.py` and `heron_sprinkler_layout.py`); the brain's audit `audit-brain-YYYYMM.jsonl` (`brain/heron_audit.py`), beside the add-in's own audit in `%APPDATA%\Heron\audit`, which is real; the Companion's notes (row 5b-416); and the hooks' log `heron-hooks.jsonl` (`.claude/skills/heron-session/bin/hook_log.py`). **Verified private-only from the owner's terminal that day:** the store, the loads project memory with its html, pdf and csv reports, `audit-brain-*.jsonl` and the Companion notes - but that project memory was COPIED there by a Claude session the same day, not written by Heron. **Seen in the private copy from Claude's shell, real folder NOT checked:** `heron-hooks.jsonl`. **A load report usually lands somewhere real:** `mcp/server/heron_brain.py` `loads_report()` (line 1775) writes to the folder the modeller chose, else the one last chosen, else `Heron loads\<run>` beside the saved Revit model, and only with none of those into the run's folder under the knowledge store - so a report is private only for an unsaved model with no folder ever chosen. **What follows, reasoned and NOT tried:** a report path the chat prints for that case opens nothing in Explorer; `python tools/hook-report.py` or a look at the brain audit from the owner's terminal sees none of it; and `python tools/heron-ready.py` run from his terminal on main builds a SECOND store in the real `%APPDATA%\Heron\knowledge`, which the live Heron does not read while the copy holds one.

**State.** **MOVED TO ISSUE [#900](https://github.com/Ajmalpshaik/Heron-AI/issues/900) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - A QUESTION, NOT A MEASUREMENT.** No report Heron itself wrote under AppData has been looked for from outside Claude. The test: with an unsaved model and no report folder ever chosen for the project, ask for a load report, then open the folder the chat names from Explorer. Where Heron's Python side should keep what a person or Revit must see is [Q-63](../OPEN-QUESTIONS.md), Ajmal PS's to decide. **FIXED IN CODE 2026-10-18 - [D-135](../decisions/D-135.md).** The knowledge store, every project's memory and the brain's audit are written under `%USERPROFILE%\.heron` on Windows, which the app does not copy - measured from both sides on 2026-10-17 - so a load report's fallback folder, under the store, follows them. The first use carries what each Store Claude's copy and `%APPDATA%\Heron` hold into it, copied, never moved. `tests/test_data_home.py` sections 2 to 5, each FAILED on the code before. The hooks' log `heron-hooks.jsonl` is not moved: development only, and DERIVED. Owed: the first chat on this code on the laptop - [NEEDS-CHECKING HF1](../needs-checking/group-hf.md). **RUN 2026-10-18 on the laptop - HF1 PASS ([Group HF](../needs-checking/group-hf.md), [#654](https://github.com/Ajmalpshaik/Heron-AI/pull/654), `c48884e3`):** the first chat on this code carried the store, every project's memory and the brain's audit into `%USERPROFILE%\.heron`, read there from outside the app, and HF3 saw new brain audit lines land in the home. This row's own test - a loads report with no folder chosen, opened from Explorer - is HF3's other half, postponed by the owner, so the row stays open (noted 2026-10-18).

---

### Row 5b-419

*Moved from the register on 2026-11-17.*

**Defect.** **A RESET OR UNINSTALL OF A MICROSOFT STORE CLAUDE APP DELETES ITS PRIVATE COPY OF APPDATA - EVERY PACKAGE CLAUDE INSTALLED, HERON'S KNOWLEDGE STORE AND PROJECT MEMORY, AND THE BRAIN'S AUDIT - AND NOTHING IN HERON SAID SO.** Found 2026-10-10 on the owner's laptop, from the redirection [row 5b-416](../fragment-issues/section-5b-rows-401-425.md) records. Windows keeps a packaged app's redirected writes in `%LOCALAPPDATA%\Packages\<package>\LocalCache` and removes that folder when the app is reset (Settings, Apps, Installed apps, Claude, Advanced options, Reset) or uninstalled - Windows' behaviour for packaged apps as the owner gave it, NOT tried here, since it would delete his data. On the laptop that folder holds every `--user` package Claude installed, `global.db`, the project memory under `knowledge\projects` (load runs, design basis, earlier answers), `audit-brain-202610.jsonl`, the Companion notes and the hooks' log ([row 5b-418](../fragment-issues/section-5b-rows-401-425.md)). The packages and the store can be made again (`heron-ready`, `heron_scope --rebuild`); the project memory and the brain's audit cannot. `platform/Heron.Core/HeronPaths.cs` says DATA *"must survive every update, uninstall and reinstall"*, and on such a PC another program's Reset deletes it. Before this row, no file in `docs/` named the Store build's copy (grep for MSIX, WindowsApps, LocalCache, AnthropicPBC). **Not checked:** whether an update of the Claude app, as opposed to a Reset, keeps the folder.

**State.** **MOVED TO ISSUE [#900](https://github.com/Ajmalpshaik/Heron-AI/issues/900) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired.** The warning proposed with [row 5b-417](../fragment-issues/section-5b-rows-401-425.md) names it, in `heron-ready` section 2a and WHAT-TO-INSTALL §1b. Keeping DATA where an app reset cannot reach it is part of [Q-63](../OPEN-QUESTIONS.md), Ajmal PS's to decide. **FIXED IN CODE 2026-10-18 - [D-135](../decisions/D-135.md):** Heron's DATA no longer lives in the copy, so a Reset takes only the packages Claude installed itself and the old copies of Heron's data, which the first use has carried out. Heron deletes none of them; removing them is the owner's to decide. Not tried: a Reset itself. **2026-10-18 on the laptop - HF1 PASS ([Group HF](../needs-checking/group-hf.md), [#654](https://github.com/Ajmalpshaik/Heron-AI/pull/654)):** the store, every project's memory and the brain's audit were carried into `%USERPROFILE%\.heron` and read there from outside the app, so a Reset no longer holds Heron's only copy of them. A Reset itself is still not tried, and removing the old copies ([HF6](../needs-checking/group-hf.md)) was not put to the owner (noted 2026-10-18).

---

### Row 5b-420

*Moved from the register on 2026-11-17.*

**Defect.** **`tests/test_heron_ready.py` FAILED THREE TIMES IN SIXTEEN RUNS OF THE COMMITTED TOOL ON THE OWNER'S LAPTOP ON 2026-10-10, AT TWO DIFFERENT STEPS, WITH NOTHING CHANGED BETWEEN RUNS - AND NO FAILING RUN'S TOOL OUTPUT WAS CAPTURED.** Found capturing the before-evidence for [row 5b-417](../fragment-issues/section-5b-rows-401-425.md) at commit 489a6e63, from Claude's tool shell inside the Store app, other Claude sessions running beside it. Twice the suite itself failed section 4: `--snapshot` exited 1 and wrote no file, so the four checks reading the file failed with it (`lookup_seconds` None). Once a scratch copy of the suite failed section 1: the tool exited 1 with no PyYAML, as it should, but its output lacked `pip install --user -r requirements.txt`. The other thirteen passed - the suite once, and a copy that printed the tool's whole output on every run nine times in a row. **Not found:** the cause - the suite prints only its own verdicts, and the tool's output from the failing runs was not kept. An exit 1 with no file would fit an exception `write_snapshot()` does not catch, which is a guess. **Not checked:** another PC, or this one with no other sessions running.

**State.** **MOVED TO ISSUE [#939](https://github.com/Ajmalpshaik/Heron-AI/issues/939) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - intermittent, cause not found; recorded, not repaired.** A suite that printed the tool's output beside a failed check would name the cause the next time. NOT RUN: CI or the office PC for this.

---

### Row 5b-423

*Moved from the register on 2026-11-17.*

**Defect.** **ON REVIT 2026 AND 2027 NO OMNICLASS CODE CAN BE CHECKED OUT OF THE BOX - AUTODESK SHIPS ONLY A 14-LINE SAMPLE CLASSIFICATION FILE, SO HERON REFUSES EVERY CODE THERE UNTIL THE OFFICE SUPPLIES ITS OWN.** Read 2026-10-11 on the owner's laptop: `Classification_Taxonomy.txt` in `C:\ProgramData\Autodesk\RVT 2026\UserDataCache` and the 2027 copy, byte-identical to the 2027 per-user one, open *"This is a sample Classification File"* and hold codes such as `*0.1` filed under no category, where the 2020 and 2024 `OmniClassTaxonomy.txt` hold 2,645 rows, 21 of them under Pipe Accessories. Revit 2026's `Utility.dll` names `AssemblyCode_Sample.txt` beside it where 2024's names `UniformatClassifications.txt`, so the Assembly Code list may be a sample there too - not looked at in Revit. `SET_FAMILY_SETTINGS` v2 takes only a code the file gives the family's category, by the owner's rule *validate, never free text*, so on 2026 and later it refuses and says the file is the sample; the family methods leave the step out; the check judges nothing. Whether Revit 2026 reads a classification file from any other place is not known (NEEDS-CHECKING DB).

**State.** **MOVED TO ISSUE [#920](https://github.com/Ajmalpshaik/Heron-AI/issues/920) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - the owner's decision.** Either the office's own classification file is put where the refusal names, which needs nothing changed in Heron, or the owner chooses another rule for 2026 and later. Nothing accepts free text meanwhile.

---

### Row 5b-424

*Moved from the register on 2026-11-17.*

**Defect.** **"SET THE KEYNOTE OF THIS TYPE" NOW REACHES `CHECK_FAMILY_STANDARDS`, A READ, WHERE IT REACHED `SET_FAMILY_TYPE_VALUES` BEFORE ROW 5b-422's REPAIR.** Measured 2026-10-11 through `heron_retrieve.find` on both backends, private stores, base `74ffb4f3` against the branch: `CHECK_FAMILY_STANDARDS` v3's semantic identity now names its OmniClass, Assembly Code and Keynote checks, and that sentence moved to it. The wrong way round in the safe direction - a request to change something answered by a read changes nothing - but the modeller is not given the tool that writes it. A narrower identity moved it back and also lost four gains and sent *"set the assembly code to D2020"* back to `CREATE_ASSEMBLY_VIEWS`, a wrong WRITE, so the broader one was kept. The sentence is the session's own wording, not a modeller's, so it was not added to any card. No answer moved from a read to a write (`tools/check-routing.py`, the same question-to-write list as on the base tree).

**State.** **MOVED TO ISSUE [#956](https://github.com/Ajmalpshaik/Heron-AI/issues/956) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired.** A modeller's own words for setting a type's keynote, when one is heard, belong on `SET_FAMILY_TYPE_VALUES` - a PROVEN card whose `fragment.yaml` is outside its proof seal.

---
