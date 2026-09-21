# Heron Plugin Extension — the structure

> **Type:** Operational work note. **Not specification.** Where a sentence here disagrees with the
> [Constitution](../../../../HERON_CONSTITUTION.md), the [Golden Rules](../../../14-golden-rules.md) or
> [DECISIONS.md](../../../DECISIONS.md), **those win and this note is out of date.**
> **Status:** **Active.** Opened 2026-09-20. **Owner:** Ajmal PS.
> **Read this before [`01-requirements.md`](01-requirements.md)** — structure decides the requirements.

---

## 1. The shape, in one picture

```text
Revit ribbon
 |
 +-- [ Heron ]            <- the tab.  TWO pieces, ticked separately (S3)
 |     |
 |     +-- AI Bridge panel        <- EXISTS TODAY
 |     |     +-- Heron              (connect / disconnect toggle)
 |     |     +-- Bridge Status
 |     |     +-- Changes            (write on / off padlock)
 |     |
 |     +-- ( tools panels )        <- DO NOT EXIST.  Install without the AI if wanted
 |
 +-- [ Heron Doc ]        <- DOES NOT EXIST.  Annotation, dimensions, sheets, revisions.
 |
 +-- [ Heron MEP ]        <- DOES NOT EXIST.  Ducts, pipes, sizing, checks.
 |
 +-- ( more later )       <- the shape must allow this without changing the installer
```

