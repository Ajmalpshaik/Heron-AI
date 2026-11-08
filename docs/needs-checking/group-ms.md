# Needs checking — Group MS

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group MS - saved commands and the Heron panel: a kept job saved from the chat and run again from a panel docked in Revit (2026-11-08)

**What was built** ([D-151](../decisions/D-151.md), proposed; [docs/49](../49-saved-commands-and-the-heron-panel.md)):
the chat's `heron_commands` saves a job as a named command; **Commands** in the Heron button's menu opens a
docked panel listing them; Run starts `mcp/server/heron_command_run.py`, which runs the command through
`revit_change`'s own body. **Compiled for 2020 to 2027 and tested without Revit - never run in a real Revit.**

**The model is a test copy.** Revit closed for the deploy; the Claude app restarted so the chat loads the
new tool and writes the runner note. Changes ON in Settings for MS3.

| ID | Do this | Pass looks like |
|---|---|---|
| **MS1** | Revit 2024, Heron tab → the Heron button's arrow → **Commands** | A panel docks on the right, titled *Heron Commands*, saying no commands are saved yet. **Negative:** press Commands again - the panel hides |
| **MS2** | Panel open. In the chat, run a PROVEN change you keep, then say *"save that as Test Rename, ask me the name each time"* | The chat says *Saved*, and **within about a second, without restarting Revit**, *Test Rename* appears in the panel with a box for the name, filled with its default. **Negative:** ask the chat to save a sync with central as a command - it refuses and the panel does not change |
| **MS3** | Heron connected, Changes ON. Pick *Test Rename*, type a new name, press **Run** | Revit shows the change and asks **Keep or Discard**; Keep, and the panel shows Heron's answer naming the model. One Ctrl+Z takes the whole change back. **Negative:** disconnect Heron and Run again - the panel says this Revit is not connected and the model is unchanged |
| **MS4** | While a chat is in the middle of a job on the same model, press Run | The panel shows the refusal in words - the model is in use by another chat - and nothing changes. **Negative:** after the chat finishes (or *Free this model*), Run works |
| **MS5** | The same as MS1 to MS3 in **Revit 2020** and **Revit 2025** | The same answers on .NET Framework and on .NET 8 |
