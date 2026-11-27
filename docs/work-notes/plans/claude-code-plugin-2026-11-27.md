# Heron as a Claude Code plugin - the design, for the owner's yes

> **Type:** Operational work note. **Not specification.** Where a sentence here disagrees with the
> [Constitution](../../../HERON_CONSTITUTION.md), the [Golden Rules](../../14-golden-rules.md) or
> [DECISIONS.md](../../DECISIONS.md), **those win and this note is out of date.**
> **Status:** **Design only, nothing built.** Written 2026-11-27. **Owner:** Ajmal PS.
> **Not the same word as [`plugin-extension/`](plugin-extension/README.md)**, which is about Heron's
> *Revit* products (tabs). This note is about a **Claude Code** plugin: the package Claude Code installs.
> **Retires** when the owner says no, or when it is built and its PC checks are run.

---

## 1. In one minute

- **What it fixes.** Heron's chat side - the MCP server, the brain, its skills - would arrive as a
  **versioned Claude Code plugin**. Claude Code copies each version into its own folder, so a modeller
  never runs `git pull`, and a chat's code never changes under it mid-job (the [D-189](../../decisions/D-189.md)
  problem, and what [Group EU](../../needs-checking/group-eu.md)'s guard catches today).
- **What it does not replace.** The **Revit add-in still comes from Setup**. A plugin cannot install
  into Revit's Addins folder, and the plugin docs describe no way to run a Windows installer.
- **What a modeller sees.** Setup, once, as today. After that, Heron's chat side updates from inside
  Claude Code (`/plugin` - Update), and a new Setup is needed only when the Revit add-in changes.
- **The owner decides three things** - §9. Recommended: **yes**, built in its own pull requests.

## 2. Where Heron is today (read in code, 2026-11-27)

| Piece | Today | Where it says so |
|---|---|---|
| Heron folder on a modeller's PC | Setup unzips `heron-project.zip` to `%LOCALAPPDATA%\Programs\Heron AI` - no `.git`, so no pull | [`HeronSetup.iss`](../../../platform/Heron.Setup/HeronSetup.iss) `DefaultDirName`, [`build-setup.py`](../../../tools/build-setup.py) |
| Chat side registered | Setup writes a **user-scope** `heron` server: `python <folder>\mcp\server\heron_mcp_server.py` | [`ClaudeCodeEntry.cs`](../../../platform/Heron.Installer/ClaudeCodeEntry.cs) `Add` |
| Python packages | Setup runs `pip install --user --require-hashes -r requirements-lock.txt` | [`PythonSetup.cs`](../../../platform/Heron.Installer/PythonSetup.cs) |
| Revit finds the chat's Python | The chat writes a note naming its `python`, runner and root; the add-in reads it | `RunnerStart` in [`HeronCommandStore.cs`](../../../revit/Heron.Revit.Addin/HeronCommandStore.cs) |
| Add-in and server agree | Both carry a protocol number; a mismatch is refused with a message | `PROTOCOL_VERSION` in [`heron_bridge_client.py`](../../../mcp/client/heron_bridge_client.py) |

So a modeller who used Setup **already never pulls**. What the plugin adds is **updating the chat side
without a new Setup**, **skills and agents that travel with the server**, and **one version number**
Claude Code itself understands. The pull problem today is the owner's own checkout, which stays a
checkout (§7).

## 3. What goes in the plugin folder

**Built, not hand-kept.** A new `tools/build-plugin.py` makes `dist/heron-plugin/` from the **same list
`heron-project.zip` already ships** (`WORKSPACE_KEEP` in
[`build-release-assets.py`](../../../tools/build-release-assets.py) - one list, so the two cannot drift),
plus a small template folder:

```text
heron-plugin/
  .claude-plugin/plugin.json   name "heron", version = heron-products.json's version
  .mcp.json                    starts the server (§4)
  hooks/hooks.json             the Python check at session start (§5)
  skills/                      the modeller-side skills, if any are written for it
  agents/                      the modeller-facing agents from .claude/agents - heron-model-auditor first
  brain/  mcp/  docs/  README.md  LICENSE  NOTICE  DISCLAIMER.md  SECURITY.md  CHANGELOG.md
```

**Not in it:** `revit/`, `platform/`, `tests/`, `tools/`, `.claude/` (the developer's house rules and
hooks) and any knowledge store - the R-87 refusal in `build-release-assets.py` applies unchanged.
Measured 2026-11-27 with `du -sh`: `brain` 42M, `docs` 20M, `mcp` 2.8M, so a version is roughly the
size of today's download.

**The plugin docs' rule that matters:** every path must stay inside the plugin folder - no `../`. That
is why it is built as one folder rather than pointed at the repository.

## 4. How the server starts

`.mcp.json` inside the plugin:

```json
{ "mcpServers": { "heron": {
    "command": "python",
    "args": ["${CLAUDE_PLUGIN_ROOT}/mcp/server/heron_mcp_server.py"],
    "env": { "HERON_CLIENT_ID": "<see Q-100>" } } } }
```

