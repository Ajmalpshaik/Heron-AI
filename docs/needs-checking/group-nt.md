# Needs checking — Group NT

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group NT - a dry run from the chat: `revit_change` with `dry_run` (2026-11-12)

[D-155](../decisions/D-155.md) lets a chat ask `revit_change` for the run itself, rolled back, with nothing
kept. The add-in's half is D-55's and is unchanged; what is new is the chat's door sending it. **Tested
only against a stand-in for Revit** (`tests/test_mcp_serves.py`, section 13), whose new checks were seen to
fail on the code before the change. **None of it has run in Revit.**

**Repaired the same day, after an independent review** ([row 5b-1591](../fragment-issues/row-5b-1591.md),
[row 5b-1592](../fragment-issues/row-5b-1592.md), [row 5b-1593](../fragment-issues/row-5b-1593.md) and
[row 5b-1602](../fragment-issues/row-5b-1602.md)): the reply says *rolled back* only on the add-in's own
verdict that its rollback held, it carries an `outcome:` word, and a capability that acts outside a
transaction is refused as a dry run. Each row below now names the **word its `outcome:` line must
carry**, on the reply's second line ([docs/04 §8.12](../04-heron-mcp.md)), and NT7 to NT10 are new. Built
and held by `tests/test_outcome_line.py` section 3c, **not run in Revit**.

**The model:** a saved scratch project copied for the purpose - never the owner's own work. Changes ON,
*Ask to Keep* in each state where a row says so.

| ID | Do this | Pass looks like |
|---|---|---|
| **NT1** | In a chat, ask for a PROVEN MODIFY change as a dry run - e.g. rename the views on one level - with *Ask to Keep* OFF | The reply opens `DRY RUN: ... rolled back`, its second line is `outcome: ROLLED BACK`, it gives Revit's counts, and the add-in's verdict says the rollback held. It says a dry run is not approval. **The model is unchanged**: the views keep their names, and Revit's undo list has no new `Heron:` entry |
| **NT2** | The same with *Ask to Keep* ON | **No Keep or Discard box appears** - a run that keeps nothing is never asked (D-126). Same reply, the same `outcome: ROLLED BACK` and the same unchanged model as NT1 |
| **NT3** | After NT1, ask the chat to keep it | A second, ordinary run: it sends `apply`, the box appears when *Ask to Keep* is ON, and Keep leaves ONE `Heron:` entry in the undo list. Its counts are its own |
| **NT4** | *The negative:* ask for a dry run of a PROVEN Publish or Admin capability | Refused in the chat with `outcome: REFUSED`, nothing reaches Revit - the banner never shows, no file is written, nothing is in the activity record for Revit |
| **NT5** | Dry run with Changes OFF | Refused by the add-in exactly as a kept change is, `outcome: REFUSED`, naming the Changes switch - the dry run is not a way round it, and the reply says nothing of a rollback |
| **NT6** | Dry run of a change that deletes elements | Nothing is deleted, and **the delete box (D-142) is not shown** - it is asked only for a run that would keep ([D-142](../decisions/D-142.md): *"A preview is never asked"*; the add-in's `DeleteGate` returns at once for a run sent without `apply`). `outcome: ROLLED BACK` |
| **NT7** | *A rollback that does not hold - what it looks like, since it cannot be made to order.* [Section 1c](../fragment-issues/section-1c.md) holds three, none repeatable on purpose. **Before every dry run in this group, note the model's element count** (or NT1's view names). If any dry run answers this way, stop and record it | The reply opens `DRY RUN NOT CONFIRMED: ... did not say its rollback held`, its second line is `outcome: OUTCOME UNKNOWN`, the add-in's verdict - *THE ROLLBACK DID NOT HOLD* or *DID NOT REPORT SUCCESS* - is printed whole, and **nothing says the model is as it was or how to keep the change**. Do not save. Record the model, the capability, the element count before and after, and `rollbackCheck` from the run record (`heron_run_record`). *The negative, which NT1 is:* a rollback that held shows none of those words |
| **NT8** | *The negative for what a rollback cannot take back:* with a plan view in front, ask for `OPEN_VIEW` (`open-view`: EXECUTE, `noTransactionOpen`) as a dry run, naming another view. Then `OPEN_FAMILY_FOR_EDITING` as a dry run, naming a loaded family | Both refused in the chat, `outcome: REFUSED`, *Nothing has been sent to Revit*. **The view in front has not changed and no tab opened; no family document opened and no `.rfa` was written.** The first refusal names `revit_show`; the second does not - that door refuses it too ([row 5b-591](../fragment-issues/section-5b-rows-401-425.md)). *The positive beside it:* the same view asked through `revit_show` opens, with Changes OFF |
| **NT9** | *A workshared model - not known.* On a local copy of a scratch central, with an element nobody has borrowed: a dry run of a change to it. Then look at who owns it - Revit's Worksharing Display, or whether *Relinquish All Mine* has anything to give back | **Not known, and this row is what finds out:** whether the element is still borrowed by this user after the rollback. If it is, *"the model is as it was"* is untrue of ownership and the reply must say so - a 5b row is owed. *The negative:* the same dry run on an element another user owns reports it skipped and never asks for it (Article 12) |
| **NT10** | *Ask to Keep, both ways, with what `revit_health` says first.* Ask `revit_health`; then a dry run and, on the modeller's word, the kept run - once with the switch OFF and once ON | `revit_health`'s line for that Revit names a dry run among what is *Different for:* - rolled back, never kept, never asked about. **OFF:** no box for either run; the dry run leaves no `Heron:` undo entry and the kept run leaves ONE, `outcome: KEPT`. **ON:** no box for the dry run; the box for the kept run, and Discard there leaves nothing, `outcome: ROLLED BACK`. *The negative:* a dry run never answers `KEPT`, with the switch either way |
