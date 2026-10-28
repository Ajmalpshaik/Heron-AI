# Needs checking — Group JH

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-28 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group JH - a curtain system laid on the faces of a mass: `CREATE_CURTAIN_SYSTEM` version 1 (2026-10-28)

A new card, [`create-curtain-system`](../../brain/fragments/create-curtain-system/fragment.yaml) (FRG-ELE-127),
written in a cloud session for the modeller's *"put a curtain system on the faces of this mass"*, *"make a
facade on the mass faces"* and *"curtain system by face on the side faces of mass Tower A"*. Nothing in the
library made a curtain system: `CREATE_WALL` draws a curtain WALL along a line, and `DIVIDE_FAMILY_SURFACE`
divides a surface inside the mass family.

**What it does.** Revit's Curtain System by Face, in the project: a curtain system of a type named in the
request, on the faces of a placed mass or generic model - the selected one, or one named by its id, family
name, *Family : Type* or Mark. The faces are the upright ones unless the request says *top* or *all*. One
`Document.Create.NewCurtainSystem2(ReferenceArray, CurtainSystemType)` per host; every refusal before the
first write; then each curtain system read back - its type, its curtain grids (one per face, the API
reference says), and on each grid the U and V grid lines, panels and mullions Revit holds.

**What is done and what is not.** It compiles for all eight releases 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-10-28, in the cloud); every Revit member it calls was read off
the reference assemblies of all eight with `tools/api-surface`. Its three sentences reach it on this
container's trained backend, and *"how many panels are on this curtain system"* reaches
`REPORT_CURTAIN_ELEMENTS`, a read. **Nothing has met a Revit.** `revit_change` refuses it until JH12 is
signed.

**The assumptions this group settles - none of them is known from the cloud:**

- that Revit takes the face references read the way the card reads them - through the family's own
  geometry (`GeometryInstance.GetSymbolGeometry()`, no transform), which the API reference names as the
  geometry fit for creating elements on (JH1, JH2);
- whether a **generic model that is not a mass** takes a curtain system - the API reference says
  *"masses or generic models"*, and nothing has run it (JH5);
- which of a face's two grid directions Revit calls U, and whether its counts are the derived ones (JH2);
- what Revit does with a second curtain system on faces that already carry one - the card counts and names
  curtain systems standing in the host's outline (`alreadyNear`) but cannot read which faces they sit on (JH8).

**The rig (JH0) - scratch, never a working model.** The owner's Project1 (Revit 2020, DefaultMetric),
**saved** as *Heron JH Curtain.rvt* in `%TEMP%\heron-work\group-jh` - a run needs a saved model. On Level 1
(elevation 0), by hand, nothing selected:

| What | How |
|---|---|
| *Heron JH Tower* | Massing & Site > In-Place Mass, named *Heron JH Tower*; a rectangle from (0, 30000) to (18000, 42000) mm, Create Form, 30000 mm high. Its Mark *Tower A*. Four upright faces - two 18000 x 30000, two 12000 x 30000 - a top and an underside |
| *Heron JH Box* | An in-place **Generic Model** named *Heron JH Box*: an extrusion 6000 mm square from (30000, 30000) to (36000, 36000), 6000 mm deep |
| *Heron JH 1500* | Type Properties of the model's curtain system type > Duplicate, *Heron JH 1500*: Grid 1 and Grid 2 Layout Fixed Distance, Spacing 1500; every mullion None |
| *Heron JH Wall* | One *Basic Wall: Generic - 200mm*, 5000 mm long, well clear of both; its Mark *Heron JH Wall* - the negative's element |

**The numbers are derived, not remembered.** 1500 divides every face's length (18000, 12000, 30000, 6000),
its half (9000, 6000, 15000, 3000) and every corner's distance from the project origin (0, 30000, 36000,
42000), so each count holds whether Revit lays a grid from a face's edge, its centre or the origin. 18000 mm
at 1500 is 12 panels and 11 lines between them; 12000 is 8 and 7; 30000 is 20 and 19; 6000 is 4 and 3.