**One product = one tab**, with the `Heron` tab the single exception: it carries the AI Bridge and the
Heron tools as two separately tickable pieces ([S3](#s3--custom-install-is-at-tab-level-and-the-heron-tab-is-the-one-exception)).
That sentence is the whole structure. Everything below follows from it.

---

## 2. What exists today — measured, not remembered

| Claim | Evidence |
|---|---|
| The tab is `Heron`, the panel is `AI Bridge` | [`HeronApplication.cs`](../../../../revit/Heron.Revit.Addin/HeronApplication.cs) lines 53–54, and [D-85](../../../DECISIONS.md) |
| The tab has exactly **3 buttons** | `BuildRibbon()` in the same file — one `SplitButton` (Heron + Bridge Status) and one `PushButton` (Changes) |
| The ribbon is **hardcoded in C#** | There is no ribbon manifest, no config file and no button list anywhere in `revit/` |
| There is **one** `.addin` manifest | [`Heron.addin`](../../../../revit/Heron.Revit.Addin/Heron.addin) — one `<AddIn>`, `AddInId` `7A1F4C62-…`, `VendorId` `AJPS` |
| There is **one** assembly | `Heron.Revit.Addin.dll`, from `Heron.Revit.Addin.csproj` |
| Deployment already works, per-user, no admin | [`tools/deploy-addin.ps1`](../../../../tools/deploy-addin.ps1) copies into `%APPDATA%\Autodesk\Revit\Addins\<version>\`. It has `-Remove` and `-Rollback`, and refuses while Revit is running |
| **No installer exists** | [`platform/README.md`](../../../../platform/README.md) lists *Installer · update system · package manager* under **"What will be here"** |

---

## 3. The nine structural decisions

These were taken by the owner in conversation on 2026-09-20. **They are not yet in
[DECISIONS.md](../../../DECISIONS.md)** — moving them there is Stage 1 of
[`02-implementation.md`](02-implementation.md).

### S1 — One product is one Revit tab

Not one panel. Not one button group. **A tab.**

`Heron`, `Heron Doc`, `Heron MEP` are three tabs sitting side by side in the Revit ribbon, the same
way Autodesk's own `Systems` and `Annotate` tabs do.

### S2 — One product is one `.addin` manifest plus one DLL

| Product | Manifest | Assembly | Tab |
|---|---|---|---|
| Heron AI Connector | `Heron.addin` | `Heron.Revit.Addin.dll` | `Heron` |
| Heron Doc | `Heron.Doc.addin` | `Heron.Doc.dll` | `Heron Doc` |
| Heron MEP | `Heron.MEP.addin` | `Heron.MEP.dll` | `Heron MEP` |

**Each needs its own `AddInId` GUID.** Revit keys add-ins by that GUID; two manifests sharing one GUID
is a load failure, not a warning.

**Why this and not one add-in that builds every tab:** installing a product then becomes *copy two
files*, and uninstalling it becomes *delete two files*. Nothing has to be rewritten, nothing has to be
merged, and **one product cannot break another product's tab** — a crash in `Heron.MEP.dll` leaves the
`Heron` tab standing.

### S3 — Custom Install is at tab level, and the Heron tab is the one exception

The user ticks **which tabs** they want. **The panels inside a tab are ours.**

> Owner, 2026-09-20: *"Not panels — tab level. What we have now, only Heron tab. Same like that we will
> add Heron MEP, Heron Annotation, like that."*

**This is the decision that makes the whole thing small.** Panel-level choice would have forced the
ribbon to be rebuilt from a manifest file at every Revit startup — a redesign of `BuildRibbon()` and a
new file format to keep in step with the code. Tab-level choice needs **none of it**: each product keeps
its ribbon hardcoded exactly the way `HeronApplication.cs` does today.

A user who could rearrange panels could also make the ribbon look wrong, and then report the result as
a defect. Fixing the layout is our job, not theirs.

**The `Heron` tab holds two things that tick separately**, and it is the only tab that does:

> Owner, 2026-09-21: *"For the Heron tab specifically, they can choose either or both options: all
> tools and the AI connector."*

```text
[ ] Heron                        <- the tab
      [ ] AI Bridge connector    <- the AI panel that exists today
      [ ] All tools              <- the Heron tools, which do not exist yet
```

Either, or both. Ticking the tools and **not** the connector installs the tools and no AI. Ticking the
connector alone gives what is in the repository today.

**`Heron Doc` and `Heron MEP` have no such split** — one tick each, whole tab.

**Why the exception is real and not a slip.** The `Heron` tab is the only one carrying two different
*kinds* of thing: a bridge to an AI, and ordinary Revit tools. A site modeller may want the tools and
refuse the AI; that is a legitimate install, not a broken one. Every other tab is tools only, so there
is nothing to separate.

**What this costs.** The `Heron` tab is built by two installed pieces rather than one, so both must be
able to build into the same tab without either one assuming it is first. Revit's `CreateRibbonTab`
throws if the tab exists — `HeronApplication.BuildRibbon()` already swallows that exception, and the
tools piece has to do the same.

### S4 — Install stays per-user, and never asks for admin rights

`%APPDATA%\Autodesk\Revit\Addins\<version>\`, which is what `deploy-addin.ps1` already does.

The target user is **a BIM modeller on a locked-down contractor laptop**. They cannot install anything
that needs an administrator without raising a ticket, and a ticket means the tool never gets used.
This is already settled in [`docs/07 §5`](../../../07-installation-and-update.md).

### S5 — Files come from a signed GitHub release

Chosen by the owner on 2026-09-20, and it agrees with
[`docs/07 §1a`](../../../07-installation-and-update.md), which already ruled that the installer fetches
**a versioned release artefact**, never whatever the default branch says today.

**The known cost, recorded here so nobody rediscovers it as a surprise:** a contractor firewall that
blocks GitHub will block the installer. An offline fallback is **[Q-PE-5](03-open-questions.md)** — not
refused, just not first.

### S6 — A ribbon button is a front door, not new logic

This is the one that decides how much work Heron Doc and Heron MEP actually are.

A **fragment body is already C# that compiles and runs inside Revit's own process**, and **327 of the
395 fragments are `PROVEN` against a real model** (measured 2026-09-20 — derive it again with the
command in §4 rather than trusting this line).

So a button in `Heron MEP` labelled **Auto Size** does not need new sizing logic written for it. It
calls the **same** `auto-size-mep` body the AI calls.

```text
                  +----------------------+
   AI chat  ----> |                      |
                  |   fragment body      | ----> Revit
   Ribbon   ----> |   (proven C#)        |
   button         +----------------------+
```

**One body, two front doors.** A fix to the body fixes both. A proof of the body proves both.

If a button ever needs logic that no fragment has, that is a signal to **write the fragment and prove
it**, not to write private code behind the button. Private code behind a button is code the AI cannot
reach, cannot explain and cannot audit.

### S7 — Three ways in, and two of them are the AI

**Restated 2026-09-21, and it changes what was written on 2026-09-20.** That version called route 1
"cloud" and route 2 "a setup file you run". The owner's fuller account makes both of them **the AI
doing the installing**, and only route 3 a window.

| | Route | What the user does | Who installs |
|---|---|---|---|
| **1** | **Natural language** | says *"install this repo"* or *"check this repo and set it up"* | **the AI** — fetches, installs everything, **then asks which panels they want** |
| **2** | **Repo upload** | downloads the repo and hands it to the AI in-product | **the AI** — installs from the files given to it |
| **3** | **Manual, one click** | double-clicks the installer | **the window** — tick, customise, Install |

> Owner, 2026-09-21: *"The user can tell the AI something like, 'Install this repo' or 'Check this repo
> and set it up.' The system will automatically install all the plugins and everything. From there, it
> will ask the user which panels they need."*

**Route 1 asks afterwards; route 3 asks first.** That is the real difference between them, and it is a
design choice rather than an accident: a conversation can install first and narrow later, a window
cannot. Route 2 sits between — the files are already decided, the panels still have to be.

**Still one engine.** Whoever is choosing, the rules are the same ones — Revit detection, the refusal
while Revit is open, the unblock, the rename-aside, the replace, the verify, the report. A rule that
lives in three installers is three rules and two of them go stale, so the engine
([Stage 3](02-implementation.md)) owns every one and the three routes only decide **what** to install.

```text
   1  natural language -> the AI asks, after installing   -\
   2  repo handed over -> the AI asks, about panels        --> ONE ENGINE -> Revit Addins folder
   3  installer window -> the user ticks, before           -/
```

**Route 2 downloads nothing** — the user already has the files — so it is the offline install, arriving
as a side effect rather than as a feature ([Q-PE-5](03-open-questions.md)).

**Routes 1 and 2 have a rule of their own that route 3 does not need.** They are an AI acting on a
repository, and [Golden Rule 19](../../../14-golden-rules.md) and
[`docs/07 §1a`](../../../07-installation-and-update.md) have something specific to say about that shape.
It is not settled here — see [Q-PE-10](03-open-questions.md).

### S8 — Install replaces; there is no separate upgrade path

> Owner, 2026-09-21: *"If an existing installation is detected, clicking 'Install' will remove or
> overwrite the previous version with the new one."*

**One button.** The installer does not ask the user whether this is a fresh install, an upgrade or a
repair. It finds what is there, and replaces it.

**Why this is the right call and not a shortcut.** An installer with Install / Update / Repair / Modify
makes the user diagnose their own machine before they are allowed to fix it, and they are the one person
who cannot. Replacing is also the only honest option for a Revit add-in: assemblies cannot be unloaded,
so an "upgrade in place" is a delete and a copy whatever the button says.

**What must not be replaced along with it.** `%APPDATA%\Heron` — the user's brain, skills, learned
patterns and audit log. The product is replaced; the data is not
([S4](#s4--install-stays-per-user-and-never-asks-for-admin-rights),
[`HeronPaths`](../../../../platform/Heron.Core/HeronPaths.cs)). A wipe that reached it would destroy a
year of work on a button press labelled *Install*.

### S9 — Installing and showing are two different things

**New 2026-09-21.** The `Heron` tab gets a **Settings panel**, and from it the user turns tabs and
panels on and off.

> Owner, 2026-09-21: *"after he install there is in the Heron tab there is one panel settings … if he
> install all the tools … from that tab he can turn off and on the tabs from their settings panels,
> like same like pyRevit … so they can hide the panels also."*

**The distinction this decision exists to hold:**

| | Decides | Lives in | Changing it means |
|---|---|---|---|
| **Install** | what is **on disk** | `%APPDATA%\Autodesk\Revit\Addins\…` | running the installer again |
| **Settings** | what is **on screen** | the user's own Heron data | ticking a box in Revit |

A modeller who installed everything and only wants two panels today should **not** have to run an
installer to say so, and should not lose the rest by saying it. Install is a decision about the machine;
settings is a decision about the afternoon.

**Where the setting lives, and why that matters.** Under `%APPDATA%\Heron` — the **data** class, which
[S8](#s8--install-replaces-there-is-no-separate-upgrade-path) and
[`HeronPaths`](../../../../platform/Heron.Core/HeronPaths.cs) say an install replaces **nothing** of. So
a user's hidden-panel choices survive every update. If they lived beside the product they would be wiped
by the next Install, and the user would blame the update for a tidy ribbon going untidy.

**Checked 2026-09-21, and the answer splits in a way that decides the design** ([Q-PE-11](03-open-questions.md)):

| | Live, no restart? | |
|---|---|---|
| **A panel** | **Yes** | `RibbonPanel.Visible` — read-write, **official Revit API** |
| **A whole tab** | — | needs `AdWindows.dll`, which **Autodesk does not support** |

**So the Settings panel hides PANELS and never tabs**, and Heron takes no dependency on the unsupported
internal API ([R-43a, R-43b](01-requirements.md)). A tidier ribbon is not worth a tool that breaks on an
Autodesk update nobody warned about.

**The API draws the line in the same place this decision already did.** A tab is a product — to remove
it, uninstall it. A panel is a view choice — tick it off and it goes, immediately.

**The three tabs this applies to, today:** `Heron`, `Heron Doc`, `Heron MEP`. The owner confirmed on
2026-09-21 that these are the three for now and more will be named later — so the Settings panel must
read its list from the same manifest the installer does ([Stage 1](02-implementation.md)), never from a
list written into the settings window.

---

## 4. What could fill Heron Doc and Heron MEP

**Measured 2026-09-20.** These numbers move. Derive them again before relying on them:

```bash
# status of every fragment
grep -h '^heron-status:' brain/fragments/*/fragment.yaml | sort | uniq -c

# how many fragments per domain
grep -h '^domain:' brain/fragments/*/fragment.yaml | sed 's/domain: //' | sort | uniq -c | sort -rn
```

| Tab | Domains | Total | `PROVEN` |
|---|---|---|---|
| Heron Doc | `revit.annotation` + `revit.sheets` | 35 | **28** |
| Heron MEP | `revit.mep` | 49 | **41** |

**Which panels those become is not decided** — see [Q-PE-3 and Q-PE-4](03-open-questions.md).

---

## 5. What this structure deliberately does NOT decide

| Not decided here | Where it lives |
|---|---|
| What **Heron Tools** is | [Q-PE-1](03-open-questions.md) — owner said he will explain |
| Whether Doc and MEP work **without** the AI Connector | [Q-PE-2](03-open-questions.md) — owner said this needs discussion |
| The panels inside Heron Doc and Heron MEP | [Q-PE-3, Q-PE-4](03-open-questions.md) |
| Whether an offline installer is ever built | [Q-PE-5](03-open-questions.md) |

**None of these block Stage 1 to Stage 4** of [`02-implementation.md`](02-implementation.md). The
installer can be built, and the shape proved, with only the `Heron` product in the list.
