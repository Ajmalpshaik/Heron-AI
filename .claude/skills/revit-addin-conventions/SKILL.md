---
name: revit-addin-conventions
description: Use before writing, refactoring or debugging any code in this repository - C# that loads into Revit, the Python bridge client, or a build script. Covers where each kind of code belongs and what may depend on what, the metadata header every source file carries, the rule that only the path manager builds paths, the Revit API threading constraint and the ExternalEvent that satisfies it, when a transaction is and is not needed, the single-undo rule, how error messages must be worded, which dialogs are allowed, and the checks to run before calling anything done. Trigger on "add a command", "fix this add-in", "where does this code go", "why does this crash Revit", "refactor this", or any change to the add-in, the bridge, or the platform.
---

# Add-in Conventions

House rules for code in this repository. They are not style preferences — most of them are enforced by
`tools/check-structure.py` and `tools/check-metadata.py`, and a change that ignores them fails the
checks.

## Where code goes

| Folder | Holds | Changing it means |
|---|---|---|
| `revit/` | Everything that loads into `Revit.exe`. C#. The **only** place `Autodesk.Revit` may appear | a Revit **restart** |
| `mcp/` | The bridge to the AI host. Python, runs outside Revit | nothing to restart |
| `brain/` | Knowledge, retrieval, fragments, skills. Python, outside Revit | nothing to restart |
| `platform/` | Kernel, paths, configuration, install. Shared by everything | rebuild everything |

**Layering:** anything may depend on `platform`; `platform` depends on nothing. `revit` and `brain`
never touch each other. `Heron.Bridge` deliberately has no Revit reference at all — that is what makes
it testable without Revit, and the project file is the enforcement.

Before adding a folder at the top level, ask whether it belongs inside one of the four parts. It almost
always does.

## The metadata header

**Every `.cs`, `.py` and `.ps1` file starts with these five fields.** No exceptions —
`tools/check-metadata.py` verifies them and cross-checks the agent ids against the registry in both
directions.

```csharp
// Heron-Agent:  HERON-REVIT-CON-001        (or 'none' if this file implements no registry agent)
// Heron-Step:   1                          (the build step this belongs to)
// Heron-Status: DRAFT                      (DRAFT | TESTING | VALIDATED | PROVEN | PRODUCTION ...)
// Heron-Since:  0.1.0
// Heron-Layer:  revit                      (bridge | revit | brain | platform | test | tool)
// See docs/29-metadata-standard.md
```

Use `#` instead of `//` in Python and PowerShell. The full definition is in
[docs/29](../../../docs/29-metadata-standard.md).

## Only the path manager builds paths

`HeronPaths` is the single place that knows where anything lives. Nothing else may construct a Heron
path, and **no file may contain a literal `%APPDATA%` or `%LOCALAPPDATA%`** — not in code, and not in a
message shown to the user.

```csharp
// Wrong - and this exact drift has already happened once, in a message that
// sent users to %APPDATA% when the logs are under %LOCALAPPDATA%.
message = "See the log in %APPDATA%\\Heron\\logs.";

// Right - ask for it.
message = "Heron did not start. The log that says why is in " + HeronPaths.Logs + ".";
```

The three categories exist for a reason and must not blur:

- **Product** — the running assemblies. Replaced wholesale on update.
- **Data** — `%APPDATA%\Heron`. The user's own. Survives every update. Roams between machines.
- **Derived** — `%LOCALAPPDATA%\Heron`. Rebuildable cache and runtime state. Safe to delete at any
  moment, and deleting it must always be a valid recovery action. **Deliberately not roaming**: a
  discovery file naming a process on another machine is meaningless here.

The audit log lives under **Data**, not Derived, because it is evidence and must survive a cache wipe.

## The Revit API threading constraint

This is the single most important thing to know about this codebase.

> **The Revit API can only be called from Revit's own thread, inside an API context.**

The bridge runs on background listener threads. The MCP server is a separate process entirely. Neither
can call the Revit API directly. Everything reaching the API must be marshalled through **one
`ExternalEvent`, one request queue, one handler** — [D-09](../../../docs/DECISIONS.md).

