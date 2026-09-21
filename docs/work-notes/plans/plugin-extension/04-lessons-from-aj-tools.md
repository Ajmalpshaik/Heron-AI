# Heron Plugin Extension — what AJ Tools' installer already knows

> **Type:** Operational work note. **Not specification.** Where a sentence here disagrees with the
> [Constitution](../../../../HERON_CONSTITUTION.md), the [Golden Rules](../../../14-golden-rules.md) or
> [DECISIONS.md](../../../DECISIONS.md), **those win and this note is out of date.**
> **Status:** **Active.** Opened 2026-09-21. **Owner:** Ajmal PS.

---

## 1. What this is, and the rule it obeys

The owner asked on 2026-09-21: *"you can refer to our AJ tools. We built that installation there … maybe
you will find some good things there as well."*

`Ajmalpshaik/AJ-Tools-Installer` was read at commit `292c411`, release `v1.56.2`. It installs a Revit
add-in for **2020 to 2027**, per-user with no admin, with an all-users option — the same job Heron's
installer has.

**The rule this note obeys.** [AGENTS.md](../../../../AGENTS.md) and
[31](../../../31-studying-the-existing-libraries.md): *mechanisms and lessons are re-authored, never
imported*. **Nothing is copied here** — not a line of script, not a file layout, not a phrase. What
follows is what its installer has already been taught by failing, written in Heron's own words, so
Heron does not have to be taught the same things the same way.

**Several of these were learned the hard way, and its own comments say so.** Those are the valuable
ones: a defect already paid for once.

---

## 2. The eight lessons

### L1 — A downloaded file carries a mark, and Revit refuses it

Windows stamps anything from a browser with a zone marker. A DLL that still carries it makes Revit fail
to load the add-in, with an error that names nothing useful.

AJ Tools' installer clears that mark on every file — **before** it copies, and **again after**, on the
copies. Its comment ties the marker directly to the Revit error it produces.

**Heron's plan had nothing about this at all.** It would have hit every user who downloaded a release —
which, under [S5](00-structure.md), is every route-C user there is. → **[R-37](01-requirements.md)**

### L2 — Never delete a working install before its replacement is in place

**This corrects [R-23b](01-requirements.md), written yesterday.** That row says *remove the old files
first, then copy*. AJ Tools' installer learned that **remove can fail**: Revit holds its loaded
assemblies open, so a delete throws while Revit is running.

What it does instead is the part worth taking: a locked file cannot be deleted but **can be renamed**.
So it renames the whole folder aside, installs clean, and sweeps the set-aside folders on a later run —
best-effort, so a sweep that fails can never fail an install.

Its comment names the rule as a dated defect: *never destroy a working install before the replacement
is in place.*

**Why this matters more for Heron than for AJ Tools.** Heron's installer is supposed to refuse while
Revit is running ([R-17](01-requirements.md)) — but a refusal is a check, and a check can be wrong: a
second Revit on another desktop, a background process, a file a virus scanner is holding. Rename-aside
is what makes the failure survivable rather than destructive. → **[R-38](01-requirements.md)**

### L3 — Never hardcode which Revit versions can be installed

Its script carries a comment about exactly this going wrong. A hardcoded list of "modern .NET versions"
was left behind after the per-version builds landed, and the result was silent: **it installed nothing
at all on 2025, 2026 and 2027** while the install document advertised support for them. Nobody saw it,
because nothing failed — the install simply skipped.

The fix was to delete the list. What can be installed is now read from **what the package actually
contains**.

**[R-9](01-requirements.md) is currently written as a hardcoded list** — *"Supported set is 2020 → 2027,
eight releases"*. That is the same shape of claim, in a plan that has not been built yet. → **[R-39](01-requirements.md)**

This is the same rule [AGENTS.md](../../../../AGENTS.md) already states for this repository — *never type
a number a command can derive* — arriving from a completely different direction.

### L4 — One payload folder per Revit release, and .NET 8+ needs more than the DLL

