// NOT STANDALONE. Assumes `doc`, `areaPlanName`, `traceWalls` and `loopsMm`
// are in scope; leaves `made`, `lineIds`, `report` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// AREA BOUNDARY LINES IN AN AREA PLAN, two ways:
//
//   traceWalls   "inside", "outside" or "centre": a line along every exterior
//                wall on the plan's level (wall type Function = Exterior), at
//                that face - the measuring rule of a gross or net area
//   loopsMm      closed loops of points in millimetres -
//                "x,y; x,y; x,y | x,y; ..." - each closed automatically
//
// Both may be given; an empty value skips that way. A wall is traced only when
// it is straight: an arc wall is named and left, because its face is not the
// centreline moved sideways.
//
// READ BACK: each new line is looked up again by its id.

const double MmPerFoot = 304.8;
var made = 0;
var lineIds = new List<ElementId>();
var lines = new List<string>();
var findings = new List<string>();

ViewPlan plan = null;
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ViewPlan)))
{
    var v = element as ViewPlan;
    if (v != null && !v.IsTemplate && v.ViewType == ViewType.AreaPlan
        && string.Equals(v.Name, (areaPlanName ?? "").Trim(), StringComparison.OrdinalIgnoreCase)) { plan = v; break; }
}
var trace = (traceWalls ?? "").Trim().ToLowerInvariant();
string refusal = null;
if (plan == null) refusal = "No area plan is named \"" + areaPlanName + "\". NOTHING WAS DRAWN.";
else if (trace != "" && trace != "inside" && trace != "outside" && trace != "centre" && trace != "center")
    refusal = "\"" + traceWalls + "\" is not inside, outside or centre. NOTHING WAS DRAWN.";
else if (trace == "" && (loopsMm ?? "").Trim() == "") refusal = "Neither walls to trace nor loops were given. NOTHING WAS DRAWN.";

var curves = new List<Curve>();
if (refusal == null && trace != "")
{
    var level = plan.GenLevel;
    foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(Wall)))
    {
        var wall = element as Wall;
        if (wall == null || level == null || wall.LevelId != level.Id) continue;
        if (wall.WallType == null || wall.WallType.Function != WallFunction.Exterior) continue;
        var located = wall.Location as LocationCurve;
        var line = located == null ? null : located.Curve as Line;
        if (line == null) { lines.Add("wall " + wall.Id + ": not straight - left for a hand-drawn line."); continue; }
        var shift = trace == "outside" ? wall.Width / 2.0 : trace == "inside" ? -wall.Width / 2.0 : 0.0;
        var move = wall.Orientation.Normalize() * shift;
        var z = level.Elevation;
        var a = line.GetEndPoint(0) + move;
        var b = line.GetEndPoint(1) + move;
        curves.Add(Line.CreateBound(new XYZ(a.X, a.Y, z), new XYZ(b.X, b.Y, z)));
    }
}

if (refusal == null && (loopsMm ?? "").Trim() != "")
{
    var culture = System.Globalization.CultureInfo.InvariantCulture;
    var numberStyle = System.Globalization.NumberStyles.Float;
    var z = plan.GenLevel == null ? 0 : plan.GenLevel.Elevation;
    foreach (var loop in loopsMm.Split('|'))
    {
        var pts = new List<XYZ>();
        foreach (var chunk in loop.Split(';'))
        {
            if (chunk.Trim() == "") continue;
            var parts = chunk.Split(',');
            double x, y;
            if (parts.Length < 2 || !double.TryParse(parts[0].Trim(), numberStyle, culture, out x)
                || !double.TryParse(parts[1].Trim(), numberStyle, culture, out y))
            { refusal = "\"" + chunk.Trim() + "\" is not a point - two numbers in millimetres. NOTHING WAS DRAWN."; break; }
            pts.Add(new XYZ(x / MmPerFoot, y / MmPerFoot, z));
        }
        if (refusal != null) break;
        if (pts.Count < 3) { lines.Add("a loop of " + pts.Count + " point(s) encloses nothing - left out."); continue; }
        for (var i = 0; i < pts.Count; i++) curves.Add(Line.CreateBound(pts[i], pts[(i + 1) % pts.Count]));
    }
}

if (refusal == null)
{
    var sketch = SketchPlane.Create(doc, plan.GenLevel.Id);
    foreach (var curve in curves)
    {
        try
        {
            var made1 = doc.Create.NewAreaBoundaryLine(sketch, curve, plan);
            if (made1 != null) lineIds.Add(made1.Id);
        }
        catch (Exception ex) { lines.Add("a line could not be drawn - " + ex.Message); }
    }
    doc.Regenerate();
    made = lineIds.Count(id => doc.GetElement(id) != null);
    findings.Add(made + " area boundary line(s) drawn in \"" + plan.Name + "\" and read back"
        + (trace == "" ? "" : ", exterior walls traced at the " + trace) + ".");
}

if (refusal != null) lines.Add(refusal);
var report = string.Join(" | ", lines);
findings.AddRange(lines);
