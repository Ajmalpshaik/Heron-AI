// NOT STANDALONE. Assumes `doc`, `view`, `regionType` and `boundary` are in
// scope; leaves `created` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Points are internal FEET.
//
// A FILLED REGION IS ANNOTATION, NOT MODEL. It exists in ONE view: not on any
// other drawing, not in the model, and in no schedule. Right for a markup, and
// completely wrong for anybody who thinks they have modelled something - which
// is why the report says so every time.
//
// THE LOOP CLOSES ITSELF and must not cross itself.
//
// A VIEW THAT TAKES NO ANNOTATION IS REFUSED WITH THE REASON, rather than
// throwing something obscure from inside Revit.

var created = ElementId.InvalidElementId;
var findings = new List<string>();

if (boundary == null || boundary.Count < 3)
{
    findings.Add(string.Format("A filled region needs at least three boundary points and {0} were given",
        boundary == null ? 0 : boundary.Count));
}
else if (view.ViewType == ViewType.ThreeD || view.IsTemplate)
{
    findings.Add(string.Format("'{0}' takes no annotation - a filled region lives in a plan, a section, "
        + "an elevation or a drafting view", view.Name));
}
else if (regionType == null)
{
    findings.Add("No filled region type was given, so there is no pattern to draw with");
}
else
{
    var edges = new List<Curve>();
    var badPoint = false;

    for (var i = 0; i < boundary.Count; i++)
    {
        var a = boundary[i];
        var b = boundary[(i + 1) % boundary.Count];
        if (a == null || b == null) { badPoint = true; break; }
        try { edges.Add(Line.CreateBound(a, b)); }
        catch { badPoint = true; break; }
    }

    if (badPoint)
    {
        findings.Add("Two boundary points are in the same place, so one edge has no length - nothing "
            + "was created");
    }
    else
    {
        try
        {
            var loops = new List<CurveLoop> { CurveLoop.Create(edges) };
            var region = FilledRegion.Create(doc, regionType.Id, view.Id, loops);

            if (region == null)
            {
                findings.Add("Revit returned no region - the usual cause is a boundary that crosses "
                    + "itself, or points that are not flat in the view's own plane");
            }
            else
            {
                created = region.Id;
                findings.Add(string.Format(
                    "Filled region drawn in '{0}'. It is ANNOTATION: it is in no other view, not in the "
                    + "model, and in no schedule - a markup, not a thing", view.Name));
            }
        }
        catch (Exception ex)
        {
            findings.Add(string.Format("The filled region could not be drawn: {0}", ex.Message));
        }
    }
}
