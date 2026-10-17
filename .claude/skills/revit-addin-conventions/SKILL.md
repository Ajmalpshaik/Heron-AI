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

**Every write produces exactly one undo step, IN ONE DOCUMENT.** The user presses Ctrl+Z once and
everything in that model reverses. Wrap a simple write in one `Transaction`; wrap anything with several
stages in one named `TransactionGroup`. On failure, roll back so the model is untouched.

**One undo cannot cross two documents, and that is Revit's shape rather than a convention:** every
`Transaction` and `TransactionGroup` constructor takes exactly one `Document`, so a job touching two models
is at best one undo *each*. Nothing in Heron writes to two documents today. If you are the one who builds
that ([D-47](../../../docs/DECISIONS.md)), the operation must **say so before it starts** - never after.

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

### Revit's own dialogs during a job

A Revit dialog that opens **inside** a Heron job has nobody to answer it, so the job hangs behind it
([row 5b-300](../../../docs/fragment-issues/section-5b-rows-276-300.md): a family from a later release
froze `LOAD_FAMILY` for over 90 s). Since 2026-10-17 `HeronDialogWatch` answers it while the dispatcher
has a job armed - **declined** (Cancel, then Close) unless a row in `HeronDialogRule` says otherwise -
and the job stops, so its group rolls back and the reply quotes what Revit asked. Built, not yet run in
Revit ([NEEDS-CHECKING group EL](../../../docs/needs-checking/group-el.md)).

- **A prompt Heron itself shows during a job goes inside `using (HeronDialogWatch.HeronAsks())`**, or the
  watch answers Heron's own question for the modeller. `tests/test_dialog_note.py` fails a bare prompt in
  any `Revit*.cs` file.
- **A new write path asks `HeronDialogWatch.Refused` before it keeps its group**, and rolls back when it
  is true. The same suite checks every `Assimilate()` under `revit/`.
- **Prefer the API's own answer where one exists** - `IFamilyLoadOptions` for a family already loaded,
  `CopyPasteOptions.SetDuplicateTypeNamesHandler` for duplicate type names. Revit's failures belong to
  the failures preprocessor, never to the watch.
- **A `HeronDialogRule` row needs the DialogId a real run reported and that run named in `Evidence`.**
  Never a guessed id, and never an answer other than Cancel or Close without one.

### Saves, syncs, exports and other models during a job

Constitution Article 10 is held in code since 2026-10-17. `HeronRunGuard`, armed by the dispatcher around
each job, cancels a save, Save As, sync, export or print of a model that was open when the job began -
unless the operation is `run_fragment_publish` - refuses a new document unless it is `run_fragment_admin`,
refuses a job that changes another open model, and closes what the job left open in the background.
Every decision is `HeronRunPolicy`'s, proved without Revit by `tests/test_run_guard.py`. Built, not yet
run in Revit ([NEEDS-CHECKING group EX](../../../docs/needs-checking/group-ex.md)).

- **Never call `Cancel()` on a Revit event without asking `e.Cancellable` first** - it throws on one that
  cannot be cancelled. The suite reads every call.
- **Heron's own fixed code that exports into a folder of Heron's own goes inside
  `using (HeronRunGuard.HeronExports())`** - `RevitCapture` does - or the guard cancels it under a read.
  The suite fails an export in the add-in's code outside one. Only an export is let through by it.
- **A path that works in a document other than the one in front names it with `HeronRunGuard.Target`**,
  or its own changes read as a change to another model.
- **A path that keeps a fragment's group asks `HeronRunGuard.Policy.Refuses`, and reads the switches again
  with `HeronRunPolicy.Withdrawn`, first** - `RevitFragment` does both. The gate's answer is as old as the
  job by then, and Article 13 says never cache a permission decision.
- **A save, a sync or an export of a model that was already open belongs in a PUBLISH fragment.** At any
  other level it is refused in Revit, whatever the card says. A file the job opened itself - or, under
  Admin, started - is its own to save.

