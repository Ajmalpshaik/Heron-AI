# Needs checking — Group AS

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-25 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group AS - Talk: from Revit into the open chat, with the selection saved (2026-09-27)

> **TALK WAS REMOVED ON 2026-09-27 - [D-105](../DECISIONS.md#d-105--talk-is-removed-and-the-modeller-works-with-heron-in-the-chat-only).** It ran once, on the owner's PC: AS1 to AS3
> passed, AS4 failed - a message was taken and never shown - and he had Talk removed from Revit and
> from the code the same evening. **None of these checks can run again.** They stay as the record of
> what that run showed, and the links to the removed files point at commit `5b18541a`.

**The owner's PC, Revit 2024 first, on a TEST COPY - never a live project.** Step 1 of
[D-104](../DECISIONS.md): Revit's **Talk** button opens a box, the modeller types (or presses Win+H and
speaks), and the words go - with what is selected - into the Claude Code chat started by
[`mcp/heron-talk.cmd`](https://github.com/Ajmalpshaik/Heron-AI/blob/5b18541a/mcp/heron-talk.cmd). Revit does not wait for the answer.

**What is already known, and it is NOT a proof.** The mailbox that writes each message
([`HeronTalkMailbox.cs`](https://github.com/Ajmalpshaik/Heron-AI/blob/5b18541a/revit/Heron.Revit.Addin/HeronTalkMailbox.cs)) runs in
`tests/Heron.Talk.TestHost`, and every step it writes is read back by the Python the chat uses
([`tests/test_talk_contract.py`](https://github.com/Ajmalpshaik/Heron-AI/blob/5b18541a/tests/test_talk_contract.py)). Heron's own MCP server, served by a
real MCP SDK over in-memory streams the way Claude Code speaks to it, declares `claude/channel` and
delivers a dropped message as `notifications/claude/channel`
([`tests/test_talk_served.py`](https://github.com/Ajmalpshaik/Heron-AI/blob/5b18541a/tests/test_talk_served.py)). **None of it has met Revit or Claude
Code.** No Talk button has been seen, and no channel event has reached a real model.

**Before AS2, update Claude Code** and note its version: channels are a research preview, and the flag
the launcher passes may change.

| # | Check | Expected |
|---|---|---|
| ~~**AS1**~~ | Deploy the add-in to Revit 2024, open a test copy, and look at **Heron > AI Bridge** | **PASSED 2026-09-27 on the owner's PC, Revit 2024** - the verdict is in *Group AS was RUN*, below. Talk was removed that evening ([D-105](../DECISIONS.md#d-105--talk-is-removed-and-the-modeller-works-with-heron-in-the-chat-only)). *It was to look like:* A **Talk** button with the speech-bubble picture, enabled with a model open. **FAIL** - no button, or the whole tab missing - is the ribbon code: the add-in log names why |
| ~~**AS2**~~ | In the Heron folder, double-click `mcp\heron-talk.cmd` | **PASSED 2026-09-27 on the owner's PC, Revit 2024** - the verdict is in *Group AS was RUN*, below. Talk was removed that evening ([D-105](../DECISIONS.md#d-105--talk-is-removed-and-the-modeller-works-with-heron-in-the-chat-only)). *It was to look like:* Claude Code shows its development-channels warning naming `server:heron`; choose *I am using this for local development*. It then notes that messages from `server:heron` inject into the session, `/mcp` shows `heron` connected, and `%LOCALAPPDATA%\Heron\talk\listener-<pid>.json` exists and its time moves every few seconds. **FAIL** - no such note, or a *blocked by org policy* line - means Talk cannot work on this account yet: write down the exact words and the Claude Code version |
| ~~**AS3**~~ | **The negative.** Close that chat and start Claude the ordinary way. Connect Heron, select three ducts, press **Talk**, type *count these*, press Enter | **PASSED 2026-09-27 on the owner's PC, Revit 2024** - the verdict is in *Group AS was RUN*, below. Talk was removed that evening ([D-105](../DECISIONS.md#d-105--talk-is-removed-and-the-modeller-works-with-heron-in-the-chat-only)). *It was to look like:* The box stays open, says **no chat is listening, so nothing was sent**, and the words are still in it. `%LOCALAPPDATA%\Heron\talk\<Revit pid>` holds **no** `message-*.json`. **FAIL** - a message file written with nobody listening - would be a modeller's words lost in silence |
| ~~**AS4**~~ | **The positive.** The AS2 chat open, Heron connected, three ducts selected in the test copy. **Talk**: *how many are selected and what size are they* - Enter | **FAILED 2026-09-27 on the owner's PC** - the message was saved and never shown, and when the chat closed it was claimed and lost; the verdict is in *Group AS was RUN*, below. Talk was removed that evening ([D-105](../DECISIONS.md#d-105--talk-is-removed-and-the-modeller-works-with-heron-in-the-chat-only)). *It was to look like:* The box closes at once and Revit is usable. Within about a second the terminal shows the message arriving from `heron`; Claude reads the saved selection with `heron_selection` and answers about **three ducts in this model**. The talk folder now holds `message-1.taken.json` and `selection-1.json`. **FAIL** - nothing arrives - is the channel; Claude answering about the live selection instead of the saved one is the instructions |
| ~~**AS5**~~ | **Revit does not wait, and "these" means what was sent.** Straight after an AS4-style message, click around Revit and select two walls instead. Then **Talk**: *select the ones I sent you* | **CANNOT BE RUN from 2026-09-27.** Talk was removed ([D-105](../DECISIONS.md#d-105--talk-is-removed-and-the-modeller-works-with-heron-in-the-chat-only)) after AS4 failed; this was never run. *It was to look like:* Revit stayed responsive the whole time, apart from Heron's banner while a tool ran. The ducts from the message are selected - **not** the walls. **FAIL** - the walls, or Revit frozen until the answer - breaks the two things D-104 promised |
| ~~**AS6**~~ | In the Talk box, press **Win+H** and say *select all ducts on level one* | **CANNOT BE RUN from 2026-09-27.** Talk was removed ([D-105](../DECISIONS.md#d-105--talk-is-removed-and-the-modeller-works-with-heron-in-the-chat-only)) after AS4 failed; this was never run. *It was to look like:* The words appear in the box, and Enter sends them. **Record either way**: the box is a transparent, borderless WPF window, and whether Windows voice typing writes into it has not been seen. Step 2 must not lean on Win+H if this fails |
| ~~**AS7**~~ | **Changes still guard the model.** With **Changes off**, **Talk**: *move these up 200* on three selected ducts | **CANNOT BE RUN from 2026-09-27.** Talk was removed ([D-105](../DECISIONS.md#d-105--talk-is-removed-and-the-modeller-works-with-heron-in-the-chat-only)) after AS4 failed; this was never run. *It was to look like:* Heron refuses the change and Claude says so; nothing moved. With **Changes ON**, the same message moves them, and **one Ctrl+Z** puts them back. **FAIL** - a move with Changes off - would mean pre-allowing Heron's tools bypassed Heron's own gate, which D-104 says it must not |
| ~~**AS8**~~ | **Two Revits.** Two Revits open and connected; in the chat, choose the first with `revit_use_session`. Then **Talk** from the second | **CANNOT BE RUN from 2026-09-27.** Talk was removed ([D-105](../DECISIONS.md#d-105--talk-is-removed-and-the-modeller-works-with-heron-in-the-chat-only)) after AS4 failed; this was never run. *It was to look like:* The message arrives with a note that it came from a different Revit from the one chosen, and Claude **asks** before sending anything to Revit. Then close both chats, start a fresh Talk chat, and Talk from the second Revit: the chat binds to the second, as chosen |
| ~~**AS9**~~ | Close Revit | **CANNOT BE RUN from 2026-09-27.** Talk was removed ([D-105](../DECISIONS.md#d-105--talk-is-removed-and-the-modeller-works-with-heron-in-the-chat-only)) after AS4 failed. Half of it was seen anyway - see *Group AS was RUN*, below. *It was to look like:* `%LOCALAPPDATA%\Heron\talk\<that Revit's pid>` is gone, and `heron_selection` in the chat says nothing is saved from it |
| ~~**AS10**~~ | AS1, AS3 and AS4 on **Revit 2020** and **Revit 2027** | **CANNOT BE RUN from 2026-09-27.** Talk was removed ([D-105](../DECISIONS.md#d-105--talk-is-removed-and-the-modeller-works-with-heron-in-the-chat-only)) after AS4 failed; this was never run. *It was to look like:* The same - the box is WPF on .NET Framework 4.7.2 at one end and .NET 10 at the other |

### Group AS was RUN on the owner's PC - 2026-09-27. AS1 TO AS3 PASSED, AS4 FAILED, AND TALK WAS REMOVED

Run by a session working beside the owner, row by row, with his hands in Revit and his screenshots as the
eyes. **Every time below is the PC's, from a file or a log** - a watcher read the talk folder four times
a second and logged each change, beside the add-in's log and Claude Code's own log for Heron's server.

**What it ran on**

| | |
|---|---|
| Model | The owner's test copy, *heron ai bulding* - his word that it is a copy with ducts, and the name every Talk file recorded. Views `1 - Mech` and `{3D}` |
| Revit | 2024 - process 24776 for AS1 and AS3, then 25652 after the owner restarted it, for AS4. The add-in was built from `5b18541a` for 2020, 2024 and 2027 and deployed to all three, each deployed file hash-checked against its build |
| Claude Code | 2.1.283, the terminal build, installed for this run - the PC had only the desktop app's own copy (2.1.281), which is not on `PATH`, so the launcher could not have started. Signed in to the owner's Claude Max account |
| MCP SDK | 1.15.0 on Python 3.11.9; `mcp.types` and `mcp.shared.message` both import |
| Changes | Off from 18:33 - on since 2026-09-23 until the owner switched it off for this run. He switched it on again at 19:21:27, after AS4's message; no change to the model was asked for |

**The rows**

| Row | Verdict | Evidence |
|---|---|---|
| AS1 | PASS | Add-in log `15:05:32Z Heron loaded. Revit 2024, add-in 0.1.0.0, pid 24776`. The owner's screenshot of **Heron > AI Bridge**: *Heron*, *Changes ON*, and **Talk** with its blue speech bubble. The button opened its box both times it was pressed |
| AS2 | PASS | The launcher's process ran `claude --dangerously-load-development-channels server:heron --allowedTools mcp__heron`. The warning, in the owner's screenshot: *WARNING: Loading development channels ... Channels: server:heron ... 1. I am using this for local development*. `/status`: *Channels: Listening for messages from server:heron*. Claude Code's log for the server: *Channel notifications registered* at 16:11:27Z. `/mcp`: *heron*, connected, 36 tools. `listener-53412.json` appeared at 19:11:27 and was rewritten about every 5 seconds. **Not seen: the dim startup line itself** - 2.1.283 folds it under *2 more notices hidden*. Its wording, read from the program, is *Channels (experimental) messages from server:heron inject directly in this session · restart without --dangerously-load-development-channels to stop* |
| AS3 | PASS | Before any Talk chat existed, Heron connected (log `15:07:03Z Connected from the ribbon`). The box, in the owner's screenshot: *3 selected: Ducts 3*, *No chat is listening, so nothing was sent...*, and the words still in it. At 18:18:16 `talk\24776` did not exist, the talk folder held no message and no listener file, and the add-in log had no *message saved* line. An earlier try, with six air terminals and a room tag selected, did the same |
| AS4 | **FAIL** | The message was saved and never shown; when the chat closed it was claimed and lost - below |
| AS5 | NOT RUN | Talk was removed ([D-105](../DECISIONS.md#d-105--talk-is-removed-and-the-modeller-works-with-heron-in-the-chat-only)) after AS4 |
| AS6 | NOT RUN | The same |
| AS7 | NOT RUN | The same |
| AS8 | NOT RUN | The same |
| AS9 | NOT RUN | The same. Its first half was seen anyway: Revit 2024 (25652) closed at 19:26:17 and `talk\25652` was gone at 19:26:17.5 |
| AS10 | NOT RUN | The same. Revit 2020 loaded the add-in that carried Talk - `Heron loaded. Revit 2020` three times after 18:04 - but no Talk button was looked at there, and 2027 was not opened |

**AS4, as the files and logs saw it**

| Time | What happened |
|---|---|
| 19:11:27 | The Talk chat's server starts listening (`listener-53412.json`), and Claude Code registers the channel |
| 19:17:46 | The owner restarts Revit 2024 (the first one's bridge had stopped at 19:02:58); Heron connected at 19:19:57 |
| 19:20:07.8 | The listener's file is rewritten for the last time |
| 19:20:10.2 | Talk saves `selection-1.json`, then `message-1.json` - log: *Talk: message 1 saved for the chat, with 3 selected in heron ai bulding*. The box closed at once (*"is gone"*, the owner) |
| 19:20 to 19:26 | Nothing moves. The message is not claimed, the listener's file is not rewritten, and the chat's transcript gains nothing - no `<channel source="heron"` in it at all |
| 19:24:00 | Another Claude session, in the desktop app, starts Heron, reads the saved selection with `heron_selection` and asks Revit something with `revit_read` - both answered |
| 19:26:14.7 | Claude Code closes the Talk chat's server - its log: *Terminating MCP server process tree* |
| 19:26:14.9 | `message-1.json` becomes `message-1.taken.json`, claimed in the server's last moment, and the listener's file is gone |
| 19:26:17.5 | Revit 2024 closes and deletes its `talk\25652` folder, with the claimed message in it |

**What it means.** The message was shown to nobody, and it was claimed - the one thing D-104 said
could not happen to a message nobody read. The listener's loop stopped after it found the message and
before it claimed it; between the two, `collect()` asks `live_revits()` - `bridge.discover()`, which
pings each Revit's bridge - which Revits are live. It went on only when the chat was being closed.
**Which call it waited in is not proven**: no stack was taken before Talk was removed.

**Found on the way - a second attempt meets these first**

1. `heron-talk.cmd` needs a terminal `claude` on `PATH`, and the owner's PC had only the desktop app's
   copy. A fresh install starts signed out, and the first Talk chat ran signed out with its listener
   already writing, so Revit would have sent into a chat that could not answer. Nothing was sent then,
   so what would have happened was not seen.
2. Claude Code's first question about Heron's server comes up with *Continue without using this MCP
   server* selected. Enter there starts a Talk chat with no Heron in it.
3. The channel's startup line is folded under *N more notices hidden*. `/status` is where it shows.
4. In AS3's second try the box held *ount these* where *count these* was meant. Whether the first key
   was lost or never pressed is not known.
