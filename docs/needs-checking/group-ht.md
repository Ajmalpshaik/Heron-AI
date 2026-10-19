# Needs checking — Group HT

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-19 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group HT - a material, a level or a family type by name, and a type by key: the three parameter writers (2026-10-19)

From [rows 5b-1242 and 5b-1243](../fragment-issues/section-5b-rows-401-425.md). On Revit 2020 the owner's
*Kerala house design* could not give its two `M_Round Column` instances a material: every parameter
writer refused, or passed to `SetValueString`, a parameter that holds another element, and a key row
could not reach a type.

**What changed.** `WRITE_ELEMENT_PARAMETERS` v5, `IMPORT_PARAMETER_VALUES` v5 and
`SET_PARAMETER_VALUES_BY_ID` v4 take a NAME for a parameter holding another element - a material or a
level by Name, a family type as `Family: Type` - matched exactly, refused by name when it matches none
or two, and the id Revit holds afterwards read back. `SET_PARAMETER_VALUES_BY_ID` takes `types=yes` on
its key row to look among types.

**What is done and what is not.** All three compile for 2020 to 2027
(`tools/check-fragments-compile.py`). **Nothing here has run in Revit.** The three cards are DRAFT
until HT1 to HT6 pass and the owner signs; their earlier proofs are kept on the cards as records.
Run on a **scratch copy** of a model on **Revit 2020** with `M_Round Column : 305mm Diameter` placed
twice (Material `<By Category>`), a material whose exact name you note (for example
`TRG_Material_Teak_Wood`), two walls on Level 1 and a Level 2 - never a working model. Every run is one
entry in Revit's undo list; Ctrl+Z after each puts it back.

| # | Run | Look for |
|---|---|---|
| **HT1** | *Positive - `WRITE_ELEMENT_PARAMETERS`.* The column TYPE as the element, `parameterName` Material, `value` the material's exact name. Then the two walls, `Base Constraint`, `Level 2` | `written` 1, `refused` empty; the type's Material in Properties is that material and both columns render in it. Then `written` 2 and both walls' Base Constraint reads Level 2 |
| **HT2** | *Negative - the same writer.* The type, Material, `No Such Material`. Then the type, Material, the name of a **level** | `written` 0, the type in `refused`, one finding naming the value and *"no material in this model is called"*; the type keeps `<By Category>`. The level's name is refused the same way (a Material parameter names materials only). **FAIL:** any change to the type |
| **HT3** | *`IMPORT_PARAMETER_VALUES` - the measured case.* A CSV `Id,Material` with rows `346587,<material>` and `346588,<material>` (the two columns' own ids), `includeTypeParameters` true, `blankCells` skip. Then the same file with `No Such Material` | First: `valuesWritten` 1 (both rows name the same type - the later stands), `valuesRejected` 0, the type's Material is the material. Second: `valuesWritten` 0, the row's line says *"no material in this model is called 'No Such Material'"* - never a bare *"Revit would not take"* |
| **HT4** | *`SET_PARAMETER_VALUES_BY_ID` - a type by key.* `rows` `key=Type Name\|in=Columns\|types=yes;305mm Diameter\|Material=<material>` | `written` 1; `resultJson` format 2 with `"types": true`, `refused` [], its readBack value the material's name and raw its id; both columns show the material. **Say** if `in=Columns` must be `Structural Columns` for this family - the measured run used Columns |
| **HT5** | *Negative - no types=yes.* The same rows without `\|types=yes` | `written` 0; `refused` names `305mm Diameter` and says a type holds it - *"add types=yes"*. Then `types=maybe`: nothing written, a finding *"types='maybe' is not yes or no"* |
| **HT6** | *Negative - a name for a type by key.* `key=Type Name\|in=Columns\|types=yes;305mm Diameter\|Material=No Such Material` | `written` 0; `refused` says *"no material in this model is called 'No Such Material'"*; the type keeps its Material |

**Then sign** `write-element-parameters` on HT1 and HT2, `import-parameter-values` on HT3, and
`set-parameter-values-by-id` on HT4 and HT5. HT6 is the case its `tests/cases.yaml` names beside them.
