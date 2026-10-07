# Needs checking — Group CR

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone.
> **The register's rules, and every group's place in it, are on that page.** A new row for this group
> goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads
> it back into the register for every tool that reads the register.

## Group CR - a connector deleted from the family being edited (2026-10-07)

`REMOVE_FAMILY_CONNECTOR` (FRG-MEP-061, DRAFT) deletes connectors from the family open in the Family
Editor, named the way `SET_FAMILY_CONNECTOR_ROLES` names them - the plane the face lies on, an id from
`REPORT_FAMILY_CONNECTORS`, or `all`. Every name is checked before the first goes; each is read back gone;
a connector NOT named going with them fails the call. It says what is left behind: the family parameters
that drove the size, a partner read back unlinked, a domain left with no primary. A project in front is
refused. Before it, a pipe connector made on `Air Outlet` in the owner's Family1 (Revit 2024) could not be
taken off by any tool ([row 5b-354](../fragment-issues/section-5b-rows-176-200.md)).

A new card rather than a mode on `SET_FAMILY_CONNECTOR_ROLES`: that card edits a connector that stays and
reads every setting back on it; its header and this card's say why. Compiled for Revit 2020 to 2027 on
2026-10-07. **Nothing below has run.** NEVER on Family1 - the request's own family is not a test copy.

**THE ARRANGEMENT - made KEPT, once, in a new scratch family**, never one of the owner's saved families.
`CREATE_FAMILY_DOCUMENT` from *Metric Generic Model* gives a family; with it in front, and
`HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` and `< /dev/null` on every line:

```
python mcp/client/heron_bridge_client.py fragment create-reference-planes --write --apply --set "planes=Conn A=-100; Conn B=100" --set axis=left-right
python mcp/client/heron_bridge_client.py fragment create-family-extrusion --write --apply --set "workPlane=Ref. Level" --set "profile=rect -100,-100 100,100" --set startMm=0 --set endMm=200 --set solid=true
python mcp/client/heron_bridge_client.py fragment add-family-parameters --write --apply --set "parameterNames=Conn Dia, Conn W, Conn H" --set kind=Length --set instance=false --set parameterGroup=Dimensions
python mcp/client/heron_bridge_client.py fragment set-family-type-values --write --apply --set typeName=T1 --set "values=Conn Dia=50; Conn W=150; Conn H=100"
python mcp/client/heron_bridge_client.py fragment add-family-connector --write --apply --set "facePlane=Conn A" --set "system=Domestic Cold Water" --set "sizeParameters=Conn Dia"
python mcp/client/heron_bridge_client.py fragment add-family-connector --write --apply --set "facePlane=Conn B" --set "system=Supply Air" --set "sizeParameters=Conn W, Conn H"
python mcp/client/heron_bridge_client.py fragment report-family-connectors
```

So a pipe connector lies on `Conn A` and a duct connector on `Conn B`. Every run below is rolled back
(`validate --write`).

| # | Run | Look for |
|---|---|---|
| **CR1** | `validate --in "<family>" --negative-in "<family>" --write --set "connectors=Conn A" --negative-set "connectors=Conn Middle" remove-family-connector` - or the job file [`remove-family-connector-2026-10-07.yaml`](../../tools/jobs/remove-family-connector-2026-10-07.yaml) | `removed` holds ONE line, the pipe connector on "Conn A"; `connectorReport` lists only the duct connector on "Conn B"; a finding names `Conn Dia` as staying in the family; the group ROLLED BACK |
| **CR2** | the negative half of CR1: `Conn Middle` | refused, the family's two connectors listed; `removed` EMPTY |
| **CR3** | `--set "connectors=Conn A, Conn Middle"` | refused as a whole - the Conn A connector still there, all or nothing |
| **CR4** | setup `add-family-connector` on `Conn A` a second time (rolled back with the run), then `connectors=Conn A` | refused - two connectors lie on that plane, both ids named. Then the same setup with the PRIMARY one's id (from `REPORT_FAMILY_CONNECTORS`): only that one goes, and a finding says no pipe connector left reads primary, with the `SET_FAMILY_CONNECTOR_ROLES` words |
| **CR5** | `--set connectors=all` | every connector in `removed`; `connectorReport` "No connectors are left in the family."; RECORD what `Document.Delete` returned besides the connectors - any extra is listed by id in a finding |
| **CR6** | two pipe connectors linked by `SET_FAMILY_CONNECTOR_ROLES` (`links=Conn A > <id>`), then one deleted | a finding reads the partner back NOT linked; RECORD whether Revit cleared that end itself |
| **CR7** | a project in front: `validate --in "<a project>" remove-family-connector --set connectors=all` with NO `--write` | `notAFamily` true, refused, nothing deleted |
| **CR8** | the family after CR1 to CR7 | `REPORT_FAMILY_CONNECTORS` lists both connectors, unchanged - every run rolled back. Look: rollback has failed on this PC before (BD1) |
| **CR9** | the signature | `python brain/heron_validate.py accept remove-family-connector --by "Ajmal PS"` on the CR1/CR2 record, on the owner's word only |
| **CR10** | the second route: the same connector selected in the Family Editor and deleted by hand, on a copy | The family ends the same as after CR1 kept |
| **CR11** | CR1 on **Revit 2020** and **2027** | The same answers. `Document.Delete(ElementId)` reads the same 2020 to 2027 (api-surface) and compiles on all eight releases; not run there |

**Routing, measured 2026-10-07 on a scratch store - not proof.** *"delete the pipe connector from the family
being edited"* - the sentence that found nothing on 2026-10-07 - resolves to it by identity; *"remove the
connector from this family"*, *"delete the connector on Air Outlet"* and *"delete the selected connectors"*
rank it first by words and nearness both. *"delete these elements"* still goes to `DELETE_ELEMENTS`,
*"put a connector on this face"* to `ADD_FAMILY_CONNECTOR` and *"flip the connector on Port Left"* to
`SET_FAMILY_CONNECTOR_ROLES`. `check-routing` lists no crossing for it.
