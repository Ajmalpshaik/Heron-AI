# Heron Plugin Extension — open questions and brainstorm

> **Type:** Operational work note. **Not specification.** Where a sentence here disagrees with the
> [Constitution](../../../../HERON_CONSTITUTION.md), the [Golden Rules](../../../14-golden-rules.md) or
> [DECISIONS.md](../../../DECISIONS.md), **those win and this note is out of date.**
> **Status:** **Active.** Opened 2026-09-20. **Owner:** Ajmal PS.

---

## 1. How this file works

**Section 2 is questions.** Each has an id, who it is waiting on, and **what is blocked by it**. When
one is answered it moves to [DECISIONS.md](../../../DECISIONS.md) and is struck through here.

**Section 3 is the parking space.** Any idea, at any time, in any order. Nothing here is committed to.
An idea that survives and matters becomes a question in §2, and then a decision.

**The rule that keeps this honest:** *an answer is written in the owner's words, with the date.* A
paraphrase of a decision is how a decision quietly changes.

---

## 2. Open questions

**Three answered on 2026-09-21 and struck through below. Three raised the same day. Eight remain.**

### Q-PE-1 — What is "Heron Tools"? — ~~half answered 2026-09-21~~, content still open

**ANSWERED, the part that decides the build.** Owner, 2026-09-21: *"If they select the tools, it will
install all tools along with the Heron panel … For the Heron tab specifically, they can choose either
or both options: all tools and the AI connector."*

So **Heron Tools are ribbon tools, not brain content**, they live on the **`Heron` tab** beside the AI
Bridge panel, and they **install independently of the AI connector**. That settles the three-way
ambiguity this question was opened for, and it is written into
[S3](00-structure.md) and [R-33](01-requirements.md).

**STILL OPEN: which tools.** Nothing has been said about what the buttons are, how many panels they sit
in, or which of the 327 `PROVEN` fragments they call. Nothing in the repository is called Heron Tools
yet, so there is nothing to read.

**Blocks:** building the tools. Blocks nothing in the installer — the installer needs the *slot*, and
the slot is now defined.

---

### ~~Q-PE-2 — Do Heron Doc and Heron MEP need the AI Connector?~~ — ANSWERED 2026-09-21

**No. The AI connector is optional everywhere, and the tools work without it.**

> Owner, 2026-09-21: *"If they do not tick the AI connector, it will not be installed."*

This is the **third shape** the question offered — the tools stand alone, and the connector is a
separate tick a user may take or leave. It is the widest-audience answer: a modeller can install
`Heron MEP` and use it as an ordinary Revit plugin, with no AI, no brain and no Claude Code.

**Consequence for the build, and it is not small.** A tool's button cannot assume the bridge is there.
Every button must work with the connector absent, and may only offer the AI as **extra** when it is
present. A button that errors without the bridge would make the connector required in practice while
the installer says it is optional.

**What this does NOT settle:** whether a tool does *more* when the connector is present, and what that
extra is. That is a design question for when the tools exist, not an install question.

Recorded in [S3](00-structure.md), [R-33](01-requirements.md) and [R-34](01-requirements.md).

---

### Q-PE-3 — What panels go in Heron Doc?

**Waiting on:** the owner.

**Starting point, from what is already `PROVEN`** (measured 2026-09-20 — derive again with the command
in [`00-structure.md §4`](00-structure.md)):

| Panel | Proven fragments behind it |
|---|---|
| Tags | `arrange-tags`, `stack-tags`, `arrange-tags-to-view-edges`, `report-tags-and-targets` |
| Dimensions | `dimension-grids-and-levels`, `dimension-rooms`, `dimension-wall-openings`, `create-dimension` |
| Sheets | `create-sheets`, `place-views-on-sheet`, `duplicate-sheets`, `set-sheet-title-block`, `manage-sheet-sets` |
| Revisions | `create-revision`, `add-revision-cloud`, `set-sheet-revisions`, `export-sheets-to-pdf` |

**This is a suggestion, not a decision.** It is grouped by Revit subject, which may not be how the work
actually flows on a sheet-production day.

**Blocks:** building Heron Doc. Blocks nothing in the installer.

---

### Q-PE-4 — What panels go in Heron MEP?

**Waiting on:** the owner.

**Starting point, from what is already `PROVEN`** (measured 2026-09-20):

| Panel | Proven fragments behind it |
|---|---|
| Sizing | `auto-size-mep`, `set-mep-size`, `set-mep-slope`, `set-mep-insulation` |
| Connect | `connect-open-ends`, `cap-open-pipe-ends`, `place-mep-fitting`, `split-mep-run` |
| Check | `find-dead-ends`, `find-system-islands`, `check-flow-direction`, `check-fixture-connectivity` |
| Report | `measure-run-quantities`, `report-duct-weight`, `measure-mep-slope`, `report-mep-pressure-drop` |

