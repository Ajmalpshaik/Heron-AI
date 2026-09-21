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

## 3. The eight structural decisions

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

### S7 — Three ways in, one engine underneath

Stated by the owner on 2026-09-21. **There are three ways Heron gets onto a machine, and they are not
three installers.** They are three front doors onto the same install engine.

| | Route | Who it is for | What the user does |
|---|---|---|---|
| **A** | **Cloud** | a new person joining, per the cloud planning | gives the repository; everything installs by itself |
| **B** | **Repo download** | anyone who takes the whole repo | sets the project location, runs the setup file; everything configures by itself |
| **C** | **Manual installer** | a modeller who just wants the tools | double-clicks the installer, ticks what they want, presses Install |

> Owner, 2026-09-21: *"if they provide the repository, it will automatically install everything …
> Alternatively, if someone downloads the repo, all the files will be included, along with an
> installation file. Once they set the project location and run the setup, everything can be
> configured automatically."*

**Why one engine and not three.** A rule that lives in three installers is three rules, and two of them
go stale. The engine ([Stage 3](02-implementation.md)) owns every rule — Revit detection, the refusal
while Revit is open, the copy, the overwrite, the verify. A, B and C differ only in **who chooses** and
**where the files come from**:

```text
   A  cloud        -> choices come from the cloud plan    -\
   B  repo + setup -> choices come from the setup answers  --> ONE ENGINE -> Revit Addins folder
   C  installer    -> choices come from the tick boxes    -/
```

**Route B ships the files with the repo.** Unlike route C it does not have to download anything — the
repo already carries them. That is worth noting against [S5](#s5--files-come-from-a-signed-github-release):
**route C downloads, route B does not**, so the offline case that [Q-PE-5](03-open-questions.md) worries
about is already solved for anyone who took the repo.

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
