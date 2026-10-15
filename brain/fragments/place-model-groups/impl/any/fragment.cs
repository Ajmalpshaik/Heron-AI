// NOT STANDALONE. Assumes `doc`, `groupTypeName`, `level` and `points` are in
// scope; leaves `placed`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// A MODEL GROUP TYPE BY ITS BROWSER NAME. A detail group belongs to a view and
// is refused by name rather than placed somewhere it cannot be seen.
//
// X AND Y ON THE PLAN, Z ABOVE THE LEVEL. The level's elevation is added here,
// and each copy's level and offset are READ BACK from Revit afterwards.

var placed = new List<Element>();
var refused = new List<string>();
var findings = new List<string>();

var wanted = (groupTypeName ?? "").Trim();
var modelGroups = new ElementId(BuiltInCategory.OST_IOSModelGroups);
var detailGroups = new ElementId(BuiltInCategory.OST_IOSDetailGroups);

GroupType groupType = null;
var names = new List<string>();
var isDetail = false;
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(GroupType)))
{
    var candidate = element as GroupType;
    if (candidate == null || candidate.Category == null) continue;
    if (candidate.Category.Id == modelGroups) names.Add(candidate.Name);
    if (!string.Equals(candidate.Name, wanted, StringComparison.Ordinal)) continue;
    if (candidate.Category.Id == detailGroups) { isDetail = true; continue; }
    if (candidate.Category.Id == modelGroups) groupType = candidate;
}

if (wanted.Length == 0)
{
    refused.Add("No group type was named, so NOTHING WAS PLACED.");
}
else if (groupType == null)
{
    refused.Add(isDetail
        ? "'" + wanted + "' is a DETAIL group, which belongs to a view and is not placed on a level. NOTHING WAS PLACED."
        : "No model group type is called '" + wanted + "', so NOTHING WAS PLACED. The model groups in this "
            + "project: " + (names.Count == 0 ? "none" : string.Join(", ", names)) + ".");
}
else if (level == null)
{
    refused.Add("No level was given, so NOTHING WAS PLACED.");
}
else if (points == null || points.Count == 0)
{
    refused.Add("No points were given, so NOTHING WAS PLACED.");
}
else
{
    var index = 0;
    foreach (var point in points)
    {
        index++;
        if (point == null) { refused.Add("Point " + index + " was empty - skipped."); continue; }
        var at = new XYZ(point.X, point.Y, level.Elevation + point.Z);
        try
        {
            var group = doc.Create.PlaceGroup(at, groupType);
            if (group == null)
            {
                refused.Add("Point " + index + ": Revit returned no group and raised no error.");
                continue;
            }
            placed.Add(group);

            var levelName = "";
            var lp = group.get_Parameter(BuiltInParameter.GROUP_LEVEL);
            if (lp != null && lp.HasValue)
            {
                var onLevel = doc.GetElement(lp.AsElementId());
                if (onLevel != null) levelName = onLevel.Name;
            }
            var offsetText = "";
            var op = group.get_Parameter(BuiltInParameter.GROUP_OFFSET_FROM_LEVEL);
            if (op != null && op.HasValue) offsetText = op.AsValueString();

            findings.Add(string.Format("Group {0} placed at point {1}: Revit reads it on '{2}'{3}.", group.Id,
                index, levelName.Length == 0 ? "no level" : levelName,
                offsetText.Length == 0 ? "" : ", offset " + offsetText));
        }
        catch (Exception ex)
        {
            refused.Add("Point " + index + ": Revit refused - " + ex.Message);
        }
    }

    findings.Insert(0, string.Format("Placed {0} of {1} copies of '{2}'. Each is a group: its members move "
        + "and change together, for every copy.", placed.Count, points.Count, groupType.Name));
}

foreach (var reason in refused) findings.Add(reason);
