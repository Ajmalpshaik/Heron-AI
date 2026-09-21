# Heron Plugin Extension — the requirement note

> **Type:** Operational work note. **Not specification.** Where a sentence here disagrees with the
> [Constitution](../../../../HERON_CONSTITUTION.md), the [Golden Rules](../../../14-golden-rules.md) or
> [DECISIONS.md](../../../DECISIONS.md), **those win and this note is out of date.**
> **Status:** **Active — nothing built from it yet.** Opened 2026-09-20. **Owner:** Ajmal PS.
> **Read [`00-structure.md`](00-structure.md) first.**

---

## 1. What this note is, and what it is not

It is **one page that says what the Heron installer has to do**.

It is **not** a new specification. Every row in §5 cites where it came from — a document, a file, or a
dated instruction from the owner. **A row with no source is not a requirement, it is an opinion, and it
does not belong here.**

---

## 2. The one sentence

> A BIM modeller downloads one file, double-clicks it, ticks the Heron products they want, presses
> Install, and those tabs are in Revit the next time it starts — **with no administrator rights and no
> help from IT**.

## 2a. Three ways in, one engine

There are **three routes**, and they are not three installers — they are three front doors onto one
engine ([S7](00-structure.md)). Only route C shows a window.

| | Route | The user does | Files come from |
|---|---|---|---|
| **A** | **Cloud** | gives the repository; it installs itself | the cloud plan |
| **B** | **Repo download** | sets the project location, runs the setup file | **already in the repo** — no download |
| **C** | **Manual installer** | double-clicks it, ticks, presses Install | a signed GitHub release |

Everything in §3 and §4 below describes **route C**, because it is the only one with a window. Routes A
and B reach the same engine with their choices already decided, so every rule in §5 binds all three.

---

## 3. What the user sees

One window. One list. Two buttons.

```text
+--------------------------------------------------------------+
|  Heron Installer                                    v0.1.0   |
+--------------------------------------------------------------+
|                                                              |
|  Revit versions found on this PC                             |
|    [x] Revit 2024        [x] Revit 2025                      |
|    [ ] Revit 2026                                            |
|                                                              |
|  Products                                                    |
|    [x] Heron                                      Installed  |
|          [x] AI Bridge connector                  Installed  |
|          [ ] All tools                            --         |
|    [ ] Heron Doc          Annotation and sheets   --         |
|    [ ] Heron MEP          Ducts, pipes, sizing    --         |
|                                                              |
|  Install location   %APPDATA%\Autodesk\Revit\Addins\         |
|                     (per user - no admin needed)             |
|                                                              |
|  [!] Close Revit before installing.                          |
|                                                              |
|                              [ Install ]     [ Close ]       |
+--------------------------------------------------------------+
```

