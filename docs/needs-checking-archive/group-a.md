# NEEDS-CHECKING archive — Group A

> **Checks that were done, moved out of the live register.** These are rows of Group A of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *does it build, and does the bridge work* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row A2

*Moved from the register on 2026-09-23.*

**Do this.** ~~`dotnet build -p:RevitVersion=2020`~~

**Pass looks like.** **DONE 2026-08-28.** Compiles, 0 warnings, after the `CreationGUID` fix above. The other five spots this row used to warn about — `WorksharingUtils.GetCheckoutStatus`, `IFailuresPreprocessor`, `TransactionGroup.GetStatus`, `BuiltInCategory.INVALID`, `UIDocument.RefreshActiveView` — are all **clean on 2020 and 2024**

---

### Row A3

*Moved from the register on 2026-09-23.*

**Do this.** ~~`dotnet build -p:RevitVersion=2024`~~

**Pass looks like.** **DONE 2026-08-28.** So are 2021, 2022 and 2023 — all four projects, 0 warnings each

---

### Row A5

*Moved from the register on 2026-09-23.*

**Do this.** ~~`python tools/check-compile.py 2025 2026 2027`~~

**Pass looks like.** **DONE 2026-08-28, and it did not need Windows either.** All four projects compile on 2025, 2026 and 2027 — `net8.0-windows` and `net10.0-windows`, 0 warnings — on Linux, with the .NET 10 SDK and `-p:EnableWindowsTargeting=true`. This row assumed the Windows Desktop SDK was a property of the operating system; it is a property of the **SDK package**, and Ubuntu's `dotnet-sdk-10.0` ships it while its `dotnet-sdk-8.0` does not. The script had the right MSBuild flag and applied it **only on Windows**, where it does nothing. Full account and the validation in [docs/30 §2a](../30-compiling-away-from-windows.md). `python tools/check-api-surface.py` still runs and still adds something a compile cannot — it reads the **shipped** assemblies, where a compile reads the NuGet reference packages

---

### Row A6

*Moved from the register on 2026-09-23.*

**Do this.** ~~On **Windows**, run `python tools/check-compile.py 2025` and read the first lines of output~~

**Pass looks like.** **DONE 2026-09-06, on the owner's PC — the probe reads the SDK correctly on Windows.** Output: *"Compiling with .NET SDK 10.0.303 on Windows. WindowsDesktop targets found in the .NET 10 SDK"*, then all four projects **ok** on Revit 2025. This row existed because `check-compile.py` decides whether it can build the WPF releases by **looking for** `Sdks/Microsoft.NET.Sdk.WindowsDesktop` under each installed SDK rather than by asking whether it is on Windows — and **that probe had only ever run on Linux**, where a false negative costs nothing. On Windows a false negative would have **silently skipped 2025-2027 on the one machine where they used to build**, which is why a five-minute check was worth keeping. It did not misread, so the fallback path (attempt the build anyway when the SDK list cannot be read at all) is **still unproven** — it was never reached

---

### Row A7

*Moved from the register on 2026-09-23.*

**Do this.** ~~`pip install --user model2vec` then `python brain/heron_embed.py "stop the air going the wrong way"`~~

**Pass looks like.** **DONE 2026-09-06 on the owner's PC. `Backend: model`, 343 fragments embedded** — the weights host is reachable from here, which it was not from either container. **The check was not stopped at that line**, because *the model loaded* and *the model helps* are different claims. Scored against candidates sharing **no word** with the query: the model ranks `check flow direction` **first at 0.391** and `rename a sheet` at **0.001**; the built-in `lexical` backend ranks the same correct answer **LAST at 0.038**, below `rename a sheet` at 0.048. **The old engine's best guess was `find dead ends` and its worst was the right answer** — which is what *tolerant of spelling but not of meaning* costs, in numbers. **Two things recorded rather than smoothed over.** In the full index `find-dead-ends` (0.478) edged `check-flow-direction` (0.475) by **0.003** — too close to call, and both are defensible readings of that sentence, so this row proves the backend understands meaning and does **not** prove any particular ordering. And **this row's PASS wording is retired with its reasoning**: it asked for *"the duct fragment"*, written when the library held seven and no damper/flow fragment existed. At 343 the honest form is *a flow-direction fragment ranks top-2 with no shared words*, which is what happened

---

### Row A12

*Moved from the register on 2026-09-23.*

**Do this.** ~~On the PC: `.\tools\deploy-addin.ps1 -RevitVersion 2024`, start Revit, and look for the **Heron AI** tab~~

