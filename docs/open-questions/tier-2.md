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

**This does not answer [Q-12](#-q-12--what-is-the-data-confidentiality-position) and must not be read as
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

### ✅ Q-58 — How does a PowerShell script ask `HeronPaths` where something lives? *(found 2026-09-21)* → **decided 2026-11-15 on the owner's delegation**

**Answer: The add-in's path code writes a folder list the install script reads - 2026-11-15, decided by Claude on Ajmal PS's delegation of 2026-11-15 - *"the remaining thing you can answer it"* - and not read back to him; his word overrules it at any time, and no decision record is written until he reads it back.** **Why:** It keeps one owner of Heron's folders - the C# path code - as the rule says, while the script still runs before any add-in is installed; a check compares the written list with the code. **Work:** built 2026-11-16, the Windows run owed. [`HeronPaths`](../../platform/Heron.Core/HeronPaths.cs) owns the install scripts' two folders - `RevitAddins(release)` and `InstallBackup(release, folder)` - and [`tests/Heron.Folders.TestHost`](../../tests/Heron.Folders.TestHost/) compiles `HeronPaths.cs` by source and prints them as templates: a Windows special folder by name, as `Environment.GetFolderPath` takes it, and the parts under it, with `{release}` and `{folder}` left blank. [`tools/heron-folders.py --write`](../../tools/heron-folders.py) writes that to the committed [`platform/heron-folders.json`](../../platform/heron-folders.json); `Get-HeronFolder` in [`tools/HeronRevit.ps1`](../../tools/HeronRevit.ps1) reads it with `ConvertFrom-Json` and the same `GetFolderPath` call, and stops by name, before anything is changed, when it is missing; `Get-RevitAddinsFolder` and [`deploy-addin.ps1`](../../tools/deploy-addin.ps1)'s backup folder ask it, so no install script spells either folder; heron-project.zip carries it. **2026-11-16, after main's D-167 merged in:** its second-copy check spelled Revit's all-user Addins folders in `HeronRevit.ps1` and `tests/test_heron_folders.py` went red on them, so `HeronPaths.RevitAllUserAddins` owns both - read only, and declared rather than read back, because .NET has no ProgramFiles folder off Windows - and `Get-RevitAllUserAddinsFolders` asks the list for them (AB11 step 4). **The check:** `python tools/heron-folders.py` exits 1 when the committed list is not what the code says - measured red on a hand edit of the list and on a `HeronPaths` change not regenerated, green once regenerated - and [`tests/test_heron_folders.py`](../../tests/test_heron_folders.py) runs it with the scripts' own checks in CI's every-suite job; on the code as it stood it failed 26 checks. **Owed:** no PowerShell ran here - the deploy, the rollback and the installer window reading the list on Windows PowerShell 5.1 are NEEDS-CHECKING [AB11](../needs-checking/group-ab.md). The other places that spelled the Addins folder, [row 5b-1746](../fragment-issues/row-5b-1746.md), read the list too since 2026-11-16 - fixed in code, the Windows run AB11 step 5.

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

### ✅ Q-60 — May Heron write a one-time id into a Revit 2020 to 2023 model, so that its answers can be kept? *(found 2026-10-06)* → **answered 2026-11-16 by the owner**

**Answer: No - Heron writes nothing into a Revit 2020 to 2023 model, and those releases keep nothing between answers, as today - 2026-11-16, the owner's answer, read back: Ajmal PS was shown this question with Claude's suggestion and answered in his own words, *"go with you suggetions"*.** **Why:** The id would be a change to the owner's model, copied with it into the next job and handed by a template to every project made from it; Revit 2024 and later have the model's own id ([D-113](../decisions/D-113.md)). **Work:** none - nothing changes. On 2020 to 2023 the standards and the load questions are still asked each time, and every answer already says so. No decision record: no behaviour moves.

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

### ✅ Q-61 — Which model, effort level and fast-mode setting should a modeller's chat run, and are the office PC and the new laptop set the same today? *(found 2026-10-10)* → **answered 2026-11-16 by the owner**

**Answer: Heron's install notes recommend one setting for modelling work - effort high, not xhigh, and fast mode on where it is offered - and say plainly it is not yet timed - 2026-11-16, the owner's answer, read back: Ajmal PS was shown this question with Claude's suggestion and answered in his own words, *"go with you suggetions"*.** **Why:** The one measured run, [row 5b-378](../fragment-issues/section-5b-rows-376-400.md), was at effort xhigh, and about 93% of its two family builds was the chat's own turns - extended thinking alone about 609 s; the effort level is the setting that can move that share, and only a timing can say how far. **Work:** done 2026-11-16 - [WHAT-TO-INSTALL §5b](../WHAT-TO-INSTALL.md#5b-the-chats-own-setting-for-modelling-work---recommended-not-yet-timed) gives the setting and says it is not timed. **Owed:** the timing, [NEEDS-CHECKING Group HF](../needs-checking/group-hf.md) HF8, on the laptop. Not part of this answer, and not done: `heron-ready --snapshot` recording the setting, the change to the snapshot code this question names as one agreed on its own. No decision record: it is advice in the install notes, and nothing Heron does changes.

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

**2026-10-18 - the office PC is retired.** The owner, Ajmal PS: *"no office pc no more"*
([NEEDS-CHECKING Group HF](../needs-checking/group-hf.md), HF5). The comparison above cannot be made as
written. What this question still asks of the laptop alone - which model, effort level and fast-mode
setting a modeller's chat should run - is the owner's to say; this note records the fact and answers
nothing.

---

### ✅ Q-62 — Should one `revit_change` call build many family forms? → **Yes: one undo per list, and cuts stay a separate call ([D-116](../decisions/D-116.md))** *(found 2026-10-10, answered 2026-10-17)*

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

### ✅ Q-63 — On a PC whose Claude app is the Microsoft Store build, where should Heron's Python side run, and where should it keep what it writes? → **Install from outside the app; keep the data outside its copy ([D-135](../decisions/D-135.md))** *(found 2026-10-10, answered 2026-10-18)*

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

**2026-10-17, evening - measured on Windows, and the run-time half put to the owner.**
[Group EP](../needs-checking/group-ep.md) ran on the laptop and measured two things this question said were
not checked. **One name in both places:** the copy's file wins - every file under `mcp`, `cryptography`,
`pydantic` and `pydantic_core` present in both read byte for byte as the copy's - while files only the real
folder has show through, and the version Python reports is the real folder's
([row 5b-1065](../fragment-issues/section-5b-rows-401-425.md)). **A write from inside into a folder that
already exists on the real disk lands on the real disk**, not in the copy
([row 5b-1066](../fragment-issues/section-5b-rows-401-425.md)) - so "every write under AppData is sent to
the copy", as written above, holds only where the folder did not exist outside. The installer, run outside
the app, also built a second knowledge store in the real `%APPDATA%\Heron\knowledge` at 22:11; the chats
still read the copy's.

Asked in plain words where Heron should keep what it saves while a chat runs - each project's memory, the
Companion's notes, the tool index - the owner answered: *"OK you can decide this thing. Which one is the
best best thing? ... Whatever good you can do it."* **Chosen, on that delegation: shape 3** - Heron's DATA
lives where a Reset or uninstall of the Claude app cannot reach it, because `HeronPaths` already says DATA
must survive every update, uninstall and reinstall, and rows 5b-416, 5b-418 and 5b-419 are that rule broken
on this one kind of PC. **Nothing was moved on 2026-10-17.** The build is its own task: it has to measure a
folder that exists in BOTH places first, because the copy already holds `Heron\knowledge`, `Heron\audit`
and `Heron\config`; and the Companion note moves on both sides, Python and add-in ([D-109](../decisions/D-109.md)).
This question stays open until that task records its decision with what it measured.

**2026-10-18 - answered: built as [D-135](../decisions/D-135.md), after measuring the case left open above.**
On the laptop at 23:53 on 2026-10-17, a throw-away folder was made in the copy from Claude's shell and on the
real disk from the owner's Terminal panel, then written from inside. **Where a folder exists in both places,
everything new goes to the copy** - a new file, a new folder, an edit to a file both hold, and a
write-then-rename, even over a file only the real disk held, which leaves a new copy hiding the real one;
only an append to a file the real disk alone holds reached it. Where the folder exists only on the real disk,
new files and new folders reach it ([row 5b-1066](../fragment-issues/section-5b-rows-401-425.md)). The profile
and the temporary folder were written from inside and seen real from outside. So Heron's Python side now keeps
its knowledge store, every project's memory and the brain's audit in `%USERPROFILE%\.heron` on Windows
(`HERON_DATA` overrides), carrying what the copy and `%APPDATA%\Heron` hold into it the first time - copied,
never moved - and the Companion's notes are in `%TEMP%\Heron\companion` on both sides. The add-in's settings and
trail stay in `%APPDATA%\Heron`, where Revit writes the real disk. **Still open elsewhere:** which
`heron.config` to believe is Q-70, below;
which package versions such a PC should end with is [row 5b-1065](../fragment-issues/section-5b-rows-401-425.md).
Nothing in the copy was removed - that is the owner's to decide.

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

### ✅ Q-69 — May Heron save the model it is working in - Save, or Save As to a path the modeller gives - once its Undo entry has closed? *(found 2026-10-17)* → **answered 2026-11-16 by the owner**

**Answer: Shape 2 - the add-in saves after Heron's group has closed, only when the modeller asks, and Heron never picks a name itself - 2026-11-16, the owner's answer, read back: Ajmal PS was shown this question with Claude's suggestion and answered in his own words, *"go with you suggetions"*.** Recorded as [D-171](../decisions/D-171.md), which also carries the two choices shape 2 is written with below - a preview saves too, and the pin follows a rename only for this run's own Save As. **Why:** It is the one shape that lets Heron save the model it works in while Article 8's one undo entry stays as it is, and both lost builds were lost to a model nobody had saved. **Work:** owed: being built - `SAVE_DOCUMENT` version 3, the add-in's save after the group and the pin's rename, in another session from 2026-11-16; then proved on a scratch model, with [Group EN](../needs-checking/group-en.md) EN4 and EN5 measured. Until it is, nothing at runtime changes.

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
([`RevitFragment.cs`](../../revit/Heron.Revit.Addin/RevitFragment.cs) line 779) and assimilates or rolls it
back at the end (line 1041) - Article 8's one undo entry. So a fragment can never save the document it runs
in, however it is written.

**What would work, read from the code - NOT RUN.** In the same request, AFTER that group is closed (lines
1052 to 1060, before the reply is built), nothing of Heron's is open and the Revit remarks allow a save. So
the add-in could run the save itself at that point, on the fragment's behalf: `SAVE_DOCUMENT` version 3
asks for it - the document it works in, by its own file for a Save, or a full `.rvt` or `.rfa` path the
modeller typed for a Save As of a model that has none - and the add-in does it once the group has closed,
then puts what happened at the end of the reply. Still behind the Publish switch; never a file name chosen
by Heron; `SaveAsOptions.OverwriteExistingFile` left at Revit's own default, false, unless the modeller
said to replace the file. The group itself does not change: one request, one `TransactionGroup`, one undo
entry, and the save is not in it - a save is not a model change and cannot be undone. Where Revit asks
the modeller Keep or Discard ([D-126](../decisions/D-126.md)), the box is answered inside the group, before
that point (line 1009), so a save there could only ever follow a Keep.

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
   one ([`heron_write.py`](../../mcp/server/heron_write.py) line 485), and only a match on it or on the file
   path lets a new title through (line 504, *"a save can rename it; same document"*). But that id,
   `Document.CreationGUID`, arrived at Revit 2024 and is null on 2020 to 2023 and on every family document
   ([`RevitOperations.cs`](../../revit/Heron.Revit.Addin/RevitOperations.cs) line 705, measured
   2026-10-06), and a model with no file has no path. So on Revit 2020 to 2023, and for a family on any
   release, a Save As of a model or family that was never saved leaves the pin holding the title alone; the reply comes back under the new title, and `revit_change` answers *"Heron will not change a
   model you did not point it at ... Nothing has been sent to Revit"* (`heron_mcp_server.py` line 1878, in the words of `check` in `heron_write.py`)
   about a model it has just saved. (Since 2026-11-11 that reply says what the add-in reported doing
   in place of *"Nothing has been sent to Revit"* -
   [row 5b-1135](../fragment-issues/section-5b-rows-401-425.md); the refusal itself is unchanged.)
   The next call is then aimed at the old title (line 1296) and refused
   until the modeller says *use this model*. Letting the pin follow a rename is a change to that guard.

**The shapes the owner chooses between:**

1. **Leave Heron as it is.** The methods ask the modeller to save before a big build, and
   `REPORT_OPEN_DOCUMENTS` - version 3 today, and DRAFT - says which open model or family has NEVER BEEN SAVED. The cheapest; it
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

**Work:** built 2026-11-16, Shape 2 - compiled for Revit 2020 to 2027 and tested in a cloud container,
**NOT RUN IN REVIT.** `SAVE_DOCUMENT` version 3 asks for a save of the document Heron works in through a new
ambient need, `saveWhenKept` - null for its own file, or the full path the modeller typed for a Save As of
a model never saved, and `replaceFile` (off unless typed) to replace a file already there - and refuses
before asking: a model that has a file is saved to it, never under a new name; a relative path, a folder
that does not exist, the wrong extension, a file open in Revit or a workshared model for a Save As; the
central model opened directly for a Save. The add-in saves in the same request, after the group's block
has closed and the reply is built ([`RevitFragmentSave.cs`](../../revit/Heron.Revit.Addin/RevitFragmentSave.cs),
called at the end of `RevitFragment.Run`), and the reply's `save` block says what it did. **The preview
point, asked again in plain words the same day** - should a preview of *"save my model"* really save? - was
put to the owner as *"Yes, preview saves - only when you asked to save; the save tool can be tested now on a
scratch model; the reply says a save cannot be undone"*, and **Ajmal PS answered *"okkey"* (2026-11-16)** -
[D-171](../decisions/D-171.md) point 6, whose note under D-55 in the decision log already says a save asked
for with a run is not rolled back (D-144: the old record keeps every word). The three points, as built:
**(1)** the act after the group is the save alone, of what the fragment asked, behind the Publish switch read
again as it saves, and said on the reply; **(2)** a group Revit reports kept is followed by the save, and so
is a preview - a run nobody was asked about, rolled back, whose rollback Revit reports held: its change is
undone, its save is not, and the reply opens *"THE CHANGE WAS ROLLED BACK ... AND THE SAVE WAS DONE"* and
says a save cannot be undone. A **Discard** or a closed box saves nothing - D-171 point 5, the box is answered
before the save, so a save follows only a Keep - and nor does a run whose rollback did not hold or one Revit
did not report kept. A preview with no save asked for saves nothing and carries no `save` block. **(3)** the
reply names the model as the change was made and the `save` block its new title and file, and the chat's pin
follows a Save As only when that block says this run did it, of the pinned model
([`heron_write.py`](../../mcp/server/heron_write.py) `DocumentPin.follow_save_as`, `after_save`; `revit_change`
calls it once the pin has passed the reply). The proving client prints the `save` block under the verdict and
writes it into a `validate` phase's record ([`heron_bridge_client.py`](../../mcp/client/heron_bridge_client.py)
`save_sentence`), so a preview's ROLLED BACK is never read as nothing written to disk. Every save that was
done says it cannot be undone; a workshared local copy says nothing went to central; nothing here calls
Synchronize with Central (Article 10). No new switch: the Publish switch the question names. A chat's
`dry_run` of a Publish card is still refused before anything is sent (`heron_tools.dry_run_refusal`), so the
preview that saves is the proving client's. **Tests:** `tests/test_save_after_group.py` (new),
`tests/test_document_pin.py` (a Save As section) and `tests/test_fragment_ambient.py` (the new need) - 40, 6
and 3 checks failed on the code before the first build; the preview rule's 18 checks failed on the first
build; all pass now. **The card is DRAFT** - its C# moved, so version 2's proof is kept as its record and no
longer stands (D-30), and a kept change from it is refused (D-117); `family-creation` step 11 f and
`mass-family-creation` step 7 say the modeller saves by hand until it is PROVEN again. **Owed:
[NEEDS-CHECKING Group EN](../needs-checking/group-en.md) EN6 to EN10, on a real Revit - EN6, EN8, EN9 and
EN10 run TODAY as previews on a scratch model**, with the Publish switch on, and are the proof's route; EN7's
Discard and the chat halves of EN6 and EN8 wait for the card to be PROVEN. **Fixed in code the same day, under
D-171 point 5 ([row 5b-1824](../fragment-issues/row-5b-1824.md)), not run in Revit:** version 2's save of ANOTHER
open document, which was made before the box, is the add-in's after the group too (`saveOtherWhenKept`,
`SaveOtherNow`) - a Discard saves and closes nothing - and Revit's Keep or Discard box tells a publish whose
save waits for Keep that nothing is saved yet, not that it is "done already"; Group EN EN11 to EN13 are owed.
**Not decided here:** the methods' *"Heron cannot save the model it works in"* lines, true while the card is
DRAFT, which change the day it is PROVEN.