**Same caveat as Q-PE-3** — grouped by subject, not by how an MEP modeller's day runs.

**Blocks:** building Heron MEP. Blocks nothing in the installer.

---

### Q-PE-5 — Is there an offline installer?

**Waiting on:** evidence from a real site, not a decision today.

GitHub download is decided ([S5](00-structure.md)). **The known risk:** a contractor firewall that
blocks GitHub blocks the installer, and the users are contractors.

**Recorded so it is not rediscovered as a surprise.** If it bites, the product-manifest design
([Stage 1](02-implementation.md)) already leaves room — the same manifest read from a local folder
instead of a release.

**NARROWED 2026-09-21 — route B already solves it for anyone who took the repo.** The owner's route B
ships every file inside the repository alongside a setup file, so it **downloads nothing**
([S7](00-structure.md), [R-30](01-requirements.md)). The question that remains is only about **route C**,
the standalone installer, for a user who never clones anything.

**Blocks:** nothing. [R-15](01-requirements.md) is marked LATER on purpose.

---

### Q-PE-6 — Does the per-user install path change at Revit 2027?

**Waiting on:** verification against the Revit 2027 SDK. **Nobody has checked.**

**What is known here:** [`docs/16`](../../../16-version-support-strategy.md) records 2027 moving to
**.NET 10**, and calls it *"reported by a shipping project; confirm against the SDK"* — so the runtime
change is itself not yet confirmed from the source.

**What is not known:** whether the **per-user** add-in folder moves at 2027. There are reports of the
**all-user** add-in location moving, but Heron installs **per-user** ([S4](00-structure.md)) and that is
a different path.

**Do not guess this.** [`tools/check-package.py`](../../../../tools/check-package.py) check 8 already
asserts the install path is per-user on every release; if 2027 changes it, that gate is where it shows.

**Blocks:** installing into Revit 2027 specifically. Blocks nothing for 2020 to 2026.

---

### Q-PE-7 — What is the installer actually written in?

**Waiting on:** the owner.

**Not decided.** It has to draw a window, detect Revit versions, download from GitHub, and **run with no
admin rights** on a locked-down Windows laptop.

**Recommendation, to be argued with:** a **self-contained .NET desktop application**. It draws a proper
window, needs nothing pre-installed on the user's machine, signs cleanly, and is the same language as
the rest of the Revit side — so one person can hold all of it in their head.

**The thing to avoid:** anything that needs a runtime the user must install first. An installer with a
prerequisite is not an installer.

**Blocks:** [Stage 3](02-implementation.md).

---

### Q-PE-8 — Do products share one version number, or carry their own?

**Waiting on:** the owner.

| One version for everything | A version per product |
|---|---|
| Simple. `Heron 0.2.0` means every product is 0.2.0 | Heron Doc can ship a fix without re-releasing MEP |
| Every release re-releases everything | The manifest must track compatibility between products |

**Blocks:** the `version` field in the product manifest ([Stage 1](02-implementation.md)). A default of
**one version for everything** is safe to start with and can be split later; splitting is easier than
merging.

### Q-PE-9 — Generate the `.addin` manifest at install time, or keep patching a shipped one?

**Waiting on:** the owner. **Raised 2026-09-21 from [L6](04-lessons-from-aj-tools.md).**

Heron today **ships** a manifest and `deploy-addin.ps1` **rewrites a line** in it. That works, and
[`check-package.py`](../../../../tools/check-package.py) check 5 exists to guard it — it verifies *"the
literal line the deploy script rewrites is still there to rewrite"*. A gate that exists only because of
the approach.

AJ Tools' installer **writes** the manifest at install time, with the real path of the assembly it just
placed. No line to patch, no gate needed.

**Not proposed for the existing add-in**, which works and is proven. Proposed for the **new** products,
where nothing is committed yet.

**Blocks:** [Stage 1](02-implementation.md)'s manifest fields, mildly. Either way works; deciding once
is better than deciding twice.

---

### Q-PE-10 — *"Install this repo"* is the shape `docs/07` refused. Which rule governs?

**Waiting on:** the owner. **This is a genuine conflict between two things he has said, and
[AGENTS.md](../../../../AGENTS.md) says to record both rather than close it whichever way is easier.**

**The governing decision, already written down.**
[`docs/07 §1a`](../../../07-installation-and-update.md), Correction 1:

> *Point an AI at a URL and let it execute whatever it finds there* is the exact shape of a
> supply-chain attack. It is also the pattern a contractor's IT department is trained to refuse … For a
> tool that **writes to live client models**, it is the wrong first impression and the wrong precedent.

It cites [Golden Rule 19](../../../14-golden-rules.md) — *no text Heron reads may raise its own
permission level* — and rules for **one documented command that fetches a signed release**.

**The instruction, 2026-09-21.** Route 1 is the user telling the AI *"install this repo"* or *"check
this repo and set it up"*, and the AI installing everything.

