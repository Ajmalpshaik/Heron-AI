# FRAGMENT-ISSUES archive — section 5b, rows 1526 to 1550

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
> Rows 1526 to 1550 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-1527

*Moved from the register on 2026-11-17.*

**Defect.** **`SET_PARAMETER_VALUES_BY_ID` IS PROVEN, AND ITS FILE ROUTE - `csvPath`, VERSION 7 - HAS NO RUN IN A REAL REVIT ON RECORD; `revit_change` KEEPS A CHANGE FROM IT ALL THE SAME.** Found 2026-11-11 reading for the round-trip method. [`SET_PARAMETER_VALUES_BY_ID`](../../brain/fragments/set-parameter-values-by-id/fragment.yaml) carries `heron-status: PROVEN` and a proof dated 2026-11-02 (Snowdon Towers Sample Architectural, Revit 2024) which `check-signatures` does not list as stale for the code as it stands - so D-117 lets a change from any of its routes be kept. That proof's two cases are rows BY KEY (`key=Mark\|in=Doors;201A\|Comments=P1102`, from [`tools/jobs/p1102-round20-revit2024-arch-2026-11-02.yaml`](../../tools/jobs/p1102-round20-revit2024-arch-2026-11-02.yaml)), and its own text reads `csvMode false` and *"csvPath not given"*. The version 7 cases in the card's [`tests/cases.yaml`](../../brain/fragments/set-parameter-values-by-id/tests/cases.yaml) - rows written from a file, an unknown id stopping all, a selection never read, skip and clear, `blankCells` left out, a type parameter, a `UniqueId` of another element, rows and a file together - describe runs nobody has recorded. The card the route came from, `IMPORT_PARAMETER_VALUES`, was proved on a CSV on 2026-10-21 (Revit 2020), but it wrote every row it could and was scoped to the elements handed in; the merge kept *all or nothing* and *the rows alone*, so that proof describes other rules. No NEEDS-CHECKING item held the run before this row: searched 2026-11-11 for `csvPath` beside either card's name in `docs/needs-checking/` and `tools/jobs/`, finding only Group HZ's HZ3, which is the deprecated card's. **Also noted, not fixed:** [`replace-in-place-family`](../../brain/skills/replace-in-place-family.yaml) step 7 still calls the card *"version 7, DRAFT"*; its own first lines say the served list is right where the two differ.

