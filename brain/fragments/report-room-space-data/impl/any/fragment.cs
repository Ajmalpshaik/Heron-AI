// NOT STANDALONE. Assumes `doc` and `elements` are in scope, and leaves
// `identities`, `areas`, `volumes`, `unplaced`, `unenclosed`,
// `volumeNotComputed`, `notSpatial`, `lengths`, `widths`, `rotations`,
// `irregular`, `unmeasurable` and `volumeComputationOff` behind.
//
// VERSION 2 (2026-11-01) - MEASURE_ROOM_DIMENSIONS MERGED IN. Every room or
// space with an area is now also MEASURED ALONG ITS OWN WALLS:
//
//   lengths, widths   on the room's OWN axes, never the bounding box. A box
//                     is aligned to project X and Y, and an 8000 x 4000 room
//                     turned thirty degrees has one of about 8900 x 7500. The
//                     axis is the longest segment of the OUTER loop - the
//                     loop with the largest area, not loop zero, which may be
//                     a column - and the boundary is turned by minus that
//                     angle IN MEMORY, nothing moved. LENGTH IS ALWAYS THE
//                     LONGER SIDE, so a table does not swap its own columns
//                     past forty-five degrees.
//   rotations         that axis's angle, degrees, -90 to 90.
//   irregular         how much bigger the room's own rectangle is than the
//                     room, in percent - near 0 on a rotated rectangle, large
//                     on an L or a U, where a length x width is a lie.
//   unmeasurable      a room or space WITH an area whose own axes could not
//                     be established.
//   volumeComputationOff  every placed ROOM read reports no volume - the
//                     project setting, said once, never a room fault. One
//                     room doing it is that room's problem and does not set it.
//
// UNPLACED IS STILL KEPT APART FROM UNENCLOSED - the safer of the two cards'
// rules. MEASURE_ROOM_DIMENSIONS called both "unplaced" because both have no
// area; they need different fixes, and this card tells them apart by the
// location.
//
// READ ONLY. Opens no transaction and needs none.
//
// ROOMS AND SPACES ARE ONE JOB, AND THE BASE TYPE IS WHY.
//
// `Room` and `Space` live in two different namespaces and behave as one thing
// here: number, name, level and area are all declared on `SpatialElement`,
// which both derive from. Reading through the base means a mixed set of rooms
// and spaces needs no branch, and it is also what keeps this fragment off the
// category-specific types.
//
// THREE STATES LOOK IDENTICAL IN A SCHEDULE AND ARE NOT THE SAME PROBLEM.
//
//   no location, zero area    NOT PLACED. It exists in the schedule and was
//                             never put in the model. Somebody places it or
//                             deletes it
//   a location, zero area     NOT ENCLOSED. It was placed where the
//                             boundaries do not close. A wall, a room-bounding
//                             setting, or a gap
//   an area, zero volume      volume computation is OFF for the project.
//                             Nothing is wrong with the room at all
//
// A schedule that prints 0 for all three sends somebody hunting for a
// modelling fault in the one case where there is none.
//
// SQUARE FEET AND CUBIC FEET. D-20 converts at the edge. The area comes
// straight off the element and the volume off its parameter, so both are in
// the units Revit stores and neither is rounded here.

var identities = new Dictionary<ElementId, string>();
var areas = new Dictionary<ElementId, double>();
var volumes = new Dictionary<ElementId, double>();
var unplaced = new List<ElementId>();
var unenclosed = new List<ElementId>();
var volumeNotComputed = new List<ElementId>();
var notSpatial = new List<ElementId>();
// VERSION 2, from MEASURE_ROOM_DIMENSIONS.
var lengths = new Dictionary<ElementId, double>();
var widths = new Dictionary<ElementId, double>();
var rotations = new Dictionary<ElementId, double>();
var irregular = new Dictionary<ElementId, double>();
var unmeasurable = new List<ElementId>();
bool volumeComputationOff = false;
int roomsSeen = 0;
int roomsWithZeroVolume = 0;

var boundaryOptions = new SpatialElementBoundaryOptions();
boundaryOptions.SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish;

