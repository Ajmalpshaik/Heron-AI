# Needs checking — Group AA

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group AA - Stage 2 of the installer plan: can two Heron tabs live in one Revit? 2026-09-21

**This is the question the whole installer plan rests on**, and it is the cheapest possible way to ask
it - [Stage 2](../work-notes/plans/plugin-extension/02-implementation.md). Two throwaway products were
built, each with one panel and one button that does nothing:

| | |
|---|---|
| `revit/Heron.Doc/` | a **second tab**, `Heron Doc` |
| `revit/Heron.Tools/` | a **second piece of the first tab** - a `Tools` panel on the tab named `Heron` |

**Both compile on all eight releases, 2020 to 2027, 0 warnings.** `tests/test_ribbon_tab_sharing.py`
passes 26 checks on what is decidable in source. **Neither has been seen in a Revit, and until the rows
below are answered the stage is BUILT, not PROVEN.** A compile proves the API agrees. It does not prove
a tab appears.

**Stage 3 was not to begin until AA1, AA2 and AA3 had passed, AND IT DID - 2026-09-21.** The plan says
so in as many words: if this fails, the cost of finding out is one dummy button; finding out after the
installer window is built costs the installer window. **The owner chose to build Stage 3 anyway**, and
the reason this sentence is here rather than quietly edited is that the bet has to be visible when the
answer arrives.

**What it costs if AA2 fails.** Nothing in the install engine changes - it decides which product goes to
which release and never touches a ribbon. What changes is [S3](../work-notes/plans/plugin-extension/00-structure.md):
a `Heron` tab that two add-ins can build between them. If that is not how Revit behaves, the tools piece
and the AI Bridge piece have to become **one** add-in with a switch, `heron-tools` stops being a separate
product, and the product list, the window and R-34 all follow it. **The engine survives; the shape does
not.** That is the bet, written down before the run.

**Close Revit before every command below.** Each one refuses while it is open and names the release it
found - that refusal is row `AA6`.

```powershell
dotnet build revit\Heron.Doc\Heron.Doc.csproj      -p:RevitVersion=2024
dotnet build revit\Heron.Tools\Heron.Tools.csproj  -p:RevitVersion=2024
.\tools\deploy-addin.ps1 -RevitVersion 2024 -Product heron-doc
.\tools\deploy-addin.ps1 -RevitVersion 2024 -Product heron-tools
```

**The script these commands use changed on 2026-09-21**, after this group was written. Stage 2 first had
a throwaway deploy script of its own, because `deploy-addin.ps1` could deploy only the AI Bridge. Stage 3
had to teach that one script every product - [R-31](../work-notes/plans/plugin-extension/01-requirements.md)
allows one engine - which left the throwaway as a second copy of the same rule, so the throwaway was
deleted. **Rows AA7 to AA9 below are the cost of that**, and they are on the same Revit session as the
rest: `deploy-addin.ps1` has not been run since it was changed, and its rollback proof of 2026-09-19
(`Group Z`) is **STALE for the current file**.