- `${CLAUDE_PLUGIN_ROOT}` is the installed version's own folder, and **changes with every update** -
  so nothing Heron keeps may be written there. Heron already keeps its data in `%USERPROFILE%\.heron`
  ([`heron_paths.py`](../../../brain/heron_paths.py)) - **keep it there**, not in the plugin's data
  folder, which Claude Code **deletes on uninstall**.
- `python` as today: Setup installs Python 3.12 per user, and the user-scope entry already starts it
  this way. The plugin docs say nothing on `python` versus `py` on Windows - **a PC check** (§8).
- The client id waits on **[Q-100](../../open-questions/tier-3.md)** (#1488): the plugin should not
  carry the owner's PC name either.
- **Tool names change**: a plugin's tools are named `mcp__plugin_heron_heron__<tool>`, not
  `mcp__heron__<tool>`. `git grep -l "mcp__heron__"` lists what names the old form - the two
  `.claude/agents` files and a handful of tests today.

## 5. Python packages

Claude Code installs Node packages for a plugin, **never Python**; the documented way is a hook. So:

1. **Setup keeps installing the lock**, per user, exactly as today - proven on the owner's PC with
   v0.2.0's Setup on 2026-11-16 ([README](../../../README.md#install)).
2. The plugin adds **one SessionStart hook**, a short standard-library Python script: it compares the
   plugin's `requirements-lock.txt` with the copy it last installed from (a hash kept in the plugin's
   data folder) and, only when they differ, runs Setup's same pip line. Nothing changed - it does
   nothing, so it is quick on every start.
3. **The server says so if a package is missing** rather than failing silently - it already starts
   without the optional ones. The docs do not say whether a plugin's server waits for a SessionStart
   hook to finish, so the first start after an update may need one reopen. **A PC check** (§8).

## 6. The Revit add-in still ships with Setup

- Setup keeps putting the add-in into every Revit year. Nothing changes there.
- **Setup's "Add Heron to Claude Code" tick changes**: instead of writing the user-scope `heron`
  server, it runs `claude plugin marketplace add Ajmalpshaik/Heron-AI` and
  `claude plugin install heron@<marketplace> --scope user`, and **removes the old user-scope entry**
  it wrote before - otherwise the modeller has two Herons and every tool twice.
- **Skew between the two halves is already handled**: an add-in and a server on different protocol
  numbers are refused with a message. Recommended addition: the server says *"run Setup <version>"*
  when the add-in's release is older than the plugin's.

## 7. Versioning and updates

- **One number.** `plugin.json`'s `version` is `heron-products.json`'s `version`, written by the build,
  and `check-products` fails if they differ. With `version` set, Claude Code moves a modeller to a new
  version **only when that number changes** - pushes to `main` never reach them.
- **Where Claude Code fetches it from.** Recommended: a `.claude-plugin/marketplace.json` at the
  repository root, pointing at a **`plugin-release` branch written only by the release workflow**,
  holding only the built folder. `main` stays the developer repository; a modeller never sees it.
- **Auto-update is off by default for a marketplace that is not Anthropic's.** Updating is then
  `/plugin` - Installed - Update. Recommended: Setup turns auto-update on for Heron's marketplace
  (a per-marketplace setting the docs name; its exact key is **to confirm**).
- **The owner's own PC** keeps its git checkout for development. A chat opened in the checkout uses the
  checkout's `.mcp.json`, so the plugin must be **disabled for that one folder** - otherwise both
  start. [`DeveloperCopy.cs`](../../../platform/Heron.Installer/DeveloperCopy.cs)'s step-aside rule
  extends to it. The exact setting is **to confirm**.

## 8. What needs a real PC - none of this is checked

1. Install from the marketplace on a clean Windows user; the first chat lists Heron's tools.
2. `python` from a plugin's `.mcp.json` on Windows, with a space in the user's folder name.
3. The SessionStart hook after an update that changes the lock: does the first chat start, or need one
   reopen.
4. Revit's saved-command runner follows the plugin's folder (the note in §2) after an update.
5. Update mid-job: the running chat keeps its version until reopened; nothing is cut off.
6. Old Setup entry removed; no duplicate tools.
7. Uninstall the plugin: `%USERPROFILE%\.heron` and `%APPDATA%\Heron` untouched.

## 9. For the owner - three questions, one recommendation each

| Question | Recommended |
|---|---|
| Build Heron's chat side as a Claude Code plugin, as above? | **Yes** - in small pull requests: build script, release branch, Setup's tick, then the PC checks |
| Publish it from a `plugin-release` branch of this repository, or a second repository? | **A branch of this one** - one place, one release workflow |
| Auto-update on for modellers? | **On** - a version only moves when a release is published, so it is the same as running a new Setup |

## Sources

The Claude Code plugin documentation, read 2026-11-27: plugins - manifest reference, create a
marketplace, marketplace reference, install, loading, dependencies, and hooks, at
`code.claude.com/docs/en/`. Where it is silent - Python on Windows, a hook finishing before a server
starts, running a Windows installer - this note says **to confirm** or **a PC check**.

---

↑ [Work notes](../README.md)
