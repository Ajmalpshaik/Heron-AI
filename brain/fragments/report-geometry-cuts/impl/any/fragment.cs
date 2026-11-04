// NOT STANDALONE. Assumes `doc` and `elements` are in scope; leaves `cuts`,
// `withoutCuts` and `findings` behind.
//
// IT READS AND CHANGES NOTHING. Every call here is a Get or a Can/Is check;
// no transaction is needed and none is opened.
//
// THE TWO CUTS OF MODIFY > CUT GEOMETRY, BOTH WAYS ROUND, for every element
// handed in:
//   a VOID cut   InstanceVoidCutUtils - what a family's unattached void cuts
//                (GetElementsBeingCut, asked only of an element that is such a
//                cutting instance) and which void families cut this element
//                (GetCuttingVoidInstances, asked only of one a void can cut);
//   a SOLID cut  SolidSolidCutUtils - which solids this one cuts
//                (GetSolidsBeingCut) and which cut it (GetCuttingSolids), asked
//                only of an element Revit allows in a cut between solids.
// The Can/Is check comes first so an element that cannot take part in a cut is
// counted as without cuts, never as an error.
//
// A CUT IS NOT A JOIN. JoinGeometryUtils is not read here; the joins and which
// side of a join cuts are REPORT_JOINED_ELEMENTS'.
//
// AN ELEMENT WITH NO CUTS IS THE ORDINARY ANSWER, counted in `withoutCuts` so
// an empty list cannot be read as a failed read. One that could not be asked
// at all is counted and named apart from both.

var cuts = new List<string>();
var withoutCuts = 0;
var findings = new List<string>();

Func<Element, string> describe = e => string.Format("{0}{1} (id {2})",
    e.Category != null ? e.Category.Name + " " : "", e.Name, e.Id);

if (elements == null || elements.Count == 0)
{
    findings.Add("No elements were given - hand in the ones whose cuts are in question");
}
else
{
    var unreadable = new List<string>();
    var asked = 0;

    foreach (var element in elements)
    {
        if (element == null) continue;
        asked++;
        var mine = describe(element);
        var found = 0;

        try
        {
            if (InstanceVoidCutUtils.IsVoidInstanceCuttingElement(element))
            {
                foreach (var id in InstanceVoidCutUtils.GetElementsBeingCut(element))
                {
                    var other = doc.GetElement(id);
                    if (other == null) continue;
                    found++;
                    cuts.Add(mine + " CUTS " + describe(other) + " - with its void");
                }
            }

            if (InstanceVoidCutUtils.CanBeCutWithVoid(element))
            {
                foreach (var id in InstanceVoidCutUtils.GetCuttingVoidInstances(element))
                {
                    var other = doc.GetElement(id);
                    if (other == null) continue;
                    found++;
                    cuts.Add(mine + " is CUT BY " + describe(other) + " - a void family");
                }
            }

            if (SolidSolidCutUtils.IsElementFromAppropriateContext(element)
                && SolidSolidCutUtils.IsAllowedForSolidCut(element))
            {
                foreach (var id in SolidSolidCutUtils.GetSolidsBeingCut(element))
                {
                    var other = doc.GetElement(id);
                    if (other == null) continue;
                    found++;
                    cuts.Add(mine + " CUTS " + describe(other) + " - solid from solid");
                }
                foreach (var id in SolidSolidCutUtils.GetCuttingSolids(element))
                {
                    var other = doc.GetElement(id);
                    if (other == null) continue;
                    found++;
                    cuts.Add(mine + " is CUT BY " + describe(other) + " - solid from solid");
                }
            }
        }
        catch (Exception ex)
        {
            unreadable.Add(string.Format("{0}: {1}", mine, ex.Message));
            continue;
        }

        if (found == 0) withoutCuts++;
    }

    findings.Add(string.Format("{0} cut(s) across {1} element(s). {2} of them cut nothing and are cut by "
        + "nothing, which is the ordinary case", cuts.Count, asked, withoutCuts));

    if (unreadable.Count > 0)
    {
        findings.Add(string.Format("{0} element(s) could not be asked about cuts at all", unreadable.Count));
        foreach (var line in unreadable) findings.Add("Could not read: " + line);
    }

    if (cuts.Count > 0)
        findings.Add("The element that is CUT loses the cutter's shape from its volume and material takeoff. "
            + "Joins are a different thing - REPORT_JOINED_ELEMENTS reads those");
}
