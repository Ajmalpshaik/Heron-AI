# Needs checking — Group EP

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group EP - one install does everything: Heron's Python side after the add-in, and the Store-Claude refusal (2026-10-17)

The owner asked on 2026-10-17 for an installer that does the whole job in one run, so a new PC stops
needing two pip commands and `tools/heron-ready.py` by hand - the half of the seven-minute first chat on
the new laptop ([row 5b-378](../fragment-issues/section-5b-rows-376-400.md)) that Heron can close.
Signing is left for later, by his word the same day: Heron is not finished.

**What changed.** After the add-in goes in, both doors - `HeronInstaller.exe` and `heron-install` - run
[`PythonSetup`](../../platform/Heron.Installer/PythonSetup.cs) when a product whose manifest row says
`needsPython` went in (the AI connector; the tools do not ask for Python, Q-PE-2): it finds the plain
command `python` (3.11 or later), runs `python -m pip install --user --require-hashes` on
[`requirements-lock.txt`](../../requirements-lock.txt), runs `tools/heron-ready.py` to build the knowledge
store, then `tools/heron-ready.py --check` as the verdict. Each step runs only if the one before worked;
the whole output goes to `%TEMP%\Heron\python-setup.log`. The window says what Install also does before
it is pressed; `heron-install --no-python` leaves it out. Both doors refuse to run from inside a
Microsoft Store Claude app's private copy of AppData ([`ClaudeAppCopy`](../../platform/Heron.Installer/ClaudeAppCopy.cs),
[row 5b-495](../fragment-issues/section-5b-rows-401-425.md)). `heron-project.zip` now carries
`tools/heron-ready.py` and the lock ([row 5b-494](../fragment-issues/section-5b-rows-401-425.md)).

**What is measured and what is not run.** Measured in a Linux container on 2026-10-17: the lock installs
with `--require-hashes` into a fresh environment and `tests/test_mcp_serves.py` passes there; every
pinned package arrives as a hash-matched Windows wheel for Python 3.11 to 3.14 (`pip download
--platform win_amd64`); the installer, its window and `heron-install` build; `tests/Heron.Installer.TestHost`
runs every decision against fakes and starts the real `ProcessProgramRunner`. **Nothing here has run on
Windows**: not pip into a real user folder, not heron-ready there, not the window's new lines, not the
identity test inside a Store-built Claude.

**Build the installer from this branch first** - `python tools/build-release-assets.py --out dist` on a PC
with the .NET 10 SDK, or a `workflow_dispatch` rehearsal of `release.yml` - and unzip
`dist\heron-project.zip` into a NEW folder. Never run these on a working model; no Revit model is needed
at all except to see the tab in EP1.

| # | Run | Look for |
|---|---|---|
| **EP1** | A PC with Python 3.12 and **none** of Heron's packages (`pip list --user` shows no `mcp`). Close Revit, double-click `HeronInstaller.exe` in the new folder from File Explorer, tick a Revit and the AI Bridge connector, press Install | Before pressing: the *Heron's Python side* paragraph above Install. After: the add-in line per Revit, then each step said in turn while it runs, then four lines ending *heron-ready says READY*; `%TEMP%\Heron\python-setup.log` holds the pip and heron-ready output; `pip list --user` shows `mcp 2.2.0`, `cryptography 50.0.1`, `cffi 2.1.1`, `PyYAML 6.0.3`; `python tools/heron-ready.py --check` in the folder says READY; the Heron tab appears when Revit opens. Write down how long the Python half took |
| **EP2** | EP1's negative, on a PC or user where `python` answers nothing, or answers the Store shortcut | The add-in still installs; the Python line says no Python answered the plain command `python` (or that it is the Store shortcut) and names `winget install Python.Python.3.12 --scope user`; pip is never run (the log shows only the look); the run reports a failure, and Install can be pressed again |
| **EP3** | `heron-install` from a terminal opened outside Claude, in the new folder; then `heron-install --no-python`; then on a user whose `python` is 3.10, plain `heron-install` | The listing prints what the Python side will do before anything happens; exit 0 and READY; with `--no-python` the add-in only, no Python lines, exit 0; with 3.10, the Python line names 3.10 and asks for 3.11 or later, exit 2, add-in installed |
| **EP4** | **The owner's laptop, Store-built Claude.** From Claude's own shell: `heron-install --list`, then `heron-install` | `--list` lists and ends with the refusal; plain `heron-install` exits 1 with *Heron was not installed. This is running inside the Claude app...*, and nothing changed - no new file under `%APPDATA%\Autodesk\Revit\Addins` seen from the owner's own terminal, no new package in the copy's `LocalCache\Roaming\Python` - **the D-30 negative** |
| **EP5** | EP4's control on the same laptop: `heron-install` from the owner's own Terminal, outside Claude | Not refused; installs as EP3. Then, in a NEW Claude chat, `/mcp` says heron connected and `python tools/heron-ready.py --check` from Claude's shell says READY - Claude reading what was installed outside, through its overlay. Note what section 2a says about the copy's own packages |
| **EP6** | EP5 on a PC whose Claude is NOT the Store build (the downloaded installer, or Claude Code in a terminal): `heron-install` from Claude's shell | Not refused - there is no copy to be inside - and installs as EP3. The control that shows the refusal is not simply "inside Claude" |
| **EP7** | *Second route, by hand:* in the new folder, `python -m pip install --user --require-hashes -r requirements-lock.txt`, then `python tests/test_mcp_serves.py` on Python 3.12 and once on 3.13 | pip installs every line with no source build and no hash refusal; the suite passes on Windows - the measurement the lock's header says is missing |
| **EP8** | *Watching, not a proof.* EP1 with Revit open when Install is pressed | The window waits for Revit to close as before; the Python side starts only after the add-in went in, never before |

**Not checked by any row:** a PC whose Claude copy already holds Heron's packages at OTHER versions than
the lock - the install puts the pinned ones in the real folder, and which set Claude then loads through its
overlay is [row 5b-417](../fragment-issues/section-5b-rows-401-425.md)'s open question. EP5's note on
section 2a is where it would show.
