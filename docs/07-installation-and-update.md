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
