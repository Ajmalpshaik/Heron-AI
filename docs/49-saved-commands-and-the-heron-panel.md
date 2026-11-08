<!--
Heron-Agent:  HERON-USR-SKL-003
Heron-Step:   18
Heron-Status: DRAFT
Heron-Since:  0.1.0
Heron-Layer:  brain
See docs/29-metadata-standard.md
-->

# 49 — Saved commands and the Heron panel

> **Status: built, compiled for Revit 2020 to 2027, tested without Revit. Never run in a real Revit.**
> Decision: [D-151](decisions/D-151.md), **Proposed**. Asked by the owner on 2026-11-08 after
> [docs/48](48-studying-bimlogiq.md) §5 item 1: *"same like a companian app a docoble panel ... same like
> daynamo if i save that and if i clock it will run"*.

---

## 1. What the modeller sees

1. In the chat, after a job they kept: *"save that as Room Sections, ask me the level each time"*.
2. Heron tab → Heron button's menu → **Commands**. A panel docks on the right: a search box, the saved
   commands, and under the one picked, its inputs with their defaults and **Run**.
3. Run. Revit shows the change and asks **Keep or Discard**, as it does for a chat. The panel shows
   Heron's answer.

A command saved in the chat appears in the panel within a second - **no Revit restart**. Installing the
panel itself needs one restart, as any add-in update does.

## 2. What a command is

One file, `%USERPROFILE%\.heron\commands\personal\<name>.json`:

```json
{"heron-command": 1, "name": "Room Sections", "tool": "change",
 "capability": "CREATE_SECTION_VIEW",
 "values": [{"name": "levelName", "value": "{level}"}],
 "inputs": [{"name": "level", "label": "Level", "default": "Level 1"}]}
```

- **ONE capability.** A click is one request, and Article 8 gives one request one undo entry. Several
  steps would leave several entries behind one click; they wait for a decision (§6).
- **Checked when saved**, against the fragment's own `contract.needs`: a value it does not take, an
  `{input}` nobody declared and an input no value uses are each refused by name, and nothing is kept.
- **`tool` is read from the fragment's risk**, not chosen by the chat: READ and ANALYZE go through
  `revit_read`; above that, `revit_change`. **PUBLISH and ADMIN are never saved** - a sync, export or
  new workset is asked for each time (Article 10).
- **Not checked when saved:** whether it is PROVEN, which switch it needs, which model. Those are asked
  again on **every run**, because a proof can go stale between saving and running (D-30).

## 3. How a run reaches Revit - and why through Python

```
Heron panel (C#, no Revit API)  --starts-->  python heron_command_run.py --name --revit-pid --set
                                                   |
                                          revit_change / revit_read  (their own bodies, via _through)
                                                   |
                                          the pipe -> the add-in's gate -> one TransactionGroup
                                                   |
                                          Revit: Keep or Discard
```

**Every rule a change obeys lives in the Python door** - the PROVEN refusal (D-117), the risk and its
switch (D-106), undeclared values named, the model pinned. The Companion's Apply already reuses that door
through `_through`; the runner does the same. Writing the rules again in C# for the panel would be a
second copy, and two copies drift.

- **Which Revit:** the one whose panel was clicked, by process id, **chosen** - never a guess (Article
  12a). Not connected, and nothing is sent; the panel says to press the Heron button.
- **Who it is to the lease:** `HERON_CLIENT_ID` from the Heron folder's `.mcp.json`, the same person as
  the chat (D-22). Its conversation is its own, and it hands back what it held when it ends (D-141) - so
  while a chat is mid-job on the same model, the panel's run is refused until that chat lets go.
- **Where Python is:** each chat's MCP process writes `commands\runner.json` at start-up naming
  `sys.executable` and the Heron folder. A command can only have been saved by a chat, so the note exists
  before the first command does.
- **The panel never waits on Revit's thread.** It starts the process and returns; Revit stays free to
  run the job and ask Keep or Discard. Both output streams are read on a worker thread.

## 4. What was built

| Part | File |
|---|---|
| The store: check, keep, list, find, delete, fill inputs, the runner note | [`brain/heron_commands.py`](../brain/heron_commands.py) |
| The chat's tool `heron_commands` (save, list, show, delete) - READ, touches no model | [`heron_mcp_server.py`](../mcp/server/heron_mcp_server.py), [`heron_tools.py`](../mcp/server/heron_tools.py) |
| The runner the panel starts | [`heron_command_run.py`](../mcp/server/heron_command_run.py) |
| The panel's reading and its command line | [`HeronCommandStore.cs`](../revit/Heron.Revit.Addin/HeronCommandStore.cs) |
| The panel | [`HeronCommandsPane.cs`](../revit/Heron.Revit.Addin/HeronCommandsPane.cs) |
| **Commands** row, pane registration | [`Commands.cs`](../revit/Heron.Revit.Addin/Commands.cs), [`HeronApplication.cs`](../revit/Heron.Revit.Addin/HeronApplication.cs) |
| The shared folder | [`HeronPaths.cs`](../platform/Heron.Core/HeronPaths.cs) (`PythonData`, `CommandsPersonal`, `CommandRunnerNote`) |

Running from the chat needs no tool of its own: `heron_commands show` gives the capability and values,
and the chat calls `revit_change` with the inputs put in.

## 5. What is proven, and what is not

| | |
|---|---|
| **PASS** | `tests/test_commands.py` - the store, filling, the runner against a stand-in server; runs everywhere |
| **PASS** | `tests/test_commands_tool.py` - the MCP tool through the real fragment contracts; needs the MCP SDK, so CI lists it as not runnable |
| **PASS** | `tests/test_command_store.py` - a command the Python side saved is read by the panel's C#; every input survives the Windows command line whole |
| **PASS** | `tools/check-compile.py` - every project on Revit 2020 to 2027 |
| **NEEDS REAL REVIT** | the pane docks and shows; Commands opens it; a save in the chat appears without restart; Run starts the runner, reaches this Revit, Keep or Discard is asked, the answer is shown; a run while a chat holds the model is refused in words; a run with the bridge off says to press Heron |

## 6. Not built - each waits on the owner

1. **Several steps in one command**, under one undo. Needs a decision on Article 8 for a saved sequence.
2. **Team commands** - a shared folder, and who may publish to it (`heron_userskills.py`'s ladder).
3. **Pin a command to the ribbon** - D-150 keeps the ribbon to three things.
4. **Edit a command in the panel** - today it is saved again from the chat with `replace`.