| ID | Do this | Pass looks like |
|---|---|---|
| ~~**AA1**~~ | Deploy both proofs with the AI Bridge add-in also installed. — [full row](../needs-checking-archive/group-aa.md#row-aa1) | **PASSED 2026-09-21 on the owner's PC** |
| ~~**AA2**~~ | TOOLS ONLY. — [full row](../needs-checking-archive/group-aa.md#row-aa2) | **PASSED 2026-09-21 on the owner's PC** |
| ~~**AA3**~~ | AI BRIDGE ONLY. — [full row](../needs-checking-archive/group-aa.md#row-aa3) | **PASSED 2026-09-21 on the owner's PC** |
| ~~**AA4**~~ | `.\tools\deploy-addin.ps1 -RevitVersion 2024 -Product heron-doc -Remove`, then start Revit — [full row](../needs-checking-archive/group-aa.md#row-aa4) | **PASSED 2026-09-21 on the owner's PC** |
| ~~**AA5**~~ | Do AA1 on 2020 as well as on a modern release — [full row](../needs-checking-archive/group-aa.md#row-aa5) | **PASSED 2026-09-21 on the owner's PC** |
| ~~**AA6**~~ | Run `.\tools\deploy-addin.ps1 -RevitVersion 2024 -Product heron-doc` **with Revit open** | **PASSED 2026-09-21 on the owner's PC** - the verdict is in *Group AA was RUN*, below. *It was to look like:* It refuses, names the release that is open, and **changes nothing**. Revit holds every assembly it has loaded, so a copy over one fails; the script waits for a person rather than renaming the folder aside - [R-38a](../work-notes/plans/plugin-extension/01-requirements.md) |
| ~~**AA7**~~ | THE ONE THAT MATTERS MOST. — [full row](../needs-checking-archive/group-aa.md#row-aa7) | **PASSED 2026-09-21 on the owner's PC** |
| ~~**AA8**~~ | After AA7, run it again to force a replace, then .\tools\deploy-addin.ps1 -RevitVersion 2024… — [full row](../needs-checking-archive/group-aa.md#row-aa8) | **PASSED 2026-09-21 on the owner's PC** |
| ~~**AA9**~~ | Download this repository as a **zip** from GitHub, extract it, build, and deploy `heron-doc` from the extracted copy | **FAILED 2026-09-21, and no longer OPEN by the owner's ruling the same day, [D-96](../DECISIONS.md).** The half about the download mark on a built assembly is still owed, by Stage 5's release asset - *Group AA was RUN*, below. *It was to look like:* It installs and the tab appears. This is [R-37](../work-notes/plans/plugin-extension/01-requirements.md), the download mark: Windows stamps anything that came through a browser, and a stamped assembly makes Revit refuse the add-in with a message naming nothing useful. `Unblock-File` was added to `deploy-addin.ps1` on 2026-09-21 and **has never run**. A FAIL here looks like Revit saying only that it cannot run the external application |
| **AA10** | **THE HALF-WRITTEN BACKUP.** After AA8 has made one, delete `replaced.json` out of `%LOCALAPPDATA%\Heron\install-backup\2024\Heron` by hand, then run `-Rollback`. Afterwards put it back and delete one **assembly** from the backup instead, and run `-Rollback` again | **Both times it refuses and changes nothing**, naming which of the two it is - no completion mark, or a file count that no longer matches its own record - and telling you to build and deploy from source. Then start Revit and check the add-in you already had **still works**: the whole point is that a refused rollback does not touch the live install. The backup is now built in `install-backup\2024\Heron.incomplete` and renamed into place, so **that folder should not exist** after a normal deploy - if one is sitting there, a copy died and the swap never happened, which is the case this exists for. Added 2026-09-21 with [row 5b-79](../FRAGMENT-ISSUES.md); **never run** |

**What a FAIL on AA2 would mean, written down before the run so the answer is not argued afterwards.**
Both pieces call `CreateRibbonTab` inside a `try` and catch `Autodesk.Revit.Exceptions.ArgumentException`,
which is the documented behaviour for a tab that already exists. If tools-only still draws no tab, the
assumption to question first is that `CreateRibbonPanel` can reach a tab created by a *different*
add-in in the same session - which is exactly the kind of thing only a real Revit answers, and exactly
why this stage exists.

**Nobody may mark Stage 2 done from a compile, from this file, or from the tests.** The word for what
exists today is **BUILT**.

### Group AA was RUN on the owner's PC - 2026-09-21. EIGHT ROWS PASS, AA9 FAILS

**Stage 2 is PROVEN.** The paragraphs above are left exactly as they were written before the run,
because the bet had to be visible when the answer arrived - and the answer is that **the bet paid**.
`AA9` is about the download mark rather than the ribbon, so it does not touch that verdict - but it
failed, and what it found is worse than what it was looking for.

**Where it was run.** Windows 11, two 1920x1080 screens at **100% scaling**, Revit **2024.3** and Revit
**2020.2.9**. Model `test projject.rvt`, the scratch model earlier fragment work used. Revit was closed
before every deploy command. `HERON_CLIENT_ID=ajmal-pc` on every bridge call.

| ID | Verdict | What was actually seen |
|---|---|---|
| AA1 | **PASS** | **TWO tabs, `Heron` and `Heron Doc`, and only ONE of them reads `Heron`** - so `CreateRibbonTab` found the existing tab rather than making a second one, and **R-35 holds**. The `Heron` tab carried **both** an `AI Bridge` panel and a `Tools` panel. All three existing buttons worked with the two extra add-ins loaded: toggle connected (`pong <- Revit 2024, session 6384`), `Bridge Status` opened on the right session, `Changes` toggled both ways. The `Tools` panel's `Proof` button opened its dialog. [proof](../proof/AA1-two-tabs-heron-and-heron-doc.png), [button](../proof/AA1-tools-proof-button.png) |
| AA2 | **PASS - AND THIS IS THE ONE THAT WAS EXPECTED TO BREAK** | With the AI Bridge **removed**, a `Heron` tab still appeared, carrying the `Tools` panel and **no `AI Bridge` panel**. The tools piece **created the tab on its own**. So `CreateRibbonPanel` can reach a tab built by a different add-in, the assumption named above as the first to question is **sound**, and **R-34 is deliverable**: a site modeller can take Heron's tools and refuse the AI. [proof](../proof/AA2-tools-only-heron-tab.png) |
| AA3 | **PASS** | AI Bridge alone: one `AI Bridge` panel, no `Tools` panel, bridge answered (`session 39652`). The proofs left nothing behind. [proof](../proof/AA3-ai-bridge-only.png) |
| AA4 | **PASS** | `Heron Doc` gone, `Heron` untouched. On disk: no `Heron.Doc` folder, no `Heron.Doc.addin`, and **no `.old` anything** - R-38c. Beside it in the same folder sit `AJ Tools.20260819155602216` and `AJ Tools.20260820191129958`, which is the renaming-aside habit Heron was ruled against, still visibly piling up. [proof](../proof/AA4-heron-doc-removed.png) |
| AA5 | **PASS** | Same on **Revit 2020** (`net472`): two tabs, one `Heron` tab carrying both panels, bridge answered (`session 11056`). `Z9`'s precedent of checking rather than assuming cost nothing here - the ribbon API behaved identically. [proof](../proof/AA5-revit2020-two-tabs.png) |
| AA6 | **PASS** | With Revit open the deploy **refused**, naming the release **and the process**: *"Cannot install for Revit 2024: Revit 2024 is open (process 14536). Close it and run this again - a loaded assembly cannot be replaced, so doing this now would half-finish and look like it worked."* `Addins\2024` held 9 entries before and 9 after; no `Heron.Doc` folder and no manifest were created |
| AA7 | **PASS** | Run as `.\tools\deploy-addin.ps1 -RevitVersion 2024` with **no `-Product` at all**, which is how it was run before the switch existed. The default resolved to the same four values it always had, and the new runtime guard spoke: *"built for .NET Framework 4.8, which is what Revit 2024 needs"*. In Revit: tab, panel, and all three buttons - connect, disconnect (icon tracks state), `Bridge Status`, and `Changes` with its confirmation on the ON direction only. `ping` answered `pong <- Revit 2024, session 32928, add-in 0.1.0.0, protocol 2`. **The regression from generalising the script did not happen.** [tab](../proof/AA7-heron-tab-ai-bridge-connected.png), [status](../proof/AA7-bridge-status-window.png), [confirmation](../proof/AA7-changes-confirmation.png) |
| AA8 | **PASS** | **Rollback was made provable rather than assumed.** Deploying the same build twice would restore an identical folder, so two marker files were planted in the install first. The replace then **removed** them (22 files, neither marker) - which is **R-38 proved by doing it**: replace, never copy over. The backup kept all **24** files including both markers, at the **new** path `install-backup\2024\Heron`, and `-Rollback` put all 24 back, naming what it was restoring first. Revit then started on the rolled-back install and the bridge answered. **The pre-change backup case was also run for real**: Revit 2027 still had a backup at the old path, and `-Rollback` there refused cleanly - *"Nothing to roll back to for Revit 2027"* plus the two commands to deploy from source - and changed nothing (15 files before and after). [proof](../proof/AA8-rolled-back-addin-works.png) |
| AA9 | **FAIL - AND NOT THE FAILURE THIS ROW PREDICTED** | The owner downloaded the repository as a zip from GitHub in a browser. **All 2130 extracted files carry `ZoneId=3`**, so the condition was real. `dotnet build` succeeded. Then `.\tools\deploy-addin.ps1` **was refused by PowerShell before Revit was ever involved**: *"cannot be loaded. The file ... is not digitally signed. You cannot run this script on the current system."* His `CurrentUser` execution policy is **`RemoteSigned`**, which refuses any *downloaded* unsigned script. **`Unblock-File` is INSIDE that script, so it cannot clear its own mark** - and the row's predicted symptom, Revit saying only that it cannot run the external application, never arrived because nothing got as far as Revit. See the two sections below for what this does and does not mean |

**What else this run established, none of it asked for by a row.**

- **The runtime guard can tell `net472` from `net48`**, which is the pair neither of the old proxy checks
  could separate: deploying for 2020 said *"built for .NET Framework 4.7.2, which is what Revit 2020
  needs"* and for 2024 said *"4.8"*. `A12`'s gap is closed in practice, not only in source.
- **The per-product backup path works per product**, not only per release: removing the tools wrote
  `install-backup\2024\Heron.Tools` and removing Heron Doc wrote `install-backup\2024\Heron.Doc`,
  which is exactly the collision the 2026-09-21 path change was made to stop.
- **One honest side effect of that path change, seen on disk.** The new backup folder for `heron-bridge`
  is `install-backup\<version>\Heron`, which is the *same path* the old layout used for the backed-up
  folder itself. So the first new deploy **replaced** the old backup's folder, leaving the old
  `Heron.addin` and `replaced.json` orphaned one level up. Nothing was lost that `-Rollback` could have
  used, and it is inside Heron's own folder, but the header's *"no old backup is deleted or moved"* is
  **not quite true for `heron-bridge`** and is true for every other product.
- **`bridge.autoConnect = false` behaves correctly.** An early reading here suspected auto-connect,
  then suspected a double-toggle. Both were wrong: **the owner was pressing the ribbon button himself
  at the same time.** Recorded because the log line reads *"Connected from the ribbon"* either way, and
  a second pair of hands on the machine is invisible to it.

#### What AA9 actually found: the execution policy, not the download mark

**A DOWNLOADED COPY OF HERON CANNOT BE INSTALLED BY HAND.** Not on this machine and not on any machine
left at the Windows default for a developer. `Get-ExecutionPolicy -List` gives `CurrentUser =
RemoteSigned`, which runs a script written locally and **refuses one that came from the internet unless
it is signed**. Heron's is not signed. The refusal is a `SecurityError`, before a single line executes.

**THE FIX FOR THE DOWNLOAD MARK CANNOT REACH ITSELF.** `Unblock-File` was added to
`deploy-addin.ps1` on 2026-09-21 to clear the mark off the files Revit loads - R-37. It sits *inside*
the script that is itself refused for carrying that mark. A person who downloads the zip has no way to
run the thing that would fix the problem, and the message they get names signing rather than the zone.

**THE INSTALLER IS NOT AFFECTED, AND THAT IS NOT LUCK.**
`platform/Heron.Installer/WindowsAdapters.cs` launches `powershell.exe` with `-NoProfile
-ExecutionPolicy Bypass`, and the comment beside it already says why. **Checked by running the deploy
from the downloaded copy exactly the way the installer invokes it** - it deployed, Revit loaded it, the
`Heron Doc` tab appeared and its button ran. **Proved it was the downloaded build and not a leftover
from `AA1`**, which is the obvious thing to get wrong here: the deployed assembly hashed
`AE35E790...` and so did the zip's build output, while the repo's own build of the same source hashed
`A66D0EF1...`. Different file, and the repo copy had been uninstalled hours earlier by `AA4`.

**So the damage is bounded to the by-hand route** - which is the route every one of these rows uses,
and the route the README gives.

#### And the thing AA9 set out to prove is STILL NOT PROVEN

**A SOURCE ZIP CANNOT CREATE THE CONDITION R-37 GUARDS AGAINST.** The mark is on the *sources*:
`Heron.Doc.csproj` carries `ZoneId=3`. The assembly `dotnet build` produces from them is a **brand new
file and carries no mark at all** - measured, not assumed. So `Unblock-File` had nothing to clear, the
deployed files came out unmarked either way, and **the guard was never exercised**.

The case R-37 exists for is a zip that already contains **built DLLs** - a release asset, which is
[Stage 5](../work-notes/plans/plugin-extension/02-implementation.md). Until one exists, `Unblock-File`
has still never done its job, and no run on this machine can make it.

**What this row is owed, restated.** Two separate things, and they were one sentence before this run:

| | |
|---|---|
| **The execution policy wall** | Real, reproduced, and it blocked the documented way to install Heron. **RULED ON THE SAME DAY - [D-96](../DECISIONS.md)** |
| **The download mark on an assembly** | Still unproven, and unprovable until a release asset exists |

##### The owner ruled on it the same day - D-96, 2026-09-21

Two ways out were put to him: **sign the script**, or **make the installer the only supported route**.
He chose the second, in as many words: *"tell people to use the installer button only."*

**So `AA9` stays FAILED and is no longer OPEN.** The wall is real and is not going away; what changed
is that nobody is walked into it any more. [07 §1a](../07-installation-and-update.md) now says
`HeronInstaller.exe` where it used to offer `deploy-addin.ps1` to a modeller, and the installer's
`-ExecutionPolicy Bypass` is **load-bearing rather than a convenience** - removing it breaks every
downloaded install.

**Signing was considered and refused**, and the reason is written down rather than left implicit: a
certificate is a yearly cost and a renewal that silently breaks every install the day it lapses, to
fix a route the installer already walks past.

**`deploy-addin.ps1` does not change and is not deprecated.** It is still the one engine that copies a
product in (R-31), the installer still drives it, and it still works perfectly from a `git clone` -
which is not a download and carries no mark. What changed is **who is told to type it**.

**The second half is untouched by the ruling.** `Unblock-File` still has never done its job, and no
run on this machine can make it: a source build emits an unmarked assembly. It is owed by Stage 5's
release asset, and when that arrives **the installer is the thing that will run into it**.

---
