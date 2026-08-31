// NOT STANDALONE. Assumes `elements` (Rooms and/or Spaces) and `doc` are in
// scope, and leaves `lengths`, `widths`, `rotations`, `irregular`, `unplaced`
// and `unmeasurable` behind.
//
// READ ONLY. Opens no transaction, creates nothing, moves nothing.
//
// WHY NOT THE BOUNDING BOX.
//
// `get_BoundingBox` is aligned to project X and Y. An 8000 x 4000 room turned
// thirty degrees has a box of roughly 8900 x 7500 - half as big again, in a
// shape the room does not have. On a site not square to true north that is
// every room, so the box answers "how big is this room" wrongly for a perfectly
// ordinary rectangle.
//
// So the room's OWN axis is found first: the longest segment of its outer
// boundary. That is the dominant wall. The boundary points are rotated by minus
// that angle in memory and the extents taken along the result.
//
// THE OUTER LOOP IS THE LARGEST, NOT LOOP ZERO.
//
// Revit hands back the outer edge and every hole - a core, a column, a shaft -
// as a flat list of loops. Loop zero is usually the outside. Measuring a room
// on "usually" is how a column's outline becomes the room.
//
// LENGTH IS FIXED AS THE LONGER SIDE.
//
// Not assigned to an axis. Past forty-five degrees of rotation the two swap
// which project axis they land on, and a table that lets that happen swaps its
// own columns halfway down without saying so.
//
// AN UNPLACED ROOM IS NOT A FAILURE.
//
// Revit reports its area as 0 and gives it no boundary. It is a real state of a
// real model - a room scheduled but not yet placed - and it is reported
// separately from a room that could not be measured, because they are different
// jobs for different people.

var lengths = new Dictionary<ElementId, double>();
var widths = new Dictionary<ElementId, double>();
var rotations = new Dictionary<ElementId, double>();
var irregular = new Dictionary<ElementId, double>();
var unplaced = new List<ElementId>();
var unmeasurable = new List<ElementId>();

var boundaryOptions = new SpatialElementBoundaryOptions();
boundaryOptions.SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish;

foreach (var element in elements)
{
    var spatial = element as SpatialElement;
    if (spatial == null) { unmeasurable.Add(element.Id); continue; }

    double area = 0.0;
    try { area = spatial.Area; } catch { }
    if (area <= 0.0) { unplaced.Add(spatial.Id); continue; }

    IList<IList<BoundarySegment>> loops = null;
    try { loops = spatial.GetBoundarySegments(boundaryOptions); } catch { }
    if (loops == null || loops.Count == 0) { unmeasurable.Add(spatial.Id); continue; }

    // Pick the outer loop by the size of its own enclosed area - the shoelace
    // sum over its segment start points. A hole is smaller than the room that
    // contains it, on every shape a room can be.
    IList<BoundarySegment> outer = null;
    double largest = 0.0;

    foreach (var loop in loops)
    {
        var corners = new List<XYZ>();
        foreach (var segment in loop)
        {
            Curve curve = null;
            try { curve = segment.GetCurve(); } catch { }
            if (curve != null) corners.Add(curve.GetEndPoint(0));
        }
        if (corners.Count < 3) continue;

        double signed = 0.0;
        for (int i = 0; i < corners.Count; i++)
        {
            var here = corners[i];
            var next = corners[(i + 1) % corners.Count];
            signed += (here.X * next.Y) - (next.X * here.Y);
        }
        signed = Math.Abs(signed) / 2.0;

        if (signed > largest) { largest = signed; outer = loop; }
    }

    if (outer == null) { unmeasurable.Add(spatial.Id); continue; }

    var points = new List<XYZ>();
    Curve longest = null;
    double longestLength = 0.0;

    foreach (var segment in outer)
    {
        Curve curve = null;
        try { curve = segment.GetCurve(); } catch { }
        if (curve == null) continue;
        points.Add(curve.GetEndPoint(0));

        double curveLength = 0.0;
        try { curveLength = curve.ApproximateLength; } catch { }
        if (curveLength > longestLength) { longestLength = curveLength; longest = curve; }
    }

    if (longest == null || points.Count < 3) { unmeasurable.Add(spatial.Id); continue; }

    var start = longest.GetEndPoint(0);
    var end = longest.GetEndPoint(1);
    // One consistent direction, so the same wall always reports the same angle
    // rather than the angle or its opposite depending on how it was drawn.
    if (start.X > end.X) { var swap = start; start = end; end = swap; }
    double angle = Math.Atan2(end.Y - start.Y, end.X - start.X);

    double cos = Math.Cos(-angle);
    double sin = Math.Sin(-angle);
    double minX = double.MaxValue, maxX = double.MinValue;
    double minY = double.MaxValue, maxY = double.MinValue;

    foreach (var point in points)
    {
        double x = (point.X * cos) - (point.Y * sin);
        double y = (point.X * sin) + (point.Y * cos);
        if (x < minX) minX = x;
        if (x > maxX) maxX = x;
        if (y < minY) minY = y;
        if (y > maxY) maxY = y;
    }

    double sideA = maxX - minX;
    double sideB = maxY - minY;

    lengths[spatial.Id] = Math.Max(sideA, sideB);
    widths[spatial.Id] = Math.Min(sideA, sideB);

    // Reported so that 0 to 180 covers every case once, and a room turned 190
    // degrees reads as 10 rather than as something nobody would recognise.
    double degrees = angle * 180.0 / Math.PI;
    while (degrees < 0.0) degrees += 180.0;
    while (degrees >= 180.0) degrees -= 180.0;
    if (degrees > 90.0) degrees -= 180.0;
    rotations[spatial.Id] = degrees;

    // How much bigger the room's OWN rectangle is than the room. This is the
    // real irregularity: it stays near zero on a rotated rectangle, which is
    // exactly the case a project-aligned box calls irregular and is not.
    double ownRectangle = sideA * sideB;
    irregular[spatial.Id] = ownRectangle > 0.0
        ? (ownRectangle - area) / ownRectangle * 100.0
        : 0.0;
}
