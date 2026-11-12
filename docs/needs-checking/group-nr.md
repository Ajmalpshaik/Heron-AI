# Needs checking — Group NR

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group NR - a dry run from the chat: `revit_change` with `dry_run` (2026-11-12)

[D-155](../decisions/D-155.md) lets a chat ask `revit_change` for the run itself, rolled back, with nothing
kept. The add-in's half is D-55's and is unchanged; what is new is the chat's door sending it. **Tested
only against a stand-in for Revit** (`tests/test_mcp_serves.py`, section 13), whose new checks were seen to
fail on the code before the change. **None of it has run in Revit.**

**The model:** a saved scratch project copied for the purpose - never the owner's own work. Changes ON,
*Ask to Keep* in each state where a row says so.

| ID | Do this | Pass looks like |
|---|---|---|
| **NR1** | In a chat, ask for a PROVEN MODIFY change as a dry run - e.g. rename the views on one level - with *Ask to Keep* OFF | The reply opens `DRY RUN: ... rolled back`, gives Revit's counts, and the add-in's verdict says the rollback held. **The model is unchanged**: the views keep their names, and Revit's undo list has no new `Heron:` entry |
| **NR2** | The same with *Ask to Keep* ON | **No Keep or Discard box appears** - a run that keeps nothing is never asked (D-126). Same reply and the same unchanged model as NR1 |
| **NR3** | After NR1, ask the chat to keep it | A second, ordinary run: it sends `apply`, the box appears when *Ask to Keep* is ON, and Keep leaves ONE `Heron:` entry in the undo list. Its counts are its own |
| **NR4** | *The negative:* ask for a dry run of a PROVEN Publish or Admin capability | Refused in the chat, nothing reaches Revit - the banner never shows, no file is written, nothing is in the activity record for Revit |
| **NR5** | Dry run with Changes OFF | Refused by the add-in exactly as a kept change is - the dry run is not a way round the switch |
| **NR6** | Dry run of a change that deletes elements | Nothing is deleted, and **the delete box (D-142) is not shown** - it is asked only for a run that would keep |
