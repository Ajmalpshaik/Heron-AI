# Needs checking — Group EU

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group EU - Heron says when its own code changed under a chat, and changes nothing until the chat is reopened (2026-10-17)

The MCP server is one long-running process per chat, so a `git pull` changes its files and not the code it
runs. On 2026-09-07 that had `revit_apply_move` answering *"There is nothing waiting to be approved"* from
code a fix had already replaced, and [B8](group-b.md) had to be proved from a fresh process. Nothing in the
chat said so.

**What changed.** [`heron_running_code.py`](../../mcp/server/heron_running_code.py) fingerprints every module
the server has loaded from `mcp/`, `brain/` and `tools/` - size, mtime and a SHA-256, from the moment each is
first seen - and checks them with one stat per file on every tool call. While any file's bytes differ from
what was loaded, every answer ends with one line: *"Heron's code changed since this chat started, so this
chat is still running the old code - close and reopen the chat to load it. Until then Heron will not change
the model."* A read still answers in full. **A tool that changes the model is refused** - which tools is
`heron_tools.writes()`, so `revit_apply_move` and `revit_change` today - and so is the Companion's Apply;
each says *"Nothing has been sent to Revit."* The reasons for refusing an approve are in that file's
docstring. A touch, or a checkout that rewrites a file with the same bytes, is not a change.

**What is tested and what is not run.** [`tests/test_running_code.py`](../../tests/test_running_code.py)
holds the watch on a scratch checkout, with no SDK: it changes a file's bytes and mtime after the first look
and the file is named; a touch is not; bytes put back are not; nothing is read again while no stat moves.
[`tests/test_mcp_serves.py`](../../tests/test_mcp_serves.py) section 16 drives the real server through the
SDK's dispatch against a stand-in Revit: the line on a preview, and `revit_apply_move` with an approval
waiting, `revit_change` and the Companion's Apply each refused with nothing sent. Both were seen to FAIL on
the code before this change - on it, the stand-in received `move_elements`. **Linux only. It has never run
in a real chat, on Windows, or against a real Revit.**

**A SCRATCH PROJECT, never a working model.** Restart the Claude app first so it starts a server with this
code in it. EU2 needs `write.enabled = true` and Changes ON.

| # | Run | Look for |
|---|---|---|
| **EU1** | In a chat, ask *"what levels are there"*. Then, with the chat still open, change one of Heron's files the way a pull would - add a blank line at the end of `mcp/server/heron_write.py` and save - and ask again | **The D-30 positive.** The second answer lists the levels in full, then the one line about the code changing, once. The first answer has no such line |
| **EU2** | The same chat: *"move the ducts up 200 mm"*, then say yes | The preview shows, with the line after it. The yes is **refused**, saying nothing was sent to Revit, and Revit's undo list has no Heron entry |
| **EU3** | Put the file back - `git checkout -- mcp/server/heron_write.py` - and ask *"what levels are there"* in the same chat | **The D-30 negative.** No line: the bytes are the ones the chat loaded. Then close and reopen the chat, and EU2's move previews and applies as [Group D](group-d.md) describes |
| **EU4** | A fresh chat after `git pull` and nothing else changed by hand | No line on any answer - the watch starts from what the new chat loaded, and Windows' file times and path spellings do not make it cry wolf |
