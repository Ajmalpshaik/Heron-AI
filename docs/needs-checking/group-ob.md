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
