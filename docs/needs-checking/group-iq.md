# Needs checking — Group IQ

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-22 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group IQ - model text that lands where asked (`ADD_FAMILY_MODEL_TEXT` version 2), and a sprinkler layout that says why it asks (2026-10-22)

From rows [5b-1315 and 5b-1316](../fragment-issues/section-5b-rows-401-425.md). Both were seen on Revit 2020
(session 40628) on 2026-10-22, in a chat building families and laying out sprinklers: a transformer family's
numbers *1* and *2* stood 2.5 m in front of their plates, and a sprinkler layout on four Spaces could not go on
because every sprinkler type loaded was a hosted one and the reply never said so. Both fixed the same day with
no Revit:

| What | Where | What changed |
|---|---|---|
| `ADD_FAMILY_MODEL_TEXT` v2 | [`add-family-model-text`](../../brain/fragments/add-family-model-text/fragment.yaml) | MODIFY. Each text is measured once placed - its left end, centre or right end against `at`, its foot against the baseline, its back against the plane plus `offsetMm`. One that missed by more than half a millimetre is placed again moved by the miss; one that still misses by a millimetre refuses the whole call. Version 1 printed the miss in its answer and kept it (5b-1315) |
| `revit_sprinkler_layout` | [`heron_sprinkler_layout.py`](../../brain/heron_sprinkler_layout.py), [`heron_mcp_server.py`](../../mcp/server/heron_mcp_server.py) | READ. The chat's answer gives each question with its why: a hosted type says it is hosted, and with no level-based type loaded the family to load is named - `Fire Protection\Sprinklers\M_Sprinkler - Pendent.rfa`. `spaces` takes UniqueIds with `;` or commas between; element ids are refused by name before Revit is asked (5b-1316) |

**What is done and what is not.** The library compiles for 2020 to 2027 (`tools/check-fragments-compile.py`,
2026-10-22). [`tests/test_sprinkler_layout.py`](../../tests/test_sprinkler_layout.py) has four new checks that
fail on main's code and pass on this change. **Nothing here has run in Revit.** `ADD_FAMILY_MODEL_TEXT` is DRAFT,
so `revit_change` refuses it until IQ1 is signed; its version 1 proof stays in the card as a record. **Which way
Revit reads the point `NewModelText` is given is not known** - version 2 does not assume one; IQ1's finding line
is the measurement. Restart the Claude app for the sprinkler change (Python); the fragment's C# is read live.

**The family.** A scratch family, never a working one: a new family from Revit 2020's *Metric Generic
Model.rft*, saved as *Heron IQ Text.rfa* in `%TEMP%\heron-work\group-iq`. In it, through `revit_change` with
`CREATE_FAMILY_FORMS`: a solid box *"rect -1000,-2560 1000,0"* on Ref. Level from 0 to 1200 - its front face at
Y -2560, the plates' face of the day. Run every row on **Revit 2020**, then IQ1 again on the newest Revit on the
PC.

| # | Run | Look for |
|---|---|---|
| **IQ0** | *Negative FIRST.* `texts` = `text=PANEL workPlane=Center (Front Back) at=0,400 heightMm=40 depthMm=2` - a plane the family has not got | Refused naming row 1 and listing the family's planes; `textCount` 0; no Model Text type made; Undo unchanged |
| **IQ1** | *The fault's own case.* `text=1 workPlane=Center (Front/Back) at=-650,510 heightMm=100 depthMm=3 align=center offsetMm=-2560` | `placed` reads its letters at **Y -2560 to -2563** (version 1: -5120 to -5123) and **centred on X -650** - their X extent about equal either side of it (version 1: -650 to -542.32), ending *"its centre where asked, measured"*. **Copy the finding *"Row 1: Revit put the letters ... from where they were asked"* into the run record whole** - or that there was none: it is how Revit read the point. **Look** at the Front elevation: the 1 on the box's face, centred where asked |
| **IQ2** | No offset, on the named plane: `text=PANEL workPlane=Center (Front/Back) at=0,400 heightMm=40 depthMm=2 align=right` | *"sketched on the plane itself"*; its X extent ends at 0 within a millimetre; Y 0 to -2 |
| **IQ3** | *A named plane away from the origin.* Make a reference plane *IQ Face* at Y -2560 facing front (`CREATE_REFERENCE_PLANES`), then `text=2 workPlane=IQ Face at=650,510 heightMm=100 depthMm=3 align=center` | Its letters at Y -2560 to -2563, centred on X 650 - whether on the plane itself or a work plane of its own, said. Any finding of a first placing that missed is copied whole |
| **IQ4** | *The other axis.* `text=TOP workPlane=Ref. Level at=0,-1280 heightMm=50 depthMm=2 align=center offsetMm=1200` | Its letters at Z 1200 to 1202, centred on X 0; read from the top in the Ref. Level plan |
| **IQ5** | IQ1 and IQ3's rows in one call | `textCount` 2 in ONE undo entry |
| **IQ6** | *The sprinkler case (5b-1316).* A scratch MEP project with four Spaces and only `M_Sprinkler - Pendent - Hosted` loaded; the four Spaces selected; `revit_sprinkler_layout` with `inputs` naming that type as `job.type` | The answer's *"Still to answer: job.type - M_Sprinkler - Pendent - Hosted: ... is a WorkPlaneBased family - it needs a face ... None of the N sprinkler type(s) loaded is level-based ... Revit's US Metric library has Fire Protection\Sprinklers\M_Sprinkler - Pendent.rfa"*. Nothing placed |
| **IQ7** | *The family named is level-based.* Load `M_Sprinkler - Pendent.rfa` from Revit 2020's US Metric library, `Fire Protection\Sprinklers`; run IQ6 again with its type | The type's placement reads `OneLevelBased` in the Companion's type list, and the rooms preview. **If it reads anything else, `LEVEL_BASED_FAMILY` names the wrong family** - reopen row 5b-1316 |
| **IQ8** | *Which rooms.* `spaces` = the four Spaces' element ids, commas between; then their UniqueIds, commas between, with nothing selected | The first refused before Revit is asked, each id named, *"Select those Spaces in Revit and ask again with `spaces` empty"*; the second reads all four rooms |

**Then sign** `add-family-model-text` version 2 on IQ1 with IQ0's negative, after IQ1 to IQ5 have passed; close
row 5b-1316 on IQ6 to IQ8 - the sprinkler change is Heron's Python, not a fragment, so there is no card to
sign. A row that fails reopens its row; nothing in a row changes to make it pass.
