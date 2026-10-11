# Needs checking — Group CL

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone.
> **The register's rules, and every group's place in it, are on that page.** A new row for this group
> goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads
> it back into the register for every tool that reads the register.

## Group CL - family types renamed and deleted in the Family Editor (2026-10-06)

`SET_FAMILY_TYPE_VALUES` version 3 renames a family's TYPE - `"Type Name=HexNut - ISO 4032"` in its
`values`, the way the Family Types dialog's Rename sits beside its rows - and deletes one with the
optional switch `deleteType`. Before it, the type `Standard` in `GM_PipeSupport_HexNut` had to be renamed
by hand ([row 5b-343](../fragment-issues/section-5b-rows-326-350.md)). The new name rides in `values`
because the binder refuses an absent text value (HeronBindingNote.AbsentValue); a separate `renameTo`
would have stopped every existing call - chosen by Ajmal PS on 2026-10-06. The change is under `impl/`, so
the version 2 proof went stale; **signed by Ajmal PS on 2026-10-06** on the CL1/CL2 record below, and PROVEN again.

**Run on 2026-10-06, rolled back**, on the scratch family *Family14* (Revit 2024, session 51820, unsaved,
made with `CREATE_FAMILY_DOCUMENT` from *Metric Generic Model.rft*), never on the owner's
`GM_PipeSupport_*` files. The arrangement - two types, `Standard` and `T2`, each with a URL - was made KEPT
in that scratch family with version 3 itself, and every run below rolled back.

| # | Run | Look for |
|---|---|---|
| **CL1** | `validate --write --set "typeName=Standard" --set "values=Type Name=Test - A" --negative-set "typeName=Standard" --negative-set "values=Type Name=t2" --in "Family14" --negative-in "Family14" set-family-type-values` | **RAN 2026-10-06:** `typeUsed Test - A`, `typesNow "T2", "Test - A"` - the type renamed, no `Standard` left; the group ROLLED BACK |
| **CL2** | the negative half of CL1: `Type Name=t2` - a name `T2` already has, in another case | **RAN 2026-10-06:** refused - *The family already has a type "T2", and Revit does not tell type names apart by case* - `typesNow "T2", "Standard"`, nothing renamed |
| **CL3** | `typeName=T2`, `values=` (empty), `deleteType=true` | **RAN 2026-10-06, rolled back:** `typesNow "Standard"`, `typeUsed Standard` - T2 gone and Standard current |
| **CL4** | `values=Type Name=Nut [M12]` | **RAN 2026-10-06, rolled back:** refused naming `[ ]`, nothing renamed |
| **CL5** | `deleteType=true` with `values=URL=x` | **RAN 2026-10-06, rolled back:** refused - a delete is done alone, nothing changed |
| **CL6** | a rename of a type that does not exist | **RAN 2026-10-06** (no-transaction run, before the arrangement): refused - *a rename never makes one - the family's types are none* |
| **CL7** | Family14 after CL1 to CL5 | **RAN 2026-10-06:** `typesNow "Standard", "T2"` - every run rolled back |
| **CL8** | the signature | **DONE 2026-10-06:** `heron_validate.py accept set-family-type-values --by "Ajmal PS"` on the CL1/CL2 record, on the owner's word - PROVEN |
| **CL9** | `deleteType=true` on a family's ONLY type | refused, nothing deleted. **Not run** - a one-type family was not arranged |
| **CL10** | a family with its OWN parameter called `Type Name`, then `Type Name=x` | refused as ambiguous. **Not run** |
| **CL11** | CL1 on **Revit 2020** and **2027** | The same answers. `RenameCurrentType` and `DeleteCurrentType` read the same 2020 to 2027 (api-surface) and compile on all eight releases; not run there |

**The routing check ranks it #2** for *rename the family type* (behind `RENAME_FAMILY`) and *delete this
family type* (behind `DELETE_FAMILY_PARAMETERS`) on nearness alone, but `heron_retrieve.py` picks it first
for both - and for *rename type Standard to HexNut - ISO 4032* - because each is a declared utterance. The
two neighbouring cards name it in their routing notes.
