# Heron Plugin Extension — the requirement note

> **Type:** Operational work note. **Not specification.** Where a sentence here disagrees with the
> [Constitution](../../../../HERON_CONSTITUTION.md), the [Golden Rules](../../../14-golden-rules.md) or
> [DECISIONS.md](../../../DECISIONS.md), **those win and this note is out of date.**
> **Status:** **Active.** Nothing in section 5 is DONE yet. What exists is the product manifest
> ([R-3](#5-the-requirements), [Stage 1](02-implementation.md)) and two unproven shape proofs
> ([Stage 2](02-implementation.md)). Opened 2026-09-20. **Owner:** Ajmal PS.
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
engine ([S7](00-structure.md)). Only route 3 shows a window.

| | Route | The user does | Files come from |
|---|---|---|---|
| **A** | **Cloud** | gives the repository; it installs itself | the cloud plan |
| **B** | **Repo download** | sets the project location, runs the setup file | **already in the repo** — no download |
| **C** | **Manual installer** | double-clicks it, ticks, presses Install | a signed GitHub release |

Everything in §3 and §4 below describes **route 3**, because it is the only one with a window. Routes 1
and 2 reach the same engine with their choices already decided, so every rule in §5 binds all three.

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
1.  WAIT until Revit is closed                 -> keep checking; name the version still open (R-38a)
2.  Fetch the product                          -> route 3 downloads and verifies; route 2 has it already
3.  Detect an existing install                 -> and say in the report that it is being replaced
4.  Back up whatever is already there          -> so Rollback has something to restore
5.  DELETE the old folder completely           -> nothing renamed, nothing left behind (R-38, R-38c)
5a. VERIFY it is gone before copying anything  -> if not, STOP and change nothing further (R-38b)
6.  Copy the product's WHOLE FOLDER of files   -> into ...\Addins\<version>\<Product>\  (R-40, R-41)
6a. Place <Product>.addin                      -> at ...\Addins\<version>\<Product>.addin
7.  Rewrite the <Assembly> line in the manifest -> to the real deployed path
8.  Verify the files landed and parse          -> the checks tools/check-package.py already makes
9.  Report, per product, per version           -> installed / replaced / skipped / failed and WHY
```

**Steps 1 and 5 together are the ones to get right, and the owner settled them on 2026-09-21.**

A delete **fails** while Revit holds the assembly open. AJ Tools works around that by renaming the old
folder aside and sweeping later. **Heron does not**: the owner ruled that a best-effort sweep leaves
`.old` folders piling up until nobody can tell which copy Revit is loading.

**So Heron waits instead of working around it.** Step 1 keeps checking until Revit is genuinely closed;
step 5 then deletes cleanly, because by then nothing is holding the files. **Nothing is ever renamed and
nothing is ever left behind** ([R-38](#the-lessons-from-aj-tools), [R-38c](#the-lessons-from-aj-tools)).

**Step 5a is what makes that safe.** Deleting has no fallback if it fails partway, so the folder is
**verified gone before a single new file is copied**. If it is not gone, the installer **stops and
changes nothing further** ([R-38b](#the-lessons-from-aj-tools)) — a clean refusal is recoverable, a
half-written install is not.

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
| R-15 | Offline install from bundled files | [Q-PE-5](03-open-questions.md) | **MUST** — Owner, 2026-09-22: *"even he dont have internet he can still use the tools"* |
| R-51 | **One download carries everything a user needs** — the built plugin for every supported Revit release **and** the brain, skills and MCP server beside it. Not the plugin alone | Owner, 2026-09-22; answers [Q-PE-12](03-open-questions.md) and [Q-PE-13](03-open-questions.md) | MUST |
| R-52 | The installer **asks where to keep** that folder and never chooses for the user. What they answer is the project folder the AI is later opened in | Owner, 2026-09-22: *"it will ask where you need to keep"*; answers [Q-PE-14](03-open-questions.md) | MUST |
| R-53 | **After that download, nothing needs the internet.** Installing, re-installing and every tool work offline | Owner, 2026-09-22; and it is what makes R-15 true rather than aspirational | MUST |
| R-54 | With internet, the installer **checks whether a newer release exists and says so**. It does **not** fetch the plugin again to find out | Owner, 2026-09-22: *"it will check only if there is update or not, no need to download again"* | MUST |
| R-55 | An update found is **offered, never applied**. One click to take it, and *"not now"* means not asked again | Owner, 2026-09-22: *"it will tell to update and one click need to update"*; [`brain/heron_update.py`](../../../../brain/heron_update.py) rules 1 and 7 | MUST |
| R-56 | The reason R-54 exists: somebody who downloaded **days ago** would otherwise never learn a newer release was published | Owner, 2026-09-22: *"if someone downloaded this repo some days before and we made update, that time he will not receive update"* | MUST |

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
| R-28 | **Route 1 — natural language.** The user says *"install this repo"* and **the AI** installs everything, **then asks which panels they want** | Owner, 2026-09-21 | MUST |
| R-28a | Route 1 **asks after installing, not before.** A conversation can install first and narrow later; that ordering is the design, not an accident | Owner, 2026-09-21: *"From there, it will ask the user which panels they need"* | MUST |
| R-29 | **Route 2 — repo handed over.** The user downloads the repo and gives it to **the AI** in-product, which installs from the files it was given | Owner, 2026-09-21 | MUST |
| R-30 | ~~**Route 2 downloads nothing.** The files are already in the repo, so it works with no internet~~ **CORRECTED 2026-09-22 — the second half was never true.** `.gitignore` excludes `bin/` and `obj/` and `git ls-files` finds **zero** tracked `.dll`, so a repository download carries source and **no built plugin at all**. Route 2 downloads nothing and works with no internet, and what it is handed is the **R-51 folder**, not a copy of the repository | Follows R-29; [Q-PE-12](03-open-questions.md) | MUST |
| R-48 | *"Install this"* means **Heron's own official installation link** — the signed release, the same link Heron publishes. **Never an arbitrary repository** | Owner, 2026-09-21; [Q-PE-10](03-open-questions.md) | MUST |
| R-49 | Route 1 **checks the source and refuses** anything that is not Heron's own release, in a sentence saying why. **Enforced, not expected** | [`docs/07 §1a`](../../../07-installation-and-update.md); [Golden Rule 19](../../../14-golden-rules.md) — a rule nothing enforces is one the first user breaks by accident | MUST |
| R-50 | The AI **never reads a repository's contents to decide what to install.** The manifest is read as data, from the release, after the source has been accepted | [Golden Rule 19](../../../14-golden-rules.md); and [R-13](#getting-the-files) | MUST |
| R-31 | All three routes drive **one engine**. No install rule exists in more than one place | [S7](00-structure.md); a rule in three installers is three rules and two go stale | MUST |
| R-32 | Routes A and B are **not silent**. Each reports what it installed, into which Revit versions, and what it skipped | [`docs/14`](../../../14-golden-rules.md) — an automatic install that says nothing cannot be checked | MUST |

### The Heron tab

| # | Requirement | Source | Status |
|---|---|---|---|
| R-33 | The `Heron` tab offers **two ticks** — AI Bridge connector, and all tools. Either, or both | Owner, 2026-09-21; [S3](00-structure.md) | MUST |
| R-34 | Ticking **tools without the connector** is a supported install. No AI is installed, and the tools work | Owner, 2026-09-21 | MUST |
| R-35 | Both pieces build into the **same `Heron` tab**, and neither may assume it is loaded first | Revit's `CreateRibbonTab` throws when the tab exists; `HeronApplication.BuildRibbon()` already catches it | MUST |
| R-36 | `Heron Doc`, `Heron MEP` and every future tab have **one tick each** — no sub-choice | [S3](00-structure.md) | MUST |
| R-36a | **Install All** is one control that ticks every product at once. It is **not a separate concept** — the same window, everything ticked | Owner, 2026-09-20 ("Install All / Custom Install") | MUST |
| R-36b | **Custom Install** is the same window with the user choosing. There is no second screen and no second code path | Owner, 2026-09-20; follows R-36a | MUST |

### The Settings panel

| # | Requirement | Source | Status |
|---|---|---|---|
| R-42 | The `Heron` tab carries a **Settings panel**, installed whenever any Heron product is | Owner, 2026-09-21; [S9](00-structure.md) | MUST |
| R-43 | From it the user turns **panels** on and off — hiding what is installed, not removing it | Owner, 2026-09-21 | MUST |
| R-43a | **Panels only. Tabs are never hidden.** `RibbonPanel.Visible` is official API; hiding a tab needs `AdWindows.dll`, which **Autodesk does not support** | [Q-PE-11](03-open-questions.md), verified 2026-09-21 | MUST |
| R-43b | Heron takes **no dependency on `Autodesk.Windows` / `AdWindows.dll`** anywhere. An unsupported internal API can change in any release, and Heron promises 2020 → 2027 and beyond | [16](../../../16-version-support-strategy.md) | MUST |
| R-43c | Hiding and un-hiding a panel takes effect **immediately, with no Revit restart** | [Q-PE-11](03-open-questions.md) — `RibbonPanel.Visible` is read-write and live | MUST |
| R-43d | A user who wants a **whole tab** gone **uninstalls that product**. That is the installer's job, not the Settings panel's | [S9](00-structure.md) — a tab is a product, a panel is a view choice | MUST |
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
| R-38 | Replacing **waits for Revit to close**, then deletes the old install completely and installs clean. **Nothing is ever renamed and nothing is ever left behind** | Owner, 2026-09-21 — **overrules the rename mechanism**; see [L2](04-lessons-from-aj-tools.md) | MUST |
| R-38a | While Revit is open the installer **keeps checking and waits**. It does not fail, and it does not work around it | Owner, 2026-09-21: *"until that close stage, it will always keep checking if Revit is closed"* | MUST |
| R-38b | After deleting, the installer **verifies the folder is actually gone before copying anything**. If the delete did not fully succeed it **stops and changes nothing further** | Follows R-38 — this is what makes deleting safe without a rename; a half-deleted install with no way back is the one outcome worse than waiting | MUST |
| R-38c | **No `.old` folders, no numbered copies, no leftovers of any kind.** One product, one folder, one version on disk | Owner, 2026-09-21: *"after a long time, you will end up with a lot of DLLs. That is a mistake"* | MUST |
| R-39 | Which Revit versions can be installed is read from **what the package contains**, never from a typed list | [L3](04-lessons-from-aj-tools.md) — **supersedes [R-9](#revit-versions)**; and [AGENTS.md](../../../../AGENTS.md) *never type a number a command can derive* | MUST |
| R-40 | A product ships **one payload folder per Revit release**, carrying that release's build **and everything the runtime needs beside it** | [L4](04-lessons-from-aj-tools.md) — .NET 8+ fails on a missing dependency manifest | MUST |
| R-41 | Each product installs into **its own folder**, never a shared one. Two products carrying different versions of the same helper assembly must not overwrite each other | [L5](04-lessons-from-aj-tools.md) — **corrects §4** | MUST |

### How the engine behaves when it finds something

| # | Requirement | Source | Status |
|---|---|---|---|
| R-51 | **Adding** something that did not exist — a fragment, a test, a case — needs **no permission**. It is done, and **reported in the same reply** | [B1](08-lessons-from-the-brain.md); the owner's own standing rule | MUST |
| R-52 | **Changing or deleting** something that already exists **asks first** | [B1](08-lessons-from-the-brain.md); [Golden Rule 13](../../../14-golden-rules.md) | MUST |
| R-53 | The report is **not optional and not deferred.** Same reply, saying what was done and why | [B1](08-lessons-from-the-brain.md) — an engine that improves things silently cannot be checked | MUST |
| R-54 | A `PROVEN` or `PRODUCTION` fragment, and any **agent**, is **proposed every time** whatever R-51 says | [Golden Rule 13](../../../14-golden-rules.md); [docs/09 §6](../../../09-skills-and-fragments.md) | MUST |

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
