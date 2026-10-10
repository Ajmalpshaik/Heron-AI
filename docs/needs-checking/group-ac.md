# Needs checking — Group AC

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## 2026-09-21 — STAGE 5 IS BUILT, and nothing has ever been downloaded from GitHub

The stage said *"the installer reads the manifest from a named release"*. **There were no releases and
nothing that could make one**, so the sentence described a thing that did not exist. Both halves are
built now. **No release has been published**, so the word PROVEN is not used here either.

### What was built

| | |
|---|---|
| `tools/build-release-assets.py` | one zip per product per Revit release, the product list beside them, `checksums.txt` over all of it written **last** |
| `.github/workflows/release.yml` | tag-triggered, calls the tool, publishes a **draft** — Stage 8 has not happened and nothing is signed |
| `ReleaseAssets.cs` | asset names, checksum reading, and the sentence for every failure. Pure |
| `ReleaseDownload.cs` | fetch, **verify in memory**, unpack, refuse a zip that escapes its folder |
| `-FromFolder` in `deploy-addin.ps1` | a verified download goes through the **same proven copy rule** a local build does |

### What was actually run, and what it said

| | |
|---|---|
| **PASS** | The builder, for real: **24 assets, 54 MB, checksums over 25 files, exit 0** |
| **PASS** | **Verified at the binary level, not from the success line** — `TargetFrameworkAttribute` read out of six zipped assemblies. 2020 `net472`; 2021 and 2024 `net48`; 2025 and 2026 .NET 8; 2027 .NET 10. All correct. No `RevitAPI*` or `AdWindows*` in any zip |
| **PASS** | **The download path against a live local HTTP server** — real ports, real zip bytes. Verified and unpacked; a changed file refused **with nothing written to disk**; a file missing from `checksums.txt` refused; a release with no checksums refused; 404, 403 and a dead host each with their own sentence; a zip escaping its folder refused whole |
| **PASS** | The ten gates, and `tests/test_release_assets.py` |
| **NOT RUN** | **The workflow has never executed** and no release exists. Nothing has been fetched from GitHub by anything, ever |
| **NEEDS REAL REVIT** | `AC1` to `AC5` below |

**Seen to fail, which is what makes them checks:**

| what was broken | checks that went red |
|---|---|
| the configuration set to `Debug` | **1** |
| a `PLANNED` skip made silent | **3** |
| Autodesk's assemblies redistributed | **2** |
| `checksums.txt` written before the assets | **2** |
| the release published not-draft | **1** |
| the download's checksum check removed | **7** |
| the check moved to **after** the unpack | **1** — and it is the one that proves nothing lands on disk |
| the zip-escape guard removed | **2** |
| 403 no longer told apart from any other refusal | **1** |
| a file missing from `checksums.txt` allowed | **2** |

**And one check was found to be testing a variable name rather than a rule.** *"The window takes the
configuration from the deployer"* matched the literal `deployer.Configuration`, so renaming that
variable turned it red against a correct file. It now asks the real thing — that the window **reads**
a configuration and **never writes one of its own** — and was shown to still go red when the window
writes `"Release"` itself. **Strictly stronger than it was.**

### Two decisions taken here, and why

**Where the release lives is DATA.** `platform/heron-products.json` gained a `source` block naming the
owner and repository. A repository that is renamed is then a line in a file rather than a rebuild of
the installer — the same reasoning as `R-3`, and nothing is executed from it: it only ever becomes a
URL.

**A build on this PC always wins.** The window downloads only when **nothing at all** has been built
here. Downloading on top of a half-built checkout would install a published version over the one a
developer just compiled.

### THE HALF THIS DOES NOT FIX, said plainly

The assets carry **the Revit plugin**. Nothing carries **the brain** — the fragments, the MCP server,
the knowledge store. So a modeller who installs from a release gets a Heron tab whose **Connect button
opens a pipe nobody answers**. That is
[Q-PE-13](../work-notes/plans/plugin-extension/03-open-questions.md) and it is unanswered. **Stage 5 is
one of the two halves of "give it to somebody", not the whole of it.**

### Rows for Ajmal's PC — five

**`AC1` comes first.** The other four ask what happens when a release is downloaded, and until one is
published there is nothing to download.

