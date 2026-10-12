// NOT STANDALONE. Assumes `doc`, `familyType`, `pointsMm` and `onNodesOf`
// are in scope; leaves `placed`, `placedIds`, `report` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// PLACES AN ADAPTIVE COMPONENT - a panel, a two-point beam, a one-point
// marker - by putting each of its placement points where it belongs, in a
// project or in a mass or adaptive family:
//
//   pointsMm   "x,y,z; x,y,z | x,y,z; ..." - one group per instance, its
//              points in the family's placement order
//   onNodesOf  the id of a divided path or a divided surface: instances are
//              placed on its nodes instead - on a path, runs of as many nodes
//              as the family has points, each starting where the last ended
//              (one per node for a one-point family); on a surface, a
//              four-point family fills each cell corner to corner, a
//              one-point family each node
//
// The points are SET, not hosted: an instance stays where it was placed when
// the path or surface changes later, and the report says so.
//
// READ BACK: every placement point's position is read again after a
// regeneration and compared with where it was asked to go, within 1 mm.

const double MmPerFoot = 304.8;
var placed = 0;
var placedIds = new List<ElementId>();
var lines = new List<string>();
var findings = new List<string>();
string refusal = null;

var culture = System.Globalization.CultureInfo.InvariantCulture;
var numberStyle = System.Globalization.NumberStyles.Float;
var groups = new List<List<XYZ>>();
foreach (var groupText in (pointsMm ?? "").Split('|'))
{
    var group = new List<XYZ>();
    foreach (var chunk in groupText.Split(';'))
    {
        if (chunk.Trim() == "") continue;
        var parts = chunk.Split(',');
        double x, y, z;
        if (parts.Length != 3 || !double.TryParse(parts[0].Trim(), numberStyle, culture, out x)
            || !double.TryParse(parts[1].Trim(), numberStyle, culture, out y)
            || !double.TryParse(parts[2].Trim(), numberStyle, culture, out z)) { groups = null; break; }
        group.Add(new XYZ(x / MmPerFoot, y / MmPerFoot, z / MmPerFoot));
    }
    if (groups == null) break;
    if (group.Count > 0) groups.Add(group);
}

if (familyType == null) refusal = "No family type was given. NOTHING WAS PLACED.";
else if (!AdaptiveComponentInstanceUtils.IsAdaptiveFamilySymbol(familyType))
    refusal = "\"" + familyType.FamilyName + ": " + familyType.Name + "\" is not an adaptive family. NOTHING WAS PLACED.";
else if (groups == null) refusal = "Each point is x,y,z in millimetres, separated by semicolons, an instance's points by |. NOTHING WAS PLACED.";

// How many placement points the family has: one trial instance, read and
// deleted - outside the family's own editor Revit offers the count only on
// an instance.
var count = 0;
if (refusal == null)
{
    if (!familyType.IsActive) { familyType.Activate(); doc.Regenerate(); }
    var trial = AdaptiveComponentInstanceUtils.CreateAdaptiveComponentInstance(doc, familyType);
    count = AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(trial).Count;
    doc.Delete(trial.Id);
    if (count == 0) refusal = "The family has no placement points. NOTHING WAS PLACED.";
}