## Logging

Log with `DateTime.UtcNow`, never `DateTime.Now`. The `u` format stamps a trailing `Z`, so local time
labels every line as UTC while being hours out — and UTC matches `startedAt` in the discovery file, so
the two line up when diagnosing.

The log is evidence ([Golden Rule 14](../../../docs/14-golden-rules.md)). Never silently discard a
skipped item: record what was skipped and why.

**Never append to a shared file by hand — call `HeronAppend.Line`.** This rule used to read *"take the
lock"*, and that was one scope short. A `lock` serialises the threads of ONE process; `addin-<date>.log`
is one file per machine per day and `audit-YYYYMM.jsonl` is one file per user, and **every Revit session
runs its own add-in in its own process**. On Windows the second session's `File.AppendAllText` throws a
sharing violation — `FileShare.Read` is what it opens with — and the `catch` around it loses the line in
silence, which is the one failure mode evidence cannot have. Measured, not reasoned:
[FRAGMENT-ISSUES §5b rows 5 and 19](../../../docs/FRAGMENT-ISSUES.md).

`HeronAppend.Line` opens with `FileShare.ReadWrite`, takes a **named system-wide** mutex so two
processes take turns rather than interleaving, retries briefly, and **returns whether the line landed**.
Keep the in-process `lock` as well — it costs nothing and keeps your own threads off the system mutex.
**Act on the `false`**: `HeronAudit` reports a lost line to the verbose log through its `ReportLoss`
hook, because a hole in the evidence has to be evidence too.

**One file per day**, named `addin-<yyyyMMdd>.log`, pruned by `log.retainDays`. A single file that grows
for ever cannot be retained for fourteen days, so the setting would be a lie.

## Never close Revit

Heron detects that Revit is open. It never closes it, and there is no flag to
make it.

An open Revit has a model in it, and that model very likely has unsaved work. Closing it so an install
can proceed would destroy hours of somebody's modelling to save them one click — and a script cannot
tell an idle session from one mid-edit.

> **Detect, report, refuse.** Name the release that is open and what to close. Then stop.

The same reasoning bounds the installer generally: it never touches a model, never saves on someone's
behalf, and never assumes an application it did not start is safe to shut down. Deciding to close a
model belongs to the person who has it open.

Installing is checked **per release**. Revit 2024 being open says nothing about whether it is safe to
install for 2020, so a single open session must not block every other version.

## The other repositories are reference only

Two other repositories sit alongside this one: an existing Revit add-in and a knowledge package. They
solve overlapping problems and are genuinely worth reading — but **nothing you write goes into them.**

- **Never commit, never open a pull request, never update them.** Every change belongs to Heron.
- **Never copy code or comments out of them.** Understand the mechanism, then write it here, in this
  repository's shape, with the reasoning stated for this codebase. The owner's instruction, 2026-08-28:
  *"study and use and make it part of our heron, blindly copy paste dont do it."*
- **Never cite them as this codebase's authority.** They stop being used once Heron is finished, so a
  comment whose argument is *"that other repository does it this way"* is worthless the day that
  happens. Put the reasoning in the file that needs it.

What that looks like in practice is `RevitFragment.SafeRollBack` and `RevitWrite.TryRefresh`: both exist
because of what that study revealed, and neither shares a line with what it came from — the hazard is
explained there, in terms of what `TransactionGroup` actually guarantees. (`SafeRollBack` lived in
`RevitWrite` too until 2026-10-17; since then **every write job's transaction group is rolled back
through `RevitFragment.RollBackAndCheck`**, which calls it and then checks what the rollback left, and
`tests/test_rollback_witness.py` fails a file that opens a group and does not. `RevitCapture`'s
always-rolled-back picture keeps a `SafeRollBack` of its own, and that suite names it as the one file not
yet watched.)

## The bridge is a boundary, not a pipe

Three rules govern how anything reaches Revit from outside.

