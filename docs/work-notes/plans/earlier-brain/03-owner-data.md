# 03 — The owner's own data, carried from his PC

> **Type:** Operational work note. **Not specification, and not product content.** Part of
> [the earlier-brain plan](README.md). **Owner:** Ajmal PS.
> **Status:** Carried 2026-09-23. **§A approved by the owner row by row on 2026-09-23** ("all ok").
> §B to §D are his values as he recorded them before; each is confirmed again when it is used.

---

**Why this file exists.** A cloud session cannot see the owner's PC, and three of these four sets lived only
there. They are carried here so a cloud session can **build the mechanism and test it** — and then, on the
PC, each set goes into **his own store** ([05](05-pc-proving.md)). **Heron ships none of it.** The Keyword
Agent's own rule is that it *"ships NO LIST"*, and a built-in vocabulary is the synonym table
[D-34](../../../DECISIONS.md) refuses ([`brain/heron_keywords.py`](../../../../brain/heron_keywords.py)).
Standards follow [D-33](../../../DECISIONS.md) and [D-83](../../../DECISIONS.md): **offered by name, as a
question, never applied silently.** This file is deleted with the folder.

**Nothing here names a client, a project, a person other than the owner, or a path.** Checked before it was
written; keep it that way.

---

## A. His site words — 52 entries, approved 2026-09-23

Recorded by **Ajmal PS**. **Misheard and misspelled forms are deliberately not kept** — his decision on
2026-09-23: *"fix the spelling mistake and keep"*. Only correctly spelled words and phrases are here.

### Words → what they mean in Revit

| # | He says | It means |
|---|---|---|
| 1 | diffuser | Air Terminal (supply or return) |
| 2 | grille | Air Terminal |
| 3 | floor level / floor levels | a Level — **not** a floor slab |
| 4 | light fitting(s) | Lighting Fixture |
| 5 | fire fighting | the sprinkler layout and checking job — not hydraulics or pumps |
| 6 | sprinkler point / fire point | a sprinkler head |
| 7 | VCD / VCDs | Volume Control Damper — a Duct Accessory family |
| 8 | AHU | Air Handling Unit — Mechanical Equipment |
| 9 | FCU | Fan Coil Unit — Mechanical Equipment |
| 10 | excel / to excel / out to excel | export to CSV |
| 11 | missed to tag / not tagged / tag missing | elements that carry no tag |
| 12 | level wise | grouped by level ("count level wise") |
| 13 | plant room | mechanical equipment room |
| 14 | false ceiling | suspended ceiling |
| 15 | wall sprinkler | sidewall sprinkler — its own spacing rules |
| 16 | how much from wall | the distance-to-wall rule |
| 17 | how much from the slab | how far an upright's deflector sits below the slab |
| 18 | ceiling void | the space between ceiling and slab (NFPA: a concealed space) |
| 19 | louvre | an Air Terminal |
| 20 | room boundary | the room's real outline, to lay out from |
| 21 | reducer | a rectangular transition fitting |
| 22 | sound attenuator | always this word — never "silencer" |
| 23 | HVAC plans | HVAC floor plan views — not plant equipment |

### Phrases that come with a rule

| # | He says | It means |
|---|---|---|
| 24 | "duct hide" | hide the ducts only. He names the thing first and the action second ("pipe isolate", "wall hide"). The fittings stay visible — say so and offer to hide them too |
| 25 | "see only the X" / "remaining everything hidden" | isolate X — the opposite of hide |
| 26 | the same thing, a new action, in the next message | he is correcting the last request, not adding a second one |
| 27 | "fitting" | a duct fitting or a pipe fitting — tell which from the conversation, otherwise ask |
| 28 | "schedule" | a real Revit schedule, or only a table in the reply — ask which |
| 29 | "coverage" | report existing coverage, lay out new devices, or (for sprinklers) a code check — ask which |
| 30 | "biggest size" | the largest size — and say which measure: width, diameter or area |
| 31 | "bigger counts" | the most common size, not the largest |
| 32 | "do the grayout" / "grayout for MEP" | his full view standard (§C), with his settled values |
| 33 | "red only VCD, remaining all grey" | a quick highlight: one thing coloured, everything else grey — not the grayout standard |
| 34 | "can I see now the X", during a colouring job | make X the red one and grey the rest — do it, do not just count |
| 35 | "maximize" (grids, levels) | Maximize 3D Extents — every datum ending on the same line |
| 36 | "X axis grid" / "Y axis grid" | the grids that run along that axis |
| 37 | "flip" (grids) | move the bubble to the other end; "all on the left / all at the bottom" is the clear form |
| 38 | "keep that to above" | the top of the plan |
| 39 | "center of the room" | the room's true centre, with care for L-shaped rooms |
| 40 | "interior / exterior dimension" | inside faces with the line inside / outside faces with the line outside — asked as a pair |
| 41 | "from the wall side" vs "from wall mid" | the wall's face vs the wall's centreline |
| 42 | "the dimension", on its own | ask which kind; "in room 4" means the room whose **Number** is 4 |
| 43 | "insulation" | the wrap — insulation or lining. It takes its host's colour |
| 44 | "with ceiling how, without ceiling how" | the two sprinkler height cases: under a ceiling, or under the slab and beams |
| 45 | "we need upright and pendant also" | two layers: uprights in the void, pendents below the ceiling |
| 46 | "visualization" / "widget" | a chart or a diagram inside the chat reply — not a render |
| 47 | "artifact" | a published page with its own link — only when he asks for one |
| 48 | "one supply one return" / "zig zag" | supply and return alternating — a checkerboard in a room |
| 49 | "the FCU any of the side" | either end is fine — pick one and say which |
| 50 | "take a branch to the side and move to the front" | route around when the diffuser sits behind the FCU's outlet |
| 51 | "from that branch takeoff, 200 mm there reducer" | 200 mm of straight duct after the takeoff, then the reducer |
| 52 | numbers when he speaks | millimetres — confirm once per project |

