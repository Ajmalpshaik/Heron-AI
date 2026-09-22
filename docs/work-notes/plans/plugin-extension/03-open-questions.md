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

**Three answered outright on 2026-09-21 and struck through below — Q-PE-2, Q-PE-10, Q-PE-11. Q-PE-1 is
half answered and Q-PE-5 narrowed, the same day. Three were raised.**

**Do not read a total here — derive it.** This line has been wrong twice already:

```bash
grep -c '^### Q-PE'   docs/work-notes/plans/plugin-extension/03-open-questions.md   # open here
grep -c '^### ~~Q-PE' docs/work-notes/plans/plugin-extension/03-open-questions.md   # closed here
grep -c '^### Q-DE'   docs/work-notes/plans/plugin-extension/07-debugging-engine.md # the engine's own
```

Derive both rather than trusting this line:

```bash
grep -c '^### Q-PE' docs/work-notes/plans/plugin-extension/03-open-questions.md
grep -c '^### Q-DE' docs/work-notes/plans/plugin-extension/07-debugging-engine.md
```

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

### ~~Q-PE-5 — Is there an offline installer?~~ — ANSWERED 2026-09-22

**Yes, and it is the only installer.** Answered by the same decision as
[Q-PE-12](#q-pe-12--route-2-says-the-files-are-already-in-the-repo-they-are-not--answered-2026-09-22):
one download carries the built plugin **and** the brain, the user says where to keep it, and after
that **nothing needs the internet** - [R-51](01-requirements.md), [R-52](01-requirements.md),
[R-53](01-requirements.md).

> Owner, 2026-09-22: *"even he dont have internet he can still use the tools that we will develop...
> there is no need internet, only need for if there is update or not"*

**THE CONTRACTOR FIREWALL BELOW IS NO LONGER A RISK TO THE INSTALL, only to the update check** -
which is [R-54](01-requirements.md), and a check that cannot reach GitHub simply says so and
installs anyway. That is the whole point of splitting the two.

**Waiting on:** evidence from a real site, not a decision today.

GitHub download is decided ([S5](00-structure.md)). **The known risk:** a contractor firewall that
blocks GitHub blocks the installer, and the users are contractors.

**Recorded so it is not rediscovered as a surprise.** If it bites, the product-manifest design
([Stage 1](02-implementation.md)) already leaves room — the same manifest read from a local folder
instead of a release.

**NARROWED 2026-09-21 — route 2 already solves it for anyone who took the repo.** The owner's route 2
ships every file inside the repository alongside a setup file, so it **downloads nothing**
([S7](00-structure.md), [R-30](01-requirements.md)). The question that remains is only about **route 3**,
the standalone installer, for a user who never clones anything.

**AND THAT NARROWING IS WITHDRAWN, THE SAME DAY.** See
[Q-PE-12](#q-pe-12--route-2-says-the-files-are-already-in-the-repo-they-are-not--answered-2026-09-22): `bin/` is gitignored
and **zero** `.dll` files are tracked, so *"every file inside the repository"* does not include the
built plugin. Route 2 downloads nothing because there is nothing there to install. **The paragraph
above is kept as written** — it records the reasoning at the time, and what was wrong with it was the
file set, not the logic.

**Blocked:** nothing. [R-15](01-requirements.md) was marked LATER on purpose. **It is a MUST as of
2026-09-22**, which is what this question was waiting for.

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

### ~~Q-PE-10 — "Install this repo" is the shape `docs/07` refused. Which rule governs?~~ — ANSWERED 2026-09-21

**There is no conflict. It is Heron's own installation link, and nothing else.**

> Owner, 2026-09-21: *"'install this repo' means there is an installation link or something like that.
> Someone will not just say 'install this hero' … It should be a link or a dedicated installation, the
> same kind of link we are providing. So if someone provides that, they will say 'install this', not
> 'another repo'."*

So route 1 is **the documented command with a friendlier surface** — which is exactly what
[`docs/07 §1a`](../../../07-installation-and-update.md) already ruled for:

> *one documented command that fetches a signed release … the command is the same for everyone, and what
> it downloads is a **versioned release artefact**.*

**What `docs/07` refused was *"paste any URL and let the AI run what it finds"*.** That is not what this
is. Question closed.

### The part that is not free, and it is a requirement rather than an assumption

**A rule nothing enforces is a rule the first user breaks by accident.** If route 1 merely *expects*
Heron's link, then a user who pastes a different repository gets it installed — and the refused pattern
arrives by the back door, performed by the user's own hand, which is the case
[`docs/07 §1a`](../../../07-installation-and-update.md) calls out by name.

So: **route 1 checks the source and refuses anything that is not Heron's own release**, in a sentence
that says why rather than an error ([R-48, R-49, R-50](01-requirements.md)). The refusal is what lets
the friendly sentence stay friendly.

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

### ~~Q-PE-12 — Route 2 says "the files are already in the repo". They are not.~~ — ANSWERED 2026-09-22
**ANSWERED TOGETHER WITH [Q-PE-13](#q-pe-13--how-does-the-brain-reach-a-users-pc-nothing-installs-it--answered-2026-09-22)
AND [Q-PE-14](#q-pe-14--the-brain-and-the-project-folder-want-claude-code-opened-in-two-different-places--answered-2026-09-22),
because one shape answers all three.**

**ONE DOWNLOAD CARRIES EVERYTHING, AND IT ASKS WHERE TO PUT IT.** The owner walked through what each
kind of user does, 2026-09-22:

> *"If someone downloads it, they need to have everything. If he needs to install that Revit plugin,
> just click the installer and it installs. If he has internet, this will check only if there is an
> update or not - no need to download again. Even if he doesn't have internet he can still use the
> tools... it will ask where you need to keep, and it will keep the downloaded folder and plugin
> there, so he can use the AI functions also."*

| was asked | answer |
|---|---|
| **Q-PE-12** what route 2's handover contains | The **[R-51](01-requirements.md) folder** - the built plugin for every supported release, **and** the brain, skills and MCP server. It is the first of the three options below, and it is now what route 1 fetches too |
| **Q-PE-13** how the brain reaches a PC | **In the same download.** The Connect button stops opening a pipe nobody answers, because the thing that answers it arrives beside the plugin |
| **Q-PE-14** where the project folder lives | **The user says.** The installer asks, and what they answer is the folder Claude Code is opened in afterwards - [R-52](01-requirements.md) |

**ROUTE 3 IS THE SAME SHAPE.** Double-click, it installs, no internet. Internet only ever answers one
question: *is there a newer release?* - [R-54](01-requirements.md), offered and never applied
([R-55](01-requirements.md)).

**AND THE LINK IN THE OWNER'S OWN EXAMPLE IS REFUSED, CORRECTLY.** He wrote
`heron-install --source https://github.com/<owner>/<repo>`, which is the **repository**, and
[R-48](01-requirements.md) - his own decision on 2026-09-21 - says *"the signed release... never an
arbitrary repository"*. The gate refuses it and names the releases page instead. **The rule is not
bent. The release is made to carry what he wanted the repository to carry**, which is what makes both
true at once rather than trading one for the other.

**WHAT THIS DOES NOT DECIDE, and it is small:** whether the folder carries the *whole* repository or
only what a user needs to run it - brain, skills, MCP and docs, but not `tests/`, `tools/` or the C#
source. Raised with the owner on 2026-09-22 and left for later on purpose: shipping more than needed
is recoverable, shipping less is not, so the first cut errs wide.

---

**The reasoning as it stood, kept because it is the record:**



**Waiting on:** the owner. **Raised 2026-09-21**, reading the plan against the repository.

[R-30](01-requirements.md) says *"Route 2 downloads nothing. The files are already in the repo, so it
works with no internet"*, and [Q-PE-5](#q-pe-5--is-there-an-offline-installer--answered-2026-09-22) was **narrowed** on that
sentence — route 2 was recorded as already solving the offline case for anyone who took the repo.

**Measured:** `.gitignore` excludes `bin/` and `obj/`, and `git ls-files` finds **zero** tracked `.dll`.
A repository download carries **source code and no built plugin at all**. Route 2 as written installs
nothing, and the narrowing of Q-PE-5 rests on a file set that does not exist.

**So something has to decide what route 2's handover actually contains**, and the options differ in
who pays for them:

| | What it costs |
|---|---|
| A release **zip** carrying the built products beside a setup file | eight releases of assemblies in one download; needs a build step in the release pipeline |
| Committing built DLLs to the repository | refused everywhere else in this repository, and it would make every build a diff |
| Route 2 simply **needs a build**, and offline means "offline once built" | honest, but then it is not an answer to Q-PE-5 at all |

**Blocked:** [Stage 6](02-implementation.md), and it un-narrowed [Q-PE-5](#q-pe-5--is-there-an-offline-installer--answered-2026-09-22). **Unblocked 2026-09-22** - and [R-15](01-requirements.md) is a MUST now rather than a LATER, which is what Q-PE-5 was waiting on.

---

### ~~Q-PE-13 — How does the BRAIN reach a user's PC? Nothing installs it.~~ — ANSWERED 2026-09-22
**Answered with [Q-PE-12](#q-pe-12--route-2-says-the-files-are-already-in-the-repo-they-are-not--answered-2026-09-22) on 2026-09-22 - one download carries everything, and it asks where to put it.**

---

**The reasoning as it stood, kept because it is the record:**


**Waiting on:** the owner. **Raised 2026-09-21**, from the owner's own question — *"it will install only
the Revit plugin, not the brain?"*

**All ten stages install `products`**, and a product is a Revit add-in: a folder, an `.addin`, an
assembly. The **brain** is none of those. The 395 fragments live in `brain/fragments/`, the MCP server
is `mcp/server/heron_mcp_server.py`, and [`docs/07`](../../../07-installation-and-update.md) puts the
knowledge store in `%APPDATA%\Heron` as **the user's own data that must survive every update**.

**The consequence is not cosmetic.** `heron-bridge` is the only `SHIPPED` product, and it is one end of
a named pipe — the other end is that Python server. So a modeller who installs the AI Bridge and
nothing else gets a **Heron tab whose Connect button opens a pipe nobody answers**. The flagship
product installs correctly and does nothing.

**Three shapes, and they are not equivalent:**

| | |
|---|---|
| The brain **ships with the install** | then every update must be prevented from overwriting what the user has learned — the exact split [D-17](../../../DECISIONS.md) exists to protect |
| The brain **seeds itself on first run** | needs a source to seed from, which is the same question one level down |
| The user **starts empty** | honest, but 395 proven fragments are the product, and a Heron that knows nothing is not the one being sold |

**Blocks:** [Stage 5](02-implementation.md) and [Stage 8](02-implementation.md)'s *Done when* — *"a
modeller installs it unaided and reports the tab appearing"* is satisfiable today while the tab does
nothing.

---

### ~~Q-PE-14 — The brain and the project folder want Claude Code opened in two different places~~ — ANSWERED 2026-09-22
**Answered with [Q-PE-12](#q-pe-12--route-2-says-the-files-are-already-in-the-repo-they-are-not--answered-2026-09-22) on 2026-09-22 - one download carries everything, and it asks where to put it.**

---

**The reasoning as it stood, kept because it is the record:**


**Waiting on:** the owner. **Raised 2026-09-21.**

[`docs/07`](../../../07-installation-and-update.md) states the rule plainly: *"Opening a different
folder should change **which project** Heron is working on — not which Heron is running, and not what
it has learned."*

**Measured:** `.mcp.json` lives in the repository root and starts the server with a **relative** path,
`mcp/server/heron_mcp_server.py`. Claude Code reads that file from the folder it is opened in. So:

| Open Claude Code in | What happens |
|---|---|
| `D:\Heron-AI` | Heron loads — and Heron's own repository is now the "project" |
| `D:\Jobs\Tower-A` | no `.mcp.json`, so **no Heron at all** |

Today "which Heron is running" and "which project" are **the same folder**, which is the one thing that
sentence forbids. It has not bitten because every session so far has been inside the repository.

**The usual answer is a user-scope registration with an absolute path**, so the server is found from any
folder. **Nothing in this repository documents one** — that is an absence found by searching, not a
statement that it cannot be done.

**And it is not only the owner's problem.** Whatever answers [Q-PE-13](#q-pe-13--how-does-the-brain-reach-a-users-pc-nothing-installs-it--answered-2026-09-22)
has to put the brain somewhere, and *this* question decides how anything finds it afterwards. The two
are best answered together.

**Blocks:** nothing built yet. It blocks the first real use of Heron on a job folder, which is a date
rather than a stage.

---

### Q-PE-15 — R-27 wants an audit line from a thing that may not reference the audit log

**Waiting on:** the owner. **Raised 2026-09-22**, building Stage 7.

[R-27](01-requirements.md) says every install, uninstall and update writes an **audit line**, through
[`HeronAudit`](../../../../platform/Heron.Core/HeronAudit.cs).

**The installer cannot reference `Heron.Core`, and that is deliberate.**
[`Heron.Installer.csproj`](../../../../platform/Heron.Installer/Heron.Installer.csproj) says so in its
own comment: *"net8.0, NOT net8.0-windows, and NOT referencing Heron.Core — the engine is
release-independent and Heron.Core is not."* Referencing it would pin the installer to whichever Revit
release happened to be building, which is exactly what [Stage 4](02-implementation.md) already tried
and undid once for `HeronPaths`.

**So R-27 is written against a route that does not exist**, and nothing had noticed because nothing had
tried to write an audit line from the installer before.

| | What it costs |
|---|---|
| The installer writes the line **itself**, in the same format | a second writer of one log, and the format then lives in two places |
| `deploy-addin.ps1` writes it, since it already runs per operation and is release-independent | a third language writing the log, but the script is already the one thing that touches every install |
| A small release-independent audit assembly both can reference | a new project, and the honest answer if the log matters |
| R-27 stays **SHOULD** and is not met by the installer | truthful, and the reason is recorded here |

**It is a SHOULD, not a MUST**, which is why Stage 7 was built without it rather than stopping. But an
uninstall that leaves no trace is the operation you most want a trace of.

**Blocks:** nothing. It leaves [Stage 7](02-implementation.md) item 5 unmet and says so.

---

### ~~Q-PE-16 — Where does the door routes 1 and 2 knock on actually live?~~ — ANSWERED 2026-09-22

**A command beside the window, not a window driven headless and not the script.**
[`platform/Heron.Installer.Cli/`](../../../../platform/Heron.Installer.Cli/), built as
**`heron-install`**, reaching the same `InstallSource` → `InstallerScreen` → `InstallPlan` →
`InstallEngine` that the window reaches. It is the first option of the four below.

> Owner, 2026-09-22, having been shown the four: *"This is the first part: I can install it through
> Windows. I just download, double-click, and install … But if I type the 'heron install' from GitHub
> like that, it will install from there also. That is the second type of installation."*

**Three things that came out of building it, none of which the options above had predicted:**

1. **The name is a safety decision.** `heron-install`, lower case and hyphenated, because
   `HeronInstall.exe` beside `HeronInstaller.exe` is two letters apart — and the window's own
   `.csproj` already warns that *"two files a dot apart is a thing somebody eventually double-clicks
   the wrong one of"*. One of these draws a window to read; the other installs with nothing to confirm.
2. **It installs and never removes.** `R-21` makes unticking mean uninstall, and unticking is a gesture
   a person makes in a window having been asked to confirm. **Nothing on a command line can mean it** —
   a product left out of `--products` was not unticked, it was not mentioned. So `InstallEngine.Install`
   is called and `Apply` never is, and somebody wanting something gone uses the window.
3. **The gate runs before anything on the PC is touched.** Judging a source is pure string work, so a
   hostile address is refused on a machine with no Revit at all, and **no PowerShell has run by the
   time it is**. The first draft had it the other way round, copying the window's order — and the
   window has no untrusted input at that point, which is this door's whole reason for existing.

**And building it found a defect in the gate that was already merged.** Asked for
`https://github.com/<owner>/<repo>` — the owner's *own* repository, just not a release — the refusal
came back saying *"it is not Heron's own repository. Heron will not install software from somebody
else's"*. It is his own. One check was answering two questions and needed three path segments to answer
either, so a bare repository address fell into the wrong half. **The refusal was right and the reason
was false**, which is worse than a blunt no: it sends somebody to check an address that was never the
problem. Split into two checks, and the suite now asks *which* refusal arrived rather than only that
one did — which is why it had passed.

**Route 2 is still blocked on [Q-PE-12](#q-pe-12--route-2-says-the-files-are-already-in-the-repo-they-are-not--answered-2026-09-22),**
and `--from` says so in as many words rather than failing in a way that looks like a bug. The owner
asked for that question to come after route 1 is finished.

---

**The four options as they stood, kept because the reasoning is the record:**

**Waiting on:** ~~the owner~~. **Raised 2026-09-22**, building Stage 6. **Answered 2026-09-22.**

[S7](00-structure.md) says three front doors and one engine. Route 3 is
[`Heron.Installer.App`](../../../../platform/Heron.Installer.App/) — a `WinExe` a person
double-clicks. Routes 1 and 2 are **the AI** installing, and there is nothing for the AI to call.

**The gate on route 1 is built and proven** —
[`InstallSource`](../../../../platform/Heron.Installer/InstallSource.cs) refuses any source but
Heron's own release ([R-48](01-requirements.md), [R-49](01-requirements.md)), and reads no repository to
decide what to install ([R-50](01-requirements.md)). **What is missing is the thing that calls it.**

| | What it costs |
|---|---|
| A **console entry point** beside the window — `--source <url>` or `--from <folder>` | a new release-independent project, a line in `Directory.Build.props` and in `brain/heron_dotnet.py`. Both routes then reach the same engine through one door, which is what S7 asks for |
| An **MCP tool** in [`mcp/`](../../../../mcp/README.md) | it is where the AI already reaches Heron — but that folder's own rule 1 is *"transport only. No BIM logic, no knowledge, no decisions"*, and it is Python while the engine is C#, so it would shell out to something anyway |
| The AI drives **`HeronInstaller.exe`** with arguments | no new project, but the window is a `WinExe`: driven headless it either draws a window nobody asked for or needs a mode that makes it not a window |
| The AI drives **`deploy-addin.ps1`** directly | it already installs one product for one release — but then routes 1 and 2 skip `InstallPlan` and `InstallEngine` entirely, and Stage 6's own opening line says that is **failure**: *"if this stage ends up re-implementing any install rule, it has failed"* |

**The fourth is the one to avoid**, and it is also the easiest to fall into, because the script works
today and calling it looks like progress.

**Route 2 has a second, separate blocker** —
[Q-PE-12](#q-pe-12--route-2-says-the-files-are-already-in-the-repo-they-are-not--answered-2026-09-22): its files are not in
a repository download at all. So route 1 needs only this question answered; route 2 needs both.

**Blocked:** [Stage 6](02-implementation.md) items 1, 2 and 3. Not item 4 or 5, which are properties of
whatever door is built. **Unblocked 2026-09-22.**

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