**The newest connection takes the PIPE.** A chat that connects displaces the previous pipe immediately,
rather than queueing or waiting out a timeout, and the dropped client reconnects on its next call.
Growing a pool instead only postpones the question of which chat is really in charge.

**But taking the pipe is not taking the right to use it.** Since Step 6 a **lease** decides that
(`HeronLease`, [D-22](../../../docs/DECISIONS.md)): one lease per Revit process, held by one chat, short
and renewable. A second chat is **refused with a message** rather than silently cutting the first off
mid-job — chopping a read is harmless, chopping a `MODIFY` mid-transaction is not.

Two things about it are easy to get wrong, and both have already been got wrong once:

- **`ping` and `info` are lease-EXEMPT.** Asking who holds a Revit must never be the act of claiming it,
  or the `(free)`/`(in use)` picker this enables becomes impossible. A tool that reads *every* session
  must use `info`, not `count_elements` — `revit_health` claimed a lease on every free Revit for one
  commit because it used the latter.
- **It cannot block a rollback** ([docs/25](../../../docs/25-multi-session-and-binding.md)). It holds by
  construction rather than by a special case: the lease is checked once when a request arrives, and a
  rollback happens *inside* a request already admitted.

**A displaced pipe is never closed in the middle of an answer** (since 2026-10-13,
[FRAGMENT-ISSUES 5b-458](../../../docs/FRAGMENT-ISSUES.md)). The pipe is still displaced at connect
time, before the lease can speak - but a connection whose request Revit is running keeps its pipe until
that answer is written, then closes; only one waiting for its next request is dropped at once. Before
that, ANY new connection - another chat, a tool merely pinging - cut off the answer then owed, and a
write that had run was reported *"the answer was lost"*. If you touch `BridgeServer`, keep the two
moves on one compare-and-swap (`Connection`): a connection must never be both displaced and answered
from a disposed pipe. docs/25 §3 has the measurements.

**Every request authenticates first — before the operation is even read.** A caller with the wrong token
must not learn which operations exist. The token is minted on connect and destroyed on disconnect, so
one read before a reconnect is refused rather than quietly served, and it is compared in constant time:
a normal string comparison returns on the first differing character, which leaks the secret one
character at a time to anything able to measure the reply.

Be honest about what that buys. The pipe's ACL is what keeps other *people* out. The token stops another
process running as the **same user** from reaching Revit by guessing a pipe name — it would have to read
the discovery file first, which makes reaching a live model a deliberate act rather than an accident.

## Naming the document is not always enough

Every answer names the model it came from — that rule stands. But two Revit sessions can have models
with the **same name** open at once, and then *"in Project1"* identifies nothing at all. That is not
hypothetical: it happened on the first live run of the selection tool.

> When more than one session could be meant, **name the session too**, not just the document.

The general rule: an identifier is only an identifier if it is unique among the things it has to
distinguish. Check what it is competing with before trusting it.

## An assumption is not a choice

When Heron picks something the user did not explicitly pick — which Revit session, which document,
which of anything — **record that it was assumed**, and treat the two differently for ever after.

| Bound how | What changed | What must happen |
|---|---|---|
| **Assumed** | a second candidate appears | **Ask.** They never chose this one |
| **Assumed** | it disappears | Quietly take the remaining one |
| **Chosen** | more appear | Keep it. Do not nag |
| **Chosen** | it disappears | **Stop and say so.** Never slide onto another |

Collapse those into one flag and "sticky" becomes "keep whatever we picked first" — so a second Revit
opening mid-conversation sends every later command to the first one, silently. That is the wrong-model
failure arriving through the mechanism built to prevent it, and it was found by running the code, not by
reasoning about it ([docs/25](../../../docs/25-multi-session-and-binding.md)).

**Every refusal says "nothing has been sent to Revit"** in as many words. The user's first thought on
any refusal is *did it half-do something?* — answering that unasked is the difference between a safe
stop and a frightening one.

Covered by `tests/test_session_binding.py`, one case per row, no Revit needed.

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
