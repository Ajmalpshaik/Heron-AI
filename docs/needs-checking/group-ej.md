# Needs checking — Group EJ

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group EJ - Heron says it is still waiting for Revit: MCP progress notes during a long tool call (2026-10-17)

Until 2026-10-17 a Heron tool waiting on Revit said nothing until Revit answered. A slow fragment, a family
build or a modeller answering a prompt in Revit can take minutes, and in that time the chat could not tell
a wait from a hang. [docs/04 §7.1](../04-heron-mcp.md#71-the-first-half-is-built--heron-says-it-is-still-waiting-2026-10-17)
has what was built.

**What changed.** `revit_change`, `revit_read`, `revit_building_loads`, `revit_sprinkler_hydraulics` and
`revit_sprinkler_layout` take the MCP SDK's `Context`, hidden from their arguments. While one of them waits
on Revit, the bridge client's join loop calls back every `WAIT_NOTE_S`
([`heron_bridge_client.py`](../../mcp/client/heron_bridge_client.py)), and the server sends an MCP progress
notification saying *"Waiting for Revit - N s so far. The modeller may be answering a prompt in Revit."* -
**only when the host sent a `progressToken`**. Nothing goes down the pipe: the add-in is unchanged and needs
no rebuild.

**What is tested and what is not run.** [`tests/test_mcp_stdio.py`](../../tests/test_mcp_stdio.py) runs the
real server over stdio against [`tests/support/slow_revit_server.py`](../../tests/support/slow_revit_server.py),
a stand-in Revit that takes the test's `SLOW_S` to answer: a host that sent a token gets a note every
`WAIT_NOTE_S`, with its token, rising progress and the words, and none after the answer; a host that sent
none gets nothing. Measured on Linux on 2026-10-17, SDK 2.2.0: a 90-second wait gave 8 notes, 10.0 s
apart, and the host with no token got none in 30 s. **It has never run in a real chat or against a real Revit**, and Claude
Code's docs do not say whether it sends a `progressToken` at all
([WHAT-TO-INSTALL §5a](../WHAT-TO-INSTALL.md#5a-when-a-revit-job-runs-long--claude-codes-own-limits)).

**A SCRATCH PROJECT, never a working model.** Any project with a few MEP Spaces, saved as *Heron Wait
Check.rvt*, in Revit 2020 first. Restart the Claude app first so it starts the new server.

| # | Run | Look for |
|---|---|---|
| **EJ1** | In a chat, a `revit_read` that takes longer than `WAIT_NOTE_S` - `revit_building_loads` on the scratch project, or a fragment's first run in a fresh Revit session, which compiles it | **The D-30 positive.** While it waits, Claude Code shows *"Waiting for Revit - N s so far ..."* on the tool line, N rising. Write down whether it showed at all: if nothing showed, Claude Code sent no `progressToken` and Heron rightly sent nothing - say so here, and that is the answer to WHAT-TO-INSTALL §5a's open question |
| **EJ2** | The same chat: `heron_lookup` on any sentence, and a `revit_levels` | **The D-30 negative.** No note - neither tool carries the callback, and both answer at once |
| **EJ3** | `revit_change` on a scratch-model change that shows a prompt in Revit, left unanswered for over a minute, then answered | Notes the whole time the prompt is open, then the change's own answer, and nothing after it. Undo the change |
| **EJ4** | EJ1 with the call running past two minutes | Claude Code moves it to a background task; `/tasks` shows the latest note; the answer still arrives |
| **EJ5** | EJ1 once on Revit 2024 and once on 2027 | The same. The notes are made outside Revit, so the release should not matter |
