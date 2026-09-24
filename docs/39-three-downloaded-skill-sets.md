<!--
Heron-Agent:  none
Heron-Step:   18
Heron-Status: DRAFT
Heron-Since:  0.1.0
Heron-Layer:  brain
See docs/29-metadata-standard.md
-->

# 39 — Three downloaded skill sets, studied

**Asked 2026-09-24.** The owner downloaded a folder of skill files, said one of them looked like Heron,
and asked whether they could be converted to Heron's style, or studied and made part of it.

**The short answer is: partly.** One of the three sets is close to Heron: the working notes somebody kept
while driving Revit through **another product's Revit connector**. The other two are prompt frameworks
for running an **architecture office**, and that is not what Heron is ([`AGENTS.md`](../AGENTS.md): a BIM
platform for a Revit modeller). **Nothing was converted, because nothing is copied** - no tool name, no
sentence, no file name ([D-25](DECISIONS.md), [31 §2](31-studying-the-existing-libraries.md)). What came
across is what 31 says travels: the mechanism and the scar.

| What it produced | Where it lives now |
|---|---|
| **A gap Heron had already named, filled.** `CREATE_ROOF` makes a flat roof, and its own routing says a slope is *"a separate job nothing in the library covers yet"*. The notes carry the method: the two long eaves for a gable, every edge for a hip | [`SLOPE_ROOF_EDGES`](../brain/fragments/slope-roof-edges/fragment.yaml), DRAFT - §3 lesson 11 |
| **A defect in Heron, exposed by one of their scars, and half fixed.** Twenty fragments find a parameter by a typed name the way Autodesk documents as *picking at random* when two parameters share the name. **The ten that change the model now refuse such a name and say so;** nine of them were PROVEN and are DRAFT again until re-proved. The ten that only read are recorded | [Row 5b-203](FRAGMENT-ISSUES.md#) |
| **A method, written down.** Levels, walls, slab, rooms, ceilings, roof, and the count that proves each, in the order they have to happen. Every step is a capability Heron already has | [`building-shell`](../brain/skills/building-shell.yaml), DRAFT |
| **Things to measure** on a scratch model - the new fragment's proof, the same-name trap on a real curtain wall, and two of the notes' claims that Heron can settle by running a command | [Group AP](needs-checking/group-ap.md) |
| **What the connector does that Heron does not** | §5 - left unbuilt on purpose |

---

## 1. What was handed over

Ten files, eight of them different: two pairs are byte-for-byte identical, by MD5.

| Set | What it is | Files | Licence stated | Worth to Heron |
|---|---|---|---|---|
| **1. A connector's working notes** | How-to guides for driving Revit through another company's MCP connector: working conventions, curtain walls, a house with a roof, site context, a tower. A combined file adds structure, MEP, documentation and coordination sections that **their own author marks as never run against Revit** | 6 | none | **The one worth studying.** Its field-tested half records real failures and what was done about each |
| **2. An office master prompt** | One long prompt that routes an architecture firm's work - site, code, zoning, design, Revit, rendering, permits - through 46 named modules, each one paragraph ending in the same sentence | 1 | none | Principles. Most are Heron's already; two contradict Heron's rules; one is worth recording |
| **3. A studio toolkit's contents page** | The index of a 46-skill toolkit: product and environmental data, project management, one city's zoning and permits, specification writing. The skills themselves are not in the folder | 1 | MIT | **Out of scope.** None of it is Revit |

**The names are left out on purpose.** They were handed over as reference, and Heron takes no names,
dependencies or branding from reference material. What was read, and when, is enough to stop it being
studied twice.

## 2. The method

[31 §3](31-studying-the-existing-libraries.md) Rule 0 first - **does Heron already cover it?** - answered
the way 31 §1 learned to answer it: *name the Heron capability that does the job, or the claim fails*.
Matching by resemblance was measured there and does not work.

Sets 2 and 3 were read in full and judged on one question: is any principle in them missing from Heron?
Set 1 was read lesson by lesson, and **every lesson was checked against Heron's code, not against its
documents.** That is how §3 lesson 6 turned into a defect: the documents already had the rule, and the
code that writes by a name did not keep it.

---

## 3. The connector notes - every field-tested lesson, and what Heron has

| # | The lesson, in Heron's words | Heron | The evidence |
|---|---|---|---|
| 1 | Read the model before building in it: its levels, the exact type names, what is already there | **Held** | `LIST_LEVELS`, `SELECT_TYPES`, `heron_context`; every skill's `preconditions` |
| 2 | Sizes arrive in the modeller's units and are converted once, on the way in | **Held, stricter** | [D-71](DECISIONS.md): every typed length is millimetres. [D-20](DECISIONS.md): the conversion is arithmetic. `WRITE_ELEMENT_PARAMETERS` hands a value to Revit's own parser rather than converting a number whose quantity it does not know |
| 3 | **A roof call failed with *"Value cannot be null"* from a 3D view, and the notes blame the view** | **Held - and Heron isolated a different cause for the same message** | See below. [AP4](needs-checking/group-ap.md) settles the view question for Heron |
| 4 | A roof boundary is its corners; repeating the first point at the end breaks it | **Held** | `CREATE_ROOF` closes the loop itself. A repeated point makes an edge of no length, and it refuses with a sentence that says so instead of failing inside Revit |
| 5 | Change the type once rather than every instance | **Held** | `DUPLICATE_TYPE`, then `WRITE_ELEMENT_PARAMETERS` on the new type - the routing in `write-element-parameters/fragment.yaml` |
| 6 | **Two parameters can share a name on one element.** A curtain wall type shows *Interior Type* under Vertical Mullions and again under Horizontal Mullions, and setting it by name only ever reached one of them | **The rule was Heron's and the code broke it. The ten fragments that change the model now keep it** | [D-54](DECISIONS.md) §3 and `RevitParameters.cs` both state it; twenty fragments did not follow it - [row 5b-203](FRAGMENT-ISSUES.md#), with the ten that read still open |
| 7 | Read back what was made; a call that returned is not a call that worked | **Held, stricter** | [D-30](DECISIONS.md); `snapped` in `WRITE_ELEMENT_PARAMETERS`; `measuredMm` in `CREATE_ROOF` |
| 8 | The connector cannot build a family's geometry | **Heron is ahead** | [`family-creation`](../brain/skills/family-creation.yaml) and its nine fragments, DRAFT, [Group AN](needs-checking/group-an.md) |
| 9 | When a route cannot do the job, say so and give the shortest way by hand; do not keep retrying | **Held** | a fragment refuses in a sentence, and the chat tool prints a refusal as a refusal rather than as *"it ran"* - `heron_mcp_server.py`, where it says a refusal is the case the check exists for |
| 10 | When a tool seems to be missing, report the connector's version first | **Held** | `heron_version`, `heron_compatibility` |
| 11 | **A pitched roof: the two long eaves carry the slope for a gable, every edge for a hip, and the pitch is the modeller's** | **Missing - and Heron's own words said so** | `CREATE_ROOF`'s routing: *"slope this roof -> NOT HERE"*. Built as `SLOPE_ROOF_EDGES`, DRAFT. It carries one scar of its own, read off Autodesk's reference: the per-edge property named *SlopeAngle* is a rise over a run, not an angle |
| 12 | Set a curtain wall's grid spacing and its mullions on the type | **Missing** | `REPORT_CURTAIN_ELEMENTS` reads a curtain wall; nothing writes its grid or its mullions. By lesson 6 those are exactly the settings whose names repeat, so a write by name now refuses them (row 5b-203), and the route that can work reaches them by Revit's own parameter ids - §5 |
| 13 | Streets and pavements as thin floors and kerbs as low walls, each set at a height where nothing floats | **Possible today, not written down** | `CREATE_FLOOR`, `CREATE_WALL`. The ground surface itself is §5 |
| 14 | A tower lives or dies on its levels: make every level first, one floor per level, then count them | **Held, and now written down** | `CREATE_LEVELS`, `CREATE_FLOOR`, `COUNT_ELEMENTS` - the order in [`building-shell`](../brain/skills/building-shell.yaml) |

### Lesson 3 is the one to keep

**Heron met the identical message from the identical Revit call** - `NewFootPrintRoof` - on 2026-09-19,
on a model with a real roof type, a real level and a clean four-point boundary. It was isolated there: the
`out` curve array was passed in as null. Autodesk's own sample creates the array first, and
[`create-roof/impl/any/fragment.cs`](../brain/fragments/create-roof/impl/any/fragment.cs) now does, with
the reason beside it.

The notes attribute the message to the active view, and build a rule and a fallback on that. The same
notes then plan for the call failing **in a plan view, after a restart** - which is what a cause other
than the view would look like. Heron's roof takes its level as an input and never asks the view for
one, so the view should not matter to it; its proof does not record which view was open, so that is **not measured**,
and AP4 measures it.

**A scar is the most valuable thing in anybody else's notes, and its diagnosis is still a hypothesis until
something isolates it.** 31 §1 learned the mirror of this from a possibility nobody had run. This is a
failure nobody had isolated, and a rule was built on the guess.

### The four sections their author never ran

Read, and trusted exactly as far as their author trusted them. Most of what they say Heron does already:
a sanitary pipe needs a fall (`SET_MEP_SLOPE`); rooms and spaces are separate things
(`REPORT_ROOM_SPACE_DATA`); a room needs a closed boundary (`SELECT_UNENCLOSED_ROOMS`); a category
override belongs to a view and an element override to one element (`SET_CATEGORY_GRAPHICS`,
`OVERRIDE_GRAPHICS_IN_VIEW`).

**One claim is wrong about Revit, which is what an untried section costs.** It says a revision cloud does
not reach a sheet's revision schedule unless the revision is also assigned to that sheet by hand. A cloud
on the sheet, or in a view placed on it, puts the revision there by itself - in Revit's own *Revisions on
Sheet* dialog that revision is ticked and cannot be unticked. Assigning by hand is for a revision with no
cloud.

**One claim is worth measuring, because if it is true it reaches every Heron fragment that creates
anything:** that a new element lands, silently, in whichever design option is active. Heron can read the
options (`REPORT_DESIGN_OPTIONS`) and move elements between them (`SET_DESIGN_OPTION`), and nothing
records where a creation lands. [AP9](needs-checking/group-ap.md) makes one wall with an option active
and reads where it went.

---

## 4. The office prompt and the studio index

**Already Heron's, in stricter form:** read before writing; plan before acting; check that the model is
the one meant before changing it (`revit_use_this_model` pins one); read back afterwards; never call a
design compliant ([`brain/instructions/host.chat.yaml`](../brain/instructions/host.chat.yaml)); say which
professional's decision a thing is; keep evidence apart by what it is - PASS, FAIL, NOT RUN, NEEDS REAL
REVIT, and [D-52](DECISIONS.md)'s line between what was found and what was turned down.

**Contradicts Heron, and rejected:** filling a gap with a reasonable assumption and labelling it, and
turning a client's adjectives into design decisions. Heron asks for every input and never assumes one
([D-33](DECISIONS.md)); a word it does not recognise is looked up or asked about, never guessed
([D-34](DECISIONS.md)).

**Not Heron's job, so not a gap:** design language and style, rendering, zoning and permits, product and
environmental data, specifications, project scheduling. The host decides what the modeller meant
([D-01](DECISIONS.md)); Heron does the Revit.

**The one idea worth recording: grade a change by what it reaches, not by its verb.** The prompt ranks a
change by what it touches - a level, a grid, the coordinates, an issued sheet - rather than by whether it
reads or writes. Heron's `risk:` grades a fragment, not a call, and **the same `MODIFY` on one wall and on
a wall type are different sizes of change**: the type reaches every placed instance of it. Heron says so
in one place already - `SET_COMPOUND_LAYER_WIDTH`'s purpose puts it first, as the thing to say before
running - and `WRITE_ELEMENT_PARAMETERS` on a type answers *written 1* all the same. Recorded here and not
built, because it changes what every write answer says - that is a decision, not a fix.

---

## 5. Not built, on purpose

What the connector's tools reach and Heron has no route for. **Each waits for a job that needs it** -
[31 §5](31-studying-the-existing-libraries.md): the library grows when a real job asks - and the owner is
asked which, one at a time.

| The job | What makes it more than one call |
|---|---|
| Curtain wall grid lines and mullions | The settings repeat their names (§3 lesson 6), so a fragment reaches them by Revit's own parameter ids, never by a typed name |
| A wall's profile - an arched head | |
| A ground surface | Revit 2024 brought in the toposolid in place of the toposurface, so it is two routes across Heron's eight releases |
| Stairs and railings | |
| Wall foundations, and reinforcement | Reinforcement needs its host to exist first |
| Parts, and making an assembly | |
| Point clouds | |
| Masses, and the floors and walls made from one | |
| Painting one face; the material of one layer of a wall | `SET_COMPOUND_LAYER_WIDTH` changes a layer's width and nothing else |
| A door or a window put into one particular wall | **Not from these notes** - [the house plan of 2026-09-22](fragment-issues/a-drawing-set-and-a-house-plan-2026-09-22.md) found it: `PLACE_HOSTED_FAMILY` resolved its host by a type name, never to one wall. A version that finds the wall at a point was being built in another change on 2026-09-24, so this row may already be out of date - read that fragment's own card first |

---

## 6. What this is not

**Not an import.** No file from the folder is in this repository, and none of its sentences. **Not a
proof.** The fragment built from this, the skill written from it and the nine fragments the repair sent
back to DRAFT all owe a run against a real model, and Group AP says exactly which.
