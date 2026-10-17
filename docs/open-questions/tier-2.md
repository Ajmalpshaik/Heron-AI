# Open questions — Tier 2

> One section of [the register](../OPEN-QUESTIONS.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## Tier 2 — Blocks a major area

### 🟠 Q-8 — Confirm the Skill vs Fragment definition

Proposed: **Skill** = what the user can ask for (BIM language, user-facing).
**Fragment** = how it is done (technical, internal, reused across many skills).

→ [09 §1](../09-skills-and-fragments.md)

**Answer: confirmed, with the part that decides the design added — a fragment is a *composable piece*, not
a whole how. See [D-29](../DECISIONS.md).**

In a library of several hundred working fragments the unit is smaller than a job: a **filter** answers
*which elements*, an **action** answers *what to do to them*, and they are joined, each declaring what it
needs in scope and what it leaves. A job that genuinely cannot be composed is a **recipe** — a named third
kind, so it cannot quietly become a giant fragment.

That matters because reading *"fragment = how it is done"* as one fragment per job grows the library one
entry per sentence a user might say, reuses nothing, and needs a separate proof for every entry.

---

### 🟠 Q-9 — What promotes a fragment to PRODUCTION?

How many successful executions (suggest **N = 10**)? Who approves the final gate — always you, or can a
company BIM lead approve for their team? Now that the platform is open source, does a maintainer approve
community fragments?

→ [09 §5](../09-skills-and-fragments.md)

**Answer: not a count at all — one recorded proof, and it must include a negative case. See
[D-30](../DECISIONS.md).**

A working library shows why a count is the wrong gate, with a real defect: a fragment whose level filter
matched **zero** elements **and reported success**. That passes ten runs, and a thousand. A count measures
that nothing threw, which is not the property anyone cares about — what caught it was a comparison, 3
against 0, side by side.

So the gate is one dated proof against a real model carrying a **positive** case, a **negative** case
(*it returns nothing when it should*), and a **second route** to the answer where one exists.

**Who approves:** whoever ran it, under their name and date. A company's BIM lead may approve for that
company's scope, because the proof travels with the fragment as evidence a later reader can judge rather
than trust. A community submission meets the same bar; one without a negative case is returned, not
reviewed.

---

### 🟠 Q-10 — Which vector store?

*Recommendation:* SQLite + `sqlite-vec` + FTS5 — one file per knowledge scope, zero install, and all
three retrieval stages in one engine.

→ [05 §5](../05-heron-brain.md)

**Answer: the recommendation, taken — see [D-23](../DECISIONS.md).** Decided on the installation
constraint rather than on retrieval quality: Heron installs per-user with no administrator rights, and a
store needing a service breaks exactly the locked-down machines it is built for. One file per scope also
makes Golden Rule 5's scope separation a fact of the filesystem instead of a `WHERE` clause somebody can
forget.

---

### 🟠 Q-11 — Local or cloud embeddings?

*Recommendation:* local by default — free re-indexing, works offline, and no project content leaves the
machine. Cloud as opt-in.

→ [05 §6](../05-heron-brain.md)

**Answer: local by default, cloud opt-in per scope — see [D-24](../DECISIONS.md).** The deciding argument is
re-indexing: a per-call cost makes rebuilding the index something to avoid, and an index nobody rebuilds
quietly stops matching what is on disk.

**This does not answer [Q-12](#q-12--what-is-the-data-confidentiality-position) and must not be read as
answering it.** Asked directly on 2026-08-28, Ajmal's reply — *"now we are in Claude, am I right, so make
it in this; when we are on the PC I will pull that there and we will test everything"* — was about where
the **work** happens, not about what **project content** may leave a machine. That is a contractual
question about client and authority work, it is his to answer, and it stays open. Local-by-default is the
setting that is safe to hold while it is open.

---

### 🟠 Q-12 — What is the data confidentiality position?

What may be sent to a model provider, from which projects? Is a fully local/offline mode a requirement
or a nice-to-have?

Sharper now that Heron is a public product: **other companies** will run it on **their** clients' models,
under NDAs you have never seen. The default must be safe for the most restricted user, not the least.

*Recommendation:* hybrid, enforced structurally — a project marked confidential is *incapable* of egress.

→ [12 §4](../12-security-and-permissions.md)

**Answer: the model FILE is never uploaded; everything else about the work is fine. See
[D-26](../DECISIONS.md).**

**It took three passes in one day to land there, and the final one is the rule.** The first answer was the
strictest position in the table above — *nothing leaves* — and two clarifications narrowed it. Ajmal,
finally and plainly: *"Any project name, data, typing, or content being in the cloud is not an issue ...
The main thing is that we should not upload the model itself, specifically the RVT or RFA files ... Do not
push the models."*

So the line is **the file, not the information**. A `.rvt`, a `.rfa`, a family or project template — never.
Project names, element counts, sizes, room names, engineering reasoning, code — that is the work, and it
travels like any other conversation with an assistant.

Third part of the same instruction: **project knowledge stays segregated.** Already the design — Golden
Rule 5's *one store per scope*, made literal by [D-23](../DECISIONS.md) — and Rule 5's wording has been
broadened to name the project scope it always covered.

**The earlier framings are recorded rather than erased**, in D-26, because commits from the same day quote
them and a reader has to know which version won.

---

### 🟠 Q-13 — Where do product, data and derived files live?

Now critical: the repository is public, so **client data must be physically incapable of reaching it**.

*Recommendation:* product under the install location, data under the user profile, derived under a cache
location — and the updater physically unable to write to the data class.

→ [06 §2](../06-heron-platform.md), [17 §2](../17-open-source-and-distribution.md)

**Answer: the recommendation, and it is already built. See [D-31](../DECISIONS.md).**

`HeronPaths` has drawn these three classes since Step 1 and is the only place allowed to construct a Heron
path — `check-structure.py` fails anything else that tries. PRODUCT is replaced wholesale on update; DATA
is `%APPDATA%\Heron` and roams, so a preference follows the person; DERIVED is `%LOCALAPPDATA%\Heron` and
deliberately does **not** roam, because a bridge file announcing process 24156 on another PC is
meaningless.

The updater half was **verified rather than assumed**: `deploy-addin.ps1` writes only into the Revit
add-ins folder, never into `%APPDATA%\Heron`. So the public-repository worry is answered structurally —
client data cannot reach the repository because it is never written inside it.

**A question answered by code that already existed.** Worth asking of the other open questions before
designing anything for them.

---

### 🟠 Q-15 — Is persona automatic, manual, or both?

*Recommendation:* infer a default, display it, let the user pin it. Silent mode-switching is a common
source of distrust.

→ [01 §4](../01-vision-and-principles.md)

**Answer: neither — the question had the wrong axis in it. See [D-27](../DECISIONS.md).**

Two assistants doing this job daily for months were read for this question, at Ajmal's suggestion. Neither
switches persona at all, and neither has needed to. **One voice — plain language, always.** What actually
varies is the **shape of the answer**, and it follows the **shape of the request**: a count gets a number,
a breakdown gets a schedule-style table, a narrowed set gets the items and their ids, finished work gets a
short close, and two comparable numbers get a picture unasked.

That dissolves the distrust the recommendation was trying to manage. Inferring a persona is guessing about
a person — wrong sometimes and invisible when wrong. Inferring an answer's shape is reading the request —
deterministic, and visible when it is wrong.

---

### 🟠 Q-41 — Can a job done in one project be repeated in another? *(new, 2026-09-06)*

**Raised by the owner during the D-16 read-back**, unprompted and in his own words: *"maybe from one
project refer same, like that need to do in another project."*

He confirmed [D-16](../DECISIONS.md) as written and then asked for something D-16 does not allow. That is
worth stating plainly rather than filing as a feature: **every binding rule in Heron assumes one job, one
document.** [Golden Rule 20](../14-golden-rules.md) pins the target document by identity and forbids
following the active window; [D-22](../DECISIONS.md) refuses a second chat rather than letting it take over;
[D-16](../DECISIONS.md) builds a picker precisely because a job belongs to exactly one Revit. A job that
starts in Tower A and lands in Podium crosses all three.

**Two readings, and they are different products:**

| Reading | What it means | Rough cost |
|---|---|---|
| **A — repeat the action** | *"Do to Podium what you just did to Tower A."* Heron remembers the operation, not the result, and re-runs it against a second document — re-resolving every element by its own identity, because element ids do not carry across models | Moderate. Needs a replayable record of a job, and a second binding |
| **B — copy the content** | *"Bring Tower A's view filters / line styles / parameters into Podium."* Standard Revit transfer-project-standards territory, and the library already has `COPY_VIEW_FILTERS`, `REMAP_LINE_STYLES` and `COPY_FROM_LINK` | Low. Mostly written already |

**Which one he means is not yet established** and the question stays open until he says. **B is nearly
free and A is a change to the binding model** — so guessing wrong is expensive in one direction and
wasteful in the other, which is exactly the case [D-33](../DECISIONS.md) says to ask about rather than assume.

**Whichever it is, one thing does not move:** a write into a second document is still a write, so
[Golden Rule 17](../14-golden-rules.md)'s preview and [Golden Rule 16](../14-golden-rules.md)'s single undo
apply to the second model as much as the first — and an undo cannot span two documents, so a job that
touches two models cannot honestly be one undo. **That alone may decide the shape of the answer.**

**Answer: BOTH — 2026-09-06. See [D-47](../DECISIONS.md).** Asked which of the two he meant, and told plainly
that one was nearly free and the other changed the binding model, the owner answered *"BOTH"*.

So neither is dropped, and the order is decided by cost rather than by preference: **B ships first**
because most of it exists, and **A follows** because it needs a replayable record of a job and a second
binding before it can be honest.

**And the undo problem is settled by Revit, not by us.** Revit keeps a separate undo stack per document,
so a job spanning two models **cannot** be one Ctrl+Z, and no design makes it one.
[Golden Rule 16](../14-golden-rules.md) is therefore restated rather than broken: **one undo per document**,
and Heron must say so before it starts — *"this touches two models; undoing in Podium will not undo Tower
A."* Saying it afterwards would be the failure Rule 16 exists to prevent.

---

### 🔵 Q-58 — How does a PowerShell script ask `HeronPaths` where something lives? *(found 2026-09-21)*

[`platform/README.md`](../../platform/README.md) rule 3 says
[`HeronPaths`](../../platform/Heron.Core/HeronPaths.cs) is **the only thing that builds a Heron path**, and
[`docs/PROJECT-MAP.md`](../PROJECT-MAP.md) repeats it. [`tools/deploy-addin.ps1`](../../tools/deploy-addin.ps1)
builds two anyway — the Revit `Addins` folder and `%LOCALAPPDATA%\Heron\install-backup\` — and has
done since the rollback was added on 2026-09-19. **A `.ps1` cannot call a C# class**, so the rule as
written cannot be obeyed by the one script that installs Heron.

**Both sides, as they stand.** The rule is right: two places that build a path are two places that can
disagree about where the add-in lives, and a rollback that looks in the wrong folder restores nothing.
The script is also right: it is what runs before any Heron assembly exists on the machine, so it cannot
depend on one.

**Found while building Stage 3** of
[the installer plan](../work-notes/plans/plugin-extension/02-implementation.md), which asked for every path
to go through `HeronPaths`. The install engine itself builds **no** Heron path, so the stage's own
requirement is met; this is the layer underneath it.

**One thing that looked like a third copy was stopped, which is how this was found.** Stage 4's window
has to know whether a product is already installed, which needs the Revit `Addins` folder. It built the
path in C#; [`tools/check-structure.py`](../../tools/check-structure.py) **refused the edit** - the rule is
enforced, not merely written. Referencing `HeronPaths` instead was tried and undone: `Heron.Core`
follows the Revit release and the installer is release-independent, so the reference would pin the
installer to whichever release happened to be building. The path now has **one owner** -
`Get-RevitAddinsFolder` in [`tools/HeronRevit.ps1`](../../tools/HeronRevit.ps1) - and both
`deploy-addin.ps1` and the installer ask it. **So there is one copy, not two, and it is in PowerShell.**

**What is still open is which language should own it.** The rule says C#; the only thing that can run
before any Heron assembly exists on the machine is PowerShell. The shapes are a small `heron-paths`
command the script calls, a generated `.ps1` of path constants that `HeronPaths` owns, or an amendment
saying PowerShell is an accepted second builder with a named test that the two agree - `check-structure`
already exempts `tools/`, and `mcp/client/heron_bridge_client.py` is already a second path owner for the
same reason, so the amendment would be writing down something half-true already. Which one is a decision
about the platform boundary. **Ajmal resolves it**, and until then the disagreement is recorded rather
than closed whichever way makes a task easier.

---

### 🔵 Q-60 — May Heron write a one-time id into a Revit 2020 to 2023 model, so that its answers can be kept? *(found 2026-10-06)*

Since [D-113](../decisions/D-113.md) a project's kept answers - its governing standards, its load runs, its
sprinkler runs - are filed under the model's own id, `Document.CreationGUID`, which arrived at **Revit 2024**.
On 2020 to 2023 nothing read-only tells two models made from one template apart: the Project Information
id is the TEMPLATE's ([FRAGMENT-ISSUES 5b-324](../fragment-issues/section-5b-rows-301-325.md)), the path
changes on Save As, and the document's version id changes on every save. So Heron keeps **nothing** for
such a model, and every answer says so: the standards and the load questions are asked each time.

**The smallest way to give them a key** is an id Heron writes once into the model - on the Project
Information element, through Extensible Storage or a project parameter - behind the Changes switch, the
first time it is asked to keep something there. **It is a change to the owner's model, which is why it is
his to decide and was not built.** Two things to weigh: the id is copied with the model, as CreationGUID
is, so a copy made to start another job keeps the first job's answers; and a model saved as a TEMPLATE
with the id in it would hand every new project the same one - the 5b-324 failure one level up - so an id
found in a model made from a template would have to be replaced on first use, not trusted.

**Ajmal decides**: build the stamp for 2020 to 2023, or accept that those releases keep nothing between
answers. Until he does, they keep nothing.

---

### 🟡 Q-61 — Which model, effort level and fast-mode setting should a modeller's chat run, and are the office PC and the new laptop set the same today? *(found 2026-10-10)*

[FRAGMENT-ISSUES 5b-378](../fragment-issues/section-5b-rows-376-400.md) timed two family builds on the
new laptop on 2026-10-09: 367 s and 792 s, of which Revit's own time was 17.2 s in all and the model's
own turns about 93% - extended thinking alone about 609 s. That chat ran `claude-opus-5-5` at effort
*xhigh* on every request. **The search for why the office PC is quicker never asked what the office
PC's chat runs.** A search of [docs/04 §8.7](../04-heron-mcp.md), [WHAT-TO-INSTALL](../WHAT-TO-INSTALL.md)
and [`tools/heron-ready.py`](../../tools/heron-ready.py) for *effort*, *xhigh* and *fast mode* finds
nothing, and `heron-ready --snapshot` records none of them, so setting the two snapshots side by side
cannot show this difference. It is the largest difference between the two PCs that nobody has examined.

**A fact first, then a decision.** The fact: note the model, effort level and fast-mode setting of a chat
on each PC, and time the same family on both. The decision, if they differ and it matters: whether
Heron's install notes recommend a setting for modelling work, and whether `--snapshot` should record what
it can see of one - a change to the snapshot code, agreed on its own and not made here.

**The owner decides**, after the comparison. Until it is made, the minutes on the two PCs can be neither
blamed on Heron nor cleared of it.

---

### 🔵 Q-62 — Should one `revit_change` call build many family forms? *(found 2026-10-10)*

Today one call builds one form, or several loops that share one plane and one depth. The pump set timed
in [FRAGMENT-ISSUES 5b-378](../fragment-issues/section-5b-rows-376-400.md) had 107 forms: they went in as
about 11 batches of calls, and the chat spent 11 more turns, about 53 s, carrying the new forms' ids
forward so that the cuts, materials and visibility could name them. Each of those calls also brought
back a hand-back of its own ([5b-407](../fragment-issues/section-5b-rows-401-425.md)).

**A proposal, not a decision.** One shape: a write that takes a list of forms - each with its plane,
profile, extent and solid or void - builds them in one call, and hands back their ids as one list a later
step can name. What it would change: the undo - one call is one undo entry (Article 8), so 107 forms
would undo as one, which a modeller may or may not want of a family build; a refusal part way through,
which stays all or nothing; and the library - widening `CREATE_FAMILY_EXTRUSION` and
`CREATE_FAMILY_REVOLUTION` to take several forms is the first thing to weigh, before any new fragment.

**The owner decides**: build a several-forms entry point, and in what shape, or keep one form per call
and accept the turns.

**Built 2026-10-11 as one shape for him to judge - the question stays open until he has.** On 2026-10-11,
in the session that built the pump set, the owner said to make part of Heron what that session showed it
needs. `CREATE_FAMILY_FORMS` (FRG-GEO-060, DRAFT) takes a list of extrusions and revolves, one row each,
in the value or in a file under `%TEMP%\heron-work`, builds them in one call and one undo entry, reads
every form back, keeps none if one row fails, and hands back every id in row order. It is new rather than
a widening of the two single-form tools, for the reasons in
[NEEDS-CHECKING Group DA](../needs-checking/group-da.md), which also holds its proof plan. Left for him:
whether a whole list should undo as one, and whether cuts belong in the same call.

**Answer: one undo per list, and cuts stay a separate call - 2026-10-17. See [D-116](../DECISIONS.md).**
Asked both halves in plain words with a worked example each - a pump family's 107 forms undone by one
Ctrl+Z, and a flange's disc and four holes built in one call and cut in a second - the owner chose
*"Yes, one undo per list"* and *"No, keep cuts separate"*. `CREATE_FAMILY_FORMS` as written already does
both, so no code changed. **This answers the shape, not the proof**: the fragment is still DRAFT until
Group DA has run in Revit.

---

### 🟡 Q-63 — On a PC whose Claude app is the Microsoft Store build, where should Heron's Python side run, and where should it keep what it writes? *(found 2026-10-10)*

**What was found**, on the owner's laptop on 2026-10-10, from his own Terminal panel beside Claude's tool
shell. The Claude desktop app there is the Microsoft Store (MSIX) build, `AnthropicPBC.Claude_fnn82j28hfe8t`.
Windows sends every write its child processes make under `%APPDATA%` and `%LOCALAPPDATA%` to the app's own
`%LOCALAPPDATA%\Packages\AnthropicPBC.Claude_fnn82j28hfe8t\LocalCache`, and shows only those processes the
two laid over each other. Claude's shell and every heron MCP server are such children; Revit and the
person's own terminal are not. Four rows record what follows:
[5b-416](../fragment-issues/section-5b-rows-401-425.md) - Revit's Companion button reads the real
folder and each chat writes its note into the copy;
[5b-417](../fragment-issues/section-5b-rows-401-425.md) - every package Claude installed is in the copy
only, so `heron-ready` said READY inside Claude and NOT READY in his terminal;
[5b-418](../fragment-issues/section-5b-rows-401-425.md) - the knowledge store, project memory, brain audit
and anything else the Python side writes under AppData is in the copy only;
[5b-419](../fragment-issues/section-5b-rows-401-425.md) - a Reset or uninstall of the Claude app deletes
the copy, DATA that `HeronPaths` says must survive.

**What is proposed and not merged:** a warning only - `tools/heron-ready.py` section 2a and
[WHAT-TO-INSTALL §1b](../WHAT-TO-INSTALL.md) tell the person to install from their own terminal, pinned to
the copy's versions. It changes nothing Heron does at runtime and settles none of the four rows.

**The shapes the owner chooses between:**

1. **Leave Heron as it is, and warn.** The cheapest. All four rows stay true on such a PC.
2. **Run the MCP server from a virtual environment outside AppData** - for example a `.venv` on `D:` beside
   the Heron folder, named in `.mcp.json` - so one copy of the packages serves Claude, the person's
   terminal and every check alike. **What it settles and what it does not - reasoned, NOT tried:** a folder
   on `D:` is not redirected, so the packages are in one place and survive a Reset (row 5b-417, half of
   5b-419). It does **not** move what the server WRITES: the server is still a child of the packaged app,
   so the store, the project memory, the audit and the Companion notes still land in the copy, wherever
   its Python lives (rows 5b-416, 5b-418, the rest of 5b-419). Those need their own folders outside AppData
   as well. `HERON_KNOWLEDGE` already moves the store and the project memory (set in `.mcp.json`'s
   `env`). `HERON_AUDIT` moves the brain's audit, but `brain/heron_gaps.py` reads the ADD-IN's trail from
   the same folder, so pointing it away from `%APPDATA%\Heron\audit` would also blind the gaps report.
   The Companion note has no switch at all, and the add-in reads it only from `HeronPaths.Companion`
   ([D-109](../decisions/D-109.md)), so moving it is a change on both sides.
3. **Keep Heron's DATA where an app reset cannot reach it, as a rule** - `HeronPaths` says DATA must survive
   every update, uninstall and reinstall, and on such a PC another program's Reset deletes it. This would
   be a decision about the platform boundary rather than a fix to one tool.

**Measure before deciding** - each is cheap: [NEEDS-CHECKING BK14](../needs-checking/group-bk.md) presses
the Companion button on the laptop, and row 5b-418 names the test for a load report written to the
knowledge folder. **Not checked:** whether Claude is offered for Windows in a form that is not
packaged; and how the merged view behaves when one file name is in both places.

**Ajmal PS decides.** Until he does, nothing Heron does at runtime changes, and the warning - if merged -
is the whole of the response.

**2026-10-17 - the INSTALL half is settled in code; the RUN-TIME half is still his.** Asked that day to
settle Q-63 as part of the one-step installer, with the shapes above put to him, the owner answered *no
preference*. So what every shape needs was built, and nothing that moves DATA:

1. **Heron's packages are installed by the installer**, which a person starts from File Explorer or their
   own terminal - never a child of the app - so they land in the real folders, and Claude's children read
   those through the overlay, as the add-in's files are read today. That is row 5b-417's cause removed for
   every install from now on; a copy that already holds packages Claude installed is unchanged.
2. **Both installers refuse to run from inside the app's copy** and name that route
   ([row 5b-495](../fragment-issues/section-5b-rows-401-425.md)) - an add-in installed from there would
   land where Revit never looks.

That is the one supported route for INSTALLING: from outside Claude, by the installer. DRAFT, not run on
Windows - [NEEDS-CHECKING Group EP](../needs-checking/group-ep.md). **Still open, and still Ajmal PS's:**
where Heron's Python side keeps what it WRITES while a chat runs. Rows 5b-416, 5b-418 and 5b-419 are
untouched by this, because the server is still a child of the app wherever its packages live. Moving DATA
out of AppData is shape 3, a decision about the platform boundary; *no preference* was not read as a
decision to make it.

---

### ✅ Q-64 — Is a picture of a Revit view "model data" under D-82, or may it be handed to the chat? → **Not model data** *(found and answered 2026-10-17)*

**What is asked.** The owner asked on 2026-10-17 for a read-only tool that returns a PNG of a Revit view
into the chat, so the AI can check its own work. It is built as `revit_view_image` and the add-in's
`capture_view` - [D-123](../decisions/D-123.md), **Proposed** - and it is not merged.
[D-82](../decisions/D-82.md) says *"Revit models, families, model data - never leave the PC"*;
[D-26](../decisions/D-26.md) put element data - counts, sizes, parameters - in the conversation. A
picture sits between the two, and neither was written with one in mind.

**The two answers, and what each does:**

1. **Not model data.** It cannot be opened or rebuilt into a model, and it is what the model shows, the
   same kind of thing as a count. The tool ships as D-123 describes.
2. **Model data.** It shows the geometry and layout of the building at a glance, and in the conversation
   it has gone to the cloud. **The tool does not ship**: its pull request is closed, or the tool and
   `capture_view` are taken out of it before merge.

D-123 holds the second thing to accept with the first - that `capture_view` is the first ANALYZE
operation to open a transaction, always rolled back - and [NEEDS-CHECKING group ER](../needs-checking/group-er.md)
the run that would prove it.

**Answer: NOT MODEL DATA — 2026-10-17. See [D-123](../decisions/D-123.md), now Accepted.** The owner:
*"A picture is not model data, ship it"*. `revit_view_image` ships as D-123 describes; it is still
DRAFT until [NEEDS-CHECKING group ER](../needs-checking/group-er.md) is run in Revit.

---

### ✅ Q-65 — Does selecting or zooming to elements inside a linked model count as "touching" it under Article 7? → **No - it needs no confirmation** *(found and answered 2026-10-17)*

**The words.** [Article 7](../../HERON_CONSTITUTION.md): *"Do not execute destructive Revit operations
without permission. Deleting elements, purging, bulk parameter writes, family reloads, workset changes and
anything touching a linked model require explicit confirmation for that specific operation."*

**Why it is asked now.** On 2026-10-17 Heron gained a door that changes only what is on screen -
`revit_show`, through the add-in's `run_fragment_show` (EXECUTE, no transaction open, runs with Changes off)
- and a fragment for it, `ZOOM_TO_LINKED_ELEMENTS` (FRG-SEL-039, DRAFT), which zooms the active view to
elements inside a loaded link and, on Revit 2023 and later, selects them as linked elements. It changes
nothing in the model and nothing in the link. Whether that is "touching" the link is not something Heron can
decide for the owner.

**Two readings, and what each costs.**

1. **It does not count.** The Article's heading is *destructive* operations, and a selection or a zoom
   destroys nothing - Ctrl+Z has nothing to undo after either. `ZOOM_TO_LINKED_ELEMENTS` would then run
   like `ZOOM_TO_ELEMENTS`, with no question in front of it.
2. **It counts.** "Anything touching a linked model" is read literally: the chat asks for the modeller's
   yes, in the chat, before every zoom or selection inside a link - one more turn each time.

**What Heron does until the owner answers: the reading that ASKS (2).** `heron_tools.link_refusal` holds any
fragment declared above ANALYZE whose contract takes `linkName` until the host says the user agreed, by
calling `revit_show` again with `link_confirmed=true`; nothing is sent to Revit before that. A READ of a link
is not held - [D-59](../decisions/D-59.md) settled reading one, and `revit_read` never asked. **What is
NOT covered either way:** `revit_change` holds nothing in code for a link - it tells the host to ask, as
[FRAGMENT-ISSUES row 5b-161](../fragment-issues/section-5b-rows-151-175.md) records for every destructive
change.

**Ajmal PS decides.** If the answer is (1), the change is one line in `link_refusal` and one in
`tests/test_read_door.py`, and this question is promoted to a decision. Measuring either way:
[NEEDS-CHECKING Group ES](../needs-checking/group-es.md) ES5 to ES7 run the fragment itself.

**Answer: (1), it does not count - 2026-10-17. See [D-124](../DECISIONS.md).** The owner: *"Q-64:
selecting/zooming linked elements is not touching, no confirmation"* - this question's first number; it is Q-65 since #556 gave Q-64 to another on 2026-10-17. The hold was taken out the same day
rather than switched off: `link_refusal`, `LINK_NEEDS` and `revit_show`'s `link_confirmed` are gone, and
`tests/test_read_door.py` holds that a fragment the show door may run goes through it whether or not it
works inside a link. Changing a link is still Article 7's, with its confirmation.

---

### ✅ Q-66 — Where does a proof on a second Revit release go, and who writes a second route on a fragment already PROVEN? → **An entry of `proof.releases`, and `accept --second-route`** *(found and answered 2026-10-17)*

**What was found**, preparing [NEEDS-CHECKING Group EQ](../needs-checking/group-eq.md) - a core set of PROVEN
fragments proved again on every release, to back the *"2020 → latest"* claim with evidence. Three things in
Heron stop it, each on purpose:

1. **`tools/batch-prove.py` refuses a PROVEN or PRODUCTION fragment** as `ALREADY` and never sends it to Revit.
   Its docstring says *"There is deliberately no flag to override that"* - it was written against re-proving
   unchanged code on the same release, which bought nothing.
2. **`heron_validate.py accept` refuses a PROVEN fragment whose proof still matches its code** - *"Signing it
   again would overwrite a good proof with the same thing."*
3. **A fragment's `proof:` holds ONE record**, and its release is prose inside `model:`.
   [`brain/heron_matrix.py`](../../brain/heron_matrix.py) reads one release per fragment and says so: *"a proof
   on 2024 is evidence about 2024 ... Nothing here spreads a proof sideways"*, and *"if PROOF_REQUIRED ever
   grows a `revit:` field this should read that instead."*

So a run of `create-floor` on 2025 that passes both halves has nowhere to be written, and the only way to
record it today would overwrite the 2024 proof. **A second route has the same problem from the other side**:
`second_route` is a field of that one record, and `accept` will not reopen it.

**Recommended - reasoned, not built:** keep `proof:` as the record that made the fragment PROVEN, and add a
list beside it, **`proof.releases:`**, one entry per further release, each with the same fields D-30 asks for
(`date`, `by`, `model`, `positive_case`, `negative_case`) and the fingerprint of the code it ran. Then:

- `batch-prove --release` runs only PROVEN fragments, and only on a release they have no entry for. It writes
  drafts and never touches `heron-status` - the guarantee it already checks.
- `accept --release` writes one entry, signed by a person, and refuses a release already recorded.
- `heron_matrix.py` marks a cell PROVEN from either the proof or an entry - never from the claim.
- A second route for a PROVEN fragment is written into `proof.second_route` by `accept --second-route`,
  signed, from a recorded run - never typed by hand.

**What the other choices cost.** Leaving it as it is keeps the claim unbacked. Re-proving into `proof:`
loses the earlier release's record. A separate register outside the fragment would be read by nothing
that reads fragments today.

**Ajmal PS decides.** Until he does, EQ1 to EQ6 (loading the add-in on each release) and EQ8 (the routes
themselves, by hand in Revit) can still be run and recorded in Group EQ; only EQ7 waits.

**Answer: the recommended shape - 2026-10-17. See [D-131](../decisions/D-131.md), Accepted.** Asked to accept
the recommendation or choose another, the owner chose *"Accept recommended (Recommended)"*. Built the same
day: `batch-prove --release`, `heron_validate.py accept --release` and `accept --second-route`, and the matrix
reading `proof.releases`, held by `tests/test_release_proofs.py`. EQ7 has its commands; every run is still
NEEDS REAL REVIT, and no fragment is proven on a new release by the answer.

---

### ✅ Q-67 — Should the Heron server start on a non-Windows machine with only its library tools, and every Revit tool answering that Revit is not reachable here? → **Yes - the library alone, and no Revit tool listed** *(found and answered 2026-10-17)*

**What was found**, in a cloud session on 2026-10-17. The last lines of
[`mcp/server/heron_mcp_server.py`](../../mcp/server/heron_mcp_server.py) exit **2** when `os.name != "nt"` -
*"Heron's bridge uses Windows named pipes. Revit is Windows-only."* - before a single tool is served. So on
Linux the host lists **no** `heron` tool and reports the server `CONNECTION_CLOSED`: not only the Revit tools
but the ones that never touch Revit - `heron_lookup`, `heron_resolve`, `heron_capabilities`, `heron_method`,
`heron_standards`. The [`heron-fragment-author`](../../.claude/agents/heron-fragment-author.md) agent is given
exactly three Heron tools, `heron_lookup`, `heron_resolve` and `heron_capabilities`, and **in a cloud session
it cannot search the library it is told to search first**. The exit is deliberate
([docs/38 §4](../38-the-cloud-environment.md) says so), and the same module imports on Linux:
`tests/test_mcp_serves.py` and `tests/test_served_claims.py` call its tools in-process there. Whether a
non-Windows machine should be served the library without Revit was left by
[row 5b-162](../fragment-issues/section-5b-rows-151-175.md) as *"a design question nobody has asked"*. A
search of DECISIONS, `decisions/`, this register and the Constitution on 2026-10-17 found nothing that
decides it either way. [D-02](../decisions/D-02.md) makes the bridge a Windows named pipe; it says nothing
about the tools that do not use the bridge.

**The one thing a yes must not break.** [`AGENTS.md`](../../AGENTS.md) tells an agent: *"If no `heron` tools
are listed, Heron is not connected"* - say so, ask for `python tools/heron-ready.py`, stop. A library-only
server lists `heron` tools where no Revit can ever be, so that sentence would read as *connected* when it is
not. A yes therefore owes two things with it: the Revit tools **not listed** off Windows - never listed and
then hanging or failing late - and the AGENTS.md line reworded to the test that still holds, *no `revit_`
tool listed means no Revit here*. The session-start line [#573](https://github.com/Ajmalpshaik/Heron-AI/pull/573)
added says, off Windows, *"no Revit and no Heron tools in this session"*
(`session_line.py` in the [`heron-session`](../../.claude/skills/heron-session/SKILL.md) skill); a yes changes it to say the
library tools are there and the Revit ones are not.

**Recommended - reasoned, not built:** **yes**, on non-Windows only, with the library tools served and every
tool that needs the bridge left unregistered, so the host's own tool list says there is no Revit here. Which
`heron_*` tools reach the bridge is derived from the code when it is built, not typed here. Windows behaviour
does not change by one line. Tests on Linux start the real server over stdio, list its tools, and fail if a
bridge tool appears or a library tool is missing.

**What the other choices cost.** Leaving it as it is keeps every cloud session - and the fragment-author agent
in it - searching the library with `grep` instead of the ranked lookup a modeller's chat gets, so the two can
disagree on what exists. Listing the Revit tools and having each refuse is honest, but it is a tool list that
says Revit is here when it is not, which is the trap AGENTS.md's sentence exists to avoid.

**Ajmal PS decides.** Until he does, the server keeps exiting 2 off Windows and nothing is built.

**Answer: yes, as recommended - 2026-10-17. See [D-132](../DECISIONS.md).** Ajmal PS, asked in plain words
what the yes would change: *"yes"*. Off Windows the server registers `heron_tools.LIBRARY` - the five tools
this question names - and nothing else, so no `revit_` tool is listed; its instructions and a lookup's version
line say there is no Revit on this machine. On Windows nothing changes. AGENTS.md's test and the
`heron-session` start-up line are reworded to match, and `tests/test_mcp_stdio.py` starts the real server off
Windows and holds all of it.

---

### 🟡 Q-69 — May Heron save the model it is working in - Save, or Save As to a path the modeller gives - once its Undo entry has closed? *(found 2026-10-17)*

**What happened.** Twice in two days a whole build was lost because the model had never been saved and
Revit was closed - a house with 9 sheets and a 32-floor tower, on 2026-10-13 and 2026-10-14
([row 5b-513](../fragment-issues/section-5b-rows-401-425.md)). Heron could not have saved either.
`SAVE_DOCUMENT` version 2 (FRG-DOC-033, PROVEN, PUBLISH) saves only ANOTHER open document that already has
a file, and refuses the one Heron works in by name ([row 5b-365](../fragment-issues/section-5b-rows-351-375.md)).

**Why, from the code.** Revit saves nothing while any transaction, sub-transaction or transaction group the
API opened is still open - *"Operation is not permitted when there is any open sub-transaction,
transaction, or transaction group"*, the same words for `Document.Save` and `Document.SaveAs` (RevitAPI.xml
2024; 2020 and 2027 read the same on 2026-10-07, row 5b-365). Every fragment `revit_change` runs goes to
the add-in's write path, which opens ONE `TransactionGroup` in the document for the whole run
([`RevitFragment.cs`](../../revit/Heron.Revit.Addin/RevitFragment.cs) line 706) and assimilates or rolls it
back at the end (line 900) - Article 8's one undo entry. So a fragment can never save the document it runs
in, however it is written.

**What would work, read from the code - NOT RUN.** In the same request, AFTER that group is closed (lines
900 to 916, before the reply is built), nothing of Heron's is open and the Revit remarks allow a save. So
the add-in could run the save itself at that point, on the fragment's behalf: `SAVE_DOCUMENT` version 3
asks for it - the document it works in, by its own file for a Save, or a full `.rvt` or `.rfa` path the
modeller typed for a Save As of a model that has none - and the add-in does it once the group has closed,
then puts what happened at the end of the reply. Still behind the Publish switch; never a file name chosen
by Heron; `SaveAsOptions.OverwriteExistingFile` left at Revit's own default, false, unless the modeller
said to replace the file. The group itself does not change: one request, one `TransactionGroup`, one undo
entry, and the save is not in it - a save is not a model change and cannot be undone. Where Revit asks
the modeller Keep or Discard ([D-126](../decisions/D-126.md)), the box is answered inside the group, before
that point (line 879), so a save there could only ever follow a Keep.

**Why it was NOT built - three things need the owner, and the third is a guard:**

1. **The add-in would do work of its own after the group closes.** Today everything a request does is
   either the fragment's run inside the group or the reply. The ambient needs that already let a fragment
   ask the add-in for something - [D-112](../DECISIONS.md)'s failure discipline, [D-114](../DECISIONS.md)'s family load
   options - each came with a decision of its own, and this would be a third, of a new kind: an act the
   add-in performs after Heron's Undo entry is closed.
2. **What a preview does with a save.** [D-55](../decisions/D-55.md): a write's preview is the run itself,
   rolled back. A save cannot be rolled back. Version 2 already writes its other document even on a preview
   - its proof records a rolled-back group and `saved true` - so the library has never said which is
   right. Either a preview saves too, or the add-in saves only when the group was KEPT and says so on a
   preview; the second keeps a preview free of anything irreversible, and means a proof of this route must
   be run with `--apply`. **And [D-117](../decisions/D-117.md), merged the same day, refuses `--apply` -
   and `revit_change` - for a fragment that is DRAFT**, which version 3 would be until proved. The one way
   a DRAFT's run is kept, [D-119](../decisions/D-119.md)'s `try-once` behind Keep or Discard, runs only on
   a model saved with nothing unsaved ([D-121](../decisions/D-121.md)) - the one state in which a save has
   nothing to do and a Save As is not needed. So with a preview that does not save, the route could never
   be proved; with a preview that saves, it can be proved today, on a scratch model, the way version 2 was.
3. **A Save As renames the model, and the chat's model pin would read that as a DIFFERENT model** -
   Golden Rule 20's guard, Articles 12a and 12b. The pin compares the model's own id where both sides have
   one ([`heron_write.py`](../../mcp/server/heron_write.py) line 255), and only a match on it or on the file
   path lets a new title through (line 461, *"a save can rename it; same document"*). But that id,
   `Document.CreationGUID`, arrived at Revit 2024 and is null on 2020 to 2023 and on every family document
   ([`RevitOperations.cs`](../../revit/Heron.Revit.Addin/RevitOperations.cs) line 664, measured
   2026-10-06), and a model with no file has no path. So on Revit 2020 to 2023, and for a family on any
   release, a Save As of a model or family that was never saved leaves the pin holding the title alone; the reply comes back under the new title, and `revit_change` answers *"Heron will not change a
   model you did not point it at ... Nothing has been sent to Revit"* (`heron_mcp_server.py` line 1921)
   about a model it has just saved. The next call is then aimed at the old title (line 1455) and refused
   until the modeller says *use this model*. Letting the pin follow a rename is a change to that guard.

**The shapes the owner chooses between:**

1. **Leave Heron as it is.** The methods ask the modeller to save before a big build, and
   `REPORT_OPEN_DOCUMENTS` version 2 says which open model or family has NEVER BEEN SAVED. The cheapest; it
   still rests on the modeller pressing Ctrl+S, which is how both builds were lost.
2. **Save after the group, from the add-in - recommended.** As above: `SAVE_DOCUMENT` version 3, an
   optional switch for a Save As of a never-saved model and one for replacing a file, and the add-in saving
   once the group has closed. With it, two choices: a preview saves too, as version 2's save of another
   document already does - the only way it can be proved while D-117 refuses a DRAFT kept for real, and
   D-55 then gains one sentence saying a save is not rolled back - and the pin accepts a new title only
   when the reply says THIS run did the Save As of the pinned document. One new decision for the
   after-the-group act; nothing in Article 8 changes.
3. **A save operation of its own**, beside `move_elements`, declared Publish in the operation registry and
   run by the add-in with no group at all. No change to the fragment executor, but a new tool for the chat,
   a new entry in the registry and the permission gate, and the same pin change as 2.

What none of the three can do: save a model the modeller never asked to save (Article 10), or choose its
name.

**Measure before deciding** - [NEEDS-CHECKING Group EN](../needs-checking/group-en.md) EN4, on the owner's
word only: a scratch fragment on the read path - no transaction and no group anywhere - calling
`SaveAs` on a NEVER-SAVED scratch project, to see whether Revit lets an ExternalEvent callback save at all.
**Not measured:** any of it. Whether a save from the callback raises Revit's own dialogs (a workshared
model, a file on a network folder), what the add-in's dialog watch does with one met after the group
([Group EL](../needs-checking/group-el.md), merged the same day), and how long the request runs for a big model - the add-in answers
*still_running* after 60 seconds (`revit.operationTimeoutSeconds`) and the save carries on.

**Ajmal PS decides.** Until he does, nothing Heron does at runtime changes: `SAVE_DOCUMENT` still refuses
the model it works in, and the methods ask the modeller to save first.

---

### 🟡 Q-70 — On a PC whose Claude app keeps its own copy of `%APPDATA%\Heron`, which `heron.config` should Heron believe, and should it say when the two differ? *(found 2026-10-17)*

**What was seen**, on the owner's laptop on 2026-10-17, timing the fragment warm-up for
[Group DA](../needs-checking/group-da.md) ([row 5b-1004](../fragment-issues/section-5b-rows-401-425.md)).
The Claude desktop app there is a packaged Windows app, and it keeps a private copy of `%APPDATA%\Heron` -
`config\heron.config` included - under its own `LocalCache` folder - the same copy Q-63,
above, asks about for packages and data. So there are **two settings files on one PC**:

| who | uses |
|---|---|
| Heron's MCP server, through `config_path()` in [`heron_config.py`](../../mcp/client/heron_config.py) | the copy |
| a Revit started from a chat's shell | the copy |
| a Revit the owner starts himself | the real file |

Each side writes only its own. A switch on the ribbon - **Changes, Admin or Publish** - pressed in one kind of
Revit is not seen by the other kind, nor by the server when the Revit is one the owner started. Measured
both ways that day: `fragments.warmUp = false` written from the chat's shell landed in the copy and the real
file kept `true`, and the Changes switch pressed in a chat-started Revit rewrote the copy and left the real
file as it was.

**What is still safe, read from the code - NOT RUN on the laptop as a case of its own.** The add-in decides
every change from ITS OWN file: the permission gate loads `heron.config` fresh on every call
([`RevitOperations.cs`](../../revit/Heron.Revit.Addin/RevitOperations.cs) line 339), and its refusal names the
file it read ([`HeronPermissions.Explain`](../../platform/Heron.Core/HeronPermissions.cs)). So the server's
copy can never open a gate Revit has closed. What it can do is **say the wrong thing**:

1. **`revit_health`'s write-gate line** is built from the server's file
   ([`heron_mcp_server.py`](../../mcp/server/heron_mcp_server.py) `revit_health`,
   [`heron_health.py`](../../mcp/server/heron_health.py) line 177). It can say *"closed - Heron cannot change the
   model"* while the Revit in front has Changes on, or the reverse. `heron_diagnose.py` reads the same file.
2. **The server's waiting times come from its file too** - `response_timeout`, `fragment_timeout` and
   `try_once_timeout` are worked out from `revit.operationTimeoutSeconds`, `revit.askTimeoutSeconds` and
   `write.askBeforeKeeping`, so that the server always waits longer than the add-in. If the two files differ
   on one of those, that can turn round: the server gives up first and reports "no answer" while Revit is
   still working, or while the Keep or Discard box is still open - and a change is sent so that it cannot be
   asked twice. This is the failure `heron_config.py` was written to prevent. Reasoned from the code, not
   seen; nobody has edited a timeout on the laptop.
3. **Two Revits on one PC disagree with each other** when one was started from a chat and one by the owner.
   The add-in re-reads the Companion switch every two seconds because *"another Revit ... may have flipped
   the shared setting"* ([`HeronLiveState.cs`](../../revit/Heron.Revit.Addin/HeronLiveState.cs)); on this PC
   the setting is not shared.

**What does NOT help:** pointing `HERON_CONFIG` at the real file's path. The app redirects the path itself -
the row's `warmUp = false` was written to the real path, spelled in full, and landed in the copy.
Reasoned from that, not tried.

**The shapes the owner chooses between:**

1. **Heron names the file it read, in `revit_health`.** One line under the write gate, e.g. *"read from
   ...\LocalCache\Roaming\Heron\config\heron.config"*. **Cost:** the Python side only - one line and its test;
   no add-in rebuild, no protocol change. **What it leaves:** the owner has to notice the `LocalCache` in the
   path and know what it means; the line can still be wrong about the Revit in front, and the waiting times
   (2) and the two Revits (3) are untouched.
2. **The add-in reports its own switches and the server warns when they differ - recommended.** The bridge's
   `info` reply ([`BridgeServer.cs`](../../revit/Heron.Bridge/BridgeServer.cs) line 633) gains the file the add-in
   read and the values it holds for the switches and the timeouts. `info` already answers without taking the
   Revit and outside the gate, so it is no new operation - the `ping`, `info`, `release` allow-list in
   [`tests/test_tool_registry.py`](../../tests/test_tool_registry.py) stays three. `revit_health` then states
   the gate as **Revit** has it, and says plainly when the server's file differs and which file each side read.
   **Cost:** a C# change in `Heron.Bridge`, rebuilt and redeployed for every release 2020 to 2027, and a run on
   the laptop with both kinds of Revit; an older add-in sends nothing, so the server must say *"not reported"*
   rather than guess. Taking the server's waiting times from Revit's reported values as well (2) is a second
   step of the same change, and is more work: one of them, `RESPONSE_TIMEOUT_S` in
   [`heron_bridge_client.py`](../../mcp/client/heron_bridge_client.py), is read once when the client starts,
   before any Revit has answered. Settles (1)
   and, with the second step, (2); not (3) - two Revits still read two files.
3. **Change nothing, and write it down.** A paragraph in [WHAT-TO-INSTALL §1b](../WHAT-TO-INSTALL.md), where
   the Store build is already described: press the switches in a Revit you started yourself, and do not
   trust the write-gate line from a chat on such a PC. **Cost:** nothing to build. All three problems above
   stay true, and the rule has to be remembered by whoever is at the PC.

Q-63's shape 3 - Heron's data kept outside `%APPDATA%` - would settle this too, if `heron.config` moved with it,
since both sides would then read one file. That is a decision about the platform boundary and costs what
Q-63 says; it is not repeated here as a fourth shape.

**Not checked:** whether the office PC has a copy at all - a PC where Claude is not a packaged app has
none; and which other keys of `heron.config` differ between the two files on the laptop today.

**Ajmal PS decides.** Until he does, nothing Heron does at runtime changes: the add-in still decides every
change from its own file, and `revit_health` still reports the server's.

---
