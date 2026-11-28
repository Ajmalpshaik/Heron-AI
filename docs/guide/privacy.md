[Heron AI](../../README.md) › [Documentation](../README.md) › **Privacy**

# Privacy - what stays on this PC, and what does not

**For office IT, in one page.** Every line below names the file that makes it true, so it can be
checked rather than trusted. Nothing here is new policy: the rules are in [DISCLAIMER](../../DISCLAIMER.md),
[SECURITY](../../SECURITY.md) and [How Heron works](how-heron-works.md); this page puts the facts in one place.

## The short answer

- **The Revit model file never leaves the PC.** No `.rvt`, `.rfa` or template is uploaded
  ([D-26](../DECISIONS.md)).
- **Heron itself opens no connection to the internet while you work.** It talks to Revit, to its own
  files, and to a page on `127.0.0.1` - all on this PC.
- **The chat is not private to the PC.** Claude Code sends what you type, and what Heron answers, to the
  AI provider. That includes project names, file names, element counts and parameter values. They are
  **not** redacted first ([Q-40](../OPEN-QUESTIONS.md)), and there is **no local-only mode**. So a
  project under a confidentiality agreement is a decision for the project, made before Heron is used on
  it - not a setting ([DISCLAIMER - What Heron sends where](../../DISCLAIMER.md#what-heron-sends-where)).

## Every connection Heron makes

| Between | How | Reaches past this PC? | Where it is enforced |
|---|---|---|---|
| Claude Code and Heron's server | Standard input and output of a local process | No | The server is started by Claude Code on this PC (`.mcp.json`) |
| Heron's server and the Revit add-in | A Windows **named pipe**. Only the signed-in Windows user may open it, and every network logon is **denied** | No | `DenyNetwork` and `CreatePipe` in [`BridgeServer.cs`](../../revit/Heron.Bridge/BridgeServer.cs) |
| Your browser and the Companion page | HTTP on **`127.0.0.1` only**, on a port Windows picks; a one-time code, then a same-site cookie; it checks the Host header | No | [`heron_companion.py`](../../mcp/companion/heron_companion.py), held by `tests/test_companion.py` |
| Heron's server and the internet | **None.** No file of the server imports a network client | No | `OPEN_WORLD = ()` in [`heron_tools.py`](../../mcp/server/heron_tools.py), held by `tests/test_tool_registry.py` |
| Claude Code and the AI provider | Claude Code's own connection | **Yes** - this is the chat | Claude Code, not Heron. Its data handling is Anthropic's to state |

## When Heron does use the internet: installing

Only **Setup** goes online, and only while it installs or updates:

- It installs Heron's Python packages **from PyPI**, every one pinned to an exact version and checked by
  hash, so a file that is not the expected one is refused
  ([`PythonSetup.cs`](../../platform/Heron.Installer/PythonSetup.cs), [`requirements-lock.txt`](../../requirements-lock.txt)).
- Its download code fetches Heron's files only from **Heron's own GitHub release**, checks each against
  its published SHA-256 before writing it, and runs nothing it downloads
  ([`ReleaseDownload.cs`](../../platform/Heron.Installer/ReleaseDownload.cs),
  [`InstallSource.cs`](../../platform/Heron.Installer/InstallSource.cs)).
- It needs **no administrator rights**: everything goes into the Windows user's own folders.

## Where Heron keeps files on this PC

| Folder | What is in it | Roams with a roaming profile? |
|---|---|---|
| `%APPDATA%\Heron` | The add-in's settings and its **audit trail** of what was done | **Yes** - Windows copies `%APPDATA%` between PCs on a domain with roaming profiles |
| `%LOCALAPPDATA%\Heron` | Throwaway state: which Revit is running, the live status the Companion page reads | No |
| `%TEMP%\Heron` | The short-lived files a chat leaves for Revit to read, such as the Companion page's address | No |
| `%USERPROFILE%\.heron` | The Python side's data: the knowledge store, saved commands, checklists and the brain's own audit | No |

The folder names come from [`HeronPaths.cs`](../../platform/Heron.Core/HeronPaths.cs) (the add-in) and
[`heron_paths.py`](../../brain/heron_paths.py) (the Python side). Uninstall leaves your own settings and
history in place ([Installing Heron](install.md)).

## What this page does not cover

- **Claude Code and the AI provider's own handling** of the chat - retention, training, region. Ask
  Anthropic; Heron cannot change it.
- **Your project's own rules.** Whether a client allows AI tools on their model is the project's call.

## Go deeper

| Document | Covers |
|---|---|
| [DISCLAIMER](../../DISCLAIMER.md) | What Heron sends where, and no warranty |
| [SECURITY](../../SECURITY.md) | The permission rules, and how to report a problem |
| [How Heron works](how-heron-works.md) | What runs where, on Windows and in a cloud session |
| [12 - Security & Permissions](../12-security-and-permissions.md) | The full specification |

---

↑ [Documentation](../README.md) · [Home](../../README.md)
