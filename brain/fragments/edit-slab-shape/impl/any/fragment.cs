// NOT STANDALONE. Assumes `doc`, `elements`, `mode` and `shapeMm` are in
// scope; leaves `edited`, `editedIds`, `report` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// SHAPE EDITING - Modify Sub Elements, Add Point, Add Split Line, Reset Shape -
// on floors, roofs and toposolids:
//
//   mode "points"   shapeMm "x,y,offset; ..."  a point added at each x,y and
//                   raised (or lowered) by the offset
//   mode "lines"    shapeMm "x,y,offset; x,y,offset | ..."  each group a run
//                   of split lines through points added at those places
//   mode "heights"  shapeMm "x,y,offset; ..."  the existing point nearest each
//                   x,y (within 300 mm) set to that offset - corners included
//   mode "reset"    shapeMm empty  the shape put back flat
//   mode "follow"   shapeMm "<element id>, <spacing mm>"  the shape reset, then
//                   points on a grid at that spacing and at every corner, each
//                   set to the top of that element (a toposolid, a floor)
//                   straight above or below it - a floor laid to the terrain
//
// Every offset is millimetres from the element's own unshaped top, the way
// Revit's shape editing measures it.
//
// FOUR RELEASES, TWO SETS OF NAMES: the editor is a property before 2024 and
// a method from 2024; a point is DrawPoint before 2025 and AddPoint from
// 2025; a split line DrawSplitLine and AddSplitLine. Each is found by name,
// so one implementation runs on all eight releases.
//
// READ BACK: after a regeneration each point is found again and its offset
// compared with the one asked.

const double MmPerFoot = 304.8;
var edited = 0;
var editedIds = new List<ElementId>();
var lines = new List<string>();
var findings = new List<string>();
string refusal = null;

var culture = System.Globalization.CultureInfo.InvariantCulture;
var numberStyle = System.Globalization.NumberStyles.Float;
var how = (mode ?? "").Trim().ToLowerInvariant();
if (how != "points" && how != "lines" && how != "heights" && how != "reset" && how != "follow")
    refusal = "\"" + mode + "\" is not points, lines, heights, reset or follow. NOTHING WAS CHANGED.";

// "x,y,offset; ..." groups split by "|", in feet.
Func<string, List<List<XYZ>>> readGroups = text =>
{
    var groups = new List<List<XYZ>>();
    foreach (var groupText in (text ?? "").Split('|'))
    {
        var group = new List<XYZ>();
        foreach (var chunk in groupText.Split(';'))
        {
            if (chunk.Trim() == "") continue;
            var parts = chunk.Split(',');
            double x, y, z = 0;
            if (parts.Length < 2 || parts.Length > 3 || !double.TryParse(parts[0].Trim(), numberStyle, culture, out x)
                || !double.TryParse(parts[1].Trim(), numberStyle, culture, out y)
                || (parts.Length == 3 && !double.TryParse(parts[2].Trim(), numberStyle, culture, out z))) return null;
            group.Add(new XYZ(x / MmPerFoot, y / MmPerFoot, z / MmPerFoot));
        }
        if (group.Count > 0) groups.Add(group);
    }
    return groups;
};

// The highest top-facing surface of an element straight above or below x,y.
Func<Element, double, double, double?> topAt = (element, x, y) =>
{
    var box = element.get_BoundingBox(null);
    if (box == null) return null;
    var probe = Line.CreateBound(new XYZ(x, y, box.Max.Z + 1.0), new XYZ(x, y, box.Min.Z - 1.0));
    double? best = null;
    var options = new Options();
    options.DetailLevel = ViewDetailLevel.Fine;
    foreach (GeometryObject item in element.get_Geometry(options))
    {
        var solid = item as Solid;
        if (solid == null) continue;
        foreach (Face face in solid.Faces)
        {
            IntersectionResultArray hits;
            if (face.Intersect(probe, out hits) != SetComparisonResult.Overlap || hits == null) continue;
            for (var i = 0; i < hits.Size; i++)
            {
                var z = hits.get_Item(i).XYZPoint.Z;
                if (best == null || z > best.Value) best = z;
            }
        }
    }
    return best;
};

