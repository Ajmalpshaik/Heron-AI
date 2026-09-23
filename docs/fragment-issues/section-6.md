# Fragment issues — section 6

> One section of [the register](../FRAGMENT-ISSUES.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## 6. WHAT CANNOT BE RUN AT ALL — 100 fragments, by what they need

Not failures. Heron has no way to receive these inputs yet, so they have never executed a line.

| Waiting on | Fragments | Why not done |
|---|---|---|
| `ElementId` | 21 | Its constructor changed from `int` to `long` at Revit 2024, and nothing in the add-in carries a version `#if` |
| `Element` (one, not a list) | 20 | **Half done, 2026-09-09.** A TYPE resolves by name — `Basic Wall: Generic - 200mm` — and **eight contracts were narrowed off `Element` the same day** so the search is confined to the kind actually wanted. An INSTANCE still cannot be typed in and never will be: `Element.Name` on one returns its type's name, so searching instances matches every element of that type. The other 14 want *that one there* and need the selection, not text |
| `XYZ` | 14 | **Done, 2026-09-09 — [D-67](../DECISIONS.md).** A point is three numbers in MILLIMETRES, `"5000, 3000, 2800"`; several are separated by semicolons. The unit was not chosen so much as read off what the library already said — `HeronUnits` converts nothing else, 59 caller values are named `...Mm`, and two fragments were already splitting points into `centreXMm`/`centreYMm` because they could not send one. Each ordinate is bounded at 100 km, and a direction needs no separate rule: scaling all three components alike does not move a vector, checked against all six that take one |
| `FamilySymbol` | 4 | **Done, 2026-09-09.** Resolved by name among family types, on the same mechanism as `Element` — no rule of its own was needed. `set-sheet-title-block` and `distribute-along-run` were the two waiting on it |
| `View3D` | 3 | A narrower view lookup |
| element/id collections | 9 | Same as the two above |
| `OverrideGraphicSettings` | 3 | A structured value, not a name |
| `IDictionary<string, double>` | 1 | **Found 2026-09-15 — row 98.** `check-minimum-clearance.rules` carries a different clearance per category, which is the normal MEP case rather than the advanced one. `FromRequest` refuses `IDictionary` by name and the need cannot be omitted, so the fragment has never run a line. A syntax decision of the same class as [D-72](../DECISIONS.md)'s four |
| everything else | 26 | One rule each |

### THE SHAPES THAT STOP A JOB BEING ARRANGED, AND ONE PROPOSED RULE EACH — 2026-09-19

**Proposals, not decisions. Every row below is the owner's to accept or send back.**

Measured with `python tools/generate-jobs.py` on 2026-09-19: **79 DRAFT fragments have no run record,
35 can be arranged as a job, and 44 cannot.** The `everything else | 26` row above counts a different
population — every fragment in the library that cannot run at all — so the two numbers were never in
conflict. **The table above is a SNAPSHOT and should be read as one:** two of its rows say `Done` in
their own text (`XYZ` at D-67, `FamilySymbol`), and the resolver has since accepted several more of
the shapes it lists, so its total no longer describes a live backlog. The 44 below is derived from the
resolver as it stands today, and it is the one that matters when arranging a proving session, because
it is the list of things that will not go in a job file tomorrow morning.

The 44, by what actually stops each one (a fragment can be stopped by two things at once, so these
sum to more than 44):

| What stops it | Count | Is a rule missing? |
|---|---|---|
| A shape with no way to type it | 10 | **Yes — proposed below** |
| Wants ONE particular element | 9 | No. The rule exists; see *the nine are not waiting on a rule* |
| Takes nothing but the model | 6 | No. Prove with `validate`, one at a time (D-53) |
| Wants a value from another fragment | 5 | No. It is a setup chain that does not exist yet |
| `risk: PUBLISH` | 5 | No. A phase decision, not a shape one |
| `risk: ADMIN` | 4 | No. Same |
| Two needs, one selection | 4 | No. Recorded 2026-09-09; one selection cannot fill two sets |
| Wants a FACE | 4 | **No, and never. See the last part of this section** |

---

#### Four ways a value can reach Revit, and every row below is one of them

This is the frame the proposals sit in, and it is worth having before the table because it is what
decides which rule a shape gets. It was read off what `FromRequest` already does rather than invented.

1. **NAME IT and let Revit look it up.** A view, a level, a wall type. Like typing into the Type
   Selector — the thing already exists in the model and has a name that tells it apart from its
   neighbours.
2. **BUILD IT from numbers, because there is nothing to look up.** A point, a colour, a graphic
   override. Like typing coordinates on the Options Bar rather than picking them: nobody NAMED the
   point 5000,3000,2800 — it is described.
3. **POINT AT IT.** Select it in Revit and pass the word `selected`. For *that duct there*, which has
   no name of its own.
4. **None of the three.** Exactly one thing in the library is here, and it is a face.

The mistake this frame prevents is reaching for (1) when the answer is (2). D-54 said
`OverrideGraphicSettings` could never work "because there is no name to look up" — true, and the wrong
conclusion, because D-72 later **built** one from typed settings. *No name* does not mean *no rule*.

---

#### The six proposals

| Shape | Proposed rule — what you would type | Unblocks, outright |
|---|---|---|
| `RoofType` | `roofType=Basic Roof: Generic - 400mm` — name it, exactly like a wall type | `create-roof` |
| `IList<Element>` | `ducts=Ducts` — name the CATEGORY and let Revit collect the set | `connect-air-terminals`, `check-room-mep-completeness`, `propose-mep-openings` |
| `Line` | `axis=0,0,0; 0,0,3000` — two points in millimetres, semicolon between | `rotate-elements-about-axis` |
| `IList<Curve>` | `curves=0,0,0; 5000,0,0 \| 0,0,0; 0,5000,0` — D-72's pipe, unchanged | `place-line-based-family` |
| `FamilyInstance` | `panel=DB-1` — the panel's own Panel Name | `create-electrical-circuit` |
| `Arc` | **write nothing yet** — see below | nothing |

Six rules, seven fragments. **None of them proves anything** — D-30 is unchanged, and a fragment that
becomes arrangeable has reached the starting line, not the finish.

**`RoofType` — the floor and the ceiling can be named and the roof cannot, and only because nobody
added the line.** `WallType`, `FloorType` and `CeilingType` each have a row in `FromRequest`;
`RoofType` does not. `create-roof`'s own purpose says it was written because *"`CREATE_FLOOR` makes
the slab and `CREATE_CEILING` makes the ceiling; the third member of that family was missing"* — and
the same thing then happened again one level down, in the resolver. The machinery to find one is
**already there**: `RoofType` is a kind of `HostObjAttributes`, and `OneOfClass` — the helper that
already serves that branch — collects roof types along with the rest, so the *lookup* needs nothing
new. What is missing is only the row that matches the word `RoofType` and hands it to `OneOfClass`.
This is the cheapest row in the table by a distance.

**The contract must NOT be widened to `HostObjAttributes` to reach that branch, and it is worth saying
so here because it is the obvious shortcut.** The need's declared type becomes the generated variable's
static type, and `create-roof` passes it to `NewFootPrintRoof(CurveArray, Level, RoofType, out ...)` —
so a widened contract compiles nothing and fails on all eight releases at once. The narrow declaration
is correct and D-54's own rule covers this: *narrower than the fragment can use is a regression dressed
as precision*, and wider than it can use is simply a broken build.

**`IList<Element>` — this is never "a list of things you point at", it is always the SECOND SET.** All
three in the library are the same shape: the first set arrives from the selection, and the second is
the thing to check it against. `propose-mep-openings` says so in its own contract — *"the same shape
FIND_CLASHES uses for its second set: the structure to check against is named by the caller"*. A
modeller already knows this move: it is the two halves of Interference Check, where you pick a
category on the left and a category on the right. So the second set should be named the way the first
set is found — **by category**. `selected` cannot do it: the selection is already spent on set one,
which is the rule recorded on 2026-09-09 and the reason four more of the 44 are stuck.

**SEVERAL categories, not one — and a single-category rule would have failed silently.**
`check-room-mep-completeness` takes `ruleCategories` as a **list**, buckets what it is handed by
`device.Category.Name`, and looks each rule's category up in that bucket. Hand it one category and
every other rule finds nothing — so a room checked for diffusers, extract, sprinklers and detectors
reports three of the four missing **when they are all there**, with no refusal anywhere. That is the
shape this file fears most: a clean-looking answer that is wrong. `propose-mep-openings` is the same —
services pass through walls, floors *and* roofs. So the value is comma-separated, which is what every
other list here already does: `devices=Air Terminals, Sprinklers, Fire Alarm Devices`.

**AND FOR ONE OF THE THREE, A CATEGORY IS NOT ENOUGH — it throws away the very thing the contract asks
for.** `connect-air-terminals` says in its own words that the list is handed in because it *"keeps the
choice with whoever knows which system is which"*. Its body then takes whatever it is given and picks
the **geometrically nearest** duct to each terminal, with no system test of its own — Revit declines
only when the system *types* disagree, which does not separate two supply runs of the same type
passing near each other. So `ducts=Ducts` hands it every duct in the model and quietly restores the
guess the contract was written to prevent: a diffuser tapped into whichever run happens to be closest,
on a plan where two are. **A category alone is the wrong rule here even though it is the right rule for
the other two**, and the missing piece is a way to say which SYSTEM — which is a name, so it is a
D-54-shaped question rather than a new kind of one.

**The part that needs a decision is narrowing.** `ducts=Ducts` collects every duct in the model, and
the proving skill's own first rule is *prove on a small selection* — `FloorPlan: M1` gives 22 ducts
where `FloorPlan: L3` gives 307. So a view-narrowed form is worth having, and `ducts=Ducts in
FloorPlan: M1` is the readable one. It is proposed rather than assumed because it puts a keyword
(`in`) inside a value, which nothing else here does — and because the comma is now spoken for by the
category list above, so the two syntaxes have to be settled together rather than one at a time.

**`Line` — this is already written, and the proof is in an error message.** D-72 built `PointPairs` so
`create-line` could receive `IList<IList<XYZ>>`, and when it is handed the wrong number of points it
says: *"a line is drawn between TWO."* The parser, the millimetre rule, the 100 km sanity bound and
the wording all exist. A `Line` is one pair, run through `Line.CreateBound`. **What it unblocks is one
fragment, not two** — `create-linear-dimension` also wants a face, so it stays dark whatever happens
here, and it would be wrong to sell this rule as the thing that brings dimensions back.

**`IList<Curve>` — the same parser, nothing new at all.** `place-line-based-family` wants the lines to
put pipes, beams or cable trays along, and D-72's pipe-separated pairs are exactly that list. Its
other three needs — a family type, a level and a word — already resolve. Straight segments only, which
is the honest limit: a curved run would need an arc in the list, and that is the next row.

**`FamilyInstance` — one panel, and it is the exception that proves D-54's rule.** D-54 refuses a
particular element because *"Element.Name on one returns its TYPE's name"* — ask a panel its name and
you get `Panelboard - 208V MLO`, which every panel of that type shares. **But an electrical panel
carries a second name that is its own**, the one on the Panel Schedule and on every circuit's Panel
column: `DB-1`, `LP-2`, `A`. The fragment already reads it — `created.PanelName` — so the model's own
word for this panel is the thing the fragment reports back. Resolving `panel=DB-1` against that
parameter is a name lookup in D-54's meaning rather than an exception to it, and D-54's existing
protection applies unchanged: **two panels with the same name is a refusal, never a pick.**

**There is a second, cheaper route here and it is not free.** `panel`'s own comment says it is
*"optional — empty leaves the circuit unassigned"*, the contract never declares `optional: true`, and
[row 101](../FRAGMENT-ISSUES.md#) records that the key is read by nothing **deliberately** — this library refuses rather
than quietly supplying an empty value. So honouring `optional` would unblock this fragment without a
resolver, at the cost of reopening a decision already taken on purpose, and it would prove the
fragment only in its degraded state, with the circuit left off every panel schedule. Both routes are
put here; neither is taken.

**That route is TWO edits, not one, and the cheaper-sounding half is the one that is missing.**
Teaching the binder to honour `optional:` changes nothing here by itself, because **`panel` does not
declare it** — only its comment calls the value optional, and a comment binds nothing. The contract
would have to gain `optional: true` as well. Worth being exact about, because "honour a key three
fragments already declare" sounds like one change to shared machinery and this fragment would sit
exactly where it is afterwards.

**`Arc` — the proposal is to write nothing, and that is the finding.** An arc is not hard. Revit
builds one from three points and the parser for three points already exists, so the rule would be
`arc=0,0,0; 5000,0,0; 2500,800,0` and an afternoon's work. **It would unblock nothing.** Its only
customer in the entire library is `create-angular-dimension`, which also needs `references` — a picked
geometry reference, which can never be typed. Writing it would produce a resolver that compiles, tests, reviews
well and is reachable by nobody. D-67 held `IList<IList<XYZ>>` back for exactly this reason and was
right to; it was written the day somebody asked for `create-line` by name. **The trigger for this one
is the same: the day a fragment wants an arc and is not standing behind a face.**

---

#### One shape is contested rather than missing, and the counter-argument is already in the code

`export-model-to-ifc.schema` wants an `IFCVersion`, and `FromRequest` leaves it out **on purpose**,
next to the one enum it does accept:

> *"`IFCVersion` is deliberately NOT here — its members differ per release, and a name that resolves
> on 2024 and refuses on 2021 is worse than a refusal on both."*

That reasoning is sound and this section does not overturn it.

> **CORRECTED 2026-09-19, before this section was a day old.** It first said this fragment was *"also
> `risk: PUBLISH`, so it is blocked twice"*, and that is wrong. `export-model-to-ifc` declares
> **`risk: MODIFY`**. The sibling directly beneath it in the generator's output —
> `export-model-to-nwc` — is the `PUBLISH` one, and the two were read as one. **So the enum is not a
> question that can wait behind a phase decision: it is this fragment's ONLY blocker**, and the
> correction reverses what the reader should do about it.

**And the correction exposes a real question rather than just fixing a sentence.** Two fragments that
both write a file to disk from the same model carry **different risk levels** — `export-model-to-nwc`
is `PUBLISH` and `export-model-to-ifc` is `MODIFY`. So the moment `IFCVersion` becomes typeable, IFC
export runs for anyone who has switched writing on, while NWC export stays out of reach entirely. One
of the two is mis-declared and it is not obvious from here which: `MODIFY` reads as *changes the
model*, and neither of these changes the model — they emit a file beside it. **Settle the risk level
before writing the enum resolver, not after**, because the resolver is what makes the discrepancy
reachable.

**AND THIS QUESTION IS NOT YET WHERE THE OWNER WOULD LOOK FOR IT.** `tools/owner-queue.py` builds his
queue from [`OPEN-QUESTIONS.md`](../OPEN-QUESTIONS.md) and does not read this file, so a decision recorded
only here is invisible to it — which is how a question that gates a piece of work waits behind a page
nobody is prompted to open. **The session that wrote this was scoped out of `OPEN-QUESTIONS.md` and
did not edit it**, so the row is owed rather than written: *which of `export-model-to-ifc` (`MODIFY`)
and `export-model-to-nwc` (`PUBLISH`) is declared wrong, given that neither changes the model.* It is
named here so the gap is visible instead of silent.

---

#### The nine that want one particular element are not waiting on a rule

They read like the largest group and they are the one place the generator's own message is misleading.
`FromRequest` has accepted the word `selected` for a singular `Element` since 2026-09-13, and it binds
exactly one, refusing when nothing or several are selected. **The rule exists and works.** What is
missing is that a job file runs unattended and cannot reach over and click a duct — so these are
blocked by the *batch runner*, not by the shape. They are proved one at a time, by hand, with the
element selected. Recording them alongside genuine shape gaps makes the wall look taller than it is.

> **CORRECTED 2026-09-19 — SEVEN, NOT NINE, AND THE OTHER TWO CANNOT BE PROVED THIS WAY AT ALL.**
> `measure-distance` needs `first` **and** `second`; `measure-available-fall` needs `upstream` **and**
> `downstream`. Both needs are `Element`, `source: request`, and `OneElement` refuses unless **exactly
> one** thing is selected — then returns `selected[0]` to whichever need asks. **So both sides bind the
> same element**, and the fragment measures a duct against itself: zero distance, zero fall, and a
> reply that looks like an answer. Selecting two refuses instead, which is the safe half of it.
> There is no arrangement of one selection that fills two singular needs — the same wall
> [§3i](../FRAGMENT-ISSUES.md#) recorded on 2026-09-09 for two-SET fragments, arriving here one element at a time. These
> two need per-input picking, or the second value chained from an earlier fragment. **The paragraph
> above was right about seven and wrong about two, and the two it was wrong about would not have
> refused — they would have answered.**

---

#### The face cannot be done, and this is not a rule waiting to be written

Four of the 44 want a `Reference`: `place-family-on-face`, `create-linear-dimension`,
`create-angular-dimension` and `create-radial-dimension`. **The answer is no** — [D-72](../DECISIONS.md)
reasoned it out on 2026-09-14 and `FromRequest` already refuses it in those words rather than falling
through to the catch-all that would end *"not one of them yet"*.

> **THIS FIRST SAID "AND IT IS SETTLED", AND THAT WORD WAS DOING WORK IT HAD NOT EARNED.**
> Corrected 2026-09-19. **D-72 carries no `Status:` line** — every neighbour has one (D-54 *Accepted*,
> D-73 *Proposed*), and D-72 has none at all. So what is true is narrower and worth stating exactly:
> the refusal is **built, shipped and observable** in `FromRequest` today, and the *reasoning* for it
> is written down — but the decision has never been formally accepted, so nothing here may treat it as
> closed. **The physical argument below stands on its own** and does not depend on D-72's status: a
> face has no name, number or coordinate whatever anybody decides. What stays open is the POLICY —
> whether Heron eventually grows a picker — and that is the owner's, not this file's.

Revit identifies a face as **a particular solid, on a particular element, seen in a particular view**.
It is what a mouse lands on. It has no name, no number and no coordinate — the same face has different
identities in two views, and one wall has dozens of them. This is not the (2) case above: a point can
be described by three numbers because a point IS three numbers, and a face is not describable that way
at all. **The keyboard cannot say it.** Picking one needs Revit's own picking, which is a different
mechanism from everything in `FromRequest` and a separate piece of work nobody has costed.

So `create-radial-dimension` can never be arranged as a job, because a picked reference is its *only*
caller value. The other three each want one **plus** something else, which is why `Line` and `Arc`
appear above wearing a rule that will not unblock them. **Dimensions are behind the picking, all three
of them, and no syntax decision reaches them.**

**A FACE IS THE COMMON CASE AND NOT THE WHOLE OF IT — and the difference only matters when somebody
builds the picker.** `place-family-on-face` genuinely wants a face. `create-radial-dimension` does
not: it refuses in its own words — *"A radial dimension witnesses an ARC, and the reference has to be
to one"* — and `RadialDimension.Create` throws on *"a reference that is not an arc"*. A linear or
angular dimension may witness an edge or a datum. So the thing that cannot be typed is a **geometry
reference**, and which KIND is permitted is each fragment's own business. A picker built for faces
alone would leave all three dimension fragments refusing at run time, having looked like it unblocked
them. The refusal above is unchanged and correct for every one of the four; only the scope of the
eventual fix is wider than the word *face* suggests.

---

#### What this does NOT do

**It changes no code.** `FromRequest` lives in `RevitFragment.cs`, which this session does not own, and
every rule above is a proposal waiting on a yes.

**It does not touch the nine at `risk: ADMIN` and `risk: PUBLISH`.** `HeronPermissions.Allows` returns
`false` for anything above `Modify` with a hard `return`, and its comment says that is deliberate so
that adding a caller is *"a deliberate edit to this method and not an accident of an unhandled case"*.
That is a phase decision about what Heron is allowed to do to a live model, not a question about
typing a value, and it should not be settled as a side effect of a proving session.

**It proves nothing.** Seven fragments becoming arrangeable is seven fragments reaching the starting
line. Each still owes a positive case, a negative case and a signature ([D-30](../DECISIONS.md)).

---
