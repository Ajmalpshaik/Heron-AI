# Needs checking — Group OA

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group OA - the Setup a modeller downloads: one file, per user, every Revit year present, uninstall from Installed apps (2026-11-16)

What [D-165](../decisions/D-165.md) built: [`platform/Heron.Setup`](../../platform/Heron.Setup/README.md) and
[`tools/build-setup.py`](../../tools/build-setup.py), `heron-install --uninstall` and `--add-to-claude`
([`ClaudeCodeEntry.cs`](../../platform/Heron.Installer/ClaudeCodeEntry.cs)), and in
[`tools/deploy-addin.ps1`](../../tools/deploy-addin.ps1) the second-copy check and the put-back of a replace that
did not finish. The design is [the work note](../work-notes/plans/windows-setup-2026-11-16.md).

**The PC.** The owner's laptop: Revit **2020, 2024, 2026 and 2027** present (`Test-Path "C:\Program
Files\Autodesk\Revit <year>\Revit.exe"`, 2026-11-16), each with a Heron install already. **2021, 2022, 2023 and
2025: NOT RUN on any row below - no Revit of that year on this PC.** Its Claude app is the Microsoft Store build,
so the Setup is run by the owner from File Explorer, never from a chat (Q-63); the session reads what it did.

**Before any row that installs:** each `Addins\<year>\Heron*` folder and manifest copied and hashed into
`%TEMP%\heron-work\setup-test-backup` (a folder both the owner's terminal and the chat see, D-136), and
`heron.config` and `~/.claude.json` hashed. Those copies are how his own installs come back (OA10).

| # | Run | Look for |
|---|---|---|
| **OA0** | *No Revit, no owner - done 2026-11-16 in a scratch folder.* `tools/deploy-addin.ps1 -RevitVersion 2020 -Configuration Release` with `APPDATA` and `LOCALAPPDATA` pointed at the session's scratchpad: a first install; a second manifest carrying Heron's add-in id in braces and lower case; another vendor's manifest; a redeploy with the installed assembly held open, sharing reads, as Revit holds it | **PASS, recorded.** First install: 29 files and the manifest. Second copy: refused, *"already has another manifest that loads 'AI Bridge connector'"*, the folder's hash unchanged. Other vendor: deployed. Held open: *"Could not fully remove ... NOTHING FURTHER WAS CHANGED."* then *"The install that was there before has been put back by itself: 29 file(s), and its assembly reads the same as when it was kept."* - 29 files, the same hash, the manifest there. **The first try of the put-back FAILED that last case** (17 of 29 files: a folder-wide `Copy-Item` stops at the open file even when told to carry on) and was rewritten file by file before this pass. Not covered: a first install that fails half-way (the take-away path) |
| **OA1** | *The Setup builds.* `python tools/build-setup.py --dist dist` on the owner's PC, Inno Setup installed for his user | `HeronSetup-<version>-unsigned.exe` made, its size and SHA-256 printed; with no Inno Setup the tool exits 3 and says NOT RUN |
| **OA2** | *Install, Revit closed.* The owner double-clicks the Setup in File Explorer; the Claude tick left on, Claude closed | **No UAC prompt at any point.** Windows names the publisher as unknown (unsigned, D-165 item 6); the report page says *"Heron is installed"* and names 2020, 2024, 2026, 2027; Heron in Settings > Apps > Installed apps |
| **OA3** | *Read back, per year.* The session reads each `Addins\<year>\Heron` | The runtime read out of the deployed assembly matches the [skill](../../.claude/skills/revit-version-support/SKILL.md)'s table - net472, net48, net8.0-windows, net10.0-windows; every file matches the payload zip |
| **OA4** | *The owner's switches.* `heron.config` and the data folders, hashed after OA2 | **Hash unchanged** - Changes, Admin, Publish and Ask to Keep exactly as before; `%USERPROFILE%\.heron` not reduced |
| **OA5** | *Each Revit, one at a time (16 GB).* The owner starts 2020, then 2024, then 2026, then 2027 | Each journal: `API_SUCCESS { Starting External Application: Heron AI`, no *"cannot run the external application"*; the Heron tab |
| **OA6** | *Revit open.* Revit 2024 open, the Setup run again | Before Install: *"Close Revit first ... Open now: ...Revit 2024..."*, and nothing installed until it is closed. Revit never closed by the Setup |
| **OA7** | *Claude Code.* After OA2, a new chat in a folder that is NOT the Heron folder | `/mcp` lists `heron` connected; `~/.claude.json.heron-backup-<time>` beside the file; no other key changed (`mcpServers` holds what it held, plus `heron`). With Claude OPEN during the Setup instead: *"Heron was NOT added to Claude Code: Claude is open"*, and the file's hash unchanged |
| **OA8** | *Uninstall.* Settings > Apps > Installed apps > Heron AI > Uninstall, Revit closed | First, heron-install's own sentence naming every product and each Revit year it is on, and *Remove it now?*; on Yes, every year's Heron folder and manifest gone, the program folder gone, the Installed-apps entry gone, `heron` out of `~/.claude.json` with a backup beside it; `heron.config` and `.heron` hashes unchanged. **Record which came first, that sentence or Windows' own confirmation** - Inno Setup's documentation does not say |
| **OA9** | *Uninstall with Revit open* | Refused before anything is removed, naming the open Revit |
| **OA10** | *Put his own installs back.* The copies taken before OA2 | Each year's folder and manifest hash matches the *before*; the owner's word on whether to keep the Setup's build instead |
| **OA11** | *Signed.* Only once a certificate exists - **NOT RUN, D-165 item 6** | `Get-AuthenticodeSignature` says Valid for the Setup, its uninstaller, `HeronInstaller.exe` and `heron-install.exe`, each timestamped |
