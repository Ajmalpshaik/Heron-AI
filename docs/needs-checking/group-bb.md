# Needs checking — Group BB

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-27 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group BB - `write-element-parameters` version 4: a Yes/No parameter ticked or unticked (2026-09-27)

**One fragment edited, not added, and it is DRAFT.**
[`write-element-parameters`](../../brain/fragments/write-element-parameters/fragment.yaml) -
WRITE_ELEMENT_PARAMETERS, FRG-PAR-003 - refused every Yes/No parameter until version 4: on 2026-09-27, in
"heron ai bulding" (Revit 2024, Heron session 46596), `Show_Clearance` on the 15 fan coil units
`TRG_MECH_EQP_Fan Coil Unit_FCU_R0 : FCU-01` (ids 930673-930687) came back `refused` 15, `written` 0, with
`value=No` and again with `value=0` ([row 5b-247](../FRAGMENT-ISSUES.md#)). Version 4 finds a tick box first,
takes Yes/No, True/False, On/Off, 1/0 or Ticked/Unticked, writes 1 or 0, reads it back with `AsInteger`, and
counts an element that already held the value in `alreadyThat`. Anything else is refused and `findings` names
the value and the words that are taken.

**What is already known, and it is NOT a proof.** Every fragment compiles on 2020 to 2027. The Yes/No test
asks `Definition.GetDataType()` against `SpecTypeId.Boolean.YesNo` on 2022 to 2027 and
`Definition.ParameterType` on 2020 and 2021 - both read from the reference assemblies of all eight releases,
neither run on a Revit. The version 4 code has not run on any model.

**Pick the chain, not the selection.** Every row feeds the fragment from `filter-elements-by-category` with
`category=Mechanical Equipment` and `levelId=none` (every level). Other chats share this Revit's selection.

| # | Check | Expected |
|---|---|---|
| **BB1** | **The working run, on the working model, Changes ON.** It is a chat run, not a proof. FIRST read `Show_Clearance` on the 15 units - the owner may already have unticked it by hand. Then `revit_change` WRITE_ELEMENT_PARAMETERS with `parameterName=Show_Clearance`, `value=No`, `expect_from` naming `filter-elements-by-category where category=Mechanical Equipment`. Then the same call again. If `revit_change` is refused, write down what it said and stop - do not find another way | First run: `written` 15 if they were ticked, or `written` 0 and `alreadyThat` 15 if the owner had already unticked them - write down which. `refused`, `snapped`, `unverified` and `findings` empty. Second run: `written` 0, `alreadyThat` 15, nothing changed. Read back: all 15 unticked |
| **BB2** | **The two legs ([D-30](../DECISIONS.md)), on a TEST COPY.** With `HERON_CLIENT_ID=ajmal-pc` set and the copy in front, read one unit's `Show_Clearance`, set the job's positive `value` to the OPPOSITE (the job says how), then `python tools/batch-prove.py tools/jobs/write-element-parameters-yes-no-heron-ai-bulding-2026-09-27.yaml --dry-run`, then without `--dry-run` | `PASS`. The positive: `written` 15, `alreadyThat`, `snapped` and `refused` empty. **The negative, `value=Maybe` on the same 15:** `written` 0, `refused` 15, one `findings` line naming 'Maybe' and the accepted words, and every box exactly as it was. Both legs rolled back - read three boxes afterwards |
| **BB3** | **The second route, THEN the signature.** On the working model after BB1, read `Show_Clearance` on three units in the Properties palette, or add it as a column to a Mechanical Equipment schedule. Write what was seen into the draft's `second_route`, and only then `python brain/heron_validate.py accept write-element-parameters --by "Ajmal PS"` and set `heron-status: PROVEN` | Unticked on every unit looked at - **seen on screen, not inferred from `written`**. The proof taken 2026-09-09 described version 2 and is kept only as a record until this replaces it |
| **BB4** | **The other road to the same answer - Revit 2020 or 2021.** Those releases have no `GetDataType`, so the tick box is found through `ParameterType` instead, and that branch has never run. On a test model in Revit 2020 with any family carrying an instance Yes/No parameter, run WRITE_ELEMENT_PARAMETERS with `value=Yes` and then `value=Maybe` | `Yes`: every element `written`, the box ticked in Properties. `Maybe`: `refused`, one `findings` line. If `Yes` comes back `refused` with no `findings` line, the Yes/No was not recognised and the element went the version 3 road - the 2020 branch has failed |
