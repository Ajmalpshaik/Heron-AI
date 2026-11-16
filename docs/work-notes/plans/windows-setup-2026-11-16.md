# A signed Windows Setup for modellers - design (written 2026-11-16)

> **Active - BUILT 2026-11-16, and run on the owner's PC the same day: installed into Revit 2020, 2024, 2026 and 2027, Revit 2020 loaded it, uninstalled cleanly, his own installs put back identical (Group OA rows OA2 to OA10). Owed: Revit 2024, 2026 and 2027 starting it, the Claude Code sitting, signing.** Written for the owner (Ajmal PS) before any
> code, as the task asked: *"Write the design first, and ask the owner before choosing the signing
> certificate or the installer tool."* **He handed the three choices to the session the same day** -
> *"do it the best am not sure for this and you anre my sesior programer or manager or what eve"* - and they
> are recorded as [D-167](../../decisions/D-167.md): **A, wrap** (section 5), **Inno Setup** (section 6), and
> **no certificate bought** - the Setup is built ready to sign and signing is NOT RUN (section 7). The runs
> are [NEEDS-CHECKING Group OA](../../needs-checking/group-oa.md). **Closure:** the Setup installs and uninstalls cleanly on the owner's PC for
> every Revit year present, the gates pass, every year not present is recorded NOT RUN, and what lasts
> has moved into [07](../../07-installation-and-update.md), a decision and a NEEDS-CHECKING group; then
> this note is retired.
>
> **Source of the idea:** idea 12 of [the research note of 2026-11-15](../investigations/2026-11-15-revit-mcp-landscape.md)
> (PR #877, merged the same day this note was written). The behaviours below are taken from that note's list, never from another project's
> script ([31](../../31-studying-the-existing-libraries.md), [D-25](../../DECISIONS.md)).

## 1. What it is for

A modeller downloads **one file**, double-clicks it, and Heron is installed for every Revit on the PC -
no administrator, no terminal, no zip to unzip, no `python tools/heron-ready.py` typed by hand. Windows
names the publisher instead of *"Unknown publisher"*, and Heron appears in **Settings > Apps > Installed
apps** with an **Uninstall** button, like any other program.

In Revit words: today Heron arrives like a family sent as loose files in a zip - it works once you know
where to put them. The Setup is the same family loaded from a signed content pack.

## 2. What Heron already has - the task's premise is out of date

The research note says *"Heron is a Python script"*. **That is not what the code says.** Recorded both
ways, as [AGENTS.md](../../../AGENTS.md) asks: the note's sentence, and the code below.
`HeronInstaller.exe` (the window) and `heron-install` (the command line) already sit on one engine,
[`platform/Heron.Installer`](../../../platform/README.md), and the zip `heron-project.zip` already carries
one payload per product per Revit year, made by
[`tools/build-release-assets.py`](../../../tools/build-release-assets.py).

| Behaviour on the note's list | Heron today | Where |
|---|---|---|
| Per user, no administrator | **Has it** - proven on three releases, no UAC prompt ([07 §10.4](../../07-installation-and-update.md)) | `tools/deploy-addin.ps1` |
| One payload per Revit year | **Has it** - `<product>-<year>.zip`, each Revit year built in its own folder | `Directory.Build.props`, `build-release-assets.py` |
| Will not install while Revit is open | **Has it** - it *waits* and never closes Revit | `InstallEngine.cs` |
| Checks the download before use | **Has it** - every zip against `checksums.txt` | `ReleaseAssets.cs` |
| Verifies after copying | **Has it** - reads the runtime back out of the copied assembly and refuses a mismatch | `deploy-addin.ps1` |
| Keeps the old install to go back to | **Has it, one deep** - `%LOCALAPPDATA%\Heron\install-backup`, `-Rollback` | `deploy-addin.ps1` |
| Heron's Python side, then `heron-ready --check` | **Has it** - from a local build only; v0.1.0 has no Python half | `PythonSetup.cs` |
| Refuses inside a Microsoft Store Claude | **Has it** | `ClaudeAppCopy.cs` |
| **One file to download** | **No** - a zip to unzip, then `HeronInstaller.exe` inside it | - |
| **An entry in Installed apps, with Uninstall** | **No** - the Heron folder is wherever it was unzipped | - |
| **A check for a second copy of Heron first** | **No** - `grep` of `deploy-addin.ps1` for `ProgramData`, `AddInId`, `duplicate`: nothing | - |
| **Puts the old install back by itself when a copy fails half-way** | **No** - it stops and prints the `-Rollback` command | `deploy-addin.ps1` |
| **Adds Heron to Claude Code** | **No** - the modeller opens the Heron folder, whose `.mcp.json` starts Heron; that file also carries one typed `HERON_CLIENT_ID` every PC would share | `.mcp.json` |
| **Signed** | **No** - SmartScreen says *"Windows protected your PC"* | `release.yml`: *"NOTHING HERE IS SIGNED"* |

**So the work is the six *No* rows, not a new installer.** And `heron-ready` is already run by the
installer; what a modeller still does by hand is **install Python itself** - section 4.

## 3. Goals, non-goals, anti-goals

**Goals**

1. One signed file, `HeronSetup-<version>.exe`, published on the GitHub release beside what is there now.
2. It puts the Heron folder in one fixed place for the user - proposed `%LOCALAPPDATA%\Programs\Heron AI`
   - and Heron in Installed apps.
3. It installs the add-in for **every Revit year present**, from the payload it carries - no download, so a
   contractor firewall that blocks GitHub ([D-91](../../decisions/D-91.md)'s consequence) does not block it.
4. It checks for a second copy of Heron before each year, and names it.
5. A failed copy puts the old install back by itself.
6. It offers to add Heron to Claude Code - asked, a backup first, only while Claude is closed.
7. Uninstall takes the add-in out of every Revit year and removes the program folder.

**Non-goals** - not in this work

- **Installing Python.** [Q-39](../../open-questions/answered.md) says Heron never pulls a runtime onto a
  PC unasked; whether a release should ever carry Python is still open in
  [Q-38](../../open-questions/from-master-specification-part-2.md#-q-38--what-is-the-exact-install-command-new).
  The Setup checks for it and, when it is missing, stops with the one `winget` line - as the installer does.
- An all-users install, an `.msi` for an IT department, an automatic update ([07 §7](../../07-installation-and-update.md) rule 1), an App Store package ([D-38](../../decisions/D-38.md)).

**Anti-goals** - it fails even if it installs, if it

- **writes `heron.config` at all.** `write.enabled` (Changes), `admin.enabled`, `publish.enabled` and
  `write.askBeforeKeeping` stay as they are: `false` by default on a new PC
  ([`HeronConfig.cs`](../../../platform/Heron.Core/HeronConfig.cs)), and **byte-for-byte unchanged** on a
  PC that has them. The Setup never turns on Changes, Admin or Publish.
- touches the user's data - `%APPDATA%\Heron` or `%USERPROFILE%\.heron` - on install **or uninstall**
  ([D-94](../../decisions/D-94.md), [D-135](../../decisions/D-135.md)).
- copies files by a second set of rules. The engine decides what goes where
  ([D-93](../../decisions/D-93.md), [D-96](../../decisions/D-96.md)); two copy engines is how one of them goes stale.
- closes Revit or Claude for the modeller, or asks for administrator rights.
- calls a self-signed test file *signed*.

## 4. Constraints found on this PC, 2026-11-16

| | Found | How |
|---|---|---|
| Revit years present | **2020, 2024, 2026, 2027** - each has a Heron install now | `Test-Path "C:\Program Files\Autodesk\Revit <year>\Revit.exe"`, `Addins\<year>\Heron` |
| Not present - **NOT RUN, never passed** | **2021, 2022, 2023, 2025** | the same |
| Revit open now | **Revit 2024** | `Get-Process Revit` |
| The Claude app | **Microsoft Store build** - its private copy of AppData. **The Setup cannot be tested from this chat**; the owner double-clicks it in File Explorer, and both doors refuse inside the app anyway ([Q-63](../../OPEN-QUESTIONS.md)) | `heron-ready --check` section 2a |
| Packaging tool, `signtool`, a code-signing certificate | **none on this PC** - each is a download that needs the owner's typed yes | the same session |
| .NET SDK | 10.0.401 - builds every Revit year | `dotnet --version` |

## 5. Approaches - DECIDED: A, wrap (D-167)

| | What it is | Gives | Costs |
|---|---|---|---|
| **A. Wrap what Heron has** *(recommended)* | A Setup file carries the Heron folder and the per-year payloads, puts the folder in place, then **runs the engine Heron already has** for the years present. Its uninstaller runs the engine's remove, then deletes the folder | Every rule stays in one tested place; the Setup is a thin wrapper; the six gaps are small additions to the engine, so the window and `heron-install` get them too | One new tool to make the Setup file (section 6), and a new decision naming the Setup as the modeller's door - amending D-96's *"HeronInstaller.exe, and only"* |
| **B. Grow `HeronInstaller.exe` into the Setup** | No new tool: the installer carries its own payload, copies itself into place, writes the Installed-apps entry and removes itself on uninstall | No new tool or language in the repository | More C# Heron owns and must test - self-extracting, self-removing - for what a Setup tool does already |
| **C. Replace it** - a new Setup with every rule inside it, shaped like the research note's example | One program | **Two install engines**, against D-93, D-94 and D-96 and the brain's *one location, never two*. Not recommended |

**Why A.** The engine is where every install lesson so far was paid for - the wrong-runtime guard of
2026-09-20, the rollback of 2026-09-19, the Store-Claude refusal. A wrapper keeps them; a replacement
must learn them again.

## 6. The tool that makes the Setup - DECIDED: Inno Setup (D-167)

Only if A. The facts of each are to be read from the maker's own page at the time, not from memory.

| | Inno Setup | WiX (makes an `.msi`) |
|---|---|---|
| What | A free program that turns one script into one `Setup.exe` | A .NET tool that builds a Windows Installer package |
| Per user, no administrator | Built in | Possible, but per-user `.msi` is known to be awkward |
| Uninstall entry and uninstaller | Built in | Built in |
| Signing | A setting in its script | A step after the build |
| For an IT department pushing it to many PCs | Possible | What they expect |
| New in the repository | One script in Inno's own language | XML files and a .NET tool |

**Recommended: Inno Setup** - the smallest thing that does the job, per user by design. WiX is the
better pick only if contractor IT departments deploying Heron by Intune or SCCM is a near goal, which no
decision says.

## 7. The certificate - DECIDED: none bought; built ready to sign, signing NOT RUN (D-167)

A new decision is owed whichever way: [D-96](../../decisions/D-96.md) refused a certificate for its
yearly cost, [D-38](../../decisions/D-38.md) says nothing is signed, [D-91](../../decisions/D-91.md) says a
release is signed, and [Q-77](../../open-questions/tier-2.md) asks which wins. The shapes, prices and who
may buy each to be checked on the seller's page on the day:

1. **A cloud signing service** (Microsoft's, among others) - a monthly fee and an identity check; whether
   it serves a person in his country is the first thing to check.
2. **A bought certificate** - yearly, the key on a hardware token.
3. **Free signing for open-source projects** - needs the repository public, and [D-07](../../decisions/D-07.md)
   keeps it private.
4. **A self-made certificate, for testing only** - trusted on his PC alone; any other PC still warns. Recorded
   as *signed for test only*, never as *signed*.

**D-96's fear, answered in part:** a signature made with a **timestamp** stays valid after the certificate
expires, so a lapsed renewal stops *new* signing, not installs already made. Confirm against Microsoft's
Authenticode documentation when the decision is written.

**Signing covers:** the Setup, its uninstaller, `HeronInstaller.exe`, `heron-install.exe`. Signing Heron's
own DLLs as well - Revit 2027's journal writes `AddInCodeSigningStatus: Unsigned`
([07 §10.9](../../07-installation-and-update.md)) - is a separate yes.

## 8. The design, assuming A - what the builder makes

**The payload** is what `build-release-assets.py --out dist` makes today: the Heron folder, one zip per
product per Revit year for every year Heron builds, `checksums.txt`. The Setup carries all of it.

**Install, in order**

1. Refuse inside a Store Claude's copy of AppData (the engine's rule, reused).
2. Python: if `python` does not answer 3.11 to 3.14, stop with the `winget` line - the engine's rule.
3. Unpack the Heron folder beside the old one, check it against `checksums.txt`, then swap them; on any
   failure put the old folder back. The old one is deleted only after step 6 passes.
4. Hand over to the engine: `heron-install --from "<program folder>"` for every Revit year found. It waits
   while a Revit it needs is open - the Setup's page says which, by year - and never closes it.
5. **New in the engine, both doors:** before each year, look for any other `Heron.addin`, or the same
   add-in id, in the other per-user folders, in `%ProgramData%\Autodesk\Revit\Addins\<year>` (2020 to 2026)
   and `C:\Program Files\Autodesk\Revit\Addins\<year>` (2027 on, from the
   [`revit-version-support`](../../../.claude/skills/revit-version-support/SKILL.md) skill). Found: that year
   is refused with the path named; nothing for it is changed.
6. **New in `deploy-addin.ps1`:** a copy that fails half-way puts the backup back by itself, then says so.
7. Verify: each year's runtime and file hashes read back, `heron-ready --check` READY, a report per year.
8. **Claude Code - asked, a tick on the tasks page, ON by default** (first written on, turned off for the
   memory each open chat costs, then back on by the owner's word on 2026-11-16 - a new modeller must never
   have to find the hidden program folder; D-167 item 5). Beside it, a tick for Heron's Python side, also on
   (D-167 item 5a). The Claude step runs only when no Claude is
   running; `~/.claude.json` copied aside first; one entry, `heron`, at user scope, with the absolute path
   to the server and a `HERON_CLIENT_ID` made from the PC and user name; read back; no other key touched.
   **Where Claude Code keeps a user-scope server is to be read from its current documentation at build
   time**, not from this note. Opening the Heron folder keeps working either way.
9. The Installed-apps entry, under the user's own registry (`HKCU`).

**Uninstall:** the engine's remove for every year Heron is in, the Claude entry if the Setup added it
(backup first), then the program folder. `%APPDATA%\Heron`, `%USERPROFILE%\.heron` and the install backup
are left.

## 9. Testing on the owner's PC - what *done* means

**Before anything:** copy and hash each `Addins\<year>\Heron` for 2020, 2024, 2026 and 2027 into the
session's scratch folder; hash `heron.config` and `~/.claude.json`. These copies are how his own installs
come back at the end.

| # | Run | Who | Look for |
|---|---|---|---|
| S1 | Double-click the Setup in File Explorer, Revit closed | owner | No UAC prompt; the publisher named (or *test-signed*, per section 7); Heron in Installed apps |
| S2 | Read each year's folder back | session | Runtime matches the table in the skill; files match the payload |
| S3 | `heron.config` and the data folders | session | **Hash unchanged** - Changes, Admin, Publish untouched |
| S4 | Start each Revit, **one at a time** (16 GB) | owner | Its journal: `API_SUCCESS ... Heron AI`; the Heron tab |
| S5 | A second copy: a scratch `Heron.addin` in another per-user folder | session | That year refused, the path named; the copy removed after |
| S6 | Revit 2024 open while the Setup runs | owner | It waits, names 2024, never closes it |
| S7 | Uninstall from Installed apps | owner | Every year's Heron gone; data hashes unchanged; entry gone |
| S8 | Claude registration, with Claude closed | owner | One `heron` entry, backup beside it; `/mcp` says connected in any folder |
| S9 | Put his own installs back from the copies | session | Hashes match the *before* |
| - | **2021, 2022, 2023, 2025** | - | **NOT RUN - no Revit of that year on this PC** |

**The four gates** - `check-docs.py`, `check-metadata.py`, `check-structure.py`, `check-package.py` - and
`git diff --check` before anything is claimed. Results go into a new NEEDS-CHECKING group (`tools/next-id.py`
for its letters).

## 10. Left to the builder

- The program folder's exact name, the Setup's page wording, and the Installed-apps fields - in plain words,
  from the [brand kit](../../brand/README.md) for colours and the icon.
- Whether the payload stays one Setup for every product, or one per product; the engine offers all by default.
- A metadata header for the Setup script, if [`check-metadata.py`](../../../tools/check-metadata.py) is
  widened to read it.
- Where the Setup is built - on the owner's PC first; in `release.yml` once signing is decided.