---

## B. His real questions — 79, with names removed

From the question log the earlier library kept on his PC — **428 entries, 2026-08-13 to 2026-08-27**, never
in git. Kept here: requests that are **real Revit work and make sense on their own**. Removed: questions
about tooling and git, follow-ups that need the previous message, and anything naming a client, a project,
a family vendor, a person, a path or a link. **Spelling is corrected**, as he asked for his words on
2026-09-23 — and because understanding a misspelling is the host's job, not Heron's
([D-34](../../../DECISIONS.md)). His vocabulary and his way of asking are kept.

The **expected answer** for each is **not** written here: package **C6** proposes one from Heron's capability
list and the owner confirms the table.

| # | Question | Asked |
|---|---|---|
| 1 | How many diffusers do I need in this room? | 08-13 |
| 2 | How many VCDs are there, and what sizes are they? | 08-13 |
| 3 | How many VCDs are there in the model? | 08-13 |
| 4 | Isolate only the VCDs, not all the duct accessories. | 08-13 |
| 5 | Isolate only the VCD with the biggest width. | 08-13 |
| 6 | Bring back all the VCDs with the bigger counts — I think it is the 200x200 size. | 08-13 |
| 7 | What is the biggest duct size? | 08-13 |
| 8 | What is the biggest space? | 08-13 |
| 9 | How many air terminals are in the biggest space? | 08-13 |
| 10 | Isolate all the VCDs. | 08-13 |
| 11 | Check all the ducts and make a visual report by width. | 08-14 |
| 12 | Colour the ducts by size and show it as a chart. | 08-14 |
| 13 | Change the 250x250 ducts to RGB(185,193,186) in the view "1 - Mech" and leave every other size as it is. | 08-14 |
| 14 | There are three ducts in the model — give each one a different colour. | 08-15 |
| 15 | Do the grayout for MEP in the active view. | 08-15 |
| 16 | Is the model connected? Check which model is active. | 08-19 |
| 17 | Create an air terminal schedule in the model with all these parameters, and do not add any filters. | 08-19 |
| 18 | Which model did you create it in? Two models are open. | 08-19 |
| 19 | How many air terminals are there in the model? | 08-19 |
| 20 | Give me the family name and type name for all these items. | 08-19 |
| 21 | Select any one air terminal of this family so I can check it. | 08-19 |
| 22 | Change the Description of this family to "Supply Air Grille - Rectangular". | 08-19 |
| 23 | Before you change the descriptions, show me a table with the family name, type name and the new description. | 08-19 |
| 24 | How many sheets are there for the duct layout? | 08-19 |
| 25 | In these sheets, how many floor plan views are there? | 08-19 |
| 26 | Fill each element's drawing-number parameter with the number of the sheet whose floor plan shows it. | 08-19 |
| 27 | Create a schedule for the duct accessories like the air terminal one. | 08-19 |
| 28 | Check the Description of the duct accessories. | 08-19 |
| 29 | Are there any round duct accessories? | 08-19 |
| 30 | Check the connector shape of all the placed duct accessories. | 08-19 |
| 31 | These four duct accessories are not on any duct layout plan — are they on a section sheet? | 08-19 |
| 32 | Select all the instances of this louvre family in the model. | 08-19 |
| 33 | Create 10 sheets named after the project, numbered 01 to 10. | 08-20 |
| 34 | Find the louvres that were modelled as mechanical equipment or duct fittings instead of air terminals. | 08-20 |
| 35 | Highlight these in the active view and keep everything else grey. | 08-20 |
| 36 | Record all the parameter values of the selected family before I change it. | 08-20 |
| 37 | Colour all the louvres red and the rest grey — only the ones that are not in the Air Terminals category. | 08-20 |
| 38 | How do I isolate the VCD dampers in this model? | 08-21 |
| 39 | How do I stop ducts overlapping the ceiling? | 08-21 |
| 40 | How many diffusers are there in the model? | 08-21 |
| 41 | How many ducts are there? | 08-21 |
| 42 | Is this duct connected anywhere, or is it open? | 08-21 |
| 43 | Hide all the air terminals. | 08-21 |
| 44 | Make the dimension in room 4. | 08-23 |
| 45 | When you dimension a wall that has a door, include the door, and give one overall dimension for the wall. | 08-23 |
| 46 | There are lots of generic models — list their family names. | 08-23 |
| 47 | Support hide — every family whose name contains "support". | 08-23 |
| 48 | Show only the fire dampers in red and make everything else grey. | 08-23 |
| 49 | Red only VCD, remaining all grey. | 08-23 |
| 50 | When you colour ducts or duct accessories, colour their insulation too. | 08-23 |
| 51 | Show me the VCD that has no insulation. | 08-23 |
| 52 | Zoom to that VCD. | 08-23 |
| 53 | Are there any clashes between the model's own elements, not with linked models? | 08-23 |
| 54 | Show me all the disconnected items. | 08-23 |
| 55 | Place sprinklers in all the rooms. | 08-23 |
| 56 | How many sprinklers do I need? | 08-23 |
| 57 | Add a view filter for this pipe system and make it red. | 08-23 |
| 58 | Add a solid fill pattern to that pipe system filter. | 08-23 |
| 59 | Size the pipes. | 08-23 |
| 60 | Create the piping for all the rooms. | 08-23 |
| 61 | In all views, turn off the Levels annotation category in Visibility/Graphics. | 08-24 |
| 62 | Place a door in every room, like the one I placed. | 08-25 |
| 63 | Move all the FCUs from 2400 to 2700. | 08-25 |
| 64 | Move the FCU from the centre of the room to the door side — the door side is the return side. | 08-25 |
| 65 | Add the diffusers at 2100 in a 2 x 4 grid — 8 diffusers, 4 supply and 4 return. | 08-25 |
| 66 | Connect all the supply diffusers to the FCU. | 08-25 |
| 67 | Create an air terminals filter, give it colours, and apply it to the active 3D view. | 08-25 |
| 68 | Centre the FCU between the left and right walls. | 08-25 |
| 69 | Create a 600x600 type in both the supply and the return diffuser families. | 08-25 |
| 70 | In the corridor, add one FCU and air terminals — one supply, one return — on the corridor centreline, with the FCU at either end. | 08-25 |
| 71 | Put each reducer 200 mm after the branch takeoff. | 08-25 |
| 72 | The air terminal layout is wrong — make it zig zag: one supply, one return, alternating. | 08-25 |
| 73 | Make the ducting for the corridor. | 08-25 |
| 74 | Set the airflow of all the diffusers to 200 L/s. | 08-25 |
| 75 | Ping the Revit model. | 08-26 |
| 76 | Find the VCDs. | 08-26 |
| 77 | Tag all the ducts in the active view. | 08-26 |
| 78 | Tag the ducts again using this tag family. | 08-26 |
| 79 | This element cannot be deleted or moved in a BIM 360 model that is already synchronised — find out why. | 08-27 |

