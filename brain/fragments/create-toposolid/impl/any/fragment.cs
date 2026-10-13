// NOT STANDALONE. Assumes `doc`, `boundaryMm`, `pointsMm`, `cadImport`,
// `cadLayers`, `typeName` and `level` are in scope; leaves `toposolid`,
// `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// NATIVE TERRAIN - A TOPOSOLID - from a boundary and, inside it, survey
// points with heights. Heights are millimetres above the project's internal
// origin, as typed from a survey sheet already reduced to the project:
//
//   boundaryMm   "x,y,z; x,y,z; ..." - the outline, in order, closed
//                automatically; it is kept as given, never hulled. EMPTY
//                lets Revit draw the outline round the points
//   pointsMm     "x,y,z; ..." - interior spot heights; empty for a flat
//                pad at the boundary's heights
//   cadImport    an imported or linked CAD drawing's id: every point,
//                polyline vertex and line end in it, at its own height,
//                joins the spot heights - a survey drawing made terrain.
//                EMPTY reads none
//   cadLayers    the drawing's layers to read, comma separated; EMPTY reads
//                every layer - name the levels layer of a busy survey
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

// A CAD SURVEY (added 2026-10-13): points closer than 100 mm in plan to one
// already taken are dropped, and no more than 5000 are kept, spread evenly -
// Revit slows on a surface of tens of thousands.
var cadText = (cadImport ?? "").Trim();
var fromCad = 0;
if (points != null && cadText != "")
{
    ImportInstance drawing = null;
    foreach (ImportInstance candidate in new FilteredElementCollector(doc).OfClass(typeof(ImportInstance)))
        if (candidate.Id.ToString() == cadText || candidate.UniqueId == cadText) { drawing = candidate; break; }
    if (drawing == null) refused = "No imported or linked CAD drawing " + cadText + " in this model. NOTHING WAS MADE.";
    else
    {
        var layers = new HashSet<string>((cadLayers ?? "").Split(',').Select(t => t.Trim().ToLowerInvariant()).Where(t => t != ""));
        var raw = new List<XYZ>();
        Action<GeometryElement> walk = null;
        walk = geometry =>
        {
            foreach (GeometryObject item in geometry)
            {
                var nested = item as GeometryInstance;
                if (nested != null) { walk(nested.GetInstanceGeometry()); continue; }
                if (layers.Count > 0)
                {
                    var style = doc.GetElement(item.GraphicsStyleId) as GraphicsStyle;
                    var layer = style != null && style.GraphicsStyleCategory != null ? style.GraphicsStyleCategory.Name.ToLowerInvariant() : "";
                    if (!layers.Contains(layer)) continue;
                }
                var point = item as Point;
                var poly = item as PolyLine;
                var curve = item as Curve;
                if (point != null) raw.Add(point.Coord);
                else if (poly != null) raw.AddRange(poly.GetCoordinates());
                else if (curve != null && curve.IsBound) { raw.Add(curve.GetEndPoint(0)); raw.Add(curve.GetEndPoint(1)); }
            }
        };
        walk(drawing.get_Geometry(new Options()));
        var cell = 100.0 / MmPerFoot;
        var taken = new HashSet<string>();
        var kept = new List<XYZ>();
        foreach (var p in raw)
            if (taken.Add(Math.Round(p.X / cell) + ":" + Math.Round(p.Y / cell))) kept.Add(p);
        if (kept.Count > 5000)
        {
            var step = kept.Count / 5000.0;
            var thinned = new List<XYZ>();
            for (var i = 0.0; i < kept.Count && thinned.Count < 5000; i += step) thinned.Add(kept[(int)i]);
            findings.Add("The drawing gave " + kept.Count + " points; " + thinned.Count + " kept, spread evenly.");
            kept = thinned;
        }
        fromCad = kept.Count;
        points.AddRange(kept);
        if (fromCad < 3) refused = "The drawing gave " + fromCad + " point(s)" + (layers.Count > 0 ? " on those layers" : "") + " - a surface needs three. NOTHING WAS MADE.";
    }
}
Element type = null;
var types = new FilteredElementCollector(doc).OfClass(typeof(ToposolidType)).ToElements();
var wantedType = (typeName ?? "").Trim();
foreach (var candidate in types)
    if (wantedType == "" || string.Equals(candidate.Name, wantedType, StringComparison.OrdinalIgnoreCase)) { type = candidate; break; }

if (refused != null) { }
else if (boundary == null || points == null) refused = "Each point is three numbers in millimetres, x,y,z, separated by semicolons. NOTHING WAS MADE.";
else if (boundary.Count == 0 && points.Count < 3) refused = "With no boundary, three points or more are needed for Revit to draw the outline round. NOTHING WAS MADE.";
else if (boundary.Count > 0 && boundary.Count < 3) refused = "A toposolid boundary needs three points or more. NOTHING WAS MADE.";
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
        // No boundary: Revit draws the outline round the points itself.
        toposolid = boundary.Count == 0
            ? Toposolid.Create(doc, heights, type.Id, level.Id)
            : Toposolid.Create(doc, new List<CurveLoop> { loop }, heights, type.Id, level.Id);
        doc.Regenerate();
        if (toposolid == null || doc.GetElement(toposolid.Id) == null) refused = "Revit returned no toposolid. NOTHING WAS MADE.";
        else findings.Add("Made a toposolid of \"" + type.Name + "\" from " + boundary.Count + " boundary point(s) and "
            + points.Count + " interior point(s)" + (fromCad > 0 ? ", " + fromCad + " of them from the CAD drawing" : "")
            + (boundary.Count == 0 ? ", the outline drawn by Revit round them" : "") + ", read back.");
    }
    catch (Exception ex) { refused = "Revit refused the toposolid: " + ex.Message + ". NOTHING WAS MADE."; toposolid = null; }
}
if (refused != null) findings.Add(refused);
