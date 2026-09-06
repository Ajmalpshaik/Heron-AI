// NOT STANDALONE. Assumes `doc`, `view`, `elements`, `offsetMm` and
// `withOverall` are in scope, and leaves `created`, `segmentsDisagree`,
// `noOpeningFaces` and `notAWall` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). A plan of walls, one undo.
//
// TAKE THE JAMBS OFF THE WALL, NOT OFF THE DOOR.
//
// A door exposes its own left and right references and dimensioning to those
// is the obvious route. They resolve to reference PLANES rather than faces: no
// origin, so no position, so no way to ORDER them - and an unordered set of
// references produces a scrambled string that still looks like a dimension.
//
// An opening CUTS the wall. The wall's own solid therefore carries planar
// faces at each jamb whose normal runs ALONG the wall, exactly like its two
// ends do. One geometry walk gives ends and jambs together, on one element,
// each with a position along the run to sort by.
//
// THE SUM IS THE ONLY CHECK THAT CATCHES A SPURIOUS FACE.
//
// A join notch, or a second solid in the wall, can leave another face parallel
// to the run - an extra segment that reads as a perfectly good dimension. The
// segments are added up and compared with the overall, and a wall where they
// disagree is named.
//
// A RUNNING STRING HAS NO SINGLE VALUE.
//
// Asking a multi-segment dimension for its value returns nothing. That is not
// an error and not a failure to create; the overall comes from the second
// dimension, which is one of the reasons there are two.

const double MmToFeet = 1.0 / 304.8;

var created = new List<ElementId>();
var segmentsDisagree = new List<ElementId>();
var noOpeningFaces = new List<ElementId>();
var notAWall = new List<ElementId>();

double offset = offsetMm * MmToFeet;

var geometryOptions = new Options();
geometryOptions.ComputeReferences = true;
geometryOptions.IncludeNonVisibleObjects = false;
geometryOptions.DetailLevel = ViewDetailLevel.Fine;

// Rooms are accepted because that is how the job is asked for - "dimension
// room 4's walls". Each room resolves to its own bounding walls.
var walls = new List<Wall>();
var seen = new HashSet<ElementId>();

foreach (var element in elements)
{
    if (element == null) continue;

    var wall = element as Wall;
    if (wall != null)
    {
        if (seen.Add(wall.Id)) walls.Add(wall);
        continue;
    }

    var room = element as SpatialElement;
    if (room == null) { notAWall.Add(element.Id); continue; }

    try
    {
        var loops = room.GetBoundarySegments(new SpatialElementBoundaryOptions());
        if (loops == null) { notAWall.Add(element.Id); continue; }
        foreach (var loop in loops)
        {
            foreach (var segment in loop)
            {
                var bounding = doc.GetElement(segment.ElementId) as Wall;
                if (bounding != null && seen.Add(bounding.Id)) walls.Add(bounding);
            }
        }
    }
    catch { notAWall.Add(element.Id); }
}

foreach (var wall in walls)
{
    var location = wall.Location as LocationCurve;
    Line run = location != null ? location.Curve as Line : null;
    if (run == null) { noOpeningFaces.Add(wall.Id); continue; }

    XYZ start = run.GetEndPoint(0);
    XYZ direction = run.Direction;
    XYZ across = direction.CrossProduct(XYZ.BasisZ).Normalize();

    // Every planar face whose normal runs ALONG the wall: its two ends, and
    // both sides of every opening cut through it.
    var cuts = new List<KeyValuePair<double, Reference>>();
    try
    {
        foreach (GeometryObject item in wall.get_Geometry(geometryOptions))
        {
            var solid = item as Solid;
            if (solid == null || solid.Faces.Size == 0) continue;
            foreach (Face face in solid.Faces)
            {
                var planar = face as PlanarFace;
                if (planar == null || face.Reference == null) continue;
                if (Math.Abs(planar.FaceNormal.DotProduct(direction)) > 0.99)
                    cuts.Add(new KeyValuePair<double, Reference>(
                        (planar.Origin - start).DotProduct(direction), face.Reference));
            }
        }
    }
    catch { noOpeningFaces.Add(wall.Id); continue; }

    // Two solids can each carry a face at the same station along the run. One
    // reference per station, or the string gains a zero-length segment.
    var byStation = new List<KeyValuePair<double, Reference>>();
    foreach (var cut in cuts.OrderBy(c => c.Key))
    {
        if (byStation.Count > 0 && Math.Abs(byStation[byStation.Count - 1].Key - cut.Key) < 1e-4) continue;
        byStation.Add(cut);
    }

    if (byStation.Count < 2) { noOpeningFaces.Add(wall.Id); continue; }

    XYZ from = start + direction * byStation[0].Key + across * offset;
    XYZ to = start + direction * byStation[byStation.Count - 1].Key + across * offset;
    if (from.DistanceTo(to) < 1e-9) { noOpeningFaces.Add(wall.Id); continue; }

    Dimension running = null;
    try
    {
        var references = new ReferenceArray();
        foreach (var cut in byStation) references.Append(cut.Value);
        running = doc.Create.NewDimension(view, Line.CreateBound(from, to), references);
    }
    catch { }

    if (running == null) { noOpeningFaces.Add(wall.Id); continue; }
    created.Add(running.Id);

    if (!withOverall) continue;

    // The overall, drawn further out, from the first station to the last.
    XYZ outerFrom = from + across * offset;
    XYZ outerTo = to + across * offset;

    Dimension overall = null;
    try
    {
        var references = new ReferenceArray();
        references.Append(byStation[0].Value);
        references.Append(byStation[byStation.Count - 1].Value);
        overall = doc.Create.NewDimension(view, Line.CreateBound(outerFrom, outerTo), references);
    }
    catch { }

    if (overall == null) continue;
    created.Add(overall.Id);

    // The check. Segments summed against the overall: an extra face from a
    // join notch is an extra segment that looks entirely valid on its own.
    double summed = 0;
    try
    {
        foreach (DimensionSegment segment in running.Segments)
            if (segment.Value.HasValue) summed += segment.Value.Value;
    }
    catch { }

    double? overallValue = null;
    try { overallValue = overall.Value; } catch { }

    if (overallValue.HasValue && summed > 0 &&
        Math.Abs(summed - overallValue.Value) > MmToFeet)
        segmentsDisagree.Add(wall.Id);
}
