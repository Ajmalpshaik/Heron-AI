# Needs checking — Group JK

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-28 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group JK - an adaptive component's points turned to the host: `SET_ADAPTIVE_POINT_ORIENTATION` version 1 (2026-10-28)

A new card, [`set-adaptive-point-orientation`](../../brain/fragments/set-adaptive-point-orientation/fragment.yaml)
(FRG-GEO-074), written in a cloud session for the gap
[`adaptive-family-creation`](../../brain/skills/adaptive-family-creation.yaml) named: each placement point's
**Orientation** - *Instance (xyz)* by default, *Host and Loop System (xyz)* for a panel placed on a surface - was a
Properties choice no Heron tool set. The modeller's words: *"set the adaptive points orientation to host and loop
system"*, *"make the placement points orient to host"*, *"change adaptive point orientation to global xyz"*,
*"set point 1 orientation to instance"*.

**What Revit's API says.** Read 2026-10-28 with `tools/api-surface --members` over the reference assemblies of
2020 to 2027, the same on all eight: `AdaptiveComponentFamilyUtils.GetPointOrientationType` and
`SetPointOrientationType`, and the enum `AdaptivePointOrientationType` with ToHost, ToHostAndLoopSystem,
ToGlobalZthenHost, ToGlobalXYZ, ToInstanceZthenHost and ToInstance. The 2024 `RevitAPI.xml` (the only release
whose XML is cached here) says both calls are *"of an Adaptive Placement Point"* and throw when the id *"does
not correspond to an Adaptive Placement Point"*, that the default is ToInstance, and that the enum's items were
renamed in 2016 *"to better align the names with the corresponding text in the Revit UI"*. The six Properties
words the card prints - *Instance (xyz)*, *Instance (z) then Host (xy)*, *Host (xyz)*, *Host and Loop System
(xyz)*, *Global (xyz)*, *Global (z) then Host (xy)* - are [43](../43-building-revit-families.md)'s, SOURCED and
never seen on a screen: **an ASSUMPTION until JK5.**

