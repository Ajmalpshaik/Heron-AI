# Needs checking — Group HF

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-18 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group HF - Heron's data out of a Store Claude's copy, and the Companion's note where Revit looks: D-135 on a real PC (2026-10-18)

Found from [Q-63](../open-questions/tier-2.md) and rows [5b-416, 5b-418 and 5b-419](../fragment-issues/section-5b-rows-401-425.md);
decided and built as [D-135](../decisions/D-135.md). **What changed.** On Windows Heron's Python side keeps its
knowledge store, every project's memory and the brain's audit in `%USERPROFILE%\.heron` (`HERON_DATA`
overrides). The MCP server at start, and `heron-ready` when it builds the store, carry in - copied, never
moved - what each Store Claude's copy and `%APPDATA%\Heron` hold, once, leaving `carried.json` to say so. The Companion's notes are in
`%TEMP%\Heron\companion` on both sides. The add-in's settings and trail do not move.

**What is measured, tested and not run.** Measured on the owner's laptop on 2026-10-17 and 18, from Claude's
shell and from his Terminal panel: where writes from inside the app land (D-135's table); and two trial
carries of his real data into scratch folders, one from each side - 39 files copied and 24,964 audit lines
from inside, 39 files and 25,004 lines from outside (the 40 more are a brain file only the real disk held,
which the copy hides from inside), and the copy's 41 files the same, byte for byte, before and after both.
`tests/test_data_home.py` and `tests/Heron.Kernel.TestHost` section 2 FAILED on the code before and pass on
it. **Not run:** the real carry, into the real `%USERPROFILE%\.heron`, by a chat on this code; and the
Companion button, which needs the add-in rebuilt and deployed from this change.

| # | Run | Look for |
|---|---|---|
| ~~**HF1**~~ | **RUN, PASS 2026-10-18 on the laptop** - one carry, 39 files copied and 25,118 audit lines, nothing failed; the copy's 41 files unchanged - [what was seen](#2026-10-18-the-run-on-the-laptop), below. *The real carry, once, on the laptop, after this change is on `main` and the Claude app restarted.* Before: `python brain/heron_paths.py --where` from Claude's shell and from the owner's own terminal - both name `C:\Users\ajmal\.heron`, which holds no `carried.json` yet (a suite run can make the folder itself - [row 5b-1070](../fragment-issues/section-5b-rows-401-425.md)). Then any chat that asks Heron anything. Then from the owner's terminal: `dir %USERPROFILE%\.heron` and `type %USERPROFILE%\.heron\carried.json` | `carried.json` names both old places, about 39 files copied and the brain's audit lines added, nothing failed; `knowledge\projects` holds the loads project's memory. **The D-30 negative:** the copy's `LocalCache\Roaming\Heron` holds as many files as before, the same - nothing moved, nothing deleted; and a second chat leaves no second run in `carried.json` |
| ~~**HF2**~~ | **RUN, PASS 2026-10-18** - 40 lines added, a second `--carry` added none - [what was seen](#2026-10-18-the-run-on-the-laptop), below. *The 40 lines the copy hides.* From the owner's terminal, outside the app: `python brain/heron_paths.py --carry` | About 40 audit lines added - the brain file only the real `%APPDATA%\Heron\audit` held - and the store named as LEFT, the home's own kept. A second `--carry` adds nothing |
| **HF3** | **RUN 2026-10-18: HALF PASS, HALF NOT RUN** - the brain's audit lands in the home and the copy did not change; no loads report was made, the loads re-run being postponed by the owner - [what was seen](#2026-10-18-the-run-on-the-laptop), below. *New writes land outside the copy.* After HF1, ask a chat for a loads report on a project, and anything else Heron answers | From the owner's terminal: the project's new run under `%USERPROFILE%\.heron\knowledge\projects` and new lines in `%USERPROFILE%\.heron\audit\audit-brain-<month>.jsonl`. **Negative:** the copy's `Heron\knowledge` and `Heron\audit` unchanged in count and size |
| **HF4** | **RUN 2026-10-18 in Revit 2024: FAIL - [row 5b-1100](../fragment-issues/section-5b-rows-401-425.md).** Revit runs with a temporary folder of its own, so the button looked where no chat writes and said no chat was running while four were - [what was seen](#2026-10-18-the-run-on-the-laptop), below. **FIXED IN CODE 2026-10-18 - [D-136](../decisions/D-136.md), [#657](https://github.com/Ajmalpshaik/Heron-AI/pull/657), NOT RUN IN REVIT; the re-press is owed:** both halves step out of a GUID-named temporary folder. The add-in rebuilt and deployed from that change for 2020, 2024, 2026 and 2027, then this row pressed again. *The Companion button, on the new folder - this is [BK14](group-bk.md) again.* The add-in rebuilt and deployed from this change; a Revit the OWNER starts (never one started from Claude's shell); a chat open with the Companion on; press **Companion** on the Heron tab | The chat's page opens in the browser. `%TEMP%\Heron\companion` holds the chat's `chat-<pid>.json`. On an add-in built from [D-136](../decisions/D-136.md)'s change, Revit's own GUID-named temporary folder gains no `Heron\companion` at the press. **Negative:** with no chat running, *"No Claude chat with Heron is running on this PC"*, and nothing opens |
| **HF5** | **NOT RUN 2026-10-18 - the office PC is retired, and the laptop, the only PC left, runs Claude only as the Store build** - [why, and what HF5 would need](#2026-10-18-the-office-pc-is-retired), below. *A PC whose Claude is NOT the Store build*, after the update and a Claude restart | The first chat carries `%APPDATA%\Heron\knowledge` and the brain's audit into `%USERPROFILE%\.heron`; `carried.json` names one old place; the old folder unchanged |
| **HF6** | **NOT PUT TO THE OWNER 2026-10-18** - HF3 has not fully passed; nothing in the copy was deleted - [what was seen](#2026-10-18-the-run-on-the-laptop), below. *Removing the old copies - only on the owner's word, never as a check.* | Not a check to run: the owner decides whether to delete the copy's `Heron\knowledge` and `Heron\audit` (or Reset the app) once HF1 to HF3 pass |
| ~~**HF7**~~ | **RUN, PASS 2026-10-18** - 42 files from both folders, the drill byte for byte, neither folder changed - [what was seen](#2026-10-18-the-run-on-the-laptop), below. *The default backup holds both folders - [row 5b-1069](../fragment-issues/section-5b-rows-401-425.md).* After HF1, from the owner's terminal outside the app, with neither `HERON_DATA` nor `HERON_BACKUP` set: `python tools/heron-backup.py backup`, then `python tools/heron-backup.py drill`, then `python tools/heron-backup.py list` | The backup lands in `%USERPROFILE%\.heron\backup\<stamp>` and names two folders - `%APPDATA%\Heron` (the add-in's config and audit) and `%USERPROFILE%\.heron` (knowledge, `knowledge\projects`, the brain's audit) - each `knowledge\*.db` skipped; the drill says both came back byte for byte; `list` still shows any backup taken in `%APPDATA%\Heron\backup` before. **The D-30 negative:** neither folder changed across the drill, and `%APPDATA%\Heron\backup` gained nothing |

### 2026-10-18: the office PC is retired

**The fact, from the owner, Ajmal PS, on 2026-10-18:** *"no office pc no more"*. The office PC - Revit 2020,
2024 and 2027 - is retired, and it was the PC HF5 named. The laptop is now the owner's only PC. Read there
the same day: one Claude, the Microsoft Store build `AnthropicPBC.Claude_2.31226.1.0_x64__fnn82j28hfe8t`, and
no Claude Code installed on its own - `Get-Command claude -All` returned nothing, and
`%USERPROFILE%\.local\bin\claude.exe` is not there.

- **HF5 - NOT RUN.** It needs a Claude that is not the Store build, and the only PC left runs Claude only as
  the Store build.
- **Claude Code installed on its own on the laptop would not, by itself, let HF5 run as written.** Reasoned
  from the code, not run: `old_homes()` in [`brain/heron_paths.py`](../../brain/heron_paths.py) finds each
  Store Claude's copy by its own `%LOCALAPPDATA%\Packages\AnthropicPBC.Claude_...\LocalCache\Roaming\Heron`
  path, from any process, inside the app or not. On the laptop that copy holds Heron's data (its 41 files,
  above), so a first chat from a Claude Code installed on its own would carry from two old places, not the
  one HF5 looks for. And the carry runs once per home: after HF1, `carried.json` is already there and a later
  chat carries nothing. HF5's look-for fits a PC with no Store Claude copy, and none is left.
- **Owed to Ajmal PS:** whether HF5 waits for such a PC, is rewritten for the laptop, or is dropped. Its
  look-for is not changed here.

### 2026-10-18: the run on the laptop

**Run by a Claude session on the owner's laptop, Ajmal PS at the PC**, after [#641](https://github.com/Ajmalpshaik/Heron-AI/pull/641)
merged at 05:04Z as `b3933e34` ([#638](https://github.com/Ajmalpshaik/Heron-AI/pull/638) closed unmerged, its content
carried inside #641). Times are the laptop's (+03:00) unless they end in Z. Store Claude `AnthropicPBC.Claude_fnn82j28hfe8t`.
"From outside" is the owner's Terminal panel, which runs outside the app; "from inside" is Claude's shell. Every
fingerprint is each file's relative path, size and sha256; the helper scripts were scratch, in `%TEMP%\heron-work`,
never in the repository.

**Before, 08:11 to 08:12.** The main checkout was brought level with `b3933e34` at about 08:08. `python
brain/heron_paths.py --where`, from inside and from outside, named the same three lines each: `HOME
C:\Users\ajmal\.heron` and two `OLD` - the copy's `LocalCache\Roaming\Heron`, then `C:\Users\ajmal\AppData\Roaming\Heron`.
`C:\Users\ajmal\.heron` did not exist (so no `carried.json`). The copy: **41 files, 11,979,200 bytes**, the same
fingerprint read from both sides. The real `%APPDATA%\Heron` from outside: 4 files - among them
`audit\audit-brain-202610.jsonl`, 40 lines, 7,118 bytes, which from inside is hidden behind the copy's file of
that name (inside, the same path shows 42 files).

**HF1 - PASS.** `carried.json` holds **one run**: `"at": "2026-10-18T08:15:46"`, `"how": "first use"`, pid 36420,
both old places as `sources`, `"copied": 39`, `"same": 40`, `"lines": 25118`, `"left": []`, `"failed": []` - read
from outside with `dir` and `type` at 10:35. The run came from a chat started at 08:15 from the main checkout after
it was brought level, not from the owner's restart: its note in `%TEMP%\Heron\companion` stopped at 10:33:51, when
the owner closed and reopened the Claude app (the app's process started 10:33:51). After the restart five MCP
servers started between 10:33:59 and 10:34:19 and `carried.json` still held that one run - **the second-chat
negative**. The home then held 41 files: 37 byte for byte the copy's; the two SQLite stores both pass
`integrity_check`, the project store with the copy's row counts table for table, and `global.db` with the copy's
571 `fragment_text` rows (its FTS shadow tables are smaller - a refresh rewrote that store at 10:21, so it is no
longer the carried file); every one of the copy's 25,134 brain lines is in the home's file; `knowledge\projects`
holds the loads project `8764c510-...-0000c160` - its `.db` and its `.loads` folder of 11 runs and take-offs. The
copy's `config\heron.config` is not carried, as D-135 says. A question asked from this session (`heron_lookup`,
*"how many ducts are in the model"*, `question=true`) wrote its `brain_lookup` line at 07:36:13Z into
`C:\Users\ajmal\.heron\audit\audit-brain-202610.jsonl`. **The D-30 negative:** the copy fingerprinted again at
10:35 from inside and from outside, after HF2, and at the end of the run - 41 files, every sha256 equal to 08:12.

**HF2 - PASS.** From outside: first, 40 of the real `%APPDATA%\Heron` brain file's 40 lines were not in the home.
`python brain/heron_paths.py --carry` printed `CARRIED 0 file(s) copied, 37 already the same, 40 audit line(s)
added`, then three `LEFT` - the copy's `knowledge\global.db`, the copy's project `.db`, and the real
`%APPDATA%\Heron\knowledge\global.db` (the store the installer made on 2026-10-17) - and *"Nothing in the old
places was changed or deleted."*; nothing `FAILED`. Afterwards none of the 40 was missing. A second `--carry`: `0
audit line(s) added`, the same three `LEFT`. Seen: the project `.db` is named `LEFT` on every `--carry`, not only
the store the look-for names, because a store carried through SQLite's backup is never byte for byte its source
(D-135, `carry_forward`'s comment) - expected, not a defect.

**HF3 - HALF PASS, HALF NOT RUN.** *PASS:* every brain line written since 05:15:56Z - the first after the carry -
is in the home's audit (25,118 lines carried, 25,158 by 10:36, 25,198 after HF2), and the copy's
`Heron\knowledge` and `Heron\audit` did not change in count, size or content. *NOT RUN:* the loads report. A loads
run keeps nothing while anything is asked (`heron_brain.building_loads`), and what it asks - design weather, set
points - is the modeller's to give (D-33); the owner has postponed the loads re-run to his HVAC test from zero,
and the small loads model ("Heron loads test") is not on the laptop. Owed on that run: its new run file under
`C:\Users\ajmal\.heron\knowledge\projects\<key>.loads`, and the copy unchanged.

**HF4 - FAIL: [row 5b-1100](../fragment-issues/section-5b-rows-401-425.md).** The add-in: Revit 2024, 2026 and
2027 held `48230c8c`'s, deployed by another session at 08:44 - Debug, add-in code equal to `b3933e34`'s; Revit
2020 held `ab77d5fb`'s (before D-135), and this session built `b3933e34` Release and deployed it with
`tools/deploy-addin.ps1 -RevitVersion 2020 -Configuration Release` from the owner's terminal at 10:38 - the
three Heron DLLs' sha256 equal to the build's. Every installed `Heron.Core.dll` holds `SharedDerived`. The owner
started Revit 2024.3 (pid 33576, 10:42:27), connected Heron, and pressed **Companion**: *"No Claude chat with
Heron is running on this PC, so there is no Companion page to open. Nothing was changed."* - while four chats'
notes, 1 to 2 s old with live pids, sat in `C:\Users\ajmal\AppData\Local\Temp\Heron\companion`. Revit runs with
`C:\Users\ajmal\AppData\Local\Temp\63835622-bdd8-4d57-a4a9-19a6afb83cd6` as its temporary folder, and the button
made and read `63835622-...\Heron\companion`, empty. The negative case was not needed: the positive failed. Not
pressed on 2020, 2026 or 2027.

**HF6 - NOT PUT TO THE OWNER.** HF3 has not fully passed. The copy's `Heron` folders are as they were: nothing in
them was deleted or changed by this run.

**HF7 - PASS.** From outside, with neither `HERON_DATA` nor `HERON_BACKUP` set: `python tools/heron-backup.py
backup` - *"Backed up 42 file(s), 6561 KB, to C:\Users\ajmal\.heron\backup\20261018-105122"*, 3 from
`%APPDATA%\Heron` and 39 from the home, the three `knowledge` stores skipped as derived; `drill` - *"verify: every
file matches its hash"*, *"compare: all 42 file(s) came back byte for byte"*; `list` - the one backup. **The D-30
negative:** the home (84 files with the backup) and `%APPDATA%\Heron` (4 files) fingerprinted before and after the
drill - the same; `%APPDATA%\Heron\backup` does not exist, before or after. Not exercised: `list` showing an older
backup in `%APPDATA%\Heron\backup`, since there was none.
