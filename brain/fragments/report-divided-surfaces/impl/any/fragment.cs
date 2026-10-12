// NOT STANDALONE. Assumes `doc` and `withNodes` are in scope; leaves
// `divided`, `surfaceCount`, `pathCount` and `findings` behind.
//
// READ ONLY - opens no transaction.
//
// EVERY DIVIDED SURFACE AND DIVIDED PATH in the document in front - a mass
// or adaptive family, or a project holding masses - with how each is laid
// out:
//
//   a surface   its host, the U and V grids (layout, number or distance,
//               justification, rotation, offset), the gridline counts and
//               the border tile setting
//   a path      its node count, layout, distance or fixed number, length,
//               whether it closes, and how distance is measured
//
// withNodes true adds where the nodes are, in millimetres - the first 50 of
// each - which is what an adaptive component is placed on. A node's place
// comes from the point Revit's geometry gives for it; a node with none is
// counted, not guessed.

const double MmPerFoot = 304.8;
var report = new List<string>();
var findings = new List<string>();
var surfaceCount = 0;
var pathCount = 0;

Func<XYZ, string> mm = p => "(" + Math.Round(p.X * MmPerFoot) + ", " + Math.Round(p.Y * MmPerFoot) + ", " + Math.Round(p.Z * MmPerFoot) + ")";
Func<SpacingRule, string> rule = r =>
{
    var text = r.Layout.ToString();
    try
    {
        if (r.Layout == SpacingRuleLayout.FixedNumber) text += " " + r.Number;
        else if (r.Layout != SpacingRuleLayout.None) text += " " + Math.Round(r.Distance * MmPerFoot) + " mm";
        if (r.Layout != SpacingRuleLayout.None) text += ", " + r.Justification;
        if (Math.Abs(r.GridlinesRotation) > 1e-9) text += ", rotated " + Math.Round(r.GridlinesRotation * 180 / Math.PI, 1) + " deg";
        if (Math.Abs(r.Offset) > 1e-9) text += ", offset " + Math.Round(r.Offset * MmPerFoot) + " mm";
    }
    catch { }
    return text;
};

foreach (DividedSurface surface in new FilteredElementCollector(doc).OfClass(typeof(DividedSurface)))
{
    surfaceCount++;
    var line = "divided surface " + surface.Id + " on " + (surface.Host != null ? (surface.Host.Category != null ? surface.Host.Category.Name + " " : "") + surface.Host.Id : "no host")
        + ": U " + rule(surface.USpacingRule) + " (" + surface.NumberOfUGridlines + " gridlines), V " + rule(surface.VSpacingRule)
        + " (" + surface.NumberOfVGridlines + " gridlines), border tiles " + surface.BorderTile;
    if (withNodes)
    {
        var places = new List<string>();
        var none = 0;
        for (var u = 0; u < surface.NumberOfUGridlines && places.Count < 50; u++)
            for (var v = 0; v < surface.NumberOfVGridlines && places.Count < 50; v++)
            {
                var node = new GridNode(u, v);
                Point point = null;
                try
                {
                    var reference = surface.GetGridNodeReference(node);
                    point = reference != null ? surface.GetGeometryObjectFromReference(reference) as Point : null;
                }
                catch { }
                if (point == null) { none++; continue; }
                places.Add(u + "," + v + " " + mm(point.Coord));
            }
        line += "; nodes " + (places.Count > 0 ? string.Join(" ", places) : "none read") + (none > 0 ? "; " + none + " node(s) gave no point" : "");
    }
    report.Add(line);
}

var options = new Options();
options.ComputeReferences = true;
options.IncludeNonVisibleObjects = true;
foreach (DividedPath path in new FilteredElementCollector(doc).OfClass(typeof(DividedPath)))
{
    pathCount++;
    var line = "divided path " + path.Id + ": " + path.NumberOfPoints + " node(s), " + path.SpacingRuleLayout;
    try
    {
        if (path.SpacingRuleLayout == SpacingRuleLayout.FixedNumber) line += " " + path.FixedNumberOfPoints;
        else if (path.SpacingRuleLayout != SpacingRuleLayout.None) line += " " + Math.Round(path.Distance * MmPerFoot) + " mm";
    }
    catch { }
    line += ", length " + Math.Round(path.TotalPathLength * MmPerFoot) + " mm" + (path.IsClosedLoop ? ", closed" : ", open")
        + ", measured " + path.MeasurementType;
    if (withNodes)
    {
        var places = new List<string>();
        try
        {
            foreach (GeometryObject item in path.get_Geometry(options))
            {
                var point = item as Point;
                if (point != null && places.Count < 50) places.Add(mm(point.Coord));
            }
        }
        catch { }
        line += "; nodes " + (places.Count > 0 ? string.Join(" ", places) : "none read from its geometry");
    }
    report.Add(line);
}

// Nothing found leaves `divided` empty, so the hand-on sees no answer; the
// finding says it in words.
findings.Add(surfaceCount + " divided surface(s) and " + pathCount + " divided path(s) read in \"" + doc.Title + "\".");
var divided = string.Join(" | ", report);
findings.AddRange(report);