**What is done and what is not.** It compiles for all eight releases (`tools/check-fragments-compile.py`,
2026-10-28, in the cloud); its four sentences reach it and it takes no other card's sentence
(`tools/check-routing.py`, a scratch store, the container's backend). The word and point-number reading was
run off Revit in a scratch harness: every phrasing above, Revit's own six, `1,2,3,4`, `1-4`, `1 to 4`,
`point 1`, `all` and empty read as meant, and `sideways`, `shape handle`, `1-x`, `4-1` and `all except 2` are
refused. **Nothing has met a Revit.** `revit_change` refuses it until JK13 is signed.

**What it does NOT do, by design.** A shape handle point is listed and never written - Revit's reference keeps
Orientation to placement points (JK7 records what Revit actually reads for one). Points are named by placement
number only. **No reader in the library answers *"what orientation are the adaptive points"***:
`REPORT_FAMILY_TEMPLATE`'s `pointList` gives numbers, ids and positions, not Orientation. That question must not
land on this write ([D-86](../DECISIONS.md)); on the container's backend it ranked this card first, as it
ranked `PLACE_ADAPTIVE_POINTS` (also a write) first before this card existed. **Widening
`REPORT_FAMILY_TEMPLATE` to read it is the owner's call** - it is PROVEN, and any change under its `impl/` makes
its proof stale. Until then the modeller reads Properties. **2026-10-29: the owner said yes** -
`REPORT_FAMILY_TEMPLATE` version 4 prints each point's Orientation in this card's words, DRAFT, and
[Group JQ](group-jq.md) proves it. JK12's question should now land there.

**The rig (JK0) - scratch, never a working family.** A new family from the metric *Generic Model Adaptive*
template with four placement points made and **kept** - `PLACE_ADAPTIVE_POINTS` *"0,0,0; 1000,0,0; 1000,1000,0;
0,1000,0"*, pointKind *placement*, through `revit_change` or by hand, never by a rolled-back proof run - **saved**
as *Heron JK Orientation.rfa* in `%TEMP%\heron-work\group-jk`, left open in front. A try-once needs a saved
model. The job file is [`tools/jobs/set-adaptive-point-orientation.yaml`](../../tools/jobs/set-adaptive-point-orientation.yaml)
(`--dry-run` reads it back and sends nothing: *WOULD RUN*, 2026-10-28). Every line below with
`HERON_CLIENT_ID=ajmal-pc` and `--session <process id>` when two Revits are connected.

| # | Run | Look for |
|---|---|---|
| **JK0** | *The rig, by hand.* As above | Four placement points numbered 1 to 4 beside them; the family saved |
| **JK1** | *Arrange - record the default.* Select each point; read Properties > Orientation | **Record** the words. Revit's reference says a new point is *Instance (xyz)*; if any reads otherwise, JK4's negative is arranged on what it does read |
| **JK2** | *Once, on the owner's yes only.* `python mcp/client/heron_bridge_client.py try-once set-adaptive-point-orientation --set "orientation=Host and Loop System" --set points=all` - Keep or Discard is his | `changed` 4; each point's Properties reads *Host and Loop System (xyz)*; one Ctrl+Z takes all four back. A KEPT run is the D-120 positive, and JK3 is then not run |
| **JK3** | *The positive, rolled back* (if JK2 was not kept). `python tools/batch-prove.py tools/jobs/set-adaptive-point-orientation.yaml --session <pid>`, its positive leg | `changed` 4; `changedPoints` *"point 1: Instance (xyz) -> Host and Loop System (xyz)"* and the same for 2, 3, 4; `pointOrientations` names 1 to 4 at Host and Loop System (xyz) |
| **JK4** | *The negative - a value that must not be touched.* The job's negative leg: `orientation=Instance`, `points=all` on the rig as JK1 read it. After a kept JK2 instead: `validate set-adaptive-point-orientation --write --kept --negative-set "orientation=Host and Loop System" --negative-set points=all` | `changed` 0, `alreadySet` 4, `changedPoints` empty, *"Every placement point asked for already reads ... - nothing was changed."* **FAIL:** any point rewritten |
| **JK5** | *Revit's words - the ASSUMPTION.* Rolled back (`validate --write`, no `--apply`), once for each: `host`, `host and loop system`, `global xyz`, `global z then host`, `instance z then host`, `instance` | **Record**, for each, the words Properties shows on a point set that way by hand, against the words `pointOrientations` printed. A difference is a FRAGMENT-ISSUES row and a wording change, not a pass |
| **JK6** | *One point, the others untouched.* Rolled back: `orientation=global xyz`, `points=1` | `changed` 1, point 1 at Global (xyz); points 2, 3 and 4 read as JK1 recorded |
| **JK7** | *A shape handle point.* Add one by hand or by `PLACE_ADAPTIVE_POINTS` *"500,500,300"*, pointKind *shape handle*, kept; then rolled back: `orientation=Instance (z) then Host (xy)`, `points=all` | `changed` 4; `pointOrientations` lists the shape handle point as not written. **Record** whether Revit read an Orientation for it (the card prints what it reads) and whether Properties shows an Orientation row for a shape handle point at all |
| **JK8** | *Refusals, rolled back.* `points=9`; `orientation=sideways`; `points=shape handle`; then in a new family from the plain metric *Generic Model* template; then with a project in front | Each `changed` 0 and `refused` - naming the points 1, 2, 3, 4; Revit's six Orientations; placement points only; *not an adaptive component* with REPORT_FAMILY_TEMPLATE named; and in the project `notAFamily` true |
| **JK9** | *A pattern-based panel.* A new family from the metric *Curtain Panel Pattern Based* template, rolled back: `orientation=host`, `points=all` | **Record** whether it is refused as not an adaptive component, or its template points take an Orientation. It decides whether [`pattern-based-family-creation`](../../brain/skills/pattern-based-family-creation.yaml) names this tool - left alone until then |
| **JK10** | *What it does, by eye.* JK2's family (or the rig set by hand to Host and Loop System), loaded into a test project with a mass whose face is sloped; placed by clicking its four points on the face anticlockwise, then a second copy clockwise; then the same with the rig at Instance (xyz) | With Host and Loop System the panel lies on the face, and the clockwise copy is **flipped** - as the enum's remark says; with Instance (xyz) it does not follow the face. LOOKED AT |
| **JK11** | *It is kept in the family.* After JK2's Keep: save, close, open again | Each point still reads Host and Loop System (xyz) |
| **JK12** | *Routing, on the PC's backend.* The four sentences, and *"what orientation are the adaptive points"* | The four reach this card. **Record** where the question lands; it must not be run as a change |
| **JK13** | *The signature, on the owner's word only*, after JK3 (or JK2 kept) and JK4 pass | `python brain/heron_validate.py accept set-adaptive-point-orientation --by "Ajmal PS"` and `heron-status` PROVEN in the same change |

A row that fails stays open; nothing in a row changes to make it pass. JK3 and JK4 on a second release (2024
or later) are a [D-131](../decisions/D-131.md) entry after the signature, not part of it.