---

## C. His grayout standard

His view standard for an MEP coordination drawing: **the architecture and structure flattened to grey, the
services forward in black, insulation a quiet dashed wrapper, rebar off.** He set the values on real work
and said on 2026-08-10 that they should not be asked again for another model — which in Heron means
**offered by name once per model**, not applied unasked ([D-83](../../../DECISIONS.md)).

| Layer | Line colour (RGB) | Fill (RGB, solid) | Line weight | Transparency |
|---|---|---|---|---|
| Background — everything not named below | 150,150,150 | 200,200,200 | 1 | 0% |
| Walls | 150,150,150 | 200,200,200 | **2** | 0% |
| Floors | 240,240,240 | 240,240,240 | 1 | 0% |
| Doors | 200,200,200 | 200,200,200 | 1 | **100%** |
| Windows | 150,150,150 | 200,200,200 | 1 | **100%** |
| Services — ducts, pipes, cable tray, their fittings and accessories, flex | 0,0,0 | (Revit discards it) | **3** | **80%** |
| Insulation — duct and pipe | 80,80,80 | (Revit discards it) | 1 | **100%** |
| Mechanical Equipment | 0,0,0 | 128,128,128 | **3** | 0% |
| **Off** | Structural Rebar, Structural Rebar Couplers | | | |