| ID | Do this | Pass looks like |
|---|---|---|
| ~~**AC1**~~ | ~~Push a tag and watch the **Release** workflow~~ | **DONE 2026-10-08.** `v0.1.0` pushed at `a88b0bc2`, every CI gate green on that commit; [the Release run](https://github.com/Ajmalpshaik/Heron-AI/actions/runs/37700010195) built a **draft** carrying **27 files** - 24 product zips, `heron-project.zip` with both installers, `heron-products.json` and `checksums.txt` - and the draft's `heron-project.zip` and product list matched their checksums once downloaded. Then **published on the owner's word** - see 2026-10-10 below |
| **AC2** | On a PC or folder with **no build at all**, run `HeronInstaller.exe`, tick a Revit, press Install | It **downloads** rather than refusing, and the tab appears in that Revit after a restart. This is the first time anything has ever been fetched from a release. Watch for: the window freezing while it downloads — it should not, the engine runs off the window's thread |
| **AC3** | Corrupt one asset in the release — re-upload a truncated zip — and install again | It **refuses**, says the file *"did not arrive whole"*, and `%APPDATA%\Autodesk\Revit\Addins\<ver>\` is **untouched**. Check the folder afterwards rather than trusting the message |
| **AC4** | Turn networking **off**, then install | It says it **could not reach the internet**, not "error". Then block `github.com` at the firewall instead and check the message changes to the one about IT and the proxy |
| **AC5** | With a build present in the checkout, install as usual | It uses the **local build** and downloads nothing. The line under *from* says the local folder. This is the half that stops a published version landing on top of what you just compiled |

**`AC3` is the one worth doing carefully.** Every other row fails loudly; that one fails by installing
something it should not have, and the only way to see it is to look in the Addins folder afterwards.

---

## 2026-10-08 — THE INSTALLER WAS IN NO RELEASE, and a download could not have used it

A new person searched the repository and its releases for `HeronInstaller.exe` and found it in
neither. **Both were right**: `.gitignore` keeps the built exe out of the code on purpose, and
`tools/build-release-assets.py` never built one, so no release could have carried it. Reading the
route through found two more gaps behind it, all three in
[row 5b-371](../fragment-issues/section-5b-rows-176-200.md):

| | |
|---|---|
| **The installer was in no asset** | Now `HeronInstaller.exe` and `heron-install.exe` sit at the top of `heron-project.zip`, published **self-contained**, so a PC needs no .NET installed first |
| **Alone, the exe cannot run** | It looks for `platform\heron-products.json` above itself and drives `tools\deploy-addin.ps1` and `tools\HeronRevit.ps1` from there. The zip now carries both scripts, so unzipped it is already that folder |
| **A download was refused after its copy** | `deploy-addin.ps1 -FromFolder` read each `.addin` from `revit\<project>\`, which the zip leaves out. It now deploys the `.addin` the asset carries |

**What this changes in the rows above.** `AC1`'s job now also builds the two installers, and its
summary lists `heron-project.zip` among the checksums. **`AC2` is run from `heron-project.zip`
unzipped into an empty folder** — that is what *"no build at all"* means for a person who
downloaded it. **And `AC2` needs a PUBLISHED release**: the installer fetches from
`releases/latest/download`, which GitHub serves only from the newest published, non-prerelease
release. `AC1` makes a draft, so `AC2` cannot pass against the release `AC1` makes until somebody
publishes it — the owner's decision.

### What was run on the owner's PC, 2026-10-08

| | |
|---|---|
| **PASS** | `python tools/build-release-assets.py` on Windows: **24 product zips and `heron-project.zip`, checksums over 26 files, exit 0**. Both installers at the top of the zip, each starting `MZ`; `tools/` in it holds the three named files and nothing else |
| **PASS** | Unzipped into an empty folder, `heron-install.exe --from <the built folder> --list` found Revit 2020, 2024 and 2027 through the zip's own `tools\HeronRevit.ps1` and offered the AI Bridge |
| **PASS** | **The defect, then the fix, in the same empty slot** (Revit 2021's add-in folder; no Revit 2021 on this PC). The OLD `deploy-addin.ps1` in that layout refused with *"...Heron.addin is missing ... The Heron files are incomplete"* **after copying the assemblies** - the half-install was removed by hand. The NEW one, same folder and same asset: exit 0, manifest naming `Heron\Heron.Revit.Addin.dll`. The slot was then put back exactly as it was |
| **PASS** | **The real route**: `heron-install.exe --from ... --releases 2027 --products heron-bridge`, run from the unzipped download with Revit 2027 closed - *"1 installed, 0 failed"*, `Heron.addin` names `Heron\Heron.Revit.Addin.dll`. **The release build is what Revit 2027 loads now**; `tools\deploy-addin.ps1 -RevitVersion 2027 -Rollback` puts back the one it replaced |
| **PASS** | `HeronInstaller.exe` opened from the unzipped folder titled *"Heron Installer   v0.1.0"* - the version is read from the product list, so it found it. **Alone in an empty folder** it said *"The Heron product list is not at ...\alone\platform\heron-products.json, so there is nothing to install"* - the reason it ships inside the zip |
| **PASS** | **The Release workflow on GitHub's runner**, rehearsed from the branch with no tag ([run 37695341811](https://github.com/Ajmalpshaik/Heron-AI/actions/runs/37695341811)): built and uploaded the assets, **published nothing** - the publish step skipped, as a rehearsal must. Its `heron-project.zip` is 105 MB, both installers at the top, `tools/` holding the three named files |
| **PASS** | **The Linux-built download, on Windows**: unzipped, `heron-install.exe --from` the downloaded assets put the AI Bridge into Revit 2027 - the deployed `Heron.Revit.Addin.dll` is the asset's own, same time and size - and its window opened titled *"Heron Installer   v0.1.0"* |
| **PASS** | **Revit 2027 started with that build** and, 51 s in, had `Heron.Revit.Addin.dll`, `Heron.Core.dll` and `Heron.Bridge.dll` loaded from `Addins\2027\Heron` - read from the process's module list, no model open. The tab was not looked at and the bridge was not pinged; Revit was closed again |
| **NOT RUN** | A published release, so `AC2` to `AC6` |

**Two things seen, recorded and not changed here.** With Revit 2020 and 2024 open and only 2027
ticked, `heron-install` printed *"Revit 2024 and 2020 is open right now. Close Revit before
installing ... it waits for you"* - and then, correctly, did not wait, since neither was being
installed into. And the lone exe's message says *"Reinstall Heron"* where the useful sentence is
*"run it from the folder heron-project.zip unzipped into"*; it is the product list's own message,
shared by every caller.

| ID | Do this | Pass looks like |
|---|---|---|
| **AC6** | On a PC whose newest Revit is 2024 or older — so no .NET 8 came with it — download `heron-project.zip` from the published release, unzip it, double-click `HeronInstaller.exe` | SmartScreen warns that the publisher is unknown, and after *Run anyway* **the window opens with no request to install .NET**. A request to install a runtime is the FAIL this row exists for: the self-contained build is the whole of the fix for it |

---

## 2026-10-10 — v0.1.0 IS PUBLISHED, and the installer has fetched from it

**The owner chose to publish** on 2026-10-08, when asked whether the new teammate should get Heron from a
public release: *"Publish v0.1.0 now"*. **Two written rules said a release is SIGNED** -
[docs/07](../07-installation-and-update.md) section 1a, and `brain/heron_tag.py`, which refuses an unsigned
artefact rather than warning. Both still say so and neither was edited: this release is the owner's
exception, made knowingly, and Stage 8 - signing - is still what closes it. The release notes say it is
unsigned and that the README's *please do not install this yet* still stands.

| | |
|---|---|
| **PASS** | `releases/latest/download/` answers 200 for the product list, `checksums.txt` and a product zip - the address the installer builds |
| **PASS** | **The download route, for the first time**: the published `heron-project.zip` unzipped into an empty folder, `heron-install.exe --releases 2027 --products heron-bridge` with no `--from` - *"Files come from Heron's published release"*, *"1 installed, 0 failed"*, and *"This is the newest published version (0.1.0)"* |
| **NOT SEEN** | Revit 2027 loading the PUBLISHED bytes. Started twice; both times it stopped at Autodesk licensing - licensing agents started, no window, no CPU - before any add-in loads, very likely because the earlier test runs closed it by force. The rehearsal build of the same code had loaded (above). Closed again by its process id |
| **NOT RUN** | `AC2` as written - the window's own Install, then the tab seen - and `AC3` to `AC6` |

---
