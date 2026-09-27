# Needs checking — Group AS

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-25 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group AS - Talk: from Revit into the open chat, with the selection saved (2026-09-27)

**The owner's PC, Revit 2024 first, on a TEST COPY - never a live project.** Step 1 of
[D-104](../DECISIONS.md): Revit's **Talk** button opens a box, the modeller types (or presses Win+H and
speaks), and the words go - with what is selected - into the Claude Code chat started by
[`mcp/heron-talk.cmd`](../../mcp/heron-talk.cmd). Revit does not wait for the answer.

**What is already known, and it is NOT a proof.** The mailbox that writes each message
([`HeronTalkMailbox.cs`](../../revit/Heron.Revit.Addin/HeronTalkMailbox.cs)) runs in
`tests/Heron.Talk.TestHost`, and every step it writes is read back by the Python the chat uses
([`tests/test_talk_contract.py`](../../tests/test_talk_contract.py)). Heron's own MCP server, served by a
real MCP SDK over in-memory streams the way Claude Code speaks to it, declares `claude/channel` and
delivers a dropped message as `notifications/claude/channel`
([`tests/test_talk_served.py`](../../tests/test_talk_served.py)). **None of it has met Revit or Claude
Code.** No Talk button has been seen, and no channel event has reached a real model.

**Before AS2, update Claude Code** and note its version: channels are a research preview, and the flag
the launcher passes may change.

| # | Check | Expected |
|---|---|---|
| **AS1** | Deploy the add-in to Revit 2024, open a test copy, and look at **Heron > AI Bridge** | A **Talk** button with the speech-bubble picture, enabled with a model open. **FAIL** - no button, or the whole tab missing - is the ribbon code: the add-in log names why |
| **AS2** | In the Heron folder, double-click `mcp\heron-talk.cmd` | Claude Code shows its development-channels warning naming `server:heron`; choose *I am using this for local development*. It then notes that messages from `server:heron` inject into the session, `/mcp` shows `heron` connected, and `%LOCALAPPDATA%\Heron\talk\listener-<pid>.json` exists and its time moves every few seconds. **FAIL** - no such note, or a *blocked by org policy* line - means Talk cannot work on this account yet: write down the exact words and the Claude Code version |
| **AS3** | **The negative.** Close that chat and start Claude the ordinary way. Connect Heron, select three ducts, press **Talk**, type *count these*, press Enter | The box stays open, says **no chat is listening, so nothing was sent**, and the words are still in it. `%LOCALAPPDATA%\Heron\talk\<Revit pid>` holds **no** `message-*.json`. **FAIL** - a message file written with nobody listening - would be a modeller's words lost in silence |
| **AS4** | **The positive.** The AS2 chat open, Heron connected, three ducts selected in the test copy. **Talk**: *how many are selected and what size are they* - Enter | The box closes at once and Revit is usable. Within about a second the terminal shows the message arriving from `heron`; Claude reads the saved selection with `heron_selection` and answers about **three ducts in this model**. The talk folder now holds `message-1.taken.json` and `selection-1.json`. **FAIL** - nothing arrives - is the channel; Claude answering about the live selection instead of the saved one is the instructions |
| **AS5** | **Revit does not wait, and "these" means what was sent.** Straight after an AS4-style message, click around Revit and select two walls instead. Then **Talk**: *select the ones I sent you* | Revit stayed responsive the whole time, apart from Heron's banner while a tool ran. The ducts from the message are selected - **not** the walls. **FAIL** - the walls, or Revit frozen until the answer - breaks the two things D-104 promised |
| **AS6** | In the Talk box, press **Win+H** and say *select all ducts on level one* | The words appear in the box, and Enter sends them. **Record either way**: the box is a transparent, borderless WPF window, and whether Windows voice typing writes into it has not been seen. Step 2 must not lean on Win+H if this fails |
| **AS7** | **Changes still guard the model.** With **Changes off**, **Talk**: *move these up 200* on three selected ducts | Heron refuses the change and Claude says so; nothing moved. With **Changes ON**, the same message moves them, and **one Ctrl+Z** puts them back. **FAIL** - a move with Changes off - would mean pre-allowing Heron's tools bypassed Heron's own gate, which D-104 says it must not |
| **AS8** | **Two Revits.** Two Revits open and connected; in the chat, choose the first with `revit_use_session`. Then **Talk** from the second | The message arrives with a note that it came from a different Revit from the one chosen, and Claude **asks** before sending anything to Revit. Then close both chats, start a fresh Talk chat, and Talk from the second Revit: the chat binds to the second, as chosen |
| **AS9** | Close Revit | `%LOCALAPPDATA%\Heron\talk\<that Revit's pid>` is gone, and `heron_selection` in the chat says nothing is saved from it |
| **AS10** | AS1, AS3 and AS4 on **Revit 2020** and **Revit 2027** | The same - the box is WPF on .NET Framework 4.7.2 at one end and .NET 10 at the other |
