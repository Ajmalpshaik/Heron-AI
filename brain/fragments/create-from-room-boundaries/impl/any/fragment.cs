// NOT STANDALONE. Assumes `doc`, `elements`, `hostType`, `heightAboveLevel`,
// `baseOffset`, `rooms` and `joinToHost` are in scope, and leaves `created`,
// `withHoles`, `unenclosed`, `refused`, `finishWalls`, `finishSummary`,
// `skippedEdges`, `skippedSummary` and `notJoined` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Every length a caller types -
// `heightAboveLevel` and `baseOffset` - is MILLIMETRES, divided by 304.8 once,
// below, before it is used for anything (D-71). Until version 2 this header
// said "internal FEET", stale since 2026-09-13 and left so on purpose: the
// proof's fingerprint hashes every byte under impl/, so correcting it alone
// would have staled a PROVEN proof (FRAGMENT-ISSUES row 5b-69). Version 2
// changes the file anyway, so the correction goes in with it.
//
// THE TYPE DECIDES WHAT IS BUILT. A floor type makes floors and a ceiling
// type makes ceilings - and, since version 2, a wall type lines the room's
// walls with a finish. There is no mode to get wrong, and a type that is none
// of the three is refused before anything is attempted.
//
// THE HOLES ARE THE POINT (floors and ceilings).
//
// A room's boundary is several LOOPS: the first is the outside edge, and every
// one after it is a shaft, a core or a column the room wraps around. Build
// from the first alone and the slab covers the shaft - correct in plan,
// through every check, and found by whoever tries to run a riser.
//
// THE CREATION CALL MOVED, SO IT IS LOOKED UP RATHER THAN NAMED.
//
// A floor is made one way up to 2021 and another from 2022; a ceiling cannot
// be made from code at all before 2022. Naming either in source stops this
// compiling on the other half of the range, so both are found at run time -
// and a release with no ceiling call says so rather than failing to build.
//
// VERSION 2: A FINISH ON THE ROOM'S WALLS (FRAGMENT-ISSUES row 5b-1255).
//
// Given a wall type - plaster, tile, cladding - every edge of the room that is
// a wall gets a thin wall of that type standing on the wall's INSIDE face, from
// the room's floor plus `baseOffset` up to `heightAboveLevel` above the room's
// floor. "The room's floor" is its level plus the room's own Base Offset, 0 on
// most rooms, so a raised-floor room is finished from its own floor.
//
//   * THE FACE, NOT THE CENTRELINE. The boundary is read at the walls' finish
//     faces, and each finish is set half its own thickness into the room, so
//     its outer face lies on the wall's face.
//   * WHICH SIDE IS THE ROOM is tested, not assumed from the loop's direction:
//     a point a few millimetres either side of each edge's middle is tested
//     against the room's own outline, holes included. Only when that cannot
//     tell (a room thinner than the probe) is the loop's turning used.
//   * CORNERS MEET. Two neighbouring finishes are trimmed or extended to the
//     point where their centrelines cross, so Revit sees two wall ends at one
//     point and joins them the way it joins any corner. Revit's own
//     Curve.Intersect is deprecated at 2026 and gone at 2027, so the plan
//     geometry - line with line, line with arc, arc with arc - is done here.
//   * GAPS AT DOORS AND OPENINGS. Every door, window, wall opening and
//     embedded curtain wall in the wall behind (FindInserts) is a rectangle on
//     the finish: across it, the finish is built only above and below it. A
//     door leaves a gap to the floor with the finish carried on above its head;
//     a window leaves the finish below its sill and above its head. A
//     rectangle's width is the insert's Rough Width, else its Width, centred
//     on its location - else its bounding box, which is wider on a wall that is
//     not square to the project. Its height is its bounding box. So a finish is
//     several walls, never one with an opening cut in it: one call does every
//     shape of wall, straight or arc, the same way, and nothing depends on an
//     opening Revit might refuse to cut.
//   * NOT ROOM BOUNDING. Every finish is made with Room Bounding off. A finish
//     that bounded the room would move its boundary in by the finish, shrink
//     its area and change every schedule that reads it.
//   * ITS EXTERIOR FACES THE ROOM. A finish type's layers are listed from the
//     exterior side, and the visible finish is the first of them, so each
//     finish is flipped, where it has to be, until its exterior faces the room.
//   * JOINED TO THE WALL BEHIND only when `joinToHost` is true; one Revit will
//     not join - the wall behind in another person's workset, say - is counted
//     in `notJoined`, never thrown.
//
// SKIPPED, COUNTED AND NAMED, NEVER LINED: an edge that is a curtain wall, a
// room or space separation line, a wall in a linked model (its doors cannot be
// read from here), anything that is not a wall (a column, say), or an edge
// that is neither straight nor an arc. A room with no usable boundary is in
// `unenclosed`, as for floors and ceilings. An area is not a room or a space
// and has no walls to line.
//
// NOT DONE HERE: a second run lays a second finish over the first - finishes
// are not room bounding, so the room's boundary cannot show they are there.
// Whether Revit joins a finish's end to anything other than the finish next to
// it is Revit's, and is not claimed.
//
// `rooms` (version 2, every kind): left out, "all" - every room or space given.
// Otherwise numbers or names with commas between; a room is built on when
// either matches, ignoring case, and a word that matches none is named in
// `refused`.