[`tools/jobs/create-curtain-system.yaml`](../../tools/jobs/create-curtain-system.yaml) runs JH2 and JH9
together (`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected; a by-hand row is
`python mcp/client/heron_bridge_client.py validate --write --set ... create-curtain-system` - `--write`
without `--apply`, so it is rolled back.

| # | Run | Look for |
|---|---|---|
| **JH0** | *The rig, by hand*, as above, saved | The four things in the table; nothing selected |
| **JH1** | *The one kept run (D-119), on the owner's yes only.* `python mcp/client/heron_bridge_client.py try-once create-curtain-system --set "host=Heron JH Tower" --set "curtainSystemType=Heron JH 1500"`, a 3D view showing the tower in front, the model saved first | The same answer as JH2; in the view, glass on the four sides and none on the top; the owner chooses Keep or Discard. A Keep is the positive half of the proof (D-120) |
| **JH2** | *The positive, rolled back.* `python tools/batch-prove.py tools/jobs/create-curtain-system.yaml --session <pid>`, its positive leg: `host=Heron JH Tower`, `curtainSystemType=Heron JH 1500`, faces left out | `created` 1, `facesAsked` 4, `facesTaken` 4, `alreadyNear` 0, `refused` null; `readBack` *4 face(s) asked, 1800 m2 (its faces: 4 vertical, 1 top, 1 underside)* and four grids - two of 11 and 19 lines and 240 panels, two of 7 and 19 lines and 160 panels, 800 in all, mullions 0 on each. **Record** which of U and V held the upright lines |
| **JH3** | *The top.* JH2 with `faces=top` | `facesAsked` 1, `facesTaken` 1; one grid of 11 and 7 lines, 96 panels |
| **JH4** | *Every face.* JH2 with `faces=all` | `facesAsked` 6, `facesTaken` 6 - the underside included, as the card says; 992 panels |
| **JH5** | *A generic model - the assumption.* `host=Heron JH Box`, faces left out | **Not known which:** four grids of 3 and 3 lines and 16 panels each, or the call failed in Revit's own words with nothing kept. Either is the finding |
| **JH6** | *The three ways to the host.* The tower selected and `host` left out; `host=Tower A` (its Mark) with nothing selected; `host=<the tower's element id>` | Each the same answer as JH2; the binding note names *elements from the selection (1)* on the first |
| **JH7** | *The other end of the range.* JH2 on Revit 2024, the rig saved from 2020 and upgraded | The same answer - one implementation for every release |
| **JH8** | *A second one on the same faces - only if JH1 was kept.* JH2 again, rolled back, on the tower that now carries JH1's curtain system | `alreadyNear` 1 and a finding naming JH1's id; **record** what Revit did - a second curtain system over the first, a warning, or a refusal |
| **JH9** | *The negative - the D-30 negative.* Its negative leg: `host=Heron JH Wall`, the same type | `created` empty, `facesAsked` 0, `readBack` empty; `refused` names the Walls element *Basic Wall : Generic - 200mm*, Mark *Heron JH Wall*, as not a mass or a generic model, ending *NOTHING WAS MADE*; the wall as it was |
| **JH10** | *The other refusals, by hand, each rolled back.* The wall selected and `host` left out; `curtainSystemType=Heron JH Not A Type`; `faces=sloped`; nothing selected and `host` left out | Each refused before anything is made: by the wall's category; with the names of the curtain system types the model has, *Heron JH 1500* among them; naming vertical, top and all; *"Nothing is selected and no host was named"* |
| **JH11** | *Routing, on the PC's backend.* Ask `heron_lookup` the modeller's three sentences, then *"how many panels are on this curtain system"*, *"how much glass is in this facade"* and *"what is this facade made of"* | The three reach `CREATE_CURTAIN_SYSTEM`; the three questions reach `REPORT_CURTAIN_ELEMENTS` - a read - never this write (D-86). On this container's backend all six did |
| **JH12** | *The signature, on the owner's word only*, after JH1 (kept) or JH2, and JH9 | `python brain/heron_validate.py accept create-curtain-system --by "Ajmal PS"`, and `heron-status` set to PROVEN in the same change |

A row that fails stays open; nothing in a row changes to make it pass.
