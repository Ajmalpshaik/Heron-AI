# mcp/companion/ — the Heron Companion page

**A page in the modeller's browser, beside Revit.** Allowed by [D-108](../../docs/DECISIONS.md); designed
in [docs/40](../../docs/40-heron-companion.md). The chat stays the only place to talk to Heron.

| | |
|---|---|
| Runs | inside the chat's MCP server process, started when the chat calls `heron_companion` |
| Listens | `127.0.0.1` only, on a port Windows picks — never on the network |
| Reads | the add-in's live file, `%LOCALAPPDATA%\Heron\live\pid-N.json` (written by `revit/Heron.Revit.Addin/HeronLiveState.cs` while the Heron button is connected) |
| Changes | **only through the chat's own write path**: the page's Apply runs `revit_change`'s one body (`_change`) under the lock every tool call holds - the same gate, Changes switch, pin and single undo entry. Its risk is declared in `heron_tools.COMPANION_ACTIONS` |
| Lives | as long as the chat that opened it |

## What's here

| | Does |
|---|---|
| [`heron_companion.py`](heron_companion.py) | The server: pairing, the protection rules, which Revit to show, the keeper that follows the Companion switch (D-109), the activity list, the after-change tables and the element table, and the Loads panel (docs/44) |
| [`static/index.html`](static/index.html), [`static/companion.js`](static/companion.js), [`static/companion.css`](static/companion.css), [`static/loads3d.js`](static/loads3d.js) | The page, and the Loads panel's 3D view - a renderer of its own. Nothing is loaded from the internet |

## Rules for this folder

1. **Never talk to an AI.** No outgoing connection of any kind; the page is served to, and reads from,
   this PC only. `tests/test_companion.py` fails on an import that could.
2. **Never print.** This code runs inside the process that speaks MCP to Claude Code over stdout; one byte
   there breaks the chat. The test watches stdout for a whole run.
3. **Never ask Revit through the pipe to keep the page fresh.** It would raise the READING banner, queue
   work on Revit's own thread and cut the chat's connection ([docs/40 §4.2](../../docs/40-heron-companion.md)).
   Live state comes from the file.
4. **No BIM logic.** What a column or a colour means belongs in `brain/` fragments; this folder moves rows.
5. **Model text is data.** Names reach the page through `textContent`, never as HTML (Golden Rule 19).
6. **The one-time address never goes in a reply to the chat** — only to the browser.

## Fix things here when

The page will not open · it shows the wrong Revit · it says "not opened from the chat" · it stops updating.
