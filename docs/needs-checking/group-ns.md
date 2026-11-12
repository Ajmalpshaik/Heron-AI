# Needs checking — Group NS

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group NS - a write's values before and after: `readBack` (2026-11-12)

[D-156](../decisions/D-156.md) puts each changed element's old and new values on a write's reply. **It
compiles for 2020 to 2027, every Revit member it calls exists in every release
(`tools/check-api-surface.py`), and the comparing runs without Revit** (`tests/test_read_back.py`, whose
checks were seen to fail on the code before the change). **None of it has run in Revit.**

**The model:** a saved scratch project copied for the purpose - never the owner's own work. Changes ON.

| ID | Do this | Pass looks like |
|---|---|---|
| **NS1** | Select three ducts, then ask for a PROVEN change that raises their offset - a **dry run** first (D-155) | `before -> after: 3 of 3`, each duct's offset old -> new in the project's units, and `from the dry run`. The model is unchanged afterwards |
| **NS2** | The same change, **kept** | The same three lines, `mode` selection. Any other element Revit counted - a fitting the ducts are joined to - is counted *not read*, with the sentence saying a dry run shows them |
| **NS3** | *The negative:* a dry run of a change that touches one element, on a model where nothing else moves | Only that element is listed. **No other element shows a difference** - if one does with no real change, a parameter's text moves by itself and D-156's "What is not known" is answered the bad way |
| **NS4** | Move a wall that has a hosted door, as a dry run | The wall's `Location` old -> new. The door, if Revit counts it, is either listed with its own moved location or counted *no difference ... hosted or joined* - never silently left out |
| **NS5** | A dry run that changes 50 or more elements on a large model | The reply still comes back; note how much longer it took than the same run before this change. 50 read, the rest counted *not read* |
| **NS6** | Discard a change in the Keep or Discard box (*Ask to Keep* ON) | **No read-back** on the reply - the Discard's own verdict only |
