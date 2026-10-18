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
command `python` (3.11 to 3.14 - the range the lock was measured on), runs `python -m pip install --user
--require-hashes --only-binary=:all:` on [`requirements-lock.txt`](../../requirements-lock.txt), runs
`tools/heron-ready.py` to build the knowledge store, then `tools/heron-ready.py --check` as the verdict.
Finding Python and installing the packages each stop the run when they fail; the build never does - the
check runs whatever it did. The whole output goes to `%TEMP%\Heron\python-setup.log`. The window says what Install also does before
it is pressed; `heron-install --no-python` leaves it out. Both doors refuse to run from inside a
Microsoft Store Claude app's private copy of AppData ([`ClaudeAppCopy`](../../platform/Heron.Installer/ClaudeAppCopy.cs),
[row 5b-495](../fragment-issues/section-5b-rows-401-425.md)). `heron-project.zip` now carries
`tools/heron-ready.py` and the lock ([row 5b-494](../fragment-issues/section-5b-rows-401-425.md)).

**What is measured and what is not run.** Measured in a Linux container on 2026-10-17: the lock installs
with `--require-hashes` into a fresh environment and `tests/test_mcp_serves.py` passes there; every
pinned package arrives as a hash-matched Windows wheel for Python 3.11 to 3.14 (`pip download
--platform win_amd64`); the installer, its window and `heron-install` build; `tests/Heron.Installer.TestHost`
runs every decision against fakes and starts the real `ProcessProgramRunner`. **Nothing here has run on
a person's Windows PC**: not the window's new lines, not an install into a real Revit's add-ins folder,
not the identity test inside a Store-built Claude.

**What runs on Windows in CI, since 2026-10-17.** [`.github/workflows/windows.yml`](../../.github/workflows/windows.yml)
runs on every pull request that touches the installer projects, the lock, `tools/heron-ready.py` or
`tools/build-release-assets.py`, and on demand - on a GitHub `windows-latest` runner, with no Revit and no
person. Every suite there must exit 0; a 3 fails the job.

