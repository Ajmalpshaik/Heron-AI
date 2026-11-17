# FRAGMENT-ISSUES archive — section 5b, rows 1326 to 1350

> **Finished rows, moved out of the live register.** These are rows from section 5b of
> [`FRAGMENT-ISSUES.md`](../FRAGMENT-ISSUES.md) — Heron's own defects found by reading the repository — whose state said they were
> finished when they were moved. Each still has its line in the register: the same number, a
> one-line title, the opening of its state, and a link to its full text here.
>
> **Nothing below was rewritten** except its links, which were re-pointed one folder deeper and
> each checked to reach the same file it reached before. Where a row disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win** — a row records what was true on
> the day it was written.
>
> **Nothing new is written here.** A new defect goes in the register, and a row here that turns
> out not to be finished is reopened **there**, on its own line.
>
> Rows 1326 to 1350 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-1329

*Moved from the register on 2026-11-17.*

**Defect.** **`PLACE_COLUMN_REBAR` PLACED NOTHING IN 77 ROUND CONCRETE COLUMNS ON REVIT 2020 - REVIT REFUSED A BAR IN EVERY ONE WITH ITS OWN *"An internal error has occurred."*** Found 2026-10-22 night on Revit 2020.2.1, session 60544, in *Structure Night Test* (a copy of Revit 2020's `rst_advanced_sample_project.rvt`): the Structural Columns selected in *EngineeringPlan: 01 - Entry Level* - 77 of them `M_Concrete-Round-Column: 450mm` (concrete; the model also holds 7 of 750mm and 7 steel `UC356x368x129`) - with `mainBars` *4 x 16M*, `ties` *10M @ 150*, `tieHook` *Stirrup/Tie - 135 deg.*, `cover` 40, rolled back (`validate --write`): `placed` 0, `skipped` 77, each *"Structural Columns '450mm' (id 122477): Revit refused a bar - An internal error has occurred. - every bar made in it was removed again"* (the whole sentence read by running the card's own source with one joined string added). The negative (*4 x T16*) was refused naming the loaded bar types, as written. The fragment's code takes a round section (a `CylindricalFace` radius) as well as a rectangle. **IT IS THE TIES:** the same run with `ties` *none* placed *"280 rebar set(s) placed and read back in 70 column(s)"* - four main bars in each concrete column - and skipped the 7 steel columns as *not a rebar host*. **Not checked:** which part of a round tie Revit refuses (its shape, the 135-degree hook, the 10M bar's bend), and whether a rectangular concrete column takes ties on 2020 (none is in the model).

