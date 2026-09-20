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
|    [x] Heron              AI bridge and brain     Installed  |
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

---

## 4. What Install actually does

For each ticked product, for each ticked Revit version:

```text
1.  Check Revit is closed                      -> refuse, with the version named, if not
2.  Fetch the product from the GitHub release  -> verify the download before using it
3.  Back up whatever is already there          -> so Rollback has something to restore
4.  Copy  <Product>.addin  +  <Product>.dll    -> into %APPDATA%\...\Addins\<version>\
5.  Rewrite the <Assembly> line in the manifest -> to the real deployed path
6.  Verify the files landed and parse          -> the checks tools/check-package.py already makes
7.  Report, per product, per version           -> installed / skipped / failed and WHY
```

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
| R-9 | Supported set is **2020 → 2027**, eight releases | [`docs/16`](../../../16-version-support-strategy.md) | MUST |
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
| R-24 | Rollback is **tested**, not merely implemented | [`docs/07 §7`](../../../07-installation-and-update.md) rule 5; `brain/heron_update.py` refuses a release without `rollback_tested` | MUST |

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
