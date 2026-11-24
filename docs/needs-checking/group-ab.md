# Needs checking — Group AB

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group AB - Stages 3 and 4 of the installer: the engine, and the window nobody has seen 2026-09-21

**Tracked as GitHub issue [#972](https://github.com/Ajmalpshaik/Heron-AI/issues/972) since 2026-11-16** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Everything the installer DECIDES is tested. Nothing it DOES has run.** 79 checks pass against a fake
Revit and a fake disk; all 13 projects compile on all eight releases with 0 warnings. **No file has been
written, no Revit has been looked for, no PowerShell has executed and no window has been drawn.** A
compile proves the API agrees. It does not prove an install.

**Three pieces have never run one line**, and all three are the pieces that touch Windows:

| | |
|---|---|
| `PowerShellRevitEnvironment` | asks `tools/HeronRevit.ps1` which Revit is installed, which is open, and where its add-ins go |
| `DeployScriptDeployer` | runs `tools/deploy-addin.ps1` |
| `InstalledProductsOnDisk` | looks in the Addins folder to answer whether a product is already there |

**And `tools/deploy-addin.ps1` itself has changed since it was last proved.** Its rollback proof of
2026-09-19 (`Group Z`) is **STALE for the current file** - `AA7` and `AA8` above are that debt, and they
come first: an engine driving a deploy script that does not work proves nothing about the engine.

Build the window and run it with:

```powershell
dotnet build platform\Heron.Installer.App -p:EnableWindowsTargeting=true
.\platform\Heron.Installer.App\bin\x64\Debug\HeronInstaller.exe
```

**`AA7` and `AA8` above come first.** They prove the deploy script still works after being generalised,
and the window does nothing but drive that script - so a FAIL there makes every row below unreadable.

| ID | Do this | Pass looks like |
|---|---|---|
| ~~**AB1**~~ | Run `HeronInstaller.exe`. — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | **FAILED, FIXED, AND THE RE-RUN PASSED - 2026-09-21 on the owner's PC.** |
| ~~**AB2**~~ | Look at the rows that cannot be ticked — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | **PASSED 2026-09-21 on the owner's PC** |
| ~~**AB3**~~ | **THE ONE THAT MATTERS MOST.** Add a product to `platform\heron-products.json` by hand - a new id, a new `addInId`, `state` SHIPPED - and **reopen the window without rebuilding anything** | **PASSED 2026-09-21 on the owner's PC** - the verdict is in *Group AB was RUN*, below. *It was to look like:* **The new row is there.** This is [R-3](../work-notes/plans/plugin-extension/01-requirements.md) and it is the reason the whole design is shaped this way: adding Heron Structure next year must be a line in a file. If this fails, the installer has a product list written inside it and the design has not been built. Undo the edit afterwards |
| ~~**AB4**~~ | Install the AI Bridge, then **reopen the window** | **PASSED 2026-09-21 on the owner's PC** - the verdict is in *Group AB was RUN*, below. *It was to look like:* Its row reads **`Installed`** rather than `--`, and **stays tickable**. Then delete its folder from `%APPDATA%\Autodesk\Revit\Addins\2024\` by hand and reopen again: it reads `--`. The state is read from the disk every time, never remembered - [R-2](../work-notes/plans/plugin-extension/01-requirements.md) |
| ~~**AB5**~~ | **Leave Revit open**, tick a product, press Install | **PASSED 2026-09-21 on the owner's PC** - the verdict is in *Group AB was RUN*, below. *It was to look like:* The window **does not freeze**, and it says which Revit is open and that it will carry on by itself. **Close Revit without touching the window.** The install then continues on its own and reports per product per release - [R-38a](../work-notes/plans/plugin-extension/01-requirements.md). **Nothing must have been changed before Revit was closed** |
| **AB6** | Tick **the tools only, without the AI Bridge**, and install | The tools install and **no bridge does** - [R-34](../work-notes/plans/plugin-extension/01-requirements.md), a site modeller who wants Heron's tools and refuses the AI. This depends on `AA2` passing first: if two add-ins cannot build one tab, this row is asking about a shape that does not exist |
| ~~**AB7**~~ | Press Install with nothing ticked, and again with no Revit version ticked — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | **PASSED 2026-09-21 on the owner's PC** |
| **AB8** | **THE ONE THAT HANGS RATHER THAN FAILS.** Make a deploy go wrong loudly while the window is watching - the simplest is to install for a release, leave **that** Revit open, and press Install - then do it again with a product whose build folder is missing. Watch the window through both | **It comes back with a sentence, every time, and never just stops.** Until 2026-09-21 the installer read one pipe to the end and then the other, which deadlocks the moment the unread one fills - and the 600-second ceiling could not fire, because the thread never reached it. Measured on Linux: **200 KB on standard error never returned at all**. Both streams are drained at once now and `tests/test_installer_adapters.py` holds the shape, but **that is a text check on C# from a machine with no PowerShell**. This is the run. Also check the release list is right at all: if the window says **no Revit was found** on a PC that has one, the environment answer is being parsed with something else mixed into it. Added 2026-09-21 with [row 5b-78](../FRAGMENT-ISSUES.md); **never run** |
| **AB11** | **THE FOLDER LIST, READ FOR REAL - [Q-58](../open-questions/tier-2.md).** Since 2026-11-16 no install script spells a folder: `HeronPaths` writes the Addins folder and the install backup to `platform\heron-folders.json`, and `Get-HeronFolder` in `tools\HeronRevit.ps1` reads it. With every Revit closed, in **Windows PowerShell 5.1** in the Heron folder: (1) `.\tools\deploy-addin.ps1 -RevitVersion 2024`, then the same with `-Rollback`; (2) open `HeronInstaller.exe` and look at the AI Bridge row; (3) rename `platform\heron-folders.json` aside, run (1) and open the window again, then put the file back; (4) as an administrator, copy the deployed `Heron.addin` into `C:\ProgramData\Autodesk\Revit\Addins\2024\`, run (1), then delete the copy - and on a PC with Revit 2027 the same into `C:\Program Files\Autodesk\Revit\Addins\2027\` with `-RevitVersion 2027`; (5) `python tools\heron-ready.py`, then `heron-install --list` | (1) It deploys into `%APPDATA%\Autodesk\Revit\Addins\2024\Heron`, prints - when a bridge was already installed - *"kept the install being replaced in ...\AppData\Local\Heron\install-backup\2024\Heron"* - **the same two folders as before**, now read from the list - and `-Rollback` finds that backup and puts it back. (2) The window lists this PC's Revits and reads the bridge as `Installed` for 2024, as in AB4: its Addins folder comes through the same list. (3) **The deploy stops before changing anything**, saying *"Heron's folder list is not at ..., so this cannot tell where 'RevitAddins' is, and nothing was changed. The Heron files are incomplete - fetch them again."*; the disk is unchanged. The window still lists the Revits and does **not** say a Revit of unknown release is open - each folder it could not read reads as not installed, and pressing Install comes back with that sentence, or its last line, rather than a build or an open-Revit reason (`PlainReason` passes an unknown refusal's last line through). (4) **added 2026-11-16, the all-user folders through the same list:** each run refuses before changing anything with *"Revit 2024 already has another manifest that loads ..."* (2027 for the second) naming the copy's path - `Get-RevitAllUserAddinsFolders` now asks the list for `RevitAllUserAddinsCommonApplicationData` and `RevitAllUserAddinsProgramFiles` instead of spelling them - and with the copy deleted, (1) deploys as before. (5) **[Row 5b-1746](../fragment-issues/row-5b-1746.md), the same list read by Python and the installer:** heron-ready's section 7 names the Revits Heron is installed for, as before, now from the folder Windows names for ApplicationData rather than the APPDATA variable; the `Into` line reads `ApplicationData\Autodesk\Revit\Addins\<release>\  (per user - no administrator needed)`, as the window's Install location does on hover. `tests/test_heron_folders.py` holds the scripts' half as text on Linux, where no PowerShell ran, and nothing but this row holds the window's; added 2026-11-16, **never run** |

**What a FAIL on AB3 means, written down before the run.** Every rule about what is offered lives in
`platform/Heron.Installer/InstallerScreen.cs` and is tested there; the window is supposed to hold
nothing but layout. `tests/test_installer_window.py` fails if a product id appears in the window's
source. So a FAIL here is not a typo - it would mean the manifest is not being re-read, and the fix is
in how the window is opened rather than in what it draws.

**Nobody may mark Stage 3 or Stage 4 done from a compile, from this file, or from the tests.** The word
for what exists today is **BUILT**.

### Group AB was RUN on the owner's PC - 2026-09-21. SIX ROWS PASS, ONE FAILED AND WAS FIXED, AB6 IS BLOCKED

**Stages 3 and 4 are PROVEN, with one row that is blocked rather than passed.** The window has been
seen, and **all three pieces that had never run one line have now run**: `PowerShellRevitEnvironment`
found the three Revit releases on this PC, `InstalledProductsOnDisk` read the Addins folder, and
`DeployScriptDeployer` **actually installed the AI Bridge** and Revit then loaded it.

**Where it was run.** Same machine as Group AA - 1920x1080 at **100% scaling**, which matters because
the one failure was a layout one and this is the plainest setting there is, not an exotic one.

| ID | Verdict | What was actually seen |
|---|---|---|
| AB1 | **FAILED, FIXED, RE-RUN PASSES** | The window appeared and read cleanly. The three releases on this PC were listed and all ticked; `Heron` carried **two ticks under it** and every other product was one tick, whole tab. **But the failure the row told us to watch for happened: text was cut off.** `heron-products.json` says *"...The three buttons that exist **today.**"* and the window drew *"...The three buttons that exist"*. **No ellipsis, and the truncated line still read as a finished sentence**, so nobody would know a word was missing. **Neither escape existed**: `Width = 620` with `ResizeMode.CanMinimize`, so it cannot be dragged wider, and the tooltip carries `WhyNot`, not the description. **Fixed the same day** by giving the tick box a wrapping `TextBlock` instead of a bare string - which is what every other block in that file already did. Widening to a bigger number was rejected: it would clip again, just as silently, the day a product with a longer description is added, and **R-3** says that is a line in a file rather than a change in the window. Re-run: the line wraps and `today.` is there. [the failure](../proof/AB1-installer-window-FAIL-text-cut-off.png), [after the fix](../proof/AB1-installer-window-fixed.png) |
| AB2 | **PASS** | Each row that cannot be ticked is greyed **and prints its reason underneath**, not only in a tooltip - R-10. *"Not ready yet. It is being built and tested, and installing it now would put a button in your ribbon that does nothing."* for the two Stage 2 throwaways, and *"Not built yet. It is in the plan, and it will appear here on its own when it is ready - this window reads the list rather than carrying it."* for Heron MEP. **None of them says SHIPPED, PROVING or PLANNED** |
| AB3 | **PASS - AND THIS IS THE ROW THE WHOLE DESIGN RESTS ON** | A `heron-structure` product was added to `platform\heron-products.json` by hand, with a fresh `addInId` and `state: SHIPPED`. The window was **reopened with no rebuild** and the new row was there - **`Heron Structure    Beams, columns and connections.`**, tickable rather than greyed. **The exe was last written at 19:22:39 and the manifest edited at 19:23:40**, so the running program predates the edit and nothing was recompiled. `Program.cs` walks up from the exe to find the live file rather than carrying a copy. **R-3 holds: adding Heron Structure next year is a line in a file.** The edit was undone and the file is byte-identical (same SHA-256, git clean). [proof](../proof/AB3-new-product-without-rebuild.png) |
| AB4 | **PASS** | With the bridge installed the row read `Installed` and stayed tickable. Its folder was then deleted from `Addins\2024\` **by hand** and the window reopened: the row changed to **`Installed for Revit 2020 and 2027`** - it had dropped 2024 from disk, not from memory. The throwaway tools, which were installed for one release only, went to **`--`**. **R-2 holds: state is read from the disk every time.** Two extras fell out of the same look: the `heron-structure` row was **gone**, so the manifest is re-read in both directions; and both `.addin` manifests were still on disk while both rows reported not-installed, which is the *"BOTH, not either"* rule working. [proof](../proof/AB4-state-read-from-disk.png) |
| AB5 | **PASS, AND IT INSTALLED FOR REAL** | With Revit 2024 open, ticking the AI Bridge and pressing Install gave: *"Revit 2024 is open (process 10828). Close it and this will carry on by itself - nothing has been changed yet."* It **named the release and the process**, the window kept responding, Install greyed itself, and the disk confirmed **nothing had been written**. Revit was then closed **without touching the window**, and the install **carried on by itself** and reported per product per release. The first attempt reported honestly that it could not install - *"'AI Bridge connector' was not installed for Revit 2024. dotnet build -c Release -p:RevitVersion=2024"* - because the window drives the deploy script with `-c Release` and only a Debug build existed. **That is the window reporting a real condition with the exact command to fix it, not a defect.** After that build, Install gave *"'AI Bridge connector' installed for Revit 2024."*, and Revit started on it: `Heron` tab, `AI Bridge` panel, bridge answered `pong <- Revit 2024, session 31228`. **The engine drove the script and the script installed something Revit loaded.** The screenshot below is a second start of that same install, `session 17040`, with `Bridge Status` open on it. [waiting](../proof/AB5-waits-while-revit-open.png), [carried on](../proof/AB5-continued-after-revit-closed.png), [installed](../proof/AB5-installed-by-the-window.png), [in Revit](../proof/AB5-installer-installed-addin-in-revit.png) |
| AB6 | **NOT RUN - BLOCKED, and the block is correct behaviour** | The row asks for the tools to be ticked **without** the AI Bridge. The only tools product that exists is the Stage 2 throwaway, whose state is `PROVING`, and the manifest says in as many words that **an installer MUST NOT offer one**. The window obeys: the row is greyed, and clicking it was tried and **left it unticked**. So this row cannot be run until `heron-tools` is a real product at `SHIPPED`. **The manifest was deliberately NOT edited to force it** - that would be rigging the test rather than running it. **What is already known**: `AA2` proved the *shape* by hand, so what is still unproven is only the **window's** ability to install tools without the bridge, and `AB3` proved the window follows the manifest with no rebuild, so the row should become tickable on its own the day the state changes. [proof](../proof/AB6-tools-row-cannot-be-ticked.png) |
| AB7 | **PASS** | Two different sentences, each plain, neither an error and neither silence. Nothing ticked: *"Nothing is ticked, so there is nothing to install."* A product ticked but no release: *"No Revit version is ticked, so there is nowhere to install to."* The disk was checked after each and nothing had been created. [nothing ticked](../proof/AB7-nothing-ticked.png), [no release](../proof/AB7-no-revit-version-ticked.png) |

**One thing the window does that no row asked for, and it is worth keeping.** Ticking a piece ticks its
heading, and after an install it names the releases that were **on this PC but not ticked** - *"Revit
2020 is on this PC but was not ticked, so nothing was installed for it. Run this again and tick it if
that was not meant."* That is the shape of a mistake a modeller actually makes, answered before it is
asked.

**What is still owed on this group.** `AB6`, and it is owed by Stage 2 closing rather than by anybody
running anything. `AA9` is owed by a browser download. `AB11`, added 2026-11-16, is owed by one run on
Windows - the install scripts reading the folder list `HeronPaths` writes.

---

## 2026-09-21 — THE INSTALLER CAN NOW INSTALL EVERY REVIT IN ONE PRESS, and the owner proved it

**Not a row in this register.** Nobody wrote this down as a thing to check, which is the point: it was
found by watching the owner use the window, after `Group AB` had already passed every row it asked for.

**What he did.** Ticked Revit 2020, 2024 and 2027, ticked `Heron` + `AI Bridge connector`, pressed
Install **once**. The window reported:

> `'AI Bridge connector' installed for Revit 2020.`
> `'AI Bridge connector' installed for Revit 2024.`
> `'AI Bridge connector' installed for Revit 2027.`

**Verified by reading the deployed files, not the success line** — the habit `A12` exists to enforce.
The `TargetFrameworkAttribute` was read out of each installed `Heron.Revit.Addin.dll`:

| Revit | Runtime actually installed | Expected |
|---|---|---|
| 2020 | `.NETFramework,Version=v4.7.2` | net472 — correct |
| 2024 | `.NETFramework,Version=v4.8` | net48 — correct |
| 2027 | `.NETCoreApp,Version=v10.0` | net10.0-windows — correct |

Three releases, three different runtimes, one press. **That sequence was impossible before `#242`**,
which gave every release its own build folder. Before it, each build overwrote the last and only the
newest release could be installed — he hit that refusal **five times**.
[the last failure](../proof/multi-version-install-FAIL-debug-not-release.png),
[all three in one press](../proof/multi-version-install-PASS-all-three-one-press.png)

### The failure in between was Debug-versus-Release, and this register already knew

The first attempt after `#242` still refused 2020 and 2024. The cause was **not** the fix: the builds
had been made in `Debug`, and `DeployScriptDeployer` (`WindowsAdapters.cs:415`) deploys `Release` and
always has. So nine correct builds sat in a folder the installer never opens.

**`AB5` above records this exact condition** — *"the window drives the deploy script with `-c Release`
and only a Debug build existed"* — and it was read as a one-off rather than as the standing rule it is.
Rebuilding in `Release` was the whole fix; no code changed.

**2027 appeared to work during that failure, and that is worth understanding.** With no
`Release/2020/` folder to find, the script fell back to the flat layout and found one leftover build
from earlier that day. It was genuinely a .NET 10 build, so the runtime guard passed it for 2027 and
refused it for the other two. **The guard behaved correctly and nothing wrong was installed.**

### A hole this exposed — FIXED 2026-09-21, and the fix is the section below

The fallback in `tools/deploy-addin.ps1` reaches for *any* build when it cannot find one for the
release it was asked about, and the runtime guard is what is supposed to make that safe. **The guard
reads the runtime, not the year.** Revit 2021, 2022, 2023 and 2024 all build `net48`, so a stale
`net48` leftover would deploy into any of them and report success — **the same class of defect `A12`
recorded**. It did not bite here only because the leftover happened to be .NET 10.

Now that every release has its own folder, the fallback buys nothing on a current checkout and can
still mislead. **That was proposed here and has since been done** — the script refuses by name and
prints the command that fixes it. See *THE TWO DEFECTS ABOVE ARE FIXED* below.

### Stale after #242 — CORRECTED 2026-09-21

Two comments still point at `Directory.Build.targets`, which does not exist — the fix landed in
`Directory.Build.props` after the `.targets` attempt was measured as a silent no-op. In
`tools/deploy-addin.ps1` (the block above the build discovery) and `tools/check-api-surface.py`. The
same `deploy-addin.ps1` block also still claims *"THE BUILD OUTPUT IS SHARED BETWEEN ALL EIGHT
RELEASES"*, which stopped being true in the commit that rewrote the lines above it.

**What was found when this was picked up, and it is half a correction of the paragraph above.**
`deploy-addin.ps1`'s wrong pointer had **already been corrected**, by [row 5b-99](../FRAGMENT-ISSUES.md),
in the very commit that wrote this paragraph — so one of the two names here was stale before anyone
read it. **`tools/check-api-surface.py` still said it**, and nothing was looking: 5b-99's check is
written as *"asked of the repository rather than pinned to a name, so it also catches the next one"*
and it asked of **one file**, so the next one was already in the tree and invisible to it. Both are
corrected now and the check covers the second file — named there rather than swept repository-wide,
because `Directory.Build.props` itself names a `.targets` file legitimately, in the paragraph recording
the measurement that ruled it out.

---

## 2026-09-21 — THE TWO DEFECTS ABOVE ARE FIXED, and neither fix has been seen by a person

The section above recorded two things found and deliberately not fixed. Both are fixed here. **Nothing
below was run on Windows and nothing below has been drawn**, so every row that needs a screen is
`NEEDS REAL REVIT` and stays that way until Ajmal says otherwise. **The word PROVEN does not appear in
this section on purpose.**

### 1. The deploy script refuses by name instead of substituting another release's build

`tools/deploy-addin.ps1` took **any** build it could find when it had none for the release it was asked
about, and left the runtime guard to decide. **That guard reads the runtime, not the year.** Revit
2021, 2022, 2023 and 2024 all build `net48`, so one stale `net48` leftover passes it for all four,
deploys, and reports success — the class of defect `A12` recorded. It did not bite on 2026-09-21 only
because the leftover happened to be .NET 10.

**The fallback is gone.** With no build for the release asked for, the script now says which release,
what it did find, and the exact `dotnet build` line. **It also tells the two reasons apart**, which the
old message could not:

| what is on disk | what it now says |
|---|---|
| a build for that release, in the **other** configuration | *"Revit 2024 has been built, but not in Release — what is on disk is in ...\Debug\2024. The installer window always deploys Release, so a Debug build is invisible to it."* |
| builds for other releases only | *"There is no Release build for Revit 2021 ... The Release builds on disk are for: 2020, 2024, 2027."* |
| nothing at all | *"... Nothing at all has been built here in Release yet."* |

**The second row of that table is trap 1 in this repository's own history** — the installer window
deploys `Release`, the dev flow builds `Debug`, and the old wording read as *"your change failed"*
rather than *"you built the other one"*. It cost a full round trip on 2026-09-21, after `AB5` had
already written the condition down.

### 2. The window greys a Revit release it has no build for, before Install is pressed

`R-10` has said since Stage 4 that a row which cannot be installed is greyed **with the reason on it**,
and `AB2` proves the window does that for products. **The release tick boxes were the one row it had
never been applied to**: every Revit on the PC could be ticked whether or not anything had been built
for it, and the only way to find out was to press Install and read the refusal. **Ajmal hit that five
times in one evening.**

**What the window can cheaply know, and it is not a build.** `BuildsOnDisk` reads the directory the
deploy script reads and asks whether the built file is already there. **It starts no process, builds
nothing, and reads one directory per product, cached while the window is open.**

**It looks exactly where `tools/deploy-addin.ps1` looks, and that is the load-bearing part.** A window
looking somewhere else would grey a release the script would have installed happily — worse than the
defect it fixes, because it refuses work that was possible. The configuration is taken **from the
deployer** rather than written out a second time, which is the same word that cost the round trip
above. `tests/test_deploy_script.py` holds the two searches together.

**The rule, and the two cases it deliberately does not cover:**

- A release is greyed when **every** product that could go into it has no build. One product with a
  build keeps it tickable — that install really would work, and the rows below say what else would not.
- A release **no product supports** is left alone. It cannot be installed either, but *"nothing has
  been built"* is not the true reason and the product rows already carry the right one. Same ruling as
  [row 5b-80](../FRAGMENT-ISSUES.md).
- **Not knowing leaves every release tickable.** Greying one that is installable costs a modeller an
  install they were entitled to, with a sentence telling them to build something already built.
  Leaving one tickable costs a refusal after the press — exactly where this stood before, so it is no
  worse than nothing.

**The one case this does NOT close, said plainly.** A release stays tickable when *one* product has a
build and *another* does not, and the second will still fail after the press. **It cannot happen
today** — `heron-bridge` is the only `SHIPPED` product, so there is never a second one to disagree —
and it is written here rather than quietly left, because the day a second product ships is the day this
rule needs looking at again.

### What was actually run, and what it said

| | |
|---|---|
| **PASS** | The **ten gates** `.github/workflows/gates.yml` decides on — `check-docs`, `check-metadata`, `check-structure`, `check-signatures`, `check-licence`, `check-narrow-errors`, `check-package`, `check-products`, `check-routing`, `check-intrusion`. All exit 0 |
| **PASS** | `tests/test_installer_engine.py` — the screen model and `BuildsOnDisk` against a fake Revit and a **real temporary folder**. The release greying, the command in the sentence, the configuration, and the whole-folder match all check out |
| **PASS** | `tests/test_deploy_script.py` and `tests/test_installer_window.py` |
| **PASS** | `tools/check-compile.py` — **all 13 projects on all eight releases, 2020 to 2027, 0 warnings**, the window included |
| **PASS** | **Every suite: 219 ran, 216 passed, 0 failed, 0 hung.** The 3 waiting are exactly the three `gates.yml` names as not runnable on a plain runner, and that list is unchanged |
| **PASS** | **And CI agreed, on a clean runner** — [#251](https://github.com/Ajmalpshaik/Heron-AI/pull/251), head `ca0e13a`, 2026-09-21: all five jobs green, no merge conflict, no review thread. That is the same ten gates, the same eight-release compile and the same sweep, run somewhere that has never had this container's `dotnet` installed by hand |
| **PASS** | **The checks were seen to FAIL, which is what makes them checks** — [heron-ship §2a](../../.claude/skills/heron-ship/SKILL.md). Counted below |
| **NOT RUN** | **No window has been drawn and no PowerShell has executed.** `BuildsOnDisk` is the only one of the four Windows adapters that runs here at all, and what it proves is its path arithmetic, not that the folder it names is the one MSBuild wrote to on Ajmal's PC |
| **NEEDS REAL REVIT** | `AA11`, `AA12`, `AB9` and `AB10` below. **Screenshots are owed for `AB9` and `AB10` and cannot be taken here** — this is Linux and the window is WPF |

**Seen to fail, against the code as it stood:**

| what was put back or broken | checks that went red |
|---|---|
| `tools/deploy-addin.ps1`, `check-api-surface.py` and the four C# files reverted to `HEAD` | **18** — 12 in `test_deploy_script.py`, 6 in `test_installer_window.py` |
| the release greying deleted, types left in place | **5** in the test host |
| `BuildsOnDisk` made to ignore the configuration | **2** |
| the release matched as a substring instead of a whole folder | **1** |
| the `CanBeTicked` guard deleted from `ChosenReleases()` | **1** |

**Two of those found a check that was not checking, which is the point of running them.**

- *"prints the exact command that makes that build"* was **green against the broken script**, because
  `$rebuildLine` further down has carried that command since before the fallback existed. Narrowed to
  the refusal's own text; it now goes red.
- *"a greyed release installs nothing even if its tick arrives set"* was **green with the guard
  deleted**, because a greyed release also starts unticked, so `Chosen` alone dropped it. The check now
  puts the tick back on by hand first — which is what a window drawn before this rule would send.

**And a third, found while reading rather than by running.** `tests/test_installer_window.py` anchored
its `AB1` no-clipping checks on the first `var tick = new CheckBox` **in the file**, which is the
*release* box, not the product one — so the block it read spanned both and it passed either way. It is
narrowed to `ProductList()`. **Nothing in that section was relaxed**; it asks for strictly more than it
did.

### Rows for Ajmal's PC — four, and two of them need the window open

| ID | Do this | Pass looks like |
|---|---|---|
| **AA11** | **Close Revit.** Delete `revit\Heron.Revit.Addin\bin\x64\Release\2021\` if it is there. Then `.\tools\deploy-addin.ps1 -RevitVersion 2021 -Product heron-bridge -Configuration Release` | It **refuses, naming Revit 2021**, says which releases *are* built in Release, and prints the `dotnet build ... -p:RevitVersion=2021` line. **Nothing is copied.** Check `%APPDATA%\Autodesk\Revit\Addins\2021\` afterwards: unchanged. Before this fix it would have taken a 2024 build — same `net48`, same guard, reported success |
| **AA12** | Build **one** release in `Debug` only (`dotnet build revit\Heron.Revit.Addin\Heron.Revit.Addin.csproj -c Debug -p:RevitVersion=2020`), delete that release's `Release` folder, and deploy it with `-Configuration Release` | It says the release **has been built but not in Release**, and names the Debug folder it found. This is the sentence that would have saved the round trip on 2026-09-21 |
| **AB9** | **Close Revit.** Delete the `Release` build folder for **one** of the releases on the PC, keep the others, and open `HeronInstaller.exe` | That release's tick box is **greyed and unticked**, and the sentence under the release row says *"Nothing has been built for Revit NNNN on this PC..."* **with the `dotnet build` command on the line below it**. The others are still ticked. **Screenshot** — `docs/proof/AB9-*.png`. The failure to watch for: the sentence landing beside the wrong release, or the window taking noticeably longer to open |
| **AB10** | With all three releases built in `Release`, open the window again | **Every release is tickable and no sentence appears.** This is the half that matters more than `AB9`: a window that greys a release it could have installed is worse than the defect being fixed. **Screenshot** — `docs/proof/AB10-*.png` |

**`AB9` and `AB10` are a pair and both must be seen.** One of them says the grey appears; the other
says it does not appear when it should not. Either alone is half an answer.

### A tooltip on a greyed row does not show, and the PRODUCT rows have the same latent hole

WPF's `ToolTipService.ShowOnDisabled` is **false by default**, so a `ToolTip` set on a disabled control
never appears. The release boxes added here set it to true; **the product rows, which have carried a
tooltip since Stage 4, do not.**

**`AB2` still passed, and correctly** — what it asks for is the sentence **printed under the row**, and
that is there and is what a person reads. `R-10` is met either way. But the window's own comment says
*"THE REASON IS ON THE ROW"* next to a tooltip that cannot be seen on the one kind of row it is for, so
half of that line has never done anything.

**READ FROM THE DOCUMENTATION, NOT FROM A WINDOW.** Nothing here has drawn one. **Worth one hover on
Ajmal's PC** while `AB9` is being looked at: hover a greyed **product** row and a greyed **release**
box, and see whether only the second shows a tooltip. Not fixed for the product rows, because that is
a change to a row `AB2` has already passed and it is his call whether to make it.

### A THIRD stale claim, FOUND AND NOT TOUCHED — waiting on Ajmal

[`docs/07-installation-and-update.md` §10.1](../07-installation-and-update.md) says, of the 2026-09-20
run: *"Each release was built **separately** and deployed before the next was built, **because the
build output folder is shared and the newest build wins the search**."* **The reason is in the present
tense and stopped being true at [#242](https://github.com/Ajmalpshaik/Heron-AI/pull/242)** — the same
sentence, in the same words, as the one corrected in `deploy-addin.ps1` here.

**It was deliberately not changed.** That section is a dated record of a run, and the block three
paragraphs below it says in as many words *"The paragraph above stays as written, because it is the
record of the gap being spotted a day before it bit."* Editing a run record to match today is a
different decision from correcting a live comment, and it is Ajmal's to make. **The smallest fix would
be one clause** — *"because the build output folder **was then** shared"* — which keeps the record
true as a record and stops it reading as a rule that still applies.

---