Insulation also takes his office line pattern `MEP_Hidden_Short_Dash`; services and background take a
solid line.

**Three values that look wrong and are not:**

1. **Windows are 150 while doors are 200.** A window sits inside a wall whose fill is already 200, so a 200
   line disappears into it; a door breaks the wall, so a light line still reads. The general rule: *anything
   sitting inside a greyed host must not take the host's fill colour as its line colour.*
2. **Floors are lighter than walls.** Floors are the biggest surface on a plan; near-white puts them behind
   the walls instead of competing with them.
3. **Everything drops to weight 1 before the services come back up to 3.** Flatten first, then rebuild the
   hierarchy.

**What Revit throws away, every time** — to be reported, never hidden: a category that cannot be cut ignores
cut settings; ducts and pipes also ignore surface fill, so services come out as coloured lines; rooms,
areas and spaces take no category override at all; sub-categories hold only line colour and weight;
transparency shows only in shaded, consistent-colour and realistic views. **So never report "all grey with
solid fill" — report what stuck.**

**Open choices — decision D4, asked one at a time the first time each one matters:**

- **Service sub-categories** (rise, drop, centreline and their kind) stayed grey while their parent went
  black. Follow the parent to black, or leave them grey?
- **Duct linings** — treat them like insulation? He was asked twice and did not answer.
- **Conduits, conduit fittings, wires** — still background grey. Bring them forward like cable tray?
- **Air terminals and sprinklers** cannot hold a fill by category override — does he want them to read
  solid, which needs a view filter or a per-element override?

---

## D. His practice values

**The defaults of his own tools**, decided once on real work. **They are his office's values, not a code** —
where a code or a project specification governs, that wins. Offered by name, never applied silently:
*"your office uses 300 mm here — the same on this job?"*

### Tagging

| | |
|---|---|
| Spacing between stacked tags | 12 mm on paper |
| Tag offset from the element | 300 mm |
| Shortest run worth tagging | 1000 mm |
| Filter by size before tagging | on — minimum width 100 mm, no height minimum |
| Use a leader | on |
| Fixing tag clashes: passes | 5 (allowed 1–50) |
| The furthest a tag may be pushed | 50 mm — beyond that, **report it** rather than drag it across the drawing |
| Clash tolerance / minimum gap between tags | 1.5 mm / 5 mm |
| Mark tags it could not fix, in the view | on |
| Skip vertical runs | on |

### MEP openings and sleeves

| | |
|---|---|
| Merge crossings closer than | 100 mm |
| Cut-out margin — pipe | 20 mm, **round** |
| Cut-out margin — duct and cable tray | 25 mm, **rectangular** |
| **Include the insulation in the opening size** | **on** |
| Look in linked models | off by default |

### Connecting MEP runs

| | |
|---|---|
| Bend angle | 90° (allowed 5–90°); if it will not build, try 45, 30, 60, then 90 |
| Copy insulation and lining onto new pieces | on |
| Copy the workset | on |
| Add a transition when sizes differ | on |
| Allow ends that are not parallel | on |

### Dimensioning

| Grids and levels — paper millimetres, so they scale with the view | |
|---|---|
| First row offset | 8 mm (allowed 0–200) |
| Gap between rows | 6 mm |
| Individual row and overall row | both on — skip the overall row when there are only two datums |

| MEP runs | |
|---|---|
| Shortest run worth dimensioning | 1000 mm |
| Skip vertical runs | on |
| Row spacing / padding | 8 mm / 6 mm on paper |
| Search band either side of the line | 150 mm |
| One chain, one side, width not included | yes |
| Skip runs already dimensioned | on |

### Cropping a view to its content

| | |
|---|---|
| Margin around the content | 300 mm; annotation crop 100 mm |
| Include linked models | on |
| **Include grids and levels** | **off** — they reach far past the building |
| Ignore hidden categories; rectangular crop only | on |

### Duct sheet-metal allowances — over the bare sheet weight

| Seam | Joint | Flange | Fittings | Wastage | Reinforcement, only where the gauge needs it |
|---|---|---|---|---|---|
| 3% | 2% | 4% | 10% | 5% | 5% |

**+24%** without reinforcement, **+29%** with it.

### Odds and ends

| | |
|---|---|
| Revision cloud offset from the elements | 50 mm |
| Workset for links and CAD imports | "Linked Models" |

### From his own words, recorded with the rules above

| | |
|---|---|
| Reducer after a branch takeoff | 200 mm of straight duct, then the reducer ([§A](#a-his-site-words--52-entries-approved-2026-09-23) #51) |
| Supply and return terminals | alternating, never all the supply on one side ([§A](#a-his-site-words--52-entries-approved-2026-09-23) #48) |
