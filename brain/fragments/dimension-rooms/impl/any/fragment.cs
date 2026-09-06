// NOT STANDALONE. Assumes `doc`, `view`, `elements`, `measureTo` and
// `offsetMm` are in scope, and leaves `created`, `values`, `disagreed`,
// `notRectangular`, `noReference` and `notARoom` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). A plan of rooms, one undo.
//
// WHICH FACE BOUNDS THE ROOM IS A DISTANCE QUESTION, NOT A NAMING ONE.
//
// A wall offers its faces as interior and exterior LAYERS, and which of those
// happens to bound this room depends on which direction the wall was drawn in.
// Choosing by the name is right about half the time and silent when it is
// wrong. The face NEAREST the room's own boundary is the one bounding it; the
// FARTHEST is the outer one. Distance is the only test that does not depend on
// how somebody drew the wall.
//
// THE CENTRE MODE DIMENSIONS TO THE WALL ITSELF.
//
// A reference to the wall element resolves to its LOCATION LINE, which is the
// geometric middle only while the wall type's location line is set to its
// centre. A type set to a finish face measures to that face instead and looks
// entirely correct. That is what the read-back below catches: the expected
// size is computed independently from the boundary extent plus each wall's own
// thickness, so an offset location line shows up as a disagreement rather than
// as a plausible wrong number on a drawing.
//
// THE STRING'S POSITION AND THE MEASUREMENT ARE INDEPENDENT.
//
// `offsetMm` moves where the dimension is DRAWN. It does not change what is
// measured. Drawing an outside measurement inside the room is legal, and on a
// crowded plan it is sometimes what is wanted.

const double MmToFeet = 1.0 / 304.8;

var created = new List<ElementId>();
var values = new Dictionary<ElementId, double>();
var disagreed = new List<ElementId>();
var notRectangular = new List<ElementId>();
var noReference = new List<ElementId>();
var notARoom = new List<ElementId>();

string mode = (measureTo ?? "").Trim().ToLowerInvariant();
bool wantCentre = mode == "centre" || mode == "center";
bool wantOutside = mode == "outside";

double offset = offsetMm * MmToFeet;
var boundaryOptions = new SpatialElementBoundaryOptions();

// The reference this mode wants, on one wall, judged against a point on the
// boundary segment that wall produced.
Func<Wall, XYZ, Reference> referenceFor = (wall, near) =>
{
    if (wantCentre)
    {
        try { return new Reference(wall); } catch { return null; }
    }

    Reference best = null;
    double bestDistance = wantOutside ? -1.0 : double.MaxValue;

    foreach (var layer in new[] { ShellLayerType.Exterior, ShellLayerType.Interior })
    {
        IList<Reference> faces = null;
        try { faces = HostObjectUtils.GetSideFaces(wall, layer); } catch { continue; }
        if (faces == null) continue;

        foreach (var candidate in faces)
        {
            Face face = null;
            try { face = wall.GetGeometryObjectFromReference(candidate) as Face; } catch { }
            if (face == null) continue;
            try
            {
                var hit = face.Project(near);
                if (hit == null) continue;
                if (wantOutside ? hit.Distance > bestDistance : hit.Distance < bestDistance)
                {
                    bestDistance = hit.Distance;
                    best = candidate;
                }
            }
            catch { }
        }
    }
    return best;
};

// How far past the room boundary this mode measures, on ONE side, in feet.
// Each wall's own thickness, so opposite walls of different types still give
// an honest expected number.
Func<Wall, double> beyond = wall =>
{
    if (wall == null || !(wantCentre || wantOutside)) return 0;
    try { return wantCentre ? wall.Width / 2.0 : wall.Width; } catch { return 0; }
};