---

### ✅ Q-70 — On a PC whose Claude app keeps its own copy of `%APPDATA%\Heron`, which `heron.config` should Heron believe, and should it say when the two differ? *(found 2026-10-17)* → **decided 2026-11-15 on the owner's delegation**

**Answer: Revit reports its own switches and settings file; the connection check states them as Revit has them and warns when the chat's copy differs - 2026-11-15, decided by Claude on Ajmal PS's delegation of 2026-11-15 - *"the remaining thing you can answer it"* - and not read back to him; his word overrules it at any time, and no decision record is written until he reads it back.** **Why:** Revit already decides every change from its own file, so it is the one to believe; the chat should say what Revit holds. **Work:** built 2026-11-16, **not yet run in Revit** - the add-in's `info` ([`BridgeServer.cs`](../../revit/Heron.Bridge/BridgeServer.cs)) also says `settingsFile`, `settingsFileFound` and `switches` (Changes, Admin, Publish, from one load on the gate's own keys) beside `askToKeep`, only read and optional, so an older client ignores them; `revit_health` says under each Revit *Switches in this Revit: ... - read just now from <path>* and one more line, naming the file(s) and each switch that differs, when the chat's copy differs by name, by a file found on one side only, or by a switch (Ask to Keep included) - one name is not taken as one file, after [NP6](../needs-checking/group-np.md); its write gate is Changes as the Revits that said have it, the chat's copy only when none did; an older add-in keeps today's answer plus *this Revit's Heron does not report its settings*; and `heron_why`, still asking Revit nothing (PROPOSALS F1), judges by what a Revit said where `revit_health` found its file differs, and says how long ago ([`heron_health.py`](../../mcp/server/heron_health.py) `settings_lines`, [`heron_bridge_client.py`](../../mcp/client/heron_bridge_client.py) `settings_state`, [`heron_runtime.py`](../../mcp/server/heron_runtime.py) `in_revit_words`). Held by `tests/test_config_and_health.py`, `tests/test_mcp_serves.py` (four stand-in add-ins: same file, differing value, differing path, older add-in; and `heron_why`), `tests/test_runtime_snapshot.py` and `tests/test_bridge_roundtrip.py` (the real bridge, switches flipped under a running host, Linux only) - each seen to fail on the code as it stood; `check-compile` and `check-api-surface` pass for every release. **Dropped under [D-166](../decisions/D-166.md) on 2026-11-16, merging `origin/main` into this branch:** D-166, accepted by the owner the same day, holds Changes, Admin and Publish in Revit's memory, off at every start and in no file, so every part of the above that read or compared them as a file's went: the add-in's `switches` object read from `heron.config` (`info` carries D-166's `changes`, `admin`, `publish` and the `...Locked` fields from memory instead, and still `settingsFile` and `settingsFileFound`); `settings_state` reading the three (it reads the file and whether it was there, and `switches_state` reads the switches); the chat's copy of Changes, Admin and Publish compared with Revit's, and `heron_config.switches()` holding them (it holds Ask to Keep alone, the one switch D-166 left in the file); the line under every Revit, *Switches in this Revit ... read just now from <path>, the file this Revit decides every change by* (D-166's switches line says them, from memory, and no line names a file unless the chat's copy is not Revit's); the write gate falling back to the chat's copy when no Revit said (no answer then, as D-166 has it); `heron_why` reading this PC's file unless `revit_health` had found a difference (it goes by what the Revit last said always, and with no word judges the switches off, the way every Revit starts, and says it assumed so); and the refusals naming `write.enabled`, `admin.enabled` and `publish.enabled` (they name the switch and Heron's Settings). What is kept: Revit names its settings file, `revit_health` says in one line when the chat's copy is not that file - by name, by a file found on one side only, or by Ask to Keep - and that line names both files, which Group OB's OB4 may read as a failure ([row 5b-1836](../fragment-issues/row-5b-1836.md), the owner's to say); the write gate is Changes as the Revits that said have it, naming them, and `heron_why` judges by what Revit said and says when. The tests that pinned the dropped half were changed, each with a comment naming D-166, and [Group NP](../needs-checking/group-np.md) NP9 to NP13 rewritten before any had run. **Not built:** problem 2's waiting times - the add-in reads them once at start-up, so `info` cannot say what Revit waits, and the server still derives its own from the chat's copy (shape 2's second step, still owed); problem 3 is unchanged; `heron_diagnose`'s write gate asks no Revit - since D-166 it no longer reads the chat's copy, and says no Revit said ([row 5b-1744](../fragment-issues/row-5b-1744.md)). **Owed:** the run on the laptop, [Group NP](../needs-checking/group-np.md) NP9 to NP14.

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

Each side writes only its own. A switch - **Changes, Admin or Publish**, on the ribbon that day and in Heron's Settings window since [D-150](../decisions/D-150.md) - pressed in one kind of
Revit is not seen by the other kind, nor by the server when the Revit is one the owner started. Measured
both ways that day: `fragments.warmUp = false` written from the chat's shell landed in the copy and the real
file kept `true`, and the Changes switch pressed in a chat-started Revit rewrote the copy and left the real
file as it was.

**What is still safe, read from the code - NOT RUN on the laptop as a case of its own.** The add-in decides
every change from ITS OWN file: the permission gate loads `heron.config` fresh on every call
([`RevitOperations.cs`](../../revit/Heron.Revit.Addin/RevitOperations.cs) line 372), and its refusal names the
file it read ([`HeronPermissions.Explain`](../../platform/Heron.Core/HeronPermissions.cs)). So the server's
copy can never open a gate Revit has closed. What it can do is **say the wrong thing**:

1. **`revit_health`'s write-gate line** is built from the server's file
   ([`heron_mcp_server.py`](../../mcp/server/heron_mcp_server.py) `revit_health`,
   [`heron_health.py`](../../mcp/server/heron_health.py) line 182). It can say *"closed - Heron cannot change the
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

> **2026-11-12 - one switch is now said by Revit itself, and this question is still open.** A separate
> task - a chat could not tell a modeller beforehand whether Revit would ask Keep or Discard - added
> `askToKeep` to the add-in's `info` answer, and `revit_health` prints it under each connected Revit
> ([docs/04 §4](../04-heron-mcp.md), [Group NP](../needs-checking/group-np.md), not yet run in Revit). That
> is shape 2's mechanism, used for **one** switch that `revit_health` did not report at all. **Both
> sentences above still hold:** the add-in decides every change from its own file, and the write-gate line
> is still the server's copy. It does **not** report Changes, Admin or Publish, name the file either side
> read, compare the two, or move a waiting time - each of those is this question's, and Ajmal PS's. The
> session that added it had not been asked this question; if he chooses shape 1 or 3, whether the new line
> stays is part of that choice. [NP6](../needs-checking/group-np.md) is the run on the laptop.

---

### ✅ Q-71 — Should a stop reach a Revit whose lease nobody holds, from any client id? *(found 2026-10-18)* → **decided 2026-11-15 on the owner's delegation**