// THE ROOM'S OWN RECTANGLE. False when its own axes cannot be established.
Func<SpatialElement, double, bool> measure = (spatial, area) =>
{
    IList<IList<BoundarySegment>> loops = null;
    try { loops = spatial.GetBoundarySegments(boundaryOptions); } catch { }
    if (loops == null || loops.Count == 0) return false;

    // The outer loop by the size of its own enclosed area - the shoelace sum
    // over its segment start points. A hole is smaller than the room.
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
    if (outer == null) return false;

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
    if (longest == null || points.Count < 3) return false;

    var start = longest.GetEndPoint(0);
    var end = longest.GetEndPoint(1);
    // One consistent direction, so the same wall always reports one angle.
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

    double degrees = angle * 180.0 / Math.PI;
    while (degrees < 0.0) degrees += 180.0;
    while (degrees >= 180.0) degrees -= 180.0;
    if (degrees > 90.0) degrees -= 180.0;
    rotations[spatial.Id] = degrees;

    double ownRectangle = sideA * sideB;
    irregular[spatial.Id] = ownRectangle > 0.0 ? (ownRectangle - area) / ownRectangle * 100.0 : 0.0;
    return true;
};

foreach (var element in elements)
{
    var spatial = element as SpatialElement;
    if (spatial == null) { notSpatial.Add(element == null ? ElementId.InvalidElementId : element.Id); continue; }

    double area = 0;
    try { area = spatial.Area; } catch { }

    // The location is what separates "never placed" from "placed badly", and
    // it is the only way to tell them apart: both report zero area.
    bool placed = false;
    try { placed = spatial.Location != null; } catch { }

    if (area > 0)
        areas[spatial.Id] = area;
    else if (!placed)
        unplaced.Add(spatial.Id);
    else
        unenclosed.Add(spatial.Id);

    // ROOM_VOLUME, not a Volume property. The parameter is present on both
    // categories and every release, and it carries the "not computed" state
    // as an absent value where a property would hand back a plain zero - and
    // a zero here is indistinguishable from a room with no volume, which is
    // not a thing that exists.
    Parameter volumeParameter = null;
    try { volumeParameter = spatial.get_Parameter(BuiltInParameter.ROOM_VOLUME); } catch { }

    double volume = 0;
    if (volumeParameter != null && volumeParameter.HasValue)
    {
        try { volume = volumeParameter.AsDouble(); } catch { }
    }

    if (volume > 0) volumes[spatial.Id] = volume;
    else if (area > 0) volumeNotComputed.Add(spatial.Id);

    // VERSION 2, from MEASURE_ROOM_DIMENSIONS. Measured only where there is an
    // area: an unplaced or unenclosed room has no boundary to measure, and is
    // already named above. Rooms are told from spaces by CATEGORY - `Room`
    // cannot be named in a fragment (its namespace is not imported).
    if (area > 0)
    {
        bool isRoom = spatial.Category != null
                      && spatial.Category.Id == new ElementId(BuiltInCategory.OST_Rooms);
        if (isRoom)
        {
            roomsSeen++;
            if (volume <= 0) roomsWithZeroVolume++;
        }
        if (!measure(spatial, area)) unmeasurable.Add(spatial.Id);
    }

    string number = "";
    string name = "";
    string level = "";
    try { number = spatial.Number ?? ""; } catch { }
    try { name = spatial.Name ?? ""; } catch { }
    try
    {
        // Not every spatial element resolves a level - an unplaced one often
        // does not - so this is read separately from the rest of the identity
        // and left blank rather than costing the number and the name.
        var host = spatial.Level;
        if (host != null) level = host.Name ?? "";
    }
    catch { }

    identities[spatial.Id] = (number.Length > 0 ? number + " " : "") + name +
                             (level.Length > 0 ? ", " + level : "");
}

// Every placed Room reporting no volume is the project setting, not the
// model (version 2). One room doing it is that room's problem.
volumeComputationOff = roomsSeen > 0 && roomsWithZeroVolume == roomsSeen;