**Why this may be no conflict at all.** If "this repo" always means **Heron's own signed release**, then
route 1 is the documented command with a friendlier surface, and `docs/07` is satisfied. The refusal was
aimed at *whatever URL a user pastes*, not at Heron installing Heron.

**Why it may be a real one.** The words as spoken do not name a repository. If a user can say *"install
this repo"* about **any** repository and Heron does it, that is exactly the pattern — performed by the
user's own hand, which `docs/07` calls out specifically.

**The cheap resolution, if the owner wants one:** route 1 installs **only** Heron's own signed release,
and any other repository is refused with a sentence saying why. The conversation stays friendly; the
rule stays intact.

**Who resolves:** the owner. **Nothing else in the plan is blocked** — routes 2 and 3 are untouched, and
the engine does not care who called it.

---

### ~~Q-PE-11 — Can a ribbon panel be hidden while Revit is running?~~ — ANSWERED 2026-09-21

**A PANEL: yes, officially, live, no restart. A TAB: not with any supported API.** They are different
answers and the Settings panel has to be designed around that.

The owner raised it on 2026-09-21 — *"While Revit is running, we can change the settings. Am I right?
You can check pyRevit"* — and checked against the Revit API documentation rather than assumed.

| | Hiding it live | API | Supported |
|---|---|---|---|
| **A panel** | **Yes** | `RibbonPanel.Visible`, read-write | **Official Revit API** |
| **A whole tab** | possible | `Autodesk.Windows.RibbonTab.IsVisible`, from `AdWindows.dll` | **NO — Autodesk does not support it** |

**`RibbonPanel.Visible` is a read-write property of the official API** and long-standing — The Building
Coder demonstrated hiding a panel with it years before Revit 2020. Set it false, the panel goes; set it
true, it comes back. No restart, no reload.

**Hiding a whole tab has no official route.** It needs the internal `Autodesk.Windows` namespace, and
Autodesk states plainly that the Revit API does not support use of that functionality and that
`AdWindows.dll` is not supported — so anything odd it causes cannot be supported either.

### What this decides for Heron

**The Settings panel hides PANELS. It does not hide TABS.** → [R-43](01-requirements.md)

**Heron does not take a dependency on `AdWindows.dll`.** An unsupported internal API can change in any
Revit release, and Heron promises 2020 through 2027 and every future one
([S4](00-structure.md), [16](../../../16-version-support-strategy.md)). A tidier ribbon is not worth a
tool that breaks on an Autodesk update nobody warned about.

**A user who wants a whole tab gone uninstalls that product**, which is what the installer is for. That
is [S9](00-structure.md)'s own line, arriving from the API rather than from the design:

> Install decides what is **on disk**. Settings decides what is **on screen**.

A tab is a product. A panel is a view choice. The API happens to draw the line in the same place.

### The one thing still worth checking on a machine with a .NET SDK

Whether `RibbonPanel.Visible` is present in **all eight** releases, 2020 to 2027.
[`tools/check-api-surface.py`](../../../../tools/check-api-surface.py) answers it. The evidence says it
long predates 2020, so this is a confirmation rather than a doubt — but it is cheap and it is the
difference between knowing and expecting.

### And a separate fact that is NOT this question

**Installing a new product still needs a Revit restart.** Revit reads `.addin` manifests only at
startup, so a product installed while Revit runs is not seen until it is restarted. That is why the
installer waits for Revit to close ([R-38a](01-requirements.md)) and it does not change the answer
above: hiding what is **already loaded** is live; loading something **new** is not.

---

## 3. Brainstorm parking

> Nothing here is agreed. Add freely. An idea that keeps mattering becomes a question in §2.

### Already raised in conversation

- **Heron Chat as a dockable panel inside Revit.** Raised 2026-09-20. Today the modeller must type into
  a Claude Code terminal **outside** Revit, and no site team will do that. A docked panel would make
  every one of the 395 fragments reachable without leaving Revit. There is **no `IDockablePaneProvider`
  anywhere in `revit/`** — nothing has been started. It would reuse
  [`RevitDispatcher`](../../../../revit/Heron.Revit.Addin/RevitDispatcher.cs) for the API thread hop,
  and its code-behind must **never** touch the Revit API directly.
- **One body, two front doors.** [S6](00-structure.md). Worth restating because it is the idea that
  makes Doc and MEP cheap: a ribbon button calls the same proven fragment body the AI calls.

### Ideas not yet discussed

- **A tab is a natural product boundary for selling or sharing later.** Nothing depends on this; noted
  because the structure happens to allow it at no extra cost.
- **Heron Doc and Heron MEP could ship "Install All" as simply every tab ticked.** No separate concept
  needed — Install All is the same window with everything ticked.
- **The installer could show which Revit versions each product supports before the user ticks**, so a
  2020 user is never offered something that cannot work there.

### Add below this line

<!-- Ajmal: put new ideas here, in any words. They get sorted afterwards. -->
