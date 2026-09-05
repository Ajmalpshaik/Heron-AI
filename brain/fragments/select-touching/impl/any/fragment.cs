// NOT STANDALONE. Assumes `doc`, `target` and `categories` are in scope; leaves
// `elements` and `findings` behind.
//
// OVERLAPPING IS NOT MEETING, AND FOR MEP THAT DECIDES THE ANSWER. Two ducts
// joined by an elbow at a shared connector are touching face to face and do NOT
// occupy the same space, so this returns nothing for them - run against exactly
// that pair, across every category, and it found nothing. The test is
// VOLUMETRIC OVERLAP and a clean connection has none.
//
// SO AN EMPTY ANSWER MEANS NO CLASH, NOT NO RELATIONSHIP, and the report says
// so - a bare zero here reads as a broken filter or, worse, as a clear model.
//
// THE TARGET IS NEVER RETURNED. It overlaps its own geometry perfectly, and
// including it puts a false pair in every clash list built on this.

var elements = new List<Element>();
var findings = new List<string>();

if (target == null)
{
    findings.Add("No element was given - name the one everything else is tested against");
}
else
{
    var targetId = target.Id;
    var targetName = string.IsNullOrEmpty(target.Name) ? "that element" : target.Name;

    try
    {
        var collector = new FilteredElementCollector(doc)
            .WhereElementIsNotElementType()
            .WherePasses(new ElementIntersectsElementFilter(target));

        if (categories != null && categories.Count > 0)
            collector = collector.WherePasses(new ElementMulticategoryFilter(categories));

        foreach (var element in collector)
        {
            if (element.Id == targetId) continue;   // it always overlaps itself
            elements.Add(element);
        }

        findings.Add(string.Format("{0} element(s) physically overlap '{1}' (id {2}){3}",
            elements.Count, targetName, targetId,
            categories == null || categories.Count == 0
                ? ", across every category"
                : string.Format(", within {0} category/categories", categories.Count)));

        if (elements.Count == 0)
            findings.Add("Nothing OVERLAPS it. That is not the same as nothing being related to it: "
                + "two runs joined at a connector meet face to face and share no volume, so a "
                + "correctly connected duct returns zero here. TRACE_CONNECTIVITY answers "
                + "'what is connected to this'");
    }
    catch (Exception ex)
    {
        findings.Add(string.Format("'{0}' could not be tested for overlap: {1}. An element with no "
            + "solid geometry has nothing to intersect", targetName, ex.Message));
    }
}
