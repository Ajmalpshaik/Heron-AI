// NOT STANDALONE. Assumes `doc`, `areaPlanName`, `pointsMm`, `names` and
// `numbers` are in scope; leaves `placed`, `areaIds`, `unenclosed`, `report`
// and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// AREAS PLACED IN AN AREA PLAN, one at each point, each inside a region the
// area boundary lines enclose - the Area tool, for many at once. Names and
// numbers, when given, go on in the same order as the points.
//
// An area placed where no boundary encloses it has no size; it is read back,
// named in `unenclosed`, and left for the modeller to decide on - REPORT_AREAS
// lists every area, CREATE_AREA_BOUNDARY_LINES draws the boundaries.

const double MmPerFoot = 304.8;
var placed = 0;
var areaIds = new List<ElementId>();
var unenclosed = new List<ElementId>();
var lines = new List<string>();
var findings = new List<string>();

ViewPlan plan = null;
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ViewPlan)))
{
    var v = element as ViewPlan;
    if (v != null && !v.IsTemplate && v.ViewType == ViewType.AreaPlan
        && string.Equals(v.Name, (areaPlanName ?? "").Trim(), StringComparison.OrdinalIgnoreCase)) { plan = v; break; }
}

var culture = System.Globalization.CultureInfo.InvariantCulture;
var numberStyle = System.Globalization.NumberStyles.Float;
var points = new List<UV>();
string refusal = null;
foreach (var chunk in (pointsMm ?? "").Split(';'))
{
    if (chunk.Trim() == "") continue;
    var parts = chunk.Split(',');
    double x, y;
    if (parts.Length < 2 || !double.TryParse(parts[0].Trim(), numberStyle, culture, out x)
        || !double.TryParse(parts[1].Trim(), numberStyle, culture, out y))
    { refusal = "\"" + chunk.Trim() + "\" is not a point - two numbers in millimetres. NOTHING WAS PLACED."; break; }
    points.Add(new UV(x / MmPerFoot, y / MmPerFoot));
}
var nameList = (names ?? "").Split(',').Select(s => s.Trim()).Where(s => s != "").ToList();
var numberList = (numbers ?? "").Split(',').Select(s => s.Trim()).Where(s => s != "").ToList();

if (refusal == null && plan == null) refusal = "No area plan is named \"" + areaPlanName + "\". NOTHING WAS PLACED.";
else if (refusal == null && points.Count == 0) refusal = "No point was given. NOTHING WAS PLACED.";

if (refusal == null)
{
    for (var i = 0; i < points.Count; i++)
    {
        try
        {
            var area = doc.Create.NewArea(plan, points[i]);
            if (area == null) { lines.Add("point " + (i + 1) + ": Revit returned no area."); continue; }
            if (i < nameList.Count)
            {
                var p = area.get_Parameter(BuiltInParameter.ROOM_NAME);
                if (p != null && !p.IsReadOnly) p.Set(nameList[i]);
            }
            if (i < numberList.Count) area.Number = numberList[i];
            areaIds.Add(area.Id);
        }
        catch (Exception ex) { lines.Add("point " + (i + 1) + ": Revit refused - " + ex.Message); }
    }
    doc.Regenerate();
    foreach (var id in areaIds)
    {
        var area = doc.GetElement(id) as SpatialElement;
        if (area != null && area.Area > 0) placed++;
        else
        {
            unenclosed.Add(id);
            lines.Add("area " + id + ": placed where no boundary encloses it - it has no size.");
        }
    }
    findings.Add(placed + " area(s) placed in \"" + plan.Name + "\" with a size read back; " + unenclosed.Count + " not enclosed.");
}

if (refusal != null) lines.Add(refusal);
var report = string.Join(" | ", lines);
findings.AddRange(lines);