**The list is read from a file, not written in the code.** Adding *Heron Structure* next year must be a
line in a manifest and a release asset — **never a rebuild of the installer**. That is the whole point
of the design; see [R-3](#5-the-requirements).

**The `Heron` tab is the only one that opens into two ticks** — the AI Bridge connector and the tools,
either or both ([S3](00-structure.md)). Every other tab is one tick, whole tab. Ticking the tools
without the connector is a **supported install**, not a degraded one.

**An already-installed piece shows `Installed` and stays tickable.** Pressing Install on it replaces it
([S8](00-structure.md)) — there is no separate Update or Repair button.

---

## 4. What Install actually does

For each ticked product, for each ticked Revit version:

```text
1.  Check Revit is closed                      -> refuse, with the version named, if not
2.  Fetch the product                          -> route C downloads and verifies; route B has it already
3.  Detect an existing install                 -> and say in the report that it is being replaced
4.  Back up whatever is already there          -> so Rollback has something to restore
5.  REMOVE the old files, then copy the new    -> replace, never merge two versions in one folder
6.  Copy the product's WHOLE FOLDER of files   -> into ...\Addins\<version>\<Product>\  (R-40, R-41)
6a. Place <Product>.addin                      -> at ...\Addins\<version>\<Product>.addin
7.  Rewrite the <Assembly> line in the manifest -> to the real deployed path
8.  Verify the files landed and parse          -> the checks tools/check-package.py already makes
9.  Report, per product, per version           -> installed / replaced / skipped / failed and WHY
```

**Step 5 is the one to get right, and yesterday's version of it was wrong.** It said *remove, then
copy*. A remove **fails** while Revit holds the assembly open, and Heron's own "is Revit closed" check
can be wrong — a second Revit on another desktop, a scanner holding a file. So: **rename the old folder
aside, install clean, sweep the set-aside folders on a later run.** A locked file cannot be deleted but
can be renamed. [R-38](#the-lessons-from-aj-tools), from [L2](04-lessons-from-aj-tools.md).

**Step 6 changed too.** A product is a **folder of files**, not two files, and each product gets its own
folder rather than sharing one with every other Heron product
([R-40](#the-lessons-from-aj-tools), [R-41](#the-lessons-from-aj-tools)).

Steps 1, 3, 4, 5 and the refusal in step 1 **already exist** in
[`tools/deploy-addin.ps1`](../../../../tools/deploy-addin.ps1). The installer should **drive that
logic, not re-implement it** — two copies of a deploy rule is two rules that drift.

---

## 5. The requirements

**Status words:** `MUST` — the installer is not finished without it. `SHOULD` — wanted, may follow.
`LATER` — recorded so it is not forgotten, deliberately not in the first release.

### The window

| # | Requirement | Source | Status |
|---|---|---|---|
| R-1 | One main window lists every Heron product as a **tickable checkbox** | Owner, 2026-09-20 | MUST |
| R-2 | Each product shows its **current state** — Installed / Not installed / Update available | Owner, 2026-09-20 ("future expansion") | MUST |
| R-3 | The product list is **read from a manifest**, so a new product needs **no installer rebuild** | Owner, 2026-09-20 ("designed for future expansion") | MUST |
| R-4 | Custom Install is at **tab level**. Panels inside a tab are not user-choosable | Owner, 2026-09-20; [S3](00-structure.md) | MUST |
| R-5 | The window states plainly that install is **per-user and needs no admin** | [`docs/07 §5`](../../../07-installation-and-update.md) | SHOULD |
| R-6 | Wording follows the add-in's rules — plain English, says what happened and what to do next | [`docs/14`](../../../14-golden-rules.md) | MUST |

### Revit versions

| # | Requirement | Source | Status |
|---|---|---|---|
| R-7 | The installer **detects which Revit versions are installed** and lists only those | Not in the owner's brief — **added here**; without it the installer cannot know where to copy | MUST |
| R-8 | The user ticks **which versions** to install into | Follows R-7; a modeller with 2020 and 2024 open on one PC is normal | MUST |
| R-9 | ~~Supported set is **2020 → 2027**, eight releases~~ **Superseded by [R-39](#the-lessons-from-aj-tools) 2026-09-21** — a typed list goes stale silently. The set is whatever the package carries | [`docs/16`](../../../16-version-support-strategy.md); [L3](04-lessons-from-aj-tools.md) | MUST |
| R-10 | A product that does not support a version is **greyed out with the reason**, never silently skipped | [`docs/14`](../../../14-golden-rules.md) — an error must say what to do next | MUST |

### Getting the files

| # | Requirement | Source | Status |
|---|---|---|---|
| R-11 | Files come from a **signed, versioned GitHub release** | Owner, 2026-09-20; [`docs/07 §1a`](../../../07-installation-and-update.md) | MUST |
| R-12 | A download is **verified before it is used** — a corrupt or wrong file must never reach the Addins folder | [`docs/12`](../../../12-security-and-permissions.md) | MUST |
| R-13 | The installer **never executes what it downloads** in order to decide what to install | [Golden Rule 19](../../../14-golden-rules.md) — nothing Heron reads may raise its own permission level | MUST |
| R-14 | If the download fails, the installer says **why** — offline, blocked, release missing — not "error" | [`docs/14`](../../../14-golden-rules.md) | MUST |
| R-15 | Offline install from bundled files | [Q-PE-5](03-open-questions.md) | LATER |

### Installing

| # | Requirement | Source | Status |
|---|---|---|---|
| R-16 | Install is **per-user**, `%APPDATA%\Autodesk\Revit\Addins\<version>\`, no admin | [`docs/07 §5`](../../../07-installation-and-update.md); already done by `deploy-addin.ps1` | MUST |
| R-17 | The installer **refuses while Revit is running**, naming the version | `deploy-addin.ps1` already does this; assemblies loaded into Revit cannot be unloaded | MUST |
| R-18 | Whatever is replaced is **backed up first**, so rollback has something to restore | `deploy-addin.ps1` `-Rollback` | MUST |
| R-19 | A failed product **does not stop the others**, and the report says which failed | Follows R-1 — a list of products implies a list of results | MUST |
| R-20 | The installer **never touches** `%APPDATA%\Heron` — the user's brain, skills and audit log | [`HeronPaths.cs`](../../../../platform/Heron.Core/HeronPaths.cs); [`docs/06 §2`](../../../06-heron-platform.md) | MUST |

### Uninstall and update

| # | Requirement | Source | Status |
|---|---|---|---|
| R-21 | **Uninstall** is the same window — untick a product and apply | Owner, 2026-09-20 (one main window) | MUST |
| R-22 | Uninstall removes the product's files **and nothing else**. The user's data survives | [`HeronPaths.IsSafeToDelete`](../../../../platform/Heron.Core/HeronPaths.cs) | MUST |
| R-23 | **Update** replaces a product in place and keeps the user's settings | [`docs/07 §7`](../../../07-installation-and-update.md) | MUST |
| R-23a | There is **no separate Update or Repair button**. Install detects an existing version and replaces it | Owner, 2026-09-21; [S8](00-structure.md) | MUST |
| R-23b | ~~Replacing **removes the old files first, then copies**~~ **Corrected by [R-38](#the-lessons-from-aj-tools) 2026-09-21** — a delete FAILS while Revit holds the DLL open. Rename aside, then install clean | Follows R-23a; [L2](04-lessons-from-aj-tools.md) | MUST |
| R-24 | Rollback is **tested**, not merely implemented | [`docs/07 §7`](../../../07-installation-and-update.md) rule 5; `brain/heron_update.py` refuses a release without `rollback_tested` | MUST |

### The three routes

| # | Requirement | Source | Status |
|---|---|---|---|
| R-28 | **Route A — cloud.** A new person gives the repository and everything installs with no further input | Owner, 2026-09-21 | MUST |
| R-29 | **Route B — repo download.** The repo carries every file **and** a setup file; the user sets the project location, runs it, and it configures itself | Owner, 2026-09-21 | MUST |
| R-30 | **Route B downloads nothing.** The files are already in the repo, so it works with no internet | Follows R-29; and it is the answer to [Q-PE-5](03-open-questions.md) for anyone who took the repo | MUST |
| R-31 | All three routes drive **one engine**. No install rule exists in more than one place | [S7](00-structure.md); a rule in three installers is three rules and two go stale | MUST |
| R-32 | Routes A and B are **not silent**. Each reports what it installed, into which Revit versions, and what it skipped | [`docs/14`](../../../14-golden-rules.md) — an automatic install that says nothing cannot be checked | MUST |

### The Heron tab

| # | Requirement | Source | Status |
|---|---|---|---|
| R-33 | The `Heron` tab offers **two ticks** — AI Bridge connector, and all tools. Either, or both | Owner, 2026-09-21; [S3](00-structure.md) | MUST |
| R-34 | Ticking **tools without the connector** is a supported install. No AI is installed, and the tools work | Owner, 2026-09-21 | MUST |
| R-35 | Both pieces build into the **same `Heron` tab**, and neither may assume it is loaded first | Revit's `CreateRibbonTab` throws when the tab exists; `HeronApplication.BuildRibbon()` already catches it | MUST |
| R-36 | `Heron Doc`, `Heron MEP` and every future tab have **one tick each** — no sub-choice | [S3](00-structure.md) | MUST |

### The Settings panel

| # | Requirement | Source | Status |
|---|---|---|---|
| R-42 | The `Heron` tab carries a **Settings panel**, installed whenever any Heron product is | Owner, 2026-09-21; [S9](00-structure.md) | MUST |
| R-43 | From it the user turns **tabs and panels on and off** — hiding what is installed, not removing it | Owner, 2026-09-21 | MUST |
| R-44 | The choice is **saved under `%APPDATA%\Heron`** and survives every install, update and replace | [S9](00-structure.md); [R-20](#installing) | MUST |
| R-45 | Hiding a panel **never uninstalls it**. Un-hiding needs no installer and no download | [S9](00-structure.md) — install decides disk, settings decides screen | MUST |
| R-46 | The Settings panel reads its list from **the same manifest the installer uses**, never a list written into the window | [Stage 1](02-implementation.md); a second list is a second thing to keep in step | MUST |
| R-47 | A panel hidden by a user who then installs a **new** product: the new one is **visible by default** | Nothing has been said; **assumed here** — a user who installs a thing expects to see it. Reverse it if wrong | SHOULD |

### The lessons from AJ Tools

Each row cites the lesson it came from in [`04-lessons-from-aj-tools.md`](04-lessons-from-aj-tools.md).
**Three of them correct a row written on 2026-09-20.**

| # | Requirement | Source | Status |
|---|---|---|---|
| R-37 | Every file is **cleared of its download mark**, before the copy and again on the copies. A marked DLL makes Revit refuse the add-in | [L1](04-lessons-from-aj-tools.md) | MUST |
| R-38 | Replacing **renames the old folder aside**, installs clean, and sweeps set-aside folders on a later run. **Never delete first** — the delete fails while Revit holds the file | [L2](04-lessons-from-aj-tools.md) — **corrects [R-23b](#uninstall-and-update)** | MUST |
| R-38a | The sweep is **best-effort**. A folder still held open fails to delete and is swept next time; it can never fail an install | [L2](04-lessons-from-aj-tools.md) | MUST |
| R-39 | Which Revit versions can be installed is read from **what the package contains**, never from a typed list | [L3](04-lessons-from-aj-tools.md) — **supersedes [R-9](#revit-versions)**; and [AGENTS.md](../../../../AGENTS.md) *never type a number a command can derive* | MUST |
| R-40 | A product ships **one payload folder per Revit release**, carrying that release's build **and everything the runtime needs beside it** | [L4](04-lessons-from-aj-tools.md) — .NET 8+ fails on a missing dependency manifest | MUST |
| R-41 | Each product installs into **its own folder**, never a shared one. Two products carrying different versions of the same helper assembly must not overwrite each other | [L5](04-lessons-from-aj-tools.md) — **corrects §4** | MUST |

### Shipping it

| # | Requirement | Source | Status |
|---|---|---|---|
| R-25 | The installer is **code-signed** | Not in the owner's brief — **added here**. Unsigned, SmartScreen and contractor IT block it, and the target user is exactly the one who cannot override that | MUST |
| R-26 | The installer carries the five Heron metadata fields | [`docs/29`](../../../29-metadata-standard.md) | MUST |
| R-27 | Every install, uninstall and update writes an **audit line** | [`HeronAudit.cs`](../../../../platform/Heron.Core/HeronAudit.cs); [Golden Rule 10](../../../14-golden-rules.md) | SHOULD |

---

## 6. Constraints that are not negotiable

These are not preferences. Breaking any one of them makes the installer unusable by the person it is for.

1. **No administrator rights.** Ever. The moment it needs admin, it needs a ticket, and it dies there.
2. **Revit must be closed.** Not a warning to click past — a refusal.
3. **The user's brain is untouchable.** `%APPDATA%\Heron` holds skills, learned patterns and the audit
   log. An installer that can reach it is an installer that can destroy a year of work.
4. **Nothing downloaded is ever executed to decide what to install.** A manifest is read as **data**.
5. **One product cannot break another.** Separate manifests, separate DLLs, separate tabs.

---

## 7. Out of scope for this note

| | Why |
|---|---|
| What buttons go in Heron Doc and Heron MEP | [Q-PE-3, Q-PE-4](03-open-questions.md) — needed before those products build, **not** before the installer does |
| A public marketplace of community packages | [`docs/06`](../../../06-heron-platform.md) already defers it, and it needs a legal position first |
| Installing Claude Code, .NET or the Revit API | The user installs Claude Code once; `docs/07` covers it |

---

## 8. Closure condition

Every row in §5 is either **DONE with evidence** or **withdrawn with a reason**. The durable
requirements then move to [`docs/07`](../../../07-installation-and-update.md), and **this file is
deleted.**
