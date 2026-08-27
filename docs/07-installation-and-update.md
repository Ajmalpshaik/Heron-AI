# 07 — Installation & Update

> Derived from [Master Specification](00-master-specification.md) §8, §9, §10, §11, §52.
> **[NOTE]** blocks are engineering commentary added during review.

---

## 1. Target install experience

1. User installs Claude Code.
2. User points Heron AI to a workspace folder.
3. Heron detects the environment.
4. Heron checks installed Revit versions.
5. Heron checks required dependencies.
6. Heron installs required packages.
7. Heron builds required components.
8. Heron deploys the Revit Add-in.
9. Heron configures MCP.
10. Heron registers Heron tools.
11. Heron Brain is initialised.
12. RAG database is initialised.
13. Required skills are installed.
14. Required fragments are indexed.
15. Health checks are performed.
16. User is told installation is complete.

The user never hand-edits configuration unless they deliberately enter Developer Mode.

## 1a. The user journey — what actually happens

**[NOTE]** Added 2026-08-27 from the owner's question: *someone sees this on GitHub, opens Claude Code,
adds a folder, pastes the link, and it installs itself — is that right?*

Mostly. Three corrections, one of which matters a great deal.

### The flow, corrected

```text
1.  Install Claude Code                          once, ever
2.  Install the Heron plugin                     one documented command
3.  Heron detects Revit 2020-2027                which versions are present
4.  "Close Revit so I can install the add-in"    mandatory - see below
5.  Heron deploys the add-in per version         per-user, no admin rights
6.  Start Revit -> ribbon -> Heron (toggle)      explicit, by design
7.  Open a project folder in Claude Code         THIS is the workspace
8.  Work
```

### Correction 1 — **do not "paste a link and let it run"**

> *"They copy the link, paste it, and run it."*

This is the one to change, and it is not a style preference.

*Point an AI at a URL and let it execute whatever it finds there* is the exact shape of a supply-chain
attack. It is also the pattern a contractor's IT department is trained to refuse — and Heron's users
work at contractors. For a tool that **writes to live client models**, it is the wrong first impression
and the wrong precedent.

It also contradicts Heron's own rules. [Golden Rule 19](14-golden-rules.md) says no text Heron reads
may raise its own permission level. A pasted URL whose contents become instructions is precisely that,
performed by the user's own hand.

**Instead: one documented command that fetches a signed release.** The user reads the command in the
README before running it, the command is the same for everyone, and what it downloads is a **versioned
release artefact** — not whatever the default branch happens to say today.

| Pattern | Verdict |
|---|---|
| *"Paste this URL and let the AI do what it says"* | **No.** Arbitrary instruction execution |
| *"Run this documented command, which installs release v0.1.0"* | **Yes.** Auditable, versioned, repeatable |

### Correction 2 — **the folder is the project workspace, not the installation**

> *"Everything will be downloaded into that folder… then it will search from this folder."*

Two different things are being conflated, and separating them is
[D-17](DECISIONS.md) and [06 §2](06-heron-platform.md):

| What | Where | Why |
|---|---|---|
| **Heron's code** | the plugin location | Replaced wholesale on update. The user never edits it |
| **Heron's brain** — skills, fragments, learned patterns, audit log | `%APPDATA%\Heron` | **The user's.** Must survive every update, uninstall and folder deletion |
| **Runtime state** — bridges, logs, caches | `%LOCALAPPDATA%\Heron` | Machine-local, rebuildable, safe to delete |
| **The project folder** | wherever the user opens | **Project scope** — this project's standards, decisions and notes |

If everything lived in the project folder, then deleting a finished project would destroy a year of
accumulated knowledge. That is the exact failure the product / data / derived split exists to prevent.

**What the folder is genuinely for:** BIM work *is* per project, so a per-project workspace is right.
It holds Project-scope knowledge ([10](10-memory-and-knowledge.md)) and keeps one client's decisions
out of another's. Opening a different folder should change **which project Heron is working on** — not
which Heron is running, and not what it has learned.

### Correction 3 — **download a release, do not build on the user's machine**

Already settled in section 3 below, but it bears repeating here because the pasted-link flow implies it:
building needs the .NET SDK, MSBuild and Revit API assemblies. A BIM modeller on a locked-down corporate
laptop has none of those and cannot install them without a ticket.

**CI builds; the user downloads.** The source stays available for anyone who wants to build it — that is
what open source means — but nobody has to.

### What the owner got right

- **Claude Code is the entry point.** That is [D-01](DECISIONS.md).
- **Installation is automatic after that one command.** Detect Revit versions, install, deploy, verify —
  the user is never asked to edit a config file.