// MILLIMETRES IN, FEET INSIDE (D-71). Every length a caller types is
// millimetres. The add-in converts an XYZ at the boundary and cannot convert a
// bare double - nothing in a contract says which doubles are lengths - so the
// conversion belongs here, once, before the value is used for anything.
const double MillimetresPerFoot = 304.8;
heightAboveLevel = heightAboveLevel / MillimetresPerFoot;
baseOffset = baseOffset / MillimetresPerFoot;

var created = new Dictionary<ElementId, ElementId>();
var withHoles = new List<ElementId>();
var unenclosed = new List<ElementId>();
var refused = new List<string>();
var finishWalls = new List<ElementId>();
var finishSummary = "";
var skippedEdges = 0;
var skippedSummary = "";
var notJoined = 0;

var invariant = System.Globalization.CultureInfo.InvariantCulture;

bool wantCeiling = hostType is CeilingType;
bool wantFloor = hostType is FloorType;
var finishType = hostType as WallType;
bool wantFinish = finishType != null;

// ---- which rooms (version 2) -------------------------------------------------

var roomWords = new List<string>();
var roomsText = (rooms ?? "").Trim();
if (roomsText.Length > 0 && !string.Equals(roomsText, "all", StringComparison.OrdinalIgnoreCase))
{
    foreach (var word in roomsText.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries))
        if (word.Trim().Length > 0) roomWords.Add(word.Trim());
}
var wordsMatched = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
var filteredOut = 0;

Func<SpatialElement, string> numberOf = spatial =>
{
    try { return spatial.Number ?? ""; } catch { return ""; }
};

Func<SpatialElement, string> nameOf = spatial =>
{
    try
    {
        var parameter = spatial.get_Parameter(BuiltInParameter.ROOM_NAME);
        if (parameter != null && parameter.AsString() != null) return parameter.AsString();
    }
    catch { }
    try { return spatial.Name ?? ""; } catch { return ""; }
};

Func<SpatialElement, string> labelOf = spatial =>
{
    var label = (numberOf(spatial) + " " + nameOf(spatial)).Trim();
    return label.Length > 0 ? label : "room " + spatial.Id.ToString();
};

Func<SpatialElement, bool> isWanted = spatial =>
{
    if (roomWords.Count == 0) return true;
    var number = numberOf(spatial).Trim();
    var name = nameOf(spatial).Trim();
    bool hit = false;
    foreach (var word in roomWords)
    {
        if (string.Equals(word, number, StringComparison.OrdinalIgnoreCase)
            || string.Equals(word, name, StringComparison.OrdinalIgnoreCase)
            || string.Equals(word, (number + " " + name).Trim(), StringComparison.OrdinalIgnoreCase)
            || string.Equals(word, (name + " " + number).Trim(), StringComparison.OrdinalIgnoreCase))
        {
            wordsMatched.Add(word);
            hit = true;
        }
    }
    return hit;
};

// ---- floors and ceilings: version 1's calls ----------------------------------

var floorNewStyle = typeof(Floor).GetMethod("Create",
    new[] { typeof(Document), typeof(IList<CurveLoop>), typeof(ElementId), typeof(ElementId) });

System.Reflection.MethodInfo floorOldStyle = null;
foreach (var candidate in doc.Create.GetType().GetMethods())
{
    if (candidate.Name != "NewFloor") continue;
    var parameters = candidate.GetParameters();
    if (parameters.Length == 4 && parameters[0].ParameterType == typeof(CurveArray))
    {
        floorOldStyle = candidate;
        break;
    }
}

var ceilingCreate = typeof(Ceiling).GetMethod("Create",
    new[] { typeof(Document), typeof(IList<CurveLoop>), typeof(ElementId), typeof(ElementId) });

if (!wantCeiling && !wantFloor && !wantFinish)
    refused.Add("The type given is not a floor type, a ceiling type or a wall type, so there is " +
                "nothing to decide what to build.");
else if (wantCeiling && ceilingCreate == null)
    refused.Add("This Revit has no way to create a ceiling from code - it arrived at 2022. A floor " +
                "type works here, or the ceilings are drawn by hand.");
else if (wantFloor && floorNewStyle == null && floorOldStyle == null)
    refused.Add("This Revit has neither way to create a floor.");
