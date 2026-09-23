# Needs checking — Group AB

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group AB - Stages 3 and 4 of the installer: the engine, and the window nobody has seen 2026-09-21

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
| ~~**AB1**~~ | Run `HeronInstaller.exe`. — [full row](../needs-checking-archive/group-ab.md#row-ab1) | **FAILED, FIXED, AND THE RE-RUN PASSED - 2026-09-21 on the owner's PC.** |
| ~~**AB2**~~ | Look at the rows that cannot be ticked — [full row](../needs-checking-archive/group-ab.md#row-ab2) | **PASSED 2026-09-21 on the owner's PC** |
| ~~**AB3**~~ | **THE ONE THAT MATTERS MOST.** Add a product to `platform\heron-products.json` by hand - a new id, a new `addInId`, `state` SHIPPED - and **reopen the window without rebuilding anything** | **PASSED 2026-09-21 on the owner's PC** - the verdict is in *Group AB was RUN*, below. *It was to look like:* **The new row is there.** This is [R-3](../work-notes/plans/plugin-extension/01-requirements.md) and it is the reason the whole design is shaped this way: adding Heron Structure next year must be a line in a file. If this fails, the installer has a product list written inside it and the design has not been built. Undo the edit afterwards |
| ~~**AB4**~~ | Install the AI Bridge, then **reopen the window** | **PASSED 2026-09-21 on the owner's PC** - the verdict is in *Group AB was RUN*, below. *It was to look like:* Its row reads **`Installed`** rather than `--`, and **stays tickable**. Then delete its folder from `%APPDATA%\Autodesk\Revit\Addins\2024\` by hand and reopen again: it reads `--`. The state is read from the disk every time, never remembered - [R-2](../work-notes/plans/plugin-extension/01-requirements.md) |
| ~~**AB5**~~ | **Leave Revit open**, tick a product, press Install | **PASSED 2026-09-21 on the owner's PC** - the verdict is in *Group AB was RUN*, below. *It was to look like:* The window **does not freeze**, and it says which Revit is open and that it will carry on by itself. **Close Revit without touching the window.** The install then continues on its own and reports per product per release - [R-38a](../work-notes/plans/plugin-extension/01-requirements.md). **Nothing must have been changed before Revit was closed** |
| **AB6** | Tick **the tools only, without the AI Bridge**, and install | The tools install and **no bridge does** - [R-34](../work-notes/plans/plugin-extension/01-requirements.md), a site modeller who wants Heron's tools and refuses the AI. This depends on `AA2` passing first: if two add-ins cannot build one tab, this row is asking about a shape that does not exist |
| ~~**AB7**~~ | Press Install with nothing ticked, and again with no Revit version ticked — [full row](../needs-checking-archive/group-ab.md#row-ab7) | **PASSED 2026-09-21 on the owner's PC** |
| **AB8** | **THE ONE THAT HANGS RATHER THAN FAILS.** Make a deploy go wrong loudly while the window is watching - the simplest is to install for a release, leave **that** Revit open, and press Install - then do it again with a product whose build folder is missing. Watch the window through both | **It comes back with a sentence, every time, and never just stops.** Until 2026-09-21 the installer read one pipe to the end and then the other, which deadlocks the moment the unread one fills - and the 600-second ceiling could not fire, because the thread never reached it. Measured on Linux: **200 KB on standard error never returned at all**. Both streams are drained at once now and `tests/test_installer_adapters.py` holds the shape, but **that is a text check on C# from a machine with no PowerShell**. This is the run. Also check the release list is right at all: if the window says **no Revit was found** on a PC that has one, the environment answer is being parsed with something else mixed into it. Added 2026-09-21 with [row 5b-78](../FRAGMENT-ISSUES.md); **never run** |

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
running anything. `AA9` is owed by a browser download.

---