var groups = how == "points" || how == "lines" || how == "heights" ? readGroups(shapeMm) : new List<List<XYZ>>();
if (refusal == null && groups == null) refusal = "Each point is x,y,offset in millimetres, separated by semicolons. NOTHING WAS CHANGED.";
else if (refusal == null && (how == "points" || how == "heights") && groups.Count == 0) refusal = "No points were given. NOTHING WAS CHANGED.";
else if (refusal == null && how == "lines" && groups.Any(g => g.Count < 2)) refusal = "A split line needs two points or more in its group. NOTHING WAS CHANGED.";

Element target = null;
double spacing = 0;
if (refusal == null && how == "follow")
{
    var parts = (shapeMm ?? "").Split(',');
    long targetNumber;
    if (parts.Length != 2 || !long.TryParse(parts[0].Trim(), out targetNumber)
        || !double.TryParse(parts[1].Trim(), numberStyle, culture, out spacing) || spacing < 100)
        refusal = "For follow, shapeMm is the element's id, then the grid spacing in millimetres (100 or more), e.g. \"412233, 2000\". NOTHING WAS CHANGED.";
    else
    {
        foreach (var candidate in new FilteredElementCollector(doc).WhereElementIsNotElementType())
            if (candidate.Id.ToString() == targetNumber.ToString()) { target = candidate; break; }
        if (target == null) refusal = "No element " + targetNumber + " in this model to follow. NOTHING WAS CHANGED.";
        spacing /= MmPerFoot;
    }
}

