// NOT STANDALONE. Assumes `doc`, `boundaryMm`, `pointsMm`, `typeName` and
// `level` are in scope; leaves `toposolid`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// NATIVE TERRAIN - A TOPOSOLID - from a boundary and, inside it, survey
// points with heights. Heights are millimetres above the project's internal
// origin, as typed from a survey sheet already reduced to the project:
//
//   boundaryMm   "x,y,z; x,y,z; ..." - the outline, in order, closed
//                automatically; it is kept as given, never hulled
//   pointsMm     "x,y,z; ..." - interior spot heights; empty for a flat
//                pad at the boundary's heights
//
// The type is found by name among the toposolid types; empty takes the first.
// REVIT 2024 brought toposolids, so the card declares 2024 on.

const double MmPerFoot = 304.8;
var findings = new List<string>();
string refused = null;
Toposolid toposolid = null;

var culture = System.Globalization.CultureInfo.InvariantCulture;
var numberStyle = System.Globalization.NumberStyles.Float;
Func<string, List<XYZ>> read = text =>
{
    var list = new List<XYZ>();
    foreach (var chunk in (text ?? "").Split(';'))
    {
        if (chunk.Trim() == "") continue;
        var parts = chunk.Split(',');
        double x, y, z;
        if (parts.Length != 3 || !double.TryParse(parts[0].Trim(), numberStyle, culture, out x)
            || !double.TryParse(parts[1].Trim(), numberStyle, culture, out y)
            || !double.TryParse(parts[2].Trim(), numberStyle, culture, out z)) return null;
        list.Add(new XYZ(x / MmPerFoot, y / MmPerFoot, z / MmPerFoot));
    }
    return list;
};

var boundary = read(boundaryMm);
var points = read(pointsMm);
Element type = null;
var types = new FilteredElementCollector(doc).OfClass(typeof(ToposolidType)).ToElements();
var wantedType = (typeName ?? "").Trim();
foreach (var candidate in types)
    if (wantedType == "" || string.Equals(candidate.Name, wantedType, StringComparison.OrdinalIgnoreCase)) { type = candidate; break; }

if (boundary == null || points == null) refused = "Each point is three numbers in millimetres, x,y,z, separated by semicolons. NOTHING WAS MADE.";
else if (boundary.Count < 3) refused = "A toposolid boundary needs three points or more. NOTHING WAS MADE.";
else if (type == null) refused = (types.Count == 0 ? "The project has no toposolid type" : "No toposolid type is named \"" + wantedType + "\"") + ". NOTHING WAS MADE.";
else if (level == null) refused = "No level was given. NOTHING WAS MADE.";

if (refused == null)
{
    try
    {
        // The outline is drawn flat at the level - Revit wants a horizontal
        // sketch - and its corner heights join the interior points, so the
        // surface still passes through every height given.
        var z = level.Elevation;
        var loop = new CurveLoop();
        for (var i = 0; i < boundary.Count; i++)
        {
            var a = boundary[i];
            var b = boundary[(i + 1) % boundary.Count];
            loop.Append(Line.CreateBound(new XYZ(a.X, a.Y, z), new XYZ(b.X, b.Y, z)));
        }
        var heights = new List<XYZ>(points);
        heights.AddRange(boundary);
        toposolid = Toposolid.Create(doc, new List<CurveLoop> { loop }, heights, type.Id, level.Id);
        doc.Regenerate();
        if (toposolid == null || doc.GetElement(toposolid.Id) == null) refused = "Revit returned no toposolid. NOTHING WAS MADE.";
        else findings.Add("Made a toposolid of \"" + type.Name + "\" from " + boundary.Count + " boundary point(s) and "
            + points.Count + " interior point(s), read back.");
    }
    catch (Exception ex) { refused = "Revit refused the toposolid: " + ex.Message + ". NOTHING WAS MADE."; toposolid = null; }
}
if (refused != null) findings.Add(refused);
