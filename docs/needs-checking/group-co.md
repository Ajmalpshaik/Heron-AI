# Needs checking — Group CO

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone.
> **The register's rules, and every group's place in it, are on that page.** A new row for this group
> goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads
> it back into the register for every tool that reads the register.

## Group CO - deleting a family parameter that labels a dimension: the label taken off first, when asked (2026-10-06)

`DELETE_FAMILY_PARAMETERS` version 2 takes an optional switch `unlabelFirst`. With it true, a dimension
labelled with a parameter being deleted no longer blocks it: the label is taken off first
(`Dimension.FamilyLabel = null`), read back bare and unmoved after Revit regenerates, and then the
parameter is deleted - one call, one Undo. Formulas, arrays, form fields, nested links and connectors still
block exactly as before, and any one of them refuses the whole call before a label comes off. Absent, the
switch is false and version 1's call is unchanged, refusal wording included. Before it, `Half Frame Flange
2` and `Frame Thickness 2` were refused in a family with *"it labels 1 dimension(s) - take the label off
first"*, and no tool could take a label off ([row 5b-349](../fragment-issues/section-5b-rows-176-200.md)).

Widened rather than a second tool, and widened HERE rather than in `LABEL_FAMILY_DIMENSION`: that card
only ever adds a label, as `ADD_FAMILY_PARAMETERS` only ever adds a parameter - this card's own header
gives the reason an add and a remove are not one card. The change is under `impl/`, so **the version 1
proof (fingerprint `6228dbeedea3a82a`) is stale and the card is DRAFT** until the runs below are signed.
Compiled for Revit 2020 to 2027 on 2026-10-06. **CO1 to CO5 RAN 2026-10-06 on Revit 2024, session 51820, in the scratch family `HeronUnlabelProof`** (saved to the session's scratchpad by `CREATE_FAMILY_DOCUMENT` v2, brought to front by `ACTIVATE_DOCUMENT` run without `--write`, every call pinned with `fragment --in HeronUnlabelProof`). **Not signed.**

**THE ARRANGEMENT - made KEPT, once, in a new unsaved scratch family**, never one of the owner's saved
families. `CREATE_FAMILY_DOCUMENT` from *Metric Generic Model.rft* gives `FamilyN`; with it in front, and
`HERON_CLIENT_ID=ajmal-pc`, `--session 51820` and `< /dev/null` on every line:

```
python mcp/client/heron_bridge_client.py fragment create-reference-planes --write --apply --set "planes=Unl Left=-150; Unl Right=150" --set axis=left-right
python mcp/client/heron_bridge_client.py fragment create-reference-planes --write --apply --set "planes=Unl Front=-150; Unl Back=150" --set axis=front-back
python mcp/client/heron_bridge_client.py fragment add-family-parameters --write --apply --set "parameterNames=Heron Unl A, Heron Unl B, Heron Unl C" --set kind=Length --set instance=false --set parameterGroup=Dimensions
python mcp/client/heron_bridge_client.py fragment set-family-type-values --write --apply --set typeName=T1 --set "values=Heron Unl A=300; Heron Unl B=300; Heron Unl C=100"
python mcp/client/heron_bridge_client.py fragment label-family-dimension --write --apply --set "fromPlane=Unl Left" --set "toPlane=Unl Right" --set "parameterName=Heron Unl A" --set "centrePlane=Center (Left/Right)"
python mcp/client/heron_bridge_client.py fragment label-family-dimension --write --apply --set "fromPlane=Unl Front" --set "toPlane=Unl Back" --set "parameterName=Heron Unl B" --set "centrePlane=Center (Front/Back)"
python mcp/client/heron_bridge_client.py fragment set-family-formula --write --apply --set "parameterName=Heron Unl C" --set "formula=Heron Unl B / 3"
```

So `Heron Unl A` labels one dimension and nothing else uses it; `Heron Unl B` labels one dimension AND
`Heron Unl C`'s formula reads it. Every run below is rolled back (`validate --write`).

| # | Run | Look for |
|---|---|---|
| **CO1** | `validate --in "FamilyN" --negative-in "FamilyN" --write --set "parameterNames=Heron Unl A" --set unlabelFirst=true --negative-set "parameterNames=Heron Unl B" --negative-set unlabelFirst=true delete-family-parameters` - or the job file [`delete-family-parameters-unlabel-2026-10-06.yaml`](../../tools/jobs/delete-family-parameters-unlabel-2026-10-06.yaml) | `deleted` holds `Heron Unl A (type)`; `unlabelled` holds ONE line - `"Unl Left" to "Unl Right"`, its view, reading 300 mm - and the group ROLLED BACK. **RAN 2026-10-06, ROLLED BACK:** `deleted` 1 - Heron Unl A (type); `unlabelled` 1 - the dimension from "Unl Left" to "Unl Right" in "Ref. Level" (the client truncates the rest of the line); findings *Took the label off 1 dimension(s) first, as asked*. Draft fingerprint `afa22976c7d862bd` |
| **CO2** | the negative half of CO1: `Heron Unl B` - a label AND a formula | refused quoting `"Heron Unl C" = Heron Unl B / 3`; `unlabelled` EMPTY and `deleted` empty - no label comes off in a refused call. **RAN 2026-10-06, ROLLED BACK:** refused - *"Heron Unl C" = Heron Unl B / 3 - change or clear that formula first*; `unlabelled` 0, `deleted` 0 |
| **CO3** | version 1's call, unchanged: `validate --in "FamilyN" --set "parameterNames=Heron Unl A" delete-family-parameters` with NO `--write` and no `unlabelFirst` (a refusal writes nothing) | refused in version 1's exact words - *it labels 1 dimension(s) - take the label off first (select the dimension, Label: <None>)* - `unlabelled` empty. **RAN 2026-10-06** (as `fragment --in HeronUnlabelProof`, no `--write`): refused in version 1's exact words; `unlabelled` 0 |
| **CO4** | `--write --set "parameterNames=Heron Unl A, Heron Unl B" --set unlabelFirst=true` | refused as a whole: neither deleted, neither dimension unlabelled - all or nothing. **RAN 2026-10-06, ROLLED BACK:** refused, 1 of 2 could not go (Heron Unl B, the formula); `deleted` 0, `unlabelled` 0 |
| **CO5** | FamilyN after CO1 to CO4 | Both dimensions still labelled (`Heron Unl A`, `Heron Unl B`) in the plan, all three parameters still there - every run rolled back. Look: rollback has failed on this PC before (BD1). **CHECKED 2026-10-06** with version 1's call after CO1 to CO4: both names still refused because each *labels 1 dimension(s)*, B also for the formula - both labels and all three parameters still there |
| **CO6** | the signature | `python brain/heron_validate.py accept delete-family-parameters --by "Ajmal PS"` on the CO1/CO2 record (`brain/proof-drafts/runs/delete-family-parameters.json` - leave `--out` off), on the owner's word only |
| **CO7** | the second route: a copy of the arrangement by hand - the Unl Left to Unl Right dimension, Label: <None>, then Family Types, Delete Parameter | The family ends the same as after CO1 kept, the planes still 300 apart |
| **CO8** | CO1 on **Revit 2020** and **2027** | The same answers. The `Dimension.FamilyLabel` setter reads the same 2020 to 2027 (api-surface) and compiles on all eight releases; not run there |

**Routing, measured 2026-10-06 on scratch stores - not proof.** The three declared phrasings resolve to it by
identity. On the trained (model) backend the bare *"unlabel the dimension"*, *"remove the label from the
dimension"* and *"take the label off this dimension"* now rank it first too - and it DELETES a parameter, so
a request to take a label off and keep the parameter has no tool that does only that; the host must ask.
*"delete the unused family parameters"* moved to `FIND_UNUSED_FAMILIES` on that backend, and the same move
happens with only the status changed from PROVEN to DRAFT - it comes back when the card is signed again.