// Nodes, when asked.
var nodeText = (onNodesOf ?? "").Trim();
if (refusal == null && nodeText != "")
{
    Element source = null;
    foreach (var candidate in new FilteredElementCollector(doc).WhereElementIsNotElementType())
        if (candidate.Id.ToString() == nodeText || candidate.UniqueId == nodeText) { source = candidate; break; }
    var path = source as DividedPath;
    var surface = source as DividedSurface;
    if (path == null && surface == null) refusal = "\"" + nodeText + "\" is not a divided path or a divided surface here. NOTHING WAS PLACED.";
    else if (path != null)
    {
        var options = new Options();
        options.ComputeReferences = true;
        options.IncludeNonVisibleObjects = true;
        var nodes = new List<XYZ>();
        foreach (GeometryObject item in path.get_Geometry(options))
        {
            var point = item as Point;
            if (point != null) nodes.Add(point.Coord);
        }
        if (nodes.Count < count) refusal = "The divided path gave " + nodes.Count + " node(s) from its geometry and the family needs " + count + ". NOTHING WAS PLACED.";
        var stride = Math.Max(1, count - 1);
        for (var i = 0; refusal == null && i + count <= nodes.Count; i += stride) groups.Add(nodes.GetRange(i, count));
    }
    else
    {
        Func<int, int, XYZ> nodeAt = (u, v) =>
        {
            try
            {
                var reference = surface.GetGridNodeReference(new GridNode(u, v));
                var point = reference != null ? surface.GetGeometryObjectFromReference(reference) as Point : null;
                return point != null ? point.Coord : null;
            }
            catch { return null; }
        };
        var gaps = 0;
        if (count == 1)
        {
            for (var u = 0; u < surface.NumberOfUGridlines; u++)
                for (var v = 0; v < surface.NumberOfVGridlines; v++)
                {
                    var p = nodeAt(u, v);
                    if (p == null) gaps++; else groups.Add(new List<XYZ> { p });
                }
        }
        else if (count == 4)
        {
            for (var u = 0; u + 1 < surface.NumberOfUGridlines; u++)
                for (var v = 0; v + 1 < surface.NumberOfVGridlines; v++)
                {
                    var corners = new List<XYZ> { nodeAt(u, v), nodeAt(u + 1, v), nodeAt(u + 1, v + 1), nodeAt(u, v + 1) };
                    if (corners.Any(c => c == null)) gaps++; else groups.Add(corners);
                }
        }
        else refusal = "On a divided surface a family of one or four placement points is placed; this one has " + count + ". NOTHING WAS PLACED.";
        if (refusal == null && gaps > 0) lines.Add(gaps + " node(s) or cell(s) gave no point and were left.");
    }
}

if (refusal == null && groups.Count == 0) refusal = "No points were given and no nodes found. NOTHING WAS PLACED.";

if (refusal == null)
{
    var placedHere = new List<Tuple<FamilyInstance, List<XYZ>>>();
    foreach (var group in groups)
    {
        if (group.Count != count) { lines.Add("a group of " + group.Count + " point(s) - the family needs " + count + "; left."); continue; }
        try
        {
            var instance = AdaptiveComponentInstanceUtils.CreateAdaptiveComponentInstance(doc, familyType);
            var ids = AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(instance);
            for (var i = 0; i < ids.Count; i++)
            {
                var point = doc.GetElement(ids[i]) as ReferencePoint;
                if (point != null) point.Position = group[i];
            }
            placedHere.Add(Tuple.Create(instance, group));
        }
        catch (Exception ex) { lines.Add("Revit refused one instance - " + ex.Message); }
    }
    doc.Regenerate();

    // Read back.
    foreach (var pair in placedHere)
    {
        var ids = AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(pair.Item1);
        var off = 0;
        for (var i = 0; i < ids.Count; i++)
        {
            var point = doc.GetElement(ids[i]) as ReferencePoint;
            if (point == null || point.Position.DistanceTo(pair.Item2[i]) > 1.0 / MmPerFoot) off++;
        }
        if (off == 0) { placed++; placedIds.Add(pair.Item1.Id); }
        else lines.Add("instance " + pair.Item1.Id + ": " + off + " point(s) read back away from where they were asked.");
    }
    findings.Add(placed + " adaptive instance(s) of \"" + familyType.FamilyName + ": " + familyType.Name + "\" placed, "
        + count + " placement point(s) each, read back within 1 mm."
        + (nodeText != "" ? " Placed by position on the nodes - they do not follow if the divided element changes." : ""));
}

if (refusal != null) lines.Add(refusal);
var report = string.Join(" | ", lines);
findings.AddRange(lines);