Consequences to hold on to:

- **Never cache a `Document` across invocations.** Ask for the current one each time.
- **Nothing blocks the main thread.** A slow handler freezes Revit's user interface.
- **`Raise()` is a request, not a guarantee.** Revit runs the handler when it is idle and ready. If a
  modal dialog is open it will not run at all — the caller must get a clean *"Revit is busy"*, never a
  hang.
- Calling the API from a modeless window's code-behind is the classic way to crash Revit with
  *"outside API context"*. Route it through the event.

## Transactions

**A read-only operation takes no transaction.** Counting, checking, listing, exporting, reporting —
none of these open one. Do not create an empty transaction "just in case".

**Every write produces exactly one undo step.** The user presses Ctrl+Z once and everything reverses.
Wrap a simple write in one `Transaction`; wrap anything with several stages in one named
`TransactionGroup`. On failure, roll back so the model is untouched.

Name it for what the user did, so Revit's undo history reads properly: `Heron: Move ducts up 200 mm`.

Everything through Step 5 is read-only by design. Writes begin at Step 6, and they arrive **with** the
safety rails — transaction group, preview, re-count before executing, document pinning, the permission
gate and the emergency stop — not after them. A write path built before its safety path is a write path
that ships without one.

## Talking to the user

The reader is a BIM modeller, not a developer. Every message follows one shape: **what happened, then
what to do next.**

```
Bad:   NullReferenceException at line 42 in ConnectCommand.cs
Bad:   Heron did not initialise.
Good:  Heron did not start. The log that says why is in C:\Users\...\AppData\Local\Heron\logs.
Good:  No ducts in the active view. Open a view that has ducts and try again.
```

No stack traces, no exception class names, no developer jargon.

### Which dialogs are allowed

| | |
|---|---|
| ✅ | A failure the user must know about |
| ✅ | Something required is missing — nothing selected, no document open |
| ✅ | Confirmation **before** a risky change: delete, move, rename, overwrite, bulk edit |
| ✅ | A warning that must be seen before continuing |
| ❌ | A success popup — "Done", "Connected", "Finished" |
| ❌ | Repeated alerts, or anything needing no action |

Use a status line, a results list, or the log for anything that is merely informative.

## Logging

Log with `DateTime.UtcNow`, never `DateTime.Now`. The `u` format stamps a trailing `Z`, so local time
labels every line as UTC while being hours out — and UTC matches `startedAt` in the discovery file, so
the two line up when diagnosing.

The log is evidence ([Golden Rule 14](../../../docs/14-golden-rules.md)). Never silently discard a
skipped item: record what was skipped and why.

**Take the lock.** Listener threads and the Revit thread all write. Appending from several at once
throws a sharing violation, and a `catch` around it loses the line in silence — the one failure mode
evidence cannot have.

**One file per day**, named `addin-<yyyyMMdd>.log`, pruned by `log.retainDays`. A single file that grows
for ever cannot be retained for fourteen days, so the setting would be a lie.

## Configuration is a promise

Every key in `HeronConfig` must be **read by something**. A setting that is declared, documented and
then ignored is worse than no setting: the user changes it, nothing happens, and nothing says why.

When a setting has nothing left to control, delete it and say so. It can come back when there is
something for it to do.

## Before calling anything done

```bash
python tools/check-docs.py
python tools/check-metadata.py
python tools/check-structure.py
python tests/test_bridge_roundtrip.py
```

Five seconds, and between them they have already caught a miscounted agent registry, three files with no
metadata header, a path built in two places, and a file referencing `Autodesk.Revit` outside `revit/`.

For anything touching the add-in, also build the full supported span — see the
`revit-version-support` skill. Compiling for one release proves nothing about the others.

## Honesty

- **Never claim something is tested in Revit** unless it has actually been run in Revit. "It builds" is
  not "it works". Say *"built, not yet run in Revit"* and mean it.
- **Never invent an API member.** If unsure it exists in a release, say it needs verification.
- **Every number in the documentation is derived, not typed.** The tooling exists; use it. A stated
  count is a claim, a derived count is a fact.