The package carries a separate folder per Revit year, each holding that year's own build. For the .NET
8 and .NET 10 releases it also carries the dependency-manifest file the .NET host reads at startup —
and its comment notes that copying only the DLL leaves that file behind, which the host then fails on.

Heron compiles on all eight releases already, so it has the builds. What its plan did not say is that
**the unit of installation is a folder of files, not two files**. → **[R-40](01-requirements.md)**

### L5 — Each product gets its own folder, not a shared one

The manifest sits beside the Addins folder; the assemblies go in a **subfolder named for the product**.

**This one is a defect in Heron's plan, not just a gap.** [§4 of the requirements](01-requirements.md)
says the installer copies `<Product>.addin` **and** `<Product>.dll` into
`%APPDATA%\Autodesk\Revit\Addins\<version>\` — flat, all products together. Heron plans **three or more
products in one folder**, each carrying its own dependencies. Two products shipping different versions
of the same helper assembly would overwrite each other, and the loser would fail at runtime in a way
that looks like a bug in whichever loaded second.

A folder per product also makes uninstall exact: one folder and one manifest, nothing shared to reason
about. → **[R-41](01-requirements.md)**

### L6 — Generate the manifest at install time

Rather than shipping a manifest and patching a line in it, its installer **writes the manifest** at
install time with the real path of the assembly it just placed.

Heron's `deploy-addin.ps1` rewrites a line in a shipped manifest, and
[`check-package.py`](../../../../tools/check-package.py) check 5 exists to make sure *"the literal line
the deploy script rewrites is still there to rewrite"* — a gate that only needs to exist because of the
patching approach. Generating removes both the patch and the gate.

**Not proposed as a change to the existing add-in**, which works. Proposed for the **new** products, and
recorded so the choice is deliberate. → **[Q-PE-9](03-open-questions.md)**

### L7 — Check the file landed, then say what was installed and what was skipped

After copying, it checks the main assembly is actually there and stops if it is not. At the end it
prints the versions installed and the versions skipped, and why.

Heron's [R-19](01-requirements.md) and [R-32](01-requirements.md) already say this. **Recorded as
agreement rather than as a new lesson** — two installers arriving at the same rule independently is
worth more than one asserting it.

### L8 — Publish a checksum file next to the release

The release carries a checksums file, and the install document tells the user to compare before
installing.

Heron's [R-12](01-requirements.md) says a download must be verified before use. This is what that looks
like in practice, and it confirms the requirement is implementable rather than aspirational.

---

## 3. What Heron should NOT take

**The all-users install.** AJ Tools offers one, and it needs Administrator. Heron's
[S4](00-structure.md) says per-user only, never admin, because the target user cannot get admin without
a ticket. That is a deliberate difference, not an oversight — and it is why Heron's installer will be
simpler here rather than richer.

**The flat `install.cmd` / `uninstall.cmd` pair.** It suits one product. Heron has several and needs a
window to choose between them ([R-1](01-requirements.md)), so the shape does not carry over even though
every rule inside it does.

**Any wording, any file name, any script.** [31](../../../31-studying-the-existing-libraries.md).

---

## 4. Where each lesson went

| | Lesson | Became |
|---|---|---|
| L1 | Clear the download mark | [R-37](01-requirements.md) |
| L2 | Rename aside, never delete first | [R-38](01-requirements.md) — **corrects R-23b** |
| L3 | Derive the version list from the package | [R-39](01-requirements.md) — **corrects R-9** |
| L4 | A payload folder per release | [R-40](01-requirements.md) |
| L5 | A folder per product | [R-41](01-requirements.md) — **corrects §4** |
| L6 | Generate the manifest | [Q-PE-9](03-open-questions.md) — open, not decided |
| L7 | Verify, then report | already [R-19](01-requirements.md), [R-32](01-requirements.md) |
| L8 | Ship a checksum file | already [R-12](01-requirements.md) |

**Three of the eight corrected something already written.** That is the return on reading it, and it is
the argument for reading the rest of AJ Tools before the tools themselves are designed.
