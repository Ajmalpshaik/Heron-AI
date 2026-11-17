# FRAGMENT-ISSUES archive — section 5b, rows 1301 to 1325

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
> Rows 1301 to 1325 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-1302

*Moved from the register on 2026-11-17.*

**Defect.** **`FRAME_ELEMENTS_IN_CAMERA` VERSION 3 TRUSTS TWO REVIT 2020 MEASUREMENTS ON EVERY RELEASE, AND ITS READ-BACK CANNOT TELL IT.** Found 2026-10-21 by a code-review agent reading PR #716 (Codex was out of review credit), after the owner signed version 3 on Revit 2020. (1) Row 5b-1301 dropped the crop box's Z-bottom guard (`Math.Min(CropBox.Min.Z, -deepest * 1.1)`) because Revit 2020 ignores it on a perspective view, and now only `reachFar()` reaches a deep corner - which does nothing, silently, when the far clip parameter is missing, read-only or refused. On a release that honours the crop box's Z bottom instead, elements beyond it would be clipped and `framed` undercount. (2) `windowDistance()` falls back to 0.1 ft in an empty `catch`, and the same number both places the window and checks it, so a release that draws the window elsewhere, or whose crop shape cannot be read, still reads back *"its window drawn 30 mm in front of the eye"* with a self-consistent `framed` and `lens`. Neither was seen: both are about releases this was not run on (2021 to 2027).

