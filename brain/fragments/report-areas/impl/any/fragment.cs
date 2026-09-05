// NOT STANDALONE. Assumes `doc`, `schemeNameContains` and `listEachArea` are in
// scope; leaves `findings`, `schemeTotalsM2`, `placedCount`, `unplaced` and
// `unbounded` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THE SCHEMES OVERLAP ON PURPOSE, AND THAT IS WHY THERE IS NO GRAND TOTAL. The
// same floor is measured twice - once under Gross Building, once under Rentable
// - and both sets of Area elements sit in OST_Areas at the same time, in the
// same place. Collect the category, sum it, and the total is roughly double the
// building and belongs to no scheme at all. Every number below is PER SCHEME and
// no cross-scheme total is offered, because such a total would be believed.
//
// UNPLACED AND UNBOUNDED BOTH READ AS ZERO AND HAVE DIFFERENT FIXES:
//   unplaced   - the Area exists in the schedule and sits nowhere. Location null
//   unbounded  - it IS placed, but its boundary does not close, so Revit gives 0
// Both occupy a row in every area schedule and contribute nothing. Reporting
// them as "0" beside real areas hides the difference, and the difference is the
// whole fix.
//
// AREA IS INTERNAL SQUARE FEET, MULTIPLIED BY 0.09290304 FOR SQUARE METRES.
// Plain arithmetic at the edge, never a units API - that is the call that breaks
// at Revit 2021.

var findings = new List<string>();
var schemeTotalsM2 = new List<string>();
var placedCount = 0;
var unplaced = new List<string>();
var unbounded = new List<string>();

var areas = new List<Area>();
foreach (var element in new FilteredElementCollector(doc)
    .OfCategory(BuiltInCategory.OST_Areas).WhereElementIsNotElementType())
{
    var area = element as Area;
    if (area != null) areas.Add(area);
}

if (areas.Count == 0)
{
    findings.Add("This model has no Area elements at all. Note AREAS are not ROOMS - a model full of "
        + "rooms has none of these, and the room fragments are what answers a room question");
}
else
{
    // Group by scheme. The scheme is the only level at which a total means
    // anything, so it is the only level one is produced at.
    var byScheme = new Dictionary<string, List<Area>>();
    foreach (var area in areas)
    {
        var schemeName = "<no scheme>";
        try
        {
            var scheme = area.AreaScheme;
            if (scheme != null) schemeName = scheme.Name;
        }
        catch (Exception) { }

        if (!string.IsNullOrEmpty(schemeNameContains)
            && schemeName.IndexOf(schemeNameContains, StringComparison.OrdinalIgnoreCase) < 0)
        {
            continue;
        }

        if (!byScheme.ContainsKey(schemeName)) byScheme[schemeName] = new List<Area>();
        byScheme[schemeName].Add(area);
    }

    if (byScheme.Count == 0)
    {
        findings.Add(string.Format("{0} Area element(s) in the model, but none in a scheme whose "
            + "name contains '{1}'", areas.Count, schemeNameContains));
    }

    foreach (var scheme in byScheme)
    {
        var schemeTotal = 0.0;
        var schemePlaced = 0;
        var byLevel = new Dictionary<string, double>();
        var levelCounts = new Dictionary<string, int>();

        findings.Add(string.Format("SCHEME '{0}' - {1} area element(s):", scheme.Key,
            scheme.Value.Count));

        foreach (var area in scheme.Value)
        {
            var label = string.Format("'{0}' no. {1}", area.Name, area.Number);

            // Unplaced and unbounded both read as zero and are different faults.
            if (area.Location == null)
            {
                unplaced.Add(string.Format("{0} in scheme '{1}'", label, scheme.Key));
                continue;
            }

            var sqFt = 0.0;
            try { sqFt = area.Area; }
            catch (Exception) { sqFt = 0.0; }

            if (sqFt <= 0)
            {
                unbounded.Add(string.Format("{0} in scheme '{1}' - placed, but its boundary does "
                    + "not close", label, scheme.Key));
                continue;
            }

            var m2 = sqFt * 0.09290304;
            schemeTotal += m2;
            schemePlaced++;
            placedCount++;

            var levelName = "<no level>";
            try
            {
                var level = area.Level;
                if (level != null) levelName = level.Name;
            }
            catch (Exception) { }

            if (byLevel.ContainsKey(levelName)) { byLevel[levelName] += m2; levelCounts[levelName]++; }
            else { byLevel[levelName] = m2; levelCounts[levelName] = 1; }

            if (listEachArea)
            {
                findings.Add(string.Format("    {0}  {1}  {2:0.##} m2", label, levelName, m2));
            }
        }

        foreach (var level in byLevel)
        {
            findings.Add(string.Format("    {0}: {1} area(s), {2:0.##} m2",
                level.Key, levelCounts[level.Key], level.Value));
        }

        findings.Add(string.Format("    SCHEME TOTAL: {0:0.##} m2 across {1} placed area(s)",
            schemeTotal, schemePlaced));

        schemeTotalsM2.Add(string.Format("{0} = {1:0.##} m2", scheme.Key, schemeTotal));
    }

    if (unplaced.Count > 0)
    {
        findings.Add(string.Format("{0} UNPLACED area(s) - they hold a row in every area schedule "
            + "and sit nowhere in the model. Place them or delete the row", unplaced.Count));
    }

    if (unbounded.Count > 0)
    {
        findings.Add(string.Format("{0} UNBOUNDED area(s) - placed, but the boundary lines do not "
            + "close, so Revit gives them nothing. Close the boundary", unbounded.Count));
    }

    findings.Add("NO TOTAL ACROSS SCHEMES IS GIVEN, on purpose - schemes measure the same floor more "
        + "than once, so adding them together is roughly double the building and belongs to no "
        + "scheme at all");
}

findings.Insert(0, string.Format("{0} placed area(s) reported; {1} unplaced, {2} unbounded",
    placedCount, unplaced.Count, unbounded.Count));