**State.** **MOVED TO ISSUE [#949](https://github.com/Ajmalpshaik/Heron-AI/issues/949) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not fixed. NARROWED 2026-10-23 ON REVIT 2024.3 (session 83588):** the same 2020 model opened in 2024 (upgraded in memory) gave the SAME refusal for all 77 round columns, *"An internal error has occurred."* - and again with `tieHook` *none* (*"The ties were placed with no hooks, as asked."*, then every column refused) - so it is neither Revit 2020 nor the hooks: it is the round hoop the code builds, two half-circle `Arc`s as one stirrup/tie set. On 2024 the RECTANGULAR route took ties: Snowdon Towers Structural's 13 `Concrete-Rectangular-Column: CC24x24`, `4 x #8` with `#3 @ 150` ties, *"36 rebar set(s) placed and read back in 12 column(s)"*, one skipped as already holding rebar. Which call throws for the hoop (`CreateFromCurves`, the layout, the regeneration) is not read. `place-column-rebar` stays DRAFT: the main-bars-only run passed both legs, but a card signed on it would say nothing about the ties that fail. The next step is a probe of one round column's tie on 2020 - the shape and hook - against the same tie on 2024.

---

### Row 5b-1335

*Moved from the register on 2026-11-17.*

**Defect.** **A WORK-PLANE-BASED NESTED PART PLACED ON A PLANE WHOSE NORMAL POINTS THE WRONG WAY CAME IN FACING INWARD AND UPSIDE DOWN, AND NO HERON TOOL COULD TURN IT: `FLIP_ELEMENTS` ONLY FLIPPED HAND AND FACING, AND A ROTATION WAS REFUSED BY REVIT.** Seen 2026-10-24 on Revit 2020. The work-plane-based nested family *TRG_Duct Accessory_Damper Quadrant Handle* was placed by `PLACE_NESTED_FAMILY` on a front reference plane made by `CREATE_REFERENCE_PLANES`. Every plane that tool makes has normal +Y, so on the front (-Y) side the part came in facing inward and upside down. `ROTATE_ELEMENTS_ABOUT_AXIS` was refused by Revit - *"Can't rotate element into this position"*. A rolled-back probe setting `IsWorkPlaneFlipped = true` fixed both: bounding box Y -345..-301 and Z -25..121, following Width. `FLIP_ELEMENTS` version 2 calls only `flipHand` and `flipFacing`, which cannot do it, so the owner flipped it by hand. Not known: whether `CREATE_REFERENCE_PLANES` should make a front plane with its normal pointing out (-Y) instead - nothing measured what that would change for the parts already locked to its planes.

**State.** **MOVED TO ISSUE [#902](https://github.com/Ajmalpshaik/Heron-AI/issues/902) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - the card is PROVEN at version 3 since, and its proof does not cover this row.** Read 2026-11-11 in a cloud container (no Revit): the proof block (2026-11-02, P1102 MEP, Revit 2020) ran `flipHand` alone - `flipWorkPlane` *not given - false*, `planeFlipped 0 item(s)` in both cases - so the work-plane flip this row added is still owed a case ([Group JB](../needs-checking/group-jb.md)). *At DRAFT* below is out of date. Was: **OPEN - widened in source 2026-10-24 at DRAFT, version 3; a Revit run is owed ([Group JB](../needs-checking/group-jb.md)).** `flip-elements` version 3, no new capability and no new required input: the optional `flipWorkPlane` (absent = false, version 2's run) asks `CanFlipWorkPlane`, toggles `IsWorkPlaneFlipped`, and after one regeneration reads back the flag, `FacingOrientation` and the bounding box against what they were - one line each in the new `workPlaneFlips`. One that cannot is named in the new `cannotFlipWorkPlane` and never forced; a throw rolls the whole call back. `brain/skills/family-creation.yaml` step 11 now points a nested part that came in upside down at it. Compiles for 2020 and 2027 (`tools/check-fragments-compile.py`). The plane's normal itself is left as it is. Before: **OPEN - recorded, not fixed.**

---

### Row 5b-1340

*Moved from the register on 2026-11-17.*

**Defect.** **AN ADD-IN AGENT CANNOT BE PROMOTED WITHOUT STALING ITS OWN PROOF: ITS STATUS LIVES IN ITS `.cs` HEADER, AND `prove-agent.py`'S FINGERPRINT HASHES THE WHOLE FILE, HEADER INCLUDED.** Found 2026-10-25 while the fifteen agent proofs were re-signed. A fragment keeps its status in `fragment.yaml`, outside the code its proof fingerprints; an add-in agent keeps `Heron-Status:` inside the file `fingerprint()` hashes, so editing it from DRAFT to PROVEN makes `prove-agent.py check` answer STALE for the proof that justified the edit. One file can also hold several agents' operations (`RevitOperations.cs`), so any change to one stales the others. This is part of why no agent in `brain/heron_agents.py` has ever read PROVEN.

**State.** **MOVED TO ISSUE [#913](https://github.com/Ajmalpshaik/Heron-AI/issues/913) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - needs the owner.** Either the status moves out of the header, or the fingerprint leaves the header out, or the registry reads a current signed proof as the status; which is a decision, not a repair.

---

### Row 5b-1347

*Moved from the register on 2026-11-17.*

**Defect.** **"ADD FOUR MORE FLOOR LEVELS" MOVED FROM `CREATE_LEVELS` (FRG-DOC-008, PROVEN) TO `CREATE_LEVEL` (FRG-ELE-022, PROVEN) WHEN THE FOUR MASSING CARDS OF [GROUP JO](../needs-checking/group-jo.md) WERE ADDED.** Measured 2026-10-28 in a cloud container with `tools/check-routing.py` on the `model` backend, main (605 fragments) against the branch (609): the routing row in `create-levels`' card is reached on main and NOT REACHED on the branch, `CREATE_LEVELS` second. Taking each of the four new cards out alone, `tools/score-routing.py` still gave `CREATE_LEVEL` - so no one card's words do it; the four together move a near-tie between two level cards. Both write levels, so the answer is a sibling, not a crossing. The same run moved *"T10 at 150 both faces of these walls"* (claimed by `PLACE_AREA_REINFORCEMENT`, NOT REACHED on main too) from `PAINT_FACES` to `CREATE_WALLS_BY_FACE` - wrong before and wrong after. Neither was reworded to win back its rank.

**State.** **MOVED TO ISSUE [#956](https://github.com/Ajmalpshaik/Heron-AI/issues/956) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired.** The lexical backend CI runs, and the owner's `model+rerank`, were not asked here.

---

### Row 5b-1348

*Moved from the register on 2026-11-17.*

**Defect.** **`tests/test_skill_proving.py` JUDGED ONE STEP'S NEED BY ANOTHER STEP'S LINE WHEN TWO STEPS OF ONE METHOD SHARE A NEED'S NAME.** Seen 2026-10-29 in CI on pull request #763 (job "Every suite, and NOT RUN kept apart from FAILED"), and again locally: *"massing-study/create-opening's `host` says to type `selected`"*, twice. The check in section 5 reads every line of the emitted job file that starts `host:`; `massing-study` declares both `CREATE_OPENING`, whose `host` is ONE element (hint *"type `selected`"*, correct), and `CREATE_CURTAIN_SYSTEM` (FRG-ELE-127, from #764), whose `host` is a string (hint *"string"*, correct too) - so the first was failed on the second's line. No method needed two steps with one need name until this one.

**State.** **FIXED 2026-10-29**, tests only: each need is read in its own job's block - the lines from its `- fragment:` to the next - and a by-hand step with no job of its own fails by name. Shown to fail first: with `tools/prove-skill.py`'s one-element hint changed to *"type its name"*, the narrowed check fails on six steps across `check-connectivity`, `clash-reroute` and `massing-study`, `CREATE_OPENING`'s `host` among them; with the hint restored it passes. Run in a cloud container.

---
