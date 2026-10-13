// NOT STANDALONE. Assumes `doc`, `view`, `elements`, `offsetMm`, `withOverall`
// and `side` are in scope, and leaves `created`, `segmentsDisagree`,
// `noOpeningFaces`, `notAWall`, `sideUsed` and `refused` behind.
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
//
// WHICH SIDE OF THE WALL (version 2). Version 1 always drew to the RIGHT of
// the wall as it was drawn - its location line, start to end - so on a wall
// drawn left to right the string went below it on the plan, whatever was
// there. Measured 2026-10-13 on the futuristic house (Project2, Revit 2020.2):
// the outside stone wall at y 19850, rooms south of it, got its 900 mm string
// on the ROOM side, through the Foyer, WC and Store tags. `side` lets the
// caller choose, and BLANK IS VERSION 1 EXACTLY:
//
//   blank    right of the wall as drawn - version 1, unchanged
//   right    the same, said out loud
//   left     the other side of the wall as drawn
//   outside  AWAY FROM THE ROOMS: the side a point just off the wall finds no
//            room or space on, when the other side finds one. With rooms on
//            both sides or neither, the side farther from the middle of the
//            building (the walls joined to it, corner to corner), and only on a tie,
//            the wall's own exterior face as Revit has it
//   inside   the opposite of outside, by the same reading
//
// Each wall's answer says which side it took and why, in plan words - above,
// below, left or right of the wall on the view - so it can be checked against
// the drawing without anybody reading coordinates.

const double MmToFeet = 1.0 / 304.8;

var created = new List<ElementId>();
var segmentsDisagree = new List<ElementId>();
var noOpeningFaces = new List<ElementId>();
var notAWall = new List<ElementId>();
var sideUsed = new List<string>();
var refused = new List<string>();

var sideMode = (side ?? "").Trim().ToLowerInvariant();
bool sideKnown = sideMode == "" || sideMode == "right" || sideMode == "left"
              || sideMode == "outside" || sideMode == "inside";
if (!sideKnown)
{
    // REFUSED WHOLE, BEFORE ANYTHING IS DRAWN. A misspelt side drawing every
    // string on the version 1 side would look like it worked.
    refused.Add("side \"" + side + "\" is not one this knows - leave it blank, or type outside, "
              + "inside, left or right. Nothing was drawn.");
}

// The phase rooms are looked for in: the view's own, so a demolished room in
// an earlier phase does not decide which side is outside.
Phase viewPhase = null;
try
{
    var phaseParameter = view.get_Parameter(BuiltInParameter.VIEW_PHASE);
    if (phaseParameter != null) viewPhase = doc.GetElement(phaseParameter.AsElementId()) as Phase;
}
catch { viewPhase = null; }

Func<XYZ, bool> inARoom = point =>
{
    try
    {
        if (viewPhase != null)
            return doc.GetRoomAtPoint(point, viewPhase) != null
                || doc.GetSpaceAtPoint(point, viewPhase) != null;
        return doc.GetRoomAtPoint(point) != null || doc.GetSpaceAtPoint(point) != null;
    }
    catch { return false; }
};

// THE MIDDLE OF THE BUILDING THIS WALL BELONGS TO: the centre, in plan, of
// the walls reached from it through the joins at their ends - its corners,
// their corners, and on round. Not every wall on the level: a level can carry
// two buildings, and the middle of both together can sit outside each of
// them. Only a wall joined to fewer than two others falls back to every wall
// on its level.
var middleFor = new Dictionary<ElementId, XYZ>();
Func<IEnumerable<Wall>, XYZ> centreOf = group =>
{
    double minX = double.MaxValue, minY = double.MaxValue;
    double maxX = double.MinValue, maxY = double.MinValue;
    foreach (var member in group)
    {
        var box = member.get_BoundingBox(null);
        if (box == null) continue;
        minX = Math.Min(minX, box.Min.X); minY = Math.Min(minY, box.Min.Y);
        maxX = Math.Max(maxX, box.Max.X); maxY = Math.Max(maxY, box.Max.Y);
    }
    return minX <= maxX ? new XYZ((minX + maxX) / 2, (minY + maxY) / 2, 0) : null;
};
Func<Wall, XYZ> buildingMiddle = forWall =>
{
    XYZ middle;
    if (middleFor.TryGetValue(forWall.Id, out middle)) return middle;

    var joined = new Dictionary<ElementId, Wall> { { forWall.Id, forWall } };
    var toVisit = new Queue<Wall>();
    toVisit.Enqueue(forWall);
    while (toVisit.Count > 0)
    {
        var at = toVisit.Dequeue();
        var atCurve = at.Location as LocationCurve;
        if (atCurve == null) continue;
        for (int end = 0; end < 2; end++)
        {
            ElementArray atJoin = null;
            try { atJoin = atCurve.get_ElementsAtJoin(end); } catch { atJoin = null; }
            if (atJoin == null) continue;
            foreach (Element neighbour in atJoin)
            {
                var neighbourWall = neighbour as Wall;
                if (neighbourWall == null || joined.ContainsKey(neighbourWall.Id)) continue;
                joined[neighbourWall.Id] = neighbourWall;
                toVisit.Enqueue(neighbourWall);
            }
        }
    }

    if (joined.Count >= 3)
    {
        middle = centreOf(joined.Values);
    }
    else
    {
        var onLevel = new List<Wall>();
        foreach (Wall other in new FilteredElementCollector(doc).OfClass(typeof(Wall)))
            if (other.LevelId == forWall.LevelId) onLevel.Add(other);
        middle = centreOf(onLevel);
    }
    middleFor[forWall.Id] = middle;
    return middle;
};