if (refusal == null)
{
    foreach (var element in elements)
    {
        if (element == null) continue;
        SlabShapeEditor editor = null;
        try
        {
            var getter = element.GetType().GetMethod("GetSlabShapeEditor", Type.EmptyTypes);
            if (getter != null) editor = getter.Invoke(element, null) as SlabShapeEditor;
            else
            {
                var property = element.GetType().GetProperty("SlabShapeEditor");
                if (property != null) editor = property.GetValue(element, null) as SlabShapeEditor;
            }
        }
        catch { }
        if (editor == null) { lines.Add(element.Id + ": no shape editing - a floor, a footprint roof or a toposolid has it."); continue; }

        try
        {
            if (how == "reset" || how == "follow") editor.ResetSlabShape();
            if (!editor.IsEnabled) editor.Enable();
            doc.Regenerate();

            // Where each wanted point goes, with its offset.
            var wanted = new List<XYZ>();
            var asked = new List<List<XYZ>>(groups);
            if (how == "follow")
            {
                var box = element.get_BoundingBox(null);
                var grid = new List<XYZ>();
                for (var x = box.Min.X + spacing / 2; x < box.Max.X; x += spacing)
                    for (var y = box.Min.Y + spacing / 2; y < box.Max.Y; y += spacing)
                        grid.Add(new XYZ(x, y, 0));
                foreach (SlabShapeVertex corner in editor.SlabShapeVertices) grid.Add(corner.Position);
                var followed = new List<XYZ>();
                var outside = 0;
                foreach (var spot in grid)
                {
                    var own = topAt(element, spot.X, spot.Y);
                    if (own == null) continue;
                    var other = topAt(target, spot.X, spot.Y);
                    if (other == null) { outside++; continue; }
                    followed.Add(new XYZ(spot.X, spot.Y, other.Value - own.Value));
                }
                asked = new List<List<XYZ>> { followed };
                if (outside > 0) lines.Add(element.Id + ": " + outside + " grid point(s) have nothing of " + target.Id + " above or below and stay flat.");
            }

            // Add new points where the mode makes them, at the top of the
            // element, then set every offset on the vertices read back.
            var addMethod = editor.GetType().GetMethod("AddPoint", new[] { typeof(XYZ) })
                ?? editor.GetType().GetMethod("DrawPoint", new[] { typeof(XYZ) });
            if (how != "heights")
            {
                foreach (var group in asked)
                    foreach (var point in group)
                    {
                        var near = editor.SlabShapeVertices.Cast<SlabShapeVertex>()
                            .Any(v => new XYZ(v.Position.X - point.X, v.Position.Y - point.Y, 0).GetLength() < 1.0 / MmPerFoot);
                        if (near) continue;
                        var top = topAt(element, point.X, point.Y);
                        if (top == null) { lines.Add(element.Id + ": (" + Math.Round(point.X * MmPerFoot) + ", " + Math.Round(point.Y * MmPerFoot) + ") is outside it."); continue; }
                        addMethod.Invoke(editor, new object[] { new XYZ(point.X, point.Y, top.Value) });
                    }
                doc.Regenerate();
            }

            Func<XYZ, double, SlabShapeVertex> vertexAt = (point, reach) =>
            {
                SlabShapeVertex best = null;
                var bestGap = reach;
                foreach (SlabShapeVertex v in editor.SlabShapeVertices)
                {
                    var gap = new XYZ(v.Position.X - point.X, v.Position.Y - point.Y, 0).GetLength();
                    if (gap < bestGap) { bestGap = gap; best = v; }
                }
                return best;
            };

            var reachFt = (how == "heights" ? 300.0 : 5.0) / MmPerFoot;
            var set = 0;
            var missed = 0;
            var probes = new List<Tuple<XYZ, double>>();
            foreach (var group in asked)
                foreach (var point in group)
                {
                    var vertex = vertexAt(point, reachFt);
                    if (vertex == null) { missed++; continue; }
                    var before = topAt(element, vertex.Position.X, vertex.Position.Y);
                    editor.ModifySubElement(vertex, point.Z);
                    set++;
                    if (before != null) probes.Add(Tuple.Create(new XYZ(vertex.Position.X, vertex.Position.Y, point.Z), before.Value));
                }

            if (how == "lines")
            {
                var splitMethod = editor.GetType().GetMethod("AddSplitLine", new[] { typeof(SlabShapeVertex), typeof(SlabShapeVertex) })
                    ?? editor.GetType().GetMethod("DrawSplitLine", new[] { typeof(SlabShapeVertex), typeof(SlabShapeVertex) });
                doc.Regenerate();
                foreach (var group in asked)
                    for (var i = 0; i + 1 < group.Count; i++)
                    {
                        var a = vertexAt(group[i], reachFt);
                        var b = vertexAt(group[i + 1], reachFt);
                        if (a == null || b == null) { missed++; continue; }
                        splitMethod.Invoke(editor, new object[] { a, b });
                    }
            }
            doc.Regenerate();

            // Read back: the top surface is probed again at every point set,
            // and how far it moved is reported - for follow, against the top
            // of the element followed.
            var moved = new List<string>();
            var onTarget = 0;
            foreach (var probe in probes)
            {
                var after = topAt(element, probe.Item1.X, probe.Item1.Y);
                if (after == null) continue;
                if (moved.Count < 6)
                    moved.Add("(" + Math.Round(probe.Item1.X * MmPerFoot) + ", " + Math.Round(probe.Item1.Y * MmPerFoot) + ") "
                        + Math.Round((after.Value - probe.Item2) * MmPerFoot) + " mm");
                if (target != null)
                {
                    var other = topAt(target, probe.Item1.X, probe.Item1.Y);
                    if (other != null && Math.Abs(other.Value - after.Value) < 5.0 / MmPerFoot) onTarget++;
                }
            }
            edited++;
            editedIds.Add(element.Id);
            lines.Add(element.Id + " (" + (element.Category != null ? element.Category.Name : "element") + "): "
                + (how == "reset" ? "shape reset" : set + " point(s) set" + (missed > 0 ? ", " + missed + " not found" : "")
                + "; the top moved " + (moved.Count > 0 ? string.Join(", ", moved) : "nowhere measurable")
                + (target != null ? "; " + onTarget + " of " + probes.Count + " within 5 mm of " + target.Id + "'s top" : ""))
                + "; " + editor.SlabShapeVertices.Size + " shape point(s) now.");
        }
        catch (System.Reflection.TargetInvocationException wrapped)
        {
            lines.Add(element.Id + ": Revit refused - " + (wrapped.InnerException ?? wrapped).Message);
        }
        catch (Exception ex) { lines.Add(element.Id + ": Revit refused - " + ex.Message); }
    }
    findings.Add(edited + " element(s) shape-edited (" + how + ") and read back.");
}

if (refusal != null) lines.Add(refusal);
var report = string.Join(" | ", lines);
findings.AddRange(lines);
