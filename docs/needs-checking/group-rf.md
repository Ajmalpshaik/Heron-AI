# Needs checking — Group RF

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-11-28 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group RF - Heron's jobs as Claude Code skills, in a real chat on the owner's Windows PC (2026-11-28)

**Tracked as GitHub issue [#1523](https://github.com/Ajmalpshaik/Heron-AI/issues/1523) since 2026-11-28** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** [`tools/build-plugin.py`](../../tools/build-plugin.py) now writes `skills/<job>/SKILL.md` for each job
`heron_method` lists - item 4 of [D-210](../decisions/D-210.md), section 10 of
[the design](../work-notes/plans/claude-code-plugin-2026-11-27.md). Each skill only sends Claude to `heron_method` with
its job id, once, before the first step; the method itself stays in `brain/skills`.

**What is done and what is not.** Run on 2026-11-28 in a Linux cloud container, Claude Code **2.1.332**, with
`claude --plugin-dir <build>/heron-plugin` from a fresh build: the session listed one `heron:<job>` skill per job;
the server connected as `plugin:heron:heron`; *"I want to lay out the air terminals in this room"* and
`/heron:terminal-layout` each made `mcp__plugin_heron_heron__heron_method` with `job` `terminal-layout` the first
call; `/heron:terminal-layout` quoted the skill's first sentence word for word; `/heron:no-such-job` loaded nothing.
`claude plugin validate` passed, plain and `--strict`. **Never run on Windows, and never from an install** - a
`--plugin-dir` session is not a marketplace install, and the container had no user-scope `heron` beside it.

**The machine:** the owner's Windows PC, Claude Code, a fresh `python tools/build-plugin.py` in his checkout, and a
chat opened in a folder that is **not** the checkout. No Revit is needed. His user-scope `heron` entry puts a second
Heron beside the plugin (section 10 of the design): note which server answered each call.

| ID | Do this | Pass looks like |
|---|---|---|
| **RF1** | In that folder: `claude --plugin-dir <checkout>\dist\heron-plugin`, then type `/heron:` | One `heron:<job>` entry per job `python tools/build-plugin.py --list` counts, and `/mcp` shows `plugin:heron:heron` connected. A FAIL - none listed, or the server not connected - proves Windows loads a plugin folder differently from the container |
| **RF2** | In the same chat: *"lay out the air terminals in this room"*, then a new chat with `/heron:terminal-layout` | Each time the first Heron call is `heron_method` with `job` `terminal-layout`, before any `revit_` call, and no file in a Heron folder is read to work the job out |
