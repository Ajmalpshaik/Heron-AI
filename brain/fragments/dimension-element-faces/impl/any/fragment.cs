// NOT STANDALONE. Assumes `doc`, `view`, `elements`, `direction` and
// `offsetMm` are in scope; leaves `created`, `measured`, `noFaces` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// ONE DIMENSION STRING PER ELEMENT, through every face of it that faces one
// way - the layers of a wall in a plan or a section, the build-up of a floor
// or a roof in a section, the base, sill, head and top of a wall in an
// elevation, the grid lines of a curtain wall.
//
//   direction   "across"     - a wall's thickness, a floor's or roof's depth
//               "along"      - down a wall's length
//               "horizontal" - the view's own left to right
//               "vertical"   - the view's own bottom to top
//               EMPTY means across.
//
// THE FACES ARE THE ELEMENT'S OWN SOLID FACES. A compound wall or floor is
// read as Revit gives its geometry; each planar face whose normal runs the
// asked way is a reference, and faces at one position count once. An opening
// cuts the wall, so its sill and head are faces of the wall - the elevation
// string comes from the wall, not from the window. A curtain wall offers its
// grid lines instead, each crossing the asked direction.
//
// The string sits through the middle of the element, moved offsetMm sideways
// in the view (0 runs through the middle). A family instance is refused here:
// DIMENSION_FAMILY_INSTANCES places those.
//
// READ BACK: each string's segments are read from the dimension Revit made.

const double MmPerFoot = 304.8;
var created = new List<ElementId>();
var noFaces = new List<ElementId>();
var lines = new List<string>();
var findings = new List<string>();
string refusal = null;

var way = (direction ?? "").Trim().ToLowerInvariant();
if (way == "") way = "across";
if (way != "across" && way != "along" && way != "horizontal" && way != "vertical")
    refusal = "\"" + direction + "\" is not across, along, horizontal or vertical. NOTHING WAS DRAWN.";
else if (view == null) refusal = "No view was given. NOTHING WAS DRAWN.";
else if (view is View3D || view.ViewType == ViewType.Schedule || view.ViewType == ViewType.DrawingSheet)
    refusal = "\"" + view.Name + "\" is not a plan, section, elevation or detail view - a dimension is drawn in one of those. NOTHING WAS DRAWN.";

var options = new Options();
options.ComputeReferences = true;
options.IncludeNonVisibleObjects = false;
options.DetailLevel = ViewDetailLevel.Fine;
var tolerance = 0.5 / MmPerFoot;

if (refusal == null)
{
    var normal = view.ViewDirection.Normalize();
    var offset = offsetMm / MmPerFoot;
    foreach (var element in elements)
    {
        if (element == null) continue;
        if (element is FamilyInstance)
        {
            noFaces.Add(element.Id);
            lines.Add(element.Id + ": a family instance - DIMENSION_FAMILY_INSTANCES places those.");
            continue;
        }

        // The direction, in the model.
        var wall = element as Wall;
        XYZ dir = null;
        if (way == "horizontal") dir = view.RightDirection;
        else if (way == "vertical") dir = view.UpDirection;
        else if (way == "across") dir = wall != null ? wall.Orientation : XYZ.BasisZ;
        else
        {
            var located = element.Location as LocationCurve;
            var run = located != null ? located.Curve as Line : null;
            dir = run != null ? run.Direction : view.RightDirection;
        }
        dir = dir.Normalize();
        if (Math.Abs(dir.DotProduct(normal)) > 0.01)
        {
            noFaces.Add(element.Id);
            lines.Add(element.Id + ": the asked direction runs into the view, not across it - a wall's layers read in a plan or a section across the wall, a floor's in a section.");
            continue;
        }

        // Every reference, with where it sits along the direction.
        var stations = new List<KeyValuePair<double, Reference>>();
        var curtain = wall != null ? wall.CurtainGrid : null;
        try
        {
            if (curtain != null)
            {
                var ids = new List<ElementId>(curtain.GetUGridLineIds());
                ids.AddRange(curtain.GetVGridLineIds());
                foreach (var id in ids)
                {
                    var gridLine = doc.GetElement(id) as CurtainGridLine;
                    var curve = gridLine != null ? gridLine.FullCurve as Line : null;
                    if (curve == null || Math.Abs(curve.Direction.DotProduct(dir)) > 0.01) continue;
                    stations.Add(new KeyValuePair<double, Reference>(curve.Evaluate(0.5, true).DotProduct(dir), new Reference(gridLine)));
                }
            }
            else
            {
                foreach (GeometryObject item in element.get_Geometry(options))
                {
                    var solid = item as Solid;
                    if (solid == null || solid.Faces.Size == 0) continue;
                    foreach (Face face in solid.Faces)
                    {
                        var planar = face as PlanarFace;
                        if (planar == null || face.Reference == null) continue;
                        if (Math.Abs(planar.FaceNormal.DotProduct(dir)) > 0.99)
                            stations.Add(new KeyValuePair<double, Reference>(planar.Origin.DotProduct(dir), face.Reference));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            noFaces.Add(element.Id);
            lines.Add(element.Id + ": its geometry could not be read - " + ex.Message);
            continue;
        }

        var kept = new List<KeyValuePair<double, Reference>>();
        foreach (var station in stations.OrderBy(s => s.Key))
            if (kept.Count == 0 || station.Key - kept[kept.Count - 1].Key > tolerance) kept.Add(station);
        if (kept.Count < 2)
        {
            noFaces.Add(element.Id);
            lines.Add(element.Id + ": " + kept.Count + " face(s) facing that way - a string needs two.");
            continue;
        }

        // The line: through the middle of the element, in the view's plane,
        // moved sideways by the offset.
        var box = element.get_BoundingBox(null);
        if (box == null) { noFaces.Add(element.Id); lines.Add(element.Id + ": no extent to place a string by."); continue; }
        var middle = (box.Min + box.Max) / 2.0;
        middle = middle - normal * (middle - view.Origin).DotProduct(normal);
        var side = normal.CrossProduct(dir).Normalize();
        middle = middle + side * offset;
        var from = middle + dir * (kept[0].Key - middle.DotProduct(dir));
        var to = middle + dir * (kept[kept.Count - 1].Key - middle.DotProduct(dir));

        Dimension dimension = null;
        try
        {
            var references = new ReferenceArray();
            foreach (var station in kept) references.Append(station.Value);
            dimension = doc.Create.NewDimension(view, Line.CreateBound(from, to), references);
        }
        catch (Exception ex) { lines.Add(element.Id + ": Revit refused the string - " + ex.Message); }
        if (dimension == null) { noFaces.Add(element.Id); continue; }

        doc.Regenerate();
        var values = new List<string>();
        try
        {
            if (dimension.Segments.Size > 0)
            {
                foreach (DimensionSegment segment in dimension.Segments)
                    if (segment.Value.HasValue) values.Add(Math.Round(segment.Value.Value * MmPerFoot).ToString());
            }
            else if (dimension.Value.HasValue) values.Add(Math.Round(dimension.Value.Value * MmPerFoot).ToString());
        }
        catch { }
        created.Add(dimension.Id);
        lines.Add(element.Id + " (" + (element.Category != null ? element.Category.Name : "element") + "): "
            + kept.Count + " references, " + string.Join(" + ", values) + " mm");
    }
    findings.Add(created.Count + " dimension string(s) drawn " + way + " in \"" + view.Name + "\" and read back; "
        + noFaces.Count + " element(s) offered no string.");
}

if (refusal != null) lines.Add(refusal);
var measured = string.Join(" | ", lines);
findings.AddRange(lines);
