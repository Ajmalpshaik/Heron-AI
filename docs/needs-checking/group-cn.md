# Needs checking — Group CN

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone.
> **The register's rules, and every group's place in it, are on that page.** A new row for this group
> goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads
> it back into the register for every tool that reads the register.

## Group CN - reload a family over the loaded one: LOAD_FAMILY version 2 (2026-10-06)

`LOAD_FAMILY` version 2 reloads a family that is already in the project or family document:
`reload=true` is Revit's *Overwrite the existing version*, and `reload=true` with `overwriteValues=true` is
*Overwrite the existing version and its parameter values*. Without `reload` a loaded family is still refused
by name. Built for [row 5b-347](../fragment-issues/section-5b-rows-176-200.md) under
[D-114](../decisions/D-114.md): the add-in supplies the ambient need `familyLoadOptions`, so **nothing here
can run until the add-in is redeployed** for every Revit release and Revit restarted.

**Compiled 2020 to 2027** (`tools/check-fragments-compile.py`, every fragment ok; the add-in built for all
eight releases). [`tests/test_fragment_ambient.py`](../../tests/test_fragment_ambient.py) holds the add-in
and the brain to the same ambient names and the `reload` switch to off-unless-typed. **Nothing has run in
Revit.** The proof is on scratch families made for it, never on the owner's `GM_PipeSupport_*` files.

| # | Run | Look for |
|---|---|---|
| **CN1** | Make scratch family A (one type, a length parameter) and save it; load it into scratch family B with `load-family --write` | **Not run.** `created` set, `reloaded` "no - ... loaded new", A's type in `typesAdded` |
| **CN2** | Edit A (change the parameter's value, add a second type), SAVE it; in B run `familyPath=<A> reload=true`, rolled back | **Not run.** `reloaded` "version", the old type in `typesKept`, the new type in `typesAdded`; the old type's value still B's (Overwrite the existing version keeps them) |
| **CN3** | CN2 with `overwriteValues=true` | **Not run.** `reloaded` "version and values"; the old type's value reads back as A's file |
| **CN4** | the negative: CN2's arrangement with `reload` left off | **Not run.** REFUSED naming A, saying reload=true would overwrite it; nothing loaded |
| **CN5** | `reload=true` with A unchanged since the last load | **Not run.** refused: same version, nothing to overwrite, SAVE an edit first. Whether Revit returns false or true here is what this row measures |
| **CN6** | A material in A sharing a name with one in B, a different colour | **Not run.** the material listed in `materialsChanged` as before -> after (the 2026-09-16 Copper case) |
| **CN7** | a SHARED nested family, reloaded into a host | **Not run.** `OnSharedFamilyFound` reached (`FamilySource.Family`), the file's version taken |
| **CN8** | the same into a PROJECT, then on Revit 2020 and 2027 | **Not run.** the same answers |
| **CN9** | the signature | **Owed** - on the owner's word only |
