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

### Row 5b-1324

*Moved from the register on 2026-11-17.*

**Defect.** **A VOID HERON MAKES IN A FLOOR-BASED FAMILY DOES NOT CUT THE TEMPLATE'S FLOOR, SO THE FAMILY CUTS NO FLOOR IN A PROJECT - AND NO CALL IN REVIT'S API 2020 TO 2027 MAKES THAT CUT.** Seen 2026-10-22 on Revit 2020, relayed by the session that started this one: a nested floor-drain family (Metric Plumbing Fixture floor based template, category Plumbing Fixtures) built through Heron; a void extrusion 130 mm across, Z +5 to -600 mm, and later a square void 150 x 150 mm, Z -10 to +5 mm, both through the template's sample floor. Loaded into a project, neither cut the host floor - it read 6 faces and 1.2 m3, uncut, and the drain's grate stayed buried - with *Cut with Voids When Loaded* off and then on. The owner then used *Modify > Geometry > Cut* in the Family Editor on the sample floor and each void by hand, reloaded, and both cut every host floor; the hour it took is the gap. So the void has to cut the TEMPLATE'S host inside the family for the cut to carry, and `CREATE_FAMILY_HOST_OPENING` (PROVEN; Revit's opening call takes a wall or a ceiling) refuses a floor- or roof-based family by name, telling the modeller *"Cut a floor- or roof-based family's host with a void form instead"* - which is half the story, since that void then cuts nothing. **Read 2026-10-22 in the RevitAPI.xml of all eight releases (`nice3point.revit.api.revitapi` 2020.2.90 to 2027.3.0, identical words):** `SolidSolidCutUtils.AddCutBetweenSolids` throws *"The element must be in a project document or in a conceptual model, pattern based curtain panel, or adaptive component family"* and *"must be solid and must be a GenericForm, GeomCombination, or a FamilyInstance"* - three refusals at once for a void and a floor in an ordinary family, and `IsElementFromAppropriateContext` says the document rule again; `Document.CombineElements` takes a `CombinableElement`, which `Floor`, `RoofBase`, `Wall` and `Ceiling` are not - each a `HostObject`, read off the 2020 assembly's class chain; `JoinGeometryUtils.JoinGeometry` is *"not available for family documents"* and joins, not cuts; `InstanceVoidCutUtils.AddInstanceVoidCut` cuts with *"the unattached voids inside a cutting instance"* - a loaded family whose Cut with Voids When Loaded is on, and *"Elements in a project"*; `FamilyItemFactory.NewOpening` takes a wall or a ceiling; no member of `GenericForm`, `Extrusion`, `Floor` or `HostObject` is about a cut, and no other member named for a cut or a void fits (`BooleanOperationsUtils` works on detached solids; `Toposolid.SetCutVoidStability` is 2026 on and a toposolid's). The Family Editor's own *Cut Geometry* between a void and the host has no public call on any release. Revit's own message for the case, on every release (`FormFailures.VoidFormsNotUsed`): *"Some Void Forms of this family do not cut anything and will be deleted. To make a Void Form cut something else use the Cut Geometry tool."* **Not run in Revit:** the documented refusals themselves - a run can only confirm them, it cannot make the call take a floor - and whether `InstanceVoidCutUtils` would take a NESTED void family placed in the family document, whose remarks say a project: NEEDS REAL REVIT, a different family from the one built, not built. **Not measured:** the same void in a wall-, ceiling- or roof-based template - [BV12](../needs-checking/group-bv.md) still asks.

**State.** **MOVED TO ISSUE [#920](https://github.com/Ajmalpshaik/Heron-AI/issues/920) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - a limit of Revit's API, nothing to fix in code; the methods now say what to do by hand.** `hosted-family-creation` step 5 and `family-creation` step 10 tell the modeller, before Load into Project: *Modify > Geometry > Cut* in the Family Editor, the template's host FIRST, then the void, once per void. [43 §7](../43-building-revit-families.md) records the measurement and the read; [BV12](../needs-checking/group-bv.md) carries the floor's answer; whether Heron should do more - the nested-void route, or nothing until Autodesk opens the call - is [Q-85](../open-questions/tier-2.md). No card changed, so no routing moved.

---