**State.** **FIXED IN THE CODE 2026-10-21, DRAFT, with row [1304](../FRAGMENT-ISSUES.md#) - `FRAME_ELEMENTS_IN_CAMERA` version 4 does what this cell asked: the Z-bottom guard is back after `reachFar()`, and `reachFar()` and `windowDistance()` say when they could not do their job. Not seen on any release; the cross-release proof still owes a picture.** Was: **OPEN - left for the next version, deliberately.** Fixing either changes the code under the signature the owner gave the same hour, and both change nothing on Revit 2020. The fix, for whoever next widens the card or proves it on another release (D-131): keep the Z-bottom guard after `reachFar()`; have `reachFar()` and `windowDistance()` say when they could not do their job, as a finding (*"the far clip could not be pushed out"*, *"the crop shape could not be read; 0.1 ft was assumed"*). The cross-release proof should look at a picture, as Group IF did - the numbers cannot catch this.

---

### Row 5b-1313

*Moved from the register on 2026-11-17.*

**Defect.** **TWO PULL REQUESTS GAVE FOUR NEW FRAGMENTS TWO IDS, EACH PASSED EVERY GATE ALONE, AND `main` WENT RED ONLY WHEN BOTH HAD MERGED - BECAUSE `tools/next-id.py` READS GROUPS, 5b ROWS, QUESTIONS AND DECISIONS, NEVER A FRAGMENT'S `id:`.** Found 2026-10-22 merging [#714](https://github.com/Ajmalpshaik/Heron-AI/pull/714) a minute after [#713](https://github.com/Ajmalpshaik/Heron-AI/pull/713), both started in the cloud on 2026-10-21: #713 gave FRG-VIEW-150 to `SET_FAMILY_LABEL_LAYOUT` and FRG-VIEW-151 to `COPY_LABELS_FROM_FAMILY`, #714 gave the same two to `SET_TEXT_TYPE` and `REPORT_TEXT_TYPES`. Each had taken the next free id in its area as the fragment-author rules say (`grep -h '^id:' brain/fragments/*/fragment.yaml`), which reads only its own checkout - #714 also grepped every remote branch before writing, and #713's was not yet pushed. On `main` at `a4500598`, `python brain/heron_fragment.py`, `tools/check-signatures.py` and `tools/check-docs.py` (section 9) each exited 1: *"id FRG-VIEW-151 is already used by copy-labels-from-family"*. `tools/next-id.py` ran on both branches before each merge and said nothing, because the fragment id is no register it reads; the merge hook named #713 as a commit main had, as advice, after the merge had been asked for.

**State.** **FIXED ON MAIN the same day** - the later pull request's two cards moved to FRG-VIEW-152 and FRG-VIEW-153 (row 5b-1300), nothing else changed; `heron_fragment.py`, `check-signatures` and `check-docs` exit 0 again. **OPEN - the cause:** `tools/next-id.py` could read every fragment card's `id:` on main, every branch and every open pull request, as it reads the other four registers, and print the next free id per area with HELD TWICE - recorded, not built (AGENTS.md: a defect found while doing something else is recorded). Until then: run `git grep -h '^id: FRG-<AREA>-' $(git branch -r \| grep -v HEAD) -- brain/fragments` before taking a fragment id, and again just before merging.

---

### Row 5b-1324

*Moved from the register on 2026-11-17.*

**Defect.** **A VOID HERON MAKES IN A FLOOR-BASED FAMILY DOES NOT CUT THE TEMPLATE'S FLOOR, SO THE FAMILY CUTS NO FLOOR IN A PROJECT - AND NO CALL IN REVIT'S API 2020 TO 2027 MAKES THAT CUT.** Seen 2026-10-22 on Revit 2020, relayed by the session that started this one: a nested floor-drain family (Metric Plumbing Fixture floor based template, category Plumbing Fixtures) built through Heron; a void extrusion 130 mm across, Z +5 to -600 mm, and later a square void 150 x 150 mm, Z -10 to +5 mm, both through the template's sample floor. Loaded into a project, neither cut the host floor - it read 6 faces and 1.2 m3, uncut, and the drain's grate stayed buried - with *Cut with Voids When Loaded* off and then on. The owner then used *Modify > Geometry > Cut* in the Family Editor on the sample floor and each void by hand, reloaded, and both cut every host floor; the hour it took is the gap. So the void has to cut the TEMPLATE'S host inside the family for the cut to carry, and `CREATE_FAMILY_HOST_OPENING` (PROVEN; Revit's opening call takes a wall or a ceiling) refuses a floor- or roof-based family by name, telling the modeller *"Cut a floor- or roof-based family's host with a void form instead"* - which is half the story, since that void then cuts nothing. **Read 2026-10-22 in the RevitAPI.xml of all eight releases (`nice3point.revit.api.revitapi` 2020.2.90 to 2027.3.0, identical words):** `SolidSolidCutUtils.AddCutBetweenSolids` throws *"The element must be in a project document or in a conceptual model, pattern based curtain panel, or adaptive component family"* and *"must be solid and must be a GenericForm, GeomCombination, or a FamilyInstance"* - three refusals at once for a void and a floor in an ordinary family, and `IsElementFromAppropriateContext` says the document rule again; `Document.CombineElements` takes a `CombinableElement`, which `Floor`, `RoofBase`, `Wall` and `Ceiling` are not - each a `HostObject`, read off the 2020 assembly's class chain; `JoinGeometryUtils.JoinGeometry` is *"not available for family documents"* and joins, not cuts; `InstanceVoidCutUtils.AddInstanceVoidCut` cuts with *"the unattached voids inside a cutting instance"* - a loaded family whose Cut with Voids When Loaded is on, and *"Elements in a project"*; `FamilyItemFactory.NewOpening` takes a wall or a ceiling; no member of `GenericForm`, `Extrusion`, `Floor` or `HostObject` is about a cut, and no other member named for a cut or a void fits (`BooleanOperationsUtils` works on detached solids; `Toposolid.SetCutVoidStability` is 2026 on and a toposolid's). The Family Editor's own *Cut Geometry* between a void and the host has no public call on any release. Revit's own message for the case, on every release (`FormFailures.VoidFormsNotUsed`): *"Some Void Forms of this family do not cut anything and will be deleted. To make a Void Form cut something else use the Cut Geometry tool."* **Not run in Revit:** the documented refusals themselves - a run can only confirm them, it cannot make the call take a floor - and whether `InstanceVoidCutUtils` would take a NESTED void family placed in the family document, whose remarks say a project: NEEDS REAL REVIT, a different family from the one built, not built. **Not measured:** the same void in a wall-, ceiling- or roof-based template - [BV12](../needs-checking/group-bv.md) still asks.

**State.** **MOVED TO ISSUE [#920](https://github.com/Ajmalpshaik/Heron-AI/issues/920) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - a limit of Revit's API, nothing to fix in code; the methods now say what to do by hand.** `hosted-family-creation` step 5 and `family-creation` step 10 tell the modeller, before Load into Project: *Modify > Geometry > Cut* in the Family Editor, the template's host FIRST, then the void, once per void. [43 §7](../43-building-revit-families.md) records the measurement and the read; [BV12](../needs-checking/group-bv.md) carries the floor's answer; whether Heron should do more - the nested-void route, or nothing until Autodesk opens the call - is [Q-85](../open-questions/tier-2.md). No card changed, so no routing moved.

---