- **EP7's measurable half**, on Python 3.12 and on 3.13: `python -m pip install --user --require-hashes -r
  requirements-lock.txt` into the runner's real user folder, failing if pip builds anything from source;
  then `tests/test_mcp_serves.py`; then `tools/heron-ready.py --check`, which must exit 1 with exactly one
  FIX - *the Heron add-in is not installed for any Revit*. That is the true answer on a machine with no
  Revit, so section 7 is checked as a negative on a real `%APPDATA%`; a second FIX, a different one, or
  READY fails the job. heron-ready is not edited and no add-in is faked to make it pass.
- **`tests/test_installer_engine.py` on Windows**, where the host also checks
  `WindowsClaudeAppLook.SameEntry` against real folders: a folder and a junction to it answer `true`, two
  different folders `false`, a missing path `null`. `ProcessProgramRunner` starts `dotnet` and `cmd` there.
- **Both executables publish on Windows** through `build-release-assets.py`'s own `build_installers`, and
  `heron-install --help` exits 0.

First green run, every job above, on [PR #597](https://github.com/Ajmalpshaik/Heron-AI/pull/597):
[run 39035516875](https://github.com/Ajmalpshaik/Heron-AI/actions/runs/39035516875), 2026-10-17 -
on Python 3.12, heron-ready's section 3 loaded the server in 0.1 s and its one FIX was the missing add-in.

**EP7 stays a row**: CI installs from a checkout, not from an unzipped `heron-project.zip`, on a runner
rather than a modeller's PC. **EP1 to EP6, EP8 and EP9 still need the owner's PC** - each needs a Revit, the
window, a Store-built Claude, or a person to watch.

**Build the installer from this branch first** - `python tools/build-release-assets.py --out dist` on a PC
with the .NET 10 SDK, or a `workflow_dispatch` rehearsal of `release.yml` - and unzip
`dist\heron-project.zip` into a NEW folder. Never run these on a working model; no Revit model is needed
at all except to see the tab in EP1.

| # | Run | Look for |
|---|---|---|
| **EP1** | **RUN 2026-10-17 on the owner's laptop - PASS on every look-for, with all four Revits ticked and the add-in fetched from v0.1.0** - [the run](#the-run-of-2026-10-17-on-the-owners-laptop), below. A PC with Python 3.12 and **none** of Heron's packages (`pip list --user` shows no `mcp`). Close Revit, double-click `HeronInstaller.exe` in the new folder from File Explorer, tick a Revit and the AI Bridge connector, press Install | Before pressing: the *Heron's Python side* paragraph above Install. After: the add-in line per Revit, then each step said in turn while it runs, then four lines ending *heron-ready says READY*; `%TEMP%\Heron\python-setup.log` holds the pip and heron-ready output; `pip list --user` shows `mcp 2.2.0`, `cryptography 50.0.1`, `cffi 2.1.1`, `PyYAML 6.0.3`; `python tools/heron-ready.py --check` in the folder says READY; the Heron tab appears when Revit opens. Write down how long the Python half took |
| **EP2** | **RUN 2026-10-17 - PASS: both shapes through `heron-install`, and the no-Python shape through the window** - [below](#the-run-of-2026-10-17-on-the-owners-laptop). EP1's negative, on a PC or user where `python` answers nothing, or answers the Store shortcut | The add-in still installs; the Python line says no Python answered the plain command `python` (or that it is the Store shortcut) and names `winget install Python.Python.3.12 --scope user`; pip is never run (the log shows only the look); the run reports a failure, and Install can be pressed again |
| **EP3** | **RUN 2026-10-17 - PASS for plain `heron-install` and for `--no-python`. NOT RUN for Python 3.10 and 3.15: neither is on the laptop, and fetching one is the owner's to allow** - [below](#the-run-of-2026-10-17-on-the-owners-laptop). `heron-install` from a terminal opened outside Claude, in the new folder; then `heron-install --no-python`; then on a user whose `python` is 3.10, plain `heron-install`; and once more where `python` is 3.15 | The listing prints what the Python side will do before anything happens; exit 0 and READY; with `--no-python` the add-in only, no Python lines, exit 0; with 3.10, and again with 3.15, the Python line names the version found and the measured range 3.11 to 3.14, exit 2, add-in installed, pip never run |
| **EP4** | **RUN 2026-10-17 - PASS, the D-30 negative** - [below](#the-run-of-2026-10-17-on-the-owners-laptop). **The owner's laptop, Store-built Claude.** From Claude's own shell: `heron-install --list`, then `heron-install` | `--list` lists and ends with the refusal; plain `heron-install` exits 1 with *Heron was not installed. This is running inside the Claude app...*, and nothing changed - no new file under `%APPDATA%\Autodesk\Revit\Addins` seen from the owner's own terminal, no new package in the copy's `LocalCache\Roaming\Python` - **the D-30 negative** |
| **EP5** | **RUN 2026-10-17 - PASS from Claude's shell; a NEW chat's `/mcp` is owed to the owner; what section 2a said is [row 5b-1065](../fragment-issues/section-5b-rows-401-425.md)** - [below](#the-run-of-2026-10-17-on-the-owners-laptop). EP4's control on the same laptop: `heron-install` from the owner's own Terminal, outside Claude | Not refused; installs as EP3. Then, in a NEW Claude chat, `/mcp` says heron connected and `python tools/heron-ready.py --check` from Claude's shell says READY - Claude reading what was installed outside, through its overlay. Note what section 2a says about the copy's own packages |
| **EP6** | **NOT RUN 2026-10-17 - the laptop has no Claude that is not the Store build**, only `AnthropicPBC.Claude_...fnn82j28hfe8t` and the `claude.exe` it carries. **NOT RUN 2026-10-18 - the office PC is retired, and the laptop, the only PC left, runs Claude only as the Store build** - [why, and what would let it run](#2026-10-18-the-office-pc-is-retired), below. Needs a Claude that is not the Store build: on the laptop, Claude Code installed on its own, from its own installer and not the Store app. EP5 on a PC whose Claude is NOT the Store build (the downloaded installer, or Claude Code in a terminal): `heron-install` from Claude's shell | Not refused - there is no copy to be inside - and installs as EP3. The control that shows the refusal is not simply "inside Claude" |
| **EP7** | **RUN IN PART 2026-10-17 - as written it cannot be followed: `heron-project.zip` has no `tests/` folder.** The suite PASSED from a checkout of the same commit, outside Claude, on Python 3.12; 3.13 NOT RUN, not on the laptop - [below](#the-run-of-2026-10-17-on-the-owners-laptop). **3.13 still NOT RUN 2026-10-18 - the laptop is now the only PC, and it has no Python 3.13** - [below](#2026-10-18-the-office-pc-is-retired). *Second route, by hand:* in the new folder, `python -m pip install --user --require-hashes -r requirements-lock.txt`, then `python tests/test_mcp_serves.py` on Python 3.12 and once on 3.13 | pip installs every line with no source build and no hash refusal; the suite passes on Windows - the measurement the lock's header says is missing |
| **EP8** | **RUN 2026-10-17 - PASS, on a second run with an empty Revit 2026: the Python half started only after the add-in went in. The first run found [row 5b-1064](../fragment-issues/section-5b-rows-401-425.md), FIXED, and the second [row 5b-1068](../fragment-issues/section-5b-rows-401-425.md), open** - [below](#the-run-of-2026-10-17-on-the-owners-laptop). *Watching, not a proof.* EP1 with Revit open when Install is pressed | The window waits for Revit to close as before; the Python side starts only after the add-in went in, never before |
| **EP9** | **NOT RUN - LOCAL: a PC with Revit, the window, and a person to watch.** [Row 5b-1068](../fragment-issues/section-5b-rows-401-425.md)'s fix: tested in a cloud container against a fake Revit and as text, never drawn. Build the installer from this branch. With an empty Revit 2026 open, double-click `HeronInstaller.exe` and read the line above the report; close Revit, press Install, and read the line again once the run reports. Then once more pressing Install while Revit is still open and closing it during the wait, as EP8 did. Then, with two Revits open, open the window, read the line and close the window without installing | Before Install: *"Revit 2026 is open right now. Close Revit before installing..."*. Once the run reports, with Revit closed, both ways: *"Close Revit before installing..."*, naming no Revit as open. With two open: *"Revit 2026 and 2020 **are** open right now"* (the releases in whatever order Windows lists them), never *is* |

**Not checked by any row:** a PC whose Claude copy already holds Heron's packages at OTHER versions than
the lock - the install puts the pinned ones in the real folder, and which set Claude then loads through its
overlay is [row 5b-417](../fragment-issues/section-5b-rows-401-425.md)'s open question. EP5's note on
section 2a is where it would show.

### The run of 2026-10-17 on the owner's laptop

**The machine.** Windows 11 Home 10.0.26300. One Python: 3.12.10 at
`%LOCALAPPDATA%\Programs\Python\Python312`, plus Windows' Store shortcut in `WindowsApps`. Revit 2020, 2024,
2026 and 2027. One Claude: the Store build, `AnthropicPBC.Claude_2.31226.1.0_x64__fnn82j28hfe8t`. Before the
run, read from the owner's own terminal: the real `%APPDATA%\Python` held PyYAML 6.0.3 and nothing else - no
`mcp`; the Heron add-in was in all four Revits, a Debug build put there at 19:33 by `tools/deploy-addin.ps1`;
no knowledge store in the real `%APPDATA%\Heron`.

**The build.** `python tools/build-release-assets.py --out <work folder>\dist` from main at `ab77d5fb`: exit 0
in 3 min 34 s, 25 assets and the product list, `heron-project.zip` unzipped into a new folder under
`%TEMP%\heron-work`. Nothing in that folder is a build, so **both doors fetched the add-in from the published
release, v0.1.0 of 2026-10-07**, each zip checked against that release's `checksums.txt`: the installer and
its Python half are main's, the add-in they put in Revit is v0.1.0's. A modeller with today's download is in
exactly that place.

**How it was driven.** Commands were typed into the owner's Terminal panel, which runs outside the Claude app
and reads the real folders, not the copy. The window was started from `explorer.exe` or from that terminal,
and read and pressed through UI Automation. **The owner pressed Install himself** in the first window - three
times, about 22:09:55, 22:11:35 and 22:12:10 - and twice in the EP2 window, with every Revit still ticked, so
those runs went into all four Revits. Afterwards Revit 2020, 2024 and 2027 were given main's build from the
work folder's `dist` with `heron-install --from <dist> --releases 2020,2024,2027 --no-python --no-check`:
route 2, on Windows, exit 0 in 3.4 s, *"Nothing will be downloaded"*. The logs are on the laptop, under
`%TEMP%\heron-work\ep-1017`.

- **EP1.** Before the press the window showed the *Heron's Python side* paragraph above Install (read
  22:10:08). It ended at 22:11:32: *"'AI Bridge connector' installed for Revit 2020."* and the same for 2024,
  2026 and 2027, then *"Python 3.12.10 found at C:\Users\ajmal\AppData\Local\Programs\Python\Python312\python.exe."*,
  *"Heron's Python packages are installed for your user, at the versions in requirements-lock.txt."*,
  *"heron-ready built what a first chat would otherwise build while you wait."*, *"heron-ready says READY: a
  chat can start Heron on this PC."* While it ran it showed *"Installing Heron's Python packages - this can
  take a few minutes..."*, then *"Building..."* at 22:11:28 and *"Checking..."* at 22:11:30. **The Python half
  took about 69 s** - the last add-in written at 22:10:22, done at 22:11:32 - nearly all of it pip fetching 30
  wheels on a home line; heron-ready built the store, 571 tool cards, in 2.6 s. pip ran with `--only-binary=:all:`
  and `--require-hashes` and exited 0, so nothing was built from source and no hash was refused. The first
  run's `python-setup.log` was overwritten by the owner's next two presses, each READY; the third's shows pip
  *"Requirement already satisfied"* for all 30 and heron-ready's full output. From the owner's terminal at
  22:46: `pip list --user` shows `mcp 2.2.0`, `cryptography 50.0.1`, `cffi 2.1.1`, `PyYAML 6.0.3` and 30
  packages in all - the lock's 30 lines - and `python tools/heron-ready.py --check` in the folder says READY,
  exit 0. **The tab:** Revit 2026.5, started from Explorer at 22:27:29, logged *"Heron loaded. Revit 2026,
  add-in 0.1.0.0, pid 55840"* and, at 22:28:35, *"Connected from the ribbon"* - the owner pressing the Heron
  tab's button. Its warm-up failure in the same log is [row 5b-575](../fragment-issues/section-5b-rows-401-425.md),
  not this group's.
- **EP2.** Through `heron-install --releases 2026`, with PATH narrowed for that one run. With only the Store
  shortcut on it: the add-in *"done"*, then *"Heron's Python side was not set up. `python` on this PC is
  Windows' shortcut to the Microsoft Store, not a Python. Install Python for your user (winget install
  Python.Python.3.12 --scope user), or turn the shortcut off..."*; the log holds the look alone, exit 9009;
  `heron-install` exit 2. With no Python at all: *"No Python answered to the plain command `python`, which is
  the one Claude Code starts Heron with. Install Python for your user (winget install Python.Python.3.12
  --scope user), open a new window, and run the installer again."*; the log holds the look alone, *"(not
  started)"*; exit 2. **The window**, started with the no-Python PATH at 22:16:33: the owner pressed Install
  and the add-in went in; its Python half wrote the same look-only log at 22:17:12; Install was pressed again
  at 22:17:10, so it came back after the failed run. The window's own closing lines were not read - the owner
  closed it at 22:17:30.
- **EP3.** `--releases 2026`, on the owner's choice to leave his other Revits alone. `heron-install --list`
  printed the *"When a product that talks to a chat is installed..."* paragraph before anything ran, and no
  refusal - outside Claude there is none. Plain: exit 0, READY, 12.7 s in all, the Python half 5 s because the
  packages were already there. `--no-python`: exit 0, 6.8 s, no Python paragraph in the listing and no Python
  line after it. The 3.10 and 3.15 runs need a Python of that version as the plain `python`; none is on the
  laptop.
- **EP4.** From Claude's own shell: `heron-install --list` exit 0, the listing, then the refusal, then
  *"--list was asked for, so nothing was changed."* Then `heron-install --releases 2026` - plain but for the
  release, which the refusal is decided before - exit 1 with *"Heron was not installed. This is running inside
  the Claude app (the Microsoft Store build)..."* and nothing else. A fingerprint of every file's name, size
  and time, taken from Claude's shell before and after: the copy's `LocalCache\Roaming\Python` (36,764 files)
  and `%APPDATA%\Autodesk\Revit\Addins` (613 files) identical. The same look from the owner's terminal:
  identical, but for an empty `%TEMP%\Heron\download` made at 22:08:21 by the `--list` run, which says
  nothing was changed - a folder, no file.
- **EP5.** Outside Claude, EP3 is this run: not refused, installed, READY. Inside, after it, from Claude's
  shell: `python tools/heron-ready.py --check` in the folder said READY in 1.1 s, section 2a saying this run is
  inside the app; `tests/test_mcp_serves.py` PASSED, 51 s. **Section 2a, run outside by the installer itself,
  said** *"install from your own terminal, pinned to the versions in Claude's copy"*, with `mcp==2.3.0` in its
  pip line - while the installer had just put the lock's `mcp` 2.2.0 in the real folder. What Claude then sees
  of the two sets, file by file, is [row 5b-1065](../fragment-issues/section-5b-rows-401-425.md): the copy's
  file wherever both have one, three stray files from the real folder, and the real folder's version number.
  The *"Not checked by any row"* case above is this laptop, and it is now checked. **Owed:** `/mcp` in a NEW
  chat - the owner's one look.
- **EP7.** The suite is not in the zip, so it ran from this branch's checkout of the same commit, outside
  Claude, on Python 3.12.10, against the packages the installer had put in the real folder: PASSED - *"a real
  MCP SDK serves every tool Heron declares"* - in 22.5 s. The by-hand pip line was not run as a separate
  measurement: the installer's pip, the same line with `--only-binary=:all:` added, had installed all 30 from
  nothing minutes before. Python 3.13 is not on the laptop; `windows.yml` runs it on a runner.
- **EP8.** Revit 2026.5 open, as process 55840, with the owner's Project1 in it. The window was started
  outside Claude with only Revit 2026 ticked, and Install was pressed through UI Automation at 22:29:19. The
  window said *"Revit 2026 is open (process 55840). Close it and this will carry on by itself - nothing has
  been changed yet."* For the next 13 min 22 s nothing changed: `Addins\2026\Heron.addin` and
  `python-setup.log` kept their times, and no Python process started. That is the half of *"the Python side
  starts only after the add-in went in, never before"* that can be seen without the other. Revit was not
  closed - it held the owner's model - so at 22:42:41 the engine reached its ceiling and gave up, having
  changed nothing, and ended on the waiting sentence: [row 5b-1064](../fragment-issues/section-5b-rows-401-425.md),
  fixed on this branch.
  **The second run, after the owner had closed his model.** An empty Revit 2026.5 (process 55048, its start
  page) started from Explorer; the window started outside Claude with only Revit 2026 ticked; Install pressed
  at 23:37:31.9. For 20 s the window waited with the same sentence, `Heron.addin` and `python-setup.log`
  kept their times, and the installer started no Python. The empty Revit was then asked to close, and the
  order, sampled every half second: **Revit exited 23:38:13.8 - `Addins\2026\Heron.addin` written
  23:38:22.2 - *"Installing Heron's Python packages..."* 23:38:22.7** - *"Building..."* 23:38:24.5 -
  *"Checking..."* 23:38:35.1 - READY and Install back at 23:38:36.3. The Python half started only after the
  add-in went in. One Python process the watcher logged at 23:38:07.3 was this session's own helper script,
  run just before the watcher; the window's lines and the log place the installer's Python after the add-in.
  The line above the report still said *"Revit 2026 is open right now"* at the end -
  [row 5b-1068](../fragment-issues/section-5b-rows-401-425.md). Revit 2026 was then given main's build from
  `dist` like the other three, so all four end the run on it.

**Found along the way, and not a row of its own.** A Heron folder with nothing built in it fetches the newest
PUBLISHED release and puts it over whatever is installed - here, over a newer build of main - and the window
says only *"Already installed. Pressing Install replaces it"*. On a modeller's PC that is the point; on a PC
where Heron is developed it is a step back, unannounced. Every window run above did this.

**Measured on the way, for Q-63:** from Claude's shell, a write into a folder that already exists on the real
disk lands on the real disk, not in the copy - [row 5b-1066](../fragment-issues/section-5b-rows-401-425.md).

### 2026-10-18: the office PC is retired

**The fact, from the owner, Ajmal PS, on 2026-10-18:** *"no office pc no more"*. The office PC - Revit 2020,
2024 and 2027 - is retired. The laptop is now the owner's only PC, so a row of this group that is still owed
is owed there.

**What the laptop has, read on 2026-10-18** by a session opened there to run EP6 on the office PC, which
stopped at its check of which PC it was on: computer name `AJMAL_PS`; Revit 2020, 2024, 2026 and 2027;
`Get-AppxPackage *Claude*` answers `AnthropicPBC.Claude_2.31226.1.0_x64__fnn82j28hfe8t`, under
`C:\Program Files\WindowsApps` - the Microsoft Store build; and no Claude Code on its own - `Get-Command
claude -All` returned nothing, and `%USERPROFILE%\.local\bin\claude.exe` is not there.

- **EP6 - NOT RUN.** It needs a Claude that is not the Store build, and the only PC left runs Claude only as
  the Store build. **What would let it run:** Claude Code installed on its own on the laptop - its own
  installer, not the Store app - then EP6's steps as written, `heron-install` from that Claude's shell.
  Nothing of that has been tried.
- **EP7 - its Python 3.13 half NOT RUN.** EP7 runs outside Claude, so it does not wait on a Claude; it waits
  on a Python 3.13 on a person's PC, and the laptop has none ([the 2026-10-17 run](#the-run-of-2026-10-17-on-the-owners-laptop)).
  `windows.yml` runs 3.13 on a runner, which is not a person's PC. Not tried here.
