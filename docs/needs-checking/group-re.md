# Needs checking — Group RE

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-11-28 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group RE - Heron's slash commands in a real Claude Code chat: its MCP prompts listed under `/`, one run, its one-word argument passed (D-210 item 3) - on the owner's Windows PC, no Revit needed - BUILT, NOT SEEN IN A CHAT (2026-11-28)

**Tracked as GitHub issue [#1521](https://github.com/Ajmalpshaik/Heron-AI/issues/1521) since 2026-11-28** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** part P6 of the Claude Code plugin ([D-210](../decisions/D-210.md) item 3) gives Heron's server MCP prompts -
commands a modeller picks from the `/` list to start a common job. They are `PROMPTS` in
[`heron_prompts.py`](../../mcp/server/heron_prompts.py), registered at the foot of
[`heron_mcp_server.py`](../../mcp/server/heron_mcp_server.py), and [04 §8.13](../04-heron-mcp.md) says what each is.
`python mcp/server/heron_prompts.py` prints them - on 2026-11-28 `check-model`, `build-family` (one optional
argument, `family`, one word), `revit-connected` and `what-heron-can-do`; where this line differs, the file is
right. Running one puts its text in the chat as the modeller's own turn, and the chat answers it.
[`tests/test_mcp_stdio.py`](../../tests/test_mcp_stdio.py) asks a real server for them over stdio, off Windows and
with no Revit: every prompt listed, each one's text with and without its argument, and a name it does not serve
refused (run 2026-11-28 in a Linux container, exit 0). **Seen headless the same day:** `claude -p` 2.1.332 in a
Linux cloud container, with `main` at `219f1928` given by `--mcp-config` under the user-scope entry's name, `heron`,
listed every prompt as
`mcp__heron__<prompt>`; `/mcp__heron__build-family door` ran as a command with `door` as its argument, the same
with nothing after it ran too, and `/mcp__heron__no-such-prompt` was answered as an ordinary message. But
`/heron:build-family door` was **not** run as a command there - it reached the chat as plain words - so the
`/heron:` form belongs to the interactive `/` menu, which a headless run has not got. **Under the plugin**, the
folder `tools/build-plugin.py` builds loaded with `claude -p --plugin-dir`, the server connected as
`plugin:heron:heron`, every prompt was listed as `mcp__plugin_heron_heron__<prompt>`, and
`/mcp__plugin_heron_heron__build-family door` ran with its argument - but only once the server had connected: sent
as the session's first message, while it was still starting, the same command was answered as not installed.
**Nothing here has been seen
in an interactive chat on the owner's PC** - the names its `/` menu shows, and whether one picked from it runs.
Claude Code's MCP page (*Use MCP prompts as commands*, read 2026-11-28) says a prompt of the user-scope `heron`
server is listed as `/heron:<prompt> (MCP)` and that `/mcp__heron__<prompt>` runs it too - the host's documents,
not a reading. Under the plugin the server is `plugin:heron:heron`, and the command's name there is not known.

**What it needs.** The owner's Windows PC, Claude Code, and a Heron that has this change - his own checkout,
brought up to date by him ([D-189](../decisions/D-189.md)) - in a **fresh** chat, so the server that starts serves
the prompts. No Revit and no model: every row stops at the chat's first answer, and nothing is built. If Revit
happens to be running, leave Changes off. Each prompt's text, to compare with what arrives, is printed by
`python -c "import sys; sys.path.insert(0, 'mcp/server'); import heron_prompts as p; print(p.render(p.job('build-family'), 'door'))"`
in the Heron folder - put the prompt's name in place of `build-family`, and leave out `, 'door'` for the text
with no argument.

**Stale when** `mcp/server/heron_prompts.py` changes after the run. With each result write down the date,
Claude Code's version (`claude --version`) and the commit `git log -1 --format=%h -- mcp/server/heron_prompts.py`
prints in the checkout the chat ran from; once a later commit changes that file, the results no longer hold and
the rows run again.

| ID | Do this | Pass looks like |
|---|---|---|
| **RE1** | *Listed, under today's user-scope `heron` server.* On the owner's PC, in a fresh chat, type `/` and read the list. Then type `/mcp__heron__what-heron-can-do` | Every prompt `python mcp/server/heron_prompts.py` prints is listed, each once, with its description. **Write down each command's name exactly as shown** - Claude Code's page says `/heron:<prompt> (MCP)` - and whether the `/mcp__heron__` form runs too. A prompt missing is the finding: the server left it out at start-up (it says *"Heron left out the prompt ..."* on its stderr) or the host never asked for the list - look at `/mcp` first. Names of another shape are a FAIL of [04 §8.13](../04-heron-mcp.md) and of `heron_prompts.py`'s own header, which both give the page's form |
| **RE2** | *One runs.* On the owner's PC, run `/heron:what-heron-can-do` - or that prompt by the name RE1 wrote down | Its text, as the command above prints it, arrives in the chat **as the modeller's own turn**, and the chat answers it. The command accepted and no text arriving, or the text shown as anything but the modeller's turn, is the FAIL - write down what Claude Code showed |
| **RE3** | *The one-word argument.* On the owner's PC, run `/heron:build-family door`; then, in a new chat, `/heron:build-family` with nothing after it. Stop at each chat's first answer | The first turn reads *"I want to build a new Revit door family. ..."* - `door` in the place of the argument - and the second is the text with no argument, which asks what the family is. `door` missing from the first is the FAIL: the argument was not passed. The second refused, or asked for the argument before it ran, means Claude Code treats the optional argument as required - write down its words |
| **RE4** | *Negative.* On the owner's PC, type `/heron:no-such-prompt` - a name that is none of Heron's prompts - and press Enter | It is not offered in the `/` list, and no Heron prompt's text arrives: Claude Code says it does not know the command, or sends the words as an ordinary message. Any Heron prompt's text arriving is the FAIL - a name that is not served was answered |
| **RE5** | *Listed under the plugin.* On a Windows PC where Heron is installed as the plugin - `/mcp` shows its server as `plugin:heron:heron` - type `/`, then run one prompt by the name shown | **Write down the exact command names the `/` menu shows** - a headless run listed `mcp__plugin_heron_heron__<prompt>`, but the menu's own form is not known - and that the one run behaves as RE2 says. Those names then replace *"a PC check, not a claim"* in [04 §8.13](../04-heron-mcp.md). **Waits on a plugin install:** nothing is published yet (D-210 part 1), and section 10 of [the plugin design](../work-notes/plans/claude-code-plugin-2026-11-27.md) keeps the plugin off the owner's PC while his user-scope `heron` entry stands |

**Not checked** by the session that wrote this group: how the plugin is put on a PC for RE5; whether Claude Code's
list shows a prompt's title or its name; where Claude Code shows a server's stderr; and whether a prompt's text
is sent at once or left in the input box to be sent. Nothing in any code changed with this group.