**Answer: A Stop must name the exact Revit session it is for - 2026-11-15, decided by Claude on Ajmal PS's delegation of 2026-11-15 - *"the remaining thing you can answer it"* - and not read back to him; his word overrules it at any time, and no decision record is written until he reads it back.** **Why:** A stop becomes a choice, never a broadcast that silently switches off another chat's idle Revit until it restarts. **Work:** built and compiled 2026-11-16, shape 2, NOT RUN IN REVIT. The stop is a new operation, `stop_session`, carrying the session as the add-in names itself - release and process id, `2024/63712`, from that Revit's own discovery record. `AnswerStop` in [`RevitDispatcher.cs`](../../revit/Heron.Revit.Addin/RevitDispatcher.cs) stops only when that is its own session, compared exactly, and before `HeronStop` is touched refuses - recorded, nothing stopped, naming the Revit - a `stop_session` naming none (`stop_unnamed`) or another (`stop_wrong_session`), and the old `stop`, which named none; that one is still answered before the queue, so its refusal never waits behind a job. A NEW NAME, so an add-in from before the change - which stops on `stop` from anyone - answers `unknown_op`, or queues the name behind a running job and is busy, and is never stopped by it; the client says that Revit's Heron is older and nothing was stopped (row 5b-1604's way with `undo_guarded`). [`heron_bridge_client.py`](../../mcp/client/heron_bridge_client.py) `stop` sends nothing without `--session`, one Revit connected or two, and lists them. No lease change, and no MCP tool sends a stop (none did). Compiled for Revit 2020 to 2027 (`tools/check-compile.py`), every Revit member it calls found in every release (`tools/check-api-surface.py`). **Tests, seen to fail first:** [`tests/test_bridge_stop.py`](../../tests/test_bridge_stop.py) - 26 of its 54 checks failed on the code as it stood and none crashed (both names answered before the queue, the session read and compared, every refusal before the stop, the wire request, an older add-in's `unknown_op` and busy answer never read as a stop, the codes declared and classified), all pass now; [`tests/test_tool_registry.py`](../../tests/test_tool_registry.py) - 2 failed, `stop_session` not answered before the queue nor declared Read. Three of its old checks changed because they held what this answer ended: one Revit and no `--session` was stopped, the request sent was `stop`, and the lease checks sent a stop with no `--session`. **NEEDS REAL REVIT:** the run with two Revits, [Group EZ](../needs-checking/group-ez.md) EZ7 to EZ9 - a stop for one leaves the other running and answering, the add-in refuses a stop that does not name it, and an add-in from before is not stopped. **Not built:** the caller's client id in the stop's record (shape 2's *Leaves*) - not part of the answer.

**What happened.** On 2026-10-18, running [Group EZ](../needs-checking/group-ez.md) EZ3's negative on the
owner's laptop, `python mcp/client/heron_bridge_client.py stop` was sent with a stranger's
`HERON_CLIENT_ID` while two Revits were connected ([row 5b-1094](../fragment-issues/section-5b-rows-401-425.md)).
Revit 2024, whose lease the run held, refused it - *"in use by another chat"* - as [D-129](../decisions/D-129.md)
says it should. Revit 2020, another chat's and idle, took it: *"Heron is stopped. Nothing was running."*
Heron there refused every change until that Revit was restarted ([D-130](../decisions/D-130.md)).

**Half of it is fixed, in the client only.** `stop` now goes to one Revit: `--session <pid>`, or the only
one connected; with two connected and no `--session` it sends nothing and lists them
([`tests/test_bridge_stop.py`](../../tests/test_bridge_stop.py)). **The add-in is not changed**, and this
question is the other half.

**The rule as it stands, read from the code.** `BridgeServer` claims the lease for every request but `ping`,
`info` and `release`, before the add-in sees it ([`BridgeServer.cs`](../../revit/Heron.Bridge/BridgeServer.cs),
`HeronLease.Claim`). A Revit nobody holds - no request for five minutes, or handed back with `release` -
grants the lease to any client id, so `stop` from anyone reaches `AnswerStop` in
[`RevitDispatcher.cs`](../../revit/Heron.Revit.Addin/RevitDispatcher.cs). D-129's *"only the chat holding a
Revit can stop it"* holds only while a chat holds it. **And the trail cannot say who:** the `stop` record
carries the Revit's own session (release and process id), whether a job was running and whether the stop
changed anything - not the caller's client id.

**Why it matters more than one test run.** "Idle" is not "nobody's": a chat bound to a Revit loses its lease
after five quiet minutes and stays bound. Anything that sends `stop` - a second terminal today, the MCP
server when it learns to (D-129: *"nothing in the MCP server sends it yet"*) - can stop that chat's Revit
until it is restarted, with no sign in that chat of who did it.

**The shapes the owner chooses between:**

1. **Leave the add-in as it is.** Any client id stops a Revit nobody holds, as a deliberate act - the client
   now asks for `--session` when there is a choice. **Cost:** nothing. **Leaves:** a stop with any id and the
   right `--session` still stops another chat's quiet Revit, and the MCP server, when it sends `stop`, has no
   rule but its own.
2. **A stop must name the Revit it is for - recommended.** The add-in refuses a `stop` whose request does not
   carry its own session (release and process id), whoever sends it; the client sends the one it chose.
   **Cost:** a C# change in `RevitDispatcher.AnswerStop` and one argument from the client, rebuilt and
   deployed for 2020 to 2027, and a Revit run with two connected ([EZ6](../needs-checking/group-ez.md)). No
   lease change. An older client's stop - the one that went to every Revit - is then refused, which is the
   point. **Leaves:** a stranger who names the session still stops an idle Revit. That stays a choice someone
   made rather than a broadcast, and adding the caller's client id to the `stop` record would make it seen.
3. **A Revit nobody holds takes a stop only from the chat that held it last.** The lease remembers its last
   holder after it lapses, and `stop` from any other id is refused `session_in_use` as a held Revit refuses
   it. **Cost:** a change to `HeronLease` in `Heron.Core` and to `BridgeServer` - the dispatcher today cannot
   tell a lease this request has just claimed from one it renewed - rebuilt for every release, and a run.
   **Leaves:** a second terminal can stop that Revit only with the chat's own id, which D-129 already asks
   for; a person who wants Heron stopped in a Revit no chat has used since it started can still do it, and
   one that a chat used and left cannot be stopped by anyone else until it restarts.

What none of the three changes: a held Revit refuses a stop from another chat, a stop lasts until Revit
restarts (D-130), and nothing over the bridge resumes.

**Not measured:** how often a bound chat's lease lapses in a working day, and whether any other caller
sends `stop` today - a grep of `mcp/server/` finds no request that sends it.

**Ajmal PS decides.** Until he does, nothing Heron does at runtime changes in the add-in: a Revit nobody
holds takes a stop from any client id. The client sends it to one Revit only.

---

### ✅ Q-72 — Part 4 §44 names three approval modes - Automatic, Ask and Require Approval. Which Heron operations sit in each, and what does "Ask" do that "Require Approval" does not? *(found 2026-10-18 checking the specification for D-139)* → **decided 2026-11-15 on the owner's delegation**

**Answer: The three words mean what is built: Automatic is reading, selecting and opening views; Ask is the Keep or Discard box; Require Approval is the Changes, Publish and Admin switches - 2026-11-15, decided by Claude on Ajmal PS's delegation of 2026-11-15 - *"the remaining thing you can answer it"* - and not read back to him; his word overrules it at any time, and no decision record is written until he reads it back.** **Why:** It matches how Heron works today and adds no new prompt to a job. **Work:** done 2026-11-15, in words: a dated note under [Part 4 §44](../00d-additional-requirements.md) gives the three modes this meaning and points here. The table keeps its words until a decision records the answer ([D-139](../decisions/D-139.md) point 5).

**What was found.** On 2026-10-18 every section of the four parts of the specification was checked against
what is built. [D-139](../decisions/D-139.md) item 4 says a choice the owner has not made is not corrected
in the parts: it is put to him as a question. This is one of them.

[Part 4 §44](../00d-additional-requirements.md) gives Heron three modes - **Automatic** for a safe
operation, **Ask** for an important one, **Require Approval** for a high-risk one - and stops there. It does
not say which operations are *important* and which are *high-risk*, or what *Ask* does that *Require
Approval* does not. It is one more way the parts describe how much a change needs before it happens:

| Where | What it says |
|---|---|
| [Part 1 §54](../00-master-specification.md) | Risk levels from `READ` to `ADMIN` - built, as `HeronRisk` in [`HeronPermissions.cs`](../../platform/Heron.Core/HeronPermissions.cs) |
| Part 1 §53 | A list of high-risk operations - deleting elements and purge among them - that *"should require appropriate permission levels"* |
| [Part 4 §47](../00d-additional-requirements.md) and [D-14](../decisions/D-14.md) | Knowledge levels: Level 3 may change the model with an accepted preview, Level 4 unattended. D-14 is still Proposed |
| Part 4 §44 | Automatic, Ask, Require Approval |

**What is built**, as the dated note under §44 reads it - a reading, not a decision:

- **Automatic** is `READ`, `ANALYZE` and `EXECUTE` - reading, selecting, the camera, opening a view - which
  need no switch.
- **Ask** is Revit's Keep or Discard box, which asks only while *Ask to Keep* is on. It is off by default
  ([D-138](../decisions/D-138.md)).
- **Require Approval** is a switch - on the ribbon then, in Heron's Settings window since
  [D-150](../decisions/D-150.md): Changes for `MODIFY`, and Publish or Admin on top of it
  ([D-19](../decisions/D-19.md), [D-106](../decisions/D-106.md)). These are standing switches the owner
  turns on once, not a yes for each operation.

**Not this question.** The status words - `DRAFT`, `PROVEN`, `PRODUCTION` and the other ladders, and
whether `PROVEN` comes before `PRODUCTION` - are [Q-34](from-master-specification-part-2.md) and D-14,
which close when the owner has seen them on a screen ([NEEDS-CHECKING R1b](../needs-checking/group-r.md)).
Whether a delete, a purge or another [Article 7](../../HERON_CONSTITUTION.md) operation must stop and ask
each time is [FRAGMENT-ISSUES row 5b-161](../fragment-issues/section-5b-rows-151-175.md), which waits on the
owner too.

**The shapes the owner chooses between:**

1. **The reading above becomes the meaning - recommended.** A decision says Part 4 §44's three modes are the
   risk levels and the switches as built, and §44 is corrected under D-139 to say so. **Cost:** a decision
   and a correction; no code. **Leaves:** *Ask* happens only when the owner has turned Ask to Keep on, and
   Article 7's yes for one operation is still 5b-161's question.
2. **Require Approval is a yes for each high-risk operation, not a standing switch.** Every change at that
   level stops and asks in Revit, whatever the switches say. **Cost:** a question in the add-in before the
   change, rebuilt for 2020 to 2027 and run in Revit; and it goes the other way from the owner's choice on
   2026-10-18 not to be asked for every change of a job (D-138).
3. **Leave the three words as intent, with no meaning given.** **Cost:** nothing. **Leaves:** a reader of
   §44 cannot tell what Heron does, and the next person to build an approval step chooses for him.

**Why the first.** It gives the part's words the meaning the owner has already chosen piece by piece - the
switches and their defaults, the box and its default - and changes nothing that runs.

**Ajmal PS decides.** Until he does, nothing changes: §44 keeps its words, and the note under it says the
choice is open.

---

### ✅ Q-73 — Constitution Article 13 says one confirmation never covers a later action, and D-138's Keep box can keep the rest of a job after one click. Should Article 13 name that as its exception? *(found 2026-10-18 checking the specification for D-139)* → **answered 2026-11-16 by the owner**

**Answer: Shape 1 - Article 13 names the job grant, D-138's third link, as its one recorded exception: one chat, one model, until ten quiet minutes pass - 2026-11-16, the owner's answer, read back: Ajmal PS was shown this question with Claude's suggestion and answered in his own words, *"go with you suggetions"*.** Recorded as [D-172](../decisions/D-172.md). **Why:** He chose the grant, Article 9 already names it, and an Article changes by a decision in its own words - so one sentence makes the two Articles agree. **Work:** done 2026-11-16 - [Article 13](../../HERON_CONSTITUTION.md) carries the sentence and the Constitution's Amendment note names it. No code changed.

**What was found**, checking the specification for [D-139](../decisions/D-139.md), whose item 4 names this
as a choice left open.

[Article 13](../../HERON_CONSTITUTION.md), *Do not bypass security*, binding since
[D-43](../decisions/D-43.md):

> Request operations through the permission layer. Never call around it, never cache a permission
> decision, never treat one confirmation as covering a later action.

[D-138](../decisions/D-138.md), accepted 2026-10-18, gives the Keep or Discard box a third link, *"Keep, and
keep the rest of this job without asking"*. One click keeps that change and lets the same chat's next
changes to the same model be kept without the box, until ten minutes pass with none, another chat or model
asks, the switch is turned off, or the model or Revit closes. The grant is held in memory by
[`HeronKeepGrant.cs`](../../revit/Heron.Revit.Addin/HeronKeepGrant.cs).

**Where the two differ, exactly:**

| Article 13's words | Beside D-138 |
|---|---|
| *"never treat one confirmation as covering a later action"* | **Differs.** One click on the third link covers that chat's later changes to that model. That is the grant's whole purpose |
| *"never cache a permission decision"* | **Differs, if the Keep answer counts as a permission decision.** The grant is the modeller's answer, held in memory and read at each later change. The switches are not cached: the add-in loads `heron.config` again on every call above the read-only level ([`RevitOperations.cs`](../../revit/Heron.Revit.Addin/RevitOperations.cs), `HeronConfig.Load()`), and the grant lets nothing through that Changes, Publish or Admin refuses (D-138, *What this does NOT change*) |
| *"Request operations through the permission layer. Never call around it"* | **Agrees.** Every change kept under the grant still goes through the gate |

**What was amended and what was not.** D-138 changed [Article 9](../../HERON_CONSTITUTION.md) - its text now
describes the grant as the second half of its one exception - and names Articles 7 and 10 as standing. It
does not name Article 13, and Article 13's text is unchanged. The Constitution's own *Amendment* section
says an Article is changed by a decision and is *"never weakened silently"*.
[Golden Rule 9](../14-golden-rules.md), which Article 13 comes from, asks for high-risk actions to be gated
in code at the boundary, and the grant is: it lives in the add-in, and only a click in Revit gives it.

Not engaged: [Article 15](../../HERON_CONSTITUTION.md)'s *"Consent is never remembered between actions"* is
about sending knowledge out - community submissions, pushes, pull requests and releases - not about
changes to a model.

**The shapes the owner chooses between:**

1. **Article 13 names the grant as its one recorded exception - recommended.** A decision adds one sentence,
   as Articles 9 and 11 gained theirs: one confirmation covers no later action, except the Keep or Discard
   box's third link (D-138) - one chat, one model, until ten quiet minutes; never a switch, never a
   publish, never a DRAFT's one run, and never Article 7's yes. **Cost:** a decision and one sentence in the
   Constitution; no code.
2. **A reading, not an exception.** A decision says Article 13 is about the permission layer's switches,
   and the Keep box is Article 9's preview, so the grant does not touch Article 13. **Cost:** a decision
   only. **Leaves:** Article 13's words still read wider than that, and someone reading it alone still sees
   the grant breaking it.
3. **Article 13 stands as written, and the third link goes.** **Cost:** a C# change, rebuilt for 2020 to
   2027; and while Ask to Keep is on, every change of a job asks again - the *"OK, OK, OK, OK"* the owner
   asked to be rid of.

**Why the first.** The owner chose the grant (D-138), the Constitution says an Article changes by decision
and in its own words, and Article 9 already names the grant - so naming it in Article 13 costs one sentence
and makes the two Articles agree.

**Not checked:** whether any other accepted decision lets one answer cover later actions. This question
looked at D-138 only.

**Ajmal PS decides.** Until he does, nothing Heron does at runtime changes: the grant works as D-138 says,
and Article 13's words stand beside it unreconciled.

---

### ✅ Q-74 — Which knowledge scopes does Heron have - is COMMUNITY one, is TEMPORARY one, and is CORE the same as GLOBAL? *(found 2026-10-18 checking the specification for D-139)* → **decided 2026-11-15 on the owner's delegation**

**Answer: The code's list of stores - global, company, project, user, temporary, experimental; Core means global, and community knowledge is a labelled source, not a store - 2026-11-15, decided by Claude on Ajmal PS's delegation of 2026-11-15 - *"the remaining thing you can answer it"* - and not read back to him; his word overrules it at any time, and no decision record is written until he reads it back.** **Why:** It is what the code and its tests already keep apart; there is no community knowledge to store. **Work:** done 2026-11-15, as notes: a dated note under each list that pointed here - [Part 1](../00-master-specification.md) §15, §33 and §72, [Part 2](../00b-master-specification-agent-os.md) §20 and [Part 3](../00c-master-handover-baseline.md) §24, §45 and §79 - says this answer. The lists keep their words until a decision records it ([D-139](../decisions/D-139.md) point 5).

**What was found**, checking the specification for [D-139](../decisions/D-139.md), whose item 4 names this
as a choice left open. A *scope* is one of the separate stores Heron keeps knowledge in, so that one
project's knowledge never leaks into another's ([Golden Rule 5](../14-golden-rules.md),
[Article 16](../../HERON_CONSTITUTION.md)). The parts name different lists:

| Where | Scopes named |
|---|---|
| [Part 1 §6](../00-master-specification.md) | personal memory, project memory, company knowledge, community knowledge |
| Part 1 §15 | CORE, COMPANY, PROJECT, USER, COMMUNITY, TEMPORARY, EXPERIMENTAL |
| Part 1 §33 | Global, Company, Project, User, Temporary, Experimental |
| [Part 3 §24](../00c-master-handover-baseline.md) | GLOBAL, COMPANY, PROJECT, USER, COMMUNITY, EXPERIMENTAL, TEMPORARY |
| Part 3 §45 | Global, Company, Project, User, Temporary, Experimental |
| Part 3 §79, the organisation chart | Global, Company, Project, User, Experimental |
| The code: `SCOPES` in [`heron_scope.py`](../../brain/heron_scope.py) | global, company, project, user, temporary, experimental |

**What is decided already, and what is not.** [D-23](../decisions/D-23.md) keeps one SQLite file per scope
and does not name them. [D-41](../decisions/D-41.md) says Heron is single-user now and company knowledge is
a git repository, so there is no community store. [D-14](../decisions/D-14.md), still Proposed, puts
COMMUNITY on its *Source* axis - where a thing came from - not on a list of where it is kept. Nothing picks
one list. That the code holds one does not make the pick (D-139 item 4).

**The shapes the owner chooses between:**

1. **The code's list, and COMMUNITY is a source, not a scope - recommended.** A decision names global,
   company, project, user, temporary and experimental; Part 1 §15's CORE is global; knowledge from the
   community, when there is any, goes into the store where it is used and carries COMMUNITY as where it came
   from. **Cost:** a decision and corrections under D-139; no code. **Leaves:** the *source* half rests on
   D-14, which is still Proposed.
2. **COMMUNITY is a scope of its own.** **Cost:** another store in `heron_scope.py`, rules for what may read
   it, and tests - for knowledge that D-41 says has nowhere to come from yet.
3. **TEMPORARY is not a scope** - Part 1 §6 and Part 3 §79 have none: a task's short-term context is kept
   with the task, not in a store. **Cost:** a change to `heron_scope.py` and its tests.

**Why the first.** It is what runs, it matches both memory tables (Part 1 §33 and Part 3 §45) word for word,
and it keeps *where it came from* off the list of *where it is kept* - the same split D-14 makes for status
words - so community knowledge never becomes a store anyone can open sideways.

**Ajmal PS decides.** Until he does, nothing changes: the code keeps its list, and the parts keep theirs
with a note.

---

### ✅ Q-75 — Which ladder of background priorities is Heron's? *(found 2026-10-18 checking the specification for D-139)* → **decided 2026-11-15 on the owner's delegation**

**Answer: Part 2's seven levels, as coded - 2026-11-15, decided by Claude on Ajmal PS's delegation of 2026-11-15 - *"the remaining thing you can answer it"* - and not read back to him; his word overrules it at any time, and no decision record is written until he reads it back.** **Why:** It is the order the code and its tests already use, and every list agrees the modeller's own request goes first. **Work:** done 2026-11-15, as notes: a dated note under each ladder - [Part 1](../00-master-specification.md) §49, [Part 2](../00b-master-specification-agent-os.md) §50, [Part 3](../00c-master-handover-baseline.md) §52 and [Part 4](../00d-additional-requirements.md) §22 - says this answer, with Part 4's *RAG indexing* and Part 3's *System Health* placed at `P3`. The ladders keep their words until a decision records it ([D-139](../decisions/D-139.md) point 5).

**What was found**, checking the specification for [D-139](../decisions/D-139.md), whose item 4 names this
as a choice left open. A priority says which background job waits for which - learning, indexing,
cleaning up - while the modeller's own request always goes first. The parts give four ladders:

| Where | Ladder |
|---|---|
| [Part 1 §49](../00-master-specification.md) | P0 User task · P1 Required validation · P2 Required maintenance · P3 Knowledge improvement · P4 Optimization · P5 Cleanup |
| [Part 2 §50](../00b-master-specification-agent-os.md) | P0 User interaction · P1 Required execution · P2 Required validation · P3 Maintenance · P4 Learning · P5 Optimization · P6 Cleanup |
| [Part 3 §52](../00c-master-handover-baseline.md) | P0 User Task · P1 Required Validation · P2 System Health · P3 Knowledge Maintenance · P4 Learning · P5 Optimization · P6 Cleanup |
| [Part 4 §22](../00d-additional-requirements.md) | Priority 1 User request · 2 Required validation · 3 System maintenance · 4 RAG indexing · 5 Learning |
| The code: `PRIORITIES` in [`heron_queue.py`](../../brain/heron_queue.py) | Part 2's, from [21 §12](../21-resilience-and-operations.md) |

**What all four agree on:** the user's work comes first, and background work waits for it.
[`heron_scheduler.py`](../../brain/heron_scheduler.py) holds that as *nothing from P2 to P6 runs during a
user task*. **Where they differ:** whether *required execution* is a level of its own, where validation
sits, whether *system health* and *RAG indexing* are named, and how many levels there are. No decision picks
one; 21 §12 follows Part 2, and the code follows 21 §12. Neither module is reached by any tool the server
serves (the note under Part 4 §22), so the choice changes nothing that runs today.

**The shapes the owner chooses between:**

1. **Part 2's ladder, as the code has it - recommended.** A decision names it, and the other three parts are
   corrected to it under D-139: Part 4's *RAG indexing* is maintenance (P3), and Part 3's *System Health*
   is maintenance too. **Cost:** a decision and corrections; no code.
2. **Part 4's five.** **Cost:** `heron_queue.py`, `heron_scheduler.py`, their tests and 21 §12 changed, for
   a ladder with no place for required execution or cleanup.
3. **Leave the four as intent** until something serves the queue. **Cost:** nothing now; whoever wires it
   up picks for him.

**Why the first.** Every level in the other three has a place in it, it is the one the design document and
the code already use, and the one rule all four share holds in it unchanged.

**Ajmal PS decides.** Until he does, nothing changes: the code keeps Part 2's ladder, and the parts keep
theirs with a note.

---

### ✅ Q-76 — Should the specification have one picture of how Heron is organised, and is a name listed in two departments one agent or two? *(found 2026-10-18 checking the specification for D-139)* → **decided 2026-11-15 on the owner's delegation**

**Answer: The as-built picture - Heron's four parts and the register's departments; the four charts stay as intent with notes, and a repeated name doing two jobs becomes two names - 2026-11-15, decided by Claude on Ajmal PS's delegation of 2026-11-15 - *"the remaining thing you can answer it"* - and not read back to him; his word overrules it at any time, and no decision record is written until he reads it back.** **Why:** There is no orchestrator inside Heron - Claude is that - so a chart that draws one misleads a reader. **Work:** done 2026-11-15. A dated note under each chart and list that pointed here - [Part 1](../00-master-specification.md) §41 and §73, [Part 2](../00b-master-specification-agent-os.md) §3, [Part 3](../00c-master-handover-baseline.md) §8 and §79, and [Part 4](../00d-additional-requirements.md) §45, §48 and the mandatory list - says this answer; each keeps its words. Two rows of [the register](../28-agent-registry.md) are renamed, each saying so: `HERON-DEV-REL-018` is the *Release Packaging Agent* and `HERON-RAG-DUP-012` the *Ingest Duplicate Detection Agent*, so the repeated-name command above now prints nothing.

**What was found**, checking the specification for [D-139](../decisions/D-139.md). Three things about how
the parts are put together, none of them a fact that can be corrected without the owner choosing:

1. **Four pictures of the whole.** [Part 1 §73](../00-master-specification.md) puts a CEO / Orchestrator
   over Engineering, Knowledge and Platform, then Communication. [Part 3 §79](../00c-master-handover-baseline.md)
   is a flat list of departments under an Executive / Orchestrator. [Part 4 §45](../00d-additional-requirements.md)
   puts a CEO and an Orchestrator over HR, Operations and Knowledge, and Part 4 §48 puts a Kernel over
   Orchestration, Knowledge and Platform, then validation, the tools, the add-in and Revit. **As built:** no
   CEO and no orchestrator inside Heron - the host does that ([D-01](../decisions/D-01.md),
   [D-80](../decisions/D-80.md)); Heron's four parts, `revit/`, `mcp/`, `brain/` and `platform/`
   ([README](../../README.md)); and the departments of [the agent register](../28-agent-registry.md),
   printed by `python tools/agent-count.py`.
2. **One name in two departments.** Part 1 lists a *Release Agent* in §36, the GitHub Department, and again
   in §41, Development Agents. The register carries both: `HERON-GIT-REL-007`, *"Tags and publishes
   releases"*, claimed by `brain/heron_tag.py`, and `HERON-DEV-REL-018`, *"Packages and releases"*, claimed
   by `tools/check-package.py`. It also carries two *Duplicate Detection Agents*, `HERON-RAG-DUP-012` in
   Knowledge & RAG and `HERON-IMP-DUP-007` in Import & Migration. Every name that repeats:
   ``grep -oE '^\| `HERON-[A-Z0-9-]+` \| [^|]+' docs/28-agent-registry.md | cut -d'|' -f3 | sed 's/\*//g;s/^ *//;s/ *$//' | sort | uniq -d``.
3. **A mandatory list with no sections behind much of it.** Part 4's list under *Strongest
   recommendations - treated as mandatory* names components Part 4 gives no section of its own - among them
   the Agent Registry, Agent Factory / HR, RAG Librarian, the fragment and skill registries, Separated
   Memory, Vector DB management, the Audit Trail, the Automatic Update Manager, the GitHub Agent and the
   Agent Performance System. Most are described in Parts 1 to 3 instead. The dated table under the list maps
   each one to its module.

**The shapes the owner chooses between:**

1. **The as-built picture is the one, and the parts' charts stay as intent - recommended.** A decision says
   Heron is organised as its four parts and the register's departments; the four charts stay, each with its
   note, because D-01 already gave their top to the host. A name in two departments stays two rows only
   where the jobs differ, and then each gets a name of its own in the register - *Release Packaging Agent*
   and *Release Publishing Agent*, say. The mandatory list is read as a checklist across all four parts,
   mapped by the table under it, and no new sections are written. **Cost:** a decision, renamed rows in the
   register, and notes under D-139.
2. **One of the four charts is chosen, and the other three are corrected to it.** **Cost:** whichever is
   chosen still puts a CEO or an orchestrator inside Heron, which D-01 gives to the host, so it needs
   correcting too.
3. **Leave it.** **Cost:** nothing. **Leaves:** a reader meets four pictures, and two agents with one name.

**Why the first.** The charts are what the owner asked for at four moments, and D-139 itself refused to
rewrite the parts because the intent would be lost; the register already holds the departments that run, so
one decision makes the picture single without touching his words.

**Ajmal PS decides.** Until he does, nothing changes: the charts keep their notes, and both rows of each
repeated name stay in the register.

---

### ✅ Q-77 — D-91 says Heron's release is signed, and v0.1.0 went out unsigned. Should a decision say releases are unsigned until signing is built, and what makes signing due? *(found 2026-10-18 checking the specification for D-139)* → **answered 2026-11-16 by the owner**

**Answer: Shape 1 - a decision amends D-91: releases are versioned and checksummed now and say plainly they are unsigned, and are signed before Heron is given to anyone outside - 2026-11-16, the owner's answer, read back: Ajmal PS was shown this question with Claude's suggestion and answered in his own words, *"go with you suggetions"*.** Recorded as [D-173](../decisions/D-173.md); D-91's index entry and record say it was superseded in part. **Why:** It writes down what the owner had already chosen twice - to publish v0.1.0, and to leave signing last - keeps *signed* as the bar, and moves the only record of the choice out of a work note that will be deleted. **Work:** done 2026-11-16, as documents; no code changes - `release.yml` already writes *"Not signed."* on every release. **Owed** before Heron is given to anyone outside: the signing itself, Stage 8.

**What was found**, checking the specification for [D-139](../decisions/D-139.md), whose context cites
[D-91](../decisions/D-91.md) as the rule a modeller's PC installs from.

- **D-91**, Accepted, is titled *"Product files come from a signed, versioned GitHub release"*, and decides
  *"Files come from a signed GitHub release, named by version."*
- **[`release.yml`](../../.github/workflows/release.yml)** builds the release and creates it with
  `gh release create --draft`. Its header says *"NOTHING HERE IS SIGNED. Stage 8 is code signing, it is
  deliberately last"*, and the notes it writes on every release say ***"Not signed."***
- **The one published release, v0.1.0**, was published by the owner on 2026-10-08, knowing it was
  unsigned: *"Publish v0.1.0 now"*. [NEEDS-CHECKING Group AC](../needs-checking/group-ac.md) records it as
  *"the owner's exception, made knowingly"*, and that two written rules still say signed:
  [07 §1a](../07-installation-and-update.md), and [`heron_tag.py`](../../brain/heron_tag.py), which refuses an
  unsigned release. `release.yml` does not run `heron_tag.py`, so that refusal has never met a real release.
- **What is checked instead:** the installer verifies every download against the release's `checksums.txt`
  before anything reaches Revit's Addins folder
  ([`ReleaseAssets.cs`](../../platform/Heron.Installer/ReleaseAssets.cs)) - D-91's own *"verified before it
  is used"*.

**Where the choice is written today:** that signing waits until Heron is finished, *"on the owner's word"*
([07 §3](../07-installation-and-update.md)), and is parked at his request in the plugin-extension work
note's Stage 8 - a folder deleted at the end of its life. No decision records it, and D-91 is unamended.
Related, not the same: signing the deploy *script* was considered and refused for a certificate's yearly
cost ([Group AA](../needs-checking/group-aa.md), AA9). That was the script, not the release.

**The shapes the owner chooses between:**

1. **A decision amends D-91 - recommended.** Releases are versioned and checksummed now, and signed at
   Stage 8; every unsigned release says so in its notes, as `release.yml`'s already do; and `heron_tag.py`'s
   refusal stays as the bar Stage 8 must meet. The decision names what makes signing due - for example,
   before Heron is offered to anyone outside the owner's office, or before the README's *please do not
   install this yet* is taken out. **Cost:** a decision; no code.
2. **Sign now.** **Cost:** a code-signing certificate - a yearly cost and a renewal, Group AA's reason -
   and a signing step in `release.yml` whose key lives in the credential store
   ([Article 17](../../HERON_CONSTITUTION.md)).
3. **No more published releases until one is signed.** **Cost:** a new teammate installs from a clone and a
   build instead of the installer's download.

**Why the first.** It says in a decision what the owner has already chosen twice - to publish v0.1.0, and to
leave signing last - keeps *signed* as the target rather than dropping it, and moves the only record of the
choice out of a work note that will be deleted.

**Ajmal PS decides.** Until he does, nothing changes: `release.yml` still makes an unsigned draft, and only
the owner publishes it.

---

### ✅ Q-78 — When a modeller says *"select all ducts"*, does Heron select every duct in the model, or only the ducts visible in the open view? *(found 2026-10-18 checking the specification for D-139)* → **decided 2026-11-15 on the owner's delegation**

**Answer: The whole model, unless the modeller says 'in this view' - 2026-11-15, decided by Claude on Ajmal PS's delegation of 2026-11-15 - *"the remaining thing you can answer it"* - and not read back to him; his word overrules it at any time, and no decision record is written until he reads it back.** **Why:** It is what was proved on two releases, the answer already says how many and in which model, and it matches Revit's own In Entire Project; the view stays one word away. **Work:** done 2026-11-15, as a note: a dated note under [Part 4 §27](../00d-additional-requirements.md) says this answer; *Expected* keeps its words until a decision records it ([D-139](../decisions/D-139.md) point 5).

**What was found**, checking the specification for [D-139](../decisions/D-139.md), whose item 4 leaves a choice
no accepted decision has made as written.

- **[Part 4 §27](../00d-additional-requirements.md)** expects *"all visible ducts"* to be selected.
- **What runs:** the PROVEN fragment [`filter-elements-by-category`](../../brain/fragments/filter-elements-by-category/fragment.yaml)
  takes every element of the category in the model, optionally on one level - not only what the open view shows.
  [27 Step 4](../27-build-order.md) proved it that way, and its answer names the count and the model:
  *"Selected 126 ducts in Tower-A.rvt"*.
- **What is decided already:** [D-59](../decisions/D-59.md) settles reading into linked models, not views.
  [Golden Rule 20](../14-golden-rules.md) settles which model, not which part of it. Nothing settles *view or
  model*. That the code picked one does not make the pick (D-139 item 4).

**The shapes the owner chooses between:**

1. **The whole model by default, the open view when the modeller says so - recommended.** *"Select all ducts"*
   selects every duct in the bound model, as Revit's *Select All Instances > In Entire Project* does, and the
   answer says how many and that some may be outside the open view; *"in this view"* or *"visible"* narrows it
   to the view. **Cost:** a decision, and a correction to Part 4 §27 under D-139; the view half needs a check
   that a view-scoped selection capability exists before it is promised.
2. **The open view by default**, as Part 4 §27 says and as *Select All Instances > Visible in View* does.
   **Cost:** the PROVEN fragment's behaviour changes, so it needs a new proof; and a modeller in a plan with
   ducts hidden would see a smaller count than the model holds.
3. **Ask every time.** **Cost:** a question on the commonest request, which [D-33](../decisions/D-33.md) keeps for
   inputs Heron cannot know.

**Why the first.** It is what was proved on two releases, the answer already says how many and in which model so
nothing is hidden, and it matches what a modeller gets from Revit's own *In Entire Project*; the view stays one
word away.

**Ajmal PS decides.** Until he does, nothing changes: the fragment keeps selecting the whole model, and Part 4 §27
keeps its words with a note.

---

### ✅ Q-79 — DISCLAIMER says Heron *"reports compliance with a standard"*, and the chat is told never to call a model compliant. Should the DISCLAIMER say what the chat says? *(found 2026-10-18 in the second documentation pass)* → **answered 2026-11-16 by the owner**

**Answer: Shape 1 - the DISCLAIMER takes the chat's words: Heron reports whether the checked values are met, with the source document, and certifies no compliance - 2026-11-16, the owner's answer, read back: Ajmal PS was shown this question with Claude's suggestion and answered in his own words, *"go with you suggetions"*.** Recorded as [D-169](../decisions/D-169.md). **Why:** It is the stricter wording, what the chat already says, and what an engineer or an authority expects of a tool: values met, not compliance granted. **Work:** done 2026-11-16 - [DISCLAIMER](../../DISCLAIMER.md) *On standards checking* reworded, with a line saying what it said before.

**What was found.**

- **[`DISCLAIMER.md`](../../DISCLAIMER.md), *On standards checking*:** *"Where Heron reports compliance with a standard -
  ISO 19650, QCS, Ashghal requirements, a company standard - it reports against the source documents it has been
  given, and cites them. **It does not certify compliance.**"*
- **[`host.chat.yaml`](../../brain/instructions/host.chat.yaml)**, the instruction every chat reads: *"A check that
  passes means the model meets the values that were checked, and that is what you say. Never call a model, a
  system or a design compliant - whether it complies is the engineer's or the authority's decision."* It came in
  with the earlier-brain plan's C2 (#307).
- **[D-36](../decisions/D-36.md)** makes the DISCLAIMER load-bearing and says it *"must change with the code rather
  than after it"*. No decision says which wording is Heron's.

The two agree that Heron certifies nothing. They differ in one word: the DISCLAIMER says Heron *reports
compliance*; the chat never says *compliant*.

**The shapes the owner chooses between:**

1. **The DISCLAIMER takes the chat's words - recommended.** *"Where Heron checks a model against a standard ... it
   reports which checked values the model meets and which it does not, against the source documents it has been
   given, and cites them. It does not certify compliance."* **Cost:** one paragraph of a legal page, which only the
   owner changes.
2. **Leave both.** **Cost:** the page a company reads before using Heron says a word the product is told never to say.

**Why the first.** It is the stricter of the two, it is what the chat already does, and it is the wording an engineer
or an authority expects from a tool: values met, not compliance granted.

**Ajmal PS decides.** Until he does, nothing changes.

---

### ✅ Q-80 — When the boundary guard's script cannot be found at all, should the edit be refused, or let through unchecked? *(found 2026-10-19 hardening the hook commands after PR #675)* → **decided 2026-11-15 on the owner's delegation**

**Answer: Keep refusing, and say why in plain words - 2026-11-15, decided by Claude on Ajmal PS's delegation of 2026-11-15 - *"the remaining thing you can answer it"* - and not read back to him; his word overrules it at any time, and no decision record is written until he reads it back.** **Why:** The same safety as today, with a message that says the guard's script was not found and to open the session in the Heron folder, in place of a cryptic error. **Work:** done 2026-11-15 - `.claude/settings.json`'s guard command, when the script is not found, writes *"Heron's boundary guard refused this edit: its script ... was not found here, and the guard fails closed. Open the session in the Heron folder."* and exits 2; [`tests/test_heron_session.py`](../../tests/test_heron_session.py) §5 holds it, and failed on the old command, which printed python's *can't open file*.

**What was found.** Every hook in [`.claude/settings.json`](../../.claude/settings.json) looks for its script
under `$CLAUDE_PROJECT_DIR` and, failing that, in the repository the hook runs in (`git rev-parse
--show-toplevel`). **When neither has it** - that variable names another folder **and** the hook runs
outside any git repository - there is no script to run.

- **The three advice hooks** (`session_line.py`, `local_only.py`, `main_moved.py`) now say nothing and
  exit 0 there. Each is documented as *never a gate*, so silence is their right answer. Until 2026-10-19
  python exited 2 on the missing path, and 2 is *"block"* to the host: every prompt was refused.
- **The guard** (`.claude/skills/heron-guard/bin/heron_guard.py`) is **left as it
  was**: python exits 2, and the host blocks the Write, Edit or MultiEdit. That is the guard's own trap 3 -
  *"a boundary that fails open is not a boundary"* - holding one level up, but it is also every edit in that
  session refused, with only python's *can't open file* to say why.
- [`tests/test_heron_session.py`](../../tests/test_heron_session.py) §5 pins both: the advice hooks silent,
  the guard not exiting 0.

**The shapes the owner chooses between:**

1. **Keep refusing - recommended.** The guard is deny-tier and fails closed everywhere else, so with no
   script it refuses. A session in that state is already broken (no folder, no repository), and
   `HERON_GUARD=off` cannot help because the script is what reads it. **Cost:** none in code; a line in
   the [`heron-session`](../../.claude/skills/heron-session/SKILL.md) skill saying what the refusal means.
2. **Refuse, but say why.** The command prints a deny of its own (*"the boundary guard's script was not
   found - open the session in the Heron folder"*) instead of python's error. **Cost:** a longer command in
   `settings.json`, held by the same suite.
3. **Let it through.** The command exits 0 like the advice hooks. **Cost:** an edit that puts the Revit
   vendor namespace outside `revit/` goes unchecked until `check-structure.py` runs.

**Why the first.** It is what the guard already promises, the state it fires in is rare and broken anyway,
and `check-structure.py` is not a reason to open the earlier gate.

**Ajmal PS decides.** Until he does, nothing changes: the guard command is as PR #675 left it.

---

### ✅ Q-81 — When `$CLAUDE_PROJECT_DIR` names ANOTHER Heron checkout, its copy of the guard checks the wrong repository. Is that accepted? *(found 2026-10-19 hardening the hook commands after PR #675)* → **decided 2026-11-15 on the owner's delegation**

**Answer: Each hook checks the copy it runs in first, and uses the project-folder setting only outside one - 2026-11-15, decided by Claude on Ajmal PS's delegation of 2026-11-15 - *"the remaining thing you can answer it"* - and not read back to him; his word overrules it at any time, and no decision record is written until he reads it back.** **Why:** The guard exists to check the file being edited; checking another copy lets edits through unchecked. **Work:** done 2026-11-15 - all four commands in `.claude/settings.json` look in the repository the hook runs in first and take `$CLAUDE_PROJECT_DIR` only when that has no script; [`tests/test_heron_session.py`](../../tests/test_heron_session.py) §5 names a fake second checkout in the variable and asks the guard and the prompt hook, and both checks FAILED on the old order. The `heron-guard` and `heron-session` skills say the new order.

**What was found.** A hook command runs the script under `$CLAUDE_PROJECT_DIR` whenever that folder has
it, and falls back to the repository the hook runs in only when it does not. So if the variable names a
**second** Heron checkout - an older clone, or a worktree - that copy's script runs, not this one's.

- **The guard works out its root from its own file**
  (`.claude/skills/heron-guard/bin/heron_guard.py` `main()`), so it takes **the
  other checkout** as the repository. An edit to a file in the real one is *outside* that root, and the
  guard lets it through **unchecked** - silently, which is the failure trap 1 warns of.
- **The advice hooks** run the other copy's code, which may be older. They only advise, so the cost is a
  stale or missing line.
- **Pre-existing**, not caused by PR #675 or by the 2026-10-19 change: the commands preferred
  `$CLAUDE_PROJECT_DIR` before either. Recorded, not fixed, because the fix changes which folder every hook
  trusts.

**The shapes the owner chooses between:**

1. **Prefer the repository the hook runs in - recommended.** Each command asks `git rev-parse
   --show-toplevel` first and falls back to `$CLAUDE_PROJECT_DIR` only outside a repository. **Cost:** four
   commands and the §5 checks in `tests/test_heron_session.py`; a session whose working folder is not Heron
   but whose variable is would then fall back as today.
2. **Leave it, and say so.** A line in the [`heron-guard`](../../.claude/skills/heron-guard/SKILL.md) skill
   that two checkouts on one PC can check each other's edits. **Cost:** none in code; the hole stays.

**Why the first.** The guard exists to check the file being edited, and the repository the hook runs in is
the one that file is in; the variable is the host's guess.

**Ajmal PS decides.** Until he does, nothing changes.

---

### ✅ Q-82 — Revit's API cannot make a label or change what one shows. Should Heron ever drive the Edit Label dialog itself, by UI automation, or call 2027's internal label members? *(found 2026-10-21 building labels for a pipe tag)* → **decided 2026-11-15 on the owner's delegation**

**Answer: Neither - the first label of each kind is made by hand once in a seed family and copied from there - 2026-11-15, decided by Claude on Ajmal PS's delegation of 2026-11-15 - *"the remaining thing you can answer it"* - and not read back to him; his word overrules it at any time, and no decision record is written until he reads it back.** **Why:** Driving the dialog or calling 2027's hidden functions would bypass the one undo entry and the Keep or Discard box, or depend on something unsupported. **Work:** nothing to build.

**What was found.** Ajmal PS, building the pipe tag *TRG_Pipe_Tag* on Revit 2020, asked for labels end to end.
Read that day off all eight releases' shipped assemblies ([docs/43 §8.3a](../43-building-revit-families.md)):
no release has a public call that MAKES a label, and none can change which parameters a label shows, their
sample value, prefix, suffix, spaces, break or unit format. Revit 2027 has those calls -
`AnnotationLabel.Create`, `SetParameterFormatting`, every setter - and keeps them **internal** in every 2027
package, 2027.0.0 to 2027.3.0. Built instead, DRAFT: `REPORT_FAMILY_LABELS`, `SET_FAMILY_LABEL_LAYOUT` and the
seed route, `COPY_LABELS_FROM_FAMILY` ([Group IJ](../needs-checking/group-ij.md)). Two other routes were assessed
and **not built**, because each needs a decision first:

- **UI automation of Edit Label** - Heron opening the dialog and clicking and typing in it. A fragment runs
  inside Revit's API, where a modal dialog cannot be driven; `PostableCommand.Label` only STARTS the Label tool,
  after the API call has ended, and the modeller places the label. Anything else would be a program outside
  Revit moving the modeller's mouse and keys. It breaks [Golden Rule 16](../14-golden-rules.md) - the change is
  not one named `TransactionGroup` Heron can roll back, and Revit's own undo entry is not Heron's; it breaks
  Constitution Article 9 - there is no Keep or Discard box around it; on 2020 to 2026 nothing can read the
  result back to prove it worked; and it depends on the dialog's layout, language and screen scale, which change
  between releases and PCs. `HeronDialogWatch` declines any dialog met during a job, by design, so Heron would
  be fighting its own guard.
- **Calling 2027's internal members by reflection.** Possible in code, and not part of the API Autodesk gives an
  add-in: undocumented to it, free to change or vanish in any update, and nothing could prove it stays safe on a
  modeller's model.

**The shapes the owner chooses between:**

1. **Neither - recommended.** Labels are made by hand or copied from a seed family of the same category; Heron
   reads and lays them out. When a Revit release makes `AnnotationLabel.Create` public,
   `tools/api-surface --members AnnotationLabel` shows it and a `CREATE_FAMILY_LABEL` for that release is a small
   job. **Cost:** the first label of each kind is made by hand once, into the seed.
2. **UI automation, as a separate tool outside the fragment library.** **Cost:** a new kind of component with no
   rollback, no read-back on 2020 to 2026 and a dependency on screen layout; a decision on how Article 9 and
   Golden Rule 16 apply to it; per-release, per-language maintenance.
3. **Reflection into 2027's internal members, 2027 only.** **Cost:** an unsupported dependency on Autodesk's
   internals, proved on one update and re-proved on every update after.

**Why the first.** The seed route reaches every release with nothing Heron cannot undo or read back, and it is
the way modellers already reuse labels. The other two trade the safety rules for a shortcut on one step.

**Ajmal PS decides.** Until he does, nothing is built for either.

**2026-11-12 - the cards have moved since; the question has not.** `REPORT_FAMILY_LABELS` and
`SET_FAMILY_LABEL_LAYOUT` say `PROVEN` on their cards today. `COPY_LABELS_FROM_FAMILY` was merged into
`COPY_ELEMENTS_BETWEEN_DOCUMENTS` on 2026-11-01 and is `DEPRECATED`: the seed route is that card's `labels`
mode now, a `DRAFT` at version 4. Each card's `heron-status` line says what it is on the day it is read.
Neither route this question asks about is built.

---

### ✅ Q-85 — Revit's API cannot make a void cut a hosted family's template host, so the modeller cuts it by hand in the Family Editor. Should Heron do more than say so? *(found 2026-10-22 building a floor drain)* → **decided 2026-11-15 on the owner's delegation**

**Answer: Nothing more - Heron's method tells the modeller to cut by hand and lists the voids to cut - 2026-11-15, decided by Claude on Ajmal PS's delegation of 2026-11-15 - *"the remaining thing you can answer it"* - and not read back to him; his word overrules it at any time, and no decision record is written until he reads it back.** **Why:** The other routes either change how every hosted family is built or bypass Heron's safety rules. **Work:** nothing to build.

**What was found.** Ajmal PS, building a nested floor-drain family on Revit 2020 through Heron, found that the
two voids Heron made through the template's floor cut nothing - not the sample floor, and not one floor in the
project the family was loaded into, with *Cut with Voids When Loaded* off and then on. Revit's *Modify >
Geometry > Cut* in the Family Editor, the sample floor first and then each void, and a reload, cut every host
floor; finding that by hand cost him an hour. Read that day off all eight releases' own XML remarks
([row 5b-1324](../fragment-issues/section-5b-rows-401-425.md), [docs/43 §7](../43-building-revit-families.md)):
no public call makes that cut in an ordinary hosted family - `SolidSolidCutUtils` refuses the document and the
floor in its own exception text, `Document.CombineElements` takes forms only, `JoinGeometryUtils` is not for
family documents and joins rather than cuts, `InstanceVoidCutUtils` wants a loaded family in a project, and
`FamilyItemFactory.NewOpening` takes a wall or a ceiling. `CREATE_FAMILY_HOST_OPENING` stays as it is - its
wall and ceiling halves are PROVEN - and
[`hosted-family-creation`](../../brain/skills/hosted-family-creation.yaml) step 5 now tells the modeller to
cut by hand before loading.

**The shapes the owner chooses between:**

1. **Nothing more - recommended.** The method says to cut by hand, host first then void, before Load into
   Project; `REPORT_FAMILY_FORMS` already lists the voids to cut. When a release gives the Family Editor's
   Cut Geometry a public call, `tools/api-surface --members SolidSolidCutUtils` and a search of that release's
   XML for *"family document"* beside *"cut"* show it, and widening `CREATE_FAMILY_HOST_OPENING` to a floor
   and a roof is a small job. **Cost:** one Cut per void in every hosted family, by the modeller.
2. **A nested-void route, if Revit takes it.** Build the voids in a small Generic Model family with *Cut with
   Voids When Loaded* on, nest it in the hosted family, and call `InstanceVoidCutUtils.AddInstanceVoidCut` on
   the template's host and the nested copy. Whether the call accepts a family document at all - its remarks
   say *"Elements in a project"* - and whether that cut carries into the project's hosts on load, is NEEDS
   REAL REVIT: a day's proving on Revit 2020, then 2024 and 2027. **Cost:** every hosted family's hole becomes
   a nested family; a void that must follow a parameter needs that family's own labels and links; a proof on
   three releases before any modeller relies on it.
3. **UI automation of Cut Geometry.** Ruled out already for the label dialog (Q-82 above, the same reasons):
   no transaction group of Heron's, no Keep or Discard box, nothing read back, and a dependence on the screen.

**Why the first.** The hand step is one click per void and is how Revit's own help teaches it; the second
changes how every hosted family is built on the strength of a call documented for projects; the third trades
the safety rules for a shortcut.

**Ajmal PS decides.** Until he does, nothing is built; the method's hand step stands.

**2026-11-12 - the card named above has moved since; the question has not.** `CREATE_FAMILY_HOST_OPENING` was
merged into `CREATE_OPENING` on 2026-11-01 and is `DEPRECATED`; `CREATE_OPENING` carries its wall and
ceiling job and is `DRAFT` at version 4 until proved again, so *"its wall and ceiling halves are PROVEN"*
is the record of the retired card. The method's step 5 names `CREATE_OPENING`, and still tells the modeller
to cut a floor's or a roof's void by hand. Nothing in shapes 2 or 3 is built.

---

### ✅ Q-86 — A card that waits for the modeller's click is timed as a slow job, and a face cannot be clicked from a card at all. How should a card say it is waiting for a person? *(found 2026-11-11 writing `PICK_ON_SCREEN`)* → **decided 2026-11-15 on the owner's delegation**

**Answer: A pick tool says on its label that it waits for a person, and Heron's add-in holds its timers for it, as it does for the Keep or Discard box - 2026-11-15, decided by Claude on Ajmal PS's delegation of 2026-11-15 - *"the remaining thing you can answer it"* - and not read back to him; his word overrules it at any time, and no decision record is written until he reads it back.** **Why:** A click made after a minute then reaches its tool, and a slow series of clicks is not reported as a runaway job. **Work:** built 2026-11-16 in a cloud container, **not run in Revit**. *The label* is an ambient need, `waitsForModeller` (bool), declared in a card's `contract.needs` the way `noTransactionOpen` is ([D-125](../decisions/D-125.md)) and spelt the same in [`heron_fragment.py`](../../brain/heron_fragment.py)'s `AMBIENT`, `RevitFragment.WaitsForModellerNeed` and the bridge client's `WAITS_FOR_MODELLER_NEED`; [`PICK_ON_SCREEN`](../../brain/fragments/pick-on-screen/fragment.yaml), DRAFT, carries it - the only card whose code calls Revit's pick, so no PROVEN card was marked and no proof or signature moved (a proof's fingerprint covers `impl/` alone, and that is unchanged). *The add-in's hold*, the one the Keep or Discard box has: on the read and show paths a labelled card's script runs inside `HeronRunClock.Pause()`, and `RevitDispatcher.AwaitingClick` marks the job, so the dispatcher waits `revit.askTimeoutSeconds` more and the activity banner steps aside; if even that runs out the chat is told `awaiting_click` in words of its own, never Keep or Discard's; a run that can change the model is refused `waits_on_change_path` before anything runs. The lease is not held - nor is it for the box, which is [row 5b-1745](../fragment-issues/row-5b-1745.md). *The chat's side*: `heron_config.fragment_timeout(waiting=True)` adds the ask timeout, `revit_read` and `revit_show` wait by the label and tell a host that shows progress *"Waiting for you to click in Revit"*, and the bridge client's `fragment` command and the batch run wait as long. Both answers are in [`heron_failure.py`](../../mcp/server/heron_failure.py). **Tests:** `tests/test_person_wait.py` - 41 checks fail on the code as it stood, none crash, and all pass now; `tests/test_fragment_ambient.py` names the new constant; `tools/check-compile.py` (all eight releases) and `tools/check-api-surface.py` exit 0. **Owed:** the add-in rebuilt and redeployed - one built before refuses the card, the label unknown to it - then the run in Revit, [NEEDS-CHECKING Group NH](../needs-checking/group-nh.md) rows NH12 to NH14 (NH8 superseded). The comment in `PICK_ON_SCREEN`'s `impl/any/fragment.cs` still describes the wait as it stood; it is the card's next code change, and its one click per call stays until a card with a loop is written.

**What was found.** The owner wants to work by chat with very few clicks in Revit, and nothing in Heron let the
modeller click to answer the chat. `PICK_ON_SCREEN` was written for it, at DRAFT: Revit's own pick, one point
for each call, handed back in millimetres. Reading the add-in before writing it
([row 5b-1507](../fragment-issues/section-5b-rows-401-425.md) has each one with its evidence), nothing there
knows that a card can be waiting for a person:

- **The dispatcher gives a started job `revit.operationTimeoutSeconds`** - 60 by default - then tells the chat
  Revit is still running the request and stops listening. A click made after that reaches nobody. The longer wait
  it gives a Keep or Discard box ([D-119](../decisions/D-119.md), `revit.askTimeoutSeconds`) is asked for by the
  add-in's own code and cannot be asked for from a card.
- **The run clock ([D-129](../decisions/D-129.md)) would stop a slow multi-click** at the next pass of a card's
  loop and report a job that ran out of time - *"a loop that never ends is the usual cause"*, untrue of a slow
  modeller - with the points already clicked not handed back. So the card asks for one click a call and has no loop. D-129's item 5 wrote a
  hold for exactly this - *"a prompt pauses it"* - but the compile gate builds a card against Revit alone and
  cannot compile the call, and the hold does not lengthen the dispatcher's wait in any case.
- **The chat's side waits 180 seconds for a show run**, so a longer wait in the add-in alone would only move the
  loss: the chat would give up first.
- **An element or a face cannot be clicked from a card.** Revit's pick for one names a kind of pick from a
  namespace cards are not given, and no card takes a face as an input ([D-72](../decisions/D-72.md)).

So the card runs today only while the click lands inside that one wait - about a minute as installed - and
its own description tells the chat to ask again when it does not. It does not work round the wait.
What the chat is actually told when a click comes late is not yet seen in Revit
([NEEDS-CHECKING Group NH](../needs-checking/group-nh.md), NH8 and NH9).

**The shapes the owner chooses between, for the wait:**

1. **The card declares it, and the add-in does the waiting - recommended.** One line on the card's contract, the
   way a card already declares that it must run with no transaction open ([D-125](../decisions/D-125.md)). For a
   card that carries it, the add-in marks the job as waiting for the modeller and holds the run clock while the
   card runs; the chat's side adds the ask timeout for that card, as it does for a keep; and the refusal for a
   wait that still runs out gets words of its own - today's are about Keep or Discard. **Cost:** a change to the
   add-in, the card reader on both sides and the chat's wait, with their tests, and a rebuild and redeploy of the
   add-in. A card that declares it is not stopped by TIME if it loops for ever - a stop asked over the bridge
   still reaches it - so the line would be honoured on the read and show paths only, and refused on a run that
   can change the model.
2. **A public call a card makes round the pick itself.** The hold D-129 already wrote, widened to mark the job
   too. **Cost:** the compile gate has to be given the add-in's type, the first time a card names one - and the
   same change to the chat's wait.
3. **Nothing more.** The limit stays; the chat tells the modeller to be at Revit before it asks, and a slow
   modeller raises `revit.operationTimeoutSeconds`. **Cost:** that setting lengthens the wait for every slow job
   and the run clock's budget with it, so a card that loops for ever freezes Revit for longer too.

**And for an element or a face:** whether Revit's selection namespace joins the list a card is given - one line
in the add-in and one in the compile gate, kept level by `tests/test_fragment_imports.py`, and the same rebuild -
and what a clicked face is handed on AS, since no card takes one: its element, the point clicked on it and the
direction it faces are all things cards take today; Revit's own reference to the face is not.

**Why the first.** The card stays plain Revit code that the compile gate can read on every release; the wait is
decided in one place by the add-in, where the Keep or Discard box's wait already is; and a card cannot lengthen
its own wait without its contract saying so, where a reviewer sees it.

**And what "put it here" means.** The card first declared that sentence, and a review took it off: it moved the
sentence away from the cards that place and show, and the chat was then offered no placement card at all. Should
"put it here" mean *click, then place* - a job of two steps, which is a skill's to claim rather than one card's -
or stay with the placement cards, the point typed or clicked as a step the chat chooses?

**Ajmal PS decides.** Until he does, nothing in the add-in is changed, and `PICK_ON_SCREEN` stays DRAFT behind
the limit: one point a call, and "put it here" declared by no card.

---

### ✅ Q-87 — Bringing an edited spreadsheet back writes every cell with nothing shown first. Should Heron read the file itself before Revit does - and if so, how does a chat reach that, and should the export carry each element's UniqueId? *(found 2026-11-11 writing the round-trip method)* → **decided 2026-11-15 on the owner's delegation**

**Answer: Heron compares the file with the exported copy first, outside Revit, lists the changed cells with old and new values, and sends only those - 2026-11-15, decided by Claude on Ajmal PS's delegation of 2026-11-15 - *"the remaining thing you can answer it"* - and not read back to him; his word overrules it at any time, and no decision record is written until he reads it back.** **Why:** Nothing unchanged or rounded is written back over the model, and the import tool needs no new proof. **Work:** built 2026-11-15, shape (a)2 with (b)'s door 3 - no new MCP tool, and `heron_sheet_list` not widened: [`brain/heron_sheet_compare.py`](../../brain/heron_sheet_compare.py) is a command the modeller runs on their PC, named in the method. It reads the file as exported and the edited copy - a CSV or an `.xlsx`, through `heron_sheet_list`'s own reader (`read_grid`, the one gate both stand on now) - lists the changed cells with id, parameter, old value and new, the ids changed, doubled, added or missing and the headers renamed or added, keeps cells blanked on purpose apart, and writes ONLY the changed cells to a small CSV in the shape `SET_PARAMETER_VALUES_BY_ID` reads through `csvPath`, brought back with `blankCells=skip`. Neither card's YAML or C# was touched. Tested off Revit by [`tests/test_sheet_compare.py`](../../tests/test_sheet_compare.py), which failed first; the method [`parameters-csv-round-trip`](../../brain/skills/parameters-csv-round-trip.yaml) was rewritten at steps 3 to 6 for it. **NEEDS REAL REVIT:** the small file through the card, [Group NJ](../needs-checking/group-nj.md) NJ13. **Still the owner's:** (b)1 and (b)2 - a chat tool of its own, or `heron_sheet_list` widened - and (c), the UniqueId column; neither was built.

**What was found.** A modeller's weekly round trip - parameters out to Excel, edited, brought back - is two
cards Heron already has: `EXPORT_PARAMETERS_TO_CSV` writes the file and `SET_PARAMETER_VALUES_BY_ID` reads it
back (`csvPath`, since `IMPORT_PARAMETER_VALUES` was merged into it on 2026-11-01 on the owner's word *"make
sure there is no duplicate tools"*). The method
[`parameters-csv-round-trip`](../../brain/skills/parameters-csv-round-trip.yaml) puts them in order. Reading
both cards' C# for it found that the return is safe in one way and blunt in three. Safe: it checks the whole
file inside Revit before writing anything, and one bad row means nothing is written. Blunt:

- **It reads a CSV only.** An `.xlsx` must be saved as CSV first, and as *CSV UTF-8*, or a name with an accent
  or in Arabic may be written wrong ([row 5b-1528](../fragment-issues/section-5b-rows-401-425.md)).
- **It writes every cell that is not blank, and shows nothing first.** A value somebody changed in Revit after
  the export is put back to the file's old one with no notice, and a length the project shows rounded is
  written back rounded in a cell nobody edited ([row 5b-1525](../fragment-issues/section-5b-rows-401-425.md)).
  The only preview is Revit's Keep or Discard box.
- **A file the export just wrote can be refused whole** - for a type parameter, a read-only one, or a
  parameter some of the elements have not got
  ([row 5b-1526](../fragment-issues/section-5b-rows-401-425.md)). And the export writes no UniqueId column, so
  a file from another model whose ids happen to be in use here is not caught.

Today the method covers this by telling the modeller three things to do by hand: delete every column and row
they did not edit, save as CSV UTF-8, and bring the file back in the same sitting. None of it has run in Revit
([NEEDS-CHECKING Group NJ](../needs-checking/group-nj.md)).

**Three questions, in a modeller's words.**

**(a) Before anything is written, do you want Heron to show you what the file would change?** The shapes:

1. **Nothing more.** The method stands as written, with its three hand rules. **Cost:** a slip - a column left
   in, a file brought back a day late - writes old or rounded values over the model, and nothing says so.
2. **Heron reads the file first, outside Revit - recommended.** As it already reads a sheet list
   ([`heron_sheet_list.py`](../../brain/heron_sheet_list.py)): an `.xlsx` or a CSV, with the file as exported
   kept beside the edited one. It lists the cells that differ - how many, and a sample of them, old value and
   new - says which ids were changed, doubled or added and which headers were renamed, and writes a small CSV
   holding ONLY the changed cells to the work folder. That small file goes to the same card as today. Nothing
   in Revit is touched by the reading, and **no second tool writes to the model** - the card that changes it
   stays the one card. **Cost:** a reader and its tests, a door for the chat to reach it (question b), and one
   habit for the modeller - keep the exported file. A cell blanked on purpose needs the card's `clear`, so it
   is asked about apart. It still cannot see a value changed in Revit since the export in a cell the modeller
   ALSO edited; only question (c) reaches that.
3. **The card does it, inside Revit.** A new version of `SET_PARAMETER_VALUES_BY_ID` takes the exported file as
   well, writes only the cells that differ, checks each against what the export held, and gains a preview.
   **Cost:** the card is PROVEN, and a change to its code makes its proof out of date - every chat is then
   refused a kept change from it ([D-117](../decisions/D-117.md)), the Companion's table Apply and the rows by
   key included, until it has been proved again on a real model. And an `.xlsx` is still not read.

**(b) If Heron reads the file (shape 2), how should a chat ask for it?** Every tool's description is paid for
by every chat ([D-118](../decisions/D-118.md)), and off Windows Heron serves its library tools only
([D-132](../decisions/D-132.md)), so a new door is a decision and was not built:

1. **A tool of its own**, beside `heron_sheet_list`. Plain to find; one more description in every chat.
2. **One tool that reads a spreadsheet, told which job it is for** - `heron_sheet_list` widened. No new
   description; but it changes a tool whose own run in Revit is still owed
   ([Group MY](../needs-checking/group-my.md)), and one description then explains two jobs.
3. **No tool** - the method gives the modeller one command to run on their PC. Nothing added to any chat;
   the modeller leaves the chat to do it.

**(c) Should the exported file carry each element's UniqueId as a second column?** With it, a file from
another model is refused, and a cell changed in Revit since the export can be refused too, as the Companion's
table already does. **Cost:** `EXPORT_PARAMETERS_TO_CSV` is PROVEN; a new column is a new version, DRAFT until
it is proved again, and while it is DRAFT no chat can run the export. A modeller also sees a long column of
letters they must not touch.

**Why shape 2 for (a).** The hand rules are exactly the kind a tired modeller skips, and the damage is silent.
Shape 3 buys the same thing by taking a proved card out of use until the PC has run it again. Shape 2 leaves
both proved cards alone, keeps one writer, and puts the check where a mistake costs nothing - before Revit.
For (b) and (c) nothing is recommended: (b) is a budget only the owner sets, and (c) trades a proved card for
a safer file.

**Ajmal PS decides.** Until he does, nothing is built: the method's hand rules stand, and it says so at the
steps where they bite.

---

### ✅ Q-88 — Heron remembers a MODEL's last kept change, not which chat kept it. May one chat undo a change another chat made? *(found 2026-11-12 repairing `revit_undo_last`)* → **decided 2026-11-15 on the owner's delegation; read back and answered 2026-11-16 by the owner, [D-175](../decisions/D-175.md)**

**Read back 2026-11-16 - the owner, Ajmal PS, asked whether any chat may undo any chat's change: *"own only"*. Recorded as [D-175](../decisions/D-175.md), with the Activity pane's Undo as the modeller's own.** **Answer: No - only the chat that made a change may undo it - 2026-11-15, decided by Claude on Ajmal PS's delegation of 2026-11-15 - *"the remaining thing you can answer it"* - and not read back to him; his word overrules it at any time, and no decision record is written until he reads it back.** **Why:** 'Undo that' typed in the wrong window never takes back another chat's work; Ctrl+Z in Revit always still works. **Work:** the refusal was built and compiled on 2026-11-16, and has not run in Revit. The add-in's undo guard records which chat kept each change - the request's conversation, the one [D-141](../decisions/D-141.md) keys the model hold on, one per chat's own server - and `revit_undo_last` is refused, with nothing sent, when Heron's last change in that model was kept by another chat: *"Heron's last change in "Tower", 'rename-views' at 14:32, was made in another chat, not this one ... Nothing was sent to Revit and nothing was undone. Ctrl+Z in Revit still undoes it - or ask in the chat that made it."* It is asked in the one request that would post, never checked first and posted after. The operation is now `undo_own`, and its check `undo_own_check`: an add-in built before answers that it has no such operation, and the chat is told that Revit's Heron is older; the updated add-in refuses `undo_guarded` and `undo_check` from a chat opened before the change, posting nothing. A chat pinned to no model keeps its refusal. Compiled for Revit 2020 to 2027 (`tools/check-compile.py`), every Revit member it calls found in every release (`tools/check-api-surface.py`). `tests/test_undo_last.py` (with section 16 of `tests/Heron.UndoGuard.TestHost`), `tests/test_mcp_serves.py` (`exercise_undo`, against stand-in add-ins), `tests/test_outcome_line.py` and `tests/test_tool_registry.py` failed on the code as it stood and pass. **Owed:** the run with two chats, [Group NU](../needs-checking/group-nu.md) NU34 to NU40. Found while building it, recorded and not fixed: [row 5b-1741](../fragment-issues/row-5b-1741.md) - the Activity pane's Undo; fixed in code on 2026-11-16 on the owner's answer that the pane's button is the modeller's own click, like Ctrl+Z, for any chat's change - this answer's rule for chats is unchanged - and not run in Revit.

**What was found.** `revit_undo_last` ([D-157](../decisions/D-157.md)) sends Revit's own Undo for Heron's
last kept change in a model. The add-in's guard keeps one entry per model - the job, its undo entry, the
time - and nothing about which chat asked for it. D-157 item 3 refuses when *"the pinned model is not that
one"*; it says nothing of a chat that has no pinned model, or of two chats in one model. **Built, as the
cautious reading:** a chat that has worked in no model is refused before anything is sent - it has no
change of its own there ([row 5b-1604](../fragment-issues/row-5b-1604.md)). **Still possible:** a chat
that has only *read* a model, or was moved onto it with *use this model*, is pinned to it - and can undo
the last change another chat kept there, once that chat's hold on the model has lapsed
([D-141](../decisions/D-141.md)). Nothing is taken that is not Heron's: the guard's other rules stand.
The question is only whose Heron change.

**The question, in a modeller's words: when you ask a chat to undo what Heron did, may it undo something a
different chat did?**

1. **Yes - any chat working on the model.** It is your Revit and your undo list; a second chat is still
   you. **Cost:** *"undo that"* typed into the wrong chat window takes back the other chat's change, and
   the reply names the job, so you find out after.
2. **No - only the chat that kept it.** The add-in records which chat kept the change, and another chat
   is refused: *"another chat made Heron's last change here; press Ctrl+Z, or ask in that chat"*. **Cost:**
   after closing the chat that made a change, no new chat can undo it for you - Ctrl+Z still does, exactly
   as today. It needs the add-in rebuilt, one field in the guard and its tests.

**Shape 2 is recommended** - it keeps *"its own last change"* true of the chat as well as of Heron, and
what it costs is one keystroke. It was not built: D-157 does not say it.

**Ajmal PS decides.** Until he does: a chat pinned to no model is refused, and a chat pinned to the model
can undo Heron's last change there, whichever chat kept it. None of it has run in Revit
([NEEDS-CHECKING Group NU](../needs-checking/group-nu.md)).

---

### ✅ Q-89 — After a run that was rolled back - a dry run, a proof, a Discard - should what the chat had carried stay as it was, or become what that run left? *(found 2026-11-12 in review of the dry run)* → **decided 2026-11-15 on the owner's delegation**

**Answer: As built, and Heron also refuses any attempt to use results from a run that was taken back - 2026-11-15, decided by Claude on Ajmal PS's delegation of 2026-11-15 - *"the remaining thing you can answer it"* - and not read back to him; his word overrules it at any time, and no decision record is written until he reads it back.** **Why:** The smallest safe change: elements Revit no longer has can never be handed on to a real change. **Work:** built 2026-11-16, **not run in Revit**. A run that was taken back still replaces what is carried (as built), and nothing it left is handed on: **the server** refuses, before a session is bound, a call at `revit_change`, `revit_read` or `revit_show` whose `expect_from` names this chat's dry run or a change the modeller discarded, saying what was taken back and what to run instead (`heron_run_records.TakenBack`, from how each call ended - a lost or unconfirmed dry run included, a keep Revit counted nothing in left to hand on); **the add-in** writes on the chain how the run that left it was taken back - sent without `apply` (a dry run or a proof's trial), or not kept when the modeller was asked (`HeronBindingNote.TakenBack`) - refuses an expectation met by it as `chain_taken_back`, before the other four refusals, binds none of it by name, and starts the chain afresh at the next run that leaves something; that half covers a server older than the rule and a run the chat never heard end. Tests, each seen to fail on the code as it stood: `tests/test_outcome_line.py` section 3f (16 checks red before), `tests/test_run_records.py` section 9 (12), `tests/test_binding_note.py` through `Heron.BindingNote.TestHost` (18), `tests/test_chain_expectation.py` (12). Owed: the add-in rebuilt and Revit restarted on every release, then [Group NT](../needs-checking/group-nt.md) NT12 to NT15. Left open: what an arrangement step made inside a job that stops before its end ([row 5b-1743](../fragment-issues/row-5b-1743.md)).

**What was found.** A chat hands one call's result to the next with `expect_from`
([docs/36](../36-remembering-between-steps.md)): *find the ducts*, then *change them, expecting what the find
left*. The add-in remembers what each run left, and it does so after **every** run - one that was kept, and
one that was rolled back ([`RevitFragment.cs`](../../revit/Heron.Revit.Addin/RevitFragment.cs), `Remember`).
Until [D-155](../decisions/D-155.md) only the proving tools and a Discard rolled a run back. A chat can now
ask for a dry run, and then:

- **The dry run uses up what the find left.** The kept call sent straight after, with the same
  `expect_from`, is refused - what is carried was left by the dry run, not by the find.
- **What the dry run left can be named.** A call with `expect_from` naming the dry run's capability would be
  given its values - the ids of elements it made among them - from a change Revit no longer holds.

Read off the code, **not run in Revit** ([row 5b-1612](../fragment-issues/row-5b-1612.md),
[Group NT](../needs-checking/group-nt.md) NT11). The reply now says both, in words: run the find again
before the kept run, and take nothing from a dry run. Nothing refuses it.

**The question, in a modeller's words: after Heron tries a change and takes it back, should it still
remember what it had found before the try?**

1. **Yes - a run that was rolled back leaves what was carried alone.** The find's result is still there for
   the kept run, and nothing a rolled-back run made can be handed on. **Cost:** a change to the add-in -
   a rebuild and a Revit restart on every release - and it changes a Discard and every proof's rollback the
   same way. A proof arranged by hand as *run a change rolled back, then read what it left* would stop
   working; none was found in the proving tools, which arrange a chain inside one request or re-make it for
   each phase, but the PC's own hand-run records were not all read.
2. **No - as built.** Every run replaces what is carried, and the reply says to run the find again. **Cost:**
   one more call for the chat on every dry run that used `expect_from`, and the second hazard stays a
   sentence, not a refusal.
3. **As built, and the add-in refuses `expect_from` naming a run that was rolled back.** Closes the second
   hazard and leaves the first as in 2. **Cost:** the same rebuild as 1, and one more refusal to explain.

**Nothing is recommended.** Shape 1 is what a modeller would expect; whether any recorded proof leans on the
present behaviour is the one fact that decides it, and that needs the PC.

**Ajmal PS decides.** Until he does the add-in is as it was, and the dry run's reply says what it does.

---

### ✅ Q-90 — A QA check with 501 to 5,000 failing elements: should Select in Revit select them in batches, refuse, or stay as built - "the first 500 of N"? → **Every one, in one press** *(found 2026-11-12 in review of #866, answered 2026-11-13)*

**What was found.** [D-158](../decisions/D-158.md) sends a check's failing ids whole - up to the add-in's
`IdsSentPerList`, 5,000 - so that *Select in Revit* takes every one. But Select goes through the Model
Dashboard's route, which sends at most `DASHBOARD_SELECT_MOST`, 500, ids at once. Between 501 and 5,000 the
button reads *Select the first 500 of N* and selects part of what failed - honestly labelled, but against
D-158's own *"Select works on every one"* and the rule it keeps, that part of what failed is never selected
([row 5b-1621](../fragment-issues/row-5b-1621.md)). Read off the code and a stand-in, **not run in Revit**
([Group NV](../needs-checking/group-nv.md) NV14).

**The question, in a modeller's words: when a check finds 2,000 failing elements, what should the Select
button do?**

1. **Select all of them, sent to Revit in batches of 500.** **Cost:** several reads in a row under one
   press, and a selection that grows batch by batch; whether Revit's selection takes thousands at once at a
   usable speed is not known until it is run.
2. **Offer no Select past 500, and say why** - as past 5,000 today. **Cost:** the modeller copies the ids
   and uses Select by ID, or narrows the check.
3. **As built** - select the first 500 and say so. **Cost:** a partial Select, which the rule forbids.

**Nothing is recommended.** Shape 1 is what D-158 promised; its speed is the one fact that decides it, and
that needs the PC.

**ANSWERED 2026-11-13 by Ajmal PS** - *"fix them"*, to the offer *Select for the full list*: every failing id is sent in **one** press, up to the add-in's `IdsSentPerList`, and past that no Select at all - shape 1 without the batches, which `SET_SELECTION` cannot add to. *The first N of M* is gone ([row 5b-1621](../fragment-issues/row-5b-1621.md)). **Still open on the PC:** the speed - [Group NV](../needs-checking/group-nv.md) NV14. A press too slow to use reopens this question.

---

### ✅ Q-91 — On Revit 2020 to 2024 Heron cannot attach a wall to a roof. Should it shape the wall's top to the roof instead - by editing the wall's profile - and if so, on which releases? *(found 2026-11-15 from issue #852)* → **answered 2026-11-16 by the owner**

**Answer: Shape 2 - a new tool that edits each wall's profile so its top follows the roof above it, on Revit 2022 to 2024; on 2020 and 2021 it says plainly that it cannot; and it says the top is a sketch that does not follow later roof changes - 2026-11-16, the owner's answer, read back: Ajmal PS was shown this question with Claude's suggestion and answered in his own words, *"go with you suggetions"*.** **Why:** It closes the open gable triangles #852 describes on the releases whose API can edit a wall's profile, and saying plainly what the sketch does not do is what makes a top that does not follow the roof something the modeller can choose knowingly. **Work:** built 2026-11-16 at DRAFT - see the Work note at the end of this section - and its proof is owed (Group OG). No decision record: a new card starts at DRAFT and is proved like any other (Article 20, [D-30](../decisions/D-30.md)), and nothing Heron allows or refuses changes. Until it is built and proved, Heron says on 2020 to 2024 that this is done by hand in Revit.

**What happened.** On Villa 2, Revit 2024, 2026-11-11, the modeller asked *"attach all walls to the roof"*
after the main roof was rebuilt: 25 walls stopped at the 2440 plate and left open gable triangles under both
roofs. `ATTACH_WALLS` is declared for 2025 to 2027 only, so Heron offered nothing that does the job, and the
modeller ran Attach Top/Base by hand once per roof (issue #852; #853 item A1).

**What was checked, 2026-11-15, off the API reference files** (RevitAPI.xml and RevitAPIUI.xml in the
Nice3point reference packages 2020.2.90, 2021.1.100, 2022.1.80, 2023.1.90, 2024.0.0 and 2025.4.60), **not
run in Revit:**

- **No wall attachment before 2025.** The Wall attachment members and `AttachmentLocation` first appear in
  2025. The wall's *top is attached* parameter exists on every release and is read-only.
- **No posted command either.** Revit's Attach Top/Base is not in the list of commands an add-in may post,
  in 2020 or in 2024. *Join/Unjoin Roof* is in it, but it waits for the modeller to pick edges.
- **A wall's profile can be edited from 2022.** `Wall.CanHaveProfileSketch`, `CreateProfileSketch`,
  `RemoveProfileSketch` and `SketchId` are in 2022, 2023 and 2024, not in 2020 or 2021. On 2020 and 2021 the
  only way to a shaped wall is a NEW wall made from a profile, which loses the doors and windows the old one
  hosted.

So the job cannot be done on 2020 to 2024 as Revit's Attach does it - a wall that follows the roof when the
roof moves. `heron_lookup` now says so in its first line on those releases (#853 item C6). What is left is
whether a fallback is worth building ([row 5b-1672](../fragment-issues/row-5b-1672.md)).

**The question, in a modeller's words: on Revit 2024, when the walls must reach the roof, what should Heron
do?**

1. **Nothing - say it is done by hand,** as it now does. **Cost:** a modeller's Attach Top/Base per roof, and
   again after every roof rebuild.
2. **A new tool that edits each wall's profile so its top follows the roof above it, on 2022 to 2024**, and
   says plainly on 2020 and 2021 that it cannot. **Cost:** a new card at DRAFT and its proof; the wall's top
   is a sketch, so it does NOT follow the roof when the roof changes, and the modeller has to know that.
3. **As 2, and on 2020 and 2021 rebuild the wall from a profile.** **Cost:** everything hosted in the wall -
   doors, windows, sweeps - is deleted or must be re-hosted; a far bigger and riskier tool.

**Nothing is recommended.** Shape 2 closes the gable triangle #852 describes; whether a sketched top that does
not follow the roof is acceptable to the modeller is the one fact that decides it.

**Ajmal PS decides.** Until he does, Heron says on 2020 to 2024 that this is done by hand in Revit.

**Work:** built 2026-11-16, at **DRAFT** - [`SHAPE_WALL_TOPS_TO_ROOF`](../../brain/fragments/shape-wall-tops-to-roof/fragment.yaml)
(FRG-ELE-170), declared for Revit 2022, 2023 and 2024 only. For each wall handed in it finds the roof above that wall,
gives the wall a profile sketch and draws its top to the roof's underside in Revit's sketch edit mode, inside the one
named transaction group the add-in opens ([D-112](../DECISIONS.md)); the wall is edited, never rebuilt, and its doors
and windows are read back still in it. It leaves and names a wall with no roof over it, one Revit cannot give a
profile, one already attached, and one already shaped unless `redraw` is asked, and every answer that shapes a wall
says its top is a sketch that does NOT follow the roof when the roof changes. On 2020 and 2021 the lookup says plainly
that Heron cannot do this; from 2025 the same words reach `ATTACH_WALLS` (measured 2026-11-16 on a scratch store).
Compiles on 2022 to 2024; nothing has run in Revit. **Owed:** [Group OG](../needs-checking/group-og.md) on Revit 2024 -
the negative and the positive with its door kept (OG2), the sketch edit mode inside Heron's group (OG3), the release-2021
refusal in words (OG5) - then the modeller's one try. A lookup wording it exposed on 2025 on is
[row 5b-1823](../fragment-issues/row-5b-1823.md).

---
