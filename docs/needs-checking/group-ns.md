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

**Repaired the same day, after an independent review** ([row 5b-1595](../fragment-issues/row-5b-1595.md),
[row 5b-1597](../fragment-issues/row-5b-1597.md), [row 5b-1598](../fragment-issues/row-5b-1598.md) and
[row 5b-1599](../fragment-issues/row-5b-1599.md)): one value is one line of the reply whatever it holds, a
read-back Heron cannot read costs one line and never the reply, the words claim only what was read, and
two parameters with one name are paired by their own ids. NS2 and NS4 below say what the reply says now,
and NS7 to NS11 are new. The chat's half is run by `tests/test_read_back.py` section 9 and the pairing by
`tests/Heron.ReadBack.TestHost`; **none of it has run in Revit**, and an add-in built before the repair
pairs by read order still.

**The model:** a saved scratch project copied for the purpose - never the owner's own work. Changes ON.

| ID | Do this | Pass looks like |
|---|---|---|
| **NS1** | Select three ducts, then ask for a PROVEN change that raises their offset - a **dry run** first (D-155) | `before -> after: 3 of 3`, each duct's offset old -> new in the project's units, and `from the dry run`. The model is unchanged afterwards |
| **NS2** | The same change, **kept** | The same three lines, `mode` selection. Any other element Revit counted - a fitting the ducts are joined to - is counted *not read*, with the sentence saying why: *a kept change reads only what was selected before it ran*, and the limit. **No line sends the chat to run it again as a dry run** |
| **NS3** | *The negative:* a dry run of a change that touches one element, on a model where nothing else moves | Only that element is listed. **No other element shows a difference** - if one does with no real change, a parameter's text moves by itself and D-156's "What is not known" is answered the bad way |
| **NS4** | Move a wall that has a hosted door, as a dry run | The wall's `Location` old -> new. The door, if Revit counts it, is either listed with its own moved location or counted *no difference in any value Heron reads* - never silently left out, and with no cause given for it |
| **NS5** | A dry run that changes 50 or more elements on a large model | The reply still comes back; note how much longer it took than the same run before this change. 50 read, the rest counted *not read* |
| **NS6** | Discard a change in the Keep or Discard box (*Ask to Keep* ON) | **No read-back** on the reply - the Discard's own verdict only |
| **NS7** | *A text value of several lines.* Give one element a Multiline Text parameter - or paste two lines into its Comments - and run a dry run that changes that value. Then the same with a value holding a double quote and `->` | The value is on **ONE line** of the reply, inside its quotes, its line break written `\n` and its quote `\"`. **No line of the reply starts with any of the value's own words.** *The negative:* a one-line value reads exactly as Revit's Properties palette shows it |
| **NS8** | *Revit 2020 and 2021, where `AsValueString` is least known.* On each: a dry run that changes a text parameter (Comments), a Yes/No instance parameter, and a level parameter (a duct's Reference Level), on one element | Each shows old -> new in words a modeller knows - the text, `Yes`/`No`, the level's name - **or record exactly what it shows instead**: `1`/`0`, an id, or `""`. A text parameter that changed never reads `""` -> `""`. *The negative:* the two of the three parameters a second run does not change show no difference |
| **NS9** | *Two parameters with one name.* Add a project parameter named `Comments` to Ducts, beside Revit's own. A dry run that changes ONLY the project parameter's value, run three times | **Exactly one value differs each time, and it is the same one each time** - shown under the same name, `Comments` or `Comments (2)`, on all three runs. *The negative:* a dry run that changes neither shows no difference under either name. If the name the changed one is shown under moves between runs, the pairing by id did not hold - a 5b row is owed |
| **NS10** | *An imperial project.* NS1 on a project in feet and fractional inches | The offset reads as the project shows it - `7' - 0"` - with its inch mark written `\"` inside the quotes, on one line. `Location` is still in millimetres: that one is Heron's own, and the reply says `mm`. *The negative:* a duct whose offset did not change shows no difference - a length that rounds differently before and after would show one |
| **NS11** | *A large selection, for the time it costs.* Select 500 elements or more on a large model and keep a change to them; then the same change with nothing selected | The reply comes back both times. **Note the seconds each took**, against the same change on an add-in from before D-156 if one is to hand. `read` is the add-in's limit (`HeronReadBack.MostRead`), the rest are counted *not read* with that limit said. *The negative:* with nothing selected the read-back is ONE line saying none of them was selected |