else if (wantFinish)
{
    // ==== VERSION 2: A FINISH ON THE ROOM'S WALLS =============================

    WallKind finishKind = WallKind.Basic;
    try { finishKind = finishType.Kind; } catch { }
    double thickness = 0;
    try { thickness = finishType.Width; } catch { }

    if (finishKind == WallKind.Curtain)
        refused.Add("'" + finishType.Name + "' is a curtain wall type, which cannot line a room. Name " +
                    "a basic wall type for the finish - plaster, tile, cladding.");
    else if (thickness <= 0)
        refused.Add("'" + finishType.Name + "' has no thickness, so there is no finish to place.");
    else if (heightAboveLevel <= baseOffset)
        refused.Add("The finish would have no height: its top, " +
                    (heightAboveLevel * MillimetresPerFoot).ToString("0", invariant) +
                    " mm above the room's floor, is not above where it starts, " +
                    (baseOffset * MillimetresPerFoot).ToString("0", invariant) +
                    " mm. Give a top height above the start height.");
    else
    {
        var half = thickness / 2;
        // Revit refuses a wall shorter than its short-curve tolerance; 3 mm is
        // the floor below which a piece is a sliver nobody wanted.
        var shortest = Math.Max(doc.Application.ShortCurveTolerance, 3.0 / MillimetresPerFoot);
        var tolerance = 1.0 / MillimetresPerFoot;
        var probe = 15.0 / MillimetresPerFoot;

        var finishOptions = new SpatialElementBoundaryOptions();
        finishOptions.SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish;

        var skipTally = new Dictionary<string, int>();
        Action<string> skipEdge = reason =>
        {
            skippedEdges++;
            int count;
            skipTally.TryGetValue(reason, out count);
            skipTally[reason] = count + 1;
        };

        var tooShortPieces = 0;
        var refusedPieces = 0;
        var unreadOpenings = 0;
        var roomLines = new List<string>();
        var seen = new HashSet<ElementId>();

        // ---- plan geometry, done here on all eight releases -----------------

        // Where two lines cross: p + t d and q + s e. Null when parallel.
        Func<XYZ, XYZ, XYZ, XYZ, XYZ> lineLine = (p, d, q, e) =>
        {
            var cross = d.X * e.Y - d.Y * e.X;
            if (Math.Abs(cross) < 1e-9) return null;
            var t = ((q.X - p.X) * e.Y - (q.Y - p.Y) * e.X) / cross;
            return new XYZ(p.X + d.X * t, p.Y + d.Y * t, p.Z);
        };

        // Where a line (p, unit d) crosses a circle (c, r) - the crossing
        // nearer `near`, or null when they miss.
        Func<XYZ, XYZ, XYZ, double, XYZ, XYZ> lineCircle = (p, d, c, r, near) =>
        {
            var fx = p.X - c.X;
            var fy = p.Y - c.Y;
            var b = fx * d.X + fy * d.Y;
            var disc = b * b - (fx * fx + fy * fy - r * r);
            if (disc < 0) return null;
            var root = Math.Sqrt(disc);
            var first = new XYZ(p.X + d.X * (-b - root), p.Y + d.Y * (-b - root), p.Z);
            var second = new XYZ(p.X + d.X * (-b + root), p.Y + d.Y * (-b + root), p.Z);
            return first.DistanceTo(near) <= second.DistanceTo(near) ? first : second;
        };

        // Where two circles cross - the crossing nearer `near`, or null.
        Func<XYZ, double, XYZ, double, XYZ, XYZ> circleCircle = (c1, r1, c2, r2, near) =>
        {
            var dx = c2.X - c1.X;
            var dy = c2.Y - c1.Y;
            var d = Math.Sqrt(dx * dx + dy * dy);
            if (d < 1e-9 || d > r1 + r2 + 1e-9 || d < Math.Abs(r1 - r2) - 1e-9) return null;
            var a = (r1 * r1 - r2 * r2 + d * d) / (2 * d);
            var h2 = r1 * r1 - a * a;
            var h = h2 > 0 ? Math.Sqrt(h2) : 0;
            var mx = c1.X + a * dx / d;
            var my = c1.Y + a * dy / d;
            var first = new XYZ(mx - h * dy / d, my + h * dx / d, near.Z);
            var second = new XYZ(mx + h * dy / d, my - h * dx / d, near.Z);
            return first.DistanceTo(near) <= second.DistanceTo(near) ? first : second;
        };

        // An angle in plan, about a centre.
        Func<XYZ, XYZ, double> angleAbout = (point, centre) =>
            Math.Atan2(point.Y - centre.Y, point.X - centre.X);

        // A sweep in (0, 2 pi].
        Func<double, double> sweepOf = value =>
        {
            while (value <= 0) value += 2 * Math.PI;
            while (value > 2 * Math.PI) value -= 2 * Math.PI;
            return value;
        };

        // The width of a door or window: Rough Width, else Width, on the
        // instance or its type. 0 when neither is there.
        Func<Element, double> widthOf = insert =>
        {
            var instance = insert as FamilyInstance;
            foreach (var which in new[] { BuiltInParameter.FAMILY_ROUGH_WIDTH_PARAM,
                                          BuiltInParameter.FAMILY_WIDTH_PARAM })
            {
                try
                {
                    var parameter = insert.get_Parameter(which);
                    if (parameter != null && parameter.HasValue && parameter.StorageType == StorageType.Double
                        && parameter.AsDouble() > 0)
                        return parameter.AsDouble();
                    if (instance != null && instance.Symbol != null)
                    {
                        parameter = instance.Symbol.get_Parameter(which);
                        if (parameter != null && parameter.HasValue && parameter.StorageType == StorageType.Double
                            && parameter.AsDouble() > 0)
                            return parameter.AsDouble();
                    }
                }
                catch { }
            }
            return 0;
        };

        foreach (var element in elements)
        {
            var spatial = element as SpatialElement;
            if (spatial == null || !(spatial is Room || spatial is Space))
            {
                refused.Add("Not a room or space: " + element.Id.ToString());
                continue;
            }
            if (!seen.Add(spatial.Id)) continue;
            if (!isWanted(spatial)) { filteredOut++; continue; }

            IList<IList<BoundarySegment>> loops = null;
            try { loops = spatial.GetBoundarySegments(finishOptions); } catch { }

            double area = 0;
            try { area = spatial.Area; } catch { }

            if (loops == null || loops.Count == 0 || area <= 0) { unenclosed.Add(spatial.Id); continue; }

            Level level = null;
            try { level = spatial.Level; } catch { }
            if (level == null) { refused.Add("No level on room " + spatial.Id.ToString()); continue; }

            double roomBase = 0;
            try
            {
                var lower = spatial.get_Parameter(BuiltInParameter.ROOM_LOWER_OFFSET);
                if (lower != null && lower.HasValue) roomBase = lower.AsDouble();
            }
            catch { }

            // NOT level.Elevation: on a model set out to a survey datum the two
            // differ, and the wrong one builds at the wrong height with no
            // error. CREATE_WALL reads the same one.
            var levelZ = level.ProjectElevation;
            var bottom = levelZ + roomBase + baseOffset;
            var top = levelZ + roomBase + heightAboveLevel;

            // The room's outline in plan, every loop, for telling inside from
            // outside: a point is in the room when a ray from it crosses the
            // loops an odd number of times, so a shaft's loop counts as outside.
            var polygons = new List<List<XYZ>>();
            var signedAreas = new List<double>();
            foreach (var loop in loops)
            {
                var points = new List<XYZ>();
                foreach (var segment in loop)
                {
                    try
                    {
                        var tessellated = segment.GetCurve().Tessellate();
                        for (int k = 0; k + 1 < tessellated.Count; k++) points.Add(tessellated[k]);
                    }
                    catch { }
                }
                double twice = 0;
                for (int k = 0; k < points.Count; k++)
                {
                    var a = points[k];
                    var b = points[(k + 1) % points.Count];
                    twice += a.X * b.Y - b.X * a.Y;
                }
                polygons.Add(points);
                signedAreas.Add(twice / 2);
            }

            var outer = 0;
            for (int k = 1; k < signedAreas.Count; k++)
                if (Math.Abs(signedAreas[k]) > Math.Abs(signedAreas[outer])) outer = k;

            Func<double, double, bool> inside = (x, y) =>
            {
                bool odd = false;
                foreach (var polygon in polygons)
                {
                    for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
                    {
                        var a = polygon[i];
                        var b = polygon[j];
                        if (((a.Y > y) != (b.Y > y))
                            && (x < (b.X - a.X) * (y - a.Y) / (b.Y - a.Y) + a.X))
                            odd = !odd;
                    }
                }
                return odd;
            };

            var madeInRoom = new List<Wall>();
            var madeHost = new List<Wall>();
            var madeNormal = new List<XYZ>();
            var edgesSkippedHere = 0;
            var doorGapsHere = 0;
            var openingsHere = 0;

            for (int loopIndex = 0; loopIndex < loops.Count; loopIndex++)
            {
                var loop = loops[loopIndex];
                var count = loop.Count;

                // ---- each edge: what bounds it, which side is the room ----------

                var curves = new List<Curve>();
                var hosts = new List<Wall>();
                var normals = new List<XYZ>();       // into the room, at the edge's middle
                var isArc = new List<bool>();
                var centres = new List<XYZ>();
                var radii = new List<double>();      // the finish centreline's radius
                var anticlockwise = new List<bool>();
                var directions = new List<XYZ>();    // a straight edge's direction
                var starts = new List<XYZ>();        // the finish centreline's ends,
                var ends = new List<XYZ>();          // trimmed at the corners below

                foreach (var segment in loop)
                {
                    Curve curve = null;
                    try { curve = segment.GetCurve(); } catch { }

                    Wall host = null;
                    string why = null;

                    if (curve == null) why = "an edge Revit gave no line for";
                    else if (!(curve is Line) && !(curve is Arc)) why = "an edge that is neither straight nor an arc";
                    else if (curve.Length < shortest) why = "an edge too short to line";
                    else if (segment.LinkElementId != ElementId.InvalidElementId)
                        why = "a wall in a linked model - its doors cannot be read from here";
                    else
                    {
                        Element bounding = null;
                        try
                        {
                            if (segment.ElementId != ElementId.InvalidElementId)
                                bounding = doc.GetElement(segment.ElementId);
                        }
                        catch { }

                        if (bounding == null) why = "an edge nothing bounds";
                        else if (bounding is Wall)
                        {
                            var wall = (Wall)bounding;
                            WallKind kind = WallKind.Basic;
                            try { kind = wall.WallType.Kind; } catch { }
                            if (kind == WallKind.Curtain) why = "a curtain wall";
                            else host = wall;
                        }
                        else if (bounding is CurveElement)
                        {
                            var category = bounding.Category;
                            if (category != null
                                && category.Id.Equals(new ElementId(BuiltInCategory.OST_MEPSpaceSeparationLines)))
                                why = "a space separation line";
                            else if (category != null
                                && category.Id.Equals(new ElementId(BuiltInCategory.OST_RoomSeparationLines)))
                                why = "a room separation line";
                            else why = "a line, not a wall";
                        }
                        else
                        {
                            var category = bounding.Category;
                            why = "a " + (category != null ? category.Name : "element") + ", not a wall";
                        }
                    }

                    if (why != null) { skipEdge(why); edgesSkippedHere++; host = null; }

                    XYZ normal = null, centre = null, direction = null, start = null, end = null;
                    double radius = 0;
                    bool turning = true;

                    if (host != null)
                    {
                        // WHICH SIDE IS THE ROOM - tested a little either side of
                        // the edge's middle against the room's own outline.
                        var middle = curve.Evaluate(0.5, true);
                        var tangent = curve.ComputeDerivatives(0.5, true).BasisX.Normalize();
                        var left = new XYZ(-tangent.Y, tangent.X, 0);
                        var leftIn = inside(middle.X + left.X * probe, middle.Y + left.Y * probe);
                        var rightIn = inside(middle.X - left.X * probe, middle.Y - left.Y * probe);
                        double side;
                        if (leftIn && !rightIn) side = 1;
                        else if (rightIn && !leftIn) side = -1;
                        else
                        {
                            // Too thin to tell by a probe: the loop's turning.
                            // The outline's inside is on the left of an
                            // anticlockwise loop, and the room round a hole is on
                            // the left of a clockwise one.
                            var anticlockwiseLoop = signedAreas[loopIndex] > 0;
                            side = (loopIndex == outer) == anticlockwiseLoop ? 1 : -1;
                        }
                        normal = left.Multiply(side);

                        var p0 = curve.GetEndPoint(0);
                        var p1 = curve.GetEndPoint(1);

                        if (curve is Line)
                        {
                            direction = new XYZ(p1.X - p0.X, p1.Y - p0.Y, 0).Normalize();
                            var offset = new XYZ(-direction.Y, direction.X, 0).Multiply(side * half);
                            start = new XYZ(p0.X + offset.X, p0.Y + offset.Y, levelZ);
                            end = new XYZ(p1.X + offset.X, p1.Y + offset.Y, levelZ);
                        }
                        else
                        {
                            var arc = (Arc)curve;
                            centre = new XYZ(arc.Center.X, arc.Center.Y, levelZ);
                            var startTangent = curve.ComputeDerivatives(0, true).BasisX.Normalize();
                            turning = (p0.X - centre.X) * startTangent.Y - (p0.Y - centre.Y) * startTangent.X > 0;
                            var startNormal = new XYZ(-startTangent.Y, startTangent.X, 0).Multiply(side);
                            var outward = (p0.X - centre.X) * startNormal.X + (p0.Y - centre.Y) * startNormal.Y > 0;
                            radius = arc.Radius + (outward ? half : -half);
                            if (radius < shortest)
                            {
                                skipEdge("an arc too tight for the finish's thickness");
                                edgesSkippedHere++;
                                host = null;
                            }
                            else
                            {
                                var scale = radius / arc.Radius;
                                start = new XYZ(centre.X + (p0.X - centre.X) * scale,
                                                centre.Y + (p0.Y - centre.Y) * scale, levelZ);
                                end = new XYZ(centre.X + (p1.X - centre.X) * scale,
                                              centre.Y + (p1.Y - centre.Y) * scale, levelZ);
                            }
                        }
                    }

                    curves.Add(curve);
                    hosts.Add(host);
                    normals.Add(normal);
                    isArc.Add(curve is Arc);
                    centres.Add(centre);
                    radii.Add(radius);
                    anticlockwise.Add(turning);
                    directions.Add(direction);
                    starts.Add(start);
                    ends.Add(end);
                }

                // ---- corners: two finishes meet where their centrelines cross ----

                if (count > 1)
                {
                    for (int i = 0; i < count; i++)
                    {
                        var j = (i + 1) % count;
                        if (hosts[i] == null || hosts[j] == null) continue;
                        var corner = curves[i].GetEndPoint(1);
                        if (corner.DistanceTo(curves[j].GetEndPoint(0)) > 0.01) continue;

                        var near = new XYZ((ends[i].X + starts[j].X) / 2, (ends[i].Y + starts[j].Y) / 2, levelZ);
                        XYZ meet;
                        if (!isArc[i] && !isArc[j]) meet = lineLine(starts[i], directions[i], starts[j], directions[j]);
                        else if (!isArc[i]) meet = lineCircle(starts[i], directions[i], centres[j], radii[j], near);
                        else if (!isArc[j]) meet = lineCircle(starts[j], directions[j], centres[i], radii[i], near);
                        else meet = circleCircle(centres[i], radii[i], centres[j], radii[j], near);

                        // Parallel neighbours already meet; a crossing far from
                        // the corner is a sharp angle, left square rather than
                        // run out to a long point.
                        if (meet == null) continue;
                        var flatCorner = new XYZ(corner.X, corner.Y, levelZ);
                        if (meet.DistanceTo(flatCorner) > 12 * half + tolerance) continue;
                        ends[i] = meet;
                        starts[j] = meet;
                    }
                }

                // ---- each lined edge: its finish centreline, then the pieces ----

                for (int i = 0; i < count; i++)
                {
                    var host = hosts[i];
                    if (host == null) continue;

                    var start = starts[i];
                    var end = ends[i];
                    var normal = normals[i];
                    double length;
                    Func<XYZ, double> along;
                    Func<double, XYZ> pointAt;
                    Func<double, XYZ> normalAt;          // into the room, at a distance along
                    Func<XYZ, double> across;            // from the finish's centreline, + into the room
                    bool arcEdge = isArc[i];

                    if (!arcEdge)
                    {
                        var direction = directions[i];
                        var run = new XYZ(end.X - start.X, end.Y - start.Y, 0);
                        length = run.GetLength();
                        if (length < shortest || run.DotProduct(direction) <= 0)
                        {
                            skipEdge("an edge too short to line once the corners are met");
                            edgesSkippedHere++;
                            continue;
                        }
                        var from = start;
                        var unit = run.Normalize();
                        along = point => (point.X - from.X) * unit.X + (point.Y - from.Y) * unit.Y;
                        pointAt = distance => new XYZ(from.X + unit.X * distance, from.Y + unit.Y * distance, levelZ);
                        normalAt = distance => normal;
                        across = point => (point.X - from.X) * normal.X + (point.Y - from.Y) * normal.Y;
                    }
                    else
                    {
                        var centre = centres[i];
                        var radius = radii[i];
                        var sense = anticlockwise[i] ? 1.0 : -1.0;
                        var startAngle = angleAbout(start, centre);
                        var sweep = sweepOf(sense * (angleAbout(end, centre) - startAngle));
                        var p0 = curves[i].GetEndPoint(0);
                        var p1 = curves[i].GetEndPoint(1);
                        var firstSweep = sweepOf(sense * (angleAbout(p1, centre) - angleAbout(p0, centre)));
                        length = sweep * radius;
                        if (length < shortest || sweep > firstSweep + Math.PI / 4)
                        {
                            skipEdge("an edge too short to line once the corners are met");
                            edgesSkippedHere++;
                            continue;
                        }
                        var outside = (2 * Math.PI - sweep) / 2;
                        along = point =>
                        {
                            var turn = sense * (angleAbout(point, centre) - startAngle);
                            while (turn < -outside) turn += 2 * Math.PI;
                            while (turn >= sweep + outside) turn -= 2 * Math.PI;
                            return turn * radius;
                        };
                        pointAt = distance =>
                        {
                            var angle = startAngle + sense * distance / radius;
                            return new XYZ(centre.X + radius * Math.Cos(angle), centre.Y + radius * Math.Sin(angle), levelZ);
                        };
                        // On an arc the room is towards the centre or away from
                        // it all along - whichever it is at the edge's middle.
                        var edgeMiddle = curves[i].Evaluate(0.5, true);
                        var inward = (edgeMiddle.X - centre.X) * normal.X + (edgeMiddle.Y - centre.Y) * normal.Y > 0
                            ? 1.0 : -1.0;
                        normalAt = distance =>
                        {
                            var here = pointAt(distance);
                            return new XYZ(here.X - centre.X, here.Y - centre.Y, 0).Normalize().Multiply(inward);
                        };
                        across = point =>
                        {
                            var dx = point.X - centre.X;
                            var dy = point.Y - centre.Y;
                            return (Math.Sqrt(dx * dx + dy * dy) - radius) * inward;
                        };
                    }

                    // The wall behind lies from -half to -(half + its width)
                    // across; what is set into it is found there and nowhere else.
                    double hostWidth = 0;
                    try { hostWidth = host.Width; } catch { }
                    var wallMiddle = -(half + hostWidth / 2);
                    var reach = hostWidth / 2 + 100.0 / MillimetresPerFoot;

                    // ---- the doors, windows and openings in the wall behind ----

                    IList<ElementId> insertIds = null;
                    try { insertIds = host.FindInserts(true, false, true, true); } catch { }

                    var holes = new List<double[]>();   // from, to, low, high
                    foreach (var insertId in insertIds ?? new List<ElementId>())
                    {
                        Element insert = null;
                        try { insert = doc.GetElement(insertId); } catch { }
                        if (insert == null) continue;

                        // WHAT CUTS THE WALL, NOT WHAT HANGS ON IT. A door, a
                        // window, an opening and a curtain wall set into the wall
                        // go through it; a socket or a light hosted on a face does
                        // not, and the finish runs behind it. Anything else counts
                        // only when its box goes right through the wall.
                        bool cutsThrough = insert is Opening || insert is Wall;
                        if (!cutsThrough && insert.Category != null)
                            cutsThrough = insert.Category.Id.Equals(new ElementId(BuiltInCategory.OST_Doors))
                                          || insert.Category.Id.Equals(new ElementId(BuiltInCategory.OST_Windows));

                        double low = 0, high = 0, from = 0, to = 0;
                        XYZ where = null;
                        bool read = false;

                        var opening = insert as Opening;
                        if (opening != null)
                        {
                            try
                            {
                                if (opening.IsRectBoundary && opening.BoundaryRect != null && opening.BoundaryRect.Count >= 2)
                                {
                                    var a = opening.BoundaryRect[0];
                                    var b = opening.BoundaryRect[1];
                                    low = Math.Min(a.Z, b.Z);
                                    high = Math.Max(a.Z, b.Z);
                                    from = Math.Min(along(a), along(b));
                                    to = Math.Max(along(a), along(b));
                                    where = new XYZ((a.X + b.X) / 2, (a.Y + b.Y) / 2, levelZ);
                                    read = true;
                                }
                            }
                            catch { }
                        }

                        if (!read)
                        {
                            BoundingBoxXYZ box = null;
                            try { box = insert.get_BoundingBox(null); } catch { }
                            if (box == null) { unreadOpenings++; continue; }

                            var corners = new List<XYZ>();
                            foreach (var x in new[] { box.Min.X, box.Max.X })
                                foreach (var y in new[] { box.Min.Y, box.Max.Y })
                                    foreach (var z in new[] { box.Min.Z, box.Max.Z })
                                        corners.Add(box.Transform.OfPoint(new XYZ(x, y, z)));

                            if (!cutsThrough && hostWidth > 0
                                && corners.Max(c => across(c)) - corners.Min(c => across(c)) >= 0.9 * hostWidth)
                                cutsThrough = true;

                            low = corners.Min(c => c.Z);
                            high = corners.Max(c => c.Z);

                            var width = widthOf(insert);
                            var location = insert.Location as LocationPoint;
                            if (width > 0 && location != null)
                            {
                                var middle = along(location.Point);
                                from = middle - width / 2;
                                to = middle + width / 2;
                                where = location.Point;
                            }
                            else
                            {
                                from = corners.Min(c => along(c));
                                to = corners.Max(c => along(c));
                                where = box.Transform.OfPoint(new XYZ((box.Min.X + box.Max.X) / 2,
                                                                      (box.Min.Y + box.Max.Y) / 2, box.Min.Z));
                            }
                        }

                        if (!cutsThrough) continue;

                        // In THIS stretch of the wall: a door far along the same
                        // wall projects onto the short face at its end, and must
                        // not leave a hole there.
                        if (Math.Abs(across(where) - wallMiddle) > reach) continue;

                        // Only what is on this stretch, at the finish's height.
                        if (high <= bottom + tolerance || low >= top - tolerance) continue;
                        if (to <= tolerance || from >= length - tolerance) continue;
                        holes.Add(new[] { Math.Max(0, from), Math.Min(length, to),
                                          Math.Max(low, bottom), Math.Min(high, top) });
                        if (low <= bottom + tolerance) doorGapsHere++;
                        else openingsHere++;
                    }

                    // ---- the pieces: stretches between the openings, each built
                    // ---- in bands of height round whatever is in that stretch

                    var stops = new List<double> { 0, length };
                    foreach (var hole in holes) { stops.Add(hole[0]); stops.Add(hole[1]); }
                    stops.Sort();
                    var distinct = new List<double>();
                    foreach (var stop in stops)
                        if (distinct.Count == 0 || stop - distinct[distinct.Count - 1] > tolerance) distinct.Add(stop);
                    if (length - distinct[distinct.Count - 1] > tolerance) distinct.Add(length);
                    else distinct[distinct.Count - 1] = length;

                    for (int k = 0; k + 1 < distinct.Count; k++)
                    {
                        var a = distinct[k];
                        var b = distinct[k + 1];
                        if (b - a < shortest) { tooShortPieces++; continue; }
                        var middle = (a + b) / 2;

                        var blocked = new List<double[]>();
                        foreach (var hole in holes)
                            if (hole[0] < middle && hole[1] > middle) blocked.Add(new[] { hole[2], hole[3] });
                        blocked.Sort((x, y) => x[0].CompareTo(y[0]));

                        var bands = new List<double[]>();
                        var floorOfBand = bottom;
                        foreach (var block in blocked)
                        {
                            if (block[0] > floorOfBand + tolerance) bands.Add(new[] { floorOfBand, block[0] });
                            if (block[1] > floorOfBand) floorOfBand = block[1];
                        }
                        if (top > floorOfBand + tolerance) bands.Add(new[] { floorOfBand, top });

                        Curve stretch = null;
                        try
                        {
                            stretch = arcEdge
                                ? (Curve)Arc.Create(pointAt(a), pointAt(b), pointAt(middle))
                                : Line.CreateBound(pointAt(a), pointAt(b));
                        }
                        catch { }

                        foreach (var band in bands)
                        {
                            if (band[1] - band[0] < shortest) { tooShortPieces++; continue; }
                            if (stretch == null) { refusedPieces++; continue; }

                            Wall piece = null;
                            try
                            {
                                piece = Wall.Create(doc, stretch, finishType.Id, level.Id,
                                                    band[1] - band[0], band[0] - levelZ, false, false);
                            }
                            catch { }
                            if (piece == null) { refusedPieces++; continue; }

                            // NOT ROOM BOUNDING - before anything reads the room again.
                            try
                            {
                                var bounding = piece.get_Parameter(BuiltInParameter.WALL_ATTR_ROOM_BOUNDING);
                                if (bounding != null && !bounding.IsReadOnly) bounding.Set(0);
                            }
                            catch { }

                            madeInRoom.Add(piece);
                            madeHost.Add(host);
                            madeNormal.Add(normalAt(middle));
                        }
                    }
                }
            }

            // ---- once the room's finishes stand: face the room, join ----------

            if (madeInRoom.Count > 0)
            {
                try { doc.Regenerate(); } catch { }
                for (int k = 0; k < madeInRoom.Count; k++)
                {
                    var piece = madeInRoom[k];
                    try { if (piece.Orientation.DotProduct(madeNormal[k]) < 0) piece.Flip(); } catch { }

                    if (joinToHost)
                    {
                        try
                        {
                            if (!JoinGeometryUtils.AreElementsJoined(doc, piece, madeHost[k]))
                                JoinGeometryUtils.JoinGeometry(doc, piece, madeHost[k]);
                        }
                        catch { notJoined++; }
                    }
                    finishWalls.Add(piece.Id);
                }
            }

            var roomLine = labelOf(spatial) + ": " + madeInRoom.Count.ToString(invariant) + " finish wall" +
                           (madeInRoom.Count == 1 ? "" : "s");
            if (doorGapsHere > 0)
                roomLine += ", a gap at " + doorGapsHere.ToString(invariant) + " door" + (doorGapsHere == 1 ? "" : "s") +
                            " or opening" + (doorGapsHere == 1 ? "" : "s") + " to the floor";
            if (openingsHere > 0)
                roomLine += ", built round " + openingsHere.ToString(invariant) + " window" +
                            (openingsHere == 1 ? "" : "s") + " or opening" + (openingsHere == 1 ? "" : "s");
            if (edgesSkippedHere > 0)
                roomLine += ", " + edgesSkippedHere.ToString(invariant) + " edge" + (edgesSkippedHere == 1 ? "" : "s") +
                            " not lined";
            roomLines.Add(roomLine);
        }

        finishSummary = string.Join("  ||  ", roomLines);

        var said = new List<string>();
        foreach (var pair in skipTally.OrderByDescending(p => p.Value))
            said.Add(pair.Key + ": " + pair.Value.ToString(invariant));
        if (unenclosed.Count > 0) said.Add("rooms with no usable boundary, nothing built: " + unenclosed.Count.ToString(invariant));
        if (tooShortPieces > 0) said.Add("pieces too short to build: " + tooShortPieces.ToString(invariant));
        if (refusedPieces > 0) said.Add("pieces Revit would not build: " + refusedPieces.ToString(invariant));
        if (unreadOpenings > 0) said.Add("openings whose size could not be read, finish built across them: " + unreadOpenings.ToString(invariant));
        if (notJoined > 0) said.Add("finishes Revit would not join to the wall behind: " + notJoined.ToString(invariant));
        if (filteredOut > 0) said.Add("rooms given but not named in rooms: " + filteredOut.ToString(invariant));
        skippedSummary = string.Join("; ", said);
    }
}
else
{
    var boundaryOptions = new SpatialElementBoundaryOptions();

    foreach (var element in elements)
    {
        var room = element as SpatialElement;
        if (room == null) { refused.Add("Not a room or space: " + element.Id.ToString()); continue; }
        if (!isWanted(room)) { filteredOut++; continue; }

        IList<IList<BoundarySegment>> loops = null;
        try { loops = room.GetBoundarySegments(boundaryOptions); } catch { }

        double area = 0;
        try { area = room.Area; } catch { }

        if (loops == null || loops.Count == 0 || area <= 0) { unenclosed.Add(room.Id); continue; }

        // Every loop: the first is the outline and the rest are holes.
        var curveLoops = new List<CurveLoop>();
        foreach (var loop in loops)
        {
            var curves = new List<Curve>();
            foreach (var segment in loop)
            {
                Curve curve = null;
                try { curve = segment.GetCurve(); } catch { }
                if (curve != null) curves.Add(curve);
            }
            if (curves.Count < 3) continue;
            try { curveLoops.Add(CurveLoop.Create(curves)); } catch { }
        }

        if (curveLoops.Count == 0) { unenclosed.Add(room.Id); continue; }
        if (curveLoops.Count > 1) withHoles.Add(room.Id);

        var levelId = ElementId.InvalidElementId;
        try { if (room.Level != null) levelId = room.Level.Id; } catch { }
        if (levelId == ElementId.InvalidElementId)
        {
            refused.Add("No level on room " + room.Id.ToString());
            continue;
        }

        Element built = null;

        if (wantCeiling)
        {
            try
            {
                built = ceilingCreate.Invoke(null,
                    new object[] { doc, (IList<CurveLoop>)curveLoops, hostType.Id, levelId }) as Element;
            }
            catch { }
        }
        else if (floorNewStyle != null)
        {
            try
            {
                built = floorNewStyle.Invoke(null,
                    new object[] { doc, (IList<CurveLoop>)curveLoops, hostType.Id, levelId }) as Element;
            }
            catch { }
        }
        else
        {
            // Pre-2022: one loop only, so a room with a hole gets a slab over
            // the shaft. It is built and NAMED rather than skipped - the caller
            // decides whether that is acceptable, but nobody finds out later.
            try
            {
                var array = new CurveArray();
                foreach (var curve in curveLoops[0]) array.Append(curve);
                var levelElement = doc.GetElement(levelId) as Level;
                built = floorOldStyle.Invoke(doc.Create,
                    new object[] { array, hostType, levelElement, false }) as Element;
                if (curveLoops.Count > 1)
                    refused.Add("Room " + room.Id.ToString() + ": this release takes one loop only, so " +
                                "its hole was NOT cut.");
            }
            catch { }
        }

        if (built == null) { refused.Add("Revit refused to build on room " + room.Id.ToString()); continue; }

        if (wantCeiling)
        {
            try
            {
                var offset = built.get_Parameter(BuiltInParameter.CEILING_HEIGHTABOVELEVEL_PARAM);
                if (offset != null && !offset.IsReadOnly) offset.Set(heightAboveLevel);
            }
            catch { }
        }

        created[room.Id] = built.Id;
    }

    if (filteredOut > 0)
        skippedSummary = "rooms given but not named in rooms: " + filteredOut.ToString(invariant);
}

// A word in `rooms` that named no room given is said, never dropped.
foreach (var word in roomWords)
    if (!wordsMatched.Contains(word))
        refused.Add("No room or space given is numbered or named '" + word + "' - nothing was built for it.");