foreach (var element in elements)
{
    var room = element as Room;
    if (room == null)
    {
        if (element != null) notARoom.Add(element.Id);
        continue;
    }

    double area = 0;
    try { area = room.Area; } catch { }
    if (area <= 0) { notARoom.Add(room.Id); continue; }

    IList<IList<BoundarySegment>> loops = null;
    try { loops = room.GetBoundarySegments(boundaryOptions); } catch { }
    if (loops == null || loops.Count == 0) { noReference.Add(room.Id); continue; }

    // A room with more than four segments in its outer loop is not a plain
    // rectangle. It is still dimensioned - its overall extent is an honest
    // answer - but it is named, because the overall size of an L is not what
    // somebody dimensioning each leg would draw.
    if (loops[0].Count != 4) notRectangular.Add(room.Id);

    // The extreme segments in each direction. A segment running along X bounds
    // the room in Y and vice versa, which is why the pairs are crossed.
    BoundarySegment lowInY = null, highInY = null, lowInX = null, highInX = null;
    double minY = double.MaxValue, maxY = double.MinValue;
    double minX = double.MaxValue, maxX = double.MinValue;
    double z = 0;
    bool haveZ = false;

    foreach (var segment in loops[0])
    {
        Curve curve = null;
        try { curve = segment.GetCurve(); } catch { }
        if (curve == null) continue;

        XYZ start = curve.GetEndPoint(0);
        XYZ end = curve.GetEndPoint(1);
        if (!haveZ) { z = start.Z; haveZ = true; }

        XYZ direction = (end - start);
        if (direction.GetLength() < 1e-9) continue;
        direction = direction.Normalize();

        double middleX = (start.X + end.X) / 2.0;
        double middleY = (start.Y + end.Y) / 2.0;

        if (Math.Abs(direction.X) > Math.Abs(direction.Y))
        {
            // runs along X, so it bounds the room in Y
            if (middleY < minY) { minY = middleY; lowInY = segment; }
            if (middleY > maxY) { maxY = middleY; highInY = segment; }
        }
        else
        {
            if (middleX < minX) { minX = middleX; lowInX = segment; }
            if (middleX > maxX) { maxX = middleX; highInX = segment; }
        }
    }

    Func<BoundarySegment, Wall> wallOf = segment =>
    {
        if (segment == null) return null;
        try { return doc.GetElement(segment.ElementId) as Wall; } catch { return null; }
    };

    Func<BoundarySegment, XYZ> midpointOf = segment =>
    {
        if (segment == null) return null;
        try
        {
            var curve = segment.GetCurve();
            return curve == null ? null : curve.Evaluate(0.5, true);
        }
        catch { return null; }
    };

    // One dimension: two opposite walls, a line drawn outside the room in the
    // direction being measured.
    Action<BoundarySegment, BoundarySegment, bool> drawOne = (first, second, alongX) =>
    {
        var wallOne = wallOf(first);
        var wallTwo = wallOf(second);
        if (wallOne == null || wallTwo == null) { noReference.Add(room.Id); return; }

        var referenceOne = referenceFor(wallOne, midpointOf(first));
        var referenceTwo = referenceFor(wallTwo, midpointOf(second));
        if (referenceOne == null || referenceTwo == null) { noReference.Add(room.Id); return; }

        double extent = alongX ? (maxX - minX) : (maxY - minY);
        double expected = extent + beyond(wallOne) + beyond(wallTwo);

        // The string sits clear of what is being MEASURED, so an outside
        // dimension is pushed out by the wall as well and the visible gap
        // stays what was asked for.
        double push = offset + (wantOutside ? Math.Max(beyond(wallOne), beyond(wallTwo)) : 0);

        XYZ from, to;
        if (alongX)
        {
            double lineY = minY - push;
            from = new XYZ(minX, lineY, z);
            to = new XYZ(maxX, lineY, z);
        }
        else
        {
            double lineX = minX - push;
            from = new XYZ(lineX, minY, z);
            to = new XYZ(lineX, maxY, z);
        }

        if (from.DistanceTo(to) < 1e-9) { noReference.Add(room.Id); return; }

        Dimension dimension = null;
        try
        {
            var references = new ReferenceArray();
            references.Append(referenceOne);
            references.Append(referenceTwo);
            dimension = doc.Create.NewDimension(view, Line.CreateBound(from, to), references);
        }
        catch { }

        if (dimension == null) { noReference.Add(room.Id); return; }

        created.Add(dimension.Id);

        // Read back what Revit actually measured, and compare it against the
        // number worked out from the boundary and the wall thicknesses. Two
        // mechanisms agreeing is the only evidence worth having here.
        double? measured = null;
        try { measured = dimension.Value; } catch { }

        if (measured.HasValue)
        {
            values[dimension.Id] = measured.Value;
            if (Math.Abs(measured.Value - expected) > MmToFeet && !disagreed.Contains(room.Id))
                disagreed.Add(room.Id);
        }
    };

    if (lowInX != null && highInX != null && lowInX != highInX) drawOne(lowInX, highInX, true);
    if (lowInY != null && highInY != null && lowInY != highInY) drawOne(lowInY, highInY, false);
}
