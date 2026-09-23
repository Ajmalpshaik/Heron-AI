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
| **AC1** | Push a tag — `git tag v0.1.0 && git push origin v0.1.0` — and watch the **Release** workflow | It builds **24 assets**, writes `checksums.txt`, and creates a **draft** release carrying all of them plus `heron-products.json`. The job summary lists every checksum. **A draft is deliberate**: nothing is signed yet, so nobody should be able to find and install it by accident |
| **AC2** | On a PC or folder with **no build at all**, run `HeronInstaller.exe`, tick a Revit, press Install | It **downloads** rather than refusing, and the tab appears in that Revit after a restart. This is the first time anything has ever been fetched from a release. Watch for: the window freezing while it downloads — it should not, the engine runs off the window's thread |
| **AC3** | Corrupt one asset in the release — re-upload a truncated zip — and install again | It **refuses**, says the file *"did not arrive whole"*, and `%APPDATA%\Autodesk\Revit\Addins\<ver>\` is **untouched**. Check the folder afterwards rather than trusting the message |
| **AC4** | Turn networking **off**, then install | It says it **could not reach the internet**, not "error". Then block `github.com` at the firewall instead and check the message changes to the one about IT and the proxy |
| **AC5** | With a build present in the checkout, install as usual | It uses the **local build** and downloads nothing. The line under *from* says the local folder. This is the half that stops a published version landing on top of what you just compiled |

**`AC3` is the one worth doing carefully.** Every other row fails loudly; that one fails by installing
something it should not have, and the only way to see it is to look in the Addins folder afterwards.

---
