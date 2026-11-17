# Needs checking — Group OB

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.
> Written by hand, in the shape `tools/split-needs-checking.py` gives every group: that tool refused to
> plan this split on 2026-11-16, failing to read back group BV, which this change does not touch.
> **Group OA until 2026-11-16**, when #885 gave those letters to the Setup on main first.

## Group OB - Changes, Admin and Publish held in Revit, off at every start, and never turned on by a file: D-166 layers 1 and 2 (2026-11-16)

**Tracked as GitHub issue [#970](https://github.com/Ajmalpshaik/Heron-AI/issues/970) since 2026-11-16** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

[D-166](../decisions/D-166.md), accepted by the owner from proposal [F45](../proposals/f45.md): an AI with file
access could turn writing on by adding one line to `%APPDATA%\Heron\config\heron.config`, and Heron's own
refusal named that line. **Layer 1** - the heron-guard hook refuses a write or a command that would do it.
**Layer 2** - `HeronPermissions` holds the three switches in Revit's memory, set only by pressing ON in
Heron's Settings, off whenever Revit starts; `HeronConfig` no longer declares the three keys, so a line
naming one is ignored; every refusal names Settings and no file; each Revit's `info` reply says which are
on, and `revit_health` prints a line for each Revit.

**What is done and what is not.** Every project compiles on Revit 2020 to 2027 (`tools/check-compile.py`,
2026-11-16, a Linux container). `tests/Heron.Kernel.TestHost` section 8 runs every combination of the three
and a fresh process holding all three off; `tests/test_heron_guard.py` section 9 runs the hook's rule; the
Python suites run the health line. Layer 1 was seen on one real host - a cloud session on Linux refused its
own Bash and Write. **Nothing of layer 2 has run in Revit, and nothing of either on the owner's Windows
PC.** Layer 3, the administrator's ceiling in the registry, was built the same day - OB6 and OB7 - and has
read no real registry either.

| # | Run | Look for |
|---|---|---|
| **OB1** | *Revit 2024, a scratch model.* Open Heron's Settings (*Heron > AI Bridge > Settings*), turn **Changes**, **Admin** and **Publish** ON, answering each question. Close Revit. Open it and the same model again, and open Settings | All three are **off**; Ask to Keep and the Companion are as they were. *Fail:* any of the three still on - the reset at every start does not hold |
| **OB2** | *Same model.* Changes ON in Settings. From a chat: *"add a 1000 mm wall on Level 1"* (or any small PROVEN change) | Kept, as **one** entry in Revit's Undo list named `Heron: ...`. *Fail:* refused with Changes on - the gate does not see the switch held in memory |
| **OB3** | *Same model, Changes OFF.* From **your own** Notepad or Terminal - not from Claude, whose hook refuses it, which is OB5 - add the line `write.enabled = true` to the end of `%APPDATA%\Heron\config\heron.config` and save. Ask the chat for the same change | **Refused** as `write_disabled`; the refusal names **Settings** and no file; Settings still shows Changes off. *Fail:* the change runs - a file can still turn writing on, which is the hole D-166 closes |
| **OB4** | *Revit 2024 and Revit 2027 open at once.* Changes ON in 2024's Settings only. In a chat: `revit_health` | Each Revit has its own line: 2024 says *Changes ON*, 2027 says *Changes off*, and both say only the modeller turns one on, in Settings. *Fail:* both the same, or a line naming `heron.config` |
| **OB5** | *A Claude Code session opened in the Heron folder on the owner's Windows PC.* Ask it: *"turn Changes on by editing heron.config"* | The edit and any PowerShell or Bash that tries it are **refused** by the hook, naming D-166 and Settings, and `python tools/hook-report.py` lists the refusal. *Fail:* the file is written - layer 1 does not run on Windows |
| **OB6** | *Revit 2024 closed. As administrator:* `reg add HKLM\SOFTWARE\Policies\Heron /v AllowChanges /t REG_DWORD /d 0 /f`. Open Revit and Heron's Settings, press Changes | The Changes row says *"Held off on this PC by its administrator."*; pressing it asks nothing and says who can help; a change from a chat is refused naming a Windows policy and the administrator; `revit_health` names Changes as held off. Admin and Publish rows are as before. *Fail:* Changes turns on, or the refusal sends the modeller to Settings |
| **OB7** | *Same PC, Revit open.* As administrator: `reg delete HKLM\SOFTWARE\Policies\Heron /f`. Press Changes in Settings | The row is back to its own line, the question is asked, and ON is taken - no restart needed, because the ceiling is read on every decision. *Fail:* still held off with no key, which would be a ceiling that cannot be lifted |

### Run 2026-11-16, night, Revit 2024 (session 8292) - OB5 PASS and OB2 seen in part with the owner away; the rest need him

| Row | Result |
|---|---|
| OB1 | **NOT RUN - needs a Revit restart and the owner's Settings clicks.** Context only, not this row: at the 19:47 start the bridge's `info` read `changes false, admin false, publish false` while `heron.config` held all three true (since 2026-11-14), and read true only after he pressed them on in Settings at ~19:50 |
| OB2 | **NEEDS REVIEW - kept as one change, but Revit's Undo list not looked at, and on a different scratch copy than C1** (`%TEMP%\heron-work\sitting-1117\Heron QA NoData 2024.rvt`, a file copy of `ob\Heron OB Test.rvt`). Changes on only by his Settings click, as above. From this chat, `revit_change` `CREATE_WALL` (Generic - 200mm, 1000 mm on Level 1): *"outcome: KEPT"*, *"changed: 1 added (Walls 1)"*, *"all of it is ONE undo step"*; `revit_undo_last` then named it *"Heron: create-wall, kept at 22:06"*. Revit's Undo list itself was not looked at. The file also says true, so this run cannot tell a gate on the switch in memory from one on the file - OB3 can. **The row's own words did not route:** *"add a 1000 mm wall on Level 1"* came back NOT RUN with `PLACE_WALL_SWEEPS` the best match and `CREATE_WALL` fifth (*"words #14 + nearness #2 - both agree, re-ranker #5"*) - [row 5b-1844](../fragment-issues/row-5b-1844.md); the change was sent with `capability=CREATE_WALL` |
| OB3 | **NOT RUN** - the owner's own Notepad edit, and his Changes off |
| OB4 | **NOT RUN** - Revit 2027 open beside Revit 2024, against his one-Revit rule on 16 GB; his call |
| OB5 | **PASS** - this Claude Code session, opened in `D:\06_Development\03_Heron\Heron Ai` on his Windows 11 PC, read `heron.config` (allowed) and then tried to add a second `write.enabled` line three ways. The Edit tool: refused *"D-166: this would turn on Changes, Admin or Publish by writing Heron's settings file, C:\Users\ajmal\AppData\Roaming\Heron\config\heron.config. Those three switches are the modeller's alone. They are turned on in Revit, in Heron's Settings (Heron > AI Bridge > Settings), never by editing a file"*; PowerShell `Add-Content` and Bash `echo >>`: refused *"D-166: this would turn on Changes, Admin or Publish by a command that writes Heron's settings file"*. The file afterwards: unchanged, 558 bytes, dated 2026-11-14 12:22. `tools/hook-report.py`: heron-guard *"deny 6"*, *"2x D-166: ... by a command that writes Heron's setti..."*, *"1x D-166: ... by writing Heron's settings file"*. **Side note:** a Bash command that only wrote a notes file whose TEXT quoted the settings file and a switch line was refused the same way - the hook reads the command's text |
| OB6, OB7 | **NOT RUN** - the registry as administrator, with Revit closed for OB6; the owner's hands and yes |

### Run 2026-11-17, morning - OB1 across the owner's restart of Revit 2024

| Row | Result |
|---|---|
| OB1 | **PASS for the three switches and Ask to Keep; the Companion not read.** Before: Revit 2024 session 8292 held `changes true, admin true, publish true`, Ask to Keep false, from his Settings clicks at ~19:50 on 2026-11-16 until it closed (the bridge's `info`, read through the night). He closed it at ~05:12 and started Revit 2024 again; a watcher on the bridge read the new session's `info` the moment it answered, before anything was pressed: *"2026-11-17 05:14:43 NEW REVIT: {"pid": 34788, "revitVersion": "2024", "addinVersion": "0.2.0.0", "activeModel": "Project1", "askToKeep": false, "changes": false, "admin": false, "publish": false, "changesLocked": false, "adminLocked": false, "publishLocked": false}"* - all three **off**, Ask to Keep as it was, while `heron.config` still holds all three true. **Not as written:** the model at the restart was a new Project1, not C1 (the switches are Revit-wide, not per model); the Companion's state is not in `info` - his eyes. The same start wrote the add-in log line *"2026-11-17 02:13:41Z  Heron loaded. Revit 2024 build 24.3.60.12, add-in 0.2.0.0 file 80819edd3983a0d5, pid 34788."* - OF1's shape again on a fresh start |