- **"Close Revit first" is mandatory, not politeness.** Assemblies loaded into Revit cannot be unloaded,
  so an installer that copies over a running add-in silently fails or half-updates
  ([section 7](#7-note-update-rules)). `deploy-addin.ps1` already refuses when Revit is running.
- **Connecting stays explicit.** A Revit that was never connected is invisible to every chat
  ([00e](00e-field-notes-proven-bridge.md)). That is a safety property, not friction — nothing reaches a
  model the user did not offer up.

### Two audiences, two flows

| | **BIM user** | **Developer** |
|---|---|---|
| Gets Heron by | one install command, prebuilt release | `git clone`, build from source |
| Needs | Claude Code, Revit | Claude Code, Revit, .NET SDK |
| Add-in deployed by | the installer | `tools/deploy-addin.ps1` |
| Sees | *"Selected 126 ducts in Tower-A"* | agent chains, logs, the build pipeline |

**[NOTE — open]** The exact plugin install command must be verified against current Claude Code plugin
documentation before it goes in the README. Publishing an install command that does not work is worse
than publishing none. Tracked as [Q-38](OPEN-QUESTIONS.md).

---

## 2. Installation Agents

| Agent | Responsibility |
|---|---|
| Installation Orchestrator | Controls the whole workflow |
| Environment Detection | Windows, Revit versions, .NET, dependencies, workspace, storage, runtimes |
| Revit Installation | Add-in installation |
| Revit Deployment | Build and deploy the add-in |
| MCP Installation | Install and configure MCP |
| Dependency | Check and install dependencies |
| Configuration | Create required configuration |
| Brain Initialization | Initialise Heron Brain |
| RAG Initialization | Vector DB, indexes, embedding config, knowledge directories |
| Health Check | Verify everything works |

---

## 3. **[NOTE — significant]** Step 7 is the problem: "Heron builds required components"

Building the Revit add-in on the user's machine requires the .NET SDK, MSBuild, and the Revit API assemblies (`RevitAPI.dll`, `RevitAPIUI.dll`) for each target version. On a BIM modeller's locked-down corporate laptop, none of that is present and installing it may require IT approval.

It also contradicts the core promise: a user who must install a build toolchain has become a developer.

**Recommendation: do not build on the user's machine.**

| | Build on user machine | Ship prebuilt binaries |
|---|---|---|
| Toolchain required | .NET SDK, MSBuild, Revit SDK | None |
| Install time | Minutes, fragile | Seconds |
| Failure modes | Many, hard to diagnose remotely | Few |
| Fits the user promise | No | Yes |

Build in **CI** (GitHub Actions), produce a signed artefact per Revit version, and have the installer copy the right one. The build path stays available in Developer Mode for people extending Heron.

This makes step 7 disappear from the normal user's install, which is exactly what §1 of the spec asks for.

Tracked as [Q-6](OPEN-QUESTIONS.md).

---

## 4. **[NOTE]** Revit API assemblies cannot be redistributed

`RevitAPI.dll` and `RevitAPIUI.dll` are Autodesk's and are not redistributable. In CI they must come from either the official NuGet packages that exist for this purpose, or from a licensed installation. They are referenced with `<Private>false</Private>` / *Copy Local = false* so they are never shipped — Revit supplies them at runtime.

Worth confirming early, because it constrains the CI design.

---

## 5. **[NOTE]** Admin rights

Per-user add-in deployment (`%AppData%\Autodesk\Revit\Addins\<version>\`) needs no administrator rights. Per-machine (`%ProgramData%`) does.

Default to per-user. In a large contractor's environment, an installer that demands admin rights simply does not get installed.

---

## 6. Update system

Periodically checks the official Heron source and determines: installed version, latest version, changed components, compatibility, required migrations, security updates.

> *"A new Heron AI update is available. Update now?"*

Must support: version detection, download, dependency update, component update, migration, validation, **rollback**.

## 7. **[NOTE]** Update rules

1. **Never auto-update without consent.** A tool that silently changes behaviour mid-project is a liability during a submission.
2. **Never update while Revit is open with unsaved work.** Check first.
3. **The add-in update needs a Revit restart** — assemblies loaded into Revit cannot be unloaded. Say so plainly; do not report success for something that has not taken effect in memory.
4. **Back up the data class before migrating it.** Fragments, skills and memory are irreplaceable.
5. **Rollback must be tested**, not merely implemented. An untested rollback path is not a rollback path.
6. **Never touch the data class during a product update.** See [06 §2](06-heron-platform.md).
7. **Version pinning.** A user mid-delivery must be able to say "not now" and stay pinned without being asked again every day.

## 8. **[NOTE]** Migration is the hard part

Fragment schema, skill metadata, registry format and the vector index format will all change. Every change needs a forward migration, and each migration must be:

- **idempotent** (safe to run twice),
- **versioned** (the data records the schema version it was written with),
- **reversible or backed up**.

The vector index is the easy case — it is derived, so the migration is "delete and rebuild". Fragments and memory are the real work.

---

## 9. Self-healing (§52)

| Condition | Response | Permission |
|---|---|---|
| MCP disconnected | reconnect | automatic |
| missing dependency | install | confirm |
| stale index | rebuild | automatic |
| invalid fragment | quarantine | automatic |
| duplicate fragment | merge **proposal** | confirm |
| failed code | send to repair workflow | automatic (sandbox only) |
| outdated package | update **proposal** | confirm |

**[NOTE]** The split above is the safe reading of "controlled self-healing": Heron may freely repair **derived** state (indexes, caches, connections) and may freely *propose* anything else. It may not silently modify **product** or **data** state. Quarantine is the right default for a broken fragment — reversible, visible, and it stops the bad fragment being used without destroying it.