// A direction in plan words, against the view's own up and right.
Func<XYZ, string> planWords = towards =>
{
    double up = towards.DotProduct(view.UpDirection);
    double right = towards.DotProduct(view.RightDirection);
    if (Math.Abs(up) >= Math.Abs(right)) return up >= 0 ? "above" : "below";
    return right >= 0 ? "right of" : "left of";
};

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

foreach (var wall in sideKnown ? walls : new List<Wall>())
{
    var location = wall.Location as LocationCurve;
    Line run = location != null ? location.Curve as Line : null;
    if (run == null) { noOpeningFaces.Add(wall.Id); continue; }

    XYZ start = run.GetEndPoint(0);
    XYZ direction = run.Direction;
    XYZ across = direction.CrossProduct(XYZ.BasisZ).Normalize();

    // WHICH SIDE. `across` is version 1's side - right of the wall as drawn -
    // and blank or "right" leaves it exactly as it was.
    string why = sideMode == "" ? "as version 1 - right of the wall as drawn"
               : sideMode == "right" ? "right of the wall as drawn" : "left of the wall as drawn";
    if (sideMode == "left") across = across.Negate();
    if (sideMode == "outside" || sideMode == "inside")
    {
        // A point just clear of each face, 300 mm above the wall's base, at
        // the middle of its run.
        XYZ middle = run.Evaluate(0.5, true);
        double clear = wall.Width / 2 + 300 * MmToFeet;
        XYZ probeRight = middle + across * clear + XYZ.BasisZ * (300 * MmToFeet);
        XYZ probeLeft = middle - across * clear + XYZ.BasisZ * (300 * MmToFeet);
        bool roomRight = inARoom(probeRight);
        bool roomLeft = inARoom(probeLeft);

        int outwardIsRight = 0;
        if (roomRight != roomLeft)
        {
            outwardIsRight = roomRight ? -1 : 1;
            why = "no room on that side, a room on the other";
        }
        else
        {
            XYZ centre = buildingMiddle(wall);
            if (centre != null)
            {
                var flatRight = new XYZ(probeRight.X - centre.X, probeRight.Y - centre.Y, 0);
                var flatLeft = new XYZ(probeLeft.X - centre.X, probeLeft.Y - centre.Y, 0);
                double gap = flatRight.GetLength() - flatLeft.GetLength();
                if (Math.Abs(gap) > 1e-6)
                {
                    outwardIsRight = gap > 0 ? 1 : -1;
                    why = (roomRight ? "rooms on both sides" : "no room on either side")
                        + " - the side farther from the middle of the building";
                }
            }
            if (outwardIsRight == 0)
            {
                outwardIsRight = wall.Orientation.DotProduct(across) >= 0 ? 1 : -1;
                why = "the wall's own exterior face - nothing else told the sides apart";
            }
        }

        bool wantRight = sideMode == "outside" ? outwardIsRight > 0 : outwardIsRight < 0;
        if (!wantRight) across = across.Negate();
        why = sideMode + ": " + why;
    }

    sideUsed.Add(wall.Name + " " + wall.Id + ": " + planWords(across) + " the wall on the view ("
               + why + ")");

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