**Pass looks like.** **DONE 2026-09-19 on the owner's PC — and on ALL THREE releases installed there, 2020, 2024 and 2027, rather than on 2024 alone.** Written up in full, with the journal lines and the hashes, in [07 section 10](../07-installation-and-update.md). Built from commit `48ae1dc`, one release at a time because the build output folder is shared, and placed by the documented script. **Two of the four questions are answered by Revit rather than by Heron**, which is the part this row was really asking for: every journal records `API_SUCCESS { Starting External Application: Heron AI, Class: Heron.Revit.Addin.HeronApplication, Vendor : AJPS(Ajmal PS - Heron AI) }`, and **no journal contains *"cannot run the external application"***. The ribbon built on all three — `HeronBridgeToggle`, `HeronStatus` and `HeronWriteToggle` under tab **Heron AI**, panel **Bridge** — and the owner clicked **Changes** on 2020, which logged `Write permission set to False from the ribbon` and `True` two seconds later, so the icon drew well enough to find and press. **The manifest question is answered three ways**, because one line naming a path is not the same as nothing else being able to supply it: every pushbutton line names `%APPDATA%\Autodesk\Revit\Addins\<release>\Heron\Heron.Revit.Addin.dll`; `%ProgramData%\Autodesk\Revit\Addins` and all three `Program Files` `AddIns` folders hold nothing called `Heron*`, so the per-user manifest is the only one on the machine; and each deployed assembly was **file-locked** by its own Revit while running. **Revit 2027 says it outright** in two lines the older releases do not emit — `The add-in folder '...\Addins\2027\Heron' was registered for the context 'DEFAULT'` and `[Jrn.AddInManifest] Rvt.Attr.AddInManifest: Heron.addin ... Rvt.Attr.AddInLoadFailureMessage: NoError , 0.248000`. **The no-admin promise held**: `IsInRole(Administrator)` was `False` throughout, six deploys and three rollbacks wrote only to `%APPDATA%` and `%LOCALAPPDATA%`, and **no UAC prompt appeared at any point**. **And the runtimes really are three different ones**, read back out of the deployed bytes rather than out of a build log: `.NETFramework,Version=v4.7.2` on 2020, `v4.8` on 2024, `.NETCoreApp,Version=v10.0` on 2027 — the thing a single-release test can never show. **One gap in the script's own guard, found by doing this:** it separates builds by whether a `deps.json` exists, which catches a 2027 build going into 2024 but **cannot tell net472 from net48**, because neither emits one. A 2024 build deployed into 2020 would pass every check. It did not happen here, but nothing stands behind that guard

---

### Row A13

*Moved from the register on 2026-09-23.*

**Do this.** ~~On the PC, with a previous Heron already installed: deploy over it, then start Revit~~

**Pass looks like.** **DONE 2026-09-19 on the owner's PC, on all three releases — and the rollback half could not be done at all until it was built, which is the finding.** Written up in [07 section 10](../07-installation-and-update.md), filed as [row 147](../FRAGMENT-ISSUES.md). **The upgrade half passed cleanly.** Every release already carried an install, so each of the six deploys was an upgrade rather than a first install, and `%APPDATA%\Heron\config\heron.config` hashed `9FC06D52CEBB099977BF0EBF9C68389652D717A84A9F536351ADBDF7FBC7FB39` **before and after every one of them**. The file's own first line claims *"Owned by you. Never overwritten by an update"* and that is now measured rather than asserted — it carries `write.enabled = true`, which is the switch [D-19](../DECISIONS.md) puts writing behind. `audit/` stayed 3 files / 4,049,586 bytes and `knowledge/` 4 files / 4,235,331 bytes across it. **The rollback half had no mechanism.** This row's own recipe — *"`-Remove` followed by a redeploy of the previous build"* — assumes the previous build still exists somewhere, and it did not: `deploy-addin.ps1` overwrote it with `Copy-Item -Force` and kept nothing, `heron-backup.py` covers only the DATA class and says so, and `brain/heron_update.py` meanwhile **refuses** any release that cannot show a recorded rollback test. Built into `deploy-addin.ps1` — every deploy now keeps the install it replaces in `%LOCALAPPDATA%\Heron\install-backup\<release>\` with a `replaced.json`, and `-Rollback` puts it back. **Proved as a round trip with a genuinely different build**, not a file copy checked by eye: a second build at `-p:Version=0.1.1` deployed over the good one to stand in for a bad update, then rolled back, then every file hashed — **2020 22 files restored, 0 differing; 2024 22 and 0; 2027 15 and 0**, manifest restored all three times. The counts differ because the Framework releases carry `System.*` assemblies .NET 10 has in the box and 2027 carries a `deps.json` they do not. **What it does NOT prove, said plainly:** it proves the FILES come back, and no more. The restored install is byte-identical to the one proved to load, so it loads for the same reason — **inference, not an independent observation**, and no release was restarted a second time to make it one. **The interesting case this row named — somebody forgetting to close Revit — is now guarded on `-Remove` and `-Rollback` too**, which it was not: `Remove-Item` on a loaded assembly fails part-way through the folder and reads as a broken uninstall rather than as *"close Revit first"*

---