**State.** **MOVED TO ISSUE [#907](https://github.com/Ajmalpshaik/Heron-AI/issues/907) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - NEEDS REAL REVIT: [NEEDS-CHECKING Group NJ](../needs-checking/group-nj.md), on a saved scratch copy.** Nothing was changed: the card's status is its proof's to say, and a proof is not edited from a reading. Until the group has run, [`parameters-csv-round-trip`](../../brain/skills/parameters-csv-round-trip.yaml) says at its head which route has run and which has not, and tells the chat to say so to the modeller and offer a scratch copy first.

---

### Row 5b-1529

*Moved from the register on 2026-11-17.*

**Defect.** **NO HERON CARD MAKES A STRUCTURAL FLOOR: `CREATE_FLOOR` HAS NO STRUCTURAL INPUT, AND ITS CODE PASSES `false` WHERE REVIT TAKES THE FLAG.** Found 2026-11-11 by a fact-check of [the ribbon coverage note](../52-revit-ribbon-coverage.md) (*Build next*, item 1), read in the code, not run. In `brain/fragments/create-floor/impl/any/fragment.cs` the older route calls Revit's `NewFloor` with `false` as its last argument - the code's own comment calls it *"a bool for whether the floor is structural"* - and the newer one calls `Floor.Create` with four arguments (document, loops, floor type, level), none of them a structural flag. The card, version 3 and PROVEN, declares no input for it. Its `impl/` last changed in #777, before #781 built that list, and no register row held the gap: the note's first reading on 2026-11-11 passed the item on the word *structural* found in other cards. `CREATE_FOUNDATION_SLAB` makes a foundation slab, which is another element. **Not checked:** whether a parameter writer can tick a placed floor's *Structural* box, which would be a second step and not this job; which `Floor.Create` overload takes the flag on each release from 2022. `SET_FLOOR_SPAN_DIRECTION` and the reinforcement placers work on a structural floor, so today one has to be drawn or ticked by hand first.

**State.** **MOVED TO ISSUE [#919](https://github.com/Ajmalpshaik/Heron-AI/issues/919) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not built.** An optional structural input on `CREATE_FLOOR`. It widens a PROVEN card, whose proof goes stale with its `impl/`, so it waits for the owner's yes.

---

### Row 5b-1530

*Moved from the register on 2026-11-17.*

**Defect.** **`SET_MEP_INSULATION` STILL REFUSES A FITTING, AN ACCESSORY AND A FLEX RUN, AND NO OTHER CARD INSULATES ONE.** Found 2026-11-11 checking [the ribbon coverage note](../52-revit-ribbon-coverage.md)'s *Build next* item 8 one half at a time, read in the code, not run. In `brain/fragments/set-mep-insulation/impl/any/fragment.cs` anything that is not a duct or a pipe goes into `refused`, named and not skipped - the card (version 1, PROVEN) saying what it does not do, which is not a fault. `grep -rlE 'DuctInsulation\.Create\|PipeInsulation\.Create' brain/fragments/*/impl` names that card alone. #781 built the item's other two halves, each taking fittings: `REMOVE_MEP_INSULATION` ([Group JX](../needs-checking/group-jx.md)) and `SET_DUCT_LINING` ([Group JY](../needs-checking/group-jy.md)). So insulation can be taken off a fitting through Heron and cannot be put on one. The note's own table says Revit's two `Create` calls accept a fitting or an accessory as host - read from the API reference there, not compiled here.

**State.** **MOVED TO ISSUE [#919](https://github.com/Ajmalpshaik/Heron-AI/issues/919) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not built.** Fittings, accessories and flex taken by `SET_MEP_INSULATION`, or a card of their own. The first widens a PROVEN card, so it waits for the owner's yes.

---

### Row 5b-1531

*Moved from the register on 2026-11-17.*

**Defect.** **A SLANTED COLUMN IS IN `PLACE_LINE_BASED_FAMILY`'S CODE AND ON NO CARD'S WORDS, AND NOTHING RECORDS ONE PLACED.** Found 2026-11-11 checking [the ribbon coverage note](../52-revit-ribbon-coverage.md)'s *Build next* item 17. The note's Structure table has *Column (slanted)* as MISSING, *"point-based only"*, and names the call that makes one: `NewFamilyInstance` given a curve and `StructuralType.Column`. `brain/fragments/place-line-based-family/impl/any/fragment.cs` makes exactly that call when `structuralType` is `column` - its refusal reads *"Say beam, brace or column"* - and the card is version 1, PROVEN. So the route was there before the note was written and the note missed it. What is missing is everything a modeller would meet: the card's identity is *"put a beam or a brace along each of these lines"*, its routing table sends *"put a column at each of these"* to `PLACE_FAMILY_INSTANCES`, which places at a point, and `grep -rliE 'slant' brain/fragments/*/fragment.yaml` finds no card that offers one. **Not checked, and NEEDS REAL REVIT:** whether Revit places a column family along a sloping line as a slanted column, what it does with a vertical one, and what the card's proof ran - read its `proof:` block before believing the column leg was ever exercised.

**State.** **MOVED TO ISSUE [#903](https://github.com/Ajmalpshaik/Heron-AI/issues/903) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN.** Run `PLACE_LINE_BASED_FAMILY` with `structuralType=column` on a sloping line, on a scratch model, and record what Revit makes. Whether the card's words then name a slanted column is the owner's: it is a PROVEN card, and a sentence added to it is measured main against branch first.

---

### Row 5b-1532

*Moved from the register on 2026-11-17.*

**Defect.** **NO HERON CARD MAKES A STRUCTURAL PLAN: `CREATE_PLAN_VIEW` TAKES A FLOOR PLAN OR A CEILING PLAN AND REFUSES ANYTHING ELSE.** Found 2026-11-11 checking [the ribbon coverage note](../52-revit-ribbon-coverage.md)'s *Build next* item 17, read in the code, not run. `brain/fragments/create-plan-view/impl/any/fragment.cs` picks its view family type by `ViewFamily.CeilingPlan` when the plan kind says ceiling or RCP and by `ViewFamily.FloorPlan` otherwise, and a kind it does not know is refused: *"say which plan - a floor plan or a reflected ceiling plan"*. The card is version 1, PROVEN. No fragment's code names the structural plan's view family (`grep -rl StructuralPlan brain/fragments/*/impl` finds none). The note names the call - `ViewPlan.Create` with a structural plan type - and #781 did not build it. `PLACE_SPAN_DIRECTION_SYMBOLS` and `PLACE_REINFORCEMENT_SYMBOLS` are placed in a plan, and their records ask what Revit does in a structural one ([Group KM](../needs-checking/group-km.md), [row 5b-1455](../fragment-issues/section-5b-rows-1451-1475.md)), so today that plan has to be there already.

**State.** **MOVED TO ISSUE [#919](https://github.com/Ajmalpshaik/Heron-AI/issues/919) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not built.** A third plan kind on `CREATE_PLAN_VIEW`, refused by name where the model holds no structural plan type. It widens a PROVEN card, so it waits for the owner's yes.

---

### Row 5b-1533

*Moved from the register on 2026-11-17.*

**Defect.** **ARTICLE 9 OF THE CONSTITUTION STILL SAYS *Ask to Keep* IS ON THE RIBBON; [D-150](../decisions/D-150.md) MOVED IT INTO HERON'S SETTINGS WINDOW ON 2026-11-07.** Found 2026-11-11 by a fact-check of the documentation. [`HERON_CONSTITUTION.md`](../../HERON_CONSTITUTION.md), Article 9, reads *"The one recorded exception is the owner's switch for that question (Ask to Keep on the ribbon, off by default)"*. D-150, Accepted, put Changes, Admin, Publish and Ask to Keep in the Settings window, and its point 5 says *"Every sentence that sent the modeller to a ribbon padlock now sends them to Settings"*; it names no Article. What the switch asks and does is unchanged, so the Article's rule holds and only its *where* is out of date. It matters past the page: the MCP server assembles its instructions from the Articles by number, so every chat is told the switch is on the ribbon. The other pages that said so were corrected the same day (README, docs/README, mcp/README, revit/README, SECURITY.md, PROJECT-MAP, the glossary). The Article was not edited: a binding Article is the owner's to amend. Its closing note of 2026-10-18, *"that switch went onto the ribbon"*, is dated history and stays as written.

**State.** **FIXED 2026-11-28 - Article 9 now says *Ask to Keep* is in Heron's Settings, with D-150 named beside D-138, amended by [D-211](../decisions/D-211.md), accepted by the owner.** Was: **MOVED TO ISSUE [#915](https://github.com/Ajmalpshaik/Heron-AI/issues/915) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - the owner's to amend.** A few words in Article 9: *in Heron's Settings* for *on the ribbon*, with D-150 named beside D-138. No session edits the Constitution.

---

### Row 5b-1545

*Moved from the register on 2026-11-17.*

**Defect.** **`brain/regression-matrix.json` HAD NOT MATCHED THE TREE SINCE 2026-10-17 - NINE PROJECTS ADDED, ONE OF THEM SHIPPED - AND NOTHING RUNS THE AGENT THAT WOULD HAVE SAID SO.** Found 2026-11-11 by the same sweep as [row 5b-1544](../FRAGMENT-ISSUES.md#): `python brain/heron_buildmatrix.py` (`HERON-DEV-RGR-014`) exited 1, *"the build matrix no longer matches ... 1 field(s) moved"*. The baseline was last recorded on 2026-10-10; `revit/Heron.Scripting/Heron.Scripting.csproj` arrived on 2026-10-17 (#609) and eight test hosts under `tests/` after it. Every one was a deliberate, merged change, so the agent was right and nobody was listening: no tool, gate, hook or house rule runs it, and its suite reads fixtures, not the committed baseline.

**State.** **MOVED TO ISSUE [#913](https://github.com/Ajmalpshaik/Heron-AI/issues/913) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - the baseline is recorded, the cause is not repaired.** `python brain/heron_buildmatrix.py --update` was run on 2026-11-11 and the diff is the nine project paths and nothing else; the command exits 0 again. **Left:** nothing runs it, so the next project added moves the baseline off the tree again unseen. Whether it joins `check-gaps` or CI's gates is one question with [D-154](../DECISIONS.md)'s - is this agent wanted.

---

### Row 5b-1546

*Moved from the register on 2026-11-17.*

**Defect.** **MEASURED 2026-11-11: OF THE AGENTS THE REGISTER COUNTS AS BUILT, 120 ARE CALLED BY NOTHING BUT THEIR OWN COMMAND LINE AND THEIR SUITES - AND A SIGNED AGENT PROOF NAMES NO REVIT RELEASE.** Two things one inventory found, neither a defect in any one file. **(a)** A call graph was traced from every tool the MCP server serves, the add-in's and the installer's entry classes, every script in `tools/`, the hooks, CI's workflows and the commands the house rules and the Claude Code subagents tell a session to run - structures, never a text match, the way [`check-reachable.py`](../../tools/check-reachable.py) reads a function. 120 built agents are reached by none of them: nearly every row of Import & Migration and of Agent Lifecycle & HR, Standards, GitHub, Workspace, Learning and Skill Lifecycle. Each passes its suites and says `DRAFT`, and the specifications' as-built notes already say *"no tool reaches it"* of most, so no sentence is wrong. **This is [row 5b-83](../fragment-issues/section-5b-rows-076-100.md)'s question, measured a second way** - that row has been OPEN since 2026-09-21 on *"90 of the modules in `brain/` are imported by nothing but their own test"*, and `python tools/module-reach.py` is its command. That tool reads imports, per module, in `brain/`; this reading is per AGENT, by function, and adds the add-in, the installer and what the house rules run, which is how it finds three agents that tool calls unreached and a Claude Code subagent does call (`RAG-FMT-004`, `FRG-CRE-007`, `IMP-DUP-007`, through `heron-fragment-author`). The two agree on the size of it. All three faults in rows 5b-1544 and 5b-1545 sat in such agents for weeks. **(b)** Each record in `brain/agent-proofs/` names its two models and its sessions and no release; *"Revit 2020"* for the 2026-10-25 run is in that folder's README and in commit `5a47a9bb` only. A fragment's proof carries `proof.releases` ([D-131](../DECISIONS.md)). **Not checked:** the trace cannot see a name assembled at run time or a module loaded by path from a string; the fifteen that run only inside Revit were not run.

**State.** **MOVED TO ISSUE [#913](https://github.com/Ajmalpshaik/Heron-AI/issues/913) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - needs the owner, [D-154](../DECISIONS.md) (Proposed).** Nothing was retired: no agent met both halves of that decision's test in full - nothing calls it AND a named thing already does its job. Three come nearest and D-154 names them: `FRG-EVO-005`, `FRG-UPD-008` and `LRN-EXT-003` each have a Claude Code subagent that calls itself their working prototype (`skill-maintainer`, `skill-recorder`), on house-rule skills and a developer session's lessons, not on fragments or a modeller's use. The inventory itself is rough work and is not in the repository - `python tools/module-reach.py --list` is the standing command; the questions it leaves are in D-154, by department, and that decision is the sitting row 5b-83 asks for. For (b), `prove-agent.py track` and `vary` would record the release the session reported, as `batch-prove` does for a fragment - not built here, because it changes what a person signs.

---

### Row 5b-1550

*Moved from the register on 2026-11-17.*

**Defect.** **THE FITTING-FAMILY METHOD SAYS `SET_ROUTING_PREFERENCE` PUTS A NEW FITTING IN ITS GROUP "WITH A SIZE WINDOW THAT COVERS THE TABLE'S SIZES"; THE CARD ADDS NO RULE AND SETS NO WINDOW.** Found 2026-11-11 reading the methods that name the three cards of [NEEDS-CHECKING Group NL](../needs-checking/group-nl.md), read and not run. [`brain/skills/mep-fitting-family-creation.yaml`](../../brain/skills/mep-fitting-family-creation.yaml), step 10. [`SET_ROUTING_PREFERENCE`](../../brain/fragments/set-routing-preference/fragment.yaml) points ONE existing rule at another part and carries that rule's size windows across unchanged; a group with no rule is refused on purpose (*"A rule has to exist before it can be changed"*), and no input names a size. So a fitting made by the method can take the place of the part in a rule the type already has, within that rule's window, and nothing more: a type with no rule in the group, or a table wider than the old window, is left as it was, and the step reads as if it were not. `COPY_ROUTING_PREFERENCES` copies whole rules from another type and does not make one either. The step's words on naming the fitting were corrected the same day (row 5b-1120); this part was not, because which way to settle it is not a wording question.

**State.** **MOVED TO ISSUE [#960](https://github.com/Ajmalpshaik/Heron-AI/issues/960) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - for the owner.** Reword the step to what the card does (swap the part in an existing rule; add a rule or widen its window by hand in the type's Routing Preferences), or widen a card to add a rule with a stated window - the size window is the thing the card's own header says must never be guessed.

---
